// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    class MouseEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is IMouseEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            SetBestTargetForEvent(evt, panel);
            SendEventToTarget(evt, panel);
            evt.stopDispatch = true;
        }

        static void SendEventToTarget(EventBase evt, IPanel panel)
        {
            SendEventToRegularTarget(evt, panel);

            if (evt.imguiEvent?.rawType == EventType.Used)
                evt.StopPropagation();

            if (evt.isPropagationStopped)
                return;

            SendEventToIMGUIContainer(evt, panel);
        }

        static void SendEventToRegularTarget(EventBase evt, IPanel panel)
        {
            if (evt.target != null)
            {
                EventDispatchUtilities.PropagateEvent(evt);
            }
        }

        static void SendEventToIMGUIContainer(EventBase evt, IPanel panel)
        {
            if (panel != null)
            {
                if (evt.target != null && evt.target is IMGUIContainer)
                {
                    evt.propagateToIMGUI = true;
                    evt.skipElements.Add(evt.target);
                }
                if (evt.propagateToIMGUI ||
                    evt.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                    evt.eventTypeId == MouseLeaveWindowEvent.TypeId()
                )
                {
                    EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
                }
            }
        }

        static void SetBestTargetForEvent(EventBase evt, IPanel panel)
        {
            UpdateElementUnderMouse(evt, panel, out VisualElement elementUnderMouse);

            if (evt.target == null && elementUnderMouse != null)
            {
                evt.propagateToIMGUI = false;
                evt.target = elementUnderMouse;
            }
            else if (evt.target == null && elementUnderMouse == null)
            {
                // Event occured outside the window.
                // Send event to visual tree root and
                // don't modify evt.propagateToIMGUI.
                evt.target = panel?.visualTree;
            }
            else if (evt.target != null)
            {
                evt.propagateToIMGUI = false;
            }
        }

        static void UpdateElementUnderMouse(EventBase evt, IPanel panel, out VisualElement elementUnderMouse)
        {
            IMouseEvent mouseEvent = evt as IMouseEvent;
            BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

            bool shouldRecomputeTopElementUnderMouse = true;
            if ((IMouseEventInternal)mouseEvent != null)
            {
                shouldRecomputeTopElementUnderMouse =
                    ((IMouseEventInternal)mouseEvent).recomputeTopElementUnderMouse;
            }

            elementUnderMouse = shouldRecomputeTopElementUnderMouse
                ? basePanel?.Pick(mouseEvent.mousePosition)
                : basePanel?.GetTopElementUnderPointer(PointerId.mousePointerId);

            if (basePanel != null)
            {
                // If mouse leaves the window, make sure element under mouse is null.
                // However, if pressed button != 0, we are getting a MouseLeaveWindowEvent as part of
                // of a drag and drop operation, at the very beginning of the drag. Since
                // we are not really exiting the window, we do not want to set the element
                // under mouse to null in this case.
                if (evt.eventTypeId == MouseLeaveWindowEvent.TypeId() &&
                    (evt as MouseLeaveWindowEvent).pressedButtons == 0)
                {
                    basePanel.SetElementUnderPointer(null, evt);
                }
                else if (shouldRecomputeTopElementUnderMouse)
                {
                    basePanel.SetElementUnderPointer(elementUnderMouse, evt);
                }
            }
        }
    }
}
