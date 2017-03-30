// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [Flags]
    internal enum AtomicSafetyHandleVersionMask
    {
        Read            = 1 << 0,
        Write           = 1 << 1,
        ReadAndWrite    = Read | Write,

        WriteInv        = ~Write,
        ReadInv         = ~Read,
        ReadAndWriteInv = ~ReadAndWrite
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Jobs/AtomicSafetyHandle.h")]
    internal struct AtomicSafetyHandle
    {
        internal IntPtr                         versionNode;
        internal AtomicSafetyHandleVersionMask  version;

        internal static extern AtomicSafetyHandle Create();

        internal static extern void Release(AtomicSafetyHandle handle);

        [ThreadSafe]
        internal static extern void PrepareUndisposable(ref AtomicSafetyHandle handle);

        [ThreadSafe]
        internal static extern void UseSecondaryVersion(ref AtomicSafetyHandle handle);

        [ThreadSafe]
        internal static extern void BumpSecondaryVersion(ref AtomicSafetyHandle handle);


        [ThreadSafe]
        internal static extern void EnforceAllBufferJobsHaveCompletedAndRelease(AtomicSafetyHandle handle);

        [ThreadSafe]
        internal static extern void CheckReadAndThrowNoEarlyOut(AtomicSafetyHandle handle);

        [ThreadSafe]
        internal static extern void CheckWriteAndThrowNoEarlyOut(AtomicSafetyHandle handle);

        [ThreadSafe]
        internal static extern void CheckDeallocateAndThrow(AtomicSafetyHandle handle);

        internal static unsafe void CheckReadAndThrow(AtomicSafetyHandle handle)
        {
            var versionPtr = (AtomicSafetyHandleVersionMask*)handle.versionNode;
            if ((handle.version & AtomicSafetyHandleVersionMask.Read) == 0 && handle.version != ((*versionPtr) & AtomicSafetyHandleVersionMask.ReadInv))
                CheckReadAndThrowNoEarlyOut(handle);
        }

        internal static unsafe void CheckWriteAndThrow(AtomicSafetyHandle handle)
        {
            var versionPtr = (AtomicSafetyHandleVersionMask*)handle.versionNode;
            if ((handle.version & AtomicSafetyHandleVersionMask.Write) == 0 && handle.version != ((*versionPtr) & AtomicSafetyHandleVersionMask.WriteInv))
                CheckWriteAndThrowNoEarlyOut(handle);
        }
    }
}
