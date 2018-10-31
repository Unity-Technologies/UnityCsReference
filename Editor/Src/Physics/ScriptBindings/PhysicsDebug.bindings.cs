// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using System;
using UnityEngine.Bindings;

using UnityEngine;


namespace UnityEditor
{
    [NativeHeader("Editor/Src/Physics/PhysicsVisualizationSettings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Dynamics/Collider.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [StaticAccessor("GetPhysicsVisualizationSettings()", StaticAccessorType.Dot)]
    public static partial class PhysicsVisualizationSettings
    {
        public enum MeshColliderType
        {
            Convex = 0,
            NonConvex = 1
        }

        public extern static bool devOptions {get; set; }
        public extern static int dirtyCount { get; }
        public extern static bool showCollisionGeometry { get; set; }
        public extern static bool enableMouseSelect {get; set; }
        public extern static bool useSceneCam { get; set; }
        public extern static float viewDistance { get; set; }
        public extern static int terrainTilesMax { get; set; }
        public extern static bool forceOverdraw { get; set; }
        public extern static Color staticColor { get; set; }
        public extern static Color rigidbodyColor { get; set; }
        public extern static Color kinematicColor { get; set; }
        public extern static Color triggerColor { get; set; }
        public extern static Color sleepingBodyColor { get; set; }
        public extern static float baseAlpha { get; set; }
        public extern static float colorVariance { get; set; }
        public extern static float dotAlpha { get; set; }
        public extern static bool forceDot { get; set; }

        public extern static void Reset();
        public extern static bool GetShowStaticColliders();
        public extern static void SetShowStaticColliders(bool show);
        public extern static bool GetShowTriggers();
        public extern static void SetShowTriggers(bool show);
        public extern static bool GetShowRigidbodies();
        public extern static void SetShowRigidbodies(bool show);
        public extern static bool GetShowKinematicBodies();
        public extern static void SetShowKinematicBodies(bool show);
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
        public extern static void InitDebugDraw();
        public extern static void DeinitDebugDraw();
        public extern static void ClearMouseHighlight();
        public extern static bool HasMouseHighlight();
        public extern static void UpdateMouseHighlight(Vector2 screenPos);
        public extern static GameObject PickClosestGameObject([NotNull] Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);

        [NativeName("CollectCollidersForDebugDraw")]
        private extern static void Internal_CollectCollidersForDebugDraw([NotNull] Camera cam, [NotNull] object colliderList);

        public static void SetShowForAllFilters(bool selected)
        {
            const int kMaxLayers = 32;
            for (int i = 0; i < kMaxLayers; i++)
                SetShowCollisionLayer(i, selected);

            SetShowStaticColliders(selected);
            SetShowTriggers(selected);
            SetShowRigidbodies(selected);
            SetShowKinematicBodies(selected);
            SetShowSleepingBodies(selected);

            SetShowBoxColliders(selected);
            SetShowSphereColliders(selected);
            SetShowCapsuleColliders(selected);
            SetShowMeshColliders(MeshColliderType.Convex, selected);
            SetShowMeshColliders(MeshColliderType.NonConvex, selected);
            SetShowTerrainColliders(selected);
        }
    }
}

