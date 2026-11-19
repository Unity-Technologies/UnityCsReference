// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>
    /// Container of individual translation, rotation and scale. It's useful to represent local space transformation data.
    /// <remark>
    /// This struct should be kept INTERNAL and never exposed to our users as it has caveats and is misleading. You SHOULD NOT use it
    /// to compose two TRS together given that it cannot properly represent shear in a hierarchy. For these types of operations,
    /// please use an <see cref="AffineTransform">AffineTransform</see>. That is why, mul(TRS x, TRS y) is intentionally returning an
    /// AffineTransform.
    /// </remark>
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    internal struct TRS
    {
        public quaternion r;
        public float3 t;
        public float3 s;

        internal static readonly TRS identity = new TRS(float3.zero, quaternion.identity, 1f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TRS(float3 t, quaternion r, float3 s)
        {
            this.t = t; this.r = r; this.s = s;
        }

        [ExcludeFromBurstCompatTesting("Returns managed string")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("T[{0}] R[{1}] S[{2}]", t, r, s);
        }
    }

    [GenerateTestsForBurstCompatibility]
    internal static partial class MathUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform AffineTransform(TRS x) =>
            math.AffineTransform(x.t, x.r, x.s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform mul(TRS x, TRS y) =>
            math.mul(AffineTransform(x), AffineTransform(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform mul(AffineTransform x, TRS y) =>
            math.mul(x, AffineTransform(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform mul(TRS x, AffineTransform y) =>
            math.mul(AffineTransform(x), y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 transform(TRS x, float3 pos) =>
            x.t + math.mul(x.r, pos * x.s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 inverseTransform(TRS x, float3 pos) =>
            math.mul(math.conjugate(x.r), pos - x.t) * rcpsafe(x.s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 rotate(TRS x, float3 dir) =>
            math.mul(x.r, dir * x.s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform inverse(TRS x)
        {
            AffineTransform a;
            a.rs = math.float3x3(math.conjugate(x.r));
            a.rs = math.scaleMul(rcpsafe(x.s), a.rs);
            a.t = math.mul(a.rs, -x.t);
            return a;
        }
    }
}
