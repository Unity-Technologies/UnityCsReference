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
    /// Base collection tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="BaseTreeView"/> inheritor.
    /// </summary>
    public abstract class BaseTreeViewController : CollectionViewController
    {
        Dictionary<int, TreeItem> m_TreeItems = new Dictionary<int, TreeItem>();
        List<int> m_RootIndices = new List<int>();
        List<TreeViewItemWrapper> m_ItemWrappers = new List<TreeViewItemWrapper>();
        List<TreeViewItemWrapper> m_WrapperInsertionList = new List<TreeViewItemWrapper>();

        /// <summary>
        /// View for this controller, cast as a <see cref="BaseTreeView"/>.
        /// </summary>
        protected BaseTreeView baseTreeView => view as BaseTreeView;

        /// <summary>
        /// Items for this tree. Contains items that are expanded in the tree.
        /// </summary>
        /// <remarks>It can only be accessed. Source is set when tree is rebuilt using <see cref="RebuildTree"/></remarks>
        public override IList itemsSource
        {
            get => base.itemsSource;
            set => throw new InvalidOperationException("Can't set itemsSource directly. Override this controller to manage tree data.");
        }

        /// <summary>
        /// Rebuilds the tree item data and regenerates wrappers to fill the source.
        /// </summary>
        /// <remarks>This needs to be called when adding/removing/moving items.</remarks>
        public void RebuildTree()
        {
            m_TreeItems.Clear();
            m_RootIndices.Clear();

            foreach (var itemId in GetAllItemIds())
            {
                var parentId = GetParentId(itemId);
                if (parentId == TreeItem.invalidId)
                    m_RootIndices.Add(itemId);

                m_TreeItems.Add(itemId, new TreeItem(itemId, parentId, GetChildrenIds(itemId)));
            }

            RegenerateWrappers();
        }

        /// <summary>
        /// Returns the root items of the tree, by ids.
        /// </summary>
        /// <returns>The root item ids.</returns>
        public IEnumerable<int> GetRootItemIds()
        {
            return m_RootIndices;
        }

        /// <summary>
        /// Returns all item ids that can be found in the tree, optionally specifying root ids from where to start.
        /// </summary>
        /// <param name="rootIds">Root ids to start from. If null, will use the tree root ids.</param>
        /// <returns>All items ids in the tree, starting from the specified ids.</returns>
        public abstract IEnumerable<int> GetAllItemIds(IEnumerable<int> rootIds = null);
        /// <summary>
        /// Returns the parent id of an item, by id.
        /// </summary>
        /// <param name="id">The id of the item to fetch the parent from.</param>
        /// <returns>The parent id, or -1 if the item is at the root of the tree.</returns>
        public abstract int GetParentId(int id);
        /// <summary>
        /// Get all children of a specific id in the tree.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>The children ids.</returns>
        public abstract IEnumerable<int> GetChildrenIds(int id);
        /// <summary>
        /// Moves an item by id, to a new parent and child index.
        /// </summary>
        /// <param name="id">The id of the item to move.</param>
        /// <param name="newParentId">The new parent id. -1 if moved at the root.</param>
        /// <param name="childIndex">The child index to insert at under the parent. -1 will add as the last child.</param>
        /// <param name="rebuildTree">Whether we need to rebuild tree data. Set to false when doing multiple operations.</param>
        public abstract void Move(int id, int newParentId, int childIndex = -1, bool rebuildTree = true);
        /// <summary>
        /// Removes an item by id.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="rebuildTree">Whether we need to rebuild tree data. Set to false when doing multiple operations.</param>
        /// <returns>Whether or not the item was successfully found and removed.</returns>
        public abstract bool TryRemoveItem(int id, bool rebuildTree = true);

        internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableTreeViewItem treeItem)
            {
                treeItem.Init(MakeItem());
                PostInitRegistration(treeItem);
            }
        }

        internal override void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
        {
            if (reusableItem is ReusableTreeViewItem treeItem)
            {
                treeItem.Indent(GetIndentationDepth(index));
                treeItem.SetExpandedWithoutNotify(IsExpandedByIndex(index));
                treeItem.SetToggleVisibility(HasChildrenByIndex(index));
            }

            base.InvokeBindItem(reusableItem, index);
        }

        internal override void InvokeDestroyItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableTreeViewItem treeItem)
            {
                treeItem.onPointerUp -= OnItemPointerUp;
                treeItem.onToggleValueChanged -= OnToggleValueChanged;
            }

            base.InvokeDestroyItem(reusableItem);
        }

        internal void PostInitRegistration(ReusableTreeViewItem treeItem)
        {
            treeItem.onPointerUp += OnItemPointerUp;
            treeItem.onToggleValueChanged += OnToggleValueChanged;
            if (baseTreeView.autoExpand)
            {
                baseTreeView.expandedItemIds.Remove(treeItem.id);
                baseTreeView.schedule.Execute(() => ExpandItem(treeItem.id, true));
            }
        }

        private void OnItemPointerUp(PointerUpEvent evt)
        {
            if ((evt.modifiers & EventModifiers.Alt) == 0)
                return;

            var target = evt.currentTarget as VisualElement;
            var toggle = target.Q<Toggle>(BaseTreeView.itemToggleUssClassName);
            var index = ((ReusableTreeViewItem)toggle.userData).index;
            var id = GetIdForIndex(index);
            var wasExpanded = IsExpandedByIndex(index);

            if (!HasChildrenByIndex(index))
                return;

            var hashSet = new HashSet<int>(baseTreeView.expandedItemIds);

            if (wasExpanded)
                hashSet.Remove(id);
            else
                hashSet.Add(id);

            var childrenIds = GetChildrenIdsByIndex(index);
            foreach (var childId in GetAllItemIds(childrenIds))
            {
                if (HasChildren(childId))
                {
                    if (wasExpanded)
                        hashSet.Remove(childId);
                    else
                        hashSet.Add(childId);
                }
            }

            baseTreeView.expandedItemIds = hashSet.ToList();

            RegenerateWrappers();
            baseTreeView.RefreshItems();

            evt.StopPropagation();
        }

        private void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            var toggle = evt.target as Toggle;
            var index = ((ReusableTreeViewItem)toggle.userData).index;
            var isExpanded = IsExpandedByIndex(index);

            if (isExpanded)
                CollapseItemByIndex(index, false);
            else
                ExpandItemByIndex(index, false);

            // To make sure our TreeView gets focus, we need to force this. :(
            baseTreeView.scrollView.contentContainer.Focus();
        }

        /// <summary>
        /// Get the number of items in the whole tree.
        /// </summary>
        /// <returns>The number of items in the tree.</returns>
        /// <remarks>This is different from <see cref="CollectionViewController.GetItemsCount"/>, which will return the number of items in the source.</remarks>
        public virtual int GetTreeItemsCount()
        {
            return m_TreeItems.Count;
        }

        /// <summary>
        /// Returns the index in the source of the item, by id.
        /// </summary>
        /// <param name="id">The id of the item to look for.</param>
        /// <returns>The index of the item in the expanded items source. Returns -1 if the item is not visible.</returns>
        public override int GetIndexForId(int id)
        {
            for (var index = 0; index < m_ItemWrappers.Count; index++)
            {
                var wrapper = m_ItemWrappers[index];
                if (wrapper.id == id)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the id for a specified index in the visible items source.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override int GetIdForIndex(int index)
        {
            return IsIndexValid(index) ? m_ItemWrappers[index].id : TreeItem.invalidId;
        }

        /// <summary>
        /// Returns whether or not the item with the specified id has one or more child.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>Whether or not the item with the specified id has one or more child.</returns>
        public virtual bool HasChildren(int id)
        {
            if (m_TreeItems.TryGetValue(id, out var item))
                return item.hasChildren;

            return false;
        }

        /// <summary>
        /// Returns whether or not the item with the specified index has one or more child.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>Whether or not the item with the specified id has one or more child.</returns>
        public bool HasChildrenByIndex(int index)
        {
            return IsIndexValid(index) && m_ItemWrappers[index].hasChildren;
        }

        /// <summary>
        /// Gets the children ids of the item with the specified index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The children ids.</returns>
        public IEnumerable<int> GetChildrenIdsByIndex(int index)
        {
            return IsIndexValid(index) ? m_ItemWrappers[index].childrenIds : null;
        }

        /// <summary>
        /// Gets the child index under the parent of the item with the specified id.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>The child index under the parent. Returns -1 if the item has no parent or doesn't exist in the tree.</returns>
        public int GetChildIndexForId(int id)
        {
            if (!m_TreeItems.TryGetValue(id, out var item))
                return -1;

            var index = 0;
            var itemIds = m_TreeItems.TryGetValue(item.parentId, out var parentItem) ? parentItem.childrenIds : m_RootIndices;
            foreach (var childId in itemIds)
            {
                if (childId == id)
                    return index;

                index++;
            }

            return -1;
        }

        int GetIndentationDepth(int index)
        {
            return IsIndexValid(index) ? m_ItemWrappers[index].depth : 0;
        }

        /// <summary>
        /// Returns whether or not the item with the specified id is expanded in the tree.
        /// </summary>
        /// <param name="id">The item id</param>
        /// <returns>Whether or not the item with the specified id is expanded in the tree.</returns>
        public bool IsExpanded(int id)
        {
            return baseTreeView.expandedItemIds.Contains(id);
        }

        /// <summary>
        /// Returns whether or not the item with the specified index is expanded in the tree.
        /// </summary>
        /// <param name="index">The item index</param>
        /// <returns>Whether or not the item with the specified id is expanded in the tree. Will return false if the index is not valid.</returns>
        public bool IsExpandedByIndex(int index)
        {
            if (!IsIndexValid(index))
                return false;

            return IsExpanded(m_ItemWrappers[index].id);
        }

        /// <summary>
        /// Expands the item with the specified index, making his children visible. Allows to expand the whole hierarchy under that item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <param name="expandAllChildren">Whether or not to expand the whole hierarchy under that item.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done.</param>
        public void ExpandItemByIndex(int index, bool expandAllChildren, bool refresh = true)
        {
            if (!HasChildrenByIndex(index))
                return;

            if (!baseTreeView.expandedItemIds.Contains(GetIdForIndex(index)) || expandAllChildren)
            {
                var childrenIds = GetChildrenIdsByIndex(index);
                var childrenIdsList = new List<int>();
                foreach (var childId in childrenIds)
                {
                    if (m_ItemWrappers.All(x => x.id != childId))
                        childrenIdsList.Add(childId);
                }

                CreateWrappers(childrenIdsList, GetIndentationDepth(index) + 1,
                    ref m_WrapperInsertionList);
                m_ItemWrappers.InsertRange(index + 1, m_WrapperInsertionList);
                if (!baseTreeView.expandedItemIds.Contains(m_ItemWrappers[index].id))
                    baseTreeView.expandedItemIds.Add(m_ItemWrappers[index].id);
                m_WrapperInsertionList.Clear();
            }

            if (expandAllChildren)
            {
                var id = GetIdForIndex(index);
                var childrenIds = GetChildrenIds(id);
                foreach (var childId in GetAllItemIds(childrenIds))
                    if (!baseTreeView.expandedItemIds.Contains(childId))
                        ExpandItemByIndex(GetIndexForId(childId), true, false);
            }

            if (refresh)
                baseTreeView.RefreshItems();
        }

        /// <summary>
        /// Expands the item with the specified id, making its children visible. Allows to expand the whole hierarchy under that item.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="expandAllChildren">Whether or not to expand the whole hierarchy under that item.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done.</param>
        public void ExpandItem(int id, bool expandAllChildren, bool refresh = true)
        {
            if (!HasChildren(id))
                return;

            // Try to find it in the currently visible list.
            for (var i = 0; i < m_ItemWrappers.Count; ++i)
                if (m_ItemWrappers[i].id == id)
                    if (expandAllChildren || !IsExpandedByIndex(i))
                    {
                        ExpandItemByIndex(i, expandAllChildren, refresh);
                        return;
                    }

            if (baseTreeView.expandedItemIds.Contains(id))
                return;

            baseTreeView.expandedItemIds.Add(id);
        }

        /// <summary>
        /// Collapses the item with the specified index, hiding its children. Allows to collapse the whole hierarchy under that item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <param name="collapseAllChildren">Whether or not to collapse the whole hierarchy under that item.</param>
        public void CollapseItemByIndex(int index, bool collapseAllChildren)
        {
            if (!HasChildrenByIndex(index))
                return;

            if (collapseAllChildren)
            {
                var id = GetIdForIndex(index);
                var childrenIds = GetChildrenIds(id);
                foreach (var childId in GetAllItemIds(childrenIds))
                    baseTreeView.expandedItemIds.Remove(childId);
            }

            baseTreeView.expandedItemIds.Remove(GetIdForIndex(index));

            var recursiveChildCount = 0;
            var currentIndex = index + 1;
            var currentDepth = GetIndentationDepth(index);
            while (currentIndex < m_ItemWrappers.Count && GetIndentationDepth(currentIndex) > currentDepth)
            {
                recursiveChildCount++;
                currentIndex++;
            }

            m_ItemWrappers.RemoveRange(index + 1, recursiveChildCount);

            baseTreeView.RefreshItems();
        }

        /// <summary>
        /// Collapses the item with the specified id, hiding its children. Allows to collapse the whole hierarchy under that item.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="collapseAllChildren">Whether or not to collapse the whole hierarchy under that item.</param>
        public void CollapseItem(int id, bool collapseAllChildren)
        {
            // Try to find it in the currently visible list.
            for (var i = 0; i < m_ItemWrappers.Count; ++i)
                if (m_ItemWrappers[i].id == id)
                    if (IsExpandedByIndex(i))
                    {
                        CollapseItemByIndex(i, collapseAllChildren);
                        return;
                    }

            if (!baseTreeView.expandedItemIds.Contains(id))
                return;

            baseTreeView.expandedItemIds.Remove(id);
        }

        /// <summary>
        /// Expands all items in the tree and refreshes the view.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var itemId in GetAllItemIds())
                if (!baseTreeView.expandedItemIds.Contains(itemId))
                    baseTreeView.expandedItemIds.Add(itemId);

            RegenerateWrappers();
            baseTreeView.RefreshItems();
        }

        /// <summary>
        /// Collapses all items in the tree and refreshes the view.
        /// </summary>
        public void CollapseAll()
        {
            if (baseTreeView.expandedItemIds.Count == 0)
                return;

            baseTreeView.expandedItemIds.Clear();
            RegenerateWrappers();
            baseTreeView.RefreshItems();
        }

        internal void RegenerateWrappers()
        {
            m_ItemWrappers.Clear();

            var rootItemIds = GetRootItemIds();
            if (rootItemIds == null)
                return;

            CreateWrappers(rootItemIds, 0, ref m_ItemWrappers);
            SetItemsSourceWithoutNotify(m_ItemWrappers);
        }

        void CreateWrappers(IEnumerable<int> treeViewItemIds, int depth, ref List<TreeViewItemWrapper> wrappers)
        {
            if (treeViewItemIds == null || wrappers == null)
                return;

            foreach (var id in treeViewItemIds)
            {
                if (!m_TreeItems.TryGetValue(id, out var treeItem))
                    continue;

                var wrapper = new TreeViewItemWrapper(treeItem, depth);
                wrappers.Add(wrapper);

                if (baseTreeView?.expandedItemIds == null)
                    continue;

                if (baseTreeView.expandedItemIds.Contains(wrapper.id) && wrapper.hasChildren)
                    CreateWrappers(GetChildrenIds(wrapper.id), depth + 1, ref wrappers);
            }
        }

        bool IsIndexValid(int index)
        {
            return index >= 0 && index < m_ItemWrappers.Count;
        }
    }
}
