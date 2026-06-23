import cv2
import numpy as np
import os
import json
import time
import argparse
import logging
import math
import re
import glob
import threading
from collections import deque, Counter
from colorama import init, Fore, Style
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

try:
    import serial
except Exception:
    serial = None

init(autoreset=True)
logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")

# ==================== CONFIG ====================
TEMPLATE_DIR = "template"
CONFIG_PATH = "template_config.json"
WINDOW_NAME = "CSI Defect Detector v5 + Arduino auto-port sensors"
EXIT_KEY = ord("q")
HOST = "0.0.0.0"
DEFAULT_PORT = 8082
ENDPOINT_STATUS = "/status"
ENDPOINT_RESULTS = "/results"
ENDPOINT_FRAME = "/frame.jpg"
ENDPOINT_STOP = "/stop"
JSON_CONTENT_TYPE = "application/json; charset=utf-8"
JPEG_CONTENT_TYPE = "image/jpeg"
JPEG_QUALITY = 82
FRAME_DELAY_SEC = 0.03

FRAME_WIDTH = 1280
FRAME_HEIGHT = 720
CAMERA_WARMUP_SEC = 1.0

# Время на установку детали. В течение этого времени анализ не выполняется.
PLACEMENT_DELAY_SEC = 12.0

# Arduino из measure.ino: Serial.begin(9600)
# None = порт определяется автоматически: /dev/ttyACM*, /dev/ttyUSB*, /dev/serial/by-id/*
ARDUINO_PORT = None
ARDUINO_BAUDRATE = 9600

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


def load_json(path, default=None):
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return default


def load_base_config():
    user_config = load_json(CONFIG_PATH, None)
    if user_config:
        cprint(Fore.GREEN, f"[OK] Загружена общая конфигурация: {CONFIG_PATH}")
        return merge_config(DEFAULT_CONFIG, user_config)
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


# ================================================================
#  ARDUINO
# ================================================================

