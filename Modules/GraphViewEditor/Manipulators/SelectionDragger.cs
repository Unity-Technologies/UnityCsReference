// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class SelectionDragger : Dragger
    {
        IDropTarget m_PrevDropTarget;

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement selectedElement { get; set; }
        GraphElement clickedElement { get; set; }

        private GraphViewChange m_GraphViewChange;
        private List<GraphElement> m_MovedElements;

        IDropTarget GetDropTargetAt(Vector2 mousePosition)
        {
            Vector2 pickPoint = target.LocalToWorld(mousePosition);
            var pickList = new List<VisualElement>();
            target.panel.PickAll(pickPoint, pickList);

            // We know that the pickList is filled in a bottom-to-top hierarchy
            IDropTarget dropTarget = null;

            for (int i = pickList.Count - 1; i >= 0; i--)
            {
                if (pickList[i] == target)
                    continue;

                dropTarget = pickList[i] as IDropTarget;

                if (dropTarget != null && dropTarget != target)
                {
                    break;
                }
            }

            return dropTarget;
        }

        public SelectionDragger()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;

            m_MovedElements = new List<GraphElement>();
            m_GraphViewChange.movedElements = m_MovedElements;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var selectionContainer = target as ISelection;

            if (selectionContainer == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a control that supports selection");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);

            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private GraphView m_GraphView;

        private Dictionary<GraphElement, Rect> m_OriginalPos;
        private Vector2 m_originalMouse;

        void SendDragAndDropEvent(IMGUIEvent evt, List<ISelectable> selection, IDropTarget dropTarget)
        {
            if (dropTarget ==  null)
                return;

            switch (evt.imguiEvent.type)
            {
                case EventType.DragPerform:
                    dropTarget.DragPerform(evt, selection, dropTarget);
                    break;
                case EventType.DragUpdated:
                    dropTarget.DragUpdated(evt, selection, dropTarget);
                    break;
                case EventType.DragExited:
                    dropTarget.DragExited();
                    break;
                default:
                    break;
            }
        }

        protected new void OnMouseDown(MouseDownEvent e)
        {
            m_PrevDropTarget = null;
            m_Active = false;

            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            m_GraphView = target as GraphView;

            if (m_GraphView == null)
                return;

            selectedElement = null;

            // avoid starting a manipulation on a non movable object
            clickedElement = e.target as GraphElement;
            if (clickedElement == null)
            {
                var ve = e.target as VisualElement;
                clickedElement = ve.GetFirstAncestorOfType<GraphElement>();
                if (clickedElement == null)
                    return;
            }

            if (CanStartManipulation(e))
            {
                selectedElement = clickedElement;

                if (!selectedElement.IsMovable())
                    return;

                m_OriginalPos = new Dictionary<GraphElement, Rect>();

                foreach (ISelectable s in m_GraphView.selection)
                {
                    GraphElement ce = s as GraphElement;
                    if (ce == null || !ce.IsMovable())
                        continue;

                    m_OriginalPos[ce] = ce.layout;
                }

                m_originalMouse = e.mousePosition;
                m_ItemPanDiff = Vector3.zero;

                if (m_PanSchedule == null)
                {
                    m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
                    m_PanSchedule.Pause();
                }

                m_Active = true;
                target.TakeMouseCapture(); // We want to receive events even when mouse is not over ourself.
                e.StopPropagation();
            }
        }

        internal const int k_PanAreaWidth = 100;
        internal const int k_PanSpeed = 4;
        internal const int k_PanInterval = 10;
        private IVisualElementScheduledItem m_PanSchedule;
        private Vector3 m_PanDiff = Vector3.zero;
        private Vector3 m_ItemPanDiff = Vector3.zero;
        private Vector2 m_MouseDiff = Vector2.zero;

        protected new void OnMouseMove(MouseMoveEvent e)
        {
            if (!target.HasMouseCapture())
            {
                // We lost the capture. Since we still receive mouse events,
                // the MouseDown target must have taken it in its ExecuteDefaultAction().
                // Stop processing the event sequence, then.
                // FIXME: replace this by a handler on the upcoming LostCaptureEvent.
                m_Active = false;
            }

            if (!m_Active)
                return;

            if (m_GraphView == null)
                return;

            var ve = (VisualElement)e.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, e.localMousePosition);
            m_PanDiff = Vector3.zero;

            if (gvMousePos.x <= k_PanAreaWidth)
                m_PanDiff.x += -k_PanSpeed;
            else if (gvMousePos.x >= m_GraphView.contentContainer.layout.width - k_PanAreaWidth)
                m_PanDiff.x += k_PanSpeed;

            if (gvMousePos.y <= k_PanAreaWidth)
                m_PanDiff.y += -k_PanSpeed;
            else if (gvMousePos.y >= m_GraphView.contentContainer.layout.height - k_PanAreaWidth)
                m_PanDiff.y += k_PanSpeed;


            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            // We need to monitor the mouse diff "by hand" because we stop positionning the graph elements once the
            // mouse has gone out.
            m_MouseDiff = m_originalMouse - e.mousePosition;

            foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
            {
                GraphElement ce = v.Key;

                Matrix4x4 g = ce.worldTransform;
                var scale = new Vector3(g.m00, g.m11, g.m22);

                ce.SetPosition(
                    new Rect(v.Value.x - (m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / scale.x,
                        v.Value.y - (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / scale.y,
                        ce.layout.width, ce.layout.height));
            }
            List<ISelectable> selection = m_GraphView.selection;

            // TODO: Replace with a temp drawing or something...maybe manipulator could fake position
            // all this to let operation know which element sits under cursor...or is there another way to draw stuff that is being dragged?

            IDropTarget dropTarget = GetDropTargetAt(e.localMousePosition);

            if (m_PrevDropTarget != dropTarget && m_PrevDropTarget != null)
            {
                using (IMGUIEvent eexit = IMGUIEvent.GetPooled(e.imguiEvent))
                {
                    eexit.imguiEvent.type = EventType.DragExited;
                    SendDragAndDropEvent(eexit, selection, m_PrevDropTarget);
                }
            }

            using (IMGUIEvent eupdated = IMGUIEvent.GetPooled(e.imguiEvent))
            {
                eupdated.imguiEvent.type = EventType.DragUpdated;
                SendDragAndDropEvent(eupdated, selection, dropTarget);
            }

            m_PrevDropTarget = dropTarget;

            selectedElement = null;
            e.StopPropagation();
        }

        private void Pan(TimerState ts)
        {
            m_GraphView.viewTransform.position -= m_PanDiff;
            m_ItemPanDiff += m_PanDiff;

            foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
            {
                GraphElement ce = v.Key;

                Matrix4x4 g = ce.worldTransform;
                var scale = new Vector3(g.m00, g.m11, g.m22);

                ce.SetPosition(
                    new Rect(v.Value.x - (m_MouseDiff.x - m_ItemPanDiff.x) * panSpeed.x / scale.x,
                        v.Value.y - (m_MouseDiff.y - m_ItemPanDiff.y) * panSpeed.y / scale.y,
                        ce.layout.width, ce.layout.height));
            }
        }

        protected new void OnMouseUp(MouseUpEvent evt)
        {
            if (m_GraphView == null)
            {
                if (target.HasMouseCapture())
                {
                    target.ReleaseMouseCapture();
                }

                return;
            }

            List<ISelectable> selection = m_GraphView.selection;

            if (CanStopManipulation(evt))
            {
                if (m_Active && target.HasMouseCapture())
                {
                    if (selectedElement == null)
                    {
                        m_MovedElements.Clear();
                        foreach (GraphElement ce in m_OriginalPos.Keys)
                        {
                            ce.UpdatePresenterPosition();

                            m_MovedElements.Add(ce);
                        }

                        var graphView = target as GraphView;
                        if (graphView != null && graphView.graphViewChanged != null)
                        {
                            graphView.graphViewChanged(m_GraphViewChange);
                        }
                    }

                    m_PanSchedule.Pause();

                    if (m_ItemPanDiff != Vector3.zero)
                    {
                        Vector3 p = m_GraphView.contentViewContainer.transform.position;
                        Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                        m_GraphView.UpdateViewTransform(p, s);
                    }

                    if (selection.Count > 0)
                    {
                        IDropTarget dropTarget = GetDropTargetAt(evt.localMousePosition);

                        if (dropTarget != null)
                        {
                            using (IMGUIEvent drop = IMGUIEvent.GetPooled(evt.imguiEvent))
                            {
                                drop.imguiEvent.type = EventType.DragPerform;

                                SendDragAndDropEvent(drop, selection, dropTarget);
                            }
                        }
                    }

                    target.ReleaseMouseCapture();
                    evt.StopPropagation();
                }
                selectedElement = null;
                m_Active = false;
                m_PrevDropTarget = null;
            }
        }

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || m_GraphView == null || !m_Active)
                return;

            // Reset the items to their original pos.
            foreach (KeyValuePair<GraphElement, Rect> v in m_OriginalPos)
            {
                v.Key.SetPosition(v.Value);
            }

            m_PanSchedule.Pause();

            if (m_ItemPanDiff != Vector3.zero)
            {
                Vector3 p = m_GraphView.contentViewContainer.transform.position;
                Vector3 s = m_GraphView.contentViewContainer.transform.scale;
                m_GraphView.UpdateViewTransform(p, s);
            }


            target.ReleaseMouseCapture();
            e.StopPropagation();
        }
    }
}

