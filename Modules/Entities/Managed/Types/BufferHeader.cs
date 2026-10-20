// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [StructLayout(LayoutKind.Explicit)]
    [NoAlias]
    internal unsafe struct BufferHeader
    {
        public const int kMinimumCapacity = 8;

        [NoAlias]
        [FieldOffset(0)] public byte* Pointer;
        [FieldOffset(8)] public int Length;
        [FieldOffset(12)] public int Capacity;

        public static byte* GetElementPointer(BufferHeader* header)
        {
            if (header->Pointer != null)
                return header->Pointer;

            return (byte*)(header + 1);
        }

        public enum TrashMode
        {
            TrashOldData,
            RetainOldData
        }

        public static void EnsureCapacity(BufferHeader* header, int count, int typeSize, int alignment, TrashMode trashMode, bool useMemoryInitPattern, byte memoryInitPattern)
        {
            if (count <= header->Capacity)
                return;
            var adjustedCount = Math.Max(kMinimumCapacity, Math.Max(2 * header->Capacity, count)); // stop pathological performance of ++Capacity allocating every time, tiny Capacities
            SetCapacity(header, adjustedCount, typeSize, alignment, trashMode, useMemoryInitPattern, memoryInitPattern, 0);
        }

        public static void SetCapacity(BufferHeader* header, int count, int typeSize, int alignment, TrashMode trashMode, bool useMemoryInitPattern, byte memoryInitPattern, int internalCapacity)
        {
            var newCapacity = count;
            if (newCapacity == header->Capacity)
                return;

            long newSizeInBytes = (long)newCapacity * typeSize;

            byte* oldData = GetElementPointer(header);
            byte* newData = (newCapacity <= internalCapacity) ? (byte*)(header + 1) : (byte*)Memory.Unmanaged.Allocate(newSizeInBytes, alignment, Allocator.Persistent);

            if (oldData != newData) // if at least one of them isn't the internal pointer...
            {
                if (useMemoryInitPattern)
                {
                    if (trashMode == TrashMode.RetainOldData)
                    {
                        var oldSizeInBytes = (header->Capacity * typeSize);
                        var bytesToInitialize = newSizeInBytes - oldSizeInBytes;
                        if (bytesToInitialize > 0)
                        {
                            UnsafeUtility.MemSet(newData + oldSizeInBytes, memoryInitPattern, bytesToInitialize);
                        }
                    }
                    else
                    {
                        UnsafeUtility.MemSet(newData, memoryInitPattern, newSizeInBytes);
                    }
                }
                if (trashMode == TrashMode.RetainOldData)
                {
                    long bytesToCopy = Math.Min((long)header->Capacity, count) * typeSize;
                    UnsafeUtility.MemCpy(newData, oldData, bytesToCopy);
                }
                // Note we're freeing the old buffer only if it was not using the internal capacity. Don't change this to 'oldData', because that would be a bug.
                if (header->Pointer != null)
                {
                    Memory.Unmanaged.Free(header->Pointer, Allocator.Persistent);
                }
            }

            header->Pointer = (newData == (byte*)(header + 1)) ? null : newData;
            header->Capacity = newCapacity;
        }

        public static void Assign(BufferHeader* header, byte* source, int count, int typeSize, int alignment, bool useMemoryInitPattern, byte memoryInitPattern)
        {
            EnsureCapacity(header, count, typeSize, alignment, TrashMode.TrashOldData, useMemoryInitPattern, memoryInitPattern);

            // Select between internal capacity buffer and heap buffer.
            byte* elementPtr = GetElementPointer(header);

            UnsafeUtility.MemCpy(elementPtr, source, (long)typeSize * count);

            header->Length = count;
        }

        public static void Initialize(BufferHeader* header, int bufferCapacity)
        {
            header->Pointer = null;
            header->Length = 0;
            header->Capacity = bufferCapacity;
        }

        public static void Destroy(BufferHeader* header)
        {
            if (header->Pointer != null)
            {
                Memory.Unmanaged.Free(header->Pointer, Allocator.Persistent);
            }

            Initialize(header, 0);
        }

        // After cloning two worlds have access to the same malloc'ed buffer pointer leading to double deallocate etc.
        // So after cloning, just allocate all malloc based buffers and copy the data.
        public static void PatchAfterCloningChunk(Archetype* archetype, byte* chunkBuffer, int entityCount)
        {
            for (int i = archetype->FirstBufferComponent, archetypeBufferTypesEnd = archetype->BufferComponentsEnd; i < archetypeBufferTypesEnd; ++i)
            {
                var type = archetype->Types[i];
                ref readonly var ti = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var sizeOf = archetype->SizeOfs[i];
                var offset = archetype->Offsets[i];
                for (var j = 0; j < entityCount; ++j)
                {
                    var offsetOfBuffer = offset + sizeOf * j;
                    var header = (BufferHeader*)(chunkBuffer + offsetOfBuffer);
                    if (header->Pointer != null) // hoo boy, it's a malloc
                    {
                        BufferHeader newHeader = *header;
                        long bytesToAllocate = (long)header->Capacity * ti.ElementSize;
                        long bytesToCopy = (long)header->Length * ti.ElementSize;
                        newHeader.Pointer = (byte*)Memory.Unmanaged.Allocate(bytesToAllocate, TypeManager.MaximumSupportedAlignment, Allocator.Persistent);
                        UnsafeUtility.MemCpy(newHeader.Pointer, header->Pointer, bytesToCopy);
                        *header = newHeader;
                    }
                }
            }
        }

        public static void MemsetUnusedMemory(BufferHeader* bufferHeader, int internalCapacity, int elementSize, byte value)
        {
            // If bufferHeader->Pointer is not null it means with rely on a dedicated buffer instead of the internal one (that follows the header) to store the elements.
            // in this case we also have to fully wipe out the internal buffer which is not in use.
            if (bufferHeader->Pointer != null)
            {
                byte* internalBuffer = (byte*)(bufferHeader + 1);
                UnsafeUtility.MemSet(internalBuffer, value, internalCapacity * elementSize);
            }

            // Wipe out excess capacity
            var elementCountToClean = bufferHeader->Capacity - bufferHeader->Length;
            var firstElementToClean = bufferHeader->Length;
            var buffer = BufferHeader.GetElementPointer(bufferHeader);
            UnsafeUtility.MemSet(buffer + (firstElementToClean * elementSize), value, elementCountToClean * elementSize);
        }
    }
}
