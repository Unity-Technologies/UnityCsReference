using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class EditorMenuExtensions
    {
        static IGenericMenu PrepareMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menu.PrepareForDisplay(triggerEvent);

            var genericMenu = new GenericOSMenu();
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

    internal class GenericOSMenu : IGenericMenu
    {
        GenericMenu m_GenericMenu;

        public GenericOSMenu()
        {
            m_GenericMenu = new GenericMenu();
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
