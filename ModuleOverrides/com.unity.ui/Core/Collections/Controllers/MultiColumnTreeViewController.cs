// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Multi-column tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="MultiColumnTreeView"/> inheritor.
    /// </summary>
    public abstract class MultiColumnTreeViewController : BaseTreeViewController
    {
        MultiColumnController m_ColumnController;

        /// <summary>
        /// The column controller, taking care of operations on the header.
        /// </summary>
        public MultiColumnController columnController => m_ColumnController;

        internal MultiColumnCollectionHeader header => m_ColumnController?.header;

        /// <summary>
        /// The constructor for MultiColumnTreeViewController.
        /// </summary>
        /// <param name="columns">The columns data used to initialize the header.</param>
        /// <param name="sortDescriptions">The sort data used to initialize the header.</param>
        /// <param name="sortedColumns">The sorted columns for the view.</param>
        protected MultiColumnTreeViewController(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
        {
            m_ColumnController = new MultiColumnController(columns, sortDescriptions, sortedColumns);
        }

        internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableMultiColumnTreeViewItem treeItem)
            {
                treeItem.Init(MakeItem(), m_ColumnController.header.columns);
                PostInitRegistration(treeItem);
            }
            else
            {
                base.InvokeMakeItem(reusableItem);
            }
        }

        /// <inheritdoc />
        protected override VisualElement MakeItem()
        {
            return m_ColumnController.MakeItem();
        }

        /// <inheritdoc />
        protected override void BindItem(VisualElement element, int index)
        {
            m_ColumnController.BindItem(element, index, GetItemForIndex(index));
        }

        /// <inheritdoc />
        protected override void UnbindItem(VisualElement element, int index)
        {
            m_ColumnController.UnbindItem(element, index);
        }

        /// <inheritdoc />
        protected override void DestroyItem(VisualElement element)
        {
            m_ColumnController.DestroyItem(element);
        }

        /// <inheritdoc />
        protected override void PrepareView()
        {
            m_ColumnController.PrepareView(view);
        }

        /// <summary>
        /// Unregisters events and removes the header from the hierarchy.
        /// </summary>
        public override void Dispose()
        {
            m_ColumnController.Dispose();
            m_ColumnController = null;
            base.Dispose();
        }
    }

    class DefaultMultiColumnTreeViewController<T> : MultiColumnTreeViewController, IDefaultTreeViewController<T>
    {
        TreeDataController<T> m_TreeDataController;
        TreeDataController<T> treeDataController => m_TreeDataController ??= new TreeDataController<T>();

        public DefaultMultiColumnTreeViewController(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
            : base(columns, sortDescriptions, sortedColumns) {}

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

        public void SetRootItems(IList<TreeViewItemData<T>> items)
        {
            if (items == base.itemsSource)
                return;

            treeDataController.SetRootItems(items);
            RebuildTree();
            RaiseItemsSourceChanged();
        }

        public void AddItem(in TreeViewItemData<T> item, int parentId, int childIndex, bool rebuildTree = true)
        {
            treeDataController.AddItem(item, parentId, childIndex);

            if (rebuildTree)
                RebuildTree();
        }

        public TreeViewItemData<T> GetTreeViewItemDataForId(int id)
        {
            return treeDataController.GetTreeItemDataForId(id);
        }

        public TreeViewItemData<T> GetTreeViewItemDataForIndex(int index)
        {
            var itemId = GetIdForIndex(index);
            return treeDataController.GetTreeItemDataForId(itemId);
        }

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

        public T GetDataForId(int id)
        {
            return treeDataController.GetDataForId(id);
        }

        public T GetDataForIndex(int index)
        {
            return treeDataController.GetDataForId(GetIdForIndex(index));
        }

        public override object GetItemForIndex(int index)
        {
            return treeDataController.GetDataForId(GetIdForIndex(index));
        }

        public override int GetParentId(int id)
        {
            return treeDataController.GetParentId(id);
        }

        public override bool HasChildren(int id)
        {
            return treeDataController.HasChildren(id);
        }

        public override IEnumerable<int> GetChildrenIds(int id)
        {
            return treeDataController.GetChildrenIds(id);
        }

        public override void Move(int id, int newParentId, int childIndex = -1, bool rebuildTree = true)
        {
            if (id == newParentId)
                return;

            if (IsChildOf(newParentId, id))
                return;

            treeDataController.Move(id, newParentId, childIndex);

            if (rebuildTree)
                RebuildTree();

            RaiseItemIndexChanged(id, newParentId);
        }

        bool IsChildOf(int childId, int id)
        {
            return treeDataController.IsChildOf(childId, id);
        }

        public override IEnumerable<int> GetAllItemIds(IEnumerable<int> rootIds = null)
        {
            return treeDataController.GetAllItemIds(rootIds);
        }
    }
}
