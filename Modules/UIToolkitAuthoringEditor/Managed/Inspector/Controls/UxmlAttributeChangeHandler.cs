// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class UxmlAttributeChangeHandler
{
    public UxmlAttributesEditingContext Context { get; set; }

    internal struct ChangeInfo
    {
        public VisualTreeAsset visualTreeAsset;
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
                    var property = Context.rootSerializedObject.FindFirstPropertyFromManagedReferencePath(mod.currentValue.propertyPath);
                    if (property == null)
                        continue;

                    var syncPathResults = UxmlAssetUtilities.SynchronizePath(vta,
                        Context.uxmlSerializedData,
                        Context.element.visualElementAsset,
                        Context.serializedBasePath, property.propertyPath,
                        true,
                        Context.element,
                        null,
                        isTemplateInstance);

                    if (!syncPathResults.success)
                        continue;

                    var uxmlAsset = isTemplateInstance ? null : Context.element.visualElementAsset;
                    var cmd = new SetAttributeCommand(vta, uxmlAsset, syncPathResults.serializedData as UxmlSerializedData, syncPathResults.attributeDescription, property.boxedValue);
                    cmd.Execute();

                    if (isTemplateInstance)
                    {
                        var cmdOverride = new SetAttributeOverrideCommand(vta, Context.element.visualElementAsset, syncPathResults.attributeDescription, Context.element, property.boxedValue);
                        cmdOverride.Execute();
                    }
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
            var stage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
            var vta = stage.EditedVisualTreeAsset;

            m_Changes.Add(new ChangeInfo
            {
                undoGroupId = Undo.GetCurrentGroup(),
                propertyModifications = capturedModifications,
                visualTreeAsset = vta
            });
            EditorApplication.delayCall += ProcessChanges;
        }

        return modifications;
    }
}
