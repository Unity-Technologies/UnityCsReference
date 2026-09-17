// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    public static partial class xxHash3
    {
        internal static unsafe void Avx2HashLongInternalLoop(ulong* acc, byte* input, byte* dest, long length, byte* secret, int isHash64)
        {
            if (X86.Avx2.IsAvx2Supported)
            {
                // Process packets of 512 bits
                var nb_blocks = (length-1) / BLOCK_LEN;
                for (int n = 0; n < nb_blocks; n++)
                {
                    Avx2Accumulate(acc, input + n * BLOCK_LEN, dest == null ? null : dest + n * BLOCK_LEN, secret, NB_ROUNDS, isHash64);
                    Avx2ScrambleAcc(acc, secret + SECRET_KEY_SIZE - STRIPE_LEN);
                }

                var nbStripes = ((length-1) - (BLOCK_LEN * nb_blocks)) / STRIPE_LEN;
                Avx2Accumulate(acc, input + nb_blocks * BLOCK_LEN, dest == null ? null : dest + nb_blocks * BLOCK_LEN, secret, nbStripes, isHash64);

                var p = input + length - STRIPE_LEN;
                Avx2Accumulate512(acc, p, null, secret + SECRET_KEY_SIZE - STRIPE_LEN - SECRET_LASTACC_START);

                if (dest != null)
                {
                    var remaining = length % STRIPE_LEN;
                    if (remaining != 0)
                    {
                        UnsafeUtility.MemCpy(dest + length - remaining, input + length - remaining, remaining);
                    }
                }
            }
        }

        internal static unsafe void Avx2ScrambleAcc(ulong* acc, byte* secret)
        {
            if (X86.Avx2.IsAvx2Supported)
            {
                var xAcc = (v256*) acc;
                var xSecret = (v256*) secret;
                var prime32 = X86.Avx.mm256_set1_epi32(unchecked((int) PRIME32_1));

                // First bank
                var acc_vec = xAcc[0];
                var shifted = X86.Avx2.mm256_srli_epi64(acc_vec, 47);
                var data_vec = X86.Avx2.mm256_xor_si256(acc_vec, shifted);

                var key_vec = X86.Avx.mm256_loadu_si256(xSecret + 0);
                var data_key = X86.Avx2.mm256_xor_si256(data_vec, key_vec);

                var data_key_hi = X86.Avx2.mm256_shuffle_epi32(data_key, X86.Sse.SHUFFLE(0, 3, 0, 1));
                var prod_lo = X86.Avx2.mm256_mul_epu32(data_key, prime32);
                var prod_hi = X86.Avx2.mm256_mul_epu32(data_key_hi, prime32);

                xAcc[0] = X86.Avx2.mm256_add_epi64(prod_lo, X86.Avx2.mm256_slli_epi64(prod_hi, 32));

                // Second bank
                acc_vec = xAcc[1];
                shifted = X86.Avx2.mm256_srli_epi64(acc_vec, 47);
                data_vec = X86.Avx2.mm256_xor_si256(acc_vec, shifted);

                key_vec = X86.Avx.mm256_loadu_si256(xSecret + 1);
                data_key = X86.Avx2.mm256_xor_si256(data_vec, key_vec);

                data_key_hi = X86.Avx2.mm256_shuffle_epi32(data_key, X86.Sse.SHUFFLE(0, 3, 0, 1));
                prod_lo = X86.Avx2.mm256_mul_epu32(data_key, prime32);
                prod_hi = X86.Avx2.mm256_mul_epu32(data_key_hi, prime32);

                xAcc[1] = X86.Avx2.mm256_add_epi64(prod_lo, X86.Avx2.mm256_slli_epi64(prod_hi, 32));
            }
        }

        internal static unsafe void Avx2Accumulate(ulong* acc, byte* input, byte* dest, byte* secret, long nbStripes,
            int isHash64)
        {
            if (X86.Avx2.IsAvx2Supported)
            {
                for (var n = 0; n < nbStripes; n++)
                {
                    var xInput = input + n * STRIPE_LEN;
                    Avx2Accumulate512(acc, xInput, dest == null ? null : dest + n * STRIPE_LEN,
                        secret + n * SECRET_CONSUME_RATE);
                }
            }
        }

        internal static unsafe void Avx2Accumulate512(ulong* acc, byte* input, byte* dest, byte* secret)
        {
            if (X86.Avx2.IsAvx2Supported)
            {
                var xAcc = (v256*) acc;
                var xSecret = (v256*) secret;
                var xInput = (v256*) input;

                // First bank
                var data_vec = X86.Avx.mm256_loadu_si256(xInput + 0);
                var key_vec  = X86.Avx.mm256_loadu_si256(xSecret + 0);
                var data_key = X86.Avx2.mm256_xor_si256(data_vec, key_vec);

                if (dest != null)
                {
                    X86.Avx.mm256_storeu_si256(dest, data_vec);
                }

                var data_key_lo = X86.Avx2.mm256_shuffle_epi32(data_key, X86.Sse.SHUFFLE(0, 3, 0, 1));
                var product = X86.Avx2.mm256_mul_epu32(data_key, data_key_lo);
                var data_swap= X86.Avx2.mm256_shuffle_epi32(data_vec, X86.Sse.SHUFFLE(1, 0, 3, 2));
                var sum= X86.Avx2.mm256_add_epi64(xAcc[0], data_swap);

                xAcc[0] = X86.Avx2.mm256_add_epi64(product, sum);

                // Second bank
                data_vec = X86.Avx.mm256_loadu_si256(xInput + 1);
                key_vec = X86.Avx.mm256_loadu_si256(xSecret + 1);
                data_key = X86.Avx2.mm256_xor_si256(data_vec, key_vec);

                if (dest != null)
                {
                    X86.Avx.mm256_storeu_si256(dest + 32, data_vec);
                }

                data_key_lo = X86.Avx2.mm256_shuffle_epi32(data_key, X86.Sse.SHUFFLE(0, 3, 0, 1));
                product = X86.Avx2.mm256_mul_epu32(data_key, data_key_lo);
                data_swap = X86.Avx2.mm256_shuffle_epi32(data_vec, X86.Sse.SHUFFLE(1, 0, 3, 2));
                sum = X86.Avx2.mm256_add_epi64(xAcc[1], data_swap);

                xAcc[1] = X86.Avx2.mm256_add_epi64(product, sum);
            }
        }

    }
}
