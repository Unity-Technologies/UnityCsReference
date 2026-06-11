// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class SetAttributeCommand : Command<SetAttributeCommand>
{
    const string CommandUndoName = "Set attribute";

    public static SetAttributeCommand GetPooled(
        object source,
        VisualTreeAsset vta,
        UxmlAsset vea,
        UxmlSerializedData uxmlSerializedData,
        UxmlSerializedAttributeDescription desc,
        object attributeValue)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = vta;
        cmd.UxmlAsset = vea;
        cmd.UxmlSerializedData = uxmlSerializedData;
        cmd.AttributeDescription = desc;
        cmd.Value = attributeValue;
        return cmd;
    }

    public static void Execute(object source,
        VisualTreeAsset vta,
        UxmlAsset vea,
        UxmlSerializedData uxmlSerializedData,
        UxmlSerializedAttributeDescription desc,
        object attributeValue)
    {
        using var command = GetPooled(source, vta, vea, uxmlSerializedData, desc, attributeValue);
        UICommandQueue.Execute(command);
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public UxmlAsset UxmlAsset { get; private set; }
    public UxmlSerializedData UxmlSerializedData { get; private set; }
    public UxmlSerializedAttributeDescription AttributeDescription { get; private set; }
    public object Value { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Attributes;

    protected override void Init()
    {
        base.Init();
        VisualTreeAsset = null;
        UxmlAsset = null;
        UxmlSerializedData = null;
        AttributeDescription = null;
        Value = null;
    }

    public override bool Validate() => VisualTreeAsset != null && AttributeDescription != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        string valueAsString = "";

        if (Value != null)
        {
            Assert.IsTrue(UxmlAttributeConverter.TryConvertToString(Value, VisualTreeAsset, out valueAsString),
                $"Value {Value} must be convertible to string.");
        }

        // Set the attribute value and flag
        AttributeDescription.SetSerializedValue(UxmlSerializedData, Value);
        AttributeDescription.SetSerializedValueAttributeFlags(UxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

        // Set the attribute value for export.
        if (UxmlAsset != null)
            UxmlAsset.SetAttribute(AttributeDescription.name, valueAsString);

        return CommandExecutionStatus.Success;
    }
}
