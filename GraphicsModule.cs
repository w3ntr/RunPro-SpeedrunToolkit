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
        public int TextureQuality = 0; // 0 = High, 1 = Medium, 2 = Low (Potato) (That's work only on tree and planks)

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<bool> configDisablePP;
        private MelonPreferences_Entry<bool> configDisableShadows;
        private MelonPreferences_Entry<bool> configHideHands;
        private MelonPreferences_Entry<bool> configHideGameHUD;
        private MelonPreferences_Entry<int> configTextureQuality;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("GraphicsMod", "Graphics Settings");
            configDisablePP = configCategory.CreateEntry("DisablePostProcessing", false, "Disable Post Processing");
            configDisableShadows = configCategory.CreateEntry("DisableShadows", false, "Disable Shadows");
            configHideHands = configCategory.CreateEntry("HideHands", false, "Hide First Person Hands");
            configHideGameHUD = configCategory.CreateEntry("HideGameHUD", false, "Hide Native Game HUD");
            configTextureQuality = configCategory.CreateEntry("TextureQuality", 0, "Texture Quality Limit");

            DisablePostProcessing = configDisablePP.Value;
            DisableShadows = configDisableShadows.Value;
            HideHands = configHideHands.Value;
            HideGameHUD = configHideGameHUD.Value;
            TextureQuality = Mathf.Clamp(configTextureQuality.Value, 0, 2);

            MelonLogger.Msg("[Graphics] Module initialized.");
        }

        public void ApplyGraphicsSettings()
        {
            // 1. Пост-обработка (Bloom, Motion Effects)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var ppLayer = mainCam.GetComponent<PostProcessLayer>();
                if (ppLayer != null)
                {
                    ppLayer.enabled = !DisablePostProcessing;
                }
            }

            // 2. Shadows
            QualitySettings.shadows = DisableShadows ? ShadowQuality.Disable : ShadowQuality.All;

            // 3. Quality
            QualitySettings.masterTextureLimit = TextureQuality;

            // 4. HideGameHUD and Hands
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
            y += 26f;

            // QualitySettings
            string qualityText = TextureQuality == 0 ? "High (Default)" : (TextureQuality == 1 ? "Medium (1/2)" : "Low / Potato (1/4)");
            if (GUI.Button(new Rect(startX, y, width, 24), $"Textures: [{qualityText}]"))
            {
                TextureQuality = (TextureQuality + 1) % 3;
                configTextureQuality.Value = TextureQuality;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 28f;

            // ForceApply
            if (GUI.Button(new Rect(startX, y, width, 24), "🔄 Force Apply All Settings"))
            {
                ApplyGraphicsSettings();
            }
        }
    }
}