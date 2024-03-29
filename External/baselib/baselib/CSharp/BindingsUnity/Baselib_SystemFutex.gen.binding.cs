//
// File autogenerated from Include/C/Baselib_SystemFutex.h
//

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using size_t = System.UIntPtr;

namespace Unity.Baselib.LowLevel
{
    [NativeHeader("baselib/CSharp/BindingsUnity/Baselib_SystemFutex.gen.binding.h")]
    internal static unsafe partial class Binding
    {
        /// <summary>Determines if the platform has access to a kernel level futex api</summary>
        /// <remarks>
        /// If native support is not present the futex will fallback to an emulated futex setup.
        ///
        /// Notes on the emulation:
        /// * It uses a single synchronization primitive to multiplex all potential addresses. This means there will be
        /// additional contention as well as spurious wakeups compared to a native implementation.
        /// * While the fallback implementation is not something that should be used in production it can still provide value
        /// when bringing up new platforms or to test features built on top of the futex api.
        /// </remarks>
        [FreeFunction(IsThreadSafe = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Baselib_SystemFutex_NativeSupport();
        /// <summary>Wait for notification.</summary>
        /// <remarks>
        /// Address will be checked atomically against expected before entering wait. This can be used to guarantee there are no lost wakeups.
        /// Note: When notified the thread always wake up regardless if the expectation match the value at address or not.
        ///
        /// | Problem this solves
        /// | Thread 1: checks condition and determine we should enter wait
        /// | Thread 2: change condition and notify waiting threads
        /// | Thread 1: enters waiting state
        /// |
        /// | With a futex the two Thread 1 operations become a single op.
        ///
        /// Spurious Wakeup - This function is subject to spurious wakeups.
        /// </remarks>
        /// <param name="address">Any address that can be read from both user and kernel space.</param>
        /// <param name="expected">What address points to will be checked against this value. If the values don't match thread will not enter a waiting state.</param>
        /// <param name="timeoutInMilliseconds">A timeout indicating to the kernel when to wake the thread. Regardless of being notified or not.</param>
        [FreeFunction(IsThreadSafe = true)]
        public static extern void Baselib_SystemFutex_Wait(IntPtr address, Int32 expected, UInt32 timeoutInMilliseconds);
        /// <summary>Notify threads waiting on a specific address.</summary>
        /// <param name="address">Any address that can be read from both user and kernel space</param>
        /// <param name="count">Number of waiting threads to wakeup.</param>
        /// <param name="wakeupFallbackStrategy">Platforms that don't support waking up a specific number of threads will use this strategy.</param>
        [FreeFunction(IsThreadSafe = true)]
        public static extern void Baselib_SystemFutex_Notify(IntPtr address, UInt32 count, Baselib_WakeupFallbackStrategy wakeupFallbackStrategy);
    }
}
