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
    ///<summary>Determines how to snap physics joints back to its constrained position when it drifts off too much.</summary>
    ///<seealso cref="ConfigurableJoint" />
    public enum JointProjectionMode
    {
        ///<summary>Don't snap at all.</summary>
        None = 0,
        ///<summary>Snap both position and rotation.</summary>
        PositionAndRotation = 1,

        ///<summary>Snap Position only.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("JointProjectionMode.PositionOnly is no longer supported", true)]
        PositionOnly = 2
    }

    ///<summary>Control <see cref="ConfigurableJoint" />'s rotation with either X &amp; YZ or Slerp Drive.</summary>
    public enum RotationDriveMode
    {
        ///<summary>Use XY &amp; Z Drive.</summary>
        XYAndZ = 0,
        ///<summary>Use Slerp drive.</summary>
        Slerp = 1
    }

    ///<summary>Constrains movement for a <see cref="ConfigurableJoint" /> along the 6 axes.</summary>
    public enum ConfigurableJointMotion
    {
        ///<summary>Motion along the axis will be locked.</summary>
        Locked = 0,
        ///<summary>Motion along the axis will be limited by the respective limit.</summary>
        Limited = 1,
        ///<summary>Motion along the axis will be completely free and completely unconstrained.</summary>
        Free = 2
    }

    ///<summary>The configurable joint is an extremely flexible joint giving you complete control over rotation and linear motion.</summary>
    ///<remarks>You can build all other joints with it and much more but it is also more complicated to setup.
    ///It gives you control over motors, drives and joint limits for each rotation axis and and linear degree of freedom.</remarks>
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/ConfigurableJoint.h")]
    [NativeClass("Unity::ConfigurableJoint", PersistentTypeId = 153)]
    public class ConfigurableJoint : Joint
    {
        internal ConfigurableJoint(global::UnityEngine.EntityId id) : base(id) {}
        public ConfigurableJoint() {}
        ///<summary>The joint's secondary axis.</summary>
        ///<remarks>Together, secondary and primary axes define the joint's coordinate space.</remarks>
        extern public Vector3 secondaryAxis { get; set; }
        ///<summary>Allow movement along the X axis to be Free, completely Locked, or Limited according to Linear Limit.</summary>
        extern public ConfigurableJointMotion xMotion { get; set; }
        ///<summary>Allow movement along the Y axis to be Free, completely Locked, or Limited according to Linear Limit.</summary>
        extern public ConfigurableJointMotion yMotion { get; set; }
        ///<summary>Allow movement along the Z axis to be Free, completely Locked, or Limited according to Linear Limit.</summary>
        extern public ConfigurableJointMotion zMotion { get; set; }
        ///<summary>Allow rotation around the X axis to be Free, completely Locked, or Limited according to Low and High Angular XLimit.</summary>
        extern public ConfigurableJointMotion angularXMotion { get; set; }
        ///<summary>Allow rotation around the Y axis to be Free, completely Locked, or Limited according to Angular YLimit.</summary>
        extern public ConfigurableJointMotion angularYMotion { get; set; }
        ///<summary>Allow rotation around the Z axis to be Free, completely Locked, or Limited according to Angular ZLimit.</summary>
        extern public ConfigurableJointMotion angularZMotion { get; set; }
        ///<summary>The configuration of the spring attached to the linear limit of the joint.</summary>
        extern public SoftJointLimitSpring linearLimitSpring { get; set; }
        ///<summary>The configuration of the spring attached to the angular X limit of the joint.</summary>
        extern public SoftJointLimitSpring angularXLimitSpring { get; set; }
        ///<summary>The configuration of the spring attached to the angular Y and angular Z limits of the joint.</summary>
        extern public SoftJointLimitSpring angularYZLimitSpring { get; set; }
        ///<summary>Boundary defining movement restriction, based on distance from the joint's origin.</summary>
        extern public SoftJointLimit linearLimit { get; set; }
        ///<summary>Boundary defining lower rotation restriction, based on delta from original rotation.</summary>
        extern public SoftJointLimit lowAngularXLimit { get; set; }
        ///<summary>Boundary defining upper rotation restriction, based on delta from original rotation.</summary>
        extern public SoftJointLimit highAngularXLimit { get; set; }
        ///<summary>Boundary defining rotation restriction, based on delta from original rotation.</summary>
        extern public SoftJointLimit angularYLimit { get; set; }
        ///<summary>Boundary defining rotation restriction, based on delta from original rotation.</summary>
        extern public SoftJointLimit angularZLimit { get; set; }
        ///<summary>The desired position that the joint should move into.</summary>
        extern public Vector3 targetPosition { get; set; }
        ///<summary>The desired velocity that the joint should move along.</summary>
        extern public Vector3 targetVelocity { get; set; }
        ///<summary>Definition of how the joint's movement will behave along its local X axis.</summary>
        extern public JointDrive xDrive { get; set; }
        ///<summary>Definition of how the joint's movement will behave along its local Y axis.</summary>
        extern public JointDrive yDrive { get; set; }
        ///<summary>Definition of how the joint's movement will behave along its local Z axis.</summary>
        extern public JointDrive zDrive { get; set; }
        ///<summary>This is a <see cref="Quaternion" />. It defines the desired rotation that the joint should rotate into.</summary>
        extern public Quaternion targetRotation { get; set; }
        ///<summary>This is a <see cref="Vector3" />. It defines the desired angular velocity that the joint should rotate into.</summary>
        extern public Vector3 targetAngularVelocity { get; set; }
        ///<summary>Control the object's rotation with either X &amp; YZ or Slerp Drive by itself.</summary>
        extern public RotationDriveMode rotationDriveMode { get; set; }
        ///<summary>Definition of how the joint's rotation will behave around its local X axis. Only used if Rotation Drive Mode is Swing &amp; Twist.</summary>
        extern public JointDrive angularXDrive { get; set; }
        ///<summary>Definition of how the joint's rotation will behave around its local Y and Z axes. Only used if Rotation Drive Mode is Swing &amp; Twist.</summary>
        extern public JointDrive angularYZDrive { get; set; }
        ///<summary>Definition of how the joint's rotation will behave around all local axes. Only used if Rotation Drive Mode is Slerp Only.</summary>
        extern public JointDrive slerpDrive { get; set; }
        ///<summary>Brings violated constraints back into alignment even when the solver fails. Projection is not a physical process and does not preserve momentum or respect collision geometry. It is best avoided if practical, but can be useful in improving simulation quality where joint separation results in unacceptable artifacts.</summary>
        extern public JointProjectionMode projectionMode { get; set; }
        ///<summary>Set the linear tolerance threshold for projection.
        ///
        ///If the joint separates by more than this distance along its locked degrees of freedom, the solver
        ///will move the bodies to close the distance.
        ///
        ///Setting a very small tolerance may result in simulation jitter or other artifacts.
        ///
        ///Sometimes it is not possible to project (for example when the joints form a cycle).</summary>
        extern public float projectionDistance { get; set; }
        ///<summary>Set the angular tolerance threshold (in degrees) for projection.
        ///
        ///If the joint deviates by more than this angle around its locked angular degrees of freedom,
        ///the solver will move the bodies to close the angle.
        ///
        ///Setting a very small tolerance may result in simulation jitter or other artifacts.
        ///
        ///Sometimes it is not possible to project (for example when the joints form a cycle).</summary>
        extern public float projectionAngle { get; set; }
        ///<summary>If enabled, all Target values will be calculated in world space instead of the object's local space.</summary>
        extern public bool configuredInWorldSpace { get; set; }
        ///<summary>Enable this property to swap the order in which the physics engine processes the Rigidbodies involved in the joint. This results in different joint motion but has no impact on Rigidbodies and anchors.</summary>
        ///<remarks>Prior to Unity 3.4, this was wrongfully applied to all ConfigurableJoints with the configuredInWorldSpace
        ///property set. If you want to restore the behaviour of older Unity versions and you are using
        ///configuredInWorldSpace, enable this property.</remarks>
        extern public bool swapBodies { get; set; }
    }
}
