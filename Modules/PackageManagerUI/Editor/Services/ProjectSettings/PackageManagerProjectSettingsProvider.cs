// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerProjectSettingsProvider : SettingsProvider
    {
        protected VisualElement rootVisualElement { get; private set; }

        internal const string k_PackageManagerSettingsPath = "Project/Package Manager";
        const string k_GeneralServicesTemplatePath = "UXML/PackageManager/PackageManagerProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        private PackageManagerProjectSettings m_Settings;

        internal static class StylesheetPath
        {
            internal static readonly string projectSettings = "StyleSheets/PackageManager/PackageManagerProjectSettings.uss";
            internal static readonly string stylesheetDark = "StyleSheets/Extensions/base/dark.uss";
            internal static readonly string stylesheetLight = "StyleSheets/Extensions/base/light.uss";
        }

        private static StyleSheet LoadAndResolveStyleSheet(bool isDarkTheme)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var projectSettingsCommon = EditorGUIUtility.Load(StylesheetPath.projectSettings) as StyleSheet;
            var baseSheet = EditorGUIUtility.Load(isDarkTheme ? StylesheetPath.stylesheetDark : StylesheetPath.stylesheetLight) as StyleSheet;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(baseSheet, projectSettingsCommon);
            resolver.ResolveTo(styleSheet);

            return styleSheet;
        }

        public PackageManagerProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            activateHandler = (s, element) =>
            {
                // Create a child to make sure all the style sheets are not added to the root.
                rootVisualElement = new ScrollView();
                rootVisualElement.StretchToParentSize();
                rootVisualElement.styleSheets.Add(LoadAndResolveStyleSheet(EditorGUIUtility.isProSkin));

                element.Add(rootVisualElement);

                m_GeneralTemplate = EditorGUIUtility.Load(k_GeneralServicesTemplatePath) as VisualTreeAsset;

                VisualElement newVisualElement = new VisualElement();
                m_GeneralTemplate.CloneTree(newVisualElement);
                rootVisualElement.Add(newVisualElement);

                cache = new VisualElementCache(rootVisualElement);

                m_Settings = PackageManagerProjectSettings.instance;

                scopedRegistriesSettingsFoldout.SetValueWithoutNotify(m_Settings.scopedRegistriesSettingsExpanded);
                m_Settings.onScopedRegistriesSettingsFoldoutChanged += OnScopedRegistriesSettingsFoldoutChanged;
                scopedRegistriesSettingsFoldout.RegisterValueChangedCallback(changeEvent =>
                {
                    if (changeEvent.target == scopedRegistriesSettingsFoldout)
                        m_Settings.scopedRegistriesSettingsExpanded = changeEvent.newValue;
                });
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            return new PackageManagerProjectSettingsProvider(k_PackageManagerSettingsPath, SettingsScope.Project, new List<string>()
            {
                L10n.Tr("scoped"),
                L10n.Tr("registries"),
                L10n.Tr("registry")
            });
        }

        private void OnScopedRegistriesSettingsFoldoutChanged(bool expanded)
        {
            if (scopedRegistriesSettingsFoldout.value != expanded)
                scopedRegistriesSettingsFoldout.value = expanded;
        }

        private VisualElementCache cache { get; set; }

        private Foldout scopedRegistriesSettingsFoldout { get { return rootVisualElement.Q<Foldout>("scopedRegistriesSettingsFoldout"); } }
    }
}
