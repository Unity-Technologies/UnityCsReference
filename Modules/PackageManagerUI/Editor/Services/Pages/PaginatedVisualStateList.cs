// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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

        public override long countLoaded => m_OrderedVisualStates.Count + m_ExtraVisualStates.Count;

        // in the case where the user wants to check a package that is not in the current list (but will be if user keep loading more packages)
        // we handle it by allowing to show some `extra` packages in the list. we don't want to add it directly to the ordered list because
        // these extra items are out of order
        [SerializeField]
        private List<VisualState> m_ExtraVisualStates = new();
        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        protected override IEnumerable<VisualState> orderedListBeforeGrouping => m_OrderedVisualStates.Concat(m_ExtraVisualStates);
#pragma warning restore RS0030

        public PaginatedVisualStateList() {}

        public PaginatedVisualStateList(IEnumerable<string> packageUniqueIds) : base(packageUniqueIds) {}

        public override bool Contains(string packageUniqueId)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return base.Contains(packageUniqueId) || m_ExtraVisualStates.Any(v => v.packageUniqueId == packageUniqueId);
#pragma warning restore RS0030
        }

        public override VisualState Get(string packageUniqueId)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return base.Get(packageUniqueId) ?? m_ExtraVisualStates.FirstOrDefault(v => v.packageUniqueId == packageUniqueId);
#pragma warning restore RS0030
        }

        public void AddRange(IEnumerable<string> packageUniqueIds)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_OrderedVisualStates.AddRange(packageUniqueIds.Select(id => Get(id) ?? new VisualState(id, string.Empty, false)));
#pragma warning restore RS0030
            SetupLookupTable();
        }

        public void AddExtraItem(string packageUniqueId)
        {
            m_ExtraVisualStates.Add(new VisualState(packageUniqueId, string.Empty, false));
        }

        public void ClearExtraItems()
        {
            m_ExtraVisualStates.Clear();
        }

        public void ClearAll()
        {
            m_OrderedVisualStates.Clear();
            m_UniqueIdToIndexLookup.Clear();
            ClearExtraItems();
        }
    }
}
