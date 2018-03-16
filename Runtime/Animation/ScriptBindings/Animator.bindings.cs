// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;
using UnityEngine.Internal;

namespace UnityEngine
{
    // Target
    public enum AvatarTarget
    {
        // The root, the position of the game object
        Root = 0,
        // The body, center of mass
        Body = 1,
        // The left foot
        LeftFoot = 2,
        // The right foot
        RightFoot = 3,
        // The left hand
        LeftHand = 4,
        // The right hand
        RightHand = 5,
    }

    // IK Goal
    public enum AvatarIKGoal
    {
        // The left foot
        LeftFoot = 0,
        // The right foot
        RightFoot = 1,
        // The left hand
        LeftHand = 2,
        // The right hand
        RightHand = 3
    }

    // IK Hint
    public enum AvatarIKHint
    {
        // The left knee
        LeftKnee = 0,
        // The right knee
        RightKnee = 1,
        // The left elbow
        LeftElbow = 2,
        // The right elbow
        RightElbow = 3
    }

    public enum AnimatorControllerParameterType
    {
        Float = 1,
        Int = 3,
        Bool = 4,
        Trigger = 9,
    }

    internal enum TransitionType
    {
        Normal = 1 << 0,
        Entry  = 1 << 1,
        Exit   = 1 << 2
    }

    internal enum StateInfoIndex
    {
        CurrentState,
        NextState,
        ExitState,
        InterruptedState
    }

    public enum AnimatorRecorderMode
    {
        Offline,
        Playback,
        Record
    }

    public enum DurationUnit
    {
        Fixed,
        Normalized
    }

    // Culling mode for the Animator
    public enum AnimatorCullingMode
    {
        // Always animate the entire character. Object is animated even when offscreen.
        AlwaysAnimate = 0,

        // Retarget, IK and write of Transforms are disabled when renderers are not visible.
        CullUpdateTransforms = 1,

        // Animation is completly disabled when renderers are not visible.
        CullCompletely = 2,

        [Obsolete("Enum member AnimatorCullingMode.BasedOnRenderers has been deprecated. Use AnimatorCullingMode.CullUpdateTransforms instead. (UnityUpgradable) -> CullUpdateTransforms", true)]
        BasedOnRenderers = 1,
    }

    public enum AnimatorUpdateMode
    {
        Normal = 0,
        AnimatePhysics = 1,
        UnscaledTime = 2
    }

