// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        public SelectionDragger()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            panSpeed = new Vector2(1, 1);
            clampToParentEdges = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
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

        bool m_AddedOnMouseDown;

        protected new void OnMouseDown(MouseDownEvent e)
        {
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

            if (!graphView.selection.Contains(clickedElement))
            {
                if (!e.ctrlKey)
                    graphView.ClearSelection();
                graphView.AddToSelection(clickedElement);
                m_AddedOnMouseDown = true;
            }

            if (CanStartManipulation(e))
            {
                selectedElement = clickedElement;

                GraphElementPresenter elementPresenter = selectedElement.presenter;
                if (elementPresenter != null && ((selectedElement.presenter.capabilities & Capabilities.Movable) != Capabilities.Movable))
                    return;

                m_Active = true;
                target.TakeCapture(); // We want to receive events even when mouse is not over ourself.
            }
        }

        bool m_Dragged;

        protected new void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            foreach (ISelectable s in graphView.selection)
            {
                GraphElement ce = s as GraphElement;
                if (ce == null || ce.presenter == null)
                    continue;

                if ((ce.presenter.capabilities & Capabilities.Movable) != Capabilities.Movable)
                    continue;

                m_Dragged = true;
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
            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (clickedElement != null && !m_Dragged)
            {
                // Since we didn't drag after all, update selection with current element only

                if (graphView.selection.Contains(clickedElement))
                {
                    if (e.ctrlKey)
                    {
                        if (!m_AddedOnMouseDown)
                        {
                            graphView.RemoveFromSelection(clickedElement);
                        }
                    }
                    else
                    {
                        graphView.ClearSelection();
                        graphView.AddToSelection(clickedElement);
                    }
                }
            }

            if (m_Active && CanStopManipulation(e))
            {
                if (selectedElement == null)
                {
                    foreach (ISelectable s in graphView.selection)
                    {
                        var ce = s as GraphElement;
                        if (ce == null || ce.presenter == null)
                            continue;

                        GraphElementPresenter elementPresenter = ce.presenter;
                        if ((ce.presenter.capabilities & Capabilities.Movable) != Capabilities.Movable)
                            continue;

                        elementPresenter.position = ce.layout;
                        elementPresenter.CommitChanges();
                    }
                }

                target.ReleaseCapture();
                e.StopPropagation();
            }
            m_AddedOnMouseDown = false;
            m_Dragged = false;
            m_Active = false;
        }
    }
}
