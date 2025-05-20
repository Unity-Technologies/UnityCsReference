// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class TreeViewItem<TIdentifier> : System.IComparable<TreeViewItem<TIdentifier>>
    {
        TIdentifier m_ID; // The id should be unique for all items in TreeView because it is used for searching, selection etc.
        TreeViewItem<TIdentifier> m_Parent;
        List<TreeViewItem<TIdentifier>> m_Children = null;
        int m_Depth;
        string m_DisplayName;
        Texture2D m_Icon;

        public TreeViewItem() { }

        public TreeViewItem(TIdentifier id)
        {
            m_ID = id;
        }

        public TreeViewItem(TIdentifier id, int depth)
        {
            m_ID = id;
            m_Depth = depth;
        }

        public TreeViewItem(TIdentifier id, int depth, string displayName)
        {
            m_Depth = depth;
            m_ID = id;
            m_DisplayName = displayName;
        }

        internal TreeViewItem(TIdentifier id, int depth, TreeViewItem<TIdentifier> parent, string displayName)
        {
            m_Depth = depth;
            m_Parent = parent;
            m_ID = id;
            m_DisplayName = displayName;
        }

        public virtual TIdentifier id { get { return m_ID; } set { m_ID = value; } }
        public virtual string displayName { get { return m_DisplayName; } set { m_DisplayName = value; } }
        public virtual int depth { get { return m_Depth; } set { m_Depth = value; } }
        public virtual bool hasChildren { get { return m_Children != null && m_Children.Count > 0; } }

        internal virtual List<TreeViewItem<TIdentifier>> childrenInternal { get { return m_Children; } set { m_Children = value; } }

        public virtual List<TreeViewItem<TIdentifier>> children { get { return childrenInternal; } set { childrenInternal = value; } }

        internal virtual TreeViewItem<TIdentifier> ParentInternal { get { return m_Parent; } set { m_Parent = value; } }

        public virtual TreeViewItem<TIdentifier> parent { get => ParentInternal; set { ParentInternal = value; } }

        public virtual Texture2D icon { get { return m_Icon; } set { m_Icon = value; } }

        public void AddChild(TreeViewItem<TIdentifier> child)
        {
            if (m_Children == null)
                m_Children = new List<TreeViewItem<TIdentifier>>();

            m_Children.Add(child);

            if (child != null)
                child.parent = this;
        }

        internal virtual int CompareToInternal(TreeViewItem<TIdentifier> other)
        {
            return displayName.CompareTo(other.displayName);
        }

        public virtual int CompareTo(TreeViewItem<TIdentifier> other)
        {
            return CompareToInternal(other);
        }

        public override string ToString()
        {
            return string.Format("Item: '{0}' ({1}), has {2} children, depth {3}, parent id {4}", displayName, id, hasChildren ? children.Count : 0, depth, (parent != null) ? parent.id : -1);
        }
    }

    internal static class TreeViewItemExtension
    {
        internal static bool Exists<TIdentifier>(this TreeViewItem<TIdentifier> parentItem, Func<TreeViewItem<TIdentifier>, bool> condition)
        {
            foreach (TreeViewItem<TIdentifier> tvitem in parentItem.hasChildren ? parentItem.children : new List<TreeViewItem<TIdentifier>>())
            {
                if (condition(tvitem))
                    return true;

                if (tvitem.Exists(condition))
                    return true;
            }
            return false;
        }
    }

    class TreeViewItemAlphaNumericSort<TIdentifier> : IComparer<TreeViewItem<TIdentifier>>
    {
        public int Compare(TreeViewItem<TIdentifier> lhs, TreeViewItem<TIdentifier> rhs)
        {
            if (lhs == rhs) return 0;
            if (lhs == null) return -1;
            if (rhs == null) return 1;

            return EditorUtility.NaturalCompare(lhs.displayName, rhs.displayName);
        }
    }
} // UnityEditor
