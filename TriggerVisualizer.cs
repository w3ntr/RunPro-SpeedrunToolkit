using System.Collections.Generic;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public static class TriggerVisualizer
    {
        private static bool _enableTriggers = false;
        public static bool EnableTriggers
        {
            get => _enableTriggers;
            set
            {
                if (_enableTriggers == value) return;
                _enableTriggers = value;
                RefreshTriggers();
            }
        }

        private static List<GameObject> activeVisualizers = new List<GameObject>();
        private static Material triggerMaterial;

        // Вызывай этот метод при загрузке новой карты или включении тумблера
        public static void RefreshTriggers()
        {
            ClearVisualizers();

            if (!EnableTriggers) return;

            InitMaterialIfNeeded();

            Collider[] allColliders = Object.FindObjectsOfType<Collider>();
            foreach (var col in allColliders)
            {
                if (col == null || !col.isTrigger) continue;

                string objName = col.gameObject.name.ToLower();
                Color boxColor;

                // 1. Проверка на Jumpbox / Jumper
                if (objName.Contains("jumper") || objName.Contains("jumpbox") || col.GetComponent("Jumper") != null)
                {
                    boxColor = new Color(1f, 0.85f, 0f, 0.35f); // Жёлтый
                }
                // 2. Проверка на Booster
                else if (objName.Contains("booster") || col.GetComponent("Booster") != null)
                {
                    boxColor = new Color(0f, 0.85f, 1f, 0.35f); // Голубой
                }
                // 3. Проверка на Finish
                else if (objName.Contains("finish") || objName.Contains("endlevel") || col.CompareTag("Finish"))
                {
                    boxColor = new Color(0f, 1f, 0.3f, 0.45f); // Зеленый
                }
                else
                {
                    continue; // Пропускаем рядовые триггеры
                }

                CreateVisualizerBox(col, boxColor);
            }
        }

        private static void CreateVisualizerBox(Collider col, Color color)
        {
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.name = "SpeedrunToolkit_TriggerVis";
            Object.DontDestroyOnLoad(vis);

            // Мгновенно удаляем физический коллайдер с нашего визуала
            Object.Destroy(vis.GetComponent<Collider>());

            // Синхронизируем позицию, размер и поворот с оригинальным триггером
            vis.transform.position = col.bounds.center;
            vis.transform.localScale = col.bounds.size;
            vis.transform.rotation = col.transform.rotation;

            Renderer ren = vis.GetComponent<Renderer>();
            if (ren != null && triggerMaterial != null)
            {
                ren.material = new Material(triggerMaterial);
                ren.material.color = color;
            }

            activeVisualizers.Add(vis);
        }

        private static void InitMaterialIfNeeded()
        {
            if (triggerMaterial != null) return;

            // Ищем стандартный шейдер с поддержкой прозрачности
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("GUI/Text Shader");
            if (shader != null)
            {
                triggerMaterial = new Material(shader);
            }
        }

        public static void ClearVisualizers()
        {
            foreach (var vis in activeVisualizers)
            {
                if (vis != null) Object.Destroy(vis);
            }
            activeVisualizers.Clear();
        }
    }
}