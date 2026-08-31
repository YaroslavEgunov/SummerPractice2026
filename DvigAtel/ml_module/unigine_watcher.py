import os
import time
import json
from pathlib import Path
from danger_zone_analyzer import DangerZoneAnalyzer 

def get_desktop():
    if os.name == 'nt':
        import winreg
        try:
            key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders")
            return Path(winreg.QueryValueEx(key, "Desktop")[0])
        except:
            pass
    return Path.home() / "Desktop"

def main():
    desktop = get_desktop()
    records_dir = desktop / "UnigineCameraRecords"
    log_file = desktop / "log.txt"
    status_file = desktop / "ml_status.json"
    
    analyzer = DangerZoneAnalyzer()
    
    zones = {
        "zone_left_foreground":  [(0.00, 0.51), (0.36, 0.51), (0.40, 1.00), (0.00, 1.00)],
        "zone_left_background":  [(0.37, 0.31), (0.44, 0.31), (0.44, 0.41), (0.38, 0.41)],
        "zone_right_background": [(0.56, 0.31), (0.63, 0.31), (0.62, 0.41), (0.56, 0.41)],
        "zone_center_gates":     [(0.45, 0.27), (0.54, 0.27), (0.54, 0.30), (0.45, 0.30)]
    }
    
    processed = set()
    # Словарь для отслеживания состояния каждой зоны отдельно
    zones_state = {zone_name: False for zone_name in zones}
    
    print(f"[*] Monitoring: {records_dir}")
    
    try:
        while True:
            if not records_dir.exists():
                time.sleep(1)
                continue
                
            sessions = sorted([d for d in records_dir.iterdir() if d.is_dir()])
            if not sessions:
                time.sleep(1)
                continue
                
            cur_session = sessions[-1]
            analyzed_dir = cur_session / "analyzed"
            analyzed_dir.mkdir(parents=True, exist_ok=True)
            
            for frame in sorted(list(cur_session.glob("frame_*.jpg"))):
                if frame in processed:
                    continue
                    
                time.sleep(0.05)
                
                # КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Передаем весь словарь `zones`, а не один `poly`
                res = analyzer.analyze_image(frame, zones, save_dir=analyzed_dir)
                processed.add(frame)
                
                # Извлекаем комплексный результат
                is_danger = res["danger_triggered"]          # Глобальный флаг
                frame_danger_status = res["zones_triggered"] # Словарь со статусами зон
                
                # Логика входа/выхода для каждой зоны по отдельности
                for zone_name, zone_is_danger in frame_danger_status.items():
                    was_danger = zones_state[zone_name]
                    
                    if zone_is_danger and not was_danger:
                        ts = time.strftime("%d.%m.%Y %H:%M:%S")
                        msg = f"[{ts}] [ML EVENT] Person ENTERED {zone_name}\n"
                        with open(log_file, "a", encoding="utf-8") as f:
                            f.write(msg)
                        print(f">>> 🔥 PERSON ENTERED: {zone_name}")
                        
                    elif not zone_is_danger and was_danger:
                        ts = time.strftime("%d.%m.%Y %H:%M:%S")
                        msg = f"[{ts}] [ML EVENT] Zone {zone_name} CLEARED\n"
                        with open(log_file, "a", encoding="utf-8") as f:
                            f.write(msg)
                        print(f">>> ✅ ZONE CLEARED: {zone_name}")
                        
                    zones_state[zone_name] = zone_is_danger
                
                # Запись общего и детального статуса в json для Unigine
                with open(status_file, "w") as f:
                    json.dump({
                        "is_danger": is_danger, 
                        "zones": frame_danger_status,
                        "time": time.time()
                    }, f)
                    
                print(f"[*] Processed {frame.name} (Global Danger: {is_danger})")
                time.sleep(0.1)
                
    except KeyboardInterrupt:
        print("\n[*] Stopped.")

if __name__ == "__main__":
    main()
