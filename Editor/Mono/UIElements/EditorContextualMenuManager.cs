// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class EditorContextualMenuManager : ContextualMenuManager
    {
        public override void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler)
        {
            if (evt.GetEventTypeId() == MouseUpEvent.TypeId())
            {
                MouseUpEvent e = evt as MouseUpEvent;
                if (e.button == (int)MouseButton.RightMouse)
                {
                    DisplayMenu(evt, eventHandler);
                    evt.StopPropagation();
                }
            }
            else if (evt.GetEventTypeId() == KeyUpEvent.TypeId())
            {
                KeyUpEvent e = evt as KeyUpEvent;
                if (e.keyCode == KeyCode.Menu)
                {
                    DisplayMenu(evt, eventHandler);
                    evt.StopPropagation();
                }
            }
        }

        protected override void DoDisplayMenu(ContextualMenu menu, EventBase triggerEvent)
        {
            var genericMenu = new GenericMenu();
            foreach (var item in menu.MenuItems())
            {
                var action = item as ContextualMenu.MenuAction;
                if (action != null)
                {
                    if ((action.status & ContextualMenu.MenuAction.StatusFlags.Hidden) == ContextualMenu.MenuAction.StatusFlags.Hidden)
                    {
                        continue;
                    }

                    bool isChecked = (action.status & ContextualMenu.MenuAction.StatusFlags.Checked) == ContextualMenu.MenuAction.StatusFlags.Checked;

                    if ((action.status & ContextualMenu.MenuAction.StatusFlags.Disabled) == ContextualMenu.MenuAction.StatusFlags.Disabled)
                    {
                        genericMenu.AddDisabledItem(new GUIContent(action.name));
                    }
                    else
                    {
                        genericMenu.AddItem(new GUIContent(action.name), isChecked, () =>
                            {
                                action.Execute(triggerEvent);
                            });
                    }
                }
                else
                {
                    genericMenu.AddSeparator(string.Empty);
                }
            }

            Vector2 position = Vector2.zero;
            if (triggerEvent is IMouseEvent)
            {
                position = ((IMouseEvent)triggerEvent).mousePosition;
            }
            else if (triggerEvent.target is VisualElement)
            {
                position = ((VisualElement)triggerEvent.target).layout.center;
            }

            genericMenu.DropDown(new Rect(position, Vector2.zero));
        }
    }
}
