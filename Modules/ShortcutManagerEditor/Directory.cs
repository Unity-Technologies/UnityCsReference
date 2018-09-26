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
        List<ShortcutEntry> m_ShortcutEntries;

        public Directory(IEnumerable<ShortcutEntry> entries)
        {
            Initialize(entries);
        }

        public void Initialize(IEnumerable<ShortcutEntry> entries)
        {
            m_ShortcutEntries = new List<ShortcutEntry>(entries);
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
    }
}
