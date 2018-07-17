// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    [Obsolete("ContextualMenu has been deprecated. Use DropdownMenu instead.", true)]
    public class ContextualMenu
    {
        [Obsolete("ContextualMenu.EventInfo has been deprecated. Use DropdownMenu.EventInfo instead.", true)]
        public class EventInfo
        {
            public EventModifiers modifiers { get; }
            public Vector2 mousePosition { get; }
            public Vector2 localMousePosition { get; }

            public EventInfo(EventBase e)
            {
            }
        }

        [Obsolete("ContextualMenu.MenuItem has been deprecated. Use DropdownMenu.MenuItem instead.", true)]
        public abstract class MenuItem {}

        [Obsolete("ContextualMenu.Separator has been deprecated. Use DropdownMenu.Separator instead.", true)]
        public class Separator : MenuItem
        {
            public string subMenuPath;

            public Separator(string subMenuPath)
            {
            }
        }

        [Obsolete("ContextualMenu.MenuAction has been deprecated. Use DropdownMenu.MenuAction instead.", true)]
        public class MenuAction : MenuItem
        {
            [Flags]
            [Obsolete("ContextualMenu.MenuAction.StatusFlags has been deprecated. Use DropdownMenu.MenuAction.StatusFlags instead.", true)]
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

            // ActionStatusCallback for action that are always enabled.
            public static StatusFlags AlwaysEnabled(MenuAction a) { return StatusFlags.Normal; }

            // ActionStatusCallback for action that are always disabled.
            public static StatusFlags AlwaysDisabled(MenuAction a) { return StatusFlags.Disabled; }

            public MenuAction(string actionName, Action<MenuAction> actionCallback, Func<MenuAction, StatusFlags> actionStatusCallback, object userData = null)
            {
            }

            public void UpdateActionStatus(EventInfo eventInfo)
            {
            }

            public void Execute()
            {
            }
        }

        public List<MenuItem> MenuItems()
        {
            return new List<MenuItem>();
        }

        public void AppendAction(string actionName, Action<MenuAction> action, Func<MenuAction, MenuAction.StatusFlags> actionStatusCallback, object userData = null)
        {
        }

        public void InsertAction(int atIndex, string actionName, Action<MenuAction> action, Func<MenuAction, MenuAction.StatusFlags> actionStatusCallback, object userData = null)
        {
        }

        public void AppendSeparator(string subMenuPath = null)
        {
        }

        public void InsertSeparator(string subMenuPath, int atIndex)
        {
        }

        public void RemoveItemAt(int index)
        {
        }

        public void PrepareForDisplay(EventBase e)
        {
        }
    }
}
