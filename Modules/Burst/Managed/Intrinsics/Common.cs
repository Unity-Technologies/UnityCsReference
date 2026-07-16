// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace Unity.Burst.Intrinsics
{
    /// <summary>
    /// Common intrinsics that are exposed across all Burst targets.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Hint that the current thread should pause.
        ///
        /// In Burst compiled code this will map to platform specific
        /// ways to hint that the current thread should be paused as
        /// it is performing a calculation that would benefit from
        /// not contending with other threads. Atomic operations in
        /// tight loops (like spin-locks) can benefit from use of this
        /// intrinsic.
        ///
        /// - On x86 systems this maps to the `pause` instruction.
        /// - On ARM systems this maps to the `yield` instruction.
        ///
        /// Note that this is not an operating system level thread yield,
        /// it only provides a hint to the CPU that the current thread can
        /// afford to pause its execution temporarily.
        /// </summary>
        public static void Pause() { }

        /// <summary>
        /// Specifies whether a prefetch operation is for reading or writing.
        /// </summary>
        public enum ReadWrite : int
        {
            /// <summary>Prefetch for reading.</summary>
            Read = 0,
            /// <summary>Prefetch for writing.</summary>
            Write = 1,
        }

        /// <summary>
        /// Specifies the expected temporal locality of a prefetch operation.
        /// </summary>
        public enum Locality : int
        {
            /// <summary>Data has no temporal locality (used once then evicted).</summary>
            NoTemporalLocality = 0,
            /// <summary>Data has low temporal locality.</summary>
            LowTemporalLocality = 1,
            /// <summary>Data has moderate temporal locality.</summary>
            ModerateTemporalLocality = 2,
            /// <summary>Data has high temporal locality (kept in all cache levels).</summary>
            HighTemporalLocality = 3,
        }

        /// <summary>
        /// Prefetch a pointer.
        /// </summary>
        /// <param name="v">The pointer to prefetch.</param>
        /// <param name="rw">Whether the pointer will be used for reading or writing.</param>
        /// <param name="locality">The cache locality of the pointer.</param>
        public static unsafe void Prefetch(void* v, ReadWrite rw, Locality locality = Locality.HighTemporalLocality) { }

        /// <summary>
        /// Return the low half of the multiplication of two numbers, and the high part as an out parameter.
        /// </summary>
        /// <param name="x">A value to multiply.</param>
        /// <param name="y">A value to multiply.</param>
        /// <param name="high">The high-half of the multiplication result.</param>
        /// <returns>The low-half of the multiplication result.</returns>
        public static ulong umul128(ulong x, ulong y, out ulong high)
        {
            // Provide a software fallback for the cases Burst isn't being used.

            // Split the inputs into high/low sections.
            ulong xLo = (uint)x;
            ulong xHi = x >> 32;
            ulong yLo = (uint)y;
            ulong yHi = y >> 32;

            // We have to use 4 multiples to compute the full range of the result.
            ulong hi = xHi * yHi;
            ulong m1 = xHi * yLo;
            ulong m2 = yHi * xLo;
            ulong lo = xLo * yLo;

            ulong m1Lo = (uint)m1;
            ulong loHi = lo >> 32;
            ulong m1Hi = m1 >> 32;

            high = hi + m1Hi + ((loHi + m1Lo + m2) >> 32);
            return x * y;
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static int InterlockedAnd(ref int location, int value)
        {
            // Provide a software fallback for the cases Burst isn't being used.

            var currentValue = System.Threading.Interlocked.Add(ref location, 0);

            while (true)
            {
                var updatedValue = currentValue & value;

                // If nothing would change by and'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static uint InterlockedAnd(ref uint location, uint value)
        {
            unsafe
            {
                ref int locationAsInt = ref Unsafe.AsRef<int>(Unsafe.AsPointer(ref location));
                int valueAsInt = (int)value;

                return (uint)InterlockedAnd(ref locationAsInt, valueAsInt);
            }
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static long InterlockedAnd(ref long location, long value)
        {
            // Provide a software fallback for the cases Burst isn't being used.

            var currentValue = System.Threading.Interlocked.Read(ref location);

            while (true)
            {
                var updatedValue = currentValue & value;

                // If nothing would change by and'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
        }

        /// <summary>
        /// Bitwise and as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically and the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.and"/>
        public static ulong InterlockedAnd(ref ulong location, ulong value)
        {
            unsafe
            {
                ref long locationAsInt = ref Unsafe.AsRef<long>(Unsafe.AsPointer(ref location));
                long valueAsInt = (long)value;

                return (ulong)InterlockedAnd(ref locationAsInt, valueAsInt);
            }
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static int InterlockedOr(ref int location, int value)
        {
            // Provide a software fallback for the cases Burst isn't being used.

            var currentValue = System.Threading.Interlocked.Add(ref location, 0);

            while (true)
            {
                var updatedValue = currentValue | value;

                // If nothing would change by or'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static uint InterlockedOr(ref uint location, uint value)
        {
            unsafe
            {
                ref int locationAsInt = ref Unsafe.AsRef<int>(Unsafe.AsPointer(ref location));
                int valueAsInt = (int)value;

                return (uint)InterlockedOr(ref locationAsInt, valueAsInt);
            }
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static long InterlockedOr(ref long location, long value)
        {
            // Provide a software fallback for the cases Burst isn't being used.

            var currentValue = System.Threading.Interlocked.Read(ref location);

            while (true)
            {
                var updatedValue = currentValue | value;

                // If nothing would change by or'ing in our thing, bail out early.
                if (updatedValue == currentValue)
                {
                    return currentValue;
                }

                var newValue = System.Threading.Interlocked.CompareExchange(ref location, updatedValue, currentValue);

                // If the original value was the same as the what we just got back from the compare exchange, it means our update succeeded.
                if (newValue == currentValue)
                {
                    return currentValue;
                }

                // Lastly update the last known good value of location and try again!
                currentValue = newValue;
            }
        }

        /// <summary>
        /// Bitwise or as an atomic operation.
        /// </summary>
        /// <param name="location">Where to atomically or the result into.</param>
        /// <param name="value">The value to be combined.</param>
        /// <returns>The original value in <paramref name="location" />.</returns>
        /// <remarks>Using the return value of this intrinsic may result in worse code-generation on some platforms (a compare-exchange loop), rather than a single atomic instruction being generated.</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.or"/>
        public static ulong InterlockedOr(ref ulong location, ulong value)
        {
            unsafe
            {
                ref long locationAsInt = ref Unsafe.AsRef<long>(Unsafe.AsPointer(ref location));
                long valueAsInt = (long)value;

                return (ulong)InterlockedOr(ref locationAsInt, valueAsInt);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [Unity.Scripting.RequiredByAssembly]
    // expose the type to btests via Unity.Burst.dll
    internal
    sealed class BurstTargetCpuAttribute : Attribute
    {
        public BurstTargetCpuAttribute(BurstTargetCpu TargetCpu)
        {
            this.TargetCpu = TargetCpu;
        }

        public readonly BurstTargetCpu TargetCpu;
    }
}
