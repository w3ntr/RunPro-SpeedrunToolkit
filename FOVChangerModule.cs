using MelonLoader;
using System;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class FovChangerModule
    {
        public bool IsEnabled = true;

        private MelonPreferences_Category configCategory;
        private MelonPreferences_Entry<bool> configFovEnabled;
        private MelonPreferences_Entry<float> configFovValue;

        public float TargetFov = 90f;
        public bool FovOverrideEnabled = true;

        private float defaultFov = 80f;
        private bool hasSavedDefault = false;

        public void Init()
        {
            configCategory = MelonPreferences.CreateCategory("FovChangerMod", "FOV Changer Settings");
            configFovEnabled = configCategory.CreateEntry("FovEnabled", true, "Enable Custom FOV");
            configFovValue = configCategory.CreateEntry("FovValue", 90f, "Custom FOV Value");

            FovOverrideEnabled = configFovEnabled.Value;
            TargetFov = configFovValue.Value;

            MelonLogger.Msg("[FOV Changer] Module initialized.");
        }

        public void Update()
        {
            if (!IsEnabled || !FovOverrideEnabled) return;

            ApplyFov();
        }

        private void ApplyFov()
        {
            // 1. Меняем FOV только у основной 3D-камеры игрока
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                if (!hasSavedDefault)
                {
                    defaultFov = mainCam.fieldOfView;
                    hasSavedDefault = true;
                }

                mainCam.fieldOfView = TargetFov;
                return;
            }

            // 2. Запасной вариант: если Camera.main равен null, ищем только игровые камеры, НЕ трогая UI/HUD
            var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            if (cameras == null) return;

            foreach (var cam in cameras)
            {
                if (cam == null || !cam.enabled) continue;

                // Не трогаем ортографические (2D) и UI-камеры, иначе ломается HUD
                if (cam.orthographic) continue;

                string camName = cam.name.ToLower();
                if (camName.Contains("ui") || camName.Contains("hud") || camName.Contains("gui") || camName.Contains("canvas") || camName.Contains("overlay"))
                {
                    continue;
                }

                cam.fieldOfView = TargetFov;
            }
        }

        public void DrawUI(float startX, float startY, float width)
        {
            if (!IsEnabled) return;

            GUI.Label(new Rect(startX, startY, width, 20), "<b>FOV Settings</b>");

            bool newFovEnabled = GUI.Toggle(new Rect(startX, startY + 22, width, 20), FovOverrideEnabled, " Enable Custom FOV");
            if (newFovEnabled != FovOverrideEnabled)
            {
                FovOverrideEnabled = newFovEnabled;
                configFovEnabled.Value = FovOverrideEnabled;
                configCategory.SaveToFile();
            }

            if (!FovOverrideEnabled) return;

            GUI.Label(new Rect(startX, startY + 45, width, 20), $"FOV: {(int)TargetFov}");
            float newFov = GUI.HorizontalSlider(new Rect(startX, startY + 68, width - 65, 20), TargetFov, 60f, 140f);

            if (Math.Abs(newFov - TargetFov) > 0.1f)
            {
                TargetFov = newFov;
                configFovValue.Value = TargetFov;
                configCategory.SaveToFile();
            }

            if (GUI.Button(new Rect(startX + width - 60, startY + 65, 60, 22), "Reset"))
            {
                TargetFov = hasSavedDefault ? defaultFov : 80f;
                configFovValue.Value = TargetFov;
                configCategory.SaveToFile();
            }
        }
    }
}
