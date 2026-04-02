// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RenameStyleRulePropertyCommand
{
    const string CommandUndoName = "Rename style rule property";

    readonly StyleSheet StyleSheet;
    readonly StyleProperty Property;
    readonly string NewName;

    public RenameStyleRulePropertyCommand(StyleSheet styleSheet, StyleProperty property, string newName)
    {
        StyleSheet = styleSheet;
        Property = property;
        NewName = newName;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsNotNull(Property);
        Assert.IsNotNull(NewName);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        Property.name = NewName;

        EditorUtility.SetDirty(StyleSheet);
    }
}
