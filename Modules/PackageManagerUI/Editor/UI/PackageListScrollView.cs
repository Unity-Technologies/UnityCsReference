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
        private const string k_UnityPackageGroupDisplayName = "Unity Technologies";

        internal new class UxmlFactory : UxmlFactory<PackageListScrollView> {}

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        private ResourceLoader m_ResourceLoader;
        private PackageFiltering m_PackageFiltering;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
        }

        internal IEnumerable<PackageItem> packageItems => packageGroups.SelectMany(group => group.packageItems);
        internal IEnumerable<PackageGroup> packageGroups => m_ItemsList.Children().OfType<PackageGroup>();

        private VisualElement m_ItemsList;

        public PackageListScrollView()
        {
            ResolveDependencies();

            m_ItemsList = new VisualElement();
            Add(m_ItemsList);

            viewDataKey = "package-list-scrollview-key";
            horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            focusable = true;
        }

        public PackageItem GetPackageItem(string packageUniqueId)
        {
            return string.IsNullOrEmpty(packageUniqueId) ? null : m_PackageItemsLookup.Get(packageUniqueId);
        }

        private ISelectableItem GetSelectedItem()
        {
            var selectedVersion = m_PageManager.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;

            if (!packageItem.visualState.expanded)
                return packageItem;

            return packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
        }

        public void ScrollToSelection()
        {
            ScrollIfNeeded(this, GetSelectedItem()?.element);
        }

        private void ScrollIfNeeded(ScrollView container, VisualElement target)
        {
            if (container == null || target == null)
                return;

            if (float.IsNaN(layout.height) || layout.height == 0 || float.IsNaN(target.layout.height))
            {
                EditorApplication.delayCall += () => ScrollIfNeeded(container, target);
                return;
            }

            var scrollViews = UIUtils.GetParentsOfType<ScrollView>(target);
            foreach (var scrollview in scrollViews)
                UIUtils.ScrollIfNeeded(scrollview, target);
        }

        public void SetSelectedItemExpanded(bool value)
        {
            var selectedVersion = m_PageManager.GetSelectedVersion();
            GetPackageItem(selectedVersion?.packageUniqueId)?.SetExpanded(value);
        }

        private void AddOrUpdatePackageItem(VisualState state, IPackage package = null)
        {
            package = package ?? m_PackageDatabase.GetPackage(state?.packageUniqueId);
            if (package == null)
                return;

            var item = GetPackageItem(state.packageUniqueId);
            if (item != null)
            {
                item.SetPackage(package);

                // Check if group has changed
                var groupName = m_PageManager.GetCurrentPage().GetGroupName(package);
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

                    ReorderGroups();
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

        internal static string GetGroupDisplayName(string groupName)
        {
            if (groupName == PageManager.k_UnityPackageGroupName)
                return k_UnityPackageGroupDisplayName;

            if (groupName == PageManager.k_OtherPackageGroupName || groupName == PageManager.k_CustomPackageGroupName)
                return L10n.Tr(groupName);

            return groupName;
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            var group = packageGroups.FirstOrDefault(g => string.Compare(g.name, groupName, StringComparison.InvariantCultureIgnoreCase) == 0);
            if (group != null)
                return group;

            var hidden = string.IsNullOrEmpty(groupName);
            var expanded = m_PageManager.IsGroupExpanded(groupName);
            group = new PackageGroup(m_ResourceLoader, m_PageManager, m_SettingsProxy, groupName, GetGroupDisplayName(groupName), expanded, hidden);
            if (!hidden)
            {
                group.onGroupToggle += value =>
                {
                    var s = GetSelectedItem();
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

            ReorderGroups();
            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            if (m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid())
                ScrollToSelection();
        }

        private void ReorderGroups()
        {
            m_ItemsList.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        }

        public void OnListRebuild(IPage page)
        {
            m_ItemsList.Clear();
            m_PackageItemsLookup.Clear();

            foreach (var visualState in page.visualStates)
                AddOrUpdatePackageItem(visualState);

            ReorderGroups();
            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid();
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
                var visualState = page.GetVisualState(package.uniqueId) ?? new VisualState(package.uniqueId, string.Empty);
                AddOrUpdatePackageItem(visualState, package);
            }
            var itemsAdded = numItems != m_PackageItemsLookup.Count;

            if (args.reorder)
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

            if (itemsRemoved || itemsAdded)
            {
                ReorderGroups();
                foreach (var group in packageGroups)
                    group.RefreshHeaderVisibility();

                if (m_PageManager.UpdateSelectionIfCurrentSelectionIsInvalid())
                    ScrollToSelection();
            }
            else
            {
                ReorderGroups();
            }
        }

        public void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(this))
                return;

            if (evt.keyCode == KeyCode.RightArrow)
            {
                SetSelectedItemExpanded(true);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.LeftArrow)
            {
                SetSelectedItemExpanded(false);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                if (SelectNext(true))
                    evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (SelectNext(false))
                    evt.StopPropagation();
            }
        }

        public bool SelectNext(bool reverseOrder)
        {
            var nextElement = FindNextVisibleSelectableItem(reverseOrder);
            if (nextElement != null)
            {
                m_PageManager.SetSelected(nextElement.package, nextElement.targetVersion);
                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.element))
                    ScrollIfNeeded(scrollView, nextElement.element);
                return true;
            }
            return false;
        }

        private ISelectableItem FindNextVisibleSelectableItem(bool reverseOrder)
        {
            var selectedVersion = m_PageManager.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;

            // First we try to look for the next visible options within all the versions of the current package when the list is expanded
            if (packageItem.visualState.expanded)
            {
                var versionItem = packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
                var nextVersionItem = UIUtils.FindNextSibling(versionItem, reverseOrder, UIUtils.IsElementVisible) as PackageVersionItem;
                if (nextVersionItem != null)
                    return nextVersionItem;
            }

            var nextPackageItem = FindNextVisiblePackageItem(packageItem, reverseOrder);
            if (nextPackageItem == null)
                return null;

            if (nextPackageItem.visualState.expanded)
            {
                var nextVersionItem = reverseOrder ? nextPackageItem.versionItems.LastOrDefault() : nextPackageItem.versionItems.FirstOrDefault();
                if (nextVersionItem != null)
                    return nextVersionItem;
            }
            return nextPackageItem;
        }

        public void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            // Check if groups have changed only for InProject tab
            if (filterTab == PackageFilterTab.InProject && m_PageManager.IsInitialFetchingDone(PackageFilterTab.InProject))
            {
                var needGroupsReordering = false;
                var currentPackageItems = packageItems.ToList();
                foreach (var packageItem in currentPackageItems)
                {
                    // Check if group has changed
                    var package = packageItem.package;
                    var groupName = m_PageManager.GetCurrentPage().GetGroupName(package);
                    if (packageItem.packageGroup.name != groupName)
                    {
                        var oldGroup = GetOrCreateGroup(packageItem.packageGroup.name);
                        var newGroup = GetOrCreateGroup(groupName);

                        var state = m_PageManager.GetVisualState(package);
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
                    ReorderGroups();
            }
        }

        private static PackageItem FindNextVisiblePackageItem(PackageItem packageItem, bool reverseOrder)
        {
            PackageItem nextVisibleItem = null;
            if (packageItem.packageGroup.expanded)
                nextVisibleItem = UIUtils.FindNextSibling(packageItem, reverseOrder, UIUtils.IsElementVisible) as PackageItem;

            if (nextVisibleItem == null)
            {
                Func<VisualElement, bool> expandedNonEmptyGroup = (element) =>
                {
                    var group = element as PackageGroup;
                    return group.expanded && group.packageItems.Any(p => UIUtils.IsElementVisible(p));
                };
                var nextGroup = UIUtils.FindNextSibling(packageItem.packageGroup, reverseOrder, expandedNonEmptyGroup) as PackageGroup;
                if (nextGroup != null)
                    nextVisibleItem = reverseOrder ? nextGroup.packageItems.LastOrDefault(p => UIUtils.IsElementVisible(p))
                        : nextGroup.packageItems.FirstOrDefault(p => UIUtils.IsElementVisible(p));
            }
            return nextVisibleItem;
        }

        public void OnSeeAllPackageVersionsChanged(bool value)
        {
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.BuiltIn || m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore)
                return;
            var page = m_PageManager.GetPage(m_PackageFiltering.currentFilterTab);
            page.Rebuild();

            var openedItems = packageItems.Where((p => p.visualState.expanded)).ToList();
            if (!value && openedItems.Count() > 0)
            {
                foreach (var packageItem in openedItems)
                {
                    packageItem.visualState.expanded = false;
                    packageItem.UpdateVisualState(null);
                }

                var packageToSelect = openedItems.FirstOrDefault(p => !string.IsNullOrEmpty(p.visualState.selectedVersionId));
                if (packageToSelect != null)
                {
                    PackageManagerWindow.SelectPackageAndFilterStatic(packageToSelect.package.uniqueId);
                    packageToSelect.visualState.expanded = false;
                    packageToSelect.UpdateVisualState(null);
                }
            }
        }
    }
}
