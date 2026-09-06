using Il2Cpprunpro.SO;
using System.Collections.Generic;
using UnityEngine;

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
        public int ColorIndex = 0;
        public float BgOpacity = 0.6f;

        private GameObject playerObj;
        private Vector3 lastPosition;
        private float currentSpeed;
        private Texture2D bgTexture;
        private List<GameObject> disabledNativeObjects = new List<GameObject>();
        private UnityEngine.Object ranksTargetObj;
        private Il2CppSystem.Reflection.FieldInfo expField;

        public static readonly string[] ColorNames = { "Cyan", "White", "Yellow", "Lime", "Orange", "Pink", "Red" };
        public static readonly Color[] Colors = {
            new Color(0f, 0.9f, 1f),
            Color.white,
            Color.yellow,
            Color.green,
            new Color(1f, 0.5f, 0f),
            new Color(1f, 0.4f, 0.8f),
            Color.red
        };

        public void Init()
        {
            UpdateBgTexture();
            FindPlayerRanksSO();
        }

        public void UpdateBgTexture()
        {
            if (bgTexture == null) bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, BgOpacity));
            bgTexture.Apply();
        }

        public void FindPlayerRanksSO()
        {
            // Классы, в которых игра может держать реальный runtime XP
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
                    // Берем самый последний активный инстанс на сцене
                    ranksTargetObj = found[found.Length - 1];

                    // Ищем подходящее поле XP
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

            // Периодически обновляем инстанс XP для работы в реальном времени при беге
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
            if (lastPosition != Vector3.zero && Time.deltaTime > 0)
            {
                Vector3 horizontalDelta = new Vector3(currentPos.x - lastPosition.x, 0, currentPos.z - lastPosition.z);
                float targetSpeed = horizontalDelta.magnitude / Time.deltaTime;
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
            }
            lastPosition = currentPos;
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
                lastPosition = player.transform.position;
            }
        }

        public void OnGUI()
        {
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
            labelStyle.normal.textColor = Color.white;

            GUIStyle valStyle = new GUIStyle(labelStyle);
            valStyle.normal.textColor = Colors[ColorIndex];

            float currentY = HudY + 6f;

            if (ShowSpeed)
            {
                GUI.Label(new Rect(HudX + 10f, currentY, width * 0.4f, lineHeight), "Speed:", labelStyle);
                GUI.Label(new Rect(HudX + width * 0.35f, currentY, width * 0.6f, lineHeight), $"{currentSpeed:F2} u/s", valStyle);
                currentY += lineHeight;
            }

            if (ShowCoords)
            {
                Vector3 pos = playerObj.transform.position;
                GUI.Label(new Rect(HudX + 10f, currentY, width * 0.3f, lineHeight), "Pos:", labelStyle);
                GUI.Label(new Rect(HudX + width * 0.25f, currentY, width * 0.7f, lineHeight), $"X:{pos.x:F1}  Y:{pos.y:F1}  Z:{pos.z:F1}", valStyle);
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

                    GUI.Label(new Rect(HudX + 10f, currentY, width * 0.3f, lineHeight), "Look:", labelStyle);
                    GUI.Label(new Rect(HudX + width * 0.25f, currentY, width * 0.75f, lineHeight), $"P:{pitch:F2}°  Y:{yaw:F2}°", valStyle);
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

                GUI.Label(new Rect(HudX + 10f, currentY, width * 0.3f, lineHeight), "XP:", labelStyle);
                GUI.Label(new Rect(HudX + width * 0.25f, currentY, width * 0.75f, lineHeight), $"Lvl {level} ({currentProgress}/{neededForNext})", valStyle);
            }
        }
    }
}