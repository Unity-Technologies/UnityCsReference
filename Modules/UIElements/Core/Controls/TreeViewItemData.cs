// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Item structure provided to a TreeView using the default implementation. For more information on usage,
    /// refer to [[wiki:UIE-uxml-element-TreeView|TreeView]] and [[wiki:UIE-ListView-TreeView|Create list and tree views]].
    /// </summary>
    /// <typeparam name="T">Data type the TreeView will hold.</typeparam>
    public readonly struct TreeViewItemData<T>
    {
        /// <summary>
        /// Id of the item.
        /// </summary>
        public int id { get; }

        /// <summary>
        /// Data for this item.
        /// </summary>
        public T data => m_Data;

        /// <summary>
        /// Children of this tree item.
        /// </summary>
        public IEnumerable<TreeViewItemData<T>> children => m_Children;

        /// <summary>
        /// Whether this item has children or not.
        /// </summary>
        public bool hasChildren => m_Children != null && m_Children.Count > 0;

        [CreateProperty]
        readonly T m_Data;
        readonly IList<TreeViewItemData<T>> m_Children;

        /// <summary>
        /// Creates a <see cref="TreeViewItemData{T}"/> with all required parameters.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="data">The item data.</param>
        /// <param name="children">The item's children.</param>
        public TreeViewItemData(int id, T data, List<TreeViewItemData<T>> children = null)
        {
            this.id = id;
            m_Data = data;
            m_Children = children ?? new List<TreeViewItemData<T>>();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void AddChild(TreeViewItemData<T> child)
        {
            m_Children.Add(child);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void AddChildren(IList<TreeViewItemData<T>> children)
        {
            foreach (var child in children)
                AddChild(child);
        }

        internal void InsertChild(TreeViewItemData<T> child, int index)
        {
            if (index < 0 || index >= m_Children.Count)
                m_Children.Add(child);
            else
                m_Children.Insert(index, child);
        }

        internal void RemoveChild(int childId)
        {
            if (m_Children == null)
                return;

            for (var i = 0; i < m_Children.Count; i++)
            {
                if (childId == m_Children[i].id)
                {
                    m_Children.RemoveAt(i);
                    break;
                }
            }
        }

        internal int GetChildIndex(int itemId)
        {
            var index = 0;
            foreach (var child in m_Children)
            {
                if (child.id == itemId)
                    return index;

                index++;
            }

            return -1;
        }

        internal void ReplaceChild(TreeViewItemData<T> newChild)
        {
            if (!hasChildren)
                return;

            var i = 0;
            foreach (var child in m_Children)
            {
                if (child.id == newChild.id)
                {
                    m_Children.RemoveAt(i);
                    m_Children.Insert(i, newChild);
                    break;
                }

                i++;
            }
        }
    }
}
