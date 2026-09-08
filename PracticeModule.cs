using Il2Cpp;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpeedrunToolkitMod
{
    public class PracticeModule
    {
        public bool IsEnabled = true;

        public struct Checkpoint
        {
            public Vector3 position;
            public Vector3 velocity;
            public float pitch;
            public float yaw;
            public bool isValid;
        }

        public const int MaxSlots = 5;
        public Checkpoint[] slots = new Checkpoint[MaxSlots];
        public int currentSlotIndex = 0;

        public float GravityScale = 1.0f;
        public bool IsBlocked = false;

        private Vector3 spawnPosition;
        private bool hasSpawnPosition = false;

        private GameObject playerObj;
        private GUIStyle watermarkStyle;

        private List<Collider> disabledColliders = new List<Collider>();
        private List<MonoBehaviour> disabledComponents = new List<MonoBehaviour>();

        public Checkpoint CurrentSlot => slots[currentSlotIndex];

        public void Init() { }

        public void OnSceneWasLoaded(string sceneName)
        {
            ResetAllCheckpoints("Scene loaded");
            hasSpawnPosition = false;
        }

        public bool HasAnySavedPosition()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (slots[i].isValid) return true;
            }
            return false;
        }


        public void ResetCurrentCheckpoint()
        {
            slots[currentSlotIndex] = new Checkpoint();
            CheckAndRestoreIfClean();
        }

        public void ResetAllCheckpoints(string reason = "")
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                slots[i] = new Checkpoint();
            }
            RestoreFinishAndTimer();
        }

        private void CheckAndRestoreIfClean()
        {
            if (!HasAnySavedPosition() && Mathf.Approximately(GravityScale, 1.0f))
            {
                RestoreFinishAndTimer();
            }
        }

        public void SelectSlot(int index)
        {
            if (index >= 0 && index < MaxSlots)
                currentSlotIndex = index;
        }

        public void NextSlot()
        {
            currentSlotIndex = (currentSlotIndex + 1) % MaxSlots;
        }

        public void PrevSlot()
        {
            currentSlotIndex = (currentSlotIndex - 1 + MaxSlots) % MaxSlots;
        }

        public void Update()
        {
            if (!IsEnabled) return;

            if (playerObj == null) FindPlayer();

            if (playerObj != null && !hasSpawnPosition)
            {
                spawnPosition = playerObj.transform.position;
                hasSpawnPosition = true;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (ChatUtils.IsChatFocused()) return;
                LoadPlayerPosition();
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
                {
                    if (go != null && (go.name.ToLower().Contains("player") || go.name.ToLower().Contains("character")))
                    {
                        player = go;
                        break;
                    }
                }
            }
            if (player != null) playerObj = player;
        }

        public static class ChatUtils
        {
            public static bool IsChatFocused()
            {
                var es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject != null)
                {
                    var input = es.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>();
                    if (input != null && input.isFocused) return true;
                }

                var chat = Object.FindObjectOfType<CanvasTextChat>();
                if (chat != null && chat.txtInput != null && chat.txtInput.isFocused)
                {
                    return true;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new System.Type[] { typeof(KeyCode) })]
        public static class InputChatBlock_Patch
        {
            private static CanvasTextChat cachedChat;
            private static float lastSearchTime;

            [HarmonyPrefix]
            public static bool Prefix(KeyCode key, ref bool __result)
            {
                if (key != KeyCode.R) return true;

                if (cachedChat == null || Time.unscaledTime - lastSearchTime > 1.0f)
                {
                    cachedChat = Object.FindObjectOfType<CanvasTextChat>();
                    lastSearchTime = Time.unscaledTime;
                }

                if (cachedChat != null && cachedChat.txtInput != null && cachedChat.txtInput.isFocused)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        public void SavePlayerPosition()
        {
            if (playerObj == null) FindPlayer();
            if (playerObj == null) return;

            Vector3 vel = Vector3.zero;
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null) vel = rb.velocity;

            float pitch = 0f;
            float yaw = playerObj.transform.eulerAngles.y;

            Camera cam = Camera.main;
            if (cam == null) cam = playerObj.GetComponentInChildren<Camera>();

            if (cam != null)
            {
                Vector3 rot = cam.transform.eulerAngles;
                pitch = rot.x > 180f ? rot.x - 360f : rot.x;
                yaw = rot.y;
            }

            slots[currentSlotIndex] = new Checkpoint
            {
                position = playerObj.transform.position,
                velocity = vel,
                pitch = pitch,
                yaw = yaw,
                isValid = true
            };

            BlockFinishAndTimer();
        }

        public void LoadPlayerPosition()
        {
            if (!CurrentSlot.isValid || playerObj == null) return;

            TeleportPlayer(CurrentSlot.position, CurrentSlot.velocity, CurrentSlot.pitch, CurrentSlot.yaw, true);
            BlockFinishAndTimer();
        }

        public void TeleportToSpawn()
        {
            if (!hasSpawnPosition || playerObj == null) return;

            TeleportPlayer(spawnPosition, Vector3.zero, 0f, 0f, false);
        }

        public void TeleportToCrosshair()
        {
            if (playerObj == null) FindPlayer();
            if (playerObj == null) return;

            Camera cam = Camera.main;
            if (cam == null) cam = playerObj.GetComponentInChildren<Camera>();
            if (cam == null) return;

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 1000f))
            {
                Vector3 targetPos = hit.point + hit.normal * 0.2f + Vector3.up * 0.8f;
                TeleportPlayer(targetPos, Vector3.zero, 0f, 0f, false);
                BlockFinishAndTimer();
            }
        }

        private void TeleportPlayer(Vector3 pos, Vector3 vel, float pitch, float yaw, bool applyAngles)
        {
            playerObj.transform.position = pos;

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = vel;
                rb.angularVelocity = Vector3.zero;
            }

            if (!applyAngles) return;

            // 1. Применяем повороты к объектам сцены
            playerObj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Camera cam = Camera.main;
            if (cam == null) cam = playerObj.GetComponentInChildren<Camera>();

            if (cam != null)
            {
                cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            // 2. Инициализируем m_MouseLook, чтобы сбросить целевые кватернионы в контроллере
            var fps = playerObj.GetComponent<Il2Cpp.FirstPersonController>();
            if (fps != null)
            {
                try
                {
                    fps.m_YRotation = yaw;
                }
                catch { }

                var mouseLook = fps.m_MouseLook;
                if (mouseLook != null && cam != null)
                {
                    try
                    {
                        mouseLook.Init(playerObj.transform, cam.transform);
                    }
                    catch
                    {
                        // Резервная рефлексия для кастомных сборок
                        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                        Quaternion charRot = playerObj.transform.localRotation;
                        Quaternion camRot = cam.transform.localRotation;

                        foreach (var f in mouseLook.GetType().GetFields(flags))
                        {
                            if (f.FieldType == typeof(Quaternion))
                            {
                                string fName = f.Name.ToLower();
                                if (fName.Contains("char") || fName.Contains("player") || fName.Contains("body"))
                                    f.SetValue(mouseLook, charRot);
                                else if (fName.Contains("cam") || fName.Contains("head"))
                                    f.SetValue(mouseLook, camRot);
                            }
                        }
                    }
                }
            }
        }

        public void BlockFinishAndTimer()
        {
            IsBlocked = true;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var mono in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (mono == null) continue;

                GameObject go = mono.gameObject;
                string gName = go.name.ToLower();
                string typeName = mono.GetType().Name.ToLower();

                if (go.CompareTag("MainCamera") || go.CompareTag("Player") ||
                    go.GetComponent<Camera>() != null ||
                    gName.Contains("camera") || gName.Contains("player") || gName.Contains("canvas") || gName.Contains("hud") ||
                    typeName.Contains("camera") || typeName.Contains("look") || typeName.Contains("input") || typeName.Contains("window"))
                {
                    continue;
                }

                var type = mono.GetType();

                foreach (var field in type.GetFields(flags))
                {
                    string fname = field.Name.ToLower();
                    if (fname.Contains("starttime") || fname.Contains("start_time") || fname.Contains("timestart"))
                    {
                        if (field.FieldType == typeof(float)) field.SetValue(mono, Time.time);
                        else if (field.FieldType == typeof(double)) field.SetValue(mono, (double)Time.time);
                    }
                    else if (fname == "time" || fname == "currenttime" || fname == "elapsedtime" || fname == "leveltime" || fname == "timer")
                    {
                        if (field.FieldType == typeof(float)) field.SetValue(mono, 0f);
                        else if (field.FieldType == typeof(double)) field.SetValue(mono, 0.0);
                    }
                }

                if (mono.enabled)
                {
                    if (typeName.Contains("finish") || typeName.Contains("leaderboard") || typeName.Contains("endlevel") || typeName.Contains("wintrigger") || typeName.Contains("winzone"))
                    {
                        mono.enabled = false;
                        if (!disabledComponents.Contains(mono)) disabledComponents.Add(mono);
                    }
                }
            }

            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (go == null) continue;
                string gName = go.name.ToLower();

                if (go.CompareTag("MainCamera") || go.CompareTag("Player") || gName.Contains("camera") || gName.Contains("player"))
                    continue;

                if (gName.Contains("finish") || gName.Contains("endzone") || gName.Contains("winzone") || gName.Contains("goal") || gName.Contains("checker"))
                {
                    var colliders = go.GetComponentsInChildren<Collider>(true);
                    foreach (var c in colliders)
                    {
                        if (c != null && c.enabled)
                        {
                            c.enabled = false;
                            if (!disabledColliders.Contains(c)) disabledColliders.Add(c);
                        }
                    }
                }
            }
        }

        public void RestoreFinishAndTimer()
        {
            IsBlocked = false;

            for (int i = disabledColliders.Count - 1; i >= 0; i--)
            {
                if (disabledColliders[i] != null) disabledColliders[i].enabled = true;
            }
            disabledColliders.Clear();

            for (int i = disabledComponents.Count - 1; i >= 0; i--)
            {
                if (disabledComponents[i] != null) disabledComponents[i].enabled = true;
            }
            disabledComponents.Clear();
        }

        public void OnGUI()
        {
            if (!IsEnabled) return;

            if (HasAnySavedPosition() || IsBlocked)
            {
                if (watermarkStyle == null)
                {
                    watermarkStyle = new GUIStyle();
                    watermarkStyle.fontSize = 18;
                    watermarkStyle.fontStyle = FontStyle.Bold;
                    watermarkStyle.normal.textColor = new Color(1f, 0.25f, 0.25f, 0.9f);
                    watermarkStyle.alignment = TextAnchor.MiddleRight;
                }

                float width = 400f;
                float height = 30f;
                float x = Screen.width - width - 20f;
                float y = Screen.height - height - 15f;

                string slotStatus = CurrentSlot.isValid ? $"[Slot {currentSlotIndex + 1}]" : $"[Slot {currentSlotIndex + 1} - Empty]";
                GUI.Label(new Rect(x, y, width, height), $"• PRACTICE MODE {slotStatus}", watermarkStyle);
            }
        }
    }
}