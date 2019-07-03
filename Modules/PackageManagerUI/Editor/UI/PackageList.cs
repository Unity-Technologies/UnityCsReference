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

        private bool m_PackagesLoaded;
        public event Action onPackagesLoaded = delegate {};

        public PackageItem selectedItem
        {
            get
            {
                var selectedElement = GetSelectedItem()?.element;
                return selectedElement == null ? null : UIUtils.GetParentOfType<PackageItem>(selectedElement);
            }
        }

        public IEnumerable<PackageItem> packageItems
        {
            get { return list.Children().Cast<PackageItem>(); }
        }

        public PackageList()
        {
            var root = Resources.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            cache = new VisualElementCache(root);

            viewDataKey = "package-list-key";
            list.viewDataKey = "package-list-scrollview-key";

            UIUtils.SetElementDisplay(emptyArea, false);
            UIUtils.SetElementDisplay(noResult, false);
            UIUtils.SetElementDisplay(mustLogin, false);

            emptyAreaText.text = L10n.Tr("There are no packages.");

            loginBtn.clickable.clicked += OnLoginClicked;

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_PackagesLoaded = false;
        }

        public void Setup()
        {
            SelectionManager.instance.onSelectionChanged += OnSelectionChanged;

            PackageFiltering.instance.onFilterTabChanged += OnFilterChanged;
            PackageFiltering.instance.onSearchTextChanged += OnSearchTextChanged;

            PackageDatabase.instance.onPackagesChanged += OnPackagesChanged;
            PackageDatabase.instance.onPackageVersionUpdated += OnPackageVersionUpdated;

            PackageDatabase.instance.onPackageOperationStart += OnPackageOperationStart;
            PackageDatabase.instance.onPackageOperationFinish += OnPackageOperationFinish;

            PackageDatabase.instance.onDownloadProgress += OnDownloadProgress;
            PackageDatabase.instance.onRefreshOperationStart += OnRefreshOperationStart;
            PackageDatabase.instance.onRefreshOperationFinish += OnRefreshOperationFinish;

            ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;

            // manually build the items on initialization to refresh the UI
            RebuildItems();
            OnSelectionChanged(SelectionManager.instance.GetSelections());
        }

        private PackageItem FindPackageItem(IPackage package)
        {
            return package == null ? null : packageItems.FirstOrDefault(p => p.package.uniqueId == package.uniqueId);
        }

        public void ShowResults(PackageItem item)
        {
            noResultText.text = string.Empty;
            UIUtils.SetElementDisplay(noResult, false);
            UIUtils.SetElementDisplay(emptyArea, false);
            UIUtils.SetElementDisplay(mustLogin, false);

            // Only select main element if none of its versions are already selected
            var hasSelection = item.GetSelectableItems().Any(i => SelectionManager.instance.IsSelected(i.package, i.targetVersion));
            if (!hasSelection)
                item.SelectMainItem();

            ScrollIfNeeded(list, item);
        }

        public void ShowEmptyResults()
        {
            UIUtils.SetElementDisplay(noResult, false);

            if (!ApplicationUtil.instance.isUserLoggedIn && PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                UIUtils.SetElementDisplay(mustLogin, true);
                UIUtils.SetElementDisplay(emptyArea, false);
            }
            else
            {
                UIUtils.SetElementDisplay(mustLogin, false);
                UIUtils.SetElementDisplay(emptyArea, true);
            }

            SelectionManager.instance.ClearSelection();
        }

        private void OnUserLoginStateChange(bool loggedIn)
        {
            if (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore)
            {
                if (loggedIn)
                {
                    UIUtils.SetElementDisplay(mustLogin, false);
                    UIUtils.SetElementDisplay(emptyArea, true);
                }
                else
                {
                    UIUtils.SetElementDisplay(mustLogin, true);
                    UIUtils.SetElementDisplay(emptyArea, false);
                }
            }
        }

        public void ShowNoResults()
        {
            if (!string.IsNullOrEmpty(PackageFiltering.instance.currentSearchText))
            {
                noResultText.text = string.Format(L10n.Tr("No results for \"{0}\""), PackageFiltering.instance.currentSearchText);
                UIUtils.SetElementDisplay(noResult, true);
            }
            UIUtils.SetElementDisplay(emptyArea, false);
            UIUtils.SetElementDisplay(mustLogin, false);
            SelectionManager.instance.ClearSelection();
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
            FindPackageItem(package)?.StopSpinner();
        }

        private void OnPackageOperationStart(IPackage package)
        {
            FindPackageItem(package)?.StartSpinner();
        }

        private void OnRefreshOperationStart()
        {
            emptyAreaText.text = (PackageFiltering.instance.currentFilterTab == PackageFilterTab.AssetStore) ? L10n.Tr("Fetching packages...") : string.Empty;
        }

        private void OnRefreshOperationFinish()
        {
            emptyAreaText.text = L10n.Tr("There are no packages.");
        }

        private void OnDownloadProgress(IPackage package, DownloadProgress progress)
        {
            var item = FindPackageItem(package);
            if (item != null)
            {
                if (progress.state == DownloadProgress.State.Completed || progress.state == DownloadProgress.State.Aborted || progress.state == DownloadProgress.State.Error)
                {
                    item.StopSpinner();
                    item.SetPackage(package);
                    if (progress.state == DownloadProgress.State.Completed)
                        item.SetExpand(false);
                }
                else
                    item.StartSpinner();
            }
        }

        private void ScrollIfNeeded(ScrollView container = null, VisualElement target = null)
        {
            container = container ?? list;
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

        private void SetSelectedExpand(bool value)
        {
            var selected = selectedItem;
            if (selected == null) return;

            selected.SetExpand(value);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Tab)
            {
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.RightArrow)
            {
                SetSelectedExpand(true);
                evt.StopPropagation();
            }

            if (evt.keyCode == KeyCode.LeftArrow)
            {
                var selected = selectedItem;
                SetSelectedExpand(false);

                // Make sure the main element get selected to not lose the selected element
                if (selected != null)
                    selected.SelectMainItem();

                evt.StopPropagation();
            }

            if (evt.keyCode == KeyCode.UpArrow)
            {
                if (SelectBy(-1))
                    evt.StopPropagation();
            }

            if (evt.keyCode == KeyCode.DownArrow)
            {
                if (SelectBy(1))
                    evt.StopPropagation();
            }
        }

        internal void OnSearchTextChanged(string searchText)
        {
            foreach (var item in packageItems)
                UIUtils.SetElementDisplay(item, PackageFiltering.instance.FilterByCurrentSearchText(item.package));

            RefreshSelection();
        }

        internal void OnFilterChanged(PackageFilterTab filter)
        {
            RebuildItems();
        }

        internal void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> updated)
        {
            var reorderNeeded = false;
            foreach (var package in added)
            {
                if (PackageFiltering.instance.FilterByCurrentTab(package))
                {
                    var item = new PackageItem(package);
                    list.Add(item);
                    reorderNeeded = true;
                    UIUtils.SetElementDisplay(item, PackageFiltering.instance.FilterByCurrentSearchText(package));
                }
            }

            foreach (var package in removed)
            {
                var item = FindPackageItem(package);
                if (item != null)
                {
                    list.Remove(item);
                }
            }

            foreach (var package in updated)
            {
                var item = FindPackageItem(package);
                if (item != null)
                {
                    if (PackageFiltering.instance.FilterByCurrentTab(package))
                    {
                        if (!reorderNeeded && item.displayName != package.displayName)
                            reorderNeeded = true;
                        item.SetPackage(package);
                    }
                    else
                        list.Remove(item);
                }
                else if (PackageFiltering.instance.FilterByCurrentTab(package))
                {
                    item = new PackageItem(package);
                    list.Add(item);
                    reorderNeeded = true;
                }
                UIUtils.SetElementDisplay(item, PackageFiltering.instance.FilterByCurrentSearchText(package));
            }

            if (!m_PackagesLoaded && added.Any())
            {
                m_PackagesLoaded = true;
                onPackagesLoaded();
            }

            if (reorderNeeded)
                ReorderPackageItems();

            RefreshSelection();
        }

        internal void OnPackageVersionUpdated(IPackageVersion version)
        {
            var item = packageItems.FirstOrDefault(x => x.targetVersion != null && x.targetVersion.uniqueId == version.uniqueId);
            if (item == null || item.displayName.Equals(version.displayName))
                return;
            item.displayName = version.displayName;
            ReorderPackageItems();
        }

        internal void ReorderPackageItems()
        {
            list.Sort((left, right) =>
            {
                var packageLeft = left as PackageItem;
                var packageRight = right as PackageItem;
                if (packageLeft == null || packageRight == null)
                    return 0;
                return string.Compare(packageLeft.displayName, packageRight.displayName,
                    StringComparison.InvariantCultureIgnoreCase);
            });
        }

        internal void OnSelectionChanged(IEnumerable<IPackageVersion> selections)
        {
            var selected = selections.FirstOrDefault();
            if (selected != null)
            {
                var package = PackageDatabase.instance.GetPackage(selected);
                if (package == null || !PackageFiltering.instance.FilterByCurrentTab(package) || !PackageFiltering.instance.FilterByCurrentSearchText(package))
                {
                    var firstVisibleItem = packageItems.FirstOrDefault(p => UIUtils.IsElementVisible(p));
                    SelectionManager.instance.SetSelected(firstVisibleItem?.package);
                    return;
                }
            }

            foreach (var packageitem in packageItems)
            {
                packageitem.RefreshSelection();
                foreach (var versionItem in packageitem.versionItems)
                    versionItem.RefreshSelection();
            }
            ScrollIfNeeded();
        }

        public List<ISelectableItem> GetSelectableItems()
        {
            return packageItems.SelectMany(item => item.GetSelectableItems()).ToList();
        }

        private bool SelectBy(int delta)
        {
            var list = GetSelectableItems();
            var selection = GetSelectedItem(list);
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

                SelectionManager.instance.SetSelected(nextElement.package, nextElement.targetVersion);

                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.element))
                    ScrollIfNeeded(scrollView, nextElement.element);
            }

            return true;
        }

        private ISelectableItem GetSelectedItem(List<ISelectableItem> items = null)
        {
            return (items ?? GetSelectableItems()).Find(s => SelectionManager.instance.IsSelected(s.package, s.targetVersion));
        }

        private void ClearAll()
        {
            list.Clear();

            UIUtils.SetElementDisplay(emptyArea, false);
            UIUtils.SetElementDisplay(noResult, false);
            UIUtils.SetElementDisplay(mustLogin, false);
        }

        public void RebuildItems()
        {
            // package items that do not match the filter are not in the list in the first place
            // while items that do not match the search text are in the list but set to be hidden
            var packages = PackageDatabase.instance.allPackages.Where(p => PackageFiltering.instance.FilterByCurrentTab(p));
            if (!m_PackagesLoaded && packages.Any())
            {
                m_PackagesLoaded = true;
                onPackagesLoaded();
            }

            ClearAll();

            var orderedPackages = packages.OrderBy(p => p.primaryVersion?.displayName ?? p.name).ToList();
            foreach (var package in orderedPackages)
            {
                var packageItem = new PackageItem(package);
                list.Add(packageItem);
                var itemVisible = PackageFiltering.instance.FilterByCurrentSearchText(package);
                UIUtils.SetElementDisplay(packageItem, itemVisible);
                if (itemVisible && SelectionManager.instance.IsExpanded(package))
                    packageItem.SetExpand(true);
            }

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            var visiblePackageItems = packageItems.Where(p => UIUtils.IsElementVisible(p));
            if (!visiblePackageItems.Any())
            {
                ShowEmptyResults();
                return;
            }
            var currentSelection = SelectionManager.instance.GetSelections().FirstOrDefault();
            var selectedItem = currentSelection == null ? null : visiblePackageItems.FirstOrDefault(item => item.package.uniqueId == currentSelection.packageUniqueId);
            ShowResults(selectedItem ?? visiblePackageItems.First());
        }

        private VisualElementCache cache { get; set; }

        private ScrollView list { get { return cache.Get<ScrollView>("scrollView"); } }
        private VisualElement emptyArea { get { return cache.Get<VisualElement>("emptyArea"); } }
        private Label emptyAreaText { get { return cache.Get<Label>("emptyAreaText"); } }
        private VisualElement noResult { get { return cache.Get<VisualElement>("noResult"); } }
        private Label noResultText { get { return cache.Get<Label>("noResultText"); } }
        private VisualElement mustLogin { get { return cache.Get<VisualElement>("mustLogin"); } }
        private Button loginBtn { get { return cache.Get<Button>("loginBtn"); } }
    }
}
