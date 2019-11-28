// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;
using Attribute = System.Attribute;
using Event = UnityEngine.Event;

namespace UnityEditor.ShortcutManagement
{
    interface ILastUsedProfileIdProvider
    {
        string lastUsedProfileId { get; set; }
    }

    interface IAvailableShortcutsChangedNotifier
    {
        event Action availableShortcutsChanged;
    }

    [InitializeOnLoad]
    static class ShortcutIntegration
    {
        class LastUsedProfileIdProvider : ILastUsedProfileIdProvider
        {
            static string lastUsedProfileIdEditorPrefKey => $"ShortcutManagement_LastUsedShortcutProfileId_{ModeService.currentId}";

            public string lastUsedProfileId
            {
                get
                {
                    var profileId = EditorPrefs.GetString(lastUsedProfileIdEditorPrefKey);
                    return profileId == "" ? null : profileId;
                }

                set
                {
                    EditorPrefs.SetString(lastUsedProfileIdEditorPrefKey, value ?? "");
                }
            }
        }

        private static ShortcutController s_Instance;
        public static ShortcutController instance
        {
            get
            {
                EnsureShortcutControllerCreated();
                return s_Instance;
            }
        }

        private static bool s_Enabled;
        internal static bool enabled
        {
            get
            {
                return s_Enabled;
            }
            set
            {
                if (value == s_Enabled) return;

                if (value)
                {
                    EditorApplication.globalEventHandler += EventHandler;
                }
                else
                {
                    EditorApplication.globalEventHandler -= EventHandler;
                }

                s_Enabled = value;
            }
        }

        [RequiredByNativeCode]
        internal static void SetShortcutIntegrationEnabled(bool enable)
        {
            enabled = enable;
        }

        static ShortcutIntegration()
        {
            // There is cases where the ShortcutIntegration was not requested even after the project was initialized, such as running tests.
            EditorApplication.delayCall += EnsureShortcutControllerCreated;
        }

        static void EnsureShortcutControllerCreated()
        {
            if (s_Instance == null)
                InitializeController();
            Debug.Assert(s_Instance != null);
        }

        static bool HasAnyEntriesHandler()
        {
            return instance.HasAnyEntries(Event.current);
        }

        static void EventHandler()
        {
            instance.contextManager.SetFocusedWindow(EditorWindow.focusedWindow);
            instance.HandleKeyEvent(Event.current);
        }

        static void OnInvokingAction(ShortcutEntry shortcutEntry, ShortcutArguments shortcutArguments)
        {
            // Separate shortcut actions into different undo groups
            Undo.IncrementCurrentGroup();
        }

        static void OnFocusChanged(bool isFocused)
        {
            instance.trigger.ResetActiveClutches();
        }

        static void InitializeController()
        {
            var shortcutProviders = new IDiscoveryShortcutProvider[]
            {
                new ShortcutAttributeDiscoveryProvider(),
                new ShortcutMenuItemDiscoveryProvider()
            };
            var bindingValidator = new BindingValidator();
            var invalidContextReporter = new DiscoveryInvalidShortcutReporter();
            var discovery = new Discovery(shortcutProviders, bindingValidator, invalidContextReporter);
            IContextManager contextManager = new ContextManager();

            s_Instance = new ShortcutController(discovery, contextManager, bindingValidator, new ShortcutProfileStore(), new LastUsedProfileIdProvider());
            s_Instance.trigger.invokingAction += OnInvokingAction;

            enabled = true;
            EditorApplication.doPressedKeysTriggerAnyShortcut += HasAnyEntriesHandler;
            EditorApplication.focusChanged += OnFocusChanged;
        }
    }

    class ShortcutController : IAvailableShortcutsChangedNotifier
    {
        const string k_MigratedProfileId = "UserProfile";
        const string k_ProfileMigratedEditorPrefKey = "ShortcutManager_ProfileMigrated";

        Directory m_Directory;
        IDiscovery m_Discovery;

        public IShortcutProfileManager profileManager { get; }
        public IDirectory directory => m_Directory;
        public IBindingValidator bindingValidator { get; }
        public Trigger trigger { get; }
        public IContextManager contextManager { get; }
        ILastUsedProfileIdProvider m_LastUsedProfileIdProvider;

        public event Action availableShortcutsChanged;

