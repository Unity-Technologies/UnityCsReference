// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class Discovery : IDiscovery
    {
        IEnumerable<IDiscoveryShortcutProvider> m_ShortcutProviders;
        IDiscoveryIdentifierConflictHandler m_IdentifierConflictHandler;
        IDiscoveryInvalidContextReporter m_InvalidContextReporter;

        public Discovery(IEnumerable<IDiscoveryShortcutProvider> shortcutProviders, IDiscoveryIdentifierConflictHandler identifierConflictHandler, IDiscoveryInvalidContextReporter invalidContextReporter)
        {
            m_ShortcutProviders = shortcutProviders;
            m_IdentifierConflictHandler = identifierConflictHandler;
            m_InvalidContextReporter = invalidContextReporter;
        }

        public Discovery(IEnumerable<IDiscoveryShortcutProvider> shortcutProviders, IDiscoveryIdentifierConflictHandler identifierConflictHandler)
            : this(shortcutProviders, identifierConflictHandler, null)
        {
        }

        public IEnumerable<ShortcutEntry> GetAllShortcuts()
        {
            var availableShortcuts = new List<ShortcutEntry>();
            var identifier2ShortcutEntry = new HashSet<Identifier>();

            foreach (var discoveryModule in m_ShortcutProviders)
            {
                var shortcuts = discoveryModule.GetDefinedShortcuts();
                foreach (var discoveredEntry in shortcuts)
                {
                    var shortcutEntry = discoveredEntry.GetShortcutEntry();
                    if (identifier2ShortcutEntry.Contains(shortcutEntry.identifier))
                    {
                        m_IdentifierConflictHandler.IdentifierConflictDetected(discoveredEntry);
                        continue;
                    }

                    if (!ValidateContext(discoveredEntry))
                    {
                        m_InvalidContextReporter?.ReportInvalidContext(discoveredEntry);
                        continue;
                    }

                    identifier2ShortcutEntry.Add(shortcutEntry.identifier);
                    availableShortcuts.Add(shortcutEntry);
                }
            }
            return availableShortcuts;
        }

        static bool ValidateContext(IShortcutEntryDiscoveryInfo discoveredEntry)
        {
            var shortcutEntry = discoveredEntry.GetShortcutEntry();
            var context = shortcutEntry.context;

            if (context == ContextManager.globalContextType)
                return true;

            var isEditorWindow = typeof(EditorWindow).IsAssignableFrom(context);
            var isIShortcutToolContext = typeof(IShortcutToolContext).IsAssignableFrom(context);

            if (isEditorWindow)
            {
                if (context != typeof(EditorWindow) && !isIShortcutToolContext)
                    return true;
            }
            else if (isIShortcutToolContext && context != typeof(IShortcutToolContext))
                return true;

            return false;
        }
    }
}
