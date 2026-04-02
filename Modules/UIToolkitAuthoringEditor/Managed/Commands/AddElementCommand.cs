// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddElementCommand
{
    const string CommandUndoName = "Add element";

    readonly Type ElementType;
    readonly VisualTreeAsset VisualTreeAsset;
    readonly VisualElementAsset ParentVea;
    readonly int Index;

    public AddElementCommand(
        Type elementType,
        VisualTreeAsset visualTreeAsset,
        VisualElementAsset parentVea,
        int index = -1)
    {
        ElementType = elementType;
        VisualTreeAsset = visualTreeAsset;
        ParentVea = parentVea ?? visualTreeAsset.visualTree;
        Index = index;
    }

    public void Execute()
    {
        Assert.IsNotNull(ElementType);
        Assert.IsNotNull(VisualTreeAsset);

        Undo.RegisterCompleteObjectUndo(VisualTreeAsset, CommandUndoName);

        var fullTypeName = ElementType.FullName;
        var vea = VisualTreeAsset.AddElementOfType(ParentVea, fullTypeName);
        vea.serializedData = UxmlSerializedDataCreator.CreateUxmlSerializedData(ElementType);

        if (ParentVea[ParentVea.childCount - 1] is VisualElementAsset newVea)
        {
            HandlePositioning(newVea);
        }

        EditorUtility.SetDirty(VisualTreeAsset);
        using var toSelectNodesHandle = ListPool<VisualElementAsset>.Get(out var toSelectAssets);
        toSelectAssets.Add(vea);
        UIToolkitStageUtility.RequestSelectionOnNextUpdate(toSelectAssets);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(VisualTreeAsset);
    }

    void HandlePositioning(VisualElementAsset newVea)
    {
        if (Index < 0 || Index >= ParentVea.childCount) return;

        VisualTreeAsset.ReparentElementInDocument(newVea, ParentVea, Index);
    }
}
