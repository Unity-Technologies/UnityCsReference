// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class Resources : ScriptableSingleton<Resources>
    {
        private const string k_TemplateRoot = "UXML/PackageManager/";

        private const string k_PackageManagerFiltersStyleSheetPath = "StyleSheets/PackageManager/Filters.uss";
        private const string k_PackageManagerCommonStyleSheetPath = "StyleSheets/PackageManager/Common.uss";
        private const string k_PackageManagerDarkVariablesSheetPath = "StyleSheets/PackageManager/Dark.uss";
        private const string k_PackageManagerLightVariablesSheetPath = "StyleSheets/PackageManager/Light.uss";
        private const string k_ExtensionDarkVariablesSheetPath = "StyleSheets/Extensions/base/dark.uss";
        private const string k_ExtensionLightVariablesSheetPath = "StyleSheets/Extensions/base/light.uss";

        private static readonly string[] k_PackageManagerStyleSheetPaths =
        {
            "StyleSheets/PackageManager/PackageDependencies.uss",
            "StyleSheets/PackageManager/PackageDetails.uss",
            "StyleSheets/PackageManager/PackageItem.uss",
            "StyleSheets/PackageManager/PackageList.uss",
            "StyleSheets/PackageManager/PackageLoadBar.uss",
            "StyleSheets/PackageManager/PackageSampleList.uss",
            "StyleSheets/PackageManager/PackageStatusBar.uss",
            "StyleSheets/PackageManager/PackageToolbar.uss",
            "StyleSheets/PackageManager/ProgressBar.uss"
        };

        [SerializeField]
        private StyleSheet m_DarkStyleSheet;
        private StyleSheet darkStyleSheet
        {
            get
            {
                if (m_DarkStyleSheet == null)
                    m_DarkStyleSheet = LoadAndResolveStyleSheet(true);
                return m_DarkStyleSheet;
            }
        }

        [SerializeField]
        private StyleSheet m_LightStyleSheet;
        private StyleSheet lightStyleSheet
        {
            get
            {
                if (m_LightStyleSheet == null)
                    m_LightStyleSheet = LoadAndResolveStyleSheet(false);
                return m_LightStyleSheet;
            }
        }

        [SerializeField]
        private StyleSheet m_DarkFilterStyleSheet;
        private StyleSheet darkFilterStyleSheet
        {
            get
            {
                if (m_DarkFilterStyleSheet == null)
                    m_DarkFilterStyleSheet = LoadAndResolveFilterStyleSheet();
                return m_DarkFilterStyleSheet;
            }
        }

        [SerializeField]
        private StyleSheet m_LightFilterStyleSheet;
        private StyleSheet lightFilterStyleSheet
        {
            get
            {
                if (m_LightFilterStyleSheet == null)
                    m_LightFilterStyleSheet = LoadAndResolveFilterStyleSheet();
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

        private static StyleSheet LoadAndResolveFilterStyleSheet()
        {
            var styleSheet = CreateInstance<StyleSheet>();
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

        public StyleSheet GetMainWindowStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkStyleSheet : lightStyleSheet;
        }

        public StyleSheet GetFiltersWindowStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkFilterStyleSheet : lightFilterStyleSheet;
        }
    }
}
