// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class VisualSplitter : VisualElement
    {
        const float k_SplitterSize = 7f;

        private readonly VisualElement m_Dragger;
        private readonly Resizer m_Resizer;

        public VisualElement fixedPane { get; private set; }
        public VisualElement flexedPane { get; private set; }

        public VisualSplitter(VisualElement fixedPane, VisualElement flexedPane, FlexDirection direction, float size = 0f)
        {
            this.fixedPane = fixedPane;
            this.flexedPane = flexedPane;
            m_Resizer = new Resizer(this, 1, direction);

            flexedPane.style.flexGrow = 1;

            m_Dragger = new VisualElement();
            m_Dragger.style.position = Position.Absolute;
            m_Dragger.AddManipulator(m_Resizer);

            var splitter = new VisualElement() { name = "Splitter" };
            if (direction == FlexDirection.Column)
            {
                m_Dragger.style.height = k_SplitterSize;
                m_Dragger.style.top = -Mathf.RoundToInt(k_SplitterSize / 2f);
                m_Dragger.style.left = 0f;
                m_Dragger.style.right = 0f;
                m_Dragger.style.cursor = LoadCursor(MouseCursor.ResizeVertical);
                //m_Dragger.style.backgroundColor = Color.green;

                if (size != 0)
                    splitter.style.height = size;
            }
            else if (direction == FlexDirection.Row)
            {
                m_Dragger.style.width = k_SplitterSize;
                m_Dragger.style.left = -Mathf.RoundToInt(k_SplitterSize / 2f);
                m_Dragger.style.top = 0f;
                m_Dragger.style.bottom = 0f;
                m_Dragger.style.cursor = LoadCursor(MouseCursor.ResizeHorizontal);
                //m_Dragger.style.backgroundColor = Color.red;

                if (size != 0)
                    splitter.style.width = size;
            }

            Add(fixedPane);
            Add(splitter);
            Add(flexedPane);
            Add(m_Dragger);

            RegisterCallback<GeometryChangedEvent>(OnSizeChange);
            fixedPane.RegisterCallback<GeometryChangedEvent>(OnSizeChange);

            style.flexGrow = 1;
            style.flexDirection = direction;
        }

        private UnityEngine.UIElements.Cursor LoadCursor(MouseCursor cursorStyle)
        {
            var cursorType = typeof(UnityEngine.UIElements.Cursor);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var defaultCursorIdProperty = cursorType.GetProperty("defaultCursorId", bindingFlags);
            object boxed = new UnityEngine.UIElements.Cursor();
            defaultCursorIdProperty.SetValue(boxed, (int)cursorStyle, null);
            return (UnityEngine.UIElements.Cursor)boxed;
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            if (style.flexDirection.value == FlexDirection.Row)
                m_Dragger.style.left = fixedPane.resolvedStyle.width;
            else
                m_Dragger.style.top = fixedPane.resolvedStyle.height;

            // Handle reducing window size with a min size on flexed pane
            var delta = style.flexDirection == FlexDirection.Row ? evt.newRect.width - evt.oldRect.width : evt.newRect.height - evt.oldRect.height;
            if (delta < 0) // if the window is reducing, we just use the last position, it will handle min size automatically in ApplyDelta
                m_Resizer.ApplyDelta(0);
        }

        class Resizer : MouseManipulator
        {
            private bool m_Active;
            private Vector2 m_Start;
            private readonly int m_Direction;
            private readonly FlexDirection m_Orientation;
            private readonly VisualSplitter m_Splitter;

            private VisualElement fixedPane => m_Splitter.fixedPane;
            private VisualElement flexedPane => m_Splitter.flexedPane;

            private float fixedPaneMinDimension => m_Orientation == FlexDirection.Row ? fixedPane.resolvedStyle.minWidth.value : fixedPane.resolvedStyle.minHeight.value;
            private float flexedPaneMinDimension => m_Orientation == FlexDirection.Row ? flexedPane.resolvedStyle.minWidth.value : flexedPane.resolvedStyle.minHeight.value;

            public Resizer(VisualSplitter splitView, int dir, FlexDirection orientation)
            {
                m_Orientation = orientation;
                m_Splitter = splitView;
                m_Direction = dir;
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

            public void ApplyDelta(float delta)
            {
                float oldDimension = m_Orientation == FlexDirection.Row ? fixedPane.resolvedStyle.width : fixedPane.resolvedStyle.height;
                float newDimension = oldDimension + delta;

                if (newDimension < oldDimension && newDimension < fixedPaneMinDimension)
                    newDimension = fixedPaneMinDimension;

                float maxDimension = m_Orientation == FlexDirection.Row ? m_Splitter.resolvedStyle.width : m_Splitter.resolvedStyle.height;
                maxDimension -= flexedPaneMinDimension;
                if (newDimension > oldDimension && newDimension > maxDimension)
                    newDimension = maxDimension;

                if (m_Orientation == FlexDirection.Row)
                {
                    fixedPane.style.width = newDimension;
                    target.style.left = newDimension;
                }
                else
                {
                    fixedPane.style.height = newDimension;
                    target.style.top = newDimension;
                }
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                }
                else if (CanStartManipulation(e))
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
                float mouseDiff = m_Orientation == FlexDirection.Row ? diff.x : diff.y;
                float delta = m_Direction * mouseDiff;

                ApplyDelta(delta);
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
