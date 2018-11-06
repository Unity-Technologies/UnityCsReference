// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class DropdownMenu
    {
        public class EventInfo
        {
            public EventModifiers modifiers { get; }
            public Vector2 mousePosition { get; }
            public Vector2 localMousePosition { get; }
            char character { get; }
            KeyCode keyCode { get; }

            public EventInfo(EventBase e)
            {
                IMouseEvent mouseEvent = e as IMouseEvent;

                if (mouseEvent != null)
                {
                    mousePosition = mouseEvent.mousePosition;
                    localMousePosition = mouseEvent.localMousePosition;
                    modifiers = mouseEvent.modifiers;
                    character = '\0';
                    keyCode = KeyCode.None;
                }
                else
                {
                    IKeyboardEvent keyboardEvent = e as IKeyboardEvent;
                    if (keyboardEvent != null)
                    {
                        character = keyboardEvent.character;
                        keyCode = keyboardEvent.keyCode;
                        modifiers = keyboardEvent.modifiers;
                        mousePosition = Vector2.zero;
                        localMousePosition = Vector2.zero;
                    }
                }
            }
        }

        public abstract class MenuItem {}

        public class Separator : MenuItem
        {
            public string subMenuPath;

            public Separator(string subMenuPath)
            {
                this.subMenuPath = subMenuPath;
            }
        }

        public class MenuAction : MenuItem
        {
            [Flags]
            public enum StatusFlags
            {
                Normal = 0, // Enabled, unchecked, shown
                Disabled = 1,
                Checked = 2,
                Hidden = 4
            }

            public string name;
            public StatusFlags status { get; private set; }

            public EventInfo eventInfo { get; private set; }

            public object userData { get; private set; }
            Action<MenuAction> actionCallback;
            Func<MenuAction, StatusFlags> actionStatusCallback;

            // ActionStatusCallback for action that are always enabled.
            public static StatusFlags AlwaysEnabled(MenuAction a) { return StatusFlags.Normal; }

            // ActionStatusCallback for action that are always disabled.
            public static StatusFlags AlwaysDisabled(MenuAction a) { return StatusFlags.Disabled; }

            public MenuAction(string actionName, Action<MenuAction> actionCallback, Func<MenuAction, StatusFlags> actionStatusCallback, object userData = null)
            {
                name = actionName;
                this.actionCallback = actionCallback;
                this.actionStatusCallback = actionStatusCallback;
                this.userData = userData;
            }

            public void UpdateActionStatus(EventInfo eventInfo)
            {
                this.eventInfo = eventInfo;
                status = actionStatusCallback?.Invoke(this) ?? StatusFlags.Hidden;
            }

            public void Execute()
            {
                if (actionCallback != null)
                {
                    actionCallback(this);
                }
            }
        }

        List<MenuItem> menuItems = new List<MenuItem>();
        EventInfo m_EventInfo;

        public List<MenuItem> MenuItems()
        {
            return menuItems;
        }

        public void AppendAction(string actionName, Action<MenuAction> action, Func<MenuAction, MenuAction.StatusFlags> actionStatusCallback, object userData = null)
        {
            MenuAction menuAction = new MenuAction(actionName, action, actionStatusCallback, userData);
            menuItems.Add(menuAction);
        }

        public void InsertAction(int atIndex, string actionName, Action<MenuAction> action, Func<MenuAction, MenuAction.StatusFlags> actionStatusCallback, object userData = null)
        {
            MenuAction menuAction = new MenuAction(actionName, action, actionStatusCallback, userData);
            menuItems.Insert(atIndex, menuAction);
        }

        public void AppendSeparator(string subMenuPath = null)
        {
            if (menuItems.Count > 0 && !(menuItems[menuItems.Count - 1] is Separator))
            {
                Separator separator = new Separator(subMenuPath == null ? String.Empty : subMenuPath);
                menuItems.Add(separator);
            }
        }

        public void InsertSeparator(string subMenuPath, int atIndex)
        {
            if (atIndex > 0 && atIndex <= menuItems.Count && !(menuItems[atIndex - 1] is Separator))
            {
                Separator separator = new Separator(subMenuPath == null ? String.Empty : subMenuPath);
                menuItems.Insert(atIndex, separator);
            }
        }

        public void RemoveItemAt(int index)
        {
            menuItems.RemoveAt(index);
        }

        public void PrepareForDisplay(EventBase e)
        {
            m_EventInfo = e != null ? new EventInfo(e) : null;

            if (menuItems.Count == 0)
                return;

            foreach (MenuItem item in menuItems)
            {
                MenuAction action = item as MenuAction;
                if (action != null)
                {
                    action.UpdateActionStatus(m_EventInfo);
                }
            }

            if (menuItems[menuItems.Count - 1] is Separator)
            {
                menuItems.RemoveAt(menuItems.Count - 1);
            }
        }
    }
}
