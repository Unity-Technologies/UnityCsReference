// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class UnsetAllAttributesCommand : Command<UnsetAllAttributesCommand>
{
    const string CommandUndoName = "Unset all attributes";

    public static UnsetAllAttributesCommand GetPooled(
        object source,
        VisualTreeAsset vta,
        UxmlAsset attributesUxmlOwner,
        UxmlSerializedData ownerSerializedData,
        UxmlSerializedDataDescription desc,
        VisualElement visualElement,
        bool isInTemplateInstance,
        List<string> ignoredAttributeNames = null)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.VisualTreeAsset = vta;
        cmd.AttributesUxmlOwner = attributesUxmlOwner;
        cmd.OwnerSerializedData = ownerSerializedData;
        cmd.Description = desc;
        cmd.VisualElement = visualElement;
        cmd.IsInTemplateInstance = isInTemplateInstance;
        cmd.IgnoredAttributeNames = ignoredAttributeNames;
        return cmd;
    }

    public static void Execute(object source,
        VisualTreeAsset vta,
        UxmlAsset attributesUxmlOwner,
        UxmlSerializedData ownerSerializedData,
        UxmlSerializedDataDescription desc,
        VisualElement visualElement,
        bool isInTemplateInstance,
        List<string> ignoredAttributeNames = null)
    {
        using var command = GetPooled(source, vta, attributesUxmlOwner, ownerSerializedData, desc, visualElement, isInTemplateInstance, ignoredAttributeNames);
        UICommandQueue.Execute(command);
    }

    public VisualTreeAsset VisualTreeAsset { get; private set; }
    public UxmlAsset AttributesUxmlOwner { get; private set; }
    public UxmlSerializedData OwnerSerializedData { get; private set; }
    public UxmlSerializedDataDescription Description { get; private set; }
    public VisualElement VisualElement { get; private set; }
    public bool IsInTemplateInstance { get; private set; }
    public List<string> IgnoredAttributeNames { get; private set; }

    public override string UndoName => CommandUndoName;
    public override CommandCategory Category => CommandCategory.Attributes;

    protected override void Init()
    {
        base.Init();
        VisualTreeAsset = null;
        AttributesUxmlOwner = null;
        OwnerSerializedData = null;
        Description = null;
        VisualElement = null;
        IsInTemplateInstance = false;
        IgnoredAttributeNames = null;
    }

    public override bool Validate() => VisualTreeAsset != null && Description != null;

    // Manages its own undo group via Undo.IncrementCurrentGroup + Undo.CollapseUndoOperations,
    // so we deliberately skip Prepare's RecordUndo to avoid double-grouping.
    public override CommandExecutionStatus Execute()
    {
        var undoGroup = Undo.GetCurrentGroup();
        Undo.IncrementCurrentGroup();
        Undo.RegisterCompleteObjectUndo(VisualTreeAsset, CommandUndoName);

        if (IsInTemplateInstance)
        {
            var templateContainer = UxmlAssetUtilities.GetRootTemplateContainerInEditedVisualTree(VisualTreeAsset, VisualElement);
            var templateAsset = templateContainer?.visualElementAsset as TemplateAsset;

            if (templateAsset != null)
            {
                var pathToTemplateAsset = UxmlAssetUtilities.GetPathToTemplateAsset(templateAsset, VisualElement);
                var attributeOverrides = new List<TemplateAsset.AttributeOverride>(templateAsset.attributeOverrides);

                foreach (var attributeOverride in attributeOverrides)
                {
                    if (IgnoredAttributeNames is { Count: > 0 } && IgnoredAttributeNames.Contains(attributeOverride.m_AttributeName))
                        continue;

                    if (attributeOverride.NamesPathMatchesElementNamesPath(pathToTemplateAsset))
                        templateAsset.RemoveAttributeOverride(attributeOverride.m_AttributeName, pathToTemplateAsset);
                }

                // Re-sync serializedDataOverrides since attribute overrides have changed.
                templateAsset.serializedDataOverrides.Clear();
                UxmlSerializer.CreateSerializedDataOverrides(VisualTreeAsset);

                var currentStage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
                currentStage?.RequestRefresh();
            }
        }
        else
        {
            // Clear UxmlObjects
            AttributesUxmlOwner.RemoveUxmlObjectAssetChildren();

            // Clear attribute overrides
            foreach (var attribute in Description.serializedAttributes)
            {
                if (IgnoredAttributeNames is { Count: > 0 } && IgnoredAttributeNames.Contains(attribute.name))
                    continue;

                if (!attribute.isUxmlObject)
                    AttributesUxmlOwner.RemoveAttribute(attribute.name);

                attribute.SyncDefaultValue(OwnerSerializedData, true);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(VisualTreeAsset);
        return CommandExecutionStatus.Success;
    }
}
