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
    internal class ResourceLoader
    {
        private const string k_TemplateRoot = "UXML/PackageManager/";

        private const string k_PackageManagerFiltersStyleSheetPath = "StyleSheets/PackageManager/Filters.uss";
        private const string k_PackageManagerCommonStyleSheetPath = "StyleSheets/PackageManager/Common.uss";
        private const string k_PackageManagerDarkVariablesSheetPath = "StyleSheets/PackageManager/Dark.uss";
        private const string k_PackageManagerLightVariablesSheetPath = "StyleSheets/PackageManager/Light.uss";
        private const string k_ExtensionDarkVariablesSheetPath = "StyleSheets/Extensions/base/dark.uss";
        private const string k_ExtensionLightVariablesSheetPath = "StyleSheets/Extensions/base/light.uss";
        private const string s_PublicNorthstarCommonVariableStyleSheetPath = "UIPackageResources/StyleSheets/Default/Variables/Public/common.uss";
        private const string k_PublicNorthstarDarkVariablesSheetPath = "UIPackageResources/StyleSheets/Default/Northstar/Palette/dark.uss";
        private const string k_PublicNorthstarLightVariablesSheetPath = "UIPackageResources/StyleSheets/Default/Northstar/Palette/light.uss";

        private static readonly string[] k_PackageManagerStyleSheetPaths = new string[] {"StyleSheets/PackageManager/PackageDependencies.uss", "StyleSheets/PackageManager/PackageDetails.uss", "StyleSheets/PackageManager/PackageItem.uss",
                                                                                         "StyleSheets/PackageManager/PackageList.uss", "StyleSheets/PackageManager/PackageLoadBar.uss", "StyleSheets/PackageManager/PackageSampleList.uss", "StyleSheets/PackageManager/PackageStatusBar.uss",
                                                                                         "StyleSheets/PackageManager/PackageToolbar.uss", "StyleSheets/PackageManager/ProgressBar.uss"};

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

        private StyleSheet m_DarkFilterStyleSheet;
        private StyleSheet darkFilterStyleSheet
        {
            get
            {
                if (m_DarkFilterStyleSheet == null)
                    m_DarkFilterStyleSheet = LoadAndResolveFilterStyleSheet(true);
                return m_DarkFilterStyleSheet;
            }
        }

        private StyleSheet m_LightFilterStyleSheet;
        private StyleSheet lightFilterStyleSheet
        {
            get
            {
                if (m_LightFilterStyleSheet == null)
                    m_LightFilterStyleSheet = LoadAndResolveFilterStyleSheet(false);
                return m_LightFilterStyleSheet;
            }
        }

        private StyleSheet LoadAndResolveStyleSheet(bool isDarkTheme)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var packageManagerThemeVariablesSheetPath = isDarkTheme ? k_PackageManagerDarkVariablesSheetPath : k_PackageManagerLightVariablesSheetPath;
            var variablesThemeStyleSheetPath = isDarkTheme ? UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath : UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;
            var extensionThemeStyleSheetPath = isDarkTheme ? k_ExtensionDarkVariablesSheetPath : k_ExtensionLightVariablesSheetPath;
            var northstarThemeStyleSheetPath = isDarkTheme ? k_PublicNorthstarDarkVariablesSheetPath : k_PublicNorthstarLightVariablesSheetPath;

            var packageManagerCommon = EditorGUIUtility.Load(k_PackageManagerCommonStyleSheetPath) as StyleSheet;
            var packageManagerTheme = EditorGUIUtility.Load(packageManagerThemeVariablesSheetPath) as StyleSheet;

            var packageManagerStyles = k_PackageManagerStyleSheetPaths.Select(p => EditorGUIUtility.Load(p) as StyleSheet).ToArray();

            var variableThemeSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(variablesThemeStyleSheetPath)) as StyleSheet;
            var extensionThemeStyleSheet = EditorGUIUtility.Load(extensionThemeStyleSheetPath) as StyleSheet;
            var northstarCommonVariablesStyleSheet = EditorGUIUtility.Load(s_PublicNorthstarCommonVariableStyleSheetPath) as StyleSheet;
            var northstarVariablesStyleSheet = EditorGUIUtility.Load(northstarThemeStyleSheetPath) as StyleSheet;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(variableThemeSheet, extensionThemeStyleSheet, northstarCommonVariablesStyleSheet, northstarVariablesStyleSheet, packageManagerCommon, packageManagerTheme);
            resolver.AddStyleSheets(packageManagerStyles);
            resolver.ResolveTo(styleSheet);

            return styleSheet;
        }

        private StyleSheet LoadAndResolveFilterStyleSheet(bool isDarkTheme)
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

        private int m_NestedGetTemplateDepth = 0;
        public virtual VisualElement GetTemplate(string templateFilename)
        {
            m_NestedGetTemplateDepth++;
            var fullTemplatePath = k_TemplateRoot + templateFilename;
            var visualTreeAsset = EditorGUIUtility.Load(fullTemplatePath) as VisualTreeAsset;
            var result = visualTreeAsset?.Instantiate();
            m_NestedGetTemplateDepth--;

            // A `GetTemplate` call could implicitly call itself again, creating nested GetTemplate calls.
            // We only want to call localization in the top level `GetTemplate` call to avoid multiple localization attempts on the same element.
            if (m_NestedGetTemplateDepth == 0)
                LocalizeVisualElement(result, L10n.Tr);
            return result;
        }

        public virtual void LocalizeVisualElement(VisualElement visualElement, Func<string, string> l10nFunc)
        {
            if (visualElement == null)
                return;

            visualElement.Query().Descendents<VisualElement>().ForEach((element) =>
            {
                if (!string.IsNullOrEmpty(element.tooltip))
                    element.tooltip = l10nFunc(element.tooltip);

                var textElement = element as TextElement;
                if (!string.IsNullOrEmpty(textElement?.text))
                    textElement.text = l10nFunc(textElement.text);
            });
        }

        public virtual StyleSheet GetMainWindowStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkStyleSheet : lightStyleSheet;
        }

        public virtual StyleSheet GetFiltersWindowStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? darkFilterStyleSheet : lightFilterStyleSheet;
        }
    }
}
