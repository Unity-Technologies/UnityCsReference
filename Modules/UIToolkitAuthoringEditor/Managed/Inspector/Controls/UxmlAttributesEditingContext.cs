// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The context in which UXML attributes are being viewed or edited.
/// </summary>
class UxmlAttributesEditingContext : IDisposable
{
    /// <summary>
    /// Arguments for the context changed event.
    /// </summary>
    public readonly record struct ContextChangedEventArgs(VisualElement newElement, bool newIsReadOnly,
        VisualElement oldElement, bool oldIsReadOnly)
    {
        public readonly VisualElement newElement = newElement;
        public readonly bool newIsReadOnly = newIsReadOnly;
        public readonly VisualElement oldElement = oldElement;
        public readonly bool oldIsReadOnly = oldIsReadOnly;
    }

    /// <summary>
    /// Scope to disable undo when editing UXML attributes using a given context for the duration of the scope.
    /// </summary>
    public class DisableUndoScope : IDisposable
    {
        UxmlAttributesEditingContext m_Context;
        bool m_OldEnabled;

        /// <summary>
        /// Creates a scope.
        /// </summary>
        /// <param name="context"></param>
        public DisableUndoScope(UxmlAttributesEditingContext context)
        {
            m_Context = context;
            m_OldEnabled = m_Context.m_UndoEnabledExplicit;
            m_Context.undoEnabled = false;
        }

        public void Dispose()
        {
            m_Context.undoEnabled = m_OldEnabled;
        }
    }

    // For when we need to view read only attributes from a visual element, such as one created by script.
    internal class TempSerializedData : VisualTreeAsset
    {
        public static TempSerializedData Create(VisualElement element, bool isTemplateInstance)
        {
            var instance = ScriptableObject.CreateInstance<TempSerializedData>();
            var desc = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            var type = element.GetType();
            var elementAsset = new VisualElementAsset(type.FullName);

            instance.visualTree.Add(elementAsset);
            instance.ResetData(element, isTemplateInstance);
            element.SetProperty(k_TempSerializedDataPropertyName, instance);
            return instance;
        }

        public void ResetData(VisualElement element, bool isTemplateInstance)
        {
            var desc = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            elementAsset.serializedData = desc.CreateDefaultSerializedData();

            // In staging mode we want to show only the UXML data, Pulling from the live element will copy values
            // that may be different in the serialized UXML.
            // We currently make an exception for templates: rebuilding them solely from serialized data would
            // require walking and reassembling the entire asset, which is complex and error‑prone.
            // This is a temporary workaround limited to templates and may be removed once we have a better approach.
            if (isTemplateInstance)
                desc.SyncSerializedData(element, elementAsset.serializedData);
        }

        public VisualElementAsset elementAsset => visualTree[0] as VisualElementAsset;
        public UxmlSerializedData serializedData => elementAsset.serializedData;
    }

    internal static readonly string k_TempSerializedDataPropertyName = "__TempSerializedData";
    internal static readonly string k_UxmlSerializedDataFieldName = "m_SerializedData";

    bool m_UndoEnabledExplicit = true;

    /// <summary>
    /// The controller that manages authoring of UXML attributes.
    /// </summary>
    public UxmlAttributesEditingController editingController { get; }

    /// <summary>
    /// The VisualTreeAsset that contains the UXML elements being edited.
    /// </summary>
    public VisualTreeAsset editedVisualTreeAsset { get; private set; }

    /// <summary>
    /// The VisualTreeAsset that contains the UXML elements being edited or the temporary VisualTreeAsset
    /// used to edit template instances and VisualElements dynamically created.
    /// </summary>
    public VisualTreeAsset visualTreeAsset { get; private set; }

    /// <summary>
    /// The VisualElementAsset being edited.
    /// </summary>
    public VisualElementAsset elementAsset { get; private set; }

    /// <summary>
    /// The serialized data being edited element.
    /// </summary>
    public UxmlSerializedData uxmlSerializedData { get; private set; }

