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
        Texture2D m_Icon;
        int m_Id;
        int m_ElementIndex = -1;
        bool m_Enabled = true;
        List<AdvancedDropdownItem> m_Children = new List<AdvancedDropdownItem>();

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public Texture2D icon
        {
            get { return m_Icon; }
            set { m_Icon = value; }
        }

        public int id
        {
            get
            {
                return m_Id;
            }
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

        public IEnumerable<AdvancedDropdownItem> children => m_Children;

        public void AddChild(AdvancedDropdownItem child)
        {
            m_Children.Add(child);
        }

        static readonly AdvancedDropdownItem k_SeparatorItem = new SeparatorDropdownItem();

        public AdvancedDropdownItem(string name)
        {
            m_Name = name;
            m_Id = name.GetHashCode();
        }

        public virtual int CompareTo(object o)
        {
            return name.CompareTo((o as AdvancedDropdownItem).name);
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

        class SeparatorDropdownItem : AdvancedDropdownItem
        {
            public SeparatorDropdownItem() : base("SEPARATOR")
            {
            }
        }
    }
}
