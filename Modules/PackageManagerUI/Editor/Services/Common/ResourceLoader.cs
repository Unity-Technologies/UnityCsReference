// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IResourceLoader : IService
    {
        StyleSheet packageManagerCommonStyleSheet { get; }
        StyleSheet packageManagerWindowStyleSheet { get; }
        StyleSheet filtersDropdownStyleSheet { get; }
        StyleSheet inputDropdownStyleSheet { get; }
        StyleSheet inProgressDropdownStyleSheet { get; }
        StyleSheet selectionWindowStyleSheet { get; }
        StyleSheet customDisplayDialogStyleSheet { get; }
        StyleSheet exportWindowStyleSheet { get; }
        StyleSheet activeTrustWindowStyleSheet { get; }
        VisualElement GetTemplate(string templateFilename, bool shouldThrowException = true);
    }

    internal class ResourceLoaderException : Exception
    {
        public ResourceLoaderException(string message) : base(message) {}
    }

    [Serializable]
    internal class ResourceLoader : BaseService<IResourceLoader>, IResourceLoader, ISerializationCallbackReceiver
    {
        internal static class StyleSheetPath
        {
            public static string defaultCommon => EditorGUIUtility.isProSkin ?
            UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath) :
            UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath);

            public static string packageManagerVariables => EditorGUIUtility.isProSkin ?
            "StyleSheets/PackageManager/Dark.uss" :
            "StyleSheets/PackageManager/Light.uss";

            public static string extensionVariables => EditorGUIUtility.isProSkin ?
            "StyleSheets/Extensions/base/dark.uss" :
            "StyleSheets/Extensions/base/light.uss";

            public static readonly string packageManagerCommon = "StyleSheets/PackageManager/Common.uss";
            public static readonly string[] packageManagerComponents =
            {
                "StyleSheets/PackageManager/PackageDetailsDependenciesTab.uss",
                "StyleSheets/PackageManager/PackageDetails.uss",
                "StyleSheets/PackageManager/ListItem.uss",
                "StyleSheets/PackageManager/ListArea.uss",
                "StyleSheets/PackageManager/PackageLoadBar.uss",
                "StyleSheets/PackageManager/PackageDetailsSamplesTab.uss",
                "StyleSheets/PackageManager/PackageDetailsReleasesTab.uss",
                "StyleSheets/PackageManager/PackageDetailsVersionsTab.uss",
                "StyleSheets/PackageManager/PackageDetailsImportedAssetsTab.uss",
                "StyleSheets/PackageManager/SampleDetails.uss",
                "StyleSheets/PackageManager/PackageSearchBar.uss",
                "StyleSheets/PackageManager/PackageStatusBar.uss",
                "StyleSheets/PackageManager/PackageToolbar.uss",
                "StyleSheets/PackageManager/ProgressBar.uss",
                "StyleSheets/PackageManager/FeatureDependencies.uss",
                "StyleSheets/PackageManager/PackageDetailsHeader.uss",
                "StyleSheets/PackageManager/Sidebar.uss",
                "StyleSheets/PackageManager/SignInBar.uss",
                "StyleSheets/PackageManager/PartiallyNonCompliantRegistryMessage.uss",
                "StyleSheets/PackageManager/MainContainerOverlay.uss",
                "StyleSheets/PackageManager/MultiSelectDetails.uss"
            };

            public static readonly string filtersDropdown = "StyleSheets/PackageManager/Filters.uss";
            public static readonly string inputDropdown = "StyleSheets/PackageManager/InputDropdown.uss";
            public static readonly string inProgressDropdown = "StyleSheets/PackageManager/InProgressDropdown.uss";
            public static readonly string customDisplayDialog = "StyleSheets/PackageManager/CustomDisplayDialog.uss";
            public static readonly string exportWindowStyleSheet = "StyleSheets/PackageManager/ExportWindow.uss";
            public static readonly string activeTrustWindowStyleSheet = "StyleSheets/PackageManager/ActiveTrustWindow.uss";

            public static readonly string selectionWindowCommon = "StyleSheets/PackageManager/SelectionWindow.uss";
            public static string selectionWindowVariables => EditorGUIUtility.isProSkin ?
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
            CustomDisplayDialog,
            ExportWindow,
            ActiveTrustWindow,

            Count
        }

        private const string k_TemplateRoot = "UXML/PackageManager/";

        private static string lightOrDarkTheme => EditorGUIUtility.isProSkin ? "Dark" : "Light";

        // We keep a static array here so that even if we create multiple instances of resource loaders
        // they'll still share style sheets instead of creating new ones. Style sheets are ScriptableObjects
        // so they silently survive domain reload even when resource loaders are not serialized
        private static readonly EntityId[] s_ResolvedDarkStyleSheetIds = new EntityId[(int)StyleSheetType.Count];
        private static readonly EntityId[] s_ResolvedLightStyleSheetIds = new EntityId[(int)StyleSheetType.Count];

        [SerializeField]
        private EntityId[] m_SerializedResolvedDarkStyleSheetIds;

        [SerializeField]
        private EntityId[] m_SerializedResolvedLightStyleSheetIds;

        [SerializeField]
        private bool m_ModalStylesheetPreloaded = false;

        private static EntityId[] resolvedStyleSheetIds => EditorGUIUtility.isProSkin ? s_ResolvedDarkStyleSheetIds : s_ResolvedLightStyleSheetIds;

        public override void OnEnable()
        {
            base.OnEnable();
            if (!m_ModalStylesheetPreloaded)
            {
                EditorApplication.delayCall += () =>
                {
                    _ = customDisplayDialogStyleSheet;
                    _ = exportWindowStyleSheet;
                    _ = activeTrustWindowStyleSheet;
                    m_ModalStylesheetPreloaded = true;
                };
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedResolvedDarkStyleSheetIds = s_ResolvedDarkStyleSheetIds;
            m_SerializedResolvedLightStyleSheetIds = s_ResolvedLightStyleSheetIds;
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < (int)StyleSheetType.Count; i++)
            {
                if (s_ResolvedDarkStyleSheetIds[i] == EntityId.None && m_SerializedResolvedDarkStyleSheetIds[i] != EntityId.None)
                    s_ResolvedDarkStyleSheetIds[i] = m_SerializedResolvedDarkStyleSheetIds[i];
                if (s_ResolvedLightStyleSheetIds[i] == EntityId.None && m_SerializedResolvedLightStyleSheetIds[i] != EntityId.None)
                    s_ResolvedLightStyleSheetIds[i] = m_SerializedResolvedLightStyleSheetIds[i];
            }
        }

        private StyleSheet FindResolvedStyleSheetFromType(StyleSheetType styleSheetType)
        {
            var styleSheetId = resolvedStyleSheetIds[(int)styleSheetType];
            if (styleSheetId != EntityId.None)
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
                if (styleSheet is null)
                {
                    var styleSheetsToResolve = StyleSheetPath.packageManagerComponents
                        .SelectAsEnumerable(p => Load<StyleSheet>(p) ?? throw new ResourceLoaderException($"Unable to load styleSheet {p}"))
                        .Join(packageManagerCommonStyleSheet);
                    styleSheet = ResolveStyleSheets(StyleSheetType.PackageManagerWindow, styleSheetsToResolve);
                }
                return styleSheet;
            }
        }

        public StyleSheet filtersDropdownStyleSheet =>
            FindResolvedStyleSheetFromType(StyleSheetType.FiltersDropdown)
            ?? ResolveStyleSheets(StyleSheetType.FiltersDropdown,
                StyleSheetPath.packageManagerVariables,
                StyleSheetPath.filtersDropdown);

        public StyleSheet inputDropdownStyleSheet =>
            FindResolvedStyleSheetFromType(StyleSheetType.InputDropdown)
            ?? ResolveStyleSheets(StyleSheetType.InputDropdown,
                StyleSheetPath.defaultCommon,
                StyleSheetPath.packageManagerVariables,
                StyleSheetPath.inputDropdown);

        public StyleSheet inProgressDropdownStyleSheet =>
            FindResolvedStyleSheetFromType(StyleSheetType.InProgressDropdown)
            ?? ResolveStyleSheets(StyleSheetType.InProgressDropdown,
                StyleSheetPath.defaultCommon,
                StyleSheetPath.packageManagerVariables,
                StyleSheetPath.inProgressDropdown);

        public StyleSheet customDisplayDialogStyleSheet =>
            FindResolvedStyleSheetFromType(StyleSheetType.CustomDisplayDialog)
            ?? ResolveStyleSheets(StyleSheetType.CustomDisplayDialog,
                        StyleSheetPath.packageManagerVariables,
                        StyleSheetPath.customDisplayDialog);

        public StyleSheet exportWindowStyleSheet =>
            FindResolvedStyleSheetFromType(StyleSheetType.ExportWindow)
            ?? ResolveStyleSheets(StyleSheetType.ExportWindow,
            StyleSheetPath.packageManagerVariables,
            StyleSheetPath.exportWindowStyleSheet);

        public StyleSheet activeTrustWindowStyleSheet =>
            FindResolvedStyleSheetFromType(StyleSheetType.ActiveTrustWindow)
            ?? ResolveStyleSheets(StyleSheetType.ActiveTrustWindow,
                StyleSheetPath.packageManagerVariables,
                StyleSheetPath.activeTrustWindowStyleSheet);

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
            var styleSheets = styleSheetPaths.SelectAsEnumerable(p =>
                Load<StyleSheet>(p) ?? throw new ResourceLoaderException($"Unable to load styleSheet {p}"));
            return ResolveStyleSheets(styleSheetType, styleSheets);
        }

        private StyleSheet ResolveStyleSheets(StyleSheetType styleSheetType, IEnumerable<StyleSheet> styleSheets)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isDefaultStyleSheet = true;

            var resolver = new StyleSheets.StyleSheetResolver();
            foreach (var sheet in styleSheets)
                resolver.AddStyleSheets(sheet);
            resolver.ResolveTo(styleSheet);

            styleSheet.name = styleSheetType + lightOrDarkTheme;
            resolvedStyleSheetIds[(int)styleSheetType] = styleSheet.GetEntityId();

            return styleSheet;
        }

        private int m_NestedGetTemplateDepth;

        // The virtual keyword is needed for unit tests
        public virtual VisualElement GetTemplate(string templateFilename, bool shouldThrowException = true)
        {
            m_NestedGetTemplateDepth++;
            var fullTemplatePath = k_TemplateRoot + templateFilename;
            var visualTreeAsset = Load<VisualTreeAsset>(fullTemplatePath);
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

        // The virtual keyword is needed for unit tests
        public virtual T Load<T>(string path) where T : UnityEngine.Object
        {
            return EditorGUIUtility.Load(path) as T;
        }

        // The virtual keyword is needed for unit tests
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
            foreach (var styleSheetId in s_ResolvedDarkStyleSheetIds.Join(s_ResolvedLightStyleSheetIds).Filter(id => id != EntityId.None))
                UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectFromInstanceID(styleSheetId));

            for (var i = 0; i < (int)StyleSheetType.Count; i++)
            {
                s_ResolvedDarkStyleSheetIds[i] = EntityId.None;
                s_ResolvedLightStyleSheetIds[i] = EntityId.None;
            }
        }
    }
}
