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
using UnityEngine.Audio;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor;

sealed class AudioContainerWindow : EditorWindow
{
    /// <summary>
    /// The cached instance of the window, if it is open.
    /// </summary>
    internal static AudioContainerWindow Instance { get; private set; }

    readonly AudioContainerWindowState m_State = new();

    AudioContainerListDragAndDropManipulator m_DragManipulator;

    Label m_AssetNameLabel;
    Button m_PlayButton;
    VisualElement m_PlayButtonImage;
    Button m_SkipButton;
    VisualElement m_SkipButtonImage;
    Slider m_VolumeSlider;
    FloatField m_VolumeField;
    Button m_VolumeRandomizationButton;
    VisualElement m_VolumeRandomizationButtonImage;
    MinMaxSlider m_VolumeRandomizationRangeSlider;
    Vector2Field m_VolumeRandomizationRangeField;
    Slider m_PitchSlider;
    FloatField m_PitchField;
    Button m_PitchRandomizationButton;
    VisualElement m_PitchRandomizationButtonImage;
    MinMaxSlider m_PitchRandomizationRangeSlider;
    Vector2Field m_PitchRandomizationRangeField;
    ListView m_ClipsListView;
    RadioButtonGroup m_TriggerRadioButtonGroup;
    RadioButtonGroup m_PlaybackModeRadioButtonGroup;
    IntegerField m_AvoidRepeatingLastField;
    RadioButtonGroup m_AutomaticTriggerModeRadioButtonGroup;
    Slider m_TimeSlider;
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
    AudioRandomRangeSliderTracker m_VolumeRandomRangeTracker;
    AudioRandomRangeSliderTracker m_PitchRandomRangeTracker;
    AudioRandomRangeSliderTracker m_TimeRandomRangeTracker;

    Texture2D m_DiceIconOff;
    Texture2D m_DiceIconOn;

    bool m_IsLoading;

    List<AudioContainerElement> m_CachedElements;

    List<AudioContainerElement> CachedElements
    {
        get => m_CachedElements ??= new List<AudioContainerElement>();

        set => m_CachedElements = value;
    }

    [RequiredByNativeCode]
    internal static void CreateAudioRandomContainerWindow()
    {
        var window = GetWindow<AudioContainerWindow>();
        window.Show();
    }

    /// <summary>
    /// Updates the state, which will implicitly refresh the window content if needed.
    /// </summary>
    internal void Refresh()
    {
        m_State.UpdateTarget();
    }

    void OnSkipButtonClicked()
    {
        if (m_State.IsPlaying()) m_State.Skip();
    }

    void OnVolumeRandomizationButtonClicked()
    {
        var newButtonStateString = !m_State.AudioContainer.volumeRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(m_State.AudioContainer, $"Modified Volume Randomization {newButtonStateString} in {m_State.AudioContainer.name}");
        m_State.AudioContainer.volumeRandomizationEnabled = !m_State.AudioContainer.volumeRandomizationEnabled;
    }

    void OnPitchRandomizationButtonClicked()
    {
        var newButtonStateString = !m_State.AudioContainer.pitchRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(m_State.AudioContainer, $"Modified Pitch Randomization {newButtonStateString} in {m_State.AudioContainer.name}");
        m_State.AudioContainer.pitchRandomizationEnabled = !m_State.AudioContainer.pitchRandomizationEnabled;
    }

    void OnCountRandomizationButtonClicked()
    {
        var newButtonStateString = !m_State.AudioContainer.loopCountRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(m_State.AudioContainer, $"Modified Count Randomization {newButtonStateString} in {m_State.AudioContainer.name}");
        m_State.AudioContainer.loopCountRandomizationEnabled = !m_State.AudioContainer.loopCountRandomizationEnabled;
    }

    void OnTimeRandomizationButtonClicked()
    {
        var newButtonStateString = !m_State.AudioContainer.automaticTriggerTimeRandomizationEnabled ? "Enabled" : "Disabled";
        Undo.RecordObject(m_State.AudioContainer, $"Modified Time Randomization {newButtonStateString} in {m_State.AudioContainer.name}");
        m_State.AudioContainer.automaticTriggerTimeRandomizationEnabled = !m_State.AudioContainer.automaticTriggerTimeRandomizationEnabled;
    }

