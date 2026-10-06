// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Jobs
{
    // The Unity.Collections.CodeGen ILPP registers job reflection data for every assembly that
    // references UnityEngine.ManagedKernelModule, but it cannot process that module itself - an
    // assembly never references itself, so the module's own job structs get no reflection data.
    // Without it, JobStruct<T>.Initialize() is [BurstDiscard]'d and Schedule() silently returns a
    // default JobHandle when called from Burst-compiled code, so Complete() completes nothing.
    static partial class CollectionsJobReflectionRegistration
    {
        [OnCodeLoaded]
        static void RegisterJobReflectionData()
        {
            IJobExtensions.EarlyJobInit<NativeBitArrayDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeHashMapDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeListDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeQueueDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeReferenceDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeRingQueueDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeStreamDisposeJob>();
            IJobExtensions.EarlyJobInit<NativeTextDisposeJob>();
            IJobExtensions.EarlyJobInit<UnsafeQueueDisposeJob>();
            IJobExtensions.EarlyJobInit<UnsafeDisposeJob>();
            IJobExtensions.EarlyJobInit<UnsafeParallelHashMapDataDisposeJob>();
            IJobExtensions.EarlyJobInit<UnsafeParallelHashMapDisposeJob>();

            IJobExtensions.EarlyJobInit<CollectionHelper.DummyJob>();

            // The stream construction jobs are scheduled from Burst-compiled code, so without reflection
            // data their Schedule() no-ops and the stream is never allocated for each buffer.
            IJobExtensions.EarlyJobInit<NativeStream.ConstructJobList>();
            IJobExtensions.EarlyJobInit<NativeStream.ConstructJobArray>();
            IJobExtensions.EarlyJobInit<NativeStream.ConstructJob>();
            IJobExtensions.EarlyJobInit<UnsafeStream.DisposeJob>();
            IJobExtensions.EarlyJobInit<UnsafeStream.ConstructJobList>();
            IJobExtensions.EarlyJobInit<UnsafeStream.ConstructJob>();
        }
    }
}
