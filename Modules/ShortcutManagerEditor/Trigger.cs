// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class Trigger
    {
        IDirectory m_Directory;
        IConflictResolver m_ConflictResolver;

        List<KeyCombination> m_KeyCombinationSequence = new List<KeyCombination>();
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();
        Dictionary<KeyCode, Tuple<ShortcutEntry, object>> m_ActiveClutches = new Dictionary<KeyCode, Tuple<ShortcutEntry, object>>();
        HashSet<KeyCode> m_KeysDown = new HashSet<KeyCode>();

        public Trigger(IDirectory directory, IConflictResolver conflictResolver)
        {
            m_Directory = directory;
            m_ConflictResolver = conflictResolver;
        }

        public void HandleKeyEvent(Event evt, IContextManager contextManager)
        {
            if (evt == null || !evt.isKey || evt.keyCode == KeyCode.None)
                return;

            if (evt.type == EventType.KeyUp)
            {
                m_KeysDown.Remove(evt.keyCode);
                Tuple<ShortcutEntry, object> clutchTuple;
                if (m_ActiveClutches.TryGetValue(evt.keyCode, out clutchTuple))
                {
                    var clutchContext = m_ActiveClutches[evt.keyCode].Item2;

                    m_ActiveClutches.Remove(evt.keyCode);
                    var args = new ShortcutArguments
                    {
                        context = clutchContext,
                        state = ShortcutState.End
                    };
                    clutchTuple.Item1.action(args);
                }
                return;
            }

            if (m_KeysDown.Contains(evt.keyCode))
            {
                evt.Use();
                return;
            }
            m_KeysDown.Add(evt.keyCode);

            var keyCodeCombination = new KeyCombination(evt);
            m_KeyCombinationSequence.Add(keyCodeCombination);

            // Ignore event if sequence is empty
            if (m_KeyCombinationSequence.Count == 0)
                return;

            m_Directory.FindShortcutEntries(m_KeyCombinationSequence, contextManager, m_Entries);
            IEnumerable<ShortcutEntry> entries = m_Entries;

            // Deal ONLY with prioritycontext
            if (entries.Count() > 1 && contextManager.HasAnyPriorityContext())
            {
                entries = m_Entries.FindAll(a => contextManager.HasPriorityContextOfType(a.context));
                if (!entries.Any())
                    entries = m_Entries;
            }

            switch (entries.Count())
            {
                case 0:
                    Reset();
                    break;

                case 1:
                    var shortcutEntry = entries.Single();
                    if (ShortcutFullyMatchesKeyCombination(shortcutEntry))
                    {
                        if (evt.keyCode != m_KeyCombinationSequence.Last().keyCode)
                            break;

                        var args = new ShortcutArguments();
                        args.context = contextManager.GetContextInstanceOfType(shortcutEntry.context);
                        switch (shortcutEntry.type)
                        {
                            case ShortcutType.Action:
                                args.state = ShortcutState.End;
                                shortcutEntry.action(args);
                                evt.Use();
                                Reset();
                                break;

                            case ShortcutType.Clutch:
                                if (!m_ActiveClutches.ContainsKey(evt.keyCode))
                                {
                                    m_ActiveClutches.Add(evt.keyCode, new Tuple<ShortcutEntry, object>(shortcutEntry, args.context));
                                    args.state = ShortcutState.Begin;
                                    shortcutEntry.action(args);
                                    evt.Use();
                                    Reset();
                                }
                                break;
                            case ShortcutType.Menu:
                                args.state = ShortcutState.End;
                                EditorApplication.ExecuteMenuItem(shortcutEntry.identifier.path);
                                evt.Use();
                                Reset();
                                break;
                        }
                    }
                    break;

                default:
                    if (HasConflicts(entries, m_KeyCombinationSequence))
                    {
                        m_ConflictResolver.ResolveConflict(m_KeyCombinationSequence, entries);
                        Reset();
                    }
                    break;
            }
        }

        public bool HasAnyEntries()
        {
            return m_Entries.Any();
        }

        // filtered entries are expected to all be in the same context and/or null context and they all are known to share the prefix
        bool HasConflicts(IEnumerable<ShortcutEntry> filteredEntries, List<KeyCombination> prefix)
        {
            if (filteredEntries.Any(e => e.FullyMatches(prefix)))
                return filteredEntries.Count() > 1;
            return false;
        }

        bool ShortcutFullyMatchesKeyCombination(ShortcutEntry shortcutEntry)
        {
            return shortcutEntry.FullyMatches(m_KeyCombinationSequence);
        }

        void Reset()
        {
            m_KeyCombinationSequence.Clear();
        }
    }
}
