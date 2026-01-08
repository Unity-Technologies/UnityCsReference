// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
    /// <summary>
    /// An affine transformation type.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    [Serializable]
    public struct AffineTransform : IEquatable<AffineTransform>, IFormattable
    {
        /// <summary>
        /// The rotation and scale part of the affine transformation.
        /// </summary>
        public float3x3 rs;

        /// <summary>
        /// The translation part of the affine transformation.
        /// </summary>
        public float3 t;

        /// <summary>An AffineTransform representing the identity transform.</summary>
        public static readonly AffineTransform identity = new AffineTransform(float3.zero, float3x3.identity);

        /// <summary>
        /// An AffineTransform zero value.
        /// </summary>
        public static readonly AffineTransform zero;

        /// <summary>Constructs an AffineTransform from a translation represented by a float3 vector and rotation represented by a unit quaternion.</summary>
        /// <param name="translation">The translation vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3 translation, quaternion rotation)
        {
            rs = float3x3(rotation);
            t = translation;
        }

        /// <summary>Constructs an AffineTransform from a translation represented by a float3 vector, rotation represented by a unit quaternion and scale represented by a float3 vector.</summary>
        /// <param name="translation">The translation vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        /// <param name="scale">The scale vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3 translation, quaternion rotation, float3 scale)
        {
            rs = mulScale(math.float3x3(rotation), scale);
            t = translation;
        }

        /// <summary>Constructs an AffineTransform from a translation represented by float3 vector and a float3x3 matrix representing both rotation and scale.</summary>
        /// <param name="translation">The translation vector.</param>
        /// <param name="rotationScale">The rotation and scale matrix.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3 translation, float3x3 rotationScale)
        {
            rs = rotationScale;
            t = translation;
        }

        /// <summary>Constructs an AffineTransform from float3x3 matrix representating both rotation and scale.</summary>
        /// <param name="rotationScale">The rotation and scale matrix.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3x3 rotationScale)
        {
            rs = rotationScale;
            t = float3.zero;
        }

        /// <summary>Constructs an AffineTransform from a RigidTransform.</summary>
        /// <param name="rigid">The RigidTransform.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(RigidTransform rigid)
        {
            rs = math.float3x3(rigid.rot);
            t = rigid.pos;
        }

        /// <summary>Constructs an AffineTransform from a float3x4 matrix.</summary>
        /// <param name="m">The float3x4 matrix.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3x4 m)
        {
            rs = math.float3x3(m.c0, m.c1, m.c2);
            t = m.c3;
        }

        /// <summary>Constructs an AffineTransform from a float4x4 matrix.</summary>
        /// <param name="m">The float4x4 matrix.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float4x4 m)
        {
            rs = math.float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            t = m.c3.xyz;
        }

        /// <summary>Implicit float3x4 cast operator.</summary>
        /// <param name="m">The AffineTransform.</param>
        /// <returns>The converted AffineTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float3x4(AffineTransform m) { return float3x4(m.rs.c0, m.rs.c1, m.rs.c2, m.t); }

        /// <summary>Implicit float4x4 cast operator.</summary>
        /// <param name="m">The AffineTransform.</param>
        /// <returns>The converted AffineTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float4x4(AffineTransform m) { return float4x4(float4(m.rs.c0, 0f), float4(m.rs.c1, 0f), float4(m.rs.c2, 0f), float4(m.t, 1f)); }

        /// <summary>Returns true if the AffineTransform is equal to a given AffineTransform, false otherwise.</summary>
        /// <param name="rhs">Right hand side argument to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AffineTransform rhs) { return rs.Equals(rhs.rs) && t.Equals(rhs.t); }

        /// <summary>Returns true if the AffineTransform is equal to a given AffineTransform, false otherwise.</summary>
        /// <param name="o">Right hand side argument to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        public override bool Equals(object o) { return o is AffineTransform converted && Equals(converted); }

        /// <summary>Returns a hash code for the AffineTransform.</summary>
        /// <returns>The computed hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return (int)hash(this); }

        /// <summary>Returns a string representation of the AffineTransform.</summary>
        /// <returns>String representation of the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("AffineTransform(({0}f, {1}f, {2}f,  {3}f, {4}f, {5}f,  {6}f, {7}f, {8}f), ({9}f, {10}f, {11}f))",
                rs.c0.x, rs.c1.x, rs.c2.x, rs.c0.y, rs.c1.y, rs.c2.y, rs.c0.z, rs.c1.z, rs.c2.z, t.x, t.y, t.z
            );
        }

        /// <summary>Returns a string representation of the AffineTransform using a specified format and culture-specific format information.</summary>
        /// <param name="format">Format string to use during string formatting.</param>
        /// <param name="formatProvider">Format provider to use during string formatting.</param>
        /// <returns>String representation of the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("AffineTransform(({0}f, {1}f, {2}f,  {3}f, {4}f, {5}f,  {6}f, {7}f, {8}f), ({9}f, {10}f, {11}f))",
                rs.c0.x.ToString(format, formatProvider), rs.c1.x.ToString(format, formatProvider), rs.c2.x.ToString(format, formatProvider),
                rs.c0.y.ToString(format, formatProvider), rs.c1.y.ToString(format, formatProvider), rs.c2.y.ToString(format, formatProvider),
                rs.c0.z.ToString(format, formatProvider), rs.c1.z.ToString(format, formatProvider), rs.c2.z.ToString(format, formatProvider),
                t.x.ToString(format, formatProvider), t.y.ToString(format, formatProvider), t.z.ToString(format, formatProvider)
            );
        }
    }

    public static partial class math
    {
        /// <summary>Returns an AffineTransform constructed from a translation represented by a float3 vector and rotation represented by a unit quaternion.</summary>
        /// <param name="translation">The AffineTransform translation.</param>
        /// <param name="rotation">The AffineTransform rotation.</param>
        /// <returns>The AffineTransform given the translation vector and rotation quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3 translation, quaternion rotation) { return new AffineTransform(translation, rotation); }

        /// <summary>Returns an AffineTransform constructed from a translation represented by a float3 vector, rotation represented by a unit quaternion and scale represented by a float3 vector.</summary>
        /// <param name="translation">The translation vector.</param>
        /// <param name="rotation">The rotation quaternion.</param>
        /// <param name="scale">The scale vector.</param>
        /// <returns>The AffineTransform given the translation vector, rotation quaternion and scale vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3 translation, quaternion rotation, float3 scale) { return new AffineTransform(translation, rotation, scale); }

        /// <summary>Returns an AffineTransform constructed from a translation represented by float3 vector and a float3x3 matrix representing both rotation and scale.</summary>
        /// <param name="translation">The translation vector.</param>
        /// <param name="rotationScale">The rotation and scale matrix.</param>
        /// <returns>The AffineTransform given the translation vector and float3x3 matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3 translation, float3x3 rotationScale) { return new AffineTransform(translation, rotationScale); }

        /// <summary>Returns an AffineTransform constructed from a float3x3 matrix representing both rotation and scale.</summary>
        /// <param name="rotationScale">The rotation and scale matrix.</param>
        /// <returns>The AffineTransform given a float3x3 matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3x3 rotationScale) { return new AffineTransform(rotationScale); }

        /// <summary>Returns an AffineTransform constructed from a float4x4 matrix.</summary>
        /// <param name="m">The float4x4 matrix.</param>
        /// <returns>The AffineTransform given a float4x4 matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float4x4 m) { return new AffineTransform(m); }

        /// <summary>Returns an AffineTransform constructed from a float3x4 matrix.</summary>
        /// <param name="m">The float3x4 matrix.</param>
        /// <returns>The AffineTransform given a float3x4 matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3x4 m) { return new AffineTransform(m); }

        /// <summary>Returns an AffineTransform constructed from a RigidTransform.</summary>
        /// <param name="rigid">The RigidTransform.</param>
        /// <returns>The AffineTransform given a RigidTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(RigidTransform rigid) { return new AffineTransform (rigid); }

        /// <summary>Returns a float4x4 matrix constructed from an AffineTransform.</summary>
        /// <param name="transform">The AffineTransform.</param>
        /// <returns>The float4x4 matrix given an AffineTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 float4x4(AffineTransform transform) { return float4x4(float4(transform.rs.c0, 0f), float4(transform.rs.c1, 0f), float4(transform.rs.c2, 0f), float4(transform.t, 1f)); }

        /// <summary>Returns a float3x4 matrix constructed from an AffineTransform.</summary>
        /// <param name="transform">The AffineTransform.</param>
        /// <returns>The float3x4 matrix given an AffineTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x4 float3x4(AffineTransform transform) { return float3x4(transform.rs.c0, transform.rs.c1, transform.rs.c2, transform.t); }

        /// <summary>Returns the result of transforming the AffineTransform b by the AffineTransform a.</summary>
        /// <param name="a">The AffineTransform on the left.</param>
        /// <param name="b">The AffineTransform on the right.</param>
        /// <returns>The AffineTransform of a transforming b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform mul(AffineTransform a, AffineTransform b)
        {
            return new AffineTransform(transform(a, b.t), mul(a.rs, b.rs));
        }

        /// <summary>Returns the result of transforming the AffineTransform b by a float3x3 matrix a.</summary>
        /// <param name="a">The float3x3 matrix on the left.</param>
        /// <param name="b">The AffineTransform on the right.</param>
        /// <returns>The AffineTransform of a transforming b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform mul(float3x3 a, AffineTransform b)
        {
            return new AffineTransform(mul(a, b.t), mul(a, b.rs));
        }

        /// <summary>Returns the result of transforming the float3x3 b by an AffineTransform a.</summary>
        /// <param name="a">The AffineTransform on the left.</param>
        /// <param name="b">The float3x3 matrix on the right.</param>
        /// <returns>The AffineTransform of a transforming b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform mul(AffineTransform a, float3x3 b)
        {
            return new AffineTransform(a.t, mul(b, a.rs));
        }

        /// <summary>Returns the result of transforming a float4 homogeneous coordinate by an AffineTransform.</summary>
        /// <param name="a">The AffineTransform.</param>
        /// <param name="pos">The position to be transformed.</param>
        /// <returns>The transformed position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 mul(AffineTransform a, float4 pos)
        {
            return float4(mul(a.rs, pos.xyz) + a.t * pos.w, pos.w);
        }

        /// <summary>Returns the result of rotating a float3 vector by an AffineTransform.</summary>
        /// <param name="a">The AffineTransform.</param>
        /// <param name="dir">The direction vector to rotate.</param>
        /// <returns>The rotated direction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 rotate(AffineTransform a, float3 dir)
        {
            return mul(a.rs, dir);
        }

        /// <summary>Returns the result of transforming a float3 point by an AffineTransform.</summary>
        /// <param name="a">The AffineTransform.</param>
        /// <param name="pos">The position to transform.</param>
        /// <returns>The transformed position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 transform(AffineTransform a, float3 pos)
        {
            return a.t + mul(a.rs, pos);
        }

        /// <summary>Returns the inverse of an AffineTransform.</summary>
        /// <param name="a">The AffineTransform to invert.</param>
        /// <returns>The inverse AffineTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform inverse(AffineTransform a)
        {
            AffineTransform inv;
            inv.rs = pseudoinverse(a.rs);
            inv.t = mul(inv.rs, -a.t);
            return inv;
        }

        /// <summary>Decomposes the AffineTransform in translation, rotation and scale.</summary>
        /// <param name="a">The AffineTransform</param>
        /// <param name="translation">The decomposed translation vector of the AffineTransform.</param>
        /// <param name="rotation">The decomposed rotation quaternion of the AffineTransform.</param>
        /// <param name="scale">The decomposed scale of the AffineTransform.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void decompose(AffineTransform a, out float3 translation, out quaternion rotation, out float3 scale)
        {
            translation = a.t;
            rotation = math.rotation(a.rs);
            var sm = mul(float3x3(conjugate(rotation)), a.rs);
            scale = float3(sm.c0.x, sm.c1.y, sm.c2.z);
        }

        /// <summary>Returns a uint hash code of an AffineTransform.</summary>
        /// <param name="a">The AffineTransform to hash.</param>
        /// <returns>The hash code of the input AffineTransform.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint hash(AffineTransform a)
        {
            return hash(a.rs) + 0xC5C5394Bu * hash(a.t);
        }

        /// <summary>
        /// Returns a uint4 vector hash code of an AffineTransform.
        /// When multiple elements are to be hashes together, it can more efficient to calculate and combine wide hash
        /// that are only reduced to a narrow uint hash at the very end instead of at every step.
        /// </summary>
        /// <param name="a">The AffineTransform to hash.</param>
        /// <returns>The uint4 wide hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 hashwide(AffineTransform a)
        {
            return hashwide(a.rs).xyzz + 0xC5C5394Bu * hashwide(a.t).xyzz;
        }
    }
}
