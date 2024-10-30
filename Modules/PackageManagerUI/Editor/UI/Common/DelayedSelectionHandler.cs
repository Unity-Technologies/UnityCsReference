// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IDelayedSelectionHandler : IService
    {
        void SelectPackage(string packageToSelect, string pageId = null);

        void SelectPage(string pageId, string searchText = null);
    }

    [Serializable]
    internal class DelayedSelectionHandler : BaseService<IDelayedSelectionHandler>, IDelayedSelectionHandler
    {
        [SerializeField]
        private string m_PackageToSelectAfterRefresh;

        [SerializeField]
        private string m_PageIdToSelectAfterRefresh;

        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IPageRefreshHandler m_PageRefreshHandler;
        private readonly IUpmCache m_UpmCache;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        public DelayedSelectionHandler(IPackageDatabase packageDatabase, IPageManager pageManager, IPageRefreshHandler pageRefreshHandler, IUpmCache upmCache, IProjectSettingsProxy settingsProxy)
        {
            m_PackageDatabase = RegisterDependency(packageDatabase);
            m_PageManager = RegisterDependency(pageManager);
            m_PageRefreshHandler = RegisterDependency(pageRefreshHandler);
            m_UpmCache = RegisterDependency(upmCache);
            m_SettingsProxy = RegisterDependency(settingsProxy);
        }

        public override void OnEnable()
        {
            m_PageRefreshHandler.onRefreshOperationFinish += OnRefreshOperationFinish;
        }

        public override void OnDisable()
        {
            m_PageRefreshHandler.onRefreshOperationFinish -= OnRefreshOperationFinish;
        }

        private void OnRefreshOperationFinish()
        {
            if (string.IsNullOrEmpty(m_PackageToSelectAfterRefresh))
                return;

            var packageToSelect = m_PackageToSelectAfterRefresh;
            var pageId = m_PageIdToSelectAfterRefresh;

            m_PackageToSelectAfterRefresh = string.Empty;
            m_PageIdToSelectAfterRefresh = string.Empty;

            SelectPackage(packageToSelect, pageId);
        }

        public void SelectPackage(string packageToSelect, string pageId = null)
        {
            if (string.IsNullOrEmpty(packageToSelect))
                return;

            m_PackageDatabase.GetPackageAndVersionByIdOrName(packageToSelect, out var package, out _, true);
            var page = m_PageManager.GetPage(pageId);
            if (package != null && page?.ShouldInclude(package) != true)
                page = m_PageManager.FindPage(package);

            if (page is { id: MyAssetsPage.k_Id })
            {
                m_PageManager.activePage = page;
                page.Load(packageToSelect);
                return;
            }

            if (package != null && page != null && page.ShouldInclude(package))
            {
                m_PageManager.activePage = page;
                page.SetNewSelection(package);
                return;
            }

            if (package == null && !m_SettingsProxy.enablePreReleasePackages && !m_SettingsProxy.seeAllPackageVersions)
            {
                var packageName = packageToSelect.Split('@')[0];
                var packageInfo = m_UpmCache.GetSearchPackageInfo(packageName);
                if (packageInfo != null && SemVersionParser.TryParse(packageInfo.version, out var parsedVersion) &&
                    parsedVersion?.GetExpOrPreOrReleaseTag() == PackageTag.PreRelease)
                {
                    Debug.Log(string.Format(L10n.Tr("You must check \"Show Pre-release Package Versions\" in Project Settings > Package Manager in order to see package {0}."), packageName));
                    return;
                }
            }

            page ??= m_PageManager.activePage;
            if (!m_PageRefreshHandler.IsInitialFetchingDone(page) || m_PackageDatabase.isEmpty)
                m_PageRefreshHandler.Refresh(page);

            if (m_PageRefreshHandler.IsRefreshInProgress(RefreshOptions.UpmSearch | RefreshOptions.UpmSearchOffline | RefreshOptions.UpmList | RefreshOptions.UpmListOffline))
            {
                m_PackageToSelectAfterRefresh = packageToSelect;
                m_PageIdToSelectAfterRefresh = pageId ?? string.Empty;
            }
        }

        // For now, we don't do any delays when we select a page, as this function is internal and only used a few times
        // so we know none of those cases would require delay selection logic. However, in the future if we were to
        // have a public function to support this, we will need to do delay selection for when user select a specific
        // scoped registry page, as the page won't be available until the Refresh is done.
        public void SelectPage(string pageId, string searchText = null)
        {
            var page = m_PageManager.GetPage(pageId);
            if (page == null)
                return;
            page.searchText = searchText ?? string.Empty;
            m_PageManager.activePage = page;
        }
    }
}
