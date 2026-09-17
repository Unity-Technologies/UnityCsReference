// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class VisualStateList : ISerializationCallbackReceiver, IVisualStateList
    {
        [SerializeField]
        private List<VisualState> m_OrderedVisualStates = new();

        [SerializeField]
        private List<string> m_OrderedGroups = new();
        public IReadOnlyList<string> orderedGroupNames => m_OrderedGroups;

        [SerializeField]
        private long m_NumUnloadedItems;

        // Temporary items are for when users open a link to an Asset Store package to show them in Unity
        // they are temporarily appended at the end of the list, and they get removed whenever the list change (rebuild, load more items)
        [SerializeField]
        private int m_NumTemporaryItems;

        public int Count => m_OrderedVisualStates.Count;
        public VisualState this[int index] => m_OrderedVisualStates[index];

        public long countLoaded => m_OrderedVisualStates.Count;
        // Temporary items are not part of the list the back end knows about, so they don't count towards the total
        public long countTotal => m_OrderedVisualStates.Count - m_NumTemporaryItems + m_NumUnloadedItems;

        // a reverse look up table such that we can find a visual state easily through unique id
        private Dictionary<string, int> m_UniqueIdToIndexLookup = new();

        public VisualStateList() : this(Array.Empty<string>()) {}

        public VisualStateList(IReadOnlyCollection<string> itemUniqueIds)
        {
            Rebuild(itemUniqueIds ?? Array.Empty<string>());
        }

        [ExcludeFromCodeCoverage]
        public void OnBeforeSerialize()
        {
        }

        [ExcludeFromCodeCoverage]
        public void OnAfterDeserialize()
        {
            SetupLookupTable();
        }

        public void Rebuild(IReadOnlyCollection<string> itemUniqueIds, long numUnloadedItems = 0)
        {
            Rebuild(itemUniqueIds.SelectToNewArray(id => Get(id) ?? new VisualState(id)), numUnloadedItems);
        }

        public void Rebuild(IReadOnlyCollection<VisualState> orderedVisualStates, long numUnloadedItems = 0)
        {
            Clear();
            m_NumUnloadedItems = numUnloadedItems;
            m_OrderedVisualStates.AddRange(orderedVisualStates);
            m_OrderedGroups.AddRange(m_OrderedVisualStates.SelectAsEnumerable(v => v.groupName).EnumerateDistinct());
            SetupLookupTable();
        }

        public void LoadMoreItems(IReadOnlyList<string> itemUniqueIds)
        {
            // We look up the visual states before clearing the temporary items, so that a temporary item that is now
            // properly loaded keeps the state it had while it was temporary.
            var newVisualStates = itemUniqueIds.SelectToNewArray(id => Get(id) ?? new VisualState(id));
            ClearTemporaryItems();
            foreach (var visualState in newVisualStates)
            {
                if (Contains(visualState.itemUniqueId))
                    continue;
                m_OrderedVisualStates.Add(visualState);
                m_UniqueIdToIndexLookup[visualState.itemUniqueId] = m_OrderedVisualStates.Count - 1;
                m_NumUnloadedItems--;
            }
        }

        public bool AddTemporaryItem(string itemUniqueId)
        {
            if (string.IsNullOrEmpty(itemUniqueId) || m_UniqueIdToIndexLookup.ContainsKey(itemUniqueId))
                return false;
            m_NumTemporaryItems++;
            m_OrderedVisualStates.Add(new VisualState(itemUniqueId));
            m_UniqueIdToIndexLookup[itemUniqueId] = m_OrderedVisualStates.Count - 1;
            return true;
        }

        public void ClearTemporaryItems()
        {
            if (m_NumTemporaryItems <= 0)
                return;
            var firstTemporaryIndex = m_OrderedVisualStates.Count - m_NumTemporaryItems;
            for (var i = firstTemporaryIndex; i < m_OrderedVisualStates.Count; i++)
                m_UniqueIdToIndexLookup.Remove(m_OrderedVisualStates[i].itemUniqueId);
            m_OrderedVisualStates.RemoveRange(firstTemporaryIndex, m_NumTemporaryItems);
            m_NumTemporaryItems = 0;
        }

        public VisualState Get(string itemUniqueId)
        {
            if (!string.IsNullOrEmpty(itemUniqueId) && m_UniqueIdToIndexLookup.TryGetValue(itemUniqueId, out var index))
                return m_OrderedVisualStates[index];
            return null;
        }

        public bool Contains(string itemUniqueId)
        {
            return !string.IsNullOrEmpty(itemUniqueId) && m_UniqueIdToIndexLookup.ContainsKey(itemUniqueId);
        }

        public VisualState GetNext(string itemUniqueId, bool reverseOrder = false)
        {
            if (string.IsNullOrEmpty(itemUniqueId) || !m_UniqueIdToIndexLookup.TryGetValue(itemUniqueId, out var index))
                return null;
            var nextIndex = reverseOrder ? index - 1 : index + 1;
            return nextIndex >= 0 && nextIndex < m_OrderedVisualStates.Count ? m_OrderedVisualStates[nextIndex] : null;
        }

        private void SetupLookupTable()
        {
            m_UniqueIdToIndexLookup.Clear();
            for (var i = 0; i < m_OrderedVisualStates.Count; i++)
                m_UniqueIdToIndexLookup[m_OrderedVisualStates[i].itemUniqueId] = i;
        }

        public void Clear()
        {
            m_OrderedVisualStates.Clear();
            m_OrderedGroups.Clear();
            m_UniqueIdToIndexLookup.Clear();
            m_NumTemporaryItems = 0;
            m_NumUnloadedItems = 0;
        }

        public IEnumerator<VisualState> GetEnumerator()
        {
            return m_OrderedVisualStates.GetEnumerator();
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
