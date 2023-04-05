// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base collection tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="BaseTreeView"/> inheritor.
    /// </summary>
    public abstract class BaseTreeViewController : CollectionViewController
    {
        Dictionary<int, TreeItem> m_TreeItems = new();
        List<int> m_RootIndices = new();
        List<TreeViewItemWrapper> m_ItemWrappers = new();
        HashSet<int> m_TreeItemIdsWithItemWrappers = new();
        List<TreeViewItemWrapper> m_WrapperInsertionList = new();

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
        /// Returns the root items of the tree, by IDs.
        /// </summary>
        /// <returns>The root item IDs.</returns>
        public IEnumerable<int> GetRootItemIds()
        {
            return m_RootIndices;
        }

        /// <summary>
        /// Returns all item IDs that can be found in the tree, optionally specifying root IDs from where to start.
        /// </summary>
        /// <param name="rootIds">Root IDs to start from. If null, will use the tree root ids.</param>
        /// <returns>All items IDs in the tree, starting from the specified IDs.</returns>
        public abstract IEnumerable<int> GetAllItemIds(IEnumerable<int> rootIds = null);
        /// <summary>
        /// Returns the parent ID of an item, by ID.
        /// </summary>
        /// <param name="id">The ID of the item to fetch the parent from.</param>
        /// <returns>The parent ID, or -1 if the item is at the root of the tree.</returns>
        public abstract int GetParentId(int id);
        /// <summary>
        /// Get all children of a specific ID in the tree.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <returns>The children IDs.</returns>
        public abstract IEnumerable<int> GetChildrenIds(int id);
        /// <summary>
        /// Moves an item by ID, to a new parent and child index.
        /// </summary>
        /// <param name="id">The ID of the item to move.</param>
        /// <param name="newParentId">The new parent ID. -1 if moved at the root.</param>
        /// <param name="childIndex">The child index to insert at under the parent. -1 will add as the last child.</param>
        /// <param name="rebuildTree">Whether we need to rebuild tree data. Set to false when doing multiple operations.</param>
        public abstract void Move(int id, int newParentId, int childIndex = -1, bool rebuildTree = true);
        /// <summary>
        /// Removes an item by id.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="rebuildTree">Whether we need to rebuild tree data. Set to <c>false</c> when doing multiple operations and call <see cref="TreeViewController.RebuildTree()"/>.</param>
        /// <returns>Whether the item was successfully found and removed.</returns>
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
                treeItem.Indent(GetIndentationDepthByIndex(index));
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
        /// Returns the index in the source of the item, by ID.
        /// </summary>
        /// <param name="id">The ID of the item to look for.</param>
        /// <returns>The index of the item in the expanded items source. Returns -1 if the item is not visible.</returns>
        public override int GetIndexForId(int id)
        {
            if (m_TreeItemIdsWithItemWrappers.Contains(id))
            {
                for (var index = 0; index < m_ItemWrappers.Count; index++)
                {
                    var wrapper = m_ItemWrappers[index];
                    if (wrapper.id == id)
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the ID for a specified index in the visible items source.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override int GetIdForIndex(int index)
        {
            return IsIndexValid(index) ? m_ItemWrappers[index].id : TreeItem.invalidId;
        }

        /// <summary>
        /// Return whether the item with the specified ID has one or more child.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>Whether the item with the specified ID has one or more child.</returns>
        public virtual bool HasChildren(int id)
        {
            if (m_TreeItems.TryGetValue(id, out var item))
                return item.hasChildren;

            return false;
        }

        /// <summary>
        /// Checks if an ID exists within this tree.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>Whether an item with this id exists in the tree.</returns>
        public bool Exists(int id)
        {
            return m_TreeItems.ContainsKey(id);
        }

        /// <summary>
        /// Return whether the item with the specified index has one or more child.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>Whether the item with the specified ID has one or more child.</returns>
        public bool HasChildrenByIndex(int index)
        {
            return IsIndexValid(index) && m_ItemWrappers[index].hasChildren;
        }

        /// <summary>
        /// Gets the children IDs of the item with the specified index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The children IDs.</returns>
        public IEnumerable<int> GetChildrenIdsByIndex(int index)
        {
            return IsIndexValid(index) ? m_ItemWrappers[index].childrenIds : null;
        }

        /// <summary>
        /// Gets the child index under the parent of the item with the specified ID.
        /// </summary>
        /// <param name="id">The item ID.</param>
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

        /// <summary>
        /// Returns the depth of the element at that ID.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <returns>The depth of the element.</returns>
        public int GetIndentationDepth(int id)
        {
            var depth = 0;
            var parentId = GetParentId(id);
            while (parentId != -1)
            {
                parentId = GetParentId(parentId);
                depth++;
            }

            return depth;
        }

        /// <summary>
        /// Return the depth of the element at that index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The depth of the element.</returns>
        public int GetIndentationDepthByIndex(int index)
        {
            var id = GetIdForIndex(index);
            return GetIndentationDepth(id);
        }

        /// <summary>
        /// Determines whether the item with the specified ID can be expanded or collapsed.
        /// </summary>
        public virtual bool CanChangeExpandedState(int id)
        {
            return true;
        }

        /// <summary>
        /// Return whether the item with the specified ID is expanded in the tree.
        /// </summary>
        /// <param name="id">The item ID</param>
        /// <returns>Whether the item with the specified ID is expanded in the tree.</returns>
        public bool IsExpanded(int id)
        {
            return baseTreeView.expandedItemIds.Contains(id);
        }

        /// <summary>
        /// Return whether the item with the specified index is expanded in the tree.
        /// </summary>
        /// <param name="index">The item index</param>
        /// <returns>Whether the item with the specified index is expanded in the tree. Will return false if the index is not valid.</returns>
        public bool IsExpandedByIndex(int index)
        {
            if (!IsIndexValid(index))
                return false;

            return IsExpanded(m_ItemWrappers[index].id);
        }

        static readonly ProfilerMarker K_ExpandItemByIndex = new ProfilerMarker(ProfilerCategory.Scripts, "BaseTreeViewController.ExpandItemByIndex");
        /// <summary>
        /// Expands the item with the specified index, making his children visible. Allows to expand the whole hierarchy under that item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <param name="expandAllChildren">Whether the whole hierarchy under that item will be expanded.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done.</param>
        public void ExpandItemByIndex(int index, bool expandAllChildren, bool refresh = true)
        {
            using var marker = K_ExpandItemByIndex.Auto();
            if (!HasChildrenByIndex(index))
                return;

            var id = GetIdForIndex(index);
            if (!CanChangeExpandedState(id))
                return;

            if (!baseTreeView.expandedItemIds.Contains(id) || expandAllChildren)
            {
                var childrenIds = GetChildrenIdsByIndex(index);
                var childrenIdsList = new List<int>();
                foreach (var childId in childrenIds)
                {
                    if (!m_TreeItemIdsWithItemWrappers.Contains(childId))
                        childrenIdsList.Add(childId);
                }

                CreateWrappers(childrenIdsList, GetIndentationDepth(id) + 1, ref m_WrapperInsertionList);
                m_ItemWrappers.InsertRange(index + 1, m_WrapperInsertionList);
                if (!baseTreeView.expandedItemIds.Contains(m_ItemWrappers[index].id))
                    baseTreeView.expandedItemIds.Add(m_ItemWrappers[index].id);
                m_WrapperInsertionList.Clear();
            }

            if (expandAllChildren)
            {
                var childrenIds = GetChildrenIds(id);
                foreach (var childId in GetAllItemIds(childrenIds))
                    if (!baseTreeView.expandedItemIds.Contains(childId))
                        ExpandItemByIndex(GetIndexForId(childId), true, false);
            }

            if (refresh)
                baseTreeView.RefreshItems();
        }

        /// <summary>
        /// Expands the item with the specified ID, making its children visible. Allows to expand the whole hierarchy under that item.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <param name="expandAllChildren">Whether the whole hierarchy under that item will be expanded.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done.</param>
        public void ExpandItem(int id, bool expandAllChildren, bool refresh = true)
        {
            if (!HasChildren(id) || !CanChangeExpandedState(id))
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
        /// <param name="collapseAllChildren">Whether the whole hierarchy under that item will be collapsed.</param>
        public void CollapseItemByIndex(int index, bool collapseAllChildren)
        {
            if (!HasChildrenByIndex(index))
                return;

            var id = GetIdForIndex(index);
            if (!CanChangeExpandedState(id))
             return;

            if (collapseAllChildren)
            {
                var childrenIds = GetChildrenIds(id);
                foreach (var childId in GetAllItemIds(childrenIds))
                    baseTreeView.expandedItemIds.Remove(childId);
            }

            baseTreeView.expandedItemIds.Remove(id);

            var recursiveChildCount = 0;
            var currentIndex = index + 1;
            var currentDepth = GetIndentationDepthByIndex(index);
            while (currentIndex < m_ItemWrappers.Count && GetIndentationDepthByIndex(currentIndex) > currentDepth)
            {
                recursiveChildCount++;
                currentIndex++;
            }
            var end = index + 1 + recursiveChildCount;
            for (int i = index + 1; i < end; i++)
            {
                m_TreeItemIdsWithItemWrappers.Remove(m_ItemWrappers[i].id);
            }

            m_ItemWrappers.RemoveRange(index + 1, recursiveChildCount);
            baseTreeView.RefreshItems();
        }

        /// <summary>
        /// Collapses the item with the specified ID, hiding its children. Allows to collapse the whole hierarchy under that item.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <param name="collapseAllChildren">Whether the whole hierarchy under that item will be collapsed.</param>
        public void CollapseItem(int id, bool collapseAllChildren)
        {
            if (!CanChangeExpandedState(id))
                return;

            // Try to find it in the currently visible list.
            for (var i = 0; i < m_ItemWrappers.Count; ++i)
            {
                if (m_ItemWrappers[i].id == id)
                {
                    if (IsExpandedByIndex(i))
                    {
                        CollapseItemByIndex(i, collapseAllChildren);
                        return;
                    }
                    break;
                }
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
            {
                if (!CanChangeExpandedState(itemId))
                    continue;

                if (!baseTreeView.expandedItemIds.Contains(itemId))
                    baseTreeView.expandedItemIds.Add(itemId);
            }

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

            using (ListPool<int>.Get(out var list))
            {
                foreach (var itemId in baseTreeView.expandedItemIds)
                {
                    if (!CanChangeExpandedState(itemId))
                    {
                        list.Add(itemId);
                    }
                }

                baseTreeView.expandedItemIds.Clear();
                baseTreeView.expandedItemIds.AddRange(list);
            }

            RegenerateWrappers();
            baseTreeView.RefreshItems();
        }

        internal void RegenerateWrappers()
        {
            m_ItemWrappers.Clear();
            m_TreeItemIdsWithItemWrappers.Clear();

            var rootItemIds = GetRootItemIds();
            if (rootItemIds == null)
                return;

            CreateWrappers(rootItemIds, 0, ref m_ItemWrappers);
            SetItemsSourceWithoutNotify(m_ItemWrappers);
        }

        static readonly ProfilerMarker k_CreateWrappers = new ProfilerMarker("BaseTreeViewController.CreateWrappers");
        void CreateWrappers(IEnumerable<int> treeViewItemIds, int depth, ref List<TreeViewItemWrapper> wrappers)
        {
            using var marker = k_CreateWrappers.Auto();
            if (treeViewItemIds == null || wrappers == null || m_TreeItemIdsWithItemWrappers == null)
                return;

            foreach (var id in treeViewItemIds)
            {
                if (!m_TreeItems.TryGetValue(id, out var treeItem))
                    continue;

                var wrapper = new TreeViewItemWrapper(treeItem, depth);
                wrappers.Add(wrapper);
                m_TreeItemIdsWithItemWrappers.Add(id); 

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

        internal void RaiseItemParentChanged(int id, int newParentId)
        {
            RaiseItemIndexChanged(id, newParentId);
        }
    }
}
