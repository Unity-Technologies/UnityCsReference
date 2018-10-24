// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;

interface IConflictResolver
{
    void ResolveConflict(IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries);
    void Cancel();
    void ExecuteOnce(ShortcutEntry entry);
    void ExecuteAlways(ShortcutEntry entry);
    void GoToShortcutManagerConflictCategory();
}
