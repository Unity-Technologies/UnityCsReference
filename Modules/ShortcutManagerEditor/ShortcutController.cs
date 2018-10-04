// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.ShortcutManagement
{
    interface ILastUsedProfileIdProvider
    {
        string lastUsedProfileId { get; set; }
    }

    [InitializeOnLoad]
    static class ShortcutIntegration
    {
        class LastUsedProfileIdProvider : ILastUsedProfileIdProvider
        {
            const string k_LastUsedProfileIdPrefKey = "ShortcutManagement_LastUsedShortcutProfileId";

            public string lastUsedProfileId
            {
                get
                {
                    var profileId = EditorPrefs.GetString(k_LastUsedProfileIdPrefKey);
                    return profileId == "" ? null : profileId;
                }

                set
                {
                    EditorPrefs.SetString(k_LastUsedProfileIdPrefKey, value ?? "");
                }
            }
        }

        public static ShortcutController instance { get; private set; }

        static ShortcutIntegration()
        {
            InitializeController();
            EditorApplication.globalEventHandler += EventHandler;
            EditorApplication.doPressedKeysTriggerAnyShortcut += HasAnyEntriesHandler;

            // Need to reinitialize after project load if we want menu items
            EditorApplication.projectWasLoaded += InitializeController;

            EditorApplication.focusChanged += OnFocusChanged;
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
                new ShortcutMenuItemDiscoveryProvider(),
            };
            var bindingValidator = new BindingValidator();
            var invalidContextReporter = new DiscoveryInvalidShortcutReporter();
            var discovery = new Discovery(shortcutProviders, bindingValidator, invalidContextReporter);
            instance = new ShortcutController(discovery, bindingValidator, new ShortcutProfileStore(), new LastUsedProfileIdProvider());
            instance.Initialize(instance.profileManager);
            instance.trigger.invokingAction += OnInvokingAction;
        }
    }

    class ShortcutController
    {
        const string k_DefaultProfileId = "UserProfile";

        Trigger m_Trigger;
        Directory m_Directory = new Directory(new ShortcutEntry[0]);

        public IShortcutProfileManager profileManager { get; }
        public IDirectory directory => m_Directory;
        public IBindingValidator bindingValidator { get; }
        public Trigger trigger => m_Trigger;

        ContextManager m_ContextManager = new ContextManager();

        public IContextManager contextManager => m_ContextManager;
        ILastUsedProfileIdProvider m_LastUsedProfileIdProvider;


        public ShortcutController(IDiscovery discovery, IBindingValidator bindingValidator, IShortcutProfileStore profileStore, ILastUsedProfileIdProvider lastUsedProfileIdProvider)
        {
            this.bindingValidator = bindingValidator;
            m_LastUsedProfileIdProvider = lastUsedProfileIdProvider;

            profileManager = new ShortcutProfileManager(discovery.GetAllShortcuts(), bindingValidator, profileStore);
            profileManager.shortcutsModified += Initialize;
            profileManager.activeProfileChanged += OnActiveProfileChanged;
            profileManager.ReloadProfiles();

            ActivateLastUsedProfile();
        }

        internal void Initialize(IShortcutProfileManager sender)
        {
            m_Directory.Initialize(profileManager.GetAllShortcuts());

            IConflictResolver conflictResolver;

            //TODO: disabled while we iterate on the window UI.
            bool enableConflictResolutionWorkflow = false;
            if (enableConflictResolutionWorkflow)
            {
                var conflictResolverView = new ConflictResolverView();
                conflictResolver = new ConflictResolver(profileManager, contextManager, conflictResolverView);
            }
            else
            {
                conflictResolver = new ConflictNotificationConsole();
            }

            m_Trigger = new Trigger(directory, conflictResolver);
        }

        void OnActiveProfileChanged(IShortcutProfileManager sender)
        {
            m_LastUsedProfileIdProvider.lastUsedProfileId = profileManager.activeProfile?.id;
        }

        void ActivateLastUsedProfile()
        {
            // Attempt to activate previously used profile
            var lastUsedProfileId = m_LastUsedProfileIdProvider.lastUsedProfileId ?? k_DefaultProfileId;
            var lastUsedProfile = profileManager.GetProfileById(lastUsedProfileId);
            if (lastUsedProfile != null)
                profileManager.activeProfile = lastUsedProfile;

            // The current UI requires a profile to be active
            if (profileManager.activeProfile == null)
            {
                var defaultProfile = profileManager.GetProfileById(k_DefaultProfileId);
                profileManager.activeProfile = defaultProfile ?? profileManager.CreateProfile(k_DefaultProfileId);
            }
        }

        internal bool HasAnyEntries(Event evt)
        {
            return m_Trigger.HasAnyEntries();
        }

        internal void HandleKeyEvent(Event evt)
        {
            m_Trigger.HandleKeyEvent(evt, contextManager);
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
    }
}
