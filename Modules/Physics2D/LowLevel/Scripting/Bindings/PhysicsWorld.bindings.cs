// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using Unity.Collections;

using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics2D
{
    [NativeHeader("Modules/Physics2D/LowLevel/PhysicsLowLevel2D.h")]
    [NativeHeader("Modules/Physics2D/LowLevel/PhysicsWorldManager2D.h")]
    [StaticAccessor("PhysicsLowLevel2D", StaticAccessorType.DoubleColon)]
    internal static partial class PhysicsLowLevelScripting2D
    {
        [NativeMethod(Name = "PhysicsWorld::GetDefaultDefinition", IsThreadSafe = true)] extern internal static PhysicsWorldDefinition PhysicsWorld_GetDefaultDefinition(bool useSettings);
        [NativeMethod(Name = "PhysicsWorld::GetDefaultExplosionDefinition", IsThreadSafe = true)] extern internal static PhysicsWorld.ExplosionDefinition PhysicsWorld_GetDefaultExplosionDefinition();
        [NativeMethod(Name = "PhysicsWorld::Create")] extern internal static PhysicsWorld PhysicsWorld_Create(PhysicsWorldDefinition definition);
        [NativeMethod(Name = "PhysicsWorld::Destroy")] extern internal static bool PhysicsWorld_Destroy(PhysicsWorld world, int ownerKey);
        [NativeMethod(Name = "PhysicsWorld::WriteDefinition")] extern internal static void PhysicsWorld_WriteDefinition(PhysicsWorld world, PhysicsWorldDefinition definition, bool onlyExtendedProperties);
        [NativeMethod(Name = "PhysicsWorld::ReadDefinition")] extern internal static PhysicsWorldDefinition PhysicsWorld_ReadDefinition(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::Reset")] extern internal static void PhysicsWorld_Reset(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsWorld_IsValid(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::IsEmpty", IsThreadSafe = true)] extern internal static bool PhysicsWorld_IsEmpty(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetPaused", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetPaused(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetPaused", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetPaused(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetSleepingAllowed")] extern internal static void PhysicsWorld_SetSleepingAllowed(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetSleepingAllowed", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetSleepingAllowed(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetContinuousAllowed")] extern internal static void PhysicsWorld_SetContinuousAllowed(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetContinuousAllowed", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetContinuousAllowed(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetWarmStartingAllowed")] extern internal static void PhysicsWorld_SetWarmStartingAllowed(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetWarmStartingAllowed", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetWarmStartingAllowed(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetContactFilterCallbacks")] extern internal static void PhysicsWorld_SetContactFilterCallbacks(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetContactFilterCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetContactFilterCallbacks(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetPreSolveCallbacks")] extern internal static void PhysicsWorld_SetPreSolveCallbacks(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetPreSolveCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetPreSolveCallbacks(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetAutoBodyUpdateCallbacks")] extern internal static void PhysicsWorld_SetAutoBodyUpdateCallbacks(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetAutoBodyUpdateCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetAutoBodyUpdateCallbacks(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetAutoContactCallbacks")] extern internal static void PhysicsWorld_SetAutoContactCallbacks(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetAutoContactCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetAutoContactCallbacks(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetAutoTriggerCallbacks")] extern internal static void PhysicsWorld_SetAutoTriggerCallbacks(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetAutoTriggerCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetAutoTriggerCallbacks(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetAutoJointThresholdCallbacks")] extern internal static void PhysicsWorld_SetAutoJointThresholdCallbacks(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetAutoJointThresholdCallbacks", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetAutoJointThresholdCallbacks(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetBounceThreshold")] extern internal static void PhysicsWorld_SetBounceThreshold(PhysicsWorld world, float value);
        [NativeMethod(Name = "PhysicsWorld::GetBounceThreshold", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetBounceThreshold(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetContactHitEventThreshold")] extern internal static void PhysicsWorld_SetContactHitEventThreshold(PhysicsWorld world, float value);
        [NativeMethod(Name = "PhysicsWorld::GetContactHitEventThreshold", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetContactHitEventThreshold(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetContactFrequency")] extern internal static void PhysicsWorld_SetContactFrequency(PhysicsWorld world, float contactFrequency);
        [NativeMethod(Name = "PhysicsWorld::GetContactFrequency", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetContactFrequency(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetContactDamping")] extern internal static void PhysicsWorld_SetContactDamping(PhysicsWorld world, float damping);
        [NativeMethod(Name = "PhysicsWorld::GetContactDamping", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetContactDamping(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetContactSpeed")] extern internal static void PhysicsWorld_SetContactSpeed(PhysicsWorld world, float contactSpeed);
        [NativeMethod(Name = "PhysicsWorld::GetContactSpeed", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetContactSpeed(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetMaximumLinearSpeed")] extern internal static void PhysicsWorld_SetMaximumLinearSpeed(PhysicsWorld world, float maximumLinearSpeed);
        [NativeMethod(Name = "PhysicsWorld::GetMaximumLinearSpeed")] extern internal static float PhysicsWorld_GetMaximumLinearSpeed(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetGravity")] extern internal static void PhysicsWorld_SetGravity(PhysicsWorld world, Vector2 gravity);
        [NativeMethod(Name = "PhysicsWorld::GetGravity", IsThreadSafe = true)] extern internal static Vector2 PhysicsWorld_GetGravity(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetSimulationWorkers")] extern internal static void PhysicsWorld_SetSimulationWorkers(PhysicsWorld world, int simulationWorkers);
        [NativeMethod(Name = "PhysicsWorld::GetSimulationWorkers", IsThreadSafe = true)] extern internal static int PhysicsWorld_GetSimulationWorkers(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetSimulationType")] extern internal static void PhysicsWorld_SetSimulationType(PhysicsWorld world, PhysicsWorld.SimulationType simulationType);
        [NativeMethod(Name = "PhysicsWorld::GetSimulationType", IsThreadSafe = true)] extern internal static PhysicsWorld.SimulationType PhysicsWorld_GetSimulationType(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetSimulationSubSteps")] extern internal static void PhysicsWorld_SetSimulationSubSteps(PhysicsWorld world, int subStepCpunt);
        [NativeMethod(Name = "PhysicsWorld::GetSimulationSubSteps", IsThreadSafe = true)] extern internal static int PhysicsWorld_GetSimulationSubSteps(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetLastSimulationTimestamp", IsThreadSafe = true)] extern internal static double PhysicsWorld_GetLastSimulationTimestamp(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetLastSimulationDeltaTime", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetLastSimulationDeltaTime(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetTransformPlane", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetTransformPlane(PhysicsWorld world, PhysicsWorld.TransformPlane transformPlane);
        [NativeMethod(Name = "PhysicsWorld::GetTransformPlane", IsThreadSafe = true)] extern internal static PhysicsWorld.TransformPlane PhysicsWorld_GetTransformPlane(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetTransformWriteMode", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetTransformWriteMode(PhysicsWorld world, PhysicsWorld.TransformWriteMode transformWriteMode);
        [NativeMethod(Name = "PhysicsWorld::GetTransformWriteMode", IsThreadSafe = true)] extern internal static PhysicsWorld.TransformWriteMode PhysicsWorld_GetTransformWriteMode(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetTransformTweening", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetTransformTweening(PhysicsWorld world, bool flag);
        [NativeMethod(Name = "PhysicsWorld::GetTransformTweening", IsThreadSafe = true)] extern internal static bool PhysicsWorld_GetTransformTweening(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::ClearTransformWriteTweens", IsThreadSafe = true)] extern internal static void PhysicsWorld_ClearTransformWriteTweens(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetTransformWriteTweens", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetTransformWriteTweens(PhysicsWorld world, ReadOnlySpan<PhysicsBody.TransformWriteTween> transformWriteTweens);
        [NativeMethod(Name = "PhysicsWorld::GetTransformWriteTweens", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetTransformWriteTweens(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::Simulate")] extern internal static void PhysicsWorld_Simulate(PhysicsWorld world, float timeStep, PhysicsWorld.SimulationType expectedSimulationType);
        [NativeMethod(Name = "PhysicsWorld::SimulateBatch")] extern internal static void PhysicsWorld_SimulateBatch(ReadOnlySpan<PhysicsWorld> worlds, float timeStep, PhysicsWorld.SimulationType expectedSimulationType);
        [NativeMethod(Name = "PhysicsWorld::Explode")] extern internal static void PhysicsWorld_Explode(PhysicsWorld world, PhysicsWorld.ExplosionDefinition definition);
        [NativeMethod(Name = "PhysicsWorld::GetBodyUpdateUserData", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetBodyUpdateUserData(PhysicsWorld world, bool ownerUserData, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetBodyUpdateEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetBodyUpdateEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetTriggerBeginEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetTriggerBeginEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetTriggerEndEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetTriggerEndEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetContactBeginEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetContactBeginEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetContactEndEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetContactEndEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetContactHitEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetContactHitEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetJointThresholdEvents", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetJointThresholdEvents(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetBodyUpdateCallbackTargets", IsThreadSafe = true)] extern internal static PhysicsCallbacks.BodyUpdateCallbackTargets PhysicsWorld_GetBodyUpdateCallbackTargets(PhysicsWorld world, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetTriggerCallbackTargets", IsThreadSafe = true)] extern internal static PhysicsCallbacks.TriggerCallbackTargets PhysicsWorld_GetTriggerCallbackTargets(PhysicsWorld world, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetContactCallbackTargets", IsThreadSafe = true)] extern internal static PhysicsCallbacks.ContactCallbackTargets PhysicsWorld_GetContactCallbackTargets(PhysicsWorld world, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetJointThresholdCallbackTargets", IsThreadSafe = true)] extern internal static PhysicsCallbacks.JointThresholdCallbackTargets PhysicsWorld_GetJointThresholdCallbackTargets(PhysicsWorld world, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::TestOverlapAABB", IsThreadSafe = true)] extern internal static bool PhysicsWorld_TestOverlapAABB(PhysicsWorld world, PhysicsAABB aabb, PhysicsQuery.QueryFilter filter);
        [NativeMethod(Name = "PhysicsWorld::TestOverlapShapeProxy", IsThreadSafe = true)] extern internal static bool PhysicsWorld_TestOverlapShapeProxy(PhysicsWorld world, PhysicsShape.ShapeProxy shapeProxy, PhysicsQuery.QueryFilter filter);
        [NativeMethod(Name = "PhysicsWorld::OverlapAABB", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_OverlapAABB(PhysicsWorld world, PhysicsAABB aabb, PhysicsQuery.QueryFilter filter, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::OverlapShapeProxy", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_OverlapShapeProxy(PhysicsWorld world, PhysicsShape.ShapeProxy shapeProxy, PhysicsQuery.QueryFilter filter, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::CastRay", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_CastRay(PhysicsWorld world, PhysicsQuery.CastRayInput input, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::CastShapeProxy", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_CastShapeProxy(PhysicsWorld world, PhysicsShape.ShapeProxy shapeProxy, Vector2 translation, PhysicsQuery.QueryFilter filter, PhysicsQuery.WorldCastMode castMode, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::CastMover", IsThreadSafe = true)] extern internal static PhysicsQuery.WorldMoverResult PhysicsWorld_CastMover(PhysicsWorld world, PhysicsQuery.WorldMoverInput input);
        [NativeMethod(Name = "PhysicsWorld::GetAwakeBodyCount", IsThreadSafe = true)] extern internal static int PhysicsWorld_GetAwakeBodyCount(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetCounters", IsThreadSafe = true)] extern internal static PhysicsWorld.WorldCounters PhysicsWorld_GetCounters(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetProfile", IsThreadSafe = true)] extern internal static PhysicsWorld.WorldProfile PhysicsWorld_GetProfile(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetGlobalCounters")] extern internal static PhysicsWorld.WorldCounters PhysicsWorld_GetGlobalCounters();
        [NativeMethod(Name = "PhysicsWorld::GetGlobalProfile")] extern internal static PhysicsWorld.WorldProfile PhysicsWorld_GetGlobalProfile();
        [NativeMethod(Name = "PhysicsWorld::GetWorldCount", IsThreadSafe = true)] extern internal static int PhysicsWorld_GetWorldCount();
        [NativeMethod(Name = "PhysicsWorld::GetWorlds", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetWorlds(Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetBodies", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetBodies(PhysicsWorld world, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetJoints", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsWorld_GetJoints(PhysicsWorld world, Allocator allocator);
        [NativeMethod(Name = "PhysicsWorld::GetHugeWorldExtent", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetHugeWorldExtent();
        [NativeMethod(Name = "PhysicsWorld::GetLinearSlop", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetLinearSlop();
        [NativeMethod(Name = "PhysicsWorld::GetSpeculativeContactDistance", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetSpeculativeContactDistance();
        [NativeMethod(Name = "PhysicsWorld::GetAABBMargin", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetAABBMargin();
        [NativeMethod(Name = "PhysicsWorld::GetBodyMaxRotation", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetBodyMaxRotation();
        [NativeMethod(Name = "PhysicsWorld::GetBodyTimeToSleep", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetBodyTimeToSleep();
        [NativeMethod(Name = "PhysicsWorld::SetDrawOptions", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawOptions(PhysicsWorld world, PhysicsWorld.DrawOptions drawOptions);
        [NativeMethod(Name = "PhysicsWorld::GetDrawOptions", IsThreadSafe = true)] extern internal static PhysicsWorld.DrawOptions PhysicsWorld_GetDrawOptions(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawFillOptions", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawFillOptions(PhysicsWorld world, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::GetDrawFillOptions", IsThreadSafe = true)] extern internal static PhysicsWorld.DrawFillOptions PhysicsWorld_GetDrawFillOptions(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawColors", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawColors(PhysicsWorld world, PhysicsWorld.DrawColors drawColors);
        [NativeMethod(Name = "PhysicsWorld::GetDrawColors", IsThreadSafe = true)] extern internal static PhysicsWorld.DrawColors PhysicsWorld_GetDrawColors(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawThickness", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawThickness(PhysicsWorld world, float thickness);
        [NativeMethod(Name = "PhysicsWorld::GetDrawThickness", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetDrawThickness(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawFillAlpha", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawFillAlpha(PhysicsWorld world, float alpha);
        [NativeMethod(Name = "PhysicsWorld::GetDrawFillAlpha", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetDrawFillAlpha(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawPointScale", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawPointScale(PhysicsWorld world, float scale);
        [NativeMethod(Name = "PhysicsWorld::GetDrawPointScale", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetDrawPointScale(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawNormalScale", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawNormalScale(PhysicsWorld world, float scale);
        [NativeMethod(Name = "PhysicsWorld::GetDrawNormalScale", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetDrawNormalScale(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawImpulseScale", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawImpulseScale(PhysicsWorld world, float scale);
        [NativeMethod(Name = "PhysicsWorld::GetDrawImpulseScale", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetDrawImpulseScale(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawCapacity", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetDrawCapacity(PhysicsWorld world, int capacity);
        [NativeMethod(Name = "PhysicsWorld::GetDrawCapacity", IsThreadSafe = true)] extern internal static int PhysicsWorld_GetDrawCapacity(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetDrawElementDepth", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetElementDepth(PhysicsWorld world, float elementDepth);
        [NativeMethod(Name = "PhysicsWorld::GetDrawElementDepth", IsThreadSafe = true)] extern internal static float PhysicsWorld_GetElementDepth(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::GetDrawResults", IsThreadSafe = true)] extern internal static PhysicsWorld.DrawResults PhysicsWorld_GetDrawResults(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::ClearDraw", IsThreadSafe = true)] extern internal static void PhysicsWorld_ClearDraw(PhysicsWorld world, bool clearWorldDraw, bool clearCustomDraw, bool clearTimedDraw);
        [NativeMethod(Name = "PhysicsWorld::Draw", IsThreadSafe = true)] extern internal static void PhysicsWorld_Draw(PhysicsWorld world, PhysicsAABB viewAABB);
        [NativeMethod(Name = "PhysicsWorld::DrawCircleGeometry", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawCircleGeometry(PhysicsWorld world, CircleGeometry geometry, PhysicsTransform transform, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawCapsuleGeometry", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawCapsuleGeometry(PhysicsWorld world, CapsuleGeometry geometry, PhysicsTransform transform, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawPolygonGeometry", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawPolygonGeometry(PhysicsWorld world, PolygonGeometry geometry, PhysicsTransform transform, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawSegmentGeometry", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawSegmentGeometry(PhysicsWorld world, SegmentGeometry geometry, PhysicsTransform transform, Color color, float lifetime);
        [NativeMethod(Name = "PhysicsWorld::DrawCircleGeometrySpan", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawCircleGeometrySpan(PhysicsWorld world, ReadOnlySpan<CircleGeometry> geometry, PhysicsTransform transform, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawCapsuleGeometrySpan", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawCapsuleGeometrySpan(PhysicsWorld world, ReadOnlySpan<CapsuleGeometry> geometry, PhysicsTransform transform, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawPolygonGeometrySpan", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawPolygonGeometrySpan(PhysicsWorld world, ReadOnlySpan<PolygonGeometry> geometry, PhysicsTransform transform, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawSegmentGeometrySpan", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawSegmentGeometrySpan(PhysicsWorld world, ReadOnlySpan<SegmentGeometry> geometry, PhysicsTransform transform, Color color, float lifetime);
        [NativeMethod(Name = "PhysicsWorld::DrawBox", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawBox(PhysicsWorld world, PhysicsTransform transform, Vector2 size, float radius, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawCircle", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawCircle(PhysicsWorld world, Vector2 center, float radius, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawCapsule", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawCapsule(PhysicsWorld world, PhysicsTransform transform, Vector2 center1, Vector2 center2, float radius, Color color, float lifetime, PhysicsWorld.DrawFillOptions drawFillOptions);
        [NativeMethod(Name = "PhysicsWorld::DrawPoint", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawPoint(PhysicsWorld world, Vector2 center, float radius, Color color, float lifetime);
        [NativeMethod(Name = "PhysicsWorld::DrawLine", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawLine(PhysicsWorld world, Vector2 point0, Vector2 point1, Color color, float lifetime);
        [NativeMethod(Name = "PhysicsWorld::DrawLineStrip", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawLineStrip(PhysicsWorld world, PhysicsTransform transform, ReadOnlySpan<Vector2> vertices, bool loop, Color color, float lifetime);
        [NativeMethod(Name = "PhysicsWorld::DrawTransformAxis", IsThreadSafe = true)] extern internal static void PhysicsWorld_DrawTransformAxis(PhysicsWorld world, PhysicsTransform transform, float scale, float lifetime);
        [NativeMethod(Name = "PhysicsWorld::GetRenderMaterial", IsThreadSafe = true)] extern internal static Material PhysicsWorld_GetRenderMaterial(string editorResourceName, string playerResourceName);
        [NativeMethod(Name = "PhysicsWorld::SetOwner", IsThreadSafe = true)] extern internal static int PhysicsWorld_SetOwner(PhysicsWorld world, Object ownerObject);
        [NativeMethod(Name = "PhysicsWorld::GetOwner", IsThreadSafe = true)] extern internal static Object PhysicsWorld_GetOwner(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::IsOwned", IsThreadSafe = true)] extern internal static bool PhysicsWorld_IsOwned(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetUserData", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetUserData(PhysicsWorld world, PhysicsUserData physicsUserData);
        [NativeMethod(Name = "PhysicsWorld::GetUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsWorld_GetUserData(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::SetOwnerUserData", IsThreadSafe = true)] extern internal static void PhysicsWorld_SetOwnerUserData(PhysicsWorld world, PhysicsUserData physicsUserData, int ownerKey);
        [NativeMethod(Name = "PhysicsWorld::GetOwnerUserData", IsThreadSafe = true)] extern internal static PhysicsUserData PhysicsWorld_GetOwnerUserData(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::IsDefaultWorld", IsThreadSafe = true)] extern internal static bool PhysicsWorld_IsDefaultWorld(PhysicsWorld world);
        [NativeMethod(Name = "PhysicsWorld::DrawAllWorlds")] extern internal static void PhysicsWorld_DrawAllWorlds(PhysicsAABB drawAABB);

        [NativeMethod(Name = "PhysicsLowLevel2D::GetDefaultWorld")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static PhysicsWorld PhysicsWorld_GetDefaultWorld();
    }
}
