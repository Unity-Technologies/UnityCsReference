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
        /// The tooltip of the item.
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
        ///    static int myAction1State;
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            myAction1State++;
        ///            myAction1State %= 4;
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), a =>
        ///            {
        ///                switch (myAction1State)
        ///                {
        ///                    case 0:
        ///                        a.tooltip = "My Action 1 has 'Normal' state";
        ///                        return DropdownMenuAction.Status.Normal;
        ///                    case 1:
        ///                        a.tooltip = "My Action 1 has 'Disabled' state";
        ///                        return DropdownMenuAction.Status.Disabled;
        ///                    case 2:
        ///                        a.tooltip = "My Action 1 has 'Normal' and 'Checked' state";
        ///                        return DropdownMenuAction.Status.Normal | DropdownMenuAction.Status.Checked;
        ///                    case 3:
        ///                        a.tooltip = "My Action 1 has 'Disabled' and 'Checked' state";
        ///                        return DropdownMenuAction.Status.Disabled | DropdownMenuAction.Status.Checked;
        ///                }
        ///                a.tooltip = "My Action 1 has an unknown state. Defaulting to 'Normal'.";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///        }));
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public string tooltip { get; set; }

        /// <summary>
        /// The icon of the item.
        /// </summary>
        public Texture2D icon { get; }

        /// <summary>
        /// The status of the item.
        /// </summary>
        public Status status { get; private set; }

        /// <summary>
        /// Provides information about the event that triggered the dropdown menu.
        /// </summary>
        public DropdownMenuEventInfo eventInfo { get; private set; }

        /// <summary>
        /// The userData object stored by the constructor.
        /// </summary>
        public object userData { get; private set; }

        internal VisualElement content { get; }

        readonly Action<DropdownMenuAction> actionCallback;
        readonly Func<DropdownMenuAction, Status> actionStatusCallback;

        /// <summary>
        /// Creates a status callback that always returns <see cref="Status.Enabled"/>.
        /// </summary>
        /// <param name="a">Unused parameter.</param>
        /// <returns>Always returns <see cref="Status.Enabled"/> </returns>
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

        public DropdownMenuAction(string actionName, Action<DropdownMenuAction> actionCallback, Func<DropdownMenuAction, Status> actionStatusCallback, object userData = null)
            : this(actionName, actionCallback, actionStatusCallback, userData, null) { }

        /// <summary>
        /// Initializes a menu action with specified parameters.
        /// </summary>
        /// <param name="actionName">The path and name of the menu item. Use the path, delimited by forward slashes ('/'), to place the menu item within a submenu.</param>
        /// <param name="actionCallback">The action to execute when the menu item is selected.</param>
        /// <param name="actionStatusCallback">The function called to determine if the menu item is enabled.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property.</param>
        /// <param name="icon">The icon to display to the user.</param>
        public DropdownMenuAction(string actionName, Action<DropdownMenuAction> actionCallback, Func<DropdownMenuAction, Status> actionStatusCallback, object userData, Texture2D icon)
        {
            name = actionName;
            this.icon = icon;
            this.actionCallback = actionCallback;
            this.actionStatusCallback = actionStatusCallback;
            this.userData = userData;
        }

        /// <summary>
        /// Initializes a menu action with specified parameters.
        /// </summary>
        /// <param name="contentName">The path and name of the menu item. Use the path, delimited by forward slashes ('/'), to place the menu item within a submenu.</param>
        /// <param name="content">The visual element for the custom menu item.</param>
        /// <param name="actionStatusCallback">The function called to determine if the menu item is enabled.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property.</param>
        public DropdownMenuAction(string contentName, VisualElement content, Func<DropdownMenuAction, Status> actionStatusCallback, object userData = null)
        {
            name = contentName;
            this.content = content;
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
    /// Represents a dropdown menu.
    /// </summary>
    public class DropdownMenu
    {
        internal object m_Descriptor;
        List<DropdownMenuItem> m_MenuItems = new List<DropdownMenuItem>();
        List<DropdownMenuItem> m_HeaderItems = new List<DropdownMenuItem>();
        DropdownMenuEventInfo m_DropdownMenuEventInfo;

        internal int Count => m_MenuItems.Count;

        /// <summary>
        /// Gets the list of menu items.
        /// </summary>
        /// <returns>The list of items in the menu.</returns>
        public List<DropdownMenuItem> MenuItems()
        {
            return m_MenuItems;
        }

        internal List<DropdownMenuItem> HeaderItems()
        {
            return m_HeaderItems;
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
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item.</param>
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
            AppendAction(actionName, action, actionStatusCallback, userData, null);
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
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item.</param>
        /// <param name="icon">The icon to display next to the item name.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    Texture2D icon;
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("Submenu/My Action 4", a => Debug.Log("My Action 4 Works"), action =>
        ///            {
        ///                action.tooltip = "Status dependent tooltip";
        ///                return DropdownMenuAction.Status.Normal;
        ///            }, null, icon);
        ///        }));
        ///
        ///        var iconField = new ObjectField()
        ///        {
        ///            label = "Icon",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///
        ///        iconField.RegisterValueChangedCallback(e =>
        ///        {
        ///            icon = (Texture2D)e.newValue;
        ///        });
        ///
        ///        contextMenuContainer.Add(iconField);
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void AppendAction(string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData, Texture2D icon)
        {
            m_MenuItems.Add(new DropdownMenuAction(actionName, action, actionStatusCallback, userData, icon));
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
            AppendAction(actionName, action, status, null);
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
        /// <param name="icon">The icon to display next to the item name.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    Texture2D icon;
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal, icon);
        ///        }));
        ///
        ///        var iconField = new ObjectField()
        ///        {
        ///            label = "Icon",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///
        ///        iconField.RegisterValueChangedCallback(e =>
        ///        {
        ///            icon = (Texture2D)e.newValue;
        ///        });
        ///
        ///        contextMenuContainer.Add(iconField);
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void AppendAction(string actionName, Action<DropdownMenuAction> action, DropdownMenuAction.Status status, Texture2D icon)
        {
            if (status == DropdownMenuAction.Status.Normal)
            {
                AppendAction(actionName, action, DropdownMenuAction.AlwaysEnabled, null, icon);
            }
            else if (status == DropdownMenuAction.Status.Disabled)
            {
                AppendAction(actionName, action, DropdownMenuAction.AlwaysDisabled, null, icon);
            }
            else
            {
                AppendAction(actionName, action, e => status, null, icon);
            }
        }

        /// <summary>
        /// Adds a header item to the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the current header item list.
        /// </remarks>
        /// <param name="icon">The icon to display to the user.</param>
        /// <param name="action">The callback to execute when the user selects this item in the menu.</param>
        /// <param name="actionStatusCallback">The callback to execute to determine the status of the item.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    Texture2D icon1;
        ///    Texture2D icon2;
        ///    Texture2D icon3;
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendHeaderAction(icon1, a => Debug.Log("My Action 1 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 1";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            e.menu.AppendHeaderAction(icon2, a => Debug.Log("My Action 2 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 2";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            e.menu.AppendHeaderAction(icon3, a => Debug.Log("My Action 3 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 3";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            e.menu.AppendAction("My Menu Item Action", a => Debug.Log("My Menu Item Action Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///
        ///        var icon1Field = new ObjectField()
        ///        {
        ///            label = "Icon 1",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon1Field.RegisterValueChangedCallback(e => icon1 = (Texture2D)e.newValue);
        ///        var icon2Field = new ObjectField()
        ///        {
        ///            label = "Icon 2",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon2Field.RegisterValueChangedCallback(e => icon2 = (Texture2D)e.newValue);
        ///        var icon3Field = new ObjectField()
        ///        {
        ///            label = "Icon 3",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///
        ///        icon3Field.RegisterValueChangedCallback(e => icon3 = (Texture2D)e.newValue);
        ///
        ///        contextMenuContainer.Add(icon1Field);
        ///        contextMenuContainer.Add(icon2Field);
        ///        contextMenuContainer.Add(icon3Field);
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void AppendHeaderAction(Texture2D icon, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            m_HeaderItems.Add(new DropdownMenuAction(string.Empty, action, actionStatusCallback, userData, icon));
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
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item. This object is accessible through the action callback.</param>
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
            InsertAction(atIndex, actionName, action, actionStatusCallback, userData, null);
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
        /// <param name="actionStatusCallback">The callback to execute to determine the status of the item.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item. This object is accessible through the action callback.</param>
        /// <param name="icon">The icon to display next to item name.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    Texture2D icon;
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
        ///            e.menu.InsertAction(1, "My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.AlwaysEnabled, null, icon);
        ///
        ///            // If we want to insert an between submenu items, we have to use shifted indices
        ///            e.menu.InsertAction(4, "Submenu/My Action 5", a => Debug.Log("My Action 5 Works"), DropdownMenuAction.AlwaysDisabled, null, icon);
        ///        }));
        ///
        ///        var iconField = new ObjectField()
        ///        {
        ///            label = "Icon",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///
        ///        iconField.RegisterValueChangedCallback(e =>
        ///        {
        ///            icon = (Texture2D)e.newValue;
        ///        });
        ///
        ///        contextMenuContainer.Add(iconField);
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void InsertAction(int atIndex, string actionName, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData, Texture2D icon)
        {
            m_MenuItems.Insert(atIndex, new DropdownMenuAction(actionName, action, actionStatusCallback, userData, icon));
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
            InsertAction(atIndex, actionName, action, status, null);
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
        /// <param name="icon">The icon to display next to item name.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    Texture2D icon;
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
        ///            e.menu.InsertAction(1, "My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal, icon);
        ///
        ///            // If we want to insert an between submenu items, we have to use shifted indices
        ///            e.menu.InsertAction(4, "Submenu/My Action 5", a => Debug.Log("My Action 5 Works"), DropdownMenuAction.Status.Disabled, icon);
        ///        }));
        ///
        ///
        ///        var iconField = new ObjectField()
        ///        {
        ///            label = "Icon",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///
        ///        iconField.RegisterValueChangedCallback(e =>
        ///        {
        ///            icon = (Texture2D)e.newValue;
        ///        });
        ///
        ///        contextMenuContainer.Add(iconField);
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void InsertAction(int atIndex, string actionName, Action<DropdownMenuAction> action, DropdownMenuAction.Status status, Texture2D icon)
        {
            if (status == DropdownMenuAction.Status.Normal)
            {
                InsertAction(atIndex, actionName, action, DropdownMenuAction.AlwaysEnabled, null, icon);
            }
            else if (status == DropdownMenuAction.Status.Disabled)
            {
                InsertAction(atIndex, actionName, action, DropdownMenuAction.AlwaysDisabled, null, icon);
            }
            else
            {
                InsertAction(atIndex, actionName, action, e => status, null, icon);
            }
        }

        /// <summary>
        /// Adds a header item to the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added at the end of the specified index in the list.
        /// </remarks>
        /// <param name="atIndex">The index to insert the item at.</param>
        /// <param name="icon">The icon to display to the user.</param>
        /// <param name="action">The callback to execute when the user selects this item in the menu.</param>
        /// <param name="actionStatusCallback">The callback to execute to determine the status of the item.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///    
        ///    Texture2D icon1;
        ///    Texture2D icon2;
        ///    Texture2D icon3;
        ///    
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendHeaderAction(icon2, a => Debug.Log("My Action 2 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 2";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            
        ///            e.menu.InsertHeaderAction(0, icon1, a => Debug.Log("My Action 1 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 1";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            
        ///            e.menu.InsertHeaderAction(2, icon3, a => Debug.Log("My Action 3 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 3";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            
        ///            e.menu.AppendAction("My Menu Item Action", a => Debug.Log("My Menu Item Action Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///        
        ///        var icon1Field = new ObjectField()
        ///        {
        ///            label = "Icon 1",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon1Field.RegisterValueChangedCallback(e => icon1 = (Texture2D)e.newValue);
        ///        
        ///        var icon2Field = new ObjectField()
        ///        {
        ///            label = "Icon 2",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon2Field.RegisterValueChangedCallback(e => icon2 = (Texture2D)e.newValue);
        ///        
        ///        var icon3Field = new ObjectField()
        ///        {
        ///            label = "Icon 3",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon3Field.RegisterValueChangedCallback(e => icon3 = (Texture2D)e.newValue);
        ///        
        ///        contextMenuContainer.Add(icon1Field);
        ///        contextMenuContainer.Add(icon2Field);
        ///        contextMenuContainer.Add(icon3Field);
        ///        
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void InsertHeaderAction(int atIndex, Texture2D icon, Action<DropdownMenuAction> action, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            m_HeaderItems.Insert(atIndex, new DropdownMenuAction(string.Empty, action, actionStatusCallback, userData, icon));
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
            if (m_MenuItems.Count > 0 && !(m_MenuItems[m_MenuItems.Count - 1] is DropdownMenuSeparator))
            {
                DropdownMenuSeparator separator = new DropdownMenuSeparator(subMenuPath ?? String.Empty);
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
        /// Removes the menu header item at index.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///    
        ///    Texture2D icon1;
        ///    Texture2D icon2;
        ///    Texture2D icon3;
        ///    
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendHeaderAction(icon1, a => Debug.Log("My Action 1 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 1";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            
        ///            e.menu.AppendHeaderAction(icon2, a => Debug.Log("My Action 2 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 2";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            
        ///            e.menu.AppendHeaderAction(icon3, a => Debug.Log("My Action 3 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 3";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            
        ///            e.menu.RemoveHeaderItemAt(0);   // Remove Icon 1
        ///            e.menu.RemoveHeaderItemAt(1);   // Remove Icon 3 (After first removal item indices have shifted)
        ///            
        ///            e.menu.AppendAction("My Menu Item Action", a => Debug.Log("My Menu Item Action Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///        
        ///        var icon1Field = new ObjectField()
        ///        {
        ///            label = "Icon 1",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon1Field.RegisterValueChangedCallback(e => icon1 = (Texture2D)e.newValue);
        ///        
        ///        var icon2Field = new ObjectField()
        ///        {
        ///            label = "Icon 2",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon2Field.RegisterValueChangedCallback(e => icon2 = (Texture2D)e.newValue);
        ///        
        ///        var icon3Field = new ObjectField()
        ///        {
        ///            label = "Icon 3",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon3Field.RegisterValueChangedCallback(e => icon3 = (Texture2D)e.newValue);
        ///        
        ///        contextMenuContainer.Add(icon1Field);
        ///        contextMenuContainer.Add(icon2Field);
        ///        contextMenuContainer.Add(icon3Field);
        ///        
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void RemoveHeaderItemAt(int index)
        {
            m_HeaderItems.RemoveAt(index);
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
        /// Clears all header items from the menu.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
        ///using UnityEditor.UIElements;
        ///using UnityEngine;
        ///using UnityEngine.UIElements;
        ///
        ///public class ContextMenuWindow : EditorWindow
        ///{
        ///    [MenuItem("My/Context Menu Window")]
        ///    static void ShowMe() => GetWindow<ContextMenuWindow>();
        ///
        ///    Texture2D icon1;
        ///    Texture2D icon2;
        ///    Texture2D icon3;
        ///
        ///    void CreateGUI()
        ///    {
        ///        var contextMenuContainer = new VisualElement();
        ///        contextMenuContainer.style.flexGrow = 1;
        ///        contextMenuContainer.AddManipulator(new ContextualMenuManipulator(e =>
        ///        {
        ///            e.menu.AppendHeaderAction(icon1, a => Debug.Log("My Action 1 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 1";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            e.menu.AppendHeaderAction(icon2, a => Debug.Log("My Action 2 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 2";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            e.menu.ClearHeaderItems();
        ///            e.menu.AppendHeaderAction(icon3, a => Debug.Log("My Action 3 Works"), a =>
        ///            {
        ///                a.tooltip = "Icon 3";
        ///                return DropdownMenuAction.Status.Normal;
        ///            });
        ///            e.menu.AppendAction("My Menu Item Action", a => Debug.Log("My Menu Item Action Works"), DropdownMenuAction.Status.Normal);
        ///        }));
        ///
        ///        var icon1Field = new ObjectField()
        ///        {
        ///            label = "Icon 1",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon1Field.RegisterValueChangedCallback(e => icon1 = (Texture2D)e.newValue);
        ///        var icon2Field = new ObjectField()
        ///        {
        ///            label = "Icon 2",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///        icon2Field.RegisterValueChangedCallback(e => icon2 = (Texture2D)e.newValue);
        ///        var icon3Field = new ObjectField()
        ///        {
        ///            label = "Icon 3",
        ///            objectType = typeof(Texture2D)
        ///        };
        ///
        ///        icon3Field.RegisterValueChangedCallback(e => icon3 = (Texture2D)e.newValue);
        ///
        ///        contextMenuContainer.Add(icon1Field);
        ///        contextMenuContainer.Add(icon2Field);
        ///        contextMenuContainer.Add(icon3Field);
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        ///]]>
        /// </code>
        /// </example>
        public void ClearHeaderItems()
        {
            m_HeaderItems.Clear();
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

            foreach (DropdownMenuItem item in m_MenuItems)
            {
                DropdownMenuAction action = item as DropdownMenuAction;
                if (action != null)
                {
                    action.UpdateActionStatus(m_DropdownMenuEventInfo);
                }
            }

            foreach (DropdownMenuItem item in m_HeaderItems)
            {
                DropdownMenuAction action = item as DropdownMenuAction;
                if (action != null)
                {
                    action.UpdateActionStatus(m_DropdownMenuEventInfo);
                }
            }

            if (m_MenuItems.Count == 0)
                return;

            if (m_MenuItems[m_MenuItems.Count - 1] is DropdownMenuSeparator)
            {
                m_MenuItems.RemoveAt(m_MenuItems.Count - 1);
            }
        }
    }
}

namespace UnityEngine.UIElements.Experimental
{
    static class DropdownMenuExtensions
    {
        /// <summary>
        /// Adds a custom item to the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the current item list.
        /// </remarks>
        /// <param name="contentName">The name of the item. This name might not be displayed in the dropdown menu if it is not included in the provided content.</param>
        /// <param name="content">The visual element for the custom menu item.</param>
        /// <param name="actionStatusCallback">The callback to execute that determines the status of the item.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using System.Linq;
        ///using UnityEditor;
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
        ///            var options = new[] { "Option 1", "Option 2", "Option 3" }.ToList();
        ///
        ///            e.menu.AppendContent("Options/My Content 2", new RadioButtonGroup(null, options), DropdownMenuAction.AlwaysEnabled);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public static void AppendContent(this DropdownMenu menu, string contentName, VisualElement content, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            menu.MenuItems().Add(new DropdownMenuAction(contentName, content, actionStatusCallback, userData));
        }

        /// <summary>
        /// Adds a custom item to the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the current item list.
        /// </remarks>
        /// <param name="contentName">The name of the item. This name might not be displayed in the dropdown menu if it is not included in the provided content.</param>
        /// <param name="content">The visual element for the custom menu item.</param>
        /// <param name="status">The status of the item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using UnityEditor;
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
        ///            var textField = new TextField();
        ///            textField.style.minWidth = 200;
        ///
        ///            e.menu.AppendContent("My Content 1", textField, DropdownMenuAction.Status.Normal);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public static void AppendContent(this DropdownMenu menu, string contentName, VisualElement content, DropdownMenuAction.Status status)
        {
            if (status == DropdownMenuAction.Status.Normal)
            {
                menu.AppendContent(contentName, content, DropdownMenuAction.AlwaysEnabled, null);
            }
            else if (status == DropdownMenuAction.Status.Disabled)
            {
                menu.AppendContent(contentName, content, DropdownMenuAction.AlwaysDisabled, null);
            }
            else
            {
                menu.AppendContent(contentName, content, e => status, null);
            }
        }

        /// <summary>
        /// Adds a custom item to the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the specified index in the list.
        /// </remarks>
        /// <param name="atIndex">The index to insert the item at.</param>
        /// <param name="contentName">The name of the item. This name might not be displayed in the dropdown menu if it is not included in the provided content.</param>
        /// <param name="content">The visual element for the custom menu item.</param>
        /// <param name="actionStatusCallback">The callback to execute to determine the status of the item.</param>
        /// <param name="userData">The object to store in the <b>userData</b> property of the <see cref="DropdownMenuAction"/> item.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///using System.Linq;
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
        ///            var options = new[] { "Option 1", "Option 2", "Option 3" }.ToList();
        ///
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendAction("My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);
        ///
        ///            e.menu.InsertContent(1, "Options/My Content", new RadioButtonGroup(null, options), DropdownMenuAction.AlwaysEnabled);
        ///        }));
        ///
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public static void InsertContent(this DropdownMenu menu, int atIndex, string contentName, VisualElement content, Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback, object userData = null)
        {
            menu.MenuItems().Insert(atIndex, new DropdownMenuAction(contentName, content, actionStatusCallback, userData));
        }

        /// <summary>
        /// Adds a custom item to the dropdown menu.
        /// </summary>
        /// <remarks>
        /// The item is added to the end of the specified index in the list.
        /// </remarks>
        /// <param name="atIndex">The index to insert the item at.</param>
        /// <param name="contentName">The name of the item. This name might not be displayed in the dropdown menu if it is not included in the provided content.</param>
        /// <param name="content">The visual element for the custom menu item.</param>
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
        ///            var textField = new TextField();
        ///            textField.style.minWidth = 200;
        ///
        ///            e.menu.AppendAction("My Action 1", a => Debug.Log("My Action 1 Works"), DropdownMenuAction.Status.Normal);
        ///            e.menu.AppendAction("My Action 2", a => Debug.Log("My Action 2 Works"), DropdownMenuAction.Status.Normal);
        ///
        ///            e.menu.InsertContent(1, "My Content", textField, DropdownMenuAction.Status.Normal);
        ///        }));
        ///        rootVisualElement.Add(contextMenuContainer);
        ///    }
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public static void InsertContent(this DropdownMenu menu, int atIndex, string contentName, VisualElement content, DropdownMenuAction.Status status)
        {
            if (status == DropdownMenuAction.Status.Normal)
            {
                menu.InsertContent(atIndex, contentName, content, DropdownMenuAction.AlwaysEnabled, null);
            }
            else if (status == DropdownMenuAction.Status.Disabled)
            {
                menu.InsertContent(atIndex, contentName, content, DropdownMenuAction.AlwaysDisabled, null);
            }
            else
            {
                menu.InsertContent(atIndex, contentName, content, e => status, null);
            }
        }
    }
}
