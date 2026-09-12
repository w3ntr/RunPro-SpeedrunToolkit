using Il2Cpprunpro.SO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SpeedrunToolkitMod
{
    public class SpeedometerModule
    {
        public bool IsEnabled = true;
        public bool ShowSpeed = true;
        public bool ShowCoords = true;
        public bool ShowAngles = true;
        public bool ShowXP = true;
        public bool HideNativeSpeedo = true;

        public float HudX = 20f;
        public float HudY = 60f;
        public int FontSize = 14;
        public FontStyle FontStyle = FontStyle.Bold;

        // --- HEX Настройки Цветов ---
        public string LabelHex = "#FFFFFF";  // Цвет названий (Speed:, Pos: и т.д.)
        public string ValueHex = "#00E5FF";  // Цвет значений (скорость, координаты)
        public string BgHex = "#000000";     // Цвет фона плашки
        public float BgOpacity = 0.6f;       // Прозрачность фона (0.0f - 1.0f)

        // --- Смещения по оси X для раздельного перемещения ---
        public float LabelOffsetX = 10f;
        public float ValueOffsetX = 110f;

        private GameObject playerObj;
        private Vector3 lastPosition;
        private float currentSpeed;
        private Texture2D bgTexture;
        private List<GameObject> disabledNativeObjects = new List<GameObject>();
        private UnityEngine.Object ranksTargetObj;
        private Il2CppSystem.Reflection.FieldInfo expField;
        private Rigidbody playerRb;
        private CharacterController playerCc;
        private float lastPosTime;

        // --- Custom Text Variables & Config ---
        public static bool EnableCustomText = false;
        public static string CustomTextContent = "Custom Text";
        public static float CustomTextSize = 28f;
        public static Color CustomTextColor = Color.white;
        public static float CustomTextPosX = 20f;
        public static float CustomTextPosY = 0f;
        private static TextMeshProUGUI customTextUI;
        public static FontStyle CustomTextFontStyle = FontStyle.Bold;
        public static string CustomTextHex = "#FFFFFF";
        public static float CustomTextH = 0f, CustomTextS = 0f, CustomTextV = 1f, CustomTextA = 1f;

        // HSV состояния интерфейса спидометра
        public float ValueH = 0.5f, ValueS = 1f, ValueV = 1f, ValueA = 1f;
        public float LabelH = 0.5f, LabelS = 1f, LabelV = 1f, LabelA = 1f;

        public void Init()
        {
            LoadConfig();
            UpdateBgTexture();
            FindPlayerRanksSO();
        }

        public static void InitCustomText(GameObject hudCanvas)
        {
            if (customTextUI != null) return;

            GameObject textObj = new GameObject("HUD_CustomText");
            textObj.transform.SetParent(hudCanvas.transform, false);

            customTextUI = textObj.AddComponent<TextMeshProUGUI>();
            customTextUI.font = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("Arial.ttf"));

            ApplyCustomTextSettings();
        }

        public void SaveConfig()
        {
            PlayerPrefs.SetString("Speedo_LabelHex", LabelHex);
            PlayerPrefs.SetString("Speedo_ValueHex", ValueHex);
            PlayerPrefs.SetString("Speedo_BgHex", BgHex);
            PlayerPrefs.SetFloat("Speedo_BgOpacity", BgOpacity);
            PlayerPrefs.SetFloat("Speedo_LabelOffsetX", LabelOffsetX);
            PlayerPrefs.SetFloat("Speedo_ValueOffsetX", ValueOffsetX);
            PlayerPrefs.SetInt("Speedo_FontSize", FontSize);
            PlayerPrefs.SetInt("Speedo_FontStyle", (int)FontStyle);
            PlayerPrefs.SetInt("Speedo_IsEnabled", IsEnabled ? 1 : 0);
            PlayerPrefs.SetInt("Speedo_ShowSpeed", ShowSpeed ? 1 : 0);
            PlayerPrefs.SetInt("Speedo_ShowCoords", ShowCoords ? 1 : 0);
            PlayerPrefs.SetInt("Speedo_ShowAngles", ShowAngles ? 1 : 0);
            PlayerPrefs.SetInt("Speedo_ShowXP", ShowXP ? 1 : 0);
            PlayerPrefs.SetInt("Speedo_HideNative", HideNativeSpeedo ? 1 : 0);

            // Сохранение Custom Text
            PlayerPrefs.SetInt("CustomText_Enable", EnableCustomText ? 1 : 0);
            PlayerPrefs.SetString("CustomText_Content", CustomTextContent);
            PlayerPrefs.SetFloat("CustomText_Size", CustomTextSize);
            PlayerPrefs.SetFloat("CustomText_PosX", CustomTextPosX);
            PlayerPrefs.SetFloat("CustomText_PosY", CustomTextPosY);
            PlayerPrefs.SetInt("CustomText_FontStyle", (int)CustomTextFontStyle);
            PlayerPrefs.SetString("CustomText_Hex", CustomTextHex);

            PlayerPrefs.Save();
        }

        public void LoadConfig()
        {
            LabelHex = PlayerPrefs.GetString("Speedo_LabelHex", "#FFFFFF");
            ValueHex = PlayerPrefs.GetString("Speedo_ValueHex", "#00E5FF");
            BgHex = PlayerPrefs.GetString("Speedo_BgHex", "#000000");
            BgOpacity = PlayerPrefs.GetFloat("Speedo_BgOpacity", 0.6f);
            LabelOffsetX = PlayerPrefs.GetFloat("Speedo_LabelOffsetX", 10f);
            ValueOffsetX = PlayerPrefs.GetFloat("Speedo_ValueOffsetX", 110f);
            FontSize = PlayerPrefs.GetInt("Speedo_FontSize", 14);
            FontStyle = (FontStyle)PlayerPrefs.GetInt("Speedo_FontStyle", (int)FontStyle.Bold);
            IsEnabled = PlayerPrefs.GetInt("Speedo_IsEnabled", 1) == 1;
            ShowSpeed = PlayerPrefs.GetInt("Speedo_ShowSpeed", 1) == 1;
            ShowCoords = PlayerPrefs.GetInt("Speedo_ShowCoords", 1) == 1;
            ShowAngles = PlayerPrefs.GetInt("Speedo_ShowAngles", 1) == 1;
            ShowXP = PlayerPrefs.GetInt("Speedo_ShowXP", 1) == 1;
            HideNativeSpeedo = PlayerPrefs.GetInt("Speedo_HideNative", 1) == 1;

            // Загрузка Custom Text
            EnableCustomText = PlayerPrefs.GetInt("CustomText_Enable", 1) == 1;
            CustomTextContent = PlayerPrefs.GetString("CustomText_Content", "Custom Text");
            CustomTextSize = PlayerPrefs.GetFloat("CustomText_Size", 28f);
            CustomTextPosX = PlayerPrefs.GetFloat("CustomText_PosX", 20f);
            CustomTextPosY = PlayerPrefs.GetFloat("CustomText_PosY", 0f);
            CustomTextFontStyle = (FontStyle)PlayerPrefs.GetInt("CustomText_FontStyle", (int)FontStyle.Bold);
            CustomTextHex = PlayerPrefs.GetString("CustomText_Hex", "#FFFFFF");

            SyncValueHSVFromHex();
            SyncLabelHSVFromHex();
            SyncCustomTextHSVFromHex();
            UpdateBgTexture();
        }

        public static void SyncCustomTextHSVFromHex()
        {
            if (ColorUtility.TryParseHtmlString(CustomTextHex, out Color col))
            {
                CustomTextColor = col;
                Color.RGBToHSV(col, out CustomTextH, out CustomTextS, out CustomTextV);
                CustomTextA = col.a;
            }
        }

        public static void ApplyCustomTextSettings()
        {
            if (customTextUI == null) return;

            customTextUI.gameObject.SetActive(EnableCustomText);
            if (!EnableCustomText) return;

            customTextUI.text = CustomTextContent;
            customTextUI.fontSize = CustomTextSize;
            customTextUI.color = CustomTextColor;

            RectTransform rect = customTextUI.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(CustomTextPosX, CustomTextPosY);
        }

        private static void SetTextAnchor(RectTransform rect, TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                    rect.anchoredPosition = new Vector2(20, -20);
                    break;
                case TextAnchor.UpperRight:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
                    rect.anchoredPosition = new Vector2(-20, -20);
                    break;
                case TextAnchor.LowerLeft:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
                    rect.anchoredPosition = new Vector2(20, 20);
                    break;
                case TextAnchor.LowerRight:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
                    rect.anchoredPosition = new Vector2(1, 0);
                    rect.anchoredPosition = new Vector2(-20, 20);
                    break;
            }
        }

        public static string ColorToHexRGBA(Color col)
        {
            byte r = (byte)(Mathf.Clamp01(col.r) * 255);
            byte g = (byte)(Mathf.Clamp01(col.g) * 255);
            byte b = (byte)(Mathf.Clamp01(col.b) * 255);
            byte a = (byte)(Mathf.Clamp01(col.a) * 255);
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        public Color ParseColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex)) return defaultColor;

            string formattedHex = hex.Trim();
            if (!formattedHex.StartsWith("#")) formattedHex = "#" + formattedHex;

            if (ColorUtility.TryParseHtmlString(formattedHex, out Color parsed))
            {
                return parsed;
            }
            return defaultColor;
        }

        public void SyncValueHSVFromHex()
        {
            if (ColorUtility.TryParseHtmlString(ValueHex, out Color c))
            {
                Color.RGBToHSV(c, out ValueH, out ValueS, out ValueV);
                ValueA = c.a;
            }
        }

        public void SyncLabelHSVFromHex()
        {
            if (ColorUtility.TryParseHtmlString(LabelHex, out Color c))
            {
                Color.RGBToHSV(c, out LabelH, out LabelS, out LabelV);
                LabelA = c.a;
            }
        }

        public static string ColorToHex(Color color, bool includeAlpha = false)
        {
            byte r = (byte)Mathf.Clamp((int)(color.r * 255f), 0, 255);
            byte g = (byte)Mathf.Clamp((int)(color.g * 255f), 0, 255);
            byte b = (byte)Mathf.Clamp((int)(color.b * 255f), 0, 255);

            if (includeAlpha)
            {
                byte a = (byte)Mathf.Clamp((int)(color.a * 255f), 0, 255);
                return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
            }

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        public void UpdateBgTexture()
        {
            if (bgTexture == null) bgTexture = new Texture2D(1, 1);

            Color bgColor = ParseColor(BgHex, Color.black);
            bgColor.a = BgOpacity;

            bgTexture.SetPixel(0, 0, bgColor);
            bgTexture.Apply();
        }

        public void FindPlayerRanksSO()
        {
            string[] targetClasses = new string[]
            {
                "runpro.PlayerRanks, Assembly-CSharp",
                "runpro.RankManager, Assembly-CSharp",
                "runpro.SO.PlayerRanksSO, Assembly-CSharp"
            };

            foreach (string className in targetClasses)
            {
                Il2CppSystem.Type type = Il2CppSystem.Type.GetType(className);
                if (type == null) continue;

                var found = Resources.FindObjectsOfTypeAll(type);
                if (found != null && found.Length > 0)
                {
                    ranksTargetObj = found[found.Length - 1];

                    var fields = type.GetFields();
                    foreach (var f in fields)
                    {
                        string fName = f.Name.ToLower();
                        if (fName == "playerexp" || fName == "currentexp" || fName == "exp" || fName == "m_exp")
                        {
                            expField = f;
                            return;
                        }
                    }
                }
            }
        }

        public void OnSceneWasLoaded(string sceneName)
        {
            disabledNativeObjects.Clear();
            if (HideNativeSpeedo) ToggleNativeSpeedometer(true);
            FindPlayerRanksSO();
        }

        public void ToggleNativeSpeedometer(bool hide)
        {
            if (hide)
            {
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (go == null) continue;
                    string name = go.name.ToLower();

                    if ((name.Contains("speed") || name.Contains("speedometer") || name.Contains("velbar") || name.Contains("velocity"))
                        && !name.Contains("speedocoords") && !name.Contains("toolkit"))
                    {
                        if (go.activeSelf && !disabledNativeObjects.Contains(go))
                        {
                            disabledNativeObjects.Add(go);
                        }
                        go.SetActive(false);
                    }
                }
            }
            else
            {
                for (int i = disabledNativeObjects.Count - 1; i >= 0; i--)
                {
                    if (disabledNativeObjects[i] != null)
                    {
                        disabledNativeObjects[i].SetActive(true);
                    }
                }
                disabledNativeObjects.Clear();
            }
        }

        public void Update()
        {
            if (HideNativeSpeedo && Time.frameCount % 180 == 0)
            {
                ToggleNativeSpeedometer(true);
            }

            if (Time.frameCount % 60 == 0)
            {
                FindPlayerRanksSO();
            }

            if (!IsEnabled) return;

            if (playerObj == null)
            {
                FindPlayer();
                return;
            }

            Vector3 currentPos = playerObj.transform.position;
            Vector3 horizontalDelta = new Vector3(currentPos.x - lastPosition.x, 0f, currentPos.z - lastPosition.z);
            float sqrDist = horizontalDelta.sqrMagnitude;

            if (sqrDist > 0.0001f)
            {
                float timePassed = Time.time - lastPosTime;
                if (timePassed > 0f)
                {
                    float realSpeed = Mathf.Sqrt(sqrDist) / timePassed;
                    currentSpeed = Mathf.Lerp(currentSpeed, realSpeed, Time.deltaTime * 12f);
                }
                lastPosition = currentPos;
                lastPosTime = Time.time;
            }
            else if (Time.time - lastPosTime > 0.12f)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 10f);
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (go != null && (go.name.ToLower().Contains("player") || go.name.ToLower().Contains("character")))
                    {
                        player = go;
                        break;
                    }
                }
            }

            if (player != null)
            {
                playerObj = player;
                playerRb = player.GetComponent<Rigidbody>() ?? player.GetComponentInChildren<Rigidbody>();
                playerCc = player.GetComponent<CharacterController>() ?? player.GetComponentInChildren<CharacterController>();
                lastPosition = player.transform.position;
            }
        }

        public void OnGUI()
        {
            // Отрисовка Custom Text (один блок, дублирующий убран)
            if (EnableCustomText && !string.IsNullOrEmpty(CustomTextContent))
            {
                Color textColor;
                if (!ColorUtility.TryParseHtmlString(CustomTextHex, out textColor))
                {
                    textColor = Color.white;
                }

                GUIStyle customTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = (int)CustomTextSize,
                    fontStyle = CustomTextFontStyle,
                    alignment = TextAnchor.UpperLeft
                };
                customTextStyle.normal.textColor = textColor;

                Vector2 size = customTextStyle.CalcSize(new GUIContent(CustomTextContent));
                GUI.Label(new Rect(CustomTextPosX, CustomTextPosY, size.x + 50f, size.y + 20f), CustomTextContent, customTextStyle);
            }

            if (!IsEnabled || playerObj == null) return;

            int lines = 0;
            if (ShowSpeed) lines++;
            if (ShowCoords) lines++;
            if (ShowAngles) lines++;
            if (ShowXP) lines++;
            if (lines == 0) return;

            float lineHeight = FontSize + 8f;
            float width = FontSize * 17f;
            if (width < 260f) width = 260f;
            float height = lines * lineHeight + 12f;

            GUI.DrawTexture(new Rect(HudX, HudY, width, height), bgTexture);

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = FontSize;
            labelStyle.fontStyle = FontStyle;
            labelStyle.normal.textColor = ParseColor(LabelHex, Color.white);

            GUIStyle valStyle = new GUIStyle(labelStyle);
            valStyle.normal.textColor = ParseColor(ValueHex, new Color(0f, 0.9f, 1f));

            float currentY = HudY + 6f;

            if (ShowSpeed)
            {
                GUI.Label(new Rect(HudX + LabelOffsetX, currentY, width * 0.4f, lineHeight), "Speed:", labelStyle);
                GUI.Label(new Rect(HudX + ValueOffsetX, currentY, width * 0.6f, lineHeight), $"{currentSpeed:F2} u/s", valStyle);
                currentY += lineHeight;
            }

            if (ShowCoords)
            {
                Vector3 pos = playerObj.transform.position;
                GUI.Label(new Rect(HudX + LabelOffsetX, currentY, width * 0.3f, lineHeight), "Pos:", labelStyle);
                GUI.Label(new Rect(HudX + ValueOffsetX, currentY, width * 0.7f, lineHeight), $"X:{pos.x:F1}  Y:{pos.y:F1}  Z:{pos.z:F1}", valStyle);
                currentY += lineHeight;
            }

            if (ShowAngles)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 rot = cam.transform.eulerAngles;
                    float pitch = rot.x > 180f ? rot.x - 360f : rot.x;
                    float yaw = rot.y;

                    GUI.Label(new Rect(HudX + LabelOffsetX, currentY, width * 0.3f, lineHeight), "Look:", labelStyle);
                    GUI.Label(new Rect(HudX + ValueOffsetX, currentY, width * 0.75f, lineHeight), $"P:{pitch:F2}°  Y:{yaw:F2}°", valStyle);
                    currentY += lineHeight;
                }
            }

            if (ShowXP)
            {
                int totalExp = 0;

                if (ranksTargetObj != null && expField != null)
                {
                    totalExp = expField.GetValue(ranksTargetObj).Unbox<int>();
                }
                else
                {
                    totalExp = PlayerPrefs.GetInt("PlayerExp", 300);
                }

                int level = (int)(System.Math.Pow(totalExp / 400f, 0.75) * System.Math.Pow(1.3333333730697632, 0.75));
                int minExp = (int)(400f * (0.75 * System.Math.Pow(level, 1.3333333730697632)));
                int nextExp = (int)(400f * (0.75 * System.Math.Pow(level + 1, 1.3333333730697632)));

                int currentProgress = totalExp - minExp;
                int neededForNext = nextExp - minExp;

                GUI.Label(new Rect(HudX + LabelOffsetX, currentY, width * 0.3f, lineHeight), "XP:", labelStyle);
                GUI.Label(new Rect(HudX + ValueOffsetX, currentY, width * 0.75f, lineHeight), $"Lvl {level} ({currentProgress}/{neededForNext})", valStyle);
            }
        }
    }
}