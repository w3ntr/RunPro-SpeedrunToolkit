using Il2Cpp;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(SpeedrunToolkitMod.Main), "Speedrun Toolkit", "6.0.0", "w3ntr")]
[assembly: MelonGame(null, null)]

namespace SpeedrunToolkitMod
{
    public class Main : MelonMod
    {
        private WindowManager windowManager = new WindowManager();
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
        private DiscordManager discordManager;
        public AntiAfkModule antiAfkModule = new AntiAfkModule();

        public static bool showMenu = false;
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

                    scoreBoard.m_deathImg.SetActive(false);
                    scoreBoard.gameObject.SetActive(false);

                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    scoreBoard.On_TryAgain();

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
        private MelonPreferences_Entry<bool> prefShowAngles;
        private MelonPreferences_Entry<bool> prefShowXP;
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
            prefShowAngles = prefCategory.CreateEntry("ShowAngles", true);
            prefShowXP = prefCategory.CreateEntry("ShowXP", true);
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
            speedoModule.ShowAngles = prefShowAngles.Value;
            speedoModule.ShowXP = prefShowXP.Value;
            speedoModule.HideNativeSpeedo = prefHideNativeSpeedo.Value;
            speedoModule.HudX = prefHudX.Value;
            speedoModule.HudY = prefHudY.Value;
            speedoModule.FontSize = prefFontSize.Value;
            speedoModule.FontStyle = (FontStyle)prefFontStyle.Value;
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
            speedoModule.Init();
            antiAfkModule.Init();

            discordManager = new DiscordManager();
        }

        public void SetGravityScale(float scale)
        {
            GravityScale = scale;
            Physics.gravity = new Vector3(0f, -9.81f * scale, 0f);
        }

        public void ResetGravity()
        {
            SetGravityScale(1.0f);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (practiceModule != null) practiceModule.OnSceneWasLoaded(sceneName);
            if (speedoModule != null) speedoModule.OnSceneWasLoaded(sceneName);
            if (graphicsModule != null) graphicsModule.ApplyGraphicsSettings();
            if (freecamModule != null) freecamModule.DisableFreecam();
            if (deathZoneModule != null) deathZoneModule.OnSceneWasLoaded(sceneName);
            if (Slomo != null) Slomo.ResetSpeed();
            if (fixesModule != null) fixesModule.OnSceneWasLoaded(sceneName);
            ResetGravity();

            if (movementModule != null)
            {
                movementModule.Reset();
            }
            ApplyTungTungSkin();
        }

        public static bool IsUserTyping()
        {
            try
            {
                // 1. Проверка IMGUI фокуса (если ввод идет в текстовое поле UI мода)
                if (!string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                    return true;

                // 2. Проверка EventSystem игры (чат, консоль)
                var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
                {
                    var selected = eventSystem.currentSelectedGameObject;

                    // Проверяем, выделено ли поле ввода и активен ли в нем курсор (isFocused)
                    var inputField = selected.GetComponent<UnityEngine.UI.InputField>();
                    if (inputField != null && inputField.isFocused)
                        return true;

                    // Для TextMeshPro полей ввода
                    var components = selected.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp != null && comp.GetType().Name.Contains("InputField"))
                        {
                            var prop = comp.GetType().GetProperty("isFocused");
                            if (prop != null && (bool)prop.GetValue(comp, null))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки при загрузке сцен
            }

            return false;
        }

        private void ToggleMenuState(bool newState)
        {
            showMenu = newState;

            if (showMenu)
            {
                Cursor.visible = true;
            }
            else
            {
                SaveConfig();
            }
        }

        public override void OnUpdate()
        {
            discordManager?.Update();
            antiAfkModule.OnUpdate();
            speedoModule?.Update();

            // Если игрок в этот момент печатает текст в любом поле — блокируем горячие клавиши
            if (IsUserTyping()) return;

            // Переключение меню
            if (Input.GetKeyDown(menuKey))
            {
                ToggleMenuState(!showMenu);
            }

            // Обработка Rebind
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

            // Блокировка вызовов мода при открытом меню
            if (showMenu) return;

            // Включаем обратно EventSystem игры, если меню закрыли
            if (EventSystem.current != null && !EventSystem.current.enabled)
            {
                EventSystem.current.enabled = true;
            }

            // Игровая логика
            if (freecamModule != null) freecamModule.OnUpdate();
            if (musicModule != null) musicModule.OnUpdate();
            graphicsModule?.OnUpdate();

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
                    if (TungTungLoader.tungObject != null && TungTungLoader.tungObject.activeSelf)
                    {
                        TungTungLoader.SetActive(false);
                    }
                }
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

                practiceModule.Update();
            }

            if (Slomo != null) Slomo.OnUpdate();
            if (speedoModule != null) speedoModule.Update();
            if (fovModule != null) fovModule.Update();

            if (movementModule != null)
            {
                if (Input.GetKeyDown(KeyCode.E)) movementModule.PerformAirDash(practiceModule);
                if (Input.GetKeyDown(KeyCode.Space)) movementModule.PerformAirJump(practiceModule);

                movementModule.Update();
                TrajectoryModule.Update();
            }
            HandleInstantRespawn();
        }

