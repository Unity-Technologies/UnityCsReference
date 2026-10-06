// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Jobs;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Jobs.LowLevel.Unsafe
{
    ///<summary>Provides an implementation for batch query jobs.</summary>
    ///<remarks>A batch query job is a job implemented in native code in Unity. This struct is part of the interface used to schedule a batch job. Use this struct
    ///to implement new BatchQuery instances, but not to execute them.</remarks>
    public struct BatchQueryJob<CommandT, ResultT> where CommandT : struct
        where ResultT : struct
    {
        public BatchQueryJob(NativeArray<CommandT> commands, NativeArray<ResultT> results)
        {
            this.commands = commands;
            this.results = results;
        }

        [ReadOnly]
        internal NativeArray<CommandT> commands;
        internal NativeArray<ResultT> results;
    }
    ///<summary>Provides an implementation for batch query jobs.</summary>
    ///<remarks>A batch query job is a job implemented in native code in Unity. This struct is part of the interface used to schedule a batch job. Use this struct
    ///to implement new BatchQuery instances, but not to execute them.</remarks>
    public partial struct BatchQueryJobStruct<T> where T : struct
    {
        [AutoStaticsCleanupOnCodeReload] // job reflection data is invalid after code reload and must be recreated
        static internal IntPtr                    jobReflectionData;

        ///<summary>Initializes a BatchQueryJobStruct instance and returns a pointer to the internal structure <c>System.IntPtr</c></summary>
        ///<remarks>Pass the returned <c>System.IntPtr</c> to <see cref="JobsUtility.JobScheduleParameters" />.</remarks>
        public static IntPtr Initialize()
        {
            if (jobReflectionData == IntPtr.Zero)
                jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), null);
            return jobReflectionData;
        }
    }
}
