// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class ContextualMenu
    {
        public abstract class MenuItem {}

        public class Separator : MenuItem {}

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

            Action<EventBase> actionCallback;
            Func<EventBase, StatusFlags> actionStatusCallback;

            // ActionStatusCallback for action that are always enabled.
            public static StatusFlags AlwaysEnabled(EventBase e) { return StatusFlags.Normal; }

            // ActionStatusCallback for action that are always disabled.
            public static StatusFlags AlwaysDisabled(EventBase e) { return StatusFlags.Disabled; }

            public MenuAction(string actionName, Action<EventBase> actionCallback, Func<EventBase, StatusFlags> actionStatusCallback)
            {
                name = actionName;
                this.actionCallback = actionCallback;
                this.actionStatusCallback = actionStatusCallback;
            }

            public void UpdateActionStatus(EventBase e)
            {
                status = (actionStatusCallback != null ? actionStatusCallback(e) : StatusFlags.Hidden);
            }

            public void Execute(EventBase e)
            {
                if (actionCallback != null)
                {
                    actionCallback(e);
                }
            }
        }

        List<MenuItem> menuItems = new List<MenuItem>();

        public List<MenuItem> MenuItems()
        {
            return menuItems;
        }

        public void AppendAction(string actionName, Action<EventBase> action, Func<EventBase, MenuAction.StatusFlags> actionStatusCallback)
        {
            MenuAction menuAction = new MenuAction(actionName, action, actionStatusCallback);
            menuItems.Add(menuAction);
        }

        public void InsertAction(string actionName, Action<EventBase> action, Func<EventBase, MenuAction.StatusFlags> actionStatusCallback, int atIndex)
        {
            MenuAction menuAction = new MenuAction(actionName, action, actionStatusCallback);
            menuItems.Insert(atIndex, menuAction);
        }

        public void AppendSeparator()
        {
            if (menuItems.Count > 0 && !(menuItems[menuItems.Count - 1] is Separator))
            {
                Separator separator = new Separator();
                menuItems.Add(separator);
            }
        }

        public void InsertSeparator(int atIndex)
        {
            if (atIndex > 0 && atIndex <= menuItems.Count && !(menuItems[atIndex - 1] is Separator))
            {
                Separator separator = new Separator();
                menuItems.Insert(atIndex, separator);
            }
        }

        public void PrepareForDisplay(EventBase e)
        {
            foreach (MenuItem item in menuItems)
            {
                MenuAction action = item as MenuAction;
                if (action != null)
                {
                    action.UpdateActionStatus(e);
                }
            }

            if (menuItems[menuItems.Count - 1] is Separator)
            {
                menuItems.RemoveAt(menuItems.Count - 1);
            }
        }
    }
}
