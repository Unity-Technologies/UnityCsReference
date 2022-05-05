// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QueryParameters
    {
        public int layerMask;
        public bool hitMultipleFaces;
        public QueryTriggerInteraction hitTriggers;
        public bool hitBackfaces;

        public QueryParameters(int layerMask = Physics.DefaultRaycastLayers, bool hitMultipleFaces = false, QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.UseGlobal, bool hitBackfaces = false)
        {
            this.layerMask = layerMask;
            this.hitMultipleFaces = hitMultipleFaces;
            this.hitTriggers = hitTriggers;
            this.hitBackfaces = hitBackfaces;
        }

        public static QueryParameters Default => new QueryParameters(Physics.DefaultRaycastLayers, false, QueryTriggerInteraction.UseGlobal, false);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ColliderHit
    {
        private int m_ColliderInstanceID;

        public int instanceID => m_ColliderInstanceID;

        // note this is a main-thread only API
        public Collider collider => Object.FindObjectFromInstanceID(instanceID) as Collider;
    }

    [NativeHeader("Modules/Physics/BatchCommands/RaycastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct RaycastCommand
    {
        public RaycastCommand(Vector3 from, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.from = from;
            this.direction = direction;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.distance = distance;
            this.queryParameters = queryParameters;

        }
        public RaycastCommand(PhysicsScene physicsScene, Vector3 from, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.from = from;
            this.direction = direction;
            this.physicsScene = physicsScene;
            this.distance = distance;
            this.queryParameters = queryParameters;
        }

        public Vector3 from { get; set; }
        public Vector3 direction {get; set; }
        public PhysicsScene physicsScene { get; set; }
        public float distance { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<RaycastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<RaycastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleRaycastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        public unsafe static JobHandle ScheduleBatch(NativeArray<RaycastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleRaycastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleRaycastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);

    }

    [NativeHeader("Modules/Physics/BatchCommands/SpherecastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct SpherecastCommand
    {
        public SpherecastCommand(Vector3 origin, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = queryParameters;
        }

        public SpherecastCommand(PhysicsScene physicsScene, Vector3 origin,  float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.origin = origin;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = queryParameters;
        }

        public Vector3 origin { get; set; }
        public float radius { get; set; }
        public Vector3 direction { get; set; }
        public float distance { get; set; }
        public PhysicsScene physicsScene { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<SpherecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<SpherecastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<SpherecastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleSpherecastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        public unsafe static JobHandle ScheduleBatch(NativeArray<SpherecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleSpherecastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleSpherecastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    [NativeHeader("Modules/Physics/BatchCommands/CapsulecastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct CapsulecastCommand
    {
        public CapsulecastCommand(Vector3 p1, Vector3 p2, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = queryParameters;
        }

        public CapsulecastCommand(PhysicsScene physicsScene, Vector3 p1, Vector3 p2, float radius, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.point1 = p1;
            this.point2 = p2;
            this.direction = direction;
            this.radius = radius;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = queryParameters;
        }

        public Vector3 point1 { get; set; }
        public Vector3 point2 { get; set; }
        public float radius {get; set; }
        public Vector3 direction {get; set; }
        public float distance { get; set; }
        public PhysicsScene physicsScene { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<CapsulecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<CapsulecastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<CapsulecastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleCapsulecastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        public unsafe static JobHandle ScheduleBatch(NativeArray<CapsulecastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleCapsulecastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleCapsulecastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    [NativeHeader("Modules/Physics/BatchCommands/BoxcastCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public partial struct BoxcastCommand
    {
        public BoxcastCommand(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.physicsScene = Physics.defaultPhysicsScene;
            this.queryParameters = queryParameters;
        }

        public BoxcastCommand(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, QueryParameters queryParameters, float distance = float.MaxValue)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.direction = direction;
            this.distance = distance;
            this.physicsScene = physicsScene;
            this.queryParameters = queryParameters;
        }

        public Vector3 center { get; set; }
        public Vector3 halfExtents {get; set; }
        public Quaternion orientation {get; set; }
        public Vector3 direction {get; set; }
        public float distance { get; set; }
        public PhysicsScene physicsScene { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<BoxcastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<BoxcastCommand, RaycastHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<BoxcastCommand, RaycastHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleBoxcastBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        public unsafe static JobHandle ScheduleBatch(NativeArray<BoxcastCommand> commands, NativeArray<RaycastHit> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            return ScheduleBatch(commands, results, minCommandsPerJob, 1, dependsOn);
        }

        [FreeFunction("ScheduleBoxcastCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleBoxcastBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    [NativeHeader("Modules/Physics/BatchCommands/ClosestPointCommand.h")]
    [NativeHeader("Runtime/Jobs/ScriptBindings/JobsBindingsTypes.h")]
    public struct ClosestPointCommand
    {
        public ClosestPointCommand(Vector3 point, int colliderInstanceID, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.point = point;
            this.colliderInstanceID = colliderInstanceID;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public ClosestPointCommand(Vector3 point, Collider collider, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.point = point;
            this.colliderInstanceID = collider.GetInstanceID();
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Vector3 point { get; set; }
        public int colliderInstanceID { get; set; }
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 scale { get; set; }

        public unsafe static JobHandle ScheduleBatch(NativeArray<ClosestPointCommand> commands, NativeArray<Vector3> results, int minCommandsPerJob, JobHandle dependsOn = new JobHandle())
        {
            var jobData = new BatchQueryJob<ClosestPointCommand, Vector3>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<ClosestPointCommand, Vector3>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleClosestPointCommandBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob);
        }

        [FreeFunction("ScheduleClosestPointCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleClosestPointCommandBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob);
    }

    [NativeHeader("Modules/Physics/BatchCommands/OverlapSphereCommand.h")]
    public struct OverlapSphereCommand
    {
        public OverlapSphereCommand(Vector3 point, float radius, QueryParameters queryParameters)
        {
            this.point = point;
            this.radius = radius;
            this.queryParameters = queryParameters;
            this.physicsScene = Physics.defaultPhysicsScene;
        }

        public OverlapSphereCommand(PhysicsScene physicsScene, Vector3 point, float radius, QueryParameters queryParameters)
        {
            this.physicsScene = physicsScene;
            this.point = point;
            this.radius = radius;
            this.queryParameters = queryParameters;
        }

        public Vector3 point { get; set; }
        public float radius {get; set; }
        public PhysicsScene physicsScene { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<OverlapSphereCommand> commands, NativeArray<ColliderHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<OverlapSphereCommand, ColliderHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<OverlapSphereCommand, ColliderHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleOverlapSphereBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        [FreeFunction("ScheduleOverlapSphereCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleOverlapSphereBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    [NativeHeader("Modules/Physics/BatchCommands/OverlapBoxCommand.h")]
    public struct OverlapBoxCommand
    {
        public OverlapBoxCommand(Vector3 center, Vector3 halfExtents, Quaternion orientation, QueryParameters queryParameters)
        {
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.queryParameters = queryParameters;
            this.physicsScene = Physics.defaultPhysicsScene;
        }

        public OverlapBoxCommand(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, QueryParameters queryParameters)
        {
            this.physicsScene = physicsScene;
            this.center = center;
            this.halfExtents = halfExtents;
            this.orientation = orientation;
            this.queryParameters = queryParameters;
        }

        public Vector3 center { get; set; }
        public Vector3 halfExtents { get; set; }
        public Quaternion orientation { get; set; }
        public PhysicsScene physicsScene { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<OverlapBoxCommand> commands, NativeArray<ColliderHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<OverlapBoxCommand, ColliderHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<OverlapBoxCommand, ColliderHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleOverlapBoxBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        [FreeFunction("ScheduleOverlapBoxCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleOverlapBoxBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }

    [NativeHeader("Modules/Physics/BatchCommands/OverlapCapsuleCommand.h")]
    public struct OverlapCapsuleCommand
    {
        public OverlapCapsuleCommand(Vector3 point0, Vector3 point1, float radius, QueryParameters queryParameters)
        {
            this.point0 = point0;
            this.point1 = point1;
            this.radius = radius;
            this.queryParameters = queryParameters;
            this.physicsScene = Physics.defaultPhysicsScene;
        }

        public OverlapCapsuleCommand(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, QueryParameters queryParameters)
        {
            this.physicsScene = physicsScene;
            this.point0 = point0;
            this.point1 = point1;
            this.radius = radius;
            this.queryParameters = queryParameters;
        }

        public Vector3 point0 { get; set; }
        public Vector3 point1 { get; set; }
        public float radius { get; set; }
        public PhysicsScene physicsScene { get; set; }
        public QueryParameters queryParameters;

        public unsafe static JobHandle ScheduleBatch(NativeArray<OverlapCapsuleCommand> commands, NativeArray<ColliderHit> results, int minCommandsPerJob, int maxHits, JobHandle dependsOn = new JobHandle())
        {
            if (maxHits < 1)
            {
                Debug.LogWarning("maxHits should be greater than 0.");
                return new JobHandle();
            }
            else if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning("The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }

            var jobData = new BatchQueryJob<OverlapCapsuleCommand, ColliderHit>(commands, results);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), BatchQueryJobStruct<BatchQueryJob<OverlapCapsuleCommand, ColliderHit>>.Initialize(), dependsOn, ScheduleMode.Parallel);

            return ScheduleOverlapCapsuleBatch(ref scheduleParams, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(results), results.Length, minCommandsPerJob, maxHits);
        }

        [FreeFunction("ScheduleOverlapCapsuleCommandBatch", ThrowsException = true)]
        unsafe extern private static JobHandle ScheduleOverlapCapsuleBatch(ref JobsUtility.JobScheduleParameters parameters, void* commands, int commandLen, void* result, int resultLen, int minCommandsPerJob, int maxHits);
    }
}
