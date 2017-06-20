// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Representation of 2D vectors and points.
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2Int
    {
        public int x { get { return m_X; } set { m_X = value; } }
        public int y { get { return m_Y; } set { m_Y = value; } }

        private int m_X;
        private int m_Y;

        public Vector2Int(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        // Set x and y components of an existing Vector.
        public void Set(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        // Access the /x/ or /y/ component using [0] or [1] respectively.
        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    default:
                        throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
                }
            }

            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    default:
                        throw new IndexOutOfRangeException(String.Format("Invalid Vector2Int index addressed: {0}!", index));
                }
            }
        }

        // Returns the length of this vector (RO).
        public float magnitude { get { return Mathf.Sqrt((float)(x * x + y * y)); } }

        // Returns the squared length of this vector (RO).
        public int sqrMagnitude { get { return x * x + y * y; } }

        // Returns the distance between /a/ and /b/.
        public static float Distance(Vector2Int a, Vector2Int b) { return (a - b).magnitude; }

        // Returns a vector that is made from the smallest components of two vectors.
        public static Vector2Int Min(Vector2Int lhs, Vector2Int rhs) { return new Vector2Int(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y)); }

        // Returns a vector that is made from the largest components of two vectors.
        public static Vector2Int Max(Vector2Int lhs, Vector2Int rhs) { return new Vector2Int(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y)); }

        // Multiplies two vectors component-wise.
        public static Vector2Int Scale(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x * b.x, a.y * b.y); }

        // Multiplies every component of this vector by the same component of /scale/.
        public void Scale(Vector2Int scale) { x *= scale.x; y *= scale.y; }

        public void Clamp(Vector2Int min, Vector2Int max)
        {
            x = Math.Max(min.x, x);
            x = Math.Min(max.x, x);
            y = Math.Max(min.y, y);
            y = Math.Min(max.y, y);
        }

        // Converts a Vector2Int to a [[Vector2]].
        public static implicit operator Vector2(Vector2Int v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2Int FloorToInt(Vector2 v)
        {
            return new Vector2Int(
                Mathf.FloorToInt(v.x),
                Mathf.FloorToInt(v.y)
                );
        }

        public static Vector2Int CeilToInt(Vector2 v)
        {
            return new Vector2Int(
                Mathf.CeilToInt(v.x),
                Mathf.CeilToInt(v.y)
                );
        }

        public static Vector2Int RoundToInt(Vector2 v)
        {
            return new Vector2Int(
                Mathf.RoundToInt(v.x),
                Mathf.RoundToInt(v.y)
                );
        }

        public static Vector2Int operator+(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x + b.x, a.y + b.y);
        }

        public static Vector2Int operator-(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x - b.x, a.y - b.y);
        }

        public static Vector2Int operator*(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x * b.x, a.y * b.y);
        }

        public static Vector2Int operator*(Vector2Int a, int b)
        {
            return new Vector2Int(a.x * b, a.y * b);
        }

        public static bool operator==(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator!=(Vector2Int lhs, Vector2Int rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector2Int)) return false;

            Vector2Int rhs = (Vector2Int)other;
            return x.Equals(rhs.x) && y.Equals(rhs.y);
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2);
        }

        /// *listonly*
        public override string ToString()
        {
            return UnityString.Format("({0}, {1})", x, y);
        }

        public static Vector2Int zero { get { return s_Zero; } }
        public static Vector2Int one { get { return s_One; } }
        public static Vector2Int up { get { return s_Up; } }
        public static Vector2Int down { get { return s_Down; } }
        public static Vector2Int left { get { return s_Left; } }
        public static Vector2Int right { get { return s_Right; } }

        private static readonly Vector2Int s_Zero = new Vector2Int(0, 0);
        private static readonly Vector2Int s_One = new Vector2Int(1, 1);
        private static readonly Vector2Int s_Up = new Vector2Int(0, 1);
        private static readonly Vector2Int s_Down = new Vector2Int(0, -1);
        private static readonly Vector2Int s_Left = new Vector2Int(-1, 0);
        private static readonly Vector2Int s_Right = new Vector2Int(1, 0);
    }
}
