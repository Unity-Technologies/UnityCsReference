// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class CyclicDependencyException : Exception
    {
    }


    [Serializable]
    class SerializableShortcutEntry
    {
        public Identifier identifier;
        public List<KeyCombination> keyCombination;

        internal SerializableShortcutEntry()
        {
        }

        internal SerializableShortcutEntry(ShortcutEntry entry)
        {
            identifier = entry.identifier;
            keyCombination = new List<KeyCombination>(entry.combinations);
        }
    }

    [Serializable]
    class ShortcutProfile
    {
        public string id => m_Id;
        [SerializeField]
        internal string m_Id;

        [SerializeField]
        List<SerializableShortcutEntry> m_Entries = new List<SerializableShortcutEntry>();

        IShortcutProfileLoader m_Loader;

        public IEnumerable<SerializableShortcutEntry> entries => m_Entries;

        internal ShortcutProfile()
        {
        }

        internal ShortcutProfile(string id, IEnumerable<SerializableShortcutEntry> entries, IShortcutProfileLoader loader = null)
        {
            m_Id = id;
            m_Loader = loader ?? new ShortcutProfileLoader();

            m_Entries = new List<SerializableShortcutEntry>(entries);
        }

        internal ShortcutProfile(string id, IShortcutProfileLoader loader = null)
        {
            m_Loader = loader ?? new ShortcutProfileLoader();
            m_Id = id;
            LoadAndApplyJsonFile(id, this);
        }

        void LoadAndApplyJsonFile(string id, ShortcutProfile instance)
        {
            if (!m_Loader.ProfileExists(id))
                return;
            var json = m_Loader.LoadShortcutProfileJson(id);
            JsonUtility.FromJsonOverwrite(json, instance);
        }

        internal void SaveToDisk()
        {
            m_Loader.SaveShortcutProfileJson(id, JsonUtility.ToJson(this));
        }

        public void Add(ShortcutEntry profileEntry)
        {
            var existingEntry = m_Entries.SingleOrDefault(entry => entry.identifier.Equals(profileEntry.identifier));
            if (existingEntry != null)
            {
                throw new InvalidOperationException("This profile already contains an existing entry with matching Identifier");
            }
            m_Entries.Add(new SerializableShortcutEntry(profileEntry));
        }
    }
}
