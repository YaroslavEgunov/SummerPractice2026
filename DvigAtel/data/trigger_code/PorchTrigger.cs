using System;
using System.IO;
using Unigine;
using Path = System.IO.Path;
using File = System.IO.File;

namespace UnigineApp.data.trigger_code
{
    /// <summary>
    /// Класс для триггера, который записывает логи в файл при входе игрока в триггерную зону
    /// и проверяет статус от ML-модели.
    /// </summary>
    public class PorchTrigger : Component
    {
        private WorldTrigger trigger;
        private string logFilePath;
        private string mlStatusPath;
        private float checkInterval = 0.5f;
        private float lastCheckTime = 0.0f;

        /// <summary>
        /// Функция инициализации компонента. Вызывается при создании объекта.
        /// </summary>
        void Init()
        {
            trigger = node as WorldTrigger;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            logFilePath = Path.Combine(desktopPath, "log.txt");
            mlStatusPath = Path.Combine(desktopPath, "ml_status.json");

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
        /// Функция обновления компонента. Вызывается каждый кадр.
        /// </summary>
        void Update()
        {
            if (Game.Time - lastCheckTime > checkInterval)
            {
                lastCheckTime = Game.Time;
                CheckMLStatus();
            }
        }

        /// <summary>
        /// Проверка статуса от ML-модели.
        /// </summary>
        private void CheckMLStatus()
        {
            if (!File.Exists(mlStatusPath)) return;

            try
            {
                string json = File.ReadAllText(mlStatusPath);
                // Простая проверка подстроки, чтобы не подключать тяжелые библиотеки
                if (json.Contains("\"is_danger\": true") || json.Contains("\"is_danger\":true"))
                {
                    // Вывод сообщения в консоль и на экран (если доступно)
                    Log.Warning("[PorchTrigger] ML МОНИТОРИНГ: ОБНАРУЖЕН ЧЕЛОВЕК В ОПАСНОЙ ЗОНЕ!\n");
                }
            }
            catch { /* Файл может быть занят другим процессом */ }
        }

        /// <summary>
        /// Функция, вызываемая при физическом входе объекта в триггер.
        /// </summary>
        /// <param name="enterNode">Объект, который пересекает триггер.</param>
        private void OnPlayerEnter(Node enterNode)
        {
            Log.Message($"[PorchTrigger] Объект '{enterNode.Name}' вошел в физический триггер.\n");

            if (enterNode.Name.Contains("first_person"))
            {
                string logMessage = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] КТО-ТО ЗАШЕЛ В ЗОНУ (Физический триггер: {enterNode.Name})";

                try
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Log.Error($"[PorchTrigger] Не удалось записать лог: {ex.Message}\n");
                }
            }
        }

        /// <summary>
        /// Очистка при удалении.
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
