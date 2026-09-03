// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

internal sealed class DuplicateElementsCommand : Command<DuplicateElementsCommand>
{
    const string CommandUndoName = "Duplicate elements";

    public VisualElementAsset[] ElementsToDuplicate { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    public static DuplicateElementsCommand GetPooled(object source, VisualElementAsset[] toDuplicate)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ElementsToDuplicate = toDuplicate;
        return cmd;
    }

    public static bool Validate(object source, VisualElementAsset[] elements)
    {
        using var cmd = GetPooled(source, elements);
        return cmd.Validate();
    }

    public static bool Validate(ReadOnlySpan<VisualElementAsset> elements)
    {
        if (elements.IsEmpty)
            return false;
        foreach (var element in elements)
        {
            if (element == null || element.visualTreeAsset == null)
                return false;
        }
        return true;
    }

    public static CommandExecutionStatus Execute(object source, VisualElementAsset[] toDuplicate)
    {
        using var command = GetPooled(source, toDuplicate);
        return UICommandQueue.Execute(command).Status;
    }

    protected override void Init()
    {
        base.Init();
        ElementsToDuplicate = null;
    }

    public override bool Validate()
        => Validate(ElementsToDuplicate.AsSpan());

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var asset in ElementsToDuplicate)
        {
            if (set.Add(asset.visualTreeAsset))
                context.RecordUndo(asset.visualTreeAsset);
        }
    }

    public override CommandExecutionStatus Execute()
    {
        var exporter = VisualTreeAssetExporter.Default;
        using var exportListHandle = ListPool<UxmlAsset>.Get(out var exportList);
        using var toSelectNodesHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);

        foreach (var asset in ElementsToDuplicate)
        {
            exportList.Clear();
            exportList.Add(asset);
            var export = exporter.ToUxmlString(asset.visualTreeAsset, exportList);
            var importer = new TempVisualTreeAssetImporter();
            importer.ImportXmlFromString(export, out var pasteVta);

            try
            {
                var parentAsset = asset.parentAsset;
                var index = parentAsset.IndexOf(asset);

                var vea = pasteVta.visualTree[0] as VisualElementAsset;
                toSelectAssets.Add(vea);
                parentAsset.Insert(index + 1, vea);
            }
            finally
            {
                Object.DestroyImmediate(pasteVta);
            }
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
        Clipboard.GetClipboardForStage()?.ClearCutElements();
        return CommandExecutionStatus.Success;
    }
}
