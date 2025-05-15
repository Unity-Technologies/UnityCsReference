// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = System.Object;
namespace Unity.UI.Builder;

internal class UxmlBatchedChangesController: IDisposable
{
    // A struct to hold a batched change. It holds view-specific listener whose methods are called when the change is processed.
    private struct BatchedChange
    {
        public VisualElement fieldElement { get; set; }
        public SerializedProperty property { get; set; }
        public IBatchedUxmlChangesListener listener { get; set; }
        public VisualTreeAsset uxmlDocument { get; set; }
    }

    // A struct to hold a validated uxml change. It holds view-specific listener to be called when the change is processed.
    private struct ValidatedUxmlChange
    {
        public UxmlAsset uxmlOwner { get; set; }
        public VisualElement fieldElement { get; set; }
        public object newValue { get; set; }
        public UxmlSerializedData serializedData { get; set; }
        public IBatchedUxmlChangesListener listener { get; set; }
        public VisualTreeAsset uxmlDocument { get; set; }
    }

    // Events that are only to be performed once per view per call to ProcessBatchedChanges.
    
    /// <summary>
    /// Deserialize the relevant <see cref="BuilderUxmlAttributesView"/>s' respective elements.
    /// </summary>
    public event Action deserializeElement;
    
    /// <summary>
    /// Used in the relevant <see cref="BuilderUxmlAttributesView"/>s to address any UI changes that need to be performed.
    /// </summary>
    public event Action notifyAllChangesProcessed;
    
    /// <summary>
    /// Called in the relevant <see cref="BuilderUxmlAttributesView"/>s when the undo/redo is performed by the controller,
    /// as it is required for the views to once again Deserialize their respective elements.
    /// </summary>
    public event Action onUndoRedoPerformedByController;

    internal bool isInsideUndoRedoUpdate { get; set; }

    readonly List<BatchedChange> m_BatchedChanges = new();
    readonly List<BatchedChange> m_BatchedUxmlObjectChanges = new();
    int? m_CurrentUndoGroup;

    BuilderInspector m_Inspector;

    public UxmlBatchedChangesController(BuilderInspector inspector)
    {
        m_Inspector = inspector;
        SerializedObjectBindingContext.PostProcessTrackedPropertyChanges += ProcessBatchedChanges;
        Undo.undoRedoPerformed += VerifyUndoRedoPerformed;
    }

    /// <summary>
    /// Used to register a property to be tracked for changes. Call this method from the relevant view.
    /// </summary>
    /// <param name="target">Typically a field.</param>
    /// <param name="property">The targeted property on the listener's current element</param>
    /// <param name="listener">Typically a <see cref="BuilderUxmlAttributesView"/> that is responsible for creating the field</param>
    /// <param name="uxmlDocument">The uxml document currently being edited by the listener</param>
    public void TrackPropertyValue(VisualElement target, SerializedProperty property, IBatchedUxmlChangesListener listener, VisualTreeAsset uxmlDocument = null)
    {
        if (target == null)
            return;

        void PrepareTrackedPropertyChangeForBatch(object obj, SerializedProperty prop)
        {
            if (isInsideUndoRedoUpdate)
                return;
            AddBatchedChange(target, property, listener, uxmlDocument);
        }

        target.TrackPropertyValue(property, PrepareTrackedPropertyChangeForBatch);
    }

    /// <summary>
    /// Used to add the uxml change to a batched list after transformation. To call this method, instead use <see cref="TrackPropertyValue"/>.
    /// </summary>
    /// <param name="obj">Typically a field element.</param>
    /// <param name="property">The targeted property on the listener's current element</param>
    /// <param name="listener">Typically a <see cref="BuilderUxmlAttributesView"/> that is responsible for creating the field</param>
    /// <param name="uxmlDocument">The uxml document currently being edited by the listener</param>
    private void AddBatchedChange(object obj, SerializedProperty property, IBatchedUxmlChangesListener listener, VisualTreeAsset uxmlDocument = null)
    {
        if (obj is not VisualElement target)
            return;

        var fieldElement = GetRootFieldElement(target);
        m_BatchedChanges.Add(new BatchedChange { fieldElement = fieldElement, property = property, listener = listener, uxmlDocument = uxmlDocument });
    }

