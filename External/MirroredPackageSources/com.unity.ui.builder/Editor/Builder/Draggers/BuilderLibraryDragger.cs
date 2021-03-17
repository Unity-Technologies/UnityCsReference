using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderLibraryDragger : BuilderDragger
    {
        static readonly string s_ContainerClassName = "unity-builder-dragger__container";
        static readonly string s_OverlayName = "unity-dragged-element-overlay";
        static readonly string s_OverlayClassName = "unity-builder-dragger__overlay";
        static readonly string s_EmptyVisualElementClassName = "unity-builder-dragger__empty-vs";
        static readonly string s_BeingDraggedClassName = "unity-builder-dragger--being-dragged";
        static readonly string s_DragPreviewElementClassName = "unity-builder-dragger__drag-preview";

        VisualElement m_DragPreviewLastParent;
        VisualElement m_DragPreviewElement;
        BuilderLibraryTreeItem m_LibraryItem;
        BuilderTooltipPreview m_TooltipPreview;
        VisualElement m_MadeElement;

        public BuilderLibraryDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport, BuilderParentTracker parentTracker,
            VisualElement explorerContainer,
            BuilderTooltipPreview tooltipPreview)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {
            builderHierarchyRoot = explorerContainer;
            m_TooltipPreview = tooltipPreview;
        }

        protected override VisualElement CreateDraggedElement()
        {
            var container = new VisualElement();
            container.AddToClassList(s_ContainerClassName);
            return container;
        }

        protected override void FillDragElement(VisualElement pill)
        {
            if (m_MadeElement == null)
                return;

            pill.Clear();

            m_MadeElement.AddToClassList(s_BeingDraggedClassName);
            pill.Add(m_MadeElement);

            if (m_MadeElement.GetType() == typeof(VisualElement))
                m_MadeElement.AddToClassList(s_EmptyVisualElementClassName);

            var overlay = new VisualElement();
            overlay.name = s_OverlayName;
            overlay.AddToClassList(s_OverlayClassName);
            pill.Add(overlay);
        }

        protected override bool StartDrag(VisualElement target, Vector2 mousePosition, VisualElement pill)
        {
            m_LibraryItem =
                target.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName)
                as BuilderLibraryTreeItem;
            if (m_LibraryItem == null)
                return false;

            var isCurrentDocumentVisualTreeAsset = m_LibraryItem.sourceAsset == paneWindow.document.visualTreeAsset;
            if (isCurrentDocumentVisualTreeAsset)
                return false;

            m_MadeElement = m_LibraryItem.makeVisualElementCallback?.Invoke();
            if (m_MadeElement == null)
                return false;

            m_TooltipPreview.Disable();

            return true;
        }

        protected override void PerformDrag(VisualElement target, VisualElement pickedElement, int index = -1)
        {
            if (pickedElement == null)
            {
                ResetDragPreviewElement();
                return;
            }

            if (pickedElement == m_DragPreviewLastParent || ElementIsInsideDragPreviewElement(pickedElement))
            {
                return;
            }
            
            ResetDragPreviewElement();

            m_DragPreviewLastParent = pickedElement;

            m_DragPreviewLastParent.HideMinSizeSpecialElement();

            FixElementSizeAndPosition(m_DragPreviewLastParent);

            var item =
                target.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName)
                as BuilderLibraryTreeItem;
            m_DragPreviewElement = item.makeVisualElementCallback();
            m_DragPreviewElement.AddToClassList(s_DragPreviewElementClassName);
        }

        protected override void PerformAction(VisualElement destination, DestinationPane pane, Vector2 localMousePosition, int index = -1)
        {
            // We should have an item reference here if the OnDragStart() worked.
            var item = m_LibraryItem;
            var itemVTA = item.sourceAsset;

            if (paneWindow.document.WillCauseCircularDependency(itemVTA))
            {
                BuilderDialogsUtility.DisplayDialog(BuilderConstants.InvalidWouldCauseCircularDependencyMessage,
                    BuilderConstants.InvalidWouldCauseCircularDependencyMessageDescription, BuilderConstants.DialogOkOption);
                return;
            }

            destination.RemoveMinSizeSpecialElement();

            // Determine if it applies and use Absolute Island insertion.
            if (BuilderProjectSettings.enableAbsolutePositionPlacement && pane == DestinationPane.Viewport && m_DragPreviewLastParent == documentRootElement && index < 0)
                m_DragPreviewLastParent = BuilderPlacementUtilities.CreateAbsoluteIsland(paneWindow, documentRootElement, localMousePosition);

            // Add VisualElement to Canvas.
            m_DragPreviewElement.RemoveFromClassList(s_DragPreviewElementClassName);
            if (index < 0)
                m_DragPreviewLastParent.Add(m_DragPreviewElement);
            else
                m_DragPreviewLastParent.Insert(index, m_DragPreviewElement);

            // Create equivalent VisualElementAsset.
            if (item.makeElementAssetCallback == null)
                BuilderAssetUtilities.AddElementToAsset(
                    paneWindow.document, m_DragPreviewElement, index);
            else
                BuilderAssetUtilities.AddElementToAsset(
                    paneWindow.document, m_DragPreviewElement, item.makeElementAssetCallback, index);

            selection.NotifyOfHierarchyChange(null);
            selection.NotifyOfStylingChange(null);
            selection.Select(null, m_DragPreviewElement);

            // Commit to the preview element as the final element.
            // This will stop the ResetDragPreviewElement() from calling
            // RemoveFromHierarchy() on it.
            m_DragPreviewElement = null;

            // If we dragged into the Viewport, focus the Viewport.
            if (pane == DestinationPane.Viewport)
                viewport.pane.Focus();
        }

        protected override void EndDrag()
        {
            ResetDragPreviewElement();
            m_TooltipPreview.Enable();
        }

        protected override bool StopEventOnMouseDown(MouseDownEvent evt)
        {
            return false;
        }

        protected override bool IsPickedElementValid(VisualElement element)
        {
            if (element == null)
                return true;

            if (element == viewport.documentRootElement)
                return true;

            if (element.contentContainer == null)
                return false;

            if (element.GetVisualElementAsset() == null)
                return false;

            if (!element.IsPartOfActiveVisualTreeAsset(paneWindow.document))
                return false;

            return true;
        }

        protected override bool SupportsDragBetweenElements(VisualElement element)
        {
            if (element == null)
                return false;

            if (element.GetVisualTreeAsset() != null)
                return false;

            if (element.GetVisualElementAsset() == null)
                return false;

            if (!element.IsPartOfActiveVisualTreeAsset(paneWindow.document))
                return false;

            return true;
        }

        bool ElementIsInsideDragPreviewElement(VisualElement ve)
        {
            if (ve == null)
                return false;

            if (m_DragPreviewElement == ve)
                return true;

            return ElementIsInsideDragPreviewElement(ve.parent);
        }

        void ResetDragPreviewElement()
        {
            if (m_DragPreviewLastParent != null)
            {
                UnfixElementSizeAndPosition(m_DragPreviewLastParent);
                m_DragPreviewLastParent.UnhideMinSizeSpecialElement();
                m_DragPreviewLastParent = null;
            }

            if (m_DragPreviewElement == null)
                return;

            m_DragPreviewElement.RemoveFromHierarchy();
            m_DragPreviewElement = null;
        }
    }
}
