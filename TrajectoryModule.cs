using Il2Cpp;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public static class TrajectoryModule
    {
        public static bool EnableTrajectory = false;
        public static bool ShowOnlyLandingMarker = false; // Включает только зеленый/красный кружок без линии!

        private static GameObject trajectoryObject;
        private static LineRenderer lineRenderer;
        private static GameObject targetMarker;
        private static Renderer markerRenderer;

        private static GameObject playerObj;
        private static CharacterController charController;
        private static FirstPersonController fpcInstance;

        private static FieldInfo jumpSpeedField;
        private static FieldInfo gravityMultField;

        public static void Update()
        {
            if (!EnableTrajectory)
            {
                if (lineRenderer != null && lineRenderer.enabled) lineRenderer.enabled = false;
                if (targetMarker != null && targetMarker.activeSelf) targetMarker.SetActive(false);
                return;
            }

            InitObjectsIfNeeded();

            if (playerObj == null || charController == null || fpcInstance == null)
            {
                fpcInstance = Object.FindObjectOfType<FirstPersonController>();
                if (fpcInstance != null)
                {
                    playerObj = fpcInstance.gameObject;
                    charController = playerObj.GetComponent<CharacterController>();

                    var type = typeof(FirstPersonController);
                    jumpSpeedField = type.GetField("m_JumpSpeed", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    gravityMultField = type.GetField("m_GravityMultiplier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                }
            }

            if (playerObj != null && charController != null)
            {
                CalculateAndDraw();
            }
        }

        private static void InitObjectsIfNeeded()
        {
            if (trajectoryObject == null)
            {
                trajectoryObject = new GameObject("SpeedrunToolkit_Trajectory");
                Object.DontDestroyOnLoad(trajectoryObject);

                lineRenderer = trajectoryObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.03f;
                lineRenderer.endWidth = 0.03f;
                lineRenderer.useWorldSpace = true;
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;

                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
                if (shader != null) lineRenderer.material = new Material(shader);
            }

            if (targetMarker == null)
            {
                // Для маркера создаем плоский диск (Cylinder)
                targetMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                targetMarker.name = "SpeedrunToolkit_LandingMarker";
                Object.DontDestroyOnLoad(targetMarker);

                Object.Destroy(targetMarker.GetComponent<Collider>());

                // Делаем тонким плоским кругом на поверхности
                targetMarker.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);
                markerRenderer = targetMarker.GetComponent<Renderer>();

                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Hidden/Internal-Colored");
                if (shader != null) markerRenderer.material = new Material(shader);
            }
        }

        private static void CalculateAndDraw()
        {
            Vector3 startPos = playerObj.transform.position;

            float jumpSpeed = 10f;
            if (jumpSpeedField != null && fpcInstance != null)
                jumpSpeed = (float)jumpSpeedField.GetValue(fpcInstance);

            float gravityMult = 2f;
            if (gravityMultField != null && fpcInstance != null)
                gravityMult = (float)gravityMultField.GetValue(fpcInstance);

            Vector3 velocity = charController.velocity;

            if (charController.isGrounded)
            {
                velocity.y = jumpSpeed;
                if (new Vector2(velocity.x, velocity.z).sqrMagnitude < 0.1f)
                {
                    Vector3 forward = playerObj.transform.forward;
                    velocity.x = forward.x * 8f;
                    velocity.z = forward.z * 8f;
                }
            }

            List<Vector3> points = new List<Vector3> { startPos };
            Vector3 currentPos = startPos;
            Vector3 effectiveGravity = Physics.gravity * gravityMult;

            // 1. Увеличиваем радиус хитбокса на 15% для запаса (учитываем задевание углов)
            float playerRadius = charController.radius * 1.15f;

            // 2. Сопротивление воздуха (теряем ~1.5% горизонтальной скорости каждый шаг)
            float airDragPerStep = 0.985f;

            int steps = 100;
            float stepTime = 0.03f;
            bool hitLandableSurface = false;
            Vector3 landingPoint = Vector3.zero;

            for (int i = 1; i <= steps; i++)
            {
                float t = i * stepTime;

                // Постепенно гасим горизонтальную скорость для компенсации воздуха/микрофризов
                velocity.x *= airDragPerStep;
                velocity.z *= airDragPerStep;

                Vector3 nextPos = currentPos + velocity * stepTime + 0.5f * effectiveGravity * (stepTime * stepTime);

                // Обновляем виртуальную вертикальную скорость под действием гравитации
                velocity += effectiveGravity * stepTime;

                Vector3 dir = nextPos - currentPos;
                float dist = dir.magnitude;

                if (dist > 0.001f)
                {
                    if (Physics.SphereCast(currentPos, playerRadius, dir.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.gameObject != playerObj)
                        {
                            landingPoint = hit.point;
                            points.Add(hit.point);

                            // Считаем поверхностью только если угол наклона меньше 45 градусов к горизонту
                            if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
                            {
                                hitLandableSurface = true;
                            }
                            break;
                        }
                    }
                }

                points.Add(nextPos);
                currentPos = nextPos;
            }

            lineRenderer.enabled = !ShowOnlyLandingMarker;
            if (!ShowOnlyLandingMarker)
            {
                lineRenderer.positionCount = points.Count;
                lineRenderer.SetPositions(points.ToArray());
            }

            if (targetMarker != null)
            {
                targetMarker.SetActive(true);
                targetMarker.transform.position = hitLandableSurface ? landingPoint + Vector3.up * 0.02f : points[points.Count - 1];

                if (markerRenderer != null)
                {
                    markerRenderer.material.color = hitLandableSurface ? new Color(0f, 1f, 0.2f, 0.8f) : new Color(1f, 0.1f, 0.1f, 0.8f);
                }
            }
        }
    }
}