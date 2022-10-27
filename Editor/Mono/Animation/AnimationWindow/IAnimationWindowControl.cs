// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    // Compatibility implementation of IAnimationWindowController.
    abstract class IAnimationWindowControl : ScriptableObject, IAnimationWindowController
    {
        [SerializeReference] AnimationWindowState m_State;

        public virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        public abstract void OnSelectionChanged();

        public void Init(AnimationWindowState state)
        {
            m_State = state;
        }

        void IAnimationWindowController.OnCreate(AnimationWindow animationWindow, UnityEngine.Component component)
        {
        }

        void IAnimationWindowController.OnDestroy()
        {
            // Animation Window has ownership of `IAnimationWindowControl` in legacy workflow.
            Object.DestroyImmediate(this);
        }

        float IAnimationWindowController.time
        {
            get => time.time;
            set => GoToTime(value);
        }

        int IAnimationWindowController.frame
        {
            get => time.frame;
            set => GoToFrame(value);
        }

        public abstract AnimationKeyTime time { get; }

        public abstract void GoToTime(float time);
        public abstract void GoToFrame(int frame);

        // Not used anymore. This was not used by either the AnimationWindow nor Timeline.
        // Replaced by setting IAnimationWindowController.time.
        public abstract void StartScrubTime();
        public abstract void ScrubTime(float time);
        public abstract void EndScrubTime();

        // Not used anymore. Required internal knowledge of the Animation Window and
        // default implementation should suffice.
        public abstract void GoToPreviousFrame();
        public abstract void GoToNextFrame();
        public abstract void GoToPreviousKeyframe();
        public abstract void GoToNextKeyframe();
        public abstract void GoToFirstKeyframe();
        public abstract void GoToLastKeyframe();

        public abstract bool canPlay { get; }

        bool IAnimationWindowController.playing
        {
            get => playing;
            set
            {
                if (value)
                    StartPlayback();
                else
                    StopPlayback();
            }
        }

        public abstract bool playing { get; }
        public abstract bool StartPlayback();
        public abstract void StopPlayback();
        public abstract bool PlaybackUpdate();

        public abstract bool canPreview { get; }

        bool IAnimationWindowController.previewing
        {
            get => previewing;
            set
            {
                if (value)
                    StartPreview();
                else
                    StopPreview();
            }
        }

        public abstract bool previewing { get; }
        public abstract bool StartPreview();
        public abstract void StopPreview();

        public abstract bool canRecord { get; }

        bool IAnimationWindowController.recording
        {
            get => recording;
            set
            {
                if (value)
                    StartRecording(null);
                else
                    StopRecording();
            }
        }

        public abstract bool recording { get; }
        // targetObject parameter is not used.
        public abstract bool StartRecording(Object targetObject);
        public abstract void StopRecording();

        public abstract void ResampleAnimation();

        public abstract void ProcessCandidates();
        public abstract void ClearCandidates();

        EditorCurveBinding[] IAnimationWindowController.GetAnimatableBindings()
        {
            if (m_State == null)
                return Array.Empty<EditorCurveBinding>();

            var rootGameObject = m_State.activeRootGameObject;
            var scriptableObject = m_State.activeScriptableObject;

            if (rootGameObject != null)
            {
                return AnimationWindowUtility.GetAnimatableBindings(rootGameObject);
            }
            if (scriptableObject != null)
            {
                return AnimationUtility.GetAnimatableBindings(scriptableObject);
            }

            return Array.Empty<EditorCurveBinding>();
        }

        EditorCurveBinding[] IAnimationWindowController.GetAnimatableBindings(GameObject gameObject)
        {
            if (m_State == null)
                return Array.Empty<EditorCurveBinding>();

            var rootGameObject = m_State.activeRootGameObject;
            return AnimationUtility.GetAnimatableBindings(gameObject, rootGameObject);
        }

        System.Type IAnimationWindowController.GetValueType(EditorCurveBinding binding)
        {
            if (m_State == null)
                return default;

            var rootGameObject = m_State.activeRootGameObject;
            var scriptableObject = m_State.activeScriptableObject;

            if (rootGameObject != null)
            {
                return AnimationUtility.GetEditorCurveValueType(rootGameObject, binding);
            }
            else if (scriptableObject != null)
            {
                return AnimationUtility.GetEditorCurveValueType(scriptableObject, binding);
            }
            else
            {
                if (binding.isPPtrCurve)
                {
                    // Cannot extract type of PPtrCurve.
                    return null;
                }
                else
                {
                    // Cannot extract type of AnimationCurve.  Default to float.
                    return typeof(float);
                }
            }
        }

        float IAnimationWindowController.GetFloatValue(EditorCurveBinding binding)
        {
            if (m_State == null)
                return default;

            AnimationUtility.GetFloatValue(m_State.activeRootGameObject, binding, out var value);
            return value;
        }

        int IAnimationWindowController.GetIntValue(EditorCurveBinding binding)
        {
            if (m_State == null)
                return default;

            AnimationUtility.GetDiscreteIntValue(m_State.activeRootGameObject, binding, out var value);
            return value;
        }

        UnityEngine.Object IAnimationWindowController.GetObjectReferenceValue(EditorCurveBinding binding)
        {
            if (m_State == null)
                return default;

            AnimationUtility.GetObjectReferenceValue(m_State.activeRootGameObject, binding, out var value);
            return value;
        }

    }
}
