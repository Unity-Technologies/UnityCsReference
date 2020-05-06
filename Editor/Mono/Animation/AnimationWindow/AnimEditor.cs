// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
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

        static private Color s_FilterBySelectionColorLight = new Color(0.82f, 0.97f, 1.00f, 1.00f);
        static private Color s_FilterBySelectionColorDark = new Color(0.54f, 0.85f, 1.00f, 1.00f);

        static private Color filterBySelectionColor
        {
            get
            {
                return EditorGUIUtility.isProSkin ? s_FilterBySelectionColorDark : s_FilterBySelectionColorLight;
            }
        }

        internal const int kSliderThickness = 13;
        internal const int kIntFieldWidth = 35;
        internal const int kHierarchyMinWidth = 300;
        internal const int kToggleButtonWidth = 80;
        internal const float kDisabledRulerAlpha = 0.12f;

        private int layoutRowHeight
        {
            get { return (int)EditorGUI.kWindowToolbarHeight; }
        }

        internal struct FrameRateMenuEntry
        {
            public FrameRateMenuEntry(GUIContent content, float value)
            {
                this.content = content;
                this.value = value;
            }

            public GUIContent content;
            public float value;
        }

        internal static FrameRateMenuEntry[] kAvailableFrameRates = new FrameRateMenuEntry[]
        {
            new FrameRateMenuEntry(EditorGUIUtility.TextContent("Set Sample Rate/24"), 24f),
            new FrameRateMenuEntry(EditorGUIUtility.TextContent("Set Sample Rate/25"), 25f),
            new FrameRateMenuEntry(EditorGUIUtility.TextContent("Set Sample Rate/30"), 30f),
            new FrameRateMenuEntry(EditorGUIUtility.TextContent("Set Sample Rate/50"), 50f),
            new FrameRateMenuEntry(EditorGUIUtility.TextContent("Set Sample Rate/60"), 60f)
        };

        public AnimationWindowState state { get { return m_State; } }

        public AnimationWindowSelectionItem selection
        {
            get
            {
                return m_State.selection;
            }

            set
            {
                m_State.selection = value;
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
                int optionsID = GUIUtility.GetControlID(FocusType.Passive);
                if (Event.current.type != EventType.Repaint)
                    OptionsOnGUI(optionsID);

                OverlayEventOnGUI();

                GUILayout.BeginHorizontal();
                SplitterGUILayout.BeginHorizontalSplit(m_HorizontalSplitter);

                // Left side
                GUILayout.BeginVertical();

                // First row of controls
                GUILayout.BeginHorizontal(AnimationWindowStyles.animPlayToolBar);
                PlayControlsOnGUI();
                GUILayout.EndHorizontal();

                // Second row of controls
                GUILayout.BeginHorizontal(AnimationWindowStyles.animClipToolBar);
                LinkOptionsOnGUI();
                ClipSelectionDropDownOnGUI();
                GUILayout.FlexibleSpace();
                FrameRateInputFieldOnGUI();
                FilterBySelectionButtonOnGUI();
                AddKeyframeButtonOnGUI();
                AddEventButtonOnGUI();
                GUILayout.EndHorizontal();

                HierarchyOnGUI();

                // Bottom row of controls
                using (new GUILayout.HorizontalScope(AnimationWindowStyles.toolbarBottom))
                {
                    TabSelectionOnGUI();
                }

                GUILayout.EndVertical();

                // Right side
                GUILayout.BeginVertical();

                // Acquire Rects
                Rect timerulerRect = GUILayoutUtility.GetRect(contentWidth, layoutRowHeight);
                Rect eventsRect = GUILayoutUtility.GetRect(contentWidth, layoutRowHeight - 1);
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

                if (Event.current.type == EventType.Repaint)
                {
                    OptionsOnGUI(optionsID);
                    AnimationWindowStyles.separator.Draw(new Rect(hierarchyWidth, 0, 1, position.height), false, false, false, false);
                }

                RenderEventTooltip();
            }
        }

        void MainContentOnGUI(Rect contentLayoutRect)
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

            var mainAreaControlID = 0;
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
                    mainAreaControlID = m_CurveEditor.areaControlID;
                }
                else
                {
                    DopeSheetOnGUI(contentLayoutRect);
                    mainAreaControlID = m_DopeSheet.areaControlID;
                }
            }

            HandleMainAreaCopyPaste(mainAreaControlID);
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
        }

        public void OnDestroy()
        {
            if (m_CurveEditor != null)
                m_CurveEditor.OnDestroy();

            DestroyImmediate(m_State);
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
                        m_State.activeAnimationClip = AnimationUtility.GetAnimationClips(animationPlayer.gameObject)[0];
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

            using (new EditorGUI.DisabledScope(!selection.animationIsEditable))
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
            GUILayout.Toggle(m_State.showCurveEditor, AnimationWindowStyles.curves, EditorStyles.toolbarButtonRight, GUILayout.Width(kToggleButtonWidth));
            if (EditorGUI.EndChangeCheck())
            {
                SwitchBetweenCurvesAndDopesheet();
            }
        }

        private void HierarchyOnGUI()
        {
            Rect hierarchyLayoutRect = GUILayoutUtility.GetRect(hierarchyWidth, hierarchyWidth, 0f, float.MaxValue, GUILayout.ExpandHeight(true));

            if (!m_State.showReadOnly && !m_State.selection.animationIsEditable)
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

        private void FrameRateInputFieldOnGUI()
        {
            if (!m_State.showFrameRate)
                return;

            using (new EditorGUI.DisabledScope(!selection.animationIsEditable))
            {
                GUILayout.Label(AnimationWindowStyles.samples, EditorStyles.toolbarLabel);

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

            m_DopeSheet.BeginViewGUI();

            GUI.Label(position, GUIContent.none, AnimationWindowStyles.dopeSheetBackground);

            if (!m_State.disabled)
            {
                m_DopeSheet.TimeRuler(noSlidersRect, m_State.frameRate, false, true, kDisabledRulerAlpha, m_State.timeFormat);  // grid
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
            Rect timeRulerBackgroundRect = timeRulerRectNoScrollbar;

            GUI.Box(timeRulerBackgroundRect, GUIContent.none, AnimationWindowStyles.timeRulerBackground);

            if (!m_State.disabled)
            {
                RenderInRangeOverlay(timeRulerRectNoScrollbar);
                RenderSelectionOverlay(timeRulerRectNoScrollbar);
            }

            m_State.timeArea.TimeRuler(timeRulerRectNoScrollbar, m_State.frameRate, true, false, 1f, m_State.timeFormat);

            if (!m_State.disabled)
                RenderOutOfRangeOverlay(timeRulerRectNoScrollbar);
        }

        private GenericMenu GenerateOptionsMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TextContent("Seconds"), m_State.timeFormat == TimeArea.TimeFormat.TimeFrame, () => m_State.timeFormat = TimeArea.TimeFormat.TimeFrame);
            menu.AddItem(EditorGUIUtility.TextContent("Frames"), m_State.timeFormat == TimeArea.TimeFormat.Frame, () => m_State.timeFormat = TimeArea.TimeFormat.Frame);

            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TextContent("Ripple"), m_State.rippleTime, () => m_State.rippleTime = !m_State.rippleTime);

            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TextContent("Show Sample Rate"), m_State.showFrameRate, () => m_State.showFrameRate = !m_State.showFrameRate);

            bool isAnimatable = selection != null && selection.animationIsEditable;

            GenericMenu.MenuFunction2 nullMenuFunction2 = null;

            for (int i = 0; i < kAvailableFrameRates.Length; ++i)
            {
                FrameRateMenuEntry entry = kAvailableFrameRates[i];

                bool isActive = m_State.clipFrameRate.Equals(entry.value);
                menu.AddItem(entry.content, isActive, isAnimatable ? SetFrameRate : nullMenuFunction2, entry.value);
            }

            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TextContent("Show Read-only Properties"), m_State.showReadOnly, () => m_State.showReadOnly = !m_State.showReadOnly);

            return menu;
        }

        private void OptionsOnGUI(int controlID)
        {
            Rect layoutRect = new Rect(hierarchyWidth, 0f, contentWidth, layoutRowHeight);

            GUI.BeginGroup(layoutRect);

            Vector2 optionsSize = EditorStyles.toolbarButtonRight.CalcSize(AnimationWindowStyles.optionsContent);
            Rect optionsRect = new Rect(layoutRect.width - kSliderThickness, 0f, optionsSize.x, optionsSize.y);
            GUI.Box(optionsRect, GUIContent.none, AnimationWindowStyles.animPlayToolBar);
            if (EditorGUI.DropdownButton(controlID, optionsRect, AnimationWindowStyles.optionsContent, AnimationWindowStyles.optionsButton))
            {
                var menu = GenerateOptionsMenu();
                menu.ShowAsContext();
            }

            GUI.EndGroup();
        }

        internal void SetFrameRate(object frameRate)
        {
            m_State.clipFrameRate = (float)frameRate;
            UpdateSelectedKeysToCurveEditor();
        }

        private void FilterBySelectionButtonOnGUI()
        {
            Color backupColor = GUI.color;

            if (m_State.filterBySelection)
            {
                Color selectionColor = filterBySelectionColor;
                selectionColor.a *= GUI.color.a;
                GUI.color = selectionColor;
            }

            EditorGUI.BeginChangeCheck();
            bool filterBySelection = GUILayout.Toggle(m_State.filterBySelection, AnimationWindowStyles.filterBySelectionContent, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                m_State.filterBySelection = filterBySelection;
            }

            GUI.color = backupColor;
        }

        private void AddEventButtonOnGUI()
        {
            using (new EditorGUI.DisabledScope(!selection.animationIsEditable))
            {
                if (GUILayout.Button(AnimationWindowStyles.addEventContent, AnimationWindowStyles.animClipToolbarButton))
                    m_Events.AddEvent(m_State.currentTime, selection.rootGameObject, selection.animationClip);
            }
        }

        private void AddKeyframeButtonOnGUI()
        {
            bool canAddKey = selection.animationIsEditable && m_State.allCurves.Count != 0;
            using (new EditorGUI.DisabledScope(!canAddKey))
            {
                if (GUILayout.Button(AnimationWindowStyles.addKeyframeContent, AnimationWindowStyles.animClipToolbarButton))
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
                    m_State.selection = null;

                    // Layout has changed, bail out now.
                    EditorGUIUtility.ExitGUI();
                }
            }
        }

        static void ExecuteShortcut(ShortcutArguments args, Action<AnimEditor> exp)
        {
            var animationWindow = (AnimationWindow)args.context;
            var animEditor = animationWindow.animEditor;

            if (EditorWindow.focusedWindow != animationWindow)
                return;

            if (animEditor.stateDisabled || animEditor.state.animatorIsOptimized)
                return;

            exp(animEditor);

            animEditor.Repaint();
        }

        static void ExecuteShortcut(ShortcutArguments args, Action<IAnimationWindowControl> exp)
        {
            ExecuteShortcut(args, animEditor => exp(animEditor.controlInterface));
        }

        [FormerlyPrefKeyAs("Animation/Show Curves", "c")]
        [Shortcut("Animation/Show Curves", typeof(AnimationWindow), KeyCode.C)]
        static void ShowCurves(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.SwitchBetweenCurvesAndDopesheet(); });
        }

        [FormerlyPrefKeyAs("Animation/Play Animation", " ")]
        [Shortcut("Animation/Play Animation", typeof(AnimationWindow), KeyCode.Space)]
        static void TogglePlayAnimation(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface =>
            {
                if (controlInterface.playing)
                    controlInterface.StopPlayback();
                else
                    controlInterface.StartPlayback();
            });
        }

        [FormerlyPrefKeyAs("Animation/Next Frame", ".")]
        [Shortcut("Animation/Next Frame", typeof(AnimationWindow), KeyCode.Period)]
        static void NextFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface => controlInterface.GoToNextFrame());
        }

        [FormerlyPrefKeyAs("Animation/Previous Frame", ",")]
        [Shortcut("Animation/Previous Frame", typeof(AnimationWindow), KeyCode.Comma)]
        static void PreviousFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface => controlInterface.GoToPreviousFrame());
        }

        [FormerlyPrefKeyAs("Animation/Previous Keyframe", "&,")]
        [Shortcut("Animation/Previous Keyframe", typeof(AnimationWindow), KeyCode.Comma, ShortcutModifiers.Alt)]
        static void PreviousKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface => controlInterface.GoToPreviousKeyframe());
        }

        [FormerlyPrefKeyAs("Animation/Next Keyframe", "&.")]
        [Shortcut("Animation/Next Keyframe", typeof(AnimationWindow), KeyCode.Period, ShortcutModifiers.Alt)]
        static void NextKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface => controlInterface.GoToNextKeyframe());
        }

        [FormerlyPrefKeyAs("Animation/First Keyframe", "#,")]
        [Shortcut("Animation/First Keyframe", typeof(AnimationWindow), KeyCode.Comma, ShortcutModifiers.Shift)]
        static void FirstKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface => controlInterface.GoToFirstKeyframe());
        }

        [FormerlyPrefKeyAs("Animation/Last Keyframe", "#.")]
        [Shortcut("Animation/Last Keyframe", typeof(AnimationWindow), KeyCode.Period, ShortcutModifiers.Shift)]
        static void LastKeyFrame(ShortcutArguments args)
        {
            ExecuteShortcut(args, controlInterface => controlInterface.GoToLastKeyframe());
        }

        [FormerlyPrefKeyAs("Animation/Key Selected", "k")]
        [Shortcut("Animation/Key Selected", null, KeyCode.K)]
        static void KeySelected(ShortcutArguments args)
        {
            AnimationWindow animationWindow = AnimationWindow.GetAllAnimationWindows().Find(aw => (aw.state.previewing || aw == EditorWindow.focusedWindow));
            if (animationWindow == null)
                return;

            var animEditor = animationWindow.animEditor;

            animEditor.SaveCurveEditorKeySelection();
            AnimationWindowUtility.AddSelectedKeyframes(animEditor.m_State, animEditor.controlInterface.time);
            animEditor.controlInterface.ClearCandidates();
            animEditor.UpdateSelectedKeysToCurveEditor();

            animEditor.Repaint();
        }

        [FormerlyPrefKeyAs("Animation/Key Modified", "#k")]
        [Shortcut("Animation/Key Modified", null, KeyCode.K, ShortcutModifiers.Shift)]
        static void KeyModified(ShortcutArguments args)
        {
            AnimationWindow animationWindow = AnimationWindow.GetAllAnimationWindows().Find(aw => (aw.state.previewing || aw == EditorWindow.focusedWindow));
            if (animationWindow == null)
                return;

            var animEditor = animationWindow.animEditor;

            animEditor.SaveCurveEditorKeySelection();
            animEditor.controlInterface.ProcessCandidates();
            animEditor.UpdateSelectedKeysToCurveEditor();

            animEditor.Repaint();
        }

        [Shortcut("Animation/Toggle Ripple", typeof(AnimationWindow), KeyCode.Alpha2, ShortcutModifiers.Shift)]
        static void ToggleRipple(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.state.rippleTime = !animEditor.state.rippleTime; });
        }

        [ClutchShortcut("Animation/Ripple (Clutch)", typeof(AnimationWindow), KeyCode.Alpha2)]
        static void ClutchRipple(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.state.rippleTimeClutch = args.stage == ShortcutStage.Begin; });
        }

        [Shortcut("Animation/Frame All", typeof(AnimationWindow), KeyCode.A)]
        static void FrameAll(ShortcutArguments args)
        {
            ExecuteShortcut(args, animEditor => { animEditor.triggerFraming = true; });
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
            if (selection.animationClip != null)
            {
                m_State.frameRate = selection.animationClip.frameRate;
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
                    AnimationWindowUtility.SaveCurves(kvp.Key, kvp.Value.bindings, kvp.Value.curves);
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

        void HandleMainAreaCopyPaste(int controlID)
        {
            if (GUIUtility.keyboardControl != controlID)
                return;

            var evt = Event.current;
            var type = evt.GetTypeForControl(controlID);
            if (type != EventType.ValidateCommand && type != EventType.ExecuteCommand)
                return;

            if (evt.commandName == EventCommandNames.Copy)
            {
                if (type == EventType.ExecuteCommand)
                {
                    if (m_State.showCurveEditor)
                        UpdateSelectedKeysFromCurveEditor();
                    m_State.CopyKeys();
                }
                evt.Use();
            }
            else if (evt.commandName == EventCommandNames.Paste)
            {
                if (type == EventType.ExecuteCommand)
                {
                    SaveCurveEditorKeySelection();
                    m_State.PasteKeys();
                    UpdateSelectedKeysToCurveEditor();

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
            settings.flushCurveCache = false; // Curve Wrappers are cached in AnimationWindowState.

            m_CurveEditor.shownArea = new Rect(1, 1, 1, 1);
            m_CurveEditor.settings = settings;
            m_CurveEditor.state = m_State;
        }

        // Called once during initialization of m_State
        private void InitializeHorizontalSplitter()
        {
            m_HorizontalSplitter = SplitterState.FromRelative(new float[] { kHierarchyMinWidth, kHierarchyMinWidth * 3 }, new float[] { kHierarchyMinWidth, kHierarchyMinWidth }, null);
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
        }
    }
}
