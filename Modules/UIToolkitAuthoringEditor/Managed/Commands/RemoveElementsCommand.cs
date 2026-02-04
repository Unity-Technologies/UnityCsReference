// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct RemoveElementsCommand
{
    const string CommandUndoName = "Delete elements";

    readonly VisualElementAsset[] ToRemoveAssets;

    public RemoveElementsCommand(VisualElementAsset[] toRemoveAssets)
    {
        ToRemoveAssets = toRemoveAssets;
    }

    public void Execute()
    {
        foreach (var asset in ToRemoveAssets)
        {
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.visualTreeAsset);
        }

        using var _ = HashSetPool<VisualTreeAsset>.Get(out var set);
        foreach (var asset in ToRemoveAssets)
        {
            var vta = asset.visualTreeAsset;
            if (vta && set.Add(vta))
                Undo.RegisterCompleteObjectUndo(asset.visualTreeAsset, CommandUndoName);
            asset.RemoveFromHierarchy();
        }

        foreach (var vta in set)
            EditorUtility.SetDirty(vta);
    }
}
