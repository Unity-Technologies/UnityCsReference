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
    internal class PageSelection : IReadOnlyCollection<string>, ISerializationCallbackReceiver
    {
        private string[] m_PreviousSelections = Array.Empty<string>();

        private HashSet<string> m_SelectionsLookup = new ();

        // The selection list is the order by when the user selects them (not in the order shown in the UI)
        // We keep track of it to make sure that if the user uses the keyboard to move in the item list, we start from their last selection
        [SerializeField]
        private List<string> m_OrderedSelections = new ();

        public IReadOnlyList<string> previousSelections => m_PreviousSelections;

        public string first => m_OrderedSelections.Count > 0 ? m_OrderedSelections[0] : null;
        public string last => m_OrderedSelections.Count > 0 ? m_OrderedSelections[^1] : null;

        public int Count => m_SelectionsLookup.Count;

        public bool SetNewSelection(IEnumerable<string> itemUniqueIds)
        {
            m_OrderedSelections.ToArray(ref m_PreviousSelections);
            m_OrderedSelections.Clear();
            m_OrderedSelections.AddRange(itemUniqueIds);
            var selectionUpdated = !m_PreviousSelections.IsSequenceEqual(m_OrderedSelections);
            if (selectionUpdated)
                m_OrderedSelections.ToHashSet(ref m_SelectionsLookup);

            return selectionUpdated;
        }

        public bool AmendSelection(IEnumerable<string> toAdd, IEnumerable<string> toRemove)
        {
            m_OrderedSelections.ToArray(ref m_PreviousSelections);

            var selectionUpdated = false;
            if (toRemove != null)
                foreach (var item in toRemove)
                    selectionUpdated |= m_SelectionsLookup.Remove(item);

            // We rebuild the ordered selection list separately here once if needed to avoid worst case O(n^2) removal
            if (selectionUpdated)
            {
                m_OrderedSelections.Clear();
                foreach (var item in m_PreviousSelections)
                    if (m_SelectionsLookup.Contains(item))
                        m_OrderedSelections.Add(item);
            }

            if (toAdd != null)
                foreach (var item in toAdd)
                    if (m_SelectionsLookup.Add(item))
                    {
                        selectionUpdated = true;
                        m_OrderedSelections.Add(item);
                    }

            return selectionUpdated;
        }

        public bool ToggleSelection(string itemUniqueId)
        {
            var itemSelected = Contains(itemUniqueId);

            // If the item is the only one selected right now, we don't want to toggle it off
            if (itemSelected && Count == 1)
                return false;

            m_OrderedSelections.ToArray(ref m_PreviousSelections);
            if (!itemSelected)
            {
                if (m_SelectionsLookup.Add(itemUniqueId))
                    m_OrderedSelections.Add(itemUniqueId);
            }
            else
            {
                m_SelectionsLookup.Remove(itemUniqueId);
                m_OrderedSelections.RemoveAll(i => i == itemUniqueId);
            }
            return true;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_OrderedSelections.ToHashSet(ref m_SelectionsLookup);
        }

        public bool Contains(string itemUniqueId)
        {
            return m_SelectionsLookup.Contains(itemUniqueId);
        }

        [ExcludeFromCodeCoverage]
        public IEnumerator<string> GetEnumerator()
        {
            return m_SelectionsLookup.GetEnumerator();
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_SelectionsLookup.GetEnumerator();
        }
    }
}
