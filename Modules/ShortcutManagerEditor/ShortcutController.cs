// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    [InitializeOnLoad]
    class ShortcutIntegration
    {
        public static ShortcutController instance { get; }

        static ShortcutIntegration()
        {
            instance = new ShortcutController(new Discovery());
            instance.Initialize(instance.profileManager);
            EditorApplication.globalEventHandler += EventHandler;
        }

        static void EventHandler()
        {
            instance.HandleKeyEventForContext(Event.current, EditorWindow.focusedWindow);
        }
    }

    class ShortcutController
    {
        Trigger m_Trigger;

        public IShortcutProfileManager profileManager { get; }
        public IDirectory directory { get; private set; }

        public static IShortcutPriorityContext priorityContext
        {
            get { return ShortcutIntegration.instance.m_Trigger.priorityContext; }
            set { ShortcutIntegration.instance.m_Trigger.priorityContext = value; }
        }

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

        internal void HandleKeyEventForContext(Event evt, object context)
        {
            m_Trigger.HandleKeyEventForContext(evt, context);
        }
    }
}
