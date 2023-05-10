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
    internal class ResourceLoader : ISerializationCallbackReceiver
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
            internal static readonly string[] packageManagerComponents =
            {
                "StyleSheets/PackageManager/PackageDetailsDependenciesTab.uss",
                "StyleSheets/PackageManager/PackageDetails.uss",
                "StyleSheets/PackageManager/PackageItem.uss",
                "StyleSheets/PackageManager/PackageList.uss",
                "StyleSheets/PackageManager/PackageLoadBar.uss",
                "StyleSheets/PackageManager/PackageDetailsSamplesTab.uss",
                "StyleSheets/PackageManager/PackageDetailsReleasesTab.uss",
                "StyleSheets/PackageManager/PackageDetailsVersionsTab.uss",
                "StyleSheets/PackageManager/PackageDetailsImportedAssetsTab.uss",
                "StyleSheets/PackageManager/PackageSearchBar.uss",
                "StyleSheets/PackageManager/PackageStatusBar.uss",
                "StyleSheets/PackageManager/PackageToolbar.uss",
                "StyleSheets/PackageManager/ProgressBar.uss",
                "StyleSheets/PackageManager/FeatureDependencies.uss",
                "StyleSheets/PackageManager/PackageDetailsHeader.uss",
                "StyleSheets/PackageManager/Sidebar.uss",
                "StyleSheets/PackageManager/SignInBar.uss",
            };

            internal static readonly string filtersDropdown = "StyleSheets/PackageManager/Filters.uss";
            internal static readonly string inputDropdown = "StyleSheets/PackageManager/InputDropdown.uss";
            internal static readonly string inProgressDropdown = "StyleSheets/PackageManager/InProgressDropdown.uss";

            internal static readonly string selectionWindowCommon = "StyleSheets/PackageManager/SelectionWindow.uss";
            internal static string selectionWindowVariables => EditorGUIUtility.isProSkin ?
                "StyleSheets/PackageManager/SelectionWindowDark.uss" :
                "StyleSheets/PackageManager/SelectionWindowLight.uss";
        }

        private enum StyleSheetType : int
        {
            PackageManagerCommon = 0,
            PackageManagerWindow,
            InputDropdown,
            FiltersDropdown,
            InProgressDropdown,
            SelectionWindow,

            Count
        }

        private const string k_TemplateRoot = "UXML/PackageManager/";

        private static string lightOrDarkTheme => EditorGUIUtility.isProSkin ? "Dark" : "Light";

        // We keep a static array here so that even if we create multiple instances of resource loaders
        // they'll still share style sheets instead of creating new ones. Style sheets are ScriptableObjects
        // so they silently survive domain reload even when resource loaders are not serialized
        private static readonly int[] s_ResolvedDarkStyleSheetIds = new int[(int)StyleSheetType.Count];
        private static readonly int[] s_ResolvedLightStyleSheetIds = new int[(int)StyleSheetType.Count];

        [SerializeField]
        private int[] m_SerializedResolvedDarkStyleSheetIds;

        [SerializeField]
        private int[] m_SerializedResolvedLightStyleSheetIds;

        private static int[] resolvedStyleSheetIds => EditorGUIUtility.isProSkin ? s_ResolvedDarkStyleSheetIds : s_ResolvedLightStyleSheetIds;

        public void OnBeforeSerialize()
        {
            m_SerializedResolvedDarkStyleSheetIds = s_ResolvedDarkStyleSheetIds;
            m_SerializedResolvedLightStyleSheetIds = s_ResolvedLightStyleSheetIds;
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < (int)StyleSheetType.Count; i++)
            {
                if (s_ResolvedDarkStyleSheetIds[i] == 0 && m_SerializedResolvedDarkStyleSheetIds[i] != 0)
                    s_ResolvedDarkStyleSheetIds[i] = m_SerializedResolvedDarkStyleSheetIds[i];
                if (s_ResolvedLightStyleSheetIds[i] == 0 && m_SerializedResolvedLightStyleSheetIds[i] != 0)
                    s_ResolvedLightStyleSheetIds[i] = m_SerializedResolvedLightStyleSheetIds[i];
            }
        }

        private StyleSheet FindResolvedStyleSheetFromType(StyleSheetType styleSheetType)
        {
            var styleSheetId = resolvedStyleSheetIds[(int)styleSheetType];
            if (styleSheetId != 0)
                return UnityEngine.Object.FindObjectFromInstanceID(styleSheetId) as StyleSheet;
            return null;
        }

        public StyleSheet packageManagerCommonStyleSheet
        {
            get
            {
                return FindResolvedStyleSheetFromType(StyleSheetType.PackageManagerCommon)
                    ?? ResolveStyleSheets(StyleSheetType.PackageManagerCommon,
                                          StyleSheetPath.defaultCommon,
                                          StyleSheetPath.extensionVariables,
                                          StyleSheetPath.packageManagerVariables,
                                          StyleSheetPath.packageManagerCommon);
            }
        }

        public StyleSheet packageManagerWindowStyleSheet
        {
            get
            {
                var styleSheet = FindResolvedStyleSheetFromType(StyleSheetType.PackageManagerWindow);
                if (styleSheet == null)
                {
                    var styleSheetsToResolve = StyleSheetPath.packageManagerComponents.Select(p =>
                    {
                        var styleSheet = m_ApplicationProxy.Load<StyleSheet>(p);
                        if (styleSheet == null)
                            throw new ResourceLoaderException($"Unable to load styleSheet {p}");
                        return styleSheet;
                    }).Concat(new[] { packageManagerCommonStyleSheet }).ToArray();
                    styleSheet = ResolveStyleSheets(StyleSheetType.PackageManagerWindow, styleSheetsToResolve);
                }
                return styleSheet;
            }
        }

        public StyleSheet filtersDropdownStyleSheet
        {
            get
            {
                return FindResolvedStyleSheetFromType(StyleSheetType.FiltersDropdown)
                    ?? ResolveStyleSheets(StyleSheetType.FiltersDropdown,
                                          StyleSheetPath.packageManagerVariables,
                                          StyleSheetPath.filtersDropdown);
            }
        }

        public StyleSheet inputDropdownStyleSheet
        {
            get
            {
                return FindResolvedStyleSheetFromType(StyleSheetType.InputDropdown)
                    ?? ResolveStyleSheets(StyleSheetType.InputDropdown,
                                          StyleSheetPath.defaultCommon,
                                          StyleSheetPath.packageManagerVariables,
                                          StyleSheetPath.inputDropdown);
            }
        }

        public StyleSheet inProgressDropdownStyleSheet
        {
            get
            {
                return FindResolvedStyleSheetFromType(StyleSheetType.InProgressDropdown)
                    ?? ResolveStyleSheets(StyleSheetType.InProgressDropdown,
                                          StyleSheetPath.defaultCommon,
                                          StyleSheetPath.packageManagerVariables,
                                          StyleSheetPath.inProgressDropdown);
            }
        }

        public StyleSheet selectionWindowStyleSheet
        {
            get
            {
                var styleSheet = FindResolvedStyleSheetFromType(StyleSheetType.SelectionWindow);
                if (styleSheet == null)
                    styleSheet = ResolveStyleSheets(StyleSheetType.SelectionWindow,
                        StyleSheetPath.selectionWindowCommon,
                        StyleSheetPath.selectionWindowVariables);
                return styleSheet;
            }
        }

        private StyleSheet ResolveStyleSheets(StyleSheetType styleSheetType, params string[] styleSheetPaths)
        {
            return ResolveStyleSheets(styleSheetType, styleSheetPaths.Select(p =>
            {
                var styleSheet = m_ApplicationProxy.Load<StyleSheet>(p);
                if (styleSheet == null)
                    throw new ResourceLoaderException($"Unable to load styleSheet {p}");
                return styleSheet;
            }).ToArray());
        }

        private StyleSheet ResolveStyleSheets(StyleSheetType styleSheetType, params StyleSheet[] styleSheets)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isDefaultStyleSheet = true;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(styleSheets);
            resolver.ResolveTo(styleSheet);

            styleSheet.name = styleSheetType.ToString() + lightOrDarkTheme;
            resolvedStyleSheetIds[(int)styleSheetType] = styleSheet.GetInstanceID();

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
            foreach (var styleSheetId in s_ResolvedDarkStyleSheetIds.Concat(s_ResolvedLightStyleSheetIds).Where(id => id != 0))
                UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectFromInstanceID(styleSheetId));

            for (var i = 0; i < (int)StyleSheetType.Count; i++)
            {
                s_ResolvedDarkStyleSheetIds[i] = 0;
                s_ResolvedLightStyleSheetIds[i] = 0;
            }
        }
    }
}
