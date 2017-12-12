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
    [System.Flags]
    public enum NativeArrayOptions
    {
        None            = 0,
        ClearMemory     = 1 << 0
    }

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


        internal Allocator                m_AllocatorLabel;

        public NativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(m_Buffer, (ulong)Length * (ulong)UnsafeUtility.SizeOf<T>());
        }

        public NativeArray(T[] array, Allocator allocator)
        {

            Allocate(array.Length, allocator, out this);
            CopyFrom(array);
        }

        public NativeArray(NativeArray<T> array, Allocator allocator)
        {
            Allocate(array.Length, allocator, out this);
            CopyFrom(array);
        }

        private static void Allocate(int length, Allocator allocator, out NativeArray<T> array)
        {
            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length;


            array.m_Buffer = UnsafeUtility.Malloc((ulong)totalSize, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

        }

        public int Length { get { return m_Length; } }

        public unsafe T this[int index]
        {
            get
            {
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }

            set
            {

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

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyFrom(T[] array)
        {

            for (var i = 0; i < Length; i++)
                UnsafeUtility.WriteArrayElement(m_Buffer, i, array[i]);
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyFrom(NativeArray<T> array)
        {
            array.CopyTo(this);
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyTo(T[] array)
        {

            for (var i = 0; i < Length; i++)
                array[i] = UnsafeUtility.ReadArrayElement<T>(m_Buffer, i);
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public void CopyTo(NativeArray<T> array)
        {

            UnsafeUtility.MemCpy(array.m_Buffer, m_Buffer, (ulong)Length * (ulong)UnsafeUtility.SizeOf<T>());
        }

        [MethodImpl((MethodImplOptions) 256)] // AggressiveInlining
        public T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }


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
            private NativeArray<T> m_Array;
            private int m_Index;

            public Enumerator(ref NativeArray<T> array)
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
                    // Let NativeArray indexer check for out of range.
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
    internal sealed class NativeArrayDebugView<T> where T : struct
    {
        private NativeArray<T> m_Array;

        public NativeArrayDebugView(NativeArray<T> array)
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
    public static class NativeArrayUnsafeUtility
    {

        public static NativeArray<T> ConvertExistingDataToNativeArray<T>(IntPtr dataPointer, int length, Allocator allocator) where T : struct
        {
            var newArray = new NativeArray<T>
            {
                // Internal method used typically by other systems to provide a view on them.
                // The caller is still the owner of the data.
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,

            };

            return newArray;
        }

        public static IntPtr GetUnsafePtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }

        public static IntPtr GetUnsafeReadOnlyPtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }

        public static IntPtr GetUnsafeBufferPointerWithoutChecks<T>(this NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }
    }
}
