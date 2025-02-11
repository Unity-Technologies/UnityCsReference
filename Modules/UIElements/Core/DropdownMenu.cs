// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides information about the event that caused the dropdown menu to display.
    /// </summary>
    public class DropdownMenuEventInfo
    {
        /// <summary>
        /// The modifier keys that were pressed if those keys triggered the dropdown menu to
        /// display of the dropdown menu. For example, Alt, Ctrl, Shift, Windows, and Command.
        /// </summary>
        public EventModifiers modifiers { get; }

        /// <summary>
        /// The mouse position expressed in the global coordinate system if the event that triggered the dropdown
        /// menu to display was a mouse event.
        /// </summary>
        /// <remarks>
        /// If the event that triggered the dropdown menu to display was not a mouse event, this property is zero.
        /// </remarks>
        public Vector2 mousePosition { get; }

        /// <summary>
        /// The position of the mouse if the event that triggered the dropdown menu to display was a mouse event.
        /// </summary>
        /// <remarks>
        /// The position is expressed in the coordinate system of the element that received the mouse event.
        /// If the event that triggered the dropdown menu to display was not a mouse event, the value of this property is zero.
        /// </remarks>
        public Vector2 localMousePosition { get; }

        char character { get; }
        KeyCode keyCode { get; }

        /// <summary>
        /// Initialized menu event info with event provided.
        /// </summary>
        /// <param name="e">Source event.</param>
        public DropdownMenuEventInfo(EventBase e)
        {
            if (e is IMouseEvent mouseEvent)
            {
                mousePosition = mouseEvent.mousePosition;
                localMousePosition = mouseEvent.localMousePosition;
                modifiers = mouseEvent.modifiers;
                character = '\0';
                keyCode = KeyCode.None;
            }
            else if (e is IPointerEvent pointerEvent)
            {
                mousePosition = pointerEvent.position;
                localMousePosition = pointerEvent.localPosition;
                modifiers = pointerEvent.modifiers;
                character = '\0';
                keyCode = KeyCode.None;
            }
            else if (e is IKeyboardEvent keyboardEvent)
            {
                character = keyboardEvent.character;
                keyCode = keyboardEvent.keyCode;
                modifiers = keyboardEvent.modifiers;
                mousePosition = Vector2.zero;
                localMousePosition = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// Represents an item in a dropdown menu.
    /// </summary>
    public abstract class DropdownMenuItem {}

    /// <summary>
    /// Provides a separator menu item.
    /// </summary>
    public class DropdownMenuSeparator : DropdownMenuItem
    {
        /// <summary>
        /// The submenu path to the separator. Path components are delimited by forward slashes ('/').
        /// </summary>
        public string subMenuPath { get; }

        /// <summary>
        /// Initializes a separator at a path.
        /// </summary>
        /// <param name="subMenuPath">The path for the submenu. Path components are delimited by forward slashes ('/').</param>
        public DropdownMenuSeparator(string subMenuPath)
        {
            this.subMenuPath = subMenuPath;
        }
    }

    /// <summary>
    /// Represents a menu action item.
    /// </summary>
    /// <remarks>
    /// A <see cref="DropdownMenu"/> contains one or more DropdownMenuAction instances.
    /// Each DropdownMenuAction instance can have its own status and callback.
    /// </remarks>
    /// <example>
    /// The following example shows how to create a dropdown menu with actions, sub-menu actions, and conditional actions.
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/DropdownMenuExample.cs"/>
    /// </example>
    public class DropdownMenuAction : DropdownMenuItem
    {
        /// <summary>
        /// Status of the menu item.
        /// </summary>
        /// <remarks>
        /// Use the values of this enumeration as flags
        /// </remarks>
        [Flags]
        public enum Status
        {
            /// <summary>
            /// Do not display the item.
            /// </summary>
            /// <remarks>
            /// This is the default value and represents the absence of flags.
            /// </remarks>
            None = 0,
            /// <summary>
            /// Display the item normally.
            /// </summary>
            Normal = 1, // Enabled, unchecked, shown
            /// <summary>
            /// Disable the item and make it so it is not selectable by the user.
            /// </summary>
            Disabled = 2,
            /// <summary>
            /// Display the item with a checkmark.
            /// </summary>
            Checked = 4,
            /// <summary>
            /// Do not display the item.
            /// </summary>
            /// <remarks>
            /// This flag can be used with other flags.
            /// </remarks>
            Hidden = 8
        }

        /// <summary>
        /// The name of the item.
        /// </summary>
        /// <remarks>
        /// The name of the time can be prefixed by its submenu path. Path components are delimited by forward slashes ('/').
        /// </remarks>
        public string name { get; }

        /// <summary>
        /// The status of the item.
        /// </summary>
        public Status status { get; private set; }

        /// <summary>
        /// Provides information about the event that triggered the dropdown menu.
        /// </summary>
        public DropdownMenuEventInfo eventInfo { get; private set; }

        /// <summary>
        /// The userData object stored by the constructor. You can use <see cref="AppendAction"/> to set it and use it in the status callbacks.
        /// </summary>
        public object userData { get; private set; }

        internal VisualElement content { get; }

        readonly Action<DropdownMenuAction> actionCallback;
        readonly Func<DropdownMenuAction, Status> actionStatusCallback;

        /// <summary>
        /// Creates a status callback that always returns <see cref="Status.Normal"/>.
        /// </summary>
        /// <param name="a">Unused parameter.</param>
        /// <returns>Always returns <see cref="Status.Normal"/> </returns>
        public static Status AlwaysEnabled(DropdownMenuAction a)
        {
            return Status.Normal;
        }

        /// <summary>
        /// Creates a status callback that always returns <see cref="Status.Disabled"/> status.
        /// </summary>
        /// <param name="a">Unused parameter.</param>
        /// <returns>Always returns <see cref="Status.Disabled"/>.</returns>
        public static Status AlwaysDisabled(DropdownMenuAction a)
        {
            return Status.Disabled;
        }

        /// <summary>
        /// Initializes a menu action with specified parameters.
        /// </summary>
        /// <param name="actionName">The path and name of the menu item. Use the path, delimited by forward slashes ('/'), to place the menu item within a submenu.</param>
        /// <param name="actionCallback">The action to execute when the menu item is selected.</param>
        /// <param name="actionStatusCallback">The function called to determine if the menu item is enabled.</param>
        /// <param name="userData">The object to store in the @@userData@@ property.</param>
        public DropdownMenuAction(string actionName, Action<DropdownMenuAction> actionCallback, Func<DropdownMenuAction, Status> actionStatusCallback, object userData = null)
        {
            name = actionName;
            this.actionCallback = actionCallback;
            this.actionStatusCallback = actionStatusCallback;
            this.userData = userData;
        }

        /// <summary>
        /// Updates the status flag of this item by calling the item status callback.
        /// </summary>
        /// <param name="eventInfo">Information about the event that caused the dropdown menu to display, such as the mouse position or the key pressed.</param>
        public void UpdateActionStatus(DropdownMenuEventInfo eventInfo)
        {
            this.eventInfo = eventInfo;
            status = actionStatusCallback?.Invoke(this) ?? Status.Hidden;
        }

        /// <summary>
        /// Executes the callback associated with this item.
        /// </summary>
        public void Execute()
        {
            actionCallback?.Invoke(this);
        }
    }

    /// <summary>
    /// Represents a dropdown menu, similar to the menus seen in most Operating Systems (OS) and across the Unity Editor.
    /// </summary>
    /// <remarks>
    /// Use this class to set custom <see cref="DropdownMenuItem"/> that
    /// executes a <see cref="DropdownMenuAction"/> based on its status.
    ///
    /// Use this class to create OS-like dropdown menus in the Unity Editor. For more generic
    /// dropdown menus designed for both Editor and Runtime use, use <see cref="GenericDropdownMenu"/>.
    /// </remarks>
    /// <example>
    /// The following example shows how to create a dropdown menu with submenus and conditional actions.
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/DropdownMenuExample.cs"/>
    /// </example>
    public class DropdownMenu
    {
        List<DropdownMenuItem> m_MenuItems = new List<DropdownMenuItem>();
        DropdownMenuEventInfo m_DropdownMenuEventInfo;

        internal int Count => m_MenuItems.Count;

        /// <summary>
        /// Determines whether the menu allows duplicate names.
        /// </summary>
        public bool allowDuplicateNames { get; set; }

        /// <summary>
        /// Gets the list of menu items.
        /// </summary>
        /// <returns>The list of items in the menu.</returns>
        public List<DropdownMenuItem> MenuItems()
        {
            return m_MenuItems;
        }

        /// <summary>
        /// Adds an item that executes an action in the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the current item list.
        /// </remarks>
        /// <param name="actionName">The name of the item. This name is displayed in the dropdown menu.</param>
        /// <param name="action">The callback to execute when the user selects this item in the menu.</param>
        /// <param name="actionStatusCallback">The callback to execute to determine the status of this item.</param>
        /// <param name="userData">The object to store in the @@userData@@ property of the <see cref="DropdownMenuAction"/> item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("Submenu/My Action 1", a => Debug.Log("My Action 1 Works"), action =>
        ///            {
        ///                action.tooltip = "Status dependent tooltip";
        ///                return DropdownMenuAction.Status.Normal;
        ///            }, null);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void AppendAction(string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            DropdownMenuAction menuAction = new DropdownMenuAction(actionName, action, actionStatusCallback, userData);
            m_MenuItems.Add(menuAction);
        }

        /// <summary>
        /// Adds an item that executes an action in the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the current item list.
        /// </remarks>
        /// <param name="actionName">The name of the item. This name is displayed in the dropdown menu.</param>
        /// <param name="action">The callback to execute when the user selects this item in the menu.</param>
        /// <param name="status">The status of the item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
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
        /// Adds an item that executes an action in the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the specified index in the list.
        /// </remarks>
        /// <param name="atIndex">The index to insert the item at.</param>
        /// <param name="actionName">The name of the item. This name is displayed in the dropdown menu.</param>
        /// <param name="action">Callback to execute when the user selects this item in the menu.</param>
        /// <param name="actionStatusCallback">The callback to execute to determine the status of the item.</param>
        /// <param name="userData">The object to store in the @@userData@@ property of the <see cref="DropdownMenuAction"/> item. This object is accessible through the action callback.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);  // 0
        ///            e.menu.AppendAction("My Action 3", a => Debug.Log("My Action 3 Works"), DropdownMenuAction.Status.Normal);  // 1
        ///            e.menu.AppendAction("Submenu/My Action 4", a => Debug.Log("My Action 4 Works"), DropdownMenuAction.Status.Normal);  // 2
        ///            e.menu.AppendAction("Submenu/My Action 6", a => Debug.Log("My Action 6 Works"), DropdownMenuAction.Status.Normal);  // 3
        ///
        ///            // Indices from 1 to 3 are shifted up index by 1. In result 'My Action 2' now has an index of 2.
        ///            e.menu.InsertAction(1, "My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.AlwaysEnabled);
        ///
        ///            // If we want to insert an between submenu items, we have to use shifted indices
        ///            e.menu.InsertAction(4, "Submenu/My Action 5", a => Debug.Log("My Action 5 Works"), DropdownMenuAction.AlwaysDisabled);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void InsertAction(int atIndex, string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            DropdownMenuAction menuAction = new DropdownMenuAction(actionName, action, actionStatusCallback, userData);
            m_MenuItems.Insert(atIndex, menuAction);
        }

        /// <summary>
        /// Adds an item that executes an action in the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the specified index in the list.
        /// </remarks>
        /// <param name="atIndex">The index to insert the item at.</param>
        /// <param name="actionName">The name of the item. This name is displayed in the dropdown menu.</param>
        /// <param name="action">The callback to execute when the user selects this item in the menu.</param>
        /// <param name="status">The status of the item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);  // 0
        ///            e.menu.AppendAction("My Action 3", a => Debug.Log("My Action 3 Works"), DropdownMenuAction.Status.Normal);  // 1
        ///            e.menu.AppendAction("Submenu/My Action 4", a => Debug.Log("My Action 4 Works"), DropdownMenuAction.Status.Normal);  // 2
        ///            e.menu.AppendAction("Submenu/My Action 6", a => Debug.Log("My Action 6 Works"), DropdownMenuAction.Status.Normal);  // 3
        ///
        ///            // Indices from 1 to 3 are shifted up index by 1. In result 'My Action 2' now has an index of 2.
        ///            e.menu.InsertAction(1, "My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);
        ///
        ///            // If we want to insert an between submenu items, we have to use shifted indices
        ///            e.menu.InsertAction(4, "Submenu/My Action 5", a => Debug.Log("My Action 5 Works"), DropdownMenuAction.Status.Disabled);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
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
        /// Adds a separator line in the menu.
        /// </summary>
        /// <remarks>
        /// The separator is added at the end of the current item list.
        /// </remarks>
        /// <param name="subMenuPath">The submenu path to add the separator to. Path components are delimited by forward slashes ('/').</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendSeparator();
        ///            e.menu.AppendAction("My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);
        ///
        ///            e.menu.AppendAction("Submenu/My Action 3", a => Debug.Log("My Action 3 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendSeparator("Submenu/");
        ///            e.menu.AppendAction("Submenu/My Action 4", a => Debug.Log("My Action 4 Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void AppendSeparator(string subMenuPath = null)
        {
            subMenuPath ??= string.Empty;
            // Do not allow a separator to be added as a first item in a menu or submenu
            var isFirstItemOfMenu = m_MenuItems.FindIndex(item => item is DropdownMenuAction action && action.name.StartsWith(subMenuPath)) == -1;
            if (m_MenuItems.Count > 0 && !(m_MenuItems[^1] is DropdownMenuSeparator && ((DropdownMenuSeparator)m_MenuItems[^1]).subMenuPath == subMenuPath) && !isFirstItemOfMenu)
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath);
                m_MenuItems.Add(separator);
            }
        }

        /// <summary>
        /// Adds a separator line in the menu.
        /// </summary>
        /// <remarks>
        /// The separator is added at the end of the specified index in the list.
        /// </remarks>
        /// <param name="subMenuPath">The submenu path to add the separator to. Path components are delimited by forward slashes ('/').</param>
        /// <param name="atIndex">The index to insert the separator at.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);  // 0
        ///            e.menu.AppendAction("My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);  // 1
        ///            e.menu.AppendAction("Submenu/My Action 3", a => Debug.Log("My Action 3 Works"), DropdownMenuAction.Status.Normal);  // 2
        ///            e.menu.AppendAction("Submenu/My Action 4", a => Debug.Log("My Action 4 Works"), DropdownMenuAction.Status.Normal);  // 3
        ///
        ///            e.menu.InsertSeparator("/", 1);     // Indices from 1 to 3 are shifted up index by 1. In result 'My Action 2' now has an index of 2.
        ///            e.menu.InsertSeparator("Submenu/", 4);  // If we want to insert a separator between submenu items, we have to use shifted indices
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void InsertSeparator(string subMenuPath, int atIndex)
        {
            if (atIndex > 0 && atIndex <= m_MenuItems.Count && !(m_MenuItems[atIndex - 1] is DropdownMenuSeparator))
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath ?? String.Empty);
                m_MenuItems.Insert(atIndex, separator);
            }
        }

        /// <summary>
        /// Removes the menu item at index.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendAction("My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendAction("My Action 3", a => Debug.Log("My Action 3 Works"), DropdownMenuAction.Status.Normal);
        ///
        ///            e.menu.RemoveItemAt(0); // Remove My Action 1
        ///            e.menu.RemoveItemAt(1); // Remove My Action 3 (item indices have shifted after first removal)
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void RemoveItemAt(int index)
        {
            m_MenuItems.RemoveAt(index);
        }

        /// <summary>
        /// Clears all items from the menu.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendAction("My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.ClearItems();
        ///            e.menu.AppendAction("My Action 3", a => Debug.Log("My Action 3 Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void ClearItems()
        {
            m_MenuItems.Clear();
        }

        /// <summary>
        /// Gets the status of all items by calling their status callback and removes the excess separators.
        /// </summary>
        /// <remarks>
        /// Called before the menu is displayed.
        /// </remarks>
        /// <param name="e">The source event.</param>
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
