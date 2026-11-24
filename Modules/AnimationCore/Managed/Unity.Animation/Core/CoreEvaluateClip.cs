// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.IntegerTime;
using UnityEngine;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    static public partial class Core
    {
        internal struct ClipKeyframe
        {
            public int Left;
            public int Right;
            public float Weight;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ClipKeyframe(float duration, float frameRate, int curveCount, DiscreteTime time)
            {
                var sampleIndex = math.clamp((float)time, 0, duration) * frameRate;
                Left = (int)math.floor(sampleIndex) * curveCount;
                Right = (int)math.ceil(sampleIndex) * curveCount;
                Weight = math.frac(sampleIndex);
            }
        }

        // time is a playback time from the start of the clip, mapped with Clip.ToTimerState(DiscreteTime)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static void SampleAnimationClipOverwrite(BlobAssetReference<Clip> clip, BlobAssetReference<ClipInstance> clipInstance,
            ref ClipSamplingCache cache,
            DiscreteTime time, ref AnimationStream stream)
        {
            var timer = clip.Value.ToTimerState(time);
            SampleAnimationClipOverwrite(clip, clipInstance, ref cache.Memory, in timer, ref stream);
        }

        // The clip curves are sampled at TimerState.Time as it is, because a clip timer from
        // Clip.ToTimerState(DiscreteTime) already holds the time on the clip curves with the cycle offset. The Loop
        // Pose correction applies when the wrap mode of the timer is Loop (Default resolves to the Loop Time of the
        // clip), and uses the position of TimerState.Time in TimerState.Range.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static void SampleAnimationClipOverwrite(BlobAssetReference<Clip> clip, BlobAssetReference<ClipInstance> clipInstance,
            ref ClipSamplingCache cache,
            in TimerState timer, ref AnimationStream stream)
        {
            SampleAnimationClipOverwrite(clip, clipInstance, ref cache.Memory, in timer, ref stream);
        }

        // Returns false if the clip has no curve data or the cache is too small, and then writes nothing to the stream
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static bool SampleAnimationClipOverwrite(BlobAssetReference<Clip> clip, BlobAssetReference<ClipInstance> clipInstance,
            ref StreamedClipMemory memory,
            in TimerState timer, ref AnimationStream stream)
        {
            return SampleAnimationClipOverwrite(clip, clipInstance, ref memory, in timer, ref stream, false);
        }

        // Removes the root motion from the root motion bone before Loop Pose, as AnimationClipPlayable::EvaluateValues
        // calls ComputeRootMotionValues before ValueArrayLoop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static void SampleAnimationClipExtractingRootMotion(BlobAssetReference<Clip> clip, BlobAssetReference<ClipInstance> clipInstance,
            ref StreamedClipMemory memory,
            in TimerState timer, ref AnimationStream stream)
        {
            SampleAnimationClipOverwrite(clip, clipInstance, ref memory, in timer, ref stream, true);
        }

        static unsafe bool SampleAnimationClipOverwrite(BlobAssetReference<Clip> clip, BlobAssetReference<ClipInstance> clipInstance,
            ref StreamedClipMemory memory,
            in TimerState timer, ref AnimationStream stream, bool extractRootMotionBone)
        {
            stream.ValidateIsNotNull();
            stream.ValidateIsWritable();
            stream.ValidateCompatibility(clipInstance);

            // Resolve the ClipCurves* via the blob's OffsetPtr — zero copy.
            var muscleClipPtr = (Clip*)clip.GetUnsafePtr();
            if (muscleClipPtr->m_Clip.IsNull)
                return false;

            var curves = (ClipCurves*)muscleClipPtr->m_Clip.Get();

            ClipSamplingCacheData.ReinitializeIfClipChanged(ref memory, curves);
            ClipSamplingCache.ValidateCacheForSampling(in memory, curves);

            // Checked in all builds: a cache initialized for a smaller clip would write past the end of its output
            if (!ClipSamplingCache.HasOutputCapacity(in memory, curves))
            {
                ClipSamplingCacheData.LogOutputCapacityErrorOnce(ref memory);
                return false;
            }

            var duration = (float)timer.Range.Duration;
            var normalizedTime = duration > 0f ? math.saturate((float)(timer.Time - timer.Range.Start) / duration) : 0f;
            // Loop Pose follows the wrap mode of the timer, like Mecanim follows the (overridable) Loop Time of the playable
            var loopDeltas = muscleClipPtr->ResolveWrapMode(timer.WrapMode) == WrapMode.Loop && muscleClipPtr->m_LoopBlend != 0 ?
                (ValueDelta*)muscleClipPtr->m_ValueArrayDelta.Get() :
                null;

            if (!extractRootMotionBone || clipInstance.Value.RootMotionSamplingData.ExtractsBone == 0)
            {
                SampleAnimationClip(curves, ref clipInstance.Value, (float)timer.Time, loopDeltas, normalizedTime, ref memory, ref stream);
                return true;
            }

            // The previous extraction rewrote the whole bone. Its channels that the clip doesn't animate restart from the
            // rig default, which the root transform assumes, or each sample would move the bone further
            var boneIndex = stream.Rig.RootMotionBoneIndex;
            var defaults = AnimationStream.FromDefaultValues(stream.RigDefinition);
            stream.SetLocalToParentTranslation(boneIndex, defaults.GetLocalToParentTranslation(boneIndex));
            stream.SetLocalToParentRotation(boneIndex, defaults.GetLocalToParentRotation(boneIndex));
            stream.SetLocalToParentScale(boneIndex, defaults.GetLocalToParentScale(boneIndex));

            SampleAnimationClip(curves, ref clipInstance.Value, (float)timer.Time, null, normalizedTime, ref memory, ref stream);

            var rootX = SampleRootMotionTransform(ref *muscleClipPtr, ref clipInstance.Value, ref memory, (float)timer.Time);
            stream.ExtractRootMotionBone(rootX.pos, rootX.rot);

            if (loopDeltas != null)
                ApplyLoopPose(ref clipInstance.Value, loopDeltas, normalizedTime, ref stream);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static RigidTransform SampleAnimationClipRootTransform(
            BlobAssetReference<Clip> clip,
            int rootTranslationIndex,
            int rootRotationIndex,
            ref ClipSamplingCache cache,
            float time,
            RigidTransform defaultX,
            int rootRotationEulerOrder = -1)
        {
            var root = defaultX;
            SampleAnimationClipRootTransformBursted(ref clip.Value, rootTranslationIndex, rootRotationIndex, rootRotationEulerOrder, ref cache.Memory, time, ref root);
            return root;
        }

        [BurstCompile]
        internal unsafe static void SampleAnimationClipRootTransformBursted(
            ref Clip clip,
            int rootTranslationIndex,
            int rootRotationIndex,
            int rootRotationEulerOrder,
            ref StreamedClipMemory memory,
            float time,
            ref RigidTransform rootTransform)
        {
            var muscleClipPtr = (Clip*)UnsafeUtility.AddressOf(ref clip);
            var curves = (ClipCurves*)muscleClipPtr->m_Clip.Get();

            ClipSamplingCacheData.ReinitializeRootMotionIfClipChanged(ref memory, curves);
            ClipSamplingCache.ValidateRootMotionCache(in memory, curves, rootTranslationIndex, rootRotationIndex, rootRotationEulerOrder >= 0 ? 3 : 4);

            if (rootTranslationIndex >= 0)
            {
                rootTransform.pos = new float3(
                    EvaluateClipAtIndex(curves, ref memory, rootTranslationIndex + 0, time),
                    EvaluateClipAtIndex(curves, ref memory, rootTranslationIndex + 1, time),
                    EvaluateClipAtIndex(curves, ref memory, rootTranslationIndex + 2, time));
            }

            if (rootRotationIndex >= 0 && rootRotationEulerOrder >= 0)
            {
                // Euler curves are in degrees, as in the pose sampling of SampleAnimationClip
                var euler = new float3(
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 0, time),
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 1, time),
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 2, time));
                rootTransform.rot = quaternion.Euler(math.radians(euler), (math.RotationOrder)rootRotationEulerOrder);
            }
            else if (rootRotationIndex >= 0)
            {
                rootTransform.rot = math.normalize(new quaternion(
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 0, time),
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 1, time),
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 2, time),
                    EvaluateClipAtIndex(curves, ref memory, rootRotationIndex + 3, time)));
            }
        }

        // The times are playback times from the start of the clip, as in the DiscreteTime overload of
        // SampleAnimationClipOverwrite. Both are mapped with Clip.ToTimerState(DiscreteTime). A looping clip with a wrap
        // mode other than PingPong applies the cycle offset of the clip and counts its own wraps. A PingPong caller
        // gets no cycle offset, because the timer already folds the time.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static RigidTransform SampleAnimationClipRootMotion(
            BlobAssetReference<Clip> clip,
            BlobAssetReference<ClipInstance> clipInstance,
            ref ClipSamplingCache prevSamplingCache,
            ref ClipSamplingCache curSamplingCache,
            DiscreteTime previousTime,
            DiscreteTime currentTime,
            WrapMode wrapMode,
            int loopCount)
        {
            var output = MotionOutput.Identity;
            SampleAnimationClipRootMotionBursted(ref clip.Value, ref clipInstance.Value, ref prevSamplingCache.Memory, ref curSamplingCache.Memory, ref previousTime, ref currentTime, wrapMode, loopCount, ref output);
            return output.RootMotion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static RigidTransform SampleAnimationClipRootMotion(
            BlobAssetReference<Clip> clip,
            BlobAssetReference<ClipInstance> clipInstance,
            ref ClipSamplingCache prevSamplingCache,
            ref ClipSamplingCache curSamplingCache,
            in TimerState timer)
        {
            return SampleAnimationClipMotion(clip, clipInstance, ref prevSamplingCache.Memory, ref curSamplingCache.Memory, in timer).RootMotion;
        }

        // TimerState.PreviousTime and TimerState.Time are sampled as they are, because a clip timer from
        // Clip.ToTimerState(DiscreteTime) already holds the time on the clip curves. The clip cycles crossed are the
        // difference between LoopCount and PreviousLoopCount. The previous memory samples PreviousTime, the current
        // one samples Time. Each only reads forward, so they can't share one.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static MotionOutput SampleAnimationClipMotion(
            BlobAssetReference<Clip> clip,
            BlobAssetReference<ClipInstance> clipInstance,
            ref StreamedClipMemory prevMemory,
            ref StreamedClipMemory curMemory,
            in TimerState timer)
        {
            var output = MotionOutput.Identity;
            var timerCopy = timer;
            SampleAnimationClipRootMotionTimerBursted(ref clip.Value, ref clipInstance.Value, ref prevMemory, ref curMemory, ref timerCopy, ref output);
            return output;
        }

        [BurstCompile]
        unsafe static void SampleAnimationClipRootMotionBursted(
            ref Clip clip,
            ref ClipInstance clipInstance,
            ref StreamedClipMemory prevMemory,
            ref StreamedClipMemory curMemory,
            ref DiscreteTime previousTime,
            ref DiscreteTime currentTime,
            WrapMode wrapMode,
            int loopCount,
            ref MotionOutput output)
        {
            var timer = ToRootMotionTimer(ref clip, previousTime, currentTime, wrapMode, loopCount);
            SampleAnimationClipRootMotionTimerBursted(ref clip, ref clipInstance, ref prevMemory, ref curMemory, ref timer, ref output);
        }

        [BurstCompile]
        unsafe static void SampleAnimationClipRootMotionTimerBursted(
            ref Clip clip,
            ref ClipInstance clipInstance,
            ref StreamedClipMemory prevMemory,
            ref StreamedClipMemory curMemory,
            ref TimerState timer,
            ref MotionOutput output)
        {
            var samplingData = clipInstance.RootMotionSamplingData;
            if (!samplingData.HasRootMotion)
            {
                output = MotionOutput.Identity;
                return;
            }

            var curX = SampleRootMotionTransform(ref clip, ref clipInstance, ref curMemory, (float)timer.Time);
            var prevX = SampleRootMotionTransform(ref clip, ref clipInstance, ref prevMemory, (float)timer.PreviousTime);

            output.RootMotion = ComputeRootMotionDelta(in samplingData, prevX, curX, timer.WrapMode, timer.LoopCount - timer.PreviousLoopCount);
            output.RootTransform = curX;
        }

        // Returns the transform whose motion is the root motion, at a time on the clip curves
        internal static RigidTransform SampleRootMotionTransform(ref Clip clip, ref ClipInstance clipInstance, ref StreamedClipMemory memory, float time)
        {
            var x = SampleRootMotionSourceTransform(ref clip, ref clipInstance, ref memory, time);
            return ProcessRootMotionTransform(clipInstance.RootMotionSamplingData, x);
        }

        // Returns the transform of the root motion curves, before the settings of their source
        internal unsafe static RigidTransform SampleRootMotionSourceTransform(ref Clip clip, ref ClipInstance clipInstance, ref StreamedClipMemory memory, float time)
        {
            var muscleClipPtr = (Clip*)UnsafeUtility.AddressOf(ref clip);
            var curves = (ClipCurves*)muscleClipPtr->m_Clip.Get();

            ClipSamplingCacheData.ReinitializeRootMotionIfClipChanged(ref memory, curves);
            ValidateRootMotionCache(in memory, curves, ref clipInstance);

            ref var nodes = ref clipInstance.m_RootMotionNodes;
            var bone = SampleRootMotionNode(curves, ref memory, ref nodes[0], time);
            var translation = bone.t;
            var rotation = bone.r;
            for (var i = 1; i < nodes.Length; i++)
            {
                var parent = SampleRootMotionNode(curves, ref memory, ref nodes[i], time);
                translation = MathUtility.transform(parent, translation);
                rotation = math.mul(parent.r, MathUtility.scaleMulQuat(parent.s, rotation));
            }

            return new RigidTransform(math.normalizesafe(rotation, quaternion.identity), translation);
        }

        // Missing curves keep the default value of their component, as in mecanim::animation::GetClipX
        static unsafe TRS SampleRootMotionNode(ClipCurves* curves, ref StreamedClipMemory memory, ref RootMotionNode node, float time)
        {
            var translation = node.DefaultTranslation;
            for (var i = 0; i < 3; i++)
            {
                var curveIndex = node.TranslationCurves[i];
                if (curveIndex >= 0)
                    translation[i] = EvaluateClipAtIndex(curves, ref memory, curveIndex, time);
            }

            quaternion rotation;
            if (node.RotationEulerOrder >= 0)
            {
                var euler = float3.zero;
                for (var i = 0; i < 3; i++)
                {
                    var curveIndex = node.RotationCurves[i];
                    if (curveIndex >= 0)
                        euler[i] = EvaluateClipAtIndex(curves, ref memory, curveIndex, time);
                }

                // Euler curves are in degrees, as in the pose sampling of SampleAnimationClip
                rotation = quaternion.Euler(math.radians(euler), (math.RotationOrder)node.RotationEulerOrder);
            }
            else
            {
                var value = node.DefaultRotation.value;
                for (var i = 0; i < 4; i++)
                {
                    var curveIndex = node.RotationCurves[i];
                    if (curveIndex >= 0)
                        value[i] = EvaluateClipAtIndex(curves, ref memory, curveIndex, time);
                }

                rotation = math.normalizesafe(new quaternion(value), quaternion.identity);
            }

            var scale = node.DefaultScale;
            for (var i = 0; i < 3; i++)
            {
                var curveIndex = node.ScaleCurves[i];
                if (curveIndex >= 0)
                    scale[i] = EvaluateClipAtIndex(curves, ref memory, curveIndex, time);
            }

            return new TRS(translation, rotation, scale);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static unsafe void ValidateRootMotionCache(in StreamedClipMemory memory, ClipCurves* curves, ref ClipInstance clipInstance)
        {
            ClipSamplingCache.ValidateRootMotionCacheState(in memory, curves);

            ref var curveIndices = ref clipInstance.m_RootMotionCurveIndices;
            for (var i = 0; i < curveIndices.Length; i++)
            {
                ClipSamplingCache.ValidateRootMotionCurves(in memory, curveIndices[i], 1);
                ClipSamplingCache.ValidateRootMotionCurvesInClip(curves, curveIndices[i], 1);
            }
        }

        // Maps the playback times of a timer that runs over the clip length from 0 to a clip timer. The cycle offset
        // shifts the clip cycles relative to the timer loops, so a looping clip counts its own cycles
        static TimerState ToRootMotionTimer(ref Clip clip, DiscreteTime previousTime, DiscreteTime currentTime, WrapMode wrapMode, int loopCount)
        {
            if (clip.LoopTime && wrapMode != WrapMode.PingPong)
            {
                var previous = clip.ToTimerState(previousTime);
                var current = clip.ToTimerState(currentTime + new DiscreteTime(clip.Length) * (double)loopCount);
                return new TimerState
                {
                    Range = current.Range,
                    PreviousTime = previous.Time,
                    Time = current.Time,
                    PreviousLoopCount = previous.LoopCount,
                    LoopCount = current.LoopCount,
                    WrapMode = WrapMode.Loop,
                };
            }
            else
            {
                var clipWrapMode = wrapMode == WrapMode.PingPong ? WrapMode.PingPong : WrapMode.ClampForever;
                var previous = clip.ToTimerState(previousTime, clipWrapMode);
                var current = clip.ToTimerState(currentTime, clipWrapMode);
                return new TimerState
                {
                    Range = current.Range,
                    PreviousTime = previous.Time,
                    Time = current.Time,
                    LoopCount = loopCount,
                    WrapMode = wrapMode,
                };
            }
        }

        static unsafe float EvaluateCache(StreamedCacheItem* cache, float sampleTime)
        {
            float t = sampleTime - cache->time;
            return (t * (t * (t * cache->coeff[0] + cache->coeff[1]) + cache->coeff[2])) + cache->coeff[3];
        }

        static unsafe void EvaluateMultipleCaches(
            StreamedCacheItem* cache0,
            StreamedCacheItem* cache1,
            StreamedCacheItem* cache2,
            StreamedCacheItem* cache3,
            float sampleTime,
            float* output)
        {
            var time = new float4(sampleTime);

            var cachetime = new float4(cache0->time, cache1->time, cache2->time, cache3->time);
            var dt = time - cachetime;

            var coeffs0 = new float4(cache0->coeff[0], cache1->coeff[0], cache2->coeff[0], cache3->coeff[0]);
            var coeffs1 = new float4(cache0->coeff[1], cache1->coeff[1], cache2->coeff[1], cache3->coeff[1]);
            var coeffs2 = new float4(cache0->coeff[2], cache1->coeff[2], cache2->coeff[2], cache3->coeff[2]);
            var coeffs3 = new float4(cache0->coeff[3], cache1->coeff[3], cache2->coeff[3], cache3->coeff[3]);

            var value = dt * (dt * (dt * coeffs0 + coeffs1) + coeffs2) + coeffs3;
            *((float4*)output) = value;
        }

        static unsafe void EvaluateDiscreteCache(StreamedCacheItem* cache, float sampleTime, float* output)
        {
            UnsafeUtility.MemCpy(output, &cache->coeff.w, sizeof(UInt32));
        }

        static unsafe void EvaluateMultipleDiscreteCaches(StreamedCacheItem* cache0,
            StreamedCacheItem* cache1,
            StreamedCacheItem* cache2,
            StreamedCacheItem* cache3,
            float* output)
        {
            UnsafeUtility.MemCpy(&output[0], &cache0->coeff.w, sizeof(UInt32));
            UnsafeUtility.MemCpy(&output[1], &cache1->coeff.w, sizeof(UInt32));
            UnsafeUtility.MemCpy(&output[2], &cache2->coeff.w, sizeof(UInt32));
            UnsafeUtility.MemCpy(&output[3], &cache3->coeff.w, sizeof(UInt32));
        }


        static unsafe void EvaluateCaches(ref StreamedClipMemory cache, float sampleTime, float* output)
        {
            var caches = &cache.caches[0];
            var i = 0;

            for (; i + 4 <= cache.cacheCount; i += 4, caches += 4)
            {
                var item0 = caches;
                var item1 = caches + 1;
                var item2 = caches + 2;
                var item3 = caches + 3;
                EvaluateMultipleCaches(item0, item1, item2, item3, sampleTime, &output[i]);
            }

            for (; i < cache.cacheCount; ++i, ++caches)
            {
                output[i] = EvaluateCache(caches, sampleTime);
            }

            int discreteOffset = i;
            for (i = 0; i + 4 <= cache.discreteCacheCount; i += 4, caches += 4)
            {
                var item0 = caches;
                var item1 = caches + 1;
                var item2 = caches + 2;
                var item3 = caches + 3;
                EvaluateMultipleDiscreteCaches(item0, item1, item2, item3, &output[discreteOffset + i]);
            }

            for (; i < cache.discreteCacheCount; ++i, ++caches)
            {
                EvaluateDiscreteCache(caches, sampleTime, &output[discreteOffset + i]);
            }
        }


        static void RewindCache(ref StreamedClipMemory cache)
        {
            cache.time = -float.MaxValue;
            cache.readByteOffset = 0;
        }


        static unsafe void ConsumeCurveTimeData(CurveTimeData* curveData, StreamedCacheItem* caches)
        {
            float time = curveData->time;
            var keys = (CurveKey*)(curveData + 1);
            var count = curveData->count;

            int curveIndex = keys[0].curveIndex;
            var coeff = keys[0].coeff;

            for (int i = 1; i < count; i++)
            {
                ref StreamedCacheItem activeCache = ref caches[curveIndex];
                activeCache.time = time;
                activeCache.coeff = coeff;
                curveIndex = keys[i].curveIndex;
                coeff = keys[i].coeff;
            }

            {
                ref StreamedCacheItem activeCache = ref caches[curveIndex];
                activeCache.time = time;
                activeCache.coeff = coeff;
            }
        }

        static unsafe void ConsumeCurveTimeDataRootMotion(CurveTimeData* curveData, in StreamedClipMemory memory)
        {
            float time = curveData->time;
            var keys = (CurveKey*)(curveData + 1);
            var count = curveData->count;

            for (int i = 0; i < count; i++)
            {
                var slot = ClipSamplingCache.FindRootCurveSlot(in memory, keys[i].curveIndex);
                if (slot < 0)
                    continue;

                ref StreamedCacheItem activeCache = ref memory.caches[slot];
                activeCache.time = time;
                activeCache.coeff = keys[i].coeff;
            }
        }

        static unsafe void SeekClipForward(byte* curveData, float time, ref StreamedClipMemory cache)
        {
            var readByteOffset = cache.readByteOffset;
            var data = (CurveTimeData*)(curveData + readByteOffset);

            while (time >= data->time)
            {
                // Consume the data and apply it to the cache
                if (cache.rootMotionOnly)
                    ConsumeCurveTimeDataRootMotion(data, in cache);
                else
                    ConsumeCurveTimeData(data, cache.caches);

                // Seek forward by the consumed data
                readByteOffset += (uint)(sizeof(CurveTimeData) + data->count * sizeof(CurveKey));

                data = (CurveTimeData*)(curveData + readByteOffset);
            }

            // Synchronize cached time & offset
            cache.time = time;
            cache.readByteOffset = readByteOffset;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateSampleTime(float time)
        {
            // The streamed data ends with a key at +infinity, and a NaN time would stop the cache from rewinding
            if (!math.isfinite(time))
                throw new ArgumentException("The sample time must be a finite number.");
        }

        static unsafe void SeekClip(ClipCurves* clip, ref StreamedClipMemory cache, float time)
        {
            ValidateSampleTime(time);

            // No seeking is necessary, we are exactly at the same cached time
            // (Happens due to SampleClipAtIndex)
            // @TODO: it would be best to remove that and instead separate root motion data from other data
            if (time == cache.time)
                return;

            // Seeking backwards is not supported. Jump the beginning of curve.
            if (time < cache.time)
                RewindCache(ref cache);

            // Seek and make sure the curve cache is up to date
            SeekClipForward((byte*)clip->m_StreamedClip.data.Get(), time, ref cache);
        }

        static unsafe void SampleStreamedClip(ClipCurves* clip, ref StreamedClipMemory cache, float time, float* output)
        {
            SeekClip(clip, ref cache, time);

            // Evaluate the cache and write sampled values to output
            EvaluateCaches(ref cache, time, output);
        }

        // Direct port of BlendArray from denseclip.cpp
        static unsafe void BlendArray(float* lhs, float* rhs, uint size, float t, float* output)
        {
            for (uint i = 0; i < size; i++)
                output[i] = math.lerp(lhs[i], rhs[i], t);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateDenseClipHasFrames(int frameCount)
        {
            if (frameCount <= 0)
                throw new InvalidOperationException("The dense clip has curves but no frames.");
        }

        // Direct port of PrepareBlendValues from denseclip.cpp
        static unsafe void PrepareBlendValues(ClipCurves* clip, float time, out float* lhs, out float* rhs, out float u)
        {
            ref var denseClip = ref clip->m_DenseClip;
            ValidateDenseClipHasFrames(denseClip.m_FrameCount);
            var samples = (float*)denseClip.m_SampleArray.Get();

            time -= denseClip.m_BeginTime;

            float index;
            u = math.modf(time * denseClip.m_SampleRate, out index);

            int lhsIndex = (int)index;
            int rhsIndex = lhsIndex + 1;
            lhsIndex = math.max(0, lhsIndex);
            lhsIndex = math.min(denseClip.m_FrameCount - 1, lhsIndex);

            rhsIndex = math.max(0, rhsIndex);
            rhsIndex = math.min(denseClip.m_FrameCount - 1, rhsIndex);

            lhs = samples + lhsIndex * denseClip.m_CurveCount;
            rhs = samples + rhsIndex * denseClip.m_CurveCount;
        }

        // Direct port of SampleClip(const DenseClip&, ...) from denseclip.cpp
        static unsafe void SampleDenseClip(ClipCurves* clip, float time, float* output)
        {
            float u;
            float* lhsPtr;
            float* rhsPtr;
            PrepareBlendValues(clip, time, out lhsPtr, out rhsPtr, out u);

            BlendArray(lhsPtr, rhsPtr, clip->m_DenseClip.m_CurveCount, u, output);
        }

        // Direct port of SampleClip(const ConstantClip&, ...) from constantclip.cpp
        static unsafe void SampleConstantClip(ClipCurves* clip, uint outputCount, float* output)
        {
            UnsafeUtility.MemCpy(output, clip->m_ConstantClip.data.Get(), outputCount * sizeof(float));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateRootMotionSlot(int slot)
        {
            if (slot < 0)
                throw new IndexOutOfRangeException("The curve index is not a root motion curve of the ClipSamplingCache.");
        }

        // Direct port of SampleClipAtIndex(const StreamedClip&, ...) from streamedclip.cpp
        static unsafe float SampleStreamedClipAtIndex(ClipCurves* clip, ref StreamedClipMemory cache, int index, float time)
        {
            SeekClip(clip, ref cache, time);

            if (cache.rootMotionOnly)
            {
                index = ClipSamplingCache.FindRootCurveSlot(in cache, index);
                ValidateRootMotionSlot(index);
            }

            return EvaluateCache(cache.caches + index, time);
        }

        // Direct port of SampleClipAtIndex(const DenseClip&, ...) from denseclip.cpp
        static unsafe float SampleDenseClipAtIndex(ClipCurves* clip, int curveIndex, float time)
        {
            float u;
            float* lhsPtr;
            float* rhsPtr;
            PrepareBlendValues(clip, time, out lhsPtr, out rhsPtr, out u);

            return math.lerp(lhsPtr[curveIndex], rhsPtr[curveIndex], u);
        }

        // Direct port of EvaluateClipAtIndex from curvedata.cpp
        internal static unsafe float EvaluateClipAtIndex(ClipCurves* clip, ref StreamedClipMemory memory, int index, float time)
        {
            var streamedCurveCount = clip->m_StreamedClip.curveCount + clip->m_StreamedClip.discreteCurveCount;
            if (index < streamedCurveCount)
                return SampleStreamedClipAtIndex(clip, ref memory, index, time);
            index -= streamedCurveCount;

            if (index < clip->m_DenseClip.m_CurveCount)
                return SampleDenseClipAtIndex(clip, index, time);
            index -= (int)clip->m_DenseClip.m_CurveCount;

            return ((float*)clip->m_ConstantClip.data.Get())[index];
        }

        // Direct port of EvaluateClip from curvedata.cpp
        static unsafe void EvaluateClip(ClipCurves* clip, ref StreamedClipMemory memory, float time, float* output)
        {
            float* outputData = output;

            ref var streamedClip = ref clip->m_StreamedClip;
            if ((streamedClip.curveCount + streamedClip.discreteCurveCount) != 0)
            {
                SampleStreamedClip(clip, ref memory, time, outputData);
                outputData += streamedClip.curveCount + streamedClip.discreteCurveCount;
            }

            var denseCurveCount = clip->m_DenseClip.m_CurveCount;
            if (denseCurveCount != 0)
            {
                SampleDenseClip(clip, time, outputData);
                outputData += denseCurveCount;
            }

            var constantCurveCount = clip->m_ConstantClip.curveCount;
            if (constantCurveCount != 0)
            {
                // Constant clips are not sampling based on the total curve count
                SampleConstantClip(clip, constantCurveCount, outputData);
            }
        }

        static unsafe float3 LoadStart(ValueDelta* deltas, int index) =>
            new float3(deltas[index].m_Start, deltas[index + 1].m_Start, deltas[index + 2].m_Start);

        static unsafe float3 LoadStop(ValueDelta* deltas, int index) =>
            new float3(deltas[index].m_Stop, deltas[index + 1].m_Stop, deltas[index + 2].m_Stop);

        static unsafe quaternion LoadStartRotation(ValueDelta* deltas, int index) =>
            new quaternion(deltas[index].m_Start, deltas[index + 1].m_Start, deltas[index + 2].m_Start, deltas[index + 3].m_Start);

        static unsafe quaternion LoadStopRotation(ValueDelta* deltas, int index) =>
            new quaternion(deltas[index].m_Stop, deltas[index + 1].m_Stop, deltas[index + 2].m_Stop, deltas[index + 3].m_Stop);

        static unsafe float3 LoopOffset(ValueDelta* deltas, int index, float normalizedTime) =>
            (LoadStart(deltas, index) - LoadStop(deltas, index)) * normalizedTime;

        static quaternion LoopRotation(quaternion rotation, quaternion start, quaternion stop, float normalizedTime) =>
            math.normalize(math.mul(rotation, MathUtility.quatWeight(math.mul(math.conjugate(stop), start), normalizedTime)));

        static unsafe float LoadReference(float* reference, int stride, int index) => reference[index * stride];

        static unsafe float3 LoadReference3(float* reference, int stride, int index) =>
            new float3(
                LoadReference(reference, stride, index),
                LoadReference(reference, stride, index + 1),
                LoadReference(reference, stride, index + 2));

        static bool HasQuaternionBinding(ref ClipInstance clipInstance, int rigIndex)
        {
            var rotationBindings = clipInstance.RotationBindingMap;
            for (var i = 0; i < rotationBindings.Length; ++i)
            {
                if (rotationBindings[i].RigIndex == rigIndex)
                    return true;
            }

            return false;
        }

        // Subtracts the reference pose of the clip from the channels that the clip animates, as ValueArraySub in
        // valuearray.cpp does for an additive layer. The reference is the additive reference pose of the clip if it has
        // one, and the start values of the clip otherwise, as DeltasFromClip in avatar.cpp. Discrete ints and object
        // references are not subtracted.
        [BurstCompile]
        internal static unsafe void SubtractAdditiveReference(
            Clip* clip,
            ref ClipInstance clipInstance,
            ref AnimationStream stream)
        {
            var deltas = (ValueDelta*)clip->m_ValueArrayDelta.Get();
            if (deltas == null)
                return;

            var hasReferencePose = !clip->m_ValueArrayReferencePose.IsNull &&
                clip->m_ValueArrayReferencePoseCount == clip->m_ValueArrayCount;

            // Without a reference pose, the reference is the start values of the clip, as DeltasFromClip in avatar.cpp
            var reference = hasReferencePose ? (float*)clip->m_ValueArrayReferencePose.Get() : &deltas->m_Start;
            var stride = hasReferencePose ? 1 : 2;

            var translationBindings = clipInstance.TranslationBindingMap;
            for (var i = 0; i < translationBindings.Length; ++i)
            {
                var binding = translationBindings[i];
                var translation = stream.GetLocalToParentTranslation(binding.RigIndex);
                translation -= LoadReference3(reference, stride, binding.ClipIndex);
                stream.SetLocalToParentTranslation(binding.RigIndex, translation);
            }

            // Euler curves are in degrees. A quaternion curve of the same channel wins when sampling, so its reference
            // wins too.
            var eulerBindings = clipInstance.EulerBindingMap;
            for (var i = 0; i < eulerBindings.Length; ++i)
            {
                var binding = eulerBindings[i];
                if (HasQuaternionBinding(ref clipInstance, binding.RigIndex))
                    continue;

                var euler = LoadReference3(reference, stride, binding.ClipIndex);
                var referenceRotation = quaternion.Euler(math.radians(euler), clipInstance.m_EulerRotationOrders[i]);
                SubtractRotation(ref stream, binding.RigIndex, referenceRotation);
            }

            // A reference pose that doesn't animate the rotation holds zeros, which normalizesafe turns into the
            // identity
            var rotationBindings = clipInstance.RotationBindingMap;
            for (var i = 0; i < rotationBindings.Length; ++i)
            {
                var binding = rotationBindings[i];
                var index = binding.ClipIndex;
                var referenceRotation = new quaternion(new float4(
                    LoadReference3(reference, stride, index),
                    LoadReference(reference, stride, index + 3)));
                SubtractRotation(ref stream, binding.RigIndex, math.normalizesafe(referenceRotation, quaternion.identity));
            }

            var scaleBindings = clipInstance.ScaleBindingMap;
            for (var i = 0; i < scaleBindings.Length; ++i)
            {
                var binding = scaleBindings[i];
                var scale = stream.GetLocalToParentScale(binding.RigIndex);
                scale -= LoadReference3(reference, stride, binding.ClipIndex);
                stream.SetLocalToParentScale(binding.RigIndex, scale);
            }

            var floatBindings = clipInstance.FloatBindingMap;
            for (var i = 0; i < floatBindings.Length; ++i)
            {
                var binding = floatBindings[i];
                var value = stream.GetFloat(binding.RigIndex) - LoadReference(reference, stride, binding.ClipIndex);
                stream.SetFloat(binding.RigIndex, value);
            }
        }

        // The rotation from the reference rotation to the sampled rotation, as ValueArraySub in valuearray.cpp
        static void SubtractRotation(ref AnimationStream stream, int rigIndex, quaternion reference)
        {
            var rotation = stream.GetLocalToParentRotation(rigIndex);
            stream.SetLocalToParentRotation(rigIndex, math.normalize(math.mul(math.conjugate(reference), rotation)));
        }

        // The Loop Pose correction of SampleAnimationClip, applied to the stream after the root motion bone was extracted.
        // The root motion bone uses its extracted start and stop values, as ValueArrayLoop runs after AdjustPoseForMotion.
        static unsafe void ApplyLoopPose(ref ClipInstance clipInstance, ValueDelta* loopDeltas, float normalizedTime, ref AnimationStream stream)
        {
            var samplingData = clipInstance.RootMotionSamplingData;
            var startBone = samplingData.LoopStartBone;
            var stopBone = samplingData.LoopStopBone;
            var boneIndex = stream.Rig.RootMotionBoneIndex;

            var translationBindings = clipInstance.TranslationBindingMap;
            for (var i = 0; i < translationBindings.Length; ++i)
            {
                var binding = translationBindings[i];
                var offset = binding.RigIndex == boneIndex ?
                    (startBone.t - stopBone.t) * normalizedTime :
                    LoopOffset(loopDeltas, binding.ClipIndex, normalizedTime);
                stream.SetLocalToParentTranslation(binding.RigIndex, stream.GetLocalToParentTranslation(binding.RigIndex) + offset);
            }

            // A quaternion curve wins over Euler curves of the same bone when sampling, so only its correction applies
            var eulerBindingMap = clipInstance.EulerBindingMap;
            for (var i = 0; i < eulerBindingMap.Length; ++i)
            {
                var binding = eulerBindingMap[i];
                if (HasQuaternionBinding(ref clipInstance, binding.RigIndex))
                    continue;

                var rotation = stream.GetLocalToParentRotation(binding.RigIndex);
                if (binding.RigIndex == boneIndex)
                    rotation = LoopRotation(rotation, startBone.r, stopBone.r, normalizedTime);
                else
                {
                    var order = clipInstance.m_EulerRotationOrders[i];
                    var start = quaternion.Euler(math.radians(LoadStart(loopDeltas, binding.ClipIndex)), order);
                    var stop = quaternion.Euler(math.radians(LoadStop(loopDeltas, binding.ClipIndex)), order);
                    rotation = LoopRotation(rotation, start, stop, normalizedTime);
                }

                stream.SetLocalToParentRotation(binding.RigIndex, rotation);
            }

            var rotationBindings = clipInstance.RotationBindingMap;
            for (var i = 0; i < rotationBindings.Length; ++i)
            {
                var binding = rotationBindings[i];
                var rotation = stream.GetLocalToParentRotation(binding.RigIndex);
                rotation = binding.RigIndex == boneIndex ?
                    LoopRotation(rotation, startBone.r, stopBone.r, normalizedTime) :
                    LoopRotation(rotation, LoadStartRotation(loopDeltas, binding.ClipIndex), LoadStopRotation(loopDeltas, binding.ClipIndex), normalizedTime);
                stream.SetLocalToParentRotation(binding.RigIndex, rotation);
            }

            var scaleBindings = clipInstance.ScaleBindingMap;
            for (var i = 0; i < scaleBindings.Length; ++i)
            {
                var binding = scaleBindings[i];
                var offset = binding.RigIndex == boneIndex ?
                    (startBone.s - stopBone.s) * normalizedTime :
                    LoopOffset(loopDeltas, binding.ClipIndex, normalizedTime);
                stream.SetLocalToParentScale(binding.RigIndex, stream.GetLocalToParentScale(binding.RigIndex) + offset);
            }

            var floatBindings = clipInstance.FloatBindingMap;
            for (var i = 0; i < floatBindings.Length; ++i)
            {
                var binding = floatBindings[i];
                var offset = (loopDeltas[binding.ClipIndex].m_Start - loopDeltas[binding.ClipIndex].m_Stop) * normalizedTime;
                stream.SetFloat(binding.RigIndex, stream.GetFloat(binding.RigIndex) + offset);
            }
        }

        // loopDeltas is null unless Loop Pose is on. It then adds the difference between the first and last frames,
        // weighted by the normalized time, as ValueArrayLoop in valuearray.cpp
        [BurstCompile]
        static unsafe void SampleAnimationClip(ClipCurves* clip,
            ref ClipInstance clipInstance,
            float time,
            ValueDelta* loopDeltas,
            float normalizedTime,
            ref StreamedClipMemory cache,
            ref AnimationStream stream)
        {
            EvaluateClip(clip, ref cache, time, cache.output);

            var translationBindings = clipInstance.TranslationBindingMap;
            for (var i = 0; i < translationBindings.Length; ++i)
            {
                var binding = translationBindings[i];
                var position = GetDataInSample<float3>(cache.output, binding.ClipIndex);
                if (loopDeltas != null)
                    position += LoopOffset(loopDeltas, binding.ClipIndex, normalizedTime);
                stream.SetLocalToParentTranslation(binding.RigIndex, position);
            }

            // Bugfix (MECA-70): Euler curves used to be skipped. Sampled before quaternions so a quaternion curve wins, as in Mecanim
            var eulerBindingMap = clipInstance.EulerBindingMap;
            for (var i = 0; i < eulerBindingMap.Length; ++i)
            {
                var binding = eulerBindingMap[i];
                var euler = GetDataInSample<float3>(cache.output, binding.ClipIndex);
                var order = clipInstance.m_EulerRotationOrders[i];
                var rotation = quaternion.Euler(math.radians(euler), order);
                if (loopDeltas != null)
                {
                    var start = quaternion.Euler(math.radians(LoadStart(loopDeltas, binding.ClipIndex)), order);
                    var stop = quaternion.Euler(math.radians(LoadStop(loopDeltas, binding.ClipIndex)), order);
                    rotation = LoopRotation(rotation, start, stop, normalizedTime);
                }

                stream.SetLocalToParentRotation(binding.RigIndex, rotation);
            }

            var rotationBindings = clipInstance.RotationBindingMap;
            for (var i = 0; i < rotationBindings.Length; ++i)
            {
                var binding = rotationBindings[i];
                var rotation = GetDataInSample<quaternion>(cache.output, binding.ClipIndex);
                // Bugfix (MECA-70): the normalized result used to be discarded, unlike Mecanim's RotationValuesFromClip
                rotation = math.normalize(rotation);
                if (loopDeltas != null)
                    rotation = LoopRotation(rotation, LoadStartRotation(loopDeltas, binding.ClipIndex), LoadStopRotation(loopDeltas, binding.ClipIndex), normalizedTime);

                stream.SetLocalToParentRotation(binding.RigIndex, rotation);
            }

            var scaleBindings = clipInstance.ScaleBindingMap;
            for (var i = 0; i < scaleBindings.Length; ++i)
            {
                var binding = scaleBindings[i];
                var scale = GetDataInSample<float3>(cache.output, binding.ClipIndex);
                if (loopDeltas != null)
                    scale += LoopOffset(loopDeltas, binding.ClipIndex, normalizedTime);
                stream.SetLocalToParentScale(binding.RigIndex, scale);
            }

            var floatBindings = clipInstance.FloatBindingMap;
            for (var i = 0; i < floatBindings.Length; ++i)
            {
                var binding = floatBindings[i];
                var value = GetDataInSample<float>(cache.output, binding.ClipIndex);
                if (loopDeltas != null)
                    value += (loopDeltas[binding.ClipIndex].m_Start - loopDeltas[binding.ClipIndex].m_Stop) * normalizedTime;
                stream.SetFloat(binding.RigIndex, value);
            }

            var intBindings = clipInstance.IntBindingMap;
            for (var i = 0; i < intBindings.Length; ++i)
            {
                var binding = intBindings[i];
                var value = GetDataInSample<int>(cache.output, binding.ClipIndex);
                stream.SetInt(binding.RigIndex, value);
            }

            var entityIdBindings = clipInstance.EntityIdBindingMap;
            for (var i = 0; i < entityIdBindings.Length; ++i)
            {
                var binding = entityIdBindings[i];
                // Mecanim PPtr curves sample a 32-bit index into the clip's mapping table,
                // not the EntityId itself. Dereference via ClipInstance.m_PPtrEntityIds.
                var pptrIndex = (int)GetDataInSample<float>(cache.output, binding.ClipIndex);
                stream.SetEntityId(binding.RigIndex, clipInstance.ResolvePPtrEntityId(pptrIndex));
            }
        }

        [BurstCompile]
        static unsafe void SampleAnimationClip(ClipCurves* clip, ref ClipInstance clipInstance, in ClipKeyframe keyframe, ref AnimationStream stream)
        {
            var samplesPtr = (float*)clip->m_DenseClip.m_SampleArray.Get();
            var scratch = stackalloc float4[4];
            var bindingIdx = 0;

            var curveIndex = 0;
            // Translations
            {
                var translationBindings = clipInstance.TranslationBindingMap;
                bindingIdx = 0;
                for (int i = 0; i < clipInstance.TranslationBindingChunkCount; ++i)
                {
                    //var curveIndex = math.mad(translationBindings[bindingIdx].ClipIndex, BindingSet.TranslationKeyFloatCount, clipBindings.TranslationSamplesOffset);
                    var lhs4 = (float4*)(samplesPtr + curveIndex + keyframe.Left);
                    var rhs4 = (float4*)(samplesPtr + curveIndex + keyframe.Right);

                    scratch[0] = math.lerp(lhs4[0], rhs4[0], keyframe.Weight);
                    scratch[1] = math.lerp(lhs4[1], rhs4[1], keyframe.Weight);
                    scratch[2] = math.lerp(lhs4[2], rhs4[2], keyframe.Weight);

                    var tScratch = (float3*)scratch;
                    stream.SetLocalToParentTranslation(translationBindings[bindingIdx + 0].RigIndex, tScratch[0]);
                    stream.SetLocalToParentTranslation(translationBindings[bindingIdx + 1].RigIndex, tScratch[1]);
                    stream.SetLocalToParentTranslation(translationBindings[bindingIdx + 2].RigIndex, tScratch[2]);
                    stream.SetLocalToParentTranslation(translationBindings[bindingIdx + 3].RigIndex, tScratch[3]);

                    bindingIdx += ClipInstance.k_BindingsInChunk;
                    curveIndex += ClipInstance.k_BindingsInChunk * 3;
                }

                // Process remaining bindings
                for (; bindingIdx < translationBindings.Length; ++bindingIdx)
                {
                    var binding = translationBindings[bindingIdx];

                    ref readonly var leftKey = ref GetDataInSample<float3>(samplesPtr, curveIndex + keyframe.Left);
                    ref readonly var rightKey = ref GetDataInSample<float3>(samplesPtr, curveIndex + keyframe.Right);
                    stream.SetLocalToParentTranslation(binding.RigIndex, math.lerp(leftKey, rightKey, keyframe.Weight));

                    curveIndex += 3;
                }
            }

            // Rotations
            {
                var rotationBindings = clipInstance.RotationBindingMap;
                var qScratch = (quaternion*)scratch;
                bindingIdx = 0;
                for (int i = 0; i < clipInstance.RotationBindingChunkCount; ++i)
                {
                    var lhsQ = (quaternion*)(samplesPtr + curveIndex + keyframe.Left);
                    var rhsQ = (quaternion*)(samplesPtr + curveIndex + keyframe.Right);

                    qScratch[0] = math.nlerp(lhsQ[0], rhsQ[0], keyframe.Weight);
                    qScratch[1] = math.nlerp(lhsQ[1], rhsQ[1], keyframe.Weight);
                    qScratch[2] = math.nlerp(lhsQ[2], rhsQ[2], keyframe.Weight);
                    qScratch[3] = math.nlerp(lhsQ[3], rhsQ[3], keyframe.Weight);

                    stream.SetLocalToParentRotation(rotationBindings[bindingIdx + 0].RigIndex, qScratch[0]);
                    stream.SetLocalToParentRotation(rotationBindings[bindingIdx + 1].RigIndex, qScratch[1]);
                    stream.SetLocalToParentRotation(rotationBindings[bindingIdx + 2].RigIndex, qScratch[2]);
                    stream.SetLocalToParentRotation(rotationBindings[bindingIdx + 3].RigIndex, qScratch[3]);

                    bindingIdx += ClipInstance.k_BindingsInChunk;
                    curveIndex += ClipInstance.k_BindingsInChunk * 3;
                }

                // Process remaining bindings
                for (; bindingIdx < rotationBindings.Length; ++bindingIdx)
                {
                    var binding = rotationBindings[bindingIdx];

                    ref readonly var leftKey = ref GetDataInSample<quaternion>(samplesPtr, curveIndex + keyframe.Left);
                    ref readonly var rightKey = ref GetDataInSample<quaternion>(samplesPtr, curveIndex + keyframe.Right);
                    stream.SetLocalToParentRotation(binding.RigIndex, math.nlerp(leftKey, rightKey, keyframe.Weight));

                    curveIndex += 3;
                }
            }
        }

    }
}
