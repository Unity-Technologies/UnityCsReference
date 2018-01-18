// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal enum WrapModeFixed
    {
        Default = (int)WrapMode.Default,
        Once = (int)WrapMode.Once,
        Loop = (int)WrapMode.Loop,
        ClampForever = (int)WrapMode.ClampForever,
        PingPong = (int)WrapMode.PingPong
    }

    internal class AnimEditor : ScriptableObject
    {
        // Active Animation windows
        private static List<AnimEditor> s_AnimationWindows = new List<AnimEditor>();
        public static List<AnimEditor> GetAllAnimationWindows() { return s_AnimationWindows; }
        public bool stateDisabled { get { return m_State.disabled; } }

        [SerializeField] private SplitterState m_HorizontalSplitter;
        [SerializeField] private AnimationWindowState m_State;
        [SerializeField] private DopeSheetEditor m_DopeSheet;
        [SerializeField] private AnimationWindowHierarchy m_Hierarchy;
        [SerializeField] private AnimationWindowClipPopup m_ClipPopup;
        [SerializeField] private AnimationEventTimeLine m_Events;
        [SerializeField] private CurveEditor m_CurveEditor;
        [SerializeField] private AnimEditorOverlay m_Overlay;
        [SerializeField] private EditorWindow m_OwnerWindow;

        [System.NonSerialized] private Rect m_Position;
        [System.NonSerialized] private bool m_TriggerFraming;
        [System.NonSerialized] private bool m_Initialized;

        private float hierarchyWidth { get { return m_HorizontalSplitter.realSizes[0]; } }
        private float contentWidth { get { return m_HorizontalSplitter.realSizes[1]; } }

        internal static PrefColor kEulerXColor = new PrefColor("Animation/EulerX", 1.0f, 0.0f, 1.0f, 1.0f);
        internal static PrefColor kEulerYColor = new PrefColor("Animation/EulerY", 1.0f, 1.0f, 0.0f, 1.0f);
        internal static PrefColor kEulerZColor = new PrefColor("Animation/EulerZ", 0.0f, 1.0f, 1.0f, 1.0f);

        static private Color s_SelectionRangeColorLight = new Color32(255, 255, 255, 90);
        static private Color s_SelectionRangeColorDark = new Color32(200, 200, 200, 40);

        static private Color selectionRangeColor
        {
            get
            {
                return EditorGUIUtility.isProSkin ? s_SelectionRangeColorDark : s_SelectionRangeColorLight;
            }
        }

        static private Color s_OutOfRangeColorLight = new Color32(160, 160, 160, 127);
        static private Color s_OutOfRangeColorDark = new Color32(40, 40, 40, 127);

        static private Color outOfRangeColor
        {
            get
            {
                return EditorGUIUtility.isProSkin ? s_OutOfRangeColorDark : s_OutOfRangeColorLight;
            }
        }

        static private Color s_InRangeColorLight = new Color32(211, 211, 211, 255);
        static private Color s_InRangeColorDark =  new Color32(75, 75, 75, 255);

        static private Color inRangeColor
        {
            get
            {
                return EditorGUIUtility.isProSkin ? s_InRangeColorDark : s_InRangeColorLight;
            }
        }

        internal static PrefKey kAnimationPlayToggle = new PrefKey("Animation/Play Animation", " ");
        internal static PrefKey kAnimationPrevFrame = new PrefKey("Animation/Previous Frame", ",");
        internal static PrefKey kAnimationNextFrame = new PrefKey("Animation/Next Frame", ".");
        internal static PrefKey kAnimationPrevKeyframe = new PrefKey("Animation/Previous Keyframe", "&,");
        internal static PrefKey kAnimationNextKeyframe = new PrefKey("Animation/Next Keyframe", "&.");
        internal static PrefKey kAnimationFirstKey = new PrefKey("Animation/First Keyframe", "#,");
        internal static PrefKey kAnimationLastKey = new PrefKey("Animation/Last Keyframe", "#.");
        internal static PrefKey kAnimationRecordKeyframeSelected = new PrefKey("Animation/Key Selected", "k");
        internal static PrefKey kAnimationRecordKeyframeModified = new PrefKey("Animation/Key Modified", "#k");
        internal static PrefKey kAnimationShowCurvesToggle = new PrefKey("Animation/Show Curves", "c");

        internal const int kSliderThickness = 15;
        internal const int kLayoutRowHeight = EditorGUI.kWindowToolbarHeight + 1;
        internal const int kIntFieldWidth = 35;
        internal const int kHierarchyMinWidth = 300;
        internal const int kToggleButtonWidth = 80;
        internal const float kDisabledRulerAlpha = 0.12f;

        public AnimationWindowState state { get { return m_State; } }

        public AnimationWindowSelection selection
        {
            get
            {
                return m_State.selection;
            }
        }

        public AnimationWindowSelectionItem selectedItem
        {
            get
            {
                return m_State.selectedItem;
            }

            set
            {
                m_State.selectedItem = value;
            }
        }

        public IAnimationWindowControl controlInterface
        {
            get
            {
                return state.controlInterface;
            }
        }

        public IAnimationWindowControl overrideControlInterface
        {
            get
            {
                return state.overrideControlInterface;
            }

            set
            {
                state.overrideControlInterface = value;
            }
        }

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

        internal CurveEditor curveEditor { get { return m_CurveEditor; } }
        internal DopeSheetEditor dopeSheetEditor { get { return m_DopeSheet; } }

        public void OnAnimEditorGUI(EditorWindow parent, Rect position)
        {
            m_DopeSheet.m_Owner = parent;
            m_OwnerWindow = parent;
            m_Position = position;

            if (!m_Initialized)
                Initialize();

            m_State.OnGUI();

            if (m_State.disabled && controlInterface.recording)
                m_State.StopRecording();

            SynchronizeLayout();

            using (new EditorGUI.DisabledScope(m_State.disabled || m_State.animatorIsOptimized))
            {
                OverlayEventOnGUI();

                GUILayout.BeginHorizontal();
                SplitterGUILayout.BeginHorizontalSplit(m_HorizontalSplitter);

                // Left side
                GUILayout.BeginVertical();

                // First row of controls
                GUILayout.BeginHorizontal(EditorStyles.toolbarButton);
                PlayControlsOnGUI();
                GUILayout.EndHorizontal();

                // Second row of controls
                GUILayout.BeginHorizontal(EditorStyles.toolbarButton);
                LinkOptionsOnGUI();
                ClipSelectionDropDownOnGUI();
                GUILayout.FlexibleSpace();
                FrameRateInputFieldOnGUI();
                AddKeyframeButtonOnGUI();
                AddEventButtonOnGUI();
                GUILayout.EndHorizontal();

                HierarchyOnGUI();

                // Bottom row of controls
                GUILayout.BeginHorizontal(AnimationWindowStyles.miniToolbar);
                TabSelectionOnGUI();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                // Right side
                GUILayout.BeginVertical();

                // Acquire Rects
                Rect timerulerRect = GUILayoutUtility.GetRect(contentWidth, kLayoutRowHeight);
                Rect eventsRect = GUILayoutUtility.GetRect(contentWidth, kLayoutRowHeight);
                Rect contentLayoutRect = GUILayoutUtility.GetRect(contentWidth, contentWidth, 0f, float.MaxValue, GUILayout.ExpandHeight(true));

                // MainContent must be done first since it resizes the Zoomable area.
                MainContentOnGUI(contentLayoutRect);
                TimeRulerOnGUI(timerulerRect);
                EventLineOnGUI(eventsRect);
                GUILayout.EndVertical();

                SplitterGUILayout.EndHorizontalSplit();
                GUILayout.EndHorizontal();

                // Overlay
                OverlayOnGUI(contentLayoutRect);

                RenderEventTooltip();

                HandleHotKeys();
            }
        }

        private void MainContentOnGUI(Rect contentLayoutRect)
        {
            //  Bail out if the hierarchy in animator is optimized.
            if (m_State.animatorIsOptimized)
            {
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
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && contentLayoutRect.Contains(evt.mousePosition))
                    m_Events.ClearSelection();

                if (triggerFraming && evt.type == EventType.Repaint)
                {
                    m_DopeSheet.FrameClip();
                    m_CurveEditor.FrameClip(true, true);

                    triggerFraming = false;
                }

                if (m_State.showCurveEditor)
                {
                    CurveEditorOnGUI(contentLayoutRect);
                }
                else
                {
                    DopeSheetOnGUI(contentLayoutRect);
                }
            }

            HandleCopyPaste();
        }

        private void OverlayEventOnGUI()
        {
            if (m_State.animatorIsOptimized)
                return;

            if (m_State.disabled)
                return;

            Rect overlayRect = new Rect(hierarchyWidth - 1, 0f, contentWidth - kSliderThickness, m_Position.height - kSliderThickness);

            GUI.BeginGroup(overlayRect);
            m_Overlay.HandleEvents();
            GUI.EndGroup();
        }

        private void OverlayOnGUI(Rect contentRect)
        {
            if (m_State.animatorIsOptimized)
                return;

            if (m_State.disabled)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            Rect contentRectNoSliders = new Rect(contentRect.xMin, contentRect.yMin, contentRect.width - kSliderThickness, contentRect.height - kSliderThickness);
            Rect overlayRectNoSliders = new Rect(hierarchyWidth - 1, 0f, contentWidth - kSliderThickness, m_Position.height - kSliderThickness);

            GUI.BeginGroup(overlayRectNoSliders);

            Rect localRect = new Rect(0, 0, overlayRectNoSliders.width, overlayRectNoSliders.height);

            Rect localContentRect = contentRectNoSliders;
            localContentRect.position -= overlayRectNoSliders.min;

            m_Overlay.OnGUI(localRect, localContentRect);

            GUI.EndGroup();
        }

        public void Update()
        {
            if (m_State == null)
                return;

            PlaybackUpdate();
        }

        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            s_AnimationWindows.Add(this);

            if (m_State == null)
            {
                m_State = CreateInstance(typeof(AnimationWindowState)) as AnimationWindowState;
                m_State.hideFlags = HideFlags.HideAndDontSave;
                m_State.animEditor = this;
                InitializeHorizontalSplitter();
                InitializeClipSelection();
                InitializeDopeSheet();
                InitializeEvents();
                InitializeCurveEditor();
                InitializeOverlay();
            }

            InitializeNonserializedValues();

            m_State.timeArea = m_State.showCurveEditor ? (TimeArea)m_CurveEditor : m_DopeSheet;
            m_DopeSheet.state = m_State;
            m_ClipPopup.state = m_State;
            m_Overlay.state = m_State;

            m_CurveEditor.curvesUpdated += SaveChangedCurvesFromCurveEditor;
            m_CurveEditor.OnEnable();

            EditorApplication.globalEventHandler += HandleGlobalHotkeys;
        }

        public void OnDisable()
        {
            s_AnimationWindows.Remove(this);

            if (m_CurveEditor != null)
            {
                m_CurveEditor.curvesUpdated -= SaveChangedCurvesFromCurveEditor;
                m_CurveEditor.OnDisable();
            }

            if (m_DopeSheet != null)
                m_DopeSheet.OnDisable();

            m_State.OnDisable();

            EditorApplication.globalEventHandler -= HandleGlobalHotkeys;
        }

        public void OnDestroy()
        {
            if (m_CurveEditor != null)
                m_CurveEditor.OnDestroy();

            DestroyImmediate(m_State);
        }

        public void OnSelectionChanged()
        {
            m_State.OnSelectionChanged();
            triggerFraming = true; // Framing of clip can only be done after Layout. Here we just order it to happen later on.
            Repaint();
        }

        public void OnStartLiveEdit()
        {
            SaveCurveEditorKeySelection();
        }

        public void OnEndLiveEdit()
        {
            UpdateSelectedKeysToCurveEditor();
            controlInterface.ResampleAnimation();
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
            if (m_State.disabled && controlInterface.playing)
                controlInterface.StopPlayback();

            if (controlInterface.PlaybackUpdate())
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
                var missingObjects = (!m_State.activeRootGameObject && !m_State.activeAnimationClip) ? AnimationWindowStyles.animatorAndAnimationClip.text : AnimationWindowStyles.animationClip.text;

                string txt = String.Format(AnimationWindowStyles.formatIsMissing.text, m_State.activeGameObject.name, missingObjects);

                const float buttonWidth = 70f;
                const float buttonHeight = 20f;
                const float buttonPadding = 3f;

                GUIContent textContent = GUIContent.Temp(txt);
                Vector2 textSize = GUI.skin.label.CalcSize(textContent);
                Rect labelRect = new Rect(positionWithoutScrollBar.width * .5f - textSize.x * .5f, positionWithoutScrollBar.height * .5f - textSize.y * .5f, textSize.x, textSize.y);
                GUI.Label(labelRect, textContent);

                Rect buttonRect = new Rect(positionWithoutScrollBar.width * .5f - buttonWidth * .5f, labelRect.yMax + buttonPadding, buttonWidth, buttonHeight);

                if (GUI.Button(buttonRect, AnimationWindowStyles.create))
                {
                    if (AnimationWindowUtility.InitializeGameobjectForAnimation(m_State.activeGameObject))
                    {
                        Component animationPlayer = AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(m_State.activeGameObject.transform);
                        m_State.selection.UpdateClip(m_State.selectedItem, AnimationUtility.GetAnimationClips(animationPlayer.gameObject)[0]);
                    }

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

        private void EventLineOnGUI(Rect eventsRect)
        {
            eventsRect.width -= kSliderThickness;
            GUI.Label(eventsRect, GUIContent.none, AnimationWindowStyles.eventBackground);

            using (new EditorGUI.DisabledScope(m_State.selectedItem == null || !m_State.selectedItem.animationIsEditable))
            {
                m_Events.EventLineGUI(eventsRect, m_State);
            }
        }

        private void RenderEventTooltip()
        {
            m_Events.DrawInstantTooltip(m_Position);
        }

        private void TabSelectionOnGUI()
        {
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(!m_State.showCurveEditor, AnimationWindowStyles.dopesheet, AnimationWindowStyles.miniToolbarButton, GUILayout.Width(kToggleButtonWidth));
            GUILayout.Toggle(m_State.showCurveEditor, AnimationWindowStyles.curves, AnimationWindowStyles.miniToolbarButton, GUILayout.Width(kToggleButtonWidth));
            if (EditorGUI.EndChangeCheck())
            {
                SwitchBetweenCurvesAndDopesheet();
            }
            else if (kAnimationShowCurvesToggle.activated)
            {
                SwitchBetweenCurvesAndDopesheet();
                Event.current.Use();
            }
        }

        private void HierarchyOnGUI()
        {
            Rect r = GUILayoutUtility.GetRect(hierarchyWidth, hierarchyWidth, 0f, float.MaxValue, GUILayout.ExpandHeight(true));

            if (!m_State.disabled)
                m_Hierarchy.OnGUI(r);
        }

        private void FrameRateInputFieldOnGUI()
        {
            var selectedItem = m_State.selectedItem;

            using (new EditorGUI.DisabledScope(selectedItem == null || !selectedItem.animationIsEditable))
            {
                GUILayout.Label(AnimationWindowStyles.samples, AnimationWindowStyles.toolbarLabel);

                EditorGUI.BeginChangeCheck();
                int clipFrameRate = EditorGUILayout.DelayedIntField((int)m_State.clipFrameRate, EditorStyles.toolbarTextField, GUILayout.Width(kIntFieldWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    m_State.clipFrameRate = clipFrameRate;
                    UpdateSelectedKeysToCurveEditor();
                }
            }
        }

        private void ClipSelectionDropDownOnGUI()
        {
            m_ClipPopup.OnGUI();
        }

        private void DopeSheetOnGUI(Rect position)
        {
            Rect noVerticalSliderRect = new Rect(position.xMin, position.yMin, position.width - kSliderThickness, position.height);

            if (Event.current.type == EventType.Repaint)
            {
                m_DopeSheet.rect = noVerticalSliderRect;
                m_DopeSheet.SetTickMarkerRanges();
                m_DopeSheet.RecalculateBounds();
            }

            if (m_State.showCurveEditor)
                return;

            Rect noSlidersRect = new Rect(position.xMin, position.yMin, position.width - kSliderThickness, position.height - kSliderThickness);
            Rect masterDopelineRect = new Rect(noSlidersRect.xMin, noSlidersRect.yMin, noSlidersRect.width, AnimationWindowHierarchyGUI.k_DopeSheetRowHeight);

            m_DopeSheet.BeginViewGUI();

            GUI.Label(position, GUIContent.none, AnimationWindowStyles.dopeSheetBackground);

            if (!m_State.disabled)
            {
                m_DopeSheet.TimeRuler(noSlidersRect, m_State.frameRate, false, true, kDisabledRulerAlpha, m_State.timeFormat);  // grid
                m_DopeSheet.DrawMasterDopelineBackground(masterDopelineRect); // This needs to be under the playhead
            }
            m_DopeSheet.OnGUI(noSlidersRect, m_State.hierarchyState.scrollPos * -1);

            m_DopeSheet.EndViewGUI();

            Rect verticalScrollBarPosition = new Rect(noVerticalSliderRect.xMax, noVerticalSliderRect.yMin, kSliderThickness, noSlidersRect.height);

            float visibleHeight = m_Hierarchy.GetTotalRect().height;
            float contentHeight = Mathf.Max(visibleHeight, m_Hierarchy.GetContentSize().y);

            m_State.hierarchyState.scrollPos.y = GUI.VerticalScrollbar(verticalScrollBarPosition, m_State.hierarchyState.scrollPos.y, visibleHeight, 0f, contentHeight);

            if (m_DopeSheet.spritePreviewLoading == true)
                Repaint();
        }

        private void CurveEditorOnGUI(Rect position)
        {
            if (Event.current.type == EventType.Repaint)
            {
                m_CurveEditor.rect = position;
                m_CurveEditor.SetTickMarkerRanges();
            }

            Rect noSlidersRect = new Rect(position.xMin, position.yMin, position.width - kSliderThickness, position.height - kSliderThickness);

            m_CurveEditor.vSlider = m_State.showCurveEditor;
            m_CurveEditor.hSlider = m_State.showCurveEditor;

            // Sync animation curves in curve editor.  Do it only once per frame.
            if (Event.current.type == EventType.Layout)
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
        }

        private void TimeRulerOnGUI(Rect timeRulerRect)
        {
            Rect timeRulerRectNoScrollbar = new Rect(timeRulerRect.xMin, timeRulerRect.yMin, timeRulerRect.width - kSliderThickness, timeRulerRect.height);

            GUI.Box(timeRulerRect, GUIContent.none, EditorStyles.toolbarButton);

            if (!m_State.disabled)
            {
                RenderInRangeOverlay(timeRulerRectNoScrollbar);
                RenderSelectionOverlay(timeRulerRectNoScrollbar);
            }

            m_State.timeArea.TimeRuler(timeRulerRectNoScrollbar, m_State.frameRate, true, false, 1f, m_State.timeFormat);

            if (!m_State.disabled)
                RenderOutOfRangeOverlay(timeRulerRectNoScrollbar);
        }

        private void AddEventButtonOnGUI()
        {
            var selectedItem = m_State.selectedItem;
            if (selectedItem != null)
            {
                using (new EditorGUI.DisabledScope(!selectedItem.animationIsEditable))
                {
                    if (GUILayout.Button(AnimationWindowStyles.addEventContent, EditorStyles.toolbarButton))
                        m_Events.AddEvent(m_State.currentTime - selectedItem.timeOffset, selectedItem.rootGameObject, selectedItem.animationClip);
                }
            }
        }

        private void AddKeyframeButtonOnGUI()
        {
            bool animationIsEditable = m_State.selection.Find(selectedItem => selectedItem.animationIsEditable);

            using (new EditorGUI.DisabledScope(!animationIsEditable))
            {
                if (GUILayout.Button(AnimationWindowStyles.addKeyframeContent, EditorStyles.toolbarButton))
                {
                    SaveCurveEditorKeySelection();
                    var keyTime = AnimationKeyTime.Time(m_State.currentTime, m_State.frameRate);
                    AnimationWindowUtility.AddSelectedKeyframes(m_State, keyTime);
                    UpdateSelectedKeysToCurveEditor();

                    // data is scheduled for an update, bail out now to avoid using out of date data.
                    EditorGUIUtility.ExitGUI();
                }
            }
        }

        private void PlayControlsOnGUI()
        {
            using (new EditorGUI.DisabledScope(!controlInterface.canPreview))
            {
                PreviewButtonOnGUI();
            }

            using (new EditorGUI.DisabledScope(!controlInterface.canRecord))
            {
                RecordButtonOnGUI();
            }

            if (GUILayout.Button(AnimationWindowStyles.firstKeyContent, EditorStyles.toolbarButton))
            {
                controlInterface.GoToFirstKeyframe();

                // Stop text editing.  User may be editing frame navigation ui which will not update until we exit text editing.
                EditorGUI.EndEditingActiveTextField();
            }

            if (GUILayout.Button(AnimationWindowStyles.prevKeyContent, EditorStyles.toolbarButton))
            {
                controlInterface.GoToPreviousKeyframe();

                // Stop text editing.  User may be editing frame navigation ui which will not update until we exit text editing.
                EditorGUI.EndEditingActiveTextField();
            }

            using (new EditorGUI.DisabledScope(!controlInterface.canPlay))
            {
                PlayButtonOnGUI();
            }

            if (GUILayout.Button(AnimationWindowStyles.nextKeyContent, EditorStyles.toolbarButton))
            {
                controlInterface.GoToNextKeyframe();

                // Stop text editing.  User may be editing frame navigation ui which will not update until we exit text editing.
                EditorGUI.EndEditingActiveTextField();
            }

            if (GUILayout.Button(AnimationWindowStyles.lastKeyContent, EditorStyles.toolbarButton))
            {
                controlInterface.GoToLastKeyframe();

                // Stop text editing.  User may be editing frame navigation ui which will not update until we exit text editing.
                EditorGUI.EndEditingActiveTextField();
            }

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            int newFrame = EditorGUILayout.DelayedIntField(m_State.currentFrame, EditorStyles.toolbarTextField, GUILayout.Width(kIntFieldWidth));
            if (EditorGUI.EndChangeCheck())
            {
                controlInterface.GoToFrame(newFrame);
            }
        }

        private void LinkOptionsOnGUI()
        {
            if (m_State.linkedWithSequencer)
            {
                if (GUILayout.Toggle(true, AnimationWindowStyles.sequencerLinkContent, EditorStyles.toolbarButton) == false)
                {
                    m_State.linkedWithSequencer = false;
                    m_State.selection.Clear();

                    // Layout has changed, bail out now.
                    EditorGUIUtility.ExitGUI();
                }
            }
        }

        private void HandleHotKeys()
        {
            if (!GUI.enabled || m_State.disabled)
                return;

            bool keyChanged = false;

            if (kAnimationPrevKeyframe.activated)
            {
                controlInterface.GoToPreviousKeyframe();
                keyChanged = true;
            }

            if (kAnimationNextKeyframe.activated)
            {
                controlInterface.GoToNextKeyframe();
                keyChanged = true;
            }

            if (kAnimationNextFrame.activated)
            {
                controlInterface.GoToNextFrame();
                keyChanged = true;
            }

            if (kAnimationPrevFrame.activated)
            {
                controlInterface.GoToPreviousFrame();
                keyChanged = true;
            }

            if (kAnimationFirstKey.activated)
            {
                controlInterface.GoToFirstKeyframe();
                keyChanged = true;
            }

            if (kAnimationLastKey.activated)
            {
                controlInterface.GoToLastKeyframe();
                keyChanged = true;
            }

            if (keyChanged)
            {
                Event.current.Use();
                Repaint();
            }

            if (kAnimationPlayToggle.activated)
            {
                if (controlInterface.playing)
                    controlInterface.StopPlayback();
                else
                    controlInterface.StartPlayback();

                Event.current.Use();
            }

            if (kAnimationRecordKeyframeSelected.activated)
            {
                SaveCurveEditorKeySelection();
                AnimationWindowUtility.AddSelectedKeyframes(m_State, controlInterface.time);
                UpdateSelectedKeysToCurveEditor();

                Event.current.Use();
            }

            if (kAnimationRecordKeyframeModified.activated)
            {
                SaveCurveEditorKeySelection();
                controlInterface.ProcessCandidates();
                UpdateSelectedKeysToCurveEditor();

                Event.current.Use();
            }
        }

        public void HandleGlobalHotkeys()
        {
            if (!m_State.previewing)
                return;

            if (!GUI.enabled || m_State.disabled)
                return;

            if (kAnimationRecordKeyframeSelected.activated)
            {
                SaveCurveEditorKeySelection();
                AnimationWindowUtility.AddSelectedKeyframes(m_State, controlInterface.time);
                controlInterface.ClearCandidates();
                UpdateSelectedKeysToCurveEditor();

                Event.current.Use();
            }

            if (kAnimationRecordKeyframeModified.activated)
            {
                SaveCurveEditorKeySelection();
                controlInterface.ProcessCandidates();
                UpdateSelectedKeysToCurveEditor();

                Event.current.Use();
            }
        }

        private void PlayButtonOnGUI()
        {
            EditorGUI.BeginChangeCheck();
            bool playbackEnabled = GUILayout.Toggle(controlInterface.playing, AnimationWindowStyles.playContent, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                if (playbackEnabled)
                    controlInterface.StartPlayback();
                else
                    controlInterface.StopPlayback();

                // Stop text editing.  User may be editing frame navigation ui which will not update until we exit text editing.
                EditorGUI.EndEditingActiveTextField();
            }
        }

        private void PreviewButtonOnGUI()
        {
            EditorGUI.BeginChangeCheck();

            bool recordingEnabled = GUILayout.Toggle(controlInterface.previewing, AnimationWindowStyles.previewContent, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                if (recordingEnabled)
                    m_State.StartPreview();
                else
                    m_State.StopPreview();
            }
        }

        private void RecordButtonOnGUI()
        {
            EditorGUI.BeginChangeCheck();

            Color backupColor = GUI.color;
            if (controlInterface.recording)
            {
                Color recordedColor = AnimationMode.recordedPropertyColor;
                recordedColor.a *= GUI.color.a;
                GUI.color = recordedColor;
            }

            bool recordingEnabled = GUILayout.Toggle(controlInterface.recording, AnimationWindowStyles.recordContent, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                if (recordingEnabled)
                    m_State.StartRecording();
                else
                {
                    m_State.StopRecording();

                    // Force refresh in inspector as stopping recording does not invalidate any data.
                    InspectorWindow.RepaintAllInspectors();
                }
            }

            GUI.color = backupColor;
        }

        private void SwitchBetweenCurvesAndDopesheet()
        {
            if (!m_State.showCurveEditor)
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

        private void RenderSelectionOverlay(Rect rect)
        {
            if (m_State.showCurveEditor && !m_CurveEditor.hasSelection)
                return;

            if (!m_State.showCurveEditor && m_State.selectedKeys.Count == 0)
                return;

            const int kOverlayMinWidth = 14;

            Bounds bounds = m_State.showCurveEditor ? m_CurveEditor.selectionBounds : m_State.selectionBounds;

            float startPixel = m_State.TimeToPixel(bounds.min.x) + rect.xMin;
            float endPixel = m_State.TimeToPixel(bounds.max.x) + rect.xMin;

            if ((endPixel - startPixel) < kOverlayMinWidth)
            {
                float centerPixel = (startPixel + endPixel) * 0.5f;

                startPixel = centerPixel - kOverlayMinWidth * 0.5f;
                endPixel = centerPixel + kOverlayMinWidth * 0.5f;
            }

            AnimationWindowUtility.DrawSelectionOverlay(rect, selectionRangeColor, startPixel, endPixel);
        }

        private void RenderInRangeOverlay(Rect rect)
        {
            Color color = inRangeColor;

            if (controlInterface.recording)
                color *= AnimationMode.recordedPropertyColor;
            else if (controlInterface.previewing)
                color *= AnimationMode.animatedPropertyColor;
            else
                color = Color.clear;

            Vector2 timeRange = m_State.timeRange;
            AnimationWindowUtility.DrawInRangeOverlay(rect, color, m_State.TimeToPixel(timeRange.x) + rect.xMin, m_State.TimeToPixel(timeRange.y) + rect.xMin);
        }

        private void RenderOutOfRangeOverlay(Rect rect)
        {
            Color color = outOfRangeColor;

            if (controlInterface.recording)
                color *= AnimationMode.recordedPropertyColor;
            else if (controlInterface.previewing)
                color *= AnimationMode.animatedPropertyColor;

            Vector2 timeRange = m_State.timeRange;
            AnimationWindowUtility.DrawOutOfRangeOverlay(rect, color, m_State.TimeToPixel(timeRange.x) + rect.xMin, m_State.TimeToPixel(timeRange.y) + rect.xMin);
        }

        private void SynchronizeLayout()
        {
            m_HorizontalSplitter.realSizes[1] = (int)Mathf.Min(m_Position.width - m_HorizontalSplitter.realSizes[0], m_HorizontalSplitter.realSizes[1]);

            // Synchronize frame rate
            if ((selectedItem != null) && (selectedItem.animationClip != null))
            {
                m_State.frameRate = selectedItem.animationClip.frameRate;
            }
            else
            {
                m_State.frameRate = AnimationWindowState.kDefaultFrameRate;
            }
        }

        struct ChangedCurvesPerClip
        {
            public List<EditorCurveBinding> bindings;
            public List<AnimationCurve> curves;
        }

        // Curve editor changes curves, but we are in charge of saving them into the clip
        private void SaveChangedCurvesFromCurveEditor()
        {
            m_State.SaveKeySelection(AnimationWindowState.kEditCurveUndoLabel);

            var curvesToUpdate = new Dictionary<AnimationClip, ChangedCurvesPerClip>();
            var changedCurves = new ChangedCurvesPerClip();

            for (int i = 0; i < m_CurveEditor.animationCurves.Length; ++i)
            {
                CurveWrapper curveWrapper = m_CurveEditor.animationCurves[i];
                if (curveWrapper.changed)
                {
                    if (!curveWrapper.animationIsEditable)
                        Debug.LogError("Curve is not editable and shouldn't be saved.");

                    if (curveWrapper.animationClip != null)
                    {
                        if (curvesToUpdate.TryGetValue(curveWrapper.animationClip, out changedCurves))
                        {
                            changedCurves.bindings.Add(curveWrapper.binding);
                            changedCurves.curves.Add(curveWrapper.curve.length > 0 ? curveWrapper.curve : null);
                        }
                        else
                        {
                            changedCurves.bindings = new List<EditorCurveBinding>();
                            changedCurves.curves = new List<AnimationCurve>();

                            changedCurves.bindings.Add(curveWrapper.binding);
                            changedCurves.curves.Add(curveWrapper.curve.length > 0 ? curveWrapper.curve : null);

                            curvesToUpdate.Add(curveWrapper.animationClip, changedCurves);
                        }
                    }

                    curveWrapper.changed = false;
                }
            }

            if (curvesToUpdate.Count > 0)
            {
                foreach (var kvp in curvesToUpdate)
                {
                    Undo.RegisterCompleteObjectUndo(kvp.Key, AnimationWindowState.kEditCurveUndoLabel);
                    AnimationUtility.SetEditorCurves(kvp.Key, kvp.Value.bindings.ToArray(), kvp.Value.curves.ToArray());
                }

                m_State.ResampleAnimation();
            }
        }

        // We sync keyframe selection from curve editor to AnimationWindowState
        private void UpdateSelectedKeysFromCurveEditor()
        {
            m_State.ClearKeySelections();
            foreach (CurveSelection curveSelection in m_CurveEditor.selectedCurves)
            {
                AnimationWindowKeyframe keyFrame = AnimationWindowUtility.CurveSelectionToAnimationWindowKeyframe(curveSelection, m_State.allCurves);
                if (keyFrame != null)
                    m_State.SelectKey(keyFrame);
            }
        }

        // We sync keyframe selection from AnimationWindowState to curve editor
        private void UpdateSelectedKeysToCurveEditor()
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

        private void SaveCurveEditorKeySelection()
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

        private void HandleCopyPaste()
        {
            if (Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand)
            {
                switch (Event.current.commandName)
                {
                    case "Copy":
                        if (Event.current.type == EventType.ExecuteCommand)
                        {
                            if (m_State.showCurveEditor)
                                UpdateSelectedKeysFromCurveEditor();
                            m_State.CopyKeys();
                        }
                        Event.current.Use();
                        break;
                    case "Paste":
                        if (Event.current.type == EventType.ExecuteCommand)
                        {
                            SaveCurveEditorKeySelection();
                            m_State.PasteKeys();
                            UpdateSelectedKeysToCurveEditor();

                            // data is scheduled for an update, bail out now to avoid using out of date data.
                            EditorGUIUtility.ExitGUI();
                        }
                        Event.current.Use();
                        break;
                }
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

        // Called just-in-time by OnGUI
        private void Initialize()
        {
            AnimationWindowStyles.Initialize();
            InitializeHierarchy();

            m_CurveEditor.state = m_State;

            // The rect here is only for initialization and will be overriden at layout
            m_HorizontalSplitter.realSizes[0] = kHierarchyMinWidth;
            m_HorizontalSplitter.realSizes[1] = (int)Mathf.Max(m_Position.width - kHierarchyMinWidth, kHierarchyMinWidth);
            m_DopeSheet.rect = new Rect(0, 0, contentWidth, 100);

            m_Initialized = true;
        }

        // Called once during initialization of m_State
        private void InitializeClipSelection()
        {
            m_ClipPopup = new AnimationWindowClipPopup();
        }

        // Called once during initialization of m_State
        private void InitializeHierarchy()
        {
            // The rect here is only for initialization and will be overriden at layout
            m_Hierarchy = new AnimationWindowHierarchy(m_State, m_OwnerWindow, new Rect(0, 0, hierarchyWidth, 100));
        }

        // Called once during initialization of m_State
        private void InitializeDopeSheet()
        {
            m_DopeSheet = new DopeSheetEditor(m_OwnerWindow);
            m_DopeSheet.SetTickMarkerRanges();
            m_DopeSheet.hSlider = true;
            m_DopeSheet.shownArea = new Rect(1, 1, 1, 1);
            // The rect here is only for initialization and will be overriden at layout
            m_DopeSheet.rect = new Rect(0, 0, contentWidth, 100);
            m_DopeSheet.hTicks.SetTickModulosForFrameRate(m_State.frameRate);
        }

        // Called once during initialization of m_State
        private void InitializeEvents()
        {
            m_Events = new AnimationEventTimeLine(m_OwnerWindow);
        }

        // Called once during initialization of m_State
        private void InitializeCurveEditor()
        {
            // The rect here is only for initialization and will be overriden at layout
            m_CurveEditor = new CurveEditor(new Rect(0, 0, contentWidth, 100), new CurveWrapper[0], false);

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

            m_CurveEditor.shownArea = new Rect(1, 1, 1, 1);
            m_CurveEditor.settings = settings;
            m_CurveEditor.state = m_State;
        }

        // Called once during initialization of m_State
        private void InitializeHorizontalSplitter()
        {
            m_HorizontalSplitter = new SplitterState(new float[] { kHierarchyMinWidth, kHierarchyMinWidth * 3 }, new int[] { kHierarchyMinWidth, kHierarchyMinWidth }, null);
            m_HorizontalSplitter.realSizes[0] = kHierarchyMinWidth;
            m_HorizontalSplitter.realSizes[1] = kHierarchyMinWidth;
        }

        // Called once during initialization of m_State
        private void InitializeOverlay()
        {
            m_Overlay = new AnimEditorOverlay();
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

            m_State.selection.onSelectionChanged += OnSelectionChanged;
        }
    }
}
