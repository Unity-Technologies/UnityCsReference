// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.ShortcutManagement
{
    interface IDirectory
    {
        void GetAllShortcuts(List<ShortcutEntry> output);

        void FindShortcutEntries(List<KeyCombination> combinationSequence, Type[] context, string[] tags, List<ShortcutEntry> outputShortcuts);
        void FindShortcutEntries(List<KeyCombination> combinationSequence, IContextManager contextManager, List<ShortcutEntry> outputShortcuts);
        void FindShortcutEntries(List<KeyCombination> combinationSequence, List<ShortcutEntry> outputShortcuts);
        ShortcutEntry FindShortcutEntry(Identifier identifier);
        ShortcutEntry FindShortcutEntry(string identifier);

        void FindPotentialConflicts(Type context, string tag, IList<KeyCombination> binding, IList<ShortcutEntry> output, IContextManager contextManager);

        void FindShortcutsWithConflicts(List<ShortcutEntry> output, IContextManager contextManager);
    }
}
