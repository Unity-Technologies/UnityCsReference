// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    class DefaultDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            if (evt is IMGUIEvent)
            {
                return false;
            }

            return true;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            if (evt.target != null)
            {
                evt.propagateToIMGUI = evt.target is IMGUIContainer;
                EventDispatchUtilities.PropagateEvent(evt);
            }
            else
            {
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
            }

            evt.stopDispatch = true;
        }
    }
}
