using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unigine;

using Path = System.IO.Path;
using File = System.IO.File;

namespace UnigineApp.data.camera_record
{
    /// <summary>
    /// Класс для записи видео с камеры.
    /// </summary>
    public class CameraRecorder : Component
    {
        [ShowInEditor]
        [Parameter(Group = "Tracking", 
            Tooltip = "Игрок (или его нода), за которым следит камера")]
        private Node targetToTrack = null;

        [ShowInEditor]
        [ParameterSlider(Min = 1, Max = 120, Group = "Recording", 
            Tooltip = "Capture rate in frames per second")]
        private int targetFps = 30;

        private Player previousPlayer;
        private Player recordingPlayer;

        private string recordingsRoot;
        private string sessionFolder;

        private bool isRecording;
        private int frameIndex;
        private DateTime nextCaptureTimeUtc;

        private bool prevF5;
        private bool prevF6;

        private Func<Input.KEY, bool> keyQuery;

        /// <summary>
        /// Функция инициализации компонента. Вызывается при создании объекта.
        /// </summary>
        void Init()
        {
            string desktopPath = Environment.GetFolderPath
                (Environment.SpecialFolder.DesktopDirectory);
            recordingsRoot = Path.Combine(desktopPath, "UnigineCameraRecords");
            Directory.CreateDirectory(recordingsRoot);

            recordingPlayer = node as Player;
            if (recordingPlayer == null)
            {
                Log.Error("[CameraRecorder] Компонент нужно повесить на ноду типа Player " +
                    "(например 'Пустышка' во вкладке Камеры).\n");
                return;
            }

            keyQuery = ResolveKeyQuery();

            Log.Message("[CameraRecorder] Готов. F5 — старт записи и просмотр через камеру," +
                " F6 — стоп.\n");
        }

        /// <summary>
        /// Функция обновления и записи видео. Вызывается каждый кадр.
        /// </summary>
        void Update()
        {
            if (recordingPlayer == null)
                return;

            // Постоянно следим за целью, если она назначена
            if (targetToTrack != null)
            {
                vec3 direction = targetToTrack.WorldPosition - recordingPlayer.WorldPosition;
                // Чтобы не было ошибок деления на ноль, если объекты в одной точке
                if (direction.Length2 > 0.001f)
                {
                    // Поворачиваем камеру в сторону цели с фиксированным вектором UP
                    recordingPlayer.SetWorldDirection(direction, vec3.UP);
                }
            }

            if (keyQuery == null) return;

            // Обработка кнопок старта / стопа
            if (PressedOnce(Input.KEY.F5, ref prevF5))
                StartRecording();

            if (PressedOnce(Input.KEY.F6, ref prevF6))
                StopRecording();

            // Запись кадров
            if (!isRecording || DateTime.UtcNow < nextCaptureTimeUtc)
                return;

            nextCaptureTimeUtc = DateTime.UtcNow.AddSeconds(1.0 / Math.Max(1, targetFps));

            try
            {
                CaptureInternalFrame();
            }
            catch (Exception ex)
            {
                Log.Error($"[CameraRecorder] Ошибка захвата: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Функция, вызываемая при удалении компонента. Вызывается при удалении объекта.
        /// </summary>
        void Shutdown()
        {
            if (isRecording) StopRecording();
        }

        /// <summary>
        /// Функция включения записи видео с камеры.
        /// </summary>
        private void StartRecording()
        {
            if (isRecording || recordingPlayer == null) return;

            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            sessionFolder = Path.Combine(recordingsRoot, stamp);
            Directory.CreateDirectory(sessionFolder);

            frameIndex = 0;
            isRecording = true;
            nextCaptureTimeUtc = DateTime.UtcNow;

            // Переключаем вид из глаз в камеру наблюдения
            previousPlayer = Game.Player;
            Game.Player = recordingPlayer;

            Log.Message($"[CameraRecorder] Запись с камеры началась: {sessionFolder}\n");
        }

        /// <summary>
        /// Функция выключения записи видео с камеры.
        /// </summary>
        private void StopRecording()
        {
            if (!isRecording) return;
            isRecording = false;

            // Возвращаем вид обратно игроку
            if (previousPlayer != null)
                Game.Player = previousPlayer;

            Log.Message("[CameraRecorder] Запись остановлена.\n");
        }

        /// <summary>
        /// Функция захвата кадра с камеры и сохранения его в файл.
        /// </summary>
        private void CaptureInternalFrame()
        {
            string framePath = Path.Combine(sessionFolder, $"frame_{frameIndex:000000}.jpg");

            if (WindowManager.MainWindow != null)
            {
                WindowManager.MainWindow.Screenshot(framePath);
            }

            frameIndex++;
        }

        /// <summary>
        /// Функция проверки, была ли нажата клавиша один раз.
        /// </summary>
        /// <param name="key">Переменная клавиши для проверки.</param>
        /// <param name="previousState">Переменная для хранения предыдущего состояния клавиши.</param>
        /// <returns></returns>
        private bool PressedOnce(Input.KEY key, ref bool previousState)
        {
            bool currentState = keyQuery(key);
            bool pressed = currentState && !previousState;
            previousState = currentState;
            return pressed;
        }

        /// <summary>
        /// Функция для определения способа проверки состояния клавиш в зависимости от платформы.
        /// </summary> 
        /// <returns>Возвращает функцию, которая проверяет состояние клавиши.</returns> 
        private Func<Input.KEY, bool> ResolveKeyQuery()
        {
            MethodInfo[] methods = typeof(Input).GetMethods
                (BindingFlags.Public | BindingFlags.Static);
            string[] names = { "IsKeyPressed", "IsKeyDown", "GetKeyDown" };
            foreach (string name in names)
            {
                MethodInfo method = methods.FirstOrDefault(m => m.Name == name && 
                    m.GetParameters().Length == 1 && 
                    m.GetParameters()[0].ParameterType == typeof(Input.KEY));
                if (method != null) return key => (bool)method.Invoke(null, new object[] { key });
            }
            return null;
        }
    }
}