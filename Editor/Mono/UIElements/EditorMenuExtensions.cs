// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class EditorMenuExtensions
    {
        // DropdownMenu
        static GenericOSMenu PrepareMenu(DropdownMenu menu, EventBase triggerEvent)
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

        public static void DoDisplayEditorMenu(this DropdownMenu menu, Rect rect, VisualElement ve)
        {
            PrepareMenu(menu, null).DropDown(rect, ve);
        }

        // This is for backward compatibility with code triggering from imgui, but it wont allow spanning the menu from code (menu item, across window)
        // Try using DoDisplayEditorMenu that takes an EventBase or an visualElement instead
        [Obsolete("Use DoDisplayEditorMenu instead")]
        public static void DoDisplayEditorMenuFromImGUI(this DropdownMenu menu, Rect rect)
        {
            PrepareMenu(menu, null).DropDownIMGUI(rect);
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

            genericMenu.DropDown(new Rect(position, Vector2.zero), triggerEvent.elementTarget);
        }

        public static Rect GUIToScreenRect(VisualElement targetElement, Rect position)
        {
            var panel = targetElement.elementPanel;

            if (panel.contextType == ContextType.Editor && panel.ownerObject is View view)
            {
                // Convert first the postion in the panel to the position in UI pixels as per the editor's window definition
                // This will not work in test where we disconnect the panel DPI from the window DPI

                position.x *= panel.scale;
                position.y *= panel.scale;
                position.width *= panel.scale;
                position.height *= panel.scale;

                // Add the offset of window to get the position in screen space
                // It include the position from the guiView to the root of the containerWindow and from the containerWindow to the screen
                position.position += view.screenPosition.position;
            }

            return position;
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
            if(targetElement is null || targetElement.panel == null)
            {
                Debug.LogError("Cannot show dropdown menu with a target visualElement not in a panel");
                return;
            }

            position = EditorMenuExtensions.GUIToScreenRect(targetElement, position);
            m_GenericMenu.DropDownScreenSpace(position, false);
        }

        [Obsolete("Use DropDown(Rect position, VisualElement targetElement, bool anchored) instead")]
        public void DropDownIMGUI(Rect position, bool anchored = false)
        {
            m_GenericMenu.DropDown(position, anchored);
        }
    }
}
