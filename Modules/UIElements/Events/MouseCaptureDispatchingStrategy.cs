// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    class MouseCaptureDispatchingStrategy : IEventDispatchingStrategy
    {
        [Flags]
        enum EventBehavior
        {
            None = 0,
            IsCapturable = 1,
            IsSentExclusivelyToCapturingElement = 2
        };

        public bool CanDispatchEvent(EventBase evt)
        {
            // Send all IMGUI events (for backward compatibility) and MouseEvents with null
            // target (because thats what we want to do in the new system)
            // to the capture, if there is one. Note that events coming from IMGUI have
            // their target set to null.

            return evt is IMouseEvent || evt.imguiEvent != null;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            EventBehavior captureBehavior = EventBehavior.None;

            if (MouseCaptureController.mouseCapture == null)
            {
                return;
            }

            // Release mouse capture if capture element is not in a panel.
            VisualElement captureVE = MouseCaptureController.mouseCapture as VisualElement;
            if (evt.eventTypeId != MouseCaptureOutEvent.TypeId() && captureVE != null && captureVE.panel == null)
            {
                MouseCaptureController.ReleaseMouse();
                return;
            }

            if (panel != null && captureVE != null && captureVE.panel.contextType != panel.contextType)
            {
                return;
            }

            IMouseEvent mouseEvent = evt as IMouseEvent;

            if (mouseEvent != null && (evt.target == null || evt.target == MouseCaptureController.mouseCapture))
            {
                // Exclusive processing by capturing element.
                captureBehavior = EventBehavior.IsCapturable;
                captureBehavior |= EventBehavior.IsSentExclusivelyToCapturingElement;
            }
            else if (evt.imguiEvent != null && evt.target == null)
            {
                // Non exclusive processing by capturing element.
                captureBehavior = EventBehavior.IsCapturable;
            }

            if (evt.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                evt.eventTypeId == MouseLeaveWindowEvent.TypeId() ||
                evt.eventTypeId == WheelEvent.TypeId())
            {
                captureBehavior = EventBehavior.None;
            }

            // FIXME ugly
            evt.skipElement = null;

            if ((captureBehavior & EventBehavior.IsCapturable) == EventBehavior.IsCapturable)
            {
                BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

                if (mouseEvent != null && basePanel != null)
                {
                    VisualElement elementUnderMouse = basePanel.Pick(mouseEvent.mousePosition);
                    basePanel.SetElementUnderMouse(elementUnderMouse, evt);
                }

                IEventHandler originalCaptureElement = MouseCaptureController.mouseCapture;

                evt.dispatch = true;
                evt.target = MouseCaptureController.mouseCapture;
                evt.currentTarget = MouseCaptureController.mouseCapture;
                evt.propagationPhase = PropagationPhase.AtTarget;
                MouseCaptureController.mouseCapture.HandleEvent(evt);

                // Do further processing with a target computed the usual way.
                // However, if mouseEventWasCaptured, the only thing remaining to do is ExecuteDefaultAction,
                // which should be done with mouseCapture as the target.
                if ((captureBehavior & EventBehavior.IsSentExclusivelyToCapturingElement) != EventBehavior.IsSentExclusivelyToCapturingElement)
                {
                    evt.target = null;
                }

                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
                evt.dispatch = false;

                // Do not call HandleEvent again for this element.
                evt.skipElement = originalCaptureElement;

                evt.stopDispatch = (captureBehavior & EventBehavior.IsSentExclusivelyToCapturingElement) == EventBehavior.IsSentExclusivelyToCapturingElement;
                evt.propagateToIMGUI = false;
            }
        }
    }
}
