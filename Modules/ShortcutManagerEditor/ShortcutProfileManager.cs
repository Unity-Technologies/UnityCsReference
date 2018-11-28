// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        readonly List<ShortcutEntry> m_Entries;
        ShortcutProfile m_ActiveProfile = new ShortcutProfile();
        public event Action<IShortcutProfileManager> shortcutsModified;

        internal ShortcutProfile activeProfile { get { return m_ActiveProfile; } }


        public ShortcutProfileManager(IEnumerable<ShortcutEntry> baseProfile)
        {
            m_Entries = baseProfile.ToList();
        }

        public IEnumerable<ShortcutEntry> GetAllShortcuts()
        {
            return m_Entries;
        }

        internal void ApplyProfile(ShortcutProfile shortcutProfile)
        {
            foreach (var shortcutOverride in shortcutProfile.entries)
            {
                var entry = m_Entries.FirstOrDefault(e => e.identifier.Equals(shortcutOverride.identifier));
                if (entry != null)
                {
                    entry.ApplyOverride(shortcutOverride);
                }
            }
            m_ActiveProfile = shortcutProfile;
        }

        public void ResetToDefault()
        {
            foreach (var entry in m_Entries)
            {
                m_ActiveProfile.Remove(entry);
                entry.ResetToDefault();
            }
            m_ActiveProfile.SaveToDisk();
            shortcutsModified?.Invoke(this);
        }

        public void ApplyActiveProfile()
        {
            m_ActiveProfile = new ShortcutProfile(k_UserProfileId);
            ApplyProfile(m_ActiveProfile);
            MigrateUserSpecifiedPrefKeys(
                EditorAssemblies.GetAllMethodsWithAttribute<FormerlyPrefKeyAsAttribute>(), m_Entries
            );
        }

        internal static void MigrateUserSpecifiedPrefKeys(
            IEnumerable<MethodInfo> methodsWithFormerlyPrefKeyAs, List<ShortcutEntry> entries
        )
        {
            foreach (var method in methodsWithFormerlyPrefKeyAs)
            {
                var shortcutAttr =
                    Attribute.GetCustomAttribute(method, typeof(ShortcutAttribute), true) as ShortcutAttribute;
                if (shortcutAttr == null)
                    continue;

                var entry = entries.Find(e => string.Equals(e.identifier.path, shortcutAttr.identifier));
                // ignore former PrefKeys if the shortcut profile has already loaded and applied an override
                if (entry == null || entry.overridden)
                    continue;

                var prefKeyAttr =
                    (FormerlyPrefKeyAsAttribute)Attribute.GetCustomAttribute(method, typeof(FormerlyPrefKeyAsAttribute));
                var prefKeyDefaultValue = $"{prefKeyAttr.name};{prefKeyAttr.defaultValue}";
                string name;
                Event keyboardEvent;
                string shortcut;
                if (!PrefKey.TryParseUniquePrefString(prefKeyDefaultValue, out name, out keyboardEvent, out shortcut))
                    continue;
                var prefKeyDefaultKeyCombination = KeyCombination.FromPrefKeyKeyboardEvent(keyboardEvent);

                // Parse current pref key value (falling back on default pref key value)
                if (!PrefKey.TryParseUniquePrefString(EditorPrefs.GetString(prefKeyAttr.name, prefKeyDefaultValue), out name, out keyboardEvent, out shortcut))
                    continue;
                var prefKeyCurrentKeyCombination = KeyCombination.FromPrefKeyKeyboardEvent(keyboardEvent);

                // only migrate pref keys that the user actually overwrote
                if (!prefKeyCurrentKeyCombination.Equals(prefKeyDefaultKeyCombination))
                    entry.SetOverride(new List<KeyCombination> { prefKeyCurrentKeyCombination });
            }
        }

        public void ModifyShortcutEntry(Identifier identifier, List<KeyCombination> combinationSequence)
        {
            var shortcutEntry = m_Entries.FirstOrDefault(e => e.identifier.Equals(identifier));
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
