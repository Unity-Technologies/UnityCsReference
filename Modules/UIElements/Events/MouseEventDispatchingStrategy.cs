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
            IMouseEvent mouseEvent = evt as IMouseEvent;

            if (mouseEvent == null)
                return;

            BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

            // update element under mouse and fire necessary events

            bool shouldRecomputeTopElementUnderMouse = true;
            if ((IMouseEventInternal)mouseEvent != null)
            {
                shouldRecomputeTopElementUnderMouse =
                    ((IMouseEventInternal)mouseEvent).recomputeTopElementUnderMouse;
            }

            VisualElement elementUnderMouse = shouldRecomputeTopElementUnderMouse
                ? basePanel?.Pick(mouseEvent.mousePosition)
                : basePanel?.GetTopElementUnderPointer(PointerId.mousePointerId);

            if (evt.target == null && elementUnderMouse != null)
            {
                evt.propagateToIMGUI = false;
                evt.target = elementUnderMouse;
            }
            else if (evt.target == null && elementUnderMouse == null)
            {
                // Don't modify evt.propagateToIMGUI.
                evt.target = panel?.visualTree;
            }
            else if (evt.target != null)
            {
                evt.propagateToIMGUI = false;
            }

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

            if (evt.target != null)
            {
                EventDispatchUtilities.PropagateEvent(evt);
            }

            if (!evt.isPropagationStopped && panel != null)
            {
                if (evt.propagateToIMGUI ||
                    evt.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                    evt.eventTypeId == MouseLeaveWindowEvent.TypeId()
                )
                {
                    EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
                }
            }

            evt.stopDispatch = true;
        }
    }
}
