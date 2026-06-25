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
        /// SSSE3 intrinsics
        /// </summary>
        public static class Ssse3
        {
            /// <summary>
            /// Evaluates to true at compile time if SSSE3 intrinsics are supported.
            /// </summary>
            public static bool IsSsse3Supported { get { return false; } }

            // _mm_abs_epi8
            /// <summary> Compute the absolute value of packed 8-bit integers in "a", and store the unsigned results in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 abs_epi8(v128 a)
            {
                v128 dst = default(v128);
                byte* dptr = &dst.Byte0;
                sbyte* aptr = &a.SByte0;
                for (int j = 0; j <= 15; j++)
                {
                    dptr[j] = (byte)Math.Abs((int)aptr[j]);
                }
                return dst;
            }

            // _mm_abs_epi16
            /// <summary> Compute the absolute value of packed 16-bit integers in "a", and store the unsigned results in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 abs_epi16(v128 a)
            {
                v128 dst = default(v128);
                ushort* dptr = &dst.UShort0;
                short* aptr = &a.SShort0;
                for (int j = 0; j <= 7; j++)
                {
                    dptr[j] = (ushort)Math.Abs((int)aptr[j]);
                }
                return dst;
            }

            // _mm_abs_epi32
            /// <summary> Compute the absolute value of packed 32-bit integers in "a", and store the unsigned results in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 abs_epi32(v128 a)
            {
                v128 dst = default(v128);
                uint* dptr = &dst.UInt0;
                int* aptr = &a.SInt0;
                for (int j = 0; j <= 3; j++)
                {
                    dptr[j] = (uint)Math.Abs((long)aptr[j]);
                }
                return dst;
            }

            // _mm_shuffle_epi8
            /// <summary> Shuffle packed 8-bit integers in "a" according to shuffle control mask in the corresponding 8-bit element of "b", and store the results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 shuffle_epi8(v128 a, v128 b)
            {
                v128 dst = default(v128);
                byte* dptr = &dst.Byte0;
                byte* aptr = &a.Byte0;
                byte* bptr = &b.Byte0;
                for (int j = 0; j <= 15; j++)
                {
                    if ((bptr[j] & 0x80) != 0)
                    {
                        dptr[j] = 0x00;
                    }
                    else
                    {
                        dptr[j] = aptr[bptr[j] & 15];
                    }
                }
                return dst;
            }


            // _mm_alignr_epi8
            /// <summary> Concatenate 16-byte blocks in "a" and "b" into a 32-byte temporary result, shift the result right by "count" bytes, and store the low 16 bytes in "dst".  </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="count">Byte count</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 alignr_epi8(v128 a, v128 b, int count)
            {
                var dst = default(v128);
                byte* dptr = &dst.Byte0;
                byte* aptr = &a.Byte0 + count;
                byte* bptr = &b.Byte0;

                int i;
                for (i = 0; i < 16 - count; ++i)
                {
                    *dptr++ = *aptr++;
                }

                for (; i < 16; ++i)
                {
                    *dptr++ = *bptr++;
                }

                return dst;
            }

            // _mm_hadd_epi16
            /// <summary> Horizontally add adjacent pairs of 16-bit integers in "a" and "b", and pack the signed 16-bit results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hadd_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (int j = 0; j <= 3; ++j)
                {
                    dptr[j] = (short)(aptr[2 * j + 1] + aptr[2 * j]);
                    dptr[j + 4] = (short)(bptr[2 * j + 1] + bptr[2 * j]);
                }
                return dst;
            }

            // _mm_hadds_epi16
            /// <summary> Horizontally add adjacent pairs of 16-bit integers in "a" and "b" using saturation, and pack the signed 16-bit results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hadds_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (int j = 0; j <= 3; ++j)
                {
                    dptr[j] = Saturate_To_Int16(aptr[2 * j + 1] + aptr[2 * j]);
                    dptr[j + 4] = Saturate_To_Int16(bptr[2 * j + 1] + bptr[2 * j]);
                }
                return dst;
            }

            // _mm_hadd_epi32
            /// <summary> Horizontally add adjacent pairs of 32-bit integers in "a" and "b", and pack the signed 32-bit results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hadd_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.SInt0 = a.SInt1 + a.SInt0;
                dst.SInt1 = a.SInt3 + a.SInt2;
                dst.SInt2 = b.SInt1 + b.SInt0;
                dst.SInt3 = b.SInt3 + b.SInt2;
                return dst;
            }

            // _mm_hsub_epi16
            /// <summary> Horizontally subtract adjacent pairs of 16-bit integers in "a" and "b", and pack the signed 16-bit results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hsub_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (int j = 0; j <= 3; ++j)
                {
                    dptr[j] = (short)(aptr[2 * j] - aptr[2 * j + 1]);
                    dptr[j + 4] = (short)(bptr[2 * j] - bptr[2 * j + 1]);
                }
                return dst;
            }

            // _mm_hsubs_epi16
            /// <summary> Horizontally subtract adjacent pairs of 16-bit integers in "a" and "b" using saturation, and pack the signed 16-bit results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hsubs_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (int j = 0; j <= 3; ++j)
                {
                    dptr[j] = Saturate_To_Int16(aptr[2 * j] - aptr[2 * j + 1]);
                    dptr[j + 4] = Saturate_To_Int16(bptr[2 * j] - bptr[2 * j + 1]);
                }
                return dst;
            }

            // _mm_hsub_epi32
            /// <summary> Horizontally subtract adjacent pairs of 32-bit integers in "a" and "b", and pack the signed 32-bit results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 hsub_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                dst.SInt0 = a.SInt0 - a.SInt1;
                dst.SInt1 = a.SInt2 - a.SInt3;
                dst.SInt2 = b.SInt0 - b.SInt1;
                dst.SInt3 = b.SInt2 - b.SInt3;
                return dst;
            }

            // _mm_maddubs_epi16
            /// <summary> Vertically multiply each unsigned 8-bit integer from "a" with the corresponding signed 8-bit integer from "b", producing intermediate signed 16-bit integers. Horizontally add adjacent pairs of intermediate signed 16-bit integers, and pack the saturated results in "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 maddubs_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                byte* aptr = &a.Byte0;
                sbyte* bptr = &b.SByte0;
                for (int j = 0; j <= 7; j++)
                {
                    int tmp = aptr[2 * j + 1] * bptr[2 * j + 1] + aptr[2 * j] * bptr[2 * j];
                    dptr[j] = Saturate_To_Int16(tmp);
                }
                return dst;
            }


            // _mm_mulhrs_epi16
            /// <summary> Multiply packed 16-bit integers in "a" and "b", producing intermediate signed 32-bit integers. Truncate each intermediate integer to the 18 most significant bits, round by adding 1, and store bits [16:1] to "dst". </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 mulhrs_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (int j = 0; j <= 7; j++)
                {
                    int tmp = aptr[j] * bptr[j];
                    tmp >>= 14;
                    tmp += 1;
                    tmp >>= 1;
                    dptr[j] = (short)tmp;
                }
                return dst;
            }

            // _mm_sign_epi8
            /// <summary> Negate packed 8-bit integers in "a" when the corresponding signed 8-bit integer in "b" is negative, and store the results in "dst". Element in "dst" are zeroed out when the corresponding element in "b" is zero. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sign_epi8(v128 a, v128 b)
            {
                v128 dst = default(v128);
                sbyte* dptr = &dst.SByte0;
                sbyte* aptr = &a.SByte0;
                sbyte* bptr = &b.SByte0;
                for (int j = 0; j <= 15; j++)
                {
                    if (bptr[j] < 0)
                    {
                        dptr[j] = (sbyte)-aptr[j];
                    }
                    else if (bptr[j] == 0)
                    {
                        dptr[j] = 0;
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_sign_epi16
            /// <summary> Negate packed 16-bit integers in "a" when the corresponding signed 16-bit integer in "b" is negative, and store the results in "dst". Element in "dst" are zeroed out when the corresponding element in "b" is zero. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sign_epi16(v128 a, v128 b)
            {
                v128 dst = default(v128);
                short* dptr = &dst.SShort0;
                short* aptr = &a.SShort0;
                short* bptr = &b.SShort0;
                for (int j = 0; j <= 7; j++)
                {
                    if (bptr[j] < 0)
                    {
                        dptr[j] = (short)-aptr[j];
                    }
                    else if (bptr[j] == 0)
                    {
                        dptr[j] = 0;
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }

            // _mm_sign_epi32
            /// <summary> Negate packed 32-bit integers in "a" when the corresponding signed 32-bit integer in "b" is negative, and store the results in "dst". Element in "dst" are zeroed out when the corresponding element in "b" is zero. </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 sign_epi32(v128 a, v128 b)
            {
                v128 dst = default(v128);
                int* dptr = &dst.SInt0;
                int* aptr = &a.SInt0;
                int* bptr = &b.SInt0;
                for (int j = 0; j <= 3; j++)
                {
                    if (bptr[j] < 0)
                    {
                        dptr[j] = -aptr[j];
                    }
                    else if (bptr[j] == 0)
                    {
                        dptr[j] = 0;
                    }
                    else
                    {
                        dptr[j] = aptr[j];
                    }
                }
                return dst;
            }
        }
    }
}
