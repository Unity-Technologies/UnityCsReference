// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetElementNameCommand
{
    const string CommandUndoName = "Rename element";

    readonly VisualElementAsset ElementAsset;
    readonly string Name;

    public SetElementNameCommand(VisualElementAsset elementVea, string name)
    {
        ElementAsset = elementVea;
        Name = name;
    }

    public void Execute()
    {
        Assert.IsNotNull(ElementAsset);

        var visualTreeAsset = ElementAsset.visualTreeAsset;
        Assert.IsNotNull(visualTreeAsset);

        Undo.RegisterCompleteObjectUndo(visualTreeAsset, CommandUndoName);

        var uxmlTypeDescription = UxmlDescriptionRegistry.GetDescription(ElementAsset.serializedData.GetType());
        var nameIndex = uxmlTypeDescription.cSharpNameToIndex[nameof(VisualElement.name)];
        var nameAttribute = uxmlTypeDescription.attributeDescriptions[nameIndex];
        nameAttribute.serializedField.SetValue(ElementAsset.serializedData, Name);
        nameAttribute.serializedFieldAttributeFlags.SetValue(ElementAsset.serializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

        ElementAsset.SetAttribute(nameof(VisualElement.name), Name);
        EditorUtility.SetDirty(visualTreeAsset);
    }
}
