// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class UxmlAttributeChangeHandler
{
    public UxmlAttributesEditingContext Context { get; set; }

    internal struct ChangeInfo
    {
        public VisualTreeAsset visualTreeAsset;
        public UxmlSerializedData editedUxmlSerializedData;
        public VisualElementAsset editedElementAsset;
        public SerializedObject rootSerializedObject;
        public string serializedBasePath;
        public List<UndoPropertyModification> propertyModifications;

    }

    public bool IsTrackingChanges { get; private set; }

    internal readonly List<ChangeInfo> m_Changes = new List<ChangeInfo>();

    int m_UndoGroupIdToCollapse = -1;

    /// <summary>
    /// Starts tracking changes to UXML attributes.
    /// </summary>
    public void StartTrackingChanges()
    {
        if (IsTrackingChanges)
            return;
        IsTrackingChanges = true;
        Undo.postprocessModifications += PostprocessModifications;
    }

    /// <summary>
    /// Stops tracking changes to UXML attributes.
    /// </summary>
    public void StopTrackingChanges()
    {
        if (!IsTrackingChanges)
            return;
        IsTrackingChanges = false;
        Undo.postprocessModifications -= PostprocessModifications;
        UxmlAssetUtilities.ClearCache();
    }

    void ProcessChanges()
    {
        try
        {
            if (m_Changes.Count == 0)
                return;

            bool isTemplateInstance = Context.isInTemplateInstance;

            foreach (var change in m_Changes)
            {
                var vta = change.visualTreeAsset;

                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(vta, "UXML Attribute Change");
                EditorUtility.SetDirty(vta);

                foreach (var mod in change.propertyModifications)
                {
                    // Convert the path from a managed reference to a serialized property path
                    var property = change.rootSerializedObject.FindFirstPropertyFromManagedReferencePath(mod.currentValue.propertyPath);
                    if (property == null)
                        continue;

                    var syncPathResults = UxmlAssetUtilities.SynchronizePath(vta,
                        change.editedUxmlSerializedData,
                        change.editedElementAsset,
                        change.serializedBasePath, property.propertyPath,
                        true,
                        Context.element,
                        null,
                        isTemplateInstance);

                    if (!syncPathResults.success)
                        continue;

                    var uxmlAsset = isTemplateInstance ? null : syncPathResults.uxmlAsset;
                    var value = property.boxedValue;

                    var serializedData = syncPathResults.serializedData as UxmlSerializedData;
                    if (syncPathResults.attributeDescription != null)
                    {
                        if (syncPathResults.attributeDescription.type.IsEnum)
                        {
                            // Convert the value to an enum if needed as enums are stored as ints in the serialized data but we want to set them as enums on the attribute description.
                            value = Enum.ToObject(syncPathResults.attributeDescription.type, value);
                        }
                        else if (syncPathResults.attributeDescription.isList && value is not IList && syncPathResults.attributeDescription is not UxmlSerializedUxmlObjectAttributeDescription)
                        {
                            // For list elements, property.boxedValue is just the edited item, but we need the full list.
                            value = syncPathResults.attributeDescription.GetSerializedValue(serializedData);
                        }
                    }

                    SetAttributeCommand.Execute(CommandSources.Inspector, vta, uxmlAsset, serializedData, syncPathResults.attributeDescription, value);

                    if (isTemplateInstance)
                        SetAttributeOverrideCommand.Execute(CommandSources.Inspector, Context.editedVisualTreeAsset, syncPathResults.attributeDescription, Context.element, value);
                }

                try
                {
                    // We call the view's DeserializeElement method only once per view per call to ProcessBatchedChanges.
                    Context.editingController.DeserializeElement();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                ListPool<UndoPropertyModification>.Release(change.propertyModifications);
                // We need to mark the hierarchy as changed so a live update can occur if needed.
                UIElementsUtility.MarkVisualTreeAssetAsChanged(vta);
            }

            if (m_UndoGroupIdToCollapse != -1)
                Undo.CollapseUndoOperations(m_UndoGroupIdToCollapse);
        }
        finally
        {
            m_Changes.Clear();
            m_UndoGroupIdToCollapse = -1;
        }
    }

    UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
    {
        // We capture changes here but dont process them yet as we can not make Undo changes inside of this callback.
        List<UndoPropertyModification> capturedModifications = null;

        foreach (var mod in modifications)
        {
            if (mod.currentValue.target == Context?.rootSerializedObject?.targetObject)
            {
                capturedModifications ??= ListPool<UndoPropertyModification>.Get();
                capturedModifications.Add(mod);
            }
        }

        if (capturedModifications != null)
        {
           var targetVta  = capturedModifications[0].currentValue.target as VisualTreeAsset;
           var editingField = SerializedObjectBindingBase.editingField;
           var undoGroupToCollapse = (int?)editingField?.GetProperty(SerializedObjectBindingBase.UndoGroupPropertyKey)
                                     ?? Undo.GetCurrentGroup();

           // Use the smallest group as the group to collapse to
           m_UndoGroupIdToCollapse = (m_UndoGroupIdToCollapse != -1) ? Math.Min(m_UndoGroupIdToCollapse, undoGroupToCollapse) : undoGroupToCollapse;

           // Save the context data in case the context is cleared before we process the changes.
            m_Changes.Add(new ChangeInfo
            {
                propertyModifications = capturedModifications,
                visualTreeAsset = targetVta,
                editedUxmlSerializedData = Context.uxmlSerializedData,
                rootSerializedObject = Context.rootSerializedObject,
                editedElementAsset = Context.elementAsset,
                serializedBasePath = Context.serializedBasePath
            });

            EditorApplication.delayCall += ProcessChanges;
        }

        return modifications;
    }
}
