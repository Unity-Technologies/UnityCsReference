// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct DuplicateStyleRulePropertyCommand
{
    const string CommandUndoName = "Duplicate style property";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;
    readonly StyleProperty SourceProperty;
    readonly string NewName;

    public DuplicateStyleRulePropertyCommand(
        StyleSheet styleSheet,
        StyleRule rule,
        StyleProperty sourceProperty,
        string newName)
    {
        StyleSheet = styleSheet;
        Rule = rule;
        SourceProperty = sourceProperty;
        NewName = newName;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);
        Assert.IsNotNull(SourceProperty);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        var newProperty = Rule.AddProperty(NewName);
        StyleSheetUtility.TransferStylePropertyHandles(StyleSheet, SourceProperty, StyleSheet, newProperty);
        EditorUtility.SetDirty(StyleSheet);
    }
}
