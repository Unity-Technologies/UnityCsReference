// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class AdvancedDropdownItem : IComparable
    {
        string m_Name;
        int m_Id;
        int m_ElementIndex = -1;
        bool m_Enabled = true;
        GUIContent m_Content;
        List<AdvancedDropdownItem> m_Children = new List<AdvancedDropdownItem>();

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        internal GUIContent content => m_Content;

        internal string tooltip
        {
            get => m_Content.tooltip;
            set { m_Content.tooltip = value; }
        }

        internal virtual string displayName
        {
            get => string.IsNullOrEmpty(m_Content.text) ? m_Name : m_Content.text;
            set { m_Content.text = value; }
        }

        public Texture2D icon
        {
            get => m_Content?.image as Texture2D;
            set { m_Content.image = value; }
        }

        public int id
        {
            get => m_Id;
            set { m_Id = value; }
        }

        internal int elementIndex
        {
            get { return m_ElementIndex; }
            set { m_ElementIndex = value; }
        }

        public bool enabled
        {
            get { return m_Enabled; }
            set { m_Enabled = value; }
        }

        internal object userData { get; set; }

        public IEnumerable<AdvancedDropdownItem> children => m_Children;

        internal bool hasChildren => m_Children.Count > 0;

        public void AddChild(AdvancedDropdownItem child)
        {
            m_Children.Add(child);
        }

        static readonly AdvancedDropdownItem k_SeparatorItem = new SeparatorDropdownItem();

        public AdvancedDropdownItem(string name)
        {
            m_Name = name;
            m_Id = name.GetHashCode();
            m_Content = new GUIContent(m_Name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public virtual int CompareTo(object o)
        {
            return string.CompareOrdinal(name, ((AdvancedDropdownItem)o).name);
        }

        public void AddSeparator()
        {
            AddChild(k_SeparatorItem);
        }

        internal bool IsSeparator()
        {
            return k_SeparatorItem == this;
        }

        public override string ToString()
        {
            return m_Name;
        }

        internal void SortChildren(Comparison<AdvancedDropdownItem> comparer, bool recursive = false)
        {
            if (recursive)
            {
                foreach (var child in m_Children)
                    child.SortChildren(comparer, recursive);
            }

            m_Children.Sort(comparer);
        }

        class SeparatorDropdownItem : AdvancedDropdownItem
        {
            public SeparatorDropdownItem() : base("SEPARATOR")
            {
            }
        }
    }
}
