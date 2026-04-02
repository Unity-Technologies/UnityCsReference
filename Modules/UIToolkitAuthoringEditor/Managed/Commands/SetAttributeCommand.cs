// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetAttributeCommand
{
    const string CommandUndoName = "Set attribute";

    readonly VisualTreeAsset m_VisualTreeAsset;
    readonly UxmlAsset m_UxmlAsset;
    readonly UxmlSerializedData m_UxmlSerializedData;
    readonly UxmlSerializedAttributeDescription m_AttributeDescription;
    readonly object m_Value;

    public SetAttributeCommand(
        VisualTreeAsset vta,
        UxmlAsset vea,
        UxmlSerializedData uxmlSerializedData,
        UxmlSerializedAttributeDescription desc,
        object attributeValue)
    {
        m_VisualTreeAsset = vta;
        m_UxmlAsset = vea;
        m_UxmlSerializedData = uxmlSerializedData;
        m_AttributeDescription = desc;
        m_Value = attributeValue;
    }

    public void Execute()
    {
        Assert.IsNotNull(m_VisualTreeAsset);
        Assert.IsNotNull(m_AttributeDescription);
        string valueAsString = "";

        if (m_Value != null)
        {
            Assert.IsTrue(UxmlAttributeConverter.TryConvertToString(m_Value, m_VisualTreeAsset, out valueAsString),
                $"Value {m_Value} must be convertible to string.");
        }

        Undo.RegisterCompleteObjectUndo(m_VisualTreeAsset, CommandUndoName);

        // Set the attribute value and flag
        m_AttributeDescription.SetSerializedValue(m_UxmlSerializedData, m_Value);
        m_AttributeDescription.SetSerializedValueAttributeFlags(m_UxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

        // Set the attribute value for export.
        if (m_UxmlAsset != null)
        {
            m_UxmlAsset.SetAttribute(m_AttributeDescription.name, valueAsString);
        }
        EditorUtility.SetDirty(m_VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(m_VisualTreeAsset);
    }
}
