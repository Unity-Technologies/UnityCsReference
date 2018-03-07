// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    [NativeHeader("Runtime/Dynamics/BatchCommands/RaycastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public struct RaycastCommand
    {
        public RaycastCommand(Vector3 from, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers, int maxHits = 1)
        {
            this.from = from;
            this.direction = direction;
            this.distance = distance;
            this.layerMask = layerMask;
            this.maxHits = maxHits;
        }

        public Vector3 from;
        public Vector3 direction;
        public float distance { get; set; }
        public int layerMask { get; set; }
        public int maxHits { get; set; }

        public unsafe static JobHandle ScheduleBatch(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            var jobData = new BatchQueryJob<RaycastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<RaycastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Batched);

            return ScheduleRaycastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob);
        }

        [FreeFunction("ScheduleRaycastCommandBatch")]
        unsafe extern private static JobHandle ScheduleRaycastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob);
    }
}
