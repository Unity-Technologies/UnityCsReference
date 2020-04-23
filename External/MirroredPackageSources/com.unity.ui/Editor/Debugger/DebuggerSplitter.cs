using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    internal class DebuggerSplitter : VisualElement
    {
        public VisualElement leftPane { get; private set; }
        public VisualElement rightPane { get; private set; }

        private VisualElement m_DragLine;

        [SerializeField]
        private float leftPaneWidth;

        public DebuggerSplitter()
        {
            name = "unity-debugger-splitter";
            viewDataKey = "unity-uie-debugger-splitter";

            leftPane = new VisualElement();
            Add(leftPane);

            var dragLineAnchor = new VisualElement();
            dragLineAnchor.name = "unity-debugger-splitter-dragline-anchor";
            Add(dragLineAnchor);

            m_DragLine = new VisualElement();
            m_DragLine.name = "unity-debugger-splitter-dragline";
            m_DragLine.AddManipulator(new SquareResizer(this));
            dragLineAnchor.Add(m_DragLine);

            rightPane = new VisualElement();
            rightPane.name = "unity-debugger-splitter__right-pane";
            Add(rightPane);

            leftPaneWidth = 400;
        }

        override internal void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            leftPane.style.width = leftPaneWidth;
        }

        class SquareResizer : MouseManipulator
        {
            private Vector2 m_Start;
            protected bool m_Active;
            private DebuggerSplitter m_Splitter;

            public SquareResizer(DebuggerSplitter splitter)
            {
                m_Splitter = splitter;
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

                m_Splitter.leftPane.style.width = m_Splitter.leftPane.layout.width + diff.x;

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();

                m_Splitter.leftPaneWidth = m_Splitter.leftPane.resolvedStyle.width;
                m_Splitter.SaveViewData();
            }
        }
    }
}
