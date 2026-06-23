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
        /// AVX2 intrinsics
        /// </summary>
        public static class Avx2
        {
            /// <summary>
            /// Evaluates to true at compile time if AVX2 intrinsics are supported.
            /// </summary>
            public static bool IsAvx2Supported { get { return false; } }

            /// <summary>
            /// Create mask from the most significant bit of each 8-bit element in a, and store the result in dst. 
            /// </summary>
            /// <remarks>
            /// **** vpmovmskb r32, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_movemask_epi8(v256 a)
            {
                uint result = 0;
                byte* ptr = &a.Byte0;
                uint bit = 1;
                for (int i = 0; i < 32; ++i, bit <<= 1)
                {
                    result |= (((uint)ptr[i]) >> 7) * bit;
                }
                return (int)result;
            }

            /// <summary>
            /// Extract an 8-bit integer from a, selected with index (which must be constant), and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="index">Index in the vector</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_extract_epi8(v256 a, int index)
            {
                return (&a.Byte0)[index & 31];
            }

            /// <summary>
            /// Extract a 16-bit integer from a, selected with index (which must be constant), and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="index">Index in the vector</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_extract_epi16(v256 a, int index)
            {
                return (&a.UShort0)[index & 15];
            }

            /// <summary>
            /// Copy the lower double-precision (64-bit) floating-point element of a to dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Double</returns>
            [DebuggerStepThrough]
            public static double mm256_cvtsd_f64(v256 a)
            {
                // Burst IR is fine here.
                return a.Double0;
            }

            /// <summary>
            /// Copy the lower 32-bit integer in a to dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int mm256_cvtsi256_si32(v256 a)
            {
                // Burst IR is fine here.
                return a.SInt0;
            }

            /// <summary>
            /// Copy the lower 64-bit integer in a to dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static long mm256_cvtsi256_si64(v256 a)
            {
                // Burst IR is fine here.
                return a.SLong0;
            }

            /// <summary>
            /// Compare packed 8-bit integers in a and b for equality, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpeq_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.cmpeq_epi8(a.Lo128, b.Lo128), Sse2.cmpeq_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 16-bit integers in a and b for equality, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpeq_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.cmpeq_epi16(a.Lo128, b.Lo128), Sse2.cmpeq_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 32-bit integers in a and b for equality, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpeq_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.cmpeq_epi32(a.Lo128, b.Lo128), Sse2.cmpeq_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 64-bit integers in a and b for equality, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpeq_epi64(v256 a, v256 b)
            {
                return new v256(Sse4_1.cmpeq_epi64(a.Lo128, b.Lo128), Sse4_1.cmpeq_epi64(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 8-bit integers in a and b for greater-than, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpgt_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.cmpgt_epi8(a.Lo128, b.Lo128), Sse2.cmpgt_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 16-bit integers in a and b for greater-than, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpgt_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.cmpgt_epi16(a.Lo128, b.Lo128), Sse2.cmpgt_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 32-bit integers in a and b for greater-than, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpgt_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.cmpgt_epi32(a.Lo128, b.Lo128), Sse2.cmpgt_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 64-bit integers in a and b for equality, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cmpgt_epi64(v256 a, v256 b)
            {
                return new v256(Sse4_2.cmpgt_epi64(a.Lo128, b.Lo128), Sse4_2.cmpgt_epi64(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 8-bit integers in a and b, and store packed maximum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_epi8(v256 a, v256 b)
            {
                return new v256(Sse4_1.max_epi8(a.Lo128, b.Lo128), Sse4_1.max_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 16-bit integers in a and b, and store packed maximum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.max_epi16(a.Lo128, b.Lo128), Sse2.max_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 32-bit integers in a and b, and store packed maximum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_epi32(v256 a, v256 b)
            {
                return new v256(Sse4_1.max_epi32(a.Lo128, b.Lo128), Sse4_1.max_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed unsigned 8-bit integers in a and b, and store packed maximum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_epu8(v256 a, v256 b)
            {
                return new v256(Sse2.max_epu8(a.Lo128, b.Lo128), Sse2.max_epu8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed unsigned 16-bit integers in a and b, and store packed maximum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_epu16(v256 a, v256 b)
            {
                return new v256(Sse4_1.max_epu16(a.Lo128, b.Lo128), Sse4_1.max_epu16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed unsigned 32-bit integers in a and b, and store packed maximum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_max_epu32(v256 a, v256 b)
            {
                return new v256(Sse4_1.max_epu32(a.Lo128, b.Lo128), Sse4_1.max_epu32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 8-bit integers in a and b, and store packed minimum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_epi8(v256 a, v256 b)
            {
                return new v256(Sse4_1.min_epi8(a.Lo128, b.Lo128), Sse4_1.min_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 16-bit integers in a and b, and store packed minimum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.min_epi16(a.Lo128, b.Lo128), Sse2.min_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed 32-bit integers in a and b, and store packed minimum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_epi32(v256 a, v256 b)
            {
                return new v256(Sse4_1.min_epi32(a.Lo128, b.Lo128), Sse4_1.min_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed unsigned 8-bit integers in a and b, and store packed minimum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_epu8(v256 a, v256 b)
            {
                return new v256(Sse2.min_epu8(a.Lo128, b.Lo128), Sse2.min_epu8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed unsigned 16-bit integers in a and b, and store packed minimum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_epu16(v256 a, v256 b)
            {
                return new v256(Sse4_1.min_epu16(a.Lo128, b.Lo128), Sse4_1.min_epu16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compare packed unsigned 32-bit integers in a and b, and store packed minimum values in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_min_epu32(v256 a, v256 b)
            {
                return new v256(Sse4_1.min_epu32(a.Lo128, b.Lo128), Sse4_1.min_epu32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise AND of 256 bits (representing integer data) in a and b, and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_and_si256(v256 a, v256 b)
            {
                return new v256(Sse2.and_si128(a.Lo128, b.Lo128), Sse2.and_si128(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise NOT of 256 bits (representing integer data) in a and then AND with b, and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_andnot_si256(v256 a, v256 b)
            {
                return new v256(Sse2.andnot_si128(a.Lo128, b.Lo128), Sse2.andnot_si128(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise OR of 256 bits (representing integer data) in a and b, and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_or_si256(v256 a, v256 b)
            {
                return new v256(Sse2.or_si128(a.Lo128, b.Lo128), Sse2.or_si128(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the bitwise XOR of 256 bits (representing integer data) in a and b, and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_xor_si256(v256 a, v256 b)
            {
                return new v256(Sse2.xor_si128(a.Lo128, b.Lo128), Sse2.xor_si128(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the absolute value of packed 8-bit integers in a, and store the unsigned results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_abs_epi8(v256 a)
            {
                return new v256(Ssse3.abs_epi8(a.Lo128), Ssse3.abs_epi8(a.Hi128));
            }

            /// <summary>
            /// Compute the absolute value of packed 16-bit integers in a, and store the unsigned results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_abs_epi16(v256 a)
            {
                return new v256(Ssse3.abs_epi16(a.Lo128), Ssse3.abs_epi16(a.Hi128));
            }

            /// <summary>
            /// Compute the absolute value of packed 32-bit integers in a, and store the unsigned results in dst. 
            /// </summary>
            /// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_abs_epi32(v256 a)
            {
                return new v256(Ssse3.abs_epi32(a.Lo128), Ssse3.abs_epi32(a.Hi128));
            }

            /// <summary>
            /// Add packed 8-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_add_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.add_epi8(a.Lo128, b.Lo128), Sse2.add_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed 16-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_add_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.add_epi16(a.Lo128, b.Lo128), Sse2.add_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed 32-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_add_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.add_epi32(a.Lo128, b.Lo128), Sse2.add_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed 64-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_add_epi64(v256 a, v256 b)
            {
                return new v256(Sse2.add_epi64(a.Lo128, b.Lo128), Sse2.add_epi64(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed 8-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_adds_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.adds_epi8(a.Lo128, b.Lo128), Sse2.adds_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed 16-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_adds_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.adds_epi16(a.Lo128, b.Lo128), Sse2.adds_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed unsigned 8-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_adds_epu8(v256 a, v256 b)
            {
                return new v256(Sse2.adds_epu8(a.Lo128, b.Lo128), Sse2.adds_epu8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Add packed unsigned 16-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_adds_epu16(v256 a, v256 b)
            {
                return new v256(Sse2.adds_epu16(a.Lo128, b.Lo128), Sse2.adds_epu16(a.Hi128, b.Hi128));
            }


            /// <summary>
            /// Subtract packed 8-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sub_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.sub_epi8(a.Lo128, b.Lo128), Sse2.sub_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed 16-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sub_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.sub_epi16(a.Lo128, b.Lo128), Sse2.sub_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed 32-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sub_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.sub_epi32(a.Lo128, b.Lo128), Sse2.sub_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed 64-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sub_epi64(v256 a, v256 b)
            {
                return new v256(Sse2.sub_epi64(a.Lo128, b.Lo128), Sse2.sub_epi64(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed 8-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_subs_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.subs_epi8(a.Lo128, b.Lo128), Sse2.subs_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed 16-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_subs_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.subs_epi16(a.Lo128, b.Lo128), Sse2.subs_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed unsigned 8-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_subs_epu8(v256 a, v256 b)
            {
                return new v256(Sse2.subs_epu8(a.Lo128, b.Lo128), Sse2.subs_epu8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Subtract packed unsigned 16-bit integers in a and b using saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_subs_epu16(v256 a, v256 b)
            {
                return new v256(Sse2.subs_epu16(a.Lo128, b.Lo128), Sse2.subs_epu16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Average packed unsigned 8-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_avg_epu8(v256 a, v256 b)
            {
                return new v256(Sse2.avg_epu8(a.Lo128, b.Lo128), Sse2.avg_epu8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Average packed unsigned 16-bit integers in a and b, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_avg_epu16(v256 a, v256 b)
            {
                return new v256(Sse2.avg_epu16(a.Lo128, b.Lo128), Sse2.avg_epu16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally add adjacent pairs of 16-bit integers in a and b, and pack the signed 16-bit results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hadd_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.hadd_epi16(a.Lo128, b.Lo128), Ssse3.hadd_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally add adjacent pairs of 32-bit integers in a and b, and pack the signed 16-bit results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hadd_epi32(v256 a, v256 b)
            {
                return new v256(Ssse3.hadd_epi32(a.Lo128, b.Lo128), Ssse3.hadd_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally add adjacent pairs of 16-bit integers in a and b using saturation, and pack the signed 16-bit results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hadds_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.hadds_epi16(a.Lo128, b.Lo128), Ssse3.hadds_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally subtract adjacent pairs of 16-bit integers in a and b, and pack the signed 16-bit results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hsub_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.hsub_epi16(a.Lo128, b.Lo128), Ssse3.hsub_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally subtract adjacent pairs of 32-bit integers in a and b, and pack the signed 16-bit results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hsub_epi32(v256 a, v256 b)
            {
                return new v256(Ssse3.hsub_epi32(a.Lo128, b.Lo128), Ssse3.hsub_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Horizontally subtract adjacent pairs of 16-bit integers in a and b using saturation, and pack the signed 16-bit results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_hsubs_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.hsubs_epi16(a.Lo128, b.Lo128), Ssse3.hsubs_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply packed signed 16-bit integers in a and b, producing
            /// intermediate signed 32-bit integers. Horizontally add adjacent
            /// pairs of intermediate 32-bit integers, and pack the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_madd_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.madd_epi16(a.Lo128, b.Lo128), Sse2.madd_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Vertically multiply each unsigned 8-bit integer from a with the
            /// corresponding signed 8-bit integer from b, producing
            /// intermediate signed 16-bit integers. Horizontally add adjacent
            /// pairs of intermediate signed 16-bit integers, and pack the
            /// saturated results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_maddubs_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.maddubs_epi16(a.Lo128, b.Lo128), Ssse3.maddubs_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply the packed 16-bit integers in a and b, producing intermediate 32-bit integers, and store the high 16 bits of the intermediate integers in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mulhi_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.mulhi_epi16(a.Lo128, b.Lo128), Sse2.mulhi_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply the packed unsigned 16-bit integers in a and b,
            /// producing intermediate 32-bit integers, and store the high 16
            /// bits of the intermediate integers in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mulhi_epu16(v256 a, v256 b)
            {
                return new v256(Sse2.mulhi_epu16(a.Lo128, b.Lo128), Sse2.mulhi_epu16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply the packed 16-bit integers in a and b, producing
            /// intermediate 32-bit integers, and store the low 16 bits of the
            /// intermediate integers in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mullo_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.mullo_epi16(a.Lo128, b.Lo128), Sse2.mullo_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply the packed 32-bit integers in a and b, producing
            /// intermediate 64-bit integers, and store the low 32 bits of the
            /// intermediate integers in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mullo_epi32(v256 a, v256 b)
            {
                return new v256(Sse4_1.mullo_epi32(a.Lo128, b.Lo128), Sse4_1.mullo_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply the low unsigned 32-bit integers from each packed 64-bit element in a and b, and store the unsigned 64-bit results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mul_epu32(v256 a, v256 b)
            {
                return new v256(Sse2.mul_epu32(a.Lo128, b.Lo128), Sse2.mul_epu32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Multiply the low 32-bit integers from each packed 64-bit element in a and b, and store the signed 64-bit results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mul_epi32(v256 a, v256 b)
            {
                return new v256(Sse4_1.mul_epi32(a.Lo128, b.Lo128), Sse4_1.mul_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Negate packed 8-bit integers in a when the corresponding signed
            /// 8-bit integer in b is negative, and store the results in dst.
            /// Element in dst are zeroed out when the corresponding element in
            /// b is zero.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sign_epi8(v256 a, v256 b)
            {
                return new v256(Ssse3.sign_epi8(a.Lo128, b.Lo128), Ssse3.sign_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Negate packed 16-bit integers in a when the corresponding signed
            /// 16-bit integer in b is negative, and store the results in dst.
            /// Element in dst are zeroed out when the corresponding element in
            /// b is zero.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sign_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.sign_epi16(a.Lo128, b.Lo128), Ssse3.sign_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Negate packed 32-bit integers in a when the corresponding signed
            /// 32-bit integer in b is negative, and store the results in dst.
            /// Element in dst are zeroed out when the corresponding element in
            /// b is zero.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sign_epi32(v256 a, v256 b)
            {
                return new v256(Ssse3.sign_epi32(a.Lo128, b.Lo128), Ssse3.sign_epi32(a.Hi128, b.Hi128));
            }


            /// <summary>
            /// Multiply packed 16-bit integers in a and b, producing
            /// intermediate signed 32-bit integers. Truncate each intermediate
            /// integer to the 18 most significant bits, round by adding 1, and
            /// store bits [16:1] to dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mulhrs_epi16(v256 a, v256 b)
            {
                return new v256(Ssse3.mulhrs_epi16(a.Lo128, b.Lo128), Ssse3.mulhrs_epi16(a.Hi128, b.Hi128));
            }


            /// <summary>
            /// Compute the absolute differences of packed unsigned 8-bit
            /// integers in a and b, then horizontally sum each consecutive 8
            /// differences to produce four unsigned 16-bit integers, and pack
            /// these unsigned 16-bit integers in the low 16 bits of 64-bit
            /// elements in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sad_epu8(v256 a, v256 b)
            {
                return new v256(Sse2.sad_epu8(a.Lo128, b.Lo128), Sse2.sad_epu8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Compute the sum of absolute differences (SADs) of quadruplets of
            /// unsigned 8-bit integers in a compared to those in b, and store
            /// the 16-bit results in dst. Eight SADs are performed for each
            /// 128-bit lane using one quadruplet from b and eight quadruplets
            /// from a. One quadruplet is selected from b starting at on the
            /// offset specified in imm8. Eight quadruplets are formed from
            /// sequential 8-bit integers selected from a starting at the offset
            /// specified in imm8.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mpsadbw_epu8(v256 a, v256 b, int imm8)
            {
                return new v256(Sse4_1.mpsadbw_epu8(a.Lo128, b.Lo128, imm8 & 7), Sse4_1.mpsadbw_epu8(a.Hi128, b.Hi128, (imm8 >> 3) & 7));
            }

            /// <summary>
            /// Shift 128-bit lanes in a left by imm8 bytes while shifting in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_slli_si256(v256 a, int imm8)
            {
                return new v256(Sse2.slli_si128(a.Lo128, imm8), Sse2.slli_si128(a.Hi128, imm8));
            }

            // This is just a #define alias
            /// <summary>
            /// Shift 128-bit lanes in a left by imm8 bytes while shifting in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_bslli_epi128(v256 a, int imm8)
            {
                return mm256_slli_si256(a, imm8);
            }

            /// <summary>
            /// Shift 128-bit lanes in a right by imm8 bytes while shifting in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srli_si256(v256 a, int imm8)
            {
                return new v256(Sse2.srli_si128(a.Lo128, imm8), Sse2.srli_si128(a.Hi128, imm8));
            }

            // This is just a #define alias
            /// <summary>
            /// Shift 128-bit lanes in a right by imm8 bytes while shifting in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_bsrli_epi128(v256 a, int imm8)
            {
                return mm256_srli_si256(a, imm8);
            }

            /// <summary>
            /// Shift packed 16-bit integers in a left by count while shifting
            /// in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Shift amount</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sll_epi16(v256 a, v128 count)
            {
                return new v256(Sse2.sll_epi16(a.Lo128, count), Sse2.sll_epi16(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a left by count while shifting
            /// in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sll_epi32(v256 a, v128 count)
            {
                return new v256(Sse2.sll_epi32(a.Lo128, count), Sse2.sll_epi32(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 64-bit integers in a left by count while shifting
            /// in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sll_epi64(v256 a, v128 count)
            {
                return new v256(Sse2.sll_epi64(a.Lo128, count), Sse2.sll_epi64(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 16-bit integers in a left by imm8 while shifting in zeros, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_slli_epi16(v256 a, int imm8)
            {
                return new v256(Sse2.slli_epi16(a.Lo128, imm8), Sse2.slli_epi16(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a left by imm8 while shifting in zeros, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_slli_epi32(v256 a, int imm8)
            {
                return new v256(Sse2.slli_epi32(a.Lo128, imm8), Sse2.slli_epi32(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 64-bit integers in a left by imm8 while shifting in zeros, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_slli_epi64(v256 a, int imm8)
            {
                return new v256(Sse2.slli_epi64(a.Lo128, imm8), Sse2.slli_epi64(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a left by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sllv_epi32(v256 a, v256 count)
            {
                return new v256(sllv_epi32(a.Lo128, count.Lo128), sllv_epi32(a.Hi128, count.Hi128));
            }

            /// <summary>
            /// Shift packed 64-bit integers in a left by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sllv_epi64(v256 a, v256 count)
            {
                return new v256(sllv_epi64(a.Lo128, count.Lo128), sllv_epi64(a.Hi128, count.Hi128));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a left by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sllv_epi32(v128 a, v128 count)
            {
                v128 dst = default;
                uint* aptr = &a.UInt0;
                uint* dptr = &dst.UInt0;
                int* sptr = &count.SInt0;
                for (int i = 0; i < 4; ++i)
                {
                    int shift = sptr[i];
                    if (shift >= 0 && shift <= 31)
                    {
                        dptr[i] = aptr[i] << shift;
                    }
                    else
                    {
                        dptr[i] = 0;
                    }
                }
                return dst;
            }

            /// <summary>
            /// Shift packed 64-bit integers in a left by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sllv_epi64(v128 a, v128 count)
            {
                v128 dst = default;
                ulong* aptr = &a.ULong0;
                ulong* dptr = &dst.ULong0;
                long* sptr = &count.SLong0;
                for (int i = 0; i < 2; ++i)
                {
                    int shift = (int)sptr[i];
                    if (shift >= 0 && shift <= 63)
                    {
                        dptr[i] = aptr[i] << shift;
                    }
                    else
                    {
                        dptr[i] = 0;
                    }
                }
                return dst;
            }

            /// <summary>
            /// Shift packed 16-bit integers in a right by count while shifting in sign bits, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sra_epi16(v256 a, v128 count)
            {
                return new v256(Sse2.sra_epi16(a.Lo128, count), Sse2.sra_epi16(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by count while shifting in sign bits, and store the results in dst.
			/// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_sra_epi32(v256 a, v128 count)
            {
                return new v256(Sse2.sra_epi32(a.Lo128, count), Sse2.sra_epi32(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 16-bit integers in a right by imm8 while shifting in sign bits, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srai_epi16(v256 a, int imm8)
            {
                return new v256(Sse2.srai_epi16(a.Lo128, imm8), Sse2.srai_epi16(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by imm8 while shifting in sign bits, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srai_epi32(v256 a, int imm8)
            {
                return new v256(Sse2.srai_epi32(a.Lo128, imm8), Sse2.srai_epi32(a.Hi128, imm8));
            }


            /// <summary>
            /// Shift packed 32-bit integers in a right by the amount specified
            /// by the corresponding element in count while shifting in sign
            /// bits, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srav_epi32(v256 a, v256 count)
            {
                return new v256(srav_epi32(a.Lo128, count.Lo128), srav_epi32(a.Hi128, count.Hi128));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by the amount specified
            /// by the corresponding element in count while shifting in sign
            /// bits, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 srav_epi32(v128 a, v128 count)
            {
                v128 dst = default;
                int* aptr = &a.SInt0;
                int* dptr = &dst.SInt0;
                int* sptr = &count.SInt0;
                for (int i = 0; i < 4; ++i)
                {
                    // Work around modulo 32 shifts on the general register file
                    int shift1 = Math.Min(sptr[i] & 0xff, 32);
                    int shift2 = 0;

                    if (shift1 >= 16)
                    {
                        shift1 -= 16;
                        shift2 += 16;
                    }

                    dptr[i] = (aptr[i] >> shift1) >> shift2;
                }
                return dst;
            }

            /// <summary>
            /// Shift packed 16-bit integers in a right by count while shifting
            /// in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srl_epi16(v256 a, v128 count)
            {
                return new v256(Sse2.srl_epi16(a.Lo128, count), Sse2.srl_epi16(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by count while shifting
            /// in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srl_epi32(v256 a, v128 count)
            {
                return new v256(Sse2.srl_epi32(a.Lo128, count), Sse2.srl_epi32(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 64-bit integers in a right by count while shifting
            /// in zeros, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srl_epi64(v256 a, v128 count)
            {
                return new v256(Sse2.srl_epi64(a.Lo128, count), Sse2.srl_epi64(a.Hi128, count));
            }

            /// <summary>
            /// Shift packed 16-bit integers in a right by imm8 while shifting in zeros, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srli_epi16(v256 a, int imm8)
            {
                return new v256(Sse2.srli_epi16(a.Lo128, imm8), Sse2.srli_epi16(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by imm8 while shifting in zeros, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srli_epi32(v256 a, int imm8)
            {
                return new v256(Sse2.srli_epi32(a.Lo128, imm8), Sse2.srli_epi32(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 64-bit integers in a right by imm8 while shifting in zeros, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srli_epi64(v256 a, int imm8)
            {
                return new v256(Sse2.srli_epi64(a.Lo128, imm8), Sse2.srli_epi64(a.Hi128, imm8));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srlv_epi32(v256 a, v256 count)
            {
                return new v256(srlv_epi32(a.Lo128, count.Lo128), srlv_epi32(a.Hi128, count.Hi128));
            }

            /// <summary>
            /// Shift packed 64-bit integers in a right by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_srlv_epi64(v256 a, v256 count)
            {
                return new v256(srlv_epi64(a.Lo128, count.Lo128), srlv_epi64(a.Hi128, count.Hi128));
            }

            /// <summary>
            /// Shift packed 32-bit integers in a right by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 srlv_epi32(v128 a, v128 count)
            {
                v128 dst = default;
                uint* aptr = &a.UInt0;
                uint* dptr = &dst.UInt0;
                int* sptr = &count.SInt0;
                for (int i = 0; i < 4; ++i)
                {
                    int shift = sptr[i];
                    if (shift >= 0 && shift <= 31)
                    {
                        dptr[i] = aptr[i] >> shift;
                    }
                    else
                    {
                        dptr[i] = 0;
                    }
                }
                return dst;
            }

            /// <summary>
            /// Shift packed 64-bit integers in a right by the amount specified
            /// by the corresponding element in count while shifting in zeros,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="count">Corresponding element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 srlv_epi64(v128 a, v128 count)
            {
                v128 dst = default;
                ulong* aptr = &a.ULong0;
                ulong* dptr = &dst.ULong0;
                long* sptr = &count.SLong0;
                for (int i = 0; i < 2; ++i)
                {
                    int shift = (int)sptr[i];
                    if (shift >= 0 && shift <= 63)
                    {
                        dptr[i] = aptr[i] >> shift;
                    }
                    else
                    {
                        dptr[i] = 0;
                    }
                }
                return dst;
            }

            /// <summary>
            /// Blend packed 32-bit integers from a and b using control mask imm8, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blend_epi32(v128 a, v128 b, int imm8)
            {
                return Sse4_1.blend_ps(a, b, imm8);
            }

            /// <summary>
            /// Blend packed 32-bit integers from a and b using control mask imm8, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blend_epi32(v256 a, v256 b, int imm8)
            {
                return Avx.mm256_blend_ps(a, b, imm8);
            }

            /// <summary>
            /// Concatenate pairs of 16-byte blocks in a and b into a 32-byte temporary result, shift the result right by imm8 bytes, and store the low 16 bytes in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_alignr_epi8(v256 a, v256 b, int imm8)
            {
                return new v256(Ssse3.alignr_epi8(a.Lo128, b.Lo128, imm8), Ssse3.alignr_epi8(a.Hi128, b.Hi128, imm8));
            }

            /// <summary>
            /// Blend packed 8-bit integers from a and b using mask, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blendv_epi8(v256 a, v256 b, v256 mask)
            {
                return new v256(Sse4_1.blendv_epi8(a.Lo128, b.Lo128, mask.Lo128), Sse4_1.blendv_epi8(a.Hi128, b.Hi128, mask.Hi128));
            }

            /// <summary>
            /// Blend packed 16-bit integers from a and b within 128-bit lanes using control mask imm8, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_blend_epi16(v256 a, v256 b, int imm8)
            {
                return new v256(Sse4_1.blend_epi16(a.Lo128, b.Lo128, imm8), Sse4_1.blend_epi16(a.Hi128, b.Hi128, imm8));
            }


            /// <summary>
            /// Convert packed 16-bit integers from a and b to packed 8-bit integers using signed saturation, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_packs_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.packs_epi16(a.Lo128, b.Lo128), Sse2.packs_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Convert packed 32-bit integers from a and b to packed 16-bit integers using signed saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_packs_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.packs_epi32(a.Lo128, b.Lo128), Sse2.packs_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Convert packed 16-bit integers from a and b to packed 8-bit integers using unsigned saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_packus_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.packus_epi16(a.Lo128, b.Lo128), Sse2.packus_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Convert packed 32-bit integers from a and b to packed 16-bit integers using unsigned saturation, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_packus_epi32(v256 a, v256 b)
            {
                return new v256(Sse4_1.packus_epi32(a.Lo128, b.Lo128), Sse4_1.packus_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 8-bit integers from the high half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpackhi_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.unpackhi_epi8(a.Lo128, b.Lo128), Sse2.unpackhi_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 16-bit integers from the high half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpackhi_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.unpackhi_epi16(a.Lo128, b.Lo128), Sse2.unpackhi_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 32-bit integers from the high half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpackhi_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.unpackhi_epi32(a.Lo128, b.Lo128), Sse2.unpackhi_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 64-bit integers from the high half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpackhi_epi64(v256 a, v256 b)
            {
                return new v256(Sse2.unpackhi_epi64(a.Lo128, b.Lo128), Sse2.unpackhi_epi64(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 8-bit integers from the low half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpacklo_epi8(v256 a, v256 b)
            {
                return new v256(Sse2.unpacklo_epi8(a.Lo128, b.Lo128), Sse2.unpacklo_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 16-bit integers from the low half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpacklo_epi16(v256 a, v256 b)
            {
                return new v256(Sse2.unpacklo_epi16(a.Lo128, b.Lo128), Sse2.unpacklo_epi16(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 32-bit integers from the low half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpacklo_epi32(v256 a, v256 b)
            {
                return new v256(Sse2.unpacklo_epi32(a.Lo128, b.Lo128), Sse2.unpacklo_epi32(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Unpack and interleave 64-bit integers from the low half of each 128-bit lane in a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_unpacklo_epi64(v256 a, v256 b)
            {
                return new v256(Sse2.unpacklo_epi64(a.Lo128, b.Lo128), Sse2.unpacklo_epi64(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Shuffle 8-bit integers in a within 128-bit lanes according to
            /// shuffle control mask in the corresponding 8-bit element of b,
            /// and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_shuffle_epi8(v256 a, v256 b)
            {
                return new v256(Ssse3.shuffle_epi8(a.Lo128, b.Lo128), Ssse3.shuffle_epi8(a.Hi128, b.Hi128));
            }

            /// <summary>
            /// Shuffle 32-bit integers in a within 128-bit lanes using the control in imm8, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_shuffle_epi32(v256 a, int imm8)
            {
                return new v256(Sse2.shuffle_epi32(a.Lo128, imm8), Sse2.shuffle_epi32(a.Hi128, imm8));
            }

            /// <summary>
            /// Shuffle 16-bit integers in the high 64 bits of 128-bit lanes of
            /// a using the control in imm8. Store the results in the high 64
            /// bits of 128-bit lanes of dst, with the low 64 bits of 128-bit
            /// lanes being copied from from a to dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_shufflehi_epi16(v256 a, int imm8)
            {
                return new v256(Sse2.shufflehi_epi16(a.Lo128, imm8), Sse2.shufflehi_epi16(a.Hi128, imm8));
            }

            /// <summary>
            /// Shuffle 16-bit integers in the low 64 bits of 128-bit lanes of
            /// a using the control in imm8. Store the results in the low 64
            /// bits of 128-bit lanes of dst, with the high 64 bits of 128-bit
            /// lanes being copied from from a to dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_shufflelo_epi16(v256 a, int imm8)
            {
                return new v256(Sse2.shufflelo_epi16(a.Lo128, imm8), Sse2.shufflelo_epi16(a.Hi128, imm8));
            }

            /// <summary>
            /// Extract 128 bits (composed of integer data) from a, selected with imm8, and store the result in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Selection</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_extracti128_si256(v256 a, int imm8)
            {
                // Burst IR is fine.
                return Avx.mm256_extractf128_si256(a, imm8);
            }

            /// <summary>
            /// Copy a to dst, then insert 128 bits (composed of integer data) from b into dst at the location specified by imm8.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_inserti128_si256(v256 a, v128 b, int imm8)
            {
                // Burst IR is fine.
                return Avx.mm256_insertf128_ps(a, b, imm8);
            }

            /// <summary>
            /// Broadcast the low single-precision (32-bit) floating-point element from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcastss_ps(v128 a)
            {
                return new v128(a.Float0);
            }

            /// <summary>
            /// Broadcast the low single-precision (32-bit) floating-point element from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastss_ps(v128 a)
            {
                return new v256(a.Float0);
            }

            /// <summary>
            /// Broadcast the low double-precision (64-bit) floating-point element from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcastsd_pd(v128 a)
            {
                return new v128(a.Double0);
            }

            /// <summary>
            /// Broadcast the low double-precision (64-bit) floating-point element from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastsd_pd(v128 a)
            {
                return new v256(a.Double0);
            }

            /// <summary>
            /// Broadcast the low packed 8-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcastb_epi8(v128 a)
            {
                return new v128(a.Byte0);
            }

            /// <summary>
            /// Broadcast the low packed 16-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcastw_epi16(v128 a)
            {
                return new v128(a.SShort0);
            }

            /// <summary>
            /// Broadcast the low packed 32-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcastd_epi32(v128 a)
            {
                return new v128(a.SInt0);
            }

            /// <summary>
            /// Broadcast the low packed 64-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 broadcastq_epi64(v128 a)
            {
                return new v128(a.SLong0);
            }

            /// <summary>
            /// Broadcast the low packed 8-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastb_epi8(v128 a)
            {
                return new v256(a.Byte0);
            }

            /// <summary>
            /// Broadcast the low packed 16-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastw_epi16(v128 a)
            {
                return new v256(a.SShort0);
            }

            /// <summary>
            /// Broadcast the low packed 32-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastd_epi32(v128 a)
            {
                return new v256(a.SInt0);
            }

            /// <summary>
            /// Broadcast the low packed 64-bit integer from a to all elements of dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastq_epi64(v128 a)
            {
                return new v256(a.SLong0);
            }

            /// <summary>
            /// Broadcast 128 bits of integer data from a to all 128-bit lanes in dst
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_broadcastsi128_si256(v128 a)
            {
                // Burst IR is fine
                return new v256(a, a);
            }

            /// <summary>
            /// Sign extend packed 8-bit integers in a to packed 16-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi8_epi16(v128 a)
            {
                v256 dst = default;
                short* dptr = &dst.SShort0;
                sbyte* aptr = &a.SByte0;

                for (int j = 0; j <= 15; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed 8-bit integers in a to packed 32-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi8_epi32(v128 a)
            {
                v256 dst = default;
                int* dptr = &dst.SInt0;
                sbyte* aptr = &a.SByte0;

                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed 8-bit integers in a to packed 64-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi8_epi64(v128 a)
            {
                v256 dst = default;
                long* dptr = &dst.SLong0;
                sbyte* aptr = &a.SByte0;

                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed 16-bit integers in a to packed 32-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi16_epi32(v128 a)
            {
                v256 dst = default;
                int* dptr = &dst.SInt0;
                short* aptr = &a.SShort0;

                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed 16-bit integers in a to packed 64-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi16_epi64(v128 a)
            {
                v256 dst = default;
                long* dptr = &dst.SLong0;
                short* aptr = &a.SShort0;

                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed 32-bit integers in a to packed 64-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepi32_epi64(v128 a)
            {
                v256 dst = default;
                long* dptr = &dst.SLong0;
                int* aptr = &a.SInt0;

                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed unsigned 8-bit integers in a to packed 16-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepu8_epi16(v128 a)
            {
                v256 dst = default;
                short* dptr = &dst.SShort0;
                byte* aptr = &a.Byte0;

                for (int j = 0; j <= 15; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed unsigned 8-bit integers in a to packed 32-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepu8_epi32(v128 a)
            {
                v256 dst = default;
                int* dptr = &dst.SInt0;
                byte* aptr = &a.Byte0;

                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed unsigned 8-bit integers in a to packed 64-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepu8_epi64(v128 a)
            {
                v256 dst = default;
                long* dptr = &dst.SLong0;
                byte* aptr = &a.Byte0;

                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed unsigned 16-bit integers in a to packed 32-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepu16_epi32(v128 a)
            {
                v256 dst = default;
                int* dptr = &dst.SInt0;
                ushort* aptr = &a.UShort0;

                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed unsigned 16-bit integers in a to packed 64-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepu16_epi64(v128 a)
            {
                v256 dst = default;
                long* dptr = &dst.SLong0;
                ushort* aptr = &a.UShort0;

                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Sign extend packed unsigned 32-bit integers in a to packed 64-bit integers, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_cvtepu32_epi64(v128 a)
            {
                v256 dst = default;
                long* dptr = &dst.SLong0;
                uint* aptr = &a.UInt0;

                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            /// <summary>
            /// Load packed 32-bit integers from memory into dst using mask
            /// (elements are zeroed out when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 maskload_epi32(void* mem_addr, v128 mask)
            {
                v128 dst = default;
                int* sptr = (int*)mem_addr;
                int* mptr = &mask.SInt0;
                int* dptr = &dst.SInt0;
                for (int i = 0; i < 4; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
                return dst;
            }

            /// <summary>
            /// Load packed 64-bit integers from memory into dst using mask
            /// (elements are zeroed out when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 maskload_epi64(void* mem_addr, v128 mask)
            {
                v128 dst = default;
                long* sptr = (long*)mem_addr;
                long* mptr = &mask.SLong0;
                long* dptr = &dst.SLong0;
                for (int i = 0; i < 2; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
                return dst;
            }

            /// <summary>
            /// Store packed 32-bit integers from a into memory using mask
            /// (elements are not stored when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void maskstore_epi32(void* mem_addr, v128 mask, v128 a)
            {
                int* dptr = (int*)mem_addr;
                int* mptr = &mask.SInt0;
                int* sptr = &a.SInt0;
                for (int i = 0; i < 4; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
            }

            /// <summary>
            /// Store packed 64-bit integers from a into memory using mask
            /// (elements are not stored when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void maskstore_epi64(void* mem_addr, v128 mask, v128 a)
            {
                long* dptr = (long*)mem_addr;
                long* mptr = &mask.SLong0;
                long* sptr = &a.SLong0;
                for (int i = 0; i < 2; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
            }

            /// <summary>
            /// Load packed 32-bit integers from memory into dst using mask
            /// (elements are zeroed out when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_maskload_epi32(void* mem_addr, v256 mask)
            {
                v256 dst = default;
                int* sptr = (int*)mem_addr;
                int* mptr = &mask.SInt0;
                int* dptr = &dst.SInt0;
                for (int i = 0; i < 8; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
                return dst;
            }

            /// <summary>
            /// Load packed 64-bit integers from memory into dst using mask
            /// (elements are zeroed out when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_maskload_epi64(void* mem_addr, v256 mask)
            {
                v256 dst = default;
                long* sptr = (long*)mem_addr;
                long* mptr = &mask.SLong0;
                long* dptr = &dst.SLong0;
                for (int i = 0; i < 4; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
                return dst;
            }

            /// <summary>
            /// Store packed 32-bit integers from a into memory using mask
            /// (elements are not stored when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_maskstore_epi32(void* mem_addr, v256 mask, v256 a)
            {
                int* dptr = (int*)mem_addr;
                int* mptr = &mask.SInt0;
                int* sptr = &a.SInt0;
                for (int i = 0; i < 8; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
            }

            /// <summary>
            /// Store packed 64-bit integers from a into memory using mask
            /// (elements are not stored when the highest bit is not set in the
            /// corresponding element).
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="mask">Mask</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void mm256_maskstore_epi64(void* mem_addr, v256 mask, v256 a)
            {
                long* dptr = (long*)mem_addr;
                long* mptr = &mask.SLong0;
                long* sptr = &a.SLong0;
                for (int i = 0; i < 4; ++i)
                {
                    if (mptr[i] < 0)
                    {
                        dptr[i] = sptr[i];
                    }
                }
            }

            /// <summary>
            /// Shuffle 32-bit integers in a across lanes using the corresponding index in idx, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="idx">idx</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permutevar8x32_epi32(v256 a, v256 idx)
            {
                v256 dst = default;
                int* iptr = &idx.SInt0;
                int* aptr = &a.SInt0;
                int* dptr = &dst.SInt0;

                for (int i = 0; i < 8; ++i)
                {
                    int index = iptr[i] & 7;
                    dptr[i] = aptr[index];
                }

                return dst;
            }

            /// <summary>
            /// Shuffle single-precision (32-bit) floating-point elements in a across lanes using the corresponding index in idx.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="idx">idx</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permutevar8x32_ps(v256 a, v256 idx)
            {
                return mm256_permutevar8x32_epi32(a, idx);
            }

            /// <summary>
            /// Shuffle 64-bit integers in a across lanes using the control in imm8, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute4x64_epi64(v256 a, int imm8)
            {
                v256 dst = default;
                long* aptr = &a.SLong0;
                long* dptr = &dst.SLong0;

                for (int i = 0; i < 4; ++i, imm8 >>= 2)
                {
                    dptr[i] = aptr[imm8 & 3];
                }

                return dst;
            }

            /// <summary>
            /// Shuffle double-precision (64-bit) floating-point elements in a across lanes using the control in imm8, and store the results in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute4x64_pd(v256 a, int imm8)
            {
                return mm256_permute4x64_epi64(a, imm8);
            }

            /// <summary>
            /// Shuffle 128-bits (composed of integer data) selected by imm8 from a and b, and store the results in dst. 
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Selection</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_permute2x128_si256(v256 a, v256 b, int imm8)
            {
                return Avx.mm256_permute2f128_si256(a, b, imm8);
            }

            /// <summary>
            /// Load 256-bits of integer data from memory into dst using a
            /// non-temporal memory hint. mem_addr must be aligned on a 32-byte
            /// boundary or a general-protection exception may be generated.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_stream_load_si256(void* mem_addr)
            {
                return *(v256*)mem_addr;
            }

            private static void EmulatedGather<T, U>(T* dptr, void* base_addr, long* indexPtr, int scale, int n, U* mask) where T : unmanaged where U : unmanaged, IComparable<U>
            {
                U maskZero = default;

                for (int i = 0; i < n; ++i)
                {
                    long baseIndex = indexPtr[i];
                    long offset = baseIndex * scale;
                    T* mem_addr = (T*)((byte*)base_addr + offset);
                    if (mask == null || mask[i].CompareTo(maskZero) < 0)
                    {
                        dptr[i] = *mem_addr;
                    }
                }
            }

            private static void EmulatedGather<T, U>(T* dptr, void* base_addr, int* indexPtr, int scale, int n, U* mask) where T : unmanaged where U : unmanaged, IComparable<U>
            {
                U maskZero = default;

                for (int i = 0; i < n; ++i)
                {
                    long baseIndex = indexPtr[i];
                    long offset = baseIndex * scale;
                    T* mem_addr = (T*)((byte*)base_addr + offset);
                    if (mask == null || mask[i].CompareTo(maskZero) < 0)
                    {
                        dptr[i] = *mem_addr;
                    }
                }
            }

            /// <summary>
            /// Gather 32-bit integers from memory using 32-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_i32gather_epi32(void* base_addr, v256 vindex, int scale)
            {
                v256 dst = default;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SInt0, scale, sizeof(v256) / sizeof(int), (int*)null);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 32-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_i32gather_pd(void* base_addr, v128 vindex, int scale)
            {
                v256 dst = default;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SInt0, scale, 4, null);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 32-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_i32gather_ps(void* base_addr, v256 vindex, int scale)
            {
                v256 dst = default;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SInt0, scale, 8, null);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 64-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
            /// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_i64gather_pd(void* base_addr, v256 vindex, int scale)
            {
                v256 dst = default;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SLong0, scale, 4, null);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 64-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_i64gather_ps(void* base_addr, v256 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SLong0, scale, 4, null);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 32-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i32gather_pd(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SInt0, scale, 2, null);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 32-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i32gather_ps(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SInt0, scale, 4, null);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 64-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i64gather_pd(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SLong0, scale, 2, null);
                return dst;
            }
            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 64-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst. scale should be
            /// 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i64gather_ps(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SLong0, scale, 2, null);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 32-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_i32gather_epi64(void* base_addr, v128 vindex, int scale)
            {
                v256 dst = default;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SInt0, scale, 4, null);
                return dst;
            }


            /// <summary>
            /// Gather 32-bit integers from memory using 64-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_i64gather_epi32(void* base_addr, v256 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SLong0, scale, 4, null);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 64-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_i64gather_epi64(void* base_addr, v256 vindex, int scale)
            {
                v256 dst = default;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SLong0, scale, 4, null);
                return dst;
            }


            /// <summary>
            /// Gather 32-bit integers from memory using 32-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i32gather_epi32(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SInt0, scale, 4, null);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 32-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i32gather_epi64(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SInt0, scale, 2, null);
                return dst;
            }

            /// <summary>
            /// Gather 32-bit integers from memory using 64-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i64gather_epi32(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SLong0, scale, 2, null);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 64-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst.
            /// scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 i64gather_epi64(void* base_addr, v128 vindex, int scale)
            {
                v128 dst = default;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SLong0, scale, 2, null);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 32-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mask_i32gather_pd(v256 src, void* base_addr, v128 vindex, v256 mask, int scale)
            {
                v256 dst = src;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SInt0, scale, 4, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 32-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mask_i32gather_ps(v256 src, void* base_addr, v256 vindex, v256 mask, int scale)
            {
                v256 dst = src;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SInt0, scale, 8, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 64-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mask_i64gather_pd(v256 src, void* base_addr, v256 vindex, v256 mask, int scale)
            {
                v256 dst = src;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SLong0, scale, 4, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 64-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_mask_i64gather_ps(v128 src, void* base_addr, v256 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SLong0, scale, 4, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather 32-bit integers from memory using 32-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mask_i32gather_epi32(v256 src, void* base_addr, v256 vindex, v256 mask, int scale)
            {
                v256 dst = src;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SInt0, scale, 8, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 32-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mask_i32gather_epi64(v256 src, void* base_addr, v128 vindex, v256 mask, int scale)
            {
                v256 dst = src;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SInt0, scale, 4, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 64-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_mask_i64gather_epi64(v256 src, void* base_addr, v256 vindex, v256 mask, int scale)
            {
                v256 dst = src;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SLong0, scale, 4, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather 32-bit integers from memory using 64-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mm256_mask_i64gather_epi32(v128 src, void* base_addr, v256 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SLong0, scale, 4, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 32-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i32gather_pd(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SInt0, scale, 2, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 32-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 32-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i32gather_ps(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SInt0, scale, 4, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather double-precision (64-bit) floating-point elements from
            /// memory using 64-bit indices. 64-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i64gather_pd(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<double, long>(&dst.Double0, base_addr, &vindex.SLong0, scale, 2, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather single-precision (32-bit) floating-point elements from
            /// memory using 64-bit indices. 32-bit elements are loaded from
            /// addresses starting at base_addr and offset by each 64-bit
            /// element in vindex (each index is scaled by the factor in
            /// scale). Gathered elements are merged into dst using mask
            /// (elements are copied from src when the highest bit is not set
            /// in the corresponding element). scale should be 1, 2, 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i64gather_ps(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                dst.UInt2 = dst.UInt3 = 0;
                EmulatedGather<float, int>(&dst.Float0, base_addr, &vindex.SLong0, scale, 2, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather 32-bit integers from memory using 32-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i32gather_epi32(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SInt0, scale, 4, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 32-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 32-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i32gather_epi64(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SInt0, scale, 2, &mask.SLong0);
                return dst;
            }

            /// <summary>
            /// Gather 32-bit integers from memory using 64-bit indices. 32-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i64gather_epi32(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                dst.UInt2 = dst.UInt3 = 0;
                EmulatedGather<int, int>(&dst.SInt0, base_addr, &vindex.SLong0, scale, 2, &mask.SInt0);
                return dst;
            }

            /// <summary>
            /// Gather 64-bit integers from memory using 64-bit indices. 64-bit
            /// elements are loaded from addresses starting at base_addr and
            /// offset by each 64-bit element in vindex (each index is scaled
            /// by the factor in scale). Gathered elements are merged into dst
            /// using mask (elements are copied from src when the highest bit
            /// is not set in the corresponding element). scale should be 1, 2,
            /// 4 or 8.
            /// </summary>
			/// <param name="src">Source</param>
			/// <param name="base_addr">Base address</param>
			/// <param name="vindex">Offset</param>
			/// <param name="mask">Mask</param>
			/// <param name="scale">Index scale</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mask_i64gather_epi64(v128 src, void* base_addr, v128 vindex, v128 mask, int scale)
            {
                v128 dst = src;
                EmulatedGather<long, long>(&dst.SLong0, base_addr, &vindex.SLong0, scale, 2, &mask.SLong0);
                return dst;
            }
        }
    }
}
