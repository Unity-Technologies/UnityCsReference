// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class DebuggerSplitter : VisualElement
    {
        public VisualElement leftPane { get; private set; }
        public VisualElement rightPane { get; private set; }

        private VisualElement m_DragLine;

        public DebuggerSplitter()
        {
            name = "unity-debugger-splitter";

            leftPane = new VisualElement();

            // TODO: would be nice but we had issues with the
            // search bar results count indicator appearing and
            // disappearing causing the left pane to change size
            // slightly.
            //leftPane.style.flexGrow = 1;
            leftPane.style.width = 400;
            Add(leftPane);

            var dragLineAnchor = new VisualElement();
            dragLineAnchor.name = "unity-debugger-splitter-dragline-anchor";
            Add(dragLineAnchor);

            m_DragLine = new VisualElement();
            m_DragLine.name = "unity-debugger-splitter-dragline";
            m_DragLine.AddManipulator(new SquareResizer(leftPane));
            dragLineAnchor.Add(m_DragLine);

            rightPane = new VisualElement();
            rightPane.name = "unity-debugger-splitter__right-pane";
            Add(rightPane);
        }

        class SquareResizer : MouseManipulator
        {
            private Vector2 m_Start;
            protected bool m_Active;
            private VisualElement m_LeftPane;

            public SquareResizer(VisualElement leftPane)
            {
                m_LeftPane = leftPane;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                m_Active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {
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

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    m_Start = e.localMousePosition;

                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.localMousePosition - m_Start;

                // TODO: See TODO above.
                //m_LeftPane.style.flexGrow = float.NaN;
                m_LeftPane.style.width = m_LeftPane.layout.width + diff.x;

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }
    }
}