    /// <summary>
    /// Used to track a UxmlObject once the relevant field has been refined.
    /// </summary>
    /// <param name="obj">Typically a field element.</param>
    /// <param name="property">The targeted property on the listener's current element</param>
    /// <param name="listener">Typically a <see cref="BuilderUxmlAttributesView"/> that is responsible for creating the field</param>
    /// <param name="uxmlDocument">The uxml document currently being edited by the listener</param>
    /// <param name="allowSingleObjectTracking">Used to allow the call to <see cref="TrackPropertyValue"/>, if, for example,
    /// the relevant view's current element is not null</param>
    private void TrackPropertyValueUxmlObject(object obj, SerializedProperty property, IBatchedUxmlChangesListener listener, VisualTreeAsset uxmlDocument = null, bool allowSingleObjectTracking = false)
    {
        if (obj is not VisualElement fieldElement)
            return;
        fieldElement.Clear();

        if (property.isArray)
            TrackCustomPropertyDrawerListElements(fieldElement, property, listener, uxmlDocument, allowSingleObjectTracking);
        else
            TrackCustomPropertyDrawerFields(fieldElement, property, listener, uxmlDocument, allowSingleObjectTracking);

        if (isInsideUndoRedoUpdate || !allowSingleObjectTracking)
            return;

        void PrepareTrackedPropertyChangeForBatch(object objectInner, SerializedProperty prop)
        {
            if (isInsideUndoRedoUpdate)
                return;
            AddBatchedUxmlObjectChange(fieldElement, property, listener, uxmlDocument);
        }

        fieldElement.TrackPropertyValue(property, PrepareTrackedPropertyChangeForBatch);
    }

    /// <summary>
    /// Used to track a list of UxmlObjects.
    /// </summary>
    /// <param name="listField">a field in a List</param>
    /// <param name="property">The targeted property on the listener's current element</param>
    /// <param name="listener">Typically a <see cref="BuilderUxmlAttributesView"/> that is responsible for creating the field</param>
    /// <param name="uxmlDocument">The uxml document currently being edited by the listener</param>
    /// <param name="allowSingleObjectTracking">Used to allow the call to <see cref="TrackPropertyValue"/>, if, for example,
    /// the relevant view's current element is not null</param>
    private void TrackCustomPropertyDrawerListElements(VisualElement listField, SerializedProperty property, IBatchedUxmlChangesListener listener, VisualTreeAsset uxmlDocument = null, bool allowSingleObjectTracking = false)
    {
        if (allowSingleObjectTracking)
            TrackPropertyValue(listField, property.FindPropertyRelative(BuilderUxmlAttributesView.ArraySizeRelativePath), listener, uxmlDocument);

        for (int i = 0; i < property.arraySize; i++)
        {
            var item = property.GetArrayElementAtIndex(i);
            TrackCustomPropertyDrawerFields(listField, item, listener, uxmlDocument, allowSingleObjectTracking);
        }
    }

