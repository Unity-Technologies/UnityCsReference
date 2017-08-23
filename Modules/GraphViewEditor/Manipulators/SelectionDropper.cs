// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    delegate void DropEvent(IMGUIEvent evt, List<ISelectable> selection, IDropTarget dropTarget);

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
    internal
    class SelectionDropper : Manipulator
    {
        readonly DragAndDropDelay m_DragAndDropDelay;

        // FIXME: remove this
        public event DropEvent OnDrop;

        bool m_Active;

        public Vector2 panSpeed { get; set; }

        public MouseButton activateButton { get; set; }

        public bool clampToParentEdges { get; set; }

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement selectedElement { get; set; }
        ISelection selectionContainer { get; set; }

        public SelectionDropper(DropEvent handler)
        {
            m_Active = true;

            OnDrop += handler;

            m_DragAndDropDelay = new DragAndDropDelay();

            activateButton = MouseButton.LeftMouse;
            panSpeed = new Vector2(1, 1);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<IMGUIEvent>(OnIMGUIEvent);
        }

        bool m_AddedByMouseDown;
        bool m_Dragging;

        protected void OnMouseDown(MouseDownEvent e)
        {
            m_Active = false;
            m_Dragging = false;
            m_AddedByMouseDown = false;

            if (target == null)
                return;
            selectionContainer = target.GetFirstAncestorOfType<ISelection>();

            if (selectionContainer == null)
                return;

            selectedElement = target.GetFirstOfType<GraphElement>();

            if (selectedElement == null)
                return;

            // Since we didn't drag after all, update selection with current element only
            if (!selectionContainer.selection.Contains(selectedElement))
            {
                if (!e.ctrlKey)
                    selectionContainer.ClearSelection();
                selectionContainer.AddToSelection(selectedElement);
                m_AddedByMouseDown = true;
            }


            if (e.button == (int)activateButton)
            {
                // avoid starting a manipulation on a non movable object

                var presenter = selectedElement.presenter;
                if (presenter != null && ((presenter.capabilities & Capabilities.Droppable) != Capabilities.Droppable))
                    return;

                // Reset drag and drop
                m_DragAndDropDelay.Init(e.localMousePosition);

                m_Active = true;
                target.TakeCapture();
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
                    bool canStartDrag = false;
                    var ce = selection[0] as GraphElement;
                    if (ce != null)
                    {
                        var presenter = ce.presenter;
                        if (presenter != null)
                            canStartDrag = (presenter.capabilities & Capabilities.Droppable) == Capabilities.Droppable;
                    }

                    if (canStartDrag && m_DragAndDropDelay.CanStartDrag(e.localMousePosition))
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new UnityEngine.Object[] {};  // this IS required for dragging to work
                        DragAndDrop.SetGenericData("DragSelection", selection);
                        m_Dragging = true;

                        DragAndDrop.StartDrag("");
                        DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                    }

                    e.StopPropagation();
                }
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || selectionContainer == null)
                return;
            if (e.button == (int)activateButton)
            {
                // Since we didn't drag after all, update selection with current element only
                if (!e.ctrlKey)
                {
                    selectionContainer.ClearSelection();
                    selectionContainer.AddToSelection(selectedElement);
                }
                else if (!m_AddedByMouseDown && !m_Dragging)
                {
                    selectionContainer.RemoveFromSelection(selectedElement);
                }

                target.ReleaseCapture();
                e.StopPropagation();
            }

            m_Active = false;
            m_AddedByMouseDown = false;
            m_Dragging = false;
        }

        public IDropTarget prevDropTarget;
        protected void OnIMGUIEvent(IMGUIEvent e)
        {
            if (!m_Active || selectionContainer == null)
                return;

            // Keep a copy of the selection
            var selection = selectionContainer.selection.ToList();

            Event evt = e.imguiEvent;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                {
                    if (target.HasCapture() && evt.button == (int)activateButton && selection.Count > 0)
                    {
                        selectedElement = null;

                        // TODO: Replace with a temp drawing or something...maybe manipulator could fake position
                        // all this to let operation know which element sits under cursor...or is there another way to draw stuff that is being dragged?

                        if (OnDrop != null)
                        {
                            var pickElem = target.panel.Pick(target.LocalToWorld(evt.mousePosition));
                            IDropTarget dropTarget = pickElem != null ? pickElem.GetFirstAncestorOfType<IDropTarget>() : null;
                            if (prevDropTarget != dropTarget && prevDropTarget != null)
                            {
                                IMGUIEvent eexit = IMGUIEvent.GetPooled(e.imguiEvent);
                                eexit.imguiEvent.type = EventType.DragExited;
                                OnDrop(eexit, selection, prevDropTarget);
                                IMGUIEvent.ReleasePooled(eexit);
                            }
                            OnDrop(e, selection, dropTarget);
                            prevDropTarget = dropTarget;
                        }

                        DragAndDrop.visualMode = evt.control ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                    }

                    break;
                }

                case EventType.DragExited:
                {
                    if (OnDrop != null && prevDropTarget != null)
                    {
                        OnDrop(e, selection, prevDropTarget);
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                    DragAndDrop.SetGenericData("DragSelection", null);

                    prevDropTarget = null;
                    m_Active = false;
                    target.ReleaseCapture();
                    break;
                }

                case EventType.DragPerform:
                {
                    if (m_Active && evt.button == (int)activateButton && selection.Count > 0)
                    {
                        if (selection.Count > 0)
                        {
                            if (OnDrop != null)
                            {
                                var pickElem = target.panel.Pick(target.LocalToWorld(evt.mousePosition));
                                IDropTarget dropTarget = pickElem != null ? pickElem.GetFirstAncestorOfType<IDropTarget>() : null;
                                OnDrop(e, selection, dropTarget);
                            }

                            DragAndDrop.visualMode = DragAndDropVisualMode.None;
                            DragAndDrop.SetGenericData("DragSelection", null);
                        }
                    }

                    prevDropTarget = null;
                    m_Active = false;
                    target.ReleaseCapture();
                    break;
                }
            }
        }
    }
}
