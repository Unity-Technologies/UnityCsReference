// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnityEngine.Collections
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
        IntPtr                                           m_Buffer;
        int                                              m_Stride;
        int                                              m_Length;


        public NativeSlice(NativeArray<T> array) : this(array, 0, array.Length) {}
        public NativeSlice(NativeArray<T> array, int start) : this(array, start, array.Length - start) {}


        public unsafe NativeSlice(NativeArray<T> array, int start, int length)
        {
            if (start < 0)
                throw new System.ArgumentException("Slice start range must be >= 0.");
            if (length < 0)
                throw new System.ArgumentException("Slice length must be >= 0.");
            if (start + length > array.Length)
                throw new System.ArgumentException("Slice start + length range must be <= array.Length");

            m_Stride = UnsafeUtility.SizeOf<T>();
            byte* ptr = (byte*)array.m_Buffer + m_Stride * start;
            m_Buffer = (IntPtr)ptr;
            m_Length = length;

        }

        // Keeps stride, changes length
        public NativeSlice<U> SliceConvert<U>() where U : struct
        {
            if (m_Stride != UnsafeUtility.SizeOf<T>())
                throw new System.ArgumentException("SliceConvert requires that stride matches the size of the source type");
            NativeSlice<U> outputSlice;
            outputSlice.m_Buffer = m_Buffer;

            int sizeofU = UnsafeUtility.SizeOf<U>();
            outputSlice.m_Stride = sizeofU;

            bool isMultiple = (m_Stride * m_Length) % sizeofU  == 0;
            if (!isMultiple)
                throw new System.ArgumentException("SliceConvert requires that Length * Stride is a multiple of sizeof(U).");

            outputSlice.m_Length = (m_Length * m_Stride) / sizeofU;

            return outputSlice;
        }

        // Keeps length, changes stride
        public unsafe NativeSlice<U> SliceWithStride<U>(int offset) where U : struct
        {
            if (offset < 0)
                throw new ArgumentException("SliceWithStride offset must be >= 0");
            if (offset + UnsafeUtility.SizeOf<U>() > UnsafeUtility.SizeOf<T>())
                throw new ArgumentException("SliceWithStride sizeof(U) + offset must be <= sizeof(T)");

            NativeSlice<U> outputSlice;
            byte* ptr = (byte*)m_Buffer + offset;
            outputSlice.m_Buffer = (IntPtr)ptr;
            outputSlice.m_Stride = m_Stride;
            outputSlice.m_Length = m_Length;

            return outputSlice;
        }

        public NativeSlice<U> SliceWithStride<U>() where U : struct
        {
            return SliceWithStride<U>(0);
        }

        public NativeSlice<U> SliceWithStride<U>(string offsetFieldName) where U : struct
        {
            int offsetOf = UnsafeUtility.OffsetOf<T>(offsetFieldName);
            //@TODO: check that U matches offsetFieldName type
            return SliceWithStride<U>(offsetOf);
        }

        public unsafe T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);

                return UnsafeUtility.ReadArrayElementWithStride<T>(m_Buffer, index, m_Stride);
            }

            set
            {
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);

                UnsafeUtility.WriteArrayElementWithStride(m_Buffer, index, m_Stride, value);
            }
        }

        private void FailOutOfRangeError(int index)
        {

            throw new IndexOutOfRangeException(string.Format("Index {0} is out of range of '{1}' Length.", index, Length));
        }

        public void CopyFrom(NativeSlice<T> slice)
        {
            if (Length != slice.Length)
                throw new ArgumentException(string.Format("array.Length ({0}) does not match NativeSlice.Length({1}).", slice.Length, Length));

            for (int i = 0; i != m_Length; i++)
                this[i] = slice[i];
        }

        public void CopyFrom(T[] array)
        {
            if (Length != array.Length)
                throw new ArgumentException(string.Format("array.Length ({0}) does not match NativeSlice.Length({1}).", array.Length, Length));

            for (int i = 0; i != m_Length; i++)
                this[i] = array[i];
        }

        public void CopyTo(NativeArray<T> array)
        {
            if (Length != array.Length)
                throw new ArgumentException(string.Format("array.Length ({0}) does not match NativeSlice.Length({1}).", array.Length, Length));

            for (int i = 0; i != m_Length; i++)
                array[i] = this[i];
        }

        public void CopyTo(T[] array)
        {
            if (Length != array.Length)
                throw new ArgumentException(string.Format("array.Length ({0}) does not match NativeSlice.Length({1}).", array.Length, Length));

            for (int i = 0; i != m_Length; i++)
                array[i] = this[i];
        }

        public T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }

        public int      Stride { get { return m_Stride; } }
        public int      Length { get { return m_Length; } }

        public IntPtr   UnsafePtr
        {
            get
            {
                return m_Buffer;
            }
        }

        public IntPtr   UnsafeReadOnlyPtr
        {
            get
            {
                return m_Buffer;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private NativeSlice<T> array;
            private int index;

            public Enumerator(ref NativeSlice<T> array)
            {
                this.array = array;
                this.index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                index++;
                return index < array.Length;
            }

            public void Reset()
            {
                index = -1;
            }

            public T Current
            {
                get
                {
                    // Let NativeSlice indexer checks for out of range
                    return array[index];
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
        private NativeSlice<T> array;

        public NativeSliceDebugView(NativeSlice<T> array)
        {
            this.array = array;
        }

        public T[] Items
        {
            get { return array.ToArray(); }
        }
    }
}

