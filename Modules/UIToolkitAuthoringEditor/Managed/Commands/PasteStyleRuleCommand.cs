// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class PasteStyleRuleCommand : Command<PasteStyleRuleCommand>
{
    const string CommandUndoName = "Paste style rule";

    public static PasteStyleRuleCommand GetPooled(object source, string copiedContent, StyleSheet targetStyleSheet)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.CopiedContent = copiedContent;
        cmd.TargetStyleSheet = targetStyleSheet;
        return cmd;
    }

    public static void Execute(object source, string copiedContent, StyleSheet targetStyleSheet)
    {
        using var command = GetPooled(source, copiedContent, targetStyleSheet);
        UICommandQueue.Execute(command);
    }

    public string CopiedContent { get; private set; }
    public StyleSheet TargetStyleSheet { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.StylingContext;

    protected override void Init()
    {
        base.Init();
        CopiedContent = null;
        TargetStyleSheet = null;
    }

    public override bool Validate() => TargetStyleSheet != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(TargetStyleSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        var pasteStyleSheet = StyleSheetUtility.CreateInstanceWithHideFlags();
        var importer = new TempStyleSheetImporter();
        importer.Import(pasteStyleSheet, CopiedContent);

        TargetStyleSheet.Swallow(pasteStyleSheet);
        Object.DestroyImmediate(pasteStyleSheet);

        return CommandExecutionStatus.Success;
    }
}
