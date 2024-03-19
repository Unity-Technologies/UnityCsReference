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
    internal class PageSelection : IEnumerable<string>, ISerializationCallbackReceiver
    {
        private IEnumerable<string> m_PreviousSelections = Enumerable.Empty<string>();

        private HashSet<string> m_SelectionsLookup = new ();

        // The order of the selection is the order in which the user selects the packages.
        // We keep track of it to make sure that if the user uses the keyboard to move in the package list, we start from their last selection
        [SerializeField]
        private List<string> m_OrderedSelections = new ();

        [SerializeField]
        private string[] m_SerializedSelectionsLookup = Array.Empty<string>();

        public IEnumerable<string> previousSelections => m_PreviousSelections;
        public IEnumerable<string> orderedSelections => m_OrderedSelections.Where(s => Contains(s));

        public string firstSelection => orderedSelections.FirstOrDefault();
        public string lastSelection => orderedSelections.LastOrDefault();

        // Normally we begin with lower case for public attribute, but `Count` is a very common container attribute, we want to make it consistent with System collections.
        public int Count => m_SelectionsLookup.Count;

        public bool SetNewSelection(IEnumerable<string> packageUniqueIds)
        {
            var newSelectionLookup = packageUniqueIds.ToHashSet();
            var selectionUpdated = newSelectionLookup.Any(s => !m_SelectionsLookup.Contains(s))
                                   || m_SelectionsLookup.Any(s => !newSelectionLookup.Contains(s));

            if (selectionUpdated)
            {
                m_PreviousSelections = orderedSelections.ToArray();
                m_SelectionsLookup = newSelectionLookup;

                m_OrderedSelections = packageUniqueIds.ToList();
            }
            return selectionUpdated;
        }

        private void TrimOrderedSelection()
        {
            if (m_OrderedSelections.Count != m_SelectionsLookup.Count)
                m_OrderedSelections = m_OrderedSelections.Where(s => Contains(s)).ToList();
        }

        public bool AmendSelection(IEnumerable<string> toAddOrUpdate, IEnumerable<string> toRemove)
        {
            var itemsToAdd = toAddOrUpdate?.Where(s => !Contains(s)).ToArray() ?? Array.Empty<string>();
            var itemsToRemove = toRemove?.Where(s => Contains(s)).ToArray() ?? Array.Empty<string>();
            if (!itemsToAdd.Any() && !itemsToRemove.Any())
                return false;

            m_PreviousSelections = orderedSelections.ToArray();

            foreach (var item in itemsToAdd)
            {
                m_SelectionsLookup.Add(item);
                m_OrderedSelections.Add(item);
            }

            foreach (var item in itemsToRemove)
                m_SelectionsLookup.Remove(item);

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
                m_SelectionsLookup.Add(packageUniqueId);
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
            m_SerializedSelectionsLookup = m_SelectionsLookup.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_SelectionsLookup = m_SerializedSelectionsLookup.ToHashSet();
        }

        public bool Contains(string packageUniqueId)
        {
            return m_SelectionsLookup.Contains(packageUniqueId);
        }

        [ExcludeFromCodeCoverage]
        public IEnumerator<string> GetEnumerator()
        {
            return m_SelectionsLookup.Cast<string>().GetEnumerator();
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_SelectionsLookup.GetEnumerator();
        }
    }
}
