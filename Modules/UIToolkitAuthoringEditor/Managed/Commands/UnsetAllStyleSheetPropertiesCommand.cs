// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct UnsetAllStyleSheetPropertiesCommand
{
    const string CommandUndoName = "Unset all style properties";

    readonly StyleSheet StyleSheet;
    readonly StyleRule Rule;

    public UnsetAllStyleSheetPropertiesCommand(
        StyleSheet styleSheet,
        StyleRule rule)
    {
        StyleSheet = styleSheet;
        Rule = rule;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Rule);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        Rule.ClearProperties();

        EditorUtility.SetDirty(StyleSheet);
    }
}
