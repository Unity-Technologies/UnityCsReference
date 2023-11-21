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
    internal class PackageListScrollView : ScrollView, IPackageListView
    {
        [Serializable]
        public new class UxmlSerializedData : ScrollView.UxmlSerializedData
        {
            public override object CreateInstance() => new PackageListScrollView();
        }

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        private IResourceLoader m_ResourceLoader;
        private IPackageDatabase m_PackageDatabase;
        private IPageManager m_PageManager;
        private IPageRefreshHandler m_PageRefreshHandler;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_PageManager = container.Resolve<IPageManager>();
            m_PageRefreshHandler = container.Resolve<IPageRefreshHandler>();
        }

        internal IEnumerable<PackageItem> packageItems => packageGroups.SelectMany(group => group.packageItems);
        internal IEnumerable<PackageGroup> packageGroups => m_ItemsList.Children().OfType<PackageGroup>();

        private VisualElement m_ItemsList;

        private VisualElement m_ScrollToTarget;

        public PackageListScrollView()
        {
            ResolveDependencies();

            m_ItemsList = new VisualElement();
            Add(m_ItemsList);

            viewDataKey = "package-list-scrollview-key";
            horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            focusable = true;

            contentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            contentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;
            contentContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;
            contentContainer.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        public PackageItem GetPackageItem(string packageUniqueId)
        {
            return string.IsNullOrEmpty(packageUniqueId) ? null : m_PackageItemsLookup.Get(packageUniqueId);
        }

        private ISelectableItem GetFirstSelectedItem()
        {
            m_PackageDatabase.GetPackageAndVersion(m_PageManager.activePage.GetSelection().firstSelection, out var package, out var version);
            var selectedVersion = version ?? package?.versions.primary;
            return GetPackageItem(selectedVersion?.package.uniqueId);
        }

        public void ScrollToSelection()
        {
            // For now we want to just scroll to any of the selections, this behaviour might change in the future depending on how users react
            ScrollIfNeeded(GetFirstSelectedItem()?.element);
        }

        private void ScrollIfNeeded()
        {
            if (m_ScrollToTarget == null)
                return;

            EditorApplication.delayCall -= ScrollIfNeeded;
            if (float.IsNaN(layout.height) || layout.height == 0 || float.IsNaN(m_ScrollToTarget.layout.height))
            {
                EditorApplication.delayCall += ScrollIfNeeded;
                return;
            }

            var scrollViews = UIUtils.GetParentsOfType<ScrollView>(m_ScrollToTarget);
            foreach (var scrollview in scrollViews)
                UIUtils.ScrollIfNeeded(scrollview, m_ScrollToTarget);
            m_ScrollToTarget = null;
        }

        private void ScrollIfNeeded(VisualElement target)
        {
            m_ScrollToTarget = target;
            ScrollIfNeeded();
        }

        private void AddOrUpdatePackageItem(VisualState state, IPackage package = null)
        {
            package ??= m_PackageDatabase.GetPackage(state?.packageUniqueId);
            if (package == null)
                return;

            var item = GetPackageItem(state.packageUniqueId);
            if (item != null)
            {
                item.SetPackage(package);

                // Check if group has changed
                var page = m_PageManager.activePage;
                var groupName = page.GetGroupName(package);
                if (item.packageGroup.name != groupName)
                {
                    var oldGroup = GetOrCreateGroup(item.packageGroup.name);
                    var newGroup = GetOrCreateGroup(groupName);
                    state.groupName = groupName;

                    // Replace PackageItem
                    m_PackageItemsLookup[package.uniqueId] = newGroup.AddPackageItem(package, state);
                    oldGroup.RemovePackageItem(item);
                    if (!oldGroup.packageItems.Any())
                        m_ItemsList.Remove(oldGroup);

                    ReorderGroups(page.visualStates.orderedGroups);
                }

                item.UpdateVisualState(state);
            }
            else
            {
                var group = GetOrCreateGroup(state.groupName);
                item = group.AddPackageItem(package, state);
                m_PackageItemsLookup[package.uniqueId] = item;
            }
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            var group = packageGroups.FirstOrDefault(g => string.Compare(g.name, groupName, StringComparison.InvariantCultureIgnoreCase) == 0);
            if (group != null)
                return group;

            var hidden = string.IsNullOrEmpty(groupName);
            var expanded = m_PageManager.activePage.IsGroupExpanded(groupName);
            group = new PackageGroup(m_ResourceLoader, m_PageManager, m_PackageDatabase, groupName, expanded, hidden);
            if (!hidden)
            {
                group.onGroupToggle += value =>
                {
                    var s = GetFirstSelectedItem();
                    if (value && s != null && group.Contains(s))
                        EditorApplication.delayCall += ScrollToSelection;
                };
            }
            m_ItemsList.Add(group);
            return group;
        }

        private void RemovePackageItem(string packageUniqueId)
        {
            var item = GetPackageItem(packageUniqueId);
            if (item != null)
            {
                item.packageGroup.RemovePackageItem(item);
                m_PackageItemsLookup.Remove(packageUniqueId);
            }
        }

        public void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (!visualStates.Any())
                return;

            foreach (var state in visualStates)
                GetPackageItem(state.packageUniqueId)?.UpdateVisualState(state);

            var page = m_PageManager.activePage;
            ReorderGroups(page.visualStates.orderedGroups);
            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            if (page.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        private void ReorderGroups(IList<string> orderedGroups)
        {
            if (orderedGroups.Count <= 1)
                return;
            m_ItemsList.Sort((x, y) => orderedGroups.IndexOf(x.name).CompareTo(orderedGroups.IndexOf(y.name)));
        }

        public void OnListRebuild(IPage page)
        {
            m_ItemsList.Clear();
            m_PackageItemsLookup.Clear();

            foreach (var visualState in page.visualStates)
                AddOrUpdatePackageItem(visualState);

            ReorderGroups(page.visualStates.orderedGroups);
            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid();
            ScrollToSelection();
        }

        public void OnListUpdate(ListUpdateArgs args)
        {
            var page = args.page;
            var numItems = m_PackageItemsLookup.Count;
            foreach (var package in args.removed)
                RemovePackageItem(package?.uniqueId);

            var itemsRemoved = numItems != m_PackageItemsLookup.Count;
            numItems = m_PackageItemsLookup.Count;

            foreach (var package in args.added.Concat(args.updated))
            {
                var visualState = page.visualStates.Get(package.uniqueId) ?? new VisualState(package.uniqueId, string.Empty, page.GetDefaultLockState(package));
                AddOrUpdatePackageItem(visualState, package);
            }
            var itemsAdded = numItems != m_PackageItemsLookup.Count;

            if (args.reorder)
            {
                if (packageGroups.Any(group => !group.isHidden))
                {
                    // re-order if there are any added or updated items
                    foreach (var group in packageGroups)
                        group.ClearPackageItems();

                    foreach (var state in page.visualStates)
                    {
                        var packageItem = GetPackageItem(state.packageUniqueId);

                        // For when user switch account and packageList gets refreshed
                        packageItem?.packageGroup.AddPackageItem(packageItem);
                    }

                    m_PackageItemsLookup = packageItems.ToDictionary(item => item.package.uniqueId, item => item);
                }
            }

            if (itemsRemoved || itemsAdded)
            {
                ReorderGroups(page.visualStates.orderedGroups);
                foreach (var group in packageGroups)
                    group.RefreshHeaderVisibility();

                if (m_PageManager.activePage.UpdateSelectionIfCurrentSelectionIsInvalid())
                    ScrollToSelection();
            }
            else
            {
                ReorderGroups(page.visualStates.orderedGroups);
            }
        }

        private PackageItem GetPackageItemFromMouseEvent(MouseDownEvent evt)
        {
            var target = evt.elementTarget;
            while (target != null && target != this)
            {
                if (target is PackageItem packageItem)
                    return packageItem;
                target = target.parent;
            }
            return null;
        }

        private void SelectAllBetween(string firstPackageUniqueId, string secondPackageUniqueId)
        {
            var newSelections = new List<PackageAndVersionIdPair>();
            var inBetweenTwoPackages = false;
            if (firstPackageUniqueId == secondPackageUniqueId)
            {
                newSelections.Add(new PackageAndVersionIdPair(firstPackageUniqueId));
            }
            else
            {
                foreach (var item in packageItems)
                {
                    if (!UIUtils.IsElementVisible(item))
                        continue;
                    var packageUniqueId = item.package?.uniqueId;
                    if (string.IsNullOrEmpty(packageUniqueId))
                        continue;

                    var matchFirstPackage = packageUniqueId == firstPackageUniqueId;
                    var matchSecondPackage = packageUniqueId == secondPackageUniqueId;
                    if (matchFirstPackage || matchSecondPackage || inBetweenTwoPackages)
                        newSelections.Add(new PackageAndVersionIdPair(packageUniqueId));

                    if (matchFirstPackage || matchSecondPackage)
                    {
                        inBetweenTwoPackages = !inBetweenTwoPackages;
                        if (!inBetweenTwoPackages)
                        {
                            // If we match the second package before we matched the first package then we want to reverse the order
                            // to first sure that the first selected is in front. Otherwise Shift selections might have weird behaviours.
                            if (matchFirstPackage)
                                newSelections.Reverse();
                            break;
                        }
                    }
                }
            }
            m_PageManager.activePage.SetNewSelection(newSelections, true);
        }

        private void SelectAllVisible()
        {
            var validItems = packageItems.Where(p => !string.IsNullOrEmpty(p.package?.uniqueId) && UIUtils.IsElementVisible(p));
            m_PageManager.activePage.SetNewSelection(validItems.Select(item => new PackageAndVersionIdPair(item.package.uniqueId)), true);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
                return;

            var packageItem = GetPackageItemFromMouseEvent(evt);
            if (packageItem == null)
                return;

            if (evt.shiftKey)
            {
                var firstItem = m_PageManager.activePage.GetSelection().firstSelection?.packageUniqueId;
                SelectAllBetween(firstItem, packageItem.package.uniqueId);
                return;
            }

            if (evt.actionKey)
                packageItem.ToggleSelectMainItem();
            else
                packageItem.SelectMainItem();
        }

        private bool HandleShiftSelection(NavigationMoveEvent evt)
        {
            if (!evt.shiftKey)
                return false;

            if (evt.direction == NavigationMoveEvent.Direction.Up || evt.direction == NavigationMoveEvent.Direction.Down)
            {
                var selection = m_PageManager.activePage.GetSelection();
                var firstItem = selection.firstSelection?.packageUniqueId;
                var lastItem = selection.lastSelection?.packageUniqueId;

                var nextItem = FindNextVisiblePackageItem(GetPackageItem(lastItem), evt.direction == NavigationMoveEvent.Direction.Up);
                SelectAllBetween(firstItem, nextItem?.package.uniqueId ?? lastItem);
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
                // On mac moving up and down will trigger the sound of an incorrect key being pressed
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

        internal bool SelectNext(bool reverseOrder)
        {
            var nextElement = FindNextVisibleSelectableItem(reverseOrder);
            if (nextElement != null)
            {
                m_PageManager.activePage.SetNewSelection(nextElement.package, nextElement.targetVersion);
                ScrollIfNeeded(nextElement.element);
                return true;
            }
            return false;
        }

        private ISelectableItem FindNextVisibleSelectableItem(bool reverseOrder)
        {
            // We use the `lastSelection` here as that is the one the user interacted last and it feels more natural that way when navigating with keyboard
            var lastSelection = m_PageManager.activePage.GetSelection().lastSelection;
            var packageItem = GetPackageItem(lastSelection?.packageUniqueId);
            if (packageItem == null)
                return null;

            return FindNextVisiblePackageItem(packageItem, reverseOrder);
        }

        public void OnActivePageChanged(IPage page)
        {
            // Check if groups have changed only for InProject page
            if (page.id != InProjectPage.k_Id || !m_PageRefreshHandler.IsInitialFetchingDone(page))
                return;

            var needGroupsReordering = false;
            var currentPackageItems = packageItems.ToList();
            foreach (var packageItem in currentPackageItems)
            {
                // Check if group has changed
                var package = packageItem.package;
                var groupName = page.GetGroupName(package);
                if (packageItem.packageGroup.name != groupName)
                {
                    var oldGroup = GetOrCreateGroup(packageItem.packageGroup.name);
                    var newGroup = GetOrCreateGroup(groupName);

                    var state = page.visualStates.Get(package?.uniqueId);
                    if (state != null)
                        state.groupName = groupName;

                    // Move PackageItem from old group to new group
                    oldGroup.RemovePackageItem(packageItem);
                    newGroup.AddPackageItem(packageItem);
                    needGroupsReordering = true;

                    if (!oldGroup.packageItems.Any())
                        m_ItemsList.Remove(oldGroup);
                }
            }

            if (needGroupsReordering)
                ReorderGroups(page.visualStates.orderedGroups);
        }

        private static PackageItem FindNextVisiblePackageItem(PackageItem packageItem, bool reverseOrder)
        {
            var nextVisibleItem = UIUtils.FindNextSibling(packageItem, reverseOrder, UIUtils.IsElementVisible) as PackageItem;
            if (nextVisibleItem == null)
            {
                Func<VisualElement, bool> nonEmptyGroup = (element) =>
                {
                    var group = element as PackageGroup;
                    return group.packageItems.Any(p => UIUtils.IsElementVisible(p));
                };
                var nextGroup = UIUtils.FindNextSibling(packageItem.packageGroup, reverseOrder, nonEmptyGroup) as PackageGroup;
                if (nextGroup != null)
                    nextVisibleItem = reverseOrder ? nextGroup.packageItems.LastOrDefault(p => UIUtils.IsElementVisible(p))
                        : nextGroup.packageItems.FirstOrDefault(p => UIUtils.IsElementVisible(p));
            }
            return nextVisibleItem;
        }
    }
}
