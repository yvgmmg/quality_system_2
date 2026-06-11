import cv2
import numpy as np
import os
import json
import time
import argparse
import threading
from colorama import init, Fore, Style
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

# ==================== CONFIG ====================
TEMPLATE_DIR = "template"
CONFIG_PATH = "template_config.json"
WINDOW_NAME = "CSI Template Maker v4 default config"
CAPTURE_KEY = ord("c")
EXIT_KEY = ord("q")
HOST = "0.0.0.0"
DEFAULT_PORT = 8081
ENDPOINT_STATUS = "/status"
ENDPOINT_CAPTURE = "/capture"
ENDPOINT_FRAME = "/frame.jpg"
ENDPOINT_STOP = "/stop"
JSON_CONTENT_TYPE = "application/json; charset=utf-8"
JPEG_CONTENT_TYPE = "image/jpeg"
JPEG_QUALITY = 82
FRAME_DELAY_SEC = 0.03

FRAME_WIDTH = 1280
FRAME_HEIGHT = 720
CAMERA_WARMUP_SEC = 1.0

# Конфигурация по умолчанию взята из приложенного template_config.json.
DEFAULT_CONFIG = {
    "template_id": 2,
    "camera": "CSI-front",
    "roi": [
        110,
        90,
        1000,
        570
    ],
    "processing": {
        "canny_low": 0,
        "canny_high": 114,
        "adaptive_block": 37,
        "adaptive_c": 4,
        "morph_kernel_size": 5,
        "morph_dilate_iter": 2,
        "morph_erode_iter": 1,
        "morph_open_iter": 1,
        "min_contour_area": 1500
    },
    "comparison": {
        "pixel_tolerance": 20,
        "mask_diff_threshold": 0.2,
        "shape_threshold": 0.35,
        "analysis_frames": 15,
        "min_ok_frames": 9,
        "contour_weight": 0.7,
        "filled_weight": 0.25,
        "shape_weight": 0.05,
        "ok_similarity": 80.0,
        "defect_similarity": 75.0
    },
    "template_geometry": {
        "bbox": [
            407,
            354,
            122,
            216
        ],
        "center": [
            468.0,
            462.0
        ],
        "contour_area": 21062.5
    }
}

os.makedirs(TEMPLATE_DIR, exist_ok=True)
init(autoreset=True)


def cprint(color, text):
    print(f"{color}{text}{Style.RESET_ALL}")


def merge_config(base, override):
    config = json.loads(json.dumps(base))
    if not isinstance(override, dict):
        return config
    for key, value in override.items():
        if key in ("processing", "comparison", "template_geometry"):
            continue
        config[key] = value
    config["processing"].update(override.get("processing", {}))
    config["comparison"].update(override.get("comparison", {}))
    if "template_geometry" in override:
        config["template_geometry"] = override["template_geometry"]
    return config


def load_config():
    if os.path.exists(CONFIG_PATH):
        try:
            with open(CONFIG_PATH, "r", encoding="utf-8") as f:
                user_config = json.load(f)
            cprint(Fore.GREEN, f"[OK] Загружена конфигурация: {CONFIG_PATH}")
            return merge_config(DEFAULT_CONFIG, user_config)
        except Exception as e:
            cprint(Fore.YELLOW, f"[WARNING] Не удалось прочитать {CONFIG_PATH}: {e}. Использую DEFAULT_CONFIG.")
    else:
        cprint(Fore.CYAN, "[INFO] template_config.json рядом со скриптом не найден. Использую DEFAULT_CONFIG из кода.")
    return json.loads(json.dumps(DEFAULT_CONFIG))


def clamp_odd(value, minimum=3, maximum=999):
    value = int(value)
    value = max(minimum, min(value, maximum))
    if value % 2 == 0:
        value += 1
    return max(minimum, min(value, maximum))


def safe_roi(frame, roi):
    x, y, w, h = roi
    height, width = frame.shape[:2]
    x = max(0, min(int(x), width - 1))
    y = max(0, min(int(y), height - 1))
    w = max(1, min(int(w), width - x))
    h = max(1, min(int(h), height - y))
    return x, y, w, h


def convert_picamera_array_to_bgr(array):
    if array is None:
        return None
    if len(array.shape) == 2:
        return cv2.cvtColor(array, cv2.COLOR_GRAY2BGR)
    channels = array.shape[2]
    if channels == 3:
        return cv2.cvtColor(array, cv2.COLOR_RGB2BGR)
    if channels == 4:
        return cv2.cvtColor(array, cv2.COLOR_RGBA2BGR)
    return array


