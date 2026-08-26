using MelonLoader;
using UnityEngine;
using System;
using System.Diagnostics;

namespace SpeedrunToolkitMod
{
    public class TimerModule
    {
        private Stopwatch stopwatch = new Stopwatch();
        public bool ShowMilliseconds = true;

        private enum TimerState { Stopped, Running, Finished }
        private TimerState currentState = TimerState.Stopped;
        private TimeSpan finalTime = TimeSpan.Zero;
        private string lastSceneName = "";

        // Переменные для отслеживания движения по позиции
        private Vector3 lastPosition = Vector3.zero;
        private bool positionInitialized = false;

        public void StartTimer()
        {
            stopwatch.Restart();
            currentState = TimerState.Running;
        }

        public void StopTimer()
        {
            if (currentState == TimerState.Running)
            {
                finalTime = stopwatch.Elapsed;
                stopwatch.Stop();
                currentState = TimerState.Finished;
            }
        }

        public void ResetTimer()
        {
            stopwatch.Reset();
            currentState = TimerState.Stopped;
            finalTime = TimeSpan.Zero;
            positionInitialized = false;
        }

        public bool IsRunning()
        {
            return currentState == TimerState.Running;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 3600)
            {
                if (ShowMilliseconds)
                    return string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                else
                    return string.Format("{0:D2}:{1:D2}.{2:D2}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds / 10);
            }
            else
            {
                if (ShowMilliseconds)
                    return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
                else
                    return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds / 10);
            }
        }

        public string GetFormattedTime()
        {
            TimeSpan currentSpan;

            if (currentState == TimerState.Running)
            {
                currentSpan = stopwatch.Elapsed;
            }
            else if (currentState == TimerState.Finished)
            {
                currentSpan = finalTime;
            }
            else
            {
                currentSpan = TimeSpan.Zero;
            }

            return FormatTimeSpan(currentSpan);
        }

        public void OnUpdate()
        {
            // Автоматический сброс при смене сцены или рестарте уровня
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene != lastSceneName)
            {
                lastSceneName = currentScene;
                ResetTimer();
            }

            // Автостарт по началу движения (отслеживаем смещение камеры/игрока)
            if (currentState == TimerState.Stopped)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    if (!positionInitialized)
                    {
                        lastPosition = cam.transform.position;
                        positionInitialized = true;
                    }
                    else
                    {
                        // Если игрок сдвинулся с начальной точки хотя бы на чуть-чуть
                        if (Vector3.Distance(cam.transform.position, lastPosition) > 0.005f)
                        {
                            StartTimer();
                        }
                        lastPosition = cam.transform.position;
                    }
                }
            }

            // Автостоп: ищем появление текста победы "YOU HAVE COMPLETED" на экране
            if (currentState == TimerState.Running)
            {
                foreach (var textMesh in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>())
                {
                    if (textMesh != null && textMesh.text != null && textMesh.text.Contains("COMPLETED"))
                    {
                        StopTimer();
                        break;
                    }
                }
            }
        }
    }
}

        // public void DrawUI(float startX, float startY, float width)
        // {
        //  float y = startY;

// GUI.Label(new Rect(startX, y, width, 20), $"Current Time: <size=14><b>{GetFormattedTime()}</b></size>");
// y += 24f;

// float btnWidth = (width - 10f) / 3f;
// if (GUI.Button(new Rect(startX, y, btnWidth, 20), "Start")) StartTimer();
// if (GUI.Button(new Rect(startX + btnWidth + 5f, y, btnWidth, 20), "Stop")) StopTimer();
// if (GUI.Button(new Rect(startX + (btnWidth + 5f) * 2, y, btnWidth, 20), "Reset")) ResetTimer();
// y += 24f;
// ShowMilliseconds = GUI.Toggle(new Rect(startX, y, width, 20), ShowMilliseconds, " Show Milliseconds (.123 vs .12)");
// }
// }
// }