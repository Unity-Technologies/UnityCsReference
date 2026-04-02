// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RenameStyleRuleCommand
{
    const string CommandUndoName = "Rename style rule";

    readonly string[] NewSelectors;
    readonly StyleRule ToUpdateStyleRule;

    public RenameStyleRuleCommand(string[] newSelectors, StyleRule toUpdateStyleRule)
    {
        NewSelectors = newSelectors;
        ToUpdateStyleRule = toUpdateStyleRule;
    }

    public void Execute()
    {
        Assert.IsNotNull(NewSelectors);
        Assert.IsNotNull(ToUpdateStyleRule);

        Undo.RegisterCompleteObjectUndo(ToUpdateStyleRule.styleSheet, CommandUndoName);

        foreach (var selector in ToUpdateStyleRule.complexSelectors)
        {
            ToUpdateStyleRule.RemoveSelector(selector);
        }

        foreach (var selector in NewSelectors)
        {
            if (ToUpdateStyleRule.TryAddSelector(selector, out _))
                continue;

            return;
        }

        EditorUtility.SetDirty(ToUpdateStyleRule.styleSheet);
    }
}
