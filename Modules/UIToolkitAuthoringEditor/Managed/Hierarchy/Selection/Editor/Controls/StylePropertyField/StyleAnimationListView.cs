// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
[UxmlElement]
internal partial class StyleAnimationListView : StyleLonghandListView<StyleAnimationListView.AnimationData>
{
    [Flags]
    public enum AnimationChangeType
    {
        None = 0,
        Clip = 1 << 0,
        Duration = 1 << 1,
        Delay = 1 << 2,
        IterationCount = 1 << 3,
        Direction = 1 << 4,
        PlayState = 1 << 5,
        All = Clip | Duration | Delay | IterationCount | Direction | PlayState
    }

    public class FoldoutAnimationField : FoldoutLonghandField<AnimationData>
    {
        internal new static readonly string ussClassName = "unity-foldout-animation-field";
        internal static readonly string clipUssClassName = ussClassName + "__clip-field";
        internal static readonly string newClipButtonUssClassName = ussClassName + "__new-clip-button";
        internal static readonly string durationUssClassName = ussClassName + "__duration-field";
        internal static readonly string delayUssClassName = ussClassName + "__delay-field";
        internal static readonly string iterationCountUssClassName = ussClassName + "__iteration-count-field";
        internal static readonly string directionUssClassName = ussClassName + "__direction-field";
        internal static readonly string playStateUssClassName = ussClassName + "__play-state-field";

        const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutAnimationField.uss";
        const string k_UssDarkSkinPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutAnimationFieldDark.uss";
        const string k_UssLightSkinPath = "UIToolkitAuthoring/Inspector/Controls/FoldoutAnimationFieldLight.uss";

        readonly StyleAnimationListView m_AnimationListView;

        UIAnimationClipField m_ClipField;
        Button m_NewClipButton;
        FloatField m_DurationField;
        FloatField m_DelayField;
        AnimationIterationCountField m_IterationCountField;
        EnumField m_DirectionField;
        ToggleButtonGroup m_PlayStateField;

        public AnimationData animation => data;
        public UIAnimationClipField clipField => m_ClipField;
        public FloatField durationField => m_DurationField;
        public FloatField delayField => m_DelayField;
        public TextValueField<AnimationIterationCount> iterationCountField => m_IterationCountField;
        public EnumField directionField => m_DirectionField;
        public ToggleButtonGroup playStateField => m_PlayStateField;

