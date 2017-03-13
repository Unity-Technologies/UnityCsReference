// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using scm = System.ComponentModel;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    // Representation of four-dimensional vectors.
    public partial struct Vector4
    {
        // *undocumented*
        public const float kEpsilon = 0.00001F;

        // X component of the vector.
        public float x;
        // Y component of the vector.
        public float y;
        // Z component of the vector.
        public float z;
        // W component of the vector.
        public float w;

        // Access the x, y, z, w components using [0], [1], [2], [3] respectively.
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector4 index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    case 3: w = value; break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector4 index!");
                }
            }
        }

        // Creates a new vector with given x, y, z, w components.
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        // Creates a new vector with given x, y, z components and sets /w/ to zero.
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; this.w = 0F; }
        // Creates a new vector with given x, y components and sets /z/ and /w/ to zero.
        public Vector4(float x, float y) { this.x = x; this.y = y; this.z = 0F; this.w = 0F; }

        // Set x, y, z and w components of an existing Vector4.
        public void Set(float newX, float newY, float newZ, float newW) { x = newX; y = newY; z = newZ; w = newW; }

        // Linearly interpolates between two vectors.
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector4(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t,
                a.w + (b.w - a.w) * t
                );
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float t)
        {
            return new Vector4(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t,
                a.w + (b.w - a.w) * t
                );
        }

        // Moves a point /current/ towards /target/.
        public static Vector4 MoveTowards(Vector4 current, Vector4 target, float maxDistanceDelta)
        {
            Vector4 toVector = target - current;
            float dist = toVector.magnitude;
            if (dist <= maxDistanceDelta || dist == 0)
                return target;
            return current + toVector / dist * maxDistanceDelta;
        }

        // Multiplies two vectors component-wise.
        public static Vector4 Scale(Vector4 a, Vector4 b)
        {
            return new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
        }

        // Multiplies every component of this vector by the same component of /scale/.
        public void Scale(Vector4 scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
            w *= scale.w;
        }

        // used to allow Vector4s to be used as keys in hash tables
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);
        }

        // also required for being able to use Vector4s as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is Vector4)) return false;

            Vector4 rhs = (Vector4)other;
            return x.Equals(rhs.x) && y.Equals(rhs.y) && z.Equals(rhs.z) && w.Equals(rhs.w);
        }

        // *undoc* --- we have normalized property now
        public static Vector4 Normalize(Vector4 a)
        {
            float mag = Magnitude(a);
            if (mag > kEpsilon)
                return a / mag;
            else
                return zero;
        }

        // Makes this vector have a ::ref::magnitude of 1.
        public void Normalize()
        {
            float mag = Magnitude(this);
            if (mag > kEpsilon)
                this = this / mag;
            else
                this = zero;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public Vector4 normalized
        {
            get
            {
                return Vector4.Normalize(this);
            }
        }

        // Dot Product of two vectors.
        public static float Dot(Vector4 a, Vector4 b) { return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w; }

        // Projects a vector onto another vector.
        public static Vector4 Project(Vector4 a, Vector4 b) { return b * Dot(a, b) / Dot(b, b); }

        // Returns the distance between /a/ and /b/.
        public static float Distance(Vector4 a, Vector4 b) { return Magnitude(a - b); }

        // *undoc* --- there's a property now
        public static float Magnitude(Vector4 a) { return Mathf.Sqrt(Dot(a, a)); }

        // Returns the length of this vector (RO).
        public float magnitude { get { return Mathf.Sqrt(Dot(this, this)); } }

        // Returns the squared length of this vector (RO).
        public float sqrMagnitude { get { return Dot(this, this); } }

        // Returns a vector that is made from the smallest components of two vectors.
        public static Vector4 Min(Vector4 lhs, Vector4 rhs)
        {
            return new Vector4(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z), Mathf.Min(lhs.w, rhs.w));
        }

        // Returns a vector that is made from the largest components of two vectors.
        public static Vector4 Max(Vector4 lhs, Vector4 rhs)
        {
            return new Vector4(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z), Mathf.Max(lhs.w, rhs.w));
        }

        static readonly Vector4 zeroVector = new Vector4(0F, 0F, 0F, 0F);
        static readonly Vector4 oneVector = new Vector4(1F, 1F, 1F, 1F);
        static readonly Vector4 positiveInfinityVector = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        static readonly Vector4 negativeInfinityVector = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        // Shorthand for writing @@Vector4(0,0,0,0)@@
        public static Vector4 zero { get { return zeroVector; } }
        // Shorthand for writing @@Vector4(1,1,1,1)@@
        public static Vector4 one { get { return oneVector; } }
        // Shorthand for writing @@Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)@@
        public static Vector4 positiveInfinity { get { return positiveInfinityVector; } }
        // Shorthand for writing @@Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)@@
        public static Vector4 negativeInfinity { get { return negativeInfinityVector; } }

        // Adds two vectors.
        public static Vector4 operator+(Vector4 a, Vector4 b) { return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w); }
        // Subtracts one vector from another.
        public static Vector4 operator-(Vector4 a, Vector4 b) { return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w); }
        // Negates a vector.
        public static Vector4 operator-(Vector4 a) { return new Vector4(-a.x, -a.y, -a.z, -a.w); }
        // Multiplies a vector by a number.
        public static Vector4 operator*(Vector4 a, float d) { return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d); }
        // Multiplies a vector by a number.
        public static Vector4 operator*(float d, Vector4 a) { return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d); }
        // Divides a vector by a number.
        public static Vector4 operator/(Vector4 a, float d) { return new Vector4(a.x / d, a.y / d, a.z / d, a.w / d); }

        // Returns true if the vectors are equal.
        public static bool operator==(Vector4 lhs, Vector4 rhs)
        {
            // Returns false in the presence of NaN values.
            return SqrMagnitude(lhs - rhs) < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        public static bool operator!=(Vector4 lhs, Vector4 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Converts a [[Vector3]] to a Vector4.
        public static implicit operator Vector4(Vector3 v)
        {
            return new Vector4(v.x, v.y, v.z, 0.0F);
        }

        // Converts a Vector4 to a [[Vector3]].
        public static implicit operator Vector3(Vector4 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        // Converts a [[Vector2]] to a Vector4.
        public static implicit operator Vector4(Vector2 v)
        {
            return new Vector4(v.x, v.y, 0.0F, 0.0F);
        }

        // Converts a Vector4 to a [[Vector2]].
        public static implicit operator Vector2(Vector4 v)
        {
            return new Vector2(v.x, v.y);
        }

        public override string ToString()
        {
            return UnityString.Format("({0:F1}, {1:F1}, {2:F1}, {3:F1})", x, y, z, w);
        }

        public string ToString(string format)
        {
            return UnityString.Format("({0}, {1}, {2}, {3})", x.ToString(format), y.ToString(format), z.ToString(format), w.ToString(format));
        }

        // *undoc* --- there's a property now
        public static float SqrMagnitude(Vector4 a) { return Vector4.Dot(a, a); }
        // *undoc* --- there's a property now
        public float SqrMagnitude() { return Dot(this, this); }
    }
} // namespace
