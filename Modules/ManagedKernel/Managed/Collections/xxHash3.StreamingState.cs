// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    public static partial class xxHash3
    {
        /// <summary>
        /// Type used to compute hash based on multiple data feed
        /// </summary>
        /// <remarks>
        /// Allow to feed the internal hashing accumulators with data through multiple calls to <see cref="Update"/>, then retrieving the final hash value using <see cref="DigestHash64"/> or <see cref="DigestHash128"/>.
        /// More info about how to use this class in its constructor.
        /// </remarks>
        [GenerateTestsForBurstCompatibility]
        public struct StreamingState
        {
            #region Public API

            /// <summary>
            /// Create a StreamingState object, ready to be used with the streaming API
            /// </summary>
            /// <param name="isHash64">true if we are computing a 64bits hash value, false if we are computing a 128bits one</param>
            /// <param name="seed">A seed value to be used to compute the hash, default is 0</param>
            /// <remarks>
            /// Once the object is constructed, you can call the <see cref="Update"/> method as many times as you want to accumulate data to hash.
            /// When all the data has been sent, call <see cref="DigestHash64"/> or <see cref="DigestHash128"/> to retrieve the corresponding key, the <see cref="StreamingState"/>
            /// instance will then be reset, using the same hash key size and same Seed in order to be ready to be used again.
            /// </remarks>
            public StreamingState(bool isHash64, ulong seed=0)
            {
                State = default;
                Reset(isHash64, seed);
            }

            /// <summary>
            /// Reset the state of the streaming instance using the given seed value.
            /// </summary>
            /// <param name="isHash64"></param>
            /// <param name="seed">The seed value to alter the computed hash value from</param>
            /// <remarks> Call this method to start a new streaming session based on this instance</remarks>
            public unsafe void Reset(bool isHash64, ulong seed=0UL)
            {
                fixed (StreamingStateData* s = &State)
                {
                    // Reset the whole buffer to 0
                    var size = UnsafeUtility.SizeOf<StreamingStateData>();
                    UnsafeUtility.MemClear(s, size);

                    // Set back the saved states
                    s->IsHash64 = isHash64 ? 1 : 0;

                    // Init the accumulator with the prime numbers
                    var acc = &s->Acc;
                    acc[0] = PRIME32_3;
                    acc[1] = PRIME64_1;
                    acc[2] = PRIME64_2;
                    acc[3] = PRIME64_3;
                    acc[4] = PRIME64_4;
                    acc[5] = PRIME32_2;
                    acc[6] = PRIME64_5;
                    acc[7] = PRIME32_1;

                    s->Seed = seed;

                    fixed (byte* secret = xxHashDefaultKey.kSecret)
                    {
                        if (seed != 0)
                        {
                            // Must encode the secret key if we're using a seed, we store it in the state object
                            EncodeSecretKey(&s->SecretKey, secret, seed);
                        }
                        else
                        {
                            // Otherwise just copy it
                            UnsafeUtility.MemCpy(&s->SecretKey, secret, SECRET_KEY_SIZE);
                        }
                    }
                }
            }

            /// <summary>
            /// Add some data to be hashed
            /// </summary>
            /// <param name="input">The memory buffer, can't be null</param>
            /// <param name="length">The length of the data to accumulate, can be zero</param>
            /// <remarks>This API allows you to feed very small data to be hashed, avoiding you to accumulate them in a big buffer, then computing the hash value from.</remarks>
            public unsafe void Update(void* input, int length)
            {
                fixed (StreamingStateData* s = &State)
                {
                    var bInput = (byte*) input;
                    var bEnd = bInput + length;
                    var isHash64 = s->IsHash64;
                    var acc = &s->Acc;
                    var buffer = &s->Buffer;
                    var secret = &s->SecretKey;
                    s->TotalLength += length;

                    if (s->BufferedSize + length <= INTERNAL_BUFFER_SIZE)
                    {
                        UnsafeUtility.MemCpy(buffer + s->BufferedSize, bInput, length);
                        s->BufferedSize += length;
                        return;
                    }

                    if (s->BufferedSize != 0)
                    {
                        var loadSize = INTERNAL_BUFFER_SIZE - s->BufferedSize;
                        UnsafeUtility.MemCpy(buffer + s->BufferedSize, bInput, loadSize);
                        bInput += loadSize;

                        ConsumeStripes(acc, ref s->NbStripesSoFar, buffer, INTERNAL_BUFFER_STRIPES, secret, isHash64);

                        s->BufferedSize = 0;
                    }

                    if (bInput + INTERNAL_BUFFER_SIZE < bEnd)
                    {
                        var limit = bEnd - INTERNAL_BUFFER_SIZE;
                        do
                        {
                            ConsumeStripes(acc, ref s->NbStripesSoFar, bInput, INTERNAL_BUFFER_STRIPES, secret, isHash64);
                            bInput += INTERNAL_BUFFER_SIZE;
                        } while (bInput < limit);
                        UnsafeUtility.MemCpy(buffer + INTERNAL_BUFFER_SIZE - STRIPE_LEN, bInput - STRIPE_LEN, STRIPE_LEN);
                    }

                    if (bInput < bEnd)
                    {
                        var newBufferedSize = bEnd - bInput;
                        UnsafeUtility.MemCpy(buffer, bInput, newBufferedSize);
                        s->BufferedSize = (int) newBufferedSize;
                    }
                }
            }

            /// <summary>
            /// Add the contents of input struct to the hash.
            /// </summary>
            /// <typeparam name="T">The input type.</typeparam>
            /// <param name="input">The input struct that will be hashed</param>
            /// <remarks>This API allows you to feed very small data to be hashed, avoiding you to accumulate them in a big buffer, then computing the hash value from.</remarks>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public unsafe void Update<T>(in T input) where T : unmanaged
            {
                fixed (T* p = &input)
                {
                    Update(p, sizeof(T));
                }
            }


            /// <summary>
            /// Compute the 128bits value based on all the data that have been accumulated
            /// </summary>
            /// <returns>The hash value</returns>
            public unsafe uint4 DigestHash128()
            {
                CheckKeySize(0);

                fixed (StreamingStateData* s = &State)
                {
                    unchecked
                    {
                        var secret = &s->SecretKey;
                        uint4 hash;
                        if (s->TotalLength > MIDSIZE_MAX)
                        {
                            var acc = stackalloc ulong[ACC_NB];
                            DigestLong(s, acc, secret, 0);

                            var low64 = MergeAcc(acc, secret + SECRET_MERGEACCS_START,
                                (ulong) s->TotalLength * PRIME64_1);
                            var high64 = MergeAcc(acc, secret + SECRET_LIMIT - SECRET_MERGEACCS_START,
                                ~((ulong) s->TotalLength * PRIME64_2));
                            hash = ToUint4(low64, high64);
                        }
                        else
                        {
                            hash = Hash128(&s->Buffer, s->TotalLength, s->Seed);
                        }
                        Reset(s->IsHash64==1, s->Seed);
                        return hash;
                    }
                }
            }

            /// <summary>
            /// Compute the 64bits value based on all the data that have been accumulated
            /// </summary>
            /// <returns>The hash value</returns>
            public unsafe uint2 DigestHash64()
            {
                CheckKeySize(1);

                fixed (StreamingStateData* s = &State)
                {
                    unchecked
                    {
                        var secret = &s->SecretKey;
                        uint2 hash;
                        if (s->TotalLength > MIDSIZE_MAX)
                        {
                            var acc = stackalloc ulong[ACC_NB];
                            DigestLong(s, acc, secret, 1);

                            hash = ToUint2(MergeAcc(acc, secret + SECRET_MERGEACCS_START, (ulong) s->TotalLength * PRIME64_1));
                        }
                        else
                        {
                            hash = Hash64(&s->Buffer, s->TotalLength, s->Seed);
                        }
                        Reset(s->IsHash64==1, s->Seed);
                        return hash;
                    }
                }
            }

            #endregion

            #region Constants

            private static readonly int SECRET_LIMIT = SECRET_KEY_SIZE - STRIPE_LEN;
            private static readonly int NB_STRIPES_PER_BLOCK = SECRET_LIMIT / SECRET_CONSUME_RATE;
            private static readonly int INTERNAL_BUFFER_SIZE = 256;
            private static readonly int INTERNAL_BUFFER_STRIPES = INTERNAL_BUFFER_SIZE / STRIPE_LEN;

            #endregion

            #region Data storage

            private StreamingStateData State;

            [StructLayout(LayoutKind.Explicit)]
            struct StreamingStateData
            {
                [FieldOffset(0)] public ulong Acc; // 64 bytes
                [FieldOffset(64)] public byte Buffer; // 256 bytes
                [FieldOffset(320)] public int IsHash64; // 4 bytes
                [FieldOffset(324)] public int BufferedSize; // 4 bytes
                [FieldOffset(328)] public int NbStripesSoFar; // 4 bytes + 4 padding
                [FieldOffset(336)] public long TotalLength; // 8 bytes
                [FieldOffset(344)] public ulong Seed; // 8 bytes
                [FieldOffset(352)] public byte SecretKey; // 192 bytes
                [FieldOffset(540)] public byte _PadEnd;
            }

            #endregion

            #region Internals

            private static unsafe void DigestLong(StreamingStateData* s, ulong* acc, byte* secret, int isHash64)
            {
                var buffer = &s->Buffer;
                UnsafeUtility.MemCpy(acc, &s->Acc, STRIPE_LEN);
                if (s->BufferedSize >= STRIPE_LEN)
                {
                    var totalNbStripes = (s->BufferedSize - 1) / STRIPE_LEN;
                    ConsumeStripes(acc, ref s->NbStripesSoFar, buffer, totalNbStripes, secret, isHash64);

                    if (X86.Avx2.IsAvx2Supported)
                    {
                        Avx2Accumulate512(acc, buffer + s->BufferedSize - STRIPE_LEN, null,
                            secret + SECRET_LIMIT - SECRET_LASTACC_START);
                    }
                    else
                    {
                        DefaultAccumulate512(acc, buffer + s->BufferedSize - STRIPE_LEN, null,
                            secret + SECRET_LIMIT - SECRET_LASTACC_START, isHash64);
                    }
                }
                else
                {
                    var lastStripe = stackalloc byte[STRIPE_LEN];
                    var catchupSize = STRIPE_LEN - s->BufferedSize;
                    UnsafeUtility.MemCpy(lastStripe, buffer + INTERNAL_BUFFER_SIZE - catchupSize, catchupSize);
                    UnsafeUtility.MemCpy(lastStripe + catchupSize, buffer, s->BufferedSize);
                    if (X86.Avx2.IsAvx2Supported)
                    {
                        Avx2Accumulate512(acc, lastStripe, null, secret+SECRET_LIMIT-SECRET_LASTACC_START);
                    }
                    else
                    {
                        DefaultAccumulate512(acc, lastStripe, null, secret+SECRET_LIMIT-SECRET_LASTACC_START, isHash64);
                    }
                }
            }

            private static unsafe void ConsumeStripes(ulong* acc, ref int nbStripesSoFar, byte* input, long totalStripes,
                byte* secret, int isHash64)
            {
                if (NB_STRIPES_PER_BLOCK - nbStripesSoFar <= totalStripes)
                {
                    var nbStripes = NB_STRIPES_PER_BLOCK - nbStripesSoFar;
                    if (X86.Avx2.IsAvx2Supported)
                    {
                        Avx2Accumulate(acc, input, null, secret + nbStripesSoFar * SECRET_CONSUME_RATE, nbStripes, isHash64);
                        Avx2ScrambleAcc(acc, secret + SECRET_LIMIT);
                        Avx2Accumulate(acc, input + nbStripes * STRIPE_LEN, null, secret, totalStripes - nbStripes, isHash64);
                    }
                    else
                    {
                        DefaultAccumulate(acc, input, null, secret + nbStripesSoFar * SECRET_CONSUME_RATE, nbStripes, isHash64);
                        DefaultScrambleAcc(acc, secret + SECRET_LIMIT);
                        DefaultAccumulate(acc, input + nbStripes * STRIPE_LEN, null, secret, totalStripes - nbStripes, isHash64);
                    }

                    nbStripesSoFar = (int) totalStripes - nbStripes;
                }
                else
                {
                    if (X86.Avx2.IsAvx2Supported)
                    {
                        Avx2Accumulate(acc, input, null, secret + nbStripesSoFar * SECRET_CONSUME_RATE, totalStripes, isHash64);
                    }
                    else
                    {
                        DefaultAccumulate(acc, input, null, secret + nbStripesSoFar * SECRET_CONSUME_RATE, totalStripes, isHash64);
                    }

                    nbStripesSoFar += (int) totalStripes;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void CheckKeySize(int isHash64)
            {
                if (State.IsHash64 != isHash64)
                {
                    var s = State.IsHash64 != 0 ? "64" : "128";
                    throw new InvalidOperationException(
                        $"The streaming state was create for {s} bits hash key, the calling method doesn't support this key size, please use the appropriate API");
                }
            }

            #endregion
        }
    }
}
