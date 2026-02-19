// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    internal static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsTransform::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsTransform_IsValid(PhysicsTransform transform);
        [NativeMethod(Name = "PhysicsTransform::TransformPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsTransform_TransformPoint(PhysicsTransform transform, Vector2 point);
        [NativeMethod(Name = "PhysicsTransform::InverseTransformPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsTransform_InverseTransformPoint(PhysicsTransform transform, Vector2 point);
        [NativeMethod(Name = "PhysicsTransform::MultiplyTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsTransform_MultiplyTransform(PhysicsTransform transform1, PhysicsTransform PhysicsTransform);
        [NativeMethod(Name = "PhysicsTransform::InverseMultiplyTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsTransform_InverseMultiplyTransform(PhysicsTransform transform1, PhysicsTransform PhysicsTransform);

        [NativeMethod(Name = "PhysicsRotate::Create", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_CreateAngle(float angle);
        [NativeMethod(Name = "PhysicsRotate::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsRotate_IsValid(PhysicsRotate rotation);
        [NativeMethod(Name = "PhysicsRotate::GetAngle", IsThreadSafe = true)] extern internal static float PhysicsRotate_GetAngle(PhysicsRotate rotate);
        [NativeMethod(Name = "PhysicsRotate::GetRelativeAngle", IsThreadSafe = true)] extern internal static float PhysicsRotate_GetRelativeAngle(PhysicsRotate rotation1, PhysicsRotate rotation2);
        [NativeMethod(Name = "PhysicsRotate::UnwindAngle", IsThreadSafe = true)] extern internal static float PhysicsRotate_UnwindAngle(float angle);
        [NativeMethod(Name = "PhysicsRotate::IntegrateRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_IntegrateRotation(PhysicsRotate rotation, float deltaAngle);
        [NativeMethod(Name = "PhysicsRotate::LerpRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_LerpRotation(PhysicsRotate rotationA, PhysicsRotate rotationB, float interval);
        [NativeMethod(Name = "PhysicsRotate::AngularVelocity", IsThreadSafe = true)] extern internal static float PhysicsRotate_AngularVelocity(PhysicsRotate rotationA, PhysicsRotate rotationB, float deltaTime);
        [NativeMethod(Name = "PhysicsRotate::Normalize", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_Normalize(in Vector2 direction);
        [NativeMethod(Name = "PhysicsRotate::IsNormalized", IsThreadSafe = true)] extern internal static bool PhysicsRotate_IsNormalized(PhysicsRotate rotation);
        [NativeMethod(Name = "PhysicsRotate::MultiplyRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_MultiplyRotation(PhysicsRotate rotation1, PhysicsRotate rotation2);
        [NativeMethod(Name = "PhysicsRotate::InverseMultiplyRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_InverseMultiplyRotation(PhysicsRotate rotation1, PhysicsRotate rotation2);
        [NativeMethod(Name = "PhysicsRotate::RotateVector", IsThreadSafe = true)] extern internal static Vector2 PhysicsRotate_RotateVector(PhysicsRotate rotation, Vector2 vector);
        [NativeMethod(Name = "PhysicsRotate::InverseRotateVector", IsThreadSafe = true)] extern internal static Vector2 PhysicsRotate_InverseRotateVector(PhysicsRotate rotation, Vector2 vector);
        [NativeMethod(Name = "PhysicsRotate::Rotate", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_Rotate(PhysicsRotate rotation, float deltaAngle);

        [NativeMethod(Name = "PhysicsAABB::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsAABB_IsValid(PhysicsAABB aabb);
        [NativeMethod(Name = "PhysicsAABB::OverlapPoint", IsThreadSafe = true)] extern internal static bool PhysicsAABB_OverlapPoint(PhysicsAABB aabb, Vector2 point);
        [NativeMethod(Name = "PhysicsAABB::CastRay", IsThreadSafe = true)] extern internal static PhysicsQuery.CastResult PhysicsAABB_CastRay(PhysicsAABB aabb, PhysicsQuery.CastRayInput input);
        [NativeMethod(Name = "PhysicsAABB::Overlap", IsThreadSafe = true)] extern internal static bool PhysicsAABB_Overlap(PhysicsAABB aabb1, PhysicsAABB aabb2);
        [NativeMethod(Name = "PhysicsAABB::Union", IsThreadSafe = true)] extern internal static PhysicsAABB PhysicsAABB_Union(PhysicsAABB aabb1, PhysicsAABB aabb2);
        [NativeMethod(Name = "PhysicsAABB::Contains", IsThreadSafe = true)] extern internal static bool PhysicsAABB_Contains(PhysicsAABB aabb1, PhysicsAABB aabb2);
        [NativeMethod(Name = "PhysicsAABB::Center", IsThreadSafe = true)] extern internal static Vector2 PhysicsAABB_Center(PhysicsAABB aabb);
        [NativeMethod(Name = "PhysicsAABB::Extents", IsThreadSafe = true)] extern internal static Vector2 PhysicsAABB_Extents(PhysicsAABB aabb);
        [NativeMethod(Name = "PhysicsAABB::Perimeter", IsThreadSafe = true)] extern internal static float PhysicsAABB_Perimeter(PhysicsAABB aabb);

        [NativeMethod(Name = "PhysicsPlane::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsPlane_IsValid(PhysicsPlane plane);
        [NativeMethod(Name = "PhysicsPlane::GetSeparation", IsThreadSafe = true)] extern internal static float PhysicsPlane_GetSeparation(PhysicsPlane plane, Vector2 point);

        [NativeMethod(Name = "PhysicsMath::PI", IsThreadSafe = true)] extern internal static float PhysicsMath_PI();
        [NativeMethod(Name = "PhysicsMath::TAU", IsThreadSafe = true)] extern internal static float PhysicsMath_TAU();
        [NativeMethod(Name = "PhysicsMath::ToDegrees", IsThreadSafe = true)] extern internal static float PhysicsMath_ToDegrees(float radians);
        [NativeMethod(Name = "PhysicsMath::ToRadians", IsThreadSafe = true)] extern internal static float PhysicsMath_ToRadians(float degrees);
        [NativeMethod(Name = "PhysicsMath::Atan2", IsThreadSafe = true)] extern internal static float PhysicsMath_Atan2(float y, float x);
        [NativeMethod(Name = "PhysicsMath::CosSin", IsThreadSafe = true)] extern internal static void PhysicsMath_CosSin(float angle, out float cos, out float sin);
        [NativeMethod(Name = "PhysicsMath::SpringDamper", IsThreadSafe = true)] extern internal static float PhysicsMath_SpringDamper(float frequency, float damping, float translation, float speed, float deltaTime);

        #region Globals

        [NativeMethod(Name = "PhysicsCore2D::Global_ReadProjectSettings")] extern internal static void PhysicsGlobal_ReadProjectSettings();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetPhysicsCoreSettings")] extern internal static UnityEngine.Object PhysicsGlobal_GetPhysicsCoreSettings();

        [NativeMethod(Name = "PhysicsCore2D::Global_GetObject", IsThreadSafe = true)] extern internal static UnityEngine.Object PhysicsGlobal_GetObject(EntityId entityId);
        [NativeMethod(Name = "PhysicsCore2D::Global_GetDefaultWorld", IsThreadSafe = true)] extern internal static PhysicsWorld PhysicsGlobal_GetDefaultWorld();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetMaximumWorldsAllocated")] extern internal static int PhysicsGlobal_GetMaximumWorldsAllocated();       
        [NativeMethod(Name = "PhysicsCore2D::Global_GetSafetyLocksEnabled")] extern internal static bool PhysicsGlobal_GetSafetyLocksEnabled();
        [NativeMethod(Name = "PhysicsCore2D::Global_SetSafetyLocksEnabled")] extern internal static void PhysicsGlobal_SetSafetyLocksEnabled(bool flag);
        [NativeMethod(Name = "PhysicsCore2D::Global_GetDisableSimulation")]extern internal static bool PhysicsGlobal_GetDisableSimulation();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetAlwaysDrawWorlds")] extern internal static bool PhysicsGlobal_GetAlwaysDrawWorlds();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetConcurrentSimulations")] extern internal static int PhysicsGlobal_GetConcurrentSimulations();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetUsePhysicsLayers")] extern internal static bool PhysicsGlobal_GetUsePhysicsLayers();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetPhysicsLayerNames")] extern internal static object PhysicsGlobal_GetPhysicsLayers();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetLengthUnitsPerMeter")] extern internal static float PhysicsGlobal_GetLengthUnitsPerMeter();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetTransformChangeMode")] extern internal static PhysicsWorld.TransformChangeMode PhysicsGlobal_GetTransformChangeMode();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetRenderingMode")] extern internal static PhysicsWorld.RenderingMode PhysicsGlobal_GetRenderingMode();
        [NativeMethod(Name = "PhysicsCore2D::Global_IsRenderingAllowed")] extern internal static bool PhysicsGlobal_IsRenderingAllowed();
        [NativeMethod(Name = "PhysicsCore2D::Global_CalculateWorldTransformWrite")] extern internal static PhysicsBuffer PhysicsGlobal_CalculateWorldTransformWrite(PhysicsWorld world, PhysicsWorld.TransformPlane transformPlane, PhysicsWorld.TransformPlaneCustom transformPlaneCustom, PhysicsWorld.TransformWriteMode transformWriteMode, IntPtr transformAccessArrayIntPtr);
        [NativeMethod(Name = "PhysicsCore2D::Global_CheckTransformChanges")] extern internal static int PhysicsGlobal_CheckTransformChanges();
        [NativeMethod(Name = "PhysicsCore2D::Global_GetContactFilterMode")] extern internal static PhysicsShape.ContactFilterMode PhysicsGlobal_GetContactFilterMode();

        #endregion
    }
}
