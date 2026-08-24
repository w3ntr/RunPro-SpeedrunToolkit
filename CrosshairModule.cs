using MelonLoader;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class CrosshairModule
    {
        public bool EnableCrosshair = true;
        public int CrosshairType = 1; // 0 = Dot, 1 = Cross, 2 = Cross + Dot

        public float Size = 10f;
        public float Thickness = 2f;
        public float Gap = 4f;
        public float DotSize = 3f;

        public float ColorR = 0f;
        public float ColorG = 1f; // Зеленый по умолчанию
        public float ColorB = 0f;
        public float Transparency = 1.0f;

        public bool DrawOutline = true;

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<bool> configEnable;
        private MelonPreferences_Entry<int> configType;
        private MelonPreferences_Entry<float> configSize;
        private MelonPreferences_Entry<float> configThickness;
        private MelonPreferences_Entry<float> configGap;
        private MelonPreferences_Entry<float> configDotSize;
        private MelonPreferences_Entry<float> configR;
        private MelonPreferences_Entry<float> configG;
        private MelonPreferences_Entry<float> configB;
        private MelonPreferences_Entry<float> configA;
        private MelonPreferences_Entry<bool> configOutline;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("CrosshairMod", "Custom Crosshair Settings");
            configEnable = configCategory.CreateEntry("EnableCrosshair", true, "Enable Custom Crosshair");
            configType = configCategory.CreateEntry("CrosshairType", 1, "Crosshair Type");
            configSize = configCategory.CreateEntry("Size", 10f, "Crosshair Line Size");
            configThickness = configCategory.CreateEntry("Thickness", 2f, "Crosshair Line Thickness");
            configGap = configCategory.CreateEntry("Gap", 4f, "Crosshair Gap");
            configDotSize = configCategory.CreateEntry("DotSize", 3f, "Dot Size");
            configR = configCategory.CreateEntry("ColorR", 0f, "Red");
            configG = configCategory.CreateEntry("ColorG", 1f, "Green");
            configB = configCategory.CreateEntry("ColorB", 0f, "Blue");
            configA = configCategory.CreateEntry("Transparency", 1f, "Alpha");
            configOutline = configCategory.CreateEntry("DrawOutline", true, "Draw Black Outline");

            EnableCrosshair = configEnable.Value;
            CrosshairType = configType.Value;
            Size = configSize.Value;
            Thickness = configThickness.Value;
            Gap = configGap.Value;
            DotSize = configDotSize.Value;
            ColorR = configR.Value;
            ColorG = configG.Value;
            ColorB = configB.Value;
            Transparency = configA.Value;
            DrawOutline = configOutline.Value;

            MelonLogger.Msg("[CrosshairModule] Initialized.");
        }

        public void OnGUI()
        {
            if (!EnableCrosshair) return;

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            Color oldColor = GUI.color;
            Color mainColor = new Color(ColorR, ColorG, ColorB, Transparency);

            // 1. Точка (Dot)
            if (CrosshairType == 0 || CrosshairType == 2)
            {
                float dSize = DotSize;
                Rect dotRect = new Rect(centerX - dSize / 2f, centerY - dSize / 2f, dSize, dSize);

                if (DrawOutline)
                {
                    GUI.color = new Color(0, 0, 0, Transparency);
                    GUI.DrawTexture(new Rect(dotRect.x - 1, dotRect.y - 1, dotRect.width + 2, dotRect.height + 2), Texture2D.whiteTexture);
                }

                GUI.color = mainColor;
                GUI.DrawTexture(dotRect, Texture2D.whiteTexture);
            }

            // 2. Перекрестие (Cross)
            if (CrosshairType == 1 || CrosshairType == 2)
            {
                float t = Thickness;
                float s = Size;
                float g = Gap;

                Rect topRect = new Rect(centerX - t / 2f, centerY - g - s, t, s);
                Rect bottomRect = new Rect(centerX - t / 2f, centerY + g, t, s);
                Rect leftRect = new Rect(centerX - g - s, centerY - t / 2f, s, t);
                Rect rightRect = new Rect(centerX + g, centerY - t / 2f, s, t);

                if (DrawOutline)
                {
                    GUI.color = new Color(0, 0, 0, Transparency);
                    GUI.DrawTexture(new Rect(topRect.x - 1, topRect.y - 1, topRect.width + 2, topRect.height + 2), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(bottomRect.x - 1, bottomRect.y - 1, bottomRect.width + 2, bottomRect.height + 2), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(leftRect.x - 1, leftRect.y - 1, leftRect.width + 2, leftRect.height + 2), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rightRect.x - 1, rightRect.y - 1, rightRect.width + 2, rightRect.height + 2), Texture2D.whiteTexture);
                }

                GUI.color = mainColor;
                GUI.DrawTexture(topRect, Texture2D.whiteTexture);
                GUI.DrawTexture(bottomRect, Texture2D.whiteTexture);
                GUI.DrawTexture(leftRect, Texture2D.whiteTexture);
                GUI.DrawTexture(rightRect, Texture2D.whiteTexture);
            }

            GUI.color = oldColor;
        }

        public float DrawUI(float startX, float startY, float width)
        {
            float y = startY;

            GUI.Label(new Rect(startX, y, width, 18), "<b>Custom Crosshair Settings:</b>");
            y += 20f;

            bool newEnable = GUI.Toggle(new Rect(startX, y, width, 18), EnableCrosshair, " Enable Custom Crosshair");
            if (newEnable != EnableCrosshair)
            {
                EnableCrosshair = newEnable;
                configEnable.Value = EnableCrosshair;
                configCategory.SaveToFile();
            }
            y += 20f;

            if (!EnableCrosshair) return y;

            bool newOutline = GUI.Toggle(new Rect(startX, y, width, 18), DrawOutline, " Draw Black Outline");
            if (newOutline != DrawOutline)
            {
                DrawOutline = newOutline;
                configOutline.Value = DrawOutline;
                configCategory.SaveToFile();
            }
            y += 20f;

            // Стиль
            GUI.Label(new Rect(startX, y, width, 18), $"Style: <b>{(CrosshairType == 0 ? "Dot" : CrosshairType == 1 ? "Cross" : "Cross + Dot")}</b>");
            y += 18f;
            if (GUI.Button(new Rect(startX, y, 70, 20), "Dot")) { CrosshairType = 0; configType.Value = 0; configCategory.SaveToFile(); }
            if (GUI.Button(new Rect(startX + 75, y, 70, 20), "Cross")) { CrosshairType = 1; configType.Value = 1; configCategory.SaveToFile(); }
            if (GUI.Button(new Rect(startX + 150, y, 90, 20), "Cross+Dot")) { CrosshairType = 2; configType.Value = 2; configCategory.SaveToFile(); }
            y += 22f;

            // Размеры
            if (CrosshairType == 1 || CrosshairType == 2)
            {
                GUI.Label(new Rect(startX, y, width, 16), $"Length: {(int)Size}px | Thickness: {Thickness:F1}px | Gap: {(int)Gap}px");
                y += 16f;
                float newSize = GUI.HorizontalSlider(new Rect(startX, y, width, 12), Size, 2f, 30f);
                if (Mathf.Abs(newSize - Size) > 0.1f) { Size = newSize; configSize.Value = Size; configCategory.SaveToFile(); }
                y += 14f;

                float newThick = GUI.HorizontalSlider(new Rect(startX, y, width, 12), Thickness, 1f, 8f);
                if (Mathf.Abs(newThick - Thickness) > 0.1f) { Thickness = newThick; configThickness.Value = Thickness; configCategory.SaveToFile(); }
                y += 14f;

                float newGap = GUI.HorizontalSlider(new Rect(startX, y, width, 12), Gap, 0f, 20f);
                if (Mathf.Abs(newGap - Gap) > 0.1f) { Gap = newGap; configGap.Value = Gap; configCategory.SaveToFile(); }
                y += 16f;
            }

            if (CrosshairType == 0 || CrosshairType == 2)
            {
                GUI.Label(new Rect(startX, y, width, 16), $"Dot Size: {(int)DotSize}px");
                y += 16f;
                float newDot = GUI.HorizontalSlider(new Rect(startX, y, width, 12), DotSize, 1f, 12f);
                if (Mathf.Abs(newDot - DotSize) > 0.1f) { DotSize = newDot; configDotSize.Value = DotSize; configCategory.SaveToFile(); }
                y += 16f;
            }

            // Цвета RGB и прозрачность
            GUI.Label(new Rect(startX, y, width, 16), $"Color (R: {(int)(ColorR * 255)}, G: {(int)(ColorG * 255)}, B: {(int)(ColorB * 255)}) | Opacity: {(int)(Transparency * 100)}%");
            y += 16f;

            float nr = GUI.HorizontalSlider(new Rect(startX, y, width, 12), ColorR, 0f, 1f);
            y += 14f;
            float ng = GUI.HorizontalSlider(new Rect(startX, y, width, 12), ColorG, 0f, 1f);
            y += 14f;
            float nb = GUI.HorizontalSlider(new Rect(startX, y, width, 12), ColorB, 0f, 1f);
            y += 14f;

            if (Mathf.Abs(nr - ColorR) > 0.01f || Mathf.Abs(ng - ColorG) > 0.01f || Mathf.Abs(nb - ColorB) > 0.01f)
            {
                ColorR = nr; ColorG = ng; ColorB = nb;
                configR.Value = ColorR; configG.Value = ColorG; configB.Value = ColorB;
                configCategory.SaveToFile();
            }

            float na = GUI.HorizontalSlider(new Rect(startX, y, width, 12), Transparency, 0.1f, 1.0f);
            if (Mathf.Abs(na - Transparency) > 0.01f)
            {
                Transparency = na;
                configA.Value = Transparency;
                configCategory.SaveToFile();
            }
            y += 16f;

            return y;
        }
    }
}
