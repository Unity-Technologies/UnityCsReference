// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using Unity.IL2CPP.CompilerServices;

namespace UnityEngine.Bindings
{
    /// <summary>
    /// Interface to wrap a managed collection type that will be marshalled to managed code
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [VisibleToOtherModules]
    internal interface ICollectionMarshallingAccessor<T>
    {
        public void CollectionChanged(int newSize);
        public void SetNull();
        public void SetEmpty();
        public int Length { get; }
        public int Capacity { get; }

        /// <summary>
        /// Note: This may not be safe to use on non-blittable types - if the underlying collection is an array of reference types, we may fail because of array covariance (which is not supported by span!)
        /// </summary>
        public Span<T> AsSpan();

        public bool IsNull { get; }
        public T this[int i] { get; set; }
        public ref T GetRef(int i);
        public ref T GetPinnableReference();
    }

    [VisibleToOtherModules]
    [Il2CppEagerStaticClassConstruction]
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// Represents an array that can be marshalled to and from native code.
    /// </summary>
    /// <remarks
    /// Note blittable arrays that are only marshalled in are passed as spans, since they are pinned in and are not changed
    /// This array is used for non-blittable arrays and blittable arrays that are out marshalled
    /// </remarks>
    internal unsafe struct MarshalledArray
    {
        internal enum DataOwner : int
        {
            /// <summary>
            /// Data is a pinned buffer, and the size is the size of the pinned buffer
            /// </summary>
            PinnedBuffer = 0,

            /// <summary>
            /// The collection size changed, but data is still pointing to the same pinned buffer
            /// </summary>
            PinnedBufferSizeChanged = 1,

            /// <summary>
            /// Data is a pointer to native memory allocated by the temp allocator, that needs to be freed
            /// </summary>
            TempAllocated = 2,

            /// <summary>
            /// Data is a pointer to native memory allocated by the temp allocator, that needs to be freed
            /// </summary>
            TempAllocatedCleanupRequired = 3,

            /// <summary>
            /// Data is a pointer that someone else owns, we do not need to free it
            /// </summary>
            ExternallyOwned = 4,

            /// <summary>
            /// Data is native owned memory, that we need to free
            /// </summary>
            NativeOwnedMemory = 5,

            /// <summary>
            /// Zero length collection
            /// </summary>
            Empty = 6,

            /// <summary>
            ///Collection is null
            /// </summary>
            Null = 7,

            /// <summary>
            ///Collection is only marshalled out
            /// </summary>
            Out = 8,
        }

        internal void* data;

        // Managed->Native: The number of elements in the pinned data buffer
        // Native->Managed: The number of elements in the returned data buffer
        internal int size;

        internal int capacity;

        internal DataOwner dataOwner;

