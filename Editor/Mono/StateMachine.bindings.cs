// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Animations
{
    public enum AnimatorConditionMode
    {
        If = 1,
        IfNot = 2,
        Greater = 3,
        Less = 4,
        //ExitTime = 5,
        Equals = 6,
        NotEqual = 7,
    }

    public enum TransitionInterruptionSource
    {
        None,
        Source,
        Destination,
        SourceThenDestination,
        DestinationThenSource
    }

    [NativeHeader("Editor/Src/Animation/Transition.h")]
    public struct AnimatorCondition
    {
        public AnimatorConditionMode      mode                                { get {return m_ConditionMode; }     set {m_ConditionMode = value; }  }
        public string                     parameter                           { get {return m_ConditionEvent; }    set {m_ConditionEvent = value; } }
        public float                      threshold                           { get {return m_EventTreshold; }     set {m_EventTreshold = value; }  }

        AnimatorConditionMode   m_ConditionMode;            //eConditionMode
        string                  m_ConditionEvent;
        float                   m_EventTreshold;// m_ParameterThreshold
    }

    [NativeHeader("Editor/Src/Animation/Transition.h")]
    [NativeHeader("Runtime/Animation/MecanimUtility.h")]
    public partial class AnimatorTransitionBase : Object
    {
        protected AnimatorTransitionBase() {}

        public string GetDisplayName(Object source)
        {
            return (source is AnimatorState) ? GetDisplayNameStateSource(source as AnimatorState) : GetDisplayNameStateMachineSource(source as AnimatorStateMachine);
        }

        [NativeMethod("GetDisplayName")]
        extern internal string GetDisplayNameStateSource(AnimatorState source);

        [NativeMethod("GetDisplayName")]
        extern internal string GetDisplayNameStateMachineSource(AnimatorStateMachine source);

        [FreeFunction]
        extern static internal string BuildTransitionName(string source, string destination);

        extern public bool solo { get; set; }
        extern public bool mute { get; set; }
        extern public bool isExit { get; set; }

        extern public AnimatorStateMachine destinationStateMachine
        {
            [NativeMethod("GetDstStateMachine")]
            get;
            [NativeMethod("SetDstStateMachine")]
            set;
        }
        extern public AnimatorState destinationState
        {
            [NativeMethod("GetDstState")]
            get;
            [NativeMethod("SetDstState")]
            set;
        }

        extern public AnimatorCondition[] conditions
        {
            get;
            set;
        }
    }

    [NativeHeader("Editor/Src/Animation/Transition.h")]
    [NativeHeader("Editor/Src/Animation/StateMachine.bindings.h")]
    public class AnimatorTransition : AnimatorTransitionBase
    {
        public AnimatorTransition()
        {
            Internal_CreateAnimatorTransition(this);
        }

        [FreeFunction("StateMachineBindings::Internal_CreateAnimatorTransition")]
        extern private static void Internal_CreateAnimatorTransition([Writable] AnimatorTransition mono);
    }

    [NativeHeader("Editor/Src/Animation/Transition.h")]
    [NativeHeader("Editor/Src/Animation/StateMachine.bindings.h")]
    public class AnimatorStateTransition : AnimatorTransitionBase
    {
        public AnimatorStateTransition()
        {
            Internal_CreateAnimatorStateTransition(this);
        }

        [FreeFunction("StateMachineBindings::Internal_CreateAnimatorStateTransition")]
        extern private static void Internal_CreateAnimatorStateTransition([Writable] AnimatorStateTransition self);

        extern public float                           duration
        {
            [NativeMethod("GetTransitionDuration")]
            get;
            [NativeMethod("SetTransitionDuration")]
            set;
        }
        extern public float                           offset
        {
            [NativeMethod("GetTransitionOffset")]
            get;
            [NativeMethod("SetTransitionOffset")]
            set;
        }
        extern public TransitionInterruptionSource    interruptionSource
        {
            [NativeMethod("GetTransitionInterruptionSource")]
            get;
            [NativeMethod("SetTransitionInterruptionSource")]
            set;
        }
        extern public bool                            orderedInterruption { get; set; }
        extern public float                           exitTime { get; set; }
        extern public bool                            hasExitTime { get; set; }
        extern public bool                            hasFixedDuration { get; set; }
        extern public bool                            canTransitionToSelf { get; set; }
    }

    [NativeHeader("Editor/Src/Animation/StateMachine.h")]
    [NativeHeader("Editor/Src/Animation/StateMachine.bindings.h")]
    [NativeHeader("Editor/Src/Animation/StateMachineBehaviourScripting.h")]
    public partial class AnimatorState : Object
    {
        public AnimatorState()
        {
            Internal_CreateAnimatorState(this);
        }

        [FreeFunction("StateMachineBindings::Internal_CreateAnimatorState")]
        extern private static void Internal_CreateAnimatorState([Writable] AnimatorState self);

        extern public int             nameHash
        {
            get;
        }
        extern public Motion          motion { get; set; }
        extern public float           speed { get; set; }
        extern public float           cycleOffset { get; set; }
        extern public bool            mirror { get; set; }
        extern public bool            iKOnFeet { get; set; }
        extern public bool            writeDefaultValues { get; set; }
        extern public string          tag { get; set; }
        extern public string          speedParameter { get; set; }
        extern public string          cycleOffsetParameter { get; set; }
        extern public string          mirrorParameter { get; set; }
        extern public string          timeParameter { get; set; }
        extern public bool            speedParameterActive
        {
            [NativeMethod("IsSpeedParameterActive")]
            get;
            set;
        }
        extern public bool            cycleOffsetParameterActive
        {
            [NativeMethod("IsCycleOffsetParameterActive")]
            get;
            set;
        }
        extern public bool            mirrorParameterActive
        {
            [NativeMethod("IsMirrorParameterActive")]
            get;
            set;
        }

        extern public bool            timeParameterActive
        {
            [NativeMethod("IsTimeParameterActive")]
            get;
            set;
        }

        extern internal void AddBehaviour(int instanceID);
        extern internal void RemoveBehaviour(int index);

        extern public AnimatorStateTransition[]   transitions { get; set; }

        [FreeFunction(Name = "ScriptingAddStateMachineBehaviourWithType", HasExplicitThis = true)]
        extern private ScriptableObject ScriptingAddStateMachineBehaviourWithType(Type stateMachineBehaviourType);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public StateMachineBehaviour AddStateMachineBehaviour(Type stateMachineBehaviourType)
        {
            return (StateMachineBehaviour)ScriptingAddStateMachineBehaviourWithType(stateMachineBehaviourType);
        }

        public T AddStateMachineBehaviour<T>() where T : StateMachineBehaviour
        {
            return AddStateMachineBehaviour(typeof(T)) as T;
        }
    }

    [NativeHeader("Editor/Src/Animation/StateMachine.h")]
    [NativeHeader("Editor/Src/Animation/StateMachine.bindings.h")]
    [RequiredByNativeCode]
    public struct ChildAnimatorState
    {
        AnimatorState m_State;
        Vector3       m_Position;

        public AnimatorState state { get { return m_State; }  set { m_State = value; } }
        public Vector3 position { get {return m_Position; } set {  m_Position = value; } }
    }


    [NativeHeader("Editor/Src/Animation/StateMachine.h")]
    [NativeHeader("Editor/Src/Animation/StateMachine.bindings.h")]
    [RequiredByNativeCode]
    public struct  ChildAnimatorStateMachine
    {
        AnimatorStateMachine  m_StateMachine;
        Vector3               m_Position;

        public AnimatorStateMachine   stateMachine    { get { return m_StateMachine; }  set { m_StateMachine = value; } }
        public Vector3                position        { get {return m_Position; } set {  m_Position = value; } }
    }

    [NativeHeader("Editor/Src/Animation/StateMachine.h")]
    [NativeHeader("Editor/Src/Animation/StateMachine.bindings.h")]
    [NativeHeader("Editor/Src/Animation/StateMachineBehaviourScripting.h")]
    public partial class AnimatorStateMachine : Object
    {
        public AnimatorStateMachine()
        {
            Internal_CreateAnimatorStateMachine(this);
        }

        [FreeFunction("StateMachineBindings::Internal_CreateAnimatorStateMachine")]
        extern private static void Internal_CreateAnimatorStateMachine([Writable] AnimatorStateMachine self);

        extern public ChildAnimatorState[] states { get; set; }

        extern public ChildAnimatorStateMachine[] stateMachines { get; set; }

        extern public AnimatorState   defaultState
        {
            [NativeMethod("DefaultState")]
            get;
            set;
        }

        extern public Vector3         anyStatePosition { get; set; }
        extern public Vector3         entryPosition { get; set; }
        extern public Vector3         exitPosition { get; set; }
        extern public Vector3         parentStateMachinePosition { get; set; }
        extern public AnimatorStateTransition[]   anyStateTransitions { get; set; }
        extern public AnimatorTransition[]    entryTransitions { get; set; }

        extern public AnimatorTransition[] GetStateMachineTransitions(AnimatorStateMachine sourceStateMachine);

        extern public void SetStateMachineTransitions(AnimatorStateMachine sourceStateMachine, AnimatorTransition[] transitions);

        extern internal void AddBehaviour(int instanceID);
        extern internal void RemoveBehaviour(int index);

        [FreeFunction(Name = "ScriptingAddStateMachineBehaviourWithType", HasExplicitThis = true)]
        extern private ScriptableObject ScriptingAddStateMachineBehaviourWithType(Type stateMachineBehaviourType);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public StateMachineBehaviour AddStateMachineBehaviour(Type stateMachineBehaviourType)
        {
            return (StateMachineBehaviour)ScriptingAddStateMachineBehaviourWithType(stateMachineBehaviourType);
        }

        public T AddStateMachineBehaviour<T>() where T : StateMachineBehaviour
        {
            return AddStateMachineBehaviour(typeof(T)) as T;
        }

        extern public string MakeUniqueStateName(string name);
        extern public string MakeUniqueStateMachineName(string name);


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Internals

        extern internal void Clear();
        [NativeMethod("RemoveState")]
        extern internal void RemoveStateInternal(AnimatorState state);
        [NativeMethod("RemoveStateMachine")]
        extern internal void RemoveStateMachineInternal(AnimatorStateMachine stateMachine);

        extern internal void MoveState(AnimatorState state, AnimatorStateMachine target);
        extern internal void MoveStateMachine(AnimatorStateMachine stateMachine, AnimatorStateMachine target);

        extern internal bool HasState(AnimatorState state, bool recursive);
        extern internal bool HasStateMachine(AnimatorStateMachine state, bool recursive);

        extern internal int transitionCount
        {
            get;
        }
    }
}
