using System;
using UnityEngine;
using MelonLoader;

namespace SpeedrunToolkitMod
{
    public class WindowManager
    {
        public Rect WindowRect = new Rect(200f, 150f, 620f, 560f);
        public float MinWidth = 520f;
        public float MinHeight = 400f;
        public float MenuOpacity = 0.92f;

        private bool isResizing = false;
        private Vector2 resizeStartMouse;
        private Vector2 resizeStartSize;
        private const int WindowID = 99123;

        private Action<int> currentContentAction;

        // --- Сохранение настроек окна через MelonPreferences ---
        private MelonPreferences_Entry<float> prefWindowX;
        private MelonPreferences_Entry<float> prefWindowY;
        private MelonPreferences_Entry<float> prefWindowW;
        private MelonPreferences_Entry<float> prefWindowH;

        public void Init(MelonPreferences_Category category)
        {
            prefWindowX = category.CreateEntry("WindowX", 200f);
            prefWindowY = category.CreateEntry("WindowY", 150f);
            prefWindowW = category.CreateEntry("WindowWidth", 620f);
            prefWindowH = category.CreateEntry("WindowHeight", 560f);

            WindowRect = new Rect(prefWindowX.Value, prefWindowY.Value, prefWindowW.Value, prefWindowH.Value);
        }

        public void SaveConfig()
        {
            if (prefWindowX == null) return;
            prefWindowX.Value = WindowRect.x;
            prefWindowY.Value = WindowRect.y;
            prefWindowW.Value = WindowRect.width;
            prefWindowH.Value = WindowRect.height;
        }

        public void ResetWindow()
        {
            float w = 620f;
            float h = 560f;
            WindowRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            SaveConfig();
        }

        public void Draw(string title, Action<int> windowContent)
        {
            UITheme.InitStyles(MenuOpacity);

            WindowRect.width = Mathf.Max(MinWidth, WindowRect.width);
            WindowRect.height = Mathf.Max(MinHeight, WindowRect.height);

            currentContentAction = windowContent;

            Rect oldRect = WindowRect;
            WindowRect = GUI.Window(WindowID, WindowRect, (Action<int>)WindowCallback, title, UITheme.WindowStyle);

            // Сохраняем позицию или размер, если окно переместили/изменили и отпустили кнопку мыши
            if (Event.current.type == EventType.MouseUp && (oldRect.position != WindowRect.position || oldRect.size != WindowRect.size))
            {
                SaveConfig();
            }
        }

        private void WindowCallback(int id)
        {
            if (currentContentAction != null)
            {
                currentContentAction.Invoke(id);
            }
            HandleResize();
            GUI.DragWindow(new Rect(0, 0, WindowRect.width - 25f, 25f));
        }

        private void HandleResize()
        {
            float handleSize = 20f;
            Rect resizeHandleRect = new Rect(WindowRect.width - handleSize, WindowRect.height - handleSize, handleSize, handleSize);
            GUI.Label(resizeHandleRect, "◢", UITheme.ResizeHandleStyle);

            Event currentEvent = Event.current;
            Vector2 mouseScreenPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (currentEvent.type == EventType.MouseDown && resizeHandleRect.Contains(currentEvent.mousePosition))
            {
                isResizing = true;
                resizeStartMouse = mouseScreenPosition;
                resizeStartSize = new Vector2(WindowRect.width, WindowRect.height);
                currentEvent.Use();
            }

            if (isResizing)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    isResizing = false;
                    SaveConfig();
                }
                else
                {
                    Vector2 delta = mouseScreenPosition - resizeStartMouse;
                    WindowRect.width = Mathf.Max(MinWidth, resizeStartSize.x + delta.x);
                    WindowRect.height = Mathf.Max(MinHeight, resizeStartSize.y + delta.y);
                }
            }
        }
    }
}