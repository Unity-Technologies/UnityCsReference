using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class DropdownMenuEventInfo
    {
        public EventModifiers modifiers { get; }
        public Vector2 mousePosition { get; }
        public Vector2 localMousePosition { get; }
        char character { get; }
        KeyCode keyCode { get; }

        public DropdownMenuEventInfo(EventBase e)
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

    public abstract class DropdownMenuItem {}

    public class DropdownMenuSeparator : DropdownMenuItem
    {
        public string subMenuPath { get; }

        public DropdownMenuSeparator(string subMenuPath)
        {
            this.subMenuPath = subMenuPath;
        }
    }

    public class DropdownMenuAction : DropdownMenuItem
    {
        [Flags]
        public enum Status
        {
            None = 0,
            Normal = 1, // Enabled, unchecked, shown
            Disabled = 2,
            Checked = 4,
            Hidden = 8
        }

        public string name { get; }

        public Status status { get; private set; }

        public DropdownMenuEventInfo eventInfo { get; private set; }

        public object userData { get; private set; }

        readonly Action<DropdownMenuAction> actionCallback;
        readonly Func<DropdownMenuAction, Status> actionStatusCallback;

        // ActionStatusCallback for action that are always enabled.
        public static Status AlwaysEnabled(DropdownMenuAction a)
        {
            return Status.Normal;
        }

        // ActionStatusCallback for action that are always disabled.
        public static Status AlwaysDisabled(DropdownMenuAction a)
        {
            return Status.Disabled;
        }

        public DropdownMenuAction(string actionName, Action<DropdownMenuAction> actionCallback, Func<DropdownMenuAction, Status> actionStatusCallback, object userData = null)
        {
            name = actionName;
            this.actionCallback = actionCallback;
            this.actionStatusCallback = actionStatusCallback;
            this.userData = userData;
        }

        public void UpdateActionStatus(DropdownMenuEventInfo eventInfo)
        {
            this.eventInfo = eventInfo;
            status = actionStatusCallback?.Invoke(this) ?? Status.Hidden;
        }

        public void Execute()
        {
            actionCallback?.Invoke(this);
        }
    }

    public class DropdownMenu
    {
        List<DropdownMenuItem> menuItems = new List<DropdownMenuItem>();
        DropdownMenuEventInfo m_DropdownMenuEventInfo;

        public List<DropdownMenuItem> MenuItems()
        {
            return menuItems;
        }

        public void AppendAction(string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            DropdownMenuAction menuAction = new DropdownMenuAction(actionName, action, actionStatusCallback, userData);
            menuItems.Add(menuAction);
        }

        public void AppendAction(string actionName, Action<DropdownMenuAction> action, DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal)
        {
            if (status == DropdownMenuAction.Status.Normal)
            {
                AppendAction(actionName, action, DropdownMenuAction.AlwaysEnabled);
            }
            else if (status == DropdownMenuAction.Status.Disabled)
            {
                AppendAction(actionName, action, DropdownMenuAction.AlwaysDisabled);
            }
            else
            {
                AppendAction(actionName, action, e => status);
            }
        }

        public void InsertAction(int atIndex, string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            DropdownMenuAction menuAction = new DropdownMenuAction(actionName, action, actionStatusCallback, userData);
            menuItems.Insert(atIndex, menuAction);
        }

        public void InsertAction(int atIndex, string actionName, Action<DropdownMenuAction> action, DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal)
        {
            if (status == DropdownMenuAction.Status.Normal)
            {
                InsertAction(atIndex, actionName, action, DropdownMenuAction.AlwaysEnabled);
            }
            else if (status == DropdownMenuAction.Status.Disabled)
            {
                InsertAction(atIndex, actionName, action, DropdownMenuAction.AlwaysDisabled);
            }
            else
            {
                InsertAction(atIndex, actionName, action, e => status);
            }
        }

        public void AppendSeparator(string subMenuPath = null)
        {
            if (menuItems.Count > 0 && !(menuItems[menuItems.Count - 1] is DropdownMenuSeparator))
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath ?? String.Empty);
                menuItems.Add(separator);
            }
        }

        public void InsertSeparator(string subMenuPath, int atIndex)
        {
            if (atIndex > 0 && atIndex <= menuItems.Count && !(menuItems[atIndex - 1] is DropdownMenuSeparator))
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath ?? String.Empty);
                menuItems.Insert(atIndex, separator);
            }
        }

        public void RemoveItemAt(int index)
        {
            menuItems.RemoveAt(index);
        }

        public void PrepareForDisplay(EventBase e)
        {
            m_DropdownMenuEventInfo = e != null ? new DropdownMenuEventInfo(e) : null;

            if (menuItems.Count == 0)
                return;

            foreach (DropdownMenuItem item in menuItems)
            {
                DropdownMenuAction action = item as DropdownMenuAction;
                if (action != null)
                {
                    action.UpdateActionStatus(m_DropdownMenuEventInfo);
                }
            }

            if (menuItems[menuItems.Count - 1] is DropdownMenuSeparator)
            {
                menuItems.RemoveAt(menuItems.Count - 1);
            }
        }
    }
}
