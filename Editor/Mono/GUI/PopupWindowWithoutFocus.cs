// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor
{
    class PopupWindowWithoutFocus : PopupWindow
    {
        static PopupWindowWithoutFocus s_PopupWindowWithoutFocus;

        public new static void Show(Rect activatorRect, PopupWindowContent windowContent)
        {
            Show(activatorRect, windowContent, null);
        }

        internal new static void Show(Rect activatorRect, PopupWindowContent windowContent, PopupLocation[] locationPriorityOrder)
        {
            if (windowContent == null)
                throw new System.ArgumentNullException(nameof(windowContent));

            if (s_PopupWindowWithoutFocus != null)
            {
                s_PopupWindowWithoutFocus.CloseContent();
            }

            if (ShouldShowWindow(activatorRect))
            {
                if (s_PopupWindowWithoutFocus == null)
                    s_PopupWindowWithoutFocus = CreateInstance<PopupWindowWithoutFocus>();

                s_PopupWindowWithoutFocus.Init(activatorRect, windowContent, locationPriorityOrder, ShowMode.PopupMenu, false);
            }
            else
            {
                windowContent.OnClose();
            }
        }

        public static bool IsVisible()
        {
            return s_PopupWindowWithoutFocus != null;
        }

        public static void Hide()
        {
            if (s_PopupWindowWithoutFocus != null)
                s_PopupWindowWithoutFocus.Close();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            hideFlags = HideFlags.DontSave;
            s_PopupWindowWithoutFocus = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_PopupWindowWithoutFocus = null;
        }

        // Invoked from C++
        static bool OnGlobalMouseOrKeyEvent(EventType type, KeyCode keyCode, Vector2 mousePosition)
        {
            if (s_PopupWindowWithoutFocus == null)
                return false;

            if (type == EventType.MouseDown && !s_PopupWindowWithoutFocus.position.Contains(mousePosition))
            {
                s_PopupWindowWithoutFocus.Close();
                return false;
            }

            if (type == EventType.KeyDown && keyCode == KeyCode.Escape)
            {
                s_PopupWindowWithoutFocus.Close();
                return true;
            }

            return false;
        }
    }
}
