// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.HierarchyV2
{
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    interface ICollectionViewSelectionContainer
    {
        bool MatchesExistingSelection(ReadOnlySpan<int> indices);

        /// <summary>
        /// Gets the number of selected indices.
        /// </summary>
        int indexCount { get; }

        /// <summary>
        /// Gets the minimum selected index, or -1 if no selection.
        /// </summary>
        int minIndex { get; }

        /// <summary>
        /// Gets the maximum selected index, or -1 if no selection.
        /// </summary>
        int maxIndex { get; }

        /// <summary>
        /// Gets the selected index, or -1 if no selection.
        /// If multiple indices are selected, returns the first selected index.
        /// </summary>
        int selectedIndex { get; set; }

        /// <summary>
        /// Determines whether the specified index is selected.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <returns>true if the index is selected; otherwise, false.</returns>
        bool ContainsIndex(int index);

        /// <summary>
        /// Adds an index to the selection.
        /// </summary>
        /// <param name="index">The index to add.</param>
        void Add(int index);

        /// <summary>
        /// Adds a range of index to the selection.
        /// </summary>
        /// <param name="newIndices">The indices to add.</param>
        void AddRange(ReadOnlySpan<int> newIndices);

        /// <summary>
        /// Attempts to remove an index from the selection.
        /// </summary>
        /// <param name="index">The index to remove.</param>
        /// <returns>true if the index was removed; false if it wasn't in the selection.</returns>
        void Remove(int index);

        /// <summary>
        /// Clears all selected indices.
        /// </summary>
        void Clear();

        /// <summary>
        /// Selects all items.
        /// </summary>
        void SelectAll();

        /// <summary>
        /// Replaces the current selection with the specified indices.
        /// This allows clearing and setting the selection in one step.
        /// </summary>
        /// <param name="indices">The indices to select.</param>
        void Select(ReadOnlySpan<int> indices);
    }

    sealed class CollectionViewSelection : ICollectionViewSelectionContainer
    {
        readonly HashSet<int> m_IndexLookup = new();
        readonly CollectionView m_CollectionView;

        int m_MinIndex = -1;
        int m_MaxIndex = -1;

        public CollectionViewSelection(CollectionView collectionView)
        {
            m_CollectionView = collectionView;
        }

        public List<int> indices { get; } = new();
        public int indexCount => indices.Count;

        public int minIndex
        {
            get
            {
                if (m_MinIndex != -1)
                    return m_MinIndex;

                m_MinIndex = int.MaxValue;
                foreach (var index in indices)
                {
                    if (index < m_MinIndex)
                        m_MinIndex = index;
                }

                return m_MinIndex;
            }
        }

        public int maxIndex
        {
            get
            {
                if (m_MaxIndex != -1)
                    return m_MaxIndex;

                foreach(var index in indices)
                {
                    if (index > m_MaxIndex)
                        m_MaxIndex = index;
                }

                return m_MaxIndex;
            }
        }

        public bool MatchesExistingSelection(ReadOnlySpan<int> selectedIndices)
        {
            if (selectedIndices.Length != indexCount)
                return false;

            var existingSelection = NoAllocHelpers.CreateReadOnlySpan(indices);
            return existingSelection.SequenceEqual(selectedIndices);
        }

        public int selectedIndex
        {
            get => indices.Count > 0 ? indices[0] : -1;
            set
            {
                Clear();
                Add(value);
            }
        }

        public bool ContainsIndex(int index) => m_IndexLookup.Contains(index);

        public void Add(int index)
        {
            if (!m_IndexLookup.Add(index))
                return;

            indices.Add(index);

            if (index < m_MinIndex)
                m_MinIndex = index;

            if (index > m_MaxIndex)
                m_MaxIndex = index;
        }

        public void AddRange(ReadOnlySpan<int> newIndices)
        {
            if (indices.Capacity < indices.Count + newIndices.Length)
                indices.Capacity = indices.Count + newIndices.Length;

            foreach (var index in newIndices)
                Add(index);
        }

        public void Remove(int index)
        {
            if (!m_IndexLookup.Remove(index))
                return;

            var i = indices.IndexOf(index);
            if (i < 0)
                return;

            indices.RemoveAt(i);

            if (index == m_MinIndex)
                m_MinIndex = -1;
            if (index == m_MaxIndex)
                m_MaxIndex = -1;
        }

        public void Clear()
        {
            m_IndexLookup.Clear();
            indices.Clear();
            m_MinIndex = -1;
            m_MaxIndex = -1;
        }

        public void SelectAll()
        {
            Clear();
            var count = m_CollectionView.itemsSource?.Count ?? 0;
            if (count <= 0)
                return;

            indices.Capacity = count;
            for (var i = 0; i < count; i++)
            {
                m_IndexLookup.Add(i);
                indices.Add(i);
            }

            m_MinIndex = 0;
            m_MaxIndex = count - 1;
        }

        public void Select(ReadOnlySpan<int> newIndices)
        {
            Clear();
            AddRange(newIndices);
        }
    }
}
