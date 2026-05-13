// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetStyleSheetImportCommand
{
    const string CommandUndoName = "Change stylesheet import";

    readonly StyleSheet StyleSheet;
    readonly int Index;
    readonly StyleSheet ImportedStyleSheet;

    public SetStyleSheetImportCommand(StyleSheet styleSheet, int index, StyleSheet importedStyleSheet)
    {
        StyleSheet = styleSheet;
        Index = index;
        ImportedStyleSheet = importedStyleSheet;
    }

    public void Execute()
    {
        Assert.IsNotNull(StyleSheet);

        if (ImportedStyleSheet == null)
            return;

        Undo.RegisterCompleteObjectUndo(StyleSheet, CommandUndoName);

        StyleSheet.SetStyleSheetImportAtIndex(Index, ImportedStyleSheet);

        EditorUtility.SetDirty(StyleSheet);
    }
}
