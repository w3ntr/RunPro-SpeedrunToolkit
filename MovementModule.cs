using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class MovementModule
    {
        public bool IsEnabled = true;

        // По умолчанию выключены, чтобы не блокировать триггеры при старте
        public bool EnableAirDash = false;
        public float DashForce = 30f;

        public bool EnableInfiniteJump = false;
        public float JumpForce = 12f;

        private GameObject playerObj;
        private Rigidbody playerRb;
        private CharacterController playerCc;

        public void Init() { }

        /// <summary>
        /// Полный сброс модификаторов движения к чистому состоянию
        /// </summary>
        public void Reset()
        {
            EnableAirDash = false;
            EnableInfiniteJump = false;

            playerObj = null;
            playerRb = null;
            playerCc = null;
        }

        public void OnSceneWasLoaded(string sceneName)
        {
            Reset();
        }

        public void Update()
        {
            if (playerObj == null) FindPlayer();
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (go != null && (go.name.ToLower().Contains("player") || go.name.ToLower().Contains("character")))
                    {
                        player = go;
                        break;
                    }
                }
            }

            if (player != null)
            {
                playerObj = player;
                playerRb = player.GetComponent<Rigidbody>() ?? player.GetComponentInChildren<Rigidbody>() ?? player.GetComponentInParent<Rigidbody>();
                playerCc = player.GetComponent<CharacterController>() ?? player.GetComponentInChildren<CharacterController>() ?? player.GetComponentInParent<CharacterController>();
            }
        }

        public void PerformAirDash(PracticeModule practice)
        {
            if (!EnableAirDash) return;
            if (playerObj == null) FindPlayer();
            if (playerObj == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 dashVector = cam.transform.forward * DashForce;

            if (playerRb != null)
            {
                playerRb.AddForce(dashVector, ForceMode.VelocityChange);
            }

            playerObj.transform.position += cam.transform.forward * (DashForce * 0.15f);

            if (playerCc != null)
            {
                playerCc.Move(dashVector * Time.deltaTime * 10f);
            }

            if (practice != null) practice.BlockFinishAndTimer();
        }

        public void PerformAirJump(PracticeModule practice)
        {
            if (!EnableInfiniteJump) return;
            if (playerObj == null) FindPlayer();
            if (playerObj == null) return;

            if (playerRb != null)
            {
                Vector3 currentVel = playerRb.velocity;
                playerRb.velocity = new Vector3(currentVel.x, 0f, currentVel.z);
                playerRb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            }

            playerObj.transform.position += Vector3.up * 0.5f;

            if (practice != null) practice.BlockFinishAndTimer();
        }

        public float DrawUI(float x, float y, float contentWidth)
        {
            GUI.Label(new Rect(x, y, contentWidth, 20), "<b>Movement Tweaks & Cheats:</b>");
            y += 24f;

            EnableAirDash = GUI.Toggle(new Rect(x, y, contentWidth, 20), EnableAirDash, " Enable Air Dash (Hotkey: E)");
            y += 22f;

            if (EnableAirDash)
            {
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Dash Force: {DashForce:F0}");
                y += 20f;
                DashForce = GUI.HorizontalSlider(new Rect(x + 15f, y + 4, contentWidth - 15f, 20), DashForce, 10f, 100f);
                y += 26f;
            }

            EnableInfiniteJump = GUI.Toggle(new Rect(x, y, contentWidth, 20), EnableInfiniteJump, " Enable Air / Infinite Jump (Hotkey: Space)");
            y += 22f;

            if (EnableInfiniteJump)
            {
                GUI.Label(new Rect(x + 15f, y, contentWidth - 15f, 20), $"Jump Force: {JumpForce:F1}");
                y += 20f;
                JumpForce = GUI.HorizontalSlider(new Rect(x + 15f, y + 4, contentWidth - 15f, 20), JumpForce, 5f, 25f);
                y += 26f;
            }

            GUI.Label(new Rect(x, y, contentWidth, 18), "<i>Note: Using movement cheats automatically disables finish triggers.</i>");
            y += 24f;

            return y;
        }
    }
}