        public ShortcutController(IDiscovery discovery, IContextManager contextManager, IBindingValidator bindingValidator, IShortcutProfileStore profileStore, ILastUsedProfileIdProvider lastUsedProfileIdProvider)
        {
            m_Discovery = discovery;
            this.bindingValidator = bindingValidator;
            m_LastUsedProfileIdProvider = lastUsedProfileIdProvider;

            profileManager = new ShortcutProfileManager(m_Discovery.GetAllShortcuts(), bindingValidator, profileStore);
            profileManager.shortcutBindingChanged += OnShortcutBindingChanged;
            profileManager.activeProfileChanged += OnActiveProfileChanged;
            profileManager.ReloadProfiles();

            var conflictResolverView = new ConflictResolverView();
            var conflictResolver = new ConflictResolver(profileManager, contextManager, conflictResolverView);

            this.contextManager = contextManager;

            m_Directory = new Directory(profileManager.GetAllShortcuts());
            trigger = new Trigger(m_Directory, conflictResolver);

            ActivateLastUsedProfile();
            MigrateUserSpecifiedPrefKeys();

            ModeService.modeChanged += HandleModeChanged;
        }

        internal void RebuildShortcuts()
        {
            profileManager.UpdateBaseProfile(m_Discovery.GetAllShortcuts());
            Initialize();
        }

        void Initialize()
        {
            m_Directory.Initialize(profileManager.GetAllShortcuts());
            availableShortcutsChanged?.Invoke();
        }

        void HandleModeChanged(ModeService.ModeChangedArgs args)
        {
            // Install profile specific to the current mode.
            var shortcutProfiles = ModeService.GetModeDataSection(args.nextIndex, ModeDescriptor.ShortcutsKey) as System.Collections.IList;
            if (shortcutProfiles == null || shortcutProfiles.Count == 0)
                return;

            string defaultMode = null;
            try
            {
                var shortcutProfilePath = ShortcutProfileStore.GetShortcutFolderPath();
                if (!System.IO.Directory.Exists(shortcutProfilePath))
                    System.IO.Directory.CreateDirectory(shortcutProfilePath);
                for (var i = 0; i < shortcutProfiles.Count; ++i)
                {
                    var srcProfilePath = shortcutProfiles[i] as string;
                    if (String.IsNullOrEmpty(srcProfilePath) || !File.Exists(srcProfilePath))
                        continue;
                    var baseName = Path.GetFileName(srcProfilePath);
                    var dstPath = Path.Combine(shortcutProfilePath, baseName);
                    if (!File.Exists(dstPath))
                    {
                        File.Copy(srcProfilePath, dstPath);
                    }

                    if (i == 0)
                    {
                        // Assume first item is the default mode if needed.
                        defaultMode = Path.GetFileNameWithoutExtension(srcProfilePath);
                    }
                }
            }
            catch (Exception)
            {
                Debug.LogError("Error while installing new profile for mode: " + ModeService.currentId);
            }

            // Reload profiles will unset the lastUsedProfile so make a copy of it:
            var lastUsedProfileId = m_LastUsedProfileIdProvider.lastUsedProfileId ?? defaultMode;
            if (!String.IsNullOrEmpty(lastUsedProfileId))
            {
                profileManager.ReloadProfiles();
                LoadProfileById(lastUsedProfileId);
            }
        }

        void OnShortcutBindingChanged(IShortcutProfileManager sender, Identifier identifier, ShortcutBinding oldBinding, ShortcutBinding newBinding)
        {
            Initialize();
        }

        void OnActiveProfileChanged(IShortcutProfileManager sender, ShortcutProfile oldActiveProfile, ShortcutProfile newActiveProfile)
        {
            m_LastUsedProfileIdProvider.lastUsedProfileId = profileManager.activeProfile?.id;
            Initialize();
        }

        internal bool HasAnyEntries(Event evt)
        {
            return trigger.HasAnyEntries();
        }

        internal void HandleKeyEvent(Event evt)
        {
            trigger.HandleKeyEvent(evt, contextManager);
        }

        internal string GetKeyCombinationFor(string shortcutId)
        {
            var shortcutEntry = directory.FindShortcutEntry(shortcutId);
            if (shortcutEntry != null)
            {
                return KeyCombination.SequenceToString(shortcutEntry.combinations);
            }
            throw new System.ArgumentException(shortcutId + " is not defined.", nameof(shortcutId));
        }

        void ActivateLastUsedProfile()
        {
            LoadProfileById(m_LastUsedProfileIdProvider.lastUsedProfileId);
        }

        void LoadProfileById(string profileId)
        {
            if (profileId == null)
                return;

            var lastUsedProfile = profileManager.GetProfileById(profileId);
            if (lastUsedProfile != null)
                profileManager.activeProfile = lastUsedProfile;
        }

