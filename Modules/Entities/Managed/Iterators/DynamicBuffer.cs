// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    /// <summary>
    /// An array-like data structure that can be used as a component.
    /// </summary>
    /// <example>
    /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.class"/>
    /// </example>
    /// <typeparam name="T">The data type stored in the buffer. Must be a value type.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(DynamicBufferDebugView<>))]
    public unsafe struct DynamicBuffer<T> : IQueryTypeParameter, IEnumerable<T>, INativeList<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        [NoAlias]
        BufferHeader* m_Buffer;

        // Stores original internal capacity of the buffer header, so heap excess can be removed entirely when trimming.
        private int m_InternalCapacity;

        internal AtomicSafetyHandle m_Safety0;
        internal AtomicSafetyHandle m_Safety1;
        internal int m_SafetyReadOnlyCount;
        internal int m_SafetyReadWriteCount;

        internal byte m_IsReadOnly;
        internal byte m_useMemoryInitPattern;
        internal byte m_memoryInitPattern;

        internal DynamicBuffer(BufferHeader* header, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly, bool useMemoryInitPattern, byte memoryInitPattern, int internalCapacity)
        {
            m_Buffer = header;
            m_Safety0 = safety;
            m_Safety1 = arrayInvalidationSafety;
            m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            m_IsReadOnly = (byte)(isReadOnly ? 1 : 0);
            m_InternalCapacity = internalCapacity;
            m_useMemoryInitPattern = (byte)(useMemoryInitPattern ? 1 : 0);
            m_memoryInitPattern = memoryInitPattern;
            CollectionHelper.InitNativeContainer<T>(m_Safety0);
            CollectionHelper.InitNativeContainer<T>(m_Safety1);
        }


        /// <summary>
        /// The number of elements the buffer holds.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.length"/>
        /// </example>
        public int Length
        {
            get
            {
                CheckReadAccess();
                return m_Buffer->Length;
            }
            set
            {
                //@TODO: This is unexpected...
                ResizeUninitialized(value);
            }
        }

        /// <summary>
        /// The number of elements the buffer can hold.
        /// </summary>
        /// <remarks>
        /// <paramref name="Capacity"/> can not be set lower than <see cref="Length"/> - this will raise an exception.
        /// If <paramref name="Capacity"/> grows greater than the internal capacity of the DynamicBuffer, memory external to the DynamicBuffer will be allocated.
        /// If <paramref name="Capacity"/> shrinks to the internal capacity of the DynamicBuffer or smaller, memory external to the DynamicBuffer will be freed.
        /// No effort is made to avoid costly reallocations when <paramref name="Capacity"/> changes slightly;
        /// if <paramref name="Capacity"/> is incremented by 1, an array 1 element bigger is allocated.
        /// </remarks>
        public int Capacity
        {
            get
            {
                CheckReadAccess();
                return m_Buffer->Capacity;
            }
            set
            {
                if (value < Length)
                    throw new InvalidOperationException($"Capacity {value} can't be set smaller than Length {Length}");
                CheckWriteAccessAndInvalidateArrayAliases();
                BufferHeader.SetCapacity(m_Buffer, value, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, m_useMemoryInitPattern == 1, m_memoryInitPattern, m_InternalCapacity);
            }
        }

        /// <summary>
        /// Reports whether container is empty.
        /// </summary>
        /// <value>True if this container empty.</value>
        public bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Whether the memory for this dynamic buffer has been allocated.
        /// </summary>
        public bool IsCreated
        {
            get
            {
                return m_Buffer != null;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckBounds(int index)
        {
            if ((uint)index >= (uint)Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{Length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccess()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccessAndInvalidateArrayAliases()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety1);
        }

        /// <summary>
        /// Array-like indexing operator.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.indexoperator"/>
        /// </example>
        /// <param name="index">The zero-based index.</param>
        public T this[int index]
        {
            get
            {
                CheckReadAccess();
                CheckBounds(index);
                return UnsafeUtility.ReadArrayElement<T>(BufferHeader.GetElementPointer(m_Buffer), index);
            }
            set
            {
                CheckWriteAccess();
                CheckBounds(index);
                UnsafeUtility.WriteArrayElement<T>(BufferHeader.GetElementPointer(m_Buffer), index, value);
            }
        }

        /// <summary>
        /// Gets the reference to the element at the given index.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>Returns the reference to the element at the index.</returns>
        public ref T ElementAt(int index)
        {
            CheckWriteAccess();
            CheckBounds(index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(BufferHeader.GetElementPointer(m_Buffer), index);
        }

        /// <summary>
        /// Sets the length of this buffer, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>If <paramref name="length"/> is less than the current
        /// length of the buffer, the length of the buffer is reduced while the
        /// capacity remains unchanged.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.resizeuninitialized"/>
        /// </example>
        /// <param name="length">The new length of the buffer.</param>
        public void ResizeUninitialized(int length)
        {
            EnsureCapacity(length);
            m_Buffer->Length = length;
        }

        /// <summary>
        /// Sets the length of this buffer, increasing the capacity if necessary.
        /// </summary>
        /// <remarks>If <paramref name="length"/> is less than the current
        /// length of the buffer, the length of the buffer is reduced while the
        /// capacity remains unchanged.</remarks>
        /// <param name="length">The new length of this buffer.</param>
        /// <param name="options">Whether to clear any newly allocated bytes to all zeroes.</param>
        public void Resize(int length, NativeArrayOptions options)
        {
            EnsureCapacity(length);

            var oldLength = m_Buffer->Length;
            m_Buffer->Length = length;
            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                var num = length - oldLength;
                byte* ptr = BufferHeader.GetElementPointer(m_Buffer);
                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemClear(ptr + oldLength * sizeOf, num * sizeOf);
            }
        }

        /// <summary>
        /// Ensures that the buffer has at least the specified capacity.
        /// </summary>
        /// <remarks>If <paramref name="length"/> is greater than the current <see cref="Capacity"/>
        /// of this buffer and greater than the capacity reserved with
        /// <see cref="InternalBufferCapacityAttribute"/>, this function allocates a new memory block
        /// and copies the current buffer to it. The number of elements in the buffer remains
        /// unchanged.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.reserve"/>
        /// </example>
        /// <param name="length">The buffer capacity is ensured to be at least this big.</param>
        public void EnsureCapacity(int length)
        {
            CheckWriteAccessAndInvalidateArrayAliases();
            BufferHeader.EnsureCapacity(m_Buffer, length, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData, m_useMemoryInitPattern == 1, m_memoryInitPattern);
        }

        /// <summary>
        /// Sets the buffer length to zero.
        /// </summary>
        /// <remarks>The capacity of the buffer remains unchanged. Buffer memory
        /// is not overwritten.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.clear"/>
        /// </example>
        public void Clear()
        {
            CheckWriteAccessAndInvalidateArrayAliases();

            m_Buffer->Length = 0;
        }

        /// <summary>
        /// Removes any excess capacity in the buffer.
        /// </summary>
        /// <remarks>Sets the buffer capacity to the current length.
        /// If the buffer memory size changes, the current contents
        /// of the buffer are copied to a new block of memory and the
        /// old memory is freed. If the buffer now fits in the space in the
        /// chunk reserved with <see cref="InternalBufferCapacityAttribute"/>,
        /// then the buffer contents are moved to the chunk.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.trimexcess"/>
        /// </example>
        public void TrimExcess()
        {
            CheckWriteAccessAndInvalidateArrayAliases();

            byte* oldPtr = m_Buffer->Pointer;
            int length = m_Buffer->Length;

            if (length == Capacity || oldPtr == null)
                return;

            int elemSize = UnsafeUtility.SizeOf<T>();
            int elemAlign = UnsafeUtility.AlignOf<T>();

            bool isInternal;
            byte* newPtr;

            // If the size fits in the internal buffer, prefer to move the elements back there.
            if (length <= m_InternalCapacity)
            {
                newPtr = (byte*)(m_Buffer + 1);
                isInternal = true;
            }
            else
            {
                newPtr = (byte*)Memory.Unmanaged.Allocate((long)elemSize * length, elemAlign, Allocator.Persistent);
                isInternal = false;
            }

            UnsafeUtility.MemCpy(newPtr, oldPtr, (long)elemSize * length);

            m_Buffer->Capacity = Math.Max(length, m_InternalCapacity);
            m_Buffer->Pointer = isInternal ? null : newPtr;

            Memory.Unmanaged.Free(oldPtr, Allocator.Persistent);
        }

        /// <summary>
        /// Adds an element to the end of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.add"/>
        /// </example>
        /// <param name="elem">The element to add to the buffer.</param>
        /// <returns>The index of the added element, which is equal to the new length of the buffer minus one.</returns>
        public int Add(T elem)
        {
            CheckWriteAccess();
            int length = Length;
            ResizeUninitialized(length + 1);
            this[length] = elem;
            return length;
        }

        /// <summary>
        /// Inserts an element at the specified index, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.insert"/>
        /// </example>
        /// <param name="index">The position at which to insert the new element.</param>
        /// <param name="elem">The element to add to the buffer.</param>
        public void Insert(int index, T elem)
        {
            CheckWriteAccess();
            int length = Length;
            ResizeUninitialized(length + 1);
            CheckBounds(index); //CheckBounds after ResizeUninitialized since index == length is allowed
            int elemSize = UnsafeUtility.SizeOf<T>();
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            UnsafeUtility.MemMove(basePtr + (index + 1) * elemSize, basePtr + index * elemSize, (long)elemSize * (length - index));
            this[index] = elem;
        }

        /// <summary>
        /// Adds all the elements from <paramref name="newElems"/> to the end
        /// of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.addrange"/>
        /// </example>
        /// <param name="newElems">The native array of elements to insert.</param>
        public void AddRange(NativeArray<T> newElems)
        {
            CheckWriteAccess();
            int elemSize = UnsafeUtility.SizeOf<T>();
            int oldLength = Length;
            ResizeUninitialized(oldLength + newElems.Length);

            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            UnsafeUtility.MemCpy(basePtr + (long)oldLength * elemSize, newElems.GetUnsafeReadOnlyPtr<T>(), (long)elemSize * newElems.Length);
        }

        /// <summary>
        /// Adds all the elements from <paramref name="newElems"/> to the end
        /// of the buffer, resizing as necessary.
        /// </summary>
        /// <remarks>The buffer is resized if it has no additional capacity.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.addrange"/>
        /// </example>
        /// <param name="newElems">The ReadOnlySpan of elements to insert.</param>
        public void AddRange(ReadOnlySpan<T> newElems)
        {
            CheckWriteAccess();
            int elemSize = UnsafeUtility.SizeOf<T>();
            int oldLength = Length;
            ResizeUninitialized(oldLength + newElems.Length);

            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            fixed(T* elementPointer = newElems)
            {
                UnsafeUtility.MemCpy(basePtr + (long)oldLength * elemSize, elementPointer, (long)elemSize * newElems.Length);
            }
        }

        /// <summary>
        /// Removes the specified number of elements, starting with the element at the specified index.
        /// </summary>
        /// <remarks>The buffer capacity remains unchanged.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.removerange"/>
        /// </example>
        /// <param name="index">The first element to remove.</param>
        /// <param name="count">How many elements tot remove.</param>
        public void RemoveRange(int index, int count)
        {
            CheckWriteAccess();
            CheckBounds(index);
            if (count == 0)
                return;
            CheckBounds(index + count - 1);

            int elemSize = UnsafeUtility.SizeOf<T>();
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);

            UnsafeUtility.MemMove(basePtr + index * elemSize, basePtr + (index + count) * elemSize, (long)elemSize * (Length - count - index));

            m_Buffer->Length -= count;
        }

        /// <summary>
        /// Removes the specified number of elements, starting with the element at the specified index. It replaces the
        /// elements that were removed with a range of elements from the back of the buffer. This is more efficient
        /// than moving all elements following the removed elements, but does change the order of elements in the buffer.
        /// </summary>
        /// <remarks>The buffer capacity remains unchanged.</remarks>
        /// <param name="index">The first element to remove.</param>
        /// <param name="count">How many elements tot remove.</param>
        public void RemoveRangeSwapBack(int index, int count)
        {
            CheckWriteAccess();
            CheckBounds(index);
            if (count == 0)
                return;
            CheckBounds(index + count - 1);

            ref var l = ref m_Buffer->Length;
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            int elemSize = UnsafeUtility.SizeOf<T>();
            int copyFrom = math.max(l - count, index + count);
            void* dst = basePtr + index * elemSize;
            void* src = basePtr + copyFrom * elemSize;
            UnsafeUtility.MemMove(dst, src, (l - copyFrom) * elemSize);
            l -= count;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.removeat"/>
        /// </example>
        /// <param name="index">The index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        /// <summary>
        /// Removes the element at the specified index and swaps the last element into its place. This is more efficient
        /// than moving all elements following the removed element, but does change the order of elements in the buffer.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        public void RemoveAtSwapBack(int index)
        {
            CheckWriteAccess();
            CheckBounds(index);

            ref var l = ref m_Buffer->Length;
            l -= 1;
            int newLength = l;
            if (index != newLength)
            {
                byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
                UnsafeUtility.WriteArrayElement(basePtr, index, UnsafeUtility.ReadArrayElement<T>(basePtr, newLength));
            }
        }

        /// <summary>
        /// Gets an <see langword="unsafe"/> read/write pointer to the contents of the buffer.
        /// </summary>
        /// <remarks>This function can only be called in unsafe code contexts.</remarks>
        /// <returns>A typed, unsafe pointer to the first element in the buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafePtr()
        {
            CheckWriteAccess();
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        /// <summary>
        /// Gets an <see langword="unsafe"/> read-only pointer to the contents of the buffer.
        /// </summary>
        /// <remarks>This function can only be called in unsafe code contexts.</remarks>
        /// <returns>A typed, unsafe pointer to the first element in the buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetUnsafeReadOnlyPtr()
        {
            CheckReadAccess();
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertReinterpretSizesMatch<U>() where U : struct
        {
            if (UnsafeUtility.SizeOf<U>() != UnsafeUtility.SizeOf<T>())
                throw new InvalidOperationException($"Types {typeof(U)} and {typeof(T)} are of different sizes; cannot reinterpret");
        }

        /// <summary>
        /// Returns a dynamic buffer of a different type, pointing to the same buffer memory.
        /// </summary>
        /// <remarks>No memory modification occurs. The reinterpreted type must be the same size
        /// in memory as the original type.</remarks>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.reinterpret"/>
        /// </example>
        /// <typeparam name="U">The reinterpreted type.</typeparam>
        /// <returns>A dynamic buffer of the reinterpreted type.</returns>
        /// <exception cref="InvalidOperationException">If the reinterpreted type is a different
        /// size than the original.</exception>
        public DynamicBuffer<U> Reinterpret<U>() where U : unmanaged
        {
            AssertReinterpretSizesMatch<U>();
            // NOTE: We're forwarding the internal capacity along to this aliased, type-punned buffer.
            // That's OK, because if mutating operations happen they are all still the same size.
            return new DynamicBuffer<U>(m_Buffer, m_Safety0, m_Safety1, m_IsReadOnly == 1, m_useMemoryInitPattern == 1, m_memoryInitPattern, m_InternalCapacity);
        }

        /// <summary>
        /// Return a native array that aliases the original buffer contents.
        /// </summary>
        /// <remarks>You can only access the native array as long as the
        /// the buffer memory has not been reallocated. Several dynamic buffer operations,
        /// such as <see cref="Add"/> and <see cref="TrimExcess"/> can result in
        /// buffer reallocation.</remarks>
        /// <returns>A NativeArray view of this buffer.</returns>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.asnativearray"/>
        /// </example>
        public NativeArray<T> AsNativeArray()
        {
            CheckReadAccess();

            var shadow = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(BufferHeader.GetElementPointer(m_Buffer), Length, Allocator.None);
            var handle = m_Safety1;
            AtomicSafetyHandle.UseSecondaryVersion(ref handle);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref shadow, handle);

            return shadow;
        }

        /// <summary>
        /// Provides an enumerator for iterating over the buffer elements.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.getenumerator"/>
        /// </example>
        /// <returns>The enumerator.</returns>
        public NativeArray<T>.Enumerator GetEnumerator()
        {
            var array = AsNativeArray();
            return new NativeArray<T>.Enumerator(ref array);
        }

        IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw new NotImplementedException(); }

        /// <summary>
        /// Copies the buffer into a new native array.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.tonativearray"/>
        /// </example>
        /// <param name="allocator">The type of memory allocation to use when creating the
        /// native array.</param>
        /// <returns>A native array containing copies of the buffer elements.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            return CollectionHelper.CreateNativeArray<T>(AsNativeArray(), allocator);
        }

        /// <summary>
        /// Copies all the elements from the specified native array into this dynamic buffer.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.nativearray"/>
        /// </example>
        /// <param name="v">The native array containing the elements to copy.</param>
        public void CopyFrom(NativeArray<T> v)
        {
            //todo remove workaround: See DOTS-1454
            ResizeUninitialized(v.Length);
            NativeSlice<T> vs = new NativeSlice<T>(v);
            vs.CopyTo(AsNativeArray());
        }

        /// <summary>
        /// Copies all the elements from the specified native slice into this dynamic buffer.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.nativeslice"/>
        /// </example>
        /// <param name="v">The native slice containing the elements to copy.</param>
        public void CopyFrom(NativeSlice<T> v)
        {
            ResizeUninitialized(v.Length);
            v.CopyTo(AsNativeArray());
        }

        /// <summary>
        /// Copies all the elements from another dynamic buffer.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.dynamicbuffer"/>
        /// </example>
        /// <param name="v">The dynamic buffer containing the elements to copy.</param>
        public void CopyFrom(DynamicBuffer<T> v)
        {
            ResizeUninitialized(v.Length);

            v.CheckReadAccess();
            CheckWriteAccess();

            UnsafeUtility.MemCpy(BufferHeader.GetElementPointer(m_Buffer),
                BufferHeader.GetElementPointer(v.m_Buffer), Length * UnsafeUtility.SizeOf<T>());
        }

        /// <summary>
        /// Copies all the elements from an array.
        /// </summary>
        /// <example>
        /// <code source="../../DocCodeSamples.Tests/DynamicBufferExamples.cs" language="csharp" region="dynamicbuffer.copyfrom.array"/>
        /// </example>
        /// <param name="v">A C# array containing the elements to copy.</param>
        /// <exception cref="ArgumentNullException">Thrown if the given array is empty.</exception>
        public void CopyFrom(T[] v)
        {
            if (v == null)
                throw new ArgumentNullException(nameof(v));

            ResizeUninitialized(v.Length);
            CheckWriteAccess();

            GCHandle gcHandle = GCHandle.Alloc((object)v, GCHandleType.Pinned);
            IntPtr num = gcHandle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(BufferHeader.GetElementPointer(m_Buffer), (void*)num, Length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }
    }

    internal sealed class DynamicBufferDebugView<T>  where T : unmanaged
    {
        private DynamicBuffer<T> _buffer;
        public DynamicBufferDebugView(DynamicBuffer<T> source)
        {
            _buffer = source;
        }

        public T[] Items => _buffer.AsNativeArray().ToArray();
    }
}
