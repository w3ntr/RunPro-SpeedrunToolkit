using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SpeedrunToolkitMod.Main), "Speedrun Toolkit", "3.3.1", "w3ntr")]
[assembly: MelonGame(null, null)]

namespace SpeedrunToolkitMod
{
    public class Main : MelonMod
    {
        private PracticeModule practiceModule;
        private SpeedometerModule speedoModule;
        private DeathZoneVisualizerModule deathZoneModule;
        private MusicReplacerModule musicModule = new MusicReplacerModule();
        private FovChangerModule fovModule = new FovChangerModule();
        private InputOverlayModule inputModule = new InputOverlayModule();

        private bool showMenu = false;
        private int selectedTab = 0; // 0 = Practice, 1 = HUD, 2 = Death Zones

        // Keybinding settings
        private KeyCode menuKey = KeyCode.F8;
        private KeyCode savePosKey = KeyCode.F9;
        private KeyCode loadPosKey = KeyCode.F10;
        private KeyCode spawnPosKey = KeyCode.F11;
        private KeyCode restartKey = KeyCode.R;

        private int activeRebindIndex = -1; // -1 = none, 0 = Menu, 1 = Save, 2 = Load, 3 = Spawn, 4 = Restart

        private MelonPreferences_Category prefCategory;
        private MelonPreferences_Entry<KeyCode> prefMenuKey;
        private MelonPreferences_Entry<KeyCode> prefSavePosKey;
        private MelonPreferences_Entry<KeyCode> prefLoadPosKey;
        private MelonPreferences_Entry<KeyCode> prefSpawnPosKey;
        private MelonPreferences_Entry<KeyCode> prefRestartKey;

        // HUD Prefs
        private MelonPreferences_Entry<bool> prefHudEnabled;
        private MelonPreferences_Entry<bool> prefShowSpeed;
        private MelonPreferences_Entry<bool> prefShowCoords;
        private MelonPreferences_Entry<bool> prefHideNativeSpeedo;
        private MelonPreferences_Entry<float> prefHudX;
        private MelonPreferences_Entry<float> prefHudY;
        private MelonPreferences_Entry<int> prefFontSize;
        private MelonPreferences_Entry<int> prefFontStyle;
        private MelonPreferences_Entry<int> prefColorIndex;
        private MelonPreferences_Entry<float> prefBgOpacity;

        public override void OnInitializeMelon()
        {
            prefCategory = MelonPreferences.CreateCategory("SpeedrunToolkit", "Speedrun Toolkit Settings");
            prefMenuKey = prefCategory.CreateEntry("MenuKey", KeyCode.F8);
            prefSavePosKey = prefCategory.CreateEntry("SavePosKey", KeyCode.F9);
            prefLoadPosKey = prefCategory.CreateEntry("LoadPosKey", KeyCode.F10);
            prefSpawnPosKey = prefCategory.CreateEntry("SpawnPosKey", KeyCode.F11);
            prefRestartKey = prefCategory.CreateEntry("RestartKey", KeyCode.R);

            prefHudEnabled = prefCategory.CreateEntry("HudEnabled", true);
            prefShowSpeed = prefCategory.CreateEntry("ShowSpeed", true);
            prefShowCoords = prefCategory.CreateEntry("ShowCoords", true);
            prefHideNativeSpeedo = prefCategory.CreateEntry("HideNativeSpeedo", true);
            prefHudX = prefCategory.CreateEntry("HudX", 20f);
            prefHudY = prefCategory.CreateEntry("HudY", 60f);
            prefFontSize = prefCategory.CreateEntry("FontSize", 14);
            prefFontStyle = prefCategory.CreateEntry("FontStyle", (int)FontStyle.Bold);
            prefColorIndex = prefCategory.CreateEntry("ColorIndex", 0);
            prefBgOpacity = prefCategory.CreateEntry("BgOpacity", 0.6f);

            menuKey = prefMenuKey.Value;
            savePosKey = prefSavePosKey.Value;
            loadPosKey = prefLoadPosKey.Value;
            spawnPosKey = prefSpawnPosKey.Value;
            restartKey = prefRestartKey.Value;

            practiceModule = new PracticeModule();
            speedoModule = new SpeedometerModule();
            deathZoneModule = new DeathZoneVisualizerModule();

            speedoModule.IsEnabled = prefHudEnabled.Value;
            speedoModule.ShowSpeed = prefShowSpeed.Value;
            speedoModule.ShowCoords = prefShowCoords.Value;
            speedoModule.HideNativeSpeedo = prefHideNativeSpeedo.Value;
            speedoModule.HudX = prefHudX.Value;
            speedoModule.HudY = prefHudY.Value;
            speedoModule.FontSize = prefFontSize.Value;
            speedoModule.FontStyle = (FontStyle)prefFontStyle.Value;
            speedoModule.ColorIndex = Mathf.Clamp(prefColorIndex.Value, 0, SpeedometerModule.Colors.Length - 1);
            speedoModule.BgOpacity = prefBgOpacity.Value;
            speedoModule.UpdateBgTexture();

            musicModule.Init();
            fovModule.Init();
            inputModule.Init();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (practiceModule != null) practiceModule.OnSceneWasLoaded(sceneName);
            if (speedoModule != null) speedoModule.OnSceneWasLoaded(sceneName);
            if (deathZoneModule != null) deathZoneModule.OnSceneWasLoaded(sceneName);
        }

