// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
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
            instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

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
    public VisualTreeAsset editedVisualTreeAsset { get; protected set; }

    /// <summary>
    /// The VisualTreeAsset that contains the UXML elements being edited or the temporary VisualTreeAsset
    /// used to edit template instances and VisualElements dynamically created.
    /// </summary>
    public VisualTreeAsset visualTreeAsset { get; protected set; }

    /// <summary>
    /// The VisualElementAsset being edited.
    /// </summary>
    public VisualElementAsset elementAsset { get; protected set; }

    /// <summary>
    /// The serialized data being edited element.
    /// </summary>
    /// <remarks>
    /// Virtual so a component-targeting context can return one of the element's
    /// <see cref="VisualElementAsset.componentData"/> entries instead of the element's own data.
    /// </remarks>
    public virtual UxmlSerializedData uxmlSerializedData
    {
        get
        {
            if (tempSerializedData != null)
            {
                return tempSerializedData.serializedData;
            }
            else if (elementAsset != null)
            {
                return elementAsset.serializedData;
            }

            return null;
        }
    }

    /// <summary>
    /// The object the live attribute values are read from when syncing the inspector to runtime state.
    /// For an element that is the element itself (its attributes are members of the element); a
    /// component context overrides this to return the live component value, whose fields the
    /// component's attribute descriptions actually read.
    /// </summary>
    internal virtual object liveAttributeOwner => element;

    /// <summary>
    /// Whether edited attribute values are mirrored onto the element's <see cref="UxmlAsset"/> (its
    /// stored UXML properties), in addition to the serialized data. This is how an element's attributes
    /// are kept ready for export. A component context returns false: a component has no UxmlAsset of its
    /// own, its values live only in the component serialized data, and the exporter rebuilds the
    /// component node from that data. Mirroring would otherwise write the component's attributes onto the
    /// owner element node, which is wrong.
    /// </summary>
    internal virtual bool mirrorsAttributesToUxmlAsset => true;

    /// <summary>
    /// Whether the attribute fields in this context can be data-bound through the inspector (the binding
    /// section of the field's right-click menu, and the bound-state affordance). Both element and component
    /// contexts return true; they differ only in how a candidate path is validated (see
    /// <see cref="IsBindablePath"/>).
    /// </summary>
    internal virtual bool supportsAttributeBindings => true;

    /// <summary>
    /// Whether <paramref name="bindingPath"/> addresses a real, bindable target. The element flow validates
    /// against the element's own property bag; a component context validates the
    /// <c>component:&lt;Type&gt;.&lt;field&gt;</c> selector against the component's data instead.
    /// </summary>
    internal virtual bool IsBindablePath(VisualElement element, BindingId bindingPath)
    {
        var container = element;
        string path = bindingPath;
        return !string.IsNullOrEmpty(path) && PropertyContainer.IsPathValid(ref container, bindingPath);
    }

    /// <summary>
    /// Persists an attribute edit made at a template instance as an override on the root template. The base
    /// writes an element attribute override; a component context overrides this to write a component override
    /// (TemplateAsset.componentAttributeOverrides) instead, since the two use different storage.
    /// </summary>
    internal virtual void WriteTemplateAttributeOverride(UxmlSerializedAttributeDescription attribute, object value)
        => SetAttributeOverrideCommand.Execute(CommandSources.Inspector, editedVisualTreeAsset, attribute, element, value);

    /// <summary>
    /// The read counterpart of <see cref="WriteTemplateAttributeOverride"/>: at a template instance, is this
    /// attribute overridden on the root template? Returns null when this context has no special storage and the
    /// shared element-attribute path should answer; a component context returns a non-null result because its
    /// override lives in TemplateAsset.componentAttributeOverrides, which the element path cannot see.
    /// </summary>
    internal virtual bool? IsTemplateAttributeOverridden(UxmlSerializedAttributeDescription attribute) => null;

    /// <summary>
    /// Indicates whether the current element is part of a template instance.
    /// </summary>
    public bool isInTemplateInstance { get; protected set; }

    /// <summary>
    /// The VisualElement that is currently being viewed or edited.
    /// </summary>
    public VisualElement element { get; private set; }

    /// <summary>
    /// The serialized object created from the VisualTreeAsset of the VisualElement being viewed or
    /// the live object used to view read-only attributes.
    /// This serialized object is used to resolved paths to serialized attribute properties.
    /// </summary>
    public SerializedObject rootSerializedObject { get; protected set; }

    /// <summary>
    /// The serialized path from the current uxml element to the current VisualTreeAsset. Using the rootSerializedObject, this path is used as base path to locate attribute properties in the serialized data.
    /// </summary>
    public string serializedBasePath { get; protected set; }

    /// <summary>
    /// The UxmlSerializedDataDescription that describes the serialized data for the current element.
    /// </summary>
    public UxmlSerializedDataDescription uxmlSerializedDataDescription { get; protected set; }

    /// <summary>
    /// Indicates whether the attributes are read-only in this context.
    /// </summary>
    public bool isReadOnly { get; private set; }

    bool m_StageShowsAncestorOverrides;

    /// <summary>
    /// True where fields never surface ancestor Attribute Overrides, such as the binding editor.
    /// </summary>
    internal bool suppressAncestorOverrides { get; set; }

    /// <summary>
    /// Whether fields in this context surface Attribute Overrides declared by ancestor UXML instances.
    /// </summary>
    /// <remarks>
    /// Snapshot taken when the context is set, so the answer stays consistent with the document chosen
    /// for editing when a stage opens or closes afterwards.
    /// </remarks>
    internal bool showsAncestorOverrides => m_StageShowsAncestorOverrides && !suppressAncestorOverrides;

    internal TempSerializedData tempSerializedData { get; set; }

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
    /// <param name="editedVisualTreeAsset">The VisualTreeAsset the uxml attributes are view or edited from</param>
    /// <param name="element">The VisualElement associated to the attributes to view or edit</param>
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
        m_StageShowsAncestorOverrides = StageUtility.GetCurrentStage() is not VisualElementEditingStage;

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
                rootSerializedObject = new SerializedObject(tempSerializedData);
                serializedBasePath = GetSerializedPath(tempSerializedData.elementAsset);
            }
            else
            {
                // Use the element's actual VTA, which may differ from editedVisualTreeAsset
                // when selecting elements outside the edited sub-document
                var visualTreeAsset = elementAsset.visualTreeAsset;

                // If the UXML file has been modified, the element may no longer be in the asset so we will ignore it. (UUM-59305)
                if (visualTreeAsset == null)
                {
                    Clear();
                    return;
                }

                if (elementAsset.serializedData == null)
                {
                    elementAsset.serializedData = uxmlSerializedDataDescription.CreateDefaultSerializedData();
                    elementAsset.serializedData.uxmlAssetId = elementAsset.id;
                }

                this.visualTreeAsset = visualTreeAsset;
                this.elementAsset = elementAsset;

                // The UXML importer marks imported assets NotEditable, which disables every bound attribute field.
                if (!isReadOnly)
                    visualTreeAsset.hideFlags &= ~HideFlags.NotEditable;

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
        elementAsset = null;
        uxmlSerializedDataDescription = null;
        tempSerializedData = null;
        rootSerializedObject = null;
        serializedBasePath = string.Empty;
        isReadOnly = false;
    }

    /// <summary>
    /// The serialized field segment appended to the element path to reach the data being edited.
    /// The element context edits the element's own <c>m_SerializedData</c>; a component context
    /// overrides this to reach a specific entry of the element's <c>m_ComponentData</c> list.
    /// </summary>
    protected virtual string SerializedDataRelativePath => k_UxmlSerializedDataFieldName;

    protected string GetSerializedPath(UxmlAsset asset)
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
        sb.Append(SerializedDataRelativePath);

        return sb.ToString();
    }

    public void Dispose()
    {
        Clear();
        editingController.Dispose();
    }
}
