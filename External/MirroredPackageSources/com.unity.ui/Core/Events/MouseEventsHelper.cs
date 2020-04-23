using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    static class MouseEventsHelper
    {
        internal static void SendEnterLeave<TLeaveEvent, TEnterEvent>(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, IMouseEvent triggerEvent, Vector2 mousePosition) where TLeaveEvent : MouseEventBase<TLeaveEvent>, new() where TEnterEvent : MouseEventBase<TEnterEvent>, new()
        {
            if (previousTopElementUnderMouse != null && previousTopElementUnderMouse.panel == null)
            {
                // If previousTopElementUnderMouse has been removed from panel,
                // do as if there is no element under the mouse.
                previousTopElementUnderMouse = null;
            }

            // We want to find the common ancestor CA of previousTopElementUnderMouse and currentTopElementUnderMouse,
            // send Leave (MouseLeave or DragLeave) events to elements between CA and previousTopElementUnderMouse
            // and send Enter (MouseEnter or DragEnter) events to elements between CA and currentTopElementUnderMouse.

            int prevDepth = 0;
            var p = previousTopElementUnderMouse;
            while (p != null)
            {
                prevDepth++;
                p = p.hierarchy.parent;
            }

            int currDepth = 0;
            var c = currentTopElementUnderMouse;
            while (c != null)
            {
                currDepth++;
                c = c.hierarchy.parent;
            }

            p = previousTopElementUnderMouse;
            c = currentTopElementUnderMouse;

            while (prevDepth > currDepth)
            {
                using (var leaveEvent = MouseEventBase<TLeaveEvent>.GetPooled(triggerEvent, mousePosition, false))
                {
                    leaveEvent.target = p;
                    p.SendEvent(leaveEvent);
                }

                prevDepth--;
                p = p.hierarchy.parent;
            }

            // We want to send enter events after all the leave events.
            // We will store the elements being entered in this list.
            List<VisualElement> enteringElements = VisualElementListPool.Get(currDepth);

            while (currDepth > prevDepth)
            {
                enteringElements.Add(c);

                currDepth--;
                c = c.hierarchy.parent;
            }

            // Now p and c are at the same depth. Go up the tree until p == c.
            while (p != c)
            {
                using (var leaveEvent = MouseEventBase<TLeaveEvent>.GetPooled(triggerEvent, mousePosition, false))
                {
                    leaveEvent.target = p;
                    p.SendEvent(leaveEvent);
                }

                enteringElements.Add(c);

                p = p.hierarchy.parent;
                c = c.hierarchy.parent;
            }

            for (var i = enteringElements.Count - 1; i >= 0; i--)
            {
                using (var enterEvent = MouseEventBase<TEnterEvent>.GetPooled(triggerEvent, mousePosition, false))
                {
                    enterEvent.target = enteringElements[i];
                    enteringElements[i].SendEvent(enterEvent);
                }
            }

            VisualElementListPool.Release(enteringElements);
        }

        internal static void SendMouseOverMouseOut(VisualElement previousTopElementUnderMouse, VisualElement currentTopElementUnderMouse, IMouseEvent triggerEvent, Vector2 mousePosition)
        {
            // Send MouseOut event for element no longer under the mouse.
            if (previousTopElementUnderMouse != null && previousTopElementUnderMouse.panel != null)
            {
                using (var outEvent = MouseOutEvent.GetPooled(triggerEvent, mousePosition, false))
                {
                    outEvent.target = previousTopElementUnderMouse;
                    previousTopElementUnderMouse.SendEvent(outEvent);
                }
            }

            // Send MouseOver event for element now under the mouse
            if (currentTopElementUnderMouse != null)
            {
                using (var overEvent = MouseOverEvent.GetPooled(triggerEvent, mousePosition, false))
                {
                    overEvent.target = currentTopElementUnderMouse;
                    currentTopElementUnderMouse.SendEvent(overEvent);
                }
            }
        }
    }

    static class PointerEventsHelper
    {
        internal static void SendEnterLeave<TLeaveEvent, TEnterEvent>(VisualElement previousTopElementUnderPointer, VisualElement currentTopElementUnderPointer, IPointerEvent triggerEvent, Vector2 position, int pointerId) where TLeaveEvent : PointerEventBase<TLeaveEvent>, new() where TEnterEvent : PointerEventBase<TEnterEvent>, new()
        {
            if (previousTopElementUnderPointer != null && previousTopElementUnderPointer.panel == null)
            {
                // If previousTopElementUnderPointer has been removed from panel,
                // do as if there is no element under the pointer.
                previousTopElementUnderPointer = null;
            }

            // We want to find the common ancestor CA of previousTopElementUnderPointer and currentTopElementUnderPointer,
            // send Leave (PointerLeave) events to elements between CA and previousTopElementUnderPointer
            // and send Enter (PointerEnter) events to elements between CA and currentTopElementUnderPointer.

            int prevDepth = 0;
            var p = previousTopElementUnderPointer;
            while (p != null)
            {
                prevDepth++;
                p = p.hierarchy.parent;
            }

            int currDepth = 0;
            var c = currentTopElementUnderPointer;
            while (c != null)
            {
                currDepth++;
                c = c.hierarchy.parent;
            }

            p = previousTopElementUnderPointer;
            c = currentTopElementUnderPointer;

            while (prevDepth > currDepth)
            {
                using (var leaveEvent = PointerEventBase<TLeaveEvent>.GetPooled(triggerEvent, position, pointerId))
                {
                    leaveEvent.target = p;
                    p.SendEvent(leaveEvent);
                }

                prevDepth--;
                p = p.hierarchy.parent;
            }

            // We want to send enter events after all the leave events.
            // We will store the elements being entered in this list.
            List<VisualElement> enteringElements = VisualElementListPool.Get(currDepth);

            while (currDepth > prevDepth)
            {
                enteringElements.Add(c);

                currDepth--;
                c = c.hierarchy.parent;
            }

            // Now p and c are at the same depth. Go up the tree until p == c.
            while (p != c)
            {
                using (var leaveEvent = PointerEventBase<TLeaveEvent>.GetPooled(triggerEvent, position, pointerId))
                {
                    leaveEvent.target = p;
                    p.SendEvent(leaveEvent);
                }

                enteringElements.Add(c);

                p = p.hierarchy.parent;
                c = c.hierarchy.parent;
            }

            for (var i = enteringElements.Count - 1; i >= 0; i--)
            {
                using (var enterEvent = PointerEventBase<TEnterEvent>.GetPooled(triggerEvent, position, pointerId))
                {
                    enterEvent.target = enteringElements[i];
                    enteringElements[i].SendEvent(enterEvent);
                }
            }

            VisualElementListPool.Release(enteringElements);
        }

        internal static void SendOverOut(VisualElement previousTopElementUnderPointer, VisualElement currentTopElementUnderPointer, IPointerEvent triggerEvent, Vector2 position, int pointerId)
        {
            // Send PointerOutEvent for element no longer under the mouse.
            if (previousTopElementUnderPointer != null && previousTopElementUnderPointer.panel != null)
            {
                using (var outEvent = PointerOutEvent.GetPooled(triggerEvent, position, pointerId))
                {
                    outEvent.target = previousTopElementUnderPointer;
                    previousTopElementUnderPointer.SendEvent(outEvent);
                }
            }

            // Send PointerOverEvent for element now under the mouse
            if (currentTopElementUnderPointer != null)
            {
                using (var overEvent = PointerOverEvent.GetPooled(triggerEvent, position, pointerId))
                {
                    overEvent.target = currentTopElementUnderPointer;
                    currentTopElementUnderPointer.SendEvent(overEvent);
                }
            }
        }
    }
}
