using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SpeedrunToolkitMod.Main), "Speedrun Toolkit", "4.5.0", "w3ntr")]
[assembly: MelonGame(null, null)]

namespace SpeedrunToolkitMod
{
    public class Main : MelonMod
    {
        private PracticeModule practiceModule;
        private SpeedometerModule speedoModule;
        private DeathZoneVisualizerModule deathZoneModule;
        private MusicReplacerModule musicModule;
        private MovementModule movementModule;
        private FovChangerModule fovModule = new FovChangerModule();
        private InputOverlayModule inputModule = new InputOverlayModule();
        private GraphicsModule graphicsModule = new GraphicsModule();
        private FreecamModule freecamModule;
        public static SlomoModule Slomo = new SlomoModule();
        private CrosshairModule crosshairModule = new CrosshairModule();

        private bool showMenu = false;
        private int selectedTab = 0;

        // Keybinding settings
        private KeyCode menuKey = KeyCode.F8;
        private KeyCode savePosKey = KeyCode.F9;
        private KeyCode loadPosKey = KeyCode.F10;
        private KeyCode spawnPosKey = KeyCode.F11;
        private KeyCode restartKey = KeyCode.R;
        private KeyCode nextSlotKey = KeyCode.PageDown;
        private KeyCode prevSlotKey = KeyCode.PageUp;

        private int activeRebindIndex = -1;

        private MelonPreferences_Category prefCategory;
        private MelonPreferences_Entry<KeyCode> prefMenuKey;
        private MelonPreferences_Entry<KeyCode> prefSavePosKey;
        private MelonPreferences_Entry<KeyCode> prefLoadPosKey;
        private MelonPreferences_Entry<KeyCode> prefSpawnPosKey;
        private MelonPreferences_Entry<KeyCode> prefRestartKey;
        private MelonPreferences_Entry<KeyCode> prefNextSlotKey;
        private MelonPreferences_Entry<KeyCode> prefPrevSlotKey;

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

        public float GravityScale = 1.0f;

        public override void OnInitializeMelon()
        {
            prefCategory = MelonPreferences.CreateCategory("SpeedrunToolkit", "Speedrun Toolkit Settings");
            prefMenuKey = prefCategory.CreateEntry("MenuKey", KeyCode.F8);
            prefSavePosKey = prefCategory.CreateEntry("SavePosKey", KeyCode.F9);
            prefLoadPosKey = prefCategory.CreateEntry("LoadPosKey", KeyCode.F10);
            prefSpawnPosKey = prefCategory.CreateEntry("SpawnPosKey", KeyCode.F11);
            prefRestartKey = prefCategory.CreateEntry("RestartKey", KeyCode.R);
            prefNextSlotKey = prefCategory.CreateEntry("NextSlotKey", KeyCode.PageDown);
            prefPrevSlotKey = prefCategory.CreateEntry("PrevSlotKey", KeyCode.PageUp);

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
            nextSlotKey = prefNextSlotKey.Value;
            prevSlotKey = prefPrevSlotKey.Value;

            practiceModule = new PracticeModule();
            speedoModule = new SpeedometerModule();
            deathZoneModule = new DeathZoneVisualizerModule();
            freecamModule = new FreecamModule();
            musicModule = new MusicReplacerModule();
            movementModule = new MovementModule();

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
            graphicsModule.Init();
            freecamModule.Init();
            Slomo.Init();
            crosshairModule.Init();
            movementModule.Init();
        }

        public void SetGravityScale(float scale)
        {
            GravityScale = scale;
            Physics.gravity = new Vector3(0f, -9.81f * scale, 0f);
            BlockFinishAndTimer();
        }

        public void ResetGravity()
        {
            SetGravityScale(1.0f);
        }

