// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using static Unity.Collections.AllocatorManager;

namespace Unity.Collections
{
    /// <summary>
    /// An arbitrarily-sized array of bits.
    /// </summary>
    /// <remarks>
    /// The number of allocated bytes is always a multiple of 8. For example, a 65-bit array could fit in 9 bytes, but its allocation is actually 16 bytes.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, IsCreated = {IsCreated}")]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct NativeBitArray
        : INativeDisposable
    {
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeBitArray>();
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeBitArray* m_BitArray;
        internal AllocatorHandle m_Allocator;

        /// <summary>
        /// Initializes and returns an instance of NativeBitArray.
        /// </summary>
        /// <param name="numBits">The number of bits.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public NativeBitArray(int numBits, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            CollectionHelper.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "Unity.Collections.NativeBitArray");
            m_BitArray = UnsafeBitArray.Alloc(allocator);
            m_Allocator = allocator;
            * m_BitArray = new UnsafeBitArray(numBits, allocator, options);
        }

        /// <summary>
        /// Whether this array has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this array has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated => m_BitArray != null && m_BitArray->IsCreated;

        /// <summary>
        /// Whether the container is empty.
        /// </summary>
        /// <value>True if the container is empty or the container has not been constructed.</value>
        public readonly bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="numBits">The new length in bits.</param>
        /// <param name="options">Whether newly allocated data should be zeroed out.</param>
        public void Resize(int numBits, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
            m_BitArray->Resize(numBits, options);
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacityInBits">The new capacity.</param>
        public void SetCapacity(int capacityInBits)
        {
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
            m_BitArray->SetCapacity(capacityInBits);
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
            m_BitArray->TrimExcess();
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            if (!IsCreated)
            {
                return;
            }

            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
            UnsafeBitArray.Free(m_BitArray, m_Allocator);
            m_BitArray = null;
            m_Allocator = Invalid;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this array.
        /// </summary>
        /// <param name="inputDeps">The handle of a job which the new job will depend upon.</param>
        /// <returns>The handle of a new job that will dispose this array. The new job depends upon inputDeps.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            if (!IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new NativeBitArrayDisposeJob { Data = new NativeBitArrayDispose { m_BitArrayData = m_BitArray, m_Allocator = m_Allocator, m_Safety = m_Safety } }.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_BitArray = null;
            m_Allocator = Invalid;

            return jobHandle;

        }

        /// <summary>
        /// Returns the number of bits.
        /// </summary>
        /// <value>The number of bits.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return CollectionHelper.AssumePositive(m_BitArray->Length);
            }
        }

        /// <summary>
        /// Returns the capacity number of bits.
        /// </summary>
        /// <value>The capacity number of bits.</value>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return CollectionHelper.AssumePositive(m_BitArray->Capacity);
            }
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = UnsafeBitArray.MaxCapacity;

        /// <summary>
        /// Sets all the bits to 0.
        /// </summary>
        public void Clear()
        {
            CheckWrite();
            m_BitArray->Clear();
        }

        /// <summary>
        /// Returns a native array that aliases the content of this array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the aliased array.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if the number of bits in this array
        /// is not evenly divisible by the size of T in bits (`sizeof(T) * 8`).</exception>
        /// <returns>A native array that aliases the content of this array.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public NativeArray<T> AsNativeArray<T>() where T : unmanaged
        {
            CheckReadBounds<T>();

            var bitsPerElement = sizeof(T) * 8;
            var length = m_BitArray->Length / bitsPerElement;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_BitArray->Ptr, length, Allocator.None);
            AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
            return array;
        }

        /// <summary>
        /// Sets the bit at an index to 0 or 1.
        /// </summary>
        /// <param name="pos">Index of the bit to set.</param>
        /// <param name="value">True for 1, false for 0.</param>
        public void Set(int pos, bool value)
        {
            CheckWrite();
            m_BitArray->Set(pos, value);
        }

        /// <summary>
        /// Sets a range of bits to 0 or 1.
        /// </summary>
        /// <remarks>
        /// The range runs from index `pos` up to (but not including) `pos + numBits`.
        /// No exception is thrown if `pos + numBits` exceeds the length.
        /// </remarks>
        /// <param name="pos">Index of the first bit to set.</param>
        /// <param name="value">True for 1, false for 0.</param>
        /// <param name="numBits">Number of bits to set.</param>
        /// <exception cref="ArgumentException">Thrown if pos is out of bounds or if numBits is less than 1.</exception>
        public void SetBits(int pos, bool value, int numBits)
        {
            CheckWrite();
            m_BitArray->SetBits(pos, value, numBits);
        }

        /// <summary>
        /// Copies bits of a ulong to bits in this array.
        /// </summary>
        /// <remarks>
        /// The destination bits in this array run from index pos up to (but not including) `pos + numBits`.
        /// No exception is thrown if `pos + numBits` exceeds the length.
        ///
        /// The lowest bit of the ulong is copied to the first destination bit; the second-lowest bit of the ulong is
        /// copied to the second destination bit; and so forth.
        /// </remarks>
        /// <param name="pos">Index of the first bit to set.</param>
        /// <param name="value">Unsigned long from which to copy bits.</param>
        /// <param name="numBits">Number of bits to set (must be between 1 and 64).</param>
        /// <exception cref="ArgumentException">Thrown if pos is out of bounds or if numBits is not between 1 and 64.</exception>
        public void SetBits(int pos, ulong value, int numBits = 1)
        {
            CheckWrite();
            m_BitArray->SetBits(pos, value, numBits);
        }

        /// <summary>
        /// Returns a ulong which has bits copied from this array.
        /// </summary>
        /// <remarks>
        /// The source bits in this array run from index pos up to (but not including) `pos + numBits`.
        /// No exception is thrown if `pos + numBits` exceeds the length.
        ///
        /// The first source bit is copied to the lowest bit of the ulong; the second source bit is copied to the second-lowest bit of the ulong; and so forth. Any remaining bits in the ulong will be 0.
        /// </remarks>
        /// <param name="pos">Index of the first bit to get.</param>
        /// <param name="numBits">Number of bits to get (must be between 1 and 64).</param>
        /// <exception cref="ArgumentException">Thrown if pos is out of bounds or if numBits is not between 1 and 64.</exception>
        /// <returns>A ulong which has bits copied from this array.</returns>
        public ulong GetBits(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray->GetBits(pos, numBits);
        }

        /// <summary>
        /// Returns true if the bit at an index is 1.
        /// </summary>
        /// <param name="pos">Index of the bit to test.</param>
        /// <returns>True if the bit at the index is 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds.</exception>
        public bool IsSet(int pos)
        {
            CheckRead();
            return m_BitArray->IsSet(pos);
        }

        /// <summary>
        /// Copies a range of bits from this array to another range in this array.
        /// </summary>
        /// <remarks>
        /// The bits to copy run from index `srcPos` up to (but not including) `srcPos + numBits`.
        /// The bits to set run from index `dstPos` up to (but not including) `dstPos + numBits`.
        ///
        /// The ranges may overlap, but the result in the overlapping region is undefined.
        /// </remarks>
        /// <param name="dstPos">Index of the first bit to set.</param>
        /// <param name="srcPos">Index of the first bit to copy.</param>
        /// <param name="numBits">Number of bits to copy.</param>
        /// <exception cref="ArgumentException">Thrown if either `dstPos + numBits` or `srcPos + numBits` exceed the length of this array.</exception>
        public void Copy(int dstPos, int srcPos, int numBits)
        {
            CheckWrite();
            m_BitArray->Copy(dstPos, srcPos, numBits);
        }

        /// <summary>
        /// Copies a range of bits from an array to a range of bits in this array.
        /// </summary>
        /// <remarks>
        /// The bits to copy in the source array run from index srcPos up to (but not including) `srcPos + numBits`.
        /// The bits to set in the destination array run from index dstPos up to (but not including) `dstPos + numBits`.
        ///
        /// When the source and destination are the same array, the ranges may still overlap, but the result in the overlapping region is undefined.
        /// </remarks>
        /// <param name="dstPos">Index of the first bit to set.</param>
        /// <param name="srcBitArray">The source array.</param>
        /// <param name="srcPos">Index of the first bit to copy.</param>
        /// <param name="numBits">The number of bits to copy.</param>
        /// <exception cref="ArgumentException">Thrown if either `dstPos + numBits` or `srcBitArray + numBits` exceed the length of this array.</exception>
        public void Copy(int dstPos, ref NativeBitArray srcBitArray, int srcPos, int numBits)
        {
            AtomicSafetyHandle.CheckReadAndThrow(srcBitArray.m_Safety);
            CheckWrite();
            m_BitArray->Copy(dstPos, ref *srcBitArray.m_BitArray, srcPos, numBits);
        }

        /// <summary>
        /// Find a sequence of N contiguous 0 bits in a bit array. To achieve higher performance, it is not guaranteed that the found index is necessarily the first occurrence of such a sequence.
        /// </summary>
        /// <remarks>The search does not guarantee the earliest occurrence in the array.</remarks>
        /// <param name="pos">Index at which to start searching.</param>
        /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
        /// <returns>The index in this array where the sequence is found. The index will be greater than or equal to `pos`.
        /// Returns -1 if no occurrence is found.</returns>
        public int Find(int pos, int numBits)
        {
            CheckRead();
            return m_BitArray->Find(pos, numBits);
        }

        /// <summary>
        /// Find a sequence of N contiguous 0 bits in a bit array. To achieve higher performance, it is not guaranteed that the found index is necessarily the first occurrence of such a sequence.
        /// </summary>
        /// <remarks>The search does not guarantee the earliest occurrence in the array.</remarks>
        /// <param name="pos">Index at which to start searching.</param>
        /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
        /// <param name="count">Number of bits to search.</param>
        /// <returns>The index in this array where the sequence is found. The index will be greater than or equal to `pos` but less than `pos + count`.
        /// Returns -1 if no occurrence is found.</returns>
        public int Find(int pos, int count, int numBits)
        {
            CheckRead();
            return m_BitArray->Find(pos, count, numBits);
        }

        /// <summary>
        /// Returns true if none of the bits in a range are 1 (*i.e.* all bits in the range are 0).
        /// </summary>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
        /// <returns>Returns true if none of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
        public bool TestNone(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray->TestNone(pos, numBits);
        }

        /// <summary>
        /// Returns true if at least one of the bits in a range is 1.
        /// </summary>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
        /// <returns>True if one ore more of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
        public bool TestAny(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray->TestAny(pos, numBits);
        }

        /// <summary>
        /// Returns true if all of the bits in a range are 1.
        /// </summary>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
        /// <returns>True if all of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
        public bool TestAll(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray->TestAll(pos, numBits);
        }

        /// <summary>
        /// Returns the number of bits in a range that are 1.
        /// </summary>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
        /// <returns>The number of bits in a range of bits that are 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
        public int CountBits(int pos, int numBits = 1)
        {
            CheckRead();
            return m_BitArray->CountBits(pos, numBits);
        }

        /// <summary>
        /// Returns a readonly version of this NativeBitArray instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the NativeBitArray it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref this);
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeBitArray. Does not have its own allocated storage.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct ReadOnly
        {
            AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ReadOnly>();
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeBitArray.ReadOnly m_BitArray;

            /// <summary>
            /// Whether this array has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this array has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated => m_BitArray.IsCreated;

            /// <summary>
            /// Whether the container is empty.
            /// </summary>
            /// <value>True if the container is empty or the container has not been constructed.</value>
            public readonly bool IsEmpty => m_BitArray.IsEmpty;

            internal ReadOnly(ref NativeBitArray data)
            {
                m_BitArray = data.m_BitArray->AsReadOnly();
                m_Safety = data.m_Safety;
                CollectionHelper.SetStaticSafetyId<ReadOnly>(ref m_Safety, ref s_staticSafetyId.Data);
            }

            /// <summary>
            /// Returns the number of bits.
            /// </summary>
            /// <value>The number of bits.</value>
            public readonly int Length
            {
                get
                {
                    CheckRead();
                    return CollectionHelper.AssumePositive(m_BitArray.Length);
                }
            }

            /// <summary>
            /// Returns a ulong which has bits copied from this array.
            /// </summary>
            /// <remarks>
            /// The source bits in this array run from index pos up to (but not including) `pos + numBits`.
            /// No exception is thrown if `pos + numBits` exceeds the length.
            ///
            /// The first source bit is copied to the lowest bit of the ulong; the second source bit is copied to the second-lowest bit of the ulong; and so forth. Any remaining bits in the ulong will be 0.
            /// </remarks>
            /// <param name="pos">Index of the first bit to get.</param>
            /// <param name="numBits">Number of bits to get (must be between 1 and 64).</param>
            /// <exception cref="ArgumentException">Thrown if pos is out of bounds or if numBits is not between 1 and 64.</exception>
            /// <returns>A ulong which has bits copied from this array.</returns>
            public readonly ulong GetBits(int pos, int numBits = 1)
            {
                CheckRead();
                return m_BitArray.GetBits(pos, numBits);
            }

            /// <summary>
            /// Returns true if the bit at an index is 1.
            /// </summary>
            /// <param name="pos">Index of the bit to test.</param>
            /// <returns>True if the bit at the index is 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds.</exception>
            public readonly bool IsSet(int pos)
            {
                CheckRead();
                return m_BitArray.IsSet(pos);
            }

            /// <summary>
            /// Finds the first length-*N* contiguous sequence of 0 bits in this bit array.
            /// </summary>
            /// <param name="pos">Index at which to start searching.</param>
            /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
            /// <returns>The index in this array where the sequence is found. The index will be greater than or equal to `pos`.
            /// Returns -1 if no occurrence is found.</returns>
            public readonly int Find(int pos, int numBits)
            {
                CheckRead();
                return m_BitArray.Find(pos, numBits);
            }

            /// <summary>
            /// Finds the first length-*N* contiguous sequence of 0 bits in this bit array. Searches only a subsection.
            /// </summary>
            /// <param name="pos">Index at which to start searching.</param>
            /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
            /// <param name="count">Number of bits to search.</param>
            /// <returns>The index in this array where the sequence is found. The index will be greater than or equal to `pos` but less than `pos + count`.
            /// Returns -1 if no occurrence is found.</returns>
            public readonly int Find(int pos, int count, int numBits)
            {
                CheckRead();
                return m_BitArray.Find(pos, count, numBits);
            }

            /// <summary>
            /// Returns true if none of the bits in a range are 1 (*i.e.* all bits in the range are 0).
            /// </summary>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
            /// <returns>Returns true if none of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
            public readonly bool TestNone(int pos, int numBits = 1)
            {
                CheckRead();
                return m_BitArray.TestNone(pos, numBits);
            }

            /// <summary>
            /// Returns true if at least one of the bits in a range is 1.
            /// </summary>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
            /// <returns>True if one ore more of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
            public readonly bool TestAny(int pos, int numBits = 1)
            {
                CheckRead();
                return m_BitArray.TestAny(pos, numBits);
            }

            /// <summary>
            /// Returns true if all of the bits in a range are 1.
            /// </summary>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
            /// <returns>True if all of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
            public readonly bool TestAll(int pos, int numBits = 1)
            {
                CheckRead();
                return m_BitArray.TestAll(pos, numBits);
            }

            /// <summary>
            /// Returns the number of bits in a range that are 1.
            /// </summary>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
            /// <returns>The number of bits in a range of bits that are 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
            public readonly int CountBits(int pos, int numBits = 1)
            {
                CheckRead();
                return m_BitArray.CountBits(pos, numBits);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            readonly void CheckRead()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckReadBounds<T>() where T : unmanaged
        {
            CheckRead();

            var bitsPerElement = sizeof(T) * 8;
            var length = m_BitArray->Length / bitsPerElement;

            if (length == 0)
            {
                throw new InvalidOperationException($"Number of bits in the NativeBitArray {m_BitArray->Length} is not sufficient to cast to NativeArray<T> {sizeof(T) * 8}.");
            }
            else if (m_BitArray->Length != bitsPerElement* length)
            {
                throw new InvalidOperationException($"Number of bits in the NativeBitArray {m_BitArray->Length} couldn't hold multiple of T {sizeof(T)}. Output array would be truncated.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWrite()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }
    }

    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct NativeBitArrayDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeBitArray* m_BitArrayData;
        public AllocatorHandle m_Allocator;

        public AtomicSafetyHandle m_Safety;

        public void Dispose()
        {
            UnsafeBitArray.Free(m_BitArrayData, m_Allocator);
        }
    }

    [BurstCompile]
    internal unsafe struct NativeBitArrayDisposeJob : IJob
    {
        public NativeBitArrayDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Unsafe helper methods for NativeBitArray.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static class NativeBitArrayUnsafeUtility
    {
        /// <summary>
        /// Returns an array's atomic safety handle.
        /// </summary>
        /// <param name="container">Array from which to get an AtomicSafetyHandle.</param>
        /// <returns>This array's atomic safety handle.</returns>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public static AtomicSafetyHandle GetAtomicSafetyHandle(in NativeBitArray container)
        {
            return container.m_Safety;
        }

        /// <summary>
        /// Sets an array's atomic safety handle.
        /// </summary>
        /// <param name="container">Array which the AtomicSafetyHandle is for.</param>
        /// <param name="safety">Atomic safety handle for this array.</param>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public static void SetAtomicSafetyHandle(ref NativeBitArray container, AtomicSafetyHandle safety)
        {
            container.m_Safety = safety;
        }

        /// <summary>
        /// Returns a bit array with content aliasing a buffer.
        /// </summary>
        /// <param name="ptr">A buffer.</param>
        /// <param name="sizeInBytes">Size of the buffer in bytes. Must be a multiple of 8.</param>
        /// <param name="allocator">The allocator that was used to create the buffer.</param>
        /// <returns>A bit array with content aliasing a buffer.</returns>
        public static unsafe NativeBitArray ConvertExistingDataToNativeBitArray(void* ptr, int sizeInBytes, AllocatorManager.AllocatorHandle allocator)
        {
            var bitArray = UnsafeBitArray.Alloc(Allocator.Persistent);
            *bitArray = new UnsafeBitArray(ptr, sizeInBytes, allocator);

            return new NativeBitArray
            {
                m_BitArray = bitArray,
                m_Allocator = Allocator.Persistent,
            };
        }
    }
}
