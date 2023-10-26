// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/HingeJoint.h")]
    [NativeClass("Unity::HingeJoint")]
    public class HingeJoint : Joint
    {
        extern public JointMotor motor { get; set; }
        extern public JointLimits limits { get; set; }
        extern public JointSpring spring { get; set; }
        extern public bool useMotor { get; set; }
        extern public bool useLimits { get; set; }
        extern public bool extendedLimits { get; set; }
        extern public bool useSpring { get; set; }
        extern public float velocity { get; }
        extern public float angle { get; }
        extern public bool useAcceleration { get; set; }
    }
}
