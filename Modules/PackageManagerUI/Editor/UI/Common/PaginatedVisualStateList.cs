// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PaginatedVisualStateList : VisualStateList
    {
        [SerializeField]
        private long m_NumTotalItems;
        public override long numTotalItems => m_NumTotalItems;

        public void SetTotal(long total)
        {
            m_NumTotalItems = total;
        }

        public override long numItems => m_OrderedVisualStates.Count + m_ExtraVisualStates.Count;

        // in the case where the user wants to check a package that is not in the current list (but will be if user keep loading more packages)
        // we handle it by allowing to show some `extra` packages in the list. we don't want to add it directly to the ordered list because
        // these extra items are out of order
        [SerializeField]
        private List<VisualState> m_ExtraVisualStates = new List<VisualState>();
        public override IEnumerable<VisualState> orderedList => m_OrderedVisualStates.Concat(m_ExtraVisualStates);

        public override bool Contains(string packageUniqueId)
        {
            return base.Contains(packageUniqueId) || m_ExtraVisualStates.Any(v => v.packageUniqueId == packageUniqueId);
        }

        public override VisualState GetVisualState(string packageUniqueId)
        {
            return base.GetVisualState(packageUniqueId) ?? m_ExtraVisualStates.FirstOrDefault(v => v.packageUniqueId == packageUniqueId);
        }

        public void AddRange(IEnumerable<string> packageUniqueIds)
        {
            m_OrderedVisualStates.AddRange(packageUniqueIds.Select(id => GetVisualState(id) ?? new VisualState(id)));
            SetupLookupTable();
        }

        public void AddExtraItem(string packageUniqueId)
        {
            m_ExtraVisualStates.Add(new VisualState(packageUniqueId));
        }

        public void ClearExtraItems()
        {
            m_ExtraVisualStates.Clear();
        }

        public void ClearList()
        {
            m_OrderedVisualStates.Clear();
            m_UniqueIdToIndexLookup.Clear();
        }
    }
}
