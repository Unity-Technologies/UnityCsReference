// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    internal static class BlobBuilderArrayExtension
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static unsafe void CopyFrom<T>(this BlobBuilderArray<T> dstArray, ref BlobArray<T> srcArray) where T : unmanaged
        {
            UnsafeUtility.MemCpy(dstArray.GetUnsafePtr(), srcArray.GetUnsafePtr(), srcArray.Length * sizeof(T));
        }
    }

    internal static class BlobBuilderExtension
    {
        public static void CopyBlobArray<T>(ref this BlobBuilder builder, ref BlobArray<T> srcArray, ref BlobArray<T> targetArray) where T : unmanaged
        {
            var blobBuilderArray = builder.Allocate(ref targetArray, srcArray.Length);
            blobBuilderArray.CopyFrom(ref srcArray);
        }
    }
}
