using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class EditorContextualMenuManager : ContextualMenuManager
    {
        public override void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler)
        {
            if (evt == null)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                if (evt.eventTypeId == MouseDownEvent.TypeId())
                {
                    MouseDownEvent e = evt as MouseDownEvent;

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
                if (evt.eventTypeId == MouseUpEvent.TypeId())
                {
                    MouseUpEvent e = evt as MouseUpEvent;
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
            menu.DoDisplayEditorMenu(triggerEvent);
        }
    }
}
