// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <undoc/>
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    readonly struct PhysicsNativeMethods
    {
        #region Write Transforms

        static TransformAccessArray[] s_WorldTransformAccessArrays = new TransformAccessArray[PhysicsConstants.MaxWorlds];

        /// <undoc/>
        [RequiredByNativeCode]
        static void CreateWorldTransformAccessArray(PhysicsWorld world, int capacity, int desiredJobCount)
        {
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
        static TransformAccessArray GetWorldTransformAccessArray(PhysicsWorld world)
        {
            var worldIndex = world.m_Index1 - 1;
            var transformAccessArray = s_WorldTransformAccessArrays[worldIndex];

            if (transformAccessArray.isCreated)
                return transformAccessArray;

            throw new InvalidOperationException($"Cannot access world transform access array for world {world}");
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static unsafe void WriteWorldTransforms(
            PhysicsWorld world,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformPlane transformPlane,
            int eventCount,
            bool transformTweening)
        {
            Profiler.BeginSample("PhysicsWorld.WriteTransforms");

            Profiler.BeginSample("PhysicsWorld.WriteTransforms.PopulateWorldTransforms");

            // Create the transform access array.
            var transformAccessArray = GetWorldTransformAccessArray(world);

            // Create the write-tweens array.
            var transformWriteTweensArray = new NativeArray<PhysicsBody.TransformWriteTween>(eventCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            // Populate the world transforms.
            // NOTE: The transform access array length determines the valid population of the transform write tweens here.
            int transformCount = PhysicsGlobal_PopulateWorldTransformWrite(world, new IntPtr(&transformAccessArray), transformWriteTweensArray.AsSpan());

            Profiler.EndSample();

            // Schedule the transform writes if we have any.
            if (transformCount > 0)
            {
                // Schedule the transform writes for the selected write mode.
                if (transformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D)
                {
                    Profiler.BeginSample("PhysicsWorld.WriteTransforms.FastWriteTransformsJob");
                    new FastWriteTransformsJob { TransformTweening = transformTweening, TransformWriteTweens = transformWriteTweensArray, TransformPlane = transformPlane }.Schedule(transformAccessArray).Complete();
                    Profiler.EndSample();
                }
                else if (transformWriteMode == PhysicsWorld.TransformWriteMode.Slow3D)
                {
                    Profiler.BeginSample("PhysicsWorld.WriteTransforms.Slow3DWriteTransformsJob");
                    new Slow3DWriteTransformsJob { TransformTweening = transformTweening, TransformWriteTweens = transformWriteTweensArray, TransformPlane = transformPlane }.Schedule(transformAccessArray).Complete();
                    Profiler.EndSample();
                }
                else
                {
                    throw new Exception("Invalid PhysicsWorld Transform Write Mode.");
                }

                // Set the transform write tweens (if active).
                if (transformTweening)
                    world.SetTransformWriteTweens(new Span<PhysicsBody.TransformWriteTween>(transformWriteTweensArray.GetUnsafeReadOnlyPtr(), transformCount));
            }

            // We're finished so dispose.
            transformWriteTweensArray.Dispose();

            Profiler.EndSample();
        }

        /// <undoc/>
        struct FastWriteTransformsJob : IJobParallelForTransform
        {
            public NativeArray<PhysicsBody.TransformWriteTween> TransformWriteTweens;
            [ReadOnly] public PhysicsWorld.TransformPlane TransformPlane;
            [ReadOnly] public bool TransformTweening;

            public void Execute(int index, TransformAccess transform)
            {
                // Skip if invalid transform.
                if (!transform.isValid)
                    return;

                // Fetch the transform tween.
                var transformTween = TransformWriteTweens[index];

                // Fetch the body transform.
                var physicsTransform = transformTween.physicsTransform;

                // Calculate the pose as per the selected TransformPlane.
                physicsTransform.GetPositionAndRotation(out var bodyPosition, out var bodyRotation);
                var newPosition = PhysicsMath.ToPosition3D(position: bodyPosition, reference: transformTween.positionFrom, transformPlane: TransformPlane);
                var newRotation = PhysicsMath.ToRotationFast3D(angle: bodyRotation.angle, transformPlane: TransformPlane);

                // Set the transform pose.
                transform.SetPositionAndRotation(newPosition, newRotation);

                // Finish if not Transform-Tweening or not Extrapolating.
                if (!TransformTweening || transformTween.transformWriteMode != PhysicsBody.TransformWriteMode.Extrapolate)
                    return;

                // Set-up the extrapolate tween.
                transformTween.positionFrom = newPosition;
                transformTween.rotationFrom = newRotation;

                // Assign the tween.
                TransformWriteTweens[index] = transformTween;
            }
        };

        /// <undoc/>
        struct Slow3DWriteTransformsJob : IJobParallelForTransform
        {
            public NativeArray<PhysicsBody.TransformWriteTween> TransformWriteTweens;
            [ReadOnly] public PhysicsWorld.TransformPlane TransformPlane;
            [ReadOnly] public bool TransformTweening;

            public void Execute(int index, TransformAccess transform)
            {
                // Skip if invalid transform.
                if (!transform.isValid)
                    return;

                // Fetch the transform tween.
                var transformTween = TransformWriteTweens[index];

                // Fetch the body transform.
                var physicsTransform = transformTween.physicsTransform;

                // Calculate the pose as per the selected TransformPlane.
                physicsTransform.GetPositionAndRotation(out var bodyPosition, out var bodyRotation);
                var newPosition = PhysicsMath.ToPosition3D(position: bodyPosition, reference: transformTween.positionFrom, transformPlane: TransformPlane);
                var newRotation = PhysicsMath.ToRotationSlow3D(angle: bodyRotation.angle, reference: transformTween.rotationFrom, transformPlane: TransformPlane);

                // Set the transform pose.
                transform.SetPositionAndRotation(newPosition, newRotation);

                // Finish if not Transform-Tweening or not Extrapolating.
                if (!TransformTweening || transformTween.transformWriteMode != PhysicsBody.TransformWriteMode.Extrapolate)
                    return;

                // Set-up the extrapolate tween.
                transformTween.positionFrom = newPosition;
                transformTween.rotationFrom = newRotation;

                // Assign the tween.
                TransformWriteTweens[index] = transformTween;
            }
        };

        #endregion

        #region Write Transform Tweens

        /// <undoc/>
        [RequiredByNativeCode]
        static unsafe void WriteTransformTweens(
            PhysicsWorld world,
            double lastSimulationTimestamp,
            float lastSimulationDeltaTime,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformPlane transformPlane,
            PhysicsBuffer transformWriteTweensBuffer)
        {
            // Finish if nothing to do.
            if (transformWriteMode == PhysicsWorld.TransformWriteMode.Off || transformWriteTweensBuffer.IsEmpty)
                return;

            Profiler.BeginSample("PhysicsWorld.WriteTransformTweens");

            // Fetch the transform write tweens.
            var transformWriteTweensArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray(transformWriteTweensBuffer.ToSpan<PhysicsBody.TransformWriteTween>(), Allocator.None);
            var transformWriteTweensSafety = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref transformWriteTweensArray, transformWriteTweensSafety);
            // Fetch a transform tween pointer (we must do this after the safety handle is assigned).
            var tweenCount = transformWriteTweensArray.Length;

            // Create the transform access array.
            var transformAccessArray = GetWorldTransformAccessArray(world);

            Profiler.BeginSample("PhysicsWorld.WriteTransformTweens.WriteTransformTweensJob");

            // To be safe, we should only handle this is the two are the same.
            if (tweenCount == transformAccessArray.length)
            {
                // Fetch the timings for interpolation and extrapolation.
                var timeSinceSimulation = (float)(Time.timeAsDouble - lastSimulationTimestamp);
                var interpolationTime = Mathf.Clamp01((float)timeSinceSimulation / lastSimulationDeltaTime);
                var extrapolationTime = timeSinceSimulation;

                // Schedule the transform tweens job.
                // NOTE: The body in any tween may have been destroyed therefore we should never refer to it. The transform is checked for validity.
                new WriteTransformTweensJob
                {
                    TransformWriteTweens = transformWriteTweensArray,
                    TransformWriteMode = transformWriteMode,
                    TransformPlane = transformPlane,
                    InterpolationTime = interpolationTime,
                    ExtrapolationTime = extrapolationTime

                }.Schedule(transformAccessArray).Complete();
            }

            Profiler.EndSample();

            AtomicSafetyHandle.Release(transformWriteTweensSafety);
            Profiler.EndSample();
        }

        /// <undoc/>
        struct WriteTransformTweensJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<PhysicsBody.TransformWriteTween> TransformWriteTweens;
            [ReadOnly] public PhysicsWorld.TransformWriteMode TransformWriteMode;
            [ReadOnly] public PhysicsWorld.TransformPlane TransformPlane;
            [ReadOnly] public float InterpolationTime;
            [ReadOnly] public float ExtrapolationTime;

            public void Execute(int index, TransformAccess transform)
            {
                // Skip if invalid transform.
                if (!transform.isValid)
                    return;

                // Fetch the transform tween.
                var transformTween = TransformWriteTweens[index];

                // Fetch the tween transform write mode.
                var tweenTransformWritemode = transformTween.transformWriteMode;

                // Interpolate?
                if (tweenTransformWritemode == PhysicsBody.TransformWriteMode.Interpolate)
                {
                    // Yes, so calculate target pose.
                    var positionFrom = transformTween.positionFrom;
                    var rotationFrom = transformTween.rotationFrom;
                    var physicsTransform = transformTween.physicsTransform;
                    var positionTo = PhysicsMath.ToPosition3D(position: physicsTransform.position, reference: positionFrom, transformPlane: TransformPlane);
                    var rotationTo = TransformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D ?
                        PhysicsMath.ToRotationFast3D(angle: physicsTransform.rotation.angle, transformPlane: TransformPlane) :
                        PhysicsMath.ToRotationSlow3D(angle: physicsTransform.rotation.angle, reference: rotationFrom, transformPlane: TransformPlane);

                    // Interpolation the pose.
                    var newPosition = Vector3.Lerp(positionFrom, positionTo, InterpolationTime);
                    var newRotation = Quaternion.Slerp(rotationFrom, rotationTo, InterpolationTime);

                    // Set the transform pose.
                    transform.SetPositionAndRotation(newPosition, newRotation);

                    return;
                }

                // Extrapolate?
                if (tweenTransformWritemode == PhysicsBody.TransformWriteMode.Extrapolate)
                {
                    // Yes, so calculate target pose.
                    var linearVelocity = transformTween.linearVelocity;
                    var transformedVelocity = PhysicsMath.Swizzle(position: new Vector3(linearVelocity.x * ExtrapolationTime, linearVelocity.y * ExtrapolationTime, 0.0f), transformPlane: TransformPlane);
                    var positionFrom = transformTween.positionFrom;
                    var rotationFrom = transformTween.rotationFrom;

                    // Extrapolate the pose.
                    var angularVelocity = transformTween.angularVelocity;
                    var newPosition = positionFrom + transformedVelocity;
                    var newRotation = PhysicsMath.AngularVelocityToQuaternion(angularVelocity: angularVelocity, deltaTime: ExtrapolationTime, transformPlane: TransformPlane) * rotationFrom;

                    // Set the transform pose.
                    transform.SetPositionAndRotation(newPosition, newRotation);
                }
            }
        };

        #endregion
    }
}
