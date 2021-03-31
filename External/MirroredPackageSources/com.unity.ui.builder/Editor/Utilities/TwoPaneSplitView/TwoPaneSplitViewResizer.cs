using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class TwoPaneSplitViewResizer : MouseManipulator
    {
        Vector2 m_Start;
        protected bool m_Active;
        TwoPaneSplitView m_SplitView;

        int m_Direction;
        TwoPaneSplitViewOrientation m_Orientation;

        VisualElement fixedPane => m_SplitView.fixedPane;
        VisualElement flexedPane => m_SplitView.flexedPane;

        float fixedPaneMinDimension
        {
            get
            {
                if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                    return fixedPane.resolvedStyle.minWidth.value;
                else
                    return fixedPane.resolvedStyle.minHeight.value;
            }
        }

        float flexedPaneMinDimension
        {
            get
            {
                if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
                    return flexedPane.resolvedStyle.minWidth.value;
                else
                    return flexedPane.resolvedStyle.minHeight.value;
            }
        }

        public TwoPaneSplitViewResizer(TwoPaneSplitView splitView, int dir, TwoPaneSplitViewOrientation orientation)
        {
            m_Orientation = orientation;
            m_SplitView = splitView;
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
            float oldDimension = m_Orientation == TwoPaneSplitViewOrientation.Horizontal
                ? fixedPane.resolvedStyle.width
                : fixedPane.resolvedStyle.height;
            float newDimension = oldDimension + delta;

            if (newDimension < oldDimension && newDimension < fixedPaneMinDimension)
                newDimension = fixedPaneMinDimension;

            float maxDimension = m_Orientation == TwoPaneSplitViewOrientation.Horizontal
                ? m_SplitView.resolvedStyle.width
                : m_SplitView.resolvedStyle.height;
            maxDimension -= flexedPaneMinDimension;
            if (newDimension > oldDimension && newDimension > maxDimension)
                newDimension = maxDimension;

            if (m_Orientation == TwoPaneSplitViewOrientation.Horizontal)
            {
                fixedPane.style.width = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                    target.style.left = newDimension;
                else
                    target.style.left = m_SplitView.resolvedStyle.width - newDimension;
            }
            else
            {
                fixedPane.style.height = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                    target.style.top = newDimension;
                else
                    target.style.top = m_SplitView.resolvedStyle.height - newDimension;
            }
            m_SplitView.fixedPaneDimension = newDimension;
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
            float mouseDiff = diff.x;
            if (m_Orientation == TwoPaneSplitViewOrientation.Vertical)
                mouseDiff = diff.y;

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
