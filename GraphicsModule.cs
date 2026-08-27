using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpeedrunToolkitMod
{
    public class GraphicsModule
    {
        public bool DisablePostProcessing = false;
        public bool DisableVSync = false;
        public int TargetFPS = -1; // -1 означает "Без ограничений" (Uncapped)
        private MelonPreferences_Entry<int> configTargetFPS;

        private string fpsInputBuffer = "-1"; // Буфер для текстового поля UI
        private MelonPreferences_Entry<bool> configDisableVSync;
        public bool DisableShadows = false;
        public bool HideHands = false;
        public bool HideGameHUD = false;
        public int TextureQuality = 0; // 0 = High, 1 = Medium, 2 = Low

        // Dark Mode
        public bool EnableDarkMode = false;
        private Color defaultAmbientLight = Color.white;
        private AmbientMode defaultAmbientMode = AmbientMode.Skybox;
        private float defaultAmbientIntensity = 1f;
        private bool hasCapturedAmbient = false;

        // Skybox Management
        private Material originalSkyboxMaterial;
        private List<string> availableSkyboxNames = new List<string>();

        // Custom Skyboxes Folder & Cache
        private List<string> customImageFiles = new List<string>();
        private Dictionary<string, Material> customMaterialsCache = new Dictionary<string, Material>();

        // Index system:
        // 0 = Map Default
        // 1 .. nativeCount = Native Skyboxes
        // nativeCount + 1 = Solid Black
        // nativeCount + 2 .. N = Custom Images
        public int CurrentSkyboxIndex = 0;

        // Distance Settings
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
        private MelonPreferences_Entry<bool> configDarkMode;

        private MelonPreferences_Entry<bool> configEnableCustomRenderDistance;
        private MelonPreferences_Entry<float> configRenderDistance;
        private MelonPreferences_Entry<bool> configEnableCustomShadowDistance;
        private MelonPreferences_Entry<float> configShadowDistance;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("GraphicsMod", "Graphics Settings");
            configDisablePP = configCategory.CreateEntry("DisablePostProcessing", false, "Disable Post Processing");
            configDisableVSync = configCategory.CreateEntry("DisableVSync", false, "Disable V-Sync");
            DisableVSync = configDisableVSync.Value;
            configTargetFPS = configCategory.CreateEntry("TargetFPS", -1, "Target Frame Rate (-1 for Uncapped)");
            TargetFPS = configTargetFPS.Value;
            fpsInputBuffer = TargetFPS.ToString();
            configDisableShadows = configCategory.CreateEntry("DisableShadows", false, "Disable Shadows");
            configHideHands = configCategory.CreateEntry("HideHands", false, "Hide First Person Hands");
            configHideGameHUD = configCategory.CreateEntry("HideGameHUD", false, "Hide Native Game HUD");
            configTextureQuality = configCategory.CreateEntry("TextureQuality", 0, "Texture Quality Limit");
            configDarkMode = configCategory.CreateEntry("DarkMode", false, "Enable Dark Mode");

            configEnableCustomRenderDistance = configCategory.CreateEntry("EnableCustomRenderDistance", false, "Enable Custom Render Distance");
            configRenderDistance = configCategory.CreateEntry("RenderDistance", 1000f, "Render Distance");
            configEnableCustomShadowDistance = configCategory.CreateEntry("EnableCustomShadowDistance", false, "Enable Custom Shadow Distance");
            configShadowDistance = configCategory.CreateEntry("ShadowDistance", 150f, "Shadow Distance");

            DisablePostProcessing = configDisablePP.Value;
            DisableShadows = configDisableShadows.Value;
            HideHands = configHideHands.Value;
            HideGameHUD = configHideGameHUD.Value;
            TextureQuality = Mathf.Clamp(configTextureQuality.Value, 0, 2);
            EnableDarkMode = configDarkMode.Value;

            EnableCustomRenderDistance = configEnableCustomRenderDistance.Value;
            RenderDistance = configRenderDistance.Value;
            EnableCustomShadowDistance = configEnableCustomShadowDistance.Value;
            ShadowDistance = configShadowDistance.Value;

            EnsureCustomFolderExists();
            ScanCustomSkyboxes();

            MelonLogger.Msg("[Graphics] Module initialized.");
        }

        private void EnsureCustomFolderExists()
        {
            string folder = GetCustomSkyboxFolderPath();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                MelonLogger.Msg($"[Graphics] Created custom skybox folder: {folder}");
            }
        }

        private string GetCustomSkyboxFolderPath()
        {
            return Path.Combine(Application.dataPath, "../UserData/Skyboxes");
        }

        public void ScanCustomSkyboxes()
        {
            customImageFiles.Clear();
            string folder = GetCustomSkyboxFolderPath();
            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "*.*")
                    .Where(f => f.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase));

                customImageFiles.AddRange(files);
                MelonLogger.Msg($"[Graphics] Found {customImageFiles.Count} custom skybox image(s).");
            }
        }

        private void EnsureSkyboxNamesLoaded()
        {
            if (availableSkyboxNames.Count == 0)
            {
                var materials = Resources.LoadAll<Material>("Skyboxes");
                if (materials != null && materials.Length > 0)
                {
                    foreach (var mat in materials)
                    {
                        if (mat != null && !string.IsNullOrEmpty(mat.name))
                        {
                            if (!availableSkyboxNames.Contains(mat.name))
                            {
                                availableSkyboxNames.Add(mat.name);
                            }
                        }
                    }
                    MelonLogger.Msg($"[Graphics] Loaded {availableSkyboxNames.Count} native skybox names!");
                }
            }
        }

        private Material GetOrCreateCustomMaterial(string filePath)
        {
            if (customMaterialsCache.ContainsKey(filePath) && customMaterialsCache[filePath] != null)
            {
                return customMaterialsCache[filePath];
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                tex.LoadImage(bytes);
                tex.wrapMode = TextureWrapMode.Clamp;

                Shader targetShader = null;

                // 1. Ищем панорамный шейдер в памяти игры среди всех загруженных
                var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
                foreach (var s in allShaders)
                {
                    if (s != null && (s.name.Contains("Panoramic") || s.name.Contains("Equirect") || s.name.Contains("Sphere")))
                    {
                        targetShader = s;
                        break;
                    }
                }

                // 2. Стандартные попытки поиска
                if (targetShader == null) targetShader = Shader.Find("Skybox/Panoramic");

                bool is6Sided = false;
                if (targetShader == null)
                {
                    targetShader = Shader.Find("Skybox/6 Sided");
                    is6Sided = true;
                }

                if (targetShader == null && originalSkyboxMaterial != null)
                {
                    targetShader = originalSkyboxMaterial.shader;
                }

                if (targetShader != null)
                {
                    Material mat = new Material(targetShader);

                    if (is6Sided)
                    {
                        // Заполняем все 6 граней картинкой, чтобы Skybox/6 Sided её отрисовал
                        mat.SetTexture("_FrontTex", tex);
                        mat.SetTexture("_BackTex", tex);
                        mat.SetTexture("_LeftTex", tex);
                        mat.SetTexture("_RightTex", tex);
                        mat.SetTexture("_UpTex", tex);
                        mat.SetTexture("_DownTex", tex);
                    }
                    else
                    {
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                        if (mat.HasProperty("_Tex")) mat.SetTexture("_Tex", tex);
                        if (mat.HasProperty("_FrontTex")) mat.SetTexture("_FrontTex", tex);
                    }

                    customMaterialsCache[filePath] = mat;
                    MelonLogger.Msg($"[Graphics] Applied custom skybox with shader: {targetShader.name}");
                    return mat;
                }
                else
                {
                    MelonLogger.Error("[Graphics] Couldn't find any skybox shader in game memory!");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to load custom skybox ({Path.GetFileName(filePath)}): {ex.Message}");
            }

            return null;
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

                var ppLayer = mainCam.GetComponent<PostProcessLayer>();
                if (ppLayer != null)
                {
                    ppLayer.enabled = !DisablePostProcessing;
                }

                if (EnableCustomRenderDistance)
                {
                    mainCam.farClipPlane = RenderDistance;
                }
                else
                {
                    mainCam.farClipPlane = defaultFarClip;
                }
            }

            QualitySettings.shadows = DisableShadows ? ShadowQuality.Disable : ShadowQuality.All;

            // 0 = V-Sync отключен, 1 = включен (60/144 FPS)
            QualitySettings.vSyncCount = DisableVSync ? 0 : 1;

            if (DisableVSync)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = TargetFPS; // Применяем наш лимит
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1; // Если V-Sync включен, отдаем управление ему
            }

            if (EnableCustomShadowDistance)
            {
                QualitySettings.shadowDistance = ShadowDistance;
            }

            QualitySettings.masterTextureLimit = TextureQuality;

            ApplySkyboxLogic();
            ApplyDarkModeLogic();
            ApplyHandsSettings();
            ApplyGameHUDSettings();
        }

        private void ApplySkyboxLogic()
        {
            if (CurrentSkyboxIndex == 0 && RenderSettings.skybox != null && !customMaterialsCache.ContainsValue(RenderSettings.skybox))
            {
                originalSkyboxMaterial = RenderSettings.skybox;
            }

            EnsureSkyboxNamesLoaded();
            Camera mainCam = Camera.main;

            int nativeCount = availableSkyboxNames.Count;
            int idxBlack = nativeCount + 1;

            if (CurrentSkyboxIndex == 0) // Map Default
            {
                if (mainCam != null) mainCam.clearFlags = CameraClearFlags.Skybox;
                if (originalSkyboxMaterial != null) RenderSettings.skybox = originalSkyboxMaterial;
            }
            else if (CurrentSkyboxIndex >= 1 && CurrentSkyboxIndex <= nativeCount) // Native
            {
                if (mainCam != null) mainCam.clearFlags = CameraClearFlags.Skybox;
                string skyName = availableSkyboxNames[CurrentSkyboxIndex - 1];
                Material loadedMat = Resources.Load<Material>("Skyboxes/" + skyName);
                if (loadedMat != null) RenderSettings.skybox = loadedMat;
            }
            else if (CurrentSkyboxIndex == idxBlack) // Solid Black
            {
                if (mainCam != null)
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = Color.black;
                }
                RenderSettings.skybox = null;
            }
            else // Custom Images
            {
                int customIdx = CurrentSkyboxIndex - (idxBlack + 1);
                if (customIdx >= 0 && customIdx < customImageFiles.Count)
                {
                    if (mainCam != null) mainCam.clearFlags = CameraClearFlags.Skybox;
                    Material customMat = GetOrCreateCustomMaterial(customImageFiles[customIdx]);
                    if (customMat != null) RenderSettings.skybox = customMat;
                }
            }

            DynamicGI.UpdateEnvironment();
        }

        private void ApplyDarkModeLogic()
        {
            Color darkColor = new Color(0.04f, 0.04f, 0.07f);

            if (EnableDarkMode)
            {
                if (!hasCapturedAmbient && RenderSettings.ambientLight != darkColor)
                {
                    defaultAmbientLight = RenderSettings.ambientLight;
                    defaultAmbientMode = RenderSettings.ambientMode;
                    defaultAmbientIntensity = RenderSettings.ambientIntensity;
                    hasCapturedAmbient = true;
                }

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = darkColor;
            }
            else
            {
                if (hasCapturedAmbient)
                {
                    RenderSettings.ambientMode = defaultAmbientMode;
                    RenderSettings.ambientLight = defaultAmbientLight;
                    RenderSettings.ambientIntensity = defaultAmbientIntensity;
                    hasCapturedAmbient = false;
                }
            }

            DynamicGI.UpdateEnvironment();
        }

        public void NextSkybox()
        {
            EnsureSkyboxNamesLoaded();
            int total = availableSkyboxNames.Count + 1 + 1 + customImageFiles.Count; // Default + Native + Black + Custom Files
            CurrentSkyboxIndex = (CurrentSkyboxIndex + 1) % total;
            ApplyGraphicsSettings();
        }

        public void PreviousSkybox()
        {
            EnsureSkyboxNamesLoaded();
            int total = availableSkyboxNames.Count + 1 + 1 + customImageFiles.Count;
            CurrentSkyboxIndex = (CurrentSkyboxIndex - 1 + total) % total;
            ApplyGraphicsSettings();
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

            // Dark Mode
            bool newDarkMode = GUI.Toggle(new Rect(startX, y, width, 20), EnableDarkMode, " Enable Dark Mode");
            if (newDarkMode != EnableDarkMode)
            {
                EnableDarkMode = newDarkMode;
                configDarkMode.Value = EnableDarkMode;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 22f;

            // Skybox Selector Controls
            EnsureSkyboxNamesLoaded();
            string skyName = "Map Default";
            int nativeCount = availableSkyboxNames.Count;
            int idxBlack = nativeCount + 1;

            if (CurrentSkyboxIndex > 0 && CurrentSkyboxIndex <= nativeCount)
            {
                skyName = $"[{CurrentSkyboxIndex}] {availableSkyboxNames[CurrentSkyboxIndex - 1]}";
            }
            else if (CurrentSkyboxIndex == idxBlack)
            {
                skyName = $"⬛ Solid Black";
            }
            else if (CurrentSkyboxIndex > idxBlack)
            {
                int customIdx = CurrentSkyboxIndex - (idxBlack + 1);
                if (customIdx < customImageFiles.Count)
                {
                    string fileName = Path.GetFileName(customImageFiles[customIdx]);
                    skyName = $"🖼️ [{customIdx + 1}/{customImageFiles.Count}] {fileName}";
                }
            }

            GUI.Label(new Rect(startX, y, width, 18), $"Skybox: <b>{skyName}</b>");
            y += 20f;

            float btnWidth = (width - 10f) / 2f;
            if (GUI.Button(new Rect(startX, y, btnWidth, 22), "◄ Previous Sky"))
            {
                PreviousSkybox();
            }
            if (GUI.Button(new Rect(startX + btnWidth + 10f, y, btnWidth, 22), "Next Sky ►"))
            {
                NextSkybox();
            }
            y += 26f;

            // Refresh Custom Skyboxes Button
            if (GUI.Button(new Rect(startX, y, width, 22), "📁 Rescan Custom Skyboxes Folder"))
            {
                ScanCustomSkyboxes();
            }
            y += 26f;

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

            GUI.Label(new Rect(startX, y, 100, 20), "FPS Limit:");

            // Текстовое поле для ввода своего числа
            fpsInputBuffer = GUI.TextField(new Rect(startX + 75, y, 50, 20), fpsInputBuffer);

            // Кнопка применить для поля ввода
            if (GUI.Button(new Rect(startX + 130, y, 50, 20), "Set"))
            {
                if (int.TryParse(fpsInputBuffer, out int parsedFPS))
                {
                    TargetFPS = parsedFPS;
                    configTargetFPS.Value = TargetFPS;
                    configCategory.SaveToFile();
                    ApplyGraphicsSettings();
                }
            }

            y += 25f;

            // Быстрые кнопки-пресеты
            int[] presets = new int[] { -1, 60, 120, 144, 240, 360 };
            float btnX = startX;

            foreach (int fps in presets)
            {
                string label = fps == -1 ? "Max" : fps.ToString();
                if (GUI.Button(new Rect(btnX, y, 45, 20), label))
                {
                    TargetFPS = fps;
                    fpsInputBuffer = fps.ToString();
                    configTargetFPS.Value = TargetFPS;
                    configCategory.SaveToFile();
                    ApplyGraphicsSettings();
                }
                btnX += 50f;
            }
            y += 25f;

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