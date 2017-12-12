// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    public static class NativeSliceExtensions
    {
        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray) where T : struct
        {
            return new NativeSlice<T>(thisArray);
        }

        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray, int length) where T : struct
        {
            return new NativeSlice<T>(thisArray, length);
        }

        public static NativeSlice<T> Slice<T>(this NativeArray<T> thisArray, int start, int length) where T : struct
        {
            return new NativeSlice<T>(thisArray, start, length);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeSliceDebugView < >))]
    public struct NativeSlice<T> : IEnumerable<T> where T : struct
    {
        internal IntPtr                                  m_Buffer;
        internal int                                     m_Stride;
        internal int                                     m_Length;


        public static implicit operator NativeSlice<T>(NativeArray<T> array)
        {
            return new NativeSlice<T>(array);
        }

        public NativeSlice(NativeArray<T> array) : this(array, 0, array.Length) {}
        public NativeSlice(NativeArray<T> array, int start) : this(array, start, array.Length - start) {}

        public unsafe NativeSlice(NativeArray<T> array, int start, int length)
        {

            m_Stride = UnsafeUtility.SizeOf<T>();
            byte* ptr = (byte*)array.m_Buffer + m_Stride * start;
            m_Buffer = (IntPtr)ptr;
            m_Length = length;

        }

        // Keeps stride, changes length
        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
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
        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public unsafe NativeSlice<U> SliceWithStride<U>(int offset) where U : struct
        {

            NativeSlice<U> outputSlice;
            byte* ptr = (byte*)m_Buffer + offset;
            outputSlice.m_Buffer = (IntPtr)ptr;
            outputSlice.m_Stride = m_Stride;
            outputSlice.m_Length = m_Length;

            return outputSlice;
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public NativeSlice<U> SliceWithStride<U>() where U : struct
        {
            return SliceWithStride<U>(0);
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public NativeSlice<U> SliceWithStride<U>(string offsetFieldName) where U : struct
        {
            var offsetOf = UnsafeUtility.OffsetOf<T>(offsetFieldName);
            return SliceWithStride<U>(offsetOf);
        }

        public unsafe T this[int index]
        {
            get
            {

                return UnsafeUtility.ReadArrayElementWithStride<T>(m_Buffer, index, m_Stride);
            }

            set
            {

                UnsafeUtility.WriteArrayElementWithStride(m_Buffer, index, m_Stride, value);
            }
        }


        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyFrom(NativeSlice<T> slice)
        {

            for (var i = 0; i != m_Length; i++)
                this[i] = slice[i];
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyFrom(T[] array)
        {

            for (var i = 0; i != m_Length; i++)
                this[i] = array[i];
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyTo(NativeArray<T> array)
        {

            for (var i = 0; i != m_Length; i++)
                array[i] = this[i];
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyTo(T[] array)
        {
            for (var i = 0; i != m_Length; i++)
                array[i] = this[i];
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }

        public int      Stride { get { return m_Stride; } }
        public int      Length { get { return m_Length; } }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal struct Enumerator : IEnumerator<T>
        {
            private NativeSlice<T> m_Array;
            private int m_Index;

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

            public T Current
            {
                get
                {
                    // Let NativeSlice indexer check for out of range.
                    return m_Array[m_Index];
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="NativeArray{T}"/>
    /// </summary>
    internal sealed class NativeSliceDebugView<T> where T : struct
    {
        private NativeSlice<T> m_Array;

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
        public static NativeSlice<T> ConvertExistingDataToNativeSlice<T>(IntPtr dataPointer, int length) where T : struct
        {
            var newSlice = new NativeSlice<T>
            {
                m_Stride = UnsafeUtility.SizeOf<T>(),
                m_Buffer = dataPointer,
                m_Length = length,

            };

            return newSlice;
        }

        public static unsafe NativeSlice<T> ConvertExistingDataToNativeSlice<T>(IntPtr dataPointer, int start, int length, int stride) where T : struct
        {
            if (length < 0)
                throw new System.ArgumentException(String.Format("Invalid length of '{0}'. It must be greater than 0.", length));
            if (start < 0)
                throw new System.ArgumentException(String.Format("Invalid start index of '{0}'. It must be greater than 0.", start));
            if (stride < 0)
                throw new System.ArgumentException(String.Format("Invalid stride '{0}'. It must be greater than 0.", stride));

            byte* ptr = (byte*)dataPointer + start;
            var newSlice = new NativeSlice<T>
            {
                m_Stride = stride,
                m_Buffer = (IntPtr)ptr,
                m_Length = length,

            };

            return newSlice;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static IntPtr GetUnsafePtr<T>(this NativeSlice<T> nativeSlice) where T : struct
        {
            return nativeSlice.m_Buffer;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static IntPtr GetUnsafeReadOnlyPtr<T>(this NativeSlice<T> nativeSlice) where T : struct
        {
            return nativeSlice.m_Buffer;
        }
    }
}
