// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class SetAttributeOverrideCommand : Command<SetAttributeOverrideCommand>
{
    public static SetAttributeOverrideCommand GetPooled(
        object source,
        VisualTreeAsset vta,
        UxmlSerializedAttributeDescription desc,
        VisualElement visualElement,
        object attributeValue)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_VisualTreeAsset = vta;
        cmd.m_AttributeDescription = desc;
        cmd.m_VisualElement = visualElement;
        cmd.m_Value = attributeValue;
        return cmd;
    }

    VisualTreeAsset m_VisualTreeAsset;
    UxmlSerializedAttributeDescription m_AttributeDescription;
    VisualElement m_VisualElement;
    object m_Value;

    public override string UndoName { get; } = "Set attribute override";

    public VisualTreeAsset VisualTreeAsset => m_VisualTreeAsset;
    public UxmlSerializedAttributeDescription AttributeDescription => m_AttributeDescription;
    public VisualElement VisualElement => m_VisualElement;
    public object Value => m_Value;

    protected override void Init()
    {
        base.Init();
        m_VisualTreeAsset = null;
        m_AttributeDescription = null;
        m_VisualElement = null;
        m_Value = null;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(m_VisualTreeAsset);
    }

    public override bool Validate()
    {
        return m_VisualTreeAsset != null && m_AttributeDescription != null;
    }

    public override CommandExecutionStatus Execute()
    {
        if (UxmlAttributeConverter.TryConvertToString(m_Value, m_VisualTreeAsset, out var valueAsString))
        {
            UxmlAssetUtilities.PostAttributeValueChange(
                m_AttributeDescription.name,
                valueAsString,
                m_VisualTreeAsset,
                null,
                true,
                m_VisualElement);

            return CommandExecutionStatus.Success;
        }

        return CommandExecutionStatus.ExecutionFailed;
    }
}
