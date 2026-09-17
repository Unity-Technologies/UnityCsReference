// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.Diagnostics;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Jobs;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    ///<summary>Enumerates the possible values returned by <see cref="AtomicSafetyHandle" /> methods that wait for all jobs accessing the native container associated with the handle to finish.</summary>
    ///<remarks>The members of this enum describe whether the job system had to wait for any jobs to finish.
    ///
    ///The following methods use this enum as a return type: 
    ///
    ///* <see cref="AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted" />
    ///* <see cref="AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndDisableReadWrite" />
    ///* <see cref="AtomicSafetyHandle.EnforceAllBufferJobsHaveCompletedAndRelease" />.</remarks>
    public enum EnforceJobResult
    {
        ///<summary>Indicates that all jobs were already complete at the time of the wait request.</summary>
        AllJobsAlreadySynced = 0,
        ///<summary>Indicates that the job system waited for at least one job to finish.</summary>
        DidSyncRunningJobs = 1,
        ///<summary>Indicates that the job system didn't wait because the AtomicSafetyHandle was invalid, likely because the associated container had already been deallocated.</summary>
        HandleWasAlreadyDeallocated = 2,
    }

    ///<summary>Error code for errors related to accessing native container instances in different situations.</summary>
    ///<remarks>Each [NativeContainer](xref:JobSystemNativeContainer) instance is monitored by an <c>AtomicSafetyHandle</c>, which coordinates safe access 
    ///to the container. If the container can't be accessed safely, then the <c>AtomicSafetyHandle</c> generates an error. The values of this enumeration classify the errors
    ///that are generated in each type of access situation.
    ///
    ///You can set custom error messages for each of these error types on a per-type basis. Use <c>SetCustomErrorMessage</c> for each value in this enumeration.</remarks>
    ///<seealso cref="AtomicSafetyHandle.SetCustomErrorMessage" />
    public enum AtomicSafetyErrorType
    {
        ///<summary>Error caused by main thread attempting to access the native container after the container memory is deallocated.</summary>
        Deallocated = 0,         // access on main thread after deallocation
        ///<summary>Error caused by worker thread attempting to access the native container after the container memory is deallocated</summary>
        DeallocatedFromJob = 1,  // access from job after deallocation
        ///<summary>Error caused by worker thread attempting to access the native container before the container memory is allocated.</summary>
        NotAllocatedFromJob = 2, // Access from job prior to assignment
    }

    // AtomicSafetyHandle is used by the C# job system to provide validation and full safety
    // for read / write permissions to access the buffers represented by each handle.
    // Each AtomicSafetyHandle represents a single container.
    // Since all Native containers are written using structs,
    // it also provides checks against destroying a container
    // and accessing from another struct pointing to the same buffer.

    ///<summary>Coordinate safe access to native container memory inside the job system.</summary>
    ///<remarks>
    ///  <c>AtomicSafetyHandle</c> holds a reference to the central information that the safety system stores for a given native container. When a job contains a <c>NativeContainer</c> instance, the job 
    ///system automatically configures the flags in <c>AtomicSafetyHandle</c> to reflect the way that the native container can be used in that job. Each job has a separate <c>AtomicSafetyHandle</c> instance for a given native 
    ///container. 
    ///
    ///Use this class when you implement a custom <c>NativeContainer</c> type. Every <c>NativeContainer</c> instance must contain an <c>AtomicSafetyHandle</c> field named <c>m_Safety</c>.
    /// 
    ///For a conceptual overview of AtomicSafetyHandle and the role it plays in the job system, refer to [Implement a custom native container](xref:job-system-custom-nativecontainer).</remarks>
    [UsedByNativeCode]
    [NativeHeader("ManagedKernel/Jobs/AtomicSafetyHandle.h")]
    [NativeHeader("ManagedKernel/Jobs/JobsDebugger.h")]
    public struct AtomicSafetyHandle
    {
        // These bit flag constants need to be in sync with
        // the bit flag enums in AtomicSafetyHandle.h
        internal const int Read = 1 << 0;
        internal const int Write = 1 << 1;
        internal const int Dispose = 1 << 2;
        internal const int TempVersion = 1 << 3;
        internal const int VersionIncrement = 1 << 4;

        internal const int ReadCheck = ~(Write | Dispose);
        internal const int WriteCheck = ~(Read | Dispose);
        internal const int DisposeCheck = ~(Read | Write);
        internal const int ReadWriteDisposeCheck = ~(Read | Write | Dispose);

        [NativeDisableUnsafePtrRestriction]
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal IntPtr versionNode;
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal int version;
        internal int staticSafetyId;

        // Creates a new AtomicSafetyHandle that is valid until Release is called.
        ///<summary>Creates a new AtomicSafetyHandle.</summary>
        ///<remarks>If the memory associated with this NativeContainer is allocated using <see cref="Allocator.Temp" />, then use <see cref="AtomicSafetyHandle.GetTempMemoryHandle" /> instead of this method.</remarks>
        ///<returns>The newly created AtomicSafetyHandle.</returns>
        ///<seealso cref="AtomicSafetyHandle.Release" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern AtomicSafetyHandle Create();

        ///<summary>Gets a single shared AtomicSafetyHandle.</summary>
        ///<remarks>For example, a shared AtomicSafetyHandle might be shared with a NativeSlice that points to stack memory. 
        ///                
        ///The shared AtomicSafetyHandle is never disposed, and you can't use it in a job.</remarks>
        ///<returns>A shared AtomicSafetyHandle.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern AtomicSafetyHandle GetTempUnsafePtrSliceHandle();

        ///<summary>Gets an AtomicSafetyHandle for the temporary memory allocations in a temporary memory scope.</summary>
        ///<remarks>The temporary safety handle is invalidated when the temporary memory is freed. This happens when the active temporary memory scope exists, which happens at least once per frame. 
        ///Don't manually call <see cref="Release" /> on this AtomicSafetyHandle.</remarks>
        ///<returns>The AtomicSafetyHandle for temporary memory allocations in the current scope.</returns>
        ///<seealso cref="AtomicSafetyHandle.IsTempMemoryHandle" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern AtomicSafetyHandle GetTempMemoryHandle();
        ///<summary>Checks if an AtomicSafetyHandle is the temporary memory safety handle for the active temporary memory scope.</summary>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>True if the safety handle is the temporary memory handle for the current scope.</returns>
        ///<seealso cref="AtomicSafetyHandle.GetTempMemoryHandle" />
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsTempMemoryHandle(AtomicSafetyHandle handle);

        // Releases a previously Created AtomicSafetyHandle.
        // You must call CheckDeallocateAndThrow before calling Release.
        ///<summary>Releases a previously created AtomicSafetyHandle.</summary>
        ///<remarks>After this call, this AtomicSafetyHandle and copies of it are no longer valid. <see cref="AtomicSafetyHandle.IsHandleValid" /> returns false.
        ///
        ///You must call <see cref="AtomicSafetyHandle.CheckDeallocateAndThrow" /> before calling Release. Don't call this method for the handle returned by 
        ///<see cref="AtomicSafetyHandle.GetTempMemoryHandle" /> or <see cref="AtomicSafetyHandle.GetTempUnsafePtrSliceHandle" />, because the lifetime of those handles is automatically managed by the engine.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to release.</param>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void Release(AtomicSafetyHandle handle);

        ///<summary>Checks if an AtomicSafetyHandle has its default value.</summary>
        ///<remarks>An AtomicSafetyHandle still has its default value if it hasn't been initialized by being assigned or passed to <see cref="AtomicSafetyHandle.Create" />.
        ///                
        ///By default, an AtomicSafetyHandle variable isn't initialized, which means it doesn't reference any central record and doesn't have a stored a version number. 
        ///This changes when you pass the variable to <see cref="AtomicSafetyHandle.Create" />, or when you assign it from an existing initialized AtomicSafetyHandle.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>
        ///  <c>true</c> if the AtomicSafetyHandle hasn't been initialized and still has its default value; <c>false</c> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool IsDefaultValue(in AtomicSafetyHandle handle);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void SetExclusiveWeak(ref AtomicSafetyHandle handle, bool enabled);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool GetExclusiveWeak(in AtomicSafetyHandle handle);

        // Marks the AtomicSafetyHandle so that it cannot be disposed of.
        ///<summary>Marks an AtomicSafetyHandle so that it can't be disposed of.</summary>
        ///<param name="handle">The AtomicSafetyHandle to mark.</param>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void PrepareUndisposable(ref AtomicSafetyHandle handle);

        // Switches the AtomicSafetyHandle to the secondary version number.
        ///<summary>Switches the AtomicSafetyHandle to the secondary version number.</summary>
        ///<remarks>For a conceptual overview of container version numbers, refer to [Copying NativeContainer structures](xref:job-system-copy-nativecontainer).</remarks>
        ///<param name="handle">The AtomicSafetyHandle to switch.</param>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void UseSecondaryVersion(ref AtomicSafetyHandle handle);

        // Switches the AtomicSafetyHandle to the secondary version number.
        ///<summary>Sets whether other AtomicSafetyHandles that use a secondary version number can write to the NativeContainer protected by a given AtomicSafetyHandle.</summary>
        ///<param name="handle">The AtomicSafetyHandle of the NativeContainer.</param>
        ///<param name="allowWriting">If true, allows write access to the NativeContainer for AtomicSafetyHandles that use secondary version number. If false, 
        ///handles that usse a secondary version number only have read access to the NativeContainer.</param>
        ///<seealso cref="AtomicSafetyHandle.UseSecondaryVersion" />
        ///<seealso href="xref:job-system-copy-nativecontainer">Copying NativeContainer structures</seealso>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetAllowSecondaryVersionWriting(AtomicSafetyHandle handle, bool allowWriting);

        ///<summary>Sets whether to automatically bump the secondary version when scheduling a job that has write access to the AtomicSafetyHandle.</summary>
        ///<remarks>For example, NativeList sets this on its AtomicSafetyHandle, which cause any aliased NativeArrays (created using <c>AsArray()</c>) to be 
        ///invalidated whenever a job is scheduled that uses this NativeList without the <c>[ReadOnly]</c> attribute.
        ///
        ///For more information about container version numbers, refer to [Copying NativeContainer structures](xref:job-system-copy-nativecontainer).</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<param name="value">Use <c>true</c> to bump the secondary version number on schedule.</param>
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetBumpSecondaryVersionOnScheduleWrite(AtomicSafetyHandle handle, bool value);

        ///<summary>Sets the read or write access on an AtomicSafetyHandle.</summary>
        ///<remarks>For example, when a buffer references other data that becomes invalid, then typically the only operation that can be performed safely 
        ///is to dispose of the buffer. Calling this method makes sure that any further attempts to read or write the buffer throws an exception.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to set read and write access on.</param>
        ///<param name="allowReadWriteAccess">Use <c>false</c> to disallow read or write access, or <c>true</c> otherwise.</param>
        ///<seealso cref="AtomicSafetyHandle.GetAllowReadOrWriteAccess" />
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern void SetAllowReadOrWriteAccess(AtomicSafetyHandle handle, bool allowReadWriteAccess);

        ///<summary>Checks if the <see cref="AtomicSafetyHandle" /> is configured to allow reading or writing.</summary>
        ///<remarks>Read and write access is allowed by default in any newly created AtomicSafetyHandle.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>True if the <see cref="AtomicSafetyHandle" /> is configured to allow reading or writing, false otherwise.</returns>
        ///<seealso cref="AtomicSafetyHandle.SetAllowReadOrWriteAccess" />
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern bool GetAllowReadOrWriteAccess(AtomicSafetyHandle handle);

        ///<summary>Sets the nested container flag on an AtomicSafetyHandle.</summary>
        ///<remarks>The job system doesn't support nested containers which are containers where the individual elements inside the container are themselves 
        ///containers), because it can't configure the AtomicSafetyHandle instances stored inside the container's elements independently for each job using the container. This 
        ///means that attempting to use a nested container in a job doesn't work correctly.
        ///
        ///To protect against using nested containers in jobs and prevent subtle and hard-to-diagnose errors from arising, you should set the nested container flag on any container's 
        ///AtomicSafetyHandle that detects that it is being used to store containers. This makes the Job Debugger throw an explicit error when scheduling a job which tries 
        ///to use a nested container.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to flag.</param>
        ///<param name="isNestedContainer">Set to <c>true</c> to flag the container protected by this AtomicSafetyHandle as nested; <c>false</c> otherwise.</param>
        ///<seealso cref="AtomicSafetyHandle.GetNestedContainer" />
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern void SetNestedContainer(AtomicSafetyHandle handle, bool isNestedContainer);

        ///<summary>Checks whether an AtomicSafetyHandle represents a nested container.</summary>
        ///<remarks>If the AtomicSafetyHandle is marked as a nested container, it can't be used safely in jobs.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>
        ///  <c>true</c> if the given AtomicSafetyHandle is marked as a nested container; <c>false</c> otherwise.</returns>
        ///<seealso cref="AtomicSafetyHandle.SetNestedContainer" />
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern bool GetNestedContainer(AtomicSafetyHandle handle);

        // Performs CheckWriteAndThrow and then bumps the secondary version.
        // This allows for example a NativeArray that becomes invalid if the Length of a List
        // is changed to be invalidated, while the NativeList handle itself remains valid.
        ///<summary>Check whether the referenced native container can be written to and increment the secondary version number if so.</summary>
        ///<remarks>For more information about native container version numbers, refer to [Copying NativeContainer structures](xref:job-system-copy-nativecontainer).</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckWriteAndBumpSecondaryVersion(AtomicSafetyHandle handle)
        {
            unsafe
            {
                int* ptr = (int*)(void*)handle.versionNode;
                if (ptr == null || handle.version != (*ptr & WriteCheck))
                {
                    CheckWriteAndThrowNoEarlyOut(handle);
                }

                ptr[1] = ptr[1] + VersionIncrement;
            }
        }

        // For debugging purposes in unit tests we need to sometimes just sync
        // all jobs against an handle before shutting down.
        ///<summary>Waits for all jobs running against the <see cref="AtomicSafetyHandle" /> to complete.</summary>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>Whether the job system waited for any jobs to finish.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern EnforceJobResult EnforceAllBufferJobsHaveCompleted(AtomicSafetyHandle handle);

        // Waits for all jobs running against this AtomicSafetyHandle to complete,
        // then releases the atomic safetyhandle.
        // e.g. You are forced to delete some memory right now,
        // irregardless of any potential jobs that are still running.
        // (It is up to you to still give an error message.)
        // This is unusual behaviour, normally we would simply throw an exception
        // and thus simply not perform the Disposing.
        // But sometimes you just need to delete memory right away, for example Mesh.EndWriteVertices().
        ///<summary>Waits for all jobs running against an <see cref="AtomicSafetyHandle" /> to complete and then releases the AtomicSafetyHandle.</summary>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>Whether the job system waited for any jobs to finish.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern EnforceJobResult EnforceAllBufferJobsHaveCompletedAndRelease(AtomicSafetyHandle handle);

        // Waits for all jobs running against this AtomicSafetyHandle to complete,
        // Then marks the atomic safety handle to no longer be readable or writable.
        // Thus the only thing you can do with it is dispose it.
        ///<summary>Waits for all jobs running against an <see cref="AtomicSafetyHandle" /> to complete and then disables the read and write access on the AtomicSafetyHandle.</summary>
        ///<remarks>When read and write acess is disabled, you can only dispose of the AtomicSafetyHandle.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>Whether the job system waited for any jobs to finish.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern EnforceJobResult EnforceAllBufferJobsHaveCompletedAndDisableReadWrite(AtomicSafetyHandle handle);

        // Make sure that a jobhandle covers all jobs scheduled against a buffer,
        [NativeMethod(IsThreadSafe = true)]
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static extern bool CheckAllBufferJobsAreDependencyOrHaveCompleted(AtomicSafetyHandle handle, JobHandle job);

        // Same as CheckReadAndThrow but the early out has already been performed in the call site for performance reasons.
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static extern void CheckReadAndThrowNoEarlyOut(AtomicSafetyHandle handle);

        // Same as CheckWriteAndThrow but the early out has already been performed in the call site for performance reasons.
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static extern void CheckWriteAndThrowNoEarlyOut(AtomicSafetyHandle handle);

        // Checks if the handle can be deallocated.
        // If not (already destroyed, job currently accessing the data) throws an exception.
        ///<summary>Check if an AtomicSafetyHandle can be deallocated.</summary>
        ///<remarks>Throws an exception if the AtomicSafetyHandle has already been destroyed or a job is currently accessing the data.</remarks>
        ///<param name="handle">The <see cref="Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle "> AtomicSafetyHandle</see> to check.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static extern void CheckDeallocateAndThrow(AtomicSafetyHandle handle);

        ///<summary>Check whether it's safe to create a memory-aliasing view to a native container.</summary>
        ///<remarks>Use this method when you implement native container methods that create memory-aliasing views, such as <c>GetEnumerator</c> or <c>AsArray</c>. This method checks whether it is safe to copy the 
        ///data pointer to the backing memory of the native container into a new view structure that uses the secondary version number.
        ///
        ///This method checks if there are pending jobs that might affect the size of the native container and risk reallocating the container's backing memory. This method doesn't throw an exception if the job writes through an 
        ///<c>AtomicSafetyHandle</c> that uses a secondary version number. 
        ///
        ///The difference between this method and `<see cref="AtomicSafetyHandle.CheckReadAndThrow" />` is that that <c>CheckReadAndThrow</c> throws an exception if there are any pending jobs writing to the native container. 
        ///<c>CheckGetSecondaryDataPointerAndThrow</c> throws an exception only if there is a risk of a job reallocating the native container's backing memory, which makes the data pointer copied to the view invalid.
        ///
        ///For more information about secondary version numbers and safe access to dynamic containers, refer to [Copying NativeContainer structures](xref:job-system-copy-nativecontainer).</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<seealso cref="NativeContainerAttribute" />
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static extern void CheckGetSecondaryDataPointerAndThrow(AtomicSafetyHandle handle);

        ///<summary>Fetches the job handles of all jobs that read from an AtomicSafetyHandle.</summary>
        ///<remarks>This is a debugging method used to produce better error messages and is not intended to be called from user code.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to return readers for.</param>
        ///<param name="maxCount">The maximum number of AtomicSafetyHandles to write to the output array.</param>
        ///<param name="output">A buffer where the job handles are written.</param>
        ///<returns>The number of readers on the handle, which might be greater than the maximum count provided.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static unsafe extern int GetReaderArray(AtomicSafetyHandle handle, int maxCount, IntPtr output);

        ///<summary>Gets any writers on an AtomicSafetyHandle.</summary>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>The job handle of the writer.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern JobHandle GetWriter(AtomicSafetyHandle handle);

        // Checks if the handle can be read from
        // If not (already destroyed, job currently writing to the data) throws an exception.
        ///<summary>Check whether the referenced native container can be read from.</summary>
        ///<remarks>Throws an exception if the AtomicSafetyHandle is already destroyed, or a job is currently writing to the data.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CheckReadAndThrow(AtomicSafetyHandle handle)
        {
            var versionPtr = (int*)handle.versionNode;
            if (handle.version != ((*versionPtr) & ReadCheck))
                CheckReadAndThrowNoEarlyOut(handle);
        }

        // Checks if the handle can be written to
        // If not (already destroyed, job currently reading or writing to the data) throws an exception.
        ///<summary>Check whether the referenced native container can be written to.</summary>
        ///<remarks>Throws the <c>InvalidOperationException</c> exception if the <c>AtomicSafetyHandle</c> is already destroyed or a job is currently reading or writing to the data.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CheckWriteAndThrow(AtomicSafetyHandle handle)
        {
            var versionPtr = (int*)handle.versionNode;
            if (handle.version != ((*versionPtr) & WriteCheck))
                CheckWriteAndThrowNoEarlyOut(handle);
        }

        // When the handle is of a non-default value, checks whether it is still valid
        // If not (already destroyed) throws an exception.
        ///<summary>Checks that the handle has been initialized, and if so, checks that it is still valid.</summary>
        ///<remarks>This is almost identical to <see cref="AtomicSafetyHandle.IsValidNonDefaultHandle" />, except that if the AtomicSafetyHandle has been initialized 
        ///but isn't valid, then an <c>ObjectDisposedException</c> is thrown instead of returning <c>false</c>.
        ///
        ///If the handle hasn't been initialized, then this method does nothing. Use <see cref="AtomicSafetyHandle.IsDefaultValue" /> to check whether a handle is uninitialized.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static unsafe void ValidateNonDefaultHandle(in AtomicSafetyHandle handle)
        {
            if (!IsDefaultValue(handle))
            {
                CheckExistsAndThrow(handle);
            }
        }

        // When the handle is of a non-default value, checks whether it is still valid
        // If not valid, return false
        ///<summary>Checks if an AtomicSafetyHandle has been initialized and is valid.</summary>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>
        ///  <c>true</c> if the AtomicSafetyHandle has been initialized and is valid; <c>false</c> otherwise.</returns>
        public static unsafe bool IsValidNonDefaultHandle(in AtomicSafetyHandle handle)
        {
            if (!IsDefaultValue(handle))
            {
                return IsHandleValid(handle);
            }

            return false;
        }

        // Checks if the handle is still valid.
        // If not (already destroyed) throws an exception.
        ///<summary>Check if an AtomicSafetyHandle is valid.</summary>
        ///<remarks>Throws an exception if the AtomicSafetyHandle is already destroyed. 
        ///                
        ///An `AtomicSafetyHandle` is invalid under the following conditions:
        ///
        ///* The version number it stores no longer matches the version number of the associated entry in the safety system.
        ///* <see cref="AtomicSafetyHandle.Release" /> is called on the <c>AtomicSafetyHandle</c>, or on another <c>AtomicSafetyHandle</c> that references the same memory region. 
        ///* The secondary version number it stores no longer matches the secondary version number of the associated entry in the safety system. This situation happens when <see cref="AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion" /> 
        ///or <see cref="AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite" /> are called on the <c>AtomicSafetyHandle</c>. 
        ///
        ///For more information about container version numbers, refer to [Copying NativeContainer structures](xref:job-system-copy-nativecontainer).
        ///
        ///CheckExistsAndThrow is identical in behavior to <see cref="AtomicSafetyHandle.IsHandleValid" />, except that it throws an exception when the handle isn't valid, rather than returning <c>false</c>.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<seealso cref="AtomicSafetyHandle.IsHandleValid" />
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CheckExistsAndThrow(in AtomicSafetyHandle handle)
        {
            var versionPtr = (int*)handle.versionNode;
            if (handle.version != ((*versionPtr) & ReadWriteDisposeCheck))
                throw new ObjectDisposedException("The NativeArray has been disposed, it is not allowed to access it");
        }

        // Checks if the handle is still valid and returns true if it is, false otherwise
        ///<summary>Checks if an AtomicSafetyHandle is valid.</summary>
        ///<remarks>An `AtomicSafetyHandle` is invalid under the following conditions:
        ///
        ///* The version number it stores no longer matches the version number of the associated entry in the safety system.
        ///* <see cref="AtomicSafetyHandle.Release" /> is called on the <c>AtomicSafetyHandle</c>, or on another <c>AtomicSafetyHandle</c> that references the same memory region. 
        ///* The secondary version number it stores no longer matches the secondary version number of the associated entry in the safety system. This situation happens when <see cref="AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion" /> 
        ///or <see cref="AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite" /> are called on the <c>AtomicSafetyHandle</c>. 
        ///
        ///For more information about container version numbers, refer to [Copying NativeContainer structures](xref:job-system-copy-nativecontainer).</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>
        ///  <c>true</c> if the AtomicSafetyHandle is valid, <c>false</c> otherwise.</returns>
        public static unsafe bool IsHandleValid(in AtomicSafetyHandle handle)
        {
            var versionPtr = (int*)handle.versionNode;
            if (handle.version != ((*versionPtr) & ReadWriteDisposeCheck))
                return false;

            return true;
        }

        ///<summary>Gets the name of a specified job that reads from an AtomicSafetyHandle.</summary>
        ///<remarks>This is a debugging method used to produce better error messages and is not intended to be called from user code.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<param name="readerIndex">Index of the reader.</param>
        ///<returns>The debug name of the reader.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetReaderName(AtomicSafetyHandle handle, int readerIndex);

        ///<summary>Gets the debug name of the current writer on an AtomicSafetyHandle.</summary>
        ///<param name="handle">The AtomicSafetyHandle to check.</param>
        ///<returns>The name of the writer, if there is any.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetWriterName(AtomicSafetyHandle handle);

        ///<summary>Allocates a new static safety ID to store information for the provided type.</summary>
        ///<remarks>After creating a new static safety ID, use <see cref="SetStaticSafetyId" /> to assign it to the applicable <see cref="AtomicSafetyHandle" /> instances.
        ///
        ///The job debugger uses this static safety ID to look up the provided type's name, and any custom error messages created with <see cref="SetCustomErrorMessage" />. Without 
        ///this information, the job debugger can only give general error messages that might not clearly identify the source of the error.</remarks>
        ///<param name="ownerTypeNameBytes">The name of the scripting type that owns the <see cref="AtomicSafetyHandle" />, to be embedded in error messages involving 
        ///the handle. This must be a UTF8-encoded byte array, and doesn't have to be null-terminated.</param>
        ///<param name="byteCount">The number of bytes in the <c>ownerTypeNameBytes</c> array, excluding the optional null terminator.</param>
        ///<returns>The newly allocated safety ID.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static unsafe extern int NewStaticSafetyId(byte* ownerTypeNameBytes, int byteCount);

        ///<summary>Allocates a new static safety ID, to store information for the provided type T.</summary>
        ///<remarks>After creating a new static safety ID, use <see cref="SetStaticSafetyId" /> to assign it to the applicable <see cref="AtomicSafetyHandle" /> instances.
        ///
        ///The job debugger uses this static safety ID to look up the provided type's name, and any custom error messages created with <see cref="SetCustomErrorMessage" />. Without 
        ///this information, the job debugger can only give general error messages that might not clearly identify the source of the error.
        ///
        ///This variant uses the name of the provided type <c>T</c> as the handle's owner type name.</remarks>
        ///<returns>The newly allocated safety ID.</returns>
        public static unsafe int NewStaticSafetyId<T>()
        {
            var ownerTypeName = typeof(T).ToString();
            var bytes = Encoding.UTF8.GetBytes(ownerTypeName);
            fixed(byte* pBytes = bytes)
            {
                return NewStaticSafetyId(pBytes, bytes.Length);
            }
        }

        ///<summary>Provides a custom error message for a specific job debugger error type, in cases where additional context can be provided.</summary>
        ///<remarks>The job debugger uses the specified static safety ID and error type to look up error messages for <see cref="AtomicSafetyHandle" /> instances. You 
        ///should provide a message for each applicable type of error defined in <see cref="AtomicSafetyErrorType" />. Without a specific error message, the job debugger only gives general 
        ///error messages that might not clearly identify the source of the error.
        ///
        ///If the message contains any of the following sequences, they are replaced with the corresponding context-specific data (if available) when the message is emitted:
        ///
        ///- {2} = this job name.           example: "BoidsJob"
        ///
        ///- {3} = this job field.          example: "BoidsJob.boidsBuffer"
        ///
        ///- {5} = this owner type.         example: "NativeArray&lt;int&gt;"</remarks>
        ///<param name="staticSafetyId">The static safety ID with which the provided custom error message should be associated. 
        ///This ID must have been allocated with <see cref="NewStaticSafetyId" />. Passing 0 is invalid because 0 is the default static safety ID, and its error messages cann't be modified.</param>
        ///<param name="errorType">The class of error that should use the provided custom error message instead of the default job debugger error message.</param>
        ///<param name="messageBytes">The error message to use for the specified error type. This should be a UTF8-encoded byte array, and doesn't have to be null-terminated.</param>
        ///<param name="byteCount">The number of bytes in the <c>messageBytes</c> array, excluding the optional null terminator.</param>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [NativeMethod(ThrowsException = true, IsThreadSafe = true)]
        public static unsafe extern void SetCustomErrorMessage(int staticSafetyId, AtomicSafetyErrorType errorType, byte* messageBytes, int byteCount);

        ///<summary>Assigns a provided static safety ID to an <see cref="AtomicSafetyHandle" />.</summary>
        ///<remarks>The ID's owner type name and any custom error messages are used by the job debugger when reporting errors involving the target handle.</remarks>
        ///<param name="handle">The AtomicSafetyHandle to modify.</param>
        ///<param name="staticSafetyId">The static safety ID to associate with the provided AtomicSafetyHandle. This ID must have been allocated with <see cref="NewStaticSafetyId" />.</param>
        ///<seealso cref="AtomicSafetyHandle.NewStaticSafetyId" />
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static unsafe void SetStaticSafetyId(ref AtomicSafetyHandle handle, int staticSafetyId)
        {
            handle.staticSafetyId = staticSafetyId;
        }

        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.AIModule")]
        internal static void CreateHandle(out AtomicSafetyHandle safety, Allocator allocator)
        {
            safety = (allocator == Allocator.Temp) ? GetTempMemoryHandle() : Create();

            if(!Jobs.LowLevel.Unsafe.JobsUtility.AreHandlesPatched)
                return;

            switch(allocator)
            {
                case Allocator.TempJob:
                case Allocator.Persistent:
                    throw new InvalidOperationException("Jobs can only create Temp memory");
            }
        }

        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.AIModule")]
        internal static void DisposeHandle(ref AtomicSafetyHandle safety)
        {
            CheckDeallocateAndThrow(safety);
            // If the safety handle is for a temp allocation, get a new safety handle for this instance which can be marked as invalid
            // Setting it to new AtomicSafetyHandle is not enough since the handle needs a valid node pointer in order to give the correct errors
            if (IsTempMemoryHandle(safety))
            {
                int staticSafetyId = safety.staticSafetyId;
                safety = AtomicSafetyHandle.GetTempMemoryHandle();
                safety.staticSafetyId = staticSafetyId;
            }
            Release(safety);
        }
    }
}

