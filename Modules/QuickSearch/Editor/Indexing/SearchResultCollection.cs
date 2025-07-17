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

        public SearchResultCollection(IReadOnlyCollection<int> docIndexes)
        {
            m_Set = new HashSet<SearchResult>(docIndexes.Count);
            foreach (var docIndex in docIndexes)
            {
                m_Set.Add(new SearchResult(docIndex));
            }
        }

        public void Add(SearchResult item)
        {
            m_Set.Add(item);
        }

        public void Add(IEnumerable<SearchResult> orSet)
        {
            m_Set.UnionWith(orSet);
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

        public void CopyTo(SearchResult[] array)
        {
            m_Set.CopyTo(array);
        }

        public bool Remove(SearchResult item)
        {
            return m_Set.Remove(item);
        }

        public void ExceptWith(IEnumerable<SearchResult> results)
        {
            m_Set.ExceptWith(results);
        }

        public void IntersectWith(IEnumerable<SearchResult> results)
        {
            m_Set.IntersectWith(results);
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
