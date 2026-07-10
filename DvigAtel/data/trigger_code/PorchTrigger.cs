using System;
using System.IO;
using Unigine;
using Path = System.IO.Path;
using File = System.IO.File;

namespace UnigineApp.data.trigger_code
{
    public class PorchTrigger : Component
    {
        private WorldTrigger trigger;
        private string logFilePath;

        void Init()
        {
            trigger = node as WorldTrigger;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            logFilePath = Path.Combine(desktopPath, "log.txt");

            if (trigger != null)
            {
                Log.Message("[PorchTrigger] WorldTrigger успешно инициализирован.\n");
                trigger.EventEnter.Connect(OnPlayerEnter);
            }
            else
            {
                Log.Error($"[PorchTrigger] Ошибка: Текущий узел '{node.Name}' не является WorldTrigger!\n");
            }
        }

        private void OnPlayerEnter(Node enterNode)
        {
            // Выводим в консоль Unigine имя любого объекта, который пересек триггер
            Log.Message($"[PorchTrigger] Объект '{enterNode.Name}' вошел в триггер.\n");

            if (enterNode.Name.Contains("first_person"))
            {
                string logMessage = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] Кто-то зашёл в красную зону (Объект: {enterNode.Name})";

                try
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                    Log.Message("[PorchTrigger] Файл успешно записан/создан!\n");
                }
                catch (Exception ex)
                {
                    Log.Error($"[PorchTrigger] Не удалось записать файл: {ex.Message}\n");
                }
            }
            else
            {
                Log.Message($"[PorchTrigger] Объект '{enterNode.Name}' проигнорирован, так как его имя не содержит 'first_person'.\n");
            }
        }

        void Shutdown()
        {
            if (trigger != null)
            {
                trigger.EventEnter.Disconnect(OnPlayerEnter);
            }
        }
    }
}
