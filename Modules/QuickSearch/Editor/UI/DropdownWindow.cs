// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search
{
    abstract class DropdownWindow<T> : EditorWindow
    {
        internal static bool requestShowWindow;
        internal static double s_CloseTime;
        private static DropdownWindow<T> s_ActiveWindow = null;

        protected virtual void OnEnable()
        {
            s_ActiveWindow = this;
        }

        protected virtual void OnDisable()
        {
            s_ActiveWindow = null;
        }

        internal static bool canShow
        {
            get
            {
                if (EditorApplication.timeSinceStartup - s_CloseTime < 0.250)
                    return false;
                return true;
            }
        }

        public static void RequestShowWindow(bool delayed = false)
        {
            if (delayed)
            {
                EditorApplication.delayCall += () => { requestShowWindow = true; };
            }
            else
            {
                requestShowWindow = true;
            }
        }

        public static void DropDownButton(Rect rect, GUIContent content, GUIStyle style, Func<DropdownWindow<T>> createWindow)
        {
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, style) || requestShowWindow)
            {
                if (!s_ActiveWindow)
                    ShowWindow(rect, createWindow);
            }
        }

        public static void CheckShowWindow(Rect rect, Func<DropdownWindow<T>> createWindow)
        {
            if (requestShowWindow)
            {
                ShowWindow(rect, createWindow);
            }
        }

        private static void ShowWindow(Rect rect, Func<DropdownWindow<T>> createWindow)
        {
            if (canShow)
            {
                requestShowWindow = false;
                var screenRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
                var window = createWindow();
                if (window != null)
                {
                    window.ShowAsDropDown(screenRect, window.position.size);
                    GUIUtility.ExitGUI();
                }
            }
        }

        protected virtual void OnDestroy()
        {
            s_CloseTime = EditorApplication.timeSinceStartup;
        }
    }
}
