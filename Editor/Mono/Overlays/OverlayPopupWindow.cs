// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    // This is an ugly hack to get popup windows to close when clicking on a button while the popup is already open.
    // The problem is that popup windows close when losing focus, which clicking the trigger button does. So the button
    // is clicked, the window loses focus and closes, then the mouse up on button requests a new window.
    [EditorBrowsable(EditorBrowsableState.Never)]
    abstract class PopupWindowBase : EditorWindow
    {
        static double s_LastClosedTime;
        static Rect s_LastActivatorRect;

        static bool ShouldShowWindow(Rect activatorRect)
        {
            const double kJustClickedTime = 0.2;
            bool justClosed = (EditorApplication.timeSinceStartup - s_LastClosedTime) < kJustClickedTime;
            if (!justClosed || activatorRect != s_LastActivatorRect)
            {
                s_LastActivatorRect = activatorRect;
                return true;
            }
            return false;
        }

        public static T Show<T>(VisualElement trigger, Vector2 size) where T : EditorWindow
        {
            return Show<T>(GUIUtility.GUIToScreenRect(trigger.worldBound), size);
        }

        public static T Show<T>(Rect activatorRect, Vector2 size) where T : EditorWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();

            if (windows.Any())
            {
                foreach (var window in windows)
                    window.Close();
                return default;
            }

            if (ShouldShowWindow(activatorRect))
            {
                var popup = CreateInstance<T>();

                popup.hideFlags = HideFlags.DontSave;
                popup.ShowAsDropDown(activatorRect, size);
                return popup;
            }

            return default;
        }

        void OnEnableINTERNAL()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        void OnDisableINTERNAL()
        {
            s_LastClosedTime = EditorApplication.timeSinceStartup;
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
        }
    }

    abstract class OverlayPopupWindow : PopupWindowBase
    {
        const float k_BorderWidth = 1;

        protected virtual void OnEnable()
        {
            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.44f, 0.44f, 0.44f, 1f) : new Color(0.51f, 0.51f, 0.51f);

            rootVisualElement.style.borderLeftWidth = k_BorderWidth;
            rootVisualElement.style.borderTopWidth = k_BorderWidth;
            rootVisualElement.style.borderRightWidth = k_BorderWidth;
            rootVisualElement.style.borderBottomWidth = k_BorderWidth;
            rootVisualElement.style.borderLeftColor = borderColor;
            rootVisualElement.style.borderTopColor = borderColor;
            rootVisualElement.style.borderRightColor = borderColor;
            rootVisualElement.style.borderBottomColor = borderColor;
        }
    }
}
