// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class Directory : IDirectory
    {
        List<ShortcutEntry>[] m_IndexedShortcutEntries;
        List<ShortcutEntry> m_ShortcutEntries;

        public const int MaxIndexedEntries = (int)KeyCode.Mouse6 + 1;

        public Directory(IEnumerable<ShortcutEntry> entries)
        {
            Initialize(entries);
        }

        public void Initialize(IEnumerable<ShortcutEntry> entries)
        {
            m_ShortcutEntries = new List<ShortcutEntry>(entries.Count());
            m_IndexedShortcutEntries = new List<ShortcutEntry>[MaxIndexedEntries];
            foreach (ShortcutEntry entry in entries)
            {
                m_ShortcutEntries.Add(entry);
                if (entry.combinations.Any())
                    AddEntry(entry.combinations.First().keyCode, entry);
                // TODO: Index unbound entries?
            }
        }

        public void GetAllShortcuts(List<ShortcutEntry> output)
        {
            output.AddRange(m_ShortcutEntries);
        }

        private List<ShortcutEntry> GetShortcutEntriesForPrimaryKey(List<KeyCombination> combinationSequence)
        {
            if (combinationSequence == null || combinationSequence.Count < 1)
                return null;
            return m_IndexedShortcutEntries[(int)combinationSequence[0].keyCode];
        }

        // These two overloads have some duplication to avoid creating predicates etc.
        public void FindShortcutEntries(List<KeyCombination> combinationSequence, Type[] context, string[] tags, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();
            List<ShortcutEntry> entries = GetShortcutEntriesForPrimaryKey(combinationSequence);

            if (entries == null) return;

            foreach (var entry in entries)
                if (entry.StartsWith(combinationSequence) && ShortcutEntryMatchesAnyContext(entry.context, context)
                    && entry.context != typeof(ContextManager.GlobalContext))
                    outputShortcuts.Add(entry);

            if (outputShortcuts.Count == 0)
            {
                foreach (var entry in entries)
                    if (entry.StartsWith(combinationSequence) && ShortcutEntryMatchesAnyContext(entry.context, context)
                        && entry.context == typeof(ContextManager.GlobalContext))
                        outputShortcuts.Add(entry);
            }

            if (tags == null) return;

            bool tagMatch = false;

            foreach (var entry in outputShortcuts)
                tagMatch |= tags.Contains(entry.tag);

            if (tagMatch)
            {
                for (int i = outputShortcuts.Count - 1; i >= 0; i--)
                {
                    if (tags.Contains(outputShortcuts[i].tag)) continue;
                    outputShortcuts.RemoveAt(i);
                }
            }
            else
            {
                for (int i = outputShortcuts.Count - 1; i >= 0; i--)
                {
                    if (outputShortcuts[i].tag == null) continue;
                    outputShortcuts.RemoveAt(i);
                }
            }
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, IContextManager contextManager, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();
            List<ShortcutEntry> entries = GetShortcutEntriesForPrimaryKey(combinationSequence);

            if (entries == null) return;

            foreach (var entry in entries)
                if (entry.StartsWith(combinationSequence) && ShortcutEntrySatisfiesContextManager(contextManager, entry)
                    && entry.context != typeof(ContextManager.GlobalContext))
                    outputShortcuts.Add(entry);

            if (outputShortcuts.Count == 0)
            {
                foreach (var entry in entries)
                    if (entry.StartsWith(combinationSequence) && ShortcutEntrySatisfiesContextManager(contextManager, entry)
                        && entry.context == typeof(ContextManager.GlobalContext))
                        outputShortcuts.Add(entry);
            }

            bool tagMatch = false;
            foreach (var entry in outputShortcuts)
                tagMatch |= contextManager.HasTag(entry.tag);

            if (tagMatch)
            {
                for (int i = outputShortcuts.Count - 1; i >= 0; i--)
                {
                    if (contextManager.HasTag(outputShortcuts[i].tag)) continue;
                    outputShortcuts.RemoveAt(i);
                }
            }
            else
            {
                for (int i = outputShortcuts.Count - 1; i >= 0; i--)
                {
                    if (outputShortcuts[i].tag == null) continue;
                    outputShortcuts.RemoveAt(i);
                }
            }
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, List<ShortcutEntry> outputShortcuts)
        {
            FindShortcutEntries(combinationSequence, (Type[])null, null, outputShortcuts);
        }

        public void FindPotentialShortcutEntries(IContextManager contextManager, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();
            List<ShortcutEntry> entries = new List<ShortcutEntry>();
            GetAllShortcuts(entries);

            if (entries == null) return;

            foreach (var entry in entries)
                if (ShortcutEntrySatisfiesContextManager(contextManager, entry)
                    && (string.IsNullOrWhiteSpace(entry.tag) || contextManager.HasTag(entry.tag)))
                    outputShortcuts.Add(entry);
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

        public void FindPotentialConflicts(Type context, string tag, IList<KeyCombination> binding, IList<ShortcutEntry> output, IContextManager contextManager)
        {
            if (!binding.Any())
                return;

            var tempCombinations = new List<KeyCombination>(binding.Count);
            output.Clear();
            List<ShortcutEntry> entries = m_IndexedShortcutEntries[(int)binding[0].keyCode];
            if (entries == null || !entries.Any())
                return;

            foreach (var shortcutEntry in entries)
            {
                if (!contextManager.DoContextsConflict(shortcutEntry.context, context) || !string.Equals(tag, shortcutEntry.tag))
                    continue;

                bool entryConflicts = false;
                tempCombinations.Clear();

                foreach (var keyCombination in binding)
                {
                    tempCombinations.Add(keyCombination);
                    if (shortcutEntry.FullyMatches(tempCombinations))
                    {
                        entryConflicts = true;
                        break;
                    }
                }

                if (entryConflicts || shortcutEntry.StartsWith(tempCombinations))
                    output.Add(shortcutEntry);
            }
        }

        public void FindShortcutsWithConflicts(List<ShortcutEntry> output, IContextManager contextManager)
        {
            var conflictingEntries = new HashSet<int>();
            foreach (List<ShortcutEntry> entries in m_IndexedShortcutEntries)
            {
                if (entries == null || entries.Count < 2)
                    continue;

                for (int i = 0; i < entries.Count; ++i)
                {
                    var firstEntryHashCode = entries[i].GetHashCode();

                    for (int j = i + 1; j < entries.Count; ++j)
                    {
                        var secondEntryHashCode = entries[j].GetHashCode();

                        if (DoShortcutEntriesConflict(entries[i], entries[j], contextManager))
                        {
                            if (!conflictingEntries.Contains(firstEntryHashCode))
                            {
                                output.Add(entries[i]);
                                conflictingEntries.Add(firstEntryHashCode);
                            }

                            if (!conflictingEntries.Contains(secondEntryHashCode))
                            {
                                output.Add(entries[j]);
                                conflictingEntries.Add(secondEntryHashCode);
                            }
                        }
                    }
                }
            }
        }

        void AddEntry(KeyCode keyCode, ShortcutEntry entry)
        {
            int keyCodeValue = (int)keyCode;
            List<ShortcutEntry> entriesForKeyCode = m_IndexedShortcutEntries[keyCodeValue];
            if (entriesForKeyCode == null)
                m_IndexedShortcutEntries[keyCodeValue] = entriesForKeyCode = new List<ShortcutEntry>();
            entriesForKeyCode.Add(entry);
        }

        bool DoShortcutEntriesConflict(ShortcutEntry shortcutEntry1, ShortcutEntry shortcutEntry2, IContextManager contextManager)
        {
            var contextConflict = contextManager.DoContextsConflict(shortcutEntry1.context, shortcutEntry2.context);
            if (!contextConflict)
                return false;

            var tagConflict = string.Equals(shortcutEntry1.tag, shortcutEntry2.tag);
            if (!tagConflict)
                return false;

            var mouseConflict = shortcutEntry1.StartsWith(shortcutEntry2.combinations, KeyCombination.k_MouseKeyCodes) ||
                shortcutEntry2.StartsWith(shortcutEntry1.combinations, KeyCombination.k_MouseKeyCodes);

            var shortcutsHaveDifferentType = (shortcutEntry1.type == ShortcutType.Action && shortcutEntry2.type == ShortcutType.Clutch) ||
                (shortcutEntry1.type == ShortcutType.Clutch && shortcutEntry2.type == ShortcutType.Action);

            if (shortcutsHaveDifferentType && mouseConflict)
                return false;

            var combinationsConflict = shortcutEntry1.StartsWith(shortcutEntry2.combinations) ||
                shortcutEntry2.StartsWith(shortcutEntry1.combinations);

            return combinationsConflict;
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
                // * Non-menu shortcuts do NOT apply only when the Game View is focused, the editor is playing, and is NOT paused
                // * Menu shortcuts are always active
                (!contextManager.playModeContextIsActive ||
                    entry.type == ShortcutType.Menu);
        }
    }
}
