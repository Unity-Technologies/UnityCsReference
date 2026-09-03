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
    public partial struct BatchQueryJobStruct<T> where T : struct
    {
        [AutoStaticsCleanupOnCodeReload] // job reflection data is invalid after code reload and must be recreated
        static internal IntPtr                    jobReflectionData;

        public static IntPtr Initialize()
        {
            if (jobReflectionData == IntPtr.Zero)
                jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), null);
            return jobReflectionData;
        }
    }
}
