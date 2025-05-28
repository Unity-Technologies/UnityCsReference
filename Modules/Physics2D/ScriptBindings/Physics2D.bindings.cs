// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine
{
    #region Scene

    [NativeHeader("Modules/Physics2D/Public/PhysicsSceneHandle2D.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PhysicsScene2D : IEquatable<PhysicsScene2D>
    {
        private int m_Handle;

        public override string ToString() { return UnityString.Format("({0})", m_Handle); }
        public static bool operator==(PhysicsScene2D lhs, PhysicsScene2D rhs) { return lhs.m_Handle == rhs.m_Handle; }
        public static bool operator!=(PhysicsScene2D lhs, PhysicsScene2D rhs) { return lhs.m_Handle != rhs.m_Handle; }
        public override int GetHashCode() { return m_Handle; }
        public override bool Equals(object other)
        {
            if (!(other is PhysicsScene2D))
                return false;

            PhysicsScene2D rhs = (PhysicsScene2D)other;
            return m_Handle == rhs.m_Handle;
        }

        public bool Equals(PhysicsScene2D other)
        {
            return m_Handle == other.m_Handle;
        }

        public bool IsValid() { return IsValid_Internal(this); }
        [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
        [NativeMethod("IsPhysicsSceneValid")]
        extern private static bool IsValid_Internal(PhysicsScene2D physicsScene);

        public bool IsEmpty()
        {
            if (IsValid())
                return IsEmpty_Internal(this);

            throw new InvalidOperationException("Cannot check if physics scene is empty as it is invalid.");
        }

        [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
        [NativeMethod("IsPhysicsWorldEmpty")]
        extern private static bool IsEmpty_Internal(PhysicsScene2D physicsScene);

        public int subStepCount { get { return SubStepCount_Internal(this); } }

        [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
        [NativeMethod("GetSubStepCount")]
        extern private static int SubStepCount_Internal(PhysicsScene2D physicsScene);

        public float subStepLostTime { get { return SubStepLostTime_Internal(this); } }

        [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
        [NativeMethod("GetSubStepLostTime")]
        extern private static float SubStepLostTime_Internal(PhysicsScene2D physicsScene);

        // Perform a manual simulation step.
        [ExcludeFromDocs]
        public bool Simulate(float deltaTime)
        {
            return Simulate(deltaTime, Physics2D.AllLayers);
        }

        public bool Simulate(float deltaTime, [DefaultValue("Physics2D.AllLayers")] int simulationLayers = Physics2D.AllLayers)
        {
            if (IsValid())
                return Physics2D.Simulate_Internal(this, deltaTime, simulationLayers);

            throw new InvalidOperationException("Cannot simulate the physics scene as it is invalid.");
        }

        #region Linecast

        public RaycastHit2D Linecast(Vector2 start, Vector2 end, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return Linecast_Internal(this, start, end, contactFilter);
        }

        public RaycastHit2D Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter)
        {
            return Linecast_Internal(this, start, end, contactFilter);
        }

        public int Linecast(Vector2 start, Vector2 end, RaycastHit2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return LinecastArray_Internal(this, start, end, contactFilter, results);
        }

        public int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return LinecastArray_Internal(this, start, end, contactFilter, results);
        }

        public int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, List<RaycastHit2D> results)
        {
            return LinecastList_Internal(this, start, end, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("Linecast_Binding")]
        extern private static RaycastHit2D Linecast_Internal(PhysicsScene2D physicsScene, Vector2 start, Vector2 end, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("LinecastArray_Binding")]
        extern private static int LinecastArray_Internal(PhysicsScene2D physicsScene, Vector2 start, Vector2 end, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("LinecastList_Binding")]
        extern private static int LinecastList_Internal(PhysicsScene2D physicsScene, Vector2 start, Vector2 end, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        #endregion

        #region Ray Cast

        public RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return Raycast_Internal(this, origin, direction, distance, contactFilter);
        }

        public RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter)
        {
            return Raycast_Internal(this, origin, direction, distance, contactFilter);
        }

        public int Raycast(Vector2 origin, Vector2 direction, float distance, RaycastHit2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return RaycastArray_Internal(this, origin, direction, distance, contactFilter, results);
        }

        public int Raycast(Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return RaycastArray_Internal(this, origin, direction, distance, contactFilter, results);
        }

        public int Raycast(Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter, List<RaycastHit2D> results)
        {
            return RaycastList_Internal(this, origin, direction, distance, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("Raycast_Binding")]
        extern private static RaycastHit2D Raycast_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("RaycastArray_Binding")]
        extern private static int RaycastArray_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("RaycastList_Binding")]
        extern private static int RaycastList_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        #endregion

        #region Circle Cast

        public RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return CircleCast_Internal(this, origin, radius, direction, distance, contactFilter);
        }

        public RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter)
        {
            return CircleCast_Internal(this, origin, radius, direction, distance, contactFilter);
        }

        public int CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, RaycastHit2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return CircleCastArray_Internal(this, origin, radius, direction, distance, contactFilter, results);
        }

        public int CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return CircleCastArray_Internal(this, origin, radius, direction, distance, contactFilter, results);
        }

        public int CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter, List<RaycastHit2D> results)
        {
            return CircleCastList_Internal(this, origin, radius, direction, distance, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CircleCast_Binding")]
        extern private static RaycastHit2D CircleCast_Internal(PhysicsScene2D physicsScene, Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CircleCastArray_Binding")]
        extern private static int CircleCastArray_Internal(PhysicsScene2D physicsScene, Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CircleCastList_Binding")]
        extern private static int CircleCastList_Internal(PhysicsScene2D physicsScene, Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        #endregion

        #region Box Cast

        public RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return BoxCast_Internal(this, origin, size, angle, direction, distance, contactFilter);
        }

        public RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter)
        {
            return BoxCast_Internal(this, origin, size, angle, direction, distance, contactFilter);
        }

        public int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, RaycastHit2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return BoxCastArray_Internal(this, origin, size, angle, direction, distance, contactFilter, results);
        }

        public int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return BoxCastArray_Internal(this, origin, size, angle, direction, distance, contactFilter, results);
        }

        public int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, List<RaycastHit2D> results)
        {
            return BoxCastList_Internal(this, origin, size, angle, direction, distance, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("BoxCast_Binding")]
        extern private static RaycastHit2D BoxCast_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("BoxCastArray_Binding")]
        extern private static int BoxCastArray_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("BoxCastList_Binding")]
        extern private static int BoxCastList_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        #endregion

        #region Capsule Cast

        public RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return CapsuleCast_Internal(this, origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        public RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter)
        {
            return CapsuleCast_Internal(this, origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        public int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, RaycastHit2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return CapsuleCastArray_Internal(this, origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        public int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return CapsuleCastArray_Internal(this, origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        public int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, List<RaycastHit2D> results)
        {
            return CapsuleCastList_Internal(this, origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CapsuleCast_Binding")]
        extern private static RaycastHit2D CapsuleCast_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CapsuleCastArray_Binding")]
        extern private static int CapsuleCastArray_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CapsuleCastList_Binding")]
        extern private static int CapsuleCastList_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        #endregion

        #region Ray Intersection

        public RaycastHit2D GetRayIntersection(Ray ray, float distance, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return GetRayIntersection_Internal(this, ray.origin, ray.direction, distance, layerMask);
        }

        public int GetRayIntersection(Ray ray, float distance, RaycastHit2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return GetRayIntersectionArray_Internal(this, ray.origin, ray.direction, distance, layerMask, results);
        }

        public int GetRayIntersection(Ray ray, float distance, List<RaycastHit2D> results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return GetRayIntersectionList_Internal(this, ray.origin, ray.direction, distance, layerMask, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRayIntersection_Binding")]
        extern private static RaycastHit2D GetRayIntersection_Internal(PhysicsScene2D physicsScene, Vector3 origin, Vector3 direction, float distance, int layerMask);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRayIntersectionArray_Binding")]
        extern private static int GetRayIntersectionArray_Internal(PhysicsScene2D physicsScene, Vector3 origin, Vector3 direction, float distance, int layerMask, [NotNull] RaycastHit2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRayIntersectionList_Binding")]
        extern private static int GetRayIntersectionList_Internal(PhysicsScene2D physicsScene, Vector3 origin, Vector3 direction, float distance, int layerMask, [NotNull] List<RaycastHit2D> results);

        #endregion

        #region Overlap Point

        public Collider2D OverlapPoint(Vector2 point, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapPoint_Internal(this, point, contactFilter);
        }

        public Collider2D OverlapPoint(Vector2 point, ContactFilter2D contactFilter)
        {
            return OverlapPoint_Internal(this, point, contactFilter);
        }

        public int OverlapPoint(Vector2 point, Collider2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapPointArray_Internal(this, point, contactFilter, results);
        }

        public int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return OverlapPointArray_Internal(this, point, contactFilter, results);
        }

        public int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapPointList_Internal(this, point, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapPoint_Binding")]
        extern private static Collider2D OverlapPoint_Internal(PhysicsScene2D physicsScene, Vector2 point, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapPointArray_Binding")]
        extern private static int OverlapPointArray_Internal(PhysicsScene2D physicsScene, Vector2 point, ContactFilter2D contactFilter, [NotNull][Unmarshalled] Collider2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapPointList_Binding")]
        extern private static int OverlapPointList_Internal(PhysicsScene2D physicsScene, Vector2 point, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        #endregion

        #region Overlap Circle

        public Collider2D OverlapCircle(Vector2 point, float radius, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCircle_Internal(this, point, radius, contactFilter);
        }

        public Collider2D OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter)
        {
            return OverlapCircle_Internal(this, point, radius, contactFilter);
        }

        public int OverlapCircle(Vector2 point, float radius, Collider2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity,  Mathf.Infinity);
            return OverlapCircleArray_Internal(this, point, radius, contactFilter, results);
        }

        public int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return OverlapCircleArray_Internal(this, point, radius, contactFilter, results);
        }

        public int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapCircleList_Internal(this, point, radius, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCircle_Binding")]
        extern private static Collider2D OverlapCircle_Internal(PhysicsScene2D physicsScene, Vector2 point, float radius, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCircleArray_Binding")]
        extern private static int OverlapCircleArray_Internal(PhysicsScene2D physicsScene, Vector2 point, float radius, ContactFilter2D contactFilter, [NotNull][Unmarshalled] Collider2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCircleList_Binding")]
        extern private static int OverlapCircleList_Internal(PhysicsScene2D physicsScene, Vector2 point, float radius, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        #endregion

        #region Overlap Box

        public Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapBox_Internal(this, point, size, angle, contactFilter);
        }

        public Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter)
        {
            return OverlapBox_Internal(this, point, size, angle, contactFilter);
        }

        public int OverlapBox(Vector2 point, Vector2 size, float angle, Collider2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapBoxArray_Internal(this, point, size, angle, contactFilter, results);
        }

        public int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return OverlapBoxArray_Internal(this, point, size, angle, contactFilter, results);
        }

        public int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapBoxList_Internal(this, point, size, angle, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapBox_Binding")]
        extern private static Collider2D OverlapBox_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapBoxArray_Binding")]
        extern private static int OverlapBoxArray_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, [NotNull][Unmarshalled] Collider2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapBoxList_Binding")]
        extern private static int OverlapBoxList_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        #endregion

        #region Overlap Area

        public Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapAreaToBoxArray_Internal(pointA, pointB, contactFilter);
        }

        public Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter)
        {
            return OverlapAreaToBoxArray_Internal(pointA, pointB, contactFilter);
        }

        private Collider2D OverlapAreaToBoxArray_Internal(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter)
        {
            Vector2 point = (pointA + pointB) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(pointA.x - pointB.x), Math.Abs(pointA.y - pointB.y));
            return OverlapBox(point, size, 0.0f, contactFilter);
        }

        public int OverlapArea(Vector2 pointA, Vector2 pointB, Collider2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapAreaToBoxArray_Internal(pointA, pointB, contactFilter, results);
        }

        public int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return OverlapAreaToBoxArray_Internal(pointA, pointB, contactFilter, results);
        }

        private int OverlapAreaToBoxArray_Internal(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, Collider2D[] results)
        {
            Vector2 point = (pointA + pointB) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(pointA.x - pointB.x), Math.Abs(pointA.y - pointB.y));
            return OverlapBox(point, size, 0.0f, contactFilter, results);
        }

        public int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapAreaToBoxList_Internal(pointA, pointB, contactFilter, results);
        }

        private int OverlapAreaToBoxList_Internal(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            Vector2 point = (pointA + pointB) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(pointA.x - pointB.x), Math.Abs(pointA.y - pointB.y));
            return OverlapBox(point, size, 0.0f, contactFilter, results);
        }

        #endregion

        #region Overlap Capsule

        public Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCapsule_Internal(this, point, size, direction, angle, contactFilter);
        }

        public Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter)
        {
            return OverlapCapsule_Internal(this, point, size, direction, angle, contactFilter);
        }

        public int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCapsuleArray_Internal(this, point, size, direction, angle, contactFilter, results);
        }

        public int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return OverlapCapsuleArray_Internal(this, point, size, direction, angle, contactFilter, results);
        }

        public int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapCapsuleList_Internal(this, point, size, direction, angle, contactFilter, results);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCapsule_Binding")]
        extern private static Collider2D OverlapCapsule_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCapsuleArray_Binding")]
        extern private static int OverlapCapsuleArray_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, [NotNull] [Unmarshalled] Collider2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCapsuleList_Binding")]
        extern private static int OverlapCapsuleList_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);
        #endregion

        #region Overlap Collider

        public static int OverlapCollider(Collider2D collider, Collider2D[] results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapColliderFilteredArray_Internal(collider, contactFilter, results);
        }

        public static int OverlapCollider(Collider2D collider, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return OverlapColliderFilteredArray_Internal(collider, contactFilter, results);
        }

        public static int OverlapCollider(Collider2D collider, List<Collider2D> results)
        {
            return OverlapColliderList_Internal(collider, results);
        }

        public static int OverlapCollider(Collider2D collider, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapColliderFilteredList_Internal(collider, contactFilter, results);
        }

        public static int OverlapCollider(Vector2 position, float angle, Collider2D collider, List<Collider2D> results)
        {
            if (collider.attachedRigidbody)
                return OverlapColliderFromList_Internal(position, angle, collider, results);

            throw new InvalidOperationException("Cannot perform a Collider Overlap at a specific position and angle if the Collider is not attached to a Rigidbody2D.");
        }

        public static int OverlapCollider(Vector2 position, float angle, Collider2D collider, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            if (collider.attachedRigidbody)
                return OverlapColliderFromFilteredList_Internal(position, angle, collider, contactFilter, results);

            throw new InvalidOperationException("Cannot perform a Collider Overlap at a specific position and angle if the Collider is not attached to a Rigidbody2D.");
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapColliderFilteredArray_Binding")]
        extern private static int OverlapColliderFilteredArray_Internal([NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] [Unmarshalled] Collider2D[] results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapColliderList_Binding")]
        extern private static int OverlapColliderList_Internal([NotNull] Collider2D collider, [NotNull] List<Collider2D> results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapColliderFilteredList_Binding")]
        extern private static int OverlapColliderFilteredList_Internal([NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapColliderFromList_Binding")]
        extern private static int OverlapColliderFromList_Internal(Vector2 position, float angle, [NotNull] Collider2D collider, [NotNull] List<Collider2D> results);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapColliderFromFilteredList_Binding")]
        extern private static int OverlapColliderFromFilteredList_Internal(Vector2 position, float angle, [NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        #endregion
    }

    public static class PhysicsSceneExtensions2D
    {
        public static PhysicsScene2D GetPhysicsScene2D(this Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Cannot get physics scene; Unity scene is invalid.", "scene");

            PhysicsScene2D physicsScene = GetPhysicsScene_Internal(scene);
            if (physicsScene.IsValid())
                return physicsScene;

            throw new Exception("The physics scene associated with the Unity scene is invalid.");
        }

        [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
        [NativeMethod("GetPhysicsSceneFromUnityScene")]
        extern private static PhysicsScene2D GetPhysicsScene_Internal(Scene scene);
    }

    #endregion

    [NativeHeader("Physics2DScriptingClasses.h")]
    [NativeHeader("Modules/Physics2D/PhysicsManager2D.h")]
    [NativeHeader("Physics2DScriptingClasses.h")]
    [StaticAccessor("GetPhysicsManager2D()", StaticAccessorType.Arrow)]
    public partial class Physics2D
    {
        #region Global Physics Settings

        public const int IgnoreRaycastLayer = 1 << 2;
        public const int DefaultRaycastLayers = ~Physics2D.IgnoreRaycastLayer;
        public const int AllLayers = ~0;

        // This should match Box2D "box2d_b2_maxPolygonVertices"
        public const int MaxPolygonShapeVertices = 8;

        public static PhysicsScene2D defaultPhysicsScene { get { return new PhysicsScene2D(); } }

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
        extern public static bool callbacksOnDisable { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool reuseCollisionCallbacks { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool autoSyncTransforms { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static SimulationMode2D simulationMode { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static LayerMask simulationLayers { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool useSubStepping { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static bool useSubStepContacts { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float minSubStepFPS { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static int maxSubStepCount { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static PhysicsJobOptions2D jobOptions { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float bounceThreshold { get; set; }

        [StaticAccessor("GetPhysics2DSettings()")]
        extern public static float contactThreshold { get; set; }

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

        // Needs to match "Physics2DSettings.h"
        [Flags]
        internal enum GizmoOptions
        {
            AllColliders        = 1 << 0,
            CollidersOutlined   = 1 << 1,
            CollidersFilled     = 1 << 2,
            CollidersSleeping   = 1 << 3,
            ColliderContacts    = 1 << 4,
            ColliderBounds      = 1 << 5
        };

        #endregion

        #region Simulation

        // Perform a manual simulation step.
        [ExcludeFromDocs]
        public static bool Simulate(float deltaTime)
        {
            return Simulate_Internal(defaultPhysicsScene, deltaTime, Physics2D.AllLayers);
        }

        public static bool Simulate(float deltaTime, [DefaultValue("Physics2D.AllLayers")] int simulationLayers = Physics2D.AllLayers)
        {
            return Simulate_Internal(defaultPhysicsScene, deltaTime, simulationLayers);
        }

        [NativeMethod("Simulate_Binding")]
        extern internal static bool Simulate_Internal(PhysicsScene2D physicsScene, float deltaTime, int simulationLayers);

        // Sync transform changes.
        extern public static void SyncTransforms();

        #endregion

        #region Collisions and Queries.

        #region Ignore Collision

        // Ignore collisions between specific colliders.
        [ExcludeFromDocs]
        public static void IgnoreCollision(Collider2D collider1, Collider2D collider2) { IgnoreCollision(collider1, collider2, true); }
        [StaticAccessor("PhysicsScene2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("IgnoreCollision_Binding")]
        extern public static void IgnoreCollision([NotNull] Collider2D collider1, [NotNull] Collider2D collider2, [DefaultValue("true")] bool ignore);

        // Get whether collisions between specific colliders are ignored or not.
        [StaticAccessor("PhysicsScene2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetIgnoreCollision_Binding")]
        extern public static bool GetIgnoreCollision([NotNull] Collider2D collider1, [NotNull] Collider2D collider2);

        // Ignore collisions between specific layers.
        [ExcludeFromDocs]
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

        #endregion

        #region Layer Collision

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

        #endregion

        #region Is Touching

        // Get whether specific colliders are currently touching or not.
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        extern public static bool IsTouching([NotNull] Collider2D collider1, [NotNull] Collider2D collider2);

        // Get whether specific colliders are currently touching or not (using the contact filter).
        public static bool IsTouching(Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter) { return IsTouching_TwoCollidersWithFilter(collider1, collider2, contactFilter); }
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("IsTouching")]
        extern private static bool IsTouching_TwoCollidersWithFilter([NotNull] Collider2D collider1, [NotNull] Collider2D collider2, ContactFilter2D contactFilter);

        // Get whether the specific collider is touching anything (using the contact filter).
        public static bool IsTouching(Collider2D collider, ContactFilter2D contactFilter) { return IsTouching_SingleColliderWithFilter(collider, contactFilter); }
        [NativeMethod("IsTouching")]
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        extern private static bool IsTouching_SingleColliderWithFilter([NotNull] Collider2D collider, ContactFilter2D contactFilter);

        // Get whether the specific collider is touching the specific layer(s).
        [ExcludeFromDocs]
        public static bool IsTouchingLayers(Collider2D collider) { return IsTouchingLayers(collider, Physics2D.AllLayers); }
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        extern public static bool IsTouchingLayers([NotNull] Collider2D collider, [DefaultValue("Physics2D.AllLayers")] int layerMask);

        #endregion

        #region Distance

        // Get the shortest distance and the respective points between two colliders.
        public static ColliderDistance2D Distance(Collider2D colliderA, Collider2D colliderB)
        {
            if (colliderA == colliderB)
                throw new ArgumentException("Cannot calculate the distance between the same collider.");

            return Distance_Internal(colliderA, colliderB);
        }

        // Get the shortest distance and the respective points between two colliders at specific poses.
        public static ColliderDistance2D Distance(
            Collider2D colliderA, Vector2 positionA, float angleA,
            Collider2D colliderB, Vector2 positionB, float angleB)
        {
            if (colliderA == colliderB)
                throw new ArgumentException("Cannot calculate the distance between the same collider.");

            if (!colliderA.attachedRigidbody || !colliderB.attachedRigidbody)
                throw new InvalidOperationException("Cannot perform a Collider Distance at a specific position and angle if the Collider is not attached to a Rigidbody2D.");

            return DistanceFrom_Internal(
                colliderA, positionA, angleA,
                colliderB, positionB, angleB);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("Distance")]
        extern private static ColliderDistance2D Distance_Internal([NotNull] Collider2D colliderA, [NotNull] Collider2D colliderB);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("DistanceFrom")]
        extern private static ColliderDistance2D DistanceFrom_Internal(
            [NotNull] Collider2D colliderA, Vector2 positionA, float angleA,
            [NotNull] Collider2D colliderB, Vector2 positionB, float angleB);

        // Get the closest point to position on the specified collider.
        public static Vector2 ClosestPoint(Vector2 position, Collider2D collider)
        {
            if (collider == null)
                throw new ArgumentNullException("Collider cannot be NULL.");

            return ClosestPoint_Collider(position, collider);
        }

        // Get the closest point to position on the specified rigidbody.
        public static Vector2 ClosestPoint(Vector2 position, Rigidbody2D rigidbody)
        {
            if (rigidbody == null)
                throw new ArgumentNullException("Rigidbody cannot be NULL.");

            return ClosestPoint_Rigidbody(position, rigidbody);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("ClosestPoint")]
        extern private static Vector2 ClosestPoint_Collider(Vector2 position, [NotNull] Collider2D collider);

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("ClosestPoint")]
        extern private static Vector2 ClosestPoint_Rigidbody(Vector2 position, [NotNull] Rigidbody2D rigidbody);

        #endregion

        #region Linecast

        // Returns the first hit along the specified line.
        [ExcludeFromDocs]
        public static RaycastHit2D Linecast(Vector2 start, Vector2 end)
        {
            return defaultPhysicsScene.Linecast(start, end);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.Linecast(start, end, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.Linecast(start, end, contactFilter);
        }

        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.Linecast(start, end, contactFilter);
        }

        // Returns all hits along the line (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        public static int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, List<RaycastHit2D> results)
        {
            return defaultPhysicsScene.Linecast(start, end, contactFilter, results);
        }

        // Returns all hits along the specified line.
        [ExcludeFromDocs]
        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return LinecastAll_Internal(defaultPhysicsScene, start, end, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return LinecastAll_Internal(defaultPhysicsScene, start, end, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return LinecastAll_Internal(defaultPhysicsScene, start, end, contactFilter);
        }

        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return LinecastAll_Internal(defaultPhysicsScene, start, end, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("LinecastAll_Binding")]
        extern private static RaycastHit2D[] LinecastAll_Internal(PhysicsScene2D physicsScene, Vector2 start, Vector2 end, ContactFilter2D contactFilter);

        #endregion

        #region Ray Cast

        // NOTE: This cannot be made obsolete right now as it's used in the "com.unity.xr.interationtoolkit:TrackedDeviceGraphicRaycaster".
        [ExcludeFromDocs]
        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, distance);
        }

        [RequiredByNativeCode]
        [ExcludeFromDocs]
        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter);
        }

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter);
        }

        // Returns all hits along the ray (limited by the size of the array) but filters using ContactFilter2D..  This does not produce any garbage.
        [ExcludeFromDocs]
        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, contactFilter, results);
        }

        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return defaultPhysicsScene.Raycast(origin, direction, distance, contactFilter, results);
        }

        // Returns all hits along the ray.
        [ExcludeFromDocs]
        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return RaycastAll_Internal(defaultPhysicsScene, origin, direction, Mathf.Infinity, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return RaycastAll_Internal(defaultPhysicsScene, origin, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return RaycastAll_Internal(defaultPhysicsScene, origin, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return RaycastAll_Internal(defaultPhysicsScene, origin, direction, distance, contactFilter);
        }

        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return RaycastAll_Internal(defaultPhysicsScene, origin, direction, distance, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("RaycastAll_Binding")]
        extern private static RaycastHit2D[] RaycastAll_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 direction, float distance, ContactFilter2D contactFilter);

        #endregion

        #region Circle Cast

        // Returns the first hit when casting the circle.
        [ExcludeFromDocs]
        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, Mathf.Infinity);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter);
        }

        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter);
        }

        // Returns all hits when casting the circle (limited by the size of the array) but filters using ContactFilter2D. This does not produce any garbage.
        [ExcludeFromDocs]
        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, Mathf.Infinity, contactFilter, results);
        }

        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return defaultPhysicsScene.CircleCast(origin, radius, direction, distance, contactFilter, results);
        }

        // Returns all hits when casting the circle.
        [ExcludeFromDocs]
        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return CircleCastAll_Internal(defaultPhysicsScene, origin, radius, direction, Mathf.Infinity, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return CircleCastAll_Internal(defaultPhysicsScene, origin, radius, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return CircleCastAll_Internal(defaultPhysicsScene, origin, radius, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return CircleCastAll_Internal(defaultPhysicsScene, origin, radius, direction, distance, contactFilter);
        }

        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return CircleCastAll_Internal(defaultPhysicsScene, origin, radius, direction, distance, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CircleCastAll_Binding")]
        extern private static RaycastHit2D[] CircleCastAll_Internal(PhysicsScene2D physicsScene, Vector2 origin, float radius, Vector2 direction, float distance, ContactFilter2D contactFilter);

        #endregion

        #region Box Cast

        // Returns the first hit when casting the box.
        [ExcludeFromDocs]
        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, Mathf.Infinity);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter);
        }

        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("Physics2D.AllLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter);
        }

        // Returns all hits when casting the box (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        [ExcludeFromDocs]
        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, Mathf.Infinity, contactFilter, results);
        }

        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return defaultPhysicsScene.BoxCast(origin, size, angle, direction, distance, contactFilter, results);
        }

        // Returns all hits when casting the box.
        [ExcludeFromDocs]
        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return BoxCastAll_Internal(defaultPhysicsScene, origin, size, angle, direction, Mathf.Infinity, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return BoxCastAll_Internal(defaultPhysicsScene, origin, size, angle, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return BoxCastAll_Internal(defaultPhysicsScene, origin, size, angle, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return BoxCastAll_Internal(defaultPhysicsScene, origin, size, angle, direction, distance, contactFilter);
        }

        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return BoxCastAll_Internal(defaultPhysicsScene, origin, size, angle, direction, distance, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("BoxCastAll_Binding")]
        extern private static RaycastHit2D[] BoxCastAll_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter);

        #endregion

        #region Capsule Cast

        // Returns the first hit when casting the capsule.
        [ExcludeFromDocs]
        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, Mathf.Infinity);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        // Returns all hits when casting the capsule (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        [ExcludeFromDocs]
        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction,  ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, Mathf.Infinity, contactFilter, results);
        }

        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction,  ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction,  ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return defaultPhysicsScene.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, contactFilter, results);
        }

        // Returns all hits when casting the capsule.
        [ExcludeFromDocs]
        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return CapsuleCastAll_Internal(defaultPhysicsScene, origin, size, capsuleDirection, angle, direction, Mathf.Infinity, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return CapsuleCastAll_Internal(defaultPhysicsScene, origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("CapsuleCastAll_Binding")]
        extern private static RaycastHit2D[] CapsuleCastAll_Internal(PhysicsScene2D physicsScene, Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter);

        [ExcludeFromDocs]
        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return CapsuleCastAll_Internal(defaultPhysicsScene, origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return CapsuleCastAll_Internal(defaultPhysicsScene, origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return CapsuleCastAll_Internal(defaultPhysicsScene, origin, size, capsuleDirection, angle, direction, distance, contactFilter);
        }

        #endregion

        #region Ray Intersection

        // Returns the first hit intersecting the 3D ray.
        [ExcludeFromDocs]
        public static RaycastHit2D GetRayIntersection(Ray ray)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, Mathf.Infinity, DefaultRaycastLayers);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D GetRayIntersection(Ray ray, float distance)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, distance, DefaultRaycastLayers);
        }

        public static RaycastHit2D GetRayIntersection(Ray ray, float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, distance, layerMask);
        }

        public static int GetRayIntersection(Ray ray, float distance, List<RaycastHit2D> results, [DefaultValue("Physics2D.DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, distance, results, layerMask);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] GetRayIntersectionAll(Ray ray)
        {
            return GetRayIntersectionAll_Internal(defaultPhysicsScene, ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers);
        }

        [ExcludeFromDocs]
        public static RaycastHit2D[] GetRayIntersectionAll(Ray ray, float distance)
        {
            return GetRayIntersectionAll_Internal(defaultPhysicsScene, ray.origin, ray.direction, distance, DefaultRaycastLayers);
        }

        // Needs the [RequiredByNativeCode] attribute as it is called by reflection
        // from GraphicsRaycaster.cs, to avoid a hard dependency to this module.
        [RequiredByNativeCode]
        public static RaycastHit2D[] GetRayIntersectionAll(Ray ray, float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return GetRayIntersectionAll_Internal(defaultPhysicsScene, ray.origin, ray.direction, distance, layerMask);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRayIntersectionAll_Binding")]
        extern static RaycastHit2D[] GetRayIntersectionAll_Internal(PhysicsScene2D physicsScene, Vector3 origin, Vector3 direction, float distance, int layerMask);

        // Needs the [RequiredByNativeCode] attribute as it is called by reflection
        // from GraphicsRaycaster.cs, to avoid a hard dependency to this module.
        [ExcludeFromDocs]
        [RequiredByNativeCode]
        public static int GetRayIntersectionNonAlloc(Ray ray, RaycastHit2D[] results, float distance, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics2D.DefaultRaycastLayers)
        {
            return defaultPhysicsScene.GetRayIntersection(ray, distance, results, layerMask);
        }

        #endregion

        #region Overlap Point

        // Returns a collider overlapping the point.
        [ExcludeFromDocs]
        public static Collider2D OverlapPoint(Vector2 point)
        {
            return defaultPhysicsScene.OverlapPoint(point);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapPoint(Vector2 point, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapPoint(Vector2 point, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter);
        }

        public static Collider2D OverlapPoint(Vector2 point, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapPoint(point, contactFilter);
        }

        // Returns all colliders overlapping the point (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, [Unmarshalled] Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        public static int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return defaultPhysicsScene.OverlapPoint(point, contactFilter, results);
        }

        // Returns all colliders overlapping the point.
        [ExcludeFromDocs]
        public static Collider2D[] OverlapPointAll(Vector2 point)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return OverlapPointAll_Internal(defaultPhysicsScene, point, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapPointAll(Vector2 point, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapPointAll_Internal(defaultPhysicsScene, point, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapPointAll(Vector2 point, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return OverlapPointAll_Internal(defaultPhysicsScene, point, contactFilter);
        }

        public static Collider2D[] OverlapPointAll(Vector2 point, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return OverlapPointAll_Internal(defaultPhysicsScene, point, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapPointAll_Binding")]
        extern private static Collider2D[] OverlapPointAll_Internal(PhysicsScene2D physicsScene, Vector2 point, ContactFilter2D contactFilter);

        #endregion

        #region Overlap Circle

        // Returns a collider overlapping the circle.
        [ExcludeFromDocs]
        public static Collider2D OverlapCircle(Vector2 point, float radius)
        {
            return defaultPhysicsScene.OverlapCircle(point, radius);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapCircle(Vector2 point, float radius, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapCircle(Vector2 point, float radius, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter);
        }

        public static Collider2D OverlapCircle(Vector2 point, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter);
        }

        // Returns all colliders overlapping the circle (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        public static int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return defaultPhysicsScene.OverlapCircle(point, radius, contactFilter, results);
        }

        // Returns all colliders overlapping the circle.
        [ExcludeFromDocs]
        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCircleAll_Internal(defaultPhysicsScene, point, radius, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCircleAll_Internal(defaultPhysicsScene, point, radius, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return OverlapCircleAll_Internal(defaultPhysicsScene, point, radius, contactFilter);
        }

        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return OverlapCircleAll_Internal(defaultPhysicsScene, point, radius, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCircleAll_Binding")]
        extern private static Collider2D[] OverlapCircleAll_Internal(PhysicsScene2D physicsScene, Vector2 point, float radius, ContactFilter2D contactFilter);

        #endregion

        #region Overlap Box

        // Returns a collider overlapping the box.
        [ExcludeFromDocs]
        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle)
        {
            return defaultPhysicsScene.OverlapBox(point, size, angle);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter);
        }

        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter);
        }

        // Returns all colliders overlapping the box (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        public static int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return defaultPhysicsScene.OverlapBox(point, size, angle, contactFilter, results);
        }

        // Returns all colliders overlapping the box.
        [ExcludeFromDocs]
        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return OverlapBoxAll_Internal(defaultPhysicsScene, point, size, angle, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapBoxAll_Internal(defaultPhysicsScene, point, size, angle, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return OverlapBoxAll_Internal(defaultPhysicsScene, point, size, angle, contactFilter);
        }

        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return OverlapBoxAll_Internal(defaultPhysicsScene, point, size, angle, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapBoxAll_Binding")]
        extern private static Collider2D[] OverlapBoxAll_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter);

        #endregion

        #region Overlap Area

        // Returns a collider overlapping the area.
        [ExcludeFromDocs]
        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB)
        {
            return defaultPhysicsScene.OverlapArea(pointA, pointB);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter);
        }

        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter);
        }

        // Returns all colliders overlapping the area (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        public static int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return defaultPhysicsScene.OverlapArea(pointA, pointB, contactFilter, results);
        }

        // Returns all colliders overlapping the area.
        [ExcludeFromDocs]
        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB)
        {
            return OverlapAreaAllToBox_Internal(pointA, pointB, DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, int layerMask)
        {
            return OverlapAreaAllToBox_Internal(pointA, pointB, layerMask, -Mathf.Infinity, Mathf.Infinity);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth)
        {
            return OverlapAreaAllToBox_Internal(pointA, pointB, layerMask, minDepth, Mathf.Infinity);
        }

        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            return OverlapAreaAllToBox_Internal(pointA, pointB, layerMask, minDepth, maxDepth);
        }

        private static Collider2D[] OverlapAreaAllToBox_Internal(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth, float maxDepth)
        {
            Vector2 point = (pointA + pointB) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(pointA.x - pointB.x), Math.Abs(pointA.y - pointB.y));
            return OverlapBoxAll(point, size, 0.0f, layerMask, minDepth, maxDepth);
        }

        #endregion

        #region Overlap Capsule

        // Returns a collider overlapping the capsule.
        [ExcludeFromDocs]
        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle)
        {
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter);
        }

        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter);
        }

        // Returns all colliders overlapping the capsule (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        public static int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return defaultPhysicsScene.OverlapCapsule(point, size, direction, angle, contactFilter, results);
        }

        // Returns all colliders overlapping the capsule.
        [ExcludeFromDocs]
        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(DefaultRaycastLayers, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCapsuleAll_Internal(defaultPhysicsScene, point, size, direction, angle, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return OverlapCapsuleAll_Internal(defaultPhysicsScene, point, size, direction, angle, contactFilter);
        }

        [ExcludeFromDocs]
        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return OverlapCapsuleAll_Internal(defaultPhysicsScene, point, size, direction, angle, contactFilter);
        }

        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return OverlapCapsuleAll_Internal(defaultPhysicsScene, point, size, direction, angle, contactFilter);
        }

        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("OverlapCapsuleAll_Binding")]
        extern private static Collider2D[] OverlapCapsuleAll_Internal(PhysicsScene2D physicsScene, Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter);

        #endregion

        #region Overlap Collider

        // Returns all colliders overlapping the collider (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public static int OverlapCollider(Collider2D collider, ContactFilter2D contactFilter, Collider2D[] results)
        {
            return PhysicsScene2D.OverlapCollider(collider, contactFilter, results);
        }

        public static int OverlapCollider(Collider2D collider, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return PhysicsScene2D.OverlapCollider(collider, contactFilter, results);
        }

        public static int OverlapCollider(Collider2D collider, List<Collider2D> results)
        {
            return PhysicsScene2D.OverlapCollider(collider, results);
        }

        #endregion

        #region Contacts Array

        public static int GetContacts(Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return GetColliderColliderContactsArray(collider1, collider2, contactFilter, contacts);
        }

        // Get all contacts for this collider.
        public static int GetContacts(Collider2D collider, ContactPoint2D[] contacts)
        {
            return GetColliderContactsArray(collider, new ContactFilter2D().NoFilter(), contacts);
        }

        // Get filtered contacts for this collider.
        public static int GetContacts(Collider2D collider, ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return GetColliderContactsArray(collider, contactFilter, contacts);
        }

        // Get all contacts for this collider.
        public static int GetContacts(Collider2D collider, Collider2D[] colliders)
        {
            return GetColliderContactsCollidersOnlyArray(collider, new ContactFilter2D().NoFilter(), colliders);
        }

        // Get filtered contacts for this collider.
        public static int GetContacts(Collider2D collider, ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return GetColliderContactsCollidersOnlyArray(collider, contactFilter, colliders);
        }

        // Get all contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, ContactPoint2D[] contacts)
        {
            return GetRigidbodyContactsArray(rigidbody, new ContactFilter2D().NoFilter(), contacts);
        }

        // Get filtered contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return GetRigidbodyContactsArray(rigidbody, contactFilter, contacts);
        }

        // Get all contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, Collider2D[] colliders)
        {
            return GetRigidbodyContactsCollidersOnlyArray(rigidbody, new ContactFilter2D().NoFilter(), colliders);
        }

        // Get filtered contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return GetRigidbodyContactsCollidersOnlyArray(rigidbody, contactFilter, colliders);
        }

        // Gets contacts for the specified collider (non-allocating).
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetColliderContactsArray_Binding")]
        extern private static int GetColliderContactsArray([NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] ContactPoint2D[] results);

        // Gets contacts between the specified colliders (non-allocating).
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetColliderColliderContactsArray_Binding")]
        extern private static int GetColliderColliderContactsArray([NotNull] Collider2D collider1, [NotNull] Collider2D collider2, ContactFilter2D contactFilter, [NotNull] ContactPoint2D[] results);

        // Gets contacts for the specified rigidbody (non-allocating).
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRigidbodyContactsArray_Binding")]
        extern private static int GetRigidbodyContactsArray([NotNull] Rigidbody2D rigidbody, ContactFilter2D contactFilter, [NotNull] ContactPoint2D[] results);

        // Gets contacting colliders for the specified collider (non-allocating) - Colliders Only.
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetColliderContactsCollidersOnlyArray_Binding")]
        extern private static int GetColliderContactsCollidersOnlyArray([NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] [Unmarshalled] Collider2D[] results);

        // Gets contacting colliders for the specified rigidbody (non-allocating) - Colliders Only.
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRigidbodyContactsCollidersOnlyArray_Binding")]
        extern private static int GetRigidbodyContactsCollidersOnlyArray([NotNull] Rigidbody2D rigidbody, ContactFilter2D contactFilter, [NotNull] [Unmarshalled] Collider2D[] results);

        #endregion

        #region Contacts List

        public static int GetContacts(Collider2D collider1, Collider2D collider2, ContactFilter2D contactFilter, List<ContactPoint2D> contacts)
        {
            return GetColliderColliderContactsList(collider1, collider2, contactFilter, contacts);
        }

        // Get all contacts for this collider.
        public static int GetContacts(Collider2D collider, List<ContactPoint2D> contacts)
        {
            return GetColliderContactsList(collider, new ContactFilter2D().NoFilter(), contacts);
        }

        // Get filtered contacts for this collider.
        public static int GetContacts(Collider2D collider, ContactFilter2D contactFilter, List<ContactPoint2D> contacts)
        {
            return GetColliderContactsList(collider, contactFilter, contacts);
        }

        // Get all contacts for this collider.
        public static int GetContacts(Collider2D collider, List<Collider2D> colliders)
        {
            return GetColliderContactsCollidersOnlyList(collider, new ContactFilter2D().NoFilter(), colliders);
        }

        // Get filtered contacts for this collider.
        public static int GetContacts(Collider2D collider, ContactFilter2D contactFilter, List<Collider2D> colliders)
        {
            return GetColliderContactsCollidersOnlyList(collider, contactFilter, colliders);
        }

        // Get all contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, List<ContactPoint2D> contacts)
        {
            return GetRigidbodyContactsList(rigidbody, new ContactFilter2D().NoFilter(), contacts);
        }

        // Get filtered contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, ContactFilter2D contactFilter, List<ContactPoint2D> contacts)
        {
            return GetRigidbodyContactsList(rigidbody, contactFilter, contacts);
        }

        // Get all contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, List<Collider2D> colliders)
        {
            return GetRigidbodyContactsCollidersOnlyList(rigidbody, new ContactFilter2D().NoFilter(), colliders);
        }

        // Get filtered contacts for this rigidbody.
        public static int GetContacts(Rigidbody2D rigidbody, ContactFilter2D contactFilter, List<Collider2D> colliders)
        {
            return GetRigidbodyContactsCollidersOnlyList(rigidbody, contactFilter, colliders);
        }

        // Gets contacts for the specified collider (non-allocating).
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetColliderContactsList_Binding")]
        extern private static int GetColliderContactsList([NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] List<ContactPoint2D> results);

        // Gets contacts between the specified colliders (non-allocating).
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetColliderColliderContactsList_Binding")]
        extern private static int GetColliderColliderContactsList([NotNull] Collider2D collider1, [NotNull] Collider2D collider2, ContactFilter2D contactFilter, [NotNull] List<ContactPoint2D> results);

        // Gets contacts for the specified rigidbody (non-allocating).
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRigidbodyContactsList_Binding")]
        extern private static int GetRigidbodyContactsList([NotNull] Rigidbody2D rigidbody, ContactFilter2D contactFilter, [NotNull] List<ContactPoint2D> results);

        // Gets contacting colliders for the specified collider (non-allocating) - Colliders Only.
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetColliderContactsCollidersOnlyList_Binding")]
        extern private static int GetColliderContactsCollidersOnlyList([NotNull] Collider2D collider, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        // Gets contacting colliders for the specified rigidbody (non-allocating) - Colliders Only.
        [StaticAccessor("PhysicsQuery2D", StaticAccessorType.DoubleColon)]
        [NativeMethod("GetRigidbodyContactsCollidersOnlyList_Binding")]
        extern private static int GetRigidbodyContactsCollidersOnlyList([NotNull] Rigidbody2D rigidbody, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        #endregion

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

    public enum SimulationMode2D
    {
        FixedUpdate = 0,
        Update      = 1,
        Script      = 2
    }

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
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

    public enum ColliderErrorState2D
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

    public enum JointBreakAction2D
    {
        // Ignore any joint break.
        Ignore = 0,

        // Perform a callback only for a joint break.
        CallbackOnly = 1,

        // Disable the Joint for a joint break.
        Disable = 2,

        // Destroy the joint for a joint break.
        Destroy = 3,
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

    // The type of a physics shape.
    public enum PhysicsShapeType2D
    {
        // Circle 1-Vertex (b2CircleShape)
        Circle = 0,

        // Capsule 2-Vertex (b2CapsuleShape)
        Capsule = 1,

        // Polygon (Physics2D.MaxPolygonShapeVertices - See "box2d_b2_maxPolygonVertices") Vertex (b2PolygonShape)
        Polygon = 2,

        // Edge n-Vertex (b2Chainhape)
        Edges = 3,
    }

    // The method used to combine both material values.
    public enum PhysicsMaterialCombine2D
    {
        // The average of both material values.
        Average = 0,

        // The geometric mean of both material values.
        Mean,

        // The product of both material values.
        Multiply,

        // The minium of both material values.
        Minimum,

        // The maximum of both material values.
        Maximum
    }

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader(Header = "Modules/Physics2D/Public/PhysicsScripting2D.h")]
    public struct PhysicsShape2D
    {
        private PhysicsShapeType2D m_ShapeType;
        private float m_Radius;
        private int m_VertexStartIndex;
        private int m_VertexCount;
        private int m_UseAdjacentStart;
        private int m_UseAdjacentEnd;
        private Vector2 m_AdjacentStart;
        private Vector2 m_AdjacentEnd;

        public PhysicsShapeType2D shapeType
        {
            get { return m_ShapeType; }
            set { m_ShapeType = value; }
        }

        public float radius
        {
            get { return m_Radius; }
            set
            {
                if (value < 0.0f)
                    throw new ArgumentOutOfRangeException("radius cannot be negative.");

                if (Single.IsNaN(value) || Single.IsInfinity(value))
                    throw new ArgumentException("radius contains an invalid value.");

                m_Radius = value;
            }
        }

        public int vertexStartIndex
        {
            get { return m_VertexStartIndex; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("vertexStartIndex cannot be negative.");

                m_VertexStartIndex = value;
            }
        }

        public int vertexCount
        {
            get { return m_VertexCount; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("vertexCount cannot be less than one.");

                m_VertexCount = value;
            }
        }

        public bool useAdjacentStart
        {
            get { return m_UseAdjacentStart != 0; }
            set { m_UseAdjacentStart = value ? 1 : 0; }
        }

        public bool useAdjacentEnd
        {
            get { return m_UseAdjacentEnd != 0; }
            set { m_UseAdjacentEnd = value ? 1 : 0; }
        }

        public Vector2 adjacentStart
        {
            get { return m_AdjacentStart; }
            set
            {
                if (Single.IsNaN(value.x) ||
                    Single.IsNaN(value.y) ||
                    Single.IsInfinity(value.x) ||
                    Single.IsInfinity(value.y))
                    throw new ArgumentException("adjacentStart contains an invalid value.");

                m_AdjacentStart = value;
            }
        }

        public Vector2 adjacentEnd
        {
            get { return m_AdjacentEnd; }
            set
            {
                if (Single.IsNaN(value.x) ||
                    Single.IsNaN(value.y) ||
                    Single.IsInfinity(value.x) ||
                    Single.IsInfinity(value.y))
                    throw new ArgumentException("adjacentEnd contains an invalid value.");

                m_AdjacentEnd = value;
            }
        }
    }

    public class PhysicsShapeGroup2D
    {
        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader(Header = "Modules/Physics2D/Public/PhysicsScripting2D.h")]
        internal struct GroupState
        {
            [NativeName("shapesList")] public List<PhysicsShape2D> m_Shapes;
            [NativeName("verticesList")] public List<Vector2> m_Vertices;
            [NativeName("localToWorld")] public Matrix4x4 m_LocalToWorld;

            public void ClearGeometry()
            {
                m_Shapes.Clear();
                m_Vertices.Clear();
            }
        }

        internal GroupState m_GroupState;

        // Get the vertices.
        internal List<Vector2> groupVertices { get { return m_GroupState.m_Vertices; } }

        // Get the shapes.
        internal List<PhysicsShape2D> groupShapes { get { return m_GroupState.m_Shapes; } }

        // Get the total shape count.
        public int shapeCount { get { return m_GroupState.m_Shapes.Count; } }

        // Get the total vertex count.
        public int vertexCount { get { return m_GroupState.m_Vertices.Count; } }

        // Get/Set the local to world pose matrix.
        public Matrix4x4 localToWorldMatrix { get { return m_GroupState.m_LocalToWorld; } set { m_GroupState.m_LocalToWorld = value; } }

        // The minimum vertex separation for polygon shapes.
        // NOTE: This equates to half the linear-s;op in the physics engine.
        private const float MinVertexSeparation = 0.5f * 0.005f;

        public PhysicsShapeGroup2D([DefaultValue("1")] int shapeCapacity = 1, [DefaultValue("8")] int vertexCapacity = 8)
        {
            m_GroupState = new GroupState
            {
                m_Shapes = new List<PhysicsShape2D>(shapeCapacity),
                m_Vertices = new List<Vector2>(vertexCapacity),
                m_LocalToWorld = Matrix4x4.identity
            };
        }

        // Clears the shape group.
        public void Clear()
        {
            m_GroupState.ClearGeometry();
            m_GroupState.m_LocalToWorld = Matrix4x4.identity;
        }

        // Add a shape group to this one.
        public void Add(PhysicsShapeGroup2D physicsShapeGroup)
        {
            if (physicsShapeGroup == null)
                throw new ArgumentNullException("Cannot merge a NULL PhysicsShapeGroup2D.");

            if (physicsShapeGroup == this)
                throw new ArgumentException("Cannot merge a PhysicsShapeGroup2D with itself.");

            // Finish if no shapes.
            if (physicsShapeGroup.shapeCount == 0)
                return;

            // Fetch the index where the first new shape will be.
            var shapeIndex = groupShapes.Count;

            // Fetch index offset we need to shift the new shape vertices by.
            var startVertexOffset = m_GroupState.m_Vertices.Count;

            // Add the new shapes and vertices.
            groupShapes.AddRange(physicsShapeGroup.groupShapes);
            groupVertices.AddRange(physicsShapeGroup.groupVertices);

            // Offset the new shape indices if required.
            if (shapeIndex > 0)
            {
                // Offset all vertices for the new shapes.
                for (var i = shapeIndex; i < m_GroupState.m_Shapes.Count; ++i)
                {
                    var physicsShape = m_GroupState.m_Shapes[i];
                    physicsShape.vertexStartIndex += startVertexOffset;
                    m_GroupState.m_Shapes[i] = physicsShape;
                }
            }
        }

        // Get all the shapes and vertices.
        public void GetShapeData(List<PhysicsShape2D> shapes, List<Vector2> vertices)
        {
            shapes.AddRange(groupShapes);
            vertices.AddRange(groupVertices);
        }

        // Get all the shapes and vertices into native arrays.
        public void GetShapeData(NativeArray<PhysicsShape2D> shapes, NativeArray<Vector2> vertices)
        {
            if (!shapes.IsCreated || shapes.Length != shapeCount)
                throw new ArgumentException($"Cannot get shape data as the native shapes array length must be identical to the current custom shape count of {shapeCount}.", "shapes");

            if (!vertices.IsCreated || vertices.Length != vertexCount)
                throw new ArgumentException($"Cannot get shape data as the native vertices array length must be identical to the current custom vertex count of {shapeCount}.", "vertices");

            // Copy the shapes.
            for (var i = 0; i < shapeCount; ++i)
                shapes[i] = m_GroupState.m_Shapes[i];

            // Copy the vertices.
            for (var i = 0; i < vertexCount; ++i)
                vertices[i] = m_GroupState.m_Vertices[i];
        }

        // Get all the shape vertices.
        public void GetShapeVertices(int shapeIndex, List<Vector2> vertices)
        {
            var shape = GetShape(shapeIndex);
            var shapeVertexCount = shape.vertexCount;

            vertices.Clear();
            if (vertices.Capacity < shapeVertexCount)
                vertices.Capacity = shapeVertexCount;

            var shapeVertices = groupVertices;
            var shapeVertexIndex = shape.vertexStartIndex;
            for (var n = 0; n < shapeVertexCount; ++n)
            {
                vertices.Add(shapeVertices[shapeVertexIndex++]);
            }
        }

        // Get the shape vertex at the specified index.
        public Vector2 GetShapeVertex(int shapeIndex, int vertexIndex)
        {
            var index = GetShape(shapeIndex).vertexStartIndex + vertexIndex;
            if (index < 0 || index >= groupVertices.Count)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get shape-vertex at index {0}. There are {1} shape-vertices.", index, shapeCount));

            return groupVertices[index];
        }

        // Set the shape vertex at the specified index.
        public void SetShapeVertex(int shapeIndex, int vertexIndex, Vector2 vertex)
        {
            var index = GetShape(shapeIndex).vertexStartIndex + vertexIndex;
            if (index < 0 || index >= groupVertices.Count)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set shape-vertex at index {0}. There are {1} shape-vertices.", index, shapeCount));

            groupVertices[index] = vertex;
        }

        // Set the shape radius at the specified index.
        public void SetShapeRadius(int shapeIndex, float radius)
        {
            // Fetch the shape.
            var shape = GetShape(shapeIndex);

            // Validate the specified radius.
            switch (shape.shapeType)
            {
                case PhysicsShapeType2D.Circle:
                    if (radius <= 0.0f)
                        throw new ArgumentException(string.Format("Circle radius {0} must be greater than zero.", radius));
                    break;

                case PhysicsShapeType2D.Capsule:
                    if (radius <= 0.00001f)
                        throw new ArgumentException(string.Format("Capsule radius: {0} is too small.", radius));
                    break;

                case PhysicsShapeType2D.Edges:
                case PhysicsShapeType2D.Polygon:
                    radius = Mathf.Max(0f, radius);
                    break;
            }

            // Set the radius and update the shape.
            shape.radius = radius;
            groupShapes[shapeIndex] = shape;
        }

        // Set the shape adjacent vertices.
        public void SetShapeAdjacentVertices(
            int shapeIndex,
            bool useAdjacentStart,
            bool useAdjacentEnd,
            Vector2 adjacentStart,
            Vector2 adjacentEnd)
        {
            if (shapeIndex < 0 || shapeIndex >= shapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set shape adjacent vertices at index {0}. There are {1} shapes(s).", shapeIndex, shapeCount));

            var shape = groupShapes[shapeIndex];

            if (shape.shapeType != PhysicsShapeType2D.Edges)
                throw new InvalidOperationException(String.Format("Cannot set shape adjacent vertices at index {0}. The shape must be of type {1} but it is of typee {2}.", shapeIndex, PhysicsShapeType2D.Edges, shape.shapeType));

            shape.useAdjacentStart = useAdjacentStart;
            shape.useAdjacentEnd = useAdjacentEnd;
            shape.adjacentStart = adjacentStart;
            shape.adjacentEnd = adjacentEnd;

            groupShapes[shapeIndex] = shape;
        }

        // Deletes the shape at the specified index.
        public void DeleteShape(int shapeIndex)
        {
            if (shapeIndex < 0 || shapeIndex >= shapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot delete shape at index {0}. There are {1} shapes(s).", shapeIndex, shapeCount));

            var shape = groupShapes[shapeIndex];
            var shapeVertexCount = shape.vertexCount;

            groupShapes.RemoveAt(shapeIndex);
            groupVertices.RemoveRange(shape.vertexStartIndex, shapeVertexCount);

            // NOTE: This makes the assumption that shape vertex usage is ordered.
            while (shapeIndex < groupShapes.Count)
            {
                var offsetShape = m_GroupState.m_Shapes[shapeIndex];
                offsetShape.vertexStartIndex -= shapeVertexCount;
                m_GroupState.m_Shapes[shapeIndex++] = offsetShape;
            }
        }

        // Get the shape at the specified index.
        public PhysicsShape2D GetShape(int shapeIndex)
        {
            if (shapeIndex < 0 || shapeIndex >= shapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get shape at index {0}. There are {1} shapes(s).", shapeIndex, shapeCount));

            return groupShapes[shapeIndex];
        }

        // Add a circle shape.
        public int AddCircle(Vector2 center, float radius)
        {
            if (radius <= 0.0f)
                throw new ArgumentException(string.Format("radius {0} must be greater than zero.", radius));

            // Add geometry.
            var vertexStartIndex = groupVertices.Count;
            groupVertices.Add(center);

            // Add shape.
            groupShapes.Add(
                new PhysicsShape2D
                {
                    shapeType = PhysicsShapeType2D.Circle,
                    radius = radius,
                    vertexStartIndex = vertexStartIndex,
                    vertexCount = 1
                });

            // Return the new shape index.
            return groupShapes.Count - 1;
        }

        // Add a capsule shape.
        public int AddCapsule(Vector2 vertex0, Vector2 vertex1, float radius)
        {
            if (radius <= 0.00001f)
                throw new ArgumentException(string.Format("radius: {0} is too small.", radius));

            // Add geometry.
            var vertexStartIndex = groupVertices.Count;
            groupVertices.Add(vertex0);
            groupVertices.Add(vertex1);

            groupShapes.Add(
                new PhysicsShape2D
                {
                    shapeType = PhysicsShapeType2D.Capsule,
                    radius = radius,
                    vertexStartIndex = vertexStartIndex,
                    vertexCount = 2
                });

            // Return the new shape index.
            return groupShapes.Count - 1;
        }

        public int AddBox(Vector2 center, Vector2 size, [DefaultValue("0f")] float angle = 0f, [DefaultValue("0f")] float edgeRadius = 0f)
        {
            if (size.x <= MinVertexSeparation || size.y <= MinVertexSeparation)
                throw new ArgumentException(string.Format("size: {0} is too small. Vertex need to be separated by at least {1}", size, MinVertexSeparation));

            // Clamp the edge-radius.
            edgeRadius = Mathf.Max(0f, edgeRadius);

            // Convert to radians.
            angle *= Mathf.Deg2Rad;
            var cos = Mathf.Cos(angle);
            var sin = Mathf.Sin(angle);

            // Add geometry.
            static Vector2 Rotate(float cos, float sin, Vector2 value) { return new Vector2(cos * value.x - sin * value.y, sin * value.x + cos * value.y); }
            var halfSize = size * 0.5f;
            var vertex0 = center + Rotate(cos, sin, -halfSize);
            var vertex1 = center + Rotate(cos, sin, new Vector2(halfSize.x, -halfSize.y));
            var vertex2 = center + Rotate(cos, sin, halfSize);
            var vertex3 = center + Rotate(cos, sin, new Vector2(-halfSize.x, halfSize.y));

            var vertexStartIndex = groupVertices.Count;
            groupVertices.Add(vertex0);
            groupVertices.Add(vertex1);
            groupVertices.Add(vertex2);
            groupVertices.Add(vertex3);

            groupShapes.Add(
                new PhysicsShape2D
                {
                    shapeType = PhysicsShapeType2D.Polygon,
                    radius = edgeRadius,
                    vertexStartIndex = vertexStartIndex,
                    vertexCount = 4
                });

            // Return the new shape index.
            return groupShapes.Count - 1;
        }

        // Add a polygon shape.
        public int AddPolygon(List<Vector2> vertices)
        {
            var vertexCount = vertices.Count;

            if (vertexCount < 3 || vertexCount > Physics2D.MaxPolygonShapeVertices)
                throw new ArgumentException(string.Format("Vertex Count {0} must be >= 3 and <= {1}.", vertexCount, Physics2D.MaxPolygonShapeVertices));

            // Validate vertex separation (squared)
            float minSeparationSqr = MinVertexSeparation * MinVertexSeparation;
            for (var i = 1; i < vertexCount; ++i)
            {
                var vertex1 = vertices[i - 1];
                var vertex2 = vertices[i];

                if ((vertex2 - vertex1).sqrMagnitude <= minSeparationSqr)
                    throw new ArgumentException(string.Format("vertices: {0} and {1} are too close. Vertices need to be separated by at least {2}", vertex1, vertex2, minSeparationSqr));
            }

            // Add geometry.
            var vertexStartIndex = groupVertices.Count;
            groupVertices.AddRange(vertices);

            groupShapes.Add(
                new PhysicsShape2D
                {
                    shapeType = PhysicsShapeType2D.Polygon,
                    radius = 0f,
                    vertexStartIndex = vertexStartIndex,
                    vertexCount = vertexCount
                });

            // Return the new shape index.
            return groupShapes.Count - 1;
        }

        // Add an edge shape.
        public int AddEdges(List<Vector2> vertices, [DefaultValue("0f")] float edgeRadius = 0f)
        {
            return AddEdges(
                vertices,
                useAdjacentStart: false,
                useAdjacentEnd: false,
                adjacentStart: Vector2.zero,
                adjacentEnd: Vector2.zero,
                edgeRadius);
        }

        // Add an edge shape with adjacent vertices.
        public int AddEdges(
            List<Vector2> vertices,
            bool useAdjacentStart,
            bool useAdjacentEnd,
            Vector2 adjacentStart,
            Vector2 adjacentEnd,
            [DefaultValue("0f")] float edgeRadius = 0f)
        {
            var vertexCount = vertices.Count;
            if (vertexCount < 2)
                throw new ArgumentOutOfRangeException(string.Format("Vertex Count {0} must be >= 2.", vertexCount));

            // Clamp the edge-radius.
            edgeRadius = Mathf.Max(0f, edgeRadius);

            // Add geometry.
            var vertexStartIndex = groupVertices.Count;
            groupVertices.AddRange(vertices);

            groupShapes.Add(
                new PhysicsShape2D
                {
                    shapeType = PhysicsShapeType2D.Edges,
                    radius = edgeRadius,
                    vertexStartIndex = vertexStartIndex,
                    vertexCount = vertexCount,

                    useAdjacentStart = useAdjacentStart,
                    useAdjacentEnd = useAdjacentEnd,
                    adjacentStart = adjacentStart,
                    adjacentEnd = adjacentEnd
                });

            // Return the new shape index.
            return groupShapes.Count - 1;
        }
    }

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
    [NativeClass("ContactFilter", "struct ContactFilter;")]
    [NativeHeader("Modules/Physics2D/Public/Collider2D.h")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
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

        extern private void CheckConsistency();

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
        public bool IsFilteringTrigger(Collider2D collider) { return !useTriggers && collider.isTrigger; }
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

        extern public bool IsFilteringNormalAngle(Vector2 normal);

        public bool IsFilteringNormalAngle(float angle)
        {
            return IsFilteringNormalAngleUsingAngle(angle);
        }

        extern private bool IsFilteringNormalAngleUsingAngle(float angle);

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

    // Describes a collision.
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial class Collision2D
    {
        internal int m_Collider;
        internal int m_OtherCollider;
        internal int m_Rigidbody;
        internal int m_OtherRigidbody;
        internal Vector2 m_RelativeVelocity;
        internal int m_Enabled;
        internal int m_ContactCount;
        internal ContactPoint2D[] m_ReusedContacts;
        internal ContactPoint2D[] m_LegacyContacts;

        // Return the appropriate contacts array.
        private ContactPoint2D[] GetContacts_Internal() { return m_LegacyContacts == null ? m_ReusedContacts : m_LegacyContacts; }

        // The first collider involved in the collision.
        public Collider2D collider { get { return Object.FindObjectFromInstanceID(m_Collider) as Collider2D; } }

        // The other collider in contact.
        public Collider2D otherCollider { get { return Object.FindObjectFromInstanceID(m_OtherCollider) as Collider2D; } }

        // The rigid-body involved in the collision.
        public Rigidbody2D rigidbody { get { return Object.FindObjectFromInstanceID(m_Rigidbody) as Rigidbody2D; } }

        // The other rigid-body involved in the collision.
        public Rigidbody2D otherRigidbody { get { return Object.FindObjectFromInstanceID(m_OtherRigidbody) as Rigidbody2D; } }

        // The transform of the rigid-body or if no rigid-body is available (static collider), the collider transform.
        public Transform transform { get { return rigidbody != null ? rigidbody.transform : collider.transform; } }

        // The game object of the rigid-body or if no rigid-body is available, the collider transform.
        public GameObject gameObject { get { return rigidbody != null ? rigidbody.gameObject : collider.gameObject; } }

        // The relative velocity between the two bodies.
        public Vector2 relativeVelocity { get { return m_RelativeVelocity; } }

        // Whether the collision is enabled or not.  Effectors can temporarily disable a collision but all collisions are reported.
        public bool enabled { get { return m_Enabled == 1; } }

        // The contact points.
        public ContactPoint2D[] contacts
        {
            get
            {
                if (m_LegacyContacts == null)
                {
                    m_LegacyContacts = new ContactPoint2D[m_ContactCount];
                    Array.Copy(m_ReusedContacts, m_LegacyContacts, m_ContactCount);
                }

                return m_LegacyContacts;
            }
        }

        // Returns the number of contacts.
        public int contactCount { get { return m_ContactCount; } }

        // Get contact at specific index.
        public ContactPoint2D GetContact(int index)
        {
            if (index < 0 || index >= m_ContactCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get contact at index {0}. There are {1} contact(s).", index, m_ContactCount));

            return GetContacts_Internal()[index];
        }

        // Get contacts for this collision.
        public int GetContacts(ContactPoint2D[] contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided array is NULL.");

            var contactCount = Mathf.Min(m_ContactCount, contacts.Length);
            Array.Copy(GetContacts_Internal(), contacts, contactCount);
            return contactCount;
        }

        // Get contacts for this collision.
        public int GetContacts(List<ContactPoint2D> contacts)
        {
            if (contacts == null)
                throw new NullReferenceException("Cannot get contacts as the provided list is NULL.");

            contacts.Clear();

            // Copy only the number of contacts available as this array can become larger than the number of current contacts.
            var contactArray = GetContacts_Internal();
            for (var i = 0; i < m_ContactCount; ++i)
            {
                contacts.Add(contactArray[i]);
            }

            return m_ContactCount;
        }
    };

    // Describes a contact point where the collision occurs.
    [RequiredByNativeCode(Optional = false, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [NativeClass("ScriptingContactPoint2D", "struct ScriptingContactPoint2D;")]
    [NativeHeader("Modules/Physics2D/Public/PhysicsScripting2D.h")]
    public struct ContactPoint2D
    {
        [NativeName("point")]
        private Vector2 m_Point;

        [NativeName("normal")]
        private Vector2 m_Normal;

        [NativeName("relativeVelocity")]
        private Vector2 m_RelativeVelocity;

        [NativeName("friction")]
        private float m_Friction;

        [NativeName("bounciness")]
        private float m_Bounciness;

        [NativeName("separation")]
        private float m_Separation;

        [NativeName("normalImpulse")]
        private float m_NormalImpulse;

        [NativeName("tangentImpulse")]
        private float m_TangentImpulse;

        [NativeName("collider")]
        private int m_Collider;

        [NativeName("otherCollider")]
        private int m_OtherCollider;

        [NativeName("rigidbody")]
        private int m_Rigidbody;

        [NativeName("otherRigidbody")]
        private int m_OtherRigidbody;

        [NativeName("enabled")]
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

        // The effective friction used here (post PhysicsMaterial2D combination).
        public float friction { get { return m_Friction; } }

        // The effective bounciness used here (post PhysicsMaterial2D combination).
        public float bounciness { get { return m_Bounciness; } }        

        // The first collider in contact.
        public Collider2D collider { get { return Object.FindObjectFromInstanceID(m_Collider) as Collider2D; } }

        // The other collider in contact.
        public Collider2D otherCollider { get { return Object.FindObjectFromInstanceID(m_OtherCollider) as Collider2D; } }

        // The rigid-body involved in the collision.
        public Rigidbody2D rigidbody { get { return Object.FindObjectFromInstanceID(m_Rigidbody) as Rigidbody2D; } }

        // The other rigid-body involved in the collision.
        public Rigidbody2D otherRigidbody { get { return Object.FindObjectFromInstanceID(m_OtherRigidbody) as Rigidbody2D; } }

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
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [NativeClass("RaycastHit2D", "struct RaycastHit2D;")]
    [NativeHeader("Runtime/Interfaces/IPhysics2D.h")]
    public partial struct RaycastHit2D
    {
        [NativeName("centroid")]
        private Vector2 m_Centroid;

        [NativeName("point")]
        private Vector2 m_Point;

        [NativeName("normal")]
        private Vector2 m_Normal;

        [NativeName("distance")]
        private float m_Distance;

        [NativeName("fraction")]
        private float m_Fraction;

        [NativeName("collider")]
        private int m_Collider;

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
            get { return Object.FindObjectFromInstanceID(m_Collider) as Collider2D; }
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

    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [NativeClass("PhysicsJobOptions2D", "struct PhysicsJobOptions2D;")]
    [NativeHeader("Modules/Physics2D/Public/Physics2DSettings.h")]
    public partial struct PhysicsJobOptions2D
    {
        private bool m_UseMultithreading;
        private bool m_UseConsistencySorting;
        private int m_InterpolationPosesPerJob;
        private int m_NewContactsPerJob;
        private int m_CollideContactsPerJob;
        private int m_ClearFlagsPerJob;
        private int m_ClearBodyForcesPerJob;
        private int m_SyncDiscreteFixturesPerJob;
        private int m_SyncContinuousFixturesPerJob;
        private int m_FindNearestContactsPerJob;
        private int m_UpdateTriggerContactsPerJob;
        private int m_IslandSolverCostThreshold;
        private int m_IslandSolverBodyCostScale;
        private int m_IslandSolverContactCostScale;
        private int m_IslandSolverJointCostScale;
        private int m_IslandSolverBodiesPerJob;
        private int m_IslandSolverContactsPerJob;

        public bool useMultithreading { get { return m_UseMultithreading; } set { m_UseMultithreading = value; } }
        public bool useConsistencySorting { get { return m_UseConsistencySorting; } set { m_UseConsistencySorting = value; } }
        public int interpolationPosesPerJob { get { return m_InterpolationPosesPerJob; } set { m_InterpolationPosesPerJob = value; } }
        public int newContactsPerJob { get { return m_NewContactsPerJob; } set { m_NewContactsPerJob = value; } }
        public int collideContactsPerJob { get { return m_CollideContactsPerJob; } set { m_CollideContactsPerJob = value; } }
        public int clearFlagsPerJob { get { return m_ClearFlagsPerJob; } set { m_ClearFlagsPerJob = value; } }
        public int clearBodyForcesPerJob { get { return m_ClearBodyForcesPerJob; } set { m_ClearBodyForcesPerJob = value; } }
        public int syncDiscreteFixturesPerJob { get { return m_SyncDiscreteFixturesPerJob; } set { m_SyncDiscreteFixturesPerJob = value; } }
        public int syncContinuousFixturesPerJob { get { return m_SyncContinuousFixturesPerJob; } set { m_SyncContinuousFixturesPerJob = value; } }
        public int findNearestContactsPerJob { get { return m_FindNearestContactsPerJob; } set { m_FindNearestContactsPerJob = value; } }
        public int updateTriggerContactsPerJob { get { return m_UpdateTriggerContactsPerJob; } set { m_UpdateTriggerContactsPerJob = value; } }
        public int islandSolverCostThreshold { get { return m_IslandSolverCostThreshold; } set { m_IslandSolverCostThreshold = value; } }
        public int islandSolverBodyCostScale { get { return m_IslandSolverBodyCostScale; } set { m_IslandSolverBodyCostScale = value; } }
        public int islandSolverContactCostScale { get { return m_IslandSolverContactCostScale; } set { m_IslandSolverContactCostScale = value; } }
        public int islandSolverJointCostScale { get { return m_IslandSolverJointCostScale; } set { m_IslandSolverJointCostScale = value; } }
        public int islandSolverBodiesPerJob { get { return m_IslandSolverBodiesPerJob; } set { m_IslandSolverBodiesPerJob = value; } }
        public int islandSolverContactsPerJob { get { return m_IslandSolverContactsPerJob; } set { m_IslandSolverContactsPerJob = value; } }
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

        public void SetRotation(float angle)
        {
            SetRotation_Angle(angle);
        }

        [NativeMethod("SetRotation")]
        extern private void SetRotation_Angle(float angle);

        public void SetRotation(Quaternion rotation)
        {
            SetRotation_Quaternion(rotation);
        }

        [NativeMethod("SetRotation")]
        extern private void SetRotation_Quaternion(Quaternion rotation);

        // Moves the rigidbody to /position/ during the next fixed update.
        extern public void MovePosition(Vector2 position);

        // Rotates the rigidbody to /angle/ during the next fixed update.
        public void MoveRotation(float angle)
        {
            MoveRotation_Angle(angle);
        }

        [NativeMethod("MoveRotation")]
        extern private void MoveRotation_Angle(float angle);

        public void MoveRotation(Quaternion rotation)
        {
            MoveRotation_Quaternion(rotation);
        }

        [NativeMethod("MoveRotation")]
        extern private void MoveRotation_Quaternion(Quaternion rotation);

        [NativeMethod("MovePositionAndRotation")]
        extern public void MovePositionAndRotation(Vector2 position, float angle);

        public void MovePositionAndRotation(Vector2 position, Quaternion rotation)
        {
            MovePositionAndRotation_Quaternion(position, rotation);
        }

        [NativeMethod("MovePositionAndRotation")]
        extern private void MovePositionAndRotation_Quaternion(Vector2 position, Quaternion rotation);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader(Header = "Modules/Physics2D/Public/Rigidbody2D.h")]
        public struct SlideMovement
        {
            public SlideMovement()
            {
                maxIterations = 3;
                surfaceSlideAngle = 90f;
                gravitySlipAngle = 90f;
                surfaceUp = Vector2.up;
                surfaceAnchor = Vector2.down;
                gravity = new Vector2(0f, -9.81f);
                startPosition = Vector2.zero;
                selectedCollider = default;
                useStartPosition = default;
                useNoMove = default;
                useSimulationMove = default;
                useAttachedTriggers = default;
                useLayerMask = default;
                layerMask = Physics2D.AllLayers;
            }
            
            [field: SerializeField] public int maxIterations { get; set; }
            [field: SerializeField] public float surfaceSlideAngle { get; set; }
            [field: SerializeField] public float gravitySlipAngle { get; set; }
            [field: SerializeField] public Vector2 surfaceUp { get; set; }
            [field: SerializeField] public Vector2 surfaceAnchor { get; set; }
            [field: SerializeField] public Vector2 gravity { get; set; }
            [field: SerializeField] public Vector2 startPosition { get; set; }
            [field: SerializeField] public Collider2D selectedCollider { get; set; }
            [field: SerializeField] public LayerMask layerMask { get; set; }
            [field: SerializeField] public bool useLayerMask { get; set; }
            [field: SerializeField] public bool useStartPosition { get; set; }
            [field: SerializeField] public bool useNoMove { get; set; }
            [field: SerializeField] public bool useSimulationMove { get; set; }
            [field: SerializeField] public bool useAttachedTriggers { get; set; }

            // Helpers.
            public void SetLayerMask(LayerMask mask) { layerMask = mask; useLayerMask = true; }
            public void SetStartPosition(Vector2 position) { startPosition = position; useStartPosition = true; }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader(Header = "Modules/Physics2D/Public/Rigidbody2D.h")]
        public struct SlideResults
        {
            [field: SerializeField] public Vector2 remainingVelocity { get; set; }
            [field: SerializeField] public Vector2 position { get; set; }
            [field: SerializeField] public int iterationsUsed { get; set; }
            [field: SerializeField] public RaycastHit2D slideHit { get; set; }
            [field: SerializeField] public RaycastHit2D surfaceHit { get; set; }
        }

        public SlideResults Slide(Vector2 velocity, float deltaTime, SlideMovement slideMovement)
        {
            if (deltaTime < 0.0f)
                throw new ArgumentException($"Time cannot be negative. It is {deltaTime}.", "deltaTime");

            // Simply return if the time is zero.
            if (Mathf.Approximately(deltaTime, 0f))
                return new SlideResults() { position = slideMovement.useStartPosition ? slideMovement.startPosition : position, remainingVelocity = velocity };

            if (slideMovement.useSimulationMove && bodyType == RigidbodyType2D.Static)
                throw new ArgumentException($"Cannot use simulation move when the body type is Static. It is {slideMovement.useSimulationMove}.", "SlideMovement.useSimulationMove");

            if (slideMovement.useNoMove && slideMovement.useSimulationMove)
                throw new ArgumentException($"Cannot use no move and simulation move at the same time; the two are conflicting options. It is {slideMovement.useNoMove}.", "SlideMovement.useNoMove");

            if (slideMovement.maxIterations < 1)
                throw new ArgumentException($"Maximum Iterations must be greater than zero. It is {slideMovement.maxIterations}.", "SlideMovement.maxIterations");

            if (!float.IsFinite(slideMovement.surfaceSlideAngle) || slideMovement.surfaceSlideAngle < 0f || slideMovement.surfaceSlideAngle > 90f)
                throw new ArgumentException($"Surface Slide Angle must be in the range of 0 to 90 degrees. It is {slideMovement.surfaceSlideAngle}.", "SlideMovement.surfaceSlideAngle");

            if (!float.IsFinite(slideMovement.gravitySlipAngle) || slideMovement.gravitySlipAngle < 0f || slideMovement.gravitySlipAngle > 90f)
                throw new ArgumentException($"Gravity Slip Angle must be in the range of 0 to 90 degrees. It is {slideMovement.gravitySlipAngle}.", "SlideMovement.gravitySlipAngle");

            if (!float.IsFinite(slideMovement.surfaceUp.x) || !float.IsFinite(slideMovement.surfaceUp.y))
                throw new ArgumentException($"Surface Up is invalid. It is {slideMovement.surfaceUp}.", "SlideMovement.surfaceUp");

            if (!float.IsFinite(slideMovement.surfaceAnchor.x) || !float.IsFinite(slideMovement.surfaceAnchor.y))
                throw new ArgumentException($"Surface Anchor is invalid. It is {slideMovement.surfaceAnchor}.", "SlideMovement.surfaceAnchor");

            if (!float.IsFinite(slideMovement.gravity.x) || !float.IsFinite(slideMovement.gravity.y))
                throw new ArgumentException($"Gravity is invalid. It is {slideMovement.gravity}.", "SlideMovement.gravity");

            if (!float.IsFinite(slideMovement.startPosition.x) || !float.IsFinite(slideMovement.startPosition.y))
                throw new ArgumentException($"Start Position is invalid. It is {slideMovement.gravity}.", "SlideMovement.startPosition");

            if (slideMovement.selectedCollider && slideMovement.selectedCollider.attachedRigidbody != this)
                throw new ArgumentException($"Selected Collider must be attached to the Slide Rigidbody2D. It is {slideMovement.selectedCollider}.", "SlideMovement.selectedCollider");

            return Slide_Internal(velocity, deltaTime, slideMovement);
        }

        [NativeMethod("Slide")]
        extern private SlideResults Slide_Internal(Vector2 velocity, float deltaTime, SlideMovement slideMovement);

        // The linear velocity vector of the object.
        extern public Vector2 linearVelocity { get; set; }

        // The linear velocity X-component vector of the object.
        extern public float linearVelocityX { get; set; }

        // The linear velocity Y-component vector of the object.
        extern public float linearVelocityY { get; set; }

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

        // The linear damping of the object.
        extern public float linearDamping { get; set; }

        // The angular damping of the object.
        extern public float angularDamping { get; set; }

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
        public int attachedColliderCount { get { return GetAttachedColliderCount_Internal(true); } }
        [NativeMethod("GetAttachedColliderCount")]
        extern private int GetAttachedColliderCount_Internal(bool findTriggers);

        // Gets/Sets the total user-applied force added to this body since the last simulation step.
        extern public Vector2 totalForce { get; set; }

        // Gets/Sets the total user-applied torque added to this body since the last simulation step.
        extern public float totalTorque { get; set; }

        // Get/Set the Exclude Layers,
        extern public LayerMask excludeLayers { get; set; }

        // Get/Set the Include Layers,
        extern public LayerMask includeLayers { get; set; }

        // Get the local to world transform of the body.
        extern public Matrix4x4 localToWorldMatrix { get; }

        // Get whether any attached collider(s) are currently touching a specific collider or not.
        extern public bool IsTouching([NotNull] Collider2D collider);

        // Get whether any attached collider(s) are currently touching a specific collider or not allowed by the contact filter.
        public bool IsTouching(Collider2D collider, ContactFilter2D contactFilter) { return IsTouching_OtherColliderWithFilter_Internal(collider, contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_OtherColliderWithFilter_Internal([NotNull] Collider2D collider, ContactFilter2D contactFilter);

        // Get whether any attached collider(s) are currently touching anything defined by the contact filter.
        public bool IsTouching(ContactFilter2D contactFilter) { return IsTouching_AnyColliderWithFilter_Internal(contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_AnyColliderWithFilter_Internal(ContactFilter2D contactFilter);

        // Get whether any attached collider(s) are touching the specific layer(s).
        [ExcludeFromDocs]
        public bool IsTouchingLayers() { return IsTouchingLayers(Physics2D.AllLayers); }
        public bool IsTouchingLayers([DefaultValue("Physics2D.AllLayers")] int layerMask = Physics2D.AllLayers)
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(layerMask);
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;

            return IsTouching(contactFilter);
        }

        // Checks whether the specified point overlaps all the rigidbody collider(s) or not.
        extern public bool OverlapPoint(Vector2 point);

        // Get the shortest distance and the respective points between all colliders on this rigidbody and another collider.
        public ColliderDistance2D Distance(Collider2D collider)
        {
            if (collider == null)
                throw new ArgumentNullException("Collider cannot be null.");

            if (collider.attachedRigidbody == this)
                throw new ArgumentException("The collider cannot be attached to the Rigidbody2D being searched.");

            return Distance_Internal(collider);
        }

        // Get the shortest distance and the respective points between two colliders at specific poses.
        public ColliderDistance2D Distance(
            Vector2 thisPosition, float thisAngle,
            Collider2D collider, Vector2 position, float angle)
        {
            if (!collider.attachedRigidbody)
                throw new InvalidOperationException("Cannot perform a Collider Distance at a specific position and angle if the Collider is not attached to a Rigidbody2D.");

            return DistanceFrom_Internal(
                thisPosition, thisAngle,
                collider, position, angle);
        }

        [NativeMethod("Distance")]
        extern private ColliderDistance2D Distance_Internal([NotNull] Collider2D collider);

        [NativeMethod("DistanceFrom")]
        extern private ColliderDistance2D DistanceFrom_Internal(
            Vector2 thisPosition, float thisAngle,
            [NotNull] Collider2D collider, Vector2 position, float angle);

        // Get the closest point to position on this rigidbody.
        public Vector2 ClosestPoint(Vector2 position)
        {
            return Physics2D.ClosestPoint(position, this);
        }

        // Adds /force/ (defined in global space) to the rigidbody center-of-mass.  No torque is therefore generated.
        [ExcludeFromDocs]
        public void AddForce(Vector2 force) { AddForce_Internal(force, ForceMode2D.Force); }
        public void AddForce(Vector2 force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode = ForceMode2D.Force) { AddForce_Internal(force, mode); }
        public void AddForceX(float force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode = ForceMode2D.Force) { AddForce_Internal(new Vector2(force, 0f), mode); }
        public void AddForceY(float force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode = ForceMode2D.Force) { AddForce_Internal(new Vector2(0f, force), mode); }

        [NativeMethod("AddForce")]
        extern private void AddForce_Internal(Vector2 force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Adds /relativeForce/ (defined relative in local space) to the rigidbody center-of-mass.  No torque is therefore generated.
        [ExcludeFromDocs]
        public void AddRelativeForce(Vector2 relativeForce) { AddRelativeForce_Internal(relativeForce, ForceMode2D.Force); }
        public void AddRelativeForce(Vector2 relativeForce, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode = ForceMode2D.Force) { AddRelativeForce_Internal(relativeForce, mode); }
        public void AddRelativeForceX(float force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode = ForceMode2D.Force) { AddRelativeForce_Internal(new Vector2(force, 0f), mode); }
        public void AddRelativeForceY(float force, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode = ForceMode2D.Force) { AddRelativeForce_Internal(new Vector2(0f, force), mode); }

        [NativeMethod("AddRelativeForce")]
        extern private void AddRelativeForce_Internal(Vector2 relativeForce, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Applies /force/ at /position/ (both defined in global space) to the rigidbody.  Torque therefore can be generated.
        [ExcludeFromDocs]
        public void AddForceAtPosition(Vector2 force, Vector2 position) { AddForceAtPosition(force, position, ForceMode2D.Force); }
        extern public void AddForceAtPosition(Vector2 force, Vector2 position, [DefaultValue("ForceMode2D.Force")] ForceMode2D mode);

        // Adds /torque/ to the rigidbody.
        [ExcludeFromDocs]
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

        // Get all contacts for this rigidbody.
        public int GetContacts(ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), contacts);
        }

        public int GetContacts(List<ContactPoint2D> contacts)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), contacts);
        }

        // Get filtered contacts for this rigidbody.
        public int GetContacts(ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, contactFilter, contacts);
        }

        public int GetContacts(ContactFilter2D contactFilter, List<ContactPoint2D> contacts)
        {
            return Physics2D.GetContacts(this, contactFilter, contacts);
        }

        // Get all contacts for this rigidbody (collider results).
        public int GetContacts(Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), colliders);
        }

        public int GetContacts(List<Collider2D> colliders)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), colliders);
        }

        // Get filtered contacts for this rigidbody (collider results).
        public int GetContacts(ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, contactFilter, colliders);
        }

        public int GetContacts(ContactFilter2D contactFilter, List<Collider2D> colliders)
        {
            return Physics2D.GetContacts(this, contactFilter, colliders);
        }

        // Get all colliders attached to this rigidbody.
        [ExcludeFromDocs]
        public int GetAttachedColliders([Out] Collider2D[] results)
        {
            return GetAttachedCollidersArray_Internal(results, true);
        }
        [ExcludeFromDocs]
        public int GetAttachedColliders(List<Collider2D> results)
        {
            return GetAttachedCollidersList_Internal(results, true);
        }

        public int GetAttachedColliders([Out] Collider2D[] results, [DefaultValue("true")] bool findTriggers = true)
        {
            return GetAttachedCollidersArray_Internal(results, findTriggers);
        }

        public int GetAttachedColliders(List<Collider2D> results, [DefaultValue("true")] bool findTriggers = true)
        {
            return GetAttachedCollidersList_Internal(results, findTriggers);
        }

        public int GetShapes(PhysicsShapeGroup2D physicsShapeGroup)
        {
            return GetShapes_Internal(ref physicsShapeGroup.m_GroupState);
        }

        // Returns the hits from casting all the rigidbody collider(s) along a ray.
        [ExcludeFromDocs]
        public int Cast(Vector2 direction, RaycastHit2D[] results)
        {
            return CastArray_Internal(direction, Mathf.Infinity, results);
        }

        public int Cast(Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance)
        {
            return CastArray_Internal(direction, distance, results);
        }

        public int Cast(Vector2 direction, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return CastList_Internal(direction, distance, results);
        }

        // Returns the hits from casting all the rigidbody collider(s) along a ray (filtered by the contact filter).
        [ExcludeFromDocs]
        public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return CastFilteredArray_Internal(direction, Mathf.Infinity, contactFilter, results);
        }

        public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return CastFilteredArray_Internal(direction, distance, contactFilter, results);
        }

        public int Cast(Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return CastFilteredList_Internal(direction, distance, contactFilter, results);
        }

        public int Cast(Vector2 position, float angle, Vector2 direction, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return CastFrom_Internal(position, angle, direction, distance, results);
        }

        public int Cast(Vector2 position, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return CastFromFiltered_Internal(position, angle, direction, distance, contactFilter, results);
        }

        public int Overlap(ContactFilter2D contactFilter, [Out] Collider2D[] results)
        {
            return OverlapArray_Internal(contactFilter, results);
        }

        public int Overlap(List<Collider2D> results)
        {
            return OverlapList_Internal(results);
        }

        public int Overlap(ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapFilteredList_Internal(contactFilter, results);
        }

        public int Overlap(Vector2 position, float angle, List<Collider2D> results)
        {
            return OverlapFromList_Internal(position, angle, results);
        }

        public int Overlap(Vector2 position, float angle, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return OverlapFromFilteredList_Internal(position, angle, contactFilter, results);
        }

        [NativeMethod("GetAttachedCollidersArray_Binding")]
        extern private int GetAttachedCollidersArray_Internal([NotNull] [Unmarshalled] Collider2D[] results, bool findTriggers);

        [NativeMethod("GetAttachedCollidersList_Binding")]
        extern private int GetAttachedCollidersList_Internal([NotNull] List<Collider2D> results, bool findTriggers);

        [NativeMethod("GetShapes_Binding")]
        extern private int GetShapes_Internal(ref PhysicsShapeGroup2D.GroupState physicsShapeGroupState);

        [NativeMethod("CastArray_Binding")]
        extern private int CastArray_Internal(Vector2 direction, float distance, [NotNull] RaycastHit2D[] results);

        [NativeMethod("CastList_Binding")]
        extern private int CastList_Internal(Vector2 direction, float distance, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("CastFilteredArray_Binding")]
        extern private int CastFilteredArray_Internal(Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        [NativeMethod("CastFilteredList_Binding")]
        extern private int CastFilteredList_Internal(Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("CastFrom_Binding")]
        extern private int CastFrom_Internal(Vector2 position, float angle, Vector2 direction, float distance, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("CastFromFiltered_Binding")]
        extern private int CastFromFiltered_Internal(Vector2 position, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("OverlapArray_Binding")]
        extern private int OverlapArray_Internal(ContactFilter2D contactFilter, [NotNull][Unmarshalled] Collider2D[] results);

        [NativeMethod("OverlapList_Binding")]
        extern private int OverlapList_Internal([NotNull] List<Collider2D> results);

        [NativeMethod("OverlapFilteredList_Binding")]
        extern private int OverlapFilteredList_Internal(ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);

        [NativeMethod("OverlapFromList_Binding")]
        extern private int OverlapFromList_Internal(Vector2 position, float angle, [NotNull] List<Collider2D> results);

        [NativeMethod("OverlapFromFilteredList_Binding")]
        extern private int OverlapFromFilteredList_Internal(Vector2 position, float angle, ContactFilter2D contactFilter, [NotNull] List<Collider2D> results);
    }

    #endregion

    #region Collider Components

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics2D/Public/Collider2D.h")]
    [RequiredByNativeCode(Optional = true)]
    public partial class Collider2D : Behaviour
    {
        // The composition operation.
        public enum CompositeOperation { None = 0, Merge = 1, Intersect = 2, Difference = 3, Flip = 4 };

        // The density of the collider.
        extern public float density { get; set; }

        // Gets whether the collider is a trigger or not.
        extern public bool isTrigger { get; set; }

        // Whether the collider is used by an attached effector or not.
        extern public bool usedByEffector { get; set; }

        // Set the composite operation to be used by an attached composite collider.
        extern public CompositeOperation compositeOperation { get; set; }

        // The composite operation order. Lower values are executed earlier than higher values. Same values result in an undefined composite operation order.
        extern public int compositeOrder { get; set; }

        // Gets the attached composite.
        extern public CompositeCollider2D composite { get; }

        // The local offset of the collider geometry.
        extern public Vector2 offset { get; set; }

        // Gets the attached rigid-body if it exists.
        extern public Rigidbody2D attachedRigidbody {[NativeMethod("GetAttachedRigidbody_Binding")] get; }

        // Get the local to world transform of the attached Collider body.
        extern public Matrix4x4 localToWorldMatrix { get; }

        // Gets the number of shapes this collider has generated.
        extern public int shapeCount { get; }

        // Gets a mesh for the specified collider.
        [ExcludeFromDocs]
        public Mesh CreateMesh(bool useBodyPosition, bool useBodyRotation)
        {
            return CreateMesh(useBodyPosition, useBodyRotation, true);
        }

        [NativeMethod("CreateMesh_Binding")]
        extern public Mesh CreateMesh(bool useBodyPosition, bool useBodyRotation, [DefaultValue("true")] bool useDelaunay = true);

        [NativeMethod("GetShapeHash_Binding")]
        extern public UInt32 GetShapeHash();

        public int GetShapes(PhysicsShapeGroup2D physicsShapeGroup)
        {
            return GetShapes_Internal(ref physicsShapeGroup.m_GroupState, 0, shapeCount);
        }

        public int GetShapes(PhysicsShapeGroup2D physicsShapeGroup, int shapeIndex, [DefaultValue("1")] int shapeCount = 1)
        {
            var colliderShapeCount = this.shapeCount;

            // Validate range.
            if (shapeIndex < 0 ||
                shapeIndex >= colliderShapeCount ||
                shapeCount < 1 ||
                (shapeIndex + shapeCount) > colliderShapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get shape range from {0} to {1} as Collider2D only has {2} shape(s).", shapeIndex, shapeIndex + shapeCount - 1, colliderShapeCount));

            return GetShapes_Internal(ref physicsShapeGroup.m_GroupState, shapeIndex, shapeCount);
        }

        [NativeMethod("GetShapes_Binding")]
        extern private int GetShapes_Internal(ref PhysicsShapeGroup2D.GroupState physicsShapeGroupState, int shapeIndex, int shapeCount);

        [NativeMethod("GetShapeBounds_Binding")]
        extern public Bounds GetShapeBounds(List<Bounds> bounds, bool useRadii, bool useWorldSpace);

        // The world space bounding volume of the collider.
        extern public Bounds bounds { get; }

        // Gets the collider error state indicating indicating if anything (and what) went wrong creating collision shape(s).
        extern public ColliderErrorState2D errorState { get; }

        // Is the collider capable of being composited?
        extern public bool compositeCapable {[NativeMethod("GetCompositeCapable_Binding")] get; }

        // The shared physics material of this collider.
        extern public PhysicsMaterial2D sharedMaterial
        {
            [NativeMethod("GetMaterial")]
            get;
            [NativeMethod("SetMaterial")]
            set;
        }

        // Get/Set the Layer Override Priority.
        extern public int layerOverridePriority { get; set; }

        // Get/Set the Exclude Layers,
        extern public LayerMask excludeLayers { get; set; }

        // Get/Set the Include Layers,
        extern public LayerMask includeLayers { get; set; }

        // Get/Set the Force Send Layers.
        extern public LayerMask forceSendLayers { get; set; }

        // Get/Set the Force Receive Layers.
        extern public LayerMask forceReceiveLayers { get; set; }

        // Get/Set the Contact Capture Layers,
        extern public LayerMask contactCaptureLayers { get; set; }

        // Get/Set the Callback Layers,
        extern public LayerMask callbackLayers { get; set; }

        // Gets the effective friction used by the collider.
        extern public float friction { get; }

        // Gets the effective bounciness used by the collider.
        extern public float bounciness { get; }

        // Gets the method used to combine both material friction values.
        extern public PhysicsMaterialCombine2D frictionCombine { get; }

        // Gets the method used to combine both material bounce values.
        extern public PhysicsMaterialCombine2D bounceCombine { get; }

        // Gets the contact mask taking into account the layer mask and the body/collider include/exclude masks.
        extern internal LayerMask contactMask { [NativeMethod("GetContactMask_Binding")] get; }

        // Get whether this collider is currently touching a specific collider or not.
        extern public bool IsTouching([NotNull] Collider2D collider);

        // Get whether this collider is currently touching a specific collider or not defined by the contact filter.
        public bool IsTouching(Collider2D collider, ContactFilter2D contactFilter) { return IsTouching_OtherColliderWithFilter(collider, contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_OtherColliderWithFilter([NotNull] Collider2D collider, ContactFilter2D contactFilter);

        // Get whether this collider is currently touching anything defined by the contact filter.
        public bool IsTouching(ContactFilter2D contactFilter) { return IsTouching_AnyColliderWithFilter(contactFilter); }
        [NativeMethod("IsTouching")]
        extern private bool IsTouching_AnyColliderWithFilter(ContactFilter2D contactFilter);

        // Get whether the specific collider is touching the specific layer(s).
        [ExcludeFromDocs]
        public bool IsTouchingLayers() { return IsTouchingLayers(Physics2D.AllLayers); }
        extern public bool IsTouchingLayers([DefaultValue("Physics2D.AllLayers")] int layerMask);

        // Checks whether the specified point overlaps the collider or not.
        extern public bool OverlapPoint(Vector2 point);

        // Returns all colliders overlapping this collider (limited by the size of the array) but filters using ContactFilter2D.  This does not produce any garbage.
        public int Overlap(ContactFilter2D contactFilter, Collider2D[] results)
        {
            return PhysicsScene2D.OverlapCollider(this, contactFilter, results);
        }

        public int Overlap(List<Collider2D> results)
        {
            return PhysicsScene2D.OverlapCollider(this, results);
        }

        public int Overlap(ContactFilter2D contactFilter, List<Collider2D> results)
        {
            return PhysicsScene2D.OverlapCollider(this, contactFilter, results);
        }

        public int Overlap(Vector2 position, float angle, List<Collider2D> results)
        {
            if (attachedRigidbody)
                return PhysicsScene2D.OverlapCollider(position, angle, this, results);

            throw new InvalidOperationException("Cannot perform a Collider Overlap at a specific position and angle if the Collider is not attached to a Rigidbody2D.");
        }

        public int Overlap(Vector2 position, float angle, ContactFilter2D contactFilter, List<Collider2D> results)
        {
            if (attachedRigidbody)
                return PhysicsScene2D.OverlapCollider(position, angle, this, contactFilter, results);

            throw new InvalidOperationException("Cannot perform a Collider Overlap at a specific position and angle if the Collider is not attached to a Rigidbody2D.");
        }

        // Returns the hits from casting the collider along a ray.
        [ExcludeFromDocs]
        public int Cast(Vector2 direction, RaycastHit2D[] results)
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            contactFilter.SetLayerMask(contactMask);

            return CastArray_Internal(direction, Mathf.Infinity, contactFilter, true, results);
        }

        [ExcludeFromDocs]
        public int Cast(Vector2 direction, RaycastHit2D[] results, float distance)
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            contactFilter.SetLayerMask(contactMask);

            return CastArray_Internal(direction, distance, contactFilter, true, results);
        }

        public int Cast(Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("true")] bool ignoreSiblingColliders)
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = Physics2D.queriesHitTriggers;
            contactFilter.SetLayerMask(contactMask);

            return CastArray_Internal(direction, distance, contactFilter, ignoreSiblingColliders, results);
        }

        // Returns the hits from casting the collider along a ray.
        [ExcludeFromDocs]
        public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return CastArray_Internal(direction, Mathf.Infinity, contactFilter, true, results);
        }

        [ExcludeFromDocs]
        public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, float distance)
        {
            return CastArray_Internal(direction, distance, contactFilter, true, results);
        }

        public int Cast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("true")] bool ignoreSiblingColliders)
        {
            return CastArray_Internal(direction, distance, contactFilter, ignoreSiblingColliders, results);
        }

        public int Cast(Vector2 direction, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity, [DefaultValue("true")] bool ignoreSiblingColliders = true)
        {
            return CastList_Internal(direction, distance, ignoreSiblingColliders, results);
        }

        public int Cast(Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity, [DefaultValue("true")] bool ignoreSiblingColliders = true)
        {
            return CastListFiltered_Internal(direction, distance, contactFilter, ignoreSiblingColliders, results);
        }

        public int Cast(Vector2 position, float angle, Vector2 direction, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity, [DefaultValue("true")] bool ignoreSiblingColliders = true)
        {
            if (attachedRigidbody)
                return CastFrom_Internal(position, angle, direction, distance, ignoreSiblingColliders, results);

            throw new InvalidOperationException("Cannot perform a Collider Cast from a specific position and angle if the Collider is not attached to a Rigidbody2D.");
        }

        public int Cast(Vector2 position, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity, [DefaultValue("true")] bool ignoreSiblingColliders = true)
        {
            if (attachedRigidbody)
                return CastFromFiltered_Internal(position, angle, direction, distance, contactFilter, ignoreSiblingColliders, results);

            throw new InvalidOperationException("Cannot perform a Collider Cast from a specific position and angle if the Collider is not attached to a Rigidbody2D.");
        }

        [NativeMethod("CastArray_Binding")]
        extern private int CastArray_Internal(Vector2 direction, float distance, ContactFilter2D contactFilter, bool ignoreSiblingColliders, [NotNull] RaycastHit2D[] results);

        [NativeMethod("CastList_Binding")]
        extern private int CastList_Internal(Vector2 direction, float distance, bool ignoreSiblingColliders, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("CastListFiltered_Binding")]
        extern private int CastListFiltered_Internal(Vector2 direction, float distance, ContactFilter2D contactFilter, bool ignoreSiblingColliders, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("CastFrom_Binding")]
        extern private int CastFrom_Internal(Vector2 position, float angle, Vector2 direction, float distance, bool ignoreSiblingColliders, [NotNull] List<RaycastHit2D> results);

        [NativeMethod("CastFromFiltered_Binding")]
        extern private int CastFromFiltered_Internal(Vector2 position, float angle, Vector2 direction, float distance, ContactFilter2D contactFilter, bool ignoreSiblingColliders, [NotNull] List<RaycastHit2D> results);

        // Returns all hits along the ray (limited by the size of the array) excluding this collider.  This does not produce any garbage.
        [ExcludeFromDocs]
        public int Raycast(Vector2 direction, RaycastHit2D[] results)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(Physics2D.AllLayers, -Mathf.Infinity, Mathf.Infinity);
            return RaycastArray_Internal(direction, Mathf.Infinity, contactFilter, results);
        }

        [ExcludeFromDocs]
        public int Raycast(Vector2 direction, RaycastHit2D[] results, float distance)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(Physics2D.AllLayers, -Mathf.Infinity, Mathf.Infinity);
            return RaycastArray_Internal(direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        public int Raycast(Vector2 direction, RaycastHit2D[] results, float distance, int layerMask)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, -Mathf.Infinity, Mathf.Infinity);
            return RaycastArray_Internal(direction, distance, contactFilter, results);
        }

        [ExcludeFromDocs]
        public int Raycast(Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, Mathf.Infinity);
            return RaycastArray_Internal(direction, distance, contactFilter, results);
        }

        public int Raycast(Vector2 direction, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance, [DefaultValue("Physics2D.AllLayers")] int layerMask, [DefaultValue("-Mathf.Infinity")] float minDepth, [DefaultValue("Mathf.Infinity")] float maxDepth)
        {
            var contactFilter = ContactFilter2D.CreateLegacyFilter(layerMask, minDepth, maxDepth);
            return RaycastArray_Internal(direction, distance, contactFilter, results);
        }

        // Returns all hits along the ray (limited by the size of the array) excluding this collider.  This does not produce any garbage.
        [ExcludeFromDocs]
        public int Raycast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results)
        {
            return RaycastArray_Internal(direction, Mathf.Infinity, contactFilter, results);
        }

        public int Raycast(Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, [DefaultValue("Mathf.Infinity")] float distance)
        {
            return RaycastArray_Internal(direction, distance, contactFilter, results);
        }

        [NativeMethod("RaycastArray_Binding")]
        extern private int RaycastArray_Internal(Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] RaycastHit2D[] results);

        public int Raycast(Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, [DefaultValue("Mathf.Infinity")] float distance = Mathf.Infinity)
        {
            return RaycastList_Internal(direction, distance, contactFilter, results);
        }

        [NativeMethod("RaycastList_Binding")]
        extern private int RaycastList_Internal(Vector2 direction, float distance, ContactFilter2D contactFilter, [NotNull] List<RaycastHit2D> results);

        // Get the shortest distance and the respective points between this collider and another.
        public ColliderDistance2D Distance(Collider2D collider)
        {
            return Physics2D.Distance(this, collider);
        }

        // Get the shortest distance and the respective points between two colliders at specific poses.
        public ColliderDistance2D Distance(
            Vector2 thisPosition, float thisAngle,
            Collider2D collider, Vector2 position, float angle)
        {
            return Physics2D.Distance(
                this, thisPosition, thisAngle,
                collider, position, angle);
        }

        // Get the closest point to position on this collider.
        public Vector2 ClosestPoint(Vector2 position)
        {
            return Physics2D.ClosestPoint(position, this);
        }

        // Get all contacts for this collider.
        public int GetContacts(ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), contacts);
        }

        public int GetContacts(List<ContactPoint2D> contacts)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), contacts);
        }

        // Get filtered contacts for this collider.
        public int GetContacts(ContactFilter2D contactFilter, ContactPoint2D[] contacts)
        {
            return Physics2D.GetContacts(this, contactFilter, contacts);
        }

        public int GetContacts(ContactFilter2D contactFilter, List<ContactPoint2D> contacts)
        {
            return Physics2D.GetContacts(this, contactFilter, contacts);
        }

        // Get all contacts for this collider.
        public int GetContacts(Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), colliders);
        }

        public int GetContacts(List<Collider2D> colliders)
        {
            return Physics2D.GetContacts(this, new ContactFilter2D().NoFilter(), colliders);
        }

        // Get filtered contacts for this collider.
        public int GetContacts(ContactFilter2D contactFilter, Collider2D[] colliders)
        {
            return Physics2D.GetContacts(this, contactFilter, colliders);
        }

        public int GetContacts(ContactFilter2D contactFilter, List<Collider2D> colliders)
        {
            return Physics2D.GetContacts(this, contactFilter, colliders);
        }
    }

    [NativeHeader("Modules/Physics2D/Public/CustomCollider2D.h")]
    public sealed partial class CustomCollider2D : Collider2D
    {
        // Gets the number of custom shapes this collider will generate.
        [NativeMethod("CustomShapeCount_Binding")]
        extern public int customShapeCount { get; }

        // Gets the number of custom shape vertices this collider will generate.
        [NativeMethod("CustomVertexCount_Binding")]
        extern public int customVertexCount { get; }

        public int GetCustomShapes(PhysicsShapeGroup2D physicsShapeGroup)
        {
            var colliderShapeCount = customShapeCount;

            // Get the custom shapes if there are any.
            if (colliderShapeCount > 0)
                return GetCustomShapes_Internal(ref physicsShapeGroup.m_GroupState, 0, colliderShapeCount);

            // No shapes so clear the group and finish.
            physicsShapeGroup.Clear();
            return 0;
        }

        public int GetCustomShapes(PhysicsShapeGroup2D physicsShapeGroup, int shapeIndex, [DefaultValue("1")] int shapeCount = 1)
        {
            var colliderShapeCount = customShapeCount;

            // Validate range.
            if (shapeIndex < 0 ||
                shapeIndex >= colliderShapeCount ||
                shapeCount < 1 ||
                (shapeIndex + shapeCount) > colliderShapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot get shape range from {0} to {1} as CustomCollider2D only has {2} shape(s).", shapeIndex, shapeIndex + shapeCount - 1, colliderShapeCount));

            return GetCustomShapes_Internal(ref physicsShapeGroup.m_GroupState, shapeIndex, shapeCount);
        }

        [NativeMethod("GetCustomShapes_Binding")]
        extern private int GetCustomShapes_Internal(ref PhysicsShapeGroup2D.GroupState physicsShapeGroupState, int shapeIndex, int shapeCount);

        public unsafe int GetCustomShapes(NativeArray<PhysicsShape2D> shapes, NativeArray<Vector2> vertices)
        {
            if (!shapes.IsCreated || shapes.Length != customShapeCount)
                throw new ArgumentException($"Cannot get custom shapes as the native shapes array length must be identical to the current custom shape count of {customShapeCount}.", "shapes");

            if (!vertices.IsCreated || vertices.Length != customVertexCount)
                throw new ArgumentException($"Cannot get custom shapes as the native vertices array length must be identical to the current custom vertex count of {customVertexCount}.", "vertices");

            return GetCustomShapesNative_Internal(
                (IntPtr)shapes.GetUnsafeReadOnlyPtr(), shapes.Length,
                (IntPtr)vertices.GetUnsafeReadOnlyPtr(), vertices.Length);
        }

        [NativeMethod("GetCustomShapesAllNative_Binding")]
        extern private int GetCustomShapesNative_Internal(IntPtr shapesPtr, int shapeCount, IntPtr verticesPtr, int vertexCount);

        // Set all custom shapes.
        public void SetCustomShapes(PhysicsShapeGroup2D physicsShapeGroup)
        {
            // Set the custom shapes if we defined any.
            if (physicsShapeGroup.shapeCount > 0)
            {
                SetCustomShapesAll_Internal(ref physicsShapeGroup.m_GroupState);
                return;
            }

            // No custom shapes so clear them.
            ClearCustomShapes();
        }

        [NativeMethod("SetCustomShapesAll_Binding")]
        extern private void SetCustomShapesAll_Internal(ref PhysicsShapeGroup2D.GroupState physicsShapeGroupState);

        // Set all custom shapes using native arrays.
        public unsafe void SetCustomShapes(NativeArray<PhysicsShape2D> shapes, NativeArray<Vector2> vertices)
        {
            if (!shapes.IsCreated || shapes.Length == 0)
                throw new ArgumentException("Cannot set custom shapes as the native shapes array is empty.", "shapes");

            if (!vertices.IsCreated || vertices.Length == 0)
                throw new ArgumentException("Cannot set custom shapes as the native vertices array is empty.", "vertices");

            SetCustomShapesNative_Internal(
                (IntPtr)shapes.GetUnsafeReadOnlyPtr(), shapes.Length,
                (IntPtr)vertices.GetUnsafeReadOnlyPtr(), vertices.Length);
        }

        [NativeMethod("SetCustomShapesAllNative_Binding", ThrowsException = true)]
        extern private void SetCustomShapesNative_Internal(IntPtr shapesPtr, int shapeCount, IntPtr verticesPtr, int vertexCount);

        // Set a single custom shape.
        public void SetCustomShape(PhysicsShapeGroup2D physicsShapeGroup, int srcShapeIndex, int dstShapeIndex)
        {
            if (srcShapeIndex < 0 || srcShapeIndex >= physicsShapeGroup.shapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set custom shape at {0} as the shape group only has {1} shape(s).", srcShapeIndex, physicsShapeGroup.shapeCount));

            var physicsShape2D = physicsShapeGroup.GetShape(srcShapeIndex);
            if (physicsShape2D.vertexStartIndex < 0 ||
                physicsShape2D.vertexStartIndex >= physicsShapeGroup.vertexCount ||
                physicsShape2D.vertexCount < 1 ||
                (physicsShape2D.vertexStartIndex + physicsShape2D.vertexCount) > physicsShapeGroup.vertexCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set custom shape at {0} as its shape indices are out of the available vertices ranges.", srcShapeIndex));

            if (dstShapeIndex < 0 || dstShapeIndex >= customShapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set custom shape at destination {0} as CustomCollider2D only has {1} custom shape(s). The destination index must be within the existing shape range.", dstShapeIndex, customShapeCount));

            SetCustomShape_Internal(ref physicsShapeGroup.m_GroupState, srcShapeIndex, dstShapeIndex);
        }

        [NativeMethod("SetCustomShape_Binding")]
        extern private void SetCustomShape_Internal(ref PhysicsShapeGroup2D.GroupState physicsShapeGroupState, int srcShapeIndex, int dstShapeIndex);

        // Set a single custom shape using native arrays.
        public unsafe void SetCustomShape(NativeArray<PhysicsShape2D> shapes, NativeArray<Vector2> vertices, int srcShapeIndex, int dstShapeIndex)
        {
            if (!shapes.IsCreated || shapes.Length == 0)
                throw new ArgumentException("Cannot set custom shapes as the native shapes array is empty.", "shapes");

            if (!vertices.IsCreated || vertices.Length == 0)
                throw new ArgumentException("Cannot set custom shapes as the native vertices array is empty.", "vertices");

            if (srcShapeIndex < 0 || srcShapeIndex >= shapes.Length)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set custom shape at {0} as the shape native array only has {1} shape(s).", srcShapeIndex, shapes.Length));

            var physicsShape2D = shapes[srcShapeIndex];
            if (physicsShape2D.vertexStartIndex < 0 ||
                physicsShape2D.vertexStartIndex >= vertices.Length ||
                physicsShape2D.vertexCount < 1 ||
                (physicsShape2D.vertexStartIndex + physicsShape2D.vertexCount) > vertices.Length)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set custom shape at {0} as its shape indices are out of the available vertices ranges.", srcShapeIndex));

            if (dstShapeIndex < 0 || dstShapeIndex >= customShapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot set custom shape at destination {0} as CustomCollider2D only has {1} custom shape(s). The destination index must be within the existing shape range.", dstShapeIndex, customShapeCount));

            SetCustomShapeNative_Internal(
                (IntPtr)shapes.GetUnsafeReadOnlyPtr(), shapes.Length,
                (IntPtr)vertices.GetUnsafeReadOnlyPtr(), vertices.Length,
                srcShapeIndex, dstShapeIndex);
        }

        [NativeMethod("SetCustomShapeNative_Binding", ThrowsException = true)]
        extern private void SetCustomShapeNative_Internal(IntPtr shapesPtr, int shapeCount, IntPtr verticesPtr, int vertexCount, int srcShapeIndex, int dstShapeIndex);

        // Clear shapes in the specified range.
        public void ClearCustomShapes(int shapeIndex, int shapeCount)
        {
            var colliderShapeCount = customShapeCount;

            if (shapeIndex < 0 || shapeIndex >= colliderShapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot clear custom shape(s) at index {0} as the CustomCollider2D only has {1} shape(s).", shapeIndex, colliderShapeCount));

            if ((shapeIndex + shapeCount) < 0 || (shapeIndex + shapeCount) > customShapeCount)
                throw new ArgumentOutOfRangeException(String.Format("Cannot clear custom shape(s) in the range (index {0}, count {1}) as this range is outside range of the existing {2} shape(s).", shapeIndex, shapeCount, customShapeCount));

            ClearCustomShapes_Internal(shapeIndex, shapeCount);
        }

        [NativeMethod("ClearCustomShapes_Binding")]
        extern private void ClearCustomShapes_Internal(int shapeIndex, int shapeCount);

        [NativeMethod("ClearCustomShapes_Binding")]
        extern public void ClearCustomShapes();
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

        // Get or set the points defining multiple continuous edges.
        extern public Vector2[] points { get; set; }

        [NativeMethod("GetPoints_Binding")]
        extern public int GetPoints([NotNull] List<Vector2> points);
        [NativeMethod("SetPoints_Binding")]
        extern public bool SetPoints([NotNull] List<Vector2> points);

        // Get or set the adjacent start/end points.
        extern public bool useAdjacentStartPoint { get; set; }
        extern public bool useAdjacentEndPoint { get; set; }
        extern public Vector2 adjacentStartPoint { get; set; }
        extern public Vector2 adjacentEndPoint { get; set; }
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
        // Get/Set Delaunay mesh usage.
        extern public bool useDelaunayMesh { get; set; }

        // Get/Set auto sprite tiling.
        extern public bool autoTiling { get; set; }

        // Get the total number of points in all paths.
        [NativeMethod("GetPointCount")]
        extern public int GetTotalPointCount();

        // Get/Set a single path of points.
        extern public Vector2[] points
        {
            [NativeMethod("GetPoints_Binding")]
            get;
            [NativeMethod("SetPoints_Binding")]
            set;
        }

        // Get the number of paths.
        extern public int pathCount { get; set; }

        // Get the specified path of points.
        public Vector2[] GetPath(int index)
        {
            if (index >= pathCount)
                throw new ArgumentOutOfRangeException(String.Format("Path {0} does not exist.", index));

            if (index < 0)
                throw new ArgumentOutOfRangeException(String.Format("Path {0} does not exist; negative path index is invalid.", index));

            return GetPath_Internal(index);
        }

        [NativeMethod("GetPath_Binding")]
        extern private Vector2[] GetPath_Internal(int index);

        // Set the specified path of points.
        public void SetPath(int index, Vector2[] points)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(String.Format("Negative path index {0} is invalid.", index));

            SetPath_Internal(index, points);
        }

        [NativeMethod("SetPath_Binding")]
        extern private void SetPath_Internal(int index, [NotNull] Vector2[] points);

        public int GetPath(int index, List<Vector2> points)
        {
            if (index < 0 || index >= pathCount)
                throw new ArgumentOutOfRangeException("index", String.Format("Path index {0} must be in the range of 0 to {1}.", index, pathCount - 1));

            if (points == null)
                throw new ArgumentNullException("points");

            return GetPathList_Internal(index, points);
        }

        [NativeMethod("GetPathList_Binding")]
        extern private int GetPathList_Internal(int index, [NotNull] List<Vector2> points);

        public void SetPath(int index, List<Vector2> points)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(String.Format("Negative path index {0} is invalid.", index));

            SetPathList_Internal(index, points);
        }

        [NativeMethod("SetPathList_Binding")]
        extern private void SetPathList_Internal(int index, [NotNull] List<Vector2> points);

        // Create a primitive n-sided polygon.
        [ExcludeFromDocs]
        public void CreatePrimitive(int sides)
        {
            CreatePrimitive(sides, Vector2.one, Vector2.zero);
        }

        [ExcludeFromDocs]
        public void CreatePrimitive(int sides, Vector2 scale)
        {
            CreatePrimitive(sides, scale, Vector2.zero);
        }

        public void CreatePrimitive(int sides, [DefaultValue("Vector2.one")] Vector2 scale, [DefaultValue("Vector2.zero")] Vector2 offset)
        {
            if (sides < 3)
            {
                Debug.LogWarning("Cannot create a 2D polygon primitive collider with less than two sides.", this);
                return;
            }

            if (!(scale.x > 0.0f && scale.y > 0.0f))
            {
                Debug.LogWarning("Cannot create a 2D polygon primitive collider with an axis scale less than or equal to zero.", this);
                return;
            }

            CreatePrimitive_Internal(sides, scale, offset, true);
        }

        [NativeMethod("CreatePrimitive")]
        extern private void CreatePrimitive_Internal(int sides, [DefaultValue("Vector2.one")] Vector2 scale, [DefaultValue("Vector2.zero")] Vector2 offset, bool autoRefresh);
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [NativeHeader("Modules/Physics2D/Public/CompositeCollider2D.h")]
    public sealed partial class CompositeCollider2D : Collider2D
    {
        public enum GeometryType { Outlines = 0, Polygons = 1 }

        public enum GenerationType { Synchronous = 0, Manual = 1 }

        // Controls the type of geometry created by the composite.
        extern public GeometryType geometryType { get; set; }

        // Controls when the collider generation happens.
        extern public GenerationType generationType { get; set; }

        // Get/Set Delaunay mesh usage.
        extern public bool useDelaunayMesh { get; set; }

        // Controls the allowed vertex distance spacing.
        extern public float vertexDistance { get; set; }

        // extern public radius of the edge(s).
        extern public float edgeRadius { get; set; }

        // Controls the distance in which vertices are offset and clipped if within this distance
        extern public float offsetDistance { get; set; }

        // Generates the geometry if using manual generation type.
        extern public void GenerateGeometry();

        // Get a list of composited colliders.
        [NativeMethod("GetCompositedColliders_Binding")]
        extern public int GetCompositedColliders([NotNull] List<Collider2D> colliders);

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

        // Get the specified path of points.
        public int GetPath(int index, Vector2[] points)
        {
            if (index < 0 || index >= pathCount)
                throw new ArgumentOutOfRangeException("index", String.Format("Path index {0} must be in the range of 0 to {1}.", index, pathCount - 1));

            if (points == null)
                throw new ArgumentNullException("points");

            return GetPathArray_Internal(index, points);
        }

        [NativeMethod("GetPathArray_Binding")]
        extern private int GetPathArray_Internal(int index, [NotNull] Vector2[] points);

        public int GetPath(int index, List<Vector2> points)
        {
            if (index < 0 || index >= pathCount)
                throw new ArgumentOutOfRangeException("index", String.Format("Path index {0} must be in the range of 0 to {1}.", index, pathCount - 1));

            if (points == null)
                throw new ArgumentNullException("points");

            return GetPathList_Internal(index, points);
        }

        [NativeMethod("GetPathList_Binding")]
        extern private int GetPathList_Internal(int index, [NotNull] List<Vector2> points);
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

        // The action required when the joint breaks.
        extern public JointBreakAction2D breakAction { get; set; }

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

        // Enables the joint's connected anchor.
        extern public bool useConnectedAnchor { get; set; }

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

        // The linear damping to apply to rigid-bodies.
        extern public float linearDamping { get; set; }

        // The angular damping to apply to rigid-bodies.
        extern public float angularDamping { get; set; }

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

        // The linear damping when touching the fluid.
        extern public float linearDamping { get; set; }

        // The angular damping when touching the fluid.
        extern public float angularDamping { get; set; }

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

        // The linear damping to apply to rigid-bodies.
        extern public float linearDamping { get; set; }

        // The angular damping to apply to rigid-bodies.
        extern public float angularDamping { get; set; }

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

        // Get combined values.
        extern static public float GetCombinedValues(float valueA, float valueB, PhysicsMaterialCombine2D materialCombineA, PhysicsMaterialCombine2D materialCombineB);

        [NativeMethod("Create_Binding")]
        extern static private void Create_Internal([Writable] PhysicsMaterial2D scriptMaterial, string name);

        // Controls how bouncy the surface contact is. A value of 0 will not bounce whereas a value of 1 will bounce without any loss of energy.
        extern public float bounciness { get; set; }

        // Controls how much friction is used for the surface contact. A value of 0 is no friction whereas any higher value increases the friction.
        extern public float friction { get; set; }

        // The method used to combine both material friction values.
        extern public PhysicsMaterialCombine2D frictionCombine { get; set; }

        // The method used to combine both material bounciness values.
        extern public PhysicsMaterialCombine2D bounceCombine { get; set; }
    }

#endregion
}
