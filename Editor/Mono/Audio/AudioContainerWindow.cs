// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Audio;
using UnityEditor.Audio.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor;

sealed class AudioContainerWindow : EditorWindow
{
    /// <summary>
    /// The cached instance of the window, if it is open.
    /// </summary>
    internal static AudioContainerWindow Instance { get; private set; }

    internal readonly AudioContainerWindowState State = new();

    /// <summary>
    /// Holds the added list elements in the list interaction callbacks.
    /// Only used locally in these methods, but it's a global member to avoid GC.
    /// </summary>
    readonly List<AudioContainerElement> m_AddedElements = new();

    readonly string k_EmptyGuidString = Guid.Empty.ToString("N");

    VisualElement m_ContainerRootVisualElement;
    VisualElement m_Day0RootVisualElement;

    // Preview section
    Label m_AssetNameLabel;
    Button m_PlayStopButton;
    VisualElement m_PlayStopButtonImage;
    Button m_SkipButton;
    VisualElement m_SkipButtonImage;

    // Volume section
    Slider m_VolumeSlider;
    AudioRandomRangeSliderTracker m_VolumeRandomRangeTracker;
    FloatField m_VolumeField;
    Button m_VolumeRandomizationButton;
    VisualElement m_VolumeRandomizationButtonImage;
    MinMaxSlider m_VolumeRandomizationRangeSlider;
    Vector2Field m_VolumeRandomizationRangeField;
    AudioLevelMeter m_Meter;

    // Pitch section
    Slider m_PitchSlider;
    AudioRandomRangeSliderTracker m_PitchRandomRangeTracker;
    FloatField m_PitchField;
    Button m_PitchRandomizationButton;
    VisualElement m_PitchRandomizationButtonImage;
    MinMaxSlider m_PitchRandomizationRangeSlider;
    Vector2Field m_PitchRandomizationRangeField;

    // Clip list section
    ListView m_ClipsListView;
    AudioContainerListDragAndDropManipulator m_DragManipulator;

    // Trigger and playback mode section
    RadioButtonGroup m_TriggerRadioButtonGroup;
    RadioButtonGroup m_PlaybackModeRadioButtonGroup;
    IntegerField m_AvoidRepeatingLastField;

    // Automatic trigger section
    RadioButtonGroup m_AutomaticTriggerModeRadioButtonGroup;
    Slider m_TimeSlider;
    AudioRandomRangeSliderTracker m_TimeRandomRangeTracker;
    FloatField m_TimeField;
    Button m_TimeRandomizationButton;
    VisualElement m_TimeRandomizationButtonImage;
    MinMaxSlider m_TimeRandomizationRangeSlider;
    Vector2Field m_TimeRandomizationRangeField;
    RadioButtonGroup m_LoopRadioButtonGroup;
    IntegerField m_CountField;
    Button m_CountRandomizationButton;
    VisualElement m_CountRandomizationButtonImage;
    MinMaxSlider m_CountRandomizationRangeSlider;
    Vector2Field m_CountRandomizationRangeField;
    Label m_AutomaticTriggerModeLabel;
    Label m_LoopLabel;

    // Shared icon references
    Texture2D m_DiceIconOff;
    Texture2D m_DiceIconOn;

    bool m_IsVisible;
    bool m_IsSubscribedToGUICallbacksAndEvents;
    bool m_IsInitializing;
    bool m_Day0ElementsInitialized;
    bool m_ContainerElementsInitialized;
    bool m_ClipFieldProgressBarsAreCleared = true;

    /// <summary>
    /// Holds the previous state of the list elements for undo/delete housekeeping
    /// </summary>
    List<AudioContainerElement> m_CachedElements = new();

    [RequiredByNativeCode]
    internal static void CreateAudioRandomContainerWindow()
    {
        var window = GetWindow<AudioContainerWindow>();
        window.Show();
    }

    static void OnCreateButtonClicked()
    {
        ProjectWindowUtil.CreateAudioRandomContainer();
    }

    void OnEnable()
    {
        Instance = this;
        m_DiceIconOff = EditorGUIUtility.IconContent("AudioRandomContainer On Icon").image as Texture2D;
        m_DiceIconOn = EditorGUIUtility.IconContent("AudioRandomContainer Icon").image as Texture2D;
        SetTitle();
    }

    void OnDisable()
    {
        Instance = null;
        State.OnDestroy();
        UnsubscribeFromGUICallbacksAndEvents();
        m_IsInitializing = false;
        m_Day0ElementsInitialized = false;
        m_ContainerElementsInitialized = false;
        m_CachedElements.Clear();
        m_AddedElements.Clear();
    }

    void Update()
    {
        if (!m_IsVisible)
            return;

        if (State.IsPlayingOrPaused()) { UpdateClipFieldProgressBars(); }
        else if (!m_ClipFieldProgressBarsAreCleared) { ClearClipFieldProgressBars(); }

        if (m_Meter != null)
        {
            if (State.IsPlayingOrPaused())
            {
                if (State != null) { m_Meter.Value = State.GetMeterValue(); }
                else { m_Meter.Value = -80.0f; }
            }
            else
            {
                if (m_Meter.Value != -80.0f) { m_Meter.Value = -80.0f; }
            }
        }
    }

    void SetTitle()
    {
        var titleString = "Audio Random Container";

        if (State.IsDirty())
            titleString += "*";

        titleContent = new GUIContent(titleString)
        {
            image = m_DiceIconOff
        };
    }

