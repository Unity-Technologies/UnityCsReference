// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddStyleRuleCommand
{
    const string CommandUndoName = "Add style rule";

    readonly StyleSheet StyleSheet;
    readonly string SelectorString;

    public AddStyleRuleCommand(StyleSheet styleSheet, string selectorString)
    {
        StyleSheet = styleSheet;
        SelectorString = selectorString;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(SelectorString);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var styleRule = StyleSheet.AddRule();
        styleRule.AddSelector(SelectorString);

        EditorUtility.SetDirty(StyleSheet);
    }
}
