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
    /// Registry that maintains PanelOwner objects alive across domain reloads and playmode transitions.
    /// </summary>
    [FilePath("Library/PanelOwnerRegistry.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class PanelOwnerRegistry : ScriptableSingleton<PanelOwnerRegistry>
    {
        [Serializable]
        private class RegistryEntry
        {
            public EntityId key;
            public PanelElement.PanelOwner owner;
        }

        [SerializeField]
        private List<RegistryEntry> m_Entries = new();

        private Dictionary<EntityId, PanelElement.PanelOwner> m_OwnerCache;

        private Dictionary<EntityId, PanelElement.PanelOwner> OwnerCache
        {
            get
            {
                if (m_OwnerCache == null)
                {
                    RebuildCache();
                }
                return m_OwnerCache;
            }
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void RebuildCache()
        {
            m_OwnerCache = new Dictionary<EntityId, PanelElement.PanelOwner>();

            m_Entries.RemoveAll(entry => entry.owner == null);

            foreach (var entry in m_Entries)
            {
                if (entry.key.IsValid() && entry.owner != null)
                {
                    m_OwnerCache[entry.key] = entry.owner;
                }
            }
        }

        /// <summary>
        /// Registers a PanelOwner with the given key.
        /// </summary>
        /// <param name="key">Unique EntityId identifier for this owner</param>
        /// <param name="owner">The PanelOwner to register</param>
        public void Register(EntityId key, PanelElement.PanelOwner owner)
        {
            if (!key.IsValid())
            {
                Debug.LogError("Cannot register PanelOwner with invalid EntityId key");
                return;
            }

            if (owner == null)
            {
                Debug.LogError("Cannot register null PanelOwner");
                return;
            }

            if (OwnerCache.TryGetValue(key, out var existingOwner))
            {
                if (existingOwner == owner)
                {
                    // Already registered
                    return;
                }

                // Replace existing owner
                for (int i = 0; i < m_Entries.Count; i++)
                {
                    if (m_Entries[i].key == key)
                    {
                        m_Entries[i].owner = owner;
                        break;
                    }
                }
            }
            else
            {
                // Add new entry
                m_Entries.Add(new RegistryEntry { key = key, owner = owner });
            }

            OwnerCache[key] = owner;
            Save(false);
        }

        /// <summary>
        /// Unregisters a PanelOwner with the given key.
        /// </summary>
        /// <param name="key">The EntityId key to unregister</param>
        public void Unregister(EntityId key)
        {
            if (!key.IsValid())
                return;

            m_Entries.RemoveAll(entry => entry.key == key);
            OwnerCache.Remove(key);
            Save(false);
        }

        /// <summary>
        /// Attempts to retrieve a registered PanelOwner by key.
        /// </summary>
        /// <param name="key">The EntityId key to look up</param>
        /// <param name="owner">The found owner, or null if not found</param>
        /// <returns>True if the owner was found, false otherwise</returns>
        public bool TryGetOwner(EntityId key, out PanelElement.PanelOwner owner)
        {
            if (!key.IsValid())
            {
                owner = null;
                return false;
            }

            return OwnerCache.TryGetValue(key, out owner) && owner != null;
        }

        /// <summary>
        /// Clears all registered owners from the registry.
        /// </summary>
        public void Clear()
        {
            m_Entries.Clear();
            m_OwnerCache?.Clear();
            Save(false);
        }

        /// <summary>
        /// Gets the number of registered owners.
        /// </summary>
        public int Count => m_Entries.Count;
    }
}
