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
                // Event occured outside the window.
                // Send event to visual tree root and
                // don't modify evt.propagateToIMGUI.
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
                    basePanel.ClearCachedElementUnderPointer(evt);
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

            // Root IMGUI is the container that handles all the GUIView/DockArea logic for EditorWindows.
            var rootIMGUI = basePanel?.rootIMGUIContainer;

            if (!evt.isPropagationStopped && panel != null && evt.imguiEvent != null && rootIMGUI != null)
            {
                // If root IMGUI doesn't use event, send it to other IMGUIs down the line.
                if (evt.propagateToIMGUI ||
                    evt.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                    evt.eventTypeId == MouseLeaveWindowEvent.TypeId() ||
                    evt.target == rootIMGUI
                )
                {
                    evt.skipElements.Add(evt.target);
                    EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
                }
                // If non-root element doesn't use event, send it to root IMGUI.
                // This is necessary for some behaviors like dropdown menus in IMGUI.
                // See case : https://fogbugz.unity3d.com/f/cases/1223087/
                else
                {
                    evt.skipElements.Add(evt.target);
                    if (!evt.Skip(rootIMGUI))
                    {
                        // Only permit switching the focus to another IMGUIContainer if the event target was not focusable
                        // and was itself an IMGUIContainer
                        Focusable f = evt.target as Focusable;
                        bool canAffectFocus = f != null && !f.focusable && f.isIMGUIContainer;
                        rootIMGUI.SendEventToIMGUI(evt, canAffectFocus);
                    }
                }
                if (evt.imguiEvent.rawType == EventType.Used)
                    evt.StopPropagation();
            }

            evt.stopDispatch = true;
        }
    }
}
