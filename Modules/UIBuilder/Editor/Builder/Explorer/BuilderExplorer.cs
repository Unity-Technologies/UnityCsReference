// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.StyleSheets;
using UnityEditor.UIElements;
using UnityEngine;
using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<UnityEngine.UIElements.VisualElement>;

namespace Unity.UI.Builder
{
    internal abstract class BuilderExplorer : BuilderPaneContent, IBuilderSelectionNotifier
    {
        static readonly string s_UssClassName = "unity-builder-explorer";
        protected static readonly string kSearchFieldName = "search-field";
        protected static readonly string kNoResultsName = "no-results-label";

        [Flags]
        internal enum BuilderElementInfoVisibilityState
        {
            TypeName = 1 << 0,
            ClassList = 1 << 1,
            StyleSheets = 1 << 2,
            FullSelectorText = 1 << 3,

            All = ~0
        }

        VisualElement m_DocumentElementRoot;
        bool m_IncludeDocumentElementRoot;
        protected VisualElement m_DocumentElement;
        protected BuilderPaneWindow m_PaneWindow;
        protected BuilderViewport m_Viewport;
        protected ElementHierarchyView m_ElementHierarchyView;
        protected BuilderSelection m_Selection;
        bool m_SelectionMadeExternally;
        [SerializeField] protected BuilderElementInfoVisibilityState m_ElementInfoVisibilityState;
        protected ToolbarSearchField m_SearchField;
        protected VisualElement m_NoResultsLabel;

        BuilderClassDragger m_ClassDragger;
        BuilderExplorerDragger m_ExplorerDragger;

        protected bool selectionMadeExternally => m_SelectionMadeExternally;

        internal ElementHierarchyView elementHierarchyView => m_ElementHierarchyView;
        public VisualElement container => m_ElementHierarchyView.container;
        public BuilderPaneWindow paneWindow => m_PaneWindow;

        // Caching whether we need to rebuild the hierarchy on a style change.
        // We need to rebuild the hierarchy to update the file name to indicate to the user that there
        // are unsaved changes.  But Style changes do not change the hierarchy.  Thus, we only need to
        // rebuild the hierarchy to indicate that there are unsaved changes due to style when:
        //     1. the document has no unsaved changes
        //     2. and it's the first style change event.
        // Otherwise there is no need.
        private bool m_ShouldRebuildHierarchyOnStyleChange;

        public BuilderExplorer(
            BuilderPaneWindow paneWindow,
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderExplorerDragger explorerDragger,
            BuilderElementContextMenu contextMenuManipulator,
            VisualElement documentElementRoot,
            bool includeDocumentElementRoot,
            HighlightOverlayPainter highlightOverlayPainter,
            string toolbarUxmlPath,
            string profilerMarkerName)
        {
            m_PaneWindow = paneWindow;
            m_Viewport = viewport;
            m_DocumentElementRoot = documentElementRoot;
            m_IncludeDocumentElementRoot = includeDocumentElementRoot;
            m_DocumentElement = viewport.documentRootElement;
            AddToClassList(s_UssClassName);

            m_ClassDragger = classDragger;
            m_ExplorerDragger = explorerDragger;

            m_SelectionMadeExternally = false;

            m_Selection = selection;

            // Query the UI
            if (!string.IsNullOrEmpty(toolbarUxmlPath))
            {
                var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(toolbarUxmlPath);
                template.CloneTree(this);
            }

            // Create the Hierarchy View.
            m_ElementHierarchyView = new ElementHierarchyView(
                m_PaneWindow,
                m_DocumentElement,
                selection, classDragger, explorerDragger,
                contextMenuManipulator, ElementSelectionChanged, highlightOverlayPainter, profilerMarkerName);
            m_ElementHierarchyView.style.flexGrow = 1;
            Add(m_ElementHierarchyView);
            // Make sure the Hierarchy View gets focus when the pane gets focused.
            primaryFocusable = m_ElementHierarchyView;
            explorerDragger.RegisterPaneContent(m_ElementHierarchyView);
            UpdateHierarchyAndSelection(false);
            m_ShouldRebuildHierarchyOnStyleChange = true;
        }

        internal void ChangeVisibilityState(BuilderElementInfoVisibilityState state)
        {
            m_ElementInfoVisibilityState ^= state;
            m_ElementHierarchyView.elementInfoVisibilityState = m_ElementInfoVisibilityState;
            SaveViewData();
            UpdateHierarchyAndSelection(m_ElementHierarchyView.hasUnsavedChanges);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            m_ElementHierarchyView.elementInfoVisibilityState = m_ElementInfoVisibilityState;
        }

        public void ClearHighlightOverlay()
        {
            m_ElementHierarchyView.ClearHighlightOverlay();
        }

        public void ResetHighlightOverlays()
        {
            m_ElementHierarchyView.ResetHighlightOverlays();
        }

        protected virtual void ElementSelectionChanged(List<VisualElement> elements)
        {
            if (m_SelectionMadeExternally)
                return;

            if (elements == null)
            {
                m_Selection.ClearSelection(this);
                return;
            }

            m_Selection.ClearSelection(this);
            foreach (var element in elements)
            {
                if (element.ClassListContains(BuilderConstants.ExplorerItemUnselectableClassName))
                {
                    m_SelectionMadeExternally = true;
                    m_ElementHierarchyView.ClearSelection();
                    m_SelectionMadeExternally = false;
                    m_Selection.ClearSelection(this);
                    return;
                }

                m_Selection.AddToSelection(this, element);
            }
        }

