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

        /// <summary>
        /// Функция инициализации компонента. Вызывается при создании объекта.
        /// </summary>
        void Init()
        {
            trigger = node as WorldTrigger;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            logFilePath = Path.Combine(desktopPath, "log.txt");

            if (trigger != null)
            {
                trigger.EventEnter.Connect(OnPlayerEnter);
            }
            else
            {
                Log.Error($"[PorchTrigger] Ошибка: Текущий узел '{node.Name}' не является WorldTrigger\n");
            }
        }

        /// <summary>
        /// Функция, вызываемая при входе объекта в триггер.
        /// </summary>
        /// <param name="enterNode">Объект, который пересекает триггер.</param>
        private void OnPlayerEnter(Node enterNode)
        {
            // Выводим название того, кто задел триггер
            Log.Message($"[PorchTrigger] Объект '{enterNode.Name}' вошел в триггер.\n");

            if (enterNode.Name.Contains("first_person"))
            {
                string logMessage = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] Кто-то зашёл в красную зону (Объект: {enterNode.Name})";

                try
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Log.Error($"[PorchTrigger] Не удалось записать файл: {ex.Message}\n");
                }
            }
        }

        /// <summary>
        /// Функция, вызываемая при выходе объекта из триггера.
        /// </summary>
        void Shutdown()
        {
            if (trigger != null)
            {
                trigger.EventEnter.Disconnect(OnPlayerEnter);
            }
        }
    }
}
