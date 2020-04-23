using System;

namespace UnityEngine.UIElements
{
    class PointerCaptureDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is IPointerEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            IPointerEvent pointerEvent = evt as IPointerEvent;
            if (pointerEvent == null)
            {
                return;
            }

            IEventHandler targetOverride = panel.GetCapturingElement(pointerEvent.pointerId);
            if (targetOverride == null)
            {
                return;
            }

            // Release pointer capture if capture element is not in a panel.
            VisualElement captureVE = targetOverride as VisualElement;
            if (evt.eventTypeId != PointerCaptureOutEvent.TypeId() && captureVE != null && captureVE.panel == null)
            {
                panel.ReleasePointer(pointerEvent.pointerId);
                return;
            }

            if (evt.target != null && evt.target != targetOverride)
            {
                return;
            }

            if (evt.eventTypeId != PointerCaptureEvent.TypeId() && evt.eventTypeId != PointerCaptureOutEvent.TypeId())
            {
                panel.ProcessPointerCapture(pointerEvent.pointerId);
            }

            // Exclusive processing by capturing element.
            evt.dispatch = true;
            evt.target = targetOverride;
            evt.currentTarget = targetOverride;
            evt.propagationPhase = PropagationPhase.AtTarget;

            targetOverride.HandleEvent(evt);

            // Leave evt.target = originalCaptureElement for ExecuteDefaultAction()
            evt.currentTarget = null;
            evt.propagationPhase = PropagationPhase.None;
            evt.dispatch = false;
            evt.stopDispatch = true;
            evt.propagateToIMGUI = false;
        }
    }
}
