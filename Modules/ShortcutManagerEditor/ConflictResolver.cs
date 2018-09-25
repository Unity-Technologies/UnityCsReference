// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class ConflictNotificationConsole : IConflictResolver
    {
        public void ResolveConflict(IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            var builder = new StringBuilder();

            builder.Append($"Shortcut conflict detected for key binding {KeyCombination.SequenceToString(keyCombinationSequence)}.\n");
            builder.Append("Please resolve the conflict by rebinding one or more of the following shortcuts:\n");

            foreach (var entry in entries)
                builder.Append($"{entry.identifier.path} ({KeyCombination.SequenceToString(entry.combinations)})\n");

            Debug.Log(builder.ToString());
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void ExecuteOnce(ShortcutEntry entry)
        {
            throw new NotImplementedException();
        }

        public void ExecuteAlways(ShortcutEntry entry)
        {
            throw new NotImplementedException();
        }
    }

    class ConflictResolver : IConflictResolver
    {
        IShortcutProfileManager m_ProfileManager;
        IContextManager m_ContextManager;
        IConflictResolverView m_ConflictResolverView;
        List<ShortcutEntry> m_Entries = new List<ShortcutEntry>();


        public ConflictResolver(IShortcutProfileManager profileManager, IContextManager contextManager, IConflictResolverView conflictResolverView)
        {
            m_ProfileManager = profileManager;
            m_ContextManager = contextManager;
            m_ConflictResolverView = conflictResolverView;
        }

        public void ResolveConflict(IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            m_Entries.AddRange(entries);
            m_ConflictResolverView.Show(this, keyCombinationSequence, m_Entries);
        }

        public void Cancel()
        {
            Cleanup();
        }

        public void ExecuteOnce(ShortcutEntry entry)
        {
            Execute(entry);
            Cleanup();
        }

        public void ExecuteAlways(ShortcutEntry entry)
        {
            foreach (var shortcutEntry in m_Entries)
            {
                if (shortcutEntry != entry)
                {
                    m_ProfileManager.ModifyShortcutEntry(shortcutEntry.identifier, new List<KeyCombination>(0));
                }
            }

            Execute(entry);
            Cleanup();
        }

        void Execute(ShortcutEntry entry)
        {
            if (entry.type == ShortcutType.Clutch)
                throw new InvalidOperationException("Clutches cannot be activated through conflict resolution");

            var args = new ShortcutArguments();
            args.context = m_ContextManager.GetContextInstanceOfType(entry.context);
            args.state = ShortcutState.End;
            entry.action(args);
        }

        void Cleanup()
        {
            m_Entries.Clear();
        }
    }
}
