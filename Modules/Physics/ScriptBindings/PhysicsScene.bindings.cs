// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    [NativeHeader("Modules/Physics/PhysicsQuery.h")]
    [NativeHeader("Modules/Physics/Public/PhysicsSceneHandle.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PhysicsScene : IEquatable<PhysicsScene>
    {
        private int m_Handle;

        public override string ToString() { return string.Format("({0})", m_Handle); }
        public static bool operator ==(PhysicsScene lhs, PhysicsScene rhs) { return lhs.m_Handle == rhs.m_Handle; }
        public static bool operator !=(PhysicsScene lhs, PhysicsScene rhs) { return lhs.m_Handle != rhs.m_Handle; }
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
                if (this == Physics.defaultPhysicsScene && Physics.simulationMode != SimulationMode.Script)
                {
                    Debug.LogWarning("PhysicsScene.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                    return;
                }

                Physics.Simulate_Internal(this, step, SimulationStage.All, SimulationOption.All);
                return;
            }

            throw new InvalidOperationException("Cannot simulate the physics scene as it is invalid.");
        }

        public void RunSimulationStages(float step, SimulationStage stages, [DefaultValue("SimulationOption.All")] SimulationOption options = SimulationOption.All)
        {
            if (!IsValid())
                throw new InvalidOperationException("Cannot simulate the physics scene as it is invalid.");

            // Only check auto-simulation if simulating the default physics scene.
            if (this == Physics.defaultPhysicsScene && Physics.simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning("PhysicsScene.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                return;
            }

            Physics.Simulate_Internal(this, step, stages, options);
        }

        public void InterpolateBodies()
        {
            if (!IsValid())
                throw new InvalidOperationException("Cannot interpolate the physics scene as it is invalid.");

            if (this == Physics.defaultPhysicsScene)
            {
                Debug.LogWarning("PhysicsScene.InterpolateBodies() was called on the default Physics Scene. This is done automatically and the call will be ignored");
                return;
            }

            Physics.InterpolateBodies_Internal(this);
        }

        public void ResetInterpolationPoses()
        {
            if (!IsValid())
                throw new InvalidOperationException("Cannot reset poses of the physics scene as it is invalid.");

            if (this == Physics.defaultPhysicsScene)
            {
                Debug.LogWarning("PhysicsScene.ResetInterpolationPoses() was called on the default Physics Scene. This is done automatically and the call will be ignored");
                return;
            }

            Physics.ResetInterpolationPoses_Internal(this);
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

        [FreeFunction("Physics::RaycastTest")]
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

        [FreeFunction("Physics::Raycast")]
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

        [FreeFunction("Physics::RaycastNonAlloc")]
        extern private static int Internal_RaycastNonAlloc(PhysicsScene physicsScene, Ray ray, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        [FreeFunction("Physics::CapsuleCast")]
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

        public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_CapsuleCast(this, point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::CapsuleCastNonAlloc")]
        extern private static int Internal_CapsuleCastNonAlloc(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public int CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_CapsuleCastNonAlloc(this, point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [FreeFunction("Physics::OverlapCapsuleNonAlloc")]
        extern private static int OverlapCapsuleNonAlloc_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, [Unmarshalled] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask = Physics.AllLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapCapsuleNonAlloc_Internal(this, point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::SphereCast")]
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

        public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_SphereCast(this, origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::SphereCastNonAlloc")]
        extern private static int Internal_SphereCastNonAlloc(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public int SphereCast(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_SphereCastNonAlloc(this, origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [FreeFunction("Physics::OverlapSphereNonAlloc")]
        extern private static int OverlapSphereNonAlloc_Internal(PhysicsScene physicsScene, Vector3 position, float radius, [Unmarshalled] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        public int OverlapSphere(Vector3 position, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphereNonAlloc_Internal(this, position, radius, results, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::BoxCast")]
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

        public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_BoxCast(this, center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return Internal_BoxCast(this, center, halfExtents, Quaternion.identity, direction, out hitInfo, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapBoxNonAlloc")]
        extern private static int OverlapBoxNonAlloc_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, [Unmarshalled] Collider[] results, Quaternion orientation, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public int OverlapBox(Vector3 center, Vector3 halfExtents, Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapBoxNonAlloc_Internal(this, center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public int OverlapBox(Vector3 center, Vector3 halfExtents, Collider[] results)
        {
            return OverlapBoxNonAlloc_Internal(this, center, halfExtents, results, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::BoxCastNonAlloc")]
        private static extern int Internal_BoxCastNonAlloc(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] raycastHits, Quaternion orientation, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        public int BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_BoxCastNonAlloc(this, center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [ExcludeFromDocs]
        public int BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCast(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }
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
}
