// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEditor.ShortcutManagement
{
    class Directory : IDirectory
    {
        List<ShortcutEntry> m_ShortcutEntries;

        public Directory(IEnumerable<ShortcutEntry> entries)
        {
            Initialize(entries);
        }

        public void Initialize(IEnumerable<ShortcutEntry> entries)
        {
            m_ShortcutEntries = new List<ShortcutEntry>(entries);
        }

        public void GetAllShortcuts(List<ShortcutEntry> output)
        {
            output.AddRange(m_ShortcutEntries);
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, Type[] context, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();

            Assert.IsNotNull(combinationSequence);
            Assert.IsTrue(combinationSequence.Count > 0, "Sequence can not be empty");

            foreach (var shortcutEntry in m_ShortcutEntries)
            {
                if (shortcutEntry.StartsWith(combinationSequence))
                {
                    if (context != null && Array.FindIndex(context, t => t == shortcutEntry.context || shortcutEntry.context != null && shortcutEntry.context.IsAssignableFrom(t)) < 0)
                        continue;

                    outputShortcuts.Add(shortcutEntry);
                }
            }
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, IContextManager contextManager, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();

            Assert.IsNotNull(combinationSequence);
            Assert.IsTrue(combinationSequence.Count > 0, "Sequence can not be empty");

            foreach (var shortcutEntry in m_ShortcutEntries)
            {
                if (shortcutEntry.StartsWith(combinationSequence))
                {
                    if (!contextManager.HasActiveContextOfType(shortcutEntry.context))
                        continue;
                    if (shortcutEntry.type != ShortcutType.Menu && contextManager.playModeContextIsActive)
                        // Emulate old play mode shortcut behavior
                        // * Menu shortcuts are always active
                        // * Non-menu shortcuts only apply when the game view does not have focus
                        continue;

                    outputShortcuts.Add(shortcutEntry);
                }
            }
        }

        public void FindShortcutEntries(List<KeyCombination> combinationSequence, List<ShortcutEntry> outputShortcuts)
        {
            outputShortcuts.Clear();

            Assert.IsNotNull(combinationSequence);
            Assert.IsTrue(combinationSequence.Count > 0, "Sequence can not be empty");

            foreach (var shortcutEntry in m_ShortcutEntries)
            {
                if (shortcutEntry.StartsWith(combinationSequence))
                {
                    outputShortcuts.Add(shortcutEntry);
                }
            }
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

        public void FindPotentialConflicts(Type context, IList<KeyCombination> binding, IList<ShortcutEntry> output, IContextManager contextManager)
        {
            var tempCombinations = new List<KeyCombination>(binding.Count);
            output.Clear();
            var hashSet = new HashSet<ShortcutEntry>();

            for (var i = 0; i < binding.Count; ++i)
            {
                tempCombinations.Add(binding[i]);
                foreach (var shortcutEntry in m_ShortcutEntries)
                {
                    if (hashSet.Contains(shortcutEntry))
                        continue;

                    if (!contextManager.DoContextsConflict(shortcutEntry.context, context))
                    {
                        continue;
                    }

                    if (shortcutEntry.FullyMatches(tempCombinations))
                    {
                        output.Add(shortcutEntry);
                        hashSet.Add(shortcutEntry);
                    }
                    else if (i == binding.Count - 1)
                    {
                        if (shortcutEntry.StartsWith(tempCombinations))
                        {
                            output.Add(shortcutEntry);
                            hashSet.Add(shortcutEntry);
                        }
                    }
                }
            }
        }

        public void FindShortcutsWithConflicts(List<ShortcutEntry> output, IContextManager contextManager)
        {
            var conflicts = new HashSet<ShortcutEntry>();
            for (var i = 0; i < m_ShortcutEntries.Count; ++i)
            {
                var shortcutEntry = m_ShortcutEntries[i];
                if (!shortcutEntry.combinations.Any())
                    continue;

                for (var j = i + 1; j < m_ShortcutEntries.Count; ++j)
                {
                    var shortcutEntry2 = m_ShortcutEntries[j];
                    if (!shortcutEntry2.combinations.Any())
                        continue;

                    if (DoShortcutEntriesConflict(shortcutEntry, shortcutEntry2, contextManager))
                    {
                        conflicts.Add(shortcutEntry);
                        conflicts.Add(shortcutEntry2);
                    }
                }
            }
            output.AddRange(conflicts);
        }

        bool DoShortcutEntriesConflict(ShortcutEntry shortcutEntry1, ShortcutEntry shortcutEntry2, IContextManager contextManager)
        {
            if (!contextManager.DoContextsConflict(shortcutEntry1.context, shortcutEntry2.context))
                return false;

            //TODO: avoid those ToList
            if (shortcutEntry1.StartsWith(shortcutEntry2.combinations.ToList()))
                return true;

            if (shortcutEntry2.StartsWith(shortcutEntry1.combinations.ToList()))
                return true;

            return false;
        }
    }
}
