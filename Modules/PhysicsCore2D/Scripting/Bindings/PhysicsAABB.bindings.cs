// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsAABB::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsAABB_IsValid(PhysicsAABB aabb);
        [NativeMethod(Name = "PhysicsAABB::OverlapPoint", IsThreadSafe = true)] extern internal static bool PhysicsAABB_OverlapPoint(PhysicsAABB aabb, Vector2 point);
        [NativeMethod(Name = "PhysicsAABB::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsAABB_CastRay(PhysicsAABB aabb, PhysicsQuery.CastRayInput input);
        [NativeMethod(Name = "PhysicsAABB::Overlap", IsThreadSafe = true)] extern internal static bool PhysicsAABB_Overlap(PhysicsAABB aabb1, PhysicsAABB aabb2);
        [NativeMethod(Name = "PhysicsAABB::Union", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsAABB_Union(PhysicsAABB aabb1, PhysicsAABB aabb2);
        [NativeMethod(Name = "PhysicsAABB::Contains", IsThreadSafe = true)] extern internal static bool PhysicsAABB_Contains(PhysicsAABB aabb1, PhysicsAABB aabb2);
        [NativeMethod(Name = "PhysicsAABB::Center", IsThreadSafe = true)] extern internal static Vector2 PhysicsAABB_Center(PhysicsAABB aabb);
        [NativeMethod(Name = "PhysicsAABB::Extents", IsThreadSafe = true)] extern internal static Vector2 PhysicsAABB_Extents(PhysicsAABB aabb);
        [NativeMethod(Name = "PhysicsAABB::Perimeter", IsThreadSafe = true)] extern internal static float PhysicsAABB_Perimeter(PhysicsAABB aabb);
    }
}
