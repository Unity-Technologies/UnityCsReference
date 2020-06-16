// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    internal abstract class DropdownWindow<T> : EditorWindow
    {
        internal static bool requestShowWindow;
        internal static double s_CloseTime;

        internal static bool canShow
        {
            get
            {
                if (EditorApplication.timeSinceStartup - s_CloseTime < 0.250)
                    return false;
                return true;
            }
        }

        public static void RequestShowWindow()
        {
            requestShowWindow = true;
        }

        public static void DropDownButton(Rect rect, Vector2 windowSize, GUIContent content, GUIStyle style, Func<DropdownWindow<T>> createWindow)
        {
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, style) || requestShowWindow)
            {
                if (canShow)
                {
                    requestShowWindow = false;
                    var screenRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
                    var window = createWindow();
                    window.ShowAsDropDown(screenRect, windowSize);
                    GUIUtility.ExitGUI();
                }
            }
        }

        [UsedImplicitly]
        protected virtual void OnDestroy()
        {
            s_CloseTime = EditorApplication.timeSinceStartup;
        }
    }
}
