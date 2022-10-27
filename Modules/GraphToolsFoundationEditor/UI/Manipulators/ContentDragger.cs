// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Changes the <see cref="GraphView"/> offset when the mouse is clicked and dragged in its background.
    /// </summary>
    class ContentDragger : MouseManipulator
    {
        Vector2 m_Start;
        public Vector2 panSpeed { get; set; }

        public bool ClampToParentWires { get; set; }

        bool m_Active;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentDragger"/> class.
        /// </summary>
        public ContentDragger()
        {
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.MiddleMouse });
            panSpeed = new Vector2(1, 1);
            ClampToParentWires = false;
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

        static void ChangeMouseCursorTo(BaseVisualElementPanel panel, int internalCursorId)
        {
            var cursor = new Cursor();
            cursor.defaultCursorId = internalCursorId;

            panel.cursorManager.SetCursor(cursor);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            m_Start = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition);

            m_Active = true;
            target.CaptureMouse();

            ChangeMouseCursorTo(graphView.elementPanel, (int)MouseCursor.Pan);

            e.StopImmediatePropagation();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            var diff = graphView.ChangeCoordinatesTo(graphView.ContentViewContainer, e.localMousePosition) - m_Start;

            // During the drag update only the view
            var scale = graphView.ContentViewContainer.transform.scale;
            var position = graphView.ViewTransform.position + Vector3.Scale(diff, scale);
            graphView.UpdateViewTransform(position, scale);

            ChangeMouseCursorTo(graphView.elementPanel, (int)MouseCursor.Pan);

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            var position = graphView.ContentViewContainer.transform.position;
            var scale = graphView.ContentViewContainer.transform.scale;
            graphView.Dispatch(new ReframeGraphViewCommand(position, scale));

            m_Active = false;
            target.ReleaseMouse();

            ChangeMouseCursorTo(graphView.elementPanel, (int)MouseCursor.Arrow);

            e.StopPropagation();
        }
    }
}
