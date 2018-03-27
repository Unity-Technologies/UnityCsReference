// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.ShortcutManagement
{
    class Trigger
    {
        IDirectory m_Directory;
        IConflictResolver m_ConflictResolver;

        List<KeyCombination> m_KeyCombinationSequence = new List<KeyCombination>();
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();
        Dictionary<KeyCode, ShortcutEntry> m_ActiveClutches = new Dictionary<KeyCode, ShortcutEntry>();
        HashSet<KeyCode> m_KeysDown = new HashSet<KeyCode>();
        static Type[] s_Context = { null, null };

        public IShortcutPriorityContext priorityContext
        {
            get
            {
                while (m_PriorityContexts.Count > 0)
                {
                    var ctx = m_PriorityContexts.Peek();
                    if (
                        ctx == null
                        || !ctx.active
                        || typeof(UnityObject).IsAssignableFrom(ctx.GetType()) && (UnityObject)ctx == null
                        )
                    {
                        // remove an inactive priority context in case e.g., user forgot to clear it when done
                        // this pattern was chosen to make it more explicit to priority context implementer how to indicate completion
                        m_PriorityContexts.Pop();
                    }
                    else
                    {
                        return ctx;
                    }
                }
                return null;
            }
            set
            {
                if (!m_PriorityContexts.Contains(value))
                    m_PriorityContexts.Push(value);
            }
        }
        readonly Stack<IShortcutPriorityContext> m_PriorityContexts = new Stack<IShortcutPriorityContext>();

        public Trigger(IDirectory directory, IConflictResolver conflictResolver)
        {
            m_Directory = directory;
            m_ConflictResolver = conflictResolver;
        }

        public void HandleKeyEventForContext(Event evt, object context)
        {
            if (evt == null || !evt.isKey || evt.keyCode == KeyCode.None)
                return;

            if (evt.type == EventType.KeyUp)
            {
                m_KeysDown.Remove(evt.keyCode);
                ShortcutEntry shortcutEntry;
                if (m_ActiveClutches.TryGetValue(evt.keyCode, out shortcutEntry))
                {
                    m_ActiveClutches.Remove(evt.keyCode);
                    var args = new ShortcutArguments
                    {
                        context = context as EditorWindow,
                        state = ShortcutState.End,
                    };
                    shortcutEntry.action(args);
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

            s_Context[1] = priorityContext?.GetType() ?? context?.GetType();
            m_Directory.FindShortcutEntries(m_KeyCombinationSequence, s_Context, m_Entries);
            if (priorityContext != null && m_Entries.Count(entry => entry.context == null) < m_Entries.Count)
                m_Entries.RemoveAll(entry => entry.context == null);

            switch (m_Entries.Count)
            {
                case 0:
                    Reset();
                    break;

                case 1:
                    var shortcutEntry = m_Entries.Single();
                    if (ShortcutFullyMatchesKeyCombination(shortcutEntry))
                    {
                        if (evt.keyCode != m_KeyCombinationSequence.Last().keyCode)
                            break;

                        var args = new ShortcutArguments();
                        args.context = context as EditorWindow;
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
                                    m_ActiveClutches.Add(evt.keyCode, shortcutEntry);
                                    args.state = ShortcutState.Begin;
                                    shortcutEntry.action(args);
                                    evt.Use();
                                    Reset();
                                }
                                break;
                        }
                    }
                    break;

                default:
                    if (HasConflicts(m_Entries, m_KeyCombinationSequence))
                    {
                        m_ConflictResolver.ResolveConflict(m_KeyCombinationSequence, m_Entries);
                        Reset();
                    }
                    break;
            }
        }

        // filtered entries are expected to all be in the same context and/or null context and they all are known to share the prefix
        bool HasConflicts(List<ShortcutEntry> filteredEntries, List<KeyCombination> prefix)
        {
            if (filteredEntries.Any(e => e.FullyMatches(prefix)))
                return filteredEntries.Count > 1;
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
