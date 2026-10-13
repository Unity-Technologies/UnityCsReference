// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        [NativeMethod(Name = "PhysicsMath::PI", IsThreadSafe = true)] extern internal static float PhysicsMath_PI();
        [NativeMethod(Name = "PhysicsMath::TAU", IsThreadSafe = true)] extern internal static float PhysicsMath_TAU();
        [NativeMethod(Name = "PhysicsMath::ToDegrees", IsThreadSafe = true)] extern internal static float PhysicsMath_ToDegrees(float radians);
        [NativeMethod(Name = "PhysicsMath::ToRadians", IsThreadSafe = true)] extern internal static float PhysicsMath_ToRadians(float degrees);
        [NativeMethod(Name = "PhysicsMath::Atan2", IsThreadSafe = true)] extern internal static float PhysicsMath_Atan2(float y, float x);
        [NativeMethod(Name = "PhysicsMath::CosSin", IsThreadSafe = true)] extern internal static void PhysicsMath_CosSin(float angle, out float cos, out float sin);
        [NativeMethod(Name = "PhysicsMath::SpringDamper", IsThreadSafe = true)] extern internal static float PhysicsMath_SpringDamper(float frequency, float damping, float translation, float speed, float deltaTime);
    }
}
