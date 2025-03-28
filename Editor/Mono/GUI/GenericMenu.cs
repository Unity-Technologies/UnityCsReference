// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    public sealed class GenericMenu
    {
        List<MenuItem> m_MenuItems = new List<MenuItem>();
        public bool allowDuplicateNames {get; set; }
        internal List<MenuItem> menuItems => m_MenuItems;

        // Callback function, called when a menu item is selected
        public delegate void MenuFunction();

        // Callback function with user data, called when a menu item is selected
        public delegate void MenuFunction2(object userData);

        // Add an item to the menu
        public void AddItem(GUIContent content, bool on, MenuFunction func)
        {
            m_MenuItems.Add(new MenuItem(content, false, on, func));
        }

        // Add an item to the menu
        public void AddItem(GUIContent content, bool on, MenuFunction2 func, object userData)
        {
            m_MenuItems.Add(new MenuItem(content, false, on, func, userData));
        }

        // Add a disabled item to the menu
        public void AddDisabledItem(GUIContent content)
        {
            m_MenuItems.Add(new MenuItem(content, false, false, null));
        }

        // Add a disabled item to the menu
        public void AddDisabledItem(GUIContent content, bool on)
        {
            m_MenuItems.Add(new MenuItem(content, false, on, null));
        }

        // Add a separator item to the menu
        public void AddSeparator(string path)
        {
            m_MenuItems.Add(new MenuItem(new GUIContent(path), true, false, null));
        }

        // Get number of items in the menu
        public int GetItemCount()
        {
            return m_MenuItems.Count;
        }

        internal sealed class MenuItem
        {
            public GUIContent content;
            public bool separator;
            public bool on;
            public MenuFunction func;
            public MenuFunction2 func2;
            public object userData;
            public MenuItem(GUIContent _content, bool _separator, bool _on, MenuFunction _func)
            {
                content = _content;
                separator = _separator;
                on = _on;
                func = _func;
            }

            public MenuItem(GUIContent _content, bool _separator, bool _on, MenuFunction2 _func, object _userData)
            {
                content = _content;
                separator = _separator;
                on = _on;
                func2 = _func;
                userData = _userData;
            }

            public override string ToString()
            {
                var label = content?.text;
                if (separator)
                    return label + "[separator]";
                return label;
            }
        }

        /// <summary>
        /// Show the menu under the mouse when used in an OnGUI callback.
        /// </summary>
        public void ShowAsContext()
        {
            if (Event.current == null)
                return;
            DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        /// <summary>
        /// Show the menu at the given rect relative to the current window in an OnGUI callback.
        /// </summary>
        /// <param name="position"></param>
        public void DropDown(Rect position)
        {
            DropDown(position, false);
        }

        /// <summary>
        /// Show the menu at the given rect relative to the current window in an OnGUI callback.
        /// </summary>
        /// <param name="position"></param>
        internal void DropDown(Rect position, bool shouldDiscardMenuOnSecondClick)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;
            DropDownScreenSpace(position, shouldDiscardMenuOnSecondClick);
        }

        /// <summary>
        /// Show the menu at the given screen rect.
        /// </summary>
        /// <param name="position"></param>
        internal void DropDownScreenSpace(Rect position, bool shouldDiscardMenuOnSecondClick)
        {
            string[] titles = new string[m_MenuItems.Count];
            bool[] enabled = new bool[m_MenuItems.Count];
            ArrayList selected = new ArrayList();
            bool[] separator = new bool[m_MenuItems.Count];

            for (int idx = 0; idx < m_MenuItems.Count; idx++)
            {
                MenuItem item = (MenuItem)m_MenuItems[idx];
                titles[idx] = item.content.text;
                enabled[idx] = ((item.func != null) || (item.func2 != null));
                separator[idx] = item.separator;
                if (item.on)
                    selected.Add(idx);
            }
            
            EditorUtility.DisplayCustomMenuWithSeparatorsWithScreenSpacePosition(position, titles, enabled, separator, (int[])selected.ToArray(typeof(int)), CatchMenu, null, true, allowDuplicateNames, shouldDiscardMenuOnSecondClick);
        }

        // Display as a popup with /selectedIndex/. How this behaves depends on the platform (on Mac, it'll try to scroll the menu to the right place)
        internal void Popup(Rect position, int selectedIndex)
        {
            DropDown(position);
        }

        private void CatchMenu(object userData, string[] options, int selected)
        {
            MenuItem i = (MenuItem)m_MenuItems[selected];
            if (i.func2 != null)
                i.func2(i.userData);
            else if (i.func != null)
                i.func();
        }

        // Show object context menu with builtin menu items plus the ones from this GenericMenu.
        internal void ObjectContextDropDown(Rect position, Object[] context, int contextUserData)
        {
            string[] titles = new string[m_MenuItems.Count];
            bool[] enabled = new bool[m_MenuItems.Count];
            ArrayList selected = new ArrayList();
            bool[] separator = new bool[m_MenuItems.Count];

            for (int idx = 0; idx < m_MenuItems.Count; idx++)
            {
                MenuItem item = (MenuItem)m_MenuItems[idx];
                titles[idx] = item.content.text;
                enabled[idx] = ((item.func != null) || (item.func2 != null));
                separator[idx] = item.separator;
                if (item.on)
                    selected.Add(idx);
            }

            EditorUtility.DisplayObjectContextPopupMenuWithExtraItems(
                position, context, contextUserData,
                titles, enabled, separator, (int[])selected.ToArray(typeof(int)), CatchMenu, null, true);
        }
    }
}
