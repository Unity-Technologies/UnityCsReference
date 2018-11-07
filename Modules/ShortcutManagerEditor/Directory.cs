// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShortcutManagement
{
    class Directory : IDirectory
    {
        List<ShortcutEntry>[] m_IndexedShortcutEntries;
        List<ShortcutEntry> m_ShortcutEntries;

        public Directory(IEnumerable<ShortcutEntry> entries)
        {
            Initialize(entries);
        }

        public void Initialize(IEnumerable<ShortcutEntry> entries)
        {
            m_ShortcutEntries = new List<ShortcutEntry>(entries.Count());
            m_IndexedShortcutEntries = new List<ShortcutEntry>[(int)KeyCode.Mouse0];
            foreach (ShortcutEntry entry in entries)
            {
                m_ShortcutEntries.Add(entry);
                if (entry.combinations.Any())
                    AddEntry(entry.combinations.First().keyCode, entry);
                // TODO: Index unbound entries?
            }
        }

        void FindShortcutEntries(List<KeyCombination> combinationSequence, List<ShortcutEntry> outputShortcuts, Predicate<ShortcutEntry> filter)
        {
            outputShortcuts.Clear();

            Assert.IsNotNull(combinationSequence);
            Assert.IsTrue(combinationSequence.Count > 0, "Sequence can not be empty");
            List<ShortcutEntry> entries = m_IndexedShortcutEntries[(int)combinationSequence[0].keyCode];
            if (entries == null)
                return;

            m_CombinationSequence = combinationSequence;
            m_Predicate = filter;
            foreach (var entry in entries)
                if (ShortcutStartsWithCombinationSequenceAndSatisfiesPredicate(entry))
                    outputShortcuts.Add(entry);
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, Type[] context, List<ShortcutEntry> outputShortcuts)
        {
            m_ShortcutEntryContextList = context;
            Predicate<ShortcutEntry> filter = null;
            if (context != null)
                filter = ShortcutEntryMatchesAnyContext;
            FindShortcutEntries(combinationSequence, outputShortcuts, filter);
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, IContextManager contextManager, List<ShortcutEntry> outputShortcuts)
        {
            m_ContextManager = contextManager;
            FindShortcutEntries(combinationSequence, outputShortcuts, ShortcutEntrySatisfiesContextManager);
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, List<ShortcutEntry> outputShortcuts)
        {
            FindShortcutEntries(combinationSequence, outputShortcuts, null);
        }

        public ShortcutEntry FindShortcutEntry(Identifier identifier)
        {
            return m_ShortcutEntries.FirstOrDefault(s => s.identifier.Equals(identifier));
        }

        public ShortcutEntry FindShortcutEntry(string shortcutId)
        {
            var id = new Identifier();
            id.path = shortcutId;
            return FindShortcutEntry(id);
        }

        void AddEntry(KeyCode keyCode, ShortcutEntry entry)
        {
            int keyCodeValue = (int)keyCode;
            List<ShortcutEntry> entriesForKeyCode = m_IndexedShortcutEntries[keyCodeValue];
            if (entriesForKeyCode == null)
                m_IndexedShortcutEntries[keyCodeValue] = entriesForKeyCode = new List<ShortcutEntry>();
            entriesForKeyCode.Add(entry);
        }

        //////////////////////////////
        // Lambda elimination helpers
        //////////////////////////////
        static Type m_ShortcutEntryContext;
        static Type[] m_ShortcutEntryContextList;
        static List<KeyCombination> m_CombinationSequence;
        static Predicate<ShortcutEntry> m_Predicate;
        static IContextManager m_ContextManager;

        static bool ShortcutStartsWithCombinationSequenceAndSatisfiesPredicate(ShortcutEntry entry)
        {
            return entry.StartsWith(m_CombinationSequence) && (m_Predicate == null || m_Predicate(entry));
        }

        static bool ShortcutEntryMatchesAnyContext(ShortcutEntry entry)
        {
            m_ShortcutEntryContext = entry.context;
            return m_ShortcutEntryContextList == null || m_ShortcutEntryContextList.Any(ShortcutEntryMatchesContext);
        }

        static bool ShortcutEntryMatchesContext(Type context)
        {
            return context == m_ShortcutEntryContext ||
                (m_ShortcutEntryContext != null && m_ShortcutEntryContext.IsAssignableFrom(context));
        }

        static bool ShortcutEntrySatisfiesContextManager(ShortcutEntry entry)
        {
            return m_ContextManager.HasActiveContextOfType(entry.context) &&
                // Emulate old play mode shortcut behavior
                // * Menu shortcuts are always active
                // * Non-menu shortcuts only apply when the game view does not have focus
                (!m_ContextManager.playModeContextIsActive ||
                    entry.type == ShortcutType.Menu);
        }
    }
}
