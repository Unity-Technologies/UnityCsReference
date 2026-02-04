// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    [Serializable]
    class SerializableShortcutEntry
    {
        public Identifier identifier;
        public List<KeyCombination> combinations;

        internal SerializableShortcutEntry()
        {
        }

        internal SerializableShortcutEntry(Identifier id, IEnumerable<KeyCombination> combinations)
        {
            identifier = id;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            this.combinations = combinations.ToList();
#pragma warning restore UA2001
        }

        internal SerializableShortcutEntry(ShortcutEntry entry)
        {
            identifier = entry.identifier;
            combinations = new List<KeyCombination>(entry.combinations);
        }
    }

    [Serializable]
    class ShortcutProfile
    {
        public string id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        public string parentId
        {
            get { return m_ParentId; }
            set { m_ParentId = value; }
        }

        [SerializeField]
        string m_Id;
        [SerializeField]
        string m_ParentId;

        [SerializeField]
        List<SerializableShortcutEntry> m_Entries = new List<SerializableShortcutEntry>();

        public ShortcutProfile parent { get; internal set; }

        public IEnumerable<SerializableShortcutEntry> entries => m_Entries;

        internal ShortcutProfile()
        {
        }

        internal ShortcutProfile(string id, IEnumerable<SerializableShortcutEntry> entries, string parentId = "")
        {
            m_Id = id;
            m_ParentId = parentId;
            m_Entries = new List<SerializableShortcutEntry>(entries);
        }

        internal ShortcutProfile(string id, string parentId = "")
        {
            m_Id = id;
            m_ParentId = parentId;
        }

        internal void BreakParentLink()
        {
            m_ParentId = "";
            parent = null;
        }

        public void Add(ShortcutEntry profileEntry)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var existingEntry = m_Entries.SingleOrDefault(entry => entry.identifier.Equals(profileEntry.identifier));
#pragma warning restore UA2001
            if (existingEntry != null)
            {
                throw new ArgumentException("This profile already contains an existing entry with matching Identifier!", nameof(profileEntry));
            }
            m_Entries.Add(new SerializableShortcutEntry(profileEntry));
        }

        public void Remove(Identifier identifier)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var existingEntry = m_Entries.FirstOrDefault(entry => entry.identifier.Equals(identifier));
#pragma warning restore UA2001
            m_Entries.Remove(existingEntry);
        }
    }
}
