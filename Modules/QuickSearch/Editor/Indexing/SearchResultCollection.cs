// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    class SearchResultCollection : ICollection<SearchResult>
    {
        private readonly HashSet<SearchResult> m_Set;

        public int Count => m_Set.Count;
        public bool IsReadOnly => false;

        public SearchResultCollection()
        {
            m_Set = new HashSet<SearchResult>();
        }

        public SearchResultCollection(IEnumerable<SearchResult> inset)
        {
            m_Set = new HashSet<SearchResult>(inset);
        }

        public void Add(SearchResult item)
        {
            m_Set.Add(item);
        }

        public void Clear()
        {
            m_Set.Clear();
        }

        public bool Contains(SearchResult item)
        {
            return m_Set.Contains(item);
        }

        public bool TryGetValue(ref SearchResult item)
        {
            return true;
        }

        public void CopyTo(SearchResult[] array, int arrayIndex)
        {
            m_Set.CopyTo(array, arrayIndex);
        }

        public bool Remove(SearchResult item)
        {
            return m_Set.Remove(item);
        }

        public IEnumerator<SearchResult> GetEnumerator()
        {
            return m_Set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Set.GetEnumerator();
        }
    }
}
