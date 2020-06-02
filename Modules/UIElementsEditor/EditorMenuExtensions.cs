// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class EditorMenuExtensions
    {
        static GenericMenu PrepareMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menu.PrepareForDisplay(triggerEvent);

            var genericMenu = new GenericMenu();
            foreach (var item in menu.MenuItems())
            {
                var action = item as DropdownMenuAction;
                if (action != null)
                {
                    if ((action.status & DropdownMenuAction.Status.Hidden) == DropdownMenuAction.Status.Hidden
                        || action.status == 0)
                    {
                        continue;
                    }


                    bool isChecked = (action.status & DropdownMenuAction.Status.Checked) == DropdownMenuAction.Status.Checked;

                    if ((action.status & DropdownMenuAction.Status.Disabled) == DropdownMenuAction.Status.Disabled)
                    {
                        genericMenu.AddDisabledItem(new GUIContent(action.name), isChecked);
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
                    var separator = item as DropdownMenuSeparator;
                    if (separator != null)
                    {
                        genericMenu.AddSeparator(separator.subMenuPath);
                    }
                }
            }

            return genericMenu;
        }

        public static void DoDisplayEditorMenu(this DropdownMenu menu, Rect rect)
        {
            PrepareMenu(menu, null).DropDown(rect);
        }

        public static void DoDisplayEditorMenu(this DropdownMenu menu, EventBase triggerEvent)
        {
            GenericMenu genericMenu = PrepareMenu(menu, triggerEvent);

            Vector2 position = Vector2.zero;
            if (triggerEvent is IMouseEvent)
            {
                position = ((IMouseEvent)triggerEvent).mousePosition;
            }
            else if (triggerEvent is IPointerEvent)
            {
                position = ((IPointerEvent)triggerEvent).position;
            }
            else if (triggerEvent.target is VisualElement)
            {
                position = ((VisualElement)triggerEvent.target).layout.center;
            }

            genericMenu.DropDown(new Rect(position, Vector2.zero));
        }
    }
}
