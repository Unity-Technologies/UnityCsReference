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
    public struct Vector2
    {
        // X component of the vector.
        public float x;
        // Y component of the vector.
        public float y;

        // Access the /x/ or /y/ component using [0] or [1] respectively.
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }
        }

        // Constructs a new vector with given x, y components.
        public Vector2(float x, float y) { this.x = x; this.y = y; }

        // Set x and y components of an existing Vector2.
        public void Set(float newX, float newY) { x = newX; y = newY; }

        // Linearly interpolates between two vectors.
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
                );
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t)
        {
            return new Vector2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
                );
        }

        // Moves a point /current/ towards /target/.
        public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta)
        {
            Vector2 toVector = target - current;
            float dist = toVector.magnitude;
            if (dist <= maxDistanceDelta || dist == 0)
                return target;
            return current + toVector / dist * maxDistanceDelta;
        }

        // Multiplies two vectors component-wise.
        public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }

        // Multiplies every component of this vector by the same component of /scale/.
        public void Scale(Vector2 scale) { x *= scale.x; y *= scale.y; }

        // Makes this vector have a ::ref::magnitude of 1.
        public void Normalize()
        {
            float mag = magnitude;
            if (mag > kEpsilon)
                this = this / mag;
            else
                this = zero;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public Vector2 normalized
        {
            get
            {
                Vector2 v = new Vector2(x, y);
                v.Normalize();
                return v;
            }
        }

        /// *listonly*
        public override string ToString() { return UnityString.Format("({0:F1}, {1:F1})", x, y); }
        // Returns a nicely formatted string for this vector.
        public string ToString(string format)
        {
            return UnityString.Format("({0}, {1})", x.ToString(format), y.ToString(format));
        }

        // used to allow Vector2s to be used as keys in hash tables
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2);
        }

        // also required for being able to use Vector2s as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is Vector2)) return false;

            Vector2 rhs = (Vector2)other;
            return x.Equals(rhs.x) && y.Equals(rhs.y);
        }

        public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
        {
            return -2F * Dot(inNormal, inDirection) * inNormal + inDirection;
        }

        // Dot Product of two vectors.
        public static float Dot(Vector2 lhs, Vector2 rhs) { return lhs.x * rhs.x + lhs.y * rhs.y; }

        // Returns the length of this vector (RO).
        public float magnitude { get { return Mathf.Sqrt(x * x + y * y); } }
        // Returns the squared length of this vector (RO).
        public float sqrMagnitude { get { return x * x + y * y; } }

        // Returns the angle in degrees between /from/ and /to/.
        public static float Angle(Vector2 from, Vector2 to) { return Mathf.Acos(Mathf.Clamp(Dot(from.normalized, to.normalized), -1F, 1F)) * Mathf.Rad2Deg; }

        // Returns the signed angle in degrees between /from/ and /to/. Always returns the smallest possible angle
        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            Vector2 from_norm = from.normalized, to_norm = to.normalized;
            float unsigned_angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(from_norm, to_norm), -1F, 1F)) * Mathf.Rad2Deg;
            float sign = Mathf.Sign(from_norm.x * to_norm.y - from_norm.y * to_norm.x);
            return unsigned_angle * sign;
        }

        // Returns the distance between /a/ and /b/.
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }

        // Returns a copy of /vector/ with its magnitude clamped to /maxLength/.
        public static Vector2 ClampMagnitude(Vector2 vector, float maxLength)
        {
            if (vector.sqrMagnitude > maxLength * maxLength)
                return vector.normalized * maxLength;
            return vector;
        }

        // [Obsolete("Use Vector2.sqrMagnitude")]
        public static float SqrMagnitude(Vector2 a) { return a.x * a.x + a.y * a.y; }
        // [Obsolete("Use Vector2.sqrMagnitude")]
        public float SqrMagnitude() { return x * x + y * y; }

        // Returns a vector that is made from the smallest components of two vectors.
        public static Vector2 Min(Vector2 lhs, Vector2 rhs) { return new Vector2(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y)); }

        // Returns a vector that is made from the largest components of two vectors.
        public static Vector2 Max(Vector2 lhs, Vector2 rhs) { return new Vector2(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y)); }

        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime, float maxSpeed /*= Mathf.Infinity*/, float deltaTime /*= Time.deltaTime*/)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Mathf.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
            Vector2 change = current - target;
            Vector2 originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = ClampMagnitude(change, maxChange);
            target = current - change;

            Vector2 temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            Vector2 output = target + (change + temp) * exp;

            // Prevent overshooting
            if (Dot(originalTo - current, output - originalTo) > 0)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        // Adds two vectors.
        public static Vector2 operator+(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        // Subtracts one vector from another.
        public static Vector2 operator-(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        // Negates a vector.
        public static Vector2 operator-(Vector2 a) { return new Vector2(-a.x, -a.y); }
        // Multiplies a vector by a number.
        public static Vector2 operator*(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        // Multiplies a vector by a number.
        public static Vector2 operator*(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        // Divides a vector by a number.
        public static Vector2 operator/(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        // Returns true if the vectors are equal.
        public static bool operator==(Vector2 lhs, Vector2 rhs)
        {
            // Returns false in the presence of NaN values.
            return (lhs - rhs).sqrMagnitude < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        public static bool operator!=(Vector2 lhs, Vector2 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Converts a [[Vector3]] to a Vector2.
        public static implicit operator Vector2(Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        // Converts a Vector2 to a [[Vector3]].
        public static implicit operator Vector3(Vector2 v)
        {
            return new Vector3(v.x, v.y, 0);
        }

        static readonly Vector2 zeroVector = new Vector2(0F, 0F);
        static readonly Vector2 oneVector = new Vector2(1F, 1F);
        static readonly Vector2 upVector = new Vector2(0F, 1F);
        static readonly Vector2 downVector = new Vector2(0F, -1F);
        static readonly Vector2 leftVector = new Vector2(-1F, 0F);
        static readonly Vector2 rightVector = new Vector2(1F, 0F);
        static readonly Vector2 positiveInfinityVector = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        static readonly Vector2 negativeInfinityVector = new Vector2(float.NegativeInfinity, float.NegativeInfinity);


        // Shorthand for writing @@Vector2(0, 0)@@
        public static Vector2 zero { get { return zeroVector; } }
        // Shorthand for writing @@Vector2(1, 1)@@
        public static Vector2 one { get { return oneVector; }   }
        // Shorthand for writing @@Vector2(0, 1)@@
        public static Vector2 up { get { return upVector; } }
        // Shorthand for writing @@Vector2(0, -1)@@
        public static Vector2 down { get { return downVector; } }
        // Shorthand for writing @@Vector2(-1, 0)@@
        public static Vector2 left { get { return leftVector; } }
        // Shorthand for writing @@Vector2(1, 0)@@
        public static Vector2 right { get { return rightVector; } }
        // Shorthand for writing @@Vector2(float.PositiveInfinity, float.PositiveInfinity)@@
        public static Vector2 positiveInfinity { get { return positiveInfinityVector; } }
        // Shorthand for writing @@Vector2(float.NegativeInfinity, float.NegativeInfinity)@@
        public static Vector2 negativeInfinity { get { return negativeInfinityVector; } }

        // *Undocumented*
        public const float kEpsilon = 0.00001F;
    }
}
