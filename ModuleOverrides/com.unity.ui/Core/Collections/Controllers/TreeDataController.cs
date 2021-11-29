// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides a set of functionality to control the data in a UI Toolkit tree view.
    /// </summary>
    /// <typeparam name="T">The data type used in the tree.</typeparam>
    internal sealed class TreeDataController<T>
    {
        TreeData<T> m_TreeData;

        Stack<IEnumerator<int>> m_IteratorStack = new Stack<IEnumerator<int>>();

        /// <summary>
        /// Sets the root items.
        /// </summary>
        /// <remarks>
        /// Root items can include their children directly.
        /// </remarks>
        /// <param name="rootItems">The TreeView root items.</param>
        public void SetRootItems(IList<TreeViewItemData<T>> rootItems)
        {
            m_TreeData = new TreeData<T>(rootItems);
        }

        /// <summary>
        /// Adds an item to the tree.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="parentId">The parent id for the item.</param>
        /// <param name="childIndex">The child index in the parent's children list.</param>
        public void AddItem(in TreeViewItemData<T> item, int parentId, int childIndex)
        {
            m_TreeData.AddItem(item, parentId, childIndex);
        }

        /// <summary>
        /// Removes an item of the tree if it can find it.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>If the item was removed from the tree.</returns>
        public bool TryRemoveItem(int id)
        {
            return m_TreeData.TryRemove(id);
        }

        /// <summary>
        /// Gets tree item data for the specified TreeView item id.
        /// </summary>
        /// <param name="id">The TreeView item id.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The tree item data.</returns>
        public TreeViewItemData<T> GetTreeItemDataForId(int id)
        {
            return m_TreeData.GetDataForId(id);
        }

        /// <summary>
        /// Gets data for the specified TreeView item id.
        /// </summary>
        /// <param name="id">The TreeView item id.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The data.</returns>
        public T GetDataForId(int id)
        {
            return m_TreeData.GetDataForId(id).data;
        }

        /// <summary>
        /// Gets the specified TreeView item's parent identifier.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>The item's parent identifier.</returns>
        public int GetParentId(int id)
        {
            return m_TreeData.GetParentId(id);
        }

        /// <summary>
        /// Returns whether or not the item with the specified id has children.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>Whether or not the item has children.</returns>
        public bool HasChildren(int id)
        {
            return m_TreeData.GetDataForId(id).hasChildren;
        }

        static IEnumerable<int> GetItemIds(IEnumerable<TreeViewItemData<T>> items)
        {
            if (items == null)
                yield break;

            foreach (var item in items)
                yield return item.id;
        }

        /// <summary>
        /// Gets all children ids from the item with the specified id.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>An enumerable of all children ids.</returns>
        public IEnumerable<int> GetChildrenIds(int id)
        {
            var item = m_TreeData.GetDataForId(id);
            return GetItemIds(item.children);
        }

        /// <summary>
        /// Moves an item by id, to a new parent and child index.
        /// </summary>
        /// <param name="id">The id of the item to move.</param>
        /// <param name="newParentId">The new parent id. -1 if moved at the root.</param>
        /// <param name="childIndex">The child index to insert at under the parent. -1 will add as the last child.</param>
        public void Move(int id, int newParentId, int childIndex = -1)
        {
            if (id == newParentId)
                return;

            if (IsChildOf(newParentId, id))
                return;

            m_TreeData.Move(id, newParentId, childIndex);
        }

        /// <summary>
        /// Returns whether or not the child id is somewhere in the hierarchy below the item with the specified id.
        /// </summary>
        /// <param name="childId">The child id to look for.</param>
        /// <param name="id">The starting id in the tree.</param>
        /// <returns>Whether or not the child item is found in the tree below the specified item.</returns>
        public bool IsChildOf(int childId, int id)
        {
            return m_TreeData.HasAncestor(childId, id);
        }

        /// <summary>
        /// Returns all item ids that can be found in the tree, optionally specifying root ids from where to start.
        /// </summary>
        /// <param name="rootIds">Root ids to start from. If null, will use the tree root ids.</param>
        /// <returns>All items ids in the tree, starting from the specified ids.</returns>
        public IEnumerable<int> GetAllItemIds(IEnumerable<int> rootIds = null)
        {
            m_IteratorStack.Clear();

            if (rootIds == null)
            {
                if (m_TreeData.rootItemIds == null)
                    yield break;

                rootIds = m_TreeData.rootItemIds;
            }

            var currentIterator = rootIds.GetEnumerator();

            while (true)
            {
                var hasNext = currentIterator.MoveNext();
                if (!hasNext)
                {
                    if (m_IteratorStack.Count > 0)
                    {
                        currentIterator = m_IteratorStack.Pop();
                        continue;
                    }

                    // We're at the end of the root items list.
                    break;
                }

                var currentItemId = currentIterator.Current;
                yield return currentItemId;

                if (HasChildren(currentItemId))
                {
                    m_IteratorStack.Push(currentIterator);
                    currentIterator = GetChildrenIds(currentItemId).GetEnumerator();
                }
            }
        }
    }
}
