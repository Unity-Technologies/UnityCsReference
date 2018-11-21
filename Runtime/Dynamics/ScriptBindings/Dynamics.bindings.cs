// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;


namespace UnityEngine
{
    [NativeHeader("Runtime/Dynamics/PhysicMaterial.h")]
    public class PhysicMaterial : UnityEngine.Object
    {
        public PhysicMaterial() { Internal_CreateDynamicsMaterial(this, "DynamicMaterial"); }
        public PhysicMaterial(string name) { Internal_CreateDynamicsMaterial(this, name); }
        extern private static void Internal_CreateDynamicsMaterial([Writable] PhysicMaterial mat, string name);

        extern public float bounciness { get; set; }
        extern public float dynamicFriction { get; set; }
        extern public float staticFriction { get; set; }
        extern public PhysicMaterialCombine frictionCombine { get; set; }
        extern public PhysicMaterialCombine bounceCombine { get; set; }

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

    [NativeHeader("Runtime/Interfaces/IRaycast.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [NativeHeader("Runtime/Dynamics/RaycastHit.h")]
    [UsedByNativeCode]
    public struct RaycastHit
    {
        [NativeName("point")] internal Vector3 m_Point;
        [NativeName("normal")] internal Vector3 m_Normal;
        [NativeName("faceID")] internal uint m_FaceID;
        [NativeName("distance")] internal float m_Distance;
        [NativeName("uv")] internal Vector2 m_UV;
        [NativeName("collider")] internal int m_Collider;

        public Collider collider { get { return Object.FindObjectFromInstanceID(m_Collider) as Collider; } }

        public Vector3 point { get { return m_Point; } set { m_Point = value; } }
        public Vector3 normal { get { return m_Normal; } set { m_Normal = value; } }
        public Vector3 barycentricCoordinate { get { return new Vector3(1.0F - (m_UV.y + m_UV.x), m_UV.x, m_UV.y); } set { m_UV = value; } }
        public float distance { get { return m_Distance; } set { m_Distance = value; } }
        public int triangleIndex { get { return (int)m_FaceID; }  }

        [FreeFunction] extern static private Vector2 CalculateRaycastTexCoord(Collider collider, Vector2 uv, Vector3 pos, uint face, int textcoord);

        public Vector2 textureCoord { get { return CalculateRaycastTexCoord(collider, m_UV, m_Point, m_FaceID, 0); } }
        public Vector2 textureCoord2 { get { return CalculateRaycastTexCoord(collider, m_UV, m_Point, m_FaceID, 1); } }

        [Obsolete("Use textureCoord2 instead. (UnityUpgradable) -> textureCoord2")]
        public Vector2 textureCoord1 { get { return textureCoord2;  } }

        public Transform transform
        {
            get
            {
                Rigidbody body = rigidbody;
                if (body != null)
                    return body.transform;
                else if (collider != null)
                    return collider.transform;
                else
                    return null;
            }
        }

        public Rigidbody rigidbody { get { return collider != null ? collider.attachedRigidbody : null; }  }

        public Vector2 lightmapCoord
        {
            get
            {
                Vector2 coord = CalculateRaycastTexCoord(collider, m_UV, m_Point, m_FaceID, 1);
                if (collider.GetComponent<Renderer>() != null)
                {
                    Vector4 st = collider.GetComponent<Renderer>().lightmapScaleOffset;
                    coord.x = coord.x * st.x + st.z;
                    coord.y = coord.y * st.y + st.w;
                }
                return coord;
            }
        }
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Dynamics/Rigidbody.h")]
    public class Rigidbody : UnityEngine.Component
    {
        extern public Vector3 velocity { get; set; }
        extern public Vector3 angularVelocity { get; set; }
        extern public float drag { get; set; }
        extern public float angularDrag { get; set; }
        extern public float mass { get; set; }
        extern public void SetDensity(float density);
        extern public bool useGravity { get; set; }
        extern public float maxDepenetrationVelocity { get; set; }
        extern public bool isKinematic { get; set; }
        extern public bool freezeRotation { get; set; }
        extern public RigidbodyConstraints constraints { get; set; }
        extern public CollisionDetectionMode collisionDetectionMode { get; set; }
        extern public Vector3 centerOfMass { get; set; }
        extern public Vector3 worldCenterOfMass { get; }
        extern public Quaternion inertiaTensorRotation { get; set; }
        extern public Vector3 inertiaTensor { get; set; }
        extern public bool detectCollisions { get; set; }
        extern public Vector3 position { get; set; }
        extern public Quaternion rotation { get; set; }
        extern public RigidbodyInterpolation interpolation { get; set; }
        extern public int solverIterations { get; set; }
        extern public float sleepThreshold { get; set; }
        extern public float maxAngularVelocity { get; set; }
        extern public void MovePosition(Vector3 position);
        extern public void MoveRotation(Quaternion rot);
        extern public void Sleep();
        extern public bool IsSleeping();
        extern public void WakeUp();
        extern public void ResetCenterOfMass();
        extern public void ResetInertiaTensor();
        extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);
        extern public Vector3 GetPointVelocity(Vector3 worldPoint);
        extern public int solverVelocityIterations { get; set; }

        [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.")]
        extern public float sleepVelocity { get; set; }

        [Obsolete("The sleepAngularVelocity is no longer supported. Set Use sleepThreshold to specify energy.")]
        extern public float sleepAngularVelocity { get; set; }

        [Obsolete("Use Rigidbody.maxAngularVelocity instead.")]
        public void SetMaxAngularVelocity(float a) { maxAngularVelocity = a; }

        [Obsolete("Cone friction is no longer supported.")]
        public bool useConeFriction { get { return false; } set {} }

        [Obsolete("Please use Rigidbody.solverIterations instead. (UnityUpgradable) -> solverIterations")]
        public int solverIterationCount { get { return solverIterations; } set { solverIterations = value; } }

        [Obsolete("Please use Rigidbody.solverVelocityIterations instead. (UnityUpgradable) -> solverVelocityIterations")]
        public int solverVelocityIterationCount { get { return solverVelocityIterations; } set { solverVelocityIterations = value; } }

        extern public void AddForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForce(Vector3 force)
        {
            AddForce(force, ForceMode.Force);
        }

        public void AddForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddForce(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddForce(float x, float y, float z)
        {
            AddForce(new Vector3(x, y, z), ForceMode.Force);
        }

        extern public void AddRelativeForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeForce(Vector3 force)
        {
            AddRelativeForce(force, ForceMode.Force);
        }

        public void AddRelativeForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddRelativeForce(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddRelativeForce(float x, float y, float z)
        {
            AddRelativeForce(new Vector3(x, y, z), ForceMode.Force);
        }

        extern public void AddTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddTorque(Vector3 torque)
        {
            AddTorque(torque, ForceMode.Force);
        }

        public void AddTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddTorque(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddTorque(float x, float y, float z)
        {
            AddTorque(new Vector3(x, y, z), ForceMode.Force);
        }

        extern public void AddRelativeTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque, ForceMode.Force);
        }

        public void AddRelativeTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddRelativeTorque(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddRelativeTorque(float x, float y, float z)
        {
            AddRelativeTorque(x, y, z, ForceMode.Force);
        }

        extern public void AddForceAtPosition(Vector3 force, Vector3 position, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            AddForceAtPosition(force, position, ForceMode.Force);
        }

        extern public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, [DefaultValue("0.0f")] float upwardsModifier, [DefaultValue("ForceMode.Force)")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Force);
        }

        [ExcludeFromDocs]
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0.0f, ForceMode.Force);
        }

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }

        extern private RaycastHit SweepTest(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction, ref bool hasHit);

        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                bool hasHit = false;
                hitInfo = SweepTest(normalizedDirection, maxDistance, queryTriggerInteraction, ref hasHit);
                return hasHit;
            }
            else
            {
                hitInfo = new RaycastHit();
                return false;
            }
        }

