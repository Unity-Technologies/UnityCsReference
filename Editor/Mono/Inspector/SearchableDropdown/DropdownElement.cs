// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Event = UnityEngine.Event;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [Serializable]
    internal class DropdownElement : IComparable
    {
        protected GUIContent m_Content;
        protected GUIContent m_ContentWhenSearching;

        protected virtual GUIStyle labelStyle
        {
            get { return AdvancedDropdownWindow.s_Styles.componentButton; }
        }

        private string m_Name;
        public string name
        {
            get { return m_Name; }
        }

        private string m_Id;
        public string id
        {
            get { return m_Id; }
        }

        private DropdownElement m_Parent;
        public DropdownElement parent
        {
            get { return m_Parent; }
        }

        private List<DropdownElement> m_Children = new List<DropdownElement>();
        public List<DropdownElement> children
        {
            get { return m_Children; }
        }

        protected virtual bool drawArrow { get; }
        protected virtual bool isSearchable { get; }

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

        public DropdownElement(string name) : this(name, name, -1)
        {
        }

        public DropdownElement(string name, string id) : this(name, id, -1)
        {
        }

        public DropdownElement(string name, int index) : this(name, name, index)
        {
        }

        public DropdownElement(string name, string id, int index)
        {
            m_Content = new GUIContent(name);
            m_ContentWhenSearching = new GUIContent(id);
            m_Name = name;
            m_Id = id;
            m_Index = index;
        }

        internal void AddChild(DropdownElement element)
        {
            children.Add(element);
        }

        public void SetParent(DropdownElement element)
        {
            m_Parent = element;
        }

        public virtual bool OnAction()
        {
            return true;
        }

        public virtual void Draw(bool selected, bool isSearching)
        {
            var content = !isSearching ? m_Content : m_ContentWhenSearching;

            var rect = GUILayoutUtility.GetRect(content, labelStyle, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            labelStyle.Draw(rect, content, false, false, selected, selected);
            if (drawArrow)
            {
                Rect arrowRect = new Rect(rect.x + rect.width - 13, rect.y + 4, 13, 13);
                AdvancedDropdownWindow.s_Styles.rightArrow.Draw(arrowRect, false, false, false, false);
            }
        }

        public DropdownElement GetSelectedChild()
        {
            if (children.Count == 0)
                return null;
            return children[m_SelectedItem];
        }

        public virtual int GetSelectedChildIndex()
        {
            var i = children[m_SelectedItem].m_Index;
            if (i >= 0)
            {
                return i;
            }
            return m_SelectedItem;
        }

        public IEnumerable<DropdownElement> GetSearchableElements()
        {
            if (isSearchable)
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
            return name.CompareTo((o as DropdownElement).name);
        }
    }

    internal class SearchableDropdownElement : DropdownElement
    {
        protected override bool isSearchable
        {
            get { return true; }
        }

        public SearchableDropdownElement(string name, int index) : this(name, name, index)
        {
        }

        public SearchableDropdownElement(string name, string id, int index) : base(name, id, index)
        {
        }
    }
}
