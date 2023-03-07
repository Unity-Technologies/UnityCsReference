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

        private readonly ResourceLoader m_ResourceLoader;
        private readonly ApplicationProxy m_ApplicationProxy;
        private readonly PackageManagerPrefs m_PackageManagerPrefs;
        private readonly PackageDatabase m_PackageDatabase;
        private readonly PackageOperationDispatcher m_OperationDispatcher;
        private readonly PageManager m_PageManager;
        private readonly PackageManagerProjectSettingsProxy m_SettingsProxy;
        private readonly UpmCache m_UpmCache;
        private readonly IOProxy m_IOProxy;

        private readonly VisualElement m_Container;
        private readonly VisualElement m_VersionHistoryList;
        private readonly VisualElement m_VersionsToolbar;
        private readonly Button m_VersionsShowOthersButton;

        private IPackageVersion m_Version;

        private readonly ButtonDisableCondition m_DisableIfCompiling;
        private readonly ButtonDisableCondition m_DisableIfInstallOrUninstallInProgress;

        public PackageDetailsVersionsTab(ResourceLoader resourceLoader,
            ApplicationProxy applicationProxyProxy,
            PackageManagerPrefs packageManagerPrefs,
            PackageDatabase packageDatabase,
            PackageOperationDispatcher operationDispatcher,
            PageManager pageManager,
            PackageManagerProjectSettingsProxy settingsProxy,
            UpmCache upmCache,
            IOProxy ioProxy)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Version History");
            m_ResourceLoader = resourceLoader;
            m_ApplicationProxy = applicationProxyProxy;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PageManager = pageManager;
            m_SettingsProxy = settingsProxy;
            m_UpmCache = upmCache;
            m_IOProxy = ioProxy;

            m_DisableIfCompiling = new ButtonDisableCondition(() => m_ApplicationProxy.isCompiling,
                L10n.Tr("You need to wait until the compilation is finished to perform this action."));
            m_DisableIfInstallOrUninstallInProgress = new ButtonDisableCondition(() => m_OperationDispatcher.isInstallOrUninstallInProgress,
                L10n.Tr("You need to wait until other install or uninstall operations are finished to perform this action."));

            m_Container = new VisualElement { name = "versionsTab" };
            Add(m_Container);

            m_VersionHistoryList = new VisualElement { name = "versionsList" };
            m_Container.Add(m_VersionHistoryList);

            m_VersionsToolbar = new VisualElement { name = "versionsToolbar" };
            m_Container.Add(m_VersionsToolbar);

            m_VersionsShowOthersButton = new Button { name = "versionsShowAllButton", text = L10n.Tr("See other versions") };
            m_VersionsToolbar.Add(m_VersionsShowOthersButton);

            m_VersionsShowOthersButton.clickable.clicked += ShowOthersVersion;
        }

        private void ShowOthersVersion()
        {
            if (m_Version?.package == null)
                return;

            m_UpmCache.SetLoadAllVersions(m_Version.package.uniqueId, true);
            PackageManagerWindowAnalytics.SendEvent("seeAllVersions", m_Version.package.uniqueId);

            Refresh(m_Version);
        }

        public override bool IsValid(IPackageVersion version)
        {
            return version != null && version.HasTag(PackageTag.UpmFormat) && !version.HasTag(PackageTag.Feature | PackageTag.BuiltIn | PackageTag.Placeholder);
        }

        public override void Refresh(IPackageVersion version)
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

            var seeVersionsToolbar = versions.numUnloadedVersions > 0 && (m_SettingsProxy.seeAllPackageVersions || m_Version.availableRegistry == RegistryType.MyRegistries || m_Version.package.versions.installed?.HasTag(PackageTag.Experimental) == true);
            UIUtils.SetElementDisplay(m_VersionsToolbar, seeVersionsToolbar);

            var latestVersion = m_Version.package?.versions.latest;
            var primaryVersion = m_Version.package?.versions.primary;
            var multipleVersionsVisible = versions.Skip(1).Any();

            foreach (var v in versions.Reverse())
            {
                PackageToolBarRegularButton button;
                if (primaryVersion?.isInstalled ?? false)
                {
                    if (v == primaryVersion)
                    {
                        button = new PackageRemoveButton(m_ApplicationProxy, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager);
                        button.SetGlobalDisableConditions(m_DisableIfCompiling, m_DisableIfInstallOrUninstallInProgress);
                    }
                    else
                    {
                        button = new PackageUpdateButton(m_ApplicationProxy, m_PackageDatabase, m_OperationDispatcher, m_PageManager, false);
                        button.SetGlobalDisableConditions(m_DisableIfCompiling, m_DisableIfInstallOrUninstallInProgress);
                    }
                }
                else
                {
                    button = new PackageAddButton(m_ApplicationProxy, m_PackageDatabase, m_OperationDispatcher);
                    button.SetGlobalDisableConditions(m_DisableIfCompiling, m_DisableIfInstallOrUninstallInProgress);
                }

                var isExpanded = m_PackageManagerPrefs.IsVersionHistoryItemExpanded(v.uniqueId);
                var isLatest = v == latestVersion;
                var versionHistoryItem = new PackageDetailsVersionHistoryItem(m_ResourceLoader,
                    m_PackageDatabase,
                    m_OperationDispatcher,
                    m_UpmCache,
                    m_ApplicationProxy,
                    m_IOProxy,
                    v,
                    multipleVersionsVisible,
                    isLatest,
                    isExpanded,
                    button);
                versionHistoryItem.onToggleChanged += expanded => m_PackageManagerPrefs.SetVersionHistoryItemExpanded(versionHistoryItem.version?.uniqueId, expanded);

                m_VersionHistoryList.Add(versionHistoryItem);
            }

            UIUtils.SetElementDisplay(m_Container, true);
        }
    }
}