class ArduinoSensors:
    """
    Читает формат из measure.ino:
      Вес: <value>
      Температура: <value>
    """

    def __init__(self, port=ARDUINO_PORT, baudrate=ARDUINO_BAUDRATE):
        self.port = port
        self.baudrate = baudrate
        self.ser = None
        self.values = {
            "weight": None,
            "temperature_c": None,
            "light_adc": None,
            "light_percent": None,
            "updated_at": None,
        }

    @staticmethod
    def _existing_device(path):
        return path and os.path.exists(path)

    @staticmethod
    def _unique(seq):
        result = []
        seen = set()
        for item in seq:
            if not item or item in seen:
                continue
            seen.add(item)
            result.append(item)
        return result

    @staticmethod
    def candidate_ports():
        """
        Ищет реальные последовательные порты Arduino/USB-UART.
        На разных платах порт может быть /dev/ttyACM0, /dev/ttyUSB0
        или стабильная ссылка /dev/serial/by-id/*.
        """
        candidates = []

        # Самый стабильный вариант в Linux — ссылка с именем устройства.
        candidates.extend(sorted(glob.glob("/dev/serial/by-id/*")))

        # Частые имена Arduino Uno/Mega/Nano Every: ttyACM*.
        candidates.extend(sorted(glob.glob("/dev/ttyACM*")))

        # Частые имена Nano/клонов через CH340/CP210x/FTDI: ttyUSB*.
        candidates.extend(sorted(glob.glob("/dev/ttyUSB*")))

        # Если pyserial умеет list_ports, добавляем найденные им устройства.
        try:
            from serial.tools import list_ports
            ports = list(list_ports.comports())
            # Сначала устройства, похожие на Arduino/USB Serial.
            priority_words = ("arduino", "ch340", "cp210", "ftdi", "usb", "acm", "serial")
            scored = []
            for port in ports:
                text = f"{port.device} {port.description} {port.manufacturer}".lower()
                score = 0 if any(w in text for w in priority_words) else 1
                scored.append((score, port.device))
            for _, dev in sorted(scored):
                candidates.append(dev)
        except Exception:
            pass

        candidates = [c for c in candidates if os.path.exists(c)]
        return ArduinoSensors._unique(candidates)

    def connect(self):
        if serial is None:
            cprint(Fore.YELLOW, "[Arduino] pyserial не установлен. Датчики недоступны. Установите: pip3 install pyserial")
            return False

        ports_to_try = []
        env_port = os.environ.get("ARDUINO_PORT")
        if env_port:
            ports_to_try.append(env_port)
        if self.port:
            ports_to_try.append(self.port)
        ports_to_try.extend(self.candidate_ports())
        ports_to_try = self._unique(ports_to_try)

        if not ports_to_try:
            cprint(Fore.YELLOW, "[Arduino] Последовательные USB-порты не найдены. Проверьте: ls /dev/ttyACM* /dev/ttyUSB* /dev/serial/by-id/*")
            return False

        errors = []
        for port in ports_to_try:
            try:
                cprint(Fore.CYAN, f"[Arduino] Пробую порт {port}...")
                self.ser = serial.Serial(port, self.baudrate, timeout=0.1)
                time.sleep(2)
                self.ser.reset_input_buffer()
                self.port = port
                cprint(Fore.GREEN, f"[OK] Arduino подключён: {self.port}, {self.baudrate} бод")
                return True
            except Exception as e:
                errors.append(f"{port}: {e}")
                try:
                    if self.ser is not None:
                        self.ser.close()
                except Exception:
                    pass
                self.ser = None

        cprint(Fore.YELLOW, "[Arduino] Не удалось подключиться ни к одному порту:")
        for err in errors:
            cprint(Fore.YELLOW, f"  - {err}")
        cprint(Fore.YELLOW, "[HINT] Проверьте права: sudo usermod -a -G dialout $USER, затем перелогиньтесь.")
        return False

    def close(self):
        try:
            if self.ser is not None:
                self.ser.close()
        except Exception:
            pass

    def discard_when_inactive(self):
        """Вне проверки старые строки от Arduino не используются."""
        try:
            if self.ser is not None:
                self.ser.reset_input_buffer()
        except Exception:
            pass

    @staticmethod
    def _parse_number(text):
        m = re.search(r"[-+]?\d+(?:[\.,]\d+)?", text)
        if not m:
            return None
        return float(m.group(0).replace(",", "."))

    @staticmethod
    def _matches_label(text, labels):
        return any(label in text for label in labels)

    @staticmethod
    def _decode_serial_line(raw):
        try:
            return raw.decode("utf-8").strip()
        except UnicodeDecodeError:
            return raw.decode("cp1251", errors="replace").strip()

    def read_active(self):
        """
        Вызывать только во время активной проверки и когда деталь присутствует в кадре.
        Возвращает последние интерпретированные значения.
        """
        if self.ser is None:
            return self.values

        try:
            deadline = time.time() + 0.3
            while time.time() < deadline or self.ser.in_waiting:
                line = self._decode_serial_line(self.ser.readline())
                if not line:
                    continue
                low = line.lower()
                value = self._parse_number(line)
                if value is None:
                    continue

                if self._matches_label(low, ("вес", "weight", "ves", "scale", "р’рµсѓ", "рІрµсЃ", "р’рµсЃ")):
                    self.values["weight"] = value
                elif self._matches_label(low, (
                    "температура",
                    "темп",
                    "temperature",
                    "temp",
                    "thermo",
                    "celsius",
                    "°c",
                    " t:",
                    "t=",
                    "рўрµрјрїрµсђр°с‚сѓсђр°",
                    "рўрµрјрїрµсЂр°с‚ур°",
                    "рўрµрјрїрµсЂр°с‚сѓсЂр°",
                )):
                    self.values["temperature_c"] = value
                elif self._matches_label(low, ("свет", "light", "рЎрІрµс‚", "сѓрірµс‚")):
                    adc = max(0.0, min(1023.0, value))
                    self.values["light_adc"] = adc
                    self.values["light_percent"] = adc / 1023.0 * 100.0
                self.values["updated_at"] = time.time()
        except Exception as e:
            cprint(Fore.RED, f"[Arduino ERROR] {e}")

        return self.values

    def format_lines(self, active=False):
        if self.ser is None:
            return ["Arduino: not connected"]

        if not active and self.values["updated_at"] is None:
            return ["Sensors: waiting for check"]

        w = "—" if self.values["weight"] is None else f"{self.values['weight']:.0f} g"
        return [f"Weight: {w}"]


# ================================================================
#  IMAGE PROCESSING
# ================================================================

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


def contour_center(contour):
    x, y, w, h = cv2.boundingRect(contour)
    return float(x + w / 2.0), float(y + h / 2.0)


