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
        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement selectedElement { get; set; }
        GraphElement clickedElement { get; set; }

        private GraphViewChange m_GraphViewChange;
        private List<GraphElement> m_MovedElements;

        public SelectionDragger()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
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
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected new void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            var graphView = target as GraphView;
            if (graphView == null)
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

                m_Active = true;
                target.TakeMouseCapture(); // We want to receive events even when mouse is not over ourself.
            }
        }

        protected new void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var selectionContainer = target as ISelection;
            if (selectionContainer == null)
                return;

            foreach (ISelectable s in selectionContainer.selection)
            {
                GraphElement ce = s as GraphElement;
                if (ce == null)
                    continue;

                if (!ce.IsMovable())
                    continue;

                Matrix4x4 g = ce.worldTransform;
                var scale = new Vector3(g.m00, g.m11, g.m22);

                ce.SetPosition(
                    CalculatePosition(ce.layout.x + e.mouseDelta.x * panSpeed.x / scale.x,
                        ce.layout.y + e.mouseDelta.y * panSpeed.y / scale.y,
                        ce.layout.width, ce.layout.height));
            }

            selectedElement = null;
            e.StopPropagation();
        }

        protected new void OnMouseUp(MouseUpEvent e)
        {
            var selectionContainer = target as ISelection;
            if (selectionContainer == null)
                return;

            if (m_Active && CanStopManipulation(e))
            {
                if (selectedElement == null)
                {
                    m_MovedElements.Clear();
                    foreach (ISelectable s in selectionContainer.selection)
                    {
                        var ce = s as GraphElement;
                        if (ce == null)
                            continue;

                        if (!ce.IsMovable())
                            continue;

                        ce.UpdatePresenterPosition();

                        m_MovedElements.Add(ce);
                    }

                    var graphView = target as GraphView;
                    if (graphView != null && graphView.graphViewChanged != null)
                    {
                        graphView.graphViewChanged(m_GraphViewChange);
                    }
                }

                target.ReleaseMouseCapture();
                e.StopPropagation();
            }
            m_Active = false;
        }
    }
}
