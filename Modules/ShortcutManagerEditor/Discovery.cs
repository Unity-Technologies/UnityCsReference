// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class Discovery : IDiscovery
    {
        IEnumerable<IDiscoveryShortcutProvider> m_ShortcutProviders;
        IBindingValidator m_BindingValidator;
        IDiscoveryInvalidShortcutReporter m_InvalidShortcutReporter;

        internal const string k_MainMenuShortcutPrefix = "Main Menu/";

        public Discovery(IEnumerable<IDiscoveryShortcutProvider> shortcutProviders, IBindingValidator bindingValidator, IDiscoveryInvalidShortcutReporter invalidShortcutReporter = null)
        {
            m_ShortcutProviders = shortcutProviders;
            m_BindingValidator = bindingValidator;
            m_InvalidShortcutReporter = invalidShortcutReporter;
        }

        public IEnumerable<ShortcutEntry> GetAllShortcuts()
        {
            var availableShortcuts = new List<ShortcutEntry>();
            var identifier2ShortcutEntry = new HashSet<Identifier>();
            var displayName2ShortcutEntry = new HashSet<string>();

            foreach (var discoveryModule in m_ShortcutProviders)
            {
                var shortcuts = discoveryModule.GetDefinedShortcuts();
                foreach (var discoveredEntry in shortcuts)
                {
                    var shortcutEntry = discoveredEntry.GetShortcutEntry();

                    if (shortcutEntry.identifier.path != null && shortcutEntry.type != ShortcutType.Menu && shortcutEntry.identifier.path.StartsWith(k_MainMenuShortcutPrefix))
                    {
                        m_InvalidShortcutReporter?.ReportReservedIdentifierPrefixConflict(discoveredEntry, k_MainMenuShortcutPrefix);
                        continue;
                    }

                    if (shortcutEntry.displayName != null && shortcutEntry.type != ShortcutType.Menu && shortcutEntry.displayName.StartsWith(k_MainMenuShortcutPrefix))
                    {
                        m_InvalidShortcutReporter?.ReportReservedDisplayNamePrefixConflict(discoveredEntry, k_MainMenuShortcutPrefix);
                        continue;
                    }

                    if (identifier2ShortcutEntry.Contains(shortcutEntry.identifier))
                    {
                        m_InvalidShortcutReporter?.ReportIdentifierConflict(discoveredEntry);
                        continue;
                    }

                    if (displayName2ShortcutEntry.Contains(shortcutEntry.displayName))
                    {
                        m_InvalidShortcutReporter?.ReportDisplayNameConflict(discoveredEntry);
                        continue;
                    }

                    if (!ValidateContext(discoveredEntry))
                    {
                        m_InvalidShortcutReporter?.ReportInvalidContext(discoveredEntry);
                        continue;
                    }

                    string invalidBindingMessage;
                    if (!m_BindingValidator.IsBindingValid(shortcutEntry.combinations, out invalidBindingMessage))
                    {
                        m_InvalidShortcutReporter?.ReportInvalidBinding(discoveredEntry, invalidBindingMessage);

                        // Replace invalid binding with empty binding
                        var emptyBinding = Enumerable.Empty<KeyCombination>();
                        shortcutEntry = new ShortcutEntry(shortcutEntry.identifier, emptyBinding, shortcutEntry.action,
                            shortcutEntry.context, shortcutEntry.tag, shortcutEntry.type);
                    }

                    identifier2ShortcutEntry.Add(shortcutEntry.identifier);
                    displayName2ShortcutEntry.Add(shortcutEntry.displayName);
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
