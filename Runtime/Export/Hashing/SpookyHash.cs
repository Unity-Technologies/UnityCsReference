// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEngine
{
    internal static class SpookyHash
    {
        static readonly bool AllowUnalignedRead = AttemptDetectAllowUnalignedRead();
        static bool AttemptDetectAllowUnalignedRead()
        {
            switch (SystemInfo.processorType)
            {
                case "x86":
                case "AMD64": // Known to tolerate unaligned-reads well.
                    return true;
            }

            return false;
        }

        // number of uint64's in internal state
        const int k_NumVars = 12;

        // size of the internal state
        const int k_BlockSize = k_NumVars * 8;

        // size of buffer of unhashed data, in bytes
        const int k_BufferSize = 2 * k_BlockSize;

        // DeadBeefConst: a constant which:
        //  * is not zero
        //  * is odd
        //  * is a not-very-regular mix of 1's and 0's
        //  * does not need any other special mathematical properties
        //
        const ulong k_DeadBeefConst = 0xdeadbeefdeadbeefL;

        [StructLayout(LayoutKind.Explicit)]
        unsafe struct U
        {
            [FieldOffset(0)]
            public byte* p8;
            [FieldOffset(0)]
            public uint* p32;
            [FieldOffset(0)]
            public ulong* p64;
            [FieldOffset(0)]
            public ulong i;

            public U(ushort* p8)
            {
                p32 = (uint*)0;
                p64 = (ulong*)0;
                i = 0;
                this.p8 = (byte*)p8;
            }
        }

        public static unsafe void Hash(
            void* message,
            ulong length,
            ulong* hash1,
            ulong* hash2
        )
        {
            if (length < k_BufferSize)
            {
                Short(message, length, hash1, hash2);
                return;
            }

            ulong h0, h1, h2, h3, h4, h5, h6, h7, h8, h9, h10, h11;
            ulong* buf = stackalloc ulong[k_NumVars];
            ulong* end;
            ulong remainder;

            h0 = h3 = h6 = h9 = *hash1;
            h1 = h4 = h7 = h10 = *hash2;
            h2 = h5 = h8 = h11 = k_DeadBeefConst;

            U u = new U((ushort*)message);
            end = u.p64 + (length / k_BlockSize) * k_NumVars;

            // handle all whole sc_blockSize blocks of bytes
            if (AllowUnalignedRead || ((u.i & 0x7) == 0))
            {
                while (u.p64 < end)
                {
                    Mix(
                        u.p64,
                        ref h0, ref h1, ref h2, ref h3,
                        ref h4, ref h5, ref h6, ref h7,
                        ref h8, ref h9, ref h10, ref h11
                    );
                    u.p64 += k_NumVars;
                }
            }
            else
            {
                while (u.p64 < end)
                {
                    UnsafeUtility.MemCpy(buf, u.p64, k_BlockSize);
                    Mix(
                        buf,
                        ref h0, ref h1, ref h2, ref h3,
                        ref h4, ref h5, ref h6, ref h7,
                        ref h8, ref h9, ref h10, ref h11
                    );
                    u.p64 += k_NumVars;
                }
            }

            // Handle the last partial block of sc_blockSize bytes.
            remainder = length - (ulong)((byte*)end - (byte*)message);
            UnsafeUtility.MemCpy(buf, end, (long)remainder);
            memset(((byte*)buf) + remainder, 0, k_BlockSize - remainder);
            ((byte*)buf)[k_BlockSize - 1] = (byte)remainder;

            // do some final mixing
            End(
                buf,
                ref h0, ref h1, ref h2, ref h3,
                ref h4, ref h5, ref h6, ref h7,
                ref h8, ref h9, ref h10, ref h11
            );
            *hash1 = h0;
            *hash2 = h1;
        }

        static unsafe void End(
            ulong* data,
            ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3,
            ref ulong h4, ref ulong h5, ref ulong h6, ref ulong h7,
            ref ulong h8, ref ulong h9, ref ulong h10, ref ulong h11
        )
        {
            h0 += data[0];   h1 += data[1];   h2 += data[2];   h3 += data[3];
            h4 += data[4];   h5 += data[5];   h6 += data[6];   h7 += data[7];
            h8 += data[8];   h9 += data[9];   h10 += data[10]; h11 += data[11];
            EndPartial(
                ref h0, ref h1, ref h2, ref h3,
                ref h4, ref h5, ref h6, ref h7,
                ref h8, ref h9, ref h10, ref h11
            );
            EndPartial(
                ref h0, ref h1, ref h2, ref h3,
                ref h4, ref h5, ref h6, ref h7,
                ref h8, ref h9, ref h10, ref h11
            );
            EndPartial(
                ref h0, ref h1, ref h2, ref h3,
                ref h4, ref h5, ref h6, ref h7,
                ref h8, ref h9, ref h10, ref h11
            );
        }

        //
        // Mix all 12 inputs together so that h0, h1 are a hash of them all.
        //
        // For two inputs differing in just the input bits
        // Where "differ" means xor or subtraction
        // And the base value is random, or a counting value starting at that bit
        // The final result will have each bit of h0, h1 flip
        // For every input bit,
        // with probability 50 +- .3%
        // For every pair of input bits,
        // with probability 50 +- 3%
        //
        // This does not rely on the last Mix() call having already mixed some.
        // Two iterations was almost good enough for a 64-bit result, but a
        // 128-bit result is reported, so End() does three iterations.
        //
        static unsafe void EndPartial(
            ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3,
            ref ulong h4, ref ulong h5, ref ulong h6, ref ulong h7,
            ref ulong h8, ref ulong h9, ref ulong h10, ref ulong h11
        )
        {
            h11 += h1;  h2 ^= h11;  Rot64(ref h1, 44);
            h0 += h2;   h3 ^= h0;   Rot64(ref h2, 15);
            h1 += h3;   h4 ^= h1;   Rot64(ref h3, 34);
            h2 += h4;   h5 ^= h2;   Rot64(ref h4, 21);
            h3 += h5;   h6 ^= h3;   Rot64(ref h5, 38);
            h4 += h6;   h7 ^= h4;   Rot64(ref h6, 33);
            h5 += h7;   h8 ^= h5;   Rot64(ref h7, 10);
            h6 += h8;   h9 ^= h6;   Rot64(ref h8, 13);
            h7 += h9;   h10 ^= h7;  Rot64(ref h9, 38);
            h8 += h10;  h11 ^= h8;  Rot64(ref h10, 53);
            h9 += h11;  h0 ^= h9;   Rot64(ref h11, 42);
            h10 += h0;  h1 ^= h10;  Rot64(ref h0, 54);
        }

        // left rotate a 64-bit value by k bytes
        static unsafe void Rot64(ref ulong x, int k)
        {
            x = (x << k) | (x >> (64 - k));
        }

        static unsafe void Short(
            void* message,
            ulong length,
            ulong* hash1,
            ulong* hash2
        )
        {
            ulong* buf = stackalloc ulong[2 * k_NumVars];
            U u = new U((ushort*)message);

            if (!AllowUnalignedRead && (u.i & 0x7) != 0)
            {
                UnsafeUtility.MemCpy(buf, message, (long)length);
                u.p64 = buf;
            }

            ulong remainder = length % 32;
            ulong a = *hash1;
            ulong b = *hash2;
            ulong c = k_DeadBeefConst;
            ulong d = k_DeadBeefConst;

            if (length > 15)
            {
                ulong* end = u.p64 + (length / 32) * 4;

                // handle all complete sets of 32 bytes
                for (; u.p64 < end; u.p64 += 4)
                {
                    c += u.p64[0];
                    d += u.p64[1];
                    ShortMix(ref a, ref b, ref c, ref d);
                    a += u.p64[2];
                    b += u.p64[3];
                }

                //Handle the case of 16+ remaining bytes.
                if (remainder >= 16)
                {
                    c += u.p64[0];
                    d += u.p64[1];
                    ShortMix(ref a, ref b, ref c, ref d);
                    u.p64 += 2;
                    remainder -= 16;
                }
            }

            // Handle the last 0..15 bytes, and its length
            d += length << 56;
            switch (remainder)
            {
                case 15:
                    d += ((ulong)u.p8[14]) << 48;
                    goto case 14;
                case 14:
                    d += ((ulong)u.p8[13]) << 40;
                    goto case 13;
                case 13:
                    d += ((ulong)u.p8[12]) << 32;
                    goto case 12;
                case 12:
                    d += u.p32[2];
                    c += u.p64[0];
                    break;
                case 11:
                    d += ((ulong)u.p8[10]) << 16;
                    goto case 10;
                case 10:
                    d += ((ulong)u.p8[9]) << 8;
                    goto case 9;
                case 9:
                    d += (ulong)u.p8[8];
                    goto case 8;
                case 8:
                    c += u.p64[0];
                    break;
                case 7:
                    c += ((ulong)u.p8[6]) << 48;
                    goto case 6;
                case 6:
                    c += ((ulong)u.p8[5]) << 40;
                    goto case 5;
                case 5:
                    c += ((ulong)u.p8[4]) << 32;
                    goto case 4;
                case 4:
                    c += u.p32[0];
                    break;
                case 3:
                    c += ((ulong)u.p8[2]) << 16;
                    goto case 2;
                case 2:
                    c += ((ulong)u.p8[1]) << 8;
                    goto case 1;
                case 1:
                    c += (ulong)u.p8[0];
                    break;
                case 0:
                    c += k_DeadBeefConst;
                    d += k_DeadBeefConst;
                    break;
            }
            ShortEnd(ref a, ref b, ref c, ref d);
            *hash1 = a;
            *hash2 = b;
        }

        //
        // The goal is for each bit of the input to expand into 128 bits of
        //   apparent entropy before it is fully overwritten.
        // n trials both set and cleared at least m bits of h0 h1 h2 h3
        //   n: 2   m: 29
        //   n: 3   m: 46
        //   n: 4   m: 57
        //   n: 5   m: 107
        //   n: 6   m: 146
        //   n: 7   m: 152
        // when run forwards or backwards
        // for all 1-bit and 2-bit diffs
        // with diffs defined by either xor or subtraction
        // with a base of all zeros plus a counter, or plus another bit, or random
        static unsafe void ShortMix(ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3)
        {
            Rot64(ref h2, 50); h2 += h3; h0 ^= h2;
            Rot64(ref h3, 52); h3 += h0; h1 ^= h3;
            Rot64(ref h0, 30); h0 += h1; h2 ^= h0;
            Rot64(ref h1, 41); h1 += h2; h3 ^= h1;
            Rot64(ref h2, 54); h2 += h3; h0 ^= h2;
            Rot64(ref h3, 48); h3 += h0; h1 ^= h3;
            Rot64(ref h0, 38); h0 += h1; h2 ^= h0;
            Rot64(ref h1, 37); h1 += h2; h3 ^= h1;
            Rot64(ref h2, 62); h2 += h3; h0 ^= h2;
            Rot64(ref h3, 34); h3 += h0; h1 ^= h3;
            Rot64(ref h0, 5); h0 += h1; h2 ^= h0;
            Rot64(ref h1, 36); h1 += h2; h3 ^= h1;
        }

        //
        // Mix all 4 inputs together so that h0, h1 are a hash of them all.
        //
        // For two inputs differing in just the input bits
        // Where "differ" means xor or subtraction
        // And the base value is random, or a counting value starting at that bit
        // The final result will have each bit of h0, h1 flip
        // For every input bit,
        // with probability 50 +- .3% (it is probably better than that)
        // For every pair of input bits,
        // with probability 50 +- .75% (the worst case is approximately that)
        //
        static unsafe void ShortEnd(ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3)
        {
            h3 ^= h2; Rot64(ref h2, 15); h3 += h2;
            h0 ^= h3; Rot64(ref h3, 52); h0 += h3;
            h1 ^= h0; Rot64(ref h0, 26); h1 += h0;
            h2 ^= h1; Rot64(ref h1, 51); h2 += h1;
            h3 ^= h2; Rot64(ref h2, 28); h3 += h2;
            h0 ^= h3; Rot64(ref h3, 9);  h0 += h3;
            h1 ^= h0; Rot64(ref h0, 47); h1 += h0;
            h2 ^= h1; Rot64(ref h1, 54); h2 += h1;
            h3 ^= h2; Rot64(ref h2, 32); h3 += h2;
            h0 ^= h3; Rot64(ref h3, 25); h0 += h3;
            h1 ^= h0; Rot64(ref h0, 63); h1 += h0;
        }

        //
        // This is used if the input is 96 bytes long or longer.
        //
        // The internal state is fully overwritten every 96 bytes.
        // Every input bit appears to cause at least 128 bits of entropy
        // before 96 other bytes are combined, when run forward or backward
        //   For every input bit,
        //   Two inputs differing in just that input bit
        //   Where "differ" means xor or subtraction
        //   And the base value is random
        //   When run forward or backwards one Mix
        // I tried 3 pairs of each; they all differed by at least 212 bits.
        //
        static unsafe void Mix(
            ulong* data,
            ref ulong s0, ref ulong s1, ref ulong s2, ref ulong s3,
            ref ulong s4, ref ulong s5, ref ulong s6, ref ulong s7,
            ref ulong s8, ref ulong s9, ref ulong s10, ref ulong s11
        )
        {
            s0 += data[0];   s2 ^= s10; s11 ^= s0;  Rot64(ref s0, 11);  s11 += s1;
            s1 += data[1];   s3 ^= s11; s0 ^= s1;   Rot64(ref s1, 32);  s0 += s2;
            s2 += data[2];   s4 ^= s0;  s1 ^= s2;   Rot64(ref s2, 43);  s1 += s3;
            s3 += data[3];   s5 ^= s1;  s2 ^= s3;   Rot64(ref s3, 31);  s2 += s4;
            s4 += data[4];   s6 ^= s2;  s3 ^= s4;   Rot64(ref s4, 17);  s3 += s5;
            s5 += data[5];   s7 ^= s3;  s4 ^= s5;   Rot64(ref s5, 28);  s4 += s6;
            s6 += data[6];   s8 ^= s4;  s5 ^= s6;   Rot64(ref s6, 39);  s5 += s7;
            s7 += data[7];   s9 ^= s5;  s6 ^= s7;   Rot64(ref s7, 57);  s6 += s8;
            s8 += data[8];   s10 ^= s6; s7 ^= s8;   Rot64(ref s8, 55);  s7 += s9;
            s9 += data[9];   s11 ^= s7; s8 ^= s9;   Rot64(ref s9, 54);  s8 += s10;
            s10 += data[10]; s0 ^= s8;  s9 ^= s10;  Rot64(ref s10, 22); s9 += s11;
            s11 += data[11]; s1 ^= s9;  s10 ^= s11; Rot64(ref s11, 46); s10 += s0;
        }

        static unsafe void memset(void* dst, int value, ulong numberOfBytes)
        {
            // Copy per 8 bytes
            {
                ulong v = ((uint)value) | ((uint)value << 32);
                ulong* dst8 = (ulong*)dst;
                var count = numberOfBytes >> 3; // divide by 8
                ulong i = 0;
                for (; i < count; ++i)
                    dst8[i] = v;

                // Get remainder
                dst = (void*)dst8;
                numberOfBytes -= count;
            }

            // Copy per byte
            {
                byte* v = stackalloc byte[4];
                v[0] = (byte)(((uint)value) & 0xF);
                v[1] = (byte)((((uint)value) >> 4) & 0xF);
                v[2] = (byte)((((uint)value) >> 8) & 0xF);
                v[3] = (byte)((((uint)value) >> 12) & 0xF);
                byte* dst1 = (byte*)dst;
                var count = numberOfBytes; // remainder
                ulong i = 0;
                for (; i < count; ++i)
                    dst1[i] = v[i % 4];
            }
        }
    }
}