def shift_mask(mask, dx, dy):
    h, w = mask.shape[:2]
    m = np.float32([[1, 0, dx], [0, 1, dy]])
    return cv2.warpAffine(mask, m, (w, h), flags=cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT, borderValue=0)


def normalize_contour(contour, target_size=256):
    x, y, w, h = cv2.boundingRect(contour)
    if w == 0 or h == 0:
        return contour.astype(np.float32)
    pts = contour.astype(np.float32).copy()
    pts[:, 0, 0] -= x
    pts[:, 0, 1] -= y
    scale = target_size / float(max(w, h))
    pts *= scale
    return pts.astype(np.float32)


def tolerant_diff(template_mask, current_mask, pixel_tolerance):
    if current_mask.shape != template_mask.shape:
        current_mask = cv2.resize(current_mask, (template_mask.shape[1], template_mask.shape[0]), interpolation=cv2.INTER_NEAREST)

    pixel_tolerance = int(pixel_tolerance)
    if pixel_tolerance > 0:
        ksize = pixel_tolerance * 2 + 1
        kernel = np.ones((ksize, ksize), np.uint8)
        template_tolerant = cv2.dilate(template_mask, kernel, iterations=1)
        current_tolerant = cv2.dilate(current_mask, kernel, iterations=1)
    else:
        template_tolerant = template_mask
        current_tolerant = current_mask

    current_extra = cv2.bitwise_and(current_mask, cv2.bitwise_not(template_tolerant))
    template_missing = cv2.bitwise_and(template_mask, cv2.bitwise_not(current_tolerant))
    diff = cv2.bitwise_or(current_extra, template_missing)

    reference_area = max(cv2.countNonZero(template_mask), 1)
    score = cv2.countNonZero(diff) / float(reference_area)
    return min(float(score), 1.0), diff


def get_status(similarity, comparison):
    ok_thr = float(comparison.get("ok_similarity", 95.0))
    defect_thr = float(comparison.get("defect_similarity", 85.0))
    if similarity >= ok_thr:
        return "OK", (0, 255, 0), Fore.GREEN
    if similarity >= defect_thr:
        return "Warning", (0, 220, 255), Fore.YELLOW
    return "Critical", (0, 0, 255), Fore.RED


class Template:
    def __init__(self, template_id, folder_path, contour, filled_mask, process_mask, processing, comparison, geometry, roi):
        self.template_id = template_id
        self.folder_path = folder_path
        self.contour = contour
        self.filled_mask = filled_mask
        self.process_mask = process_mask
        self.processing = processing
        self.comparison = comparison
        self.geometry = geometry or {}
        self.roi = roi
        if self.geometry and "center" in self.geometry:
            self.center = (float(self.geometry["center"][0]), float(self.geometry["center"][1]))
        else:
            self.center = contour_center(contour)


def load_templates(base_config):
    templates = []
    if not os.path.exists(TEMPLATE_DIR):
        cprint(Fore.RED, f"[ERROR] Template directory '{TEMPLATE_DIR}' not found")
        return templates

    for folder in sorted(os.listdir(TEMPLATE_DIR), key=lambda x: int(x) if x.isdigit() else 10**9):
        if not folder.isdigit():
            continue
        template_id = int(folder)
        folder_path = os.path.join(TEMPLATE_DIR, folder)
        if not os.path.isdir(folder_path):
            continue

        config_path = os.path.join(folder_path, "template_config.json")
        config = merge_config(base_config, load_json(config_path, {}))
        processing = config["processing"]
        comparison = config["comparison"]
        geometry = config.get("template_geometry", {})
        roi = config.get("roi", base_config.get("roi", DEFAULT_CONFIG["roi"]))

        contour_path = os.path.join(folder_path, f"process_front_{template_id}.npy")
        filled_path = os.path.join(folder_path, f"mask_front_{template_id}.jpg")
        process_path = os.path.join(folder_path, f"process_front_{template_id}.jpg")

        if not os.path.exists(contour_path):
            logging.warning("Contour file not found: %s", contour_path)
            continue
        try:
            contour = np.load(contour_path, allow_pickle=True)
        except Exception as e:
            logging.error("Cannot load contour %s: %s", contour_path, e)
            continue

        filled_mask = cv2.imread(filled_path, cv2.IMREAD_GRAYSCALE)
        if filled_mask is None:
            w, h = int(roi[2]), int(roi[3])
            filled_mask = make_filled_mask((h, w, 3), contour)
            logging.warning("Filled mask not found: %s. Rebuilt from contour.", filled_path)
        _, filled_mask = cv2.threshold(filled_mask, 127, 255, cv2.THRESH_BINARY)

        process_mask = cv2.imread(process_path, cv2.IMREAD_GRAYSCALE)
        if process_mask is None:
            process_mask = np.zeros_like(filled_mask)
            cv2.drawContours(process_mask, [contour], -1, 255, 2)
            logging.warning("Process mask not found: %s. Rebuilt from contour line.", process_path)
        _, process_mask = cv2.threshold(process_mask, 127, 255, cv2.THRESH_BINARY)

        templates.append(Template(template_id, folder_path, contour, filled_mask, process_mask, processing, comparison, geometry, roi))
        cprint(Fore.GREEN, f"[OK] Template #{template_id} loaded: OK>={comparison.get('ok_similarity', 80.0)}%, defect<{comparison.get('defect_similarity', 75.0)}%")

    return templates


