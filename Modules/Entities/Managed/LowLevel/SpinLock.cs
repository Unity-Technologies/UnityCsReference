// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;

namespace Unity.Entities.LowLevel
{
    [GenerateTestsForBurstCompatibility]
    internal struct SpinLock
    {
        int m_Lock;

        /// <summary>
        /// Continually spin until the lock can be acquired.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acquire()
        {
            for (;;)
            {
                // Optimistically assume the lock is free on the first try.
                if (Interlocked.CompareExchange(ref m_Lock, 1, 0) == 0)
                    return;

                // Wait for lock to be released without generating cache misses.
                while (Volatile.Read(ref m_Lock) == 1)
                    continue;

                // Future improvement: the 'continue' instruction above could be swapped for a 'pause' intrinsic
                // instruction when the CPU supports it, to further reduce contention by reducing load-store unit
                // utilization. However, this would need to be optional because if you don't use hyper-threading
                // and you don't care about power efficiency, using the 'pause' instruction will slow down lock
                // acquisition in the contended scenario.
            }
        }

        /// <summary>
        /// Try to acquire the lock and immediately return without spinning.
        /// </summary>
        /// <returns><see langword="true"/> if the lock was acquired, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire()
        {
            // First do a memory load (read) to check if lock is free in order to prevent unnecessary cache misses.
            return Volatile.Read(ref m_Lock) == 0 &&
                Interlocked.CompareExchange(ref m_Lock, 1, 0) == 0;
        }

        /// <summary>
        /// Try to acquire the lock, and spin only if <paramref name="spin"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="spin">Set to true to spin the lock.</param>
        /// <returns><see langword="true"/> if the lock was acquired, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquire(bool spin)
        {
            if (spin)
            {
                Acquire();
                return true;
            }
            return TryAcquire();
        }

        /// <summary>
        /// Release the lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            Volatile.Write(ref m_Lock, 0);
        }
    }
}
