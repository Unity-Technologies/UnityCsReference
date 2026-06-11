// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor.Importers;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class PasteElementsCommand : Command<PasteElementsCommand>
{
    const string CommandUndoName = "Paste elements";

    public static PasteElementsCommand GetPooled(object source, string copiedContent, VisualElementAsset copyIntoAsset)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.CopiedContent = copiedContent;
        cmd.CopyIntoAsset = copyIntoAsset;
        return cmd;
    }

    public static void Execute(object source, string copiedContent, VisualElementAsset copyIntoAsset)
    {
        using var command = GetPooled(source, copiedContent, copyIntoAsset);
        UICommandQueue.Execute(command);
    }

    public string CopiedContent { get; private set; }
    public VisualElementAsset CopyIntoAsset { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        CopiedContent = null;
        CopyIntoAsset = null;
    }

    public override bool Validate() => CopyIntoAsset != null && CopyIntoAsset.visualTreeAsset != null;

    public override void Prepare(in PrepareContext context)
    {
        var vta = CopyIntoAsset.visualTreeAsset;
        context.RecordUndo(vta);
        context.RecordUndo(vta.inlineSheet);
    }

    public override CommandExecutionStatus Execute()
    {
        using var toSelectNodesHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);
        var importer = new TempVisualTreeAssetImporter();
        importer.ImportXmlFromString(CopiedContent, out var pasteVta);

        try
        {
            for (var i = 0; i < pasteVta.visualTree.childCount; ++i)
                toSelectAssets.Add(pasteVta.visualTree[i] as VisualElementAsset);

            CopyIntoAsset.visualTreeAsset.Swallow(CopyIntoAsset, pasteVta);
        }
        finally
        {
            Object.DestroyImmediate(pasteVta);
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
        return CommandExecutionStatus.Success;
    }
}
