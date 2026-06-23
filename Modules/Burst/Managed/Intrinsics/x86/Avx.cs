// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// AVX intrinsics
        /// </summary>
        public static class Avx
        {
            /// <summary>
            /// Evaluates to true at compile time if AVX intrinsics are supported.
            /// </summary>
            public static bool IsAvxSupported { get { return false; } }

            /// <summary>
            /// Compare predicates for scalar and packed compare intrinsic functions
            /// </summary>
            public enum CMP
            {
                ///<summary>
                /// Equal (ordered, nonsignaling)
                ///</summary>
                EQ_OQ = 0x00,
                /// <summary>
                /// Less-than (ordered, signaling)
                /// </summary>
                LT_OS = 0x01,
                /// <summary>
                /// Less-than-or-equal (ordered, signaling)
                /// </summary>
                LE_OS = 0x02,
                /// <summary>
                /// Unordered (nonsignaling)
                /// </summary>
                UNORD_Q = 0x03,
                /// <summary>
                /// Not-equal (unordered, nonsignaling)
                /// </summary>
                NEQ_UQ = 0x04,
                /// <summary>
                /// Not-less-than (unordered, signaling)
                /// </summary>
                NLT_US = 0x05,
                /// <summary>
                /// Not-less-than-or-equal (unordered, ignaling)
                /// </summary>
                NLE_US = 0x06,
                /// <summary>
                /// Ordered (nonsignaling)
                /// </summary>
                ORD_Q = 0x07,
                /// <summary>
                /// Equal (unordered, non-signaling)
                /// </summary>
                EQ_UQ = 0x08,
                /// <summary>
                /// Not-greater-than-or-equal (unordered, signaling)
                /// </summary>
                NGE_US = 0x09,
                /// <summary>
                /// Not-greater-than (unordered, signaling)
                /// </summary>
                NGT_US = 0x0A,
                /// <summary>
                /// False (ordered, nonsignaling)
                /// </summary>
                FALSE_OQ = 0x0B,
                /// <summary>
                /// Not-equal (ordered, non-signaling)
                /// </summary>
                NEQ_OQ = 0x0C,
                /// <summary>
                /// Greater-than-or-equal (ordered, signaling)
                /// </summary>
                GE_OS = 0x0D,
                /// <summary>
                /// Greater-than (ordered, signaling)
                /// </summary>
                GT_OS = 0x0E,
                /// <summary>
                /// True (unordered, non-signaling)
                /// </summary>
                TRUE_UQ = 0x0F,
                /// <summary>
                /// Equal (ordered, signaling)
                /// </summary>
                EQ_OS = 0x10,
                /// <summary>
                /// Less-than (ordered, nonsignaling)
                /// </summary>
                LT_OQ = 0x11,
                /// <summary>
                /// Less-than-or-equal (ordered, nonsignaling)
                /// </summary>
                LE_OQ = 0x12,
                /// <summary>
                /// Unordered (signaling)
                /// </summary>
                UNORD_S = 0x13,
                /// <summary>
                /// Not-equal (unordered, signaling)
                /// </summary>
                NEQ_US = 0x14,
                /// <summary>
                /// Not-less-than (unordered, nonsignaling)
                /// </summary>
                NLT_UQ = 0x15,
                /// <summary>
                /// Not-less-than-or-equal (unordered, nonsignaling)
                /// </summary>
                NLE_UQ = 0x16,
                /// <summary>
                /// Ordered (signaling)
                /// </summary>
                ORD_S = 0x17,
                /// <summary>
                /// Equal (unordered, signaling)
                /// </summary>
                EQ_US = 0x18,
                /// <summary>
                /// Not-greater-than-or-equal (unordered, nonsignaling)
                /// </summary>
                NGE_UQ = 0x19,
                /// <summary>
                /// Not-greater-than (unordered, nonsignaling)
                /// </summary>
                NGT_UQ = 0x1A,
                /// <summary>
                /// False (ordered, signaling)
                /// </summary>
                FALSE_OS = 0x1B,
                /// <summary>
                /// Not-equal (ordered, signaling)
                /// </summary>
                NEQ_OS = 0x1C,
                /// <summary>
                /// Greater-than-or-equal (ordered, nonsignaling)
                /// </summary>
                GE_OQ = 0x1D,
                /// <summary>
                /// Greater-than (ordered, nonsignaling)
                /// </summary>
                GT_OQ = 0x1E,
                /// <summary>
                /// True (unordered, signaling)
                /// </summary>
                TRUE_US = 0x1F,
            }

            /// <summary>
            /// Add packed double-precision (64-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_add_pd(v256 a, v256 b)
            {
                return new v256(Sse2.add_pd(a.Lo128, b.Lo128), Sse2.add_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed single-precision (32-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_add_ps(v256 a, v256 b)
            {
                return new v256(Sse.add_ps(a.Lo128, b.Lo128), Sse.add_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Alternatively add and subtract packed double-precision (64-bit) floating-point elements in a to/from packed elements in b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_addsub_pd(v256 a, v256 b)
            {
                return new v256(Sse3.addsub_pd(a.Lo128, b.Lo128), Sse3.addsub_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Alternatively add and subtract packed single-precision (32-bit) floating-point elements in a to/from packed elements in b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_addsub_ps(v256 a, v256 b)
            {
                return new v256(Sse3.addsub_ps(a.Lo128, b.Lo128), Sse3.addsub_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise AND of packed double-precision (64-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_and_pd(v256 a, v256 b)
            {
                return new v256(Sse2.and_pd(a.Lo128, b.Lo128), Sse2.and_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise AND of packed single-precision (32-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_and_ps(v256 a, v256 b)
            {
                return new v256(Sse.and_ps(a.Lo128, b.Lo128), Sse.and_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise NOT of packed double-precision (64-bit) floating-point elements in a and then AND with b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_andnot_pd(v256 a, v256 b)
            {
                return new v256(Sse2.andnot_pd(a.Lo128, b.Lo128), Sse2.andnot_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise NOT of packed single-precision (32-bit) floating-point elements in a and then AND with b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_andnot_ps(v256 a, v256 b)
            {
                return new v256(Sse.andnot_ps(a.Lo128, b.Lo128), Sse.andnot_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Blend packed double-precision (64-bit) floating-point elements from a and b using control mask imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VBLENDPD ymm1, ymm2, ymm3/v256, imm8
            /// Double-Precision Floating-Point values from the second source operand are
            /// conditionally merged with values from the first source operand and written
            /// to the destination. The immediate bits [3:0] determine whether the
            /// corresponding Double-Precision Floating Point value in the destination is
            /// copied from the second source or first source. If a bit in the mask,
            /// corresponding to a word, is "1", then the Double-Precision Floating-Point
            /// value in the second source operand is copied, else the value in the first
            /// source operand is copied
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blend_pd(v256 a, v256 b, int imm8)
            {
                return new v256(Sse4_1.blend_pd(a.Lo128, b.Lo128, imm8 & 0x3), Sse4_1.blend_pd(a.Hi128, b.Hi128, imm8 >> 2));
            }

            /// <summary>
            /// Blend packed single-precision (32-bit) floating-point elements from a and b using control mask imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VBLENDPS ymm1, ymm2, ymm3/v256, imm8
            /// Single precision floating point values from the second source operand are
            /// conditionally merged with values from the first source operand and written
            /// to the destination. The immediate bits [7:0] determine whether the
            /// corresponding single precision floating-point value in the destination is
            /// copied from the second source or first source. If a bit in the mask,
            /// corresponding to a word, is "1", then the single-precision floating-point
            /// value in the second source operand is copied, else the value in the first
            /// source operand is copied
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blend_ps(v256 a, v256 b, int imm8)
            {
                return new v256(Sse4_1.blend_ps(a.Lo128, b.Lo128, imm8 & 0xf), Sse4_1.blend_ps(a.Hi128, b.Hi128, imm8 >> 4));
            }

            /// <summary>
            /// Blend packed double-precision (64-bit) floating-point elements from a and b using mask, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VBLENDVPD ymm1, ymm2, ymm3/v256, ymm4
            /// Conditionally copy each quadword data element of double-precision
            /// floating-point value from the second source operand (third operand) and the
            /// first source operand (second operand) depending on mask bits defined in the
            /// mask register operand (fourth operand).
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blendv_pd(v256 a, v256 b, v256 mask)
            {
                return new v256(Sse4_1.blendv_pd(a.Lo128, b.Lo128, mask.Lo128), Sse4_1.blendv_pd(a.Hi128, b.Hi128, mask.Hi128));
            }

            /// <summary>
            /// Blend packed single-precision (32-bit) floating-point elements from a and b using mask, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// Blend Packed Single Precision Floating-Point Values
            /// **** VBLENDVPS ymm1, ymm2, ymm3/v256, ymm4
            /// Conditionally copy each dword data element of single-precision
            /// floating-point value from the second source operand (third operand) and the
            /// first source operand (second operand) depending on mask bits defined in the
            /// mask register operand (fourth operand).
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blendv_ps(v256 a, v256 b, v256 mask)
            {
                return new v256(Sse4_1.blendv_ps(a.Lo128, b.Lo128, mask.Lo128), Sse4_1.blendv_ps(a.Hi128, b.Hi128, mask.Hi128));
            }

            /// <summary>
            /// Divide packed double-precision (64-bit) floating-point elements in a by packed elements in b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VDIVPD ymm1, ymm2, ymm3/v256
            /// Performs an SIMD divide of the four packed double-precision floating-point
            /// values in the first source operand by the four packed double-precision
            /// floating-point values in the second source operand
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_div_pd(v256 a, v256 b)
            {
                return new v256(Sse2.div_pd(a.Lo128, b.Lo128), Sse2.div_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Divide packed single-precision (32-bit) floating-point elements in a by packed elements in b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// Divide Packed Single-Precision Floating-Point Values
            /// **** VDIVPS ymm1, ymm2, ymm3/v256
            /// Performs an SIMD divide of the eight packed single-precision
            /// floating-point values in the first source operand by the eight packed
            /// single-precision floating-point values in the second source operand
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_div_ps(v256 a, v256 b)
            {
                return new v256(Sse.div_ps(a.Lo128, b.Lo128), Sse.div_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Conditionally multiply the packed single-precision (32-bit)
            /// floating-point elements in a and b using the high 4 bits in
            /// imm8, sum the four products, and conditionally store the sum in
            /// dst using the low 4 bits of imm8.
            /// </summary>
            /// <remarks>
            /// **** VDPPS ymm1, ymm2, ymm3/v256, imm8
            /// Multiplies the packed single precision floating point values in the
            /// first source operand with the packed single-precision floats in the
            /// second source. Each of the four resulting single-precision values is
            /// conditionally summed depending on a mask extracted from the high 4 bits
            /// of the immediate operand. This sum is broadcast to each of 4 positions
            /// in the destination if the corresponding bit of the mask selected from
            /// the low 4 bits of the immediate operand is "1". If the corresponding
            /// low bit 0-3 of the mask is zero, the destination is set to zero.
            /// The process is replicated for the high elements of the destination.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_dp_ps(v256 a, v256 b, int imm8)
            {
                return new v256(Sse4_1.dp_ps(a.Lo128, b.Lo128, imm8), Sse4_1.dp_ps(a.Hi128, b.Hi128, imm8));
            }

            /// <summary>
            /// Horizontally add adjacent pairs of double-precision (64-bit) floating-point elements in a and b, and pack the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VHADDPD ymm1, ymm2, ymm3/v256
            /// Adds pairs of adjacent double-precision floating-point values in the
            /// first source operand and second source operand and stores results in
            /// the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hadd_pd(v256 a, v256 b)
            {
                return new v256(Sse3.hadd_pd(a.Lo128, b.Lo128), Sse3.hadd_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally add adjacent pairs of single-precision (32-bit) floating-point elements in a and b, and pack the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VHADDPS ymm1, ymm2, ymm3/v256
            /// Adds pairs of adjacent single-precision floating-point values in the
            /// first source operand and second source operand and stores results in
            /// the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hadd_ps(v256 a, v256 b)
            {
                return new v256(Sse3.hadd_ps(a.Lo128, b.Lo128), Sse3.hadd_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally subtract adjacent pairs of double-precision (64-bit) floating-point elements in a and b, and pack the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VHSUBPD ymm1, ymm2, ymm3/v256
            /// Subtract pairs of adjacent double-precision floating-point values in
            /// the first source operand and second source operand and stores results
            /// in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hsub_pd(v256 a, v256 b)
            {
                return new v256(Sse3.hsub_pd(a.Lo128, b.Lo128), Sse3.hsub_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally add adjacent pairs of single-precision (32-bit) floating-point elements in a and b, and pack the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VHSUBPS ymm1, ymm2, ymm3/v256
            /// Subtract pairs of adjacent single-precision floating-point values in
            /// the first source operand and second source operand and stores results
            /// in the destination.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hsub_ps(v256 a, v256 b)
            {
                return new v256(Sse3.hsub_ps(a.Lo128, b.Lo128), Sse3.hsub_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed double-precision (64-bit) floating-point elements in a and b, and store packed maximum values in dst.
            /// </summary>
            /// <remarks>
            /// **** VMAXPD ymm1, ymm2, ymm3/v256
            /// Performs an SIMD compare of the packed double-precision floating-point
            /// values in the first source operand and the second source operand and
            /// returns the maximum value for each pair of values to the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_pd(v256 a, v256 b)
            {
                return new v256(Sse2.max_pd(a.Lo128, b.Lo128), Sse2.max_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed single-precision (32-bit) floating-point elements in a and b, and store packed maximum values in dst.
            /// </summary>
            /// <remarks>
            /// **** VMAXPS ymm1, ymm2, ymm3/v256
            /// Performs an SIMD compare of the packed single-precision floating-point
            /// values in the first source operand and the second source operand and
            /// returns the maximum value for each pair of values to the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_ps(v256 a, v256 b)
            {
                return new v256(Sse.max_ps(a.Lo128, b.Lo128), Sse.max_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed double-precision (64-bit) floating-point elements in a and b, and store packed minimum values in dst. 
            /// </summary>
            /// <remarks>
            /// **** VMINPD ymm1, ymm2, ymm3/v256
            /// Performs an SIMD compare of the packed double-precision floating-point
            /// values in the first source operand and the second source operand and
            /// returns the minimum value for each pair of values to the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_pd(v256 a, v256 b)
            {
                return new v256(Sse2.min_pd(a.Lo128, b.Lo128), Sse2.min_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed single-precision (32-bit) floating-point elements in a and b, and store packed minimum values in dst.
            /// </summary>
            /// <remarks>
            /// **** VMINPS ymm1, ymm2, ymm3/v256
            /// Performs an SIMD compare of the packed single-precision floating-point
            /// values in the first source operand and the second source operand and
            /// returns the minimum value for each pair of values to the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_ps(v256 a, v256 b)
            {
                return new v256(Sse.min_ps(a.Lo128, b.Lo128), Sse.min_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VMULPD ymm1, ymm2, ymm3/v256
            /// Performs a SIMD multiply of the four packed double-precision floating-point
            /// values from the first Source operand to the Second Source operand, and
            /// stores the packed double-precision floating-point results in the
            /// destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mul_pd(v256 a, v256 b)
            {
                return new v256(Sse2.mul_pd(a.Lo128, b.Lo128), Sse2.mul_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VMULPS ymm1, ymm2, ymm3/v256
            /// Performs an SIMD multiply of the eight packed single-precision
            /// floating-point values from the first source operand to the second source
            /// operand, and stores the packed double-precision floating-point results in
            /// the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mul_ps(v256 a, v256 b)
            {
                return new v256(Sse.mul_ps(a.Lo128, b.Lo128), Sse.mul_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise OR of packed double-precision (64-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VORPD ymm1, ymm2, ymm3/v256
            /// Performs a bitwise logical OR of the four packed double-precision
            /// floating-point values from the first source operand and the second
            /// source operand, and stores the result in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_or_pd(v256 a, v256 b)
            {
                return new v256(Sse2.or_pd(a.Lo128, b.Lo128), Sse2.or_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise OR of packed single-precision (32-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VORPS ymm1, ymm2, ymm3/v256
            /// Performs a bitwise logical OR of the eight packed single-precision
            /// floating-point values from the first source operand and the second
            /// source operand, and stores the result in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_or_ps(v256 a, v256 b)
            {
                return new v256(Sse.or_ps(a.Lo128, b.Lo128), Sse.or_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Shuffle double-precision (64-bit) floating-point elements within 128-bit lanes using the control in imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VSHUFPD ymm1, ymm2, ymm3/v256, imm8
            /// Moves either of the two packed double-precision floating-point values from
            /// each double quadword in the first source operand into the low quadword
            /// of each double quadword of the destination; moves either of the two packed
            /// double-precision floating-point values from the second source operand into
            /// the high quadword of each double quadword of the destination operand.
            /// The selector operand determines which values are moved to the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_shuffle_pd(v256 a, v256 b, int imm8)
            {
                return new v256(Sse2.shuffle_pd(a.Lo128, b.Lo128, imm8 & 3), Sse2.shuffle_pd(a.Hi128, b.Hi128, imm8 >> 2));
            }

            /// <summary>
            /// Shuffle single-precision (32-bit) floating-point elements in a within 128-bit lanes using the control in imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VSHUFPS ymm1, ymm2, ymm3/v256, imm8
            /// Moves two of the four packed single-precision floating-point values
            /// from each double qword of the first source operand into the low
            /// quadword of each double qword of the destination; moves two of the four
            /// packed single-precision floating-point values from each double qword of
            /// the second source operand into to the high quadword of each double qword
            /// of the destination. The selector operand determines which values are moved
            /// to the destination.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_shuffle_ps(v256 a, v256 b, int imm8)
            {
                return new v256(Sse.shuffle_ps(a.Lo128, b.Lo128, imm8), Sse.shuffle_ps(a.Hi128, b.Hi128, imm8));
            }

            /// <summary>
            /// Subtract packed double-precision (64-bit) floating-point elements in b from packed double-precision (64-bit) floating-point elements in a, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VSUBPD ymm1, ymm2, ymm3/v256
            /// Performs an SIMD subtract of the four packed double-precision floating-point
            /// values of the second Source operand from the first Source operand, and
            /// stores the packed double-precision floating-point results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sub_pd(v256 a, v256 b)
            {
                return new v256(Sse2.sub_pd(a.Lo128, b.Lo128), Sse2.sub_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed single-precision (32-bit) floating-point elements in b from packed single-precision (32-bit) floating-point elements in a, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VSUBPS ymm1, ymm2, ymm3/v256
            /// Performs an SIMD subtract of the eight packed single-precision
            /// floating-point values in the second Source operand from the First Source
            /// operand, and stores the packed single-precision floating-point results in
            /// the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sub_ps(v256 a, v256 b)
            {
                return new v256(Sse.sub_ps(a.Lo128, b.Lo128), Sse.sub_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise XOR of packed double-precision (64-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VXORPD ymm1, ymm2, ymm3/v256
            /// Performs a bitwise logical XOR of the four packed double-precision
            /// floating-point values from the first source operand and the second
            /// source operand, and stores the result in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_xor_pd(v256 a, v256 b)
            {
                return new v256(Sse2.xor_pd(a.Lo128, b.Lo128), Sse2.xor_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise XOR of packed single-precision (32-bit) floating-point elements in a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VXORPS ymm1, ymm2, ymm3/v256
            /// Performs a bitwise logical XOR of the eight packed single-precision
            /// floating-point values from the first source operand and the second
            /// source operand, and stores the result in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_xor_ps(v256 a, v256 b)
            {
                return new v256(Sse.xor_ps(a.Lo128, b.Lo128), Sse.xor_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed double-precision (64-bit) floating-point elements in a and b based on the comparison operand specified by imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCMPPD xmm1, xmm2, xmm3/v128, imm8
            /// Performs an SIMD compare of the four packed double-precision floating-point
            /// values in the second source operand (third operand) and the first source
            /// operand (second operand) and returns the results of the comparison to the
            /// destination operand (first operand). The comparison predicate operand
            /// (immediate) specifies the type of comparison performed on each of the pairs
            /// of packed values.
            /// For 128-bit intrinsic function with compare predicate values in range 0-7
            /// compiler may generate SSE2 instructions if it is warranted for performance
            /// reasons.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmp_pd(v128 a, v128 b, int imm8)
            {
                switch ((CMP)(imm8 & 0x1F))
                {
                    // The first variants map to SSE variants
                    case CMP.EQ_OQ: return Sse2.cmpeq_pd(a, b);
                    case CMP.LT_OS: return Sse2.cmplt_pd(a, b);
                    case CMP.LE_OS: return Sse2.cmple_pd(a, b);
                    case CMP.UNORD_Q: return Sse2.cmpunord_pd(a, b);
                    case CMP.NEQ_UQ: return Sse2.cmpneq_pd(a, b);
                    case CMP.NLT_US: return Sse2.cmpnlt_pd(a, b);
                    case CMP.NLE_US: return Sse2.cmpnle_pd(a, b);
                    case CMP.ORD_Q: return Sse2.cmpord_pd(a, b);

                    case CMP.EQ_UQ: return Sse2.or_pd(Sse2.cmpeq_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.NGE_UQ: return Sse2.or_pd(Sse2.cmpnge_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.NGT_US: return Sse2.or_pd(Sse2.cmpngt_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.FALSE_OQ: return default;
                    case CMP.NEQ_OQ: return Sse2.and_pd(Sse2.cmpneq_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.GE_OS: return Sse2.and_pd(Sse2.cmpge_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.GT_OS: return Sse2.and_pd(Sse2.cmpgt_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.TRUE_UQ: return new v128(-1);

                    case CMP.EQ_OS: return Sse2.and_pd(Sse2.cmpeq_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.LT_OQ: return Sse2.and_pd(Sse2.cmplt_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.LE_OQ: return Sse2.and_pd(Sse2.cmple_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.UNORD_S: return Sse2.cmpunord_pd(a, b);
                    case CMP.NEQ_US: return Sse2.cmpneq_pd(a, b);
                    case CMP.NLT_UQ: return Sse2.or_pd(Sse2.cmpnlt_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.NLE_UQ: return Sse2.or_pd(Sse2.cmpnle_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.ORD_S: return Sse2.cmpord_pd(a, b);
                    case CMP.EQ_US: return Sse2.or_pd(Sse2.cmpeq_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.NGE_US: return Sse2.or_pd(Sse2.cmpnge_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.NGT_UQ: return Sse2.or_pd(Sse2.cmpngt_pd(a, b), Sse2.cmpunord_pd(a, b));
                    case CMP.FALSE_OS: return default;
                    case CMP.NEQ_OS: return Sse2.and_pd(Sse2.cmpneq_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.GE_OQ: return Sse2.and_pd(Sse2.cmpge_pd(a, b), Sse2.cmpord_pd(a, b));
                    case CMP.GT_OQ: return Sse2.and_pd(Sse2.cmpgt_pd(a, b), Sse2.cmpord_pd(a, b));
                    default:
                        return new v128(-1);
                }
            }

            /// <summary>
            /// Compare packed double-precision (64-bit) floating-point elements in a and b based on the comparison operand specified by imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCMPPD ymm1, ymm2, ymm3/v256, imm8
            /// Performs an SIMD compare of the four packed double-precision floating-point
            /// values in the second source operand (third operand) and the first source
            /// operand (second operand) and returns the results of the comparison to the
            /// destination operand (first operand). The comparison predicate operand
            /// (immediate) specifies the type of comparison performed on each of the pairs
            /// of packed values.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmp_pd(v256 a, v256 b, int imm8)
            {
                return new v256(cmp_pd(a.Lo128, b.Lo128, imm8), cmp_pd(a.Hi128, b.Hi128, imm8));
            }

            /// **** VCMPPS ymm1, ymm2, ymm3/v256, imm8
            /// <summary>
            /// Compare packed single-precision (32-bit) floating-point elements in a and b based on the comparison operand specified by imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCMPPS xmm1, xmm2, xmm3/v256, imm8
            /// Performs a SIMD compare of the packed single-precision floating-point values
            /// in the second source operand (third operand) and the first source operand
            /// (second operand) and returns the results of the comparison to the
            /// destination operand (first operand). The comparison predicate operand
            /// (immediate) specifies the type of comparison performed on each of the pairs
            /// of packed values.
            /// For 128-bit intrinsic function with compare predicate values in range 0-7
            /// compiler may generate SSE2 instructions if it is warranted for performance
            /// reasons.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmp_ps(v128 a, v128 b, int imm8)
            {
                switch ((CMP)(imm8 & 0x1F))
                {
                    // The first variants map to SSE variants
                    case CMP.EQ_OQ: return Sse.cmpeq_ps(a, b);
                    case CMP.LT_OS: return Sse.cmplt_ps(a, b);
                    case CMP.LE_OS: return Sse.cmple_ps(a, b);
                    case CMP.UNORD_Q: return Sse.cmpunord_ps(a, b);
                    case CMP.NEQ_UQ: return Sse.cmpneq_ps(a, b);
                    case CMP.NLT_US: return Sse.cmpnlt_ps(a, b);
                    case CMP.NLE_US: return Sse.cmpnle_ps(a, b);
                    case CMP.ORD_Q: return Sse.cmpord_ps(a, b);

                    case CMP.EQ_UQ: return Sse.or_ps(Sse.cmpeq_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.NGE_UQ: return Sse.or_ps(Sse.cmpnge_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.NGT_US: return Sse.or_ps(Sse.cmpngt_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.FALSE_OQ: return default;
                    case CMP.NEQ_OQ: return Sse.and_ps(Sse.cmpneq_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.GE_OS: return Sse.and_ps(Sse.cmpge_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.GT_OS: return Sse.and_ps(Sse.cmpgt_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.TRUE_UQ: return new v128(-1);

                    case CMP.EQ_OS: return Sse.and_ps(Sse.cmpeq_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.LT_OQ: return Sse.and_ps(Sse.cmplt_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.LE_OQ: return Sse.and_ps(Sse.cmple_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.UNORD_S: return Sse.cmpunord_ps(a, b);
                    case CMP.NEQ_US: return Sse.cmpneq_ps(a, b);
                    case CMP.NLT_UQ: return Sse.or_ps(Sse.cmpnlt_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.NLE_UQ: return Sse.or_ps(Sse.cmpnle_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.ORD_S: return Sse.cmpord_ps(a, b);
                    case CMP.EQ_US: return Sse.or_ps(Sse.cmpeq_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.NGE_US: return Sse.or_ps(Sse.cmpnge_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.NGT_UQ: return Sse.or_ps(Sse.cmpngt_ps(a, b), Sse.cmpunord_ps(a, b));
                    case CMP.FALSE_OS: return default;
                    case CMP.NEQ_OS: return Sse.and_ps(Sse.cmpneq_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.GE_OQ: return Sse.and_ps(Sse.cmpge_ps(a, b), Sse.cmpord_ps(a, b));
                    case CMP.GT_OQ: return Sse.and_ps(Sse.cmpgt_ps(a, b), Sse.cmpord_ps(a, b));
                    default:
                        return new v128(-1);
                }
            }

            /// <summary>
            /// Compare packed single-precision (32-bit) floating-point elements in a and b based on the comparison operand specified by imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCMPPS xmm1, xmm2, xmm3/v256, imm8
            /// Performs a SIMD compare of the packed single-precision floating-point values
            /// in the second source operand (third operand) and the first source operand
            /// (second operand) and returns the results of the comparison to the
            /// destination operand (first operand). The comparison predicate operand
            /// (immediate) specifies the type of comparison performed on each of the pairs
            /// of packed values.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmp_ps(v256 a, v256 b, int imm8)
            {
                return new v256(cmp_ps(a.Lo128, b.Lo128, imm8), cmp_ps(a.Hi128, b.Hi128, imm8));
            }

            /// <summary>
            /// Compare the lower double-precision (64-bit) floating-point
            /// element in a and b based on the comparison operand specified by
            /// imm8, store the result in the lower element of dst, and copy
            /// the upper element from a to the upper element of dst.
            /// </summary>
            /// <remarks>
            /// **** VCMPSD xmm1, xmm2, xmm3/m64, imm8
            /// Compares the low double-precision floating-point values in the second source
            /// operand (third operand) and the first source operand (second operand) and
            /// returns the results in of the comparison to the destination operand (first
            /// operand). The comparison predicate operand (immediate operand) specifies the
            /// type of comparison performed.
            /// For compare predicate values in range 0-7 compiler may generate SSE2
            /// instructions if it is warranted for performance reasons.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmp_sd(v128 a, v128 b, int imm8)
            {
                v128 full = cmp_pd(a, b, imm8);
                return new v128(full.ULong0, a.ULong1);
            }

            /// <summary>
            /// Compare the lower single-precision (32-bit) floating-point
            /// element in a and b based on the comparison operand specified by
            /// imm8, store the result in the lower element of dst, and copy
            /// the upper 3 packed elements from a to the upper elements of
            /// dst.
            /// </summary>
            /// <remarks>
            /// **** VCMPSS xmm1, xmm2, xmm3/m64, imm8
            /// Compares the low single-precision floating-point values in the second source
            /// operand (third operand) and the first source operand (second operand) and
            /// returns the results of the comparison to the destination operand (first
            /// operand). The comparison predicate operand (immediate operand) specifies
            /// the type of comparison performed.
            /// For compare predicate values in range 0-7 compiler may generate SSE2
            /// instructions if it is warranted for performance reasons.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmp_ss(v128 a, v128 b, int imm8)
            {
                v128 full = cmp_ps(a, b, imm8);
                return new v128(full.UInt0, a.UInt1, a.UInt2, a.UInt3);
            }

            /// <summary>
            /// Convert packed 32-bit integers in a to packed double-precision (64-bit) floating-point elements, and store the results in dst.
            /// </summary>
            /// <param name="a"></param>
            /// <remarks>
            /// **** VCVTDQ2PD ymm1, xmm2/v128
            /// Converts four packed signed doubleword integers in the source operand to
            /// four packed double-precision floating-point values in the destination
            /// </remarks>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi32_pd(v128 a)
            {
                return new v256((double)a.SInt0, (double)a.SInt1, (double)a.SInt2, (double)a.SInt3);
            }

            /// <summary>
            /// Convert packed 32-bit integers in a to packed single-precision (32-bit) floating-point elements, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTDQ2PS ymm1, ymm2/v256
            /// Converts eight packed signed doubleword integers in the source operand to
            /// eight packed double-precision floating-point values in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi32_ps(v256 a)
            {
                return new v256(Sse2.cvtepi32_ps(a.Lo128), Sse2.cvtepi32_ps(a.Hi128));
            }

            /// <summary>
            /// Convert packed double-precision (64-bit) floating-point
            /// elements in a to packed single-precision (32-bit)
            /// floating-point elements, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTPD2PS xmm1, ymm2/v256
            /// Converts four packed double-precision floating-point values in the source
            /// operand to four packed single-precision floating-point values in the
            /// destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_cvtpd_ps(v256 a)
            {
                v128 lo = Sse2.cvtpd_ps(a.Lo128);
                v128 hi = Sse2.cvtpd_ps(a.Hi128);
                return new v128(lo.Float0, lo.Float1, hi.Float0, hi.Float1);
            }

            /// <summary>
            /// Convert packed single-precision (32-bit) floating-point
            /// elements in a to packed 32-bit integers, and store the results
            /// in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTPS2DQ ymm1, ymm2/v256
            /// Converts eight packed single-precision floating-point values in the source
            /// operand to eight signed doubleword integers in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtps_epi32(v256 a)
            {
                return new v256(Sse2.cvtps_epi32(a.Lo128), Sse2.cvtps_epi32(a.Hi128));
            }

            /// <summary>
            /// Convert packed single-precision (32-bit) floating-point
            /// elements in a to packed double-precision (64-bit)
            /// floating-point elements, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTPS2PD ymm1, xmm2/v128
            /// Converts four packed single-precision floating-point values in the source
            /// operand to four packed double-precision floating-point values in the
            /// destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtps_pd(v128 a)
            {
                // The normal Burst IR does fine here.
                return new v256(a.Float0, a.Float1, a.Float2, a.Float3);
            }

            /// <summary>
            /// Convert packed double-precision (64-bit) floating-point
            /// elements in a to packed 32-bit integers with truncation, and
            /// store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTTPD2DQ xmm1, ymm2/v256
            /// Converts four packed double-precision floating-point values in the source
            /// operand to four packed signed doubleword integers in the destination.
            /// When a conversion is inexact, a truncated (round toward zero) value is
            /// returned. If a converted result is larger than the maximum signed doubleword
            /// integer, the floating-point invalid exception is raised, and if this
            /// exception is masked, the indefinite integer value (80000000H) is returned
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_cvttpd_epi32(v256 a)
            {
                return new v128((int)a.Double0, (int)a.Double1, (int)a.Double2, (int)a.Double3);
            }

            /// <summary>
            /// Convert packed double-precision(64-bit) floating-point elements
            /// in a to packed 32-bit integers, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTPD2DQ xmm1, ymm2/v256
            /// Converts four packed double-precision floating-point values in the source
            /// operand to four packed signed doubleword integers in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v128 mm256_cvtpd_epi32(v256 a)
            {
                v128 q = Sse2.cvtpd_epi32(new v128(a.Double0, a.Double1));
                v128 r = Sse2.cvtpd_epi32(new v128(a.Double2, a.Double3));
                return new v128(q.SInt0, q.SInt1, r.SInt0, r.SInt1);
            }

            /// <summary>
            /// Convert packed single-precision (32-bit) floating-point
            /// elements in a to packed 32-bit integers with truncation, and
            /// store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VCVTTPS2DQ ymm1, ymm2/v256
            /// Converts eight packed single-precision floating-point values in the source
            /// operand to eight signed doubleword integers in the destination.
            /// When a conversion is inexact, a truncated (round toward zero) value is
            /// returned. If a converted result is larger than the maximum signed doubleword
            /// integer, the floating-point invalid exception is raised, and if this
            /// exception is masked, the indefinite integer value (80000000H) is returned
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvttps_epi32(v256 a)
            {
                return new v256(Sse2.cvttps_epi32(a.Lo128), Sse2.cvttps_epi32(a.Hi128));
            }

            /*
             * Convert Scalar Single-Precision Floating-point value in 256-bit vector to
             * equivalent C/C++ float type.
             */
            /// <summary>
            /// Copy the lower single-precision (32-bit) floating-point element of a to dst.
            /// </summary>
            /// <remarks>
            /// Identical in HPC# to accessing Float0, kept for compatibility with existing code while porting.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Float</returns>
            [DebuggerStepThrough]
            public static float mm256_cvtss_f32(v256 a)
            {
                // Burst IR is fine here.
                return a.Float0;
            }

            /// <summary>
            /// Extract 128 bits (composed of 4 packed single-precision (32-bit) floating-point elements) from a, selected with imm8, and store the result in dst.
            /// </summary>
            /// <remarks>
            /// **** VEXTRACTF128 xmm1/v128, ymm2, imm8
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_extractf128_ps(v256 a, int imm8)
            {
                return imm8 != 0 ? a.Hi128 : a.Lo128;
            }

            /// <summary>
            /// Extract 128 bits (composed of 2 packed double-precision (64-bit) floating-point elements) from a, selected with imm8, and store the result in dst.
            /// </summary>
            /// <remarks>
            /// **** VEXTRACTF128 xmm1/v128, ymm2, imm8
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_extractf128_pd(v256 a, int imm8)
            {
                return imm8 != 0 ? a.Hi128 : a.Lo128;
            }

            /// <summary>
            /// Extract 128 bits (composed of integer data) from a, selected with imm8, and store the result in dst.
            /// </summary>
            /// <remarks>
            /// **** VEXTRACTF128 xmm1/v128, ymm2, imm8
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_extractf128_si256(v256 a, int imm8)
            {
                return imm8 != 0 ? a.Hi128 : a.Lo128;
            }

            /// <summary>
            /// Zeros the contents of all YMM registers
            /// </summary>
            /// <remarks>
            /// **** VZEROALL
            /// </remarks>
            [DebuggerStepThrough]
            public static void mm256_zeroall()
            {
                // This is a no-op in C# land
            }

            /// <summary>
            /// Zero the upper 128 bits of all YMM registers; the lower 128-bits of the registers are unmodified.
            /// </summary>
            /// <remarks>
            /// **** VZEROUPPER
            /// </remarks>
            [DebuggerStepThrough]
            public static void mm256_zeroupper()
            {
                // This is a no-op in C# land
            }

            /// <summary>
            /// Shuffle single-precision (32-bit) floating-point elements in a using the control in b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPS xmm1, xmm2, xmm3/v128
            /// Permute Single-Precision Floating-Point values in the first source operand
            /// using 8-bit control fields in the low bytes of corresponding elements the
            /// shuffle control and store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 permutevar_ps(v128 a, v128 b)
            {
                v128 dst = default;
                uint* dptr = &dst.UInt0;
                uint* aptr = &a.UInt0;
                int* bptr = &b.SInt0;

                for (int i = 0; i < 4; ++i)
                {
                    int ndx = bptr[i] & 3;
                    dptr[i] = aptr[ndx];
                }

                return dst;
            }

            /// <summary>
            /// Shuffle single-precision (32-bit) floating-point elements in a within 128-bit lanes using the control in b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPS ymm1, ymm2, ymm3/v256
            /// Permute Single-Precision Floating-Point values in the first source operand
            /// using 8-bit control fields in the low bytes of corresponding elements the
            /// shuffle control and store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permutevar_ps(v256 a, v256 b)
            {
                return new v256(permutevar_ps(a.Lo128, b.Lo128), permutevar_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Shuffle single-precision (32-bit) floating-point elements in a using the control in imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPS xmm1, xmm2/v128, imm8
            /// Permute Single-Precision Floating-Point values in the first source operand
            /// using four 2-bit control fields in the 8-bit immediate and store results
            /// in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 permute_ps(v128 a, int imm8)
            {
                return Sse2.shuffle_epi32(a, imm8);
            }

            /// <summary>
            /// Shuffle single-precision (32-bit) floating-point elements in a
            /// within 128-bit lanes using the control in imm8, and store the
            /// results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPS ymm1, ymm2/v256, imm8
            /// Permute Single-Precision Floating-Point values in the first source operand
            /// using four 2-bit control fields in the 8-bit immediate and store results
            /// in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute_ps(v256 a, int imm8)
            {
                return new v256(permute_ps(a.Lo128, imm8), permute_ps(a.Hi128, imm8));
            }

            /// <summary>
            /// Shuffle double-precision (64-bit) floating-point elements in a using the control in b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPD xmm1, xmm2, xmm3/v128
            /// Permute Double-Precision Floating-Point values in the first source operand
            /// using 8-bit control fields in the low bytes of the second source operand
            /// and store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 permutevar_pd(v128 a, v128 b)
            {
                v128 dst = default;
                double* dptr = &dst.Double0;
                double* aptr = &a.Double0;
                dptr[0] = aptr[(int)(b.SLong0 & 2) >> 1];
                dptr[1] = aptr[(int)(b.SLong1 & 2) >> 1];
                return dst;
            }

            /// <summary>
            /// Shuffle double-precision (64-bit) floating-point elements in a within 128-bit lanes using the control in b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPD ymm1, ymm2, ymm3/v256
            /// Permute Double-Precision Floating-Point values in the first source operand
            /// using 8-bit control fields in the low bytes of the second source operand
            /// and store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permutevar_pd(v256 a, v256 b)
            {
                v256 dst = default;
                double* dptr = &dst.Double0;
                double* aptr = &a.Double0;
                dptr[0] = aptr[(int)(b.SLong0 & 2) >> 1];
                dptr[1] = aptr[(int)(b.SLong1 & 2) >> 1];
                dptr[2] = aptr[2 + ((int)(b.SLong2 & 2) >> 1)];
                dptr[3] = aptr[2 + ((int)(b.SLong3 & 2) >> 1)];
                return dst;
            }

            /*
             * Permute Double-Precision Floating-Point Values
             */
            /// <summary>
            /// Shuffle double-precision (64-bit) floating-point elements in a within 128-bit lanes using the control in imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPD ymm1, ymm2/v256, imm8
            /// Permute Double-Precision Floating-Point values in the first source operand
            /// using two, 1-bit control fields in the low 2 bits of the 8-bit immediate
            /// and store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute_pd(v256 a, int imm8)
            {
                return new v256(permute_pd(a.Lo128, imm8 & 3), permute_pd(a.Hi128, imm8 >> 2));
            }

            /// <summary>
            /// Shuffle double-precision (64-bit) floating-point elements in a using the control in imm8, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERMILPD xmm1, xmm2/v128, imm8
            /// Permute Double-Precision Floating-Point values in the first source operand
            /// using two, 1-bit control fields in the low 2 bits of the 8-bit immediate
            /// and store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 permute_pd(v128 a, int imm8)
            {
                v128 dst = default;
                double* dptr = &dst.Double0;
                double* aptr = &a.Double0;
                dptr[0] = aptr[imm8 & 1];
                dptr[1] = aptr[(imm8 >> 1) & 1];
                return dst;
            }

            private static v128 Select4(v256 src1, v256 src2, int control)
            {
                switch (control & 3)
                {
                    case 0: return src1.Lo128;
                    case 1: return src1.Hi128;
                    case 2: return src2.Lo128;
                    default: return src2.Hi128;
                }
            }

            /// <summary>
            /// Shuffle 128-bits (composed of 4 packed single-precision (32-bit) floating-point elements) selected by imm8 from a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERM2F128 ymm1, ymm2, ymm3/v256, imm8
            /// Permute 128 bit floating-point-containing fields from the first source
            /// operand and second source operand using bits in the 8-bit immediate and
            /// store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute2f128_ps(v256 a, v256 b, int imm8)
            {
                return new v256(Select4(a, b, imm8), Select4(a, b, imm8 >> 4));
            }

            /// <summary>
            /// Shuffle 128-bits (composed of 2 packed double-precision (64-bit) floating-point elements) selected by imm8 from a and b, and store the results in dst. 
            /// </summary>
            /// <remarks>
            /// **** VPERM2F128 ymm1, ymm2, ymm3/v256, imm8
            /// Permute 128 bit floating-point-containing fields from the first source
            /// operand and second source operand using bits in the 8-bit immediate and
            /// store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute2f128_pd(v256 a, v256 b, int imm8)
            {
                return mm256_permute2f128_ps(a, b, imm8);
            }


            /// <summary>
            /// Shuffle 128-bits (composed of integer data) selected by imm8 from a and b, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VPERM2F128 ymm1, ymm2, ymm3/v256, imm8
            /// Permute 128 bit floating-point-containing fields from the first source
            /// operand and second source operand using bits in the 8-bit immediate and
            /// store results in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute2f128_si256(v256 a, v256 b, int imm8)
            {
                return mm256_permute2f128_ps(a, b, imm8);
            }

            /// <summary>
            /// Broadcast a single-precision (32-bit) floating-point element from memory to all elements of dst.
            /// </summary>
            /// <remarks>
            /// **** VBROADCASTSS ymm1, m32
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcast_ss(void* ptr)
            {
                return new v256(*(uint*)ptr);
            }

            /// <summary>
            /// Broadcast a single-precision (32-bit) floating-point element from memory to all elements of dst.
            /// </summary>
            /// <remarks>
            /// **** VBROADCASTSS xmm1, m32
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcast_ss(void* ptr)
            {
                return new v128(*(uint*)ptr);
            }

            /// <summary>
            /// Broadcast a double-precision (64-bit) floating-point element from memory to all elements of dst.
            /// </summary>
            /// <remarks>
            /// **** VBROADCASTSD ymm1, m64
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcast_sd(void* ptr)
            {
                return new v256(*(double*)ptr);
            }

            /// <summary>
            /// Broadcast 128 bits from memory (composed of 4 packed single-precision (32-bit) floating-point elements) to all elements of dst.
            /// </summary>
            /// <remarks>
            /// **** VBROADCASTF128 ymm1, v128
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcast_ps(void* ptr)
            {
                v128 a = Sse.loadu_ps(ptr);
                return new v256(a, a);
            }

            /// <summary>
            /// Broadcast 128 bits from memory (composed of 2 packed double-precision (64-bit) floating-point elements) to all elements of dst.
            /// </summary>
            /// <param name="ptr">Pointer</param>
            /// <returns>
            /// **** VBROADCASTF128 ymm1, v128
            /// </returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcast_pd(void* ptr)
            {
                return mm256_broadcast_ps(ptr);
            }

            /// <summary>
            /// Copy a to dst, then insert 128 bits (composed of 4 packed single-precision (32-bit) floating-point elements) from b into dst at the location specified by imm8.
            /// </summary>
            /// <remarks>
            /// **** VINSERTF128 ymm1, ymm2, xmm3/v128, imm8
            /// Performs an insertion of 128-bits of packed floating-point values from the
            /// second source operand into an the destination at an 128-bit offset from
            /// imm8[0]. The remaining portions of the destination are written by the
            /// corresponding fields of the first source operand
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insertf128_ps(v256 a, v128 b, int imm8)
            {
                if (0 == (imm8 & 1))
                    return new v256(b, a.Hi128);
                else
                    return new v256(a.Lo128, b);
            }

            /// <summary>
            /// Copy a to dst, then insert 128 bits (composed of 2 packed double-precision (64-bit) floating-point elements) from b into dst at the location specified by imm8.
            /// </summary>
            /// <remarks>
            /// **** VINSERTF128 ymm1, ymm2, xmm3/v128, imm8
            /// Performs an insertion of 128-bits of packed floating-point values from the
            /// second source operand into an the destination at an 128-bit offset from
            /// imm8[0]. The remaining portions of the destination are written by the
            /// corresponding fields of the first source operand
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insertf128_pd(v256 a, v128 b, int imm8)
            {
                return mm256_insertf128_ps(a, b, imm8);
            }

            /// <summary>
            /// Copy a to dst, then insert 128 bits of integer data from b into dst at the location specified by imm8.
            /// </summary>
            /// <remarks>
            /// **** VINSERTF128 ymm1, ymm2, xmm3/v128, imm8
            /// Performs an insertion of 128-bits of packed floating-point values from the
            /// second source operand into an the destination at an 128-bit offset from
            /// imm8[0]. The remaining portions of the destination are written by the
            /// corresponding fields of the first source operand
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insertf128_si256(v256 a, v128 b, int imm8)
            {
                return mm256_insertf128_ps(a, b, imm8);
            }

            /// <summary>
            /// Load 256-bits (composed of 8 packed single-precision (32-bit) floating-point elements) from memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS ymm1, v256
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_load_ps(void* ptr)
            {
                return *(v256*)ptr;
            }

            /// <summary>
            /// Store 256-bits (composed of 8 packed single-precision (32-bit) floating-point elements) from a into memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS v256, ymm1
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="val">Value</param>
            [DebuggerStepThrough]
            public static void mm256_store_ps(void* ptr, v256 val)
            {
                *(v256*)ptr = val;
            }

            /// <summary>
            /// Load 256-bits (composed of 8 packed single-precision (32-bit) floating-point elements) from memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS ymm1, v256
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_load_pd(void* ptr)
            {
                return mm256_load_ps(ptr);
            }

            /// <summary>
            /// Store 256-bits (composed of 4 packed double-precision (64-bit) floating-point elements) from a into memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS v256, ymm1
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_store_pd(void* ptr, v256 a)
            {
                mm256_store_ps(ptr, a);
            }

            /// <summary>
            /// Load 256-bits (composed of 4 packed double-precision (64-bit) floating-point elements) from memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS ymm1, v256
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_loadu_pd(void* ptr)
            {
                return mm256_load_ps(ptr);
            }

            /// <summary>
            /// Store 256-bits (composed of 4 packed double-precision (64-bit) floating-point elements) from a into memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS v256, ymm1
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_storeu_pd(void* ptr, v256 a)
            {
                mm256_store_ps(ptr, a);
            }

            /// <summary>
            /// Load 256-bits (composed of 8 packed single-precision (32-bit) floating-point elements) from memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS ymm1, v256
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_loadu_ps(void* ptr)
            {
                return mm256_load_ps(ptr);
            }

            /// <summary>
            /// Store 256-bits (composed of 8 packed single-precision (32-bit) floating-point elements) from a into memory
            /// </summary>
            /// <remarks>
            /// **** VMOVUPS v256, ymm1
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_storeu_ps(void* ptr, v256 a)
            {
                mm256_store_ps(ptr, a);
            }

            /// <summary>
            /// Load 256-bits (composed of 8 packed 32-bit integers elements) from memory
            /// </summary>
            /// <remarks>
            /// **** VMOVDQU ymm1, v256
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_load_si256(void* ptr)
            {
                return mm256_load_ps(ptr);
            }

            /// <summary>
            /// Store 256-bits (composed of 8 packed 32-bit integer elements) from a into memory
            /// </summary>
            /// <remarks>
            /// **** VMOVDQU v256, ymm1
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="v">Vector</param>
            [DebuggerStepThrough]
            public static void mm256_store_si256(void* ptr, v256 v)
            {
                mm256_store_ps(ptr, v);
            }

            /// <summary>
            /// Load 256-bits (composed of 8 packed 32-bit integers elements) from memory
            /// </summary>
            /// <remarks>
            /// **** VMOVDQU ymm1, v256
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_loadu_si256(void* ptr)
            {
                return mm256_load_ps(ptr);
            }

            /// <summary>
            /// Store 256-bits (composed of 8 packed 32-bit integer elements) from a into memory
            /// </summary>
            /// <remarks>
            /// **** VMOVDQU v256, ymm1
            /// Burst only generates unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="v">Vector</param>
            [DebuggerStepThrough]
            public static void mm256_storeu_si256(void* ptr, v256 v)
            {
                mm256_store_ps(ptr, v);
            }

            /// <summary>
            /// Load two 128-bit values (composed of 4 packed single-precision
            /// (32-bit) floating-point elements) from memory, and combine them
            /// into a 256-bit value in dst. hiaddr and loaddr do not need to
            /// be aligned on any particular boundary.
            /// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hiaddr">High address pointer</param>
			/// <param name="loaddr">Low address pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_loadu2_m128(void* hiaddr, void* loaddr)
            {
                return mm256_set_m128(Sse.loadu_ps(hiaddr), Sse.loadu_ps(loaddr));
            }

            /// <summary>
            /// Load two 128-bit values (composed of 2 packed double-precision
            /// (64-bit) floating-point elements) from memory, and combine them
            /// into a 256-bit value in dst. hiaddr and loaddr do not need to
            /// be aligned on any particular boundary.
            /// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hiaddr">High address pointer</param>
			/// <param name="loaddr">Low address pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_loadu2_m128d(void* hiaddr, void* loaddr)
            {
                return mm256_loadu2_m128(hiaddr, loaddr);
            }

            /// <summary>
            /// Load two 128-bit values (composed of integer data) from memory,
            /// and combine them into a 256-bit value in dst. hiaddr and loaddr
            /// do not need to be aligned on any particular boundary.
            /// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hiaddr">High address pointer</param>
			/// <param name="loaddr">Low address pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_loadu2_m128i(void* hiaddr, void* loaddr)
            {
                return mm256_loadu2_m128(hiaddr, loaddr);
            }

            /// <summary>
            /// Set packed __m256 vector dst with the supplied values.
			/// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hi">High half of the vector</param>
			/// <param name="lo">Low half of the vector</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_m128(v128 hi, v128 lo)
            {
                return new v256(lo, hi);
            }

            /// <summary>
            /// Store the high and low 128-bit halves (each composed of 4
            /// packed single-precision (32-bit) floating-point elements) from
            /// a into memory two different 128-bit locations. hiaddr and
            /// loaddr do not need to be aligned on any particular boundary.
            /// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hiaddr">High address pointer</param>
			/// <param name="loaddr">Low address pointer</param>
			/// <param name="val">Value</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static void mm256_storeu2_m128(void* hiaddr, void* loaddr, v256 val)
            {
                Sse.storeu_ps(hiaddr, val.Hi128);
                Sse.storeu_ps(loaddr, val.Lo128);
            }

            /// <summary>
            /// Store the high and low 128-bit halves (each composed of 2
            /// packed double-precision (64-bit) floating-point elements) from
            /// a into memory two different 128-bit locations. hiaddr and
            /// loaddr do not need to be aligned on any particular boundary.
            /// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hiaddr">High address pointer</param>
			/// <param name="loaddr">Low address pointer</param>
			/// <param name="val">Value</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static void mm256_storeu2_m128d(void* hiaddr, void* loaddr, v256 val)
            {
                Sse.storeu_ps(hiaddr, val.Hi128);
                Sse.storeu_ps(loaddr, val.Lo128);
            }

            /// <summary>
            /// Store the high and low 128-bit halves (each composed of integer
            /// data) from a into memory two different 128-bit locations. hiaddr
            /// and loaddr do not need to be aligned on any particular boundary.
            /// </summary>
            /// <remarks>
            /// This is a composite function which can generate more than one instruction.
            /// </remarks>
			/// <param name="hiaddr">High address pointer</param>
			/// <param name="loaddr">Low address pointer</param>
			/// <param name="val">Value</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static void mm256_storeu2_m128i(void* hiaddr, void* loaddr, v256 val)
            {
                Sse.storeu_ps(hiaddr, val.Hi128);
                Sse.storeu_ps(loaddr, val.Lo128);
            }

            /// <summary>
            /// Load packed double-precision (64-bit) floating-point elements
            /// from memory into dst using mask (elements are zeroed out when
            /// the high bit of the corresponding element is not set).
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPD xmm1, xmm2, v128
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 maskload_pd(void* mem_addr, v128 mask)
            {
                ulong* addr = (ulong*)mem_addr;
                v128 result = default;
                if (mask.SLong0 < 0) result.ULong0 = addr[0];
                if (mask.SLong1 < 0) result.ULong1 = addr[1];
                return result;
            }

            /// <summary>
            /// Load packed double-precision (64-bit) floating-point elements
            /// from memory into dst using mask (elements are zeroed out when
            /// the high bit of the corresponding element is not set).
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPD ymm1, ymm2, v256
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_maskload_pd(void* mem_addr, v256 mask)
            {
                return new v256(maskload_pd(mem_addr, mask.Lo128), maskload_pd(((byte*)mem_addr) + 16, mask.Hi128));
            }

            /// <summary>
            /// Store packed double-precision (64-bit) floating-point elements from a into memory using mask.
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPD v128, xmm1, xmm2
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void maskstore_pd(void* mem_addr, v128 mask, v128 a)
            {
                ulong* addr = (ulong*)mem_addr;
                if (mask.SLong0 < 0) addr[0] = a.ULong0;
                if (mask.SLong1 < 0) addr[1] = a.ULong1;
            }

            /// <summary>
            /// Store packed double-precision (64-bit) floating-point elements from a into memory using mask.
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPD v256, ymm1, ymm2
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_maskstore_pd(void* mem_addr, v256 mask, v256 a)
            {
                maskstore_pd(mem_addr, mask.Lo128, a.Lo128);
                maskstore_pd(((byte*)mem_addr) + 16, mask.Hi128, a.Hi128);
            }

            /// <summary>
            /// Load packed single-precision (32-bit) floating-point elements
            /// from memory into dst using mask (elements are zeroed out when
            /// the high bit of the corresponding element is not set).
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPS xmm1, xmm2, v128
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 maskload_ps(void* mem_addr, v128 mask)
            {
                uint* addr = (uint*)mem_addr;
                v128 result = default;
                if (mask.SInt0 < 0) result.UInt0 = addr[0];
                if (mask.SInt1 < 0) result.UInt1 = addr[1];
                if (mask.SInt2 < 0) result.UInt2 = addr[2];
                if (mask.SInt3 < 0) result.UInt3 = addr[3];
                return result;
            }

            /// <summary>
            /// Load packed single-precision (32-bit) floating-point elements
            /// from memory into dst using mask (elements are zeroed out when
            /// the high bit of the corresponding element is not set).
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPS ymm1, ymm2, v256
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_maskload_ps(void* mem_addr, v256 mask)
            {
                return new v256(maskload_ps(mem_addr, mask.Lo128), maskload_ps(((byte*)mem_addr) + 16, mask.Hi128));
            }

            /// <summary>
            /// Store packed single-precision (32-bit) floating-point elements from a into memory using mask.
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPS v128, xmm1, xmm2
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void maskstore_ps(void* mem_addr, v128 mask, v128 a)
            {
                uint* addr = (uint*)mem_addr;
                if (mask.SInt0 < 0) addr[0] = a.UInt0;
                if (mask.SInt1 < 0) addr[1] = a.UInt1;
                if (mask.SInt2 < 0) addr[2] = a.UInt2;
                if (mask.SInt3 < 0) addr[3] = a.UInt3;
            }

            /// <summary>
            /// Store packed single-precision (32-bit) floating-point elements from a into memory using mask.
            /// </summary>
            /// <remarks>
            /// **** VMASKMOVPS v256, ymm1, ymm2
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_maskstore_ps(void* mem_addr, v256 mask, v256 a)
            {
                maskstore_ps(mem_addr, mask.Lo128, a.Lo128);
                maskstore_ps(((byte*)mem_addr) + 16, mask.Hi128, a.Hi128);
            }

            /*
             * Replicate Single-Precision Floating-Point Values
             * Duplicates odd-indexed single-precision floating-point values from the
             * source operand
             */
            /// <summary>
            /// Duplicate odd-indexed single-precision (32-bit) floating-point elements from a, and store the results in dst. 
            /// </summary>
            /// <remarks>
            /// **** VMOVSHDUP ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_movehdup_ps(v256 a)
            {
                return new v256(a.UInt1, a.UInt1, a.UInt3, a.UInt3, a.UInt5, a.UInt5, a.UInt7, a.UInt7);
            }

            /// <summary>
            /// Duplicate even-indexed single-precision (32-bit) floating-point elements from a, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VMOVSLDUP ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_moveldup_ps(v256 a)
            {
                return new v256(a.UInt0, a.UInt0, a.UInt2, a.UInt2, a.UInt4, a.UInt4, a.UInt6, a.UInt6);
            }


            /// <summary>
            /// Duplicate even-indexed double-precision (64-bit) floating-point elements from a, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VMOVDDUP ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_movedup_pd(v256 a)
            {
                return new v256(a.Double0, a.Double0, a.Double2, a.Double2);
            }

            /// <summary>
            /// Load 256-bits of integer data from unaligned memory into dst.
            /// This intrinsic may perform better than mm256_loadu_si256 when
            /// the data crosses a cache line boundary.
            /// </summary>
            /// <remarks>
            /// **** VLDDQU ymm1, v256
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_lddqu_si256(void* mem_addr)
            {
                return *(v256*)mem_addr;
            }

            /*
             * Store Packed Integers Using Non-Temporal Hint
             * **** VMOVNTDQ v256, ymm1
             * Moves the packed integers in the source operand to the destination using a
             * non-temporal hint to prevent caching of the data during the write to memory
             */
            /// <summary>
            /// Store 256-bits of integer data from a into memory using a
            /// non-temporal memory hint. mem_addr must be aligned on a 32-byte
            /// boundary or a general-protection exception may be generated.
            /// </summary>
            /// <remarks>
            /// **** VMOVNTDQ v256, ymm1
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_stream_si256(void* mem_addr, v256 a)
            {
                *(v256*)mem_addr = a;
            }


            /// <summary>
            /// Store 256-bits (composed of 4 packed double-precision (64-bit)
            /// floating-point elements) from a into memory using a
            /// non-temporal memory hint. mem_addr must be aligned on a 32-byte
            /// boundary or a general-protection exception may be generated.
            /// </summary>
            /// <remarks>
            /// **** VMOVNTPD v256, ymm1
            /// </remarks>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_stream_pd(void* mem_addr, v256 a)
            {
                *(v256*)mem_addr = a;
            }

            /// <summary>
            /// Store 256-bits (composed of 8 packed single-precision (32-bit)
            /// floating-point elements) from a into memory using a
            /// non-temporal memory hint. mem_addr must be aligned on a 32-byte
            /// boundary or a general-protection exception may be generated.
            /// </summary>
            /// <remarks>
            /// **** VMOVNTPS v256, ymm1
            /// </remarks>
            /// <param name="mem_addr">Memory address</param>
            /// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_stream_ps(void* mem_addr, v256 a)
            {
                *(v256*)mem_addr = a;
            }

            /// <summary>
            /// Compute the approximate reciprocal of packed single-precision
            /// (32-bit) floating-point elements in a, and store the results in
            /// dst. The maximum relative error for this approximation is less
            /// than 1.5*2^-12.
            /// </summary>
            /// <remarks>
            /// **** VRCPPS ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_rcp_ps(v256 a)
            {
                return new v256(Sse.rcp_ps(a.Lo128), Sse.rcp_ps(a.Hi128));
            }

            /// <summary>
            /// Compute the approximate reciprocal square root of packed
            /// single-precision (32-bit) floating-point elements in a, and
            /// store the results in dst. The maximum relative error for this
            /// approximation is less than 1.5*2^-12.
            /// </summary>
            /// <remarks>
            /// **** VRSQRTPS ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_rsqrt_ps(v256 a)
            {
                return new v256(Sse.rsqrt_ps(a.Lo128), Sse.rsqrt_ps(a.Hi128));
            }

            /// <summary>
            /// Compute the square root of packed double-precision (64-bit)
            /// floating-point elements in a, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VSQRTPD ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sqrt_pd(v256 a)
            {
                return new v256(Sse2.sqrt_pd(a.Lo128), Sse2.sqrt_pd(a.Hi128));
            }

            /// <summary>
            /// Compute the square root of packed single-precision (32-bit)
            /// floating-point elements in a, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VSQRTPS ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sqrt_ps(v256 a)
            {
                return new v256(Sse.sqrt_ps(a.Lo128), Sse.sqrt_ps(a.Hi128));
            }

            /// <summary>
            /// Round the packed double-precision (64-bit) floating-point
            /// elements in a using the rounding parameter, and store the
            /// results as packed double-precision floating-point elements in
            /// dst.
            /// </summary>
            /// <remarks>
            ///**** VROUNDPD ymm1,ymm2/v256,imm8
            /// Rounding is done according to the rounding parameter, which can be one of:
            /// (_MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC) // round to nearest, and suppress exceptions
            /// (_MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC)     // round down, and suppress exceptions
            /// (_MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC)     // round up, and suppress exceptions
            /// (_MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC)        // truncate, and suppress exceptions
            /// _MM_FROUND_CUR_DIRECTION                       // use MXCSR.RC; see _MM_SET_ROUNDING_MODE
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_round_pd(v256 a, int rounding)
            {
                return new v256(Sse4_1.round_pd(a.Lo128, rounding), Sse4_1.round_pd(a.Hi128, rounding));
            }


            /// <summary>
            /// Round the packed double-precision (64-bit) floating-point
            /// elements in a up to an integer value, and store the results as
            /// packed double-precision floating-point elements in dst.
            /// </summary>
			/// <param name="val">Value</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_ceil_pd(v256 val)
            {
                return mm256_round_pd(val, (int)RoundingMode.FROUND_CEIL);
            }

            /// <summary>
            /// Round the packed double-precision (64-bit) floating-point
            /// elements in a down to an integer value, and store the results
            /// as packed double-precision floating-point elements in dst.
            /// </summary>
			/// <param name="val">Value</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_floor_pd(v256 val)
            {
                return mm256_round_pd(val, (int)RoundingMode.FROUND_FLOOR);
            }

            /// <summary>
            /// Round the packed single-precision (32-bit) floating-point
            /// elements in a using the rounding parameter, and store the
            /// results as packed single-precision floating-point elements in
            /// dst.
            /// </summary>
            /// <remarks>
            /// **** VROUNDPS ymm1,ymm2/v256,imm8
            /// Round the four single-precision floating-point values values in the source
            /// operand by the rounding mode specified in the immediate operand and place
            /// the result in the destination. The rounding process rounds the input to an
            /// integral value and returns the result as a double-precision floating-point
            /// value. The Precision Floating Point Exception is signaled according to the
            /// immediate operand. If any source operand is an SNaN then it will be
            /// converted to a QNaN.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_round_ps(v256 a, int rounding)
            {
                return new v256(Sse4_1.round_ps(a.Lo128, rounding), Sse4_1.round_ps(a.Hi128, rounding));
            }

            /// <summary>
            /// Round the packed single-precision (32-bit) floating-point
            /// elements in a up to an integer value, and store the results as
            /// packed single-precision floating-point elements in dst.
            /// </summary>
			/// <param name="val">Value</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_ceil_ps(v256 val)
            {
                return mm256_round_ps(val, (int)RoundingMode.FROUND_CEIL);
            }

            /// <summary>
            /// Round the packed single-precision (32-bit) floating-point
            /// elements in a down to an integer value, and store the results
            /// as packed single-precision floating-point elements in dst.
            /// </summary>
			/// <param name="val">Value</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_floor_ps(v256 val)
            {
                return mm256_round_ps(val, (int)RoundingMode.FROUND_FLOOR);
            }

            /// <summary>
            /// Unpack and interleave double-precision (64-bit) floating-point
            /// elements from the high half of each 128-bit lane in a and b,
            /// and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VUNPCKHPD ymm1,ymm2,ymm3/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpackhi_pd(v256 a, v256 b)
            {
                return new v256(Sse2.unpackhi_pd(a.Lo128, b.Lo128), Sse2.unpackhi_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave double-precision (64-bit) floating-point
            /// elements from the low half of each 128-bit lane in a and b, and
            /// store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VUNPCKLPD ymm1,ymm2,ymm3/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpacklo_pd(v256 a, v256 b)
            {
                return new v256(Sse2.unpacklo_pd(a.Lo128, b.Lo128), Sse2.unpacklo_pd(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave single-precision(32-bit) floating-point
            /// elements from the high half of each 128-bit lane in a and b,
            /// and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VUNPCKHPS ymm1,ymm2,ymm3/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_unpackhi_ps(v256 a, v256 b)
            {
                return new v256(Sse.unpackhi_ps(a.Lo128, b.Lo128), Sse.unpackhi_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave single-precision (32-bit) floating-point
            /// elements from the low half of each 128-bit lane in a and b, and
            /// store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** VUNPCKLPS ymm1,ymm2,ymm3/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_unpacklo_ps(v256 a, v256 b)
            {
                return new v256(Sse.unpacklo_ps(a.Lo128, b.Lo128), Sse.unpacklo_ps(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing integer data)
            /// in a and b, and set ZF to 1 if the result is zero, otherwise
            /// set ZF to 0. Compute the bitwise NOT of a and then AND with b,
            /// and set CF to 1 if the result is zero, otherwise set CF to 0.
            /// Return the ZF value.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>ZF value</returns>
            [DebuggerStepThrough]
            public static int mm256_testz_si256(v256 a, v256 b)
            {
                return Sse4_1.testz_si128(a.Lo128, b.Lo128) & Sse4_1.testz_si128(a.Hi128, b.Hi128);
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing integer data)
            /// in a and b, and set ZF to 1 if the result is zero, otherwise
            /// set ZF to 0. Compute the bitwise NOT of a and then AND with b,
            /// and set CF to 1 if the result is zero, otherwise set CF to 0.
            /// Return the CF value.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>CF value</returns>
            [DebuggerStepThrough]
            public static int mm256_testc_si256(v256 a, v256 b)
            {
                return Sse4_1.testc_si128(a.Lo128, b.Lo128) & Sse4_1.testc_si128(a.Hi128, b.Hi128);
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing integer data)
            /// in a and b, and set ZF to 1 if the result is zero, otherwise
            /// set ZF to 0. Compute the bitwise NOT of a and then AND with b,
            /// and set CF to 1 if the result is zero, otherwise set CF to 0.
            /// Return 1 if both the ZF and CF values are zero, otherwise
            /// return 0.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_testnzc_si256(v256 a, v256 b)
            {
                int zf = mm256_testz_si256(a, b);
                int cf = mm256_testc_si256(a, b);
                return 1 - (zf | cf);
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing
            /// double-precision (64-bit) floating-point elements) in a and b,
            /// producing an intermediate 256-bit value, and set ZF to 1 if the
            /// sign bit of each 64-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 64-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the ZF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPD ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>ZF value</returns>
            [DebuggerStepThrough]
            public static int mm256_testz_pd(v256 a, v256 b)
            {
                ulong* aptr = &a.ULong0;
                ulong* bptr = &b.ULong0;
                for (int i = 0; i < 4; ++i)
                {
                    if (((aptr[i] & bptr[i]) & 0x8000_0000_0000_0000) != 0)
                        return 0;
                }
                return 1;
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing
            /// double-precision (64-bit) floating-point elements) in a and b,
            /// producing an intermediate 256-bit value, and set ZF to 1 if the
            /// sign bit of each 64-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 64-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the CF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPD ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>CF value</returns>
            [DebuggerStepThrough]
            public static int mm256_testc_pd(v256 a, v256 b)
            {
                ulong* aptr = &a.ULong0;
                ulong* bptr = &b.ULong0;
                for (int i = 0; i < 4; ++i)
                {
                    if ((((~aptr[i]) & bptr[i]) & 0x8000_0000_0000_0000) != 0)
                        return 0;
                }
                return 1;
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing
            /// double-precision (64-bit) floating-point elements) in a and b,
            /// producing an intermediate 256-bit value, and set ZF to 1 if the
            /// sign bit of each 64-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 64-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return 1 if both the ZF
            /// and CF values are zero, otherwise return 0.
            /// </summary>
            /// <remarks>
            /// **** VTESTPD ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_testnzc_pd(v256 a, v256 b)
            {
                return 1 - (mm256_testz_pd(a, b) | mm256_testc_pd(a, b));
            }

            /// <summary>
            /// Compute the bitwise AND of 128 bits (representing
            /// double-precision (64-bit) floating-point elements) in a and b,
            /// producing an intermediate 128-bit value, and set ZF to 1 if the
            /// sign bit of each 64-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 64-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the ZF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPD xmm1, xmm2/v128
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>ZF value</returns>
            [DebuggerStepThrough]
            public static int testz_pd(v128 a, v128 b)
            {
                ulong* aptr = &a.ULong0;
                ulong* bptr = &b.ULong0;
                for (int i = 0; i < 2; ++i)
                {
                    if (((aptr[i] & bptr[i]) & 0x8000_0000_0000_0000) != 0)
                        return 0;
                }
                return 1;
            }

            /// <summary>
            /// Compute the bitwise AND of 128 bits (representing
            /// double-precision (64-bit) floating-point elements) in a and b,
            /// producing an intermediate 128-bit value, and set ZF to 1 if the
            /// sign bit of each 64-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 64-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the CF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPD xmm1, xmm2/v128
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>CF value</returns>
            [DebuggerStepThrough]
            public static int testc_pd(v128 a, v128 b)
            {
                ulong* aptr = &a.ULong0;
                ulong* bptr = &b.ULong0;
                for (int i = 0; i < 2; ++i)
                {
                    if ((((~aptr[i]) & bptr[i]) & 0x8000_0000_0000_0000) != 0)
                        return 0;
                }
                return 1;
            }

            /// <summary>
            /// Compute the bitwise AND of 128 bits (representing
            /// double-precision (64-bit) floating-point elements) in a and b,
            /// producing an intermediate 128-bit value, and set ZF to 1 if the
            /// sign bit of each 64-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 64-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return 1 if both the ZF
            /// and CF values are zero, otherwise return 0.
            /// </summary>
            /// <remarks>
            /// **** VTESTPD xmm1, xmm2/v128
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int testnzc_pd(v128 a, v128 b)
            {
                return 1 - (testz_pd(a, b) | testc_pd(a, b));
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing
            /// single-precision (32-bit) floating-point elements) in a and b,
            /// producing an intermediate 256-bit value, and set ZF to 1 if the
            /// sign bit of each 32-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 32-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the ZF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPS ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>ZF value</returns>
            [DebuggerStepThrough]
            public static int mm256_testz_ps(v256 a, v256 b)
            {
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (int i = 0; i < 8; ++i)
                {
                    if (((aptr[i] & bptr[i]) & 0x8000_0000) != 0)
                        return 0;
                }
                return 1;

            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing
            /// single-precision (32-bit) floating-point elements) in a and b,
            /// producing an intermediate 256-bit value, and set ZF to 1 if the
            /// sign bit of each 32-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 32-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the CF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPS ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>CF value</returns>
            [DebuggerStepThrough]
            public static int mm256_testc_ps(v256 a, v256 b)
            {
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (int i = 0; i < 8; ++i)
                {
                    if ((((~aptr[i]) & bptr[i]) & 0x8000_0000) != 0)
                        return 0;
                }
                return 1;
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing
            /// single-precision (32-bit) floating-point elements) in a and b,
            /// producing an intermediate 256-bit value, and set ZF to 1 if the
            /// sign bit of each 32-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 32-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return 1 if both the ZF
            /// and CF values are zero, otherwise return 0.
            /// </summary>
            /// <remarks>
            /// **** VTESTPS ymm1, ymm2/v256
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_testnzc_ps(v256 a, v256 b)
            {
                return 1 - (mm256_testz_ps(a, b) | mm256_testc_ps(a, b));
            }

            /// <summary>
            /// Compute the bitwise AND of 128 bits (representing
            /// single-precision (32-bit) floating-point elements) in a and b,
            /// producing an intermediate 128-bit value, and set ZF to 1 if the
            /// sign bit of each 32-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 32-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the ZF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPS xmm1, xmm2/v128
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>ZF value</returns>
            [DebuggerStepThrough]
            public static int testz_ps(v128 a, v128 b)
            {
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (int i = 0; i < 4; ++i)
                {
                    if (((aptr[i] & bptr[i]) & 0x8000_0000) != 0)
                        return 0;
                }
                return 1;

            }

            /// <summary>
            /// Compute the bitwise AND of 128 bits (representing
            /// single-precision (32-bit) floating-point elements) in a and b,
            /// producing an intermediate 128-bit value, and set ZF to 1 if the
            /// sign bit of each 32-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 32-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return the CF value.
            /// </summary>
            /// <remarks>
            /// **** VTESTPS xmm1, xmm2/v128
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>CF value</returns>
            [DebuggerStepThrough]
            public static int testc_ps(v128 a, v128 b)
            {
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (int i = 0; i < 4; ++i)
                {
                    if ((((~aptr[i]) & bptr[i]) & 0x8000_0000) != 0)
                        return 0;
                }
                return 1;
            }

            /// <summary>
            /// Compute the bitwise AND of 128 bits (representing
            /// single-precision (32-bit) floating-point elements) in a and b,
            /// producing an intermediate 128-bit value, and set ZF to 1 if the
            /// sign bit of each 32-bit element in the intermediate value is
            /// zero, otherwise set ZF to 0. Compute the bitwise NOT of a and
            /// then AND with b, producing an intermediate value, and set CF to
            /// 1 if the sign bit of each 32-bit element in the intermediate
            /// value is zero, otherwise set CF to 0. Return 1 if both the ZF
            /// and CF values are zero, otherwise return 0.
            /// </summary>
            /// <remarks>
            /// **** VTESTPS xmm1, xmm2/v128
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int testnzc_ps(v128 a, v128 b)
            {
                return 1 - (testz_ps(a, b) | testc_ps(a, b));
            }

            /// <summary>
            /// Set each bit of mask dst based on the most significant bit of the corresponding packed double-precision (64-bit) floating-point element in a.
            /// </summary>
            /// <remarks>
            /// **** VMOVMSKPD r32, ymm2
            /// Extracts the sign bits from the packed double-precision floating-point
            /// values in the source operand, formats them into a 4-bit mask, and stores
            /// the mask in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_movemask_pd(v256 a)
            {
                return Sse2.movemask_pd(a.Lo128) | (Sse2.movemask_pd(a.Hi128) << 2);
            }


            /// <summary>
            /// Set each bit of mask dst based on the most significant bit of the corresponding packed single-precision (32-bit) floating-point element in a.
            /// </summary>
            /// <remarks>
            /// **** VMOVMSKPS r32, ymm2
            /// Extracts the sign bits from the packed single-precision floating-point
            /// values in the source operand, formats them into a 8-bit mask, and stores
            /// the mask in the destination
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_movemask_ps(v256 a)
            {
                return Sse.movemask_ps(a.Lo128) | (Sse.movemask_ps(a.Hi128) << 4);
            }

            // Normal IR is fine for this

            /// <summary>
            /// Return Vector with all elements set to zero.
            /// </summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setzero_pd() { return default; }

            /// <summary>
            /// Return Vector with all elements set to zero.
            /// </summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setzero_ps() { return default; }

            /// <summary>
            /// Return Vector with all elements set to zero.
            /// </summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setzero_si256() { return default; }

            /// <summary>
            /// Set packed double-precision (64-bit) floating-point elements in dst with the supplied values.
            /// </summary>
			/// <param name="d">Element d</param>
			/// <param name="c">Element c</param>
			/// <param name="b">Element b</param>
			/// <param name="a">Element a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_pd(double d, double c, double b, double a)
            {
                return new v256(a, b, c, d);
            }

            /// <summary>
            /// Set packed single-precision (32-bit) floating-point elements in dst with the supplied values.
            /// </summary>
			/// <param name="e7">Element 7</param>
			/// <param name="e6">Element 6</param>
			/// <param name="e5">Element 5</param>
			/// <param name="e4">Element 4</param>
			/// <param name="e3">Element 3</param>
			/// <param name="e2">Element 2</param>
			/// <param name="e1">Element 1</param>
			/// <param name="e0">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_ps(float e7, float e6, float e5, float e4, float e3, float e2, float e1, float e0)
            {
                return new v256(e0, e1, e2, e3, e4, e5, e6, e7);
            }

            /// <summary>
            /// Set packed byte elements in dst with the supplied values.
            /// </summary>
			/// <param name="e31_">Element 31</param>
			/// <param name="e30_">Element 30</param>
			/// <param name="e29_">Element 29</param>
			/// <param name="e28_">Element 28</param>
			/// <param name="e27_">Element 27</param>
			/// <param name="e26_">Element 26</param>
			/// <param name="e25_">Element 25</param>
			/// <param name="e24_">Element 24</param>
			/// <param name="e23_">Element 23</param>
			/// <param name="e22_">Element 22</param>
			/// <param name="e21_">Element 21</param>
			/// <param name="e20_">Element 20</param>
			/// <param name="e19_">Element 19</param>
			/// <param name="e18_">Element 18</param>
			/// <param name="e17_">Element 17</param>
			/// <param name="e16_">Element 16</param>
			/// <param name="e15_">Element 15</param>
			/// <param name="e14_">Element 14</param>
			/// <param name="e13_">Element 13</param>
			/// <param name="e12_">Element 12</param>
			/// <param name="e11_">Element 11</param>
			/// <param name="e10_">Element 10</param>
			/// <param name="e9_">Element 9</param>
			/// <param name="e8_">Element 8</param>
			/// <param name="e7_">Element 7</param>
			/// <param name="e6_">Element 6</param>
			/// <param name="e5_">Element 5</param>
			/// <param name="e4_">Element 4</param>
			/// <param name="e3_">Element 3</param>
			/// <param name="e2_">Element 2</param>
			/// <param name="e1_">Element 1</param>
			/// <param name="e0_">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_epi8(
                byte e31_, byte e30_, byte e29_, byte e28_, byte e27_, byte e26_, byte e25_, byte e24_, byte e23_, byte e22_, byte e21_, byte e20_, byte e19_, byte e18_, byte e17_, byte e16_,
                byte e15_, byte e14_, byte e13_, byte e12_, byte e11_, byte e10_, byte e9_, byte e8_, byte e7_, byte e6_, byte e5_, byte e4_, byte e3_, byte e2_, byte e1_, byte e0_)
            {
                return new v256(
                    e0_, e1_, e2_, e3_, e4_, e5_, e6_, e7_,
                    e8_, e9_, e10_, e11_, e12_, e13_, e14_, e15_,
                    e16_, e17_, e18_, e19_, e20_, e21_, e22_, e23_,
                    e24_, e25_, e26_, e27_, e28_, e29_, e30_, e31_);
            }

            /// <summary>
            /// Set packed short elements in dst with the supplied values.
            /// </summary>
			/// <param name="e15_">Element 15</param>
			/// <param name="e14_">Element 14</param>
			/// <param name="e13_">Element 13</param>
			/// <param name="e12_">Element 12</param>
			/// <param name="e11_">Element 11</param>
			/// <param name="e10_">Element 10</param>
			/// <param name="e9_">Element 9</param>
			/// <param name="e8_">Element 8</param>
			/// <param name="e7_">Element 7</param>
			/// <param name="e6_">Element 6</param>
			/// <param name="e5_">Element 5</param>
			/// <param name="e4_">Element 4</param>
			/// <param name="e3_">Element 3</param>
			/// <param name="e2_">Element 2</param>
			/// <param name="e1_">Element 1</param>
			/// <param name="e0_">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_epi16(short e15_, short e14_, short e13_, short e12_, short e11_, short e10_, short e9_, short e8_, short e7_, short e6_, short e5_, short e4_, short e3_, short e2_, short e1_, short e0_)
            {
                return new v256(
                    e0_, e1_, e2_, e3_, e4_, e5_, e6_, e7_,
                    e8_, e9_, e10_, e11_, e12_, e13_, e14_, e15_);
            }

            /// <summary>
            /// Set packed int elements in dst with the supplied values.
            /// </summary>
			/// <param name="e7">Element 7</param>
			/// <param name="e6">Element 6</param>
			/// <param name="e5">Element 5</param>
			/// <param name="e4">Element 4</param>
			/// <param name="e3">Element 3</param>
			/// <param name="e2">Element 2</param>
			/// <param name="e1">Element 1</param>
			/// <param name="e0">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_epi32(int e7, int e6, int e5, int e4, int e3, int e2, int e1, int e0)
            {
                return new v256(e0, e1, e2, e3, e4, e5, e6, e7);
            }

            /// <summary>
            /// Set packed 64-bit integers in dst with the supplied values.
            /// </summary>
			/// <param name="e3">Element 3</param>
			/// <param name="e2">Element 2</param>
			/// <param name="e1">Element 1</param>
			/// <param name="e0">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_epi64x(long e3, long e2, long e1, long e0)
            {
                return new v256(e0, e1, e2, e3);
            }

            /// <summary>
            /// Set packed v256 vector with the supplied values.
            /// </summary>
			/// <param name="hi">High half of the vector</param>
			/// <param name="lo">Low half of the vector</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_m128d(v128 hi, v128 lo)
            {
                return new v256(lo, hi);
            }

            /// <summary>
            /// Set packed v256 vector with the supplied values.
            /// </summary>
			/// <param name="hi">High half of the vector</param>
			/// <param name="lo">Low half of the vector</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set_m128i(v128 hi, v128 lo)
            {
                return new v256(lo, hi);
            }

            /// <summary>
            /// Set packed double-precision (64-bit) floating-point elements in dst with the supplied values in reverse order.
            /// </summary>
			/// <param name="d">Element d</param>
			/// <param name="c">Element c</param>
			/// <param name="b">Element b</param>
			/// <param name="a">Element a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_pd(double d, double c, double b, double a)
            {
                return new v256(d, c, b, a);
            }

            /// <summary>
            /// Set packed single-precision (32-bit) floating-point elements in dst with the supplied values in reverse order.
            /// </summary>
			/// <param name="e7">Element 7</param>
			/// <param name="e6">Element 6</param>
			/// <param name="e5">Element 5</param>
			/// <param name="e4">Element 4</param>
			/// <param name="e3">Element 3</param>
			/// <param name="e2">Element 2</param>
			/// <param name="e1">Element 1</param>
			/// <param name="e0">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_ps(float e7, float e6, float e5, float e4, float e3, float e2, float e1, float e0)
            {
                return new v256(e7, e6, e5, e4, e3, e2, e1, e0);
            }

            /// <summary>
            /// Set packed byte elements in dst with the supplied values in reverse order.
            /// </summary>
			/// <param name="e31_">Element 31</param>
			/// <param name="e30_">Element 30</param>
			/// <param name="e29_">Element 29</param>
			/// <param name="e28_">Element 28</param>
			/// <param name="e27_">Element 27</param>
			/// <param name="e26_">Element 26</param>
			/// <param name="e25_">Element 25</param>
			/// <param name="e24_">Element 24</param>
			/// <param name="e23_">Element 23</param>
			/// <param name="e22_">Element 22</param>
			/// <param name="e21_">Element 21</param>
			/// <param name="e20_">Element 20</param>
			/// <param name="e19_">Element 19</param>
			/// <param name="e18_">Element 18</param>
			/// <param name="e17_">Element 17</param>
			/// <param name="e16_">Element 16</param>
			/// <param name="e15_">Element 15</param>
			/// <param name="e14_">Element 14</param>
			/// <param name="e13_">Element 13</param>
			/// <param name="e12_">Element 12</param>
			/// <param name="e11_">Element 11</param>
			/// <param name="e10_">Element 10</param>
			/// <param name="e9_">Element 9</param>
			/// <param name="e8_">Element 8</param>
			/// <param name="e7_">Element 7</param>
			/// <param name="e6_">Element 6</param>
			/// <param name="e5_">Element 5</param>
			/// <param name="e4_">Element 4</param>
			/// <param name="e3_">Element 3</param>
			/// <param name="e2_">Element 2</param>
			/// <param name="e1_">Element 1</param>
			/// <param name="e0_">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_epi8(
                byte e31_, byte e30_, byte e29_, byte e28_, byte e27_, byte e26_, byte e25_, byte e24_, byte e23_, byte e22_, byte e21_, byte e20_, byte e19_, byte e18_, byte e17_, byte e16_,
                byte e15_, byte e14_, byte e13_, byte e12_, byte e11_, byte e10_, byte e9_, byte e8_, byte e7_, byte e6_, byte e5_, byte e4_, byte e3_, byte e2_, byte e1_, byte e0_)
            {
                return new v256(
                    e31_, e30_, e29_, e28_, e27_, e26_, e25_, e24_,
                    e23_, e22_, e21_, e20_, e19_, e18_, e17_, e16_,
                    e15_, e14_, e13_, e12_, e11_, e10_, e9_, e8_,
                    e7_, e6_, e5_, e4_, e3_, e2_, e1_, e0_);
            }

            /// <summary>
            /// Set packed short elements in dst with the supplied values in reverse order.
            /// </summary>
			/// <param name="e15_">Element 15</param>
			/// <param name="e14_">Element 14</param>
			/// <param name="e13_">Element 13</param>
			/// <param name="e12_">Element 12</param>
			/// <param name="e11_">Element 11</param>
			/// <param name="e10_">Element 10</param>
			/// <param name="e9_">Element 9</param>
			/// <param name="e8_">Element 8</param>
			/// <param name="e7_">Element 7</param>
			/// <param name="e6_">Element 6</param>
			/// <param name="e5_">Element 5</param>
			/// <param name="e4_">Element 4</param>
			/// <param name="e3_">Element 3</param>
			/// <param name="e2_">Element 2</param>
			/// <param name="e1_">Element 1</param>
			/// <param name="e0_">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_epi16(short e15_, short e14_, short e13_, short e12_, short e11_, short e10_, short e9_, short e8_, short e7_, short e6_, short e5_, short e4_, short e3_, short e2_, short e1_, short e0_)
            {
                return new v256(
                    e15_, e14_, e13_, e12_, e11_, e10_, e9_, e8_,
                    e7_, e6_, e5_, e4_, e3_, e2_, e1_, e0_);
            }

            /// <summary>
            /// Set packed int elements in dst with the supplied values in reverse order.
            /// </summary>
			/// <param name="e7">Element 7</param>
			/// <param name="e6">Element 6</param>
			/// <param name="e5">Element 5</param>
			/// <param name="e4">Element 4</param>
			/// <param name="e3">Element 3</param>
			/// <param name="e2">Element 2</param>
			/// <param name="e1">Element 1</param>
			/// <param name="e0">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_epi32(int e7, int e6, int e5, int e4, int e3, int e2, int e1, int e0)
            {
                return new v256(e7, e6, e5, e4, e3, e2, e1, e0);
            }

            /// <summary>
            /// Set packed 64-bit integers in dst with the supplied values in reverse order.
            /// </summary>
			/// <param name="e3">Element 3</param>
			/// <param name="e2">Element 2</param>
			/// <param name="e1">Element 1</param>
			/// <param name="e0">Element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_epi64x(long e3, long e2, long e1, long e0)
            {
                return new v256(e3, e2, e1, e0);
            }

            /// <summary>
            /// Set packed v256 vector with the supplied values in reverse order.
            /// </summary>
			/// <param name="hi">High half of the vector</param>
			/// <param name="lo">Low half of the vector</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_m128(v128 hi, v128 lo)
            {
                return new v256(hi, lo);
            }

            /// <summary>
            /// Set packed v256 vector with the supplied values in reverse order.
            /// </summary>
			/// <param name="hi">High half of the vector</param>
			/// <param name="lo">Low half of the vector</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_m128d(v128 hi, v128 lo)
            {
                return new v256(hi, lo);
            }

            /// <summary>
            /// Set packed v256 vector with the supplied values in reverse order.
            /// </summary>
			/// <param name="hi">High half of the vector</param>
			/// <param name="lo">Low half of the vector</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_setr_m128i(v128 hi, v128 lo)
            {
                return new v256(hi, lo);
            }

            /// <summary>
            /// Broadcast double-precision (64-bit) floating-point value a to all elements of dst.
            /// </summary>
			/// <param name="a">Value</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set1_pd(double a)
            {
                return new v256(a);
            }

            /// <summary>
            /// Broadcast single-precision (32-bit) floating-point value a to all elements of dst.
            /// </summary>
			/// <param name="a">Value</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set1_ps(float a)
            {
                return new v256(a);
            }

            /// <summary>
            /// Broadcast 8-bit integer a to all elements of dst. This intrinsic may generate the vpbroadcastb instruction.
            /// </summary>
			/// <param name="a">8-bit integer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set1_epi8(byte a)
            {
                return new v256(a);
            }

            /// <summary>
            /// Broadcast 16-bit integer a to all all elements of dst. This intrinsic may generate the vpbroadcastw instruction.
            /// </summary>
			/// <param name="a">16-bit integer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set1_epi16(short a)
            {
                return new v256(a);
            }

            /// <summary>
            /// Broadcast 32-bit integer a to all elements of dst. This intrinsic may generate the vpbroadcastd instruction.
            /// </summary>
			/// <param name="a">32-bit integer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set1_epi32(int a)
            {
                return new v256(a);
            }

            /// <summary>
            /// Broadcast 64-bit integer a to all elements of dst. This intrinsic may generate the vpbroadcastq instruction.
            /// </summary>
            /// <param name="a">64-bit integer</param>
            /// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_set1_epi64x(long a)
            {
                return new v256(a);
            }

            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castpd_ps(v256 a) { return a; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castps_pd(v256 a) { return a; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castps_si256(v256 a) { return a; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castpd_si256(v256 a) { return a; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castsi256_ps(v256 a) { return a; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castsi256_pd(v256 a) { return a; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_castps256_ps128(v256 a) { return a.Lo128; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_castpd256_pd128(v256 a) { return a.Lo128; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_castsi256_si128(v256 a) { return a.Lo128; }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castps128_ps256(v128 a) { return new v256(a, Sse.setzero_ps()); }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castpd128_pd256(v128 a) { return new v256(a, Sse.setzero_ps()); }
            /// <summary>For compatibility with C++ code only. This is a no-op in Burst.</summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_castsi128_si256(v128 a) { return new v256(a, Sse.setzero_ps()); }

            /// <summary>Return a 128-bit vector with undefined contents.</summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 undefined_ps()
            {
                return default;
            }

            /// <summary>Return a 128-bit vector with undefined contents.</summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v128 undefined_pd()
            {
                return undefined_ps();
            }

            /// <summary>Return a 128-bit vector with undefined contents.</summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v128 undefined_si128()
            {
                return undefined_ps();
            }

            /// <summary>Return a 256-bit vector with undefined contents.</summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_undefined_ps()
            {
                return default;
            }

            /// <summary>Return a 256-bit vector with undefined contents.</summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_undefined_pd()
            {
                return mm256_undefined_ps();
            }

            /// <summary>Return a 256-bit vector with undefined contents.</summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_undefined_si256()
            {
                return mm256_undefined_ps();
            }

            // Zero-extended cast functions

            /// <summary>
            /// Casts vector of type v128 to type v256; the upper 128 bits of the result
            /// are zeroed. This intrinsic is only used for compilation and does not
            /// generate any instructions, thus it has zero latency.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_zextps128_ps256(v128 a) { return new v256(a, Sse.setzero_ps()); }

            /// <summary>
            /// Casts vector of type v128 to type v256; the upper 128 bits of the result
            /// are zeroed. This intrinsic is only used for compilation and does not
            /// generate any instructions, thus it has zero latency.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_zextpd128_pd256(v128 a) { return mm256_zextps128_ps256(a); }

            /// <summary>
            /// Casts vector of type v128 to type v256; the upper 128 bits of the result
            /// are zeroed. This intrinsic is only used for compilation and does not
            /// generate any instructions, thus it has zero latency.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.AVX)]
            public static v256 mm256_zextsi128_si256(v128 a) { return mm256_zextps128_ps256(a); }


            /// <summary>
            /// Copy a to dst, and insert the 8-bit integer i into dst at the location specified by index (which must be a constant).
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="i">8-bit integer i</param>
			/// <param name="index">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insert_epi8(v256 a, int i, int index)
            {
                v256 dst = a;
                byte* target = &dst.Byte0;
                target[index & 31] = (byte)i;
                return dst;
            }

            /// <summary>
            /// Copy a to dst, and insert the 16-bit integer i into dst at the location specified by index (which must be a constant).
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="i">16-bit integer i</param>
			/// <param name="index">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insert_epi16(v256 a, int i, int index)
            {
                v256 dst = a;
                short* target = &dst.SShort0;
                target[index & 15] = (short)i;
                return dst;
            }

            /// <summary>
            /// Copy a to dst, and insert the 32-bit integer i into dst at the location specified by index (which must be a constant).
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="i">32-bit integer i</param>
			/// <param name="index">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insert_epi32(v256 a, int i, int index)
            {
                v256 dst = a;
                int* target = &dst.SInt0;
                target[index & 7] = i;
                return dst;
            }

            /// <summary>
            /// Copy a to dst, and insert the 64-bit integer i into dst at the location specified by index (which must be a constant).
            /// </summary>
            /// <remarks>
            /// This intrinsic requires a 64-bit processor.
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="i">64-bit integer i</param>
			/// <param name="index">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_insert_epi64(v256 a, long i, int index)
            {
                v256 dst = a;
                long* target = &dst.SLong0;
                target[index & 3] = i;
                return dst;
            }

            /// <summary>
            /// Extract a 32-bit integer from a, selected with index (which must be a constant), and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="index">Index</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static int mm256_extract_epi32(v256 a, int index)
            {
                return (&a.SInt0)[index & 7];
            }

            /// <summary>
            /// Extract a 64-bit integer from a, selected with index (which must be a constant), and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="index">Index</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static long mm256_extract_epi64(v256 a, int index)
            {
                return (&a.SLong0)[index & 3];
            }
        }
    }
}
