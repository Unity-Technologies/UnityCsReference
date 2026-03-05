// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal sealed class ItemListScrollView : ScrollView, IItemListView
    {
        private sealed class ReusableItemsPool
        {
            private readonly List<PackageItem> m_PackageItemsPool = new ();
            private readonly List<SampleItem> m_SampleItemsPool = new ();
            private readonly List<ListItemGroup> m_GroupsPool = new ();

            private readonly IResourceLoader m_ResourceLoader;
            private readonly IPageManager m_PageManager;
            private readonly IPackageDatabase m_PackageDatabase;
            public ReusableItemsPool(IResourceLoader resourceLoader, IPageManager pageManager, IPackageDatabase packageDatabase)
            {
                m_ResourceLoader = resourceLoader;
                m_PageManager = pageManager;
                m_PackageDatabase = packageDatabase;
            }

            public void Recycle(IListItem item)
            {
                switch (item)
                {
                    case PackageItem packageItem:
                        m_PackageItemsPool.Add(packageItem);
                        break;
                    case SampleItem sampleItem:
                        m_SampleItemsPool.Add(sampleItem);
                        break;
                }
            }

            public void Recycle(IEnumerable<IListItem> items)
            {
                foreach (var item in items)
                    Recycle(item);
            }

            public void Recycle(IEnumerable<ListItemGroup> groups) => m_GroupsPool.AddRange(groups);

            public PackageItem CreatePackageItem(VisualState visualState)
            {
                var result = UseLastItemFromPool(m_PackageItemsPool) ?? new PackageItem(m_PageManager, m_PackageDatabase);
                result.BindVisualState(visualState);
                return result;
            }

            public SampleItem CreateSampleItem(VisualState visualState)
            {
                var result = UseLastItemFromPool(m_SampleItemsPool) ?? new SampleItem(m_PageManager, m_PackageDatabase);
                result.BindVisualState(visualState);
                return result;
            }

            public ListItemGroup CreateGroup(string groupName)
            {
                var result = UseLastItemFromPool(m_GroupsPool);
                if (result != null)
                {
                    result.SetGroupName(groupName);
                    result.Clear();
                }
                else
                    result = new ListItemGroup(m_ResourceLoader, m_PageManager, groupName);
                return result;
            }

            private static T UseLastItemFromPool<T>(IList<T> pool) where T : class
            {
                if (pool.Count == 0)
                    return null;
                var lastIndex = pool.Count - 1;
                var last = pool[lastIndex];
                pool.RemoveAt(lastIndex);
                return last;
            }
        }

        private readonly Dictionary<string, IListItem> m_ItemsLookup = new ();
        private readonly Dictionary<string, ListItemGroup> m_GroupLookup = new ();

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal IEnumerable<IListItem> EnumerateVisibleItems()
        {
            foreach (var group in Children().FilterByType<ListItemGroup>())
                foreach (var item in group.items)
                    if (UIUtils.IsElementVisible(item.element))
                        yield return item;
        }

        private IEnumerable<ListItemGroup> EnumerateGroups() => Children().FilterByType<ListItemGroup>();

        public VisualElement element => this;

        private readonly ReusableItemsPool m_ItemsPool;

        private readonly IPageManager m_PageManager;
        public ItemListScrollView(IResourceLoader resourceLoader, IPageManager pageManager, IPackageDatabase packageDatabase)
        {
            m_PageManager = pageManager;

            m_ItemsPool = new ReusableItemsPool(resourceLoader, pageManager, packageDatabase);

            horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            focusable = true;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<MouseDownEvent>(OnMouseDown);
            UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
            UnregisterCallback<NavigationMoveEvent>(OnNavigationMoveShortcut);
        }

        public IListItem GetListItem(string itemUniqueId)
        {
            return string.IsNullOrEmpty(itemUniqueId) ? null : m_ItemsLookup.Get(itemUniqueId);
        }

        private ListItemGroup GetGroup(string groupName)
        {
            // Since empty group name is allowed, we need to explicitly check for null here rather than using string.isNullOrEmpty
            return groupName == null ? null : m_GroupLookup.Get(groupName);
        }

        public void ScrollToSelection()
        {
            UIUtils.ScrollIfNeeded(this, GetListItem(m_PageManager.activePage.GetSelection().last)?.element);
        }

        private IListItem CreateItem(VisualState visualState, bool isSample)
        {
            return isSample ? m_ItemsPool.CreateSampleItem(visualState) : m_ItemsPool.CreatePackageItem(visualState);
        }

        public void OnVisualStateChange(IReadOnlyCollection<VisualState> visualStates)
        {
            if (visualStates.Count == 0)
                return;

            foreach (var state in visualStates)
                GetListItem(state.itemUniqueId)?.BindVisualState(state);

            foreach (var group in EnumerateGroups())
                group.RefreshHeaderVisibility();

            if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void OnListRebuild(IPage page)
        {
            m_ItemsPool.Recycle(m_ItemsLookup.Values);
            m_ItemsLookup.Clear();

            var isSample = page.id == SamplesPage.k_Id;
            foreach (var visualState in page.visualStates)
                m_ItemsLookup[visualState.itemUniqueId] = CreateItem(visualState, isSample);

            SortGroupAndItems(page.visualStates);

            foreach (var group in EnumerateGroups())
                group.RefreshHeaderVisibility();

            m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid();
            ScrollToSelection();
        }

        public void OnListUpdate(ListUpdateArgs args)
        {
            foreach (var itemUniqueId in args.removed.Join(args.updated))
            {
                var item = GetListItem(itemUniqueId);
                if (item == null)
                    continue;
                m_ItemsLookup.Remove(itemUniqueId);
                item.element?.RemoveFromHierarchy();
                m_ItemsPool.Recycle(item);
            }

            var visualStates = args.page.visualStates;
            if (args.added.Count > 0 || args.updated.Count > 0)
            {
                var isSample = args.page.id == SamplesPage.k_Id;
                foreach (var itemUniqueId in args.added.Join(args.updated))
                    m_ItemsLookup[itemUniqueId] = CreateItem(visualStates.Get(itemUniqueId), isSample);

                SortGroupAndItems(visualStates);
            }

            foreach (var group in EnumerateGroups())
                group.RefreshHeaderVisibility();

            if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        public void SortGroupAndItems(IVisualStateList visualStates)
        {
            m_ItemsPool.Recycle(m_GroupLookup.Values);
            m_GroupLookup.Clear();
            Clear();

            foreach (var groupName in visualStates.orderedGroupNames)
            {
                var group = m_ItemsPool.CreateGroup(groupName);
                m_GroupLookup[groupName] = group;
                Add(group);
            }

            foreach (var state in visualStates)
            {
                var item = GetListItem(state.itemUniqueId)?.element;
                if (item != null)
                    GetGroup(state.groupName)?.Add(item);
            }
        }

        private IListItem GetItemFromMouseEvent(MouseDownEvent evt)
        {
            var target = evt.elementTarget;
            while (target != null && target != this)
            {
                if (target is IListItem listItem)
                    return listItem;
                target = target.parent;
            }
            return null;
        }

        private void SelectAllBetween(string firstItemUniqueId, string secondItemUniqueId)
        {
            var newSelections = new List<string>();
            var inBetweenTwoItems = false;
            if (firstItemUniqueId == secondItemUniqueId)
            {
                newSelections.Add(firstItemUniqueId);
            }
            else
            {
                foreach (var item in EnumerateVisibleItems())
                {
                    var itemUniqueId = item.visualState?.itemUniqueId;
                    if (string.IsNullOrEmpty(itemUniqueId))
                        continue;

                    var matchFirstItem = itemUniqueId == firstItemUniqueId;
                    var matchSecondItem = itemUniqueId == secondItemUniqueId;
                    if (matchFirstItem || matchSecondItem || inBetweenTwoItems)
                        newSelections.Add(itemUniqueId);

                    if (matchFirstItem || matchSecondItem)
                    {
                        inBetweenTwoItems = !inBetweenTwoItems;
                        if (!inBetweenTwoItems)
                        {
                            // If we match the second item before we matched the first item then we want to reverse the order
                            // to first sure that the first selected is in front. Otherwise, shift selections might have weird behaviours.
                            if (matchFirstItem)
                                newSelections.Reverse();
                            break;
                        }
                    }
                }
            }
            m_PageManager.activePage.SetNewSelection(newSelections, false);
        }

        private void SelectAllVisible()
        {
            var visibleItems = EnumerateVisibleItems().SelectNonEmpty(i => i.visualState?.itemUniqueId);
            m_PageManager.activePage.SetNewSelection(visibleItems, false);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
                return;

            var itemUniqueId = GetItemFromMouseEvent(evt)?.visualState.itemUniqueId;
            if (string.IsNullOrEmpty(itemUniqueId))
                return;

            if (evt.shiftKey)
            {
                var firstItem = m_PageManager.activePage.GetSelection().first;
                SelectAllBetween(firstItem, itemUniqueId);
                return;
            }

            if (evt.actionKey)
                m_PageManager.activePage.ToggleSelection(itemUniqueId, true);
            else
                m_PageManager.activePage.SetNewSelection(itemUniqueId, true);
        }

        private bool HandleShiftSelection(NavigationMoveEvent evt)
        {
            if (!evt.shiftKey)
                return false;

            if (evt.direction is NavigationMoveEvent.Direction.Up or NavigationMoveEvent.Direction.Down)
            {
                var page = m_PageManager.activePage;
                var selection = page.GetSelection();
                var firstItem = selection.first;
                var lastItem = selection.last;

                var nextItem = FindNextVisibleItemToSelect(page, lastItem, evt.direction == NavigationMoveEvent.Direction.Up);
                SelectAllBetween(firstItem, nextItem?.itemUniqueId ?? lastItem);
                return true;
            }
            return false;
        }

        public void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            switch (evt.keyCode)
            {
                case KeyCode.A when evt.actionKey:
                    SelectAllVisible();
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

        public void OnNavigationMoveShortcut(NavigationMoveEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            if (HandleShiftSelection(evt))
            {
                evt.StopPropagation();
                return;
            }

            if (evt.direction == NavigationMoveEvent.Direction.Up)
            {
                if (SelectNext(true))
                    evt.StopPropagation();
            }
            else if (evt.direction == NavigationMoveEvent.Direction.Down)
            {
                if (SelectNext(false))
                    evt.StopPropagation();
            }
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal bool SelectNext(bool reverseOrder)
        {
            var activePage = m_PageManager.activePage;
            var nextItem = FindNextVisibleItemToSelect(activePage, activePage.GetSelection().last, reverseOrder);
            if (nextItem != null)
            {
                activePage.SetNewSelection(nextItem.itemUniqueId, false);
                return true;
            }
            return false;
        }

        private VisualState FindNextVisibleItemToSelect(IPage page, string currentItem, bool reverseOrder)
        {
            var visualStateList = page.visualStates;
            var nextElement = visualStateList.GetNext(currentItem, reverseOrder);
            while (nextElement != null && (!nextElement.visible || !UIUtils.IsElementVisible(GetListItem(nextElement.itemUniqueId)?.element)))
                nextElement = visualStateList.GetNext(nextElement.itemUniqueId, reverseOrder);
            return nextElement;
        }
    }
}