    #pragma warning disable 649 //Field is never assigned to and will always have its default value
    // Information about what animation clips is played and its weight
    [NativeHeader("Runtime/Animation/AnimatorInfo.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    public struct AnimatorClipInfo
    {
        // Animation clip that is played
        public AnimationClip clip
        {
            get {return m_ClipInstanceID != 0 ? InstanceIDToAnimationClipPPtr(m_ClipInstanceID) : null; }
        }

        // The weight of the animation clip
        public float weight
        {
            get { return m_Weight; }
        }

        [FreeFunction("AnimationBindings::InstanceIDToAnimationClipPPtr")]
        extern private static AnimationClip InstanceIDToAnimationClipPPtr(int instanceID);

        private int m_ClipInstanceID;
        private float m_Weight;
    }

    // Information about the current or next state
    [NativeHeader("Runtime/Animation/AnimatorInfo.h")]
    [RequiredByNativeCode]
    public struct AnimatorStateInfo
    {
        // Does /name/ match the name of the active state in the statemachine.
        public bool IsName(string name)    { int hash = Animator.StringToHash(name); return hash == m_FullPath || hash == m_Name || hash == m_Path; }

        // For backwards compatibility this is actually the path...
        public int fullPathHash             { get { return m_FullPath; } }

        [Obsolete("AnimatorStateInfo.nameHash has been deprecated. Use AnimatorStateInfo.fullPathHash instead.")]
        public int nameHash                 { get { return m_Path; } }

        public int shortNameHash            { get { return m_Name; } }

        // Normalized time of the State
        public float normalizedTime         { get { return m_NormalizedTime; } }

        // Current duration of the state
        public float length                 { get { return m_Length; } }

        // State speed
        public float speed                  { get { return m_Speed; } }

        // State speed multiplier
        public float speedMultiplier        { get { return m_SpeedMultiplier; } }

        // The Tag of the State
        public int tagHash                  { get { return m_Tag; } }

        // Does /tag/ match the tag of the active state in the statemachine.
        public bool IsTag(string tag)      { return Animator.StringToHash(tag) == m_Tag; }

        // Is the state looping
        public bool loop                    { get { return m_Loop != 0; } }

        private int    m_Name;
        private int    m_Path;
        private int    m_FullPath;
        private float  m_NormalizedTime;
        private float  m_Length;
        private float  m_Speed;
        private float  m_SpeedMultiplier;
        private int    m_Tag;
        private int    m_Loop;
    }

    // Information about the current transition
    [NativeHeader("Runtime/Animation/AnimatorInfo.h")]
    [RequiredByNativeCode]
    public struct AnimatorTransitionInfo
    {
        // Does /name/ match the name of the active Transition.
        public bool IsName(string name) { return Animator.StringToHash(name) == m_Name  || Animator.StringToHash(name) == m_FullPath; }

        // Does /userName/ match the name of the active Transition.
        public bool IsUserName(string name) { return Animator.StringToHash(name) == m_UserName; }


        public int fullPathHash               { get { return m_FullPath; } }

        // The unique name of the Transition
        public int nameHash                   { get { return m_Name; } }

        // The user-specidied name of the Transition
        public int userNameHash               { get { return m_UserName; } }

        // The duration unit: can be either Fixed (in seconds) or Normalized (in percentage)
        public DurationUnit durationUnit      { get { return m_HasFixedDuration ? DurationUnit.Fixed : DurationUnit.Normalized; } }

        // Duration of the Transition
        public float duration                 { get { return m_Duration; } }

        // Normalized time of the Transition
        public float normalizedTime           { get { return m_NormalizedTime; } }

        public bool anyState                  { get { return m_AnyState; } }

        internal bool entry                   { get { return (m_TransitionType & (int)TransitionType.Entry) != 0; }}

        internal bool exit                    { get { return (m_TransitionType & (int)TransitionType.Exit) != 0; }}

        [NativeName("fullPathHash")]
        private int m_FullPath;
        [NativeName("userNameHash")]
        private int m_UserName;
        [NativeName("nameHash")]
        private int m_Name;
        [NativeName("hasFixedDuration")]
        private bool m_HasFixedDuration;
        [NativeName("duration")]
        private float m_Duration;
        [NativeName("normalizedTime")]
        private float m_NormalizedTime;
        [NativeName("anyState")]
        private bool m_AnyState;
        [NativeName("transitionType")]
        private int m_TransitionType;
    }
    #pragma warning restore 649


    // To specify position and rotation weight mask for Animator::MatchTarget
    [NativeHeader("Runtime/Animation/Animator.h")]
    public struct MatchTargetWeightMask
    {
        // MatchTargetWeightMask contructor
        public MatchTargetWeightMask(Vector3 positionXYZWeight, float rotationWeight)
        {
            m_PositionXYZWeight = positionXYZWeight;
            m_RotationWeight = rotationWeight;
        }

        // Position XYZ weight
        public Vector3 positionXYZWeight
        {
            get { return m_PositionXYZWeight; }
            set { m_PositionXYZWeight = value; }
        }

        // Rotation weight
        public float rotationWeight
        {
            get { return m_RotationWeight; }
            set { m_RotationWeight = value; }
        }

        private Vector3 m_PositionXYZWeight;
        private float m_RotationWeight;
    }

    // Interface to control the Mecanim animation system
    [NativeHeader("Runtime/Animation/Animator.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/Animator.bindings.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimatorControllerParameter.bindings.h")]
    [UsedByNativeCode]
    public partial class Animator : Behaviour
    {
        // Returns true if the current rig is optimizable
        extern public bool isOptimizable
        {
            [NativeMethod("IsOptimizable")]
            get;
        }

        // Returns true if the current rig is ''humanoid'', false if it is ''generic''
        extern public bool isHuman
        {
            [NativeMethod("IsHuman")]
            get;
        }

        // Returns true if the current generic rig has a root motion
        extern public bool hasRootMotion
        {
            [NativeMethod("HasRootMotion")]
            get;
        }

        // Returns true if root translation or rotation is driven by curves
        extern internal bool isRootPositionOrRotationControlledByCurves
        {
            [NativeMethod("IsRootTranslationOrRotationControllerByCurves")]
            get;
        }

        // Returns the scale of the current Avatar for a humanoid rig, (1 by default if the rig is generic)
        extern public float humanScale
        {
            get;
        }

        // Return true if the animator is currently initialized and ready to be use
        extern public bool isInitialized
        {
            [NativeMethod("IsInitialized")]
            get;
        }

        // Gets the value of a float parameter
        public float GetFloat(string name)             { return GetFloatString(name); }
        // Gets the value of a float parameter
        public float GetFloat(int id)                  { return GetFloatID(id); }
        // Sets the value of a float parameter
        public void SetFloat(string name, float value) { SetFloatString(name, value); }
        // Sets the value of a float parameter
        public void SetFloat(string name, float value, float dampTime, float deltaTime) { SetFloatStringDamp(name, value, dampTime, deltaTime); }

        // Sets the value of a float parameter
        public void SetFloat(int id, float value)       { SetFloatID(id, value); }
        // Sets the value of a float parameter
        public void SetFloat(int id, float value, float dampTime, float deltaTime) { SetFloatIDDamp(id, value, dampTime, deltaTime); }

        // Gets the value of a bool parameter
        public bool GetBool(string name)                { return GetBoolString(name); }
        // Gets the value of a bool parameter
        public bool GetBool(int id)                     { return GetBoolID(id); }
        // Sets the value of a bool parameter
        public void SetBool(string name, bool value)    { SetBoolString(name, value); }
        // Sets the value of a bool parameter
        public void SetBool(int id, bool value)         { SetBoolID(id, value); }

        // Gets the value of an integer parameter
        public int GetInteger(string name)              { return GetIntegerString(name); }
        // Gets the value of an integer parameter
        public int GetInteger(int id)                   { return GetIntegerID(id); }
        // Sets the value of an integer parameter
        public void SetInteger(string name, int value)  { SetIntegerString(name, value); }

        // Sets the value of an integer parameter
        public void SetInteger(int id, int value)       { SetIntegerID(id, value); }

        // Sets the trigger parameter on
        public void SetTrigger(string name)       { SetTriggerString(name); }

        // Sets the trigger parameter at on
        public void SetTrigger(int id)       { SetTriggerID(id); }

        // Resets the trigger parameter at off
        public void ResetTrigger(string name)       { ResetTriggerString(name); }

        // Resets the trigger parameter at off
        public void ResetTrigger(int id)       { ResetTriggerID(id); }

        // Returns true if a parameter is controlled by an additional curve on an animation
        public bool IsParameterControlledByCurve(string name)     { return IsParameterControlledByCurveString(name); }
        // Returns true if a parameter is controlled by an additional curve on an animation
        public bool IsParameterControlledByCurve(int id)          { return IsParameterControlledByCurveID(id); }

        // Gets the avatar delta position for the last evaluated frame
        extern public Vector3 deltaPosition { get; }
        // Gets the avatar delta rotation for the last evaluated frame
        extern public Quaternion  deltaRotation { get; }

        // Gets the avatar velocity for the last evaluated frame
        extern public Vector3 velocity { get; }
        // Gets the avatar angular velocity for the last evaluated frame
        extern public Vector3 angularVelocity { get; }

        //  The root position, the position of the game object
        extern public Vector3 rootPosition
        {
            [NativeMethod("GetAvatarPosition")]
            get;
            [NativeMethod("SetAvatarPosition")]
            set;
        }
        //  The root rotation, the rotation of the game object
        extern public Quaternion rootRotation
        {
            [NativeMethod("GetAvatarRotation")]
            get;
            [NativeMethod("SetAvatarRotation")]
            set;
        }
        // Root is controlled by animations
        extern public bool applyRootMotion
        {
            get;
            set;
        }

        // Linear velocity blending for root motion
        extern public bool linearVelocityBlending
        {
            get;
            set;
        }

        // When turned on, animations will be executed in the physics loop. This is only useful in conjunction with kinematic rigidbodies.
        [Obsolete("Animator.animatePhysics has been deprecated. Use Animator.updateMode instead.")]
        public bool animatePhysics
        {
            get { return updateMode == AnimatorUpdateMode.AnimatePhysics; }
            set { updateMode =  (value ? AnimatorUpdateMode.AnimatePhysics : AnimatorUpdateMode.Normal); }
        }

        extern public AnimatorUpdateMode updateMode
        {
            get;
            set;
        }

        // Tell if the corresponding Character has transform hierarchy.
        extern public bool hasTransformHierarchy
        {
            get;
        }

        extern internal bool allowConstantClipSamplingOptimization
        {
            get;
            set;
        }

        // The current gravity weight based on current animations that are played
        extern public float gravityWeight
        {
            get;
        }


        // The position of the body center of mass
        public Vector3 bodyPosition
        {
            get { CheckIfInIKPass(); return bodyPositionInternal; }
            set { CheckIfInIKPass(); bodyPositionInternal = value; }
        }

        extern internal Vector3 bodyPositionInternal
        {
            [NativeMethod("GetBodyPosition")]
            get;
            [NativeMethod("SetBodyPosition")]
            set;
        }

        // The rotation of the body center of mass
        public Quaternion bodyRotation
        {
            get { CheckIfInIKPass(); return bodyRotationInternal; }
            set { CheckIfInIKPass(); bodyRotationInternal = value; }
        }

        extern internal Quaternion bodyRotationInternal
        {
            [NativeMethod("GetBodyRotation")]
            get;
            [NativeMethod("SetBodyRotation")]
            set;
        }

        // Gets the position of an IK goal
        public Vector3 GetIKPosition(AvatarIKGoal goal) {  CheckIfInIKPass(); return GetGoalPosition(goal); }
        extern private Vector3 GetGoalPosition(AvatarIKGoal goal);

        // Sets the position of an IK goal
        public void SetIKPosition(AvatarIKGoal goal, Vector3 goalPosition) { CheckIfInIKPass(); SetGoalPosition(goal, goalPosition); }
        extern private void SetGoalPosition(AvatarIKGoal goal, Vector3 goalPosition);

        // Gets the rotation of an IK goal
        public Quaternion GetIKRotation(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalRotation(goal); }
        extern private Quaternion GetGoalRotation(AvatarIKGoal goal);

        // Sets the rotation of an IK goal
        public void SetIKRotation(AvatarIKGoal goal, Quaternion goalRotation) { CheckIfInIKPass();  SetGoalRotation(goal, goalRotation); }
        extern private void SetGoalRotation(AvatarIKGoal goal, Quaternion goalRotation);

        // Gets the translative weight of an IK goal (0 = at the original animation before IK, 1 = at the goal)
        public float GetIKPositionWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalWeightPosition(goal); }
        extern private float GetGoalWeightPosition(AvatarIKGoal goal);

        // Sets the translative weight of an IK goal (0 = at the original animation before IK, 1 = at the goal)
        public void SetIKPositionWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetGoalWeightPosition(goal, value); }
        extern private void SetGoalWeightPosition(AvatarIKGoal goal, float value);

        // Gets the rotational weight of an IK goal (0 = rotation before IK, 1 = rotation at the IK goal)
        public float GetIKRotationWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalWeightRotation(goal); }
        extern private float GetGoalWeightRotation(AvatarIKGoal goal);

