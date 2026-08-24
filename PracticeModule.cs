using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace SpeedrunToolkitMod
{
    public class PracticeModule
    {
        public bool IsEnabled = true;

        public struct Checkpoint
        {
            public Vector3 position;
            public Vector3 velocity;
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
            ResetGravity();
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

        public void SetGravityScale(float scale)
        {
            GravityScale = scale;
            Physics.gravity = new Vector3(0f, -9.81f * scale, 0f);

            if (!Mathf.Approximately(scale, 1.0f))
            {
                BlockFinishAndTimer();
            }
            else
            {
                CheckAndRestoreIfClean();
            }
        }

        public void ResetGravity()
        {
            GravityScale = 1.0f;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            CheckAndRestoreIfClean();
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

        public void SavePlayerPosition()
        {
            if (playerObj == null) FindPlayer();
            if (playerObj == null) return;

            Vector3 vel = Vector3.zero;
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null) vel = rb.velocity;

            slots[currentSlotIndex] = new Checkpoint
            {
                position = playerObj.transform.position,
                velocity = vel,
                isValid = true
            };

            BlockFinishAndTimer();
        }

        public void LoadPlayerPosition()
        {
            if (!CurrentSlot.isValid || playerObj == null) return;

            TeleportPlayer(CurrentSlot.position, CurrentSlot.velocity);
            BlockFinishAndTimer();
        }

        public void TeleportToSpawn()
        {
            if (!hasSpawnPosition || playerObj == null) return;

            TeleportPlayer(spawnPosition, Vector3.zero);
        }

        public void TeleportToCrosshair()
        {
            if (playerObj == null) FindPlayer();
            if (playerObj == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 1000f))
            {
                Vector3 targetPos = hit.point + hit.normal * 0.2f + Vector3.up * 0.8f;
                TeleportPlayer(targetPos, Vector3.zero);
                BlockFinishAndTimer();
            }
        }

        private void TeleportPlayer(Vector3 pos, Vector3 vel)
        {
            playerObj.transform.position = pos;

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = vel;
                rb.angularVelocity = Vector3.zero;
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

                // Пропускаем камеры, игрока, ввод и элементы интерфейса
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
