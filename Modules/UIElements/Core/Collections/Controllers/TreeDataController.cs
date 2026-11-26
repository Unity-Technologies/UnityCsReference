// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides a set of functionality to control the data in a UI Toolkit tree view.
    /// </summary>
    /// <typeparam name="T">The data type used in the tree.</typeparam>
    internal sealed class TreeDataController<T>
    {
        Dictionary<HierarchyNode, TreeViewItemData<T>> m_NodeToItemDataDictionary = new();
        Stack<IEnumerator<TreeViewItemData<T>>> m_ItemStack = new();
        Stack<HierarchyNode> m_NodeStack = new();
        Stack<int> m_IdStack = new();
        HashSet<int> m_PathIds = new();

        /// <summary>
        /// Adds an item to the tree.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="node">The node that the item will be associated to.</param>
        public void AddItem(in TreeViewItemData<T> item, HierarchyNode node)
        {
            m_NodeToItemDataDictionary.TryAdd(node, item);
        }

        /// <summary>
        /// Removes an item of the tree if it can find it.
        /// </summary>
        /// <param name="node">The item node to be removed from the tree.</param>
        public void RemoveItem(HierarchyNode node)
        {
            m_NodeToItemDataDictionary.Remove(node);
        }

        /// <summary>
        /// Gets tree item data for the specified TreeView item id.
        /// </summary>
        /// <param name="node">The node representing a TreeView item.</param>
        /// <returns>The tree item data.</returns>
        public TreeViewItemData<T> GetTreeItemDataForNode(HierarchyNode node)
        {
            if (m_NodeToItemDataDictionary.TryGetValue(node, out var item))
                return item;

            return default;
        }

        /// <summary>
        /// Gets data for the specified node.
        /// </summary>
        /// <param name="node">The node representing a TreeView item.</param>
        /// <returns>The data.</returns>
        public T GetDataForNode(HierarchyNode node)
        {
            if (m_NodeToItemDataDictionary.TryGetValue(node, out var item))
                return item.data;

            return default;
        }

        // The below are internal utilities to update the dictionary map between a HierarchyNode to an TreeViewItemData<T>.
        // We should remove this once Hierarchy property supports managed types.
        internal void ConvertTreeViewItemDataToHierarchy(IEnumerable<TreeViewItemData<T>> list, Func<HierarchyNode, HierarchyNode> createNode, Action<int, HierarchyNode> updateDictionary, HashSet<int> ancestorIds = null)
        {
            if (list == null)
                return;

            m_ItemStack.Clear();
            m_NodeStack.Clear();
            m_IdStack.Clear();
            m_PathIds.Clear();
            if (ancestorIds != null)
                m_PathIds.UnionWith(ancestorIds);
            var currentIterator = list.GetEnumerator();
            HierarchyNode parentNode = HierarchyNode.Null;

            while (true)
            {
                var hasNext = currentIterator.MoveNext();
                if (!hasNext)
                {
                    if (m_ItemStack.Count > 0)
                    {
                        parentNode = m_NodeStack.Pop();
                        m_PathIds.Remove(m_IdStack.Pop());
                        currentIterator = m_ItemStack.Pop();
                        continue;
                    }

                    break;
                }

                var item = currentIterator.Current;
                if (m_PathIds.Contains(item.id))
                {
                    Debug.LogError("TreeView item id " + item.id + " is already used by one of its ancestors. The item and its children were ignored.");
                    continue;
                }

                var node = createNode(parentNode);
                UpdateNodeToDataDictionary(node, item);
                updateDictionary(item.id, node);

                if (item.children != null && ((IList<TreeViewItemData<T>>)item.children).Count > 0)
                {
                    // Push the parent node into the stack before updating it
                    m_NodeStack.Push(parentNode);
                    m_IdStack.Push(item.id);
                    m_PathIds.Add(item.id);
                    parentNode = node;
                    m_ItemStack.Push(currentIterator);
                    currentIterator = item.children.GetEnumerator();
                }
            }
        }

        internal void UpdateNodeToDataDictionary(HierarchyNode node, TreeViewItemData<T> item)
        {
            m_NodeToItemDataDictionary.TryAdd(node, item);
        }

        internal void ClearNodeToDataDictionary()
        {
            m_NodeToItemDataDictionary.Clear();
        }
    }
}
