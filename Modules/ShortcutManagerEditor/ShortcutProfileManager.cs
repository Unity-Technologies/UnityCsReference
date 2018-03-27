// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    interface IShortcutProfileManager
    {
        // Preferences UI relies on this order being stable
        IEnumerable<ShortcutEntry> GetAllShortcuts();
        void ModifyShortcutEntry(Identifier identifier, List<KeyCombination> combinationSequence);
        void ApplyActiveProfile();
        event Action<IShortcutProfileManager> shortcutsModified;
        void ResetToDefault();
        void PersistChanges();
    }

    class ShortcutProfileManager : IShortcutProfileManager
    {
        const string k_UserProfileId = "UserProfile";
        IEnumerable<ShortcutEntry> entries;
        ShortcutProfile m_ActiveProfile = new ShortcutProfile();
        public event Action<IShortcutProfileManager> shortcutsModified;

        internal ShortcutProfile activeProfile { get { return m_ActiveProfile; } }


        public ShortcutProfileManager(IEnumerable<ShortcutEntry> baseProfile)
        {
            entries = baseProfile.ToList();
        }

        public IEnumerable<ShortcutEntry> GetAllShortcuts()
        {
            return entries;
        }

        internal void ApplyProfile(ShortcutProfile shortcutProfile, bool migratePrefKeys = false)
        {
            // pref keys should only be migrated when the default user profile is applied
            if (migratePrefKeys)
            {
                foreach (var entry in entries)
                {
                    if (entry.prefKeyMigratedValue != null)
                    {
                        var overrideEntry = new SerializableShortcutEntry
                        {
                            identifier = entry.identifier,
                            keyCombination = new List<KeyCombination> { entry.prefKeyMigratedValue.Value }
                        };
                        entry.ApplyOverride(overrideEntry);
                    }
                }
            }
            foreach (var shortcutOverride in shortcutProfile.entries)
            {
                var entry = entries.FirstOrDefault(e => e.identifier.Equals(shortcutOverride.identifier));
                if (entry != null)
                {
                    entry.ApplyOverride(shortcutOverride);
                }
            }
            m_ActiveProfile = shortcutProfile;
        }

        public void ResetToDefault()
        {
            foreach (var entry in entries)
            {
                entry.ResetToDefault();
            }
        }

        public void ApplyActiveProfile()
        {
            m_ActiveProfile = new ShortcutProfile(k_UserProfileId);
            ApplyProfile(m_ActiveProfile, true);
        }

        public void ModifyShortcutEntry(Identifier identifier, List<KeyCombination> combinationSequence)
        {
            var shortcutEntry = entries.FirstOrDefault(e => e.identifier.Equals(identifier));
            shortcutEntry.SetOverride(combinationSequence);

            SerializableShortcutEntry profileEntry = null;
            foreach (var activeProfileEntry in m_ActiveProfile.entries)
            {
                if (activeProfileEntry.identifier.Equals(identifier))
                {
                    profileEntry = activeProfileEntry;
                    profileEntry.keyCombination = new List<KeyCombination>(combinationSequence);
                    break;
                }
            }

            if (profileEntry == null)
            {
                m_ActiveProfile.Add(shortcutEntry);
            }

            if (shortcutsModified != null)
                shortcutsModified(this);
        }

        public void PersistChanges()
        {
            m_ActiveProfile.SaveToDisk();
        }
    }
}
