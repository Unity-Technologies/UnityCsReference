// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal sealed class PackageListView : VisualElement, IItemListView
    {
        // We need this wrapper class so that PackageItem's style doesn't get affected by the styles applied to the ListView's items
        private class ListItem : VisualElement
        {
            public PackageItem packageItem { get; }
            public ListItem(IPageManager pageManager, IPackageDatabase packageDatabase)
            {
                packageItem = new PackageItem(pageManager, packageDatabase);
                Add(packageItem);
            }
        }

        private class ListViewWithoutDragAndDrop : ListView
        {
            internal override ICollectionDragAndDropController CreateDragAndDropController() => null;
        }

        private readonly Dictionary<string, PackageItem> m_PackageItemsLookup = new ();

        private readonly ListView m_ListView;

        private readonly List<VisualState> m_VisualStates = new ();

        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IBackgroundFetchHandler m_BackgroundFetchHandler;
        public PackageListView(
            IResourceLoader resourceLoader,
            IApplicationProxy application,
            IUnityConnectProxy unityConnect,
            IProjectSettingsProxy settingsProxy,
            IPageRefreshHandler pageRefreshHandler,
            IPackageDatabase packageDatabase,
            IPageManager pageManager,
            IAssetStoreCache assetStoreCache,
            IBackgroundFetchHandler backgroundFetchHandler)
        {
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;
            m_AssetStoreCache = assetStoreCache;
            m_BackgroundFetchHandler = backgroundFetchHandler;

            name = "listViewContainer";

            m_ListView = new ListViewWithoutDragAndDrop { name =  "listView" };

            m_ListView.makeItem = MakeItem;
            m_ListView.unbindItem = UnbindItem;
            m_ListView.bindItem = BindItem;

            m_ListView.selectionType = SelectionType.Multiple;
            m_ListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            m_ListView.fixedItemHeight = PackageItem.k_MainItemHeight;

            m_ListView.horizontalScrollingEnabled = false;
            var scrollView = m_ListView.Q<ScrollView>();
            if (scrollView != null)
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            Add(m_ListView);

            Add(new PackageLoadBar(resourceLoader, application, unityConnect, pageManager, settingsProxy, pageRefreshHandler) { name = "packageLoadBar" });

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            RegisterCallback<KeyDownEvent>(IgnoreEscapeKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut);

            m_ListView.selectionChanged += SyncListViewSelectionToPageManager;
            m_PageManager.onSelectionChanged += OnSelectionChanged;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            UnregisterCallback<KeyDownEvent>(IgnoreEscapeKeyDown, TrickleDown.TrickleDown);
            UnregisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut);

            m_ListView.selectionChanged -= SyncListViewSelectionToPageManager;
            m_PageManager.onSelectionChanged -= OnSelectionChanged;
        }

        private void SyncListViewSelectionToPageManager(IEnumerable<object> items)
        {
            var selections = items.SelectNonEmpty(item => (item as VisualState)?.itemUniqueId);
            m_PageManager.activePage.SetNewSelection(selections, true);
        }

        private void UnbindItem(VisualElement item, int index)
        {
            if (item is not ListItem listItem)
                return;

            var itemUniqueId = listItem.packageItem.visualState?.itemUniqueId ?? string.Empty;
            var product = m_PackageDatabase.GetPackage(itemUniqueId)?.product;
            if (product == null)
                return;

            m_BackgroundFetchHandler.RemoveFromFetchProductInfoQueue(product.id);
            m_PackageItemsLookup.Remove(itemUniqueId);
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not ListItem listItem)
                return;

            var visualState = GetVisualStateByIndex(index);
            if (string.IsNullOrEmpty(visualState?.itemUniqueId))
                return;

            var packageItem = listItem.packageItem;
            packageItem.BindVisualState(visualState);
            m_PackageItemsLookup[visualState.itemUniqueId] = packageItem;
            var package = m_PackageDatabase.GetPackage(visualState.itemUniqueId);
            var product = package?.product;
            if (product == null)
                return;

            if (package.versions.primary.HasTag(PackageTag.Placeholder))
                m_BackgroundFetchHandler.AddToFetchProductInfoQueue(product.id);

            if (m_AssetStoreCache.GetLocalInfo(product.id) != null && m_AssetStoreCache.GetUpdateInfo(product.id) == null)
                m_BackgroundFetchHandler.PushToCheckUpdateStack(product.id);
        }

        private VisualElement MakeItem()
        {
            return new ListItem(m_PageManager, m_PackageDatabase);
        }

        public VisualElement element => this;
        public IListItem GetListItem(string itemUniqueId) => GetPackageItem(itemUniqueId);

        private PackageItem GetPackageItem(string itemUniqueId)
        {
            return string.IsNullOrEmpty(itemUniqueId) ? null : m_PackageItemsLookup.Get(itemUniqueId);
        }

        // Returns true if RefreshItems is triggered and false otherwise
        private bool UpdateItemsSource(IEnumerable<VisualState> visualStates, bool forceRefresh = false)
        {
            var oldNumItems = m_ListView.itemsSource?.Count ?? 0;
            m_VisualStates.Clear();
            m_VisualStates.AddRange(visualStates);

            var refresh = forceRefresh || m_VisualStates.Count != oldNumItems;
            m_ListView.itemsSource = m_VisualStates;
            if (refresh)
            {
                m_ListView.RefreshItems();
                SyncPageManagerSelectionToListView();
            }
            return refresh;
        }

        private VisualState GetVisualStateByIndex(int index)
        {
            if (index < 0 || index >= m_VisualStates.Count)
                return null;
            return m_VisualStates[index];
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
            if (m_VisualStates == null)
                return;

            page ??= m_PageManager.activePage;
            if (page.id != MyAssetsPage.k_Id)
                return;

            selection ??= page.GetSelection();
            var numOldSelections = 0;
            var selectionRemoved = false;
            foreach (var index in m_ListView.selectedIndices)
            {
                numOldSelections++;
                var visualState = GetVisualStateByIndex(index);
                if (visualState != null && selection.Contains(visualState.itemUniqueId))
                    continue;
                selectionRemoved = true;
                break;
            }

            if (!selectionRemoved && numOldSelections == selection.Count)
                return;

            var newSelectionIndices = new List<int>();
            for (var i = 0; i < m_VisualStates.Count; i++)
                if (selection.Contains(m_VisualStates[i].itemUniqueId))
                    newSelectionIndices.Add(i);
            m_ListView.SetSelection(newSelectionIndices);
        }

        public void ScrollToSelection()
        {
            if (m_PackageItemsLookup.Count == 0 || float.IsNaN(layout.height) || layout.height == 0)
            {
                EditorApplication.delayCall -= ScrollToSelection;
                EditorApplication.delayCall += ScrollToSelection;
                return;
            }

            // For now, we want to just scroll to any of the selections, this behaviour might change in the future depending on how users react
            var selection = m_PageManager.activePage.GetSelection();
            if (selection.Count == 0)
                return;

            EditorApplication.delayCall -= ScrollToSelection;

            var visualStates = m_ListView.itemsSource as List<VisualState>;
            var index = visualStates?.FindIndex(v => selection.Contains(v.itemUniqueId)) ?? -1;
            if (index >= 0)
                m_ListView.ScrollToItem(index);
        }

        public void OnVisualStateChange(IReadOnlyCollection<VisualState> visualStates)
        {
            if (visualStates.Count == 0)
                return;

            foreach (var state in visualStates)
                GetListItem(state.itemUniqueId)?.BindVisualState(state);

            if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void OnListRebuild(IPage page)
        {
            UpdateItemsSource(page.visualStates, true);

            m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid();
            ScrollToSelection();
        }

        public void OnListUpdate(ListUpdateArgs args)
        {
            var rebuildCalled = UpdateItemsSource(args.page.visualStates, args.added.Count > 0 || args.removed.Count > 0);
            if (!rebuildCalled)
                foreach (var itemUniqueId in args.updated)
                    GetPackageItem(itemUniqueId)?.BindVisualState(m_PageManager.activePage.visualStates.Get(itemUniqueId));

            if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        private int FindMaxSelectedIndex()
        {
            var result = -1;
            foreach (var index in m_ListView.selectedIndices)
                if (index > result)
                    result = index;
            return result;
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            // We use keyboard events for ctrl, shift, A, and esc because UIToolkit does not
            // handle navigation events for them (18/07/2022)
            switch (evt.keyCode)
            {
                case KeyCode.A when evt.actionKey:
                    m_ListView.SelectAll();
                    evt.StopPropagation();
                    break;
                case KeyCode.PageUp:
                    var maxSelectedIndex = FindMaxSelectedIndex();
                    if (maxSelectedIndex < 0)
                        return;
                    var index = Math.Max(0, maxSelectedIndex - (m_ListView.virtualizationController.visibleItemCount - 1));
                    HandleSelectionAndScroll(index, evt.shiftKey);
                    evt.StopPropagation();
                    break;
                case KeyCode.PageDown:
                    maxSelectedIndex = FindMaxSelectedIndex();
                    if (maxSelectedIndex < 0)
                        return;
                    index = Math.Min(m_ListView.viewController.itemsSource.Count - 1, maxSelectedIndex + (m_ListView.virtualizationController.visibleItemCount - 1));
                    HandleSelectionAndScroll(index, evt.shiftKey);
                    evt.StopPropagation();
                    break;
                // On macOS moving up and down will trigger the sound of an incorrect key being pressed
                // This should be fixed in UUM-26264 by the UIToolkit team
                case KeyCode.DownArrow:
                case KeyCode.UpArrow:
                    evt.StopPropagation();
                    break;
            }
        }

        // The default ListView escape key behaviour is to clear all selections, however, we want to always have something selected
        // therefore we register a TrickleDown callback handler to intercept escape key events to make it do nothing.
        private void IgnoreEscapeKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                evt.StopImmediatePropagation();
        }

        public void OnNavigationMoveShortcut(NavigationMoveEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            var newSelectedIndex = -1;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    newSelectedIndex = m_ListView.selectedIndex - 1;
                    break;
                case NavigationMoveEvent.Direction.Down:
                    newSelectedIndex = m_ListView.selectedIndex + 1;
                    break;
            }

            if (newSelectedIndex < 0 || newSelectedIndex >= m_ListView.itemsSource.Count)
                return;
            HandleSelectionAndScroll(newSelectedIndex, evt.shiftKey);
            Focus();
            evt.StopPropagation();
        }

        private void HandleSelectionAndScroll(int index, bool shiftKey)
        {
            if (shiftKey)
                m_ListView.DoRangeSelection(index);
            else
                m_ListView.selectedIndex = index;
            m_ListView.ScrollToItem(index);
        }
    }
}
