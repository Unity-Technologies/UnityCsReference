// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Mathematics
{
    /// <summary>
    /// A static class to contain various math functions and constants.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public static partial class math
    {
        /// <summary>Extrinsic rotation order. Specifies in which order rotations around the principal axes (x, y and z) are to be applied.</summary>
        public enum RotationOrder : byte
        {
            /// <summary>Extrinsic rotation around the x axis, then around the y axis and finally around the z axis.</summary>
            XYZ,
            /// <summary>Extrinsic rotation around the x axis, then around the z axis and finally around the y axis.</summary>
            XZY,
            /// <summary>Extrinsic rotation around the y axis, then around the x axis and finally around the z axis.</summary>
            YXZ,
            /// <summary>Extrinsic rotation around the y axis, then around the z axis and finally around the x axis.</summary>
            YZX,
            /// <summary>Extrinsic rotation around the z axis, then around the x axis and finally around the y axis.</summary>
            ZXY,
            /// <summary>Extrinsic rotation around the z axis, then around the y axis and finally around the x axis.</summary>
            ZYX,
            /// <summary>Unity default rotation order. Extrinsic Rotation around the z axis, then around the x axis and finally around the y axis.</summary>
            Default = ZXY
        };

        /// <summary>Specifies a shuffle component.</summary>
        public enum ShuffleComponent : byte
        {
            /// <summary>Specified the x component of the left vector.</summary>
            LeftX,
            /// <summary>Specified the y component of the left vector.</summary>
            LeftY,
            /// <summary>Specified the z component of the left vector.</summary>
            LeftZ,
            /// <summary>Specified the w component of the left vector.</summary>
            LeftW,

            /// <summary>Specified the x component of the right vector.</summary>
            RightX,
            /// <summary>Specified the y component of the right vector.</summary>
            RightY,
            /// <summary>Specified the z component of the right vector.</summary>
            RightZ,
            /// <summary>Specified the w component of the right vector.</summary>
            RightW
        };

        /// <summary>The mathematical constant e also known as Euler's number. Approximately 2.72. This is a f64/double precision constant.</summary>
        public const double E_DBL = 2.71828182845904523536;

        /// <summary>The base 2 logarithm of e. Approximately 1.44. This is a f64/double precision constant.</summary>
        public const double LOG2E_DBL = 1.44269504088896340736;

        /// <summary>The base 10 logarithm of e. Approximately 0.43. This is a f64/double precision constant.</summary>
        public const double LOG10E_DBL = 0.434294481903251827651;

        /// <summary>The natural logarithm of 2. Approximately 0.69. This is a f64/double precision constant.</summary>
        public const double LN2_DBL = 0.693147180559945309417;

        /// <summary>The natural logarithm of 10. Approximately 2.30. This is a f64/double precision constant.</summary>
        public const double LN10_DBL = 2.30258509299404568402;

        /// <summary>The mathematical constant pi. Approximately 3.14. This is a f64/double precision constant.</summary>
        public const double PI_DBL = 3.14159265358979323846;

        /// <summary>
        /// The mathematical constant (2 * pi). Approximately 6.28. This is a f64/double precision constant. Also known as <see cref="TAU_DBL"/>.
        /// </summary>
        public const double PI2_DBL = PI_DBL * 2.0;

        /// <summary>
        /// The mathematical constant (pi / 2). Approximately 1.57. This is a f64/double precision constant.
        /// </summary>
        public const double PIHALF_DBL = PI_DBL * 0.5;

        /// <summary>
        /// The mathematical constant tau. Approximately 6.28. This is a f64/double precision constant. Also known as <see cref="PI2_DBL"/>.
        /// </summary>
        public const double TAU_DBL = PI2_DBL;

        /// <summary>
        /// The conversion constant used to convert radians to degrees. Multiply the radian value by this constant to get degrees.
        /// </summary>
        /// <remarks>Multiplying by this constant is equivalent to using <see cref="math.degrees(double)"/>.</remarks>
        public const double TODEGREES_DBL = 57.29577951308232;

        /// <summary>
        /// The conversion constant used to convert degrees to radians. Multiply the degree value by this constant to get radians.
        /// </summary>
        /// <remarks>Multiplying by this constant is equivalent to using <see cref="math.radians(double)"/>.</remarks>
        public const double TORADIANS_DBL = 0.017453292519943296;

        /// <summary>The square root 2. Approximately 1.41. This is a f64/double precision constant.</summary>
        public const double SQRT2_DBL = 1.41421356237309504880;

        /// <summary>
        /// The difference between 1.0 and the next representable f64/double precision number.
        ///
        /// Beware:
        /// This value is different from System.Double.Epsilon, which is the smallest, positive, denormalized f64/double.
        /// </summary>
        public const double EPSILON_DBL = 2.22044604925031308085e-16;

        /// <summary>
        /// Double precision constant for positive infinity.
        /// </summary>
        public const double INFINITY_DBL = Double.PositiveInfinity;

        /// <summary>
        /// Double precision constant for Not a Number.
        ///
        /// NAN_DBL is considered unordered, which means all comparisons involving it are false except for not equal (operator !=).
        /// As a consequence, NAN_DBL == NAN_DBL is false but NAN_DBL != NAN_DBL is true.
        ///
        /// Additionally, there are multiple bit representations for Not a Number, so if you must test if your value
        /// is NAN_DBL, use isnan().
        /// </summary>
        public const double NAN_DBL = Double.NaN;

        /// <summary>The smallest positive normal number representable in a float.</summary>
        public const float FLT_MIN_NORMAL = 1.175494351e-38F;

        /// <summary>The smallest positive normal number representable in a double. This is a f64/double precision constant.</summary>
        public const double DBL_MIN_NORMAL = 2.2250738585072014e-308;

        /// <summary>The mathematical constant e also known as Euler's number. Approximately 2.72.</summary>
        public const float E = (float)E_DBL;

        /// <summary>The base 2 logarithm of e. Approximately 1.44.</summary>
        public const float LOG2E = (float)LOG2E_DBL;

        /// <summary>The base 10 logarithm of e. Approximately 0.43.</summary>
        public const float LOG10E = (float)LOG10E_DBL;

        /// <summary>The natural logarithm of 2. Approximately 0.69.</summary>
        public const float LN2 = (float)LN2_DBL;

        /// <summary>The natural logarithm of 10. Approximately 2.30.</summary>
        public const float LN10 = (float)LN10_DBL;

        /// <summary>The mathematical constant pi. Approximately 3.14.</summary>
        public const float PI = (float)PI_DBL;

        /// <summary>
        /// The mathematical constant (2 * pi). Approximately 6.28. Also known as <see cref="TAU"/>.
        /// </summary>
        public const float PI2 = (float)PI2_DBL;

        /// <summary>
        /// The mathematical constant (pi / 2). Approximately 1.57.
        /// </summary>
        public const float PIHALF = (float)PIHALF_DBL;

        /// <summary>
        /// The mathematical constant tau. Approximately 6.28. Also known as <see cref="PI2"/>.
        /// </summary>
        public const float TAU = (float)PI2_DBL;

        /// <summary>
        /// The conversion constant used to convert radians to degrees. Multiply the radian value by this constant to get degrees.
        /// </summary>
        /// <remarks>Multiplying by this constant is equivalent to using <see cref="math.degrees(float)"/>.</remarks>
        public const float TODEGREES = (float)TODEGREES_DBL;

        /// <summary>
        /// The conversion constant used to convert degrees to radians. Multiply the degree value by this constant to get radians.
        /// </summary>
        /// <remarks>Multiplying by this constant is equivalent to using <see cref="math.radians(float)"/>.</remarks>
        public const float TORADIANS = (float)TORADIANS_DBL;

        /// <summary>The square root 2. Approximately 1.41.</summary>
        public const float SQRT2 = (float)SQRT2_DBL;

        /// <summary>
        /// The difference between 1.0f and the next representable f32/single precision number.
        ///
        /// Beware:
        /// This value is different from System.Single.Epsilon, which is the smallest, positive, denormalized f32/single.
        /// </summary>
        public const float EPSILON = 1.1920928955078125e-7f;

        /// <summary>
        /// Single precision constant for positive infinity.
        /// </summary>
        public const float INFINITY = Single.PositiveInfinity;

        /// <summary>
        /// Single precision constant for Not a Number.
        ///
        /// NAN is considered unordered, which means all comparisons involving it are false except for not equal (operator !=).
        /// As a consequence, NAN == NAN is false but NAN != NAN is true.
        ///
        /// Additionally, there are multiple bit representations for Not a Number, so if you must test if your value
        /// is NAN, use isnan().
        /// </summary>
        public const float NAN = Single.NaN;

        /// <summary>Returns the bit pattern of a uint as an int.</summary>
        /// <param name="x">The uint bits to copy.</param>
        /// <returns>The int with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int asint(uint x)
        {
            unsafe
            {
                return *(int*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint2 as an int2.</summary>
        /// <param name="x">The uint2 bits to copy.</param>
        /// <returns>The int2 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 asint(uint2 x)
        {
            unsafe
            {
                return *(int2*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint3 as an int3.</summary>
        /// <param name="x">The uint3 bits to copy.</param>
        /// <returns>The int3 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 asint(uint3 x)
        {
            unsafe
            {
                return *(int3*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint4 as an int4.</summary>
        /// <param name="x">The uint4 bits to copy.</param>
        /// <returns>The int4 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 asint(uint4 x)
        {
            unsafe
            {
                return *(int4*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float as an int.</summary>
        /// <param name="x">The float bits to copy.</param>
        /// <returns>The int with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int asint(float x)
        {
            unsafe
            {
                return *(int*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float2 as an int2.</summary>
        /// <param name="x">The float2 bits to copy.</param>
        /// <returns>The int2 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 asint(float2 x)
        {
            unsafe
            {
                return *(int2*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float3 as an int3.</summary>
        /// <param name="x">The float3 bits to copy.</param>
        /// <returns>The int3 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 asint(float3 x)
        {
            unsafe
            {
                return *(int3*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float4 as an int4.</summary>
        /// <param name="x">The float4 bits to copy.</param>
        /// <returns>The int4 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 asint(float4 x)
        {
            unsafe
            {
                return *(int4*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int as a uint.</summary>
        /// <param name="x">The int bits to copy.</param>
        /// <returns>The uint with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint asuint(int x) { return (uint)x; }

        /// <summary>Returns the bit pattern of an int2 as a uint2.</summary>
        /// <param name="x">The int2 bits to copy.</param>
        /// <returns>The uint2 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 asuint(int2 x)
        {
            unsafe
            {
                return *(uint2*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int3 as a uint3.</summary>
        /// <param name="x">The int3 bits to copy.</param>
        /// <returns>The uint3 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 asuint(int3 x)
        {
            unsafe
            {
                return *(uint3*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int4 as a uint4.</summary>
        /// <param name="x">The int4 bits to copy.</param>
        /// <returns>The uint4 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 asuint(int4 x)
        {
            unsafe
            {
                return *(uint4*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float as a uint.</summary>
        /// <param name="x">The float bits to copy.</param>
        /// <returns>The uint with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint asuint(float x)
        {
            unsafe
            {
                return *(uint*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float2 as a uint2.</summary>
        /// <param name="x">The float2 bits to copy.</param>
        /// <returns>The uint2 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 asuint(float2 x)
        {
            unsafe
            {
                return *(uint2*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float3 as a uint3.</summary>
        /// <param name="x">The float3 bits to copy.</param>
        /// <returns>The uint3 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 asuint(float3 x)
        {
            unsafe
            {
                return *(uint3*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a float4 as a uint4.</summary>
        /// <param name="x">The float4 bits to copy.</param>
        /// <returns>The uint4 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 asuint(float4 x)
        {
            unsafe
            {
                return *(uint4*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a ulong as a long.</summary>
        /// <param name="x">The ulong bits to copy.</param>
        /// <returns>The long with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long aslong(ulong x) { return (long)x; }

        /// <summary>Returns the bit pattern of a double as a long.</summary>
        /// <param name="x">The double bits to copy.</param>
        /// <returns>The long with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long aslong(double x)
        {
            unsafe
            {
                return *(long*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a long as a ulong.</summary>
        /// <param name="x">The long bits to copy.</param>
        /// <returns>The ulong with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong asulong(long x) { return (ulong)x; }

        /// <summary>Returns the bit pattern of a double as a ulong.</summary>
        /// <param name="x">The double bits to copy.</param>
        /// <returns>The ulong with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong asulong(double x)
        {
            unsafe
            {
                return *(ulong*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int as a float.</summary>
        /// <param name="x">The int bits to copy.</param>
        /// <returns>The float with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float asfloat(int x)
        {
            unsafe
            {
                return *(float*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int2 as a float2.</summary>
        /// <param name="x">The int2 bits to copy.</param>
        /// <returns>The float2 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 asfloat(int2 x)
        {
            unsafe
            {
                return *(float2*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int3 as a float3.</summary>
        /// <param name="x">The int3 bits to copy.</param>
        /// <returns>The float3 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asfloat(int3 x)
        {
            unsafe
            {
                return *(float3*)&x;
            }
        }

        /// <summary>Returns the bit pattern of an int4 as a float4.</summary>
        /// <param name="x">The int4 bits to copy.</param>
        /// <returns>The float4 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat(int4 x)
        {
            unsafe
            {
                return *(float4*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint as a float.</summary>
        /// <param name="x">The uint bits to copy.</param>
        /// <returns>The float with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float asfloat(uint x)
        {
            unsafe
            {
                return *(float*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint2 as a float2.</summary>
        /// <param name="x">The uint2 bits to copy.</param>
        /// <returns>The float2 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 asfloat(uint2 x)
        {
            unsafe
            {
                return *(float2*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint3 as a float3.</summary>
        /// <param name="x">The uint3 bits to copy.</param>
        /// <returns>The float3 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asfloat(uint3 x)
        {
            unsafe
            {
                return *(float3*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a uint4 as a float4.</summary>
        /// <param name="x">The uint4 bits to copy.</param>
        /// <returns>The float4 with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asfloat(uint4 x)
        {
            unsafe
            {
                return *(float4*)&x;
            }
        }

        /// <summary>
        /// Returns a bitmask representation of a bool4. Storing one 1 bit per component
        /// in LSB order, from lower to higher bits (so 4 bits in total).
        /// The component x is stored at bit 0,
        /// The component y is stored at bit 1,
        /// The component z is stored at bit 2,
        /// The component w is stored at bit 3
        /// The bool4(x = true, y = true, z = false, w = true) would produce the value 1011 = 0xB
        /// </summary>
        /// <param name="value">The input bool4 to calculate the bitmask for</param>
        /// <returns>A bitmask representation of the bool4, in LSB order</returns>
        public static int bitmask(bool4 value)
        {
            int mask = 0;
            if (value.x) mask |= 0x01;
            if (value.y) mask |= 0x02;
            if (value.z) mask |= 0x04;
            if (value.w) mask |= 0x08;
            return mask;
        }

        /// <summary>Returns the bit pattern of a long as a double.</summary>
        /// <param name="x">The long bits to copy.</param>
        /// <returns>The double with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double asdouble(long x)
        {
            unsafe
            {
                return *(double*)&x;
            }
        }

        /// <summary>Returns the bit pattern of a ulong as a double.</summary>
        /// <param name="x">The ulong bits to copy.</param>
        /// <returns>The double with the same bit pattern as the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double asdouble(ulong x)
        {
            unsafe
            {
                return *(double*)&x;
            }
        }

        /// <summary>Returns true if the input float is a finite floating point value, false otherwise.</summary>
        /// <param name="x">The float value to test.</param>
        /// <returns>True if the float is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isfinite(float x) { return abs(x) < float.PositiveInfinity; }

        /// <summary>Returns a bool2 indicating for each component of a float2 whether it is a finite floating point value.</summary>
        /// <param name="x">The float2 value to test.</param>
        /// <returns>A bool2 where it is true in a component if that component is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 isfinite(float2 x) { return abs(x) < float.PositiveInfinity; }

        /// <summary>Returns a bool3 indicating for each component of a float3 whether it is a finite floating point value.</summary>
        /// <param name="x">The float3 value to test.</param>
        /// <returns>A bool3 where it is true in a component if that component is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 isfinite(float3 x) { return abs(x) < float.PositiveInfinity; }

        /// <summary>Returns a bool4 indicating for each component of a float4 whether it is a finite floating point value.</summary>
        /// <param name="x">The float4 value to test.</param>
        /// <returns>A bool4 where it is true in a component if that component is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 isfinite(float4 x) { return abs(x) < float.PositiveInfinity; }


        /// <summary>Returns true if the input double is a finite floating point value, false otherwise.</summary>
        /// <param name="x">The double value to test.</param>
        /// <returns>True if the double is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isfinite(double x) { return abs(x) < double.PositiveInfinity; }

        /// <summary>Returns a bool2 indicating for each component of a double2 whether it is a finite floating point value.</summary>
        /// <param name="x">The double2 value to test.</param>
        /// <returns>A bool2 where it is true in a component if that component is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 isfinite(double2 x) { return abs(x) < double.PositiveInfinity; }

        /// <summary>Returns a bool3 indicating for each component of a double3 whether it is a finite floating point value.</summary>
        /// <param name="x">The double3 value to test.</param>
        /// <returns>A bool3 where it is true in a component if that component is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 isfinite(double3 x) { return abs(x) < double.PositiveInfinity; }

        /// <summary>Returns a bool4 indicating for each component of a double4 whether it is a finite floating point value.</summary>
        /// <param name="x">The double4 value to test.</param>
        /// <returns>A bool4 where it is true in a component if that component is finite, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 isfinite(double4 x) { return abs(x) < double.PositiveInfinity; }


        /// <summary>Returns true if the input float is an infinite floating point value, false otherwise.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the input was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isinf(float x) { return abs(x) == float.PositiveInfinity; }

        /// <summary>Returns a bool2 indicating for each component of a float2 whether it is an infinite floating point value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 isinf(float2 x) { return abs(x) == float.PositiveInfinity; }

        /// <summary>Returns a bool3 indicating for each component of a float3 whether it is an infinite floating point value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 isinf(float3 x) { return abs(x) == float.PositiveInfinity; }

        /// <summary>Returns a bool4 indicating for each component of a float4 whether it is an infinite floating point value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 isinf(float4 x) { return abs(x) == float.PositiveInfinity; }

        /// <summary>Returns true if the input double is an infinite floating point value, false otherwise.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the input was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isinf(double x) { return abs(x) == double.PositiveInfinity; }

        /// <summary>Returns a bool2 indicating for each component of a double2 whether it is an infinite floating point value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 isinf(double2 x) { return abs(x) == double.PositiveInfinity; }

        /// <summary>Returns a bool3 indicating for each component of a double3 whether it is an infinite floating point value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 isinf(double3 x) { return abs(x) == double.PositiveInfinity; }

        /// <summary>Returns a bool4 indicating for each component of a double4 whether it is an infinite floating point value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was an infinite value; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 isinf(double4 x) { return abs(x) == double.PositiveInfinity; }


        /// <summary>Returns true if the input float is a NaN (not a number) floating point value, false otherwise.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the value was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isnan(float x) { return (asuint(x) & 0x7FFFFFFF) > 0x7F800000; }

        /// <summary>Returns a bool2 indicating for each component of a float2 whether it is a NaN (not a number) floating point value.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 isnan(float2 x) { return (asuint(x) & 0x7FFFFFFF) > 0x7F800000; }

        /// <summary>Returns a bool3 indicating for each component of a float3 whether it is a NaN (not a number) floating point value.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 isnan(float3 x) { return (asuint(x) & 0x7FFFFFFF) > 0x7F800000; }

        /// <summary>Returns a bool4 indicating for each component of a float4 whether it is a NaN (not a number) floating point value.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 isnan(float4 x) { return (asuint(x) & 0x7FFFFFFF) > 0x7F800000; }


        /// <summary>Returns true if the input double is a NaN (not a number) floating point value, false otherwise.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the value was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isnan(double x) { return (asulong(x) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000; }

        /// <summary>Returns a bool2 indicating for each component of a double2 whether it is a NaN (not a number) floating point value.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 isnan(double2 x) {
            return bool2((asulong(x.x) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000,
                         (asulong(x.y) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000);
        }

        /// <summary>Returns a bool3 indicating for each component of a double3 whether it is a NaN (not a number) floating point value.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 isnan(double3 x)
        {
            return bool3((asulong(x.x) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000,
                         (asulong(x.y) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000,
                         (asulong(x.z) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000);
        }

        /// <summary>Returns a bool4 indicating for each component of a double4 whether it is a NaN (not a number) floating point value.</summary>
        /// <remarks>
        /// NaN has several representations and may vary across architectures. Use this function to check if you have a NaN.
        /// </remarks>
        /// <param name="x">Input value.</param>
        /// <returns>True if the component was NaN; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 isnan(double4 x)
        {
            return bool4((asulong(x.x) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000,
                         (asulong(x.y) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000,
                         (asulong(x.z) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000,
                         (asulong(x.w) & 0x7FFFFFFFFFFFFFFF) > 0x7FF0000000000000);
        }

        /// <summary>
        /// Checks if the input is a power of two.
        /// </summary>
        /// <remarks>If x is less than or equal to zero, then this function returns false.</remarks>
        /// <param name="x">Integer input.</param>
        /// <returns>bool where true indicates that input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ispow2(int x)
        {
            return x > 0 && ((x & (x - 1)) == 0);
        }

        /// <summary>
        /// Checks if each component of the input is a power of two.
        /// </summary>
        /// <remarks>If a component of x is less than or equal to zero, then this function returns false in that component.</remarks>
        /// <param name="x">int2 input</param>
        /// <returns>bool2 where true in a component indicates the same component in the input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 ispow2(int2 x)
        {
            return new bool2(ispow2(x.x), ispow2(x.y));
        }

        /// <summary>
        /// Checks if each component of the input is a power of two.
        /// </summary>
        /// <remarks>If a component of x is less than or equal to zero, then this function returns false in that component.</remarks>
        /// <param name="x">int3 input</param>
        /// <returns>bool3 where true in a component indicates the same component in the input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 ispow2(int3 x)
        {
            return new bool3(ispow2(x.x), ispow2(x.y), ispow2(x.z));
        }

        /// <summary>
        /// Checks if each component of the input is a power of two.
        /// </summary>
        /// <remarks>If a component of x is less than or equal to zero, then this function returns false in that component.</remarks>
        /// <param name="x">int4 input</param>
        /// <returns>bool4 where true in a component indicates the same component in the input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 ispow2(int4 x)
        {
            return new bool4(ispow2(x.x), ispow2(x.y), ispow2(x.z), ispow2(x.w));
        }

        /// <summary>
        /// Checks if the input is a power of two.
        /// </summary>
        /// <remarks>If x is less than or equal to zero, then this function returns false.</remarks>
        /// <param name="x">Unsigned integer input.</param>
        /// <returns>bool where true indicates that input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ispow2(uint x)
        {
            return x > 0 && ((x & (x - 1)) == 0);
        }

        /// <summary>
        /// Checks if each component of the input is a power of two.
        /// </summary>
        /// <remarks>If a component of x is less than or equal to zero, then this function returns false in that component.</remarks>
        /// <param name="x">uint2 input</param>
        /// <returns>bool2 where true in a component indicates the same component in the input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 ispow2(uint2 x)
        {
            return new bool2(ispow2(x.x), ispow2(x.y));
        }

        /// <summary>
        /// Checks if each component of the input is a power of two.
        /// </summary>
        /// <remarks>If a component of x is less than or equal to zero, then this function returns false in that component.</remarks>
        /// <param name="x">uint3 input</param>
        /// <returns>bool3 where true in a component indicates the same component in the input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 ispow2(uint3 x)
        {
            return new bool3(ispow2(x.x), ispow2(x.y), ispow2(x.z));
        }

        /// <summary>
        /// Checks if each component of the input is a power of two.
        /// </summary>
        /// <remarks>If a component of x is less than or equal to zero, then this function returns false in that component.</remarks>
        /// <param name="x">uint4 input</param>
        /// <returns>bool4 where true in a component indicates the same component in the input was a power of two.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 ispow2(uint4 x)
        {
            return new bool4(ispow2(x.x), ispow2(x.y), ispow2(x.z), ispow2(x.w));
        }

        /// <summary>Returns the minimum of two int values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int min(int x, int y) { return x < y ? x : y; }

        /// <summary>Returns the componentwise minimum of two int2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 min(int2 x, int2 y) { return new int2(min(x.x, y.x), min(x.y, y.y)); }

        /// <summary>Returns the componentwise minimum of two int3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 min(int3 x, int3 y) { return new int3(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z)); }

        /// <summary>Returns the componentwise minimum of two int4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 min(int4 x, int4 y) { return new int4(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z), min(x.w, y.w)); }


        /// <summary>Returns the minimum of two uint values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint min(uint x, uint y) { return x < y ? x : y; }

        /// <summary>Returns the componentwise minimum of two uint2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 min(uint2 x, uint2 y) { return new uint2(min(x.x, y.x), min(x.y, y.y)); }

        /// <summary>Returns the componentwise minimum of two uint3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 min(uint3 x, uint3 y) { return new uint3(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z)); }

        /// <summary>Returns the componentwise minimum of two uint4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 min(uint4 x, uint4 y) { return new uint4(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z), min(x.w, y.w)); }


        /// <summary>Returns the minimum of two long values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long min(long x, long y) { return x < y ? x : y; }


        /// <summary>Returns the minimum of two ulong values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong min(ulong x, ulong y) { return x < y ? x : y; }


        /// <summary>Returns the minimum of two float values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float min(float x, float y) { return float.IsNaN(y) || x < y ? x : y; }

        /// <summary>Returns the componentwise minimum of two float2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 min(float2 x, float2 y) { return new float2(min(x.x, y.x), min(x.y, y.y)); }

        /// <summary>Returns the componentwise minimum of two float3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 min(float3 x, float3 y) { return new float3(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z)); }

        /// <summary>Returns the componentwise minimum of two float4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 min(float4 x, float4 y) { return new float4(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z), min(x.w, y.w)); }


        /// <summary>Returns the minimum of two double values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double min(double x, double y) { return double.IsNaN(y) || x < y ? x : y; }

        /// <summary>Returns the componentwise minimum of two double2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 min(double2 x, double2 y) { return new double2(min(x.x, y.x), min(x.y, y.y)); }

        /// <summary>Returns the componentwise minimum of two double3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 min(double3 x, double3 y) { return new double3(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z)); }

        /// <summary>Returns the componentwise minimum of two double4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise minimum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 min(double4 x, double4 y) { return new double4(min(x.x, y.x), min(x.y, y.y), min(x.z, y.z), min(x.w, y.w)); }


        /// <summary>Returns the maximum of two int values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int max(int x, int y) { return x > y ? x : y; }

        /// <summary>Returns the componentwise maximum of two int2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 max(int2 x, int2 y) { return new int2(max(x.x, y.x), max(x.y, y.y)); }

        /// <summary>Returns the componentwise maximum of two int3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 max(int3 x, int3 y) { return new int3(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z)); }

        /// <summary>Returns the componentwise maximum of two int4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 max(int4 x, int4 y) { return new int4(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z), max(x.w, y.w)); }


        /// <summary>Returns the maximum of two uint values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint max(uint x, uint y) { return x > y ? x : y; }

        /// <summary>Returns the componentwise maximum of two uint2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 max(uint2 x, uint2 y) { return new uint2(max(x.x, y.x), max(x.y, y.y)); }

        /// <summary>Returns the componentwise maximum of two uint3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 max(uint3 x, uint3 y) { return new uint3(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z)); }

        /// <summary>Returns the componentwise maximum of two uint4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 max(uint4 x, uint4 y) { return new uint4(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z), max(x.w, y.w)); }


        /// <summary>Returns the maximum of two long values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long max(long x, long y) { return x > y ? x : y; }


        /// <summary>Returns the maximum of two ulong values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong max(ulong x, ulong y) { return x > y ? x : y; }


        /// <summary>Returns the maximum of two float values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float max(float x, float y) { return float.IsNaN(y) || x > y ? x : y; }

        /// <summary>Returns the componentwise maximum of two float2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 max(float2 x, float2 y) { return new float2(max(x.x, y.x), max(x.y, y.y)); }

        /// <summary>Returns the componentwise maximum of two float3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 max(float3 x, float3 y) { return new float3(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z)); }

        /// <summary>Returns the componentwise maximum of two float4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 max(float4 x, float4 y) { return new float4(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z), max(x.w, y.w)); }


        /// <summary>Returns the maximum of two double values.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double max(double x, double y) { return double.IsNaN(y) || x > y ? x : y; }

        /// <summary>Returns the componentwise maximum of two double2 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 max(double2 x, double2 y) { return new double2(max(x.x, y.x), max(x.y, y.y)); }

        /// <summary>Returns the componentwise maximum of two double3 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 max(double3 x, double3 y) { return new double3(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z)); }

        /// <summary>Returns the componentwise maximum of two double4 vectors.</summary>
        /// <param name="x">The first input value.</param>
        /// <param name="y">The second input value.</param>
        /// <returns>The componentwise maximum of the two input values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 max(double4 x, double4 y) { return new double4(max(x.x, y.x), max(x.y, y.y), max(x.z, y.z), max(x.w, y.w)); }


        /// <summary>Returns the result of linearly interpolating from start to end using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The interpolation from start to end.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lerp(float start, float end, float t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 lerp(float2 start, float2 end, float t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 lerp(float3 start, float3 end, float t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 lerp(float4 start, float4 end, float t) { return start + t * (end - start); }


        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the corresponding components of the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 lerp(float2 start, float2 end, float2 t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the corresponding components of the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 lerp(float3 start, float3 end, float3 t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the corresponding components of the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 lerp(float4 start, float4 end, float4 t) { return start + t * (end - start); }


        /// <summary>Returns the result of linearly interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double lerp(double start, double end, double t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 lerp(double2 start, double2 end, double t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 lerp(double3 start, double3 end, double t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 lerp(double4 start, double4 end, double t) { return start + t * (end - start); }


        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the corresponding components of the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 lerp(double2 start, double2 end, double2 t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the corresponding components of the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 lerp(double3 start, double3 end, double3 t) { return start + t * (end - start); }

        /// <summary>Returns the result of a componentwise linear interpolating from x to y using the corresponding components of the interpolation parameter t.</summary>
        /// <remarks>
        /// If the interpolation parameter is not in the range [0, 1], then this function extrapolates.
        /// </remarks>
        /// <param name="start">The start point, corresponding to the interpolation parameter value of 0.</param>
        /// <param name="end">The end point, corresponding to the interpolation parameter value of 1.</param>
        /// <param name="t">The interpolation parameter. May be a value outside the interval [0, 1].</param>
        /// <returns>The componentwise interpolation from x to y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 lerp(double4 start, double4 end, double4 t) { return start + t * (end - start); }


        /// <summary>Returns the result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float unlerp(float start, float end, float x) { return (x - start) / (end - start); }

        /// <summary>Returns the componentwise result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 unlerp(float2 start, float2 end, float2 x) { return (x - start) / (end - start); }

        /// <summary>Returns the componentwise result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 unlerp(float3 start, float3 end, float3 x) { return (x - start) / (end - start); }

        /// <summary>Returns the componentwise result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 unlerp(float4 start, float4 end, float4 x) { return (x - start) / (end - start); }


        /// <summary>Returns the result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double unlerp(double start, double end, double x) { return (x - start) / (end - start); }

        /// <summary>Returns the componentwise result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 unlerp(double2 start, double2 end, double2 x) { return (x - start) / (end - start); }

        /// <summary>Returns the componentwise result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 unlerp(double3 start, double3 end, double3 x) { return (x - start) / (end - start); }

        /// <summary>Returns the componentwise result of normalizing a floating point value x to a range [a, b]. The opposite of lerp. Equivalent to (x - a) / (b - a).</summary>
        /// <param name="start">The start point of the range.</param>
        /// <param name="end">The end point of the range.</param>
        /// <param name="x">The value to normalize to the range.</param>
        /// <returns>The componentwise interpolation parameter of x with respect to the input range [a, b].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 unlerp(double4 start, double4 end, double4 x) { return (x - start) / (end - start); }


        /// <summary>Returns the result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float remap(float srcStart, float srcEnd, float dstStart, float dstEnd, float x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }

        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 remap(float2 srcStart, float2 srcEnd, float2 dstStart, float2 dstEnd, float2 x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }

        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 remap(float3 srcStart, float3 srcEnd, float3 dstStart, float3 dstEnd, float3 x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }

        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 remap(float4 srcStart, float4 srcEnd, float4 dstStart, float4 dstEnd, float4 x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }


        /// <summary>Returns the result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double remap(double srcStart, double srcEnd, double dstStart, double dstEnd, double x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }

        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 remap(double2 srcStart, double2 srcEnd, double2 dstStart, double2 dstEnd, double2 x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }

        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 remap(double3 srcStart, double3 srcEnd, double3 dstStart, double3 dstEnd, double3 x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }

        /// <summary>Returns the componentwise result of a non-clamping linear remapping of a value x from source range [srcStart, srcEnd] to the destination range [dstStart, dstEnd].</summary>
        /// <param name="srcStart">The start point of the source range [srcStart, srcEnd].</param>
        /// <param name="srcEnd">The end point of the source range [srcStart, srcEnd].</param>
        /// <param name="dstStart">The start point of the destination range [dstStart, dstEnd].</param>
        /// <param name="dstEnd">The end point of the destination range [dstStart, dstEnd].</param>
        /// <param name="x">The value to remap from the source to destination range.</param>
        /// <returns>The componentwise remap of input x from the source range to the destination range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 remap(double4 srcStart, double4 srcEnd, double4 dstStart, double4 dstEnd, double4 x) { return lerp(dstStart, dstEnd, unlerp(srcStart, srcEnd, x)); }


        /// <summary>Returns the result of a multiply-add operation (a * b + c) on 3 int values.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int mad(int mulA, int mulB, int addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 int2 vectors.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 mad(int2 mulA, int2 mulB, int2 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 int3 vectors.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 mad(int3 mulA, int3 mulB, int3 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 int4 vectors.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 mad(int4 mulA, int4 mulB, int4 addC) { return mulA * mulB + addC; }


        /// <summary>Returns the result of a multiply-add operation (a * b + c) on 3 uint values.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint mad(uint mulA, uint mulB, uint addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 uint2 vectors.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 mad(uint2 mulA, uint2 mulB, uint2 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 uint3 vectors.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 mad(uint3 mulA, uint3 mulB, uint3 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 uint4 vectors.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 mad(uint4 mulA, uint4 mulB, uint4 addC) { return mulA * mulB + addC; }


        /// <summary>Returns the result of a multiply-add operation (a * b + c) on 3 long values.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long mad(long mulA, long mulB, long addC) { return mulA * mulB + addC; }


        /// <summary>Returns the result of a multiply-add operation (a * b + c) on 3 ulong values.</summary>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong mad(ulong mulA, ulong mulB, ulong addC) { return mulA * mulB + addC; }


        /// <summary>Returns the result of a multiply-add operation (a * b + c) on 3 float values.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float mad(float mulA, float mulB, float addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 float2 vectors.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 mad(float2 mulA, float2 mulB, float2 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 float3 vectors.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mad(float3 mulA, float3 mulB, float3 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 float4 vectors.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 mad(float4 mulA, float4 mulB, float4 addC) { return mulA * mulB + addC; }


        /// <summary>Returns the result of a multiply-add operation (a * b + c) on 3 double values.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double mad(double mulA, double mulB, double addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 double2 vectors.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 mad(double2 mulA, double2 mulB, double2 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 double3 vectors.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 mad(double3 mulA, double3 mulB, double3 addC) { return mulA * mulB + addC; }

        /// <summary>Returns the result of a componentwise multiply-add operation (a * b + c) on 3 double4 vectors.</summary>
        /// <remarks>
        /// When Burst compiled with fast math enabled on some architectures, this could be converted to a fused multiply add (FMA).
        /// FMA is more accurate due to rounding once at the end of the computation rather than twice that is required when
        /// this computation is not fused.
        /// </remarks>
        /// <param name="mulA">First value to multiply.</param>
        /// <param name="mulB">Second value to multiply.</param>
        /// <param name="addC">Third value to add to the product of a and b.</param>
        /// <returns>The componentwise multiply-add of the inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 mad(double4 mulA, double4 mulB, double4 addC) { return mulA * mulB + addC; }


        /// <summary>Returns the result of clamping the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are int values.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int clamp(int valueToClamp, int lowerBound, int upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the int2 x into the interval [a, b], where a and b are int2 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 clamp(int2 valueToClamp, int2 lowerBound, int2 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the int3 x into the interval [a, b], where x, a and b are int3 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 clamp(int3 valueToClamp, int3 lowerBound, int3 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are int4 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 clamp(int4 valueToClamp, int4 lowerBound, int4 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }


        /// <summary>Returns the result of clamping the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are uint values.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint clamp(uint valueToClamp, uint lowerBound, uint upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are uint2 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 clamp(uint2 valueToClamp, uint2 lowerBound, uint2 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are uint3 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 clamp(uint3 valueToClamp, uint3 lowerBound, uint3 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are uint4 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 clamp(uint4 valueToClamp, uint4 lowerBound, uint4 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }


        /// <summary>Returns the result of clamping the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are long values.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long clamp(long valueToClamp, long lowerBound, long upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of clamping the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are ulong values.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong clamp(ulong valueToClamp, ulong lowerBound, ulong upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }


        /// <summary>Returns the result of clamping the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are float values.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float clamp(float valueToClamp, float lowerBound, float upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are float2 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 clamp(float2 valueToClamp, float2 lowerBound, float2 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are float3 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 clamp(float3 valueToClamp, float3 lowerBound, float3 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are float4 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 clamp(float4 valueToClamp, float4 lowerBound, float4 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }


        /// <summary>Returns the result of clamping the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are double values.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double clamp(double valueToClamp, double lowerBound, double upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are double2 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 clamp(double2 valueToClamp, double2 lowerBound, double2 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are double3 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 clamp(double3 valueToClamp, double3 lowerBound, double3 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }

        /// <summary>Returns the result of a componentwise clamping of the value valueToClamp into the interval (inclusive) [lowerBound, upperBound], where valueToClamp, lowerBound and upperBound are double4 vectors.</summary>
        /// <param name="valueToClamp">Input value to be clamped.</param>
        /// <param name="lowerBound">Lower bound of the interval.</param>
        /// <param name="upperBound">Upper bound of the interval.</param>
        /// <returns>The componentwise clamping of the input valueToClamp into the interval (inclusive) [lowerBound, upperBound].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 clamp(double4 valueToClamp, double4 lowerBound, double4 upperBound) { return max(lowerBound, min(upperBound, valueToClamp)); }


        /// <summary>Returns the result of clamping the float value x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float saturate(float x) { return clamp(x, 0.0f, 1.0f); }

        /// <summary>Returns the result of a componentwise clamping of the float2 vector x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 saturate(float2 x) { return clamp(x, new float2(0.0f), new float2(1.0f)); }

        /// <summary>Returns the result of a componentwise clamping of the float3 vector x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 saturate(float3 x) { return clamp(x, new float3(0.0f), new float3(1.0f)); }

        /// <summary>Returns the result of a componentwise clamping of the float4 vector x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 saturate(float4 x) { return clamp(x, new float4(0.0f), new float4(1.0f)); }


        /// <summary>Returns the result of clamping the double value x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double saturate(double x) { return clamp(x, 0.0, 1.0); }

        /// <summary>Returns the result of a componentwise clamping of the double2 vector x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 saturate(double2 x) { return clamp(x, new double2(0.0), new double2(1.0)); }

        /// <summary>Returns the result of a componentwise clamping of the double3 vector x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 saturate(double3 x) { return clamp(x, new double3(0.0), new double3(1.0)); }

        /// <summary>Returns the result of a componentwise clamping of the double4 vector x into the interval [0, 1].</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise clamping of the input into the interval [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 saturate(double4 x) { return clamp(x, new double4(0.0), new double4(1.0)); }


        /// <summary>Returns the absolute value of a int value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int abs(int x) { return max(-x, x); }

        /// <summary>Returns the componentwise absolute value of a int2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 abs(int2 x) { return max(-x, x); }

        /// <summary>Returns the componentwise absolute value of a int3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 abs(int3 x) { return max(-x, x); }

        /// <summary>Returns the componentwise absolute value of a int4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 abs(int4 x) { return max(-x, x); }

        /// <summary>Returns the absolute value of a long value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long abs(long x) { return max(-x, x); }


        /// <summary>Returns the absolute value of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float abs(float x) { return asfloat(asuint(x) & 0x7FFFFFFF); }

        /// <summary>Returns the componentwise absolute value of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 abs(float2 x) { return asfloat(asuint(x) & 0x7FFFFFFF); }

        /// <summary>Returns the componentwise absolute value of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 abs(float3 x) { return asfloat(asuint(x) & 0x7FFFFFFF); }

        /// <summary>Returns the componentwise absolute value of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 abs(float4 x) { return asfloat(asuint(x) & 0x7FFFFFFF); }


        /// <summary>Returns the absolute value of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double abs(double x) { return asdouble(asulong(x) & 0x7FFFFFFFFFFFFFFF); }

        /// <summary>Returns the componentwise absolute value of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 abs(double2 x) { return double2(asdouble(asulong(x.x) & 0x7FFFFFFFFFFFFFFF), asdouble(asulong(x.y) & 0x7FFFFFFFFFFFFFFF)); }

        /// <summary>Returns the componentwise absolute value of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 abs(double3 x) { return double3(asdouble(asulong(x.x) & 0x7FFFFFFFFFFFFFFF), asdouble(asulong(x.y) & 0x7FFFFFFFFFFFFFFF), asdouble(asulong(x.z) & 0x7FFFFFFFFFFFFFFF)); }

        /// <summary>Returns the componentwise absolute value of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise absolute value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 abs(double4 x) { return double4(asdouble(asulong(x.x) & 0x7FFFFFFFFFFFFFFF), asdouble(asulong(x.y) & 0x7FFFFFFFFFFFFFFF), asdouble(asulong(x.z) & 0x7FFFFFFFFFFFFFFF), asdouble(asulong(x.w) & 0x7FFFFFFFFFFFFFFF)); }


        /// <summary>Returns the dot product of two int values. Equivalent to multiplication.</summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>The dot product of two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dot(int x, int y) { return x * y; }

        /// <summary>Returns the dot product of two int2 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dot(int2 x, int2 y) { return x.x * y.x + x.y * y.y; }

        /// <summary>Returns the dot product of two int3 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dot(int3 x, int3 y) { return x.x * y.x + x.y * y.y + x.z * y.z; }

        /// <summary>Returns the dot product of two int4 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dot(int4 x, int4 y) { return x.x * y.x + x.y * y.y + x.z * y.z + x.w * y.w; }


        /// <summary>Returns the dot product of two uint values. Equivalent to multiplication.</summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>The dot product of two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint dot(uint x, uint y) { return x * y; }

        /// <summary>Returns the dot product of two uint2 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint dot(uint2 x, uint2 y) { return x.x * y.x + x.y * y.y; }

        /// <summary>Returns the dot product of two uint3 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint dot(uint3 x, uint3 y) { return x.x * y.x + x.y * y.y + x.z * y.z; }

        /// <summary>Returns the dot product of two uint4 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint dot(uint4 x, uint4 y) { return x.x * y.x + x.y * y.y + x.z * y.z + x.w * y.w; }


        /// <summary>Returns the dot product of two float values. Equivalent to multiplication.</summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>The dot product of two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float x, float y) { return x * y; }

        /// <summary>Returns the dot product of two float2 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float2 x, float2 y) { return x.x * y.x + x.y * y.y; }

        /// <summary>Returns the dot product of two float3 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float3 x, float3 y) { return x.x * y.x + x.y * y.y + x.z * y.z; }

        /// <summary>Returns the dot product of two float4 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float dot(float4 x, float4 y) { return x.x * y.x + x.y * y.y + x.z * y.z + x.w * y.w; }


        /// <summary>Returns the dot product of two double values. Equivalent to multiplication.</summary>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <returns>The dot product of two values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double dot(double x, double y) { return x * y; }

        /// <summary>Returns the dot product of two double2 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double dot(double2 x, double2 y) { return x.x * y.x + x.y * y.y; }

        /// <summary>Returns the dot product of two double3 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double dot(double3 x, double3 y) { return x.x * y.x + x.y * y.y + x.z * y.z; }

        /// <summary>Returns the dot product of two double4 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double dot(double4 x, double4 y) { return x.x * y.x + x.y * y.y + x.z * y.z + x.w * y.w; }


        /// <summary>Returns the tangent of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float tan(float x) { return (float)System.Math.Tan(x); }

        /// <summary>Returns the componentwise tangent of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 tan(float2 x) { return new float2(tan(x.x), tan(x.y)); }

        /// <summary>Returns the componentwise tangent of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 tan(float3 x) { return new float3(tan(x.x), tan(x.y), tan(x.z)); }

        /// <summary>Returns the componentwise tangent of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 tan(float4 x) { return new float4(tan(x.x), tan(x.y), tan(x.z), tan(x.w)); }


        /// <summary>Returns the tangent of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double tan(double x) { return System.Math.Tan(x); }

        /// <summary>Returns the componentwise tangent of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 tan(double2 x) { return new double2(tan(x.x), tan(x.y)); }

        /// <summary>Returns the componentwise tangent of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 tan(double3 x) { return new double3(tan(x.x), tan(x.y), tan(x.z)); }

        /// <summary>Returns the componentwise tangent of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 tan(double4 x) { return new double4(tan(x.x), tan(x.y), tan(x.z), tan(x.w)); }


        /// <summary>Returns the hyperbolic tangent of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float tanh(float x) { return (float)System.Math.Tanh(x); }

        /// <summary>Returns the componentwise hyperbolic tangent of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 tanh(float2 x) { return new float2(tanh(x.x), tanh(x.y)); }

        /// <summary>Returns the componentwise hyperbolic tangent of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 tanh(float3 x) { return new float3(tanh(x.x), tanh(x.y), tanh(x.z)); }

        /// <summary>Returns the componentwise hyperbolic tangent of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 tanh(float4 x) { return new float4(tanh(x.x), tanh(x.y), tanh(x.z), tanh(x.w)); }


        /// <summary>Returns the hyperbolic tangent of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double tanh(double x) { return System.Math.Tanh(x); }

        /// <summary>Returns the componentwise hyperbolic tangent of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 tanh(double2 x) { return new double2(tanh(x.x), tanh(x.y)); }

        /// <summary>Returns the componentwise hyperbolic tangent of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 tanh(double3 x) { return new double3(tanh(x.x), tanh(x.y), tanh(x.z)); }

        /// <summary>Returns the componentwise hyperbolic tangent of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic tangent of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 tanh(double4 x) { return new double4(tanh(x.x), tanh(x.y), tanh(x.z), tanh(x.w)); }


        /// <summary>Returns the arctangent of a float value.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float atan(float x) { return (float)System.Math.Atan(x); }

        /// <summary>Returns the componentwise arctangent of a float2 vector.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The componentwise arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 atan(float2 x) { return new float2(atan(x.x), atan(x.y)); }

        /// <summary>Returns the componentwise arctangent of a float3 vector.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The componentwise arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 atan(float3 x) { return new float3(atan(x.x), atan(x.y), atan(x.z)); }

        /// <summary>Returns the componentwise arctangent of a float4 vector.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The componentwise arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 atan(float4 x) { return new float4(atan(x.x), atan(x.y), atan(x.z), atan(x.w)); }


        /// <summary>Returns the arctangent of a double value.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double atan(double x) { return System.Math.Atan(x); }

        /// <summary>Returns the componentwise arctangent of a double2 vector.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The componentwise arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 atan(double2 x) { return new double2(atan(x.x), atan(x.y)); }

        /// <summary>Returns the componentwise arctangent of a double3 vector.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The componentwise arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 atan(double3 x) { return new double3(atan(x.x), atan(x.y), atan(x.z)); }

        /// <summary>Returns the componentwise arctangent of a double4 vector.</summary>
        /// <param name="x">A tangent value, usually the ratio y/x on the unit circle.</param>
        /// <returns>The componentwise arctangent of the input, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 atan(double4 x) { return new double4(atan(x.x), atan(x.y), atan(x.z), atan(x.w)); }


        /// <summary>Returns the 2-argument arctangent of a pair of float values.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float atan2(float y, float x) { return (float)System.Math.Atan2(y, x); }

        /// <summary>Returns the componentwise 2-argument arctangent of a pair of floats2 vectors.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The componentwise arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 atan2(float2 y, float2 x) { return new float2(atan2(y.x, x.x), atan2(y.y, x.y)); }

        /// <summary>Returns the componentwise 2-argument arctangent of a pair of floats3 vectors.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The componentwise arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 atan2(float3 y, float3 x) { return new float3(atan2(y.x, x.x), atan2(y.y, x.y), atan2(y.z, x.z)); }

        /// <summary>Returns the componentwise 2-argument arctangent of a pair of floats4 vectors.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The componentwise arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 atan2(float4 y, float4 x) { return new float4(atan2(y.x, x.x), atan2(y.y, x.y), atan2(y.z, x.z), atan2(y.w, x.w)); }


        /// <summary>Returns the 2-argument arctangent of a pair of double values.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double atan2(double y, double x) { return System.Math.Atan2(y, x); }

        /// <summary>Returns the 2-argument arctangent of a pair of double2 vectors.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The componentwise arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 atan2(double2 y, double2 x) { return new double2(atan2(y.x, x.x), atan2(y.y, x.y)); }

        /// <summary>Returns the 2-argument arctangent of a pair of double3 vectors.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The componentwise arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 atan2(double3 y, double3 x) { return new double3(atan2(y.x, x.x), atan2(y.y, x.y), atan2(y.z, x.z)); }

        /// <summary>Returns the 2-argument arctangent of a pair of double4 vectors.</summary>
        /// <param name="y">Numerator of the ratio y/x, usually the y component on the unit circle.</param>
        /// <param name="x">Denominator of the ratio y/x, usually the x component on the unit circle.</param>
        /// <returns>The componentwise arctangent of the ratio y/x, in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 atan2(double4 y, double4 x) { return new double4(atan2(y.x, x.x), atan2(y.y, x.y), atan2(y.z, x.z), atan2(y.w, x.w)); }


        /// <summary>Returns the cosine of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cos(float x) { return (float)System.Math.Cos(x); }

        /// <summary>Returns the componentwise cosine of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cos(float2 x) { return new float2(cos(x.x), cos(x.y)); }

        /// <summary>Returns the componentwise cosine of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 cos(float3 x) { return new float3(cos(x.x), cos(x.y), cos(x.z)); }

        /// <summary>Returns the componentwise cosine of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 cos(float4 x) { return new float4(cos(x.x), cos(x.y), cos(x.z), cos(x.w)); }


        /// <summary>Returns the cosine of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cos(double x) { return System.Math.Cos(x); }

        /// <summary>Returns the componentwise cosine of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 cos(double2 x) { return new double2(cos(x.x), cos(x.y)); }

        /// <summary>Returns the componentwise cosine of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 cos(double3 x) { return new double3(cos(x.x), cos(x.y), cos(x.z)); }

        /// <summary>Returns the componentwise cosine of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise cosine cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 cos(double4 x) { return new double4(cos(x.x), cos(x.y), cos(x.z), cos(x.w)); }


        /// <summary>Returns the hyperbolic cosine of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cosh(float x) { return (float)System.Math.Cosh(x); }

        /// <summary>Returns the componentwise hyperbolic cosine of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cosh(float2 x) { return new float2(cosh(x.x), cosh(x.y)); }

        /// <summary>Returns the componentwise hyperbolic cosine of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 cosh(float3 x) { return new float3(cosh(x.x), cosh(x.y), cosh(x.z)); }

        /// <summary>Returns the componentwise hyperbolic cosine of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 cosh(float4 x) { return new float4(cosh(x.x), cosh(x.y), cosh(x.z), cosh(x.w)); }


        /// <summary>Returns the hyperbolic cosine of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cosh(double x) { return System.Math.Cosh(x); }

        /// <summary>Returns the componentwise hyperbolic cosine of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 cosh(double2 x) { return new double2(cosh(x.x), cosh(x.y)); }

        /// <summary>Returns the componentwise hyperbolic cosine of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 cosh(double3 x) { return new double3(cosh(x.x), cosh(x.y), cosh(x.z)); }

        /// <summary>Returns the componentwise hyperbolic cosine of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic cosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 cosh(double4 x) { return new double4(cosh(x.x), cosh(x.y), cosh(x.z), cosh(x.w)); }


        /// <summary>Returns the arccosine of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float acos(float x) { return (float)System.Math.Acos((float)x); }

        /// <summary>Returns the componentwise arccosine of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 acos(float2 x) { return new float2(acos(x.x), acos(x.y)); }

        /// <summary>Returns the componentwise arccosine of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 acos(float3 x) { return new float3(acos(x.x), acos(x.y), acos(x.z)); }

        /// <summary>Returns the componentwise arccosine of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 acos(float4 x) { return new float4(acos(x.x), acos(x.y), acos(x.z), acos(x.w)); }


        /// <summary>Returns the arccosine of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double acos(double x) { return System.Math.Acos(x); }

        /// <summary>Returns the componentwise arccosine of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 acos(double2 x) { return new double2(acos(x.x), acos(x.y)); }

        /// <summary>Returns the componentwise arccosine of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 acos(double3 x) { return new double3(acos(x.x), acos(x.y), acos(x.z)); }

        /// <summary>Returns the componentwise arccosine of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arccosine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 acos(double4 x) { return new double4(acos(x.x), acos(x.y), acos(x.z), acos(x.w)); }


        /// <summary>Returns the sine of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sin(float x) { return (float)System.Math.Sin((float)x); }

        /// <summary>Returns the componentwise sine of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sin(float2 x) { return new float2(sin(x.x), sin(x.y)); }

        /// <summary>Returns the componentwise sine of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sin(float3 x) { return new float3(sin(x.x), sin(x.y), sin(x.z)); }

        /// <summary>Returns the componentwise sine of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sin(float4 x) { return new float4(sin(x.x), sin(x.y), sin(x.z), sin(x.w)); }


        /// <summary>Returns the sine of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double sin(double x) { return System.Math.Sin(x); }

        /// <summary>Returns the componentwise sine of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 sin(double2 x) { return new double2(sin(x.x), sin(x.y)); }

        /// <summary>Returns the componentwise sine of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 sin(double3 x) { return new double3(sin(x.x), sin(x.y), sin(x.z)); }

        /// <summary>Returns the componentwise sine of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 sin(double4 x) { return new double4(sin(x.x), sin(x.y), sin(x.z), sin(x.w)); }


        /// <summary>Returns the hyperbolic sine of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sinh(float x) { return (float)System.Math.Sinh((float)x); }

        /// <summary>Returns the componentwise hyperbolic sine of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sinh(float2 x) { return new float2(sinh(x.x), sinh(x.y)); }

        /// <summary>Returns the componentwise hyperbolic sine of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sinh(float3 x) { return new float3(sinh(x.x), sinh(x.y), sinh(x.z)); }

        /// <summary>Returns the componentwise hyperbolic sine of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sinh(float4 x) { return new float4(sinh(x.x), sinh(x.y), sinh(x.z), sinh(x.w)); }


        /// <summary>Returns the hyperbolic sine of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double sinh(double x) { return System.Math.Sinh(x); }

        /// <summary>Returns the componentwise hyperbolic sine of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 sinh(double2 x) { return new double2(sinh(x.x), sinh(x.y)); }

        /// <summary>Returns the componentwise hyperbolic sine of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 sinh(double3 x) { return new double3(sinh(x.x), sinh(x.y), sinh(x.z)); }

        /// <summary>Returns the componentwise hyperbolic sine of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise hyperbolic sine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 sinh(double4 x) { return new double4(sinh(x.x), sinh(x.y), sinh(x.z), sinh(x.w)); }


        /// <summary>Returns the arcsine of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float asin(float x) { return (float)System.Math.Asin((float)x); }

        /// <summary>Returns the componentwise arcsine of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 asin(float2 x) { return new float2(asin(x.x), asin(x.y)); }

        /// <summary>Returns the componentwise arcsine of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 asin(float3 x) { return new float3(asin(x.x), asin(x.y), asin(x.z)); }

        /// <summary>Returns the componentwise arcsine of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 asin(float4 x) { return new float4(asin(x.x), asin(x.y), asin(x.z), asin(x.w)); }


        /// <summary>Returns the arcsine of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double asin(double x) { return System.Math.Asin(x); }

        /// <summary>Returns the componentwise arcsine of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 asin(double2 x) { return new double2(asin(x.x), asin(x.y)); }

        /// <summary>Returns the componentwise arcsine of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 asin(double3 x) { return new double3(asin(x.x), asin(x.y), asin(x.z)); }

        /// <summary>Returns the componentwise arcsine of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise arcsine of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 asin(double4 x) { return new double4(asin(x.x), asin(x.y), asin(x.z), asin(x.w)); }


        /// <summary>Returns the result of rounding a float value up to the nearest integral value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float floor(float x) { return (float)System.Math.Floor((float)x); }

        /// <summary>Returns the result of rounding each component of a float2 vector value down to the nearest value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 floor(float2 x) { return new float2(floor(x.x), floor(x.y)); }

        /// <summary>Returns the result of rounding each component of a float3 vector value down to the nearest value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 floor(float3 x) { return new float3(floor(x.x), floor(x.y), floor(x.z)); }

        /// <summary>Returns the result of rounding each component of a float4 vector value down to the nearest value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 floor(float4 x) { return new float4(floor(x.x), floor(x.y), floor(x.z), floor(x.w)); }


        /// <summary>Returns the result of rounding a double value up to the nearest integral value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double floor(double x) { return System.Math.Floor(x); }

        /// <summary>Returns the result of rounding each component of a double2 vector value down to the nearest value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 floor(double2 x) { return new double2(floor(x.x), floor(x.y)); }

        /// <summary>Returns the result of rounding each component of a double3 vector value down to the nearest value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 floor(double3 x) { return new double3(floor(x.x), floor(x.y), floor(x.z)); }

        /// <summary>Returns the result of rounding each component of a double4 vector value down to the nearest value less or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round down to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 floor(double4 x) { return new double4(floor(x.x), floor(x.y), floor(x.z), floor(x.w)); }


        /// <summary>Returns the result of rounding a float value up to the nearest integral value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ceil(float x) { return (float)System.Math.Ceiling((float)x); }

        /// <summary>Returns the result of rounding each component of a float2 vector value up to the nearest value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ceil(float2 x) { return new float2(ceil(x.x), ceil(x.y)); }

        /// <summary>Returns the result of rounding each component of a float3 vector value up to the nearest value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ceil(float3 x) { return new float3(ceil(x.x), ceil(x.y), ceil(x.z)); }

        /// <summary>Returns the result of rounding each component of a float4 vector value up to the nearest value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 ceil(float4 x) { return new float4(ceil(x.x), ceil(x.y), ceil(x.z), ceil(x.w)); }


        /// <summary>Returns the result of rounding a double value up to the nearest greater integral value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ceil(double x) { return System.Math.Ceiling(x); }

        /// <summary>Returns the result of rounding each component of a double2 vector value up to the nearest integral value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 ceil(double2 x) { return new double2(ceil(x.x), ceil(x.y)); }

        /// <summary>Returns the result of rounding each component of a double3 vector value up to the nearest integral value greater or equal to the original value..</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 ceil(double3 x) { return new double3(ceil(x.x), ceil(x.y), ceil(x.z)); }

        /// <summary>Returns the result of rounding each component of a double4 vector value up to the nearest integral value greater or equal to the original value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round up to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 ceil(double4 x) { return new double4(ceil(x.x), ceil(x.y), ceil(x.z), ceil(x.w)); }


        /// <summary>Returns the result of rounding a float value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float round(float x) { return (float)System.Math.Round((float)x); }

        /// <summary>Returns the result of rounding each component of a float2 vector value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 round(float2 x) { return new float2(round(x.x), round(x.y)); }

        /// <summary>Returns the result of rounding each component of a float3 vector value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 round(float3 x) { return new float3(round(x.x), round(x.y), round(x.z)); }

        /// <summary>Returns the result of rounding each component of a float4 vector value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 round(float4 x) { return new float4(round(x.x), round(x.y), round(x.z), round(x.w)); }


        /// <summary>Returns the result of rounding a double value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double round(double x) { return System.Math.Round(x); }

        /// <summary>Returns the result of rounding each component of a double2 vector value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 round(double2 x) { return new double2(round(x.x), round(x.y)); }

        /// <summary>Returns the result of rounding each component of a double3 vector value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 round(double3 x) { return new double3(round(x.x), round(x.y), round(x.z)); }

        /// <summary>Returns the result of rounding each component of a double4 vector value to the nearest integral value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise round to nearest integral value of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 round(double4 x) { return new double4(round(x.x), round(x.y), round(x.z), round(x.w)); }


        /// <summary>Returns the result of truncating a float value to an integral float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float trunc(float x) { return (float)System.Math.Truncate((float)x); }

        /// <summary>Returns the result of a componentwise truncation of a float2 value to an integral float2 value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 trunc(float2 x) { return new float2(trunc(x.x), trunc(x.y)); }

        /// <summary>Returns the result of a componentwise truncation of a float3 value to an integral float3 value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 trunc(float3 x) { return new float3(trunc(x.x), trunc(x.y), trunc(x.z)); }

        /// <summary>Returns the result of a componentwise truncation of a float4 value to an integral float4 value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 trunc(float4 x) { return new float4(trunc(x.x), trunc(x.y), trunc(x.z), trunc(x.w)); }


        /// <summary>Returns the result of truncating a double value to an integral double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double trunc(double x) { return System.Math.Truncate(x); }

        /// <summary>Returns the result of a componentwise truncation of a double2 value to an integral double2 value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 trunc(double2 x) { return new double2(trunc(x.x), trunc(x.y)); }

        /// <summary>Returns the result of a componentwise truncation of a double3 value to an integral double3 value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 trunc(double3 x) { return new double3(trunc(x.x), trunc(x.y), trunc(x.z)); }

        /// <summary>Returns the result of a componentwise truncation of a double4 value to an integral double4 value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise truncation of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 trunc(double4 x) { return new double4(trunc(x.x), trunc(x.y), trunc(x.z), trunc(x.w)); }


        /// <summary>Returns the fractional part of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float frac(float x) { return x - floor(x); }

        /// <summary>Returns the componentwise fractional parts of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 frac(float2 x) { return x - floor(x); }

        /// <summary>Returns the componentwise fractional parts of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 frac(float3 x) { return x - floor(x); }

        /// <summary>Returns the componentwise fractional parts of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 frac(float4 x) { return x - floor(x); }


        /// <summary>Returns the fractional part of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double frac(double x) { return x - floor(x); }

        /// <summary>Returns the componentwise fractional parts of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 frac(double2 x) { return x - floor(x); }

        /// <summary>Returns the componentwise fractional parts of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 frac(double3 x) { return x - floor(x); }

        /// <summary>Returns the componentwise fractional parts of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise fractional part of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 frac(double4 x) { return x - floor(x); }


        /// <summary>Returns the reciprocal a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float rcp(float x) { return 1.0f / x; }

        /// <summary>Returns the componentwise reciprocal a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 rcp(float2 x) { return 1.0f / x; }

        /// <summary>Returns the componentwise reciprocal a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 rcp(float3 x) { return 1.0f / x; }

        /// <summary>Returns the componentwise reciprocal a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 rcp(float4 x) { return 1.0f / x; }


        /// <summary>Returns the reciprocal a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double rcp(double x) { return 1.0 / x; }

        /// <summary>Returns the componentwise reciprocal a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 rcp(double2 x) { return 1.0 / x; }

        /// <summary>Returns the componentwise reciprocal a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 rcp(double3 x) { return 1.0 / x; }

        /// <summary>Returns the componentwise reciprocal a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise reciprocal of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 rcp(double4 x) { return 1.0 / x; }

        /// <summary>Returns the sign of a int value. -1 if it is less than zero, 0 if it is zero and 1 if it greater than zero.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int sign(int x) { return (x > 0 ? 1 : 0) - (x < 0 ? 1 : 0); }

        /// <summary>Returns the componentwise sign of a int2 value. 1 for positive components, 0 for zero components and -1 for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 sign(int2 x) { return new int2(sign(x.x), sign(x.y)); }

        /// <summary>Returns the componentwise sign of a int3 value. 1 for positive components, 0 for zero components and -1 for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 sign(int3 x) { return new int3(sign(x.x), sign(x.y), sign(x.z)); }

        /// <summary>Returns the componentwise sign of a int4 value. 1 for positive components, 0 for zero components and -1 for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 sign(int4 x) { return new int4(sign(x.x), sign(x.y), sign(x.z), sign(x.w)); }

        /// <summary>Returns the sign of a float value. -1.0f if it is less than zero, 0.0f if it is zero and 1.0f if it greater than zero.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sign(float x) { return (x > 0.0f ? 1.0f : 0.0f) - (x < 0.0f ? 1.0f : 0.0f); }

        /// <summary>Returns the componentwise sign of a float2 value. 1.0f for positive components, 0.0f for zero components and -1.0f for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sign(float2 x) { return new float2(sign(x.x), sign(x.y)); }

        /// <summary>Returns the componentwise sign of a float3 value. 1.0f for positive components, 0.0f for zero components and -1.0f for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sign(float3 x) { return new float3(sign(x.x), sign(x.y), sign(x.z)); }

        /// <summary>Returns the componentwise sign of a float4 value. 1.0f for positive components, 0.0f for zero components and -1.0f for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sign(float4 x) { return new float4(sign(x.x), sign(x.y), sign(x.z), sign(x.w)); }


        /// <summary>Returns the sign of a double value. -1.0 if it is less than zero, 0.0 if it is zero and 1.0 if it greater than zero.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double sign(double x) { return x == 0 ? 0 : (x > 0.0 ? 1.0 : 0.0) - (x < 0.0 ? 1.0 : 0.0); }

        /// <summary>Returns the componentwise sign of a double2 value. 1.0 for positive components, 0.0 for zero components and -1.0 for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 sign(double2 x) { return new double2(sign(x.x), sign(x.y)); }

        /// <summary>Returns the componentwise sign of a double3 value. 1.0 for positive components, 0.0 for zero components and -1.0 for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 sign(double3 x) { return new double3(sign(x.x), sign(x.y), sign(x.z)); }

        /// <summary>Returns the componentwise sign of a double4 value. 1.0 for positive components, 0.0 for zero components and -1.0 for negative components.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise sign of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 sign(double4 x) { return new double4(sign(x.x), sign(x.y), sign(x.z), sign(x.w)); }


        /// <summary>Returns x raised to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float pow(float x, float y) { return (float)System.Math.Pow((float)x, (float)y); }

        /// <summary>Returns the componentwise result of raising x to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 pow(float2 x, float2 y) { return new float2(pow(x.x, y.x), pow(x.y, y.y)); }

        /// <summary>Returns the componentwise result of raising x to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 pow(float3 x, float3 y) { return new float3(pow(x.x, y.x), pow(x.y, y.y), pow(x.z, y.z)); }

        /// <summary>Returns the componentwise result of raising x to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 pow(float4 x, float4 y) { return new float4(pow(x.x, y.x), pow(x.y, y.y), pow(x.z, y.z), pow(x.w, y.w)); }


        /// <summary>Returns x raised to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double pow(double x, double y) { return System.Math.Pow(x, y); }

        /// <summary>Returns the componentwise result of raising x to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 pow(double2 x, double2 y) { return new double2(pow(x.x, y.x), pow(x.y, y.y)); }

        /// <summary>Returns the componentwise result of raising x to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 pow(double3 x, double3 y) { return new double3(pow(x.x, y.x), pow(x.y, y.y), pow(x.z, y.z)); }

        /// <summary>Returns the componentwise result of raising x to the power y.</summary>
        /// <param name="x">The exponent base.</param>
        /// <param name="y">The exponent power.</param>
        /// <returns>The componentwise result of raising x to the power y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 pow(double4 x, double4 y) { return new double4(pow(x.x, y.x), pow(x.y, y.y), pow(x.z, y.z), pow(x.w, y.w)); }


        /// <summary>Returns the base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float exp(float x) { return (float)System.Math.Exp((float)x); }

        /// <summary>Returns the componentwise base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 exp(float2 x) { return new float2(exp(x.x), exp(x.y)); }

        /// <summary>Returns the componentwise base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 exp(float3 x) { return new float3(exp(x.x), exp(x.y), exp(x.z)); }

        /// <summary>Returns the componentwise base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 exp(float4 x) { return new float4(exp(x.x), exp(x.y), exp(x.z), exp(x.w)); }


        /// <summary>Returns the base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double exp(double x) { return System.Math.Exp(x); }

        /// <summary>Returns the componentwise base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 exp(double2 x) { return new double2(exp(x.x), exp(x.y)); }

        /// <summary>Returns the componentwise base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 exp(double3 x) { return new double3(exp(x.x), exp(x.y), exp(x.z)); }

        /// <summary>Returns the componentwise base-e exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-e exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 exp(double4 x) { return new double4(exp(x.x), exp(x.y), exp(x.z), exp(x.w)); }


        /// <summary>Returns the base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float exp2(float x) { return (float)System.Math.Exp((float)x * 0.69314718f); }

        /// <summary>Returns the componentwise base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 exp2(float2 x) { return new float2(exp2(x.x), exp2(x.y)); }

        /// <summary>Returns the componentwise base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 exp2(float3 x) { return new float3(exp2(x.x), exp2(x.y), exp2(x.z)); }

        /// <summary>Returns the componentwise base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 exp2(float4 x) { return new float4(exp2(x.x), exp2(x.y), exp2(x.z), exp2(x.w)); }


        /// <summary>Returns the base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double exp2(double x) { return System.Math.Exp(x * 0.693147180559945309); }

        /// <summary>Returns the componentwise base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 exp2(double2 x) { return new double2(exp2(x.x), exp2(x.y)); }

        /// <summary>Returns the componentwise base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 exp2(double3 x) { return new double3(exp2(x.x), exp2(x.y), exp2(x.z)); }

        /// <summary>Returns the componentwise base-2 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 exp2(double4 x) { return new double4(exp2(x.x), exp2(x.y), exp2(x.z), exp2(x.w)); }


        /// <summary>Returns the base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float exp10(float x) { return (float)System.Math.Exp((float)x * 2.30258509f); }

        /// <summary>Returns the componentwise base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 exp10(float2 x) { return new float2(exp10(x.x), exp10(x.y)); }

        /// <summary>Returns the componentwise base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 exp10(float3 x) { return new float3(exp10(x.x), exp10(x.y), exp10(x.z)); }

        /// <summary>Returns the componentwise base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 exp10(float4 x) { return new float4(exp10(x.x), exp10(x.y), exp10(x.z), exp10(x.w)); }


        /// <summary>Returns the base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double exp10(double x) { return System.Math.Exp(x * 2.302585092994045684); }

        /// <summary>Returns the componentwise base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 exp10(double2 x) { return new double2(exp10(x.x), exp10(x.y)); }

        /// <summary>Returns the componentwise base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 exp10(double3 x) { return new double3(exp10(x.x), exp10(x.y), exp10(x.z)); }

        /// <summary>Returns the componentwise base-10 exponential of x.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 exponential of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 exp10(double4 x) { return new double4(exp10(x.x), exp10(x.y), exp10(x.z), exp10(x.w)); }


        /// <summary>Returns the natural logarithm of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float log(float x) { return (float)System.Math.Log((float)x); }

        /// <summary>Returns the componentwise natural logarithm of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 log(float2 x) { return new float2(log(x.x), log(x.y)); }

        /// <summary>Returns the componentwise natural logarithm of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 log(float3 x) { return new float3(log(x.x), log(x.y), log(x.z)); }

        /// <summary>Returns the componentwise natural logarithm of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 log(float4 x) { return new float4(log(x.x), log(x.y), log(x.z), log(x.w)); }


        /// <summary>Returns the natural logarithm of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double log(double x) { return System.Math.Log(x); }

        /// <summary>Returns the componentwise natural logarithm of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 log(double2 x) { return new double2(log(x.x), log(x.y)); }

        /// <summary>Returns the componentwise natural logarithm of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 log(double3 x) { return new double3(log(x.x), log(x.y), log(x.z)); }

        /// <summary>Returns the componentwise natural logarithm of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise natural logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 log(double4 x) { return new double4(log(x.x), log(x.y), log(x.z), log(x.w)); }


        /// <summary>Returns the base-2 logarithm of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float log2(float x) { return (float)System.Math.Log((float)x, 2.0f); }

        /// <summary>Returns the componentwise base-2 logarithm of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 log2(float2 x) { return new float2(log2(x.x), log2(x.y)); }

        /// <summary>Returns the componentwise base-2 logarithm of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 log2(float3 x) { return new float3(log2(x.x), log2(x.y), log2(x.z)); }

        /// <summary>Returns the componentwise base-2 logarithm of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 log2(float4 x) { return new float4(log2(x.x), log2(x.y), log2(x.z), log2(x.w)); }


        /// <summary>Returns the base-2 logarithm of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double log2(double x) { return System.Math.Log(x, 2.0); }

        /// <summary>Returns the componentwise base-2 logarithm of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 log2(double2 x) { return new double2(log2(x.x), log2(x.y)); }

        /// <summary>Returns the componentwise base-2 logarithm of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 log2(double3 x) { return new double3(log2(x.x), log2(x.y), log2(x.z)); }

        /// <summary>Returns the componentwise base-2 logarithm of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-2 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 log2(double4 x) { return new double4(log2(x.x), log2(x.y), log2(x.z), log2(x.w)); }

        /// <summary>Returns the base-10 logarithm of a float value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float log10(float x) { return (float)System.Math.Log10((float)x); }

        /// <summary>Returns the componentwise base-10 logarithm of a float2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 log10(float2 x) { return new float2(log10(x.x), log10(x.y)); }

        /// <summary>Returns the componentwise base-10 logarithm of a float3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 log10(float3 x) { return new float3(log10(x.x), log10(x.y), log10(x.z)); }

        /// <summary>Returns the componentwise base-10 logarithm of a float4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 log10(float4 x) { return new float4(log10(x.x), log10(x.y), log10(x.z), log10(x.w)); }


        /// <summary>Returns the base-10 logarithm of a double value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double log10(double x) { return System.Math.Log10(x); }

        /// <summary>Returns the componentwise base-10 logarithm of a double2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 log10(double2 x) { return new double2(log10(x.x), log10(x.y)); }

        /// <summary>Returns the componentwise base-10 logarithm of a double3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 log10(double3 x) { return new double3(log10(x.x), log10(x.y), log10(x.z)); }

        /// <summary>Returns the componentwise base-10 logarithm of a double4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise base-10 logarithm of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 log10(double4 x) { return new double4(log10(x.x), log10(x.y), log10(x.z), log10(x.w)); }


        /// <summary>Returns the floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float fmod(float x, float y) { return x % y; }

        /// <summary>Returns the componentwise floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 fmod(float2 x, float2 y) { return new float2(x.x % y.x, x.y % y.y); }

        /// <summary>Returns the componentwise floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 fmod(float3 x, float3 y) { return new float3(x.x % y.x, x.y % y.y, x.z % y.z); }

        /// <summary>Returns the componentwise floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 fmod(float4 x, float4 y) { return new float4(x.x % y.x, x.y % y.y, x.z % y.z, x.w % y.w); }


        /// <summary>Returns the double precision floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double fmod(double x, double y) { return x % y; }

        /// <summary>Returns the componentwise double precision floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 fmod(double2 x, double2 y) { return new double2(x.x % y.x, x.y % y.y); }

        /// <summary>Returns the componentwise double precision floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 fmod(double3 x, double3 y) { return new double3(x.x % y.x, x.y % y.y, x.z % y.z); }

        /// <summary>Returns the componentwise double precision floating point remainder of x/y.</summary>
        /// <param name="x">The dividend in x/y.</param>
        /// <param name="y">The divisor in x/y.</param>
        /// <returns>The componentwise remainder of x/y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 fmod(double4 x, double4 y) { return new double4(x.x % y.x, x.y % y.y, x.z % y.z, x.w % y.w); }


        /// <summary>Splits a float value into an integral part i and a fractional part that gets returned. Both parts take the sign of the input.</summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float modf(float x, out float i) { i = trunc(x); return x - i; }

        /// <summary>
        /// Performs a componentwise split of a float2 vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 modf(float2 x, out float2 i) { i = trunc(x); return x - i; }

        /// <summary>
        /// Performs a componentwise split of a float3 vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 modf(float3 x, out float3 i) { i = trunc(x); return x - i; }

        /// <summary>
        /// Performs a componentwise split of a float4 vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 modf(float4 x, out float4 i) { i = trunc(x); return x - i; }


        /// <summary>Splits a double value into an integral part i and a fractional part that gets returned. Both parts take the sign of the input.</summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double modf(double x, out double i) { i = trunc(x); return x - i; }

        /// <summary>
        /// Performs a componentwise split of a double2 vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 modf(double2 x, out double2 i) { i = trunc(x); return x - i; }

        /// <summary>
        /// Performs a componentwise split of a double3 vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 modf(double3 x, out double3 i) { i = trunc(x); return x - i; }

        /// <summary>
        /// Performs a componentwise split of a double4 vector into an integral part i and a fractional part that gets returned.
        /// Both parts take the sign of the corresponding input component.
        /// </summary>
        /// <param name="x">Value to split into integral and fractional part.</param>
        /// <param name="i">Output value containing integral part of x.</param>
        /// <returns>The componentwise fractional part of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 modf(double4 x, out double4 i) { i = trunc(x); return x - i; }


        /// <summary>Returns the square root of a float value.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sqrt(float x) { return (float)System.Math.Sqrt((float)x); }

        /// <summary>Returns the componentwise square root of a float2 vector.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The componentwise square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 sqrt(float2 x) { return new float2(sqrt(x.x), sqrt(x.y)); }

        /// <summary>Returns the componentwise square root of a float3 vector.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The componentwise square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 sqrt(float3 x) { return new float3(sqrt(x.x), sqrt(x.y), sqrt(x.z)); }

        /// <summary>Returns the componentwise square root of a float4 vector.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The componentwise square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 sqrt(float4 x) { return new float4(sqrt(x.x), sqrt(x.y), sqrt(x.z), sqrt(x.w)); }


        /// <summary>Returns the square root of a double value.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double sqrt(double x) { return System.Math.Sqrt(x); }

        /// <summary>Returns the componentwise square root of a double2 vector.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The componentwise square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 sqrt(double2 x) { return new double2(sqrt(x.x), sqrt(x.y)); }

        /// <summary>Returns the componentwise square root of a double3 vector.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The componentwise square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 sqrt(double3 x) { return new double3(sqrt(x.x), sqrt(x.y), sqrt(x.z)); }

        /// <summary>Returns the componentwise square root of a double4 vector.</summary>
        /// <param name="x">Value to use when computing square root.</param>
        /// <returns>The componentwise square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 sqrt(double4 x) { return new double4(sqrt(x.x), sqrt(x.y), sqrt(x.z), sqrt(x.w)); }


        /// <summary>Returns the reciprocal square root of a float value.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float rsqrt(float x) { return 1.0f / sqrt(x); }

        /// <summary>Returns the componentwise reciprocal square root of a float2 vector.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The componentwise reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 rsqrt(float2 x) { return 1.0f / sqrt(x); }

        /// <summary>Returns the componentwise reciprocal square root of a float3 vector.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The componentwise reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 rsqrt(float3 x) { return 1.0f / sqrt(x); }

        /// <summary>Returns the componentwise reciprocal square root of a float4 vector</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The componentwise reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 rsqrt(float4 x) { return 1.0f / sqrt(x); }


        /// <summary>Returns the reciprocal square root of a double value.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double rsqrt(double x) { return 1.0 / sqrt(x); }

        /// <summary>Returns the componentwise reciprocal square root of a double2 vector.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The componentwise reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 rsqrt(double2 x) { return 1.0 / sqrt(x); }

        /// <summary>Returns the componentwise reciprocal square root of a double3 vector.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The componentwise reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 rsqrt(double3 x) { return 1.0 / sqrt(x); }

        /// <summary>Returns the componentwise reciprocal square root of a double4 vector.</summary>
        /// <param name="x">Value to use when computing reciprocal square root.</param>
        /// <returns>The componentwise reciprocal square root.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 rsqrt(double4 x) { return 1.0 / sqrt(x); }


        /// <summary>Returns a normalized version of the float2 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 normalize(float2 x) { return rsqrt(dot(x, x)) * x; }

        /// <summary>Returns a normalized version of the float3 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 normalize(float3 x) { return rsqrt(dot(x, x)) * x; }

        /// <summary>Returns a normalized version of the float4 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 normalize(float4 x) { return rsqrt(dot(x, x)) * x; }


        /// <summary>Returns a normalized version of the double2 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 normalize(double2 x) { return rsqrt(dot(x, x)) * x; }

        /// <summary>Returns a normalized version of the double3 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 normalize(double3 x) { return rsqrt(dot(x, x)) * x; }

        /// <summary>Returns a normalized version of the double4 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 normalize(double4 x) { return rsqrt(dot(x, x)) * x; }


        /// <summary>
        /// Returns a safe normalized version of the float2 vector x by scaling it by 1 / length(x).
        /// Returns the given default value when 1 / length(x) does not produce a finite number.
        /// </summary>
        /// <param name="x">Vector to normalize.</param>
        /// <param name="defaultvalue">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float2 normalizesafe(float2 x, float2 defaultvalue = new float2())
        {
            float len = math.dot(x, x);
            return math.select(defaultvalue, x * math.rsqrt(len), len > FLT_MIN_NORMAL);
        }

        /// <summary>
        /// Returns a safe normalized version of the float3 vector x by scaling it by 1 / length(x).
        /// Returns the given default value when 1 / length(x) does not produce a finite number.
        /// </summary>
        /// <param name="x">Vector to normalize.</param>
        /// <param name="defaultvalue">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float3 normalizesafe(float3 x, float3 defaultvalue = new float3())
        {
            float len = math.dot(x, x);
            return math.select(defaultvalue, x * math.rsqrt(len), len > FLT_MIN_NORMAL);
        }

        /// <summary>
        /// Returns a safe normalized version of the float4 vector x by scaling it by 1 / length(x).
        /// Returns the given default value when 1 / length(x) does not produce a finite number.
        /// </summary>
        /// <param name="x">Vector to normalize.</param>
        /// <param name="defaultvalue">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float4 normalizesafe(float4 x, float4 defaultvalue = new float4())
        {
            float len = math.dot(x, x);
            return math.select(defaultvalue, x * math.rsqrt(len), len > FLT_MIN_NORMAL);
        }


        /// <summary>
        /// Returns a safe normalized version of the double4 vector x by scaling it by 1 / length(x).
        /// Returns the given default value when 1 / length(x) does not produce a finite number.
        /// </summary>
        /// <param name="x">Vector to normalize.</param>
        /// <param name="defaultvalue">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public double2 normalizesafe(double2 x, double2 defaultvalue = new double2())
        {
            double len = math.dot(x, x);
            return math.select(defaultvalue, x * math.rsqrt(len), len > FLT_MIN_NORMAL);
        }

        /// <summary>
        /// Returns a safe normalized version of the double4 vector x by scaling it by 1 / length(x).
        /// Returns the given default value when 1 / length(x) does not produce a finite number.
        /// </summary>
        /// <param name="x">Vector to normalize.</param>
        /// <param name="defaultvalue">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public double3 normalizesafe(double3 x, double3 defaultvalue = new double3())
        {
            double len = math.dot(x, x);
            return math.select(defaultvalue, x * math.rsqrt(len), len > FLT_MIN_NORMAL);
        }

        /// <summary>
        /// Returns a safe normalized version of the double4 vector x by scaling it by 1 / length(x).
        /// Returns the given default value when 1 / length(x) does not produce a finite number.
        /// </summary>
        /// <param name="x">Vector to normalize.</param>
        /// <param name="defaultvalue">Vector to return if normalized vector is not finite.</param>
        /// <returns>The normalized vector or the default value if the normalized vector is not finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public double4 normalizesafe(double4 x, double4 defaultvalue = new double4())
        {
            double len = math.dot(x, x);
            return math.select(defaultvalue, x * math.rsqrt(len), len > FLT_MIN_NORMAL);
        }


        /// <summary>Returns the length of a float value. Equivalent to the absolute value.</summary>
        /// <param name="x">Value to use when computing length.</param>
        /// <returns>Length of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float x) { return abs(x); }

        /// <summary>Returns the length of a float2 vector.</summary>
        /// <param name="x">Vector to use when computing length.</param>
        /// <returns>Length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float2 x) { return sqrt(dot(x, x)); }

        /// <summary>Returns the length of a float3 vector.</summary>
        /// <param name="x">Vector to use when computing length.</param>
        /// <returns>Length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float3 x) { return sqrt(dot(x, x)); }

        /// <summary>Returns the length of a float4 vector.</summary>
        /// <param name="x">Vector to use when computing length.</param>
        /// <returns>Length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float length(float4 x) { return sqrt(dot(x, x)); }


        /// <summary>Returns the length of a double value. Equivalent to the absolute value.</summary>
        /// <param name="x">Value to use when computing squared length.</param>
        /// <returns>Squared length of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double length(double x) { return abs(x); }

        /// <summary>Returns the length of a double2 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double length(double2 x) { return sqrt(dot(x, x)); }

        /// <summary>Returns the length of a double3 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double length(double3 x) { return sqrt(dot(x, x)); }

        /// <summary>Returns the length of a double4 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double length(double4 x) { return sqrt(dot(x, x)); }


        /// <summary>Returns the squared length of a float value. Equivalent to squaring the value.</summary>
        /// <param name="x">Value to use when computing squared length.</param>
        /// <returns>Squared length of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float x) { return x*x; }

        /// <summary>Returns the squared length of a float2 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float2 x) { return dot(x, x); }

        /// <summary>Returns the squared length of a float3 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float3 x) { return dot(x, x); }

        /// <summary>Returns the squared length of a float4 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lengthsq(float4 x) { return dot(x, x); }


        /// <summary>Returns the squared length of a double value. Equivalent to squaring the value.</summary>
        /// <param name="x">Value to use when computing squared length.</param>
        /// <returns>Squared length of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double lengthsq(double x) { return x * x; }

        /// <summary>Returns the squared length of a double2 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double lengthsq(double2 x) { return dot(x, x); }

        /// <summary>Returns the squared length of a double3 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double lengthsq(double3 x) { return dot(x, x); }

        /// <summary>Returns the squared length of a double4 vector.</summary>
        /// <param name="x">Vector to use when computing squared length.</param>
        /// <returns>Squared length of vector x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double lengthsq(double4 x) { return dot(x, x); }


        /// <summary>Returns the distance between two float values.</summary>
        /// <param name="x">First value to use in distance computation.</param>
        /// <param name="y">Second value to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float x, float y) { return abs(y - x); }

        /// <summary>Returns the distance between two float2 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float2 x, float2 y) { return length(y - x); }

        /// <summary>Returns the distance between two float3 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float3 x, float3 y) { return length(y - x); }

        /// <summary>Returns the distance between two float4 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distance(float4 x, float4 y) { return length(y - x); }


        /// <summary>Returns the distance between two double values.</summary>
        /// <param name="x">First value to use in distance computation.</param>
        /// <param name="y">Second value to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distance(double x, double y) { return abs(y - x); }

        /// <summary>Returns the distance between two double2 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distance(double2 x, double2 y) { return length(y - x); }

        /// <summary>Returns the distance between two double3 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distance(double3 x, double3 y) { return length(y - x); }

        /// <summary>Returns the distance between two double4 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distance(double4 x, double4 y) { return length(y - x); }


        /// <summary>Returns the squared distance between two float values.</summary>
        /// <param name="x">First value to use in distance computation.</param>
        /// <param name="y">Second value to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distancesq(float x, float y) { return (y - x) * (y - x); }

        /// <summary>Returns the squared distance between two float2 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distancesq(float2 x, float2 y) { return lengthsq(y - x); }

        /// <summary>Returns the squared distance between two float3 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distancesq(float3 x, float3 y) { return lengthsq(y - x); }

        /// <summary>Returns the squared distance between two float4 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distancesq(float4 x, float4 y) { return lengthsq(y - x); }


        /// <summary>Returns the squared distance between two double values.</summary>
        /// <param name="x">First value to use in distance computation.</param>
        /// <param name="y">Second value to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distancesq(double x, double y) { return (y - x) * (y - x); }

        /// <summary>Returns the squared distance between two double2 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distancesq(double2 x, double2 y) { return lengthsq(y - x); }

        /// <summary>Returns the squared distance between two double3 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distancesq(double3 x, double3 y) { return lengthsq(y - x); }

        /// <summary>Returns the squared distance between two double4 vectors.</summary>
        /// <param name="x">First vector to use in distance computation.</param>
        /// <param name="y">Second vector to use in distance computation.</param>
        /// <returns>The squared distance between x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double distancesq(double4 x, double4 y) { return lengthsq(y - x); }


        /// <summary>Returns the cross product of two float3 vectors.</summary>
        /// <param name="x">First vector to use in cross product.</param>
        /// <param name="y">Second vector to use in cross product.</param>
        /// <returns>The cross product of x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 cross(float3 x, float3 y) { return (x * y.yzx - x.yzx * y).yzx; }

        /// <summary>Returns the cross product of two double3 vectors.</summary>
        /// <param name="x">First vector to use in cross product.</param>
        /// <param name="y">Second vector to use in cross product.</param>
        /// <returns>The cross product of x and y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 cross(double3 x, double3 y) { return (x * y.yzx - x.yzx * y).yzx; }


        /// <summary>Returns a smooth Hermite interpolation between 0.0f and 1.0f when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns a value camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float smoothstep(float xMin, float xMax, float x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0f - (2.0f * t));
        }

        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0f and 1.0f when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 smoothstep(float2 xMin, float2 xMax, float2 x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0f - (2.0f * t));
        }

        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0f and 1.0f when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 smoothstep(float3 xMin, float3 xMax, float3 x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0f - (2.0f * t));
        }

        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0f and 1.0f when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 smoothstep(float4 xMin, float4 xMax, float4 x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0f - (2.0f * t));
        }


        /// <summary>Returns a smooth Hermite interpolation between 0.0 and 1.0 when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns a value camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double smoothstep(double xMin, double xMax, double x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0 - (2.0 * t));
        }

        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0 and 1.0 when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 smoothstep(double2 xMin, double2 xMax, double2 x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0 - (2.0 * t));
        }

        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0 and 1.0 when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 smoothstep(double3 xMin, double3 xMax, double3 x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0 - (2.0 * t));
        }

        /// <summary>Returns a componentwise smooth Hermite interpolation between 0.0 and 1.0 when x is in the interval (inclusive) [xMin, xMax].</summary>
        /// <param name="xMin">The minimum range of the x parameter.</param>
        /// <param name="xMax">The maximum range of the x parameter.</param>
        /// <param name="x">The value to be interpolated.</param>
        /// <returns>Returns component values camped to the range [0, 1].</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 smoothstep(double4 xMin, double4 xMax, double4 x)
        {
            var t = saturate((x - xMin) / (xMax - xMin));
            return t * t * (3.0 - (2.0 * t));
        }


        /// <summary>Returns true if any component of the input bool2 vector is true, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are true, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool2 x) { return x.x || x.y; }

        /// <summary>Returns true if any component of the input bool3 vector is true, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are true, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool3 x) { return x.x || x.y || x.z; }

        /// <summary>Returns true if any components of the input bool4 vector is true, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are true, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(bool4 x) { return x.x || x.y || x.z || x.w; }


        /// <summary>Returns true if any component of the input int2 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int2 x) { return x.x != 0 || x.y != 0; }

        /// <summary>Returns true if any component of the input int3 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int3 x) { return x.x != 0 || x.y != 0 || x.z != 0; }

        /// <summary>Returns true if any components of the input int4 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(int4 x) { return x.x != 0 || x.y != 0 || x.z != 0 || x.w != 0; }


        /// <summary>Returns true if any component of the input uint2 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(uint2 x) { return x.x != 0 || x.y != 0; }

        /// <summary>Returns true if any component of the input uint3 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(uint3 x) { return x.x != 0 || x.y != 0 || x.z != 0; }

        /// <summary>Returns true if any components of the input uint4 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(uint4 x) { return x.x != 0 || x.y != 0 || x.z != 0 || x.w != 0; }


        /// <summary>Returns true if any component of the input float2 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float2 x) { return x.x != 0.0f || x.y != 0.0f; }

        /// <summary>Returns true if any component of the input float3 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float3 x) { return x.x != 0.0f || x.y != 0.0f || x.z != 0.0f; }

        /// <summary>Returns true if any component of the input float4 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(float4 x) { return x.x != 0.0f || x.y != 0.0f || x.z != 0.0f || x.w != 0.0f; }


        /// <summary>Returns true if any component of the input double2 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(double2 x) { return x.x != 0.0 || x.y != 0.0; }

        /// <summary>Returns true if any component of the input double3 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(double3 x) { return x.x != 0.0 || x.y != 0.0 || x.z != 0.0; }

        /// <summary>Returns true if any component of the input double4 vector is non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if any the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool any(double4 x) { return x.x != 0.0 || x.y != 0.0 || x.z != 0.0 || x.w != 0.0; }


        /// <summary>Returns true if all components of the input bool2 vector are true, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are true, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool2 x) { return x.x && x.y; }

        /// <summary>Returns true if all components of the input bool3 vector are true, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are true, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool3 x) { return x.x && x.y && x.z; }

        /// <summary>Returns true if all components of the input bool4 vector are true, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are true, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(bool4 x) { return x.x && x.y && x.z && x.w; }


        /// <summary>Returns true if all components of the input int2 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int2 x) { return x.x != 0 && x.y != 0; }

        /// <summary>Returns true if all components of the input int3 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int3 x) { return x.x != 0 && x.y != 0 && x.z != 0; }

        /// <summary>Returns true if all components of the input int4 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(int4 x) { return x.x != 0 && x.y != 0 && x.z != 0 && x.w != 0; }


        /// <summary>Returns true if all components of the input uint2 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(uint2 x) { return x.x != 0 && x.y != 0; }

        /// <summary>Returns true if all components of the input uint3 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(uint3 x) { return x.x != 0 && x.y != 0 && x.z != 0; }

        /// <summary>Returns true if all components of the input uint4 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(uint4 x) { return x.x != 0 && x.y != 0 && x.z != 0 && x.w != 0; }


        /// <summary>Returns true if all components of the input float2 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float2 x) { return x.x != 0.0f && x.y != 0.0f; }

        /// <summary>Returns true if all components of the input float3 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float3 x) { return x.x != 0.0f && x.y != 0.0f && x.z != 0.0f; }

        /// <summary>Returns true if all components of the input float4 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(float4 x) { return x.x != 0.0f && x.y != 0.0f && x.z != 0.0f && x.w != 0.0f; }


        /// <summary>Returns true if all components of the input double2 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(double2 x) { return x.x != 0.0 && x.y != 0.0; }

        /// <summary>Returns true if all components of the input double3 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(double3 x) { return x.x != 0.0 && x.y != 0.0 && x.z != 0.0; }

        /// <summary>Returns true if all components of the input double4 vector are non-zero, false otherwise.</summary>
        /// <param name="x">Vector of values to compare.</param>
        /// <returns>True if all the components of x are non-zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool all(double4 x) { return x.x != 0.0 && x.y != 0.0 && x.z != 0.0 && x.w != 0.0; }


        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int select(int falseValue, int trueValue, bool test)    { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 select(int2 falseValue, int2 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 select(int3 falseValue, int3 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 select(int4 falseValue, int4 trueValue, bool test) { return test ? trueValue : falseValue; }


        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 select(int2 falseValue, int2 trueValue, bool2 test) { return new int2(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 select(int3 falseValue, int3 trueValue, bool3 test) { return new int3(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 select(int4 falseValue, int4 trueValue, bool4 test) { return new int4(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z, test.w ? trueValue.w : falseValue.w); }


        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint select(uint falseValue, uint trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 select(uint2 falseValue, uint2 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 select(uint3 falseValue, uint3 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 select(uint4 falseValue, uint4 trueValue, bool test) { return test ? trueValue : falseValue; }


        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 select(uint2 falseValue, uint2 trueValue, bool2 test) { return new uint2(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 select(uint3 falseValue, uint3 trueValue, bool3 test) { return new uint3(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 select(uint4 falseValue, uint4 trueValue, bool4 test) { return new uint4(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z, test.w ? trueValue.w : falseValue.w); }


        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long select(long falseValue, long trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong select(ulong falseValue, ulong trueValue, bool test) { return test ? trueValue : falseValue; }


        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float select(float falseValue, float trueValue, bool test)    { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 select(float2 falseValue, float2 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 select(float3 falseValue, float3 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 select(float4 falseValue, float4 trueValue, bool test) { return test ? trueValue : falseValue; }


        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 select(float2 falseValue, float2 trueValue, bool2 test) { return new float2(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 select(float3 falseValue, float3 trueValue, bool3 test) { return new float3(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 select(float4 falseValue, float4 trueValue, bool4 test) { return new float4(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z, test.w ? trueValue.w : falseValue.w); }


        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double select(double falseValue, double trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 select(double2 falseValue, double2 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 select(double3 falseValue, double3 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>Returns trueValue if test is true, falseValue otherwise.</summary>
        /// <param name="falseValue">Value to use if test is false.</param>
        /// <param name="trueValue">Value to use if test is true.</param>
        /// <param name="test">Bool value to choose between falseValue and trueValue.</param>
        /// <returns>The selection between falseValue and trueValue according to bool test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 select(double4 falseValue, double4 trueValue, bool test) { return test ? trueValue : falseValue; }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 select(double2 falseValue, double2 trueValue, bool2 test) { return new double2(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 select(double3 falseValue, double3 trueValue, bool3 test) { return new double3(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z); }

        /// <summary>
        /// Returns a componentwise selection between two double4 vectors falseValue and trueValue based on a bool4 selection mask test.
        /// Per component, the component from trueValue is selected when test is true, otherwise the component from falseValue is selected.
        /// </summary>
        /// <param name="falseValue">Values to use if test is false.</param>
        /// <param name="trueValue">Values to use if test is true.</param>
        /// <param name="test">Selection mask to choose between falseValue and trueValue.</param>
        /// <returns>The componentwise selection between falseValue and trueValue according to selection mask test.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 select(double4 falseValue, double4 trueValue, bool4 test) { return new double4(test.x ? trueValue.x : falseValue.x, test.y ? trueValue.y : falseValue.y, test.z ? trueValue.z : falseValue.z, test.w ? trueValue.w : falseValue.w); }


        /// <summary>Returns the result of a step function where the result is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Value to be used as a threshold for returning 1.</param>
        /// <param name="x">Value to compare against threshold.</param>
        /// <returns>1 if the comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float step(float threshold, float x) { return select(0.0f, 1.0f, x >= threshold); }

        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name="x">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 step(float2 threshold, float2 x) { return select(float2(0.0f), float2(1.0f), x >= threshold); }

        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name="x">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 step(float3 threshold, float3 x) { return select(float3(0.0f), float3(1.0f), x >= threshold); }

        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name="x">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 step(float4 threshold, float4 x) { return select(float4(0.0f), float4(1.0f), x >= threshold); }


        /// <summary>Returns the result of a step function where the result is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Values to be used as a threshold for returning 1.</param>
        /// <param name="x">Value to compare against threshold.</param>
        /// <returns>1 if the comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double step(double threshold, double x) { return select(0.0, 1.0, x >= threshold); }

        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name="x">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 step(double2 threshold, double2 x) { return select(double2(0.0), double2(1.0), x >= threshold); }

        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name="x">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 step(double3 threshold, double3 x) { return select(double3(0.0), double3(1.0), x >= threshold); }

        /// <summary>Returns the result of a componentwise step function where each component is 1.0f when x &gt;= threshold and 0.0f otherwise.</summary>
        /// <param name="threshold">Vector of values to be used as a threshold for returning 1.</param>
        /// <param name="x">Vector of values to compare against threshold.</param>
        /// <returns>1 if the componentwise comparison x &gt;= threshold is true, otherwise 0.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 step(double4 threshold, double4 x) { return select(double4(0.0), double4(1.0), x >= threshold); }


        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0f * dot(i, n) * n.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 reflect(float2 i, float2 n) { return i - 2f * n * dot(i, n); }

        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0f * dot(i, n) * n.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 reflect(float3 i, float3 n) { return i - 2f * n * dot(i, n); }

        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0f * dot(i, n) * n.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 reflect(float4 i, float4 n) { return i - 2f * n * dot(i, n); }


        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0 * dot(i, n) * n.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 reflect(double2 i, double2 n) { return i - 2 * n * dot(i, n); }

        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0 * dot(i, n) * n.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 reflect(double3 i, double3 n) { return i - 2 * n * dot(i, n); }

        /// <summary>Given an incident vector i and a normal vector n, returns the reflection vector r = i - 2.0 * dot(i, n) * n.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <returns>Reflection vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 reflect(double4 i, double4 n) { return i - 2 * n * dot(i, n); }


        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <param name="indexOfRefraction">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 refract(float2 i, float2 n, float indexOfRefraction)
        {
            float ni = dot(n, i);
            float k = 1.0f - indexOfRefraction * indexOfRefraction * (1.0f - ni * ni);
            return select(0.0f, indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }

        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <param name="indexOfRefraction">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 refract(float3 i, float3 n, float indexOfRefraction)
        {
            float ni = dot(n, i);
            float k = 1.0f - indexOfRefraction * indexOfRefraction * (1.0f - ni * ni);
            return select(0.0f, indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }

        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <param name="indexOfRefraction">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 refract(float4 i, float4 n, float indexOfRefraction)
        {
            float ni = dot(n, i);
            float k = 1.0f - indexOfRefraction * indexOfRefraction * (1.0f - ni * ni);
            return select(0.0f, indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }


        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <param name="indexOfRefraction">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 refract(double2 i, double2 n, double indexOfRefraction)
        {
            double ni = dot(n, i);
            double k = 1.0 - indexOfRefraction * indexOfRefraction * (1.0 - ni * ni);
            return select(0.0f, indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }

        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <param name="indexOfRefraction">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 refract(double3 i, double3 n, double indexOfRefraction)
        {
            double ni = dot(n, i);
            double k = 1.0 - indexOfRefraction * indexOfRefraction * (1.0 - ni * ni);
            return select(0.0f, indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }

        /// <summary>Returns the refraction vector given the incident vector i, the normal vector n and the refraction index.</summary>
        /// <param name="i">Incident vector.</param>
        /// <param name="n">Normal vector.</param>
        /// <param name="indexOfRefraction">Index of refraction.</param>
        /// <returns>Refraction vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 refract(double4 i, double4 n, double indexOfRefraction)
        {
            double ni = dot(n, i);
            double k = 1.0 - indexOfRefraction * indexOfRefraction * (1.0 - ni * ni);
            return select(0.0f, indexOfRefraction * i - (indexOfRefraction * ni + sqrt(k)) * n, k >= 0);
        }

        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Single.MaxValue) or when b's components are very small (close to FLT_MIN_NORMAL).
        /// In these cases, you can call <see cref="projectsafe(Unity.Mathematics.float2,Unity.Mathematics.float2,Unity.Mathematics.float2)"/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 project(float2 a, float2 ontoB)
        {
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }

        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Single.MaxValue) or when b's components are very small (close to FLT_MIN_NORMAL).
        /// In these cases, you can call <see cref="projectsafe(Unity.Mathematics.float3,Unity.Mathematics.float3,Unity.Mathematics.float3)"/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 project(float3 a, float3 ontoB)
        {
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }

        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Single.MaxValue) or when b's components are very small (close to FLT_MIN_NORMAL).
        /// In these cases, you can call <see cref="projectsafe(Unity.Mathematics.float4,Unity.Mathematics.float4,Unity.Mathematics.float4)"/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 project(float4 a, float4 ontoB)
        {
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }

        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref="project(Unity.Mathematics.float2,Unity.Mathematics.float2)"/> instead which is faster than this
        /// function.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <param name="defaultValue">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 projectsafe(float2 a, float2 ontoB, float2 defaultValue = new float2())
        {
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }

        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref="project(Unity.Mathematics.float3,Unity.Mathematics.float3)"/> instead which is faster than this
        /// function.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <param name="defaultValue">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 projectsafe(float3 a, float3 ontoB, float3 defaultValue = new float3())
        {
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }

        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref="project(Unity.Mathematics.float4,Unity.Mathematics.float4)"/> instead which is faster than this
        /// function.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <param name="defaultValue">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 projectsafe(float4 a, float4 ontoB, float4 defaultValue = new float4())
        {
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }

        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Double.MaxValue) or when b's components are very small (close to DBL_MIN_NORMAL).
        /// In these cases, you can call <see cref="projectsafe(Unity.Mathematics.double2,Unity.Mathematics.double2,Unity.Mathematics.double2)"/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 project(double2 a, double2 ontoB)
        {
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }

        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Double.MaxValue) or when b's components are very small (close to DBL_MIN_NORMAL).
        /// In these cases, you can call <see cref="projectsafe(Unity.Mathematics.double3,Unity.Mathematics.double3,Unity.Mathematics.double3)"/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 project(double3 a, double3 ontoB)
        {
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }

        /// <summary>
        /// Compute vector projection of a onto b.
        /// </summary>
        /// <remarks>
        /// Some finite vectors a and b could generate a non-finite result. This is most likely when a's components
        /// are very large (close to Double.MaxValue) or when b's components are very small (close to DBL_MIN_NORMAL).
        /// In these cases, you can call <see cref="projectsafe(Unity.Mathematics.double4,Unity.Mathematics.double4,Unity.Mathematics.double4)"/>
        /// which will use a given default value if the result is not finite.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <returns>Vector projection of a onto b.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 project(double4 a, double4 ontoB)
        {
            return (dot(a, ontoB) / dot(ontoB, ontoB)) * ontoB;
        }

        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref="project(Unity.Mathematics.double2,Unity.Mathematics.double2)"/> instead which is faster than this
        /// function.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <param name="defaultValue">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 projectsafe(double2 a, double2 ontoB, double2 defaultValue = new double2())
        {
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }

        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref="project(Unity.Mathematics.double3,Unity.Mathematics.double3)"/> instead which is faster than this
        /// function.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <param name="defaultValue">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 projectsafe(double3 a, double3 ontoB, double3 defaultValue = new double3())
        {
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }

        /// <summary>
        /// Compute vector projection of a onto b. If result is not finite, then return the default value instead.
        /// </summary>
        /// <remarks>
        /// This function performs extra checks to see if the result of projecting a onto b is finite. If you know that
        /// your inputs will generate a finite result or you don't care if the result is finite, then you can call
        /// <see cref="project(Unity.Mathematics.double4,Unity.Mathematics.double4)"/> instead which is faster than this
        /// function.
        /// </remarks>
        /// <param name="a">Vector to project.</param>
        /// <param name="ontoB">Non-zero vector to project onto.</param>
        /// <param name="defaultValue">Default value to return if projection is not finite.</param>
        /// <returns>Vector projection of a onto b or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 projectsafe(double4 a, double4 ontoB, double4 defaultValue = new double4())
        {
            var proj = project(a, ontoB);

            return select(defaultValue, proj, all(isfinite(proj)));
        }

        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name="n">Vector to conditionally flip.</param>
        /// <param name="i">First vector in direction comparison.</param>
        /// <param name="ng">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 faceforward(float2 n, float2 i, float2 ng) { return select(n, -n, dot(ng, i) >= 0.0f); }

        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name="n">Vector to conditionally flip.</param>
        /// <param name="i">First vector in direction comparison.</param>
        /// <param name="ng">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 faceforward(float3 n, float3 i, float3 ng) { return select(n, -n, dot(ng, i) >= 0.0f); }

        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name="n">Vector to conditionally flip.</param>
        /// <param name="i">First vector in direction comparison.</param>
        /// <param name="ng">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 faceforward(float4 n, float4 i, float4 ng) { return select(n, -n, dot(ng, i) >= 0.0f); }


        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name="n">Vector to conditionally flip.</param>
        /// <param name="i">First vector in direction comparison.</param>
        /// <param name="ng">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 faceforward(double2 n, double2 i, double2 ng) { return select(n, -n, dot(ng, i) >= 0.0f); }

        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name="n">Vector to conditionally flip.</param>
        /// <param name="i">First vector in direction comparison.</param>
        /// <param name="ng">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 faceforward(double3 n, double3 i, double3 ng) { return select(n, -n, dot(ng, i) >= 0.0f); }

        /// <summary>Conditionally flips a vector n if two vectors i and ng are pointing in the same direction. Returns n if dot(i, ng) &lt; 0, -n otherwise.</summary>
        /// <param name="n">Vector to conditionally flip.</param>
        /// <param name="i">First vector in direction comparison.</param>
        /// <param name="ng">Second vector in direction comparison.</param>
        /// <returns>-n if i and ng point in the same direction; otherwise return n unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 faceforward(double4 n, double4 i, double4 ng) { return select(n, -n, dot(ng, i) >= 0.0f); }


        /// <summary>Returns the sine and cosine of the input float value x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input angle in radians.</param>
        /// <param name="s">Output sine of the input.</param>
        /// <param name="c">Output cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float x, out float s, out float c) { s = sin(x); c = cos(x); }

        /// <summary>Returns the componentwise sine and cosine of the input float2 vector x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input vector containing angles in radians.</param>
        /// <param name="s">Output vector containing the componentwise sine of the input.</param>
        /// <param name="c">Output vector containing the componentwise cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float2 x, out float2 s, out float2 c) { s = sin(x); c = cos(x); }

        /// <summary>Returns the componentwise sine and cosine of the input float3 vector x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input vector containing angles in radians.</param>
        /// <param name="s">Output vector containing the componentwise sine of the input.</param>
        /// <param name="c">Output vector containing the componentwise cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float3 x, out float3 s, out float3 c) { s = sin(x); c = cos(x); }

        /// <summary>Returns the componentwise sine and cosine of the input float4 vector x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input vector containing angles in radians.</param>
        /// <param name="s">Output vector containing the componentwise sine of the input.</param>
        /// <param name="c">Output vector containing the componentwise cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(float4 x, out float4 s, out float4 c) { s = sin(x); c = cos(x); }


        /// <summary>Returns the sine and cosine of the input double value x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input angle in radians.</param>
        /// <param name="s">Output sine of the input.</param>
        /// <param name="c">Output cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(double x, out double s, out double c) { s = sin(x); c = cos(x); }

        /// <summary>Returns the componentwise sine and cosine of the input double2 vector x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input vector containing angles in radians.</param>
        /// <param name="s">Output vector containing the componentwise sine of the input.</param>
        /// <param name="c">Output vector containing the componentwise cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(double2 x, out double2 s, out double2 c) { s = sin(x); c = cos(x); }

        /// <summary>Returns the componentwise sine and cosine of the input double3 vector x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input vector containing angles in radians.</param>
        /// <param name="s">Output vector containing the componentwise sine of the input.</param>
        /// <param name="c">Output vector containing the componentwise cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(double3 x, out double3 s, out double3 c) { s = sin(x); c = cos(x); }

        /// <summary>Returns the componentwise sine and cosine of the input double4 vector x through the out parameters s and c.</summary>
        /// <remarks>When Burst compiled, his method is faster than calling sin() and cos() separately.</remarks>
        /// <param name="x">Input vector containing angles in radians.</param>
        /// <param name="s">Output vector containing the componentwise sine of the input.</param>
        /// <param name="c">Output vector containing the componentwise cosine of the input.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void sincos(double4 x, out double4 s, out double4 c) { s = sin(x); c = cos(x); }


        /// <summary>Returns number of 1-bits in the binary representation of an int value. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">int value in which to count bits set to 1.</param>
        /// <returns>Number of bits set to 1 within x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countbits(int x) { return countbits((uint)x); }

        /// <summary>Returns component-wise number of 1-bits in the binary representation of an int2 vector. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">int2 value in which to count bits for each component.</param>
        /// <returns>int2 containing number of bits set to 1 within each component of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 countbits(int2 x) { return countbits((uint2)x); }

        /// <summary>Returns component-wise number of 1-bits in the binary representation of an int3 vector. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>int3 containing number of bits set to 1 within each component of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 countbits(int3 x) { return countbits((uint3)x); }

        /// <summary>Returns component-wise number of 1-bits in the binary representation of an int4 vector. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>int4 containing number of bits set to 1 within each component of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 countbits(int4 x) { return countbits((uint4)x); }


        /// <summary>Returns number of 1-bits in the binary representation of a uint value. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>Number of bits set to 1 within x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countbits(uint x)
        {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return (int)((((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24);
        }

        /// <summary>Returns component-wise number of 1-bits in the binary representation of a uint2 vector. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>int2 containing number of bits set to 1 within each component of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 countbits(uint2 x)
        {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return int2((((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24);
        }

        /// <summary>Returns component-wise number of 1-bits in the binary representation of a uint3 vector. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>int3 containing number of bits set to 1 within each component of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 countbits(uint3 x)
        {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return int3((((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24);
        }

        /// <summary>Returns component-wise number of 1-bits in the binary representation of a uint4 vector. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>int4 containing number of bits set to 1 within each component of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 countbits(uint4 x)
        {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            return int4((((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24);
        }

        /// <summary>Returns number of 1-bits in the binary representation of a ulong value. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>Number of bits set to 1 within x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countbits(ulong x)
        {
            x = x - ((x >> 1) & 0x5555555555555555);
            x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
            return (int)((((x + (x >> 4)) & 0x0F0F0F0F0F0F0F0F) * 0x0101010101010101) >> 56);
        }

        /// <summary>Returns number of 1-bits in the binary representation of a long value. Also known as the Hamming weight, popcnt on x86, and vcnt on ARM.</summary>
        /// <param name="x">Number in which to count bits.</param>
        /// <returns>Number of bits set to 1 within x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countbits(long x) { return countbits((ulong)x); }


        /// <summary>Returns the componentwise number of leading zeros in the binary representations of an int vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int lzcnt(int x) { return lzcnt((uint)x); }

        /// <summary>Returns the componentwise number of leading zeros in the binary representations of an int2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 lzcnt(int2 x) { return int2(lzcnt(x.x), lzcnt(x.y)); }

        /// <summary>Returns the componentwise number of leading zeros in the binary representations of an int3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 lzcnt(int3 x) { return int3(lzcnt(x.x), lzcnt(x.y), lzcnt(x.z)); }

        /// <summary>Returns the componentwise number of leading zeros in the binary representations of an int4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 lzcnt(int4 x) { return int4(lzcnt(x.x), lzcnt(x.y), lzcnt(x.z), lzcnt(x.w)); }


        /// <summary>Returns number of leading zeros in the binary representations of a uint value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int lzcnt(uint x)
        {
            if (x == 0)
                return 32;
            LongDoubleUnion u;
            u.doubleValue = 0.0;
            u.longValue = 0x4330000000000000L + x;
            u.doubleValue -= 4503599627370496.0;
            return 0x41E - (int)(u.longValue >> 52);
        }

        /// <summary>Returns the componentwise number of leading zeros in the binary representations of a uint2 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 lzcnt(uint2 x) { return int2(lzcnt(x.x), lzcnt(x.y)); }

        /// <summary>Returns the componentwise number of leading zeros in the binary representations of a uint3 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 lzcnt(uint3 x) { return int3(lzcnt(x.x), lzcnt(x.y), lzcnt(x.z)); }

        /// <summary>Returns the componentwise number of leading zeros in the binary representations of a uint4 vector.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 lzcnt(uint4 x) { return int4(lzcnt(x.x), lzcnt(x.y), lzcnt(x.z), lzcnt(x.w)); }


        /// <summary>Returns number of leading zeros in the binary representations of a long value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int lzcnt(long x) { return lzcnt((ulong)x); }


        /// <summary>Returns number of leading zeros in the binary representations of a ulong value.</summary>
        /// <param name="x">Input value.</param>
        /// <returns>The number of leading zeros of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int lzcnt(ulong x)
        {
            if (x == 0)
                return 64;

            uint xh = (uint)(x >> 32);
            uint bits = xh != 0 ? xh : (uint)x;
            int offset = xh != 0 ? 0x41E : 0x43E;

            LongDoubleUnion u;
            u.doubleValue = 0.0;
            u.longValue = 0x4330000000000000L + bits;
            u.doubleValue -= 4503599627370496.0;
            return offset - (int)(u.longValue >> 52);
        }

        /// <summary>
        /// Computes the trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int tzcnt(int x) { return tzcnt((uint)x); }

        /// <summary>
        /// Computes the component-wise trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the component-wise trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 tzcnt(int2 x) { return int2(tzcnt(x.x), tzcnt(x.y)); }

        /// <summary>
        /// Computes the component-wise trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the component-wise trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 tzcnt(int3 x) { return int3(tzcnt(x.x), tzcnt(x.y), tzcnt(x.z)); }

        /// <summary>
        /// Computes the component-wise trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the component-wise trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 tzcnt(int4 x) { return int4(tzcnt(x.x), tzcnt(x.y), tzcnt(x.z), tzcnt(x.w)); }


        /// <summary>
        /// Computes the trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int tzcnt(uint x)
        {
            if (x == 0)
                return 32;

            x &= (uint)-x;
            LongDoubleUnion u;
            u.doubleValue = 0.0;
            u.longValue = 0x4330000000000000L + x;
            u.doubleValue -= 4503599627370496.0;
            return (int)(u.longValue >> 52) - 0x3FF;
        }

        /// <summary>
        /// Computes the component-wise trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the component-wise trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 tzcnt(uint2 x) { return int2(tzcnt(x.x), tzcnt(x.y)); }

        /// <summary>
        /// Computes the component-wise trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the component-wise trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 tzcnt(uint3 x) { return int3(tzcnt(x.x), tzcnt(x.y), tzcnt(x.z)); }

        /// <summary>
        /// Computes the component-wise trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the component-wise trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 tzcnt(uint4 x) { return int4(tzcnt(x.x), tzcnt(x.y), tzcnt(x.z), tzcnt(x.w)); }

        /// <summary>
        /// Computes the trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int tzcnt(long x) { return tzcnt((ulong)x); }

        /// <summary>
        /// Computes the trailing zero count in the binary representation of the input value.
        /// </summary>
        /// <remarks>
        /// Assuming that the least significant bit is on the right, the integer value 4 has a binary representation
        /// 0100 and the trailing zero count is two. The integer value 1 has a binary representation 0001 and the
        /// trailing zero count is zero.
        /// </remarks>
        /// <param name="x">Input to use when computing the trailing zero count.</param>
        /// <returns>Returns the trailing zero count of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int tzcnt(ulong x)
        {
            if (x == 0)
                return 64;

            x = x & (ulong)-(long)x;
            uint xl = (uint)x;

            uint bits = xl != 0 ? xl : (uint)(x >> 32);
            int offset = xl != 0 ? 0x3FF : 0x3DF;

            LongDoubleUnion u;
            u.doubleValue = 0.0;
            u.longValue = 0x4330000000000000L + bits;
            u.doubleValue -= 4503599627370496.0;
            return (int)(u.longValue >> 52) - offset;
        }



        /// <summary>Returns the result of performing a reversal of the bit pattern of an int value.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int reversebits(int x) { return (int)reversebits((uint)x); }

        /// <summary>Returns the result of performing a componentwise reversal of the bit pattern of an int2 vector.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with componentwise reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 reversebits(int2 x) { return (int2)reversebits((uint2)x); }

        /// <summary>Returns the result of performing a componentwise reversal of the bit pattern of an int3 vector.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with componentwise reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 reversebits(int3 x) { return (int3)reversebits((uint3)x); }

        /// <summary>Returns the result of performing a componentwise reversal of the bit pattern of an int4 vector.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with componentwise reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 reversebits(int4 x) { return (int4)reversebits((uint4)x); }


        /// <summary>Returns the result of performing a reversal of the bit pattern of a uint value.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint reversebits(uint x) {
            x = ((x >> 1) & 0x55555555) | ((x & 0x55555555) << 1);
            x = ((x >> 2) & 0x33333333) | ((x & 0x33333333) << 2);
            x = ((x >> 4) & 0x0F0F0F0F) | ((x & 0x0F0F0F0F) << 4);
            x = ((x >> 8) & 0x00FF00FF) | ((x & 0x00FF00FF) << 8);
            return (x >> 16) | (x << 16);
        }

        /// <summary>Returns the result of performing a componentwise reversal of the bit pattern of an uint2 vector.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with componentwise reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 reversebits(uint2 x)
        {
            x = ((x >> 1) & 0x55555555) | ((x & 0x55555555) << 1);
            x = ((x >> 2) & 0x33333333) | ((x & 0x33333333) << 2);
            x = ((x >> 4) & 0x0F0F0F0F) | ((x & 0x0F0F0F0F) << 4);
            x = ((x >> 8) & 0x00FF00FF) | ((x & 0x00FF00FF) << 8);
            return (x >> 16) | (x << 16);
        }

        /// <summary>Returns the result of performing a componentwise reversal of the bit pattern of an uint3 vector.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with componentwise reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 reversebits(uint3 x)
        {
            x = ((x >> 1) & 0x55555555) | ((x & 0x55555555) << 1);
            x = ((x >> 2) & 0x33333333) | ((x & 0x33333333) << 2);
            x = ((x >> 4) & 0x0F0F0F0F) | ((x & 0x0F0F0F0F) << 4);
            x = ((x >> 8) & 0x00FF00FF) | ((x & 0x00FF00FF) << 8);
            return (x >> 16) | (x << 16);
        }

        /// <summary>Returns the result of performing a componentwise reversal of the bit pattern of an uint4 vector.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with componentwise reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 reversebits(uint4 x)
        {
            x = ((x >> 1) & 0x55555555) | ((x & 0x55555555) << 1);
            x = ((x >> 2) & 0x33333333) | ((x & 0x33333333) << 2);
            x = ((x >> 4) & 0x0F0F0F0F) | ((x & 0x0F0F0F0F) << 4);
            x = ((x >> 8) & 0x00FF00FF) | ((x & 0x00FF00FF) << 8);
            return (x >> 16) | (x << 16);
        }


        /// <summary>Returns the result of performing a reversal of the bit pattern of a long value.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long reversebits(long x) { return (long)reversebits((ulong)x); }


        /// <summary>Returns the result of performing a reversal of the bit pattern of a ulong value.</summary>
        /// <param name="x">Value to reverse.</param>
        /// <returns>Value with reversed bits.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong reversebits(ulong x)
        {
            x = ((x >> 1) & 0x5555555555555555ul) | ((x & 0x5555555555555555ul) << 1);
            x = ((x >> 2) & 0x3333333333333333ul) | ((x & 0x3333333333333333ul) << 2);
            x = ((x >> 4) & 0x0F0F0F0F0F0F0F0Ful) | ((x & 0x0F0F0F0F0F0F0F0Ful) << 4);
            x = ((x >> 8) & 0x00FF00FF00FF00FFul) | ((x & 0x00FF00FF00FF00FFul) << 8);
            x = ((x >> 16) & 0x0000FFFF0000FFFFul) | ((x & 0x0000FFFF0000FFFFul) << 16);
            return (x >> 32) | (x << 32);
        }


        /// <summary>Returns the result of rotating the bits of an int left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int rol(int x, int n) { return (int)rol((uint)x, n); }

        /// <summary>Returns the componentwise result of rotating the bits of an int2 left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 rol(int2 x, int n) { return (int2)rol((uint2)x, n); }

        /// <summary>Returns the componentwise result of rotating the bits of an int3 left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 rol(int3 x, int n) { return (int3)rol((uint3)x, n); }

        /// <summary>Returns the componentwise result of rotating the bits of an int4 left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 rol(int4 x, int n) { return (int4)rol((uint4)x, n); }


        /// <summary>Returns the result of rotating the bits of a uint left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint rol(uint x, int n) { return (x << n) | (x >> (32 - n)); }

        /// <summary>Returns the componentwise result of rotating the bits of a uint2 left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 rol(uint2 x, int n) { return (x << n) | (x >> (32 - n)); }

        /// <summary>Returns the componentwise result of rotating the bits of a uint3 left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 rol(uint3 x, int n) { return (x << n) | (x >> (32 - n)); }

        /// <summary>Returns the componentwise result of rotating the bits of a uint4 left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 rol(uint4 x, int n) { return (x << n) | (x >> (32 - n)); }


        /// <summary>Returns the result of rotating the bits of a long left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long rol(long x, int n) { return (long)rol((ulong)x, n); }


        /// <summary>Returns the result of rotating the bits of a ulong left by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong rol(ulong x, int n) { return (x << n) | (x >> (64 - n)); }


        /// <summary>Returns the result of rotating the bits of an int right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ror(int x, int n) { return (int)ror((uint)x, n); }

        /// <summary>Returns the componentwise result of rotating the bits of an int2 right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 ror(int2 x, int n) { return (int2)ror((uint2)x, n); }

        /// <summary>Returns the componentwise result of rotating the bits of an int3 right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ror(int3 x, int n) { return (int3)ror((uint3)x, n); }

        /// <summary>Returns the componentwise result of rotating the bits of an int4 right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 ror(int4 x, int n) { return (int4)ror((uint4)x, n); }


        /// <summary>Returns the result of rotating the bits of a uint right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ror(uint x, int n) { return (x >> n) | (x << (32 - n)); }

        /// <summary>Returns the componentwise result of rotating the bits of a uint2 right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 ror(uint2 x, int n) { return (x >> n) | (x << (32 - n)); }

        /// <summary>Returns the componentwise result of rotating the bits of a uint3 right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 ror(uint3 x, int n) { return (x >> n) | (x << (32 - n)); }

        /// <summary>Returns the componentwise result of rotating the bits of a uint4 right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The componentwise rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 ror(uint4 x, int n) { return (x >> n) | (x << (32 - n)); }


        /// <summary>Returns the result of rotating the bits of a long right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ror(long x, int n) { return (long)ror((ulong)x, n); }


        /// <summary>Returns the result of rotating the bits of a ulong right by bits n.</summary>
        /// <param name="x">Value to rotate.</param>
        /// <param name="n">Number of bits to rotate.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ror(ulong x, int n) { return (x >> n) | (x << (64 - n)); }


        /// <summary>Returns the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ceilpow2(int x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>Returns the result of a componentwise calculation of the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 ceilpow2(int2 x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>Returns the result of a componentwise calculation of the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ceilpow2(int3 x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>Returns the result of a componentwise calculation of the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 ceilpow2(int4 x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }


        /// <summary>Returns the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ceilpow2(uint x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>Returns the result of a componentwise calculation of the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 ceilpow2(uint2 x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>Returns the result of a componentwise calculation of the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 ceilpow2(uint3 x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        /// <summary>Returns the result of a componentwise calculation of the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The componentwise smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 ceilpow2(uint4 x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }


        /// <summary>Returns the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ceilpow2(long x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;
            return x + 1;
        }


        /// <summary>Returns the smallest power of two greater than or equal to the input.</summary>
        /// <remarks>Also known as nextpow2.</remarks>
        /// <param name="x">Input value.</param>
        /// <returns>The smallest power of two greater than or equal to the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ceilpow2(ulong x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;
            return x + 1;
        }

        /// <summary>
        /// Computes the ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// x must be greater than 0, otherwise the result is undefined.
        /// </remarks>
        /// <param name="x">Integer to be used as input.</param>
        /// <returns>Ceiling of the base-2 logarithm of x, as an integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ceillog2(int x)
        {
            return 32 - lzcnt((uint)x - 1);
        }

        /// <summary>
        /// Computes the componentwise ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// Components of x must be greater than 0, otherwise the result for that component is undefined.
        /// </remarks>
        /// <param name="x">int2 to be used as input.</param>
        /// <returns>Componentwise ceiling of the base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 ceillog2(int2 x)
        {
            return new int2(ceillog2(x.x), ceillog2(x.y));
        }

        /// <summary>
        /// Computes the componentwise ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// Components of x must be greater than 0, otherwise the result for that component is undefined.
        /// </remarks>
        /// <param name="x">int3 to be used as input.</param>
        /// <returns>Componentwise ceiling of the base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ceillog2(int3 x)
        {
            return new int3(ceillog2(x.x), ceillog2(x.y), ceillog2(x.z));
        }

        /// <summary>
        /// Computes the componentwise ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// Components of x must be greater than 0, otherwise the result for that component is undefined.
        /// </remarks>
        /// <param name="x">int4 to be used as input.</param>
        /// <returns>Componentwise ceiling of the base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 ceillog2(int4 x)
        {
            return new int4(ceillog2(x.x), ceillog2(x.y), ceillog2(x.z), ceillog2(x.w));
        }

        /// <summary>
        /// Computes the ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// x must be greater than 0, otherwise the result is undefined.
        /// </remarks>
        /// <param name="x">Unsigned integer to be used as input.</param>
        /// <returns>Ceiling of the base-2 logarithm of x, as an integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ceillog2(uint x)
        {
            return 32 - lzcnt(x - 1);
        }

        /// <summary>
        /// Computes the componentwise ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// Components of x must be greater than 0, otherwise the result for that component is undefined.
        /// </remarks>
        /// <param name="x">uint2 to be used as input.</param>
        /// <returns>Componentwise ceiling of the base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 ceillog2(uint2 x)
        {
            return new int2(ceillog2(x.x), ceillog2(x.y));
        }

        /// <summary>
        /// Computes the componentwise ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// Components of x must be greater than 0, otherwise the result for that component is undefined.
        /// </remarks>
        /// <param name="x">uint3 to be used as input.</param>
        /// <returns>Componentwise ceiling of the base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ceillog2(uint3 x)
        {
            return new int3(ceillog2(x.x), ceillog2(x.y), ceillog2(x.z));
        }

        /// <summary>
        /// Computes the componentwise ceiling of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>
        /// Components of x must be greater than 0, otherwise the result for that component is undefined.
        /// </remarks>
        /// <param name="x">uint4 to be used as input.</param>
        /// <returns>Componentwise ceiling of the base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 ceillog2(uint4 x)
        {
            return new int4(ceillog2(x.x), ceillog2(x.y), ceillog2(x.z), ceillog2(x.w));
        }

        /// <summary>
        /// Computes the floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>x must be greater than zero, otherwise the result is undefined.</remarks>
        /// <param name="x">Integer to be used as input.</param>
        /// <returns>Floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int floorlog2(int x)
        {
            return 31 - lzcnt((uint)x);
        }

        /// <summary>
        /// Computes the componentwise floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>Components of x must be greater than zero, otherwise the result of the component is undefined.</remarks>
        /// <param name="x">int2 to be used as input.</param>
        /// <returns>Componentwise floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 floorlog2(int2 x)
        {
            return new int2(floorlog2(x.x), floorlog2(x.y));
        }

        /// <summary>
        /// Computes the componentwise floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>Components of x must be greater than zero, otherwise the result of the component is undefined.</remarks>
        /// <param name="x">int3 to be used as input.</param>
        /// <returns>Componentwise floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 floorlog2(int3 x)
        {
            return new int3(floorlog2(x.x), floorlog2(x.y), floorlog2(x.z));
        }

        /// <summary>
        /// Computes the componentwise floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>Components of x must be greater than zero, otherwise the result of the component is undefined.</remarks>
        /// <param name="x">int4 to be used as input.</param>
        /// <returns>Componentwise floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 floorlog2(int4 x)
        {
            return new int4(floorlog2(x.x), floorlog2(x.y), floorlog2(x.z), floorlog2(x.w));
        }

        /// <summary>
        /// Computes the floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>x must be greater than zero, otherwise the result is undefined.</remarks>
        /// <param name="x">Unsigned integer to be used as input.</param>
        /// <returns>Floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int floorlog2(uint x)
        {
            return 31 - lzcnt(x);
        }

        /// <summary>
        /// Computes the componentwise floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>Components of x must be greater than zero, otherwise the result of the component is undefined.</remarks>
        /// <param name="x">uint2 to be used as input.</param>
        /// <returns>Componentwise floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 floorlog2(uint2 x)
        {
            return new int2(floorlog2(x.x), floorlog2(x.y));
        }

        /// <summary>
        /// Computes the componentwise floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>Components of x must be greater than zero, otherwise the result of the component is undefined.</remarks>
        /// <param name="x">uint3 to be used as input.</param>
        /// <returns>Componentwise floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 floorlog2(uint3 x)
        {
            return new int3(floorlog2(x.x), floorlog2(x.y), floorlog2(x.z));
        }

        /// <summary>
        /// Computes the componentwise floor of the base-2 logarithm of x.
        /// </summary>
        /// <remarks>Components of x must be greater than zero, otherwise the result of the component is undefined.</remarks>
        /// <param name="x">uint4 to be used as input.</param>
        /// <returns>Componentwise floor of base-2 logarithm of x.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 floorlog2(uint4 x)
        {
            return new int4(floorlog2(x.x), floorlog2(x.y), floorlog2(x.z), floorlog2(x.w));
        }

        /// <summary>Returns the result of converting a float value from degrees to radians.</summary>
        /// <param name="x">Angle in degrees.</param>
        /// <returns>Angle converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float radians(float x) { return x * TORADIANS; }

        /// <summary>Returns the result of a componentwise conversion of a float2 vector from degrees to radians.</summary>
        /// <param name="x">Vector containing angles in degrees.</param>
        /// <returns>Vector containing angles converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 radians(float2 x) { return x * TORADIANS; }

        /// <summary>Returns the result of a componentwise conversion of a float3 vector from degrees to radians.</summary>
        /// <param name="x">Vector containing angles in degrees.</param>
        /// <returns>Vector containing angles converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 radians(float3 x) { return x * TORADIANS; }

        /// <summary>Returns the result of a componentwise conversion of a float4 vector from degrees to radians.</summary>
        /// <param name="x">Vector containing angles in degrees.</param>
        /// <returns>Vector containing angles converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 radians(float4 x) { return x * TORADIANS; }


        /// <summary>Returns the result of converting a float value from degrees to radians.</summary>
        /// <param name="x">Angle in degrees.</param>
        /// <returns>Angle converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double radians(double x) { return x * TORADIANS_DBL; }

        /// <summary>Returns the result of a componentwise conversion of a float2 vector from degrees to radians.</summary>
        /// <param name="x">Vector containing angles in degrees.</param>
        /// <returns>Vector containing angles converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 radians(double2 x) { return x * TORADIANS_DBL; }

        /// <summary>Returns the result of a componentwise conversion of a float3 vector from degrees to radians.</summary>
        /// <param name="x">Vector containing angles in degrees.</param>
        /// <returns>Vector containing angles converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 radians(double3 x) { return x * TORADIANS_DBL; }

        /// <summary>Returns the result of a componentwise conversion of a float4 vector from degrees to radians.</summary>
        /// <param name="x">Vector containing angles in degrees.</param>
        /// <returns>Vector containing angles converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 radians(double4 x) { return x * TORADIANS_DBL; }


        /// <summary>Returns the result of converting a double value from radians to degrees.</summary>
        /// <param name="x">Angle in radians.</param>
        /// <returns>Angle converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float degrees(float x) { return x * TODEGREES; }

        /// <summary>Returns the result of a componentwise conversion of a double2 vector from radians to degrees.</summary>
        /// <param name="x">Vector containing angles in radians.</param>
        /// <returns>Vector containing angles converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 degrees(float2 x) { return x * TODEGREES; }

        /// <summary>Returns the result of a componentwise conversion of a double3 vector from radians to degrees.</summary>
        /// <param name="x">Vector containing angles in radians.</param>
        /// <returns>Vector containing angles converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 degrees(float3 x) { return x * TODEGREES; }

        /// <summary>Returns the result of a componentwise conversion of a double4 vector from radians to degrees.</summary>
        /// <param name="x">Vector containing angles in radians.</param>
        /// <returns>Vector containing angles converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 degrees(float4 x) { return x * TODEGREES; }


        /// <summary>Returns the result of converting a double value from radians to degrees.</summary>
        /// <param name="x">Angle in radians.</param>
        /// <returns>Angle converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double degrees(double x) { return x * TODEGREES_DBL; }

        /// <summary>Returns the result of a componentwise conversion of a double2 vector from radians to degrees.</summary>
        /// <param name="x">Vector containing angles in radians.</param>
        /// <returns>Vector containing angles converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 degrees(double2 x) { return x * TODEGREES_DBL; }

        /// <summary>Returns the result of a componentwise conversion of a double3 vector from radians to degrees.</summary>
        /// <param name="x">Vector containing angles in radians.</param>
        /// <returns>Vector containing values converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 degrees(double3 x) { return x * TODEGREES_DBL; }

        /// <summary>Returns the result of a componentwise conversion of a double4 vector from radians to degrees.</summary>
        /// <param name="x">Vector containing angles in radians.</param>
        /// <returns>Vector containing angles converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 degrees(double4 x) { return x * TODEGREES_DBL; }


        /// <summary>Returns the minimum component of an int2 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cmin(int2 x) { return min(x.x, x.y); }

        /// <summary>Returns the minimum component of an int3 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cmin(int3 x) { return min(min(x.x, x.y), x.z); }

        /// <summary>Returns the minimum component of an int4 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cmin(int4 x) { return min(min(x.x, x.y), min(x.z, x.w)); }


        /// <summary>Returns the minimum component of a uint2 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint cmin(uint2 x) { return min(x.x, x.y); }

        /// <summary>Returns the minimum component of a uint3 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint cmin(uint3 x) { return min(min(x.x, x.y), x.z); }

        /// <summary>Returns the minimum component of a uint4 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint cmin(uint4 x) { return min(min(x.x, x.y), min(x.z, x.w)); }


        /// <summary>Returns the minimum component of a float2 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cmin(float2 x) { return min(x.x, x.y); }

        /// <summary>Returns the minimum component of a float3 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cmin(float3 x) { return min(min(x.x, x.y), x.z); }

        /// <summary>Returns the minimum component of a float4 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cmin(float4 x) { return min(min(x.x, x.y), min(x.z, x.w)); }


        /// <summary>Returns the minimum component of a double2 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cmin(double2 x) { return min(x.x, x.y); }

        /// <summary>Returns the minimum component of a double3 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cmin(double3 x) { return min(min(x.x, x.y), x.z); }

        /// <summary>Returns the minimum component of a double4 vector.</summary>
        /// <param name="x">The vector to use when computing the minimum component.</param>
        /// <returns>The value of the minimum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cmin(double4 x) { return min(min(x.x, x.y), min(x.z, x.w)); }


        /// <summary>Returns the maximum component of an int2 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cmax(int2 x) { return max(x.x, x.y); }

        /// <summary>Returns the maximum component of an int3 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cmax(int3 x) { return max(max(x.x, x.y), x.z); }

        /// <summary>Returns the maximum component of an int4 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int cmax(int4 x) { return max(max(x.x, x.y), max(x.z, x.w)); }


        /// <summary>Returns the maximum component of a uint2 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint cmax(uint2 x) { return max(x.x, x.y); }

        /// <summary>Returns the maximum component of a uint3 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint cmax(uint3 x) { return max(max(x.x, x.y), x.z); }

        /// <summary>Returns the maximum component of a uint4 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint cmax(uint4 x) { return max(max(x.x, x.y), max(x.z, x.w)); }


        /// <summary>Returns the maximum component of a float2 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cmax(float2 x) { return max(x.x, x.y); }

        /// <summary>Returns the maximum component of a float3 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cmax(float3 x) { return max(max(x.x, x.y), x.z); }

        /// <summary>Returns the maximum component of a float4 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cmax(float4 x) { return max(max(x.x, x.y), max(x.z, x.w)); }


        /// <summary>Returns the maximum component of a double2 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cmax(double2 x) { return max(x.x, x.y); }

        /// <summary>Returns the maximum component of a double3 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cmax(double3 x) { return max(max(x.x, x.y), x.z); }

        /// <summary>Returns the maximum component of a double4 vector.</summary>
        /// <param name="x">The vector to use when computing the maximum component.</param>
        /// <returns>The value of the maximum component of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double cmax(double4 x) { return max(max(x.x, x.y), max(x.z, x.w)); }


        /// <summary>Returns the horizontal sum of components of an int2 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int csum(int2 x) { return x.x + x.y; }

        /// <summary>Returns the horizontal sum of components of an int3 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int csum(int3 x) { return x.x + x.y + x.z; }

        /// <summary>Returns the horizontal sum of components of an int4 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int csum(int4 x) { return x.x + x.y + x.z + x.w; }


        /// <summary>Returns the horizontal sum of components of a uint2 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint csum(uint2 x) { return x.x + x.y; }

        /// <summary>Returns the horizontal sum of components of a uint3 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint csum(uint3 x) { return x.x + x.y + x.z; }

        /// <summary>Returns the horizontal sum of components of a uint4 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint csum(uint4 x) { return x.x + x.y + x.z + x.w; }


        /// <summary>Returns the horizontal sum of components of a float2 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float csum(float2 x) { return x.x + x.y; }

        /// <summary>Returns the horizontal sum of components of a float3 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float csum(float3 x) { return x.x + x.y + x.z; }

        /// <summary>Returns the horizontal sum of components of a float4 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float csum(float4 x) { return (x.x + x.y) + (x.z + x.w); }


        /// <summary>Returns the horizontal sum of components of a double2 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double csum(double2 x) { return x.x + x.y; }

        /// <summary>Returns the horizontal sum of components of a double3 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double csum(double3 x) { return x.x + x.y + x.z; }

        /// <summary>Returns the horizontal sum of components of a double4 vector.</summary>
        /// <param name="x">The vector to use when computing the horizontal sum.</param>
        /// <returns>The horizontal sum of of components of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double csum(double4 x) { return (x.x + x.y) + (x.z + x.w); }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float square(float x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 square(float2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 square(float3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 square(float4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double square(double x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 square(double2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 square(double3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 square(double4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(46341)</c>
        /// will return <c>-2147479015</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int square(int x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(new int2(46341))</c>
        /// will return <c>new int2(-2147479015)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 square(int2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(new int3(46341))</c>
        /// will return <c>new int3(-2147479015)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 square(int3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x)</c> is positive. For example, <c>square(new int4(46341))</c>
        /// will return <c>new int4(-2147479015)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 square(int4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(4294967295u)</c>
        /// will return <c>1u</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint square(uint x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(new uint2(4294967295u))</c>
        /// will return <c>new uint2(1u)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 square(uint2 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(new uint3(4294967295u))</c>
        /// will return <c>new uint3(1u)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 square(uint3 x)
        {
            return x * x;
        }

        /// <summary>
        /// Computes the component-wise square (x * x) of the input argument x.
        /// </summary>
        /// <remarks>
        /// Due to integer overflow, it's not always guaranteed that <c>square(x) &gt;= x</c>. For example, <c>square(new uint4(4294967295u))</c>
        /// will return <c>new uint4(1u)</c>.
        /// </remarks>
        /// <param name="x">Value to square.</param>
        /// <returns>Returns the square of the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 square(uint4 x)
        {
            return x * x;
        }

        /// <summary>
        /// Packs components with an enabled mask to the left.
        /// </summary>
        /// <remarks>
        /// This function is also known as left packing. The effect of this function is to filter out components that
        /// are not enabled and leave an output buffer tightly packed with only the enabled components. A common use
        /// case is if you perform intersection tests on arrays of data in structure of arrays (SoA) form and need to
        /// produce an output array of the things that intersected.
        /// </remarks>
        /// <param name="output">Pointer to packed output array where enabled components should be stored to.</param>
        /// <param name="index">Index into output array where first enabled component should be stored to.</param>
        /// <param name="val">The value to to compress.</param>
        /// <param name="mask">Mask indicating which components are enabled.</param>
        /// <returns>Index to element after the last one stored.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int compress(int* output, int index, int4 val, bool4 mask)
        {
            if (mask.x)
                output[index++] = val.x;
            if (mask.y)
                output[index++] = val.y;
            if (mask.z)
                output[index++] = val.z;
            if (mask.w)
                output[index++] = val.w;

            return index;
        }

        /// <summary>
        /// Packs components with an enabled mask to the left.
        /// </summary>
        /// <remarks>
        /// This function is also known as left packing. The effect of this function is to filter out components that
        /// are not enabled and leave an output buffer tightly packed with only the enabled components. A common use
        /// case is if you perform intersection tests on arrays of data in structure of arrays (SoA) form and need to
        /// produce an output array of the things that intersected.
        /// </remarks>
        /// <param name="output">Pointer to packed output array where enabled components should be stored to.</param>
        /// <param name="index">Index into output array where first enabled component should be stored to.</param>
        /// <param name="val">The value to to compress.</param>
        /// <param name="mask">Mask indicating which components are enabled.</param>
        /// <returns>Index to element after the last one stored.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int compress(uint* output, int index, uint4 val, bool4 mask)
        {
            return compress((int*)output, index, *(int4*)&val, mask);
        }

        /// <summary>
        /// Packs components with an enabled mask to the left.
        /// </summary>
        /// <remarks>
        /// This function is also known as left packing. The effect of this function is to filter out components that
        /// are not enabled and leave an output buffer tightly packed with only the enabled components. A common use
        /// case is if you perform intersection tests on arrays of data in structure of arrays (SoA) form and need to
        /// produce an output array of the things that intersected.
        /// </remarks>
        /// <param name="output">Pointer to packed output array where enabled components should be stored to.</param>
        /// <param name="index">Index into output array where first enabled component should be stored to.</param>
        /// <param name="val">The value to to compress.</param>
        /// <param name="mask">Mask indicating which components are enabled.</param>
        /// <returns>Index to element after the last one stored.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int compress(float* output, int index, float4 val, bool4 mask)
        {
            return compress((int*)output, index, *(int4*)&val, mask);
        }

        /// <summary>Returns the floating point representation of a half-precision floating point value.</summary>
        /// <param name="x">The half precision float.</param>
        /// <returns>The single precision float representation of the half precision float.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float f16tof32(uint x)
        {
            const uint shifted_exp = (0x7c00 << 13);
            uint uf = (x & 0x7fff) << 13;
            uint e = uf & shifted_exp;
            uf += (127 - 15) << 23;
            uf += select(0, (128u - 16u) << 23, e == shifted_exp);
            uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0);
            uf |= (x & 0x8000) << 16;
            return asfloat(uf);
        }

        /// <summary>Returns the floating point representation of a half-precision floating point vector.</summary>
        /// <param name="x">The half precision float vector.</param>
        /// <returns>The single precision float vector representation of the half precision float vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 f16tof32(uint2 x)
        {
            const uint shifted_exp = (0x7c00 << 13);
            uint2 uf = (x & 0x7fff) << 13;
            uint2 e = uf & shifted_exp;
            uf += (127 - 15) << 23;
            uf += select(0, (128u - 16u) << 23, e == shifted_exp);
            uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0);
            uf |= (x & 0x8000) << 16;
            return asfloat(uf);
        }

        /// <summary>Returns the floating point representation of a half-precision floating point vector.</summary>
        /// <param name="x">The half precision float vector.</param>
        /// <returns>The single precision float vector representation of the half precision float vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 f16tof32(uint3 x)
        {
            const uint shifted_exp = (0x7c00 << 13);
            uint3 uf = (x & 0x7fff) << 13;
            uint3 e = uf & shifted_exp;
            uf += (127 - 15) << 23;
            uf += select(0, (128u - 16u) << 23, e == shifted_exp);
            uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0);
            uf |= (x & 0x8000) << 16;
            return asfloat(uf);
        }

        /// <summary>Returns the floating point representation of a half-precision floating point vector.</summary>
        /// <param name="x">The half precision float vector.</param>
        /// <returns>The single precision float vector representation of the half precision float vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 f16tof32(uint4 x)
        {
            const uint shifted_exp = (0x7c00 << 13);
            uint4 uf = (x & 0x7fff) << 13;
            uint4 e = uf & shifted_exp;
            uf += (127 - 15) << 23;
            uf += select(0, (128u - 16u) << 23, e == shifted_exp);
            uf = select(uf, asuint(asfloat(uf + (1 << 23)) - 6.10351563e-05f), e == 0);
            uf |= (x & 0x8000) << 16;
            return asfloat(uf);
        }

        /// <summary>Returns the result converting a float value to its nearest half-precision floating point representation.</summary>
        /// <param name="x">The single precision float.</param>
        /// <returns>The half precision float representation of the single precision float.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint f32tof16(float x)
        {
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint ux = asuint(x);
            uint uux = ux & msk;
            uint h = (uint)(asuint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13;   // Clamp to signed infinity if overflowed
            h = select(h, select(0x7c00u, 0x7e00u, (int)uux > infinity_32), (int)uux >= infinity_32);   // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;
        }

        /// <summary>Returns the result of a componentwise conversion of a float2 vector to its nearest half-precision floating point representation.</summary>
        /// <param name="x">The single precision float vector.</param>
        /// <returns>The half precision float vector representation of the single precision float vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 f32tof16(float2 x)
        {
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint2 ux = asuint(x);
            uint2 uux = ux & msk;
            uint2 h = (uint2)(asint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13;   // Clamp to signed infinity if overflowed
            h = select(h, select(0x7c00u, 0x7e00u, (int2)uux > infinity_32), (int2)uux >= infinity_32);   // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;
        }

        /// <summary>Returns the result of a componentwise conversion of a float3 vector to its nearest half-precision floating point representation.</summary>
        /// <param name="x">The single precision float vector.</param>
        /// <returns>The half precision float vector representation of the single precision float vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 f32tof16(float3 x)
        {
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint3 ux = asuint(x);
            uint3 uux = ux & msk;
            uint3 h = (uint3)(asint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13;   // Clamp to signed infinity if overflowed
            h = select(h, select(0x7c00u, 0x7e00u, (int3)uux > infinity_32), (int3)uux >= infinity_32);   // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;
        }

        /// <summary>Returns the result of a componentwise conversion of a float4 vector to its nearest half-precision floating point representation.</summary>
        /// <param name="x">The single precision float vector.</param>
        /// <returns>The half precision float vector representation of the single precision float vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 f32tof16(float4 x)
        {
            const int infinity_32 = 255 << 23;
            const uint msk = 0x7FFFF000u;

            uint4 ux = asuint(x);
            uint4 uux = ux & msk;
            uint4 h = (uint4)(asint(min(asfloat(uux) * 1.92592994e-34f, 260042752.0f)) + 0x1000) >> 13;   // Clamp to signed infinity if overflowed
            h = select(h, select(0x7c00u, 0x7e00u, (int4)uux > infinity_32), (int4)uux >= infinity_32);   // NaN->qNaN and Inf->Inf
            return h | (ux & ~msk) >> 16;
        }

        /// <summary>
        /// Generate an orthonormal basis given a single unit length normal vector.
        /// </summary>
        /// <remarks>
        /// This implementation is from "Building an Orthonormal Basis, Revisited"
        /// https://graphics.pixar.com/library/OrthonormalB/paper.pdf
        /// </remarks>
        /// <param name="normal">Unit length normal vector.</param>
        /// <param name="basis1">Output unit length vector, orthogonal to normal vector.</param>
        /// <param name="basis2">Output unit length vector, orthogonal to normal vector and basis1.</param>
        public static void orthonormal_basis(float3 normal, out float3 basis1, out float3 basis2)
        {
            var sign = normal.z >= 0.0f ? 1.0f : -1.0f;
            var a = -1.0f / (sign + normal.z);
            var b = normal.x * normal.y * a;
            basis1.x = 1.0f + sign * normal.x * normal.x * a;
            basis1.y = sign * b;
            basis1.z = -sign * normal.x;
            basis2.x = b;
            basis2.y = sign + normal.y * normal.y * a;
            basis2.z = -normal.y;
        }

        /// <summary>
        /// Generate an orthonormal basis given a single unit length normal vector.
        /// </summary>
        /// <remarks>
        /// This implementation is from "Building an Orthonormal Basis, Revisited"
        /// https://graphics.pixar.com/library/OrthonormalB/paper.pdf
        /// </remarks>
        /// <param name="normal">Unit length normal vector.</param>
        /// <param name="basis1">Output unit length vector, orthogonal to normal vector.</param>
        /// <param name="basis2">Output unit length vector, orthogonal to normal vector and basis1.</param>
        public static void orthonormal_basis(double3 normal, out double3 basis1, out double3 basis2)
        {
            var sign = normal.z >= 0.0 ? 1.0 : -1.0;
            var a = -1.0 / (sign + normal.z);
            var b = normal.x * normal.y * a;
            basis1.x = 1.0 + sign * normal.x * normal.x * a;
            basis1.y = sign * b;
            basis1.z = -sign * normal.x;
            basis2.x = b;
            basis2.y = sign + normal.y * normal.y * a;
            basis2.z = -normal.y;
        }

        /// <summary>Change the sign of x based on the most significant bit of y [msb(y) ? -x : x].</summary>
        /// <param name="x">The single precision float to change the sign.</param>
        /// <param name="y">The single precision float used to test the most significant bit.</param>
        /// <returns>Returns x with changed sign based on y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float chgsign(float x, float y)
        {
            return asfloat(asuint(x) ^ (asuint(y) & 0x80000000));
        }

        /// <summary>Change the sign of components of x based on the most significant bit of components of y [msb(y) ? -x : x].</summary>
        /// <param name="x">The single precision float vector to change the sign.</param>
        /// <param name="y">The single precision float vector used to test the most significant bit.</param>
        /// <returns>Returns vector x with changed sign based on vector y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 chgsign(float2 x, float2 y)
        {
            return asfloat(asuint(x) ^ (asuint(y) & 0x80000000));
        }

        /// <summary>Change the sign of components of x based on the most significant bit of components of y [msb(y) ? -x : x].</summary>
        /// <param name="x">The single precision float vector to change the sign.</param>
        /// <param name="y">The single precision float vector used to test the most significant bit.</param>
        /// <returns>Returns vector x with changed sign based on vector y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 chgsign(float3 x, float3 y)
        {
            return asfloat(asuint(x) ^ (asuint(y) & 0x80000000));
        }

        /// <summary>Change the sign of components of x based on the most significant bit of components of y [msb(y) ? -x : x].</summary>
        /// <param name="x">The single precision float vector to change the sign.</param>
        /// <param name="y">The single precision float vector used to test the most significant bit.</param>
        /// <returns>Returns vector x with changed sign based on vector y.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 chgsign(float4 x, float4 y)
        {
            return asfloat(asuint(x) ^ (asuint(y) & 0x80000000));
        }

        /// <summary>
        /// Read 32 bits of data in little endian format.
        /// </summary>
        /// <param name="pBuffer">Memory address to read from.</param>
        /// <returns>32 bits in little endian format.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint read32_little_endian(void* pBuffer)
        {
            byte* ptr = (byte*)pBuffer;
            return (uint)ptr[0] | ((uint)ptr[1] << 8) | ((uint)ptr[2] << 16) | ((uint)ptr[3] << 24);
        }

        private static unsafe uint hash_with_unaligned_loads(void* pBuffer, int numBytes, uint seed)
        {
            unchecked
            {
                const uint Prime1 = 2654435761;
                const uint Prime2 = 2246822519;
                const uint Prime3 = 3266489917;
                const uint Prime4 = 668265263;
                const uint Prime5 = 374761393;

                uint4* p = (uint4*)pBuffer;
                uint hash = seed + Prime5;
                if (numBytes >= 16)
                {
                    uint4 state = new uint4(Prime1 + Prime2, Prime2, 0, (uint)-Prime1) + seed;

                    int count = numBytes >> 4;
                    for (int i = 0; i < count; ++i)
                    {
                        state += *p++ * Prime2;
                        state = (state << 13) | (state >> 19);
                        state *= Prime1;
                    }

                    hash = rol(state.x, 1) + rol(state.y, 7) + rol(state.z, 12) + rol(state.w, 18);
                }

                hash += (uint)numBytes;

                uint* puint = (uint*)p;
                for (int i = 0; i < ((numBytes >> 2) & 3); ++i)
                {
                    hash += *puint++ * Prime3;
                    hash = rol(hash, 17) * Prime4;
                }

                byte* pbyte = (byte*)puint;
                for (int i = 0; i < ((numBytes) & 3); ++i)
                {
                    hash += (*pbyte++) * Prime5;
                    hash = rol(hash, 11) * Prime1;
                }

                hash ^= hash >> 15;
                hash *= Prime2;
                hash ^= hash >> 13;
                hash *= Prime3;
                hash ^= hash >> 16;

                return hash;
            }
        }

        private static unsafe uint hash_without_unaligned_loads(void* pBuffer, int numBytes, uint seed)
        {
            unchecked
            {
                const uint Prime1 = 2654435761;
                const uint Prime2 = 2246822519;
                const uint Prime3 = 3266489917;
                const uint Prime4 = 668265263;
                const uint Prime5 = 374761393;

                byte* p = (byte*)pBuffer;
                uint hash = seed + Prime5;
                if (numBytes >= 16)
                {
                    uint4 state = new uint4(Prime1 + Prime2, Prime2, 0, (uint)-Prime1) + seed;

                    int count = numBytes >> 4;
                    for (int i = 0; i < count; ++i)
                    {
                        var data = new uint4(read32_little_endian(p), read32_little_endian(p + 4), read32_little_endian(p + 8), read32_little_endian(p + 12));
                        state += data * Prime2;
                        state = rol(state, 13);
                        state *= Prime1;
                        p += 16;
                    }

                    hash = rol(state.x, 1) + rol(state.y, 7) + rol(state.z, 12) + rol(state.w, 18);
                }

                hash += (uint)numBytes;

                for (int i = 0; i < ((numBytes >> 2) & 3); ++i)
                {
                    hash += read32_little_endian(p) * Prime3;
                    hash = rol(hash, 17) * Prime4;
                    p += 4;
                }

                for (int i = 0; i < ((numBytes) & 3); ++i)
                {
                    hash += (*p++) * Prime5;
                    hash = rol(hash, 11) * Prime1;
                }

                hash ^= hash >> 15;
                hash *= Prime2;
                hash ^= hash >> 13;
                hash *= Prime3;
                hash ^= hash >> 16;

                return hash;
            }
        }

        /// <summary>Returns a uint hash from a block of memory using the xxhash32 algorithm. Can only be used in an unsafe context.</summary>
        /// <param name="pBuffer">A pointer to the beginning of the data.</param>
        /// <param name="numBytes">Number of bytes to hash.</param>
        /// <param name="seed">Starting seed value.</param>
        /// <returns>The 32 bit hash of the input data buffer.</returns>
        public static unsafe uint hash(void* pBuffer, int numBytes, uint seed = 0)
        {
            return hash_with_unaligned_loads(pBuffer, numBytes, seed);
        }

        /// <summary>
        /// Unity's up axis (0, 1, 0).
        /// </summary>
        /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-up.html](https://docs.unity3d.com/ScriptReference/Vector3-up.html)</remarks>
        /// <returns>The up axis.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 up() { return new float3(0.0f, 1.0f, 0.0f); }  // for compatibility

        /// <summary>
        /// Unity's down axis (0, -1, 0).
        /// </summary>
        /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-down.html](https://docs.unity3d.com/ScriptReference/Vector3-down.html)</remarks>
        /// <returns>The down axis.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 down() { return new float3(0.0f, -1.0f, 0.0f); }

        /// <summary>
        /// Unity's forward axis (0, 0, 1).
        /// </summary>
        /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-forward.html](https://docs.unity3d.com/ScriptReference/Vector3-forward.html)</remarks>
        /// <returns>The forward axis.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 forward() { return new float3(0.0f, 0.0f, 1.0f); }

        /// <summary>
        /// Unity's back axis (0, 0, -1).
        /// </summary>
        /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-back.html](https://docs.unity3d.com/ScriptReference/Vector3-back.html)</remarks>
        /// <returns>The back axis.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 back() { return new float3(0.0f, 0.0f, -1.0f); }

        /// <summary>
        /// Unity's left axis (-1, 0, 0).
        /// </summary>
        /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-left.html](https://docs.unity3d.com/ScriptReference/Vector3-left.html)</remarks>
        /// <returns>The left axis.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 left() { return new float3(-1.0f, 0.0f, 0.0f); }

        /// <summary>
        /// Unity's right axis (1, 0, 0).
        /// </summary>
        /// <remarks>Matches [https://docs.unity3d.com/ScriptReference/Vector3-right.html](https://docs.unity3d.com/ScriptReference/Vector3-right.html)</remarks>
        /// <returns>The right axis.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 right() { return new float3(1.0f, 0.0f, 0.0f); }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the XYZ rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in XYZ order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 EulerXYZ(quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.z - d1.y;
            if (y1 * y1 < cutoff)
            {
                var x1 = d2.y + d1.x;
                var x2 = d3.z + d3.w - d3.y - d3.x;
                var z1 = d2.x + d1.z;
                var z2 = d3.x + d3.w - d3.y - d3.z;
                euler = float3(atan2(x1, x2), -asin(y1), atan2(z1, z2));
            }
            else //xzx
            {
                y1 = clamp(y1, -1f, 1f);
                var abcd = float4(d2.z, d1.y, d2.x, d1.z);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = csum(abcd * abcd * float4(-1f, 1f, -1f, 1f));
                euler = float3(atan2(x1, x2), -asin(y1), 0f);
            }

            return euler;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the XZY rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in XZY order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 EulerXZY(quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.x + d1.z;
            if (y1 * y1 < cutoff)
            {
                var x1 = -d2.y + d1.x;
                var x2 = d3.y + d3.w - d3.z - d3.x;
                var z1 = -d2.z + d1.y;
                var z2 = d3.x + d3.w - d3.y - d3.z;
                euler = float3(atan2(x1, x2), asin(y1), atan2(z1, z2));
            }
            else //xyx
            {
                y1 = clamp(y1, -1f, 1f);
                var abcd = float4(d2.x, d1.z, d2.z, d1.y);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = csum(abcd * abcd * float4(-1f, 1f, -1f, 1f));
                euler = float3(atan2(x1, x2), asin(y1), 0f);
            }

            return euler.xzy;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the YXZ rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in YXZ order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 EulerYXZ(quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.y + d1.x;
            if (y1 * y1 < cutoff)
            {
                var x1 = -d2.z + d1.y;
                var x2 = d3.z + d3.w - d3.x - d3.y;
                var z1 = -d2.x + d1.z;
                var z2 = d3.y + d3.w - d3.z - d3.x;
                euler = float3(atan2(x1, x2), asin(y1), atan2(z1, z2));
            }
            else //yzy
            {
                y1 = clamp(y1, -1f, 1f);
                var abcd = float4(d2.x, d1.z, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = csum(abcd * abcd * float4(-1f, 1f, -1f, 1f));
                euler = float3(atan2(x1, x2), asin(y1), 0f);
            }

            return euler.yxz;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the YZX rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in YZX order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 EulerYZX(quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.x - d1.z;
            if (y1 * y1 < cutoff)
            {
                var x1 = d2.z + d1.y;
                var x2 = d3.x + d3.w - d3.z - d3.y;
                var z1 = d2.y + d1.x;
                var z2 = d3.y + d3.w - d3.x - d3.z;
                euler = float3(atan2(x1, x2), -asin(y1), atan2(z1, z2));
            }
            else //yxy
            {
                y1 = clamp(y1, -1f, 1f);
                var abcd = float4(d2.x, d1.z, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = csum(abcd * abcd * float4(-1f, 1f, -1f, 1f));
                euler = float3(atan2(x1, x2), -asin(y1), 0f);
            }

            return euler.zxy;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the ZXY rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in ZXY order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 EulerZXY(quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.y - d1.x;
            if (y1 * y1 < cutoff)
            {
                var x1 = d2.x + d1.z;
                var x2 = d3.y + d3.w - d3.x - d3.z;
                var z1 = d2.z + d1.y;
                var z2 = d3.z + d3.w - d3.x - d3.y;
                euler = float3(atan2(x1, x2), -asin(y1), atan2(z1, z2));
            }
            else //zxz
            {
                y1 = clamp(y1, -1f, 1f);
                var abcd = float4(d2.z, d1.y, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = csum(abcd * abcd * float4(-1f, 1f, -1f, 1f));
                euler = float3(atan2(x1, x2), -asin(y1), 0f);
            }

            return euler.yzx;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion following the ZYX rotation order.
        /// All rotation angles are in radians and clockwise when looking along the rotation axis towards the origin.
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <returns>The Euler angle representation of the quaternion in ZYX order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 EulerZYX(quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = Unity.Mathematics.float3.zero;

            var y1 = d2.z + d1.y;
            if (y1 * y1 < cutoff)
            {
                var x1 = -d2.x + d1.z;
                var x2 = d3.x + d3.w - d3.y - d3.z;
                var z1 = -d2.y + d1.x;
                var z2 = d3.z + d3.w - d3.y - d3.x;
                euler = float3(atan2(x1, x2), asin(y1), atan2(z1, z2));
            }
            else //zxz
            {
                y1 = clamp(y1, -1f, 1f);
                var abcd = float4(d2.z, d1.y, d2.y, d1.x);
                var x1 = 2f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                var x2 = csum(abcd * abcd * float4(-1f, 1f, -1f, 1f));
                euler = float3(atan2(x1, x2), asin(y1), 0f);
            }

            return euler.zyx;
        }

        /// <summary>
        /// Returns the Euler angle representation of the quaternion. The returned angles depend on the specified order to apply the
        /// three rotations around the principal axes. All rotation angles are in radians and clockwise when looking along the
        /// rotation axis towards the origin.
        /// When the rotation order is known at compile time, to get the best performance you should use the specific
        /// Euler rotation constructors such as EulerZXY(...).
        /// </summary>
        /// <param name="q">The quaternion to convert to Euler angles.</param>
        /// <param name="order">The order in which the rotations are applied.</param>
        /// <returns>The Euler angle representation of the quaternion in the specified order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Euler(quaternion q, math.RotationOrder order = math.RotationOrder.Default)
        {
            switch (order)
            {
                case math.RotationOrder.XYZ:
                    return EulerXYZ(q);
                case math.RotationOrder.XZY:
                    return EulerXZY(q);
                case math.RotationOrder.YXZ:
                    return EulerYXZ(q);
                case math.RotationOrder.YZX:
                    return EulerYZX(q);
                case math.RotationOrder.ZXY:
                    return EulerZXY(q);
                case math.RotationOrder.ZYX:
                    return EulerZYX(q);
                default:
                    return Unity.Mathematics.float3.zero;
            }
        }

        /// <summary>
        /// Matrix columns multiplied by scale components
        /// m.c0.x * s.x | m.c1.x * s.y | m.c2.x * s.z
        /// m.c0.y * s.x | m.c1.y * s.y | m.c2.y * s.z
        /// m.c0.z * s.x | m.c1.z * s.y | m.c2.z * s.z
        /// </summary>
        /// <param name="m">Matrix to scale.</param>
        /// <param name="s">Scaling coefficients for each column.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 mulScale(float3x3 m, float3 s) => new float3x3(m.c0 * s.x, m.c1 * s.y, m.c2 * s.z);

        /// <summary>
        /// Matrix rows multiplied by scale components
        /// m.c0.x * s.x | m.c1.x * s.x | m.c2.x * s.x
        /// m.c0.y * s.y | m.c1.y * s.y | m.c2.y * s.y
        /// m.c0.z * s.z | m.c1.z * s.z | m.c2.z * s.z
        /// </summary>
        /// <param name="s">Scaling coefficients for each row.</param>
        /// <param name="m">Matrix to scale.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 scaleMul(float3 s, float3x3 m) => new float3x3(m.c0 * s, m.c1 * s, m.c2 * s);

        // Internal

        // SSE shuffles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 unpacklo(float4 a, float4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double4 unpacklo(double4 a, double4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.RightX, ShuffleComponent.LeftY, ShuffleComponent.RightY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 unpackhi(float4 a, float4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double4 unpackhi(double4 a, double4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftZ, ShuffleComponent.RightZ, ShuffleComponent.LeftW, ShuffleComponent.RightW);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 movelh(float4 a, float4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double4 movelh(double4 a, double4 b)
        {
            return shuffle(a, b, ShuffleComponent.LeftX, ShuffleComponent.LeftY, ShuffleComponent.RightX, ShuffleComponent.RightY);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float4 movehl(float4 a, float4 b)
        {
            return shuffle(b, a, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double4 movehl(double4 a, double4 b)
        {
            return shuffle(b, a, ShuffleComponent.LeftZ, ShuffleComponent.LeftW, ShuffleComponent.RightZ, ShuffleComponent.RightW);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint fold_to_uint(double x)  // utility for double hashing
        {
            LongDoubleUnion u;
            u.longValue = 0;
            u.doubleValue = x;
            return (uint)(u.longValue >> 32) ^ (uint)u.longValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint2 fold_to_uint(double2 x) { return uint2(fold_to_uint(x.x), fold_to_uint(x.y)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint3 fold_to_uint(double3 x) { return uint3(fold_to_uint(x.x), fold_to_uint(x.y), fold_to_uint(x.z)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint4 fold_to_uint(double4 x) { return uint4(fold_to_uint(x.x), fold_to_uint(x.y), fold_to_uint(x.z), fold_to_uint(x.w)); }

        [StructLayout(LayoutKind.Explicit)]
        internal struct LongDoubleUnion
        {
            [FieldOffset(0)]
            public long longValue;
            [FieldOffset(0)]
            public double doubleValue;
        }
    }
}
