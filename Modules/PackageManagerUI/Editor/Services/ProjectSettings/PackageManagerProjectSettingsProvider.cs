// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageManagerProjectSettingsProvider : SettingsProvider
    {
        // The naming of this constant does not follow typical conventions for our codebase.
        // However, it cannot be changed as it is referenced by other test projects.
        public const string k_PackageManagerSettingsPath = "Project/Package Manager";
        public static readonly string[] k_Keywords = new []
        {
            L10n.Tr("enable"),
            L10n.Tr("preview"),
            L10n.Tr("package"),
            L10n.Tr("scoped"),
            L10n.Tr("registries"),
            L10n.Tr("registry"),
            L10n.Tr("dependencies")
        };

        const string k_GeneralServicesTemplatePath = "UXML/PackageManager/PackageManagerProjectSettings.uxml";
        protected VisualTreeAsset m_GeneralTemplate;

        private static readonly string k_Message = L10n.Tr(
            "Pre-release package versions are in the process of becoming stable. The recommended best practice is to use them only for testing purposes and to give direct feedback to the authors.");

        internal static class StylesheetPath
        {
            public static readonly string scopedRegistriesSettings = "StyleSheets/PackageManager/ScopedRegistriesSettings.uss";
            public static readonly string projectSettings = "StyleSheets/PackageManager/PackageManagerProjectSettings.uss";
            public static readonly string stylesheetCommon = "StyleSheets/Extensions/base/common.uss";
            public static readonly string stylesheetDark = "StyleSheets/Extensions/base/dark.uss";
            public static readonly string stylesheetLight = "StyleSheets/Extensions/base/light.uss";
            // We use those stylesheet variables for icons
            public static readonly string styleSheetPackageManagerDark = "StyleSheets/PackageManager/Dark.uss";
            public static readonly string styleSheetPackageManagerLight = "StyleSheets/PackageManager/Light.uss";
        }

        private readonly IProjectSettingsProxy m_SettingsProxy;
        public PackageManagerProjectSettingsProvider(
            IResourceLoader resourceLoader,
            IProjectSettingsProxy settingsProxy,
            IApplicationProxy application)
            : base(k_PackageManagerSettingsPath, SettingsScope.Project, k_Keywords)
        {
            m_SettingsProxy = settingsProxy;
            activateHandler = (_, element) =>
            {
                // Create a child to make sure all the style sheets are not added to the root.
                var scrollView = new ScrollView();
                scrollView.StretchToParentSize();
                scrollView.AddStyleSheetPath(StylesheetPath.scopedRegistriesSettings);
                scrollView.AddStyleSheetPath(StylesheetPath.projectSettings);
                scrollView.AddStyleSheetPath(EditorGUIUtility.isProSkin ? StylesheetPath.stylesheetDark : StylesheetPath.stylesheetLight);
                scrollView.AddStyleSheetPath(EditorGUIUtility.isProSkin ? StylesheetPath.styleSheetPackageManagerDark : StylesheetPath.styleSheetPackageManagerLight);
                scrollView.AddStyleSheetPath(StylesheetPath.stylesheetCommon);
                scrollView.styleSheets.Add(resourceLoader.packageManagerCommonStyleSheet);

                element.Add(scrollView);

                m_GeneralTemplate = EditorGUIUtility.Load(k_GeneralServicesTemplatePath) as VisualTreeAsset;

                VisualElement newVisualElement = new VisualElement();
                m_GeneralTemplate.CloneTree(newVisualElement);
                scrollView.Add(newVisualElement);

                cache = new VisualElementCache(scrollView);

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

                preReleaseInfoBox.readMoreUrl = $"https://docs.unity3d.com/{application.shortUnityVersion}/Documentation/Manual/pack-preview.html";

                RefreshEnablePreReleasePackagesCheckbox();
                enablePreReleasePackages.RegisterValueChangedCallback(changeEvent =>
                {
                    var newValue = changeEvent.newValue;

                    if (newValue != m_SettingsProxy.enablePreReleasePackages)
                    {
                        var saveIt = true;
                        if (newValue && !m_SettingsProxy.oneTimeWarningShown)
                        {
                            if (application.DisplayDialog("showPreReleasePackages", L10n.Tr("Show Pre-release Package Versions"), k_Message, L10n.Tr("I understand"), L10n.Tr("Cancel")))
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
                    RefreshEnablePreReleasePackagesCheckbox();
                });

                UIUtils.SetElementDisplay(seeAllPackageVersions, Unsupported.IsDeveloperBuild());
                UIUtils.SetElementDisplay(seeAllVersionsInfoBox, Unsupported.IsDeveloperBuild());
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
                    RefreshEnablePreReleasePackagesCheckbox();
                });
            };
        }

        private void RefreshEnablePreReleasePackagesCheckbox()
        {
            // When `seeAllPackageVersions` is set to true, PreRelease package versions will be shown whether `enablePreReleasePackages`
            // is set to true or not. We want the UI to reflect this implicit relationship between the two settings by checking
            // `enablePreReleasePackages` in the UI when `seeAllPackageVersions` is set to true, and not allowing users to uncheck
            // `enablePreReleasePackages` until they unchecked `seeAllPackageVersions`
            enablePreReleasePackages.SetValueWithoutNotify(m_SettingsProxy.seeAllPackageVersions || m_SettingsProxy.enablePreReleasePackages);
            enablePreReleasePackages.SetEnabled(!m_SettingsProxy.seeAllPackageVersions || !Unsupported.IsDeveloperBuild());
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectSettingsProvider()
        {
            var container = ServicesContainer.instance;
            return new PackageManagerProjectSettingsProvider(
                container.Resolve<IResourceLoader>(),
                container.Resolve<IProjectSettingsProxy>(),
                container.Resolve<IApplicationProxy>());
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

        private ExtendedHelpBox preReleaseInfoBox => cache.Get<ExtendedHelpBox>("preReleaseInfoBox");
        private Toggle enablePreReleasePackages => cache.Get<Toggle>("enablePreReleasePackages");
        private Foldout advancedSettingsFoldout => cache.Get<Foldout>("advancedSettingsFoldout");
        private Foldout scopedRegistriesSettingsFoldout => cache.Get<Foldout>("scopedRegistriesSettingsFoldout");
        private Toggle seeAllPackageVersions => cache.Get<Toggle>("seeAllVersions");
        private HelpBox seeAllVersionsInfoBox => cache.Get<HelpBox>("seeAllVersionsInfoBox");
    }
}
