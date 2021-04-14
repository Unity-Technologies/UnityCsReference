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
                var leafFocusElement = panel.focusController.GetLeafFocusedElement();

                if (leafFocusElement != null)
                {
                    if (leafFocusElement.isIMGUIContainer)
                    {
                        IMGUIContainer imguiContainer =  (IMGUIContainer)leafFocusElement;
                        if (!evt.Skip(imguiContainer) && imguiContainer.SendEventToIMGUI(evt))
                        {
                            evt.StopPropagation();
                            evt.PreventDefault();
                        }
                    }
                    else
                    {
                        evt.target = leafFocusElement;
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
