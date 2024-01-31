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

        // the current set of keys used for querying shortcut entry matches against active contexts. cleared frequently.
        // this is a list because chord support was planned for, but ultimately not implemented. in practice, this is
        // only ever 1 entry long (key + modifier).
        List<KeyCombination> m_KeyCombinationSequence = new List<KeyCombination>();
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();

        // separate lists of action and clutch entries are used to store candidate shortcut entries when a mouse down
        // is not yet determined to be a clutch or action. clicking, or dragging or invoking a clutch dependent context
        // shortcut will determine if the mouse event is an action or clutch (respectively).
        List<ShortcutEntry> m_MouseActionEntries = new List<ShortcutEntry>();
        List<ShortcutEntry> m_MouseClutchEntries = new List<ShortcutEntry>();

        // this is a frequently used temporary list kept as a property to avoid gc
        List<KeyCode> m_ContextClutchKeyRemoveQueue = new List<KeyCode>();
        // key down events is used to keep track of mismatched key down/up events. ex, external code calls Event.Use()
        // on key down but not up.
        List<KeyCode> m_KeyDownEvents = new List<KeyCode>();

        internal List<ShortcutEntry> activeEntries => m_Entries;
        internal List<ShortcutEntry> activeMouseActionEntries => m_MouseActionEntries;
        internal List<ShortcutEntry> activeMouseClutchEntries => m_MouseClutchEntries;

        // accessed by tests
        internal Dictionary<KeyCode, ClutchShortcutContext> m_ClutchActivatedContexts = new();

        static readonly Event s_QueuedMouseClutchEvent = new Event(), s_CachedCurrentEvent = new Event();

        struct ActiveClutch
        {
            public ShortcutEntry entry;
            public object context;
            public ClutchShortcutContext clutchContext;
        }

        // There can be more than one active clutch at a time.
        readonly Dictionary<KeyCode, ActiveClutch> m_ActiveClutches = new();

        (KeyCode keyCode, ShortcutEntry entry, object context) m_ActiveMouseActionEntry;

        static readonly List<EventType> k_ShortcutEventFilter = new List<EventType>
        {
            EventType.KeyDown,
            EventType.KeyUp,
            EventType.MouseDown,
            EventType.MouseUp,
            EventType.MouseDrag,
            EventType.ScrollWheel,
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

        public event Action<ShortcutEntry, ShortcutArguments> beforeShortcutInvoked;

        public Trigger(IDirectory directory, IConflictResolver conflictResolver)
        {
            m_Directory = directory;
            m_ConflictResolver = conflictResolver;
        }

        public void ResetShortcutState(EventType type, KeyCode keyCode)
        {
            switch (type)
            {
                case EventType.MouseDown:
                    if (m_ActiveMouseActionEntry.entry != null && m_ActiveMouseActionEntry.keyCode == keyCode)
                        ResetMouseShortcuts();
                    m_ActiveClutches.Remove(keyCode);
                    break;
                case EventType.KeyDown:
                    if (m_KeyDownEvents.Contains(keyCode))
                        return;
                    m_KeyDownEvents.Add(keyCode);
                    m_ActiveClutches.Remove(keyCode);
                    break;
                case EventType.KeyUp:
                    m_KeyDownEvents.Remove(keyCode);
                    break;
            }
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
                // mouse shortcuts can be both action and clutch. if this is the case, m_ActiveMouseActionEntry will be
                // valid when mouse delta is less than a drag threshold
                if (m_ActiveMouseActionEntry.entry != null && m_ActiveMouseActionEntry.keyCode == evt.keyCode)
                {
                    var shortcutEntry = m_ActiveMouseActionEntry.entry;
                    var context = m_ActiveMouseActionEntry.context;

                    ExecuteShortcut(shortcutEntry, ShortcutStage.End, context, true);
                    evt.Use();

                    return;
                }

                if (m_ActiveClutches.TryGetValue(evt.keyCode, out var active))
                {
                    var shortcutEntry = active.entry;
                    var context = active.context;

                    DeactivateClutchContext(active.clutchContext);
                    m_ActiveClutches.Remove(evt.keyCode);
                    ExecuteShortcut(shortcutEntry, ShortcutStage.End, context, true);
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
                if (ModifierKeysChangedWasHandled(activeClutch.Value.entry, evt, newModifier, contextManager))
                    return;
            }
            else if (!modifierKeysChanged && isKeyUpOrMouseUpEvent)
            {
                return;
            }

            // Use the event and return if the key is currently used in an active clutch
            if (m_ActiveClutches.ContainsKey(evt.keyCode))
            {
                evt.Use();
                return;
            }

            var keyCodeCombination = KeyCombination.FromInput(evt);
            m_KeyCombinationSequence.Add(keyCodeCombination);

            // Ignore event if sequence is empty
            if (m_KeyCombinationSequence.Count == 0)
                return;

            List<ShortcutEntry> entries = new List<ShortcutEntry>();

            switch (evt.type)
            {
                case EventType.MouseDrag when m_MouseClutchEntries.Count > 1:
                    entries = m_MouseClutchEntries;
                    break;

                case EventType.MouseDrag when m_MouseClutchEntries.Count == 1:
                {
                    if ((evt.mousePosition - m_StartPosition).magnitude <= k_DragOffset)
                    {
                        ResetKeyCombo();
                        return;
                    }

                    // in some cases the drag can change ShortcutContext.active (ex, mouse down very close to window
                    // border, then drag outside exceeds k_DragOffset)
                    var entry = m_MouseClutchEntries[0];
                    var key = entry.combinations[0].keyCode;

                    if (contextManager.HasActiveContextOfType(entry.context))
                    {
                        entries.Add(entry);
                    }
                    else if (m_ClutchActivatedContexts.TryGetValue(key, out var clutchCtx))
                    {
                        DeactivateClutchContext(clutchCtx);
                        m_ClutchActivatedContexts.Remove(key);
                    }

                    ResetActiveMouseActionEntry();
                    m_MouseClutchEntries.Clear();
                    break;
                }

                case EventType.MouseDrag:
                    ResetKeyCombo();
                    return;

                case EventType.MouseDown:
                case EventType.KeyDown:
                case EventType.ScrollWheel:
                    m_Directory.FindShortcutEntries(m_KeyCombinationSequence, contextManager, m_Entries);
                    entries = m_Entries;
                    SetClutchContextActive(contextManager, evt.keyCode, entries);
                    break;
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

                    s_QueuedMouseClutchEvent.CopyFrom(evt);

                    SetClutchContextActive(contextManager, evt.keyCode, m_MouseClutchEntries);

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
                            args.context = contextManager.GetContextInstanceOfType(m_MouseActionEntries[0].context, true);
                            m_ActiveMouseActionEntry = (evt.keyCode, m_MouseActionEntries[0], args.context);
                        }

                        ResetKeyCombo();
                        return;
                    }
                }
            }

            switch (entries.Count)
            {
                case 0:
                    ResetKeyCombo();
                    break;

                case 1:
                    var shortcutEntry = entries[0];
                    if (ShortcutFullyMatchesKeyCombination(shortcutEntry, m_KeyCombinationSequence))
                    {
                        if (evt.keyCode != m_KeyCombinationSequence[^1].keyCode)
                            break;

                        var context = contextManager.GetContextInstanceOfType(shortcutEntry.context, true);
                        if (context == null)
                        {
                            ResetKeyCombo();
                            break;
                        }

                        // When a clutch key that belongs to a dependent context is invoked while we are waiting to
                        // determine whether a mouse button is a click or drag, consider that key to be an indication
                        // that this is a "drag" (clutch).
                        if(m_MouseActionEntries.Count > 0 && m_MouseClutchEntries.Count == 1)
                        {
                            foreach (var kvp in m_ClutchActivatedContexts)
                            {
                                if (kvp.Value != context)
                                    continue;

                                // invoke the queued mouse clutch shortcut first, then allow the current event to be invoked
                                var queued = m_MouseClutchEntries[0];
                                var key = queued.combinations[0].keyCode;

                                if (!m_ActiveClutches.ContainsKey(key)
                                    && contextManager.GetContextInstanceOfType(queued.context, true) is var mouseCtx)
                                {
                                    m_ClutchActivatedContexts.Remove(kvp.Key);

                                    // null check because when HandleKeyEvent is invoked from tests the current event is
                                    // not used.
                                    if (Event.current != null)
                                    {
                                        s_CachedCurrentEvent.CopyFrom(Event.current);
                                        Event.current.CopyFrom(s_QueuedMouseClutchEvent);
                                        ExecuteShortcut(queued, ShortcutStage.Begin, mouseCtx, true);
                                        Event.current.CopyFrom(s_CachedCurrentEvent);
                                    }
                                    else
                                    {
                                        ExecuteShortcut(queued, ShortcutStage.Begin, mouseCtx, true);
                                    }

                                    m_ActiveClutches.Add(key, new ActiveClutch()
                                    {
                                        entry = queued,
                                        context = mouseCtx,
                                        clutchContext = context as ClutchShortcutContext
                                    });

                                    break;
                                }
                            }
                        }

                        switch (shortcutEntry.type)
                        {
                            case ShortcutType.Menu:
                            case ShortcutType.Action:
                                ExecuteShortcut(shortcutEntry, ShortcutStage.End, context, true);
                                evt.Use();
                                break;

                            case ShortcutType.Clutch:
                                if (!m_ActiveClutches.ContainsKey(evt.keyCode))
                                {
                                    var evtKeyCode = evt.keyCode;
                                    m_ClutchActivatedContexts.Remove(evtKeyCode);
                                    var args = ExecuteShortcut(shortcutEntry, ShortcutStage.Begin, context, true);
                                    m_ActiveClutches.Add(evtKeyCode, new ActiveClutch()
                                    {
                                        entry = shortcutEntry,
                                        context = args.context,
                                        clutchContext = shortcutEntry.clutchContext == null
                                            ? null
                                            : contextManager.GetContextInstanceOfType(shortcutEntry.clutchContext, true) as
                                                ClutchShortcutContext
                                    });
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
                        ResetKeyCombo();
                    }
                    break;
            }
        }

        void SetClutchContextActive(IContextManager context, KeyCode key, IEnumerable<ShortcutEntry> shortcuts)
        {
            foreach (var entry in shortcuts)
            {
                if (entry.type != ShortcutType.Clutch || entry.clutchContext == null)
                    continue;

                if (context.GetContextInstanceOfType(entry.clutchContext, false) is ClutchShortcutContext ctx)
                {
                    m_ClutchActivatedContexts[key] = ctx;
                    ctx.active = true;
                }
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
                    var newContext = contextManager.GetContextInstanceOfType(newShortcutEntry.context, true);
                    var newContextType = newContext.GetType();
                    var previousContext = activeClutchShortcutEntry.context;
                    if (previousContext != newContextType)
                        return false;

                    ResetActiveClutches();

                    m_ClutchActivatedContexts.Remove(newActiveClutchKeyCombination.keyCode);

                    var newArgs = ExecuteShortcut(newShortcutEntry, ShortcutStage.Begin, newContext, true);

                    m_ActiveClutches.Add(newActiveClutchKeyCombination.keyCode, new ActiveClutch()
                    {
                        entry = newShortcutEntry,
                        context = newArgs.context,
                        clutchContext = contextManager.GetContextInstanceOfType(newShortcutEntry.clutchContext, true) as ClutchShortcutContext
                    });

                    evt.Use();

                    return true;

                default:
                    if (HasConflicts(newEntries, keyCombinationSequence))
                    {
                        if (ShowConflictsWindow)
                            m_ConflictResolver.ResolveConflict(keyCombinationSequence, newEntries);

                        evt.Use();
                        ResetKeyCombo();

                        return true;
                    }

                    return false;
            }
        }

        public void ResetActiveClutches()
        {
            // this is done in two passes to ensure that shortcuts enabled by a clutch context are ended before the
            // parent clutch shortcut ends. it is necessary because the child clutch can be dependent on state managed
            // by the parent (see scene view fps camera navigation).
            foreach (var shortcut in m_ActiveClutches.Values)
            {
                if (shortcut.clutchContext == null)
                    ExecuteShortcut(shortcut.entry, ShortcutStage.End, shortcut.context);
            }

            foreach (var shortcut in m_ActiveClutches.Values)
            {
                if (shortcut.clutchContext == null)
                    continue;
                ExecuteShortcut(shortcut.entry, ShortcutStage.End, shortcut.context);
                shortcut.clutchContext.active = false;
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

        bool CurrentContextManagerHasPriorityContextFor(ShortcutEntry entry)
        {
            return m_CurrentContextManager.HasPriorityContextOfType(entry.context);
        }

        ShortcutArguments ExecuteShortcut(ShortcutEntry entry, ShortcutStage stage, object context, bool reset = false)
        {
            var args = new ShortcutArguments
            {
                context = context,
                stage = stage
            };

            try
            {
                // it is imperative that exception does not derail execution here. after execution active clutches
                // need to be cleared, otherwise trigger state can be irreparably corrupted.
                beforeShortcutInvoked?.Invoke(entry, args);
                entry.action(args);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (reset)
                {
                    ResetKeyCombo();
                    ResetMouseShortcuts();
                }
            }

            return args;
        }

        void DeactivateClutchContext(ClutchShortcutContext context)
        {
            if (context == null)
                return;

            m_ContextClutchKeyRemoveQueue.Clear();

            // end any active clutches that are subject to this context
            foreach (var active in m_ActiveClutches)
            {
                if (active.Value.context != context)
                    continue;
                m_ContextClutchKeyRemoveQueue.Add(active.Key);
                ExecuteShortcut(active.Value.entry, ShortcutStage.End, context);
            }

            foreach (var key in m_ContextClutchKeyRemoveQueue)
                m_ActiveClutches.Remove(key);

            context.active = false;
        }

        void ResetKeyCombo()
        {
            m_KeyCombinationSequence.Clear();
        }

        void ResetMouseShortcuts()
        {
            foreach (var clutch in m_ClutchActivatedContexts)
                DeactivateClutchContext(clutch.Value);

            m_ClutchActivatedContexts.Clear();

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
