// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveStyleRulesCommand
{
    const string CommandUndoName = "Remove style rules";

    readonly StyleRule[] ToRemoveStyleRules;

    public RemoveStyleRulesCommand(StyleRule[] toRemoveStyleRules)
    {
        ToRemoveStyleRules = toRemoveStyleRules;
    }

    public void Execute()
    {
        Assert.IsTrue(ToRemoveStyleRules.Length > 0);

        foreach (var rule in ToRemoveStyleRules)
        {
            Assert.IsNotNull(rule.styleSheet);
        }

        using var _ = HashSetPool<StyleSheet>.Get(out var set);
        foreach (var rule in ToRemoveStyleRules)
        {
            var styleSheet = rule.styleSheet;
            if (styleSheet && set.Add(styleSheet))
                Undo.RegisterCompleteObjectUndo(rule.styleSheet, CommandUndoName);

            styleSheet.RemoveRule(rule);
        }

        foreach (var styleSheet in set)
            EditorUtility.SetDirty(styleSheet);
    }
}
