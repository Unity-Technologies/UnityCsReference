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

        private Dictionary<string, PackageItem> m_PackageItemsLookup;

        internal IEnumerable<PackageItem> packageItems
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

            HidePackagesShowMessage(false, false);

            loginButton.clickable.clicked += OnLoginClicked;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            focusable = true;
        }

        public void OnEnable()
        {
            PackageDatabase.instance.onPackageProgressUpdate += OnPackageProgressUpdate;

            PageManager.instance.onRefreshOperationStart += OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationFinish += OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange += OnVisualStateChange;
            PageManager.instance.onListRebuild += OnListRebuild;
            PageManager.instance.onListUpdate += OnListUpdate;

            ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;

            // manually build the items on initialization to refresh the UI
            OnListRebuild(PageManager.instance.GetCurrentPage());
        }

        public void OnDisable()
        {
            PackageDatabase.instance.onPackageProgressUpdate -= OnPackageProgressUpdate;

            PageManager.instance.onRefreshOperationStart -= OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange -= OnVisualStateChange;
            PageManager.instance.onListRebuild -= OnListRebuild;
            PageManager.instance.onListUpdate -= OnListUpdate;

            ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        private PackageItem GetPackageItem(IPackage package)
        {
            return GetPackageItem(package?.uniqueId);
        }

        private PackageItem GetPackageItem(string packageUniqueId)
        {
            return string.IsNullOrEmpty(packageUniqueId) ? null : m_PackageItemsLookup.Get(packageUniqueId);
        }

        private ISelectableItem GetSelectedItem()
        {
            var selectedVersion = PageManager.instance.GetSelectedVersion();
            var packageItem = GetPackageItem(selectedVersion?.packageUniqueId);
            if (packageItem == null)
                return null;

            if (packageItem.targetVersion == selectedVersion)
                return packageItem;

            return packageItem.versionItems.FirstOrDefault(v => v.targetVersion == selectedVersion);
        }

        private void OnUserLoginStateChange(bool loggedIn)
        {
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
                RefreshList(false);
        }

        private void ShowPackages(bool updateScrollPosition)
        {
            UIUtils.SetElementDisplay(scrollView, true);
            UIUtils.SetElementDisplay(emptyArea, false);

            var page = PageManager.instance.GetCurrentPage();
            var selectedVersion = page.GetSelectedVersion();
            var selectedVisualState = selectedVersion != null ? page.GetVisualState(selectedVersion.packageUniqueId) : null;
            if (selectedVisualState?.visible != true)
            {
                var firstVisible = page.visualStates.FirstOrDefault(v => v.visible);
                if (firstVisible != null)
                {
                    IPackage package;
                    IPackageVersion version;
                    PackageDatabase.instance.GetPackageAndVersion(firstVisible.packageUniqueId, firstVisible.selectedVersionId, out package, out version);
                    PageManager.instance.SetSelected(package, version);
                }
                else
                    PageManager.instance.ClearSelection();
            }

            if (updateScrollPosition)
                ScrollIfNeeded();
        }

        private void HidePackagesShowLogin()
        {
            UIUtils.SetElementDisplay(scrollView, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, false);
            UIUtils.SetElementDisplay(loginContainer, true);

            PageManager.instance.ClearSelection();
        }

        private void HidePackagesShowMessage(bool isRefreshInProgress, bool isInitialFetchingDone)
        {
            UIUtils.SetElementDisplay(scrollView, false);
            UIUtils.SetElementDisplay(emptyArea, true);
            UIUtils.SetElementDisplay(noPackagesLabel, true);
            UIUtils.SetElementDisplay(loginContainer, false);

            if (isRefreshInProgress)
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = ApplicationUtil.instance.GetTranslationForText("Fetching packages...");
                else
                    noPackagesLabel.text = ApplicationUtil.instance.GetTranslationForText("Refreshing packages...");
            }
            else if (string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText))
            {
                if (!isInitialFetchingDone)
                    noPackagesLabel.text = string.Empty;
                else
                    noPackagesLabel.text = ApplicationUtil.instance.GetTranslationForText("There are no packages.");
            }
            else
            {
                const int maxSearchTextToDisplay = 64;
                var searchText = PackageFiltering.instance.currentSearchText;
                if (searchText?.Length > maxSearchTextToDisplay)
                    searchText = searchText.Substring(0, maxSearchTextToDisplay) + "...";
                noPackagesLabel.text = string.Format(ApplicationUtil.instance.GetTranslationForText("No results for \"{0}\""), searchText);
            }

            PageManager.instance.ClearSelection();
        }

        private void RefreshList(bool updateScrollPosition)
        {
            var isAssetStore = PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore;
            var isLoggedIn = ApplicationUtil.instance.isUserLoggedIn;

            if (isAssetStore && !isLoggedIn)
            {
                HidePackagesShowLogin();
                return;
            }

            var page = PageManager.instance.GetCurrentPage();
            var isListEmpty = !page.visualStates.Any(v => v.visible);
            var isInitialFetchingDone = PageManager.instance.IsInitialFetchingDone();

            if (isListEmpty || !isInitialFetchingDone)
            {
                HidePackagesShowMessage(PageManager.instance.IsRefreshInProgress(), isInitialFetchingDone);
                return;
            }

            ShowPackages(updateScrollPosition);
        }

        private void OnLoginClicked()
        {
            ApplicationUtil.instance.ShowLogin();
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

        private void OnPackageProgressUpdate(IPackage package)
        {
            GetPackageItem(package)?.UpdateStatusIcon();
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

            RefreshList(true);
        }

        private void OnListRebuild(IPage page)
        {
            itemsList.Clear();
            m_PackageItemsLookup.Clear();

            foreach (var visualState in page.visualStates)
            {
                var package = PackageDatabase.instance.GetPackage(visualState.packageUniqueId);
                var packageItem = AddOrUpdatePackageItem(package);
                packageItem?.UpdateVisualState(visualState);
            }

            RefreshList(true);
        }

        private void OnListUpdate(IPage page, IEnumerable<IPackage> addedOrUpated, IEnumerable<IPackage> removed, bool reorder)
        {
            addedOrUpated = addedOrUpated ?? Enumerable.Empty<IPackage>();
            removed = removed ?? Enumerable.Empty<IPackage>();

            var numItems = m_PackageItemsLookup.Count;
            foreach (var package in removed)
                RemovePackageItem(package);

            var itemsRemoved = numItems != m_PackageItemsLookup.Count;
            numItems = m_PackageItemsLookup.Count;

            foreach (var package in addedOrUpated)
            {
                var packageItem = AddOrUpdatePackageItem(package);
                var visualState = page.GetVisualState(package.uniqueId);
                packageItem.UpdateVisualState(visualState);
            }
            var itemsAdded = numItems != m_PackageItemsLookup.Count;

            if (reorder)
            {
                // re-order if there are any added or updated items
                itemsList.Clear();
                foreach (var state in page.visualStates)
                    itemsList.Add(GetPackageItem(state.packageUniqueId));
                m_PackageItemsLookup = packageItems.ToDictionary(item => item.package.uniqueId, item => item);
            }

            if (itemsRemoved || itemsAdded)
                RefreshList(true);
        }

        public List<ISelectableItem> GetSelectableItems()
        {
            return packageItems.SelectMany(item => item.GetSelectableItems()).ToList();
        }

        internal bool SelectBy(int delta)
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

                PageManager.instance.SetSelected(nextElement.package, nextElement.targetVersion, true);

                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.element))
                    ScrollIfNeeded(scrollView, nextElement.element);
            }

            return true;
        }

        internal int CalculateNumberOfPackagesToDisplay()
        {
            return ApplicationUtil.instance.CalculateNumberOfElementsInsideContainerToDisplay(this, 24);
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