        // Sets the rotational weight of an IK goal (0 = rotation before IK, 1 = rotation at the IK goal)
        public void SetIKRotationWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetGoalWeightRotation(goal, value); }
        extern private void SetGoalWeightRotation(AvatarIKGoal goal, float value);

        // Gets the position of an IK hint
        public Vector3 GetIKHintPosition(AvatarIKHint hint) {  CheckIfInIKPass(); return GetHintPosition(hint); }
        extern private Vector3 GetHintPosition(AvatarIKHint hint);

        // Sets the position of an IK hint
        public void SetIKHintPosition(AvatarIKHint hint, Vector3 hintPosition) { CheckIfInIKPass(); SetHintPosition(hint, hintPosition); }
        extern private void SetHintPosition(AvatarIKHint hint, Vector3 hintPosition);

        // Gets the translative weight of an IK hint (0 = at the original animation before IK, 1 = points toward the hint)
        public float GetIKHintPositionWeight(AvatarIKHint hint) { CheckIfInIKPass(); return GetHintWeightPosition(hint); }
        extern private float GetHintWeightPosition(AvatarIKHint hint);

        // Sets the translative weight of an IK hint (0 = at the original animation before IK, 1 = points toward the hint)
        public void SetIKHintPositionWeight(AvatarIKHint hint, float value) { CheckIfInIKPass(); SetHintWeightPosition(hint, value); }
        extern private void SetHintWeightPosition(AvatarIKHint hint, float value);

        // Sets the look at position
        public void SetLookAtPosition(Vector3 lookAtPosition) { CheckIfInIKPass(); SetLookAtPositionInternal(lookAtPosition); }

        [NativeMethod("SetLookAtPosition")]
        extern private void SetLookAtPositionInternal(Vector3 lookAtPosition);

        public void SetLookAtWeight(float weight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, 0.00f, 1.00f, 0.00f, 0.50f);
        }

        public void SetLookAtWeight(float weight, float bodyWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, 1.00f, 0.00f, 0.50f);
        }

        public void SetLookAtWeight(float weight, float bodyWeight, float headWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, 0.00f, 0.50f);
        }

        public void SetLookAtWeight(float weight, float bodyWeight, float headWeight, float eyesWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, 0.50f);
        }

        public void SetLookAtWeight(float weight, [DefaultValue("0.0f")] float bodyWeight, [DefaultValue("1.0f")] float headWeight, [DefaultValue("0.0f")] float eyesWeight, [DefaultValue("0.5f")] float clampWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }

        [NativeMethod("SetLookAtWeight")]
        extern private void SetLookAtWeightInternal(float weight, float bodyWeight, float headWeight, float eyesWeight, float clampWeight);

        // Set Local Rotation of humanoid bone during IK pass
        public void SetBoneLocalRotation(HumanBodyBones humanBoneId, Quaternion rotation) { CheckIfInIKPass(); SetBoneLocalRotationInternal(HumanTrait.GetBoneIndexFromMono((int)humanBoneId), rotation); }

        [NativeMethod("SetBoneLocalRotation")]
        extern private void SetBoneLocalRotationInternal(int humanBoneId, Quaternion rotation);

        extern private ScriptableObject GetBehaviour([NotNull] Type type);

        public T GetBehaviour<T>() where T : StateMachineBehaviour { return GetBehaviour(typeof(T)) as T; }

        private static T[] ConvertStateMachineBehaviour<T>(ScriptableObject[] rawObjects) where T : StateMachineBehaviour
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        public T[] GetBehaviours<T>() where T : StateMachineBehaviour
        {
            return ConvertStateMachineBehaviour<T>(InternalGetBehaviours(typeof(T)));
        }

        [FreeFunction(Name = "AnimatorBindings::InternalGetBehaviours", HasExplicitThis = true)]
        extern internal ScriptableObject[] InternalGetBehaviours([NotNull] Type type);

        public StateMachineBehaviour[] GetBehaviours(int fullPathHash, int layerIndex)
        {
            return InternalGetBehavioursByKey(fullPathHash, layerIndex, typeof(StateMachineBehaviour)) as StateMachineBehaviour[];
        }

        [FreeFunction(Name = "AnimatorBindings::InternalGetBehavioursByKey", HasExplicitThis = true)]
        extern internal ScriptableObject[] InternalGetBehavioursByKey(int fullPathHash, int layerIndex, [NotNull] Type type);

        // Automatic stabilization of feet during transition and blending
        extern public bool stabilizeFeet
        {
            get;
            set;
        }

        // The AnimatorController layer count
        extern public int layerCount
        {
            get;
        }

        // Gets name of the layer
        extern public string GetLayerName(int layerIndex);
        extern public int GetLayerIndex(string layerName);
        // Gets the layer's current weight
        extern public float GetLayerWeight(int layerIndex);
        // Sets the layer's current weight
        extern public void SetLayerWeight(int layerIndex, float weight);

        extern private void GetAnimatorStateInfo(int layerIndex, StateInfoIndex stateInfoIndex, out AnimatorStateInfo info);

        // Gets the current State information on a specified AnimatorController layer
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            AnimatorStateInfo info;
            GetAnimatorStateInfo(layerIndex, StateInfoIndex.CurrentState, out info);
            return info;
        }

        // Gets the next State information on a specified AnimatorController layer
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex)
        {
            AnimatorStateInfo info;
            GetAnimatorStateInfo(layerIndex, StateInfoIndex.NextState, out info);
            return info;
        }

        extern private void GetAnimatorTransitionInfo(int layerIndex, out AnimatorTransitionInfo info);

        // Gets the Transition information on a specified AnimatorController layer
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex)
        {
            AnimatorTransitionInfo  info;
            GetAnimatorTransitionInfo(layerIndex, out info);
            return info;
        }

        extern internal int GetAnimatorClipInfoCount(int layerIndex, bool current);

        // Gets the number of AnimatorClipInfo currently played by the current state
        public int GetCurrentAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCount(layerIndex, true);
        }

        // Gets the number of AnimatorClipInfo currently played by the next state
        public int GetNextAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCount(layerIndex, false);
        }

        [FreeFunction(Name = "AnimatorBindings::GetCurrentAnimatorClipInfo", HasExplicitThis = true)]
        extern public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex);

        [FreeFunction(Name = "AnimatorBindings::GetNextAnimatorClipInfo", HasExplicitThis = true)]
        extern public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex);

        // Gets the list of AnimatorClipInfo currently played by the current state
        public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, true, clips);
        }

        [FreeFunction(Name = "AnimatorBindings::GetAnimatorClipInfoInternal", HasExplicitThis = true)]
        extern private void GetAnimatorClipInfoInternal(int layerIndex, bool isCurrent, object clips);


        [NativeConditional("ENABLE_DOTNET")]
        [FreeFunction(Name = "AnimatorBindings::GetAnimatorClipInfoInternalWinRT", HasExplicitThis = true)]
        extern private AnimatorClipInfo[] GetAnimatorClipInfoInternalWinRT(int layerIndex, bool isCurrent);


        // Gets the list of AnimatorClipInfo currently played by the next state
        public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, false, clips);
        }

        // Is the specified AnimatorController layer in a transition
        extern public bool IsInTransition(int layerIndex);

        extern public AnimatorControllerParameter[] parameters
        {
            [FreeFunction(Name = "AnimatorBindings::GetParameters", HasExplicitThis = true)]
            get;
        }

        extern public int parameterCount
        {
            get;
        }

        public AnimatorControllerParameter GetParameter(int index)
        {
            AnimatorControllerParameter[] param = parameters;
            if (index < 0 && index >= parameters.Length)
                throw new IndexOutOfRangeException("Index must be between 0 and " + parameters.Length);
            return param[index];
        }

        // Blends pivot point between body center of mass and feet pivot. At 0%, the blending point is body center of mass. At 100%, the blending point is feet pivot
        extern public float feetPivotActive
        {
            get;
            set;
        }

        // Gets the pivot weight
        extern public float pivotWeight
        {
            get;
        }

        // Get the current position of the pivot
        extern public Vector3 pivotPosition
        {
            get;
        }

        extern private void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, int targetBodyPart, MatchTargetWeightMask weightMask, float startNormalizedTime, float targetNormalizedTime);

        // Automatically adjust the gameobject position and rotation so that the AvatarTarget reaches the matchPosition when the current state is at the specified progress
        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, 1);
        }

        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime, [DefaultValue("1")] float targetNormalizedTime)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, targetNormalizedTime);
        }

        // Interrupts the automatic target matching
        public void InterruptMatchTarget()
        {
            InterruptMatchTarget(true);
        }

        extern public void InterruptMatchTarget([DefaultValue("true")] bool completeMatch);


        // If automatic matching is active
        extern public bool isMatchingTarget
        {
            [NativeMethod("IsMatchingTarget")]
            get;
        }

        // The playback speed of the Animator. 1 is normal playback speed
        extern public float speed
        {
            get;
            set;
        }

        // Force the normalized time of a state to a user defined value
        [Obsolete("ForceStateNormalizedTime is deprecated. Please use Play or CrossFade instead.")]
        public void ForceStateNormalizedTime(float normalizedTime) { Play(0, 0, normalizedTime); }

        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            int layer = -1;
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer, float fixedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, [DefaultValue("-1")] int layer, [DefaultValue("0.0f")] float fixedTimeOffset, [DefaultValue("0.0f")] float normalizedTransitionTime)
        {
            CrossFadeInFixedTime(StringToHash(stateName), fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, int layer , float fixedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float fixedTimeOffset = 0.0f;
            int layer = -1;
            CrossFadeInFixedTime(stateHashName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);
        }

        [FreeFunction(Name = "AnimatorBindings::CrossFadeInFixedTime", HasExplicitThis = true)]
        extern public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("0.0f")]  float fixedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime);

        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer , float normalizedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFade(string stateName, float normalizedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFade(string stateName, float normalizedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            int layer = -1;
            CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFade(string stateName, float normalizedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("float.NegativeInfinity")]  float normalizedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime)
        {
            CrossFade(StringToHash(stateName), normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        [FreeFunction(Name = "AnimatorBindings::CrossFade", HasExplicitThis = true)]
        extern public void CrossFade(int stateHashName, float normalizedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("0.0f")]  float normalizedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime);

        public void CrossFade(int stateHashName, float normalizedTransitionDuration, int layer , float normalizedTimeOffset)
        {
            float normalizedTransitionTime = 0.0f;
            CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFade(int stateHashName, float normalizedTransitionDuration, int layer)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        public void CrossFade(int stateHashName, float normalizedTransitionDuration)
        {
            float normalizedTransitionTime = 0.0f;
            float normalizedTimeOffset = float.NegativeInfinity;
            int layer = -1;
            CrossFade(stateHashName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        public void PlayInFixedTime(string stateName, int layer)
        {
            float fixedTime = float.NegativeInfinity;
            PlayInFixedTime(stateName, layer, fixedTime);
        }

        public void PlayInFixedTime(string stateName)
        {
            float fixedTime = float.NegativeInfinity;
            int layer = -1;
            PlayInFixedTime(stateName, layer, fixedTime);
        }

        public void PlayInFixedTime(string stateName, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTime(StringToHash(stateName), layer, fixedTime);
        }

        [FreeFunction(Name = "AnimatorBindings::PlayInFixedTime", HasExplicitThis = true)]
        extern public void PlayInFixedTime(int stateNameHash, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime);

        public void PlayInFixedTime(int stateNameHash, int layer)
        {
            float fixedTime = float.NegativeInfinity;
            PlayInFixedTime(stateNameHash, layer, fixedTime);
        }

        public void PlayInFixedTime(int stateNameHash)
        {
            float fixedTime = float.NegativeInfinity;
            int layer = -1;
            PlayInFixedTime(stateNameHash, layer, fixedTime);
        }

        public void Play(string stateName, int layer)
        {
            float normalizedTime = float.NegativeInfinity;
            Play(stateName, layer, normalizedTime);
        }

        public void Play(string stateName)
        {
            float normalizedTime = float.NegativeInfinity;
            int layer = -1;
            Play(stateName, layer, normalizedTime);
        }

        public void Play(string stateName, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            Play(StringToHash(stateName), layer, normalizedTime);
        }

        [FreeFunction(Name = "AnimatorBindings::Play", HasExplicitThis = true)]
        extern public void Play(int stateNameHash, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime);

        public void Play(int stateNameHash, int layer)
        {
            float normalizedTime = float.NegativeInfinity;
            Play(stateNameHash, layer, normalizedTime);
        }

        public void Play(int stateNameHash)
        {
            float normalizedTime = float.NegativeInfinity;
            int layer = -1;
            Play(stateNameHash, layer, normalizedTime);
        }

        // Sets an AvatarTarget and a targetNormalizedTime for the current state
        extern public void SetTarget(AvatarTarget targetIndex, float targetNormalizedTime);

        //  Returns the position of the target specified by SetTarget(AvatarTarget targetIndex, float targetNormalizedTime))
        extern public Vector3 targetPosition
        {
            get;
        }
        //  Returns the rotation of the target specified by SetTarget(AvatarTarget targetIndex, float targetNormalizedTime))
        extern public Quaternion targetRotation
        {
            get;
        }


        [Obsolete("Use mask and layers to control subset of transfroms in a skeleton.", true)]
        public bool IsControlled(Transform transform) {return false; }

        // Returns ture if a transform a bone controlled by human
        extern internal bool IsBoneTransform(Transform transform);

        extern internal Transform avatarRoot
        {
            get;
        }

        public Transform GetBoneTransform(HumanBodyBones humanBoneId) {return GetBoneTransformInternal(HumanTrait.GetBoneIndexFromMono((int)humanBoneId)); }

        [NativeMethod("GetBoneTransform")]
        extern internal Transform GetBoneTransformInternal(int humanBoneId);

        // Controls culling of this Animator component.
        extern public AnimatorCullingMode cullingMode
        {
            get;
            set;
        }

        // Sets the animator in playback mode
        extern public void StartPlayback();

        // Stops animator playback mode
        extern public void StopPlayback();

        // Plays recorded data
        extern public float playbackTime
        {
            get;
            set;
        }

        // Sets the animator in record mode
        extern public void StartRecording(int frameCount);

        // Stops animator record mode
        extern public void StopRecording();

        // The time at which the recording data starts
        public float recorderStartTime
        {
            get { return GetRecorderStartTime(); }
            // Obsolete is not supported right now on property get/set
            // @jonh to avoid a breaking API change we simply left an empty set for now
            //[Obsolete("Animator.recorderStartTime cannot be set. You need to use Animator.StartRecording() instead.", true)]
            set {}
        }

        extern private float GetRecorderStartTime();

        // The time at which the recoding data stops
        public float recorderStopTime
        {
            get { return GetRecorderStopTime(); }
            // Obsolete is not supported right now on property get/set
            // @jonh to avoid a breaking API change we simply left an empty set for now
            //[Obsolete("Animator.recorderStopTime cannot be set. You need to use Animator.StopRecording() instead.", true)]
            set {}
        }

        extern private float GetRecorderStopTime();

        extern public AnimatorRecorderMode recorderMode
        {
            get;
        }

        // The runtime representation of AnimatorController that controls the Animator
        extern public RuntimeAnimatorController runtimeAnimatorController
        {
            get;
            set;
        }

        // Returns true if Animator has any playables assigned to it.
        extern public bool hasBoundPlayables
        {
            [NativeMethod("HasBoundPlayables")]
            get;
        }

        extern internal void ClearInternalControllerPlayable();

        extern public bool HasState(int layerIndex, int stateID);


        // Generates an parameter id from a string
        [NativeMethod(Name = "ScriptingStringToCRC32", IsThreadSafe = true)]
        extern public static int StringToHash(string name);

        // Gets/Sets the current Avatar
        extern public Avatar avatar
        {
            get;
            set;
        }

        extern internal string GetStats();

        public PlayableGraph playableGraph
        {
            get
            {
                PlayableGraph graph = new PlayableGraph();
                GetCurrentGraph(ref graph);
                return graph;
            }
        }

        [FreeFunction(Name = "AnimatorBindings::GetCurrentGraph", HasExplicitThis = true)]
        extern private void GetCurrentGraph(ref PlayableGraph graph);

        private void CheckIfInIKPass()
        {
            if (logWarnings && !IsInIKPass())
                Debug.LogWarning("Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK");
        }

        extern private bool IsInIKPass();

        [FreeFunction(Name = "AnimatorBindings::SetFloatString", HasExplicitThis = true)]
        extern private void SetFloatString(string name, float value);

        [FreeFunction(Name = "AnimatorBindings::SetFloatID", HasExplicitThis = true)]
        extern private void SetFloatID(int id, float value);

        [FreeFunction(Name = "AnimatorBindings::GetFloatString", HasExplicitThis = true)]
        extern private float GetFloatString(string name);
        [FreeFunction(Name = "AnimatorBindings::GetFloatID", HasExplicitThis = true)]
        extern private float GetFloatID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetBoolString", HasExplicitThis = true)]
        extern private void SetBoolString(string name, bool value);
        [FreeFunction(Name = "AnimatorBindings::SetBoolID", HasExplicitThis = true)]
        extern private void SetBoolID(int id, bool value);

        [FreeFunction(Name = "AnimatorBindings::GetBoolString", HasExplicitThis = true)]
        extern private bool GetBoolString(string name);
        [FreeFunction(Name = "AnimatorBindings::GetBoolID", HasExplicitThis = true)]
        extern private bool GetBoolID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetIntegerString", HasExplicitThis = true)]
        extern private void SetIntegerString(string name, int value);
        [FreeFunction(Name = "AnimatorBindings::SetIntegerID", HasExplicitThis = true)]
        extern private void SetIntegerID(int id, int value);

        [FreeFunction(Name = "AnimatorBindings::GetIntegerString", HasExplicitThis = true)]
        extern private int GetIntegerString(string name);
        [FreeFunction(Name = "AnimatorBindings::GetIntegerID", HasExplicitThis = true)]
        extern private int GetIntegerID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetTriggerString", HasExplicitThis = true)]
        extern private void SetTriggerString(string name);
        [FreeFunction(Name = "AnimatorBindings::SetTriggerID", HasExplicitThis = true)]
        extern private void SetTriggerID(int id);

        [FreeFunction(Name = "AnimatorBindings::ResetTriggerString", HasExplicitThis = true)]
        extern private void ResetTriggerString(string name);
        [FreeFunction(Name = "AnimatorBindings::ResetTriggerID", HasExplicitThis = true)]
        extern private void ResetTriggerID(int id);

        [FreeFunction(Name = "AnimatorBindings::IsParameterControlledByCurveString", HasExplicitThis = true)]
        extern private bool IsParameterControlledByCurveString(string name);
        [FreeFunction(Name = "AnimatorBindings::IsParameterControlledByCurveID", HasExplicitThis = true)]
        extern private bool IsParameterControlledByCurveID(int id);

        [FreeFunction(Name = "AnimatorBindings::SetFloatStringDamp", HasExplicitThis = true)]
        extern private void SetFloatStringDamp(string name, float value, float dampTime, float deltaTime);
        [FreeFunction(Name = "AnimatorBindings::SetFloatIDDamp", HasExplicitThis = true)]
        extern private void SetFloatIDDamp(int id, float value, float dampTime, float deltaTime);

        // True if additional layers affect the center of mass
        extern public bool layersAffectMassCenter
        {
            get;
            set;
        }

        // Get left foot bottom height.
        extern public float leftFeetBottomHeight
        {
            get;
        }

        // Get right foot bottom height.
        extern public float rightFeetBottomHeight
        {
            get;
        }

        [NativeConditional("UNITY_EDITOR")]
        extern internal bool supportsOnAnimatorMove
        {
            [NativeMethod("SupportsOnAnimatorMove")]
            get;
        }

        [NativeConditional("UNITY_EDITOR")]
        extern internal void OnUpdateModeChanged();

        [NativeConditional("UNITY_EDITOR")]
        extern internal void OnCullingModeChanged();

        [NativeConditional("UNITY_EDITOR")]
        extern internal void WriteDefaultPose();

        [NativeMethod("UpdateWithDelta")]
        extern public void Update(float deltaTime);

        public void Rebind() { Rebind(true); }
        extern private void Rebind(bool writeDefaultValues);

        // Applies the default root motion. Use in OnAvatarMove when you don't want to override the default root motion.
        extern public void ApplyBuiltinRootMotion();

        // Evalutes only the StateMachine, does not write into transforms, uses previous deltaTime
        // Mostly used for editor previews ( BlendTrees )
        [NativeConditional("UNITY_EDITOR")]
        internal void EvaluateController() { EvaluateController(0); }
        extern private void EvaluateController(float deltaTime);

        [NativeConditional("UNITY_EDITOR")]
        internal string GetCurrentStateName(int layerIndex) { return GetAnimatorStateName(layerIndex, true); }

        [NativeConditional("UNITY_EDITOR")]
        internal string GetNextStateName(int layerIndex) { return GetAnimatorStateName(layerIndex, false); }

        [NativeConditional("UNITY_EDITOR")]
        extern private string GetAnimatorStateName(int layerIndex, bool current);

        extern internal string ResolveHash(int hash);

        extern public bool logWarnings
        {
            get;
            set;
        }
        extern public bool fireEvents
        {
            get;
            set;
        }

        extern public bool keepAnimatorControllerStateOnDisable
        {
            get;
            set;
        }

        [Obsolete("GetVector is deprecated.")]
        public Vector3 GetVector(string name)                     { return Vector3.zero; }
        [Obsolete("GetVector is deprecated.")]
        public Vector3 GetVector(int id)                          { return Vector3.zero; }
        [Obsolete("SetVector is deprecated.")]
        public void SetVector(string name, Vector3 value)         {}
        [Obsolete("SetVector is deprecated.")]
        public void SetVector(int id, Vector3 value)              {}

        [Obsolete("GetQuaternion is deprecated.")]
        public Quaternion GetQuaternion(string name)              { return Quaternion.identity; }
        [Obsolete("GetQuaternion is deprecated.")]
        public Quaternion GetQuaternion(int id)                   { return Quaternion.identity; }
        [Obsolete("SetQuaternion is deprecated.")]
        public void SetQuaternion(string name, Quaternion value)  {}
        [Obsolete("SetQuaternion is deprecated.")]
        public void SetQuaternion(int id, Quaternion value)       {}
    }
}
