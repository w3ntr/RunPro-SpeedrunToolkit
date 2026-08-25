using HarmonyLib;
using Il2Cpp;
using System.Reflection;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class FixesModule
    {
        public static bool EnableJumperFix = true;
        public static bool EnableBoosterFix = true;

        public static float JumperForceMultiplier = 1.01f;
        public static float BoosterForceMultiplier = 1.00f;

        // Разрешенный диапазон честной игры (от 1.00x до 1.02x включительно)
        public const float MinSafeLimit = 1.00f;
        public const float MaxSafeLimit = 1.01f;

        public void DrawUI(float x, float y, float contentWidth)
        {
            GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Physics Fixes & QoL</b>");
            y += 24f;

            // --- ФИКС ДЖАМПЕРОВ ---
            EnableJumperFix = GUI.Toggle(new Rect(x, y, contentWidth, 20), EnableJumperFix, " Fix Jumper Height (Deterministic Jumpbox)");
            y += 22f;

            if (EnableJumperFix)
            {
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Jump Height Multiplier: <b>{JumperForceMultiplier:F2}x</b>");
                y += 18f;

                float prevJumper = JumperForceMultiplier;
                JumperForceMultiplier = GUI.HorizontalSlider(new Rect(x + 15f, y, contentWidth - 115f, 15), JumperForceMultiplier, 0.5f, 2.0f);

                if (GUI.Button(new Rect(x + contentWidth - 95f, y - 2f, 95f, 20f), "Reset (1.01x)"))
                {
                    JumperForceMultiplier = 1.01f;
                }

                if (Mathf.Abs(prevJumper - JumperForceMultiplier) > 0.001f)
                {
                    CheckFinishAbuse();
                }

                y += 25f;
            }

            // --- ФИКС БУСТЕРОВ ---
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
        }

        // Выход за пределы 1.00x - 1.02x скрывает финиш
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

        // Универсальное получение силы бустера через рефлексию
        public static float GetBoosterForce(Booster booster)
        {
            if (booster == null) return 0f;
            var type = booster.GetType();

            string[] possibleNames = new string[] { "boostForce", "force", "speed", "boostSpeed", "boost", "power" };
            foreach (var name in possibleNames)
            {
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(float))
                {
                    return (float)field.GetValue(booster);
                }
            }

            // Фоллбэк: берем первое числовое float-поле класса Booster
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(float))
                {
                    return (float)f.GetValue(booster);
                }
            }

            return 10f;
        }
    }

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
                if (rb != null)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                }

                fps.JumpboxJump(__instance.jumpForce * FixesModule.JumperForceMultiplier);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Booster), "OnTriggerEnter")]
    public static class Booster_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Booster __instance, Collider other)
        {
            if (!FixesModule.EnableBoosterFix) return true;

            FixesModule.CheckFinishAbuse();

            var controller = other.GetComponent<CharacterController>();
            var fakeForce = other.GetComponent<PlayerFakeForce>();

            if (controller != null && fakeForce != null)
            {
                AudioSystem audio = Object.FindObjectOfType<AudioSystem>();
                if (audio != null) audio.Play("booster");

                // Сбрасываем старую инерцию
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                }

                // Передаем параметры ускорения с учетом множителя и затем вызываем ApplyFakeForce()
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