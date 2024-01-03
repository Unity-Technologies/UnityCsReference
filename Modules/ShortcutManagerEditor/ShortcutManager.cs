// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEditor.ShortcutManagement
{
    public interface IShortcutManager
    {
        event Action<ActiveProfileChangedEventArgs> activeProfileChanged;
        string activeProfileId { get; set; }
        IEnumerable<string> GetAvailableProfileIds();
        bool IsProfileIdValid(string profileId);
        bool IsProfileReadOnly(string profileId);
        void CreateProfile(string profileId);
        void DeleteProfile(string profileId);
        void RenameProfile(string profileId, string newProfileId);

        event Action<ShortcutBindingChangedEventArgs> shortcutBindingChanged;
        IEnumerable<string> GetAvailableShortcutIds();
        ShortcutBinding GetShortcutBinding(string shortcutId);
        void RebindShortcut(string shortcutId, ShortcutBinding binding);
        void ClearShortcutOverride(string shortcutId);
        bool IsShortcutOverridden(string shortcutId);
    }

    public struct ActiveProfileChangedEventArgs
    {
        public string previousActiveProfileId { get; }
        public string currentActiveProfileId { get; }

        public ActiveProfileChangedEventArgs(string previousActiveProfileId, string currentActiveProfileId)
        {
            this.previousActiveProfileId = previousActiveProfileId;
            this.currentActiveProfileId = currentActiveProfileId;
        }
    }

    public struct ShortcutBindingChangedEventArgs
    {
        public string shortcutId { get; }
        public ShortcutBinding oldBinding { get; }
        public ShortcutBinding newBinding { get; }

        public ShortcutBindingChangedEventArgs(string shortcutId, ShortcutBinding oldBinding, ShortcutBinding newBinding)
        {
            this.shortcutId = shortcutId;
            this.oldBinding = oldBinding;
            this.newBinding = newBinding;
        }
    }

    public static class ShortcutManager
    {
        public const string defaultProfileId = "Default";

        public static IShortcutManager instance { get; } = new ShortcutManagerImplementation(ShortcutIntegration.instance.profileManager);

        public static void RegisterTag(string tag) => ShortcutIntegration.instance.contextManager.RegisterTag(tag);

        public static void RegisterTag(Enum e) => ShortcutIntegration.instance.contextManager.RegisterTag(e);

        public static void UnregisterTag(string tag) => ShortcutIntegration.instance.contextManager.UnregisterTag(tag);

        public static void UnregisterTag(Enum e) => ShortcutIntegration.instance.contextManager.UnregisterTag(e);

        public static void RegisterContext(IShortcutContext context) => ShortcutIntegration.instance.contextManager.RegisterToolContext(context);

        public static void UnregisterContext(IShortcutContext context) => ShortcutIntegration.instance.contextManager.DeregisterToolContext(context);
    }

    class ShortcutManagerImplementation : IShortcutManager
    {
        IShortcutProfileManager m_ShortcutProfileManager;

        internal ShortcutManagerImplementation(IShortcutProfileManager profileManager)
        {
            m_ShortcutProfileManager = profileManager;

            // TODO: Not sure if these events mean the same
            m_ShortcutProfileManager.activeProfileChanged += RaiseActiveProfileChanged;
            m_ShortcutProfileManager.shortcutBindingChanged += RaiseShortcutBindingChanged;
        }

        void RaiseActiveProfileChanged(IShortcutProfileManager shortcutProfileManager, ShortcutProfile oldActiveProfile, ShortcutProfile newActiveProfile)
        {
            var oldActiveProfileId = oldActiveProfile?.id ?? ShortcutManager.defaultProfileId;
            var newActiveProfileId = newActiveProfile?.id ?? ShortcutManager.defaultProfileId;
            var eventArgs = new ActiveProfileChangedEventArgs(oldActiveProfileId, newActiveProfileId);
            activeProfileChanged?.Invoke(eventArgs);
        }

        void RaiseShortcutBindingChanged(IShortcutProfileManager shortcutProfileManager, Identifier identifier, ShortcutBinding oldBinding, ShortcutBinding newBinding)
        {
            var eventArgs = new ShortcutBindingChangedEventArgs(identifier.path, oldBinding, newBinding);
            shortcutBindingChanged?.Invoke(eventArgs);
        }

        public event Action<ActiveProfileChangedEventArgs> activeProfileChanged;

        public string activeProfileId
        {
            get { return m_ShortcutProfileManager.activeProfile?.id ?? ShortcutManager.defaultProfileId; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value == ShortcutManager.defaultProfileId)
                    m_ShortcutProfileManager.activeProfile = null;
                else
                {
                    var profile = m_ShortcutProfileManager.GetProfileById(value);
                    if (profile == null)
                        throw new ArgumentException("Profile not available");

                    m_ShortcutProfileManager.activeProfile = profile;
                }
            }
        }

        public IEnumerable<string> GetAvailableProfileIds()
        {
            yield return ShortcutManager.defaultProfileId;
            foreach (var id in m_ShortcutProfileManager.GetProfiles().Select(profile => profile.id))
                yield return id;
        }

        public bool IsProfileIdValid(string profileId)
        {
            if (profileId == null)
            {
                throw new ArgumentNullException(nameof(profileId));
            }

            return !profileId.Equals(string.Empty) && profileId.Length <= 127 && profileId.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
        }

        public bool IsProfileReadOnly(string profileId)
        {
            if (profileId == null)
                throw new ArgumentNullException(nameof(profileId));

            if (profileId == ShortcutManager.defaultProfileId)
                return true;

            if (m_ShortcutProfileManager.GetProfileById(profileId) == null)
                throw new ArgumentException("Profile not available", nameof(profileId));

            return false;
        }

        public void CreateProfile(string profileId)
        {
            if (profileId == null)
                throw new ArgumentNullException(nameof(profileId));

            if (profileId == ShortcutManager.defaultProfileId)
                throw new ArgumentException("Duplicate profile id", nameof(profileId));

            switch (m_ShortcutProfileManager.CanCreateProfile(profileId))
            {
                case CanCreateProfileResult.Success:
                    m_ShortcutProfileManager.CreateProfile(profileId);
                    break;

                case CanCreateProfileResult.InvalidProfileId:
                    throw new ArgumentException("Invalid profile id", nameof(profileId));

                case CanCreateProfileResult.DuplicateProfileId:
                    throw new ArgumentException("Duplicate profile id", nameof(profileId));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void DeleteProfile(string profileId)
        {
            if (IsProfileReadOnly(profileId))
                throw new ArgumentException("Cannot delete read-only profile", nameof(profileId));

            var profile = m_ShortcutProfileManager.GetProfileById(profileId);

            switch (m_ShortcutProfileManager.CanDeleteProfile(profile))
            {
                case CanDeleteProfileResult.Success:
                    m_ShortcutProfileManager.DeleteProfile(profile);
                    break;

                case CanDeleteProfileResult.ProfileHasDependencies:
                    // Should not never happen through use of IShortcutManager API
                    throw new ArgumentException("Profile has dependencies", nameof(profileId));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RenameProfile(string profileId, string newProfileId)
        {
            if (IsProfileReadOnly(profileId))
                throw new ArgumentException("Cannot rename read-only profile", nameof(profileId));

            if (newProfileId == ShortcutManager.defaultProfileId)
                throw new ArgumentException("Duplicate profile id", nameof(newProfileId));

            var profile = m_ShortcutProfileManager.GetProfileById(profileId);

            switch (m_ShortcutProfileManager.CanRenameProfile(profile, newProfileId))
            {
                case CanRenameProfileResult.Success:
                    m_ShortcutProfileManager.RenameProfile(profile, newProfileId);
                    break;

                case CanRenameProfileResult.InvalidProfileId:
                    throw new ArgumentException("Profile not available", nameof(profileId));

                case CanRenameProfileResult.DuplicateProfileId:
                    throw new ArgumentException("Duplicate profile id", nameof(newProfileId));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public event Action<ShortcutBindingChangedEventArgs> shortcutBindingChanged;

        public IEnumerable<string> GetAvailableShortcutIds()
        {
            return m_ShortcutProfileManager.GetAllShortcuts().Select(entry => entry.identifier.path);
        }

        public ShortcutBinding GetShortcutBinding(string shortcutId)
        {
            if (shortcutId == null)
                throw new ArgumentNullException(nameof(shortcutId) + ":" + shortcutId);

            var shortcutEntries = m_ShortcutProfileManager.GetAllShortcuts();
            var shortcutEntry = shortcutEntries.FirstOrDefault(entry => entry.identifier.path == shortcutId);
            if (shortcutEntry == null)
            {
                if (MenuService.IsShortcutAvailableInMode(shortcutId))
                    throw new ArgumentException("Shortcut not available", nameof(shortcutId) + ": " + shortcutId);
                else
                    return ShortcutBinding.empty;
            }
            return new ShortcutBinding(shortcutEntry.combinations);
        }

        public void RebindShortcut(string shortcutId, ShortcutBinding binding)
        {
            if (shortcutId == null)
                throw new ArgumentNullException(nameof(shortcutId) + ":" + shortcutId);

            var shortcutEntries = m_ShortcutProfileManager.GetAllShortcuts();
            var shortcutEntry = shortcutEntries.FirstOrDefault(entry => entry.identifier.path == shortcutId);
            if (shortcutEntry == null)
                throw new ArgumentException("Shortcut not available", nameof(shortcutId) + ": " + shortcutId);

            if (IsProfileReadOnly(activeProfileId))
                throw new InvalidOperationException("Cannot rebind shortcut on read-only profile");

            m_ShortcutProfileManager.ModifyShortcutEntry(shortcutEntry.identifier, binding.keyCombinationSequence);
        }

        public void ClearShortcutOverride(string shortcutId)
        {
            if (shortcutId == null)
                throw new ArgumentNullException(nameof(shortcutId) + ": " + shortcutId);

            var shortcutEntries = m_ShortcutProfileManager.GetAllShortcuts();
            var shortcutEntry = shortcutEntries.FirstOrDefault(entry => entry.identifier.path == shortcutId);
            if (shortcutEntry == null)
                throw new ArgumentException("Shortcut not available", nameof(shortcutId) + ": " + shortcutId);

            if (IsProfileReadOnly(activeProfileId))
                throw new InvalidOperationException("Cannot clear shortcut override on read-only profile");

            m_ShortcutProfileManager.ClearShortcutOverride(shortcutEntry.identifier);
        }

        public bool IsShortcutOverridden(string shortcutId)
        {
            if (shortcutId == null)
                throw new ArgumentNullException(nameof(shortcutId) + ": " + shortcutId);

            var shortcutEntries = m_ShortcutProfileManager.GetAllShortcuts();
            var shortcutEntry = shortcutEntries.FirstOrDefault(entry => entry.identifier.path == shortcutId);
            if (shortcutEntry == null)
                throw new ArgumentException("Shortcut not available", nameof(shortcutId) + ": " + shortcutId);

            return shortcutEntry.overridden;
        }
    }
}
