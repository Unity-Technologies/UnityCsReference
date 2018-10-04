// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
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
                EventDispatchUtilities.PropagateEvent(evt);
                evt.propagateToIMGUI = false;
            }
            else
            {
                if (!evt.isPropagationStopped && panel != null)
                {
                    if (evt.propagateToIMGUI ||
                        evt.GetEventTypeId() == MouseEnterWindowEvent.TypeId() ||
                        evt.GetEventTypeId() == MouseLeaveWindowEvent.TypeId()
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
