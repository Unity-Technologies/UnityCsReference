// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngineInternal;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor.Animations
{



public enum AnimatorLayerBlendingMode
{
    Override = 0,
    Additive = 1,
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct StateMotionPair
{
    public AnimatorState m_State;
    public Motion m_Motion;
    
    
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct StateBehavioursPair
{
    public AnimatorState m_State;
    public StateMachineBehaviour[] m_Behaviours;
}

public sealed partial class AnimatorControllerLayer
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
                        return pair.m_Behaviours;
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

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public sealed partial class StateMachineBehaviourContext
{
            public AnimatorController           animatorController;
            public Object                       animatorObject;
            public int                          layerIndex;
}

public sealed partial class AnimatorController : RuntimeAnimatorController
{
    public AnimatorController()
        {
            Internal_Create(this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create (AnimatorController mono) ;

    public extern  AnimatorControllerLayer[] layers
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  AnimatorControllerParameter[] parameters
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  AnimatorController GetEffectiveAnimatorController (Animator animator) ;

    internal static AnimatorControllerPlayable FindAnimatorControllerPlayable(Animator animator, AnimatorController controller)
        {
            PlayableHandle handle = new PlayableHandle();
            FindAnimatorControllerPlayableInternal(ref handle, animator, controller);
            if (!handle.IsValid())
                return AnimatorControllerPlayable.Null;
            return new AnimatorControllerPlayable(handle);
        }
    
    
    internal static void FindAnimatorControllerPlayableInternal (ref PlayableHandle ret, Animator animator, AnimatorController controller) {
        INTERNAL_CALL_FindAnimatorControllerPlayableInternal ( ref ret, animator, controller );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_FindAnimatorControllerPlayableInternal (ref PlayableHandle ret, Animator animator, AnimatorController controller);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetAnimatorController (Animator behavior, AnimatorController controller) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int IndexOfParameter (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RenameParameter (string prevName, string newName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string MakeUniqueParameterName (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public string MakeUniqueLayerName (string name) ;

    static public StateMachineBehaviourContext[] FindStateMachineBehaviourContext(StateMachineBehaviour behaviour)
        {
            return Internal_FindStateMachineBehaviourContext(behaviour);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int CreateStateMachineBehaviour (MonoScript script) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CanAddStateMachineBehaviours () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal MonoScript GetBehaviourMonoScript (AnimatorState state, int layerIndex, int behaviourIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private ScriptableObject Internal_AddStateMachineBehaviourWithType (Type stateMachineBehaviourType, AnimatorState state, int layerIndex) ;

    [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
    public StateMachineBehaviour AddEffectiveStateMachineBehaviour(Type stateMachineBehaviourType, AnimatorState state, int layerIndex)
        {
            return (StateMachineBehaviour)Internal_AddStateMachineBehaviourWithType(stateMachineBehaviourType, state, layerIndex);
        }
    
    
    public T AddEffectiveStateMachineBehaviour<T>(AnimatorState state, int layerIndex) where T : StateMachineBehaviour
        {
            return AddEffectiveStateMachineBehaviour(typeof(T), state, layerIndex) as T;
        }
    
    
    
    public T[] GetBehaviours<T>() where T : StateMachineBehaviour
        {
            return ConvertStateMachineBehaviour<T>(GetBehaviours(typeof(T)));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal ScriptableObject[] GetBehaviours (Type type) ;

    internal static T[] ConvertStateMachineBehaviour<T>(ScriptableObject[] rawObjects) where T : StateMachineBehaviour
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal Object[] CollectObjectsUsingParameter (string parameterName) ;

    internal extern  bool isAssetBundled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void AddStateEffectiveBehaviour (AnimatorState state, int layerIndex, int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RemoveStateEffectiveBehaviour (AnimatorState state, int layerIndex, int behaviourIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal StateMachineBehaviour[] Internal_GetEffectiveBehaviours (AnimatorState state, int layerIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void Internal_SetEffectiveBehaviours (AnimatorState state, int layerIndex, StateMachineBehaviour[] behaviours) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  StateMachineBehaviourContext[] Internal_FindStateMachineBehaviourContext (ScriptableObject scriptableObject) ;

}

}
