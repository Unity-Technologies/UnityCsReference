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

internal readonly record struct PasteElementsCommand
{
    const string CommandUndoName = "Paste elements";

    readonly string CopiedContent;
    readonly VisualElementAsset CopyIntoAsset;

    public PasteElementsCommand(string copiedContent, VisualElementAsset copyIntoAsset)
    {
        CopiedContent = copiedContent;
        CopyIntoAsset = copyIntoAsset;
    }

    public void Execute()
    {
        Assert.IsNotNull(CopyIntoAsset);
        Assert.IsNotNull(CopyIntoAsset.visualTreeAsset);

        Undo.RegisterCompleteObjectUndo(CopyIntoAsset.visualTreeAsset, CommandUndoName);

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

        EditorUtility.SetDirty(CopyIntoAsset.visualTreeAsset);
        EditorUtility.SetDirty(CopyIntoAsset.visualTreeAsset.inlineSheet);

        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
    }
}