    void CreateGUI()
    {
        try
        {
            if (m_IsInitializing)
                return;

            m_IsInitializing = true;

            var root = rootVisualElement;

            if (root.childCount == 0)
            {
                var rootAsset = UIToolkitUtilities.LoadUxml("UXML/Audio/AudioRandomContainer.uxml");
                Assert.IsNotNull(rootAsset);
                rootAsset.CloneTree(root);

                var styleSheet = UIToolkitUtilities.LoadStyleSheet("StyleSheets/Audio/AudioRandomContainer.uss");
                Assert.IsNotNull(styleSheet);
                root.styleSheets.Add(styleSheet);
                root.Add(State.GetResourceTrackerElement());

                m_ContainerRootVisualElement = UIToolkitUtilities.GetChildByName<ScrollView>(root, "ARC_ScrollView");
                Assert.IsNotNull(m_ContainerRootVisualElement);
                m_Day0RootVisualElement = UIToolkitUtilities.GetChildByName<VisualElement>(root, "Day0");
                Assert.IsNotNull(m_Day0RootVisualElement);
            }

            if (m_ContainerElementsInitialized)
                root.Unbind();

            if (State.AudioContainer == null)
            {
                if (!m_Day0ElementsInitialized)
                {
                    InitializeDay0Elements();
                    m_Day0ElementsInitialized = true;
                }

                m_Day0RootVisualElement.style.display = DisplayStyle.Flex;
                m_ContainerRootVisualElement.style.display = DisplayStyle.None;
            }
            else
            {
                if (!m_ContainerElementsInitialized)
                {
                    InitializeContainerElements();
                    m_ContainerElementsInitialized = true;
                    EditorApplication.update += OneTimeEditorApplicationUpdate;
                }

                if (!m_IsSubscribedToGUICallbacksAndEvents)
                    SubscribeToGUICallbacksAndEvents();

                BindAndTrackObjectAndProperties();

                m_Day0RootVisualElement.style.display = DisplayStyle.None;
                m_ContainerRootVisualElement.style.display = DisplayStyle.Flex;
                m_ClipsListView.Rebuild(); // Force a list rebuild when the list has changed or it will not always render correctly due to a UI toolkit bug.
            }
        }
        finally
        {
            m_IsInitializing = false;
        }
    }

    bool IsDisplayingTarget()
    {
        return
            m_Day0RootVisualElement != null
            && m_Day0RootVisualElement.style.display == DisplayStyle.None
            && m_ContainerRootVisualElement != null
            && m_ContainerRootVisualElement.style.display == DisplayStyle.Flex;
    }

    void InitializeDay0Elements()
    {
        var createButtonLabel = UIToolkitUtilities.GetChildByName<Label>(m_Day0RootVisualElement, "CreateButtonLabel");
        var createButton = UIToolkitUtilities.GetChildByName<Button>(m_Day0RootVisualElement, "CreateButton");
        createButton.clicked += OnCreateButtonClicked;
        createButtonLabel.text = "Select an existing Audio Random Container asset in the project browser or create a new one using the button below.";
    }

    void InitializeContainerElements()
    {
        InitializePreviewElements();
        InitializeVolumeElements();
        InitializePitchElements();
        InitializeClipListElements();
        InitializeTriggerAndPlayModeElements();
        InitializeAutomaticTriggerElements();
    }

    void SubscribeToGUICallbacksAndEvents()
    {
        if (!m_ContainerElementsInitialized || m_IsSubscribedToGUICallbacksAndEvents)
        {
            return;
        }

        SubscribeToPreviewCallbacksAndEvents();
        SubscribeToVolumeCallbacksAndEvents();
        SubscribeToPitchCallbacksAndEvents();
        SubscribeToClipListCallbacksAndEvents();
        SubscribeToAutomaticTriggerCallbacksAndEvents();
        m_IsSubscribedToGUICallbacksAndEvents = true;
    }

    void UnsubscribeFromGUICallbacksAndEvents()
    {
        if (!m_ContainerElementsInitialized || !m_IsSubscribedToGUICallbacksAndEvents)
        {
            return;
        }

        UnsubscribeFromPreviewCallbacksAndEvents();
        UnsubscribeFromVolumeCallbacksAndEvents();
        UnsubscribeFromPitchCallbacksAndEvents();
        UnsubscribeFromClipListCallbacksAndEvents();
        UnsubscribeFromAutomaticTriggerCallbacksAndEvents();
        m_IsSubscribedToGUICallbacksAndEvents = false;
    }

    void BindAndTrackObjectAndProperties()
    {
        m_ContainerRootVisualElement.TrackSerializedObjectValue(State.SerializedObject, OnSerializedObjectChanged);

        BindAndTrackPreviewProperties();
        BindAndTrackVolumeProperties();
        BindAndTrackPitchProperties();
        BindAndTrackClipListProperties();
        BindAndTrackTriggerAndPlayModeProperties();
        BindAndTrackAutomaticTriggerProperties();
    }

    void OnTargetChanged(object sender, EventArgs e)
    {
        SetTitle();
        CreateGUI();

        if (State.AudioContainer == null)
            m_CachedElements.Clear();
        else
            m_CachedElements = State.AudioContainer.elements.ToList();

        m_AddedElements.Clear();
    }

    void OnSerializedObjectChanged(SerializedObject obj)
    {
        SetTitle();
    }

    void OneTimeEditorApplicationUpdate()
    {
        // Setting this is a temp workaround for a UIToolKit bug
        // https://unity.slack.com/archives/C3414V4UV/p1681828689005249?thread_ts=1676901177.340799&cid=C3414V4UV
        m_ClipsListView.reorderable = true;
        m_ClipsListView.reorderMode = ListViewReorderMode.Animated;
        EditorApplication.update -= OneTimeEditorApplicationUpdate;
    }

    static void InsertUnitFieldForFloatField(VisualElement field, string unit)
    {
        var floatInput = UIToolkitUtilities.GetChildByName<VisualElement>(field, "unity-text-input");
        var unitTextElement = new TextElement
        {
            name = "numeric-field-unit-label",
            text = unit
        };
        floatInput.Add(unitTextElement);
    }

    #region Preview

    void InitializePreviewElements()
    {
        m_AssetNameLabel = UIToolkitUtilities.GetChildByName<Label>(m_ContainerRootVisualElement, "asset-name-label");
        m_PlayStopButton = UIToolkitUtilities.GetChildByName<Button>(m_ContainerRootVisualElement, "play-button");
        m_PlayStopButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(m_ContainerRootVisualElement, "play-button-image");
        m_SkipButton = UIToolkitUtilities.GetChildByName<Button>(m_ContainerRootVisualElement, "skip-button");
        m_SkipButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(m_ContainerRootVisualElement, "skip-button-image");

        var skipIcon = UIToolkitUtilities.LoadIcon("Skip");
        m_SkipButtonImage.style.backgroundImage = new StyleBackground(skipIcon);
    }

    void SubscribeToPreviewCallbacksAndEvents()
    {
        m_PlayStopButton.clicked += OnPlayStopButtonClicked;
        m_SkipButton.clicked += OnSkipButtonClicked;
    }

    void UnsubscribeFromPreviewCallbacksAndEvents()
    {
        if (m_PlayStopButton != null)
            m_PlayStopButton.clicked -= OnPlayStopButtonClicked;

        if (m_SkipButton != null)
            m_SkipButton.clicked -= OnSkipButtonClicked;
    }

    void BindAndTrackPreviewProperties()
    {
        UpdateTransportButtonStates();
        m_AssetNameLabel.text = State.AudioContainer.name;
    }

