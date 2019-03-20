// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Internal;
using System;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        public static bool BeginFoldoutHeaderGroup(bool foldout, string content, [DefaultValue("EditorStyles.foldoutHeader")]
            GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            return BeginFoldoutHeaderGroup(foldout, GUIContent.Temp(content), style, menuAction, menuIcon);
        }

        public static bool BeginFoldoutHeaderGroup(bool foldout, GUIContent content, [DefaultValue("EditorStyles.foldoutHeader")] GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            if (style == null)
                style = EditorStyles.foldoutHeader;
            var backgroundRect = GUILayoutUtility.GetRect(content, style);
            return EditorGUI.BeginFoldoutHeaderGroup(backgroundRect, foldout, content, style, menuAction, menuIcon);
        }

        public static void EndFoldoutHeaderGroup()
        {
            EditorGUI.EndFoldoutHeaderGroup();
        }
    }

    public sealed partial class EditorGUI
    {
        static bool s_FoldoutHeaderGroupActive;

        public static bool BeginFoldoutHeaderGroup(Rect position, bool foldout, string content, [DefaultValue("EditorStyles.foldoutHeader")]
            GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            return BeginFoldoutHeaderGroup(position, foldout, GUIContent.Temp(content), style, menuAction, menuIcon);
        }

        public static bool BeginFoldoutHeaderGroup(Rect position, bool foldout, GUIContent content, [DefaultValue("EditorStyles.foldoutHeader")] GUIStyle style = null, Action<Rect> menuAction = null, GUIStyle menuIcon = null)
        {
            // Removing the default margin for inspectors
            if (EditorGUIUtility.hierarchyMode)
            {
                position.xMin -= EditorStyles.inspectorDefaultMargins.padding.left - EditorStyles.inspectorDefaultMargins.padding.right;
            }

            if (style == null)
                style = EditorStyles.foldoutHeader;
            if (s_FoldoutHeaderGroupActive)
            {
                EditorGUI.HelpBox(position, L10n.Tr("You can't nest Foldout Headers, end it with EndHeaderFoldoutGroup."), MessageType.Error);
                return false;
            }
            s_FoldoutHeaderGroupActive = true;

            const float iconSize = 16;
            Rect menuRect = new Rect
            {
                x = position.xMax - style.padding.right - iconSize,
                y = position.y + style.padding.top,
                size = Vector2.one * iconSize
            };

            bool menuIconHover = menuRect.Contains(Event.current.mousePosition);
            bool menuIconActive = (menuIconHover && Event.current.type == EventType.MouseDown && Event.current.button == 0);
            if (menuAction != null && menuIconActive)
            {
                menuAction.Invoke(menuRect);
                Event.current.Use();
            }

            foldout = GUI.Toggle(position, foldout, content, style);

            // Menu icon
            if (menuAction != null && Event.current.type == EventType.Repaint)
            {
                if (menuIcon == null)
                    menuIcon = EditorStyles.foldoutHeaderIcon;

                menuIcon.Draw(menuRect, menuIconHover, menuIconActive, false, false);
            }

            return foldout;
        }

        public static void EndFoldoutHeaderGroup()
        {
            s_FoldoutHeaderGroupActive = false;
        }
    }
}
