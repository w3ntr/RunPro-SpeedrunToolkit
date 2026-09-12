using System;
using MelonLoader;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class AntiAfkModule
    {
        public static bool EnableAntiAfk = false;
        private float nextNudgeTime = 0f;
        private bool toggleDirection = false;

        public void Init()
        {
            MelonLogger.Msg("[Anti-AFK] Module initialized.");
        }

        public void OnUpdate()
        {
            if (!EnableAntiAfk) return;

            // 1. Не даем игре засыпать в фоновом режиме
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
            }

            // 2. Делаем физический микро-сдвиг каждые 2 секунды
            if (Time.unscaledTime >= nextNudgeTime)
            {
                nextNudgeTime = Time.unscaledTime + 2.0f;
                PulseHeartbeat();
            }
        }

        private void PulseHeartbeat()
        {
            try
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player == null) return;

                // Физически сдвигаем персонажа в пространстве туда-обратно,
                // чтобы движок сгенерировал и отправил реальный пакет движения на сервер
                float offset = toggleDirection ? 0.005f : -0.005f;
                player.transform.position += new Vector3(offset, 0f, offset);
                toggleDirection = !toggleDirection;
            }
            catch
            {
                // Игнорируем вне игры
            }
        }

        public void DrawUI(float x, float y, float contentWidth)
        {
            bool newAntiAfk = GUI.Toggle(new Rect(x, y, contentWidth, 20f), EnableAntiAfk, " Enable Anti-AFK / Anti-Kick");
            if (newAntiAfk != EnableAntiAfk)
            {
                EnableAntiAfk = newAntiAfk;
                if (!EnableAntiAfk)
                {
                    Application.runInBackground = false;
                }
            }
        }
    }
}