// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class Resources
    {
        private const string k_TemplateRoot = "UXML/PackageManager/";

        private const string k_PackageManagerFiltersStyleSheetPath = "StyleSheets/PackageManager/Filters.uss";
        private const string k_PackageManagerCommonStyleSheetPath = "StyleSheets/PackageManager/Common.uss";
        private const string k_PackageManagerDarkVariablesSheetPath = "StyleSheets/PackageManager/Dark.uss";
        private const string k_PackageManagerLightVariablesSheetPath = "StyleSheets/PackageManager/Light.uss";
        private const string k_ExtensionDarkVariablesSheetPath = "StyleSheets/Extensions/base/dark.uss";
        private const string k_ExtensionLightVariablesSheetPath = "StyleSheets/Extensions/base/light.uss";

        private static readonly string[] k_PackageManagerStyleSheetPaths = new string[] {"StyleSheets/PackageManager/PackageDependencies.uss", "StyleSheets/PackageManager/PackageDetails.uss", "StyleSheets/PackageManager/PackageItem.uss",
                                                                                         "StyleSheets/PackageManager/PackageList.uss", "StyleSheets/PackageManager/PackageLoadBar.uss", "StyleSheets/PackageManager/PackageSampleList.uss", "StyleSheets/PackageManager/PackageStatusBar.uss",
                                                                                         "StyleSheets/PackageManager/PackageToolbar.uss", "StyleSheets/PackageManager/ProgressBar.uss"};

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

        private static StyleSheet m_DarkFilterStyleSheet;
        private static StyleSheet darkFilterStyleSheet
        {
            get
            {
                if (m_DarkFilterStyleSheet == null)
                    m_DarkFilterStyleSheet = LoadAndResolveFilterStyleSheet(true);
                return m_DarkFilterStyleSheet;
            }
        }

        private static StyleSheet m_LightFilterStyleSheet;
        private static StyleSheet lightFilterStyleSheet
        {
            get
            {
                if (m_LightFilterStyleSheet == null)
                    m_LightFilterStyleSheet = LoadAndResolveFilterStyleSheet(false);
                return m_LightFilterStyleSheet;
            }
        }

        private static StyleSheet LoadAndResolveStyleSheet(bool isDarkTheme)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var packageManagerThemeVariablesSheetPath = isDarkTheme ? k_PackageManagerDarkVariablesSheetPath : k_PackageManagerLightVariablesSheetPath;
            var variablesThemeStyleSheetPath = isDarkTheme ? UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath : UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;
            var extensionThemeStyleSheetPath = isDarkTheme ? k_ExtensionDarkVariablesSheetPath : k_ExtensionLightVariablesSheetPath;

            var packageManagerCommon = EditorGUIUtility.Load(k_PackageManagerCommonStyleSheetPath) as StyleSheet;
            var packageManagerTheme = EditorGUIUtility.Load(packageManagerThemeVariablesSheetPath) as StyleSheet;

            var packageManagerStyles = k_PackageManagerStyleSheetPaths.Select(p => EditorGUIUtility.Load(p) as StyleSheet).ToArray();

            var variableThemeSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(variablesThemeStyleSheetPath)) as StyleSheet;
            var extensionThemeStyleSheet = EditorGUIUtility.Load(extensionThemeStyleSheetPath) as StyleSheet;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(variableThemeSheet, extensionThemeStyleSheet, packageManagerCommon, packageManagerTheme);
            resolver.AddStyleSheets(packageManagerStyles);
            resolver.ResolveTo(styleSheet);

            return styleSheet;
        }

        private static StyleSheet LoadAndResolveFilterStyleSheet(bool isDarkTheme)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var packageManagerThemeVariablesSheetPath = EditorGUIUtility.isProSkin ? k_PackageManagerDarkVariablesSheetPath : k_PackageManagerLightVariablesSheetPath;
            var packageManagerTheme = EditorGUIUtility.Load(packageManagerThemeVariablesSheetPath) as StyleSheet;
            var packageManagerFilterCommon = EditorGUIUtility.Load(k_PackageManagerFiltersStyleSheetPath) as StyleSheet;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(packageManagerFilterCommon, packageManagerTheme);
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
            return GetVisualTreeAsset(templateFilename)?.Instantiate();
        }

        public static StyleSheet GetMainWindowStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkStyleSheet : lightStyleSheet;
        }

        public static StyleSheet GetFiltersWindowStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkFilterStyleSheet : lightFilterStyleSheet;
        }
    }
}
