// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Use these flags to constrain motion of Rigidbodies.
    public enum RigidbodyConstraints
    {
        None = 0,
        FreezePositionX = 0x02,
        FreezePositionY = 0x04,
        FreezePositionZ = 0x08,
        FreezeRotationX = 0x10,
        FreezeRotationY = 0x20,
        FreezeRotationZ = 0x40,
        FreezePosition = 0x0e,
        FreezeRotation = 0x70,
        FreezeAll = 0x7e,
    }

    // Option for how to apply a force using Rigidbody.AddForce.
    public enum ForceMode
    {
        Force = 0,
        Acceleration = 5,
        Impulse = 1,
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
        None = 0,
        PositionAndRotation = 1,

        // Snap Position only
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("JointProjectionMode.PositionOnly is no longer supported", true)]
        PositionOnly = 2
    }

    [Flags]
    public enum MeshColliderCookingOptions
    {
        None,
        [Obsolete("No longer used because the problem this was trying to solve is gone since Unity 2018.3", true)] InflateConvexMesh = 1 << 0,
        CookForFasterSimulation = 1 << 1,
        EnableMeshCleaning = 1 << 2,
        WeldColocatedVertices = 1 << 3,
        UseFastMidphase = 1 << 4
    }

    // WheelFrictionCurve is used by the WheelCollider to describe friction properties of the wheel tire.
    public struct WheelFrictionCurve
    {
        private float m_ExtremumSlip;
        private float m_ExtremumValue;
        private float m_AsymptoteSlip;
        private float m_AsymptoteValue;
        private float m_Stiffness;

        public float extremumSlip { get { return m_ExtremumSlip; } set { m_ExtremumSlip = value; } }
        public float extremumValue { get { return m_ExtremumValue; } set { m_ExtremumValue = value; } }
        public float asymptoteSlip { get { return m_AsymptoteSlip; } set { m_AsymptoteSlip = value; } }
        public float asymptoteValue { get { return m_AsymptoteValue; } set { m_AsymptoteValue = value; } }
        public float stiffness { get { return m_Stiffness; } set { m_Stiffness = value; } }
    }

    // The limits defined by the CharacterJoint
    public struct SoftJointLimit
    {
        private float m_Limit;
        private float m_Bounciness;
        private float m_ContactDistance;

        public float limit { get { return m_Limit; } set { m_Limit = value; } }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Spring has been moved to SoftJointLimitSpring class in Unity 5", true)]
        public float spring { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Damper has been moved to SoftJointLimitSpring class in Unity 5", true)]
        public float damper { get { return 0; } set {} }

        public float bounciness { get { return m_Bounciness; } set { m_Bounciness = value; } }
        public float contactDistance { get { return m_ContactDistance; } set { m_ContactDistance = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use SoftJointLimit.bounciness instead", true)]
        public float bouncyness { get { return m_Bounciness; } set { m_Bounciness = value; } }
    }

    public struct SoftJointLimitSpring
    {
        private float m_Spring;
        private float m_Damper;

        public float spring { get { return m_Spring; } set { m_Spring = value; } }
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

        public float positionSpring { get { return m_PositionSpring; } set { m_PositionSpring = value; } }
        public float positionDamper { get { return m_PositionDamper; } set { m_PositionDamper = value; } }
        public float maximumForce { get { return m_MaximumForce; } set { m_MaximumForce = value; } }
    }

    public enum RigidbodyInterpolation
    {
        None = 0,
        Interpolate = 1,
        Extrapolate = 2
    }

    // The JointMotor is used to motorize a joint.
    public struct JointMotor
    {
        private float m_TargetVelocity;
        private float m_Force;
        private int  m_FreeSpin;

        public float targetVelocity { get { return m_TargetVelocity; } set { m_TargetVelocity = value; } }
        public float force { get { return m_Force; } set { m_Force = value; } }
        public bool  freeSpin { get { return m_FreeSpin == 1; } set { m_FreeSpin = value ? 1 : 0; } }
    }

    // JointSpring is used add a spring force to HingeJoint and PhysicMaterial.
    public struct JointSpring
    {
        public float spring;
        public float damper;
        public float targetPosition;

        // We have to keep those as public variables because of a bug in the C# raycast sample.
    }

    // JointLimits is used by the HingeJoint to limit the joints angle.
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

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("minBounce and maxBounce are replaced by a single JointLimits.bounciness for both limit ends.", true)]
        public float minBounce;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

        public CharacterController controller { get { return m_Controller; } }
        public Collider collider { get { return m_Collider; } }
        public Rigidbody rigidbody { get { return m_Collider.attachedRigidbody; } }
        public GameObject gameObject { get { return m_Collider.gameObject; } }
        public Transform transform { get { return m_Collider.transform; } }
        public Vector3 point { get { return m_Point; } }
        public Vector3 normal { get { return m_Normal; } }
        public Vector3 moveDirection { get { return m_MoveDirection; } }
        public float moveLength { get { return m_MoveLength; } }
        private bool push { get { return m_Push != 0; } set { m_Push = value ? 1 : 0; } }
    }

    // Describes how physics materials of colliding objects are combined.
    public enum PhysicMaterialCombine
    {
        Average = 0,
        Minimum = 2,
        Multiply = 1,
        Maximum = 3
    }

    // Describes collision.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public class Collision
    {
        internal Vector3 m_Impulse;
        internal Vector3 m_RelativeVelocity;
        internal Component m_Body;
        internal Collider m_Collider;
        internal int m_ContactCount;
        internal ContactPoint[] m_ReusedContacts;
        internal ContactPoint[] m_LegacyContacts;

        // Return the appropriate contacts array.
        private ContactPoint[] GetContacts_Internal() { return m_LegacyContacts == null ? m_ReusedContacts : m_LegacyContacts; }
        public Vector3 relativeVelocity { get { return m_RelativeVelocity; } }
        public Rigidbody rigidbody { get { return m_Body as Rigidbody; } }
        public ArticulationBody articulationBody { get { return m_Body as ArticulationBody; } }
        public Component body { get { return m_Body; } }
        public Collider collider { get { return m_Collider; } }
        public Transform transform { get { return rigidbody != null ? rigidbody.transform : collider.transform; } }
        public GameObject gameObject { get { return m_Body != null ? m_Body.gameObject : m_Collider.gameObject; } }

        // The number of contacts available.
        public int contactCount { get { return m_ContactCount; } }

        // The contact points generated by the physics engine.
        // NOTE: This produces garbage and should be avoided.
        public ContactPoint[] contacts
        {
            get
            {
                if (m_LegacyContacts == null)
                {
                    m_LegacyContacts = new ContactPoint[m_ContactCount];
                    Array.Copy(m_ReusedContacts, m_LegacyContacts, m_ContactCount);
                }

                return m_LegacyContacts;
            }
        }

        // Get contact at specific index.
        public ContactPoint GetContact(int index)
        {
            if (index < 0 || index >= m_ContactCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get contact at index {0}. There are {1} contact(s).", index, m_ContactCount));

            return GetContacts_Internal()[index];
        }

        // Get contacts for this collision.
        public int GetContacts(ContactPoint[] contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided array is NULL.");

            var contactCount = Mathf.Min(m_ContactCount, contacts.Length);
            Array.Copy(GetContacts_Internal(), contacts, contactCount);
            return contactCount;
        }

        // Get contacts for this collision.
        public int GetContacts(List<ContactPoint> contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided list is NULL.");

            contacts.Clear();
            contacts.AddRange(GetContacts_Internal());
            return contactCount;
        }

        //*undocumented*
        [Obsolete("Do not use Collision.GetEnumerator(), enumerate using non-allocating array returned by Collision.GetContacts() or enumerate using Collision.GetContact(index) instead.", false)]
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
        public Component other { get { return m_Body != null ? (Component)m_Body : (Component)m_Collider; } }
    }

    // CollisionFlags is a bitmask returned by CharacterController.Move.
    public enum CollisionFlags
    {
        None = 0,
        Sides = 1,
        Above = 2,
        Below = 4,
        CollidedSides = 1,
        CollidedAbove = 2,
        CollidedBelow = 4
    }

    public enum QueryTriggerInteraction
    {
        UseGlobal = 0,
        Ignore = 1,
        Collide = 2
    }

    public enum CollisionDetectionMode
    {
        Discrete = 0,
        Continuous = 1,
        ContinuousDynamic = 2,
        ContinuousSpeculative = 3
    }

    public enum ConfigurableJointMotion
    {
        Locked = 0,
        Limited = 1,
        Free = 2
    }

    public enum RotationDriveMode
    {
        XYAndZ = 0,
        Slerp = 1
    }
}
