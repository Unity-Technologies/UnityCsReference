// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace UnityEditor
{
    internal class TransitionPreview
    {
        private AvatarPreview m_AvatarPreview;
        private TimelineControl m_Timeline;

        private AnimatorController m_Controller;
        private AnimatorStateMachine m_StateMachine;
        private List<Vector2> m_ParameterMinMax = new List<Vector2>();
        private List<ParameterInfo> m_ParameterInfoList;

        private AnimatorStateTransition m_RefTransition;
        private TransitionInfo m_RefTransitionInfo = new TransitionInfo();
        private AnimatorStateTransition m_Transition;
        private AnimatorState m_SrcState;
        private AnimatorState m_DstState;
        private AnimatorState m_RefSrcState;
        private AnimatorState m_RefDstState;


        private Motion m_SrcMotion;
        private Motion m_DstMotion;

        private bool m_ShowBlendValue = false;

        private bool m_MustResample = true;
        private bool m_MustSampleMotions = false;
        public bool mustResample { set { m_MustResample = value; } get { return m_MustResample; } }
        private float m_LastEvalTime = -1.0f;
        private bool m_IsResampling = false;

        private AvatarMask m_LayerMask;
        private int m_LayerIndex;

        private bool m_ValidTransition = true;

        class ParameterInfo
        {
            public string m_Name;
            public float m_Value;
        }

        int FindParameterInfo(List<ParameterInfo> parameterInfoList, string name)
        {
            int ret = -1;

            for (int i = 0; i < parameterInfoList.Count && ret == -1; i++)
            {
                if (parameterInfoList[i].m_Name == name)
                {
                    ret = i;
                }
            }

            return ret;
        }

        void SetMotion(AnimatorState state, int layerIndex, Motion motion)
        {
            AnimatorControllerLayer[] layers = m_Controller.layers;
            state.motion = motion;
            m_Controller.layers = layers;
        }

        class TransitionInfo
        {
            AnimatorState m_SrcState;
            AnimatorState m_DstState;
            float m_TransitionDuration;
            float m_TransitionOffset;
            float m_ExitTime;

            public bool IsEqual(TransitionInfo info)
            {
                return m_SrcState == info.m_SrcState &&
                    m_DstState == info.m_DstState &&
                    Mathf.Approximately(m_TransitionDuration, info.m_TransitionDuration) &&
                    Mathf.Approximately(m_TransitionOffset, info.m_TransitionOffset) &&
                    Mathf.Approximately(m_ExitTime, info.m_ExitTime);
            }

            public TransitionInfo()
            {
                Init();
            }

            void Init()
            {
                m_SrcState = null;
                m_DstState = null;
                m_TransitionDuration = 0.0f;
                m_TransitionOffset = 0.0f;
                m_ExitTime = 0.5f;
            }

            public void Set(AnimatorStateTransition transition, AnimatorState srcState, AnimatorState dstState)
            {
                if (transition != null)
                {
                    m_SrcState = srcState;
                    m_DstState = dstState;
                    m_TransitionDuration = transition.duration;
                    m_TransitionOffset = transition.offset;
                    m_ExitTime = 0.5f;
                }
                else
                {
                    Init();
                }
            }
        };


        private void CopyStateForPreview(AnimatorState src, ref AnimatorState dst)
        {
            dst.iKOnFeet = src.iKOnFeet;
            dst.speed = src.speed;
            dst.mirror = src.mirror;

            dst.motion = src.motion;
        }

        private void CopyTransitionForPreview(AnimatorStateTransition src, ref AnimatorStateTransition dst)
        {
            if (src != null)
            {
                dst.duration = src.duration;
                dst.offset = src.offset;
                dst.exitTime = src.exitTime;
                dst.hasFixedDuration = src.hasFixedDuration;
            }
        }

        float m_LeftStateWeightA = 0;
        float m_LeftStateWeightB = 1;
        float m_LeftStateTimeA = 0;
        float m_LeftStateTimeB = 1;

        float m_RightStateWeightA = 0;
        float m_RightStateWeightB = 1;
        float m_RightStateTimeA = 0;
        float m_RightStateTimeB = 1;

        List<TimelineControl.PivotSample> m_SrcPivotList = new List<TimelineControl.PivotSample>();
        List<TimelineControl.PivotSample> m_DstPivotList = new List<TimelineControl.PivotSample>();

        private bool MustResample(TransitionInfo info)
        {
            bool isInPlayback = m_AvatarPreview != null  && m_AvatarPreview.Animator != null && m_AvatarPreview.Animator.recorderMode == AnimatorRecorderMode.Playback;
            return mustResample || !info.IsEqual(m_RefTransitionInfo) || !isInPlayback;
        }

        private void WriteParametersInController()
        {
            if (m_Controller)
            {
                int parameterCount = m_Controller.parameters.Length;

                for (int i = 0; i < parameterCount; i++)
                {
                    string parameterName = m_Controller.parameters[i].name;

                    int parameterInfoIndex = FindParameterInfo(m_ParameterInfoList, parameterName);

                    if (parameterInfoIndex != -1)
                    {
                        m_AvatarPreview.Animator.SetFloat(parameterName, m_ParameterInfoList[parameterInfoIndex].m_Value);
                    }
                }
            }
        }

        private void ResampleTransition(AnimatorStateTransition transition, AvatarMask layerMask, TransitionInfo info, Animator previewObject)
        {
            m_IsResampling = true;
            m_MustResample = false;
            m_ValidTransition = true;

            bool resetTimeSettings = m_RefTransition != transition;

            m_RefTransition = transition;
            m_RefTransitionInfo = info;

            m_LayerMask = layerMask;

            if (m_AvatarPreview != null)
            {
                m_AvatarPreview.OnDestroy();
                m_AvatarPreview = null;
            }

            ClearController();


            Motion sourceStateMotion = m_RefSrcState.motion;
            Init(previewObject, sourceStateMotion != null ? sourceStateMotion : m_RefDstState.motion);

            if (m_Controller == null)  //  did not create controller
            {
                m_IsResampling = false;
                return;
            }


            // since transform might change during sampling, and could alter the default valuesarray, and break recording
            m_AvatarPreview.Animator.allowConstantClipSamplingOptimization = false;

            /// sample all frames

            m_StateMachine.defaultState = m_DstState;
            m_Transition.mute = true;
            AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, m_Controller);
            m_AvatarPreview.Animator.Update(0.00001f);
            WriteParametersInController();
            m_AvatarPreview.Animator.SetLayerWeight(m_LayerIndex, 1);

            float nextStateDuration = m_AvatarPreview.Animator.GetCurrentAnimatorStateInfo(m_LayerIndex).length;

            m_StateMachine.defaultState = m_SrcState;
            m_Transition.mute = false;
            AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, m_Controller);
            m_AvatarPreview.Animator.Update(0.00001f);
            WriteParametersInController();
            m_AvatarPreview.Animator.SetLayerWeight(m_LayerIndex, 1);

            float currentStateDuration = m_AvatarPreview.Animator.GetCurrentAnimatorStateInfo(m_LayerIndex).length;

            if (m_LayerIndex > 0) m_AvatarPreview.Animator.stabilizeFeet = false;
            float maxDuration = (currentStateDuration * m_RefTransition.exitTime) + (m_Transition.duration * (m_RefTransition.hasFixedDuration ? 1.0f : currentStateDuration)) + nextStateDuration;

            // case 546812 disable previewer if the duration is too big, otherwise it hang Unity. 2000.0f is an arbitrary choice, it can be increase if needed.
            // in some case we got a m_Transition.duration == Infinity, bail out before unity hang.
            if (maxDuration > 2000.0f)
            {
                Debug.LogWarning("Transition duration is longer than 2000 second, Disabling previewer.");
                m_ValidTransition = false;
                m_IsResampling = false;
                return;
            }

            float effectiveCurrentStatetime = m_RefTransition.exitTime > 0 ? currentStateDuration * m_RefTransition.exitTime : currentStateDuration;
            // We want 30 samples/sec, maxed at 300 sample for very long state, and very short animation like 1 frame should at least get 5 sample
            float currentStateStepTime = effectiveCurrentStatetime > 0 ? Mathf.Min(Mathf.Max(effectiveCurrentStatetime / 300.0f, 1.0f / 30.0f), effectiveCurrentStatetime / 5.0f) :  1.0f / 30.0f;
            float nextStateStepTime = nextStateDuration > 0 ? Mathf.Min(Mathf.Max(nextStateDuration / 300.0f, 1.0f / 30.0f), nextStateDuration / 5.0f) : 1.0f / 30.0f;

            currentStateStepTime = Mathf.Max(currentStateStepTime, maxDuration / 600.0f);
            nextStateStepTime = Mathf.Max(nextStateStepTime, maxDuration / 600.0f);

            float stepTime = currentStateStepTime;

            float currentTime = 0.0f;

            bool hasStarted = false;
            bool hasTransitioned = false;
            bool hasFinished = false;

            //For transitions with exit time == 0, skip to end of clip so transition happens on first frame
            if (m_RefTransition.exitTime == 0)
            {
                m_AvatarPreview.Animator.CrossFade(0, 0f, 0, 0.9999f);
            }
            m_AvatarPreview.Animator.StartRecording(-1);

            m_LeftStateWeightA = 0;
            m_LeftStateTimeA = 0;

            m_AvatarPreview.Animator.Update(0.0f);


            while (!hasFinished && currentTime < maxDuration)
            {
                m_AvatarPreview.Animator.Update(stepTime);

                AnimatorStateInfo currentState = m_AvatarPreview.Animator.GetCurrentAnimatorStateInfo(m_LayerIndex);
                currentTime += stepTime;

                if (!hasStarted)
                {
                    m_LeftStateWeightA = m_LeftStateWeightB = currentState.normalizedTime;
                    m_LeftStateTimeA = m_LeftStateTimeB = currentTime;

                    hasStarted = true;
                }

                if (hasTransitioned && currentTime >= maxDuration)
                {
                    hasFinished = true;
                }

                if (!hasTransitioned && currentState.IsName(m_DstState.name))
                {
                    m_RightStateWeightA = currentState.normalizedTime;
                    m_RightStateTimeA = currentTime;

                    hasTransitioned = true;
                }

                if (!hasTransitioned)
                {
                    m_LeftStateWeightB = currentState.normalizedTime;
                    m_LeftStateTimeB = currentTime;
                }

                if (hasTransitioned)
                {
                    m_RightStateWeightB = currentState.normalizedTime;
                    m_RightStateTimeB = currentTime;
                }


                if (m_AvatarPreview.Animator.IsInTransition(m_LayerIndex))
                {
                    stepTime = nextStateStepTime;
                }
            }

            float endTime = currentTime;
            m_AvatarPreview.Animator.StopRecording();

            if (Mathf.Approximately(m_LeftStateWeightB, m_LeftStateWeightA) || Mathf.Approximately(m_RightStateWeightB,  m_RightStateWeightA))
            {
                Debug.LogWarning("Difference in effective length between states is too big. Transition preview will be disabled.");
                m_ValidTransition = false;
                m_IsResampling = false;
                return;
            }

            float leftDuration =  (m_LeftStateTimeB - m_LeftStateTimeA) / (m_LeftStateWeightB - m_LeftStateWeightA);
            float rightDuration = (m_RightStateTimeB - m_RightStateTimeA) / (m_RightStateWeightB - m_RightStateWeightA);

            if (m_MustSampleMotions)
            {
                // Do this as infrequently as possible
                m_MustSampleMotions = false;
                m_SrcPivotList.Clear();
                m_DstPivotList.Clear();


                stepTime = nextStateStepTime;
                m_StateMachine.defaultState  = m_DstState;
                m_Transition.mute = true;
                AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, m_Controller);
                m_AvatarPreview.Animator.Update(0.0f);
                m_AvatarPreview.Animator.SetLayerWeight(m_LayerIndex, 1);
                m_AvatarPreview.Animator.Update(0.0000001f);
                WriteParametersInController();
                currentTime = 0.0f;
                while (currentTime <= rightDuration)
                {
                    TimelineControl.PivotSample sample = new TimelineControl.PivotSample();
                    sample.m_Time = currentTime;
                    sample.m_Weight = m_AvatarPreview.Animator.pivotWeight;
                    m_DstPivotList.Add(sample);
                    m_AvatarPreview.Animator.Update(stepTime * 2);
                    currentTime += stepTime * 2;
                }

                stepTime = currentStateStepTime;
                m_StateMachine.defaultState = m_SrcState;
                m_Transition.mute = true;
                AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, m_Controller);
                m_AvatarPreview.Animator.Update(0.0000001f);
                WriteParametersInController();
                m_AvatarPreview.Animator.SetLayerWeight(m_LayerIndex, 1);
                currentTime = 0.0f;
                while (currentTime <= leftDuration)
                {
                    TimelineControl.PivotSample sample = new TimelineControl.PivotSample();
                    sample.m_Time = currentTime;
                    sample.m_Weight = m_AvatarPreview.Animator.pivotWeight;
                    m_SrcPivotList.Add(sample);
                    m_AvatarPreview.Animator.Update(stepTime * 2);
                    currentTime += stepTime * 2;
                }


                m_Transition.mute = false;
                AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, m_Controller);
                m_AvatarPreview.Animator.Update(0.0000001f);
                WriteParametersInController();
            }


            m_Timeline.StopTime = m_AvatarPreview.timeControl.stopTime = endTime;
            m_AvatarPreview.timeControl.currentTime = m_Timeline.Time;
            if (resetTimeSettings)
            {
                m_Timeline.Time = m_Timeline.StartTime = m_AvatarPreview.timeControl.currentTime = m_AvatarPreview.timeControl.startTime = 0;
                m_Timeline.ResetRange();
            }

            m_AvatarPreview.Animator.StartPlayback();

            m_AvatarPreview.Animator.playbackTime = 0f;
            m_AvatarPreview.Animator.Update(0f);
            m_AvatarPreview.ResetPreviewFocus();

            m_IsResampling = false;
        }

        public void SetTransition(AnimatorStateTransition transition, AnimatorState sourceState, AnimatorState destinationState, AnimatorControllerLayer srcLayer, Animator previewObject)
        {
            m_RefSrcState = sourceState;
            m_RefDstState = destinationState;
            TransitionInfo info = new TransitionInfo();
            info.Set(transition, sourceState, destinationState);

            if (MustResample(info))
            {
                ResampleTransition(transition, srcLayer.avatarMask, info, previewObject);
            }
        }

        private void OnPreviewAvatarChanged()
        {
            m_RefTransitionInfo = new TransitionInfo();
            ClearController();
            CreateController();
            CreateParameterInfoList();
        }

        void ClearController()
        {
            if (m_AvatarPreview != null && m_AvatarPreview.Animator != null)
                AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, null);

            Object.DestroyImmediate(m_Controller);
            Object.DestroyImmediate(m_SrcState);
            Object.DestroyImmediate(m_DstState);
            Object.DestroyImmediate(m_Transition);

            m_StateMachine = null;
            m_Controller = null;
            m_SrcState = null;
            m_DstState = null;
            m_Transition = null;
        }

        void CreateParameterInfoList()
        {
            m_ParameterInfoList = new List<ParameterInfo>();
            if (m_Controller && m_Controller.parameters != null)
            {
                int parameterCount = m_Controller.parameters.Length;
                for (int i = 0; i < parameterCount; i++)
                {
                    ParameterInfo parameterInfo = new ParameterInfo();
                    parameterInfo.m_Name = m_Controller.parameters[i].name;
                    m_ParameterInfoList.Add(parameterInfo);
                }
            }
        }

        void CreateController()
        {
            if (m_Controller == null && m_AvatarPreview != null && m_AvatarPreview.Animator != null && m_RefTransition != null)
            {
                // controller
                m_LayerIndex = 0;
                m_Controller = new AnimatorController();
                m_Controller.pushUndo = false;
                m_Controller.hideFlags = HideFlags.HideAndDontSave;
                m_Controller.AddLayer("preview");

                bool isDefaultMask = true;

                if (m_LayerMask != null)
                {
                    for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart && isDefaultMask; i++)
                        if (!m_LayerMask.GetHumanoidBodyPartActive(i)) isDefaultMask = false;

                    if (!isDefaultMask)
                    {
                        m_Controller.AddLayer("Additionnal");
                        m_LayerIndex++;
                        AnimatorControllerLayer[] layers = m_Controller.layers;
                        layers[m_LayerIndex].avatarMask = m_LayerMask;
                        m_Controller.layers = layers;
                    }
                }
                m_StateMachine = m_Controller.layers[m_LayerIndex].stateMachine;
                m_StateMachine.pushUndo = false;
                m_StateMachine.hideFlags = HideFlags.HideAndDontSave;

                m_SrcMotion = m_RefSrcState.motion;
                m_DstMotion = m_RefDstState.motion;

                /// Add parameters
                m_ParameterMinMax.Clear();

                if (m_SrcMotion && m_SrcMotion is BlendTree)
                {
                    BlendTree leftBlendTree = m_SrcMotion as BlendTree;

                    for (int i = 0; i < leftBlendTree.recursiveBlendParameterCount; i++)
                    {
                        string blendValueName = leftBlendTree.GetRecursiveBlendParameter(i);
                        if (m_Controller.IndexOfParameter(blendValueName) == -1)
                        {
                            m_Controller.AddParameter(blendValueName, AnimatorControllerParameterType.Float);
                            m_ParameterMinMax.Add(new Vector2(leftBlendTree.GetRecursiveBlendParameterMin(i), leftBlendTree.GetRecursiveBlendParameterMax(i)));
                        }
                    }
                }

                if (m_DstMotion && m_DstMotion is BlendTree)
                {
                    BlendTree rightBlendTree = m_DstMotion as BlendTree;

                    for (int i = 0; i < rightBlendTree.recursiveBlendParameterCount; i++)
                    {
                        string blendValueName = rightBlendTree.GetRecursiveBlendParameter(i);
                        int parameterIndex = m_Controller.IndexOfParameter(blendValueName);
                        if (parameterIndex == -1)
                        {
                            m_Controller.AddParameter(blendValueName, AnimatorControllerParameterType.Float);
                            m_ParameterMinMax.Add(new Vector2(rightBlendTree.GetRecursiveBlendParameterMin(i), rightBlendTree.GetRecursiveBlendParameterMax(i)));
                        }
                        else
                        {
                            m_ParameterMinMax[parameterIndex] =
                                new Vector2(Mathf.Min(rightBlendTree.GetRecursiveBlendParameterMin(i), m_ParameterMinMax[parameterIndex][0]),
                                    Mathf.Max(rightBlendTree.GetRecursiveBlendParameterMax(i), m_ParameterMinMax[parameterIndex][1]));
                        }
                    }
                }


                // states
                m_SrcState = m_StateMachine.AddState(m_RefSrcState.name);
                m_SrcState.pushUndo = false;
                m_SrcState.hideFlags = HideFlags.HideAndDontSave;
                m_DstState = m_StateMachine.AddState(m_RefDstState.name);
                m_DstState.pushUndo = false;
                m_DstState.hideFlags = HideFlags.HideAndDontSave;

                CopyStateForPreview(m_RefSrcState, ref m_SrcState);
                CopyStateForPreview(m_RefDstState, ref m_DstState);

                // transition
                m_Transition = m_SrcState.AddTransition(m_DstState, true);
                m_Transition.pushUndo = false;
                m_Transition.hideFlags = HideFlags.DontSave;
                CopyTransitionForPreview(m_RefTransition, ref m_Transition);

                DisableIKOnFeetIfNeeded();


                AnimatorController.SetAnimatorController(m_AvatarPreview.Animator, m_Controller);

                m_Controller.OnAnimatorControllerDirty += ControllerDirty;
            }
        }

        private void ControllerDirty()
        {
            if (!m_IsResampling)
                m_MustResample = true;
        }

        private void DisableIKOnFeetIfNeeded()
        {
            bool disable = false;
            if (m_SrcMotion == null || m_DstMotion == null)
            {
                disable = true;
            }

            if (m_LayerIndex > 0)
            {
                disable = !m_LayerMask.hasFeetIK;
            }

            if (disable)
            {
                m_SrcState.iKOnFeet = false;
                m_DstState.iKOnFeet = false;
            }
        }

        private void Init(Animator scenePreviewObject, Motion motion)
        {
            if (m_AvatarPreview == null)
            {
                m_AvatarPreview = new AvatarPreview(scenePreviewObject, motion);
                m_AvatarPreview.OnAvatarChangeFunc = OnPreviewAvatarChanged;
                m_AvatarPreview.ShowIKOnFeetButton = false;
                m_AvatarPreview.ResetPreviewFocus();
            }

            if (m_Timeline == null)
            {
                m_Timeline = new TimelineControl();
                m_MustSampleMotions = true;
            }

            CreateController();

            if (m_ParameterInfoList == null)
            {
                CreateParameterInfoList();
            }
        }

        public void DoTransitionPreview()
        {
            if (m_Controller == null)
                return;

            if (Event.current.type == EventType.Repaint)
                m_AvatarPreview.timeControl.Update();

            DoTimeline();

            // Draw the blend values

            AnimatorControllerParameter[] parameters = m_Controller.parameters;
            if (parameters.Length > 0)
            {
                m_ShowBlendValue = EditorGUILayout.Foldout(m_ShowBlendValue, "BlendTree Parameters", true);

                if (m_ShowBlendValue)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        AnimatorControllerParameter parameter = m_Controller.parameters[i];
                        float value = m_ParameterInfoList[i].m_Value;
                        float newValue = EditorGUILayout.Slider(parameter.name, value, m_ParameterMinMax[i][0], m_ParameterMinMax[i][1]);
                        if (newValue != value)
                        {
                            m_ParameterInfoList[i].m_Value = newValue;
                            mustResample = true;
                            m_MustSampleMotions = true;
                        }
                    }
                }
            }
        }

        private void DoTimeline()
        {
            if (!m_ValidTransition)
            {
                return;
            }
            // get local durations
            float srcStateDuration =  (m_LeftStateTimeB - m_LeftStateTimeA) / (m_LeftStateWeightB - m_LeftStateWeightA);
            float dstStateDuration = (m_RightStateTimeB - m_RightStateTimeA) / (m_RightStateWeightB - m_RightStateWeightA);
            float transitionDuration =  m_Transition.duration * (m_RefTransition.hasFixedDuration ? 1.0f : srcStateDuration);

            // Set the timeline values
            m_Timeline.SrcStartTime = 0f;
            m_Timeline.SrcStopTime = srcStateDuration;
            m_Timeline.SrcName = m_RefSrcState.name;
            m_Timeline.HasExitTime = m_RefTransition.hasExitTime;

            m_Timeline.srcLoop = m_SrcMotion ? m_SrcMotion.isLooping : false;
            m_Timeline.dstLoop = m_DstMotion ? m_DstMotion.isLooping : false;

            m_Timeline.TransitionStartTime = m_RefTransition.exitTime * srcStateDuration;
            m_Timeline.TransitionStopTime = m_Timeline.TransitionStartTime + transitionDuration;

            m_Timeline.Time = m_AvatarPreview.timeControl.currentTime;

            m_Timeline.DstStartTime = m_Timeline.TransitionStartTime - m_RefTransition.offset * dstStateDuration;
            m_Timeline.DstStopTime =  m_Timeline.DstStartTime + dstStateDuration;

            m_Timeline.SampleStopTime = m_AvatarPreview.timeControl.stopTime;

            if (m_Timeline.TransitionStopTime == Mathf.Infinity)
                m_Timeline.TransitionStopTime = Mathf.Min(m_Timeline.DstStopTime, m_Timeline.SrcStopTime);


            m_Timeline.DstName = m_RefDstState.name;

            m_Timeline.SrcPivotList = m_SrcPivotList;
            m_Timeline.DstPivotList = m_DstPivotList;

            // Do the timeline
            Rect previewRect = EditorGUILayout.GetControlRect(false, 150, EditorStyles.label);

            EditorGUI.BeginChangeCheck();

            bool changedData = m_Timeline.DoTimeline(previewRect);

            if (EditorGUI.EndChangeCheck())
            {
                if (changedData)
                {
                    Undo.RegisterCompleteObjectUndo(m_RefTransition, "Edit Transition");
                    m_RefTransition.exitTime =  m_Timeline.TransitionStartTime / m_Timeline.SrcDuration;
                    m_RefTransition.duration = m_Timeline.TransitionDuration / (m_RefTransition.hasFixedDuration ? 1.0f : m_Timeline.SrcDuration);
                    m_RefTransition.offset = (m_Timeline.TransitionStartTime - m_Timeline.DstStartTime) / m_Timeline.DstDuration;
                }

                m_AvatarPreview.timeControl.nextCurrentTime = Mathf.Clamp(m_Timeline.Time, 0, m_AvatarPreview.timeControl.stopTime);
            }
        }

        public void OnDisable()
        {
            ClearController();
        }

        public void OnDestroy()
        {
            ClearController();

            if (m_Timeline != null)
            {
                m_Timeline = null;
            }

            if (m_AvatarPreview != null)
            {
                m_AvatarPreview.OnDestroy();
                m_AvatarPreview = null;
            }
        }

        public bool HasPreviewGUI()
        {
            return true;
        }

        public void OnPreviewSettings()
        {
            if (m_AvatarPreview != null)
                m_AvatarPreview.DoPreviewSettings();
        }

        public void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (m_AvatarPreview != null && m_Controller != null)
            {
                if (m_LastEvalTime != m_AvatarPreview.timeControl.currentTime && Event.current.type == EventType.Repaint)
                {
                    m_AvatarPreview.Animator.playbackTime = m_AvatarPreview.timeControl.currentTime;
                    m_AvatarPreview.Animator.Update(0);
                    m_LastEvalTime = m_AvatarPreview.timeControl.currentTime;
                }

                m_AvatarPreview.DoAvatarPreview(r, background);
            }
        }
    }
}//namespace UnityEditor
