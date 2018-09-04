// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.ShortcutManagement
{
    [InitializeOnLoad]
    class ShortcutIntegration
    {
        public static ShortcutController instance { get; private set; }

        static ShortcutIntegration()
        {
            InitializeController();
            EditorApplication.globalEventHandler += EventHandler;

            // Need to reinitialize after project load if we want menu items
            EditorApplication.projectWasLoaded += InitializeController;
        }

        static void EventHandler()
        {
            instance.contextManager.SetFocusedWindow(EditorWindow.focusedWindow);
            instance.HandleKeyEvent(Event.current);
        }

        static void InitializeController()
        {
            var shortcutProviders = new IDiscoveryShortcutProvider[]
            {
                new ShortcutAttributeDiscoveryProvider(),
                new ShortcutMenuItemDiscoveryProvider(),
            };
            var identifierConflictHandler = new DiscoveryIdentifierConflictHandler();
            var invalidContextReporter = new DiscoveryInvalidContextReporter();
            var discovery = new Discovery(shortcutProviders, identifierConflictHandler, invalidContextReporter);
            instance = new ShortcutController(discovery);
            instance.Initialize(instance.profileManager);
        }
    }

    class ShortcutController
    {
        Trigger m_Trigger;
        Directory m_Directory = new Directory(new ShortcutEntry[0]);

        public IShortcutProfileManager profileManager { get; }
        public IDirectory directory => m_Directory;

        ContextManager m_ContextManager = new ContextManager();

        public IContextManager contextManager => m_ContextManager;

        public ShortcutController(IDiscovery discovery)
        {
            profileManager = new ShortcutProfileManager(discovery.GetAllShortcuts());
            profileManager.shortcutsModified += Initialize;
            profileManager.ApplyActiveProfile();
        }

        internal void Initialize(IShortcutProfileManager sender)
        {
            m_Directory.Initialize(profileManager.GetAllShortcuts());
            m_Trigger = new Trigger(directory, new ConflictResolver());
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
