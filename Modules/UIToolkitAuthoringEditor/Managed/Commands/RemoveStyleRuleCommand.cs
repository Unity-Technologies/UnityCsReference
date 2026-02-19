// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveStyleRuleCommand
{
    const string CommandUndoName = "Remove style rule";

    readonly StyleSheet StyleSheet;
    readonly StyleComplexSelector StyleComplexSelector;

    public RemoveStyleRuleCommand(StyleSheet styleSheet, StyleComplexSelector styleComplexSelector)
    {
        StyleSheet = styleSheet;
        StyleComplexSelector = styleComplexSelector;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(StyleComplexSelector);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        if (StyleSheet.RemoveRule(StyleComplexSelector.rule))
        {
            EditorUtility.SetDirty(StyleSheet);
        }
    }
}
