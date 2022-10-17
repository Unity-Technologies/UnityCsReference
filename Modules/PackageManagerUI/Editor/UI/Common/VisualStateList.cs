// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class VisualStateList : ISerializationCallbackReceiver, IEnumerable<VisualState>
    {
        [SerializeField]
        protected List<VisualState> m_OrderedVisualStates = new List<VisualState>();
        public virtual IEnumerable<VisualState> orderedList => m_OrderedVisualStates;

        public virtual long numItems => m_OrderedVisualStates.Count;
        public virtual long numTotalItems => m_OrderedVisualStates.Count;

        // a reverse look up table such that we can find an visual state easily through package unique id
        protected Dictionary<string, int> m_UniqueIdToIndexLookup = new Dictionary<string, int>();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            SetupLookupTable();
        }

        public void Rebuild(IEnumerable<string> packageUniqueIds)
        {
            var newVisualStates = packageUniqueIds.Select(id => GetVisualState(id) ?? new VisualState(id, string.Empty, false)).ToList();
            m_OrderedVisualStates = newVisualStates;
            SetupLookupTable();
        }

        public void Rebuild(IEnumerable<Tuple<string, string, bool>> orderedPackageIdGroupsAndDefaultLockedStates)
        {
            var newVisualStates = orderedPackageIdGroupsAndDefaultLockedStates.Select(t =>
            {
                var visualState = GetVisualState(t.Item1) ?? new VisualState(t.Item1, t.Item2, t.Item3);
                visualState.groupName = t.Item2;
                visualState.lockedByDefault = t.Item3;
                return visualState;
            }).ToList();
            m_OrderedVisualStates = newVisualStates;
            SetupLookupTable();
        }

        public virtual VisualState GetVisualState(string packageUniqueId)
        {
            int index;
            if (!string.IsNullOrEmpty(packageUniqueId) && m_UniqueIdToIndexLookup.TryGetValue(packageUniqueId, out index))
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
            return orderedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return orderedList.GetEnumerator();
        }
    }
}
