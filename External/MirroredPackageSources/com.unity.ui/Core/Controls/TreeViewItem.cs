using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface ITreeViewItem
    {
        int id { get; }

        ITreeViewItem parent { get; }

        IEnumerable<ITreeViewItem> children { get; }

        bool hasChildren { get; }

        void AddChild(ITreeViewItem child);

        void AddChildren(IList<ITreeViewItem> children);

        void RemoveChild(ITreeViewItem child);
    }

    internal class TreeViewItem<T> : ITreeViewItem
    {
        public int id { get; private set; }

        internal TreeViewItem<T> m_Parent;
        public ITreeViewItem parent => m_Parent;

        List<ITreeViewItem> m_Children;
        public IEnumerable<ITreeViewItem> children { get { return m_Children; } }

        public bool hasChildren { get { return m_Children != null && m_Children.Count > 0; } }

        public T data { get; private set; }

        public TreeViewItem(int id, T data, List<TreeViewItem<T>> children = null)
        {
            this.id = id;
            this.data = data;

            if (children != null)
                foreach (var child in children)
                    AddChild(child);
        }

        public void AddChild(ITreeViewItem child)
        {
            var treeChild = child as TreeViewItem<T>;
            if (treeChild == null)
                return;

            if (m_Children == null)
                m_Children = new List<ITreeViewItem>();

            m_Children.Add(treeChild);

            treeChild.m_Parent = this;
        }

        public void AddChildren(IList<ITreeViewItem> children)
        {
            foreach (var child in children)
                AddChild(child);
        }

        public void RemoveChild(ITreeViewItem child)
        {
            if (m_Children == null)
                return;

            var treeChild = child as TreeViewItem<T>;
            if (treeChild == null)
                return;

            m_Children.Remove(treeChild);
        }
    }
}
