// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    // Representation of 3D vectors and points.
    [NativeHeader("Runtime/Math/Vector3.h")]
    [NativeClass("Vector3f")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Unity.IL2CPP.CompilerServices.Il2CppEagerStaticClassConstruction]
    public partial struct Vector3 : IEquatable<Vector3>, IFormattable
    {
        // *Undocumented*
        public const float kEpsilon = 0.00001F;
        // *Undocumented*
        public const float kEpsilonNormalSqrt = 1e-15F;

        // X component of the vector.
        public float x;
        // Y component of the vector.
        public float y;
        // Z component of the vector.
        public float z;

        // Linearly interpolates between two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t
            );
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t
            );
        }

        // Moves a point /current/ in a straight line towards a /target/ point.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            // avoid vector ops because current scripting backends are terrible at inlining
            float toVector_x = target.x - current.x;
            float toVector_y = target.y - current.y;
            float toVector_z = target.z - current.z;

            float sqdist = toVector_x * toVector_x + toVector_y * toVector_y + toVector_z * toVector_z;

            if (sqdist == 0 || (maxDistanceDelta >= 0 && sqdist <= maxDistanceDelta * maxDistanceDelta))
                return target;
            var dist = (float)Math.Sqrt(sqdist);

            return new Vector3(current.x + toVector_x / dist * maxDistanceDelta,
                current.y + toVector_y / dist * maxDistanceDelta,
                current.z + toVector_z / dist * maxDistanceDelta);
        }

        [uei.ExcludeFromDocs]
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, float maxSpeed)
        {
            float deltaTime = Time.deltaTime;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        [uei.ExcludeFromDocs]
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime)
        {
            float deltaTime = Time.deltaTime;
            float maxSpeed = Mathf.Infinity;
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        // Gradually changes a vector towards a desired goal over time.
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime, [uei.DefaultValue("Mathf.Infinity")]  float maxSpeed, [uei.DefaultValue("Time.deltaTime")]  float deltaTime)
        {
            float output_x = 0f;
            float output_y = 0f;
            float output_z = 0f;

            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Mathf.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);

            float change_x = current.x - target.x;
            float change_y = current.y - target.y;
            float change_z = current.z - target.z;
            Vector3 originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;

            float maxChangeSq = maxChange * maxChange;
            float sqrmag = change_x * change_x + change_y * change_y + change_z * change_z;
            if (sqrmag > maxChangeSq)
            {
                var mag = (float)Math.Sqrt(sqrmag);
                change_x = change_x / mag * maxChange;
                change_y = change_y / mag * maxChange;
                change_z = change_z / mag * maxChange;
            }

            target.x = current.x - change_x;
            target.y = current.y - change_y;
            target.z = current.z - change_z;

            float temp_x = (currentVelocity.x + omega * change_x) * deltaTime;
            float temp_y = (currentVelocity.y + omega * change_y) * deltaTime;
            float temp_z = (currentVelocity.z + omega * change_z) * deltaTime;

            currentVelocity.x = (currentVelocity.x - omega * temp_x) * exp;
            currentVelocity.y = (currentVelocity.y - omega * temp_y) * exp;
            currentVelocity.z = (currentVelocity.z - omega * temp_z) * exp;

            output_x = target.x + (change_x + temp_x) * exp;
            output_y = target.y + (change_y + temp_y) * exp;
            output_z = target.z + (change_z + temp_z) * exp;

            // Prevent overshooting
            float origMinusCurrent_x = originalTo.x - current.x;
            float origMinusCurrent_y = originalTo.y - current.y;
            float origMinusCurrent_z = originalTo.z - current.z;
            float outMinusOrig_x = output_x - originalTo.x;
            float outMinusOrig_y = output_y - originalTo.y;
            float outMinusOrig_z = output_z - originalTo.z;

            if (origMinusCurrent_x * outMinusOrig_x + origMinusCurrent_y * outMinusOrig_y + origMinusCurrent_z * outMinusOrig_z > 0)
            {
                output_x = originalTo.x;
                output_y = originalTo.y;
                output_z = originalTo.z;

                currentVelocity.x = (output_x - originalTo.x) / deltaTime;
                currentVelocity.y = (output_y - originalTo.y) / deltaTime;
                currentVelocity.z = (output_z - originalTo.z) / deltaTime;
            }

            return new Vector3(output_x, output_y, output_z);
        }

        // Access the x, y, z components using [0], [1], [2] respectively.
        public float this[int index]
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
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
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }

        // Creates a new vector with given x, y, z components.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        // Creates a new vector with given x, y components and sets /z/ to zero.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0F; }

        // Set x, y and z components of an existing Vector3.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set(float newX, float newY, float newZ) { x = newX; y = newY; z = newZ; }

        // Multiplies two vectors component-wise.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }

        // Multiplies every component of this vector by the same component of /scale/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Scale(Vector3 scale) { x *= scale.x; y *= scale.y; z *= scale.z; }

        // Cross Product of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(
                lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.z * rhs.x - lhs.x * rhs.z,
                lhs.x * rhs.y - lhs.y * rhs.x);
        }

        // used to allow Vector3s to be used as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        // also required for being able to use Vector3s as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (other is Vector3 v)
                return Equals(v);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(Vector3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        // Reflects a vector off the plane defined by a normal.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            float factor = -2F * Dot(inNormal, inDirection);
            return new Vector3(factor * inNormal.x + inDirection.x,
                factor * inNormal.y + inDirection.y,
                factor * inNormal.z + inDirection.z);
        }

        // *undoc* --- we have normalized property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Normalize(Vector3 value)
        {
            float mag = Magnitude(value);
            if (mag > kEpsilon)
                return value / mag;
            else
                return zero;
        }

        // Makes this vector have a ::ref::magnitude of 1.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Normalize()
        {
            float mag = Magnitude(this);
            if (mag > kEpsilon)
                this = this / mag;
            else
                this = zero;
        }

        // Returns this vector with a ::ref::magnitude of 1 (RO).
        public Vector3 normalized
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get { return Vector3.Normalize(this); }
        }

        // Dot Product of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Dot(Vector3 lhs, Vector3 rhs) { return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z; }

        // Projects a vector onto another vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Project(Vector3 vector, Vector3 onNormal)
        {
            float sqrMag = Dot(onNormal, onNormal);
            if (sqrMag < Mathf.Epsilon)
                return zero;
            else
            {
                var dotSqrMag = Dot(vector, onNormal)/ sqrMag;
                return new Vector3(onNormal.x * dotSqrMag,
                    onNormal.y * dotSqrMag,
                    onNormal.z * dotSqrMag);
            }
        }

        // Projects a vector onto a plane defined by a normal orthogonal to the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            float sqrMag = Dot(planeNormal, planeNormal);
            if (sqrMag < Mathf.Epsilon)
                return vector;
            else
            {
                var dotSqrMag = Dot(vector, planeNormal)/ sqrMag;
                return new Vector3(vector.x - planeNormal.x * dotSqrMag,
                    vector.y - planeNormal.y * dotSqrMag,
                    vector.z - planeNormal.z * dotSqrMag);
            }
        }

        // Returns the angle in degrees between /from/ and /to/. This is always the smallest
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Angle(Vector3 from, Vector3 to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = (float)Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < kEpsilonNormalSqrt)
                return 0F;

            float dot = Mathf.Clamp(Dot(from, to) / denominator, -1F, 1F);
            return ((float)Math.Acos(dot)) * Mathf.Rad2Deg;
        }

        // The smaller of the two possible angles between the two vectors is returned, therefore the result will never be greater than 180 degrees or smaller than -180 degrees.
        // If you imagine the from and to vectors as lines on a piece of paper, both originating from the same point, then the /axis/ vector would point up out of the paper.
        // The measured angle between the two vectors would be positive in a clockwise direction and negative in an anti-clockwise direction.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsignedAngle = Angle(from, to);

            float cross_x = from.y * to.z - from.z * to.y;
            float cross_y = from.z * to.x - from.x * to.z;
            float cross_z = from.x * to.y - from.y * to.x;
            float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
        }

        // Returns the distance between /a/ and /b/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Distance(Vector3 a, Vector3 b)
        {
            float diff_x = a.x - b.x;
            float diff_y = a.y - b.y;
            float diff_z = a.z - b.z;
            return (float)Math.Sqrt(diff_x * diff_x + diff_y * diff_y + diff_z * diff_z);
        }

        // Returns a copy of /vector/ with its magnitude clamped to /maxLength/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
        {
            float sqrmag = vector.sqrMagnitude;
            if (sqrmag > maxLength * maxLength)
            {
                float mag = (float)Math.Sqrt(sqrmag);
                //these intermediate variables force the intermediate result to be
                //of float precision. without this, the intermediate result can be of higher
                //precision, which changes behavior.
                float normalized_x = vector.x / mag;
                float normalized_y = vector.y / mag;
                float normalized_z = vector.z / mag;
                return new Vector3(normalized_x * maxLength,
                    normalized_y * maxLength,
                    normalized_z * maxLength);
            }
            return vector;
        }

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float Magnitude(Vector3 vector) { return (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z); }

        // Returns the length of this vector (RO).
        public float magnitude
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get { return (float)Math.Sqrt(x * x + y * y + z * z); }
        }

        // *undoc* --- there's a property now
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static float SqrMagnitude(Vector3 vector) { return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z; }

        // Returns the squared length of this vector (RO).
        public float sqrMagnitude { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return x * x + y * y + z * z; } }

        // Returns a vector that is made from the smallest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Min(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));
        }

        // Returns a vector that is made from the largest components of two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 Max(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));
        }

        static readonly Vector3 zeroVector = new Vector3(0F, 0F, 0F);
        static readonly Vector3 oneVector = new Vector3(1F, 1F, 1F);
        static readonly Vector3 upVector = new Vector3(0F, 1F, 0F);
        static readonly Vector3 downVector = new Vector3(0F, -1F, 0F);
        static readonly Vector3 leftVector = new Vector3(-1F, 0F, 0F);
        static readonly Vector3 rightVector = new Vector3(1F, 0F, 0F);
        static readonly Vector3 forwardVector = new Vector3(0F, 0F, 1F);
        static readonly Vector3 backVector = new Vector3(0F, 0F, -1F);
        static readonly Vector3 positiveInfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        static readonly Vector3 negativeInfinityVector = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        // Shorthand for writing @@Vector3(0, 0, 0)@@
        public static Vector3 zero { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return zeroVector; } }
        // Shorthand for writing @@Vector3(1, 1, 1)@@
        public static Vector3 one { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return oneVector; } }
        // Shorthand for writing @@Vector3(0, 0, 1)@@
        public static Vector3 forward { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return forwardVector; } }
        public static Vector3 back { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return backVector; } }
        // Shorthand for writing @@Vector3(0, 1, 0)@@
        public static Vector3 up { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return upVector; } }
        public static Vector3 down { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return downVector; } }
        public static Vector3 left { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return leftVector; } }
        // Shorthand for writing @@Vector3(1, 0, 0)@@
        public static Vector3 right { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return rightVector; } }
        // Shorthand for writing @@Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)@@
        public static Vector3 positiveInfinity { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return positiveInfinityVector; } }
        // Shorthand for writing @@Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)@@
        public static Vector3 negativeInfinity { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return negativeInfinityVector; } }

        // Adds two vectors.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 operator+(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        // Subtracts one vector from another.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 operator-(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        // Negates a vector.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 operator-(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 operator*(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        // Multiplies a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 operator*(float d, Vector3 a) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        // Divides a vector by a number.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector3 operator/(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }

        // Returns true if the vectors are equal.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Vector3 lhs, Vector3 rhs)
        {
            // Returns false in the presence of NaN values.
            float diff_x = lhs.x - rhs.x;
            float diff_y = lhs.y - rhs.y;
            float diff_z = lhs.z - rhs.z;
            float sqrmag = diff_x * diff_x + diff_y * diff_y + diff_z * diff_z;
            return sqrmag < kEpsilon * kEpsilon;
        }

        // Returns true if vectors are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Vector3 lhs, Vector3 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override string ToString()
        {
            return ToString(null, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return UnityString.Format("({0}, {1}, {2})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider));
        }

        [System.Obsolete("Use Vector3.forward instead.")]
        public static Vector3 fwd { get { return new Vector3(0F, 0F, 1F); } }
        [System.Obsolete("Use Vector3.Angle instead. AngleBetween uses radians instead of degrees and was deprecated for this reason")]
        public static float AngleBetween(Vector3 from, Vector3 to) { return (float)Math.Acos(Mathf.Clamp(Vector3.Dot(from.normalized, to.normalized), -1F, 1F)); }
        [System.Obsolete("Use Vector3.ProjectOnPlane instead.")]
        public static Vector3 Exclude(Vector3 excludeThis, Vector3 fromThat) { return ProjectOnPlane(fromThat, excludeThis); }
    }
}
