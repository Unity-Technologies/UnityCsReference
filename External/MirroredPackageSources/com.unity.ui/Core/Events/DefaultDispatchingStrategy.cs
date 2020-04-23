namespace UnityEngine.UIElements
{
    class DefaultDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return !(evt is IMGUIEvent);
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
