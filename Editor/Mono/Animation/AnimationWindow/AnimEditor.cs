// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    [Serializable]
    class AnimEditor : ScriptableObject
    {
        // Active Animation windows
        private static List<AnimEditor> s_AnimationWindows = new List<AnimEditor>();
        public static List<AnimEditor> GetAllAnimationWindows() { return s_AnimationWindows; }
        public bool stateDisabled => m_State.disabled;

        [SerializeReference] private AnimationWindowState m_State;
        [SerializeReference] private DopeSheetEditor m_DopeSheet;
        [SerializeReference] private CurveEditor m_CurveEditor;
        [SerializeField] private AnimationWindowHierarchy m_Hierarchy;
        [SerializeField] private UnityEditor.AnimationWindowBuiltin.AnimationEventTimeLine m_Events;
        [SerializeField] private EditorWindow m_OwnerWindow;

        [System.NonSerialized] private bool m_TriggerFraming;
        [System.NonSerialized] private bool m_Initialized;

        internal const int kSliderThickness = 13;
        internal const float kDisabledRulerAlpha = 0.12f;

        public AnimationWindowState state => m_State;

        public IAnimationWindowSelectionItem selection
        {
            get => m_State.selection;
            set => m_State.selection = value;
        }

        public IAnimationWindowController controller => state.controller;

        private bool triggerFraming
        {
            set
            {
                m_TriggerFraming = value;
            }

            get
            {
                return m_TriggerFraming;
            }
        }

        internal CurveEditor curveEditor => m_CurveEditor;
        internal DopeSheetEditor dopeSheetEditor => m_DopeSheet;

        internal string eventToolTipText => m_Events.tooltipText;
        internal Vector2 eventToolTipPosition => m_Events.tooltipPosition;

        internal void MainContentOnGUI(Rect contentLayoutRect)
        {
            //  Bail out if the hierarchy in animator is optimized.
            if (m_State.animatorIsOptimized)
            {
                GUI.Label(contentLayoutRect, GUIContent.none, AnimationWindowStyles.dopeSheetBackground);

                Vector2 textSize = GUI.skin.label.CalcSize(AnimationWindowStyles.animatorOptimizedText);
                Rect labelRect = new Rect(contentLayoutRect.x + contentLayoutRect.width * .5f - textSize.x * .5f, contentLayoutRect.y + contentLayoutRect.height * .5f - textSize.y * .5f, textSize.x, textSize.y);
                GUI.Label(labelRect, AnimationWindowStyles.animatorOptimizedText);
                return;
            }

            if (m_State.disabled)
            {
                SetupWizardOnGUI(contentLayoutRect);
            }
            else
            {
                if (m_State.showCurveEditor)
                {
                    CurveEditorOnGUI(contentLayoutRect);
                }
                else
                {
                    DopeSheetOnGUI(contentLayoutRect);
                }
            }
        }

        public void Update()
        {
            if (m_State == null)
                return;

            PlaybackUpdate();
        }

        public void OnEnable()
        {
            s_AnimationWindows.Add(this);

            if (m_State == null)
            {
                m_State = new AnimationWindowState();
                m_State.animEditor = this;
                m_State.selection = new FallbackSelectionItem();

                InitializeDopeSheet();
                InitializeEvents();
                InitializeCurveEditor();
            }

            InitializeNonserializedValues();

            m_State.timeArea = m_State.showCurveEditor ? (TimeArea)m_CurveEditor : m_DopeSheet;
            m_DopeSheet.state = m_State;

            m_State.OnEnable();

            m_CurveEditor.curvesUpdated += SaveChangedCurvesFromCurveEditor;
            m_CurveEditor.OnEnable();
        }

        public void OnDisable()
        {
            s_AnimationWindows.Remove(this);

            if (m_CurveEditor != null)
            {
                m_CurveEditor.curvesUpdated -= SaveChangedCurvesFromCurveEditor;
                m_CurveEditor.OnDisable();
            }

            m_DopeSheet?.OnDisable();
            m_State.OnDisable();
        }

        public void OnDestroy()
        {
            m_CurveEditor?.OnDestroy();
            m_State?.OnDestroy();
        }

        public void OnSelectionChanged()
        {
            triggerFraming = true; // Framing of clip can only be done after Layout. Here we just order it to happen later on.
            Repaint();
        }

        public void OnSelectionUpdated()
        {
            m_State.OnSelectionUpdated();
            Repaint();
        }

        public void OnStartLiveEdit()
        {
            SaveCurveEditorKeySelection();
        }

        public void OnEndLiveEdit()
        {
            UpdateSelectedKeysToCurveEditor();
            m_State.ResampleAnimation();
        }

        public void OnLostFocus()
        {
            // RenameOverlay might still be active, close before switching to another window.
            if (m_Hierarchy != null)
                m_Hierarchy.EndNameEditing(true);

            // Stop text editing.  FrameRate ui or Frame ui may still be in edition mode.
            EditorGUI.EndEditingActiveTextField();
        }

        private void PlaybackUpdate()
        {
            if (m_State.disabled && m_State.playing)
                m_State.playing = false;

            if (m_State.PlaybackUpdate())
                Repaint();
        }

        private void SetupWizardOnGUI(Rect position)
        {
            GUI.Label(position, GUIContent.none, AnimationWindowStyles.dopeSheetBackground);

            Rect positionWithoutScrollBar = new Rect(position.x, position.y, position.width - kSliderThickness, position.height - kSliderThickness);
            GUI.BeginClip(positionWithoutScrollBar);
            GUI.enabled = true;

            m_State.showCurveEditor = false;
            m_State.timeArea = m_DopeSheet;
            m_State.timeArea.SetShownHRangeInsideMargins(0f, 1f);

            bool animatableObject = m_State.activeGameObject && !EditorUtility.IsPersistent(m_State.activeGameObject);

            if (animatableObject)
            {
                var missingObjects = (selection.rootGameObject == null && selection.clip == null) ? AnimationWindowStyles.animatorAndAnimationClip.text : AnimationWindowStyles.animationClip.text;

                string txt = String.Format(AnimationWindowStyles.formatIsMissing.text, m_State.activeGameObject.name, missingObjects);

                const float buttonWidth = 70f;
                const float buttonHeight = 20f;
                const float buttonPadding = 3f;

                GUIContent textContent = EditorGUIUtility.TempContent(txt);
                Vector2 textSize = GUI.skin.label.CalcSize(textContent);
                Rect labelRect = new Rect(positionWithoutScrollBar.width * .5f - textSize.x * .5f, positionWithoutScrollBar.height * .5f - textSize.y * .5f, textSize.x, textSize.y);
                GUI.Label(labelRect, textContent);

                Rect buttonRect = new Rect(positionWithoutScrollBar.width * .5f - buttonWidth * .5f, labelRect.yMax + buttonPadding, buttonWidth, buttonHeight);

                if (GUI.Button(buttonRect, AnimationWindowStyles.create))
                {
                    if (selection.InitializeSelection())
                        AnimationWindowUtility.ControllerChanged();

                    //  Layout has changed, bail out now.
                    EditorGUIUtility.ExitGUI();
                }
            }
            else
            {
                Color oldColor = GUI.color;
                GUI.color = Color.gray;
                Vector2 textSize = GUI.skin.label.CalcSize(AnimationWindowStyles.noAnimatableObjectSelectedText);
                Rect labelRect = new Rect(positionWithoutScrollBar.width * .5f - textSize.x * .5f, positionWithoutScrollBar.height * .5f - textSize.y * .5f, textSize.x, textSize.y);
                GUI.Label(labelRect, AnimationWindowStyles.noAnimatableObjectSelectedText);
                GUI.color = oldColor;
            }
            GUI.EndClip();
            GUI.enabled = false; // Reset state to false. It's always false originally for SetupWizardOnGUI.
        }

        internal void EventLineOnGUI(Rect eventsRect)
        {
            eventsRect.width -= kSliderThickness;
            GUI.Label(eventsRect, GUIContent.none, AnimationWindowStyles.eventBackground);

            using (new EditorGUI.DisabledScope(selection.isReadOnly))
            {
                m_Events.EventLineGUI(eventsRect, m_State);
            }
        }

        internal void HierarchyOnGUI(Rect hierarchyLayoutRect)
        {
            if (!m_State.showReadOnly && m_State.selection.isReadOnly)
            {
                Vector2 labelSize = GUI.skin.label.CalcSize(AnimationWindowStyles.readOnlyPropertiesLabel);

                const float buttonWidth = 210f;
                const float buttonHeight = 20f;
                const float buttonPadding = 3f;

                Rect labelRect = new Rect(hierarchyLayoutRect.x + hierarchyLayoutRect.width * .5f - labelSize.x * .5f, hierarchyLayoutRect.y + hierarchyLayoutRect.height * .5f - labelSize.y, labelSize.x, labelSize.y);

                Rect buttonRect = new Rect(hierarchyLayoutRect.x + hierarchyLayoutRect.width * .5f - buttonWidth * .5f, labelRect.yMax + buttonPadding, buttonWidth, buttonHeight);

                GUI.Label(labelRect, AnimationWindowStyles.readOnlyPropertiesLabel);
                if (GUI.Button(buttonRect, AnimationWindowStyles.readOnlyPropertiesButton))
                {
                    m_State.showReadOnly = true;

                    //  Layout has changed, bail out now.
                    EditorGUIUtility.ExitGUI();
                }

                return;
            }

            if (!m_State.disabled)
                m_Hierarchy.OnGUI(hierarchyLayoutRect);
        }

        internal void DopeSheetOnGUI(Rect position)
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && position.Contains(evt.mousePosition))
                m_Events.ClearSelection();

            if (triggerFraming && evt.type == EventType.Repaint)
            {
                m_DopeSheet.FrameClip();
                m_CurveEditor.FrameClip(true, true);

                triggerFraming = false;
            }

            Rect noVerticalSliderRect = new Rect(position.xMin, position.yMin, position.width - kSliderThickness, position.height);

            if (evt.type == EventType.Repaint)
            {
                m_DopeSheet.rect = noVerticalSliderRect;
                m_DopeSheet.SetTickMarkerRanges();
                m_DopeSheet.RecalculateBounds();
            }

            if (m_State.showCurveEditor)
                return;

            Rect noSlidersRect = new Rect(position.xMin, position.yMin, position.width - kSliderThickness, position.height - kSliderThickness);

            m_DopeSheet.BeginViewGUI();

            GUI.Label(position, GUIContent.none, AnimationWindowStyles.dopeSheetBackground);

            if (!m_State.disabled)
            {
                m_DopeSheet.TimeRuler(noSlidersRect, m_State.frameRate, false, true, kDisabledRulerAlpha, ((ICurveEditorState)m_State).timeFormat);  // grid
            }
            m_DopeSheet.OnGUI(noSlidersRect, m_State.hierarchyState.scrollPos * -1);

            m_DopeSheet.EndViewGUI();

            Rect verticalScrollBarPosition = new Rect(noVerticalSliderRect.xMax, noVerticalSliderRect.yMin, kSliderThickness, noSlidersRect.height);

            float visibleHeight = m_Hierarchy.GetTotalRect().height;
            float contentHeight = Mathf.Max(visibleHeight, m_Hierarchy.GetContentSize().y);

            m_State.hierarchyState.scrollPos.y = GUI.VerticalScrollbar(verticalScrollBarPosition, m_State.hierarchyState.scrollPos.y, visibleHeight, 0f, contentHeight);

            if (m_DopeSheet.spritePreviewLoading == true)
                Repaint();

            HandleMainAreaCopyPaste(m_CurveEditor.areaControlID);
        }

        internal void CurveEditorOnGUI(Rect position)
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && position.Contains(evt.mousePosition))
                m_Events.ClearSelection();

            if (triggerFraming && evt.type == EventType.Repaint)
            {
                m_DopeSheet.FrameClip();
                m_CurveEditor.FrameClip(true, true);

                triggerFraming = false;
            }

            if (evt.type == EventType.Repaint)
            {
                m_CurveEditor.rect = position;
                m_CurveEditor.SetTickMarkerRanges();
            }

            Rect noSlidersRect = new Rect(position.xMin, position.yMin, position.width - kSliderThickness, position.height - kSliderThickness);

            m_CurveEditor.vSlider = m_State.showCurveEditor;
            m_CurveEditor.hSlider = m_State.showCurveEditor;

            // Sync animation curves in curve editor.  Do it only once per frame.
            if (evt.type == EventType.Layout)
                UpdateCurveEditorData();

            m_CurveEditor.BeginViewGUI();

            if (!m_State.disabled)
            {
                GUI.Box(noSlidersRect, GUIContent.none, AnimationWindowStyles.curveEditorBackground);
                m_CurveEditor.GridGUI();
            }

            EditorGUI.BeginChangeCheck();
            m_CurveEditor.CurveGUI();
            if (EditorGUI.EndChangeCheck())
            {
                SaveChangedCurvesFromCurveEditor();
            }
            m_CurveEditor.EndViewGUI();

            HandleMainAreaCopyPaste(m_CurveEditor.areaControlID);
        }

        internal void SetFrameRate(object frameRate)
        {
            m_State.frameRate = (float)frameRate;
            UpdateSelectedKeysToCurveEditor();
        }

        internal void FrameClipDelayed() => triggerFraming = true;

        internal void SwitchBetweenCurvesAndDopesheet()
        {
            if (!state.showCurveEditor)
            {
                SwitchToCurveEditor();
            }
            else
            {
                SwitchToDopeSheetEditor();
            }
        }

        internal void SwitchToCurveEditor()
        {
            m_State.showCurveEditor = true;

            UpdateSelectedKeysToCurveEditor();
            AnimationWindowUtility.SyncTimeArea(m_DopeSheet, m_CurveEditor);
            m_State.timeArea = m_CurveEditor;
        }

        internal void SwitchToDopeSheetEditor()
        {
            m_State.showCurveEditor = false;

            UpdateSelectedKeysFromCurveEditor();
            AnimationWindowUtility.SyncTimeArea(m_CurveEditor, m_DopeSheet);
            m_State.timeArea = m_DopeSheet;
        }

        internal void CreateKeyframesAtCurrentTime()
        {
            SaveCurveEditorKeySelection();
            AnimationWindowUtility.AddSelectedKeyframes(m_State, AnimationKeyTime.Frame(m_State.currentFrame, m_State.frameRate));
            m_State.ClearCandidates();
            UpdateSelectedKeysToCurveEditor();

            Repaint();
        }

        // Curve editor changes curves, but we are in charge of saving them into the clip
        private void SaveChangedCurvesFromCurveEditor()
        {
            m_State.SaveKeySelection(AnimationWindowState.kEditCurveUndoLabel);

            var clip = m_State.selection.clip;
            if (clip == null)
                return;

            var curvesToUpdate = new HashSet<CurveWrapper>();

            foreach (var curveWrapper in m_CurveEditor.animationCurves)
            {
                if (curveWrapper.changed)
                {
                    if (!curveWrapper.animationIsEditable)
                        throw new ArgumentException("Curve is not editable and shouldn't be saved.");

                    curvesToUpdate.Add(curveWrapper);
                }
            }

            clip.SaveCurves(curvesToUpdate, AnimationWindowState.kEditCurveUndoLabel);
            m_State.ResampleAnimation();
        }

        // We sync keyframe selection from curve editor to AnimationWindowState
        private void UpdateSelectedKeysFromCurveEditor()
        {
            m_State.ClearKeySelections();
            foreach (CurveSelection curveSelection in m_CurveEditor.selectedCurves)
            {
                AnimationWindowKeyframe keyFrame = AnimationWindowUtility.CurveSelectionToAnimationWindowKeyframe(curveSelection, m_State.filteredCurves);
                if (keyFrame != null)
                    m_State.SelectKey(keyFrame);
            }
        }

        // We sync keyframe selection from AnimationWindowState to curve editor
        internal void UpdateSelectedKeysToCurveEditor()
        {
            UpdateCurveEditorData();

            m_CurveEditor.ClearSelection();
            m_CurveEditor.BeginRangeSelection();
            foreach (AnimationWindowKeyframe keyframe in m_State.selectedKeys)
            {
                CurveSelection curveSelection = AnimationWindowUtility.AnimationWindowKeyframeToCurveSelection(keyframe, m_CurveEditor);
                if (curveSelection != null)
                    m_CurveEditor.AddSelection(curveSelection);
            }
            m_CurveEditor.EndRangeSelection();
        }

        internal void SaveCurveEditorKeySelection()
        {
            // Synchronize current selection in curve editor and save selection snapshot in undo redo.
            if (m_State.showCurveEditor)
                UpdateSelectedKeysFromCurveEditor();
            else
                UpdateSelectedKeysToCurveEditor();

            m_CurveEditor.SaveKeySelection(AnimationWindowState.kEditCurveUndoLabel);
        }

        public void BeginKeyModification()
        {
            SaveCurveEditorKeySelection();

            m_State.SaveKeySelection(AnimationWindowState.kEditCurveUndoLabel);
            m_State.ClearKeySelections();
        }

        public void EndKeyModification()
        {
            UpdateSelectedKeysToCurveEditor();
        }

        void HandleMainAreaCopyPaste(int controlID)
        {
            var evt = Event.current;
            var type = evt.GetTypeForControl(controlID);
            if (type != EventType.ValidateCommand && type != EventType.ExecuteCommand)
                return;

            if (evt.commandName == "Copy")
            {
                // If events timeline has selected events right now then bail out; copying of
                // these will get processed later by AnimationEventTimeLine.
                if (m_Events.HasSelectedEvents)
                    return;

                if (type == EventType.ExecuteCommand)
                {
                    if (m_State.showCurveEditor)
                        UpdateSelectedKeysFromCurveEditor();
                    m_State.CopyKeys();
                }
                evt.Use();
            }
            else if (evt.commandName == "Paste")
            {
                if (type == EventType.ExecuteCommand)
                {
                    // If clipboard contains events right now then paste those.
                    if (m_Events.CanPaste())
                    {
                        m_Events.PasteEvents(selection.rootGameObject, selection.clip, m_State.currentTime);
                    }
                    else
                    {
                        SaveCurveEditorKeySelection();
                        m_State.PasteKeys();
                        UpdateSelectedKeysToCurveEditor();
                    }

                    // data is scheduled for an update, bail out now to avoid using out of date data.
                    EditorGUIUtility.ExitGUI();
                }
                evt.Use();
            }
        }

        internal void UpdateCurveEditorData()
        {
            m_CurveEditor.animationCurves = m_State.activeCurveWrappers;
        }

        public void Repaint()
        {
            if (m_OwnerWindow != null)
                m_OwnerWindow.Repaint();
        }

        public void SetOwnerWindow(EditorWindow parent)
        {
            m_DopeSheet.m_Owner = parent;
            m_OwnerWindow = parent;
        }

        // Called just-in-time by OnGUI
        internal void Initialize()
        {
            if (m_Initialized)
                return;

            AnimationWindowStyles.Initialize();
            InitializeHierarchy();

            m_CurveEditor.state = m_State;

            // The rect here is only for initialization and will be overriden at layout
            m_DopeSheet.rect = new Rect(0, 0, 100, 100);

            m_Initialized = true;
        }

        // Called once during initialization of m_State
        private void InitializeHierarchy()
        {
            // The rect here is only for initialization and will be overriden at layout
            m_Hierarchy = new AnimationWindowHierarchy(m_State, m_OwnerWindow, new Rect(0, 0, 100, 100));
        }

        // Called once during initialization of m_State
        private void InitializeDopeSheet()
        {
            m_DopeSheet = new DopeSheetEditor(m_OwnerWindow);
            m_DopeSheet.SetTickMarkerRanges();
            m_DopeSheet.hSlider = true;
            m_DopeSheet.shownArea = new Rect(1, 1, 1, 1);
            // The rect here is only for initialization and will be overriden at layout
            m_DopeSheet.rect = new Rect(0, 0, 100, 100);
            m_DopeSheet.hTicks.SetTickModulosForFrameRate(m_State.frameRate);
        }

        // Called once during initialization of m_State
        private void InitializeEvents()
        {
            m_Events = new AnimationWindowBuiltin.AnimationEventTimeLine(m_OwnerWindow);
        }

        // Called once during initialization of m_State
        private void InitializeCurveEditor()
        {
            // The rect here is only for initialization and will be overriden at layout
            m_CurveEditor = new CurveEditor(new Rect(0, 0, 100, 100), Array.Empty<CurveWrapper>(), false);

            CurveEditorSettings settings = new CurveEditorSettings();
            settings.hTickStyle.distMin = 30; // min distance between vertical lines before they disappear completely
            settings.hTickStyle.distFull = 80; // distance between vertical lines where they gain full strength
            settings.hTickStyle.distLabel = 0; // min distance between vertical lines labels
            if (EditorGUIUtility.isProSkin)
            {
                settings.vTickStyle.tickColor.color = new Color(1, 1, 1, settings.vTickStyle.tickColor.color.a);  // color and opacity of horizontal lines
                settings.vTickStyle.labelColor.color = new Color(1, 1, 1, settings.vTickStyle.labelColor.color.a);  // color and opacity of horizontal line labels
            }
            settings.vTickStyle.distMin = 15; // min distance between horizontal lines before they disappear completely
            settings.vTickStyle.distFull = 40; // distance between horizontal lines where they gain full strength
            settings.vTickStyle.distLabel = 30; // min distance between horizontal lines labels
            settings.vTickStyle.stubs = true;
            settings.hRangeMin = 0;
            settings.hRangeLocked = false;
            settings.vRangeLocked = false;
            settings.hSlider = true;
            settings.vSlider = true;
            settings.allowDeleteLastKeyInCurve = true;
            settings.rectangleToolFlags = CurveEditorSettings.RectangleToolFlags.FullRectangleTool;
            settings.undoRedoSelection = true;
            settings.flushCurveCache = false; // Curve Wrappers are cached in AnimationWindowState.

            m_CurveEditor.shownArea = new Rect(1, 1, 1, 1);
            m_CurveEditor.settings = settings;
            m_CurveEditor.state = m_State;
        }

        // Called during initialization, even when m_State already exists
        private void InitializeNonserializedValues()
        {
            // Since CurveEditor doesn't know about AnimationWindowState, a delegate allows it to reflect frame rate changes
            m_State.onFrameRateChange += delegate(float newFrameRate)
            {
                m_CurveEditor.invSnap = newFrameRate;
                m_CurveEditor.hTicks.SetTickModulosForFrameRate(newFrameRate);
            };

            m_State.onStartLiveEdit += OnStartLiveEdit;
            m_State.onEndLiveEdit += OnEndLiveEdit;
        }

        internal bool DisplayUnsavedChangesDialogIfNecessary()
        {
            if (!selection?.hasUnsavedChanges ?? true)
                return true;

            var title = m_OwnerWindow.titleContent.text;
            var saveMessage = m_OwnerWindow.saveChangesMessage;

            const int kSave = 0;
            const int kCancel = 1;
            const int kRevert = 2;

            var option = EditorUtility.DisplayDialogComplex((string.IsNullOrEmpty(title) ? "" : (title + " - ")) + L10n.Tr("Unsaved Changes Detected"),
                saveMessage,
                L10n.Tr("Save"),
                L10n.Tr("Cancel"),
                L10n.Tr("Revert"));

            try
            {
                switch (option)
                {
                    case kSave:
                        selection?.SaveChanges();
                        break;
                    case kRevert:
                        selection?.DiscardChanges();
                        break;
                    case kCancel:
                        break;
                    default:
                        Debug.LogError("Unrecognized option.");
                        break;
                }

                return option != kCancel;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Save Changes Failed"),
                    ex.Message,
                    L10n.Tr("OK"));

                return false;
            }
        }
    }
}
