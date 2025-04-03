// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    enum CanCreateProfileResult
    {
        Success,
        InvalidProfileId,
        DuplicateProfileId,
        MissingParentProfile,
    }

    enum CanDeleteProfileResult
    {
        Success,
        ProfileNotFound,
        ProfileHasDependencies,
    }

    enum CanRenameProfileResult
    {
        Success,
        ProfileNotFound,
        InvalidProfileId,
        DuplicateProfileId,
    }

    interface IShortcutProfileManager
    {
        event Action<IShortcutProfileManager, Identifier, ShortcutBinding, ShortcutBinding> shortcutBindingChanged;
        event Action<IShortcutProfileManager, ShortcutProfile, ShortcutProfile> activeProfileChanged;
        event Action<IShortcutProfileManager> loadedProfilesChanged;

        void UpdateBaseProfile(IEnumerable<ShortcutEntry> baseProfile);

        ShortcutProfile activeProfile { get; set; }

        void ReloadProfiles();
        IEnumerable<ShortcutProfile> GetProfiles();
        ShortcutProfile GetProfileById(string profileId);
        CanCreateProfileResult CanCreateProfile(string profileId, ShortcutProfile parentProfile = null);
        ShortcutProfile CreateProfile(string profileId, ShortcutProfile parentProfile = null);
        CanDeleteProfileResult CanDeleteProfile(ShortcutProfile profile);
        void DeleteProfile(ShortcutProfile profile);
        CanRenameProfileResult CanRenameProfile(ShortcutProfile profile, string newProfileId);
        void RenameProfile(ShortcutProfile profile, string newProfileId);

        IEnumerable<ShortcutEntry> GetAllShortcuts(); // Preferences UI relies on this order being stable
        void ModifyShortcutEntry(Identifier identifier, IEnumerable<KeyCombination> combinationSequence);
        void ClearShortcutOverride(Identifier identifier);
        void ResetToDefault(); // TODO: Remove this when current UI is replaced. Set activeProfile to null instead.
        void ResetToDefault(ShortcutEntry entry);

        string GetProfileId(string path);
        void ImportProfile(string path);
        void ExportProfile(string path);
    }

    class ShortcutProfileManager : IShortcutProfileManager
    {
        List<ShortcutEntry> m_Entries;
        IShortcutProfileStore m_ProfileStore;
        IBindingValidator m_BindingValidator;

        ShortcutProfile m_ActiveProfile;
        Dictionary<string, ShortcutProfile> m_LoadedProfiles = new Dictionary<string, ShortcutProfile>();

        public event Action<IShortcutProfileManager, Identifier, ShortcutBinding, ShortcutBinding> shortcutBindingChanged;
        public event Action<IShortcutProfileManager, ShortcutProfile, ShortcutProfile> activeProfileChanged;
        public event Action<IShortcutProfileManager> loadedProfilesChanged;

        public ShortcutProfileManager(IEnumerable<ShortcutEntry> baseProfile, IBindingValidator bindingValidator, IShortcutProfileStore profileStore)
        {
            UpdateBaseProfile(baseProfile);
            m_ProfileStore = profileStore;
            m_BindingValidator = bindingValidator;
        }

        public void UpdateBaseProfile(IEnumerable<ShortcutEntry> baseProfile)
        {
            m_Entries = baseProfile.ToList();
            if (activeProfile != null)
                SwitchActiveProfileTo(activeProfile);
        }

        public ShortcutProfile activeProfile
        {
            get { return m_ActiveProfile; }
            set
            {
                var lastActiveProfile = m_ActiveProfile;

                if (value == null)
                {
                    if (m_ActiveProfile != null)
                    {
                        ResetShortcutEntries();
                        m_ActiveProfile = null;
                    }
                }
                else
                {
                    ShortcutProfile profile;
                    if (m_LoadedProfiles.TryGetValue(value.id, out profile))
                        SwitchActiveProfileTo(profile);
                    else
                        throw new ArgumentException("Profile not loaded", nameof(value));
                }

                if (m_ActiveProfile != lastActiveProfile)
                    activeProfileChanged?.Invoke(this, lastActiveProfile, m_ActiveProfile);
            }
        }

        public void ReloadProfiles()
        {
            InitializeLoadedProfilesDictionary();

            // Update active profile from store (it might be changed or deleted)
            if (activeProfile != null)
                activeProfile = GetProfileById(activeProfile.id);

            loadedProfilesChanged?.Invoke(this);
        }

        public IEnumerable<ShortcutProfile> GetProfiles()
        {
            return m_LoadedProfiles.Values.OrderBy(profile => profile.id);
        }

        public ShortcutProfile GetProfileById(string profileId)
        {
            ShortcutProfile profile;
            m_LoadedProfiles.TryGetValue(profileId, out profile);
            return profile;
        }

        public CanCreateProfileResult CanCreateProfile(string profileId, ShortcutProfile parentProfile = null)
        {
            // Profile id must be valid
            if (!m_ProfileStore.ValidateProfileId(profileId))
                return CanCreateProfileResult.InvalidProfileId;

            // Profile id must be unique (case-insensitive)
            if (m_LoadedProfiles.Any(pair => pair.Key.Equals(profileId, StringComparison.OrdinalIgnoreCase)))
                return CanCreateProfileResult.DuplicateProfileId;

            // Parent profile must exist, if given
            if (parentProfile != null && !m_LoadedProfiles.ContainsKey(parentProfile.id))
                return CanCreateProfileResult.MissingParentProfile;

            return CanCreateProfileResult.Success;
        }

        public ShortcutProfile CreateProfile(string profileId, ShortcutProfile parentProfile = null)
        {
            switch (CanCreateProfile(profileId, parentProfile))
            {
                case CanCreateProfileResult.Success:
                    var profile = new ShortcutProfile(profileId, parentProfile?.id ?? "");
                    m_LoadedProfiles.Add(profileId, profile);
                    ResolveProfileParentReference(profile);
                    SaveShortcutProfile(profile);
                    loadedProfilesChanged?.Invoke(this);
                    return profile;

                case CanCreateProfileResult.InvalidProfileId:
                    throw new ArgumentException("Invalid profile id", nameof(profileId));

                case CanCreateProfileResult.DuplicateProfileId:
                    throw new ArgumentException("Duplicate profile id", nameof(profileId));

                case CanCreateProfileResult.MissingParentProfile:
                    throw new ArgumentException("Missing parent profile", nameof(parentProfile));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public CanDeleteProfileResult CanDeleteProfile(ShortcutProfile profile)
        {
            // Profile must exist
            if (!m_LoadedProfiles.ContainsKey(profile.id))
                return CanDeleteProfileResult.ProfileNotFound;

            // Profile cannot have any dependent profiles
            if (m_LoadedProfiles.Values.Any(p => p.parentId == profile.id))
                return CanDeleteProfileResult.ProfileHasDependencies;

            return CanDeleteProfileResult.Success;
        }

        public void DeleteProfile(ShortcutProfile profile)
        {
            switch (CanDeleteProfile(profile))
            {
                case CanDeleteProfileResult.Success:
                    profile = m_LoadedProfiles[profile.id];
                    if (activeProfile == profile)
                        activeProfile = null;
                    m_LoadedProfiles.Remove(profile.id);
                    m_ProfileStore.DeleteShortcutProfile(profile.id);
                    loadedProfilesChanged?.Invoke(this);
                    break;

                case CanDeleteProfileResult.ProfileNotFound:
                    throw new ArgumentException("Profile not found", nameof(profile));

                case CanDeleteProfileResult.ProfileHasDependencies:
                    throw new ArgumentException("Profile has dependencies", nameof(profile));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public CanRenameProfileResult CanRenameProfile(ShortcutProfile profile, string newProfileId)
        {
            // Profile must exist
            if (!m_LoadedProfiles.ContainsKey(profile.id))
                return CanRenameProfileResult.ProfileNotFound;

            // Profile id must be valid
            if (!m_ProfileStore.ValidateProfileId(newProfileId))
                return CanRenameProfileResult.InvalidProfileId;

            // Profile id must be unique
            if (m_LoadedProfiles.ContainsKey(newProfileId))
                return CanRenameProfileResult.DuplicateProfileId;

            return CanRenameProfileResult.Success;
        }

        public void RenameProfile(ShortcutProfile profile, string newProfileId)
        {
            switch (CanRenameProfile(profile, newProfileId))
            {
                case CanRenameProfileResult.Success:
                {
                    var newProfile = new ShortcutProfile(newProfileId, profile.entries, profile.parentId);
                    m_LoadedProfiles.Add(newProfile.id, newProfile);
                    SaveShortcutProfile(newProfile);
                    foreach (var childProfile in m_LoadedProfiles.Values.Where(childProfile => childProfile.parent == profile))
                    {
                        childProfile.parent = newProfile;
                        childProfile.parentId = newProfile.id;
                        SaveShortcutProfile(childProfile);
                    }

                    if (activeProfile == profile)
                        activeProfile = newProfile;

                    try
                    {
                        DeleteProfile(profile);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Error removing old shortcut profile: {exception}");
                    }
                    break;
                }

                case CanRenameProfileResult.ProfileNotFound:
                    throw new ArgumentException("Profile not found", nameof(profile));

                case CanRenameProfileResult.InvalidProfileId:
                    throw new ArgumentException("Invalid profile id", nameof(newProfileId));

                case CanRenameProfileResult.DuplicateProfileId:
                    throw new ArgumentException("Duplicate profile id", nameof(newProfileId));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerable<ShortcutEntry> GetAllShortcuts()
        {
            return m_Entries;
        }

        public void ModifyShortcutEntry(Identifier identifier, IEnumerable<KeyCombination> combinationSequence)
        {
            if (activeProfile == null)
                throw new InvalidOperationException("No active profile");

            if (!m_BindingValidator.IsCombinationValid(combinationSequence, out string invalidBindingMessage))
            {
                Debug.LogError(invalidBindingMessage);
                return;
            }

            var shortcutEntry = m_Entries.FirstOrDefault(e => e.identifier.Equals(identifier));
            var wasOverridden = shortcutEntry.overridden;
            var oldBinding = new ShortcutBinding(shortcutEntry.combinations);

            shortcutEntry.SetOverride(combinationSequence);

            if (!m_BindingValidator.IsBindingValid(shortcutEntry, out invalidBindingMessage))
            {
                if(wasOverridden)
                    shortcutEntry.SetOverride(oldBinding.keyCombinationSequence);
                else
                    shortcutEntry.ResetToDefault();

                Debug.LogError(invalidBindingMessage);
                shortcutBindingChanged?.Invoke(this, identifier, oldBinding, oldBinding);
                return;
            }

            SerializableShortcutEntry profileEntry = null;
            foreach (var activeProfileEntry in m_ActiveProfile.entries)
            {
                if (activeProfileEntry.identifier.Equals(identifier))
                {
                    profileEntry = activeProfileEntry;
                    oldBinding = new ShortcutBinding(profileEntry.combinations);
                    profileEntry.combinations = new List<KeyCombination>(combinationSequence);
                    break;
                }
            }

            if (profileEntry == null)
            {
                m_ActiveProfile.Add(shortcutEntry);
            }

            SaveShortcutProfile(m_ActiveProfile);

            var newBinding = new ShortcutBinding(combinationSequence);
            shortcutBindingChanged?.Invoke(this, identifier, oldBinding, newBinding);
        }

        public void ClearShortcutOverride(Identifier identifier)
        {
            if (activeProfile == null)
                throw new InvalidOperationException("No active profile");

            activeProfile.Remove(identifier);

            // Persist changes to store and reload profile
            SaveShortcutProfile(activeProfile);
            SwitchActiveProfileTo(activeProfile);
        }

        // TODO: Make this private when current UI is replaced. Set activeProfile to null instead.
        public void ResetToDefault()
        {
            ResetShortcutEntries();
            ResetActiveProfile();
            SaveShortcutProfile(m_ActiveProfile);
        }

        public void ResetToDefault(ShortcutEntry entry)
        {
            var oldBinding = new ShortcutBinding(entry.combinations);
            entry.ResetToDefault();
            activeProfile.Remove(entry.identifier);
            SaveShortcutProfile(activeProfile);
            var newBinding = new ShortcutBinding(entry.combinations);
            shortcutBindingChanged?.Invoke(this, entry.identifier, oldBinding, newBinding);
        }

        void ResetShortcutEntries()
        {
            foreach (var entry in m_Entries)
            {
                entry.ResetToDefault();
            }
        }

        void ResetActiveProfile()
        {
            var newProfile = new ShortcutProfile(m_ActiveProfile.id, m_ActiveProfile.parentId)
            {
                parent = m_ActiveProfile.parent
            };

            m_LoadedProfiles.Remove(m_ActiveProfile.id);
            m_LoadedProfiles.Add(newProfile.id, newProfile);
            activeProfile = newProfile;

            SaveShortcutProfile(m_ActiveProfile);
        }

        void SwitchActiveProfileTo(ShortcutProfile shortcutProfile)
        {
            ResetShortcutEntries();
            var profileParents = GetAncestorProfiles(shortcutProfile);

            if (profileParents != null)
            {
                for (int i = profileParents.Count - 1; i >= 0; --i)
                {
                    ApplySingleProfile(profileParents[i]);
                }
            }

            ApplySingleProfile(shortcutProfile);
        }

        static List<ShortcutProfile> GetAncestorProfiles(ShortcutProfile profile)
        {
            var ancestors = new List<ShortcutProfile>();
            profile = profile.parent;
            while (profile != null)
            {
                ancestors.Add(profile);
                profile = profile.parent;
            }
            return ancestors;
        }

        void ApplySingleProfile(ShortcutProfile shortcutProfile)
        {
            foreach (var shortcutOverride in shortcutProfile.entries)
            {
                var entry = m_Entries.FirstOrDefault(e => e.identifier.Equals(shortcutOverride.identifier));
                if (entry != null)
                {
                    if (!m_BindingValidator.IsCombinationValid(shortcutOverride.combinations, out string invalidBindingMessage))
                    {
                        var nameSnippet = entry.displayName == entry.identifier.path
                            ? $"\"{entry.displayName}\""
                            : $"with ID \"{entry.identifier.path}\" and name \"{entry.displayName}\"";
                        Debug.LogWarning($"Ignoring shortcut {nameSnippet} in profile \"{shortcutProfile.id}\" with invalid binding.\n{invalidBindingMessage}.");
                        continue;
                    }

                    entry.ApplyOverride(shortcutOverride);

                    if (!m_BindingValidator.IsBindingValid(entry, out invalidBindingMessage))
                    {
                        var nameSnippet = entry.displayName == entry.identifier.path
                            ? $"\"{entry.displayName}\""
                            : $"with ID \"{entry.identifier.path}\" and name \"{entry.displayName}\"";
                        Debug.LogWarning($"Cannot apply override to shortcut {nameSnippet} in profile \"{shortcutProfile.id}\" with invalid binding.\n{invalidBindingMessage}.");
                        entry.ResetToDefault();
                        continue;
                    }
                }
            }
            m_ActiveProfile = shortcutProfile;
        }

        void InitializeLoadedProfilesDictionary()
        {
            var foundShortcutProfiles = LoadProfiles();
            m_LoadedProfiles.Clear();

            foreach (var profile in foundShortcutProfiles)
            {
                if (m_LoadedProfiles.ContainsKey(profile.id))
                {
                    Debug.LogWarning($"Found multiple shortcut profiles that share the id '{profile.id}'!");
                }
                else
                {
                    m_LoadedProfiles.Add(profile.id, profile);
                }
            }

            ResolveProfileParentReferences();
        }

        void ResolveProfileParentReferences()
        {
            foreach (var profile in m_LoadedProfiles.Values)
            {
                ResolveProfileParentReference(profile);
            }

            BreakProfileParentCyclicDependencies();
        }

        void ResolveProfileParentReference(ShortcutProfile profile)
        {
            if (string.IsNullOrEmpty(profile.parentId))
                return;

            if (m_LoadedProfiles.ContainsKey(profile.parentId))
                profile.parent = m_LoadedProfiles[profile.parentId];
            else
            {
                Debug.LogWarning($"Shortcut profile with id '{profile.id}' references unknown parent profile with id '{profile.parentId}'. Breaking parent link.");
                profile.BreakParentLink();
            }
        }

        void BreakProfileParentCyclicDependencies()
        {
            foreach (var profile in m_LoadedProfiles.Values)
            {
                BreakProfileParentCyclicDependency(profile);
            }
        }

        static void BreakProfileParentCyclicDependency(ShortcutProfile profile)
        {
            if (profile.parent == null)
                return;

            var seenProfiles = new List<ShortcutProfile>();

            do
            {
                seenProfiles.Add(profile);

                if (seenProfiles.Contains(profile.parent))
                {
                    Debug.LogWarning($"Cyclic dependency between shortcut profiles found! Breaking parent link at '{profile.id}'.");
                    profile.BreakParentLink();
                    return;
                }

                profile = profile.parent;
            }
            while (profile.parent != null);
        }

        void SaveShortcutProfile(ShortcutProfile profile)
        {
            m_ProfileStore.SaveShortcutProfileJson(profile.id, JsonUtility.ToJson(profile));
        }

        ShortcutProfile LoadProfile(string id)
        {
            if (m_ProfileStore.ProfileExists(id))
            {
                ShortcutProfile tmpProfile = new ShortcutProfile(id);
                LoadAndApplyJsonFile(id, tmpProfile);
                return tmpProfile;
            }
            throw new FileNotFoundException(string.Format("'{0}.shortcut' does not exist!", id));
        }

        List<ShortcutProfile> LoadProfiles()
        {
            var shortcutIds = m_ProfileStore.GetAllProfileIds();
            var shortcutProfiles = new List<ShortcutProfile>();
            foreach (var id in shortcutIds)
            {
                shortcutProfiles.Add(LoadProfile(id));
            }
            return shortcutProfiles;
        }

        void LoadAndApplyJsonFile(string id, ShortcutProfile instance)
        {
            if (!m_ProfileStore.ProfileExists(id))
            {
                return;
            }

            var json = m_ProfileStore.LoadShortcutProfileJson(id);
            JsonUtility.FromJsonOverwrite(json, instance);
            if (id != instance.id)
            {
                Debug.LogWarning(string.Format("The identifier '{0}' doesn't match the filename of '{1}.shortcut'!", id, instance.id));
            }
        }

        public string GetProfileId(string path)
        {
            var profile = new ShortcutProfile();
            var profileJson = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(profileJson, profile);
            return profile.id;
        }

        public void ImportProfile(string path)
        {
            var profile = new ShortcutProfile();
            var profileJson = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(profileJson, profile);
            m_ProfileStore.SaveShortcutProfileJson(profile.id, profileJson);
            m_LoadedProfiles[profile.id] = profile;
        }

        public void ExportProfile(string path) => File.WriteAllText(path, JsonUtility.ToJson(activeProfile, true));
    }
}