        private void BlockFinishAndTimer()
        {
            // Optional implementation for invalidating timer/finish triggers
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (practiceModule != null) practiceModule.OnSceneWasLoaded(sceneName);
            if (speedoModule != null) speedoModule.OnSceneWasLoaded(sceneName);
            if (graphicsModule != null) graphicsModule.ApplyGraphicsSettings();
            if (freecamModule != null) freecamModule.DisableFreecam();
            if (deathZoneModule != null) deathZoneModule.OnSceneWasLoaded(sceneName);
            if (Slomo != null) Slomo.ResetSpeed();
            // 1. Сбрасываем гравитацию на дефолтную (1.0f)
            ResetGravity();

            // 2. Отключаем/сбрасываем модификации мувмента при загрузке карты
            if (movementModule != null)
            {
                movementModule.Reset();
            }
        }
        

        public override void OnUpdate()
        {
            if (freecamModule != null) freecamModule.OnUpdate();
            if (musicModule != null) musicModule.OnUpdate();

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
                            case 5: nextSlotKey = kcode; break;
                            case 6: prevSlotKey = kcode; break;
                        }
                        activeRebindIndex = -1;
                        SaveConfig();
                        break;
                    }
                }
                return;
            }

            if (Input.GetKeyDown(menuKey))
            {
                showMenu = !showMenu;
                Cursor.lockState = showMenu ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = showMenu;
                if (!showMenu) SaveConfig();
            }

            if (practiceModule != null)
            {
                if (Input.GetKeyDown(restartKey)) practiceModule.ResetCurrentCheckpoint();
                if (Input.GetKeyDown(savePosKey)) practiceModule.SavePlayerPosition();
                if (Input.GetKeyDown(loadPosKey)) practiceModule.LoadPlayerPosition();
                if (Input.GetKeyDown(spawnPosKey)) practiceModule.TeleportToSpawn();
                if (Input.GetKeyDown(nextSlotKey)) practiceModule.NextSlot();
                if (Input.GetKeyDown(prevSlotKey)) practiceModule.PrevSlot();

                if (Input.GetKeyDown(KeyCode.T)) practiceModule.TeleportToCrosshair();
                if (Input.GetKeyDown(KeyCode.Keypad0)) practiceModule.ResetGravity();

                practiceModule.Update();
            }

            if (Slomo != null) Slomo.OnUpdate();
            if (speedoModule != null) speedoModule.Update();
            if (fovModule != null) fovModule.Update();

            if (movementModule != null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    movementModule.PerformAirDash(practiceModule);
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    movementModule.PerformAirJump(practiceModule);
                }

                movementModule.Update();
            }
        }

        public override void OnFixedUpdate()
        {
            // if (movementModule != null) movementModule.OnFixedUpdate();
        }

        public override void OnGUI()
        {
            if (practiceModule != null) practiceModule.OnGUI();
            if (speedoModule != null) speedoModule.OnGUI();
            if (inputModule != null) inputModule.OnGUI();
            if (crosshairModule != null) crosshairModule.OnGUI();

            if (showMenu) DrawSettingsMenu();
        }

        private void SaveConfig()
        {
            prefMenuKey.Value = menuKey;
            prefSavePosKey.Value = savePosKey;
            prefLoadPosKey.Value = loadPosKey;
            prefSpawnPosKey.Value = spawnPosKey;
            prefRestartKey.Value = restartKey;
            prefNextSlotKey.Value = nextSlotKey;
            prefPrevSlotKey.Value = prevSlotKey;

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
            float menuWidth = 580f;
            float menuHeight = 560f;
            float menuX = (Screen.width - menuWidth) / 2f;
            float menuY = (Screen.height - menuHeight) / 2f;

            Rect menuRect = new Rect(menuX, menuY, menuWidth, menuHeight);
            GUI.Box(menuRect, "Speedrun Toolkit v4.5.0");

            float x = menuRect.x + 15f;
            float y = menuRect.y + 28f;
            float contentWidth = menuWidth - 30f;

            string[] tabNames = new string[] { "Practice", "HUD", "Death", "FOV", "Input", "Graphics", "Music", "Movement", "Slomo", "Info" };
            int tabsPerRow = 5;
            float tabGap = 3f;
            float tabWidth = (contentWidth - (tabGap * (tabsPerRow - 1))) / tabsPerRow;
            float tabHeight = 22f;

            for (int i = 0; i < tabNames.Length; i++)
            {
                int row = i / tabsPerRow;
                int col = i % tabsPerRow;
                float rx = x + (tabWidth + tabGap) * col;
                float ry = y + (tabHeight + 3f) * row;

                string label = selectedTab == i ? $"<b>{tabNames[i]}</b>" : tabNames[i];
                if (GUI.Button(new Rect(rx, ry, tabWidth, tabHeight), label))
                {
                    selectedTab = i;
                }
            }

            y += (tabHeight + 3f) * 2f + 12f;

            if (selectedTab == 0 && practiceModule != null)
            {
                GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Checkpoint Slots:</b>");
                y += 22;

                float slotBtnWidth = (contentWidth - 20f) / 5f;
                for (int i = 0; i < PracticeModule.MaxSlots; i++)
                {
                    bool isSelected = (practiceModule.currentSlotIndex == i);
                    bool hasData = practiceModule.slots[i].isValid;

                    string slotTitle = $"Slot {i + 1}" + (hasData ? " *" : "");
                    if (isSelected) slotTitle = $"<b>[{slotTitle}]</b>";

                    if (GUI.Button(new Rect(x + (slotBtnWidth + 5f) * i, y, slotBtnWidth, 25f), slotTitle))
                    {
                        practiceModule.SelectSlot(i);
                    }
                }
                y += 32;

                GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Practice Keybindings:</b>");
                y += 22;

                DrawRebindButton(x, ref y, contentWidth, "Save Checkpoint (Current Slot)", savePosKey, 1);
                DrawRebindButton(x, ref y, contentWidth, "Load Checkpoint (Current Slot)", loadPosKey, 2);
                DrawRebindButton(x, ref y, contentWidth, "Next Slot", nextSlotKey, 5);
                DrawRebindButton(x, ref y, contentWidth, "Previous Slot", prevSlotKey, 6);
                DrawRebindButton(x, ref y, contentWidth, "Teleport to Spawn", spawnPosKey, 3);
                DrawRebindButton(x, ref y, contentWidth, "Reset Current Slot Checkpoint", restartKey, 4);

                GUI.Label(new Rect(x, y, contentWidth, 20), $"<b>Gravity Scale:</b> {practiceModule.GravityScale * 100f:F0}%");
                y += 22;

                float newGravity = GUI.HorizontalSlider(new Rect(x, y + 4, contentWidth - 105f, 20), practiceModule.GravityScale, 0.0f, 3.0f);

                if (GUI.Button(new Rect(x + contentWidth - 100f, y, 100f, 22), "Reset (100%)"))
                {
                    practiceModule.ResetGravity();
                }
                else if (Mathf.Abs(newGravity - practiceModule.GravityScale) > 0.01f)
                {
                    practiceModule.SetGravityScale(newGravity);
                }
                y += 28;

                y += 10;
                GUI.Label(new Rect(x, y, contentWidth, 20), "<b>General Settings:</b>");
                y += 22;
                DrawRebindButton(x, ref y, contentWidth, "Menu Toggle Key", menuKey, 0);
            }
            else if (selectedTab == 1 && speedoModule != null)
            {
                speedoModule.IsEnabled = GUI.Toggle(new Rect(x, y, contentWidth, 20), speedoModule.IsEnabled, " Enable HUD Overlay");
                y += 22;
                speedoModule.ShowSpeed = GUI.Toggle(new Rect(x, y, contentWidth, 20), speedoModule.ShowSpeed, " Show Speedometer");
                y += 22;
                speedoModule.ShowCoords = GUI.Toggle(new Rect(x, y, contentWidth, 20), speedoModule.ShowCoords, " Show Player Coordinates");
                y += 22;

                bool newHideNative = GUI.Toggle(new Rect(x, y, contentWidth, 20), speedoModule.HideNativeSpeedo, " Hide Native Game Speedometer");
                if (newHideNative != speedoModule.HideNativeSpeedo)
                {
                    speedoModule.HideNativeSpeedo = newHideNative;
                    speedoModule.ToggleNativeSpeedometer(newHideNative);
                }
                y += 25;

                GUI.Label(new Rect(x, y, contentWidth, 20), $"Font Size: {speedoModule.FontSize}");
                y += 18;
                speedoModule.FontSize = (int)GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), speedoModule.FontSize, 10f, 32f);
                y += 25;

                GUI.Box(new Rect(x, y, contentWidth, 1), "");
                y += 10;

                if (crosshairModule != null)
                {
                    y = crosshairModule.DrawUI(x, y, contentWidth);
                }
            }
            else if (selectedTab == 2 && deathZoneModule != null)
            {
                string btnVis = deathZoneModule.IsVisualsOn ? "Visuals: ON" : "Visuals: OFF";
                if (GUI.Button(new Rect(x, y, contentWidth, 25), btnVis))
                {
                    deathZoneModule.IsVisualsOn = !deathZoneModule.IsVisualsOn;
                    deathZoneModule.RefreshVisuals();
                }
                y += 30;

                bool newXRay = GUI.Toggle(new Rect(x, y, contentWidth, 20), deathZoneModule.XRay, " X-Ray Mode (Visible Through Walls)");
                if (newXRay != deathZoneModule.XRay)
                {
                    deathZoneModule.XRay = newXRay;
                    deathZoneModule.UpdateMaterialProperties();
                }
                y += 22;

                bool newWire = GUI.Toggle(new Rect(x, y, contentWidth, 20), deathZoneModule.WireframeMode, " Wireframe Render Mode");
                if (newWire != deathZoneModule.WireframeMode)
                {
                    deathZoneModule.WireframeMode = newWire;
                    deathZoneModule.RefreshVisuals();
                }
                y += 25;

                GUI.Label(new Rect(x, y, contentWidth, 20), $"Color (R:{(int)(deathZoneModule.ColorR * 255)} G:{(int)(deathZoneModule.ColorG * 255)} B:{(int)(deathZoneModule.ColorB * 255)})");
                y += 18;
                float nr = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), deathZoneModule.ColorR, 0f, 1f);
                y += 16;
                float ng = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), deathZoneModule.ColorG, 0f, 1f);
                y += 16;
                float nb = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), deathZoneModule.ColorB, 0f, 1f);

                if (Mathf.Abs(nr - deathZoneModule.ColorR) > 0.01f || Mathf.Abs(ng - deathZoneModule.ColorG) > 0.01f || Mathf.Abs(nb - deathZoneModule.ColorB) > 0.01f)
                {
                    deathZoneModule.ColorR = nr; deathZoneModule.ColorG = ng; deathZoneModule.ColorB = nb;
                    deathZoneModule.UpdateMaterialProperties();
                }
                y += 22;

                GUI.Label(new Rect(x, y, contentWidth, 20), $"Transparency: {(int)(deathZoneModule.Transparency * 100)}%");
                y += 18;
                float newTrans = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), deathZoneModule.Transparency, 0.05f, 1.0f);
                if (Mathf.Abs(newTrans - deathZoneModule.Transparency) > 0.01f)
                {
                    deathZoneModule.Transparency = newTrans;
                    deathZoneModule.UpdateMaterialProperties();
                }
                y += 25;

                if (GUI.Button(new Rect(x, y, contentWidth, 22), "🔄 Force Rescan Map Triggers"))
                {
                    deathZoneModule.RefreshVisuals();
                }
            }
            else if (selectedTab == 3 && fovModule != null)
            {
                fovModule.DrawUI(x, y, contentWidth);
            }
            else if (selectedTab == 4 && inputModule != null)
            {
                inputModule.DrawUI(x, y, contentWidth);
            }
            else if (selectedTab == 5 && graphicsModule != null)
            {
                graphicsModule.DrawUI(x, y, contentWidth);
            }
            else if (selectedTab == 6 && musicModule != null)
            {
                musicModule.DrawUI(x, y, contentWidth);
            }
            else if (selectedTab == 7 && movementModule != null)
            {
                y = movementModule.DrawUI(x, y, contentWidth);
            }
            else if (selectedTab == 8 && Slomo != null)
            {
                GUI.Label(new Rect(x, y, contentWidth, 20), $"<b>Game Speed: {Slomo.CurrentScale:F1}x</b>");
                y += 22;

                float newScale = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), Slomo.CurrentScale, 0.1f, 1.0f);
                if (Mathf.Abs(newScale - Slomo.CurrentScale) > 0.01f)
                {
                    Slomo.SetSpeed(newScale);
                }
                y += 25;

                float btnW = (contentWidth - 15f) / 4f;
                if (GUI.Button(new Rect(x, y, btnW, 25), "0.25x")) Slomo.SetSpeed(0.25f);
                if (GUI.Button(new Rect(x + btnW + 5, y, btnW, 25), "0.50x")) Slomo.SetSpeed(0.50f);
                if (GUI.Button(new Rect(x + (btnW + 5) * 2, y, btnW, 25), "0.75x")) Slomo.SetSpeed(0.75f);
                if (GUI.Button(new Rect(x + (btnW + 5) * 3, y, btnW, 25), "1.00x")) Slomo.ResetSpeed();
                y += 35;

                GUI.Label(new Rect(x, y, contentWidth, 80),
                    "<b>Hotkeys:</b>\n" +
                    " • <b>[</b> / <b>]</b> или <b>Numpad - / +</b> : Изменить скорость (-/+ 0.1x)\n" +
                    " • <b>Numpad 0</b> : Сбросить скорость на 1.0x"
                );
            }
            else if (selectedTab == 9)
            {
                GUI.Label(new Rect(x, y, contentWidth, 20), "<b>📖 Hotkeys & Information</b>");
                y += 22;

                string infoText =
                    $"• <b>{menuKey}</b> — Открыть/Закрыть Меню Настроек\n" +
                    $"• <b>{savePosKey}</b> / <b>{loadPosKey}</b> — Сохранить / Загрузить Активный Слот\n" +
                    $"• <b>{prevSlotKey}</b> / <b>{nextSlotKey}</b> — Переключить Слот (1–5)\n" +
                    $"• <b>{spawnPosKey}</b> — Вернуться на Старт Карты\n" +
                    $"• <b>{restartKey}</b> — Сбросить Активный Слот\n" +
                    "• <b>F3</b> — Freecam (Свободная камера)\n" +
                    "• <b>[ / ]</b> — Замедление времени\n" +
                    "• <b>Numpad 0</b> — Сброс скорости времени";

                GUI.Label(new Rect(x, y, contentWidth, 260), infoText);
            }

            y = menuRect.y + menuHeight - 35f;
            if (GUI.Button(new Rect(x, y, contentWidth, 25), "Save & Close"))
            {
                SaveConfig();
                showMenu = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void DrawRebindButton(float x, ref float y, float width, string label, KeyCode currentKey, int rebindIndex)
        {
            string btnText = (activeRebindIndex == rebindIndex) ? "Press Any Key..." : $"{label}: [{currentKey}]";
            if (GUI.Button(new Rect(x, y, width, 22), btnText))
            {
                activeRebindIndex = rebindIndex;
            }
            y += 26;
        }
    }
}
