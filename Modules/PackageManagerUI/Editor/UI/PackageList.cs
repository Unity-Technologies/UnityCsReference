// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageList> {}

        public event Action OnLoaded = delegate {};
        public event Action OnFocusChange = delegate {};

        private readonly VisualElement root;
        private List<PackageGroup> Groups;
        private Selection Selection;

        internal PackageSearchFilter searchFilter;

        public PackageItem SelectedItem
        {
            get
            {
                var selected = GetSelectedElement();
                if (selected == null)
                    return null;

                var element = selected.Element;
                return UIUtils.GetParentOfType<PackageItem>(element);
            }
        }

        public PackageList()
        {
            Groups = new List<PackageGroup>();

            root = Resources.GetTemplate("PackageList.uxml");
            Add(root);
            root.StretchToParentSize();
            Cache = new VisualElementCache(root);

            List.contentContainer.AddToClassList("fix-scroll-view");

            UIUtils.SetElementDisplay(Empty, false);
            UIUtils.SetElementDisplay(NoResult, false);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        public void GrabFocus()
        {
            if (SelectedItem == null)
                return;

            SelectedItem.Focus();
        }

        public void ShowResults(PackageItem item)
        {
            NoResultText.text = string.Empty;
            UIUtils.SetElementDisplay(NoResult, false);

            // Only select main element if none of its versions are already selected
            var hasSelection = item.GetSelectionList().Any(i => Selection.IsSelected(i.TargetVersion));
            if (!hasSelection)
                item.SelectMainItem();

            EditorApplication.delayCall += ScrollIfNeededDelayed;

            UpdateGroups();
        }

        public void ShowNoResults()
        {
            NoResultText.text = string.Format("No results for \"{0}\"", searchFilter.SearchText);
            UIUtils.SetElementDisplay(NoResult, true);
            Selection.ClearSelection();
            foreach (var group in Groups)
            {
                UIUtils.SetElementDisplay(group, false);
            }
        }

        public void SetSearchFilter(PackageSearchFilter filter)
        {
            searchFilter = filter;
        }

        public void SetSelection(Selection selection)
        {
            Selection = selection;
        }

        private void UpdateGroups()
        {
            foreach (var group in Groups)
            {
                PackageItem firstPackage = null;
                PackageItem lastPackage = null;

                foreach (var item in group.PackageItems)
                {
                    if (!item.visible)
                        continue;

                    if (firstPackage == null) firstPackage = item;
                    lastPackage = item;
                }

                if (firstPackage == null && lastPackage == null)
                {
                    UIUtils.SetElementDisplay(group, false);
                }
                else
                {
                    UIUtils.SetElementDisplay(group, true);
                    group.firstPackage = firstPackage;
                    group.lastPackage = lastPackage;
                }
            }
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        private void ScrollIfNeededDelayed() {ScrollIfNeeded();}

        private void ScrollIfNeeded(VisualElement target = null)
        {
            EditorApplication.delayCall -= ScrollIfNeededDelayed;
            UIUtils.ScrollIfNeeded(List, target);
        }

        private void SetSelectedExpand(bool value)
        {
            var selected = SelectedItem;
            if (selected == null) return;

            selected.SetExpand(value);
        }

        private void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Tab)
            {
                OnFocusChange();
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
                var selected = SelectedItem;
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

        internal void OnLatestPackageInfoFetched(PackageInfo fetched, bool isDefaultVersion)
        {
            // only need to refresh the name label if it's the default version
            if (!isDefaultVersion)
                return;

            foreach (var group in Groups)
            {
                var item = group.PackageItems.FirstOrDefault(x => x.TargetVersion.PackageId == fetched.PackageId);
                if (item != null)
                {
                    item.SetDisplayName(fetched.DisplayName);
                    group.ReorderPackageItems();
                    return;
                }
            }
        }

        public List<IPackageSelection> GetSelectionList()
        {
            return Groups.SelectMany(g => g.GetSelectionList()).ToList();
        }

        public void EnsureSelectionIsVisible()
        {
            var list = GetSelectionList();
            var selection = GetSelectedElement(list);
            if (selection != null)
            {
                ScrollIfNeeded(selection.Element);
            }
        }

        private bool SelectBy(int delta)
        {
            var list = GetSelectionList();
            var selection = GetSelectedElement(list);
            if (selection != null)
            {
                var index = list.IndexOf(selection);

                var direction = Math.Sign(delta);
                delta = Math.Abs(delta);
                var nextIndex = index;
                var numVisibleElements = 0;
                IPackageSelection nextElement = null;
                while (numVisibleElements < delta)
                {
                    nextIndex += direction;
                    if (nextIndex >= list.Count)
                        return false;
                    if (nextIndex < 0)
                        return false;
                    nextElement = list.ElementAt(nextIndex);
                    if (UIUtils.IsElementVisible(nextElement.Element))
                        ++numVisibleElements;
                }

                Selection.SetSelection(nextElement.TargetVersion);

                foreach (var scrollView in UIUtils.GetParentsOfType<ScrollView>(nextElement.Element))
                    UIUtils.ScrollIfNeeded(scrollView, nextElement.Element);
            }

            return true;
        }

        private IPackageSelection GetSelectedElement(List<IPackageSelection> list = null)
        {
            list = list ?? GetSelectionList();
            var selection = list.Find(s => Selection.IsSelected(s.TargetVersion));

            return selection;
        }

        private void ClearAll()
        {
            List.Clear();
            Groups.Clear();

            UIUtils.SetElementDisplay(Empty, false);
            UIUtils.SetElementDisplay(NoResult, false);
        }

        public void SetPackages(PackageFilter filter, IEnumerable<Package> packages)
        {
            if (filter == PackageFilter.Modules)
            {
                packages = packages.Where(pkg => pkg.IsBuiltIn);
            }
            else if (filter == PackageFilter.All)
            {
                packages = packages.Where(pkg => !pkg.IsBuiltIn && pkg.IsDiscoverable);
            }
            else
            {
                packages = packages.Where(pkg => !pkg.IsBuiltIn);
                packages = packages.Where(pkg => pkg.Current != null && pkg.Current.IsDirectDependency);
            }

            OnLoaded();
            ClearAll();

            var packagesGroup = new PackageGroup(PackageGroupOrigins.Packages.ToString(), Selection);
            Groups.Add(packagesGroup);
            List.Add(packagesGroup);
            packagesGroup.previousGroup = null;

            var builtInGroup = new PackageGroup(PackageGroupOrigins.BuiltInPackages.ToString(), Selection);
            Groups.Add(builtInGroup);
            List.Add(builtInGroup);

            if (filter == PackageFilter.Modules)
            {
                packagesGroup.nextGroup = builtInGroup;
                builtInGroup.previousGroup = packagesGroup;
                builtInGroup.nextGroup = null;
            }
            else
            {
                packagesGroup.nextGroup = null;
                UIUtils.SetElementDisplay(builtInGroup, false);
            }

            var items = packages.OrderBy(pkg => pkg.VersionToDisplay == null ? pkg.Name : pkg.VersionToDisplay.DisplayName).ToList();
            foreach (var package in items)
            {
                AddPackage(package);
            }

            if (!Selection.Selected.Any() && items.Any())
                Selection.SetSelection(items.First());

            PackageFiltering.FilterPackageList(this);
        }

        private void AddPackage(Package package)
        {
            var groupName = package.Latest != null ? package.Latest.Group : package.Current.Group;
            var group = GetOrCreateGroup(groupName);
            group.AddPackage(package);
        }

        private PackageGroup GetOrCreateGroup(string groupName)
        {
            foreach (var g in Groups)
            {
                if (g.name == groupName)
                    return g;
            }

            var group = new PackageGroup(groupName, Selection);
            var latestGroup = Groups.LastOrDefault();
            Groups.Add(group);
            List.Add(group);

            group.previousGroup = null;
            if (latestGroup != null)
            {
                latestGroup.nextGroup = group;
                group.previousGroup = latestGroup;
                group.nextGroup = null;
            }
            return group;
        }

        private VisualElementCache Cache { get; set; }

        private ScrollView List { get { return Cache.Get<ScrollView>("scrollView"); } }
        private VisualElement Empty { get { return Cache.Get<VisualElement>("emptyArea"); } }
        private VisualElement NoResult { get { return Cache.Get<VisualElement>("noResult"); } }
        private Label NoResultText { get { return Cache.Get<Label>("noResultText"); } }
    }
}
