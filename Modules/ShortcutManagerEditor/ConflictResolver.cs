// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class ConflictResolver : IConflictResolver
    {
        IShortcutProfileManager m_ProfileManager;
        IContextManager m_ContextManager;
        IConflictResolverView m_ConflictResolverView;
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();
        List<object> m_Contexts = new List<object>();
        bool m_UnresolvedConflictPending;


        public ConflictResolver(IShortcutProfileManager profileManager, IContextManager contextManager, IConflictResolverView conflictResolverView)
        {
            m_ProfileManager = profileManager;
            m_ContextManager = contextManager;
            m_ConflictResolverView = conflictResolverView;
        }

        public void ResolveConflict(IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            // Ignore further conflicts while there is an unresolved conflict pending
            if (m_UnresolvedConflictPending)
                return;

            m_UnresolvedConflictPending = true;

            m_Entries.AddRange(entries);

            foreach (var entry in entries)
            {
                m_Contexts.Add(m_ContextManager.GetContextInstanceOfType(entry.context));
            }
            m_ConflictResolverView.Show(this, keyCombinationSequence, m_Entries);
        }

        public void Cancel()
        {
            m_UnresolvedConflictPending = false;

            Cleanup();
        }

        public void ExecuteOnce(ShortcutEntry entry)
        {
            m_UnresolvedConflictPending = false;

            Execute(entry);
            Cleanup();
        }

        public void ExecuteAlways(ShortcutEntry entry)
        {
            m_UnresolvedConflictPending = false;

            foreach (var shortcutEntry in m_Entries)
            {
                if (shortcutEntry != entry)
                {
                    m_ProfileManager.ModifyShortcutEntry(shortcutEntry.identifier, new List<KeyCombination>(0));
                }
            }

            if (entry.type != ShortcutType.Clutch)
                Execute(entry);
            Cleanup();
        }

        public void GoToShortcutManagerConflictCategory()
        {
            ShortcutManagerWindow.ShowConflicts();
        }

        void Execute(ShortcutEntry entry)
        {
            if (entry.type == ShortcutType.Clutch)
                throw new InvalidOperationException("Clutches cannot be activated through conflict resolution");

            var entryIndex = m_Entries.IndexOf(entry);

            var args = new ShortcutArguments();
            args.context = m_Contexts[entryIndex];
            args.stage = ShortcutStage.End;
            entry.action(args);
        }

        void Cleanup()
        {
            m_Entries.Clear();
            m_Contexts.Clear();
        }
    }
}
