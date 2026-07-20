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
        except: pass
    return Path.home() / "Desktop"

def main():
    desktop = get_desktop()
    records_dir = desktop / "UnigineCameraRecords"
    log_file = desktop / "log.txt"
    status_file = desktop / "ml_status.json"
    
    analyzer = DangerZoneAnalyzer()
    danger_poly = [(0.2, 0.5), (0.8, 0.5), (0.95, 0.95), (0.05, 0.95)]
    processed = set()
    
    # Для контроля входа/выхода
    was_danger = False

    print(f"[*] Monitoring: {records_dir}")
    
    try:
        while True:
            sessions = sorted([d for d in records_dir.iterdir() if d.is_dir()])
            if not sessions:
                time.sleep(1)
                continue
            
            cur_session = sessions[-1]
            analyzed_dir = cur_session / "analyzed"
            
            for frame in sorted(list(cur_session.glob("frame_*.jpg"))):
                if frame not in processed:
                    time.sleep(0.05)
                    res = analyzer.analyze_image(frame, danger_poly, save_dir=analyzed_dir)
                    processed.add(frame)
                    
                    is_danger = res["danger_triggered"]
                    
                    # Логика входа/выхода (одна запись на событие)
                    if is_danger and not was_danger:
                        ts = time.strftime("%d.%m.%Y %H:%M:%S")
                        msg = f"[{ts}] [ML EVENT] Person ENTERED danger zone\n"
                        with open(log_file, "a", encoding="utf-8") as f: f.write(msg)
                        print(">>> 🔥 PERSON ENTERED")
                    elif not is_danger and was_danger:
                        ts = time.strftime("%d.%m.%Y %H:%M:%S")
                        msg = f"[{ts}] [ML EVENT] Danger zone CLEARED\n"
                        with open(log_file, "a", encoding="utf-8") as f: f.write(msg)
                        print(">>> ✅ ZONE CLEARED")
                    
                    was_danger = is_danger

                    # Статус для Unigine
                    with open(status_file, "w") as f:
                        json.dump({"is_danger": is_danger, "time": time.time()}, f)
                    
                    print(f"[*] Processed {frame.name} (Danger: {is_danger})")
            
            time.sleep(0.1)
    except KeyboardInterrupt:
        print("\n[*] Stopped.")

if __name__ == "__main__":
    main()
