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
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeArrayDebugView < >))]
    public struct NativeArray<T> : IDisposable, IEnumerable<T> where T : struct
    {
        internal IntPtr                   m_Buffer;
        internal int                      m_Length;


        Allocator                         m_AllocatorLabel;

        public NativeArray(int length, Allocator allocMode)
        {
            Allocate(length, allocMode, out this);
            UnsafeUtility.MemClear(m_Buffer, Length * UnsafeUtility.SizeOf<T>());
        }

        public NativeArray(T[] array, Allocator allocMode)
        {
            if (array == null) throw new ArgumentNullException("array");
            Allocate(array.Length, allocMode, out this);
            CopyFrom(array);
        }

        public NativeArray(NativeArray<T> array, Allocator allocMode)
        {
            Allocate(array.Length, allocMode, out this);
            CopyFrom(array);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static NativeArray<T> ConvertExistingDataToNativeArrayInternal(IntPtr dataPointer, int length, AtomicSafetyHandle safety, Allocator allocMode)
        {
            NativeArray<T> newArray = new NativeArray<T>();

            // Internal method used typically by other systems to provide a view on them
            // The caller is still the owner of the data
            newArray.m_Buffer = dataPointer;
            newArray.m_Length = length;
            newArray.m_AllocatorLabel = allocMode;


            return newArray;
        }

        private static void Allocate(int length, Allocator allocator, out NativeArray<T> array)
        {
            // Native allocation is only valid for Temp, Job and Persistent
            if (allocator <= Allocator.None)
                throw new ArgumentOutOfRangeException("allocMode", "Allocator must be Temp, Job or Persistent");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");

            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length;
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException("length", "Length * sizeof(T) cannot exceed " + int.MaxValue + "bytes");

            array.m_Buffer = UnsafeUtility.Malloc((int)totalSize, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

        }

        public int Length { get { return m_Length; } }

        public unsafe T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);

                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }

            set
            {
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);

                UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
            }
        }

        public bool IsCreated
        {
            get { return m_Buffer != IntPtr.Zero; }
        }

        public void Dispose()
        {

            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
            m_Buffer = IntPtr.Zero;
            m_Length = 0;
        }

        public IntPtr UnsafePtr
        {
            get
            {
                return m_Buffer;
            }
        }

        public IntPtr UnsafeReadOnlyPtr
        {
            get
            {
                return m_Buffer;
            }
        }

        internal void GetUnsafeBufferPointerWithoutChecksInternal(out AtomicSafetyHandle handle, out IntPtr ptr)
        {
            ptr = m_Buffer;
            handle = new AtomicSafetyHandle();
        }

        public void CopyFrom(T[] array)
        {

            if (Length != array.Length)
                throw new ArgumentException("Array length does not match the length of this instance");

            for (var i = 0; i < Length; i++)
                UnsafeUtility.WriteArrayElement(m_Buffer, i, array[i]);
        }

        public void CopyFrom(NativeArray<T> array)
        {
            array.CopyTo(this);
        }

        public void CopyTo(T[] array)
        {

            if (Length != array.Length)
                throw new ArgumentException("Array length does not match the length of this instance");

            for (var i = 0; i < Length; i++)
                array[i] = UnsafeUtility.ReadArrayElement<T>(m_Buffer, i);
        }

        public void CopyTo(NativeArray<T> array)
        {

            if (Length != array.Length)
                throw new ArgumentException("Array length does not match the length of this instance");

            UnsafeUtility.MemCpy(array.m_Buffer, m_Buffer, Length * UnsafeUtility.SizeOf<T>());
        }

        public T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }

        private void FailOutOfRangeError(int index)
        {

            throw new IndexOutOfRangeException(string.Format("Index {0} is out of range of '{1}' Length.", index, Length));
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
