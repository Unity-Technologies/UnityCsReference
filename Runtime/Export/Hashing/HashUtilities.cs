// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public static partial class HashUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendHash(ref Hash128 inHash, ref Hash128 outHash)
        {
            Unity.LowLevel.HashUtilities.AppendHash(ref inHash, ref outHash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeHash128<T>(ref T value, ref Hash128 hash)
            where T : struct
        {
            Unity.LowLevel.HashUtilities.ComputeHash128(ref value, ref hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeHash128(byte[] value, ref Hash128 hash)
        {
            Unity.LowLevel.HashUtilities.ComputeHash128(value, ref hash);
        }

        public static void QuantisedMatrixHash(ref Matrix4x4 value, ref Hash128 hash)
        {
            unsafe
            {
                fixed(Hash128 * h = &hash)
                {
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
                    int* quantisedVector = stackalloc int[3];
                    for (int i = 0; i < 3; ++i)
                        quantisedVector[i] = (int)((value[i] * 1000) + .5f);

                    HashUnsafeUtilities.ComputeHash128(quantisedVector, sizeof(int) * 3, h);
                }
            }
        }
    }
}
