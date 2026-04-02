// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <undoc/>
    [StructLayout(LayoutKind.Sequential)]
    readonly struct PhysicsTransformTweener
    {
        [RequiredByNativeCode]
        static unsafe void WriteTransformTweens(
            bool syncInterpolation,
            PhysicsWorld world,
            float interpolationTime,
            float extrapolationTime,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformTweenMode transformTweenMode,
            PhysicsWorld.TransformPlane transformPlane,
            PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
            PhysicsBuffer transformWriteTweensBuffer)
        {
            // Finish if nothing to do.
            if (transformWriteTweensBuffer.isEmpty ||
                transformWriteMode == PhysicsWorld.TransformWriteMode.Off ||
                transformTweenMode == PhysicsWorld.TransformTweenMode.Off)
                return;

            Profiler.BeginSample("PhysicsWorld.WriteTransformTweens");

            // Fetch the transform write tweens.
            var transformWriteTweens = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray(transformWriteTweensBuffer.ToSpan<PhysicsBody.TransformWriteTween>(), Allocator.None);
            var transformWriteTweensSafety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref transformWriteTweens, transformWriteTweensSafety);
            // Parallel (Transform Access Array method).
            if (transformTweenMode == PhysicsWorld.TransformTweenMode.Parallel)
            {
                // Fetch a transform tween pointer (we must do this after the safety handle is assigned).
                var tweenCount = transformWriteTweens.Length;

                // Create the transform access array.
                var transformAccessArray = GetWorldTransformAccessArray(world);

                Profiler.BeginSample("PhysicsWorld.WriteTransformTweens.Parallel");

                // To be safe, we should only handle this is the two are the same.
                if (tweenCount == transformAccessArray.length)
                {
                    // Schedule the transform tweens job.
                    // NOTE: The body in any tween may have been destroyed therefore we should never refer to it. The transform is checked for validity.
                    new WriteTransformTweensParallelJob
                    {
                        interpolationTime = interpolationTime,
                        extrapolationTime = extrapolationTime,
                        transformWriteMode = transformWriteMode,
                        transformPlane = transformPlane,
                        transformPlaneCustom = transformPlaneCustom,
                        transformWriteTweens = transformWriteTweens,
                        syncInterpolation = syncInterpolation

                    }.Schedule(transformAccessArray).Complete();
                }

                Profiler.EndSample();
            }
            // Sequential (main-thread method).
            else if (transformTweenMode == PhysicsWorld.TransformTweenMode.Sequential)
            {
                Profiler.BeginSample("PhysicsWorld.WriteTransformTweens.Sequential");

                WriteTransformTweensTask(
                    syncInterpolation,
                    interpolationTime,
                    extrapolationTime,
                    transformWriteMode,
                    transformPlane,
                    ref transformPlaneCustom,
                    ref transformWriteTweens);

                Profiler.EndSample();
            }

            AtomicSafetyHandle.Release(transformWriteTweensSafety);
            Profiler.EndSample();
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static unsafe void WriteTransformTweensCustom(
            System.Object transformWriteCallbackTarget,
            PhysicsWorld world,
            float interpolationTime,
            float extrapolationTime,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformPlane transformPlane,
            PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
            PhysicsBuffer transformWriteTweensBuffer)
        {
            Profiler.BeginSample("PhysicsWorld.WriteTransformTweens");

            // Fetch the transform write tweens.
            var transformWriteTweens = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray(transformWriteTweensBuffer.ToSpan<PhysicsBody.TransformWriteTween>(), Allocator.None);
            var transformWriteTweensSafety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref transformWriteTweens, transformWriteTweensSafety);
            // Fetch the callback target.
            var callbackTarget = transformWriteCallbackTarget as PhysicsCallbacks.ITransformWriteCallback;
            if (callbackTarget != null)
            {
                Profiler.BeginSample("PhysicsWorld.WriteTransformTweens.Custom");

                // Create the event.
                var transformTweenWriteEvent = new PhysicsEvents.TransformTweenWriteEvent(
                    world,
                    interpolationTime,
                    extrapolationTime,
                    transformPlane,
                    transformPlaneCustom,
                    ref transformWriteTweens);

                // Send the event.
                callbackTarget.OnTransformTweenWrite(transformTweenWriteEvent);

                Profiler.EndSample();
            }

            AtomicSafetyHandle.Release(transformWriteTweensSafety);
            Profiler.EndSample();
        }

        #region Writers

        /// <undoc/>
        struct WriteTransformTweensParallelJob : IJobParallelForTransform
        {
            [ReadOnly] public float interpolationTime;
            [ReadOnly] public float extrapolationTime;
            [ReadOnly] public PhysicsWorld.TransformWriteMode transformWriteMode;
            [ReadOnly] public PhysicsWorld.TransformPlane transformPlane;
            [ReadOnly] public PhysicsWorld.TransformPlaneCustom transformPlaneCustom;
            [ReadOnly] public NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens;
            [ReadOnly] public bool syncInterpolation;

            public void Execute(int index, TransformAccess transformAccess)
            {
                // Skip if invalid transform.
                if (!transformAccess.isValid)
                    return;

                // Fetch the transform tween.
                var transformTween = transformWriteTweens[index];

                // Skip if invalid body.
                if (!transformTween.body.isValid)
                    return;

                Vector3 newPosition;
                Quaternion newRotation;

                // Fetch the tween transform write mode.
                var tweenTransformWritemode = transformTween.transformWriteMode;

                // Interpolate?
                if (tweenTransformWritemode == PhysicsBody.TransformWriteMode.Interpolate)
                {
                    // Yes, so calculate target pose.
                    var positionFrom = transformTween.positionFrom;
                    var rotationFrom = transformTween.rotationFrom;

                    // Fetch the body transform.
                    var physicsTransform = transformTween.physicsTransform;

                    Vector3 positionTo;
                    Quaternion rotationTo;

                    // Handle non-custom plane projection.
                    if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                    {
                        positionTo = PhysicsMath.ToPosition3D(position: physicsTransform.position, reference: positionFrom, transformPlane: transformPlane);
                        rotationTo = transformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D ?
                            PhysicsMath.ToRotationFast3D(angle: physicsTransform.rotation.radians, transformPlane: transformPlane) :
                            PhysicsMath.ToRotationSlow3D(angle: physicsTransform.rotation.radians, reference: rotationFrom, transformPlane: transformPlane);
                    }
                    else
                    {
                        // Custom plane projection.
                        transformPlaneCustom.PlaneProjection(in physicsTransform, out positionTo, out rotationTo);
                    }

                    // Interpolate the pose.
                    if (syncInterpolation || interpolationTime >= 1.0f)
                    {
                        newPosition = positionTo;
                        newRotation = rotationTo;
                    }
                    else
                    {
                        newPosition = Vector3.Lerp(positionFrom, positionTo, interpolationTime);
                        newRotation = Quaternion.Slerp(rotationFrom, rotationTo, interpolationTime);
                    }

                    // Set the transform pose.
                    PhysicsWorld.SetTransformAccess(ref transformAccess, ref newPosition, ref newRotation);

                    return;
                }

                // Finish if sync interpolation or not extrapolating.
                if (syncInterpolation || tweenTransformWritemode != PhysicsBody.TransformWriteMode.Extrapolate)
                    return;
                    
                // Fetch the tween velocities.
                var linearVelocity = transformTween.linearVelocity;
                var angularVelocity = transformTween.angularVelocity;

                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    var transformedVelocity = PhysicsMath.Swizzle(position: new Vector3(linearVelocity.x * extrapolationTime, linearVelocity.y * extrapolationTime, 0.0f), transformPlane: transformPlane);
                    var positionFrom = transformTween.positionFrom;
                    var rotationFrom = transformTween.rotationFrom;

                    // Extrapolate the pose.
                    newPosition = positionFrom + transformedVelocity;
                    newRotation = PhysicsMath.AngularVelocityToQuaternion(angularVelocity: angularVelocity, deltaTime: extrapolationTime, transformPlane: transformPlane) * rotationFrom;
                }
                else
                {
                    // Fetch the body transform.
                    var physicsTransform = transformTween.physicsTransform;

                    // Calculate the new transform.
                    var newTransform = new PhysicsTransform
                    {
                        position = physicsTransform.position + linearVelocity * extrapolationTime,
                        rotation = physicsTransform.rotation.IntegrateRotation(angularVelocity * extrapolationTime)
                    };

                    // Custom plane projection.
                    transformPlaneCustom.PlaneProjection(in newTransform, out newPosition, out newRotation);
                }

                // Set the transform pose.
                PhysicsWorld.SetTransformAccess(ref transformAccess, ref newPosition, ref newRotation);
            }
        }

        /// <undoc/>
        static void WriteTransformTweensTask(
            bool syncInterpolation,
            float interpolationTime,
            float extrapolationTime,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformPlane transformPlane,
            ref PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
            ref NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens)
        {
            var tweenCount = transformWriteTweens.Length;
            for (var i = 0; i < tweenCount; ++i)
            {
                // Fetch the transform tween.
                var transformTween = transformWriteTweens[i];

                // Skip if invalid body.
                if (!transformTween.body.isValid)
                    continue;

                // Fetch the transform.
                var transform = transformTween.transform;
                if (transform == null)
                    continue;

                Vector3 newPosition;
                Quaternion newRotation;

                // Fetch the tween transform write mode.
                var tweenTransformWritemode = transformTween.transformWriteMode;

                // Interpolate?
                if (tweenTransformWritemode == PhysicsBody.TransformWriteMode.Interpolate)
                {
                    // Yes, so calculate target pose.
                    var positionFrom = transformTween.positionFrom;
                    var rotationFrom = transformTween.rotationFrom;

                    // Fetch the body transform.
                    var physicsTransform = transformTween.physicsTransform;

                    Vector3 positionTo;
                    Quaternion rotationTo;

                    // Handle non-custom plane projection.
                    if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                    {
                        positionTo = PhysicsMath.ToPosition3D(position: physicsTransform.position, reference: positionFrom, transformPlane: transformPlane);
                        rotationTo = transformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D ?
                            PhysicsMath.ToRotationFast3D(angle: physicsTransform.rotation.radians, transformPlane: transformPlane) :
                            PhysicsMath.ToRotationSlow3D(angle: physicsTransform.rotation.radians, reference: rotationFrom, transformPlane: transformPlane);
                    }
                    else
                    {
                        // Custom plane projection.
                        transformPlaneCustom.PlaneProjection(in physicsTransform, out positionTo, out rotationTo);
                    }

                    // Interpolate the pose.
                    if (syncInterpolation || interpolationTime >= 1.0f)
                    {
                        newPosition = positionTo;
                        newRotation = rotationTo;
                    }
                    else
                    {
                        // Interpolation the pose.
                        newPosition = Vector3.Lerp(positionFrom, positionTo, interpolationTime);
                        newRotation = Quaternion.Slerp(rotationFrom, rotationTo, interpolationTime);
                    }

                    // Set the transform pose.
                    PhysicsWorld.SetTransform(transform, ref newPosition, ref newRotation);

                    continue;
                }

                // Skip if sync interpolation or not extrapolating.
                if (syncInterpolation || tweenTransformWritemode != PhysicsBody.TransformWriteMode.Extrapolate)
                    continue;

                // Fetch the tween velocities.
                var linearVelocity = transformTween.linearVelocity;
                var angularVelocity = transformTween.angularVelocity;

                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    var transformedVelocity = PhysicsMath.Swizzle(position: new Vector3(linearVelocity.x * extrapolationTime, linearVelocity.y * extrapolationTime, 0.0f), transformPlane: transformPlane);
                    var positionFrom = transformTween.positionFrom;
                    var rotationFrom = transformTween.rotationFrom;

                    // Extrapolate the pose.
                    newPosition = positionFrom + transformedVelocity;
                    newRotation = PhysicsMath.AngularVelocityToQuaternion(angularVelocity: angularVelocity, deltaTime: extrapolationTime, transformPlane: transformPlane) * rotationFrom;
                }
                else
                {
                    // Fetch the body transform.
                    var physicsTransform = transformTween.physicsTransform;

                    // Calculate the new transform.
                    var newTransform = new PhysicsTransform
                    {
                        position = physicsTransform.position + linearVelocity * extrapolationTime,
                        rotation = physicsTransform.rotation.IntegrateRotation(angularVelocity * extrapolationTime)
                    };

                    // Custom plane projection.
                    transformPlaneCustom.PlaneProjection(in newTransform, out newPosition, out newRotation);
                }

                // Set the transform pose.
                PhysicsWorld.SetTransform(transform, ref newPosition, ref newRotation);
            }
        }

        #endregion

        #region Transform Access Arrays

        static TransformAccessArray[] s_WorldTransformAccessArrays = null;

        /// <undoc/>
        [RequiredByNativeCode]
        static void CreateWorldTransformAccessArray(PhysicsWorld world, int capacity, int desiredJobCount)
        {
            // Create the transform access arrays if not available.
            if (s_WorldTransformAccessArrays == null)
                s_WorldTransformAccessArrays = new TransformAccessArray[PhysicsWorld.maximumWorldsAllocated];

            // Fetch the transform access array.
            var worldIndex = world.m_Index1 - 1;
            var transformAccessArray = s_WorldTransformAccessArrays[worldIndex];

            // Dispose if already created.
            if (transformAccessArray.isCreated)
                transformAccessArray.Dispose();

            // Create the transform access array.
            transformAccessArray = new TransformAccessArray(capacity: capacity, desiredJobCount: desiredJobCount);
            s_WorldTransformAccessArrays[worldIndex] = transformAccessArray;
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void DestroyWorldTransformAccessArray(PhysicsWorld world)
        {
            // Finish if no transform access arrays are available or the world is invalid.
            if (s_WorldTransformAccessArrays == null || !world.isValid)
                return;

            // Fetch the transform access array.
            var worldIndex = world.m_Index1 - 1;
            var transformAccessArray = s_WorldTransformAccessArrays[worldIndex];

            // Dispose if already created.
            if (transformAccessArray.isCreated)
                transformAccessArray.Dispose();

            // Set the disposed transform access array.
            s_WorldTransformAccessArrays[worldIndex] = default;
        }

        /// <undoc/>
        internal static TransformAccessArray GetWorldTransformAccessArray(PhysicsWorld world)
        {
            // Create the transform access arrays if not available.
            if (s_WorldTransformAccessArrays == null)
                s_WorldTransformAccessArrays = new TransformAccessArray[PhysicsWorld.maximumWorldsAllocated];

            var worldIndex = world.m_Index1 - 1;
            var transformAccessArray = s_WorldTransformAccessArrays[worldIndex];

            if (transformAccessArray.isCreated)
                return transformAccessArray;

            throw new InvalidOperationException($"Cannot access world transform access array for world {world}");
        }

        #endregion
    }
}
