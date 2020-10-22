namespace UnityEngine.UIElements
{
    class NavigationEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is INavigationEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            if (panel != null)
            {
                evt.target = panel.focusController.GetLeafFocusedElement() ?? panel.visualTree;
                EventDispatchUtilities.PropagateEvent(evt);
            }

            evt.propagateToIMGUI = false;
            evt.stopDispatch = true;
        }
    }
}
