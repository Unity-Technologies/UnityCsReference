// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ResourceLoaderException : Exception
    {
        public ResourceLoaderException(string message) : base(message) {}
    }

    [Serializable]
    internal class ResourceLoader
    {
        internal static class StyleSheetPath
        {
            internal static string defaultCommon => EditorGUIUtility.isProSkin ?
            UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath) :
            UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath);

            internal static string packageManagerVariables => EditorGUIUtility.isProSkin ?
            "StyleSheets/PackageManager/Dark.uss" :
            "StyleSheets/PackageManager/Light.uss";

            internal static string extensionVariables => EditorGUIUtility.isProSkin ?
            "StyleSheets/Extensions/base/dark.uss" :
            "StyleSheets/Extensions/base/light.uss";

            internal static readonly string packageManagerCommon = "StyleSheets/PackageManager/Common.uss";
            internal static readonly string[] packageManagerComponents = {
                "StyleSheets/PackageManager/PackageDependencies.uss",
                "StyleSheets/PackageManager/PackageDetails.uss",
                "StyleSheets/PackageManager/PackageItem.uss",
                "StyleSheets/PackageManager/PackageList.uss",
                "StyleSheets/PackageManager/PackageLoadBar.uss",
                "StyleSheets/PackageManager/PackageSampleList.uss",
                "StyleSheets/PackageManager/PackageStatusBar.uss",
                "StyleSheets/PackageManager/PackageToolbar.uss",
                "StyleSheets/PackageManager/ProgressBar.uss",
                "StyleSheets/PackageManager/FeatureDependencies.uss",
                "StyleSheets/PackageManager/PackagePlatformList.uss"
            };

            internal static readonly string filtersDropdown = "StyleSheets/PackageManager/Filters.uss";
            internal static readonly string inputDropdown = "StyleSheets/PackageManager/InputDropdown.uss";
        }

        private enum StyleSheetId : int
        {
            PackageManagerCommon = 0,
            PackageManagerWindow,
            InputDropdown,
            FiltersDropdown,

            Count
        }

        private const string k_TemplateRoot = "UXML/PackageManager/";

        private static string lightOrDarkTheme => EditorGUIUtility.isProSkin ? "Dark" : "Light";

        [SerializeField]
        private StyleSheet[] m_ResolvedDarkStyleSheets = new StyleSheet[(int)StyleSheetId.Count];

        [SerializeField]
        private StyleSheet[] m_ResolvedLightStyleSheets = new StyleSheet[(int)StyleSheetId.Count];

        private StyleSheet[] resolvedStyleSheets => EditorGUIUtility.isProSkin ? m_ResolvedDarkStyleSheets : m_ResolvedLightStyleSheets;

        public StyleSheet packageManagerCommonStyleSheet
        {
            get
            {
                var styleSheet = resolvedStyleSheets[(int)StyleSheetId.PackageManagerCommon];
                if (styleSheet == null)
                {
                    styleSheet = ResolveStyleSheets(StyleSheetPath.defaultCommon,
                        StyleSheetPath.extensionVariables,
                        StyleSheetPath.packageManagerVariables,
                        StyleSheetPath.packageManagerCommon);
                    styleSheet.name = "PackageManagerCommon" + lightOrDarkTheme;
                    resolvedStyleSheets[(int)StyleSheetId.PackageManagerCommon] = styleSheet;
                }
                return styleSheet;
            }
        }

        public StyleSheet packageManagerWindowStyleSheet
        {
            get
            {
                var styleSheet = resolvedStyleSheets[(int)StyleSheetId.PackageManagerWindow];
                if (styleSheet == null)
                {
                    var styleSheetsToResolve = StyleSheetPath.packageManagerComponents.Select(p =>
                        {
                            var styleSheet = m_ApplicationProxy.Load<StyleSheet>(p);
                            if (styleSheet == null)
                                throw new ResourceLoaderException($"Unable to load styleSheet {p}");
                            return styleSheet;
                        })
                        .Concat(new[] { packageManagerCommonStyleSheet }).ToArray();
                    styleSheet = ResolveStyleSheets(styleSheetsToResolve);
                    styleSheet.name = "PackageManagerWindow" + lightOrDarkTheme;
                    resolvedStyleSheets[(int)StyleSheetId.PackageManagerWindow] = styleSheet;
                }
                return styleSheet;
            }
        }

        public StyleSheet filtersDropdownStyleSheet
        {
            get
            {
                var stylesheet = resolvedStyleSheets[(int)StyleSheetId.FiltersDropdown];
                if (stylesheet == null)
                {
                    stylesheet = ResolveStyleSheets(StyleSheetPath.packageManagerVariables, StyleSheetPath.filtersDropdown);
                    stylesheet.name = "FiltersDropdown" + lightOrDarkTheme;
                    resolvedStyleSheets[(int)StyleSheetId.FiltersDropdown] = stylesheet;
                }
                return stylesheet;
            }
        }

        public StyleSheet inputDropdownStyleSheet
        {
            get
            {
                var styleSheet = resolvedStyleSheets[(int)StyleSheetId.InputDropdown];
                if (styleSheet == null)
                {
                    styleSheet = ResolveStyleSheets(StyleSheetPath.defaultCommon, StyleSheetPath.packageManagerVariables, StyleSheetPath.inputDropdown);
                    styleSheet.name = "InputDropdown" + lightOrDarkTheme;
                    resolvedStyleSheets[(int)StyleSheetId.InputDropdown] = styleSheet;
                }
                return styleSheet;
            }
        }

        private StyleSheet ResolveStyleSheets(params string[] styleSheetPaths)
        {
            return ResolveStyleSheets(styleSheetPaths.Select(p =>
            {
                var styleSheet = m_ApplicationProxy.Load<StyleSheet>(p);
                if (styleSheet == null)
                    throw new ResourceLoaderException($"Unable to load styleSheet {p}");
                return styleSheet;
            }).ToArray());
        }

        private static StyleSheet ResolveStyleSheets(params StyleSheet[] styleSheets)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isDefaultStyleSheet = true;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(styleSheets);
            resolver.ResolveTo(styleSheet);
            return styleSheet;
        }

        private int m_NestedGetTemplateDepth;

        [NonSerialized]
        private ApplicationProxy m_ApplicationProxy;

        public void ResolveDependencies(ApplicationProxy applicationProxy)
        {
            m_ApplicationProxy = applicationProxy;
        }

        public virtual VisualElement GetTemplate(string templateFilename, bool shouldThrowException = true)
        {
            m_NestedGetTemplateDepth++;
            var fullTemplatePath = k_TemplateRoot + templateFilename;
            var visualTreeAsset = m_ApplicationProxy.Load<VisualTreeAsset>(fullTemplatePath);
            var result = visualTreeAsset?.Instantiate();
            m_NestedGetTemplateDepth--;

            // A `GetTemplate` call could implicitly call itself again, creating nested GetTemplate calls.
            // We only want to call localization in the top level `GetTemplate` call to avoid multiple localization attempts on the same element.
            if (m_NestedGetTemplateDepth == 0)
                LocalizeVisualElement(result, L10n.Tr);

            if (result == null && shouldThrowException)
                throw new ResourceLoaderException($"Unable to load resource {templateFilename}");
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

        public void Reset()
        {
            m_ResolvedDarkStyleSheets = new StyleSheet[(int)StyleSheetId.Count];
            m_ResolvedLightStyleSheets = new StyleSheet[(int)StyleSheetId.Count];
            _ = packageManagerCommonStyleSheet;
            _ = packageManagerWindowStyleSheet;
            _ = filtersDropdownStyleSheet;
            _ = inputDropdownStyleSheet;
        }
    }
}
