// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.UIToolkit.Editor.Importers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct DuplicateElementsCommand
{
    const string CommandUndoName = "Duplicate elements";

    readonly VisualElementAsset[] ToDuplicateAssets;

    public DuplicateElementsCommand(VisualElementAsset[] toDuplicate)
    {
        ToDuplicateAssets = toDuplicate;
    }

    public void Execute()
    {
        foreach (var asset in ToDuplicateAssets)
        {
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.visualTreeAsset);
        }

        var exporter = new VisualTreeAssetExporter();
        using var exportListHandle = ListPool<UxmlAsset>.Get(out var exportList);
        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        using var toSelectNodesHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);
        foreach (var asset in ToDuplicateAssets)
        {
            if (set.Add(asset.visualTreeAsset))
                Undo.RegisterCompleteObjectUndo(asset.visualTreeAsset, CommandUndoName);

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

        foreach (var vta in set)
        {
            EditorUtility.SetDirty(vta);
            UIElementsUtility.MarkVisualTreeAssetAsChanged(vta);
        }

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
    }
}
