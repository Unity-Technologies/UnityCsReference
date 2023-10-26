// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [Flags]
    public enum MeshColliderCookingOptions
    {
        None,
        [Obsolete("No longer used because the problem this was trying to solve is gone since Unity 2018.3", true)] InflateConvexMesh = 1 << 0,
        CookForFasterSimulation = 1 << 1,
        EnableMeshCleaning = 1 << 2,
        WeldColocatedVertices = 1 << 3,
        UseFastMidphase = 1 << 4
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/MeshCollider.h")]
    [NativeHeader("Runtime/Graphics/Mesh/Mesh.h")]
    public partial class MeshCollider : Collider
    {
        extern public Mesh sharedMesh { get; set; }
        extern public bool convex { get; set; }

        extern public MeshColliderCookingOptions cookingOptions { get; set; }
    }
}
