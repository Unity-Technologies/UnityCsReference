// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace Unity.Mathematics
{
    public static partial class math
    {
        // Mirrors math::quatConj(const float4&) in Modules/Mathematics/Include/Mathematics/Simd/vec-quat.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static float4 quatConj(float4 q)
        {
            return conjugate(new quaternion(q)).value;
        }

        // Mirrors math::quatMul(const float4&, const float4&) in
        // Modules/Mathematics/Include/Mathematics/Simd/vec-quat.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static float4 quatMul(float4 a, float4 b)
        {
            return mul(new quaternion(a), new quaternion(b)).value;
        }

        // Mirrors math::scaleMulQuat(const float3&, const float4&) in
        // Modules/Mathematics/Include/Mathematics/Simd/vec-quat.h. Flips quaternion component signs to account for
        // negative scale. No Unity.Mathematics equivalent.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static float4 scaleMulQuat(float3 scale, float4 q)
        {
            float3 s = chgsign(new float3(1.0f), scale);
            return chgsign(q, new float4(s.yxx * s.zzy, 0.0f));
        }
    }
}