        public FoldoutAnimationField(StyleAnimationListView listView)
            : base(listView, ussClassName, k_UssPath, k_UssDarkSkinPath, k_UssLightSkinPath)
        {
            m_AnimationListView = listView;

            m_ClipField = new UIAnimationClipField("Clip");
            m_ClipField.AddToClassList(clipUssClassName);
            m_ClipField.AddToClassList(UIAnimationClipField.alignedFieldUssClassName);
            m_ClipField.RegisterCallback<ChangeEvent<UIAnimationClip>, FoldoutAnimationField>(OnClipChanged, this);
            m_ClipField.tooltip = "<b>USS property: animation-name</b>\nAnimation clip to apply with the current element as root. Animation paths are relative to this element.";
            RegisterLonghand((int)AnimationChangeType.Clip, m_ClipField);

            // Text and tooltip are owned by the controller, which swaps them with the clip state.
            m_NewClipButton = new Button { name = AnimationClipNewButtonController.AnimationClipNewButtonName };
            m_NewClipButton.AddToClassList(newClipButtonUssClassName);
            AnimationClipNewButtonController.ConnectRowButton(m_NewClipButton, m_ClipField,
                () => m_AnimationListView.canEditClipsInAnimationWindow);
            m_ClipField.Add(m_NewClipButton);

            m_DurationField = new NonNegativeFloatField("Duration");
            m_DurationField.AddToClassList(durationUssClassName);
            m_DurationField.AddToClassList(FloatField.alignedFieldUssClassName);
            m_DurationField.RegisterCallback<ChangeEvent<float>, FoldoutAnimationField>(OnDurationChanged, this);
            m_DurationField.tooltip = "<b>USS property: animation-duration</b>\nDuration of one iteration of the animation, in seconds. The default of 0 uses the clip's intrinsic length.";
            RegisterLonghand((int)AnimationChangeType.Duration, m_DurationField);

            m_DelayField = new FloatField("Delay");
            m_DelayField.AddToClassList(delayUssClassName);
            m_DelayField.AddToClassList(FloatField.alignedFieldUssClassName);
            m_DelayField.RegisterCallback<ChangeEvent<float>, FoldoutAnimationField>(OnDelayChanged, this);
            m_DelayField.tooltip = "<b>USS property: animation-delay</b>\nDelay before the animation begins, in seconds. Negative values start the animation partway through its first iteration.";
            RegisterLonghand((int)AnimationChangeType.Delay, m_DelayField);

            m_IterationCountField = new AnimationIterationCountField("Iteration Count");
            m_IterationCountField.AddToClassList(iterationCountUssClassName);
            m_IterationCountField.AddToClassList(AnimationIterationCountField.alignedFieldUssClassName);
            m_IterationCountField.RegisterCallback<ChangeEvent<AnimationIterationCount>, FoldoutAnimationField>(OnIterationCountChanged, this);
            m_IterationCountField.tooltip = "<b>USS property: animation-iteration-count</b>\nNumber of times the animation repeats. Use 'infinite' to loop forever.";
            RegisterLonghand((int)AnimationChangeType.IterationCount, m_IterationCountField);

            m_DirectionField = new EnumField("Direction", AnimationDirection.Normal);
            m_DirectionField.AddToClassList(directionUssClassName);
            m_DirectionField.AddToClassList(EnumField.alignedFieldUssClassName);
            m_DirectionField.RegisterCallback<ChangeEvent<Enum>, FoldoutAnimationField>(OnDirectionChanged, this);
            m_DirectionField.tooltip = "<b>USS property: animation-direction</b>\nWhether the animation plays forward, backward, or alternates direction across iterations.";
            RegisterLonghand((int)AnimationChangeType.Direction, m_DirectionField);

            // Two-icon toggle: index 0 = Running, index 1 = Paused. Single-select. The "running"/"paused"
            // icons are supplied by the shared FoldoutAnimationField skin sheets.
            m_PlayStateField = new ToggleButtonGroup("Play State");
            m_PlayStateField.AddToClassList(playStateUssClassName);
            m_PlayStateField.AddToClassList(ToggleButtonGroup.alignedFieldUssClassName);
            m_PlayStateField.Add(new Button { name = "running", tooltip = "The animation is currently playing." });
            m_PlayStateField.Add(new Button { name = "paused", tooltip = "The animation is currently paused." });
            m_PlayStateField.RegisterCallback<ChangeEvent<ToggleButtonGroupState>, FoldoutAnimationField>(OnPlayStateChanged, this);
            m_PlayStateField.tooltip = "<b>USS property: animation-play-state</b>\nControls whether the animation is running or paused.";
            RegisterLonghand((int)AnimationChangeType.PlayState, m_PlayStateField);

            // Support search by the field labels rather than the raw USS property names.
            AddTrackedProperty("Clip");
            AddTrackedProperty("Iteration Count");
            AddTrackedProperty("Direction");
            AddTrackedProperty("Play State");
        }

        static void OnClipChanged(ChangeEvent<UIAnimationClip> evt, FoldoutAnimationField field)
        {
            var animation = new AnimationData(evt.newValue, field.data.duration, field.data.delay, field.data.iterationCount, field.data.direction, field.data.playState);
            field.OnChanged(evt, (int)AnimationChangeType.Clip, animation);
        }

        static void OnDurationChanged(ChangeEvent<float> evt, FoldoutAnimationField field)
        {
            var animation = new AnimationData(field.data.clip, evt.newValue, field.data.delay, field.data.iterationCount, field.data.direction, field.data.playState);
            field.OnChanged(evt, (int)AnimationChangeType.Duration, animation);
        }

        static void OnDelayChanged(ChangeEvent<float> evt, FoldoutAnimationField field)
        {
            var animation = new AnimationData(field.data.clip, field.data.duration, evt.newValue, field.data.iterationCount, field.data.direction, field.data.playState);
            field.OnChanged(evt, (int)AnimationChangeType.Delay, animation);
        }

        static void OnIterationCountChanged(ChangeEvent<AnimationIterationCount> evt, FoldoutAnimationField field)
        {
            var animation = new AnimationData(field.data.clip, field.data.duration, field.data.delay, evt.newValue, field.data.direction, field.data.playState);
            field.OnChanged(evt, (int)AnimationChangeType.IterationCount, animation);
        }

        static void OnDirectionChanged(ChangeEvent<Enum> evt, FoldoutAnimationField field)
        {
            var animation = new AnimationData(field.data.clip, field.data.duration, field.data.delay, field.data.iterationCount, (AnimationDirection)evt.newValue, field.data.playState);
            field.OnChanged(evt, (int)AnimationChangeType.Direction, animation);
        }

