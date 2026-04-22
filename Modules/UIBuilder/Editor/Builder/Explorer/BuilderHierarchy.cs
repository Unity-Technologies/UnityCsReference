// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchy : BuilderExplorer, IBuilderSelectionNotifier
    {
        public BuilderHierarchy(
            BuilderPaneWindow paneWindow,
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderElementContextMenu contextMenuManipulator,
            HighlightOverlayPainter highlightOverlayPainter)
            : base(
                  paneWindow,
                  viewport,
                  selection,
                  classDragger,
                  hierarchyDragger,
                  contextMenuManipulator,
                  viewport.documentRootElement,
                  true,
                  highlightOverlayPainter,
                  null,
                  "Hierarchy")
        {
            viewDataKey = "builder-hierarchy";

            m_ElementHierarchyView.RegisterCallback<FocusEvent>(e => ShortcutIntegration.instance.contextManager.RegisterToolContext(m_Viewport), useTrickleDown: TrickleDown.TrickleDown);
            m_ElementHierarchyView.RegisterCallback<BlurEvent>(e => ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_Viewport), useTrickleDown: TrickleDown.TrickleDown);
            m_ElementHierarchyView.RegisterCallback<DetachFromPanelEvent>(e => ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_Viewport));

            // Setup the search field
            m_SearchField = new ToolbarSearchField() { placeholderText = "Search..." };
            m_SearchField.RegisterValueChangedCallback(e => UpdateSearchFilter(e.newValue));
            Insert(0, m_SearchField);
            m_NoResultsLabel = new Label("No matches found.") { name = kNoResultsName };
            Add(m_NoResultsLabel);
            m_NoResultsLabel.style.display = DisplayStyle.None;
        }

        public override void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            var newUnsavedChanges = elementHierarchyView.hasUnsavedChanges != m_Selection.hasUnsavedChanges;
            elementHierarchyView.hasUnsavedChanges = m_Selection.hasUnsavedChanges;

            base.HierarchyChanged(element, changeType);

            if ((changeType & (BuilderHierarchyChangeType.ElementName | BuilderHierarchyChangeType.ClassList)) != 0)
            {
                // Look for the item associated with the element renamed. Note that the edited element may not be selected
                // yet.
                var item = m_ElementHierarchyView.FindElement(m_ElementHierarchyView.treeRootItems, element);
                var index = m_ElementHierarchyView.treeView.viewController.GetIndexForId(item.id);
                if (index == -1)
                {
                    return;
                }

                m_ElementHierarchyView.treeView.RefreshItem(index);
            }

            if (newUnsavedChanges)
            {
               // Update header
                var item = m_ElementHierarchyView.FindElement(m_ElementHierarchyView.treeRootItems, m_DocumentElement);
                var index = m_ElementHierarchyView.treeView.viewController.GetIndexForId(item.id);
                if (index == -1)
                {
                    return;
                }

                m_ElementHierarchyView.treeView.RefreshItem(index);
            }
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isVEA = element.GetVisualElementAsset() != null;
            var isVTA = element.GetVisualTreeAsset() != null;

            return isVEA || isVTA;
        }

        protected override void InitEllipsisMenu()
        {
            base.InitEllipsisMenu();

            if (pane == null)
                return;

            pane.AppendActionToEllipsisMenu("Type",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.TypeName),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.TypeName)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu("Class List",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.ClassList),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.ClassList)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu("Attached StyleSheets",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.StyleSheets),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.StyleSheets)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);
        }

        /// <summary>
        /// Returns the first item in the hierarchy view.
        /// </summary>
        /// <returns>The first BuilderExplorerItem in the hierarchy view, or null if the document is empty</returns>
        public BuilderExplorerItem GetFirstItem()
        {
            if (m_Viewport.documentRootElement.childCount == 0)
                return null;

            var firstDocumentElement = m_Viewport.documentRootElement[0];
            return (BuilderExplorerItem)firstDocumentElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName);
        }

        /// <summary>
        /// Retrieves the item in the hierarchy view that corresponds to the specified element name.
        /// </summary>
        /// <param name="elementName">The element name to seek</param>
        /// <returns>The hierarchy item found or null otherwise</returns>
        public BuilderExplorerItem GetItemByElementName(string elementName)
        {
            return this.Query<BuilderExplorerItem>()
                .Where(item => GetLinkedDocumentElement(item).name == elementName).First();
        }

        /// <summary>
        /// Retrieves the item in the hierarchy view that corresponds to the specified element type.
        /// </summary>
        /// <param name="elementType">The type associated to the item to seek</param>
        /// <returns>The hierarchy item found</returns>
        public BuilderExplorerItem GetItemByElementType(Type elementType)
        {
            return this.Query<BuilderExplorerItem>()
                .Where(item => GetLinkedDocumentElement(item).GetType() == elementType).First();
        }

        static VisualElement GetLinkedDocumentElement(VisualElement hierarchyItem)
        {
            return (VisualElement)hierarchyItem.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName);
        }
    }
}
