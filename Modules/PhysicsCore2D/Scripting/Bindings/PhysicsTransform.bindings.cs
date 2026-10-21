// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsTransform::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsTransform_IsValid(PhysicsTransform transform);
        [NativeMethod(Name = "PhysicsTransform::TransformPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsTransform_TransformPoint(PhysicsTransform transform, Vector2 point);
        [NativeMethod(Name = "PhysicsTransform::InverseTransformPoint", IsThreadSafe = true)] extern internal static Vector2 PhysicsTransform_InverseTransformPoint(PhysicsTransform transform, Vector2 point);
        [NativeMethod(Name = "PhysicsTransform::MultiplyTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsTransform_MultiplyTransform(PhysicsTransform transform1, PhysicsTransform PhysicsTransform);
        [NativeMethod(Name = "PhysicsTransform::InverseMultiplyTransform", IsThreadSafe = true)] extern internal static PhysicsTransform PhysicsTransform_InverseMultiplyTransform(PhysicsTransform transform1, PhysicsTransform PhysicsTransform);
    }
}
