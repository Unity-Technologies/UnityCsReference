// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    internal readonly struct TreeData<T>
    {
        readonly IList<int> m_RootItemIds;
        readonly Dictionary<int, TreeViewItemData<T>> m_Tree;
        readonly Dictionary<int, int> m_ParentIds;
        readonly Dictionary<int, List<int>> m_ChildrenIds;

        public IEnumerable<int> rootItemIds => m_RootItemIds;

        public TreeData(IList<TreeViewItemData<T>> rootItems)
        {
            m_RootItemIds = new List<int>();
            m_Tree = new Dictionary<int, TreeViewItemData<T>>();
            m_ParentIds = new Dictionary<int, int>();
            m_ChildrenIds = new Dictionary<int, List<int>>();

            RefreshTree(rootItems);
        }

        public TreeViewItemData<T> GetDataForId(int id)
        {
            if (m_Tree.TryGetValue(id, out var item))
                return item;

            return default;
        }

        public int GetParentId(int id)
        {
            if (m_ParentIds.TryGetValue(id, out var parentId))
                return parentId;

            return TreeItem.invalidId;
        }

        public void AddItem(TreeViewItemData<T> item, int parentId, int childIndex)
        {
            var enumerator = ListPool<TreeViewItemData<T>>.Get();
            enumerator.Add(item);
            BuildTree(enumerator, false);
            AddItemToParent(item, parentId, childIndex);
            ListPool<TreeViewItemData<T>>.Release(enumerator);
        }

        public bool TryRemove(int id)
        {
            if (m_ParentIds.TryGetValue(id, out var parentId))
            {
                RemoveFromParent(id, parentId);
            }
            else
            {
                m_RootItemIds.Remove(id);
            }

            return TryRemoveChildrenIds(id);
        }

        public void Move(int id, int newParentId, int childIndex)
        {
            if (!m_Tree.TryGetValue(id, out var child))
                return;

            if (m_ParentIds.TryGetValue(id, out var currentParentId))
            {
                if (currentParentId == newParentId)
                {
                    var index = m_Tree[currentParentId].GetChildIndex(id);
                    if (index < childIndex)
                        childIndex--;
                }

                RemoveFromParent(child.id, currentParentId);
            }
            else
            {
                var rootItemIdsPositionForId = m_RootItemIds.IndexOf(id);
                if (rootItemIdsPositionForId < childIndex)
                    childIndex--;

                m_RootItemIds.Remove(id);
            }

            AddItemToParent(child, newParentId, childIndex);
        }

        public bool HasAncestor(int childId, int ancestorId)
        {
            if (childId == TreeItem.invalidId || ancestorId == TreeItem.invalidId)
                return false;

            int parentId;
            var currentId = childId;

            while ((parentId = GetParentId(currentId)) != TreeItem.invalidId)
            {
                if (ancestorId == parentId)
                {
                    return true;
                }

                currentId = parentId;
            }

            return false;
        }

        void AddItemToParent(TreeViewItemData<T> item, int parentId, int childIndex)
        {
            if (parentId == TreeItem.invalidId)
            {
                m_ParentIds.Remove(item.id);
                if (childIndex < 0 || childIndex >= m_RootItemIds.Count)
                    m_RootItemIds.Add(item.id);
                else
                    m_RootItemIds.Insert(childIndex, item.id);

                return;
            }

            var parent = m_Tree[parentId];
            parent.InsertChild(item, childIndex);
            m_Tree[parentId] = parent;
            m_ParentIds[item.id] = parentId;

            // We need to replace struct in parents since they're not value reference types.
            UpdateParentTree(parent);
        }

        void RemoveFromParent(int id, int parentId)
        {
            var currentParent = m_Tree[parentId];
            currentParent.RemoveChild(id);
            m_Tree[parentId] = currentParent;

            if (m_ChildrenIds.TryGetValue(parentId, out var childrenList))
                childrenList.Remove(id);

            // We need to replace struct in parents since they're not value reference types.
            UpdateParentTree(currentParent);
        }

        void UpdateParentTree(TreeViewItemData<T> current)
        {
            while (m_ParentIds.TryGetValue(current.id, out var nextParentId))
            {
                var nextParent = m_Tree[nextParentId];
                nextParent.ReplaceChild(current);
                m_Tree[nextParentId] = nextParent;

                current = nextParent;
            }
        }

        bool TryRemoveChildrenIds(int id)
        {
            if (m_Tree.TryGetValue(id, out var item) && item.children != null)
            {
                foreach (var child in item.children)
                {
                    TryRemoveChildrenIds(child.id);
                }
            }

            if (m_ChildrenIds.TryGetValue(id, out var childrenIds))
            {
                ListPool<int>.Release(childrenIds);
            }

            var removed = false;
            removed |= m_RootItemIds.Remove(id);
            removed |= m_ChildrenIds.Remove(id);
            removed |= m_ParentIds.Remove(id);
            removed |= m_Tree.Remove(id);
            removed |= m_RootItemIds.Remove(id);

            return removed;
        }

        void RefreshTree(IList<TreeViewItemData<T>> rootItems)
        {
            m_Tree.Clear();
            m_ParentIds.Clear();
            m_ChildrenIds.Clear();
            m_RootItemIds.Clear();

            BuildTree(rootItems, true);
        }

        void BuildTree(IEnumerable<TreeViewItemData<T>> items, bool isRoot)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                m_Tree.Add(item.id, item);

                if (isRoot)
                    m_RootItemIds.Add(item.id);

                if (item.children != null)
                {
                    if (!m_ChildrenIds.TryGetValue(item.id, out var childIndexList))
                    {
                        m_ChildrenIds.Add(item.id, childIndexList = ListPool<int>.Get());
                    }

                    foreach (var child in item.children)
                    {
                        m_ParentIds.Add(child.id, item.id);
                        childIndexList.Add(child.id);
                    }

                    BuildTree(item.children, false);
                }
            }
        }
    }

    internal readonly struct TreeItem
    {
        public const int invalidId = -1;

        public int id { get; }
        public int parentId { get; }
        public IEnumerable<int> childrenIds { get; }

        public bool hasChildren => childrenIds != null && childrenIds.Any();

        public TreeItem(int id, int parentId = invalidId, IEnumerable<int> childrenIds = null)
        {
            this.id = id;
            this.parentId = parentId;
            this.childrenIds = childrenIds;
        }
    }

    internal readonly struct TreeViewItemWrapper
    {
        public readonly TreeItem item;
        public int id => item.id;
        public int parentId => item.parentId;
        public IEnumerable<int> childrenIds => item.childrenIds;
        public bool hasChildren => item.hasChildren;
        public readonly int depth;

        public TreeViewItemWrapper(TreeItem item, int depth)
        {
            this.item = item;
            this.depth = depth;
        }
    }
}
