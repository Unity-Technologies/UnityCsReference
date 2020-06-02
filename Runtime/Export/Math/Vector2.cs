// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    static class MethodImplOptionsEx
    {
        public const short AggressiveInlining = 256;
    }
    // Representation of 2D vectors and points.
    [StructLayout(LayoutKind.Sequential)]
    [NativeClass("Vector2f")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstruction]
    public struct Vector2 : IEquatable<Vector2>, IFormattable
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
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector2(float x, float y) { this.x = x; this.y = y; }

        // Set x and y components of an existing Vector2.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set(float newX, float newY) { x = newX; y = newY; }

        // Linearly interpolates between two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector2(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t
            );
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
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
            // avoid vector ops because current scripting backends are terrible at inlining
            float toVector_x = target.x - current.x;
            float toVector_y = target.y - current.y;

            float sqDist = toVector_x * toVector_x + toVector_y * toVector_y;

            if (sqDist == 0 || (maxDistanceDelta >= 0 && sqDist <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float dist = (float)Math.Sqrt(sqDist);

            return new Vector2(current.x + toVector_x / dist * maxDistanceDelta,
                current.y + toVector_y / dist * maxDistanceDelta);
        }

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
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
        public override string ToString()
        {
            return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        }

        // Returns a nicely formatted string for this vector.
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F1";
            return UnityString.Format("({0}, {1})", x.ToString(format, formatProvider), y.ToString(format, formatProvider));
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

            return Equals((Vector2)other);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(Vector2 other)
        {
            return x == other.x && y == other.y;
        }

        public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
        {
            float factor = -2F * Dot(inNormal, inDirection);
            return new Vector2(factor * inNormal.x + inDirection.x, factor * inNormal.y + inDirection.y);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 Perpendicular(Vector2 inDirection)
        {
            return new Vector2(-inDirection.y, inDirection.x);
        }

        // Dot Product of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Dot(Vector2 lhs, Vector2 rhs) { return lhs.x * rhs.x + lhs.y * rhs.y; }

        // Returns the length of this vector (RO).
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        // Returns the squared length of this vector (RO).
        public float sqrMagnitude { get { return x * x + y * y; } }

        // Returns the angle in degrees between /from/ and /to/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Angle(Vector2 from, Vector2 to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = (float)Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < kEpsilonNormalSqrt)
                return 0F;

            float dot = Mathf.Clamp(Dot(from, to) / denominator, -1F, 1F);
            return (float)Math.Acos(dot) * Mathf.Rad2Deg;
        }

        // Returns the signed angle in degrees between /from/ and /to/. Always returns the smallest possible angle
        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            float unsigned_angle = Angle(from, to);
            float sign = Mathf.Sign(from.x * to.y - from.y * to.x);
            return unsigned_angle * sign;
        }

        // Returns the distance between /a/ and /b/.
        public static float Distance(Vector2 a, Vector2 b)
        {
            float diff_x = a.x - b.x;
            float diff_y = a.y - b.y;
            return (float)Math.Sqrt(diff_x * diff_x + diff_y * diff_y);
        }

        // Returns a copy of /vector/ with its magnitude clamped to /maxLength/.
        public static Vector2 ClampMagnitude(Vector2 vector, float maxLength)
        {
            float sqrMagnitude = vector.sqrMagnitude;
            if (sqrMagnitude > maxLength * maxLength)
            {
                float mag = (float)Math.Sqrt(sqrMagnitude);

                //these intermediate variables force the intermediate result to be
                //of float precision. without this, the intermediate result can be of higher
                //precision, which changes behavior.
                float normalized_x = vector.x / mag;
                float normalized_y = vector.y / mag;
                return new Vector2(normalized_x * maxLength,
                    normalized_y * maxLength);
            }
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

        [uei.ExcludeFromDocs]
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime, float maxSpeed)
        {
            float deltaTime = Time.deltaTime;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        [uei.ExcludeFromDocs]
        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime)
        {
            float deltaTime = Time.deltaTime;
            float maxSpeed = Mathf.Infinity;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime, [uei.DefaultValue("Mathf.Infinity")] float maxSpeed, [uei.DefaultValue("Time.deltaTime")] float deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Mathf.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);

            float change_x = current.x - target.x;
            float change_y = current.y - target.y;
            Vector2 originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;

            float maxChangeSq = maxChange * maxChange;
            float sqDist = change_x * change_x + change_y * change_y;
            if (sqDist > maxChangeSq)
            {
                var mag = (float)Math.Sqrt(sqDist);
                change_x = change_x / mag * maxChange;
                change_y = change_y / mag * maxChange;
            }

            target.x = current.x - change_x;
            target.y = current.y - change_y;

            float temp_x = (currentVelocity.x + omega * change_x) * deltaTime;
            float temp_y = (currentVelocity.y + omega * change_y) * deltaTime;

            currentVelocity.x = (currentVelocity.x - omega * temp_x) * exp;
            currentVelocity.y = (currentVelocity.y - omega * temp_y) * exp;

            float output_x = target.x + (change_x + temp_x) * exp;
            float output_y = target.y + (change_y + temp_y) * exp;

            // Prevent overshooting
            float origMinusCurrent_x = originalTo.x - current.x;
            float origMinusCurrent_y = originalTo.y - current.y;
            float outMinusOrig_x = output_x - originalTo.x;
            float outMinusOrig_y = output_y - originalTo.y;

            if (origMinusCurrent_x * outMinusOrig_x + origMinusCurrent_y * outMinusOrig_y > 0)
            {
                output_x = originalTo.x;
                output_y = originalTo.y;

                currentVelocity.x = (output_x - originalTo.x) / deltaTime;
                currentVelocity.y = (output_y - originalTo.y) / deltaTime;
            }
            return new Vector2(output_x, output_y);
        }

        // Adds two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator+(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        // Subtracts one vector from another.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator-(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        // Multiplies one vector by another.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator*(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        // Divides one vector over another.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator/(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
        // Negates a vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator-(Vector2 a) { return new Vector2(-a.x, -a.y); }
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator*(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator*(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        // Divides a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 operator/(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        // Returns true if the vectors are equal.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Vector2 lhs, Vector2 rhs)
        {
            // Returns false in the presence of NaN values.
            float diff_x = lhs.x - rhs.x;
            float diff_y = lhs.y - rhs.y;
            return (diff_x * diff_x + diff_y * diff_y) < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Vector2 lhs, Vector2 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Converts a [[Vector3]] to a Vector2.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector2(Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        // Converts a Vector2 to a [[Vector3]].
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
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
        // *Undocumented*
        public const float kEpsilonNormalSqrt = 1e-15f;
    }
}
