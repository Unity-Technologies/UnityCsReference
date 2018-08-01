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

        public Vector3 from { get; set; }
        public Vector3 direction {get; set; }
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

    [NativeHeader("Runtime/Dynamics/BatchCommands/SpherecastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public struct SpherecastCommand
    {
        public SpherecastCommand(Vector3 origin,  float radius, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.layerMask = layerMask;
            maxHits = 1;
        }

        public Vector3 origin { get; set; }
        public float radius { get; set; }
        public Vector3 direction { get; set; }
        public float distance { get; set; }
        public int layerMask { get; set; }
        internal int maxHits { get; set; }

        public unsafe static JobHandle ScheduleBatch(NativeArray<SpherecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            var jobData = new BatchQueryJob<SpherecastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<SpherecastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Batched);

            return ScheduleSpherecastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob);
        }

        [FreeFunction("ScheduleSpherecastCommandBatch")]
        unsafe extern private static JobHandle ScheduleSpherecastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob);
    }

    [NativeHeader("Runtime/Dynamics/BatchCommands/CapsulecastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public struct CapsulecastCommand
    {
        public  CapsulecastCommand(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.layerMask = layerMask;
            maxHits = 1;
        }

        public Vector3 point1 { get; set; }
        public Vector3 point2 { get; set; }
        public float radius {get; set; }
        public Vector3 direction {get; set; }
        public float distance { get; set; }
        public int layerMask { get; set; }
        internal int maxHits {get; set; }

        public unsafe static JobHandle ScheduleBatch(NativeArray<CapsulecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            var jobData = new BatchQueryJob<CapsulecastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<CapsulecastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Batched);

            return ScheduleCapsulecastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob);
        }

        [FreeFunction("ScheduleCapsulecastCommandBatch")]
        unsafe extern private static JobHandle ScheduleCapsulecastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob);
    }

    [NativeHeader("Runtime/Dynamics/BatchCommands/BoxcastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public struct BoxcastCommand
    {
        public  BoxcastCommand(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance = float.MaxValue, int layerMask = Physics.DefaultRaycastLayers)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.layerMask = layerMask;
            maxHits = 1;
        }

        public Vector3 center { get; set; }
        public Vector3 halfExtents {get; set; }
        public Quaternion orientation {get; set; }
        public Vector3 direction {get; set; }
        public float distance { get; set; }
        public int layerMask { get; set; }
        internal int maxHits {get; set; }

        public unsafe static JobHandle ScheduleBatch(NativeArray<BoxcastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            var jobData = new BatchQueryJob<BoxcastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<BoxcastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Batched);

            return ScheduleBoxcastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob);
        }

        [FreeFunction("ScheduleBoxcastCommandBatch")]
        unsafe extern private static JobHandle ScheduleBoxcastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob);
    }
}
