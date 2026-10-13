// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
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
    }
}
