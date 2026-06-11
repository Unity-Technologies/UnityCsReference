// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsShape::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsShapeDefinition PhysicsShape_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsShape::GetDefaultSurfaceMaterial", IsThreadSafe = true)] extern internal static PhysicsShape.SurfaceMaterial PhysicsShape_GetDefaultSurfaceMaterial(bool useSettings);
        [NativeMethod(Name = "PhysicsShape::CreateCircleShape", IsThreadSafe = true)] extern internal static PhysicsShape PhysicsShape_CreateCircleShape(PhysicsBody body, CircleGeometry geometry, PhysicsShapeDefinition definition);
        [NativeMethod(Name = "PhysicsShape::CreatePolygonShape", IsThreadSafe = true)] extern internal static PhysicsShape PhysicsShape_CreatePolygonShape(PhysicsBody body, PolygonGeometry geometry, PhysicsShapeDefinition definition);
        [NativeMethod(Name = "PhysicsShape::CreateCapsuleShape", IsThreadSafe = true)] extern internal static PhysicsShape PhysicsShape_CreateCapsuleShape(PhysicsBody body, CapsuleGeometry geometry, PhysicsShapeDefinition definition);
        [NativeMethod(Name = "PhysicsShape::CreateSegmentShape", IsThreadSafe = true)] extern internal static PhysicsShape PhysicsShape_CreateSegmentShape(PhysicsBody body, SegmentGeometry geometry, PhysicsShapeDefinition definition);
        [NativeMethod(Name = "PhysicsShape::CreateChainSegmentShape", IsThreadSafe = true)] extern internal static PhysicsShape PhysicsShape_CreateChainSegmentShape(PhysicsBody body, ChainSegmentGeometry geometry, PhysicsShapeDefinition definition);
        [NativeMethod(Name = "PhysicsShape::CreateShapeBatch", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsShape_CreateShapeBatch(PhysicsBody body, PhysicsBuffer spanGeometry, PhysicsShape.ShapeType shapeType, PhysicsShapeDefinition definition, Allocator allocator);
        [NativeMethod(Name = "PhysicsShape::Destroy", IsThreadSafe = true)] extern internal static bool PhysicsShape_Destroy(PhysicsShape shape, bool updateBodyMass, int ownerKey);
        [NativeMethod(Name = "PhysicsShape::DestroyBatch", IsThreadSafe = true)] extern internal static void PhysicsShape_DestroyBatch(ReadOnlySpan<PhysicsShape> shapes, bool updateBodyMass);
        [NativeMethod(Name = "PhysicsShape::ApplyBuoyancy", IsThreadSafe = true)] extern internal static void PhysicsShape_ApplyBuoyancy(PhysicsBody.BuoyancyInput input, ReadOnlySpan<PhysicsShape> shapes, float deltaTime);
        [NativeMethod(Name = "PhysicsShape::WriteDefinition")] extern internal static void PhysicsShape_WriteDefinition(PhysicsShape shape, PhysicsShapeDefinition definition, bool onlyExtendedProperties);
        [NativeMethod(Name = "PhysicsShape::ReadDefinition")] extern internal static PhysicsShapeDefinition PhysicsShape_ReadDefinition(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsShape_IsValid(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsShape_GetWorld(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetBody", IsThreadSafe = true)] extern internal static PhysicsBody PhysicsShape_GetBody(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetIsTrigger", IsThreadSafe = true)] extern internal static void PhysicsShape_SetIsTrigger(PhysicsShape shape, bool flag);
        [NativeMethod(Name = "PhysicsShape::GetIsTrigger", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetIsTrigger(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetShapeType", IsThreadSafe = true)] extern internal static PhysicsShape.ShapeType PhysicsShape_GetShapeType(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetDensity", IsThreadSafe = true)] extern internal static void PhysicsShape_SetDensity(PhysicsShape shape, float density, bool updateBodyMass);
        [NativeMethod(Name = "PhysicsShape::GetDensity", IsThreadSafe = true)] extern internal static float PhysicsShape_GetDensity(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetMassData", IsThreadSafe = true)] extern internal static PhysicsBody.MassConfiguration PhysicsShape_GetMassConfiguration(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetFriction", IsThreadSafe = true)] extern internal static void PhysicsShape_SetFriction(PhysicsShape shape, float friction);
        [NativeMethod(Name = "PhysicsShape::GetFriction", IsThreadSafe = true)] extern internal static float PhysicsShape_GetFriction(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetBounciness", IsThreadSafe = true)] extern internal static void PhysicsShape_SetBounciness(PhysicsShape shape, float bounciness);
        [NativeMethod(Name = "PhysicsShape::GetBounciness", IsThreadSafe = true)] extern internal static float PhysicsShape_GetBounciness(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetFrictionMixing", IsThreadSafe = true)] extern internal static void PhysicsShape_SetFrictionMixing(PhysicsShape shape, PhysicsShape.SurfaceMaterial.MixingMode frictionMixing);
        [NativeMethod(Name = "PhysicsShape::GetFrictionMixing", IsThreadSafe = true)] extern internal static PhysicsShape.SurfaceMaterial.MixingMode PhysicsShape_GetFrictionMixing(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetBouncinessMixing", IsThreadSafe = true)] extern internal static void PhysicsShape_SetBouncinessMixing(PhysicsShape shape, PhysicsShape.SurfaceMaterial.MixingMode bouncinessMixing);
        [NativeMethod(Name = "PhysicsShape::GetBouncinessMixing", IsThreadSafe = true)] extern internal static PhysicsShape.SurfaceMaterial.MixingMode PhysicsShape_GetBouncinessMixing(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetFrictionPriority", IsThreadSafe = true)] extern internal static void PhysicsShape_SetFrictionPriority(PhysicsShape shape, UInt16 frictionPriority);
        [NativeMethod(Name = "PhysicsShape::GetFrictionPriority", IsThreadSafe = true)] extern internal static UInt16 PhysicsShape_GetFrictionPriority(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetBouncinessPriority", IsThreadSafe = true)] extern internal static void PhysicsShape_SetBouncinessPriority(PhysicsShape shape, UInt16 bouncinessPriority);
        [NativeMethod(Name = "PhysicsShape::GetBouncinessPriority", IsThreadSafe = true)] extern internal static UInt16 PhysicsShape_GetBouncinessPriority(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetRollingResistance", IsThreadSafe = true)] extern internal static void PhysicsShape_SetRollingResistance(PhysicsShape shape, float rollingResistance);
        [NativeMethod(Name = "PhysicsShape::GetRollingResistance", IsThreadSafe = true)] extern internal static float PhysicsShape_GetRollingResistance(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetTangentSpeed", IsThreadSafe = true)] extern internal static void PhysicsShape_SetTangentSpeed(PhysicsShape shape, float tangentSpeed);
        [NativeMethod(Name = "PhysicsShape::GetTangentSpeed", IsThreadSafe = true)] extern internal static float PhysicsShape_GetTangentSpeed(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetCustomColor", IsThreadSafe = true)] extern internal static void PhysicsShape_SetCustomColor(PhysicsShape shape, Color32 customColor);
        [NativeMethod(Name = "PhysicsShape::GetCustomColor", IsThreadSafe = true)] extern internal static Color32 PhysicsShape_GetCustomColor(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetWorldDrawing", IsThreadSafe = true)] extern internal static void PhysicsShape_SetWorldDrawing(PhysicsShape shape, bool flag);
        [NativeMethod(Name = "PhysicsShape::GetWorldDrawing", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetWorldDrawing(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetSurfaceMaterial", IsThreadSafe = true)] extern internal static void PhysicsShape_SetSurfaceMaterial(PhysicsShape shape, PhysicsShape.SurfaceMaterial surfaceMateria);
        [NativeMethod(Name = "PhysicsShape::GetSurfaceMaterial", IsThreadSafe = true)] extern internal static PhysicsShape.SurfaceMaterial PhysicsShape_GetSurfaceMaterial(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetContactFilter", IsThreadSafe = true)] extern internal static void PhysicsShape_SetContactFilter(PhysicsShape shape, PhysicsShape.ContactFilter filter);
        [NativeMethod(Name = "PhysicsShape::GetContactFilter", IsThreadSafe = true)] extern internal static PhysicsShape.ContactFilter PhysicsShape_GetContactFilter(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetMoverData", IsThreadSafe = true)] extern internal static void PhysicsShape_SetMoverData(PhysicsShape shape, PhysicsShape.MoverData moverData);
        [NativeMethod(Name = "PhysicsShape::GetMoverData", IsThreadSafe = true)] extern internal static PhysicsShape.MoverData PhysicsShape_GetMoverData(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::ApplyWind", IsThreadSafe = true)] extern internal static void PhysicsShape_ApplyWind(PhysicsBody.WindInput input, ReadOnlySpan<PhysicsShape> shapes);
        [NativeMethod(Name = "PhysicsShape::SetTriggerEvents", IsThreadSafe = true)] extern internal static void PhysicsShape_SetTriggerEvents(PhysicsShape shape, bool enableContactEvents);
        [NativeMethod(Name = "PhysicsShape::GetTriggerEvents", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetTriggerEvents(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetContactEvents", IsThreadSafe = true)] extern internal static void PhysicsShape_SetContactEvents(PhysicsShape shape, bool enableContactEvents);
        [NativeMethod(Name = "PhysicsShape::GetContactEvents", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetContactEvents(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetHitEvents", IsThreadSafe = true)] extern internal static void PhysicsShape_SetHitEvents(PhysicsShape shape, bool enableHitEvents);
        [NativeMethod(Name = "PhysicsShape::GetHitEvents", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetHitEvents(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetContactFilterCallbacks", IsThreadSafe = true)] extern internal static void PhysicsShape_SetContactFilterCallbacks(PhysicsShape shape, bool flag);
        [NativeMethod(Name = "PhysicsShape::GetContactFilterCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetContactFilterCallbacks(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetPreSolveCallbacks", IsThreadSafe = true)] extern internal static void PhysicsShape_SetPreSolveCallbacks(PhysicsShape shape, bool flag);
        [NativeMethod(Name = "PhysicsShape::GetPreSolveCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetPreSolveCallbacks(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetStartStaticContacts", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetStartStaticContacts(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetStartMassUpdate", IsThreadSafe = true)] extern internal static bool PhysicsShape_GetStartMassUpdate(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::OverlapPoint", IsThreadSafe = true)] extern internal static bool PhysicsShape_OverlapPoint(PhysicsShape shape, Vector2 point);
        [NativeMethod(Name = "PhysicsShape::ClosestPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsShape_ClosestPoint(PhysicsShape shape, Vector2 point);
        [NativeMethod(Name = "PhysicsShape::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsShape_CastRay(PhysicsShape shape, PhysicsQuery.CastRayInput input);
        [NativeMethod(Name = "PhysicsShape::CastShape", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsShape_CastShape(PhysicsShape shape, PhysicsQuery.CastShapeInput input);
        [NativeMethod(Name = "PhysicsShape::GetCircleGeometry", IsThreadSafe = true)] extern internal static CircleGeometry PhysicsShape_GetCircleGeometry(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetCapsuleGeometry", IsThreadSafe = true)] extern internal static CapsuleGeometry PhysicsShape_GetCapsuleGeometry(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetPolygonGeometry", IsThreadSafe = true)] extern internal static PolygonGeometry PhysicsShape_GetPolygonGeometry(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetSegmentGeometry", IsThreadSafe = true)] extern internal static SegmentGeometry PhysicsShape_GetSegmentGeometry(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetChainSegmentGeometry", IsThreadSafe = true)] extern internal static ChainSegmentGeometry PhysicsShape_GetChainSegmentGeometry(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetCircleGeometry", IsThreadSafe = true)] extern internal static void PhysicsShape_SetCircleGeometry(PhysicsShape shape, CircleGeometry geometry);
        [NativeMethod(Name = "PhysicsShape::SetCapsuleGeometry", IsThreadSafe = true)] extern internal static void PhysicsShape_SetCapsuleGeometry(PhysicsShape shape, CapsuleGeometry geometry);
        [NativeMethod(Name = "PhysicsShape::SetPolygonGeometry", IsThreadSafe = true)] extern internal static void PhysicsShape_SetPolygonGeometry(PhysicsShape shape, PolygonGeometry geometry);
        [NativeMethod(Name = "PhysicsShape::SetSegmentGeometry", IsThreadSafe = true)] extern internal static void PhysicsShape_SetSegmentGeometry(PhysicsShape shape, SegmentGeometry geometry);
        [NativeMethod(Name = "PhysicsShape::SetChainSegmentGeometry", IsThreadSafe = true)] extern internal static void PhysicsShape_SetChainSegmentGeometry(PhysicsShape shape, ChainSegmentGeometry geometry);
        [NativeMethod(Name = "PhysicsShape::IsChainSegmentShape", IsThreadSafe = true)] extern internal static bool PhysicsShape_IsChainSegmentShape(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetChain", IsThreadSafe = true)] extern internal static PhysicsChain PhysicsShape_GetChain(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetContacts", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsShape_GetContacts(PhysicsShape shape, Allocator allocator);
        [NativeMethod(Name = "PhysicsShape::GetTriggerVisitors", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsShape_GetTriggerVisitors(PhysicsShape shape, Allocator allocator);
        [NativeMethod(Name = "PhysicsShape::CalculateAABB", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsShape_CalculateAABB(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetLocalCenter", IsThreadSafe = true)] extern internal static Vector2 PhysicsShape_GetLocalCenter(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetPerimeter", IsThreadSafe = true)] extern internal static float PhysicsShape_GetPerimeter(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::GetPerimeterProjected", IsThreadSafe = true)] extern internal static float PhysicsShape_GetPerimeterProjected(PhysicsShape shape, Vector2 axis);
        [NativeMethod(Name = "PhysicsShape::Draw", IsThreadSafe = true)] extern internal static void PhysicsShape_Draw(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetOwner", IsThreadSafe = true)] extern internal static void PhysicsShape_SetOwner(ReadOnlySpan<PhysicsShape> shapes, UnityEngine.Object ownerObject, int ownerKey);
        [NativeMethod(Name = "PhysicsShape::GetOwner", IsThreadSafe = true)] extern internal static UnityEngine.Object PhysicsShape_GetOwner(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::IsOwned", IsThreadSafe = true)] extern internal static bool PhysicsShape_IsOwned(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetCallbackTarget", IsThreadSafe = true)] extern internal static void PhysicsShape_SetCallbackTarget(PhysicsShape shape, System.Object callbackTarget);
        [NativeMethod(Name = "PhysicsShape::GetCallbackTarget", IsThreadSafe = true)] extern internal static System.Object PhysicsShape_GetCallbackTarget(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetUserData", IsThreadSafe = true)] extern internal static void PhysicsShape_SetUserData(PhysicsShape shape, PhysicsUserData physicsUserData);
        [NativeMethod(Name = "PhysicsShape::GetUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsShape_GetUserData(PhysicsShape shape);
        [NativeMethod(Name = "PhysicsShape::SetOwnerUserData", IsThreadSafe = true)] extern internal static void PhysicsShape_SetOwnerUserData(PhysicsShape shape, PhysicsUserData physicsUserData, int ownerKey);
        [NativeMethod(Name = "PhysicsShape::GetOwnerUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsShape_GetOwnerUserData(PhysicsShape shape);

        [NativeMethod(Name = "PhysicsCore2D::ContactFilter::CanContact", IsThreadSafe = true)] extern internal static bool PhysicsShape_ContactFilter_CanContact(PhysicsShape.ContactFilter filterA, PhysicsShape.ContactFilter filterB);

        [NativeMethod(Name = "PhysicsCore2D::PhysicsContactId::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsContactId_IsValid(PhysicsShape.ContactId contactId);
        [NativeMethod(Name = "PhysicsCore2D::PhysicsContactId::GetContact", IsThreadSafe = true)] extern internal static PhysicsShape.Contact PhysicsContactId_GetContact(PhysicsShape.ContactId contactId);
    }
}
