// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsVersionsTab : PackageDetailsTabElement
    {
        public const string k_Id = "versions";

        private readonly VisualElement m_Container;
        private readonly VisualElement m_VersionHistoryList;
        private readonly VisualElement m_VersionsToolbar;
        private readonly Button m_VersionsShowOthersButton;
        private readonly Label m_LoadingLabel;

        private IPackageVersion m_Version;

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IApplicationProxy m_ApplicationProxy;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPageManager m_PageManager;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IUpmCache m_UpmCache;
        private readonly IPackageLinkFactory m_PackageLinkFactory;
        public PackageDetailsVersionsTab(IUnityConnectProxy unityConnect,
            IResourceLoader resourceLoader,
            IApplicationProxy applicationProxy,
            IPackageManagerPrefs packageManagerPrefs,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IPageManager pageManager,
            IProjectSettingsProxy settingsProxy,
            IUpmCache upmCache,
            IPackageLinkFactory packageLinkFactory) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Version History");
            m_ResourceLoader = resourceLoader;
            m_ApplicationProxy = applicationProxy;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_UpmCache = upmCache;
            m_PackageLinkFactory = packageLinkFactory;

            m_Container = new VisualElement { name = "versionsTab" };
            m_ContentContainer.Add(m_Container);

            m_VersionHistoryList = new VisualElement { name = "versionsList" };
            m_Container.Add(m_VersionHistoryList);

            m_VersionsToolbar = new VisualElement { name = "versionsToolbar" };
            m_Container.Add(m_VersionsToolbar);

            m_VersionsShowOthersButton = new Button { name = "versionsShowAllButton", text = L10n.Tr("See other versions") };
            m_VersionsToolbar.Add(m_VersionsShowOthersButton);

            m_LoadingLabel = new Label { name = "versionsLoadingLabel", text = L10n.Tr("Loading...") };
            m_Container.Add(m_LoadingLabel);

            m_VersionsShowOthersButton.clickable.clicked += ShowOthersVersion;
        }

        private void ShowOthersVersion()
        {
            if (m_Version?.package == null)
                return;

            UIUtils.SetElementDisplay(m_LoadingLabel, true);
            UIUtils.SetElementDisplay(m_VersionsToolbar, false);

            EditorApplication.delayCall += () =>
            {
                m_UpmCache.SetLoadAllVersions(m_Version.package.uniqueId, true);
                PackageManagerWindowAnalytics.SendEvent("seeAllVersions", m_Version.package.uniqueId);

                Refresh(m_Version);
            };
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.HasTag(PackageTag.UpmFormat) && !version.HasTag(PackageTag.Feature | PackageTag.BuiltIn | PackageTag.Placeholder);
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            m_Version = version;
            if (m_Version?.uniqueId != m_PackageManagerPrefs.packageDisplayedInVersionHistoryTab)
            {
                m_PackageManagerPrefs.ClearExpandedVersionHistoryItems();
                m_PackageManagerPrefs.packageDisplayedInVersionHistoryTab = m_Version?.uniqueId;
            }

            foreach (var historyItem in m_VersionHistoryList.Children().OfType<PackageDetailsVersionHistoryItem>())
                historyItem.StopSpinner();
            m_VersionHistoryList.Clear();

            var versions = m_Version?.package?.versions;
            if (versions == null)
            {
                UIUtils.SetElementDisplay(m_Container, false);
                return;
            }

            var seeVersionsToolbar = versions.numUnloadedVersions > 0 && (m_SettingsProxy.seeAllPackageVersions || m_Version.availableRegistry != RegistryType.UnityRegistry || m_Version.package.versions.installed?.HasTag(PackageTag.Experimental) == true);
            UIUtils.SetElementDisplay(m_VersionsToolbar, seeVersionsToolbar);
            UIUtils.SetElementDisplay(m_LoadingLabel, false);

            var primaryVersion = m_Version.package?.versions.primary;

            foreach (var v in versions.Reverse())
            {
                PackageAction action;
                if (primaryVersion?.isInstalled ?? false)
                {
                    if (v == primaryVersion)
                        action = new RemoveAction(m_OperationDispatcher, m_ApplicationProxy, m_PackageManagerPrefs, m_PackageDatabase, m_PageManager);
                    else
                        action = new UpdateAction(m_OperationDispatcher, m_ApplicationProxy, m_PackageDatabase, m_PageManager, false);
                }
                else
                    action = new AddAction(m_OperationDispatcher, m_ApplicationProxy, m_PackageDatabase);

                var isExpanded = m_PackageManagerPrefs.IsVersionHistoryItemExpanded(v.uniqueId);
                var versionHistoryItem = new PackageDetailsVersionHistoryItem(m_ResourceLoader,
                    m_PackageDatabase,
                    m_OperationDispatcher,
                    m_UpmCache,
                    m_ApplicationProxy,
                    m_PackageLinkFactory,
                    v,
                    isExpanded,
                    action);
                versionHistoryItem.onToggleChanged += expanded => m_PackageManagerPrefs.SetVersionHistoryItemExpanded(versionHistoryItem.version?.uniqueId, expanded);

                m_VersionHistoryList.Add(versionHistoryItem);
            }

            UIUtils.SetElementDisplay(m_Container, true);
        }
    }
}
