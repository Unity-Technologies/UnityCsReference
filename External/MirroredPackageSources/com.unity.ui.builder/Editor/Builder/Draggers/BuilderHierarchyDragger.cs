using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchyDragger : BuilderExplorerDragger
    {
        static readonly string s_DragPreviewElementClassName = "unity-builder-dragger__drag-preview";

        public BuilderHierarchyDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport, BuilderParentTracker parentTracker)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {
        }

        protected override bool ExplorerCanStartDrag(VisualElement targetElement)
        {
            if (!targetElement.IsPartOfActiveVisualTreeAsset(paneWindow.document))
                return false;

            return true;
        }

        protected override string ExplorerGetDraggedPillText(VisualElement targetElement)
        {
            return string.IsNullOrEmpty(targetElement.name)
                ? targetElement.GetType().Name
                : targetElement.name;
        }

        protected override void ExplorerPerformDrag()
        {
            m_TargetElementToReparent.AddToClassList(s_DragPreviewElementClassName);
        }

        protected override void PerformAction(VisualElement destination, DestinationPane pane, Vector2 localMousePosition, int index = -1)
        {
            base.PerformAction(destination, pane, localMousePosition, index);

            m_TargetElementToReparent.RemoveFromClassList(s_DragPreviewElementClassName);

            // Remove temporary min-size element.
            destination.RemoveMinSizeSpecialElement();

            var newParent = destination;

            // We already have the correct index from the preview element that is
            // already inserted in the hierarchy. The index we get from the arguments
            // is actually incorrect (off by one) because it will count the
            // preview element.
            index = m_DragPreviewLastParent.IndexOf(m_TargetElementToReparent);

            bool undo = true;
            foreach (var elementToReparent in m_ElementsToReparent)
            {
                var element = elementToReparent.element;

                if (newParent == element || newParent.HasAncestor(element))
                    continue;

                BuilderAssetUtilities.ReparentElementInAsset(
                    paneWindow.document, element, newParent, index, undo);

                undo = false;
            }

            BuilderAssetUtilities.SortElementsByTheirVisualElementInAsset(newParent);

            selection.NotifyOfHierarchyChange(null);
            selection.NotifyOfStylingChange(null);
            selection.ForceReselection(null);
        }

        protected override bool IsPickedElementValid(VisualElement element)
        {
            if (element == null)
                return true;

            if (element.contentContainer == null)
                return false;

            if (element.GetVisualElementAsset() == null)
                return false;

            if (!element.IsPartOfActiveVisualTreeAsset(paneWindow.document))
                return false;

            var newParent = element;
            foreach (var elementToReparent in m_ElementsToReparent)
                if (newParent == elementToReparent.element || newParent.HasAncestor(elementToReparent.element))
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

            var newParent = element;
            foreach (var elementToReparent in m_ElementsToReparent)
                if (newParent == elementToReparent.element || newParent.HasAncestor(elementToReparent.element))
                    return false;

            return true;
        }

        protected override bool SupportsDragInEmptySpace(VisualElement element)
        {
            return element != null && element.HasAncestor(builderHierarchyRoot);
        }

        protected override void ResetDragPreviewElement()
        {
            m_TargetElementToReparent.RemoveFromClassList(s_DragPreviewElementClassName);

            if (m_DragPreviewLastParent == null)
                return;

            UnfixElementSizeAndPosition(m_DragPreviewLastParent);
            m_DragPreviewLastParent.UnhideMinSizeSpecialElement();
            m_DragPreviewLastParent = null;
        }
    }
}
