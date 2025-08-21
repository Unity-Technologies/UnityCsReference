// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using Unity.Collections;

using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics2D
{
    [NativeHeader("Modules/Physics2D/LowLevel/PhysicsDestructor2D.h")]
    [StaticAccessor("PhysicsDestructor2D", StaticAccessorType.DoubleColon)]
    internal static class PhysicsDestructorScripting2D
    {
        [NativeMethod(Name = "Fragment", IsThreadSafe = true)] extern internal static PhysicsDestructor.FragmentResult PhysicsDestructor_Fragment(PhysicsDestructor.FragmentGeometry target, ReadOnlySpan<Vector2> fragmentPoints, Allocator allocator);
        [NativeMethod(Name = "FragmentMasked", IsThreadSafe = true)] extern internal static PhysicsDestructor.FragmentResult PhysicsDestructor_FragmentMasked(PhysicsDestructor.FragmentGeometry target, PhysicsDestructor.FragmentGeometry mask, ReadOnlySpan<Vector2> fragmentPoints, Allocator allocator);
        [NativeMethod(Name = "Slice", IsThreadSafe = true)] extern internal static PhysicsDestructor.SliceResult PhysicsDestructor_Slice(PhysicsDestructor.FragmentGeometry target, Vector2 origin, Vector2 translation, Allocator allocator);
    }
}
