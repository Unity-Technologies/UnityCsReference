// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class TreeViewItem : System.IComparable<TreeViewItem>
    {
        int m_ID; // The id should be unique for all items in TreeView because it is used for searching, selection etc.
        TreeViewItem m_Parent;
        List<TreeViewItem> m_Children = null;
        int m_Depth;
        string m_DisplayName;
        Texture2D m_Icon;

        public TreeViewItem() {}

        public TreeViewItem(int id)
        {
            m_ID = id;
        }

        public TreeViewItem(int id, int depth)
        {
            m_ID = id;
            m_Depth = depth;
        }

        public TreeViewItem(int id, int depth, string displayName)
        {
            m_Depth = depth;
            m_ID = id;
            m_DisplayName = displayName;
        }

        internal TreeViewItem(int id, int depth, TreeViewItem parent, string displayName)
        {
            m_Depth = depth;
            m_Parent = parent;
            m_ID = id;
            m_DisplayName = displayName;
        }

        public virtual int id { get { return m_ID; } set { m_ID = value; }}
        public virtual string displayName { get { return m_DisplayName; } set { m_DisplayName = value; } }
        public virtual int depth { get { return m_Depth; } set { m_Depth = value; } }
        public virtual bool hasChildren { get { return m_Children != null && m_Children.Count > 0; } }
        public virtual List<TreeViewItem> children { get { return m_Children; } set { m_Children = value; } }
        public virtual TreeViewItem parent { get { return m_Parent; } set { m_Parent = value; } }
        public virtual Texture2D icon { get { return m_Icon; } set { m_Icon = value; } }

        public void AddChild(TreeViewItem child)
        {
            if (m_Children == null)
                m_Children = new List<TreeViewItem>();

            m_Children.Add(child);

            if (child != null)
                child.parent = this;
        }

        public virtual int CompareTo(TreeViewItem other)
        {
            return displayName.CompareTo(other.displayName);
        }

        public override string ToString()
        {
            return string.Format("Item: '{0}' ({1}), has {2} children, depth {3}, parent id {4}", displayName, id, hasChildren ? children.Count : 0, depth, (parent != null) ? parent.id : -1);
        }
    }

    class TreeViewItemAlphaNumericSort : IComparer<TreeViewItem>
    {
        public int Compare(TreeViewItem lhs, TreeViewItem rhs)
        {
            if (lhs == rhs) return 0;
            if (lhs == null) return -1;
            if (rhs == null) return 1;

            return EditorUtility.NaturalCompare(lhs.displayName, rhs.displayName);
        }
    }
} // UnityEditor