    /// <summary>
    /// Indicates whether the current element is part of a template instance.
    /// </summary>
    public bool isInTemplateInstance { get; private set; }

    /// <summary>
    /// The VisualElement that is currently being viewed or edited.
    /// </summary>
    public VisualElement element { get; private set; }

    /// <summary>
    /// The serialized object created from the VisualTreeAsset of the VisualElement being viewed or
    /// the live object used to view read-only attributes.
    /// This serialized object is used to resolved paths to serialized attribute properties.
    /// </summary>
    public SerializedObject rootSerializedObject { get; private set; }

    /// <summary>
    /// The serialized path from the current uxml element to the current VisualTreeAsset. Using the rootSerializedObject, this path is used as base path to locate attribute properties in the serialized data.
    /// </summary>
    public string serializedBasePath { get; private set; }

    /// <summary>
    /// The UxmlSerializedDataDescription that describes the serialized data for the current element.
    /// </summary>
    public UxmlSerializedDataDescription uxmlSerializedDataDescription { get; private set; }

    /// <summary>
    /// Indicates whether the attributes are read-only in this context.
    /// </summary>
    public bool isReadOnly { get; private set; }

    internal TempSerializedData tempSerializedData { get; private set; }

    /// <summary>
    /// Indicates whether the undo system is enabled for this context.
    /// </summary>
    public bool undoEnabled { get => m_UndoEnabledExplicit && !isReadOnly; set => m_UndoEnabledExplicit = value; }

    /// <summary>
    /// Event sent when the context changes.
    /// </summary>
    public event EventHandler<ContextChangedEventArgs> contextChanged;

    /// <summary>
    /// Creates a UxmlAttributesAuthoringContext with the specified authoring controller.
    /// </summary>
    /// <param name="editingController">The authoring controller related to the context</param>
    /// <exception cref="ArgumentNullException">Exception thrown if the authoring controller is null</exception>
    public UxmlAttributesEditingContext(UxmlAttributesEditingController editingController)
    {
        this.editingController = editingController ?? throw new ArgumentNullException(nameof(editingController));
        editingController.context = this;
    }

    public void Set(VisualElement element, bool isReadOnly = false)
    {
        VisualTreeAsset vta;
        var stage = StageUtility.GetCurrentStage() as VisualElementEditingStage;

        if (stage != null)
        {
            vta = stage.EditedVisualTreeAsset;
        }
        else
        {
            vta = element.visualTreeAssetSource;
        }

        Set(vta, element, isReadOnly);
    }

    public void Set(VisualTreeAsset editedVisualTreeAsset, VisualElement element, bool isReadOnly = false)
    {
        SetInternal(editedVisualTreeAsset, element, isReadOnly);
    }

    /// <summary>
    /// Set the context
    /// </summary>
    /// <param name="element">The VisualElement associated to the attributes to view or edit</param>
    /// <param name="environment">The environment where the uxml attributes are view or edited</param>
    /// <param name="isReadOnly">Indicates whether the attributes are read-only</param>
    void SetInternal(VisualTreeAsset editedVisualTreeAsset, VisualElement element, bool isReadOnly)
    {
        var oldElement = this.element;
        var oldIsReadOnly = this.isReadOnly;

        // If nothing changed, do nothing
        if (oldElement == element && oldIsReadOnly == isReadOnly)
            return;

        ClearWithoutNotification();

        this.element = element;
        this.isReadOnly = isReadOnly || (element != null && element.visualElementAsset == null);

        try
        {
            Init(editedVisualTreeAsset);
        }
        finally
        {
            NotifyContextChanged(oldElement, oldIsReadOnly);
        }
    }

    void NotifyContextChanged(VisualElement oldElement, bool oldIsReadOnly)
    {
        contextChanged?.Invoke(this, new ContextChangedEventArgs(element, isReadOnly, oldElement, oldIsReadOnly));
    }

