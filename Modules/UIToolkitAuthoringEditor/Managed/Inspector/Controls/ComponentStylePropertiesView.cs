// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Edits the <see cref="StylePropertyAttribute"/> ("[StyleProperty]") fields of one attached component.
/// A component's <c>[StyleProperty("--name")]</c> field maps to a USS custom property, so an inline
/// override is persisted the same way a VisualElement's inline styles are: as a custom property on the
/// owning element's inline stylesheet (<see cref="VisualTreeAsset.inlineSheet"/>). At runtime the existing
/// <c>CustomStyleResolvedEvent</c> path applies that value back into the component, so no extra read path
/// is needed here.
/// </summary>
/// <remarks>
/// One row per supported field. A field whose USS name is present in the element's inline rule is
/// "overridden" (it shows the override bar, and a right-click menu offers Unset). Editing a row writes the
/// custom property; Unset removes it, returning the field to the cascade. The shipped <c>[StyleProperty]</c>
/// value types are editable: <see cref="Color"/>, <see cref="float"/>, <see cref="int"/>, <see cref="bool"/>,
/// <see cref="Length"/>, and any enum (shown with the default enum popup, stored as a USS identifier token).
/// </remarks>
sealed class ComponentStylePropertiesView : VisualElement
{
    const string k_UssClassName = "unity-visual-element-components-inspector__style-properties";
    const string k_RowUssClassName = k_UssClassName + "__row";

    [NoAutoStaticsCleanup]
    static readonly MethodInfo k_HasComponentMethod =
        typeof(VisualElement).GetMethod(nameof(VisualElement.HasComponent));

    readonly VisualElement m_Target;
    readonly Type m_ComponentType;
    readonly bool m_CanEdit;

    // One refresher per row; re-reads the row's value and override marker. Run when the owner's custom
    // styles resolve, so a row reflects the value the cascade (or an inline override) produced.
    readonly List<Action> m_RowRefreshers = new();

    public ComponentStylePropertiesView(VisualElement target, Type componentType, bool canEdit)
    {
        AddToClassList(k_UssClassName);
        m_Target = target;
        m_ComponentType = componentType;
        m_CanEdit = canEdit;

        foreach (var field in GetStylePropertyFields(componentType))
        {
            var row = BuildRow(field, field.GetCustomAttribute<StylePropertyAttribute>().name);
            if (row != null)
                Add(row);
        }

        // A [StyleProperty] value is produced during style resolution (USS cascade or an inline override),
        // so re-read each row when the owner's custom styles resolve.
        RegisterCallback<AttachToPanelEvent>(_ => m_Target?.RegisterCallback<CustomStyleResolvedEvent>(OnTargetStyleResolved));
        RegisterCallback<DetachFromPanelEvent>(_ => m_Target?.UnregisterCallback<CustomStyleResolvedEvent>(OnTargetStyleResolved));
    }

    void OnTargetStyleResolved(CustomStyleResolvedEvent _) => RefreshAll();

    void RefreshAll()
    {
        foreach (var refresh in m_RowRefreshers)
            refresh();
    }

    // True if any of the component's editable [StyleProperty] fields currently has an inline override.
    internal bool AnyOverridden()
    {
        foreach (var field in GetStylePropertyFields(m_ComponentType))
        {
            var name = field.GetCustomAttribute<StylePropertyAttribute>().name;
            if (IsSupported(field.FieldType) && IsOverridden(name))
                return true;
        }
        return false;
    }

    internal void ClearAllOverrides()
    {
        foreach (var field in GetStylePropertyFields(m_ComponentType))
        {
            var name = field.GetCustomAttribute<StylePropertyAttribute>().name;
            if (IsSupported(field.FieldType) && IsOverridden(name))
                ClearOverride(name);
        }
    }

    /// <summary>True if the component declares at least one editable [StyleProperty] field.</summary>
    public static bool HasStyleProperties(Type componentType)
    {
        foreach (var field in GetStylePropertyFields(componentType))
        {
            if (IsSupported(field.FieldType))
                return true;
        }
        return false;
    }

