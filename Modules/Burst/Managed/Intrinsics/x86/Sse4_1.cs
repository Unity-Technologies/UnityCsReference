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
        /// SSE 4.1 intrinsics
        /// </summary>
        public static class Sse4_1
        {
            /// <summary>
            /// Evaluates to true at compile time if SSE 4.1 intrinsics are supported.
            /// </summary>
            public static bool IsSse41Supported { get { return false; } }

            // _mm_stream_load_si128
            /// <summary>
            /// Load 128-bits of integer data from memory into dst using a non-temporal memory hint. mem_addr must be aligned on a 16-byte boundary or a general-protection exception may be generated.
            /// </summary>
			/// <param name="mem_addr">Memory address</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 stream_load_si128(void* mem_addr)
            {
                return GenericCSharpLoad(mem_addr);
            }

            // _mm_blend_pd
            /// <summary> Blend packed double-precision (64-bit) floating-point elements from "a" and "b" using control mask "imm8", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blend_pd(v128 a, v128 b, int imm8)
            {
                int j;
                v128 dst = default(v128);
                double* dptr = &dst.Double0;
                double* aptr = &a.Double0;
                double* bptr = &b.Double0;
                for (j = 0; j <= 1; j++)
                {
                    if (0 != (imm8 & (1 << j)))
                    {
                        dptr[j] = bptr[j];
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_blend_ps
            /// <summary> Blend packed single-precision (32-bit) floating-point elements from "a" and "b" using control mask "imm8", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blend_ps(v128 a, v128 b, int imm8)
            {
                int j;
                v128 dst = default(v128);
                // Use integers, rather than floats, because of a Mono bug.
                uint* dptr = &dst.UInt0;
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (j = 0; j <= 3; j++)
                {
                    if (0 != (imm8 & (1 << j)))
                    {
                        dptr[j] = bptr[j];
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_blendv_pd
            /// <summary> Blend packed double-precision (64-bit) floating-point elements from "a" and "b" using "mask", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blendv_pd(v128 a, v128 b, v128 mask)
            {
                int j;
                v128 dst = default(v128);
                double* dptr = &dst.Double0;
                double* aptr = &a.Double0;
                double* bptr = &b.Double0;
                long* mptr = &mask.SLong0;
                for (j = 0; j <= 1; j++)
                {
                    if (mptr[j] < 0)
                    {
                        dptr[j] = bptr[j];
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_blendv_ps
            /// <summary> Blend packed single-precision (32-bit) floating-point elements from "a" and "b" using "mask", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blendv_ps(v128 a, v128 b, v128 mask)
            {
                int j;
                v128 dst = default(v128);
                // Use integers, rather than floats, because of a Mono bug.
                uint* dptr = &dst.UInt0;
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                int* mptr = &mask.SInt0;
                for (j = 0; j <= 3; j++)
                {
                    if (mptr[j] < 0)
                    {
                        dptr[j] = bptr[j];
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_blendv_epi8
            /// <summary> Blend packed 8-bit integers from "a" and "b" using "mask", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="mask">Mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blendv_epi8(v128 a, v128 b, v128 mask)
            {
                int j;
                v128 dst = default(v128);
                byte* dptr = &dst.Byte0;
                byte* aptr = &a.Byte0;
                byte* bptr = &b.Byte0;
                sbyte* mptr = &mask.SByte0;
                for (j = 0; j <= 15; j++)
                {
                    if (mptr[j] < 0)
                    {
                        dptr[j] = bptr[j];
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }

                return dst;
            }

            // _mm_blend_epi16
            /// <summary> Blend packed 16-bit integers from "a" and "b" using control mask "imm8", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 blend_epi16(v128 a, v128 b, int imm8)
            {
                int j;
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (j = 0; j <= 7; j++)
                {
                    if (0 != ((imm8 >> j) & 1))
                    {
                        dptr[j] = bptr[j];
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_dp_pd
            /// <summary> Conditionally multiply the packed double-precision (64-bit) floating-point elements in "a" and "b" using the high 4 bits in "imm8", sum the four products, and conditionally store the sum in "dst" using the low 4 bits of "imm8". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">High 4 bits in imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 dp_pd(v128 a, v128 b, int imm8)
            {
                double t0 = (imm8 & 0x10) != 0 ? a.Double0 * b.Double0 : 0.0;
                double t1 = (imm8 & 0x20) != 0 ? a.Double1 * b.Double1 : 0.0;
                double sum = t0 + t1;

                v128 dst = default(v128);
                dst.Double0 = (imm8 & 1) != 0 ? sum : 0.0;
                dst.Double1 = (imm8 & 2) != 0 ? sum : 0.0;

                return dst;
            }

            // _mm_dp_ps
            /// <summary> Conditionally multiply the packed single-precision (32-bit) floating-point elements in "a" and "b" using the high 4 bits in "imm8", sum the four products, and conditionally store the sum in "dst" using the low 4 bits of "imm8". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">High 4 bits in imm8</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 dp_ps(v128 a, v128 b, int imm8)
            {
                float t0 = (imm8 & 0x10) != 0 ? a.Float0 * b.Float0 : 0.0f;
                float t1 = (imm8 & 0x20) != 0 ? a.Float1 * b.Float1 : 0.0f;
                float t2 = (imm8 & 0x40) != 0 ? a.Float2 * b.Float2 : 0.0f;
                float t3 = (imm8 & 0x80) != 0 ? a.Float3 * b.Float3 : 0.0f;
                float sum = t0 + t1 + t2 + t3;

                v128 dst = default(v128);
                dst.Float0 = (imm8 & 1) != 0 ? sum : 0.0f;
                dst.Float1 = (imm8 & 2) != 0 ? sum : 0.0f;
                dst.Float2 = (imm8 & 4) != 0 ? sum : 0.0f;
                dst.Float3 = (imm8 & 8) != 0 ? sum : 0.0f;

                return dst;
            }

            // _mm_extract_ps
            /// <summary> Extract a single-precision (32-bit) floating-point element from "a", selected with "imm8", and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int extract_ps(v128 a, int imm8)
            {
                int* iptr = &a.SInt0;
                return iptr[imm8 & 0x3];
            }

            // unity extension
            /// <summary> Extract a single-precision (32-bit) floating-point element from "a", selected with "imm8", and store the result in "dst" (as a float).</summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Float</returns>
            [DebuggerStepThrough]
            public static float extractf_ps(v128 a, int imm8)
            {
                float* fptr = &a.Float0;
                return fptr[imm8 & 0x3];
            }

            // _mm_extract_epi8
            /// <summary> Extract an 8-bit integer from "a", selected with "imm8", and store the result in the lower element of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Byte</returns>
            [DebuggerStepThrough]
            public static byte extract_epi8(v128 a, int imm8)
            {
                byte* bptr = &a.Byte0;
                return bptr[imm8 & 0xf];
            }

            // _mm_extract_epi32
            /// <summary> Extract a 32-bit integer from "a", selected with "imm8", and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int extract_epi32(v128 a, int imm8)
            {
                int* iptr = &a.SInt0;
                return iptr[imm8 & 0x3];
            }

            // _mm_extract_epi64
            /// <summary> Extract a 64-bit integer from "a", selected with "imm8", and store the result in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="imm8">imm8</param>
			/// <returns>64-bit integer</returns>
            [DebuggerStepThrough]
            public static long extract_epi64(v128 a, int imm8)
            {
                long* lptr = &a.SLong0;
                return lptr[imm8 & 0x1];
            }

            // _mm_insert_ps
            /// <summary> Copy "a" to "tmp", then insert a single-precision (32-bit) floating-point element from "b" into "tmp" using the control in "imm8". Store "tmp" to "dst" using the mask in "imm8" (elements are zeroed out when the corresponding bit is set).  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control mask</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 insert_ps(v128 a, v128 b, int imm8)
            {
                v128 dst = a;
                (&dst.Float0)[(imm8 >> 4) & 3] = (&b.Float0)[(imm8 >> 6) & 3];
                for (int i = 0; i < 4; ++i)
                {
                    if (0 != (imm8 & (1 << i)))
                        (&dst.Float0)[i] = 0.0f;
                }
                return dst;
            }

            // _mm_insert_epi8
            /// <summary> Copy "a" to "dst", and insert the lower 8-bit integer from "i" into "dst" at the location specified by "imm8".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="i">lower 8-bit integer</param>
			/// <param name="imm8">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 insert_epi8(v128 a, byte i, int imm8)
            {
                v128 dst = a;
                (&dst.Byte0)[imm8 & 0xf] = i;
                return dst;
            }

            // _mm_insert_epi32
            /// <summary> Copy "a" to "dst", and insert the 32-bit integer "i" into "dst" at the location specified by "imm8".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="i">32-bit integer</param>
			/// <param name="imm8">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 insert_epi32(v128 a, int i, int imm8)
            {
                v128 dst = a;
                (&dst.SInt0)[imm8 & 0x3] = i;
                return dst;
            }

            // _mm_insert_epi64
            /// <summary> Copy "a" to "dst", and insert the 64-bit integer "i" into "dst" at the location specified by "imm8".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="i">64-bit integer</param>
			/// <param name="imm8">Location</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 insert_epi64(v128 a, long i, int imm8)
            {
                v128 dst = a;
                (&dst.SLong0)[imm8 & 0x1] = i;
                return dst;
            }

            // _mm_max_epi8
            /// <summary> Compare packed 8-bit integers in "a" and "b", and store packed maximum values in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 max_epi8(v128 a, v128 b)
            {
                v128 dst = default(v128);
                sbyte* dptr = &dst.SByte0;
                sbyte* aptr = &a.SByte0;
                sbyte* bptr = &b.SByte0;
                for (int j = 0; j <= 15; j++)
                {
                    dptr[j] = Math.Max(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_max_epi32
            /// <summary> Compare packed 32-bit integers in "a" and "b", and store packed maximum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 max_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                int* aptr = &a.SInt0;
                int* bptr = &b.SInt0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = Math.Max(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_max_epu32
            /// <summary> Compare packed unsigned 32-bit integers in "a" and "b", and store packed maximum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 max_epu32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                uint* dptr = &dst.UInt0;
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = Math.Max(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_max_epu16
            /// <summary> Compare packed unsigned 16-bit integers in "a" and "b", and store packed maximum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 max_epu16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                ushort* dptr = &dst.UShort0;
                ushort* aptr = &a.UShort0;
                ushort* bptr = &b.UShort0;
                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = Math.Max(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_min_epi8
            /// <summary> Compare packed 8-bit integers in "a" and "b", and store packed minimum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 min_epi8(v128 a, v128 b)
            {
                v128 dst = default(v128);
                sbyte* dptr = &dst.SByte0;
                sbyte* aptr = &a.SByte0;
                sbyte* bptr = &b.SByte0;
                for (int j = 0; j <= 15; j++)
                {
                    dptr[j] = Math.Min(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_min_epi32
            /// <summary> Compare packed 32-bit integers in "a" and "b", and store packed minimum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 min_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                int* aptr = &a.SInt0;
                int* bptr = &b.SInt0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = Math.Min(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_min_epu32
            /// <summary> Compare packed unsigned 32-bit integers in "a" and "b", and store packed minimum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 min_epu32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                uint* dptr = &dst.UInt0;
                uint* aptr = &a.UInt0;
                uint* bptr = &b.UInt0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = Math.Min(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_min_epu16
            /// <summary> Compare packed unsigned 16-bit integers in "a" and "b", and store packed minimum values in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 min_epu16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                ushort* dptr = &dst.UShort0;
                ushort* aptr = &a.UShort0;
                ushort* bptr = &b.UShort0;
                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = Math.Min(aptr[j], bptr[j]);
                }
                return dst;
            }

            // _mm_packus_epi32
            /// <summary> Convert packed 32-bit integers from "a" and "b" to packed 16-bit integers using unsigned saturation, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 packus_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.UShort0 = Saturate_To_UnsignedInt16(a.SInt0);
                dst.UShort1 = Saturate_To_UnsignedInt16(a.SInt1);
                dst.UShort2 = Saturate_To_UnsignedInt16(a.SInt2);
                dst.UShort3 = Saturate_To_UnsignedInt16(a.SInt3);
                dst.UShort4 = Saturate_To_UnsignedInt16(b.SInt0);
                dst.UShort5 = Saturate_To_UnsignedInt16(b.SInt1);
                dst.UShort6 = Saturate_To_UnsignedInt16(b.SInt2);
                dst.UShort7 = Saturate_To_UnsignedInt16(b.SInt3);
                return dst;
            }

            // _mm_cmpeq_epi64
            /// <summary> Compare packed 64-bit integers in "a" and "b" for equality, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpeq_epi64(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.SLong0 = a.SLong0 == b.SLong0 ? -1L : 0L;
                dst.SLong1 = a.SLong1 == b.SLong1 ? -1L : 0L;
                return dst;
            }

            // _mm_cvtepi8_epi16
            /// <summary> Sign extend packed 8-bit integers in "a" to packed 16-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepi8_epi16(v128 a)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                sbyte* aptr = &a.SByte0;

                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepi8_epi32
            /// <summary> Sign extend packed 8-bit integers in "a" to packed 32-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepi8_epi32(v128 a)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                sbyte* aptr = &a.SByte0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepi8_epi64
            /// <summary> Sign extend packed 8-bit integers in the low 8 bytes of "a" to packed 64-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepi8_epi64(v128 a)
            {
                v128 dst = default(v128);
                long* dptr = &dst.SLong0;
                sbyte* aptr = &a.SByte0;
                for (int j = 0; j <= 1; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepi16_epi32
            /// <summary> Sign extend packed 16-bit integers in "a" to packed 32-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepi16_epi32(v128 a)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                short* aptr = &a.SShort0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepi16_epi64
            /// <summary> Sign extend packed 16-bit integers in "a" to packed 64-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepi16_epi64(v128 a)
            {
                v128 dst = default(v128);
                long* dptr = &dst.SLong0;
                short* aptr = &a.SShort0;
                for (int j = 0; j <= 1; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepi32_epi64
            /// <summary> Sign extend packed 32-bit integers in "a" to packed 64-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepi32_epi64(v128 a)
            {
                v128 dst = default(v128);
                long* dptr = &dst.SLong0;
                int* aptr = &a.SInt0;
                for (int j = 0; j <= 1; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepu8_epi16
            /// <summary> Zero extend packed unsigned 8-bit integers in "a" to packed 16-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepu8_epi16(v128 a)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                byte* aptr = &a.Byte0;
                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepu8_epi32
            /// <summary> Zero extend packed unsigned 8-bit integers in "a" to packed 32-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepu8_epi32(v128 a)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                byte* aptr = &a.Byte0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepu8_epi64
            /// <summary> Zero extend packed unsigned 8-bit integers in the low 8 byte sof "a" to packed 64-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepu8_epi64(v128 a)
            {
                v128 dst = default(v128);
                long* dptr = &dst.SLong0;
                byte* aptr = &a.Byte0;
                for (int j = 0; j <= 1; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepu16_epi32
            /// <summary> Zero extend packed unsigned 16-bit integers in "a" to packed 32-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepu16_epi32(v128 a)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                ushort* aptr = &a.UShort0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepu16_epi64
            /// <summary> Zero extend packed unsigned 16-bit integers in "a" to packed 64-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepu16_epi64(v128 a)
            {
                v128 dst = default(v128);
                long* dptr = &dst.SLong0;
                ushort* aptr = &a.UShort0;
                for (int j = 0; j <= 1; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_cvtepu32_epi64
            /// <summary> Zero extend packed unsigned 32-bit integers in "a" to packed 64-bit integers, and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cvtepu32_epi64(v128 a)
            {
                v128 dst = default(v128);
                long* dptr = &dst.SLong0;
                uint* aptr = &a.UInt0;
                for (int j = 0; j <= 1; j++)
                {
                    dptr[j] = aptr[j];
                }
                return dst;
            }

            // _mm_mul_epi32
            /// <summary> Multiply the low 32-bit integers from each packed 64-bit element in "a" and "b", and store the signed 64-bit results in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mul_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.SLong0 = a.SInt0 * (long)b.SInt0;
                dst.SLong1 = a.SInt2 * (long)b.SInt2;
                return dst;
            }

            // _mm_mullo_epi32
            /// <summary> Multiply the packed 32-bit integers in "a" and "b", producing intermediate 64-bit integers, and store the low 32 bits of the intermediate integers in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mullo_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                int* aptr = &a.SInt0;
                int* bptr = &b.SInt0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = aptr[j] * bptr[j];
                }
                return dst;
            }

            // _mm_testz_si128
            /// <summary> Compute the bitwise AND of 128 bits (representing integer data) in "a" and "b", and set "ZF" to 1 if the result is zero, otherwise set "ZF" to 0. Compute the bitwise NOT of "a" and then AND with "b", and set "CF" to 1 if the result is zero, otherwise set "CF" to 0. Return the "ZF" value. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>ZF value</returns>
            [DebuggerStepThrough]
            public static int testz_si128(v128 a, v128 b)
            {
                return ((a.SLong0 & b.SLong0) == 0 && (a.SLong1 & b.SLong1) == 0) ? 1 : 0;
            }


            // _mm_testc_si128
            /// <summary> Compute the bitwise AND of 128 bits (representing integer data) in "a" and "b", and set "ZF" to 1 if the result is zero, otherwise set "ZF" to 0. Compute the bitwise NOT of "a" and then AND with "b", and set "CF" to 1 if the result is zero, otherwise set "CF" to 0. Return the "CF" value. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>CF value</returns>
            [DebuggerStepThrough]
            public static int testc_si128(v128 a, v128 b)
            {
                return (((~a.SLong0) & b.SLong0) == 0 && ((~a.SLong1) & b.SLong1) == 0) ? 1 : 0;
            }

            // _mm_testnzc_si128
            /// <summary>Compute the bitwise AND of 128 bits (representing integer data) in "a" and "b", and set "ZF" to 1 if the result is zero, otherwise set "ZF" to 0. Compute the bitwise NOT of "a" and then AND with "b", and set "CF" to 1 if the result is zero, otherwise set "CF" to 0. Return 1 if both the "ZF" and "CF" values are zero, otherwise return 0.</summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            public static int testnzc_si128(v128 a, v128 b)
            {
                int zf = ((a.SLong0 & b.SLong0) == 0 && (a.SLong1 & b.SLong1) == 0) ? 1 : 0;
                int cf = (((~a.SLong0) & b.SLong0) == 0 && ((~a.SLong1) & b.SLong1) == 0) ? 1 : 0;
                return 1 - (zf | cf);
            }
            // _mm_test_all_zeros
            /// <summary> Compute the bitwise AND of 128 bits (representing integer data) in "a" and "mask", and return 1 if the result is zero, otherwise return 0. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="mask">Mask</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static int test_all_zeros(v128 a, v128 mask)
            {
                return testz_si128(a, mask);
            }

            // _mm_test_mix_ones_zeros
            /// <summary>Compute the bitwise AND of 128 bits (representing integer data) in "a" and "mask", and set "ZF" to 1 if the result is zero, otherwise set "ZF" to 0. Compute the bitwise NOT of "a" and then AND with "mask", and set "CF" to 1 if the result is zero, otherwise set "CF" to 0. Return 1 if both the "ZF" and "CF" values are zero, otherwise return 0.</summary>
			/// <param name="a">Vector a</param>
			/// <param name="mask">Mask</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static int test_mix_ones_zeroes(v128 a, v128 mask)
            {
                return testnzc_si128(a, mask);
            }

            // _mm_test_all_ones
            /// <summary>Compute the bitwise NOT of "a" and then AND with a 128-bit vector containing all 1's, and return 1 if the result is zero, otherwise return 0.></summary> 
			/// <param name="a">Vector a</param>
			/// <returns>Boolean result</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static int test_all_ones(v128 a)
            {
                return testc_si128(a, Sse2.cmpeq_epi32(a, a));
            }

            // Wrapper for C# reference mode to handle FROUND_xxx
            private static double RoundDImpl(double d, int roundingMode)
            {
                switch (roundingMode & 7)
                {
                    case 0: return Math.Round(d);
                    case 1: return Math.Floor(d);
                    case 2:
                        {
                            double r = Math.Ceiling(d);
                            if (r == 0.0 && d < 0.0)
                            {
                                // Emulate intel's ceil rounding to zero leaving the data at negative zero
                                return new v128(0x8000_0000_0000_0000).Double0;
                            }
                            else
                            {
                                return r;
                            }
                        }
                    case 3: return Math.Truncate(d);
                    default:
                        switch (MXCSR & MXCSRBits.RoundingControlMask)
                        {
                            case MXCSRBits.RoundToNearest: return Math.Round(d);
                            case MXCSRBits.RoundDown: return Math.Floor(d);
                            case MXCSRBits.RoundUp: return Math.Ceiling(d);
                            default: return Math.Truncate(d);
                        }
                }
            }

            // _mm_round_pd
            /// <summary> Round the packed double-precision (64-bit) floating-point elements in "a" using the "rounding" parameter, and store the results as packed double-precision floating-point elements in "dst".</summary>
			/// <param name="a">Vector a</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 round_pd(v128 a, int rounding)
            {
                v128 dst = default(v128);
                dst.Double0 = RoundDImpl(a.Double0, rounding);
                dst.Double1 = RoundDImpl(a.Double1, rounding);
                return dst;
            }

            // _mm_floor_pd
            /// <summary> Round the packed double-precision (64-bit) floating-point elements in "a" down to an integer value, and store the results as packed double-precision floating-point elements in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 floor_pd(v128 a)
            {
                return round_pd(a, (int)RoundingMode.FROUND_FLOOR);
            }

            // _mm_ceil_pd
            /// <summary> Round the packed double-precision (64-bit) floating-point elements in "a" up to an integer value, and store the results as packed double-precision floating-point elements in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 ceil_pd(v128 a)
            {
                return round_pd(a, (int)RoundingMode.FROUND_CEIL);
            }

            // _mm_round_ps
            /// <summary> Round the packed single-precision (32-bit) floating-point elements in "a" using the "rounding" parameter, and store the results as packed single-precision floating-point elements in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 round_ps(v128 a, int rounding)
            {
                v128 dst = default(v128);
                dst.Float0 = (float)RoundDImpl(a.Float0, rounding);
                dst.Float1 = (float)RoundDImpl(a.Float1, rounding);
                dst.Float2 = (float)RoundDImpl(a.Float2, rounding);
                dst.Float3 = (float)RoundDImpl(a.Float3, rounding);
                return dst;
            }

            // _mm_floor_ps
            /// <summary> Round the packed single-precision (32-bit) floating-point elements in "a" down to an integer value, and store the results as packed single-precision floating-point elements in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 floor_ps(v128 a)
            {
                return round_ps(a, (int)RoundingMode.FROUND_FLOOR);
            }

            // _mm_ceil_ps
            /// <summary> Round the packed single-precision (32-bit) floating-point elements in "a" up to an integer value, and store the results as packed single-precision floating-point elements in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 ceil_ps(v128 a)
            {
                return round_ps(a, (int)RoundingMode.FROUND_CEIL);
            }

            // _mm_round_sd
            /// <summary> Round the lower double-precision (64-bit) floating-point element in "b" using the "rounding" parameter, store the result as a double-precision floating-point element in the lower element of "dst", and copy the upper element from "a" to the upper element of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 round_sd(v128 a, v128 b, int rounding)
            {
                v128 dst = default(v128);
                dst.Double0 = RoundDImpl(b.Double0, rounding);
                dst.Double1 = a.Double1;
                return dst;
            }

            // _mm_floor_sd
            /// <summary> Round the lower double-precision (64-bit) floating-point element in "b" down to an integer value, store the result as a double-precision floating-point element in the lower element of "dst", and copy the upper element from "a" to the upper element of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 floor_sd(v128 a, v128 b)
            {
                return round_sd(a, b, (int)RoundingMode.FROUND_FLOOR);
            }

            // _mm_ceil_sd
            /// <summary> Round the lower double-precision (64-bit) floating-point element in "b" up to an integer value, store the result as a double-precision floating-point element in the lower element of "dst", and copy the upper element from "a" to the upper element of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 ceil_sd(v128 a, v128 b)
            {
                return round_sd(a, b, (int)RoundingMode.FROUND_CEIL);
            }

            // _mm_round_ss
            /// <summary> Round the lower single-precision (32-bit) floating-point element in "b" using the "rounding" parameter, store the result as a single-precision floating-point element in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst".</summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="rounding">Rounding mode</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 round_ss(v128 a, v128 b, int rounding)
            {
                v128 dst = a;
                dst.Float0 = (float)RoundDImpl(b.Float0, rounding);
                return dst;
            }

            // _mm_floor_ss
            /// <summary> Round the lower single-precision (32-bit) floating-point element in "b" down to an integer value, store the result as a single-precision floating-point element in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 floor_ss(v128 a, v128 b)
            {
                return round_ss(a, b, (int)RoundingMode.FROUND_FLOOR);
            }

            // _mm_ceil_ss
            /// <summary> Round the lower single-precision (32-bit) floating-point element in "b" up to an integer value, store the result as a single-precision floating-point element in the lower element of "dst", and copy the upper 3 packed elements from "a" to the upper elements of "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            [BurstTargetCpu(BurstTargetCpu.X64_SSE4)]
            public static v128 ceil_ss(v128 a, v128 b)
            {
                return round_ss(a, b, (int)RoundingMode.FROUND_CEIL);
            }

            // _mm_minpos_epu16
            /// <summary> Horizontally compute the minimum amongst the packed unsigned 16-bit integers in "a", store the minimum and index in "dst", and zero the remaining bits in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 minpos_epu16(v128 a)
            {
                int index = 0;
                ushort min = a.UShort0;
                ushort* aptr = &a.UShort0;
                for (int j = 1; j <= 7; j++)
                {
                    if (aptr[j] < min)
                    {
                        index = j;
                        min = aptr[j];
                    }
                }

                v128 dst = default(v128);
                dst.UShort0 = min;
                dst.UShort1 = (ushort)index;
                return dst;
            }

            // _mm_mpsadbw_epu8
            /// <summary> Compute the sum of absolute differences (SADs) of quadruplets of unsigned 8-bit integers in "a" compared to those in "b", and store the 16-bit results in "dst".</summary>
            /// <remarks>Eight SADs are performed using one quadruplet from "b" and eight quadruplets from "a". One quadruplet is selected from "b" starting at on the offset specified in "imm8". Eight quadruplets are formed from sequential 8-bit integers selected from "a" starting at the offset specified in "imm8".</remarks>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Offset</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mpsadbw_epu8(v128 a, v128 b, int imm8)
            {
                v128 dst = default(v128);
                ushort* dptr = &dst.UShort0;
                byte* aptr = &a.Byte0 + ((imm8 >> 2) & 1) * 4;
                byte* bptr = &b.Byte0 + (imm8 & 3) * 4;

                byte b0 = bptr[0];
                byte b1 = bptr[1];
                byte b2 = bptr[2];
                byte b3 = bptr[3];

                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = (ushort)(Math.Abs(aptr[j + 0] - b0) + Math.Abs(aptr[j + 1] - b1) + Math.Abs(aptr[j + 2] - b2) + Math.Abs(aptr[j + 3] - b3));
                }
                return dst;
            }

            /// <summary>Helper macro to create index-parameter value for insert_ps</summary> 
			/// <param name="srcField">Source field</param>
			/// <param name="dstField">Destination field</param>
			/// <param name="zeroMask">Zero mask</param>
			/// <returns>Integer</returns>
            [DebuggerStepThrough]
            public static int MK_INSERTPS_NDX(int srcField, int dstField, int zeroMask)
            {
                return (srcField << 6) | (dstField << 4) | zeroMask;
            }
        }
    }
}
