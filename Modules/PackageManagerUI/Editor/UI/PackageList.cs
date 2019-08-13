// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageList> {}

        private bool m_PackageListLoaded;
        public event Action onPackageListLoaded;

        private bool m_RefreshInProgress;

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        private IEnumerable<PackageItem> packageItems
        {
            get { return itemsList.Children().Cast<PackageItem>(); }
        }

        public PackageList()
        {
            var root = Resources.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            viewDataKey = "package-list-key";
            scrollView.viewDataKey = "package-list-scrollview-key";

            UIUtils.SetElementDisplay(emptyArea, false);
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
        }

        private PackageItem GetPackageItem(IPackage package)
        {
            return GetPackageItem(package?.uniqueId);
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
                UIUtils.SetElementDisplay(emptyArea, false);
                return;
            }

            var showLogin = !ApplicationUtil.instance.isUserLoggedIn && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore;

            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, !showLogin);
            UIUtils.SetElementDisplay(loginContainer, showLogin);

            UpdateNoPackagesLabel();

            PageManager.instance.ClearSelection();
        }

        private void OnUserLoginStateChange(bool loggedIn)
        {
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                UIUtils.SetElementDisplay(emptyArea, true);
                UIUtils.SetElementDisplay(loginContainer, !loggedIn);
                UIUtils.SetElementDisplay(noPackagesLabel, loggedIn);
            }
        }

        private void OnLoginClicked()
        {
            ApplicationUtil.instance.ShowLogin();
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnPackageOperationFinish(IPackage package)
        {
            GetPackageItem(package)?.StopSpinner();
        }

        private void OnPackageOperationStart(IPackage package)
        {
            GetPackageItem(package)?.StartSpinner();
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
            var item = GetPackageItem(package);
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

            UIUtils.ScrollIfNeeded(container, target);
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
                if (SelectBy(-1))
                    evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (SelectBy(1))
                    evt.StopPropagation();
            }
        }

        private PackageItem AddOrUpdatePackageItem(IPackage package)
        {
            if (package == null)
                return null;

            var item = GetPackageItem(package);
            if (item != null)
                item.SetPackage(package);
            else
            {
                item = new PackageItem(package);
                itemsList.Add(item);
                m_PackageItemsLookup[package.uniqueId] = item;
            }
            return item;
        }

        private PackageItem RemovePackageItem(IPackage package)
        {
            var item = GetPackageItem(package);
            if (item != null)
            {
                itemsList.Remove(item);
                m_PackageItemsLookup.Remove(package.uniqueId);
            }
            return item;
        }

        private void OnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            if (!visualStates.Any())
                return;

            foreach (var state in visualStates)
                GetPackageItem(state.packageUniqueId)?.UpdateVisualState(state);

            ShowResults();
        }

        private void OnPageRebuild(IPage page)
        {
            ClearAll();

            UIUtils.SetElementDisplay(loadMoreContainer, page.morePackagesToFetch);

            foreach (var state in page.packageVisualStates)
            {
                var package = PackageDatabase.instance.GetPackage(state.packageUniqueId);
                var packageItem = AddOrUpdatePackageItem(package);
                packageItem?.UpdateVisualState(state);
            }

            if (!m_PackageListLoaded && page.packageVisualStates.Any())
            {
                m_PackageListLoaded = true;
                onPackageListLoaded?.Invoke();
            }

            ShowResults();
        }

        private void OnPageUpdate(IPage page, IEnumerable<IPackage> addedOrUpated, IEnumerable<IPackage> removed)
        {
            foreach (var package in removed)
                RemovePackageItem(package);

            UIUtils.SetElementDisplay(loadMoreContainer, page.morePackagesToFetch);

            foreach (var package in addedOrUpated)
            {
                var packageItem = AddOrUpdatePackageItem(package);
                var visualState = page.GetVisualState(package.uniqueId);
                packageItem.UpdateVisualState(visualState);
            }

            if (addedOrUpated.Any())
            {
                if (!m_PackageListLoaded)
                {
                    m_PackageListLoaded = true;
                    onPackageListLoaded?.Invoke();
                }

                // re-order if there are any added or updated items
                itemsList.Clear();
                foreach (var state in page.packageVisualStates)
                    itemsList.Add(GetPackageItem(state.packageUniqueId));
                m_PackageItemsLookup = packageItems.ToDictionary(item => item.package.uniqueId, item => item);
            }

            ShowResults();
        }

        internal void OnSelectionChanged(IPackageVersion newSelection)
        {
            ScrollIfNeeded();
        }

        public List<ISelectableItem> GetSelectableItems()
        {
            return packageItems.SelectMany(item => item.GetSelectableItems()).ToList();
        }

        private bool SelectBy(int delta)
        {
            var list = GetSelectableItems();
            var selection = GetSelectedItem();
            if (selection != null)
            {
                var index = list.IndexOf(selection);

                var direction = Math.Sign(delta);
                delta = Math.Abs(delta);
                var nextIndex = index;
                var numVisibleElement = 0;
                ISelectableItem nextElement = null;
                while (numVisibleElement < delta)
                {
                    nextIndex += direction;
                    if (nextIndex >= list.Count)
                        return false;
                    if (nextIndex < 0)
                        return false;
                    nextElement = list.ElementAt(nextIndex);
                    if (UIUtils.IsElementVisible(nextElement.element))
                        ++numVisibleElement;
                }

                PageManager.instance.SetSelected(nextElement.package, nextElement.targetVersion);

                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.element))
                    ScrollIfNeeded(scrollView, nextElement.element);
            }

            return true;
        }

        private void ClearAll()
        {
            itemsList.Clear();
            m_PackageItemsLookup.Clear();

            UIUtils.SetElementDisplay(emptyArea, false);
        }

        private void ShowResults()
        {
            var visiblePackageItems = packageItems.Where(UIUtils.IsElementVisible);
            var showEmptyResults = !visiblePackageItems.Any();
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
