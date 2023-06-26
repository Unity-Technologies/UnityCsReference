// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    /// <summary>
    /// Class giving readonly access to the current list of selected items in QuickSearch.
    /// </summary>
    public class SearchSelection : IReadOnlyCollection<SearchItem>
    {
        int m_ListInitialCount;
        private ISearchList m_List;
        private IList<int> m_Selection;
        private List<SearchItem> m_Items;

        /// <summary>
        /// Create a new SearchSelection
        /// </summary>
        /// <param name="selection">Current list of selected SearchItem indices.</param>
        /// <param name="filteredItems">List of SearchItem displayed in QuickSearch.</param>
        public SearchSelection(IList<int> selection, ISearchList filteredItems)
        {
            m_Selection = selection;
            m_List = filteredItems;
            m_ListInitialCount = m_List.Count;
        }

        public SearchSelection(IEnumerable<SearchItem> items)
        {
            m_Items = new List<SearchItem>(items);
            m_Selection = new List<int>();
            for (int i = 0; i < m_Items.Count; ++i)
                m_Selection.Add(i);
            m_List = null;
        }

        internal bool SyncSelectionIfInvalid()
        {
            if (m_Items == null)
                // Will be sync next time it is accessed
                return false;
            if (m_List == null)
                return false;

            if (m_Selection.Count != m_Items.Count || m_List.Count != m_ListInitialCount)
            {
                BuildSelection();
                return true;
            }
            return false;
        }

        internal IList<int> indexes => m_Selection;
        /// <summary>
        /// How many items are selected.
        /// </summary>
        public int Count => m_Selection.Count;

        /// <summary>
        /// Lowest selected index of any item in the selection.
        /// </summary>
        /// <returns>Returns the lowest selected index.</returns>
        public int MinIndex()
        {
            return m_Selection.Count > 0 ? m_Selection.Min() : -1;
        }

        /// <summary>
        /// Highest selected index of any item in the selection.
        /// </summary>
        /// <returns>Returns the highest selected index.</returns>
        public int MaxIndex()
        {
            return m_Selection.Count > 0 ? m_Selection.Max() : -1;
        }

        /// <summary>
        /// Get the first selected item in the selection.
        /// </summary>
        /// <returns>First select item in selection. Returns null if no item are selected</returns>
        public SearchItem First()
        {
            if (m_Selection.Count == 0)
                return null;
            if (m_Items == null)
                BuildSelection();
            return m_Items[0];
        }

        /// <summary>
        /// Get the last selected item in the selection.
        /// </summary>
        /// <returns>Last select item in selection. Returns null if no item are selected</returns>
        public SearchItem Last()
        {
            if (m_Selection.Count == 0)
                return null;
            if (m_Items == null)
                BuildSelection();
            return m_Items[m_Items.Count - 1];
        }

        /// <summary>
        /// Get an enumerator on the currently selected SearchItems.
        /// </summary>
        /// <returns>Enumerator on the currently selected SearchItems.</returns>
        public IEnumerator<SearchItem> GetEnumerator()
        {
            if (m_Items == null)
                BuildSelection();
            return m_Items.GetEnumerator();
        }

        /// <summary>
        /// Get an enumerator on the currently selected SearchItems.
        /// </summary>
        /// <returns>Enumerator on the SearchItems (selected or not).</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void BuildSelection()
        {
            m_Items = new List<SearchItem>(m_Selection.Count);
            if (m_List == null)
                return;

            if (m_List.Count != m_ListInitialCount)
            {
                m_Selection = new List<int>(m_Selection.Where(index => index >= 0 && index < m_List.Count));
                m_ListInitialCount = m_List.Count;
            }

            foreach (var s in m_Selection)
            {
                m_Items.Add(m_List.ElementAt(s));
            }
        }

        public bool Contains(SearchItem item)
        {
            if (m_Items == null)
                return false;
            foreach (var e in m_Items)
            {
                if (string.Equals(e.id, item.id, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
