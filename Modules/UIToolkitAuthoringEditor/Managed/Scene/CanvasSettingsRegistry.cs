// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Registry that maintains CanvasSettings objects alive across domain reloads and playmode transitions.
    /// </summary>
    [FilePath("Library/CanvasSettingsRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class CanvasSettingsRegistry : ScriptableSingleton<CanvasSettingsRegistry>
    {
        [Serializable]
        private class RegistryEntry
        {
            public EntityId key;
            public CanvasSettings settings;
        }

        [SerializeField]
        private List<RegistryEntry> m_Entries = new();

        private Dictionary<EntityId, CanvasSettings> m_SettingsCache;

        private Dictionary<EntityId, CanvasSettings> SettingsCache
        {
            get
            {
                if (m_SettingsCache == null)
                {
                    RebuildCache();
                }
                return m_SettingsCache;
            }
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void RebuildCache()
        {
            m_SettingsCache = new Dictionary<EntityId, CanvasSettings>();

            m_Entries.RemoveAll(entry => entry.settings == null);

            foreach (var entry in m_Entries)
            {
                if (entry.key.IsValid() && entry.settings != null)
                {
                    m_SettingsCache[entry.key] = entry.settings;
                }
            }
        }

        /// <summary>
        /// Registers CanvasSettings with the given key.
        /// </summary>
        /// <param name="key">Unique EntityId identifier for these settings</param>
        /// <param name="settings">The CanvasSettings to register</param>
        public void Register(EntityId key, CanvasSettings settings)
        {
            if (!key.IsValid())
            {
                Debug.LogError("Cannot register CanvasSettings with invalid EntityId key");
                return;
            }

            if (settings == null)
            {
                Debug.LogError("Cannot register null CanvasSettings");
                return;
            }

            if (SettingsCache.TryGetValue(key, out var existingSettings))
            {
                if (existingSettings == settings)
                {
                    // Already registered
                    return;
                }

                // Replace existing settings
                for (int i = 0; i < m_Entries.Count; i++)
                {
                    if (m_Entries[i].key == key)
                    {
                        m_Entries[i].settings = settings;
                        break;
                    }
                }
            }
            else
            {
                // Add new entry
                m_Entries.Add(new RegistryEntry { key = key, settings = settings });
            }

            SettingsCache[key] = settings;
            Save(false);
        }

        /// <summary>
        /// Unregisters CanvasSettings with the given key.
        /// </summary>
        /// <param name="key">The EntityId key to unregister</param>
        public void Unregister(EntityId key)
        {
            if (!key.IsValid())
                return;

            m_Entries.RemoveAll(entry => entry.key == key);
            SettingsCache.Remove(key);
            Save(false);
        }

        /// <summary>
        /// Attempts to retrieve registered CanvasSettings by key.
        /// </summary>
        /// <param name="key">The EntityId key to look up</param>
        /// <param name="settings">The found settings, or null if not found</param>
        /// <returns>True if the settings were found, false otherwise</returns>
        public bool TryGetSettings(EntityId key, out CanvasSettings settings)
        {
            if (!key.IsValid())
            {
                settings = null;
                return false;
            }

            return SettingsCache.TryGetValue(key, out settings) && settings != null;
        }

        /// <summary>
        /// Clears all registered settings from the registry.
        /// </summary>
        public void Clear()
        {
            m_Entries.Clear();
            m_SettingsCache?.Clear();
            Save(false);
        }

        /// <summary>
        /// Gets the number of registered settings.
        /// </summary>
        public int Count => m_Entries.Count;
    }
}