    protected virtual void Init(VisualTreeAsset editedVisualTreeAsset)
    {
        this.editedVisualTreeAsset = editedVisualTreeAsset;
        isInTemplateInstance = false;

        if (element != null)
        {
            uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            if (uxmlSerializedDataDescription == null)
                return;

            var templateAsset = element.templateAsset;

            isInTemplateInstance = templateAsset != null && editedVisualTreeAsset != element.visualTreeAssetSource;

            var elementAsset = element.visualElementAsset;

            if (elementAsset == null || isInTemplateInstance)
            {
                tempSerializedData = element.GetProperty(k_TempSerializedDataPropertyName) as TempSerializedData;

                if (tempSerializedData == null)
                {
                    tempSerializedData = TempSerializedData.Create(element, isInTemplateInstance);
                }
                else
                {
                    tempSerializedData.ResetData(element, isInTemplateInstance);
                }
                visualTreeAsset = tempSerializedData;
                this.elementAsset = tempSerializedData.elementAsset;
                uxmlSerializedData = tempSerializedData.serializedData;
                rootSerializedObject = new SerializedObject(tempSerializedData);
                serializedBasePath = GetSerializedPath(tempSerializedData.elementAsset);
            }
            else
            {
                var visualTreeAsset = editedVisualTreeAsset;

                visualTreeAsset.hideFlags = isReadOnly ? HideFlags.NotEditable : HideFlags.None;

                // TODO : Restore the hideFlags after done

                // If the UXML file has been modified, the element may no longer be in the asset so we will ignore it. (UUM-59305)
                if (elementAsset.visualTreeAsset != visualTreeAsset)
                {
                    Clear();
                    return;
                }

                if (elementAsset.serializedData == null)
                {
                    elementAsset.serializedData = uxmlSerializedData = uxmlSerializedDataDescription.CreateDefaultSerializedData();
                    elementAsset.serializedData.uxmlAssetId = elementAsset.id;
                }
                else
                {
                    uxmlSerializedData = elementAsset.serializedData;
                }

                this.visualTreeAsset = visualTreeAsset;
                this.elementAsset = elementAsset;

                rootSerializedObject = new SerializedObject(visualTreeAsset);
                serializedBasePath = GetSerializedPath(elementAsset);
            }
        }
    }

    /// <summary>
    /// Clears the context, resetting all properties to their default values.
    /// </summary>
    public void Clear()
    {
        Set(null, null);
    }

    void ClearWithoutNotification()
    {
        editingController.liveAttributePropertyController.RemoveLiveProperties();
        element = null;
        uxmlSerializedDataDescription = null;
        uxmlSerializedData = null;
        tempSerializedData = null;
        rootSerializedObject = null;
        serializedBasePath = string.Empty;
        isReadOnly = false;
    }

    static string GetSerializedPath(UxmlAsset asset)
    {
        using var _parents = ListPool<(UxmlAsset a, int i)>.Get(out var parents);
        var sb = new System.Text.StringBuilder();
        var previous = asset;
        var current = asset;

        while (null != current)
        {
            if (current == previous)
            {
                parents.Add((current, -1));
            }
            else
            {
                var childIndex = -1;
                for (var i = 0; i < current.childCount; ++i)
                {
                    if (current[i] == previous)
                    {
                        childIndex = i;
                        break;
                    }
                }
                parents.Add((current, childIndex));
            }

            previous = current;
            current = current.parentAsset;
        }

        if (parents.Count == 0 || !parents[^1].a.isRoot)
            throw new InvalidOperationException("The asset is not part of a UXML document.");

        sb.Append("m_VisualTree");
        for (var i = parents.Count - 1; i >= 0; --i)
        {
            if (parents[i].i < 0)
                break;
            sb.Append($".m_Children.Array.data[{parents[i].i}]");
        }

        sb.Append(".");
        sb.Append(k_UxmlSerializedDataFieldName);

        return sb.ToString();
    }

    public void Dispose()
    {
        Clear();
        editingController.Dispose();
    }
}
