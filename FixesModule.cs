using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class FixesModule
    {
        public static bool EnableJumperFix = true;
        public static bool EnableBoosterFix = true;
        public static bool EnableLedgeFix = true;

        public static float JumperForceMultiplier = 1.025f;
        public static float BoosterForceMultiplier = 1.00f;

        public const float MinSafeLimit = 1.00f;
        public const float MaxSafeLimit = 1.025f;

        private TimerModule timerModule = new TimerModule();

        public void OnUpdate()
        {
            timerModule.OnUpdate();
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

                if (Mathf.Abs(prevJumper - JumperForceMultiplier) > 0.001f)
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
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Booster Force Multiplier: <b>{BoosterForceMultiplier:F2}x</b>");
                y += 18f;

                float prevBooster = BoosterForceMultiplier;
                BoosterForceMultiplier = GUI.HorizontalSlider(new Rect(x + 15f, y, contentWidth - 115f, 15), BoosterForceMultiplier, 0.5f, 2.0f);

                if (GUI.Button(new Rect(x + contentWidth - 95f, y - 2f, 95f, 20f), "Reset (1.00x)"))
                {
                    BoosterForceMultiplier = 1.00f;
                }

                if (Mathf.Abs(prevBooster - BoosterForceMultiplier) > 0.001f)
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
            bool isJumperAbused = EnableJumperFix && (JumperForceMultiplier < MinSafeLimit || JumperForceMultiplier > MaxSafeLimit);
            bool isBoosterAbused = EnableBoosterFix && (BoosterForceMultiplier < MinSafeLimit || BoosterForceMultiplier > MaxSafeLimit);
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

    // --- ИСПРАВЛЕННЫЙ ПАТЧ БУСТЕРОВ ---
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

                // 1. Принудительно отключаем прижатие к земле в FPS-контроллере
                if (fps != null)
                {
                    fps.cancelGroundForce = true;
                    fps.m_Jumping = true;
                    fps.m_PreviouslyGrounded = false;
                }

                // 2. Сдвигаем контроллер штатным методом CharacterController
                controller.Move(Vector3.up * __instance.jumpOffset);

                // 3. Применяем силы с учетом ползунка BoosterForceMultiplier
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