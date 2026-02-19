// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    static class UIToolkitUtility
    {
        const string k_TimelineClassNameFormat = "timeline-{0}";

        public static void CloneTemplateInto(this VisualElement parent, string templatePath)
        {
            var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);

            if (treeAsset == null)
            {
                Debug.LogError($"Template file not found: {templatePath}");
                return;
            }

            treeAsset.CloneTree(parent);
        }

        public static void ApplyCommonStyleSheet(VisualElement ve)
        {
            UIResources.CommonStylesheet.ApplyTo(ve);
        }

        public static void ApplyStyleSheet(this VisualElement ve, string stylesheetPath)
        {
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetPath);
            if (stylesheet != null)
            {
                ve.styleSheets.Add(stylesheet);
            }
        }

        public static void ApplyStyleSheetWithValidation(this VisualElement ve, string stylesheetPath)
        {
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesheetPath);

            if (stylesheet == null)
            {
                Debug.LogError($"StyleSheet not found: {stylesheetPath}");
                return;
            }

            ve.styleSheets.Add(stylesheet);
        }

        public static void AddToTimelineClassList(this VisualElement ve, string className)
        {
            ve.AddToClassList(string.Format(k_TimelineClassNameFormat, className));
        }

        public static void RemoveFromTimelineClassList(this VisualElement ve, string className)
        {
            ve.RemoveFromClassList(string.Format(k_TimelineClassNameFormat, className));
        }

        public static bool TimelineClassListContains(this VisualElement ve, string className)
        {
            return ve.ClassListContains(string.Format(k_TimelineClassNameFormat, className));
        }
    }
}
