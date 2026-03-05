// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
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

    // For when we need to view read only attributes from a visual element, such as one created by script.
    internal class TempSerializedData : ScriptableObject
    {
        [SerializeReference] public UxmlSerializedData serializedData;
    }

    internal static readonly string k_TempSerializedDataPropertyName = "__TempSerializedData";
    internal static readonly string k_UxmlSerializedDataFieldName = "m_SerializedData";
    internal static readonly string k_TempSerializedRootPath = nameof(TempSerializedData.serializedData);

    /// <summary>
    /// The controller that manages authoring of UXML attributes.
    /// </summary>
    public UxmlAttributesEditingController editingController { get; }

    /// <summary>
    /// The VisualElement that is currently being viewed or edited.
    /// </summary>
    public VisualElement element { get; private set; }

    /// <summary>
    /// The serialized data for the current UXML element.
    /// </summary>
    public UxmlSerializedData uxmlSerializedData { get; private set; }

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

    /// <summary>
    /// Set the context
    /// </summary>
    /// <param name="element">The VisualElement associated to the attributes to view or edit</param>
    /// <param name="environment">The environment where the uxml attributes are view or edited</param>
    /// <param name="isReadOnly">Indicates whether the attributes are read-only</param>
    public void Set(VisualElement element, bool isReadOnly = false)
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
            Init();
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

    protected virtual void Init()
    {
        if (element != null)
        {
            uxmlSerializedDataDescription = UxmlSerializedDataRegistry.GetDescription(element.fullTypeName);

            if (uxmlSerializedDataDescription == null)
                return;

            var templateAsset = element.templateAsset;
            bool isTemplateInstance = templateAsset != null;

            var elementAsset = element.visualElementAsset;
            
            if (elementAsset == null || isTemplateInstance)
            {
                tempSerializedData = element.GetProperty(k_TempSerializedDataPropertyName) as TempSerializedData;

                if (tempSerializedData == null)
                {
                    tempSerializedData = ScriptableObject.CreateInstance<TempSerializedData>();

                    // We need to keep the serialized data alive so we can undo/redo changes
                    element.SetProperty(k_TempSerializedDataPropertyName, tempSerializedData);

                    tempSerializedData.serializedData = uxmlSerializedDataDescription.CreateDefaultSerializedData();

                    // In staging mode we want to show only the UXML data, Pulling from the live element will copy values
                    // that may be different in the serialized UXML.
                    // We currently make an exception for templates: rebuilding them solely from serialized data would
                    // require walking and reassembling the entire asset, which is complex and error‑prone.
                    // This is a temporary workaround limited to templates and may be removed once we have a better approach.
                    if (isTemplateInstance)
                        uxmlSerializedDataDescription.SyncSerializedData(element, tempSerializedData.serializedData);
                }

                tempSerializedData.hideFlags = isTemplateInstance && !isReadOnly ? HideFlags.None : HideFlags.NotEditable;

                rootSerializedObject = new SerializedObject(tempSerializedData);
                serializedBasePath = k_TempSerializedRootPath;
                uxmlSerializedData = tempSerializedData.serializedData;
            }
            else
            {
                var visualTreeAsset = element.visualTreeAssetSource;
                visualTreeAsset.hideFlags = isReadOnly ? HideFlags.NotEditable : HideFlags.None;

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
        Set(null);
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