        private MarshalledArray(void* data, int size, int capacity, DataOwner dataOwner)
        {
            this.data = data;
            this.size = size;
            this.capacity = capacity;
            this.dataOwner = dataOwner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<TNative> AsSpan<TNative>()
        {
            if (dataOwner == DataOwner.NativeOwnedMemory)
                return new Span<TNative>(BindingsAllocator.GetNativeOwnedDataPointer(data), size);
            return new Span<TNative>(data, size);
        }

        public void MarkAsOutMarshalledWithCapacity<T, TAccessor>(in TAccessor accessor) where TAccessor:ICollectionMarshallingAccessor<T>
        {
            // We assume that if we have data, it is a pinned buffer
            // Otherwise reseting the dataOwner to Null/Out would lose track of the pinned buffer
            Debug.Assert(data == null || dataOwner == DataOwner.PinnedBuffer);

            if (accessor.IsNull)
            {
                dataOwner = DataOwner.Null;
            }
            else
            {
                // Why are we setting size to accessor.Capacity and not using this.capacity?
                // The marshalling code may have already set data and capacity to a pinned/stackalloc'd buffer and we want to keep that untouched
                // so the out marshaller can use that buffer.  But this.size is free so we use that.
                size = accessor.Capacity;
                dataOwner = DataOwner.Out;
            }
        }

        public static MarshalledArray CreateFromPinnedAccessor<T, TAccessor>(in TAccessor accessor, void* data) where TAccessor:ICollectionMarshallingAccessor<T>
        {
            if (accessor.IsNull)
                return new MarshalledArray(null, 0, 0, DataOwner.Null);
            if (accessor.Length == 0)
                return new MarshalledArray(null, 0, 0, DataOwner.Empty);
            return new MarshalledArray(data, accessor.Length, accessor.Capacity, DataOwner.PinnedBuffer);
        }

        public static MarshalledArray CreateFromPinnedData(void* data, int size)
        {
            return new MarshalledArray(data, size, size, DataOwner.PinnedBuffer);
        }

        public static MarshalledArray CreateFromNativeAllocatedData(void* data, int size)
        {
            return new MarshalledArray(data, size, size, DataOwner.TempAllocated);
        }

        public static MarshalledArray CreateFromNonAllocatedBuffer(void* data, int size, int capacity)
        {
            return new MarshalledArray(data, size, capacity, DataOwner.ExternallyOwned);
        }

        public static void Allocate<TManaged, TNative, TCollectionAccessor>(in TCollectionAccessor collectionAccessor, ref MarshalledArray marshalledArray, bool elementCleanupRequired)
            where TNative : unmanaged
            where TCollectionAccessor : struct, ICollectionMarshallingAccessor<TManaged>
        {
            Allocate<TManaged, TCollectionAccessor>(collectionAccessor, ref marshalledArray, sizeof(TNative), elementCleanupRequired);
        }

        /// <summary>
        /// Allocates a native buffer of the required size if needed
        /// </summary>
        /// <typeparam name="TManaged"></typeparam>
        /// <param name="collectionAccessor">The managed array being marshalled</param>
        /// <param name="marshalledArray">The marshalled buffer to pass to native.  This buffer may be already allocated (size > 0), if so we assume that data is already zero filled</param>
        /// <param name="nativeElementSize"></param>
        public static void Allocate<TManaged, TCollectionAccessor>(in TCollectionAccessor collectionAccessor, ref MarshalledArray marshalledArray, int nativeElementSize, bool elementCleanupRequired)
            where TCollectionAccessor: struct, ICollectionMarshallingAccessor<TManaged>
        {
            if (collectionAccessor.IsNull)
            {
                marshalledArray = new MarshalledArray(null, 0, 0, DataOwner.Null);
            }
            else if (collectionAccessor.Length == 0)
            {
                marshalledArray = new MarshalledArray(null, 0, 0, DataOwner.Empty);
            }
            else if (marshalledArray.size >= collectionAccessor.Length)
            {
                System.Diagnostics.Debug.Assert(marshalledArray.data != null, "Expected a non-null buffer");
                System.Diagnostics.Debug.Assert(new Span<byte>(marshalledArray.data, marshalledArray.size * nativeElementSize).SequenceEqual(new Span<byte>(new byte[marshalledArray.size * nativeElementSize])), "Expected the buffer to be zero filled");

                // The user passed in a marshalled array that already has a large enough buffer
                // In usages this should be that the buffer is stack alloc'd
                marshalledArray.size = collectionAccessor.Length;
            }
            else
            {
                // This buffer needs to be zero filled because of exceptions than may be thrown during marshalling
                // We need to be sure that any memory that hasn't been allocated at exception time is zero filled so we don't try to any inner allocated items
                marshalledArray = new MarshalledArray(BindingsAllocator.AllocateZeroedBuffer(nativeElementSize * collectionAccessor.Capacity), collectionAccessor.Length, collectionAccessor.Capacity, elementCleanupRequired ? DataOwner.TempAllocatedCleanupRequired : DataOwner.TempAllocated);
            }
        }

        public static MarshalledArray AllocateAndCopyBlittable<TBlittable, TCollectionAccessor>(in TCollectionAccessor collectionAccessor)
            where TBlittable : unmanaged
            where TCollectionAccessor : struct, ICollectionMarshallingAccessor<TBlittable>
        {
            if (collectionAccessor.IsNull)
                return new MarshalledArray(null, 0, 0, DataOwner.Null);
            if (collectionAccessor.Length == 0)
                return new MarshalledArray(null, 0, 0, DataOwner.Empty);

            return new MarshalledArray(BindingsAllocator.AllocateAndCopyToBuffer<TBlittable>(collectionAccessor.AsSpan(), collectionAccessor.Capacity), collectionAccessor.Length, collectionAccessor.Capacity, DataOwner.TempAllocated);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void UnmarshalBlittable<TBlittable, TCollectionAccessor>(ref TCollectionAccessor collectionAccessor) where TBlittable : unmanaged
            where TCollectionAccessor : struct, ICollectionMarshallingAccessor<TBlittable>
        {
            switch (dataOwner)
            {
                case DataOwner.PinnedBuffer:
                case DataOwner.TempAllocated: // If we're still marked as temp allocated, then we didn't change anything
                    break;
                case DataOwner.PinnedBufferSizeChanged:
                case DataOwner.ExternallyOwned:
                    collectionAccessor.CollectionChanged(size);
                    new ReadOnlySpan<TBlittable>(data, size).CopyTo(collectionAccessor.AsSpan());
                    break;
                case DataOwner.NativeOwnedMemory:
                    collectionAccessor.CollectionChanged(size);
                    new ReadOnlySpan<TBlittable>(BindingsAllocator.GetNativeOwnedDataPointer(data), size).CopyTo(collectionAccessor.AsSpan());
                    break;
                case DataOwner.Empty:
                    collectionAccessor.SetEmpty();
                    break;
                case DataOwner.Null:
                    collectionAccessor.SetNull();
                    break;
                default:
                    ThrowUnimplementedDataOwnerCase(dataOwner);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<TNative> GetDataForUnmarshal<TManaged, TNative, TCollectionAccessor>(ref TCollectionAccessor collectionAccessor)
            where TCollectionAccessor : struct, ICollectionMarshallingAccessor<TManaged>
        {
            // NOTE: TCollectionAccessor may be a mutable struct, so we need to pass it by ref

            switch (dataOwner)
            {
                case DataOwner.PinnedBuffer:
                    return new Span<TNative>(data, size);
                case DataOwner.TempAllocated:
                case DataOwner.TempAllocatedCleanupRequired:
                case DataOwner.PinnedBufferSizeChanged:
                case DataOwner.ExternallyOwned:
                    collectionAccessor.CollectionChanged(size);
                    return new Span<TNative>(data, size);
                case DataOwner.NativeOwnedMemory:
                    collectionAccessor.CollectionChanged(size);
                    return new Span<TNative>(BindingsAllocator.GetNativeOwnedDataPointer(data), size);
                case DataOwner.Empty:
                    collectionAccessor.SetEmpty();
                    return default;
                case DataOwner.Null:
                    collectionAccessor.SetNull();
                    return default;
                default:
                    ThrowUnimplementedDataOwnerCase(dataOwner);
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Free()
        {
            switch (dataOwner)
            {
                case DataOwner.ExternallyOwned:
                case DataOwner.Empty:
                case DataOwner.Null:
                case DataOwner.PinnedBufferSizeChanged:
                case DataOwner.PinnedBuffer:
                    break;
                case DataOwner.TempAllocated:
                case DataOwner.TempAllocatedCleanupRequired:
                    BindingsAllocator.Free(data);
                    break;
                case DataOwner.NativeOwnedMemory:
                    // The native marshaller already freed the data buffer we passed in (or it was null before)
                    // But it returned NativeOwnedMemory, so we need to free it
                    BindingsAllocator.FreeNativeOwnedMemory(data);
                    break;
                default:
                    ThrowUnimplementedDataOwnerCase(dataOwner);
                    break;
            }
        }

        #region Error and Asserts

        // These error cases are written the way that they are to prevent allocations during Mono Jitting
        // In Mono an ldstr of a constant string allocates a string object at jit time
        // We have tests that assert no GC allocations and the string allocation causes them to fail (The jit allocations go through the normal string API's so the are profiled)
        // The static string load needs to go through an extra function call to prevent Roslyn from converting optimizing the static string into a constant interpolated string

        private const string UnimplementedDataOwnerCaseMessage = "Unhandled case for {0}.{1}";

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static string GetUnimplementedDataOwnerCaseMessage()
        {
            return UnimplementedDataOwnerCaseMessage;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowUnimplementedDataOwnerCase(DataOwner dataOwner)
        {
            throw new NotImplementedException(string.Format(GetUnimplementedDataOwnerCaseMessage(), nameof(DataOwner), dataOwner));
        }

        #endregion
    }
}
