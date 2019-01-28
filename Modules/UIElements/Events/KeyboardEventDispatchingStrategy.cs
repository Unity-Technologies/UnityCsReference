// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    class KeyboardEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return evt is IKeyboardEvent;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            if (panel != null)
            {
                if (panel.focusController.GetLeafFocusedElement() != null)
                {
                    IMGUIContainer imguiContainer = panel.focusController.GetLeafFocusedElement() as IMGUIContainer;

                    if (imguiContainer != null)
                    {
                        // THINK ABOUT THIS PF: shouldn't we allow for the trickleDown dispatch phase?
                        if (!evt.Skip(imguiContainer) && imguiContainer.HandleIMGUIEvent(evt.imguiEvent))
                        {
                            evt.StopPropagation();
                            evt.PreventDefault();
                        }
                    }
                    else
                    {
                        evt.target = panel.focusController.GetLeafFocusedElement();
                        EventDispatchUtilities.PropagateEvent(evt);
                    }
                }
                else
                {
                    evt.target = panel.visualTree;
                    EventDispatchUtilities.PropagateEvent(evt);

                    if (!evt.isPropagationStopped)
                        EventDispatchUtilities.PropagateToIMGUIContainer(panel.visualTree, evt);
                }
            }

            evt.propagateToIMGUI = false;
            evt.stopDispatch = true;
        }
    }
}
