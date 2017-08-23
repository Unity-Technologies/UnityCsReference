// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using System.Collections;


namespace UnityEditor
{




public sealed partial class GenericMenu
{
    
    
            public delegate void MenuFunction();
    
            public delegate void MenuFunction2(object userData);
    
    
    public void AddItem(GUIContent content, bool on, MenuFunction func)
        {
            menuItems.Add(new MenuItem(content, false, on, func));
        }
    
    public void AddItem(GUIContent content, bool on, MenuFunction2 func, object userData)
        {
            menuItems.Add(new MenuItem(content, false, on, func, userData));
        }
    
    public void AddDisabledItem(GUIContent content)
        {
            menuItems.Add(new MenuItem(content, false, false, null));
        }
    
    public void AddSeparator(string path)
        {
            menuItems.Add(new MenuItem(new GUIContent(path), true, false, null));
        }
    
    public int GetItemCount()
        {
            return menuItems.Count;
        }
    
            private ArrayList menuItems = new ArrayList();
    
    
    private sealed partial class MenuItem    
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
        
        
    }

    
    public void ShowAsContext()
        {
            if (Event.current == null)
                return;
            DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }
    
    public void DropDown(Rect position)
        {
            string[] titles = new string[menuItems.Count];
            bool[] enabled = new bool[menuItems.Count];
            ArrayList selected = new ArrayList();
            bool[] separator = new bool[menuItems.Count];

            for (int idx = 0; idx < menuItems.Count; idx++)
            {
                MenuItem item = (MenuItem)menuItems[idx];
                titles[idx] = item.content.text;
                enabled[idx] = ((item.func != null) || (item.func2 != null));
                separator[idx] = item.separator;
                if (item.on)
                    selected.Add(idx);
            }

            EditorUtility.DisplayCustomMenuWithSeparators(position, titles, enabled, separator, (int[])selected.ToArray(typeof(int)), CatchMenu, null, true);
        }
    
    internal void Popup(Rect position, int selectedIndex)
        {
            DropDown(position);
        }
    
    private void CatchMenu(object userData, string[] options, int selected)
        {
            MenuItem i = (MenuItem)menuItems[selected];
            if (i.func2 != null)
                i.func2(i.userData);
            else if (i.func != null)
                i.func();
        }
    
    
}



} 
