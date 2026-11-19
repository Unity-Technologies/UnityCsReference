// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animations
{
    // SOA 4-wide quaternion
    /// <summary>Four quaternions stored as a structure of arrays, to process four rotations at the same time.</summary>
    /// <remarks>
    /// Use a quaternion4 to apply the same operation to four rotations at once, for better performance than
    /// processing the rotations one at a time. For example, you can blend the rotation channels of a rig four at a
    /// time. The rotation channels of an <see cref="AnimationStream"/> are stored as quaternion4 chunks.
    ///
    /// Each of the fields <see cref="x"/>, <see cref="y"/>, <see cref="z"/>, and <see cref="w"/> is a `float4` that
    /// holds one component of all four quaternions. Lane 0 of every field belongs to the first quaternion, lane 1 to
    /// the second quaternion, and so on. This layout is called structure of arrays and lets the central processing
    /// unit (CPU) process the four lanes together with single instruction, multiple data (SIMD) instructions.
    ///
    /// The operators of this struct and the methods in <see cref="Quaternion4Extensions"/> apply the operation to each
    /// lane separately.
    ///
    /// The following example uses the `Unity.Animations` and `Unity.Mathematics` namespaces. It combines four parent
    /// rotations with four local rotations in one operation, then reads each result back.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/Quaternion4Example.cs#Quaternion4Example}]]></code>
    /// </example>
    /// <seealso cref="Quaternion4Extensions"/>
    [Serializable]
    [GenerateTestsForBurstCompatibility]
    public partial struct quaternion4 : IEquatable<quaternion4>, IFormattable
    {
        /// <summary>The x components of the four quaternions, one in each lane.</summary>
        public float4 x;
        /// <summary>The y components of the four quaternions, one in each lane.</summary>
        public float4 y;
        /// <summary>The z components of the four quaternions, one in each lane.</summary>
        public float4 z;
        /// <summary>The w components of the four quaternions, one in each lane.</summary>
        public float4 w;

        /// <summary>A quaternion4 where all four quaternions are the identity rotation.</summary>
        public static readonly quaternion4 identity = new quaternion4(float4.zero, float4.zero, float4.zero, math.float4(1f));

        /// <summary>Creates a quaternion4 from four vectors that hold the components of the four quaternions.</summary>
        /// <remarks>
        /// Lane 0 of each parameter holds a component of the first quaternion, lane 1 holds a component of the second
        /// quaternion, and so on. The constructor doesn't normalize the values. To build a quaternion4 from four
        /// separate quaternions, use the constructor that takes four `quaternion` values.
        /// </remarks>
        /// <param name="x">The x components of the four quaternions, one in each lane.</param>
        /// <param name="y">The y components of the four quaternions, one in each lane.</param>
        /// <param name="z">The z components of the four quaternions, one in each lane.</param>
        /// <param name="w">The w components of the four quaternions, one in each lane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion4(float4 x, float4 y, float4 z, float4 w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        /// <summary>Creates a quaternion4 from four quaternions.</summary>
        /// <remarks>
        /// The constructor stores each quaternion in its own lane: the components of <paramref name="q0"/> go to lane 0
        /// of x, y, z, and w, the components of <paramref name="q1"/> go to lane 1, and so on. The constructor doesn't
        /// normalize the quaternions.
        /// </remarks>
        /// <param name="q0">The quaternion to store in lane 0 of the new quaternion4.</param>
        /// <param name="q1">The quaternion to store in lane 1 of the new quaternion4.</param>
        /// <param name="q2">The quaternion to store in lane 2 of the new quaternion4.</param>
        /// <param name="q3">The quaternion to store in lane 3 of the new quaternion4.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion4(quaternion q0, quaternion q1, quaternion q2, quaternion q3)
        {
            var m = math.transpose(math.float4x4(q0.value, q1.value, q2.value, q3.value));
            x = m.c0; y = m.c1; z = m.c2; w = m.c3;
        }

        /// <summary>Creates a quaternion4 from the four columns of a matrix.</summary>
        /// <remarks>
        /// The columns of the matrix become the fields <see cref="x"/>, <see cref="y"/>, <see cref="z"/>, and
        /// <see cref="w"/>. This constructor doesn't convert a rotation matrix into quaternions.
        /// </remarks>
        /// <param name="m">The matrix whose columns hold the x, y, z, and w components of the four quaternions.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion4(float4x4 m)
        {
            x = m.c0; y = m.c1; z = m.c2; w = m.c3;
        }

        /// <summary>Adds the components of two quaternion4 values.</summary>
        /// <remarks>
        /// The result isn't normalized, and the operator doesn't flip the quaternions into the same hemisphere. To flip
        /// <paramref name="rhs"/> into the hemisphere of <paramref name="lhs"/> before the addition, use
        /// <see cref="Quaternion4Extensions.Add"/>.
        /// </remarks>
        /// <param name="lhs">The first <see cref="quaternion4"/> to add.</param>
        /// <param name="rhs">The second <see cref="quaternion4"/> to add.</param>
        /// <returns>The <see cref="quaternion4"/> whose components are the sums of the components of
        /// <paramref name="lhs"/> and <paramref name="rhs"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 operator+(quaternion4 lhs, quaternion4 rhs) { return new quaternion4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w); }

        /// <summary>Multiplies every component of a quaternion4 by a scalar.</summary>
        /// <remarks>
        /// This operator scales the components. It isn't the quaternion product. To combine rotations, use
        /// <see cref="Quaternion4Extensions.Mul"/>.
        /// </remarks>
        /// <param name="lhs">The <see cref="quaternion4"/> whose components the operator scales.</param>
        /// <param name="rhs">The scalar to multiply every component by.</param>
        /// <returns>The resulting <see cref="quaternion4"/> after the multiplication.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 operator*(quaternion4 lhs, float rhs) { return new quaternion4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }

        /// <summary>Multiplies the components of each quaternion in a quaternion4 by a separate scalar.</summary>
        /// <remarks>
        /// Lane 0 of <paramref name="rhs"/> scales the four components of the first quaternion, lane 1 scales the
        /// second quaternion, and so on. This operator scales the components. It isn't the quaternion product. To
        /// combine rotations, use <see cref="Quaternion4Extensions.Mul"/>.
        /// </remarks>
        /// <param name="lhs">The <see cref="quaternion4"/> whose components the operator scales.</param>
        /// <param name="rhs">The scalars to multiply the quaternions by, one in each lane.</param>
        /// <returns>The resulting <see cref="quaternion4"/> after the multiplication.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 operator*(quaternion4 lhs, float4 rhs) { return new quaternion4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }

        /// <summary>Returns true if the components of this quaternion4 equal those of another quaternion4.</summary>
        /// <remarks>
        /// The comparison is exact. It doesn't use a tolerance, and it doesn't treat <paramref name="q"/> and its
        /// negation as the same rotation.
        /// </remarks>
        /// <param name="q">The <see cref="quaternion4"/> to compare with this one.</param>
        /// <returns>`true` if all 16 components are equal. Otherwise, `false`.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(quaternion4 q) { return x.Equals(q.x) && y.Equals(q.y) && z.Equals(q.z) && w.Equals(q.w); }

        [ExcludeFromBurstCompatTesting("Takes managed object")]
        public override bool Equals(object o) { return Equals((quaternion4)o); }

        /// <summary>Returns a hash code for the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return (int)this.Hash(); }

        /// <summary>Returns a string representation of the quaternion4.</summary>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("quaternion4({0}f, {1}f, {2}f, {3}f,  {4}f, {5}f, {6}f, {7}f,  {8}f, {9}f, {10}f, {11}f,  {12}f, {13}f, {14}f, {15}f)", x.x, y.x, z.x, w.x, x.y, y.y, z.y, w.y, x.z, y.z, z.z, w.z, x.w, y.w, z.w, w.w);
        }

        /// <summary>Returns a string representation of the quaternion4, using a format for each component.</summary>
        /// <param name="format">The numeric format string to apply to each component.</param>
        /// <param name="formatProvider">The object that provides culture-specific formatting information.</param>
        /// <returns>A string that lists the x, y, z, and w components of lane 0, then those of lane 1, lane 2, and
        /// lane 3.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("quaternion4({0}f, {1}f, {2}f, {3}f,  {4}f, {5}f, {6}f, {7}f,  {8}f, {9}f, {10}f, {11}f,  {12}f, {13}f, {14}f, {15}f)", x.x.ToString(format, formatProvider), y.x.ToString(format, formatProvider), z.x.ToString(format, formatProvider), w.x.ToString(format, formatProvider), x.y.ToString(format, formatProvider), y.y.ToString(format, formatProvider), z.y.ToString(format, formatProvider), w.y.ToString(format, formatProvider), x.z.ToString(format, formatProvider), y.z.ToString(format, formatProvider), z.z.ToString(format, formatProvider), w.z.ToString(format, formatProvider), x.w.ToString(format, formatProvider), y.w.ToString(format, formatProvider), z.w.ToString(format, formatProvider), w.w.ToString(format, formatProvider));
        }
    }

    /// <summary>Extension methods for the quaternion4 type. Every operation is applied per lane.</summary>
    [GenerateTestsForBurstCompatibility]
    public static class Quaternion4Extensions
    {
        /// <summary>Returns a float4x4 whose columns are the x, y, z and w planes of the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 ToFloat4x4(this quaternion4 q) { return new float4x4(q.x, q.y, q.z, q.w); }

        /// <summary>Returns b for the lanes where c is true, a otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 Select(this quaternion4 a, quaternion4 b, bool4 c)
        {
            return new quaternion4(math.select(a.x, b.x, c), math.select(a.y, b.y, c), math.select(a.z, b.z, c), math.select(a.w, b.w, c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static quaternion4 ChangeSign(quaternion4 q, float4 sign)
        {
            uint4 signBits = math.asuint(sign) & 0x80000000;
            return new quaternion4(
                math.asfloat(math.asuint(q.x) ^ signBits),
                math.asfloat(math.asuint(q.y) ^ signBits),
                math.asfloat(math.asuint(q.z) ^ signBits),
                math.asfloat(math.asuint(q.w) ^ signBits)
            );
        }

        /// <summary>Returns the dot product of two quaternion4 values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Dot(this quaternion4 q1, quaternion4 q2) =>
            q1.x * q2.x + q1.y * q2.y + q1.z * q2.z + q1.w * q2.w;

        /// <summary>Returns the normalized quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 Normalize(this quaternion4 q) =>
            q * math.rsqrt(q.Dot(q));

        /// <summary>Returns the normalized quaternion4, or the identity for the lanes whose squared length isn't greater than <see cref="math.FLT_MIN_NORMAL"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 NormalizeSafe(this quaternion4 q)
        {
            float4 lensq = q.Dot(q);
            return quaternion4.identity.Select(q * math.rsqrt(lensq), lensq > math.FLT_MIN_NORMAL);
        }

        /// <summary>Returns the normalized linear interpolation from q1 to q2 along the shortest path.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 Nlerp(this quaternion4 q1, quaternion4 q2, float4 blend)
        {
            q2 = ChangeSign(q2, q1.Dot(q2));
            return new quaternion4(
                math.lerp(q1.x, q2.x, blend),
                math.lerp(q1.y, q2.y, blend),
                math.lerp(q1.z, q2.z, blend),
                math.lerp(q1.w, q2.w, blend)
            ).Normalize();
        }

        /// <summary>Returns q1 + q2 with q2 flipped into the hemisphere of q1. The result isn't normalized, so it can be used to accumulate weighted rotations.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 Add(this quaternion4 q1, quaternion4 q2) =>
            q1 + ChangeSign(q2, q1.Dot(q2));

        /// <summary>Returns the rotation scaled by weight and normalized, for additive blending.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 QuatWeight(this quaternion4 q, float4 weight)
        {
            q.x *= weight; q.y *= weight; q.z *= weight;
            return q.NormalizeSafe();
        }

        /// <summary>Returns the quaternion product of lhs and rhs.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 Mul(this quaternion4 lhs, quaternion4 rhs)
        {
            // Same per-component association as math.mul(quaternion, quaternion), evaluated 4 lanes at a time.
            return new quaternion4(
                lhs.w * rhs.x + (lhs.x * rhs.w + lhs.y * rhs.z) - lhs.z * rhs.y,
                lhs.w * rhs.y + (lhs.y * rhs.w + lhs.z * rhs.x) - lhs.x * rhs.z,
                lhs.w * rhs.z + (lhs.z * rhs.w + lhs.x * rhs.y) - lhs.y * rhs.x,
                lhs.w * rhs.w - (lhs.x * rhs.x + lhs.y * rhs.y) - lhs.z * rhs.z
            );
        }

        /// <summary>Returns the conjugate of the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 Conjugate(this quaternion4 q)
        {
            return new quaternion4(q.x * -1f, q.y * -1f, q.z * -1f, q.w);
        }

        /// <summary>Returns a hash code for the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Hash(this quaternion4 q)
        {
            return math.csum(
                math.asuint(q.x) * math.uint4(0xC4B1493Fu, 0xBA0966D3u, 0xAFBEE253u, 0x5B419C01u) +
                math.asuint(q.y) * math.uint4(0x515D90F5u, 0xEC9F68F3u, 0xF9EA92D5u, 0xC2FAFCB9u) +
                math.asuint(q.z) * math.uint4(0x616E9CA1u, 0xC5C5394Bu, 0xCAE78587u, 0x7A1541C9u) +
                math.asuint(q.w) * math.uint4(0xF83BD927u, 0x6A243BCBu, 0x509B84C9u, 0x91D13847u)) + 0x52F7230Fu;
        }

        /// <summary>Returns a per-lane hash code for the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 HashWide(this quaternion4 q)
        {
            return (math.asuint(q.x) * math.uint4(0xCF286E83u, 0xE121E6ADu, 0xC9CA1249u, 0x69B60C81u) +
                math.asuint(q.y) * math.uint4(0xE0EB6C25u, 0xF648BEABu, 0x6BDB2B07u, 0xEF63C699u) +
                math.asuint(q.z) * math.uint4(0x9001903Fu, 0xA895B9CDu, 0x9D23B201u, 0x4B01D3E1u) +
                math.asuint(q.w) * math.uint4(0x7461CA0Du, 0x79725379u, 0xD6258E5Bu, 0xEE390C97u)) + 0x9C8A2F05u;
        }
    }
}
