// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Provides context for editing UXML attributes in the UI Builder.
    /// </summary>
    class BuilderUxmlAttributesEditingContext
    {
        // For when we need to view the serialized data from a temp visual element, such as one created by script.
        internal class TempSerializedData : ScriptableObject
        {
            [SerializeReference] public UxmlSerializedData serializedData;
        }
        static readonly string s_TempSerializedRootPath = nameof(TempSerializedData.serializedData);

        /// <summary>
        /// Scope to disable undo when editing UXML attributes using a given context for the duration of the scope.
        /// </summary>
        public class DisableUndoScope : IDisposable
        {
            BuilderUxmlAttributesEditingContext m_Context;
            bool m_OldEnabled;

            /// <summary>
            /// Creates a scope.
            /// </summary>
            /// <param name="context"></param>
            public DisableUndoScope(BuilderUxmlAttributesEditingContext context)
            {
                m_Context = context;
                m_OldEnabled = m_Context.undoEnabled;
                m_Context.undoEnabled = false;
            }

            public void Dispose()
            {
                m_Context.undoEnabled = m_OldEnabled;
            }
        }

        /// <summary>
        /// The UIBuilder window this context is associated with.
        /// </summary>
        public Builder builder => document?.primaryViewportWindow as Builder;

        /// <summary>
        /// The builder document that this context is associated with.
        /// </summary>
        public BuilderDocument document { get; private set; }

        /// <summary>
        /// The VisualTreeAsset that contains the UXML elements being edited.
        /// </summary>
        public VisualTreeAsset visualTree { get; private set; }

        /// <summary>
        /// The VisualElement that is currently being edited.
        /// </summary>
        public VisualElement element { get; private set; }

        /// <summary>
        /// The VisualElementAsset associated with the current element, if available.
        /// </summary>
        public VisualElementAsset elementAsset { get; private set; }

        /// <summary>
        /// The serialized data for the current UXML element.
        /// </summary>
        public UxmlSerializedData uxmlSerializedData => elementAsset != null ? elementAsset.serializedData : tempSerializedData?.serializedData;

        /// <summary>
        /// The UxmlSerializedDataDescription that describes the serialized data for the current element.
        /// </summary>
        public UxmlSerializedDataDescription uxmlSerializedDataDescription { get; private set; }

        /// <summary>
        /// The serialized object created from the current VisualTreeAsset. This serialized object is used to resolved paths to serialized attribute properties.
        /// </summary>
        internal SerializedObject rootSerializedObject { get; private set; }

        /// <summary>
        /// The serialized path from the current uxml element to the current VisualTreeAsset. Using the rootSerializedObject, this path is used as base path to locate attribute properties in the serialized data.
        /// </summary>
        public string serializedBasePath { get;  set; }

        /// <summary>
        /// Indicates whether the current element is part of a template instance.
        /// </summary>
        public bool isInTemplateInstance { get; private set; }

        internal TempSerializedData tempSerializedData { get; private set; }

        /// <summary>
        /// Indicates whether the undo system is enabled for this context.
        /// </summary>
        public bool undoEnabled { get; set; } = true;

        /// <summary>
        /// Indicates whether we are able to edit the element or just view its data.
        /// </summary>
        public bool readOnly
        {
            get
            {
                // Elements in templates should be editable when they have a name
                if (isInTemplateInstance)
                {
                    return string.IsNullOrEmpty(element.name);
                }

                return elementAsset == null;
            }
        }

        /// <summary>
        /// The controller for batching changes to UXML attributes.
        /// </summary>
        public UxmlBatchedChangesController batchedChangesController { get; set; }

        /// <summary>
        /// Event triggered when UXML attributes of the current element change.
        /// </summary>
        public event Action<string> notifyAttributesChanged;

        /// <summary>
        /// Creates a empty context for editing UXML attributes.
        /// </summary>
        public BuilderUxmlAttributesEditingContext()
        {
        }

        /// <summary>
        /// Sets the context for editing UXML attributes with the specified parameters.
        /// </summary>
        /// <param name="document">The active Builder document</param>
        /// <param name="visualTree">The VisualTree to edit</param>
        /// <param name="element">The VisualElement to edit</param>
        /// <param name="batchedChangesController">The controller for batching changes to UXML attributes</param>
        /// <param name="isInTemplateInstance">Indicates whether the VisualElement is part of a template instance</param>
        public void Set(BuilderDocument document, VisualTreeAsset visualTree, VisualElement element, UxmlBatchedChangesController batchedChangesController, bool isInTemplateInstance = false)
        {
            this.document = document;
            this.visualTree = visualTree;
            elementAsset = element?.GetVisualElementAsset();
            this.element = element;
            uxmlSerializedDataDescription = null;
            rootSerializedObject = null;
            serializedBasePath = null;
            tempSerializedData = null;
            this.isInTemplateInstance = isInTemplateInstance;
            this.batchedChangesController = batchedChangesController;
            Init();
        }

        void Init()
        {
            if (element != null)
            {
                uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);
                if (uxmlSerializedDataDescription != null)
                {
                    if (elementAsset == null)
                    {
                        tempSerializedData = element.GetProperty(BuilderConstants.InspectorTempSerializedDataPropertyName) as BuilderUxmlAttributesEditingContext.TempSerializedData;

                        if (tempSerializedData == null)
                        {
                            tempSerializedData = ScriptableObject.CreateInstance<TempSerializedData>();

                            // We need to keep the serialized data alive so we can undo/redo changes
                            element.SetProperty(BuilderConstants.InspectorTempSerializedDataPropertyName, tempSerializedData);

                            // Elements without a VisualElementAsset should not be editable,
                            // except for elements that can have an attribute override.
                            tempSerializedData.hideFlags = readOnly ? HideFlags.NotEditable : HideFlags.None;

                            tempSerializedData.serializedData = uxmlSerializedDataDescription.CreateSerializedData();
                        }

                        uxmlSerializedDataDescription.SyncSerializedData(element, tempSerializedData.serializedData);
                        serializedBasePath = s_TempSerializedRootPath;
                        rootSerializedObject = new SerializedObject(tempSerializedData);
                    }
                    else
                    {
                        if (elementAsset.serializedData == null)
                        {
                            elementAsset.serializedData = uxmlSerializedDataDescription.CreateDefaultSerializedData();
                            elementAsset.serializedData.uxmlAssetId = elementAsset.id;
                        }
                        else
                        {
                            // We treat the serialized data as the source of truth.
                            // There are times when we may need to resync, such as when an undo/redo was performed.
                            uxmlSerializedDataDescription.SyncDefaultValues(elementAsset.serializedData, false);
                            BuilderAssetUtilities.CallDeserializeOnElement(this);
                        }

                        // We need to sync the serialized data with the current element including the default values so they can be edited.
                        uxmlSerializedDataDescription.SyncSerializedData(element, elementAsset.serializedData);

                        visualTree.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

                        UxmlAsset targetAsset = null;
                        foreach (var asset in visualTree.DepthFirstTraversal())
                        {
                            if (asset.id == elementAsset.id)
                            {
                                targetAsset = asset;
                                break;
                            }
                        }

                        // If the UXML file has been modified, the element may no longer be in the asset so we will ignore it. (UUM-59305)
                        if (targetAsset == null)
                        {
                            Clear();
                            return;
                        }

                        serializedBasePath = targetAsset.GetSerializedPath();
                        rootSerializedObject = new SerializedObject(visualTree);
                    }
                }

                // Special case for toggle button groups.
                // We want to sync the length of the state with the number of buttons in the hierarchy.
                if (element is ToggleButtonGroup group)
                {
                    var stateProperty = rootSerializedObject.FindProperty($"{serializedBasePath}.value");
                    var lengthProperty = stateProperty.FindPropertyRelative("m_Length");
                    var length = lengthProperty.intValue;

                    var valueFlagsField = rootSerializedObject.FindProperty(stateProperty.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
                    valueFlagsField.intValue = (int)UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml;

                    var buttonCount = group.Query<Button>().Build().GetCount();
                    if (buttonCount != length && buttonCount < ToggleButtonGroupState.maxLength)
                    {
                        var value = group.value;
                        value.length = buttonCount;
                        group.SetValueWithoutNotify(value);

                        lengthProperty.intValue = buttonCount;
                        rootSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        /// <summary>
        /// Clears the context, resetting all properties to their default values.
        /// </summary>
        public void Clear()
        {
            document = null;
            visualTree = null;
            element = null;
            elementAsset = null;
            rootSerializedObject = null;
            tempSerializedData = null;
            uxmlSerializedDataDescription = null;
            serializedBasePath = string.Empty;
            tempSerializedData = null;
            batchedChangesController = null;
        }

        /// <summary>
        /// Notifies that the specified attributes of the current element have changed.
        /// </summary>
        /// <param name="attributeName">The attribute that has changed</param>
        public void NotifyAttributesChanged(string attributeName)
        {
            notifyAttributesChanged?.Invoke(attributeName);
        }
    }
}
