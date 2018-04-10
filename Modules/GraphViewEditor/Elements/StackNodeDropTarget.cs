// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public partial class StackNode : IDropTarget
    {
        private bool m_DragEntered;             // Indicates whether a drag has entered
        private VisualElement m_RemovedPreview; // The preview being removed
        private VisualElement m_CurrentPreview; // The current preview
        private int m_CurrentInsertIndex;       // The current index where the dragged item will be inserted once dropped

        private const string k_PreviewClass = "stack-node-preview";
        private Func<GraphElement, VisualElement> m_DropPreviewTemplate;

        private bool m_InstantAdd = false; // Temporarily set to true right after an item is detached from the stack to show its preview right away (instead of being animated)

        private ValueAnimation<Rect> m_AddAnimation;
        private ValueAnimation<Rect> m_RemoveAnimation;
        private StyleValue<int> m_AnimationDuration;
        private const string k_AnimationDuration = "animation-duration";
        private int animationDuration => m_AnimationDuration.GetSpecifiedValueOrDefault(40);

        private bool dragEntered
        {
            get { return m_DragEntered; }
            set
            {
                if (m_DragEntered == value)
                    return;

                m_DragEntered = value;
                m_SeparatorContainer.visible = !value; // Hide the separators when while dragging
                m_CurrentInsertIndex = -1;
            }
        }

        public Func<GraphElement, VisualElement> dropPreviewTemplate
        {
            get { return m_DropPreviewTemplate ?? DefaultDropPreviewTemplate; }
            set { m_DropPreviewTemplate = value; }
        }

        private VisualElement CreateDropPreview(GraphElement element)
        {
            VisualElement preview = dropPreviewTemplate(element);

            preview.AddToClassList(k_PreviewClass);
            preview.style.positionType = PositionType.Relative;

            return preview;
        }

        private void InitAnimations()
        {
            m_AddAnimation = new ValueAnimation<Rect>(this);
            m_AddAnimation.valueUpdated += (value) => UpdatePreviewLayout(m_CurrentPreview, value);

            m_RemoveAnimation = new ValueAnimation<Rect>(this);
            m_RemoveAnimation.valueUpdated += (value) => UpdatePreviewLayout(m_RemovedPreview, value);
            m_RemoveAnimation.finished += OnRemoveAnimationFinished;
        }

        private static VisualElement DefaultDropPreviewTemplate(GraphElement source)
        {
            VisualElement preview = new VisualElement();

            preview.AddToClassList("default");

            return preview;
        }

        private void AddPreview(VisualElement preview, int index, bool animated)
        {
            AddPreviewHelper(preview, index, animated);
        }

        private void AddPreviewHelper(VisualElement preview, int index, bool animated)
        {
            // Update the current preview with the newly added preview
            m_CurrentPreview = preview;

            Insert(index, m_CurrentPreview);

            if (animated)
            {
                Rect startRect = new Rect(Vector2.zero, m_CurrentPreview.layout.size);
                Rect endRect = startRect;

                startRect.height = 0f;
                m_AddAnimation.from = startRect;
                m_AddAnimation.to = endRect;
                m_AddAnimation.duration = animationDuration;
                m_AddAnimation.Start();
            }
            else
            {
                m_CurrentPreview.style.positionType = PositionType.Relative;
                m_CurrentPreview.Dirty(ChangeType.Layout);
            }
        }

        private void RemovePreview(bool animated)
        {
            if (m_CurrentPreview == null || m_RemovedPreview != null)
                return;

            m_AddAnimation.Stop();

            m_RemovedPreview = m_CurrentPreview;

            if (animated)
            {
                Rect startRect = new Rect(Vector2.zero, m_RemovedPreview.layout.size);
                Rect endRect = startRect;

                endRect.height = 0f;
                m_RemoveAnimation.from = startRect;
                m_RemoveAnimation.to = endRect;
                m_RemoveAnimation.duration = animationDuration;
                m_RemoveAnimation.Start();
            }
            else
            {
                RemovePreviewHelper();
            }
        }

        private void RemovePreviewHelper()
        {
            m_RemovedPreview.RemoveFromHierarchy();

            if (m_RemovedPreview == m_CurrentPreview)
                m_CurrentPreview = null;
            m_RemovedPreview = null;
        }

        private void UpdatePreviewLayout(VisualElement preview, Rect layout)
        {
            if (preview != null)
            {
                preview.layout = layout;
                preview.style.positionType = PositionType.Relative;
                preview.Dirty(ChangeType.Layout);
            }
        }

        private void OnRemoveAnimationFinished()
        {
            RemovePreviewHelper();

            // If a new preview was added while another one was been removed and that a drag exit occurred
            // before the RemoveAnimation finished then removed this added preview
            if (dragEntered == false && m_CurrentPreview != null)
                RemovePreview(true);
        }

        private void HandleDragAndDropEvent(IMouseEvent evt, IEnumerable<ISelectable> dragSelection)
        {
            if (m_AddAnimation.running || m_RemoveAnimation.running)
                return;

            IEnumerable<GraphElement> draggedElements = dragSelection.OfType<GraphElement>();

            dragEntered = true;

            GraphElement draggedElement = draggedElements.FirstOrDefault();
            int insertIndex = -1;
            Vector2 localMousePosition = graphView.ChangeCoordinatesTo(contentContainer, graphView.WorldToLocal(evt.mousePosition));
            int previewIndex = m_CurrentPreview?.parent.IndexOf(m_CurrentPreview) ?? -1;
            int maxIndex = 0;

            // If there is no child then add at index 0
            if (childCount == 0)
            {
                insertIndex = 0;
            }
            // Otherwise
            else
            {
                insertIndex = 0;

                // Determine the insert index by traversing the stack from top to bottom ignoring the previews and footer.
                // For each child element, use the vertical center as reference to determine if the dragged element should be inserted above or below it.
                foreach (VisualElement child in Children())
                {
                    Rect rect = child.layout;

                    if (m_RemovedPreview != child && m_CurrentPreview != child)
                    {
                        maxIndex++;
                        if (localMousePosition.y > (rect.y + rect.height / 2))
                        {
                            ++insertIndex;
                        }
                    }
                }
            }

            if (insertIndex != -1)
            {
                if (AcceptsElement(draggedElement, ref insertIndex, maxIndex))
                {
                    // Do nothing if the insert index has not changed
                    if (previewIndex == insertIndex)
                        return;

                    m_CurrentInsertIndex = insertIndex;

                    // Remove the current preview if there is any with some animation
                    RemovePreview(true);

                    // Create a preview
                    VisualElement preview = CreateDropPreview(draggedElement);

                    float previewWidth = contentContainer.layout.width;

                    if (!m_InstantAdd)
                    {
                        previewWidth = contentContainer.layout.width > 0f
                            ? contentContainer.layout.width
                            : draggedElement.layout.width;
                    }

                    preview.layout = new Rect(0, 0, previewWidth, draggedElement.layout.height + separatorHeight);

                    previewIndex = m_CurrentInsertIndex;

                    // If there is already a preview being removed then adjust the insert index of the new preview
                    if (m_RemovedPreview != null)
                    {
                        int removePreviewIndex = m_RemovedPreview?.parent.IndexOf(m_RemovedPreview) ?? -1;

                        if (removePreviewIndex < previewIndex)
                        {
                            previewIndex++;
                        }
                    }

                    AddPreview(preview, previewIndex, !m_InstantAdd);
                }
            }
        }

        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            IEnumerable<GraphElement> draggedElements = selection.OfType<GraphElement>();

            // TODO: Can only move one at a time for now, let's try to have multiple dragSelection support soon
            if (draggedElements.Count() != 1)
                return false;

            GraphElement draggedElement = draggedElements.FirstOrDefault();
            int proposedIndex = -1;
            int maxIndex = 1;

            if (draggedElement == null || draggedElement == this || !AcceptsElementInternal(draggedElement, ref proposedIndex, maxIndex))
            {
                return false;
            }

            return true;
        }

        public bool DragExited()
        {
            // Remove the current preview with some animation
            RemovePreview(true);

            dragEntered = false;

            return true;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            HandleDragAndDropEvent(evt, selection);

            GraphElement droppedElement = selection.First() as GraphElement;

            // Remove the current preview with no animation
            RemovePreview(false);

            if (m_CurrentInsertIndex != -1)
            {
                // Notify the model that an element should be inserted at the specified index
                if (graphView != null && graphView.elementInsertedToStackNode != null)
                {
                    graphView.elementInsertedToStackNode(this, m_CurrentInsertIndex, droppedElement);
                }
                else
                {
                    InsertElement(m_CurrentInsertIndex, droppedElement);
                }
            }

            dragEntered = false;

            return true;
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            HandleDragAndDropEvent(evt, selection);
            return true;
        }
    }
}
