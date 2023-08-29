// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerProjectSettingsProvider : SettingsProvider
    {
        internal VisualElement rootVisualElement { get; private set; }

        internal const string k_PackageManagerSettingsPath = "Project/Package Manager";
        const string k_GeneralServicesTemplatePath = "UXML/PackageManager/PackageManagerProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        private static readonly string k_Message = L10n.Tr(
            "Pre-release packages are in the process of becoming stable and will be available as production-ready by the end of this LTS release. \n" +
            "We recommend using these only for testing purposes and to give us direct feedback until then.");

        private IResourceLoader m_ResourceLoader;
        private IProjectSettingsProxy m_SettingsProxy;
        private IApplicationProxy m_Application;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
            m_Application = container.Resolve<IApplicationProxy>();
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

                preReleaseInfoBox.Q<Button>().clickable.clicked += () =>
                {
                    m_Application.OpenURL($"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-preview.html");
                };

                enablePreReleasePackages.SetValueWithoutNotify(m_SettingsProxy.enablePreReleasePackages);
                enablePreReleasePackages.RegisterValueChangedCallback(changeEvent =>
                {
                    var newValue = changeEvent.newValue;

                    if (newValue != m_SettingsProxy.enablePreReleasePackages)
                    {
                        var saveIt = true;
                        if (newValue && !m_SettingsProxy.oneTimeWarningShown)
                        {
                            if (m_Application.DisplayDialog("showPreReleasePackages", L10n.Tr("Show pre-release packages"), k_Message, L10n.Tr("I understand"), L10n.Tr("Cancel")))
                                m_SettingsProxy.oneTimeWarningShown = true;
                            else
                                saveIt = false;
                        }

                        if (saveIt)
                        {
                            m_SettingsProxy.enablePreReleasePackages = newValue;
                            m_SettingsProxy.Save();
                            PackageManagerWindowAnalytics.SendEvent("togglePreReleasePackages");
                        }
                    }
                    enablePreReleasePackages.SetValueWithoutNotify(m_SettingsProxy.enablePreReleasePackages);
                });

                UIUtils.SetElementDisplay(seeAllPackageVersions, Unsupported.IsDeveloperBuild());
                seeAllPackageVersions.SetValueWithoutNotify(m_SettingsProxy.seeAllPackageVersions);

                seeAllPackageVersions.RegisterValueChangedCallback(changeEvent =>
                {
                    seeAllPackageVersions.SetValueWithoutNotify(changeEvent.newValue);
                    var newValue = changeEvent.newValue;

                    if (newValue != m_SettingsProxy.seeAllPackageVersions)
                    {
                        m_SettingsProxy.seeAllPackageVersions = newValue;
                        m_SettingsProxy.Save();
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

        private HelpBox preReleaseInfoBox { get { return cache.Get<HelpBox>("preReleaseInfoBox"); } }
        private Toggle enablePreReleasePackages { get { return rootVisualElement.Q<Toggle>("enablePreReleasePackages"); } }
        private Foldout advancedSettingsFoldout { get { return rootVisualElement.Q<Foldout>("advancedSettingsFoldout"); } }
        private Foldout scopedRegistriesSettingsFoldout { get { return rootVisualElement.Q<Foldout>("scopedRegistriesSettingsFoldout"); } }
        private Toggle seeAllPackageVersions { get { return rootVisualElement.Q<Toggle>("seeAllVersions"); } }
    }
}
