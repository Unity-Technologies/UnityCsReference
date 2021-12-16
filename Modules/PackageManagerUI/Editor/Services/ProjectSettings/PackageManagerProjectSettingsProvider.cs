// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageManagerProjectSettingsProvider : SettingsProvider
    {
        protected VisualElement rootVisualElement { get; private set; }

        internal const string k_PackageManagerSettingsPath = "Project/Package Manager";
        const string k_GeneralServicesTemplatePath = "UXML/PackageManager/PackageManagerProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        private static readonly string k_Message =
            "Preview packages are in the early stage of development and not yet ready for production.\n" +
            "We recommend using these only for testing purpose and to give us direct feedback.";

        private ResourceLoader m_ResourceLoader;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private ApplicationProxy m_ApplicationProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_ApplicationProxy = container.Resolve<ApplicationProxy>();
        }

        internal static class StylesheetPath
        {
            internal static readonly string scopedRegistriesSettings = "StyleSheets/PackageManager/ScopedRegistriesSettings.uss";
            internal static readonly string projectSettings = "StyleSheets/PackageManager/PackageManagerProjectSettings.uss";
            internal static readonly string stylesheetCommon = "StyleSheets/Extensions/base/common.uss";
            internal static readonly string stylesheetDark = "StyleSheets/Extensions/base/dark.uss";
            internal static readonly string stylesheetLight = "StyleSheets/Extensions/base/light.uss";
        }

        public PackageManagerProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            activateHandler = (s, element) =>
            {
                ResolveDependencies();

                // Create a child to make sure all the style sheets are not added to the root.
                rootVisualElement = new ScrollView();
                rootVisualElement.StretchToParentSize();
                rootVisualElement.AddStyleSheetPath(StylesheetPath.scopedRegistriesSettings);
                rootVisualElement.AddStyleSheetPath(StylesheetPath.projectSettings);
                rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? StylesheetPath.stylesheetDark : StylesheetPath.stylesheetLight);
                rootVisualElement.AddStyleSheetPath(StylesheetPath.stylesheetCommon);
                rootVisualElement.styleSheets.Add(m_ResourceLoader.packageManagerCommonStyleSheet);

                element.Add(rootVisualElement);

                m_GeneralTemplate = EditorGUIUtility.Load(k_GeneralServicesTemplatePath) as VisualTreeAsset;

                VisualElement newVisualElement = new VisualElement();
                m_GeneralTemplate.CloneTree(newVisualElement);
                rootVisualElement.Add(newVisualElement);

                cache = new VisualElementCache(rootVisualElement);

                advancedSettingsFoldout.SetValueWithoutNotify(m_SettingsProxy.advancedSettingsExpanded);
                m_SettingsProxy.onAdvancedSettingsFoldoutChanged += OnAdvancedSettingsFoldoutChanged;
                advancedSettingsFoldout.RegisterValueChangedCallback(changeEvent =>
                {
                    if (changeEvent.target == advancedSettingsFoldout)
                        m_SettingsProxy.advancedSettingsExpanded = changeEvent.newValue;
                });

                scopedRegistriesSettingsFoldout.SetValueWithoutNotify(m_SettingsProxy.scopedRegistriesSettingsExpanded);
                m_SettingsProxy.onScopedRegistriesSettingsFoldoutChanged += OnScopedRegistriesSettingsFoldoutChanged;
                scopedRegistriesSettingsFoldout.RegisterValueChangedCallback(changeEvent =>
                {
                    if (changeEvent.target == scopedRegistriesSettingsFoldout)
                        m_SettingsProxy.scopedRegistriesSettingsExpanded = changeEvent.newValue;
                });

                previewInfoBox.Q<Button>().clickable.clicked += () =>
                {
                    m_ApplicationProxy.OpenURL($"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/pack-preview.html");
                };

                enablePreviewPackages.SetValueWithoutNotify(m_SettingsProxy.enablePreviewPackages);
                enablePreviewPackages.RegisterValueChangedCallback(changeEvent =>
                {
                    var newValue = changeEvent.newValue;

                    if (newValue != m_SettingsProxy.enablePreviewPackages)
                    {
                        var saveIt = true;
                        if (newValue && !m_SettingsProxy.oneTimeWarningShown)
                        {
                            if (EditorUtility.DisplayDialog(L10n.Tr("Package Manager"), L10n.Tr(k_Message), L10n.Tr("I understand"), L10n.Tr("Cancel")))
                                m_SettingsProxy.oneTimeWarningShown = true;
                            else
                                saveIt = false;
                        }

                        if (saveIt)
                        {
                            m_SettingsProxy.enablePreviewPackages = newValue;
                            m_SettingsProxy.Save();
                            PackageManagerWindowAnalytics.SendEvent("togglePreviewPackages");
                        }
                    }
                    enablePreviewPackages.SetValueWithoutNotify(m_SettingsProxy.enablePreviewPackages);
                });

                enablePackageDependencies.SetValueWithoutNotify(m_SettingsProxy.enablePackageDependencies);
                enablePackageDependencies.RegisterValueChangedCallback(changeEvent =>
                {
                    enablePackageDependencies.SetValueWithoutNotify(changeEvent.newValue);
                    var newValue = changeEvent.newValue;

                    if (newValue != m_SettingsProxy.enablePackageDependencies)
                    {
                        m_SettingsProxy.enablePackageDependencies = newValue;
                        m_SettingsProxy.Save();
                        PackageManagerWindowAnalytics.SendEvent("toggleDependencies");
                    }
                });
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            return new PackageManagerProjectSettingsProvider(k_PackageManagerSettingsPath, SettingsScope.Project, new List<string>()
            {
                L10n.Tr("enable"),
                L10n.Tr("preview"),
                L10n.Tr("package"),
                L10n.Tr("scoped"),
                L10n.Tr("registries"),
                L10n.Tr("registry"),
                L10n.Tr("dependencies"),
            });
        }

        private void OnAdvancedSettingsFoldoutChanged(bool expanded)
        {
            if (advancedSettingsFoldout.value != expanded)
                advancedSettingsFoldout.value = expanded;
        }

        private void OnScopedRegistriesSettingsFoldoutChanged(bool expanded)
        {
            if (scopedRegistriesSettingsFoldout.value != expanded)
                scopedRegistriesSettingsFoldout.value = expanded;
        }

        private VisualElementCache cache { get; set; }

        private HelpBox previewInfoBox { get { return cache.Get<HelpBox>("previewInfoBox"); } }
        private Toggle enablePreviewPackages { get { return rootVisualElement.Q<Toggle>("enablePreviewPackages"); } }
        private Toggle enablePackageDependencies { get { return rootVisualElement.Q<Toggle>("enableDependencies"); } }
        private Foldout advancedSettingsFoldout { get { return rootVisualElement.Q<Foldout>("advancedSettingsFoldout"); } }
        private Foldout scopedRegistriesSettingsFoldout { get { return rootVisualElement.Q<Foldout>("scopedRegistriesSettingsFoldout"); } }
    }
}
