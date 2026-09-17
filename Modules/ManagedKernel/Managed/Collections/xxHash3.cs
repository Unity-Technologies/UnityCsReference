// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Collections
{
    /// <summary>
    /// A feature complete hashing API based on xxHash3 (https://github.com/Cyan4973/xxHash)
    /// </summary>
    /// <remarks>
    /// Features:
    ///  - Compute 64bits or 128bits hash keys, based on a private key, with an optional given seed value.
    ///  - Hash on buffer (with or without a ulong based seed value)
    ///  - Hash on buffer while copying the data to a destination
    ///  - Use instances of <see cref="xxHash3.StreamingState"/> to accumulate data to hash in multiple calls, suited for small data, then retrieve the hash key at the end.
    ///  - xxHash3 has several implementation based on the size to hash to ensure best performances
    ///  - We currently have two implementations:
    ///    - A generic one based on Unity.Mathematics, that should always be executed compiled with Burst.
    ///    - An AVX2 based implementation for platforms supporting it, using Burst intrinsics.
    ///  - Whether or not the call site is compiled with burst, the hashing function will be executed by Burst(*) to ensure optimal performance.
    ///    (*) Only when the hashing size justifies such transition.
    /// </remarks>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public static partial class xxHash3
    {
        #region Public API

        /// <summary>
        /// Compute a 64bits hash of a memory region
        /// </summary>
        /// <param name="input">The memory buffer, can't be null</param>
        /// <param name="length">The length of the memory buffer, can be zero</param>
        /// <returns>The hash result</returns>
        public static unsafe uint2 Hash64(void* input, long length)
        {
            fixed (void* secret = xxHashDefaultKey.kSecret)
            {
                return ToUint2(Hash64Internal((byte*) input, null, length, (byte*) secret, 0));
            }
        }


        /// <summary>
        /// Compute a 64bits hash from the contents of the input struct
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="input">The input struct that will be hashed</param>
        /// <returns>The hash result</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static unsafe uint2 Hash64<T>(in T input) where T : unmanaged
        {
            return Hash64(UnsafeUtilityExtensions.AddressOf(input), sizeof(T));
        }

        /// <summary>
        /// Compute a 64bits hash of a memory region using a given seed value
        /// </summary>
        /// <param name="input">The memory buffer, can't be null</param>
        /// <param name="length">The length of the memory buffer, can be zero</param>
        /// <param name="seed">The seed value to alter the hash computation from</param>
        /// <returns>The hash result</returns>
        public static unsafe uint2 Hash64(void* input, long length, ulong seed)
        {
            fixed (byte* secret = xxHashDefaultKey.kSecret)
            {
                return ToUint2(Hash64Internal((byte*) input, null, length, secret, seed));
            }
        }

        /// <summary>
        /// Compute a 128bits hash of a memory region
        /// </summary>
        /// <param name="input">The memory buffer, can't be null</param>
        /// <param name="length">The length of the memory buffer, can be zero</param>
        /// <returns>The hash result</returns>
        public static unsafe uint4 Hash128(void* input, long length)
        {
            fixed (void* secret = xxHashDefaultKey.kSecret)
            {
                Hash128Internal((byte*) input, null, length, (byte*) secret, 0, out var result);
                return result;
            }
        }

        /// <summary>
        /// Compute a 128bits hash from the contents of the input struct
        /// </summary>
        /// <typeparam name="T">The input type.</typeparam>
        /// <param name="input">The input struct that will be hashed</param>
        /// <returns>The hash result</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static unsafe uint4 Hash128<T>(in T input) where T : unmanaged
        {
            return Hash128(UnsafeUtilityExtensions.AddressOf(input), sizeof(T));
        }

        /// <summary>
        /// Compute a 128bits hash while copying the data to a destination buffer
        /// </summary>
        /// <param name="input">The memory buffer to compute the hash and copy from, can't be null</param>
        /// <param name="destination">The destination buffer, can't be null and must be at least big enough to match the input's length</param>
        /// <param name="length">The length of the memory buffer, can be zero</param>
        /// <returns>The hash result</returns>
        /// <remarks>Use this API to avoid a double memory scan in situations where the hash as to be compute and the data copied at the same time. Performances improvements vary between 30-50% on
        /// big data.</remarks>
        public static unsafe uint4 Hash128(void* input, void* destination, long length)
        {
            fixed (byte* secret = xxHashDefaultKey.kSecret)
            {
                Hash128Internal((byte*) input, (byte*) destination, length, secret, 0, out var result);

                return result;
            }
        }

        /// <summary>
        /// Compute a 128bits hash of a memory region using a given seed value
        /// </summary>
        /// <param name="input">The memory buffer, can't be null</param>
        /// <param name="length">The length of the memory buffer, can be zero</param>
        /// <param name="seed">The seed value to alter the hash computation from</param>
        /// <returns>The hash result</returns>
        public static unsafe uint4 Hash128(void* input, long length, ulong seed)
        {
            fixed (byte* secret = xxHashDefaultKey.kSecret)
            {
                Hash128Internal((byte*) input, null, length, secret, seed, out var result);

                return result;
            }
        }

        /// <summary>
        /// Compute a 128bits hash while copying the data to a destination buffer using a given seed value
        /// </summary>
        /// <param name="input">The memory buffer to compute the hash and copy from, can't be null</param>
        /// <param name="destination">The destination buffer, can't be null and must be at least big enough to match the input's length</param>
        /// <param name="length">The length of the memory buffer, can be zero</param>
        /// <param name="seed">The seed value to alter the hash computation from</param>
        /// <returns>The hash result</returns>
        public static unsafe uint4 Hash128(void* input, void* destination, long length, ulong seed)
        {
            fixed (byte* secret = xxHashDefaultKey.kSecret)
            {
                Hash128Internal((byte*) input, (byte*) destination, length, secret, seed, out var result);

                return result;
            }
        }

        #endregion

        #region Constants

        private const int STRIPE_LEN = 64;
        private const int ACC_NB = STRIPE_LEN / 8; // Accumulators are ulong sized
        private const int SECRET_CONSUME_RATE = 8;
        private const int SECRET_KEY_SIZE = 192;
        private const int SECRET_KEY_MIN_SIZE = 136;
        private const int SECRET_LASTACC_START = 7;
        private const int NB_ROUNDS = (SECRET_KEY_SIZE - STRIPE_LEN) / SECRET_CONSUME_RATE;
        private const int BLOCK_LEN = STRIPE_LEN * NB_ROUNDS;

        private const uint PRIME32_1 = 0x9E3779B1U;
        private const uint PRIME32_2 = 0x85EBCA77U;

        private const uint PRIME32_3 = 0xC2B2AE3DU;

        // static readonly uint PRIME32_4 = 0x27D4EB2FU;
        private const uint PRIME32_5 = 0x165667B1U;
        private const ulong PRIME64_1 = 0x9E3779B185EBCA87UL;
        private const ulong PRIME64_2 = 0xC2B2AE3D27D4EB4FUL;
        private const ulong PRIME64_3 = 0x165667B19E3779F9UL;
        private const ulong PRIME64_4 = 0x85EBCA77C2B2AE63UL;
        private const ulong PRIME64_5 = 0x27D4EB2F165667C5UL;

        private const int MIDSIZE_MAX = 240;
        private const int MIDSIZE_STARTOFFSET = 3;
        private const int MIDSIZE_LASTOFFSET = 17;

        private const int SECRET_MERGEACCS_START = 11;

        #endregion

        private struct ulong2
        {
            public ulong x;
            public ulong y;

            public ulong2(ulong x, ulong y)
            {
                this.x = x;
                this.y = y;
            }
        }

        internal static unsafe ulong Hash64Internal(byte* input, byte* dest, long length, byte* secret, ulong seed)
        {
            if (dest != null && length < MIDSIZE_MAX)
            {
                UnsafeUtility.MemCpy(dest, input, length);
            }

            if (length <= 16)
            {
                return Hash64Len0To16(input, length, secret, seed);
            }

            if (length <= 128)
            {
                return Hash64Len17To128(input, length, secret, seed);
            }

            if (length <= MIDSIZE_MAX)
            {
                return Hash64Len129To240(input, length, secret, seed);
            }

            if (seed != 0)
            {
                var addr = stackalloc byte[SECRET_KEY_SIZE + 31];

                // Aligned the allocated address on 32 bytes
                var newSecret = (byte*)((ulong)addr + 31 & 0xFFFFFFFFFFFFFFE0);

                EncodeSecretKey(newSecret, secret, seed);
                return Hash64Long(input, dest, length, newSecret);
            }

            return Hash64Long(input, dest, length, secret);
        }

        internal static unsafe void Hash128Internal(byte* input, byte* dest, long length, byte* secret, ulong seed, out uint4 result)
        {
            if (dest != null && length < MIDSIZE_MAX)
            {
                UnsafeUtility.MemCpy(dest, input, length);
            }

            if (length <= 16)
            {
                Hash128Len0To16(input, length, secret, seed, out result);
                return;
            }

            if (length <= 128)
            {
                Hash128Len17To128(input, length, secret, seed, out result);
                return;
            }

            if (length <= MIDSIZE_MAX)
            {
                Hash128Len129To240(input, length, secret, seed, out result);
                return;
            }

            if (seed != 0)
            {
                var addr = stackalloc byte[SECRET_KEY_SIZE + 31];

                // Aligned the allocated address on 32 bytes
                var newSecret = (byte*) ((ulong) addr + 31 & 0xFFFFFFFFFFFFFFE0);

                EncodeSecretKey(newSecret, secret, seed);
                Hash128Long(input, dest, length, newSecret, out result);
            }
            else
            {
                Hash128Long(input, dest, length, secret, out result);
            }
        }

        #region 64-bits hash, size dependent implementations

        private static unsafe ulong Hash64Len1To3(byte* input, long len, byte* secret, ulong seed)
        {
            unchecked
            {
                var c1 = input[0];
                var c2 = input[len >> 1];
                var c3 = input[len - 1];
                var combined = ((uint)c1 << 16) | ((uint)c2  << 24) | ((uint)c3 <<  0) | ((uint)len << 8);
                ulong bitflip = (Read32LE(secret) ^ Read32LE(secret+4)) + seed;
                ulong keyed = (ulong)combined ^ bitflip;
                return AvalancheH64(keyed);
            }
        }

        private static unsafe ulong Hash64Len4To8(byte* input, long length, byte* secret, ulong seed)
        {
            unchecked
            {
                seed ^= (ulong)Swap32((uint)seed) << 32;
                var input1 = Read32LE(input);
                var input2 = Read32LE(input + length - 4);
                var bitflip = (Read64LE(secret+8) ^ Read64LE(secret+16)) - seed;
                var input64 = input2 + (((ulong)input1) << 32);
                var keyed = input64 ^ bitflip;
                return rrmxmx(keyed, (ulong)length);
            }
        }

        private static unsafe ulong Hash64Len9To16(byte* input, long length, byte* secret, ulong seed)
        {
            unchecked
            {
                var bitflip1 = (Read64LE(secret+24) ^ Read64LE(secret+32)) + seed;
                var bitflip2 = (Read64LE(secret+40) ^ Read64LE(secret+48)) - seed;
                var input_lo = Read64LE(input) ^ bitflip1;
                var input_hi = Read64LE(input + length - 8) ^ bitflip2;
                var acc = (ulong)length + Swap64(input_lo) + input_hi + Mul128Fold64(input_lo, input_hi);
                return Avalanche(acc);
            }
        }

        private static unsafe ulong Hash64Len0To16(byte* input, long length, byte* secret, ulong seed)
        {
            if (length > 8)
            {
                return Hash64Len9To16(input, length, secret, seed);
            }

            if (length >= 4)
            {
                return Hash64Len4To8(input, length, secret, seed);
            }

            if (length > 0)
            {
                return Hash64Len1To3(input, length, secret, seed);
            }

            return AvalancheH64(seed ^ (Read64LE(secret+56) ^ Read64LE(secret+64)));
        }

        private static unsafe ulong Hash64Len17To128(byte* input, long length, byte* secret, ulong seed)
        {
            unchecked
            {
                var acc = (ulong) length * PRIME64_1;
                if (length > 32)
                {
                    if (length > 64)
                    {
                        if (length > 96)
                        {
                            acc += Mix16(input + 48, secret + 96, seed);
                            acc += Mix16(input + length - 64, secret + 112, seed);
                        }

                        acc += Mix16(input + 32, secret + 64, seed);
                        acc += Mix16(input + length - 48, secret + 80, seed);
                    }

                    acc += Mix16(input + 16, secret + 32, seed);
                    acc += Mix16(input + length - 32, secret + 48, seed);
                }

                acc += Mix16(input + 0, secret + 0, seed);
                acc += Mix16(input + length - 16, secret + 16, seed);

                return Avalanche(acc);
            }
        }

        private static unsafe ulong Hash64Len129To240(byte* input, long length, byte* secret, ulong seed)
        {
            unchecked
            {
                var acc = (ulong) length * PRIME64_1;
                var nbRounds = (int) length / 16;
                for (var i = 0; i < 8; i++)
                {
                    acc += Mix16(input + (16 * i), secret + (16 * i), seed);
                }

                acc = Avalanche(acc);

                for (var i = 8; i < nbRounds; i++)
                {
                    acc += Mix16(input + (16 * i), secret + (16 * (i - 8)) + MIDSIZE_STARTOFFSET, seed);
                }

                acc += Mix16(input + length - 16, secret + SECRET_KEY_MIN_SIZE - MIDSIZE_LASTOFFSET, seed);
                return Avalanche(acc);
            }
        }

        [BurstCompile]
        private static unsafe ulong Hash64Long(byte* input, byte* dest, long length, byte* secret)
        {
            var addr = stackalloc byte[STRIPE_LEN + 31];
            var acc = (ulong*) ((ulong) addr + 31 & 0xFFFFFFFFFFFFFFE0); // Aligned the allocated address on 32 bytes
            acc[0] = PRIME32_3;
            acc[1] = PRIME64_1;
            acc[2] = PRIME64_2;
            acc[3] = PRIME64_3;
            acc[4] = PRIME64_4;
            acc[5] = PRIME32_2;
            acc[6] = PRIME64_5;
            acc[7] = PRIME32_1;

            unchecked
            {
                if (X86.Avx2.IsAvx2Supported)
                {
                    Avx2HashLongInternalLoop(acc, input, dest, length, secret, 1);
                }
                else
                {
                    DefaultHashLongInternalLoop(acc, input, dest, length, secret, 1);
                }
                return MergeAcc(acc, secret + SECRET_MERGEACCS_START, (ulong) length * PRIME64_1);
            }
        }

        #endregion

        #region 128-bits hash, size dependent implementations

        private static unsafe void Hash128Len1To3(byte* input, long length, byte* secret, ulong seed,
            out uint4 result)
        {
            unchecked
            {
                var c1 = input[0];
                var c2 = input[length >> 1];
                var c3 = input[length - 1];
                var combinedl = ((uint) c1 << 16) + (((uint) c2) << 24) + (((uint) c3) << 0) + (((uint) length) << 8);
                var combinedh = RotL32(Swap32(combinedl), 13);
                var bitflipl = (Read32LE(secret) ^ Read32LE(secret+4)) + seed;
                var bitfliph = (Read32LE(secret+8) ^ Read32LE(secret+12)) - seed;
                var keyed_lo = combinedl ^ bitflipl;
                var keyed_hi = combinedh ^ bitfliph;

                result = ToUint4(AvalancheH64(keyed_lo), AvalancheH64(keyed_hi));
            }
        }

        private static unsafe void Hash128Len4To8(byte* input, long len, byte* secret, ulong seed,
            out uint4 result)
        {
            unchecked
            {
                seed ^= (ulong)Swap32((uint)seed) << 32;
                var input_lo = Read32LE(input);
                var input_hi = Read32LE(input + len - 4);
                var input_64 = input_lo + ((ulong)input_hi << 32);
                var bitflip = (Read64LE(secret+16) ^ Read64LE(secret+24)) + seed;
                var keyed = input_64 ^ bitflip;

                var low = Common.umul128(keyed, PRIME64_1 + (ulong)(len << 2), out var high);

                high += (low << 1);
                low ^= (high >> 3);

                low = XorShift64(low, 35);
                low*= 0x9FB21C651E98DF25UL;
                low = XorShift64(low, 28);
                high = Avalanche(high);
                result = ToUint4(low, high);
            }
        }

        private static unsafe void Hash128Len9To16(byte* input, long len, byte* secret, ulong seed,
            out uint4 result)
        {
            unchecked
            {
                var bitflipl = (Read64LE(secret+32) ^ Read64LE(secret+40)) - seed;
                var bitfliph = (Read64LE(secret+48) ^ Read64LE(secret+56)) + seed;
                var input_lo = Read64LE(input);
                var input_hi = Read64LE(input + len - 8);
                var low = Common.umul128(input_lo ^ input_hi ^ bitflipl, PRIME64_1, out var high);

                low += (ulong)(len - 1) << 54;
                input_hi   ^= bitfliph;
                high += input_hi + Mul32To64((uint)input_hi, PRIME32_2 - 1);
                low  ^= Swap64(high);

                var hlow = Common.umul128(low, PRIME64_2, out var hhigh);
                hhigh += high * PRIME64_2;

                result = ToUint4(Avalanche(hlow), Avalanche(hhigh));
            }
        }

        private static unsafe void Hash128Len0To16(byte* input, long length, byte* secret, ulong seed,
            out uint4 result)
        {
            if (length > 8)
            {
                Hash128Len9To16(input, length, secret, seed, out result);
                return;
            }

            if (length >= 4)
            {
                Hash128Len4To8(input, length, secret, seed, out result);
                return;
            }

            if (length > 0)
            {
                Hash128Len1To3(input, length, secret, seed, out result);
                return;
            }

            var bitflipl = Read64LE(secret+64) ^ Read64LE(secret+72);
            var bitfliph = Read64LE(secret+80) ^ Read64LE(secret+88);
            var low = AvalancheH64(seed ^ bitflipl);
            var hi = AvalancheH64( seed ^ bitfliph);
            result = ToUint4(low, hi);
        }

        private static unsafe void Hash128Len17To128(byte* input, long length, byte* secret, ulong seed,
            out uint4 result)
        {
            unchecked
            {
                var acc = new ulong2((ulong) length * PRIME64_1, 0);
                if (length > 32)
                {
                    if (length > 64)
                    {
                        if (length > 96)
                        {
                            acc = Mix32(acc, input + 48, input + length - 64, secret + 96, seed);
                        }

                        acc = Mix32(acc, input + 32, input + length - 48, secret + 64, seed);
                    }

                    acc = Mix32(acc, input + 16, input + length - 32, secret + 32, seed);
                }

                acc = Mix32(acc, input, input + length - 16, secret, seed);

                var low64 = acc.x + acc.y;
                var high64 = acc.x * PRIME64_1 + acc.y * PRIME64_4 + ((ulong) length - seed) * PRIME64_2;

                result = ToUint4(Avalanche(low64), 0ul - Avalanche(high64));
            }
        }

        private static unsafe void Hash128Len129To240(byte* input, long length, byte* secret, ulong seed,
            out uint4 result)
        {
            unchecked
            {
                var acc = new ulong2((ulong) length * PRIME64_1, 0);
                var nbRounds = length / 32;
                int i;

                for (i = 0; i < 4; i++)
                {
                    acc = Mix32(acc, input + 32 * i, input + 32 * i + 16, secret + 32 * i, seed);
                }

                acc.x = Avalanche(acc.x);
                acc.y = Avalanche(acc.y);

                for (i = 4; i < nbRounds; i++)
                {
                    acc = Mix32(acc, input + 32 * i, input + 32 * i + 16, secret + MIDSIZE_STARTOFFSET + 32 * (i - 4),
                        seed);
                }

                acc = Mix32(acc, input + length - 16, input + length - 32,
                    secret + SECRET_KEY_MIN_SIZE - MIDSIZE_LASTOFFSET - 16, 0UL - seed);

                var low64 = acc.x + acc.y;
                var high64 = acc.x * PRIME64_1 + acc.y * PRIME64_4 + ((ulong) length - seed) * PRIME64_2;

                result = ToUint4(Avalanche(low64), 0ul - Avalanche(high64));
            }
        }

        [BurstCompile]
        private static unsafe void Hash128Long(byte* input, byte* dest, long length, byte* secret, out uint4 result)
        {
            // var acc = stackalloc ulong[ACC_NB];
            var addr = stackalloc byte[STRIPE_LEN + 31];
            var acc = (ulong*) ((ulong) addr + 31 & 0xFFFFFFFFFFFFFFE0); // Aligned the allocated address on 32 bytes
            acc[0] = PRIME32_3;
            acc[1] = PRIME64_1;
            acc[2] = PRIME64_2;
            acc[3] = PRIME64_3;
            acc[4] = PRIME64_4;
            acc[5] = PRIME32_2;
            acc[6] = PRIME64_5;
            acc[7] = PRIME32_1;

            unchecked
            {
                if (X86.Avx2.IsAvx2Supported)
                {
                    Avx2HashLongInternalLoop(acc, input, dest, length, secret, 0);
                }
                else
                {
                    DefaultHashLongInternalLoop(acc, input, dest, length, secret, 0);
                }

                var low64 = MergeAcc(acc, secret + SECRET_MERGEACCS_START, (ulong) length * PRIME64_1);
                var high64 = MergeAcc(acc, secret + SECRET_KEY_SIZE - 64 - SECRET_MERGEACCS_START,
                    ~((ulong) length * PRIME64_2));

                result = ToUint4(low64, high64);
            }
        }

        #endregion

        #region Internal helpers

        internal static uint2 ToUint2(ulong u)
        {
            return new uint2((uint)(u & 0xFFFFFFFF), (uint)(u >> 32));
        }

        internal static uint4 ToUint4(ulong ul0, ulong ul1)
        {
            return new uint4((uint)(ul0 & 0xFFFFFFFF), (uint)(ul0 >> 32), (uint)(ul1 & 0xFFFFFFFF), (uint)(ul1 >> 32));
        }

        internal static unsafe void EncodeSecretKey(byte* dst, byte* secret, ulong seed)
        {
            unchecked
            {
                var seedInitCount = SECRET_KEY_SIZE / (8 * 2);
                for (var i = 0; i < seedInitCount; i++)
                {
                    Write64LE(dst + 16 * i + 0, Read64LE(secret + 16 * i + 0) + seed);
                    Write64LE(dst + 16 * i + 8, Read64LE(secret + 16 * i + 8) - seed);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong Read64LE(void* addr) => Unsafe.ReadUnaligned<ulong>(addr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Read32LE(void* addr) => Unsafe.ReadUnaligned<uint>(addr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Write64LE(void* addr, ulong value) => Unsafe.WriteUnaligned(addr, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Mul32To64(uint x, uint y) => (ulong) x * y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Swap64(ulong x)
        {
            return ((x << 56) & 0xff00000000000000UL) |
                   ((x << 40) & 0x00ff000000000000UL) |
                   ((x << 24) & 0x0000ff0000000000UL) |
                   ((x <<  8) & 0x000000ff00000000UL) |
                   ((x >>  8) & 0x00000000ff000000UL) |
                   ((x >> 24) & 0x0000000000ff0000UL) |
                   ((x >> 40) & 0x000000000000ff00UL) |
                   ((x >> 56) & 0x00000000000000ffUL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Swap32(uint x)
        {
            return ((x << 24) & 0xff000000) |
                   ((x <<  8) & 0x00ff0000) |
                   ((x >>  8) & 0x0000ff00) |
                   ((x >> 24) & 0x000000ff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotL32(uint x, int r) => (((x) << (r)) | ((x) >> (32 - (r))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotL64(ulong x, int r) => (((x) << (r)) | ((x) >> (64 - (r))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong XorShift64(ulong v64, int shift)
        {
            return v64 ^ (v64 >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Mul128Fold64(ulong lhs, ulong rhs)
        {
            var lo = Common.umul128(lhs, rhs, out var hi);
            return lo ^ hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong Mix16(byte* input, byte* secret, ulong seed)
        {
            var input_lo = Read64LE(input);
            var input_hi = Read64LE(input + 8);
            return Mul128Fold64(
                input_lo ^ (Read64LE(secret + 0) + seed),
                input_hi ^ (Read64LE(secret + 8) - seed));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong2 Mix32(ulong2 acc, byte* input_1, byte* input_2, byte* secret, ulong seed)
        {
            unchecked
            {
                var l0 = acc.x + Mix16(input_1, secret + 0, seed);
                l0 ^= Read64LE(input_2) + Read64LE(input_2 + 8);

                var l1 = acc.y + Mix16(input_2, secret + 16, seed);
                l1 ^= Read64LE(input_1) + Read64LE(input_1 + 8);

                return new ulong2(l0, l1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Avalanche(ulong h64)
        {
            unchecked
            {
                h64 = XorShift64(h64, 37);
                h64 *= 0x165667919E3779F9UL;
                h64 = XorShift64(h64, 32);
                return h64;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AvalancheH64(ulong h64)
        {
            unchecked
            {
                h64 ^= h64 >> 33;
                h64 *= PRIME64_2;
                h64 ^= h64 >> 29;
                h64 *= PRIME64_3;
                h64 ^= h64 >> 32;
                return h64;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong rrmxmx(ulong h64, ulong length)
        {
            h64 ^= RotL64(h64, 49) ^ RotL64(h64, 24);
            h64 *= 0x9FB21C651E98DF25UL;
            h64 ^= (h64 >> 35) + length ;
            h64 *= 0x9FB21C651E98DF25UL;
            return XorShift64(h64, 28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong Mix2Acc(ulong acc0, ulong acc1, byte* secret)
        {
            return Mul128Fold64(acc0 ^ Read64LE(secret), acc1 ^ Read64LE(secret+8));
        }

        internal static unsafe ulong MergeAcc(ulong* acc, byte* secret, ulong start)
        {
            unchecked
            {
                var result64 = start;

                result64 += Mix2Acc(acc[0], acc[1], secret + 0);
                result64 += Mix2Acc(acc[2], acc[3], secret + 16);
                result64 += Mix2Acc(acc[4], acc[5], secret + 32);
                result64 += Mix2Acc(acc[6], acc[7], secret + 48);

                return Avalanche(result64);
            }
        }

        #endregion

        #region Default Implementation

        private static unsafe void DefaultHashLongInternalLoop(ulong* acc, byte* input, byte* dest, long length, byte* secret, int isHash64)
        {
            // Process packets of 512 bits
            var nb_blocks = (length-1) / BLOCK_LEN;
            for (int n = 0; n < nb_blocks; n++)
            {
                DefaultAccumulate(acc, input + n * BLOCK_LEN, dest == null ? null : dest + n * BLOCK_LEN, secret,
                    NB_ROUNDS, isHash64);
                DefaultScrambleAcc(acc, secret + SECRET_KEY_SIZE - STRIPE_LEN);
            }

            var nbStripes = ((length-1) - (BLOCK_LEN * nb_blocks)) / STRIPE_LEN;
            DefaultAccumulate(acc, input + nb_blocks * BLOCK_LEN, dest == null ? null : dest + nb_blocks * BLOCK_LEN,
                secret, nbStripes, isHash64);

            var p = input + length - STRIPE_LEN;
            DefaultAccumulate512(acc, p, null, secret + SECRET_KEY_SIZE - STRIPE_LEN - SECRET_LASTACC_START,
                isHash64);

            if (dest != null)
            {
                var remaining = length % STRIPE_LEN;
                if (remaining != 0)
                {
                    UnsafeUtility.MemCpy(dest + length - remaining, input + length - remaining, remaining);
                }
            }
        }

        internal static unsafe void DefaultAccumulate(ulong* acc, byte* input, byte* dest, byte* secret, long nbStripes, int isHash64)
        {
            for (int n = 0; n < nbStripes; n++)
            {
                DefaultAccumulate512(acc, input + n * STRIPE_LEN, dest == null ? null : dest + n * STRIPE_LEN,
                    secret + n * SECRET_CONSUME_RATE, isHash64);
            }
        }

        internal static unsafe void DefaultAccumulate512(ulong* acc, byte* input, byte* dest, byte* secret, int isHash64)
        {
            var count = ACC_NB;
            for (var i = 0; i < count; i++)
            {
                var data_val = Read64LE(input + 8 * i);
                var data_key = data_val ^ Read64LE(secret + i * 8);

                if (dest != null)
                {
                    Write64LE(dest + 8 * i, data_val);
                }

                acc[i ^ 1] += data_val;
                acc[i] += Mul32To64((uint) (data_key & 0xFFFFFFFF), (uint) (data_key >> 32));
            }
        }

        internal static unsafe void DefaultScrambleAcc(ulong* acc, byte* secret)
        {
            for (var i = 0; i < ACC_NB; i++)
            {
                var key64 = Read64LE(secret + 8 * i);
                var acc64 = acc[i];
                acc64 = XorShift64(acc64, 47);
                acc64 ^= key64;
                acc64 *= PRIME32_1;
                acc[i] = acc64;
            }
        }

        #endregion
    }

    static class xxHashDefaultKey
    {
        // The default xxHash3 encoding key, other implementations of this algorithm should use the same key to produce identical hashes
        public static readonly byte[] kSecret =
        {
            0xb8, 0xfe, 0x6c, 0x39, 0x23, 0xa4, 0x4b, 0xbe, 0x7c, 0x01, 0x81, 0x2c, 0xf7, 0x21, 0xad, 0x1c,
            0xde, 0xd4, 0x6d, 0xe9, 0x83, 0x90, 0x97, 0xdb, 0x72, 0x40, 0xa4, 0xa4, 0xb7, 0xb3, 0x67, 0x1f,
            0xcb, 0x79, 0xe6, 0x4e, 0xcc, 0xc0, 0xe5, 0x78, 0x82, 0x5a, 0xd0, 0x7d, 0xcc, 0xff, 0x72, 0x21,
            0xb8, 0x08, 0x46, 0x74, 0xf7, 0x43, 0x24, 0x8e, 0xe0, 0x35, 0x90, 0xe6, 0x81, 0x3a, 0x26, 0x4c,
            0x3c, 0x28, 0x52, 0xbb, 0x91, 0xc3, 0x00, 0xcb, 0x88, 0xd0, 0x65, 0x8b, 0x1b, 0x53, 0x2e, 0xa3,
            0x71, 0x64, 0x48, 0x97, 0xa2, 0x0d, 0xf9, 0x4e, 0x38, 0x19, 0xef, 0x46, 0xa9, 0xde, 0xac, 0xd8,
            0xa8, 0xfa, 0x76, 0x3f, 0xe3, 0x9c, 0x34, 0x3f, 0xf9, 0xdc, 0xbb, 0xc7, 0xc7, 0x0b, 0x4f, 0x1d,
            0x8a, 0x51, 0xe0, 0x4b, 0xcd, 0xb4, 0x59, 0x31, 0xc8, 0x9f, 0x7e, 0xc9, 0xd9, 0x78, 0x73, 0x64,

            0xea, 0xc5, 0xac, 0x83, 0x34, 0xd3, 0xeb, 0xc3, 0xc5, 0x81, 0xa0, 0xff, 0xfa, 0x13, 0x63, 0xeb,
            0x17, 0x0d, 0xdd, 0x51, 0xb7, 0xf0, 0xda, 0x49, 0xd3, 0x16, 0x55, 0x26, 0x29, 0xd4, 0x68, 0x9e,
            0x2b, 0x16, 0xbe, 0x58, 0x7d, 0x47, 0xa1, 0xfc, 0x8f, 0xf8, 0xb8, 0xd1, 0x7a, 0xd0, 0x31, 0xce,
            0x45, 0xcb, 0x3a, 0x8f, 0x95, 0x16, 0x04, 0x28, 0xaf, 0xd7, 0xfb, 0xca, 0xbb, 0x4b, 0x40, 0x7e,
        };
    }

    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    internal static partial class SpookyHashV2
    {
        // This code is here only for Burst compatibility testing since Editor. Code for SpookyHashV2 is in Editor.
        internal static unsafe void ComputeHash128(FixedString512Bytes input, Hash128* hash)
        {
            HashUnsafeUtilities.ComputeHash128(input.GetUnsafePtr(), (ulong)input.Length, hash);
        }
    }
}
