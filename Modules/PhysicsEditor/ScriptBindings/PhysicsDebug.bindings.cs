// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using System;
using UnityEngine.Bindings;

using UnityEngine;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

namespace UnityEditor
{
    [NativeHeader("Modules/PhysicsEditor/PhysicsVisualizationSettings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Modules/Physics/Collider.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [StaticAccessor("GetPhysicsVisualizationSettings()", StaticAccessorType.Dot)]
    public static partial class PhysicsVisualizationSettings
    {
        public enum FilterWorkflow
        {
            HideSelectedItems = 0,
            ShowSelectedItems = 1
        }

        public enum MeshColliderType
        {
            Convex = 0,
            NonConvex = 1
        }

        public enum QueryFilter
        {
            All         = ~0,
            Box         = 1 << 0,
            Capsule     = 1 << 1,
            Cast        = 1 << 2,
            Check       = 1 << 3,
            None        = 0,
            Overlap     = 1 << 4,
            Ray         = 1 << 5,
            ShowQueries = 1 << 6,
            Sphere      = 1 << 7,
        }

        internal extern static bool isDebuggerActive { get; set; }
        public extern static bool devOptions { get; set; }
        public extern static int dirtyCount { get; }
        public extern static bool showCollisionGeometry { get; set; }
        public extern static bool enableMouseSelect { get; set; }
        public extern static bool useSceneCam { get; set; }
        public extern static float viewDistance { get; set; }
        public extern static int terrainTilesMax { get; set; }
        public extern static bool forceOverdraw { get; set; }
        public extern static Color staticColor { get; set; }
        public extern static Color rigidbodyColor { get; set; }
        public extern static Color kinematicColor { get; set; }
        public extern static Color articulationBodyColor { get; set; }
        public extern static Color triggerColor { get; set; }
        public extern static Color sleepingBodyColor { get; set; }
        public extern static float baseAlpha { get; set; }
        public extern static float colorVariance { get; set; }
        public extern static bool centerOfMassUseScreenSize { get; set; }
        public extern static float inertiaTensorScale { get; set; }
        public extern static float dotAlpha { get; set; }
        public extern static bool forceDot { get; set; }
        public extern static Color contactColor { get; set; }
        public extern static Color contactSeparationColor { get; set; }
        public extern static Color contactImpulseColor { get; set; }
        public extern static bool showContacts { get; set; }
        public extern static bool showContactImpulse { get; set; }
        public extern static bool showContactSeparation { get; set; }
        public extern static bool showAllContacts { get; set; }
        public extern static bool useContactFiltering { get; set; }
        public extern static bool useVariedContactColors { get; set; }
        public extern static Color queryColor { get; set; }
        public extern static int maxNumberOfQueries { get; set; }

        public extern static void Reset();
        public extern static bool GetShowStaticColliders();
        public extern static void SetShowStaticColliders(bool show);
        public extern static bool GetShowTriggers();
        public extern static void SetShowTriggers(bool show);
        public extern static bool GetShowRigidbodies();
        public extern static void SetShowRigidbodies(bool show);
        public extern static bool GetShowKinematicBodies();
        public extern static void SetShowKinematicBodies(bool show);
        public extern static bool GetShowArticulationBodies();
        public extern static void SetShowArticulationBodies(bool show);
        public extern static bool GetShowSleepingBodies();
        public extern static void SetShowSleepingBodies(bool show);
        public extern static bool GetShowCollisionLayer(int layer);
        public extern static void SetShowCollisionLayer(int layer, bool show);
        public extern static int GetShowCollisionLayerMask();
        public extern static void SetShowCollisionLayerMask(int mask);
        public extern static bool GetShowBoxColliders();
        public extern static void SetShowBoxColliders(bool show);
        public extern static bool GetShowSphereColliders();
        public extern static void SetShowSphereColliders(bool show);
        public extern static bool GetShowCapsuleColliders();
        public extern static void SetShowCapsuleColliders(bool show);
        public extern static bool GetShowMeshColliders(MeshColliderType colliderType);
        public extern static void SetShowMeshColliders(MeshColliderType colliderType, bool show);
        public extern static bool GetShowTerrainColliders();
        public extern static void SetShowTerrainColliders(bool show);
        public extern static int GetShowPhysicsSceneMask();
        public extern static void SetShowPhysicsSceneMask(int mask);
        public extern static int GetShowUnitySceneMask();
        public extern static void SetShowUnitySceneMask(int mask);
        public extern static bool GetQueryFilterState(QueryFilter filter);
        public extern static void SetQueryFilterState(QueryFilter filter, bool value);
        public extern static void InitDebugDraw();
        public extern static void DeinitDebugDraw();
        public extern static void ClearMouseHighlight();
        public extern static bool HasMouseHighlight();
        public extern static void UpdateMouseHighlight(Vector2 screenPos);
        internal extern static void UpdateMouseHighlight_Internal(Vector2 screenPos, [NotNull] Camera providedCamera);
        public extern static GameObject PickClosestGameObject([NotNull] Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);

        [NativeName("CollectCollidersForDebugDraw")]
        private extern static void Internal_CollectCollidersForDebugDraw([NotNull] Camera cam, [NotNull] object colliderList);

        public static void SetShowForAllFilters(bool selected)
        {
            SetShowPhysicsSceneMask(selected ? ~0 : 0);
            SetShowUnitySceneMask(selected ? ~0 : 0);

            const int kMaxLayers = 32;
            for (int i = 0; i < kMaxLayers; i++)
                SetShowCollisionLayer(i, selected);

            SetShowStaticColliders(selected);
            SetShowTriggers(selected);
            SetShowRigidbodies(selected);
            SetShowKinematicBodies(selected);
            SetShowArticulationBodies(selected);
            SetShowSleepingBodies(selected);

            SetShowBoxColliders(selected);
            SetShowSphereColliders(selected);
            SetShowCapsuleColliders(selected);
            SetShowMeshColliders(MeshColliderType.Convex, selected);
            SetShowMeshColliders(MeshColliderType.NonConvex, selected);
            SetShowTerrainColliders(selected);

            showContacts = selected;
            showAllContacts = selected;
            showContactImpulse = selected;
            useContactFiltering = !selected;
        }
    }

    [NativeHeader("Modules/PhysicsEditor/PhysicsDebugDraw.h")]
    internal static class PhysicsDebugDraw
    {
        [FreeFunction("PhysicsDebugDraw::GetPooledQueries")]
        internal extern static void GetPooledQueries();

        [FreeFunction("PhysicsDebugDraw::ClearAllPools")]
        internal extern static void ClearAllPools();

        [FreeFunction("PhysicsDebugDraw::UpdateFilterConditionally")]
        internal extern static void UpdateFilterConditionally();

        [FreeFunction("PhysicsDebugDraw::UpdateFilter")]
        internal extern static void UpdateFilter();

        [FreeFunction("PhysicsDebugDraw::IsContactVisualised")]
        internal extern static bool IsColliderVisualised(Collider collider);

        [FreeFunction("PhysicsDebugDraw::IsContactVisualisedThreadSafe", isThreadSafe: true)]
        internal extern static bool IsContactVisualisedThreadSafe(IntPtr shapePtr);

        internal static event Action<PhysicsScene> OnBeforeSimulate;
        internal static event Action<NativeArray<Query>> OnRetrievePooledQueries;
        internal static event Action<PhysicsScene> OnDestroyPhysicsScene;

        [RequiredByNativeCode]
        private static unsafe void OnReportPooledQueries(IntPtr buffer, int count)
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Query>(buffer.ToPointer(), count, Allocator.None);

            var safety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);

            if (OnRetrievePooledQueries != null)
                OnRetrievePooledQueries(array);

            AtomicSafetyHandle.Release(safety);
        }

        [RequiredByNativeCode]
        private static void InvokeBeforeSimulate(PhysicsScene scene)
        {
            OnBeforeSimulate?.Invoke(scene);
        }

        [RequiredByNativeCode]
        private static void OnPhysicsWorldDestroyed(PhysicsScene scene)
        {
            OnDestroyPhysicsScene?.Invoke(scene);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Query : IEquatable<Query>
        {
            public PhysicsVisualizationSettings.QueryFilter filter;
            public Vector3 v1;
            public Vector3 v2;
            public Quaternion q;
            public float r;
            // If this is a cast
            public float distance;
            public Vector3 direction;

            public override bool Equals(object obj)
            {
                return obj is Query query && Equals(query);
            }

            public bool Equals(Query other)
            {
                return filter == other.filter &&
                       v1.Equals(other.v1) &&
                       v2.Equals(other.v2) &&
                       q.Equals(other.q) &&
                       r == other.r &&
                       distance == other.distance &&
                       direction.Equals(other.direction);
            }

            public override int GetHashCode()
            {
                int hashCode = 1232828845;
                hashCode = hashCode * -1521134295 + filter.GetHashCode();
                hashCode = hashCode * -1521134295 + v1.GetHashCode();
                hashCode = hashCode * -1521134295 + v2.GetHashCode();
                hashCode = hashCode * -1521134295 + q.GetHashCode();
                hashCode = hashCode * -1521134295 + r.GetHashCode();
                hashCode = hashCode * -1521134295 + distance.GetHashCode();
                hashCode = hashCode * -1521134295 + direction.GetHashCode();
                return hashCode;
            }
        }
    }
}
