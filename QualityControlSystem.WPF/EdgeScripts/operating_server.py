import argparse
import json
import math
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

import cv2
import numpy as np

import operating


latest_frame = None
latest_status = {"state": "starting", "message": ""}
results = []
stop_requested = threading.Event()
frame_lock = threading.Lock()
status_lock = threading.Lock()
results_lock = threading.Lock()


def set_status(**kwargs):
    with status_lock:
        latest_status.update(kwargs)


def clean_number(value):
    if value is None:
        return None
    try:
        number = float(value)
        return None if math.isnan(number) or math.isinf(number) else number
    except Exception:
        return None


def encode_frame(frame):
    ok, buffer = cv2.imencode(".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), 82])
    return buffer.tobytes() if ok else None


class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        path = urlparse(self.path).path
        if path == "/status":
            self.write_json(latest_status)
            return

        if path == "/results":
            with results_lock:
                payload = list(results)
            self.write_json(payload)
            return

        if path == "/frame.jpg":
            with frame_lock:
                frame = latest_frame
            if frame is None:
                self.send_error(404, "frame is not ready")
                return
            self.send_response(200)
            self.send_header("Content-Type", "image/jpeg")
            self.send_header("Cache-Control", "no-cache")
            self.end_headers()
            self.wfile.write(frame)
            return

        self.send_error(404)

    def do_POST(self):
        if urlparse(self.path).path == "/stop":
            stop_requested.set()
            self.write_json({"ok": True})
            return
        self.send_error(404)

    def write_json(self, payload):
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def log_message(self, format, *args):
        return


def append_result(summary, sensors):
    sensor_values = dict(getattr(sensors, "values", {}) or {})
    item = {
        "id": len(results) + 1,
        "recordedAt": time.strftime("%Y-%m-%dT%H:%M:%S", time.localtime()),
        "status": summary.get("status"),
        "templateId": int(summary.get("template_id", 0)),
        "similarity": clean_number(summary.get("median_similarity")),
        "contourScore": clean_number(summary.get("median_contour")),
        "filledScore": clean_number(summary.get("median_filled")),
        "shapeScore": clean_number(summary.get("median_shape")),
        "reason": summary.get("reason"),
        "weight": clean_number(sensor_values.get("weight")),
        "lightPercent": clean_number(sensor_values.get("light_percent")),
        "lightAdc": clean_number(sensor_values.get("light_adc")),
    }
    with results_lock:
        results.append(item)
        del results[:-100]


def camera_loop():
    base_config = operating.load_base_config()
    templates = operating.load_templates(base_config)
    if not templates:
        set_status(state="error", message="templates were not found")
        return

    cap_csi = operating.init_csi_camera()
    if cap_csi is None:
        set_status(state="error", message="CSI camera is not available")
        return

    sensors = operating.ArduinoSensors()
    sensors.connect()

    state = "WAITING"
    placement_started_at = None
    history = []
    final_summary = None
    final_recorded = False
    analysis_frames = max(int(t.comparison.get("analysis_frames", 15)) for t in templates)
    roi_cfg = templates[0].roi
    set_status(state=state, message="operating is ready")

    try:
        while not stop_requested.is_set():
            try:
                arr = cap_csi.capture_array()
                frame = operating.convert_picamera_array_to_bgr(arr)
            except Exception as exc:
                set_status(state="error", message=f"CSI capture failed: {exc}")
                break

            x, y, w, h = operating.safe_roi(frame, roi_cfg)
            roi = frame[y:y + h, x:x + w]
            part_present, presence_mask, presence_contour = operating.detect_part_present(roi, templates[0].processing)
            current_result = None
            current_contour = presence_contour
            countdown = 0.0
            active_sensor_read = False

            if state == "WAITING":
                sensors.discard_when_inactive()
                final_summary = None
                final_recorded = False
                history = []
                if part_present:
                    placement_started_at = time.time()
                    state = "PLACEMENT_WAIT"

            elif state == "PLACEMENT_WAIT":
                sensors.discard_when_inactive()
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                else:
                    elapsed = time.time() - (placement_started_at or time.time())
                    countdown = max(0.0, operating.PLACEMENT_DELAY_SEC - elapsed)
                    if countdown <= 0:
                        state = "ANALYZING"
                        history = []

            elif state == "ANALYZING":
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                    history = []
                    final_summary = None
                    sensors.discard_when_inactive()
                else:
                    active_sensor_read = True
                    sensors.read_active()
                    checks = [operating.evaluate_against_template(roi, t) for t in templates]
                    current_result = operating.choose_best_result(checks)
                    current_contour = current_result["contour"] if current_result["contour"] is not None else presence_contour
                    history.append({
                        "template_id": current_result["template"].template_id,
                        "similarity": current_result["similarity"],
                        "status": current_result["status"],
                        "filled_score": current_result["filled_score"],
                        "contour_score": current_result["contour_score"],
                        "shape_score": current_result["shape_score"],
                        "reason": current_result["reason"],
                    })
                    if len(history) >= analysis_frames:
                        final_summary = operating.summarize_history(history, current_result["template"].comparison)
                        state = "HOLD_RESULT"

            elif state == "HOLD_RESULT":
                if final_summary is not None and not final_recorded:
                    append_result(final_summary, sensors)
                    final_recorded = True
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                    history = []
                    final_summary = None
                    final_recorded = False
                    sensors.discard_when_inactive()

            full_display = operating.build_main_display(
                frame=frame,
                roi_rect=(x, y, w, h),
                roi_frame=roi,
                state=state,
                part_present=part_present,
                countdown=countdown,
                current_contour=current_contour,
                current_result=current_result,
                final_summary=final_summary,
                sensors=sensors,
                active_sensors=active_sensor_read,
            )
            encoded = encode_frame(full_display)
            if encoded is not None:
                with frame_lock:
                    global latest_frame
                    latest_frame = encoded

            set_status(state=state, message="operating is running")
            time.sleep(0.03)
    finally:
        try:
            cap_csi.stop()
            cap_csi.close()
        except Exception:
            pass
        sensors.close()
        set_status(state="stopped", message="operating stopped")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8082)
    args = parser.parse_args()

    thread = threading.Thread(target=camera_loop, daemon=True)
    thread.start()

    server = ThreadingHTTPServer((args.host, args.port), Handler)
    try:
        while not stop_requested.is_set():
            server.handle_request()
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