    /// <summary>
    /// Used to track a list of UxmlObjects from an appropriate view. Call this method from the relevant view.
    /// </summary>
    /// <param name="uxmlObjectField">a UXML ObectField</param>
    /// <param name="property">The targeted property on the listener's current element</param>
    /// <param name="listener">Typically a <see cref="BuilderUxmlAttributesView"/> that is responsible for creating the field</param>
    /// <param name="uxmlDocument">The uxml document currently being edited by the listener</param>
    /// <param name="allowSingleObjectTracking">Used to allow the call to <see cref="TrackPropertyValue"/>, if, for example,
    /// the relevant view's current element is not null</param>
    public void TrackCustomPropertyDrawerFields(VisualElement uxmlObjectField, SerializedProperty property,
        IBatchedUxmlChangesListener listener, VisualTreeAsset uxmlDocument = null, bool allowSingleObjectTracking = false)
    {
        var instance = property.boxedValue;
        if (instance == null)
            return;

        var dataDescription = UxmlSerializedDataRegistry.GetDescription(instance.GetType().DeclaringType.FullName);
        var itemRoot = new BuilderUxmlAttributesView.UxmlAssetSerializedDataRoot
        {
            dataDescription = dataDescription,
            rootPath = property.propertyPath,
            name = property.propertyPath
        };
        uxmlObjectField.Add(itemRoot);

        foreach (var desc in dataDescription.serializedAttributes)
        {
            var fieldElement = new BuilderUxmlAttributesView.UxmlSerializedDataAttributeField { name = desc.serializedField.Name };
            fieldElement.SetLinkedAttributeDescription(desc);

            itemRoot.Add(fieldElement);
            var p = property.FindPropertyRelative(desc.serializedField.Name);

            void PrepareTrackedObjectPropertyChangeForBatch(object objectInner, SerializedProperty prop)
            {
                TrackPropertyValueUxmlObject(fieldElement, p, listener, uxmlDocument, allowSingleObjectTracking);
            }

            if (desc.isUxmlObject)
            {
                // Track a UxmlObject field/list that we do not have control of.
                fieldElement.TrackPropertyValue(p, PrepareTrackedObjectPropertyChangeForBatch);

                if (p.isArray)
                    // add a custom bool here
                    TrackCustomPropertyDrawerListElements(fieldElement, p, listener, uxmlDocument, allowSingleObjectTracking);
                else
                    TrackCustomPropertyDrawerFields(fieldElement, p, listener, uxmlDocument, allowSingleObjectTracking);
            }
            else
            {
                TrackPropertyValue(fieldElement, p, listener, uxmlDocument);
            }
        }
    }

    /// <summary>
    /// Used to add the uxml object change to a batched list after transformation. To call this method, instead use <see cref="TrackCustomPropertyDrawerFields"/>.
    /// </summary>
    /// <param name="target">Typically a field.</param>
    /// <param name="property">The targeted property on the listener's current element</param>
    /// <param name="listener">Typically a <see cref="BuilderUxmlAttributesView"/> that is responsible for creating the field</param>
    /// <param name="uxmlDocument">The uxml document currently being edited by the listener</param>
    private void AddBatchedUxmlObjectChange(VisualElement target, SerializedProperty property, IBatchedUxmlChangesListener listener, VisualTreeAsset uxmlDocument = null)
    {
        var fieldElement = GetRootFieldElement(target);
        m_BatchedUxmlObjectChanges.Add(new BatchedChange { fieldElement = fieldElement, property = property, listener = listener, uxmlDocument = uxmlDocument});
    }

