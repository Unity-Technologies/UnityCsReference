// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Default implementation of a <see cref="TreeViewController"/>.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public class DefaultTreeViewController<T> : TreeViewController, IDefaultTreeViewController, IDefaultTreeViewController<T>
    {
        TreeDataController<T> m_TreeDataController;
        TreeDataController<T> treeDataController => m_TreeDataController ??= new TreeDataController<T>();

        /// <inheritdoc />
        public override IList itemsSource
        {
            get => base.itemsSource;
            set
            {
                if (value == null)
                {
                    SetRootItems(null);
                }
                else if (value is IList<TreeViewItemData<T>> dataList)
                {
                    SetRootItems(dataList);
                }
                else
                {
                    Debug.LogError($"Type does not match this tree view controller's data type ({typeof(T)}).");
                }
            }
        }

        /// <summary>
        /// Sets the root items.
        /// </summary>
        /// <remarks>
        /// Root items can include their children directly.
        /// </remarks>
        /// <param name="items">The TreeView root items.</param>
        public void SetRootItems(IList<TreeViewItemData<T>> items)
        {
            if (items == base.itemsSource)
                return;

            treeDataController.SetRootItems(items);
            RebuildTree();
            RaiseItemsSourceChanged();
        }

        /// <summary>
        /// Adds an item to the tree.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <param name="parentId">The parent id for the item.</param>
        /// <param name="childIndex">The child index in the parent's children list.</param>
        /// <param name="rebuildTree">Whether the tree data should be rebuilt right away. Call <see cref="TreeViewController.RebuildTree()"/> when <c>false</c>.</param>
        public virtual void AddItem(in TreeViewItemData<T> item, int parentId, int childIndex, bool rebuildTree = true)
        {
            treeDataController.AddItem(item, parentId, childIndex);

            if (rebuildTree)
                RebuildTree();
        }

        /// <inheritdoc />
        public override bool TryRemoveItem(int id, bool rebuildTree = true)
        {
            if (treeDataController.TryRemoveItem(id))
            {
                if (rebuildTree)
                    RebuildTree();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the tree item data for the specified TreeView item ID.
        /// </summary>
        /// <param name="id">The TreeView item ID.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The tree item data.</returns>
        public virtual object GetItemDataForId(int id)
        {
            return treeDataController.GetTreeItemDataForId(id).data;
        }

        /// <summary>
        /// Gets the tree item data for the specified TreeView item ID.
        /// </summary>
        /// <param name="id">The TreeView item ID.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The tree item data.</returns>
        public virtual TreeViewItemData<T> GetTreeViewItemDataForId(int id)
        {
            return treeDataController.GetTreeItemDataForId(id);
        }

        /// <summary>
        /// Gets the tree item data for the specified TreeView item index.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The tree item data.</returns>
        public virtual TreeViewItemData<T> GetTreeViewItemDataForIndex(int index)
        {
            var itemId = GetIdForIndex(index);
            return treeDataController.GetTreeItemDataForId(itemId);
        }

        /// <summary>
        /// Gets data for the specified TreeView item ID.
        /// </summary>
        /// <param name="id">The TreeView item ID.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The data.</returns>
        public virtual T GetDataForId(int id)
        {
            return treeDataController.GetDataForId(id);
        }

        /// <summary>
        /// Gets data for the specified TreeView item index.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The data.</returns>
        public virtual T GetDataForIndex(int index)
        {
            return treeDataController.GetDataForId(GetIdForIndex(index));
        }

        /// <inheritdoc />
        public override object GetItemForIndex(int index)
        {
            return treeDataController.GetDataForId(GetIdForIndex(index));
        }

        /// <inheritdoc />
        public override int GetParentId(int id)
        {
            return treeDataController.GetParentId(id);
        }

        /// <inheritdoc />
        public override bool HasChildren(int id)
        {
            return treeDataController.HasChildren(id);
        }

        /// <inheritdoc />
        public override IEnumerable<int> GetChildrenIds(int id)
        {
            return treeDataController.GetChildrenIds(id);
        }

        /// <inheritdoc />
        public override void Move(int id, int newParentId, int childIndex = -1, bool rebuildTree = true)
        {
            if (id == newParentId)
                return;

            if (IsChildOf(newParentId, id))
                return;

            treeDataController.Move(id, newParentId, childIndex);

            if (rebuildTree)
            {
                RebuildTree();
                RaiseItemParentChanged(id, newParentId);
            }
        }

        bool IsChildOf(int childId, int id)
        {
            return treeDataController.IsChildOf(childId, id);
        }

        /// <inheritdoc />
        public override IEnumerable<int> GetAllItemIds(IEnumerable<int> rootIds = null)
        {
            return treeDataController.GetAllItemIds(rootIds);
        }
    }
}
