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
    internal class PackageList : VisualElement
    {
        private const string k_UnityPackageGroupDisplayName = "Unity Technologies";
        private const double k_DelayBeforeCheck = 0.5;

        internal new class UxmlFactory : UxmlFactory<PackageList> {}

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        private ResourceLoader m_ResourceLoader;
        private UnityConnectProxy m_UnityConnect;
        private PackageFiltering m_PackageFiltering;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private ApplicationProxy m_ApplicationProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PackageFiltering = container.Resolve<PackageFiltering>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_ApplicationProxy = container.Resolve<ApplicationProxy>();
        }

        internal IEnumerable<PackageItem> packageItems => packageGroups.SelectMany(group => group.packageItems);
        internal IEnumerable<PackageGroup> packageGroups => itemsList.Children().OfType<PackageGroup>();

        [NonSerialized]
        private double m_Timestamp;

        [NonSerialized]
        private float m_LastVerticalScrollerValue = float.NegativeInfinity;

        [NonSerialized]
        private Vector2 m_LastScrollViewSize;

        public PackageList()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            viewDataKey = "package-list-key";
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.viewDataKey = "package-list-scrollview-key";
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            loginButton.clickable.clicked += OnLoginClicked;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            focusable = true;
        }

        public void OnEnable()
        {
            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_PageManager.onRefreshOperationStart += OnRefreshOperationStart;
            m_PageManager.onRefreshOperationFinish += OnRefreshOperationFinish;

            m_PageManager.onVisualStateChange += OnVisualStateChange;
            m_PageManager.onListRebuild += OnListRebuild;
            m_PageManager.onListUpdate += OnListUpdate;

            m_PageManager.onPackageRefreshed += OnPackageRefreshed;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;

            m_SettingsProxy.onSeeAllVersionsChanged += OnSeeAllPackageVersionsChanged;

            m_PackageFiltering.onFilterTabChanged += OnFilterTabChanged;

            var isDeveloperBuild = Unsupported.IsDeveloperBuild();
            if (!isDeveloperBuild)
            {
                if (isDeveloperBuild != m_SettingsProxy.seeAllPackageVersions)
                {
                    m_SettingsProxy.seeAllPackageVersions = isDeveloperBuild;
                    m_SettingsProxy.Save();
                }
            }

            RegisterScrollEvents();

            // manually build the items on initialization to refresh the UI
            OnListRebuild(m_PageManager.GetCurrentPage());
        }

        public void OnDisable()
        {
            m_PackageDatabase.onPackageProgressUpdate -= OnPackageProgressUpdate;

            m_PageManager.onRefreshOperationStart -= OnRefreshOperationStart;
            m_PageManager.onRefreshOperationFinish -= OnRefreshOperationFinish;

            m_PageManager.onVisualStateChange -= OnVisualStateChange;
            m_PageManager.onListRebuild -= OnListRebuild;
            m_PageManager.onListUpdate -= OnListUpdate;

            m_PageManager.onPackageRefreshed -= OnPackageRefreshed;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;

            m_SettingsProxy.onSeeAllVersionsChanged -= OnSeeAllPackageVersionsChanged;
            m_PackageFiltering.onFilterTabChanged -= OnFilterTabChanged;

            UnregisterScrollEvents();
        }

        private void RegisterScrollEvents()
        {
            scrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
            scrollView.verticalScroller.valueChanged += OnScrollViewVerticalScrollerValueChanged;
            scrollView.verticalScroller.slider.visualInput.RegisterCallback<MouseUpEvent>(OnScrollViewVerticalScrollerMouseUp);
        }

        private void UnregisterScrollEvents()
        {
            scrollView.UnregisterCallback<GeometryChangedEvent>(OnScrollViewGeometryChanged);
            scrollView.verticalScroller.valueChanged -= OnScrollViewVerticalScrollerValueChanged;
            scrollView.verticalScroller.slider.visualInput.UnregisterCallback<MouseUpEvent>(OnScrollViewVerticalScrollerMouseUp);
        }

        private void OnPackageRefreshed(IPackage package)
        {
            GetPackageItem(package?.uniqueId)?.RefreshState();
        }

        private void OnScrollViewVerticalScrollerValueChanged(float value)
        {
            if (Mathf.Abs(m_LastVerticalScrollerValue - value) < PackageItem.k_MainItemHeight / 2.0f)
                return;

            m_LastVerticalScrollerValue = value;
            ForceDisplayOfVisibleItems();

            UpdateItemsVisibleInScrollView();

            if (m_PackageFiltering.currentFilterTab != PackageFilterTab.AssetStore)
                return;

            m_Timestamp = EditorApplication.timeSinceStartup;
            EditorApplication.update -= DelayedCheckPackageItems;
            EditorApplication.update += DelayedCheckPackageItems;
        }

        private void ForceDisplayOfVisibleItems()
        {
            // Make sure all item are displayed properly
            var scrollViewWorldBound = scrollView.worldBound;
            foreach (var packageGroup in packageGroups)
            {
                var packageGroupWorldBound = packageGroup.worldBound;
                if (packageGroupWorldBound.yMax < scrollViewWorldBound.yMin)
                    continue;
                if (packageGroupWorldBound.yMin > scrollViewWorldBound.yMax)
                    break;

                foreach (var item in packageGroup.packageItems)
                {
                    // Make sure selected item is shown
                    if (!string.IsNullOrEmpty(item.visualState?.selectedVersionId))
                    {
                        item.IncrementVersion(VersionChangeType.Transform);
                        continue;
                    }

                    var itemWorldBound = item.worldBound;
                    if (itemWorldBound.yMax <= scrollViewWorldBound.yMin)
                        continue;
                    if (itemWorldBound.yMin >= scrollViewWorldBound.yMax)
                        break;

                    item.IncrementVersion(VersionChangeType.Transform);
                }
            }
        }

        private void OnScrollViewGeometryChanged(GeometryChangedEvent evt)
        {
            if (Mathf.Abs(evt.oldRect.height - evt.newRect.height) < PackageItem.k_MainItemHeight / 2.0f)
                return;

            if (evt.newRect.height - evt.oldRect.height > PackageItem.k_MainItemHeight / 2.0f ||
                Mathf.Abs(evt.newRect.width - evt.oldRect.width) > 5.0f)
            {
                var yMin = scrollView.worldBound.yMin;
                var yMax = scrollView.worldBound.yMax;
                foreach (var item in packageItems)
                {
                    if (item.worldBound.yMax < yMin)
                        continue;
                    if (item.worldBound.yMin > yMax)
                        break;

                    item.visibleInScrollView = true;
                }
            }

            if (Mathf.Abs(m_LastScrollViewSize.y - evt.newRect.height) < PackageItem.k_MainItemHeight / 2.0f)
                return;

            m_LastScrollViewSize.y = evt.newRect.height;

            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore)
            {
                m_Timestamp = EditorApplication.timeSinceStartup;
                EditorApplication.update -= DelayedCheckPackageItems;
                EditorApplication.update += DelayedCheckPackageItems;
            }
        }

        private void OnScrollViewVerticalScrollerMouseUp(MouseUpEvent evt)
        {
            if (m_PackageFiltering.currentFilterTab != PackageFilterTab.AssetStore)
                return;

            m_Timestamp = EditorApplication.timeSinceStartup;
            EditorApplication.update -= DelayedCheckPackageItems;
            EditorApplication.update += DelayedCheckPackageItems;
        }

        private void DelayedCheckPackageItems()
        {
            if (EditorApplication.timeSinceStartup - m_Timestamp <= k_DelayBeforeCheck)
                return;

            EditorApplication.update -= DelayedCheckPackageItems;
            CheckPackageItems();
        }

        private void CheckPackageItems()
        {
            var fetchDetailsQueue = new List<string>();

            var scrollViewWorldBound = scrollView.worldBound;
            foreach (var packageGroup in packageGroups)
            {
                var packageGroupWorldBound = packageGroup.worldBound;
                if (packageGroupWorldBound.yMax < scrollViewWorldBound.yMin)
                    continue;
                if (packageGroupWorldBound.yMin > scrollViewWorldBound.yMax)
                    break;

                foreach (var item in packageGroup.packageItems)
                {
                    // Make sure selected item becomes visible
                    if (!string.IsNullOrEmpty(item.visualState?.selectedVersionId))
                    {
                        if (item.package is PlaceholderPackage)
                            fetchDetailsQueue.Add(item.package.uniqueId);

                        continue;
                    }

                    var itemWorldBound = item.worldBound;
                    if (itemWorldBound.yMax <= scrollViewWorldBound.yMin)
                        continue;

                    if (itemWorldBound.yMin >= scrollViewWorldBound.yMax)
                        break;

                    if (item.package is PlaceholderPackage)
                        fetchDetailsQueue.Add(item.package.uniqueId);
                }
            }

            m_PageManager.SetFetchDetailsQueue(fetchDetailsQueue);
        }

        private PackageItem GetPackageItem(string packageUniqueId)
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

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore)
                RefreshList(false);
        }

        private void ShowPackages(bool updateScrollPosition)
        {
            UIUtils.SetElementDisplay(scrollView, true);
            UIUtils.SetElementDisplay(emptyArea, false);

            var page = m_PageManager.GetCurrentPage();
            var selectedVersion = page.GetSelectedVersion();
            var selectedVisualState = selectedVersion != null ? page.GetVisualState(selectedVersion.packageUniqueId) : null;
            if (selectedVisualState?.visible != true)
            {
                var firstVisible = page.visualStates.FirstOrDefault(v => v.visible);
                if (firstVisible != null)
                {
                    IPackage package;
                    IPackageVersion version;
                    m_PackageDatabase.GetPackageAndVersion(firstVisible.packageUniqueId, firstVisible.selectedVersionId, out package, out version);
                    m_PageManager.SetSelected(package, version);
                }
                else
                    m_PageManager.ClearSelection();
            }

            if (updateScrollPosition)
                ScrollIfNeeded();
        }

        private void HidePackagesShowLogin()
        {
            UIUtils.SetElementDisplay(scrollView, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, false);
            // when the editor first starts, we detect the user as not logged in (even though they are) because userInfo is not ready yet
            // in this case, we want to delay showing the login window until the userInfo is ready
            UIUtils.SetElementDisplay(loginContainer, m_UnityConnect.isUserInfoReady);

            m_PageManager.ClearSelection();
        }

        public void HidePackagesShowMessage(bool isRefreshInProgress, bool isInitialFetchingDone, string messageWhenInitialFetchNotDone = "")
        {
            UIUtils.SetElementDisplay(scrollView, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, true);
            UIUtils.SetElementDisplay(loginContainer, false);

            if (isRefreshInProgress)
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = L10n.Tr("Fetching packages...");
                else
                    noPackagesLabel.text = L10n.Tr("Refreshing packages...");
            }
            else if (string.IsNullOrEmpty(m_PackageFiltering.currentSearchText))
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = messageWhenInitialFetchNotDone;
                else
                    noPackagesLabel.text = L10n.Tr("There are no packages.");
            }
            else
            {
                const int maxSearchTextToDisplay = 64;
                var searchText = m_PackageFiltering.currentSearchText;
                if (searchText?.Length > maxSearchTextToDisplay)
                    searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                noPackagesLabel.text = string.Format(L10n.Tr("No results for \"{0}\""), searchText);
            }

            m_PageManager.ClearSelection();
        }

        private void RefreshList(bool updateScrollPosition)
        {
            if (m_PackageFiltering.currentFilterTab == PackageFilterTab.AssetStore && !m_UnityConnect.isUserLoggedIn)
            {
                HidePackagesShowLogin();
                return;
            }

            var page = m_PageManager.GetCurrentPage();
            var isListEmpty = !page.visualStates.Any(v => v.visible);
            var isInitialFetchingDone = m_PageManager.IsInitialFetchingDone();
            if (isListEmpty || !isInitialFetchingDone)
            {
                HidePackagesShowMessage(m_PageManager.IsRefreshInProgress(), isInitialFetchingDone);
                return;
            }

            ReorderGroups();
            ShowPackages(updateScrollPosition);
        }

        private void OnLoginClicked()
        {
            m_UnityConnect.ShowLogin();
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            if (panel != null)
                panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (panel != null)
                panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            float containerHeight = resolvedStyle.height;
            if (!float.IsNaN(containerHeight))
                m_PackageManagerPrefs.numItemsPerPage = (int)(containerHeight / PackageItem.k_MainItemHeight);
        }

        private void OnPackageProgressUpdate(IPackage package)
        {
            GetPackageItem(package?.uniqueId)?.RefreshState();
        }

        private void OnRefreshOperationStart()
        {
            RefreshList(false);
        }

        private void OnRefreshOperationFinish()
        {
            RefreshList(false);
        }

        internal void OnFocus()
        {
            ScrollIfNeeded();
        }

        private void ScrollIfNeeded(ScrollView container = null, VisualElement target = null)
        {
            container = container ?? scrollView;
            target = target ?? GetSelectedItem()?.element;

            if (container == null || target == null)
                return;

            if (float.IsNaN(scrollView.layout.height) || scrollView.layout.height == 0 || float.IsNaN(target.layout.height))
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
            var selectedVersion = m_PageManager.GetSelectedVersion();
            GetPackageItem(selectedVersion?.packageUniqueId)?.SetExpanded(value);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!UIUtils.IsElementVisible(scrollView))
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
                        itemsList.Remove(oldGroup);

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

            if (groupName == PageManager.k_OtherPackageGroupName)
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

            RefreshList(true);
        }

        private void ReorderGroups()
        {
            itemsList.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        }

        private void OnListRebuild(IPage page)
        {
            UnregisterScrollEvents();

            itemsList.Clear();
            m_PackageItemsLookup.Clear();

            foreach (var visualState in page.visualStates)
                AddOrUpdatePackageItem(visualState);

            foreach (var group in packageGroups)
                group.RefreshHeaderVisibility();

            RefreshList(true);

            RegisterScrollEvents();
        }

        private void OnListUpdate(IPage page, IEnumerable<IPackage> addedOrUpdated, IEnumerable<IPackage> removed, bool reorder)
        {
            UnregisterScrollEvents();

            addedOrUpdated = addedOrUpdated ?? Enumerable.Empty<IPackage>();
            removed = removed ?? Enumerable.Empty<IPackage>();

            var numItems = m_PackageItemsLookup.Count;
            foreach (var package in removed)
                RemovePackageItem(package?.uniqueId);

            var itemsRemoved = numItems != m_PackageItemsLookup.Count;
            numItems = m_PackageItemsLookup.Count;

            foreach (var package in addedOrUpdated)
            {
                var visualState = page.GetVisualState(package.uniqueId) ?? new VisualState(package.uniqueId, string.Empty);
                AddOrUpdatePackageItem(visualState, package);
            }
            var itemsAdded = numItems != m_PackageItemsLookup.Count;

            if (reorder)
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
                foreach (var group in packageGroups)
                    group.RefreshHeaderVisibility();

                RefreshList(true);
            }
            else
            {
                ReorderGroups();
            }

            RegisterScrollEvents();
        }

        internal bool SelectNext(bool reverseOrder)
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

        private void OnFilterTabChanged(PackageFilterTab filterTab)
        {
            if (m_PackageFiltering.previousFilterTab == PackageFilterTab.AssetStore)
                m_PageManager.CancelRefresh();

            UpdateItemsVisibleInScrollView();

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

        private void OnSeeAllPackageVersionsChanged(bool value)
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

        private void UpdateItemsVisibleInScrollView()
        {
            var bound = scrollView.worldBound;
            foreach (var item in packageItems)
                item.visibleInScrollView = bound.Contains(item.worldBound.min) || bound.Contains(item.worldBound.max);
        }

        private VisualElementCache cache { get; set; }

        private ScrollView scrollView { get { return cache.Get<ScrollView>("scrollView"); } }
        private VisualElement itemsList { get { return cache.Get<VisualElement>("itemsList"); } }
        private VisualElement emptyArea { get { return cache.Get<VisualElement>("emptyArea"); } }
        private Label noPackagesLabel { get { return cache.Get<Label>("noPackagesLabel"); } }
        private VisualElement loginContainer { get { return cache.Get<VisualElement>("loginContainer"); } }
        private Button loginButton { get { return cache.Get<Button>("loginButton"); } }
    }
}
