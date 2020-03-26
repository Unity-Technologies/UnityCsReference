// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            if (evt.target is IMGUIContainer)
            {
                evt.propagateToIMGUI = true;
                evt.skipElements.Add(evt.target);
            }

            return IsDone(evt);
        }

        static bool SendEventToIMGUIContainer(EventBase evt, BaseVisualElementPanel panel)
        {
            if (evt.propagateToIMGUI ||
                evt.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                evt.eventTypeId == MouseLeaveWindowEvent.TypeId()
            )
            {
                EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
            }
            else
            {
                // Send the events to the GUIView container so that it can process them.
                // This is necessary for some behaviors like dropdown menus in IMGUI.
                // See case : https://fogbugz.unity3d.com/f/cases/1223087/
                var topLevelIMGUI = panel.rootIMGUIContainer;
                if (topLevelIMGUI != null && !evt.Skip(topLevelIMGUI) && evt.imguiEvent != null)
                {
                    topLevelIMGUI.SendEventToIMGUI(evt, false);
                }
            }

            return IsDone(evt);
        }

        static void SetBestTargetForEvent(EventBase evt, BaseVisualElementPanel panel)
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

        static void UpdateElementUnderMouse(EventBase evt, BaseVisualElementPanel panel, out VisualElement elementUnderMouse)
        {
            bool shouldRecomputeTopElementUnderMouse = (evt as IMouseEventInternal)?.recomputeTopElementUnderMouse ?? true;

            elementUnderMouse = shouldRecomputeTopElementUnderMouse
                ? panel.Pick(((IMouseEvent)evt).mousePosition)
                : panel.GetTopElementUnderPointer(PointerId.mousePointerId);

            // If mouse leaves the window, make sure element under mouse is null.
            // However, if pressed button != 0, we are getting a MouseLeaveWindowEvent as part of
            // of a drag and drop operation, at the very beginning of the drag. Since
            // we are not really exiting the window, we do not want to set the element
            // under mouse to null in this case.
            if (evt.eventTypeId == MouseLeaveWindowEvent.TypeId() &&
                (evt as MouseLeaveWindowEvent).pressedButtons == 0)
            {
                panel.SetElementUnderPointer(null, evt);
            }
            else if (shouldRecomputeTopElementUnderMouse)
            {
                panel.SetElementUnderPointer(elementUnderMouse, evt);
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
