using System;
using UnityEngine;

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

        public void ResetWindow()
        {
            float w = 620f;
            float h = 560f;
            WindowRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        }

        public void Draw(string title, Action<int> windowContent)
        {
            UITheme.InitStyles(MenuOpacity);

            WindowRect.width = Mathf.Max(MinWidth, WindowRect.width);
            WindowRect.height = Mathf.Max(MinHeight, WindowRect.height);

            currentContentAction = windowContent;

            WindowRect = GUI.Window(WindowID, WindowRect, (Action<int>)WindowCallback, title, UITheme.WindowStyle);
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
            // Экранные координаты мыши (Y инвертирован в Unity IMGUI)
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