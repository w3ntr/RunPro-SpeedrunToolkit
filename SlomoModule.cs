using MelonLoader;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class SlomoModule
    {
        public float CurrentScale = 1.0f;
        public bool IsSlomoActive => CurrentScale < 0.99f;

        // Горячие клавиши (можно использовать как Numpad, так и квадратные скобки [ и ])
        private KeyCode decreaseKey = KeyCode.KeypadMinus;
        private KeyCode increaseKey = KeyCode.KeypadPlus;
        private KeyCode resetKey = KeyCode.Keypad0;

        private KeyCode altDecreaseKey = KeyCode.LeftBracket;  // '['
        private KeyCode altIncreaseKey = KeyCode.RightBracket; // ']'

        public void Init()
        {
            MelonLogger.Msg("[Slomo] Module initialized.");
        }

        public void OnUpdate()
        {
            // Уменьшение скорости
            if (Input.GetKeyDown(decreaseKey) || Input.GetKeyDown(altDecreaseKey))
            {
                ChangeSpeed(-0.1f);
            }

            // Увеличение скорости
            if (Input.GetKeyDown(increaseKey) || Input.GetKeyDown(altIncreaseKey))
            {
                ChangeSpeed(0.1f);
            }

            // Быстрый сброс на 1.0x
            if (Input.GetKeyDown(resetKey))
            {
                SetSpeed(1.0f);
            }
        }

        public void ChangeSpeed(float delta)
        {
            SetSpeed(CurrentScale + delta);
        }

        public void SetSpeed(float scale)
        {
            // Ограничиваем от 0.1x до 1.0x (не выше 1.0x!)
            CurrentScale = Mathf.Clamp(scale, 0.1f, 1.0f);

            // Округляем до сотых для красоты (например, 0.5 вместо 0.5000001)
            CurrentScale = Mathf.Round(CurrentScale * 10f) / 10f;

            Time.timeScale = CurrentScale;

            // Синхронизируем физику Unity, чтобы не было микро-фризов и лагов
            Time.fixedDeltaTime = 0.02f * CurrentScale;

            MelonLogger.Msg($"[Slomo] Speed set to: {CurrentScale}x");
        }

        public void ResetSpeed()
        {
            SetSpeed(1.0f);
        }
    }
}