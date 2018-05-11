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
        public static ShortcutController instance { get; }

        static ShortcutIntegration()
        {
            instance = new ShortcutController(new Discovery());
            instance.Initialize(instance.profileManager);
            EditorApplication.globalEventHandler += EventHandler;
        }

        static void EventHandler()
        {
            instance.contextManager.SetFocusedWindow(EditorWindow.focusedWindow);
            instance.HandleKeyEvent(Event.current);
        }
    }

    class ShortcutController
    {
        Trigger m_Trigger;

        public IShortcutProfileManager profileManager { get; }
        public IDirectory directory { get; private set; }

        public ContextManager contextManager = new ContextManager();

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
            if (contextManager.priorityContext != null && contextManager.priorityContext.active)
            {
                // For now the ReserveModifiersAttribute only works with priority context.
                // Making it working with any contex will require changing how Directory works
                var contextType = contextManager.priorityContext.GetType();
                var attributes = contextType.GetCustomAttributes(typeof(ReserveModifiersAttribute), true);

                foreach (var attribue in attributes)
                {
                    var modifier = (attribue as ReserveModifiersAttribute).modifier;
                    if ((modifier & ShortcutModifiers.Shift) == ShortcutModifiers.Shift)
                    {
                        evt.shift = false;
                    }
                    if ((modifier & ShortcutModifiers.Alt) == ShortcutModifiers.Alt)
                    {
                        evt.alt = false;
                    }
                    if ((modifier & ShortcutModifiers.ControlOrCommand) == ShortcutModifiers.ControlOrCommand)
                    {
                        evt.control = evt.command = false;
                    }
                }
            }

            HandleInTrigger(evt, contextManager);
        }

        internal virtual void HandleInTrigger(Event evt, IContextManager contextManager)
        {
            m_Trigger.HandleKeyEvent(evt, contextManager);
        }
    }
}
