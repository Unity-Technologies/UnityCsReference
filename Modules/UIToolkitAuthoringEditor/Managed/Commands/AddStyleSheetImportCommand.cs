// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddStyleSheetImportCommand
{
    const string CommandUndoName = "Add import to stylesheet";

    readonly StyleSheet StyleSheet;

    public AddStyleSheetImportCommand(StyleSheet styleSheet)
    {
        StyleSheet = styleSheet;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        StyleSheet.AddImportAtIndex(-1, new StyleSheet.ImportStruct());

        EditorUtility.SetDirty(StyleSheet);
    }
}
