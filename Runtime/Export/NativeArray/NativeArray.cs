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
    [DebuggerTypeProxy(typeof(NativeArrayDebugView<>))]
    public unsafe struct NativeArray<T> : IDisposable, IEnumerable<T>, IEquatable<NativeArray<T>> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal void*                    m_Buffer;
        internal int                      m_Length;

        internal int                      m_MinIndex;
        internal int                      m_MaxIndex;
        internal AtomicSafetyHandle       m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel          m_DisposeSentinel;

        internal Allocator                m_AllocatorLabel;

        public NativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(m_Buffer, (long)Length * UnsafeUtility.SizeOf<T>());
        }

        public NativeArray(T[] array, Allocator allocator)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

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

            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");

            IsBlittableAndThrow();

            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int.
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(length), $"Length * sizeof(T) cannot exceed {int.MaxValue} bytes");

            array.m_Buffer = UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
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
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            var versionPtr = (AtomicSafetyHandleVersionMask*)m_Safety.versionNode;
            if ((m_Safety.version & AtomicSafetyHandleVersionMask.Read) == 0 && m_Safety.version != ((*versionPtr) & AtomicSafetyHandleVersionMask.WriteInv))
                AtomicSafetyHandle.CheckReadAndThrowNoEarlyOut(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementWriteAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            var versionPtr = (AtomicSafetyHandleVersionMask*)m_Safety.versionNode;
            if ((m_Safety.version & AtomicSafetyHandleVersionMask.Write) == 0 && m_Safety.version != ((*versionPtr) & AtomicSafetyHandleVersionMask.ReadInv))
                AtomicSafetyHandle.CheckWriteAndThrowNoEarlyOut(m_Safety);
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

            if (!UnsafeUtility.IsValidAllocator(m_AllocatorLabel))
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

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

        void FailOutOfRangeError(int index)
        {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                    "You can use double buffering strategies to avoid race conditions due to " +
                    "reading & writing in parallel to the same elements from a job.");

            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
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
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);

            if (src.Length != dst.Length)
                throw new ArgumentException("source and destination length must be the same");

            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, NativeArray<T> dst)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);

            if (src.Length != dst.Length)
                throw new ArgumentException("source and destination length must be the same");

            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(NativeArray<T> src, T[] dst)
        {
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);

            if (src.Length != dst.Length)
                throw new ArgumentException("source and destination length must be the same");

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
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");

            if (srcIndex < 0 || srcIndex > src.Length || (srcIndex == src.Length && src.Length > 0))
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source NativeArray.");

            if (dstIndex < 0 || dstIndex > dst.Length || (dstIndex == dst.Length && dst.Length > 0))
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination NativeArray.");

            if (srcIndex + length > src.Length)
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source NativeArray.", nameof(length));

            if (dstIndex + length > dst.Length)
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination NativeArray.", nameof(length));

            UnsafeUtility.MemCpy(
                (byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());
        }

        public static void Copy(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);

            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");

            if (srcIndex < 0 || srcIndex > src.Length || (srcIndex == src.Length && src.Length > 0))
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source array.");

            if (dstIndex < 0 || dstIndex > dst.Length || (dstIndex == dst.Length && dst.Length > 0))
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination NativeArray.");

            if (srcIndex + length > src.Length)
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source array.", nameof(length));

            if (dstIndex + length > dst.Length)
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination NativeArray.", nameof(length));

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
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);

            if (dst == null)
                throw new ArgumentNullException(nameof(dst));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");

            if (srcIndex < 0 || srcIndex > src.Length || (srcIndex == src.Length && src.Length > 0))
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source NativeArray.");

            if (dstIndex < 0 || dstIndex > dst.Length || (dstIndex == dst.Length && dst.Length > 0))
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination array.");

            if (srcIndex + length > src.Length)
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source NativeArray.", nameof(length));

            if (dstIndex + length > dst.Length)
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination array.", nameof(length));

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
        public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(NativeArray<T> array) where T : struct
        {
            return array.m_Safety;
        }

        public static void SetAtomicSafetyHandle<T>(ref NativeArray<T> array, AtomicSafetyHandle safety) where T : struct
        {
            array.m_Safety = safety;
        }


        /// Internal method used typically by other systems to provide a view on them.
        /// The caller is still the owner of the data.
        public static unsafe NativeArray<T> ConvertExistingDataToNativeArray<T>(void* dataPointer, int length, Allocator allocator) where T : struct
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");

            NativeArray<T>.IsBlittableAndThrow();

            var totalSize = UnsafeUtility.SizeOf<T>() * (long)length;
            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int.
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(length), $"Length * sizeof(T) cannot exceed {int.MaxValue} bytes");

            var newArray = new NativeArray<T>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,

                m_MinIndex = 0,
                m_MaxIndex = length - 1,
            };

            return newArray;
        }

        public static unsafe void* GetUnsafePtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckWriteAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeBufferPointerWithoutChecks<T>(NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }
    }
}
