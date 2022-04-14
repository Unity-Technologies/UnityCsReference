// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    class KeyboardEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return (evt is IKeyboardEvent);
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            if (panel != null)
            {
                // Note that here we always overwrite evt.target, even if it's already set. This might be required by
                // some pre-existing editor content, but it's not standard among the dispatching strategies and should
                // probably be changed in the future.

                var leafFocusElement = panel.focusController.GetLeafFocusedElement();

                if (leafFocusElement != null)
                {
                    evt.target = leafFocusElement;

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
