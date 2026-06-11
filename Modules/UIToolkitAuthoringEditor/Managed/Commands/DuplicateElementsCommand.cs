// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor.Importers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class DuplicateElementsCommand : Command<DuplicateElementsCommand>
{
    const string CommandUndoName = "Duplicate elements";

    public static DuplicateElementsCommand GetPooled(object source, VisualElementAsset[] toDuplicate)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ToDuplicateAssets = toDuplicate;
        return cmd;
    }

    public static void Execute(object source, VisualElementAsset[] toDuplicate)
    {
        using var command = GetPooled(source, toDuplicate);
        UICommandQueue.Execute(command);
    }

    public VisualElementAsset[] ToDuplicateAssets { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Hierarchy;

    protected override void Init()
    {
        base.Init();
        ToDuplicateAssets = null;
    }

    public override bool Validate()
    {
        if (ToDuplicateAssets == null)
            return false;
        foreach (var asset in ToDuplicateAssets)
        {
            if (asset == null || asset.visualTreeAsset == null)
                return false;
        }
        return true;
    }

    public override void Prepare(in PrepareContext context)
    {
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var asset in ToDuplicateAssets)
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

        foreach (var asset in ToDuplicateAssets)
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
        return CommandExecutionStatus.Success;
    }
}