    /// <summary>
    /// The main synchronization method that processes all batched changes. It also handles the undo group for the changes.
    /// </summary>
    /// <param name="previousSerializedObjectVersion">Used to allow the processing.</param>
    /// <param name="currentSerializedObjectVersion">Used to allow the processing.</param>
    /// <remarks>
    /// Each batched change has its own view-specific listener assigned by the view which holds the field associated to the change.
    /// Each change will call its own callback individually. Then, if a view had a validated change, the view has its own actions to perform
    /// once all the changes are processed: <see cref="deserializeElement"/>, <see cref="notifyAllChangesProcessed"/>.
    /// </remarks>
    public void ProcessBatchedChanges(uint previousSerializedObjectVersion = 0, uint currentSerializedObjectVersion = 0)
    {
        // Process changes only if the serializedObject version is unchanged.
        // If the version does not change, it means that multiple properties have been changed in the same update,
        // thus allowing for the batched changes to be processed.
        // This is done at this level and not at the level of SerializedObjectBindingContext to allow for other
        // classes to still implement SerializedObjectBindingContext.PostProcessTrackedPropertyChanges.
        if (previousSerializedObjectVersion != currentSerializedObjectVersion)
            return;

        if (m_BatchedChanges.Count == 0 && m_BatchedUxmlObjectChanges.Count == 0)
            return;

        var undoGroup = GetCurrentUndoGroup();
        using var pool = ListPool<ValidatedUxmlChange>.Get(out var validatedUxmlChangesToProcess);
        using var poolObject = ListPool<(VisualElement visualElement, Action<VisualElement> viewUxmlObjectChangedCallback)>.Get(out var validatedUxmlObjectChangesToProcess);

        try
        {
            foreach (var changeEvent in m_BatchedChanges)
            {
                var fieldElement = changeEvent.fieldElement;
                if (fieldElement.panel == null)
                    continue;

                Undo.IncrementCurrentGroup();
                var revertGroup = Undo.GetCurrentGroup();
                var result = changeEvent.listener.SynchronizePath(changeEvent.property.propertyPath, true);
                if (!result.success)
                    continue;

                if (result.serializedData is not UxmlSerializedData currentUxmlSerializedData)
                    continue;

                var description = result.attributeDescription ?? fieldElement.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                var currentAttributeUxmlOwner = result.uxmlAsset;
                var newValue = description.GetSerializedValue(currentUxmlSerializedData);

                if (result.attributeOwner != null)
                {
                    description.TryGetValueFromObject(result.attributeOwner, out var previousValue);
                    // Unity serializes null values as default objects, so we need to do the same to compare.
                    if (!description.isUxmlObject && previousValue == null && !typeof(Object).IsAssignableFrom(description.type) && description.type.GetConstructor(Type.EmptyTypes) != null)
                        previousValue = Activator.CreateInstance(description.type);

                    if (newValue is VisualTreeAsset vta && Builder.ActiveWindow.inspector.document.WillCauseCircularDependency(vta))
                    {
                        description.SetSerializedValue(currentUxmlSerializedData, previousValue);
                        BuilderDialogsUtility.DisplayDialog(BuilderConstants.InvalidWouldCauseCircularDependencyMessage,
                            BuilderConstants.InvalidWouldCauseCircularDependencyMessageDescription, BuilderConstants.DialogOkOption);
                        return;
                    }

                    // We choose to disregard listener when the value remains unchanged,
                    // which can occur during an Undo/Redo when the bound fields are updated.
                    // To do this we compare the value on the attribute owner to its serialized value,
                    // if they match then we consider them to have not changed.
                    if (UxmlAttributeComparison.ObjectEquals(previousValue, newValue))
                    {
                        Undo.RevertAllDownToGroup(revertGroup);
                        continue;
                    }
                }

                // Apply the attribute flags before we CallDeserializeOnElement
                description.SetSerializedValueAttributeFlags(currentUxmlSerializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

                // Only if a change is validated do we later notify the view that the associated attribute has changed.
                var validatedUxmlChange = new ValidatedUxmlChange
                {
                    uxmlOwner = currentAttributeUxmlOwner,
                    fieldElement = fieldElement,
                    newValue = newValue,
                    serializedData = currentUxmlSerializedData,
                    listener = changeEvent.listener,
                    uxmlDocument = changeEvent.uxmlDocument
                };
                validatedUxmlChangesToProcess.Add(validatedUxmlChange);

                // A call is necessary here to signal the affected view that it must deserialize the element.
                changeEvent.listener.ToggleUxmlChangeFlagForView(true);
            }

            foreach (var changeEvent in m_BatchedUxmlObjectChanges)
            {
                var fieldElement = changeEvent.fieldElement;
                if (fieldElement.panel == null)
                    continue;

                var result = changeEvent.listener.SynchronizePath(changeEvent.property.propertyPath, true);
                if (!result.success)
                    continue;

                validatedUxmlObjectChangesToProcess.Add((fieldElement, changeEvent.listener.UxmlObjectChanged));
                // A call is necessary here to signal the affected view that it must deserialize the element.
                changeEvent.listener.ToggleUxmlChangeFlagForView(true);
            }
        }
        finally
        {
            m_BatchedChanges.Clear();
            m_BatchedUxmlObjectChanges.Clear();
        }

        try
        {
            // We call the view's DeserializeElement method only once per view per call to ProcessBatchedChanges.
            deserializeElement?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        foreach (var change in validatedUxmlChangesToProcess)
        {
            if (change.newValue == null || !UxmlAttributeConverter.TryConvertToString(change.newValue, change.uxmlDocument, out var stringValue))
                stringValue = change.newValue?.ToString();

            change.listener.AttributeValueChanged(change.fieldElement, stringValue, change.uxmlOwner);
            // Use the undo group of the field if it has one, otherwise use the current undo group
            var fieldUndoGroup = change.fieldElement.Q<BindableElement>()?.GetProperty(BuilderUxmlAttributesView.UndoGroupPropertyKey);
            if (fieldUndoGroup != null)
                undoGroup = (int)fieldUndoGroup;
        }

        foreach (var objectChange in validatedUxmlObjectChangesToProcess)
        {
            objectChange.viewUxmlObjectChangedCallback?.Invoke(objectChange.visualElement);
        }

        // We call the view's NotifyAllChangesProcessed method only once per view per call to ProcessBatchedChanges.
        try
        {
            notifyAllChangesProcessed?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        // If inline editing is enabled on a property that has a UXML binding, we cache the binding
        // to preview the inline value in the canvas.
        if (m_Inspector.cachedBinding != null)
        {
            m_Inspector.currentVisualElement.ClearBinding(m_Inspector.cachedBinding.property);
        }
        Undo.CollapseUndoOperations(undoGroup);
    }

    void VerifyUndoRedoPerformed()
    {
        onUndoRedoPerformedByController?.Invoke();
        try
        {
            // We need to discard any change events that happen during the undo/redo update in order to avoid reapplying those changes.
            isInsideUndoRedoUpdate = true;
            m_Inspector?.elementPanel?.TickSchedulingUpdaters();
            
            // This is necessary, as a BuilderBindingWindow is not the inspector's window.
            if (BuilderBindingWindow.activeWindow != null)
                BuilderBindingWindow.activeWindow?.rootVisualElement?.elementPanel?.TickSchedulingUpdaters();
        }
        finally
        {
            isInsideUndoRedoUpdate = false;
        }
    }

    public int GetCurrentUndoGroup()
    {
        // We track the undo group so we can fold multiple change events into 1.
        if (m_CurrentUndoGroup == null)
        {
            m_CurrentUndoGroup = Undo.GetCurrentGroup();
            EditorApplication.delayCall += () => m_CurrentUndoGroup = null;
        }

        return m_CurrentUndoGroup.Value;
    }

    private static VisualElement GetRootFieldElement(VisualElement visualElement)
    {
        if (visualElement == null)
            return null;

        var dataField = visualElement as BuilderUxmlAttributesView.UxmlSerializedDataAttributeField ?? visualElement.GetFirstAncestorOfType<BuilderUxmlAttributesView.UxmlSerializedDataAttributeField>();
        return dataField ?? visualElement;
    }

    public void Dispose()
    {
        deserializeElement = null;
        notifyAllChangesProcessed = null;
        Undo.undoRedoPerformed -= VerifyUndoRedoPerformed;
        m_BatchedChanges.Clear();
        m_BatchedUxmlObjectChanges.Clear();
        SerializedObjectBindingContext.PostProcessTrackedPropertyChanges -= ProcessBatchedChanges;
    }
}
