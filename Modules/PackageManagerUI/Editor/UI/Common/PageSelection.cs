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
    internal class PageSelection : IEnumerable<PackageAndVersionIdPair>, ISerializationCallbackReceiver
    {
        private IEnumerable<PackageAndVersionIdPair> m_PreviousSelections = Enumerable.Empty<PackageAndVersionIdPair>();

        private Dictionary<string, PackageAndVersionIdPair> m_SelectionsLookup = new Dictionary<string, PackageAndVersionIdPair>();

        [SerializeField]
        private List<string> m_OrderedSelections = new List<string>();

        [SerializeField]
        private PackageAndVersionIdPair[] m_SerializedSelectionsLookup = new PackageAndVersionIdPair[0];

        public IEnumerable<PackageAndVersionIdPair> previousSelections => m_PreviousSelections;
        public IEnumerable<PackageAndVersionIdPair> orderedSelections => m_OrderedSelections.Select(s => TryGetValue(s, out var value) ? value : null).Where(s => s != null);

        public PackageAndVersionIdPair firstSelection => orderedSelections.FirstOrDefault();
        public PackageAndVersionIdPair lastSelection => orderedSelections.LastOrDefault();

        // Normally we begin with lower case for public attribute, but `Count` is a very common container attribute, we want to make it consistent with System collections.
        public int Count => m_SelectionsLookup.Count;

        public bool SetNewSelection(IEnumerable<PackageAndVersionIdPair> packageAndVersionIds)
        {
            var newSelectionLookup = packageAndVersionIds.ToDictionary(s => s.packageUniqueId, s => s);
            var selectionUpdated = newSelectionLookup.Values.Any(s => !m_SelectionsLookup.TryGetValue(s.packageUniqueId, out var oldSelection) || oldSelection.versionUniqueId != s.versionUniqueId)
                || m_SelectionsLookup.Values.Any(s => !newSelectionLookup.TryGetValue(s.packageUniqueId, out var newSelection) || newSelection.versionUniqueId != s.versionUniqueId);

            if (selectionUpdated)
            {
                m_PreviousSelections = orderedSelections.ToArray();
                m_SelectionsLookup = newSelectionLookup;

                m_OrderedSelections = packageAndVersionIds.Select(s => s.packageUniqueId).ToList();
            }
            return selectionUpdated;
        }

        private void TrimOrderedSelection()
        {
            if (m_OrderedSelections.Count != m_SelectionsLookup.Count)
                m_OrderedSelections = m_OrderedSelections.Where(s => Contains(s)).ToList() ;
        }

        public bool AmendSelection(IEnumerable<PackageAndVersionIdPair> toAddOrUpdate, IEnumerable<PackageAndVersionIdPair> toRemove)
        {
            var itemsToAdd = toAddOrUpdate?.Where(item => !TryGetValue(item.packageUniqueId, out var value)).ToArray() ?? new PackageAndVersionIdPair[0];
            var itemsToUpdate = toAddOrUpdate?.Where(item => TryGetValue(item.packageUniqueId, out var value) && item.versionUniqueId != value.versionUniqueId).ToArray() ?? new PackageAndVersionIdPair[0];
            var itemsToRemove = toRemove?.Where(item => Contains(item.packageUniqueId)).ToArray() ?? new PackageAndVersionIdPair[0];
            if (!itemsToAdd.Any() && !itemsToUpdate.Any() && !itemsToRemove.Any())
                return false;

            m_PreviousSelections = orderedSelections.ToArray();

            foreach (var item in itemsToAdd)
            {
                m_SelectionsLookup[item.packageUniqueId] = item;
                m_OrderedSelections.Add(item.packageUniqueId);
            }

            foreach (var item in itemsToUpdate)
                m_SelectionsLookup[item.packageUniqueId] = item;

            foreach (var item in itemsToRemove)
                m_SelectionsLookup.Remove(item.packageUniqueId);

            TrimOrderedSelection();

            return true;
        }

        public bool ToggleSelection(string packageUniqueId)
        {
            var packageSelected = Contains(packageUniqueId);

            // If the package is the only one selected right now, we don't want to toggle it off
            if (packageSelected && Count == 1)
                return false;

            m_PreviousSelections = orderedSelections.ToArray();
            if (!packageSelected)
            {
                m_SelectionsLookup.Add(packageUniqueId, new PackageAndVersionIdPair(packageUniqueId));
                m_OrderedSelections.Add(packageUniqueId);
            }
            else
            {
                m_SelectionsLookup.Remove(packageUniqueId);
                TrimOrderedSelection();
            }
            return true;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedSelectionsLookup = m_SelectionsLookup.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_SelectionsLookup = m_SerializedSelectionsLookup.ToDictionary(s => s.packageUniqueId, s => s);
        }

        public bool Contains(string packageUniqueId)
        {
            return m_SelectionsLookup.ContainsKey(packageUniqueId);
        }

        public bool TryGetValue(string packageUniqueId, out PackageAndVersionIdPair value)
        {
            return m_SelectionsLookup.TryGetValue(packageUniqueId, out value);
        }

        public IEnumerator<PackageAndVersionIdPair> GetEnumerator()
        {
            return m_SelectionsLookup.Values.Cast<PackageAndVersionIdPair>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_SelectionsLookup.Values.GetEnumerator();
        }
    }
}