def init_csi_camera():
    try:
        from picamera2 import Picamera2
    except Exception as e:
        cprint(Fore.RED, f"[ERROR] Picamera2 import failed: {e}")
        return None

    configs = [
        {"size": (FRAME_WIDTH, FRAME_HEIGHT), "format": "RGB888", "buffer_count": 2},
        {"size": (1024, 576), "format": "RGB888", "buffer_count": 2},
        {"size": (640, 480), "format": "RGB888", "buffer_count": 2},
    ]

    last_error = None
    for i, cfg in enumerate(configs, start=1):
        picam2 = None
        try:
            cprint(Fore.CYAN, f"[CSI] Пробую конфигурацию #{i}: {cfg['size'][0]}x{cfg['size'][1]} {cfg['format']}")
            picam2 = Picamera2(0)
            camera_config = picam2.create_video_configuration(
                main={"size": cfg["size"], "format": cfg["format"]},
                buffer_count=cfg["buffer_count"],
                queue=False,
            )
            picam2.configure(camera_config)
            picam2.start()
            time.sleep(CAMERA_WARMUP_SEC)
            test = picam2.capture_array()
            if test is None or test.size == 0:
                raise RuntimeError("empty CSI frame")
            cprint(Fore.GREEN, f"[OK] CSI camera started: {test.shape[1]}x{test.shape[0]}")
            return picam2
        except Exception as e:
            last_error = e
            cprint(Fore.YELLOW, f"[CSI] Config #{i} failed: {e}")
            try:
                if picam2 is not None:
                    picam2.stop()
                    picam2.close()
            except Exception:
                pass
            time.sleep(0.5)

    cprint(Fore.RED, f"[ERROR] CSI camera not started. Last error: {last_error}")
    return None


def preprocess(frame, processing):
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    blur = cv2.GaussianBlur(gray, (7, 7), 0)
    blur2 = cv2.bilateralFilter(blur, 9, 75, 75)

    edges = cv2.Canny(blur2, int(processing["canny_low"]), int(processing["canny_high"]))

    adaptive_block = clamp_odd(processing["adaptive_block"], 3, 99)
    adaptive = cv2.adaptiveThreshold(
        blur2,
        255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY_INV,
        adaptive_block,
        int(processing["adaptive_c"]),
    )

    combined = cv2.bitwise_or(edges, adaptive)

    ksize = int(processing.get("morph_kernel_size", 5))
    kernel = np.ones((ksize, ksize), np.uint8)
    processed = cv2.dilate(combined, kernel, iterations=int(processing.get("morph_dilate_iter", 2)))
    processed = cv2.erode(processed, kernel, iterations=int(processing.get("morph_erode_iter", 1)))
    processed = cv2.morphologyEx(processed, cv2.MORPH_OPEN, kernel, iterations=int(processing.get("morph_open_iter", 1)))

    return processed, gray, edges, adaptive, combined


def find_main_contour(mask, min_area):
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_TC89_L1)
    if not contours:
        return None
    main_contour = max(contours, key=cv2.contourArea)
    area = cv2.contourArea(main_contour)
    if area < min_area:
        return None
    return main_contour, area


def make_filled_mask(shape, contour):
    filled = np.zeros(shape[:2], dtype=np.uint8)
    if contour is not None:
        cv2.drawContours(filled, [contour], -1, 255, thickness=cv2.FILLED)
    return filled


def contour_info(contour):
    x, y, w, h = cv2.boundingRect(contour)
    return {
        "bbox": [int(x), int(y), int(w), int(h)],
        "center": [float(x + w / 2.0), float(y + h / 2.0)],
        "contour_area": float(cv2.contourArea(contour)),
    }


def get_next_template_number():
    existing = [
        d for d in os.listdir(TEMPLATE_DIR)
        if os.path.isdir(os.path.join(TEMPLATE_DIR, d)) and d.isdigit()
    ]
    return max([int(d) for d in existing] + [0]) + 1


def draw_overlay(crop, contour, config, template_num):
    overlay = crop.copy()
    if contour is not None:
        cv2.drawContours(overlay, [contour], -1, (0, 255, 0), 2)
        x, y, w, h = cv2.boundingRect(contour)
        cv2.rectangle(overlay, (x, y), (x + w, y + h), (255, 0, 0), 2)

    p = config["processing"]
    c = config["comparison"]
    lines = [
        f"Template #{template_num}",
        f"Canny {p['canny_low']}/{p['canny_high']} | Adaptive block {p['adaptive_block']} C {p['adaptive_c']}",
        f"tol={c.get('pixel_tolerance', 20)} | OK>={c.get('ok_similarity', 80.0)}% | defect<{c.get('defect_similarity', 75.0)}%",
    ]
    for i, line in enumerate(lines):
        cv2.putText(overlay, line, (15, 30 + i * 25), cv2.FONT_HERSHEY_SIMPLEX, 0.65, (0, 220, 255), 2)
    return overlay


