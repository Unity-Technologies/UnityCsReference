// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;

using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics2D
{
    internal static partial class PhysicsLowLevelScripting2D
    {
        [NativeMethod(Name = "PhysicsChain::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsChainDefinition PhysicsChain_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsChain::Create")] extern internal static PhysicsChain PhysicsChain_Create(PhysicsBody body, ChainGeometry geometry, PhysicsChainDefinition definition);
        [NativeMethod(Name = "PhysicsChain::Destroy")] extern internal static bool PhysicsChain_Destroy(PhysicsChain chain, int ownerKey);
        [NativeMethod(Name = "PhysicsChain::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsChain_IsValid(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::GetWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsChain_GetWorld(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::GetBody", IsThreadSafe = true)] extern internal static PhysicsBody PhysicsChain_GetBody(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetFriction", IsThreadSafe = true)] extern internal static void PhysicsChain_SetFriction(PhysicsChain chain, float friction);
        [NativeMethod(Name = "PhysicsChain::GetFriction", IsThreadSafe = true)] extern internal static float PhysicsChain_GetFriction(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetBounciness", IsThreadSafe = true)] extern internal static void PhysicsChain_SetBounciness(PhysicsChain chain, float bounciness);
        [NativeMethod(Name = "PhysicsChain::GetBounciness", IsThreadSafe = true)] extern internal static float PhysicsChain_GetBounciness(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetFrictionMixing", IsThreadSafe = true)] extern internal static void PhysicsChain_SetFrictionMixing(PhysicsChain chain, PhysicsShape.SurfaceMaterial.MixingMode frictionMixing);
        [NativeMethod(Name = "PhysicsChain::GetFrictionMixing", IsThreadSafe = true)] extern internal static PhysicsShape.SurfaceMaterial.MixingMode PhysicsChain_GetFrictionMixing(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetBouncinessMixing", IsThreadSafe = true)] extern internal static void PhysicsChain_SetBouncinessMixing(PhysicsChain chain, PhysicsShape.SurfaceMaterial.MixingMode bouncinessMixing);
        [NativeMethod(Name = "PhysicsChain::GetBouncinessMixing", IsThreadSafe = true)] extern internal static PhysicsShape.SurfaceMaterial.MixingMode PhysicsChain_GetBouncinessMixing(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::GetSegmentCount", IsThreadSafe = true)] extern internal static int PhysicsChain_GetSegmentCount(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::GetSegments", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsChain_GetSegments(PhysicsChain chain, Allocator allocator);
        [NativeMethod(Name = "PhysicsChain::GetSegmentIndex", IsThreadSafe = true)] extern internal static int PhysicsChain_GetSegmentIndex(PhysicsChain chain, PhysicsShape chainSegmentShape);
        [NativeMethod(Name = "PhysicsChain::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsChain_CalculateAABB(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsChain_ClosestPoint(PhysicsChain chain, Vector2 point, out PhysicsShape chainSegmentShape);
        [NativeMethod(Name = "PhysicsChain::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsChain_CastRay(PhysicsChain chain, PhysicsQuery.CastRayInput input, out PhysicsShape chainSegmentShape);
        [NativeMethod(Name = "PhysicsChain::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsChain_CastShape(PhysicsChain chain, PhysicsQuery.CastShapeInput input, out PhysicsShape chainSegmentShape);
        [NativeMethod(Name = "PhysicsChain::SetOwner", IsThreadSafe = true)] extern internal static int PhysicsChain_SetOwner(PhysicsChain chain, Object ownerObject);
        [NativeMethod(Name = "PhysicsChain::GetOwner", IsThreadSafe = true)] extern internal static Object PhysicsChain_GetOwner(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::IsOwned", IsThreadSafe = true)] extern internal static bool PhysicsChain_IsOwned(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetCallbackTarget", IsThreadSafe = true)] extern internal static void PhysicsChain_SetCallbackTarget(PhysicsChain chain, System.Object callbackTarget);
        [NativeMethod(Name = "PhysicsChain::GetCallbackTarget", IsThreadSafe = true)] extern internal static System.Object PhysicsChain_GetCallbackTarget(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetUserData", IsThreadSafe = true)] extern internal static void PhysicsChain_SetUserData(PhysicsChain chain, PhysicsUserData physicsUserData);
        [NativeMethod(Name = "PhysicsChain::GetUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsChain_GetUserData(PhysicsChain chain);
        [NativeMethod(Name = "PhysicsChain::SetOwnerUserData", IsThreadSafe = true)] extern internal static void PhysicsChain_SetOwnerUserData(PhysicsChain chain, PhysicsUserData physicsUserData, int ownerKey);
        [NativeMethod(Name = "PhysicsChain::GetOwnerUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsChain_GetOwnerUserData(PhysicsChain chain);
    }
}
