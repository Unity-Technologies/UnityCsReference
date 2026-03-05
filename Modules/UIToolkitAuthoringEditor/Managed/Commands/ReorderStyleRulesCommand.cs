// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
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

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        int idx = InsertIndex;
        for (int i = 0; i < DraggedRules.Count; i++)
        {
            var fromRule = DraggedRules[i];
            var fromStyleSheet = fromRule.styleSheet;
            var newRule = StyleSheet.AddRuleAtIndex(idx++);
            StyleSheetExtensions.SwallowStyleRule(StyleSheet, newRule, fromStyleSheet, fromRule);
            fromStyleSheet.RemoveRule(fromRule);
        }

        EditorUtility.SetDirty(StyleSheet);
        return true;
    }
}
