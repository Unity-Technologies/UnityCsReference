// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor
{
    // Representation of rotations in double precision, for computations that must not accumulate float rounding errors.
    // Euler angles are in degrees, in the same Z, X, Y rotation order as Quaternion.
    struct QuaternionDouble : IEquatable<QuaternionDouble>, IFormattable
    {
        // Below this cosine of the X angle, the X angle is considered to be ±90 degrees, where Y and Z rotate around the same axis.
        const double k_GimbalLockThreshold = 1e-9;

        public double x
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => m_X;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_X = value;
        }
        public double y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => m_Y;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_Y = value;
        }
        public double z
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => m_Z;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_Z = value;
        }
        public double w
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] readonly get => m_W;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_W = value;
        }

        private double m_X;
        private double m_Y;
        private double m_Z;
        private double m_W;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QuaternionDouble(double x, double y, double z, double w)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_W = w;
        }

        // The identity rotation (RO).
        public static QuaternionDouble identity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => k_Identity; }

        private static readonly QuaternionDouble k_Identity = new QuaternionDouble(0, 0, 0, 1);

        // Returns this rotation with a magnitude of 1, or the identity rotation if its magnitude is 0 (RO).
        public readonly QuaternionDouble normalized
        {
            get
            {
                double magnitude = Math.Sqrt(Dot(this, this));
                return magnitude > 0 ? new QuaternionDouble(m_X / magnitude, m_Y / magnitude, m_Z / magnitude, m_W / magnitude) : identity;
            }
        }

        // Returns the inverse of /rotation/, which must be normalized.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Inverse(QuaternionDouble rotation) => new QuaternionDouble(-rotation.m_X, -rotation.m_Y, -rotation.m_Z, rotation.m_W);

        // The dot product between two rotations.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(QuaternionDouble a, QuaternionDouble b) => a.m_X * b.m_X + a.m_Y * b.m_Y + a.m_Z * b.m_Z + a.m_W * b.m_W;

        // Returns the angle in degrees between two normalized rotations.
        public static double Angle(QuaternionDouble a, QuaternionDouble b)
        {
            double dot = Math.Min(Math.Abs(Dot(a, b)), 1.0);
            return Math.Acos(dot) * 2.0 * (180.0 / Math.PI);
        }

        // Returns a rotation that rotates /z/ degrees around the z axis, /x/ degrees around the x axis, and /y/ degrees around the y axis, in that order.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble Euler(Vector3Double euler) => AngleAxis(euler.y, 1) * AngleAxis(euler.x, 0) * AngleAxis(euler.z, 2);

        static QuaternionDouble AngleAxis(double degrees, int axis)
        {
            double halfAngle = degrees * (Math.PI / 360.0);
            double sin = Math.Sin(halfAngle);
            return new QuaternionDouble(axis == 0 ? sin : 0, axis == 1 ? sin : 0, axis == 2 ? sin : 0, Math.Cos(halfAngle));
        }

        // Returns the euler angles representing this rotation that are the closest to /reference/.
        // At ±90 degrees around the x axis, the y and z axes are aligned, so the z angle of /reference/ is kept.
        public readonly Vector3Double GetClosestEulerAngles(Vector3Double reference)
        {
            QuaternionDouble q = normalized;

            // Rotation matrix elements, for a matrix equal to RotationY * RotationX * RotationZ.
            double m00 = 1 - 2 * (q.m_Y * q.m_Y + q.m_Z * q.m_Z);
            double m01 = 2 * (q.m_X * q.m_Y - q.m_W * q.m_Z);
            double m02 = 2 * (q.m_X * q.m_Z + q.m_W * q.m_Y);
            double m10 = 2 * (q.m_X * q.m_Y + q.m_W * q.m_Z);
            double m11 = 1 - 2 * (q.m_X * q.m_X + q.m_Z * q.m_Z);
            double m12 = 2 * (q.m_Y * q.m_Z - q.m_W * q.m_X);
            double m22 = 1 - 2 * (q.m_X * q.m_X + q.m_Y * q.m_Y);

            // Using atan2 rather than asin for the x angle keeps its precision near ±90 degrees.
            double cosX = Math.Sqrt(m10 * m10 + m11 * m11);
            double x = Degrees(Math.Atan2(-m12, cosX));
            double y, z;
            if (cosX < k_GimbalLockThreshold)
            {
                // Only the sum or the difference of the y and z angles is defined.
                z = reference.z;
                y = m12 < 0 ? z + Degrees(Math.Atan2(m01, m00)) : Degrees(Math.Atan2(-m01, m00)) - z;
            }
            else
            {
                y = Degrees(Math.Atan2(m02, m22));
                z = Degrees(Math.Atan2(m10, m11));
            }

            // (x, y, z) and (180 - x, y + 180, z + 180) are the two representations of the rotation, up to multiples of 360 degrees.
            Vector3Double first = ClosestTo(reference, x, y, z, out double firstDistance);
            Vector3Double second = ClosestTo(reference, 180 - x, y + 180, z + 180, out double secondDistance);
            return firstDistance <= secondDistance ? first : second;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double Degrees(double radians) => radians * (180.0 / Math.PI);

        // Offsets each angle by the multiple of 360 degrees that brings it closest to the reference.
        static Vector3Double ClosestTo(Vector3Double reference, double x, double y, double z, out double distance)
        {
            var angles = new Vector3Double(WrapNear(x, reference.x), WrapNear(y, reference.y), WrapNear(z, reference.z));
            distance = Math.Abs(angles.x - reference.x) + Math.Abs(angles.y - reference.y) + Math.Abs(angles.z - reference.z);
            return angles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double WrapNear(double angle, double reference) => angle + 360.0 * Math.Round((reference - angle) / 360.0);

        // Converts a Quaternion to a QuaternionDouble, without loss of precision.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator QuaternionDouble(Quaternion q) => new QuaternionDouble(q.x, q.y, q.z, q.w);

        // Converts a QuaternionDouble to a Quaternion, rounding each component to the closest float.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Quaternion(QuaternionDouble q) => new Quaternion((float)q.m_X, (float)q.m_Y, (float)q.m_Z, (float)q.m_W);

        // Combines rotations /lhs/ and /rhs/.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuaternionDouble operator*(QuaternionDouble lhs, QuaternionDouble rhs) => new QuaternionDouble(
            lhs.m_W * rhs.m_X + lhs.m_X * rhs.m_W + lhs.m_Y * rhs.m_Z - lhs.m_Z * rhs.m_Y,
            lhs.m_W * rhs.m_Y + lhs.m_Y * rhs.m_W + lhs.m_Z * rhs.m_X - lhs.m_X * rhs.m_Z,
            lhs.m_W * rhs.m_Z + lhs.m_Z * rhs.m_W + lhs.m_X * rhs.m_Y - lhs.m_Y * rhs.m_X,
            lhs.m_W * rhs.m_W - lhs.m_X * rhs.m_X - lhs.m_Y * rhs.m_Y - lhs.m_Z * rhs.m_Z);

        // Rotates the point /point/ with /rotation/, which must be normalized: p + 2w(u x p) + 2u x (u x p), with u the vector part of the rotation.
        public static Vector3Double operator*(QuaternionDouble rotation, Vector3Double point)
        {
            double tx = 2 * (rotation.m_Y * point.z - rotation.m_Z * point.y);
            double ty = 2 * (rotation.m_Z * point.x - rotation.m_X * point.z);
            double tz = 2 * (rotation.m_X * point.y - rotation.m_Y * point.x);
            return new Vector3Double(
                point.x + rotation.m_W * tx + rotation.m_Y * tz - rotation.m_Z * ty,
                point.y + rotation.m_W * ty + rotation.m_Z * tx - rotation.m_X * tz,
                point.z + rotation.m_W * tz + rotation.m_X * ty - rotation.m_Y * tx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(QuaternionDouble lhs, QuaternionDouble rhs) => lhs.Equals(rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(QuaternionDouble lhs, QuaternionDouble rhs) => !lhs.Equals(rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is QuaternionDouble q)
                return Equals(q);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(QuaternionDouble other) => m_X == other.m_X && m_Y == other.m_Y && m_Z == other.m_Z && m_W == other.m_W;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode() => HashCode.Combine(m_X, m_Y, m_Z, m_W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("({0}, {1}, {2}, {3})", m_X.ToString(format, formatProvider), m_Y.ToString(format, formatProvider), m_Z.ToString(format, formatProvider), m_W.ToString(format, formatProvider));
        }
    }
}
