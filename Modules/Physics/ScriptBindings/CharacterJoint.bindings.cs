// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/CharacterJoint.h")]
    [NativeClass("Unity::CharacterJoint")]
    public partial class CharacterJoint : Joint
    {
        extern public Vector3 swingAxis { get; set; }
        extern public SoftJointLimitSpring twistLimitSpring { get; set; }
        extern public SoftJointLimitSpring swingLimitSpring { get; set; }
        extern public SoftJointLimit lowTwistLimit { get; set; }
        extern public SoftJointLimit highTwistLimit { get; set; }
        extern public SoftJointLimit swing1Limit { get; set; }
        extern public SoftJointLimit swing2Limit { get; set; }
        extern public bool enableProjection { get; set; }
        extern public float projectionDistance { get; set; }
        extern public float projectionAngle { get; set; }
    }
}
