// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

internal readonly record struct PasteStyleRuleCommand
{
    const string CommandUndoName = "Paste style rule";

    readonly string CopiedContent;
    readonly StyleSheet TargetStyleSheet;

    public PasteStyleRuleCommand(string copiedContent, StyleSheet targetStyleSheet)
    {
        CopiedContent = copiedContent;
        TargetStyleSheet = targetStyleSheet;
    }

    public void Execute()
    {
        Undo.RegisterCompleteObjectUndo(TargetStyleSheet, CommandUndoName);

        var pasteStyleSheet = StyleSheetUtility.CreateInstanceWithHideFlags();
        var importer = new TempStyleSheetImporter();
        importer.Import(pasteStyleSheet, CopiedContent);

        TargetStyleSheet.Swallow(pasteStyleSheet);
        Object.DestroyImmediate(pasteStyleSheet);

        EditorUtility.SetDirty(TargetStyleSheet);
    }
}
