// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
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

        const float kExposureSliderAbsoluteMax = 23.0f;
        static readonly GUIContent s_ExposureIcon = EditorGUIUtility.TrIconContent("Exposure", "Controls the number of stops to over or under expose the lightmap.");

        static internal Rect GetTooltipRect() { return tooltipRect; }
        static internal string GetMouseTooltip() { return mouseTooltip; }
        internal static bool DoToggleForward(Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            Event evt = Event.current;

            if (EditorGUI.showMixedValue)
                style = mixedToggleStyle;

            // Ignore mouse clicks that are not with the primary (left) mouse button so those can be grabbed by other things later.
            EventType origType = evt.type;
            bool nonLeftClick = (evt.type == EventType.MouseDown && evt.button != 0);
            if (nonLeftClick)
                evt.type = EventType.Ignore;
            bool returnValue = DoToggle(position, id, EditorGUI.showMixedValue ? false : value, content, style);
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

        internal static float ExposureSlider(float value, ref float maxValue, GUIStyle style)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20;
            value = EditorGUILayout.Slider(s_ExposureIcon, value, -maxValue, maxValue, -kExposureSliderAbsoluteMax, kExposureSliderAbsoluteMax, style, GUILayout.MaxWidth(64));

            // This will allow the user to set a new max value for the current session
            if (value >= 0)
                maxValue = Mathf.Max(maxValue, value);
            else
                maxValue = Mathf.Max(maxValue, value * -1);

            EditorGUIUtility.labelWidth = labelWidth;

            return value;
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
