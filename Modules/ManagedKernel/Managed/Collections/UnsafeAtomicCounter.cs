// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using Unity.Mathematics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// A 32-bit atomic counter.
    /// </summary>
    /// <remarks>Rather than have its own int, a counter *points* to an int. This arrangement lets counters in different jobs share reference to the same underlying int.</remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeAtomicCounter32
    {
        /// <summary>
        /// The int that is modified by this counter.
        /// </summary>
        /// <value>The int that is modified by this counter.</value>
        public int* Counter;

        /// <summary>
        /// Initializes and returns an instance of UnsafeAtomicCounter32.
        /// </summary>
        /// <param name="ptr">A pointer to the int to be modified by this counter.</param>
        public UnsafeAtomicCounter32(void* ptr)
        {
            Counter = (int*)ptr;
        }

        /// <summary>
        /// Non-atomically sets this counter to a value.
        /// </summary>
        /// <param name="value">The value to set. Defaults to 0</param>
        public void Reset(int value = 0)
        {
            *Counter = value;
        }

        /// <summary>
        /// Atomically adds a value to this counter.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The original value before the add.</returns>
        public int Add(int value)
        {
            return Interlocked.Add(ref UnsafeUtility.AsRef<int>(Counter), value) - value;
        }

        /// <summary>
        /// Atomically subtracts a value from this counter.
        /// </summary>
        /// <param name="value">The value to subtract.</param>
        /// <returns>The original value before the subtract.</returns>
        public int Sub(int value) => Add(-value);

        /// <summary>
        /// Atomically adds a value to this counter. The result will not be greater than a maximum value.
        /// </summary>
        /// <param name="value">The value to add to this counter.</param>
        /// <param name="max">The maximum which the result will not be greater than.</param>
        /// <returns>The original value before the add.</returns>
        public int AddSat(int value, int max = int.MaxValue)
        {
            int oldVal;
            int newVal = *Counter;
            do
            {
                oldVal = newVal;
                newVal = newVal >= max ? max : math.min(max, newVal + value);
                newVal = Interlocked.CompareExchange(ref UnsafeUtility.AsRef<int>(Counter), newVal, oldVal);
            }
            while (oldVal != newVal && oldVal != max);

            return oldVal;
        }

        /// <summary>
        /// Atomically subtracts a value from this counter. The result will not be less than a minimum value.
        /// </summary>
        /// <param name="value">The value to subtract from this counter.</param>
        /// <param name="min">The minimum which the result will not be less than.</param>
        /// <returns>The original value before the subtract.</returns>
        public int SubSat(int value, int min = int.MinValue)
        {
            int oldVal;
            int newVal = *Counter;
            do
            {
                oldVal = newVal;
                newVal = newVal <= min ? min : math.max(min, newVal - value);
                newVal = Interlocked.CompareExchange(ref UnsafeUtility.AsRef<int>(Counter), newVal, oldVal);
            }
            while (oldVal != newVal && oldVal != min);

            return oldVal;
        }
    }

    /// <summary>
    /// A 64-bit atomic counter.
    /// </summary>
    /// <remarks>Rather than have its own long, a counter *points* to a long. This arrangement lets counters in different jobs share reference to the same underlying long.</remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeAtomicCounter64
    {
        /// <summary>
        /// The long that is modified by this counter.
        /// </summary>
        /// <value>The long that is modified by this counter.</value>
        public long* Counter;

        /// <summary>
        /// Initializes and returns an instance of UnsafeAtomicCounter64.
        /// </summary>
        /// <param name="ptr">A pointer to the long to be modified by this counter.</param>
        public UnsafeAtomicCounter64(void* ptr)
        {
            Counter = (long*)ptr;
        }

        /// <summary>
        /// Non-atomically sets this counter to a value.
        /// </summary>
        /// <param name="value">The value to set. Defaults to 0</param>
        public void Reset(long value = 0)
        {
            *Counter = value;
        }

        /// <summary>
        /// Atomically adds a value to this counter.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The original value before the add.</returns>
        public long Add(long value)
        {
            return Interlocked.Add(ref UnsafeUtility.AsRef<long>(Counter), value) - value;
        }

        /// <summary>
        /// Atomically subtracts a value from this counter.
        /// </summary>
        /// <param name="value">The value to subtract.</param>
        /// <returns>The original value before the subtract.</returns>
        public long Sub(long value) => Add(-value);

        /// <summary>
        /// Atomically adds a value to this counter. The result will not be greater than a maximum value.
        /// </summary>
        /// <param name="value">The value to add to this counter.</param>
        /// <param name="max">The maximum which the result will not be greater than.</param>
        /// <returns>The original value before the add.</returns>
        public long AddSat(long value, long max = long.MaxValue)
        {
            long oldVal;
            long newVal = *Counter;
            do
            {
                oldVal = newVal;
                newVal = newVal >= max ? max : math.min(max, newVal + value);
                newVal = Interlocked.CompareExchange(ref UnsafeUtility.AsRef<long>(Counter), newVal, oldVal);
            }
            while (oldVal != newVal && oldVal != max);

            return oldVal;
        }

        /// <summary>
        /// Atomically subtracts a value from this counter. The result will not be less than a minimum value.
        /// </summary>
        /// <param name="value">The value to subtract from this counter.</param>
        /// <param name="min">The minimum which the result will not be less than.</param>
        /// <returns>The original value before the subtract.</returns>
        public long SubSat(long value, long min = long.MinValue)
        {
            long oldVal;
            long newVal = *Counter;
            do
            {
                oldVal = newVal;
                newVal = newVal <= min ? min : math.max(min, newVal - value);
                newVal = Interlocked.CompareExchange(ref UnsafeUtility.AsRef<long>(Counter), newVal, oldVal);
            }
            while (oldVal != newVal && oldVal != min);

            return oldVal;
        }
    }
}