        void UpdateHierarchy(bool hasUnsavedChanges)
        {
            m_ElementHierarchyView.hierarchyHasChanged = true;

            // reset unsaved changes on save
            if (!hasUnsavedChanges)
                m_ElementHierarchyView.hasUnsavedChanges = false;

            m_ElementHierarchyView.RebuildTree(m_DocumentElementRoot, m_IncludeDocumentElementRoot);
        }

        protected void UpdateHierarchyAndSelection(bool hasUnsavedChanges)
        {
            m_SelectionMadeExternally = true;

            m_ElementHierarchyView.ClearHighlightOverlay();

            UpdateHierarchy(hasUnsavedChanges);

            if (!m_Selection.isEmpty)
            {
                m_ElementHierarchyView.SelectElements(m_Selection.selection);
                m_ElementHierarchyView.IncrementVersion(VersionChangeType.Styles);
            }

            m_SelectionMadeExternally = false;

            m_ElementHierarchyView.ApplyRegisteredSelectionInternallyIfNeeded();
        }

        public virtual void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if (element == null ||
                (changeType & (BuilderHierarchyChangeType.ChildrenAdded |
                    BuilderHierarchyChangeType.ChildrenRemoved)) != 0)
            {
                UpdateHierarchyAndSelection(m_Selection.hasUnsavedChanges);
                m_ShouldRebuildHierarchyOnStyleChange = !m_Selection.hasUnsavedChanges;
            }
        }

        protected virtual bool IsSelectedItemValid(VisualElement element)
        {
            return true;
        }

        public virtual void SelectionChanged()
        {
            if (!m_Selection.selection.Any())
            {
                m_SelectionMadeExternally = true;
                m_ElementHierarchyView.ClearSelection();
                m_SelectionMadeExternally = false;
                return;
            }

            foreach (var element in m_Selection.selection)
            {
                if (!IsSelectedItemValid(element))
                {
                    m_SelectionMadeExternally = true;
                    m_ElementHierarchyView.ClearSelection();
                    m_SelectionMadeExternally = false;
                    return;
                }
            }

            m_SelectionMadeExternally = true;
            m_ElementHierarchyView.SelectElements(m_Selection.selection);
            m_SelectionMadeExternally = false;
        }

        public virtual void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            if (m_ShouldRebuildHierarchyOnStyleChange && changeType == BuilderStylingChangeType.Default)
            {
                UpdateHierarchyAndSelection(m_Selection.hasUnsavedChanges);
            }
            m_ShouldRebuildHierarchyOnStyleChange = !m_Selection.hasUnsavedChanges;
        }

        protected void UpdateSearchFilter(string value)
        {
            // When search is cleared and there's a selection, expand the tree to show the selected elements
            List<VisualElement> previousSelection = null;
            if (string.IsNullOrEmpty(value) && !m_Selection.isEmpty)
            {
                previousSelection = new List<VisualElement>(m_Selection.selection);
            }

            FilterView(value);
            var noResults = !string.IsNullOrEmpty(value) && (m_ElementHierarchyView.treeRootItems == null || m_ElementHierarchyView.treeRootItems.Count == 0);
            m_NoResultsLabel.style.display = noResults ? DisplayStyle.Flex : DisplayStyle.None;
            m_ElementHierarchyView.style.display = noResults ? DisplayStyle.None : DisplayStyle.Flex;

            if (previousSelection != null)
            {
                foreach (var selectedElement in previousSelection)
                {
                    m_ElementHierarchyView.RecursivelyExpandToItem(selectedElement.parent);
                }
            }
        }

        public void FilterView(string value)
        {
            var items = string.IsNullOrEmpty(value) ? m_ElementHierarchyView.unfilteredTreeRootItems : FilterTreeViewItems(m_ElementHierarchyView.unfilteredTreeRootItems, value);

            m_ElementHierarchyView.treeRootItems = items;
            m_ElementHierarchyView.treeView.SetRootItems(m_ElementHierarchyView.treeRootItems);
            m_ElementHierarchyView.treeView.RefreshItems();
        }

        protected virtual IList<TreeViewItem> FilterTreeViewItems(IEnumerable<TreeViewItem> items, string searchText)
        {
            var filteredItems = new List<TreeViewItem>();

            if (items == null)
                return filteredItems;

            foreach (var item in items)
            {
                var nameMatch = item.data.name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                var classMatch = item.data.ClassListContains(searchText);
                var typeMatch = item.data.GetUxmlTypeName().Contains(searchText, StringComparison.OrdinalIgnoreCase);

                var selector = item.data.GetStyleComplexSelector();
                if (selector != null)
                {
                    var selectorStr = StyleSheetToUss.ToUssSelector(selector);
                    nameMatch = selectorStr.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                }

                if (nameMatch || classMatch || typeMatch)
                {
                    var itemCopy = new TreeViewItem(item.id, item.data);
                    filteredItems.Add(itemCopy);
                }

                if (item.children != null)
                {
                    // Recursively filter children
                    var filteredChildren = FilterTreeViewItems(item.children, searchText);
                    foreach (var filteredChild in filteredChildren)
                    {
                        var itemCopy = new TreeViewItem(filteredChild.id, filteredChild.data);
                        filteredItems.Add(itemCopy);
                    }
                }
            }

            return filteredItems;
        }
    }
}
