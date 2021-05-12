// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    abstract class OverlayPopupWindow : EditorWindow
    {
        protected const float borderWidth = 1;

        public static T ShowOverlayPopup<T>(VisualElement trigger, Vector2 size) where T : OverlayPopupWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();
            foreach (var window in windows)
                window.Close();
            var popup = CreateInstance<T>();
            popup.Init(trigger, size);
            return popup;
        }

        void OnEnableINTERNAL()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
        }

        void OnDisableINTERNAL()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
        }

        void Init(VisualElement trigger, Vector2 size)
        {
            var buttonRect = GUIUtility.GUIToScreenRect(trigger.worldBound);

            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.44f, 0.44f, 0.44f, 1f) : new Color(0.51f, 0.51f, 0.51f);

            rootVisualElement.style.borderLeftWidth = borderWidth;
            rootVisualElement.style.borderTopWidth = borderWidth;
            rootVisualElement.style.borderRightWidth = borderWidth;
            rootVisualElement.style.borderBottomWidth = borderWidth;
            rootVisualElement.style.borderLeftColor = borderColor;
            rootVisualElement.style.borderTopColor = borderColor;
            rootVisualElement.style.borderRightColor = borderColor;
            rootVisualElement.style.borderBottomColor = borderColor;

            ShowAsDropDown(buttonRect, size);
        }
    }
}