        static bool TryParseUniquePrefKeyString(string prefString, out string name, out Event keyboardEvent, out string shortcut)
        {
            int i = prefString.IndexOf(";", StringComparison.Ordinal);
            if (i < 0)
            {
                name = null;
                keyboardEvent = null;
                shortcut = null;
                return false;
            }
            name = prefString.Substring(0, i);
            shortcut = prefString.Substring(i + 1);
            keyboardEvent = Event.KeyboardEvent(shortcut);
            return true;
        }

        void MigrateUserSpecifiedPrefKeys()
        {
            // If migration already happened then don't do anything
            if (EditorPrefs.GetBool(k_ProfileMigratedEditorPrefKey, false))
                return;

            EditorPrefs.SetBool(k_ProfileMigratedEditorPrefKey, true);

            // Find shortcut entries that might need to be migrated
            var allShortcuts = new List<ShortcutEntry>();
            directory.GetAllShortcuts(allShortcuts);

            // Find existing or create migrated profile and make it active so we can amend it
            var originalActiveProfile = profileManager.activeProfile;
            var migratedProfile = profileManager.GetProfileById(k_MigratedProfileId);
            var migratedProfileAlreadyExisted = migratedProfile != null;
            if (!migratedProfileAlreadyExisted)
                migratedProfile = profileManager.CreateProfile(k_MigratedProfileId);
            profileManager.activeProfile = migratedProfile;

            var migratedProfileModified = false;

            var tempKeyCombinations = new KeyCombination[1];
            var methodsWithFormerlyPrefKeyAs = EditorAssemblies.GetAllMethodsWithAttribute<FormerlyPrefKeyAsAttribute>();
            foreach (var method in methodsWithFormerlyPrefKeyAs)
            {
                var shortcutAttr = Attribute.GetCustomAttribute(method, typeof(ShortcutAttribute), true) as ShortcutAttribute;
                if (shortcutAttr == null)
                    continue;

                var entry = allShortcuts.Find(e => string.Equals(e.identifier.path, shortcutAttr.identifier));
                if (entry == null)
                    continue;

                // Ignore former PrefKey if it is overriden in existing migrated profile
                if (entry.overridden)
                    continue;

                // Parse default pref key value from FormerlyPrefKeyAs attribute
                var prefKeyAttr = (FormerlyPrefKeyAsAttribute)Attribute.GetCustomAttribute(method, typeof(FormerlyPrefKeyAsAttribute));
                var editorPrefDefaultValue = $"{prefKeyAttr.name};{prefKeyAttr.defaultValue}";
                string name;
                Event keyboardEvent;
                string shortcut;
                if (!TryParseUniquePrefKeyString(editorPrefDefaultValue, out name, out keyboardEvent, out shortcut))
                    continue;
                var prefKeyDefaultKeyCombination = KeyCombination.FromPrefKeyKeyboardEvent(keyboardEvent);

                // Parse current pref key value (falling back on default pref key value)
                if (!TryParseUniquePrefKeyString(EditorPrefs.GetString(prefKeyAttr.name, editorPrefDefaultValue), out name, out keyboardEvent, out shortcut))
                    continue;
                var prefKeyCurrentKeyCombination = KeyCombination.FromPrefKeyKeyboardEvent(keyboardEvent);

                // Only migrate pref keys that the user actually overwrote
                if (prefKeyCurrentKeyCombination.Equals(prefKeyDefaultKeyCombination))
                    continue;

                string invalidBindingMessage;
                tempKeyCombinations[0] = prefKeyCurrentKeyCombination;
                if (!bindingValidator.IsBindingValid(tempKeyCombinations, out invalidBindingMessage))
                {
                    Debug.LogWarning($"Could not migrate existing binding for shortcut \"{entry.identifier.path}\" with invalid binding.\n{invalidBindingMessage}.");
                    continue;
                }

                profileManager.ModifyShortcutEntry(entry.identifier, new List<KeyCombination> { prefKeyCurrentKeyCombination });

                migratedProfileModified = true;
            }

            // Delete migrated profile if it was created and not modified
            if (!migratedProfileAlreadyExisted && !migratedProfileModified)
                profileManager.DeleteProfile(migratedProfile);

            // Restore original active profile unless last loaded profile was null and the migrated profile was created
            if (originalActiveProfile != null || migratedProfileAlreadyExisted)
                profileManager.activeProfile = originalActiveProfile;
        }
    }
}
