using System;
using System.Globalization;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public static class UITheme
    {
        public class ThemePreset
        {
            public string Name;
            public Color Background;
            public Color Surface0;
            public Color Surface1;
            public Color Accent;
            public Color Text;

            public ThemePreset(string name, string bg, string s0, string s1, string accent, string text)
            {
                Name = name;
                Background = HexToColor(bg);
                Surface0 = HexToColor(s0);
                Surface1 = HexToColor(s1);
                Accent = HexToColor(accent);
                Text = HexToColor(text);
            }
        }

        public static readonly ThemePreset[] Presets = new ThemePreset[]
        {
            new ThemePreset("Catppuccin", "#1E1E2E", "#313244", "#45475A", "#CBA6F7", "#CDD6F4"),
            new ThemePreset("Cyberpunk",  "#0D0F18", "#1A1D2E", "#2A2E45", "#00FF99", "#E0E6ED"),
            new ThemePreset("Tokyo Night", "#1A1B26", "#24283B", "#414868", "#7AA2F7", "#C0CAF5"),
            new ThemePreset("Nord",        "#2E3440", "#3B4252", "#4C566A", "#88C0D0", "#ECEFF4"),
            new ThemePreset("Dracula",     "#282A36", "#44475A", "#6272A4", "#BD93F9", "#F8F8F2")
        };

        public static int ActivePresetIndex = 0;

        // Настройки Акцента (Accent)
        public static string CustomAccentHex = "#CBA6F7";
        public static float AccentH = 0.73f;
        public static float AccentS = 0.33f;
        public static float AccentV = 0.96f;

        // Настройки Фона (Background)
        public static string CustomBgHex = "#1E1E2E";
        public static float BgH = 0.67f;
        public static float BgS = 0.35f;
        public static float BgV = 0.18f;

        public static GUIStyle WindowStyle;
        public static GUIStyle TabStyle;
        public static GUIStyle TabActiveStyle;
        public static GUIStyle ButtonStyle;
        public static GUIStyle LabelStyle;
        public static GUIStyle ResizeHandleStyle;
        public static GUIStyle TextFieldStyle;

        private static float cachedOpacity = -1f;

        public static void SetPreset(int index)
        {
            ActivePresetIndex = index;
            SetAccentColor(Presets[index].Accent);
            SetBgColor(Presets[index].Background);
        }

        public static void SetAccentColor(Color color)
        {
            CustomAccentHex = ColorToHex(color);
            RGBToHSV(color, out AccentH, out AccentS, out AccentV);
            ForceRebuild();
        }

        public static void SetBgColor(Color color)
        {
            CustomBgHex = ColorToHex(color);
            RGBToHSV(color, out BgH, out BgS, out BgV);
            ForceRebuild();
        }

        public static void ForceRebuild()
        {
            cachedOpacity = -1f;
        }

        public static void InitStyles(float opacity)
        {
            if (WindowStyle != null && WindowStyle.normal.background != null && Mathf.Approximately(cachedOpacity, opacity))
                return;

            cachedOpacity = opacity;

            ThemePreset current = Presets[ActivePresetIndex];
            Color activeAccent = HexToColor(CustomAccentHex, current.Accent);
            Color activeBg = HexToColor(CustomBgHex, current.Background);

            // Динамический расчёт цвета кнопок/карточек под выведенный фон
            Color surface0 = AdjustBrightness(activeBg, 0.08f);
            Color surface1 = AdjustBrightness(activeBg, 0.15f);

            Texture2D bgTex = MakeTexture(1, 1, ApplyAlpha(activeBg, opacity));
            Texture2D cardTex = MakeTexture(1, 1, ApplyAlpha(surface0, opacity));
            Texture2D hoverTex = MakeTexture(1, 1, ApplyAlpha(surface1, opacity));
            Texture2D accentTex = MakeTexture(1, 1, ApplyAlpha(activeAccent, opacity));

            WindowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset { left = 12, right = 12, top = 28, bottom = 12 },
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            WindowStyle.normal.background = bgTex;
            WindowStyle.normal.textColor = activeAccent;
            WindowStyle.onNormal.background = bgTex;

            TabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            TabStyle.normal.background = cardTex;
            TabStyle.normal.textColor = current.Text;
            TabStyle.hover.background = hoverTex;

            TabActiveStyle = new GUIStyle(TabStyle)
            {
                fontStyle = FontStyle.Bold
            };
            TabActiveStyle.normal.background = accentTex;
            TabActiveStyle.normal.textColor = activeBg;

            ButtonStyle = new GUIStyle(TabStyle);

            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12
            };
            LabelStyle.normal.textColor = current.Text;

            ResizeHandleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 12
            };
            ResizeHandleStyle.normal.textColor = ApplyAlpha(activeAccent, opacity);

            TextFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            TextFieldStyle.normal.background = cardTex;
            TextFieldStyle.normal.textColor = activeAccent;
            TextFieldStyle.focused.background = hoverTex;
            TextFieldStyle.focused.textColor = activeAccent;
        }

        public static void DrawColorPreview(Rect rect, Color color)
        {
            Texture2D swatchTex = MakeTexture(1, 1, color);
            GUI.DrawTexture(rect, swatchTex);
        }

        private static Color AdjustBrightness(Color baseColor, float amount)
        {
            return new Color(
                Mathf.Clamp01(baseColor.r + amount),
                Mathf.Clamp01(baseColor.g + amount),
                Mathf.Clamp01(baseColor.b + amount),
                baseColor.a
            );
        }

        public static Color HexToColor(string hex, Color fallback = default)
        {
            if (string.IsNullOrEmpty(hex)) return fallback == default ? Color.magenta : fallback;

            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                if (byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
                    byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
                    byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                {
                    return new Color(r / 255f, g / 255f, b / 255f, 1f);
                }
            }
            return fallback == default ? Color.magenta : fallback;
        }

        public static string ColorToHex(Color color)
        {
            byte r = (byte)Mathf.Clamp((int)(color.r * 255f), 0, 255);
            byte g = (byte)Mathf.Clamp((int)(color.g * 255f), 0, 255);
            byte b = (byte)Mathf.Clamp((int)(color.b * 255f), 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public static Color HSVToRGB(float h, float s, float v)
        {
            if (s == 0) return new Color(v, v, v);
            h = (h % 1.0f) * 6.0f;
            int i = (int)Mathf.Floor(h);
            float f = h - i;
            float p = v * (1.0f - s);
            float q = v * (1.0f - s * f);
            float t = v * (1.0f - s * (1.0f - f));
            switch (i)
            {
                case 0: return new Color(v, t, p);
                case 1: return new Color(q, v, p);
                case 2: return new Color(p, v, t);
                case 3: return new Color(p, q, v);
                case 4: return new Color(t, p, v);
                default: return new Color(v, p, q);
            }
        }

        public static void RGBToHSV(Color color, out float h, out float s, out float v)
        {
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float delta = max - min;
            v = max;
            if (delta < 0.00001f) { h = 0; s = 0; return; }
            s = (max > 0) ? (delta / max) : 0;
            if (color.r >= max) h = (color.g - color.b) / delta;
            else if (color.g >= max) h = 2.0f + (color.b - color.r) / delta;
            else h = 4.0f + (color.r - color.g) / delta;
            h /= 6.0f;
            if (h < 0) h += 1.0f;
        }

        private static Color ApplyAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.hideFlags = HideFlags.DontSave;
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}