    static IEnumerable<FieldInfo> GetStylePropertyFields(Type componentType)
    {
        foreach (var field in componentType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = field.GetCustomAttribute<StylePropertyAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.name))
                yield return field;
        }
    }

    static bool IsSupported(Type t) =>
        t == typeof(Color) || t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(Length) || t.IsEnum;

    static VariablesInspector.VariableType VariableTypeFor(Type t)
    {
        if (t == typeof(Color)) return VariablesInspector.VariableType.Color;
        if (t == typeof(bool)) return VariablesInspector.VariableType.Keyword;
        if (t == typeof(Length)) return VariablesInspector.VariableType.Length;
        if (t.IsEnum) return VariablesInspector.VariableType.Enum; // stored as a USS identifier token
        // float and int are both stored as a USS number (int is read back via a float cast).
        return VariablesInspector.VariableType.Float;
    }

    VisualElement BuildRow(FieldInfo field, string ussName)
    {
        var type = field.FieldType;
        if (!IsSupported(type))
            return null;

        // The editor field, typed to the value. Each one writes the override on value change.
        VisualElement editor;
        Action refreshValue; // pushes the current effective value into the editor field

        if (type == typeof(Color))
        {
            var f = new ColorField(ussName);
            f.RegisterValueChangedCallback(e => SetOverride(ussName, field.FieldType, e.newValue));
            refreshValue = () => f.SetValueWithoutNotify(CurrentValue(field) is Color c ? c : default);
            editor = f;
        }
        else if (type == typeof(float))
        {
            var f = new FloatField(ussName);
            f.RegisterValueChangedCallback(e => SetOverride(ussName, field.FieldType, e.newValue));
            refreshValue = () => f.SetValueWithoutNotify(CurrentValue(field) is float v ? v : 0f);
            editor = f;
        }
        else if (type == typeof(int))
        {
            var f = new IntegerField(ussName);
            f.RegisterValueChangedCallback(e => SetOverride(ussName, field.FieldType, e.newValue));
            refreshValue = () => f.SetValueWithoutNotify(CurrentValue(field) is int v ? v : 0);
            editor = f;
        }
        else if (type == typeof(Length))
        {
            var f = new LengthField(ussName);
            f.RegisterValueChangedCallback(e => SetOverride(ussName, field.FieldType, e.newValue));
            refreshValue = () => f.SetValueWithoutNotify(CurrentValue(field) is Length v ? v : new Length());
            editor = f;
        }
        else if (type.IsEnum)
        {
            // A [Flags] enum needs the multi-select EnumFlagsField; a plain enum uses the single-select
            // popup. Both need a seed value to learn the enum type (EnumFlagsField shows "Nothing" for a
            // 0 seed even when the enum declares no zero member, so the seed is safe for either).
            var seed = (Enum)Enum.ToObject(type, 0);
            BaseField<Enum> f = type.IsDefined(typeof(FlagsAttribute), false)
                ? new EnumFlagsField(ussName, seed)
                : new EnumField(ussName, seed);
            f.RegisterValueChangedCallback(e => SetOverride(ussName, field.FieldType, e.newValue));
            refreshValue = () => f.SetValueWithoutNotify(CurrentValue(field) is Enum v ? v : seed);
            editor = f;
        }
        else // bool
        {
            var f = new Toggle(ussName);
            f.RegisterValueChangedCallback(e => SetOverride(ussName, field.FieldType, e.newValue));
            refreshValue = () => f.SetValueWithoutNotify(CurrentValue(field) is bool v && v);
            editor = f;
        }

        editor.AddToClassList(k_RowUssClassName);
        editor.AddToClassList(BaseField<float>.alignedFieldUssClassName);
        editor.SetEnabled(m_CanEdit);

        // Wrap the field in an OverrideRow so an overridden property shows the same left-gutter bar that
        // built-in style fields use, instead of a bold label.
        var row = new OverrideRow();
        row.AddToClassList(OverrideRow.ussClassName);
        row.AddTrackedProperty(ussName); // for inspector search
        var affordance = new FieldAffordanceElement();
        row.Add(affordance);
        row.Add(editor);

        void Refresh()
        {
            refreshValue();
            var overridden = IsOverridden(ussName);
            row.UpdateTrackedProperties(overridden ? new[] { ussName } : Array.Empty<string>());
            affordance.fieldAffordanceData.sourceTypeInfo =
                overridden ? FieldAffordanceSourceInfoType.Inline
                : IsFromCascade(ussName) ? FieldAffordanceSourceInfoType.MatchingUSSSelector
                : FieldAffordanceSourceInfoType.Default;
        }

        Refresh();
        m_RowRefreshers.Add(Refresh);

        void PopulateMenu(DropdownMenu menu)
        {
            if (!m_CanEdit)
                return;
            menu.AppendAction(UxmlAttributeFieldDecorator.k_UnsetText,
                _ => { ClearOverride(ussName); RefreshAll(); },
                _ => IsOverridden(ussName) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction(UxmlAttributeFieldDecorator.k_UnsetAllText,
                _ => { ClearAllOverrides(); RefreshAll(); },
                _ => AnyOverridden() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        affordance.populateMenuItems = PopulateMenu;
        editor.AddManipulator(new ContextualMenuManipulator(evt => PopulateMenu(evt.menu)));

        return row;
    }

    // The value to show: the component's current field value. After style resolution this already reflects
    // the inline override (the CustomStyleResolvedEvent path writes it into the component) or the cascade /
    // default when not overridden — so it is the effective value either way, with no cross-module sheet read.
    object CurrentValue(FieldInfo field)
    {
        var component = LiveComponentValue();
        return component != null ? field.GetValue(component) : null;
    }

    object LiveComponentValue() => LiveComponentValue(m_Target);

    object LiveComponentValue(VisualElement element)
    {
        if (element == null || m_ComponentType == null)
            return null;
        var has = (bool)k_HasComponentMethod.MakeGenericMethod(m_ComponentType).Invoke(element, null);
        return has ? ComponentValueReflection.GetComponentValueMethod.MakeGenericMethod(m_ComponentType).Invoke(null, new object[] { element }) : null;
    }

    // ----- Inline-rule access -----

    StyleRule GetInlineRule()
    {
        var inlineSheet = m_Target?.visualTreeAssetSource?.inlineSheet;
        var vea = m_Target?.visualElementAsset;
        if (inlineSheet == null || vea == null || vea.ruleIndex < 0 || inlineSheet.rules.Length <= vea.ruleIndex)
            return null;
        return inlineSheet.rules[vea.ruleIndex];
    }

    internal bool IsOverridden(string ussName) => GetInlineRule()?.FindLastProperty(ussName) != null;

    // A resolved custom property with no inline entry means a matching selector supplies the value.
    bool IsFromCascade(string ussName)
    {
        return m_Target != null
            && UniqueStyleString.TryGet(ussName, out var name)
            && m_Target.computedStyle.customProperties.TryGetValue(name, out _);
    }

    internal void SetOverride(string ussName, Type valueType, object value)
    {
        if (!m_CanEdit)
            return;
        var vta = m_Target?.visualTreeAssetSource;
        var vea = m_Target?.visualElementAsset;
        if (vta == null || vea == null)
            return;

        var inlineSheet = vta.GetOrCreateInlineStyleSheet();
        var rule = vta.GetOrCreateInlineStyleRule(vea);
        var variableType = VariableTypeFor(valueType);

        var property = rule.FindLastProperty(ussName);
        if (property == null)
        {
            AddStyleRulePropertyCommand.Execute(CommandSources.Inspector, inlineSheet, rule, ussName, variableType);
            property = rule.FindLastProperty(ussName);
        }

        if (property == null)
            return;

        WriteValue(inlineSheet, property, valueType, variableType, value);

        VisualElementUtility.UpdateInlineRuleOnAllClones(m_Target, inlineSheet, rule);
        MarkDocumentChanged();
    }

    static void WriteValue(StyleSheet inlineSheet, StyleProperty property, Type type, VariablesInspector.VariableType variableType, object value)
    {
        if (type == typeof(Color))
            WriteStyleRulePropertyValueCommand<Color>.Execute(CommandSources.Inspector, inlineSheet, property, variableType, (Color)value);
        else if (type == typeof(bool))
            WriteStyleRulePropertyValueCommand<StyleValueKeyword>.Execute(CommandSources.Inspector, inlineSheet, property, variableType, (bool)value ? StyleValueKeyword.True : StyleValueKeyword.False);
        else if (type == typeof(int))
            WriteStyleRulePropertyValueCommand<float>.Execute(CommandSources.Inspector, inlineSheet, property, variableType, (int)value);
        else if (type == typeof(Length))
            WriteStyleRulePropertyValueCommand<Length>.Execute(CommandSources.Inspector, inlineSheet, property, variableType, (Length)value);
        else if (type.IsEnum)
            // Stored as a dash-case identifier token (RowDense -> row-dense), the inverse of the resolver's parse.
            WriteStyleRulePropertyValueCommand<string>.Execute(CommandSources.Inspector, inlineSheet, property, variableType, StyleSheetUtility.GetEnumExportString((Enum)value));
        else // float
            WriteStyleRulePropertyValueCommand<float>.Execute(CommandSources.Inspector, inlineSheet, property, variableType, (float)value);
    }

    internal void ClearOverride(string ussName)
    {
        if (!m_CanEdit)
            return;
        var inlineSheet = m_Target?.visualTreeAssetSource?.inlineSheet;
        var rule = GetInlineRule();
        var property = rule?.FindLastProperty(ussName);
        if (inlineSheet == null || rule == null || property == null)
            return;

        RemoveStyleRulePropertyCommand.Execute(CommandSources.Inspector, inlineSheet, rule, property);

        // Every clone shares the inline rule, and each one's component holds its own copy of the value.
        VisualElementUtility.ForEachClone(m_Target, element => ResetFieldToDefault(element, ussName));
        VisualElementUtility.UpdateInlineRuleOnAllClones(m_Target, inlineSheet, rule);
        MarkDocumentChanged();
    }

    // The resolved-event handler only writes fields whose custom property is present, so removing the
    // property would otherwise leave the unset value in the component; the next style pass reapplies
    // any matching selector on top of this default.
    void ResetFieldToDefault(VisualElement element, string ussName)
    {
        FieldInfo target = null;
        foreach (var field in GetStylePropertyFields(m_ComponentType))
        {
            if (field.GetCustomAttribute<StylePropertyAttribute>().name == ussName)
            {
                target = field;
                break;
            }
        }

        var component = LiveComponentValue(element);
        if (target == null || component == null)
            return;

        var defaults = ComponentValueReflection.CreateComponentValueMethod
            .MakeGenericMethod(m_ComponentType).Invoke(null, null);
        target.SetValue(component, target.GetValue(defaults));
        ClearInlineOverrideBit(component, target.Name);
        ComponentValueReflection.SetComponentValueMethod.MakeGenericMethod(m_ComponentType)
            .Invoke(null, new object[] { element, component });
        if (target.IsDefined(typeof(UxmlAttributeAttribute)))
            ReapplyUxmlValues(element);

        // Mirror the generated Set{Field} / ClearInline{Field}: dirty the component, raise the field's
        // binding id when the generator emitted one, and force a style pass so USS can reclaim the field.
        ComponentValueReflection.MarkComponentDirtyMethod.MakeGenericMethod(m_ComponentType)
            .Invoke(null, new object[] { element });
        var property = m_ComponentType.GetField(target.Name + "Property",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.GetValue(null) is BindingId bindingId)
            element.NotifyComponentChanged(bindingId);
        element.MarkDirtyStyles();
    }

    // A field can also be a [UxmlAttribute], so the unset value is the authored one, as a fresh clone would get.
    void ReapplyUxmlValues(VisualElement element)
    {
        var componentData = element.visualElementAsset?.componentData;
        if (componentData == null)
            return;

        foreach (var data in componentData)
        {
            if (data != null && data.GetType().DeclaringType == m_ComponentType)
            {
                data.Deserialize(element);
                UxmlAssetUtilities.ReapplyAncestorSerializedDataOverrides(element);
                return;
            }
        }
    }

    // The C# inline pin (Set/SetInline{Field} sets a bit in the generated __styleOverrides) would make
    // the resolved-event handler skip the field forever after this reset.
    static void ClearInlineOverrideBit(object component, string fieldName)
    {
        var overrides = component.GetType().GetField("__styleOverrides",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (overrides == null)
            return;

        var pascalName = char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
        if (Array.IndexOf(Enum.GetNames(overrides.FieldType), pascalName) < 0)
            return;

        var bit = Convert.ToUInt64(Enum.Parse(overrides.FieldType, pascalName));
        var current = Convert.ToUInt64(overrides.GetValue(component));
        overrides.SetValue(component, Enum.ToObject(overrides.FieldType, current & ~bit));
    }

    // The style commands dirty the inline StyleSheet, not the owning asset, but Save tracks the asset — so
    // dirty it here too (this also persists the ruleIndex the first override sets on the element asset).
    void MarkDocumentChanged()
    {
        var vta = m_Target?.visualTreeAssetSource;
        if (vta == null)
            return;
        EditorUtility.SetDirty(vta);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(vta);
    }
}
