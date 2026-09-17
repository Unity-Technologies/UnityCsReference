// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;


namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    unsafe internal struct Memory
    {
        internal const long k_MaximumRamSizeInBytes = 1L << 40; // a terabyte

        [GenerateTestsForBurstCompatibility]
        internal struct Unmanaged
        {
            internal static void* Allocate(long size, int align, AllocatorManager.AllocatorHandle allocator)
            {
                return Array.Resize(null, 0, 1, allocator, size, align);
            }
            
            internal static void* Allocate(long size, int align, MemoryLabel label)
            {
                return Array.Resize(null, 0, 1, label, size, align);
            }            

            internal static void Free(void* pointer, AllocatorManager.AllocatorHandle allocator)
            {
                if (pointer == null)
                    return;
                Array.Resize(pointer, 1, 0, allocator, 1, 1);
            }
            
            internal static void Free(void* pointer, MemoryLabel label)
            {
                if (pointer == null)
                    return;
                Array.Resize(pointer, 1, 0, label, 1, 1);
            }            

            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static T* Allocate<T>(AllocatorManager.AllocatorHandle allocator) where T : unmanaged
            {
                return Array.Resize<T>(null, 0, 1, allocator);
            }
            
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static T* Allocate<T>(MemoryLabel label) where T : unmanaged
            {
                return Array.Resize<T>(null, 0, 1, label);
            }            

            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Free<T>(T* pointer, AllocatorManager.AllocatorHandle allocator) where T : unmanaged
            {
                if (pointer == null)
                    return;
                Array.Resize(pointer, 1, 0, allocator);
            }
            
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Free<T>(T* pointer, MemoryLabel label) where T : unmanaged
            {
                if (pointer == null)
                    return;
                Array.Resize(pointer, 1, 0, label);
            }            

            [GenerateTestsForBurstCompatibility]
            internal struct Array
            {
                static bool IsCustom(AllocatorManager.AllocatorHandle allocator)
                {
                    return (int) allocator.Index >= AllocatorManager.FirstUserIndex;
                }

                static void* CustomResize(void* oldPointer, long oldCount, long newCount, AllocatorManager.AllocatorHandle allocator, long size, int align)
                {
                    AllocatorManager.Block block = default;
                    block.Range.Allocator = allocator;
                    block.Range.Items = (int)newCount;
                    block.Range.Pointer = (IntPtr)oldPointer;
                    block.BytesPerItem = (int)size;
                    block.Alignment = align;
                    block.AllocatedItems = (int)oldCount;
                    var error = AllocatorManager.Try(ref block);
                    AllocatorManager.CheckFailedToAllocate(error);
                    return (void*)block.Range.Pointer;
                }

                internal static void* Resize(void* oldPointer, long oldCount, long newCount, AllocatorManager.AllocatorHandle allocator,
                    long size, int align)
                {
                    return Resize(oldPointer, oldCount, newCount, allocator, default, size, align);
                }
                
                internal static void* Resize(void* oldPointer, long oldCount, long newCount, MemoryLabel label,
                    long size, int align)
                {
                    return Resize(oldPointer, oldCount, newCount, default, label, size, align);
                } 
                
                static void* Resize(void* oldPointer, long oldCount, long newCount, AllocatorManager.AllocatorHandle allocator, MemoryLabel label, long size, int align)
                {
                    // Make the alignment multiple of cacheline size
                    var alignment = math.max(JobsUtility.CacheLineSize, align);

                    if (IsCustom(allocator != default ? allocator : label.allocator))
                        return CustomResize(oldPointer, oldCount, newCount, allocator != default ? allocator : label.allocator, size, alignment);
                    void* newPointer = default;
                    if (newCount > 0)
                    {
                        long bytesToAllocate = newCount * size;
                        CheckByteCountIsReasonable(bytesToAllocate);
                        newPointer = allocator != default ? UnsafeUtility.MallocTracked(bytesToAllocate, alignment, allocator.ToAllocator, 0) : UnsafeUtility.MallocTracked(bytesToAllocate, alignment, label, 0);
                        
                        if (oldCount > 0)
                        {
                            long count = math.min(oldCount, newCount);
                            long bytesToCopy = count * size;
                            CheckByteCountIsReasonable(bytesToCopy);
                            UnsafeUtility.MemCpy(newPointer, oldPointer, bytesToCopy);
                        }
                    }

                    if (oldCount > 0)
                    {
                        if (allocator != default)
                            UnsafeUtility.FreeTracked(oldPointer, allocator.ToAllocator);
                        else
                            UnsafeUtility.FreeTracked(oldPointer, label);
                    }
                    return newPointer;
                }                 

                [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
                internal static T* Resize<T>(T* oldPointer, long oldCount, long newCount, AllocatorManager.AllocatorHandle allocator) where T : unmanaged
                {
                    return (T*)Resize((byte*)oldPointer, oldCount, newCount, allocator, sizeof(T), UnsafeUtility.AlignOf<T>());
                }
                
                [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
                internal static T* Resize<T>(T* oldPointer, long oldCount, long newCount, MemoryLabel label) where T : unmanaged
                {
                    return (T*)Resize((byte*)oldPointer, oldCount, newCount, label, sizeof(T), UnsafeUtility.AlignOf<T>());
                }                

                [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
                internal static T* Allocate<T>(long count, AllocatorManager.AllocatorHandle allocator)
                    where T : unmanaged
                {
                    return Resize<T>(null, 0, count, allocator);
                }
                
                [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
                internal static T* Allocate<T>(long count, MemoryLabel label)
                    where T : unmanaged
                {
                    return Resize<T>(null, 0, count, label);
                }
                

                [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
                internal static void Free<T>(T* pointer, long count, AllocatorManager.AllocatorHandle allocator)
                    where T : unmanaged
                {
                    if (pointer == null)
                        return;
                    Resize(pointer, count, 0, allocator);
                }
                
                [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
                internal static void Free<T>(T* pointer, long count, MemoryLabel label)
                    where T : unmanaged
                {
                    if (pointer == null)
                        return;
                    Resize(pointer, count, 0, label);
                }                
            }
        }

        [GenerateTestsForBurstCompatibility]
        internal struct Array
        {
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Set<T>(T* pointer, long count, T t = default) where T : unmanaged
            {
                long bytesToSet = count * sizeof(T);
                CheckByteCountIsReasonable(bytesToSet);
                for (var i = 0; i < count; ++i)
                    pointer[i] = t;
            }

            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Clear<T>(T* pointer, long count) where T : unmanaged
            {
                long bytesToClear = count * sizeof(T);
                CheckByteCountIsReasonable(bytesToClear);
                UnsafeUtility.MemClear(pointer, bytesToClear);
            }

            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            internal static void Copy<T>(T* dest, T* src, long count) where T : unmanaged
            {
                long bytesToCopy = count * sizeof(T);
                CheckByteCountIsReasonable(bytesToCopy);
                UnsafeUtility.MemCpy(dest, src, bytesToCopy);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckByteCountIsReasonable(long size)
        {
            if (size < 0)
                throw new InvalidOperationException($"Attempted to operate on {size} bytes of memory: negative size");
            if (size > k_MaximumRamSizeInBytes)
                throw new InvalidOperationException($"Attempted to operate on {size} bytes of memory: size too big");
        }

        [GenerateTestsForBurstCompatibility]
        internal static MemoryLabel CreateLabel(FixedString32Bytes category, FixedString64Bytes name, Allocator allocator = Allocator.Persistent)
        {
            return new MemoryLabel(
                category.GetUnsafePtr(), category.Length,
                name.GetUnsafePtr(), name.Length,
                allocator);
        }
    }
}
