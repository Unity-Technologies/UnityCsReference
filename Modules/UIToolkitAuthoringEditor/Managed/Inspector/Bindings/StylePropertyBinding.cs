// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Debug = UnityEngine.Debug;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal enum TrackedPropertyType
{
    MarkOverride,
    ClearOverride,
    StopTracking,
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal interface ITrackablePropertyProvider
{
    event Action<ITrackablePropertyProvider, string, TrackedPropertyType> OnTrackedPropertyChanged;
}

[UxmlObject]
sealed partial class StylePropertyBinding : CustomBinding, ITrackablePropertyProvider
{
    [Flags]
    enum UpdateFlags
    {
        None = 0,
        IgnoreChanges = 1,
        Readonly = 2
    }

    readonly struct IgnoreChangeScope : IDisposable
    {
        readonly StylePropertyBinding m_Binding;
        readonly bool m_WasIgnoringChanges;

        public IgnoreChangeScope(StylePropertyBinding binding)
        {
            m_Binding = binding;
            m_WasIgnoringChanges = m_Binding.ignoreChanges;
            m_Binding.ignoreChanges = true;
        }

        public void Dispose()
        {
            m_Binding.ignoreChanges = m_WasIgnoringChanges;
        }
    }

    readonly record struct CallbackContext(StylePropertyBinding binding, VisualElement element, BindingId bindingId)
    {
        public readonly StylePropertyBinding binding = binding;
        public readonly VisualElement element = element;
        public readonly StyleDiff styleDiff = (StyleDiff)element.GetHierarchicalDataSourceContext().dataSource;
        public readonly BindingId bindingId = bindingId;
    }

    readonly record struct BindingInfoKey(VisualElement target, in BindingId id)
    {
        public readonly VisualElement target = target;
        public readonly BindingId id = id;
    }

    struct State
    {
        public bool IsInlined;
        public bool HasBinding;
        public bool HasVariable;

        public bool IsOverridden => IsInlined || HasBinding || HasVariable;
    }

    interface IProcessGenericChange<T>
    {
        void ProcessGenericChange(ref T value);
    }

    partial class GenericValueAtPath : PathVisitor
    {
        public StylePropertyBinding binding;
        public BindingId bindingId;
        public VisualElement element;
        public StyleDiff diff;

        public override void Reset()
        {
            base.Reset();
            binding = null;
            bindingId = default;
            element = null;
            diff = null;
        }

        protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
        {
            if (this is IProcessGenericChange<TValue> typedProcessor)
            {
                typedProcessor.ProcessGenericChange(ref value);
            }
        }

        private bool ShouldProcessChange()
        {
            if (binding.isReadonly)
            {
                binding.Update(bindingId, binding.stylePropertyId, diff, element);
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public new class UxmlSerializedData : CustomBinding.UxmlSerializedData
    {
        internal const string k_DataSourcePathTooltip = "The name of the style property that is targeted.";

        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
            {
                new (nameof(styleProperty), "style-property"),
            }, true);
        }

#pragma warning disable 649
        [SerializeField, HideInInspector, UxmlAttribute("style-property")]
        [Tooltip(k_DataSourcePathTooltip)]
        string styleProperty;
        [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags styleProperty_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance() =>  new StylePropertyBinding();

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);

            var e = (StylePropertyBinding) obj;
            if (ShouldWriteAttributeValue(styleProperty_UxmlAttributeFlags))
                e.styleProperty = styleProperty;
        }
    }

    static readonly UnityEngine.Pool.ObjectPool<GenericValueAtPath> s_VisitorPool = new (CreateVisitor, null, OnReleaseVisitor);

    static GenericValueAtPath CreateVisitor()
    {
        return new GenericValueAtPath();
    }

    static void OnReleaseVisitor(GenericValueAtPath visitor)
    {
        visitor.Reset();
    }

    private readonly Dictionary<BindingInfoKey, State> m_State = new ();
    private UpdateFlags m_UpdateFlags;
    private string m_StyleProperty;
    private StylePropertyId stylePropertyId;

    public event Action<ITrackablePropertyProvider, string, TrackedPropertyType> OnTrackedPropertyChanged;

    /// <summary>
    /// Object that serves as a local source for the binding, and is particularly useful when the data source is not
    /// part of the UI hierarchy, such as a static localization table. If this object is null, the binding resolves
    /// the data source using its normal resolution method.
    /// </summary>
    /// <remarks>
    /// Using a local source does not prevent children of the target from using the hierarchy source.
    /// </remarks>
    [CreateProperty]
    public string styleProperty
    {
        get => m_StyleProperty;
        private set
        {
            if (m_StyleProperty == value)
                return;
            m_StyleProperty = value;
            stylePropertyId = GetPropertyId(m_StyleProperty);
        }
    }

    private bool ignoreChanges
    {
        get => (m_UpdateFlags & UpdateFlags.IgnoreChanges) == UpdateFlags.IgnoreChanges;
        set
        {
            if (value)
                m_UpdateFlags |= UpdateFlags.IgnoreChanges;
            else
                m_UpdateFlags &= ~UpdateFlags.IgnoreChanges;
        }
    }

    public bool isReadonly
    {
        get => (m_UpdateFlags & UpdateFlags.Readonly) == UpdateFlags.Readonly;
        set
        {
            if (value)
                m_UpdateFlags |= UpdateFlags.Readonly;
            else
                m_UpdateFlags &= ~UpdateFlags.Readonly;
        }
    }

    private StylePropertyBinding()
        :this(null)
    {
    }

    public StylePropertyBinding(string styleProperty)
    {
        updateTrigger = BindingUpdateTrigger.OnSourceChanged;
        isReadonly = true;
        this.styleProperty = styleProperty;
    }

    protected internal override void OnActivated(in BindingActivationContext context)
    {
        base.OnActivated(in context);
        SendTrackPropertyEvent(this, context.targetElement, styleProperty, StylePropertyTrackingType.Register);
        RegisterCallbacks(this, context.bindingId, stylePropertyId, context.targetElement);
    }

    protected internal override void OnDeactivated(in BindingActivationContext context)
    {
        base.OnDeactivated(in context);
        OnTrackedPropertyChanged?.Invoke(this, styleProperty, TrackedPropertyType.StopTracking);
        UnregisterCallbacks(this, stylePropertyId, context.targetElement);
        m_State.Remove(new BindingInfoKey(context.targetElement, context.bindingId));
    }

    protected internal override BindingResult Update(in BindingContext context)
    {
        if (context.dataSource is not StyleDiff diff)
            return new BindingResult(BindingStatus.Failure, "Expected a StyleDiff as the data source");

        // StyleDiff has not been run yet.
        if (diff.currentContextType == StyleDiff.ContextType.None)
            return new BindingResult(BindingStatus.Pending);

        if (!IsStylePropertySupported(stylePropertyId))
            return new BindingResult(BindingStatus.Failure, "Expected a style property as the target. Shorthand and custom properties are not supported.");

        var targetElement = context.targetElement;
        return Update(context.bindingId, stylePropertyId, diff, targetElement);
    }

    static void ProcessChange<T>(T value, StyleDiff styleDiff, StylePropertyBinding binding, Action<StyleProperty, StyleSheet, T> setter)
    {
        switch (styleDiff.currentContextType)
        {
            case StyleDiff.ContextType.VisualElement:
            {
                var command = new SetInlineStylePropertyCommand<T>(styleDiff.currentTarget, binding.stylePropertyId, setter, value);
                command.Execute();
                break;
            }
            case StyleDiff.ContextType.StyleSheet:
            {
                var command = new SetStyleSheetPropertyCommand<T>(styleDiff.currentStyleSheet, styleDiff.currentRule, binding.stylePropertyId, setter, value);
                command.Execute();
                break;
            }
            case StyleDiff.ContextType.None:
            default:
                break;
        }
    }

    static void ProcessChange<T>(ChangeEvent<T> evt, CallbackContext ctx, Action<StyleProperty, StyleSheet, T> setter)
    {
        if (ctx.binding.ignoreChanges)
            return;

        if (ShouldProcessChange(evt, ctx))
            ProcessChange(evt.newValue, ctx.styleDiff, ctx.binding, setter);
    }


    BindingResult Update<TInline, TComputed>(in BindingId id, StylePropertyData<TInline, TComputed> value, StyleDiff diff, VisualElement targetElement)
    {
        var currentState = new State
        {
            IsInlined = value.uxmlValue.isInlined,
            HasBinding = value.binding != null,
            HasVariable = value.uxmlValue.requireVariableResolve
        };

        var isOverridden = currentState.IsOverridden;
        var key = new BindingInfoKey(targetElement, in id);
        if (m_State.TryGetValue(key, out var previousState))
        {
            m_State[key] = currentState;
            if (previousState.IsOverridden != currentState.IsOverridden)
                OnTrackedPropertyChanged?.Invoke(this, StylePropertyUtil.stylePropertyIdToPropertyName[value.id], isOverridden ? TrackedPropertyType.MarkOverride : TrackedPropertyType.ClearOverride);
        }
        // If state is not tracked yet and the value is not inlined, let's wait until it is to start tracking it.
        else if (isOverridden)
        {
            m_State[key] = currentState;
            OnTrackedPropertyChanged?.Invoke(this, StylePropertyUtil.stylePropertyIdToPropertyName[value.id], TrackedPropertyType.MarkOverride);
        }

        targetElement.EnableInClassList("style-property-field__inline-value", value.uxmlValue.isInlined);
        targetElement.EnableInClassList("style-property-field__variable", value.uxmlValue.requireVariableResolve);
        targetElement.EnableInClassList("style-property-field__bound", value.binding != null);

        var inlineValue = value.inlineValue;
        var computedValue = value.computedValue;

        var fieldAffordanceElement = (targetElement as IAffordanceField)?.affordanceElement;
        if (fieldAffordanceElement != null)
            FieldAffordanceController.UpdateFieldAffordanceData(fieldAffordanceElement.fieldAffordanceData, diff.currentTarget, diff.currentContextType, value);

        using var _ = new IgnoreChangeScope(this);
        switch (targetElement)
        {
            case BaseField<TComputed> field when id == BaseField<TComputed>.valueProperty:
                field.value = computedValue;
                break;
            case BaseField<TInline> inlineField when id == BaseField<TInline>.valueProperty:
            {
                if (TypeConversion.TryConvert<TComputed, TInline>(ref computedValue, out var convertedValue))
                    inlineField.value = convertedValue;
                else
                    Debug.LogWarning($"Invalid Cast from: `{typeof(TComputed).Name}` to `{typeof(TInline).Name}`");
                break;
            }
            default:
                PropertyContainer.SetValue(targetElement, id, computedValue);
                break;
        }

        return default;
    }

    private static void SendTrackPropertyEvent(ITrackablePropertyProvider provider, VisualElement target, string styleProperty, StylePropertyTrackingType type)
    {
        using var evt = TrackStylePropertyEvent.GetPooled(provider, styleProperty);
        evt.target = target;
        target.SendEvent(evt);
    }

    private static StylePropertyId GetPropertyId(string propertyName)
    {
        // i.e. backgroundColor => background-color
        if (StylePropertyUtil.cSharpNameToUssName.TryGetValue(propertyName, out var ussName))
            propertyName = ussName;

        return StylePropertyUtil.propertyNameToStylePropertyId.GetValueOrDefault(propertyName, StylePropertyId.Unknown);
    }

    private static bool IsStylePropertySupported(StylePropertyId stylePropertyId)
    {
        return stylePropertyId is not (StylePropertyId.All or StylePropertyId.Custom) && !StyleDebug.IsShorthandProperty(stylePropertyId);
    }

    private static string GetUnsupportedPropertyId(StylePropertyId stylePropertyId)
    {
        return $"[UI Toolkit] Unsupported style property: '{stylePropertyId}'";
    }

    static void RegisterEnumCallbacks<T>(StylePropertyBinding binding, in BindingId id, VisualElement targetElement)
        where T: struct, Enum, IConvertible
    {
        var isValueProperty = id == BaseField<T>.valueProperty;
        switch (targetElement)
        {
            case BaseField<T> when isValueProperty:
                targetElement.RegisterCallback<ChangeEvent<T>, CallbackContext>(ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
            case BaseField<StyleEnum<T>> when isValueProperty:
                targetElement.RegisterCallback<ChangeEvent<StyleEnum<T>>, CallbackContext>(ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
            case BaseField<Enum> when isValueProperty:
                targetElement.RegisterCallback<ChangeEvent<Enum>, CallbackContext>(ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
            default:
                targetElement.RegisterCallback<PropertyChangedEvent, CallbackContext>(binding.ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
        }
    }

    static void RegisterCallbacks<TStyleValue, TValue>(
        StylePropertyBinding binding,
        in BindingId id,
        VisualElement targetElement,
        EventCallback<ChangeEvent<TStyleValue>, CallbackContext> styleValueCallback,
        EventCallback<ChangeEvent<TValue>, CallbackContext> valueCallback)
    {
        switch (targetElement)
        {
            case INotifyValueChanged<TStyleValue> when id == BaseField<TStyleValue>.valueProperty:
                targetElement.RegisterCallback(styleValueCallback, new CallbackContext(binding, targetElement, id));
                break;
            case INotifyValueChanged<TValue> when id == BaseField<TValue>.valueProperty:
                targetElement.RegisterCallback(valueCallback, new CallbackContext(binding, targetElement, id));
                break;
            default:
                targetElement.RegisterCallback<PropertyChangedEvent, CallbackContext>(binding.ProcessChange, new CallbackContext(binding, targetElement, id));
                break;
        }
    }

    static void SetStyleValue<TStyleValue, TValue>(StyleProperty property, StyleSheet sheet, TStyleValue styleValue, Action<StyleProperty, StyleSheet, TValue> setterDelegate)
        where TStyleValue : IStyleValue<TValue>
    {
        switch (styleValue.keyword)
        {
            case StyleKeyword.Undefined:
                setterDelegate(property, sheet, styleValue.value);
                break;
            case StyleKeyword.Null:
                break;
            case StyleKeyword.Auto:
                property.SetKeyword(sheet, StyleValueKeyword.Auto);
                break;
            case StyleKeyword.None:
                property.SetKeyword(sheet, StyleValueKeyword.None);
                break;
            case StyleKeyword.Initial:
                property.SetKeyword(sheet, StyleValueKeyword.Initial);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static bool ShouldProcessChange<T>(ChangeEvent<T> evt, CallbackContext ctx)
    {
        if (evt.target != ctx.element)
            return false;

        if (!ctx.binding.isReadonly)
            return true;

        // Write back the previous value so that it looks like it's readonly.
        ((BaseField<T>)evt.target).SetValueWithoutNotify(evt.previousValue);
        evt.StopImmediatePropagation();
        return false;
    }
}
