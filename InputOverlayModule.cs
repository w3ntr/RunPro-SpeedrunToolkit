using MelonLoader;
using System;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class InputOverlayModule
    {
        public bool IsEnabled = true;

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<bool> configEnabled;
        private MelonPreferences_Entry<float> configPosX;
        private MelonPreferences_Entry<float> configPosY;
        private MelonPreferences_Entry<float> configScale;
        private MelonPreferences_Entry<int> configActiveColor;
        private MelonPreferences_Entry<int> configNormalColor;

        public float PosX = 20f;
        public float PosY = 200f;
        public float Scale = 1.0f;
        public int ActiveColorIndex = 0; // Зеленый по умолчанию
        public int NormalColorIndex = 6; // Темно-прозрачный по умолчанию

        // Пресеты цветов
        public static readonly Color[] PresetColors = new Color[]
        {
            new Color(0.1f, 0.8f, 0.3f, 0.85f), // Green
            new Color(0.0f, 0.8f, 1.0f, 0.85f), // Cyan
            new Color(1.0f, 0.2f, 0.2f, 0.85f), // Red
            new Color(1.0f, 0.8f, 0.0f, 0.85f), // Yellow
            new Color(0.9f, 0.3f, 0.9f, 0.85f), // Pink
            new Color(1.0f, 1.0f, 1.0f, 0.85f), // White
            new Color(0.1f, 0.1f, 0.1f, 0.5f),  // Dark Translucent
            new Color(0.3f, 0.3f, 0.3f, 0.7f),  // Gray
        };

        public static readonly string[] ColorNames = new string[]
        {
            "Green", "Cyan", "Red", "Yellow", "Pink", "White", "Dark Translucent", "Gray"
        };

        private Texture2D normalTex;
        private Texture2D activeTex;
        private bool texturesInitialized = false;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("InputOverlayMod", "Input Overlay Settings");
            configEnabled = configCategory.CreateEntry("OverlayEnabled", true, "Enable Input Overlay");
            configPosX = configCategory.CreateEntry("PosX", 20f, "Position X");
            configPosY = configCategory.CreateEntry("PosY", 200f, "Position Y");
            configScale = configCategory.CreateEntry("Scale", 1.0f, "Overlay Scale");
            configActiveColor = configCategory.CreateEntry("ActiveColor", 0, "Active Color Index");
            configNormalColor = configCategory.CreateEntry("NormalColor", 6, "Normal Color Index");

            IsEnabled = configEnabled.Value;
            PosX = configPosX.Value;
            PosY = configPosY.Value;
            Scale = configScale.Value;
            ActiveColorIndex = Mathf.Clamp(configActiveColor.Value, 0, PresetColors.Length - 1);
            NormalColorIndex = Mathf.Clamp(configNormalColor.Value, 0, PresetColors.Length - 1);

            MelonLogger.Msg("[Input Overlay] Module initialized.");
        }

        public void RefreshTextures()
        {
            texturesInitialized = false;
        }

        private void InitTextures()
        {
            // Если текстуры удалились из памяти Unity, создаем их заново
            if (texturesInitialized && normalTex != null && activeTex != null) return;

            if (normalTex != null) UnityEngine.Object.Destroy(normalTex);
            if (activeTex != null) UnityEngine.Object.Destroy(activeTex);

            normalTex = MakeTexture(2, 2, PresetColors[NormalColorIndex]);
            activeTex = MakeTexture(2, 2, PresetColors[ActiveColorIndex]);
            texturesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public void OnGUI()
        {
            if (!IsEnabled) return;

            InitTextures();

            float keySize = 40f * Scale;
            float gap = 4f * Scale;

            // Клавиши движения
            bool w = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            bool a = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool s = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            bool d = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

            // Действия
            bool space = Input.GetKey(KeyCode.Space);
            bool lmb = Input.GetMouseButton(0);

            // 1. Кнопка W
            DrawKey(new Rect(PosX + keySize + gap, PosY, keySize, keySize), "W", w);

            // 2. Кнопки A, S, D
            float row1Y = PosY + keySize + gap;
            DrawKey(new Rect(PosX, row1Y, keySize, keySize), "A", a);
            DrawKey(new Rect(PosX + keySize + gap, row1Y, keySize, keySize), "S", s);
            DrawKey(new Rect(PosX + (keySize + gap) * 2, row1Y, keySize, keySize), "D", d);

            // 3. LMB (высокая кнопка мыши)
            float lmbWidth = keySize * 1.2f;
            float lmbHeight = (keySize * 2f) + gap;
            DrawKey(new Rect(PosX + (keySize + gap) * 3 + gap, PosY, lmbWidth, lmbHeight), "LMB", lmb);

            // 4. SPACE (широкая плашка)
            float row2Y = PosY + (keySize + gap) * 2;
            float spaceWidth = (keySize * 3f) + (gap * 2f);
            DrawKey(new Rect(PosX, row2Y, spaceWidth, keySize * 0.8f), "SPACE", space);
        }

        private void DrawKey(Rect rect, string label, bool isActive)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = isActive ? activeTex : normalTex;

            // Динамический выбор цвета текста (черный для светлых клавиш, белый для темных)
            Color currentCol = isActive ? PresetColors[ActiveColorIndex] : PresetColors[NormalColorIndex];
            bool isLight = (currentCol.r * 0.3f + currentCol.g * 0.59f + currentCol.b * 0.11f) > 0.6f;
            style.normal.textColor = isLight ? Color.black : Color.white;

            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = Mathf.RoundToInt(13f * Scale);
            style.fontStyle = FontStyle.Bold;

            GUI.Box(rect, label, style);
        }

        public void DrawUI(float startX, float startY, float width)
        {
            GUI.Label(new Rect(startX, startY, width, 20), "<b>Input Overlay Settings</b>");

            bool newEnabled = GUI.Toggle(new Rect(startX, startY + 22, width, 20), IsEnabled, " Enable Input Overlay");
            if (newEnabled != IsEnabled)
            {
                IsEnabled = newEnabled;
                configEnabled.Value = IsEnabled;
                configCategory.SaveToFile();
            }

            if (!IsEnabled) return;

            // Выбор цвета активных и неактивных клавиш
            if (GUI.Button(new Rect(startX, startY + 45, width, 22), $"Active Color: [{ColorNames[ActiveColorIndex]}]"))
            {
                ActiveColorIndex = (ActiveColorIndex + 1) % PresetColors.Length;
                configActiveColor.Value = ActiveColorIndex;
                configCategory.SaveToFile();
                RefreshTextures();
            }

            if (GUI.Button(new Rect(startX, startY + 70, width, 22), $"Inactive Color: [{ColorNames[NormalColorIndex]}]"))
            {
                NormalColorIndex = (NormalColorIndex + 1) % PresetColors.Length;
                configNormalColor.Value = NormalColorIndex;
                configCategory.SaveToFile();
                RefreshTextures();
            }

            // Настройки масштаба и позиции
            GUI.Label(new Rect(startX, startY + 95, width, 20), $"Scale: {Scale:F1}x");
            float newScale = GUI.HorizontalSlider(new Rect(startX, startY + 115, width, 15), Scale, 0.6f, 2.0f);
            if (Mathf.Abs(newScale - Scale) > 0.05f)
            {
                Scale = newScale;
                configScale.Value = Scale;
                configCategory.SaveToFile();
            }

            GUI.Label(new Rect(startX, startY + 135, width, 20), $"Position X: {(int)PosX}");
            float newX = GUI.HorizontalSlider(new Rect(startX, startY + 155, width, 15), PosX, 0f, Screen.width - 200);

            GUI.Label(new Rect(startX, startY + 175, width, 20), $"Position Y: {(int)PosY}");
            float newY = GUI.HorizontalSlider(new Rect(startX, startY + 195, width, 15), PosY, 0f, Screen.height - 150);

            if (Mathf.Abs(newX - PosX) > 0.1f || Mathf.Abs(newY - PosY) > 0.1f)
            {
                PosX = newX;
                PosY = newY;
                configPosX.Value = PosX;
                configPosY.Value = PosY;
                configCategory.SaveToFile();
            }
        }
    }
}
