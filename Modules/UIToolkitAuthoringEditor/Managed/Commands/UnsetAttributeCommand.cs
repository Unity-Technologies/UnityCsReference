// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct UnsetAttributeCommand
{
    const string CommandUndoName = "Unset attribute";

    readonly VisualTreeAsset m_VisualTreeAsset;
    readonly UxmlAsset m_AttributeUxmlOwner;
    readonly UxmlSerializedData m_OwnerSerializedData;
    readonly UxmlSerializedAttributeDescription m_AttributeDescription;
    readonly VisualElement m_VisualElement;
    readonly string m_BindingPath;
    private readonly bool m_IsInTemplateInstance;
    readonly bool m_RemoveBinding;

    public UnsetAttributeCommand(
        VisualTreeAsset vta,
        UxmlAsset attributeUxmlOwner,
        UxmlSerializedData ownerSerializedData,
        UxmlSerializedAttributeDescription desc,
        VisualElement visualElement,
        string bindingPath,
        bool isInTemplateInstance,
        bool removeBinding)
    {
        m_VisualTreeAsset = vta;
        m_OwnerSerializedData = ownerSerializedData;
        m_AttributeUxmlOwner = attributeUxmlOwner;
        m_AttributeDescription = desc;
        m_VisualElement = visualElement;
        m_BindingPath = bindingPath;
        m_IsInTemplateInstance = isInTemplateInstance;
        m_RemoveBinding = removeBinding;
    }

    public void Execute()
    {
        Assert.IsNotNull(m_VisualTreeAsset);
        Assert.IsNotNull(m_AttributeDescription);

        Undo.RegisterCompleteObjectUndo(m_VisualTreeAsset, CommandUndoName);

        if (m_IsInTemplateInstance)
        {
            var templateContainer = UxmlAssetUtilities.GetRootTemplateContainerInEditedVisualTree(m_VisualTreeAsset, m_VisualElement);
            var templateAsset = templateContainer?.visualElementAsset as TemplateAsset;

            if (templateAsset != null)
            {
                var attributeName = m_AttributeDescription.name;
                var pathToTemplateAsset = UxmlAssetUtilities.GetPathToTemplateAsset(templateAsset, m_VisualElement);

                templateAsset.RemoveAttributeOverride(attributeName, pathToTemplateAsset);

                // Re-sync serializedDataOverrides since attribute overrides have changed.
                templateAsset.serializedDataOverrides.Clear();
                UxmlSerializer.CreateSerializedDataOverrides(m_VisualTreeAsset);

                var currentStage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
                currentStage?.RequestRefresh();
            }
            else
            {
                return;
            }
        }
        else
        {
            if (m_RemoveBinding)
            {
                var cmd = new RemoveBindingCommand(m_VisualElement, m_BindingPath);
                cmd.Execute();
            }

            m_AttributeUxmlOwner.RemoveAttribute(m_AttributeDescription.name);
            m_AttributeDescription.SyncDefaultValue(m_OwnerSerializedData, true);
        }

        UnsetEnumValue();
        EditorUtility.SetDirty(m_VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(m_VisualTreeAsset);
    }

    void UnsetEnumValue()
    {
        if (m_AttributeDescription.name != "type")
            return;

        // When unsetting the type value for an enum field, we also need to clear the value field as well.
        if (m_VisualElement is EnumField or EnumFlagsField)
        {
            var valueAttribute = m_AttributeDescription.dataDescription.FindAttributeWithUxmlName("value");
            var valueBindingAttribute = nameof(EnumField.value);

            var unsetValueCommand = new UnsetAttributeCommand(m_VisualTreeAsset,
                m_AttributeUxmlOwner,
                m_OwnerSerializedData,
                valueAttribute,
                m_VisualElement,
                valueBindingAttribute,
                m_IsInTemplateInstance, m_RemoveBinding);

            unsetValueCommand.Execute();
        }
    }
}
