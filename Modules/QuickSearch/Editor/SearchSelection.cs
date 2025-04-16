// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace UnityEditor.Search
{
    /// <summary>
    /// Class giving readonly access to the current list of selected items in QuickSearch.
    /// </summary>
    public class SearchSelection : IReadOnlyCollection<SearchItem>
    {
        int m_ListInitialCount;
        int m_InitialSelectionCount;
        ISearchList m_List;
        IList<int> m_Selection;
        List<int> m_ActualSelection;
        List<SearchItem> m_Items;

        /// <summary>
        /// Create a new SearchSelection
        /// </summary>
        /// <param name="selection">Current list of selected SearchItem indices.</param>
        /// <param name="filteredItems">List of SearchItem displayed in QuickSearch.</param>
        public SearchSelection(IList<int> selection, ISearchList filteredItems)
        {
            m_Selection = selection;
            m_ActualSelection = new List<int>(selection);
            m_List = filteredItems;
            m_ListInitialCount = m_List.Count;
            m_InitialSelectionCount = selection.Count;
        }

        public SearchSelection(IEnumerable<SearchItem> items)
        {
            m_Items = new List<SearchItem>(items);
            m_ActualSelection = new List<int>(m_Items.Count);
            for (int i = 0; i < m_Items.Count; ++i)
                m_ActualSelection.Add(i);
            m_Selection = m_ActualSelection;
            m_List = null;
        }

        internal bool SyncSelectionIfInvalid()
        {
            if (m_Items == null)
                // Will be sync next time it is accessed
                return false;
            if (m_List == null)
                return false;

            // TODO: We need a better way to find that these collections have really changed, i.e. this will not work
            // if selection changed from {1, 2, 3} to {4, 5, 6}.
            if (m_Selection.Count != m_InitialSelectionCount || m_List.Count != m_ListInitialCount)
            {
                BuildSelection();
                return true;
            }
            return false;
        }

        internal IList<int> indexes => m_ActualSelection;

        /// <summary>
        /// How many items are selected.
        /// </summary>
        public int Count => m_ActualSelection.Count;

        /// <summary>
        /// Lowest selected index of any item in the selection.
        /// </summary>
        /// <returns>Returns the lowest selected index.</returns>
        public int MinIndex()
        {
            return m_ActualSelection.Count > 0 ? m_ActualSelection.Min() : -1;
        }

        /// <summary>
        /// Highest selected index of any item in the selection.
        /// </summary>
        /// <returns>Returns the highest selected index.</returns>
        public int MaxIndex()
        {
            return m_ActualSelection.Count > 0 ? m_ActualSelection.Max() : -1;
        }

        /// <summary>
        /// Get the first selected item in the selection.
        /// </summary>
        /// <returns>First select item in selection. Returns null if no item are selected</returns>
        public SearchItem First()
        {
            if (m_ActualSelection.Count == 0)
                return null;
            if (m_Items == null)
                BuildSelection();
            if (m_Items.Count == 0)
                return null;
            return m_Items[0];
        }

        /// <summary>
        /// Get the last selected item in the selection.
        /// </summary>
        /// <returns>Last select item in selection. Returns null if no item are selected</returns>
        public SearchItem Last()
        {
            if (m_ActualSelection.Count == 0)
                return null;
            if (m_Items == null)
                BuildSelection();
            if (m_Items.Count == 0)
                return null;
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

            // We have to rebuild the list of selected items AND the list of indices
            // to keep them in sync.
            using var _ = ListPool<int>.Get(out var tempSelection);
            tempSelection.AddRange(m_Selection);
            m_ActualSelection.Clear();
            for (var i = 0; i < tempSelection.Count; ++i)
            {
                var index = tempSelection[i];
                if (index < 0 || index >= m_List.Count)
                    continue;
                m_ActualSelection.Add(index);
                m_Items.Add(m_List.ElementAt(index));
            }

            m_InitialSelectionCount = m_Selection.Count;
            m_ListInitialCount = m_List.Count;
        }

        public bool Contains(SearchItem item)
        {
            if (m_Items == null)
                return false;
            foreach (var e in m_Items)
            {
                if (item.Equals(e))
                    return true;
            }

            return false;
        }
    }
}
