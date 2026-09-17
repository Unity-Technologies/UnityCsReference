// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

#pragma warning disable 0649

namespace Unity.Collections
{
    internal struct Long8
    {
        internal long f0,f1,f2,f3,f4,f5,f6,f7;
    }

    internal struct Long64
    {
        internal Long8 f0,f1,f2,f3,f4,f5,f6,f7;
    }

    internal struct Long512
    {
        internal Long64 f0,f1,f2,f3,f4,f5,f6,f7;
    }

    internal struct Long1024 : IIndexable<long>
    {
        internal Long512 f0,f1;
        public int Length { get { return 1024;} set {} }
        public ref long ElementAt(int index)
        {
            unsafe { fixed(Long512* p = &f0) {
                return ref UnsafeUtility.AsRef<long>((long*)p + index);
            } }
        }
    }

    internal class ConcurrentMask
    {
        internal static long AtomicOr(ref long destination, long source)
        {
            var readValue = Interlocked.Read(ref destination);
            long oldReadValue, writtenValue;
            do
            {
                writtenValue = readValue | source;
                oldReadValue = readValue;
                readValue = Interlocked.CompareExchange(ref destination, writtenValue, oldReadValue);
            } while(readValue != oldReadValue);
            return writtenValue;
        }

        internal static long AtomicAnd(ref long destination, long source)
        {
            var readValue = Interlocked.Read(ref destination);
            long oldReadValue, writtenValue;
            do
            {
                writtenValue = readValue & source;
                oldReadValue = readValue;
                readValue = Interlocked.CompareExchange(ref destination, writtenValue, oldReadValue);
            } while(readValue != oldReadValue);
            return writtenValue;
        }

        internal static void longestConsecutiveOnes(long value, out int offset, out int count)
        {
            count = 0;
            var newvalue = value;
            while(newvalue != 0)
            {
                value = newvalue;
                newvalue = value & (long)((ulong)value >> 1);
                ++count;
            }
            offset = math.tzcnt(value);
        }

        internal static bool foundAtLeastThisManyConsecutiveOnes(long value, int minimum, out int offset, out int count)
        {
            if(minimum == 1)
            {
                offset = math.tzcnt(value); // find offset of first 1 bit
                count = 1;
                return offset != 64;
            }
            longestConsecutiveOnes(value, out offset, out count);
            return count >= minimum;
        }

        internal static bool foundAtLeastThisManyConsecutiveZeroes(long value, int minimum, out int offset, out int count)
        {
            return foundAtLeastThisManyConsecutiveOnes(~value, minimum, out offset, out count);
        }

        internal const int ErrorFailedToFree = -1;
        internal const int ErrorFailedToAllocate = -2;
        internal const int ErrorAllocationCrossesWordBoundary = -3;
        internal const int EmptyBeforeAllocation = 0;
        internal const int EmptyAfterFree = 0;

        internal static bool Succeeded(int error)
        {
            return error >= 0;
        }

        internal static long MakeMask(int offset, int bits)
        {
            return (long)(~0UL >> (64-bits)) << offset;
        }

        internal static int TryAllocate(ref long l, int offset, int bits)
        {
            var mask = MakeMask(offset, bits);
            var readValue = Interlocked.Read(ref l);
            long oldReadValue, writtenValue;
            do
            {
                if((readValue & mask) != 0)
                    return ErrorFailedToAllocate;
                writtenValue = readValue | mask;
                oldReadValue = readValue;
                readValue = Interlocked.CompareExchange(ref l, writtenValue, oldReadValue);
            } while(readValue != oldReadValue);
            return math.countbits(readValue); // how many bits were set, before i allocated? sometimes if 0, do something special (allocate chunk?)
        }

        internal static int TryFree(ref long l, int offset, int bits)
        {
            var mask = MakeMask(offset, bits);
            var readValue = Interlocked.Read(ref l);
            long oldReadValue, writtenValue;
            do
            {
                if((readValue & mask) != mask)
                    return ErrorFailedToFree;
                writtenValue = readValue & ~mask;
                oldReadValue = readValue;
                readValue = Interlocked.CompareExchange(ref l, writtenValue, oldReadValue);
            } while(readValue != oldReadValue);
            return math.countbits(writtenValue); // how many bits are set, after i freed? sometimes if 0, do something special (free chunk?)
        }

        internal static int TryAllocate(ref long l, out int offset, int bits)
        {
            var readValue = Interlocked.Read(ref l);
            long oldReadValue, writtenValue;
            do
            {
                if(!foundAtLeastThisManyConsecutiveZeroes(readValue, bits, out offset, out int _))
                    return ErrorFailedToAllocate;
                var mask = MakeMask(offset, bits);
                writtenValue = readValue | mask;
                oldReadValue = readValue;
                readValue = Interlocked.CompareExchange(ref l, writtenValue, oldReadValue);
            } while(readValue != oldReadValue);
            return math.countbits(readValue); // how many bits were set, before i allocated? sometimes if 0, do something special (allocate chunk?)
        }

        internal static int TryAllocate<T>(ref T t, int offset, int bits) where T : IIndexable<long>
        {
            var wordOffset = offset >> 6;
            var bitOffset = offset & 63;
            if(bitOffset + bits > 64)
                return ErrorAllocationCrossesWordBoundary;
            return TryAllocate(ref t.ElementAt(wordOffset), bitOffset, bits);
        }

        internal static int TryFree<T>(ref T t, int offset, int bits) where T : IIndexable<long>
        {
            var wordOffset = offset >> 6;
            var bitOffset = offset & 63;
            return TryFree(ref t.ElementAt(wordOffset), bitOffset, bits);
        }

        internal static int TryAllocate<T>(ref T t, out int offset, int begin, int end, int bits) where T : IIndexable<long>
        {
            var wordOffset = begin;
            for(; wordOffset < end; ++wordOffset)
                if(t.ElementAt(wordOffset) != ~0L)
                    break;
            for(; wordOffset < end; ++wordOffset)
            {
                int error, bitOffset;
                error = TryAllocate(ref t.ElementAt(wordOffset), out bitOffset, bits);
                if(Succeeded(error))
                {
                    offset = wordOffset * 64 + bitOffset;
                    return error;
                }
            }
            offset = -1;
            return ErrorFailedToAllocate;
        }

        internal static int TryAllocate<T>(ref T t, out int offset, int begin, int bits) where T : IIndexable<long>
        {
            var error = TryAllocate(ref t, out offset, begin, t.Length, bits);
            if(Succeeded(error))
                return error;
            return TryAllocate(ref t, out offset, 0, begin, bits);
        }

        internal static int TryAllocate<T>(ref T t, out int offset, int bits) where T : IIndexable<long>
        {
            return TryAllocate(ref t, out offset, 0, t.Length, bits);
        }

    }
}
