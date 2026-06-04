// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsCore2D::RegisterTransformWatcher")] extern internal static void PhysicsCore2D_RegisterTransformWatcher(Transform transform);
        [NativeMethod(Name = "PhysicsCore2D::UnregisterTransformWatcher")] extern internal static void PhysicsCore2D_UnregisterTransformWatcher(Transform transform);
    }
}
