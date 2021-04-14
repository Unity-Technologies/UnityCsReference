// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
