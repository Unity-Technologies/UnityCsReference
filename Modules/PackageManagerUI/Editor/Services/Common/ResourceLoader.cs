// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class ResourceLoader
    {
        private const string k_TemplateRoot = "UXML/PackageManager/";

        private const string k_PackageManagerInputDropdownStyleSheetPath = "StyleSheets/PackageManager/InputDropdown.uss";
        private const string k_PackageManagerFiltersStyleSheetPath = "StyleSheets/PackageManager/Filters.uss";
        private const string k_PackageManagerDarkVariablesSheetPath = "StyleSheets/PackageManager/Dark.uss";
        private const string k_PackageManagerLightVariablesSheetPath = "StyleSheets/PackageManager/Light.uss";
        private const string k_ExtensionDarkVariablesSheetPath = "StyleSheets/Extensions/base/dark.uss";
        private const string k_ExtensionLightVariablesSheetPath = "StyleSheets/Extensions/base/light.uss";

        private static readonly string[] k_PackageManagerStyleSheetPaths = new string[]
        {
            "StyleSheets/PackageManager/Common.uss",
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
                {
                    var commonStyleSheets = new[] { GetDefaultCommonStyleSheet(true), GetExtensionStyleSheet(true), GetPackageManagerVariablesStyleSheet(true) };
                    m_DarkStyleSheet = ResolveStyleSheets(commonStyleSheets.Concat(GetPackageManagerStyles()).ToArray());
                }
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
                {
                    var commonStyleSheets = new[] { GetDefaultCommonStyleSheet(false), GetExtensionStyleSheet(false), GetPackageManagerVariablesStyleSheet(false) };
                    m_LightStyleSheet = ResolveStyleSheets(commonStyleSheets.Concat(GetPackageManagerStyles()).ToArray());
                }
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
                    m_DarkFilterStyleSheet = ResolveStyleSheets(GetPackageManagerVariablesStyleSheet(true), EditorGUIUtility.Load(k_PackageManagerFiltersStyleSheetPath) as StyleSheet);
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
                    m_LightFilterStyleSheet = ResolveStyleSheets(GetPackageManagerVariablesStyleSheet(false), EditorGUIUtility.Load(k_PackageManagerFiltersStyleSheetPath) as StyleSheet);
                return m_LightFilterStyleSheet;
            }
        }

        [SerializeField]
        private StyleSheet m_DarkInputDropdownStyleSheet;
        private StyleSheet darkInputDropdownStyleSheet
        {
            get
            {
                if (m_DarkInputDropdownStyleSheet == null)
                    m_DarkInputDropdownStyleSheet = ResolveStyleSheets(GetDefaultCommonStyleSheet(true), GetPackageManagerVariablesStyleSheet(true), EditorGUIUtility.Load(k_PackageManagerInputDropdownStyleSheetPath) as StyleSheet);
                return m_DarkInputDropdownStyleSheet;
            }
        }

        [SerializeField]
        private StyleSheet m_LightInputDropdownStyleSheet;
        private StyleSheet lightInputDropdownStyleSheet
        {
            get
            {
                if (m_LightInputDropdownStyleSheet == null)
                    m_LightInputDropdownStyleSheet = ResolveStyleSheets(GetDefaultCommonStyleSheet(false), GetPackageManagerVariablesStyleSheet(false), EditorGUIUtility.Load(k_PackageManagerInputDropdownStyleSheetPath) as StyleSheet);
                return m_LightInputDropdownStyleSheet;
            }
        }

        private StyleSheet GetDefaultCommonStyleSheet(bool isDarkTheme)
        {
            var stylesheetPath = isDarkTheme ? UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath : UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;
            return EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(stylesheetPath)) as StyleSheet;
        }

        private StyleSheet GetExtensionStyleSheet(bool isDarkTheme)
        {
            return EditorGUIUtility.Load(isDarkTheme ? k_ExtensionDarkVariablesSheetPath : k_ExtensionLightVariablesSheetPath) as StyleSheet;
        }

        private StyleSheet GetPackageManagerVariablesStyleSheet(bool isDarkTheme)
        {
            return EditorGUIUtility.Load(isDarkTheme ? k_PackageManagerDarkVariablesSheetPath : k_PackageManagerLightVariablesSheetPath) as StyleSheet;
        }

        private IEnumerable<StyleSheet> GetPackageManagerStyles()
        {
            return k_PackageManagerStyleSheetPaths.Select(p => EditorGUIUtility.Load(p) as StyleSheet);
        }

        private static StyleSheet ResolveStyleSheets(params StyleSheet[] sheets)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(sheets);
            resolver.ResolveTo(styleSheet);
            return styleSheet;
        }

        private int m_NestedGetTemplateDepth;
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
            visualElement?.Query().Descendents<VisualElement>().ForEach((element) =>
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

        public virtual StyleSheet GetInputDropdownWindowStylesheet()
        {
            return EditorGUIUtility.isProSkin ? darkInputDropdownStyleSheet : lightInputDropdownStyleSheet;
        }
    }
}
