// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [RequiredByNativeCode]
    public sealed partial class SharedBetweenAnimatorsAttribute : Attribute
    {
    }

    [RequiredByNativeCode]
    public abstract class StateMachineBehaviour : ScriptableObject
    {
        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        virtual public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        virtual public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        virtual public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        // OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
        virtual public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
        virtual public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }

        // OnStateMachineEnter is called when entering a statemachine via its Entry Node
        virtual public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
        {
        }

        // OnStateMachineExit is called when exiting a statemachine via its Exit Node
        virtual public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
        {
        }

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        virtual public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        virtual public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        virtual public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }

        // OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
        virtual public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }

        // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
        virtual public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }

        // OnStateMachineEnter is called when entering a statemachine via its Entry Node
        virtual public void OnStateMachineEnter(Animator animator, int stateMachinePathHash, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }

        // OnStateMachineExit is called when exiting a statemachine via its Exit Node
        virtual public void OnStateMachineExit(Animator animator, int stateMachinePathHash, UnityEngine.Animations.AnimatorControllerPlayable controller)
        {
        }
    }
}
