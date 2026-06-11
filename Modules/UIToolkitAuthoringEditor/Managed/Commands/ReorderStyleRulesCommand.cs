// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class ReorderStyleRulesCommand : Command<ReorderStyleRulesCommand>
{
    const string CommandUndoName = "Reorder style rules";

    public static ReorderStyleRulesCommand GetPooled(object source, StyleSheet toStyleSheet, IReadOnlyList<StyleRule> rules, int insertIndex)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.StyleSheet = toStyleSheet;
        cmd.DraggedRules = rules;
        cmd.InsertIndex = insertIndex;
        return cmd;
    }

    public static void Execute(object source, StyleSheet toStyleSheet, IReadOnlyList<StyleRule> rules, int insertIndex)
    {
        using var command = GetPooled(source, toStyleSheet, rules, insertIndex);
        UICommandQueue.Execute(command);
    }

    public StyleSheet StyleSheet { get; private set; }
    public IReadOnlyList<StyleRule> DraggedRules { get; private set; }
    public int InsertIndex { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        StyleSheet = null;
        DraggedRules = null;
        InsertIndex = -1;
    }

    public override bool Validate()
    {
        if (StyleSheet == null || DraggedRules == null || DraggedRules.Count == 0)
            return false;

        if (InsertIndex != -1 && (InsertIndex < 0 || InsertIndex > StyleSheet.rules.Length))
        {
            Debug.LogError($"Target index must be between -1 (append) and {StyleSheet.rules.Length}, but was {InsertIndex}.");
            return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(StyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var targetIndex = InsertIndex == -1 ? StyleSheet.rules.Length : InsertIndex;

        // Copy rules data before removing in order to preserve their references
        using var _ = ListPool<(StyleSheet sourceSheet, StyleRule sourceRule)>.Get(out var rulesToCopy);
        foreach (var rule in DraggedRules)
            rulesToCopy.Add((rule.styleSheet, rule));

        // Remove all rules first since dragging multiple rules within the same stylesheet will drastically change the rules order
        foreach (var rule in DraggedRules)
            rule.styleSheet.RemoveRule(rule);

        targetIndex = Math.Min(targetIndex, StyleSheet.rules.Length);
        foreach (var (fromStyleSheet, fromRule) in rulesToCopy)
        {
            var newRule = StyleSheet.AddRuleAtIndex(targetIndex++);
            StyleSheetExtensions.SwallowStyleRule(StyleSheet, newRule, fromStyleSheet, fromRule);
        }

        return CommandExecutionStatus.Success;
    }
}