    void OnPlayStopButtonClicked()
    {
        if (State.IsPlayingOrPaused())
        {
            State.Stop();
            ClearClipFieldProgressBars();
        }
        else
            State.Play();

        UpdateTransportButtonStates();
    }

    void OnSkipButtonClicked()
    {
        if (State.IsPlayingOrPaused())
            State.Skip();
    }

    void UpdateTransportButtonStates()
    {
        var editorIsPaused = EditorApplication.isPaused;

        m_PlayStopButton?.SetEnabled(State.IsReadyToPlay() && !editorIsPaused);
        m_SkipButton?.SetEnabled(State.IsPlayingOrPaused() && State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic && !editorIsPaused);

        var image =
            State.IsPlayingOrPaused()
                ? UIToolkitUtilities.LoadIcon("Stop")
                : UIToolkitUtilities.LoadIcon("Play");

        m_PlayStopButtonImage.style.backgroundImage = new StyleBackground(image);
    }

    void OnTransportStateChanged(object sender, EventArgs e)
    {
        UpdateTransportButtonStates();
    }

    void EditorPauseStateChanged(object sender, EventArgs e)
    {
        UpdateTransportButtonStates();
    }

    #endregion

    #region Volume

    void InitializeVolumeElements()
    {
        m_Meter = UIToolkitUtilities.GetChildByName<AudioLevelMeter>(m_ContainerRootVisualElement, "meter");
        m_VolumeSlider = UIToolkitUtilities.GetChildByName<Slider>(m_ContainerRootVisualElement, "volume-slider");
        m_VolumeRandomRangeTracker = AudioRandomRangeSliderTracker.Create(m_VolumeSlider, State.AudioContainer.volumeRandomizationRange);
        m_VolumeField = UIToolkitUtilities.GetChildByName<FloatField>(m_ContainerRootVisualElement, "volume-field");
        m_VolumeRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(m_ContainerRootVisualElement, "volume-randomization-button");
        m_VolumeRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(m_ContainerRootVisualElement, "volume-randomization-button-image");
        m_VolumeRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(m_ContainerRootVisualElement, "volume-randomization-range-slider");
        m_VolumeRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(m_ContainerRootVisualElement, "volume-randomization-range-field");
        var volumeRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_VolumeRandomizationRangeField, "unity-x-input");
        var volumeRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_VolumeRandomizationRangeField, "unity-y-input");

        m_VolumeField.formatString = "0.#";
        InsertUnitFieldForFloatField(m_VolumeField, "dB");
        m_VolumeField.isDelayed = true;
        volumeRandomizationMinField.isDelayed = true;
        volumeRandomizationMinField.label = "";
        volumeRandomizationMinField.formatString = "0.#";
        InsertUnitFieldForFloatField(volumeRandomizationMinField, "dB");
        volumeRandomizationMaxField.isDelayed = true;
        volumeRandomizationMaxField.label = "";
        volumeRandomizationMaxField.formatString = "0.#";
        InsertUnitFieldForFloatField(volumeRandomizationMaxField, "dB");
    }

    void SubscribeToVolumeCallbacksAndEvents()
    {
        m_VolumeRandomizationButton.clicked += OnVolumeRandomizationButtonClicked;
        m_VolumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        m_VolumeRandomizationRangeSlider.RegisterValueChangedCallback(OnVolumeRandomizationRangeChanged);
        m_VolumeRandomizationRangeField.RegisterValueChangedCallback(OnVolumeRandomizationRangeChanged);
    }

    void UnsubscribeFromVolumeCallbacksAndEvents()
    {
        if (m_VolumeRandomizationButton != null)
            m_VolumeRandomizationButton.clicked -= OnVolumeRandomizationButtonClicked;

        m_VolumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
        m_VolumeRandomizationRangeSlider?.UnregisterValueChangedCallback(OnVolumeRandomizationRangeChanged);
        m_VolumeRandomizationRangeField?.UnregisterValueChangedCallback(OnVolumeRandomizationRangeChanged);
    }

    void BindAndTrackVolumeProperties()
    {
        var volumeProperty = State.SerializedObject.FindProperty("m_Volume");
        var volumeRandomizationEnabledProperty = State.SerializedObject.FindProperty("m_VolumeRandomizationEnabled");
        var volumeRandomizationRangeProperty = State.SerializedObject.FindProperty("m_VolumeRandomizationRange");

        m_VolumeSlider.BindProperty(volumeProperty);
        m_VolumeField.BindProperty(volumeProperty);
        m_VolumeRandomizationRangeSlider.BindProperty(volumeRandomizationRangeProperty);
        m_VolumeRandomizationRangeField.BindProperty(volumeRandomizationRangeProperty);

        m_VolumeRandomizationButton.TrackPropertyValue(volumeRandomizationEnabledProperty, OnVolumeRandomizationEnabledChanged);

        OnVolumeRandomizationEnabledChanged(volumeRandomizationEnabledProperty);
    }

    void OnVolumeChanged(ChangeEvent<float> evt)
    {
        m_VolumeRandomRangeTracker.SetRange(State.AudioContainer.volumeRandomizationRange);
    }

    void OnVolumeRandomizationRangeChanged(ChangeEvent<Vector2> evt)
    {
        // Have to clamp immediately here to avoid UI jitter because the min-max slider cannot clamp before updating the property
        var newValue = evt.newValue;

        if (newValue.x > 0)
            newValue.x = 0;

        if (newValue.y < 0)
            newValue.y = 0;

        m_VolumeRandomRangeTracker.SetRange(newValue);
    }

    void OnVolumeRandomizationEnabledChanged(SerializedProperty property)
    {
        if (property.boolValue)
        {
            m_VolumeRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOn);
            m_VolumeRandomizationRangeSlider.SetEnabled(true);
            m_VolumeRandomizationRangeField.SetEnabled(true);
        }
        else
        {
            m_VolumeRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOff);
            m_VolumeRandomizationRangeSlider.SetEnabled(false);
            m_VolumeRandomizationRangeField.SetEnabled(false);
        }
    }

    void OnVolumeRandomizationButtonClicked()
    {
        var newButtonStateString = !State.AudioContainer.volumeRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(State.AudioContainer, $"Modified Volume Randomization {newButtonStateString} in {State.AudioContainer.name}");
        State.AudioContainer.volumeRandomizationEnabled = !State.AudioContainer.volumeRandomizationEnabled;
    }

    #endregion

    #region Pitch

    void InitializePitchElements()
    {
        m_PitchSlider = UIToolkitUtilities.GetChildByName<Slider>(m_ContainerRootVisualElement, "pitch-slider");
        m_PitchRandomRangeTracker = AudioRandomRangeSliderTracker.Create(m_PitchSlider, State.AudioContainer.pitchRandomizationRange);
        m_PitchField = UIToolkitUtilities.GetChildByName<FloatField>(m_ContainerRootVisualElement, "pitch-field");
        m_PitchRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(m_ContainerRootVisualElement, "pitch-randomization-button");
        m_PitchRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(m_ContainerRootVisualElement, "pitch-randomization-button-image");
        m_PitchRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(m_ContainerRootVisualElement, "pitch-randomization-range-slider");
        m_PitchRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(m_ContainerRootVisualElement, "pitch-randomization-range-field");
        var pitchRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_PitchRandomizationRangeField, "unity-x-input");
        var pitchRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_PitchRandomizationRangeField, "unity-y-input");

        m_PitchField.formatString = "0";
        InsertUnitFieldForFloatField(m_PitchField, "ct");
        m_PitchField.isDelayed = true;
        pitchRandomizationMinField.isDelayed = true;
        pitchRandomizationMinField.label = "";
        pitchRandomizationMinField.formatString = "0";
        InsertUnitFieldForFloatField(pitchRandomizationMinField, "ct");
        pitchRandomizationMaxField.isDelayed = true;
        pitchRandomizationMaxField.label = "";
        pitchRandomizationMaxField.formatString = "0";
        InsertUnitFieldForFloatField(pitchRandomizationMaxField, "ct");
    }

    void SubscribeToPitchCallbacksAndEvents()
    {
        m_PitchRandomizationButton.clicked += OnPitchRandomizationButtonClicked;
        m_PitchSlider.RegisterValueChangedCallback(OnPitchChanged);
        m_PitchRandomizationRangeSlider.RegisterValueChangedCallback(OnPitchRandomizationRangeChanged);
        m_PitchRandomizationRangeField.RegisterValueChangedCallback(OnPitchRandomizationRangeChanged);
    }

    void UnsubscribeFromPitchCallbacksAndEvents()
    {
        if (m_PitchRandomizationButton != null)
            m_PitchRandomizationButton.clicked -= OnPitchRandomizationButtonClicked;

        m_PitchSlider?.UnregisterValueChangedCallback(OnPitchChanged);
        m_PitchRandomizationRangeSlider?.UnregisterValueChangedCallback(OnPitchRandomizationRangeChanged);
        m_PitchRandomizationRangeField?.UnregisterValueChangedCallback(OnPitchRandomizationRangeChanged);
    }

    void BindAndTrackPitchProperties()
    {
        var pitchProperty = State.SerializedObject.FindProperty("m_Pitch");
        var pitchRandomizationEnabledProperty = State.SerializedObject.FindProperty("m_PitchRandomizationEnabled");
        var pitchRandomizationRangeProperty = State.SerializedObject.FindProperty("m_PitchRandomizationRange");

        m_PitchSlider.BindProperty(pitchProperty);
        m_PitchField.BindProperty(pitchProperty);
        m_PitchRandomizationRangeSlider.BindProperty(pitchRandomizationRangeProperty);
        m_PitchRandomizationRangeField.BindProperty(pitchRandomizationRangeProperty);

        m_PitchRandomizationButton.TrackPropertyValue(pitchRandomizationEnabledProperty, OnPitchRandomizationEnabledChanged);

        OnPitchRandomizationEnabledChanged(pitchRandomizationEnabledProperty);
    }

    void OnPitchChanged(ChangeEvent<float> evt)
    {
        m_PitchRandomRangeTracker.SetRange(State.AudioContainer.pitchRandomizationRange);
    }

    void OnPitchRandomizationRangeChanged(ChangeEvent<Vector2> evt)
    {
        // Have to clamp immediately here to avoid UI jitter because the min-max slider cannot clamp before updating the property
        var newValue = evt.newValue;

        if (newValue.x > 0)
            newValue.x = 0;

        if (newValue.y < 0)
            newValue.y = 0;

        m_PitchRandomRangeTracker.SetRange(newValue);
    }

    void OnPitchRandomizationEnabledChanged(SerializedProperty property)
    {
        if (property.boolValue)
        {
            m_PitchRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOn);
            m_PitchRandomizationRangeSlider.SetEnabled(true);
            m_PitchRandomizationRangeField.SetEnabled(true);
        }
        else
        {
            m_PitchRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOff);
            m_PitchRandomizationRangeSlider.SetEnabled(false);
            m_PitchRandomizationRangeField.SetEnabled(false);
        }
    }

    void OnPitchRandomizationButtonClicked()
    {
        var newButtonStateString = !State.AudioContainer.pitchRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(State.AudioContainer, $"Modified Pitch Randomization {newButtonStateString} in {State.AudioContainer.name}");
        State.AudioContainer.pitchRandomizationEnabled = !State.AudioContainer.pitchRandomizationEnabled;
    }

    #endregion

    #region ClipList

    void InitializeClipListElements()
    {
        m_ClipsListView = UIToolkitUtilities.GetChildByName<ListView>(m_ContainerRootVisualElement, "audio-clips-list-view");
        m_ClipsListView.CreateDragAndDropController();
        m_DragManipulator = new AudioContainerListDragAndDropManipulator(m_ContainerRootVisualElement);
        m_ClipsListView.fixedItemHeight = 24;
    }

    void SubscribeToClipListCallbacksAndEvents()
    {
        m_ClipsListView.itemsAdded += OnListItemsAdded;
        m_ClipsListView.itemsRemoved += OnListItemsRemoved;
        m_ClipsListView.itemIndexChanged += OnItemListIndexChanged;
        m_ClipsListView.makeItem = OnMakeListItem;
        m_ClipsListView.bindItem = OnBindListItem;

        // We need a no-op unbind callback to prevent the default implementation from being run.
        // See the comments in UUM-46918.
        m_ClipsListView.unbindItem = (elm, idx) => { };
        m_DragManipulator.addAudioClipsDelegate += OnAudioClipDrag;
    }

    void UnsubscribeFromClipListCallbacksAndEvents()
    {
        if (m_ClipsListView != null)
        {
            m_ClipsListView.itemsAdded -= OnListItemsAdded;
            m_ClipsListView.itemsRemoved -= OnListItemsRemoved;
            m_ClipsListView.itemIndexChanged -= OnItemListIndexChanged;
            m_ClipsListView.makeItem = null;
            m_ClipsListView.bindItem = null;
            m_ClipsListView.unbindItem = null;
        }

        if (m_DragManipulator != null)
            m_DragManipulator.addAudioClipsDelegate -= OnAudioClipDrag;
    }

    void BindAndTrackClipListProperties()
    {
        var clipsProperty = State.SerializedObject.FindProperty("m_Elements");

        m_ClipsListView.BindProperty(clipsProperty);
        m_ClipsListView.TrackPropertyValue(clipsProperty, OnAudioClipListChanged);
    }

    static void UpdateListElementName(Object element, Object clip = null)
    {
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(element, out var guid, out var localId);
        var name = clip == null ? nameof(AudioContainerElement) : clip.name;
        element.name = $"{name}_{{{localId}}}";
    }

    static VisualElement OnMakeListItem()
    {
        var element = UIToolkitUtilities.LoadUxml("UXML/Audio/AudioContainerElement.uxml").Instantiate();
        var volumeField = UIToolkitUtilities.GetChildByName<FloatField>(element, "volume-field");
        InsertUnitFieldForFloatField(volumeField, "dB");
        return element;
    }

    void OnBindListItem(VisualElement element, int index)
    {
        // There is currently a bug in UIToolkit where the reported index can be out of bounds after shrinking the list
        if (index > State.AudioContainer.elements.Length - 1)
            return;

        var enabledToggle = UIToolkitUtilities.GetChildByName<Toggle>(element, "enabled-toggle");
        var audioClipField = UIToolkitUtilities.GetChildByName<AudioContainerElementClipField>(element, "audio-clip-field");
        var volumeField = UIToolkitUtilities.GetChildByName<FloatField>(element, "volume-field");
        volumeField.formatString = "0.#";

        audioClipField.objectType = typeof(AudioClip);

        var listElement = State.AudioContainer.elements[index];

        if (listElement == null)
        {
            Debug.LogError($"AudioContainerElement at index {index} is null. Please report using `Help > Report a Bug...`.");
            element.SetEnabled(false);
            return;
        }

        element.SetEnabled(true);
        audioClipField.RegisterCallback<DragPerformEvent>(OnListDragPerform);
        audioClipField.AssetElementInstanceID = listElement.GetInstanceID();

        var serializedObject = new SerializedObject(listElement);

        var enabledProperty = serializedObject.FindProperty("m_Enabled");
        var audioClipProperty = serializedObject.FindProperty("m_AudioClip");
        var volumeProperty = serializedObject.FindProperty("m_Volume");

        // Shouldn't be necessary to unbind here, but currently required to work around an exception
        // being thrown when calling TrackPropertyValue. See https://jira.unity3d.com/browse/UUM-46918
        // Should be removed once this issue has been fixed.
        enabledToggle.Unbind();
        audioClipField.Unbind();
        volumeField.Unbind();

        enabledToggle.BindProperty(enabledProperty);
        audioClipField.BindProperty(audioClipProperty);
        volumeField.BindProperty(volumeProperty);

        enabledToggle.TrackPropertyValue(enabledProperty, OnElementEnabledToggleChanged);
        audioClipField.TrackPropertyValue(audioClipProperty, OnElementAudioClipChanged);
        volumeField.TrackPropertyValue(volumeProperty, OnElementPropertyChanged);
    }

    static void OnListDragPerform(DragPerformEvent evt)
    {
        evt.StopPropagation();
    }

    void OnElementAudioClipChanged(SerializedProperty property)
    {
        var element = property.serializedObject.targetObject as AudioContainerElement;
        Assert.IsNotNull(element);
        var clip = property.objectReferenceValue as AudioClip;
        UpdateListElementName(element, clip);
        OnElementPropertyChanged(property);
        UpdateTransportButtonStates();
        State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
    }

    void OnElementEnabledToggleChanged(SerializedProperty property)
    {
        OnElementPropertyChanged(property);
        UpdateTransportButtonStates();

        // Changing a property on the ListElement subasset does not call CheckConsistency on the main Asset
        // So quickly flip the values to force an update. :(
        var last = State.AudioContainer.avoidRepeatingLast;
        State.AudioContainer.avoidRepeatingLast = -1;
        State.AudioContainer.avoidRepeatingLast = last;

        if (State.IsPlayingOrPaused())
            State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
    }

    void OnElementPropertyChanged(SerializedProperty property)
    {
        EditorUtility.SetDirty(State.AudioContainer);
        SetTitle();
    }

    void OnListItemsAdded(IEnumerable<int> indices)
    {
        var indicesArray = indices as int[] ?? indices.ToArray();
        var elements = State.AudioContainer.elements.ToList();

        foreach (var index in indicesArray)
        {
            var element = new AudioContainerElement
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(element, State.AudioContainer);
            UpdateListElementName(element);
            elements[index] = element;
            m_AddedElements.Add(element);
        }

        State.AudioContainer.elements = elements.ToArray();

        // Object creation undo recording needs to be done in a separate pass from the object property changes above
        foreach (var element in m_AddedElements)
            Undo.RegisterCreatedObjectUndo(element, "Create AudioContainerElement");

        m_AddedElements.Clear();

        var undoName = $"Add {nameof(AudioRandomContainer)} element";

        if (indicesArray.Length > 1)
        {
            undoName = $"{undoName}s";
        }

        Undo.SetCurrentGroupName(undoName);

        m_AddedElements.Clear();
    }

    void OnListItemsRemoved(IEnumerable<int> indices)
    {
        var indicesArray = indices as int[] ?? indices.ToArray();

        // Confusingly, this callback is sometimes invoked post-delete and sometimes pre-delete,
        // i.e. the AudioRandomContainer.elements property may or may not be updated at this time,
        // so we use the cached list to be sure we get the correct reference to the subasset to delete.
        foreach (var index in indicesArray)
        {
            if (m_CachedElements[index] != null)
            {
                AssetDatabase.RemoveObjectFromAsset(m_CachedElements[index]);
                Undo.DestroyObjectImmediate(m_CachedElements[index]);
            }
        }

        State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);

        var undoName = $"Remove {nameof(AudioRandomContainer)} element";

        if (indicesArray.Length > 1)
        {
            undoName = $"{undoName}s";
        }

        Undo.SetCurrentGroupName(undoName);
    }

    void OnItemListIndexChanged(int oldIndex, int newIndex)
    {
        Undo.SetCurrentGroupName($"Reorder {nameof(AudioRandomContainer)} list");
        State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
    }

    void OnAudioClipDrag(List<AudioClip> audioClips)
    {
        var undoName = $"Add {nameof(AudioRandomContainer)} element";

        if (audioClips.Count > 1)
            undoName = $"{undoName}s";

        Undo.RegisterCompleteObjectUndo(State.AudioContainer, undoName);

        var elements = State.AudioContainer.elements.ToList();

        foreach (var audioClip in audioClips)
        {
            var element = new AudioContainerElement
            {
                audioClip = audioClip,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(element, State.AudioContainer);
            UpdateListElementName(element, audioClip);
            elements.Add(element);
            m_AddedElements.Add(element);
        }

        State.AudioContainer.elements = elements.ToArray();

        // Object creation undo recording needs to be done in a separate pass from the object property changes above
        foreach (var element in m_AddedElements)
            Undo.RegisterCreatedObjectUndo(element, "Create AudioContainerElement");

        m_AddedElements.Clear();
        Undo.SetCurrentGroupName(undoName);
    }

    void OnAudioClipListChanged(SerializedProperty property)
    {
        // Do manual fixup of orphaned subassets after a possible undo of item removal
        // because the undo system does not play nice with RegisterCreatedObjectUndo.
        if (m_CachedElements.Count < State.AudioContainer.elements.Length)
        {
            var elements = State.AudioContainer.elements;

            foreach (var elm in elements)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(elm, out var guid, out var localId);

                // An empty asset GUID means the subasset has lost the reference
                // to the main asset after an undo of item removal, so re-add it manually.
                if (guid.Equals(k_EmptyGuidString))
                    AssetDatabase.AddObjectToAsset(elm, State.AudioContainer);
            }
        }

        // Update the cached list of elements
        m_CachedElements = State.AudioContainer.elements.ToList();

        // Force a list rebuild when the list has changed or it will not always render correctly
        m_ClipsListView.Rebuild();

        UpdateTransportButtonStates();
        SetTitle();
    }

    void UpdateClipFieldProgressBars()
    {
        var playables = State.GetActivePlayables();

        if (playables == null)
            return;

        // Iterate over the ActivePlayables from the runtime and try and match them to the instance ID on the clip field.
        // if its a match, set the progress and remove the clip field to avoid overwriting the progress.
        var clipFields = m_ClipsListView.Query<AudioContainerElementClipField>().ToList();

        // We need to sort the active playables as the runtime does not guarantee order
        Array.Sort(playables, (x, y) => x.settings.scheduledTime.CompareTo(y.settings.scheduledTime));

        for (var i = playables.Length - 1; i >= 0; i--)
        {
            var playable = new AudioClipPlayable(playables[i].clipPlayableHandle);

            for (var j = clipFields.Count - 1; j >= 0; j--)
            {
                var field = clipFields[j];

                if (field.AssetElementInstanceID == playables[i].settings.element.GetInstanceID())
                {
                    field.Progress = playable.GetClipPositionSec() / playable.GetClip().length;
                    clipFields.RemoveAt(j);
                }
            }
        }

        // Any clip fields that did not have a match with active playables should have their progress set to 0.
        foreach (var field in clipFields)
            if (field.Progress != 0.0f)
                field.Progress = 0.0f;

        m_ClipFieldProgressBarsAreCleared = false;
    }

    void ClearClipFieldProgressBars()
    {
        if (m_ClipsListView == null)
            return;

        var clipFields = m_ClipsListView.Query<AudioContainerElementClipField>().ToList();

        foreach (var field in clipFields)
            field.Progress = 0.0f;

        m_ClipFieldProgressBarsAreCleared = true;
    }

    #endregion

    #region TriggerAndPlaybackMode

    void InitializeTriggerAndPlayModeElements()
    {
        m_TriggerRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(m_ContainerRootVisualElement, "trigger-radio-button-group");
        m_PlaybackModeRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(m_ContainerRootVisualElement, "playback-radio-button-group");
        m_AvoidRepeatingLastField = UIToolkitUtilities.GetChildByName<IntegerField>(m_ContainerRootVisualElement, "avoid-repeating-last-field");
    }

    void BindAndTrackTriggerAndPlayModeProperties()
    {
        var triggerProperty = State.SerializedObject.FindProperty("m_TriggerMode");
        var playbackModeProperty = State.SerializedObject.FindProperty("m_PlaybackMode");
        var avoidRepeatingLastProperty = State.SerializedObject.FindProperty("m_AvoidRepeatingLast");

        m_TriggerRadioButtonGroup.BindProperty(triggerProperty);
        m_TriggerRadioButtonGroup.TrackPropertyValue(triggerProperty, OnTriggerChanged);
        m_PlaybackModeRadioButtonGroup.BindProperty(playbackModeProperty);
        m_PlaybackModeRadioButtonGroup.TrackPropertyValue(playbackModeProperty, OnPlaybackModeChanged);
        m_AvoidRepeatingLastField.BindProperty(avoidRepeatingLastProperty);

        OnTriggerChanged((AudioRandomContainerTriggerMode)m_TriggerRadioButtonGroup.value);
        OnPlaybackModeChanged(playbackModeProperty);
    }

    void OnTriggerChanged(SerializedProperty property)
    {
        OnTriggerChanged((AudioRandomContainerTriggerMode)property.intValue);
    }

    void OnTriggerChanged(AudioRandomContainerTriggerMode mode)
    {
        var enabled = mode == AudioRandomContainerTriggerMode.Automatic;
        m_AutomaticTriggerModeRadioButtonGroup.SetEnabled(enabled);
        m_TimeSlider.SetEnabled(enabled);
        m_TimeField.SetEnabled(enabled);
        m_LoopRadioButtonGroup.SetEnabled(enabled);
        m_AutomaticTriggerModeLabel.SetEnabled(enabled);
        m_LoopLabel.SetEnabled(enabled);
        m_TimeRandomizationButton.SetEnabled(enabled);
        m_CountRandomizationButton.SetEnabled(enabled);

        var loopProperty = State.SerializedObject.FindProperty("m_LoopMode");
        OnLoopChanged(loopProperty);

        var timeRandomizationEnabledProperty = State.SerializedObject.FindProperty("m_AutomaticTriggerTimeRandomizationEnabled");
        OnTimeRandomizationEnabledChanged(timeRandomizationEnabledProperty);
    }

    void OnPlaybackModeChanged(SerializedProperty property)
    {
        m_AvoidRepeatingLastField.SetEnabled(property.intValue == (int)AudioRandomContainerPlaybackMode.Random);
    }

    #endregion

    #region AutomaticTrigger

    void InitializeAutomaticTriggerElements()
    {
        m_AutomaticTriggerModeRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(m_ContainerRootVisualElement, "trigger-mode-radio-button-group");
        m_TimeSlider = UIToolkitUtilities.GetChildByName<Slider>(m_ContainerRootVisualElement, "time-slider");
        m_TimeRandomRangeTracker = AudioRandomRangeSliderTracker.Create(m_TimeSlider, State.AudioContainer.automaticTriggerTimeRandomizationRange);
        m_TimeField = UIToolkitUtilities.GetChildByName<FloatField>(m_ContainerRootVisualElement, "time-field");
        m_TimeRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(m_ContainerRootVisualElement, "time-randomization-button");
        m_TimeRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(m_ContainerRootVisualElement, "time-randomization-button-image");
        m_TimeRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(m_ContainerRootVisualElement, "time-randomization-range-slider");
        m_TimeRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(m_ContainerRootVisualElement, "time-randomization-range-field");
        var timeRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_TimeRandomizationRangeField, "unity-x-input");
        var timeRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_TimeRandomizationRangeField, "unity-y-input");
        m_LoopRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(m_ContainerRootVisualElement, "loop-radio-button-group");
        m_CountField = UIToolkitUtilities.GetChildByName<IntegerField>(m_ContainerRootVisualElement, "count-field");
        m_CountRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(m_ContainerRootVisualElement, "count-randomization-button");
        m_CountRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(m_ContainerRootVisualElement, "count-randomization-button-image");
        m_CountRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(m_ContainerRootVisualElement, "count-randomization-range-slider");
        m_CountRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(m_ContainerRootVisualElement, "count-randomization-range-field");
        var countRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_CountRandomizationRangeField, "unity-x-input");
        var countRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_CountRandomizationRangeField, "unity-y-input");
        m_AutomaticTriggerModeLabel = UIToolkitUtilities.GetChildByName<Label>(m_ContainerRootVisualElement, "automatic-trigger-mode-label");
        m_LoopLabel = UIToolkitUtilities.GetChildByName<Label>(m_ContainerRootVisualElement, "loop-label");

        m_TimeField.formatString = "0.00";
        InsertUnitFieldForFloatField(m_TimeField, "s");
        m_TimeField.isDelayed = true;
        timeRandomizationMinField.isDelayed = true;
        timeRandomizationMinField.label = "";
        timeRandomizationMinField.formatString = "0.#";
        InsertUnitFieldForFloatField(timeRandomizationMinField, "s");
        timeRandomizationMaxField.isDelayed = true;
        timeRandomizationMaxField.label = "";
        timeRandomizationMaxField.formatString = "0.#";
        InsertUnitFieldForFloatField(timeRandomizationMaxField, "s");

        m_CountField.formatString = "0.#";
        m_CountField.isDelayed = true;
        countRandomizationMinField.isDelayed = true;
        countRandomizationMinField.label = "";
        countRandomizationMaxField.isDelayed = true;
        countRandomizationMaxField.label = "";
    }

    void SubscribeToAutomaticTriggerCallbacksAndEvents()
    {
        m_TimeRandomizationButton.clicked += OnTimeRandomizationButtonClicked;
        m_CountRandomizationButton.clicked += OnCountRandomizationButtonClicked;
        m_TimeSlider.RegisterValueChangedCallback(OnTimeChanged);
        m_TimeRandomizationRangeField.RegisterValueChangedCallback(OnTimeRandomizationRangeChanged);
        m_TimeRandomizationRangeSlider.RegisterValueChangedCallback(OnTimeRandomizationRangeChanged);
    }

    void UnsubscribeFromAutomaticTriggerCallbacksAndEvents()
    {
        if (m_TimeRandomizationButton != null)
            m_TimeRandomizationButton.clicked -= OnTimeRandomizationButtonClicked;

        if (m_CountRandomizationButton != null)
            m_CountRandomizationButton.clicked -= OnCountRandomizationButtonClicked;

        m_TimeSlider?.UnregisterValueChangedCallback(OnTimeChanged);
        m_TimeRandomizationRangeField?.UnregisterValueChangedCallback(OnTimeRandomizationRangeChanged);
        m_TimeRandomizationRangeSlider?.UnregisterValueChangedCallback(OnTimeRandomizationRangeChanged);
    }

    void BindAndTrackAutomaticTriggerProperties()
    {
        var automaticTriggerModeProperty = State.SerializedObject.FindProperty("m_AutomaticTriggerMode");
        var triggerTimeProperty = State.SerializedObject.FindProperty("m_AutomaticTriggerTime");
        var triggerTimeRandomizationEnabledProperty = State.SerializedObject.FindProperty("m_AutomaticTriggerTimeRandomizationEnabled");
        var triggerTimeRandomizationRangeProperty = State.SerializedObject.FindProperty("m_AutomaticTriggerTimeRandomizationRange");
        var loopModeProperty = State.SerializedObject.FindProperty("m_LoopMode");
        var loopCountProperty = State.SerializedObject.FindProperty("m_LoopCount");
        var loopCountRandomizationEnabledProperty = State.SerializedObject.FindProperty("m_LoopCountRandomizationEnabled");
        var loopCountRandomizationRangeProperty = State.SerializedObject.FindProperty("m_LoopCountRandomizationRange");

        m_AutomaticTriggerModeRadioButtonGroup.BindProperty(automaticTriggerModeProperty);
        m_TimeSlider.BindProperty(triggerTimeProperty);
        m_TimeField.BindProperty(triggerTimeProperty);
        m_TimeRandomizationRangeSlider.BindProperty(triggerTimeRandomizationRangeProperty);
        m_TimeRandomizationRangeField.BindProperty(triggerTimeRandomizationRangeProperty);
        m_LoopRadioButtonGroup.BindProperty(loopModeProperty);
        m_CountField.BindProperty(loopCountProperty);
        m_CountRandomizationRangeSlider.BindProperty(loopCountRandomizationRangeProperty);
        m_CountRandomizationRangeField.BindProperty(loopCountRandomizationRangeProperty);

        m_TimeRandomizationButton.TrackPropertyValue(triggerTimeRandomizationEnabledProperty, OnTimeRandomizationEnabledChanged);
        m_LoopRadioButtonGroup.TrackPropertyValue(loopModeProperty, OnLoopChanged);
        m_CountRandomizationButton.TrackPropertyValue(loopCountRandomizationEnabledProperty, OnCountRandomizationEnabledChanged);

        OnTimeRandomizationEnabledChanged(triggerTimeRandomizationEnabledProperty);
        OnLoopChanged(loopModeProperty);
        OnCountRandomizationEnabledChanged(loopCountRandomizationEnabledProperty);
    }

    void OnTimeChanged(ChangeEvent<float> evt)
    {
        m_TimeRandomRangeTracker.SetRange(State.AudioContainer.automaticTriggerTimeRandomizationRange);
    }

    void OnTimeRandomizationRangeChanged(ChangeEvent<Vector2> evt)
    {
        // Have to clamp immediately here to avoid UI jitter because the min-max slider cannot clamp before updating the property
        var newValue = evt.newValue;

        if (newValue.x > 0)
            newValue.x = 0;

        if (newValue.y < 0)
            newValue.y = 0;

        m_TimeRandomRangeTracker.SetRange(newValue);
    }

    void OnTimeRandomizationEnabledChanged(SerializedProperty property)
    {
        if (property.boolValue
            && State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic)
        {
            m_TimeRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOn);
            m_TimeRandomizationRangeSlider.SetEnabled(true);
            m_TimeRandomizationRangeField.SetEnabled(true);
        }
        else
        {
            m_TimeRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOff);
            m_TimeRandomizationRangeSlider.SetEnabled(false);
            m_TimeRandomizationRangeField.SetEnabled(false);
        }
    }

    void OnTimeRandomizationButtonClicked()
    {
        var newButtonStateString = !State.AudioContainer.automaticTriggerTimeRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(State.AudioContainer, $"Modified Time Randomization {newButtonStateString} in {State.AudioContainer.name}");
        State.AudioContainer.automaticTriggerTimeRandomizationEnabled = !State.AudioContainer.automaticTriggerTimeRandomizationEnabled;
    }

    void OnLoopChanged(SerializedProperty property)
    {
        var enabled = property.intValue != (int)AudioRandomContainerLoopMode.Infinite && State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic;

        m_CountField.SetEnabled(enabled);
        m_CountRandomizationRangeSlider.SetEnabled(enabled);
        m_CountRandomizationRangeField.SetEnabled(enabled);
        m_CountRandomizationButton.SetEnabled(enabled);

        var countRandomizationEnabledProperty = State.SerializedObject.FindProperty("m_LoopCountRandomizationEnabled");
        OnCountRandomizationEnabledChanged(countRandomizationEnabledProperty);
    }

    void OnCountRandomizationEnabledChanged(SerializedProperty property)
    {
        if (property.boolValue
            && State.AudioContainer.loopMode != AudioRandomContainerLoopMode.Infinite
            && State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic)
        {
            m_CountRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOn);
            m_CountRandomizationRangeSlider.SetEnabled(true);
            m_CountRandomizationRangeField.SetEnabled(true);
        }
        else
        {
            m_CountRandomizationButtonImage.style.backgroundImage = new StyleBackground(m_DiceIconOff);
            m_CountRandomizationRangeSlider.SetEnabled(false);
            m_CountRandomizationRangeField.SetEnabled(false);
        }
    }

    void OnCountRandomizationButtonClicked()
    {
        var newButtonStateString = !State.AudioContainer.loopCountRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(State.AudioContainer, $"Modified Count Randomization {newButtonStateString} in {State.AudioContainer.name}");
        State.AudioContainer.loopCountRandomizationEnabled = !State.AudioContainer.loopCountRandomizationEnabled;
    }

    #endregion

    #region GlobalEditorCallbackHandlers

    void OnBecameVisible()
    {
        m_IsVisible = true;
        State.TargetChanged += OnTargetChanged;
        State.TransportStateChanged += OnTransportStateChanged;
        State.EditorPauseStateChanged += EditorPauseStateChanged;
        State.Resume();

        if (!m_IsSubscribedToGUICallbacksAndEvents
            && m_ContainerElementsInitialized
            && IsDisplayingTarget())
        {
            SubscribeToGUICallbacksAndEvents();
        }
    }

    void OnBecameInvisible()
    {
        m_IsVisible = false;
        State.TargetChanged -= OnTargetChanged;
        State.TransportStateChanged -= OnTransportStateChanged;
        State.EditorPauseStateChanged -= EditorPauseStateChanged;
        State.Suspend();

        if (m_IsSubscribedToGUICallbacksAndEvents
            && m_ContainerElementsInitialized
            && IsDisplayingTarget())
        {
            UnsubscribeFromGUICallbacksAndEvents();
        }

        EditorApplication.update -= OneTimeEditorApplicationUpdate;
        ClearClipFieldProgressBars();
    }

    void OnWillSaveAssets(IEnumerable<string> paths)
    {
        // If there is no target we are in day 0 state.
        if (State.AudioContainer == null)
            return;

        foreach (var path in paths)
            if (path == State.TargetPath)
            {
                SetTitle();
                return;
            }
    }

    void OnAssetsImported(IEnumerable<string> paths)
    {
        // If there is no target we are in day 0 state.
        if (State.AudioContainer == null)
            return;

        foreach (var path in paths)
            if (path == State.TargetPath)
            {
                State.SerializedObject.Update();
                OnTargetChanged(this, EventArgs.Empty);
                return;
            }
    }

    void OnAssetsDeleted(IEnumerable<string> paths)
    {
        // The target reference will already be invalid at this point if it's been deleted.
        if (State.AudioContainer != null)
            return;

        // ...but we still have the target path available for the check.
        foreach (var path in paths)
            if (path == State.TargetPath)
            {
                State.Reset();
                OnTargetChanged(this, EventArgs.Empty);
                return;
            }
    }

    class AudioContainerModificationProcessor : AssetModificationProcessor
    {
        /// <summary>
        /// Handles save of AudioRandomContainer assets
        /// and relays it to AudioContainerWindow,
        /// removing the asterisk in the window tab label.
        /// </summary>
        static string[] OnWillSaveAssets(string[] paths)
        {
            if (Instance != null)
                Instance.OnWillSaveAssets(paths);

            return paths;
        }
    }

    class AudioContainerPostProcessor : AssetPostprocessor
    {
        /// <summary>
        /// Handles import and deletion of AudioRandomContainer assets
        /// and relays it to AudioContainerWindow,
        /// refreshing or clearing the window content.
        /// </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Instance == null)
                return;

            if (importedAssets.Length > 0)
                Instance.OnAssetsImported(importedAssets);

            if (deletedAssets.Length > 0)
                Instance.OnAssetsDeleted(deletedAssets);
        }
    }

    #endregion
}
