// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class VisualStateList : ISerializationCallbackReceiver, IVisualStateList
    {
        [SerializeField]
        protected List<VisualState> m_OrderedVisualStates = new();
        protected virtual IEnumerable<VisualState> orderedListBeforeGrouping => m_OrderedVisualStates;

        [SerializeField]
        private string[] m_OrderedGroups = Array.Empty<string>();
        public virtual IList<string> orderedGroups => m_OrderedGroups;

        public virtual long countLoaded => m_OrderedVisualStates.Count;
        public virtual long countTotal => m_OrderedVisualStates.Count;

        // a reverse look up table such that we can find an visual state easily through package unique id
        protected Dictionary<string, int> m_UniqueIdToIndexLookup = new();

        public VisualStateList() : this(Enumerable.Empty<string>()) {}

        public VisualStateList(IEnumerable<string> packageUniqueIds)
        {
            Rebuild(packageUniqueIds ?? Enumerable.Empty<string>());
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

        public void Rebuild(IEnumerable<string> packageUniqueIds)
        {
            Rebuild(packageUniqueIds.Select(id => Get(id) ?? new VisualState(id)));
        }

        public void Rebuild(IEnumerable<VisualState> orderedVisualStates, IEnumerable<string> orderedGroupNames = null)
        {
            m_OrderedVisualStates = orderedVisualStates.ToList();
            m_OrderedGroups = orderedGroupNames?.ToArray() ?? Array.Empty<string>();
            SetupLookupTable();
        }

        public virtual VisualState Get(string packageUniqueId)
        {
            if (!string.IsNullOrEmpty(packageUniqueId) && m_UniqueIdToIndexLookup.TryGetValue(packageUniqueId, out var index))
                return m_OrderedVisualStates[index];
            return null;
        }

        public virtual bool Contains(string packageUniqueId)
        {
            return !string.IsNullOrEmpty(packageUniqueId) && m_UniqueIdToIndexLookup.ContainsKey(packageUniqueId);
        }

        protected void SetupLookupTable()
        {
            m_UniqueIdToIndexLookup.Clear();
            for (var i = 0; i < m_OrderedVisualStates.Count; i++)
                m_UniqueIdToIndexLookup[m_OrderedVisualStates[i].packageUniqueId] = i;
        }

        public virtual IEnumerator<VisualState> GetEnumerator()
        {
            // The final ordering of the visual states is decided by the order of the group and the order of the items
            // We prioritize the order of the groups, i.e. items matching the first group name will always be enumerated first
            // The order of items within each group is kept untouched
            if (m_OrderedGroups.Length > 1)
                foreach (var groupName in m_OrderedGroups)
                    foreach (var v in orderedListBeforeGrouping.Where(v => v.groupName == groupName))
                        yield return v;
            else
                foreach (var v in orderedListBeforeGrouping)
                    yield return v;
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
