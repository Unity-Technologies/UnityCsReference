// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsPlane::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsPlane_IsValid(PhysicsPlane plane);
        [NativeMethod(Name = "PhysicsPlane::GetSeparation", IsThreadSafe = true)] extern internal static float PhysicsPlane_GetSeparation(PhysicsPlane plane, Vector2 point);
    }
}
