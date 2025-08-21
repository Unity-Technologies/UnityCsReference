// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics2D
{
    internal static partial class PhysicsLowLevelScripting2D
    {
        [NativeMethod(Name = "PhysicsTransform::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsTransform_IsValid(PhysicsTransform transform);
        [NativeMethod(Name = "PhysicsTransform::TransformPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsTransform_TransformPoint(PhysicsTransform transform, Vector2 point);
        [NativeMethod(Name = "PhysicsTransform::InverseTransformPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsTransform_InverseTransformPoint(PhysicsTransform transform, Vector2 point);
        [NativeMethod(Name = "PhysicsTransform::MultiplyTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsTransform_MultiplyTransform(PhysicsTransform transform1, PhysicsTransform PhysicsTransform);
        [NativeMethod(Name = "PhysicsTransform::InverseMultiplyTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsTransform_InverseMultiplyTransform(PhysicsTransform transform1, PhysicsTransform PhysicsTransform);

        [NativeMethod(Name = "PhysicsRotate::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsRotate_IsValid(PhysicsRotate rotation);
        [NativeMethod(Name = "PhysicsRotate::GetAngle", IsThreadSafe = true)] extern internal static float PhysicsRotate_GetAngle(PhysicsRotate rotate);
        [NativeMethod(Name = "PhysicsRotate::SetAngle", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_SetAngle(float angle);
        [NativeMethod(Name = "PhysicsRotate::GetRelativeAngle", IsThreadSafe = true)] extern internal static float PhysicsRotate_GetRelativeAngle(PhysicsRotate rotation1, PhysicsRotate rotation2);
        [NativeMethod(Name = "PhysicsRotate::UnwindAngle", IsThreadSafe = true)] extern internal static float PhysicsRotate_UnwindAngle(float angle);
        [NativeMethod(Name = "PhysicsRotate::IntegrateRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_IntegrateRotation(PhysicsRotate rotation, float deltaAngle);
        [NativeMethod(Name = "PhysicsRotate::LerpRotation", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_LerpRotation(PhysicsRotate rotationA, PhysicsRotate rotationB, float interval);
        [NativeMethod(Name = "PhysicsRotate::AngularVelocity", IsThreadSafe = true)] extern internal static float PhysicsRotate_AngularVelocity(PhysicsRotate rotationA, PhysicsRotate rotationB, float deltaTime);
        [NativeMethod(Name = "PhysicsRotate::Normalize", IsThreadSafe = true)] extern internal static PhysicsRotate PhysicsRotate_Normalize(PhysicsRotate rotation);
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

        [NativeMethod(Name = "PhysicsLowLevel2D::SetSafetyLocksEnabled")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static void PhysicsGlobal_SetSafetyLocksEnabled(bool flag);
        [NativeMethod(Name = "PhysicsLowLevel2D::GetSafetyLocksEnabled")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static bool PhysicsGlobal_GetSafetyLocksEnabled();
        [NativeMethod(Name = "PhysicsLowLevel2D::GetBypassLowLevel")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static bool PhysicsGlobal_GetBypassLowLevel();
        [NativeMethod(Name = "PhysicsLowLevel2D::IsRenderingAllowed")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static bool PhysicsGlobal_IsRenderingAllowed();
        [NativeMethod(Name = "PhysicsLowLevel2D::GetConcurrentSimulations")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static int PhysicsGlobal_GetConcurrentSimulations();
        [NativeMethod(Name = "PhysicsLowLevel2D::GetLengthUnitsPerMeter")] extern internal static float PhysicsGlobal_GetLengthUnitsPerMeter();
        [NativeMethod(Name = "PhysicsLowLevel2D::GetPhysicsLayerNames")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static object PhysicsGlobal_GetPhysicsLayers();
        [NativeMethod(Name = "PhysicsLowLevel2D::GetUseFullLayers")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static bool PhysicsGlobal_GetUseFullLayers();

        [NativeMethod(Name = "PhysicsWorldManager2D::PopulateWorldTransformWrite")] [StaticAccessor("GetPhysicsLowLevel2D()->GetWorldManager2D()", StaticAccessorType.Arrow)] extern internal static int PhysicsGlobal_PopulateWorldTransformWrite(PhysicsWorld world, IntPtr transformAccessArrayIntPtr, Span<PhysicsBody.TransformWriteTween> transformWriteTweensArray);

        [NativeMethod(Name = "PhysicsLowLevel2D::ReadProjectSettings")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static void PhysicsGlobal_ReadProjectSettings();
        [NativeMethod(Name = "PhysicsLowLevel2D::GetPhysicsLowLevelSettings")][StaticAccessor("GetPhysicsLowLevel2D()", StaticAccessorType.Arrow)] extern internal static Object PhysicsGlobal_GetPhysicsLowLevelSettings();
        #endregion
    }
}
