// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// FMA intrinsics
        /// </summary>
        public static class Fma
        {
            /// <summary>
            /// Evaluates to true at compile time if FMA intrinsics are supported.
            ///
            /// Burst ties FMA support to AVX2 support to simplify feature sets to support.
            /// </summary>
            public static bool IsFmaSupported { get { return Avx2.IsAvx2Supported; } }

            [DebuggerStepThrough]
            private static float FmaHelper(float a, float b, float c)
            {
                return (float)((((double)a) * b) + c);
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct Union
            {
                [FieldOffset(0)]
                public float f;

                [FieldOffset(0)]
                public uint u;
            }

            [DebuggerStepThrough]
            private static float FnmaHelper(float a, float b, float c)
            {
                return FmaHelper(-a, b, c);
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, add the intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmadd213pd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>>
            [DebuggerStepThrough]
            public static v128 fmadd_pd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, add the intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmadd213pd ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmadd_pd(v256 a, v256 b, v256 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, add the intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmadd213ps xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmadd_ps(v128 a, v128 b, v128 c)
            {
                return new v128(FmaHelper(a.Float0, b.Float0, c.Float0),
                                FmaHelper(a.Float1, b.Float1, c.Float1),
                                FmaHelper(a.Float2, b.Float2, c.Float2),
                                FmaHelper(a.Float3, b.Float3, c.Float3));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, add the intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmadd213ps ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmadd_ps(v256 a, v256 b, v256 c)
            {
                return new v256(FmaHelper(a.Float0, b.Float0, c.Float0),
                                FmaHelper(a.Float1, b.Float1, c.Float1),
                                FmaHelper(a.Float2, b.Float2, c.Float2),
                                FmaHelper(a.Float3, b.Float3, c.Float3),
                                FmaHelper(a.Float4, b.Float4, c.Float4),
                                FmaHelper(a.Float5, b.Float5, c.Float5),
                                FmaHelper(a.Float6, b.Float6, c.Float6),
                                FmaHelper(a.Float7, b.Float7, c.Float7));
            }

            /// <summary>
            /// Multiply the lower double-precision (64-bit) floating-point elements in a and b, and add the intermediate result to the lower element in c. Store the result in the lower element of dst, and copy the upper element from a to the upper element of dst.
            /// </summary>
            /// <remarks>
            /// **** vfmadd213sd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmadd_sd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply the lower single-precision (32-bit) floating-point elements in a and b, and add the intermediate result to the lower element in c. Store the result in the lower element of dst, and copy the upper 3 packed elements from a to the upper elements of dst.
            /// </summary>
            /// <remarks>
            /// **** vfmadd213ss xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmadd_ss(v128 a, v128 b, v128 c)
            {
                var result = a;
                result.Float0 = FmaHelper(a.Float0, b.Float0, c.Float0);
                return result;
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, alternatively add and subtract packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmaddsub213pd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmaddsub_pd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, alternatively add and subtract packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmaddsub213pd ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmaddsub_pd(v256 a, v256 b, v256 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, alternatively add and subtract packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmaddsub213ps xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmaddsub_ps(v128 a, v128 b, v128 c)
            {
                return new v128(FmaHelper(a.Float0, b.Float0, -c.Float0),
                                FmaHelper(a.Float1, b.Float1, c.Float1),
                                FmaHelper(a.Float2, b.Float2, -c.Float2),
                                FmaHelper(a.Float3, b.Float3, c.Float3));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, alternatively add and subtract packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmaddsub213ps ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmaddsub_ps(v256 a, v256 b, v256 c)
            {
                return new v256(FmaHelper(a.Float0, b.Float0, -c.Float0),
                                FmaHelper(a.Float1, b.Float1, c.Float1),
                                FmaHelper(a.Float2, b.Float2, -c.Float2),
                                FmaHelper(a.Float3, b.Float3, c.Float3),
                                FmaHelper(a.Float4, b.Float4, -c.Float4),
                                FmaHelper(a.Float5, b.Float5, c.Float5),
                                FmaHelper(a.Float6, b.Float6, -c.Float6),
                                FmaHelper(a.Float7, b.Float7, c.Float7));
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, subtract packed elements in c from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsub213pd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmsub_pd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, subtract packed elements in c from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsub213pd ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmsub_pd(v256 a, v256 b, v256 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, subtract packed elements in c from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsub213ps xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmsub_ps(v128 a, v128 b, v128 c)
            {
                return new v128(FmaHelper(a.Float0, b.Float0, -c.Float0),
                                FmaHelper(a.Float1, b.Float1, -c.Float1),
                                FmaHelper(a.Float2, b.Float2, -c.Float2),
                                FmaHelper(a.Float3, b.Float3, -c.Float3));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, subtract packed elements in c from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsub213ps ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmsub_ps(v256 a, v256 b, v256 c)
            {
                return new v256(FmaHelper(a.Float0, b.Float0, -c.Float0),
                                FmaHelper(a.Float1, b.Float1, -c.Float1),
                                FmaHelper(a.Float2, b.Float2, -c.Float2),
                                FmaHelper(a.Float3, b.Float3, -c.Float3),
                                FmaHelper(a.Float4, b.Float4, -c.Float4),
                                FmaHelper(a.Float5, b.Float5, -c.Float5),
                                FmaHelper(a.Float6, b.Float6, -c.Float6),
                                FmaHelper(a.Float7, b.Float7, -c.Float7));
            }

            /// <summary>
            /// Multiply the lower double-precision(64-bit) floating-point elements in a and b, and subtract the lower element in c from the intermediate result.Store the result in the lower element of dst, and copy the upper element from a to the upper element of dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsub213sd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmsub_sd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply the lower single-precision (32-bit) floating-point elements in a and b, and subtract the lower element in c from the intermediate result. Store the result in the lower element of dst, and copy the upper 3 packed elements from a to the upper elements of dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsub213ss xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmsub_ss(v128 a, v128 b, v128 c)
            {
                var result = a;
                result.Float0 = FmaHelper(a.Float0, b.Float0, -c.Float0);
                return result;
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, alternatively subtract and add packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsubadd213pd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmsubadd_pd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, alternatively subtract and add packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsubadd213pd ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmsubadd_pd(v256 a, v256 b, v256 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, alternatively subtract and add packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsubadd213ps xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fmsubadd_ps(v128 a, v128 b, v128 c)
            {
                return new v128(FmaHelper(a.Float0, b.Float0, c.Float0),
                                FmaHelper(a.Float1, b.Float1, -c.Float1),
                                FmaHelper(a.Float2, b.Float2, c.Float2),
                                FmaHelper(a.Float3, b.Float3, -c.Float3));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, alternatively subtract and add packed elements in c to/from the intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfmsubadd213ps ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fmsubadd_ps(v256 a, v256 b, v256 c)
            {
                return new v256(FmaHelper(a.Float0, b.Float0, c.Float0),
                                FmaHelper(a.Float1, b.Float1, -c.Float1),
                                FmaHelper(a.Float2, b.Float2, c.Float2),
                                FmaHelper(a.Float3, b.Float3, -c.Float3),
                                FmaHelper(a.Float4, b.Float4, c.Float4),
                                FmaHelper(a.Float5, b.Float5, -c.Float5),
                                FmaHelper(a.Float6, b.Float6, c.Float6),
                                FmaHelper(a.Float7, b.Float7, -c.Float7));
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, add the negated intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmadd213pd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmadd_pd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, add the negated intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmadd213pd ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fnmadd_pd(v256 a, v256 b, v256 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, add the negated intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmadd213ps xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmadd_ps(v128 a, v128 b, v128 c)
            {
                return new v128(FnmaHelper(a.Float0, b.Float0, c.Float0),
                                FnmaHelper(a.Float1, b.Float1, c.Float1),
                                FnmaHelper(a.Float2, b.Float2, c.Float2),
                                FnmaHelper(a.Float3, b.Float3, c.Float3));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, add the negated intermediate result to packed elements in c, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmadd213ps ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fnmadd_ps(v256 a, v256 b, v256 c)
            {
                return new v256(FnmaHelper(a.Float0, b.Float0, c.Float0),
                                FnmaHelper(a.Float1, b.Float1, c.Float1),
                                FnmaHelper(a.Float2, b.Float2, c.Float2),
                                FnmaHelper(a.Float3, b.Float3, c.Float3),
                                FnmaHelper(a.Float4, b.Float4, c.Float4),
                                FnmaHelper(a.Float5, b.Float5, c.Float5),
                                FnmaHelper(a.Float6, b.Float6, c.Float6),
                                FnmaHelper(a.Float7, b.Float7, c.Float7));
            }

            /// <summary>
            /// Multiply the lower double-precision (64-bit) floating-point elements in a and b, and add the negated intermediate result to the lower element in c. Store the result in the lower element of dst, and copy the upper element from a to the upper element of dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmadd213sd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmadd_sd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply the lower single-precision (32-bit) floating-point elements in a and b, and add the negated intermediate result to the lower element in c. Store the result in the lower element of dst, and copy the upper 3 packed elements from a to the upper elements of dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmadd213ss xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmadd_ss(v128 a, v128 b, v128 c)
            {
                var result = a;
                result.Float0 = FnmaHelper(a.Float0, b.Float0, c.Float0);
                return result;
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, subtract packed elements in c from the negated intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmsub213pd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmsub_pd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed double-precision (64-bit) floating-point elements in a and b, subtract packed elements in c from the negated intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmsub213pd ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fnmsub_pd(v256 a, v256 b, v256 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, subtract packed elements in c from the negated intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmsub213ps xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmsub_ps(v128 a, v128 b, v128 c)
            {
                return new v128(FnmaHelper(a.Float0, b.Float0, -c.Float0),
                                FnmaHelper(a.Float1, b.Float1, -c.Float1),
                                FnmaHelper(a.Float2, b.Float2, -c.Float2),
                                FnmaHelper(a.Float3, b.Float3, -c.Float3));
            }

            /// <summary>
            /// Multiply packed single-precision (32-bit) floating-point elements in a and b, subtract packed elements in c from the negated intermediate result, and store the results in dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmsub213ps ymm, ymm, ymm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v256 mm256_fnmsub_ps(v256 a, v256 b, v256 c)
            {
                return new v256(FnmaHelper(a.Float0, b.Float0, -c.Float0),
                                FnmaHelper(a.Float1, b.Float1, -c.Float1),
                                FnmaHelper(a.Float2, b.Float2, -c.Float2),
                                FnmaHelper(a.Float3, b.Float3, -c.Float3),
                                FnmaHelper(a.Float4, b.Float4, -c.Float4),
                                FnmaHelper(a.Float5, b.Float5, -c.Float5),
                                FnmaHelper(a.Float6, b.Float6, -c.Float6),
                                FnmaHelper(a.Float7, b.Float7, -c.Float7));
            }

            /// <summary>
            /// Multiply the lower double-precision(64-bit) floating-point elements in a and b, and subtract the lower element in c from the negated intermediate result.Store the result in the lower element of dst, and copy the upper element from a to the upper element of dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmsub213sd xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmsub_sd(v128 a, v128 b, v128 c)
            {
                throw new Exception("Double-precision FMA not emulated in C#");
            }

            /// <summary>
            /// Multiply the lower single-precision (32-bit) floating-point elements in a and b, and subtract the lower element in c from the negated intermediate result. Store the result in the lower element of dst, and copy the upper 3 packed elements from a to the upper elements of dst.
            /// </summary>
            /// <remarks>
            /// **** vfnmsub213ss xmm, xmm, xmm
            /// </remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="c">Vector c</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 fnmsub_ss(v128 a, v128 b, v128 c)
            {
                var result = a;
                result.Float0 = FnmaHelper(a.Float0, b.Float0, -c.Float0);
                return result;
            }
        }
    }
}
