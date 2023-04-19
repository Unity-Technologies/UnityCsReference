// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class MyAssetsPage : BasePage
    {
        public static readonly PageSortOption[] k_SupportedSortOptions = { PageSortOption.PurchasedDateDesc, PageSortOption.UpdateDateDesc, PageSortOption.NameAsc, PageSortOption.NameDesc };
        public static readonly PageFilters.Status[] k_SupportedStatusFilters =
        {
            PageFilters.Status.Downloaded,
            PageFilters.Status.Imported,
            PageFilters.Status.UpdateAvailable,
            PageFilters.Status.Unlabeled,
            PageFilters.Status.Hidden,
            PageFilters.Status.Deprecated
        };

        public const string k_Id = "MyAssets";

        public override string id => k_Id;
        public override string displayName => L10n.Tr("My Assets");

        public override IEnumerable<PageFilters.Status> supportedStatusFilters => k_SupportedStatusFilters;
        public override IEnumerable<PageSortOption> supportedSortOptions => k_SupportedSortOptions;
        public override RefreshOptions refreshOptions => RefreshOptions.Purchased | RefreshOptions.ImportedAssets;
        public override PageCapability capability => PageCapability.RequireNetwork | PageCapability.RequireUserLoggedIn;

        [SerializeField]
        private PaginatedVisualStateList m_VisualStateList = new();

        public override IVisualStateList visualStates => m_VisualStateList;

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        public void ResolveDependencies(PackageDatabase packageDatabase,
                                        PackageManagerPrefs packageManagerPrefs,
                                        UnityConnectProxy unityConnect,
                                        AssetStoreClientV2 assetStoreClient)
        {
            ResolveDependencies(packageDatabase);
            m_UnityConnect = unityConnect;
            m_AssetStoreClient = assetStoreClient;
            m_PackageManagerPrefs = packageManagerPrefs;
        }

        public MyAssetsPage(PackageDatabase packageDatabase,
                            PackageManagerPrefs packageManagerPrefs,
                            UnityConnectProxy unityConnect,
                            AssetStoreClientV2 assetStoreClient)
            :base(packageDatabase)
        {
            ResolveDependencies(packageDatabase, packageManagerPrefs, unityConnect, assetStoreClient);
        }

        public override bool ShouldInclude(IPackage package)
        {
            return package?.product != null;
        }

        public override bool UpdateFilters(PageFilters newFilters)
        {
            if (!base.UpdateFilters(newFilters))
                return false;

            ListPurchases();
            ClearAllAndTriggerRebuildEvent();
            return true;
        }

        protected override void RefreshListOnSearchTextChange()
        {
            ListPurchases();
            ClearAllAndTriggerRebuildEvent();
        }

        private void ListPurchases()
        {
            var numItems = m_PackageManagerPrefs.numItemsPerPage ?? PackageManagerPrefs.k_DefaultPageSize;
            var queryArgs = new PurchasesQueryArgs(0, numItems, trimmedSearchText, filters);
            m_AssetStoreClient.ListPurchases(queryArgs);
        }

        private void ClearAllAndTriggerRebuildEvent()
        {
            m_VisualStateList.ClearAll();
            TriggerListRebuild();
        }

        public override void OnActivated()
        {
            base.OnActivated();
            TriggerListRebuild();
            UpdateVisualStateVisibilityWithSearchText();
            TriggerOnSelectionChanged();
        }

        public override void LoadMore(long numberOfPackages)
        {
            if (visualStates.countLoaded >= visualStates.countTotal)
                return;

            var startIndex = (int)visualStates.countLoaded;
            var numItems = (int)numberOfPackages;
            var queryArgs = new PurchasesQueryArgs(startIndex, numItems, trimmedSearchText, filters);
            m_AssetStoreClient.ListPurchases(queryArgs);
        }

        public override void Load(string packageUniqueId)
        {
            if (string.IsNullOrEmpty(packageUniqueId) || !long.TryParse(packageUniqueId, out var productId))
                return;

            m_PackageDatabase.GetPackageAndVersionByIdOrName(packageUniqueId, out var package, out var version, true);
            if (package == null || (version ?? package.versions.primary).HasTag(PackageTag.Placeholder))
            {
                if (m_UnityConnect.isUserLoggedIn)
                    m_AssetStoreClient.ExtraFetch(productId);
            }
            else
            {
                if (!m_VisualStateList.Contains(package.uniqueId))
                {
                    m_VisualStateList.AddExtraItem(package.uniqueId);
                    TriggerOnListUpdate(added: new[] { package });
                }
                else
                    TriggerOnListUpdate(updated: new[] { package });

                SetNewSelection(new[] { new PackageAndVersionIdPair(package.uniqueId, version?.uniqueId) });
            }
        }

        public override void LoadExtraItems(IEnumerable<IPackage> packages)
        {
            var addedPackages = packages.Where(p => !m_VisualStateList.Contains(p.uniqueId)).ToArray();
            foreach (var package in addedPackages)
                m_VisualStateList.AddExtraItem(package.uniqueId);
            TriggerOnListUpdate(added: addedPackages);
        }

        public void OnProductExtraFetched(long productId)
        {
            var uniqueId = productId.ToString();
            var isNewItem = !m_VisualStateList.Contains(uniqueId);

            if (isNewItem)
                m_VisualStateList.AddExtraItem(productId.ToString());

            if (!isActivePage)
                return;

            var package = m_PackageDatabase.GetPackage(uniqueId);
            if (isNewItem)
                TriggerOnListUpdate(added: new[] { package });
            else
                TriggerOnListUpdate(updated: new[] { package });
            SetNewSelection(new[] { new PackageAndVersionIdPair(package.uniqueId) });
        }

        public void OnProductListFetched(AssetStorePurchases purchases)
        {
            var isSet = purchases.queryArgs?.isFilterSet == true;
            if (isSet && !filters.Equals(purchases.queryArgs))
                return;

            // if a new page has arrived but the total has changed or the searchText has changed, do a re-fetch
            if (purchases.startIndex > 0 && visualStates.countTotal != purchases.total)
            {
                var startIndex = (int)visualStates.countLoaded;
                var numItems = purchases.startIndex + purchases.list.Count;
                var queryArgs = new PurchasesQueryArgs(startIndex, numItems, trimmedSearchText, filters);
                m_AssetStoreClient.ListPurchases(queryArgs);
                return;
            }

            var oldPackageIds = new HashSet<string>(m_VisualStateList.Select(v => v.packageUniqueId));
            var newPackageIds = purchases.productIds.Select(i => i.ToString()).ToList();
            if (purchases.startIndex == 0)
            {
                // override the result if the new list starts from index 0 (meaning it's a refresh)
                m_VisualStateList.Rebuild(newPackageIds);
                m_VisualStateList.ClearExtraItems();
                m_VisualStateList.SetTotal(purchases.total);
            }
            else if (purchases.startIndex == visualStates.countLoaded)
            {
                // append the result if it is the next page
                m_VisualStateList.AddRange(newPackageIds);
                m_VisualStateList.ClearExtraItems();
            }
            else
            {
                // if the content is neither starting from zero or next page, we simply discard it
                return;
            }

            // only try to rebuild the list immediately if we are already on the `AssetStore` page.
            // if not we'll just wait for page switch which will trigger the rebuild as well
            if (isActivePage)
            {
                HashSet<string> removed = null;
                List<string> added = null;
                if (purchases.startIndex == 0)
                {
                    removed = oldPackageIds;
                    added = new List<string>();
                    foreach (var packageId in newPackageIds)
                    {
                        if (removed.Contains(packageId))
                            removed.Remove(packageId);
                        else
                            added.Add(packageId);
                    }
                }
                else if (purchases.startIndex == oldPackageIds.Count)
                {
                    added = newPackageIds;
                }

                var addedPackages = added?.Select(i => m_PackageDatabase.GetPackage(i)).ToArray();
                var removedPackages = removed?.Select(i => m_PackageDatabase.GetPackage(i)).ToArray();
                TriggerOnListUpdate(added: addedPackages, removed: removedPackages);
            }

            UpdateVisualStateVisibilityWithSearchText();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                // When users log out, even when we are not on `My Assets` page we should still clear the Asset Store page properly
                ClearAllAndTriggerRebuildEvent();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_AssetStoreClient.onProductListFetched += OnProductListFetched;
            m_AssetStoreClient.onProductExtraFetched += OnProductExtraFetched;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_AssetStoreClient.onProductListFetched -= OnProductListFetched;
            m_AssetStoreClient.onProductExtraFetched -= OnProductExtraFetched;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        [ExcludeFromCodeCoverage]
        public override void RebuildAndReorderVisualStates()
        {
            // do nothing because for paginated pages, the order of visual states is pre-determined
        }

        [ExcludeFromCodeCoverage]
        public override void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            // do nothing, only simple page needs implementation right now
        }

        [ExcludeFromCodeCoverage]
        public override void ResetUserUnlockedState()
        {
            // do nothing, only simple page needs implementation right now
        }

        public override bool GetDefaultLockState(IPackage package) => false;
    }
}
