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
        public static bool AutoRespawnOnDeath = false;
        public static bool hasAbusedThisSession = false;

        public static float JumperForceMultiplier = 1.0190f;
        public static float BoosterForceMultiplier = 1.0180f;

        public const float MinJumperLimit = 1.0000f;
        public const float MaxJumperLimit = 1.0190f;
        public const float MinBoosterLimit = 1.0000f;
        public const float MaxBoosterLimit = 1.0180f;

        private TimerModule timerModule = new TimerModule();

        public void OnUpdate()
        {
            timerModule.OnUpdate();
        }

        public void OnGUI()
        {
            if (EnableBoosterFix || EnableJumperFix)
            {
                GUIStyle watermarkStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight
                };

                float yOffset = 20f;

                if (EnableBoosterFix)
                {
                    bool isBoosterAbused = BoosterForceMultiplier < MinBoosterLimit || BoosterForceMultiplier > MaxBoosterLimit;
                    Color color = isBoosterAbused ? Color.red : Color.green;
                    color.a = 0.6f;
                    watermarkStyle.normal.textColor = color;

                    string text = isBoosterAbused
                        ? $"[BOOSTER FIX: ABUSE ({BoosterForceMultiplier:F4}x)]"
                        : $"[BOOSTER FIX: {BoosterForceMultiplier:F4}x]";

                    GUI.Label(new Rect(Screen.width - 320f, yOffset, 300f, 30f), text, watermarkStyle);
                    yOffset += 25f;
                }

                if (EnableJumperFix)
                {
                    bool isJumperAbused = JumperForceMultiplier < MinJumperLimit || JumperForceMultiplier > MaxJumperLimit;
                    Color color = isJumperAbused ? Color.red : Color.green;
                    color.a = 0.6f;
                    watermarkStyle.normal.textColor = color;

                    string text = isJumperAbused
                        ? $"[JUMPER FIX: ABUSE ({JumperForceMultiplier:F4}x)]"
                        : $"[JUMPER FIX: {JumperForceMultiplier:F4}x]";

                    GUI.Label(new Rect(Screen.width - 320f, yOffset, 300f, 30f), text, watermarkStyle);
                }
            }
        }

        public void DrawUI(float x, float y, float contentWidth)
        {
            GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Physics Fixes & QoL</b>", UITheme.LabelStyle);
            y += 24f;

            // --- JUMPERS ---
            EnableJumperFix = GUI.Toggle(new Rect(x, y, contentWidth, 20), EnableJumperFix, " Fix Jumper Height (Deterministic Jumpbox)");
            y += 22f;

            if (EnableJumperFix)
            {
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Jump Height Multiplier: <b>{JumperForceMultiplier:F4}x</b>", UITheme.LabelStyle);
                y += 18f;

                float prevJumper = JumperForceMultiplier;
                JumperForceMultiplier = GUI.HorizontalSlider(new Rect(x + 15f, y, contentWidth - 125f, 15), JumperForceMultiplier, 0.9500f, 1.0500f);

                if (GUI.Button(new Rect(x + contentWidth - 115f, y - 2f, 115f, 20f), "Reset (1.0190x)", UITheme.ButtonStyle))
                {
                    JumperForceMultiplier = 1.0190f;
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
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Booster Force Multiplier: <b>{BoosterForceMultiplier:F4}x</b>", UITheme.LabelStyle);
                y += 18f;

                float prevBooster = BoosterForceMultiplier;

                BoosterForceMultiplier = GUI.HorizontalSlider(new Rect(x + 15f, y, contentWidth - 125f, 15), BoosterForceMultiplier, 0.9500f, 1.0500f);

                if (GUI.Button(new Rect(x + contentWidth - 115f, y - 2f, 115f, 20f), "Reset (1.0175x)", UITheme.ButtonStyle))
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
            GUI.Label(new Rect(x, y, contentWidth, 20f), "<b>Visual Helpers</b>", UITheme.LabelStyle);
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
            GUI.Label(new Rect(x, y, contentWidth, 20f), "<b>General QoL</b>", UITheme.LabelStyle);
            y += 22f;

            // 1. Тумблер Anti-AFK / Anti-Kick
            bool newAntiAfk = GUI.Toggle(new Rect(x, y, contentWidth, 20f), AntiAfkModule.EnableAntiAfk, " Enable Anti-AFK / Anti-Kick");
            if (newAntiAfk != AntiAfkModule.EnableAntiAfk)
            {
                AntiAfkModule.EnableAntiAfk = newAntiAfk;
                if (!newAntiAfk)
                {
                    Application.runInBackground = false;
                }
            }
            y += 25f;

            // 2. Мгновенный респаун
            string statusText = AutoRespawnOnDeath ? "<color=#2ED573>[ON]</color>" : "<color=#FF4757>[OFF]</color>";
            if (GUI.Button(new Rect(x, y, contentWidth, 24f), $"<b>⚡ Instant Auto-Respawn:</b> {statusText}", UITheme.ButtonStyle))
            {
                AutoRespawnOnDeath = !AutoRespawnOnDeath;
            }
            y += 28f;
        }

        public static void CheckFinishAbuse()
        {
            bool isJumperAbused = EnableJumperFix && (JumperForceMultiplier < MinJumperLimit || JumperForceMultiplier > MaxJumperLimit);
            bool isBoosterAbused = EnableBoosterFix && (BoosterForceMultiplier < MinBoosterLimit || BoosterForceMultiplier > MaxBoosterLimit);

            if (isJumperAbused || isBoosterAbused)
            {
                hasAbusedThisSession = true;
            }

            // Финиш выключается, если сейчас абуз ИЛИ если абуз был совершен в течение этой попытки
            bool shouldDisableFinish = isJumperAbused || isBoosterAbused || hasAbusedThisSession;

            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
            foreach (var go in allObjects)
            {
                string nameLower = go.name.ToLower();
                if (nameLower.Contains("finish") || nameLower.Contains("endlevel") || go.CompareTag("Finish"))
                {
                    go.SetActive(!shouldDisableFinish);
                }
            }
        }

        public void OnSceneWasLoaded(string sceneName)
        {
            hasAbusedThisSession = false; // При смене уровня сбрасываем флаг
        }

        // --- ПАТЧ МГНОВЕННОГО РЕСПАВНА ПРИ ПАДЕНИИ ---
        [HarmonyPatch(typeof(GameScene), nameof(GameScene.Olay_OyuncuDustu))]
        public static class InstantRespawn_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(GameScene __instance)
            {
                if (FixesModule.AutoRespawnOnDeath)
                {
                    if (__instance.checkpointActive)
                    {
                        __instance.SpawnCheckpoint();
                    }
                    else
                    {
                        __instance.SpawnChar();
                    }

                    __instance.LockCursor();

                    FirstPersonController fps = Object.FindObjectOfType<FirstPersonController>();
                    if (fps != null)
                    {
                        fps.enabled = true;
                    }

                    return false;
                }
                return true;
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
}