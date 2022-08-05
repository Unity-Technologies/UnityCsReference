// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="TreeView"/> inheritor.
    /// </summary>
    public abstract class TreeViewController : BaseTreeViewController
    {
        /// <summary>
        /// View for this controller, cast as a <see cref="TreeView"/>.
        /// </summary>
        protected TreeView treeView => view as TreeView;

        /// <inheritdoc />
        protected override VisualElement MakeItem()
        {
            if (treeView.makeItem == null)
            {
                if (treeView.bindItem != null)
                    throw new NotImplementedException("You must specify makeItem if bindItem is specified.");
                return new Label();
            }
            return treeView.makeItem.Invoke();
        }

        /// <inheritdoc />
        protected override void BindItem(VisualElement element, int index)
        {
            if (treeView.bindItem == null)
            {
                if (treeView.makeItem != null)
                    throw new NotImplementedException("You must specify bindItem if makeItem is specified.");

                var label = (Label)element;
                var item = GetItemForIndex(index);
                label.text = item?.ToString() ?? "null";
                return;
            }

            treeView.bindItem.Invoke(element, index);
        }

        /// <inheritdoc />
        protected override void UnbindItem(VisualElement element, int index)
        {
            treeView.unbindItem?.Invoke(element, index);
        }

        /// <inheritdoc />
        protected override void DestroyItem(VisualElement element)
        {
            treeView.destroyItem?.Invoke(element);
        }
    }

    internal sealed class DefaultTreeViewController<T> : TreeViewController, IDefaultTreeViewController<T>
    {
        TreeDataController<T> m_TreeDataController;
        TreeDataController<T> treeDataController => m_TreeDataController ??= new TreeDataController<T>();

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

        public TreeViewItemData<T> GetTreeViewItemDataForId(int id)
        {
            return treeDataController.GetTreeItemDataForId(id);
        }

        public TreeViewItemData<T> GetTreeViewItemDataForIndex(int index)
        {
            var itemId = GetIdForIndex(index);
            return treeDataController.GetTreeItemDataForId(itemId);
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