def save_template(frame_full, template_num, config):
    roi = config.get("roi", DEFAULT_CONFIG["roi"])
    processing = config["processing"]
    x, y, w, h = safe_roi(frame_full, roi)
    crop = frame_full[y:y + h, x:x + w]

    processed, _, edges, adaptive, combined = preprocess(crop, processing)
    result = find_main_contour(processed, int(processing.get("min_contour_area", 1500)))

    template_path = os.path.join(TEMPLATE_DIR, str(template_num))
    os.makedirs(template_path, exist_ok=True)

    cv2.imwrite(os.path.join(template_path, f"origin_front_{template_num}.jpg"), crop)
    cv2.imwrite(os.path.join(template_path, f"process_front_{template_num}.jpg"), processed)
    cv2.imwrite(os.path.join(template_path, f"edges_front_{template_num}.jpg"), edges)
    cv2.imwrite(os.path.join(template_path, f"adaptive_front_{template_num}.jpg"), adaptive)
    cv2.imwrite(os.path.join(template_path, f"combined_front_{template_num}.jpg"), combined)

    save_config = json.loads(json.dumps(config))
    save_config["template_id"] = int(template_num)
    save_config["camera"] = "CSI-front"
    save_config["roi"] = [int(x), int(y), int(w), int(h)]

    if result is None:
        cprint(Fore.YELLOW, f"[WARNING] Template #{template_num}: main contour not found. Images saved, contour/mask not saved.")
    else:
        contour, area = result
        filled = make_filled_mask(crop.shape, contour)
        cv2.imwrite(os.path.join(template_path, f"mask_front_{template_num}.jpg"), filled)
        np.save(os.path.join(template_path, f"process_front_{template_num}.npy"), contour)
        save_config["template_geometry"] = contour_info(contour)
        cprint(Fore.GREEN, f"[OK] Template #{template_num} saved. Area={area:.1f}")

    with open(os.path.join(template_path, "template_config.json"), "w", encoding="utf-8") as f:
        json.dump(save_config, f, ensure_ascii=False, indent=2)


