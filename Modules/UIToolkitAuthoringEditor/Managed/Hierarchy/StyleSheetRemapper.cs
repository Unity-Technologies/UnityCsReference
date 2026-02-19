// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly struct StyleSheetRemap
{
    public StyleSheetRemap(StyleSheet previous, StyleSheet remapped)
    {
        Previous = previous;
        Remapped = remapped;
    }

    public readonly StyleSheet Previous;
    public readonly StyleSheet Remapped;
}

/// <summary>
/// When a StyleSheet file is reloaded (e.g., after a script reload or asset reimport):
/// - The old StyleSheet instance is in the "removed" set
/// - The new StyleSheet instance is in the "added" set
/// - The remapper matches them by GUID and name
/// - Creates a StyleSheetRemap(oldInstance, newInstance)
/// - The selection handler can use this to update the StyleSheetSelection ScriptableObjects to point to the new instance
/// This maintains stable selection across domain reloads, keeping the inspector and hierarchy in sync even when assets are reloaded.
/// </summary>
internal static class StyleSheetRemapper
{
    private readonly struct RemapContext : IEquatable<RemapContext>
    {
        private readonly GUID m_StyleSheet;
        public bool Remappable { get; }

        public RemapContext(StyleSheet styleSheet)
        {
            m_StyleSheet = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(styleSheet));
            Remappable = styleSheet && !m_StyleSheet.Empty();
        }

        public bool Equals(RemapContext other)
        {
            return m_StyleSheet == other.m_StyleSheet;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_StyleSheet);
        }
    }

    public static void Remap(HashSet<StyleSheet> addedOrMoved, HashSet<StyleSheet> removed, List<StyleSheetRemap> remappings)
    {
        remappings.Clear();

        using var handle = DictionaryPool<RemapContext, StyleSheet>.Get(out var remapContext);
        using var conflictsHandle = DictionaryPool<RemapContext, List<StyleSheet>>.Get(out var remapConflicts);

        foreach (var styleSheet in removed)
        {
            if (styleSheet == null)
                continue;
            var remap = new RemapContext(styleSheet);
            if (remap.Remappable)
            {
                if (!remapContext.TryAdd(remap, styleSheet))
                {
                    if (!remapConflicts.TryGetValue(remap, out var conflictList))
                    {
                        remapConflicts.Add(remap, conflictList = new List<StyleSheet>{remapContext[remap]});
                    }
                    conflictList.Add(styleSheet);
                }
            }
        }

        // We need to take a deeper look at conflicts. For now, don't remap those.
        foreach (var conflict in remapConflicts.Keys)
        {
            remapContext.Remove(conflict);
        }

        foreach (var styleSheet in addedOrMoved)
        {
            var context = new RemapContext(styleSheet);
            if (!context.Remappable)
                continue;

            if (remapContext.TryGetValue(context, out var p))
            {
                remappings.Add(new StyleSheetRemap(p, styleSheet));
            }
        }

        foreach (var list in remapConflicts.Values)
        {
            ListPool<StyleSheet>.Release(list);
        }
    }
}
