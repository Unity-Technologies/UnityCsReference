using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This class provides information about the event that triggered displaying the drop-down menu.
    /// </summary>
    public class DropdownMenuEventInfo
    {
        /// <summary>
        /// If modifier keys (Alt, Ctrl, Shift, Windows/Command) were pressed to trigger the display of the dropdown menu, this property lists the modifier keys.
        /// </summary>
        public EventModifiers modifiers { get; }
        /// <summary>
        /// If the triggering event was a mouse event, this property is the mouse position expressed using the global coordinate system. Otherwise this property is zero.
        /// </summary>
        public Vector2 mousePosition { get; }
        /// <summary>
        /// If the triggering event was a mouse event, this property is the mouse position. The position is expressed using the coordinate system of the element that received the mouse event. Otherwise this property is zero.
        /// </summary>
        public Vector2 localMousePosition { get; }
        char character { get; }
        KeyCode keyCode { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
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

    /// <summary>
    /// An item in a drop-down menu.
    /// </summary>
    public abstract class DropdownMenuItem {}

    /// <summary>
    /// A separator menu item.
    /// </summary>
    public class DropdownMenuSeparator : DropdownMenuItem
    {
        /// <summary>
        /// The submenu path where the separator will be added. Path components are delimited by forward slashes ('/').
        /// </summary>
        public string subMenuPath { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="subMenuPath">The path for the submenu. Path components are delimited by forward slashes ('/').</param>
        public DropdownMenuSeparator(string subMenuPath)
        {
            this.subMenuPath = subMenuPath;
        }
    }

    /// <summary>
    /// A menu action item.
    /// </summary>
    public class DropdownMenuAction : DropdownMenuItem
    {
        /// <summary>
        /// Status of the menu item. The values of this enumeration should be used as flags.
        /// </summary>
        [Flags]
        public enum Status
        {
            /// <summary>
            /// The item is not displayed. This is the default value and represents the absence of flags.
            /// </summary>
            None = 0,
            /// <summary>
            /// The item is displayed normally.
            /// </summary>
            Normal = 1, // Enabled, unchecked, shown
            /// <summary>
            /// The item is disabled and is not be selectable by the user.
            /// </summary>
            Disabled = 2,
            /// <summary>
            /// The item is displayed with a checkmark.
            /// </summary>
            Checked = 4,
            /// <summary>
            /// The item is not displayed. This flag can be used with other flags.
            /// </summary>
            Hidden = 8
        }

        /// <summary>
        /// The name of the item. The name can be prefixed by its submenu path. Path components are delimited by forward slashes ('/').
        /// </summary>
        public string name { get; }

        /// <summary>
        /// The status of the item.
        /// </summary>
        public Status status { get; private set; }

        /// <summary>
        /// Provides information on the event that triggered the drop-down menu.
        /// </summary>
        public DropdownMenuEventInfo eventInfo { get; private set; }

        /// <summary>
        /// The userData object stored by the constructor.
        /// </summary>
        public object userData { get; private set; }

        readonly Action<DropdownMenuAction> actionCallback;
        readonly Func<DropdownMenuAction, Status> actionStatusCallback;

        // ActionStatusCallback for action that are always enabled.
        /// <summary>
        /// Status callback that always returns Status.Enabled.
        /// </summary>
        /// <param name="a">Unused parameter.</param>
        /// <returns>Always return Status.Enabled.</returns>
        public static Status AlwaysEnabled(DropdownMenuAction a)
        {
            return Status.Normal;
        }

        // ActionStatusCallback for action that are always disabled.
        /// <summary>
        /// Status callback that always returns Status.Disabled.
        /// </summary>
        /// <param name="a">Unused parameter.</param>
        /// <returns>Always return Status.Disabled.</returns>
        public static Status AlwaysDisabled(DropdownMenuAction a)
        {
            return Status.Disabled;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actionName">The path and name of the menu item. Use the path, delimited by forward slashes ('/'), to place the menu item within a submenu.</param>
        /// <param name="actionCallback">Action to be executed when the menu item is selected.</param>
        /// <param name="actionStatusCallback">Function called to determine if the menu item is enabled.</param>
        /// <param name="userData">An object that will be stored in the userData property.</param>
        public DropdownMenuAction(string actionName, Action<DropdownMenuAction> actionCallback, Func<DropdownMenuAction, Status> actionStatusCallback, object userData = null)
        {
            name = actionName;
            this.actionCallback = actionCallback;
            this.actionStatusCallback = actionStatusCallback;
            this.userData = userData;
        }

        /// <summary>
        /// Update the status flag of this item by calling the item status callback.
        /// </summary>
        /// <param name="eventInfo">Information about the event that triggered the display of the drop-down menu, such as the mouse position or the key pressed.</param>
        public void UpdateActionStatus(DropdownMenuEventInfo eventInfo)
        {
            this.eventInfo = eventInfo;
            status = actionStatusCallback?.Invoke(this) ?? Status.Hidden;
        }

        /// <summary>
        /// Execute the callback associated with this item.
        /// </summary>
        public void Execute()
        {
            actionCallback?.Invoke(this);
        }
    }

    /// <summary>
    /// A drop-down menu.
    /// </summary>
    public class DropdownMenu
    {
        List<DropdownMenuItem> m_MenuItems = new List<DropdownMenuItem>();
        DropdownMenuEventInfo m_DropdownMenuEventInfo;

        /// <summary>
        /// Get the list of menu items.
        /// </summary>
        /// <returns>The list of items in the menu.</returns>
        public List<DropdownMenuItem> MenuItems()
        {
            return m_MenuItems;
        }

        /// <summary>
        /// Add an item that will execute an action in the drop-down menu. The item is added at the end of the current item list.
        /// </summary>
        /// <param name="actionName">Name of the item. This name will be displayed in the drop-down menu.</param>
        /// <param name="action">Callback to execute when the user selects this item in the menu.</param>
        /// <param name="actionStatusCallback">Callback to execute to determine the status of the item.</param>
        /// <param name="userData">An object that will be stored in the userData property of the DropdownMenuAction item.</param>
        public void AppendAction(string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            DropdownMenuAction menuAction = new DropdownMenuAction(actionName, action, actionStatusCallback, userData);
            m_MenuItems.Add(menuAction);
        }

        /// <summary>
        /// Add an item that will execute an action in the drop-down menu. The item is added at the end of the current item list.
        /// </summary>
        /// <param name="actionName">Name of the item. This name will be displayed in the drop-down menu.</param>
        /// <param name="action">Callback to execute when the user selects this item in the menu.</param>
        /// <param name="status">The status of the item.</param>
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

        /// <summary>
        /// Add an item that will execute an action in the drop-down menu. The item is added at the end of the specified index in the list.
        /// </summary>
        /// <param name="atIndex">Index where the item should be inserted.</param>
        /// <param name="actionName">Name of the item. This name will be displayed in the drop-down menu.</param>
        /// <param name="action">Callback to execute when the user selects this item in the menu.</param>
        /// <param name="actionStatusCallback">Callback to execute to determine the status of the item.</param>
        /// <param name="userData">An object that will be stored in the userData property of the DropdownMenuAction item. This object is accessible through the action callback.</param>
        public void InsertAction(int atIndex, string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            DropdownMenuAction menuAction = new DropdownMenuAction(actionName, action, actionStatusCallback, userData);
            m_MenuItems.Insert(atIndex, menuAction);
        }

        /// <summary>
        /// Add an item that will execute an action in the drop-down menu. The item is added at the end of the specified index in the list.
        /// </summary>
        /// <param name="atIndex">Index where the item should be inserted.</param>
        /// <param name="actionName">Name of the item. This name will be displayed in the drop-down menu.</param>
        /// <param name="action">Callback to execute when the user selects this item in the menu.</param>
        /// <param name="status">The status of the item.</param>
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

        /// <summary>
        /// Add a separator line in the menu. The separator is added at the end of the current item list.
        /// </summary>
        /// <param name="subMenuPath">The submenu path where the separator will be added. Path components are delimited by forward slashes ('/').</param>
        public void AppendSeparator(string subMenuPath = null)
        {
            if (m_MenuItems.Count > 0 && !(m_MenuItems[m_MenuItems.Count - 1] is DropdownMenuSeparator))
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath ?? String.Empty);
                m_MenuItems.Add(separator);
            }
        }

        /// <summary>
        /// Add a separator line in the menu. The separator is added at the end of the specified index in the list.
        /// </summary>
        /// <param name="subMenuPath">The submenu path where the separator is added. Path components are delimited by forward slashes ('/').</param>
        /// <param name="atIndex">Index where the separator should be inserted.</param>
        public void InsertSeparator(string subMenuPath, int atIndex)
        {
            if (atIndex > 0 && atIndex <= m_MenuItems.Count && !(m_MenuItems[atIndex - 1] is DropdownMenuSeparator))
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath ?? String.Empty);
                m_MenuItems.Insert(atIndex, separator);
            }
        }

        /// <summary>
        /// Remove the menu item at index.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public void RemoveItemAt(int index)
        {
            m_MenuItems.RemoveAt(index);
        }

        /// <summary>
        /// Update the status of all items by calling their status callback and remove the separators in excess. This is called just before displaying the menu.
        /// </summary>
        public void PrepareForDisplay(EventBase e)
        {
            m_DropdownMenuEventInfo = e != null ? new DropdownMenuEventInfo(e) : null;

            if (m_MenuItems.Count == 0)
                return;

            foreach (DropdownMenuItem item in m_MenuItems)
            {
                DropdownMenuAction action = item as DropdownMenuAction;
                if (action != null)
                {
                    action.UpdateActionStatus(m_DropdownMenuEventInfo);
                }
            }

            if (m_MenuItems[m_MenuItems.Count - 1] is DropdownMenuSeparator)
            {
                m_MenuItems.RemoveAt(m_MenuItems.Count - 1);
            }
        }
    }
}