        public override void OnApplicationQuit()
        {
            discordManager?.Dispose();
        }

        public override void OnGUI()
        {
            if (practiceModule != null) practiceModule.OnGUI();
            if (speedoModule != null) speedoModule.OnGUI();
            if (inputModule != null) inputModule.OnGUI();
            if (crosshairModule != null) crosshairModule.OnGUI();

            if (showMenu)
            {
                DrawSettingsMenu();

                // Поглощаем клик, если курсор находится над окном мода
                if (windowManager != null && windowManager.WindowRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)
                    {
                        Event.current.Use();
                    }
                }
            }
            fixesModule.OnGUI();
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
                prefShowAngles.Value = speedoModule.ShowAngles;
                prefShowXP.Value = speedoModule.ShowXP;
                prefHideNativeSpeedo.Value = speedoModule.HideNativeSpeedo;
                prefHudX.Value = speedoModule.HudX;
                prefHudY.Value = speedoModule.HudY;
                prefFontSize.Value = speedoModule.FontSize;
                prefFontStyle.Value = (int)speedoModule.FontStyle;
                prefBgOpacity.Value = speedoModule.BgOpacity;
            }

            prefCategory.SaveToFile();
        }

        private void ApplyTungTungSkin()
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player != null)
            {
                foreach (var rend in player.GetComponentsInChildren<Renderer>())
                {
                    rend.enabled = false;
                }

                TungTungLoader.SpawnTungTung(player.transform);
            }
        }

        private void DrawSettingsMenu()
        {
            windowManager.Draw("Speedrun Toolkit v6.0.0", (id) =>
            {
                float x = 12f;
                float y = 30f;
                float contentWidth = windowManager.WindowRect.width - 24f;

                string[] tabNames = new string[] { "Practice", "HUD & Model", "Death Zone", "FOV", "Input", "Graphics", "Music", "Movement", "Fix & QoL", "Slomo", "Settings", "Info" };
                int tabsPerRow = 6;
                float tabGap = 4f;
                float tabWidth = (contentWidth - (tabGap * (tabsPerRow - 1))) / tabsPerRow;
                float tabHeight = 22f;

                for (int i = 0; i < tabNames.Length; i++)
                {
                    int row = i / tabsPerRow;
                    int col = i % tabsPerRow;
                    float rx = x + (tabWidth + tabGap) * col;
                    float ry = y + (tabHeight + 3f) * row;

                    GUIStyle style = (selectedTab == i) ? UITheme.TabActiveStyle : UITheme.TabStyle;
                    if (GUI.Button(new Rect(rx, ry, tabWidth, tabHeight), tabNames[i], style))
                    {
                        selectedTab = i;
                    }
                }

                y += (tabHeight + 3f) * ((tabNames.Length + tabsPerRow - 1) / tabsPerRow) + 10f;

                if (selectedTab == 0 && practiceModule != null)
                {
                    GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Checkpoint Slots:</b>", UITheme.LabelStyle);
                    y += 22;

                    float slotBtnWidth = (contentWidth - 20f) / 5f;
                    for (int i = 0; i < PracticeModule.MaxSlots; i++)
                    {
                        bool isSelected = (practiceModule.currentSlotIndex == i);
                        bool hasData = practiceModule.slots[i].isValid;

                        string slotTitle = $"Slot {i + 1}" + (hasData ? " *" : "");
                        if (isSelected) slotTitle = $"[{slotTitle}]";

                        GUIStyle btnStyle = isSelected ? UITheme.TabActiveStyle : UITheme.ButtonStyle;
                        if (GUI.Button(new Rect(x + (slotBtnWidth + 5f) * i, y, slotBtnWidth, 25f), slotTitle, btnStyle))
                        {
                            practiceModule.SelectSlot(i);
                        }
                    }
                    y += 32;

                    GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Practice Keybindings:</b>", UITheme.LabelStyle);
                    y += 22;

                    DrawRebindButton(x, ref y, contentWidth, "Save Checkpoint (Current Slot)", savePosKey, 1);
                    DrawRebindButton(x, ref y, contentWidth, "Load Checkpoint (Current Slot)", loadPosKey, 2);
                    DrawRebindButton(x, ref y, contentWidth, "Next Slot", nextSlotKey, 5);
                    DrawRebindButton(x, ref y, contentWidth, "Previous Slot", prevSlotKey, 6);
                    DrawRebindButton(x, ref y, contentWidth, "Teleport to Spawn", spawnPosKey, 3);
                    DrawRebindButton(x, ref y, contentWidth, "Reset Current Slot Checkpoint", restartKey, 4);

                    y += 10;
                    GUI.Label(new Rect(x, y, contentWidth, 20), "<b>General Settings:</b>", UITheme.LabelStyle);
                    y += 22;
                    DrawRebindButton(x, ref y, contentWidth, "Menu Toggle Key", menuKey, 0);
                }
                else if (selectedTab == 1 && speedoModule != null)
                {
                    speedoModule.IsEnabled = GUI.Toggle(new Rect(x, y, contentWidth, 18), speedoModule.IsEnabled, " Enable HUD Overlay");
                    y += 20;

                    float checkW = contentWidth / 2f;
                    speedoModule.ShowSpeed = GUI.Toggle(new Rect(x, y, checkW, 18), speedoModule.ShowSpeed, " Speedometer");
                    speedoModule.ShowCoords = GUI.Toggle(new Rect(x + checkW, y, checkW, 18), speedoModule.ShowCoords, " Coordinates");
                    y += 20;

                    speedoModule.ShowAngles = GUI.Toggle(new Rect(x, y, checkW, 18), speedoModule.ShowAngles, " Look Angles");
                    speedoModule.ShowXP = GUI.Toggle(new Rect(x + checkW, y, checkW, 18), speedoModule.ShowXP, " Player XP & Level");
                    y += 20;

                    bool newHideNative = GUI.Toggle(new Rect(x, y, contentWidth, 18), speedoModule.HideNativeSpeedo, " Hide Native Game Speedometer");
                    if (newHideNative != speedoModule.HideNativeSpeedo)
                    {
                        speedoModule.HideNativeSpeedo = newHideNative;
                        speedoModule.ToggleNativeSpeedometer(newHideNative);
                    }
                    y += 22;
                    // --- Выбор стиля шрифта ---
                    GUI.Label(new Rect(x, y, 90, 18), "Font Style:", UITheme.LabelStyle);
                    string[] fontNames = new string[] { "Bold", "Normal", "Italic", "Bold-Italic" };
                    FontStyle[] fontStyles = new FontStyle[] { FontStyle.Bold, FontStyle.Normal, FontStyle.Italic, FontStyle.BoldAndItalic };

                    float fBtnW = (contentWidth - 95f) / 4f;
                    for (int i = 0; i < fontNames.Length; i++)
                    {
                        if (GUI.Button(new Rect(x + 95f + (i * fBtnW), y, fBtnW - 2f, 18), fontNames[i]))
                        {
                            speedoModule.FontStyle = fontStyles[i];
                            speedoModule.SaveConfig();
                        }
                    }
                    y += 24;

                    // --- Цвет значений (Value Color + 4 Слайдера) ---
                    GUI.Label(new Rect(x, y, 75, 18), "Value Color:", UITheme.LabelStyle);
                    string newValHex = GUI.TextField(new Rect(x + 80, y, 75, 18), speedoModule.ValueHex);
                    if (newValHex != speedoModule.ValueHex)
                    {
                        speedoModule.ValueHex = newValHex;
                        speedoModule.SyncValueHSVFromHex();
                        speedoModule.SaveConfig();
                    }

                    float vSliderW = (contentWidth - 160f) / 4f;
                    float vH = Mathf.Clamp(GUI.HorizontalSlider(new Rect(x + 160, y + 2, vSliderW - 2, 15), speedoModule.ValueH, 0f, 1f), 0f, 0.999f);
                    float vS = GUI.HorizontalSlider(new Rect(x + 160 + vSliderW, y + 2, vSliderW - 2, 15), speedoModule.ValueS, 0f, 1f);
                    float vV = GUI.HorizontalSlider(new Rect(x + 160 + vSliderW * 2, y + 2, vSliderW - 2, 15), speedoModule.ValueV, 0f, 1f);
                    float vA = GUI.HorizontalSlider(new Rect(x + 160 + vSliderW * 3, y + 2, vSliderW - 2, 15), speedoModule.ValueA, 0f, 1f);

                    if (vH != speedoModule.ValueH || vS != speedoModule.ValueS || vV != speedoModule.ValueV || vA != speedoModule.ValueA)
                    {
                        speedoModule.ValueH = vH;
                        speedoModule.ValueS = vS;
                        speedoModule.ValueV = vV;
                        speedoModule.ValueA = vA;
                        Color col = Color.HSVToRGB(vH, vS, vV);
                        col.a = vA;
                        speedoModule.ValueHex = SpeedometerModule.ColorToHex(col, true);
                        speedoModule.SaveConfig();
                    }
                    y += 22;

                    // --- Цвет подписей (Label Color + 4 Слайдера) ---
                    GUI.Label(new Rect(x, y, 75, 18), "Label Color:", UITheme.LabelStyle);
                    string newLblHex = GUI.TextField(new Rect(x + 80, y, 75, 18), speedoModule.LabelHex);
                    if (newLblHex != speedoModule.LabelHex)
                    {
                        speedoModule.LabelHex = newLblHex;
                        speedoModule.SyncLabelHSVFromHex();
                        speedoModule.SaveConfig();
                    }

                    float lSliderW = (contentWidth - 160f) / 4f;
                    float lH = Mathf.Clamp(GUI.HorizontalSlider(new Rect(x + 160, y + 2, lSliderW - 2, 15), speedoModule.LabelH, 0f, 1f), 0f, 0.999f);
                    float lS = GUI.HorizontalSlider(new Rect(x + 160 + lSliderW, y + 2, lSliderW - 2, 15), speedoModule.LabelS, 0f, 1f);
                    float lV = GUI.HorizontalSlider(new Rect(x + 160 + lSliderW * 2, y + 2, lSliderW - 2, 15), speedoModule.LabelV, 0f, 1f);
                    float lA = GUI.HorizontalSlider(new Rect(x + 160 + lSliderW * 3, y + 2, lSliderW - 2, 15), speedoModule.LabelA, 0f, 1f);

                    if (lH != speedoModule.LabelH || lS != speedoModule.LabelS || lV != speedoModule.LabelV || lA != speedoModule.LabelA)
                    {
                        speedoModule.LabelH = lH;
                        speedoModule.LabelS = lS;
                        speedoModule.LabelV = lV;
                        speedoModule.LabelA = lA;
                        Color col = Color.HSVToRGB(lH, lS, lV);
                        col.a = lA;
                        speedoModule.LabelHex = SpeedometerModule.ColorToHex(col, true);
                        speedoModule.SaveConfig();
                    }
                    y += 26;

                    // --- Слайдеры смещения по оси X ---
                    GUI.Label(new Rect(x, y, contentWidth, 18), $"Label Offset X: {(int)speedoModule.LabelOffsetX}px", UITheme.LabelStyle);
                    y += 18;
                    float newLabelOff = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), speedoModule.LabelOffsetX, 0f, 250f);
                    if (newLabelOff != speedoModule.LabelOffsetX)
                    {
                        speedoModule.LabelOffsetX = newLabelOff;
                        speedoModule.SaveConfig();
                    }
                    y += 22;

                    GUI.Label(new Rect(x, y, contentWidth, 18), $"Value Offset X: {(int)speedoModule.ValueOffsetX}px", UITheme.LabelStyle);
                    y += 18;
                    float newValOff = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), speedoModule.ValueOffsetX, 0f, 250f);
                    if (newValOff != speedoModule.ValueOffsetX)
                    {
                        speedoModule.ValueOffsetX = newValOff;
                        speedoModule.SaveConfig();
                    }
                    y += 28;

                    if (crosshairModule != null)
                    {
                        y = crosshairModule.DrawUI(x, y, contentWidth);
                    }
                    prefTungEnabled.Value = GUI.Toggle(new Rect(x, y, contentWidth, 20), prefTungEnabled.Value, " Enable Tung Tung Sahur Model");
                    y += 22;
                }
                else if (selectedTab == 2 && deathZoneModule != null)
                {
                    string btnVis = deathZoneModule.IsVisualsOn ? "Visuals: ON" : "Visuals: OFF";
                    if (GUI.Button(new Rect(x, y, contentWidth, 25), btnVis, UITheme.ButtonStyle))
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

                    GUI.Label(new Rect(x, y, contentWidth, 20), $"Color (R:{(int)(deathZoneModule.ColorR * 255)} G:{(int)(deathZoneModule.ColorG * 255)} B:{(int)(deathZoneModule.ColorB * 255)})", UITheme.LabelStyle);
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

                    GUI.Label(new Rect(x, y, contentWidth, 20), $"Transparency: {(int)(deathZoneModule.Transparency * 100)}%", UITheme.LabelStyle);
                    y += 18;
                    float newTrans = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), deathZoneModule.Transparency, 0.05f, 1.0f);
                    if (Mathf.Abs(newTrans - deathZoneModule.Transparency) > 0.01f)
                    {
                        deathZoneModule.Transparency = newTrans;
                        deathZoneModule.UpdateMaterialProperties();
                    }
                    y += 25;

                    if (GUI.Button(new Rect(x, y, contentWidth, 22), "🔄 Force Rescan Map Triggers", UITheme.ButtonStyle))
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
                    GUI.Label(new Rect(x, y, contentWidth, 20), $"<b>Game Speed: {Slomo.CurrentScale:F1}x</b>", UITheme.LabelStyle);
                    y += 22;

                    float newScale = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 15), Slomo.CurrentScale, 0.1f, 1.0f);
                    if (Mathf.Abs(newScale - Slomo.CurrentScale) > 0.01f)
                    {
                        Slomo.SetSpeed(newScale);
                    }
                    y += 25;

                    float btnW = (contentWidth - 15f) / 4f;
                    if (GUI.Button(new Rect(x, y, btnW, 25), "0.25x", UITheme.ButtonStyle)) Slomo.SetSpeed(0.25f);
                    if (GUI.Button(new Rect(x + btnW + 5, y, btnW, 25), "0.50x", UITheme.ButtonStyle)) Slomo.SetSpeed(0.50f);
                    if (GUI.Button(new Rect(x + (btnW + 5) * 2, y, btnW, 25), "0.75x", UITheme.ButtonStyle)) Slomo.SetSpeed(0.75f);
                    if (GUI.Button(new Rect(x + (btnW + 5) * 3, y, btnW, 25), "1.00x", UITheme.ButtonStyle)) Slomo.ResetSpeed();
                    y += 35;

                    GUI.Label(new Rect(x, y, contentWidth, 80),
                        "<b>Hotkeys:</b>\n" +
                        " • <b>[</b> / <b>]</b> Or <b>Numpad - / +</b> : Change Speed (-/+ 0.1x)\n" +
                        " • <b>Numpad 0</b> : Reset Speed on 1.0x",
                        UITheme.LabelStyle
                    );
                }
                else if (selectedTab == 10) // Settings
                {
                    GUI.Label(new Rect(x, y, contentWidth, 20), "<b>🎨 UI Theme Presets</b>", UITheme.LabelStyle);
                    y += 22;

                    float presetBtnW = (contentWidth - (UITheme.Presets.Length - 1) * 4f) / UITheme.Presets.Length;
                    for (int p = 0; p < UITheme.Presets.Length; p++)
                    {
                        GUIStyle style = (UITheme.ActivePresetIndex == p) ? UITheme.TabActiveStyle : UITheme.ButtonStyle;
                        if (GUI.Button(new Rect(x + p * (presetBtnW + 4f), y, presetBtnW, 22f), UITheme.Presets[p].Name, style))
                        {
                            UITheme.SetPreset(p);
                        }
                    }
                    y += 28;

                    float colWidth = (contentWidth - 16f) / 2f;

                    // === ЛЕВАЯ КОЛОНКА: Accent Color ===
                    float lx = x;
                    float ly = y;

                    GUI.Label(new Rect(lx, ly, colWidth, 18), "<b>✨ Accent Color (Buttons)</b>", UITheme.LabelStyle);
                    ly += 20;

                    Color currentAccent = UITheme.HexToColor(UITheme.CustomAccentHex);
                    UITheme.DrawColorPreview(new Rect(lx, ly, 18f, 18f), currentAccent);

                    GUI.Label(new Rect(lx + 24f, ly, 45f, 18f), "HEX:", UITheme.LabelStyle);
                    string newAccentHex = GUI.TextField(new Rect(lx + 65f, ly, 75f, 18f), UITheme.CustomAccentHex, UITheme.TextFieldStyle);
                    if (newAccentHex != UITheme.CustomAccentHex)
                    {
                        UITheme.CustomAccentHex = newAccentHex;
                        UITheme.SetAccentColor(UITheme.HexToColor(newAccentHex));
                    }
                    ly += 22;

                    string[] accentSwatches = new string[] { "#CBA6F7", "#00E5FF", "#FF4757", "#2ED573", "#FFA502", "#FF78FA" };
                    float swW = (colWidth - (accentSwatches.Length - 1) * 3f) / accentSwatches.Length;
                    for (int c = 0; c < accentSwatches.Length; c++)
                    {
                        Color swColor = UITheme.HexToColor(accentSwatches[c]);
                        Rect swRect = new Rect(lx + c * (swW + 3f), ly, swW, 16f);
                        UITheme.DrawColorPreview(swRect, swColor);
                        if (GUI.Button(swRect, "", GUIStyle.none)) UITheme.SetAccentColor(swColor);
                    }
                    ly += 22;

                    GUI.Label(new Rect(lx, ly, colWidth, 18f), $"Hue: {(int)(UITheme.AccentH * 360f)}°", UITheme.LabelStyle);
                    ly += 18f;
                    float newAH = GUI.HorizontalSlider(new Rect(lx, ly, colWidth, 14f), UITheme.AccentH, 0f, 1f);
                    ly += 20f;

                    GUI.Label(new Rect(lx, ly, colWidth, 18f), $"Saturation: {(int)(UITheme.AccentS * 100f)}%", UITheme.LabelStyle);
                    ly += 18f;
                    float newAS = GUI.HorizontalSlider(new Rect(lx, ly, colWidth, 14f), UITheme.AccentS, 0f, 1f);
                    ly += 20f;

                    GUI.Label(new Rect(lx, ly, colWidth, 18f), $"Brightness: {(int)(UITheme.AccentV * 100f)}%", UITheme.LabelStyle);
                    ly += 18f;
                    float newAV = GUI.HorizontalSlider(new Rect(lx, ly, colWidth, 14f), UITheme.AccentV, 0f, 1f);
                    ly += 22f;

                    if (Mathf.Abs(newAH - UITheme.AccentH) > 0.001f || Mathf.Abs(newAS - UITheme.AccentS) > 0.001f || Mathf.Abs(newAV - UITheme.AccentV) > 0.001f)
                    {
                        UITheme.AccentH = newAH; UITheme.AccentS = newAS; UITheme.AccentV = newAV;
                        UITheme.CustomAccentHex = UITheme.ColorToHex(UITheme.HSVToRGB(newAH, newAS, newAV));
                        UITheme.ForceRebuild();
                    }

                    // === ПРАВАЯ КОЛОНКА: Background Color ===
                    float rx = x + colWidth + 16f;
                    float ry = y;

                    GUI.Label(new Rect(rx, ry, colWidth, 18), "<b>🌌 Background Color</b>", UITheme.LabelStyle);
                    ry += 20;

                    Color currentBg = UITheme.HexToColor(UITheme.CustomBgHex);
                    UITheme.DrawColorPreview(new Rect(rx, ry, 18f, 18f), currentBg);

                    GUI.Label(new Rect(rx + 24f, ry, 45f, 18f), "HEX:", UITheme.LabelStyle);
                    string newBgHex = GUI.TextField(new Rect(rx + 65f, ry, 75f, 18f), UITheme.CustomBgHex, UITheme.TextFieldStyle);
                    if (newBgHex != UITheme.CustomBgHex)
                    {
                        UITheme.CustomBgHex = newBgHex;
                        UITheme.SetBgColor(UITheme.HexToColor(newBgHex));
                    }
                    ry += 22;

                    string[] bgSwatches = new string[] { "#1E1E2E", "#0D0F18", "#1A1B26", "#050508", "#181818", "#0F172A" };
                    float bgSwW = (colWidth - (bgSwatches.Length - 1) * 3f) / bgSwatches.Length;
                    for (int c = 0; c < bgSwatches.Length; c++)
                    {
                        Color swColor = UITheme.HexToColor(bgSwatches[c]);
                        Rect swRect = new Rect(rx + c * (bgSwW + 3f), ry, bgSwW, 16f);
                        UITheme.DrawColorPreview(swRect, swColor);
                        if (GUI.Button(swRect, "", GUIStyle.none)) UITheme.SetBgColor(swColor);
                    }
                    ry += 22;

                    GUI.Label(new Rect(rx, ry, colWidth, 18f), $"Hue: {(int)(UITheme.BgH * 360f)}°", UITheme.LabelStyle);
                    ry += 18f;
                    float newBH = GUI.HorizontalSlider(new Rect(rx, ry, colWidth, 14f), UITheme.BgH, 0f, 1f);
                    ry += 20f;

                    GUI.Label(new Rect(rx, ry, colWidth, 18f), $"Saturation: {(int)(UITheme.BgS * 100f)}%", UITheme.LabelStyle);
                    ry += 18f;
                    float newBS = GUI.HorizontalSlider(new Rect(rx, ry, colWidth, 14f), UITheme.BgS, 0f, 1f);
                    ry += 20f;

                    GUI.Label(new Rect(rx, ry, colWidth, 18f), $"Brightness: {(int)(UITheme.BgV * 100f)}%", UITheme.LabelStyle);
                    ry += 18f;
                    float newBV = GUI.HorizontalSlider(new Rect(rx, ry, colWidth, 14f), UITheme.BgV, 0f, 1f);
                    ry += 22f;

                    if (Mathf.Abs(newBH - UITheme.BgH) > 0.001f || Mathf.Abs(newBS - UITheme.BgS) > 0.001f || Mathf.Abs(newBV - UITheme.BgV) > 0.001f)
                    {
                        UITheme.BgH = newBH; UITheme.BgS = newBS; UITheme.BgV = newBV;
                        UITheme.CustomBgHex = UITheme.ColorToHex(UITheme.HSVToRGB(newBH, newBS, newBV));
                        UITheme.ForceRebuild();
                    }

                    y = Mathf.Max(ly, ry) + 15f;

                    GUI.Box(new Rect(x, y, contentWidth, 1), "");
                    y += 10;

                    GUI.Label(new Rect(x, y, contentWidth, 18), "<b>⚙️ Window & Controls</b>", UITheme.LabelStyle);
                    y += 20;

                    GUI.Label(new Rect(x, y, contentWidth, 16), $"Menu Transparency: {(int)(windowManager.MenuOpacity * 100)}%", UITheme.LabelStyle);
                    y += 18;
                    float newOpacity = GUI.HorizontalSlider(new Rect(x, y, contentWidth, 12), windowManager.MenuOpacity, 0.2f, 1.0f);
                    if (Mathf.Abs(newOpacity - windowManager.MenuOpacity) > 0.01f)
                    {
                        windowManager.MenuOpacity = newOpacity;
                    }
                    y += 22;

                    DrawRebindButton(x, ref y, contentWidth, "Menu Toggle Key", menuKey, 0);
                    y += 8;

                    if (GUI.Button(new Rect(x, y, 220f, 22f), "↺ Reset Window Position & Size", UITheme.ButtonStyle))
                    {
                        windowManager.ResetWindow();
                    }
                }
                else if (selectedTab == 11) // Вкладка Info
                {
                    GUI.Label(new Rect(x, y, contentWidth, 20f), "<b>📖 Hotkeys & Information</b>", UITheme.LabelStyle);
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

                    GUI.Label(new Rect(x, y, contentWidth, 520f), infoText, UITheme.LabelStyle);
                }
            });
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

    // 1. Блокировка кликов сквозь UI мода в IL2CPP
    [HarmonyPatch]
    public static class BlockClickThrough_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            // Динамически находит метод Raycast в IL2CPP-обертке GraphicRaycaster без жесткого указания параметров
            return AccessTools.FirstMethod(typeof(GraphicRaycaster), m => m.Name == "Raycast");
        }

        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !Main.showMenu;
        }
    }

    // 2. Автоматическое удержание и показ курсора во всех меню
    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.Update))]
    public static class SmartCursorRestore_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Main.showMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            var fps = Object.FindObjectOfType<Il2Cpp.FirstPersonController>();
            ScoreBoardCanvas scoreBoard = Object.FindObjectOfType<ScoreBoardCanvas>();

            bool inScoreBoard = (scoreBoard != null && scoreBoard.gameObject.activeSelf);
            bool isGameplay = (fps != null && fps.enabled && !inScoreBoard);

            // Если игрок не бегает на карте — гарантируем активный и видимый курсор
            if (!isGameplay)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}