// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class CommandEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is ICommandEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            if (panel != null)
            {
                IMGUIContainer imguiContainer = panel.focusController.focusedElement as IMGUIContainer;

                if (imguiContainer != null)
                {
                    if (imguiContainer != evt.skipElement && imguiContainer.HandleIMGUIEvent(evt.imguiEvent))
                    {
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                }
                else if (panel.focusController.focusedElement != null)
                {
                    evt.target = panel.focusController.focusedElement;
                    EventDispatchUtilities.PropagateEvent(evt);
                }
                else
                {
                    EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
                }
            }

            evt.propagateToIMGUI = false;
            evt.stopDispatch = true;
        }
    }
}
