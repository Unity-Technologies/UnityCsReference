namespace UnityEngine.UIElements
{
    class IMGUIEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is IMGUIEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            if (panel != null)
            {
                EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
            }

            evt.propagateToIMGUI = false;
            evt.stopDispatch = true;
        }
    }
}
