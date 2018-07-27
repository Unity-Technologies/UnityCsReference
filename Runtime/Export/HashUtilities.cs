// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    public static class HashUtilities
    {
        public static void AppendHash(
            ref Hash128 inHash,
            ref Hash128 outHash
        )
        {
            unsafe
            {
                fixed(Hash128 * outH = &outHash)
                fixed(Hash128 * message = &inHash)
                {
                    HashUnsafeUtilities.ComputeHash128(message, (ulong)sizeof(Hash128), outH);
                }
            }
        }

        public static void QuantisedMatrixHash(ref Matrix4x4 value, ref Hash128 hash)
        {
            unsafe
            {
                fixed(Hash128 * h = &hash)
                {
                    // Transform matrix.
                    int* quantisedMatrix = stackalloc int[16];
                    for (int i = 0; i < 16; ++i)
                        quantisedMatrix[i] = (int)((value[i] * 1000) + .5f);

                    HashUnsafeUtilities.ComputeHash128(quantisedMatrix, sizeof(int) * 16, h);
                }
            }
        }

        public static void QuantisedVectorHash(ref Vector3 value, ref Hash128 hash)
        {
            unsafe
            {
                fixed(Hash128 * h = &hash)
                {
                    // Transform matrix.
                    int* quantisedVector = stackalloc int[3];
                    for (int i = 0; i < 3; ++i)
                        quantisedVector[i] = (int)((value[i] * 1000) + .5f);

                    HashUnsafeUtilities.ComputeHash128(quantisedVector, sizeof(int) * 3, h);
                }
            }
        }

        public static unsafe void ComputeHash128<T>(ref T value, ref Hash128 hash)
            where T : struct
        {
            var data = UnsafeUtility.AddressOf(ref value);
            var dataSize = (ulong)UnsafeUtility.SizeOf<T>();
            var hashPtr = (Hash128*)UnsafeUtility.AddressOf(ref hash);
            HashUnsafeUtilities.ComputeHash128(data, dataSize, hashPtr);
        }
    }

    public static class HashUnsafeUtilities
    {
        public static unsafe void ComputeHash128(void* data, ulong dataSize, ulong* hash1, ulong* hash2)
        {
            SpookyHash.Hash(data, dataSize, hash1, hash2);
        }

        public static unsafe void ComputeHash128(void* data, ulong dataSize, Hash128* hash)
        {
            // We don't have ref return in C# 6.0, and we don't want to expose an unsafe pointer
            // to Hash128's internal data.
            // Se we make a copy and then recreate the hash in the end.
            // It won't create GCable memory, but it introduce copy operations.
            var u61_0 = hash->u64_0;
            var u61_1 = hash->u64_1;
            ComputeHash128(data, dataSize, &u61_0, &u61_1);
            *hash = new Hash128(u61_0, u61_1);
        }
    }
}
