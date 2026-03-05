// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetAttributeOverrideCommand
{
    const string CommandUndoName = "Set attribute override";

    readonly VisualTreeAsset m_VisualTreeAsset;
    readonly UxmlAsset m_UxmlAsset;
    readonly UxmlSerializedAttributeDescription m_AttributeDescription;
    readonly VisualElement m_VisualElement;
    readonly object m_Value;

    public SetAttributeOverrideCommand(
        VisualTreeAsset vta,
        UxmlAsset vea,
        UxmlSerializedAttributeDescription desc,
        VisualElement visualElement,
        object attributeValue)
    {
        m_VisualTreeAsset = vta;
        m_UxmlAsset = vea;
        m_AttributeDescription = desc;
        m_VisualElement = visualElement;
        m_Value = attributeValue;
    }

    public void Execute()
    {
        Assert.IsNotNull(m_VisualTreeAsset);
        Assert.IsNotNull(m_UxmlAsset);
        Assert.IsNotNull(m_AttributeDescription);

        Undo.RegisterCompleteObjectUndo(m_VisualTreeAsset, CommandUndoName);

        if (UxmlAttributeConverter.TryConvertToString(m_Value, m_VisualTreeAsset, out var valueAsString))
        {
            UxmlAssetUtilities.PostAttributeValueChange(
                m_AttributeDescription.name,
                valueAsString,
                m_VisualTreeAsset,
                m_UxmlAsset,
                true,
                m_VisualElement);
        }
        EditorUtility.SetDirty(m_VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(m_VisualTreeAsset);
    }
}
