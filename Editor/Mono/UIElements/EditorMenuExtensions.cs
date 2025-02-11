// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class EditorMenuExtensions
    {
        // DropdownMenu
        static IGenericMenu PrepareMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menu.PrepareForDisplay(triggerEvent);

            var genericMenu = new GenericOSMenu(menu.allowDuplicateNames);
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

                    bool isChecked = action.status.HasFlag(DropdownMenuAction.Status.Checked);

                    if ((action.status & DropdownMenuAction.Status.Disabled) == DropdownMenuAction.Status.Disabled)
                    {
                        genericMenu.AddDisabledItem(action.name, isChecked);
                    }
                    else
                    {
                        genericMenu.AddItem(action.name, isChecked, () =>
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
            var genericMenu = PrepareMenu(menu, triggerEvent);

            var position = Vector2.zero;
            if (triggerEvent is IMouseEvent mouseEvent)
            {
                position = mouseEvent.mousePosition;
            }
            else if (triggerEvent is IPointerEvent pointerEvent)
            {
                position = pointerEvent.position;
            }
            else if (triggerEvent.elementTarget != null)
            {
                position = triggerEvent.elementTarget.layout.center;
            }

            genericMenu.DropDown(new Rect(position, Vector2.zero));

        }
    }

    internal class GenericOSMenu : IGenericMenu
    {
        GenericMenu m_GenericMenu;

        public GenericOSMenu(bool allowDuplicateNames = false)
        {
            m_GenericMenu = new GenericMenu();
            m_GenericMenu.allowDuplicateNames = allowDuplicateNames;
        }

        public GenericOSMenu(GenericMenu genericMenu)
        {
            m_GenericMenu = genericMenu;
        }

        public void AddItem(string itemName, bool isChecked, System.Action action)
        {
            if (action == null)
                m_GenericMenu.AddItem(new GUIContent(itemName), isChecked, null);
            else
                m_GenericMenu.AddItem(new GUIContent(itemName), isChecked, action.Invoke);
        }

        public void AddItem(string itemName, bool isChecked, System.Action<object> action, object data)
        {
            if (action == null)
                m_GenericMenu.AddItem(new GUIContent(itemName), isChecked, null, data);
            else
                m_GenericMenu.AddItem(new GUIContent(itemName), isChecked, action.Invoke, data);
        }

        public void AddDisabledItem(string itemName, bool isChecked)
        {
            m_GenericMenu.AddDisabledItem(new GUIContent(itemName), isChecked);
        }

        public void AddSeparator(string path)
        {
            m_GenericMenu.AddSeparator(path);
        }

        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            m_GenericMenu.DropDown(position);
        }
    }
}
