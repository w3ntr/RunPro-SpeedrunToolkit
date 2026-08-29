using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using System.IO;

[assembly: MelonInfo(typeof(SpeedrunToolkitMod.Main), "Speedrun Toolkit", "5.5.0", "w3ntr")]
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
        public FixesModule fixesModule = new FixesModule();
       // private TimerModule timerModule = new TimerModule();
        private bool showMenu = false;
        public static bool instantRespawn = false;
        private static float lastRespawnTime = 0f;


        private void HandleInstantRespawn()
        {
            if (!instantRespawn) return;
            if (Time.time - lastRespawnTime < 0.2f) return;

            ScoreBoardCanvas scoreBoard = Object.FindObjectOfType<ScoreBoardCanvas>();
            if (scoreBoard != null && scoreBoard.gameObject.activeSelf)
            {
                if (scoreBoard.m_deathImg != null && scoreBoard.m_deathImg.activeSelf)
                {
                    lastRespawnTime = Time.time;

                    // 1. Гасим экраны смерти
                    scoreBoard.m_deathImg.SetActive(false);
                    scoreBoard.gameObject.SetActive(false);

                    // 2. Возвращаем захват мыши (игра отключает его при смерти)
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    // 3. Вызываем родной респавн
                    scoreBoard.On_TryAgain();

                    // 4. Принудительно включаем контроллер игрока
                    var fps = Object.FindObjectOfType<Il2Cpp.FirstPersonController>();
                    if (fps != null)
                    {
                        fps.enabled = true;
                    }
                }
            }
        }
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
        public static MelonPreferences_Entry<bool> prefTungEnabled;
        public static MelonPreferences_Entry<KeyCode> prefTungKey;
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
            prefTungEnabled = prefCategory.CreateEntry("TungEnabled", false);
            prefTungKey = prefCategory.CreateEntry("TungKey", KeyCode.F7);

            string toolkitFolder = Path.Combine(MelonEnvironment.UserDataDirectory, "SpeedrunToolkit");

            if (!Directory.Exists(toolkitFolder))
            {
                Directory.CreateDirectory(toolkitFolder);
            }

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
            // Задержка или вызов подмены после прогрузки карты
            ApplyTungTungSkin();
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
            {
                // Нажатие F7 переключает видимость Тун Туна
                if (Input.GetKeyDown(prefTungKey.Value))
                {
                    prefTungEnabled.Value = !prefTungEnabled.Value;
                    SaveConfig();
                }

                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    if (prefTungEnabled.Value)
                    {
                        // Если включен — создаем/включаем и анимируем
                        if (TungTungLoader.tungObject == null)
                        {
                            TungTungLoader.SpawnTungTung(player.transform);
                        }
                        else if (!TungTungLoader.tungObject.activeSelf)
                        {
                            TungTungLoader.SetActive(true);
                        }

                        TungTungLoader.UpdateAnimation(player.transform);
                    }
                    else
                    {
                        // Если выключен — скрываем
                        if (TungTungLoader.tungObject != null && TungTungLoader.tungObject.activeSelf)
                        {
                            TungTungLoader.SetActive(false);
                        }
                    }
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
                    TrajectoryModule.Update();
                    // timerModule.OnUpdate();
                }
                // Мгновенный респавн проверяется каждый кадр
                HandleInstantRespawn();
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

        private void ApplyTungTungSkin()
        {
            // Ищем объект игрока по тегу или имени
            GameObject player = GameObject.FindWithTag("Player");

            // Если по тегу не находит, распечатаем лог для отладки
            if (player == null)
            {
                MelonLogger.Warning(" Player isn't found!! Trying search by name...");
                player = GameObject.Find("Player");
            }

            if (player != null)
            {
                // 1. Отключаем видимость родного меша
                foreach (var rend in player.GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = false;
                }

                // 2. Спавним Тун Туна
                GameObject tung = TungTungLoader.SpawnTungTung(player.transform);

                if (tung != null)
                {
                    MelonLogger.Msg("Tung Tung Tung Sahur was added!! 🗿🥁");
                }
                else
                {
                    MelonLogger.Error("Maybe there's no file of model in UserData/SpeedrunToolkit/ ? Or create folder");
                }
            }
        }

        private void DrawSettingsMenu()
        {
            float menuWidth = 580f;
            float menuHeight = 560f;
            float menuX = (Screen.width - menuWidth) / 2f;
            float menuY = (Screen.height - menuHeight) / 2f;

            Rect menuRect = new Rect(menuX, menuY, menuWidth, menuHeight);
            GUI.Box(menuRect, "Speedrun Toolkit v5.5.0");

            float x = menuRect.x + 15f;
            float y = menuRect.y + 28f;
            float contentWidth = menuWidth - 30f;

            // Добавлена вкладка "Fixes" (всего 11 вкладок)
            string[] tabNames = new string[] { "Practice", "HUD & Model", "Death", "FOV", "Input", "Graphics", "Music", "Movement", "Fix & QoL", "Slomo", "Info" };
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

            // Автоматический расчет высоты для любого количества рядов кнопок
            int totalRows = (tabNames.Length + tabsPerRow - 1) / tabsPerRow;
            y += (tabHeight + 3f) * totalRows + 12f;

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
                prefTungEnabled.Value = GUI.Toggle(new Rect(x, y, contentWidth, 20), prefTungEnabled.Value, " Enable Tung Tung Sahur Model");
                y += 25;
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
            else if (selectedTab == 8 && fixesModule != null)
            {
                fixesModule.DrawUI(x, y, contentWidth);
            }
            else if (selectedTab == 9 && Slomo != null)
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
                    " • <b>[</b> / <b>]</b> Or <b>Numpad - / +</b> : Change Speed (-/+ 0.1x)\n" +
                    " • <b>Numpad 0</b> : Reset Speed on 1.0x"
                );
            }
            else if (selectedTab == 10)
            {
                GUI.Label(new Rect(x, y, contentWidth, 20f), "<b>📖 Hotkeys & Information</b>");
                y += 22f;

                string infoText =
                    $"• <b>{menuKey}</b> — Toggle Settings Menu\n" +
                    $"• <b>{savePosKey}</b> / <b>{loadPosKey}</b> — Save / Load Active Checkpoint\n" +
                    $"• <b>{prevSlotKey}</b> / <b>{nextSlotKey}</b> — Switch Active Slot (1–5)\n" +
                    $"• <b>{spawnPosKey}</b> — Teleport to Level Start\n" +
                    $"• <b>{restartKey}</b> — Reset Current Slot\n" +
                    "• <b>F3</b> — Toggle Freecam Mode\n" +
                    "• <b>[ / ]</b> or <b>Numpad - / +</b> — Adjust Game Speed\n" +
                    "• <b>Numpad 0</b> — Reset Speed to 1.0x\n\n" +
                    "<b>Anti-Cheat Note:</b> Setting Force Multipliers outside 1.00x–1.02x in Fix & QoL tab disables the finish trigger to keep leaderboards fair.\n\n" +
                    "--------------------------------------------------\n" +
                    "<b>🔥 ULTRA POTATO MODE INFO</b>\n" +
                    "--------------------------------------------------\n" +
                    "• <b>Solid Background:</b> Replaces skybox mesh with flat black color to bypass sky shaders.\n" +
                    "• <b>Extreme Downscaling:</b> Forces maximum texture compression and ultra-low 3D LODs.\n" +
                    "• <b>Far Clip Distance:</b> Limits camera render distance to 30m to drop distant geometry.\n" +
                    "• <b>Light Suppression:</b> Disables Point/Spot lights and real-time shadow calculations.\n" +
                    "• <b>Uncapped Latency:</b> Sets V-Sync to 0 and queued frames to 0 for minimum input lag.\n\n" +
                    "<b>📺 Resolution Presets:</b>\n" +
                    "• <b>480p Full:</b> Stretches pixelated 480x270 viewport across full monitor for max FPS.\n" +
                    "• <b>480p Window:</b> Switches game into a compact 480x270 window.\n" +
                    "• <b>Native Res:</b> Restores your monitor's original screen resolution.";

                // Увеличена высота блока с 280 до 520px
                GUI.Label(new Rect(x, y, contentWidth, 520f), infoText);
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