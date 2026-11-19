// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Animations
{
    struct CurveTimeData
    {
        public float  time;
        public UInt32 count;
        // This is implicitly followed by CurveKey in the stream
    };

    struct CurveKey
    {
        public int         curveIndex;
        public float4 coeff;
    };

    struct StreamedCacheItem
    {
        public float        time;
        public float4  coeff;
    };

    unsafe struct ClipSamplingCacheData
    {
        const int k_RootCurveCount = 7;

        public Allocator Allocator;
        public UnsafeList<StreamedCacheItem> Caches;
        public UnsafeList<float> Output;
        public StreamedClipMemory Memory;

        // data must point to memory that the caller allocated. The helpers below work on that memory only, so a cache
        // can live inside another allocation, as in ClipSamplerData
        internal static void Create(ClipSamplingCacheData* data, int curveCapacity, Allocator allocator)
        {
            data->Allocator = allocator;
            data->Memory = default;
            data->Memory.owner = data;
            AllocateLists(data, curveCapacity);
        }

        internal static void Free(ClipSamplingCacheData* data)
        {
            DisposeLists(data);
        }

        // A clip without muscle data has no curves, so the cache stays empty and samples nothing
        internal static void Initialize(ClipSamplingCacheData* data, Clip* muscleClipPtr)
        {
            if (muscleClipPtr->m_Clip.IsNull)
            {
                InitializeCurves(data, 0, 0, 0, 0);
                return;
            }

            InitializeForClip(data, ref data->Memory, (ClipCurves*)muscleClipPtr->m_Clip.Get());
        }

        // The memory is passed by reference, and only written through that reference when sampling re-initializes it,
        // because Burst treats a ref parameter as not aliased with the pointer to the data that contains it
        static void InitializeForClip(ClipSamplingCacheData* data, ref StreamedClipMemory memory, ClipCurves* curves)
        {
            InitializeCurves(data, ref memory, curves->m_StreamedClip.curveCount, curves->m_StreamedClip.discreteCurveCount,
                (int)curves->m_DenseClip.m_CurveCount, (int)curves->m_ConstantClip.curveCount);
            memory.clip = curves;
        }

        // Called by every sampling entry point, so it must stay Burst compatible and must not touch the safety handle
        // of the ClipSamplingCache that owns the memory. A cache that was never initialized is left alone, so that
        // sampling it is still reported as a missing Initialize call.
        internal static void ReinitializeIfClipChanged(ref StreamedClipMemory memory, ClipCurves* clip)
        {
            if (memory.clip == null || memory.rootMotionOnly || IsInitializedFor(in memory, clip))
                return;

            // A memory without an owner was laid out by hand and cannot grow
            if (memory.owner != null)
                InitializeForClip(memory.owner, ref memory, clip);
        }

        // A root motion cache holds no curve count, so it only keeps the clip it last sampled. The first sample binds
        // it to the clip without a reset, because its state is still empty
        internal static void ReinitializeRootMotionIfClipChanged(ref StreamedClipMemory memory, ClipCurves* clip)
        {
            if (!memory.rootMotionOnly)
            {
                ReinitializeIfClipChanged(ref memory, clip);
                return;
            }

            if (memory.clip == clip)
                return;

            var hadClip = memory.clip != null;
            memory.clip = clip;
            if (hadClip)
                ResetRootMotionState(ref memory);
        }

        static bool IsInitializedFor(in StreamedClipMemory memory, ClipCurves* clip)
        {
            return memory.clip == clip && memory.curveCount == ClipSamplingCache.GetCurveCount(clip);
        }

        static void ResetRootMotionState(ref StreamedClipMemory memory)
        {
            UnsafeUtility.MemSet(memory.caches, 0, UnsafeUtility.SizeOf<StreamedCacheItem>() * k_RootCurveCount);
            memory.readByteOffset = 0;
            memory.time = -float.MaxValue;
        }

        internal static void LogOutputCapacityErrorOnce(ref StreamedClipMemory memory)
        {
            if (memory.capacityErrorLogged)
                return;

            memory.capacityErrorLogged = true;
            UnityEngine.Debug.LogError("The ClipSamplingCache is too small for the clip, so the clip is not sampled. Call Initialize with the clip before sampling.");
        }

        internal static void InitializeCurves(ClipSamplingCacheData* data, ushort streamedClipCurveCount, ushort discreteStreamedClipCurveCount, int denseClipCurveCount, int constantClipCurveCount)
        {
            InitializeCurves(data, ref data->Memory, streamedClipCurveCount, discreteStreamedClipCurveCount,
                denseClipCurveCount, constantClipCurveCount);
        }

        static void InitializeCurves(ClipSamplingCacheData* data, ref StreamedClipMemory memory, ushort streamedClipCurveCount, ushort discreteStreamedClipCurveCount, int denseClipCurveCount, int constantClipCurveCount)
        {
            var streamedCurveCount = streamedClipCurveCount + discreteStreamedClipCurveCount;
            var totalCurveCount = streamedCurveCount + denseClipCurveCount + constantClipCurveCount;

            GrowIfNeeded(data, totalCurveCount);

            memory = new StreamedClipMemory()
            {
                caches = data->Caches.GetUnsafePtr(),
                owner = data,
                cacheCount = streamedClipCurveCount,
                discreteCacheCount = discreteStreamedClipCurveCount,
                curveCount = totalCurveCount,
                outputCapacity = data->Output.Length,
                output = data->Output.GetUnsafePtr(),
                readByteOffset = 0,
                time = -float.MaxValue
            };

            UnsafeUtility.MemSet(memory.caches, 0, UnsafeUtility.SizeOf<StreamedCacheItem>() * streamedCurveCount);
            UnsafeUtility.MemSet(memory.output, 0, UnsafeUtility.SizeOf<float>() * totalCurveCount);
        }

        internal static void InitializeRootMotion(ClipSamplingCacheData* data, int rootTranslationIndex, int rootRotationIndex)
        {
            ref var caches = ref data->Caches;
            if (caches.Length < k_RootCurveCount)
                caches.Resize(k_RootCurveCount, NativeArrayOptions.UninitializedMemory);

            ref var memory = ref data->Memory;
            memory = new StreamedClipMemory()
            {
                caches = caches.GetUnsafePtr(),
                owner = data,
                cacheCount = 0,
                discreteCacheCount = 0,
                rootMotionOnly = true,
                // -1 would fall inside the (uint) range checks; push missing curves out of range.
                rootTranslationIndex = rootTranslationIndex >= 0 ? rootTranslationIndex : int.MinValue,
                rootRotationIndex = rootRotationIndex >= 0 ? rootRotationIndex : int.MinValue,
                output = null,
                readByteOffset = 0,
                time = -float.MaxValue
            };

            UnsafeUtility.MemSet(memory.caches, 0, UnsafeUtility.SizeOf<StreamedCacheItem>() * k_RootCurveCount);
        }

        static void GrowIfNeeded(ClipSamplingCacheData* data, int requiredCurveCount)
        {
            if (requiredCurveCount > data->Output.Length)
            {
                DisposeLists(data);
                AllocateLists(data, requiredCurveCount);
            }
        }

        static void AllocateLists(ClipSamplingCacheData* data, int curveCapacity)
        {
            var allocator = data->Allocator;
            data->Caches = new UnsafeList<StreamedCacheItem>(curveCapacity, allocator);
            data->Caches.Resize(curveCapacity, NativeArrayOptions.UninitializedMemory);
            data->Output = new UnsafeList<float>(curveCapacity, allocator);
            data->Output.Resize(curveCapacity, NativeArrayOptions.UninitializedMemory);
        }

        static void DisposeLists(ClipSamplingCacheData* data)
        {
            data->Caches.Dispose();
            data->Output.Dispose();
        }
    }

    /// <summary>
    /// Stores the sampling state of a clip between samples, so that sampling the clip forward in time is faster.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Initialize(ref Clip)"/> with the clip before the first sample. The cache identifies its clip by
    /// the address of the clip data and its curve count. When you sample other clip data, for example the data that
    /// Unity bakes again after an edit of the animation clip, the cache initializes itself for that data as
    /// <see cref="Initialize(ref Clip)"/> does, and grows if needed. This also works in Burst-compiled code and in
    /// players. Keep the previous clip data alive until you sample the new data, because a new allocation can reuse the
    /// address of data that was released. If you can't, call <see cref="Initialize(ref Clip)"/> yourself.
    ///
    /// A cache that was never initialized and a cache prepared with <see cref="InitializeRootMotion(int, int)"/> are not
    /// initialized for any clip. If safety checks are enabled, sampling the full clip with them
    /// throws an <see cref="InvalidOperationException"/>. Without safety checks, the clip is not sampled and the cache
    /// logs an error once. Safety checks are always enabled in the Editor and never in a built Player.
    ///
    /// Copies of this struct share the same memory. Dispose only one copy.
    /// </remarks>
    /// <seealso cref="ClipInstance"/>
    [NativeContainer]
    internal unsafe struct ClipSamplingCache : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal ClipSamplingCacheData* m_Data;

        internal AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<ClipSamplingCache>();

        /// <summary>Creates a sampling cache that can hold the sampling state of a given number of curves.</summary>
        /// <remarks>
        /// You can reuse one cache for several clips by calling <see cref="Initialize(ref Clip)"/> before you sample
        /// each clip. The cache is not ready to sample until you call Initialize. If you know the largest number of
        /// curves of the clips that you sample, pass it as <paramref name="curveCapacity"/> to avoid a reallocation.
        /// Call <see cref="Dispose()"/> to release the memory of the cache.
        /// </remarks>
        /// <param name="curveCapacity">The number of curves the cache can hold before it must grow. Accepted values are
        /// 0 or greater. <see cref="Initialize(ref Clip)"/> grows the cache if the clip has more curves.</param>
        /// <param name="allocator">The allocator to use for the memory of the cache. Must be a valid allocator, so
        /// <see cref="Allocator.Invalid"/> and <see cref="Allocator.None"/> are not allowed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if safety checks are enabled and
        /// <paramref name="curveCapacity"/> is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and <paramref name="allocator"/> is
        /// not a valid allocator.</exception>
        public ClipSamplingCache(int curveCapacity, Allocator allocator)
        {
            ValidateCapacity(curveCapacity, allocator);

            this = default;
            m_Data = AllocatorManager.Allocate<ClipSamplingCacheData>(allocator);
            ClipSamplingCacheData.Create(m_Data, curveCapacity, allocator);

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<ClipSamplingCache>(ref m_Safety, ref s_SafetyId.Data);
        }

        /// <summary>Whether the cache has allocated memory.</summary>
        /// <remarks>
        /// `false` for a default value, and for the copy of the cache on which <see cref="Dispose()"/> was called.
        /// </remarks>
        public bool IsCreated => m_Data != null;

        internal ref StreamedClipMemory Memory
        {
            get
            {
                ValidateIsCreated();
                return ref m_Data->Memory;
            }
        }

        /// <summary>Prepares the cache to sample a clip.</summary>
        /// <remarks>
        /// Call this method before the first sample. Sampling other clip data initializes the cache again by itself,
        /// but you can call this method to do it explicitly. It resets the sampling state, so the next sample starts
        /// from the beginning of the clip data. If the clip has more curves than the cache can hold, the cache
        /// allocates more memory. If the clip has no curve data, sampling it with this cache doesn't change the
        /// animation stream.
        ///
        /// This method also switches a cache prepared with <see cref="InitializeRootMotion(int, int)"/> back to
        /// sampling the full clip.
        /// </remarks>
        /// <param name="clip">The clip to sample. The cache keeps a pointer to the curve data of the clip, so the
        /// clip must stay in memory for as long as the cache samples it.</param>
        public void Initialize(ref Clip clip)
        {
            var muscleClipPtr = (Clip*)UnsafeUtility.AddressOf(ref clip);
            ValidateClipPtr(muscleClipPtr);
            ValidateIsCreated();

            ClipSamplingCacheData.Initialize(m_Data, muscleClipPtr);
        }

        internal void InitializeCurves(ushort streamedClipCurveCount, ushort discreteStreamedClipCurveCount, int denseClipCurveCount, int constantClipCurveCount)
        {
            ValidateIsCreated();

            ClipSamplingCacheData.InitializeCurves(m_Data, streamedClipCurveCount, discreteStreamedClipCurveCount,
                denseClipCurveCount, constantClipCurveCount);
        }

        /// <summary>Prepares the cache to sample only the root motion curves of a clip.</summary>
        /// <remarks>
        /// The cache then holds the sampling state of the 7 root curves only: 3 for the translation and 4 for the
        /// rotation. It cannot sample the full clip:
        /// <see cref="Core.SampleAnimationClipOverwrite(BlobAssetReference{Clip}, BlobAssetReference{ClipInstance}, ref ClipSamplingCache, Unity.IntegerTime.DiscreteTime, ref AnimationStream)"/>
        /// throws an <see cref="InvalidOperationException"/> if safety checks are enabled. Call
        /// <see cref="Initialize(ref Clip)"/> to go back to sampling the full clip.
        ///
        /// Both indices use the same numbering as <see cref="ClipInstance.MappedBinding.ClipIndex"/>.
        /// </remarks>
        /// <param name="rootTranslationIndex">The index of the first of the 3 root translation curves: x, y, and z.
        /// Use `-1` if the clip has no root translation curves.</param>
        /// <param name="rootRotationIndex">The index of the first of the 4 root rotation curves: x, y, z, and w of
        /// a quaternion. Use `-1` if the clip has no root rotation curves.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if safety checks are enabled and an index is less
        /// than -1.</exception>
        public void InitializeRootMotion(int rootTranslationIndex, int rootRotationIndex)
        {
            ValidateIsCreated();
            ValidateRootIndices(rootTranslationIndex, rootRotationIndex);

            ClipSamplingCacheData.InitializeRootMotion(m_Data, rootTranslationIndex, rootRotationIndex);
        }

        /// <summary>Releases the memory of the cache.</summary>
        /// <remarks>
        /// Copies of the cache share the same memory. Call this method on only one copy, and do not use the other
        /// copies afterward. Calling it on a default cache has no effect.
        /// </remarks>
        public void Dispose()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            if (m_Data == null)
                return;

            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
            ClipSamplingCacheData.Free(m_Data);

            AllocatorManager.Free(m_Data->Allocator, m_Data);
            m_Data = null;
        }

        internal static int GetCurveCount(ClipCurves* clip)
        {
            return clip->m_StreamedClip.curveCount + clip->m_StreamedClip.discreteCurveCount +
                (int)clip->m_DenseClip.m_CurveCount + (int)clip->m_ConstantClip.curveCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasOutputCapacity(in StreamedClipMemory memory, ClipCurves* clip)
        {
            return GetCurveCount(clip) <= memory.outputCapacity;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateCapacity(int curveCapacity, Allocator allocator)
        {
            if (curveCapacity < 0)
                throw new ArgumentOutOfRangeException("curveCapacity", "The curve capacity must be greater than or equal to zero.");
            if ((int)allocator <= (int)Allocator.None)
                throw new ArgumentException("The allocator must be a valid allocator.", "allocator");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateClipPtr(Clip* clip)
        {
            if (clip == null)
                throw new ArgumentNullException("clip", "The clip must not be null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateRootIndices(int rootTranslationIndex, int rootRotationIndex)
        {
            if (rootTranslationIndex < -1)
                throw new ArgumentOutOfRangeException("rootTranslationIndex", "The root translation index must be greater than or equal to -1.");
            if (rootRotationIndex < -1)
                throw new ArgumentOutOfRangeException("rootRotationIndex", "The root rotation index must be greater than or equal to -1.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateCacheForSampling(in StreamedClipMemory memory, ClipCurves* clip)
        {
            if (memory.rootMotionOnly)
                throw new InvalidOperationException("The ClipSamplingCache was initialized for root motion only and cannot sample a full clip.");
            if (memory.clip == null)
                throw new InvalidOperationException("The ClipSamplingCache is not initialized. Call Initialize with the clip before sampling.");
            if (memory.clip != clip || memory.curveCount != GetCurveCount(clip))
                throw new InvalidOperationException("The ClipSamplingCache was initialized for a different clip. Call Initialize with the clip before sampling.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateRootMotionCache(in StreamedClipMemory memory, ClipCurves* clip, int rootTranslationIndex, int rootRotationIndex, int rootRotationCurveCount = 4)
        {
            if (rootTranslationIndex < 0 && rootRotationIndex < 0)
                return;

            if (memory.rootMotionOnly)
            {
                var expectedTranslationIndex = rootTranslationIndex >= 0 ? rootTranslationIndex : int.MinValue;
                var expectedRotationIndex = rootRotationIndex >= 0 ? rootRotationIndex : int.MinValue;
                if (memory.rootTranslationIndex != expectedTranslationIndex || memory.rootRotationIndex != expectedRotationIndex)
                    throw new InvalidOperationException("The root indices do not match the ones passed to InitializeRootMotion.");
            }
            else if (memory.clip == null)
                throw new InvalidOperationException("The ClipSamplingCache is not initialized. Call Initialize or InitializeRootMotion before sampling.");
            else if (memory.clip != clip || memory.curveCount != GetCurveCount(clip))
                throw new InvalidOperationException("The ClipSamplingCache was initialized for a different clip. Call Initialize with the clip before sampling.");

            var curveCount = GetCurveCount(clip);
            if ((rootTranslationIndex >= 0 && rootTranslationIndex + 3 > curveCount) ||
                (rootRotationIndex >= 0 && rootRotationIndex + rootRotationCurveCount > curveCount))
                throw new IndexOutOfRangeException("The root motion curve indices are outside the curves of the clip.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsCreated()
        {
            if (m_Data == null)
                throw new NullReferenceException("The ClipSamplingCache is not created.");
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }
    }

    unsafe struct StreamedClipMemory
    {
        public StreamedCacheItem* caches;
        public UInt16             cacheCount;
        public UInt16             discreteCacheCount;
        public float              time;
        public UInt32             readByteOffset;

        [MarshalAs(UnmanagedType.U1)]
        public bool               rootMotionOnly;
        public int                rootTranslationIndex;
        public int                rootRotationIndex;

        public float* output;

        // Clip passed to ClipSamplingCache.Initialize and its total curve count, used only for validation
        public ClipCurves*      clip;
        public int                curveCount;
        public int                outputCapacity;

        // The cache data that holds this memory, so that sampling can re-initialize the cache for another clip.
        // Null for a memory laid out by hand
        public ClipSamplingCacheData* owner;

        [MarshalAs(UnmanagedType.U1)]
        public bool               capacityErrorLogged;
    };

    /// <summary>Extension methods for animation clips that give access to the clip data used for sampling.</summary>
    /// <remarks>
    /// Call <see cref="ToClipSampler(AnimationClip, BlobAssetReference{RigDefinition}, Allocator)"/> to sample an
    /// animation clip into an <see cref="AnimationStream"/>. Call <see cref="ToClipReference(AnimationClip)"/> to get a
    /// reference to the clip data of an animation clip. Use the reference, for example, to read the length of the clip
    /// or to store the reference in a component. Convert the reference with
    /// <see cref="BlobObjectReference{T}.AsBlobAssetReference"/> to read the data from jobs.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.IntegerTime`,
    /// and `UnityEngine` namespaces. It builds a rig that has one float channel and creates a
    /// <see cref="ClipSampler"/> from an animation clip. It then samples the clip into an animation stream. The
    /// example animates the `intensity` field of the `SampleClipExampleTarget` component. To run the example, add a
    /// `MonoBehaviour` named `SampleClipExampleTarget` that has a `float` field named `intensity`, or replace the
    /// component and the field with the ones that your animation clip animates.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipExample.cs#SampleClip}]]></code>
    /// </example>
    /// <seealso cref="ClipSampler"/>
    public static class AnimationClipExtensions
    {
        /// <summary>Acquires a reference to the sampling data of an animation clip.</summary>
        /// <remarks>
        /// Use this method to get the clip data in a form that jobs and Burst-compiled code can read. The reference
        /// keeps the data alive until you dispose it, even if the animation clip is destroyed or its data is replaced.
        /// Call this method from the main thread.
        /// </remarks>
        /// <param name="clip">The animation clip to get the data from. Can be null.</param>
        /// <returns>The reference to the <see cref="Clip"/> data of the animation clip. Dispose the reference when
        /// you finish with it. <see cref="BlobObjectReference{T}.IsValid"/> is `false` if <paramref name="clip"/> is
        /// `null`, is a legacy clip, has no data, or stores data with another layout version.</returns>
        public static BlobObjectReference<Clip> ToClipReference(this AnimationClip clip)
        {
            // Like an Animator, ignore the data of a legacy clip: a legacy clip is animated by the Animation component
            return clip != null && !clip.legacy ? clip.AcquireReference<Clip>(Clip.BlobVersion) : default;
        }

        /// <summary>Creates a <see cref="ClipSampler"/> that samples an animation clip for a rig.</summary>
        /// <remarks>
        /// Call this method from the main thread. The sampler acquires its own reference to the clip data. It keeps
        /// sampling that data even if the animation clip is destroyed, edited, or its data is replaced. To sample the
        /// edited clip, create the sampler again.
        ///
        /// This method is expensive. It matches every curve of the clip with the channels of the rig, and the time it
        /// takes grows with the number of curves times the number of channels. Create one sampler for each pair of
        /// clip and rig, for example when your content loads, and keep it. When you need more samplers for the same
        /// clip and rig, for example one for each character, call <see cref="ClipSampler.Clone"/> on the sampler you
        /// kept. A clone only copies memory.
        ///
        /// Dispose the sampler when you finish with it.
        ///
        /// Humanoid animation isn't supported yet. If <paramref name="clip"/> is a Humanoid clip, this method logs a
        /// warning and returns a sampler that isn't created.
        /// </remarks>
        /// <param name="clip">The animation clip to sample. Can be null.</param>
        /// <param name="rig">The rig of the animation streams to sample into. The sampler samples only the channels
        /// of the clip that the rig has.</param>
        /// <param name="allocator">The allocator to use for the memory of the sampler. Use a valid allocator.
        /// <see cref="Allocator.Invalid"/> and <see cref="Allocator.None"/> aren't allowed.</param>
        /// <returns>The new sampler. Dispose it when you finish with it. <see cref="ClipSampler.IsCreated"/> is
        /// `false` if <paramref name="clip"/> is `null`, is a Humanoid clip, has no data, or stores data with another
        /// layout version, or if `rig.IsCreated` is `false`.</returns>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and <paramref name="allocator"/>
        /// isn't a valid allocator.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="clip"/> is a legacy clip. A legacy
        /// clip is animated by the Animation component and can't be sampled by a <see cref="ClipSampler"/>.</exception>
        public static ClipSampler ToClipSampler(this AnimationClip clip, BlobAssetReference<RigDefinition> rig, Allocator allocator)
        {
            ClipSampler.ValidateAllocator(allocator);

            if (clip != null && clip.legacy)
                throw new InvalidOperationException($"The AnimationClip '{clip.name}' is a legacy clip, which can't be sampled. Clear AnimationClip.legacy to use the clip with Unity.Animations.");

            if (clip == null || !rig.IsCreated)
                return default;

            if (clip.humanMotion)
            {
                UnityEngine.Debug.LogWarning($"The AnimationClip '{clip.name}' is a Humanoid clip, which can't be sampled. Humanoid animation isn't supported by Unity.Animations.", clip);
                return default;
            }

            var clipReference = clip.ToClipReference();
            if (!clipReference.IsValid)
                return default;

            var clipInstance = ClipInstance.Create(rig, clip, allocator);
            if (!clipInstance.IsCreated)
            {
                clipReference.Dispose();
                return default;
            }

            return new ClipSampler(clipReference, clipInstance, allocator);
        }
    }

    /// <summary>
    /// Clip instance holds a list of matching bindings between the clip and rig in order to sample the data.
    /// Binding maps are sorted by type and chunked for efficient sampling.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    internal partial struct ClipInstance
    {
        internal const int k_BindingsInChunk = 4;

        /// <summary>Links the curves of a binding of the clip to a binding of the rig.</summary>
        /// <remarks>
        /// The binding maps of a <see cref="ClipInstance"/> contain one entry for each clip binding that has a
        /// matching rig binding.
        /// </remarks>
        public readonly struct MappedBinding
        {
            /// <summary>Index of the first curve of the clip binding in the curves of the clip.</summary>
            /// <remarks>
            /// A translation or scale binding uses 3 consecutive curves from this index: x, y, and z. A rotation binding
            /// uses 4 consecutive curves: x, y, z, and w of a quaternion. Any other binding uses 1 curve.
            /// </remarks>
            public readonly int ClipIndex;

            /// <summary>Index of the matching binding in the rig.</summary>
            /// <remarks>
            /// The index is in the list of rig bindings of the same kind: translation, rotation, scale, float, integer, or
            /// entity ID.
            /// </remarks>
            public readonly int RigIndex;

            internal MappedBinding(int clip, int rig)
            {
                ClipIndex = clip;
                RigIndex = rig;
            }
        }

        internal struct BindingInfo
        {
            public int ScaleOffset;
            public int FloatOffset;
            public int IntOffset;
            public int EntityIdOffset;
            public int RotationOffset;
            public int EulerRotationOffset;

            public ushort TranslationChunkCount;
            public ushort ScaleChunkCount;
            public ushort FloatChunkCount;
            public ushort IntChunkCount;
            public ushort EntityIdChunkCount;
            public ushort RotationChunkCount;
        }

        internal BlobArray<MappedBinding> m_BindingMap;
        internal BindingInfo              m_BindingInfo;

        // Flat PPtr → EntityId table sourced from AnimationClip.GetPPtrCurveMappingEntityIds().
        // Mecanim PPtr curves sample a 32-bit index (not the EntityId itself); at sample time
        // we dereference that index through this table to get the real EntityId.
        internal BlobArray<EntityId> m_PPtrEntityIds;

        // Parallel to EulerBindingMap
        internal BlobArray<math.RotationOrder> m_EulerRotationOrders;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EntityId ResolvePPtrEntityId(int index) =>
            (uint)index < (uint)m_PPtrEntityIds.Length ? m_PPtrEntityIds[index] : EntityId.None;

        internal Core.ClipRootMotionSamplingData RootMotionSamplingData { get; set; }

        // Address of the RigDefinition blob that the instance was built for. Only compared, never dereferenced
        internal ulong RigId;

        /// <summary>The bindings that map the translation curves of the clip to the rig.</summary>
        /// <remarks>
        /// Each entry uses 3 clip curves: x, y, and z. The array is a view of the memory of the clip instance. Do not
        /// dispose it, and do not use it after the clip instance is disposed.
        /// </remarks>
        public NativeArray<MappedBinding>.ReadOnly TranslationBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>(m_BindingMap.GetUnsafePtr(), m_BindingInfo.ScaleOffset, Allocator.None, true).AsReadOnly(); } }
        }

        /// <summary>The bindings that map the scale curves of the clip to the rig.</summary>
        /// <remarks>
        /// Each entry uses 3 clip curves: x, y, and z. The array is a view of the memory of the clip instance. Do not
        /// dispose it, and do not use it after the clip instance is disposed.
        /// </remarks>
        public NativeArray<MappedBinding>.ReadOnly ScaleBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.ScaleOffset, m_BindingInfo.FloatOffset - m_BindingInfo.ScaleOffset, Allocator.None, true).AsReadOnly(); } }
        }

        /// <summary>The bindings that map the float curves of the clip to the rig.</summary>
        /// <remarks>
        /// Each entry uses 1 clip curve. The array is a view of the memory of the clip instance. Do not dispose it, and
        /// do not use it after the clip instance is disposed.
        /// </remarks>
        public NativeArray<MappedBinding>.ReadOnly FloatBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.FloatOffset, m_BindingInfo.IntOffset - m_BindingInfo.FloatOffset, Allocator.None, true).AsReadOnly(); } }
        }

        /// <summary>The bindings that map the discrete integer curves of the clip to the rig.</summary>
        /// <remarks>
        /// Each entry uses 1 clip curve. The array is a view of the memory of the clip instance. Do not dispose it, and
        /// do not use it after the clip instance is disposed.
        /// </remarks>
        public NativeArray<MappedBinding>.ReadOnly IntBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.IntOffset, m_BindingInfo.EntityIdOffset - m_BindingInfo.IntOffset, Allocator.None, true).AsReadOnly(); } }
        }

        /// <summary>The bindings that map the object reference curves of the clip to the entity ID bindings of the
        /// rig.</summary>
        /// <remarks>
        /// Each entry uses 1 clip curve. The array is a view of the memory of the clip instance. Do not dispose it, and
        /// do not use it after the clip instance is disposed.
        /// </remarks>
        public NativeArray<MappedBinding>.ReadOnly EntityIdBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.EntityIdOffset, m_BindingInfo.RotationOffset - m_BindingInfo.EntityIdOffset, Allocator.None, true).AsReadOnly(); } }
        }

        /// <summary>The bindings that map the quaternion rotation curves of the clip to the rig.</summary>
        /// <remarks>
        /// Each entry uses 4 clip curves: x, y, z, and w of a quaternion. The array is a view of the memory of the clip
        /// instance. Do not dispose it, and do not use it after the clip instance is disposed.
        /// </remarks>
        public NativeArray<MappedBinding>.ReadOnly RotationBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.RotationOffset, m_BindingInfo.EulerRotationOffset - m_BindingInfo.RotationOffset, Allocator.None, true).AsReadOnly(); } }
        }

        internal NativeArray<MappedBinding>.ReadOnly EulerBindingMap
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return CollectionHelper.ConvertExistingDataToNativeArray<MappedBinding>((MappedBinding*)m_BindingMap.GetUnsafePtr() + m_BindingInfo.EulerRotationOffset, m_BindingMap.Length - m_BindingInfo.EulerRotationOffset, Allocator.None, true).AsReadOnly(); } }
        }

        /// <summary>The number of chunks of translation bindings in the translation binding map.</summary>
        /// <remarks>
        /// A chunk is a group of 4 bindings that can be sampled together. The chunks are at the start of the binding
        /// map, and the bindings after them are sampled one by one. <see cref="Create(BlobAssetReference{RigDefinition}, AnimationClip, Allocator)"/> does not group bindings into
        /// chunks, so the value is 0 for the clip instances that it creates.
        /// </remarks>
        public readonly ushort TranslationBindingChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BindingInfo.TranslationChunkCount;
        }

        /// <summary>The number of chunks of scale bindings in the scale binding map.</summary>
        /// <remarks>
        /// A chunk is a group of 4 bindings that can be sampled together. The chunks are at the start of the binding
        /// map, and the bindings after them are sampled one by one. <see cref="Create(BlobAssetReference{RigDefinition}, AnimationClip, Allocator)"/> does not group bindings into
        /// chunks, so the value is 0 for the clip instances that it creates.
        /// </remarks>
        public readonly ushort ScaleBindingChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BindingInfo.ScaleChunkCount;
        }
        /// <summary>The number of chunks of float bindings in the float binding map.</summary>
        /// <remarks>
        /// A chunk is a group of 4 bindings that can be sampled together. The chunks are at the start of the binding
        /// map, and the bindings after them are sampled one by one. <see cref="Create(BlobAssetReference{RigDefinition}, AnimationClip, Allocator)"/> does not group bindings into
        /// chunks, so the value is 0 for the clip instances that it creates.
        /// </remarks>
        public readonly ushort FloatBindingChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BindingInfo.FloatChunkCount;
        }

        /// <summary>The number of chunks of int bindings in the int binding map.</summary>
        /// <remarks>
        /// A chunk is a group of 4 bindings that can be sampled together. The chunks are at the start of the binding
        /// map, and the bindings after them are sampled one by one. <see cref="Create(BlobAssetReference{RigDefinition}, AnimationClip, Allocator)"/> does not group bindings into
        /// chunks, so the value is 0 for the clip instances that it creates.
        /// </remarks>
        public readonly ushort IntBindingChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BindingInfo.IntChunkCount;
        }

        /// <summary>The number of chunks of entity ID bindings in the entity ID binding map.</summary>
        /// <remarks>
        /// A chunk is a group of 4 bindings that can be sampled together. The chunks are at the start of the binding
        /// map, and the bindings after them are sampled one by one. <see cref="Create(BlobAssetReference{RigDefinition}, AnimationClip, Allocator)"/> does not group bindings into
        /// chunks, so the value is 0 for the clip instances that it creates.
        /// </remarks>
        public readonly ushort EntityIdBindingChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BindingInfo.EntityIdChunkCount;
        }

        /// <summary>The number of chunks of rotation bindings in the rotation binding map.</summary>
        /// <remarks>
        /// A chunk is a group of 4 bindings that can be sampled together. The chunks are at the start of the binding
        /// map, and the bindings after them are sampled one by one. <see cref="Create(BlobAssetReference{RigDefinition}, AnimationClip, Allocator)"/> does not group bindings into
        /// chunks, so the value is 0 for the clip instances that it creates.
        /// </remarks>
        public readonly ushort RotationBindingChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BindingInfo.RotationChunkCount;
        }
    }
}
