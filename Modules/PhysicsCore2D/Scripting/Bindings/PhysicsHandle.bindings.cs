// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using Unity.Collections;
using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsHandle::Create", IsThreadSafe = true)] extern internal static PhysicsHandle PhysicsHandle_Create();
        [NativeMethod(Name = "PhysicsHandle::CreateBatch", IsThreadSafe = true)] extern internal static PhysicsBuffer PhysicsHandle_CreateBatch(int handleCount, Allocator allocator);
        [NativeMethod(Name = "PhysicsHandle::DestroyBatch", IsThreadSafe = true)] extern internal static void PhysicsHandle_DestroyBatch(ReadOnlySpan<PhysicsHandle> physicsHandles);
        [NativeMethod(Name = "PhysicsHandle::IsValid", IsThreadSafe = true)] extern internal static bool PhysicsHandle_IsValid(PhysicsHandle physicsHandle);
    }
}
