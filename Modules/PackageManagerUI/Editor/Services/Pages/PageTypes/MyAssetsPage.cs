// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class MyAssetsPage : BasePage, IPage<IPackage>
    {
        public static readonly PageSortOption[] k_SupportedSortOptions = { PageSortOption.PurchasedDateDesc, PageSortOption.UpdateDateDesc, PageSortOption.NameAsc, PageSortOption.NameDesc };
        public static readonly PageFilterStatus[] k_SupportedStatusFilters =
        {
            PageFilterStatus.Downloaded,
            PageFilterStatus.Imported,
            PageFilterStatus.UpdateAvailable,
            PageFilterStatus.Unlabeled,
            PageFilterStatus.Hidden,
            PageFilterStatus.Deprecated
        };

        public static readonly string[] k_SupportedCategories =
        {
            "3D",
            "Add-Ons",
            "2D",
            "Audio",
            "Essentials",
            "Templates",
            "Tools",
            "VFX",
            "Decentralization"
        };

        public const string k_Id = "MyAssets";

        public override string id => k_Id;
        public override string displayName => L10n.Tr("My Assets");
        public override Icon icon => Icon.MyAssetsPage;

        public override RefreshOptions refreshOptions => RefreshOptions.Purchased | RefreshOptions.ImportedAssets | RefreshOptions.LocalInfo;
        public override PageCapability capability => PageCapability.RequireNetwork | PageCapability.RequireUserLoggedIn;

        [SerializeField]
        private PaginatedVisualStateList m_VisualStateList = new();
        public override IVisualStateList visualStates => m_VisualStateList;

        [NonSerialized]
        private IPackageDatabase m_PackageDatabase;
        [NonSerialized]
        private IUnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private IAssetStoreClient m_AssetStoreClient;
        [NonSerialized]
        private IAssetStoreRestAPI m_AssetStoreRestAPI;
        [NonSerialized]
        private IPackageManagerPrefs m_PackageManagerPrefs;
        [ExcludeFromCodeCoverage]
        public void ResolveDependencies(IPackageDatabase packageDatabase,
                                        IPackageManagerPrefs packageManagerPrefs,
                                        IUnityConnectProxy unityConnect,
                                        IAssetStoreClient assetStoreClient,
                                        IAssetStoreRestAPI assetStoreRestAPI)
        {
            m_PackageDatabase = packageDatabase;
            m_UnityConnect = unityConnect;
            m_AssetStoreClient = assetStoreClient;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreRestAPI = assetStoreRestAPI;
        }

        public MyAssetsPage(IPackageDatabase packageDatabase,
                            IPackageManagerPrefs packageManagerPrefs,
                            IUnityConnectProxy unityConnect,
                            IAssetStoreClient assetStoreClient,
                            IAssetStoreRestAPI assetStoreRestAPI)
        {
            ResolveDependencies(packageDatabase, packageManagerPrefs, unityConnect, assetStoreClient, assetStoreRestAPI);

            UpdateSupportedSortOptions(k_SupportedSortOptions, false);
            UpdateSupportedStatuses(k_SupportedStatusFilters, false);
            UpdateSupportedCategories(k_SupportedCategories, false);
        }

        public override void OnEnable()
        {
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;

            m_AssetStoreClient.onProductListFetched += OnProductListFetched;
            m_AssetStoreClient.onProductExtraFetched += OnProductExtraFetched;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public override void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;

            m_AssetStoreClient.onProductListFetched -= OnProductListFetched;
            m_AssetStoreClient.onProductExtraFetched -= OnProductExtraFetched;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        public bool ShouldInclude(IPackage package)
        {
            return package?.product != null;
        }

        protected override void UpdateFiltersInternal(PageFilters newFilters, PageFilters.ChangedTypes changedTypes, bool triggerEvent = true)
        {
            base.UpdateFiltersInternal(newFilters, changedTypes, triggerEvent);
            if (!changedTypes.AnyFilterValuesChanged())
                return;
            ListPurchases();
            ClearAllAndTriggerRebuildEvent();
        }

        public override void UpdateSupportedFiltersAsync()
        {
            m_AssetStoreRestAPI.ListLabels(
                labels => UpdateSupportedLabels(labels, true),
                error => Debug.LogWarning(string.Format(L10n.Tr("[Package Manager Window] Error while fetching labels: {0}"), error.message)));
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

        protected void OnPackagesChanged(PackagesChangeArgs args)
        {
            // We don't need to worry about packages change when the page is not active, because when an inactive page
            // becomes active, it will rebuild its visual states from scratch anyway.
            if (!isActive)
                return;

            // Since MyAssets page's list is not affected by package change, we only check if any items has been updated.
            var updateList = new List<string>(args.added.Join(args.updated, args.removed).SelectAsEnumerable(i => i.uniqueId).Filter(i => visualStates.Contains(i)));
            if (updateList.Count > 0)
                TriggerOnListUpdate(updated: updateList);
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

        public override void Load(string itemUniqueId)
        {
            if (string.IsNullOrEmpty(itemUniqueId) || !long.TryParse(itemUniqueId, out var productId))
                return;

            var package = m_PackageDatabase.GetPackage(itemUniqueId);
            if (package == null || package.versions.primary.HasTag(PackageTag.Placeholder))
            {
                if (m_UnityConnect.isUserLoggedIn)
                    m_AssetStoreClient.ExtraFetch(productId);
            }
            else
            {
                if (!visualStates.Contains(package.uniqueId))
                {
                    m_VisualStateList.AddExtraItem(package.uniqueId);
                    TriggerOnListUpdate(added: new[] { package.uniqueId });
                }
                else
                    TriggerOnListUpdate(updated: new[] { package.uniqueId });

                SetNewSelection(new[] { package.uniqueId }, false);
            }
        }

        public override void LoadExtraItems(IEnumerable<string> itemUniqueIds)
        {
            var addedItems = new List<string>(itemUniqueIds.Filter(p => !visualStates.Contains(p)));
            foreach (var item in addedItems)
                m_VisualStateList.AddExtraItem(item);
            TriggerOnListUpdate(added: addedItems);
        }

        public virtual void OnProductExtraFetched(long productId)
        {
            var uniqueId = productId.ToString();
            var isNewItem = !visualStates.Contains(uniqueId);

            if (isNewItem)
                m_VisualStateList.AddExtraItem(productId.ToString());

            if (!isActive)
                return;

            var package = m_PackageDatabase.GetPackage(uniqueId);

            if (package == null)
                return;

            if (isNewItem)
                TriggerOnListUpdate(added: new[] { package.uniqueId });
            else
                TriggerOnListUpdate(updated: new[] { package.uniqueId });
            SetNewSelection(new[] { package.uniqueId }, false);
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

            var oldPackageIds = new HashSet<string>(visualStates.SelectAsEnumerable(v => v.itemUniqueId));
            var newPackageIds = purchases.list.SelectToNewArray(i => i.productId.ToString());
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
                // if the content is neither starting from zero nor next page, we simply discard it
                return;
            }

            // only try to rebuild the list immediately if we are already on the `AssetStore` page.
            // if not we'll just wait for page switch which will trigger the rebuild as well
            if (isActive)
            {
                HashSet<string> removed = null;
                IReadOnlyCollection<string> added = null;
                if (purchases.startIndex == 0)
                {
                    removed = oldPackageIds;
                    var addedList = new List<string>();
                    foreach (var packageId in newPackageIds)
                    {
                        if (removed.Contains(packageId))
                            removed.Remove(packageId);
                        else
                            addedList.Add(packageId);
                    }
                    added = addedList;
                }
                else if (purchases.startIndex == oldPackageIds.Count)
                {
                    added = newPackageIds;
                }

                TriggerOnListUpdate(added: added, removed: removed);
            }
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            searchText = string.Empty;
            ClearFilters(true);
            if (!loggedIn)
            {
                // When users log out, even when we are not on `My Assets` page we should still clear the Asset Store page properly
                ClearAllAndTriggerRebuildEvent();
            }
        }
    }
}
