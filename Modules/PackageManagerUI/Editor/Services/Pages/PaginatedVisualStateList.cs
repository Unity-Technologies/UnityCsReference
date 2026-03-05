// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PaginatedVisualStateList : VisualStateList
    {
        [SerializeField]
        private long m_NumTotalItems;
        public override long countTotal => m_NumTotalItems;

        public void SetTotal(long total)
        {
            m_NumTotalItems = total;
        }

        public override long countLoaded => m_OrderedVisualStates.Length + m_ExtraVisualStates.Count;

        // in the case where the user wants to check a package that is not in the current list (but will be if user keep loading more packages)
        // we handle it by allowing to show some `extra` packages in the list. we don't want to add it directly to the ordered list because
        // these extra items are out of order
        [SerializeField]
        private List<VisualState> m_ExtraVisualStates = new();
        protected override IEnumerable<VisualState> orderedList => m_OrderedVisualStates.Join(m_ExtraVisualStates);

        public PaginatedVisualStateList() {}

        public PaginatedVisualStateList(IReadOnlyCollection<string> itemUniqueIds) : base(itemUniqueIds) {}

        public override bool Contains(string itemUniqueId)
        {
            return base.Contains(itemUniqueId) || m_ExtraVisualStates.Exists(v => v.itemUniqueId == itemUniqueId);
        }

        public override VisualState Get(string itemUniqueId)
        {
            return base.Get(itemUniqueId) ?? m_ExtraVisualStates.FirstMatch(v => v.itemUniqueId == itemUniqueId);
        }

        public override VisualState GetNext(string itemUniqueId, bool reverseOrder = false)
        {
            if (string.IsNullOrEmpty(itemUniqueId) || m_ExtraVisualStates.Count == 0)
                return base.GetNext(itemUniqueId, reverseOrder);

            if (m_UniqueIdToIndexLookup.TryGetValue(itemUniqueId, out var index))
            {
                if (index == m_OrderedVisualStates.Length && !reverseOrder)
                    return m_ExtraVisualStates[0];
                return base.GetNext(itemUniqueId, reverseOrder);
            }

            var extraIndex = m_ExtraVisualStates.FindIndex(v => v.itemUniqueId == itemUniqueId);
            if (extraIndex < 0)
                return null;

            var nextIndex = reverseOrder ? extraIndex - 1 : extraIndex + 1;
            if (nextIndex == -1)
                return m_OrderedVisualStates.Length == 0  ? null : m_OrderedVisualStates[m_OrderedVisualStates.Length - 1];
            return nextIndex == m_ExtraVisualStates.Count ? null : m_ExtraVisualStates[nextIndex];
        }

        public void AddRange(IReadOnlyList<string> itemUniqueIds)
        {
            m_OrderedVisualStates = m_OrderedVisualStates.Join(itemUniqueIds.SelectAsEnumerable(id => Get(id) ?? new VisualState(id, string.Empty, false))).ToNewArray(m_OrderedVisualStates.Length + itemUniqueIds.Count);
            SetupLookupTable();
        }

        public void AddExtraItem(string itemUniqueId)
        {
            m_ExtraVisualStates.Add(new VisualState(itemUniqueId, string.Empty, false));
        }

        public void ClearExtraItems()
        {
            m_ExtraVisualStates.Clear();
        }

        public void ClearAll()
        {
            m_OrderedVisualStates = Array.Empty<VisualState>();
            m_UniqueIdToIndexLookup.Clear();
            ClearExtraItems();
        }
    }
}
