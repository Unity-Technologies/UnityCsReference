// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageList : VisualElement
    {
        private const string k_UnityPackageGroupDisplayName = "Unity Technologies";

        internal new class UxmlFactory : UxmlFactory<PackageList> {}

        private bool m_PackageListLoaded;
        public event Action onPackageListLoaded;

        private bool m_RefreshInProgress;

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        private IEnumerable<PackageItem> packageItems => packageGroups.SelectMany(group => group.packageItems);
        private IEnumerable<PackageGroup> packageGroups => itemsList.Children().Cast<PackageGroup>();

        public PackageList()
        {
            var root = Resources.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            viewDataKey = "package-list-key";
            scrollView.viewDataKey = "package-list-scrollview-key";

            SetEmptyAreaDisplay(false);
            UIUtils.SetElementDisplay(loadMoreContainer, false);

            loginButton.clickable.clicked += OnLoginClicked;
            loadMoreLabel.OnLeftClick(LoadMoreItemsClicked);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_PackageListLoaded = false;
            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            m_RefreshInProgress = false;
            focusable = true;
        }

        public void OnEnable()
        {
            PageManager.instance.onSelectionChanged += OnSelectionChanged;

            PackageDatabase.instance.onPackageOperationStart += OnPackageOperationStart;
            PackageDatabase.instance.onPackageOperationFinish += OnPackageOperationFinish;

            PackageDatabase.instance.onDownloadProgress += OnDownloadProgress;
            PackageDatabase.instance.onRefreshOperationStart += OnRefreshOperationStart;
            PackageDatabase.instance.onRefreshOperationFinish += OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange += OnVisualStateChange;
            PageManager.instance.onPageRebuild += OnPageRebuild;
            PageManager.instance.onPageUpdate += OnPageUpdate;

            ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;

            PackageFiltering.instance.onFilterTabChanged += OnFilterTabChanged;

            // manually build the items on initialization to refresh the UI
            OnPageRebuild(PageManager.instance.GetCurrentPage());
            OnSelectionChanged(PageManager.instance.GetSelectedVersion());
        }

        public void OnDisable()
        {
            PageManager.instance.onSelectionChanged -= OnSelectionChanged;

            PackageDatabase.instance.onPackageOperationStart -= OnPackageOperationStart;
            PackageDatabase.instance.onPackageOperationFinish -= OnPackageOperationFinish;

            PackageDatabase.instance.onDownloadProgress -= OnDownloadProgress;
            PackageDatabase.instance.onRefreshOperationStart -= OnRefreshOperationStart;
            PackageDatabase.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange -= OnVisualStateChange;
            PageManager.instance.onPageRebuild -= OnPageRebuild;
            PageManager.instance.onPageUpdate -= OnPageUpdate;

            ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;

            PackageFiltering.instance.onFilterTabChanged -= OnFilterTabChanged;
        }

        private PackageItem GetPackageItem(string packageUniqueId)
        {
            PackageItem result = null;
            return (string.IsNullOrEmpty(packageUniqueId) || !m_PackageItemsLookup.TryGetValue(packageUniqueId, out result)) ? null : result;
        }

        private ISelectableItem GetSelectedItem()
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;
            if (packageItem.targetVersion == selectedVersion)
                return packageItem;
            else
                return packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
        }

        public void LoadMoreItemsClicked()
        {
            UIUtils.SetElementDisplay(loadMoreContainer, false);

            PageManager.instance.LoadMore();
        }

        private void UpdateNoPackagesLabel()
        {
            if (!UIUtils.IsElementVisible(noPackagesLabel))
                return;

            if (m_RefreshInProgress && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                noPackagesLabel.text = L10n.Tr("Fetching packages...");
            else if (string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText))
                noPackagesLabel.text = L10n.Tr("There are no packages.");
            else
                noPackagesLabel.text = string.Format(L10n.Tr("No results for \"{0}\""), PackageFiltering.instance.currentSearchText);
        }

        public void ShowEmptyResults(bool value)
        {
            if (!value)
            {
                SetEmptyAreaDisplay(false);
                return;
            }

            var showLogin = !ApplicationUtil.instance.isUserLoggedIn && ApplicationUtil.instance.isUserInfoReady && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore;

            SetEmptyAreaDisplay(true);
            UIUtils.SetElementDisplay(noPackagesLabel, !showLogin);
            UIUtils.SetElementDisplay(loginContainer, showLogin);

            UpdateNoPackagesLabel();

            PageManager.instance.ClearSelection();
        }

        private void OnUserLoginStateChange(bool loggedIn)
        {
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                SetEmptyAreaDisplay(true);
                UIUtils.SetElementDisplay(loginContainer, !loggedIn);
                UIUtils.SetElementDisplay(noPackagesLabel, loggedIn);
            }
        }

        private void SetEmptyAreaDisplay(bool value)
        {
            // empty area & scroll view should never be shown at the same time
            UIUtils.SetElementDisplay(emptyArea, value);
            UIUtils.SetElementDisplay(scrollView, !value);
        }

        private void OnLoginClicked()
        {
            ApplicationUtil.instance.ShowLogin();
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            panel?.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            panel?.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnPackageOperationFinish(IPackage package)
        {
            GetPackageItem(package?.uniqueId)?.StopSpinner();
        }

        private void OnPackageOperationStart(IPackage package)
        {
            GetPackageItem(package?.uniqueId)?.StartSpinner();
        }

        private void OnRefreshOperationStart()
        {
            m_RefreshInProgress = true;
            UpdateNoPackagesLabel();
        }

        private void OnRefreshOperationFinish(PackageFilterTab tab)
        {
            m_RefreshInProgress = false;
            UpdateNoPackagesLabel();
        }

        private void OnDownloadProgress(IPackage package, DownloadProgress progress)
        {
            var item = GetPackageItem(package?.uniqueId);
            if (item != null)
            {
                if (progress.state == DownloadProgress.State.Completed || progress.state == DownloadProgress.State.Aborted || progress.state == DownloadProgress.State.Error)
                {
                    item.StopSpinner();
                    item.SetPackage(package);
                }
                else
                    item.StartSpinner();
            }
        }

        private void ScrollIfNeeded(ScrollView container = null, VisualElement target = null)
        {
            container = container ?? scrollView;
            target = target ?? GetSelectedItem()?.element;
            if (container == null || target == null)
                return;

            if (float.IsNaN(target.layout.height))
            {
                EditorApplication.delayCall += () => ScrollIfNeeded(container, target);
                return;
            }

            var scrollViews = UIUtils.GetParentsOfType<ScrollView>(target);
            foreach (var scrollview in scrollViews)
                UIUtils.ScrollIfNeeded(scrollview, target);
        }

        private void SetSelectedItemExpanded(bool value)
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            GetPackageItem(selectedVersion?.packageUniqueId)?.SetExpanded(value);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
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

        private void AddOrUpdatePackageItem(VisualState state, IPackage package = null)
        {
            package = package ?? PackageDatabase.instance.GetPackage(state?.packageUniqueId);
            if (package == null)
                return;

            var item = GetPackageItem(state.packageUniqueId);
            if (item != null)
            {
                item.SetPackage(package);

                // Check if group has changed
                var groupName = PageManager.instance.GetCurrentPage().GetGroupName(package);
                if (item.packageGroup.name != groupName)
                {
                    var oldGroup = GetOrCreateGroup(item.packageGroup.name);
                    var newGroup = GetOrCreateGroup(groupName);
                    state.groupName = groupName;

                    // Replace PackageItem
                    m_PackageItemsLookup[package.uniqueId] = newGroup.AddPackageItem(package, state);
                    oldGroup.RemovePackageItem(item);
                    if (!oldGroup.packageItems.Any())
                        itemsList.Remove(oldGroup);

                    ReorderGroups();
                }

                item.UpdateVisualState(state);
            }
            else
            {
                var groupName = PageManager.instance.GetCurrentPage().GetGroupName(package);
                if (state.groupName != groupName)
                    state.groupName = groupName;

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
            var expanded = PageManager.instance.IsGroupExpanded(groupName);
            group = new PackageGroup(groupName, GetGroupDisplayName(groupName), expanded, hidden);
            if (!hidden)
            {
                group.onGroupToggle += value =>
                {
                    if (value && group.Contains(GetSelectedItem()))
                        EditorApplication.delayCall += () => ScrollIfNeeded();
                };
            }

            itemsList.Add(group);

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

        private void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (!visualStates.Any())
                return;

            foreach (var state in visualStates)
                GetPackageItem(state.packageUniqueId)?.UpdateVisualState(state);

            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            ShowResults();
        }

        private void ReorderGroups()
        {
            itemsList.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        }

        private void OnPageRebuild(IPage page)
        {
            ClearAll();

            UIUtils.SetElementDisplay(loadMoreContainer, page.morePackagesToFetch);

            foreach (var state in page.packageVisualStates)
                AddOrUpdatePackageItem(state);

            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            m_PackageItemsLookup = packageItems.ToDictionary(item => item.package.uniqueId, item => item);

            if (!m_PackageListLoaded && page.packageVisualStates.Any())
            {
                m_PackageListLoaded = true;
                onPackageListLoaded?.Invoke();
            }

            ReorderGroups();
            ShowResults();
        }

        private void OnPageUpdate(IPage page, IEnumerable<IPackage> addedOrUpdated, IEnumerable<IPackage> removed)
        {
            foreach (var package in removed)
                RemovePackageItem(package?.uniqueId);

            UIUtils.SetElementDisplay(loadMoreContainer, page.morePackagesToFetch);

            if (addedOrUpdated.Any())
            {
                ClearAll();

                foreach (var state in page.packageVisualStates)
                    AddOrUpdatePackageItem(state);

                m_PackageItemsLookup = packageItems.ToDictionary(item => item.package.uniqueId, item => item);

                if (!m_PackageListLoaded)
                {
                    m_PackageListLoaded = true;
                    onPackageListLoaded?.Invoke();
                }

                ReorderGroups();
            }

            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            ShowResults();
        }

        internal void OnSelectionChanged(IPackageVersion newSelection)
        {
            ScrollIfNeeded();
        }

        private bool SelectNext(bool reverseOrder)
        {
            var nextElement = FindNextVisibleSelectableItem(reverseOrder);
            if (nextElement != null)
            {
                PageManager.instance.SetSelected(nextElement.package, nextElement.targetVersion);
                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.element))
                    ScrollIfNeeded(scrollView, nextElement.element);
                return true;
            }
            return false;
        }

        private ISelectableItem FindNextVisibleSelectableItem(bool reverseOrder)
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;

            // First we try to look for the next visible options within all the versions of the current package.
            // If that doesn't work we'll jump to the other packages.
            if (packageItem.targetVersion != selectedVersion)
            {
                // When the current selection is in the version list, we look in the list first
                var versionItem = packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
                var nextVersionItem = UIUtils.FindNextSibling(versionItem, reverseOrder, UIUtils.IsElementVisible) as PackageVersionItem;
                if (nextVersionItem != null)
                    return nextVersionItem;
                if (reverseOrder)
                    return packageItem;
            }
            else if (!reverseOrder && packageItem.visualState.expanded)
            {
                // If the main item is currently selected, and the version list is expanded, we look for the first item in the list when we are not in reveseOrder
                var nextVersionItem = packageItem.versionItems.FirstOrDefault();
                if (nextVersionItem != null)
                    return nextVersionItem;
            }

            var nextPackageItem = FindNextVisiblePackageItem(packageItem, reverseOrder);
            if (nextPackageItem == null)
                return null;

            if (reverseOrder && nextPackageItem.visualState.expanded)
            {
                var nextVersionItem = nextPackageItem.versionItems.LastOrDefault();
                if (nextVersionItem != null)
                    return nextVersionItem;
            }
            return nextPackageItem;
        }

        private void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            // Check if groups have changed only for Local tab
            if (filterTab == PackageFilterTab.Local && PageManager.instance.HasFetchedPageForFilterTab(PackageFilterTab.Local))
            {
                var needGroupsReordering = false;
                var currentPackageItems = packageItems.ToList();
                foreach (var packageItem in currentPackageItems)
                {
                    // Check if group has changed
                    var package = packageItem.package;
                    var groupName = PageManager.instance.GetCurrentPage().GetGroupName(package);
                    if (packageItem.packageGroup.name != groupName)
                    {
                        var oldGroup = GetOrCreateGroup(packageItem.packageGroup.name);
                        var newGroup = GetOrCreateGroup(groupName);

                        var state = PageManager.instance.GetVisualState(package);
                        if (state != null)
                            state.groupName = groupName;

                        // Move PackageItem from old group to new group
                        oldGroup.RemovePackageItem(packageItem);
                        newGroup.AddPackageItem(packageItem);
                        needGroupsReordering = true;

                        if (!oldGroup.packageItems.Any())
                            itemsList.Remove(oldGroup);
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

        private void ClearAll()
        {
            foreach (var group in packageGroups)
                group.ClearPackageItems();
            itemsList.Clear();

            m_PackageItemsLookup.Clear();
            SetEmptyAreaDisplay(false);
        }

        private void ShowResults()
        {
            var showEmptyResults = !packageItems.Where(item => item.visualState.visible).Any();
            ShowEmptyResults(showEmptyResults);

            if (!showEmptyResults)
                ScrollIfNeeded();
        }

        private VisualElementCache cache { get; set; }

        private ScrollView scrollView { get { return cache.Get<ScrollView>("scrollView"); } }
        private VisualElement itemsList { get { return cache.Get<VisualElement>("itemsList"); } }

        private VisualElement loadMoreContainer { get { return cache.Get<VisualElement>("loadMoreContainer"); } }
        private Label loadMoreLabel { get { return cache.Get<Label>("loadMore"); } }

        private VisualElement emptyArea { get { return cache.Get<VisualElement>("emptyArea"); } }
        private Label noPackagesLabel { get { return cache.Get<Label>("noPackagesLabel"); } }
        private VisualElement loginContainer { get { return cache.Get<VisualElement>("loginContainer"); } }
        private Button loginButton { get { return cache.Get<Button>("loginButton"); } }
    }
}
