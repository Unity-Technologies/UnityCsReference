// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{
    ///<summary>Enumeration of all the muscles in the body.</summary>
    ///<remarks>These muscles are a sub-part of a human part.</remarks>
    ///<seealso cref="HumanPartDof" />
    public enum BodyDof
    {
        ///<summary>The spine front-back muscle.</summary>
        SpineFrontBack = 0,
        ///<summary>The spine left-right muscle.</summary>
        SpineLeftRight,
        ///<summary>The spine roll left-right muscle.</summary>
        SpineRollLeftRight,
        ///<summary>The chest front-back muscle.</summary>
        ChestFrontBack,
        ///<summary>The chest left-right muscle.</summary>
        ChestLeftRight,
        ///<summary>The chest roll left-right muscle.</summary>
        ChestRollLeftRight,
        ///<summary>The upper chest front-back muscle.</summary>
        UpperChestFrontBack,
        ///<summary>The upper chest left-right muscle.</summary>
        UpperChestLeftRight,
        ///<summary>The upper chest roll left-right muscle.</summary>
        UpperChestRollLeftRight,
        ///<summary>The last value of the <see cref="BodyDof" /> enum.</summary>
        ///<remarks>This value can be used in <c>for</c> loops.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///using UnityEngine.Animations;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        for (int i = 0; i < (int)BodyDof.LastBodyDof; ++i)
        ///        {
        ///            var handle = new MuscleHandle((BodyDof)i);
        ///            Debug.Log(handle.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        LastBodyDof
    }

    ///<summary>Enumeration of all the muscles in the head.</summary>
    ///<remarks>These muscles are a sub-part of a human part.</remarks>
    ///<seealso cref="HumanPartDof" />
    public enum HeadDof
    {
        ///<summary>The neck front-back muscle.</summary>
        NeckFrontBack = 0,
        ///<summary>The neck left-right muscle.</summary>
        NeckLeftRight,
        ///<summary>The neck roll left-right muscle.</summary>
        NeckRollLeftRight,
        ///<summary>The head front-back muscle.</summary>
        HeadFrontBack,
        ///<summary>The head left-right muscle.</summary>
        HeadLeftRight,
        ///<summary>The head roll left-right muscle.</summary>
        HeadRollLeftRight,
        ///<summary>The left eye down-up muscle.</summary>
        LeftEyeDownUp,
        ///<summary>The left eye in-out muscle.</summary>
        LeftEyeInOut,
        ///<summary>The right eye down-up muscle.</summary>
        RightEyeDownUp,
        ///<summary>The right eye in-out muscle.</summary>
        RightEyeInOut,
        ///<summary>The jaw down-up muscle.</summary>
        JawDownUp,
        ///<summary>The jaw left-right muscle.</summary>
        JawLeftRight,
        ///<summary>The last value of the <see cref="HeadDof" /> enum.</summary>
        ///<remarks>This value can be used in <c>for</c> loops.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///using UnityEngine.Animations;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        for (int i = 0; i < (int)HeadDof.LastHeadDof; ++i)
        ///        {
        ///            var handle = new MuscleHandle((HeadDof)i);
        ///            Debug.Log(handle.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        LastHeadDof
    }

    ///<summary>Enumeration of all the muscles in a leg.</summary>
    ///<remarks>These muscles are a sub-part of a human part.</remarks>
    ///<seealso cref="HumanPartDof" />
    public enum LegDof
    {
        ///<summary>The upper leg front-back muscle.</summary>
        UpperLegFrontBack = 0,
        ///<summary>The upper leg in-out muscle.</summary>
        UpperLegInOut,
        ///<summary>The upper leg roll in-out muscle.</summary>
        UpperLegRollInOut,
        ///<summary>The leg close-open muscle.</summary>
        LegCloseOpen,
        ///<summary>The leg roll in-out muscle.</summary>
        LegRollInOut,
        ///<summary>The foot close-open muscle.</summary>
        FootCloseOpen,
        ///<summary>The foot in-out muscle.</summary>
        FootInOut,
        ///<summary>The toes up-down muscle.</summary>
        ToesUpDown,
        ///<summary>The last value of the <see cref="LegDof" /> enum.</summary>
        ///<remarks>This value can be used in <c>for</c> loops.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///using UnityEngine.Animations;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        for (int i = 0; i < (int)LegDof.LastLegDof; ++i)
        ///        {
        ///            var handle = new MuscleHandle(HumanPartDof.LeftLeg, (LegDof)i);
        ///            Debug.Log(handle.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        LastLegDof
    }

    ///<summary>Enumeration of all the muscles in an arm.</summary>
    ///<remarks>These muscles are a sub-part of a human part.</remarks>
    ///<seealso cref="HumanPartDof" />
    public enum ArmDof
    {
        ///<summary>The shoulder down-up muscle.</summary>
        ShoulderDownUp = 0,
        ///<summary>The shoulder front-back muscle.</summary>
        ShoulderFrontBack,
        ///<summary>The arm down-up muscle.</summary>
        ArmDownUp,
        ///<summary>The arm front-back muscle.</summary>
        ArmFrontBack,
        ///<summary>The arm roll in-out muscle.</summary>
        ArmRollInOut,
        ///<summary>The forearm close-open muscle.</summary>
        ForeArmCloseOpen,
        ///<summary>The forearm roll in-out muscle.</summary>
        ForeArmRollInOut,
        ///<summary>The hand down-up muscle.</summary>
        HandDownUp,
        ///<summary>The hand in-out muscle.</summary>
        HandInOut,
        ///<summary>The last value of the <see cref="ArmDof" /> enum.</summary>
        ///<remarks>This value can be used in <c>for</c> loops.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///using UnityEngine.Animations;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        for (int i = 0; i < (int)ArmDof.LastArmDof; ++i)
        ///        {
        ///            var handle = new MuscleHandle(HumanPartDof.LeftArm, (ArmDof)i);
        ///            Debug.Log(handle.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        LastArmDof
    }

    ///<summary>Enumeration of all the muscles in a finger.</summary>
    ///<remarks>These muscles are a sub-part of a human part.</remarks>
    ///<seealso cref="HumanPartDof" />
    public enum FingerDof
    {
        ///<summary>The proximal down-up muscle.</summary>
        ProximalDownUp = 0,
        ///<summary>The proximal in-out muscle.</summary>
        ProximalInOut,
        ///<summary>The intermediate close-open muscle.</summary>
        IntermediateCloseOpen,
        ///<summary>The distal close-open muscle.</summary>
        DistalCloseOpen,
        ///<summary>The last value of the <see cref="FingerDof" /> enum.</summary>
        ///<remarks>This value can be used in <c>for</c> loops.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///using UnityEngine.Animations;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        for (int i = 0; i < (int)FingerDof.LastFingerDof; ++i)
        ///        {
        ///            var handle = new MuscleHandle(HumanPartDof.LeftThumb, (FingerDof)i);
        ///            Debug.Log(handle.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        LastFingerDof
    }

    ///<summary>Enumeration of all the parts in a human.</summary>
    ///<remarks>A human part is a region defining a set of muscles in a human, to get a specific muscle, it has to be used with another DoF enumerations.</remarks>
    ///<seealso cref="BodyDof" />
    ///<seealso cref="HeadDof" />
    ///<seealso cref="LegDof" />
    ///<seealso cref="ArmDof" />
    ///<seealso cref="FingerDof" />
    public enum HumanPartDof
    {
        ///<summary>The human body part.</summary>
        ///<seealso cref="BodyDof" />
        Body = 0,
        ///<summary>The human head part.</summary>
        ///<seealso cref="HeadDof" />
        Head,
        ///<summary>The human left leg part.</summary>
        ///<seealso cref="LegDof" />
        LeftLeg,
        ///<summary>The human right leg part.</summary>
        ///<seealso cref="LegDof" />
        RightLeg,
        ///<summary>The human left arm part.</summary>
        ///<seealso cref="ArmDof" />
        LeftArm,
        ///<summary>The human right arm part.</summary>
        ///<seealso cref="ArmDof" />
        RightArm,
        ///<summary>The human left thumb finger part.</summary>
        ///<seealso cref="FingerDof" />
        LeftThumb,
        ///<summary>The human left index finger part.</summary>
        ///<seealso cref="FingerDof" />
        LeftIndex,
        ///<summary>The human left middle finger part.</summary>
        ///<seealso cref="FingerDof" />
        LeftMiddle,
        ///<summary>The human left ring finger part.</summary>
        ///<seealso cref="FingerDof" />
        LeftRing,
        ///<summary>The human left little finger part.</summary>
        ///<seealso cref="FingerDof" />
        LeftLittle,
        ///<summary>The human right thumb finger part.</summary>
        ///<seealso cref="FingerDof" />
        RightThumb,
        ///<summary>The human right index finger part.</summary>
        ///<seealso cref="FingerDof" />
        RightIndex,
        ///<summary>The human right middle finger part.</summary>
        ///<seealso cref="FingerDof" />
        RightMiddle,
        ///<summary>The human right ring finger part.</summary>
        ///<seealso cref="FingerDof" />
        RightRing,
        ///<summary>The human right little finger part.</summary>
        ///<seealso cref="FingerDof" />
        RightLittle,
        ///<exclude />
        LastHumanPartDof
    }

    internal enum Dof
    {
        BodyDofStart = 0,
        HeadDofStart = (int)BodyDofStart + (int)BodyDof.LastBodyDof,
        LeftLegDofStart = (int)HeadDofStart + (int)HeadDof.LastHeadDof,
        RightLegDofStart = (int)LeftLegDofStart + (int)LegDof.LastLegDof,
        LeftArmDofStart = (int)RightLegDofStart + (int)LegDof.LastLegDof,
        RightArmDofStart = (int)LeftArmDofStart + (int)ArmDof.LastArmDof,

        LeftThumbDofStart = (int)RightArmDofStart + (int)ArmDof.LastArmDof,
        LeftIndexDofStart = (int)LeftThumbDofStart + (int)FingerDof.LastFingerDof,
        LeftMiddleDofStart = (int)LeftIndexDofStart + (int)FingerDof.LastFingerDof,

        LeftRingDofStart = (int)LeftMiddleDofStart + (int)FingerDof.LastFingerDof,
        LeftLittleDofStart = (int)LeftRingDofStart + (int)FingerDof.LastFingerDof,

        RightThumbDofStart = (int)LeftLittleDofStart + (int)FingerDof.LastFingerDof,
        RightIndexDofStart = (int)RightThumbDofStart + (int)FingerDof.LastFingerDof,
        RightMiddleDofStart = (int)RightIndexDofStart + (int)FingerDof.LastFingerDof,
        RightRingDofStart = (int)RightMiddleDofStart + (int)FingerDof.LastFingerDof,
        RightLittleDofStart = (int)RightRingDofStart + (int)FingerDof.LastFingerDof,

        LastDof = (int)RightLittleDofStart + (int)FingerDof.LastFingerDof
    }

    // Human Body Bones
    ///<summary>Human Body Bones.</summary>
    ///<seealso cref="Animator.GetBoneTransform" />
    public enum HumanBodyBones
    {
        // This is the Hips bone
        ///<summary>This is the Hips bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        Hips = 0,

        // This is the Left Upper Leg bone
        ///<summary>This is the Left Upper Leg bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftUpperLeg = 1,

        // This is the Right Upper Leg bone
        ///<summary>This is the Right Upper Leg bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightUpperLeg = 2,

        // This is the Left Knee bone
        ///<summary>This is the Left Knee bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftLowerLeg = 3,

        // This is the Right Knee bone
        ///<summary>This is the Right Knee bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightLowerLeg = 4,

        // This is the Left Ankle bone
        ///<summary>This is the Left Ankle bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftFoot = 5,

        // This is the Right Ankle bone
        ///<summary>This is the Right Ankle bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightFoot = 6,

        // This is the first Spine bone
        ///<summary>This is the first Spine bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        Spine = 7,

        // This is the Chest bone
        ///<summary>This is the Chest bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        Chest = 8,

        // This is the UpperChest bone
        ///<summary>This is the Upper Chest bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        UpperChest = 54,

        // This is the Neck bone
        ///<summary>This is the Neck bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        Neck = 9,

        // This is the Head bone
        ///<summary>This is the Head bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        Head = 10,

        // This is the Left Shoulder bone
        ///<summary>This is the Left Shoulder bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftShoulder = 11,

        // This is the Right Shoulder bone
        ///<summary>This is the Right Shoulder bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightShoulder = 12,

        // This is the Left Upper Arm bone
        ///<summary>This is the Left Upper Arm bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftUpperArm = 13,

        // This is the Right Upper Arm bone
        ///<summary>This is the Right Upper Arm bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightUpperArm = 14,

        // This is the Left Elbow bone
        ///<summary>This is the Left Elbow bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftLowerArm = 15,

        // This is the Right Elbow bone
        ///<summary>This is the Right Elbow bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightLowerArm = 16,

        // This is the Left Wrist bone
        ///<summary>This is the Left Wrist bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftHand = 17,

        // This is the Right Wrist bone
        ///<summary>This is the Right Wrist bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightHand = 18,

        // This is the Left Toes bone
        ///<summary>This is the Left Toes bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftToes = 19,

        // This is the Right Toes bone
        ///<summary>This is the Right Toes bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightToes = 20,

        // This is the Left Eye bone
        ///<summary>This is the Left Eye bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftEye = 21,

        // This is the Right Eye bone
        ///<summary>This is the Right Eye bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightEye = 22,

        // This is the Jaw bone
        ///<summary>This is the Jaw bone.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        Jaw = 23,

        ///<summary>This is the left thumb 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftThumbProximal = 24,
        ///<summary>This is the left thumb 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftThumbIntermediate = 25,
        ///<summary>This is the left thumb 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftThumbDistal = 26,

        ///<summary>This is the left index 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftIndexProximal = 27,
        ///<summary>This is the left index 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftIndexIntermediate = 28,
        ///<summary>This is the left index 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftIndexDistal = 29,

        ///<summary>This is the left middle 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftMiddleProximal = 30,
        ///<summary>This is the left middle 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftMiddleIntermediate = 31,
        ///<summary>This is the left middle 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftMiddleDistal = 32,

        ///<summary>This is the left ring 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftRingProximal = 33,
        ///<summary>This is the left ring 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftRingIntermediate = 34,
        ///<summary>This is the left ring 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftRingDistal = 35,

        ///<summary>This is the left little 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftLittleProximal = 36,
        ///<summary>This is the left little 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftLittleIntermediate = 37,
        ///<summary>This is the left little 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LeftLittleDistal = 38,

        ///<summary>This is the right thumb 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightThumbProximal = 39,
        ///<summary>This is the right thumb 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightThumbIntermediate = 40,
        ///<summary>This is the right thumb 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightThumbDistal = 41,

        ///<summary>This is the right index 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightIndexProximal = 42,
        ///<summary>This is the right index 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightIndexIntermediate = 43,
        ///<summary>This is the right index 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightIndexDistal = 44,

        ///<summary>This is the right middle 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightMiddleProximal = 45,
        ///<summary>This is the right middle 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightMiddleIntermediate = 46,
        ///<summary>This is the right middle 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightMiddleDistal = 47,

        ///<summary>This is the right ring 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightRingProximal = 48,
        ///<summary>This is the right ring 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightRingIntermediate = 49,
        ///<summary>This is the right ring 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightRingDistal = 50,

        ///<summary>This is the right little 1st phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightLittleProximal = 51,
        ///<summary>This is the right little 2nd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightLittleIntermediate = 52,
        ///<summary>This is the right little 3rd phalange.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        RightLittleDistal = 53,

        // UpperChest = 54

        // This is the Last bone index delimiter
        ///<summary>This is the Last bone index delimiter.</summary>
        ///<seealso cref="Animator.GetBoneTransform" />
        LastBone = 55
    }

    internal enum  HumanParameter
    {
        UpperArmTwist = 0,
        LowerArmTwist,
        UpperLegTwist,
        LowerLegTwist,
        ArmStretch,
        LegStretch,
        FeetSpacing
    }

    ///<summary>The avatar asset describes the mapping of the character in the <see cref="Animator" />.</summary>
    ///<remarks>Use the <see cref="Avatar" /> asset on a character with an <see cref="Animator" /> to control how the hierarchy animates.
    ///You must create an <see cref="Avatar" /> to use with humanoid animation. If there is no <see cref="Avatar" /> on a character
    ///with generic animation, an <see cref="Avatar" /> internal to the <see cref="Animator" /> is created instead.
    ///Normally, the <see cref="Avatar" /> is created by the <see cref="T:UnityEditor.ModelImporter" />, but it can be created manually with <see cref="AvatarBuilder" />
    ///or through the "Build Generic Avatar" menu item of the <see cref="Animator" /> context menu.
    ///
    ///**Generic Avatar**
    ///
    ///For generic animation, use <see cref="AvatarBuilder.BuildGenericAvatar" /> to specify what is the root of the animated
    ///hierarchy (This can be nested in the hierarchy of the <see cref="Animator" />) and the name of the node that holds the
    ///root motion animation.
    ///
    ///**Human Avatar**
    ///
    ///For humanoid animation, use <see cref="AvatarBuilder.BuildHumanAvatar" /> to specify what is the root of the animated
    ///hierarchy (this can be nested in the hierarchy of the <see cref="Animator" />) and the <see cref="HumanDescription" /> that provides
    ///the mapping of transforms to human bones.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/GenericAvatarBuilderExample.cs}]]></code>
    ///</example>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/HumanAvatarBuilderExample.cs}]]></code>
    ///</example>
    ///<seealso cref="Animator.avatar" />
    ///<seealso cref="AvatarBuilder" />
    ///<seealso cref="HumanDescription" />
    ///<seealso cref="T:UnityEditor.ModelImporter" />
    [NativeHeader("Modules/Animation/Avatar.h")]
    [global::UnityEngine.NativeClass("Avatar", PersistentTypeId = 90)]
    [UsedByNativeCode]
    public class Avatar : Object
    {
        internal Avatar(global::UnityEngine.EntityId id) : base(id) {}
        private Avatar()
        {
        }

        // Return true if this avatar is a valid mecanim avatar. It can be a generic avatar or a human avatar.
        ///<summary>Return true if this avatar is a valid mecanim avatar. It can be a generic avatar or a human avatar.</summary>
        extern public bool isValid
        {
            [NativeMethod("IsValid")]
            get;
        }

        // Return true if this avatar is a valid human avatar.
        ///<summary>Return true if this avatar is a valid human avatar.</summary>
        extern public bool isHuman
        {
            [NativeMethod("IsHuman")]
            get;
        }

        ///<summary>Returns the <see cref="HumanDescription" /> used to create this Avatar.</summary>
        ///<remarks>Note that an avatar created before 2019.1 returns an empty <see cref="HumanDescription" /> until the avatar is reimported.</remarks>
        extern public HumanDescription humanDescription
        {
            get;
        }

        extern internal void SetMuscleMinMax(int muscleId, float min, float max);

        extern internal void SetParameter(int parameterId, float value);

        internal float GetAxisLength(int humanId)
        {
            return Internal_GetAxisLength(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        internal Quaternion GetPreRotation(int humanId)
        {
            return Internal_GetPreRotation(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        internal Quaternion GetPostRotation(int humanId)
        {
            return Internal_GetPostRotation(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        internal Quaternion GetZYPostQ(int humanId, Quaternion parentQ, Quaternion q)
        {
            return Internal_GetZYPostQ(HumanTrait.GetBoneIndexFromMono(humanId), parentQ, q);
        }

        internal Quaternion GetZYRoll(int humanId, Vector3 uvw)
        {
            return Internal_GetZYRoll(HumanTrait.GetBoneIndexFromMono(humanId), uvw);
        }

        internal Vector3 GetLimitSign(int humanId)
        {
            return Internal_GetLimitSign(HumanTrait.GetBoneIndexFromMono(humanId));
        }

        [NativeMethod("GetAxisLength")]
        extern internal float Internal_GetAxisLength(int humanId);

        [NativeMethod("GetPreRotation")]
        extern internal Quaternion Internal_GetPreRotation(int humanId);

        [NativeMethod("GetPostRotation")]
        extern internal Quaternion Internal_GetPostRotation(int humanId);

        [NativeMethod("GetZYPostQ")]
        extern internal Quaternion Internal_GetZYPostQ(int humanId, Quaternion parentQ, Quaternion q);

        [NativeMethod("GetZYRoll")]
        extern internal Quaternion Internal_GetZYRoll(int humanId, Vector3 uvw);

        [NativeMethod("GetLimitSign")]
        extern internal Vector3 Internal_GetLimitSign(int humanId);
    }
}
