// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class TwoPaneSplitViewResizer : PointerManipulator
    {
        // Distance between the drag line and the anchor
        private const float k_DragLineTolerance = 1f;
        Vector3 m_Start;
        protected bool m_Active;
        TwoPaneSplitView m_SplitView;

        int m_Direction;
        float m_Delta;

        TwoPaneSplitViewOrientation orientation => m_SplitView.orientation;

        VisualElement fixedPane => m_SplitView.fixedPane;
        VisualElement flexedPane => m_SplitView.flexedPane;
        public float delta => m_Delta;

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

            float adjustedfixedPaneMinDimension = fixedPaneMinDimension;

            // Track the size of the anchor so that the cannot be hidden when moving it to the right.
            if (m_SplitView.fixedPaneIndex == 1)
                adjustedfixedPaneMinDimension += orientation == TwoPaneSplitViewOrientation.Horizontal
                    ? target.worldBound.width + Math.Abs(m_SplitView.dragLine.resolvedStyle.left)
                    : target.worldBound.height + Math.Abs(m_SplitView.dragLine.resolvedStyle.top);

            if (newDimension < oldDimension && newDimension < adjustedfixedPaneMinDimension)
                newDimension = adjustedfixedPaneMinDimension;

            float maxDimension = orientation == TwoPaneSplitViewOrientation.Horizontal
                ? m_SplitView.resolvedStyle.width
                : m_SplitView.resolvedStyle.height;
            maxDimension -= flexedPaneMinDimension + flexedPaneMargin + fixedPaneMargins;

            // Track the size of the anchor so that the cannot be hidden when moving it to the right.
            // Need to take into account the width of the anchor and the width of the dragline minus
            // how far back/up the dragline is
            if (m_SplitView.fixedPaneIndex == 0)
                maxDimension -= orientation == TwoPaneSplitViewOrientation.Horizontal
                    ? Math.Abs(target.worldBound.width  - (m_SplitView.dragLine.resolvedStyle.width - Math.Abs(m_SplitView.dragLine.resolvedStyle.left)))
                    : Math.Abs(target.worldBound.height  - (m_SplitView.dragLine.resolvedStyle.height - Math.Abs(m_SplitView.dragLine.resolvedStyle.top)));

            if (newDimension > oldDimension && newDimension > maxDimension)
                newDimension = maxDimension;

            if (orientation == TwoPaneSplitViewOrientation.Horizontal)
            {
                fixedPane.style.width = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                {
                    var newLeftValue = newDimension + fixedPaneMargins;
                    if (newLeftValue >= fixedPaneMinDimension)
                        target.style.left = newLeftValue;
                }
                else
                {
                    var newLeftValue = m_SplitView.resolvedStyle.width - newDimension - fixedPaneMargins;
                    // Ensure the dragLine respects the flexed pane's min dimension
                    if (newLeftValue >= flexedPaneMinDimension + flexedPaneMargin)
                        target.style.left = newLeftValue;
                }

            }
            else
            {
                fixedPane.style.height = newDimension;
                if (m_SplitView.fixedPaneIndex == 0)
                {
                    //Same as horizontal
                    var newTopValue = newDimension + fixedPaneMargins;
                    if (newTopValue >= fixedPaneMinDimension)
                        target.style.top = newTopValue;
                }
                else
                {
                    var newTopValue = m_SplitView.resolvedStyle.height - newDimension - fixedPaneMargins;
                    // Ensure the dragLine respects the flexed pane's min dimension
                    if (newTopValue >= flexedPaneMinDimension + flexedPaneMargin)
                        target.style.top = newTopValue;
                }
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

            // A cases where the anchor can go past the dropdown
            var dragLineIsBeforeAnchor = orientation == TwoPaneSplitViewOrientation.Horizontal
                ? m_SplitView.dragLine.worldBound.x < target.worldBound.x
                : m_SplitView.dragLine.worldBound.y < target.worldBound.y;

            var distanceBetweenDragLineAndAnchor = orientation == TwoPaneSplitViewOrientation.Horizontal
                ? Math.Abs(target.worldBound.x - m_SplitView.dragLine.worldBound.x)
                : Math.Abs(target.worldBound.y - m_SplitView.dragLine.worldBound.y);

            var dragLineOffset = orientation == TwoPaneSplitViewOrientation.Horizontal
                ? m_SplitView.dragLine.resolvedStyle.left
                : m_SplitView.dragLine.resolvedStyle.top;

            if (dragLineIsBeforeAnchor && (Math.Abs(dragLineOffset) + k_DragLineTolerance) <= distanceBetweenDragLineAndAnchor)
            {
                InterruptPointerMove(e);
                return;
            }

            Vector2 diff = e.localPosition - m_Start;
            var mouseDiff = diff.x;
            if (orientation == TwoPaneSplitViewOrientation.Vertical)
                mouseDiff = diff.y;

            m_Delta = m_Direction * mouseDiff;

            ApplyDelta(m_Delta);

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

        protected void InterruptPointerMove(PointerMoveEvent e)
        {
            if (!CanStopManipulation(e))
                return;

            m_Active = false;
            target.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }
    }
}
