// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace Unity.Mathematics
{
    // Mirrors math::trsX in Modules/Mathematics/Include/Mathematics/Simd/vec-trs-type.h. The native float3 and
    // float4 are 16-byte SIMD vectors, so each member starts on a 16-byte boundary.
    [StructLayout(LayoutKind.Explicit)]
    [VisibleToOtherModules("UnityEngine.EntitiesModule")]
    internal struct trsX
    {
        [FieldOffset(0)] [VisibleToOtherModules("UnityEngine.EntitiesModule")] internal float3 t;
        [FieldOffset(16)] [VisibleToOtherModules("UnityEngine.EntitiesModule")] internal float4 q;
        [FieldOffset(32)] [VisibleToOtherModules("UnityEngine.EntitiesModule")] internal float3 s;
        [FieldOffset(44)] internal float padding;
    }

    // Unity.Mathematics can express mul/invMul via AffineTransform (math.transform, math.inverse),
    // but that uses float3x3. These keep the quaternion form native uses, which is cheaper per level
    // and never has to recover a rotation from a matrix with non-uniform scale.
    public static partial class math
    {
        // Mirrors math::mul(trsX const&, float3 const&) in Modules/Mathematics/Include/Mathematics/Simd/vec-trs.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static float3 mul(in trsX x, float3 v)
        {
            return x.t + mul(new quaternion(x.q), v * x.s);
        }

        // Mirrors math::invMul(trsX const&, float3 const&) in Modules/Mathematics/Include/Mathematics/Simd/vec-trs.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static float3 invMul(in trsX x, float3 v)
        {
            return mul(conjugate(new quaternion(x.q)), v - x.t) * inverseScale(x.s);
        }
    }
}
