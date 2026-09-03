// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Inspector section that lists the UI Toolkit components attached to the selected element, lets the
/// user add and remove them, and edits each component's <c>[UxmlAttribute]</c> fields. It reuses the
/// same attribute view, controller, and change handler as the element attributes section, pointed at
/// the element's <see cref="VisualElementAsset.componentData"/> through
/// <see cref="ComponentUxmlAttributesEditingContext"/>, so component edits persist exactly like
/// element attribute edits.
/// </summary>
[UxmlElement]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed partial class VisualElementComponentsInspectorElement : VisualElement
{
    public static readonly BindingId TargetProperty = nameof(Target);
    public static readonly BindingId IsReadOnlyProperty = nameof(IsReadOnly);

    // Raised after a component is added or removed. A host (for example UI Builder) subscribes to refresh
    // its own views and mark its document changed; the standalone inspector leaves it unset.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal event Action changed;

    const string k_UssClassName = "unity-visual-element-components-inspector";
    const string k_AddButtonUssClassName = k_UssClassName + "__add-button";
    const string k_EntryUssClassName = k_UssClassName + "__entry";
    const string k_RemoveButtonUssClassName = k_UssClassName + "__remove-button";
    const string k_DataFoldoutUssClassName = k_UssClassName + "__data-foldout";

    static readonly string k_AddComponentText = L10n.Tr("Add Component", null);
    static readonly string k_RemoveText = L10n.Tr("Remove", null);
    static readonly string k_UnresolvedComponentText = L10n.Tr("Unresolved component (type missing)", null);

    const string k_AddComponentUndoName = "Add component to element";
    const string k_RemoveComponentUndoName = "Remove component from element";

    // RemoveComponent<T>() is generic-only; the inspector knows the type only at runtime, so it calls
    // the live detach through reflection (a cold path, once per remove).
    [NoAutoStaticsCleanup]
    static readonly MethodInfo k_RemoveComponentMethod =
        typeof(VisualElement).GetMethod(nameof(VisualElement.RemoveComponent));

    VisualElement m_Target;
    bool m_IsReadOnly;

    readonly VisualElement m_ComponentsContainer;
    readonly Button m_AddButton;
    readonly List<ComponentEntryView> m_Entries = new();

    [CreateProperty]
    public VisualElement Target
    {
        get => m_Target;
        set
        {
            if (m_Target == value)
                return;
            m_Target = value;
            Rebuild();
            NotifyPropertyChanged(TargetProperty);
        }
    }

    [CreateProperty]
    public bool IsReadOnly
    {
        get => m_IsReadOnly;
        set
        {
            if (m_IsReadOnly == value)
                return;
            m_IsReadOnly = value;
            Rebuild();
            NotifyPropertyChanged(IsReadOnlyProperty);
        }
    }

    public VisualElementComponentsInspectorElement()
    {
        AddToClassList(k_UssClassName);

        m_ComponentsContainer = new VisualElement();
        Add(m_ComponentsContainer);

        m_AddButton = new Button(OnAddClicked) { text = k_AddComponentText };
        m_AddButton.AddToClassList(k_AddButtonUssClassName);
        Add(m_AddButton);

        // A bound attribute field would otherwise show the default SerializedProperty right-click menu (the
        // binding system shows it and stops the event). This hook tells that path "a field decorator handles
        // its own menu", which lets our Unset / Unset All / binding menu through — the same hook the element
        // attributes inspector registers, so the components section works on its own and not only when the
        // attributes section happens to be present.
        RegisterCallback<AttachToPanelEvent>(_ => BindingsStyleHelpers.HandleRightClickMenu += HandleRightClickMenu);
        RegisterCallback<DetachFromPanelEvent>(_ => BindingsStyleHelpers.HandleRightClickMenu -= HandleRightClickMenu);
    }

    static void HandleRightClickMenu(VisualElement ve, ref bool handled)
    {
        while (ve != null)
        {
            if (ve is UxmlAttributeFieldDecorator)
            {
                handled = true;
                return;
            }
            ve = ve.parent;
        }
    }

    // Components are editable only against a real backing asset; script-created elements (no
    // VisualElementAsset) and read-only selections show their components but cannot be changed.
    bool CanEdit => !m_IsReadOnly && m_Target?.visualElementAsset != null;

    void Rebuild()
    {
        ClearEntries();

        var elementAsset = m_Target?.visualElementAsset;
        if (elementAsset != null && elementAsset.hasComponentData)
        {
            foreach (var data in elementAsset.componentData)
            {
                var description = ResolveDescription(data);
                if (description == null)
                {
                    // The component's type can't be resolved (deleted/renamed, or a compile error
                    // elsewhere). Show a disabled placeholder rather than hiding it, so the author sees the
                    // component still exists in the asset and isn't surprised when saving warns about it.
                    var placeholder = new Label(k_UnresolvedComponentText);
                    placeholder.SetEnabled(false);
                    m_ComponentsContainer.Add(placeholder);
                    continue;
                }

                var componentType = description.serializedDataType.DeclaringType;
                var entry = new ComponentEntryView(m_Target, description, m_IsReadOnly,
                    CanEdit ? () => RemoveComponent(componentType) : null);
                m_Entries.Add(entry);
                m_ComponentsContainer.Add(entry);
            }
        }

        m_AddButton.style.display = CanEdit ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void ClearEntries()
    {
        foreach (var entry in m_Entries)
            entry.DisposeContext();
        m_Entries.Clear();
        m_ComponentsContainer.Clear();
    }

    static UxmlSerializedDataDescription ResolveDescription(UnityEngine.UIElements.UxmlSerializedData data)
    {
        var declaringType = data?.GetType().DeclaringType;
        return declaringType == null ? null : UxmlSerializedDataRegistry.GetDescription(declaringType.FullName);
    }

    void OnAddClicked()
    {
        if (!CanEdit)
            return;

        var candidates = GetAddableComponents();
        var dropdown = new AddComponentDropdown(candidates, AddComponent);
        dropdown.Show(m_AddButton.worldBound);
    }

    internal List<UxmlSerializedDataDescription> GetAddableComponents()
    {
        var result = new List<UxmlSerializedDataDescription>();
        var elementAsset = m_Target?.visualElementAsset;
        if (elementAsset == null)
            return result;

        foreach (var pair in UxmlSerializedDataRegistry.SerializedDataTypes)
        {
            var description = UxmlSerializedDataRegistry.GetDescription(pair.Key);
            if (description == null || !description.isComponent)
                continue;

            var componentType = description.serializedDataType?.DeclaringType;
            if (componentType == null)
                continue;

            // [RequiresElementOfType]: only offer components whose owner constraint the element satisfies.
            var requires = componentType.GetCustomAttribute<RequiresElementOfTypeAttribute>();
            if (requires != null && !requires.ElementType.IsInstanceOfType(m_Target))
                continue;

            // One component per type per element.
            if (IndexOfComponent(elementAsset, description.serializedDataType) >= 0)
                continue;

            result.Add(description);
        }

        result.Sort(static (a, b) => string.CompareOrdinal(
            a.serializedDataType.DeclaringType.Name, b.serializedDataType.DeclaringType.Name));
        return result;
    }

    internal void AddComponent(UxmlSerializedDataDescription description)
    {
        var elementAsset = m_Target?.visualElementAsset;
        var vta = elementAsset?.visualTreeAsset;
        if (vta == null)
            return;

        Undo.RegisterCompleteObjectUndo(vta, k_AddComponentUndoName);

        var data = (UxmlComponentSerializedData)description.CreateDefaultSerializedData();
        elementAsset.AddComponentData(data);
        EditorUtility.SetDirty(vta);

        // Attach to the live element so the change is visible immediately, mirroring the importer:
        // CreateInstance(owner) does owner.GetOrAddComponent<T>(); Deserialize writes the authored values.
        data.CreateInstance(m_Target);
        data.Deserialize(m_Target);
        UxmlAssetUtilities.ReapplyAncestorSerializedDataOverrides(m_Target);

        UIElementsUtility.MarkVisualTreeAssetAsChanged(vta);
        Rebuild();
        changed?.Invoke();
    }

    internal void RemoveComponent(Type componentType)
    {
        var elementAsset = m_Target?.visualElementAsset;
        var vta = elementAsset?.visualTreeAsset;
        if (vta == null || componentType == null)
            return;

        var index = IndexOfComponentByOwnerType(elementAsset, componentType);
        if (index < 0)
            return;

        // Detach the live component FIRST, then edit the asset. RemoveComponent<T>() is generic-only, so
        // dispatch by reflection. If the detach throws, leave the asset untouched and bail — otherwise the
        // asset would say "no component" while the live element still carries it until the next Rebuild.
        if (k_RemoveComponentMethod != null)
        {
            try
            {
                k_RemoveComponentMethod.MakeGenericMethod(componentType).Invoke(m_Target, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
        }

        Undo.RegisterCompleteObjectUndo(vta, k_RemoveComponentUndoName);
        elementAsset.componentData.RemoveAt(index);
        EditorUtility.SetDirty(vta);

        UIElementsUtility.MarkVisualTreeAssetAsChanged(vta);
        Rebuild();
        changed?.Invoke();
    }

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

    static int IndexOfComponentByOwnerType(VisualElementAsset elementAsset, Type componentType)
    {
        var data = elementAsset?.componentData;
        if (data == null)
            return -1;
        for (var i = 0; i < data.Count; ++i)
        {
            if (data[i] != null && data[i].GetType().DeclaringType == componentType)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// One attached component: a foldout whose header carries the component name and a remove button,
    /// and whose body hosts a <see cref="UxmlAttributesView"/> bound to the component's serialized data.
    /// </summary>
    sealed class ComponentEntryView : VisualElement
    {
        readonly UxmlAttributesView m_AttributesView;
        PropertyField m_RootPropertyField;

        public ComponentEntryView(VisualElement target, UxmlSerializedDataDescription description,
            bool isReadOnly, Action onRemove)
        {
            AddToClassList(k_EntryUssClassName);

            var foldout = new Foldout { text = description.serializedDataType.DeclaringType.Name, value = true };
            Add(foldout);

            if (onRemove != null)
            {
                var removeButton = new Button(onRemove) { text = k_RemoveText };
                removeButton.AddToClassList(k_RemoveButtonUssClassName);
                var header = foldout.Q<Toggle>(className: Foldout.toggleUssClassName);
                (header ?? (VisualElement)foldout).Add(removeButton);
            }

            // Swap the view's default element context for a component-targeting one, then bind it.
            m_AttributesView = new UxmlAttributesView();
            m_AttributesView.Context.Dispose();
            m_AttributesView.Context = new ComponentUxmlAttributesEditingContext(
                new UxmlAttributesEditingController(), description);
            m_AttributesView.ContextChanged += OnContextChanged;
            foldout.Add(m_AttributesView);

            m_AttributesView.Context.Set(target, isReadOnly);

            // Inline [StyleProperty] editing, persisted as custom properties on the element's inline
            // stylesheet (see ComponentStylePropertiesView). Editable only against a real backing asset.
            var componentType = description.serializedDataType.DeclaringType;
            if (componentType != null && ComponentStylePropertiesView.HasStyleProperties(componentType))
            {
                var canEdit = !isReadOnly && target?.visualElementAsset != null;
                foldout.Add(new ComponentStylePropertiesView(target, componentType, canEdit));
            }
        }

        // Mirror of VisualElementAttributesInspectorElement.OnContextChanged: bind a root PropertyField
        // to the component's serialized base path; the property drawer expands it into the attribute UI.
        void OnContextChanged(object sender, UxmlAttributesEditingContext.ContextChangedEventArgs args)
        {
            if (sender is not UxmlAttributesView view || view.Context == null)
                return;

            if (view.Context.uxmlSerializedDataDescription == null)
            {
                m_RootPropertyField?.RemoveFromHierarchy();
                m_RootPropertyField = null;
                return;
            }

            if (m_RootPropertyField == null)
            {
                m_RootPropertyField = new PropertyField();
                m_RootPropertyField.AddToClassList(VisualElementAttributesInspectorElement.k_RootPropertyFieldUssClassName);
                // A component's serialized data is a [SerializeReference] entry in a list, so the property
                // drawer wraps the attribute fields in an array-element foldout ("Element 0"). Hide that
                // wrapper's header so the fields sit directly under the component foldout.
                m_RootPropertyField.RegisterCallback<GeometryChangedEvent>(_ => HideDataFoldoutHeader());
                m_AttributesView.Add(m_RootPropertyField);
            }

            m_RootPropertyField.bindingPath = view.Context.serializedBasePath;
        }

        void HideDataFoldoutHeader()
        {
            // The first descendant foldout is the array-element wrapper the drawer adds for the component
            // data. Collapse its header (a nested foldout for a [UxmlObjectReference] attribute, if any,
            // is deeper and left untouched).
            var dataFoldout = m_RootPropertyField?.Q<Foldout>();
            if (dataFoldout == null)
                return;

            dataFoldout.value = true;
            dataFoldout.AddToClassList(k_DataFoldoutUssClassName);   // header hide + content margin live in USS
        }

        public void DisposeContext()
        {
            m_AttributesView.Context.Dispose();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
