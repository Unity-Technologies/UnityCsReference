// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Bindings;

namespace Unity.Animations
{
    // Lives next to BlobObject so that the Entities blob internals only need to be visible to the Animation module
    [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
    internal static class BlobAssetExtensions
    {
        public static unsafe ulong GetDataHash<T>(this BlobAssetReference<T> blobAssetReference) where T : unmanaged
        {
            return blobAssetReference.IsCreated ? blobAssetReference.m_data.Header->Hash : 0;
        }

        public static unsafe Allocator GetAllocator<T>(this BlobAssetReference<T> blobAssetReference) where T : unmanaged
        {
            return blobAssetReference.IsCreated ? blobAssetReference.m_data.Header->Allocator.ToAllocator : Allocator.None;
        }

        /// <summary>
        /// Create a clone of a BlobAssetReference
        /// </summary>
        /// <param name="source">The blob asset to clone.</param>
        /// <param name="allocator">The built-in allocator of the clone.</param>
        /// <returns>A clone of the source blob asset. It has the hash of the source.</returns>
        public static unsafe BlobAssetReference<T> Clone<T>(this BlobAssetReference<T> source, Allocator allocator) where T : unmanaged
        {
            if (!source.IsCreated)
                return default;

            // Blobs only hold relative offsets, so a byte copy is a valid blob. Allocate the way that
            // BlobAssetReference.Dispose frees: Memory.Unmanaged.Free calls FreeTracked for built-in allocators
            var size = sizeof(BlobAssetHeader) + source.m_data.Header->Length;
            var buffer = (byte*)UnsafeUtility.MallocTracked(size, math.max(JobsUtility.CacheLineSize, 16), allocator, 0);
            UnsafeUtility.MemCpy(buffer, source.m_data.Header, size);

            var header = (BlobAssetHeader*)buffer;
            header->Allocator = allocator;
            header->ValidationPtr = buffer + sizeof(BlobAssetHeader);

            BlobAssetReference<T> clone = default;
            clone.m_data.m_Ptr = buffer + sizeof(BlobAssetHeader);
            return clone;
        }

        public static ref int GetOffsetPtr<T>(this ref BlobArray<T> blobArray) where T : unmanaged
        {
            return ref blobArray.m_OffsetPtr;
        }

        public static ref int GetLength<T>(this ref BlobArray<T> blobArray) where T : unmanaged
        {
            return ref blobArray.m_Length;
        }

        public static unsafe int GetLength<T>(this BlobAssetReference<T> source) where T : unmanaged
        {
            return source.m_data.Header->Length;
        }

        // Borrowed view of the blob whose root is at root: it doesn't own the data and must not be disposed
        public static unsafe BlobAssetReference<T> CreateBorrowedBlobAssetReference<T>(void* root) where T : unmanaged
        {
            if (root == null)
                return default;

            return BlobAssetReference<T>.Create(new BlobAssetReferenceData()
            {
                m_Ptr = (byte*)root
            });
        }
    }
}
