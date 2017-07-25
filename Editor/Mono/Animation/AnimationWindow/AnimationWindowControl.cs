// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AnimationWindowControl : IAnimationWindowControl, IAnimationContextualResponder
    {
        class CandidateRecordingState : IAnimationRecordingState
        {
            public GameObject activeGameObject { get; private set; }
            public GameObject activeRootGameObject { get; private set; }
            public AnimationClip activeAnimationClip { get; private set; }
            public int currentFrame { get { return 0; } }

            public bool addZeroFrame { get { return false; } }

            public CandidateRecordingState(AnimationWindowState state, AnimationClip candidateClip)
            {
                activeGameObject = state.activeGameObject;
                activeRootGameObject = state.activeRootGameObject;
                activeAnimationClip = candidateClip;
            }

            public bool DiscardModification(PropertyModification modification)
            {
                return !AnimationMode.IsPropertyAnimated(modification.target, modification.propertyPath);
            }

            public void SaveCurve(AnimationWindowCurve curve)
            {
                Undo.RegisterCompleteObjectUndo(curve.clip, "Edit Candidate Curve");
                AnimationRecording.SaveModifiedCurve(curve, curve.clip);
            }

            public void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride)
            {
                AnimationMode.AddCandidate(binding, propertyModification, keepPrefabOverride);
            }
        }

        enum RecordingStateMode
        {
            ManualKey,
            AutoKey
        };

        class RecordingState : IAnimationRecordingState
        {
            private AnimationWindowState m_State;
            private RecordingStateMode m_Mode;

            public GameObject activeGameObject { get { return m_State.activeGameObject; } }
            public GameObject activeRootGameObject { get { return m_State.activeRootGameObject; } }
            public AnimationClip activeAnimationClip { get { return m_State.activeAnimationClip; } }
            public int currentFrame { get { return m_State.currentFrame; } }

            public bool addZeroFrame { get { return (m_Mode == RecordingStateMode.AutoKey); } }
            public bool addPropertyModification { get { return m_State.previewing; } }

            public RecordingState(AnimationWindowState state, RecordingStateMode mode)
            {
                m_State = state;
                m_Mode = mode;
            }

            public bool DiscardModification(PropertyModification modification)
            {
                return false;
            }

            public void SaveCurve(AnimationWindowCurve curve)
            {
                m_State.SaveCurve(curve);
            }

            public void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride)
            {
                AnimationMode.AddPropertyModification(binding, propertyModification, keepPrefabOverride);
            }
        }

        [SerializeField] private AnimationKeyTime m_Time;

        [NonSerialized] private float m_PreviousUpdateTime;

        [NonSerialized] public AnimationWindowState state;
        public AnimEditor animEditor { get { return state.animEditor; } }

        [SerializeField] private AnimationClip m_CandidateClip;

        [SerializeField] private AnimationModeDriver m_Driver;
        [SerializeField] private AnimationModeDriver m_CandidateDriver;

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public void OnDisable()
        {
            StopPreview();
            StopPlayback();

            if (AnimationMode.InAnimationMode(GetAnimationModeDriver()))
                AnimationMode.StopAnimationMode(GetAnimationModeDriver());
        }

        public override void OnSelectionChanged()
        {
            // Set back time at beginning and stop recording.
            if (state != null)
                m_Time = AnimationKeyTime.Time(0f, state.frameRate);

            StopPreview();
            StopPlayback();
        }

        public override AnimationKeyTime time
        {
            get
            {
                return m_Time;
            }
        }

        public override void GoToTime(float time)
        {
            SetCurrentTime(time);
        }

        public override void GoToFrame(int frame)
        {
            SetCurrentFrame(frame);
        }

        public override void StartScrubTime()
        {
            // nothing to do...
        }

        public override void ScrubTime(float time)
        {
            SetCurrentTime(time);
        }

        public override void EndScrubTime()
        {
            // nothing to do...
        }

        public override void GoToPreviousFrame()
        {
            SetCurrentFrame(time.frame - 1);
        }

        public override void GoToNextFrame()
        {
            SetCurrentFrame(time.frame + 1);
        }

        public override void GoToPreviousKeyframe()
        {
            List<AnimationWindowCurve> curves = (state.showCurveEditor && state.activeCurves.Count > 0) ? state.activeCurves : state.allCurves;

            float newTime = AnimationWindowUtility.GetPreviousKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, AnimationWindowState.SnapMode.SnapToClipFrame));
        }

        public void GoToPreviousKeyframe(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = AnimationWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return;

            List<AnimationWindowCurve> curves = new List<AnimationWindowCurve>();
            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                AnimationWindowCurve curve = state.allCurves[i];
                if (Array.Exists(bindings, binding => curve.binding.Equals(binding)))
                    curves.Add(curve);
            }

            float newTime = AnimationWindowUtility.GetPreviousKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, AnimationWindowState.SnapMode.SnapToClipFrame));

            state.Repaint();
        }

        public override void GoToNextKeyframe()
        {
            List<AnimationWindowCurve> curves = (state.showCurveEditor && state.activeCurves.Count > 0) ? state.activeCurves : state.allCurves;

            float newTime = AnimationWindowUtility.GetNextKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, AnimationWindowState.SnapMode.SnapToClipFrame));
        }

        public void GoToNextKeyframe(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = AnimationWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return;

            List<AnimationWindowCurve> curves = new List<AnimationWindowCurve>();
            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                AnimationWindowCurve curve = state.allCurves[i];
                if (Array.Exists(bindings, binding => curve.binding.Equals(binding)))
                    curves.Add(curve);
            }

            float newTime = AnimationWindowUtility.GetNextKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, AnimationWindowState.SnapMode.SnapToClipFrame));

            state.Repaint();
        }

        public override void GoToFirstKeyframe()
        {
            if (state.activeAnimationClip)
                SetCurrentTime(state.activeAnimationClip.startTime);
        }

        public override void GoToLastKeyframe()
        {
            if (state.activeAnimationClip)
                SetCurrentTime(state.activeAnimationClip.stopTime);
        }

        private void SnapTimeToFrame()
        {
            float newTime = state.FrameToTime(time.frame);
            SetCurrentTime(newTime);
        }

        private void SetCurrentTime(float value)
        {
            if (!Mathf.Approximately(value, time.time))
            {
                m_Time = AnimationKeyTime.Time(value, state.frameRate);
                StartPreview();
                ClearCandidates();
                ResampleAnimation();
            }
        }

        private void SetCurrentFrame(int value)
        {
            if (value != time.frame)
            {
                m_Time = AnimationKeyTime.Frame(value, state.frameRate);
                StartPreview();
                ClearCandidates();
                ResampleAnimation();
            }
        }

        public override bool canPlay
        {
            get
            {
                return canPreview;
            }
        }

        public override bool playing
        {
            get
            {
                return AnimationMode.InAnimationPlaybackMode() && previewing;
            }
        }

        public override bool StartPlayback()
        {
            if (!canPlay)
                return false;

            if (!playing)
            {
                AnimationMode.StartAnimationPlaybackMode();

                m_PreviousUpdateTime = Time.realtimeSinceStartup;

                // Auto-Preview when start playing
                StartPreview();
                ClearCandidates();
            }

            return true;
        }

        public override void StopPlayback()
        {
            if (AnimationMode.InAnimationPlaybackMode())
            {
                AnimationMode.StopAnimationPlaybackMode();

                // Snap to frame when playing stops
                SnapTimeToFrame();
            }
        }

        public override bool PlaybackUpdate()
        {
            if (!playing)
                return false;

            float deltaTime = Time.realtimeSinceStartup - m_PreviousUpdateTime;
            m_PreviousUpdateTime = Time.realtimeSinceStartup;

            float newTime = time.time + deltaTime;

            // looping
            if (newTime > state.maxTime)
                newTime = state.minTime;

            m_Time = AnimationKeyTime.Time(Mathf.Clamp(newTime, state.minTime, state.maxTime), state.frameRate);

            ResampleAnimation();

            return true;
        }

        public override bool canPreview
        {
            get
            {
                if (!state.selection.canPreview)
                    return false;

                return AnimationMode.InAnimationMode(GetAnimationModeDriver()) || !AnimationMode.InAnimationMode();
            }
        }

        public override bool previewing
        {
            get
            {
                return AnimationMode.InAnimationMode(GetAnimationModeDriver());
            }
        }

        public override bool StartPreview()
        {
            if (previewing)
                return true;

            if (!canPreview)
                return false;

            AnimationMode.StartAnimationMode(GetAnimationModeDriver());
            AnimationPropertyContextualMenu.Instance.SetResponder(this);
            Undo.postprocessModifications += PostprocessAnimationRecordingModifications;
            return true;
        }

        public override void StopPreview()
        {
            StopPlayback();
            StopRecording();
            ClearCandidates();

            AnimationMode.StopAnimationMode(GetAnimationModeDriver());

            // reset responder only if we have set it
            if (AnimationPropertyContextualMenu.Instance.IsResponder(this))
            {
                AnimationPropertyContextualMenu.Instance.SetResponder(null);
            }

            Undo.postprocessModifications -= PostprocessAnimationRecordingModifications;
        }

        public override bool canRecord
        {
            get
            {
                if (!state.selection.canRecord)
                    return false;

                return canPreview;
            }
        }

        public override bool recording
        {
            get
            {
                if (previewing)
                    return AnimationMode.InAnimationRecording();
                return false;
            }
        }

        public override bool StartRecording(Object targetObject)
        {
            return StartRecording();
        }

        private bool StartRecording()
        {
            if (recording)
                return true;

            if (!canRecord)
                return false;

            if (StartPreview())
            {
                AnimationMode.StartAnimationRecording();
                ClearCandidates();
                return true;
            }

            return false;
        }

        public override void StopRecording()
        {
            if (recording)
            {
                AnimationMode.StopAnimationRecording();
            }
        }

        private void StartCandidateRecording()
        {
            AnimationMode.StartCandidateRecording(GetCandidateDriver());
        }

        private void StopCandidateRecording()
        {
            AnimationMode.StopCandidateRecording();
        }

        public override void ResampleAnimation()
        {
            if (state.disabled)
                return;

            if (previewing == false)
                return;
            if (canPreview == false)
                return;

            bool changed = false;

            AnimationMode.BeginSampling();

            AnimationWindowSelectionItem[] selectedItems = state.selection.ToArray();
            for (int i = 0; i < selectedItems.Length; ++i)
            {
                AnimationWindowSelectionItem selectedItem = selectedItems[i];
                if (selectedItem.animationClip != null)
                {
                    Undo.FlushUndoRecordObjects();

                    AnimationMode.SampleAnimationClip(selectedItem.rootGameObject, selectedItem.animationClip, time.time - selectedItem.timeOffset);
                    if (m_CandidateClip != null)
                        AnimationMode.SampleCandidateClip(selectedItem.rootGameObject, m_CandidateClip, 0f);

                    changed = true;
                }
            }

            AnimationMode.EndSampling();

            if (changed)
            {
                SceneView.RepaintAll();
                InspectorWindow.RepaintAllInspectors();

                // Particle editor needs to be manually repainted to refresh the animated properties
                var particleSystemWindow = ParticleSystemWindow.GetInstance();
                if (particleSystemWindow)
                    particleSystemWindow.Repaint();
            }
        }

        private AnimationModeDriver GetAnimationModeDriver()
        {
            if (m_Driver == null)
            {
                m_Driver = CreateInstance<AnimationModeDriver>();
                m_Driver.name = "AnimationWindowDriver";
                m_Driver.isKeyCallback += (Object target, string propertyPath) =>
                    {
                        if (AnimationMode.IsPropertyAnimated(target, propertyPath))
                        {
                            var modification = new PropertyModification();
                            modification.target = target;
                            modification.propertyPath = propertyPath;

                            return KeyExists(new PropertyModification[] {modification});
                        }

                        return false;
                    };
            }

            return m_Driver;
        }

        private AnimationModeDriver GetCandidateDriver()
        {
            if (m_CandidateDriver == null)
            {
                m_CandidateDriver = CreateInstance<AnimationModeDriver>();
                m_CandidateDriver.name = "AnimationWindowCandidateDriver";
            }

            return m_CandidateDriver;
        }

        private UndoPropertyModification[] PostprocessAnimationRecordingModifications(UndoPropertyModification[] modifications)
        {
            //Fix for case 751009: The animationMode can be changed outside the AnimationWindow, and this callback needs to be unregistered.
            if (!AnimationMode.InAnimationMode(GetAnimationModeDriver()))
            {
                Undo.postprocessModifications -= PostprocessAnimationRecordingModifications;
                return modifications;
            }

            if (recording)
                return ProcessAutoKey(modifications);
            else if (previewing)
                return RegisterCandidates(modifications);

            return modifications;
        }

        private UndoPropertyModification[] ProcessAutoKey(UndoPropertyModification[] modifications)
        {
            BeginKeyModification();

            RecordingState recordingState = new RecordingState(state, RecordingStateMode.AutoKey);
            UndoPropertyModification[] discardedModifications = AnimationRecording.Process(recordingState, modifications);

            EndKeyModification();

            return discardedModifications;
        }

        private UndoPropertyModification[] RegisterCandidates(UndoPropertyModification[] modifications)
        {
            bool createNewClip = (m_CandidateClip == null);
            if (createNewClip)
            {
                m_CandidateClip = new AnimationClip();
                m_CandidateClip.legacy = state.activeAnimationClip.legacy;
                m_CandidateClip.name = "CandidateClip";

                StartCandidateRecording();
            }

            CandidateRecordingState recordingState = new CandidateRecordingState(state, m_CandidateClip);
            UndoPropertyModification[] discardedModifications = AnimationRecording.Process(recordingState, modifications);

            // No modifications were added to the candidate clip, discard.
            if (createNewClip && discardedModifications.Length == modifications.Length)
            {
                ClearCandidates();
            }

            // Make sure inspector is repainted after adding new candidates to get appropriate feedback.
            InspectorWindow.RepaintAllInspectors();

            return discardedModifications;
        }

        private void RemoveFromCandidates(PropertyModification[] modifications)
        {
            if (m_CandidateClip == null)
                return;

            EditorCurveBinding[] bindings = AnimationWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, m_CandidateClip);
            if (bindings.Length == 0)
                return;

            // Remove entry from candidate clip.
            Undo.RegisterCompleteObjectUndo(m_CandidateClip, "Edit Candidate Curve");

            for (int i = 0; i < bindings.Length; ++i)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.isPPtrCurve)
                    AnimationUtility.SetObjectReferenceCurve(m_CandidateClip, binding, null);
                else
                    AnimationUtility.SetEditorCurve(m_CandidateClip, binding, null);
            }

            // Clear out candidate clip if it's empty.
            if (AnimationUtility.GetCurveBindings(m_CandidateClip).Length == 0 && AnimationUtility.GetObjectReferenceCurveBindings(m_CandidateClip).Length == 0)
                ClearCandidates();
        }

        public override void ClearCandidates()
        {
            m_CandidateClip = null;
            StopCandidateRecording();
        }

        public override void ProcessCandidates()
        {
            if (m_CandidateClip == null)
                return;

            BeginKeyModification();

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(m_CandidateClip);
            EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(m_CandidateClip);

            List<AnimationWindowCurve> curves = new List<AnimationWindowCurve>();

            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                AnimationWindowCurve curve = state.allCurves[i];
                EditorCurveBinding remappedBinding = RotationCurveInterpolation.RemapAnimationBindingForRotationCurves(curve.binding, m_CandidateClip);
                if (Array.Exists(bindings, binding => remappedBinding.Equals(binding)) || Array.Exists(objectCurveBindings, binding => remappedBinding.Equals(binding)))
                    curves.Add(curve);
            }

            AnimationWindowUtility.AddKeyframes(state, curves.ToArray(), time);

            EndKeyModification();

            ClearCandidates();
        }

        private List<AnimationWindowKeyframe> GetKeys(PropertyModification[] modifications)
        {
            var keys = new List<AnimationWindowKeyframe>();

            EditorCurveBinding[] bindings = AnimationWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return keys;

            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                AnimationWindowCurve curve = state.allCurves[i];
                if (Array.Exists(bindings, binding => curve.binding.Equals(binding)))
                {
                    int keyIndex = curve.GetKeyframeIndex(state.time);
                    if (keyIndex >= 0)
                    {
                        keys.Add(curve.m_Keyframes[keyIndex]);
                    }
                }
            }

            return keys;
        }

        public bool IsAnimatable(PropertyModification[] modifications)
        {
            for (int i = 0; i < modifications.Length; ++i)
            {
                var modification = modifications[i];
                if (AnimationWindowUtility.PropertyIsAnimatable(modification.target, modification.propertyPath, state.activeRootGameObject))
                    return true;
            }

            return false;
        }

        public bool IsEditable(Object targetObject)
        {
            if (state.selection.disabled)
                return false;

            if (previewing == false)
                return false;

            var selectedItem = state.selectedItem;
            if (selectedItem != null)
            {
                GameObject gameObject = null;
                if (targetObject is Component)
                    gameObject = ((Component)targetObject).gameObject;
                else if (targetObject is GameObject)
                    gameObject = (GameObject)targetObject;

                if (gameObject != null)
                {
                    Component animationPlayer = AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
                    if (selectedItem.animationPlayer == animationPlayer)
                    {
                        return selectedItem.animationIsEditable;
                    }
                }
            }

            return false;
        }

        public bool KeyExists(PropertyModification[] modifications)
        {
            return (GetKeys(modifications).Count > 0);
        }

        public bool CandidateExists(PropertyModification[] modifications)
        {
            if (!HasAnyCandidates())
                return false;

            for (int i = 0; i < modifications.Length; ++i)
            {
                var modification = modifications[i];
                if (AnimationMode.IsPropertyCandidate(modification.target, modification.propertyPath))
                    return true;
            }

            return false;
        }

        public bool CurveExists(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = AnimationWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return false;

            EditorCurveBinding[] clipBindings = AnimationUtility.GetCurveBindings(state.activeAnimationClip);
            if (clipBindings.Length == 0)
                return false;

            if (Array.Exists(bindings, binding => Array.Exists(clipBindings, clipBinding => clipBinding.Equals(binding))))
                return true;

            EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(state.activeAnimationClip);
            if (objectCurveBindings.Length == 0)
                return false;

            return Array.Exists(objectCurveBindings, binding => Array.Exists(clipBindings, clipBinding => clipBinding.Equals(binding)));
        }

        public bool HasAnyCandidates()
        {
            return (m_CandidateClip != null);
        }

        public bool HasAnyCurves()
        {
            return (state.allCurves.Count > 0);
        }

        public void AddKey(SerializedProperty property)
        {
            AddKey(AnimationWindowUtility.SerializedPropertyToPropertyModifications(property));
        }

        public void AddKey(PropertyModification[] modifications)
        {
            var undoModifications = new UndoPropertyModification[modifications.Length];
            for (int i = 0; i < modifications.Length; ++i)
            {
                var modification = modifications[i];
                undoModifications[i].previousValue = modification;
                undoModifications[i].currentValue = modification;
            }

            BeginKeyModification();

            var recordingState = new RecordingState(state, RecordingStateMode.ManualKey);
            AnimationRecording.Process(recordingState, undoModifications);

            EndKeyModification();

            RemoveFromCandidates(modifications);

            ResampleAnimation();
            state.Repaint();
        }

        public void RemoveKey(SerializedProperty property)
        {
            RemoveKey(AnimationWindowUtility.SerializedPropertyToPropertyModifications(property));
        }

        public void RemoveKey(PropertyModification[] modifications)
        {
            BeginKeyModification();

            List<AnimationWindowKeyframe> keys = GetKeys(modifications);
            state.DeleteKeys(keys);

            RemoveFromCandidates(modifications);

            EndKeyModification();

            ResampleAnimation();
            state.Repaint();
        }

        public void RemoveCurve(SerializedProperty property)
        {
            RemoveCurve(AnimationWindowUtility.SerializedPropertyToPropertyModifications(property));
        }

        public void RemoveCurve(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = AnimationWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return;

            BeginKeyModification();

            Undo.RegisterCompleteObjectUndo(state.activeAnimationClip, "Remove Curve");

            for (int i = 0; i < bindings.Length; ++i)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.isPPtrCurve)
                    AnimationUtility.SetObjectReferenceCurve(state.activeAnimationClip, binding, null);
                else
                    AnimationUtility.SetEditorCurve(state.activeAnimationClip, binding, null);
            }

            EndKeyModification();

            RemoveFromCandidates(modifications);

            ResampleAnimation();
            state.Repaint();
        }

        public void AddCandidateKeys()
        {
            ProcessCandidates();

            ResampleAnimation();
            state.Repaint();
        }

        public void AddAnimatedKeys()
        {
            BeginKeyModification();

            AnimationWindowUtility.AddKeyframes(state, state.allCurves.ToArray(), time);
            ClearCandidates();

            EndKeyModification();

            ResampleAnimation();
            state.Repaint();
        }

        private void BeginKeyModification()
        {
            if (animEditor != null)
                animEditor.BeginKeyModification();
        }

        private void EndKeyModification()
        {
            if (animEditor != null)
                animEditor.EndKeyModification();
        }
    }
}
