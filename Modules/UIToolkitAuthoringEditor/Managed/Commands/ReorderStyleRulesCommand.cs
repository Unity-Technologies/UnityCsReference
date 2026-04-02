// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct ReorderStyleRulesCommand
{
    const string CommandUndoName = "Reorder style rules";

    readonly StyleSheet StyleSheet;
    readonly IReadOnlyList<StyleRule> DraggedRules;
    readonly int InsertIndex;

    public ReorderStyleRulesCommand(StyleSheet toStyleSheet, IReadOnlyList<StyleRule> rules, int insertIdx)
    {
        StyleSheet = toStyleSheet;
        DraggedRules = rules;
        InsertIndex = insertIdx;
    }

    public bool Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(DraggedRules);

        if (DraggedRules.Count == 0)
            return false;

        if (InsertIndex != -1 && (InsertIndex < 0 || InsertIndex > StyleSheet.rules.Length))
        {
            Debug.LogError( $"Target index must be between -1 (append) and {StyleSheet.rules.Length}, but was {InsertIndex}.");
            return false;
        }

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var targetIndex = InsertIndex == -1 ? StyleSheet.rules.Length : InsertIndex;

        // Copy rules data before removing in order to preserve their references
        using var _ = ListPool<(StyleSheet sourceSheet, StyleRule sourceRule)>.Get(out var rulesToCopy);
        foreach (var rule in DraggedRules)
        {
            rulesToCopy.Add((rule.styleSheet, rule));
        }

        // Remove all rules first since dragging multiple rules within the same stylesheet will drastically change the rules order
        foreach (var rule in DraggedRules)
        {
            rule.styleSheet.RemoveRule(rule);
        }

        targetIndex = Math.Min(targetIndex, StyleSheet.rules.Length);
        foreach (var (fromStyleSheet, fromRule) in rulesToCopy)
        {
            var newRule = StyleSheet.AddRuleAtIndex(targetIndex++);
            StyleSheetExtensions.SwallowStyleRule(StyleSheet, newRule, fromStyleSheet, fromRule);
        }

        EditorUtility.SetDirty(StyleSheet);
        return true;
    }
}
