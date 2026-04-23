// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityObject = UnityEngine.Object;
using UnityEditorInternal;

using AnimationWindowLayout = UnityEditor.Animations.AnimationWindow.Widgets.Layout;

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

        AnimationWindowLayout m_Layout;

        [SerializeField]
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        [SerializeField] private EntityId m_LastSelectedObjectID;

        GUIStyle m_LockButtonStyle;

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
            get => state?.activeClip;
            set
            {
                if (state != null)
                {
                    state.activeClip = value;
                }
            }
        }

        public AnimationClip animationClip
        {
            get => clip is UnityEditor.AnimationWindowBuiltin.AnimationWindowClip builtinClip ? builtinClip.animationClip : null;
            set => clip = new UnityEditor.AnimationWindowBuiltin.AnimationWindowClip(value, state.activeRootGameObject);
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal IAnimationWindowSelectionItem selection
        {
            get => state?.selection;
            set
            {
                if (state != null)
                {
                    state.selection = value;
                }
            }
        }

        public bool previewing
        {
            get => state?.previewing ?? false;
            set
            {
                if (state != null)
                {
                    state.previewing = value;
                }
            }
        }

        public bool canPreview => state?.canPreview ?? false;

        public bool recording
        {
            get => state?.recording ?? false;
            set
            {
                if (state != null)
                {
                    state.recording = value;
                }
            }
        }

        public bool canRecord => state?.canRecord ?? false;

        public bool playing
        {
            get => state?.playing ?? false;
            set
            {
                if (state != null)
                {
                    state.playing = value;
                }
            }
        }

        public float time
        {
            get => state?.currentTime ?? 0.0f;
            set
            {
                if (state != null)
                {
                    state.currentTime = value;
                }
            }
        }

        public int frame
        {
            get => state?.currentFrame ?? 0;
            set
            {
                if (state != null)
                {
                    state.currentFrame = value;
                }
            }
        }

        private AnimationWindow()
        {}

        internal void RefreshCurve(EditorCurveBinding binding)
        {
            state?.RefreshCurve(binding);
        }

        internal void RefreshClip()
        {
            state?.RefreshClip();
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            if (m_AnimEditor == null)
            {
                m_AnimEditor = CreateInstance<AnimEditor>();
                m_AnimEditor.SetOwnerWindow(this);
                m_AnimEditor.hideFlags = HideFlags.HideAndDontSave;
            }

            s_AnimationWindows.Add(this);

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
            state?.Update();

            if (m_AnimEditor != null)
                m_AnimEditor.Update();

            hasUnsavedChanges = selection?.hasUnsavedChanges ?? false;

            m_Layout?.Update();
        }

        void CreateGUI()
        {
            m_Layout = new AnimationWindowLayout(m_AnimEditor);
            rootVisualElement.Add(m_Layout);
        }

        internal void OnSelectionChange() =>
            OnSelectionChangeInternal(true);

        internal void OnDidOpenScene() =>
            OnSelectionChangeInternal(true);

        void OnSelectionChangeInternal(bool fromCallback)
        {
            if (state == null)
                return;

            UnityObject activeObject = Selection.activeObject;

            bool restoringLockedSelection = false;
            if (m_LockTracker.isLocked && state.disabled)
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
                        else if (fromCallback)
                        {
                            // Handle unsaved changes.
                            if (DisplayUnsavedChangesDialogIfNecessary())
                                selection = newSelection;
                            // Fallback to last selected object if changes were canceled
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
                        }
                        else
                        {
                            selection = newSelection;
                        }

                        m_LastSelectedObjectID = activeObject != null ? activeObject.GetEntityId() : EntityId.None;
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
                    else if (fromCallback)
                    {
                        // Handle unsaved changes.
                        if (DisplayUnsavedChangesDialogIfNecessary())
                            selection = fallbackSelection;
                        // Fallback to last selected object if changes were canceled
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
                    }
                    else
                    {
                        selection = fallbackSelection;
                    }

                    m_LastSelectedObjectID = activeObject != null ? activeObject.GetEntityId() : EntityId.None;
                }
            }

            if (restoringLockedSelection && !state.disabled)
            {
                m_LockTracker.isLocked = true;
            }
        }

        internal void OnSelectionUpdated()
        {
            state?.OnSelectionUpdated();
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
        static bool OnOpenAsset(EntityId entityId, int line)
        {
            var clip = EditorUtility.EntityIdToObject(entityId) as AnimationClip;
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

            if (state.disabled)
                return new FallbackSelectionItem();

            return selection;
        }

        internal bool EditSequencerClip(AnimationClip animationClip, UnityObject sourceObject, IAnimationWindowControl controlInterface)
        {
            var newSelection = UnityEditor.AnimationWindowBuiltin.AnimationClipSelectionItem.Create(this, animationClip, sourceObject);

            newSelection.controller = controlInterface != null ? controlInterface : newSelection.controller;
            state.selection = newSelection;
            m_LastSelectedObjectID = animationClip != null ? animationClip.GetEntityId() : EntityId.None;

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
                state.selection = new FallbackSelectionItem();
                OnSelectionChangeInternal(false);
            }
        }

        void ShowButton(Rect r)
        {
            if (m_LockButtonStyle == null)
                m_LockButtonStyle = "IN LockButton";

            EditorGUI.BeginChangeCheck();

            m_LockTracker.ShowButton(r, m_LockButtonStyle, state.disabled);

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
                    // Reload selection when exiting play mode
                    OnSelectionChangeInternal(false);
                }

                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    // Reload selection when entering play mode
                    OnSelectionChangeInternal(false);
                }
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu, state.disabled);
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