        static void OnPlayStateChanged(ChangeEvent<ToggleButtonGroupState> evt, FoldoutAnimationField field)
        {
            var animation = new AnimationData(field.data.clip, field.data.duration, field.data.delay, field.data.iterationCount, field.data.direction, StateToPlayState(evt.newValue));
            field.OnChanged(evt, (int)AnimationChangeType.PlayState, animation);
        }

        // The play-state toggle group maps its selected button to AnimationPlayState: index 0 = Running,
        // index 1 = Paused (matching the enum's underlying values).
        static AnimationPlayState StateToPlayState(ToggleButtonGroupState state)
        {
            Span<int> active = stackalloc int[state.length];
            var selected = state.GetActiveOptions(active);
            return selected.Length > 0 && selected[0] == (int)AnimationPlayState.Paused
                ? AnimationPlayState.Paused
                : AnimationPlayState.Running;
        }

        static ToggleButtonGroupState PlayStateToState(AnimationPlayState playState)
        {
            var state = new ToggleButtonGroupState(0, 2);
            state[(int)playState] = true;
            return state;
        }

        protected override void RefreshValues()
        {
            text = data.ToString(mask);
            m_ClipField.SetValueWithoutNotify(data.clip);
            AnimationClipNewButtonController.UpdateButtonMode(m_NewClipButton,
                m_AnimationListView.canEditClipsInAnimationWindow && data.clip != null);
            m_DurationField.SetValueWithoutNotify(data.duration);
            m_IterationCountField.SetValueWithoutNotify(data.iterationCount);
            m_DelayField.SetValueWithoutNotify(data.delay);
            m_DirectionField.SetValueWithoutNotify(data.direction);
            m_PlayStateField.SetValueWithoutNotify(PlayStateToState(data.playState));
        }

        // A FloatField clamped to >= 0. animation-duration treats a negative value the same as 0 (the runtime
        // uses the clip's intrinsic length), so negative input is meaningless. The value-setter clamp also
        // catches label-drag, which assigns through value. (animation-delay stays a plain FloatField — negative
        // delays are valid and start the animation partway through.)
        sealed class NonNegativeFloatField : FloatField
        {
            public NonNegativeFloatField(string label) : base(label) {}

            public override float value
            {
                get => base.value;
                set => base.value = value < 0f ? 0f : value;
            }

            protected override float StringToValue(string str)
            {
                var v = base.StringToValue(str);
                return v < 0f ? 0f : v;
            }
        }

    }

    internal readonly record struct AnimationData(
        UIAnimationClip clip,
        float duration,
        float delay,
        AnimationIterationCount iterationCount,
        AnimationDirection direction,
        AnimationPlayState playState
        )
    {
        public readonly UIAnimationClip clip = clip;
        public readonly float duration = duration;
        public readonly float delay = delay;
        public readonly AnimationIterationCount iterationCount = iterationCount;
        public readonly AnimationDirection direction = direction;
        public readonly AnimationPlayState playState = playState;

        public string ToString(int overrides)
        {
            var clipName = clip != null ? clip.name : "none";
            var c = (overrides & (int)AnimationChangeType.Clip) != 0;
            var du = (overrides & (int)AnimationChangeType.Duration) != 0;
            var de = (overrides & (int)AnimationChangeType.Delay) != 0;
            var ic = (overrides & (int)AnimationChangeType.IterationCount) != 0;
            var di = (overrides & (int)AnimationChangeType.Direction) != 0;
            var ps = (overrides & (int)AnimationChangeType.PlayState) != 0;
            var iterationText = iterationCount.ToString();
            return $"{Bold(clipName, c)} {Bold($"{duration}s", du)} {Bold($"{delay}s", de)} {Bold(iterationText, ic)} {Bold(StyleSheetUtility.GetEnumExportString(direction), di)} {Bold(StyleSheetUtility.GetEnumExportString(playState), ps)}";
        }

        private string Bold(string input, bool bold)
        {
            return $"{(bold ? "<b>":"")}{input}{(bold ? "</b>":"")}";
        }
    }

    public static readonly BindingId animationNamesProperty = nameof(animationNames);
    public static readonly BindingId animationDurationProperty = nameof(animationDuration);
    public static readonly BindingId animationDelayProperty = nameof(animationDelay);
    public static readonly BindingId animationIterationCountProperty = nameof(animationIterationCount);
    public static readonly BindingId animationDirectionProperty = nameof(animationDirection);
    public static readonly BindingId animationPlayStatesProperty = nameof(animationPlayStates);

