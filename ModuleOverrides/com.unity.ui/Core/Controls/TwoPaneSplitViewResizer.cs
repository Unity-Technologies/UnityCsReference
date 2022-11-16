// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal class TwoPaneSplitViewResizer : PointerManipulator
    {
        Vector3 m_Start;
        protected bool m_Active;
        TwoPaneSplitView m_SplitView;

        int m_Direction;
        TwoPaneSplitViewOrientation orientation => m_SplitView.orientation;

        VisualElement fixedPane => m_SplitView.fixedPane;
        VisualElement flexedPane => m_SplitView.flexedPane;

        float fixedPaneMinDimension
        {
            get
            {
                if (orientation == TwoPaneSplitViewOrientation.Horizontal)
                    return fixedPane.resolvedStyle.minWidth.value;
                else
                    return fixedPane.resolvedStyle.minHeight.value;
            }
        }

        float fixedPaneMargins
        {
            get
            {
                if (orientation == TwoPaneSplitViewOrientation.Horizontal)
                    return fixedPane.resolvedStyle.marginLeft + fixedPane.resolvedStyle.marginRight;
                else
                    return fixedPane.resolvedStyle.marginTop + fixedPane.resolvedStyle.marginBottom;
            }
        }

        float flexedPaneMinDimension
        {
            get
            {
                if (orientation == TwoPaneSplitViewOrientation.Horizontal)
                    return flexedPane.resolvedStyle.minWidth.value;
                else
                    return flexedPane.resolvedStyle.minHeight.value;
            }
        }

        float flexedPaneMargin
        {
            get
            {
                if (orientation == TwoPaneSplitViewOrientation.Horizontal)
                    return flexedPane.resolvedStyle.marginLeft + flexedPane.resolvedStyle.marginRight;
                else
                    return flexedPane.resolvedStyle.marginTop + flexedPane.resolvedStyle.marginBottom;
            }
        }

        public TwoPaneSplitViewResizer(TwoPaneSplitView splitView, int dir)
        {
            m_SplitView = splitView;
            m_Direction = dir;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_Active = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        public void ApplyDelta(float delta)
        {
            float oldDimension = orientation == TwoPaneSplitViewOrientation.Horizontal
                ? fixedPane.resolvedStyle.width
                : fixedPane.resolvedStyle.height;
            float newDimension = oldDimension + delta;

            if (newDimension < oldDimension && newDimension < fixedPaneMinDimension)
                newDimension = fixedPaneMinDimension;

            float maxDimension = orientation == TwoPaneSplitViewOrientation.Horizontal
                ? m_SplitView.resolvedStyle.width
                : m_SplitView.resolvedStyle.height;
            maxDimension -= flexedPaneMinDimension + flexedPaneMargin + fixedPaneMargins;
            if (newDimension > oldDimension && newDimension > maxDimension)
                newDimension = maxDimension;

            if (orientation == TwoPaneSplitViewOrientation.Horizontal)
            {
                fixedPane.style.width = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                    target.style.left = newDimension + fixedPaneMargins;
                else
                    target.style.left = m_SplitView.resolvedStyle.width - newDimension - fixedPaneMargins;
            }
            else
            {
                fixedPane.style.height = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                    target.style.top = newDimension + fixedPaneMargins;
                else
                    target.style.top = m_SplitView.resolvedStyle.height - newDimension - fixedPaneMargins;
            }
            m_SplitView.fixedPaneDimension = newDimension;
        }

        protected void OnPointerDown(PointerDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                m_Start = e.localPosition;

                m_Active = true;
                target.CapturePointer(e.pointerId);
                e.StopPropagation();
            }
        }

        protected void OnPointerMove(PointerMoveEvent e)
        {
            if (!m_Active || !target.HasPointerCapture(e.pointerId))
                return;

            Vector2 diff = e.localPosition - m_Start;
            var mouseDiff = diff.x;
            if (orientation == TwoPaneSplitViewOrientation.Vertical)
                mouseDiff = diff.y;

            var delta = m_Direction * mouseDiff;

            ApplyDelta(delta);

            e.StopPropagation();
        }

        protected void OnPointerUp(PointerUpEvent e)
        {
            if (!m_Active || !target.HasPointerCapture(e.pointerId) || !CanStopManipulation(e))
                return;

            m_Active = false;
            target.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }
    }
}
