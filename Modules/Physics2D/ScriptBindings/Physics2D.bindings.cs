// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;


namespace UnityEngine
{
    [NativeHeader("Modules/Physics2D/PhysicsManager2D.h")]
    [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
    public partial class Physics2D
    {
        #region Global Physics Settings

        public const int IgnoreRaycastLayer = 1 << 2;
        public const int DefaultRaycastLayers = ~Physics2D.IgnoreRaycastLayer;
        public const int AllLayers = ~0;

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static int velocityIterations { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static int positionIterations { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static Vector2 gravity { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool queriesHitTriggers { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool queriesStartInColliders { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool changeStopsCallbacks { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool callbacksOnDisable { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool autoSyncTransforms { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool autoSimulation { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float velocityThreshold { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float maxLinearCorrection { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float maxAngularCorrection { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float maxTranslationSpeed { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float maxRotationSpeed { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float defaultContactOffset { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float baumgarteScale { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float baumgarteTOIScale { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float timeToSleep { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float linearSleepTolerance { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float angularSleepTolerance { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool alwaysShowColliders { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool showColliderSleep { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool showColliderContacts { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool showColliderAABB { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float contactArrowScale { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static Color colliderAwakeColor { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static Color colliderAsleepColor { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static Color colliderContactColor { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static Color colliderAABBColor { get; set; }

        #endregion

        #region Simulation

        // Perform a manual simulation step.
        [NativeMethod("Simulate_Binding")]
        extern public static bool Simulate(float step);

        // Sync transform changes.
        extern public static void SyncTransforms();

        #endregion

        #region Collisions, Contacts and Queries.

        // Ignore collisions between specific colliders.
        public static void IgnoreCollision([Writable] Collider2D collider1, [Writable] Collider2D collider2) { IgnoreCollision(collider1, collider2, true); }
        extern public static void IgnoreCollision([NotNull][Writable] Collider2D collider1, [NotNull][Writable] Collider2D collider2, [DefaultValue("true")] bool ignore);

        // Get whether collisions between specific colliders are ignored or not.
        extern public static bool GetIgnoreCollision([Writable] Collider2D collider1, [Writable] Collider2D collider2);

        // Ignore collisions between specific layers.
        public static void IgnoreLayerCollision(int layer1, int layer2) { IgnoreLayerCollision(layer1, layer2, true); }
        public static void IgnoreLayerCollision(int layer1, int layer2, bool ignore)
        {
            if (layer1 < 0 || layer1 > 31)
                throw new ArgumentOutOfRangeException("layer1 is out of range. Layer numbers must be in the range 0 to 31.");

            if (layer2 < 0 || layer2 > 31)
                throw new ArgumentOutOfRangeException("layer2 is out of range. Layer numbers must be in the range 0 to 31.");

            IgnoreLayerCollision_Internal(layer1, layer2, ignore);
        }

        [StaticAccessor("GetPhysics2DSettings()")]
        [NativeMethod("IgnoreLayerCollision")]
        extern private static void IgnoreLayerCollision_Internal(int layer1, int layer2, bool ignore);

        // Get whether collisions between specific layers are ignored or not.
        public static bool GetIgnoreLayerCollision(int layer1, int layer2)
        {
            if (layer1 < 0 || layer1 > 31)
                throw new ArgumentOutOfRangeException("layer1 is out of range. Layer numbers must be in the range 0 to 31.");

            if (layer2 < 0 || layer2 > 31)
                throw new ArgumentOutOfRangeException("layer2 is out of range. Layer numbers must be in the range 0 to 31.");

            return GetIgnoreLayerCollision_Internal(layer1, layer2);
        }

        [StaticAccessor("GetPhysics2DSettings()")]
        [NativeMethod("GetIgnoreLayerCollision")]
        extern private static bool GetIgnoreLayerCollision_Internal(int layer1, int layer2);

        // Set the layer collision mask for a specific layer.
        public static void SetLayerCollisionMask(int layer, int layerMask)
        {
            if (layer < 0 || layer > 31)
                throw new ArgumentOutOfRangeException("layer1 is out of range. Layer numbers must be in the range 0 to 31.");

            SetLayerCollisionMask_Internal(layer, layerMask);
        }

        [StaticAccessor("GetPhysics2DSettings()")]
        [NativeMethod("SetLayerCollisionMask")]
        extern private static void SetLayerCollisionMask_Internal(int layer, int layerMask);

        // Get the layer collision mask for a specific layer.
        public static int GetLayerCollisionMask(int layer)
        {
            if (layer < 0 || layer > 31)
                throw new ArgumentOutOfRangeException("layer1 is out of range. Layer numbers must be in the range 0 to 31.");

            return GetLayerCollisionMask_Internal(layer);
        }

        [StaticAccessor("GetPhysics2DSettings()")]
        [NativeMethod("GetLayerCollisionMask")]
        extern private static int GetLayerCollisionMask_Internal(int layer);

        // Get whether specific colliders are currently touching or not.
        extern public static bool IsTouching([NotNull][Writable] Collider2D collider1, [NotNull][Writable] Collider2D collider2);

        // Get whether specific colliders are currently touching or not (using the contact filter).
        public static bool IsTouching([Writable] Collider2D collider1, [Writable] Collider2D collider2, ContactFilter2D contactFilter) { return IsTouching_TwoCollidersWithFilter(collider1, collider2, contactFilter); }
        [NativeMethod("IsTouching")]
        extern private static bool IsTouching_TwoCollidersWithFilter([NotNull][Writable] Collider2D collider1, [NotNull][Writable] Collider2D collider2, ContactFilter2D contactFilter);

        // Get whether the specific collider is touching anything (using the contact filter).
        public static bool IsTouching([Writable] Collider2D collider, ContactFilter2D contactFilter) { return IsTouching_SingleColliderWithFilter(collider, contactFilter); }
        [NativeMethod("IsTouching")]
        extern private static bool IsTouching_SingleColliderWithFilter([NotNull][Writable] Collider2D collider, ContactFilter2D contactFilter);

        // Get whether the specific collider is touching the specific layer(s).
        public static bool IsTouchingLayers([Writable] Collider2D collider) { return IsTouchingLayers(collider, Physics2D.AllLayers); }
        extern public static bool IsTouchingLayers([NotNull][Writable] Collider2D collider, [DefaultValue("Physics2D.AllLayers")] int layerMask);

        // Get the shortest distance and the respective points between two colliders.
        public static ColliderDistance2D Distance([Writable] Collider2D colliderA, [Writable] Collider2D colliderB)
        {
            if (colliderA == null)
                throw new ArgumentNullException("ColliderA cannot be NULL.");

            if (colliderB == null)
                throw new ArgumentNullException("ColliderB cannot be NULL.");

            if (colliderA == colliderB)
                throw new ArgumentException("Cannot calculate the distance between the same collider.");

            return Distance_Internal(colliderA, colliderB);
        }

        [StaticAccessor("GetPhysicsQuery2D()", StaticAccessorType.Arrow)]
        [NativeMethod("Distance")]
        extern private static ColliderDistance2D Distance_Internal([NotNull][Writable] Collider2D colliderA, [NotNull][Writable] Collider2D colliderB);

        #endregion

        #region Editor

        private static List<Rigidbody2D> m_LastDisabledRigidbody2D = new List<Rigidbody2D>();
        internal static void SetEditorDragMovement(bool dragging, GameObject[] objs)
        {
            // Reset drag behaviour for all previously dragged bodies.
            foreach (var body in m_LastDisabledRigidbody2D)
            {
                if (body != null)
                    body.SetDragBehaviour(false);
            }
            m_LastDisabledRigidbody2D.Clear();

            // If we're not dragging then the work is already done.
            if (!dragging)
                return;

            // Set all bodies drag behaviour.
            foreach (var obj in objs)
            {
                var bodyComponents = obj.GetComponentsInChildren<Rigidbody2D>(false);
                foreach (var body in bodyComponents)
                {
                    m_LastDisabledRigidbody2D.Add(body);
                    body.SetDragBehaviour(true);
                }
            }
        }

        #endregion
    }

    #region Enums

    public enum CapsuleDirection2D
    {
        // Vertical (radii top/bottom)
        Vertical = 0,

        // Horizontal (radii left/right)
        Horizontal = 1
    }

    [Flags]
    public enum RigidbodyConstraints2D
    {
        // No constraints
        None = 0,

        // Freeze motion along the X-axis.
        FreezePositionX = 1 << 0,

        // Freeze motion along the Y-axis.
        FreezePositionY = 1 << 1,

        // Freeze rotation along the Z-axis.
        FreezeRotation = 1 << 2,

        // Freeze motion along all axes.
        FreezePosition = FreezePositionX | FreezePositionY,

        // Freeze rotation and motion along all axes.
        FreezeAll = FreezePosition | FreezeRotation,
    }

    public enum RigidbodyInterpolation2D
    {
        // No Interpolation.
        None = 0,

        // Interpolation will always lag a little bit behind but can be smoother than extrapolation.
        Interpolate = 1,

        // Extrapolation will predict the position of the rigidbody based on the current velocity.
        Extrapolate = 2
    }

    public enum RigidbodySleepMode2D
    {
        // Never sleep.
        NeverSleep = 0,

        // Start the rigid body awake.
        StartAwake = 1,

        // Start the rigid body asleep.
        StartAsleep = 2
    }

    public enum CollisionDetectionMode2D
    {
        // Obsolete.  Use Discrete instead.
        [Obsolete("Enum member CollisionDetectionMode2D.None has been deprecated. Use CollisionDetectionMode2D.Discrete instead (UnityUpgradable) -> Discrete", true)]
        None = 0,

        // Bodies move but may cause colliders to pass through other colliders at higher speeds but is much faster to calculate than continuous mode.
        Discrete = 0,

        // Provides the most accurate collision detection to prevent colliders passing through other colliders at higher speeds but is much more expensive to calculate.
        Continuous = 1
    }

    public enum RigidbodyType2D
    {
        // Dynamic body.
        Dynamic = 0,

        // Kinematic body.
        Kinematic = 1,

        // Static body.
        Static = 2,
    }

    public enum ForceMode2D
    {
        // Add a force to the rigidbody, using its mass.
        Force = 0,

        // Add an instant velocity change (impulse) to the rigidbody, using its mass.
        Impulse = 1,
    }

    internal enum ColliderErrorState2D
    {
        // No errors were encountered when creating the collider.
        None = 0,

        // No shapes were generated when creating the collider.
        NoShapes = 1,

        // Some shapes were removed when creating the collider.
        RemovedShapes = 2
    }

    public enum JointLimitState2D
    {
        // No limit set.
        Inactive = 0,

        // At the lower limit.
        LowerLimit = 1,

        // At the upper limit.
        UpperLimit = 2,

        // At both lower and upper limits (they are identical).
        EqualLimits = 3,
    }

    // Selects source and targets to be used by an Effector2D.
    public enum EffectorSelection2D
    {
        // Rigid-body (refers to the rigid-body center-of-mass).
        Rigidbody = 0,

        // Collider (refers to the centroid of the AABB defined by the collider).
        Collider = 1,
    }


    // The mode used to apply the [[Effector2D]] force.
    public enum EffectorForceMode2D
    {
        // Force is applied at a constant rate.
        Constant = 0,

        // Force is applied inverse-linear relative to a point.
        InverseLinear = 1,

        // Force is applied inverse-squared relative to a point.
        InverseSquared = 2,
    }

    #endregion

    #region Structures

    // Represents the closest points and distance between two colliders.
    [StructLayout(LayoutKind.Sequential)]
    public struct ColliderDistance2D
    {
        private Vector2 m_PointA;
        private Vector2 m_PointB;
        private Vector2 m_Normal;
        private float m_Distance;
        private int m_IsValid;

        // The closest points between the colliders.
        public Vector2 pointA { get { return m_PointA; } set { m_PointA = value; } }
        public Vector2 pointB { get { return m_PointB; } set { m_PointB = value; } }

        // The normal with respect to point A.
        public Vector2 normal { get { return m_Normal; } }

        // Distance between the colliders.
        public float distance { get { return m_Distance; } set { m_Distance = value; } }

        // Gets whether the distance is overlapped or not.
        public bool isOverlapped { get { return m_Distance < 0.0f; } }

        // Gets/Sets whether the distance is valid or not.
        public bool isValid { get { return m_IsValid != 0; } set { m_IsValid = value ? 1 : 0; } }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ContactFilter2D
    {
        public ContactFilter2D NoFilter()
        {
            useTriggers = true;

            useLayerMask = false;
            layerMask = Physics2D.AllLayers;

            useDepth = false;
            useOutsideDepth = false;
            minDepth = -Mathf.Infinity;
            maxDepth = Mathf.Infinity;

            useNormalAngle = false;
            useOutsideNormalAngle = false;
            minNormalAngle = 0.0f;
            maxNormalAngle = NormalAngleUpperLimit;

            return this;
        }

        private void CheckConsistency()
        {
            // Clamp depth-range bounds specified as +- infinity to real values.
            minDepth = (minDepth == -Mathf.Infinity || minDepth == Mathf.Infinity || Single.IsNaN(minDepth)) ? Single.MinValue : minDepth;
            maxDepth = (maxDepth == -Mathf.Infinity || maxDepth == Mathf.Infinity || Single.IsNaN(maxDepth)) ? Single.MaxValue : maxDepth;
            if (minDepth > maxDepth)
            {
                var temp = minDepth; minDepth = maxDepth; maxDepth = temp;
            }

            // Clamp normal-range bounds specified as +- infinity to real values.
            minNormalAngle = Single.IsNaN(minNormalAngle) ? 0.0f : Mathf.Clamp(minNormalAngle, 0.0f, NormalAngleUpperLimit);
            maxNormalAngle = Single.IsNaN(maxNormalAngle) ? NormalAngleUpperLimit : Mathf.Clamp(maxNormalAngle, 0.0f, NormalAngleUpperLimit);
            if (minNormalAngle > maxNormalAngle)
            {
                var temp = minNormalAngle; minNormalAngle = maxNormalAngle; maxNormalAngle = temp;
            }
        }

        public void ClearLayerMask() { useLayerMask = false; }
        public void SetLayerMask(LayerMask layerMask) { this.layerMask = layerMask; useLayerMask = true; }

        public void ClearDepth() { useDepth = false; }
        public void SetDepth(float minDepth, float maxDepth)
        {
            this.minDepth = minDepth;
            this.maxDepth = maxDepth;
            useDepth = true;
            CheckConsistency();
        }

        public void ClearNormalAngle() { useNormalAngle = false; }
        public void SetNormalAngle(float minNormalAngle, float maxNormalAngle)
        {
            this.minNormalAngle = minNormalAngle;
            this.maxNormalAngle = maxNormalAngle;
            useNormalAngle = true;
            CheckConsistency();
        }

        public bool isFiltering { get { return !useTriggers || useLayerMask || useDepth || useNormalAngle; } }
        public bool IsFilteringTrigger([Writable] Collider2D collider) { return !useTriggers && collider.isTrigger; }
        public bool IsFilteringLayerMask(GameObject obj) { return useLayerMask && ((layerMask & (1 << obj.layer)) == 0); }

        public bool IsFilteringDepth(GameObject obj)
        {
            if (!useDepth)
                return false;

            if (minDepth > maxDepth)
            {
                var temp = minDepth; minDepth = maxDepth; maxDepth = temp;
            }

            var depth = obj.transform.position.z;

            var result = depth<minDepth || depth> maxDepth;
            if (useOutsideDepth)
                return !result;

            return result;
        }

        public bool IsFilteringNormalAngle(Vector2 normal)
        {
            var angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
            return IsFilteringNormalAngle(angle);
        }

        public bool IsFilteringNormalAngle(float angle)
        {
            angle -= Mathf.Floor(angle / NormalAngleUpperLimit) * NormalAngleUpperLimit;
            var minRange = Mathf.Clamp(minNormalAngle, 0.0f, NormalAngleUpperLimit);
            var maxRange = Mathf.Clamp(maxNormalAngle, 0.0f, NormalAngleUpperLimit);
            if (minRange > maxRange)
            {
                var temp = minRange; minRange = maxRange; maxRange = temp;
            }

            var result = angle<minRange || angle> maxRange;
            if (useOutsideNormalAngle)
                return !result;

            return result;
        }

        [NativeName("m_UseTriggers")]
        public bool useTriggers;
        [NativeName("m_UseLayerMask")]
        public bool useLayerMask;
        [NativeName("m_UseDepth")]
        public bool useDepth;
        [NativeName("m_UseOutsideDepth")]
        public bool useOutsideDepth;
        [NativeName("m_UseNormalAngle")]
        public bool useNormalAngle;
        [NativeName("m_UseOutsideNormalAngle")]
        public bool useOutsideNormalAngle;
        [NativeName("m_LayerMask")]
        public LayerMask layerMask;
        [NativeName("m_MinDepth")]
        public float minDepth;
        [NativeName("m_MaxDepth")]
        public float maxDepth;
        [NativeName("m_MinNormalAngle")]
        public float minNormalAngle;
        [NativeName("m_MaxNormalAngle")]
        public float maxNormalAngle;

        public const float NormalAngleUpperLimit = 359.9999f;

        // This can be removed once all the legacy calls that use this filter are eventually deprecated and removed.
        static internal ContactFilter2D CreateLegacyFilter(int layerMask, float minDepth, float maxDepth)
        {
            var contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            contactFilter.SetLayerMask(layerMask);
            contactFilter.SetDepth(minDepth, maxDepth);
            return contactFilter;
        }
    }

    // Describes a contact point where the collision occurs.
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ContactPoint2D
    {
        private Vector2 m_Point;
        private Vector2 m_Normal;
        private Vector2 m_RelativeVelocity;
        private float m_Separation;
        private float m_NormalImpulse;
        private float m_TangentImpulse;
        private int m_Collider;
        private int m_OtherCollider;
        private int m_Rigidbody;
        private int m_OtherRigidbody;
        private int m_Enabled;

        // The point of contact.
        public Vector2 point  { get { return m_Point; } }

        // Normal of the contact point.
        public Vector2 normal { get { return m_Normal; } }

        // Separation of colliders at the intersection point (negative means overlap).
        public float separation { get { return m_Separation; } }

        // The impulse applied by the solver along the contact normal.
        public float normalImpulse { get { return m_NormalImpulse; } }

        // The impulse applied by the solver along the contact normal tangent.
        public float tangentImpulse { get { return m_TangentImpulse; } }

        // The relative velocity between the two colliders at the contact point.
        public Vector2 relativeVelocity { get { return m_RelativeVelocity; } }

        // The first collider in contact.
        public Collider2D collider { get { return Physics2D.GetColliderFromInstanceID(m_Collider); } }

        // The other collider in contact.
        public Collider2D otherCollider { get { return Physics2D.GetColliderFromInstanceID(m_OtherCollider); } }

        // The rigid-body involved in the collision.
        public Rigidbody2D rigidbody { get { return Physics2D.GetRigidbodyFromInstanceID(m_Rigidbody); } }

        // The other rigid-body involved in the collision.
        public Rigidbody2D otherRigidbody { get { return Physics2D.GetRigidbodyFromInstanceID(m_OtherRigidbody); } }

        // Whether the contact is enabled or not.  Effectors can temporarily disable a contact but all contact are reported.
        public bool enabled { get { return m_Enabled == 1; } }
    }

    // JointAngleLimits2D is used by the HingeJoint2D to limit the joints angle.
    [StructLayout(LayoutKind.Sequential)]
    public struct JointAngleLimits2D
    {
        private float m_LowerAngle;
        private float m_UpperAngle;

        // The lower angle limit of the joint.
        public float min { get { return m_LowerAngle; } set { m_LowerAngle = value; } }

        // The upper angle limit of the joint.
        public float max { get { return m_UpperAngle; } set { m_UpperAngle = value; } }
    }


    // JointTranslationLimits2D is used by the SliderJoint2D to limit the joints translation.
    [StructLayout(LayoutKind.Sequential)]
    public struct JointTranslationLimits2D
    {
        private float m_LowerTranslation;
        private float m_UpperTranslation;

        // The lower translation limit of the joint.
        public float min { get { return m_LowerTranslation; } set { m_LowerTranslation = value; } }

        // The upper translation limit of the joint.
        public float max { get { return m_UpperTranslation; } set { m_UpperTranslation = value; } }
    }


    // JointMotor2D is used by the HingeJoint2D, SliderJoint2D and WheelJoint2D to motorize a joint.
    [StructLayout(LayoutKind.Sequential)]
    public struct JointMotor2D
    {
        private float m_MotorSpeed;
        private float m_MaximumMotorTorque;

        // The target motor speed in degrees/second.
        public float motorSpeed { get { return m_MotorSpeed; } set { m_MotorSpeed = value; } }

        // The maximum torque in N-m the motor can use to achieve the desired motor speed.
        public float maxMotorTorque { get { return m_MaximumMotorTorque; } set { m_MaximumMotorTorque = value; } }
    }


    // JointSuspension2D is used by the WheelJoint2D to add suspension to a joint.
    [StructLayout(LayoutKind.Sequential)]
    public struct JointSuspension2D
    {
        private float m_DampingRatio;
        private float m_Frequency;
        private float m_Angle;

        // The damping ratio for the oscillation of the suspension.  0 means no damping.  1 means critical damping.  range { 0.0, 1.0 }
        public float dampingRatio { get { return m_DampingRatio; } set { m_DampingRatio = value; } }

        // The frequency in Hertz for the oscillation of the suspension.  range { 0.0, infinity }
        public float frequency { get { return m_Frequency; } set { m_Frequency = value; } }

        // The local movement angle for the suspension.
        public float angle { get { return m_Angle; } set { m_Angle = value; } }
    }

    // NOTE: must match memory layout of native RaycastHit2D
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct RaycastHit2D
    {
        private Vector2 m_Centroid;
        private Vector2 m_Point;
        private Vector2 m_Normal;
        private float m_Distance;
        private float m_Fraction;
        private Collider2D m_Collider;

        public Vector2 centroid
        {
            get { return m_Centroid; }
            set { m_Centroid = value; }
        }

        public Vector2 point
        {
            get { return m_Point; }
            set { m_Point = value; }
        }

        public Vector2 normal
        {
            get { return m_Normal; }
            set { m_Normal = value; }
        }

        public float distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }

        public float fraction
        {
            get { return m_Fraction; }
            set { m_Fraction = value; }
        }

        public Collider2D collider
        {
            get { return m_Collider; }
        }


        public Rigidbody2D rigidbody
        {
            get { return collider != null ? collider.attachedRigidbody : null; }
        }

        public Transform transform
        {
            get
            {
                Rigidbody2D body = rigidbody;
                if (body != null)
                    return body.transform;
                else if (collider != null)
                    return collider.transform;
                else
                    return null;
            }
        }

        // Implicitly convert a hit to a boolean based upon whether a collider reference exists or not.
        public static implicit operator bool(RaycastHit2D hit)
        {
            return hit.collider != null;
        }

        // Compare the hit by fraction along the ray.  If no colliders exist then fraction is moved "up".  This allows sorting an array of sparse results.
        public int CompareTo(RaycastHit2D other)
        {
            if (collider == null) return 1;
            if (other.collider == null) return -1;
            return fraction.CompareTo(other.fraction);
        }
    }

    #endregion

    #region Rigidbody Components

    [NativeHeader("Modules/Physics2D/Public/Rigidbody2D.h")]
    [RequireComponent(typeof(Transform))]
    public sealed partial class Rigidbody2D : Component
    {
        // The position of the rigidbody.
        extern public Vector2 position { get; set; }

        // The rotation of the rigidbody.
        extern public float rotation { get; set; }

        // Moves the rigidbody to /position/ during the next fixed update.
        extern public void MovePosition(Vector2 position);

        // Rotates the rigidbody to /angle/ during the next fixed update.
        extern public void MoveRotation(float angle);

        // The linear velocity vector of the object.
        extern public Vector2 velocity { get; set; }

        // The angular velocity vector of the object in degrees/sec.
        extern public float angularVelocity { get; set; }

        // Whether to calculate the mass from the collider(s) density and area.
        extern public bool useAutoMass { get; set; }

        // Controls the mass of the object by adjusting the density of all colliders attached to the object.
        extern public float mass { get; set; }

        // The shared physics material of this rigidbody.
        [NativeMethod("Material")]
        extern public PhysicsMaterial2D sharedMaterial { get; set; }

        // The center of mass (defined relative in local space).
        extern public Vector2 centerOfMass { get; set; }

        // The center of mass of the rigidbody in world space (read-only).
        extern public Vector2 worldCenterOfMass { get; }

        // The rotational inertia of the rigidbody about the local origin in kg-m^2 (read-only).
        extern public float inertia { get; set; }

        // The (linear) drag of the object.
        extern public float drag { get; set; }

        // The angular drag of the object.
        extern public float angularDrag { get; set; }

        // Controls the effect of gravity on the object.
        extern public float gravityScale { get; set; }

        // Controls the rigid body type.
        extern public RigidbodyType2D bodyType
        {
            get;
            [NativeMethod("SetBodyType_Binding")]
            set;
        }

        // Used internally when dragging a rigid-body.
        extern internal void SetDragBehaviour(bool dragged);

        // Should kinematic/kinematic and kinematic/static contacts be allowed?
        extern public bool useFullKinematicContacts { get; set; }

        // This property is obsolete but will be deprecated at a later date as it's commonly used.
        // The end-user should use bodyType instead.
        public bool isKinematic { get { return bodyType == RigidbodyType2D.Kinematic; } set { bodyType = value ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic; } }

        //OBSOLETE warning
        [Obsolete("'fixedAngle' is no longer supported. Use constraints instead.", false)]
        [NativeMethod("FreezeRotation")]
        extern public bool fixedAngle { get; set; }

        // Controls whether physics will change the rotation of the object.
        extern public bool freezeRotation { get; set; }

        // Controls constrained motion and/or rotation.
        extern public RigidbodyConstraints2D constraints { get; set; }

        // Checks whether the rigid body is sleeping or not.
        extern public bool IsSleeping();

        // Checks whether the rigid body is awake or not.
        extern public bool IsAwake();

        // Sets the rigid body into a sleep state.
        extern public void Sleep();

        // Wakes the rigid from sleeping.
        [NativeMethod("Wake")]
        extern public void WakeUp();

        // Sets whether the rigid body should be simulated or not.
        extern public bool simulated
        {
            get;
            [NativeMethod("SetSimulated_Binding")]
            set;
        }

        // Interpolation allows you to smooth out the effect of running physics at a fixed rate.
        extern public RigidbodyInterpolation2D interpolation { get; set; }

        // Controls how the object sleeps.
        extern public RigidbodySleepMode2D sleepMode { get; set; }

        // The rigidbody collision detection mode.
        extern public CollisionDetectionMode2D collisionDetectionMode { get; set; }

        // Gets a count of the colliders attached to this rigidbody.
        extern public int attachedColliderCount { get; }

        // Get whether any attached collider(s) are currently touching a specific collider or not.
        extern public bool IsTouching([NotNull][Writable] Collider2D collider);

        // Get whether any attached collider(s) are currently touching a specific collider or not allowed by the contact filter.
        public bool IsTouching([Writable] Collider2D collider, ContactFilter2D contactFilter) { return IsTouching_OtherColliderWithFilter_Internal(collider, contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_OtherColliderWithFilter_Internal([NotNull][Writable] Collider2D collider, ContactFilter2D contactFilter);

        // Get whether any attached collider(s) are currently touching anything defined by the contact filter.
        public bool IsTouching(ContactFilter2D contactFilter) { return IsTouching_AnyColliderWithFilter_Internal(contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_AnyColliderWithFilter_Internal(ContactFilter2D contactFilter);

        // Get whether any attached collider(s) are touching the specific layer(s).
        public bool IsTouchingLayers() { return IsTouchingLayers(Physics2D.AllLayers); }
        extern public bool IsTouchingLayers([DefaultValue("Physics2D.AllLayers")] int layerMask);

        // Checks whether the specified point overlaps all the rigidbody collider(s) or not.
        extern public bool OverlapPoint(Vector2 point);

        // Get the shortest distance and the respective points between all colliders on this rigidbody and another collider.
        public ColliderDistance2D Distance([Writable] Collider2D collider)
        {
            if (collider == null)
                throw new ArgumentNullException("Collider cannot be null.");

            if (collider.attachedRigidbody == this)
                throw new ArgumentException("The collider cannot be attached to the Rigidbody2D being searched.");

            return Distance_Internal(collider);
        }

        [NativeMethod("Distance")]
        extern private ColliderDistance2D Distance_Internal([NotNull][Writable] Collider2D collider);

        // Adds /force/ (defined in global space) to the rigidbody center-of-mass.  No torque is therefore generated.
        public void AddForce(Vector2 force) { AddForce(force, ForceMode2D.Force); }
        extern public void AddForce(Vector2 force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Adds /relativeForce/ (defined relative in local space) to the rigidbody center-of-mass.  No torque is therefore generated.
        public void AddRelativeForce(Vector2 relativeForce) { AddRelativeForce(relativeForce, ForceMode2D.Force); }
        extern public void AddRelativeForce(Vector2 relativeForce, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Applies /force/ at /position/ (both defined in global space) to the rigidbody.  Torque therefore can be generated.
        public void AddForceAtPosition(Vector2 force, Vector2 position) { AddForceAtPosition(force, position, ForceMode2D.Force); }
        extern public void AddForceAtPosition(Vector2 force, Vector2 position, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Adds /torque/ to the rigidbody.
        public void AddTorque(float torque) { AddTorque(torque, ForceMode2D.Force); }
        extern public void AddTorque(float torque, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Converts a /point/ (defined in global space) to a point in local space.
        extern public Vector2 GetPoint(Vector2 point);

        // Converts a /relativePoint/ (defined relative in local space) to a point in global space.
        extern public Vector2 GetRelativePoint(Vector2 relativePoint);

        // Converts a /vector/ (defined in global space) to a vector in local space.
        extern public Vector2 GetVector(Vector2 vector);

        // Converts a /relativeVector/ (defined relative in local space) to a vector in global space.
        extern public Vector2 GetRelativeVector(Vector2 relativeVector);

        // The velocity of the rigidbody at the point /worldPoint/ in global space.
        extern public Vector2 GetPointVelocity(Vector2 point);

        // The velocity relative to the rigidbody at the point /relativePoint/.
        extern public Vector2 GetRelativePointVelocity(Vector2 relativePoint);
    }

    #endregion

    #region Collider Components

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics2D/Public/Collider2D.h")]
    public partial class Collider2D : Behaviour
    {
        // The density of the collider.
        extern public float density { get; set; }

        // Gets whether the collider is a trigger or not.
        extern public bool isTrigger { get; set; }

        // Whether the collider is used by an attached effector or not.
        extern public bool usedByEffector { get; set; }

        // Whether the collider can be used by an attached composite collider or not.
        extern public bool usedByComposite { get; set; }

        // Gets the attached composite.
        extern public CompositeCollider2D composite { get; }

        // The local offset of the collider geometry.
        extern public Vector2 offset { get; set; }

        // Gets the attached rigid-body if it exists.
        extern public Rigidbody2D attachedRigidbody {[NativeMethod("GetAttachedRigidbody_Binding")] get; }

        // Gets the number of shapes this collider has generated.
        extern public int shapeCount { get; }

        // The world space bounding volume of the collider.
        extern public Bounds bounds { get; }

        // Gets the collider error state indicating indicating if anything (and what) went wrong creating collision shape(s).
        extern internal ColliderErrorState2D errorState { get; }

        // Is the collider capable of being composited?
        extern internal bool compositeCapable {[NativeMethod("GetCompositeCapable_Binding")] get; }

        // The shared physics material of this collider.
        extern public PhysicsMaterial2D sharedMaterial
        {
            [NativeMethod("GetMaterial")]
            get;
            [NativeMethod("SetMaterial")]
            set;
        }

        // Gets the effective friction used by the collider.
        extern public float friction { get; }

        // Gets the effective bounciness used by the collider.
        extern public float bounciness { get; }

        // Get whether this collider is currently touching a specific collider or not.
        extern public bool IsTouching([NotNull][Writable] Collider2D collider);

        // Get whether this collider is currently touching a specific collider or not defined by the contact filter.
        public bool IsTouching([Writable] Collider2D collider, ContactFilter2D contactFilter) { return IsTouching_OtherColliderWithFilter(collider, contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_OtherColliderWithFilter([NotNull][Writable] Collider2D collider, ContactFilter2D contactFilter);

        // Get whether this collider is currently touching anything defined by the contact filter.
        public bool IsTouching(ContactFilter2D contactFilter) { return IsTouching_AnyColliderWithFilter(contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_AnyColliderWithFilter(ContactFilter2D contactFilter);

        // Get whether the specific collider is touching the specific layer(s).
        public bool IsTouchingLayers() { return IsTouchingLayers(Physics2D.AllLayers); }
        extern public bool IsTouchingLayers([DefaultValue("Physics2D.AllLayers")] int layerMask);

        // Checks whether the specified point overlaps the collider or not.
        extern public bool OverlapPoint(Vector2 point);

        // Get the shortest distance and the respective points between this collider and another.
        public ColliderDistance2D Distance([Writable] Collider2D collider)
        {
            return Physics2D.Distance(this, collider);
        }
    }

    [NativeHeader("Modules/Physics2D/Public/CircleCollider2D.h")]
    public sealed partial class CircleCollider2D : Collider2D
    {
        // The radius of the circle.
        extern public float radius { get; set; }
    }

    [NativeHeader("Modules/Physics2D/Public/CapsuleCollider2D.h")]
    public sealed partial class CapsuleCollider2D : Collider2D
    {
        // The size of the capsule.
        extern public Vector2 size { get; set; }

        // The direction of the capsule.
        extern public CapsuleDirection2D direction { get; set; }
    }

    [NativeHeader("Modules/Physics2D/Public/EdgeCollider2D.h")]
    public sealed partial class EdgeCollider2D : Collider2D
    {
        // Reset to a single horizontal edge.
        extern public void Reset();

        // The radius of the edge(s).
        extern public float edgeRadius { get; set; }

        // Get the number of edges.  This is one less than the number of points.
        extern public int edgeCount { get; }

        // Get the number of points.  This cannot be less than two which will form a single edge.
        extern public int pointCount { get; }
    }

    [NativeHeader("Modules/Physics2D/Public/BoxCollider2D.h")]
    public sealed partial class BoxCollider2D : Collider2D
    {
        // The size of the box.
        extern public Vector2 size { get; set; }

        // The radius of the edge(s).
        extern public float edgeRadius  { get; set; }

        // Get/Set auto sprite tiling.
        extern public bool autoTiling  { get; set; }
    }

    [NativeHeader("Modules/Physics2D/Public/PolygonCollider2D.h")]
    public sealed partial class PolygonCollider2D : Collider2D
    {
        // Get the number of paths.
        extern public int pathCount { get; set; }

        // Get the total number of points in all paths.
        [NativeMethod("GetPointCount")]
        extern public int GetTotalPointCount();

        // Get/Set auto sprite tiling.
        extern public bool autoTiling { get; set; }
    }

    [NativeHeader("Modules/Physics2D/Public/CompositeCollider2D.h")]
    public sealed partial class CompositeCollider2D : Collider2D
    {
        public enum GeometryType { Outlines = 0, Polygons = 1 }

        public enum GenerationType { Synchronous = 0, Manual = 1 }

        // Controls the type of geometry created by the composite.
        extern public GeometryType geometryType { get; set; }

        // Controls when the collider generation happens.
        extern public GenerationType generationType { get; set; }

        // Controls the allowed vertex distance spacing.
        extern public float vertexDistance { get; set; }

        // extern public radius of the edge(s).
        extern public float edgeRadius { get; set; }

        // Generates the geometry if using manual generation type.
        extern public void GenerateGeometry();

        // Gets the count of points in the specified path.
        public int GetPathPointCount(int index)
        {
            int maxPathIndex = pathCount - 1;
            if (index < 0 || index > maxPathIndex)
                throw new ArgumentOutOfRangeException("index", String.Format("Path index {0} must be in the range of 0 to {1}.", index, maxPathIndex));

            return GetPathPointCount_Internal(index);
        }

        [NativeMethod("GetPathPointCount_Binding")]
        extern private int GetPathPointCount_Internal(int index);

        // Get the number of paths.
        extern public int pathCount { get; }

        // Get the total number of points in all paths.
        extern public int pointCount { get; }
    }

    #endregion

    #region Joint Components

    // Joint2D is the base class for all 2D joints.
    [NativeHeader("Modules/Physics2D/Joint2D.h")]
    [RequireComponent(typeof(Transform), typeof(Rigidbody2D))]
    public partial class Joint2D : Behaviour
    {
        // Gets the attached rigid-body.
        extern public Rigidbody2D attachedRigidbody { get; }

        // A reference to another rigid-body this joint connects to.
        extern public Rigidbody2D connectedBody { get; set; }

        // Should rigid bodies connected with this joint collide?
        extern public bool enableCollision { get; set; }

        // The magnitude of the force required to break the joint.
        extern public float breakForce { get; set; }

        // The magnitude of the torque required to break the joint.
        extern public float breakTorque { get; set; }

        // Get the reaction force using the fixed time-step (Unit is Newtons).
        extern public Vector2 reactionForce {[NativeMethod("GetReactionForceFixedTime")] get; }

        // Get the reaction torque due to the joint limit using the fixed time-step (Unit is N*m).
        extern public float reactionTorque {[NativeMethod("GetReactionTorqueFixedTime")] get; }

        // Get the reaction force given the timeStep (Unit is Newtons).
        extern public Vector2 GetReactionForce(float timeStep);

        // Get the reaction torque due to the joint limit given the timeStep (Unit is N*m).
        extern public float GetReactionTorque(float timeStep);
    }

    // AnchoredJoint2D is the base class for all 2D joints that have anchor points.
    [NativeHeader("Modules/Physics2D/AnchoredJoint2D.h")]
    public partial class AnchoredJoint2D : Joint2D
    {
        // The Position of the anchor around which the joints motion is constrained.
        extern public Vector2 anchor { get; set; }

        // The Position of the anchor around which the joints motion is constrained.
        extern public Vector2 connectedAnchor { get; set; }

        // Should the connected anchor be automatically configured to match the anchor in world space?
        extern public bool autoConfigureConnectedAnchor { get; set; }
    }

    // The SpringJoint2D ensures that the two connected rigid-bodies stay at a specific distance apart using a spring system.
    [NativeHeader("Modules/Physics2D/SpringJoint2D.h")]
    public sealed class SpringJoint2D : AnchoredJoint2D
    {
        // Should the distance be automatically calculated from the relative distance between the anchor points?
        extern public bool autoConfigureDistance { get; set; }

        // The distance the joint should maintain between the two connected rigid-bodies.
        extern public float distance { get; set; }

        // The damping ratio for the oscillation whilst trying to achieve the specified distance.  0 means no damping.  1 means critical damping.  range { 0.0, 1.0 }
        extern public float dampingRatio { get; set; }

        // The frequency in Hertz for the oscillation whilst trying to achieve the specified distance.  range { 0.0, infinity }
        extern public float frequency { get; set; }
    }

    // The DistanceJoint2D ensures that the two connected rigid-bodies stay at a maximum specific distance apart.
    [NativeHeader("Modules/Physics2D/DistanceJoint2D.h")]
    public sealed class DistanceJoint2D : AnchoredJoint2D
    {
        // Should the distance be automatically calculated from the relative distance between the anchor points?
        extern public bool autoConfigureDistance { get; set; }

        // The maximum distance the joint should maintain between the two connected rigid-bodies.
        extern public float distance { get; set; }

        // Whether to maintain a maximum distance only or not.  If not then the absolute distance will be maintained instead.
        extern public bool maxDistanceOnly { get; set; }
    }

    // The FrictionJoint2D reduces the relative linear/angular velocities between two connected rigid-bodies to zero.
    [NativeHeader("Modules/Physics2D/FrictionJoint2D.h")]
    public sealed class FrictionJoint2D : AnchoredJoint2D
    {
        // The maximum force which the joint should use to adjust position.
        extern public float maxForce { get; set; }

        // The maximum torque which the joint should use to adjust rotation.
        extern public float maxTorque { get; set; }
    }

    // The HingeJoint2D constrains the two connected rigid-bodies around the anchor points not restricting the relative rotation of them.  Can be used for wheels, rollers, chains, rag-dol joints, levers etc.
    [NativeHeader("Modules/Physics2D/HingeJoint2D.h")]
    public sealed class HingeJoint2D : AnchoredJoint2D
    {
        // Setting the motor or limit automatically enabled them.

        // Enables the joint's motor.
        extern public bool useMotor { get; set; }

        // Enables the joint's limits.
        extern public bool useLimits { get; set; }

        // The motor will apply a force up to a maximum torque to achieve the target velocity in degrees per second.
        extern public JointMotor2D motor { get; set; }

        // The limits of the hinge joint.
        extern public JointAngleLimits2D limits { get; set; }

        // Get the state of the joint angle limit.
        extern public JointLimitState2D limitState { get; }

        // Get the reference angle between the two bodies (Unit is degrees).
        extern public float referenceAngle { get; }

        // Get the current joint angle (Unit is degrees).
        extern public float jointAngle { get; }

        // Get the current joint angle speed (Unit is degrees/sec).
        extern public float jointSpeed { get; }

        // Get the current motor torque force given the /timeStep/ (Unit is N*m).
        extern public float GetMotorTorque(float timeStep);
    }

    // The RelativeJoint2D ensures that the two connected rigid-bodies stay at a relative orientation.
    [NativeHeader("Modules/Physics2D/RelativeJoint2D.h")]
    public sealed class RelativeJoint2D : Joint2D
    {
        // The maximum motor force which the joint should use to adjust position.
        extern public float maxForce { get; set; }

        // The maximum motor torque which the joint should use to adjust rotation.
        extern public float maxTorque { get; set; }

        // Scales both the position and angle correction constraint such that it controls the size of the generated force/torque produced.
        extern public float correctionScale { get; set; }

        // Should the offsets be automatically calculated from the relative distance between the two rigid-bodies?
        extern public bool autoConfigureOffset { get; set; }

        // The relative linear offset between the two rigid-bodies.
        extern public Vector2 linearOffset { get; set; }

        // The relative angular offset between the two rigid-bodies.
        extern public float angularOffset { get; set; }

        // Get the target position for the relative joint.
        extern public Vector2 target { get; }
    }

    // The SliderJoint2D constrains the two connected rigid-bodies to have on degree of freedom: translation along a fixed axis.  Relative motion is prevented.
    [NativeHeader("Modules/Physics2D/SliderJoint2D.h")]
    public sealed class SliderJoint2D : AnchoredJoint2D
    {
        // Should the angle be automatically calculated from the relative angle between the anchor points?
        extern public bool autoConfigureAngle { get; set; }

        // The translation angle that the joint slides along.
        extern public float angle { get; set; }

        // Enables the joint's motor.
        extern public bool useMotor { get; set; }

        // Enables the joint's limits.
        extern public bool useLimits { get; set; }

        // The motor will apply a force up to a maximum torque to achieve the target velocity in degrees per second.
        extern public JointMotor2D motor { get; set; }

        // The limits of the slider joint.
        extern public JointTranslationLimits2D limits { get; set; }

        // Get the state of the joint translation limit.
        extern public JointLimitState2D limitState { get; }

        // Get the reference angle between the two bodies (Unit is degrees).
        extern public float referenceAngle { get; }

        // Get the current joint translation (Unit is meters).
        extern public float jointTranslation { get; }

        // Get the current joint angle speed (Unit is degrees/sec).
        extern public float jointSpeed { get; }

        // Get the current motor force given the /timeStep/ (Unit is N*m).
        extern public float GetMotorForce(float timeStep);
    }

    // The TargetJoint2D moves a rigid-body towards a specific target position.
    [NativeHeader("Modules/Physics2D/TargetJoint2D.h")]
    public sealed class TargetJoint2D : Joint2D
    {
        // The Position of the anchor around which the joints motion is constrained.
        extern public Vector2 anchor { get; set; }

        // The world-space position that the joint should move the rigid-body towards.
        extern public Vector2 target { get; set; }

        // Should the target be automatically calculated as the rigid-body position?
        extern public bool autoConfigureTarget { get; set; }

        // The maximum force which the joint should use to adjust position.
        extern public float maxForce { get; set; }

        // The damping ratio for the oscillation whilst trying to reach the target.
        extern public float dampingRatio { get; set; }

        // The frequency in Hertz for the oscillation whilst trying to reach the target.
        extern public float frequency { get; set; }
    }

    // The FixedJoint2D welds two rigid-bodies together.
    [NativeHeader("Modules/Physics2D/FixedJoint2D.h")]
    public sealed class FixedJoint2D : AnchoredJoint2D
    {
        // The damping ratio for the oscillation whilst trying to achieve the fixed constraint.
        extern public float dampingRatio { get; set; }

        // The frequency in Hertz for the rotational oscillation whilst trying to achieve the fixed constraint.
        extern public float frequency { get; set; }

        // Get the reference angle between the two bodies (Unit is degrees).
        extern public float referenceAngle { get; }
    }

    // The WheelJoint2D constrains the two connected rigid-bodies along a local suspension axis and provides a spring to act as suspension with an optional motor to drive rotation.
    [NativeHeader("Modules/Physics2D/WheelJoint2D.h")]
    public sealed class WheelJoint2D : AnchoredJoint2D
    {
        // The suspension for the joint.
        extern public JointSuspension2D suspension { get; set; }

        // Enables the joint's motor.
        extern public bool useMotor { get; set; }

        // The motor will apply a force up to a maximum torque to achieve the target velocity in degrees per second.
        extern public JointMotor2D motor { get; set; }

        // Get the current joint translation (Unit is meters).
        extern public float jointTranslation { get; }

        // Get the current joint linear speed, usually in meters per second.
        extern public float jointLinearSpeed { get; }

        // Get the current joint angle speed (Unit is degrees/sec).
        extern public float jointSpeed {[NativeMethod("GetJointAngularSpeed")] get; }

        // Get the current joint angle (Unit is degrees).
        extern public float jointAngle { get; }

        // Get the current motor torque force given the /timeStep/ (Unit is N*m).
        extern public float GetMotorTorque(float timeStep);
    }

    #endregion

    #region Effector Components

    // Base type for all 2D effectors.
    [NativeHeader("Modules/Physics2D/Effector2D.h")]
    public partial class Effector2D : Behaviour
    {
        // Should the collider mask be used or the global collision matrix?
        extern public bool useColliderMask { get; set; }

        // The mask used to select specific layers allowed to interact with the effector.
        extern public int colliderMask { get; set; }

        // Whether the effector requires a collider or not.
        extern internal bool requiresCollider { get; }

        // Whether the effector was designed to work optimally with a trigger collider.
        extern internal bool designedForTrigger { get; }

        // Whether the effector was designed to work optimally with a non-trigger collider.
        extern internal bool designedForNonTrigger { get; }
    }

    // Applies forces within an area.
    [NativeHeader("Modules/Physics2D/AreaEffector2D.h")]
    public partial class AreaEffector2D : Effector2D
    {
        // The angle of the force to be applied.
        extern public float forceAngle { get; set; }

        // Should the 'forceAngle' be a global-space or local-space angle.
        extern public bool useGlobalAngle { get; set; }

        // The magnitude of the force to be applied.
        extern public float forceMagnitude { get; set; }

        // The variation of the magnitude of the force to be applied.
        extern public float forceVariation { get; set; }

        // The linear drag to apply to rigid-bodies.
        extern public float drag { get; set; }

        // The angular drag to apply to rigid-bodies.
        extern public float angularDrag { get; set; }

        // The target for where the effector applies any force.
        extern public EffectorSelection2D forceTarget { get; set; }
    }


    // Applies buoyancy forces within an area.
    [NativeHeader("Modules/Physics2D/BuoyancyEffector2D.h")]
    public partial class BuoyancyEffector2D : Effector2D
    {
        // The local-space surface level that determines the 'surface' of the fluid.
        extern public float surfaceLevel { get; set; }

        // The density of the fluid.
        extern public float density { get; set; }

        // The linear drag when touching the fluid.
        extern public float linearDrag { get; set; }

        // The angular drag when touching the fluid.
        extern public float angularDrag { get; set; }

        // The angle of the flow force to be applied.
        extern public float flowAngle { get; set; }

        // The magnitude of the flow force to be applied
        extern public float flowMagnitude { get; set; }

        // The variation added to the magnitude of the flow to be applied.
        extern public float flowVariation { get; set; }
    }


    // Applies forces to attract/repulse against a point.
    [NativeHeader("Modules/Physics2D/PointEffector2D.h")]
    public partial class PointEffector2D : Effector2D
    {
        // The magnitude of the force to be applied.
        extern public float forceMagnitude { get; set; }

        // The variation of the magnitude of the force to be applied.
        extern public float forceVariation { get; set; }

        // The scale applied to the distance between the source and target.
        extern public float distanceScale { get; set; }

        // The linear drag to apply to rigid-bodies.
        extern public float drag { get; set; }

        // The angular drag to apply to rigid-bodies.
        extern public float angularDrag { get; set; }

        // The source for where the effector calculates any force.
        extern public EffectorSelection2D forceSource { get; set; }

        // The target for where the effector applies any force.
        extern public EffectorSelection2D forceTarget { get; set; }

        // The mode used to apply the effector force.
        extern public EffectorForceMode2D forceMode { get; set; }
    }

    // Applies "platform" behaviour such as one-way collisions etc.
    [NativeHeader("Modules/Physics2D/PlatformEffector2D.h")]
    public partial class PlatformEffector2D : Effector2D
    {
        // Whether to use one-way collision behaviour or not.
        extern public bool useOneWay { get; set; }

        // Should a contact, disabled by the one-way collision behaviour, affect all colliders attached to the effector?
        extern public bool useOneWayGrouping { get; set; }

        // Whether friction should be used on the platform sides or not.
        extern public bool useSideFriction { get; set; }

        // Whether bounce should be used on the platform sides or not.
        extern public bool useSideBounce { get; set; }

        // The angle of an arc that defines the surface of the platform center of the local 'up' of the effector.
        extern public float surfaceArc { get; set; }

        // The angle of an arc that defines the sides of the platform centered on the local 'left' and 'right' of the effector.
        extern public float sideArc { get; set; }

        // The rotational offset angle from the local 'up'
        extern public float rotationalOffset { get; set; }
    }


    // Applies tangent forces along the surfaces of colliders.
    [NativeHeader("Modules/Physics2D/SurfaceEffector2D.h")]
    public partial class SurfaceEffector2D : Effector2D
    {
        // The speed to be maintained along the surface.
        extern public float speed { get; set; }

        // The speed variation (from zero to the variation) added to base speed to be applied.
        extern public float speedVariation { get; set; }

        // The scale of the impulse force applied while attempting to reach the surface speed.
        extern public float forceScale { get; set; }

        // Should the impulse force but applied to the contact point?
        extern public bool useContactForce { get; set; }

        // Should friction be used for any contact with the surface?
        extern public bool useFriction { get; set; }

        // Should bounce be used for any contact with the surface?
        extern public bool useBounce { get; set; }
    }

    #endregion

    #region Miscellaneous Components

    // A base type that provides constant physics behaviour support.
    [NativeHeader("Modules/Physics2D/PhysicsUpdateBehaviour2D.h")]
    public partial class PhysicsUpdateBehaviour2D : Behaviour
    {
    }

    // Applies constant forces to the Rigidbody2D.
    [NativeHeader("Modules/Physics2D/ConstantForce2D.h")]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed partial class ConstantForce2D : PhysicsUpdateBehaviour2D
    {
        // The force to apply globally each physics update.
        extern public Vector2 force { get; set; }

        // The force to apply locally each physics update.
        extern public Vector2 relativeForce { get; set; }

        // The torque to apply each physics update.
        extern public float torque { get; set; }
    }

    [NativeHeader("Modules/Physics2D/Public/PhysicsMaterial2D.h")]
    public sealed partial class PhysicsMaterial2D : Object
    {
        // Creates a new material.
        public PhysicsMaterial2D() { Create_Internal(this, null); }

        // Creates a new material named /name/.
        public PhysicsMaterial2D(string name) { Create_Internal(this, name); }

        [NativeMethod("Create_Binding")]
        extern static private void Create_Internal([Writable] PhysicsMaterial2D scriptMaterial, string name);

        //  How bouncy is the surface? A value of 0 will not bounce. A value of 1 will bounce without any loss of energy.
        extern public float bounciness { get; set; }

        // The friction.
        extern public float friction { get; set; }
    }

    #endregion
}
