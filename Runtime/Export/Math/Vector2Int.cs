// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Representation of 2D vectors and points.
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType("Runtime/Math/Vector2Int.h")]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstruction]
    public struct Vector2Int : IEquatable<Vector2Int>, IFormattable
    {
        public int x
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            readonly get => m_X;

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            set => m_X = value;
        }


        public int y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            readonly get => m_Y;

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            set => m_Y = value;
        }

        private int m_X;
        private int m_Y;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector2Int(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        // Set x and y components of an existing Vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        // Access the /x/ or /y/ component using [0] or [1] respectively.
        public int this[int index]
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            readonly get
            {
                switch (index)
                {
                    case 0: return m_X;
                    case 1: return m_Y;
                    default:
                        throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
                }
            }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0: m_X = value; break;
                    case 1: m_Y = value; break;
                    default:
                        throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
                }
            }
        }

        // Returns the length of this vector (RO).
        public readonly float magnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => Mathf.Sqrt((float)(m_X * m_X + m_Y * m_Y));
        }

        // Returns the squared length of this vector (RO).
        public readonly int sqrMagnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => m_X * m_X + m_Y * m_Y;
        }

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(Vector2Int a, Vector2Int b) => Distance(in a, in b);

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(in Vector2Int a, in Vector2Int b)
        {
            float diff_x = a.m_X - b.m_X;
            float diff_y = a.m_Y - b.m_Y;

            return (float)Math.Sqrt(diff_x * diff_x + diff_y * diff_y);
        }

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int Min(Vector2Int lhs, Vector2Int rhs) => Min(in lhs, in rhs);

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int Min(in Vector2Int lhs, in Vector2Int rhs) => new Vector2Int(Mathf.Min(lhs.m_X, rhs.m_X), Mathf.Min(lhs.m_Y, rhs.m_Y));

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int Max(Vector2Int lhs, Vector2Int rhs) => Max(in lhs, in rhs);

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int Max(in Vector2Int lhs, in Vector2Int rhs) => new Vector2Int(Mathf.Max(lhs.m_X, rhs.m_X), Mathf.Max(lhs.m_Y, rhs.m_Y));

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int Scale(Vector2Int a, Vector2Int b) => Scale(in a, in b);

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int Scale(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.m_X * b.m_X, a.m_Y * b.m_Y);

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(Vector2Int scale) => Scale(in scale);

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(in Vector2Int scale) { m_X *= scale.m_X; m_Y *= scale.m_Y; }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Clamp(Vector2Int min, Vector2Int max) => Clamp(in min, in max);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Clamp(in Vector2Int min, in Vector2Int max)
        {
            m_X = Math.Max(min.m_X, m_X);
            m_X = Math.Min(max.m_X, m_X);
            m_Y = Math.Max(min.m_Y, m_Y);
            m_Y = Math.Min(max.m_Y, m_Y);
        }

        // Converts a Vector2Int to a [[Vector2]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector2(in Vector2Int v) => new Vector2(v.m_X, v.m_Y);

        // Converts a Vector2Int to a [[Vector3Int]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static explicit operator Vector3Int(in Vector2Int v) => new Vector3Int(v.m_X, v.m_Y, 0);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int FloorToInt(in Vector2 v) => new Vector2Int(
                Mathf.FloorToInt(v.x),
                Mathf.FloorToInt(v.y)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int CeilToInt(in Vector2 v) => new Vector2Int(
                Mathf.CeilToInt(v.x),
                Mathf.CeilToInt(v.y)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int RoundToInt(in Vector2 v) => new Vector2Int(
                Mathf.RoundToInt(v.x),
                Mathf.RoundToInt(v.y)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator-(in Vector2Int v) => new Vector2Int(-v.m_X, -v.m_Y);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator+(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.m_X + b.m_X, a.m_Y + b.m_Y);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator-(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.m_X - b.m_X, a.m_Y - b.m_Y);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator*(in Vector2Int a, in Vector2Int b) => new Vector2Int(a.m_X * b.m_X, a.m_Y * b.m_Y);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator*(int a, in Vector2Int b) => new Vector2Int(a * b.m_X, a * b.m_Y);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator*(in Vector2Int a, int b) => new Vector2Int(a.m_X * b, a.m_Y * b);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2Int operator/(in Vector2Int a, int b) => new Vector2Int(a.m_X / b, a.m_Y / b);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(in Vector2Int lhs, in Vector2Int rhs) => lhs.m_X == rhs.m_X && lhs.m_Y == rhs.m_Y;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(in Vector2Int lhs, in Vector2Int rhs) => !(lhs == rhs);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Vector2Int v)
                return Equals(v);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Vector2Int other) => m_X == other.m_X && m_Y == other.m_Y;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
           const int p1 = 73856093;
           const int p2 = 83492791;
           return (m_X * p1) ^ (m_Y * p2);
        }

        /// *listonly*
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("({0}, {1})", m_X.ToString(format, formatProvider), m_Y.ToString(format, formatProvider));
        }

        public static Vector2Int zero  { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => s_Zero; }
        public static Vector2Int one   { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => s_One; }
        public static Vector2Int up    { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => s_Up; }
        public static Vector2Int down  { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => s_Down; }
        public static Vector2Int left  { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => s_Left; }
        public static Vector2Int right { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => s_Right; }

        private static readonly Vector2Int s_Zero = new Vector2Int(0, 0);
        private static readonly Vector2Int s_One = new Vector2Int(1, 1);
        private static readonly Vector2Int s_Up = new Vector2Int(0, 1);
        private static readonly Vector2Int s_Down = new Vector2Int(0, -1);
        private static readonly Vector2Int s_Left = new Vector2Int(-1, 0);
        private static readonly Vector2Int s_Right = new Vector2Int(1, 0);
    }
}
