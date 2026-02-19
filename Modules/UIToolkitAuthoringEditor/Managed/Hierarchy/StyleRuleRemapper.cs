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

internal readonly struct StyleRuleRemap
{
    public StyleRuleRemap(StyleRule previous, StyleRule remapped)
    {
        Previous = previous;
        Remapped = remapped;
    }

    public readonly StyleRule Previous;
    public readonly StyleRule Remapped;
}

internal static class StyleRuleRemapper
{
    private readonly struct RemapContext : IEquatable<RemapContext>
    {
        private readonly GUID m_StyleSheet;
        private readonly int m_Line;
        public bool Remappable { get; }

        public RemapContext(StyleRule rule)
        {
            m_StyleSheet = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(rule.styleSheet));
            m_Line = rule.line;
            Remappable = rule.styleSheet && !m_StyleSheet.Empty();
        }

        public bool Equals(RemapContext other)
        {
            return m_StyleSheet == other.m_StyleSheet &&
                m_Line == other.m_Line;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_StyleSheet, m_Line);
        }
    }

    public static void Remap(HashSet<StyleRule> addedOrMoved, HashSet<StyleRule> removed, List<StyleRuleRemap> remappings)
    {
        remappings.Clear();

        using var handle = DictionaryPool<RemapContext, StyleRule>.Get(out var remapContext);
        using var conflictsHandle = DictionaryPool<RemapContext, List<StyleRule>>.Get(out var remapConflicts);

        foreach (var rule in removed)
        {
            if (rule == null)
                continue;
            var remap = new RemapContext(rule);
            if (remap.Remappable)
            {
                if (!remapContext.TryAdd(remap, rule))
                {
                    if (!remapConflicts.TryGetValue(remap, out var conflictList))
                    {
                        remapConflicts.Add(remap, conflictList = new List<StyleRule>{remapContext[remap]});
                    }
                    conflictList.Add(rule);
                }
            }
        }

        // We need to take a deeper look at conflicts. For now, don't remap those.
        foreach (var conflict in remapConflicts.Keys)
        {
            remapContext.Remove(conflict);
        }

        foreach (var rule in addedOrMoved)
        {
            var context = new RemapContext(rule);
            if (!context.Remappable)
                continue;

            if (remapContext.TryGetValue(context, out var p))
            {
                remappings.Add(new StyleRuleRemap(p, rule));
            }
        }

        foreach (var list in remapConflicts.Values)
        {
            ListPool<StyleRule>.Release(list);
        }
    }
}
