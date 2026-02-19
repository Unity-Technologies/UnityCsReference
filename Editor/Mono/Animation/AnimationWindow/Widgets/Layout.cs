// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using UnityObject = UnityEngine.Object;

using UnityEditor.Animations.AnimationWindow.TimelineFoundation;
using UnityEditor.AnimationWindowBuiltin;
using UnityEditor.Experimental;
using UnityEditor.UIElements;
using UnityEngine.Playables;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    class Layout : VisualElement, IDisposable
    {
        const string k_TemplatePath = "UXML/Animation/AnimationWindow.uxml";

        const string k_AnimationEditor = "animation-editor";
        const string k_AnimationKeyframeHeader = "animation-keyframeHeader";
        const string k_AnimationPropertyHeader = "animation-propertyHeader";
        const string k_AnimationControls = "animation-controls";
        const string k_AnimationTimeArea = "animation-timeArea";

        const string k_AnimationContentOverlay = "animation-contentsOverlay";

        const string k_AnimationLinkWithSequencerButton = "animation-linkWithSequencerButton";

        const string k_AnimationClipDropdown = "animation-clipDropdown";
        const string k_AnimationFrameRate = "animation-frameRate";

        const string k_AnimationPreviewButton = "animation-previewButton";
        const string k_AnimationRecordButton = "animation-recordButton";

        const string k_AnimationAddPropertyButton = "animation-addPropertyButton";
        const string k_AnimationAddKeyframeButton = "animation-addKeyframeButton";
        const string k_AnimationAddEventButton = "animation-addEventButton";
        const string k_AnimationFilterBySelectionToggle = "animation-filterBySelectionToggle";

        const string k_AnimationApplyButton = "animation-applyButton";
        const string k_AnimationRevertButton = "animation-revertButton";

        const string k_AnimationContentSwitcherButtons = "animation-contentSwitcherButtons";

        const string k_ControlsColumnResizer = "controls-column-resizer";

        const string k_AnimationOnboarding = "animation-onboarding";

        static readonly CustomStyleProperty<float> k_PreviewColorMultiplier = new CustomStyleProperty<float>("--preview-color-multiplier");
        static readonly CustomStyleProperty<float> k_RecordColorMultiplier = new CustomStyleProperty<float>("--record-color-multiplier");

        static string s_AnimatorOptimizedText = L10n.Tr("Editing and playback of animations on optimized game object hierarchy is not supported.\nPlease select a game object that does not have 'Optimize Game Objects' applied.");
        static string s_AnimatorAndAnimationClipText = L10n.Tr("an Animator and an Animation Clip");
        static string s_AnimationClipText = L10n.Tr("an Animation Clip");
        static string s_FormatIsMissingText = L10n.Tr("To begin animating {0}, create {1}.");
        static string s_NoAnimatableObjectSelectedText = L10n.Tr("No animatable object selected.");

        static string s_RecordContentTooltip = L10n.Tr("Enable/disable keyframe recording mode.");
        static string s_PreviewContentTooltip = L10n.Tr("Enable/disable scene preview mode.");

        static string s_RevertContentTooltip = L10n.Tr("Discard changes made to imported animation.");
        static string s_ApplyContentTooltip = L10n.Tr("Apply changes made to imported animation.");
        static string s_AddKeyframeContentTooltip = L10n.Tr("Add keyframe.");
        static string s_AddEventContentTooltip = L10n.Tr("Add event.");
        static string s_FilterBySelectionContentTooltip = L10n.Tr("Filter by selection.");
        static string s_SequencerLinkContentTooltip = L10n.Tr("Animation Window is linked to Timeline Editor.  Press to Unlink.");

        const float k_LeftMargin = 40f;
        const float k_RightMargin = 40f;

        class DopesheetButton : IToggleButtonItem
        {
            public string Name => L10n.Tr("Dopesheet");
        }

        class CurveEditorButton : IToggleButtonItem
        {
            public string Name => L10n.Tr("Curves");
        }

        AnimEditor m_AnimEditor;

        VisualElement m_AnimationEditor;
        VisualElement m_AnimationKeyframeHeader;
        VisualElement m_AnimationPropertyHeader;
        VisualElement m_AnimationControls;
        ToolbarToggle m_LinkWithSequencerButton;
        ClipDropdownField m_ClipDropdownField;
        VisualElement m_AnimationClipFrameRate;
        IntegerField m_AnimationClipFrameRateField;
        HierarchyElement m_HierarchyElement;
        AnimationEventTimelineElement m_AnimationEventTimeline;
        TimelineFoundation.TimeArea m_TimeArea;

        VisualElement m_OnboardingPanel;
        Label m_OnboardingPanelLabel;
        Button m_OnboardingPanelButton;
        DopeSheetElement m_DopeSheetElement;
        CurveEditorElement m_CurveEditorElement;

        Button m_AddPropertyButton;
        Button m_AddKeyframeButton;
        Button m_AddEventButton;
        ToolbarToggle m_FilterBySelectionToggle;

        ToggleButtonStrip m_ContentSwitcherButtons;

        ToolbarToggle m_PreviewButton;
        ToolbarToggle m_RecordButton;
        PlayControls m_PlayControls;
        OptionsButton m_OptionsButton;

        Button m_ApplyButton;
        Button m_RevertButton;

        CanvasManager m_Canvas;

        OverlayManager m_ControlsOverlay;
        CanvasOverlayManager m_CanvasOverlayManager;

        VisualElement m_HeaderResizeElement;
        HeaderResizeManipulator m_HeaderResizeManipulator;

        PlayHeadOverlay m_PlayHeadOverlay;
        PlayHeadOverlay m_DurationOverlay;
        TimeDragManipulator m_PlayHeadDragManipulator;
        TimeDragManipulator m_DurationDragManipulator;
        TimeDragManipulator m_TimeAreaDragManipulator;

        EventsOverlay m_EventsOverlay;

        AnimationWindowState state => m_AnimEditor.state;
        IAnimationWindowSelectionItem selection => state?.selection;

        enum MainContentState
        {
            OnboardingPanel,
            DopeSheetEditor,
            CurveEditor
        };

        public Layout(AnimEditor animEditor)
        {
            m_AnimEditor = animEditor;

            var template = EditorResources.Load<UnityObject>(k_TemplatePath) as VisualTreeAsset;

            template.CloneTree(this);

            AddStyleSheetPath("StyleSheets/Animation/AnimationWindow.uss");
            AddStyleSheetPath("StyleSheets/TimelineFoundation/Common.uss");

            if (EditorGUIUtility.isProSkin)
            {
                AddStyleSheetPath("StyleSheets/Animation/AnimationWindowDark.uss");
                AddStyleSheetPath("StyleSheets/TimelineFoundation/CommonDark.uss");
            }
            else
            {
                AddStyleSheetPath("StyleSheets/Animation/AnimationWindowLight.uss");
                AddStyleSheetPath("StyleSheets/TimelineFoundation/CommonLight.uss");
            }

            InitPlayHead();
            InitToolbar();
            InitAnimationContent();
            InitControlsResize();

            state.onRefresh += OnRefresh;
            OnRefresh();
        }

        void InitPlayHead()
        {
            m_ControlsOverlay = this.Q<OverlayManager>();
            m_CanvasOverlayManager = this.Q<CanvasOverlayManager>(className: k_AnimationContentOverlay);

            m_Canvas = new CanvasManager(m_CanvasOverlayManager, state);
            m_CanvasOverlayManager.canvas = m_Canvas;

            m_DurationOverlay = new PlayHeadOverlay();
            m_DurationOverlay.name = "preview-duration-overlay";
            m_CanvasOverlayManager.AddOverlay(m_DurationOverlay);

            m_DurationDragManipulator = new TimeDragManipulator(m_Canvas);
            SetupTimeDragManipulator(m_DurationDragManipulator);
            m_DurationOverlay.AddManipulator(m_DurationDragManipulator);

            m_PlayHeadOverlay = new PlayHeadOverlay(PickingMode.Position);
            m_CanvasOverlayManager.AddOverlay(m_PlayHeadOverlay);

            m_PlayHeadDragManipulator = new TimeDragManipulator(m_Canvas);
            SetupTimeDragManipulator(m_PlayHeadDragManipulator);
            m_PlayHeadOverlay.AddManipulator(m_PlayHeadDragManipulator);

            m_EventsOverlay = new EventsOverlay();
            m_CanvasOverlayManager.AddOverlay(m_EventsOverlay);
        }

        void InitToolbar()
        {
            m_PreviewButton = this.Q<ToolbarToggle>(className: k_AnimationPreviewButton);
            m_PreviewButton.tooltip = s_PreviewContentTooltip;
            m_PreviewButton.RegisterValueChangedCallback(evt => state.previewing = evt.newValue);

            m_RecordButton = this.Q<ToolbarToggle>(className: k_AnimationRecordButton);
            m_RecordButton.tooltip = s_RecordContentTooltip;
            m_RecordButton.RegisterValueChangedCallback(evt => state.recording = evt.newValue);

            var playControlsContainer = this.Q<VisualElement>("timeline-previewToolbar");
            m_PlayControls = new PlayControls();
            m_PlayControls.TimeFormat = state.timeFormat;
            m_PlayControls.Initialize(state);

            playControlsContainer.Add(m_PlayControls);

            m_OptionsButton = this.Q<OptionsButton>();
            m_OptionsButton.Initialize(
                m_AnimEditor,
                () => m_PlayControls.TimeFormat,
                (timeFormat) =>
                {
                    m_PlayControls.TimeFormat = timeFormat;
                    m_TimeArea.TimeFormat = timeFormat;
                    state.timeFormat = timeFormat;
                },
                () => true);
        }

        void InitAnimationContent()
        {
            m_AnimationEditor = this.Q(className: k_AnimationEditor);
            m_AnimationKeyframeHeader = this.Q(className: k_AnimationKeyframeHeader);
            m_AnimationPropertyHeader = this.Q(className: k_AnimationPropertyHeader);
            m_AnimationControls = this.Q(className: k_AnimationControls);

            m_OnboardingPanel = this.Q<VisualElement>(className: k_AnimationOnboarding);
            m_OnboardingPanelLabel = m_OnboardingPanel.Q<Label>();
            m_OnboardingPanelButton = m_OnboardingPanel.Q<Button>();
            m_OnboardingPanelButton.clicked += () =>
            {
                if (selection.InitializeSelection())
                    AnimationWindowUtility.ControllerChanged();
            };

            m_DopeSheetElement = this.Q<DopeSheetElement>(className: DopeSheetElement.ussClassName);
            m_CurveEditorElement = this.Q<CurveEditorElement>(className: CurveEditorElement.ussClassName);

            m_HierarchyElement = this.Q<HierarchyElement>(className: HierarchyElement.ussClassName);
            m_AnimationEventTimeline = this.Q<AnimationEventTimelineElement>(className: AnimationEventTimelineElement.ussClassName);
            m_TimeArea = this.Q<TimelineFoundation.TimeArea>(className: k_AnimationTimeArea);
            m_TimeArea.TimeFormat = state.timeFormat;

            m_AddPropertyButton = this.Q<Button>(className: k_AnimationAddPropertyButton);
            m_AddPropertyButton.clicked += () =>
                AddCurvesPopup.ShowAtPosition(m_AddPropertyButton.worldBound, state);
            m_AddPropertyButton.SetEnabled(m_AnimEditor.selection.canAddCurves);

            m_AddKeyframeButton = this.Q<Button>(className: k_AnimationAddKeyframeButton);
            m_AddKeyframeButton.tooltip = s_AddKeyframeContentTooltip;
            m_AddKeyframeButton.clicked += () =>
            {
                m_AnimEditor.SaveCurveEditorKeySelection();
                var keyTime = AnimationKeyTime.Time(state.currentTime, state.frameRate);
                AnimationWindowUtility.AddSelectedKeyframes(state, keyTime);
                m_AnimEditor.UpdateSelectedKeysToCurveEditor();
            };
            m_AddKeyframeButton.SetEnabled(false);

            m_AddEventButton = this.Q<Button>(className: k_AnimationAddEventButton);
            m_AddEventButton.tooltip = s_AddEventContentTooltip;
            m_AddEventButton.clicked += () =>
            {
                if (m_AnimEditor.selection.clip is not UnityEditor.AnimationWindowBuiltin.AnimationWindowClip clip)
                    return;

                AnimationEventTimeLine.AddEvent(
                    state.currentTime,
                    selection.rootGameObject,
                    clip.animationClip);
            };
            m_AddKeyframeButton.SetEnabled(false);

            m_LinkWithSequencerButton = this.Q<ToolbarToggle>(className: k_AnimationLinkWithSequencerButton);
            m_LinkWithSequencerButton.tooltip = s_SequencerLinkContentTooltip;
            m_LinkWithSequencerButton.RegisterValueChangedCallback(_ =>
            {
                state.linkedWithSequencer = false;
                state.selection = new FallbackSelectionItem();
            });
            m_LinkWithSequencerButton.EnableInClassList(k_AnimationLinkWithSequencerButton + "__hidden", true);

            m_ClipDropdownField = this.Q<ClipDropdownField>(className: k_AnimationClipDropdown);
            m_ClipDropdownField.Initialize(state);

            m_AnimationClipFrameRate = this.Q<VisualElement>(className: k_AnimationFrameRate);

            m_AnimationClipFrameRateField = m_AnimationClipFrameRate.Q<IntegerField>();
            m_AnimationClipFrameRateField.RegisterValueChangedCallback(changeEvent =>
            {
                state.frameRate = changeEvent.newValue;
            });

            m_FilterBySelectionToggle = this.Q<ToolbarToggle>(className: k_AnimationFilterBySelectionToggle);
            m_FilterBySelectionToggle.tooltip = s_FilterBySelectionContentTooltip;
            m_FilterBySelectionToggle.RegisterValueChangedCallback(_ =>
            {
                state.filterBySelection =  !state.filterBySelection;
            });
            m_FilterBySelectionToggle.SetEnabled(false);

            m_ApplyButton = this.Q<Button>(className: k_AnimationApplyButton);
            m_ApplyButton.tooltip = s_ApplyContentTooltip;
            m_ApplyButton.clicked += () =>
            {
                state.selection?.SaveChanges();
            };
            m_ApplyButton.EnableInClassList(k_AnimationApplyButton + "__hidden", true);

            m_RevertButton = this.Q<Button>(className: k_AnimationRevertButton);
            m_RevertButton.tooltip = s_RevertContentTooltip;
            m_RevertButton.clicked += () =>
            {
                state.ClearSelections();
                state.selection?.DiscardChanges();
            };
            m_RevertButton.EnableInClassList(k_AnimationRevertButton + "__hidden", true);

            m_ContentSwitcherButtons = this.Q<ToggleButtonStrip>(className: k_AnimationContentSwitcherButtons);

            var contentSwitcherButtonItems = new IToggleButtonItem[] { new DopesheetButton(), new CurveEditorButton() };

            m_ContentSwitcherButtons.items = contentSwitcherButtonItems;
            m_ContentSwitcherButtons.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is DopesheetButton)
                {
                    m_AnimEditor.SwitchToDopeSheetEditor();
                }
                else if (evt.newValue is CurveEditorButton)
                {
                    m_AnimEditor.SwitchToCurveEditor();
                }
            });

            m_ContentSwitcherButtons.SetValueWithoutNotify(contentSwitcherButtonItems[0]);

            m_DopeSheetElement.Initialize(m_AnimEditor);
            m_CurveEditorElement.Initialize(m_AnimEditor);

            m_HierarchyElement.Initialize(m_AnimEditor);
            m_AnimationEventTimeline.Initialize(m_AnimEditor);

            m_TimeAreaDragManipulator = new TimeDragManipulator(m_Canvas);
            SetupTimeDragManipulator(m_TimeAreaDragManipulator);
            m_TimeArea.AddManipulator(m_TimeAreaDragManipulator);
        }

        void InitControlsResize()
        {
            m_HeaderResizeElement = new VisualElement();
            m_HeaderResizeManipulator = new HeaderResizeManipulator(m_HeaderResizeElement);

            m_AnimationEditor.Add(m_HeaderResizeElement);
            m_HeaderResizeElement.AddToClassList(k_ControlsColumnResizer);
            m_HeaderResizeManipulator.OnDrag += OnControlsWidthManipulatorDragged;
        }

        void SetupTimeDragManipulator(TimeDragManipulator manip)
        {
            manip.StartDrag += _ => m_PlayHeadOverlay.SetTooltipDisplayState(true);
            manip.EndDrag += _ => m_PlayHeadOverlay.SetTooltipDisplayState(false);
            manip.SetTime += time =>
            {
                var newTime = AnimationKeyTime.Time((float)time, state.frameRate);
                state.currentFrame = newTime.frame;
            };
        }

        void OnControlsWidthManipulatorDragged(float delta)
        {
            var width = m_AnimationKeyframeHeader.resolvedStyle.width;
            var minWidth = m_AnimationKeyframeHeader.resolvedStyle.minWidth.value;

            float newWidth = Mathf.Max(minWidth, width + delta);
            float adjustedDelta = newWidth - width;

            var rect = state.timeArea.rect;
            rect.width -= adjustedDelta;

            state.timeArea.rect = rect;
            state.timeArea.SetTickMarkerRanges();

            m_AnimationKeyframeHeader.style.width = newWidth;
            m_AnimationPropertyHeader.style.width = newWidth;
            m_AnimationControls.style.width = newWidth;
            m_ControlsOverlay.style.width = newWidth;
            m_HeaderResizeElement.style.left = newWidth;
        }

        public void Dispose()
        {
            state.onRefresh -= OnRefresh;
            m_PlayControls.Dispose();
            m_ClipDropdownField.Dispose();
            m_OptionsButton.Dispose();

            m_AnimEditor.OnDisable();
            UnityObject.DestroyImmediate(m_AnimEditor);

        }

        public void Update()
        {
            if (!customStyle.TryGetValue(k_PreviewColorMultiplier, out var previewColorMultiplier))
                previewColorMultiplier = 1f;

            if (!customStyle.TryGetValue(k_RecordColorMultiplier, out var recordColorMultiplier))
                recordColorMultiplier = 1f;

            var mainContentState = MainContentState.DopeSheetEditor;
            if (state.disabled) mainContentState = MainContentState.OnboardingPanel;
            else if (state.showCurveEditor) mainContentState = MainContentState.CurveEditor;

            // Update play controls
            if (m_PreviewButton.value != state.previewing)
                m_PreviewButton.SetValueWithoutNotify(state.previewing);

            if (m_RecordButton.value != state.recording)
                m_RecordButton.SetValueWithoutNotify(state.recording);

            m_RecordButton.style.backgroundColor =
                state.recording || (state.canRecord && m_RecordButton.hasHoverPseudoState) ?
                    AnimationMode.recordedPropertyColor.RGBMultiplied(recordColorMultiplier) : StyleKeyword.Null;

            m_PlayControls.Update();

            DiscreteTime currentTime = state.time;
            if (m_PlayHeadOverlay.time != currentTime)
                m_PlayHeadOverlay.time = currentTime;

            m_EventsOverlay.Set(m_AnimEditor.eventToolTipText, m_AnimEditor.eventToolTipPosition);

            // Update clip frame rate
            m_AnimationClipFrameRate.EnableInClassList(k_AnimationFrameRate + "__hidden", !state.showFrameRate);

            // Update time area display range
            float xmin, xmax;
            if (mainContentState == MainContentState.OnboardingPanel)
            {
                var width = m_TimeArea.layout.width;
                var widthInsideMargins = width - k_LeftMargin - k_RightMargin;

                xmin = (CanvasTransform.foundationCanvasPixelsBeforeZero - k_LeftMargin) / widthInsideMargins;
                xmax = (width - k_LeftMargin) / widthInsideMargins;
            }
            else
            {
                xmin = state.PixelToTime(CanvasTransform.foundationCanvasPixelsBeforeZero);
                xmax = Mathf.Max(xmin, state.PixelToTime(m_TimeArea.layout.width));
                xmax = float.IsNaN(xmax) ? xmin : xmax;
            }

            var displayRange = new TimeRange(xmin, xmax);

            if (m_TimeArea.DisplayRange != displayRange)
            {
                m_TimeArea.SetDisplayRange(displayRange);
                m_CanvasOverlayManager.UpdateOverlays();
            }

            StyleColor color = StyleKeyword.Null;
            if (state.recording)
                color = AnimationMode.recordedPropertyColor.RGBMultiplied(recordColorMultiplier);
            else if (state.previewing)
                color = AnimationMode.animatedPropertyColor.RGBMultiplied(previewColorMultiplier);

            m_TimeArea.style.backgroundColor = color;

            // Update main content
            m_OnboardingPanel.EnableInClassList(k_AnimationOnboarding + "__hidden", mainContentState != MainContentState.OnboardingPanel);
            m_DopeSheetElement.EnableInClassList(DopeSheetElement.ussClassName + "__hidden", mainContentState != MainContentState.DopeSheetEditor);
            m_CurveEditorElement.EnableInClassList(CurveEditorElement.ussClassName + "__hidden", mainContentState != MainContentState.CurveEditor);

            if (mainContentState == MainContentState.OnboardingPanel)
            {
                bool animatableObject = state.activeGameObject && !EditorUtility.IsPersistent(state.activeGameObject);
                bool animatorIsOptimized = state.animatorIsOptimized;

                m_OnboardingPanel.EnableInClassList(k_AnimationOnboarding + "__disabled", animatorIsOptimized || !animatableObject);

                if (animatorIsOptimized)
                {
                    m_OnboardingPanelLabel.text = s_AnimatorOptimizedText;
                }
                else if (animatableObject)
                {
                    var missingObjects = (selection.rootGameObject == null && selection.clip == null) ? s_AnimatorAndAnimationClipText : s_AnimationClipText;
                    string txt = String.Format(s_FormatIsMissingText, state.activeGameObject.name, missingObjects);

                    m_OnboardingPanelLabel.text = txt;
                }
                else
                {
                    m_OnboardingPanelLabel.text = s_NoAnimatableObjectSelectedText;
                }
            }
        }

        void OnRefresh()
        {
            // Play Controls
            m_PreviewButton.SetEnabled(state.canPreview);
            m_PreviewButton.SetValueWithoutNotify(state.previewing);
            m_RecordButton.SetEnabled(state.canRecord);
            m_RecordButton.SetValueWithoutNotify(state.recording);

            // Link with Timeline
            m_LinkWithSequencerButton.EnableInClassList(k_AnimationLinkWithSequencerButton + "__hidden", !state.linkedWithSequencer);
            m_LinkWithSequencerButton.SetValueWithoutNotify(state.linkedWithSequencer);

            // Clip Selection
            m_ClipDropdownField.SetEnabled(!state.disabled);

            // Overlays
            m_CanvasOverlayManager.EnableInClassList(k_AnimationContentOverlay + "__hidden", state.disabled);

            m_DurationOverlay.time = new DiscreteTime(state.timeRange.y);

            // Time Area
            m_TimeArea.SetEnabled(!state.disabled);
            m_TimeArea.FrameRate = new FrameRate((uint)state.frameRate);

            // Content Switcher
            m_ContentSwitcherButtons.SetValueWithoutNotify(state.showCurveEditor ? 1 : 0);
            m_ContentSwitcherButtons.SetEnabled(!state.disabled);

            // Clip frame rate field
            m_AnimationClipFrameRate.SetEnabled(!m_AnimEditor.selection.isReadOnly);
            m_AnimationClipFrameRateField.SetValueWithoutNotify((int)state.frameRate);

            // Keyframing panel
            bool canAddKey = !m_AnimEditor.selection.isReadOnly && state.filteredCurves.Count != 0;
            m_AddKeyframeButton.SetEnabled(canAddKey);

            bool canAddEvent = !m_AnimEditor.selection.isReadOnly;
            bool showAddEvent = m_AnimEditor.selection.clip is not UnityEditor.AnimationWindowBuiltin.AnimationWindowClip;

            m_AddEventButton.SetEnabled(canAddEvent);
            m_AddEventButton.EnableInClassList(k_AnimationAddEventButton + "__hidden", showAddEvent);

            // Filter by selection
            m_FilterBySelectionToggle.SetEnabled(!state.disabled);
            m_FilterBySelectionToggle.SetValueWithoutNotify(state.filterBySelection);

            // Add Property button
            m_AddPropertyButton.SetEnabled(m_AnimEditor.selection.canAddCurves);

            // Apply/Revert
            bool importedSelection = selection?.isImported ?? false;
            m_ApplyButton.EnableInClassList(k_AnimationApplyButton + "__hidden", !importedSelection);
            m_RevertButton.EnableInClassList(k_AnimationRevertButton + "__hidden", !importedSelection);

            bool hasUnsavedChanges = selection?.hasUnsavedChanges ?? false;
            m_ApplyButton.SetEnabled(hasUnsavedChanges);
            m_RevertButton.SetEnabled(hasUnsavedChanges);
        }
    }
}
