// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor;
using System.Runtime.InteropServices;

namespace UnityEditor.Animations
{
    public enum AnimatorLayerBlendingMode
    {
        Override = 0,
        Additive = 1,
    }

    [NativeHeader("Editor/Src/Animation/AnimatorControllerLayer.h")]
    [NativeHeader("Editor/Src/Animation/AnimatorControllerLayer.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoStateMotionPair")]
    internal struct StateMotionPair
    {
        public AnimatorState m_State;
        public Motion m_Motion;
    }

    [NativeHeader("Editor/Src/Animation/AnimatorControllerLayer.h")]
    [NativeHeader("Editor/Src/Animation/AnimatorControllerLayer.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoStateBehavioursPair")]
    internal struct StateBehavioursPair
    {
        public AnimatorState m_State;
        public ScriptableObject[] m_Behaviours;
    }

    [NativeHeader("Editor/Src/Animation/AnimatorControllerLayer.h")]
    [NativeHeader("Editor/Src/Animation/AnimatorControllerLayer.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    [NativeType(CodegenOptions.Custom, "MonoAnimatorControllerLayer")]
    public partial class AnimatorControllerLayer
    {
        public string                     name                        {   get { return m_Name; }                      set { m_Name = value; }  }
        public AnimatorStateMachine       stateMachine                {   get { return m_StateMachine; }              set { m_StateMachine = value; }  }
        public AvatarMask                 avatarMask                  {   get { return m_AvatarMask; }                set { m_AvatarMask = value; }    }
        public AnimatorLayerBlendingMode  blendingMode                {   get { return m_BlendingMode; }              set { m_BlendingMode = value; }  }
        public int                        syncedLayerIndex            {   get { return m_SyncedLayerIndex; }          set { m_SyncedLayerIndex = value; }  }
        public bool                       iKPass                      {   get { return m_IKPass; }                    set { m_IKPass = value; }    }
        public float                      defaultWeight               {   get { return m_DefaultWeight; }             set { m_DefaultWeight = value; } }
        public bool                       syncedLayerAffectsTiming    {   get { return m_SyncedLayerAffectsTiming; }  set { m_SyncedLayerAffectsTiming = value; }}

        public Motion                     GetOverrideMotion(AnimatorState state)
        {
            if (m_Motions != null)
                foreach (StateMotionPair pair in m_Motions)
                    if (pair.m_State == state)
                        return pair.m_Motion;

            return null;
        }

        public void                       SetOverrideMotion(AnimatorState state, Motion motion)
        {
            if (m_Motions == null) m_Motions =  new StateMotionPair[] {};
            for (int i = 0; i < m_Motions.Length; ++i)
            {
                if (m_Motions[i].m_State == state)
                {
                    m_Motions[i].m_Motion = motion;
                    return;
                }
            }

            StateMotionPair newPair;
            newPair.m_State = state;
            newPair.m_Motion = motion;
            ArrayUtility.Add(ref m_Motions, newPair);
        }

        public StateMachineBehaviour[]    GetOverrideBehaviours(AnimatorState state)
        {
            if (m_Behaviours != null)
            {
                foreach (StateBehavioursPair pair in m_Behaviours)
                {
                    if (pair.m_State == state)
                        return pair.m_Behaviours as StateMachineBehaviour[];
                }
            }
            return new StateMachineBehaviour[0];
        }

        public void                           SetOverrideBehaviours(AnimatorState state, StateMachineBehaviour[] behaviours)
        {
            if (m_Behaviours == null) m_Behaviours =  new StateBehavioursPair[] {};
            for (int i = 0; i < m_Behaviours.Length; ++i)
            {
                if (m_Behaviours[i].m_State == state)
                {
                    m_Behaviours[i].m_Behaviours = behaviours;
                    return;
                }
            }

            StateBehavioursPair newPair;
            newPair.m_State = state;
            newPair.m_Behaviours = behaviours;
            ArrayUtility.Add(ref m_Behaviours, newPair);
        }

        string                            m_Name;
        AnimatorStateMachine              m_StateMachine;
        AvatarMask                        m_AvatarMask;
        StateMotionPair[]                 m_Motions;
        StateBehavioursPair[]             m_Behaviours;
        AnimatorLayerBlendingMode         m_BlendingMode;
        int                               m_SyncedLayerIndex = -1;
        bool                              m_IKPass;
        float                             m_DefaultWeight;
        bool                              m_SyncedLayerAffectsTiming;
    }
}
