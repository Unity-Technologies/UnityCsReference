// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// Persists an edit to a component attribute at a template instance as a component override
// (TemplateAsset.componentAttributeOverrides, folded into m_ComponentOverrides). The component analog of
// SetAttributeOverrideCommand; the override is a whole component UxmlSerializedData, so we set the one
// changed attribute on it via the attribute description rather than writing a name/value string.
internal sealed class SetComponentAttributeOverrideCommand : Command<SetComponentAttributeOverrideCommand>
{
    public static SetComponentAttributeOverrideCommand GetPooled(
        object source,
        VisualTreeAsset vta,
        UxmlSerializedDataDescription componentDescription,
        UxmlSerializedAttributeDescription attribute,
        VisualElement element,
        object value)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.m_VisualTreeAsset = vta;
        cmd.m_ComponentDescription = componentDescription;
        cmd.m_Attribute = attribute;
        cmd.m_Element = element;
        cmd.m_Value = value;
        return cmd;
    }

    public static void Execute(
        object source,
        VisualTreeAsset vta,
        UxmlSerializedDataDescription componentDescription,
        UxmlSerializedAttributeDescription attribute,
        VisualElement element,
        object value)
    {
        using var command = GetPooled(source, vta, componentDescription, attribute, element, value);
        UICommandQueue.Execute(command);
    }

    VisualTreeAsset m_VisualTreeAsset;
    UxmlSerializedDataDescription m_ComponentDescription;
    UxmlSerializedAttributeDescription m_Attribute;
    VisualElement m_Element;
    object m_Value;

    public override string UndoName { get; } = "Set component attribute override";

    protected override void Init()
    {
        base.Init();
        m_VisualTreeAsset = null;
        m_ComponentDescription = null;
        m_Attribute = null;
        m_Element = null;
        m_Value = null;
    }

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(m_VisualTreeAsset);
    }

    public override bool Validate()
    {
        return m_VisualTreeAsset != null && m_ComponentDescription != null && m_Attribute != null;
    }

    public override CommandExecutionStatus Execute()
    {
        UxmlAssetUtilities.PostComponentAttributeOverride(
            m_VisualTreeAsset, m_ComponentDescription, m_Attribute, m_Value, m_Element);
        return CommandExecutionStatus.Success;
    }
}
