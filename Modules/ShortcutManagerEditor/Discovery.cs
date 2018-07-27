// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.ShortcutManagement
{
    class Discovery : IDiscovery
    {
        IEnumerable<IDiscoveryShortcutProvider> m_ShortcutProviders;
        IDiscoveryIdentifierConflictHandler m_IdentifierConflictHandler;

        public Discovery(IEnumerable<IDiscoveryShortcutProvider> shortcutProviders, IDiscoveryIdentifierConflictHandler identifierConflictHandler)
        {
            m_ShortcutProviders = shortcutProviders;
            m_IdentifierConflictHandler = identifierConflictHandler;
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

                    identifier2ShortcutEntry.Add(shortcutEntry.identifier);
                    availableShortcuts.Add(shortcutEntry);
                }
            }
            return availableShortcuts;
        }
    }
}
