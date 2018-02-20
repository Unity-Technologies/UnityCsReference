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

        public extern static bool devOptions {get; set; }
        public extern static int dirtyCount { get; }
        public extern static PhysicsVisualizationSettings.FilterWorkflow filterWorkflow { get; set; }
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
        public extern static bool GetShowStaticColliders(FilterWorkflow filterWorkFlow);
        public extern static void SetShowStaticColliders(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowTriggers(FilterWorkflow filterWorkflow);
        public extern static void SetShowTriggers(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowRigidbodies(FilterWorkflow filterWorkflow);
        public extern static void SetShowRigidbodies(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowKinematicBodies(FilterWorkflow filterWorkflow);
        public extern static void SetShowKinematicBodies(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowSleepingBodies(FilterWorkflow filterWorkflow);
        public extern static void SetShowSleepingBodies(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowCollisionLayer(FilterWorkflow filterWorkflow, int layer);
        public extern static void SetShowCollisionLayer(FilterWorkflow filterWorkflow, int layer, bool show);
        public extern static int GetShowCollisionLayerMask(FilterWorkflow filterWorkflow);
        public extern static void SetShowCollisionLayerMask(FilterWorkflow filterWorkflow, int mask);
        public extern static bool GetShowBoxColliders(FilterWorkflow filterWorkflow);
        public extern static void SetShowBoxColliders(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowSphereColliders(FilterWorkflow filterWorkflow);
        public extern static void SetShowSphereColliders(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowCapsuleColliders(FilterWorkflow filterWorkflow);
        public extern static void SetShowCapsuleColliders(FilterWorkflow filterWorkflow, bool show);
        public extern static bool GetShowMeshColliders(FilterWorkflow filterWorkflow, MeshColliderType colliderType);
        public extern static void SetShowMeshColliders(FilterWorkflow filterWorkflow, MeshColliderType colliderType, bool show);
        public extern static bool GetShowTerrainColliders(FilterWorkflow filterWorkflow);
        public extern static void SetShowTerrainColliders(FilterWorkflow filterWorkflow, bool show);
        public extern static void InitDebugDraw();
        public extern static void DeinitDebugDraw();
        public extern static void ClearMouseHighlight();
        public extern static bool HasMouseHighlight();
        public extern static void UpdateMouseHighlight(Vector2 screenPos);
        public extern static GameObject PickClosestGameObject([NotNull] Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex);

        [NativeName("CollectCollidersForDebugDraw")]
        private extern static void Internal_CollectCollidersForDebugDraw([NotNull] Camera cam, [NotNull] object colliderList);

        public static void SetShowForAllFilters(FilterWorkflow filterWorkflow, bool selected)
        {
            const int kMaxLayers = 32;
            for (int i = 0; i < kMaxLayers; i++)
                SetShowCollisionLayer(filterWorkflow, i, selected);
            SetShowStaticColliders(filterWorkflow, selected);
            SetShowTriggers(filterWorkflow, selected);
            SetShowRigidbodies(filterWorkflow, selected);
            SetShowKinematicBodies(filterWorkflow, selected);
            SetShowSleepingBodies(filterWorkflow, selected);

            SetShowBoxColliders(filterWorkflow, selected);
            SetShowSphereColliders(filterWorkflow, selected);
            SetShowCapsuleColliders(filterWorkflow, selected);
            SetShowMeshColliders(filterWorkflow, MeshColliderType.Convex, selected);
            SetShowMeshColliders(filterWorkflow, MeshColliderType.NonConvex, selected);
            SetShowTerrainColliders(filterWorkflow, selected);
        }
    }
}

