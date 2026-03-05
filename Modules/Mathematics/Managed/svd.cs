// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Mathematics
{
    // SVD algorithm as described in:
    // Computing the singular value decomposition of 3x3 matrices with minimal branching and elementary floating point operations,
    // A.McAdams, A.Selle, R.Tamstorf, J.Teran and E.Sifakis, University of Wisconsin - Madison technical report TR1690, May 2011
    /// <summary>
    /// Class with methods for computing the Singular Value Decomposition (SVD) of 3x3 matrices.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    static public class svd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void condSwap(bool c, ref float x, ref float y)
        {
            var tmp = x;
            x = math.select(x, y, c);
            y = math.select(y, tmp, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void condNegSwap(bool c, ref float3 x, ref float3 y)
        {
            var tmp = -x;
            x = math.select(x, y, c);
            y = math.select(y, tmp, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static quaternion condNegSwapQuat(bool c, quaternion q, float4 mask)
        {
            const float halfSqrt2 = 0.707106781186548f;
            return math.mul(q, math.select(quaternion.identity.value, mask * halfSqrt2, c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void sortSingularValues(ref float3x3 b, ref quaternion v)
        {
            var l0 = math.lengthsq(b.c0);
            var l1 = math.lengthsq(b.c1);
            var l2 = math.lengthsq(b.c2);

            var c = l0 < l1;
            condNegSwap(c, ref b.c0, ref b.c1);
            v = condNegSwapQuat(c, v, math.float4(0f, 0f, 1f, 1f));
            condSwap(c, ref l0, ref l1);

            c = l0 < l2;
            condNegSwap(c, ref b.c0, ref b.c2);
            v = condNegSwapQuat(c, v, math.float4(0f, -1f, 0f, 1f));
            condSwap(c, ref l0, ref l2);

            c = l1 < l2;
            condNegSwap(c, ref b.c1, ref b.c2);
            v = condNegSwapQuat(c, v, math.float4(1f, 0f, 0f, 1f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static quaternion approxGivensQuat(float3 pq, float4 mask)
        {
            const float c8 = 0.923879532511287f; // cos(pi/8)
            const float s8 = 0.38268343236509f; // sin(pi/8)
            const float g = 5.82842712474619f; // 3 + 2 * sqrt(2)

            var ch = 2f * (pq.x - pq.y); // approx cos(a/2)
            var sh = pq.z; // approx sin(a/2)
            var r = math.select(math.float4(s8, s8, s8, c8), math.float4(sh, sh, sh, ch), g * sh * sh < ch * ch) * mask;
            return math.normalize(r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static quaternion qrGivensQuat(float2 pq, float4 mask)
        {
            var l = math.sqrt(pq.x * pq.x + pq.y * pq.y);
            var sh = math.select(0f, pq.y, l > k_EpsilonNormalSqrt);
            var ch = math.abs(pq.x) + math.max(l, k_EpsilonNormalSqrt);
            condSwap(pq.x < 0f, ref sh, ref ch);

            return math.normalize(math.float4(sh, sh, sh, ch) * mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static quaternion givensQRFactorization(float3x3 b, out float3x3 r)
        {
            var u = qrGivensQuat(math.float2(b.c0.x, b.c0.y), math.float4(0f, 0f, 1f, 1f));
            var qmt = math.float3x3(math.conjugate(u));
            r = math.mul(qmt, b);

            var q = qrGivensQuat(math.float2(r.c0.x, r.c0.z), math.float4(0f, -1f, 0f, 1f));
            u = math.mul(u, q);
            qmt = math.float3x3(math.conjugate(q));
            r = math.mul(qmt, r);

            q = qrGivensQuat(math.float2(r.c1.y, r.c1.z), math.float4(1f, 0f, 0f, 1f));
            u = math.mul(u, q);
            qmt = math.float3x3(math.conjugate(q));
            r = math.mul(qmt, r);

            return u;
        }

        static quaternion jacobiIteration(ref float3x3 s, int iterations = 5)
        {
            float3x3 qm;
            quaternion q;
            quaternion v = quaternion.identity;

            for (int i = 0; i < iterations; ++i)
            {
                q = approxGivensQuat(math.float3(s.c0.x, s.c1.y, s.c0.y), math.float4(0f, 0f, 1f, 1f));
                v = math.mul(v, q);
                qm = math.float3x3(q);
                s = math.mul(math.mul(math.transpose(qm), s), qm);

                q = approxGivensQuat(math.float3(s.c1.y, s.c2.z, s.c1.z), math.float4(1f, 0f, 0f, 1f));
                v = math.mul(v, q);
                qm = math.float3x3(q);
                s = math.mul(math.mul(math.transpose(qm), s), qm);

                q = approxGivensQuat(math.float3(s.c2.z, s.c0.x, s.c2.x), math.float4(0f, 1f, 0f, 1f));
                v = math.mul(v, q);
                qm = math.float3x3(q);
                s = math.mul(math.mul(math.transpose(qm), s), qm);
            }

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float3 singularValuesDecomposition(float3x3 a, out quaternion u, out quaternion v)
        {
            u = quaternion.identity;
            v = quaternion.identity;

            var s = math.mul(math.transpose(a), a);
            v = jacobiIteration(ref s);
            var b = math.float3x3(v);
            b = math.mul(a, b);
            sortSingularValues(ref b, ref v);
            u = givensQRFactorization(b, out var e);

            return math.float3(e.c0.x, e.c1.y, e.c2.z);
        }
        /// <summary>
        /// Floating point epsilon value used to determine if the determinant is too close to zero.
        /// </summary>
        public const float k_EpsilonDeterminant = 1e-6f;
        /// <summary>
        /// Floating point epsilon value used to avoid division by zero in reciprocal calculations.
        /// </summary>
        public const float k_EpsilonRCP = 1e-9f;
        /// <summary>
        /// Floating point epsilon value used to avoid instability in normal calculations.
        /// </summary>
        public const float k_EpsilonNormalSqrt = 1e-15f;
        /// <summary>
        /// Floating point epsilon value used to avoid instability in normal calculations.
        /// </summary>
        public const float k_EpsilonNormal = 1e-30f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float3 rcpsafe(float3 x, float epsilon = k_EpsilonRCP) =>
            math.select(math.rcp(x), float3.zero, math.abs(x) < epsilon);

        /// <summary>
        /// Calculates the inverse of a 3x3 matrix using Singular Value Decomposition (SVD).
        /// </summary>
        /// <param name="a">The 3x3 matrix to calculate the inverse of.</param>
        /// <returns>The inverse of the 3x3 matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 svdInverse(float3x3 a)
        {
            var e = singularValuesDecomposition(a, out var u, out var v);
            var um = math.float3x3(u);
            var vm = math.float3x3(v);

            return math.mul(vm, math.scaleMul(rcpsafe(e, k_EpsilonDeterminant), math.transpose(um)));
        }
        /// <summary>
        /// Calculates the rotation component of a 3x3 matrix using Singular Value Decomposition (SVD).
        /// </summary>
        /// <param name="a">The 3x3 matrix to calculate the rotation component of.</param>
        /// <returns>The calculated rotation component as a quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion svdRotation(float3x3 a)
        {
            singularValuesDecomposition(a, out var u, out var v);
            return math.mul(u, math.conjugate(v));
        }
    }
}
