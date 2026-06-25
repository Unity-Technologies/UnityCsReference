// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.Burst.Intrinsics
{
    public unsafe static partial class X86
    {
        /// <summary>
        /// SSE 4.2 intrinsics
        /// </summary>
        public static class Sse4_2
        {
            /// <summary>
            /// Evaluates to true at compile time if SSE 4.2 intrinsics are supported.
            /// </summary>
            public static bool IsSse42Supported { get { return false; } }

            /// <summary>
            /// Constants for string comparison intrinsics
            /// </summary>
            [Flags]
            public enum SIDD
            {
                /// <summary>
                /// Compare 8-bit unsigned characters
                /// </summary>
                UBYTE_OPS = 0x00,
                /// <summary>
                /// Compare 16-bit unsigned characters
                /// </summary>
                UWORD_OPS = 0x01,
                /// <summary>
                /// Compare 8-bit signed characters
                /// </summary>
                SBYTE_OPS = 0x02,
                /// <summary>
                /// Compare 16-bit signed characters
                /// </summary>
                SWORD_OPS = 0x03,

                /// <summary>
                /// Compare any equal
                /// </summary>
                CMP_EQUAL_ANY = 0x00,
                /// <summary>
                /// Compare ranges
                /// </summary>
                CMP_RANGES = 0x04,
                /// <summary>
                /// Compare equal each
                /// </summary>
                CMP_EQUAL_EACH = 0x08,
                /// <summary>
                /// Compare equal ordered
                /// </summary>
                CMP_EQUAL_ORDERED = 0x0C,

                /// <summary>
                /// Normal result polarity
                /// </summary>
                POSITIVE_POLARITY = 0x00,
                /// <summary>
                /// Negate results
                /// </summary>
                NEGATIVE_POLARITY = 0x10,
                /// <summary>
                /// Normal results only before end of string
                /// </summary>
                MASKED_POSITIVE_POLARITY = 0x20,
                /// <summary>
                /// Negate results only before end of string
                /// </summary>
                MASKED_NEGATIVE_POLARITY = 0x30,

                /// <summary>
                /// Index only: return least significant bit
                /// </summary>
                LEAST_SIGNIFICANT = 0x00,
                /// <summary>
                /// Index only: return most significan bit
                /// </summary>
                MOST_SIGNIFICANT = 0x40,

                /// <summary>
                /// mask only: return bit mask
                /// </summary>
                BIT_MASK = 0x00,
                /// <summary>
                /// mask only: return byte/word mask
                /// </summary>
                UNIT_MASK = 0x40,

            }

            /*
             * Intrinsics for text/string processing.
             */

            private unsafe struct StrBoolArray
            {
                public fixed ushort Bits[16];

                public void SetBit(int aindex, int bindex, bool val)
                {
                    fixed (ushort* b = Bits)
                    {
                        if (val)
                            b[aindex] |= (ushort)(1 << bindex);
                        else
                            b[aindex] &= (ushort)(~(1 << bindex));
                    }
                }

                public bool GetBit(int aindex, int bindex)
                {
                    fixed (ushort* b = Bits)
                    {
                        return (b[aindex] & (1 << bindex)) != 0;
                    }
                }
            }

            private static v128 cmpistrm_emulation<T>(T* a, T* b, int len, int imm8, int allOnes, T allOnesT) where T : unmanaged, IComparable<T>, IEquatable<T>
            {
                int intRes2 = ComputeStrCmpIntRes2<T>(a, ComputeStringLength<T>(a, len), b, ComputeStringLength<T>(b, len), len, imm8, allOnes);

                return ComputeStrmOutput(len, imm8, allOnesT, intRes2);
            }

            private static v128 cmpestrm_emulation<T>(T* a, int alen, T* b, int blen, int len, int imm8, int allOnes, T allOnesT) where T : unmanaged, IComparable<T>, IEquatable<T>
            {
                int intRes2 = ComputeStrCmpIntRes2<T>(a, alen, b, blen, len, imm8, allOnes);

                return ComputeStrmOutput(len, imm8, allOnesT, intRes2);
            }

            private static v128 ComputeStrmOutput<T>(int len, int imm8, T allOnesT, int intRes2) where T : unmanaged, IComparable<T>, IEquatable<T>
            {
                // output
                v128 result = default;
                if ((imm8 & (1 << 6)) != 0)
                {
                    // byte / word mask
                    T* maskDst = (T*)&result.Byte0;
                    for (int i = 0; i < len; ++i)
                    {
                        if ((intRes2 & (1 << i)) != 0)
                        {
                            maskDst[i] = allOnesT;
                        }
                        else
                        {
                            maskDst[i] = default(T);
                        }
                    }
                }
                else
                {
                    // bit mask
                    result.SInt0 = intRes2;
                }

                return result;
            }

            private static int cmpistri_emulation<T>(T* a, T* b, int len, int imm8, int allOnes, T allOnesT) where T : unmanaged, IComparable<T>, IEquatable<T>
            {
                int intRes2 = ComputeStrCmpIntRes2<T>(a, ComputeStringLength<T>(a, len), b, ComputeStringLength<T>(b, len), len, imm8, allOnes);

                return ComputeStriOutput(len, imm8, intRes2);
            }

            private static int cmpestri_emulation<T>(T* a, int alen, T* b, int blen, int len, int imm8, int allOnes, T allOnesT) where T : unmanaged, IComparable<T>, IEquatable<T>
            {
                int intRes2 = ComputeStrCmpIntRes2<T>(a, alen, b, blen, len, imm8, allOnes);

                return ComputeStriOutput(len, imm8, intRes2);
            }

            private static int ComputeStriOutput(int len, int imm8, int intRes2)
            {
                // output
                if ((imm8 & (1 << 6)) == 0)
                {
                    int bit = 0;
                    while (bit < len)
                    {
                        if ((intRes2 & (1 << bit)) != 0)
                            return bit;
                        ++bit;
                    }
                }
                else
                {
                    int bit = len - 1;
                    while (bit >= 0)
                    {
                        if ((intRes2 & (1 << bit)) != 0)
                            return bit;
                        --bit;
                    }
                }

                return len;
            }

            private static int ComputeStringLength<T>(T* ptr, int max) where T : unmanaged, IEquatable<T>
            {
                for (int i = 0; i < max; ++i)
                {
                    if (EqualityComparer<T>.Default.Equals(ptr[i], default(T)))
                    {
                        return i;
                    }
                }
                return max;
            }

            private static int ComputeStrCmpIntRes2<T>(T* a, int alen, T* b, int blen, int len, int imm8, int allOnes) where T : unmanaged, IComparable<T>, IEquatable<T>
            {
                bool aInvalid = false;
                bool bInvalid = false;
                StrBoolArray boolRes = default;
                int i, j, intRes2;

                for (i = 0; i < len; ++i)
                {
                    T aCh = a[i];

                    if (i == alen)
                        aInvalid = true;

                    bInvalid = false;
                    for (j = 0; j < len; ++j)
                    {
                        T bCh = b[j];
                        if (j == blen)
                            bInvalid = true;

                        bool match;

                        // override comparisons for invalid characters
                        switch ((imm8 >> 2) & 3)
                        {
                            case 0:  // equal any
                                match = EqualityComparer<T>.Default.Equals(aCh, bCh);
                                if (!aInvalid && bInvalid)
                                    match = false;
                                else if (aInvalid && !bInvalid)
                                    match = false;
                                else if (aInvalid && bInvalid)
                                    match = false;
                                break;

                            case 1:  // ranges
                                if (0 == (i & 1))
                                    match = Comparer<T>.Default.Compare(bCh, aCh) >= 0;
                                else
                                    match = Comparer<T>.Default.Compare(bCh, aCh) <= 0;

                                if (!aInvalid && bInvalid)
                                    match = false;
                                else if (aInvalid && !bInvalid)
                                    match = false;
                                else if (aInvalid && bInvalid)
                                    match = false;
                                break;
                            case 2:  // equal each
                                match = EqualityComparer<T>.Default.Equals(aCh, bCh);
                                if (!aInvalid && bInvalid)
                                    match = false;
                                else if (aInvalid && !bInvalid)
                                    match = false;
                                else if (aInvalid && bInvalid)
                                    match = true;
                                break;
                            default:  // equal ordered
                                match = EqualityComparer<T>.Default.Equals(aCh, bCh);
                                if (!aInvalid && bInvalid)
                                    match = false;
                                else if (aInvalid && !bInvalid)
                                    match = true;
                                else if (aInvalid && bInvalid)
                                    match = true;
                                break;
                        }

                        boolRes.SetBit(i, j, match);
                    }
                }

                int intRes1 = 0;

                // aggregate results
                switch ((imm8 >> 2) & 3)
                {
                    case 0:  // equal any
                        for (i = 0; i < len; ++i)
                        {
                            for (j = 0; j < len; ++j)
                            {
                                intRes1 |= (boolRes.GetBit(j, i) ? 1 : 0) << i;
                            }
                        }
                        /*
                        for (i = 0; i < len; ++i)
                        {
                            intRes1 |= boolRes.Bits[i];
                        }*/
                        break;
                    case 1:  // ranges
                        for (i = 0; i < len; ++i)
                        {
                            for (j = 0; j < len; j += 2)
                            {
                                intRes1 |= ((boolRes.GetBit(j, i) && boolRes.GetBit(j + 1, i)) ? 1 : 0) << i;
                            }
                        }
                        break;
                    case 2:  // equal each
                        for (i = 0; i < len; ++i)
                        {
                            intRes1 |= (boolRes.GetBit(i, i) ? 1 : 0) << i;
                        }
                        break;
                    case 3:  // equal ordered
                        intRes1 = allOnes;
                        for (i = 0; i < len; ++i)
                        {
                            int k = i;
                            for (j = 0; j < len - i; ++j)
                            {
                                if (!boolRes.GetBit(j, k))
                                    intRes1 &= ~(1 << i);
                                k += 1;
                            }
                        }
                        break;
                }

                intRes2 = 0;

                // optionally negate results
                bInvalid = false;
                for (i = 0; i < len; ++i)
                {
                    if ((imm8 & (1 << 4)) != 0)
                    {
                        if ((imm8 & (1 << 5)) != 0) // only negate valid
                        {
                            if (EqualityComparer<T>.Default.Equals(b[i], default(T)))
                            {
                                bInvalid = true;
                            }

                            if (bInvalid) // invalid, don't negate
                                intRes2 |= intRes1 & (1 << i);
                            else // valid, negate
                                intRes2 |= (~intRes1) & (1 << i);
                        }
                        else // negate all
                            intRes2 |= (~intRes1) & (1 << i);
                    }
                    else // don't negate
                        intRes2 |= intRes1 & (1 << i);
                }

                return intRes2;
            }

            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and store the generated mask in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpistrm(v128 a, v128 b, int imm8)
            {
                v128 c;

                if (0 == (imm8 & 1))
                    if (0 == (imm8 & 2))
                        c = cmpistrm_emulation(&a.Byte0, &b.Byte0, 16, imm8, 0xffff, (byte)0xff);
                    else
                        c = cmpistrm_emulation(&a.SByte0, &b.SByte0, 16, imm8, 0xffff, (sbyte)-1);
                else
                    if (0 == (imm8 & 2))
                    c = cmpistrm_emulation(&a.UShort0, &b.UShort0, 8, imm8, 0xff, (ushort)0xffff);
                else
                    c = cmpistrm_emulation(&a.SShort0, &b.SShort0, 8, imm8, 0xff, (short)-1);

                return c;
            }


            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and store the generated index in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Index</returns>
            [DebuggerStepThrough]
            public static int cmpistri(v128 a, v128 b, int imm8)
            {
                if (0 == (imm8 & 1))
                    if (0 == (imm8 & 2))
                        return cmpistri_emulation(&a.Byte0, &b.Byte0, 16, imm8, 0xffff, (byte)0xff);
                    else
                        return cmpistri_emulation(&a.SByte0, &b.SByte0, 16, imm8, 0xffff, (sbyte)-1);
                else
                    if (0 == (imm8 & 2))
                    return cmpistri_emulation(&a.UShort0, &b.UShort0, 8, imm8, 0xff, (ushort)0xffff);
                else
                    return cmpistri_emulation(&a.SShort0, &b.SShort0, 8, imm8, 0xff, (short)-1);
            }

            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and store the generated mask in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpestrm(v128 a, int la, v128 b, int lb, int imm8)
            {
                v128 c;

                if (0 == (imm8 & 1))
                    if (0 == (imm8 & 2))
                        c = cmpestrm_emulation(&a.Byte0, la, &b.Byte0, lb, 16, imm8, 0xffff, (byte)0xff);
                    else
                        c = cmpestrm_emulation(&a.SByte0, la, &b.SByte0, lb, 16, imm8, 0xffff, (sbyte)-1);
                else
                    if (0 == (imm8 & 2))
                    c = cmpestrm_emulation(&a.UShort0, la, &b.UShort0, lb, 8, imm8, 0xff, (ushort)0xffff);
                else
                    c = cmpestrm_emulation(&a.SShort0, la, &b.SShort0, lb, 8, imm8, 0xff, (short)-1);

                return c;
            }

            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and store the generated index in dst.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Index</returns>
            [DebuggerStepThrough]
            public static int cmpestri(v128 a, int la, v128 b, int lb, int imm8)
            {
                if (0 == (imm8 & 1))
                    if (0 == (imm8 & 2))
                        return cmpestri_emulation(&a.Byte0, la, &b.Byte0, lb, 16, imm8, 0xffff, (byte)0xff);
                    else
                        return cmpestri_emulation(&a.SByte0, la, &b.SByte0, lb, 16, imm8, 0xffff, (sbyte)-1);
                else
                    if (0 == (imm8 & 2))
                    return cmpestri_emulation(&a.UShort0, la, &b.UShort0, lb, 8, imm8, 0xff, (ushort)0xffff);
                else
                    return cmpestri_emulation(&a.SShort0, la, &b.SShort0, lb, 8, imm8, 0xff, (short)-1);
            }

            /*
             * Intrinsics for text/string processing and reading values of EFlags.
             */

            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and returns 1 if any character in b was null, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpistrz(v128 a, v128 b, int imm8)
            {
                if (0 == (imm8 & 1))
                    return ComputeStringLength<byte>(&b.Byte0, 16) < 16 ? 1 : 0;
                else
                    return ComputeStringLength<ushort>(&b.UShort0, 8) < 8 ? 1 : 0;
            }


            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and returns 1 if the resulting mask was non-zero, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpistrc(v128 a, v128 b, int imm8)
            {
                v128 q = cmpistrm(a, b, imm8);
                return q.SInt0 == 0 && q.SInt1 == 0 && q.SInt2 == 0 && q.SInt3 == 0 ? 0 : 1;
            }
            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and returns 1 if any character in a was null, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpistrs(v128 a, v128 b, int imm8)
            {
                if (0 == (imm8 & 1))
                    return ComputeStringLength<byte>(&a.Byte0, 16) < 16 ? 1 : 0;
                else
                    return ComputeStringLength<ushort>(&a.UShort0, 8) < 8 ? 1 : 0;
            }

            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and returns bit 0 of the resulting bit mask.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Bit 0</returns>
            [DebuggerStepThrough]
            public static int cmpistro(v128 a, v128 b, int imm8)
            {
                int intRes2;

                if (0 == (imm8 & 1))
                {
                    int al = ComputeStringLength<byte>(&a.Byte0, 16);
                    int bl = ComputeStringLength<byte>(&b.Byte0, 16);

                    if (0 == (imm8 & 2))
                        intRes2 = ComputeStrCmpIntRes2<byte>(&a.Byte0, al, &b.Byte0, bl, 16, imm8, 0xffff);
                    else
                        intRes2 = ComputeStrCmpIntRes2<sbyte>(&a.SByte0, al, &b.SByte0, bl, 16, imm8, 0xffff);
                }
                else
                {
                    int al = ComputeStringLength<ushort>(&a.UShort0, 8);
                    int bl = ComputeStringLength<ushort>(&b.UShort0, 8);

                    if (0 == (imm8 & 2))
                        intRes2 = ComputeStrCmpIntRes2<ushort>(&a.UShort0, al, &b.UShort0, bl, 8, imm8, 0xff);
                    else
                        intRes2 = ComputeStrCmpIntRes2<short>(&a.SShort0, al, &b.SShort0, bl, 8, imm8, 0xff);
                }

                return intRes2 & 1;
            }

            /// <summary>
            /// Compare packed strings with implicit lengths in a and b using the control in imm8, and returns 1 if b did not contain a null character and the resulting mask was zero, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpistra(v128 a, v128 b, int imm8)
            {
                return ((~cmpistrc(a, b, imm8)) & (~cmpistrz(a, b, imm8))) & 1;
            }

            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and returns 1 if any character in b was null, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpestrz(v128 a, int la, v128 b, int lb, int imm8)
            {
                int size = (imm8 & 1) == 1 ? 16 : 8;
                int upperBound = (128 / size) - 1;
                return lb <= upperBound ? 1 : 0;
            }

            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and returns 1 if the resulting mask was non-zero, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpestrc(v128 a, int la, v128 b, int lb, int imm8)
            {
                int intRes2;

                if (0 == (imm8 & 1))
                {
                    if (0 == (imm8 & 2))
                        intRes2 = ComputeStrCmpIntRes2<byte>(&a.Byte0, la, &b.Byte0, lb, 16, imm8, 0xffff);
                    else
                        intRes2 = ComputeStrCmpIntRes2<sbyte>(&a.SByte0, la, &b.SByte0, lb, 16, imm8, 0xffff);
                }
                else
                {
                    if (0 == (imm8 & 2))
                        intRes2 = ComputeStrCmpIntRes2<ushort>(&a.UShort0, la, &b.UShort0, lb, 8, imm8, 0xff);
                    else
                        intRes2 = ComputeStrCmpIntRes2<short>(&a.SShort0, la, &b.SShort0, lb, 8, imm8, 0xff);
                }

                return intRes2 != 0 ? 1 : 0;
            }
            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and returns 1 if any character in a was null, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpestrs(v128 a, int la, v128 b, int lb, int imm8)
            {
                int size = (imm8 & 1) == 1 ? 16 : 8;
                int upperBound = (128 / size) - 1;
                return la <= upperBound ? 1 : 0;
            }

            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and returns bit 0 of the resulting bit mask.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Bit 0</returns>
            [DebuggerStepThrough]
            public static int cmpestro(v128 a, int la, v128 b, int lb, int imm8)
            {
                int intRes2;

                if (0 == (imm8 & 1))
                {
                    if (0 == (imm8 & 2))
                        intRes2 = ComputeStrCmpIntRes2<byte>(&a.Byte0, la, &b.Byte0, lb, 16, imm8, 0xffff);
                    else
                        intRes2 = ComputeStrCmpIntRes2<sbyte>(&a.SByte0, la, &b.SByte0, lb, 16, imm8, 0xffff);
                }
                else
                {
                    if (0 == (imm8 & 2))
                        intRes2 = ComputeStrCmpIntRes2<ushort>(&a.UShort0, la, &b.UShort0, lb, 8, imm8, 0xff);
                    else
                        intRes2 = ComputeStrCmpIntRes2<short>(&a.SShort0, la, &b.SShort0, lb, 8, imm8, 0xff);
                }

                return intRes2 & 1;
            }
            /// <summary>
            /// Compare packed strings in a and b with lengths la and lb using the control in imm8, and returns 1 if b did not contain a null character and the resulting mask was zero, and 0 otherwise.
            /// </summary>
			/// <param name="a">Vector a</param>
			/// <param name="b">Vector b</param>
			/// <param name="la">Length a</param>
			/// <param name="lb">Length b</param>
			/// <param name="imm8">Control</param>
			/// <returns>Boolean value</returns>
            [DebuggerStepThrough]
            public static int cmpestra(v128 a, int la, v128 b, int lb, int imm8)
            {
                return ((~cmpestrc(a, la, b, lb, imm8)) & (~cmpestrz(a, la, b, lb, imm8))) & 1;
            }

            /// <summary>
            /// Compare packed 64-bit integers in a and b for greater-than, and store the results in dst.
            /// </summary>
			/// <param name="val1">Vector a</param>
			/// <param name="val2">Vector b</param>
			/// <returns>Vector</returns>
            [DebuggerStepThrough]
            public static v128 cmpgt_epi64(v128 val1, v128 val2)
            {
                v128 result = default;
                result.SLong0 = val1.SLong0 > val2.SLong0 ? -1 : 0;
                result.SLong1 = val1.SLong1 > val2.SLong1 ? -1 : 0;
                return result;
            }

            /*
             * Accumulate CRC32 (polynomial 0x11EDC6F41) value
             */

            private static readonly uint[] crctab = new uint[]
            {
                0x00000000U,0xF26B8303U,0xE13B70F7U,0x1350F3F4U,0xC79A971FU,0x35F1141CU,0x26A1E7E8U,0xD4CA64EBU,
                0x8AD958CFU,0x78B2DBCCU,0x6BE22838U,0x9989AB3BU,0x4D43CFD0U,0xBF284CD3U,0xAC78BF27U,0x5E133C24U,
                0x105EC76FU,0xE235446CU,0xF165B798U,0x030E349BU,0xD7C45070U,0x25AFD373U,0x36FF2087U,0xC494A384U,
                0x9A879FA0U,0x68EC1CA3U,0x7BBCEF57U,0x89D76C54U,0x5D1D08BFU,0xAF768BBCU,0xBC267848U,0x4E4DFB4BU,
                0x20BD8EDEU,0xD2D60DDDU,0xC186FE29U,0x33ED7D2AU,0xE72719C1U,0x154C9AC2U,0x061C6936U,0xF477EA35U,
                0xAA64D611U,0x580F5512U,0x4B5FA6E6U,0xB93425E5U,0x6DFE410EU,0x9F95C20DU,0x8CC531F9U,0x7EAEB2FAU,
                0x30E349B1U,0xC288CAB2U,0xD1D83946U,0x23B3BA45U,0xF779DEAEU,0x05125DADU,0x1642AE59U,0xE4292D5AU,
                0xBA3A117EU,0x4851927DU,0x5B016189U,0xA96AE28AU,0x7DA08661U,0x8FCB0562U,0x9C9BF696U,0x6EF07595U,
                0x417B1DBCU,0xB3109EBFU,0xA0406D4BU,0x522BEE48U,0x86E18AA3U,0x748A09A0U,0x67DAFA54U,0x95B17957U,
                0xCBA24573U,0x39C9C670U,0x2A993584U,0xD8F2B687U,0x0C38D26CU,0xFE53516FU,0xED03A29BU,0x1F682198U,
                0x5125DAD3U,0xA34E59D0U,0xB01EAA24U,0x42752927U,0x96BF4DCCU,0x64D4CECFU,0x77843D3BU,0x85EFBE38U,
                0xDBFC821CU,0x2997011FU,0x3AC7F2EBU,0xC8AC71E8U,0x1C661503U,0xEE0D9600U,0xFD5D65F4U,0x0F36E6F7U,
                0x61C69362U,0x93AD1061U,0x80FDE395U,0x72966096U,0xA65C047DU,0x5437877EU,0x4767748AU,0xB50CF789U,
                0xEB1FCBADU,0x197448AEU,0x0A24BB5AU,0xF84F3859U,0x2C855CB2U,0xDEEEDFB1U,0xCDBE2C45U,0x3FD5AF46U,
                0x7198540DU,0x83F3D70EU,0x90A324FAU,0x62C8A7F9U,0xB602C312U,0x44694011U,0x5739B3E5U,0xA55230E6U,
                0xFB410CC2U,0x092A8FC1U,0x1A7A7C35U,0xE811FF36U,0x3CDB9BDDU,0xCEB018DEU,0xDDE0EB2AU,0x2F8B6829U,
                0x82F63B78U,0x709DB87BU,0x63CD4B8FU,0x91A6C88CU,0x456CAC67U,0xB7072F64U,0xA457DC90U,0x563C5F93U,
                0x082F63B7U,0xFA44E0B4U,0xE9141340U,0x1B7F9043U,0xCFB5F4A8U,0x3DDE77ABU,0x2E8E845FU,0xDCE5075CU,
                0x92A8FC17U,0x60C37F14U,0x73938CE0U,0x81F80FE3U,0x55326B08U,0xA759E80BU,0xB4091BFFU,0x466298FCU,
                0x1871A4D8U,0xEA1A27DBU,0xF94AD42FU,0x0B21572CU,0xDFEB33C7U,0x2D80B0C4U,0x3ED04330U,0xCCBBC033U,
                0xA24BB5A6U,0x502036A5U,0x4370C551U,0xB11B4652U,0x65D122B9U,0x97BAA1BAU,0x84EA524EU,0x7681D14DU,
                0x2892ED69U,0xDAF96E6AU,0xC9A99D9EU,0x3BC21E9DU,0xEF087A76U,0x1D63F975U,0x0E330A81U,0xFC588982U,
                0xB21572C9U,0x407EF1CAU,0x532E023EU,0xA145813DU,0x758FE5D6U,0x87E466D5U,0x94B49521U,0x66DF1622U,
                0x38CC2A06U,0xCAA7A905U,0xD9F75AF1U,0x2B9CD9F2U,0xFF56BD19U,0x0D3D3E1AU,0x1E6DCDEEU,0xEC064EEDU,
                0xC38D26C4U,0x31E6A5C7U,0x22B65633U,0xD0DDD530U,0x0417B1DBU,0xF67C32D8U,0xE52CC12CU,0x1747422FU,
                0x49547E0BU,0xBB3FFD08U,0xA86F0EFCU,0x5A048DFFU,0x8ECEE914U,0x7CA56A17U,0x6FF599E3U,0x9D9E1AE0U,
                0xD3D3E1ABU,0x21B862A8U,0x32E8915CU,0xC083125FU,0x144976B4U,0xE622F5B7U,0xF5720643U,0x07198540U,
                0x590AB964U,0xAB613A67U,0xB831C993U,0x4A5A4A90U,0x9E902E7BU,0x6CFBAD78U,0x7FAB5E8CU,0x8DC0DD8FU,
                0xE330A81AU,0x115B2B19U,0x020BD8EDU,0xF0605BEEU,0x24AA3F05U,0xD6C1BC06U,0xC5914FF2U,0x37FACCF1U,
                0x69E9F0D5U,0x9B8273D6U,0x88D28022U,0x7AB90321U,0xAE7367CAU,0x5C18E4C9U,0x4F48173DU,0xBD23943EU,
                0xF36E6F75U,0x0105EC76U,0x12551F82U,0xE03E9C81U,0x34F4F86AU,0xC69F7B69U,0xD5CF889DU,0x27A40B9EU,
                0x79B737BAU,0x8BDCB4B9U,0x988C474DU,0x6AE7C44EU,0xBE2DA0A5U,0x4C4623A6U,0x5F16D052U,0xAD7D5351U,
            };

            /// <summary>
            /// Starting with the initial value in crc, accumulates a CRC32 value for unsigned 32-bit integer v, and stores the result in dst.
            /// </summary>
			/// <param name="crc">Initial value</param>
			/// <param name="v">Unsigned 32-bit integer</param>
			/// <returns>Result</returns>
            [DebuggerStepThrough]
            public static uint crc32_u32(uint crc, uint v)
            {
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v);
                return crc;
            }

            /// <summary>
            /// Starting with the initial value in crc, accumulates a CRC32 value for unsigned 8-bit integer v, and stores the result in dst.
            /// </summary>
			/// <param name="crc">Initial value</param>
			/// <param name="v">Unsigned 8-bit integer</param>
			/// <returns>Result</returns>
            [DebuggerStepThrough]
            public static uint crc32_u8(uint crc, byte v)
            {
                crc = (crc >> 8) ^ crctab[(crc ^ v) & 0xff];
                return crc;
            }

            /// <summary>
            /// Starting with the initial value in crc, accumulates a CRC32 value for unsigned 16-bit integer v, and stores the result in dst.
            /// </summary>
			/// <param name="crc">Initial value</param>
			/// <param name="v">Unsigned 16-bit integer</param>
			/// <returns>Result</returns>
            [DebuggerStepThrough]
            public static uint crc32_u16(uint crc, ushort v)
            {
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v);
                return crc;
            }

            /// <summary>
            /// Starting with the initial value in crc, accumulates a CRC32 value for unsigned 64-bit integer v, and stores the result in dst.
            /// </summary>
			/// <param name="crc_ul">Initial value</param>
			/// <param name="v">Signed 64-bit integer</param>
			/// <returns>Result</returns>
            [DebuggerStepThrough]
            [Obsolete("Use the ulong version of this intrinsic instead.")]
            public static ulong crc32_u64(ulong crc_ul, long v)
            {
                return crc32_u64(crc_ul, (ulong)v);
            }

            /// <summary>
            /// Starting with the initial value in crc, accumulates a CRC32 value for unsigned 64-bit integer v, and stores the result in dst.
            /// </summary>
			/// <param name="crc_ul">Initial value</param>
			/// <param name="v">Unsigned 64-bit integer</param>
			/// <returns>Result</returns>
            [DebuggerStepThrough]
            public static ulong crc32_u64(ulong crc_ul, ulong v)
            {
                uint crc = (uint)crc_ul;

                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v); v >>= 8;
                crc = crc32_u8(crc, (byte)v);

                return crc;
            }
        }
    }
}
