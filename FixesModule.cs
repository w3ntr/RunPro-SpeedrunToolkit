using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class FixesModule
    {
        public static bool EnableJumperFix = false;
        public static bool EnableBoosterFix = false;
        public static bool EnableLedgeFix = true;

        public static float JumperForceMultiplier = 1.025f;
        public static float BoosterForceMultiplier = 1.0175f; // Идеальное базовое значение

        // Безопасные границы для Джампера
        public const float MinJumperLimit = 1.0000f;
        public const float MaxJumperLimit = 1.0250f;

        // Новые безопасные границы для Бустера
        public const float MinBoosterLimit = 1.0000f;
        public const float MaxBoosterLimit = 1.0180f; // Лимит (абуз начинается с 1.0181f)

        private TimerModule timerModule = new TimerModule();

        public void OnUpdate()
        {
            timerModule.OnUpdate();
        }

        // Отрисовка прозрачного водяного знака (~60% видимости)
        public void OnGUI()
        {
            if (EnableBoosterFix)
            {
                GUIStyle watermarkStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight
                };

                // Проверка на абуз (выше 1.0180x)
                bool isAbused = BoosterForceMultiplier < MinBoosterLimit || BoosterForceMultiplier > MaxBoosterLimit;

                // Прозрачность ~60% (Alpha = 0.6f)
                Color color = isAbused ? Color.red : Color.green;
                color.a = 0.6f;
                watermarkStyle.normal.textColor = color;

                string text = isAbused
                    ? $"[BOOSTER FIX: ABUSE ({BoosterForceMultiplier:F4}x)]"
                    : $"[BOOSTER FIX: {BoosterForceMultiplier:F4}x]";

                GUI.Label(new Rect(Screen.width - 270f, 20f, 250f, 30f), text, watermarkStyle);
            }
        }

        public void DrawUI(float x, float y, float contentWidth)
        {
            GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Physics Fixes & QoL</b>");
            y += 24f;

            // --- JUMPERS ---
            EnableJumperFix = GUI.Toggle(new Rect(x, y, contentWidth, 20), EnableJumperFix, " Fix Jumper Height (Deterministic Jumpbox)");
            y += 22f;

            if (EnableJumperFix)
            {
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Jump Height Multiplier: <b>{JumperForceMultiplier:F3}x</b>");
                y += 18f;

                float prevJumper = JumperForceMultiplier;
                JumperForceMultiplier = GUI.HorizontalSlider(new Rect(x + 15f, y, contentWidth - 125f, 15), JumperForceMultiplier, 0.5f, 2.0f);

                if (GUI.Button(new Rect(x + contentWidth - 105f, y - 2f, 105f, 20f), "Reset (1.025x)"))
                {
                    JumperForceMultiplier = 1.025f;
                }

                if (Mathf.Abs(prevJumper - JumperForceMultiplier) > 0.0001f)
                {
                    CheckFinishAbuse();
                }

                y += 25f;
            }

            // --- BOOSTERS ---
            EnableBoosterFix = GUI.Toggle(new Rect(x, y, contentWidth, 20), EnableBoosterFix, " Fix Booster Momentum (Deterministic Booster)");
            y += 22f;

            if (EnableBoosterFix)
            {
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Booster Force Multiplier: <b>{BoosterForceMultiplier:F4}x</b>");
                y += 18f;

                float prevBooster = BoosterForceMultiplier;

                // Слайдер строго до 1.0500f
                BoosterForceMultiplier = GUI.HorizontalSlider(new Rect(x + 15f, y, contentWidth - 125f, 15), BoosterForceMultiplier, 0.9500f, 1.0500f);

                if (GUI.Button(new Rect(x + contentWidth - 115f, y - 2f, 115f, 20f), "Reset (1.0175x)"))
                {
                    BoosterForceMultiplier = 1.0175f;
                }

                if (Mathf.Abs(prevBooster - BoosterForceMultiplier) > 0.0001f)
                {
                    CheckFinishAbuse();
                }

                y += 25f;
            }

            // --- VISUAL HELPERS ---
            y += 10f;
            GUI.Label(new Rect(x, y, contentWidth, 20f), "<b>Visual Helpers</b>");
            y += 22f;

            TrajectoryModule.EnableTrajectory = GUI.Toggle(
                new Rect(x, y, contentWidth, 20f),
                TrajectoryModule.EnableTrajectory,
                " Show Jump Trajectory Line"
            );
            y += 22f;

            bool newTriggerState = GUI.Toggle(
                new Rect(x, y, contentWidth, 20f),
                TriggerVisualizer.EnableTriggers,
                " Show Interactive Triggers (Jumper / Booster / Finish)"
            );

            if (newTriggerState != TriggerVisualizer.EnableTriggers)
            {
                TriggerVisualizer.EnableTriggers = newTriggerState;
            }
            y += 25f;

            // --- GENERAL QOL ---
            y += 10f;
            GUI.Label(new Rect(x, y, contentWidth, 20f), "<b>General QoL</b>");
            y += 22f;

            Main.instantRespawn = GUI.Toggle(
                new Rect(x, y, contentWidth, 20f),
                Main.instantRespawn,
                " Instant Respawn on Death (EXPERIMENTAL)"
            );
            y += 25f;
        }

        public static void CheckFinishAbuse()
        {
            bool isJumperAbused = EnableJumperFix && (JumperForceMultiplier < MinJumperLimit || JumperForceMultiplier > MaxJumperLimit);
            bool isBoosterAbused = EnableBoosterFix && (BoosterForceMultiplier < MinBoosterLimit || BoosterForceMultiplier > MaxBoosterLimit);
            bool isAbused = isJumperAbused || isBoosterAbused;

            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in allObjects)
            {
                string nameLower = go.name.ToLower();
                if (nameLower.Contains("finish") || nameLower.Contains("endlevel") || go.CompareTag("Finish"))
                {
                    go.SetActive(!isAbused);
                }
            }
        }
    }

    // --- ФИКС ТОНКИХ БЛОКОВ И КРАЕВ ---
    [HarmonyPatch(typeof(FirstPersonController), "Start")]
    public static class ControllerSetup_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(FirstPersonController __instance)
        {
            if (__instance.m_CharacterController != null)
            {
                __instance.m_CharacterController.skinWidth = 0.005f;
                __instance.m_CharacterController.minMoveDistance = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(FirstPersonController), "FixedUpdate")]
    public static class ThinEdgeFix_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(FirstPersonController __instance)
        {
            if (!FixesModule.EnableLedgeFix) return;

            var controller = __instance.m_CharacterController;
            if (controller == null) return;

            Vector3 origin = __instance.transform.position;
            float checkDistance = (controller.height / 2f) + 0.1f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance))
            {
                if (hit.normal.y > 0.5f)
                {
                    controller.stepOffset = 0.3f;
                }
            }
        }
    }

    // --- ПАТЧ ДЖАМПЕРОВ ---
    [HarmonyPatch(typeof(Jumpbox), "OnTriggerEnter")]
    public static class Jumpbox_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Jumpbox __instance, Collider other)
        {
            if (!FixesModule.EnableJumperFix) return true;
            FixesModule.CheckFinishAbuse();

            var fps = other.GetComponent<FirstPersonController>();
            var controller = other.GetComponent<CharacterController>();

            if (fps != null && controller != null)
            {
                AudioSystem audio = Object.FindObjectOfType<AudioSystem>();
                if (audio != null) audio.Play("jumper");

                float topY = __instance.transform.position.y + (__instance.transform.localScale.y / 2f);
                Vector3 currentPos = other.transform.position;
                other.transform.position = new Vector3(currentPos.x, topY + __instance.jumpOffset, currentPos.z);

                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

                fps.JumpboxJump(__instance.jumpForce * FixesModule.JumperForceMultiplier);
            }
            return false;
        }
    }

    // --- ПАТЧ БУСТЕРОВ ---
    [HarmonyPatch(typeof(Booster), "OnTriggerEnter")]
    public static class Booster_Patch
    {
        private static float lastBoostTime = 0f;

        [HarmonyPrefix]
        public static bool Prefix(Booster __instance, Collider other)
        {
            if (!FixesModule.EnableBoosterFix) return true;
            FixesModule.CheckFinishAbuse();

            if (Time.time - lastBoostTime < 0.15f) return false;

            var fps = other.GetComponent<FirstPersonController>();
            var controller = other.GetComponent<CharacterController>();
            var fakeForce = other.GetComponent<PlayerFakeForce>();

            if (controller != null && fakeForce != null)
            {
                lastBoostTime = Time.time;

                AudioSystem audio = Object.FindObjectOfType<AudioSystem>();
                if (audio != null) audio.Play("booster");

                if (fps != null)
                {
                    fps.cancelGroundForce = true;
                    fps.m_Jumping = true;
                    fps.m_PreviouslyGrounded = false;
                }

                controller.Move(Vector3.up * __instance.jumpOffset);

                fakeForce.SetFakeForce(
                    __instance.forwardForce * FixesModule.BoosterForceMultiplier,
                    __instance.jumpForce * FixesModule.BoosterForceMultiplier,
                    __instance.airControl
                );
                fakeForce.ApplyFakeForce();
            }
            return false;
        }
    }
}