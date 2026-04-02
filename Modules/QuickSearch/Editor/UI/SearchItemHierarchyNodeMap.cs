// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;

namespace UnityEditor.Search
{
    sealed class SearchItemHierarchyNodeMap
    {
        sealed class StringViewHierarchyMapEqualityComparer : IEqualityComparer<StringView>
        {
            public bool Equals(StringView x, StringView y)
            {
                return x.Equals(y);
            }
            public int GetHashCode(StringView sv)
            {
                return HashingUtils.GetHashCode(sv);
            }
        }

        readonly Dictionary<StringView, HierarchyNode> m_SearchItemToNode = new(new StringViewHierarchyMapEqualityComparer());
        readonly Dictionary<HierarchyNode, SearchItem> m_NodeToSearchItem = new();

        public void Add(SearchItem searchItem, in HierarchyNode node)
        {
            if (searchItem == null)
                throw new ArgumentNullException(nameof(searchItem));
            if (searchItem.id == null)
                throw new ArgumentNullException(nameof(SearchItem.id));
            if (node == HierarchyNode.Null)
                throw new ArgumentNullException(nameof(node));

            // We use the searchItem id StringView as a key because there are cases where we only have the id available,
            // but we still want to know if the corresponding HierarchyNode exists. By using the StringView, we can avoid
            // having to allocate substrings. Furthermore, we use a custom comparer that uses a stable hash code that is identical
            // for strings and StringViews with the same content, so that we can find the node even if we only have a string id instead of a StringView.
            m_SearchItemToNode[searchItem.id.GetStringView()] = node;
            m_NodeToSearchItem[node] = searchItem;
        }

        public bool TryGetSearchItem(in HierarchyNode node, out SearchItem searchItem)
        {
            return m_NodeToSearchItem.TryGetValue(node, out searchItem);
        }

        public bool TryGetNode(SearchItem searchItem, out HierarchyNode node)
        {
            if (searchItem == null)
                throw new ArgumentNullException(nameof(searchItem));
            if (searchItem.id == null)
                throw new ArgumentNullException(nameof(SearchItem.id));
            return m_SearchItemToNode.TryGetValue(searchItem.id.GetStringView(), out node);
        }

        public bool TryGetNode(string searchItemId, out HierarchyNode node)
        {
            if (searchItemId == null)
                throw new ArgumentNullException(nameof(searchItemId));
            return m_SearchItemToNode.TryGetValue(searchItemId.GetStringView(), out node);
        }

        public bool TryGetNode(StringView searchItemId, out HierarchyNode node)
        {
            if (!searchItemId.valid)
                throw new ArgumentNullException(nameof(searchItemId));
            return m_SearchItemToNode.TryGetValue(searchItemId, out node);
        }

        public void Clear()
        {
            m_SearchItemToNode.Clear();
            m_NodeToSearchItem.Clear();
        }
    }
}
