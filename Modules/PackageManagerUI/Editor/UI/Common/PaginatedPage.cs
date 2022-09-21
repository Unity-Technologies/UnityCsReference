// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PaginatedPage : BasePage
    {
        [SerializeField]
        private PaginatedVisualStateList m_VisualStateList = new PaginatedVisualStateList();

        public override IVisualStateList visualStates => m_VisualStateList;

        public override IEnumerable<SubPage> subPages => Enumerable.Empty<SubPage>();

        public override SubPage currentSubPage { get => null; set {} }

        public override string contentType { get; set; }

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        public void ResolveDependencies(PackageDatabase packageDatabase,
                                        PackageManagerPrefs packageManagerPrefs,
                                        UnityConnectProxy unityConnect,
                                        AssetStoreClientV2 assetStoreClient)
        {
            ResolveDependencies(packageDatabase, packageManagerPrefs);
            m_UnityConnect = unityConnect;
            m_AssetStoreClient = assetStoreClient;
        }

        public PaginatedPage(string contentType,
                             PackageDatabase packageDatabase,
                             PackageManagerPrefs packageManagerPrefs,
                             UnityConnectProxy unityConnect,
                             AssetStoreClientV2 assetStoreClient,
                             PackageFilterTab tab,
                             PageCapability capability)
            :base(packageDatabase, packageManagerPrefs, tab, capability)
        {
            this.contentType = contentType;
            ResolveDependencies(packageDatabase, packageManagerPrefs, unityConnect, assetStoreClient);
        }

        public override bool UpdateFilters(PageFilters filters)
        {
            if (!base.UpdateFilters(filters))
                return false;

            ListPurchases();
            ClearListAndTriggerRebuildEvent();
            return true;
        }

        public override void UpdateSearchText(string searchText)
        {
            ListPurchases();
            ClearListAndTriggerRebuildEvent();
        }

        private void ListPurchases()
        {
            var numItems = m_PackageManagerPrefs.numItemsPerPage ?? PackageManagerPrefs.k_DefaultPageSize;
            var queryArgs = new PurchasesQueryArgs(0, numItems, m_PackageManagerPrefs.trimmedSearchText, filters);
            m_AssetStoreClient.ListPurchases(queryArgs);
        }

        private void ClearListAndTriggerRebuildEvent()
        {
            m_VisualStateList.ClearList();
            m_VisualStateList.ClearExtraItems();
            TriggerListRebuild();
        }

        public override void OnActivated()
        {
            TriggerListRebuild();
            UpdateVisualStateVisbilityWithSearchText();
            TriggerOnSelectionChanged();
        }

        public override void OnDeactivated() {}

        public override void LoadMore(long numberOfPackages)
        {
            if (visualStates.countLoaded >= visualStates.countTotal)
                return;

            var startIndex = (int)visualStates.countLoaded;
            var numItems = (int)numberOfPackages;
            var queryArgs = new PurchasesQueryArgs(startIndex, numItems, m_PackageManagerPrefs.trimmedSearchText, filters);
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

            if (m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore)
            {
                var package = m_PackageDatabase.GetPackage(uniqueId);
                if (isNewItem)
                    TriggerOnListUpdate(added: new[] { package });
                else
                    TriggerOnListUpdate(updated: new[] { package });
                SetNewSelection(new[] { new PackageAndVersionIdPair(package.uniqueId) });
            }
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
                var queryArgs = new PurchasesQueryArgs(startIndex, numItems, m_PackageManagerPrefs.trimmedSearchText, filters);
                m_AssetStoreClient.ListPurchases(queryArgs);
                return;
            }

            var oldPackageIds = new HashSet<string>(m_VisualStateList.Select(v => v.packageUniqueId));
            var newPackageIds = purchases.productIds.Select(id => id.ToString()).ToList();
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

            // only try to rebuild the list immediately if we are already on the `AssetStore` tab.
            // if not we'll just wait for tab switch which will trigger the rebuild as well
            if (m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore)
            {
                HashSet<string> removed = null;
                List<string> added = null;
                if (purchases.startIndex == 0)
                {
                    removed = oldPackageIds;
                    added = new List<string>();
                    foreach (var id in newPackageIds)
                    {
                        if (removed.Contains(id))
                            removed.Remove(id);
                        else
                            added.Add(id);
                    }
                }
                else if (purchases.startIndex == oldPackageIds.Count)
                {
                    added = newPackageIds;
                }

                var addedPackages = added?.Select(id => m_PackageDatabase.GetPackage(id));
                var removedPackages = removed?.Select(id => m_PackageDatabase.GetPackage(id));
                TriggerOnListUpdate(added: addedPackages, removed: removedPackages);
            }

            UpdateVisualStateVisbilityWithSearchText();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                // When users log out, even when we are not on `My Assets` tab we should still clear the Asset Store page properly
                ClearListAndTriggerRebuildEvent();
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

        public override void AddSubPage(SubPage subPage)
        {
            // do nothing because we don't support sub pages on PaginatedPages yet.
        }

        public override void RebuildAndReorderVisualStates()
        {
            // do nothing because for paginated pages, the order of visual states is pre-determined
        }

        public override void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            // do nothing, only simple page needs implementation right now
        }

        public override void ResetUserUnlockedState()
        {
            // do nothing, only simple page needs implementation right now
        }

        public override bool GetDefaultLockState(IPackage package) => false;
    }
}
