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
        /// SSE intrinsics
        /// </summary>
        public static class Sse
        {
            /// <summary>
            /// Evaluates to true at compile time if SSE intrinsics are supported.
            /// </summary>
            public static bool IsSseSupported { get { return false; } }

            /// <summary>
            /// Load 128-bits (composed of 4 packed single-precision (32-bit)
            /// floating-point elements) from memory into dst.
            /// </summary>
            /// <remarks>
            /// Burst will always generate unaligned loads.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 load_ps(void* ptr)
            {
                return GenericCSharpLoad(ptr);
            }

            /// <summary>
            /// Load 128-bits (composed of 4 packed single-precision (32-bit)
            /// floating-point elements) from memory into dst. mem_addr does
            /// not need to be aligned on any particular boundary.
            /// </summary>
			/// <param name="ptr">Pointer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 loadu_ps(void* ptr)
            {
                return GenericCSharpLoad(ptr);
            }

            /// <summary>
            /// Store 128-bits (composed of 4 packed single-precision (32-bit)
            /// floating-point elements) from a into memory.
            /// </summary>
            /// <remarks>
            /// Burst will always generate unaligned stores.
            /// </remarks>
			/// <param name="ptr">Pointer</param>
			/// <param name="val">Value vector</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static void store_ps(void* ptr, v128 val)
            {
                GenericCSharpStore(ptr, val);
            }

            /// <summary>
            /// Store 128-bits (composed of 4 packed single-precision (32-bit)
            /// floating-point elements) from a into memory. mem_addr does not
            /// need to be aligned on any particular boundary.
            /// </summary>
			/// <param name="ptr">Pointer</param>
			/// <param name="val">Value vector</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static void storeu_ps(void* ptr, v128 val)
            {
                GenericCSharpStore(ptr, val);
            }

            /// <summary>
            /// Store 128-bits (composed of 4 packed single-precision (32-bit) floating-point elements) from "a" into memory using a non-temporal memory hint. "mem_addr" must be aligned on a 16-byte boundary or a general-protection exception will be generated.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static void stream_ps(void* mem_addr, v128 a)
            {
                GenericCSharpStore(mem_addr, a);
            }

            // _mm_cvtsi32_ss
            /// <summary> Convert the 32-bit integer "b" to a single-precision (32-bit) floating-point element, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">32-bit integer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtsi32_ss(v128 a, int b)
            {
                v128 dst = a;
                dst.Float0 = b;
                return dst;
            }

            // _mm_cvtsi64_ss
            /// <summary> Convert the 64-bit integer "b" to a single-precision (32-bit) floating-point element, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">64-bit integer</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtsi64_ss(v128 a, long b)
            {
                v128 dst = a;
                dst.Float0 = b;
                return dst;
            }

            // _mm_add_ss
            /// <summary> Add the lower single-precision (32-bit) floating-point element in "a" and "b", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 add_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = dst.Float0 + b.Float0;
                return dst;
            }

            // _mm_add_ps
            /// <summary> Add packed single-precision (32-bit) floating-point elements in "a" and "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 add_ps(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 += b.Float0;
                dst.Float1 += b.Float1;
                dst.Float2 += b.Float2;
                dst.Float3 += b.Float3;
                return dst;
            }

            // _mm_sub_ss
            /// <summary> Subtract the lower single-precision (32-bit) floating-point element in "b" from the lower single-precision (32-bit) floating-point element in "a", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sub_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = a.Float0 - b.Float0;
                return dst;
            }

            // _mm_sub_ps
            /// <summary> Subtract packed single-precision (32-bit) floating-point elements in "b" from packed single-precision (32-bit) floating-point elements in "a", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sub_ps(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 -= b.Float0;
                dst.Float1 -= b.Float1;
                dst.Float2 -= b.Float2;
                dst.Float3 -= b.Float3;
                return dst;
            }

            // _mm_mul_ss
            /// <summary> Multiply the lower single-precision (32-bit) floating-point element in "a" and "b", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mul_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = a.Float0 * b.Float0;
                return dst;
            }

            // _mm_mul_ps
            /// <summary> Multiply packed single-precision (32-bit) floating-point elements in "a" and "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mul_ps(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 *= b.Float0;
                dst.Float1 *= b.Float1;
                dst.Float2 *= b.Float2;
                dst.Float3 *= b.Float3;
                return dst;
            }

            // _mm_div_ss
            /// <summary> Divide the lower single-precision (32-bit) floating-point element in "a" by the lower single-precision (32-bit) floating-point element in "b", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 div_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = a.Float0 / b.Float0;
                return dst;
            }

            // _mm_div_ps
            /// <summary> Divide packed single-precision (32-bit) floating-point elements in "a" by packed elements in "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 div_ps(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 /= b.Float0;
                dst.Float1 /= b.Float1;
                dst.Float2 /= b.Float2;
                dst.Float3 /= b.Float3;
                return dst;
            }

            // _mm_sqrt_ss
            /// <summary> Compute the square root of the lower single-precision (32-bit) floating-point element in "a", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sqrt_ss(v128 a)
            {
                v128 dst = a;
                dst.Float0 = (float)Math.Sqrt(a.Float0);
                return dst;
            }

            // _mm_sqrt_ps
            /// <summary> Compute the square root of packed single-precision (32-bit) floating-point elements in "a", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sqrt_ps(v128 a)
            {
                v128 dst = default(v128);
                dst.Float0 = (float)Math.Sqrt(a.Float0);
                dst.Float1 = (float)Math.Sqrt(a.Float1);
                dst.Float2 = (float)Math.Sqrt(a.Float2);
                dst.Float3 = (float)Math.Sqrt(a.Float3);
                return dst;
            }

            // _mm_rcp_ss
            /// <summary> Compute the approximate reciprocal of the lower single-precision (32-bit) floating-point element in "a", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". The maximum relative error for this approximation is less than 1.5*2^-12. </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 rcp_ss(v128 a)
            {
                v128 dst = a;
                dst.Float0 = 1.0f / a.Float0;
                return dst;
            }

            // _mm_rcp_ps
            /// <summary> Compute the approximate reciprocal of packed single-precision (32-bit) floating-point elements in "a", and store the results in "dst". The maximum relative error for this approximation is less than 1.5*2^-12. </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 rcp_ps(v128 a)
            {
                v128 dst = default(v128);
                dst.Float0 = 1.0f / a.Float0;
                dst.Float1 = 1.0f / a.Float1;
                dst.Float2 = 1.0f / a.Float2;
                dst.Float3 = 1.0f / a.Float3;
                return dst;
            }

            // _mm_rsqrt_ss
            /// <summary> Compute the approximate reciprocal square root of the lower single-precision (32-bit) floating-point element in "a", store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". The maximum relative error for this approximation is less than 1.5*2^-12. </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 rsqrt_ss(v128 a)
            {
                v128 dst = a;
                dst.Float0 = 1.0f / (float)Math.Sqrt(a.Float0);
                return dst;
            }

            // _mm_rsqrt_ps
            /// <summary> Compute the approximate reciprocal square root of packed single-precision (32-bit) floating-point elements in "a", and store the results in "dst". The maximum relative error for this approximation is less than 1.5*2^-12. </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 rsqrt_ps(v128 a)
            {
                v128 dst = default(v128);
                dst.Float0 = 1.0f / (float)Math.Sqrt(a.Float0);
                dst.Float1 = 1.0f / (float)Math.Sqrt(a.Float1);
                dst.Float2 = 1.0f / (float)Math.Sqrt(a.Float2);
                dst.Float3 = 1.0f / (float)Math.Sqrt(a.Float3);
                return dst;
            }

            // _mm_min_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b", store the minimum value in the lower element of "dst", and copy the upper element from "a" to the upper element of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 min_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = Math.Min(a.Float0, b.Float0);
                return dst;
            }

            // _mm_min_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b", and store packed minimum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 min_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = Math.Min(a.Float0, b.Float0);
                dst.Float1 = Math.Min(a.Float1, b.Float1);
                dst.Float2 = Math.Min(a.Float2, b.Float2);
                dst.Float3 = Math.Min(a.Float3, b.Float3);
                return dst;
            }

            // _mm_max_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b", store the maximum value in the lower element of "dst", and copy the upper element from "a" to the upper element of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 max_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = Math.Max(a.Float0, b.Float0);
                return dst;
            }

            // _mm_max_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b", and store packed maximum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 max_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = Math.Max(a.Float0, b.Float0);
                dst.Float1 = Math.Max(a.Float1, b.Float1);
                dst.Float2 = Math.Max(a.Float2, b.Float2);
                dst.Float3 = Math.Max(a.Float3, b.Float3);
                return dst;
            }

            // _mm_and_ps
            /// <summary> Compute the bitwise AND of packed single-precision (32-bit) floating-point elements in "a" and "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 and_ps(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 &= b.UInt0;
                dst.UInt1 &= b.UInt1;
                dst.UInt2 &= b.UInt2;
                dst.UInt3 &= b.UInt3;
                return dst;
            }

            // _mm_andnot_ps
            /// <summary> Compute the bitwise NOT of packed single-precision (32-bit) floating-point elements in "a" and then AND with "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 andnot_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = (~a.UInt0) & b.UInt0;
                dst.UInt1 = (~a.UInt1) & b.UInt1;
                dst.UInt2 = (~a.UInt2) & b.UInt2;
                dst.UInt3 = (~a.UInt3) & b.UInt3;
                return dst;
            }

            // _mm_or_ps
            /// <summary> Compute the bitwise OR of packed single-precision (32-bit) floating-point elements in "a" and "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 or_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = a.UInt0 | b.UInt0;
                dst.UInt1 = a.UInt1 | b.UInt1;
                dst.UInt2 = a.UInt2 | b.UInt2;
                dst.UInt3 = a.UInt3 | b.UInt3;
                return dst;
            }

            // _mm_xor_ps
            /// <summary> Compute the bitwise XOR of packed single-precision (32-bit) floating-point elements in "a" and "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 xor_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = a.UInt0 ^ b.UInt0;
                dst.UInt1 = a.UInt1 ^ b.UInt1;
                dst.UInt2 = a.UInt2 ^ b.UInt2;
                dst.UInt3 = a.UInt3 ^ b.UInt3;
                return dst;
            }

            // _mm_cmpeq_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for equality, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpeq_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = a.Float0 == b.Float0 ? ~0u : 0;
                return dst;
            }

            // _mm_cmpeq_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for equality, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpeq_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = a.Float0 == b.Float0 ? ~0u : 0;
                dst.UInt1 = a.Float1 == b.Float1 ? ~0u : 0;
                dst.UInt2 = a.Float2 == b.Float2 ? ~0u : 0;
                dst.UInt3 = a.Float3 == b.Float3 ? ~0u : 0;
                return dst;
            }

            // _mm_cmplt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for less-than, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmplt_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = a.Float0 < b.Float0 ? ~0u : 0u;
                return dst;
            }

            // _mm_cmplt_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for less-than, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmplt_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = a.Float0 < b.Float0 ? ~0u : 0u;
                dst.UInt1 = a.Float1 < b.Float1 ? ~0u : 0u;
                dst.UInt2 = a.Float2 < b.Float2 ? ~0u : 0u;
                dst.UInt3 = a.Float3 < b.Float3 ? ~0u : 0u;
                return dst;
            }

            // _mm_cmple_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for less-than-or-equal, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmple_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = a.Float0 <= b.Float0 ? ~0u : 0;
                return dst;
            }

            // _mm_cmple_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for less-than-or-equal, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmple_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = a.Float0 <= b.Float0 ? ~0u : 0u;
                dst.UInt1 = a.Float1 <= b.Float1 ? ~0u : 0u;
                dst.UInt2 = a.Float2 <= b.Float2 ? ~0u : 0u;
                dst.UInt3 = a.Float3 <= b.Float3 ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpgt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for greater-than, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpgt_ss(v128 a, v128 b)
            {
                return cmplt_ss(b, a);
            }

            // _mm_cmpgt_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for greater-than, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpgt_ps(v128 a, v128 b)
            {
                return cmplt_ps(b, a);
            }

            // _mm_cmpge_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for greater-than-or-equal, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpge_ss(v128 a, v128 b)
            {
                return cmple_ss(b, a);
            }

            // _mm_cmpge_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for greater-than-or-equal, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpge_ps(v128 a, v128 b)
            {
                return cmple_ps(b, a);
            }

            // _mm_cmpneq_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for not-equal, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpneq_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = a.Float0 != b.Float0 ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpneq_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for not-equal, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpneq_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = a.Float0 != b.Float0 ? ~0u : 0u;
                dst.UInt1 = a.Float1 != b.Float1 ? ~0u : 0u;
                dst.UInt2 = a.Float2 != b.Float2 ? ~0u : 0u;
                dst.UInt3 = a.Float3 != b.Float3 ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpnlt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for not-less-than, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpnlt_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = !(a.Float0 < b.Float0) ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpnlt_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for not-less-than, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpnlt_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = !(a.Float0 < b.Float0) ? ~0u : 0u;
                dst.UInt1 = !(a.Float1 < b.Float1) ? ~0u : 0u;
                dst.UInt2 = !(a.Float2 < b.Float2) ? ~0u : 0u;
                dst.UInt3 = !(a.Float3 < b.Float3) ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpnle_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for not-less-than-or-equal, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpnle_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = !(a.Float0 <= b.Float0) ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpnle_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for not-less-than-or-equal, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpnle_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = !(a.Float0 <= b.Float0) ? ~0u : 0u;
                dst.UInt1 = !(a.Float1 <= b.Float1) ? ~0u : 0u;
                dst.UInt2 = !(a.Float2 <= b.Float2) ? ~0u : 0u;
                dst.UInt3 = !(a.Float3 <= b.Float3) ? ~0u : 0u;
                return dst;
            }

            // _mm_cmpngt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for not-greater-than, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpngt_ss(v128 a, v128 b)
            {
                return cmpnlt_ss(b, a);
            }

            // _mm_cmpngt_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for not-greater-than, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpngt_ps(v128 a, v128 b)
            {
                return cmpnlt_ps(b, a);
            }

            // _mm_cmpnge_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" for not-greater-than-or-equal, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpnge_ss(v128 a, v128 b)
            {
                return cmpnle_ss(b, a);
            }

            // _mm_cmpnge_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" for not-greater-than-or-equal, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 cmpnge_ps(v128 a, v128 b)
            {
                return cmpnle_ps(b, a);
            }

            // _mm_cmpord_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" to see if neither is NaN, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpord_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = IsNaN(a.UInt0) || IsNaN(b.UInt0) ? 0 : ~0u;
                return dst;
            }

            // _mm_cmpord_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" to see if neither is NaN, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpord_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = IsNaN(a.UInt0) || IsNaN(b.UInt0) ? 0 : ~0u;
                dst.UInt1 = IsNaN(a.UInt1) || IsNaN(b.UInt1) ? 0 : ~0u;
                dst.UInt2 = IsNaN(a.UInt2) || IsNaN(b.UInt2) ? 0 : ~0u;
                dst.UInt3 = IsNaN(a.UInt3) || IsNaN(b.UInt3) ? 0 : ~0u;
                return dst;
            }

            // _mm_cmpunord_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point elements in "a" and "b" to see if either is NaN, store the result in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpunord_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.UInt0 = IsNaN(a.UInt0) || IsNaN(b.UInt0) ? ~0u : 0;
                return dst;
            }

            // _mm_cmpunord_ps
            /// <summary> Compare packed single-precision (32-bit) floating-point elements in "a" and "b" to see if either is NaN, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpunord_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UInt0 = IsNaN(a.UInt0) || IsNaN(b.UInt0) ? ~0u : 0;
                dst.UInt1 = IsNaN(a.UInt1) || IsNaN(b.UInt1) ? ~0u : 0;
                dst.UInt2 = IsNaN(a.UInt2) || IsNaN(b.UInt2) ? ~0u : 0;
                dst.UInt3 = IsNaN(a.UInt3) || IsNaN(b.UInt3) ? ~0u : 0;
                return dst;
            }

            // _mm_comieq_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for equality, and return the boolean result (0 or 1). </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int comieq_ss(v128 a, v128 b)
            {
                return a.Float0 == b.Float0 ? 1 : 0;
            }

            // _mm_comilt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for less-than, and return the boolean result (0 or 1). </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int comilt_ss(v128 a, v128 b)
            {
                return a.Float0 < b.Float0 ? 1 : 0;
            }

            // _mm_comile_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for less-than-or-equal, and return the boolean result (0 or 1). </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int comile_ss(v128 a, v128 b)
            {
                return a.Float0 <= b.Float0 ? 1 : 0;
            }

            // _mm_comigt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for greater-than, and return the boolean result (0 or 1). </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int comigt_ss(v128 a, v128 b)
            {
                return a.Float0 > b.Float0 ? 1 : 0;
            }

            // _mm_comige_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for greater-than-or-equal, and return the boolean result (0 or 1). </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int comige_ss(v128 a, v128 b)
            {
                return a.Float0 >= b.Float0 ? 1 : 0;
            }

            // _mm_comineq_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for not-equal, and return the boolean result (0 or 1). </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int comineq_ss(v128 a, v128 b)
            {
                return a.Float0 != b.Float0 ? 1 : 0;
            }

            // _mm_ucomieq_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for equality, and return the boolean result (0 or 1). This instruction will not signal an exception for QNaNs. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int ucomieq_ss(v128 a, v128 b)
            {
                return a.Float0 == b.Float0 ? 1 : 0;
            }

            // _mm_ucomilt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for less-than, and return the boolean result (0 or 1). This instruction will not signal an exception for QNaNs. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int ucomilt_ss(v128 a, v128 b)
            {
                return a.Float0 < b.Float0 ? 1 : 0;
            }

            // _mm_ucomile_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for less-than-or-equal, and return the boolean result (0 or 1). This instruction will not signal an exception for QNaNs. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int ucomile_ss(v128 a, v128 b)
            {
                return a.Float0 <= b.Float0 ? 1 : 0;
            }

            // _mm_ucomigt_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for greater-than, and return the boolean result (0 or 1). This instruction will not signal an exception for QNaNs. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int ucomigt_ss(v128 a, v128 b)
            {
                return a.Float0 > b.Float0 ? 1 : 0;
            }

            // _mm_ucomige_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for greater-than-or-equal, and return the boolean result (0 or 1). This instruction will not signal an exception for QNaNs. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int ucomige_ss(v128 a, v128 b)
            {
                return a.Float0 >= b.Float0 ? 1 : 0;
            }

            // _mm_ucomineq_ss
            /// <summary> Compare the lower single-precision (32-bit) floating-point element in "a" and "b" for not-equal, and return the boolean result (0 or 1). This instruction will not signal an exception for QNaNs. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int ucomineq_ss(v128 a, v128 b)
            {
                return a.Float0 != b.Float0 ? 1 : 0;
            }

            // _mm_cvtss_si32
            /// <summary> Convert the lower single-precision (32-bit) floating-point element in "a" to a 32-bit integer, and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static int cvtss_si32(v128 a)
            {
                return cvt_ss2si(a);
            }

            // _mm_cvt_ss2si
            /// <summary>
            /// Convert the lower single-precision (32-bit) floating-point element in "a" to a 32-bit integer, and store the result in "dst".
            /// Follows standard of rounding to nearest, and for midpoint rounding it rounds to even.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int cvt_ss2si(v128 a)
            {
                return (int)Math.Round(a.Float0, MidpointRounding.ToEven);
            }

            // _mm_cvtss_si64
            /// <summary>
            /// Convert the lower single-precision (32-bit) floating-point element in "a" to a 64-bit integer, and store the result in "dst".
            /// Follows standard of rounding to nearest, and for midpoint rounding it rounds to even.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static long cvtss_si64(v128 a)
            {
                return (long)Math.Round(a.Float0, MidpointRounding.ToEven);
            }

            // _mm_cvtss_f32
            /// <summary> Copy the lower single-precision (32-bit) floating-point element of "a" to "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>32-bit floating point element</returns>
            [DebuggerStepThrough]
            public static float cvtss_f32(v128 a)
            {
                return a.Float0;
            }

            // _mm_cvttss_si32
            /// <summary> Convert the lower single-precision (32-bit) floating-point element in "a" to a 32-bit integer with truncation, and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            public static int cvttss_si32(v128 a)
            {
                using (var csr = new RoundingScope(MXCSRBits.RoundTowardZero))
                {
                    return (int)a.Float0;
                }
            }

            // _mm_cvtt_ss2si
            /// <summary> Convert the lower single-precision (32-bit) floating-point element in "a" to a 32-bit integer with truncation, and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>32-bit integer</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static int cvtt_ss2si(v128 a)
            {
                return cvttss_si32(a);
            }

            // _mm_cvttss_si64
            /// <summary> Convert the lower single-precision (32-bit) floating-point element in "a" to a 64-bit integer with truncation, and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static long cvttss_si64(v128 a)
            {
                using (var csr = new RoundingScope(MXCSRBits.RoundTowardZero))
                {
                    return (long)a.Float0;
                }
            }

            // _mm_set_ss
            /// <summary> Copy single-precision (32-bit) floating-point element "a" to the lower element of "dst", and zero the upper 3 elements. </summary>
			/// <param name="a">Floating point element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 set_ss(float a)
            {
                return new v128(a, 0.0f, 0.0f, 0.0f);
            }

            // _mm_set1_ps
            /// <summary> Broadcast single-precision (32-bit) floating-point value "a" to all elements of "dst". </summary>
			/// <param name="a">Floating point element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 set1_ps(float a)
            {
                return new v128(a, a, a, a);
            }

            // _mm_set_ps1
            /// <summary> Broadcast single-precision (32-bit) floating-point value "a" to all elements of "dst". </summary>
			/// <param name="a">Floating point element</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static v128 set_ps1(float a)
            {
                return set1_ps(a);
            }

            // _mm_set_ps
            /// <summary> Set packed single-precision (32-bit) floating-point elements in "dst" with the supplied values. </summary>
			/// <param name="e3">Floating point element 3</param>
			/// <param name="e2">Floating point element 2</param>
			/// <param name="e1">Floating point element 1</param>
			/// <param name="e0">Floating point element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 set_ps(float e3, float e2, float e1, float e0)
            {
                return new v128(e0, e1, e2, e3);
            }

            // _mm_setr_ps
            /// <summary> Set packed single-precision (32-bit) floating-point elements in "dst" with the supplied values in reverse order. </summary>
			/// <param name="e3">Floating point element 3</param>
			/// <param name="e2">Floating point element 2</param>
			/// <param name="e1">Floating point element 1</param>
			/// <param name="e0">Floating point element 0</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 setr_ps(float e3, float e2, float e1, float e0)
            {
                return new v128(e3, e2, e1, e0);
            }

            // _mm_move_ss
            /// <summary> Move the lower single-precision (32-bit) floating-point element from "b" to the lower element of "dst", and copy the upper 3 elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 move_ss(v128 a, v128 b)
            {
                v128 dst = a;
                dst.Float0 = b.Float0;
                return dst;
            }

            // _MM_SHUFFLE macro
            /// <summary>
            /// Return a shuffle immediate suitable for use with shuffle_ps and similar instructions.
            /// </summary>
			/// <param name="d">Integer d</param>
			/// <param name="c">Integer c</param>
			/// <param name="b">Integer b</param>
			/// <param name="a">Integer a</param>
			/// <returns>Shuffle suitable for use with shuffle_ps</returns>
            public static int SHUFFLE(int d, int c, int b, int a)
            {
                return ((a & 3)) | ((b & 3) << 2) | ((c & 3) << 4) | ((d & 3) << 6);
            }

            // _mm_shuffle_ps
            /// <summary> Shuffle single-precision (32-bit) floating-point elements in "a" using the control in "imm8", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 shuffle_ps(v128 a, v128 b, int imm8)
            {
                v128 dst = default(v128);
                // Use integers, rather than floats, because of a Mono bug.
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                dst.UInt0 = aptr[(imm8 >> 0) & 3];
                dst.UInt1 = aptr[(imm8 >> 2) & 3];
                dst.UInt2 = bptr[(imm8 >> 4) & 3];
                dst.UInt3 = bptr[(imm8 >> 6) & 3];
                return dst;
            }

            // _mm_unpackhi_ps
            /// <summary> Unpack and interleave single-precision (32-bit) floating-point elements from the high half "a" and "b", and store the results in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 unpackhi_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = a.Float2;
                dst.Float1 = b.Float2;
                dst.Float2 = a.Float3;
                dst.Float3 = b.Float3;
                return dst;
            }

            // _mm_unpacklo_ps
            /// <summary> Unpack and interleave single-precision (32-bit) floating-point elements from the low half of "a" and "b", and store the results in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 unpacklo_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = a.Float0;
                dst.Float1 = b.Float0;
                dst.Float2 = a.Float1;
                dst.Float3 = b.Float1;
                return dst;
            }

            // _mm_movehl_ps
            /// <summary> Move the upper 2 single-precision (32-bit) floating-point elements from "b" to the lower 2 elements of "dst", and copy the upper 2 elements from "a" to the upper 2 elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 movehl_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = b.Float2;
                dst.Float1 = b.Float3;
                dst.Float2 = a.Float2;
                dst.Float3 = a.Float3;
                return dst;
            }

            // _mm_movelh_ps
            /// <summary> Move the lower 2 single-precision (32-bit) floating-point elements from "b" to the upper 2 elements of "dst", and copy the lower 2 elements from "a" to the lower 2 elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 movelh_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = a.Float0;
                dst.Float1 = a.Float1;
                dst.Float2 = b.Float0;
                dst.Float3 = b.Float1;
                return dst;
            }

            // _mm_movemask_ps
            /// <summary> Set each bit of mask "dst" based on the most significant bit of the corresponding packed single-precision (32-bit) floating-point element in "a". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int movemask_ps(v128 a)
            {
                int dst = 0;
                if ((a.UInt0 & 0x80000000) != 0) dst |= 1;
                if ((a.UInt1 & 0x80000000) != 0) dst |= 2;
                if ((a.UInt2 & 0x80000000) != 0) dst |= 4;
                if ((a.UInt3 & 0x80000000) != 0) dst |= 8;
                return dst;
            }

            /// <summary>
            /// Transposes a 4x4 matrix of single precision floating point values (_MM_TRANSPOSE4_PS).
            /// </summary>
            /// <remarks>
            /// Arguments row0, row1, row2, and row3 are __m128    
            /// values whose elements form the corresponding rows
            /// of a 4x4 matrix.  The matrix transpose is returned
            /// in arguments row0, row1, row2, and row3 where row0 
            /// now holds column 0 of the original matrix, row1 now 
            /// holds column 1 of the original matrix, etc.          
            /// </remarks>
			/// <param name="row0">__m128 value on corresponding row</param>
			/// <param name="row1">__m128 value on corresponding row</param>
			/// <param name="row2">__m128 value on corresponding row</param>
			/// <param name="row3">__m128 value on corresponding row</param>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE2)]
            public static void TRANSPOSE4_PS(ref v128 row0, ref v128 row1, ref v128 row2, ref v128 row3)
            {
                v128 _Tmp3, _Tmp2, _Tmp1, _Tmp0;

                _Tmp0 = shuffle_ps((row0), (row1), 0x44);
                _Tmp2 = shuffle_ps((row0), (row1), 0xEE);
                _Tmp1 = shuffle_ps((row2), (row3), 0x44);
                _Tmp3 = shuffle_ps((row2), (row3), 0xEE);

                row0 = shuffle_ps(_Tmp0, _Tmp1, 0x88);
                row1 = shuffle_ps(_Tmp0, _Tmp1, 0xDD);
                row2 = shuffle_ps(_Tmp2, _Tmp3, 0x88);
                row3 = shuffle_ps(_Tmp2, _Tmp3, 0xDD);
            }

            /// <summary>
            /// Return vector of type v128 with all elements set to zero.
            /// </summary>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 setzero_ps()
            {
                return default;
            }

            /// <summary>
            /// Load unaligned 16-bit integer from memory into the first element of dst.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 loadu_si16(void* mem_addr)
            {
                return new v128(*(short*)mem_addr, 0, 0, 0, 0, 0, 0, 0);
            }

            /// <summary>
            /// Store 16-bit integer from the first element of a into memory.
            /// mem_addr does not need to be aligned on any particular
            /// boundary.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="a">Vector a</param>
            public static void storeu_si16(void* mem_addr, v128 a)
            {
                *(short*)mem_addr = a.SShort0;
            }

            /// <summary>
            /// Load unaligned 64-bit integer from memory into the first element of dst.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 loadu_si64(void* mem_addr)
            {
                return new v128(*(long*)mem_addr, 0);
            }

            /// <summary>
            /// Store 64-bit integer from the first element of a into memory.
            /// mem_addr does not need to be aligned on any particular
            /// boundary.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <param name="a">Vector a</param>
            [DebuggerStepThrough]
            public static void storeu_si64(void* mem_addr, v128 a)
            {
                *(long*)mem_addr = a.SLong0;
            }
        }
    }
}
