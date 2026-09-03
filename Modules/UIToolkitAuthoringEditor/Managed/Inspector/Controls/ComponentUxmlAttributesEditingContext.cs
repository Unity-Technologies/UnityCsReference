// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// An attributes editing context that targets one UI Toolkit component attached to an element,
/// instead of the element's own serialized data. It points the shared view/controller stack at the
/// matching entry of <see cref="VisualElementAsset.componentData"/>, so component attributes are
/// edited and persisted through exactly the same path as element attributes.
/// </summary>
/// <remarks>
/// The component to edit is identified by its generated <c>UxmlSerializedData</c> type. There is at
/// most one component of each type per element, so the type alone locates the entry, even if the
/// list order changes when other components are added or removed.
/// </remarks>
sealed class ComponentUxmlAttributesEditingContext : UxmlAttributesEditingContext
{
    // The component type is known only at runtime, so the live value is read by reflection
    // through ComponentValueReflection (a cold path, once per selection / sync).
    [NoAutoStaticsCleanup]
    static readonly MethodInfo k_HasComponentMethod =
        typeof(VisualElement).GetMethod(nameof(VisualElement.HasComponent));

    readonly UxmlSerializedDataDescription m_Description;
    readonly Type m_ComponentDataType;

    public ComponentUxmlAttributesEditingContext(UxmlAttributesEditingController editingController,
        UxmlSerializedDataDescription description)
        : base(editingController)
    {
        m_Description = description;
        m_ComponentDataType = description?.serializedDataType;
    }

    /// <summary>The component <c>partial struct</c> type, used by the inspector for add/remove and filtering.</summary>
    public Type componentType => m_ComponentDataType?.DeclaringType;

    static int IndexOfComponent(VisualElementAsset elementAsset, Type componentDataType)
    {
        var data = elementAsset?.componentData;
        if (data == null)
            return -1;
        for (var i = 0; i < data.Count; ++i)
        {
            if (data[i] != null && data[i].GetType() == componentDataType)
                return i;
        }
        return -1;
    }

    public override UxmlSerializedData uxmlSerializedData
    {
        get
        {
            var i = IndexOfComponent(elementAsset, m_ComponentDataType);
            return i >= 0 ? elementAsset.componentData[i] : null;
        }
    }

    // Reach the component entry inside the element's component list rather than the element's own data.
    protected override string SerializedDataRelativePath
        => $"m_ComponentData.Array.data[{IndexOfComponent(elementAsset, m_ComponentDataType)}]";

    // A component has no UxmlAsset of its own. Its attribute values live only in the component serialized
    // data, and the exporter rebuilds the component node from that data. Mirroring them onto the owner's
    // UxmlAsset would write the component's attributes onto the owner element node instead.
    internal override bool mirrorsAttributesToUxmlAsset => false;

    // A component attribute binds through the ${component:<Type>}.<field> selector rather than an element
    // property path, so validate the path against the component's data rather than the element property bag.
    internal override bool IsBindablePath(VisualElement element, BindingId bindingPath)
    {
        string path = bindingPath;
        return !string.IsNullOrEmpty(path) && SerializedPropertyExtensions.TryGetComponentAttributeType(path, out _);
    }

    // A template-instance edit persists as a component override (a whole component UxmlSerializedData in the
    // template's componentAttributeOverrides), not an element attribute override.
    internal override void WriteTemplateAttributeOverride(UxmlSerializedAttributeDescription attribute, object value)
        => SetComponentAttributeOverrideCommand.Execute(CommandSources.Inspector, editedVisualTreeAsset, m_Description, attribute, element, value);

    // A component override lives in TemplateAsset.componentAttributeOverrides, so the override bar must read it
    // from there (not from the element's attribute overrides) when this component sits at a template instance.
    internal override bool? IsTemplateAttributeOverridden(UxmlSerializedAttributeDescription attribute)
        => UxmlAssetUtilities.HasComponentAttributeOverrideInRootTemplate(editedVisualTreeAsset, element, m_Description, attribute);

    // Live attribute values are read from the component, not the element: the component's attribute
    // descriptions read fields of the component struct. Returns null when the component is not attached
    // to the live element, which tells the live-property sync there is nothing to read.
    internal override object liveAttributeOwner
    {
        get
        {
            if (element == null || componentType == null)
                return null;
            var hasComponent = (bool)k_HasComponentMethod.MakeGenericMethod(componentType).Invoke(element, null);
            return hasComponent ? ComponentValueReflection.GetComponentValueMethod.MakeGenericMethod(componentType).Invoke(null, new object[] { element }) : null;
        }
    }

    protected override void Init(VisualTreeAsset editedVisualTreeAsset)
    {
        this.editedVisualTreeAsset = editedVisualTreeAsset;

        if (element == null || m_Description == null)
        {
            isInTemplateInstance = false;
            return;
        }

        uxmlSerializedDataDescription = m_Description;

        var templateAsset = element.templateAsset;
        isInTemplateInstance = templateAsset != null && editedVisualTreeAsset != element.visualTreeAssetSource;

        var elementAsset = element.visualElementAsset;

        // No editable backing asset for this element — either it has no VisualElementAsset (a runtime or
        // script-created element selected in the main scene stage) or it is a template instance. Mirror the
        // element attributes inspector: show the component read-only against a temporary VisualTreeAsset, so
        // the fields appear instead of just the component name. The temp is shared with the element
        // attributes inspector; add this component to it, populated from the live component so real values show.
        if (elementAsset == null || isInTemplateInstance)
        {
            var temp = element.GetProperty(k_TempSerializedDataPropertyName) as TempSerializedData
                       ?? TempSerializedData.Create(element, isInTemplateInstance);
            tempSerializedData = temp;

            if (IndexOfComponent(temp.elementAsset, m_ComponentDataType) < 0)
            {
                var data = (UxmlComponentSerializedData)m_Description.CreateDefaultSerializedData();
                var live = liveAttributeOwner;
                if (live != null)
                    m_Description.SyncSerializedData(live, data);
                temp.elementAsset.AddComponentData(data);
            }

            visualTreeAsset = temp;
            this.elementAsset = temp.elementAsset;
            rootSerializedObject = new SerializedObject(temp);
            serializedBasePath = GetSerializedPath(temp.elementAsset);
            return;
        }

        var vta = elementAsset.visualTreeAsset;
        if (vta == null)
            return;

        // The entry must already exist (the inspector adds it before binding the context).
        if (IndexOfComponent(elementAsset, m_ComponentDataType) < 0)
            return;

        vta.hideFlags = isReadOnly ? HideFlags.NotEditable : HideFlags.None;

        this.visualTreeAsset = vta;
        this.elementAsset = elementAsset;
        rootSerializedObject = new SerializedObject(vta);
        serializedBasePath = GetSerializedPath(elementAsset);
    }
}
