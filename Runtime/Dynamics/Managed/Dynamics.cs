// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Use these flags to constrain motion of Rigidbodies.
    public enum RigidbodyConstraints
    {
        // No constraints
        None = 0,

        // Freeze motion along the X-axis.
        FreezePositionX = 0x02,

        // Freeze motion along the Y-axis.
        FreezePositionY = 0x04,

        // Freeze motion along the Z-axis.
        FreezePositionZ = 0x08,

        // Freeze rotation along the X-axis.
        FreezeRotationX = 0x10,

        // Freeze rotation along the Y-axis.
        FreezeRotationY = 0x20,

        // Freeze rotation along the Z-axis.
        FreezeRotationZ = 0x40,

        // Freeze motion along all axes.
        FreezePosition = 0x0e,

        // Freeze rotation along all axes.
        FreezeRotation = 0x70,

        // Freeze rotation and motion along all axes.
        FreezeAll = 0x7e,
    }

    // Option for how to apply a force using Rigidbody.AddForce.
    public enum ForceMode
    {
        // Add a continuous force to the rigidbody, using its mass.
        Force = 0,

        // Add a continuous acceleration to the rigidbody, ignoring its mass.
        Acceleration = 5,

        // Add an instant force impulse to the rigidbody, using its mass.
        Impulse = 1,

        // Add an instant velocity change to the rigidbody, ignoring its mass.
        VelocityChange = 2,
    }

    // The [[ConfigurableJoint]] attempts to attain position / velocity targets based on this flag
    [Flags()]
    [Obsolete("JointDriveMode is no longer supported")]
    public enum JointDriveMode
    {
        [Obsolete("JointDriveMode.None is no longer supported")]
        // Don't apply any forces to reach the target
        None = 0,

        [Obsolete("JointDriveMode.Position is no longer supported")]
        // Try to reach the specified target position
        Position = 1,

        [Obsolete("JointDriveMode.Velocity is no longer supported")]
        // Try to reach the specified target velocity
        Velocity = 2,

        [Obsolete("JointDriveMode.PositionAndvelocity is no longer supported")]
        // Try to reach the specified target position and velocity
        PositionAndVelocity = 3
    }

    // Determines how to snap physics joints back to its constrained position when it drifts off too much. Note: PositionOnly is not supported anymore!
    public enum JointProjectionMode
    {
        // Don't snap at all
        None = 0,

        // Snap both position and rotation
        PositionAndRotation = 1,

        // Snap Position only
        [Obsolete("JointProjectionMode.PositionOnly is no longer supported", true)]
        PositionOnly = 2
    }

    [Flags]
    public enum MeshColliderCookingOptions
    {
        None,
        InflateConvexMesh = 1 << 0,
        CookForFasterSimulation = 1 << 1,
        EnableMeshCleaning = 1 << 2,
        WeldColocatedVertices = 1 << 3
    }

    // WheelFrictionCurve is used by the [[WheelCollider]] to describe friction properties of the wheel tire.
    public struct WheelFrictionCurve
    {
        private float m_ExtremumSlip;
        private float m_ExtremumValue;
        private float m_AsymptoteSlip;
        private float m_AsymptoteValue;
        private float m_Stiffness;

        // Extremum point slip (default 1).
        public float extremumSlip { get { return m_ExtremumSlip; } set { m_ExtremumSlip = value; } }

        // Force at the extremum slip (default 20000).
        public float extremumValue { get { return m_ExtremumValue; } set { m_ExtremumValue = value; } }

        // Asymptote point slip (default 2).
        public float asymptoteSlip { get { return m_AsymptoteSlip; } set { m_AsymptoteSlip = value; } }

        // Force at the asymptote slip (default 10000).
        public float asymptoteValue { get { return m_AsymptoteValue; } set { m_AsymptoteValue = value; } }

        // Multiplier for the ::ref::extremumValue and ::ref::asymptoteValue values (default 1).
        public float stiffness { get { return m_Stiffness; } set { m_Stiffness = value; } }
    }

    // The limits defined by the [[CharacterJoint]]
    public struct SoftJointLimit
    {
        private float m_Limit;
        private float m_Bounciness;
        private float m_ContactDistance;

        // The limit position/angle of the joint.
        public float limit { get { return m_Limit; } set { m_Limit = value; } }

        [Obsolete("Spring has been moved to SoftJointLimitSpring class in Unity 5", true)]
        public float spring { get { return 0; } set {} }

        [Obsolete("Damper has been moved to SoftJointLimitSpring class in Unity 5", true)]
        public float damper { get { return 0; } set {} }

        // When the joint hits the limit, it can be made to bounce off it.
        public float bounciness { get { return m_Bounciness; } set { m_Bounciness = value; } }

        /// Within the contact distance from the limit contacts will persist in order to avoid jitter.
        public float contactDistance { get { return m_ContactDistance; } set { m_ContactDistance = value; } }

        [Obsolete("Use SoftJointLimit.bounciness instead", true)]
        public float bouncyness { get { return m_Bounciness; } set { m_Bounciness = value; } }
    }

    public struct SoftJointLimitSpring
    {
        private float m_Spring;
        private float m_Damper;

        // If greater than zero, the limit is soft. The spring will pull the joint back.
        public float spring { get { return m_Spring; } set { m_Spring = value; } }

        // If spring is greater than zero, the limit is soft.
        public float damper { get { return m_Damper; } set { m_Damper = value; } }
    }

    // How the joint's movement will behave along its local X axis
    public struct JointDrive
    {
        private float m_PositionSpring;
        private float m_PositionDamper;
        private float m_MaximumForce;

        [Obsolete("JointDriveMode is obsolete")]
        // Whether the drive should attempt to reach position, velocity, both or nothing
        public JointDriveMode mode { get { return (JointDriveMode)0; } set {} }

        // Strength of a rubber-band pull toward the defined direction. Only used if /mode/ includes Position.
        public float positionSpring { get { return m_PositionSpring; } set { m_PositionSpring = value; } }

        // Resistance strength against the Position Spring. Only used if /mode/ includes Position.
        public float positionDamper { get { return m_PositionDamper; } set { m_PositionDamper = value; } }

        // Amount of force applied to push the object toward the defined direction.
        public float maximumForce { get { return m_MaximumForce; } set { m_MaximumForce = value; } }
    }

    public enum RigidbodyInterpolation
    {
        // No Interpolation.
        None = 0,

        // Interpolation will always lag a little bit behind but can be smoother than extrapolation.
        Interpolate = 1,

        // Extrapolation will predict the position of the rigidbody based on the current velocity.
        Extrapolate = 2
    }

    // The JointMotor is used to motorize a joint.
    public struct JointMotor
    {
        private float m_TargetVelocity;
        private float m_Force;
        private int  m_FreeSpin;

        // The motor will apply a force up to /force/ to achieve /targetVelocity/.
        public float targetVelocity { get { return m_TargetVelocity; } set { m_TargetVelocity = value; } }

        // The motor will apply a force.
        public float force { get { return m_Force; } set { m_Force = value; } }

        // If /freeSpin/ is enabled the motor will only accelerate but never slow down.
        public bool  freeSpin { get { return m_FreeSpin == 1; } set { m_FreeSpin = value ? 1 : 0; } }
    }

    // JointSpring is used add a spring force to [[HingeJoint]] and [[PhysicMaterial]].
    public struct JointSpring
    {
        // The spring forces used to reach the target position
        public float spring;

        // The damper force uses to dampen the spring
        public float damper;

        // The target position the joint attempts to reach.
        public float targetPosition;

        // We have to keep those as public variables because of a bug in the C# raycast sample.
    }

    // JointLimits is used by the [[HingeJoint]] to limit the joints angle.
    public struct JointLimits
    {
        private float m_Min;
        private float m_Max;
        private float m_Bounciness;
        private float m_BounceMinVelocity;
        private float m_ContactDistance;

        public float min { get { return m_Min; } set { m_Min = value; } }

        public float max { get { return m_Max; } set { m_Max = value; } }

        public float bounciness { get { return m_Bounciness; } set { m_Bounciness = value; } }

        public float bounceMinVelocity { get { return m_BounceMinVelocity; } set { m_BounceMinVelocity = value; } }

        public float contactDistance { get { return m_ContactDistance; } set { m_ContactDistance = value; } }

        [Obsolete("minBounce and maxBounce are replaced by a single JointLimits.bounciness for both limit ends.", true)]
        public float minBounce;

        [Obsolete("minBounce and maxBounce are replaced by a single JointLimits.bounciness for both limit ends.", true)]
        public float maxBounce;
    }

    // ControllerColliderHit is used by CharacterController.OnControllerColliderHit to give detailed information about the collision and how to deal with it.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public partial class ControllerColliderHit
    {
        internal CharacterController m_Controller;
        internal Collider            m_Collider;
        internal Vector3             m_Point;
        internal Vector3             m_Normal;
        internal Vector3             m_MoveDirection;
        internal float               m_MoveLength;
        internal int                 m_Push;

        // The controller that hit the collider
        public CharacterController controller { get { return m_Controller; } }

        // The collider that was hit by the controller
        public Collider collider { get { return m_Collider; } }

        // The rigidbody that was hit by the controller.
        public Rigidbody rigidbody { get { return m_Collider.attachedRigidbody; } }

        // The game object that was hit by the controller.
        public GameObject gameObject { get { return m_Collider.gameObject; } }

        // The transform that was hit by the controller.
        public Transform transform { get { return m_Collider.transform; } }

        // The impact point in world space.
        public Vector3 point { get { return m_Point; } }

        // The normal of the surface we collided with in world space.
        public Vector3 normal { get { return m_Normal; } }

        // Approximately the direction from the center of the capsule to the point we touch.
        public Vector3 moveDirection { get { return m_MoveDirection; } }

        // How far the character has travelled until it hit the collider.
        public float moveLength { get { return m_MoveLength; } }

        //*undocumented NOT IMPLEMENTED
        private bool push { get { return m_Push != 0; } set { m_Push = value ? 1 : 0; } }
    }

    // Describes how physic materials of colliding objects are combined.
    public enum PhysicMaterialCombine
    {
        // Averages the friction/bounce of the two colliding materials.
        Average = 0,
        // Uses the smaller friction/bounce of the two colliding materials.
        Minimum = 2,
        // Multiplies the friction/bounce of the two colliding materials.
        Multiply = 1,
        // Uses the larger friction/bounce of the two colliding materials.
        Maximum = 3
    }

    // Describes collision.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class Collision
    {
        internal Vector3 m_Impulse;
        internal Vector3 m_RelativeVelocity;
        internal Rigidbody m_Rigidbody;
        internal Collider m_Collider;

        internal ContactPoint[] m_Contacts;

        // The relative linear velocity of the two colliding objects (RO).
        public Vector3 relativeVelocity { get { return m_RelativeVelocity; } }

        // The [[Rigidbody]] we hit (RO). This is /null/ if the object we hit is a collider with no rigidbody attached.
        public Rigidbody rigidbody { get { return m_Rigidbody; } }

        // The [[Collider]] we hit (RO).
        public Collider collider { get { return m_Collider; } }

        // The [[Transform]] of the object we hit (RO).
        public Transform transform { get { return rigidbody != null ? rigidbody.transform : collider.transform; } }

        // The [[GameObject]] whose collider we are colliding with. (RO).
        public GameObject gameObject { get { return m_Rigidbody != null ? m_Rigidbody.gameObject : m_Collider.gameObject; } }

        // The contact points generated by the physics engine.
        public ContactPoint[] contacts { get { return m_Contacts; } }

        //*undocumented*
        public virtual IEnumerator GetEnumerator()
        {
            return contacts.GetEnumerator();
        }

        public Vector3 impulse { get { return m_Impulse; }}

        //*undocumented* DEPRECATED
        [Obsolete("Use Collision.relativeVelocity instead.", false)]
        public Vector3 impactForceSum { get { return relativeVelocity; } }

        //*undocumented* DEPRECATED
        [Obsolete("Will always return zero.", false)]
        public Vector3 frictionForceSum { get { return Vector3.zero; } }

        [Obsolete("Please use Collision.rigidbody, Collision.transform or Collision.collider instead", false)]
        public Component other { get { return m_Rigidbody != null ? (Component)m_Rigidbody : (Component)m_Collider; } }
    }

    // CollisionFlags is a bitmask returned by CharacterController.Move.
    public enum CollisionFlags
    {
        // CollisionFlags is a bitmask returned by CharacterController.Move.
        None = 0,

        // CollisionFlags is a bitmask returned by CharacterController.Move.
        Sides = 1,

        // CollisionFlags is a bitmask returned by CharacterController.Move.
        Above = 2,

        // CollisionFlags is a bitmask returned by CharacterController.Move.
        Below = 4,

        //*undocumented
        CollidedSides = 1,
        //*undocumented
        CollidedAbove = 2,
        //*undocumented
        CollidedBelow = 4
    }

    public enum QueryTriggerInteraction
    {
        UseGlobal = 0,
        Ignore = 1,
        Collide = 2
    }
} // namespace

