// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveStyleSheetImportCommand
{
    const string CommandUndoName = "Remove import from stylesheet";

    readonly StyleSheet StyleSheet;
    readonly int Index;

    public RemoveStyleSheetImportCommand(StyleSheet styleSheet, int index)
    {
        StyleSheet = styleSheet;
        Index = index;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);
        Assert.IsTrue(Index >= 0 && Index < StyleSheet.imports.Length);

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        StyleSheet.RemoveImport(Index);

        EditorUtility.SetDirty(StyleSheet);
    }
}
