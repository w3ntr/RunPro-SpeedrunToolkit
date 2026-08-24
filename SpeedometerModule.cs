using UnityEngine;
using System.Collections.Generic;

namespace SpeedrunToolkitMod
{
    public class SpeedometerModule
    {
        public bool IsEnabled = true;
        public bool ShowSpeed = true;
        public bool ShowCoords = true;
        public bool ShowAngles = true; // Поле для отображения углов обзора
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
        }

        public void UpdateBgTexture()
        {
            if (bgTexture == null) bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, BgOpacity));
            bgTexture.Apply();
        }

        public void OnSceneWasLoaded(string sceneName)
        {
            disabledNativeObjects.Clear();
            if (HideNativeSpeedo) ToggleNativeSpeedometer(true);
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
            if (lines == 0) return;

            float lineHeight = FontSize + 8f;
            float width = FontSize * 16f;
            if (width < 240f) width = 240f;
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
                }
            }
        }
    }
}