    internal static readonly string ussClassName = "unity-animation-list-view";
    internal static readonly string addAnimationButtonUssClassName = ussClassName + "__add-animation-button";
    internal static readonly string clipOverriddenUssClassName = ussClassName + "__animation-clip--overridden";
    internal static readonly string durationOverriddenUssClassName = ussClassName + "__animation-duration--overridden";
    internal static readonly string delayOverriddenUssClassName = ussClassName + "__animation-delay--overridden";
    internal static readonly string iterationCountOverriddenUssClassName = ussClassName + "__animation-iteration-count--overridden";
    internal static readonly string directionOverriddenUssClassName = ussClassName + "__animation-direction--overridden";
    internal static readonly string playStateOverriddenUssClassName = ussClassName + "__animation-play-state--overridden";

    // Defaults for a newly-added animation, matching the per-property USS initial values.
    const float k_DefaultDuration = 0f;
    const float k_DefaultDelay = 0f;
    static readonly AnimationIterationCount k_DefaultIterationCount = new AnimationIterationCount(1f);
    const AnimationDirection k_DefaultDirection = AnimationDirection.Normal;
    const AnimationPlayState k_DefaultPlayState = AnimationPlayState.Running;

    const string k_AnimationListViewName = "animation-list-view";

    const string k_UssPath = "UIToolkitAuthoring/Inspector/Controls/AnimationsListView.uss";
    const string k_UssDarkSkinPath = "UIToolkitAuthoring/Inspector/Controls/AnimationsListViewDark.uss";
    const string k_UssLightSkinPath = "UIToolkitAuthoring/Inspector/Controls/AnimationsListViewLight.uss";

    readonly List<UIAnimationClip> m_AnimationClip = new();
    readonly List<float> m_AnimationDuration = new();
    readonly List<float> m_AnimationDelay = new();
    readonly List<AnimationIterationCount> m_AnimationIterationCount = new();
    readonly List<AnimationDirection> m_AnimationDirection = new();
    readonly List<AnimationPlayState> m_AnimationPlayState = new();

    LonghandDescriptor<AnimationData>[] m_Descriptors;
    bool m_CanEditClipsInAnimationWindow = true;

    protected override IReadOnlyList<LonghandDescriptor<AnimationData>> Descriptors => m_Descriptors ??= new[]
    {
        new LonghandDescriptor<AnimationData> { flag = (int)AnimationChangeType.Clip, bindingId = animationNamesProperty, stylePropertyId = StylePropertyId.AnimationNames, overriddenUssClassName = clipOverriddenUssClassName, backingList = m_AnimationClip, defaultValue = null, read = a => a.clip },
        new LonghandDescriptor<AnimationData> { flag = (int)AnimationChangeType.Duration, bindingId = animationDurationProperty, stylePropertyId = StylePropertyId.AnimationDuration, overriddenUssClassName = durationOverriddenUssClassName, backingList = m_AnimationDuration, defaultValue = k_DefaultDuration, read = a => a.duration },
        new LonghandDescriptor<AnimationData> { flag = (int)AnimationChangeType.Delay, bindingId = animationDelayProperty, stylePropertyId = StylePropertyId.AnimationDelay, overriddenUssClassName = delayOverriddenUssClassName, backingList = m_AnimationDelay, defaultValue = k_DefaultDelay, read = a => a.delay },
        new LonghandDescriptor<AnimationData> { flag = (int)AnimationChangeType.IterationCount, bindingId = animationIterationCountProperty, stylePropertyId = StylePropertyId.AnimationIterationCount, overriddenUssClassName = iterationCountOverriddenUssClassName, backingList = m_AnimationIterationCount, defaultValue = k_DefaultIterationCount, read = a => a.iterationCount },
        new LonghandDescriptor<AnimationData> { flag = (int)AnimationChangeType.Direction, bindingId = animationDirectionProperty, stylePropertyId = StylePropertyId.AnimationDirection, overriddenUssClassName = directionOverriddenUssClassName, backingList = m_AnimationDirection, defaultValue = k_DefaultDirection, read = a => a.direction },
        new LonghandDescriptor<AnimationData> { flag = (int)AnimationChangeType.PlayState, bindingId = animationPlayStatesProperty, stylePropertyId = StylePropertyId.AnimationPlayStates, overriddenUssClassName = playStateOverriddenUssClassName, backingList = m_AnimationPlayState, defaultValue = k_DefaultPlayState, read = a => a.playState },
    };

