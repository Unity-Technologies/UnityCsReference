// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Event = UnityEngine.Event;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AdvancedDropdownItem : IComparable
    {
        internal static AdvancedDropdownItem s_SeparatorItem = new SeparatorDropdownItem();

        private static class Styles
        {
            public static GUIStyle itemStyle = new GUIStyle("PR Label");

            static Styles()
            {
                itemStyle.alignment = TextAnchor.MiddleLeft;
                itemStyle.padding = new RectOffset(0, 0, 2, 2);
                itemStyle.margin = new RectOffset(0, 0, 0, 0);
            }
        }

        public virtual GUIStyle lineStyle => Styles.itemStyle;

        protected GUIContent m_Content;
        public virtual GUIContent content => m_Content;

        protected GUIContent m_ContentWhenSearching;
        public virtual GUIContent contentWhenSearching => m_ContentWhenSearching;

        private string m_Name;
        public string name => m_Name;

        private string m_Id;
        public string id => m_Id;

        private AdvancedDropdownItem m_Parent;
        public virtual AdvancedDropdownItem parent => m_Parent;

        private List<AdvancedDropdownItem> m_Children = new List<AdvancedDropdownItem>();
        public virtual List<AdvancedDropdownItem> children => m_Children;

        public bool hasChildren => children.Any();
        public virtual bool drawArrow => hasChildren;

        public virtual bool searchable { get; set; }

        internal int m_Index = -1;
        internal Vector2 m_Scroll;
        internal int m_SelectedItem = 0;

        public int selectedItem
        {
            get { return m_SelectedItem; }
            set
            {
                if (value < 0)
                {
                    m_SelectedItem = 0;
                }
                else if (value >= children.Count)
                {
                    m_SelectedItem = children.Count - 1;
                }
                else
                {
                    m_SelectedItem = value;
                }
            }
        }

        public AdvancedDropdownItem(string name, int index) : this(name, name, index)
        {
        }

        public AdvancedDropdownItem(string name, string id, int index)
        {
            m_Content = new GUIContent(name);
            m_ContentWhenSearching = new GUIContent(id);
            m_Name = name;
            m_Id = id;
            m_Index = index;
        }

        public AdvancedDropdownItem(GUIContent content, int index) : this(content, content, index)
        {
        }

        public AdvancedDropdownItem(GUIContent content, GUIContent contentWhenSearching, int index)
        {
            m_Content = content;
            m_ContentWhenSearching = contentWhenSearching;
            m_Name = content.text;
            m_Id = contentWhenSearching.text;
            m_Index = index;
        }

        internal void AddChild(AdvancedDropdownItem item)
        {
            children.Add(item);
        }

        internal void SetParent(AdvancedDropdownItem item)
        {
            m_Parent = item;
        }

        internal void AddSeparator()
        {
            children.Add(s_SeparatorItem);
        }

        internal virtual bool IsSeparator()
        {
            return false;
        }

        public virtual bool OnAction()
        {
            return true;
        }

        public AdvancedDropdownItem GetSelectedChild()
        {
            if (children.Count == 0 || m_SelectedItem < 0)
                return null;
            return children[m_SelectedItem];
        }

        public int GetSelectedChildIndex()
        {
            var i = children[m_SelectedItem].m_Index;
            if (i >= 0)
            {
                return i;
            }
            return m_SelectedItem;
        }

        public IEnumerable<AdvancedDropdownItem> GetSearchableElements()
        {
            if (searchable)
                yield return this;
            foreach (var child in children)
            {
                foreach (var searchableChildren in child.GetSearchableElements())
                {
                    yield return searchableChildren;
                }
            }
        }

        public virtual int CompareTo(object o)
        {
            return name.CompareTo((o as AdvancedDropdownItem).name);
        }

        public void MoveDownSelection()
        {
            var selectedIndex = selectedItem;
            do
            {
                ++selectedIndex;
            }
            while (selectedIndex < children.Count && children[selectedIndex].IsSeparator());

            if (selectedIndex < children.Count)
                selectedItem = selectedIndex;
        }

        public void MoveUpSelection()
        {
            var selectedIndex = selectedItem;
            do
            {
                --selectedIndex;
            }
            while (selectedIndex >= 0 && children[selectedIndex].IsSeparator());
            if (selectedIndex >= 0)
                selectedItem = selectedIndex;
        }

        class SeparatorDropdownItem : AdvancedDropdownItem
        {
            public SeparatorDropdownItem() : base("SEPARATOR", -1)
            {
            }

            internal override bool IsSeparator()
            {
                return true;
            }
        }
    }
}
