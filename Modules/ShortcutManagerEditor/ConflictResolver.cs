// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    class ConflictResolver : IConflictResolver
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
    }
}
