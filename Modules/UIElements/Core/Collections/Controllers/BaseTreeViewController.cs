// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Profiling;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    internal class ReadOnlyHierarchyViewModelList : IList
    {
        readonly HierarchyViewModel m_HierarchyViewModel;

        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public int Count => m_HierarchyViewModel.Count;
        public bool Contains(object value) => value is HierarchyNode node && m_HierarchyViewModel.Contains(node);
        public int IndexOf(object value) => value is HierarchyNode node ? m_HierarchyViewModel.IndexOf(node) : BaseTreeView.invalidId;

        public ReadOnlyHierarchyViewModelList(HierarchyViewModel viewModel)
        {
            m_HierarchyViewModel = viewModel;
        }

        public object this[int index]
        {
            get => m_HierarchyViewModel[index];
            set => throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            for (var i = index; i < m_HierarchyViewModel.Count; ++i)
                array.SetValue(m_HierarchyViewModel[i], i - index);
        }

        public IEnumerator GetEnumerator() => new Enumerator(m_HierarchyViewModel);
        public bool IsSynchronized => throw new NotSupportedException();
        public object SyncRoot => throw new NotSupportedException();
        public int Add(object value) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, object value) => throw new NotSupportedException();
        public void Remove(object value) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();

        struct Enumerator : IEnumerator
        {
            readonly HierarchyViewModel m_HierarchyViewModel;
            HierarchyViewModel.Enumerator m_Enumerator;

            public Enumerator(HierarchyViewModel hierarchyViewModel)
            {
                m_HierarchyViewModel = hierarchyViewModel;
                m_Enumerator = hierarchyViewModel.GetEnumerator();
            }

            public object Current => m_Enumerator.Current;
            public bool MoveNext() => m_Enumerator.MoveNext();
            public void Reset() => m_Enumerator = m_HierarchyViewModel.GetEnumerator();
        }
    }

    /// <summary>
    /// Base collection tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="BaseTreeView"/> inheritor.
    /// </summary>
    public abstract class BaseTreeViewController : CollectionViewController
    {
        private protected Hierarchy m_Hierarchy;
        private protected HierarchyFlattened m_HierarchyFlattened;
        private protected HierarchyViewModel m_HierarchyViewModel;
        private protected Dictionary<int, HierarchyNode> m_IdToNodeDictionary = new();

        private const string k_HierarchyPropertyName = "TreeViewDataProperty";
        private IHierarchyProperty<int> m_TreeViewDataProperty;

        // This Flag helps reduce the amount of C# bindings calls when RefreshItems() and Rebuild() are called.
        private bool m_HierarchyHasPendingChanged;

        /// <summary>
        /// View for this controller, cast as a <see cref="BaseTreeView"/>.
        /// </summary>
        protected BaseTreeView baseTreeView => view as BaseTreeView;

        /// <summary>
        /// Constructor for a BaseTreeViewController
        /// </summary>
        protected BaseTreeViewController()
        {
            hierarchy = new Hierarchy();
        }

        /// <summary>
        /// Destructor for a BaseTreeViewController
        /// </summary>
        ~BaseTreeViewController()
        {
            DisposeHierarchy();
        }

        private protected Hierarchy hierarchy
        {
            get => m_Hierarchy;
            set
            {
                if (hierarchy == value)
                    return;

                DisposeHierarchy();

                if (value == null)
                    return;

                m_Hierarchy = value;
                m_HierarchyFlattened = new HierarchyFlattened(m_Hierarchy);
                m_HierarchyViewModel = new HierarchyViewModel(m_HierarchyFlattened);
                m_TreeViewDataProperty = m_Hierarchy.GetOrCreatePropertyUnmanaged<int>(k_HierarchyPropertyName);
            }
        }

        internal void DisposeHierarchy()
        {
            if (m_HierarchyViewModel != null)
            {
                if (m_HierarchyViewModel.IsCreated)
                    m_HierarchyViewModel.Dispose();
                m_HierarchyViewModel = null;
            }

            if (m_HierarchyFlattened != null)
            {
                if (m_HierarchyFlattened.IsCreated)
                    m_HierarchyFlattened.Dispose();
                m_HierarchyFlattened = null;
            }

            if (m_Hierarchy != null)
            {
                if (m_Hierarchy.IsCreated)
                    m_Hierarchy.Dispose();
                m_Hierarchy = null;
            }
        }

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
        [Obsolete("RebuildTree is no longer supported and will be removed.", false)]
        public void RebuildTree() {}

        /// <summary>
        /// Returns the root items of the tree, by IDs.
        /// </summary>
        /// <returns>The root item IDs.</returns>
        public IEnumerable<int> GetRootItemIds()
        {
            var nodes = m_Hierarchy.EnumerateChildren(m_Hierarchy.Root);

            foreach (var node in nodes)
            {
                yield return m_TreeViewDataProperty.GetValue(node);
            }
        }

        /// <summary>
        /// Returns all item IDs that can be found in the tree, optionally specifying root IDs from where to start.
        /// </summary>
        /// <param name="rootIds">Root IDs to start from. If null, will use the tree root ids.</param>
        /// <returns>All items IDs in the tree, starting from the specified IDs.</returns>
        public virtual IEnumerable<int> GetAllItemIds(IEnumerable<int> rootIds = null)
        {
            if (rootIds == null)
            {
                foreach (var flattenedNode in m_HierarchyFlattened)
                {
                    if (flattenedNode.Node == m_Hierarchy.Root)
                        continue;

                    yield return m_TreeViewDataProperty.GetValue(flattenedNode.Node);
                }

                yield break;
            }

            foreach (var id in rootIds)
            {
                var flattenedNodeChildren = m_HierarchyFlattened.EnumerateChildren(m_IdToNodeDictionary[id]);

                foreach (var node in flattenedNodeChildren)
                    yield return m_TreeViewDataProperty.GetValue(node);

                yield return id;
            }
        }

        /// <summary>
        /// Returns the parent ID of an item, by ID.
        /// </summary>
        /// <param name="id">The ID of the item to fetch the parent from.</param>
        /// <returns>The parent ID, or -1 if the item is at the root of the tree.</returns>
        public virtual int GetParentId(int id)
        {
            var node = GetHierarchyNodeById(id);
            if (node == HierarchyNode.Null || !m_Hierarchy.Exists(node))
                return BaseTreeView.invalidId;

            var parentNode = m_Hierarchy.GetParent(node);
            if (parentNode == m_Hierarchy.Root)
                return BaseTreeView.invalidId;

            return m_TreeViewDataProperty.GetValue(parentNode);
        }

        /// <summary>
        /// Get all children of a specific ID in the tree.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <returns>The children IDs.</returns>
        public virtual IEnumerable<int> GetChildrenIds(int id)
        {
            var nodeById = GetHierarchyNodeById(id);
            if (nodeById == HierarchyNode.Null || !m_Hierarchy.Exists(nodeById))
                yield break;

            var nodes = m_Hierarchy.EnumerateChildren(nodeById);
            foreach (var node in nodes)
            {
                yield return m_TreeViewDataProperty.GetValue(node);
            }
        }

        /// <summary>
        /// Moves an item by ID, to a new parent and child index.
        /// </summary>
        /// <param name="id">The ID of the item to move.</param>
        /// <param name="newParentId">The new parent ID. -1 if moved at the root.</param>
        /// <param name="childIndex">The child index to insert at under the parent. -1 will add as the last child.</param>
        /// <param name="rebuildTree">Whether we need to rebuild tree data. Set to false when doing multiple operations.</param>
        public virtual void Move(int id, int newParentId, int childIndex = -1, bool rebuildTree = true)
        {
            if (id == newParentId)
                return;

            if (IsChildOf(newParentId, id))
                return;

            if (!m_IdToNodeDictionary.TryGetValue(id, out var node))
                return;

            var newParent = newParentId == BaseTreeView.invalidId ? m_Hierarchy.Root : GetHierarchyNodeById(newParentId);
            var currentParent = m_Hierarchy.GetParent(node);

            if (currentParent == newParent)
            {
                var index = GetChildIndexForId(id);

                if (index < childIndex)
                    childIndex--;
            }
            else
            {
                m_Hierarchy.SetParent(node, newParent);
            }

            UpdateSortOrder(newParent, node, childIndex);

            if (rebuildTree)
                RaiseItemParentChanged(id, newParentId);
        }

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
        }

        private void OnItemPointerUp(PointerUpEvent evt)
        {
            if ((evt.modifiers & EventModifiers.Alt) == 0)
                return;

            var target = evt.currentTarget as VisualElement;
            var toggle = target.Q<Toggle>(BaseTreeView.itemToggleUssClassName);
            var index = ((ReusableTreeViewItem)toggle.userData).index;

            if (this is MultiColumnTreeViewController multiColumnTreeViewController)
            {
                index = multiColumnTreeViewController.columnController.GetSortedIndex(index);
            }

            if (!HasChildrenByIndex(index))
                return;

            var wasExpanded = IsExpandedByIndex(index);

            if (IsViewDataKeyEnabled())
            {
                var id = GetIdForIndex(index);
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

                baseTreeView.expandedItemIds = new List<int>(hashSet);
            }

            if (wasExpanded)
                m_HierarchyViewModel.ClearFlags(GetHierarchyNodeByIndex(index), HierarchyNodeFlags.Expanded, true);
            else
                m_HierarchyViewModel.SetFlags(GetHierarchyNodeByIndex(index), HierarchyNodeFlags.Expanded, true);

            UpdateHierarchy();
            baseTreeView.RefreshItems();

            evt.StopPropagation();
        }

        private void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            var toggle = evt.target as Toggle;
            var index = ((ReusableTreeViewItem)toggle.userData).index;

            if (this is MultiColumnTreeViewController multiColumnTreeViewController)
            {
                index = multiColumnTreeViewController.columnController.GetSortedIndex(index);
            }

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
            return m_Hierarchy.Count;
        }

        /// <summary>
        /// Returns the index in the source of the item, by ID.
        /// </summary>
        /// <param name="id">The ID of the item to look for.</param>
        /// <returns>The index of the item in the expanded items source. Returns -1 if the item is not visible.</returns>
        public override int GetIndexForId(int id)
        {
            return m_IdToNodeDictionary.TryGetValue(id, out var node) ? m_HierarchyViewModel.IndexOf(node) : BaseTreeView.invalidId;
        }

        /// <summary>
        /// Returns the ID for a specified index in the visible items source.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override int GetIdForIndex(int index)
        {
            var availableNodeCount = m_HierarchyViewModel.Count;
            if (index == availableNodeCount && availableNodeCount > 0)
                return m_TreeViewDataProperty.GetValue(m_HierarchyViewModel[^1]);

            return !IsIndexValid(index) ? BaseTreeView.invalidId : m_TreeViewDataProperty.GetValue(m_HierarchyViewModel[index]);
        }

        /// <summary>
        /// Return whether the item with the specified ID has one or more child.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <returns>Whether the item with the specified ID has one or more child.</returns>
        public virtual bool HasChildren(int id)
        {
            if (m_IdToNodeDictionary.TryGetValue(id, out var node))
                return m_Hierarchy.GetChildrenCount(node) > 0;

            return false;
        }

        /// <summary>
        /// Checks if an ID exists within this tree.
        /// </summary>
        /// <param name="id">The id to look for.</param>
        /// <returns>Whether an item with this id exists in the tree.</returns>
        public bool Exists(int id)
        {
            return m_IdToNodeDictionary.ContainsKey(id);
        }

        /// <summary>
        /// Return whether the item with the specified index has one or more child.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>Whether the item with the specified ID has one or more child.</returns>
        public bool HasChildrenByIndex(int index)
        {
            if (!IsIndexValid(index))
                return false;

            return m_HierarchyViewModel.GetChildrenCount(m_HierarchyViewModel[index]) > 0;
        }

        /// <summary>
        /// Gets the children IDs of the item with the specified index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The children IDs.</returns>
        public IEnumerable<int> GetChildrenIdsByIndex(int index)
        {
            if (!IsIndexValid(index))
                yield break;

            var nodes = m_Hierarchy.EnumerateChildren(m_HierarchyViewModel[index]);

            foreach (var node in nodes)
            {
                yield return m_TreeViewDataProperty.GetValue(node);
            }
        }

        /// <summary>
        /// Gets the child index under the parent of the item with the specified ID.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <returns>The child index under the parent. Returns -1 if the item has no parent or doesn't exist in the tree.</returns>
        public int GetChildIndexForId(int id)
        {
            if (m_IdToNodeDictionary.TryGetValue(id, out var node))
            {
                var parent = m_Hierarchy.GetParent(node);

                if (parent == HierarchyNode.Null)
                    return BaseTreeView.invalidId;

                var nodes = m_Hierarchy.EnumerateChildren(parent);
                var index = 0;

                foreach (var n in nodes)
                {
                    if (n == node)
                        break;

                    index++;
                }

                return index;
            }

            return BaseTreeView.invalidId;
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
            while (parentId != BaseTreeView.invalidId)
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
            if (IsViewDataKeyEnabled())
                return baseTreeView.expandedItemIds.Contains(id);

            return m_IdToNodeDictionary.ContainsKey(id) && m_Hierarchy.Exists(m_IdToNodeDictionary[id]) && m_HierarchyViewModel.HasAllFlags(m_IdToNodeDictionary[id], HierarchyNodeFlags.Expanded);
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

            return IsExpanded(GetIdForIndex(index));
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

            ExpandItemByNode(GetHierarchyNodeById(GetIdForIndex(index)), expandAllChildren, refresh);
        }

        /// <summary>
        /// Expands the item with the specified ID, making its children visible. Allows to expand the whole hierarchy under that item.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <param name="expandAllChildren">Whether the whole hierarchy under that item will be expanded.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done. This is true by default.</param>
        public void ExpandItem(int id, bool expandAllChildren, bool refresh = true)
        {
            if (!HasChildren(id) || !CanChangeExpandedState(id))
                return;

            if (m_IdToNodeDictionary.TryGetValue(id, out var node))
                ExpandItemByNode(node, expandAllChildren, refresh);
        }

        /// <summary>
        /// Collapses the item with the specified index, hiding its children. Allows to collapse the whole hierarchy under that item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <param name="collapseAllChildren">Whether the whole hierarchy under that item will be collapsed.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done. This is true by default.</param>
        public void CollapseItemByIndex(int index, bool collapseAllChildren, bool refresh = true)
        {
            if (!HasChildrenByIndex(index))
                return;

            CollapseItemByNode(GetHierarchyNodeById(GetIdForIndex(index)), collapseAllChildren, refresh);
        }

        /// <summary>
        /// Collapses the item with the specified ID, hiding its children. Allows to collapse the whole hierarchy under that item.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <param name="collapseAllChildren">Whether the whole hierarchy under that item will be collapsed.</param>
        /// <param name="refresh">Whether to refresh items or not. Set to false when doing multiple operations on the tree, to only do one RefreshItems once all operations are done.</param>
        public void CollapseItem(int id, bool collapseAllChildren, bool refresh = true)
        {
            if (!HasChildren(id) || !CanChangeExpandedState(id))
                return;

            if (m_IdToNodeDictionary.TryGetValue(id, out var node))
                CollapseItemByNode(node, collapseAllChildren, refresh);
        }

        /// <summary>
        /// Expands all items in the tree and refreshes the view.
        /// </summary>
        public void ExpandAll()
        {
            m_HierarchyViewModel.SetFlags(HierarchyNodeFlags.Expanded);
            UpdateHierarchy();

            if (IsViewDataKeyEnabled())
            {
                baseTreeView.expandedItemIds.Clear();

                foreach (var node in m_HierarchyViewModel.EnumerateNodesWithAllFlags(HierarchyNodeFlags.Expanded))
                    baseTreeView.expandedItemIds.Add(m_TreeViewDataProperty.GetValue(node));

                baseTreeView.SaveViewData();
            }

            baseTreeView.RefreshItems();
        }

        /// <summary>
        /// Collapses all items in the tree and refreshes the view.
        /// </summary>
        public void CollapseAll()
        {
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Expanded);
            UpdateHierarchy();

            if (IsViewDataKeyEnabled())
            {
                baseTreeView.expandedItemIds.Clear();
                baseTreeView.SaveViewData();
            }

            baseTreeView.RefreshItems();
        }

        // Once we update the TreeView to be 100% Hierarchy, we can replace ExpandItemByIndex with this method. Or, we
        // should at least provide some proper lookup for the node's index.
        void ExpandItemByNode(in HierarchyNode node, bool expandAllChildren, bool refresh)
        {
            var id = m_TreeViewDataProperty.GetValue(node);
            if (!CanChangeExpandedState(id))
                return;

            // Using a HashSet in order to prevent duplicates and it is faster than List.Contains(id)
            m_HierarchyViewModel.SetFlags(node, HierarchyNodeFlags.Expanded, expandAllChildren);
            m_HierarchyHasPendingChanged = true;

            if (IsViewDataKeyEnabled())
            {
                var hashSet = new HashSet<int>(baseTreeView.expandedItemIds) { id };
                // Required to update the expandedItemIds, can get rid of once we find a way to handle the serialized
                // field for the viewDataKey
                if (expandAllChildren)
                {
                    // We need to refresh the view model in order the updated nodes
                    UpdateHierarchy();

                    var childrenIds = GetChildrenIds(id);

                    foreach (var childId in GetAllItemIds(childrenIds))
                        hashSet.Add(childId);
                }

                baseTreeView.expandedItemIds.Clear();
                baseTreeView.expandedItemIds.AddRange(hashSet);
                baseTreeView.SaveViewData();
            }

            if (refresh)
                baseTreeView.RefreshItems();
        }

        // Once we update the TreeView to be 100% Hierarchy, we can replace CollapseItemByIndex with this method. Or, we
        // should at least provide some proper lookup for the node's index.
        void CollapseItemByNode(in HierarchyNode node, bool collapseAllChildren, bool refresh)
        {
            var id = m_TreeViewDataProperty.GetValue(node);
            if (!CanChangeExpandedState(id))
                return;

            if (IsViewDataKeyEnabled())
            {
                if (collapseAllChildren)
                {
                    var childrenIds = GetChildrenIds(id);

                    foreach (var childId in GetAllItemIds(childrenIds))
                        baseTreeView.expandedItemIds.Remove(childId);
                }

                baseTreeView.expandedItemIds.Remove(id);
                baseTreeView.SaveViewData();
            }

            m_HierarchyViewModel.ClearFlags(GetHierarchyNodeById(id), HierarchyNodeFlags.Expanded, collapseAllChildren);
            m_HierarchyHasPendingChanged = true;

            if (refresh)
                baseTreeView.RefreshItems();
        }

        // Helps to determine which expandedItemsIds set to use (the serialized or the view model one).
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void GetExpandedItemIds(List<int> list)
        {
            // This is just in case the function receives a list that contains old data. If so, we will clear the list
            // since we want to get the expanded item ids from a clean slate.
            if (list.Count > 0)
                list.Clear();

            if (IsViewDataKeyEnabled())
                list.AddRange(baseTreeView.expandedItemIds);

            // Otherwise we will populate the list with the expanded IDs from the view model
            foreach (var node in m_HierarchyViewModel.EnumerateNodesWithAllFlags(HierarchyNodeFlags.Expanded))
                list.Add(m_TreeViewDataProperty.GetValue(node));
        }

        // Used to check if it's a view data key path
        internal bool IsViewDataKeyEnabled()
        {
            return baseTreeView.enableViewDataPersistence && !string.IsNullOrEmpty(baseTreeView.viewDataKey);
        }

        // For the use case of expanding all parents when interacting with a collapsed child.
        internal void ExpandAncestorNodes(in HierarchyNode node)
        {
            var parentNode = m_Hierarchy.GetParent(node);
            while (parentNode != m_Hierarchy.Root && (parentNode = GetHierarchyNodeById(m_TreeViewDataProperty.GetValue(parentNode))) != m_Hierarchy.Root)
            {
                var parentItemId = m_TreeViewDataProperty.GetValue(parentNode);
                if (!m_HierarchyViewModel.HasAllFlags(parentNode, HierarchyNodeFlags.Expanded) && CanChangeExpandedState(parentItemId))
                {
                    if (IsViewDataKeyEnabled())
                        baseTreeView.expandedItemIds.Add(parentItemId);

                    m_HierarchyViewModel.SetFlags(parentNode, HierarchyNodeFlags.Expanded);
                    m_HierarchyViewModel.Update();
                }

                parentNode = m_Hierarchy.GetParent(parentNode);
            }
        }

        // A way for the view controller to request an update on the Hierarchy, HierarchyFlattened, and HierarchyViewModel.
        internal override void PreRefresh()
        {
            if (!m_HierarchyHasPendingChanged)
                return;

            UpdateHierarchy();
        }

        // Returns the valid visible node
        bool IsIndexValid(int index)
        {
            return index >= 0 && index < m_HierarchyViewModel.Count;
        }

        bool IsChildOf(int childId, int id)
        {
            if (childId == BaseTreeView.invalidId || id == BaseTreeView.invalidId)
                return false;

            HierarchyNode parentNode;

            var childNode = GetHierarchyNodeById(childId);
            var ancestorNode = GetHierarchyNodeById(id);

            if (ancestorNode == childNode)
                return true;

            while ((parentNode = m_Hierarchy.GetParent(childNode)) != m_Hierarchy.Root)
            {
                if (ancestorNode == parentNode)
                    return true;

                childNode = parentNode;
            }

            return false;
        }

        internal void RaiseItemParentChanged(int id, int newParentId)
        {
            RaiseItemIndexChanged(id, newParentId);
        }

        internal HierarchyNode CreateNode(in HierarchyNode parent)
        {
            return m_Hierarchy.Add(parent == HierarchyNode.Null ? m_Hierarchy.Root : parent);
        }

        internal void UpdateIdToNodeDictionary(int id, in HierarchyNode node, bool isAdd = true)
        {
            if (isAdd)
            {
                m_TreeViewDataProperty.SetValue(node, id);
                m_IdToNodeDictionary[id] = node;
                return;
            }

            m_IdToNodeDictionary.Remove(id);
        }

        // This can be removed once we drop support for the Dictionary in TreeDataController and this file. The goal of this
        // function is to find all children associated to the node being passed and perform the callback that will remove
        // from its collection.
        internal void RemoveAllChildrenItemsFromCollections(in HierarchyNode node, Action<HierarchyNode, int> removeCallback)
        {
            if (node == HierarchyNode.Null)
                return;

            var nodeIndex = m_HierarchyFlattened.IndexOf(in node);
            if (nodeIndex == -1)
                return;

            // We want to skip the current node's index since we only care about the children nodes.
            var nextNodeIndex = nodeIndex + 1;
            var count = m_HierarchyFlattened.GetChildrenCountRecursive(in node);
            for (var i = nextNodeIndex; i < nextNodeIndex + count; ++i)
            {
                var item = m_HierarchyFlattened[i];
                removeCallback(item.Node, m_TreeViewDataProperty.GetValue(item.Node));
            }
        }

        internal void ClearIdToNodeDictionary()
        {
            m_IdToNodeDictionary.Clear();
        }

        internal void UpdateSortOrder(in HierarchyNode newParent, in HierarchyNode insertedNode, int insertedIndex)
        {
            Span<HierarchyNode> existingChildren = m_Hierarchy.GetChildren(newParent);

            if (insertedIndex == -1)
                insertedIndex = existingChildren.Length;

            // If dragging from inside the view, it is possible that the dragged nodes are already children of the parent node.
            // In that case, we need to skip them.
            var currentSortIndex = 0;
            for (var i = 0; i < insertedIndex && i < existingChildren.Length; ++i)
            {
                if (insertedNode == existingChildren[i])
                    continue;

                m_Hierarchy.SetSortIndex(existingChildren[i], currentSortIndex++);
            }

            m_Hierarchy.SetSortIndex(insertedNode, insertedIndex);
            if (insertedIndex == currentSortIndex)
                currentSortIndex++;

            for (var i = insertedIndex; i < existingChildren.Length; ++i)
            {
                if (insertedNode == existingChildren[i])
                    continue;

                m_Hierarchy.SetSortIndex(existingChildren[i], currentSortIndex++);
            }

            m_Hierarchy.SortChildren(newParent);
            UpdateHierarchy();

            // Clear the node's sort indices otherwise the next time they will hold the wrong sort index value.
            Span<HierarchyNode> newChildren = m_Hierarchy.GetChildren(newParent);
            foreach (var node in newChildren)
            {
                m_Hierarchy.SetSortIndex(node, 0);
            }
        }

        // Update the node's flags based on the serialized expandedItemsIds. This will not be called if it's not coming
        // from the view data key path.
        internal void OnViewDataReadyUpdateNodes()
        {
            foreach (var id in baseTreeView.expandedItemIds)
            {
                if (!m_IdToNodeDictionary.TryGetValue(id, out var node)) continue;

                m_HierarchyViewModel.SetFlags(node, HierarchyNodeFlags.Expanded);
            }

            UpdateHierarchy();
        }

        internal void UpdateHierarchy()
        {
            if (m_Hierarchy.UpdateNeeded)
            {
                m_Hierarchy.Update();
            }
            if (m_HierarchyFlattened.UpdateNeeded)
            {
                m_HierarchyFlattened.Update();
            }
            if (m_HierarchyViewModel.UpdateNeeded)
            {
                m_HierarchyViewModel.Update();
            }

            // Clear the flag otherwise when the TreeView refreshes or rebuilds it will unnecessary call UpdateNeeded.
            m_HierarchyHasPendingChanged = false;
        }

        internal HierarchyNode GetHierarchyNodeById(int id)
        {
            return m_IdToNodeDictionary.TryGetValue(id, out var node) ? node : HierarchyNode.Null;
        }

        internal HierarchyNode GetHierarchyNodeByIndex(int index)
        {
            if (!IsIndexValid(index))
                return HierarchyNode.Null;

            return m_HierarchyViewModel[index];
        }
    }
}
