// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct ReparentElementsCommand
{
    const string CommandUndoName = "Reparent elements";

    readonly VisualElementAsset ParentAsset;
    readonly int Index;
    readonly VisualElementAsset[] ChildrenAssetsAssets;

    public ReparentElementsCommand(VisualElementAsset parentAsset, int index, VisualElementAsset[] childrenAssets)
    {
        ParentAsset = parentAsset;
        Index = index;
        ChildrenAssetsAssets = childrenAssets;
    }

    public void Execute()
    {
        Assert.IsNotNull(ParentAsset);
        Assert.IsTrue(Index >= -1 && Index <= ParentAsset.childCount);
        foreach (var asset in ChildrenAssetsAssets)
            Assert.IsNotNull(asset);

        var visualTreeAsset = ParentAsset.visualTreeAsset;
        Assert.IsNotNull(visualTreeAsset);

        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var asset in ChildrenAssetsAssets)
        {
            var vta = asset.visualTreeAsset;
            if (vta && vta != visualTreeAsset)
                set.Add(vta);
        }

        foreach (var vta in set)
            Undo.RegisterCompleteObjectUndo(vta, CommandUndoName);

        var inlineStyleSheet = visualTreeAsset.GetOrCreateInlineStyleSheet();
        Undo.RegisterCompleteObjectUndo(inlineStyleSheet, CommandUndoName);

        for (var i = 0; i < ChildrenAssetsAssets.Length; ++i)
        {
            var asset = ChildrenAssetsAssets[i];
            if (Index < 0)
            {
                ParentAsset.Add(asset);
                continue;
            }
            var index = Index + i;
            ParentAsset.Insert(index, asset);
        }

        EditorUtility.SetDirty(visualTreeAsset);
        foreach (var vta in set)
            EditorUtility.SetDirty(vta);
    }
}
