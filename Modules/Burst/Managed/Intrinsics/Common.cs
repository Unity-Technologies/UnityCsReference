// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

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
