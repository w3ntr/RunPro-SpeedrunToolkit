using MelonLoader;
using UnityEngine;

namespace SpeedrunToolkitMod
{
    public class FreecamModule
    {
        public bool IsEnabled = false;
        public float FlySpeed = 12f;
        public float FastFlyMultiplier = 3f;
        public float MouseSensitivity = 2f;

        private KeyCode toggleKey = KeyCode.F3;
        private GameObject freecamObj;
        private Camera freecamComp;
        private Camera originalMainCam;

        private float savedTimeScale = 1f;
        private float rotX = 0f;
        private float rotY = 0f;

        public void Init()
        {
            MelonLogger.Msg("[Freecam] Module initialized.");
        }

        public void OnUpdate()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleFreecam();
            }

            if (!IsEnabled) return;

            // Авто-отключение, если игрок умер, перезапустил карту или пропала камера
            if (freecamObj == null || originalMainCam == null)
            {
                DisableFreecam();
                return;
            }

            // 1. Поворот камеры мышью
            rotX += Input.GetAxis("Mouse X") * MouseSensitivity * 2f;
            rotY -= Input.GetAxis("Mouse Y") * MouseSensitivity * 2f;
            rotY = Mathf.Clamp(rotY, -89f, 89f);

            freecamObj.transform.rotation = Quaternion.Euler(rotY, rotX, 0f);

            // 2. Полет (unscaledDeltaTime позволяет летать при timeScale = 0)
            float speed = FlySpeed * (Input.GetKey(KeyCode.LeftShift) ? FastFlyMultiplier : 1f) * Time.unscaledDeltaTime;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += freecamObj.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= freecamObj.transform.forward;
            if (Input.GetKey(KeyCode.D)) move += freecamObj.transform.right;
            if (Input.GetKey(KeyCode.A)) move -= freecamObj.transform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

            freecamObj.transform.position += move * speed;
        }

        public void ToggleFreecam()
        {
            if (!IsEnabled)
            {
                originalMainCam = Camera.main;
                if (originalMainCam == null) return;

                IsEnabled = true;

                // Замораживаем время в игре (персонаж и физика замирают)
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;

                // Создаем виртуальную камеру
                freecamObj = new GameObject("ST_FreecamCamera");
                freecamObj.tag = "MainCamera";

                freecamComp = freecamObj.AddComponent<Camera>();
                freecamComp.CopyFrom(originalMainCam);

                if (originalMainCam.clearFlags == CameraClearFlags.Depth || originalMainCam.clearFlags == CameraClearFlags.Nothing)
                {
                    freecamComp.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    freecamComp.clearFlags = originalMainCam.clearFlags;
                }

                freecamComp.backgroundColor = originalMainCam.backgroundColor;
                freecamComp.cullingMask = originalMainCam.cullingMask;

                var listener = freecamObj.GetComponent<AudioListener>();
                if (listener != null) Object.Destroy(listener);

                freecamObj.transform.position = originalMainCam.transform.position;
                freecamObj.transform.rotation = originalMainCam.transform.rotation;

                rotX = freecamObj.transform.eulerAngles.y;
                rotY = freecamObj.transform.eulerAngles.x;

                originalMainCam.enabled = false;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                MelonLogger.Msg("[Freecam] Enabled.");
            }
            else
            {
                DisableFreecam();
            }
        }

        public void DisableFreecam()
        {
            if (!IsEnabled) return;

            IsEnabled = false;

            // Возвращаем игровое время
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;

            if (originalMainCam != null)
            {
                originalMainCam.enabled = true;
            }

            if (freecamObj != null)
            {
                Object.Destroy(freecamObj);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            MelonLogger.Msg("[Freecam] Disabled.");
        }
    }
}