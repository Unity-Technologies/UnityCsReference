// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
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
                IMGUIContainer imguiContainer = panel.focusController.GetLeafFocusedElement() as IMGUIContainer;

                if (imguiContainer != null)
                {
                    if (!evt.Skip(imguiContainer) && imguiContainer.SendEventToIMGUI(evt))
                    {
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }

                    if (!evt.isPropagationStopped && evt.propagateToIMGUI)
                    {
                        evt.skipElements.Add(imguiContainer);
                        EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
                    }
                }
                else if (panel.focusController.GetLeafFocusedElement() != null)
                {
                    evt.target = panel.focusController.GetLeafFocusedElement();
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
