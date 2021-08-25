// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageListView : BaseVerticalCollectionView, IPackageListView
    {
        internal new class UxmlFactory : UxmlFactory<PackageListView> {}

        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private AssetStoreCache m_AssetStoreCache;
        private AssetStoreCallQueue m_AssetStoreCallQueue;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
            m_AssetStoreCallQueue = container.Resolve<AssetStoreCallQueue>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
        }

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        public PackageListView()
        {
            ResolveDependencies();

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            makeItem = MakeItem;
            unbindItem = UnbindItem;
            bindItem = BindItem;
            onSelectionChange += OnSelectionChange;
            m_PageManager.onSelectionChanged += version => SyncSelectionIndex(version?.packageUniqueId);

            selectionType = SelectionType.Single;
            virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            fixedItemHeight = PackageItem.k_MainItemHeight;

            horizontalScrollingEnabled = false;
            var scrollView = this.Q<ScrollView>();
            if (scrollView != null)
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        private void OnSelectionChange(IEnumerable<object> items)
        {
            var selectedPackageUniqueId = (selectedItem as VisualState)?.packageUniqueId;

            // SelectionChange happens before BindItems, hence we use m_PageManager.SetSelected instead of packageItem.SelectMainItem
            // as PackageItems are null sometimes when SelectionChange is triggered
            var package = m_PackageDatabase.GetPackage(selectedPackageUniqueId);
            if (package != null)
                m_PageManager.SetSelected(package, null, true);
        }

        private void UnbindItem(VisualElement item, int index)
        {
            var packageItem = item as PackageItem;
            var packageUniqueId = packageItem?.package?.uniqueId;
            if (string.IsNullOrEmpty(packageUniqueId))
                return;

            m_AssetStoreCallQueue.RemoveFromFetchDetailsQueue(packageUniqueId);
            m_PackageItemsLookup.Remove(packageUniqueId);
        }

        private void BindItem(VisualElement item, int index)
        {
            var packageItem = item as PackageItem;
            if (packageItem == null)
                return;

            var visualState = GetVisualStateByIndex(index);
            if (!string.IsNullOrEmpty(visualState?.packageUniqueId))
            {
                var package = m_PackageDatabase.GetPackage(visualState.packageUniqueId);
                packageItem.SetPackageAndVisualState(package, visualState);

                m_PackageItemsLookup[visualState.packageUniqueId] = packageItem;

                if (package is PlaceholderPackage)
                    m_AssetStoreCallQueue.AddToFetchDetailsQueue(visualState.packageUniqueId);

                var localInfo = m_AssetStoreCache.GetLocalInfo(visualState.packageUniqueId);
                if (localInfo?.updateInfoFetched == false)
                    m_AssetStoreCallQueue.InsertToCheckUpdateQueue(visualState.packageUniqueId);
            }
        }

        private VisualElement MakeItem()
        {
            return new PackageItem(m_PageManager, m_SettingsProxy, m_PackageDatabase);
        }

        public PackageItem GetPackageItem(string packageUniqueId)
        {
            return string.IsNullOrEmpty(packageUniqueId) ? null : m_PackageItemsLookup.Get(packageUniqueId);
        }

        // Returns true if RefreshItems is triggered and false otherwise
        private bool UpdateItemsSource(List<VisualState> visualStates, bool forceRefesh = false)
        {
            var refresh = forceRefesh || visualStates?.Count != itemsSource?.Count;
            itemsSource = visualStates;
            if (refresh)
            {
                RefreshItems();
                SyncSelectionIndex();
            }
            return refresh;
        }

        private VisualState GetVisualStateByIndex(int index)
        {
            return (itemsSource as List<VisualState>)?.ElementAtOrDefault(index);
        }

        // In PageManager we track selection by keeping track of the package unique ids (hence it's not affected by reordering)
        // In ListView, the selection is tracked by indices. As a result, when we update the itemsSource, we want to sync selection index if needed
        // Because there might be some sort of reordering
        private void SyncSelectionIndex(string selectedPackageUniqueIdInPageManager = null)
        {
            selectedPackageUniqueIdInPageManager ??= m_PageManager.GetCurrentPage().GetSelectedVisualState()?.packageUniqueId;
            var selectedPackageUniqueIdInListView = (selectedItem as VisualState)?.packageUniqueId;
            if (selectedPackageUniqueIdInListView == selectedPackageUniqueIdInPageManager)
                return;

            var visualStates = itemsSource as List<VisualState>;
            if (visualStates == null)
                return;

            for (var i = 0; i < visualStates.Count; i++)
            {
                if (selectedPackageUniqueIdInPageManager == visualStates[i].packageUniqueId)
                {
                    if (selectedIndex != i)
                        selectedIndex = i;
                    return;
                }
            }
        }

        public void ScrollToSelection()
        {
            if (m_PackageItemsLookup.Count == 0 || float.IsNaN(layout.height) || layout.height == 0)
            {
                EditorApplication.delayCall -= ScrollToSelection;
                EditorApplication.delayCall += ScrollToSelection;
                return;
            }

            var selectedPackageUniqueId = m_PageManager.GetCurrentPage().GetSelectedVersion()?.packageUniqueId;
            if (string.IsNullOrEmpty(selectedPackageUniqueId))
                return;

            EditorApplication.delayCall -= ScrollToSelection;

            var visualStates = itemsSource as List<VisualState>;
            var index = visualStates?.FindIndex(v => v.packageUniqueId == selectedPackageUniqueId) ?? -1;
            if (index >= 0)
                ScrollToItem(index);
        }

        public void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (!visualStates.Any())
                return;

            foreach (var state in visualStates)
                GetPackageItem(state.packageUniqueId)?.UpdateVisualState(state);

            if (m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void OnListRebuild(IPage page)
        {
            UpdateItemsSource(page.visualStates.ToList(), true);

            m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid();
            ScrollToSelection();
        }

        public void OnListUpdate(ListUpdateArgs args)
        {
            var rebuildCalled = UpdateItemsSource(args.page.visualStates.ToList(), args.added.Any() || args.removed.Any());
            if (!rebuildCalled)
            {
                foreach (var package in args.updated)
                    GetPackageItem(package.uniqueId)?.SetPackageAndVisualState(package, m_PageManager.GetCurrentPage().GetVisualState(package.uniqueId));
            }

            if (m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            // Do nothing as we only show PackageListView for `My Assets` tab for now
        }

        public void OnSeeAllPackageVersionsChanged(bool value)
        {
            // Do nothing as `My Assets` tab is not affected by `See All Package Versions` setting
        }

        public void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            // Since the Keyboard Navigation Manipulator only works when the list view is focused, we need to do a special handling
            // when the focus is not on the ListView so that the behaviour is consistent with the scroll view. Note that once the
            // ListView is focused, this function won't trigger because the KeyDownEvent would get intercepted at the ListView level
            // and will not bubble up. Only the arrow keys are supported in the scroll view, so we do the same for ListView for now.
            if ((evt.keyCode == KeyCode.UpArrow && SelectNext(true)) || (evt.keyCode == KeyCode.DownArrow && SelectNext(false)))
            {
                Focus();
                evt.StopPropagation();
            }
        }

        private bool SelectNext(bool reverseOrder)
        {
            var newSelectedIndex = reverseOrder ? selectedIndex - 1 : selectedIndex + 1;
            if (newSelectedIndex < 0 || newSelectedIndex >= itemsSource.Count)
                return false;
            selectedIndex = newSelectedIndex;
            return true;
        }

        internal override ICollectionDragAndDropController CreateDragAndDropController() => null;
    }
}
