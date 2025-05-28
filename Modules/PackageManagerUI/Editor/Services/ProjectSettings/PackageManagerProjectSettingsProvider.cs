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
            "Pre-release package versions are in the process of becoming stable. The recommended best practice is to use them only for testing purposes and to give direct feedback to the authors.");

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
            // We use those stylesheet variables for icons
            internal static readonly string styleSheetPackageManagerDark = "StyleSheets/PackageManager/Dark.uss";
            internal static readonly string styleSheetPackageManagerLight = "StyleSheets/PackageManager/Light.uss";
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
                rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? StylesheetPath.styleSheetPackageManagerDark : StylesheetPath.styleSheetPackageManagerLight);
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

                preReleaseInfoBox.readMoreUrl = $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/pack-preview.html";

                RefreshEnablePreReleasePackagesCheckbox();
                enablePreReleasePackages.RegisterValueChangedCallback(changeEvent =>
                {
                    var newValue = changeEvent.newValue;

                    if (newValue != m_SettingsProxy.enablePreReleasePackages)
                    {
                        var saveIt = true;
                        if (newValue && !m_SettingsProxy.oneTimeWarningShown)
                        {
                            if (m_Application.DisplayDialog("showPreReleasePackages", L10n.Tr("Show Pre-release Package Versions"), k_Message, L10n.Tr("I understand"), L10n.Tr("Cancel")))
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

        private HelpBoxWithOptionalReadMore preReleaseInfoBox => cache.Get<HelpBoxWithOptionalReadMore>("preReleaseInfoBox");
        private Toggle enablePreReleasePackages => rootVisualElement.Q<Toggle>("enablePreReleasePackages");
        private Foldout advancedSettingsFoldout => rootVisualElement.Q<Foldout>("advancedSettingsFoldout");
        private Foldout scopedRegistriesSettingsFoldout => rootVisualElement.Q<Foldout>("scopedRegistriesSettingsFoldout");
        private Toggle seeAllPackageVersions => rootVisualElement.Q<Toggle>("seeAllVersions");
        private HelpBox seeAllVersionsInfoBox => rootVisualElement.Q<HelpBox>("seeAllVersionsInfoBox");
    }
}
