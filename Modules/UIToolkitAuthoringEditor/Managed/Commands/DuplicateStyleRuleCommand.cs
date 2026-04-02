// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct DuplicateStyleRuleCommand
{
    const string CommandUndoName = "Duplicate style rule";

    readonly StyleRule[] ToDuplicateStyleRules;

    public DuplicateStyleRuleCommand(StyleRule[] toDuplicate)
    {
        ToDuplicateStyleRules = toDuplicate;
    }

    public void Execute()
    {
        foreach (var styleRule in ToDuplicateStyleRules)
        {
            Assert.IsNotNull(styleRule);
            Assert.IsNotNull(styleRule.styleSheet);
        }

        using var _ = HashSetPool<StyleSheet>.Get(out var dirtyStyleSheets);

        foreach (var originalRule in ToDuplicateStyleRules)
        {
            var styleSheet = originalRule.styleSheet;

            if (dirtyStyleSheets.Add(styleSheet))
                Undo.RegisterCompleteObjectUndo(styleSheet, CommandUndoName);

            var ruleIndex = Array.IndexOf(styleSheet.rules, originalRule);
            if (ruleIndex == -1)
                continue;

            var targetIndex = ruleIndex + 1;

            var newRule = styleSheet.AddRuleAtIndex(targetIndex, null);
            StyleSheetExtensions.SwallowStyleRule(styleSheet, newRule, styleSheet, originalRule);
        }

        foreach (var styleSheet in dirtyStyleSheets)
            EditorUtility.SetDirty(styleSheet);
    }
}
