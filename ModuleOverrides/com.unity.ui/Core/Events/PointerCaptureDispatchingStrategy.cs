// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

            // Case 1342115: mouse position is in local panel coordinates; sending event to a target from a different
            // panel will lead to a wrong position, so we don't allow it. Note that in general the mouse-down-move-up
            // sequence still works properly because the OS captures the mouse on the starting EditorWindow.
            if (panel != null && captureVE != null && captureVE.panel != panel)
            {
                return;
            }

            if (evt.eventTypeId != PointerCaptureEvent.TypeId() && evt.eventTypeId != PointerCaptureOutEvent.TypeId())
            {
                panel.ProcessPointerCapture(pointerEvent.pointerId);
            }

            // Case 1353921: this will enforce PointerEnter/Out events even during pointer capture.
            // According to the W3 standard (https://www.w3.org/TR/pointerevents3/#the-pointerout-event), these events
            // are *not* supposed to occur, but we have been sending MouseEnter/Out events during mouse capture
            // since the early days of UI Toolkit, and users have been relying on it.
            if (panel is BaseVisualElementPanel basePanel)
            {
                bool shouldRecomputeTopElementUnderPointer = (pointerEvent as IPointerEventInternal)?.recomputeTopElementUnderPointer ?? true;

                if (shouldRecomputeTopElementUnderPointer)
                    basePanel.RecomputeTopElementUnderPointer(pointerEvent.pointerId, pointerEvent.position, evt);
            }

            // Exclusive processing by capturing element.
            evt.dispatch = true;
            evt.target = targetOverride;
            evt.skipDisabledElements = false;

            (targetOverride as CallbackEventHandler)?.HandleEventAtTargetPhase(evt);

            // Leave evt.target = originalCaptureElement for ExecuteDefaultAction()
            evt.currentTarget = null;
            evt.propagationPhase = PropagationPhase.None;
            evt.dispatch = false;
            evt.stopDispatch = true;
            evt.propagateToIMGUI = false;
        }
    }
}
