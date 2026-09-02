import os
import cv2
import numpy as np
from pathlib import Path
from ultralytics import YOLO

class DangerZoneAnalyzer:
    
    def __init__(self, model_path="best.pt", confidence=0.2):
        self.model = YOLO(model_path)
        self.confidence = confidence

    def analyze_image(self, image_path, danger_zones, save_dir=None):

        # Прогоняем нейросеть ОДИН раз для кадра
        results = self.model(image_path, conf=self.confidence, verbose=False)
        img = cv2.imread(str(image_path))
        if img is None:
            return {"global_danger": False, "zones_triggered": {}, "trigger_coordinates": {}}

        h, w = img.shape[:2]
        
        # Подготавливаем полигоны в пикселях и словари для результатов
        zones_px = {}
        zones_triggered = {}
        trigger_coordinates = {}
        
        for zone_name, poly in danger_zones.items():
            zones_px[zone_name] = np.array([(int(x * w), int(y * h)) for x, y in poly], np.int32)
            zones_triggered[zone_name] = False
            trigger_coordinates[zone_name] = []

        # Поиск нарушителей во всех зонах
        for result in results:
            for box in result.boxes:
                if int(box.cls[0]) == 0:  # person
                    x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())
                    foot_point = (int((x1 + x2) / 2), y2)
                    
                    for zone_name, poly_px in zones_px.items():
                        if cv2.pointPolygonTest(poly_px, foot_point, False) >= 0:
                            zones_triggered[zone_name] = True
                            trigger_coordinates[zone_name].append(foot_point)

        global_danger = any(zones_triggered.values())

        # Отрисовка всех полигонов зон
        for zone_name, poly_px in zones_px.items():
            # Красный если зона нарушена, синий — если пуста
            color = (0, 0, 255) if zones_triggered[zone_name] else (255, 0, 0)
            cv2.polylines(img, [poly_px], True, color, 2)

        # Общий статус-текст в левом верхнем углу
        status_text = "DANGER DETECTED!" if global_danger else "ZONE CLEAR"
        top_color = (0, 0, 255) if global_danger else (255, 0, 0)
        cv2.rectangle(img, (0, 0), (320, 40), (0, 0, 0), -1)  # Черная подложка
        cv2.putText(img, status_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, top_color, 2)

        # Отрисовка рамок людей с цветовой кодировкой нарушения
        for result in results:
            for box in result.boxes:
                if int(box.cls[0]) == 0:
                    x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())
                    foot_point = (int((x1 + x2) / 2), y2)
                    
                    person_inside_any_zone = False
                    for zone_name, poly_px in zones_px.items():
                        if cv2.pointPolygonTest(poly_px, foot_point, False) >= 0:
                            person_inside_any_zone = True
                            break
                            
                    p_color = (0, 0, 255) if person_inside_any_zone else (0, 255, 0)
                    cv2.rectangle(img, (x1, y1), (x2, y2), p_color, 2)
                    cv2.circle(img, foot_point, 5, p_color, -1)

        # Сохранение финальной комплексной картинки
        if save_dir:
            save_path = Path(save_dir)
            save_path.mkdir(parents=True, exist_ok=True)
            output_file = save_path / Path(image_path).name 
            cv2.imwrite(str(output_file), img)

        return {
            "danger_triggered": global_danger,       
            "zones_triggered": zones_triggered,      
            "trigger_coordinates": trigger_coordinates 
        }