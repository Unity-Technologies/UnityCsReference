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
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeArrayDebugView < >))]
    public struct NativeArray<T> : IDisposable, IEnumerable<T> where T : struct
    {
        IntPtr                                           m_Buffer;
        int                                              m_Length;
        int                                              m_Stride;


        readonly Allocator                               m_AllocatorLabel;

        public NativeArray(int length, Allocator allocMode)
        {
            Allocate(length, allocMode, out this);
        }

        public NativeArray(T[] array, Allocator allocMode)
        {
            if (array == null) throw new ArgumentNullException("array");
            Allocate(array.Length, allocMode, out this);
            FromArray(array);
        }

        internal NativeArray(IntPtr dataPointer, int length) : this(dataPointer, length, Allocator.None)
        {
        }

        internal NativeArray(IntPtr dataPointer, int length, int stride, AtomicSafetyHandle safety, Allocator allocMode)
        {
            // Internal method used typically by other systems to provide a view on them
            // The caller is still the owner of the data
            // This is the only place we provide a stride argument
            m_Buffer = dataPointer;
            m_Length = length;
            m_Stride = stride;
            m_AllocatorLabel = allocMode;

        }

        public int Length { get { return m_Length; } }

        public unsafe T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);

                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index * m_Stride);
            }

            set
            {
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);

                UnsafeUtility.WriteArrayElement(m_Buffer, index * m_Stride, value);
            }
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
            m_Buffer = IntPtr.Zero;
            m_Length = 0;
        }

        public IntPtr GetUnsafeReadBufferPtr()
        {
            return m_Buffer;
        }

        public IntPtr GetUnsafeWriteBufferPtr()
        {
            return m_Buffer;
        }

        public void FromArray(T[] array)
        {

            if (Length != array.Length)
                throw new ArgumentException("Array length does not match the length of this instance");

            // TODO: optimize with cpblk
            for (var i = 0; i < Length; i++)
                UnsafeUtility.WriteArrayElement(m_Buffer, i * m_Stride, array[i]);
        }

        public T[] ToArray()
        {
            // TODO: optimize with cpblk
            var array = new T[Length];
            for (var i = 0; i < Length; i++)
                array[i] = UnsafeUtility.ReadArrayElement<T>(m_Buffer, i * m_Stride);

            return array;
        }

        private void FailOutOfRangeError(int index)
        {

            throw new IndexOutOfRangeException(string.Format("Index {0} is out of range of '{1}' Length.", index, Length));
        }

        private static void Allocate(int length, Allocator allocMode, out NativeArray<T> outArray)
        {
            // Native allocation is only valid for Temp, Job and Persistent
            if (allocMode <= Allocator.None)
                throw new ArgumentOutOfRangeException("allocMode", "Allocator must be Temp, Job or Persistent");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");

            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length;
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException("length", "Length * sizeof(T) cannot exceed " + int.MaxValue + "bytes");

            outArray = new NativeArray<T>(
                    UnsafeUtility.Malloc((int)totalSize, UnsafeUtility.AlignOf<T>(), allocMode),
                    length,
                    allocMode);
        }

        private NativeArray(IntPtr dataPointer, int length, Allocator allocMode)
        {
            if (dataPointer == IntPtr.Zero)
                throw new ArgumentOutOfRangeException("dataPointer", "Pointer must not be zero");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");

            m_Buffer = dataPointer;
            m_Length = length;
            m_Stride = UnsafeUtility.SizeOf<T>();
            m_AllocatorLabel = allocMode;

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
            private NativeArray<T> array;
            private int index;

            public Enumerator(ref NativeArray<T> array)
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
                    // Let NativeArray indexer checks for out of range
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
    internal sealed class NativeArrayDebugView<T> where T : struct
    {
        private NativeArray<T> array;

        public NativeArrayDebugView(NativeArray<T> array)
        {
            this.array = array;
        }

        public T[] Items
        {
            get { return array.ToArray(); }
        }
    }
}
