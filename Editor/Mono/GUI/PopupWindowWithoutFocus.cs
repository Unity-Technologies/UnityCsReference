// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor
{
    class PopupWindowWithoutFocus : PopupWindow
    {
        static PopupWindowWithoutFocus s_PopupWindowWithoutFocus;
        bool hasBeenFocused = false;

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

        [RequiredByNativeCode]
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
            s_PopupWindowWithoutFocus = null;
            base.OnDisable();
        }

        // Invoked from C++
        // If we want to use this paradigm long term, we should consider reducing the overhead of a managed call.
        // Returns true if we suppress event propagation.
        [RequiredByNativeCode]
        static bool OnGlobalMouseOrKeyEvent(EventType type, KeyCode keyCode, Vector2 mousePosition)
        {
            bool suppress = false;

            if (s_PopupWindowWithoutFocus == null)
                return suppress;
            else if (type == EventType.KeyDown)
            {
                if (keyCode == KeyCode.Escape)
                {
                    // Always close this window type when escape is pressed, even if never got focus.
                    // We don't want the esc key propogated to the window with keyboard focus.
                    s_PopupWindowWithoutFocus.Close();
                    suppress = true;
                }
            }
            else if (type == EventType.MouseDown && !s_PopupWindowWithoutFocus.hasBeenFocused)
            {
                // If the window has been clicked, it becomes a normal popup window that can spawn other windows and be treated as another AuxWindow.
                // If the click was somewhere else, we assume that the user did not want to see this window anymore, so it should be closed.
                if (s_PopupWindowWithoutFocus.position.Contains(mousePosition))
                    s_PopupWindowWithoutFocus.hasBeenFocused = true;
                else
                    s_PopupWindowWithoutFocus.Close();
            }

            return suppress;
        }
    }
}
