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
        }

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

            IEventHandler capturingElement = panel?.GetCapturingElement(PointerId.mousePointerId);
            if (capturingElement == null)
            {
                return;
            }

            // Release mouse capture if capture element is not in a panel.
            VisualElement captureVE = capturingElement as VisualElement;
            if (evt.eventTypeId != MouseCaptureOutEvent.TypeId() && captureVE != null && captureVE.panel == null)
            {
                captureVE.ReleaseMouse();
                return;
            }

            if (panel != null && captureVE != null && captureVE.panel.contextType != panel.contextType)
            {
                return;
            }

            IMouseEvent mouseEvent = evt as IMouseEvent;

            if (mouseEvent != null && (evt.target == null || evt.target == capturingElement))
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

            if ((captureBehavior & EventBehavior.IsCapturable) == EventBehavior.IsCapturable)
            {
                BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

                if (mouseEvent != null && basePanel != null)
                {
                    bool shouldRecomputeTopElementUnderMouse = (mouseEvent as IMouseEventInternal)?.recomputeTopElementUnderMouse ?? true;

                    if (shouldRecomputeTopElementUnderMouse)
                        basePanel.RecomputeTopElementUnderPointer(mouseEvent.mousePosition, evt);
                }

                evt.dispatch = true;
                evt.target = capturingElement;
                (capturingElement as CallbackEventHandler)?.HandleEventAtTargetPhase(evt);
                // Do further processing with a target computed the usual way.
                // However, if IsSentExclusivelyToCapturingElement, the only thing remaining to do is ExecuteDefaultAction,
                // which should be done with mouseCapture as the target.
                if ((captureBehavior & EventBehavior.IsSentExclusivelyToCapturingElement) != EventBehavior.IsSentExclusivelyToCapturingElement)
                {
                    evt.target = null;
                }

                evt.currentTarget = null;
                evt.propagationPhase = PropagationPhase.None;
                evt.dispatch = false;

                // Do not call HandleEvent again for this element.
                evt.skipElements.Add(capturingElement);

                evt.stopDispatch = (captureBehavior & EventBehavior.IsSentExclusivelyToCapturingElement) == EventBehavior.IsSentExclusivelyToCapturingElement;

                if (evt.target is IMGUIContainer)
                {
                    evt.propagateToIMGUI = true;
                    evt.skipElements.Add(evt.target);
                }
                else
                {
                    evt.propagateToIMGUI = false;
                }
            }
        }
    }
}
