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
        protected VisualState[] m_OrderedVisualStates = Array.Empty<VisualState>();
        protected virtual IEnumerable<VisualState> orderedList => m_OrderedVisualStates;

        [SerializeField]
        private List<string> m_OrderedGroups = new();
        public virtual IReadOnlyCollection<string> orderedGroupNames => m_OrderedGroups;

        public virtual long countLoaded => m_OrderedVisualStates.Length;
        public virtual long countTotal => m_OrderedVisualStates.Length;

        // a reverse look up table such that we can find a visual state easily through unique id
        protected Dictionary<string, int> m_UniqueIdToIndexLookup = new();

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

        public void Rebuild(IReadOnlyCollection<string> itemUniqueIds)
        {
            Rebuild(itemUniqueIds.SelectToNewArray(id => Get(id) ?? new VisualState(id)));
        }

        public void Rebuild(VisualState[] orderedVisualStates)
        {
            m_OrderedVisualStates = orderedVisualStates;
            m_OrderedGroups = new List<string>(m_OrderedVisualStates.SelectAsEnumerable(v => v.groupName).EnumerateDistinct());
            SetupLookupTable();
        }

        public virtual VisualState Get(string itemUniqueId)
        {
            if (!string.IsNullOrEmpty(itemUniqueId) && m_UniqueIdToIndexLookup.TryGetValue(itemUniqueId, out var index))
                return m_OrderedVisualStates[index];
            return null;
        }

        public virtual bool Contains(string itemUniqueId)
        {
            return !string.IsNullOrEmpty(itemUniqueId) && m_UniqueIdToIndexLookup.ContainsKey(itemUniqueId);
        }

        public virtual VisualState GetNext(string itemUniqueId, bool reverseOrder = false)
        {
            if (string.IsNullOrEmpty(itemUniqueId) || !m_UniqueIdToIndexLookup.TryGetValue(itemUniqueId, out var index))
                return null;
            var nextIndex = reverseOrder ? index - 1 : index + 1;
            return nextIndex >= 0 && nextIndex < m_OrderedVisualStates.Length ? m_OrderedVisualStates[nextIndex] : null;
        }

        protected void SetupLookupTable()
        {
            m_UniqueIdToIndexLookup.Clear();
            for (var i = 0; i < m_OrderedVisualStates.Length; i++)
                m_UniqueIdToIndexLookup[m_OrderedVisualStates[i].itemUniqueId] = i;
        }

        public virtual IEnumerator<VisualState> GetEnumerator()
        {
            return orderedList.GetEnumerator();
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