    [CreateProperty] public List<UIAnimationClip> animationNames { get => m_AnimationClip; set => SetBackingList(m_AnimationClip, value); }
    [CreateProperty] public List<float> animationDuration { get => m_AnimationDuration; set => SetBackingList(m_AnimationDuration, value); }
    [CreateProperty] public List<float> animationDelay { get => m_AnimationDelay; set => SetBackingList(m_AnimationDelay, value); }
    [CreateProperty] public List<AnimationIterationCount> animationIterationCount { get => m_AnimationIterationCount; set => SetBackingList(m_AnimationIterationCount, value); }
    [CreateProperty] public List<AnimationDirection> animationDirection { get => m_AnimationDirection; set => SetBackingList(m_AnimationDirection, value); }
    [CreateProperty] public List<AnimationPlayState> animationPlayStates { get => m_AnimationPlayState; set => SetBackingList(m_AnimationPlayState, value); }

    public AnimationChangeType overrides
    {
        get => (AnimationChangeType)OverridesMask;
        set => OverridesMask = (int)value;
    }

    public StyleAnimationListView()
        : base(ussClassName, k_AnimationListViewName, k_UssPath, k_UssDarkSkinPath, k_UssLightSkinPath)
    {
    }

    internal void OnAnimationAdded() => OnAdded();

    internal void OnAnimationRemoved() => RemoveSelected();

    // ---- Host seam (the UI Builder drives the control through this surface; the authoring host uses the
    // [CreateProperty] two-way binding and never calls into it) -----------------------------------------------

    // Bulk-pushes the six longhand lists as composed from the host's backing store and rebuilds the rows once,
    // without raising the host change event (the analogue of FilterStyleField.SetValueWithoutNotify).
    public void SetLonghandListsWithoutNotify(
        List<UIAnimationClip> clips,
        List<float> durations,
        List<float> delays,
        List<AnimationIterationCount> iterationCounts,
        List<AnimationDirection> directions,
        List<AnimationPlayState> playStates)
    {
        ReplaceBackingList(m_AnimationClip, clips);
        ReplaceBackingList(m_AnimationDuration, durations);
        ReplaceBackingList(m_AnimationDelay, delays);
        ReplaceBackingList(m_AnimationIterationCount, iterationCounts);
        ReplaceBackingList(m_AnimationDirection, directions);
        ReplaceBackingList(m_AnimationPlayState, playStates);
        RefreshFromExternalPush();
    }

    public void SetLonghandContextMenu(StylePropertyId stylePropertyId, Action<DropdownMenu> populateMenu)
        => SetLonghandContextMenuCore(stylePropertyId, populateMenu);

    // Whether a row's clip button offers Edit... (opening the Animation window) once a clip is assigned. The
    // window edits whatever the global selection points at, so only a host that drives that selection may
    // leave this on. Rebuilding the rows on change is what repaints buttons that already rendered as Edit...
    public bool canEditClipsInAnimationWindow
    {
        get => m_CanEditClipsInAnimationWindow;
        set
        {
            if (m_CanEditClipsInAnimationWindow == value)
                return;

            m_CanEditClipsInAnimationWindow = value;
            Refresh();
        }
    }

    protected override void RaiseHostChangeEvent(int changeType, bool structural, bool cleared)
    {
        using var evt = AnimationLonghandListChangedEvent.GetPooled();
        evt.elementTarget = this;
        evt.changeType = (AnimationChangeType)changeType;
        evt.structural = structural;
        evt.cleared = cleared;
        SendEvent(evt);
    }

    protected override FoldoutLonghandField<AnimationData> CreateRow() => new FoldoutAnimationField(this);

    protected override AnimationData MakeDefaultData()
        => new AnimationData(null, k_DefaultDuration, k_DefaultDelay, k_DefaultIterationCount, k_DefaultDirection, k_DefaultPlayState);

    protected override AnimationData ComposeData(int index)
    {
        // The clip is not longhand-wrapped: past the end it is null rather than repeating.
        var clip = m_AnimationClip.Count > index ? m_AnimationClip[index] : null;
        var duration = ValueAt(m_AnimationDuration, index, k_DefaultDuration);
        var delay = ValueAt(m_AnimationDelay, index, k_DefaultDelay);
        var iterationCount = ValueAt(m_AnimationIterationCount, index, k_DefaultIterationCount);
        var direction = ValueAt(m_AnimationDirection, index, k_DefaultDirection);
        var playState = ValueAt(m_AnimationPlayState, index, k_DefaultPlayState);
        return new AnimationData(clip, duration, delay, iterationCount, direction, playState);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
