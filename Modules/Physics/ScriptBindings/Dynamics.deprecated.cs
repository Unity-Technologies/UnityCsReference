// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;


namespace UnityEngine
{
    public partial class PhysicMaterial
    {
        [Obsolete("Use PhysicMaterial.bounciness instead (UnityUpgradable) -> bounciness")]
        public float bouncyness { get { return bounciness; } set { bounciness = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Anisotropic friction is no longer supported since Unity 5.0.", true)]
        public Vector3 frictionDirection2 { get { return Vector3.zero; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Anisotropic friction is no longer supported since Unity 5.0.", true)]
        public float dynamicFriction2 { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Anisotropic friction is no longer supported since Unity 5.0.", true)]
        public float staticFriction2 { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Anisotropic friction is no longer supported since Unity 5.0.", true)]
        public Vector3 frictionDirection { get { return Vector3.zero; } set {} }
    }

    partial struct RaycastHit
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use textureCoord2 instead. (UnityUpgradable) -> textureCoord2")]
        public Vector2 textureCoord1 { get { return textureCoord2;  } }
    }

    public partial class Rigidbody
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public float sleepVelocity { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold to specify energy.", true)]
        public float sleepAngularVelocity { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Rigidbody.maxAngularVelocity instead.")]
        public void SetMaxAngularVelocity(float a) { maxAngularVelocity = a; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Cone friction is no longer supported.", true)]
        public bool useConeFriction { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody.solverIterations instead. (UnityUpgradable) -> solverIterations")]
        public int solverIterationCount { get { return solverIterations; } set { solverIterations = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody.solverVelocityIterations instead. (UnityUpgradable) -> solverVelocityIterations")]
        public int solverVelocityIterationCount { get { return solverVelocityIterations; } set { solverVelocityIterations = value; } }
    }

    public partial class MeshCollider
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Configuring smooth sphere collisions is no longer needed.", true)]
        public bool smoothSphereCollisions { get { return true; } set {} }

        [Obsolete("MeshCollider.skinWidth is no longer used.")]
        public float skinWidth { get { return 0f; } set {} }

        [Obsolete("MeshCollider.inflateMesh is no longer supported. The new cooking algorithm doesn't need inflation to be used.")]
        public bool inflateMesh
        {
            get { return false; } set {}
        }
    }

    public partial class BoxCollider
    {
        [Obsolete("Use BoxCollider.size instead. (UnityUpgradable) -> size")]
        public Vector3 extents { get { return size * 0.5F; }  set { size = value * 2.0F; } }
    }

    public partial class CharacterJoint
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("TargetRotation not in use for Unity 5 and assumed disabled.", true)]
        public Quaternion targetRotation;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("TargetAngularVelocity not in use for Unity 5 and assumed disabled.", true)]
        public Vector3 targetAngularVelocity;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RotationDrive not in use for Unity 5 and assumed disabled.", true)]
        public JointDrive rotationDrive;
    }

    public partial class Physics
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Physics.IgnoreRaycastLayer instead. (UnityUpgradable) -> IgnoreRaycastLayer", true)]
        public const int kIgnoreRaycastLayer = IgnoreRaycastLayer;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Physics.DefaultRaycastLayers instead. (UnityUpgradable) -> DefaultRaycastLayers", true)]
        public const int kDefaultRaycastLayers = DefaultRaycastLayers;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Physics.AllLayers instead. (UnityUpgradable) -> AllLayers", true)]
        public const int kAllLayers = AllLayers;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Physics.defaultContactOffset or Collider.contactOffset instead.", true)]
        public static float minPenetrationForPenalty { get { return 0f; } set {} }

        [Obsolete("Please use bounceThreshold instead. (UnityUpgradable) -> bounceThreshold")]
        public static float bounceTreshold { get { return bounceThreshold; } set { bounceThreshold = value; }  }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public static float sleepVelocity { get { return 0f; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public static float sleepAngularVelocity { get { return 0f; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Rigidbody.maxAngularVelocity instead.", true)]
        public static float maxAngularVelocity { get { return 0f; } set {} }

        [Obsolete("Please use Physics.defaultSolverIterations instead. (UnityUpgradable) -> defaultSolverIterations")]
        public static int solverIterationCount { get { return defaultSolverIterations; } set { defaultSolverIterations = value; } }

        [Obsolete("Please use Physics.defaultSolverVelocityIterations instead. (UnityUpgradable) -> defaultSolverVelocityIterations")]
        public static int solverVelocityIterationCount { get { return defaultSolverVelocityIterations; } set { defaultSolverVelocityIterations = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("penetrationPenaltyForce has no effect.", true)]
        public static float penetrationPenaltyForce { get { return 0f; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Physics.autoSimulation has been replaced by Physics.simulationMode", false)]
        public static bool autoSimulation
        {
            get { return simulationMode != SimulationMode.Script; }
            set { simulationMode = value ? SimulationMode.FixedUpdate : SimulationMode.Script; }
        }

        [Obsolete("Physics.RebuildBroadphaseRegions has been deprecated alongside Multi Box Pruning. Use Automatic Box Pruning instead.", false)]
        public static void RebuildBroadphaseRegions(Bounds worldBounds, int subdivisions)
        {
            return;
        }
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

    public partial struct SoftJointLimit
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Spring has been moved to SoftJointLimitSpring class in Unity 5", true)]
        public float spring { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Damper has been moved to SoftJointLimitSpring class in Unity 5", true)]
        public float damper { get { return 0; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use SoftJointLimit.bounciness instead", true)]
        public float bouncyness { get { return m_Bounciness; } set { m_Bounciness = value; } }
    }

    public partial struct JointDrive
    {
        [Obsolete("JointDriveMode is obsolete")]
        public JointDriveMode mode { get { return (JointDriveMode)0; } set {} }
    }

    public partial class Collision
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Do not use Collision.GetEnumerator(), enumerate using non-allocating array returned by Collision.GetContacts() or enumerate using Collision.GetContact(index) instead.", false)]
        public virtual IEnumerator GetEnumerator()
        {
            return contacts.GetEnumerator();
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Collision.relativeVelocity instead. (UnityUpgradable) -> relativeVelocity", false)]
        public Vector3 impactForceSum { get { return Vector3.zero; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Will always return zero.", true)]
        public Vector3 frictionForceSum { get { return Vector3.zero; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Please use Collision.rigidbody, Collision.transform or Collision.collider instead", false)]
        public Component other { get { return body != null ? body : collider; } }
    }
}

