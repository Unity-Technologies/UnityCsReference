// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

namespace Unity.Collections
{
    public static class NativeSliceExtensions
    {
        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray) where T : struct
        {
            return new NativeSlice<T>(thisArray);
        }

        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray, int start) where T : struct
        {
            return new NativeSlice<T>(thisArray, start);
        }

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


    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeSliceDebugView < >))]
    public unsafe struct NativeSlice<T> : IEnumerable<T>, IEquatable<NativeSlice<T>> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal byte*                                   m_Buffer;
        internal int                                     m_Stride;
        internal int                                     m_Length;


        public NativeSlice(NativeSlice<T> slice, int start) : this(slice, start, slice.Length - start) {}

        public NativeSlice(NativeSlice<T> slice, int start, int length)
        {

            m_Stride = slice.m_Stride;
            m_Buffer = slice.m_Buffer + m_Stride * start;
            m_Length = length;
        }

        public NativeSlice(NativeArray<T> array) : this(array, 0, array.Length) {}
        public NativeSlice(NativeArray<T> array, int start) : this(array, start, array.Length - start) {}

        public static implicit operator NativeSlice<T>(NativeArray<T> array)
        {
            return new NativeSlice<T>(array);
        }

        public NativeSlice(NativeArray<T> array, int start, int length)
        {

            m_Stride = UnsafeUtility.SizeOf<T>();
            var ptr = (byte*)array.m_Buffer + m_Stride * start;
            m_Buffer = ptr;
            m_Length = length;
        }

        // Keeps stride, changes length
        public NativeSlice<U> SliceConvert<U>() where U : struct
        {
            var sizeofU = UnsafeUtility.SizeOf<U>();

            NativeSlice<U> outputSlice;
            outputSlice.m_Buffer = m_Buffer;
            outputSlice.m_Stride = sizeofU;
            outputSlice.m_Length = (m_Length * m_Stride) / sizeofU;

            return outputSlice;
        }

        // Keeps length, changes stride
        public NativeSlice<U> SliceWithStride<U>(int offset) where U : struct
        {
            NativeSlice<U> outputSlice;
            outputSlice.m_Buffer = m_Buffer + offset;
            outputSlice.m_Stride = m_Stride;
            outputSlice.m_Length = m_Length;

            return outputSlice;
        }

        public NativeSlice<U> SliceWithStride<U>() where U : struct
        {
            return SliceWithStride<U>(0);
        }

        // These are double-whammy excluded to we can elide bounds checks in the Burst disassembly view
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadIndex(int index)
        {
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteIndex(int index)
        {
        }

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


        [WriteAccessRequired]
        public void CopyFrom(NativeSlice<T> slice)
        {

            UnsafeUtility.MemCpyStride(this.GetUnsafePtr(), Stride, slice.GetUnsafeReadOnlyPtr(), slice.Stride, UnsafeUtility.SizeOf<T>(), m_Length);
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array)
        {

            for (var i = 0; i != m_Length; i++)
                this[i] = array[i];
        }

        public void CopyTo(NativeArray<T> array)
        {

            var sizeOf = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpyStride(array.GetUnsafePtr(), sizeOf, this.GetUnsafeReadOnlyPtr(), Stride, sizeOf, m_Length);
        }

        public void CopyTo(T[] array)
        {

            for (var i = 0; i != m_Length; i++)
                array[i] = this[i];
        }

        public T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }

        public int      Stride => m_Stride;
        public int      Length => m_Length;

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

        public bool Equals(NativeSlice<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Stride == other.m_Stride && m_Length == other.m_Length;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NativeSlice<T> && Equals((NativeSlice<T>)obj);
        }

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

        public static bool operator==(NativeSlice<T> left, NativeSlice<T> right)
        {
            return left.Equals(right);
        }

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
    public static class NativeSliceUnsafeUtility
    {

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

            };

            return newSlice;
        }

        public static unsafe void* GetUnsafePtr<T>(this NativeSlice<T> nativeSlice) where T : struct
        {
            return nativeSlice.m_Buffer;
        }

        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeSlice<T> nativeSlice) where T : struct
        {
            return nativeSlice.m_Buffer;
        }
    }
}