        public override void OnUpdate()
        {
            // Rebinding logic
            if (activeRebindIndex != -1)
            {
                foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kcode))
                    {
                        switch (activeRebindIndex)
                        {
                            case 0: menuKey = kcode; break;
                            case 1: savePosKey = kcode; break;
                            case 2: loadPosKey = kcode; break;
                            case 3: spawnPosKey = kcode; break;
                            case 4: restartKey = kcode; break;
                        }
                        activeRebindIndex = -1;
                        SaveConfig();
                        break;
                    }
                }
                return;
            }

            // Menu toggle
            if (Input.GetKeyDown(menuKey))
            {
                showMenu = !showMenu;
                Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = showMenu;
                if (!showMenu) SaveConfig();
            }

            // Practice hotkeys
            if (practiceModule != null)
            {
                if (Input.GetKeyDown(restartKey)) practiceModule.ResetCheckpoint("Restart pressed");
                if (Input.GetKeyDown(savePosKey)) practiceModule.SavePlayerPosition();
                if (Input.GetKeyDown(loadPosKey)) practiceModule.LoadPlayerPosition();
                if (Input.GetKeyDown(spawnPosKey)) practiceModule.TeleportToSpawn();

                practiceModule.Update();
            }

            // Speedometer update
            if (speedoModule != null)
            {
                speedoModule.Update();
            }
            musicModule.Update();

            fovModule.Update();
        }

        public override void OnGUI()
        {
            if (practiceModule != null) practiceModule.OnGUI();
            if (speedoModule != null) speedoModule.OnGUI(); // Вот эта строчка потерялась!
            if (musicModule != null) musicModule.OnGUI();
            if (inputModule != null) inputModule.OnGUI();

            if (showMenu) DrawSettingsMenu();
        }

        private void SaveConfig()
        {
            prefMenuKey.Value = menuKey;
            prefSavePosKey.Value = savePosKey;
            prefLoadPosKey.Value = loadPosKey;
            prefSpawnPosKey.Value = spawnPosKey;
            prefRestartKey.Value = restartKey;

            if (speedoModule != null)
            {
                prefHudEnabled.Value = speedoModule.IsEnabled;
                prefShowSpeed.Value = speedoModule.ShowSpeed;
                prefShowCoords.Value = speedoModule.ShowCoords;
                prefHideNativeSpeedo.Value = speedoModule.HideNativeSpeedo;
                prefHudX.Value = speedoModule.HudX;
                prefHudY.Value = speedoModule.HudY;
                prefFontSize.Value = speedoModule.FontSize;
                prefFontStyle.Value = (int)speedoModule.FontStyle;
                prefColorIndex.Value = speedoModule.ColorIndex;
                prefBgOpacity.Value = speedoModule.BgOpacity;
            }

            prefCategory.SaveToFile();
        }

        private void DrawSettingsMenu()
        {
            float menuWidth = 320f;
            float menuHeight = 520f;
            float menuX = (Screen.width - menuWidth) / 2f;
            float menuY = (Screen.height - menuHeight) / 2f;

            Rect menuRect = new Rect(menuX, menuY, menuWidth, menuHeight);
            GUI.Box(menuRect, "Speedrun Toolkit v3.3.1");

            float x = menuRect.x + 15;
            float y = menuRect.y + 30;

            // 5 вкладок меню
            float tabWidth = 55f;
            float tabGap = 3f;

            if (GUI.Button(new Rect(x, y, tabWidth, 25), selectedTab == 0 ? "<b>Practice</b>" : "Practice")) selectedTab = 0;
            if (GUI.Button(new Rect(x + (tabWidth + tabGap), y, tabWidth, 25), selectedTab == 1 ? "<b>HUD</b>" : "HUD")) selectedTab = 1;
            if (GUI.Button(new Rect(x + (tabWidth + tabGap) * 2, y, tabWidth, 25), selectedTab == 2 ? "<b>Death Z.</b>" : "Death Z.")) selectedTab = 2;
            if (GUI.Button(new Rect(x + (tabWidth + tabGap) * 3, y, tabWidth, 25), selectedTab == 3 ? "<b>FOV</b>" : "FOV")) selectedTab = 3;
            if (GUI.Button(new Rect(x + (tabWidth + tabGap) * 4, y, tabWidth, 25), selectedTab == 4 ? "<b>Input</b>" : "Input")) selectedTab = 4;
            y += 35;

            if (selectedTab == 0)
            {
                GUI.Label(new Rect(x, y, 270, 20), "<b>Practice Keybindings:</b>");
                y += 22;

                DrawRebindButton(x, ref y, "Save Checkpoint", savePosKey, 1);
                DrawRebindButton(x, ref y, "Load Checkpoint", loadPosKey, 2);
                DrawRebindButton(x, ref y, "Teleport to Spawn", spawnPosKey, 3);
                DrawRebindButton(x, ref y, "Reset Checkpoint", restartKey, 4);

                y += 10;
                GUI.Label(new Rect(x, y, 270, 20), "<b>General:</b>");
                y += 22;
                DrawRebindButton(x, ref y, "Menu Toggle Key", menuKey, 0);
            }
            else if (selectedTab == 1 && speedoModule != null)
            {
                speedoModule.IsEnabled = GUI.Toggle(new Rect(x, y, 270, 20), speedoModule.IsEnabled, " Enable HUD");
                y += 22;
                speedoModule.ShowSpeed = GUI.Toggle(new Rect(x, y, 270, 20), speedoModule.ShowSpeed, " Show Speedometer");
                y += 22;
                speedoModule.ShowCoords = GUI.Toggle(new Rect(x, y, 270, 20), speedoModule.ShowCoords, " Show Coordinates");
                y += 22;

                bool newHideNative = GUI.Toggle(new Rect(x, y, 270, 20), speedoModule.HideNativeSpeedo, " Hide Native Speedometer");
                if (newHideNative != speedoModule.HideNativeSpeedo)
                {
                    speedoModule.HideNativeSpeedo = newHideNative;
                    speedoModule.ToggleNativeSpeedometer(newHideNative);
                }
                y += 25;

                // Size and style
                GUI.Label(new Rect(x, y, 270, 20), $"Font Size: {speedoModule.FontSize}");
                y += 18;
                speedoModule.FontSize = (int)GUI.HorizontalSlider(new Rect(x, y, 270, 15), speedoModule.FontSize, 10f, 28f);
                y += 20;

                if (GUI.Button(new Rect(x, y, 270, 22), $"Font Style: [{speedoModule.FontStyle}]"))
                {
                    switch (speedoModule.FontStyle)
                    {
                        case FontStyle.Normal: speedoModule.FontStyle = FontStyle.Bold; break;
                        case FontStyle.Bold: speedoModule.FontStyle = FontStyle.Italic; break;
                        case FontStyle.Italic: speedoModule.FontStyle = FontStyle.BoldAndItalic; break;
                        default: speedoModule.FontStyle = FontStyle.Normal; break;
                    }
                }
                y += 24;

                if (GUI.Button(new Rect(x, y, 270, 22), $"Value Color: [{SpeedometerModule.ColorNames[speedoModule.ColorIndex]}]"))
                {
                    speedoModule.ColorIndex = (speedoModule.ColorIndex + 1) % SpeedometerModule.Colors.Length;
                }
                y += 24;

                GUI.Label(new Rect(x, y, 270, 20), $"Background Alpha: {(int)(speedoModule.BgOpacity * 100)}%");
                y += 18;
                float newOpacity = GUI.HorizontalSlider(new Rect(x, y, 270, 15), speedoModule.BgOpacity, 0f, 1f);
                if (Mathf.Abs(newOpacity - speedoModule.BgOpacity) > 0.01f)
                {
                    speedoModule.BgOpacity = newOpacity;
                    speedoModule.UpdateBgTexture();
                }
                y += 24;

                // Sliders X and Y
                GUI.Label(new Rect(x, y, 270, 20), $"Position X: {(int)speedoModule.HudX}");
                y += 18;
                speedoModule.HudX = GUI.HorizontalSlider(new Rect(x, y, 270, 15), speedoModule.HudX, 0f, Screen.width - 230);
                y += 20;

                GUI.Label(new Rect(x, y, 270, 20), $"Position Y: {(int)speedoModule.HudY}");
                y += 18;
                speedoModule.HudY = GUI.HorizontalSlider(new Rect(x, y, 270, 15), speedoModule.HudY, 0f, Screen.height - 60);
            }
            else if (selectedTab == 2 && deathZoneModule != null)
            {
                string btnVis = deathZoneModule.IsVisualsOn ? "Visuals: ON" : "Visuals: OFF";
                if (GUI.Button(new Rect(x, y, 270, 25), btnVis))
                {
                    deathZoneModule.IsVisualsOn = !deathZoneModule.IsVisualsOn;
                    deathZoneModule.RefreshVisuals();
                }
                y += 30;

                bool newXRay = GUI.Toggle(new Rect(x, y, 270, 20), deathZoneModule.XRay, " X-Ray (Through Walls)");
                if (newXRay != deathZoneModule.XRay)
                {
                    deathZoneModule.XRay = newXRay;
                    deathZoneModule.UpdateMaterialProperties();
                }
                y += 22;

                bool newWire = GUI.Toggle(new Rect(x, y, 270, 20), deathZoneModule.WireframeMode, " Wireframe Mode");
                if (newWire != deathZoneModule.WireframeMode)
                {
                    deathZoneModule.WireframeMode = newWire;
                    deathZoneModule.RefreshVisuals();
                }
                y += 25;

                GUI.Label(new Rect(x, y, 270, 20), $"Color (R:{(int)(deathZoneModule.ColorR * 255)} G:{(int)(deathZoneModule.ColorG * 255)} B:{(int)(deathZoneModule.ColorB * 255)})");
                y += 18;
                float nr = GUI.HorizontalSlider(new Rect(x, y, 270, 15), deathZoneModule.ColorR, 0f, 1f);
                y += 16;
                float ng = GUI.HorizontalSlider(new Rect(x, y, 270, 15), deathZoneModule.ColorG, 0f, 1f);
                y += 16;
                float nb = GUI.HorizontalSlider(new Rect(x, y, 270, 15), deathZoneModule.ColorB, 0f, 1f);

                if (Mathf.Abs(nr - deathZoneModule.ColorR) > 0.01f || Mathf.Abs(ng - deathZoneModule.ColorG) > 0.01f || Mathf.Abs(nb - deathZoneModule.ColorB) > 0.01f)
                {
                    deathZoneModule.ColorR = nr; deathZoneModule.ColorG = ng; deathZoneModule.ColorB = nb;
                    deathZoneModule.UpdateMaterialProperties();
                }
                y += 22;

                GUI.Label(new Rect(x, y, 270, 20), $"Transparency: {(int)(deathZoneModule.Transparency * 100)}%");
                y += 18;
                float newTrans = GUI.HorizontalSlider(new Rect(x, y, 270, 15), deathZoneModule.Transparency, 0.05f, 1.0f);
                if (Mathf.Abs(newTrans - deathZoneModule.Transparency) > 0.01f)
                {
                    deathZoneModule.Transparency = newTrans;
                    deathZoneModule.UpdateMaterialProperties();
                }
                y += 25;

                if (GUI.Button(new Rect(x, y, 270, 22), "🔄 Force Rescan Map"))
                {
                    deathZoneModule.RefreshVisuals();
                }
            }
            else if (selectedTab == 3 && fovModule != null)
            {
                fovModule.DrawUI(x, y, 270f);
            }
            else if (selectedTab == 4 && inputModule != null)
            {
                inputModule.DrawUI(x, y, 270f);
            }

            y = menuRect.y + menuHeight - 35f;
            if (GUI.Button(new Rect(x, y, 290, 25), "Save & Close"))
            {
                SaveConfig();
                showMenu = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void DrawRebindButton(float x, ref float y, string label, KeyCode currentKey, int rebindIndex)
        {
            string btnText = (activeRebindIndex == rebindIndex) ? "Press Any Key..." : $"{label}: [{currentKey}]";
            if (GUI.Button(new Rect(x, y, 290, 22), btnText))
            {
                activeRebindIndex = rebindIndex;
            }
            y += 26;
        }
    }
}