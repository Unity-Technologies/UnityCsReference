using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    class MouseEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is IMouseEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel iPanel)
        {
            if (iPanel != null)
            {
                Assert.IsTrue(iPanel is BaseVisualElementPanel);
                var panel = (BaseVisualElementPanel)iPanel;
                SetBestTargetForEvent(evt, panel);
                SendEventToTarget(evt, panel);
            }
            evt.stopDispatch = true;
        }

        static bool SendEventToTarget(EventBase evt, BaseVisualElementPanel panel)
        {
            return SendEventToRegularTarget(evt, panel) ||
                SendEventToIMGUIContainer(evt, panel);
        }

        static bool SendEventToRegularTarget(EventBase evt, BaseVisualElementPanel panel)
        {
            if (evt.target == null)
                return false;

            EventDispatchUtilities.PropagateEvent(evt);

            return IsDone(evt);
        }

        static bool SendEventToIMGUIContainer(EventBase evt, BaseVisualElementPanel panel)
        {
            if (evt.imguiEvent == null)
                return false;

            // Root IMGUI is the container that handles all the GUIView/DockArea logic for EditorWindows.
            var rootIMGUI = panel.rootIMGUIContainer;
            if (rootIMGUI == null)
                return false;

            // If root IMGUI doesn't use event, send it to other IMGUIs down the line.
            if (evt.propagateToIMGUI ||
                evt.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                evt.eventTypeId == MouseLeaveWindowEvent.TypeId())
            {
                evt.skipElements.Add(evt.target);
                EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
            }

            return IsDone(evt);
        }

        static void SetBestTargetForEvent(EventBase evt, BaseVisualElementPanel panel)
        {
            UpdateElementUnderMouse(evt, panel, out VisualElement elementUnderMouse);

            if (evt.target != null)
            {
                evt.propagateToIMGUI = false;
            }
            else if (elementUnderMouse != null)
            {
                evt.propagateToIMGUI = false;
                evt.target = elementUnderMouse;
            }
            else
            {
                // Event occured outside the window.
                // Send event to visual tree root and
                // don't modify evt.propagateToIMGUI.
                evt.target = panel?.visualTree;
            }
        }

        static void UpdateElementUnderMouse(EventBase evt, BaseVisualElementPanel panel, out VisualElement elementUnderMouse)
        {
            bool shouldRecomputeTopElementUnderMouse = (evt as IMouseEventInternal)?.recomputeTopElementUnderMouse ?? true;

            elementUnderMouse = shouldRecomputeTopElementUnderMouse
                ? panel.RecomputeTopElementUnderPointer(((IMouseEvent)evt).mousePosition, evt)
                : panel.GetTopElementUnderPointer(PointerId.mousePointerId);

            // If mouse leaves the window, make sure element under mouse is null.
            // However, if pressed button != 0, we are getting a MouseLeaveWindowEvent as part of
            // of a drag and drop operation, at the very beginning of the drag. Since
            // we are not really exiting the window, we do not want to set the element
            // under mouse to null in this case.
            if (evt.eventTypeId == MouseLeaveWindowEvent.TypeId() &&
                (evt as MouseLeaveWindowEvent).pressedButtons == 0)
            {
                panel.ClearCachedElementUnderPointer(evt);
            }
        }

        static bool IsDone(EventBase evt)
        {
            if (evt.imguiEvent?.rawType == EventType.Used)
                evt.StopPropagation();
            return evt.isPropagationStopped;
        }
    }
}