def evaluate_against_template(roi_frame, template):
    processed, gray, edges, adaptive, combined = preprocess(roi_frame, template.processing)
    result = find_main_contour(processed, int(template.processing.get("min_contour_area", 1500)))
    if result is None:
        return {
            "template": template,
            "similarity": 0.0,
            "status": "Critical",
            "reason": "NO_CONTOUR",
            "filled_score": 1.0,
            "contour_score": 1.0,
            "shape_score": float("inf"),
            "processed": processed,
            "current_filled": np.zeros_like(template.filled_mask),
            "aligned_filled": np.zeros_like(template.filled_mask),
            "aligned_process": np.zeros_like(template.process_mask),
            "diff": template.process_mask.copy(),
            "contour_diff": template.process_mask.copy(),
            "filled_diff": template.filled_mask.copy(),
            "contour": None,
            "shift": (0, 0),
        }

    current_contour, _ = result
    current_filled = make_filled_mask(roi_frame.shape, current_contour)

    cx, cy = contour_center(current_contour)
    tx, ty = template.center
    dx = int(round(tx - cx))
    dy = int(round(ty - cy))

    aligned_filled = shift_mask(current_filled, dx, dy)
    aligned_process = shift_mask(processed, dx, dy)

    pixel_tol = int(template.comparison.get("pixel_tolerance", 20))
    contour_tol = max(1, pixel_tol // 2)

    filled_score, filled_diff = tolerant_diff(template.filled_mask, aligned_filled, pixel_tol)
    contour_score, contour_diff = tolerant_diff(template.process_mask, aligned_process, contour_tol)

    shape_score = cv2.matchShapes(
        normalize_contour(template.contour),
        normalize_contour(current_contour),
        cv2.CONTOURS_MATCH_I1,
        0.0,
    )

    shape_threshold = max(float(template.comparison.get("shape_threshold", 0.35)), 1e-6)
    shape_norm = min(float(shape_score) / shape_threshold, 1.0)

    contour_w = float(template.comparison.get("contour_weight", 0.70))
    filled_w = float(template.comparison.get("filled_weight", 0.25))
    shape_w = float(template.comparison.get("shape_weight", 0.05))
    w_sum = max(contour_w + filled_w + shape_w, 1e-6)

    total_diff = (contour_w * contour_score + filled_w * filled_score + shape_w * shape_norm) / w_sum
    similarity = max(0.0, min(100.0, (1.0 - total_diff) * 100.0))

    status, _, _ = get_status(similarity, template.comparison)
    if status == "OK":
        reason = "OK"
    elif contour_score >= filled_score and contour_score >= shape_norm:
        reason = "CONTOUR_DIFF"
    elif filled_score >= contour_score and filled_score >= shape_norm:
        reason = "SILHOUETTE_DIFF"
    else:
        reason = "SHAPE_DIFF"

    diff = cv2.bitwise_or(contour_diff, filled_diff)

    return {
        "template": template,
        "similarity": float(similarity),
        "status": status,
        "reason": reason,
        "filled_score": float(filled_score),
        "contour_score": float(contour_score),
        "shape_score": float(shape_score),
        "processed": processed,
        "current_filled": current_filled,
        "aligned_filled": aligned_filled,
        "aligned_process": aligned_process,
        "diff": diff,
        "contour_diff": contour_diff,
        "filled_diff": filled_diff,
        "contour": current_contour,
        "shift": (dx, dy),
    }


def choose_best_result(results):
    return max(results, key=lambda r: r["similarity"])


def detect_part_present(roi_frame, processing):
    processed, _, _, _, _ = preprocess(roi_frame, processing)
    result = find_main_contour(processed, int(processing.get("min_contour_area", 1500)))
    return result is not None, processed, result[0] if result else None


def summarize_history(history, comparison):
    if not history:
        return None
    similarities = [x["similarity"] for x in history]
    median_similarity = float(np.median(similarities))
    status, status_color, console_color = get_status(median_similarity, comparison)

    best_template_ids = [x["template_id"] for x in history]
    best_template_id = Counter(best_template_ids).most_common(1)[0][0]

    return {
        "status": status,
        "status_color": status_color,
        "console_color": console_color,
        "median_similarity": median_similarity,
        "median_contour": float(np.median([x["contour_score"] for x in history])),
        "median_filled": float(np.median([x["filled_score"] for x in history])),
        "median_shape": float(np.median([x["shape_score"] for x in history if np.isfinite(x["shape_score"])] or [999.0])),
        "template_id": best_template_id,
        "reason": Counter([x["reason"] for x in history]).most_common(1)[0][0],
    }


def put_lines(img, lines, x=15, y=30, color=(255, 255, 255), scale=0.66, dy=25, thickness=2):
    for i, line in enumerate(lines):
        cv2.putText(img, str(line), (x, y + i * dy), cv2.FONT_HERSHEY_SIMPLEX, scale, color, thickness)


def build_main_display(frame, roi_rect, roi_frame, state, part_present, countdown, current_contour, current_result, final_summary, sensors, active_sensors):
    x, y, w, h = roi_rect
    full = frame.copy()
    cv2.rectangle(full, (x, y), (x + w, y + h), (0, 255, 0) if part_present else (0, 165, 255), 3)
    cv2.putText(full, "ROI", (x + 8, y + 28), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)

    display_roi = roi_frame.copy()

    # На основное окно НЕ накладывается маска шаблона/карта отличий.
    # Показываем только текущий найденный контур и текстовые метрики.
    draw_color = (255, 255, 255)
    if final_summary is not None:
        draw_color = final_summary["status_color"]
    elif current_result is not None:
        _, draw_color, _ = get_status(current_result["similarity"], current_result["template"].comparison)

    if current_contour is not None:
        cv2.drawContours(display_roi, [current_contour], -1, draw_color, 2)

    lines = []
    if state == "WAITING":
        lines = ["State: WAITING", "Put detail into ROI"]
    elif state == "PLACEMENT_WAIT":
        lines = [f"State: POSITIONING  {countdown:.1f}s", "Place detail and remove hand"]
    elif state == "ANALYZING":
        if current_result is None:
            lines = ["State: ANALYZING", "No result yet"]
        else:
            lines = [
                "State: ANALYZING",
                f"Similarity: {current_result['similarity']:.1f}%",
                f"Contour diff: {current_result['contour_score']:.3f}  Filled diff: {current_result['filled_score']:.3f}",
                f"Reason: {current_result['reason']}",
            ]
    elif state == "HOLD_RESULT" and final_summary is not None:
        lines = [
            f"RESULT: {final_summary['status']}  template #{final_summary['template_id']}",
            f"Similarity: {final_summary['median_similarity']:.1f}%",
            f"Contour diff: {final_summary['median_contour']:.3f}  Filled diff: {final_summary['median_filled']:.3f}",
            f"Shape: {final_summary['median_shape']:.3f}  Reason: {final_summary['reason']}",
        ]

    text_color = draw_color if state in ("ANALYZING", "HOLD_RESULT") else (0, 220, 255)
    put_lines(display_roi, lines, 15, 32, text_color)

    sensor_lines = sensors.format_lines(active=active_sensors or final_summary is not None)
    put_lines(display_roi, sensor_lines, 15, display_roi.shape[0] - 78, (255, 255, 255), scale=0.62, dy=24)

    full[y:y + h, x:x + w] = display_roi
    return full


def main():
    cprint(Fore.CYAN, f"=== {WINDOW_NAME} ===")
    base_config = load_base_config()

    templates = load_templates(base_config)
    if not templates:
        cprint(Fore.RED, "[CRITICAL] No templates loaded. Create templates with photomaker_rasp_template_v4_default_config.py first.")
        return

    cap_csi = init_csi_camera()
    if cap_csi is None:
        cprint(Fore.RED, "[CRITICAL] CSI camera is not available")
        return

    sensors = ArduinoSensors()
    sensors.connect()

    state = "WAITING"
    placement_started_at = None
    history = []
    final_summary = None
    final_printed = False

    analysis_frames = max(int(t.comparison.get("analysis_frames", 15)) for t in templates)
    roi_cfg = templates[0].roi if templates else base_config.get("roi", DEFAULT_CONFIG["roi"])

    cprint(Fore.GREEN, f"System started. Placement delay: {PLACEMENT_DELAY_SEC} sec. Press Q to exit.")

    try:
        while True:
            try:
                arr = cap_csi.capture_array()
                frame = convert_picamera_array_to_bgr(arr)
            except Exception as e:
                logging.error("CSI capture failed: %s", e)
                break

            x, y, w, h = safe_roi(frame, roi_cfg)
            roi = frame[y:y + h, x:x + w]

            part_present, presence_mask, presence_contour = detect_part_present(roi, templates[0].processing)
            current_result = None
            current_contour = presence_contour
            countdown = 0.0
            active_sensor_read = False

            if state == "WAITING":
                sensors.discard_when_inactive()
                final_summary = None
                final_printed = False
                history = []
                if part_present:
                    placement_started_at = time.time()
                    state = "PLACEMENT_WAIT"
                    cprint(Fore.CYAN, f"[STATE] Деталь обнаружена. Жду {PLACEMENT_DELAY_SEC} секунд перед проверкой.")

            elif state == "PLACEMENT_WAIT":
                sensors.discard_when_inactive()
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                    cprint(Fore.YELLOW, "[STATE] Деталь убрана до начала проверки. Возврат в ожидание.")
                else:
                    elapsed = time.time() - (placement_started_at or time.time())
                    countdown = max(0.0, PLACEMENT_DELAY_SEC - elapsed)
                    if countdown <= 0:
                        state = "ANALYZING"
                        history = []
                        cprint(Fore.GREEN, "[STATE] Запуск проверки. Значения Arduino считываются только сейчас.")

            elif state == "ANALYZING":
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                    history = []
                    final_summary = None
                    sensors.discard_when_inactive()
                    cprint(Fore.YELLOW, "[STATE] Деталь убрана во время проверки. Проверка отменена.")
                else:
                    active_sensor_read = True
                    sensors.read_active()

                    results = [evaluate_against_template(roi, t) for t in templates]
                    current_result = choose_best_result(results)
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
                        final_summary = summarize_history(history, current_result["template"].comparison)
                        state = "HOLD_RESULT"

            elif state == "HOLD_RESULT":
                # Новые значения датчиков после завершения проверки не читаем.
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                    history = []
                    final_summary = None
                    final_printed = False
                    sensors.discard_when_inactive()
                    cprint(Fore.BLUE, "[STATE] Деталь убрана. Готов к следующей проверке.")

            full_display = build_main_display(
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

            cv2.imshow("CSI Defect Detector", full_display)
            cv2.imshow("Presence / process mask", presence_mask)

            # Debug-окна с отличиями оставлены отдельно, но не накладываются на основной видеопоток.
            if current_result is not None:
                cv2.imshow("Contour diff", current_result["contour_diff"])
                cv2.imshow("Filled diff", current_result["filled_diff"])

            if state == "HOLD_RESULT" and final_summary is not None and not final_printed:
                final_printed = True
                cprint(
                    final_summary["console_color"],
                    f"[RESULT] {final_summary['status']}: similarity={final_summary['median_similarity']:.1f}%, "
                    f"template #{final_summary['template_id']}, reason={final_summary['reason']}",
                )
                sensor_line = " | ".join(sensors.format_lines(active=True))
                cprint(Fore.WHITE, f"[SENSORS] {sensor_line}")

            key = cv2.waitKey(1) & 0xFF
            if key == EXIT_KEY:
                break
    finally:
        try:
            cap_csi.stop()
            cap_csi.close()
        except Exception:
            pass
        sensors.close()
        cv2.destroyAllWindows()
        cprint(Fore.CYAN, "System stopped")



# ==================== REMOTE SERVER WRAPPER ====================
class OperatingState:
    """Runtime state shared by the inspection loop and HTTP handler."""

    def __init__(self):
        self.latest_frame = None
        self.latest_status = {"state": "starting", "message": ""}
        self.results = []
        self.stop_requested = threading.Event()
        self.frame_lock = threading.Lock()
        self.status_lock = threading.Lock()
        self.results_lock = threading.Lock()

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

    def get_results(self):
        with self.results_lock:
            return list(self.results)

    def append_result(self, item):
        with self.results_lock:
            item["id"] = len(self.results) + 1
            self.results.append(item)
            del self.results[:-100]


state = OperatingState()


def set_status(**kwargs):
    state.update_status(**kwargs)


def clean_number(value):
    if value is None:
        return None
    try:
        number = float(value)
        return None if math.isnan(number) or math.isinf(number) else number
    except Exception:
        return None


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


class OperatingRequestHandler(JsonResponseMixin, BaseHTTPRequestHandler):
    """HTTP handler exposing operating inspection endpoints."""

    def do_GET(self):
        path = urlparse(self.path).path
        if path == ENDPOINT_STATUS:
            self.write_json(state.get_status())
            return

        if path == ENDPOINT_RESULTS:
            self.write_json(state.get_results())
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


def append_result(summary, sensors, runtime_state):
    sensor_values = dict(getattr(sensors, "values", {}) or {})
    item = {
        "recordedAt": time.strftime("%Y-%m-%dT%H:%M:%S", time.localtime()),
        "status": summary.get("status"),
        "templateId": int(summary.get("template_id", 0)),
        "similarity": clean_number(summary.get("median_similarity")),
        "contourScore": clean_number(summary.get("median_contour")),
        "filledScore": clean_number(summary.get("median_filled")),
        "shapeScore": clean_number(summary.get("median_shape")),
        "reason": summary.get("reason"),
        "weight": clean_number(sensor_values.get("weight")),
        "temperatureC": clean_number(sensor_values.get("temperature_c")),
        "lightPercent": clean_number(sensor_values.get("light_percent")),
        "lightAdc": clean_number(sensor_values.get("light_adc")),
    }
    runtime_state.append_result(item)


class OperatingController:
    """Main inspection loop for the operating module."""

    def __init__(self, runtime_state):
        self.state = runtime_state

    def run(self):
        camera_loop(self.state)


def camera_loop(runtime_state):
    base_config = load_base_config()
    templates = load_templates(base_config)
    if not templates:
        set_status(state="error", message="templates were not found")
        return

    cap_csi = init_csi_camera()
    if cap_csi is None:
        set_status(state="error", message="CSI camera is not available")
        return

    sensors = ArduinoSensors()
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
        while not runtime_state.stop_requested.is_set():
            try:
                arr = cap_csi.capture_array()
                frame = convert_picamera_array_to_bgr(arr)
            except Exception as exc:
                set_status(state="error", message=f"CSI capture failed: {exc}")
                break

            x, y, w, h = safe_roi(frame, roi_cfg)
            roi = frame[y:y + h, x:x + w]
            part_present, presence_mask, presence_contour = detect_part_present(roi, templates[0].processing)
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
                    countdown = max(0.0, PLACEMENT_DELAY_SEC - elapsed)
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
                    checks = [evaluate_against_template(roi, t) for t in templates]
                    current_result = choose_best_result(checks)
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
                        final_summary = summarize_history(history, current_result["template"].comparison)
                        state = "HOLD_RESULT"

            elif state == "HOLD_RESULT":
                if final_summary is not None and not final_recorded:
                    append_result(final_summary, sensors, runtime_state)
                    final_recorded = True
                if not part_present:
                    state = "WAITING"
                    placement_started_at = None
                    history = []
                    final_summary = None
                    final_recorded = False
                    sensors.discard_when_inactive()

            full_display = build_main_display(
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
                runtime_state.set_frame(encoded)

            set_status(state=state, message="operating is running")
            time.sleep(FRAME_DELAY_SEC)
    finally:
        try:
            cap_csi.stop()
            cap_csi.close()
        except Exception:
            pass
        sensors.close()
        set_status(state="stopped", message="operating stopped")


def create_server(host, port):
    """Create the HTTP server for operating endpoints."""

    return ThreadingHTTPServer((host, port), OperatingRequestHandler)


def parse_args():
    """Parse command-line arguments for remote server mode."""

    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default=HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    return parser.parse_args()


def server_main():
    """Script entrypoint used by EdgeDeviceService."""

    args = parse_args()
    controller = OperatingController(state)

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
