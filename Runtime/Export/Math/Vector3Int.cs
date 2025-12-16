// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstruction]
    [Serializable]
    public struct Vector3Int : IEquatable<Vector3Int>, IFormattable
    {
        public int x
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_X;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_X = value;
        }
        public int y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Y;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Y = value;
        }
        public int z
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Z;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Z = value;
        }

        private int m_X;
        private int m_Y;
        private int m_Z;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector3Int(int x, int y)
        {
            m_X = x;
            m_Y = y;
            m_Z = 0;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector3Int(int x, int y, int z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        // Set x, y and z components of an existing Vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set(int x, int y, int z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        // Access the /x/, /y/ or /z/ component using [0], [1] or [2] respectively.
        public int this[int index]
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            readonly get
            {
                switch (index)
                {
                    case 0: return m_X;
                    case 1: return m_Y;
                    case 2: return m_Z;
                    default:
                        throw new IndexOutOfRangeException(string.Format("Invalid Vector3Int index addressed: {0}!", index));
                }
            }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0: m_X = value; break;
                    case 1: m_Y = value; break;
                    case 2: m_Z = value; break;
                    default:
                        throw new IndexOutOfRangeException(string.Format("Invalid Vector3Int index addressed: {0}!", index));
                }
            }
        }

        // Returns the length of this vector (RO).
        public readonly float magnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => Mathf.Sqrt((float)(m_X * m_X + m_Y * m_Y + m_Z * m_Z));
        }

        // Returns the squared length of this vector (RO).
        public readonly int sqrMagnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => m_X * m_X + m_Y * m_Y + m_Z * m_Z;
        }

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(Vector3Int a, Vector3Int b) => (a - b).magnitude;

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(in Vector3Int a, in Vector3Int b) => (a - b).magnitude;

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int Min(Vector3Int lhs, Vector3Int rhs) => new Vector3Int(Mathf.Min(lhs.m_X, rhs.m_X), Mathf.Min(lhs.m_Y, rhs.m_Y), Mathf.Min(lhs.m_Z, rhs.m_Z));

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int Min(in Vector3Int lhs, in Vector3Int rhs) => new Vector3Int(Mathf.Min(lhs.m_X, rhs.m_X), Mathf.Min(lhs.m_Y, rhs.m_Y), Mathf.Min(lhs.m_Z, rhs.m_Z));

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int Max(Vector3Int lhs, Vector3Int rhs) => new Vector3Int(Mathf.Max(lhs.m_X, rhs.m_X), Mathf.Max(lhs.m_Y, rhs.m_Y), Mathf.Max(lhs.m_Z, rhs.m_Z));

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int Max(in Vector3Int lhs, in Vector3Int rhs) => new Vector3Int(Mathf.Max(lhs.m_X, rhs.m_X), Mathf.Max(lhs.m_Y, rhs.m_Y), Mathf.Max(lhs.m_Z, rhs.m_Z));

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int Scale(Vector3Int a, Vector3Int b) => new Vector3Int(a.m_X * b.m_X, a.m_Y * b.m_Y, a.m_Z * b.m_Z);

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int Scale(in Vector3Int a, in Vector3Int b) => new Vector3Int(a.m_X * b.m_X, a.m_Y * b.m_Y, a.m_Z * b.m_Z);

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(Vector3Int scale) { m_X *= scale.m_X; m_Y *= scale.m_Y; m_Z *= scale.m_Z; }

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(in Vector3Int scale) { m_X *= scale.m_X; m_Y *= scale.m_Y; m_Z *= scale.m_Z; }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Clamp(Vector3Int min, Vector3Int max)
        {
            m_X = Mathf.Clamp(m_X, min.m_X, max.m_X);
            m_Y = Mathf.Clamp(m_Y, min.m_Y, max.m_Y);
            m_Z = Mathf.Clamp(m_Z, min.m_Z, max.m_Z);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Clamp(in Vector3Int min, in Vector3Int max)
        {
            m_X = Mathf.Clamp(m_X, min.m_X, max.m_X);
            m_Y = Mathf.Clamp(m_Y, min.m_Y, max.m_Y);
            m_Z = Mathf.Clamp(m_Z, min.m_Z, max.m_Z);
        }

        // Converts a Vector3Int to a [[Vector3]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector3(Vector3Int v) => new Vector3() { x = v.m_X, y = v.m_Y, z = v.m_Z };

        // Converts a Vector3Int to a [[Vector2Int]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static explicit operator Vector2Int(Vector3Int v) => new Vector2Int(v.m_X, v.m_Y);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int FloorToInt(Vector3 v) => new Vector3Int(
                Mathf.FloorToInt(v.x),
                Mathf.FloorToInt(v.y),
                Mathf.FloorToInt(v.z)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int FloorToInt(in Vector3 v) => new Vector3Int(
                Mathf.FloorToInt(v.x),
                Mathf.FloorToInt(v.y),
                Mathf.FloorToInt(v.z)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int CeilToInt(Vector3 v) => new Vector3Int(
                Mathf.CeilToInt(v.x),
                Mathf.CeilToInt(v.y),
                Mathf.CeilToInt(v.z)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int CeilToInt(in Vector3 v) => new Vector3Int(
                Mathf.CeilToInt(v.x),
                Mathf.CeilToInt(v.y),
                Mathf.CeilToInt(v.z)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int RoundToInt(Vector3 v) => new Vector3Int(
                Mathf.RoundToInt(v.x),
                Mathf.RoundToInt(v.y),
                Mathf.RoundToInt(v.z)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int RoundToInt(in Vector3 v) => new Vector3Int(
                Mathf.RoundToInt(v.x),
                Mathf.RoundToInt(v.y),
                Mathf.RoundToInt(v.z)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator+(Vector3Int a, Vector3Int b) => new Vector3Int(a.m_X + b.m_X, a.m_Y + b.m_Y, a.m_Z + b.m_Z);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator-(Vector3Int a, Vector3Int b) => new Vector3Int(a.m_X - b.m_X, a.m_Y - b.m_Y, a.m_Z - b.m_Z);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator*(Vector3Int a, Vector3Int b) => new Vector3Int(a.m_X * b.m_X, a.m_Y * b.m_Y, a.m_Z * b.m_Z);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator-(Vector3Int a) => new Vector3Int(-a.m_X, -a.m_Y, -a.m_Z);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator*(Vector3Int a, int b) => new Vector3Int(a.m_X * b, a.m_Y * b, a.m_Z * b);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator*(int a, Vector3Int b) => new Vector3Int(a * b.m_X, a * b.m_Y, a * b.m_Z);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3Int operator/(Vector3Int a, int b) => new Vector3Int(a.m_X / b, a.m_Y / b, a.m_Z / b);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Vector3Int lhs, Vector3Int rhs) => lhs.m_X == rhs.m_X && lhs.m_Y == rhs.m_Y && lhs.m_Z == rhs.m_Z;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Vector3Int lhs, Vector3Int rhs) => !(lhs == rhs);


        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Vector3Int v)
                return Equals(in v);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Vector3Int other) => this == other;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in Vector3Int other) => this == other;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            var yHash = m_Y.GetHashCode();
            var zHash = m_Z.GetHashCode();
            return m_X.GetHashCode() ^ (yHash << 4) ^ (yHash >> 28) ^ (zHash >> 4) ^ (zHash << 28);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("({0}, {1}, {2})", m_X.ToString(format, formatProvider), m_Y.ToString(format, formatProvider), m_Z.ToString(format, formatProvider));
        }

        public static Vector3Int zero
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Zero;
        }
        public static Vector3Int one
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_One;
        }
        public static Vector3Int up
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Up;
        }
        public static Vector3Int down
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Down;
        }
        public static Vector3Int left
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Left;
        }
        public static Vector3Int right
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Right;
        }
        public static Vector3Int forward
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Forward;
        }
        public static Vector3Int back
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => s_Back;
        }

        private static readonly Vector3Int s_Zero = new Vector3Int(0, 0, 0);
        private static readonly Vector3Int s_One = new Vector3Int(1, 1, 1);
        private static readonly Vector3Int s_Up = new Vector3Int(0, 1, 0);
        private static readonly Vector3Int s_Down = new Vector3Int(0, -1, 0);
        private static readonly Vector3Int s_Left = new Vector3Int(-1, 0, 0);
        private static readonly Vector3Int s_Right = new Vector3Int(1, 0, 0);
        private static readonly Vector3Int s_Forward = new Vector3Int(0, 0, 1);
        private static readonly Vector3Int s_Back = new Vector3Int(0, 0, -1);
    }
}
