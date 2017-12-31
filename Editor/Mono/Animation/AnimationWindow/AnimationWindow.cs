// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Animation", useTypeNameAsIconName = true)]
    internal class AnimationWindow : EditorWindow, IHasCustomMenu
    {
        // Active Animation windows
        private static List<AnimationWindow> s_AnimationWindows = new List<AnimationWindow>();
        public static List<AnimationWindow> GetAllAnimationWindows() { return s_AnimationWindows; }

        [SerializeField] private AnimEditor m_AnimEditor;

        [SerializeField]
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        [SerializeField] private int m_LastSelectedObjectID;

        private GUIStyle m_LockButtonStyle;
        private GUIContent m_DefaultTitleContent;
        private GUIContent m_RecordTitleContent;

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

        public void ForceRefresh()
        {
            if (m_AnimEditor != null)
            {
                m_AnimEditor.state.ForceRefresh();
            }
        }

        public void OnEnable()
        {
            if (m_AnimEditor == null)
            {
                m_AnimEditor = CreateInstance(typeof(AnimEditor)) as AnimEditor;
                m_AnimEditor.hideFlags = HideFlags.HideAndDontSave;
            }

            s_AnimationWindows.Add(this);
            titleContent = GetLocalizedTitleContent();

            m_DefaultTitleContent = titleContent;
            m_RecordTitleContent = EditorGUIUtility.TextContentWithIcon(titleContent.text, "Animation.Record");

            OnSelectionChange();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void OnDisable()
        {
            s_AnimationWindows.Remove(this);
            m_AnimEditor.OnDisable();

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public void OnDestroy()
        {
            DestroyImmediate(m_AnimEditor);
        }

        public void Update()
        {
            m_AnimEditor.Update();
        }

        public void OnGUI()
        {
            Profiler.BeginSample("AnimationWindow.OnGUI");
            titleContent = m_AnimEditor.state.recording ? m_RecordTitleContent : m_DefaultTitleContent;
            m_AnimEditor.OnAnimEditorGUI(this, position);
            Profiler.EndSample();
        }

        public void OnSelectionChange()
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

            GameObject activeGameObject = activeObject as GameObject;
            if (activeGameObject != null)
            {
                EditGameObject(activeGameObject);
            }
            else
            {
                AnimationClip activeAnimationClip = activeObject as AnimationClip;
                if (activeAnimationClip != null)
                    EditAnimationClip(activeAnimationClip);
            }

            if (restoringLockedSelection && !m_AnimEditor.stateDisabled)
            {
                m_LockTracker.isLocked = true;
            }
        }

        public void OnFocus()
        {
            OnSelectionChange();
        }

        public void OnControllerChange()
        {
            // Refresh selectedItem to update selected clips.
            OnSelectionChange();
        }

        public void OnLostFocus()
        {
            if (m_AnimEditor != null)
                m_AnimEditor.OnLostFocus();
        }

        public bool EditGameObject(GameObject gameObject)
        {
            if (state.linkedWithSequencer == true)
                return false;

            return EditGameObjectInternal(gameObject, (IAnimationWindowControl)null);
        }

        public bool EditAnimationClip(AnimationClip animationClip)
        {
            if (state.linkedWithSequencer == true)
                return false;

            return EditAnimationClipInternal(animationClip, (Object)null, (IAnimationWindowControl)null);
        }

        public bool EditSequencerClip(AnimationClip animationClip, Object sourceObject, IAnimationWindowControl controlInterface)
        {
            if (EditAnimationClipInternal(animationClip, sourceObject, controlInterface))
            {
                state.linkedWithSequencer = true;
                return true;
            }

            return false;
        }

        public void UnlinkSequencer()
        {
            if (state.linkedWithSequencer)
            {
                state.linkedWithSequencer = false;

                // Selected object could have been changed when unlocking the animation window
                EditAnimationClip(null);
                OnSelectionChange();
            }
        }

        private bool EditGameObjectInternal(GameObject gameObject, IAnimationWindowControl controlInterface)
        {
            if (EditorUtility.IsPersistent(gameObject))
                return false;

            if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            var newSelection = GameObjectSelectionItem.Create(gameObject);
            if (ShouldUpdateGameObjectSelection(newSelection))
            {
                m_AnimEditor.selection = newSelection;
                m_AnimEditor.overrideControlInterface = controlInterface;

                m_LastSelectedObjectID = gameObject != null ? gameObject.GetInstanceID() : 0;
            }
            else
                return false;

            return true;
        }

        private bool EditAnimationClipInternal(AnimationClip animationClip, Object sourceObject, IAnimationWindowControl controlInterface)
        {
            var newSelection = AnimationClipSelectionItem.Create(animationClip, sourceObject);
            if (ShouldUpdateSelection(newSelection))
            {
                m_AnimEditor.selection = newSelection;
                m_AnimEditor.overrideControlInterface = controlInterface;

                m_LastSelectedObjectID = animationClip != null ? animationClip.GetInstanceID() : 0;
            }
            else
                return false;

            return true;
        }

        protected virtual void ShowButton(Rect r)
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

        private void UndoRedoPerformed()
        {
            Repaint();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu, m_AnimEditor.stateDisabled);
        }
    }
}
