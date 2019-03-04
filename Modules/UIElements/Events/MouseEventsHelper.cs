// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    struct MousePositionTracker
    {
        public static Vector2 mousePosition { get; private set; }

        public static IPanel panel { get; private set; }

        public static void SaveMousePosition(Vector2 position, IPanel panel)
        {
            mousePosition = position;
            MousePositionTracker.panel = panel;
        }
    }

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
}
