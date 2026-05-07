// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IDelayedSelectionHandler : IService
    {
        void SelectPackage(string packageToSelect, string pageId = null);

        void SelectPage(string pageId, string searchText = null);
        void SelectSamplePageWithPackageFilters(IReadOnlyList<string> packagesToSelect);
    }

    [Serializable]
    internal class DelayedSelectionHandler : BaseService<IDelayedSelectionHandler>, IDelayedSelectionHandler
    {
        [SerializeField]
        private string m_PackageToSelectAfterRefresh;

        [SerializeField]
        private string[] m_PackagesToSelectOnSamplePageAfterRefresh;

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
            var packageToSelect = m_PackageToSelectAfterRefresh;
            var pageId = m_PageIdToSelectAfterRefresh;
            var packagesToSelectOnSamplesPage = m_PackagesToSelectOnSamplePageAfterRefresh;

            ClearDelayedSelectionAfterRefresh();

            if (!string.IsNullOrEmpty(packageToSelect))
                SelectPackage(packageToSelect, pageId);
            else if (packagesToSelectOnSamplesPage?.Length > 0)
                SelectSamplePageWithPackageFilters(packagesToSelectOnSamplesPage);
        }

        private void ClearDelayedSelectionAfterRefresh()
        {
            m_PackageToSelectAfterRefresh = string.Empty;
            m_PageIdToSelectAfterRefresh = string.Empty;
            m_PackagesToSelectOnSamplePageAfterRefresh = Array.Empty<string>();
        }

        public void SelectPackage(string packageToSelect, string pageId = null)
        {
            if (string.IsNullOrEmpty(packageToSelect))
                return;

            ClearDelayedSelectionAfterRefresh();

            var package = m_PackageDatabase.GetPackageByIdOrName(packageToSelect) ?? m_PackageDatabase.GetPackageByDisplayName(packageToSelect);
            var page = package != null ? m_PageManager.FindPage(package, pageId) : m_PageManager.GetPage(pageId);
            if (page is { id: MyAssetsPage.k_Id })
            {
                m_PageManager.activePage = page;
                page.Load(package?.uniqueId ?? packageToSelect);
                return;
            }

            if (package != null && page != null)
            {
                m_PageManager.activePage = page;
                page.SetNewSelection(package.uniqueId, false);
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
            if (!m_PageRefreshHandler.IsInitialFetchingDone(page))
                m_PageRefreshHandler.Refresh(page);

            if (m_PageRefreshHandler.IsRefreshInProgress(RefreshOptions.UpmSearch | RefreshOptions.UpmSearchOffline | RefreshOptions.UpmList | RefreshOptions.UpmListOffline))
            {
                m_PackageToSelectAfterRefresh = packageToSelect;
                m_PageIdToSelectAfterRefresh = pageId ?? string.Empty;
            }
            else
            {
                Debug.Log(string.Format(L10n.Tr("Unable to find the package {0} in the Package Manager Window."), packageToSelect));
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
            ClearDelayedSelectionAfterRefresh();
        }

        public void SelectSamplePageWithPackageFilters(IReadOnlyList<string> packagesToSelect)
        {
            var page = m_PageManager.GetPage(SamplesPage.k_Id);
            if (page == null)
                return;
            m_PageManager.activePage = page;
            ClearDelayedSelectionAfterRefresh();

            if (packagesToSelect == null || packagesToSelect.Count == 0)
                return;

            if (!m_PageRefreshHandler.IsInitialFetchingDone(page))
                m_PageRefreshHandler.Refresh(page);

            if (m_PageRefreshHandler.IsRefreshInProgress(RefreshOptions.UpmList | RefreshOptions.ImportedSamples))
                packagesToSelect.ToArray(ref m_PackagesToSelectOnSamplePageAfterRefresh);
            else
            {
                var newPageFilters = new PageFilters(page.filters);
                var validPackageUniqueIds = new List<string>();
                foreach (var packageIdOrName in packagesToSelect)
                {
                    var packageUniqueId = m_PackageDatabase.GetPackageByIdOrName(packageIdOrName)?.uniqueId ?? packageIdOrName;
                    // We don't check if the packageUniqueId is part of the supported filters because sometimes `SelectSamplePage` is called externally before the samples are generated.
                    // And adding more invalid package uniqueId filters does not affect the filtering behaviours because if no samples will match those package unique ids anyway.
                    // The next time pageFilters.supportedPackageUniqueIds change, these invalid packageUniqueId filters will be removed.
                    if (!string.IsNullOrEmpty(packageUniqueId))
                        validPackageUniqueIds.Add(packageUniqueId);
                }
                newPageFilters.UpdatePackages(validPackageUniqueIds);
                page.UpdateFilters(newPageFilters);
            }
        }
    }
}