    void SetTitle(bool targetIsDirty)
    {
        var titleString = "Audio Random Container";

        if (targetIsDirty) titleString += "*";

        titleContent = new GUIContent(titleString)
        {
            image = m_DiceIconOff
        };
    }

    void CreateGUI()
    {
        try
        {
            if (m_IsLoading)
            {
                return;
            }

            m_IsLoading = true;

            rootVisualElement.Unbind();
            rootVisualElement.Clear();

            if (m_State.AudioContainer == null)
            {
                return;
            }

            var rootAsset = UIToolkitUtilities.LoadUxml("UXML/Audio/AudioRandomContainer.uxml");
            rootAsset.CloneTree(rootVisualElement);

            var styleSheet = UIToolkitUtilities.LoadStyleSheet("StyleSheets/Audio/AudioRandomContainer.uss");
            rootVisualElement.styleSheets.Add(styleSheet);

            CreateBindings();
            UpdateTransportButtonStates();
        }
        finally
        {
            m_IsLoading = false;
        }
    }

    void OnPauseStateChanged(object sender, EventArgs e)
    {
        UpdateTransportButtonStates();
    }

    void CreateBindings()
    {
        var volumeProperty = m_State.SerializedObject.FindProperty("m_Volume");
        var volumeRandomizationEnabledProperty = m_State.SerializedObject.FindProperty("m_VolumeRandomizationEnabled");
        var volumeRandomizationRangeProperty = m_State.SerializedObject.FindProperty("m_VolumeRandomizationRange");
        var pitchProperty = m_State.SerializedObject.FindProperty("m_Pitch");
        var pitchRandomizationEnabledProperty = m_State.SerializedObject.FindProperty("m_PitchRandomizationEnabled");
        var pitchRandomizationRangeProperty = m_State.SerializedObject.FindProperty("m_PitchRandomizationRange");
        var clipsProperty = m_State.SerializedObject.FindProperty("m_Elements");
        var triggerProperty = m_State.SerializedObject.FindProperty("m_TriggerMode");
        var playbackModeProperty = m_State.SerializedObject.FindProperty("m_PlaybackMode");
        var avoidRepeatingLastProperty = m_State.SerializedObject.FindProperty("m_AvoidRepeatingLast");
        var automaticTriggerModeProperty = m_State.SerializedObject.FindProperty("m_AutomaticTriggerMode");
        var timeProperty = m_State.SerializedObject.FindProperty("m_AutomaticTriggerTime");
        var timeRandomizationEnabledProperty = m_State.SerializedObject.FindProperty("m_AutomaticTriggerTimeRandomizationEnabled");
        var timeRandomizationRangeProperty = m_State.SerializedObject.FindProperty("m_AutomaticTriggerTimeRandomizationRange");
        var loopProperty = m_State.SerializedObject.FindProperty("m_LoopMode");
        var countProperty = m_State.SerializedObject.FindProperty("m_LoopCount");
        var countRandomizationEnabledProperty = m_State.SerializedObject.FindProperty("m_LoopCountRandomizationEnabled");
        var countRandomizationRangeProperty = m_State.SerializedObject.FindProperty("m_LoopCountRandomizationRange");

        m_AssetNameLabel = UIToolkitUtilities.GetChildByName<Label>(rootVisualElement, "asset-name-label");
        m_PlayButton = UIToolkitUtilities.GetChildByName<Button>(rootVisualElement, "play-button");
        m_PlayButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(rootVisualElement, "play-button-image");
        m_SkipButton = UIToolkitUtilities.GetChildByName<Button>(rootVisualElement, "skip-button");
        m_SkipButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(rootVisualElement, "skip-button-image");
        m_VolumeSlider = UIToolkitUtilities.GetChildByName<Slider>(rootVisualElement, "volume-slider");
        m_VolumeField = UIToolkitUtilities.GetChildByName<FloatField>(rootVisualElement, "volume-field");
        m_VolumeRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(rootVisualElement, "volume-randomization-button");
        m_VolumeRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(rootVisualElement, "volume-randomization-button-image");
        m_VolumeRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(rootVisualElement, "volume-randomization-range-slider");
        m_VolumeRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(rootVisualElement, "volume-randomization-range-field");
        m_PitchSlider = UIToolkitUtilities.GetChildByName<Slider>(rootVisualElement, "pitch-slider");
        m_PitchField = UIToolkitUtilities.GetChildByName<FloatField>(rootVisualElement, "pitch-field");
        m_PitchRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(rootVisualElement, "pitch-randomization-button");
        m_PitchRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(rootVisualElement, "pitch-randomization-button-image");
        m_PitchRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(rootVisualElement, "pitch-randomization-range-slider");
        m_PitchRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(rootVisualElement, "pitch-randomization-range-field");
        m_ClipsListView = UIToolkitUtilities.GetChildByName<ListView>(rootVisualElement, "audio-clips-list-view");
        m_TriggerRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(rootVisualElement, "trigger-radio-button-group");
        m_PlaybackModeRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(rootVisualElement, "playback-radio-button-group");
        m_AvoidRepeatingLastField = UIToolkitUtilities.GetChildByName<IntegerField>(rootVisualElement, "avoid-repeating-last-field");
        m_AutomaticTriggerModeRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(rootVisualElement, "trigger-mode-radio-button-group");
        m_TimeSlider = UIToolkitUtilities.GetChildByName<Slider>(rootVisualElement, "time-slider");
        m_TimeField = UIToolkitUtilities.GetChildByName<FloatField>(rootVisualElement, "time-field");
        m_TimeRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(rootVisualElement, "time-randomization-button");
        m_TimeRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(rootVisualElement, "time-randomization-button-image");
        m_TimeRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(rootVisualElement, "time-randomization-range-slider");
        m_TimeRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(rootVisualElement, "time-randomization-range-field");
        m_LoopRadioButtonGroup = UIToolkitUtilities.GetChildByName<RadioButtonGroup>(rootVisualElement, "loop-radio-button-group");
        m_CountField = UIToolkitUtilities.GetChildByName<IntegerField>(rootVisualElement, "count-field");
        m_CountRandomizationButton = UIToolkitUtilities.GetChildByName<Button>(rootVisualElement, "count-randomization-button");
        m_CountRandomizationButtonImage = UIToolkitUtilities.GetChildByName<VisualElement>(rootVisualElement, "count-randomization-button-image");
        m_CountRandomizationRangeSlider = UIToolkitUtilities.GetChildByName<MinMaxSlider>(rootVisualElement, "count-randomization-range-slider");
        m_CountRandomizationRangeField = UIToolkitUtilities.GetChildByName<Vector2Field>(rootVisualElement, "count-randomization-range-field");
        m_AutomaticTriggerModeLabel = UIToolkitUtilities.GetChildByName<Label>(rootVisualElement, "automatic-trigger-mode-label");
        m_LoopLabel = UIToolkitUtilities.GetChildByName<Label>(rootVisualElement, "loop-label");

        m_AssetNameLabel.text = m_State.AudioContainer.name;

        m_VolumeField.formatString = "0.# dB";
        m_VolumeField.isDelayed = true;

        var volumeRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_VolumeRandomizationRangeField, "unity-x-input");
        volumeRandomizationMinField.isDelayed = true;
        volumeRandomizationMinField.label = "";
        volumeRandomizationMinField.formatString = "0.# dB";

