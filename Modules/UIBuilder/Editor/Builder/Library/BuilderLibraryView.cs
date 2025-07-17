// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using TreeViewItem = UnityEngine.UIElements.TreeViewItemData<Unity.UI.Builder.BuilderLibraryTreeItem>;

namespace Unity.UI.Builder
{
    abstract class BuilderLibraryView : VisualElement
    {
        VisualElement m_DocumentRootElement;
        BuilderSelection m_Selection;
        BuilderTooltipPreview m_TooltipPreview;
        BuilderPaneContent m_BuilderPaneContent;

        protected BuilderPaneWindow m_PaneWindow;
        protected BuilderLibraryDragger m_Dragger;

        protected IList<TreeViewItem> m_Items;
        protected IList<TreeViewItem> m_VisibleItems;

        internal IList<TreeViewItem> visibleItems => m_VisibleItems;

        public abstract VisualElement primaryFocusable { get; }

        public virtual void SetupView(BuilderLibraryDragger dragger, BuilderTooltipPreview tooltipPreview,
            BuilderPaneContent builderPaneContent, BuilderPaneWindow builderPaneWindow,
            VisualElement documentElement, BuilderSelection selection)
        {
            m_Dragger = dragger;
            m_TooltipPreview = tooltipPreview;
            m_BuilderPaneContent = builderPaneContent;
            m_PaneWindow = builderPaneWindow;
            m_DocumentRootElement = documentElement;
            m_Selection = selection;
        }

        public abstract void Refresh();
        public abstract void FilterView(string value);

        protected void RegisterControlContainer(VisualElement element)
        {
            m_Dragger?.RegisterCallbacksOnTarget(element);

            if (m_TooltipPreview != null)
            {
                element.RegisterCallback<MouseEnterEvent>(OnItemMouseEnter);
                element.RegisterCallback<MouseLeaveEvent>(OnItemMouseLeave);
            }
        }

        protected void LinkToTreeViewItem(VisualElement element, BuilderLibraryTreeItem libraryTreeItem)
        {
            element.userData = libraryTreeItem;
            element.SetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName, libraryTreeItem);
        }

        protected BuilderLibraryTreeItem GetLibraryTreeItem(VisualElement element)
        {
            return (BuilderLibraryTreeItem)element.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName);
        }

        internal void AddItemToTheDocument(BuilderLibraryTreeItem item)
        {
            var builder = m_PaneWindow as Builder;
            var activeVTARootElement = builder.GetActiveRootElement();

            if (activeVTARootElement == null)
                return;

            BuilderLibraryUtility.InsertElementToDocument(m_PaneWindow.document, m_PaneWindow.primarySelection, item, activeVTARootElement);
        }

        void OnItemMouseEnter(MouseEnterEvent evt)
        {
            var box = evt.elementTarget;
            var libraryTreeItem = box.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName) as BuilderLibraryTreeItem;

            if (!libraryTreeItem.hasPreview)
                return;

            var sample = libraryTreeItem.makeVisualElementCallback?.Invoke();
            if (sample == null)
                return;

            var panel = m_TooltipPreview.panel as BaseVisualElementPanel;
            var styleUpdater = panel.GetUpdater(VisualTreeUpdatePhase.Styles) as VisualTreeStyleUpdater;
            if (styleUpdater.traversal is BuilderVisualTreeStyleUpdaterTraversal updaterTraversal)
            {
                updaterTraversal.previewDocument = m_TooltipPreview;
            }

            m_TooltipPreview.Add(sample);
            m_TooltipPreview.Show();

            m_TooltipPreview.style.left = m_BuilderPaneContent.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset;
            m_TooltipPreview.style.top = m_BuilderPaneContent.pane.resolvedStyle.top;
        }

        void OnItemMouseLeave(MouseLeaveEvent evt)
        {
            HidePreview();
        }

        protected void HidePreview()
        {
            m_TooltipPreview.Clear();
            m_TooltipPreview.Hide();
        }

        protected static IList<TreeViewItem> FilterTreeViewItems(IEnumerable<TreeViewItem> items, string searchText)
        {
            var filteredItems = new List<TreeViewItem>();

            foreach (var item in items)
            {
                if (item.data.name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    filteredItems.Add(item);
                }
                else if (item.children != null && item.children.GetCount() > 0)
                {
                    // Recursively filter children
                    var filteredChildren = FilterTreeViewItems(item.children, searchText);
                    if (filteredChildren.Count > 0)
                    {
                        // If any children match, add a copy of the parent item with filtered children
                        var itemCopy = new TreeViewItem(item.id, item.data);
                        itemCopy.AddChildren(filteredChildren);
                        filteredItems.Add(itemCopy);
                    }
                }
            }

            return filteredItems;
        }
    }
}
