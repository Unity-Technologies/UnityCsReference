// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Scripting;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <undoc/>
    [StructLayout(LayoutKind.Sequential)]
    readonly struct PhysicsTransformWriter
    {
        static readonly ProfilerMarker s_WriteTransformsMarker = new ProfilerMarker("PhysicsWorld.WriteTransforms");
        static readonly ProfilerMarker s_WriteTransformsCalculateWorldTransformsMarker = new ProfilerMarker("PhysicsWorld.WriteTransforms.CalculateWorldTransforms");
        static readonly ProfilerMarker s_WriteTransformsWriteTransformsParallelJobMarker = new ProfilerMarker("PhysicsWorld.WriteTransforms.WriteTransformsParallelJob");
        static readonly ProfilerMarker s_WriteTransformsWriteTransformsSequentialJobMarker = new ProfilerMarker("PhysicsWorld.WriteTransforms.WriteTransformsSequentialJob");
        static readonly ProfilerMarker s_WriteTransformsCustomMarker = new ProfilerMarker("PhysicsWorld.WriteTransforms.Custom");

        /// <undoc/>
        [RequiredByNativeCode]
        static unsafe void WriteWorldTransforms(
            PhysicsWorld world,
            PhysicsWorld.SimulationType simulationType,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformPlane transformPlane,
            PhysicsWorld.TransformPlaneCustom transfomPlaneCustom,
            PhysicsWorld.TransformTweenMode transformTweenMode
            )
        {
            using (s_WriteTransformsMarker.Auto())
            {
                // Create the transform access array.
                var isParallelTweening = transformTweenMode == PhysicsWorld.TransformTweenMode.Parallel;
                var transformAccessArray = isParallelTweening ? PhysicsTransformTweener.GetWorldTransformAccessArray(world) : default;
                var transformAccessArrayPtr = isParallelTweening ? new IntPtr(&transformAccessArray) : default;

                // Fill the world transforms.
                // NOTE: We can pass an empty transform-access-array here which will result in the transform-write tweens being sorted (by ascending transform-depth) for sequential writing.
                NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens;
                using (s_WriteTransformsCalculateWorldTransformsMarker.Auto())
                {
                    transformWriteTweens = PhysicsGlobal_CalculateWorldTransformWrite(world, transformPlane, transfomPlaneCustom, transformWriteMode, transformAccessArrayPtr).ToNativeArray<PhysicsBody.TransformWriteTween>();
                }

                // Do we have any transforms to write?
                if (transformWriteTweens.Length > 0)
                {
                    // Yes, so calculate if we should be calculate transform tweens.
                    var transformTweening = simulationType == PhysicsWorld.SimulationType.FixedUpdate;

                    // Are we parallel tweening?
                    if (isParallelTweening)
                    {
                        // Yes, so use the parallel job.
                        using (s_WriteTransformsWriteTransformsParallelJobMarker.Auto())
                        {
                            new WriteTransformsParallelJob { transformWriteTweens = transformWriteTweens, transformPlane = transformPlane, transformPlaneCustom = transfomPlaneCustom, transformTweening = transformTweening, fastWrite2D = transformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D }.Schedule(transformAccessArray).Complete();
                        }
                    }
                    else
                    {
                        // No, so use the sequential job.
                        using (s_WriteTransformsWriteTransformsSequentialJobMarker.Auto())
                        {
                            WriteTransformsSequentialTask(
                                ref transformWriteTweens,
                                transformPlane,
                                ref transfomPlaneCustom,
                                transformTweening,
                                transformWriteMode == PhysicsWorld.TransformWriteMode.Fast2D);
                        }
                    }

                    // Set the transform write tweens (if active).
                    if (transformTweening)
                        world.SetTransformWriteTweens(new Span<PhysicsBody.TransformWriteTween>(transformWriteTweens.GetUnsafeReadOnlyPtr(), transformWriteTweens.Length));
                }

                // We're finished so dispose.
                transformWriteTweens.Dispose();
            }
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static unsafe void WriteWorldTransformsCustom(
            System.Object transformWriteCallbackTarget,
            PhysicsWorld world,
            PhysicsWorld.SimulationType simulationType,
            PhysicsWorld.TransformWriteMode transformWriteMode,
            PhysicsWorld.TransformPlane transformPlane,
            PhysicsWorld.TransformPlaneCustom transfomPlaneCustom,
            PhysicsWorld.TransformTweenMode transformTweenMode)
        {
            using (s_WriteTransformsCustomMarker.Auto())
            {
                // Create the transform access array.
                var isParallelTweening = transformTweenMode == PhysicsWorld.TransformTweenMode.Parallel;
                var transformAccessArray = isParallelTweening ? PhysicsTransformTweener.GetWorldTransformAccessArray(world) : default;
                var transformAccessArrayPtr = isParallelTweening ? new IntPtr(&transformAccessArray) : default;

                // Fill the world transforms.
                // NOTE: We can pass an empty transform-access-array here which will result in the transform-write tweens being sorted (by ascending transform-depth) for sequential writing.
                NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens;
                using (s_WriteTransformsCalculateWorldTransformsMarker.Auto())
                {
                    transformWriteTweens = PhysicsGlobal_CalculateWorldTransformWrite(world, transformPlane, transfomPlaneCustom, transformWriteMode, transformAccessArrayPtr).ToNativeArray<PhysicsBody.TransformWriteTween>();
                }

                // Do we have any transforms to write?
                if (transformWriteTweens.Length > 0)
                {
                    // Yes, so calculate if we should be calculate transform tweens.
                    var transformTweening = simulationType == PhysicsWorld.SimulationType.FixedUpdate;

                    // Fetch the callback target.
                    var callbackTarget = transformWriteCallbackTarget as PhysicsCallbacks.ITransformWriteCallback;
                    if (callbackTarget != null)
                    {
                        // Create the event.
                        var transformWriteEvent = new PhysicsEvents.TransformWriteEvent(
                            world,
                            simulationType,
                            transformPlane,
                            transfomPlaneCustom,
                            transformTweenMode,
                            ref transformWriteTweens);

                        // Send the event.
                        callbackTarget.OnTransformWrite(transformWriteEvent);

                        // Set the transform write tweens (if active).
                        if (transformTweening)
                            world.SetTransformWriteTweens(new Span<PhysicsBody.TransformWriteTween>(transformWriteTweens.GetUnsafeReadOnlyPtr(), transformWriteTweens.Length));
                    }
                }

                // Dispose of the tweens.
                if (transformWriteTweens.IsCreated)
                    transformWriteTweens.Dispose();
            }
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static unsafe void WriteWorldTransformsGetPhysicsTransformPose3D(
            PhysicsBody.TransformWriteTween transformWriteTween,
            PhysicsWorld.TransformPlane transformPlane,
            PhysicsWorld.TransformPlaneCustom transfomPlaneCustom,
            bool fast2D,
            out Vector3 position, out Quaternion rotation) => transformWriteTween.GetPose(transformPlane, ref transfomPlaneCustom, fast2D, out position, out rotation);

        #region Writers

        /// <undoc/>
        struct WriteTransformsParallelJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens;
            [ReadOnly] public PhysicsWorld.TransformPlane transformPlane;
            [ReadOnly] public PhysicsWorld.TransformPlaneCustom transformPlaneCustom;
            [ReadOnly] public bool transformTweening;
            [ReadOnly] public bool fastWrite2D;

            public void Execute(int index, TransformAccess transformAccess)
            {
                // Skip if invalid transform.
                if (!transformAccess.isValid)
                    return;

                // Fetch the transform tween.
                var transformTween = transformWriteTweens[index];

                // Fetch the body transform.
                var physicsTransform = transformTween.physicsTransform;

                Vector3 newPosition;
                Quaternion newRotation;

                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    // Fetch the body position and rotation.
                    physicsTransform.GetPositionAndRotation(out var bodyPosition, out var bodyRotation);

                    // Calculate the pose as per the selected TransformPlane.
                    newPosition = PhysicsMath.ToPosition3D(position: bodyPosition, reference: transformTween.positionFrom, transformPlane: transformPlane);
                    newRotation = fastWrite2D ?
                        PhysicsMath.ToRotationFast3D(angle: bodyRotation.radians, transformPlane: transformPlane) :
                        PhysicsMath.ToRotationSlow3D(angle: bodyRotation.radians, reference: transformTween.rotationFrom, transformPlane: transformPlane);
                }
                else
                {
                    // Custom plane projection.
                    transformPlaneCustom.PlaneProjection(in physicsTransform, out newPosition, out newRotation);
                }

                // Set the transform pose.
                PhysicsWorld.SetTransformAccess(ref transformAccess, ref newPosition, ref newRotation);
            }
        };

        /// <undoc/>
        static void WriteTransformsSequentialTask(
            ref NativeArray<PhysicsBody.TransformWriteTween> transformWriteTweens,
            PhysicsWorld.TransformPlane transformPlane,
            ref PhysicsWorld.TransformPlaneCustom transformPlaneCustom,
            bool transformTweening,
            bool fastWrite2D)
        {
            var tweenCount = transformWriteTweens.Length;
            for (var i = 0; i < tweenCount; ++i)
            {
                // Fetch the transform tween.
                var transformTween = transformWriteTweens[i];

                // Fetch the transform.
                var transform = transformTween.transform;
                if (transform == null)
                    continue;

                // Fetch the body transform.
                var physicsTransform = transformTween.physicsTransform;

                Vector3 newPosition;
                Quaternion newRotation;

                // Handle non-custom plane projection.
                if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                {
                    // Calculate the pose as per the selected TransformPlane.
                    physicsTransform.GetPositionAndRotation(out var bodyPosition, out var bodyRotation);
                    newPosition = PhysicsMath.ToPosition3D(position: bodyPosition, reference: transformTween.positionFrom, transformPlane: transformPlane);
                    newRotation = fastWrite2D ?
                        PhysicsMath.ToRotationFast3D(angle: bodyRotation.radians, transformPlane: transformPlane) :
                        PhysicsMath.ToRotationSlow3D(angle: bodyRotation.radians, reference: transformTween.rotationFrom, transformPlane: transformPlane);
                }
                else
                {
                    // Custom plane projection.
                    transformPlaneCustom.PlaneProjection(in physicsTransform, out newPosition, out newRotation);
                }

                // Set the transform pose.
                PhysicsWorld.SetTransform(transform, ref newPosition, ref newRotation);
            }
        }

        #endregion
    }
}
