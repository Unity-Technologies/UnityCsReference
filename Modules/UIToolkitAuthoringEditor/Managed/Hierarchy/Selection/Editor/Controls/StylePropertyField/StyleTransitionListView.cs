// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Allows <see cref="StylePropertyBinding"/> to route per-property affordance elements on composite
/// fields that own multiple sub-properties (e.g. transition lists).
/// </summary>
internal interface IPropertyMappedAffordanceField
{
    void GetAffordanceElements(StylePropertyId propertyId, List<FieldAffordanceElement> elements);
}

[UxmlElement]
internal partial class StyleTransitionListView : VisualElement, IPropertyMappedAffordanceField
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

    private static bool IsSet(TransitionChangeType value, TransitionChangeType flag)
    {
        return (value & flag) == flag;
    }

    class TransitionChangedEvent : EventBase<TransitionChangedEvent>
    {
        public FoldoutTransitionField field;
        public TransitionData transition;
        public TransitionChangeType changeType;
        public int index;

        public TransitionChangedEvent()
        {
            bubbles = true;
        }
    }

    public class FoldoutTransitionField : OverrideFoldout
    {
        internal new static readonly string ussClassName = "unity-foldout-transition-field";
        internal static readonly string propertyUssClassName = ussClassName + "__property-field";
        internal static readonly string durationUssClassName = ussClassName + "__duration-field";
        internal static readonly string timingFunctionUssClassName = ussClassName + "__timing-function-field";
        internal static readonly string delayUssClassName = ussClassName + "__delay-field";

        const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutTransitionField.uss";
        const string k_UssDarkSkinPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutTransitionFieldDark.uss";
        const string k_UssLightSkinPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutTransitionFieldLight.uss";

        TransitionData m_Transition;

        CategoryDropdownField m_PropertyField;
        TimeValueField m_DurationField;
        EnumField m_TimingFunctionField;
        TimeValueField m_DelayField;

        FieldAffordanceElement m_PropertyAffordance;
        FieldAffordanceElement m_DurationAffordance;
        FieldAffordanceElement m_TimingFunctionAffordance;
        FieldAffordanceElement m_DelayAffordance;

        OverrideBarManipulator m_FoldoutOverride;
        OverrideBarManipulator m_PropertyOverride;
        OverrideBarManipulator m_DurationOverride;
        OverrideBarManipulator m_TimingFunctionOverride;
        OverrideBarManipulator m_DelayOverride;
        TransitionChangeType m_Type;

        public TransitionData transition => m_Transition;

        public TransitionChangeType type => m_Type;
        public CategoryDropdownField propertyField => m_PropertyField;
        public TimeValueField durationField => m_DurationField;
        public EnumField timingFunctionField => m_TimingFunctionField;
        public TimeValueField delayField => m_DelayField;
        public FieldAffordanceElement propertyAffordance => m_PropertyAffordance;
        public FieldAffordanceElement durationAffordance => m_DurationAffordance;
        public FieldAffordanceElement timingFunctionAffordance => m_TimingFunctionAffordance;
        public FieldAffordanceElement delayAffordance => m_DelayAffordance;

        public void SetTransitionData(TransitionData data, TransitionChangeType overrides)
        {
            m_Type = overrides;
            m_Transition = data;
            RefreshValues();
            RefreshOverrides();
        }

        public int index { get; set; }

        public FoldoutTransitionField(StyleTransitionListView listView)
        {
            styleSheets.Add(EditorGUIUtility.Load(k_UssPath) as StyleSheet);
            styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_UssDarkSkinPath : k_UssLightSkinPath) as StyleSheet);

            AddToClassList(ussClassName);
            m_FoldoutOverride = new OverrideBarManipulator { target = listView, OverrideContainer = overrideContainer };

            m_PropertyField = new CategoryDropdownField("Property");
            m_PropertyField.categoryContent = AnimatableProperties;
            m_PropertyField.AddToClassList(propertyUssClassName);
            m_PropertyField.AddToClassList(CategoryDropdownField.alignedFieldUssClassName);
            m_PropertyAffordance = new FieldAffordanceElement();
            m_PropertyField.hierarchy.Insert(0, m_PropertyAffordance);
            m_PropertyOverride = new OverrideBarManipulator { target = listView, OverrideContainer = m_PropertyField };
            m_PropertyField.RegisterCallback<ChangeEvent<string>, FoldoutTransitionField>(OnPropertyChanged, this);
            m_PropertyField.tooltip = "<b>USS property: transition-property</b>\nProperties to which a transition effect should be applied.";
            AddFieldContextMenu(m_PropertyField, m_PropertyAffordance);
            Add(m_PropertyField);

            m_DurationField = new TimeValueField("Duration");
            m_DurationField.AddToClassList(durationUssClassName);
            m_DurationField.AddToClassList(TimeValueField.alignedFieldUssClassName);
            m_DurationAffordance = new FieldAffordanceElement();
            m_DurationField.hierarchy.Insert(0, m_DurationAffordance);
            m_DurationOverride = new OverrideBarManipulator { target = listView, OverrideContainer = m_DurationField };
            m_DurationField.RegisterCallback<ChangeEvent<TimeValue>, FoldoutTransitionField>(OnDurationChanged, this);
            m_DurationField.tooltip = "<b>USS property: transition-duration</b>\nTime a transition animation should take to complete.";
            AddFieldContextMenu(m_DurationField, m_DurationAffordance);
            Add(m_DurationField);

            m_TimingFunctionField = new EnumField("Easing", EasingMode.Ease);
            m_TimingFunctionField.AddToClassList(timingFunctionUssClassName);
            m_TimingFunctionField.AddToClassList(EnumField.alignedFieldUssClassName);
            m_TimingFunctionAffordance = new FieldAffordanceElement();
            m_TimingFunctionField.hierarchy.Insert(0, m_TimingFunctionAffordance);
            m_TimingFunctionOverride = new OverrideBarManipulator { target = listView, OverrideContainer = m_TimingFunctionField };
            m_TimingFunctionField.RegisterCallback<ChangeEvent<Enum>, FoldoutTransitionField>(OnTimingFunctionChanged, this);
            m_TimingFunctionField.tooltip = "<b>USS property: transition-timing-function</b>\nDetermines how intermediate values are calculated for properties modified by a transition effect.";
            AddFieldContextMenu(m_TimingFunctionField, m_TimingFunctionAffordance);
            Add(m_TimingFunctionField);

            m_DelayField = new TimeValueField("Delay");
            m_DelayField.AddToClassList(delayUssClassName);
            m_DelayField.AddToClassList(TimeValueField.alignedFieldUssClassName);
            m_DelayAffordance = new FieldAffordanceElement();
            m_DelayField.hierarchy.Insert(0, m_DelayAffordance);
            m_DelayOverride = new OverrideBarManipulator { target = listView, OverrideContainer = m_DelayField };
            m_DelayField.RegisterCallback<ChangeEvent<TimeValue>, FoldoutTransitionField>(OnDelayChanged, this);
            m_DelayField.tooltip = "<b>USS property: transition-delay</b>\nDuration to wait before starting a property's transition effect when its value changes.";
            AddFieldContextMenu(m_DelayField, m_DelayAffordance);
            Add(m_DelayField);

            // Needed to support search by "Easing" rather than "Timing function"
            AddTrackedProperty("Easing");
        }

        static void AddFieldContextMenu(VisualElement field, FieldAffordanceElement affordance)
        {
            var manipulator = new ContextualMenuManipulator(evt => affordance.OnContextualMenuPopulate(evt));
            manipulator.acceptClicksIfDisabled = true;
            field.AddManipulator(manipulator);
        }

        static void OnChanged<T>(ChangeEvent<T> evt, FoldoutTransitionField field, TransitionChangeType changeType, TransitionData data)
        {
            using var nestedEvt = TransitionChangedEvent.GetPooled();
            nestedEvt.elementTarget = field;
            nestedEvt.field = field;
            nestedEvt.changeType = changeType;
            nestedEvt.index = field.index;
            nestedEvt.transition = data;
            field.SetTransitionData(data, field.type | changeType);
            field.SendEvent(nestedEvt);
            evt.StopPropagation();
        }

        static void OnPropertyChanged(ChangeEvent<string> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(new StylePropertyName(evt.newValue), field.transition.duration, field.transition.timingFunction, field.transition.delay);
            OnChanged(evt, field, TransitionChangeType.Property, transition);
        }

        static void OnDurationChanged(ChangeEvent<TimeValue> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(field.transition.property, evt.newValue, field.transition.timingFunction, field.transition.delay);
            OnChanged(evt, field, TransitionChangeType.Duration, transition);
        }

        static void OnTimingFunctionChanged(ChangeEvent<Enum> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(field.transition.property, field.transition.duration, (EasingMode)evt.newValue, field.transition.delay);
            OnChanged(evt, field, TransitionChangeType.TimingFunction, transition);
        }

        static void OnDelayChanged(ChangeEvent<TimeValue> evt, FoldoutTransitionField field)
        {
            var transition = new TransitionData(field.transition.property, field.transition.duration, field.transition.timingFunction, evt.newValue);
            OnChanged(evt, field, TransitionChangeType.Delay, transition);
        }

        void RefreshValues()
        {
            text = m_Transition.ToString(m_Type);
            m_PropertyField.SetValueWithoutNotify(transition.property.ToString());
            m_DurationField.SetValueWithoutNotify(transition.duration);
            m_TimingFunctionField.SetValueWithoutNotify(transition.timingFunction.mode);
            m_DelayField.SetValueWithoutNotify(transition.delay);
        }

        void RefreshOverrides()
        {
            m_FoldoutOverride.IsOverridden = m_Type != TransitionChangeType.None;
            m_PropertyOverride.IsOverridden = (m_Type & TransitionChangeType.Property) == TransitionChangeType.Property;
            m_DurationOverride.IsOverridden = (m_Type & TransitionChangeType.Duration) == TransitionChangeType.Duration;
            m_TimingFunctionOverride.IsOverridden = (m_Type & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction;
            m_DelayOverride.IsOverridden = (m_Type & TransitionChangeType.Delay) == TransitionChangeType.Delay;
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

        public string ToString(TransitionChangeType overrides)
        {
            var propertyName = property.ToString() ?? IgnoredProperty;
            var p = (overrides & TransitionChangeType.Property) == TransitionChangeType.Property;
            var du = (overrides & TransitionChangeType.Duration) == TransitionChangeType.Duration;
            var tf = (overrides & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction;
            var de = (overrides & TransitionChangeType.Delay) == TransitionChangeType.Delay;
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

    const string TransitionPropertyName = "transition-property";
    const string TransitionDurationName = "transition-duration";
    const string TransitionTimingFunctionName = "transition-timing-function";
    const string TransitionDelayName = "transition-delay";

    private static readonly CategoryDropdownContent AnimatableProperties = GenerateTransitionPropertiesContent();

    const string k_TransitionListViewName = "transition-list-view";

    const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/TransitionsListView.uss";
    const string k_UssDarkSkinPath = "UIToolkitAuthoring/Inspector/Controls/TransitionsListViewDark.uss";
    const string k_UssLightSkinPath = "UIToolkitAuthoring/Inspector/Controls/TransitionsListViewLight.uss";

    private readonly List<StylePropertyName> m_TransitionProperty = new();
    private readonly List<TimeValue> m_TransitionDuration = new();
    private readonly List<TimeValue> m_TransitionDelay = new();
    private readonly List<EasingFunction> m_TransitionTimingFunction = new();

    private readonly List<TransitionData> m_Data = new();

    private ListView m_ListView;

    [CreateProperty]
    public List<StylePropertyName> transitionProperty
    {
        get => m_TransitionProperty;
        set
        {
            if (AreEquivalent(m_TransitionProperty, value))
                return;

            m_TransitionProperty.Clear();
            m_TransitionProperty.AddRange(value);
            Refresh();
        }
    }

    [CreateProperty]
    public List<TimeValue> transitionDuration
    {
        get => m_TransitionDuration;
        set
        {
            if (AreEquivalent(m_TransitionDuration, value))
                return;

            m_TransitionDuration.Clear();
            m_TransitionDuration.AddRange(value);
            Refresh();
        }
    }

    [CreateProperty]
    public List<TimeValue> transitionDelay
    {
        get => m_TransitionDelay;
        set
        {
            if (AreEquivalent(m_TransitionDelay, value))
                return;

            m_TransitionDelay.Clear();
            m_TransitionDelay.AddRange(value);
            Refresh();
        }
    }

    [CreateProperty]
    public List<EasingFunction> transitionTimingFunction
    {
        get => m_TransitionTimingFunction;
        set
        {
            if (AreEquivalent(m_TransitionTimingFunction, value))
                return;

            m_TransitionTimingFunction.Clear();
            m_TransitionTimingFunction.AddRange(value);
            Refresh();
        }
    }

    public StyleTransitionListView()
    {
        AddToClassList(ussClassName);

        var styleSheet = EditorGUIUtility.Load(k_UssPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_UssDarkSkinPath : k_UssLightSkinPath) as StyleSheet);

        m_ListView = new ListView
        {
            name = k_TransitionListViewName,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            itemsSource = m_Data,
            makeItem = CreateTransitionFoldout,
            bindItem = BindTransitionFoldout,
            unbindItem = UnbindTransitionFoldout,
            allowAdd = true,
            allowRemove = true,
            showAddRemoveFooter = true,
            selectionType = SelectionType.Multiple
        };
        m_ListView.onAdd += OnTransitionAddedClicked;
        m_ListView.onRemove += OnTransitionsRemoved;
        Add(m_ListView);
        RegisterCallback<TrackPropertyEvent>(Callback);
    }

    TransitionChangeType m_Overrides;
    bool m_AffordanceUpdatePending;

    // Foldout affordances: populated/cleared by BindTransitionFoldout/UnbindTransitionFoldout.
    // StylePropertyBinding loops through these to call UpdateFieldAffordanceData and SetupContextMenu.
    readonly HashSet<FieldAffordanceElement> m_PropertyAffordances = new();
    readonly HashSet<FieldAffordanceElement> m_DurationAffordances = new();
    readonly HashSet<FieldAffordanceElement> m_TimingFunctionAffordances = new();
    readonly HashSet<FieldAffordanceElement> m_DelayAffordances = new();

    public void GetAffordanceElements(StylePropertyId propertyId, List<FieldAffordanceElement> elements)
    {
        var source = propertyId switch
        {
            StylePropertyId.TransitionProperty => m_PropertyAffordances,
            StylePropertyId.TransitionDuration => m_DurationAffordances,
            StylePropertyId.TransitionTimingFunction => m_TimingFunctionAffordances,
            StylePropertyId.TransitionDelay => m_DelayAffordances,
            _ => null
        };
        if (source != null)
            elements.AddRange(source);
    }

    internal TransitionChangeType overrides
    {
        get => m_Overrides;
        set
        {
            if (m_Overrides == value)
                return;
            m_Overrides = value;
            RefreshOverrideClasses();
        }
    }

    private void RefreshOverrideClasses()
    {
        EnableInClassList(propertyOverriddenUssClassName, (m_Overrides & TransitionChangeType.Property) == TransitionChangeType.Property);
        EnableInClassList(durationOverriddenUssClassName, (m_Overrides & TransitionChangeType.Duration) == TransitionChangeType.Duration);
        EnableInClassList(timingFunctionOverriddenUssClassName, (m_Overrides & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction);
        EnableInClassList(delayOverriddenUssClassName, (m_Overrides & TransitionChangeType.Delay) == TransitionChangeType.Delay);
    }

    private void Callback(TrackPropertyEvent evt)
    {
        Track(evt.provider);
    }

    private void Track(ITrackablePropertyProvider provider, string propertyName = null)
    {
        provider.OnTrackedPropertyChanged += PropertyChanged;
    }

    private void Untrack(ITrackablePropertyProvider provider, string propertyName = null)
    {
        RemoveFromTrackedProperties(provider, propertyName);
        provider.OnTrackedPropertyChanged -= PropertyChanged;
    }

    VisualElement CreateTransitionFoldout()
    {
        var field = new FoldoutTransitionField(this);
        field.RegisterCallback<TransitionChangedEvent>(OnTransitionChanged);
        return field;
    }

    void Set<T>(List<T> list, int index, T value, TransitionChangeType property, BindingId bindingId)
    {
        m_ListView.allowRemove = true;

        var isOverridden = IsSet(overrides, property);
        if (!isOverridden || index >= list.Count)
            Transfer(property);

        list[index] = value;
        NotifyPropertyChanged(bindingId);
    }

    void Transfer(TransitionChangeType toTransfer)
    {
        if (IsSet(toTransfer, TransitionChangeType.Property))
        {
            m_TransitionProperty.Clear();
            for(var i = 0; i < m_Data.Count; ++i)
                m_TransitionProperty.Add(m_Data[i].property);
        }

        if (IsSet(toTransfer, TransitionChangeType.Duration))
        {
            m_TransitionDuration.Clear();
            for(var i = 0; i < m_Data.Count; ++i)
                m_TransitionDuration.Add(m_Data[i].duration);
        }

        if (IsSet(toTransfer, TransitionChangeType.TimingFunction))
        {
            m_TransitionTimingFunction.Clear();
            for(var i = 0; i < m_Data.Count; ++i)
                m_TransitionTimingFunction.Add(m_Data[i].timingFunction);
        }

        if (IsSet(toTransfer, TransitionChangeType.Delay))
        {
            m_TransitionDelay.Clear();
            for(var i = 0; i < m_Data.Count; ++i)
                m_TransitionDelay.Add(m_Data[i].delay);
        }
    }

    void Notify(TransitionChangeType type)
    {
        if (IsSet(type, TransitionChangeType.Property))
            NotifyPropertyChanged(transitionPropertyProperty);

        if (IsSet(type, TransitionChangeType.Duration))
            NotifyPropertyChanged(transitionDurationProperty);

        if (IsSet(type, TransitionChangeType.TimingFunction))
            NotifyPropertyChanged(transitionTimingFunctionProperty);

        if (IsSet(type, TransitionChangeType.Delay))
            NotifyPropertyChanged(transitionDelayProperty);
    }

    void OnTransitionChanged(TransitionChangedEvent evt)
    {
        var index = evt.index;
        if ((evt.changeType | TransitionChangeType.Property) == TransitionChangeType.Property)
            Set(m_TransitionProperty, index, evt.transition.property, TransitionChangeType.Property, transitionPropertyProperty);

        if ((evt.changeType | TransitionChangeType.Duration) == TransitionChangeType.Duration)
            Set(m_TransitionDuration, index, evt.transition.duration, TransitionChangeType.Duration, transitionDurationProperty);

        if ((evt.changeType | TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction)
            Set(m_TransitionTimingFunction, index, evt.transition.timingFunction, TransitionChangeType.TimingFunction, transitionTimingFunctionProperty);

        if ((evt.changeType | TransitionChangeType.Delay) == TransitionChangeType.Delay)
            Set(m_TransitionDelay, index, evt.transition.delay, TransitionChangeType.Delay, transitionDelayProperty);

        Refresh();
    }

    void OnTransitionAddedClicked(BaseListView listView)
    {
        OnTransitionAdded();
    }

    internal void OnTransitionAdded()
    {
        m_ListView.allowRemove = true;

        var changeType = overrides;
        if (changeType == TransitionChangeType.None)
            changeType = TransitionChangeType.All;

        if ((changeType & TransitionChangeType.Property) == TransitionChangeType.Property)
        {
            transitionProperty.Add(IgnoredProperty);
            NotifyPropertyChanged(transitionPropertyProperty);
        }

        if ((changeType & TransitionChangeType.Duration) == TransitionChangeType.Duration)
        {
            transitionDuration.Add(TimeValue.Seconds(0));
            NotifyPropertyChanged(transitionDurationProperty);
        }

        if ((changeType & TransitionChangeType.TimingFunction) == TransitionChangeType.TimingFunction)
        {
            transitionTimingFunction.Add(EasingMode.Ease);
            NotifyPropertyChanged(transitionTimingFunctionProperty);
        }

        if ((changeType & TransitionChangeType.Delay) == TransitionChangeType.Delay)
        {
            transitionDelay.Add(TimeValue.Seconds(0));
            NotifyPropertyChanged(transitionDelayProperty);
        }

        Refresh();

        // The usual GeometryChangedEvent doesn't seem to work at all here.
        schedule.Execute(ScrollToLastItem).StartingIn(20);
    }

    void ScrollToLastItem()
    {
        var sv = GetFirstAncestorOfType<ScrollView>();
        sv?.ScrollTo(m_ListView.Q<Button>(BaseListView.footerAddButtonName));
    }

    void OnTransitionsRemoved(BaseListView listView)
    {
        using var _ = ListPool<int>.Get(out var selection);
        selection.AddRange(listView.selectedIds);
        selection.Sort();

        // If nothing was selected, remove the last item.
        if (selection.Count == 0)
            selection.Add(m_Data.Count - 1);

        for (var i = selection.Count - 1; i >= 0; --i)
        {
            var selectedId = selection[i];
            if (selectedId >= 0 && selectedId < m_Data.Count)
                m_Data.RemoveAt(selection[i]);
        }

        listView.ClearSelection();

        var changeType = overrides != TransitionChangeType.None ? overrides : TransitionChangeType.All;

        // Never fully remove all transitions - fall back to a single "all" transition
        if (m_Data.Count == 0)
        {
            m_Data.Add(new TransitionData(new StylePropertyName(AllProperty), TimeValue.Seconds(0), new EasingFunction(EasingMode.Ease), TimeValue.Seconds(0)));
            m_ListView.allowRemove = false;
            changeType = TransitionChangeType.All;
        }

        Transfer(changeType);
        Notify(changeType);
        Refresh();
    }

    void BindTransitionFoldout(VisualElement element, int index)
    {
        if (element is FoldoutTransitionField field)
        {
            field.index = index;
            field.SetTransitionData(m_Data[index], overrides);
            field.EnableInClassList("last-item", index == m_Data.Count - 1);

            m_PropertyAffordances.Add(field.propertyAffordance);
            m_DurationAffordances.Add(field.durationAffordance);
            m_TimingFunctionAffordances.Add(field.timingFunctionAffordance);
            m_DelayAffordances.Add(field.delayAffordance);

            ScheduleAffordanceBindingUpdate();
        }
    }

    // StylePropertyBinding.Update runs before BindTransitionFoldout populates the affordance sets,
    // so the affordance context menus are never set up.
    // Schedule a deferred MarkDirty so the bindings re-run with populated affordances.
    void ScheduleAffordanceBindingUpdate()
    {
        if (m_AffordanceUpdatePending)
            return;

        m_AffordanceUpdatePending = true;
        schedule.Execute(MarkAffordanceBindingsDirty);
    }

    void MarkAffordanceBindingsDirty()
    {
        m_AffordanceUpdatePending = false;
        GetBinding(transitionPropertyProperty)?.MarkDirty();
        GetBinding(transitionDurationProperty)?.MarkDirty();
        GetBinding(transitionTimingFunctionProperty)?.MarkDirty();
        GetBinding(transitionDelayProperty)?.MarkDirty();
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
                            value =  StylePropertyUtil.stylePropertyIdToPropertyName[stylePropertyId],
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

    void UnbindTransitionFoldout(VisualElement element, int index)
    {
        if (element is FoldoutTransitionField field)
        {
            field.index = -1;
            field.SetTransitionData(default, TransitionChangeType.None);

            m_PropertyAffordances.Remove(field.propertyAffordance);
            m_DurationAffordances.Remove(field.durationAffordance);
            m_TimingFunctionAffordances.Remove(field.timingFunctionAffordance);
            m_DelayAffordances.Remove(field.delayAffordance);

            m_AffordanceUpdatePending = false;
        }
    }

    private void Refresh()
    {
        m_Data.Clear();

        var count = GetMaxCount();
        for (var i = 0; i < count; ++i)
        {
            var property = m_TransitionProperty?.Count > i && m_TransitionProperty[i].id != StylePropertyId.Unknown
                ? m_TransitionProperty[i] : new StylePropertyName(IgnoredProperty);

            TimeValue duration;
            if (null == m_TransitionDuration || m_TransitionDuration.Count == 0)
                duration = new TimeValue(0, TimeUnit.Millisecond);
            else if (m_TransitionDuration.Count > i)
                duration = m_TransitionDuration[i];
            else
                duration = m_TransitionDuration[i%m_TransitionDuration.Count];

            EasingFunction timingFunction;
            if (null == m_TransitionTimingFunction || m_TransitionTimingFunction.Count == 0)
                timingFunction = new EasingFunction(EasingMode.Ease);
            else if (m_TransitionTimingFunction.Count > i)
                timingFunction = m_TransitionTimingFunction[i];
            else
                timingFunction = m_TransitionTimingFunction[i%m_TransitionTimingFunction.Count];

            TimeValue delay;
            if (null == m_TransitionDelay || m_TransitionDelay.Count == 0)
                delay = new TimeValue(0, TimeUnit.Millisecond);
            else if (m_TransitionDelay.Count > i)
                delay = m_TransitionDelay[i];
            else
                delay = m_TransitionDelay[i%m_TransitionDelay.Count];

            var transition = new TransitionData(property, duration, timingFunction, delay);
            m_Data.Add(transition);
        }
        m_ListView.RefreshItems();
    }

    private int GetMaxCount()
    {
        var count = 0;
        count = Math.Max(count, m_TransitionProperty.Count);
        count = Math.Max(count, m_TransitionDuration.Count);
        count = Math.Max(count, m_TransitionDelay.Count);
        count = Math.Max(count, m_TransitionTimingFunction.Count);
        return count;
    }

    private static bool AreEquivalent<T>(List<T> lhs, List<T> rhs)
    {
        if (lhs == null)
            return rhs == null;
        if (rhs == null)
            return false;
        if (lhs.Count != rhs.Count)
            return false;

        for (var i = 0; i < lhs.Count; ++i)
        {
            if (!EqualityComparer<T>.Default.Equals(lhs[i], rhs[i]))
                return false;
        }
        return true;
    }

    Dictionary<string, HashSet<ITrackablePropertyProvider>> m_TrackedProviders = new();

    void PropertyChanged(ITrackablePropertyProvider provider, string propertyName, TrackedPropertyType type)
    {
        switch (type)
        {
            case TrackedPropertyType.StopTracking:
                Untrack(provider, propertyName);
                break;
            case TrackedPropertyType.MarkOverride:
                AddToTrackedProperties(provider, propertyName);
                break;
            case TrackedPropertyType.ClearOverride:
                RemoveFromTrackedProperties(provider, propertyName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    void AddToTrackedProperties(ITrackablePropertyProvider provider, string propertyName)
    {
        if (!m_TrackedProviders.TryGetValue(propertyName, out var providerSet))
            m_TrackedProviders[propertyName] = providerSet = new HashSet<ITrackablePropertyProvider>();

        providerSet.Add(provider);
        if (!StylePropertyUtil.cSharpNameToUssName.TryGetValue(propertyName, out var ussName))
            ussName = propertyName;
        switch (ussName)
        {
            case TransitionPropertyName:
                overrides |= TransitionChangeType.Property;
                break;
            case TransitionDurationName:
                overrides |= TransitionChangeType.Duration;
                break;
            case TransitionTimingFunctionName:
                overrides |= TransitionChangeType.TimingFunction;
                break;
            case TransitionDelayName:
                overrides |= TransitionChangeType.Delay;
                break;
        }
        UpdateOverriddenState();
    }

    void RemoveFromTrackedProperties(ITrackablePropertyProvider provider, string propertyName)
    {
        if (!m_TrackedProviders.TryGetValue(propertyName, out var providerSet))
            return;

        providerSet.Remove(provider);

        if (providerSet.Count == 0)
        {
            m_TrackedProviders.Remove(propertyName);
            if (!StylePropertyUtil.cSharpNameToUssName.TryGetValue(propertyName, out var ussName))
                ussName = propertyName;
            switch (ussName)
            {
                case TransitionPropertyName:
                    overrides &= ~TransitionChangeType.Property;
                    break;
                case TransitionDurationName:
                    overrides &= ~TransitionChangeType.Duration;
                    break;
                case TransitionTimingFunctionName:
                    overrides &= ~TransitionChangeType.TimingFunction;
                    break;
                case TransitionDelayName:
                    overrides &= ~TransitionChangeType.Delay;
                    break;
            }
        }

        UpdateOverriddenState();
    }

    void UpdateOverriddenState()
    {
        m_ListView.RefreshItems();
    }
}
