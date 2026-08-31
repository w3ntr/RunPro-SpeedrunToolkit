using MelonLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace SpeedrunToolkitMod
{
    public class GraphicsModule
    {
        private int originalWidth = Screen.width;
        private int originalHeight = Screen.height;
        private Vector2 scrollPosition = Vector2.zero;
        private bool isPotatoMode = false;
        private List<Light> disabledLights = new List<Light>();

        // Кэш для надежного восстановления исходных материалов карты
        private Dictionary<Renderer, Material[]> savedOriginalMaterials = new Dictionary<Renderer, Material[]>();

        public bool DisablePostProcessing = false;
        public bool DisableVSync = false;

        // Чистый C# каллер без MonoBehaviour для совместимости с IL2CPP
        private DistanceCuller cullerComponent = new DistanceCuller();

        public int TargetFPS = -1;
        private MelonPreferences_Entry<int> configTargetFPS;

        private string fpsInputBuffer = "-1";
        private MelonPreferences_Entry<bool> configDisableVSync;
        public bool DisableShadows = false;
        public bool HideHands = false;
        public bool HideGameHUD = false;
        public int TextureQuality = 0;

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

        public void OnUpdate()
        {
            cullerComponent?.Update();
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

                var allShaders = Resources.FindObjectsOfTypeAll<Shader>();
                foreach (var s in allShaders)
                {
                    if (s != null && (s.name.Contains("Panoramic") || s.name.Contains("Equirect") || s.name.Contains("Sphere")))
                    {
                        targetShader = s;
                        break;
                    }
                }

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

            QualitySettings.vSyncCount = DisableVSync ? 0 : 1;

            if (DisableVSync)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = TargetFPS;
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
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

            if (CurrentSkyboxIndex == 0)
            {
                if (mainCam != null) mainCam.clearFlags = CameraClearFlags.Skybox;
                if (originalSkyboxMaterial != null) RenderSettings.skybox = originalSkyboxMaterial;
            }
            else if (CurrentSkyboxIndex >= 1 && CurrentSkyboxIndex <= nativeCount)
            {
                if (mainCam != null) mainCam.clearFlags = CameraClearFlags.Skybox;
                string skyName = availableSkyboxNames[CurrentSkyboxIndex - 1];
                Material loadedMat = Resources.Load<Material>("Skyboxes/" + skyName);
                if (loadedMat != null) RenderSettings.skybox = loadedMat;
            }
            else if (CurrentSkyboxIndex == idxBlack)
            {
                if (mainCam != null)
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = Color.black;
                }
                RenderSettings.skybox = null;
            }
            else
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

        public void ApplyUltraPotatoMode()
        {
            isPotatoMode = true;
            disabledLights.Clear();

            AnnihilateParticles();
            ApplyUnlitFlatShaders();

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
                Camera.main.farClipPlane = 80f;
            }
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            QualitySettings.masterTextureLimit = 8;
            QualitySettings.pixelLightCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = false;
            QualitySettings.softVegetation = false;
            QualitySettings.particleRaycastBudget = 0;
            QualitySettings.lodBias = 0.01f;

            foreach (var light in Object.FindObjectsOfType<Light>())
            {
                if (light != null && light.enabled)
                {
                    disabledLights.Add(light);
                    light.enabled = false;
                }
            }

            // Distance Culling без AddComponent
            var list = new List<Renderer>();
            foreach (var rend in savedOriginalMaterials.Keys)
            {
                if (rend != null) list.Add(rend);
            }
            cullerComponent.SetTargetRenderers(list.ToArray());
            cullerComponent.IsEnabled = true;

            QualitySettings.vSyncCount = 0;
            QualitySettings.maxQueuedFrames = 0;
            Application.targetFrameRate = -1;
        }

        private Shader GetSafeFlatShader()
        {
            Shader shader = Shader.Find("Unlit/Color")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Legacy Shaders/Diffuse");

            if (shader == null)
            {
                var anyRenderer = Object.FindObjectOfType<MeshRenderer>();
                if (anyRenderer != null && anyRenderer.sharedMaterial != null)
                {
                    shader = anyRenderer.sharedMaterial.shader;
                }
            }

            return shader;
        }

        public void RestoreDefaultGraphics()
        {
            isPotatoMode = false;

            cullerComponent.RestoreAllRenderers();
            cullerComponent.IsEnabled = false;

            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;
                Camera.main.farClipPlane = 1000f;
            }
            RenderSettings.fog = true;

            foreach (var light in disabledLights)
            {
                if (light != null) light.enabled = true;
            }
            disabledLights.Clear();

            RestoreOriginalShaders();
            ApplyGraphicsSettings();
        }

        public void ApplyPotatoFullscreen()
        {
            if (Screen.width > 600) originalWidth = Screen.width;
            if (Screen.height > 400) originalHeight = Screen.height;

            Screen.SetResolution(480, 270, FullScreenMode.ExclusiveFullScreen);
        }

        public void ApplyPotatoWindowed()
        {
            if (Screen.width > 600) originalWidth = Screen.width;
            if (Screen.height > 400) originalHeight = Screen.height;

            Screen.SetResolution(480, 270, false);
        }

        public void ApplyPotato320x240Full()
        {
            Screen.SetResolution(320, 240, FullScreenMode.ExclusiveFullScreen);
        }

        public void ApplyPotato320x180Full()
        {
            Screen.SetResolution(320, 180, FullScreenMode.ExclusiveFullScreen);
        }

        public void RestoreNativeResolution()
        {
            Screen.SetResolution(originalWidth, originalHeight, FullScreenMode.FullScreenWindow);
        }

        public void AnnihilateParticles()
        {
            foreach (var ps in Object.FindObjectsOfType<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        public void ApplyUnlitFlatShaders()
        {
            Shader flatShader = GetSafeFlatShader();
            if (flatShader == null) return;

            Material flatMaterial = new Material(flatShader);
            if (flatMaterial.HasProperty("_Color"))
            {
                flatMaterial.color = new Color(0.65f, 0.65f, 0.65f, 1f);
            }

            Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;

                string name = r.gameObject.name.ToLower();
                if (name.Contains("player") || name.Contains("canvas") || name.Contains("hud") || name.Contains("sky")) continue;

                if (!savedOriginalMaterials.ContainsKey(r))
                {
                    savedOriginalMaterials[r] = r.sharedMaterials;
                }

                Material[] flatMaterialsArray = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < flatMaterialsArray.Length; i++)
                {
                    flatMaterialsArray[i] = flatMaterial;
                }

                r.materials = flatMaterialsArray;
            }
        }

        public void RestoreOriginalShaders()
        {
            foreach (var pair in savedOriginalMaterials)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    pair.Key.materials = pair.Value;
                }
            }
            savedOriginalMaterials.Clear();
        }

        public class WireframeController : MonoBehaviour
        {
            public static bool ShowWireframe = false;

            void OnPreRender()
            {
                if (ShowWireframe) GL.wireframe = true;
            }

            void OnPostRender()
            {
                if (ShowWireframe) GL.wireframe = false;
            }
        }

        public void ToggleWireframe()
        {
            WireframeController.ShowWireframe = !WireframeController.ShowWireframe;
        }

        public void NextSkybox()
        {
            EnsureSkyboxNamesLoaded();
            int total = availableSkyboxNames.Count + 1 + 1 + customImageFiles.Count;
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
            scrollPosition = GUI.BeginScrollView(
                new Rect(startX, startY, width, 310f),
                scrollPosition,
                new Rect(0, 0, width - 20f, 580f)
            );

            float y = 5f;
            float contentWidth = width - 25f;

            string potatoBtnText = isPotatoMode ? "⚡ RESTORE NORMAL GRAPHICS" : "🔥 ENABLE ULTRA POTATO MODE";
            if (GUI.Button(new Rect(0, y, contentWidth, 25f), potatoBtnText))
            {
                if (isPotatoMode)
                {
                    RestoreDefaultGraphics();
                }
                else
                {
                    ApplyUltraPotatoMode();
                }
            }
            y += 30f;

            GUI.Label(new Rect(0, y, contentWidth, 18f), "<b>Potato Resolution Presets:</b>");
            y += 20f;

            float thirdWidth = (contentWidth - 10f) / 3f;
            if (GUI.Button(new Rect(0, y, thirdWidth, 22f), "🖥️ 480p Full"))
            {
                ApplyPotatoFullscreen();
            }
            if (GUI.Button(new Rect(thirdWidth + 5f, y, thirdWidth, 22f), "🔲 480p Window"))
            {
                ApplyPotatoWindowed();
            }
            if (GUI.Button(new Rect((thirdWidth * 2f) + 10f, y, thirdWidth, 22f), "🔄 Native Res"))
            {
                RestoreNativeResolution();
            }
            y += 28f;

            GUI.Label(new Rect(0, y, contentWidth, 18f), "<b>Extreme Pixel Resolutions:</b>");
            y += 20f;

            float halfWidth = (contentWidth - 5f) / 2f;

            if (GUI.Button(new Rect(0, y, halfWidth, 22f), "👾 320x240 (4:3)"))
            {
                ApplyPotato320x240Full();
            }

            if (GUI.Button(new Rect(halfWidth + 5f, y, halfWidth, 22f), "👾 320x180 (16:9)"))
            {
                ApplyPotato320x180Full();
            }
            y += 26f;

            GUI.Label(new Rect(0, y, contentWidth, 20f), "<b>Graphics & View Settings</b>");
            y += 24f;

            bool newDarkMode = GUI.Toggle(new Rect(0, y, contentWidth, 20f), EnableDarkMode, " Enable Dark Mode");
            if (newDarkMode != EnableDarkMode)
            {
                EnableDarkMode = newDarkMode;
                configDarkMode.Value = EnableDarkMode;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 22f;

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

            GUI.Label(new Rect(0, y, contentWidth, 18f), $"Skybox: <b>{skyName}</b>");
            y += 20f;

            float skyBtnWidth = (contentWidth - 10f) / 2f;
            if (GUI.Button(new Rect(0, y, skyBtnWidth, 22f), "◄ Previous Sky"))
            {
                PreviousSkybox();
            }
            if (GUI.Button(new Rect(skyBtnWidth + 10f, y, skyBtnWidth, 22f), "Next Sky ►"))
            {
                NextSkybox();
            }
            y += 26f;

            if (GUI.Button(new Rect(0, y, contentWidth, 22f), "📁 Rescan Custom Skyboxes Folder"))
            {
                ScanCustomSkyboxes();
            }
            y += 26f;

            bool newPP = GUI.Toggle(new Rect(0, y, contentWidth, 20f), DisablePostProcessing, " Disable Post-Processing (Bloom, FX)");
            if (newPP != DisablePostProcessing)
            {
                DisablePostProcessing = newPP;
                configDisablePP.Value = DisablePostProcessing;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 22f;

            GUI.Label(new Rect(0, y, 70f, 20f), "FPS Limit:");
            fpsInputBuffer = GUI.TextField(new Rect(75f, y, 50f, 20f), fpsInputBuffer);
            if (GUI.Button(new Rect(130f, y, 50f, 20f), "Set"))
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

            int[] presets = new int[] { -1, 60, 120, 144, 240, 360 };
            float btnX = 0f;
            float presetBtnWidth = (contentWidth - 25f) / 6f;

            foreach (int fps in presets)
            {
                string label = fps == -1 ? "Max" : fps.ToString();
                if (GUI.Button(new Rect(btnX, y, presetBtnWidth, 20f), label))
                {
                    TargetFPS = fps;
                    fpsInputBuffer = fps.ToString();
                    configTargetFPS.Value = TargetFPS;
                    configCategory.SaveToFile();
                    ApplyGraphicsSettings();
                }
                btnX += presetBtnWidth + 5f;
            }
            y += 25f;

            bool newShadows = GUI.Toggle(new Rect(0, y, contentWidth, 20f), DisableShadows, " Disable Shadows (FPS Boost)");
            if (newShadows != DisableShadows)
            {
                DisableShadows = newShadows;
                configDisableShadows.Value = DisableShadows;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 22f;

            bool newCustomDist = GUI.Toggle(new Rect(0, y, contentWidth, 20f), EnableCustomRenderDistance, " Enable Custom Draw Distance");
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
                GUI.Label(new Rect(0, y, contentWidth, 18f), $"Draw Distance: {(int)RenderDistance}m");
                y += 16f;
                float newDist = GUI.HorizontalSlider(new Rect(0, y, contentWidth, 15f), RenderDistance, 50f, 3000f);
                if (Mathf.Abs(newDist - RenderDistance) > 1f)
                {
                    RenderDistance = newDist;
                    configRenderDistance.Value = RenderDistance;
                    ApplyGraphicsSettings();
                }
                y += 20f;
            }

            bool newCustomShadow = GUI.Toggle(new Rect(0, y, contentWidth, 20f), EnableCustomShadowDistance, " Enable Custom Shadow Distance");
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
                GUI.Label(new Rect(0, y, contentWidth, 18f), $"Shadow Distance: {(int)ShadowDistance}m");
                y += 16f;
                float newShadow = GUI.HorizontalSlider(new Rect(0, y, contentWidth, 15f), ShadowDistance, 0f, 500f);
                if (Mathf.Abs(newShadow - ShadowDistance) > 1f)
                {
                    ShadowDistance = newShadow;
                    configShadowDistance.Value = ShadowDistance;
                    ApplyGraphicsSettings();
                }
                y += 20f;
            }

            bool newHands = GUI.Toggle(new Rect(0, y, contentWidth, 20f), HideHands, " Hide First-Person Hands");
            if (newHands != HideHands)
            {
                HideHands = newHands;
                configHideHands.Value = HideHands;
                configCategory.SaveToFile();
                ApplyHandsSettings();
            }
            y += 22f;

            bool newHUD = GUI.Toggle(new Rect(0, y, contentWidth, 20f), HideGameHUD, " Hide Native Game HUD");
            if (newHUD != HideGameHUD)
            {
                HideGameHUD = newHUD;
                configHideGameHUD.Value = HideGameHUD;
                configCategory.SaveToFile();
                ApplyGameHUDSettings();
            }
            y += 24f;

            string qualityText = TextureQuality == 0 ? "High (Default)" : (TextureQuality == 1 ? "Medium (1/2)" : "Low / Potato (1/4)");
            if (GUI.Button(new Rect(0, y, contentWidth, 22f), $"Textures: [{qualityText}]"))
            {
                TextureQuality = (TextureQuality + 1) % 3;
                configTextureQuality.Value = TextureQuality;
                configCategory.SaveToFile();
                ApplyGraphicsSettings();
            }
            y += 26f;

            if (GUI.Button(new Rect(0, y, contentWidth, 22f), "🔄 Force Apply All Settings"))
            {
                ApplyGraphicsSettings();
            }

            GUI.EndScrollView();
        }
    }

    public class DistanceCuller
    {
        public float CullDistance = 50f;
        public bool IsEnabled = false;
        private Renderer[] renderersToCull;
        private float lastCheckTime;

        public void SetTargetRenderers(Renderer[] renderers)
        {
            renderersToCull = renderers;
        }

        public void Update()
        {
            if (!IsEnabled || renderersToCull == null) return;
            if (Time.unscaledTime - lastCheckTime < 0.2f) return;
            lastCheckTime = Time.unscaledTime;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 camPos = mainCam.transform.position;
            float sqrDist = CullDistance * CullDistance;

            foreach (var r in renderersToCull)
            {
                if (r == null) continue;
                r.enabled = (r.transform.position - camPos).sqrMagnitude <= sqrDist;
            }
        }

        public void RestoreAllRenderers()
        {
            if (renderersToCull == null) return;
            foreach (var r in renderersToCull)
            {
                if (r != null) r.enabled = true;
            }
        }
    }
}