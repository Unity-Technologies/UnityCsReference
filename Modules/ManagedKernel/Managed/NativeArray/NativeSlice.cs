// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace Unity.Collections
{
    ///<exclude />
    public static class NativeSliceExtensions
    {
        ///<summary>[[NativeArray] extension methods for creating a <see cref="NativeSlice{T}" />.</summary>
        ///<param name="thisArray">NativeArray to slice.</param>
        ///<returns>NativeSlice.</returns>
        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray) where T : struct
        {
            return new NativeSlice<T>(thisArray);
        }

        ///<summary>[[NativeArray] extension methods for creating a <see cref="NativeSlice{T}" />.</summary>
        ///<param name="thisArray">NativeArray to slice.</param>
        ///<param name="start">Slice start index.</param>
        ///<returns>NativeSlice.</returns>
        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray, int start) where T : struct
        {
            return new NativeSlice<T>(thisArray, start);
        }

        ///<summary>[[NativeArray] extension methods for creating a <see cref="NativeSlice{T}" />.</summary>
        ///<param name="thisArray">NativeArray to slice.</param>
        ///<param name="length">Slice length.</param>
        ///<param name="start">Slice start index.</param>
        ///<returns>NativeSlice.</returns>
        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray, int start, int length) where T : struct
        {
            return new NativeSlice<T>(thisArray, start, length);
        }

        public static NativeSlice<T> Slice<T>(this NativeSlice<T> thisSlice) where T : struct
        {
            return thisSlice;
        }

        public static NativeSlice<T> Slice<T>(this NativeSlice<T> thisSlice, int start) where T : struct
        {
            return new NativeSlice<T>(thisSlice, start);
        }

        public static NativeSlice<T> Slice<T>(this NativeSlice<T> thisSlice, int start, int length) where T : struct
        {
            return new NativeSlice<T>(thisSlice, start, length);
        }
    }

    ///<summary>Provides a view on a buffer of native memory most commonly acquired from a <see cref="Unity.Collections.NativeArray{T}" />.</summary>
    ///<remarks>A <c>NativeSlice</c> includes safety mechanisms for use with the job system. A <c>NativeSlice</c> doesn't own any memory allocations and can't be disposed, unlike a <c>NativeArray</c><see cref="Unity.Collections.NativeArray{T}" />. 
    ///                
    ///A <c>NativeSlice</c> supports a stride value and doesn't necessarily represent a contiguous memory range. The stride value 
    ///determines the number of bytes from the first byte of the element to the first byte of the next element. The stride value 
    ///must always be a multiple of the size of the type of the slice in bytes. The stride value allows you to skip elements from the underlying buffer.
    ///                
    ///By default, the stride is set to the size of the type of slice in bytes. This means that the slice represents a contiguous memory range.
    ///If you don't need a stride and are only working with contiguous memory ranges, use <see cref="Unity.Collections.NativeArray{T}" /> instead.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeSliceDebugView<>))]
    public unsafe struct NativeSlice<T> : IEnumerable<T>, IEquatable<NativeSlice<T>> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal byte*                                   m_Buffer;
        internal int                                     m_Stride;
        internal int                                     m_Length;

        internal int                                     m_MinIndex;
        internal int                                     m_MaxIndex;
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal AtomicSafetyHandle                      m_Safety;

        ///<summary>Constructs a new NativeSlice from another NativeSlice, with a defined start index.</summary>
        ///<remarks>Constructs a new <c>NativeSlice</c> that provides a view over the memory of another existing <c>NativeSlice</c>, beginning at a specified starting index.</remarks>
        ///<param name="slice">The <see cref="Unity.Collections.NativeSlice{T}" /> to use.</param>
        ///<param name="start">The index of the first element from the source slice to include in the new slice.</param>
        public NativeSlice(NativeSlice<T> slice, int start) : this(slice, start, slice.Length - start) {}

        ///<summary>Constructs a new NativeSlice of defined length <c>length</c>, from another NativeSlice, with a defined start index.</summary>
        ///<remarks>Constructs a new <c>NativeSlice</c> that provides a view over a defined sub-region of another existing <c>NativeSlice</c>, beginning at a specified starting index and covering an exact number of elements.</remarks>
        ///<param name="slice">The <see cref="Unity.Collections.NativeSlice{T}" /> to use.</param>
        ///<param name="start">The index of the first element from the source slice to include in the new slice.</param>
        ///<param name="length">The number of elements that the new NativeSlice will have.</param>
        public NativeSlice(NativeSlice<T> slice, int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), $"Slice start {start} < 0.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"Slice length {length} < 0.");
            if (start + length > slice.Length)
                throw new ArgumentException($"Slice start + length ({start + length}) range must be <= slice.Length ({slice.Length})");
            if ((slice.m_MinIndex != 0 || slice.m_MaxIndex != slice.m_Length - 1) && (start < slice.m_MinIndex || slice.m_MaxIndex < start || slice.m_MaxIndex < start + length - 1))
                throw new ArgumentException("Slice may not be used on a restricted range slice", nameof(slice));

            m_MinIndex = 0;
            m_MaxIndex = length - 1;
            m_Safety = slice.m_Safety;

            m_Stride = slice.m_Stride;
            m_Buffer = slice.m_Buffer + m_Stride * start;
            m_Length = length;
        }

        ///<summary>Constructs a new NativeSlice from a NativeArray.</summary>
        ///<remarks>Constructs a new <c>NativeSlice</c> that provides a view over the memory of the specified <c>NativeArray</c>.</remarks>
        ///<param name="array">The <see cref="Unity.Collections.NativeArray{T}" /> to use.</param>
        public NativeSlice(NativeArray<T> array) : this(array, 0, array.Length) {}
        ///<summary>Constructs a new NativeSlice from a NativeArray, with a defined start index.</summary>
        ///<remarks>Constructs a new <c>NativeSlice</c> that provides a view over the memory of the specified <c>NativeArray</c>, beginning at a specified starting index.</remarks>
        ///<param name="array">The <see cref="Unity.Collections.NativeArray{T}" /> to use.</param>
        ///<param name="start">The index of the first element from the source to include in the slice.</param>
        public NativeSlice(NativeArray<T> array, int start) : this(array, start, array.Length - start) {}

        ///<summary>Implicit operator to create a <see cref="Unity.Collections.NativeSlice{T}" /> from a <see cref="Unity.Collections.NativeArray{T}" />.</summary>
        ///<param name="array">The <see cref="Unity.Collections.NativeArray{T}" /> to use.</param>
        public static implicit operator NativeSlice<T>(NativeArray<T> array)
        {
            return new NativeSlice<T>(array);
        }

        ///<summary>Constructs a new NativeSlice of defined length <c>length</c>, from a NativeArray with a defined start index.</summary>
        ///<remarks>Constructs a new <c>NativeSlice</c> that provides a view over a sub-region of the specified <c>NativeArray</c>, beginning at a specified starting index and covering a specific number of elements.</remarks>
        ///<param name="array">The <see cref="Unity.Collections.NativeArray{T}" /> to use.</param>
        ///<param name="start">The index of the first element from the source to include in the slice.</param>
        ///<param name="length">The number of elements that the new NativeSlice will have.</param>
        public NativeSlice(NativeArray<T> array, int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), $"Slice start {start} < 0.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"Slice length {length} < 0.");
            if (start + length > array.Length)
                throw new ArgumentException($"Slice start + length ({start + length}) range must be <= array.Length ({array.Length})");
            if ((array.m_MinIndex != 0 || array.m_MaxIndex != array.m_Length - 1) && (start < array.m_MinIndex || array.m_MaxIndex < start || array.m_MaxIndex < start + length - 1))
                throw new ArgumentException("Slice may not be used on a restricted range array", nameof(array));
            if (start + length < 0)
                throw new ArgumentException("Slice start + length ({start + length}) causes an integer overflow");

            m_MinIndex = 0;
            m_MaxIndex = length - 1;
            m_Safety = array.m_Safety;

            m_Stride = UnsafeUtility.SizeOf<T>();
            var ptr = (byte*)array.m_Buffer + m_Stride * start;
            m_Buffer = ptr;
            m_Length = length;
        }

        // Keeps stride, changes length
        ///<summary>Reinterprets a NativeSlice with a different data type (type punning).</summary>
        ///<returns>A new <see cref="Unity.Collections.NativeSlice{T}" /> that views the same memory, but is reinterpreted as the target type.</returns>
        public NativeSlice<U> SliceConvert<U>() where U : struct
        {
            var sizeofU = UnsafeUtility.SizeOf<U>();

            NativeSlice<U> outputSlice;
            outputSlice.m_Buffer = m_Buffer;
            outputSlice.m_Stride = sizeofU;
            outputSlice.m_Length = (m_Length * m_Stride) / sizeofU;

            if (m_Stride != UnsafeUtility.SizeOf<T>())
                throw new InvalidOperationException("SliceConvert requires that stride matches the size of the source type");
            if (m_MinIndex != 0 || m_MaxIndex != m_Length - 1)
                throw new InvalidOperationException("SliceConvert may not be used on a restricted range array");
            if (m_Stride * m_Length % sizeofU != 0)
                throw new InvalidOperationException("SliceConvert requires that Length * sizeof(T) is a multiple of sizeof(U).");

            outputSlice.m_MinIndex = 0;
            outputSlice.m_MaxIndex = outputSlice.m_Length - 1;
            outputSlice.m_Safety = m_Safety;
            return outputSlice;
        }

        // Keeps length, changes stride
        ///<summary>SliceWithStride.</summary>
        ///<param name="offset">Stride offset.</param>
        ///<returns>NativeSlice.</returns>
        public NativeSlice<U> SliceWithStride<U>(int offset) where U : struct
        {
            NativeSlice<U> outputSlice;
            outputSlice.m_Buffer = m_Buffer + offset;
            outputSlice.m_Stride = m_Stride;
            outputSlice.m_Length = m_Length;

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "SliceWithStride offset must be >= 0");
            if (offset + UnsafeUtility.SizeOf<U>() > UnsafeUtility.SizeOf<T>())
                throw new ArgumentException("SliceWithStride sizeof(U) + offset must be <= sizeof(T)", nameof(offset));

            outputSlice.m_MinIndex = m_MinIndex;
            outputSlice.m_MaxIndex = m_MaxIndex;
            outputSlice.m_Safety = m_Safety;
            return outputSlice;
        }

        ///<summary>SliceWithStride.</summary>
        ///<returns>NativeSlice.</returns>
        public NativeSlice<U> SliceWithStride<U>() where U : struct
        {
            return SliceWithStride<U>(0);
        }

        // These are double-whammy excluded to we can elide bounds checks in the Burst disassembly view
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadIndex(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            var versionPtr = (int*)m_Safety.versionNode;
            if (m_Safety.version != ((*versionPtr) & AtomicSafetyHandle.ReadCheck))
                AtomicSafetyHandle.CheckReadAndThrowNoEarlyOut(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteIndex(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            var versionPtr = (int*)m_Safety.versionNode;
            if (m_Safety.version != ((*versionPtr) & AtomicSafetyHandle.WriteCheck))
                AtomicSafetyHandle.CheckWriteAndThrowNoEarlyOut(m_Safety);
        }

        ///<summary>Accesses <see cref="Unity.Collections.NativeSlice{T}" /> elements by index.</summary>
        ///<remarks>Structs are returned by value and not by reference. This property also takes the NativeSlice's stride into account.</remarks>
        public T this[int index]
        {
            get
            {
                CheckReadIndex(index);
                return UnsafeUtility.ReadArrayElementWithStride<T>(m_Buffer, index, m_Stride);
            }

            [WriteAccessRequired]
            set
            {
                CheckWriteIndex(index);
                UnsafeUtility.WriteArrayElementWithStride(m_Buffer, index, m_Stride, value);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index)
        {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                    "You can use double buffering strategies to avoid race conditions due to " +
                    "reading & writing in parallel to the same elements from a job.");

            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }


        ///<summary>Copies all the elements from a <see cref="Unity.Collections.NativeSlice{T}" /> or managed array of the same length.</summary>
        ///<param name="slice">The <see cref="Unity.Collections.NativeSlice{T}" /> to copy the elements from.</param>
        [WriteAccessRequired]
        public void CopyFrom(NativeSlice<T> slice)
        {
            if (Length != slice.Length)
                throw new ArgumentException($"slice.Length ({slice.Length}) does not match the Length of this instance ({Length}).", nameof(slice));

            UnsafeUtility.MemCpyStride(this.GetUnsafePtr(), Stride, slice.GetUnsafeReadOnlyPtr(), slice.Stride, UnsafeUtility.SizeOf<T>(), m_Length);
        }

        ///<summary>Copies all the elements from a <see cref="Unity.Collections.NativeSlice{T}" /> or managed array of the same length.</summary>
        ///<param name="array">The array to copy elements from.</param>
        [WriteAccessRequired]
        public void CopyFrom(T[] array)
        {
            if (Length != array.Length)
                throw new ArgumentException($"array.Length ({array.Length}) does not match the Length of this instance ({Length}).", nameof(array));
            unsafe
            {
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                IntPtr addr = handle.AddrOfPinnedObject();

                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpyStride(this.GetUnsafePtr(), Stride, (byte*)addr, sizeOf, sizeOf, m_Length);

                handle.Free();
            }
        }

        ///<summary>Copies all the elements of a <see cref="Unity.Collections.NativeSlice{T}" /> to a <see cref="Unity.Collections.NativeArray{T}" /> or managed array of the same length.</summary>
        ///<param name="array">The array to copy elements to.</param>
        public void CopyTo(NativeArray<T> array)
        {
            if (Length != array.Length)
                throw new ArgumentException($"array.Length ({array.Length}) does not match the Length of this instance ({Length}).", nameof(array));

            var sizeOf = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpyStride(array.GetUnsafePtr(), sizeOf, this.GetUnsafeReadOnlyPtr(), Stride, sizeOf, m_Length);
        }

        ///<summary>Copies all the elements of a <see cref="Unity.Collections.NativeSlice{T}" /> to a <see cref="Unity.Collections.NativeArray{T}" /> or managed array of the same length.</summary>
        ///<param name="array">The array to copy elements to.</param>
        public void CopyTo(T[] array)
        {
            if (Length != array.Length)
                throw new ArgumentException($"array.Length ({array.Length}) does not match the Length of this instance ({Length}).", nameof(array));
            unsafe
            {
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                IntPtr addr = handle.AddrOfPinnedObject();

                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpyStride((byte*)addr, sizeOf, this.GetUnsafeReadOnlyPtr(), Stride, sizeOf, m_Length);

                handle.Free();
            }
        }

        ///<summary>Converts a <see cref="Unity.Collections.NativeSlice{T}" /> to managed array.</summary>
        ///<returns>A managed array with a copy of the contents of the NativeSlice.</returns>
        public T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }

        ///<summary>Gets the stride value for the <see cref="Unity.Collections.NativeSlice{T}" /> instance.</summary>
        ///<remarks>The stride value allows a NativeSlice to represent non-contiguous memory ranges.</remarks>
        public int      Stride => m_Stride;
        ///<summary>Represents the number of elements in a <see cref="Unity.Collections.NativeSlice{T}" />.</summary>
        public int      Length
        {
            get
            {
                return m_Length;
            }
        }

        ///<summary>Gets an enumerator to iterate through the elements of a <see cref="Unity.Collections.NativeSlice{T}" />.</summary>
        ///<returns>An enumerator that can iterate through the elements of a NativeSlice.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>
        {
            NativeSlice<T> m_Array;
            int m_Index;

            public Enumerator(ref NativeSlice<T> array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_Index++;
                return m_Index < m_Array.Length;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            // Let NativeSlice indexer check for out of range.
            public T Current => m_Array[m_Index];

            object IEnumerator.Current => Current;
        }

        ///<exclude />
        public bool Equals(NativeSlice<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Stride == other.m_Stride && m_Length == other.m_Length;
        }

        ///<exclude />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NativeSlice<T> && Equals((NativeSlice<T>)obj);
        }

        ///<exclude />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_Buffer;
                hashCode = (hashCode * 397) ^ m_Stride;
                hashCode = (hashCode * 397) ^ m_Length;
                return hashCode;
            }
        }

        ///<exclude />
        public static bool operator==(NativeSlice<T> left, NativeSlice<T> right)
        {
            return left.Equals(right);
        }

        ///<exclude />
        public static bool operator!=(NativeSlice<T> left, NativeSlice<T> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="NativeArray{T}"/>
    /// </summary>
    internal sealed class NativeSliceDebugView<T> where T : struct
    {
        NativeSlice<T> m_Array;

        public NativeSliceDebugView(NativeSlice<T> array)
        {
            m_Array = array;
        }

        public T[] Items
        {
            get { return m_Array.ToArray(); }
        }
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    ///<summary>Contains unsafe methods for working with <see cref="NativeSlice{T}" /> instances.</summary>
    public static class NativeSliceUnsafeUtility
    {
        ///<summary>Gets the <see cref="AtomicSafetyHandle" /> for a <see cref="NativeSlice{T}" />.</summary>
        ///<param name="slice">The <see cref="NativeSlice{T}" /> to check.</param>
        ///<returns>The <see cref="AtomicSafetyHandle" /> of <c>slice</c>.</returns>
        public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(NativeSlice<T> slice) where T : struct
        {
            return slice.m_Safety;
        }

        ///<summary>Set the <see cref="AtomicSafetyHandle" /> on <see cref="NativeSlice{T}" />.</summary>
        ///<param name="slice">The NativeSlice to set.</param>
        ///<param name="safety">The AtomicSafetyHandle to use.</param>
        public static void SetAtomicSafetyHandle<T>(ref NativeSlice<T> slice, AtomicSafetyHandle safety) where T : struct
        {
            slice.m_Safety = safety;
        }


        ///<summary>Creates a new <see cref="NativeSlice{T}" /> from existing data.</summary>
        ///<param name="length">Number of elements in the data set.</param>
        ///<param name="dataPointer">Memory pointer to the data.</param>
        ///<param name="stride">Stride in bytes.</param>
        ///<returns>The created NativeSlice.</returns>
        public static unsafe NativeSlice<T> ConvertExistingDataToNativeSlice<T>(void* dataPointer, int stride, int length) where T : struct
        {
            if (length < 0)
                throw new ArgumentException($"Invalid length of '{length}'. It must be greater than 0.", nameof(length));
            if (stride < 0)
                throw new ArgumentException($"Invalid stride '{stride}'. It must be greater than 0.", nameof(stride));

            var newSlice = new NativeSlice<T>
            {
                m_Stride = stride,
                m_Buffer = (byte*)dataPointer,
                m_Length = length,

                m_MinIndex = 0,
                m_MaxIndex = length - 1,
            };

            return newSlice;
        }

        ///<summary>Gets a <see cref="NativeSlice{T}" /> memory buffer pointer and checks whether the native array can be written to.</summary>
        ///<param name="nativeSlice">The NativeSlice to check.</param>
        ///<returns>The memory buffer pointer of <c>nativeSlice</c>.</returns>
        public static unsafe void* GetUnsafePtr<T>(this NativeSlice<T> nativeSlice) where T : struct
        {
            AtomicSafetyHandle.CheckWriteAndThrow(nativeSlice.m_Safety);
            return nativeSlice.m_Buffer;
        }

        ///<summary>Gets a <see cref="NativeSlice{T}" /> memory buffer pointer and checks whether the native array can be read from.</summary>
        ///<param name="nativeSlice">The NativeSlice to check.</param>
        ///<returns>The memory buffer pointer of <c>nativeSlice</c>.</returns>
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeSlice<T> nativeSlice) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(nativeSlice.m_Safety);
            return nativeSlice.m_Buffer;
        }
    }
}
