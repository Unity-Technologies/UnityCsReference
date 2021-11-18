// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            if (evt.target is VisualElement ve && ve.panel == panel)
            {
                evt.propagateToIMGUI = ve.isIMGUIContainer;
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
