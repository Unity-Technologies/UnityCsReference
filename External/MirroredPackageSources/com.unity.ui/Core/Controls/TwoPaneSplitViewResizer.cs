namespace UnityEngine.UIElements
{
    internal class TwoPaneSplitViewResizer : PointerManipulator
    {
        Vector3 m_Start;
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
            if (m_Orientation == TwoPaneSplitViewOrientation.Vertical)
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
