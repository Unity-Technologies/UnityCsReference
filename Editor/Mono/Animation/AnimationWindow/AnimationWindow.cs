// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityObject = UnityEngine.Object;
using UnityEditorInternal;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Animation", useTypeNameAsIconName = true)]
    public sealed partial class AnimationWindow : EditorWindow, IHasCustomMenu
    {
        // Active Animation windows
        static readonly List<AnimationWindow> s_AnimationWindows = new List<AnimationWindow>();
        internal static List<AnimationWindow> GetAllAnimationWindows() { return s_AnimationWindows; }

        static readonly List<IAnimationWindowResponder> s_Responders = new List<IAnimationWindowResponder>();

        [InitializeOnLoadMethod]
        static void InitializeResponders()
        {
            var responderTypes = TypeCache.GetTypesDerivedFrom<IAnimationWindowResponder>();

            foreach (var responderType in responderTypes)
            {
                if (responderType.IsAbstract)
                    continue;

                var responder = (IAnimationWindowResponder)Activator.CreateInstance(responderType);
                s_Responders.Add(responder);
            }
        }

        AnimEditor m_AnimEditor;

        [SerializeField]
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        [SerializeField] private int m_LastSelectedObjectID;

        GUIStyle m_LockButtonStyle;
        GUIContent m_DefaultTitleContent;
        GUIContent m_RecordTitleContent;

        internal AnimEditor animEditor => m_AnimEditor;

        internal AnimationWindowState state
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state;
                }
                return null;
            }
        }

        internal IAnimationWindowClip clip
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.activeClip;
                }
                return null;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.activeClip = value;
                }
            }
        }

        public AnimationClip animationClip
        {
            get => clip is UnityEditor.AnimationWindowBuiltin.AnimationWindowClip builtinClip ? builtinClip.animationClip : null;
            set => clip = new UnityEditor.AnimationWindowBuiltin.AnimationWindowClip(value, m_AnimEditor.state.activeRootGameObject);
        }

        internal IAnimationWindowSelectionItem selection
        {
            get
            {
                if (m_AnimEditor != null)
                    return m_AnimEditor.selection;
                return null;
            }
            set
            {
                if (m_AnimEditor == null)
                    return;
                m_AnimEditor.selection = value;
            }
        }

        public bool previewing
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.previewing;
                }
                return false;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.previewing = value;
                }
            }
        }

        public bool canPreview
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.canPreview;
                }

                return false;
            }
        }

        public bool recording
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.recording;
                }
                return false;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.recording = value;
                }
            }
        }

        public bool canRecord
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.canRecord;
                }

                return false;
            }
        }

        public bool playing
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.playing;
                }
                return false;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.playing = value;
                }
            }
        }

        public float time
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.currentTime;
                }
                return 0.0f;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.currentTime = value;
                }
            }
        }

        public int frame
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.currentFrame;
                }
                return 0;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.currentFrame = value;
                }
            }
        }

        private AnimationWindow()
        {}

        internal void RefreshCurve(EditorCurveBinding binding)
        {
            if (m_AnimEditor != null)
            {
                m_AnimEditor.state.RefreshCurve(binding);
            }
        }

        internal void RefreshClip()
        {
            if (m_AnimEditor != null)
            {
                m_AnimEditor.state.RefreshClip();
            }
        }

        void OnEnable()
        {
            if (m_AnimEditor == null)
            {
                m_AnimEditor = CreateInstance<AnimEditor>();
                m_AnimEditor.hideFlags = HideFlags.HideAndDontSave;
            }

            s_AnimationWindows.Add(this);
            titleContent = GetLocalizedTitleContent();

            m_DefaultTitleContent = titleContent;
            m_RecordTitleContent = EditorGUIUtility.TextContentWithIcon(titleContent.text, "Animation.Record");

            OnSelectionChangeInternal(false);

            Undo.undoRedoEvent += UndoRedoPerformed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable()
        {
            s_AnimationWindows.Remove(this);
            m_AnimEditor.OnDisable();

            Undo.undoRedoEvent -= UndoRedoPerformed;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnDestroy()
        {
            DestroyImmediate(m_AnimEditor);
        }

        void Update()
        {
            if (m_AnimEditor != null)
                m_AnimEditor.Update();

            hasUnsavedChanges = selection?.hasUnsavedChanges ?? false;
        }

        void OnGUI()
        {
            if (m_AnimEditor == null)
                return;

            titleContent = m_AnimEditor.state.recording ? m_RecordTitleContent : m_DefaultTitleContent;
            m_AnimEditor.OnAnimEditorGUI(this, position);
        }

        internal void OnSelectionChange() =>
            OnSelectionChangeInternal(true);

        internal void OnDidOpenScene() =>
            OnSelectionChangeInternal(true);

        void OnSelectionChangeInternal(bool fromCallback)
        {
            if (m_AnimEditor == null)
                return;

            UnityObject activeObject = Selection.activeObject;

            bool restoringLockedSelection = false;
            if (m_LockTracker.isLocked && m_AnimEditor.stateDisabled)
            {
                activeObject = EditorUtility.EntityIdToObject(m_LastSelectedObjectID);
                restoringLockedSelection = true;
                m_LockTracker.isLocked = false;
            }

            if (m_LockTracker.isLocked || state.linkedWithSequencer)
            {
                OnSelectionUpdated();
            }
            else
            {
                bool selectionChanged = false;
                foreach (var responder in s_Responders)
                {
                    if (responder.OnSelectionChange(this, activeObject, out var newSelection))
                    {
                        if (selection == newSelection)
                            OnSelectionUpdated();
                        else if (fromCallback && DisplayUnsavedChangesDialogIfNecessary())
                            selection = newSelection;
                        else
                        {
                            selection = newSelection;
                            var lastSelectedObject = EditorUtility.EntityIdToObject(m_LastSelectedObjectID);
                            if (lastSelectedObject != null)
                            {
                                activeObject = lastSelectedObject;
                                Selection.activeObject = activeObject;
                            }
                        }

                        m_LastSelectedObjectID = activeObject != null ? activeObject.GetInstanceID() : 0;
                        selectionChanged = true;
                        break;
                    }
                }

                // Fallback selection responder
                if (!selectionChanged)
                {
                    var fallbackSelection = GetFallbackSelection(activeObject);
                    if (selection == fallbackSelection)
                        OnSelectionUpdated();
                    else if (fromCallback && DisplayUnsavedChangesDialogIfNecessary())
                        selection = fallbackSelection;
                    else
                    {
                        selection = fallbackSelection;
                        var lastSelectedObject = EditorUtility.EntityIdToObject(m_LastSelectedObjectID);
                        if (lastSelectedObject != null)
                        {
                            activeObject = lastSelectedObject;
                            Selection.activeObject = activeObject;
                        }
                    }

                    m_LastSelectedObjectID = activeObject != null ? activeObject.GetInstanceID() : 0;
                }
            }

            if (restoringLockedSelection && !m_AnimEditor.stateDisabled)
            {
                m_LockTracker.isLocked = true;
            }
        }

        internal void OnSelectionUpdated()
        {
            if (m_AnimEditor != null)
                m_AnimEditor.OnSelectionUpdated();
        }

        void OnFocus()
        {
            OnSelectionChangeInternal(false);
        }

        internal void OnControllerChange()
        {
            // Refresh selectedItem to update selected clips.
            OnSelectionChangeInternal(false);
        }

        void OnLostFocus()
        {
            if (m_AnimEditor != null)
                m_AnimEditor.OnLostFocus();
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            var clip = EditorUtility.EntityIdToObject(instanceID) as AnimationClip;
            if (clip)
            {
                EditorWindow.GetWindow<AnimationWindow>();
                return true;
            }
            return false;
        }

        IAnimationWindowSelectionItem GetFallbackSelection(UnityObject selectedObject)
        {
            if (selectedObject is GameObject gameObject)
            {
                if (!EditorUtility.IsPersistent(gameObject) &&
                    (gameObject.hideFlags & HideFlags.NotEditable) == 0)
                {
                    var fallbackSelection = new FallbackSelectionItem();
                    fallbackSelection.gameObject = gameObject;
                    return fallbackSelection;
                }
            }

            if (m_AnimEditor.stateDisabled)
                return new FallbackSelectionItem();

            return selection;
        }

        internal bool EditSequencerClip(AnimationClip animationClip, UnityObject sourceObject, IAnimationWindowControl controlInterface)
        {
            var newSelection = UnityEditor.AnimationWindowBuiltin.AnimationClipSelectionItem.Create(this, animationClip, sourceObject);

            newSelection.controller = controlInterface != null ? controlInterface : newSelection.controller;
            m_AnimEditor.selection = newSelection;
            m_LastSelectedObjectID = animationClip != null ? animationClip.GetInstanceID() : 0;

            state.linkedWithSequencer = true;

            if (controlInterface != null)
                controlInterface.Init(state);

            return true;
        }

        internal void UnlinkSequencer()
        {
            if (state.linkedWithSequencer)
            {
                state.linkedWithSequencer = false;

                // Selected object could have been changed when unlocking the animation window
                m_AnimEditor.selection = new FallbackSelectionItem();
                OnSelectionChangeInternal(false);
            }
        }

        void ShowButton(Rect r)
        {
            if (m_LockButtonStyle == null)
                m_LockButtonStyle = "IN LockButton";

            EditorGUI.BeginChangeCheck();

            m_LockTracker.ShowButton(r, m_LockButtonStyle, m_AnimEditor.stateDisabled);

            // Selected object could have been changed when unlocking the animation window
            if (EditorGUI.EndChangeCheck())
                OnSelectionChangeInternal(false);
        }

        private void UndoRedoPerformed(in UndoRedoInfo info)
        {
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (!DisplayUnsavedChangesDialogIfNecessary())
                {
                    EditorApplication.ExitPlaymode();
                }
                else
                {
                    selection?.OnPlayModeStateChanged(state);
                }
            }
            else
            {
                selection?.OnPlayModeStateChanged(state);

                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    // Reload selection
                    OnSelectionChangeInternal(false);
                }
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu, m_AnimEditor.stateDisabled);
        }

        public override void SaveChanges()
        {
            selection?.SaveChanges();
        }

        public override void DiscardChanges()
        {
            selection?.DiscardChanges();
        }

        /// <summary>
        /// Display the unsaved changes dialog window if there are unsaved changes present on the currently
        /// active clip. Otherwise, allow the selection of the Animation Window to change.
        /// </summary>
        /// <returns>True, if the selection is allowed to change</returns>
        bool DisplayUnsavedChangesDialogIfNecessary()
        {

            // If there are no unsaved changes, do not display the dialog
            // and allow selection to change.
            if (!hasUnsavedChanges)
                return true;

            return animEditor.DisplayUnsavedChangesDialogIfNecessary();
        }
    }
}
