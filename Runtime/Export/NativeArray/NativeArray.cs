// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

namespace Unity.Collections
{
    public enum NativeArrayOptions
    {
        UninitializedMemory            = 0,
        ClearMemory                    = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainerSupportsDeferredConvertListToArray]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeArrayDebugView < >))]
    public unsafe struct NativeArray<T> : IDisposable, IEnumerable<T>, IEquatable<NativeArray<T>> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal void*                    m_Buffer;
        internal int                      m_Length;


        internal Allocator                m_AllocatorLabel;

        public NativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(m_Buffer, (long)Length * UnsafeUtility.SizeOf<T>());
        }

        public NativeArray(T[] array, Allocator allocator)
        {

            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        public NativeArray(NativeArray<T> array, Allocator allocator)
        {
            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        static void Allocate(int length, Allocator allocator, out NativeArray<T> array)
        {
            var totalSize = UnsafeUtility.SizeOf<T>() * (long)length;


            array.m_Buffer = UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

        }

        public int Length => m_Length;

        [BurstDiscard]
        internal static void IsBlittableAndThrow()
        {
            if (!UnsafeUtility.IsBlittable<T>())
            {
                throw new InvalidOperationException(
                    $"{typeof(T)} used in NativeArray<{typeof(T)}> must be blittable.\n{UnsafeUtility.GetReasonForValueTypeNonBlittable<T>()}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementReadAccess(int index)
        {
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementWriteAccess(int index)
        {
        }

        public T this[int index]
        {
            get
            {
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }

            [WriteAccessRequired]
            set
            {
                CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
            }
        }

        public bool IsCreated => m_Buffer != null;

        [WriteAccessRequired]
        public void Dispose()
        {

            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
            m_Length = 0;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array)
        {
            Copy(array, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(NativeArray<T> array)
        {
            Copy(array, this);
        }

        public void CopyTo(T[] array)
        {
            Copy(this, array);
        }

        public void CopyTo(NativeArray<T> array)
        {
            Copy(this, array);
        }

        public T[] ToArray()
        {
            var array = new T[Length];
            Copy(this, array, Length);
            return array;
        }


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
            NativeArray<T> m_Array;
            int m_Index;

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

            // Let NativeArray indexer check for out of range.
            public T Current => m_Array[m_Index];

            object IEnumerator.Current => Current;
        }

        public bool Equals(NativeArray<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Length == other.m_Length;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NativeArray<T> && Equals((NativeArray<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_Buffer * 397) ^ m_Length;
            }
        }

        public static bool operator==(NativeArray<T> left, NativeArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(NativeArray<T> left, NativeArray<T> right)
        {
            return !left.Equals(right);
        }

        public static void Copy(NativeArray<T> src, NativeArray<T> dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, NativeArray<T> dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(NativeArray<T> src, T[] dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(NativeArray<T> src, NativeArray<T> dst, int length)
        {
            Copy(src, 0, dst, 0, length);
        }

        public static void Copy(T[] src, NativeArray<T> dst, int length)
        {
            Copy(src, 0, dst, 0, length);
        }

        public static void Copy(NativeArray<T> src, T[] dst, int length)
        {
            Copy(src, 0, dst, 0, length);
        }

        public static void Copy(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            UnsafeUtility.MemCpy(
                (byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());
        }

        public static void Copy(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(
                (byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)addr + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        public static void Copy(NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(
                (byte*)addr + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }
    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="NativeArray{T}"/>
    /// </summary>
    internal sealed class NativeArrayDebugView<T> where T : struct
    {
        NativeArray<T> m_Array;

        public NativeArrayDebugView(NativeArray<T> array)
        {
            m_Array = array;
        }

        public T[] Items => m_Array.ToArray();
    }
}
namespace Unity.Collections.LowLevel.Unsafe
{
    public static class NativeArrayUnsafeUtility
    {

        /// Internal method used typically by other systems to provide a view on them.
        /// The caller is still the owner of the data.
        public static unsafe NativeArray<T> ConvertExistingDataToNativeArray<T>(void* dataPointer, int length, Allocator allocator) where T : struct
        {

            var newArray = new NativeArray<T>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,

            };

            return newArray;
        }

        public static unsafe void* GetUnsafePtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeBufferPointerWithoutChecks<T>(NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }
    }
}
