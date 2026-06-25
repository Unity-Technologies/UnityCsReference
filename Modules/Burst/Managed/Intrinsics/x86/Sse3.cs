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
        /// SSE3 intrinsics
        /// </summary>
        public static class Sse3
        {
            /// <summary>
            /// Evaluates to true at compile time if SSE3 intrinsics are supported.
            /// </summary>
            public static bool IsSse3Supported { get { return false; } }

            // _mm_addsub_ps
            /// <summary> Alternatively add and subtract packed single-precision (32-bit) floating-point elements in "a" to/from packed elements in "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 addsub_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = a.Float0 - b.Float0;
                dst.Float1 = a.Float1 + b.Float1;
                dst.Float2 = a.Float2 - b.Float2;
                dst.Float3 = a.Float3 + b.Float3;
                return dst;
            }

            // _mm_addsub_pd
            /// <summary> Alternatively add and subtract packed double-precision (64-bit) floating-point elements in "a" to/from packed elements in "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 addsub_pd(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Double0 = a.Double0 - b.Double0;
                dst.Double1 = a.Double1 + b.Double1;
                return dst;
            }

            // _mm_hadd_pd
            /// <summary> Horizontally add adjacent pairs of double-precision (64-bit) floating-point elements in "a" and "b", and pack the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hadd_pd(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Double0 = a.Double0 + a.Double1;
                dst.Double1 = b.Double0 + b.Double1;
                return dst;
            }

            // _mm_hadd_ps
            /// <summary> Horizontally add adjacent pairs of single-precision (32-bit) floating-point elements in "a" and "b", and pack the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hadd_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = a.Float0 + a.Float1;
                dst.Float1 = a.Float2 + a.Float3;
                dst.Float2 = b.Float0 + b.Float1;
                dst.Float3 = b.Float2 + b.Float3;
                return dst;
            }

            // _mm_hsub_pd
            /// <summary> Horizontally subtract adjacent pairs of double-precision (64-bit) floating-point elements in "a" and "b", and pack the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hsub_pd(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Double0 = a.Double0 - a.Double1;
                dst.Double1 = b.Double0 - b.Double1;
                return dst;
            }

            // _mm_hsub_ps
            /// <summary> Horizontally add adjacent pairs of single-precision (32-bit) floating-point elements in "a" and "b", and pack the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hsub_ps(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.Float0 = a.Float0 - a.Float1;
                dst.Float1 = a.Float2 - a.Float3;
                dst.Float2 = b.Float0 - b.Float1;
                dst.Float3 = b.Float2 - b.Float3;
                return dst;
            }

            // _mm_movedup_pd
            /// <summary> Duplicate the low double-precision (64-bit) floating-point element from "a", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 movedup_pd(v128 a)
            {
                // Burst IR is fine
                v128 dst = default(v128);
                dst.Double0 = a.Double0;
                dst.Double1 = a.Double0;
                return dst;
            }

            // _mm_movehdup_ps
            /// <summary> Duplicate odd-indexed single-precision (32-bit) floating-point elements from "a", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 movehdup_ps(v128 a)
            {
                // Burst IR is fine
                v128 dst = default(v128);
                dst.Float0 = a.Float1;
                dst.Float1 = a.Float1;
                dst.Float2 = a.Float3;
                dst.Float3 = a.Float3;
                return dst;
            }

            // _mm_moveldup_ps
            /// <summary> Duplicate even-indexed single-precision (32-bit) floating-point elements from "a", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 moveldup_ps(v128 a)
            {
                // Burst IR is fine
                v128 dst = default(v128);
                dst.Float0 = a.Float0;
                dst.Float1 = a.Float0;
                dst.Float2 = a.Float2;
                dst.Float3 = a.Float2;
                return dst;
            }
        }
    }
}
