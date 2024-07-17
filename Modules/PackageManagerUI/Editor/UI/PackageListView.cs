// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageListView : ListView, IPackageListView
    {
        [Serializable]
        public new class UxmlSerializedData : ListView.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageListView();
        }

        private IPackageDatabase m_PackageDatabase;
        private IPageManager m_PageManager;
        private IAssetStoreCache m_AssetStoreCache;
        private IBackgroundFetchHandler m_BackgroundFetchHandler;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_PageManager = container.Resolve<IPageManager>();
            m_AssetStoreCache = container.Resolve<IAssetStoreCache>();
            m_BackgroundFetchHandler = container.Resolve<IBackgroundFetchHandler>();
        }

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        public PackageListView()
        {
            ResolveDependencies();

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            makeItem = MakeItem;
            unbindItem = UnbindItem;
            bindItem = BindItem;

            selectionType = SelectionType.Multiple;
            virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            fixedItemHeight = PackageItem.k_MainItemHeight;

            horizontalScrollingEnabled = false;
            var scrollView = this.Q<ScrollView>();
            if (scrollView != null)
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        public void OnEnable()
        {
            selectionChanged += SyncListViewSelectionToPageManager;
            m_PageManager.onSelectionChanged += OnSelectionChanged;
        }

        public void OnDisable()
        {
            selectionChanged -= SyncListViewSelectionToPageManager;
            m_PageManager.onSelectionChanged -= OnSelectionChanged;
        }

        private void SyncListViewSelectionToPageManager(IEnumerable<object> items)
        {
            var selections = items.Select(item =>
            {
                var visualState = item as VisualState;
                var package = m_PackageDatabase.GetPackage(visualState?.packageUniqueId);
                return package?.uniqueId;
            }).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            // SelectionChange happens before BindItems, hence we use m_PageManager.SetSelected instead of packageItem.SelectMainItem
            // as PackageItems are null sometimes when SelectionChange is triggered
            m_PageManager.activePage.SetNewSelection(selections);
        }

        private void UnbindItem(VisualElement item, int index)
        {
            var package = (item as PackageItem)?.package;
            var product = package?.product;
            if(product == null)
                return;

            m_BackgroundFetchHandler.RemoveFromFetchProductInfoQueue(product.id);
            m_PackageItemsLookup.Remove(package.uniqueId);
        }

        private void BindItem(VisualElement item, int index)
        {
            var packageItem = item as PackageItem;
            if (packageItem == null)
                return;

            var visualState = GetVisualStateByIndex(index);
            if (string.IsNullOrEmpty(visualState?.packageUniqueId))
                return;

            var package = m_PackageDatabase.GetPackage(visualState.packageUniqueId);
            packageItem.SetPackageAndVisualState(package, visualState);

            m_PackageItemsLookup[visualState.packageUniqueId] = packageItem;
            var product = package?.product;
            if(product == null)
                return;

            if (package.versions.primary.HasTag(PackageTag.Placeholder))
                m_BackgroundFetchHandler.AddToFetchProductInfoQueue(product.id);

            if (m_AssetStoreCache.GetLocalInfo(product.id) != null && m_AssetStoreCache.GetUpdateInfo(product.id) == null)
                m_BackgroundFetchHandler.PushToCheckUpdateStack(product.id);
        }

        private VisualElement MakeItem()
        {
            return new PackageItem(m_PageManager, m_PackageDatabase);
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
                SyncPageManagerSelectionToListView();
            }
            return refresh;
        }

        private VisualState GetVisualStateByIndex(int index)
        {
            return (itemsSource as List<VisualState>)?.ElementAtOrDefault(index);
        }

        private void OnSelectionChanged(PageSelectionChangeArgs args)
        {
            SyncPageManagerSelectionToListView(args.page, args.selection);
        }

        // In PageManager we track selection by keeping track of the package unique ids (hence it's not affected by reordering)
        // In ListView, the selection is tracked by indices. As a result, when we update the itemsSource, we want to sync selection index if needed
        // Because there might be some sort of reordering
        private void SyncPageManagerSelectionToListView(IPage page = null, PageSelection selection = null)
        {
            var visualStates = itemsSource as List<VisualState>;
            if (visualStates == null)
                return;

            page ??= m_PageManager.activePage;
            if (page.id != MyAssetsPage.k_Id)
                return;

            selection ??= page.GetSelection();
            var oldSelectedVisualStates = selectedItems.OfType<VisualState>().ToArray();
            if (oldSelectedVisualStates.Length == selection.Count && oldSelectedVisualStates.All(v => selection.Contains(v.packageUniqueId)))
                return;

            var newSelectionIndices = new List<int>();
            for (var i = 0; i < visualStates.Count; i++)
                if (selection.Contains(visualStates[i].packageUniqueId))
                    newSelectionIndices.Add(i);
            SetSelection(newSelectionIndices);
        }

        public void ScrollToSelection()
        {
            if (m_PackageItemsLookup.Count == 0 || float.IsNaN(layout.height) || layout.height == 0)
            {
                EditorApplication.delayCall -= ScrollToSelection;
                EditorApplication.delayCall += ScrollToSelection;
                return;
            }

            // For now we want to just scroll to any of the selections, this behaviour might change in the future depending on how users react
            var firstSelectedPackageUniqueId = m_PageManager.activePage.GetSelection().firstSelection;
            if (string.IsNullOrEmpty(firstSelectedPackageUniqueId))
                return;

            EditorApplication.delayCall -= ScrollToSelection;

            var visualStates = itemsSource as List<VisualState>;
            var index = visualStates?.FindIndex(v => v.packageUniqueId == firstSelectedPackageUniqueId) ?? -1;
            if (index >= 0)
                ScrollToItem(index);
        }

        public void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (!visualStates.Any())
                return;

            foreach (var state in visualStates)
                GetPackageItem(state.packageUniqueId)?.UpdateVisualState(state);

            if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void OnListRebuild(IPage page)
        {
            UpdateItemsSource(page.visualStates.ToList(), true);

            m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid();
            ScrollToSelection();
        }

        public void OnListUpdate(ListUpdateArgs args)
        {
            var rebuildCalled = UpdateItemsSource(args.page.visualStates.ToList(), args.added.Any() || args.removed.Any());
            if (!rebuildCalled)
            {
                foreach (var package in args.updated)
                    GetPackageItem(package.uniqueId)?.SetPackageAndVisualState(package, m_PageManager.activePage.visualStates.Get(package.uniqueId));
            }

            if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void OnActivePageChanged(IPage page)
        {
            // Do nothing as we only show PackageListView for `My Assets` page for now
        }

        public void OnSeeAllPackageVersionsChanged(bool value)
        {
            // Do nothing as `My Assets` page is not affected by `See All Package Versions` setting
        }

        public void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            // We use keyboard events for ctrl, shift, A, and esc because UIToolkit does not
            // handle naigation events for them (18/07/2022)
            int index;
            switch (evt.keyCode)
            {
                case KeyCode.A when evt.actionKey:
                    SelectAll();
                    evt.StopPropagation();
                    break;
                case KeyCode.PageUp:
                    if (!selectedIndices.Any()) return;
                    index = Mathf.Max(0, selectedIndices.Max() - (virtualizationController.visibleItemCount - 1));
                    HandleSelectionAndScroll(index, evt.shiftKey);
                    evt.StopPropagation();
                    break;
                case KeyCode.PageDown:
                    if (!selectedIndices.Any()) return;
                    index = Mathf.Min(viewController.itemsSource.Count - 1,
                        selectedIndices.Max() + (virtualizationController.visibleItemCount - 1));
                    HandleSelectionAndScroll(index, evt.shiftKey);
                    evt.StopPropagation();
                    break;
                // On mac moving up and down will trigger the sound of an incorrect key being pressed
                // This should be fixed in UUM-26264 by the UIToolkit team
                case KeyCode.DownArrow:
                case KeyCode.UpArrow:
                    evt.StopPropagation();
                    break;
            }

            Focus();
        }

        public void OnNavigationMoveShortcut(NavigationMoveEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            var newSelectedIndex = -1;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    newSelectedIndex = selectedIndex - 1;
                    break;
                case NavigationMoveEvent.Direction.Down:
                    newSelectedIndex = selectedIndex + 1;
                    break;
            }

            if (newSelectedIndex < 0 || newSelectedIndex >= itemsSource.Count)
                return;
            HandleSelectionAndScroll(newSelectedIndex, evt.shiftKey);
            Focus();
            evt.StopPropagation();
        }

        private void HandleSelectionAndScroll(int index, bool shiftKey)
        {
            if (shiftKey)
                DoRangeSelection(index);
            else
                selectedIndex = index;
            ScrollToItem(index);
        }

        internal override ICollectionDragAndDropController CreateDragAndDropController() => null;
    }
}
