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
            var discoveryIdentifierConflictHandler = new DiscoveryIdentifierConflictHandler();
            var discovery = new Discovery(new IDiscoveryShortcutProvider[] { new ShortcutAttributeDiscoveryProvider(), new ShortcutMenuItemDiscoveryProvider() }, discoveryIdentifierConflictHandler);
            instance = new ShortcutController(discovery);
            instance.Initialize(instance.profileManager);
        }
    }

    class ShortcutController
    {
        Trigger m_Trigger;

        public IShortcutProfileManager profileManager { get; }
        public IDirectory directory { get; private set; }

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
            directory = new Directory(profileManager.GetAllShortcuts());
            m_Trigger = new Trigger(directory, new ConflictResolver());
        }

        internal void HandleKeyEvent(Event evt)
        {
            m_Trigger.HandleKeyEvent(evt, contextManager);
        }
    }
}
