// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using Unity.Hierarchy;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Default implementation of a <see cref="TreeViewController"/>.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public class DefaultTreeViewController<T> : TreeViewController, IDefaultTreeViewController<T>
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

            if (m_Hierarchy.IsCreated)
            {
                ClearIdToNodeDictionary();
                treeDataController.ClearNodeToDataDictionary();

                // Recreate memory for the new dataset
                hierarchy = new Hierarchy();
            }

            if (items != null)
            {
                treeDataController.ConvertTreeViewItemDataToHierarchy(items, (node) => CreateNode(node), (id, node) => UpdateIdToNodeDictionary(id, node));
                UpdateHierarchy();

                // We want to sync the expanded state(s) if there's a viewDataKey
                if (IsViewDataKeyEnabled())
                    OnViewDataReadyUpdateNodes();
            }

            // Required to set the CollectionViewController's items source.
            SetHierarchyViewModelWithoutNotify(m_HierarchyViewModel);
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
            HierarchyNode node;

            if (parentId == BaseTreeView.invalidId)
            {
                node = CreateNode(HierarchyNode.Null);
            }
            else
            {
                var parentNode = GetHierarchyNodeById(parentId);
                node = CreateNode(parentNode);
                // Update our internal TreeViewItemData otherwise the content will be out of date when the user fetches it.
                var treeItemData = treeDataController.GetTreeItemDataForNode(parentNode);
                if (treeItemData.data != null)
                    treeItemData.InsertChild(item, childIndex);
            }

            treeDataController.AddItem(item, node);
            UpdateIdToNodeDictionary(item.id, node);
            UpdateHierarchy();

            // If the item being added contains children, we want to convert them into HierarchyNode(s). For example,
            // users can drive their TreeView solely with the AddItem and TryRemoveItem APIs
            if (item.children.GetCount() > 0)
            {
                var parentNode = GetHierarchyNodeById(item.id);

                treeDataController.ConvertTreeViewItemDataToHierarchy(
                    item.children,
                    (itemNode) => CreateNode(itemNode == HierarchyNode.Null ? parentNode : itemNode),
                    (id, newNode) =>
                {
                    UpdateIdToNodeDictionary(id, newNode);
                    UpdateHierarchy();
                });
            }

            if (baseTreeView.autoExpand)
                ExpandAncestorNodes(node);

            if (childIndex != -1)
                UpdateSortOrder(m_Hierarchy.GetParent(node), node, childIndex);
        }

        /// <summary>
        /// Gets the tree item data for the specified TreeView item ID.
        /// </summary>
        /// <param name="id">The TreeView item ID.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The tree item data.</returns>
        public virtual TreeViewItemData<T> GetTreeViewItemDataForId(int id)
        {
            return treeDataController.GetTreeItemDataForNode(GetHierarchyNodeById(id));
        }

        /// <summary>
        /// Gets the tree item data for the specified TreeView item index.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The tree item data.</returns>
        public virtual TreeViewItemData<T> GetTreeViewItemDataForIndex(int index)
        {
            var id = GetIdForIndex(index);
            return treeDataController.GetTreeItemDataForNode(GetHierarchyNodeById(id));
        }

        /// <inheritdoc />
        public override bool TryRemoveItem(int id, bool rebuildTree = true)
        {
            var node = GetHierarchyNodeById(id);
            if (node != HierarchyNode.Null)
            {
                // Update our internal TreeViewDataItem reference by removing the child from the parent - if applicable.
                var parentId = GetParentId(id);
                if (parentId != BaseTreeView.invalidId)
                {
                    var treeItemData = treeDataController.GetTreeItemDataForNode(GetHierarchyNodeById(parentId));
                    if (treeItemData.data != null)
                        treeItemData.RemoveChild(id);
                }

                RemoveAllChildrenItemsFromCollections(node, (hierarchyNode, itemId) =>
                {
                    treeDataController.RemoveItem(hierarchyNode);
                    UpdateIdToNodeDictionary(itemId, node, false);
                });
                treeDataController.RemoveItem(node);
                UpdateIdToNodeDictionary(id, node, false);
                m_Hierarchy.Remove(node);
                UpdateHierarchy();

                return true;
            }

            return false;
        }

        public override object GetItemForId(int id)
        {
            return treeDataController.GetTreeItemDataForNode(GetHierarchyNodeById(id)).data;
        }

        /// <summary>
        /// Gets data for the specified TreeView item ID.
        /// </summary>
        /// <param name="id">The TreeView item ID.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The data.</returns>
        public virtual T GetDataForId(int id)
        {
            return treeDataController.GetDataForNode(GetHierarchyNodeById(id));
        }

        /// <summary>
        /// Gets data for the specified TreeView item index.
        /// </summary>
        /// <param name="index">The TreeView item index.</param>
        /// <typeparam name="T">Type of the data inside TreeViewItemData.</typeparam>
        /// <returns>The data.</returns>
        public virtual T GetDataForIndex(int index)
        {
            return treeDataController.GetDataForNode(GetHierarchyNodeByIndex(index));
        }

        /// <inheritdoc />
        public override object GetItemForIndex(int index)
        {
            return treeDataController.GetDataForNode(GetHierarchyNodeByIndex(index));
        }
    }
}
