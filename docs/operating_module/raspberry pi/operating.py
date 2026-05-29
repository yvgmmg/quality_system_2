import cv2
import numpy as np
import time

# ---------- Параметры ----------
MIN_CONTOUR_AREA = 3000        # минимальная площадь детали (подбирается)
STABILITY_FRAMES = 50          # сколько кадров подряд деталь видна для старта
MORPH_KERNEL = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5,5))

# ---------- Инициализация ----------
#cap = cv2.VideoCapture("test_video.mp4")
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    print("Ошибка камеры")
    exit()

# Создаём вычитатель фона
backSub = cv2.createBackgroundSubtractorMOG2(history=500, varThreshold=36, detectShadows=False)
#history - сколько кадров для обучения
#varThreshold - чувствительность. чем больще, тем меньше шума, но мелкие детали может не улавливать

# Переменные состояния
state = 'WAITING'            # WAITING / PROCESSING
stable_counter = 0           # счётчик стабильного присутствия детали
frame_processed = None       # кадр для обработки

print("Начинается инициализация фона (убедитесь, что конвейер пуст)...")
time.sleep(5)
# Дадим несколько секунд на накопление статистики, не запуская логику
for _ in range(30):
    ret, frame = cap.read()
    if ret:
        # Обновляем модель фона с автоматической скоростью обучения (-1)
        backSub.apply(frame, learningRate=-1) #-1 - автоматический подбор скорости обучения

print("Ожидание детали...")

while True:
    ret, frame = cap.read()
    if not ret:
        break

    # 1. Получаем маску переднего плана (без обновления модели)
    fg_mask = backSub.apply(frame, learningRate=0)  # <-- не учим!

    # 2. Морфологическая очистка
    fg_mask = cv2.morphologyEx(fg_mask, cv2.MORPH_OPEN, MORPH_KERNEL, iterations=2)   # убрать точечный шум
    #fg_mask = cv2.morphologyEx(fg_mask, cv2.MORPH_CLOSE, MORPH_KERNEL)  # залить дыры в детали

    # 3. Поиск контуров и оценка площади
    contours, _ = cv2.findContours(fg_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    total_area = sum(cv2.contourArea(c) for c in contours)

    # 4. Определение факта присутствия детали
    part_present = (total_area > MIN_CONTOUR_AREA)

    # 5. Конечный автомат с гистерезисом
    if state == 'WAITING':
        if part_present:
            stable_counter += 1
            if stable_counter >= STABILITY_FRAMES:
                # Деталь надёжно появилась — переходим к обработке
                state = 'PROCESSING'
                stable_counter = 0
                frame_processed = frame.copy()
                print("Деталь обнаружена! Запуск измерения/инспекции...")
                # Здесь вызовите свою функцию обработки: analyse_part(frame_processed)
                cv2.putText(frame, "PROCESSING", (10, 30),cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 255, 0), 2)
        else:
            stable_counter = 0   # сброс, если деталь пропала

    elif state == 'PROCESSING':
        # Пока деталь обрабатывается, продолжаем контролировать её уход
        if not part_present:
            stable_counter += 1
            if stable_counter >= STABILITY_FRAMES:
                state = 'WAITING'
                stable_counter = 0
                print("Деталь ушла, конвейер свободен. Обновляем фон...")
                # Можно дать команду на короткое доучивание фона пустого конвейера
                # Например, несколько кадров с learningRate=-1
                for _ in range(30):
                    ret, blank_frame = cap.read()
                    if ret:
                        backSub.apply(blank_frame, learningRate=-1)
        else:
            stable_counter = 0
        # Во время обработки можно выводить рамку или данные
        cv2.putText(frame, "Inspecting...", (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 255), 2)

    # Визуализация (опционально)
    cv2.drawContours(frame, contours, -1, (0, 255, 0), 2)
    cv2.imshow('Detection', frame)
    cv2.imshow('Foreground Mask', fg_mask)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()