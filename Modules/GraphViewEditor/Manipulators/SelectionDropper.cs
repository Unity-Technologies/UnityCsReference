// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    // TODO: Should stay internal when GraphView becomes public
    internal class DragAndDropDelay
    {
        const float k_StartDragTreshold = 4.0f;

        Vector2 mouseDownPosition { get; set; }

        public void Init(Vector2 mousePosition)
        {
            mouseDownPosition = mousePosition;
        }

        public bool CanStartDrag(Vector2 mousePosition)
        {
            return Vector2.Distance(mouseDownPosition, mousePosition) > k_StartDragTreshold;
        }
    }

    // Manipulates movable objects, can also initiate a Drag and Drop operation
    // FIXME: update this code once we have support for drag and drop events in UIElements.
    public class SelectionDropper : Manipulator
    {
        readonly DragAndDropDelay m_DragAndDropDelay;

        bool m_Active;

        public Vector2 panSpeed { get; set; }

        public MouseButton activateButton { get; set; }

        public bool clampToParentEdges { get; set; }

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement selectedElement { get; set; }
        ISelection selectionContainer { get; set; }

        public SelectionDropper()
        {
            m_Active = false;

            m_DragAndDropDelay = new DragAndDropDelay();

            activateButton = MouseButton.LeftMouse;
            panSpeed = new Vector2(1, 1);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        bool m_AddedByMouseDown;
        bool m_Dragging;

        void Reset()
        {
            m_Active = false;
            m_AddedByMouseDown = false;
            m_Dragging = false;
        }

        void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                Reset();
            }
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            m_Active = false;
            m_Dragging = false;
            m_AddedByMouseDown = false;

            if (target == null)
                return;

            selectionContainer = target.GetFirstAncestorOfType<ISelection>();

            if (selectionContainer == null)
            {
                // Keep for potential later use in OnMouseUp (where e.target might be different then)
                selectionContainer = target.GetFirstOfType<ISelection>();
                selectedElement = e.target as GraphElement;
                return;
            }

            selectedElement = target.GetFirstOfType<GraphElement>();

            if (selectedElement == null)
                return;

            // Since we didn't drag after all, update selection with current element only
            if (!selectionContainer.selection.Contains(selectedElement))
            {
                if (!e.actionKey)
                    selectionContainer.ClearSelection();
                selectionContainer.AddToSelection(selectedElement);
                m_AddedByMouseDown = true;
            }

            if (e.button == (int)activateButton)
            {
                // avoid starting a manipulation on a non movable object

                if (!selectedElement.IsDroppable())
                    return;

                // Reset drag and drop
                m_DragAndDropDelay.Init(e.localMousePosition);

                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active && !m_Dragging && selectionContainer != null)
            {
                // Keep a copy of the selection
                var selection = selectionContainer.selection.ToList();

                if (selection.Count > 0)
                {
                    var ce = selection[0] as GraphElement;
                    bool canStartDrag = ce != null && ce.IsDroppable();

                    if (canStartDrag && m_DragAndDropDelay.CanStartDrag(e.localMousePosition))
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new UnityEngine.Object[] {};   // this IS required for dragging to work
                        DragAndDrop.SetGenericData("DragSelection", selection);
                        m_Dragging = true;

                        DragAndDrop.StartDrag("");
                        DragAndDrop.visualMode = e.actionKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                        target.ReleaseMouse();
                    }

                    e.StopPropagation();
                }
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || selectionContainer == null)
            {
                if (selectedElement != null && selectionContainer != null && !m_Dragging)
                {
                    if (selectedElement.IsSelected((VisualElement)selectionContainer) && !e.actionKey)
                    {
                        // Reset to single selection
                        selectionContainer.ClearSelection();
                        selectedElement.Select((VisualElement)selectionContainer, e.actionKey);
                    }
                }

                Reset();
                return;
            }

            if (e.button == (int)activateButton)
            {
                // Since we didn't drag after all, update selection with current element only
                if (!e.actionKey)
                {
                    selectionContainer.ClearSelection();
                    selectionContainer.AddToSelection(selectedElement);
                }
                else if (m_AddedByMouseDown && !m_Dragging && selectionContainer.selection.Contains(selectedElement))
                {
                    selectionContainer.RemoveFromSelection(selectedElement);
                }

                target.ReleaseMouse();
                e.StopPropagation();
                Reset();
            }
        }
    }
}