        [ExcludeFromDocs]
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SweepTest(direction, out hitInfo, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo)
        {
            return SweepTest(direction, out hitInfo, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("SweepTestAll")]
        extern private RaycastHit[] Internal_SweepTestAll(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction);

        public RaycastHit[] SweepTestAll(Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                return Internal_SweepTestAll(normalizedDirection, maxDistance, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public RaycastHit[] SweepTestAll(Vector3 direction, float maxDistance)
        {
            return SweepTestAll(direction, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public RaycastHit[] SweepTestAll(Vector3 direction)
        {
            return SweepTestAll(direction, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
        }
    }

    [RequiredByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Dynamics/Collider.h")]
    public partial class Collider : Component
    {
        extern public bool enabled { get; set; }
        extern public Rigidbody attachedRigidbody {[NativeMethod("GetRigidbody")] get; }
        extern public bool isTrigger { get; set; }
        extern public float contactOffset { get; set; }
        extern public Vector3 ClosestPoint(Vector3 position);
        extern public Bounds bounds { get; }

        [NativeMethod("Material")]
        extern public PhysicMaterial sharedMaterial { get; set; }

        extern public PhysicMaterial material
        {
            [NativeMethod("GetClonedMaterial")] get;
            [NativeMethod("SetMaterial")] set;
        }

        extern private RaycastHit Raycast(Ray ray, float maxDistance, ref bool hasHit);

        public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            bool hasHit = false;
            hitInfo = Raycast(ray, maxDistance, ref hasHit);
            return hasHit;
        }

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }
    }

    [NativeHeader("Runtime/Dynamics/CharacterController.h")]
    public class CharacterController : Collider
    {
        extern public bool SimpleMove(Vector3 speed);
        extern public CollisionFlags Move(Vector3 motion);
        extern public Vector3 velocity { get; }
        extern public bool isGrounded {[NativeName("IsGrounded")] get; }
        extern public CollisionFlags collisionFlags { get; }
        extern public float radius { get; set; }
        extern public float height { get; set; }
        extern public Vector3 center { get; set; }
        extern public float slopeLimit { get; set; }
        extern public float stepOffset { get; set; }
        extern public float skinWidth { get; set; }
        extern public float minMoveDistance { get; set; }
        extern public bool detectCollisions { get; set; }
        extern public bool enableOverlapRecovery { get; set; }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Dynamics/MeshCollider.h")]
    [NativeHeader("Runtime/Graphics/Mesh/Mesh.h")]
    public class MeshCollider : Collider
    {
        extern public Mesh sharedMesh { get; set; }
        extern public bool convex { get; set; }

        [Obsolete("MeshCollider.inflateMesh is no longer supported. The new cooking algorithm doesn't need inflation to be used.")]
        public bool inflateMesh
        {
            get { return false; } set {}
        }

        extern public MeshColliderCookingOptions cookingOptions { get; set; }

        [Obsolete("MeshCollider.skinWidth is no longer used.")]
        public float skinWidth { get { return 0f; } set {} }

        [Obsolete("Configuring smooth sphere collisions is no longer needed.")]
        public bool smoothSphereCollisions { get { return true; } set {} }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Dynamics/CapsuleCollider.h")]
    public class CapsuleCollider : Collider
    {
        extern public Vector3 center { get; set; }
        extern public float radius { get; set; }
        extern public float height { get; set; }
        extern public int direction { get; set; }

        extern internal Vector2 GetGlobalExtents();
        extern internal Matrix4x4 CalculateTransform();
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Dynamics/BoxCollider.h")]
    public class BoxCollider : Collider
    {
        extern public Vector3 center { get; set; }
        extern public Vector3 size { get; set; }

        [Obsolete("Use BoxCollider.size instead.")]
        public Vector3 extents { get { return size * 0.5F; }  set { size = value * 2.0F; } }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Dynamics/SphereCollider.h")]
    public class SphereCollider : Collider
    {
        extern public Vector3 center { get; set; }
        extern public float radius { get; set; }
    }

    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Runtime/Dynamics/ConstantForce.h")]
    public class ConstantForce : Behaviour
    {
        extern public Vector3 force { get; set; }
        extern public Vector3 relativeForce { get; set; }

        extern public Vector3 torque { get; set; }
        extern public Vector3 relativeTorque { get; set; }
    }

    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Runtime/Dynamics/Joint.h")]
    [NativeClass("Unity::Joint")]
    public class Joint : UnityEngine.Component
    {
        extern public Rigidbody connectedBody { get; set; }
        extern public Vector3 axis { get; set; }
        extern public Vector3 anchor { get; set; }
        extern public Vector3 connectedAnchor { get; set; }
        extern public bool autoConfigureConnectedAnchor { get; set; }
        extern public float breakForce { get; set; }
        extern public float breakTorque { get; set; }
        extern public bool enableCollision { get; set; }
        extern public bool enablePreprocessing { get; set; }
        extern public float massScale { get; set; }
        extern public float connectedMassScale { get; set; }

        extern private void GetCurrentForces(ref Vector3 linearForce, ref Vector3 angularForce);

        public Vector3 currentForce
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return force;
            }
        }

        public Vector3 currentTorque
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return torque;
            }
        }

        extern internal Matrix4x4 GetLocalPoseMatrix(int bodyIndex);
    }

    [NativeHeader("Runtime/Dynamics/HingeJoint.h")]
    [NativeClass("Unity::HingeJoint")]
    public class HingeJoint : Joint
    {
        extern public JointMotor motor { get; set; }
        extern public JointLimits limits { get; set; }
        extern public JointSpring spring { get; set; }
        extern public bool useMotor { get; set; }
        extern public bool useLimits { get; set; }
        extern public bool useSpring { get; set; }
        extern public float velocity { get; }
        extern public float angle { get; }
    }

    [NativeHeader("Runtime/Dynamics/SpringJoint.h")]
    [NativeClass("Unity::SpringJoint")]
    public class SpringJoint : Joint
    {
        extern public float spring { get; set; }
        extern public float damper { get; set; }
        extern public float minDistance { get; set; }
        extern public float maxDistance { get; set; }
        extern public float tolerance { get; set; }
    }

    [NativeHeader("Runtime/Dynamics/FixedJoint.h")]
    [NativeClass("Unity::FixedJoint")]
    public class FixedJoint : Joint
    {
    }

    [NativeHeader("Runtime/Dynamics/CharacterJoint.h")]
    [NativeClass("Unity::CharacterJoint")]
    public class CharacterJoint : Joint
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

        [Obsolete("TargetRotation not in use for Unity 5 and assumed disabled.", true)]
        public Quaternion targetRotation;

        [Obsolete("TargetAngularVelocity not in use for Unity 5 and assumed disabled.", true)]
        public Vector3 targetAngularVelocity;

        [Obsolete("RotationDrive not in use for Unity 5 and assumed disabled.")]
        public JointDrive rotationDrive;
    }

    [NativeHeader("Runtime/Dynamics/ConfigurableJoint.h")]
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

    [UsedByNativeCode]
    [NativeHeader("Runtime/Dynamics/MessageParameters.h")]
    public struct ContactPoint
    {
        internal Vector3  m_Point;
        internal Vector3  m_Normal;
        internal int m_ThisColliderInstanceID;
        internal int m_OtherColliderInstanceID;
        internal float m_Separation;

        public Vector3 point  { get { return m_Point; } }
        public Vector3 normal { get { return m_Normal; } }

        public Collider thisCollider { get { return GetColliderByInstanceID(m_ThisColliderInstanceID); } }
        public Collider otherCollider { get { return GetColliderByInstanceID(m_OtherColliderInstanceID); } }
        public float separation { get { return m_Separation; }}

        [FreeFunction]
        extern private static Collider GetColliderByInstanceID(int instanceID);
    }

    #region Scene

    [NativeHeader("Runtime/Dynamics/Public/PhysicsSceneHandle.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PhysicsScene : IEquatable<PhysicsScene>
    {
        private int m_Handle;

        public override string ToString() { return UnityString.Format("({0})", m_Handle); }
        public static bool operator==(PhysicsScene lhs, PhysicsScene rhs) { return lhs.m_Handle == rhs.m_Handle; }
        public static bool operator!=(PhysicsScene lhs, PhysicsScene rhs) { return lhs.m_Handle != rhs.m_Handle; }
        public override int GetHashCode() { return m_Handle; }
        public override bool Equals(object other)
        {
            if (!(other is PhysicsScene))
                return false;

            PhysicsScene rhs = (PhysicsScene)other;
            return m_Handle == rhs.m_Handle;
        }

        public bool Equals(PhysicsScene other)
        {
            return m_Handle == other.m_Handle;
        }

        public bool IsValid() { return IsValid_Internal(this); }
        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsPhysicsSceneValid")]
        extern private static bool IsValid_Internal(PhysicsScene physicsScene);

        public bool IsEmpty()
        {
            if (IsValid())
                return IsEmpty_Internal(this);

            throw new InvalidOperationException("Cannot check if physics scene is empty as it is invalid.");
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsPhysicsWorldEmpty")]
        extern private static bool IsEmpty_Internal(PhysicsScene physicsScene);

        // Perform a manual simulation step.
        public void Simulate(float step)
        {
            if (IsValid())
            {
                // Only check auto-simulation if simulating the default physics scene.
                if (this == Physics.defaultPhysicsScene && Physics.autoSimulation)
                {
                    Debug.LogWarning("PhysicsScene.Simulate(...) was called but auto simulation is active. You should disable auto simulation first before calling this function therefore the simulation was not run.");
                    return;
                }

                Physics.Simulate_Internal(this, step);
                return;
            }

            throw new InvalidOperationException("Cannot simulate the physics scene as it is invalid.");
        }

        // Hit Test.
        public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastTest(this, ray, maxDistance, layerMask, queryTriggerInteraction);
            }

            return false;
        }

        [NativeName("RaycastTest")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static bool Internal_RaycastTest(PhysicsScene physicsScene, Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // Single hit.
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            hitInfo = new RaycastHit();

            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);

                return Internal_Raycast(this, ray, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        [NativeName("Raycast")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static bool Internal_Raycast(PhysicsScene physicsScene, Ray ray, float maxDistance, ref RaycastHit hit, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // Multiple hits.
        public int Raycast(Vector3 origin, Vector3 direction, RaycastHit[] raycastHits, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                Ray ray = new Ray(origin, direction.normalized);
                return Internal_RaycastNonAlloc(this, ray, raycastHits, maxDistance, layerMask, queryTriggerInteraction);
            }

            return 0;
        }

        [NativeName("RaycastNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static int Internal_RaycastNonAlloc(PhysicsScene physicsScene, Ray ray, [Out] RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);
    }

    public static class PhysicsSceneExtensions
    {
        public static PhysicsScene GetPhysicsScene(this Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Cannot get physics scene; Unity scene is invalid.", "scene");

            PhysicsScene physicsScene = GetPhysicsScene_Internal(scene);
            if (physicsScene.IsValid())
                return physicsScene;

            throw new Exception("The physics scene associated with the Unity scene is invalid.");
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("GetPhysicsSceneFromUnityScene")]
        extern private static PhysicsScene GetPhysicsScene_Internal(Scene scene);
    }

    #endregion

    [NativeHeader("Runtime/Dynamics/PhysicsManager.h")]
    [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
    public class Physics
    {
        // PX_MAX_F32_MINUS_EPS; currently used in e.g., EnforceJointLimitsConsistency()
        internal const float k_MaxFloatMinusEpsilon = 340282326356119260000000000000000000000f;

        public const int IgnoreRaycastLayer = 1 << 2;
        public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;
        public const int AllLayers = ~0;

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

        extern public static Vector3 gravity {[ThreadSafe] get; set; }
        extern public static float defaultContactOffset { get; set; }
        extern public static float sleepThreshold { get; set; }
        extern public static bool queriesHitTriggers { get; set; }
        extern public static bool queriesHitBackfaces { get; set; }
        extern public static float bounceThreshold { get; set; }
        extern public static int defaultSolverIterations { get; set; }
        extern public static int defaultSolverVelocityIterations { get; set; }

        [Obsolete("Please use bounceThreshold instead.")]
        public static float bounceTreshold { get { return bounceThreshold; } set { bounceThreshold = value; }  }

        [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.")]
        public static float sleepVelocity { get { return 0f; } set {} }

        [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.")]
        public static float sleepAngularVelocity { get { return 0f; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use Rigidbody.maxAngularVelocity instead.", true)]
        public static float maxAngularVelocity { get { return 0f; } set {} }

        [Obsolete("Please use Physics.defaultSolverIterations instead. (UnityUpgradable) -> defaultSolverIterations")]
        public static int solverIterationCount { get { return defaultSolverIterations; } set { defaultSolverIterations = value; } }

        [Obsolete("Please use Physics.defaultSolverVelocityIterations instead. (UnityUpgradable) -> defaultSolverVelocityIterations")]
        public static int solverVelocityIterationCount { get { return defaultSolverVelocityIterations; } set { defaultSolverVelocityIterations = value; } }

        [Obsolete("penetrationPenaltyForce has no effect.")]
        public static float penetrationPenaltyForce { get { return 0f; } set {} }

        [NativeProperty("DefaultPhysicsSceneHandle")]
        extern public static PhysicsScene defaultPhysicsScene { get; }

        extern public static void IgnoreCollision(Collider collider1, Collider collider2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreCollision(Collider collider1, Collider collider2)
        {
            IgnoreCollision(collider1, collider2, true);
        }

        [NativeName("IgnoreCollision")]
        extern public static void IgnoreLayerCollision(int layer1, int layer2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreLayerCollision(int layer1, int layer2)
        {
            IgnoreLayerCollision(layer1, layer2, true);
        }

        [NativeName("GetIgnoreCollision")]
        extern public static bool GetIgnoreLayerCollision(int layer1, int layer2);

        static public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Raycast(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Raycast(Ray ray, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Linecast(Vector3 start, Vector3 end, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, int layerMask)
        {
            return Linecast(start, end, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end)
        {
            return Linecast(start, end, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, out hitInfo, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
        {
            return Linecast(start, end, out hitInfo, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo)
        {
            return Linecast(start, end, out hitInfo, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("CapsuleCast")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static bool Query_CapsuleCast(PhysicsScene physicsScene, Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, ref RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_CapsuleCast(PhysicsScene physicsScene, Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCast(physicsScene, point1, point2, radius, normalizedDirection, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return Internal_CapsuleCast(defaultPhysicsScene, point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCast(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return Internal_CapsuleCast(defaultPhysicsScene, point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("SphereCast")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static bool Query_SphereCast(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, ref RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_SphereCast(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCast(physicsScene, origin, radius, normalizedDirection, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return Internal_SphereCast(defaultPhysicsScene, origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return SphereCast(origin, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool SphereCast(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return Internal_SphereCast(defaultPhysicsScene, ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance)
        {
            return SphereCast(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius)
        {
            return SphereCast(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return Internal_CapsuleCast(defaultPhysicsScene, ray.origin, ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo)
        {
            return SphereCast(ray, radius, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("BoxCast")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern static private bool Query_BoxCast(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, ref RaycastHit outHit, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_BoxCast(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_BoxCast(physicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return Internal_BoxCast(defaultPhysicsScene, center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCast(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return Internal_BoxCast(defaultPhysicsScene, center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("RaycastAll")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern static RaycastHit[] Internal_RaycastAll(PhysicsScene physicsScene, Ray ray, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastAll(defaultPhysicsScene, ray, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return RaycastAll(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            return RaycastAll(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public RaycastHit[] RaycastAll(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray)
        {
            return RaycastAll(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("CapsuleCastAll")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static RaycastHit[] Query_CapsuleCastAll(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCastAll(defaultPhysicsScene, point1, point2, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCastAll(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("SphereCastAll")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static RaycastHit[] Query_SphereCastAll(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCastAll(defaultPhysicsScene, origin, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction)
        {
            return SphereCastAll(origin, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public RaycastHit[] SphereCastAll(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastAll(ray.origin, radius, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCastAll(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance)
        {
            return SphereCastAll(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius)
        {
            return SphereCastAll(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("OverlapCapsule")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static Collider[] OverlapCapsule_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapCapsule_Internal(defaultPhysicsScene, point0, point1, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask)
        {
            return OverlapCapsule(point0, point1, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius)
        {
            return OverlapCapsule(point0, point1, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("OverlapSphere")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()", StaticAccessorType.Dot)]
        extern private static Collider[] OverlapSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static Collider[] OverlapSphere(Vector3 position, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask)
        {
            return OverlapSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius)
        {
            return OverlapSphere(position, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("Simulate")]
        extern internal static void Simulate_Internal(PhysicsScene physicsScene, float step);

        public static void Simulate(float step)
        {
            if (autoSimulation)
            {
                Debug.LogWarning("Physics.Simulate(...) was called but auto simulation is active. You should disable auto simulation first before calling this function therefore the simulation was not run.");
                return;
            }

            Simulate_Internal(defaultPhysicsScene, step);
        }

        extern public static bool autoSimulation { get; set; }
        extern public static void SyncTransforms();
        extern public static bool autoSyncTransforms { get; set; }
        extern public static bool reuseCollisionCallbacks { get; set; }

        [NativeName("ComputePenetration")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static bool Query_ComputePenetration([NotNull] Collider colliderA, Vector3 positionA, Quaternion rotationA, [NotNull] Collider colliderB, Vector3 positionB, Quaternion rotationB, ref Vector3 direction, ref float distance);

        public static bool ComputePenetration(Collider colliderA, Vector3 positionA, Quaternion rotationA, Collider colliderB, Vector3 positionB, Quaternion rotationB, out Vector3 direction, out float distance)
        {
            direction = Vector3.zero;
            distance = 0f;
            return Query_ComputePenetration(colliderA, positionA, rotationA, colliderB, positionB, rotationB, ref direction, ref distance);
        }

        [NativeName("ClosestPoint")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static Vector3 Query_ClosestPoint([NotNull] Collider collider, Vector3 position, Quaternion rotation, Vector3 point);

        public static Vector3 ClosestPoint(Vector3 point, Collider collider, Vector3 position, Quaternion rotation)
        {
            return Query_ClosestPoint(collider, position, rotation, point);
        }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionDistance {[NativeName("GetClothInterCollisionDistance")] get; [NativeName("SetClothInterCollisionDistance")] set; }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionStiffness {[NativeName("GetClothInterCollisionStiffness")] get; [NativeName("SetClothInterCollisionStiffness")] set; }

        [StaticAccessor("GetPhysicsManager()")]
        public extern static bool interCollisionSettingsToggle {[NativeName("GetClothInterCollisionSettingsToggle")] get; [NativeName("SetClothInterCollisionSettingsToggle")] set; }

        [NativeName("OverlapSphereNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static int OverlapSphereNonAlloc_Internal(PhysicsScene physicsScene, Vector3 position, float radius, [Out] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, [Out] Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphereNonAlloc_Internal(defaultPhysicsScene, position, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, [Out] Collider[] results, int layerMask)
        {
            return OverlapSphereNonAlloc(position, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, [Out] Collider[] results)
        {
            return OverlapSphereNonAlloc(position, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("SphereTest")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static bool CheckSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static bool CheckSphere(Vector3 position, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius, int layerMask)
        {
            return CheckSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius)
        {
            return CheckSphere(position, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("CapsuleCastNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static int Internal_CapsuleCastNonAlloc(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, [Out] RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_CapsuleCastNonAlloc(defaultPhysicsScene, point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("SphereCastNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static int Internal_SphereCastNonAlloc(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, [Out] RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_SphereCastNonAlloc(defaultPhysicsScene, origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastNonAlloc(ray.origin, radius, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results)
        {
            return SphereCastNonAlloc(ray, radius, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("CapsuleTest")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static bool CheckCapsule_Internal(PhysicsScene physicsScene, Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckCapsule_Internal(defaultPhysicsScene, start, end, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask)
        {
            return CheckCapsule(start, end, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius)
        {
            return CheckCapsule(start, end, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("BoxTest")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static bool CheckBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layermask, QueryTriggerInteraction queryTriggerInteraction);
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layermask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layermask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return CheckBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return CheckBox(center, halfExtents, orientation, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents)
        {
            return CheckBox(center, halfExtents, Quaternion.identity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("OverlapBox")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static Collider[] OverlapBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return OverlapBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return OverlapBox(center, halfExtents, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents)
        {
            return OverlapBox(center, halfExtents, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("OverlapBoxNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static int OverlapBoxNonAlloc_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, [Out] Collider[] results, Quaternion orientation, int mask, QueryTriggerInteraction queryTriggerInteraction);
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, [Out] Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int mask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapBoxNonAlloc_Internal(defaultPhysicsScene, center, halfExtents, results, orientation, mask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, [Out] Collider[] results, Quaternion orientation, int mask)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, mask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, [Out] Collider[] results, Quaternion orientation)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, [Out] Collider[] results)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("BoxCastNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        private static extern int Internal_BoxCastNonAlloc(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, [Out] RaycastHit[] raycastHits, Quaternion orientation, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_BoxCastNonAlloc(defaultPhysicsScene, center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("BoxCastAll")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        private static extern RaycastHit[] Internal_BoxCastAll(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Internal_BoxCastAll(defaultPhysicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return new RaycastHit[0];
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCastAll(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("OverlapCapsuleNonAlloc")]
        [StaticAccessor("GetPhysicsManager().GetPhysicsQuery()")]
        extern private static int OverlapCapsuleNonAlloc_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, [Out] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, [Out] Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapCapsuleNonAlloc_Internal(defaultPhysicsScene, point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, [Out] Collider[] results, int layerMask)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, [Out] Collider[] results)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("RebuildBroadphaseRegions")]
        [StaticAccessor("GetPhysicsManager()")]
        private static extern void Internal_RebuildBroadphaseRegions(Bounds bounds, int subdivisions);

        public static void RebuildBroadphaseRegions(Bounds worldBounds, int subdivisions)
        {
            if (subdivisions < 1 || subdivisions > 16)
            {
                throw new ArgumentException("Physics.RebuildBroadphaseRegions requires the subdivisions to be greater than zero and less than 17.");
            }

            if (worldBounds.extents.x < 0 || worldBounds.extents.y < 0 || worldBounds.extents.z < 0)
            {
                throw new ArgumentException("Physics.RebuildBroadphaseRegions requires the world bounds to be non-empty, and have positive extents.");
            }

            Internal_RebuildBroadphaseRegions(worldBounds, subdivisions);
        }
    }
}

