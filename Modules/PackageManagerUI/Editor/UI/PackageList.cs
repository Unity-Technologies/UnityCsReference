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

            SetEmptyAreaDisplay(false);

            loginButton.clickable.clicked += OnLoginClicked;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_PackageListLoaded = false;
            m_PackageItemsLookup = new Dictionary<string, PackageItem>();

            m_RefreshInProgress = false;
            focusable = true;
        }

        public void OnEnable()
        {
            PackageDatabase.instance.onPackageProgressUpdate += OnPackageProgressUpdate;

            PageManager.instance.onRefreshOperationStart += OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationFinish += OnRefreshOperationFinish;

            PageManager.instance.onVisualStateChange += OnVisualStateChange;
            PageManager.instance.onPageRebuild += OnPageRebuild;
            PageManager.instance.onPageUpdate += OnPageUpdate;

            ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;

            // manually build the items on initialization to refresh the UI
            OnPageRebuild(PageManager.instance.GetCurrentPage());
        }

        public void OnDisable()
        {
            PackageDatabase.instance.onPackageProgressUpdate -= OnPackageProgressUpdate;

            PageManager.instance.onRefreshOperationStart -= OnRefreshOperationStart;
            PageManager.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;

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

            var showLogin = !ApplicationUtil.instance.isUserLoggedIn && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore;

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
            m_RefreshInProgress = true;
            UpdateNoPackagesLabel();
        }

        private void OnRefreshOperationFinish()
        {
            m_RefreshInProgress = false;
            UpdateNoPackagesLabel();
        }

        internal void OnFocus()
        {
            ScrollIfNeeded();
            UpdateActiveSelection();
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

            SetEmptyAreaDisplay(false);
        }

        private void ShowResults()
        {
            var visiblePackageItems = packageItems.Where(UIUtils.IsElementVisible);
            var showEmptyResults = !visiblePackageItems.Any();
            ShowEmptyResults(showEmptyResults);
            UpdateActiveSelection();
        }

        private void UpdateActiveSelection()
        {
            IPackage package;
            IPackageVersion version;
            PageManager.instance.GetSelectedPackageAndVersion(out package, out version);

            var packageSelectionObject = PageManager.instance.CreatePackageSelectionObject(package, version);
            if (packageSelectionObject != null && Selection.activeObject != packageSelectionObject)
                Selection.activeObject = packageSelectionObject;
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
