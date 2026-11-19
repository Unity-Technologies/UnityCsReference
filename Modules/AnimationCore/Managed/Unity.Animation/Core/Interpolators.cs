// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animations
{
    /// <summary>Easing functions that remap a value between 0 and 1 along a curve.</summary>
    /// <remarks>
    /// An easing function remaps an input value from 0 to 1 to an output value from 0 to 1, and its curve controls how
    /// fast the output changes. Each function maps the input 0 to the output 0, and the input 1 to the output 1.
    ///
    /// Ease-in functions, such as <see cref="QuadraticIn"/>, start slowly and accelerate. Ease-out functions, such as
    /// <see cref="QuadraticOut"/>, start quickly and slow down. Ease-in-out functions, such as
    /// <see cref="QuadraticInOut"/>, start and end slowly. The sine-based functions, <see cref="SineIn"/>,
    /// <see cref="SineOut"/>, and <see cref="SineInOut"/>, use a fast approximation of the sine and cosine functions.
    ///
    /// Call <see cref="Evaluate"/> to choose the function with a <see cref="FunctionType"/> value. `Evaluate` clamps
    /// its input to the range 0 to 1. The individual functions don't clamp their input, and some are only valid for a
    /// smaller or larger range, which their remarks list.
    /// </remarks>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public static class Interpolator
    {
        // All functions in this file have been modeled and validated using desmos:
        // https://www.desmos.com/calculator/7kmsmih2zg

        static readonly float4 k_QuadraticInOutCoeff = math.float4(-2f, 4f, -1f, 0f);
        static readonly float4 k_CubicOutCoeff       = math.float4(1f, -3f, 3f, 0f);
        static readonly float4 k_CubicInOutCoeff     = math.float4(4f, -12f, 12f, -3f);
        static readonly float4 k_QuarticOutCoeff     = math.float4(-1f, 4f, -6f, 4f);
        static readonly float4 k_QuarticInOutCoeff   = math.float4(32f, -48f, 32f, -7f);
        static readonly float4 k_QuinticOutCoeff     = math.float4(-5f, 10f, -10f, 5f);
        static readonly float4 k_QuinticInOutCoeff   = math.float4(-80f, 160f, -160f, 80f);

        /// <summary>
        /// Options for the easing curve that is applied to a value.
        /// </summary>
        /// <remarks>
        /// <see cref="Evaluate"/> applies the selected curve to a value. In the following formulas, x is the input value from 0 to 1 and y is the output value.
        ///
        /// - <see cref="SmoothStep"/>: `y = 3x^2 - 2x^3`
        /// - <see cref="QuadraticIn"/>: `y = x^2`. <see cref="QuadraticOut"/>: `y = 1 - (1 - x)^2`.
        /// - <see cref="CubicIn"/>: `y = x^3`. <see cref="CubicOut"/>: `y = 1 - (1 - x)^3`.
        /// - <see cref="QuarticIn"/>: `y = x^4`. <see cref="QuarticOut"/>: `y = 1 - (1 - x)^4`.
        /// - <see cref="QuinticIn"/>: `y = x^5`. <see cref="QuinticOut"/>: `y = 1 - (1 - x)^5`.
        /// - <see cref="ExponentialIn"/>: `y = 2^(10(x - 1))` for x greater than 0, and 0 when x is 0.
        /// <see cref="ExponentialOut"/>: `y = 1 - 2^(-10x)` for x less than 1, and 1 when x is 1.
        ///
        /// The quadratic, cubic, quartic, quintic, and exponential in-out functions apply the in curve to the first half
        /// of the range and the out curve to the second half, each scaled to half the range.
        /// </remarks>
        public enum FunctionType : byte
        {
            /// <summary>Constant rate of change. The output is equal to the input.</summary>
            Linear,
            /// <summary>Starts and ends slowly, following the smoothstep curve.</summary>
            SmoothStep,
            /// <summary>Starts slowly and accelerates, following a parabola.</summary>
            QuadraticIn,
            /// <summary>Starts quickly and slows down, following a parabola.</summary>
            QuadraticOut,
            /// <summary>Starts and ends slowly, using two parabolas.</summary>
            QuadraticInOut,
            /// <summary>Starts slowly and accelerates, following a cubic curve.</summary>
            CubicIn,
            /// <summary>Starts quickly and slows down, following a cubic curve.</summary>
            CubicOut,
            /// <summary>Starts and ends slowly, using two cubics.</summary>
            CubicInOut,
            /// <summary>Starts slowly and accelerates, following a quartic curve.</summary>
            QuarticIn,
            /// <summary>Starts quickly and slows down, following a quartic curve.</summary>
            QuarticOut,
            /// <summary>Starts and ends slowly, using two quartics.</summary>
            QuarticInOut,
            /// <summary>Starts slowly and accelerates, following a quintic curve.</summary>
            QuinticIn,
            /// <summary>Starts quickly and slows down, following a quintic curve.</summary>
            QuinticOut,
            /// <summary>Starts and ends slowly, using two quintics.</summary>
            QuinticInOut,
            /// <summary>Starts slowly and accelerates, following a quarter of a circle.</summary>
            CircularIn,
            /// <summary>Starts quickly and slows down, following a quarter of a circle.</summary>
            CircularOut,
            /// <summary>Starts and ends slowly, using two quarter circles.</summary>
            CircularInOut,
            /// <summary>Starts slowly and accelerates, following an exponential curve.</summary>
            ExponentialIn,
            /// <summary>Starts quickly and slows down, following an exponential curve.</summary>
            ExponentialOut,
            /// <summary>Starts and ends slowly, using two exponentials.</summary>
            ExponentialInOut,
            /// <summary>Starts slowly and accelerates, following a quarter cycle of a sine wave.</summary>
            SineIn,
            /// <summary>Starts quickly and slows down, following a quarter cycle of a sine wave.</summary>
            SineOut,
            /// <summary>Starts and ends slowly, following half a cycle of a sine wave.</summary>
            SineInOut
        }

        /// <summary>
        /// Interpolates value of x based on specified interpolation function type.
        /// </summary>
        /// <param name="func">Interpolation function type.</param>
        /// <param name="x">Value to interpolate. If x is outside of [0, 1] range, it will be clamped to [0, 1].</param>
        /// <returns>Interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Evaluate(FunctionType func, float x)
        {
            x = math.saturate(x);
            switch (func)
            {
                case FunctionType.Linear:           return Linear(x);
                case FunctionType.SmoothStep:       return SmoothStep(x);
                case FunctionType.QuadraticIn:      return QuadraticIn(x);
                case FunctionType.QuadraticOut:     return QuadraticOut(x);
                case FunctionType.QuadraticInOut:   return QuadraticInOut(x);
                case FunctionType.CubicIn:          return CubicIn(x);
                case FunctionType.CubicOut:         return CubicOut(x);
                case FunctionType.CubicInOut:       return CubicInOut(x);
                case FunctionType.QuarticIn:        return QuarticIn(x);
                case FunctionType.QuarticOut:       return QuarticOut(x);
                case FunctionType.QuarticInOut:     return QuarticInOut(x);
                case FunctionType.QuinticIn:        return QuinticIn(x);
                case FunctionType.QuinticOut:       return QuinticOut(x);
                case FunctionType.QuinticInOut:     return QuinticInOut(x);
                case FunctionType.CircularIn:       return CircularIn(x);
                case FunctionType.CircularOut:      return CircularOut(x);
                case FunctionType.CircularInOut:    return CircularInOut(x);
                case FunctionType.ExponentialIn:    return ExponentialIn(x);
                case FunctionType.ExponentialOut:   return ExponentialOut(x);
                case FunctionType.ExponentialInOut: return ExponentialInOut(x);
                case FunctionType.SineIn:           return SineIn(x);
                case FunctionType.SineOut:          return SineOut(x);
                case FunctionType.SineInOut:        return SineInOut(x);

                default:
                    return Linear(x);
            }
        }

        /// <summary>
        /// Interpolation function modeled after the line y = x.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Linear(float x) =>
            x;

        /// <summary>
        /// Interpolation function modeled after the smoothstep curve y = 3x^2 - 2x^3.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothStep(float x) =>
            math.smoothstep(0f, 1f, x);

        /// <summary>
        /// Interpolation function modeled after the parabola y = x^2.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuadraticIn(float x) =>
            x * x;

        /// <summary>
        /// Interpolation function modeled after the parabola y = 1 - (1 - x)^2.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuadraticOut(float x) =>
            x * (2f - x);


        /// <summary>
        /// Interpolation function modeled after the piecewise quadratic:
        /// y = (1/2)((2x)^2)               : [0, 0.5]
        /// y = (1/2)(1 + (1-(1-(2x-1))^2)) : (0.5, 1]
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuadraticInOut(float x)
        {
            var x2 = x * x;
            return math.select(2f * x2, math.dot(k_QuadraticInOutCoeff, math.float4(x2, x, 1f, 0f)), x > 0.5f);
        }

        /// <summary>
        /// Interpolation function modeled after the cubic y = x^3.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CubicIn(float x) =>
            x * x * x;

        /// <summary>
        /// Interpolation function modeled after the cubic y = 1 + (x - 1)^3.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CubicOut(float x)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            return math.dot(k_CubicOutCoeff, math.float4(x3, x2, x, 0f));
        }

        /// <summary>
        /// Interpolation function modeled after the piecewise cubic:
        /// y = (1/2)((2x)^3)               : [0, 0.5]
        /// y = (1/2)(1 + (1-(1-(2x-1))^3)) : (0.5, 1]
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CubicInOut(float x)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            return math.select(4f * x3, math.dot(k_CubicInOutCoeff, math.float4(x3, x2, x, 1f)), x > 0.5f);
        }

        /// <summary>
        /// Interpolation function modeled after the quartic y = x^4.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuarticIn(float x)
        {
            var x2 = x * x;
            return x2 * x2;
        }

        /// <summary>
        /// Interpolation function modeled after the quartic y = 1 - (x - 1)^4.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuarticOut(float x)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var x4 = x3 * x;
            return math.dot(k_QuarticOutCoeff, math.float4(x4, x3, x2, x));
        }

        /// <summary>
        /// Interpolation function modeled after the piecewise quartic:
        /// y = (1/2)((2x)^4)               : [0, 0.5]
        /// y = (1/2)(1 + (1-(1-(2x-1))^4)) : (0.5, 1]
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuarticInOut(float x)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var tmp = 8f * x3 * x;
            return math.select(tmp, -tmp + math.dot(k_QuarticInOutCoeff, math.float4(x3, x2, x, 1f)), x > 0.5f);
        }

        /// <summary>
        /// Interpolation function modeled after the quintic y = x^5.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuinticIn(float x)
        {
            var x2 = x * x;
            return x * x2 * x2;
        }

        /// <summary>
        /// Interpolation function modeled after the quintic y = 1 + (x - 1)^5.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuinticOut(float x)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var x4 = x3 * x;
            return x4 * x + math.dot(k_QuinticOutCoeff, math.float4(x4, x3, x2, x));
        }

        /// <summary>
        /// Interpolation function modeled after the piecewise quintic:
        /// y = (1/2)((2x)^5)               : [0, 0.5]
        /// y = (1/2)(1 + (1-(1-(2x-1))^5)) : (0.5, 1]
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float QuinticInOut(float x)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var x4 = x3 * x;
            var tmp = 16f * x4 * x;
            return math.select(tmp, tmp + math.dot(k_QuinticInOutCoeff, math.float4(x4, x3, x2, x)) - 15f, x > 0.5f);
        }

        /// <summary>
        /// Interpolation function modeled after shifting quadrant IV of a unit circle.
        /// </summary>
        /// <remarks>Only valid for x values in range [-1, 1].</remarks>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CircularIn(float x) =>
            1f - math.sqrt(1f - x * x);

        /// <summary>
        /// Interpolation function modeled after shifting quadrant II of a unit circle.
        /// </summary>
        /// <remarks>Only valid for x values in range [0, 2].</remarks>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CircularOut(float x) =>
            math.sqrt(x * (2f - x));

        /// <summary>
        /// Interpolation function modeled after the piecewise circular function:
        /// y = (1/2)(1 - sqrt(1 - 4x^2))           : [0, 0.5]
        /// y = (1/2)(sqrt(-(2x - 3)*(2x - 1)) + 1) : (0.5, 1]
        /// </summary>
        /// <remarks>Only valid for x values in range [-0.5, 1.5].</remarks>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CircularInOut(float x)
        {
            if (x > 0.5f)
            {
                var tmp = 2f * x;
                return 0.5f * (math.sqrt(-(tmp - 3f) * (tmp - 1f)) + 1f);
            }

            return 0.5f * (1f - math.sqrt(1f - 4f * (x * x)));
        }

        /// <summary>
        /// Interpolation function modeled after the exponential y = 2^(10(x - 1)).
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ExponentialIn(float x) =>
            x > 0f ? math.pow(2f, 10f * (x - 1f)) : 0f;

        /// <summary>
        /// Interpolation function modeled after the exponential y = 1 - 2^(-10x).
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ExponentialOut(float x) =>
            x < 1f ? 1f - math.pow(2f, -10f * x) : 1f;

        /// <summary>
        /// Interpolation function modeled after the piecewise exponential
        /// y = (1/2)2^(10(2x - 1))         : [0,0.5]
        /// y = -(1/2)*2^(-10(2x - 1))) + 1 : (0.5,1]
        /// </summary>
        /// <remarks>Only valid for x values in range [-0.5, 1.5].</remarks>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ExponentialInOut(float x)
        {
            if (x > 0.5f)
            {
                return 0.5f * ExponentialOut(2f * x - 1f) + 0.5f;
            }

            return 0.5f * ExponentialIn(2f * x);
        }

        /// <summary>
        /// Interpolation function modeled after the quarter-cycle of a shifted sine wave.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SineIn(float x) =>
            1f + MathUtility.fastsin((x - 1f) * math.PIHALF);

        /// <summary>
        /// Interpolation function modeled after the quarter-cycle of a shifted sine wave.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SineOut(float x) =>
            MathUtility.fastsin(x * math.PIHALF);

        /// <summary>
        /// Interpolation function modeled after half sine wave.
        /// </summary>
        /// <param name="x">Value to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SineInOut(float x) =>
            0.5f * (1f - MathUtility.fastcos(x * math.PI));
    }
}
