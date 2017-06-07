// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

// This file is only used by the ScriptUpdater

namespace UnityEditorInternal
{
    [System.Obsolete("Transition is obsolete. Use UnityEditor.Animations.AnimatorTransition instead (UnityUpgradable) -> UnityEditor.Animations.AnimatorTransition", true)]
    public partial class Transition : Object
    {
        /*
        -----------------------------------------------------------------------------------------------------------------
        The following members have been moved to base classes in the target class (which by itself is a breaking change
        but since those types were *internal* it does not worth the work to fix updater/validation/configure more updates)
        -----------------------------------------------------------------------------------------------------------------
        public string uniqueName { get { return string.Empty; } }
        public int uniqueNameHash { get { return -1; } }
        public int conditionCount { get { return -1; } }

        public float duration { get { return -1.0f; } set {} }
        public float offset { get { return -1.0f; } set {} }

        public bool atomic { get { return false; } set {} }
        public bool solo { get { return false; } set {} }
        public bool mute { get { return false; } set {} }
        public bool canTransitionToSelf { get { return false; } set {} }

        public State srcState  { get { return default(State); } }
        public State dstState  { get { return default(State); } }
        public AnimatorCondition GetCondition(int index) { return default(AnimatorCondition); }
        public AnimatorCondition AddCondition() { return default(AnimatorCondition); }

        public GUIContent GetTransitionContentForRect(Rect rect) { return default(GUIContent); }
        */
    }

    [System.Obsolete("StateMachine is obsolete. Use UnityEditor.Animations.AnimatorStateMachine instead (UnityUpgradable) -> UnityEditor.Animations.AnimatorStateMachine", true)]
    public partial class StateMachine : Object
    {
        /*
        -----------------------------------------------------------------------------------------------------------------
        The following members have been moved to base classes in the target class (which by itself is a breaking change
        but since those types were *internal* it does not worth the work to fix updater/validation/configure more updates)
        -----------------------------------------------------------------------------------------------------------------
        public int stateCount { get { return -1; } }
        public int stateMachineCount { get { return -1; } }
        public int motionSetCount { get { return -1; } }
        */

        public State defaultState { get { return default(State); }  set {} }
        public Vector3 anyStatePosition { get { return default(Vector3); } set {} }
        public Vector3 parentStateMachinePosition { get { return default(Vector3); } set {} }
        public State GetState(int index) { return default(State); }
        public State AddState(string stateName)  { return default(State); }
        public StateMachine GetStateMachine(int index)  { return default(StateMachine); }
        public StateMachine AddStateMachine(string stateMachineName)  { return default(StateMachine); }
        public Transition AddTransition(State src, State dst)  { return default(Transition); }
        public Transition AddAnyStateTransition(State dst)  { return default(Transition); }
        public Vector3 GetStateMachinePosition(int i)   { return default(Vector3); }
        public Transition[] GetTransitionsFromState(State srcState)   { return null; }
    }

    [System.Obsolete("State is obsolete. Use UnityEditor.Animations.AnimatorState instead (UnityUpgradable) -> UnityEditor.Animations.AnimatorState", true)]
    public partial class State : Object
    {
        /*
        -----------------------------------------------------------------------------------------------------------------
        The following members have been moved to base classes in the target class (which by itself is a breaking change
        but since those types were *internal* it does not worth the work to fix updater/validation/configure more updates)
        -----------------------------------------------------------------------------------------------------------------
        public StateMachine stateMachine { get { return default(StateMachine); } }
        public Vector3 position { get { return default(Vector3); } set {} }
        */

        public string uniqueName { get { return string.Empty; } }
        public int uniqueNameHash  { get { return -1; } }
        public float speed { get { return -1.0f; } set {} }
        public bool mirror { get { return false; } set {} }
        public bool iKOnFeet { get { return false; } set {} }
        public string tag { get { return string.Empty; } set {} }
        public Motion GetMotion() { return default(Motion); }
        public Motion GetMotion(AnimatorControllerLayer layer) { return default(Motion); }
        public BlendTree CreateBlendTree() { return default(BlendTree); }
        public BlendTree CreateBlendTree(AnimatorControllerLayer layer) { return default(BlendTree); }
    }

    // removed this until scripting team can find proper solution to having API moved from one namespace to another
    [System.Obsolete("AnimatorController is obsolete. Use UnityEditor.Animations.AnimatorController instead (UnityUpgradable) -> UnityEditor.Animations.AnimatorController", true)]
    public class AnimatorController : RuntimeAnimatorController
    {
    }

    [System.Obsolete("BlendTree is obsolete. Use UnityEditor.Animations.BlendTree instead (UnityUpgradable) -> UnityEditor.Animations.BlendTree", true)]
    public partial class BlendTree : Motion
    {
    }

    [System.Obsolete("AnimatorControllerLayer is obsolete. Use UnityEditor.Animations.AnimatorControllerLayer instead (UnityUpgradable) -> UnityEditor.Animations.AnimatorControllerLayer", true)]
    public partial class AnimatorControllerLayer
    {
    }

    [System.Obsolete("AnimatorControllerParameter is obsolete. Use UnityEngine.AnimatorControllerParameter instead (UnityUpgradable) -> [UnityEngine] UnityEngine.AnimatorControllerParameter", true)]
    public partial class AnimatorControllerParameter
    {
    }

    [System.Obsolete("AnimatorControllerParameterType is obsolete. Use UnityEngine.AnimatorControllerParameterType instead (UnityUpgradable) -> [UnityEngine] UnityEngine.AnimatorControllerParameterType", true)]
    public enum AnimatorControllerParameterType
    {
        // members need to be declared only to avoid resolution failures. They cannot (and are not) used at runtime
        Float = -1,
        Int = -1,
        Bool = -1,
        Trigger = -1
    }

    [System.Obsolete("AnimatorLayerBlendingMode is obsolete. Use UnityEditor.Animations.AnimatorLayerBlendingMode instead (UnityUpgradable) -> UnityEditor.Animations.AnimatorLayerBlendingMode", true)]
    public enum AnimatorLayerBlendingMode
    {
        Override = -1,
        Additive = -1
    }
}
