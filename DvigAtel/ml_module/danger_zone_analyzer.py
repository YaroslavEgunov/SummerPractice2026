import os
import cv2
import numpy as np
from pathlib import Path
from ultralytics import YOLO

class DangerZoneAnalyzer:
    """Анализатор для обнаружения людей в опасной зоне с визуализацией."""
    def __init__(self, model_path="yolo11n.pt", confidence=0.2):
        self.model = YOLO(model_path)
        self.confidence = confidence

    def analyze_image(self, image_path, danger_zone, save_dir=None):
        """Проверяет наличие человека в зоне и сохраняет результат с текстом."""
        results = self.model(image_path, conf=self.confidence, verbose=False)
        img = cv2.imread(str(image_path))
        if img is None:
            return {"danger_triggered": False, "trigger_coordinates": []}
            
        h, w = img.shape[:2]
        polygon_px = np.array([(int(x * w), int(y * h)) for x, y in danger_zone], np.int32)
        
        danger_triggered = False
        trigger_points = []
        
        # Сначала ищем нарушения
        for result in results:
            for box in result.boxes:
                if int(box.cls[0]) == 0:  # person
                    x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())
                    foot_point = (int((x1 + x2) / 2), y2)
                    if cv2.pointPolygonTest(polygon_px, foot_point, False) >= 0:
                        danger_triggered = True
                        trigger_points.append(foot_point)

        # Отрисовка
        color = (0, 0, 255) if danger_triggered else (255, 0, 0)
        status_text = "DANGER DETECTED!" if danger_triggered else "ZONE CLEAR"
        
        # 1. Зона
        cv2.polylines(img, [polygon_px], True, color, 2)
        
        # 2. Текст в левом верхнем углу
        cv2.rectangle(img, (0, 0), (300, 40), (0, 0, 0), -1) # Фон для текста
        cv2.putText(img, status_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, color, 2)
        
        # 3. Рамки людей
        for result in results:
            for box in result.boxes:
                if int(box.cls[0]) == 0:
                    x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())
                    foot_point = (int((x1 + x2) / 2), y2)
                    is_inside = cv2.pointPolygonTest(polygon_px, foot_point, False) >= 0
                    p_color = (0, 0, 255) if is_inside else (0, 255, 0)
                    cv2.rectangle(img, (x1, y1), (x2, y2), p_color, 2)
                    cv2.circle(img, foot_point, 5, p_color, -1)

        if save_dir:
            save_path = Path(save_dir)
            save_path.mkdir(parents=True, exist_ok=True)
            output_file = save_path / f"proc_{Path(image_path).name}"
            cv2.imwrite(str(output_file), img)
                        
        return {
            "danger_triggered": danger_triggered,
            "trigger_coordinates": trigger_points
        }
