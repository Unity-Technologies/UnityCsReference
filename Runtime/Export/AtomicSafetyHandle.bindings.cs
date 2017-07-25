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
        Dispose         = 1 << 2,
        ReadAndWrite = Read | Write,
        ReadWriteAndDispose = Read | Write | Dispose,

        WriteInv = ~Write,
        ReadInv         = ~Read,
        ReadAndWriteInv = ~ReadAndWrite,
        ReadWriteAndDisposeInv = ~ReadWriteAndDispose
    }

    // AtomicSafetyHandle is used by the C# job system to provide validation and full safety
    // For read / write permissions to access the buffers represented by each handle.
    // Each AtomicSafetyHandle represents a single container.
    // Since all Native containers are written using structs,
    // it also provides checks against destroying a container
    // and accessing from another struct pointing to the same buffer.

    [UsedByNativeCode]
    [NativeType(Header = "Runtime/Jobs/AtomicSafetyHandle.h")]
    internal struct AtomicSafetyHandle
    {
        internal IntPtr                         versionNode;
        internal AtomicSafetyHandleVersionMask  version;

        //@TODO: Ensure AtomicSafetyHandle.Create / release is actually threadsafe

        // Creates a new AtomicSafetyHandle that is valid until Release is called.
        [UnityEngine.Bindings.ThreadSafe]
        public static extern AtomicSafetyHandle Create();

        // Releases a previously Created AtomicSafetyHandle.
        // You must call CheckDeallocateAndThrow before calling Release
        [UnityEngine.Bindings.ThreadSafe]
        public static extern void Release(AtomicSafetyHandle handle);

        // Marks the AtomicSafetyHandle to be undisposable.
        // CheckDeallocateAndThrow will throw an exception on this handle afterwards.
        // (Note: this mutates the value type, not the referenced node)
        [UnityEngine.Bindings.ThreadSafe]
        public static extern void PrepareUndisposable(ref AtomicSafetyHandle handle);

        // Switches the AtomicSafetyHandle to the secondary version number.
        [UnityEngine.Bindings.ThreadSafe]
        public static extern void UseSecondaryVersion(ref AtomicSafetyHandle handle);

        // Performs CheckWriteAndThrow and then bumps the secondary version.
        // This allows for example a NativeArray that becomes invalid if the Length of a List
        // is changed to be invalidated, while the NativeList handle itself remains valid.
        [UnityEngine.Bindings.ThreadSafe]
        public static extern void CheckWriteAndBumpSecondaryVersion(AtomicSafetyHandle handle);


        // Waits for all jobs running against this AtomicSafetyHandle to complete,
        // then releases the atomic safetyhandle.
        // e.g. You are forced to delete some memory right now,
        // irregardless of any potential jobs that are still running
        // (It is up to you to still give an error message)
        // This is unusual behaviour, normally we would simply throw an exception
        // and thus simply not perform the Disposing.
        // But sometimes you just need to delete memory right away, for example Mesh.EndWriteVertices()
        [UnityEngine.Bindings.ThreadSafe]
        public static extern void EnforceAllBufferJobsHaveCompletedAndRelease(AtomicSafetyHandle handle);

        // Same as CheckReadAndThrow but the early out has already been performed in the call site for perf reasons
        [UnityEngine.Bindings.ThreadSafe]
        internal static extern void CheckReadAndThrowNoEarlyOut(AtomicSafetyHandle handle);

        // Same as CheckWriteAndThrow but the early out has already been performed in the call site for perf reasons
        [UnityEngine.Bindings.ThreadSafe]
        internal static extern void CheckWriteAndThrowNoEarlyOut(AtomicSafetyHandle handle);

        // Checks if the handle can be deallocated.
        // If not (already destroyed, job currently accessing the data) throws an exception.
        [UnityEngine.Bindings.ThreadSafe]
        public static extern void CheckDeallocateAndThrow(AtomicSafetyHandle handle);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void CheckGetSecondaryDataPointerAndThrow(AtomicSafetyHandle handle);

        // Checks if the handle can be read from
        // If not (already destroyed, job currently writing to the data) throws an exception.
        public static unsafe void CheckReadAndThrow(AtomicSafetyHandle handle)
        {
            var versionPtr = (AtomicSafetyHandleVersionMask*)handle.versionNode;
            if ((handle.version & AtomicSafetyHandleVersionMask.Read) == 0 && handle.version != ((*versionPtr) & AtomicSafetyHandleVersionMask.WriteInv))
                CheckReadAndThrowNoEarlyOut(handle);
        }

        // Checks if the handle can be written to
        // If not (already destroyed, job currently reading or writing to the data) throws an exception.
        public static unsafe void CheckWriteAndThrow(AtomicSafetyHandle handle)
        {
            var versionPtr = (AtomicSafetyHandleVersionMask*)handle.versionNode;
            if ((handle.version & AtomicSafetyHandleVersionMask.Write) == 0 && handle.version != ((*versionPtr) & AtomicSafetyHandleVersionMask.ReadInv))
                CheckWriteAndThrowNoEarlyOut(handle);
        }

        // Checks if the handle is still valid.
        // If not (already destroyed) throws an exception.
        public static unsafe void CheckExistsAndThrow(AtomicSafetyHandle handle)
        {
            AtomicSafetyHandleVersionMask* versionPtr = (AtomicSafetyHandleVersionMask*)handle.versionNode;
            if ((handle.version & AtomicSafetyHandleVersionMask.ReadWriteAndDisposeInv) != ((*versionPtr) & AtomicSafetyHandleVersionMask.ReadWriteAndDisposeInv))
                throw new System.InvalidOperationException("The NativeArray has been deallocated, it is not allowed to access it");
        }
    }
}
