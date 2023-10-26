// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Determines how to snap physics joints back to its constrained position when it drifts off too much. Note: PositionOnly is not supported anymore!
    // TODO: We should just move to a flag and remove this enum
    public enum JointProjectionMode
    {
        None = 0,
        PositionAndRotation = 1,

        // Snap Position only
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("JointProjectionMode.PositionOnly is no longer supported", true)]
        PositionOnly = 2
    }

    public enum RotationDriveMode
    {
        XYAndZ = 0,
        Slerp = 1
    }

    public enum ConfigurableJointMotion
    {
        Locked = 0,
        Limited = 1,
        Free = 2
    }

    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/ConfigurableJoint.h")]
    [NativeClass("Unity::ConfigurableJoint")]
    public class ConfigurableJoint : Joint
    {
        extern public Vector3 secondaryAxis { get; set; }
        extern public ConfigurableJointMotion xMotion { get; set; }
        extern public ConfigurableJointMotion yMotion { get; set; }
        extern public ConfigurableJointMotion zMotion { get; set; }
        extern public ConfigurableJointMotion angularXMotion { get; set; }
        extern public ConfigurableJointMotion angularYMotion { get; set; }
        extern public ConfigurableJointMotion angularZMotion { get; set; }
        extern public SoftJointLimitSpring linearLimitSpring { get; set; }
        extern public SoftJointLimitSpring angularXLimitSpring { get; set; }
        extern public SoftJointLimitSpring angularYZLimitSpring { get; set; }
        extern public SoftJointLimit linearLimit { get; set; }
        extern public SoftJointLimit lowAngularXLimit { get; set; }
        extern public SoftJointLimit highAngularXLimit { get; set; }
        extern public SoftJointLimit angularYLimit { get; set; }
        extern public SoftJointLimit angularZLimit { get; set; }
        extern public Vector3 targetPosition { get; set; }
        extern public Vector3 targetVelocity { get; set; }
        extern public JointDrive xDrive { get; set; }
        extern public JointDrive yDrive { get; set; }
        extern public JointDrive zDrive { get; set; }
        extern public Quaternion targetRotation { get; set; }
        extern public Vector3 targetAngularVelocity { get; set; }
        extern public RotationDriveMode rotationDriveMode { get; set; }
        extern public JointDrive angularXDrive { get; set; }
        extern public JointDrive angularYZDrive { get; set; }
        extern public JointDrive slerpDrive { get; set; }
        extern public JointProjectionMode projectionMode { get; set; }
        extern public float projectionDistance { get; set; }
        extern public float projectionAngle { get; set; }
        extern public bool configuredInWorldSpace { get; set; }
        extern public bool swapBodies { get; set; }
    }
}
