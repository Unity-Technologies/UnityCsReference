// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct AddElementCommand
{
    const string CommandUndoName = "Add element";

    readonly Type ElementType;
    readonly UIDocument UIDocument;

    public AddElementCommand(Type elementType, UIDocument uiDocument)
    {
        ElementType = elementType;
        UIDocument = uiDocument;
    }

    public void Execute()
    {
        Assert.IsNotNull(ElementType);
        Assert.IsNotNull(UIDocument);
        Assert.IsNotNull(UIDocument.visualTreeAsset);

        var visualTreeAsset = UIDocument.visualTreeAsset;
        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        var fullTypeName = ElementType.FullName;
        var vea = visualTreeAsset.AddElementOfType(visualTreeAsset.visualTree, fullTypeName);

        // Create and attach the serialized data
        var serializedData = UxmlSerializedDataCreator.CreateUxmlSerializedData(ElementType);
        vea.serializedData = serializedData;

        Undo.RegisterCompleteObjectUndo(UIDocument, CommandUndoName);

        // Instantiate the element from the serialized data
        var element = serializedData.CreateInstance() as VisualElement;
        serializedData.Deserialize(element);

        UIDocument.rootVisualElement.Add(element);

        EditorUtility.SetDirty(visualTreeAsset);
        EditorUtility.SetDirty(UIDocument);
    }
}
