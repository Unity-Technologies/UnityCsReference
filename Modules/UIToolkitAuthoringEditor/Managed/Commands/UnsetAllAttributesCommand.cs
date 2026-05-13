// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct UnsetAllAttributesCommand
{
    const string CommandUndoName = "Unset all attributes";

    readonly VisualTreeAsset m_VisualTreeAsset;
    readonly UxmlAsset m_AttributesUxmlOwner;
    readonly UxmlSerializedData m_OwnerSerializedData;
    readonly UxmlSerializedDataDescription m_Description;
    readonly VisualElement m_VisualElement;
    readonly bool m_IsInTemplateInstance;
    readonly List<string> m_IgnoredAttributeNames;

    public UnsetAllAttributesCommand(
        VisualTreeAsset vta,
        UxmlAsset attributesUxmlOwner,
        UxmlSerializedData ownerSerializedData,
        UxmlSerializedDataDescription desc,
        VisualElement visualElement,
        bool isInTemplateInstance,
        List<string> ignoredAttributeNames = null)
    {
        m_VisualTreeAsset = vta;
        m_AttributesUxmlOwner = attributesUxmlOwner;
        m_OwnerSerializedData = ownerSerializedData;
        m_Description = desc;
        m_VisualElement = visualElement;
        m_IsInTemplateInstance = isInTemplateInstance;
        m_IgnoredAttributeNames = ignoredAttributeNames;
    }

    public void Execute()
    {
        Assert.IsNotNull(m_VisualTreeAsset);
        Assert.IsNotNull(m_Description);

        var undoGroup = Undo.GetCurrentGroup();
        Undo.IncrementCurrentGroup();
        Undo.RegisterCompleteObjectUndo(m_VisualTreeAsset, CommandUndoName);

        if (m_IsInTemplateInstance)
        {
            var templateContainer = UxmlAssetUtilities.GetRootTemplateContainerInEditedVisualTree(m_VisualTreeAsset, m_VisualElement);
            var templateAsset = templateContainer?.visualElementAsset as TemplateAsset;

            if (templateAsset != null)
            {
                var pathToTemplateAsset = UxmlAssetUtilities.GetPathToTemplateAsset(templateAsset, m_VisualElement);
                var attributeOverrides = new List<TemplateAsset.AttributeOverride>(templateAsset.attributeOverrides);

                foreach (var attributeOverride in attributeOverrides)
                {
                    if (m_IgnoredAttributeNames is { Count: > 0 } && m_IgnoredAttributeNames.Contains(attributeOverride.m_AttributeName))
                        continue;

                    if (attributeOverride.NamesPathMatchesElementNamesPath(pathToTemplateAsset))
                    {
                        templateAsset.RemoveAttributeOverride(attributeOverride.m_AttributeName, pathToTemplateAsset);
                    }
                }

                // Re-sync serializedDataOverrides since attribute overrides have changed.
                templateAsset.serializedDataOverrides.Clear();
                UxmlSerializer.CreateSerializedDataOverrides(m_VisualTreeAsset);

                var currentStage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
                currentStage?.RequestRefresh();
            }
        }
        else
        {
            // Clear UxmlObjects
            m_AttributesUxmlOwner.RemoveUxmlObjectAssetChildren();

            // Clear attribute overrides
            foreach (var attribute in m_Description.serializedAttributes)
            {
                if (m_IgnoredAttributeNames is { Count: > 0 } && m_IgnoredAttributeNames.Contains(attribute.name))
                    continue;

                if (!attribute.isUxmlObject)
                {
                    m_AttributesUxmlOwner.RemoveAttribute(attribute.name);
                }
                attribute.SyncDefaultValue(m_OwnerSerializedData, true);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(m_VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(m_VisualTreeAsset);
    }
}
