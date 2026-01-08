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
        private IReadOnlyCollection<string> m_PreviousSelections = Array.Empty<string>();

        private HashSet<string> m_SelectionsLookup = new ();

        // The order of the selection is the order in which the user selects the packages.
        // We keep track of it to make sure that if the user uses the keyboard to move in the package list, we start from their last selection
        [SerializeField]
        private List<string> m_OrderedSelections = new ();

        [SerializeField]
        private string[] m_SerializedSelectionsLookup = Array.Empty<string>();

        public IReadOnlyCollection<string> previousSelections => m_PreviousSelections;
        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IEnumerable<string> orderedSelections => m_OrderedSelections.Where(s => Contains(s));
#pragma warning restore RS0030

        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public string firstSelection => orderedSelections.FirstOrDefault();
#pragma warning restore RS0030
        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public string lastSelection => orderedSelections.LastOrDefault();
#pragma warning restore RS0030

        // Normally we begin with lower case for public attribute, but `Count` is a very common container attribute, we want to make it consistent with System collections.
        public int Count => m_SelectionsLookup.Count;

        public bool SetNewSelection(IEnumerable<string> packageUniqueIds)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var newSelectionLookup = packageUniqueIds.ToHashSet();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectionUpdated = newSelectionLookup.Any(s => !m_SelectionsLookup.Contains(s))
#pragma warning restore RS0030
                                   #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                   || m_SelectionsLookup.Any(s => !newSelectionLookup.Contains(s));
#pragma warning restore RS0030

            if (selectionUpdated)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_PreviousSelections = orderedSelections.ToArray();
#pragma warning restore RS0030
                m_SelectionsLookup = newSelectionLookup;

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_OrderedSelections = packageUniqueIds.ToList();
#pragma warning restore RS0030
            }
            return selectionUpdated;
        }

        private void TrimOrderedSelection()
        {
            if (m_OrderedSelections.Count != m_SelectionsLookup.Count)
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_OrderedSelections = m_OrderedSelections.Where(s => Contains(s)).ToList();
#pragma warning restore RS0030
        }

        public bool AmendSelection(IEnumerable<string> toAddOrUpdate, IEnumerable<string> toRemove)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var itemsToAdd = toAddOrUpdate?.Where(s => !Contains(s)).ToArray() ?? Array.Empty<string>();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var itemsToRemove = toRemove?.Where(s => Contains(s)).ToArray() ?? Array.Empty<string>();
#pragma warning restore RS0030
            if (itemsToAdd.Length == 0 && itemsToRemove.Length == 0)
                return false;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_PreviousSelections = orderedSelections.ToArray();
#pragma warning restore RS0030

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

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_PreviousSelections = orderedSelections.ToArray();
#pragma warning restore RS0030
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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SerializedSelectionsLookup = m_SelectionsLookup.ToArray();
#pragma warning restore RS0030
        }

        public void OnAfterDeserialize()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SelectionsLookup = m_SerializedSelectionsLookup.ToHashSet();
#pragma warning restore RS0030
        }

        public bool Contains(string packageUniqueId)
        {
            return m_SelectionsLookup.Contains(packageUniqueId);
        }

        [ExcludeFromCodeCoverage]
        public IEnumerator<string> GetEnumerator()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_SelectionsLookup.Cast<string>().GetEnumerator();
#pragma warning restore RS0030
        }

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_SelectionsLookup.GetEnumerator();
        }
    }
}
