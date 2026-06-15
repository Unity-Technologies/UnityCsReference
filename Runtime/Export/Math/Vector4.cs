// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    [NativeHeader("Runtime/Math/Vector4.h")]
    [NativeClass("Vector4f")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstruction]
    [Serializable]
    // Representation of four-dimensional vectors.
    public partial struct Vector4 : IEquatable<Vector4>, IFormattable
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
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            readonly get
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

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        // Creates a new vector with given x, y, z components and sets /w/ to zero.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector4(float x, float y, float z) { this.x = x; this.y = y; this.z = z; this.w = 0F; }
        // Creates a new vector with given x, y components and sets /z/ and /w/ to zero.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector4(float x, float y) { this.x = x; this.y = y; this.z = 0F; this.w = 0F; }

        // Set x, y, z and w components of an existing Vector4.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set(float newX, float newY, float newZ, float newW) { x = newX; y = newY; z = newZ; w = newW; }

        // Linearly interpolates between two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector4() {
                x = a.x + (b.x - a.x) * t,
                y = a.y + (b.y - a.y) * t,
                z = a.z + (b.z - a.z) * t,
                w = a.w + (b.w - a.w) * t
            };
        }

        // Linearly interpolates between two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Lerp(in Vector4 a, in Vector4 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector4() {
                x = a.x + (b.x - a.x) * t,
                y = a.y + (b.y - a.y) * t,
                z = a.z + (b.z - a.z) * t,
                w = a.w + (b.w - a.w) * t
            };
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float t) => new Vector4() {
            x = a.x + (b.x - a.x) * t,
            y = a.y + (b.y - a.y) * t,
            z = a.z + (b.z - a.z) * t,
            w = a.w + (b.w - a.w) * t
        };

        // Linearly interpolates between two vectors without clamping the interpolant
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 LerpUnclamped(in Vector4 a, in Vector4 b, float t) => new Vector4() {
            x = a.x + (b.x - a.x) * t,
            y = a.y + (b.y - a.y) * t,
            z = a.z + (b.z - a.z) * t,
            w = a.w + (b.w - a.w) * t
        };

        // Moves a point /current/ towards /target/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 MoveTowards(Vector4 current, Vector4 target, float maxDistanceDelta)         
        {
            float toVector_x = target.x - current.x;
            float toVector_y = target.y - current.y;
            float toVector_z = target.z - current.z;
            float toVector_w = target.w - current.w;

            float sqdist = (toVector_x * toVector_x +
                toVector_y * toVector_y +
                toVector_z * toVector_z +
                toVector_w * toVector_w);

            if (sqdist == 0 || (maxDistanceDelta >= 0 && sqdist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            var dist = (float)Math.Sqrt(sqdist);

            Vector4 v;
            v.x = current.x + toVector_x / dist * maxDistanceDelta;
            v.y = current.y + toVector_y / dist * maxDistanceDelta;
            v.z = current.z + toVector_z / dist * maxDistanceDelta;
            v.w = current.w + toVector_w / dist * maxDistanceDelta;
            return v;
        }

        // Moves a point /current/ towards /target/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 MoveTowards(in Vector4 current, in Vector4 target, float maxDistanceDelta)
        {
            float toVector_x = target.x - current.x;
            float toVector_y = target.y - current.y;
            float toVector_z = target.z - current.z;
            float toVector_w = target.w - current.w;

            float sqdist = (toVector_x * toVector_x +
                toVector_y * toVector_y +
                toVector_z * toVector_z +
                toVector_w * toVector_w);

            if (sqdist == 0 || (maxDistanceDelta >= 0 && sqdist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            var dist = (float)Math.Sqrt(sqdist);

            Vector4 v;
            v.x = current.x + toVector_x / dist * maxDistanceDelta;
            v.y = current.y + toVector_y / dist * maxDistanceDelta;
            v.z = current.z + toVector_z / dist * maxDistanceDelta;
            v.w = current.w + toVector_w / dist * maxDistanceDelta;
            return v;
        }

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Scale(Vector4 a, Vector4 b) => new Vector4() { x = a.x * b.x, y = a.y * b.y, z = a.z * b.z, w = a.w * b.w };

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Scale(in Vector4 a, in Vector4 b) => new Vector4() { x = a.x * b.x, y = a.y * b.y, z = a.z * b.z, w = a.w * b.w };

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(Vector4 scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
            w *= scale.w;
        }

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(in Vector4 scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
            w *= scale.w;
        }

        // used to allow Vector4s to be used as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);

        // also required for being able to use Vector4s as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Vector4 v)
                return Equals(in v);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Vector4 other) => x == other.x && y == other.y && z == other.z && w == other.w;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in Vector4 other) => x == other.x && y == other.y && z == other.z && w == other.w;

        // *undoc* --- we have normalized property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Normalize(Vector4 a)
        {
            float mag = a.magnitude;
            return mag > kEpsilon ? new Vector4() { x = a.x / mag, y = a.y / mag, z = a.z / mag, w = a.w / mag } : zeroVector;
        }

        // *undoc* --- we have normalized property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Normalize(in Vector4 a)
        {
            float mag = a.magnitude;
            return mag > kEpsilon ? new Vector4() { x = a.x / mag, y = a.y / mag, z = a.z / mag, w = a.w / mag } : zeroVector;
        }

        // Makes this vector have a ::ref::magnitude of 1.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Normalize()
        {
            float mag = Magnitude(in this);
            if (mag > kEpsilon)
            {
                x /= mag;
                y /= mag;
                z /= mag; 
                w /= mag;
            }
            else
            {
                x = 0f;
                y = 0f;
                z = 0f;
                w = 0f;
            }
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public readonly Vector4 normalized
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => Vector4.Normalize(in this);
        }

        // Dot Product of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Dot(Vector4 a, Vector4 b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        // Dot Product of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Dot(in Vector4 a, in Vector4 b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        // Projects a vector onto another vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Project(Vector4 a, Vector4 b) => b * (Dot(in a, in b) / Dot(in b, in b));

        // Projects a vector onto another vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Project(in Vector4 a, in Vector4 b) => b * (Dot(in a, in b) / Dot(in b, in b));

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(Vector4 a, Vector4 b) => Magnitude(a - b);

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(in Vector4 a, in Vector4 b) => Magnitude(a - b);

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Magnitude(Vector4 a) => (float)Math.Sqrt(Dot(in a, in a));

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Magnitude(in Vector4 a) => (float)Math.Sqrt(Dot(in a, in a));

        // Returns the length of this vector (RO).
        public readonly float magnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => (float)Math.Sqrt(Dot(in this, in this));
        }

        // Returns the squared length of this vector (RO).
        public readonly float sqrMagnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => Dot(in this, in this);
        }

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Min(Vector4 lhs, Vector4 rhs) => new Vector4() { x = Mathf.Min(lhs.x, rhs.x), y = Mathf.Min(lhs.y, rhs.y), z = Mathf.Min(lhs.z, rhs.z), w = Mathf.Min(lhs.w, rhs.w) };

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Min(in Vector4 lhs, in Vector4 rhs) => new Vector4() { x = Mathf.Min(lhs.x, rhs.x), y = Mathf.Min(lhs.y, rhs.y), z = Mathf.Min(lhs.z, rhs.z), w = Mathf.Min(lhs.w, rhs.w) };

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Max(Vector4 lhs, Vector4 rhs) => new Vector4() { x = Mathf.Max(lhs.x, rhs.x), y = Mathf.Max(lhs.y, rhs.y), z = Mathf.Max(lhs.z, rhs.z), w = Mathf.Max(lhs.w, rhs.w) };

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 Max(in Vector4 lhs, in Vector4 rhs) => new Vector4() { x = Mathf.Max(lhs.x, rhs.x), y = Mathf.Max(lhs.y, rhs.y), z = Mathf.Max(lhs.z, rhs.z), w = Mathf.Max(lhs.w, rhs.w) };

        static readonly Vector4 zeroVector = new Vector4(0F, 0F, 0F, 0F);
        static readonly Vector4 oneVector = new Vector4(1F, 1F, 1F, 1F);
        static readonly Vector4 positiveInfinityVector = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        static readonly Vector4 negativeInfinityVector = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        // Shorthand for writing @@Vector4(0,0,0,0)@@
        public static Vector4 zero
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => zeroVector;
        }
        // Shorthand for writing @@Vector4(1,1,1,1)@@
        public static Vector4 one
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => oneVector;
        }
        // Shorthand for writing @@Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)@@
        public static Vector4 positiveInfinity
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => positiveInfinityVector;
        }
        // Shorthand for writing @@Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)@@
        public static Vector4 negativeInfinity
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => negativeInfinityVector;
        }

        // Adds two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 operator+(Vector4 a, Vector4 b) => new Vector4() { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z, w = a.w + b.w };
        // Subtracts one vector from another.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 operator-(Vector4 a, Vector4 b) => new Vector4() { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z, w = a.w - b.w };
        // Negates a vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 operator-(Vector4 a) => new Vector4() { x = -a.x, y = -a.y, z = -a.z, w = -a.w };
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 operator*(Vector4 a, float d) => new Vector4() { x = a.x * d, y = a.y * d, z = a.z * d, w = a.w * d };
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 operator*(float d, Vector4 a) => new Vector4() { x = a.x * d, y = a.y * d, z = a.z * d, w = a.w * d };
        // Divides a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector4 operator/(Vector4 a, float d) => new Vector4() { x = a.x / d, y = a.y / d, z = a.z / d, w = a.w / d };

        // Returns true if the vectors are equal.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Vector4 lhs, Vector4 rhs)
        {
            // Returns false in the presence of NaN values.
            float diffx = lhs.x - rhs.x;
            float diffy = lhs.y - rhs.y;
            float diffz = lhs.z - rhs.z;
            float diffw = lhs.w - rhs.w;
            float sqrmag = diffx * diffx + diffy * diffy + diffz * diffz + diffw * diffw;
            return sqrmag < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Vector4 lhs, Vector4 rhs)
        {
            // Returns true in the presence of NaN values.
            float diffx = lhs.x - rhs.x;
            float diffy = lhs.y - rhs.y;
            float diffz = lhs.z - rhs.z;
            float diffw = lhs.w - rhs.w;
            float sqrmag = diffx * diffx + diffy * diffy + diffz * diffz + diffw * diffw;
            return !(sqrmag < kEpsilon * kEpsilon);
        }


        // Converts a [[Vector3]] to a Vector4.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector4(Vector3 v) => new Vector4() { x = v.x, y = v.y, z = v.z, w = 0.0F };

        // Converts a Vector4 to a [[Vector3]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector3(Vector4 v) => new Vector3() { x = v.x, y = v.y, z = v.z };

        // Converts a [[Vector2]] to a Vector4.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector4(Vector2 v) => new Vector4() { x = v.x, y = v.y, z = 0.0F, w = 0.0F };

        // Converts a Vector4 to a [[Vector2]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector2(Vector4 v) => new Vector2() { x = v.x, y = v.y };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("({0}, {1}, {2}, {3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider), w.ToString(format, formatProvider));
        }

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float SqrMagnitude(Vector4 a) => a.sqrMagnitude;

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float SqrMagnitude(in Vector4 a) => a.sqrMagnitude;

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly float SqrMagnitude() => sqrMagnitude;
    }
} // namespace
