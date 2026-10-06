// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Playables;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // Target
    ///<summary>Target.</summary>
    ///<seealso cref="Animator.SetTarget" />
    ///<seealso cref="Animator.MatchTarget" />
    public enum AvatarTarget
    {
        // The root, the position of the game object
        ///<summary>The root, the position of the game object.</summary>
        Root = 0,
        // The body, center of mass
        ///<summary>The body, center of mass.</summary>
        Body = 1,
        // The left foot
        ///<summary>The left foot.</summary>
        LeftFoot = 2,
        // The right foot
        ///<summary>The right foot.</summary>
        RightFoot = 3,
        // The left hand
        ///<summary>The left hand.</summary>
        LeftHand = 4,
        // The right hand
        ///<summary>The right hand.</summary>
        RightHand = 5,
    }

    // IK Goal
    ///<summary>IK Goal.</summary>
    ///<remarks>Used to set and get IK weights, position and rotation.</remarks>
    ///<seealso cref="Animator.SetIKPosition" />
    ///<seealso cref="Animator.SetIKPositionWeight" />
    ///<seealso cref="Animator.SetIKRotation" />
    ///<seealso cref="Animator.SetIKRotationWeight" />
    public enum AvatarIKGoal
    {
        // The left foot
        ///<summary>The left foot.</summary>
        LeftFoot = 0,
        // The right foot
        ///<summary>The right foot.</summary>
        RightFoot = 1,
        // The left hand
        ///<summary>The left hand.</summary>
        LeftHand = 2,
        // The right hand
        ///<summary>The right hand.</summary>
        RightHand = 3
    }

    // IK Hint
    ///<summary>IK Hint.</summary>
    ///<remarks>Used to set and get IK weights and position.</remarks>
    ///<seealso cref="Animator.GetIKHintPosition" />
    ///<seealso cref="Animator.GetIKHintPositionWeight" />
    ///<seealso cref="Animator.SetIKHintPosition" />
    ///<seealso cref="Animator.SetIKHintPositionWeight" />
    public enum AvatarIKHint
    {
        // The left knee
        ///<summary>The left knee IK hint.</summary>
        LeftKnee = 0,
        // The right knee
        ///<summary>The right knee IK hint.</summary>
        RightKnee = 1,
        // The left elbow
        ///<summary>The left elbow IK hint.</summary>
        LeftElbow = 2,
        // The right elbow
        ///<summary>The right elbow IK hint.</summary>
        RightElbow = 3
    }

    ///<summary>The type of the parameter.</summary>
    ///<remarks>Can be bool, float, int or trigger.</remarks>
    public enum AnimatorControllerParameterType
    {
        ///<summary>Float type parameter.</summary>
        Float = 1,
        ///<summary>Int type parameter.</summary>
        Int = 3,
        ///<summary>Boolean type parameter.</summary>
        Bool = 4,
        ///<summary>Trigger type parameter.</summary>
        ///<remarks>Trigger work mostly like bool parameter, but their values are reset to false when used in a Transition.</remarks>
        Trigger = 9,
    }

    internal static class AnimatorControllerParameterTypeConstants
    {
        // Users should never have to deal with this type, so exposing it is actually counter-productive.
        // Instead, we put it into a constant so that we can still reap the readability benefits.
        public const int InvalidType = 0;
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

    ///<summary>The mode of the Animator's recorder.</summary>
    ///<remarks>The recorder can either be Offline, in Playback or in Record.</remarks>
    public enum AnimatorRecorderMode
    {
        ///<summary>The Animator recorder is offline.</summary>
        Offline,
        ///<summary>The Animator recorder is in Playback.</summary>
        Playback,
        ///<summary>The Animator recorder is in Record.</summary>
        Record
    }

    ///<summary>Describe the unit of a duration.</summary>
    public enum DurationUnit
    {
        ///<summary>A fixed duration is a duration expressed in seconds.</summary>
        Fixed,
        ///<summary>A normalized duration is a duration expressed in percentage.</summary>
        Normalized
    }

    // Culling mode for the Animator
    ///<summary>The culling mode for an Animator.</summary>
    ///<remarks>
    ///  <para>To specify how an Animator manages animations for objects that might not be visible, set a value from this enum to <see cref="Animator.cullingMode" />.</para>
    ///  <para />
    ///</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorCullingModeExample.cs}]]></code>
    ///</example>
    ///<seealso cref="Animator.cullingMode" />
    public enum AnimatorCullingMode
    {
        // Always animate the entire character. Object is animated even when offscreen.
        ///<summary>Always animate the entire character. Object is animated even when offscreen.</summary>
        AlwaysAnimate = 0,

        // Retarget, IK and write of Transforms are disabled when renderers are not visible.
        ///<summary>Retarget, IK and write of Transforms are disabled when renderers are not visible.</summary>
        ///<remarks>The statemachine and root motion will always be evaluated. Thus you will always receive the OnAnimatorMove callbacks.
        ///All other animation will be skipped if the character is not visible.
        ///Specifically evaluation of bone animation, IK, OnAnimatorIK will be skipped.
        ///
        ///Note that animation will still be visible in the Scene view, ie it is not affected by animation culling.</remarks>
        CullUpdateTransforms = 1,

        // Animation is completly disabled when renderers are not visible.
        ///<summary>Animation is completely disabled when renderers are not visible.</summary>
        ///<remarks>Note that animation will still be visible in the Scene view, ie it is not affected by animation culling.</remarks>
        CullCompletely = 2,

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Enum member AnimatorCullingMode.BasedOnRenderers has been deprecated. Use AnimatorCullingMode.CullUpdateTransforms instead. (UnityUpgradable) -> CullUpdateTransforms", true)]
        BasedOnRenderers = 1,
    }

    ///<summary>Controls when and how the Animator component updates animations.</summary>
    ///<remarks>
    ///  <para>Use the <c>AnimatorUpdateMode</c> enum to control the timing animation updates. The timing of animation updates can affect how animations sync with your game's logic and physics. 
    ///
    ///* **Normal Mode** (<see cref="AnimatorUpdateMode.Normal" />): Syncs animation updates with the game loop during the Update phase. Use this mode for animations that run independently of physics.
    ///* **Fixed Mode** (<see cref="AnimatorUpdateMode.Fixed" />): Syncs animation updates with the fixed update loop, primarily to align with the physics engine's updates. By default, physics evaluations happen during FixedUpdate, but you can adjust this loop to suit your project's requirements. Set <see cref="Animator.animatePhysics" /> to true to handle collisions and other physics interactions correctly.
    ///* **Unscaled Time** (<see cref="AnimatorUpdateMode.UnscaledTime" />): Keeps animations running at real-time speed, unaffected by time scaling. This is ideal for UI elements or effects that remain active when the game is paused.
    ///
    ///Each mode affects how animations behave and interact. Select the mode that aligns with your game's needs to avoid issues like skipped frames or desynchronization.
    ///
    ///</para>
    ///  <para />
    ///</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorUpdateMode.cs}]]></code>
    ///</example>
    ///<seealso cref="Animator" />
    ///<seealso cref="Time" />
    ///<seealso cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />
    ///<seealso cref="Animator.updateMode" />
    ///<seealso cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />
    ///<seealso cref="Animator" />
    ///<seealso cref="Time" />
    ///<seealso cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />
    ///<seealso cref="Animator.updateMode" />
    public enum AnimatorUpdateMode
    {
        ///<summary>Animator updates in the Update loop, aligning with the main game loop for standard animation processing.</summary>
        ///<remarks>The Normal mode is suited for most general animations that are not critically dependent on precise timing with physics. It processes animations during the regular Update phase, which is synchronized with the rendering of frames, ensuring animations are updated at the frame rate of the game, providing consistent visual results.</remarks>
        Normal = 0,
        ///<summary>Animator updates in the FixedUpdate loop. This is ideal for physics-driven animations that require frame rate independence.</summary>
        ///<remarks>This mode ensures that animation updates align with the physics engine's update loop, maintaining smooth and consistent physics interactions regardless of the frame rate. By default, it synchronizes with the FixedUpdate loop, but this can be reconfigured for specific project requirements. Essential for animations tightly coupled with physics simulations, such as character rigs and vehicle dynamics. Ensure <see cref="Animator.animatePhysics" /> is set to true for accurate synchronization with physics calculations and collision responses.</remarks>
        Fixed = 1,
        ///<summary>Animator updates independently of Time.timeScale, maintaining real-time animation progression.</summary>
        ///<remarks>This mode is beneficial when animations should continue unaffected by global time scale adjustments, such as when animating UI elements or background effects when the game is paused or slowed down. It guarantees consistent playback speed irrespective of time scale changes, enhancing player experience during time manipulation scenarios.</remarks>
        UnscaledTime = 2,

        ///<exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Enum member AnimatorUpdateMode.AnimatePhysics has been deprecated. Use AnimatorUpdateMode.Fixed to evaluate in FixedUpdate time and Animator.animatePhysics to sync transforms for physics. (UnityUpgradable) -> Fixed", true)]
        AnimatePhysics = 1
    }

    #pragma warning disable 649 //Field is never assigned to and will always have its default value
    // Information about what animation clips is played and its weight
    ///<summary>Information about clip being played and blended by the Animator.</summary>
    ///<example>
    ///  <code><![CDATA[
    /// //Create a GameObject and attach an Animator component (Click the Add Component button in the Inspector of the GameObject and go to Miscellaneous>Animator). Set up the Animator how you would like.
    /// //Attach this script to the GameObject
    ///
    /// //This script outputs the current clip from the Animator to the console
    ///using UnityEngine;
    ///
    ///public class AnimationClipInfoClipExample : MonoBehaviour
    ///{
    ///    Animator m_Animator;
    ///    AnimatorClipInfo[] m_AnimatorClipInfo;
    ///
    ///    // Use this for initialization
    ///    void Start()
    ///    {
    ///        //Fetch the Animator component from the GameObject
    ///        m_Animator = GetComponent<Animator>();
    ///        //Get the animator clip information from the Animator Controller
    ///        m_AnimatorClipInfo = m_Animator.GetCurrentAnimatorClipInfo(0);
    ///        //Output the name of the starting clip
    ///        Debug.Log("Starting clip : " + m_AnimatorClipInfo[0].clip);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Animator.GetCurrentAnimatorClipInfo" />
    ///<seealso cref="Animator.GetNextAnimatorClipInfo" />
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    public struct AnimatorClipInfo
    {
        // Animation clip that is played
        ///<summary>Returns the animation clip played by the Animator.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Create a GameObject and attach an Animator component (Click the __Add Component__ button in the Inspector of the GameObject and go to __Miscellaneous__>__Animator__). Set up the Animator how you would like.
        /// //Attach this script to the GameObject
        ///
        /// //This script outputs the current clip from the Animator to the console
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    AnimatorClipInfo[] m_AnimatorClipInfo;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Animator component from the GameObject
        ///        m_Animator = GetComponent<Animator>();
        ///        //Get the animator clip information from the Animator Controller
        ///        m_AnimatorClipInfo = m_Animator.GetCurrentAnimatorClipInfo(0);
        ///        //Output the name of the starting clip
        ///        Debug.Log("Starting clip : " + m_AnimatorClipInfo[0].clip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public AnimationClip clip
        {
            get {return m_ClipEntityId != EntityId.None ? InstanceIDToAnimationClipPPtr(m_ClipEntityId) : null; }
        }

        // The weight of the animation clip
        ///<summary>Returns the blending weight used by the <see cref="Animator" /> to blend this clip.</summary>
        public float weight
        {
            get { return m_Weight; }
        }

        [FreeFunction("AnimationBindings::InstanceIDToAnimationClipPPtr")]
        extern private static AnimationClip InstanceIDToAnimationClipPPtr(EntityId entityId);

        private EntityId m_ClipEntityId;
        private float m_Weight;
    }

    // Information about the current or next state
    ///<summary>Runtime information on the current or next animation state, managed by an Animator.</summary>
    ///<remarks>Use this struct to obtain information on the current animation state and its transitions. This information includes the name, duration, speed, and the looping status.
    ///
    ///You can also use <c>AnimatorStateInfo</c> to control animation timing and to handle blending, state changes, and event timing.
    ///
    ///
    ///
    ///
    ///The following example demonstrates how to use <c>AnimatorStateInfo</c> to manage transitions and adjust playback in real-time.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorStateInfo.cs}]]></code>
    ///</example>
    ///<seealso cref="Animator" />
    ///<seealso cref="T:UnityEditor.Animations.AnimatorController" />
    ///<seealso cref="Animator.StringToHash" />
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [RequiredByNativeCode]
    public struct AnimatorStateInfo
    {
        // Does /name/ match the name of the active state in the statemachine.
        ///<summary>Checks if <c>name</c> matches the name of the active state in the state machine.</summary>
        ///<remarks>
        ///  <para>The name should be in the form Layer.Name or Layer.SubStateMachine.Name. For example, <c>Base.Idle</c> or <c>Base.RunSM.JogForward</c>.
        ///
        ///This method calls <see cref="Animator.StringToHash" /> on the name parameter and compares it to <see cref="AnimatorStateInfo.shortNameHash" /> and <see cref="AnimatorStateInfo.fullPathHash" /> internally. If you call this method often, consider precomputing the hash of the name to improve performance.</para>
        ///  <para />
        ///</remarks>
        ///<param name="name">The name to check.</param>
        ///<returns>True if the animation state has the given name, false otherwise.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorStateInfoIsNameExample.cs}]]></code>
        ///</example>
        ///<seealso cref="Animator.StringToHash" />
        ///<seealso cref="AnimatorStateInfo.IsTag" />
        public bool IsName(string name)    { int hash = Animator.StringToHash(name); return hash == m_FullPath || hash == m_Name || hash == m_Path; }

        // For backwards compatibility this is actually the path...
        ///<summary>The full path hash for this state.</summary>
        ///<remarks>The hash is generated using <see cref="Animator.StringToHash" />.</remarks>
        public int fullPathHash             { get { return m_FullPath; } }

        ///<summary>The hashed name of the State.</summary>
        ///<remarks>The hash includes the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the string used to generate the hash is <c>Base Layer.Run</c>.</remarks>
        [Obsolete("AnimatorStateInfo.nameHash has been deprecated. Use AnimatorStateInfo.fullPathHash instead.")]
        public int nameHash                 { get { return m_Path; } }

        ///<summary>The hash is generated using <see cref="Animator.StringToHash" />. The hash does not include the name of the parent layer.</summary>
        public int shortNameHash            { get { return m_Name; } }

        // Normalized time of the State
        ///<summary>Normalized time of the State.</summary>
        ///<remarks>The normalized time is a progression ratio. The integer part is the number of times the State has looped. The fractional part is a percentage (0-1) that represents the progress of the current loop. For example, a normalized time of 2.5 means that the State has looped twice (2) and has progressed halfway (50% or .5) through its third loop.</remarks>
        public float normalizedTime         { get { return m_NormalizedTime; } }

        // Current duration of the state
        ///<summary>Current duration of the state.</summary>
        ///<remarks>In seconds
        ///Can vary when the State contains a Blend Tree.</remarks>
        public float length                 { get { return m_Length; } }

        // State speed
        ///<summary>The playback speed of the animation. 1 is the normal playback speed.</summary>
        ///<remarks>A negative playback speed will play the animation from the end.
        ///
        ///see <see cref="P:UnityEditor.Animations.AnimatorState.speed" />.</remarks>
        public float speed                  { get { return m_Speed; } }

        // State speed multiplier
        ///<summary>The speed multiplier for this state.</summary>
        ///<remarks>A negative speed multiplier will play the animation backwards.
        ///If no speed parameter as been set for this <see cref="T:UnityEditor.Animations.AnimatorState" />, the default value will be 1.
        ///
        ///see <see cref="P:UnityEditor.Animations.AnimatorState.speedParameter" />, <see cref="P:UnityEditor.Animations.AnimatorState.speedParameterActive" />.</remarks>
        public float speedMultiplier        { get { return m_SpeedMultiplier; } }

        // The Tag of the State
        ///<summary>The Tag of the State.</summary>
        ///<remarks>The hash is generated using <see cref="Animator.StringToHash" />.</remarks>
        public int tagHash                  { get { return m_Tag; } }

        // Does /tag/ match the tag of the active state in the statemachine.
        ///<summary>Checks whether the animation state has the specified tag.</summary>
        ///<remarks>You can manually set a tag for each state in the [Animator State inspector](xref:class-State) or with the <see cref="P:UnityEditor.Animations.AnimatorState.tag" /> property. Use <see cref="AnimatorStateInfo.IsTag" /> to query if an activate state in the <see cref="Animator" /> component has a tag that matches a specific string.
        ///<see cref="AnimatorStateInfo.IsTag" /> calls <see cref="Animator.StringToHash" /> on the tag parameter and compares it to <see cref="AnimatorStateInfo.tagHash" /> internally; if you call that method often, consider precomputing the hash of the tag for a gain in performance.</remarks>
        ///<param name="tag">The tag to check.</param>
        ///<returns>True if the animation state has the specified tag, false otherwise.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorStateInfoIsTagExample.cs}]]></code>
        ///</example>
        ///<seealso cref="AnimatorStateInfo.IsName" />
        ///<seealso cref="P:UnityEditor.Animations.AnimatorState.tag" />
        public bool IsTag(string tag)      { return Animator.StringToHash(tag) == m_Tag; }

        // Is the state looping
        ///<summary>Is the state looping.</summary>
        ///<remarks>All animations in the state must be looping.</remarks>
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
    ///<summary>Information about the current transition on a specific state machine layer.</summary>
    ///<remarks>Use <see cref="Animator.GetAnimatorTransitionInfo" /> to access transition information during playmode. Use this information to, for example, check the current transition and measure its progress. You can use the weight of the target state to measure the progress of a transition.
    ///
    ///This struct is not related to <see cref="T:UnityEditor.Animations.AnimatorTransition" /> which is only available in the Editor and is used to build state machine transitions from code.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/GrabBehaviour.cs}]]></code>
    ///</example>
    ///<seealso cref="AnimatorStateInfo" />
    ///<seealso cref="Animator.GetAnimatorTransitionInfo" />
    ///<seealso cref="M:UnityEngine.Animations.AnimatorControllerPlayable.GetAnimatorTransitionInfo" />
    [NativeHeader("Modules/Animation/AnimatorInfo.h")]
    [RequiredByNativeCode]
    public struct AnimatorTransitionInfo
    {
        // Does /name/ match the name of the active Transition.
        ///<summary>Does <c>name</c> match the name of the active Transition.</summary>
        ///<remarks>Format is "CURRENT_STATE -&gt; NEXT_STATE".</remarks>
        public bool IsName(string name) { return Animator.StringToHash(name) == m_Name  || Animator.StringToHash(name) == m_FullPath; }

        // Does /userName/ match the name of the active Transition.
        ///<summary>Does <c>userName</c> match the name of the active Transition.</summary>
        public bool IsUserName(string name) { return Animator.StringToHash(name) == m_UserName; }


        ///<summary>The hash name of the Transition.</summary>
        ///<remarks>Format is "FULLPATH.CURRENT_STATE -&gt; FULLPATH.NEXT_STATE".
        ///
        ///The hash is generated using <see cref="Animator.StringToHash" />.</remarks>
        public int fullPathHash               { get { return m_FullPath; } }

        // The unique name of the Transition
        ///<summary>The simplified name of the Transition.</summary>
        ///<remarks>Format is "CURRENT_STATE -&gt; NEXT_STATE".
        ///The hash is generated using <see cref="Animator.StringToHash" />.</remarks>
        public int nameHash                   { get { return m_Name; } }

        // The user-specidied name of the Transition
        ///<summary>The user-specified name of the Transition.</summary>
        ///<remarks>The hash is generated using <see cref="Animator.StringToHash" />.</remarks>
        public int userNameHash               { get { return m_UserName; } }

        // The duration unit: can be either Fixed (in seconds) or Normalized (in percentage)
        ///<summary>The unit of the transition duration.</summary>
        ///<remarks>Can be either <see cref="DurationUnit.Fixed" /> (in seconds) or <see cref="DurationUnit.Normalized" /> (in percentage).</remarks>
        ///<seealso cref="AnimatorTransitionInfo.duration" />
        public DurationUnit durationUnit      { get { return m_HasFixedDuration ? DurationUnit.Fixed : DurationUnit.Normalized; } }

        // Duration of the Transition
        ///<summary>Duration of the transition.</summary>
        ///<remarks>Depending on <see cref="AnimatorTransitionInfo.durationUnit" /> the duration can either be expressed in seconds (i.e. <see cref="DurationUnit.Fixed" />) or in percentage (i.e. <see cref="DurationUnit.Normalized" />). A normalized duration is based on the source state duration.
        ///
        ///Note: a normalized duration converted in seconds can change from frame to frame, since the source state duration can change depending on varying factors, like the weights in a blendtree.</remarks>
        public float duration                 { get { return m_Duration; } }

        // Normalized time of the Transition
        ///<summary>Normalized time of the Transition.</summary>
        ///<remarks>0.0f to 1.0f.</remarks>
        public float normalizedTime           { get { return m_NormalizedTime; } }

        ///<summary>Returns true if the transition is from an AnyState node, or from <see cref="Animator.CrossFade" />.</summary>
        public bool anyState                  { get { return m_AnyState; } }

        internal bool entry                   { get { return (m_TransitionType & (int)TransitionType.Entry) != 0; }}

        internal bool exit                    { get { return (m_TransitionType & (int)TransitionType.Exit) != 0; }}

        [NativeName("fullPathHash")]
        private int   m_FullPath;
        [NativeName("userNameHash")]
        private int   m_UserName;
        [NativeName("nameHash")]
        private int   m_Name;
        [NativeName("hasFixedDuration")]
        private bool  m_HasFixedDuration;
        [NativeName("duration")]
        private float m_Duration;
        [NativeName("normalizedTime")]
        private float m_NormalizedTime;
        [NativeName("anyState")]
        private bool  m_AnyState;
        [NativeName("transitionType")]
        private int   m_TransitionType;
    }
    #pragma warning restore 649


    // To specify position and rotation weight mask for Animator::MatchTarget
    ///<summary>Use this struct to specify the position and rotation weight mask for <see cref="Animator.MatchTarget" />.</summary>
    [NativeHeader("Modules/Animation/Animator.h")]
    public struct MatchTargetWeightMask
    {
        // MatchTargetWeightMask contructor
        ///<summary>MatchTargetWeightMask contructor.</summary>
        ///<param name="positionXYZWeight">Position XYZ weight.</param>
        ///<param name="rotationWeight">Rotation weight.</param>
        public MatchTargetWeightMask(Vector3 positionXYZWeight, float rotationWeight)
        {
            m_PositionXYZWeight = positionXYZWeight;
            m_RotationWeight = rotationWeight;
        }

        // Position XYZ weight
        ///<summary>Position XYZ weight.</summary>
        public Vector3 positionXYZWeight
        {
            get { return m_PositionXYZWeight; }
            set { m_PositionXYZWeight = value; }
        }

        // Rotation weight
        ///<summary>Rotation weight.</summary>
        public float rotationWeight
        {
            get { return m_RotationWeight; }
            set { m_RotationWeight = value; }
        }

        private Vector3 m_PositionXYZWeight;
        private float m_RotationWeight;
    }

    // Interface to control the Mecanim animation system
    [NativeHeader("Modules/Animation/Animator.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animator.bindings.h")]
    [global::UnityEngine.NativeClass("Animator", PersistentTypeId = 95)]
    [UsedByNativeCode]
    public partial class Animator : Behaviour
    {
        internal Animator(global::UnityEngine.EntityId id) : base(id) {}
        public Animator() {}
        // Returns true if the current rig is optimizable
        ///<summary>Returns true if the current rig is optimizable with <see cref="AnimatorUtility.OptimizeTransformHierarchy" />.</summary>
        extern public bool isOptimizable
        {
            [NativeMethod("IsOptimizable")]
            get;
        }

        // Returns true if the current rig is ''humanoid'', false if it is ''generic''
        ///<summary>Returns true if the current rig is humanoid, false if it is generic.</summary>
        extern public bool isHuman
        {
            [NativeMethod("IsHuman")]
            get;
        }

        // Returns true if the current generic rig has a root motion
        ///<summary>Returns true if the current rig has root motion.</summary>
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
        ///<summary>Returns the scale of the current Avatar for a humanoid rig, (1 by default if the rig is generic).</summary>
        ///<remarks>The scale is relative to Unity's Default Avatar.</remarks>
        extern public float humanScale
        {
            get;
        }

        // Return true if the animator is currently initialized and ready to be use
        ///<summary>Returns whether the animator is initialized successfully.</summary>
        ///<remarks>See <see cref="Animator.Rebind" /> to manually initialize the animator.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class CheckAndRebind : MonoBehaviour
        ///{
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///
        ///        if (!animator.isInitialized)
        ///            animator.Rebind();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool isInitialized
        {
            [NativeMethod("IsInitialized")]
            get;
        }

        // Gets the value of a float parameter
        ///<summary>Returns the value of the given float parameter.</summary>
        ///<remarks>If the float parameter you specify doesn't exist, the float returns as 0.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<returns>The value of the parameter.</returns>
        public float GetFloat(string name)             { return GetFloatString(name); }
        // Gets the value of a float parameter
        ///<summary>Returns the value of the given float parameter.</summary>
        ///<remarks>If the float parameter you specify doesn't exist, the float returns as 0.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<returns>The value of the parameter.</returns>
        public float GetFloat(int id)                  { return GetFloatID(id); }
        // Sets the value of a float parameter
        ///<summary>Send float values to the Animator to affect transitions.</summary>
        ///<remarks>
        ///  <para>Use SetFloat in a script to send float values to the Animator in order to activate transitions. In the Animator, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</para>
        ///  <para>
        ///    <img src="AnimatorSetFloat.png" />
        ///
        ///Above is an example setup of the Animator for accepting floats.</para>
        ///</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //The code below shows how to send the horizontal value of the controller or keys to the Animator.
        /// //You must assign the same parameter name in the Animator as you set in SetFloat, in this case “horizontalSpeed”. You must also handle the transition conditions in the Animator, to tell which values should cause each transition.
        /// //For example, the walking animation triggers when the horizontal value is above 0, and the running animation triggers when the horizontal value reaches past 0.5. Assigning animations to states are also done in the Animator.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    float m_HorizontalMovement;
        ///
        ///    void Start()
        ///    {
        ///        //Get the animator, which you attach to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Translate the left and right button presses or the horizontal joystick movements to a float
        ///        m_HorizontalMovement = Input.GetAxis("Horizontal");
        ///        //Sends the value from the horizontal axis input to the animator. Change the settings in the
        ///        //Animator to define when the character is walking or running
        ///        m_Animator.SetFloat("horizontalSpeed", m_HorizontalMovement);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetFloat(string name, float value) { SetFloatString(name, value); }
        // Sets the value of a float parameter
        ///<summary>Send float values to the Animator to affect transitions.</summary>
        ///<remarks>
        ///  <para>Use SetFloat in a script to send float values to the Animator in order to activate transitions. In the Animator, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</para>
        ///  <para>
        ///    <img src="AnimatorSetFloat.png" />
        ///
        ///Above is an example setup of the Animator for accepting floats.</para>
        ///</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        ///<param name="dampTime">The damper total time.</param>
        ///<param name="deltaTime">The delta time to give to the damper.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //The code below shows how to send the horizontal value of the controller or keys to the Animator.
        /// //You must assign the same parameter name in the Animator as you set in SetFloat, in this case “horizontalSpeed”. You must also handle the transition conditions in the Animator, to tell which values should cause each transition.
        /// //For example, the walking animation triggers when the horizontal value is above 0, and the running animation triggers when the horizontal value reaches past 0.5. Assigning animations to states are also done in the Animator.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    float m_HorizontalMovement;
        ///
        ///    void Start()
        ///    {
        ///        //Get the animator, which you attach to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Translate the left and right button presses or the horizontal joystick movements to a float
        ///        m_HorizontalMovement = Input.GetAxis("Horizontal");
        ///        //Sends the value from the horizontal axis input to the animator. Change the settings in the
        ///        //Animator to define when the character is walking or running
        ///        m_Animator.SetFloat("horizontalSpeed", m_HorizontalMovement);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetFloat(string name, float value, float dampTime, float deltaTime) { SetFloatStringDamp(name, value, dampTime, deltaTime); }

        // Sets the value of a float parameter
        ///<summary>Send float values to the Animator to affect transitions.</summary>
        ///<remarks>
        ///  <para>Use SetFloat in a script to send float values to the Animator in order to activate transitions. In the Animator, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</para>
        ///  <para>
        ///    <img src="AnimatorSetFloat.png" />
        ///
        ///Above is an example setup of the Animator for accepting floats.</para>
        ///</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //The code below shows how to send the horizontal value of the controller or keys to the Animator.
        /// //You must assign the same parameter name in the Animator as you set in SetFloat, in this case “horizontalSpeed”. You must also handle the transition conditions in the Animator, to tell which values should cause each transition.
        /// //For example, the walking animation triggers when the horizontal value is above 0, and the running animation triggers when the horizontal value reaches past 0.5. Assigning animations to states are also done in the Animator.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    float m_HorizontalMovement;
        ///
        ///    void Start()
        ///    {
        ///        //Get the animator, which you attach to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Translate the left and right button presses or the horizontal joystick movements to a float
        ///        m_HorizontalMovement = Input.GetAxis("Horizontal");
        ///        //Sends the value from the horizontal axis input to the animator. Change the settings in the
        ///        //Animator to define when the character is walking or running
        ///        m_Animator.SetFloat("horizontalSpeed", m_HorizontalMovement);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetFloat(int id, float value)       { SetFloatID(id, value); }
        // Sets the value of a float parameter
        ///<summary>Send float values to the Animator to affect transitions.</summary>
        ///<remarks>
        ///  <para>Use SetFloat in a script to send float values to the Animator in order to activate transitions. In the Animator, define what values affect how certain animations transition. This is useful in various situations, especially in animation cycles such as movement animations where you might require the character to walk or run depending on the button pressure applied.</para>
        ///  <para>
        ///    <img src="AnimatorSetFloat.png" />
        ///
        ///Above is an example setup of the Animator for accepting floats.</para>
        ///</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        ///<param name="dampTime">The damper total time.</param>
        ///<param name="deltaTime">The delta time to give to the damper.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //The code below shows how to send the horizontal value of the controller or keys to the Animator.
        /// //You must assign the same parameter name in the Animator as you set in SetFloat, in this case “horizontalSpeed”. You must also handle the transition conditions in the Animator, to tell which values should cause each transition.
        /// //For example, the walking animation triggers when the horizontal value is above 0, and the running animation triggers when the horizontal value reaches past 0.5. Assigning animations to states are also done in the Animator.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    float m_HorizontalMovement;
        ///
        ///    void Start()
        ///    {
        ///        //Get the animator, which you attach to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Translate the left and right button presses or the horizontal joystick movements to a float
        ///        m_HorizontalMovement = Input.GetAxis("Horizontal");
        ///        //Sends the value from the horizontal axis input to the animator. Change the settings in the
        ///        //Animator to define when the character is walking or running
        ///        m_Animator.SetFloat("horizontalSpeed", m_HorizontalMovement);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetFloat(int id, float value, float dampTime, float deltaTime) { SetFloatIDDamp(id, value, dampTime, deltaTime); }

        // Gets the value of a bool parameter
        ///<summary>Returns the value of the given boolean parameter.</summary>
        ///<remarks>Return the current state of a bool parameter within the Animator Controller. Use the parameter’s name or ID to search for the appropriate one.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<returns>The value of the parameter.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject with an Animator component attached.
        /// //For this example, create parameters in the Animator and name them “Crouch” and “Jump”
        /// //Apply these parameters to your transitions between states
        ///
        /// //This script allows you to set a Boolean Animator parameter on and set another Boolean parameter to off if it is currently playing. Press the space key to do this.
        ///
        ///using UnityEngine;
        ///
        ///public class AnimatorGetBool : MonoBehaviour
        ///{
        ///    //Fetch the Animator
        ///    Animator m_Animator;
        ///    // Use this to decide if the GameObject can jump or not
        ///    bool m_Jump;
        ///
        ///    void Start()
        ///    {
        ///        //This gets the Animator, which should be attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///        // The GameObject cannot jump
        ///        m_Jump = false;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the space bar to enable the "Jump" parameter in the Animator Controller
        ///        if (Input.GetKey(KeyCode.Space))
        ///        {
        ///            //Set the "Jump" parameter in the Animator Controller to true
        ///            m_Animator.SetBool("Jump", true);
        ///            //Check to see if the "Crouch" parameter is enabled
        ///            if (m_Animator.GetBool("Crouch"))
        ///            {
        ///                //If the "Crouch" parameter is enabled, disable it as the Animation should no longer be crouching
        ///                m_Animator.SetBool("Crouch", false);
        ///            }
        ///        }
        ///        //Otherwise the "Jump" parameter should be false
        ///        else m_Animator.SetBool("Jump", false);
        ///
        ///        //Press the down arrow key to enable the "Crouch" parameter
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///            m_Animator.SetBool("Crouch", true);
        ///        else
        ///            m_Animator.SetBool("Crouch", false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool GetBool(string name)                { return GetBoolString(name); }
        // Gets the value of a bool parameter
        ///<summary>Returns the value of the given boolean parameter.</summary>
        ///<remarks>Return the current state of a bool parameter within the Animator Controller. Use the parameter’s name or ID to search for the appropriate one.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<returns>The value of the parameter.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject with an Animator component attached.
        /// //For this example, create parameters in the Animator and name them “Crouch” and “Jump”
        /// //Apply these parameters to your transitions between states
        ///
        /// //This script allows you to set a Boolean Animator parameter on and set another Boolean parameter to off if it is currently playing. Press the space key to do this.
        ///
        ///using UnityEngine;
        ///
        ///public class AnimatorGetBool : MonoBehaviour
        ///{
        ///    //Fetch the Animator
        ///    Animator m_Animator;
        ///    // Use this to decide if the GameObject can jump or not
        ///    bool m_Jump;
        ///
        ///    void Start()
        ///    {
        ///        //This gets the Animator, which should be attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///        // The GameObject cannot jump
        ///        m_Jump = false;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the space bar to enable the "Jump" parameter in the Animator Controller
        ///        if (Input.GetKey(KeyCode.Space))
        ///        {
        ///            //Set the "Jump" parameter in the Animator Controller to true
        ///            m_Animator.SetBool("Jump", true);
        ///            //Check to see if the "Crouch" parameter is enabled
        ///            if (m_Animator.GetBool("Crouch"))
        ///            {
        ///                //If the "Crouch" parameter is enabled, disable it as the Animation should no longer be crouching
        ///                m_Animator.SetBool("Crouch", false);
        ///            }
        ///        }
        ///        //Otherwise the "Jump" parameter should be false
        ///        else m_Animator.SetBool("Jump", false);
        ///
        ///        //Press the down arrow key to enable the "Crouch" parameter
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///            m_Animator.SetBool("Crouch", true);
        ///        else
        ///            m_Animator.SetBool("Crouch", false);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool GetBool(int id)                     { return GetBoolID(id); }
        // Sets the value of a bool parameter
        ///<summary>Sets the value of the given boolean parameter.</summary>
        ///<remarks>Use Animator.SetBool to pass Boolean values to an [Animator Controller](xref:class-AnimatorController)
        ///via script.
        ///
        ///Use this to trigger transitions between Animator states. For example, triggering a death animation by setting an “alive” boolean to false. See documentation on [Animation](xref:AnimatorControllerCreation) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the Animator.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //Set up a new Boolean parameter in the Unity Animator and name it, in this case “Jump”.
        /// //Set up transitions between each state that the animation could follow. For example, the player could be running or idle before they jump, so both would need transitions into the animation.
        /// //If the “Jump” boolean is set to true at any point, the m_Animator plays the animation. However, if it is ever set to false, the animation would return to the appropriate state (“Idle”).
        /// //This script enables and disables this boolean in this case by listening for the mouse click or a tap of the screen.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    //Fetch the Animator
        ///    Animator m_Animator;
        ///    // Use this for deciding if the GameObject can jump or not
        ///    bool m_Jump;
        ///
        ///    void Start()
        ///    {
        ///        //This gets the Animator, which should be attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///        // The GameObject cannot jump
        ///        m_Jump = false;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Click the mouse or tap the screen to change the animation
        ///        if (Input.GetMouseButtonDown(0))
        ///            m_Jump = true;
        ///
        ///        //Otherwise the GameObject cannot jump.
        ///        else m_Jump = false;
        ///
        ///        //If the GameObject is not jumping, send that the Boolean “Jump” is false to the Animator. The jump animation does not play.
        ///        if (m_Jump == false)
        ///            m_Animator.SetBool("Jump", false);
        ///
        ///        //The GameObject is jumping, so send the Boolean as enabled to the Animator. The jump animation plays.
        ///        if (m_Jump == true)
        ///            m_Animator.SetBool("Jump", true);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetBool(string name, bool value)    { SetBoolString(name, value); }
        // Sets the value of a bool parameter
        ///<summary>Sets the value of the given boolean parameter.</summary>
        ///<remarks>Use Animator.SetBool to pass Boolean values to an [Animator Controller](xref:class-AnimatorController)
        ///via script.
        ///
        ///Use this to trigger transitions between Animator states. For example, triggering a death animation by setting an “alive” boolean to false. See documentation on [Animation](xref:AnimatorControllerCreation) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the Animator.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //Set up a new Boolean parameter in the Unity Animator and name it, in this case “Jump”.
        /// //Set up transitions between each state that the animation could follow. For example, the player could be running or idle before they jump, so both would need transitions into the animation.
        /// //If the “Jump” boolean is set to true at any point, the m_Animator plays the animation. However, if it is ever set to false, the animation would return to the appropriate state (“Idle”).
        /// //This script enables and disables this boolean in this case by listening for the mouse click or a tap of the screen.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    //Fetch the Animator
        ///    Animator m_Animator;
        ///    // Use this for deciding if the GameObject can jump or not
        ///    bool m_Jump;
        ///
        ///    void Start()
        ///    {
        ///        //This gets the Animator, which should be attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///        // The GameObject cannot jump
        ///        m_Jump = false;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Click the mouse or tap the screen to change the animation
        ///        if (Input.GetMouseButtonDown(0))
        ///            m_Jump = true;
        ///
        ///        //Otherwise the GameObject cannot jump.
        ///        else m_Jump = false;
        ///
        ///        //If the GameObject is not jumping, send that the Boolean “Jump” is false to the Animator. The jump animation does not play.
        ///        if (m_Jump == false)
        ///            m_Animator.SetBool("Jump", false);
        ///
        ///        //The GameObject is jumping, so send the Boolean as enabled to the Animator. The jump animation plays.
        ///        if (m_Jump == true)
        ///            m_Animator.SetBool("Jump", true);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetBool(int id, bool value)         { SetBoolID(id, value); }

        // Gets the value of an integer parameter
        ///<summary>Returns the value of the given integer parameter.</summary>
        ///<param name="name">The parameter name.</param>
        ///<returns>The value of the parameter.</returns>
        public int GetInteger(string name)              { return GetIntegerString(name); }
        // Gets the value of an integer parameter
        ///<summary>Returns the value of the given integer parameter.</summary>
        ///<param name="id">The parameter ID.</param>
        ///<returns>The value of the parameter.</returns>
        public int GetInteger(int id)                   { return GetIntegerID(id); }
        // Sets the value of an integer parameter
        ///<summary>Sets the value of the given integer parameter.</summary>
        ///<remarks>Use this as a way to trigger transitions between Animator states. One way of using Integers instead of Floats or Booleans is to use it for something that has multiple states, for example directions (turn left, turn right etc.). Each direction could correspond to a number instead of having multiple Booleans that have to be reset each time.
        ///
        ///See documentation on [Animation](xref:Animator) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the Animator.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<param name="value">The new parameter value.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //This script sends messages to an Animator component to tell it to make transitions based on an integer named “States”. You change and send this integer to the Animator by pressing the space and arrow keys.
        ///
        /// //In order for this script to work, you have to set up your Animator Controller so the script can interact with it.
        /// //Create a new Animator Controller if you do not already have one you want to use. To do this, click on the GameObject you want to animate and go to its Inspector window. Click the __Add Component__ button and go to __Miscellaneous__>__Animator__).
        /// //Double click the Animator to see the Animator Controller window.  Open the __Parameters__ tab and click the plus icon to add a new parameter. Choose Int from the dropdown. Name the new Integer (for this script, call it “States”).
        /// //Create a few animation states (right click the grid and choose __Create State__>__Empty__) and choose an Animation for each in the __Motion__ field.
        /// //Next create transitions between each of the states (right click the state, choose __Make Transition__ and click on the state you want it to transition to).
        /// //Finally, click on one of the arrows to bring up its Inspector. Click the + icon under the Conditions section and choose the parameter you made (“States”). Change __Greater__ to __Equals__ and choose a number that you want to represent this state. Do the same with any other states.
        /// //You may want to set up transitions back to the first animation state so that when the button is let go, it will return to the first state. You may also want to uncheck the __Has Exit Time__ box for each transition. Otherwise transitions will wait for an animation to finish before proceeding.
        ///
        ///using UnityEngine;
        ///
        ///public class AnimatorSetIntExample : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Animator from the GameObject you attached the script to
        ///        m_Animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Check if the horizontal buttons (A,D, left and right arrow keys) are being pressed
        ///        if (Input.GetAxis("Horizontal") > 0 || Input.GetAxis("Horizontal") < 0)
        ///            //Set the integer named "States" in your Animator to 1. If the Animator is set up properly, this should trigger an animation.
        ///            m_Animator.SetInteger("States", 1);
        ///        //Press the down arrow key to start another animation transition
        ///        else if (Input.GetKey(KeyCode.DownArrow))
        ///            //Set the "States" integer to 2. This triggers the animation that should start when "States" is equal to 2
        ///            m_Animator.SetInteger("States", 2);
        ///        //Press the space key to set the "States integer to 3
        ///        else if (Input.GetKey(KeyCode.Space))
        ///            m_Animator.SetInteger("States", 3);
        ///        else
        ///            //If all the other keys are let go, set the "States" integer to 0.
        ///            m_Animator.SetInteger("States", 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetInteger(string name, int value)  { SetIntegerString(name, value); }

        // Sets the value of an integer parameter
        ///<summary>Sets the value of the given integer parameter.</summary>
        ///<remarks>Use this as a way to trigger transitions between Animator states. One way of using Integers instead of Floats or Booleans is to use it for something that has multiple states, for example directions (turn left, turn right etc.). Each direction could correspond to a number instead of having multiple Booleans that have to be reset each time.
        ///
        ///See documentation on [Animation](xref:Animator) for more information on setting up Animators.
        ///
        ///Note: You can identify the parameter by name or by ID number, but the name or ID number must be the same as the parameter you want to change in the Animator.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<param name="value">The new parameter value.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //This script sends messages to an Animator component to tell it to make transitions based on an integer named “States”. You change and send this integer to the Animator by pressing the space and arrow keys.
        ///
        /// //In order for this script to work, you have to set up your Animator Controller so the script can interact with it.
        /// //Create a new Animator Controller if you do not already have one you want to use. To do this, click on the GameObject you want to animate and go to its Inspector window. Click the __Add Component__ button and go to __Miscellaneous__>__Animator__).
        /// //Double click the Animator to see the Animator Controller window.  Open the __Parameters__ tab and click the plus icon to add a new parameter. Choose Int from the dropdown. Name the new Integer (for this script, call it “States”).
        /// //Create a few animation states (right click the grid and choose __Create State__>__Empty__) and choose an Animation for each in the __Motion__ field.
        /// //Next create transitions between each of the states (right click the state, choose __Make Transition__ and click on the state you want it to transition to).
        /// //Finally, click on one of the arrows to bring up its Inspector. Click the + icon under the Conditions section and choose the parameter you made (“States”). Change __Greater__ to __Equals__ and choose a number that you want to represent this state. Do the same with any other states.
        /// //You may want to set up transitions back to the first animation state so that when the button is let go, it will return to the first state. You may also want to uncheck the __Has Exit Time__ box for each transition. Otherwise transitions will wait for an animation to finish before proceeding.
        ///
        ///using UnityEngine;
        ///
        ///public class AnimatorSetIntExample : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Animator from the GameObject you attached the script to
        ///        m_Animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Check if the horizontal buttons (A,D, left and right arrow keys) are being pressed
        ///        if (Input.GetAxis("Horizontal") > 0 || Input.GetAxis("Horizontal") < 0)
        ///            //Set the integer named "States" in your Animator to 1. If the Animator is set up properly, this should trigger an animation.
        ///            m_Animator.SetInteger("States", 1);
        ///        //Press the down arrow key to start another animation transition
        ///        else if (Input.GetKey(KeyCode.DownArrow))
        ///            //Set the "States" integer to 2. This triggers the animation that should start when "States" is equal to 2
        ///            m_Animator.SetInteger("States", 2);
        ///        //Press the space key to set the "States integer to 3
        ///        else if (Input.GetKey(KeyCode.Space))
        ///            m_Animator.SetInteger("States", 3);
        ///        else
        ///            //If all the other keys are let go, set the "States" integer to 0.
        ///            m_Animator.SetInteger("States", 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetInteger(int id, int value)       { SetIntegerID(id, value); }

        // Sets the trigger parameter on
        ///<summary>Sets the value of the given trigger parameter.</summary>
        ///<remarks>This method allows you to set (i.e. activate) an animation trigger, to cause a change in flow in the state machine of an animator controller. The [Animation Parameters](xref:AnimationParameters) page describes the purpose of the Animator Controller Parameters window.  <c>Trigger</c> is one of the 4 selectable options. Selecting this adds a <c>Trigger</c> to the list of chosen parameters.  Once this is added to the selected list it can be named.  Unlike <c>bool</c>s which have the same <c>true/false</c> option, <c>Trigger</c>s have a <c>true</c> option which automatically returns back to <c>false</c>.  A typical example might be to have a Jump option.  If this option is entered during run-time the character will jump.  At the end of the Jump the previous motion (perhaps a walk or run state) will be returned to.
        ///
        ///In the example script below, pressing <c>UpArrow</c> or <c>DownArrow</c> activates the Jump or Crouch triggers using  <see cref="SetTrigger" />.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject with an Animator component attached.
        /// //For this example, create parameters in the Animator and name them “Crouch” and “Jump”
        /// //Apply these parameters to your transitions between states
        ///
        /// //This script allows you to trigger an Animator parameter and reset the other that could possibly still be active. Press the up and down arrow keys to do this.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///
        ///    void Start()
        ///    {
        ///        //Get the Animator attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the up arrow button to reset the trigger and set another one
        ///        if (Input.GetKey(KeyCode.UpArrow))
        ///        {
        ///            //Reset the "Crouch" trigger
        ///            m_Animator.ResetTrigger("Crouch");
        ///
        ///            //Send the message to the Animator to activate the trigger parameter named "Jump"
        ///            m_Animator.SetTrigger("Jump");
        ///        }
        ///
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///        {
        ///            //Reset the "Jump" trigger
        ///            m_Animator.ResetTrigger("Jump");
        ///
        ///            //Send the message to the Animator to activate the trigger parameter named "Crouch"
        ///            m_Animator.SetTrigger("Crouch");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTrigger(string name)       { SetTriggerString(name); }

        // Sets the trigger parameter at on
        ///<summary>Sets the value of the given trigger parameter.</summary>
        ///<remarks>This method allows you to set (i.e. activate) an animation trigger, to cause a change in flow in the state machine of an animator controller. The [Animation Parameters](xref:AnimationParameters) page describes the purpose of the Animator Controller Parameters window.  <c>Trigger</c> is one of the 4 selectable options. Selecting this adds a <c>Trigger</c> to the list of chosen parameters.  Once this is added to the selected list it can be named.  Unlike <c>bool</c>s which have the same <c>true/false</c> option, <c>Trigger</c>s have a <c>true</c> option which automatically returns back to <c>false</c>.  A typical example might be to have a Jump option.  If this option is entered during run-time the character will jump.  At the end of the Jump the previous motion (perhaps a walk or run state) will be returned to.
        ///
        ///In the example script below, pressing <c>UpArrow</c> or <c>DownArrow</c> activates the Jump or Crouch triggers using  <see cref="SetTrigger" />.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject with an Animator component attached.
        /// //For this example, create parameters in the Animator and name them “Crouch” and “Jump”
        /// //Apply these parameters to your transitions between states
        ///
        /// //This script allows you to trigger an Animator parameter and reset the other that could possibly still be active. Press the up and down arrow keys to do this.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///
        ///    void Start()
        ///    {
        ///        //Get the Animator attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the up arrow button to reset the trigger and set another one
        ///        if (Input.GetKey(KeyCode.UpArrow))
        ///        {
        ///            //Reset the "Crouch" trigger
        ///            m_Animator.ResetTrigger("Crouch");
        ///
        ///            //Send the message to the Animator to activate the trigger parameter named "Jump"
        ///            m_Animator.SetTrigger("Jump");
        ///        }
        ///
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///        {
        ///            //Reset the "Jump" trigger
        ///            m_Animator.ResetTrigger("Jump");
        ///
        ///            //Send the message to the Animator to activate the trigger parameter named "Crouch"
        ///            m_Animator.SetTrigger("Crouch");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetTrigger(int id)       { SetTriggerID(id); }

        // Resets the trigger parameter at off
        ///<summary>Resets the value of the given trigger parameter.</summary>
        ///<remarks>Use this to reset a Trigger [parameter](xref:AnimationParameters) in an Animator Controller that could still be active. Make sure to create a parameter in the Animator Controller with the same name. See <see cref="Animator.SetTrigger" /> for more information about how to set a Trigger.</remarks>
        ///<param name="name">The parameter name.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject with an Animator component attached.
        /// //For this example, create parameters in the Animator and name them “Crouch” and “Jump”
        /// //Apply these parameters to your transitions between states
        ///
        /// //This script allows you to trigger an Animator parameter and reset the other that could possibly still be active. Press the up and down arrow keys to do this.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///
        ///    void Start()
        ///    {
        ///        //Get the Animator attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the up arrow button to reset the trigger and set another one
        ///        if (Input.GetKey(KeyCode.UpArrow))
        ///        {
        ///            //Reset the "Crouch" trigger
        ///            m_Animator.ResetTrigger("Crouch");
        ///            //Send the message to the Animator to activate the trigger parameter named "Jump"
        ///            m_Animator.SetTrigger("Jump");
        ///        }
        ///
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///        {
        ///            //Reset the "Jump" trigger
        ///            m_Animator.ResetTrigger("Jump");
        ///            //Send the message to the Animator to activate the trigger parameter named "Crouch"
        ///            m_Animator.SetTrigger("Crouch");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void ResetTrigger(string name)       { ResetTriggerString(name); }

        // Resets the trigger parameter at off
        ///<summary>Resets the value of the given trigger parameter.</summary>
        ///<remarks>Use this to reset a Trigger [parameter](xref:AnimationParameters) in an Animator Controller that could still be active. Make sure to create a parameter in the Animator Controller with the same name. See <see cref="Animator.SetTrigger" /> for more information about how to set a Trigger.</remarks>
        ///<param name="id">The parameter ID.</param>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject with an Animator component attached.
        /// //For this example, create parameters in the Animator and name them “Crouch” and “Jump”
        /// //Apply these parameters to your transitions between states
        ///
        /// //This script allows you to trigger an Animator parameter and reset the other that could possibly still be active. Press the up and down arrow keys to do this.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///
        ///    void Start()
        ///    {
        ///        //Get the Animator attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the up arrow button to reset the trigger and set another one
        ///        if (Input.GetKey(KeyCode.UpArrow))
        ///        {
        ///            //Reset the "Crouch" trigger
        ///            m_Animator.ResetTrigger("Crouch");
        ///            //Send the message to the Animator to activate the trigger parameter named "Jump"
        ///            m_Animator.SetTrigger("Jump");
        ///        }
        ///
        ///        if (Input.GetKey(KeyCode.DownArrow))
        ///        {
        ///            //Reset the "Jump" trigger
        ///            m_Animator.ResetTrigger("Jump");
        ///            //Send the message to the Animator to activate the trigger parameter named "Crouch"
        ///            m_Animator.SetTrigger("Crouch");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void ResetTrigger(int id)       { ResetTriggerID(id); }

        // Returns true if a parameter is controlled by an additional curve on an animation
        ///<summary>Returns true if the parameter is controlled by a curve, false otherwise.</summary>
        ///<param name="name">The parameter name.</param>
        ///<returns>True if the parameter is controlled by a curve, false otherwise.</returns>
        public bool IsParameterControlledByCurve(string name)     { return IsParameterControlledByCurveString(name); }
        // Returns true if a parameter is controlled by an additional curve on an animation
        ///<summary>Returns true if the parameter is controlled by a curve, false otherwise.</summary>
        ///<param name="id">The parameter ID.</param>
        ///<returns>True if the parameter is controlled by a curve, false otherwise.</returns>
        public bool IsParameterControlledByCurve(int id)          { return IsParameterControlledByCurveID(id); }

        // Gets the avatar delta position for the last evaluated frame
        ///<summary>Gets the avatar delta position for the last evaluated frame.</summary>
        ///<remarks>
        ///  <see cref="Animator.applyRootMotion" /> must be enabled for deltaPosition to be calculated.</remarks>
        extern public Vector3 deltaPosition { get; }
        // Gets the avatar delta rotation for the last evaluated frame
        ///<summary>Gets the avatar delta rotation for the last evaluated frame.</summary>
        ///<remarks>
        ///  <see cref="Animator.applyRootMotion" /> must be enabled for deltaRotation to be calculated.</remarks>
        extern public Quaternion  deltaRotation { get; }

        // Gets the avatar velocity for the last evaluated frame
        ///<summary>Gets the avatar velocity  for the last evaluated frame.</summary>
        ///<remarks>
        ///  <see cref="Animator.applyRootMotion" /> must be enabled for velocity to be calculated.</remarks>
        extern public Vector3 velocity { get; }
        // Gets the avatar angular velocity for the last evaluated frame
        ///<summary>Gets the avatar angular velocity for the last evaluated frame.</summary>
        ///<remarks>
        ///  <see cref="Animator.applyRootMotion" /> must be enabled for angularVelocity to be calculated.</remarks>
        extern public Vector3 angularVelocity { get; }

        //  The root position, the position of the game object
        ///<summary>The root position, the position of the game object.</summary>
        ///<remarks>You should only set this value from within an <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK">OnAnimatorIK()</see> function call.</remarks>
        extern public Vector3 rootPosition
        {
            [NativeMethod("GetAvatarPosition")]
            get;
            [NativeMethod("SetAvatarPosition")]
            set;
        }
        //  The root rotation, the rotation of the game object
        ///<summary>The root rotation, the rotation of the game object.</summary>
        ///<remarks>You should only set this value from within an <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK">OnAnimatorIK()</see> function call.</remarks>
        extern public Quaternion rootRotation
        {
            [NativeMethod("GetAvatarRotation")]
            get;
            [NativeMethod("SetAvatarRotation")]
            set;
        }
        // Root is controlled by animations
        ///<summary>Should root motion be applied?</summary>
        ///<remarks>Root motion is the effect where an object's entire mesh moves away from its starting point but that motion is created by the animation itself rather than by changing the Transform position. Note that <c>applyRootMotion</c> has no effect when the script implements a <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorMove" /> function.
        ///
        ///Changing the value of applyRootMotion at runtime will re-initialize the animator.</remarks>
        extern public bool applyRootMotion
        {
            get;
            set;
        }

        // Linear velocity blending for root motion
        ///<summary>When linearVelocityBlending is set to true, the root motion velocity and angular velocity will be blended linearly.</summary>
        [Obsolete("Animator.linearVelocityBlending is no longer used and has been deprecated.")]
        extern public bool linearVelocityBlending
        {
            get;
            set;
        }

        // When turned on, animations will synchronize transforms with physics. This is only useful in conjunction with kinematic rigidbodies.
        ///<summary>When enabled, the physics system uses animated transforms from GameObjects with kinematic Rigidbody components to influence other GameObjects.</summary>
        ///<remarks>For example, enable animatePhysics to apply velocity and friction from an animated platform to GameObjects on the platform. For velocity and friction to be applied, the platform GameObject must have a kinematic Rigidbody. To make a Rigidbody kinematic, enable the Is Kinematic property in the Rigidbody component.</remarks>
        extern public bool animatePhysics
        {
            get;
            set;
        }

        ///<summary>Specifies the update mode of the <see cref="Animator" />.</summary>
        extern public AnimatorUpdateMode updateMode
        {
            get;
            set;
        }

        // Tell if the corresponding Character has transform hierarchy.
        ///<summary>Returns true if the object has a transform hierarchy.</summary>
        ///<remarks>This value is based on the Optimize GameObject property in the Model Importer.</remarks>
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
        ///<summary>The current gravity weight based on current animations that are played.</summary>
        extern public float gravityWeight
        {
            get;
        }


        // The position of the body center of mass
        ///<summary>The position of the body center of mass.</summary>
        ///<remarks>The position is in worldspace.  You should only set this value from within an <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK">OnAnimatorIK()</see> function call.</remarks>
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
        ///<summary>The rotation of the body center of mass.</summary>
        ///<remarks>The rotation is in worldspace. You should only set this value from within an <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK">OnAnimatorIK()</see> function call.</remarks>
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
        ///<summary>Gets the position of an IK goal.</summary>
        ///<remarks>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///This function gets the current position of the specified goal in world space.</remarks>
        ///<param name="goal">The AvatarIKGoal that is queried.</param>
        ///<returns>Return the current position of this IK goal in world space.</returns>
        ///<seealso cref="GetIKPositionWeight" />
        ///<seealso cref="SetIKPosition" />
        public Vector3 GetIKPosition(AvatarIKGoal goal) {  CheckIfInIKPass(); return GetGoalPosition(goal); }
        extern private Vector3 GetGoalPosition(AvatarIKGoal goal);

        // Sets the position of an IK goal
        ///<summary>Sets the position of an IK goal.</summary>
        ///<remarks>
        ///  <para>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///This function sets the position of the ultimate goal in world space; the actual point in space where the body part ends up is also influenced by a weight parameter that specifies how far between the start and the goal the IK should aim (a value in the range 0..1).
        ///
        ///This function should always be called in <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="goal">The AvatarIKGoal that is set.</param>
        ///<param name="goalPosition">The position in world space.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Transform objToPickUp;
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void OnAnimatorIK(int layerIndex)
        ///    {
        ///        float reach = animator.GetFloat("RightHandReach");
        ///        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, reach);
        ///        animator.SetIKPosition(AvatarIKGoal.RightHand, objToPickUp.position);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="SetIKPositionWeight" />
        ///<seealso cref="SetIKRotation" />
        public void SetIKPosition(AvatarIKGoal goal, Vector3 goalPosition) { CheckIfInIKPass(); SetGoalPosition(goal, goalPosition); }
        extern private void SetGoalPosition(AvatarIKGoal goal, Vector3 goalPosition);

        // Gets the rotation of an IK goal
        ///<summary>Gets the rotation of an IK goal.</summary>
        ///<remarks>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///This function gets the current rotation of the specified goal in world space.</remarks>
        ///<param name="goal">The AvatarIKGoal that is is queried.</param>
        ///<seealso cref="GetIKRotationWeight" />
        ///<seealso cref="SetIKRotation" />
        public Quaternion GetIKRotation(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalRotation(goal); }
        extern private Quaternion GetGoalRotation(AvatarIKGoal goal);

        // Sets the rotation of an IK goal
        ///<summary>Sets the rotation of an IK goal.</summary>
        ///<remarks>
        ///  <para>An IK goal is a specified target position and rotation for a specific body part. Unity calculates how to move the body part towards this target from a starting point. This starting point could be, for example, the current position and rotation obtained from an animation.
        ///
        ///This function sets the IK goal rotation in world space. When specifying the IK goal rotation, it should follow Unity's world coordinates convention:
        ///• The /X-Axis/ is parallel to the palm of the hand (or sole of the foot), pointing sideways to the right of the hand (or foot).
        ///
        ///• The /Y-Axis/ is perpendicular to the top of the hand (or foot), pointing upwards.
        ///
        ///• The /Z-Axis/ is parallel to the palm of the hand (or sole of the foot), pointing forwards toward the fingers (or toes).
        ///
        ///
        ///It is recommended that the bone orientation of the avatar skeleton pose should also follow Unity's world coordinates convention. If your avatar skeleton pose follows a different convention, the bone rotation applied to the corresponding <c>GameObject</c> might differ from the IK goal rotation.
        ///
        ///In addition, you can set a weight value to set the amount of influence that the IK goal rotation has over the starting rotation. Use the <c>SetIKRotationWeight</c> method to set a weight value between 0..1 where a weight of 0 means no influence and a weight of 1 means full influence.
        ///
        ///The following code example demonstrates how to use the <c>SetIKRotation</c> method and <c>SetIKRotationWeight</c> method.</para>
        ///  <para />
        ///</remarks>
        ///<param name="goal">The AvatarIKGoal that is set.</param>
        ///<param name="goalRotation">The rotation of the goal in world space which should follow Unity's world coordinates convention (see below).</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Transform objToAimAt;
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void OnAnimatorIK(int layerIndex)
        ///    {
        ///        Quaternion handRotation = Quaternion.LookRotation(objToAimAt.position - transform.position);
        ///        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
        ///        animator.SetIKRotation(AvatarIKGoal.RightHand, handRotation);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="SetIKRotationWeight" />
        ///<seealso cref="SetIKPosition" />
        public void SetIKRotation(AvatarIKGoal goal, Quaternion goalRotation) { CheckIfInIKPass();  SetGoalRotation(goal, goalRotation); }
        extern private void SetGoalRotation(AvatarIKGoal goal, Quaternion goalRotation);

        // Gets the translative weight of an IK goal (0 = at the original animation before IK, 1 = at the goal)
        ///<summary>Gets the translative weight of an IK goal (0 = at the original animation before IK, 1 = at the goal).</summary>
        ///<remarks>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///The point calculated by the IK is also influenced by a weight value in the range 0..1 that determines how far between the start and the goal to aim. This function returns the current weight value for the position of the goal.</remarks>
        ///<param name="goal">The AvatarIKGoal that is queried.</param>
        ///<seealso cref="GetIKPosition" />
        ///<seealso cref="SetIKPosition" />
        public float GetIKPositionWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalWeightPosition(goal); }
        extern private float GetGoalWeightPosition(AvatarIKGoal goal);

        // Sets the translative weight of an IK goal (0 = at the original animation before IK, 1 = at the goal)
        ///<summary>Sets the translative weight of an IK goal (0 = at the original animation before IK, 1 = at the goal).</summary>
        ///<remarks>
        ///  <para>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///This function sets a weight value in the range 0..1 to determine how far between the start and goal positions the IK will aim. The position itself is set separately using <see cref="SetIKPosition" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="goal">The AvatarIKGoal that is set.</param>
        ///<param name="value">The translative weight.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Transform objToPickUp;
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void OnAnimatorIK(int layerIndex)
        ///    {
        ///        // Retrieves the value of the parameter "RightHandReach" that must be created in the AnimatorController.
        ///        float reach = animator.GetFloat("RightHandReach");
        ///
        ///        // Sets IK Position and IK Position Weight.
        ///        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, reach);
        ///        animator.SetIKPosition(AvatarIKGoal.RightHand, objToPickUp.position);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="SetIKPosition" />
        ///<seealso cref="SetIKRotationWeight" />
        public void SetIKPositionWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetGoalWeightPosition(goal, value); }
        extern private void SetGoalWeightPosition(AvatarIKGoal goal, float value);

        // Gets the rotational weight of an IK goal (0 = rotation before IK, 1 = rotation at the IK goal)
        ///<summary>Gets the rotational weight of an IK goal (0 = rotation before IK, 1 = rotation at the IK goal).</summary>
        ///<remarks>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///The rotation calculated by the IK is also influenced by a weight value in the range 0..1 that determines how far between the start and the goal to aim. This function returns the current weight value for the rotation of the goal.</remarks>
        ///<param name="goal">The AvatarIKGoal that is queried.</param>
        ///<seealso cref="GetIKRotation" />
        ///<seealso cref="SetIKRotation" />
        public float GetIKRotationWeight(AvatarIKGoal goal) { CheckIfInIKPass(); return GetGoalWeightRotation(goal); }
        extern private float GetGoalWeightRotation(AvatarIKGoal goal);

        // Sets the rotational weight of an IK goal (0 = rotation before IK, 1 = rotation at the IK goal)
        ///<summary>Sets the rotational weight of an IK goal (0 = rotation before IK, 1 = rotation at the IK goal).</summary>
        ///<remarks>
        ///  <para>An IK goal is a target position and rotation for a specific body part. Unity can calculate how to move the part toward the target from the starting point (ie, the current position and rotation obtained from the animation).
        ///
        ///This function sets a weight value in the range 0..1 to determine how far between the start and goal rotations the IK will aim. The goal itself is set separately using <see cref="SetIKRotation" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="goal">The AvatarIKGoal that is set.</param>
        ///<param name="value">The rotational weight.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Transform objToAimAt;
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void OnAnimatorIK(int layerIndex)
        ///    {
        ///        Quaternion handRotation = Quaternion.LookRotation(objToAimAt.position - transform.position);
        ///        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
        ///        animator.SetIKRotation(AvatarIKGoal.RightHand, handRotation);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="SetIKRotation" />
        ///<seealso cref="SetIKPositionWeight" />
        public void SetIKRotationWeight(AvatarIKGoal goal, float value) { CheckIfInIKPass(); SetGoalWeightRotation(goal, value); }
        extern private void SetGoalWeightRotation(AvatarIKGoal goal, float value);

        // Gets the position of an IK hint
        ///<summary>Gets the position of an IK hint.</summary>
        ///<param name="hint">The AvatarIKHint that is queried.</param>
        ///<returns>Return the current position of this IK hint in world space.</returns>
        public Vector3 GetIKHintPosition(AvatarIKHint hint) {  CheckIfInIKPass(); return GetHintPosition(hint); }
        extern private Vector3 GetHintPosition(AvatarIKHint hint);

        // Sets the position of an IK hint
        ///<summary>Sets the position of an IK hint.</summary>
        ///<param name="hint">The AvatarIKHint that is set.</param>
        ///<param name="hintPosition">The position in world space.</param>
        public void SetIKHintPosition(AvatarIKHint hint, Vector3 hintPosition) { CheckIfInIKPass(); SetHintPosition(hint, hintPosition); }
        extern private void SetHintPosition(AvatarIKHint hint, Vector3 hintPosition);

        // Gets the translative weight of an IK hint (0 = at the original animation before IK, 1 = points toward the hint)
        ///<summary>Gets the translative weight of an IK Hint (0 = at the original animation before IK, 1 = at the hint).</summary>
        ///<param name="hint">The AvatarIKHint that is queried.</param>
        ///<returns>Return translative weight.</returns>
        public float GetIKHintPositionWeight(AvatarIKHint hint) { CheckIfInIKPass(); return GetHintWeightPosition(hint); }
        extern private float GetHintWeightPosition(AvatarIKHint hint);

        // Sets the translative weight of an IK hint (0 = at the original animation before IK, 1 = points toward the hint)
        ///<summary>Sets the translative weight of an IK hint (0 = at the original animation before IK, 1 = at the hint).</summary>
        ///<param name="hint">The AvatarIKHint that is set.</param>
        ///<param name="value">The translative weight.</param>
        public void SetIKHintPositionWeight(AvatarIKHint hint, float value) { CheckIfInIKPass(); SetHintWeightPosition(hint, value); }
        extern private void SetHintWeightPosition(AvatarIKHint hint, float value);

        // Sets the look at position
        ///<summary>Sets the look at position for a character during animations.</summary>
        ///<remarks>Use this method in conjunction with <see cref="Animator.SetLookAtWeight" /> to determine how strongly the character should look toward the specified position.
        ///
        ///You can only call <see cref="Animator.SetLookAtPosition" /> from the <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK">MonoBehaviour.OnAnimatorIK</see> or <see cref="StateMachineBehaviour.OnStateIK" /> callback. If called from a different context, this method has no effect and issues a warning.</remarks>
        ///<param name="lookAtPosition">The position in the world space to look at.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/SetLookAtPositionExample.cs}]]></code>
        ///</example>
        ///<seealso cref="Animator.SetLookAtWeight" />
        ///<seealso cref="M:UnityEngine.MonoBehaviour.OnAnimatorIK">MonoBehaviour.OnAnimatorIK</seealso>
        ///<seealso cref="StateMachineBehaviour.OnStateIK" />
        public void SetLookAtPosition(Vector3 lookAtPosition) { CheckIfInIKPass(); SetLookAtPositionInternal(lookAtPosition); }

        [NativeMethod("SetLookAtPosition")]
        extern private void SetLookAtPositionInternal(Vector3 lookAtPosition);

        ///<summary>Set look at weights.</summary>
        ///<param name="weight">(0-1) the global weight of the LookAt, multiplier for other parameters.</param>
        public void SetLookAtWeight(float weight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, 0.00f, 1.00f, 0.00f, 0.50f);
        }

        ///<summary>Set look at weights.</summary>
        ///<param name="weight">(0-1) the global weight of the LookAt, multiplier for other parameters.</param>
        ///<param name="bodyWeight">(0-1) determines how much the body is involved in the LookAt.</param>
        public void SetLookAtWeight(float weight, float bodyWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, 1.00f, 0.00f, 0.50f);
        }

        ///<summary>Set look at weights.</summary>
        ///<param name="weight">(0-1) the global weight of the LookAt, multiplier for other parameters.</param>
        ///<param name="bodyWeight">(0-1) determines how much the body is involved in the LookAt.</param>
        ///<param name="headWeight">(0-1) determines how much the head is involved in the LookAt.</param>
        public void SetLookAtWeight(float weight, float bodyWeight, float headWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, 0.00f, 0.50f);
        }

        ///<summary>Set look at weights.</summary>
        ///<param name="weight">(0-1) the global weight of the LookAt, multiplier for other parameters.</param>
        ///<param name="bodyWeight">(0-1) determines how much the body is involved in the LookAt.</param>
        ///<param name="headWeight">(0-1) determines how much the head is involved in the LookAt.</param>
        ///<param name="eyesWeight">(0-1) determines how much the eyes are involved in the LookAt.</param>
        public void SetLookAtWeight(float weight, float bodyWeight, float headWeight, float eyesWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, 0.50f);
        }

        ///<summary>Set look at weights.</summary>
        ///<param name="weight">(0-1) the global weight of the LookAt, multiplier for other parameters.</param>
        ///<param name="bodyWeight">(0-1) determines how much the body is involved in the LookAt.</param>
        ///<param name="headWeight">(0-1) determines how much the head is involved in the LookAt.</param>
        ///<param name="eyesWeight">(0-1) determines how much the eyes are involved in the LookAt.</param>
        ///<param name="clampWeight">(0-1) 0.0 means the character is unrestrained in motion. 1.0 means the character is clamped (look at becomes impossible). 0.5 means the character is able to move on half of the possible range (180 degrees).</param>
        public void SetLookAtWeight(float weight, [DefaultValue("0.0f")] float bodyWeight, [DefaultValue("1.0f")] float headWeight, [DefaultValue("0.0f")] float eyesWeight, [DefaultValue("0.5f")] float clampWeight)
        {
            CheckIfInIKPass();
            SetLookAtWeightInternal(weight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }

        [NativeMethod("SetLookAtWeight")]
        extern private void SetLookAtWeightInternal(float weight, float bodyWeight, float headWeight, float eyesWeight, float clampWeight);

        // Set Local Rotation of humanoid bone during IK pass
        ///<summary>Sets local rotation of a human bone during a IK pass.</summary>
        ///<remarks>Can be used to create rotation IK goals for any human bone. Ex: Control lower and upper body independantly by setting Hips and Spine local rotation during an IK pass.</remarks>
        ///<param name="humanBoneId">The human bone Id.</param>
        ///<param name="rotation">The local rotation.</param>
        public void SetBoneLocalRotation(HumanBodyBones humanBoneId, Quaternion rotation) { CheckIfInIKPass(); SetBoneLocalRotationInternal(HumanTrait.GetBoneIndexFromMono((int)humanBoneId), rotation); }

        [NativeMethod("SetBoneLocalRotation")]
        extern private void SetBoneLocalRotationInternal(int humanBoneId, Quaternion rotation);

        extern private ScriptableObject GetBehaviour([NotNull] Type type);

        ///<summary>Returns the first <see cref="StateMachineBehaviour" /> that matches type <c>T</c> or is derived from <c>T</c>. Returns null if none are found.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEditor;
        ///using UnityEngine;
        ///
        ///public class RunBehaviour : StateMachineBehaviour
        ///{
        ///    // OnStateUpdate is called at each Update frame between OnStateEnter and OnStateExit callback
        ///    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        ///    {
        ///        Transform transform = animator.GetComponent<Transform>();
        ///
        ///        RaycastHit hitInfo;
        ///        Vector3 dir = transform.TransformDirection(Vector3.forward);
        ///        if (Physics.Raycast(transform.position + new Vector3(0, 1.5f, 0), dir, out hitInfo, 10))
        ///        {
        ///            if (hitInfo.collider.tag == "Obstacle")
        ///            {
        ///                animator.GetBehaviour<SlideBehaviour>().target = transform.position + 1.25f * hitInfo.distance * dir;
        ///                if (hitInfo.distance < 6)
        ///                    animator.SetTrigger("Slide");
        ///            }
        ///        }
        ///    }
        ///}
        ///
        ///public class SlideBehaviour : StateMachineBehaviour
        ///{
        ///    public Vector3 target;
        ///
        ///    public float slideMatchTargetStart = 0.11f;
        ///    public float slideMatchTargetStop = 0.40f;
        ///
        ///    // OnStateUpdate is called at each Update frame between OnStateEnter and OnStateExit callback
        ///    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        ///    {
        ///        animator.MatchTarget(target, new Quaternion(), AvatarTarget.Root, new MatchTargetWeightMask(new Vector3(1, 0, 1), 0), slideMatchTargetStart, slideMatchTargetStop);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public T GetBehaviour<T>() where T : StateMachineBehaviour { return GetBehaviour(typeof(T)) as T; }

        private static T[] ConvertStateMachineBehaviour<T>(ScriptableObject[] rawObjects) where T : StateMachineBehaviour
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        ///<summary>Returns all <see cref="StateMachineBehaviour" /> that match type <c>T</c> or are derived from <c>T</c>. Returns null if none are found.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // An example StateMachineBehaviour.
        ///public class BreathBehaviour : StateMachineBehaviour
        ///{
        ///    public bool  fastBreath;
        ///
        ///    // OnStateUpdate is called at each Update frame between OnStateEnter and OnStateExit callback
        ///    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        ///    {
        ///        animator.SetBool("FastBreath", fastBreath);
        ///    }
        ///}
        ///
        ///
        ///public class RunBehaviour : StateMachineBehaviour
        ///{
        ///    // OnStateUpdate is called at each Update frame between OnStateEnter and OnStateExit callback
        ///    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        ///    {
        ///        BreathBehaviour[] breathBehaviours = animator.GetBehaviours<BreathBehaviour>();
        ///        for (int i = 0; i < breathBehaviours.Length; i++)
        ///            breathBehaviours[i].fastBreath = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public T[] GetBehaviours<T>() where T : StateMachineBehaviour
        {
            return ConvertStateMachineBehaviour<T>(InternalGetBehaviours(typeof(T)));
        }

        [FreeFunction(Name = "AnimatorBindings::InternalGetBehaviours", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal ScriptableObject[] InternalGetBehaviours([NotNull] Type type);

        public StateMachineBehaviour[] GetBehaviours(int fullPathHash, int layerIndex)
        {
            return InternalGetBehavioursByKey(fullPathHash, layerIndex, typeof(StateMachineBehaviour)) as StateMachineBehaviour[];
        }

        [FreeFunction(Name = "AnimatorBindings::InternalGetBehavioursByKey", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern internal ScriptableObject[] InternalGetBehavioursByKey(int fullPathHash, int layerIndex, [NotNull] Type type);

        // Automatic stabilization of feet during transition and blending
        ///<summary>Automatic stabilization of feet during transition and blending.</summary>
        extern public bool stabilizeFeet
        {
            get;
            set;
        }

        // The AnimatorController layer count
        ///<summary>Returns the number of layers in the controller.</summary>
        extern public int layerCount
        {
            get;
        }

        // Gets name of the layer
        ///<summary>Returns the layer name.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The layer name.</returns>
        extern public string GetLayerName(int layerIndex);
        ///<summary>Returns the index of the animation layer with the given name.</summary>
        ///<remarks>
        ///  <para>You can use <see cref="Animator.GetLayerName" /> to retrieve the name of an animation layer using its index.</para>
        ///  <para />
        ///</remarks>
        ///<param name="layerName">The name of the animation layer to seek.</param>
        ///<returns>The index of the specified layer. Returns -1 if the layer name is not found.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/GetLayerIndexExample.cs}]]></code>
        ///</example>
        ///<seealso href="xref:AnimationLayers" />
        extern public int GetLayerIndex(string layerName);
        // Gets the layer's current weight
        ///<summary>Returns the weight of the layer at the specified index.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The layer weight.</returns>
        extern public float GetLayerWeight(int layerIndex);
        // Sets the layer's current weight
        ///<summary>Changes the weight of the layer at a specific index.</summary>
        ///<remarks>If the specified layer does not exist, this method does nothing.</remarks>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="weight">The new layer weight which is a value between 0 and 1.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorSetLayerWeightExamples.cs}]]></code>
        ///</example>
        extern public void SetLayerWeight(int layerIndex, float weight);

        extern private void GetAnimatorStateInfo(int layerIndex, StateInfoIndex stateInfoIndex, out AnimatorStateInfo info);

        // Gets the current State information on a specified AnimatorController layer
        ///<summary>Returns an <see cref="AnimatorStateInfo" /> with the information on the current state.</summary>
        ///<remarks>Fetches the data from the current state in the Animator. Use this to get details from the state, including accessing the state’s speed, length, name and other variables. For gathering information from the clips that the states hold, see <see cref="Animator.GetCurrentAnimatorClipInfo" />.</remarks>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An <see cref="AnimatorStateInfo" /> with the information on the current state.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// //Create a GameObject and attach an Animator component (Click the __Add Component__ button in the Inspector window, go to __Miscellaneous__>__Animator__).
        /// //Create an Animator by going to __Assets__ >  __Create__ > __Animator Controller__. Attach this Controller to the Animator attached to your GameObject
        /// //In the Animator Controller, create a Trigger parameter in the __Parameters__ tab and name it “Jump”. Then create states and transition arrows that use this parameter.
        ///
        /// //This script triggers an Animation parameter when you press the space key.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    //Use to output current speed of the state to the screen
        ///    float m_CurrentSpeed;
        ///
        ///    void Start()
        ///    {
        ///        //Get the Animator, which you attach to the GameObject you intend to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///        //The current speed of the first Animator state
        ///        m_CurrentSpeed = m_Animator.GetCurrentAnimatorStateInfo(0).speed;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press the space bar to tell the Animator to trigger the Jump Animation
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            m_Animator.SetTrigger("Jump");
        ///        }
        ///
        ///        //When entering the Jump state in the Animator, output the message in the console
        ///        if (m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
        ///        {
        ///            Debug.Log("Jumping");
        ///        }
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        //Output the first Animation speed to the screen
        ///        GUI.Label(new Rect(25, 25, 200, 20),  "Speed of State : " + m_CurrentSpeed);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            AnimatorStateInfo info;
            GetAnimatorStateInfo(layerIndex, StateInfoIndex.CurrentState, out info);
            return info;
        }

        // Gets the next State information on a specified AnimatorController layer
        ///<summary>Returns an <see cref="AnimatorStateInfo" /> with the information on the next state.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An <see cref="AnimatorStateInfo" /> with the information on the next state.</returns>
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex)
        {
            AnimatorStateInfo info;
            GetAnimatorStateInfo(layerIndex, StateInfoIndex.NextState, out info);
            return info;
        }

        extern private void GetAnimatorTransitionInfo(int layerIndex, out AnimatorTransitionInfo info);

        // Gets the Transition information on a specified AnimatorController layer
        ///<summary>Returns an <see cref="AnimatorTransitionInfo" /> with the informations on the current transition.</summary>
        ///<param name="layerIndex">The layer's index.</param>
        ///<returns>An <see cref="AnimatorTransitionInfo" /> with the informations on the current transition.</returns>
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex)
        {
            AnimatorTransitionInfo  info;
            GetAnimatorTransitionInfo(layerIndex, out info);
            return info;
        }

        extern internal int GetAnimatorClipInfoCount(int layerIndex, bool current);

        // Gets the number of AnimatorClipInfo currently played by the current state
        ///<summary>Returns the number of <see cref="AnimatorClipInfo" /> in the current state.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The number of <see cref="AnimatorClipInfo" /> in the current state.</returns>
        public int GetCurrentAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCount(layerIndex, true);
        }

        // Gets the number of AnimatorClipInfo currently played by the next state
        ///<summary>Returns the number of <see cref="AnimatorClipInfo" /> in the next state.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>The number of <see cref="AnimatorClipInfo" /> in the next state.</returns>
        public int GetNextAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCount(layerIndex, false);
        }

        ///<summary>Returns an array of all the <see cref="AnimatorClipInfo" /> in the current state of the given layer.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An array of all the <see cref="AnimatorClipInfo" /> in the current state.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// //This script outputs the name and length of the Animation clip played at start-up.
        ///
        ///using UnityEngine;
        ///
        ///public class GetCurrentAnimatorClipInfoExample : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    string m_ClipName;
        ///    AnimatorClipInfo[] m_CurrentClipInfo;
        ///
        ///    float m_CurrentClipLength;
        ///
        ///    void Start()
        ///    {
        ///        //Get them_Animator, which you attach to the GameObject you intend to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///        //Fetch the current Animation clip information for the base layer
        ///        m_CurrentClipInfo = this.m_Animator.GetCurrentAnimatorClipInfo(0);
        ///        //Access the current length of the clip
        ///        m_CurrentClipLength = m_CurrentClipInfo[0].clip.length;
        ///        //Access the Animation clip name
        ///        m_ClipName = m_CurrentClipInfo[0].clip.name;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        //Output the current Animation name and length to the screen
        ///        GUI.Label(new Rect(0, 0, 200, 20),  "Clip Name : " + m_ClipName);
        ///        GUI.Label(new Rect(0, 30, 200, 20),  "Clip Length : " + m_CurrentClipLength);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction(Name = "AnimatorBindings::GetCurrentAnimatorClipInfo", HasExplicitThis = true)]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex);

        ///<summary>Returns an array of all the <see cref="AnimatorClipInfo" /> in the next state of the given layer.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>An array of all the <see cref="AnimatorClipInfo" /> in the next state.</returns>
        [FreeFunction(Name = "AnimatorBindings::GetNextAnimatorClipInfo", HasExplicitThis = true)]
        [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex);

        // Gets the list of AnimatorClipInfo currently played by the current state
        ///<summary>Fills <c>clips</c> with the list of all the <see cref="AnimatorClipInfo" /> in the current state of the given layer.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="clips">The list of <see cref="AnimatorClipInfo" /> to fill.</param>
        ///<seealso cref="GetCurrentAnimatorClipInfoCount" />
        public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, true, clips);
        }

        [FreeFunction(Name = "AnimatorBindings::GetAnimatorClipInfoInternal", HasExplicitThis = true)]
        extern private void GetAnimatorClipInfoInternal(int layerIndex, bool isCurrent, [Out,NotNull] List<AnimatorClipInfo> clips);

        // Gets the list of AnimatorClipInfo currently played by the next state
        ///<summary>Fills <c>clips</c> with the list of all the <see cref="AnimatorClipInfo" /> in the next state of the given layer.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="clips">The list of <see cref="AnimatorClipInfo" /> to fill.</param>
        ///<seealso cref="GetNextAnimatorClipInfoCount" />
        public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(layerIndex, false, clips);
        }

        // Is the specified AnimatorController layer in a transition
        ///<summary>Returns true if there is a transition on the given layer, false otherwise.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<returns>True if there is a transition on the given layer, false otherwise.</returns>
        extern public bool IsInTransition(int layerIndex);

        ///<summary>The <see cref="AnimatorControllerParameter" /> list used by the animator. (RO)</summary>
        ///<remarks>In Play mode, the list comes from the first playable controller. Otherwise, the list comes from the AnimatorController associated with this animator.</remarks>
        public extern AnimatorControllerParameter[] parameters
        {
            [FreeFunction(Name = "AnimatorBindings::GetParameters", HasExplicitThis = true)]
            get;
        }

        ///<summary>Returns the number of parameters in the controller.</summary>
        public extern int parameterCount
        {
            get;
        }

        [FreeFunction(Name = "AnimatorBindings::GetParameterInternal", HasExplicitThis = true)]
        private extern AnimatorControllerParameter GetParameterInternal(int index);

        ///<summary>Obtains a reference to the AnimatorControllerParameter at the given index.</summary>
        ///<remarks>
        ///  <para>Throws an <c>IndexOutOfRangeException</c> when the index is not in the range from greater than or equal to 0 to less than <see cref="Animator.parameterCount" />.</para>
        ///  <para />
        ///</remarks>
        ///<param name="index">The index of the parameter to obtain.</param>
        ///<returns>The parameter at the given index.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/GetParameterExample.cs}]]></code>
        ///</example>
        ///<seealso href="xref:AnimationParameters" />
        ///<seealso cref="P:UnityEditor.Animations.AnimatorController.parameters" />
        public AnimatorControllerParameter GetParameter(int index)
        {
            var parameter = GetParameterInternal(index);
            if ((int)parameter.m_Type == AnimatorControllerParameterTypeConstants.InvalidType)
                throw new IndexOutOfRangeException("Index must be between 0 and " + parameterCount);
            return parameter;
        }

        // Blends pivot point between body center of mass and feet pivot. At 0%, the blending point is body center of mass. At 100%, the blending point is feet pivot
        ///<summary>Blends pivot point between body center of mass and feet pivot.</summary>
        ///<remarks>At 0%, the blending point is body center of mass. At 100%, the blending point is feet pivot.</remarks>
        extern public float feetPivotActive
        {
            get;
            set;
        }

        // Gets the pivot weight
        ///<summary>Gets the pivot weight.</summary>
        ///<remarks>The pivot is the most stable point between the left and right foot of the avatar.
        ///For a value of 0, the left foot is the most stable point.
        ///For a value of 1, the right foot is the most stable point.</remarks>
        extern public float pivotWeight
        {
            get;
        }

        // Get the current position of the pivot
        ///<summary>Get the current position of the pivot.</summary>
        ///<remarks>The pivot is the most stable point between the left and right foot of the avatar.</remarks>
        extern public Vector3 pivotPosition
        {
            get;
        }

        extern private void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, int targetBodyPart, MatchTargetWeightMask weightMask, float startNormalizedTime, float targetNormalizedTime, bool completeMatch);

        // Automatically adjust the gameobject position and rotation so that the AvatarTarget reaches the matchPosition when the current state is at the specified progress
        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, 1, true);
        }

        ///<summary>Automatically adjust the <c>GameObject</c> position and rotation.</summary>
        ///<remarks>Adjust the <c>GameObject</c> position and rotation so that the AvatarTarget reaches the matchPosition when the current state is at the specified progress. Target matching only works on the base layer (index 0).
        ///You can only queue one match target at a time and you must wait for the first one to finish, otherwise your target matching will be discarded.
        ///If you call a <see cref="MatchTarget" /> with a start time lower than the clip's normalized time and the clip can loop, <see cref="MatchTarget" /> will adjust the time to match the next clip loop. For example, start time= 0.2 normalized time = 0.3, start time will be 1.2.  <see cref="Animator.applyRootMotion" /> must be enabled for MatchTarget to take effect.</remarks>
        ///<param name="matchPosition">The position we want the body part to reach.</param>
        ///<param name="matchRotation">The rotation in which we want the body part to be.</param>
        ///<param name="targetBodyPart">The body part that is involved in the match.</param>
        ///<param name="weightMask">Structure that contains weights for matching position and rotation.</param>
        ///<param name="startNormalizedTime">Start time within the animation clip (0 - beginning of clip, 1 - end of clip).</param>
        ///<param name="targetNormalizedTime">End time within the animation clip (0 - beginning of clip, 1 - end of clip), values greater than 1 can be set to trigger a match after a certain number of loops. Ex: 2.3 means at 30% of 2nd loop.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class TargetMatchingManager : MonoBehaviour
        ///{
        ///    public void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime)
        ///    {
        ///        var animator = GetComponent<Animator>();
        ///
        ///        if (animator.isMatchingTarget)
        ///            return;
        ///
        ///        float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
        ///
        ///        if (normalizeTime > normalisedEndTime)
        ///            return;
        ///
        ///        animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime, [DefaultValue("1")] float targetNormalizedTime)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, targetNormalizedTime, true);
        }

        public void MatchTarget(Vector3 matchPosition,  Quaternion matchRotation, AvatarTarget targetBodyPart,  MatchTargetWeightMask weightMask, float startNormalizedTime, [DefaultValue("1")] float targetNormalizedTime, [DefaultValue("true")] bool completeMatch)
        {
            MatchTarget(matchPosition, matchRotation, (int)targetBodyPart, weightMask, startNormalizedTime, targetNormalizedTime, completeMatch);
        }

        // Interrupts the automatic target matching
        ///<summary>Interrupts the automatic target matching.</summary>
        ///<remarks>CompleteMatch will make the gameobject match the target completely at the next frame.</remarks>
        public void InterruptMatchTarget()
        {
            InterruptMatchTarget(true);
        }

        ///<summary>Interrupts the automatic target matching.</summary>
        ///<remarks>CompleteMatch will make the gameobject match the target completely at the next frame.</remarks>
        extern public void InterruptMatchTarget([DefaultValue("true")] bool completeMatch);


        // If automatic matching is active
        ///<summary>If automatic matching is active.</summary>
        extern public bool isMatchingTarget
        {
            [NativeMethod("IsMatchingTarget")]
            get;
        }

        // The playback speed of the Animator. 1 is normal playback speed
        ///<summary>The playback speed of the Animator. 1 is normal playback speed.</summary>
        ///<remarks>Use <see cref="Animator.speed" /> to manipulate the playback speed of the Animator. Any animations currently being played by the Animator are slowed down or sped up depending on how the speed is altered. Set speed to 1 for normal playback. Negative playback speed is only supported when the recorder is enabled. For more details refer to <see cref="Animator.recorderMode" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator m_Animator;
        ///    //Value from the slider, and it converts to speed level
        ///    float m_MySliderValue;
        ///
        ///    void Start()
        ///    {
        ///        //Get the animator, attached to the GameObject you are intending to animate.
        ///        m_Animator = gameObject.GetComponent<Animator>();
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        //Create a Label in Game view for the Slider
        ///        GUI.Label(new Rect(0, 25, 40, 60), "Speed");
        ///        //Create a horizontal Slider to control the speed of the Animator. Drag the slider to 1 for normal speed.
        ///
        ///        m_MySliderValue = GUI.HorizontalSlider(new Rect(45, 25, 200, 60), m_MySliderValue, 0.0F, 1.0F);
        ///        //Make the speed of the Animator match the Slider value
        ///        m_Animator.speed = m_MySliderValue;
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Creates a crossfade from the current state to any other state using times in seconds.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.</remarks>
        ///<param name="stateName">The name of the state.</param>
        ///<param name="fixedTransitionDuration">The duration of the transition (in seconds).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="fixedTimeOffset">The time of the state (in seconds).</param>
        ///<param name="normalizedTransitionTime">The time of the transition (normalized).</param>
        ///<seealso cref="Animator.CrossFade" />
        ///<seealso cref="Animator.StringToHash" />
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

        ///<summary>Creates a crossfade from the current state to any other state using times in seconds.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.</remarks>
        ///<param name="stateHashName">The hash name of the state.</param>
        ///<param name="fixedTransitionDuration">The duration of the transition (in seconds).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="fixedTimeOffset">The time of the state (in seconds).</param>
        ///<param name="normalizedTransitionTime">The time of the transition (normalized).</param>
        ///<seealso cref="Animator.CrossFade" />
        ///<seealso cref="Animator.StringToHash" />
        [FreeFunction(Name = "AnimatorBindings::CrossFadeInFixedTime", HasExplicitThis = true)]
        extern public void CrossFadeInFixedTime(int stateHashName, float fixedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("0.0f")]  float fixedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime);

        ///<summary>Forces a write of the default values stored in the animator.</summary>
        [FreeFunction(Name = "AnimatorBindings::WriteDefaultValues", HasExplicitThis = true)]
        extern public void WriteDefaultValues();

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

        ///<summary>Creates a crossfade from the current state to any other state using normalized times.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.</remarks>
        ///<param name="stateName">The name of the state.</param>
        ///<param name="normalizedTransitionDuration">The duration of the transition (normalized).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="normalizedTimeOffset">The time of the state (normalized).</param>
        ///<param name="normalizedTransitionTime">The time of the transition (normalized).</param>
        ///<seealso cref="Animator.CrossFadeInFixedTime" />
        ///<seealso cref="Animator.StringToHash" />
        public void CrossFade(string stateName, float normalizedTransitionDuration, [DefaultValue("-1")]  int layer , [DefaultValue("float.NegativeInfinity")]  float normalizedTimeOffset , [DefaultValue("0.0f")]  float normalizedTransitionTime)
        {
            CrossFade(StringToHash(stateName), normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);
        }

        ///<summary>Creates a crossfade from the current state to any other state using normalized times.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.</remarks>
        ///<param name="stateHashName">The hash name of the state.</param>
        ///<param name="normalizedTransitionDuration">The duration of the transition (normalized).</param>
        ///<param name="layer">The layer where the crossfade occurs.</param>
        ///<param name="normalizedTimeOffset">The time of the state (normalized).</param>
        ///<param name="normalizedTransitionTime">The time of the transition (normalized).</param>
        ///<seealso cref="Animator.CrossFadeInFixedTime" />
        ///<seealso cref="Animator.StringToHash" />
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

        ///<summary>Plays a state.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.</remarks>
        ///<param name="stateName">The state name.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="fixedTime">The time offset (in seconds).</param>
        ///<seealso cref="Animator.StringToHash" />
        public void PlayInFixedTime(string stateName, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTime(StringToHash(stateName), layer, fixedTime);
        }

        ///<summary>Plays a state.</summary>
        ///<remarks>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Run</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Run</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.</remarks>
        ///<param name="stateNameHash">The state hash name. If stateNameHash is 0, it changes the current state time.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="fixedTime">The time offset (in seconds).</param>
        ///<seealso cref="Animator.StringToHash" />
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

        ///<summary>Plays a state.</summary>
        ///<remarks>
        ///  <para>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Bounce</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Bounce</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.
        ///
        ///The <c>normalizedTime</c> parameter varies between 0 and 1.  If this parameter is left at zero then <see cref="Play" /> will operate as expected.  A different starting point can be given.  An example could be <c>normalizedTime</c> set to 0.5, which means the animation starts halfway through.  If the transition from one state switches to another, it may or may not be blended.  If the transition starts at 0.75 it will be blended with the other state.  If no transition is set up then <see cref="Play" /> will continue to 1.0 with no changes.
        ///
        ///<img src="AnimatorPlay.png" />
        ///
        ///The following example script animates a cube.
        ///
        ///This cube has two Animator states called <c>Rest</c> and <c>Bounce</c>.  An empty animation is played in the <c>Rest</c> state.  When the Space key is pressed the cube switches into the <c>Bounce</c> state.  This causes the cube to jump up and down twice.  The cube then returns to the <c>Rest</c> state.  Because <c>Bounce</c> is selected from the <see cref="Animator.Play" /> script, no Transition is needed. However the return from <c>Bounce</c> to <c>Rest</c> does have a Transition. <c>Has Exit Time</c> is ticked to make <c>Bounce</c> last for its one second. Attach this script to the GameObject you want to animate.</para>
        ///  <para />
        ///</remarks>
        ///<param name="stateName">The state name.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="normalizedTime">The time offset between zero and one.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorPlayExample.cs}]]></code>
        ///</example>
        ///<seealso cref="Animator.StringToHash" />
        public void Play(string stateName, [DefaultValue("-1")]  int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            Play(StringToHash(stateName), layer, normalizedTime);
        }

        ///<summary>Plays a state.</summary>
        ///<remarks>
        ///  <para>When you specify a state name, or the string used to generate a hash, it should include the name of the parent layer. For example, if you have a <c>Bounce</c> state in the <c>Base Layer</c>, the name is <c>Base Layer.Bounce</c>.
        ///When you use the <c>stateName</c> parameter, this method calls <see cref="Animator.StringToHash" /> internally. If you use this method with the same <c>stateName</c> often, precompute the hash and use the <c>stateHashName</c> parameter to improve performance.
        ///
        ///The <c>normalizedTime</c> parameter varies between 0 and 1.  If this parameter is left at zero then <see cref="Play" /> will operate as expected.  A different starting point can be given.  An example could be <c>normalizedTime</c> set to 0.5, which means the animation starts halfway through.  If the transition from one state switches to another, it may or may not be blended.  If the transition starts at 0.75 it will be blended with the other state.  If no transition is set up then <see cref="Play" /> will continue to 1.0 with no changes.
        ///
        ///<img src="AnimatorPlay.png" />
        ///
        ///The following example script animates a cube.
        ///
        ///This cube has two Animator states called <c>Rest</c> and <c>Bounce</c>.  An empty animation is played in the <c>Rest</c> state.  When the Space key is pressed the cube switches into the <c>Bounce</c> state.  This causes the cube to jump up and down twice.  The cube then returns to the <c>Rest</c> state.  Because <c>Bounce</c> is selected from the <see cref="Animator.Play" /> script, no Transition is needed. However the return from <c>Bounce</c> to <c>Rest</c> does have a Transition. <c>Has Exit Time</c> is ticked to make <c>Bounce</c> last for its one second. Attach this script to the GameObject you want to animate.</para>
        ///  <para />
        ///</remarks>
        ///<param name="stateNameHash">The state hash name. If stateNameHash is 0, it changes the current state time.</param>
        ///<param name="layer">The layer index. If layer is -1, it plays the first state with the given state name or hash.</param>
        ///<param name="normalizedTime">The time offset between zero and one.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorPlayExample.cs}]]></code>
        ///</example>
        ///<seealso cref="Animator.StringToHash" />
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

        ///<summary>Resets the AnimatorController to its default state.</summary>
        ///<remarks>Use this method to reset the layers in the <see cref="T:UnityEditor.Animations.AnimatorController" /> to their default state.</remarks>
        ///<param name="resetParameters">Set to true to also reset the controller parameters to their default values. When set to false, only the controller state is reset.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorResetExample.cs}]]></code>
        ///</example>
        extern public void ResetControllerState([DefaultValue("true")] bool resetParameters = true);

        // Sets an AvatarTarget and a targetNormalizedTime for the current state
        ///<summary>Sets an AvatarTarget and a targetNormalizedTime for the current state.</summary>
        ///<remarks>Once the frame is evaluated, use <see cref="Animator.targetPosition" /> and <see cref="Animator.targetRotation" /> to query the position and rotation.</remarks>
        ///<param name="targetIndex">The avatar body part that is queried.</param>
        ///<param name="targetNormalizedTime">The current state Time that is queried.</param>
        extern public void SetTarget(AvatarTarget targetIndex, float targetNormalizedTime);

        //  Returns the position of the target specified by SetTarget(AvatarTarget targetIndex, float targetNormalizedTime))
        ///<summary>Returns the position of the target specified by <see cref="SetTarget" />.</summary>
        ///<remarks>The position is only valid when a frame is being evaluated after the <see cref="SetTarget" /> call. <see cref="Animator.applyRootMotion" /> must be enabled for targetPosition to be calculated.</remarks>
        extern public Vector3 targetPosition
        {
            get;
        }
        //  Returns the rotation of the target specified by SetTarget(AvatarTarget targetIndex, float targetNormalizedTime))
        ///<summary>Returns the rotation of the target specified by <see cref="SetTarget" />.</summary>
        ///<remarks>The rotation is only valid when a frame is being evaluated after the <see cref="SetTarget" /> call. <see cref="Animator.applyRootMotion" /> must be enabled for targetRotation to be calculated.</remarks>
        extern public Quaternion targetRotation
        {
            get;
        }

        ///<summary>Returns true if the transform is controlled by the Animator\.</summary>
        ///<param name="transform">The transform that is queried.</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use mask and layers to control subset of transfroms in a skeleton.", true)]
        public bool IsControlled(Transform transform) {return false; }

        // Returns ture if a transform a bone controlled by human
        extern internal bool IsBoneTransform(Transform transform);

        ///<summary>Returns the Avatar root Transform.</summary>
        ///<remarks>The Avatar root Transform specifies where the Avatar hierarchy is nested in the Animator hierarchy.</remarks>
        extern public Transform avatarRoot
        {
            get;
        }

        ///<summary>Retrieves the <see cref="Transform" /> mapped to a human bone based on its id.</summary>
        ///<remarks>Throws **InvalidOperationException** when Animator.avatar is null.
        ///
        ///Throws **InvalidOperationException** when Animator.avatar is not a valid avatar.
        ///
        ///Throws **InvalidOperationException** when Animator.avatar is not a Humanoid avatar.
        ///
        ///Throws **IndexOutOfRangeException** when humanBoneId is not between 0 and HumanBodyBones.LastBone.</remarks>
        ///<param name="humanBoneId">The human bone to be queried. See the <see cref="HumanBodyBones" /> enum for a list of possible values.</param>
        ///<returns>Returns the <see cref="Transform" /> mapped to the human bone. Returns null if the human bone has no <see cref="Transform" />.</returns>
        public Transform GetBoneTransform(HumanBodyBones humanBoneId)
        {
            if (avatar == null)
                throw new InvalidOperationException("Avatar is null.");

            if (!avatar.isValid)
                throw new InvalidOperationException("Avatar is not valid.");

            if (!avatar.isHuman)
                throw new InvalidOperationException("Avatar is not of type humanoid.");

            if (humanBoneId < 0 || humanBoneId >= HumanBodyBones.LastBone)
                throw new IndexOutOfRangeException("humanBoneId must be between 0 and " + HumanBodyBones.LastBone);

            return GetBoneTransformInternal(HumanTrait.GetBoneIndexFromMono((int)humanBoneId));
        }

        [NativeMethod("GetBoneTransform")]
        extern internal Transform GetBoneTransformInternal(int humanBoneId);

        // Controls culling of this Animator component.
        ///<summary>Controls culling of this Animator component.</summary>
        ///<seealso cref="AnimatorCullingMode" />
        extern public AnimatorCullingMode cullingMode
        {
            get;
            set;
        }

        // Sets the animator in playback mode
        ///<summary>Sets the animator in playback mode.</summary>
        ///<remarks>In playback mode, you control the animator by setting a time value. The animator is not updated from game logic. Use <see cref="playbackTime" /> to explicitly manipulate the progress of time.</remarks>
        extern public void StartPlayback();

        // Stops animator playback mode
        ///<summary>Stops the animator playback mode. When playback stops, the avatar resumes getting control from game logic.</summary>
        ///<seealso cref="StartRecording" />
        ///<seealso cref="StopRecording" />
        ///<seealso cref="recorderStartTime" />
        ///<seealso cref="recorderStopTime" />
        ///<seealso cref="StartPlayback" />
        ///<seealso cref="playbackTime" />
        extern public void StopPlayback();

        // Plays recorded data
        ///<summary>Sets the playback position in the recording buffer.</summary>
        ///<remarks>When in playback mode (see <see cref="StartPlayback" />), this value is used for controlling the current playback position in the buffer (in seconds). The value can range between <see cref="recorderStartTime" /> and <see cref="recorderStopTime" /></remarks>
        ///<seealso cref="StartPlayback" />
        ///<seealso cref="StopPlayback" />
        extern public float playbackTime
        {
            get;
            set;
        }

        // Sets the animator in record mode
        ///<summary>Sets the animator in recording mode, and allocates a circular buffer of size frameCount.</summary>
        ///<remarks>After this call, the recorder starts collecting up to frameCount frames in the buffer. Note it is not possible to start playback until a call to <see cref="StopRecording" /> is made.</remarks>
        ///<param name="frameCount">The number of frames (updates) that will be recorded. If frameCount is 0, the recording will continue until the user calls <see cref="StopRecording" />. The maximum value for frameCount is 10000.</param>
        ///<seealso cref="StopRecording" />
        ///<seealso cref="recorderStartTime" />
        ///<seealso cref="recorderStopTime" />
        ///<seealso cref="StartPlayback" />
        ///<seealso cref="StopPlayback" />
        ///<seealso cref="playbackTime" />
        extern public void StartRecording(int frameCount);

        // Stops animator record mode
        ///<summary>Stops animator record mode.</summary>
        ///<remarks>A call to <see cref="StopRecording" /> will lock the recording buffer's contents in its current state. The data get saved for subsequent playback with <see cref="StartPlayback" />.</remarks>
        ///<seealso cref="StartRecording" />
        ///<seealso cref="recorderStartTime" />
        ///<seealso cref="recorderStopTime" />
        ///<seealso cref="StartPlayback" />
        ///<seealso cref="StopPlayback" />
        ///<seealso cref="playbackTime" />
        extern public void StopRecording();

        // The time at which the recording data starts
        ///<summary>Start time of the first frame of the buffer relative to the frame at which <see cref="StartRecording" /> was called.</summary>
        ///<remarks>For example, if we started recording at frame 10, and the buffer is 5 frames long.
        ///If the buffer is not initialized (<see cref="StartRecording" /> is not called), the value of this property will be -1.</remarks>
        ///<seealso cref="recorderStopTime" />
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
        ///<summary>End time of the recorded clip relative to when <see cref="StartRecording" /> was called.</summary>
        ///<remarks>For example, if we started recording at second 10, and ended recording at second 15, then this will have a value of 5.
        ///If the buffer is not initialized (<see cref="StartRecording" /> is not called), the value of this property will be -1.</remarks>
        ///<seealso cref="recorderStartTime" />
        public float recorderStopTime
        {
            get { return GetRecorderStopTime(); }
            // Obsolete is not supported right now on property get/set
            // @jonh to avoid a breaking API change we simply left an empty set for now
            //[Obsolete("Animator.recorderStopTime cannot be set. You need to use Animator.StopRecording() instead.", true)]
            set {}
        }

        extern private float GetRecorderStopTime();

        ///<summary>Gets the mode of the Animator recorder.</summary>
        extern public AnimatorRecorderMode recorderMode
        {
            get;
        }

        // The runtime representation of AnimatorController that controls the Animator
        ///<summary>The runtime representation of AnimatorController that controls the Animator.</summary>
        ///<remarks>Swapping <see cref="P:UnityEngine.Animator.runtimeAnimatorController" /> with an <see cref="AnimatorOverrideController" /> based on the same <see cref="T:UnityEditor.Animations.AnimatorController" /> at runtime doesn't reset state machine's current state.</remarks>
        extern public RuntimeAnimatorController runtimeAnimatorController
        {
            get;
            set;
        }

        // Returns true if Animator has any playables assigned to it.
        ///<summary>Returns true if Animator has any playables assigned to it.</summary>
        extern public bool hasBoundPlayables
        {
            [NativeMethod("HasBoundPlayables")]
            get;
        }

        extern internal void ClearInternalControllerPlayable();

        ///<summary>Returns true if the state exists in this layer, false otherwise.</summary>
        ///<param name="layerIndex">The layer index.</param>
        ///<param name="stateID">The state ID.</param>
        ///<returns>True if the state exists in this layer, false otherwise.</returns>
        extern public bool HasState(int layerIndex, int stateID);


        // Generates an parameter id from a string
        ///<summary>Generates a parameter id from a string.</summary>
        ///<remarks>This method uses CRC32 to generate an id from a string. Use a generated id to optimize assigning and retrieving parameters. A generated id is valid as long as the input string doesn't change. This means that a generated id persists between sessions and can be used for networking.</remarks>
        ///<param name="name">The string to convert to an id.</param>
        ///<returns>The hash of the input string.</returns>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AnimatorPlayExample.cs}]]></code>
        ///</example>
        [NativeMethod(Name = "ScriptingStringToCRC32", IsThreadSafe = true)]
        extern public static int StringToHash(string name);

        // Gets/Sets the current Avatar
        ///<summary>Gets/Sets the current Avatar.</summary>
        extern public Avatar avatar
        {
            get;
            set;
        }

        extern internal string GetStats();

        ///<summary>The <see cref="T:UnityEngine.Playables.PlayableGraph" /> created by the <see cref="Animator" />.</summary>
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
        ///<summary>Additional layers affects the center of mass.</summary>
        extern public bool layersAffectMassCenter
        {
            get;
            set;
        }

        // Get left foot bottom height.
        ///<summary>Get left foot bottom height.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void LateUpdate()
        ///    {
        ///        if (animator)
        ///        {
        ///            Vector3 leftFootT = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        ///            Quaternion leftFootQ = animator.GetIKRotation(AvatarIKGoal.LeftFoot);
        ///
        ///            Vector3 leftFootH = new Vector3(0, -animator.leftFeetBottomHeight, 0);
        ///
        ///            Vector3 pos = leftFootT + leftFootQ * leftFootH;
        ///            Debug.Log(pos);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float leftFeetBottomHeight
        {
            get;
        }

        // Get right foot bottom height.
        ///<summary>Get right foot bottom height.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Animator animator;
        ///
        ///    void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///    }
        ///
        ///    void LateUpdate()
        ///    {
        ///        if (animator)
        ///        {
        ///            Vector3 rightFootT = animator.GetIKPosition(AvatarIKGoal.RightFoot);
        ///            Quaternion rightFootQ = animator.GetIKRotation(AvatarIKGoal.RightFoot);
        ///
        ///            Vector3 rightFootH = new Vector3(0, -animator.rightFeetBottomHeight, 0);
        ///
        ///            Vector3 pos = rightFootT + rightFootQ * rightFootH;
        ///            Debug.Log(pos);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Evaluates the animator based on deltaTime.</summary>
        ///<remarks>Updating the animator with this function might not work well with the physics engine or any other system that is normally evaluated by the Game loop.</remarks>
        ///<param name="deltaTime">The time delta.</param>
        [NativeMethod("UpdateWithDelta")]
        extern public void Update(float deltaTime);

        ///<summary>Rebind all the animated properties and mesh data with the Animator.</summary>
        ///<remarks>This function can be used when you manually change your GameObject hierarchy by script, like combining meshes or swap a complete transform hierarchy.</remarks>
        public void Rebind() { Rebind(true); }
        extern private void Rebind(bool writeDefaultValues);

        // Applies the default root motion. Use in OnAvatarMove when you don't want to override the default root motion.
        ///<summary>Apply the default Root Motion.</summary>
        ///<remarks>Applies the default root motion. Use this in <see cref="M:UnityEngine.MonoBehaviour.OnAnimatorMove" /> or in <see cref="StateMachineBehaviour.OnStateMove" /> on frames where you don't want to handle the root motion manually.</remarks>
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

        ///<exclude />
        extern public bool logWarnings
        {
            get;
            set;
        }
        ///<summary>Sets whether the Animator sends events of type <see cref="AnimationEvent" />.</summary>
        ///<remarks>Default value is true.</remarks>
        extern public bool fireEvents
        {
            get;
            set;
        }

        ///<summary>Controls the behaviour of the Animator component when a GameObject is disabled.</summary>
        ///<remarks>This property is obsolete, use <see cref="Animator.keepAnimatorStateOnDisable" /> instead.
        ///
        ///
        ///Set to true to keep the current state of the Animator controller.
        ///
        ///Set to false to clear the current state of the Animator controller.
        ///
        ///The default value is false.
        ///
        ///
        ///This property is serializable and can be saved in a Prefab.
        ///
        ///This property applies to the AnimatorController associated with the Animator. This property does not affect <see cref="T:UnityEngine.Animations.AnimatorControllerPlayable" />.</remarks>
        [Obsolete("keepAnimatorControllerStateOnDisable is deprecated, use keepAnimatorStateOnDisable instead. (UnityUpgradable) -> keepAnimatorStateOnDisable", false)]
        public bool keepAnimatorControllerStateOnDisable
        {
            get { return keepAnimatorStateOnDisable; }
            set { keepAnimatorStateOnDisable = value;}
        }

        ///<summary>Controls the behaviour of the Animator component when a GameObject is inactive.</summary>
        ///<remarks>Set to true to keep the current state of the Animator controller.
        ///
        ///Set to false to clear the current state of the Animator controller.
        ///
        ///The default value is false.
        ///
        ///
        ///When this property is set to true, the Animator also preserves the animated values for inactive GameObjects. For example, a GameObject's transform is animated from x=0 to x=3 while it is active. When this GameObject is inactive, it keeps the animated value x=3 instead of x=0.
        ///
        ///
        ///This property is serializable and can be saved in a Prefab.
        ///
        ///This property applies to the AnimatorController associated with the Animator. This property does not affect <see cref="T:UnityEngine.Animations.AnimatorControllerPlayable" />.</remarks>
        extern public bool keepAnimatorStateOnDisable
        {
            get;
            set;
        }

        ///<summary>Specifies whether playable graph values are reset or preserved when the <see cref="Animator" /> is disabled.</summary>
        ///<remarks>Set this property to true to reset the playable graph to its default values when the <see cref="Animator" /> is disabled.
        ///
        ///Set to this property to false (default value) to preserve the current state and values of the playable graph, and to call the <see cref="Animator.WriteDefaultValues" /> method when the <see cref="Animator" /> is disabled.
        ///
        ///
        ///Setting this property to false also preserves animated values when a GameObject is disabled. For example, if a GameObject has animated values from x=0 to x=3, when the GameObject is disabled, it preserves the animated value x=3.
        ///
        ///
        ///This property is serializable. You can save it in a Prefab.</remarks>
        extern public bool writeDefaultValuesOnDisable
        {
            get;
            set;
        }

        ///<summary>Gets the value of a vector parameter.</summary>
        ///<param name="name">The name of the parameter.</param>
        [Obsolete("GetVector is deprecated.")]
        public Vector3 GetVector(string name)                     { return Vector3.zero; }
        ///<summary>Gets the value of a vector parameter.</summary>
        ///<param name="id">The id of the parameter. The id is generated using Animator::StringToHash.</param>
        [Obsolete("GetVector is deprecated.")]
        public Vector3 GetVector(int id)                          { return Vector3.zero; }
        ///<summary>Sets the value of a vector parameter.</summary>
        ///<param name="name">The name of the parameter.</param>
        ///<param name="value">The new value for the parameter.</param>
        [Obsolete("SetVector is deprecated.")]
        public void SetVector(string name, Vector3 value)         {}
        ///<summary>Sets the value of a vector parameter.</summary>
        ///<param name="id">The id of the parameter. The id is generated using Animator::StringToHash.</param>
        ///<param name="value">The new value for the parameter.</param>
        [Obsolete("SetVector is deprecated.")]
        public void SetVector(int id, Vector3 value)              {}

        ///<summary>Gets the value of a quaternion parameter.</summary>
        ///<param name="name">The name of the parameter.</param>
        [Obsolete("GetQuaternion is deprecated.")]
        public Quaternion GetQuaternion(string name)              { return Quaternion.identity; }
        ///<summary>Gets the value of a quaternion parameter.</summary>
        ///<param name="id">The id of the parameter. The id is generated using Animator::StringToHash.</param>
        [Obsolete("GetQuaternion is deprecated.")]
        public Quaternion GetQuaternion(int id)                   { return Quaternion.identity; }
        ///<summary>Sets the value of a quaternion parameter.</summary>
        ///<param name="name">The name of the parameter.</param>
        ///<param name="value">The new value for the parameter.</param>
        [Obsolete("SetQuaternion is deprecated.")]
        public void SetQuaternion(string name, Quaternion value)  {}
        ///<summary>Sets the value of a quaternion parameter.</summary>
        ///<param name="id">Of the parameter. The id is generated using Animator::StringToHash.</param>
        ///<param name="value">The new value for the parameter.</param>
        [Obsolete("SetQuaternion is deprecated.")]
        public void SetQuaternion(int id, Quaternion value)       {}
    }
}
