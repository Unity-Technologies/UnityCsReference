// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class EditorContextualMenuManager : ContextualMenuManager
    {
        public override void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler)
        {
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                if (evt.eventTypeId == PointerDownEvent.TypeId() ||
                    evt.eventTypeId == PointerMoveEvent.TypeId() && ((PointerMoveEvent)evt).isPointerDown)
                {
                    IPointerEvent e = (IPointerEvent) evt;

                    if (e.button == (int)MouseButton.RightMouse ||
                        (e.button == (int)MouseButton.LeftMouse && e.modifiers == EventModifiers.Control))
                    {
                        DisplayMenu(evt, eventHandler);
                        evt.StopPropagation();
                        return;
                    }
                }
            }
            else
            {
                if (evt.eventTypeId == PointerUpEvent.TypeId() ||
                    evt.eventTypeId == PointerMoveEvent.TypeId() && ((PointerMoveEvent)evt).isPointerUp)
                {
                    IPointerEvent e = (IPointerEvent) evt;
                    if (e.button == (int)MouseButton.RightMouse)
                    {
                        DisplayMenu(evt, eventHandler);
                        evt.StopPropagation();
                        return;
                    }
                }
            }

            if (evt.eventTypeId == KeyUpEvent.TypeId())
            {
                KeyUpEvent e = evt as KeyUpEvent;
                if (e.keyCode == KeyCode.Menu)
                {
                    DisplayMenu(evt, eventHandler);
                    evt.StopPropagation();
                }
            }
        }

        protected internal override void DoDisplayMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menu.DisplayEditorMenu(triggerEvent);
        }
    }
}
