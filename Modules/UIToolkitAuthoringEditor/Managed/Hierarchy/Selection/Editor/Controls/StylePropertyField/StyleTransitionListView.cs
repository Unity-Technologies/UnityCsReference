// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal partial class StyleTransitionListView : StyleLonghandListView<StyleTransitionListView.TransitionData>
{
    [Flags]
    public enum TransitionChangeType
    {
        None = 0,
        Property = 1 << 0,
        Duration = 1 << 1,
        TimingFunction = 1 << 2,
        Delay = 1 << 3,
        All = Property | Duration | TimingFunction | Delay
    }

    public class FoldoutTransitionField : FoldoutLonghandField<TransitionData>
    {
        internal new static readonly string ussClassName = "unity-foldout-transition-field";
        internal static readonly string propertyUssClassName = ussClassName + "__property-field";
        internal static readonly string durationUssClassName = ussClassName + "__duration-field";
        internal static readonly string timingFunctionUssClassName = ussClassName + "__timing-function-field";
        internal static readonly string delayUssClassName = ussClassName + "__delay-field";

        const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutTransitionField.uss";
        const string k_UssDarkSkinPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutTransitionFieldDark.uss";
        const string k_UssLightSkinPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutTransitionFieldLight.uss";

        CategoryDropdownField m_PropertyField;
        TimeValueField m_DurationField;
        EnumField m_TimingFunctionField;
        TimeValueField m_DelayField;

        public TransitionData transition => data;
        public CategoryDropdownField propertyField => m_PropertyField;
        public TimeValueField durationField => m_DurationField;
        public EnumField timingFunctionField => m_TimingFunctionField;
        public TimeValueField delayField => m_DelayField;

        public FieldAffordanceElement propertyAffordance => GetAffordance((int)TransitionChangeType.Property);
        public FieldAffordanceElement durationAffordance => GetAffordance((int)TransitionChangeType.Duration);
        public FieldAffordanceElement timingFunctionAffordance => GetAffordance((int)TransitionChangeType.TimingFunction);
        public FieldAffordanceElement delayAffordance => GetAffordance((int)TransitionChangeType.Delay);

        public TransitionChangeType type => (TransitionChangeType)mask;

        public FoldoutTransitionField(StyleTransitionListView listView)
            : base(listView, ussClassName, k_UssPath, k_UssDarkSkinPath, k_UssLightSkinPath)
        {
            m_PropertyField = new CategoryDropdownField("Property");
            m_PropertyField.categoryContent = AnimatableProperties;
            m_PropertyField.AddToClassList(propertyUssClassName);
            m_PropertyField.AddToClassList(CategoryDropdownField.alignedFieldUssClassName);
            m_PropertyField.RegisterCallback<ChangeEvent<string>, FoldoutTransitionField>(OnPropertyChanged, this);
            m_PropertyField.tooltip = "<b>USS property: transition-property</b>\nProperties to which a transition effect should be applied.";
            RegisterLonghand((int)TransitionChangeType.Property, m_PropertyField);

            m_DurationField = new TimeValueField("Duration");
            m_DurationField.AddToClassList(durationUssClassName);
            m_DurationField.AddToClassList(TimeValueField.alignedFieldUssClassName);
            m_DurationField.RegisterCallback<ChangeEvent<TimeValue>, FoldoutTransitionField>(OnDurationChanged, this);
            m_DurationField.tooltip = "<b>USS property: transition-duration</b>\nTime a transition animation should take to complete.";
            RegisterLonghand((int)TransitionChangeType.Duration, m_DurationField);

            m_TimingFunctionField = new EnumField("Easing", EasingMode.Ease);
            m_TimingFunctionField.AddToClassList(timingFunctionUssClassName);
            m_TimingFunctionField.AddToClassList(EnumField.alignedFieldUssClassName);
            m_TimingFunctionField.RegisterCallback<ChangeEvent<Enum>, FoldoutTransitionField>(OnTimingFunctionChanged, this);
            m_TimingFunctionField.tooltip = "<b>USS property: transition-timing-function</b>\nDetermines how intermediate values are calculated for properties modified by a transition effect.";
            RegisterLonghand((int)TransitionChangeType.TimingFunction, m_TimingFunctionField);

            m_DelayField = new TimeValueField("Delay");
            m_DelayField.AddToClassList(delayUssClassName);
            m_DelayField.AddToClassList(TimeValueField.alignedFieldUssClassName);
            m_DelayField.RegisterCallback<ChangeEvent<TimeValue>, FoldoutTransitionField>(OnDelayChanged, this);
            m_DelayField.tooltip = "<b>USS property: transition-delay</b>\nDuration to wait before starting a property's transition effect when its value changes.";
            RegisterLonghand((int)TransitionChangeType.Delay, m_DelayField);

            // Needed to support search by "Easing" rather than "Timing function"
            AddTrackedProperty("Easing");
        }

        static void OnPropertyChanged(ChangeEvent<string> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(new StylePropertyName(evt.newValue), field.data.duration, field.data.timingFunction, field.data.delay);
            field.OnChanged(evt, (int)TransitionChangeType.Property, transition);
        }

        static void OnDurationChanged(ChangeEvent<TimeValue> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(field.data.property, evt.newValue, field.data.timingFunction, field.data.delay);
            field.OnChanged(evt, (int)TransitionChangeType.Duration, transition);
        }

        static void OnTimingFunctionChanged(ChangeEvent<Enum> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(field.data.property, field.data.duration, (EasingMode)evt.newValue, field.data.delay);
            field.OnChanged(evt, (int)TransitionChangeType.TimingFunction, transition);
        }

        static void OnDelayChanged(ChangeEvent<TimeValue> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(field.data.property, field.data.duration, field.data.timingFunction, evt.newValue);
            field.OnChanged(evt, (int)TransitionChangeType.Delay, transition);
        }

        protected override void RefreshValues()
        {
            text = data.ToString(mask);
            m_PropertyField.SetValueWithoutNotify(data.property.ToString());
            m_DurationField.SetValueWithoutNotify(data.duration);
            m_TimingFunctionField.SetValueWithoutNotify(data.timingFunction.mode);
            m_DelayField.SetValueWithoutNotify(data.delay);
        }
    }

    internal readonly record struct TransitionData(
        StylePropertyName property,
        TimeValue duration,
        EasingFunction timingFunction,
        TimeValue delay
        )
    {
        public readonly StylePropertyName property = property;
        public readonly TimeValue duration = duration;
        public readonly EasingFunction timingFunction = timingFunction;
        public readonly TimeValue delay = delay;

        public string ToString(int overrides)
        {
            var propertyName = property.ToString() ?? IgnoredProperty;
            var p = (overrides & (int)TransitionChangeType.Property) != 0;
            var du = (overrides & (int)TransitionChangeType.Duration) != 0;
            var tf = (overrides & (int)TransitionChangeType.TimingFunction) != 0;
            var de = (overrides & (int)TransitionChangeType.Delay) != 0;
            return $"{Bold(propertyName, p)} {Bold(duration.ToString(), du)} {Bold(StyleSheetUtility.GetEnumExportString(timingFunction.mode), tf)} {Bold(delay.ToString(), de)}";
        }

        private string Bold(string input, bool bold)
        {
            return $"{(bold ? "<b>":"")}{input}{(bold ? "</b>":"")}";
        }
    }

    public const string IgnoredProperty = "ignored";
    public const string AllProperty = "all";
    public static readonly BindingId transitionPropertyProperty = nameof(transitionProperty);
    public static readonly BindingId transitionDurationProperty = nameof(transitionDuration);
    public static readonly BindingId transitionTimingFunctionProperty = nameof(transitionTimingFunction);
    public static readonly BindingId transitionDelayProperty = nameof(transitionDelay);

    internal static readonly string ussClassName = "unity-transition-list-view";
    internal static readonly string addTransitionButtonUssClassName = ussClassName + "__add-transition-button";
    internal static readonly string propertyOverriddenUssClassName = ussClassName + "__transition-property--overridden";
    internal static readonly string durationOverriddenUssClassName = ussClassName + "__transition-duration--overridden";
    internal static readonly string timingFunctionOverriddenUssClassName = ussClassName + "__transition-timing-function--overridden";
    internal static readonly string delayOverriddenUssClassName = ussClassName + "__transition-delay--overridden";

    [NoAutoStaticsCleanup] // precomputed dropdown content, safe to persist
    static readonly CategoryDropdownContent AnimatableProperties = GenerateTransitionPropertiesContent();

    const string k_TransitionListViewName = "transition-list-view";

    const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/TransitionsListView.uss";
    const string k_UssDarkSkinPath = "UIToolkitAuthoring/Inspector/Controls/TransitionsListViewDark.uss";
    const string k_UssLightSkinPath = "UIToolkitAuthoring/Inspector/Controls/TransitionsListViewLight.uss";

    readonly List<StylePropertyName> m_TransitionProperty = new();
    readonly List<TimeValue> m_TransitionDuration = new();
    readonly List<TimeValue> m_TransitionDelay = new();
    readonly List<EasingFunction> m_TransitionTimingFunction = new();

    LonghandDescriptor<TransitionData>[] m_Descriptors;

    protected override IReadOnlyList<LonghandDescriptor<TransitionData>> Descriptors => m_Descriptors ??= new[]
    {
        new LonghandDescriptor<TransitionData> { flag = (int)TransitionChangeType.Property, bindingId = transitionPropertyProperty, stylePropertyId = StylePropertyId.TransitionProperty, overriddenUssClassName = propertyOverriddenUssClassName, backingList = m_TransitionProperty, defaultValue = new StylePropertyName(IgnoredProperty), read = t => t.property },
        new LonghandDescriptor<TransitionData> { flag = (int)TransitionChangeType.Duration, bindingId = transitionDurationProperty, stylePropertyId = StylePropertyId.TransitionDuration, overriddenUssClassName = durationOverriddenUssClassName, backingList = m_TransitionDuration, defaultValue = TimeValue.Seconds(0), read = t => t.duration },
        new LonghandDescriptor<TransitionData> { flag = (int)TransitionChangeType.TimingFunction, bindingId = transitionTimingFunctionProperty, stylePropertyId = StylePropertyId.TransitionTimingFunction, overriddenUssClassName = timingFunctionOverriddenUssClassName, backingList = m_TransitionTimingFunction, defaultValue = new EasingFunction(EasingMode.Ease), read = t => t.timingFunction },
        new LonghandDescriptor<TransitionData> { flag = (int)TransitionChangeType.Delay, bindingId = transitionDelayProperty, stylePropertyId = StylePropertyId.TransitionDelay, overriddenUssClassName = delayOverriddenUssClassName, backingList = m_TransitionDelay, defaultValue = TimeValue.Seconds(0), read = t => t.delay },
    };

    [CreateProperty] public List<StylePropertyName> transitionProperty { get => m_TransitionProperty; set => SetBackingList(m_TransitionProperty, value); }
    [CreateProperty] public List<TimeValue> transitionDuration { get => m_TransitionDuration; set => SetBackingList(m_TransitionDuration, value); }
    [CreateProperty] public List<TimeValue> transitionDelay { get => m_TransitionDelay; set => SetBackingList(m_TransitionDelay, value); }
    [CreateProperty] public List<EasingFunction> transitionTimingFunction { get => m_TransitionTimingFunction; set => SetBackingList(m_TransitionTimingFunction, value); }

    internal TransitionChangeType overrides
    {
        get => (TransitionChangeType)OverridesMask;
        set => OverridesMask = (int)value;
    }

    public StyleTransitionListView()
        : base(ussClassName, k_TransitionListViewName, k_UssPath, k_UssDarkSkinPath, k_UssLightSkinPath)
    {
    }

    internal void OnTransitionAdded() => OnAdded();

    protected override FoldoutLonghandField<TransitionData> CreateRow() => new FoldoutTransitionField(this);

    protected override TransitionData MakeDefaultData()
        => new TransitionData(new StylePropertyName(AllProperty), TimeValue.Seconds(0), new EasingFunction(EasingMode.Ease), TimeValue.Seconds(0));

    protected override TransitionData ComposeData(int index)
    {
        var property = (m_TransitionProperty.Count > index && m_TransitionProperty[index].id != StylePropertyId.Unknown)
            ? m_TransitionProperty[index]
            : new StylePropertyName(IgnoredProperty);
        var duration = ValueAt(m_TransitionDuration, index, new TimeValue(0, TimeUnit.Millisecond));
        var timingFunction = ValueAt(m_TransitionTimingFunction, index, new EasingFunction(EasingMode.Ease));
        var delay = ValueAt(m_TransitionDelay, index, new TimeValue(0, TimeUnit.Millisecond));
        return new TransitionData(property, duration, timingFunction, delay);
    }

    static CategoryDropdownContent GenerateTransitionPropertiesContent()
    {
        var content = new CategoryDropdownContent();
        var animatableProperties = StylePropertyUtil.AllPropertyIds();
        foreach (var stylePropertyId in animatableProperties)
        {
            var stringNameHashSet = HashSetPool<string>.Get();
            try
            {
                var stylePropertyIdAsString = stylePropertyId.ToString();
                if (!StylePropertyUtil.IsAnimatable(stylePropertyId) || !stringNameHashSet.Add(stylePropertyIdAsString))
                    continue;

                if (!string.IsNullOrWhiteSpace(stylePropertyIdAsString))
                {
                    content.AppendValue(
                        new CategoryDropdownContent.ValueItem
                        {
                            value = StylePropertyUtil.stylePropertyIdToPropertyName[stylePropertyId],
                            displayName = ObjectNames.NicifyVariableName(stylePropertyIdAsString)
                        });
                }
            }
            finally
            {
                HashSetPool<string>.Release(stringNameHashSet);
            }
        }

        content.AppendSeparator();
        content.AppendValue(new CategoryDropdownContent.ValueItem { value = "all", displayName = "all" });
        content.AppendValue(new CategoryDropdownContent.ValueItem { value = "none", displayName = "none" });
        content.AppendValue(new CategoryDropdownContent.ValueItem { value = "initial", displayName = "initial" });
        content.AppendValue(new CategoryDropdownContent.ValueItem { value = "ignored", displayName = "ignored" });

        return content;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
