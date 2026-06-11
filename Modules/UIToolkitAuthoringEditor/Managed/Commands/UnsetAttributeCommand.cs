// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class UnsetAttributeCommand : Command<UnsetAttributeCommand>
{
    const string CommandUndoName = "Unset attribute";

    public static UnsetAttributeCommand GetPooled(
        object source,
        VisualTreeAsset vta,
        UxmlAsset attributeUxmlOwner,
        UxmlSerializedData ownerSerializedData,
        UxmlSerializedAttributeDescription desc,
        VisualElement visualElement,
        string bindingPath,
        bool isInTemplateInstance,
        bool removeBinding)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = vta;
        cmd.AttributeUxmlOwner = attributeUxmlOwner;
        cmd.OwnerSerializedData = ownerSerializedData;
        cmd.AttributeDescription = desc;
        cmd.VisualElement = visualElement;
        cmd.BindingPath = bindingPath;
        cmd.IsInTemplateInstance = isInTemplateInstance;
        cmd.RemoveBinding = removeBinding;
        return cmd;
    }

    public static void Execute(object source,
        VisualTreeAsset vta,
        UxmlAsset attributeUxmlOwner,
        UxmlSerializedData ownerSerializedData,
        UxmlSerializedAttributeDescription desc,
        VisualElement visualElement,
        string bindingPath,
        bool isInTemplateInstance,
        bool removeBinding)
    {
        using var command = GetPooled(source, vta, attributeUxmlOwner, ownerSerializedData, desc, visualElement, bindingPath, isInTemplateInstance, removeBinding);
        UICommandQueue.Execute(command);
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public UxmlAsset AttributeUxmlOwner { get; private set; }
    public UxmlSerializedData OwnerSerializedData { get; private set; }
    public UxmlSerializedAttributeDescription AttributeDescription { get; private set; }
    public VisualElement VisualElement { get; private set; }
    public string BindingPath { get; private set; }
    public bool IsInTemplateInstance { get; private set; }
    public bool RemoveBinding { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Attributes;

    protected override void Init()
    {
        base.Init();
        VisualTreeAsset = null;
        AttributeUxmlOwner = null;
        OwnerSerializedData = null;
        AttributeDescription = null;
        VisualElement = null;
        BindingPath = null;
        IsInTemplateInstance = false;
        RemoveBinding = false;
    }

    public override bool Validate() => VisualTreeAsset != null && AttributeDescription != null;

    public override void Prepare(in PrepareContext context)
    {
        context.RecordUndo(VisualTreeAsset);
    }

    public override CommandExecutionStatus Execute()
    {
        if (IsInTemplateInstance)
        {
            var templateContainer = UxmlAssetUtilities.GetRootTemplateContainerInEditedVisualTree(VisualTreeAsset, VisualElement);
            var templateAsset = templateContainer?.visualElementAsset as TemplateAsset;

            if (templateAsset == null)
                return CommandExecutionStatus.Success;

            var attributeName = AttributeDescription.name;
            var pathToTemplateAsset = UxmlAssetUtilities.GetPathToTemplateAsset(templateAsset, VisualElement);

            templateAsset.RemoveAttributeOverride(attributeName, pathToTemplateAsset);

            // Re-sync serializedDataOverrides since attribute overrides have changed.
            templateAsset.serializedDataOverrides.Clear();
            UxmlSerializer.CreateSerializedDataOverrides(VisualTreeAsset);

            var currentStage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
            currentStage?.RequestRefresh();
        }
        else
        {
            if (RemoveBinding)
            {
                RemoveBindingCommand.Execute(Source, VisualElement, BindingPath);
            }

            AttributeUxmlOwner.RemoveAttribute(AttributeDescription.name);
            AttributeDescription.SyncDefaultValue(OwnerSerializedData, true);
        }

        UnsetEnumValue();
        return CommandExecutionStatus.Success;
    }

    void UnsetEnumValue()
    {
        if (AttributeDescription.name != "type")
            return;

        // When unsetting the type value for an enum field, we also need to clear the value field as well.
        if (VisualElement is EnumField or EnumFlagsField)
        {
            var valueAttribute = AttributeDescription.dataDescription.FindAttributeWithUxmlName("value");
            var valueBindingAttribute = nameof(EnumField.value);

            using var unsetValueCommand = GetPooled(
                Source,
                VisualTreeAsset,
                AttributeUxmlOwner,
                OwnerSerializedData,
                valueAttribute,
                VisualElement,
                valueBindingAttribute,
                IsInTemplateInstance,
                RemoveBinding);

            UICommandQueue.Execute(unsetValueCommand);
        }
    }
}
