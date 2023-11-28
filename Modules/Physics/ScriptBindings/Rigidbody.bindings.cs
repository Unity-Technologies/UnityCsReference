// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine
{
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

    public enum RigidbodyInterpolation
    {
        None = 0,
        Interpolate = 1,
        Extrapolate = 2
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/Rigidbody.h")]
    public partial class Rigidbody : Component
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
        public bool freezeRotation
        {
            get => constraints.HasFlag(RigidbodyConstraints.FreezeRotation);
            set
            {
                if (value)
                    constraints |= RigidbodyConstraints.FreezeRotation;
                else
                    constraints &= RigidbodyConstraints.FreezePosition;
            }
        }
        extern public RigidbodyConstraints constraints { get; set; }
        extern public CollisionDetectionMode collisionDetectionMode { get; set; }
        extern public bool automaticCenterOfMass { get; set; }
        extern public Vector3 centerOfMass { get; set; }
        extern public Vector3 worldCenterOfMass { get; }
        extern public bool automaticInertiaTensor { get; set; }
        extern public Quaternion inertiaTensorRotation { get; set; }
        extern public Vector3 inertiaTensor { get; set; }
        extern internal Matrix4x4 worldInertiaTensorMatrix { get; }
        extern public bool detectCollisions { get; set; }
        extern public Vector3 position { get; set; }
        extern public Quaternion rotation { get; set; }
        extern public RigidbodyInterpolation interpolation { get; set; }
        extern public int solverIterations { get; set; }
        extern public float sleepThreshold { get; set; }
        extern public float maxAngularVelocity { get; set; }
        extern public float maxLinearVelocity { get; set; }
        extern public void MovePosition(Vector3 position);
        extern public void MoveRotation(Quaternion rot);
        extern public void Move(Vector3 position, Quaternion rotation);
        extern public void Sleep();
        extern public bool IsSleeping();
        extern public void WakeUp();
        extern public void ResetCenterOfMass();
        extern public void ResetInertiaTensor();
        extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);
        extern public Vector3 GetPointVelocity(Vector3 worldPoint);
        extern public int solverVelocityIterations { get; set; }
        extern public void PublishTransform();
        extern public LayerMask excludeLayers { get; set; }
        extern public LayerMask includeLayers { get; set; }
        extern public Vector3 GetAccumulatedForce([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedForce()
        {
            return GetAccumulatedForce(Time.fixedDeltaTime);
        }

        extern public Vector3 GetAccumulatedTorque([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedTorque()
        {
            return GetAccumulatedTorque(Time.fixedDeltaTime);
        }

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
}
