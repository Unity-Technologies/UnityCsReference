// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    [NativeHeader("Modules/PhysicsCore2D/Core/PhysicsSpace2D.h")]
    [StaticAccessor("PhysicsSpace2D", StaticAccessorType.DoubleColon)]
    static class PhysicsSpaceScripting2D
    {
        [NativeMethod(Name = "Create", IsThreadSafe = true)] extern internal static PhysicsSpace PhysicsSpace_Create();
        [NativeMethod(Name = "CreateWithWorld", IsThreadSafe = true)] extern internal static PhysicsSpace PhysicsSpace_CreateWithWorld(PhysicsWorld world);
        [NativeMethod(Name = "GetSourceWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsSpace_GetSourceWorld(PhysicsSpace space);
        [NativeMethod(Name = "Destroy", IsThreadSafe = true)] extern internal static bool PhysicsSpace_Destroy(PhysicsSpace space);
        [NativeMethod(Name = "DestroyAll", IsThreadSafe = true)] extern internal static void PhysicsSpace_DestroyAll();
        [NativeMethod(Name = "IsValid", IsThreadSafe = true)] extern internal static bool Space_IsValid(PhysicsSpace space);
        [NativeMethod(Name = "GetSpaces", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_GetSpaces(Allocator allocator);
        [NativeMethod(Name = "Clone", IsThreadSafe = true)] extern internal static int PhysicsSpace_Clone(PhysicsSpace space, PhysicsWorld world, PhysicsQuery.QueryFilter filter, PhysicsAABB aabb, bool useAABB, bool destroyExistingProxies);
        [NativeMethod(Name = "SyncShapes", IsThreadSafe = true)] extern internal static int PhysicsSpace_SyncShapes(PhysicsSpace space, PhysicsWorld world);
        [NativeMethod(Name = "SyncShapesProxies", IsThreadSafe = true)] extern internal static int PhysicsSpace_SyncShapesProxies(PhysicsSpace space, PhysicsWorld world, ReadOnlySpan<PhysicsSpace.ProxyHandle> proxies);
        [NativeMethod(Name = "CreateProxy", IsThreadSafe = true)] extern internal static PhysicsSpace.ProxyHandle PhysicsSpace_CreateProxy(PhysicsSpace space, PhysicsWorld world, PhysicsAABB aabb, PhysicsMask categories, PhysicsHandle userHandle);
        [NativeMethod(Name = "CreateBatchProxyShapes", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_CreateBatchProxyShapes(PhysicsSpace space, PhysicsWorld world, ReadOnlySpan<PhysicsHandle> shapes, Allocator allocator);
        [NativeMethod(Name = "DestroyProxy", IsThreadSafe = true)] extern internal static bool PhysicsSpace_DestroyProxy(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "ClearProxies", IsThreadSafe = true)] extern internal static bool PhysicsSpace_ClearProxies(PhysicsSpace space);
        [NativeMethod(Name = "GetProxyCount", IsThreadSafe = true)] extern internal static int PhysicsSpace_GetProxyCount(PhysicsSpace space);
        [NativeMethod(Name = "GetMemoryAllocated", IsThreadSafe = true)] extern internal static int PhysicsSpace_GetMemoryAllocated(PhysicsSpace space);
        [NativeMethod(Name = "GetRootAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsSpace_GetRootAABB(PhysicsSpace space);
        [NativeMethod(Name = "GetProxyAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsSpace_GetProxyAABB(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "GetProxyCategories", IsThreadSafe = true)] extern internal static PhysicsMask PhysicsSpace_GetProxyCategories(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "SetProxyUserHandle", IsThreadSafe = true)] extern internal static void PhysicsSpace_SetProxyUserHandle(PhysicsSpace space, PhysicsWorld world, PhysicsSpace.ProxyHandle proxyHandle, PhysicsHandle userHandle);
        [NativeMethod(Name = "GetProxyUserHandle", IsThreadSafe = true)] extern internal static PhysicsHandle PhysicsSpace_GetProxyUserHandle(PhysicsSpace space, PhysicsSpace.ProxyHandle proxyHandle);
        [NativeMethod(Name = "SetBatchProxyAABB", IsThreadSafe = true)] extern internal static int PhysicsSpace_SetBatchProxyAABB(PhysicsSpace space, ReadOnlySpan<PhysicsSpace.ProxyHandle> proxies, ReadOnlySpan<PhysicsAABB> aabbs, bool updateAncestors);
        [NativeMethod(Name = "SetBatchProxyCategories", IsThreadSafe = true)] extern internal static int PhysicsSpace_SetBatchProxyCategories(PhysicsSpace space, ReadOnlySpan<PhysicsSpace.ProxyHandle> proxies, ReadOnlySpan<PhysicsMask> categories);
        [NativeMethod(Name = "GetBatchProxyAABB", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_GetBatchProxyAABB(PhysicsSpace space, ReadOnlySpan<PhysicsSpace.ProxyHandle> proxies, Allocator allocator);
        [NativeMethod(Name = "GetBatchProxyCategories", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_GetBatchProxyCategories(PhysicsSpace space, ReadOnlySpan<PhysicsSpace.ProxyHandle> proxies, Allocator allocator);
        [NativeMethod(Name = "GetBatchProxyUserHandle", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_GetBatchProxyUserHandle(PhysicsSpace space, ReadOnlySpan<PhysicsSpace.ProxyHandle> proxies, Allocator allocator);
        [NativeMethod(Name = "OverlapAABB", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_OverlapAABB(PhysicsSpace space, PhysicsAABB aabb, PhysicsMask categories, Allocator allocator);
        [NativeMethod(Name = "CastRay", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_CastRay(PhysicsSpace space, PhysicsQuery.CastRayInput input, PhysicsMask categories, Allocator allocator);
        [NativeMethod(Name = "CastShape", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsSpace_CastShape(PhysicsSpace space, PhysicsQuery.CastShapeInput input, PhysicsMask categories, Allocator allocator);
    }
}
