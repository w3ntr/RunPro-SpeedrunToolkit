using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace SpeedrunToolkitMod
{
    public class GraphicsModule
    {
        public bool DisablePostProcessing = false;
        public bool DisableShadows = false;
        public bool HideHands = false;
        public bool HideGameHUD = false;
        public int TextureQuality = 0; // 0 = High, 1 = Medium, 2 = Low (Potato)

        // Настройки дальности
        public bool EnableCustomRenderDistance = false;
        public float RenderDistance = 1000f;
        public bool EnableCustomShadowDistance = false;
        public float ShadowDistance = 150f;

        private float defaultFarClip = 1000f;
        private float defaultShadowDist = 150f;
        private bool hasDefaults = false;

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<bool> configDisablePP;
        private MelonPreferences_Entry<bool> configDisableShadows;
        private MelonPreferences_Entry<bool> configHideHands;
        private MelonPreferences_Entry<bool> configHideGameHUD;
        private MelonPreferences_Entry<int> configTextureQuality;

        private MelonPreferences_Entry<bool> configEnableCustomRenderDistance;
        private MelonPreferences_Entry<float> configRenderDistance;
        private MelonPreferences_Entry<bool> configEnableCustomShadowDistance;
        private MelonPreferences_Entry<float> configShadowDistance;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("GraphicsMod", "Graphics Settings");
            configDisablePP = configCategory.CreateEntry("DisablePostProcessing", false, "Disable Post Processing");
            configDisableShadows = configCategory.CreateEntry("DisableShadows", false, "Disable Shadows");
            configHideHands = configCategory.CreateEntry("HideHands", false, "Hide First Person Hands");
            configHideGameHUD = configCategory.CreateEntry("HideGameHUD", false, "Hide Native Game HUD");
            configTextureQuality = configCategory.CreateEntry("TextureQuality", 0, "Texture Quality Limit");

            configEnableCustomRenderDistance = configCategory.CreateEntry("EnableCustomRenderDistance", false, "Enable Custom Render Distance");
            configRenderDistance = configCategory.CreateEntry("RenderDistance", 1000f, "Render Distance");
            configEnableCustomShadowDistance = configCategory.CreateEntry("EnableCustomShadowDistance", false, "Enable Custom Shadow Distance");
            configShadowDistance = configCategory.CreateEntry("ShadowDistance", 150f, "Shadow Distance");

            DisablePostProcessing = configDisablePP.Value;
            DisableShadows = configDisableShadows.Value;
            HideHands = configHideHands.Value;
            HideGameHUD = configHideGameHUD.Value;
            TextureQuality = Mathf.Clamp(configTextureQuality.Value, 0, 2);

            EnableCustomRenderDistance = configEnableCustomRenderDistance.Value;
            RenderDistance = configRenderDistance.Value;
            EnableCustomShadowDistance = configEnableCustomShadowDistance.Value;
            ShadowDistance = configShadowDistance.Value;

            MelonLogger.Msg("[Graphics] Module initialized.");
        }

        public void ApplyGraphicsSettings()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (!hasDefaults)
                {
                    defaultFarClip = mainCam.farClipPlane;
                    defaultShadowDist = QualitySettings.shadowDistance;
                    hasDefaults = true;
                }

                // 1. Пост-обработка (Bloom, Motion Effects)
                var ppLayer = mainCam.GetComponent<PostProcessLayer>();
                if (ppLayer != null)
                {
                    ppLayer.enabled = !DisablePostProcessing;
                }

                // 2. Custom Draw Distance
                if (EnableCustomRenderDistance)
                {
                    mainCam.farClipPlane = RenderDistance;
                }
                else
                {
                    mainCam.farClipPlane = defaultFarClip;
                }
            }

            // 3. Shadows
            QualitySettings.shadows = DisableShadows ? ShadowQuality.Disable : ShadowQuality.All;

            // 4. Custom Shadow Distance
            if (EnableCustomShadowDistance)
            {
                QualitySettings.shadowDistance = ShadowDistance;
            }

            // 5. Quality
            QualitySettings.masterTextureLimit = TextureQuality;

            // 6. HideGameHUD and Hands
            ApplyHandsSettings();
            ApplyGameHUDSettings();
        }

        public void ApplyHandsSettings()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Renderer[] renderers = mainCam.GetComponentsInChildren<Renderer>(true);
                foreach (var rend in renderers)
                {
                    if (rend != null && rend.gameObject != mainCam.gameObject)
                    {
                        rend.enabled = !HideHands;
                    }
                }
            }
        }

        public void ApplyGameHUDSettings()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = !HideGameHUD;
                }
            }
        }

        public void DrawUI(float startX, float startY, float width)
        {
            float y = startY;

            GUI.Label(new Rect(startX, y, width, 20), "<b>Graphics & View Settings</b>");
            y += 24f;

            // Post-Processing
            bool newPP = GUI.Toggle(new Rect(startX, y, width, 20), DisablePostProcessing, " Disable Post-Processing (Bloom, FX)");
            if (newPP != DisablePostProcessing)
            {
                DisablePostProcessing = newPP;
                configDisablePP.Value = DisablePostProcessing;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 22f;

            // Shadows
            bool newShadows = GUI.Toggle(new Rect(startX, y, width, 20), DisableShadows, " Disable Shadows (FPS Boost)");
            if (newShadows != DisableShadows)
            {
                DisableShadows = newShadows;
                configDisableShadows.Value = DisableShadows;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 22f;

            // Custom Draw Distance
            bool newCustomDist = GUI.Toggle(new Rect(startX, y, width, 20), EnableCustomRenderDistance, " Enable Custom Draw Distance");
            if (newCustomDist != EnableCustomRenderDistance)
            {
                EnableCustomRenderDistance = newCustomDist;
                configEnableCustomRenderDistance.Value = EnableCustomRenderDistance;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 20f;

            if (EnableCustomRenderDistance)
            {
                GUI.Label(new Rect(startX, y, width, 18), $"Draw Distance: {(int)RenderDistance}m");
                y += 16f;
                float newDist = GUI.HorizontalSlider(new Rect(startX, y, width, 15), RenderDistance, 50f, 3000f);
                if (Mathf.Abs(newDist - RenderDistance) > 1f)
                {
                    RenderDistance = newDist;
                    configRenderDistance.Value = RenderDistance;
                    configCategory.SaveToFile();
                    ApplyGraphicsSettings();
                }
                y += 20f;
            }

            // Custom Shadow Distance
            bool newCustomShadow = GUI.Toggle(new Rect(startX, y, width, 20), EnableCustomShadowDistance, " Enable Custom Shadow Distance");
            if (newCustomShadow != EnableCustomShadowDistance)
            {
                EnableCustomShadowDistance = newCustomShadow;
                configEnableCustomShadowDistance.Value = EnableCustomShadowDistance;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 20f;

            if (EnableCustomShadowDistance)
            {
                GUI.Label(new Rect(startX, y, width, 18), $"Shadow Distance: {(int)ShadowDistance}m");
                y += 16f;
                float newShadow = GUI.HorizontalSlider(new Rect(startX, y, width, 15), ShadowDistance, 0f, 500f);
                if (Mathf.Abs(newShadow - ShadowDistance) > 1f)
                {
                    ShadowDistance = newShadow;
                    configShadowDistance.Value = ShadowDistance;
                    configCategory.SaveToFile();
                    ApplyGraphicsSettings();
                }
                y += 20f;
            }

            // Hide Hands
            bool newHands = GUI.Toggle(new Rect(startX, y, width, 20), HideHands, " Hide First-Person Hands");
            if (newHands != HideHands)
            {
                HideHands = newHands;
                configHideHands.Value = HideHands;
                configCategory.SaveToFile();
                ApplyHandsSettings();
            }
            y += 22f;

            // HideGameHUD
            bool newHUD = GUI.Toggle(new Rect(startX, y, width, 20), HideGameHUD, " Hide Native Game HUD");
            if (newHUD != HideGameHUD)
            {
                HideGameHUD = newHUD;
                configHideGameHUD.Value = HideGameHUD;
                configCategory.SaveToFile();
                ApplyGameHUDSettings();
            }
            y += 24f;

            // QualitySettings
            string qualityText = TextureQuality == 0 ? "High (Default)" : (TextureQuality == 1 ? "Medium (1/2)" : "Low / Potato (1/4)");
            if (GUI.Button(new Rect(startX, y, width, 22), $"Textures: [{qualityText}]"))
            {
                TextureQuality = (TextureQuality + 1) % 3;
                configTextureQuality.Value = TextureQuality;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 26f;

            // ForceApply
            if (GUI.Button(new Rect(startX, y, width, 22), "🔄 Force Apply All Settings"))
            {
                ApplyGraphicsSettings();
            }
        }
    }
}
