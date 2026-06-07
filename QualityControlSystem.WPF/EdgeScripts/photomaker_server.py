import argparse
import json
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

import cv2

import photomaker_rasp as maker


latest_frame = None
latest_status = {"state": "starting", "template": None, "message": ""}
capture_requested = threading.Event()
stop_requested = threading.Event()
frame_lock = threading.Lock()
status_lock = threading.Lock()


def set_status(**kwargs):
    with status_lock:
        latest_status.update(kwargs)


def encode_frame(frame):
    ok, buffer = cv2.imencode(".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), 82])
    return buffer.tobytes() if ok else None


class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        path = urlparse(self.path).path
        if path == "/status":
            self.write_json(latest_status)
            return

        if path == "/capture":
            capture_requested.set()
            self.write_json({"ok": True, "message": "capture requested"})
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


def camera_loop():
    config = maker.load_config()
    cap_csi = maker.init_csi_camera()
    if cap_csi is None:
        set_status(state="error", message="CSI camera is not available")
        return

    template_num = maker.get_next_template_number()
    set_status(state="running", template=template_num, message="photomaker is ready")

    try:
        while not stop_requested.is_set():
            try:
                arr = cap_csi.capture_array()
                frame = maker.convert_picamera_array_to_bgr(arr)
            except Exception as exc:
                set_status(state="error", message=f"CSI capture failed: {exc}")
                break

            roi = config.get("roi", maker.DEFAULT_CONFIG["roi"])
            x, y, w, h = maker.safe_roi(frame, roi)
            crop = frame[y:y + h, x:x + w]
            processed, _, _, _, _ = maker.preprocess(crop, config["processing"])
            result = maker.find_main_contour(processed, int(config["processing"].get("min_contour_area", 1500)))
            contour = result[0] if result else None

            full_display = frame.copy()
            cv2.rectangle(full_display, (x, y), (x + w, y + h), (0, 255, 0), 3)
            cv2.putText(full_display, "ROI", (x + 8, y + 28), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
            overlay = maker.draw_overlay(crop, contour, config, template_num)
            full_display[y:y + h, x:x + w] = overlay

            encoded = encode_frame(full_display)
            if encoded is not None:
                with frame_lock:
                    global latest_frame
                    latest_frame = encoded

            if capture_requested.is_set():
                capture_requested.clear()
                maker.save_template(frame, template_num, config)
                set_status(state="running", template=template_num, message=f"template #{template_num} saved")
                template_num += 1

            time.sleep(0.03)
    finally:
        try:
            cap_csi.stop()
            cap_csi.close()
        except Exception:
            pass
        set_status(state="stopped", message="photomaker stopped")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8081)
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
