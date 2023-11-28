// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEditor
{
    public static partial class PhysicsVisualizationSettings
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated.", true)]
        public static FilterWorkflow filterWorkflow { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowStaticColliders(FilterWorkflow filterWorkFlow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowStaticColliders(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowTriggers(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowTriggers(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowRigidbodies(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowRigidbodies(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowKinematicBodies(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowKinematicBodies(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowSleepingBodies(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowSleepingBodies(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowCollisionLayer(FilterWorkflow filterWorkflow, int layer) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowCollisionLayer(FilterWorkflow filterWorkflow, int layer, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static int GetShowCollisionLayerMask(FilterWorkflow filterWorkflow) { return 0; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowCollisionLayerMask(FilterWorkflow filterWorkflow, int mask) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowBoxColliders(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowBoxColliders(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowSphereColliders(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowSphereColliders(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowCapsuleColliders(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowCapsuleColliders(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowMeshColliders(FilterWorkflow filterWorkflow, MeshColliderType colliderType) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowMeshColliders(FilterWorkflow filterWorkflow, MeshColliderType colliderType, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static bool GetShowTerrainColliders(FilterWorkflow filterWorkflow) { return false; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowTerrainColliders(FilterWorkflow filterWorkflow, bool show) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static int GetShowPhysicsSceneMask(FilterWorkflow filterWorkflow) { return 0; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowPhysicsSceneMask(FilterWorkflow filterWorkflow, int mask) { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Enum PhysicsVisualizationSettings.FilterWorkflow has been deprecated. Use APIs without this argument instead", true)]
        public static void SetShowForAllFilters(FilterWorkflow filterWorkflow, bool selected) { }
    }
}
