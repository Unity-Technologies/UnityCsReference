// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace Unity.Mathematics
{
    // Mirrors math::affineX in Modules/Mathematics/Include/Mathematics/Simd/vec-affine-type.h.
    [VisibleToOtherModules("UnityEngine.EntitiesModule")]
    internal struct affineX
    {
        internal float3x3 rs;
        internal float3 t;

        internal affineX(float3 t, float3x3 rs)
        {
            this.rs = rs;
            this.t = t;
        }

        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        public static implicit operator float4x4(affineX a)
        {
            return new float4x4(
                new float4(a.rs.c0, 0f),
                new float4(a.rs.c1, 0f),
                new float4(a.rs.c2, 0f),
                new float4(a.t, 1.0f));
        }
    }

    public static partial class math
    {
        // Mirrors math::convert(const trsX&, affineX&) in Modules/Mathematics/Include/Mathematics/Simd/vec-transform.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static affineX convert(trsX trs)
        {
            float3x3 rs = new float3x3(trs.q);
            rs = mulScale(rs, trs.s);
            return new affineX(trs.t, rs);
        }

        // Mirrors math::inverse(const trsX&, affineX&) in Modules/Mathematics/Include/Mathematics/Simd/vec-transform.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static affineX inverse(trsX trs)
        {
            float3x3 rs = new float3x3(conjugate(new quaternion(trs.q)));
            rs = scaleMul(inverseScale(trs.s), rs);
            return new affineX(mul(rs, -trs.t), rs);
        }

        // Mirrors math::inverseScale(float3 const&, float) in
        // Modules/Mathematics/Include/Mathematics/Simd/vec-matrix.h, with epsilon_scale() == 1e-9f from
        // vec-const.h. Components too small to invert become 0 rather than infinity. Note native's rcp is only
        // bit-exact off ARM: on NEON it is vrecpeq plus two Newton-Raphson steps (vec-math.h:1180), so exact parity
        // with native was never platform-independent here anyway.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 inverseScale(float3 s)
        {
            const float epsilonScale = 1e-9f;
            return select(rcp(s), new float3(0.0f), abs(s) < epsilonScale);
        }

        // Mirrors math::mul(const affineX&, const float3&) in
        // Modules/Mathematics/Include/Mathematics/Simd/vec-affine.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 mul(affineX a, float3 v)
        {
            return a.t + mul(a.rs, v);
        }

        // Mirrors math::mul(const affineX&, const affineX&) in
        // Modules/Mathematics/Include/Mathematics/Simd/vec-affine.h.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [VisibleToOtherModules("UnityEngine.EntitiesModule")]
        internal static affineX mul(affineX a, affineX b)
        {
            return new affineX(mul(a, b.t), mul(a.rs, b.rs));
        }
    }
}