def main():
    cprint(Fore.CYAN, f"=== {WINDOW_NAME} ===")
    config = load_config()
    cprint(Fore.CYAN, "Ползунков нет: используются фиксированные значения из template_config.json/DEFAULT_CONFIG.")
    cprint(Fore.CYAN, "Arduino в photomaker не подключается.")

    cap_csi = init_csi_camera()
    if cap_csi is None:
        cprint(Fore.RED, "[CRITICAL] CSI camera is not available")
        return

    template_num = get_next_template_number()

    try:
        while True:
            try:
                arr = cap_csi.capture_array()
                frame = convert_picamera_array_to_bgr(arr)
            except Exception as e:
                cprint(Fore.RED, f"[ERROR] CSI capture failed: {e}")
                break

            roi = config.get("roi", DEFAULT_CONFIG["roi"])
            x, y, w, h = safe_roi(frame, roi)
            crop = frame[y:y + h, x:x + w]
            processed, gray, edges, adaptive, combined = preprocess(crop, config["processing"])
            result = find_main_contour(processed, int(config["processing"].get("min_contour_area", 1500)))
            contour = result[0] if result else None
            filled = make_filled_mask(crop.shape, contour)

            full_display = frame.copy()
            cv2.rectangle(full_display, (x, y), (x + w, y + h), (0, 255, 0), 3)
            cv2.putText(full_display, "ROI", (x + 8, y + 28), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
            cv2.imshow("CSI Original", full_display)
            cv2.imshow("CSI Process mask", processed)
            cv2.imshow("CSI Filled mask", filled)
            cv2.imshow("CSI Contours", draw_overlay(crop, contour, config, template_num))

            key = cv2.waitKey(1) & 0xFF
            if key == EXIT_KEY:
                break
            if key == CAPTURE_KEY or key == 32:
                save_template(frame, template_num, config)
                template_num += 1
    finally:
        try:
            cap_csi.stop()
            cap_csi.close()
        except Exception:
            pass
        cv2.destroyAllWindows()
        cprint(Fore.CYAN, "Program finished")



# ==================== REMOTE SERVER WRAPPER ====================
class PhotomakerState:
    """Runtime state shared by the camera loop and HTTP handler."""

    def __init__(self):
        self.latest_frame = None
        self.latest_status = {"state": "starting", "template": None, "message": ""}
        self.capture_requested = threading.Event()
        self.stop_requested = threading.Event()
        self.frame_lock = threading.Lock()
        self.status_lock = threading.Lock()

    def update_status(self, **kwargs):
        with self.status_lock:
            self.latest_status.update(kwargs)

    def get_status(self):
        with self.status_lock:
            return dict(self.latest_status)

    def set_frame(self, frame):
        with self.frame_lock:
            self.latest_frame = frame

    def get_frame(self):
        with self.frame_lock:
            return self.latest_frame


state = PhotomakerState()


def set_status(**kwargs):
    state.update_status(**kwargs)


def encode_frame(frame):
    ok, buffer = cv2.imencode(".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), JPEG_QUALITY])
    return buffer.tobytes() if ok else None


class JsonResponseMixin:
    """HTTP response helpers used by module request handlers."""

    def write_json(self, payload):
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", JSON_CONTENT_TYPE)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def write_jpeg(self, frame):
        self.send_response(200)
        self.send_header("Content-Type", JPEG_CONTENT_TYPE)
        self.send_header("Cache-Control", "no-cache")
        self.end_headers()
        self.wfile.write(frame)


class PhotomakerRequestHandler(JsonResponseMixin, BaseHTTPRequestHandler):
    """HTTP handler exposing photomaker endpoints."""

    def do_GET(self):
        path = urlparse(self.path).path
        if path == ENDPOINT_STATUS:
            self.write_json(state.get_status())
            return

        if path == ENDPOINT_CAPTURE:
            state.capture_requested.set()
            self.write_json({"ok": True, "message": "capture requested"})
            return

        if path == ENDPOINT_FRAME:
            frame = state.get_frame()
            if frame is None:
                self.send_error(404, "frame is not ready")
                return
            self.write_jpeg(frame)
            return

        self.send_error(404)

    def do_POST(self):
        if urlparse(self.path).path == ENDPOINT_STOP:
            state.stop_requested.set()
            self.write_json({"ok": True})
            return
        self.send_error(404)

    def log_message(self, format, *args):
        return


class PhotomakerController:
    """Main capture loop for the photomaker module."""

    def __init__(self, runtime_state):
        self.state = runtime_state

    def run(self):
        camera_loop(self.state)


def camera_loop(runtime_state):
    config = load_config()
    cap_csi = init_csi_camera()
    if cap_csi is None:
        set_status(state="error", message="CSI camera is not available")
        return

    template_num = get_next_template_number()
    set_status(state="running", template=template_num, message="photomaker is ready")

    try:
        while not runtime_state.stop_requested.is_set():
            try:
                arr = cap_csi.capture_array()
                frame = convert_picamera_array_to_bgr(arr)
            except Exception as exc:
                set_status(state="error", message=f"CSI capture failed: {exc}")
                break

            roi = config.get("roi", DEFAULT_CONFIG["roi"])
            x, y, w, h = safe_roi(frame, roi)
            crop = frame[y:y + h, x:x + w]
            processed, _, _, _, _ = preprocess(crop, config["processing"])
            result = find_main_contour(processed, int(config["processing"].get("min_contour_area", 1500)))
            contour = result[0] if result else None

            full_display = frame.copy()
            cv2.rectangle(full_display, (x, y), (x + w, y + h), (0, 255, 0), 3)
            cv2.putText(full_display, "ROI", (x + 8, y + 28), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
            overlay = draw_overlay(crop, contour, config, template_num)
            full_display[y:y + h, x:x + w] = overlay

            encoded = encode_frame(full_display)
            if encoded is not None:
                runtime_state.set_frame(encoded)

            if runtime_state.capture_requested.is_set():
                runtime_state.capture_requested.clear()
                save_template(frame, template_num, config)
                set_status(state="running", template=template_num, message=f"template #{template_num} saved")
                template_num += 1

            time.sleep(FRAME_DELAY_SEC)
    finally:
        try:
            cap_csi.stop()
            cap_csi.close()
        except Exception:
            pass
        set_status(state="stopped", message="photomaker stopped")


def create_server(host, port):
    """Create the HTTP server for photomaker endpoints."""

    return ThreadingHTTPServer((host, port), PhotomakerRequestHandler)


def parse_args():
    """Parse command-line arguments for remote server mode."""

    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default=HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    return parser.parse_args()


def server_main():
    """Script entrypoint used by EdgeDeviceService."""

    args = parse_args()
    controller = PhotomakerController(state)

    thread = threading.Thread(target=controller.run, daemon=True)
    thread.start()

    server = create_server(args.host, args.port)
    try:
        while not state.stop_requested.is_set():
            server.handle_request()
    finally:
        server.server_close()


if __name__ == "__main__":
    server_main()
