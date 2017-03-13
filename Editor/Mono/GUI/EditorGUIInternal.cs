// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using ICSharpCode.NRefactory.Ast;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    //@todo This should be handled through friend assemblies instead
    internal sealed class EditorGUIInternal : GUI
    {
        // Choose how toggles appear when showing mixed values
        static internal GUIStyle mixedToggleStyle
        {
            get { return s_MixedToggleStyle; }
            set { s_MixedToggleStyle = value; }
        }

        private static GUIStyle s_MixedToggleStyle = EditorStyles.toggleMixed;

        static internal Rect GetTooltipRect() { return tooltipRect; }
        static internal string GetMouseTooltip() { return mouseTooltip; }
        internal static bool DoToggleForward(Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            Event evt = Event.current;

            // Toggle selected toggle on space or return key
            if (evt.MainActionKeyForControl(id))
            {
                value = !value;
                evt.Use();
                GUI.changed = true;
            }

            if (EditorGUI.showMixedValue)
                style = mixedToggleStyle;

            // Ignore mouse clicks that are not with the primary (left) mouse button so those can be grabbed by other things later.
            EventType origType = evt.type;
            bool nonLeftClick = (evt.type == EventType.MouseDown && evt.button != 0);
            if (nonLeftClick)
                evt.type = EventType.Ignore;
            bool returnValue = DoToggle(position, id, EditorGUI.showMixedValue ? false : value, content, style.m_Ptr);
            if (nonLeftClick)
                evt.type = origType;
            else if (evt.type != origType)
                EditorGUIUtility.keyboardControl = id; // If control used event, give it keyboard focus.
            return returnValue;
        }

        internal static Vector2 DoBeginScrollViewForward(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            return DoBeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
        }

        internal static void BeginWindowsForward(int skinMode, int editorWindowInstanceID)
        {
            BeginWindows(skinMode, editorWindowInstanceID);
        }

        internal static void AssetPopup<T>(SerializedProperty serializedProperty, GUIContent content, string fileExtension) where T : Object, new()
        {
            AssetPopup<T>(serializedProperty, content, fileExtension, "Default");
        }

        internal static void AssetPopup<T>(SerializedProperty serializedProperty, GUIContent content, string fileExtension, string defaultFieldName) where T : Object, new()
        {
            AssetPopupBackend.AssetPopup<T>(serializedProperty, content, fileExtension, defaultFieldName);
        }
    }

    //@todo This should be handled through friend assemblies instead
    internal sealed class EditorGUILayoutUtilityInternal : GUILayoutUtility
    {
        internal new static GUILayoutGroup BeginLayoutArea(GUIStyle style, System.Type LayoutType)
        {
            return GUILayoutUtility.DoBeginLayoutArea(style, LayoutType);
        }

        internal new static GUILayoutGroup topLevel
        {
            get { return GUILayoutUtility.topLevel; }
        }
    }
}
