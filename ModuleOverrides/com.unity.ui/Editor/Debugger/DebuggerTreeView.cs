// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class DebuggerTreeView : VisualElement
    {
        internal const string itemContentName = "unity-treeview-item-content";
        internal const string labelContainerUssClassName = "unity-debugger-tree-item-label-cont";
        internal const string itemLabelUssClassName = "unity-debugger-tree-item-label";
        internal const string itemTypeUssClassName = "unity-debugger-tree-item-type";
        internal const string itemNameUssClassName = "unity-debugger-tree-item-name";
        internal const string itemNameLabelUssClassName = "unity-debugger-tree-item-name-label";
        internal const string itemClassListUssClassName = "unity-debugger-tree-item-classlist";
        internal const string itemClassListLabelUssClassName = "unity-debugger-tree-item-classlist-label";
        internal const string debuggerHighlightUssClassName = "unity-debugger-highlight";

        public bool hierarchyHasChanged { get; set; }

        public IList<ITreeViewItem> treeRootItems
        {
            get
            {
                return m_TreeRootItems;
            }
            private set {}
        }

        public IEnumerable<ITreeViewItem> treeItems
        {
            get
            {
                return m_TreeView?.items;
            }
        }

        private IList<ITreeViewItem> m_TreeRootItems = new List<ITreeViewItem>();

        private InternalTreeView m_TreeView;
        private HighlightOverlayPainter m_TreeViewHoverOverlay;

        private VisualElement m_Container;
        private DebuggerSearchBar m_SearchBar;

        private Action<VisualElement> m_SelectElementCallback;

        private List<VisualElement> m_SearchResultsHightlights;

        private DebuggerSelection m_DebuggerSelection;
        private IPanelDebug m_CurrentPanelDebug;

        public DebuggerTreeView(DebuggerSelection debuggerSelection, Action<VisualElement> selectElementCallback)
        {
            this.focusable = true;

            m_DebuggerSelection = debuggerSelection;
            m_DebuggerSelection.onPanelDebugChanged += pdbg => RebuildTree(pdbg);
            m_DebuggerSelection.onSelectedElementChanged += element => SelectElement(element, null);

            m_SelectElementCallback = selectElementCallback;
            hierarchyHasChanged = true;

            m_SearchResultsHightlights = new List<VisualElement>();

            this.RegisterCallback<FocusEvent>(e => m_TreeView?.Focus());

            m_TreeViewHoverOverlay = new HighlightOverlayPainter();

            m_Container = new VisualElement();
            m_Container.style.flexGrow = 1f;
            Add(m_Container);

            m_SearchBar = new DebuggerSearchBar(this);
            Add(m_SearchBar);
        }

        public void DrawOverlay(MeshGenerationContext mgc)
        {
            m_TreeViewHoverOverlay.Draw(mgc);
        }

        private void ActivateSearchBar(ExecuteCommandEvent evt)
        {
            Debug.Log(evt.commandName);
            if (evt.commandName == "Find")
                m_SearchBar.Focus();
        }

        private void FillItem(VisualElement element, ITreeViewItem item)
        {
            element.Clear();

            var target = (item as TreeViewItem<VisualElement>).data;
            element.userData = target;

            var labelCont = new VisualElement();
            labelCont.AddToClassList(labelContainerUssClassName);
            element.Add(labelCont);

            var label = new Label(target.typeName);
            label.AddToClassList(itemLabelUssClassName);
            label.AddToClassList(itemTypeUssClassName);
            labelCont.Add(label);

            if (!string.IsNullOrEmpty(target.name))
            {
                var nameLabelCont = new VisualElement();
                nameLabelCont.AddToClassList(labelContainerUssClassName);
                element.Add(nameLabelCont);

                var nameLabel = new Label("#" + target.name);
                nameLabel.AddToClassList(itemLabelUssClassName);
                nameLabel.AddToClassList(itemNameUssClassName);
                nameLabel.AddToClassList(itemNameLabelUssClassName);
                nameLabelCont.Add(nameLabel);
            }

            foreach (var ussClass in target.GetClasses())
            {
                var classLabelCont = new VisualElement();
                classLabelCont.AddToClassList(labelContainerUssClassName);
                element.Add(classLabelCont);

                var classLabel = new Label("." + ussClass);
                classLabel.AddToClassList(itemLabelUssClassName);
                classLabel.AddToClassList(itemClassListUssClassName);
                classLabel.AddToClassList(itemClassListLabelUssClassName);
                classLabelCont.Add(classLabel);
            }
        }

        private void HighlightItemInTargetWindow(VisualElement item)
        {
            m_TreeViewHoverOverlay.ClearOverlay();
            m_TreeViewHoverOverlay.AddOverlay(item.userData as VisualElement);
            m_CurrentPanelDebug?.MarkDirtyRepaint();
        }

        private void UnhighlightItemInTargetWindow(VisualElement item)
        {
            m_TreeViewHoverOverlay.ClearOverlay();
            m_CurrentPanelDebug?.MarkDirtyRepaint();
        }

        public void RebuildTree(IPanelDebug panelDebug)
        {
            if (!hierarchyHasChanged && m_CurrentPanelDebug == panelDebug)
                return;

            m_CurrentPanelDebug = panelDebug;
            m_Container.Clear();

            int nextId = 1;

            m_TreeRootItems.Clear();

            var visualTree = panelDebug?.visualTree;
            if (visualTree != null)
            {
                var rootItem = new TreeViewItem<VisualElement>(nextId++, visualTree);
                m_TreeRootItems.Add(rootItem);

                var childItems = new List<ITreeViewItem>();
                AddTreeItemsForElement(childItems, visualTree, ref nextId);

                rootItem.AddChildren(childItems);
            }

            Func<VisualElement> makeItem = () =>
            {
                var element = new VisualElement();
                element.name = itemContentName;
                element.RegisterCallback<MouseEnterEvent>((e) =>
                {
                    HighlightItemInTargetWindow(e.target as VisualElement);
                });
                element.RegisterCallback<MouseLeaveEvent>((e) =>
                {
                    UnhighlightItemInTargetWindow(e.target as VisualElement);
                });
                return element;
            };

            // Clear selection which would otherwise persist via view data persistence.
            m_TreeView?.ClearSelection();

            m_TreeView = new InternalTreeView(m_TreeRootItems, 20, makeItem, FillItem);
            m_TreeView.style.flexGrow = 1;
            m_TreeView.horizontalScrollingEnabled = true;
            m_TreeView.onSelectionChange += items =>
            {
                if (m_SelectElementCallback == null)
                    return;

                if (!items.Any())
                {
                    m_SelectElementCallback(null);
                    return;
                }

                var item = items.First() as TreeViewItem<VisualElement>;
                var element = item != null ? item.data : null;
                m_SelectElementCallback(element);
            };

            m_Container.Add(m_TreeView);

            hierarchyHasChanged = false;
            m_SearchBar.ClearSearch();
        }

        internal TreeViewItem<VisualElement> FindElement(IEnumerable<ITreeViewItem> list, VisualElement element)
        {
            if (list == null)
                return null;

            foreach (var item in list)
            {
                var treeItem = item as TreeViewItem<VisualElement>;
                if (treeItem.data == element)
                    return treeItem;

                TreeViewItem<VisualElement> itemFoundInChildren = null;
                if (treeItem.hasChildren)
                    itemFoundInChildren = FindElement(treeItem.children, element);

                if (itemFoundInChildren != null)
                    return itemFoundInChildren;
            }

            return null;
        }

        public void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        public void SelectElement(VisualElement element, string query)
        {
            SelectElement(element, query, SearchHighlight.None);
        }

        public void SelectElement(VisualElement element, string query, SearchHighlight searchHighlight)
        {
            ClearSearchResults();

            var item = FindElement(m_TreeRootItems, element);
            if (item == null)
                return;

            m_TreeView.SetSelection(item.id);
            m_TreeView.ScrollToItem(item.id);

            if (string.IsNullOrEmpty(query))
                return;

            schedule.Execute(() =>
            {
                var selected = m_TreeView.Q(className: BaseVerticalCollectionView.itemSelectedVariantUssClassName);
                if (selected == null || searchHighlight == SearchHighlight.None)
                    return;

                var content = selected.Q(itemContentName);
                var labelContainers = content.Query(classes: labelContainerUssClassName).ToList();
                foreach (var labelContainer in labelContainers)
                {
                    var label = labelContainer.Q<Label>();

                    if (label.ClassListContains(itemTypeUssClassName) && searchHighlight != SearchHighlight.Type)
                        continue;

                    if (label.ClassListContains(itemNameUssClassName) && searchHighlight != SearchHighlight.Name)
                        continue;

                    if (label.ClassListContains(itemClassListUssClassName) && searchHighlight != SearchHighlight.Class)
                        continue;

                    var text = label.text;
                    var indexOf = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                    if (indexOf < 0)
                        continue;

                    var highlight = new VisualElement();
                    m_SearchResultsHightlights.Add(highlight);
                    highlight.AddToClassList(debuggerHighlightUssClassName);
                    var letterSize = 8.4f;
                    highlight.style.width = query.Length * letterSize;
                    highlight.style.left = indexOf * letterSize;
                    labelContainer.Insert(0, highlight);

                    break;
                }
            });
        }

        private void AddTreeItemsForElement(IList<ITreeViewItem> items, VisualElement ve, ref int nextId)
        {
            if (ve == null)
                return;

            int count = ve.hierarchy.childCount;
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];

                var treeItem = new TreeViewItem<VisualElement>(nextId, child);
                items.Add(treeItem);
                nextId++;

                var childItems = new List<ITreeViewItem>();
                AddTreeItemsForElement(childItems, child, ref nextId);
                if (childItems.Count == 0)
                    continue;

                treeItem.AddChildren(childItems);
            }
        }

        protected internal void ScrollToSelection()
        {
            var item = FindElement(m_TreeRootItems, m_DebuggerSelection?.element);
            if (item == null)
                return;
            m_TreeView.ScrollToItem(item.id);
        }
    }
}
