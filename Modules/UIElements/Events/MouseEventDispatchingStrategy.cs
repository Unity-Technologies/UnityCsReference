// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

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

            // FIXME: we should not change hover state when capture is true.
            // However, when doing drag and drop, drop target should be highlighted.

            // TODO when EditorWindow is docked MouseLeaveWindow is not always sent
            // this is a problem in itself but it could leave some elements as "hover"

            BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;

            if (basePanel != null && (evt.eventTypeId == MouseLeaveWindowEvent.TypeId() || evt.eventTypeId == DragExitedEvent.TypeId()))
            {
                basePanel.SetElementUnderMouse(null, evt);
            }
            else
            {
                // update element under mouse and fire necessary events
                if (basePanel != null)
                {
                    bool shouldRecomputeTopElementUnderMouse = true;
                    if ((IMouseEventInternal)mouseEvent != null)
                    {
                        shouldRecomputeTopElementUnderMouse =
                            ((IMouseEventInternal)mouseEvent).recomputeTopElementUnderMouse;
                    }

                    VisualElement elementUnderMouse = shouldRecomputeTopElementUnderMouse ?
                        basePanel.Pick(mouseEvent.mousePosition) :
                        basePanel.topElementUnderMouse;

                    if (evt.target == null)
                    {
                        evt.target = elementUnderMouse;
                    }

                    // Because events are queued, we can set the element under mouse
                    // right now, instead of waiting after the PropagateEvent() as we
                    // did before.
                    basePanel.SetElementUnderMouse(elementUnderMouse, evt);
                }

                if (evt.target != null)
                {
                    evt.propagateToIMGUI = false;
                    EventDispatchUtilities.PropagateEvent(evt);
                }
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
