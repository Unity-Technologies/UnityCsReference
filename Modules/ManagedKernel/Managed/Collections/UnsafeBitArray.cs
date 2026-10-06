// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.InteropServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An arbitrarily-sized array of bits.
    /// </summary>
    /// <remarks>
    /// The number of allocated bytes is always a multiple of 8. For example, a 65-bit array could fit in 9 bytes, but its allocation is actually 16 bytes.
    /// </remarks>
    [DebuggerDisplay("Length = {Length}, IsCreated = {IsCreated}")]
    [DebuggerTypeProxy(typeof(UnsafeBitArrayDebugView))]
    [GenerateTestsForBurstCompatibility]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeBitArray
        : INativeDisposable
    {
        /// <summary>
        /// Pointer to the data.
        /// </summary>
        /// <value>Pointer to the data.</value>
        [NativeDisableUnsafePtrRestriction]
        public ulong* Ptr;

        /// <summary>
        /// The number of bits.
        /// </summary>
        /// <value>The number of bits.</value>
        public int Length;

        /// <summary>
        /// The capacity number of bits.
        /// </summary>
        /// <value>The capacity number of bits.</value>
        public int Capacity;

        /// <summary>
        /// The allocator to use.
        /// </summary>
        /// <value>The allocator to use.</value>
        public AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        /// Initializes and returns an instance of UnsafeBitArray which aliases an existing buffer.
        /// </summary>
        /// <param name="ptr">An existing buffer.</param>
        /// <param name="allocator">The allocator that was used to allocate the bytes. Needed to dispose this array.</param>
        /// <param name="sizeInBytes">The number of bytes. The length will be `sizeInBytes * 8`.</param>
        public unsafe UnsafeBitArray(void* ptr, int sizeInBytes, AllocatorManager.AllocatorHandle allocator = new AllocatorManager.AllocatorHandle())
        {
            CheckSizeMultipleOf8(sizeInBytes);
            Ptr = (ulong*)ptr;
            Length = sizeInBytes * 8;
            Capacity = sizeInBytes * 8;
            Allocator = allocator;
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeBitArray.
        /// </summary>
        /// <param name="numBits">Number of bits.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public UnsafeBitArray(int numBits, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            CollectionHelper.CheckAllocator(allocator);
            Allocator = allocator;

            Ptr = null;
            Length = 0;
            Capacity = 0;

            Resize(numBits, options);
        }

        internal static UnsafeBitArray* Alloc(AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeBitArray* data = (UnsafeBitArray*)Memory.Unmanaged.Allocate(sizeof(UnsafeBitArray), UnsafeUtility.AlignOf<UnsafeBitArray>(), allocator);
            return data;
        }

        internal static void Free(UnsafeBitArray* data, AllocatorManager.AllocatorHandle allocator)
        {
            if (data == null)
            {
                throw new InvalidOperationException("UnsafeBitArray has yet to be created or has been destroyed!");
            }
            data->Dispose();
            Memory.Unmanaged.Free(data, allocator);
        }

        /// <summary>
        /// Whether this array has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this array has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated => Ptr != null;

        /// <summary>
        /// Whether the container is empty.
        /// </summary>
        /// <value>True if the container is empty or the container has not been constructed.</value>
        public readonly bool IsEmpty => !IsCreated || Length == 0;

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = int.MaxValue - 63;

        void Realloc(int capacityInBits)
        {
            int newCapacity = (int)math.min(Bitwise.AlignUp((long)capacityInBits, 64), int.MaxValue-63);
            int sizeInBytes = newCapacity / 8;

            ulong* newPointer = null;

            if (sizeInBytes > 0)
            {
                newPointer = (ulong*)Memory.Unmanaged.Allocate(sizeInBytes, 16, Allocator);

                if (Capacity > 0)
                {
                    var itemsToCopy = math.min(newCapacity, Capacity);
                    var bytesToCopy = itemsToCopy / 8;
                    UnsafeUtility.MemCpy(newPointer, Ptr, bytesToCopy);
                }
            }

            Memory.Unmanaged.Free(Ptr, Allocator);

            Ptr = newPointer;
            Capacity = newCapacity;
            Length = math.min(Length, newCapacity);
        }

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="numBits">The new length in bits.</param>
        /// <param name="options">Whether newly allocated data should be zeroed out.</param>
        public void Resize(int numBits, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            CollectionHelper.CheckAllocator(Allocator);

            var minCapacity = math.max(numBits, 1);

            if (minCapacity > Capacity)
            {
                SetCapacity(minCapacity);
            }

            var oldLength = Length;
            Length = numBits;

            if (options == NativeArrayOptions.ClearMemory && oldLength < Length)
            {
                SetBits(oldLength, false, Length - oldLength);
            }
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacityInBits">The new capacity.</param>
        public void SetCapacity(int capacityInBits)
        {
            CollectionHelper.CheckCapacityInRange(capacityInBits, int.MaxValue-63, Length);

            if (Capacity == capacityInBits)
            {
                return;
            }

            Realloc(capacityInBits);
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            SetCapacity(Length);
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                Memory.Unmanaged.Free(Ptr, Allocator);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            Length = 0;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this array.
        /// </summary>
        /// <param name="inputDeps">The handle of a job which the new job will depend upon.</param>
        /// <returns>The handle of a new job that will dispose this array. The new job depends upon inputDeps.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return inputDeps;
            }

            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                var jobHandle = new UnsafeDisposeJob { Ptr = Ptr, Allocator = Allocator }.Schedule(inputDeps);

                Ptr = null;
                Allocator = AllocatorManager.Invalid;

                return jobHandle;
            }

            Ptr = null;

            return inputDeps;
        }

        /// <summary>
        /// Sets all the bits to 0.
        /// </summary>
        public void Clear()
        {
            var sizeInBytes = Bitwise.AlignUp(Length, 64) / 8;
            UnsafeUtility.MemClear(Ptr, sizeInBytes);
        }

        /// <summary>
        /// Sets the bit at an index to 0 or 1.
        /// </summary>
        /// <param name="ptr">pointer to the bit buffer</param>
        /// <param name="pos">Index of the bit to set.</param>
        /// <param name="value">True for 1, false for 0.</param>
        public static void Set(ulong* ptr, int pos, bool value)
        {
            var idx = pos >> 6;
            var shift = pos & 0x3f;
            var mask = 1ul << shift;
            var bits = (ptr[idx] & ~mask) | ((ulong)-Bitwise.FromBool(value) & mask);
            ptr[idx] = bits;
        }

        /// <summary>
        /// Sets the bit at an index to 0 or 1.
        /// </summary>
        /// <param name="pos">Index of the bit to set.</param>
        /// <param name="value">True for 1, false for 0.</param>
        public void Set(int pos, bool value)
        {
            CheckArgs(pos, 1);
            Set(Ptr, pos, value);
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
            CheckArgs(pos, numBits);

            var end = math.min(pos + numBits, Length);
            var idxB = pos >> 6;
            var shiftB = pos & 0x3f;
            var idxE = (end - 1) >> 6;
            var shiftE = end & 0x3f;
            var maskB = 0xfffffffffffffffful << shiftB;
            var maskE = 0xfffffffffffffffful >> (64 - shiftE);
            var orBits = (ulong)-Bitwise.FromBool(value);
            var orBitsB = maskB & orBits;
            var orBitsE = maskE & orBits;
            var cmaskB = ~maskB;
            var cmaskE = ~maskE;

            if (idxB == idxE)
            {
                var maskBE = maskB & maskE;
                var cmaskBE = ~maskBE;
                var orBitsBE = orBitsB & orBitsE;
                Ptr[idxB] = (Ptr[idxB] & cmaskBE) | orBitsBE;
                return;
            }

            Ptr[idxB] = (Ptr[idxB] & cmaskB) | orBitsB;

            for (var idx = idxB + 1; idx < idxE; ++idx)
            {
                Ptr[idx] = orBits;
            }

            Ptr[idxE] = (Ptr[idxE] & cmaskE) | orBitsE;
        }

        /// <summary>
        /// Copies bits of a ulong to bits in this array.
        /// </summary>
        /// <remarks>
        /// The destination bits in this array run from index `pos` up to (but not including) `pos + numBits`.
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
            CheckArgsUlong(pos, numBits);

            var idxB = pos >> 6;
            var shiftB = pos & 0x3f;

            if (shiftB + numBits <= 64)
            {
                var mask = 0xfffffffffffffffful >> (64 - numBits);
                Ptr[idxB] = Bitwise.ReplaceBits(Ptr[idxB], shiftB, mask, value);

                return;
            }

            var end = math.min(pos + numBits, Length);
            var idxE = (end - 1) >> 6;
            var shiftE = end & 0x3f;

            var maskB = 0xfffffffffffffffful >> shiftB;
            Ptr[idxB] = Bitwise.ReplaceBits(Ptr[idxB], shiftB, maskB, value);

            var valueE = value >> (64 - shiftB);
            var maskE = 0xfffffffffffffffful >> (64 - shiftE);
            Ptr[idxE] = Bitwise.ReplaceBits(Ptr[idxE], 0, maskE, valueE);
        }

        /// <summary>
        /// Returns a ulong which has bits copied from this array.
        /// </summary>
        /// <remarks>
        /// The source bits in this array run from index `pos` up to (but not including) `pos + numBits`.
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
            CheckArgsUlong(pos, numBits);
            return Bitwise.GetBits(Ptr, Length, pos, numBits);
        }

        /// <summary>
        /// Returns true if the bit at an index is 1.
        /// </summary>
        /// <param name="pos">Index of the bit to test.</param>
        /// <returns>True if the bit at the index is 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds.</exception>
        public bool IsSet(int pos)
        {
            CheckArgs(pos, 1);
            return Bitwise.IsSet(Ptr, pos);
        }

        internal void CopyUlong(int dstPos, ref UnsafeBitArray srcBitArray, int srcPos, int numBits) => SetBits(dstPos, srcBitArray.GetBits(srcPos, numBits), numBits);

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
            if (dstPos == srcPos)
            {
                return;
            }

            Copy(dstPos, ref this, srcPos, numBits);
        }

        /// <summary>
        /// Copies a range of bits from an array to a range of bits in this array.
        /// </summary>
        /// <remarks>
        /// The bits to copy in the source array run from index srcPos up to (but not including) `srcPos + numBits`.
        /// The bits to set in the destination array run from index dstPos up to (but not including) `dstPos + numBits`.
        ///
        /// It's fine if source and destination array are one and the same, even if the ranges overlap, but the result in the overlapping region is undefined.
        /// </remarks>
        /// <param name="dstPos">Index of the first bit to set.</param>
        /// <param name="srcBitArray">The source array.</param>
        /// <param name="srcPos">Index of the first bit to copy.</param>
        /// <param name="numBits">The number of bits to copy.</param>
        /// <exception cref="ArgumentException">Thrown if either `dstPos + numBits` or `srcBitArray + numBits` exceed the length of this array.</exception>
        public void Copy(int dstPos, ref UnsafeBitArray srcBitArray, int srcPos, int numBits)
        {
            if (numBits == 0)
            {
                return;
            }

            CheckArgsCopy(ref this, dstPos, ref srcBitArray, srcPos, numBits);

            if (numBits <= 64) // 1x CopyUlong
            {
                CopyUlong(dstPos, ref srcBitArray, srcPos, numBits);
            }
            else if (numBits <= 128) // 2x CopyUlong
            {
                CopyUlong(dstPos, ref srcBitArray, srcPos, 64);
                numBits -= 64;

                if (numBits > 0)
                {
                    CopyUlong(dstPos + 64, ref srcBitArray, srcPos + 64, numBits);
                }
            }
            else if ((dstPos & 7) == (srcPos & 7)) // aligned copy
            {
                var dstPosInBytes = CollectionHelper.Align(dstPos, 8) >> 3;
                var srcPosInBytes = CollectionHelper.Align(srcPos, 8) >> 3;
                var numPreBits = dstPosInBytes * 8 - dstPos;

                if (numPreBits > 0)
                {
                    CopyUlong(dstPos, ref srcBitArray, srcPos, numPreBits);
                }

                var numBitsLeft = numBits - numPreBits;
                var numBytes = numBitsLeft / 8;

                if (numBytes > 0)
                {
                    unsafe
                    {
                        UnsafeUtility.MemMove((byte*)Ptr + dstPosInBytes, (byte*)srcBitArray.Ptr + srcPosInBytes, numBytes);
                    }
                }

                var numPostBits = numBitsLeft & 7;

                if (numPostBits > 0)
                {
                    CopyUlong((dstPosInBytes + numBytes) * 8, ref srcBitArray, (srcPosInBytes + numBytes) * 8, numPostBits);
                }
            }
            else // unaligned copy
            {
                var dstPosAligned = CollectionHelper.Align(dstPos, 64);
                var numPreBits = dstPosAligned - dstPos;

                if (numPreBits > 0)
                {
                    CopyUlong(dstPos, ref srcBitArray, srcPos, numPreBits);
                    numBits -= numPreBits;
                    dstPos += numPreBits;
                    srcPos += numPreBits;
                }

                for (; numBits >= 64; numBits -= 64, dstPos += 64, srcPos += 64)
                {
                    Ptr[dstPos >> 6] = srcBitArray.GetBits(srcPos, 64);
                }

                if (numBits > 0)
                {
                    CopyUlong(dstPos, ref srcBitArray, srcPos, numBits);
                }
            }
        }

        /// <summary>
        /// Find a sequence of N contiguous 0 bits in a bit array. To achieve higher performance, it is not guaranteed that the found index is necessarily the first occurrence of such a sequence.
        /// </summary>
        /// <remarks>The search does not guarantee the earliest occurrence in the array.</remarks>
        /// <remarks>The search is linear.</remarks>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
        /// <returns>The index of the first occurrence in this array of `numBits` contiguous 0 bits. Range is pos up to (but not including) the length of this array. Returns int.MaxValue if no occurrence is found.</returns>
        public int Find(int pos, int numBits)
        {
            var count = Length - pos;
            CheckArgsPosCount(pos, count, numBits);
            return Bitwise.Find(Ptr, pos, count, numBits);
        }

        /// <summary>
        /// Find a sequence of N contiguous 0 bits in a bit array. To achieve higher performance, it is not guaranteed that the found index is necessarily the first occurrence of such a sequence.
        /// </summary>
        /// <remarks>The search does not guarantee the earliest occurrence in the array.</remarks>
        /// <remarks>The search is linear.</remarks>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
        /// <param name="count">Number of indexes to consider as the return value.</param>
        /// <returns>The index of the first occurrence in this array of `numBits` contiguous 0 bits. Range is pos up to (but not including) `pos + count`. Returns int.MaxValue if no occurrence is found.</returns>
        public int Find(int pos, int count, int numBits)
        {
            CheckArgsPosCount(pos, count, numBits);
            return Bitwise.Find(Ptr, pos, count, numBits);
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
            CheckArgs(pos, numBits);
            return Bitwise.TestNone(Ptr, Length, pos, numBits);
        }

        /// <summary>
        /// Returns true if at least one of the bits in a range is 1.
        /// </summary>
        /// <param name="pos">Index of the bit at which to start searching.</param>
        /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
        /// <returns>True if one or more of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
        /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
        public bool TestAny(int pos, int numBits = 1)
        {
            CheckArgs(pos, numBits);
            return Bitwise.TestAny(Ptr, Length, pos, numBits);
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
            CheckArgs(pos, numBits);
            return Bitwise.TestAll(Ptr, Length, pos, numBits);
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
            CheckArgs(pos, numBits);
            return Bitwise.CountBits(Ptr, Length, pos, numBits);
        }

        /// <summary>
        /// Returns a readonly version of this UnsafeBitArray instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the UnsafeBitArray it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(Ptr, Length);
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeBitArray. Does not have its own allocated storage.
        /// </summary>
        public struct ReadOnly
        {
            /// <summary>
            /// Pointer to the data.
            /// </summary>
            /// <value>Pointer to the data.</value>
            [NativeDisableUnsafePtrRestriction]
            public readonly ulong* Ptr;

            /// <summary>
            /// The number of bits.
            /// </summary>
            /// <value>The number of bits.</value>
            public readonly int Length;

            /// <summary>
            /// Whether this array has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this array has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated => Ptr != null;

            /// <summary>
            /// Whether the container is empty.
            /// </summary>
            /// <value>True if the container is empty or the container has not been constructed.</value>
            public readonly bool IsEmpty => !IsCreated || Length == 0;

            internal ReadOnly(ulong* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            /// Returns a ulong which has bits copied from this array.
            /// </summary>
            /// <remarks>
            /// The source bits in this array run from index `pos` up to (but not including) `pos + numBits`.
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
                CheckArgsUlong(pos, numBits);
                return Bitwise.GetBits(Ptr, Length, pos, numBits);
            }

            /// <summary>
            /// Returns true if the bit at an index is 1.
            /// </summary>
            /// <param name="pos">Index of the bit to test.</param>
            /// <returns>True if the bit at the index is 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds.</exception>
            public readonly bool IsSet(int pos)
            {
                CheckArgs(pos, 1);
                return Bitwise.IsSet(Ptr, pos);
            }

            /// <summary>
            /// Returns the index of the first occurrence in this array of *N* contiguous 0 bits.
            /// </summary>
            /// <remarks>The search is linear.</remarks>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
            /// <returns>The index of the first occurrence in this array of `numBits` contiguous 0 bits. Range is pos up to (but not including) the length of this array. Returns int.MaxValue if no occurrence is found.</returns>
            public readonly int Find(int pos, int numBits)
            {
                var count = Length - pos;
                CheckArgsPosCount(pos, count, numBits);
                return Bitwise.Find(Ptr, pos, count, numBits);
            }

            /// <summary>
            /// Returns the index of the first occurrence in this array of a given number of contiguous 0 bits.
            /// </summary>
            /// <remarks>The search is linear.</remarks>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of contiguous 0 bits to look for.</param>
            /// <param name="count">Number of indexes to consider as the return value.</param>
            /// <returns>The index of the first occurrence in this array of `numBits` contiguous 0 bits. Range is pos up to (but not including) `pos + count`. Returns int.MaxValue if no occurrence is found.</returns>
            public readonly int Find(int pos, int count, int numBits)
            {
                CheckArgsPosCount(pos, count, numBits);
                return Bitwise.Find(Ptr, pos, count, numBits);
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
                CheckArgs(pos, numBits);
                return Bitwise.TestNone(Ptr, Length, pos, numBits);
            }

            /// <summary>
            /// Returns true if at least one of the bits in a range is 1.
            /// </summary>
            /// <param name="pos">Index of the bit at which to start searching.</param>
            /// <param name="numBits">Number of bits to test. Defaults to 1.</param>
            /// <returns>True if one or more of the bits in range `pos` up to (but not including) `pos + numBits` are 1.</returns>
            /// <exception cref="ArgumentException">Thrown if `pos` is out of bounds or `numBits` is less than 1.</exception>
            public readonly bool TestAny(int pos, int numBits = 1)
            {
                CheckArgs(pos, numBits);
                return Bitwise.TestAny(Ptr, Length, pos, numBits);
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
                CheckArgs(pos, numBits);
                return Bitwise.TestAll(Ptr, Length, pos, numBits);
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
                CheckArgs(pos, numBits);
                return Bitwise.CountBits(Ptr, Length, pos, numBits);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void CheckArgs(int pos, int numBits)
            {
                if (pos < 0
                    || pos >= Length
                    || numBits < 1)
                {
                    throw new ArgumentException($"BitArray invalid arguments: pos {pos} (must be 0-{Length - 1}), numBits {numBits} (must be greater than 0).");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void CheckArgsPosCount(int begin, int count, int numBits)
            {
                if (begin < 0 || begin >= Length)
                {
                    throw new ArgumentException($"BitArray invalid argument: begin {begin} (must be 0-{Length - 1}).");
                }

                if (count < 0 || count > Length)
                {
                    throw new ArgumentException($"BitArray invalid argument: count {count} (must be 0-{Length}).");
                }

                if (numBits < 1 || count < numBits)
                {
                    throw new ArgumentException($"BitArray invalid argument: numBits {numBits} (must be greater than 0).");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void CheckArgsUlong(int pos, int numBits)
            {
                CheckArgs(pos, numBits);

                if (numBits < 1 || numBits > 64)
                {
                    throw new ArgumentException($"BitArray invalid arguments: numBits {numBits} (must be 1-64).");
                }

                if (pos + numBits > Length)
                {
                    throw new ArgumentException($"BitArray invalid arguments: Out of bounds pos {pos}, numBits {numBits}, Length {Length}.");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckSizeMultipleOf8(int sizeInBytes)
        {
            if ((sizeInBytes & 7) != 0)
            {
                throw new ArgumentException($"BitArray invalid arguments: sizeInBytes {sizeInBytes} (must be multiple of 8-bytes, sizeInBytes: {sizeInBytes}).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckArgs(int pos, int numBits)
        {
            if (pos < 0
                || pos >= Length
                || numBits < 1)
            {
                throw new ArgumentException($"BitArray invalid arguments: pos {pos} (must be 0-{Length - 1}), numBits {numBits} (must be greater than 0).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckArgsPosCount(int begin, int count, int numBits)
        {
            if (begin < 0 || begin >= Length)
            {
                throw new ArgumentException($"BitArray invalid argument: begin {begin} (must be 0-{Length - 1}).");
            }

            if (count < 0 || count > Length)
            {
                throw new ArgumentException($"BitArray invalid argument: count {count} (must be 0-{Length}).");
            }

            if (numBits < 1 || count < numBits)
            {
                throw new ArgumentException($"BitArray invalid argument: numBits {numBits} (must be greater than 0).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckArgsUlong(int pos, int numBits)
        {
            CheckArgs(pos, numBits);

            if (numBits < 1 || numBits > 64)
            {
                throw new ArgumentException($"BitArray invalid arguments: numBits {numBits} (must be 1-64).");
            }

            if (pos + numBits > Length)
            {
                throw new ArgumentException($"BitArray invalid arguments: Out of bounds pos {pos}, numBits {numBits}, Length {Length}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckArgsCopy(ref UnsafeBitArray dstBitArray, int dstPos, ref UnsafeBitArray srcBitArray, int srcPos, int numBits)
        {
            if (srcPos + numBits > srcBitArray.Length)
            {
                throw new ArgumentException($"BitArray invalid arguments: Out of bounds - source position {srcPos}, numBits {numBits}, source bit array Length {srcBitArray.Length}.");
            }

            if (dstPos + numBits > dstBitArray.Length)
            {
                throw new ArgumentException($"BitArray invalid arguments: Out of bounds - destination position {dstPos}, numBits {numBits}, destination bit array Length {dstBitArray.Length}.");
            }
        }
    }

    sealed class UnsafeBitArrayDebugView
    {
        UnsafeBitArray Data;

        public UnsafeBitArrayDebugView(UnsafeBitArray data)
        {
            Data = data;
        }

        public bool[] Bits
        {
            get
            {
                var array = new bool[Data.Length];
                for (int i = 0; i < Data.Length; ++i)
                {
                    array[i] = Data.IsSet(i);
                }
                return array;
            }
        }
    }
}
