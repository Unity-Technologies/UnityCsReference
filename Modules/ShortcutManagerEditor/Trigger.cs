// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class Trigger
    {
        IDirectory m_Directory;
        IConflictResolver m_ConflictResolver;
        IContextManager m_CurrentContextManager;
        KeyCombination m_CurrentActiveClutch;

        List<KeyCombination> m_KeyCombinationSequence = new List<KeyCombination>();
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();

        List<ShortcutEntry> m_MouseActionEntries = new List<ShortcutEntry>();
        List<ShortcutEntry> m_MouseClutchEntries = new List<ShortcutEntry>();

        // There can be more than one active clutch at a time.
        Dictionary<KeyCode, KeyValuePair<ShortcutEntry, object>> m_ActiveClutches = new Dictionary<KeyCode, KeyValuePair<ShortcutEntry, object>>();

        (KeyCode keyCode, ShortcutEntry entry, object context) m_ActiveMouseActionEntry;

        static readonly List<EventType> k_ShortcutEventFilter = new List<EventType>
        {
            EventType.KeyDown,
            EventType.KeyUp,
            EventType.MouseDown,
            EventType.MouseUp,
            EventType.MouseDrag
        };

        Vector2 m_StartPosition;

        const float k_DragOffset = 6f;

        // Used only in tests
        bool m_ShowConflictsWindow = true;
        internal bool ShowConflictsWindow
        {
            get => m_ShowConflictsWindow;
            set => m_ShowConflictsWindow = value;
        }

        public event Action<ShortcutEntry, ShortcutArguments> invokingAction;

        public Trigger(IDirectory directory, IConflictResolver conflictResolver)
        {
            m_Directory = directory;
            m_ConflictResolver = conflictResolver;
        }

        public void HandleKeyEvent(Event evt, IContextManager contextManager)
        {
            if (evt == null || evt.keyCode == KeyCode.None)
                return;

            if (!k_ShortcutEventFilter.Contains(evt.type))
                return;

            var isKeyUpOrMouseUpEvent = evt.type == EventType.KeyUp || evt.type == EventType.MouseUp;
            if (isKeyUpOrMouseUpEvent)
            {
                if (m_ActiveMouseActionEntry.entry != null && m_ActiveMouseActionEntry.keyCode == evt.keyCode)
                {
                    var shortcutEntry = m_ActiveMouseActionEntry.entry;
                    var context = m_ActiveMouseActionEntry.context;

                    ExecuteShortcut(shortcutEntry, ShortcutStage.End, context);
                    evt.Use();

                    return;
                }

                KeyValuePair<ShortcutEntry, object> clutchKeyValuePair;
                if (m_ActiveClutches.TryGetValue(evt.keyCode, out clutchKeyValuePair))
                {
                    var shortcutEntry = clutchKeyValuePair.Key;
                    var context = m_ActiveClutches[evt.keyCode].Value;

                    m_ActiveClutches.Remove(evt.keyCode);

                    ExecuteShortcut(shortcutEntry, ShortcutStage.End, context);
                    evt.Use();

                    return;
                }
            }

            var newModifier = EventModifiers.None;
            var isModifierKey = KeyCombination.k_KeyCodeToEventModifiers.TryGetValue(evt.keyCode, out newModifier);
            var isKeyEvent = evt.type == EventType.KeyDown || evt.type == EventType.KeyUp;
            var modifierKeysChanged = isKeyEvent && isModifierKey;

            // Won't handle the case where there are multiple active clutches at once.
            if (modifierKeysChanged && m_ActiveClutches != null && m_ActiveClutches.Count == 1)
            {
                var enumerator = m_ActiveClutches.GetEnumerator();
                enumerator.MoveNext();

                var activeClutch = enumerator.Current;
                if (ModifierKeysChangedWasHandled(activeClutch.Value.Key, evt, newModifier, contextManager))
                    return;
            }
            else if (!modifierKeysChanged && isKeyUpOrMouseUpEvent)
            {
                return;
            }

            // Use the event and return if the key is currently used in an active clutch
            if (m_ActiveClutches.ContainsKey(evt.keyCode))
                return;

            var keyCodeCombination = KeyCombination.FromInput(evt);
            m_KeyCombinationSequence.Add(keyCodeCombination);

            // Ignore event if sequence is empty
            if (m_KeyCombinationSequence.Count == 0)
                return;

            List<ShortcutEntry> entries = new List<ShortcutEntry>();
            if (evt.type == EventType.MouseDrag)
            {
                if (m_MouseClutchEntries.Count > 1)
                {
                    entries = m_MouseClutchEntries;
                }
                else if (m_MouseClutchEntries.Count == 1)
                {
                    if ((evt.mousePosition - m_StartPosition).magnitude <= k_DragOffset)
                    {
                        Reset();
                        return;
                    }

                    entries = new List<ShortcutEntry>(m_MouseClutchEntries);
                    ResetActiveMouseActionEntry();
                    m_MouseClutchEntries.Clear();
                }
                else
                {
                    Reset();
                    return;
                }
            }
            else if (evt.type == EventType.MouseDown || evt.type == EventType.KeyDown)
            {
                m_Directory.FindShortcutEntries(m_KeyCombinationSequence, contextManager, m_Entries);
                entries = m_Entries;
            }

            if (entries.Count > 1)
            {
                // Deal ONLY with prioritycontext
                if (contextManager.HasAnyPriorityContext())
                {
                    m_CurrentContextManager = contextManager;
                    entries = m_Entries.FindAll(CurrentContextManagerHasPriorityContextFor);
                    if (entries.Count == 0)
                        entries = m_Entries;
                }

                if (evt.type == EventType.MouseDown)
                {
                    ResetMouseShortcuts();
                    foreach (var entry in entries)
                    {
                        if (entry.type == ShortcutType.Action)
                            m_MouseActionEntries.Add(entry);
                        else if (entry.type == ShortcutType.Clutch)
                            m_MouseClutchEntries.Add(entry);
                    }

                    if (m_MouseActionEntries.Count > 1)
                    {
                        entries = m_MouseActionEntries;
                    }
                    else if (m_MouseActionEntries.Count == 1)
                    {
                        m_StartPosition = evt.mousePosition;

                        ShortcutArguments args = new ShortcutArguments();
                        if (m_ActiveMouseActionEntry.entry == null || m_ActiveMouseActionEntry.keyCode != evt.keyCode)
                        {
                            args.context = contextManager.GetContextInstanceOfType(m_MouseActionEntries[0].context);
                            m_ActiveMouseActionEntry = (evt.keyCode, m_MouseActionEntries[0], args.context);
                        }

                        Reset();
                        return;
                    }
                }
            }

            switch (entries.Count)
            {
                case 0:
                    Reset();
                    break;

                case 1:
                    var shortcutEntry = entries[0];
                    if (ShortcutFullyMatchesKeyCombination(shortcutEntry, m_KeyCombinationSequence))
                    {
                        if (evt.keyCode != m_KeyCombinationSequence[^1].keyCode)
                            break;

                        var context = contextManager.GetContextInstanceOfType(shortcutEntry.context);
                        switch (shortcutEntry.type)
                        {
                            case ShortcutType.Menu:
                            case ShortcutType.Action:
                                ExecuteShortcut(shortcutEntry, ShortcutStage.End, context);
                                evt.Use();
                                break;

                            case ShortcutType.Clutch:
                                if (!m_ActiveClutches.ContainsKey(evt.keyCode))
                                {
                                    var evtKeyCode = evt.keyCode;
                                    var args = ExecuteShortcut(shortcutEntry, ShortcutStage.Begin, context);
                                    var keyValuePair = new KeyValuePair<ShortcutEntry, object>(shortcutEntry, args.context);
                                    m_ActiveClutches.Add(evtKeyCode, keyValuePair);
                                    evt.Use();
                                }
                                break;
                        }
                    }
                    break;

                default:
                    if (HasConflicts(entries, m_KeyCombinationSequence))
                    {
                        if (ShowConflictsWindow)
                            m_ConflictResolver.ResolveConflict(m_KeyCombinationSequence, entries);

                        evt.Use();
                        Reset();
                    }
                    break;
            }
        }

        bool ModifierKeysChangedWasHandled(ShortcutEntry activeClutchShortcutEntry, Event evt, EventModifiers modifier, IContextManager contextManager)
        {
            // Assume that there is only one key combination per shortcut entry.
            var previousActiveClutchKeyCombination = activeClutchShortcutEntry.combinations[0];
            if (!m_CurrentActiveClutch.Equals(default) && !m_CurrentActiveClutch.Equals(previousActiveClutchKeyCombination))
                previousActiveClutchKeyCombination = m_CurrentActiveClutch;

            KeyCombination newActiveClutchKeyCombination = default;
            if (evt.type == EventType.KeyDown)
            {
                // Add the new modifier key (the event key code) to the shortcut's key combination.
                newActiveClutchKeyCombination = KeyCombination.FromKeyboardInputAndAddEventModifiers(previousActiveClutchKeyCombination, modifier | evt.modifiers);
            }
            else if (evt.type == EventType.KeyUp)
            {
                if (evt.modifiers != EventModifiers.None)
                {
                    // In the case that the event has a modifier, add it to the shortcut's key combination
                    // and remove the modifier key (the event key code) from the shortcut's key combination.
                    newActiveClutchKeyCombination = KeyCombination.FromKeyboardInputAndAddEventModifiers(previousActiveClutchKeyCombination, evt.modifiers);
                    newActiveClutchKeyCombination = KeyCombination.FromKeyboardInputAndRemoveEventModifiers(newActiveClutchKeyCombination, modifier);
                }
                else
                {
                    // Remove the modifier key (the event key code) from the shortcut's key combination.
                    newActiveClutchKeyCombination = KeyCombination.FromKeyboardInputAndRemoveEventModifiers(previousActiveClutchKeyCombination, modifier);
                }
            }

            m_CurrentActiveClutch = newActiveClutchKeyCombination;

            var keyCombinationSequence = new List<KeyCombination>();
            keyCombinationSequence.Add(newActiveClutchKeyCombination);

            if (ShortcutFullyMatchesKeyCombination(activeClutchShortcutEntry, keyCombinationSequence))
                return false;

            var newEntries = new List<ShortcutEntry>();
            m_Directory.FindShortcutEntries(keyCombinationSequence, contextManager, newEntries);
            if (newEntries.Count == 0)
                return false;

            // Check for conflicts.
            if (newEntries.Count > 1)
            {
                // Deal ONLY with prioritycontext
                if (contextManager.HasAnyPriorityContext())
                {
                    m_CurrentContextManager = contextManager;
                    var priorityEntries = newEntries.FindAll(CurrentContextManagerHasPriorityContextFor);
                    if (priorityEntries.Count != 0)
                        newEntries = priorityEntries;
                }

                // Only retrieve the clutch shortcut entries.
                var clutchShortcuts = new List<ShortcutEntry>();
                foreach (var entry in newEntries)
                {
                    if (entry.type == ShortcutType.Clutch)
                        clutchShortcuts.Add(entry);
                }

                newEntries = clutchShortcuts;
            }
            else
            {
                if (newEntries[0].type == ShortcutType.Action)
                    return false;
            }

            switch (newEntries.Count)
            {
                case 0:
                    return false;

                case 1:
                    var newShortcutEntry = newEntries[0];
                    var newContext = contextManager.GetContextInstanceOfType(newShortcutEntry.context);
                    var newContextType = newContext.GetType();
                    var previousContext = activeClutchShortcutEntry.context;
                    if (previousContext != newContextType)
                        return false;

                    ResetActiveClutches();

                    var newArgs = ExecuteShortcut(newShortcutEntry, ShortcutStage.Begin, newContext);
                    var newKeyValuePair = new KeyValuePair<ShortcutEntry, object>(newShortcutEntry, newArgs.context);
                    m_ActiveClutches.Add(newActiveClutchKeyCombination.keyCode, newKeyValuePair);

                    evt.Use();

                    return true;

                default:
                    if (HasConflicts(newEntries, keyCombinationSequence))
                    {
                        if (ShowConflictsWindow)
                            m_ConflictResolver.ResolveConflict(keyCombinationSequence, newEntries);

                        evt.Use();
                        Reset();

                        return true;
                    }

                    return false;
            }
        }

        public void ResetActiveClutches()
        {
            foreach (var clutchKeyValuePair in m_ActiveClutches.Values)
            {
                var args = new ShortcutArguments
                {
                    context = clutchKeyValuePair.Value,
                    stage = ShortcutStage.End,
                };
                invokingAction?.Invoke(clutchKeyValuePair.Key, args);
                clutchKeyValuePair.Key.action(args);
            }

            m_ActiveClutches.Clear();
        }

        public bool HasAnyEntries()
        {
            return m_Entries.Count > 0;
        }

        // filtered entries are expected to all be in the same context and/or null context and they all are known to share the prefix
        bool HasConflicts(List<ShortcutEntry> filteredEntries, List<KeyCombination> prefix)
        {
            var hasEntryFullyMatchesPrefix = false;
            foreach (var entry in filteredEntries)
            {
                if (entry.FullyMatches(prefix))
                {
                    hasEntryFullyMatchesPrefix = true;
                    break;
                }
            }

            if (hasEntryFullyMatchesPrefix)
                return filteredEntries.Count > 1;

            return false;
        }

        bool ShortcutFullyMatchesKeyCombination(ShortcutEntry shortcutEntry, List<KeyCombination> keyCombinationSequence)
        {
            return shortcutEntry.FullyMatches(keyCombinationSequence);
        }

        void Reset()
        {
            m_KeyCombinationSequence.Clear();
        }

        bool CurrentContextManagerHasPriorityContextFor(ShortcutEntry entry)
        {
            return m_CurrentContextManager.HasPriorityContextOfType(entry.context);
        }

        ShortcutArguments ExecuteShortcut(ShortcutEntry entry, ShortcutStage stage, object context)
        {
            var args = new ShortcutArguments
            {
                context = context,
                stage = stage
            };

            invokingAction?.Invoke(entry, args);
            entry.action(args);

            Reset();
            ResetMouseShortcuts();

            return args;
        }

        void ResetMouseShortcuts()
        {
            ResetActiveMouseActionEntry();
            m_MouseClutchEntries.Clear();
            m_MouseActionEntries.Clear();
            m_CurrentActiveClutch = default;
        }

        void ResetActiveMouseActionEntry()
        {
            m_ActiveMouseActionEntry.keyCode = KeyCode.None;
            m_ActiveMouseActionEntry.context = null;
            m_ActiveMouseActionEntry.entry = null;
        }
    }
}
