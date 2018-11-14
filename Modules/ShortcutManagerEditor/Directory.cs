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

        private List<ShortcutEntry> GetShortcutEntriesForPrimaryKey(List<KeyCombination> combinationSequence)
        {
            if (combinationSequence == null || combinationSequence.Count < 1)
                return null;
            return m_IndexedShortcutEntries[(int)combinationSequence[0].keyCode];
        }

        // These two overloads have some duplication to avoid creating predicates etc.
        public void FindShortcutEntries(List<KeyCombination> combinationSequence, Type[] context, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();
            List<ShortcutEntry> entries = GetShortcutEntriesForPrimaryKey(combinationSequence);
            if (entries == null)
                return;
            foreach (var entry in entries)
                if (entry.StartsWith(combinationSequence) && ShortcutEntryMatchesAnyContext(entry.context, context))
                    outputShortcuts.Add(entry);
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, IContextManager contextManager, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();
            List<ShortcutEntry> entries = GetShortcutEntriesForPrimaryKey(combinationSequence);
            if (entries == null)
                return;
            foreach (var entry in entries)
                if (entry.StartsWith(combinationSequence) && ShortcutEntrySatisfiesContextManager(contextManager, entry))
                    outputShortcuts.Add(entry);
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, List<ShortcutEntry> outputShortcuts)
        {
            FindShortcutEntries(combinationSequence, (Type[])null, outputShortcuts);
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
        static bool ShortcutEntryMatchesAnyContext(Type shortcutEntryContext, Type[] contextList)
        {
            if (contextList == null)
                return true;
            foreach (var type in contextList)
                if (ShortcutEntryMatchesContext(shortcutEntryContext, type))
                    return true;
            return false;
        }

        static bool ShortcutEntryMatchesContext(Type shortcutEntryContext, Type context)
        {
            return context == shortcutEntryContext ||
                (shortcutEntryContext != null && shortcutEntryContext.IsAssignableFrom(context));
        }

        static bool ShortcutEntrySatisfiesContextManager(IContextManager contextManager, ShortcutEntry entry)
        {
            return contextManager.HasActiveContextOfType(entry.context) &&
                // Emulate old play mode shortcut behavior
                // * Menu shortcuts are always active
                // * Non-menu shortcuts only apply when the game view does not have focus
                (!contextManager.playModeContextIsActive ||
                    entry.type == ShortcutType.Menu);
        }
    }
}
