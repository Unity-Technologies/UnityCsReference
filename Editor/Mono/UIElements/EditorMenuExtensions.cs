// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    static class EditorMenuExtensions
    {
        static GenericMenu PrepareMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menu.PrepareForDisplay(triggerEvent);

            var genericMenu = new GenericMenu();
            foreach (var item in menu.MenuItems())
            {
                var action = item as DropdownMenu.MenuAction;
                if (action != null)
                {
                    if ((action.status & DropdownMenu.MenuAction.StatusFlags.Hidden) == DropdownMenu.MenuAction.StatusFlags.Hidden)
                    {
                        continue;
                    }

                    bool isChecked = (action.status & DropdownMenu.MenuAction.StatusFlags.Checked) == DropdownMenu.MenuAction.StatusFlags.Checked;

                    if ((action.status & DropdownMenu.MenuAction.StatusFlags.Disabled) == DropdownMenu.MenuAction.StatusFlags.Disabled)
                    {
                        genericMenu.AddDisabledItem(new GUIContent(action.name));
                    }
                    else
                    {
                        genericMenu.AddItem(new GUIContent(action.name), isChecked, () =>
                        {
                            action.Execute();
                        });
                    }
                }
                else
                {
                    var separator = item as DropdownMenu.Separator;
                    if (separator != null)
                    {
                        genericMenu.AddSeparator(separator.subMenuPath);
                    }
                }
            }

            return genericMenu;
        }

        public static void DoDisplayEditorMenu(this DropdownMenu menu, Vector2 position)
        {
            PrepareMenu(menu, null).DropDown(new Rect(position, Vector2.zero));
        }

        public static void DoDisplayEditorMenu(this DropdownMenu menu, EventBase triggerEvent)
        {
            GenericMenu genericMenu = PrepareMenu(menu, triggerEvent);

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
