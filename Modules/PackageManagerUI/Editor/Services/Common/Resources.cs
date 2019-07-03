// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class Resources
    {
        private const string k_TemplateRoot = "UXML/PackageManager/";

        private const string k_PackageManagerCommonStyleSheetPath = "StyleSheets/PackageManager/Common.uss";
        private const string k_PackageManagerDarkVariablesSheetPath = "StyleSheets/PackageManager/Dark.uss";
        private const string k_PackageManagerLightVariablesSheetPath = "StyleSheets/PackageManager/Light.uss";

        private static StyleSheet m_DarkStyleSheet;
        private static StyleSheet darkStyleSheet
        {
            get
            {
                if (m_DarkStyleSheet == null)
                    m_DarkStyleSheet = LoadAndResolveStyleSheet(true);
                return m_DarkStyleSheet;
            }
        }

        private static StyleSheet m_LightStyleSheet;
        private static StyleSheet lightStyleSheet
        {
            get
            {
                if (m_LightStyleSheet == null)
                    m_LightStyleSheet = LoadAndResolveStyleSheet(false);
                return m_LightStyleSheet;
            }
        }

        private static StyleSheet LoadAndResolveStyleSheet(bool isDarkTheme)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var packageManagerThemeVariablesSheetPath = isDarkTheme ? k_PackageManagerDarkVariablesSheetPath : k_PackageManagerLightVariablesSheetPath;
            var variablesThemeStyleSheetPath = isDarkTheme ? UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath : UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;

            var packageManagerCommon = EditorGUIUtility.Load(k_PackageManagerCommonStyleSheetPath) as StyleSheet;
            var packageManagerTheme = EditorGUIUtility.Load(packageManagerThemeVariablesSheetPath) as StyleSheet;

            var variableThemeSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(variablesThemeStyleSheetPath)) as StyleSheet;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(variableThemeSheet, packageManagerCommon, packageManagerTheme);
            resolver.ResolveTo(styleSheet);

            return styleSheet;
        }

        private static string TemplatePath(string filename)
        {
            return k_TemplateRoot + filename;
        }

        public static VisualTreeAsset GetVisualTreeAsset(string templateFilename)
        {
            return EditorGUIUtility.Load(TemplatePath(templateFilename)) as VisualTreeAsset;
        }

        public static VisualElement GetTemplate(string templateFilename)
        {
            return GetVisualTreeAsset(templateFilename)?.CloneTree();
        }

        public static string GetIconPath(string iconName)
        {
            return $"Icons/PackageManager/{(EditorGUIUtility.isProSkin ? "Dark" : "Light")}/{iconName}.png";
        }

        public static StyleSheet GetStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkStyleSheet : lightStyleSheet;
        }
    }
}
