// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;

using UnityEngine.Bindings;
using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    [NativeHeader("Modules/Physics2D/LowLevel/PhysicsComposer2D.h")]
    [StaticAccessor("PhysicsComposer2D", StaticAccessorType.DoubleColon)]
    internal static class PhysicsComposerScripting2D
    {
        [NativeMethod(Name = "Create", IsThreadSafe = true)] extern internal static PhysicsComposer PhysicsComposer_Create(Allocator allocator);
        [NativeMethod(Name = "Destroy", IsThreadSafe = true)] extern internal static bool PhysicsComposer_Destroy(PhysicsComposer composer);
        [NativeMethod(Name = "DestroyAll", IsThreadSafe = true)] extern internal static void PhysicsComposer_DestroyAll();
        [NativeMethod(Name = "IsValid", IsThreadSafe = true)] extern internal static bool Composer_IsValid(PhysicsComposer composer);
        [NativeMethod(Name = "GetComposers", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_GetComposers(Allocator allocator);
        [NativeMethod(Name = "AddLayer", IsThreadSafe = true)] extern internal static PhysicsComposer.LayerHandle PhysicsComposer_AddLayer(PhysicsComposer composer, PhysicsComposer.Layer layer);
        [NativeMethod(Name = "RemoveLayer", IsThreadSafe = true)] extern internal static void PhysicsComposer_RemoveLayer(PhysicsComposer composer, PhysicsComposer.LayerHandle layerHandle);
        [NativeMethod(Name = "ClearLayers", IsThreadSafe = true)] extern internal static void PhysicsComposer_ClearLayers(PhysicsComposer composer);
        [NativeMethod(Name = "GetLayerCount", IsThreadSafe = true)] extern internal static int PhysicsComposer_GetLayerCount(PhysicsComposer composer);
        [NativeMethod(Name = "GetRejectedGeometryCount", IsThreadSafe = true)] extern internal static int PhysicsComposer_GetRejectedGeometryCount(PhysicsComposer composer);
        [NativeMethod(Name = "GetLayerHandles", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_GetLayerHandles(PhysicsComposer composer);
        [NativeMethod(Name = "SetUseDelaunay", IsThreadSafe = true)] extern internal static void PhysicsComposer_SetUseDelaunay(PhysicsComposer composer, bool flag);
        [NativeMethod(Name = "GetUseDelaunay", IsThreadSafe = true)] extern internal static bool PhysicsComposer_GetUseDelaunay(PhysicsComposer composer);
        [NativeMethod(Name = "SetMaxPolygonVertices", IsThreadSafe = true)] extern internal static void PhysicsComposer_SetMaxPolygonVertices(PhysicsComposer composer, int maxPolygonVertices);
        [NativeMethod(Name = "GetMaxPolygonVertices", IsThreadSafe = true)] extern internal static int PhysicsComposer_GetMaxPolygonVertices(PhysicsComposer composer);
        [NativeMethod(Name = "CreatePolygonGeometry", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_CreatePolygonGeometry(PhysicsComposer composer, Vector2 vertexScale, float radius, Allocator allocator);
        [NativeMethod(Name = "CreateConvexHulls", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_CreateConvexHulls(PhysicsComposer composer, Vector2 vertexScale, Allocator allocator);
        [NativeMethod(Name = "CreateChainGeometry", IsThreadSafe = true)] extern internal static PhysicsBufferPair PhysicsComposer_CreateChainGeometry(PhysicsComposer composer, Vector2 vertexScale, Allocator allocator);
        [NativeMethod(Name = "GetGeometryIslands", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsComposer_GetGeometryIslands(PhysicsComposer composer, Allocator allocator);
    }
}
