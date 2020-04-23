// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Object = UnityEngine.Object;

using UnityEngine.Playables;
using UnityEngine.Animations;

using UnityEngine.Experimental.Animations;

using Unity.Profiling;

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
                AnimationWindowUtility.SaveCurve(curve.clip, curve);
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
        }

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
                m_State.SaveCurve(curve.clip, curve);
            }

            public void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride)
            {
                AnimationMode.AddPropertyModification(binding, propertyModification, keepPrefabOverride);
            }
        }

        [Flags]
        enum ResampleFlags
        {
            None                = 0,

            RebuildGraph        = 1 << 0,
            RefreshViews        = 1 << 1,
            FlushUndos          = 1 << 2,

            Default             = RefreshViews | FlushUndos
        }

        private static bool HasFlag(ResampleFlags flags, ResampleFlags flag)
        {
            return (flags & flag) != 0;
        }

        [SerializeField] private AnimationKeyTime m_Time;

        [NonSerialized] private float m_PreviousUpdateTime;

        [NonSerialized] public AnimationWindowState state;
        public AnimEditor animEditor { get { return state.animEditor; } }

        [SerializeField] private AnimationClip m_CandidateClip;
        [SerializeField] private AnimationClip m_DefaultPose;

        [SerializeField] private AnimationModeDriver m_Driver;
        [SerializeField] private AnimationModeDriver m_CandidateDriver;

        private PlayableGraph m_Graph;
        private Playable m_GraphRoot;
        private AnimationClipPlayable m_ClipPlayable;
        private AnimationClipPlayable m_CandidateClipPlayable;
        private AnimationClipPlayable m_DefaultPosePlayable;
        private bool m_UsesPostProcessComponents = false;

        private static ProfilerMarker s_ResampleAnimationMarker = new ProfilerMarker("AnimationWindowControl.ResampleAnimation");

        public override void OnEnable()
        {
            base.OnEnable();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void OnDisable()
        {
            StopPreview();
            StopPlayback();

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        public void OnDestroy()
        {
            if (m_Driver != null)
                DestroyImmediate(m_Driver);
        }

        public override void OnSelectionChanged()
        {
            // Set back time at beginning and stop recording.
            if (state != null)
                m_Time = AnimationKeyTime.Time(0f, state.frameRate);

            StopPreview();
            StopPlayback();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopPreview();
                StopPlayback();
            }
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

                var driver = GetAnimationModeDriverNoAlloc();

                return (driver != null && AnimationMode.InAnimationMode(driver)) || !AnimationMode.InAnimationMode();
            }
        }

        public override bool previewing
        {
            get
            {
                var driver = GetAnimationModeDriverNoAlloc();
                if (driver == null)
                    return false;

                return AnimationMode.InAnimationMode(driver);
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
            DestroyGraph();
            CreateCandidateClip();

            IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
            m_UsesPostProcessComponents = previewComponents != null;
            if (previewComponents != null)
            {
                foreach (var component in previewComponents)
                {
                    component.StartPreview();
                }
            }

            return true;
        }

        public override void StopPreview()
        {
            StopPlayback();
            StopRecording();
            ClearCandidates();
            DestroyGraph();
            DestroyCandidateClip();

            AnimationMode.StopAnimationMode(GetAnimationModeDriver());

            // reset responder only if we have set it
            if (AnimationPropertyContextualMenu.Instance.IsResponder(this))
            {
                AnimationPropertyContextualMenu.Instance.SetResponder(null);
            }

            if (m_UsesPostProcessComponents)
            {
                IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
                if (previewComponents != null)
                {
                    foreach (var component in previewComponents)
                    {
                        component.StopPreview();
                    }

                    if (!Application.isPlaying)
                    {
                        var animator = state.activeAnimationPlayer as Animator;
                        if (animator != null)
                        {
                            animator.UnbindAllHandles();
                        }
                    }
                }

                m_UsesPostProcessComponents = false;
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

        private void DestroyGraph()
        {
            if (!m_Graph.IsValid())
                return;

            m_Graph.Destroy();
            m_GraphRoot = Playable.Null;
        }

        private void RebuildGraph(Animator animator)
        {
            DestroyGraph();

            m_Graph = PlayableGraph.Create("PreviewGraph");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            m_ClipPlayable = AnimationClipPlayable.Create(m_Graph, state.activeAnimationClip);
            m_ClipPlayable.SetOverrideLoopTime(true);
            m_ClipPlayable.SetLoopTime(false);
            m_ClipPlayable.SetApplyFootIK(false);

            m_CandidateClipPlayable = AnimationClipPlayable.Create(m_Graph, m_CandidateClip);
            m_CandidateClipPlayable.SetApplyFootIK(false);

            IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
            bool requiresDefaultPose = previewComponents != null && previewComponents.Length > 0;
            int nInputs = requiresDefaultPose ? 3 : 2;

            // Create a layer mixer if necessary, we'll connect playable nodes to it after having populated AnimationStream.
            AnimationLayerMixerPlayable mixer = AnimationLayerMixerPlayable.Create(m_Graph, nInputs);
            m_GraphRoot = (Playable)mixer;

            // Populate custom playable preview graph.
            if (previewComponents != null)
            {
                foreach (var component in previewComponents)
                {
                    m_GraphRoot = component.BuildPreviewGraph(m_Graph, m_GraphRoot);
                }
            }

            // Finish hooking up mixer.
            int inputIndex = 0;

            if (requiresDefaultPose)
            {
                AnimationMode.RevertPropertyModificationsForGameObject(state.activeRootGameObject);

                EditorCurveBinding[] streamBindings = AnimationUtility.GetAnimationStreamBindings(state.activeRootGameObject);

                m_DefaultPose = new AnimationClip() { name = "DefaultPose" };

                AnimationWindowUtility.CreateDefaultCurves(state, m_DefaultPose, streamBindings);

                m_DefaultPosePlayable = AnimationClipPlayable.Create(m_Graph, m_DefaultPose);
                m_DefaultPosePlayable.SetApplyFootIK(false);

                mixer.ConnectInput(inputIndex++, m_DefaultPosePlayable, 0, 1.0f);
            }

            mixer.ConnectInput(inputIndex++, m_ClipPlayable, 0, 1.0f);
            mixer.ConnectInput(inputIndex++, m_CandidateClipPlayable, 0, 1.0f);

            if (animator.applyRootMotion)
            {
                var motionX = AnimationMotionXToDeltaPlayable.Create(m_Graph);
                motionX.SetAbsoluteMotion(true);
                motionX.SetInputWeight(0, 1.0f);

                m_Graph.Connect(m_GraphRoot, 0, motionX, 0);

                m_GraphRoot = (Playable)motionX;
            }

            var output = AnimationPlayableOutput.Create(m_Graph, "ouput", animator);
            output.SetSourcePlayable(m_GraphRoot);
            output.SetWeight(0.0f);
        }

        private IAnimationWindowPreview[] FetchPostProcessComponents()
        {
            if (state.activeRootGameObject != null)
            {
                return state.activeRootGameObject.GetComponents<IAnimationWindowPreview>();
            }

            return null;
        }

        public override void ResampleAnimation()
        {
            ResampleAnimation(ResampleFlags.Default);
        }

        private void ResampleAnimation(ResampleFlags flags)
        {
            if (state.disabled)
                return;

            if (previewing == false)
                return;
            if (canPreview == false)
                return;

            s_ResampleAnimationMarker.Begin();
            if (state.activeAnimationClip != null)
            {
                var animationPlayer = state.activeAnimationPlayer;
                bool usePlayableGraph = animationPlayer is Animator;

                if (usePlayableGraph)
                {
                    var isValidGraph = m_Graph.IsValid();
                    if (isValidGraph)
                    {
                        var playableOutput = (AnimationPlayableOutput)m_Graph.GetOutput(0);
                        isValidGraph = playableOutput.GetTarget() == (Animator)animationPlayer;
                    }

                    if (HasFlag(flags, ResampleFlags.RebuildGraph) || !isValidGraph)
                    {
                        RebuildGraph((Animator)animationPlayer);
                    }
                }

                AnimationMode.BeginSampling();

                if (HasFlag(flags, ResampleFlags.FlushUndos))
                    Undo.FlushUndoRecordObjects();

                if (usePlayableGraph)
                {
                    if (m_UsesPostProcessComponents)
                    {
                        IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
                        if (previewComponents != null)
                        {
                            foreach (var component in previewComponents)
                            {
                                component.UpdatePreviewGraph(m_Graph);
                            }
                        }
                    }

                    if (!m_CandidateClip.empty)
                        AnimationMode.AddCandidates(state.activeRootGameObject, m_CandidateClip);

                    m_ClipPlayable.SetSampleRate(playing ? -1 : state.activeAnimationClip.frameRate);

                    AnimationMode.SamplePlayableGraph(m_Graph, 0, time.time);

                    // This will cover euler/quaternion matching in basic playable graphs only (animation clip + candidate clip).
                    AnimationUtility.SampleEulerHint(state.activeRootGameObject, state.activeAnimationClip, time.time, WrapMode.Clamp);
                    if (!m_CandidateClip.empty)
                        AnimationUtility.SampleEulerHint(state.activeRootGameObject, m_CandidateClip, time.time, WrapMode.Clamp);
                }
                else
                {
                    AnimationMode.SampleAnimationClip(state.activeRootGameObject, state.activeAnimationClip, time.time);
                    if (!m_CandidateClip.empty)
                        AnimationMode.SampleCandidateClip(state.activeRootGameObject, m_CandidateClip, 0f);
                }

                AnimationMode.EndSampling();

                if (HasFlag(flags, ResampleFlags.RefreshViews))
                {
                    SceneView.RepaintAll();
                    InspectorWindow.RepaintAllInspectors();

                    // Particle editor needs to be manually repainted to refresh the animated properties
                    var particleSystemWindow = ParticleSystemWindow.GetInstance();
                    if (particleSystemWindow)
                        particleSystemWindow.Repaint();
                }
            }
            s_ResampleAnimationMarker.End();
        }

        private AnimationModeDriver GetAnimationModeDriver()
        {
            if (m_Driver == null)
            {
                m_Driver = CreateInstance<AnimationModeDriver>();
                m_Driver.hideFlags = HideFlags.HideAndDontSave;
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

        private AnimationModeDriver GetAnimationModeDriverNoAlloc()
        {
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
                modifications = ProcessAutoKey(modifications);
            else if (previewing)
                modifications = RegisterCandidates(modifications);

            // Only resample when playable graph has been customized with post process nodes.
            if (m_UsesPostProcessComponents)
                ResampleAnimation(ResampleFlags.None);

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
            bool hasCandidates = AnimationMode.IsRecordingCandidates();

            if (!hasCandidates)
                StartCandidateRecording();

            CandidateRecordingState recordingState = new CandidateRecordingState(state, m_CandidateClip);
            UndoPropertyModification[] discardedModifications = AnimationRecording.Process(recordingState, modifications);

            // No modifications were added to the candidate clip, stop recording candidates.
            if (!hasCandidates && discardedModifications.Length == modifications.Length)
                StopCandidateRecording();

            // Make sure inspector is repainted after adding new candidates to get appropriate feedback.
            InspectorWindow.RepaintAllInspectors();

            return discardedModifications;
        }

        private void RemoveFromCandidates(PropertyModification[] modifications)
        {
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

        private void CreateCandidateClip()
        {
            m_CandidateClip = new AnimationClip();
            m_CandidateClip.legacy = state.activeAnimationClip.legacy;
            m_CandidateClip.name = "CandidateClip";
        }

        private void DestroyCandidateClip()
        {
            m_CandidateClip = null;
        }

        public override void ClearCandidates()
        {
            StopCandidateRecording();

            if (m_CandidateClip != null)
                m_CandidateClip.ClearCurves();
        }

        public override void ProcessCandidates()
        {
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

            AnimationWindowUtility.AddKeyframes(state, curves, time);

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

            GameObject gameObject = null;
            if (targetObject is Component)
                gameObject = ((Component)targetObject).gameObject;
            else if (targetObject is GameObject)
                gameObject = (GameObject)targetObject;

            if (gameObject != null)
            {
                Component animationPlayer = AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
                if (state.selection.animationPlayer == animationPlayer)
                {
                    return state.selection.animationIsEditable;
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
            return !m_CandidateClip.empty;
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

            AnimationWindowUtility.AddKeyframes(state, state.allCurves, time);
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
