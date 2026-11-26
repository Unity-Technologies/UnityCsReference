// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    internal static partial class MathUtility
    {
        internal const float k_OneOverTwoPI = 0.159154943091895335768883763372514362f;
        internal const float k_InversePI = 0.318309886183790671537767526745028724068f; // 1 / PI

        // rsqrt normalisation perturbs the dot of two opposite directions by up to ~3e-7, so EPSILON cannot separate them from noise.
        internal const float k_OppositeDirectionsTolerance = 1e-6f;

        // FIXME: Added this temporarily because the math.AffineTransform.identity
        // is unstable in IL2CPP builds and returns default values rather than the identity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform affineIdentity() =>
            math.AffineTransform(float3.zero, float3x3.identity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 one() => new float3(1f);

        /// <summary>
        /// Returns the sign of x.
        /// <remark>Contrary to <see cref="Unity.Mathematics.math.sign(float)"/> this function returns 1 for x >= 0 otherwise -1.</remark>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float sgn(float x) => math.select(1f, -1f, x < 0f);

        /// <summary>
        /// Returns the sign of x.
        /// <remark>Contrary to <see cref="Unity.Mathematics.math.sign(double)"/> this function returns 1 for x >= 0 otherwise -1.</remark>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double sgn(double x) => math.select(1.0, -1.0, x < 0.0);

        /// <summary>Returns the angle x wrapped to the range -PI to PI.</summary>
        /// <remarks>The result is congruent to x modulo 2 * PI and lies in the half-open range [-PI, PI).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float unwind(float x)
        {
            x = math.fmod(x + math.PI, math.PI2);
            return math.select(x - math.PI, x + math.PI, x < 0f);
        }

        /// <summary>Returns the componentwise angles of a float2 vector wrapped to the range -PI to PI.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 unwind(float2 x)
        {
            x = math.fmod(x + math.PI, math.PI2);
            return math.select(x - math.PI, x + math.PI, x < 0f);
        }

        /// <summary>Returns the componentwise angles of a float3 vector wrapped to the range -PI to PI.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 unwind(float3 x)
        {
            x = math.fmod(x + math.PI, math.PI2);
            return math.select(x - math.PI, x + math.PI, x < 0f);
        }

        /// <summary>Returns the componentwise angles of a float4 vector wrapped to the range -PI to PI.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 unwind(float4 x)
        {
            x = math.fmod(x + math.PI, math.PI2);
            return math.select(x - math.PI, x + math.PI, x < 0f);
        }

        /// <summary>Returns the smallest signed angle in radians from one angle to another.</summary>
        /// <remarks>The result lies in the half-open range [-PI, PI).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float deltaangle(float from, float to) => unwind(to - from);

        /// <summary>Returns the componentwise smallest signed angles in radians from the angles in one float2 vector to the angles in another.</summary>
        /// <remarks>Each component of from and to is an angle in radians, not a direction. The results lie in the half-open range [-PI, PI).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 deltaangle(float2 from, float2 to) => unwind(to - from);

        /// <summary>Returns the componentwise smallest signed angles in radians from the angles in one float3 vector to the angles in another.</summary>
        /// <remarks>Each component of from and to is an angle in radians, not a direction. The results lie in the half-open range [-PI, PI).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 deltaangle(float3 from, float3 to) => unwind(to - from);

        /// <summary>Returns the componentwise smallest signed angles in radians from the angles in one float4 vector to the angles in another.</summary>
        /// <remarks>Each component of from and to is an angle in radians, not a direction. The results lie in the half-open range [-PI, PI).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 deltaangle(float4 from, float4 to) => unwind(to - from);

        /// <summary>Returns the reciprocal of x, or zero if the magnitude of x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float rcpsafe(float x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(0f, math.rcp(x), math.abs(x) > epsilon);

        /// <summary>Returns the componentwise reciprocal of x, or zero where the magnitude of x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 rcpsafe(float2 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float2.zero, math.rcp(x), math.abs(x) > epsilon);

        /// <summary>Returns the componentwise reciprocal of x, or zero where the magnitude of x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 rcpsafe(float3 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float3.zero, math.rcp(x), math.abs(x) > epsilon);

        /// <summary>Returns the componentwise reciprocal of x, or zero where the magnitude of x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 rcpsafe(float4 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float4.zero, math.rcp(x), math.abs(x) > epsilon);

        /// <summary>Returns the square root of x, or zero if x isn't greater than zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float sqrtsafe(float x) => math.select(0f, math.sqrt(x), x > 0f);

        /// <summary>Returns the componentwise square root of x, or zero where x isn't greater than zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 sqrtsafe(float2 x) => math.select(float2.zero, math.sqrt(x), x > 0f);

        /// <summary>Returns the componentwise square root of x, or zero where x isn't greater than zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 sqrtsafe(float3 x) => math.select(float3.zero, math.sqrt(x), x > 0f);

        /// <summary>Returns the componentwise square root of x, or zero where x isn't greater than zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 sqrtsafe(float4 x) => math.select(float4.zero, math.sqrt(x), x > 0f);

        /// <summary>Returns the reciprocal square root of x, or zero if x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float rsqrtsafe(float x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(0f, math.rsqrt(x), x > epsilon);

        /// <summary>Returns the componentwise reciprocal square root of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 rsqrtsafe(float2 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float2.zero, math.rsqrt(x), x > epsilon);

        /// <summary>Returns the componentwise reciprocal square root of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 rsqrtsafe(float3 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float3.zero, math.rsqrt(x), x > epsilon);

        /// <summary>Returns the componentwise reciprocal square root of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 rsqrtsafe(float4 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float4.zero, math.rsqrt(x), x > epsilon);

        /// <summary>Returns the natural logarithm of x, or zero if x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float logsafe(float x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(0f, math.log(x), x > epsilon);

        /// <summary>Returns the componentwise natural logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 logsafe(float2 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float2.zero, math.log(x), x > epsilon);

        /// <summary>Returns the componentwise natural logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 logsafe(float3 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float3.zero, math.log(x), x > epsilon);

        /// <summary>Returns the componentwise natural logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 logsafe(float4 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float4.zero, math.log(x), x > epsilon);

        /// <summary>Returns the base-2 logarithm of x, or zero if x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float log2safe(float x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(0f, math.log2(x), x > epsilon);

        /// <summary>Returns the componentwise base-2 logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 log2safe(float2 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float2.zero, math.log2(x), x > epsilon);

        /// <summary>Returns the componentwise base-2 logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 log2safe(float3 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float3.zero, math.log2(x), x > epsilon);

        /// <summary>Returns the componentwise base-2 logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 log2safe(float4 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float4.zero, math.log2(x), x > epsilon);

        /// <summary>Returns the base-10 logarithm of x, or zero if x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float log10safe(float x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(0f, math.log10(x), x > epsilon);

        /// <summary>Returns the componentwise base-10 logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 log10safe(float2 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float2.zero, math.log10(x), x > epsilon);

        /// <summary>Returns the componentwise base-10 logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 log10safe(float3 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float3.zero, math.log10(x), x > epsilon);

        /// <summary>Returns the componentwise base-10 logarithm of x, or zero where x isn't greater than epsilon.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 log10safe(float4 x, float epsilon = math.FLT_MIN_NORMAL) =>
            math.select(float4.zero, math.log10(x), x > epsilon);

        /// <summary>Returns the floating point remainder of x / y, or zero if y is zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float fmodsafe(float x, float y) => math.select(0f, math.fmod(x, y), math.abs(y) > 0f);

        /// <summary>Returns the componentwise floating point remainder of x / y, or zero where y is zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 fmodsafe(float2 x, float2 y) =>
            math.select(float2.zero, math.fmod(x, y), math.abs(y) > 0f);

        /// <summary>Returns the componentwise floating point remainder of x / y, or zero where y is zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 fmodsafe(float3 x, float3 y) =>
            math.select(float3.zero, math.fmod(x, y), math.abs(y) > 0f);

        /// <summary>Returns the componentwise floating point remainder of x / y, or zero where y is zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 fmodsafe(float4 x, float4 y) =>
            math.select(float4.zero, math.fmod(x, y), math.abs(y) > 0f);

        /// <summary>Returns the projection of a onto the line through the origin that is perpendicular to planeNormal.</summary>
        /// <remarks>planeNormal doesn't need to be unit length but must not be zero. Use <see cref="projectonplanesafe(float2,float2,float2)"/> when the result may not be finite.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 projectonplane(float2 a, float2 planeNormal) => a - math.project(a, planeNormal);

        /// <summary>Returns the projection of a onto the plane through the origin with normal planeNormal.</summary>
        /// <remarks>planeNormal doesn't need to be unit length but must not be zero. Use <see cref="projectonplanesafe(float3,float3,float3)"/> when the result may not be finite.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 projectonplane(float3 a, float3 planeNormal) => a - math.project(a, planeNormal);

        /// <summary>Returns the projection of a onto the hyperplane through the origin with normal planeNormal.</summary>
        /// <remarks>planeNormal doesn't need to be unit length but must not be zero. Use <see cref="projectonplanesafe(float4,float4,float4)"/> when the result may not be finite.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 projectonplane(float4 a, float4 planeNormal) => a - math.project(a, planeNormal);

        /// <summary>Returns the projection of a onto the line through the origin that is perpendicular to planeNormal, or defaultValue if the projection isn't finite, for example when planeNormal is zero or nearly zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 projectonplanesafe(float2 a, float2 planeNormal, float2 defaultValue = default)
        {
            var proj = projectonplane(a, planeNormal);
            return math.select(defaultValue, proj, math.all(math.isfinite(proj)));
        }

        /// <summary>Returns the projection of a onto the plane through the origin with normal planeNormal, or defaultValue if the projection isn't finite, for example when planeNormal is zero or nearly zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 projectonplanesafe(float3 a, float3 planeNormal, float3 defaultValue = default)
        {
            var proj = projectonplane(a, planeNormal);
            return math.select(defaultValue, proj, math.all(math.isfinite(proj)));
        }

        /// <summary>Returns the projection of a onto the hyperplane through the origin with normal planeNormal, or defaultValue if the projection isn't finite, for example when planeNormal is zero or nearly zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 projectonplanesafe(float4 a, float4 planeNormal, float4 defaultValue = default)
        {
            var proj = projectonplane(a, planeNormal);
            return math.select(defaultValue, proj, math.all(math.isfinite(proj)));
        }

        /// <summary>Returns the angle in radians between two float2 vectors.</summary>
        /// <remarks>The vectors don't need to be unit length. Returns zero if either vector is zero length or not finite.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float angle(float2 a, float2 b)
        {
            float lengthsqA = math.lengthsq(a);
            float lengthsqB = math.lengthsq(b);
            float cosine = math.dot(a, b) * math.rsqrt(lengthsqA) * math.rsqrt(lengthsqB);
            return math.select(0f, math.acos(math.clamp(cosine, -1f, 1f)),
                math.isfinite(cosine) & math.isfinite(lengthsqA) & math.isfinite(lengthsqB));
        }

        /// <summary>Returns the angle in radians between two float3 vectors.</summary>
        /// <remarks>The vectors don't need to be unit length. Returns zero if either vector is zero length or not finite.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float angle(float3 a, float3 b)
        {
            float lengthsqA = math.lengthsq(a);
            float lengthsqB = math.lengthsq(b);
            float cosine = math.dot(a, b) * math.rsqrt(lengthsqA) * math.rsqrt(lengthsqB);
            return math.select(0f, math.acos(math.clamp(cosine, -1f, 1f)),
                math.isfinite(cosine) & math.isfinite(lengthsqA) & math.isfinite(lengthsqB));
        }

        /// <summary>Returns a fast approximation of the sine of x.</summary>
        /// <remarks>The maximum absolute error is below 1e-5 for inputs with a magnitude up to 10 * PI, and grows with the magnitude of the input.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float fastsin(float x) => fastsin(math.float4(x)).x;

        /// <summary>Returns a fast componentwise approximation of the sine of x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 fastsin(float2 x) => fastsin(math.float4(x, 0f, 0f)).xy;

        /// <summary>Returns a fast componentwise approximation of the sine of x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 fastsin(float3 x) => fastsin(math.float4(x, 0f)).xyz;

        /// <summary>Returns a fast componentwise approximation of the sine of x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 fastsin(float4 x)
        {
            x = math.mad(x, k_OneOverTwoPI, -0.25f);
            x = 0.25f - math.abs(x - math.round(x));
            return polysincos(x);
        }

        /// <summary>Returns a fast approximation of the cosine of x.</summary>
        /// <remarks>The maximum absolute error is below 1e-5 for inputs with a magnitude up to 10 * PI, and grows with the magnitude of the input.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float fastcos(float x) => fastcos(math.float4(x)).x;

        /// <summary>Returns a fast componentwise approximation of the cosine of x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float2 fastcos(float2 x) => fastcos(math.float4(x, 0f, 0f)).xy;

        /// <summary>Returns a fast componentwise approximation of the cosine of x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 fastcos(float3 x) => fastcos(math.float4(x, 0f)).xyz;

        /// <summary>Returns a fast componentwise approximation of the cosine of x.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 fastcos(float4 x)
        {
            x *= k_OneOverTwoPI;
            x = 0.25f - math.abs(x - math.round(x));
            return polysincos(x);
        }

        // Odd polynomial approximation of sin(2 * PI * x) for x in [-0.25, 0.25].
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float4 polysincos(float4 x)
        {
            const float c0 = 6.283185005f;
            const float c1 = -41.341659546f;
            const float c2 = 81.601821899f;
            const float c3 = -76.568618774f;
            const float c4 = 39.657032013f;
            float4 x2 = x * x;
            return x * math.mad(x2, math.mad(x2, math.mad(x2, math.mad(x2, c4, c3), c2), c1), c0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion scaleMulQuat(float3 scale, quaternion q)
        {
            float3 s = math.chgsign(one(), scale);
            return math.chgsign(q.value, new float4(s.yxx * s.zzy, 0f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion add(quaternion p, quaternion q) =>
            p.value + (math.chgsign(q.value, math.dot(p.value, q.value)));

        // returns the weighted quaternion. This is very useful for additive blending.
        // var result = math.mul(basevalue, quaternionWeight(layerValue, 0.5F))
        // q must be normalized
        // returns a normalized quaternion
        // Not a slerp: scaling the vector part matches Mecanim's QuatWeight (vec-quat.h) bit for bit, which blending relies on for parity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion quatWeight(quaternion q, float weight) =>
            math.normalizesafe(math.quaternion(math.float4(q.value.xyz * weight, q.value.w)));

        internal static RigidTransform rigidPow(RigidTransform x, uint pow)
        {
            var ret = RigidTransform.identity;

            while (pow > 0)
            {
                if ((pow & 1) == 1)
                    ret = math.mul(ret, x);

                x = math.mul(x, x);
                pow >>= 1;
            }

            return ret;
        }

        /// <summary>Returns a quaternion that rotates the direction from onto the direction to.</summary>
        /// <remarks>
        /// The two input vectors are not assumed to be unit length, but must not be zero length, point in opposite
        /// directions, or be of such extreme magnitude that squaring their length overflows.
        /// If these assumptions are not met use <see cref="fromToRotationSafe"/> instead.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion fromToRotation(float3 from, float3 to)
        {
            from *= math.rsqrt(math.dot(from, from));
            to *= math.rsqrt(math.dot(to, to));

            return math.normalize(math.quaternion(math.float4(math.cross(from, to), 1f + math.dot(from, to))));
        }

        /// <summary>Returns a quaternion that rotates the direction from onto the direction to.</summary>
        /// <remarks>
        /// The two input vectors are not assumed to be unit length. If the magnitude of either of the vectors is so
        /// extreme that the calculation cannot be carried out reliably, the identity will be returned instead.
        /// If the vectors point in opposite directions, the result is a half turn around an axis perpendicular to from.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion fromToRotationSafe(float3 from, float3 to)
        {
            float fromLengthSq = math.dot(from, from);
            float toLengthSq = math.dot(to, to);

            from *= math.rsqrt(fromLengthSq);
            to *= math.rsqrt(toLengthSq);

            float cosAngle = math.dot(from, to);
            float4 q = math.float4(math.cross(from, to), 1f + cosAngle);

            // Opposite directions: the cross product vanishes, so build the half turn from an axis known to be perpendicular to from.
            float3 basis = math.select(math.float3(1f, 0f, 0f), math.float3(0f, 1f, 0f), math.abs(from.x) > math.abs(from.y));
            float4 halfTurn = math.float4(math.normalize(math.cross(from, basis)), 0f);
            q = math.select(q, halfTurn, cosAngle < -1f + k_OppositeDirectionsTolerance);

            float mn = math.min(fromLengthSq, toLengthSq);
            float mx = math.max(fromLengthSq, toLengthSq);

            bool accept = mn > 1e-35f && mx < 1e35f && math.isfinite(fromLengthSq) && math.isfinite(toLengthSq);
            return math.quaternion(math.select(quaternion.identity.value, math.normalize(q), accept));
        }

        /// <summary>Returns the inverse of a quaternion value, or the identity if the quaternion has zero length.</summary>
        /// <remarks>Unlike <see cref="Unity.Mathematics.math.inverse(Unity.Mathematics.quaternion)"/>, this function never divides by zero.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion inversesafe(quaternion q)
        {
            float4 x = q.value;
            float len = math.dot(x, x);
            return math.quaternion(math.select(quaternion.identity.value,
                math.rcp(len) * x * math.float4(-1f, -1f, -1f, 1f), len > math.FLT_MIN_NORMAL));
        }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static quaternion select(quaternion falseValue, quaternion trueValue, bool test) =>
            test ? trueValue : falseValue;

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RigidTransform select(RigidTransform falseValue, RigidTransform trueValue, bool test) =>
            test ? trueValue : falseValue;

        /// <summary>Returns the angle in radians that a unit quaternion rotates by.</summary>
        /// <remarks>The result is the angle of the axis-angle representation of q, in the range 0 to 2 * PI.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float angle(quaternion q) => 2f * math.acos(math.clamp(q.value.w, -1f, 1f));

        /// <summary>Returns the axis that a unit quaternion rotates around.</summary>
        /// <remarks>If q doesn't rotate, the axis is arbitrary and this function returns the x-axis.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 axis(quaternion q) => math.normalizesafe(q.value.xyz, math.float3(1f, 0f, 0f));

        /// <summary>Decomposes a unit quaternion into a swing rotation and a twist rotation around the given axis, so that q equals mul(swing, twist).</summary>
        /// <remarks>The twist is the part of q that rotates around twistAxis and the swing is the remaining rotation, whose axis is perpendicular to twistAxis.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void swingtwist(quaternion q, float3 twistAxis, out quaternion swing, out quaternion twist)
        {
            twistAxis = math.normalizesafe(twistAxis);
            twist = math.normalizesafe(math.quaternion(math.float4(math.dot(twistAxis, q.value.xyz) * twistAxis, q.value.w)), quaternion.identity);
            swing = math.mul(q, math.conjugate(twist));
        }

        /// <summary>Decomposes a unit quaternion into a twist rotation around the given axis and a swing rotation, so that q equals mul(twist, swing).</summary>
        /// <remarks>The twist is the part of q that rotates around twistAxis and the swing is the remaining rotation, whose axis is perpendicular to twistAxis.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void twistswing(quaternion q, float3 twistAxis, out quaternion twist, out quaternion swing)
        {
            twistAxis = math.normalizesafe(twistAxis);
            twist = math.normalizesafe(math.quaternion(math.float4(math.dot(twistAxis, q.value.xyz) * twistAxis, q.value.w)), quaternion.identity);
            swing = math.mul(math.conjugate(twist), q);
        }

        /// <summary>
        /// Creates a rotation which rotates from vectors A to B. Input vectors can be either normalized or not,
        /// function will return a normalized quaternion. Returns identity on null input vectors.
        /// An optional rotation hint axis will be used to compute a 180 rotation when input vectors are colinear but pointing in opposite directions.
        /// When not specified or null, identity will be returned.
        /// </summary>
        /// <param name="a">Vector A</param>
        /// <param name="b">Vector B</param>
        /// <param name="axisHint">Optional hint to specify which rotation axis to take when input vectors A and B are colinear but pointing in opposite directions.</param>
        /// <returns>The rotation which rotates vector A to vector B.</returns>
        internal static quaternion fromToSafe(float3 a, float3 b, float3 axisHint = default)
        {
            // If input vectors are opposite, return PI rotation using axisHint. When not specified identity rotation will be returned.
            // (fromToRotationSafe would pick an arbitrary perpendicular axis instead.)
            var aLenSq = math.lengthsq(a);
            var bLenSq = math.lengthsq(b);
            if (aLenSq < math.FLT_MIN_NORMAL || bLenSq < math.FLT_MIN_NORMAL)
                return quaternion.identity;

            if (math.dot(math.rsqrt(aLenSq) * a, math.rsqrt(bLenSq) * b) < -1f + k_OppositeDirectionsTolerance)
            {
                var hintLenSq = math.lengthsq(axisHint);
                return math.select(quaternion.AxisAngle(math.rsqrt(hintLenSq) * axisHint, math.PI).value, quaternion.identity.value, hintLenSq < math.FLT_MIN_NORMAL);
            }

            return fromToRotationSafe(a, b);
        }

        /// <summary>
        /// Returns the smallest signed angle between two input angles in degrees
        /// </summary>
        /// <param name="from">From angle in degrees</param>
        /// <param name="to">To angle in degrees</param>
        /// <returns>Signed angle in degrees</returns>
        internal static float deltaAngleDegrees(float from, float to)
        {
            // Normalize angles to [-360, 360] to avoid float imprecision and overflow
            to %= 360;
            from %= 360;
            var diff = (to - from + 180) % 360 - 180;
            return math.select(diff, diff + 360, diff < -180);
        }

        static float3 eulerReorder(float3 euler, math.RotationOrder order)
        {
            switch (order)
            {
                case math.RotationOrder.XZY:
                    return euler.xzy;
                case math.RotationOrder.YZX:
                    return euler.yzx;
                case math.RotationOrder.YXZ:
                    return euler.yxz;
                case math.RotationOrder.ZXY:
                    return euler.zxy;
                case math.RotationOrder.ZYX:
                    return euler.zyx;
                default:
                    return euler;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float3 eulerReorderBack(float3 euler, math.RotationOrder order)
        {
            switch (order)
            {
                case math.RotationOrder.XZY:
                    return euler.xzy;
                case math.RotationOrder.YZX:
                    return euler.zxy;
                case math.RotationOrder.YXZ:
                    return euler.yxz;
                case math.RotationOrder.ZXY:
                    return euler.yzx;
                case math.RotationOrder.ZYX:
                    return euler.zyx;
                case math.RotationOrder.XYZ:
                default:
                    return euler;
            }
        }

        internal static float3 alternateEuler(float3 euler, math.RotationOrder order = math.RotationOrder.Default)
        {
            float3 eulerAlt = eulerReorder(euler, order);
            //avoid incorrect results when yaw gets close to a 90degree congruence
            if (math.abs(eulerAlt.y % math.PIHALF) < 1e-4)
                return euler;

            if(eulerAlt.y is < math.PIHALF and > -math.PIHALF)
            {
                eulerAlt = math.chgsign(eulerAlt, math.float3(1, -1, 1));
                eulerAlt += math.PI;
            }
            else
            {
                eulerAlt += math.PI;
                eulerAlt = math.chgsign(eulerAlt, math.float3(1, -1, 1));
            }
            return eulerReorderBack(eulerAlt, order);
        }

        static float3 syncEuler(float3 euler, float3 eulerHint)
        {
            return euler + math.round((eulerHint - euler) * k_OneOverTwoPI) * math.PI2;
        }

        /// <summary>
        /// Calculates the closest euler value to a given quaternion based on a euler hint value.
        /// If the euler hint value matches its quaternion counterpart, then the euler hint
        /// will be used. If not, the euler hint is discarded, and the quaternion is converted
        /// to euler.
        /// </summary>
        /// <param name="q">Quaternion input</param>
        /// <param name="eulerHint">Euler hint</param>
        /// <param name="order">Rotation order</param>
        /// <returns>Euler result</returns>
        internal static float3 closestEuler(quaternion q, float3 eulerHint, math.RotationOrder order = math.RotationOrder.Default)
        {
            float3 euler = math.Euler(q, order);

            var qHint = quaternion.Euler(eulerHint, order);
            var angleDiff = math.degrees(math.angle(q, qHint));

            return math.select(closestEuler(euler, eulerHint, order), eulerHint, angleDiff < 1e-3f);
        }

        /// <summary>
        /// Synchronizes the euler value with the specified euler hint. Either picks the
        /// current euler value or its alternate equivalent counterpart depending on the value
        /// closest to the euler hint.
        /// </summary>
        /// <param name="euler">Euler input</param>
        /// <param name="eulerHint">Euler hint</param>
        /// <param name="order">Rotation order</param>
        /// <returns>Euler result</returns>
        internal static float3 closestEuler(float3 euler, float3 eulerHint, math.RotationOrder order = math.RotationOrder.Default)
        {
            float3 eulerSynced = syncEuler(euler, eulerHint);
            float3 altEulerSynced = syncEuler(alternateEuler(euler, order), eulerHint);

            float3 diff = eulerSynced - eulerHint;
            float3 altDiff = altEulerSynced - eulerHint;

            return math.select(altEulerSynced, eulerSynced, math.dot(diff, diff) <= math.dot(altDiff, altDiff));
        }

        /// <summary>
        /// Extracts the scale from a matrix given its rotation.
        /// </summary>
        /// <param name="r">The rotation</param>
        /// <param name="rs">The matrix containing the rotation and scale.</param>
        /// <returns>The extracted scale.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 extractScale(in quaternion r, in float3x3 rs)
        {
            float3x3 scaleMat = math.mul(math.float3x3(math.conjugate(r)), rs);
            return math.float3(scaleMat.c0.x, scaleMat.c1.y, scaleMat.c2.z);
        }

        /// <summary>Returns the rotation axis scaled by the rotation angle in radians, using the shortest arc.</summary>
        /// <remarks>Matches math::quatToAngularDisplacement in Mecanim, except that it always takes the shortest arc.</remarks>
        internal static float3 toAngularDisplacement(quaternion rotation)
        {
            var q = math.normalize(rotation).value;
            q = math.select(q, -q, q.w < 0f);
            var length = math.length(q.xyz);
            if (length == 0f)
                return float3.zero;
            return q.xyz * (2f * math.asin(math.min(length, 1f)) / length);
        }
    }
}
