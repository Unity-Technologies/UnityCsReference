// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor
{
    // Representation of 3D vectors and points in double precision, for computations that must not accumulate float rounding errors.
    struct Vector3Double : IEquatable<Vector3Double>, IFormattable
    {
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

        private double m_X;
        private double m_Y;
        private double m_Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double(double x, double y, double z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        // Access the /x/, /y/ or /z/ component using [0], [1] or [2] respectively.
        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                switch (index)
                {
                    case 0: return m_X;
                    case 1: return m_Y;
                    case 2: return m_Z;
                    default:
                        throw new IndexOutOfRangeException(String.Format("Invalid Vector3Double index addressed: {0}!", index));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0: m_X = value; break;
                    case 1: m_Y = value; break;
                    case 2: m_Z = value; break;
                    default:
                        throw new IndexOutOfRangeException(String.Format("Invalid Vector3Double index addressed: {0}!", index));
                }
            }
        }

        // Returns the largest absolute value of the components of this vector (RO).
        public readonly double maxAbsComponent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Max(Math.Abs(m_X), Math.Max(Math.Abs(m_Y), Math.Abs(m_Z)));
        }

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double Scale(Vector3Double a, Vector3Double b) => new Vector3Double(a.m_X * b.m_X, a.m_Y * b.m_Y, a.m_Z * b.m_Z);

        // Converts a Vector3 to a Vector3Double, without loss of precision.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3Double(Vector3 v) => new Vector3Double(v.x, v.y, v.z);

        // Converts a Vector3Double to a Vector3, rounding each component to the closest float.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3(Vector3Double v) => new Vector3((float)v.m_X, (float)v.m_Y, (float)v.m_Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator-(Vector3Double v) => new Vector3Double(-v.m_X, -v.m_Y, -v.m_Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator+(Vector3Double a, Vector3Double b) => new Vector3Double(a.m_X + b.m_X, a.m_Y + b.m_Y, a.m_Z + b.m_Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator-(Vector3Double a, Vector3Double b) => new Vector3Double(a.m_X - b.m_X, a.m_Y - b.m_Y, a.m_Z - b.m_Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator*(double a, Vector3Double b) => new Vector3Double(a * b.m_X, a * b.m_Y, a * b.m_Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator*(Vector3Double a, double b) => new Vector3Double(a.m_X * b, a.m_Y * b, a.m_Z * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Double operator/(Vector3Double a, double b) => new Vector3Double(a.m_X / b, a.m_Y / b, a.m_Z / b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(Vector3Double lhs, Vector3Double rhs) => lhs.m_X == rhs.m_X && lhs.m_Y == rhs.m_Y && lhs.m_Z == rhs.m_Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(Vector3Double lhs, Vector3Double rhs) => !(lhs == rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Vector3Double v)
                return Equals(v);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3Double other) => m_X == other.m_X && m_Y == other.m_Y && m_Z == other.m_Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode() => HashCode.Combine(m_X, m_Y, m_Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("({0}, {1}, {2})", m_X.ToString(format, formatProvider), m_Y.ToString(format, formatProvider), m_Z.ToString(format, formatProvider));
        }

        public static Vector3Double zero { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => k_Zero; }

        private static readonly Vector3Double k_Zero = new Vector3Double(0, 0, 0);
    }
}
