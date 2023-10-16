// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;
using Unity.Collections;

namespace UnityEngine
{
    // Use these flags to constrain motion of Rigidbodies.
    public enum RigidbodyConstraints
    {
        None = 0,
        FreezePositionX = 1 << 1,
        FreezePositionY = 1 << 2,
        FreezePositionZ = 1 << 3,
        FreezeRotationX = 1 << 4,
        FreezeRotationY = 1 << 5,
        FreezeRotationZ = 1 << 6,
        FreezePosition = FreezePositionX | FreezePositionY | FreezePositionZ,
        FreezeRotation = FreezeRotationX | FreezeRotationY | FreezeRotationZ,
        FreezeAll = FreezePosition | FreezeRotation
    }

    // Option for how to apply a force using Rigidbody.AddForce.
    public enum ForceMode
    {
        Force = 0,
        Acceleration = 5,
        Impulse = 1,
        VelocityChange = 2,
    }

    // Determines how to snap physics joints back to its constrained position when it drifts off too much. Note: PositionOnly is not supported anymore!
    // TODO: We should just move to a flag and remove this enum
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
    public partial struct SoftJointLimit
    {
        private float m_Limit;
        private float m_Bounciness;
        private float m_ContactDistance;

        public float limit { get { return m_Limit; } set { m_Limit = value; } }
        public float bounciness { get { return m_Bounciness; } set { m_Bounciness = value; } }
        public float contactDistance { get { return m_ContactDistance; } set { m_ContactDistance = value; } }
    }

    public struct SoftJointLimitSpring
    {
        private float m_Spring;
        private float m_Damper;

        public float spring { get { return m_Spring; } set { m_Spring = value; } }
        public float damper { get { return m_Damper; } set { m_Damper = value; } }
    }

    // How the joint's movement will behave along its local X axis
    public partial struct JointDrive
    {
        private float m_PositionSpring;
        private float m_PositionDamper;
        private float m_MaximumForce;
        private int m_UseAcceleration;

        public float positionSpring { get { return m_PositionSpring; } set { m_PositionSpring = value; } }
        public float positionDamper { get { return m_PositionDamper; } set { m_PositionDamper = value; } }
        public float maximumForce { get { return m_MaximumForce; } set { m_MaximumForce = value; } }
        public bool useAcceleration { get { return m_UseAcceleration == 1; } set { m_UseAcceleration = value ? 1 : 0; } }
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

    // JointSpring is used add a spring force to HingeJoint and PhysicsMaterial.
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

        // NB - member fields can't be in other partial structs, so we cannot move this out; work out a plan to remove them then
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

    // Describes collision.
    public partial class Collision
    {
        private ContactPairHeader m_Header;
        private ContactPair m_Pair;
        private bool m_Flipped;
        private ContactPoint[] m_LegacyContacts = null;

        public Vector3 impulse => m_Pair.impulseSum;
        public Vector3 relativeVelocity => m_Flipped ? m_Header.m_RelativeVelocity : -m_Header.m_RelativeVelocity;
        public Rigidbody rigidbody => body as Rigidbody;
        public ArticulationBody articulationBody => body as ArticulationBody;
        public Component body => m_Flipped ? m_Header.body : m_Header.otherBody;
        public Collider collider => m_Flipped ? m_Pair.collider : m_Pair.otherCollider;
        public Transform transform { get { return rigidbody != null ? rigidbody.transform : collider.transform; } }
        public GameObject gameObject { get { return body != null ? body.gameObject : collider.gameObject; } }
        internal bool Flipped { get { return m_Flipped; } set { m_Flipped = value; } }

        // The number of contacts available.
        public int contactCount { get { return (int)m_Pair.m_NbPoints; } }

        // The contact points generated by the physics engine.
        // NOTE: This produces garbage and should be avoided.
        public ContactPoint[] contacts
        {
            get
            {
                if (m_LegacyContacts == null)
                {
                    m_LegacyContacts = new ContactPoint[m_Pair.m_NbPoints];
                    m_Pair.ExtractContactsArray(m_LegacyContacts, m_Flipped);
                }

                return m_LegacyContacts;
            }
        }

        public Collision()
        {
            m_Header = new ContactPairHeader();
            m_Pair = new ContactPair();
            m_Flipped = false;

            m_LegacyContacts = null;
        }

        // Assumes we are NOT in the reusing mode
        internal Collision(in ContactPairHeader header, in ContactPair pair, bool flipped)
        {
            m_LegacyContacts = new ContactPoint[pair.m_NbPoints];
            pair.ExtractContactsArray(m_LegacyContacts, flipped);
            m_Header = header;
            m_Pair = pair;
            m_Flipped = flipped;
        }

        // Assumes we are in the reusing mode
        internal void Reuse(in ContactPairHeader header, in ContactPair pair)
        {
            m_Header = header;
            m_Pair = pair;
            m_LegacyContacts = null;
            m_Flipped = false;
        }

        // Get contact at specific index.
        public unsafe ContactPoint GetContact(int index)
        {
            if (index < 0 || index >= contactCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get contact at index {0}. There are {1} contact(s).", index, contactCount));

            if (m_LegacyContacts != null)
                return m_LegacyContacts[index];

            float sign = m_Flipped ? -1f : 1f;
            var ptr = m_Pair.GetContactPoint_Internal(index);

            return new ContactPoint(
                    ptr->m_Position,
                    ptr->m_Normal * sign,
                    ptr->m_Impulse,
                    ptr->m_Separation,
                    m_Flipped ? m_Pair.otherColliderInstanceID : m_Pair.colliderInstanceID,
                    m_Flipped ? m_Pair.colliderInstanceID : m_Pair.otherColliderInstanceID);
        }

        // Get contacts for this collision.
        public int GetContacts(ContactPoint[] contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided array is NULL.");

            if(m_LegacyContacts != null)
            {
                int length = Mathf.Min(m_LegacyContacts.Length, contacts.Length);
                Array.Copy(m_LegacyContacts, contacts, length);
                return length;
            }

            return m_Pair.ExtractContactsArray(contacts, m_Flipped);
        }

        // Get contacts for this collision.
        public int GetContacts(List<ContactPoint> contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided list is NULL.");

            contacts.Clear();

            if(m_LegacyContacts != null)
            {
                contacts.AddRange(m_LegacyContacts);
                return m_LegacyContacts.Length;
            }

            int n = (int)m_Pair.m_NbPoints;

            if (n == 0)
                return 0;

            if (contacts.Capacity < n) // Resize here instead of in native
                contacts.Capacity = n;

            return m_Pair.ExtractContacts(contacts, m_Flipped);
        }
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
