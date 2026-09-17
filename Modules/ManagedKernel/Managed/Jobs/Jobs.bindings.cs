// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Diagnostics;
using UnityEngine.Scripting;
using Unity.Burst;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Jobs.LowLevel.Unsafe
{
    // Preserve JobProducerType so Burst can find the attribute, despite no player references (UUM-147717).
    ///<summary>Indicates that a job interface's Execute method can be Burst compiled.</summary>
    ///<remarks>All job interface types must be marked with the <see cref="JobProducerTypeAttribute" />. This is used to compile the Execute method by the Burst ASM inspector.</remarks>
    [AttributeUsage(AttributeTargets.Interface)]
    [RequireAttributeUsages]
    public sealed class JobProducerTypeAttribute : Attribute
    {
        ///<summary>Contains the <c>Execute</c> static method which the job system invokes.</summary>
        public Type ProducerType { get; }

        ///<summary>Indicates which type is the JobProducerType.</summary>
        ///<remarks>The job system and the Burst compiler uses this attribute when determining how to instantiate custom job types.</remarks>
        ///<param name="producerType">The type containing a static <c>Execute</c> method which the job system invokes.</param>
        public JobProducerTypeAttribute(Type producerType)
        {
            ProducerType = producerType;
        }
    }

    ///<summary>Provides information about a range that a job is allowed to work on.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct JobRanges
    {
        ///<summary>The size of the batch.</summary>
        internal int    BatchSize;
        ///<summary>The number of jobs.</summary>
        internal int    NumJobs;
        ///<summary>The total iteration count.</summary>
        public   int    TotalIterationCount;

        ///<summary>The start and end index of the job range.</summary>
        internal IntPtr StartEndIndex;
    }

    ///<summary>Options for scheduling a managed job.</summary>
    public enum ScheduleMode
    {
        ///<summary>Run a job immediately on calling thread.</summary>
        Run          = 0,
        ///<summary>Schedule a job as batched. This enumeration value exists to support legacy code, and is the same as <see cref="ScheduleMode.Parallel" />, which you should use instead.</summary>
        [Obsolete("Batched is obsolete, use Parallel or Single depending on job type. (UnityUpgradable) -> Parallel", false)]
        Batched      = 1,
        ///<summary>Schedule a job to run on multiple worker threads if possible. Jobs that can't run concurrently run on one thread only.</summary>
        Parallel     = 1,
        ///<summary>Schedule a job to run on a single worker thread.</summary>
        Single       = 2,
    }

    ///<summary>Sets what the job is used for.</summary>
    [Obsolete("Reflection data is now universal between job types. The parameter can be removed.", false)]
    public enum JobType
    {
        ///<summary>A single job.</summary>
        Single      = 0,
        ///<summary>A parallel for job.</summary>
        ParallelFor = 1
    }

    ///<summary>Provides methods for creating, running, and debugging jobs.</summary>
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindings.h")]
    [NativeHeader("NativeJobs/JobSystem.h")]
    public static partial class JobsUtility
    {
        ///<summary>Provides job parameters for scheduling.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct JobScheduleParameters
        {
            ///<summary>A <see cref="JobHandle" /> of any dependency that the job has.</summary>
            public JobHandle Dependency;
            ///<summary>Corresponds to <see cref="ScheduleMode" /> options.</summary>
            public int ScheduleMode;
            ///<summary>A pointer to the reflection data.</summary>
            public IntPtr ReflectionData;
            ///<summary>A pointer to the job data.</summary>
            public IntPtr JobDataPtr;

            unsafe public JobScheduleParameters(void* i_jobData, IntPtr i_reflectionData, JobHandle i_dependency, ScheduleMode i_scheduleMode)
            {
                Dependency = i_dependency;
                JobDataPtr = (IntPtr)i_jobData;
                ReflectionData = i_reflectionData;
                ScheduleMode = (int)i_scheduleMode;
            }
        }

        ///<summary>Gets the begin index and end index of a range.</summary>
        public static unsafe void GetJobRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex)
        {
            int* startEndIndices = (int*)ranges.StartEndIndex;
            beginIndex = startEndIndices[jobIndex * 2];
            endIndex = startEndIndices[jobIndex * 2 + 1];
        }

        ///<summary>Gets a work stealing range.</summary>
        ///<returns>Returns true if successful.</returns>
        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool GetWorkStealingRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex);

        ///<summary>Schedules a single <see cref="IJob" />.</summary>
        ///<returns>A <see cref="JobHandle" /> to the new job.</returns>
        [FreeFunction("ScheduleManagedJob", ThrowsException = true, IsThreadSafe = true)]
        public static extern JobHandle Schedule(ref JobScheduleParameters parameters);

        ///<summary>Schedules a <see cref="IJobParallelFor" /> job.</summary>
        ///<returns>A <see cref="JobHandle" /> to the new job.</returns>
        [FreeFunction("ScheduleManagedJobParallelFor", ThrowsException = true, IsThreadSafe = true)]
        public static extern JobHandle ScheduleParallelFor(ref JobScheduleParameters parameters, int arrayLength, int innerloopBatchCount);

        ///<summary>Schedules a <see cref="IJobParallelFor" /> job.</summary>
        ///<returns>A <see cref="JobHandle" /> to the new job.</returns>
        [FreeFunction("ScheduleManagedJobParallelForDeferArraySize", ThrowsException = true, IsThreadSafe = true)]
        unsafe public static extern JobHandle ScheduleParallelForDeferArraySize(ref JobScheduleParameters parameters, int innerloopBatchCount, void* listData, void* listDataAtomicSafetyHandle);

        ///<summary>Schedules an <see cref="T:UnityEngine.Jobs.IJobParallelForTransform" /> job.</summary>
        ///<returns>A <see cref="JobHandle" /> to the new job.</returns>
        [FreeFunction("ScheduleManagedJobParallelForTransform", ThrowsException = true)]
        public static extern JobHandle ScheduleParallelForTransform(ref JobScheduleParameters parameters, IntPtr transfromAccesssArray);

        ///<summary>Schedules an <see cref="T:UnityEngine.Jobs.IJobParallelForTransform" /> job with read-only access to the transform data.</summary>
        ///<remarks>This method provides better parallelization because it can read all transforms in parallel instead of just parallelizing across transforms in 
        ///different hierarchies.</remarks>
        ///<returns>A <see cref="JobHandle" /> to the new job.</returns>
        [FreeFunction("ScheduleManagedJobParallelForTransformReadOnly", ThrowsException = true)]
        public static extern JobHandle ScheduleParallelForTransformReadOnly(ref JobScheduleParameters parameters, IntPtr transfromAccesssArray, int innerloopBatchCount);

        ///<summary>Injects debug checks for min and max ranges of a native array.</summary>
        ///<param name="bufferRangePatchData">The buffer range patch data to write the checks into.</param>
        ///<param name="jobdata">Pointer to the job data that owns the buffer.</param>
        ///<param name="startIndex">The index of the first element in the range.</param>
        ///<param name="rangeSize">The number of elements in the range.</param>
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        unsafe public static extern void PatchBufferMinMaxRanges(IntPtr bufferRangePatchData, void* jobdata, int startIndex, int rangeSize);

        [FreeFunction(ThrowsException = true, IsThreadSafe = true)]
        private static extern IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, object managedJobFunction0, object managedJobFunction1, object managedJobFunction2);

        [Obsolete("JobType is obsolete. The parameter should be removed. (UnityUpgradable) -> !1")]
        public static IntPtr CreateJobReflectionData(Type type, JobType jobType, object managedJobFunction0, object managedJobFunction1 = null, object managedJobFunction2 = null)
        {
            return CreateJobReflectionData(type, type, managedJobFunction0, managedJobFunction1, managedJobFunction2);
        }

        ///<summary>Creates job reflection data.</summary>
        ///<returns>Returns a pointer to internal JobReflectionData.</returns>
        public static IntPtr CreateJobReflectionData(Type type, object managedJobFunction0, object managedJobFunction1 = null, object managedJobFunction2 = null)
        {
            return CreateJobReflectionData(type, type, managedJobFunction0, managedJobFunction1, managedJobFunction2);
        }

        [Obsolete("JobType is obsolete. The parameter should be removed. (UnityUpgradable) -> !2")]
        public static IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, JobType jobType, object managedJobFunction0)
        {
            return CreateJobReflectionData(wrapperJobType, userJobType, managedJobFunction0, null, null);
        }

        ///<summary>Creates job reflection data.</summary>
        ///<returns>Returns a pointer to internal JobReflectionData.</returns>
        public static IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, object managedJobFunction0)
        {
            return CreateJobReflectionData(wrapperJobType, userJobType, managedJobFunction0, null, null);
        }

        ///<summary>Checks if this is in a job.</summary>
        ///<remarks>Returns true if this is called from a job.</remarks>
        public static extern bool IsExecutingJob { [NativeMethod(Name = "GetIsExecutingScriptingJob", IsFreeFunction = true, IsThreadSafe = true)] get; }

        internal static extern bool AreHandlesPatched { [NativeMethod(Name = "GetAreHandlesPatched", IsFreeFunction = true, IsThreadSafe = true)] get; }

        ///<summary>Set whether to use the jobs debugger at runtime.</summary>
        ///<remarks>The jobs debugger is only supported in the Editor, so this property only has effect in the Editor.</remarks>
        public static extern bool JobDebuggerEnabled { [FreeFunction] get; [FreeFunction] set; }
        ///<summary>Set whether to run jobs in Mono or Burst.</summary>
        ///<remarks>When disabled, this forces jobs that have already been compiled with Burst to run in Mono instead. For example, if you want to debug jobs 
        ///or want to compare behavior or performance.</remarks>
        public static extern bool JobCompilerEnabled { [FreeFunction] get; [FreeFunction] set; }

        [FreeFunction("JobSystem::GetJobQueueWorkerThreadCount")]
        static extern int GetJobQueueWorkerThreadCount();

        [FreeFunction("JobSystem::ForceSetJobQueueWorkerThreadCount")]
        static extern void SetJobQueueMaximumActiveThreadCount(int count);

        ///<summary>Maximum number of worker threads available to the Unity JobQueue (RO).</summary>
        ///<remarks>This property determines the maximum number of worker threads when you set the <see cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount">JobWorkerCount</see> property. This value is read-only at runtime, but you can set it with the command line argument <c>-job-worker-count value</c> for the Editor or standalone Players.   
        ///Unity ignores this setting if this value exceeds the default value that the runtime selects for the specific platform and hardware.</remarks>
        public static extern int JobWorkerMaximumCount
        {
            [FreeFunction("JobSystem::GetJobQueueMaximumThreadCount")]
            get;
        }

        ///<summary>Reset <see cref="JobWorkerCount" /> to the Unity adjusted value.</summary>
        ///<remarks>Sets <see cref="JobWorkerCount" /> to the Unity adjusted value. By default, this is <see cref="JobWorkerMaximumCount" />. Also re-enables the Unity automatic adjustment 
        ///mode for <see cref="JobWorkerCount" />.</remarks>
        [FreeFunction("JobSystem::ResetJobQueueWorkerThreadCount")]
        public static extern void ResetJobWorkerCount();

        ///<summary>Current number of worker threads available to the Unity JobQueue.</summary>
        ///<remarks>By default, this property takes the value of <see cref="JobWorkerMaximumCount" />. You can set the value of this property at runtime to dynamically 
        ///reduce the number worker threads available to the job queue. This can have the effect of saving power, or reducing the CPU load on a shared or virtual machine. This is useful 
        ///if you have multiple instances of your application running as a server, and you want to prevent any single instance from monopolizing the resources of the machine.
        ///
        ///You can't set this value below 0, or above the value of the <see cref="JobWorkerMaximumCount" /> property. Trying to do so throwa an out of range exception.
        ///
        ///On some platforms, such as Android, Unity automatically adjusts this value at runtime if the operating system indicates that the number of available cores has changed. This 
        ///can happen if the device has entered, or left, power-saving mode. However, if you set this property manually to any valid value, Unity stops any automatic adjustment and 
        ///ignores any requests from the operating system. To restore the automatic adjustment mode, call <see cref="ResetJobWorkerCount" />.</remarks>
        public static int JobWorkerCount
        {
            get { return GetJobQueueWorkerThreadCount(); }
            set
            {
                if ((value < 0) || (value > JobsUtility.JobWorkerMaximumCount))
                {
                    throw new ArgumentOutOfRangeException("JobWorkerCount", $"Invalid JobWorkerCount {value} must be in the range 0 -> {JobsUtility.JobWorkerMaximumCount}");
                }
                SetJobQueueMaximumActiveThreadCount(value);
            }
        }

        ///<summary>The maximum number of job threads that the job system can create.</summary>
        ///<remarks>This maximum is the maximum number of threads that the job can system support. However, the maximum number of job worker threads that the job system 
        ///creates is lower because the job system doesn't create more job worker threads than logical CPU cores on the target hardware. This value is useful for compile time constants, 
        ///however when used for creating buffers it might be larger than what you need. If you want to allocate a buffer that can be subdivided evenly between job worker threads, use 
        ///the runtime constant that <see cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount" /> returns.</remarks>
        ///<seealso cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount" />
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class JobWorkerExample : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        // Sets worker count thread to 1
        ///        Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount = 1;
        ///        // Sets worker count thread to 4
        ///        Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount = 4;
        ///        // Reads the value of JobWorkerCount and outputs it to the Unity log
        ///        Debug.Log("${ Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount}");
        ///
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public const int MaxJobThreadCount = 128;
        ///<summary>The size of a cache line.</summary>
        public const int CacheLineSize = 64;

        ///<summary>Gets the index for the current thread when executing a job, otherwise 0.</summary>
        ///<remarks>When multiple threads are executing jobs, no two threads have the same index. The range of this property is [0, <see cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount" />).
        ///The value returned when used from within a job is the same as the value stored in job members decorated with <see cref="Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndexAttribute" />.</remarks>
        ///<seealso cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndexCount" />
        public static extern int ThreadIndex
        {
            [BurstAuthorizedExternalMethod]
            [FreeFunction("GetJobWorkerIndex", IsThreadSafe = true)]
            get;
        }

        ///<summary>Gets the maximum number of job workers that can work on a job at the same time.</summary>
        ///<remarks>The job system creates a number of job worker threads no greater than the number of logical CPU cores for the platform. However, because arbitrary 
        ///threads can execute jobs via work stealing, the job system allocates extra workers which act as temporary job worker threads. JobsUtility.ThreadIndexCount represents the 
        ///maximum number of job worker threads plus the temporary workers that the job system uses. As such, this value is useful to allocate buffers which should be subdivided evenly 
        ///between job workers because <see cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.ThreadIndex" /> and <see cref="Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndexAttribute" /> never return a value greater
        ///than JobsUtility.ThreadIndexCount.</remarks>
        public static extern int ThreadIndexCount
        {
            [BurstAuthorizedExternalMethod]
            [FreeFunction("GetJobWorkerIndexCount", IsThreadSafe = true)]
            get;
        }

        [FreeFunction("IsJobQueueBatchingEnabled")]
        static extern bool GetJobBatchingEnabled();
        internal static bool JobBatchingEnabled => GetJobBatchingEnabled();

        [FreeFunction("JobDebuggerGetSystemIdCellPtr")]
        internal static extern IntPtr GetSystemIdCellPtr();

        [FreeFunction("JobDebuggerClearSystemIds")]
        internal static extern void ClearSystemIds();

        [FreeFunction("JobDebuggerGetSystemIdMappings")]
        internal static unsafe extern int GetSystemIdMappings(JobHandle* handles, int* systemIds, int maxCount);

        ///<exclude />
        internal delegate void PanicFunction_();

        [AutoStaticsCleanupOnCodeReload]
        internal static PanicFunction_ PanicFunction;

        [RequiredByNativeCode]
        private static void InvokePanicFunction()
        {
            var func = PanicFunction;
            if (func == null)
                return;
            func();
        }
    }
}
