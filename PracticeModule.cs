using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace SpeedrunToolkitMod
{
    public class PracticeModule
    {
        public bool IsEnabled = true;

        private Vector3 savedPosition;
        private bool hasSavedPosition = false;

        private Vector3 spawnPosition;
        private bool hasSpawnPosition = false;

        private GameObject playerObj;
        private GUIStyle watermarkStyle;

        private List<Collider> disabledColliders = new List<Collider>();
        private List<MonoBehaviour> disabledComponents = new List<MonoBehaviour>();

        public void Init()
        {
        }

        public void OnSceneWasLoaded(string sceneName)
        {
            ResetCheckpoint("Scene loaded");
            hasSpawnPosition = false;
        }

        public void ResetCheckpoint(string reason = "")
        {
            hasSavedPosition = false;
            savedPosition = Vector3.zero;
            RestoreFinishAndTimer();
        }

        public void Update()
        {
            if (!IsEnabled) return;

            if (playerObj == null)
            {
                FindPlayer();
            }

            if (playerObj != null && !hasSpawnPosition)
            {
                spawnPosition = playerObj.transform.position;
                hasSpawnPosition = true;
            }

            if (hasSavedPosition)
            {
                BlockFinishAndTimer();
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

            savedPosition = playerObj.transform.position;
            hasSavedPosition = true;
        }

        public void LoadPlayerPosition()
        {
            if (!hasSavedPosition || playerObj == null) return;

            TeleportPlayer(savedPosition);
            BlockFinishAndTimer();
        }

        public void TeleportToSpawn()
        {
            if (!hasSpawnPosition || playerObj == null) return;

            TeleportPlayer(spawnPosition);
        }

        private void TeleportPlayer(Vector3 pos)
        {
            playerObj.transform.position = pos;

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void BlockFinishAndTimer()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var mono in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
            {
                if (mono == null) continue;

                // Переменная объявляется сразу на входе в цикл
                string typeName = mono.GetType().Name.ToLower();
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

                foreach (var prop in type.GetProperties(flags))
                {
                    if (!prop.CanWrite) continue;
                    string pname = prop.Name.ToLower();
                    if (pname.Contains("starttime") || pname.Contains("start_time") || pname.Contains("timestart"))
                    {
                        if (prop.PropertyType == typeof(float)) prop.SetValue(mono, Time.time, null);
                        else if (prop.PropertyType == typeof(double)) prop.SetValue(mono, (double)Time.time, null);
                    }
                    else if (pname == "time" || pname == "currenttime" || pname == "elapsedtime" || pname == "leveltime" || pname == "timer")
                    {
                        if (prop.PropertyType == typeof(float)) prop.SetValue(mono, 0f, null);
                        else if (prop.PropertyType == typeof(double)) prop.SetValue(mono, 0.0, null);
                    }
                }

                if (mono.enabled)
                {
                    if (typeName.Contains("finish") || typeName.Contains("win") || typeName.Contains("leaderboard") || typeName.Contains("score") || typeName.Contains("endlevel"))
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

                if (gName.Contains("finish") || gName.Contains("end") || gName.Contains("win") || gName.Contains("goal") || gName.Contains("checker"))
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

        private void RestoreFinishAndTimer()
        {
            for (int i = disabledColliders.Count - 1; i >= 0; i--)
            {
                if (disabledColliders[i] != null)
                {
                    disabledColliders[i].enabled = true;
                }
            }
            disabledColliders.Clear();

            for (int i = disabledComponents.Count - 1; i >= 0; i--)
            {
                if (disabledComponents[i] != null)
                {
                    disabledComponents[i].enabled = true;
                }
            }
            disabledComponents.Clear();
        }

        public void OnGUI()
        {
            if (hasSavedPosition)
            {
                if (watermarkStyle == null)
                {
                    watermarkStyle = new GUIStyle();
                    watermarkStyle.fontSize = 18;
                    watermarkStyle.fontStyle = FontStyle.Bold;
                    watermarkStyle.normal.textColor = new Color(1f, 0.25f, 0.25f, 0.9f);
                }

                float width = 180f;
                float height = 30f;
                float x = Screen.width - width - 20f;
                float y = Screen.height - height - 15f;

                GUI.Label(new Rect(x, y, width, height), "• PRACTICE MODE", watermarkStyle);
            }
        }
    }
}