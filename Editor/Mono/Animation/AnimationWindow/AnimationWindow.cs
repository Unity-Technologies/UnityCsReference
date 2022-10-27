// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Animation", useTypeNameAsIconName = true)]
    public sealed class AnimationWindow : EditorWindow, IHasCustomMenu
    {
        // Active Animation windows
        private static List<AnimationWindow> s_AnimationWindows = new List<AnimationWindow>();
        internal static List<AnimationWindow> GetAllAnimationWindows() { return s_AnimationWindows; }

        private static Type[] s_CustomControllerTypes = null;

        private AnimEditor m_AnimEditor;

        [SerializeField]
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        [SerializeField] private int m_LastSelectedObjectID;

        private GUIStyle m_LockButtonStyle;
        private GUIContent m_DefaultTitleContent;
        private GUIContent m_RecordTitleContent;

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

        public AnimationClip animationClip
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.activeAnimationClip;
                }
                return null;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.activeAnimationClip = value;
                }
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

        internal void ForceRefresh()
        {
            if (m_AnimEditor != null)
            {
                m_AnimEditor.state.ForceRefresh();
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

            OnSelectionChange();

            Undo.undoRedoEvent += UndoRedoPerformed;
        }

        void OnDisable()
        {
            s_AnimationWindows.Remove(this);
            m_AnimEditor.OnDisable();

            Undo.undoRedoEvent -= UndoRedoPerformed;
        }

        void OnDestroy()
        {
            DestroyImmediate(m_AnimEditor);
        }

        void Update()
        {
            if (m_AnimEditor == null)
                return;

            m_AnimEditor.Update();
        }

        void OnGUI()
        {
            if (m_AnimEditor == null)
                return;

            titleContent = m_AnimEditor.state.recording ? m_RecordTitleContent : m_DefaultTitleContent;
            m_AnimEditor.OnAnimEditorGUI(this, position);
        }

        internal void OnSelectionChange()
        {
            if (m_AnimEditor == null)
                return;

            Object activeObject = Selection.activeObject;

            bool restoringLockedSelection = false;
            if (m_LockTracker.isLocked && m_AnimEditor.stateDisabled)
            {
                activeObject = EditorUtility.InstanceIDToObject(m_LastSelectedObjectID);
                restoringLockedSelection = true;
                m_LockTracker.isLocked = false;
            }

            if (activeObject is GameObject activeGameObject)
            {
                EditGameObject(activeGameObject);
            }
            else
            {
                if (activeObject is Transform activeTransform)
                {
                    EditGameObject(activeTransform.gameObject);
                }
                else
                {
                    if (activeObject is AnimationClip activeAnimationClip)
                        EditAnimationClip(activeAnimationClip);
                }
            }

            if (restoringLockedSelection && !m_AnimEditor.stateDisabled)
            {
                m_LockTracker.isLocked = true;
            }
        }

        void OnFocus()
        {
            OnSelectionChange();
        }

        internal void OnControllerChange()
        {
            // Refresh selectedItem to update selected clips.
            OnSelectionChange();
        }

        void OnLostFocus()
        {
            if (m_AnimEditor != null)
                m_AnimEditor.OnLostFocus();
        }

        [Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            var clip = EditorUtility.InstanceIDToObject(instanceID) as AnimationClip;
            if (clip)
            {
                EditorWindow.GetWindow<AnimationWindow>();
                return true;
            }
            return false;
        }

        internal bool EditGameObject(GameObject gameObject)
        {
            if (EditorUtility.IsPersistent(gameObject))
                return false;

            if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            var newSelection = GameObjectSelectionItem.Create(gameObject);
            if (ShouldUpdateGameObjectSelection(newSelection))
            {
                m_AnimEditor.selection = newSelection;

                IAnimationWindowController controller = null;

                var rootGameObject = newSelection.rootGameObject;
                if (rootGameObject != null)
                    controller = FindCustomController(rootGameObject);

                m_AnimEditor.overrideControlInterface = controller;

                m_LastSelectedObjectID = gameObject != null ? gameObject.GetInstanceID() : 0;
            }
            else
                m_AnimEditor.OnSelectionUpdated();

            return true;
        }

        internal bool EditAnimationClip(AnimationClip animationClip)
        {
            if (state.linkedWithSequencer == true)
                return false;

            EditAnimationClipInternal(animationClip, (Object)null, (IAnimationWindowController)null);
            return true;
        }

        internal bool EditSequencerClip(AnimationClip animationClip, Object sourceObject, IAnimationWindowControl controlInterface)
        {
            EditAnimationClipInternal(animationClip, sourceObject, controlInterface);
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
                EditAnimationClip(null);
                OnSelectionChange();
            }
        }

        private IAnimationWindowController FindCustomController(GameObject gameObject)
        {
            IAnimationWindowController controller = null;

            if (s_CustomControllerTypes == null)
            {
                s_CustomControllerTypes = TypeCache
                    .GetTypesWithAttribute<AnimationWindowControllerAttribute>()
                    .Where(type => typeof(IAnimationWindowController).IsAssignableFrom(type))
                    .ToArray();
            }

            foreach (var controllerType in s_CustomControllerTypes)
            {
                var attribute = controllerType.GetCustomAttribute<AnimationWindowControllerAttribute>();
                var component = gameObject.GetComponent(attribute.componentType);

                if (component != null)
                {
                    controller = (IAnimationWindowController)Activator.CreateInstance(controllerType);
                    controller.OnCreate(this, component);
                    break;
                }
            }

            return controller;
        }

        private void EditAnimationClipInternal(AnimationClip animationClip, Object sourceObject, IAnimationWindowController controlInterface)
        {
            var newSelection = AnimationClipSelectionItem.Create(animationClip, sourceObject);
            if (ShouldUpdateSelection(newSelection))
            {
                m_AnimEditor.selection = newSelection;
                m_AnimEditor.overrideControlInterface = controlInterface;

                m_LastSelectedObjectID = animationClip != null ? animationClip.GetInstanceID() : 0;
            }
            else
                m_AnimEditor.OnSelectionUpdated();
        }

        void ShowButton(Rect r)
        {
            if (m_LockButtonStyle == null)
                m_LockButtonStyle = "IN LockButton";

            EditorGUI.BeginChangeCheck();

            m_LockTracker.ShowButton(r, m_LockButtonStyle, m_AnimEditor.stateDisabled);

            // Selected object could have been changed when unlocking the animation window
            if (EditorGUI.EndChangeCheck())
                OnSelectionChange();
        }

        private bool ShouldUpdateGameObjectSelection(GameObjectSelectionItem selectedItem)
        {
            if (m_LockTracker.isLocked)
                return false;

            if (state.linkedWithSequencer)
                return false;

            // Selected game object with no animation player.
            if (selectedItem.rootGameObject == null)
                return true;

            AnimationWindowSelectionItem currentSelection = m_AnimEditor.selection;

            // Game object holding animation player has changed.  Update selection.
            if (selectedItem.rootGameObject != currentSelection.rootGameObject)
                return true;

            // No clip in current selection, favour new selection.
            if (currentSelection.animationClip == null)
                return true;

            // Make sure that animation clip is still referenced in animation player.
            if (currentSelection.rootGameObject != null)
            {
                AnimationClip[] allClips = AnimationUtility.GetAnimationClips(currentSelection.rootGameObject);
                if (!Array.Exists(allClips, x => x == currentSelection.animationClip))
                    return true;
            }

            return false;
        }

        private bool ShouldUpdateSelection(AnimationWindowSelectionItem selectedItem)
        {
            if (m_LockTracker.isLocked)
                return false;

            AnimationWindowSelectionItem currentSelection = m_AnimEditor.selection;
            return (selectedItem.GetRefreshHash() != currentSelection.GetRefreshHash());
        }

        private void UndoRedoPerformed(in UndoRedoInfo info)
        {
            Repaint();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu, m_AnimEditor.stateDisabled);
        }
    }
}