        var volumeRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_VolumeRandomizationRangeField, "unity-y-input");
        volumeRandomizationMaxField.isDelayed = true;
        volumeRandomizationMaxField.label = "";
        volumeRandomizationMaxField.formatString = "0.# dB";

        m_PitchField.formatString = "0 ct";
        m_PitchField.isDelayed = true;

        var pitchRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_PitchRandomizationRangeField, "unity-x-input");
        pitchRandomizationMinField.isDelayed = true;
        pitchRandomizationMinField.label = "";
        pitchRandomizationMinField.formatString = "0 ct";

        var pitchRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_PitchRandomizationRangeField, "unity-y-input");
        pitchRandomizationMaxField.isDelayed = true;
        pitchRandomizationMaxField.label = "";
        pitchRandomizationMaxField.formatString = "0 ct";

        m_TimeField.formatString = "0.00 s";
        m_TimeField.isDelayed = true;

        var timeRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_TimeRandomizationRangeField, "unity-x-input");
        timeRandomizationMinField.isDelayed = true;
        timeRandomizationMinField.label = "";
        timeRandomizationMinField.formatString = "0.#";

        var timeRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_TimeRandomizationRangeField, "unity-y-input");
        timeRandomizationMaxField.isDelayed = true;
        timeRandomizationMaxField.label = "";
        timeRandomizationMaxField.formatString = "0.#";

        m_CountField.formatString = "0.#";
        m_CountField.isDelayed = true;

        var countRandomizationMinField = UIToolkitUtilities.GetChildByName<FloatField>(m_CountRandomizationRangeField, "unity-x-input");
        countRandomizationMinField.isDelayed = true;
        countRandomizationMinField.label = "";

        var countRandomizationMaxField = UIToolkitUtilities.GetChildByName<FloatField>(m_CountRandomizationRangeField, "unity-y-input");
        countRandomizationMaxField.isDelayed = true;
        countRandomizationMaxField.label = "";

        m_VolumeSlider.BindProperty(volumeProperty);
        m_VolumeField.BindProperty(volumeProperty);
        m_VolumeRandomizationRangeSlider.BindProperty(volumeRandomizationRangeProperty);
        m_VolumeRandomizationRangeField.BindProperty(volumeRandomizationRangeProperty);
        m_VolumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
        m_VolumeRandomizationRangeSlider.RegisterValueChangedCallback(OnVolumeRandomizationRangeChanged);
        m_VolumeRandomizationRangeField.RegisterValueChangedCallback(OnVolumeRandomizationRangeChanged);

        m_PitchSlider.BindProperty(pitchProperty);
        m_PitchField.BindProperty(pitchProperty);
        m_PitchRandomizationRangeSlider.BindProperty(pitchRandomizationRangeProperty);
        m_PitchRandomizationRangeField.BindProperty(pitchRandomizationRangeProperty);
        m_PitchSlider.RegisterValueChangedCallback(OnPitchChanged);
        m_PitchRandomizationRangeSlider.RegisterValueChangedCallback(OnPitchRandomizationRangeChanged);
        m_PitchRandomizationRangeField.RegisterValueChangedCallback(OnPitchRandomizationRangeChanged);

        m_ClipsListView.BindProperty(clipsProperty);
        m_ClipsListView.TrackPropertyValue(clipsProperty, OnAudioClipListChanged);
        m_ClipsListView.showBorder = true;

        m_TriggerRadioButtonGroup.BindProperty(triggerProperty);
        m_PlaybackModeRadioButtonGroup.BindProperty(playbackModeProperty);
        m_AvoidRepeatingLastField.BindProperty(avoidRepeatingLastProperty);
        m_AvoidRepeatingLastField.isDelayed = true;
        m_AutomaticTriggerModeRadioButtonGroup.BindProperty(automaticTriggerModeProperty);

        m_TimeSlider.BindProperty(timeProperty);
        m_TimeField.BindProperty(timeProperty);
        m_TimeRandomizationRangeSlider.BindProperty(timeRandomizationRangeProperty);
        m_TimeRandomizationRangeField.BindProperty(timeRandomizationRangeProperty);
        m_TimeSlider.RegisterValueChangedCallback(OnTimeChanged);
        m_TimeRandomizationRangeSlider.RegisterValueChangedCallback(OnTimeRandomizationRangeChanged);
        m_TimeRandomizationRangeField.RegisterValueChangedCallback(OnTimeRandomizationRangeChanged);

        m_LoopRadioButtonGroup.BindProperty(loopProperty);
        m_CountField.BindProperty(countProperty);
        m_CountRandomizationRangeSlider.BindProperty(countRandomizationRangeProperty);
        m_CountRandomizationRangeField.BindProperty(countRandomizationRangeProperty);

        m_PlayButton.clicked += OnPlayButtonClicked;
        m_SkipButton.clicked += OnSkipButtonClicked;
        m_VolumeRandomizationButton.clicked += OnVolumeRandomizationButtonClicked;
        m_PitchRandomizationButton.clicked += OnPitchRandomizationButtonClicked;
        m_TimeRandomizationButton.clicked += OnTimeRandomizationButtonClicked;
        m_CountRandomizationButton.clicked += OnCountRandomizationButtonClicked;

        m_VolumeRandomizationButton.TrackPropertyValue(volumeRandomizationEnabledProperty, OnVolumeRandomizationEnabledChanged);
        m_PitchRandomizationButton.TrackPropertyValue(pitchRandomizationEnabledProperty, OnPitchRandomizationEnabledChanged);
        m_TriggerRadioButtonGroup.TrackPropertyValue(triggerProperty, OnTriggerChanged);
        m_PlaybackModeRadioButtonGroup.TrackPropertyValue(playbackModeProperty, OnPlaybackModeChanged);
        m_TimeRandomizationButton.TrackPropertyValue(timeRandomizationEnabledProperty, OnTimeRandomizationEnabledChanged);
        m_LoopRadioButtonGroup.TrackPropertyValue(loopProperty, OnLoopChanged);
        m_CountRandomizationButton.TrackPropertyValue(countRandomizationEnabledProperty, OnCountRandomizationEnabledChanged);

        OnVolumeRandomizationEnabledChanged(volumeRandomizationEnabledProperty);
        OnPitchRandomizationEnabledChanged(pitchRandomizationEnabledProperty);
        OnTriggerChanged(triggerProperty);
        OnPlaybackModeChanged(playbackModeProperty);
        OnTimeRandomizationEnabledChanged(timeRandomizationEnabledProperty);
        OnLoopChanged(loopProperty);
        OnCountRandomizationEnabledChanged(countRandomizationEnabledProperty);

        UpdateTransportButtonStates();

        m_ClipsListView.fixedItemHeight = 24;
        m_ClipsListView.itemsAdded += OnListItemsAdded;
        m_ClipsListView.itemsRemoved += OnListItemsRemoved;
        m_ClipsListView.itemIndexChanged += OnItemListIndexChanged;
        m_ClipsListView.makeItem = OnMakeListItem;
        m_ClipsListView.bindItem = OnBindListItem;

        m_ClipsListView.CreateDragAndDropController();

        var skipIcon = UIToolkitUtilities.LoadIcon("icon_next");
        m_SkipButtonImage.style.backgroundImage = new StyleBackground(skipIcon);

        m_DragManipulator = new AudioContainerListDragAndDropManipulator(rootVisualElement);
        m_DragManipulator.addAudioClipsDelegate += OnAudioClipDrag;

        rootVisualElement.TrackSerializedObjectValue(m_State.SerializedObject, OnSerializedObjectChanged);

        m_VolumeRandomRangeTracker = AudioRandomRangeSliderTracker.Create(m_VolumeSlider, m_State.AudioContainer.volumeRandomizationRange);
        m_PitchRandomRangeTracker = AudioRandomRangeSliderTracker.Create(m_PitchSlider, m_State.AudioContainer.pitchRandomizationRange);
        m_TimeRandomRangeTracker = AudioRandomRangeSliderTracker.Create(m_TimeSlider, m_State.AudioContainer.automaticTriggerTimeRandomizationRange);

        EditorApplication.update += OnEditorApplicationUpdate;
    }

    void OnEditorApplicationUpdate()
    {
        m_ClipsListView.reorderable = true;
        m_ClipsListView.reorderMode = ListViewReorderMode.Animated;
        EditorApplication.update -= OnEditorApplicationUpdate;
    }

    void OnSerializedObjectChanged(SerializedObject obj)
    {
        SetTitle(m_State.IsDirty());
    }

    void OnPlaybackStateChanged(object sender, EventArgs e)
    {
        UpdateTransportButtonStates();
    }

    void UpdateTransportButtonStates()
    {
        var editorIsPaused = EditorApplication.isPaused;

        m_PlayButton?.SetEnabled(m_State.IsReadyToPlay() && !editorIsPaused);
        m_SkipButton?.SetEnabled(m_State.IsPlaying() && m_State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic && !editorIsPaused);

        var image =
            m_State.IsPlaying()
                ? UIToolkitUtilities.LoadIcon("icon_stop")
                : UIToolkitUtilities.LoadIcon("icon_play");

        m_PlayButtonImage.style.backgroundImage = new StyleBackground(image);
    }

    void OnEnable()
    {
        Instance = this;
        m_State.OnTargetChanged += OnTargetChanged;
        m_State.OnPlaybackStateChanged += OnPlaybackStateChanged;
        m_State.OnPauseStateChanged += OnPauseStateChanged;
        m_DiceIconOff = EditorGUIUtility.IconContent("AudioRandomContainer On Icon").image as Texture2D;
        m_DiceIconOn = EditorGUIUtility.IconContent("AudioRandomContainer Icon").image as Texture2D;
        SetTitle(m_State.IsDirty());
    }

    void OnDisable()
    {
        Instance = null;

        if (m_PlayButton != null) m_PlayButton.clicked -= OnPlayButtonClicked;
        if (m_SkipButton != null) m_SkipButton.clicked -= OnSkipButtonClicked;

        m_VolumeRandomizationRangeSlider?.UnregisterValueChangedCallback(OnVolumeRandomizationRangeChanged);
        m_VolumeRandomizationRangeField?.UnregisterValueChangedCallback(OnVolumeRandomizationRangeChanged);

        m_PitchRandomizationRangeSlider?.UnregisterValueChangedCallback(OnPitchRandomizationRangeChanged);
        m_PitchRandomizationRangeField?.UnregisterValueChangedCallback(OnPitchRandomizationRangeChanged);

        m_TimeRandomizationRangeSlider?.UnregisterValueChangedCallback(OnTimeRandomizationRangeChanged);
        m_TimeRandomizationRangeField?.UnregisterValueChangedCallback(OnTimeRandomizationRangeChanged);

        m_State.OnDestroy();

        CachedElements.Clear();
    }

    void OnBecameInvisible()
    {
        m_State.Stop();
    }

    void OnPlayButtonClicked()
    {
        if (m_State.IsPlaying())
            m_State.Stop();
        else
            m_State.Play();

        UpdateTransportButtonStates();
    }

    void OnVolumeChanged(ChangeEvent<float> evt)
    {
        m_VolumeRandomRangeTracker.SetRange(m_State.AudioContainer.volumeRandomizationRange);
    }

    void OnVolumeRandomizationRangeChanged(ChangeEvent<Vector2> evt)
    {
        // Have to clamp immediately here to avoid UI jitter because the min-max slider cannot clamp before updating the property
        var newValue = evt.newValue;

        if (newValue.x > 0)
        {
            newValue.x = 0;
        }

        if (newValue.y < 0)
        {
            newValue.y = 0;
        }

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

    void OnPitchChanged(ChangeEvent<float> evt)
    {
        m_PitchRandomRangeTracker.SetRange(m_State.AudioContainer.pitchRandomizationRange);
    }

    void OnPitchRandomizationRangeChanged(ChangeEvent<Vector2> evt)
    {
        // Have to clamp immediately here to avoid UI jitter because the min-max slider cannot clamp before updating the property
        var newValue = evt.newValue;

        if (newValue.x > 0)
        {
            newValue.x = 0;
        }

        if (newValue.y < 0)
        {
            newValue.y = 0;
        }

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

    void OnTimeChanged(ChangeEvent<float> evt)
    {
        m_TimeRandomRangeTracker.SetRange(m_State.AudioContainer.automaticTriggerTimeRandomizationRange);
    }

    void OnTimeRandomizationRangeChanged(ChangeEvent<Vector2> evt)
    {
        // Have to clamp immediately here to avoid UI jitter because the min-max slider cannot clamp before updating the property
        var newValue = evt.newValue;

        if (newValue.x > 0)
        {
            newValue.x = 0;
        }

        if (newValue.y < 0)
        {
            newValue.y = 0;
        }

        m_TimeRandomRangeTracker.SetRange(newValue);
    }

    void OnTimeRandomizationEnabledChanged(SerializedProperty property)
    {
        if (property.boolValue
            && m_State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic)
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

    void OnCountRandomizationEnabledChanged(SerializedProperty property)
    {
        if (property.boolValue
            && m_State.AudioContainer.loopMode != AudioRandomContainerLoopMode.Infinite
            && m_State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic)
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

    void OnAudioClipListChanged(SerializedProperty property)
    {
        UpdateTransportButtonStates();
        CachedElements = m_State.AudioContainer.elements.ToList();
    }

    void OnPlaybackModeChanged(SerializedProperty property)
    {
        m_AvoidRepeatingLastField.SetEnabled(property.intValue == (int)AudioRandomContainerPlaybackMode.Random);
    }

    void OnTriggerChanged(SerializedProperty property)
    {
        var enabled = property.intValue == (int)AudioRandomContainerTriggerMode.Automatic;

        m_AutomaticTriggerModeRadioButtonGroup.SetEnabled(enabled);
        m_TimeSlider.SetEnabled(enabled);
        m_TimeField.SetEnabled(enabled);
        m_LoopRadioButtonGroup.SetEnabled(enabled);
        m_AutomaticTriggerModeLabel.SetEnabled(enabled);
        m_LoopLabel.SetEnabled(enabled);
        m_TimeRandomizationButton.SetEnabled(enabled);
        m_CountRandomizationButton.SetEnabled(enabled);

        var loopProperty = m_State.SerializedObject.FindProperty("m_LoopMode");
        OnLoopChanged(loopProperty);

        var timeRandomizationEnabledProperty = m_State.SerializedObject.FindProperty("m_AutomaticTriggerTimeRandomizationEnabled");
        OnTimeRandomizationEnabledChanged(timeRandomizationEnabledProperty);
    }

    void OnLoopChanged(SerializedProperty property)
    {
        var enabled = property.intValue != (int)AudioRandomContainerLoopMode.Infinite && m_State.AudioContainer.triggerMode == AudioRandomContainerTriggerMode.Automatic;

        m_CountField.SetEnabled(enabled);
        m_CountRandomizationRangeSlider.SetEnabled(enabled);
        m_CountRandomizationRangeField.SetEnabled(enabled);
        m_CountRandomizationButton.SetEnabled(enabled);

        var countRandomizationEnabledProperty = m_State.SerializedObject.FindProperty("m_LoopCountRandomizationEnabled");
        OnCountRandomizationEnabledChanged(countRandomizationEnabledProperty);
    }

    void OnTargetChanged(object sender, EventArgs e)
    {
        SetTitle(m_State.IsDirty());
        CreateGUI();

        if (m_State.AudioContainer != null)
        {
            CachedElements = m_State.AudioContainer.elements.ToList();
        }
    }

    static VisualElement OnMakeListItem()
    {
        return UIToolkitUtilities.LoadUxml("UXML/Audio/AudioContainerElement.uxml").Instantiate();
    }

    void OnBindListItem(VisualElement element, int index)
    {
        var listElement = m_State.AudioContainer.elements[index];

        if (listElement == null) return;

        var serializedObject = new SerializedObject(listElement);

        var enabledProperty = serializedObject.FindProperty("m_Enabled");
        var audioClipProperty = serializedObject.FindProperty("m_AudioClip");
        var volumeProperty = serializedObject.FindProperty("m_Volume");

        var enabledToggle = (Toggle)element.Query<Toggle>("enabled-toggle");
        var audioClipField = (ObjectField)element.Query<ObjectField>("audio-clip-field");
        var volumeField = (FloatField)element.Query<FloatField>("volume-field");
        volumeField.formatString = "0.# dB";

        audioClipField.objectType = typeof(AudioClip);
        audioClipField.RegisterCallback<DragPerformEvent>(evt => { evt.StopPropagation(); });

        enabledToggle.BindProperty(enabledProperty);
        audioClipField.BindProperty(audioClipProperty);
        volumeField.BindProperty(volumeProperty);

        enabledToggle.TrackPropertyValue(enabledProperty, OnElementEnabledToggleChanged);
        audioClipField.TrackPropertyValue(audioClipProperty, OnElementAudioClipChanged);
        volumeField.TrackPropertyValue(volumeProperty, OnElementPropertyChanged);
    }

    void OnElementAudioClipChanged(SerializedProperty property)
    {
        OnElementPropertyChanged(property);
        UpdateTransportButtonStates();
        m_State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
    }

    void OnElementEnabledToggleChanged(SerializedProperty property)
    {
        OnElementPropertyChanged(property);
        UpdateTransportButtonStates();

        // Changing a property on the ListElement subasset does not call CheckConsistency on the main Asset
        // So quickly flip the values to force an update. :(
        var last = m_State.AudioContainer.avoidRepeatingLast;
        m_State.AudioContainer.avoidRepeatingLast = -1;
        m_State.AudioContainer.avoidRepeatingLast = last;

        if (m_State.IsPlaying())
        {
            m_State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
        }
    }

    void OnElementPropertyChanged(SerializedProperty property)
    {
        EditorUtility.SetDirty(m_State.AudioContainer);
        SetTitle(true);
    }

    void OnListItemsAdded(IEnumerable<int> indices)
    {
        var elements = m_State.AudioContainer.elements.ToList();

        foreach (var index in indices.Reverse())
        {
            var element = new AudioContainerElement
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            elements[index] = element;
            AssetDatabase.AddObjectToAsset(element, m_State.AudioContainer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(element, out var guid, out long localId);
            element.name = $"AudioContainerElement_{{{localId}}}";
        }

        m_State.AudioContainer.elements = elements.ToArray();
    }

    void OnListItemsRemoved(IEnumerable<int> indices)
    {
        foreach (var index in indices)
        {
            AssetDatabase.RemoveObjectFromAsset(CachedElements[index]);
        }

        m_State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
    }

    void OnItemListIndexChanged(int oldIndex, int newIndex)
    {
        m_ClipsListView.Rebuild();
        m_State.AudioContainer.NotifyObservers(AudioRandomContainer.ChangeEventType.List);
    }

    void OnAudioClipDrag(List<AudioClip> audioClips)
    {
        var elements = m_State.AudioContainer.elements.ToList();

        foreach (var audioClip in audioClips)
        {
            var element = new AudioContainerElement
            {
                audioClip = audioClip,
                hideFlags = HideFlags.HideInHierarchy
            };
            elements.Add(element);
            AssetDatabase.AddObjectToAsset(element, m_State.AudioContainer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(element, out var guid, out long localId);
            element.name = $"{audioClip.name}_{{{localId}}}";
        }

        m_State.AudioContainer.elements = elements.ToArray();
    }

    void OnWillSaveAssets(IEnumerable<string> paths)
    {
        if (m_State.AudioContainer == null)
        {
            return;
        }

        var currentSelectionPath = AssetDatabase.GetAssetPath(m_State.AudioContainer);

        foreach (var path in paths)
        {
            if (path == currentSelectionPath)
            {
                SetTitle(false);
                return;
            }
        }
    }

    void OnAssetsImported(IEnumerable<string> paths)
    {
        if (m_State.AudioContainer == null)
        {
            return;
        }

        foreach (var path in paths)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AudioRandomContainer) &&
                AssetDatabase.GetMainAssetInstanceID(path) == m_State.AudioContainer.GetInstanceID())
            {
                m_State.SerializedObject.Update();
                OnTargetChanged(this, EventArgs.Empty);
            }
        }
    }

    void OnAssetsDeleted(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (path == m_State.TargetPath)
            {
                m_State.Reset();
                SetTitle(false);
                CreateGUI();
                CachedElements.Clear();
                break;
            }
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
            {
                Instance.OnWillSaveAssets(paths);
            }

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
            {
                return;
            }

            if (importedAssets.Length > 0)
            {
                Instance.OnAssetsImported(importedAssets);
            }

            if (deletedAssets.Length > 0)
            {
                Instance.OnAssetsDeleted(deletedAssets);
            }
        }
    }
}
