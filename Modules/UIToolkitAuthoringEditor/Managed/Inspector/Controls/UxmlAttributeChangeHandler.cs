// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        public int undoGroupId;
        public List<UndoPropertyModification> propertyModifications;

    }

    public bool IsTrackingChanges { get; private set; }

    internal readonly List<ChangeInfo> m_Changes = new List<ChangeInfo>();

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
            bool isTemplateInstance = Context.element.templateAsset != null;
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

                    // Convert the value to an enum if needed as enums are stored as ints in the serialized data but we want to set them as enums on the attribute description.
                    if (syncPathResults.attributeDescription != null && syncPathResults.attributeDescription.type.IsEnum)
                    {
                        value = Enum.ToObject(syncPathResults.attributeDescription.type, value);
                    }

                    var cmd = new SetAttributeCommand(vta, uxmlAsset, syncPathResults.serializedData as UxmlSerializedData, syncPathResults.attributeDescription, value);
                    cmd.Execute();

                    if (isTemplateInstance)
                    {
                        var cmdOverride = new SetAttributeOverrideCommand(Context.editedVisualTreeAsset, syncPathResults.attributeDescription, Context.element, value);
                        cmdOverride.Execute();
                    }
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
                Undo.CollapseUndoOperations(change.undoGroupId);

                // We need to mark the hierarchy as changed so a live update can occur if needed.
                UIElementsUtility.MarkVisualTreeAssetAsChanged(vta);
            }
        }
        finally
        {
            m_Changes.Clear();
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

           // Save the context data in case the context is cleared before we process the changes.
            m_Changes.Add(new ChangeInfo
            {
                undoGroupId = Undo.GetCurrentGroup(),
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
