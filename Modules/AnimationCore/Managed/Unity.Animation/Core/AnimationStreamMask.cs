// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Animations.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>
    /// An AnimationStreamMask is a data proxy to a Rig's WeightData.
    /// It can be used to read and write individual weight mask rig channels.
    /// </summary>
    /// <remarks>
    /// A mask is a native container. How the safety system checks it depends on how you create it:
    ///
    ///- <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/> allocates a buffer that the mask owns.
    /// Call <see cref="Dispose()"/> or <see cref="Dispose(JobHandle)"/> to free it.
    ///- <see cref="Create(BlobAssetReference{RigDefinition}, NativeArray{WeightData})"/> and
    /// <see cref="CreateReadOnly(BlobAssetReference{RigDefinition}, NativeArray{WeightData})"/> return a view of the
    /// array that uses the safety handle of the array. The array keeps its memory, and you dispose it as usual.
    ///- <see cref="CreateReadOnly(BlobAssetReference{RigDefinition}, in WeightMaskSampler)"/> returns a view of the
    /// sampler that uses the safety handle of the sampler. The sampler keeps its memory, and you dispose it as usual.
    ///
    /// If safety checks are enabled, a mask throws an `ObjectDisposedException` if you use it after you dispose its
    /// memory.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.IntegerTime`,
    /// `Unity.Mathematics`, and `UnityEngine` namespaces. It is a `MonoBehaviour` that creates a weight mask for the
    /// upper body or the lower body of a character. Each frame, it samples two clips and blends their streams with a
    /// stream mask that views the weights of the mask.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BodyMaskExample.cs#MaskUpperOrLowerBody}]]></code>
    /// </example>
    [DebuggerTypeProxy(typeof(AnimationStreamMaskDebugView))]
    [GenerateTestsForBurstCompatibility]
    [NativeContainer]
    public struct AnimationStreamMask : IDisposable
    {
        const int k_BufferAlignment = 16;

        internal readonly BlobAssetReference<RigDefinition> m_Rig;
        internal readonly Ptr<float3> m_TranslationMasks;
        internal readonly Ptr<float3> m_ScaleMasks;
        internal readonly Ptr<float> m_FloatMasks;
        internal readonly Ptr<float> m_IntMasks;
        // Duplicate the weight of an EntityId inside a float2 because EntityId occupies 2 integers in memory, so it's
        // simpler and more SIMD-friendly to interpret an EntityId as a couple of integers, each of these 2 integers
        // having the same weight.
        // This way, a discrete chunk containing animated integers + EntityIds can be interpreted as a chunk of seamless
        // integers, which can be interpolated efficiently with SIMD.
        internal readonly Ptr<float2> m_EntityIdMasks;
        internal readonly Ptr<float> m_RotationMasks;

        [MarshalAs(UnmanagedType.U1)]
        readonly bool m_IsReadOnly;

        // Only a mask created with an allocator owns its buffer. The others keep Allocator.Invalid
        Allocator m_Allocator;

        // Lets an owning mask free its buffer after the rig is disposed
        int m_BufferLength;

        internal AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<AnimationStreamMask>();

        /// <summary>
        /// Whether the mask is read-only.
        /// </summary>
        public readonly bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsReadOnly;
        }

        /// <summary>
        /// Whether the mask has a rig definition and a buffer.
        /// </summary>
        /// <remarks>
        /// A mask isn't created if it is the default value, if a creation method returned a default mask, or after
        /// you call <see cref="Dispose()"/> on it.
        /// </remarks>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return m_Rig != default && m_TranslationMasks.m_Ptr != null; } }
        }

        /// <summary>
        /// Returns the <see cref="RigDefinition"/> used by this AnimationStreamMask.
        /// </summary>
        public readonly BlobAssetReference<RigDefinition> RigDefinition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig;
        }

        /// <summary>
        /// Returns a reference to the <see cref="RigDefinition"/>. This is used internally when implementing the
        /// AnimationStreamMask
        /// to prevent <see cref="BlobAssetReference{T}"/> defensive copies when accessing properties of the
        /// RigDefinition.
        /// </summary>
        /// <remarks>
        /// To clarify, the defensive copies generated in IL relate only to what a BlobAssetReference defines.
        /// They aren't a deep copy of the RigDefinition itself.
        /// </remarks>
        internal readonly ref RigDefinition Rig
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Rig.Value;
        }

        // Address of the RigDefinition blob, which identifies the rig
        internal readonly unsafe ulong RigId => (ulong)m_Rig.GetUnsafePtr();

        internal unsafe AnimationStreamMask(BlobAssetReference<RigDefinition> rig, void* ptr, bool isReadOnly)
        {
            ValidateEntityIdSize();

            ref var bindings = ref rig.Value.Bindings;
            float* floatPtr = (float*)ptr;

            var interpolatedDataOffset = 0;
            var discreteDataOffset = interpolatedDataOffset + bindings.InterpolatedDataChunkCount * BindingSet.k_WeightDataChunkSize;
            var rotationDataOffset = discreteDataOffset + bindings.DiscreteDataChunkCount * BindingSet.k_WeightDataChunkSize;

            var scaleDataOffset = interpolatedDataOffset + bindings.TranslationCurveCount;
            var floatDataOffset = scaleDataOffset + bindings.ScaleCurveCount;
            var objectIDDataOffset = discreteDataOffset + bindings.IntCurveCount;

            m_Rig = rig;
            m_TranslationMasks = new Ptr<float3>((float3*)floatPtr);
            m_ScaleMasks = new Ptr<float3>((float3*)(floatPtr + scaleDataOffset));
            m_FloatMasks = new Ptr<float>(floatPtr + floatDataOffset);
            m_IntMasks = new Ptr<float>(floatPtr + discreteDataOffset);
            m_EntityIdMasks = new Ptr<float2>((float2*)(floatPtr + objectIDDataOffset));
            m_RotationMasks = new Ptr<float>(floatPtr + rotationDataOffset);
            m_IsReadOnly = isReadOnly;
            m_Allocator = Allocator.Invalid;

            m_Safety = AtomicSafetyHandle.GetTempUnsafePtrSliceHandle();
            CollectionHelper.SetStaticSafetyId<AnimationStreamMask>(ref m_Safety, ref s_SafetyId.Data);
        }

        static unsafe AnimationStreamMask CreateView(BlobAssetReference<RigDefinition> rig, NativeArray<WeightData> buffer, void* ptr, bool isReadOnly)
        {
            var mask = new AnimationStreamMask(rig, ptr, isReadOnly);
            mask.m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(buffer);
            return mask;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateAllocator(Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("The allocator must be a valid allocator.", nameof(allocator));
        }

        /// <summary>
        /// Creates a read-write AnimationStreamMask from a RigDefinition and a NativeArray of WeightData.
        /// </summary>
        /// <param name="rig">The rig definition that describes the layout of the weight data.</param>
        /// <param name="buffer">The <see cref="NativeArray{T}"/> of <see cref="WeightData"/> that the mask
        /// views.</param>
        /// <returns></returns>
        public static AnimationStreamMask Create(BlobAssetReference<RigDefinition> rig, NativeArray<WeightData> buffer)
        {
            if (rig == default || buffer.Length == 0)
                return default;

            ValidateWeightDataLength(rig, buffer.Length);
            if (buffer.Length < rig.Value.Bindings.StreamMaskSize)
                return default;

            unsafe { return CreateView(rig, buffer, buffer.GetUnsafePtr(), false); }
        }

        /// <summary>
        /// Creates a read-write AnimationStreamMask that allocates and owns its buffer.
        /// </summary>
        /// <remarks>
        /// The method clears the buffer, which holds <see cref="BindingSet.StreamMaskSize"/> elements, so that every
        /// weight is 0. Call <see cref="Dispose()"/> or <see cref="Dispose(JobHandle)"/> on one copy of the mask to
        /// free the buffer. If safety checks are enabled, any copy of the mask throws an `ObjectDisposedException`
        /// when you use it after you dispose the mask.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the weight data.</param>
        /// <param name="allocator">The <see cref="Allocator"/> to use to allocate the buffer. The mask frees the
        /// buffer with the same allocator.</param>
        /// <returns>A read-write <see cref="AnimationStreamMask"/>. Returns a default mask if <paramref name="rig"/>
        /// is the default value.</returns>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and <paramref name="allocator"/> is
        /// <see cref="Allocator.Invalid"/> or <see cref="Allocator.None"/>.</exception>
        public static AnimationStreamMask Create(BlobAssetReference<RigDefinition> rig, Allocator allocator)
        {
            if (rig == default)
                return default;

            ValidateAllocator(allocator);

            var length = rig.Value.Bindings.StreamMaskSize;
            unsafe
            {
                var buffer = AllocateBuffer(length, allocator);
                UnsafeUtility.MemClear(buffer, UnsafeUtility.SizeOf<WeightData>() * length);

                var mask = new AnimationStreamMask(rig, buffer, false);
                mask.m_Allocator = allocator;
                mask.m_BufferLength = length;
                mask.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
                CollectionHelper.SetStaticSafetyId<AnimationStreamMask>(ref mask.m_Safety, ref s_SafetyId.Data);
                return mask;
            }
        }

        // An empty mask still gets a buffer, because the mask pointers must not be null
        static unsafe void* AllocateBuffer(int length, Allocator allocator) =>
            AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<WeightData>(), k_BufferAlignment, math.max(length, 1));

        internal static unsafe void FreeBuffer(void* buffer, int length, Allocator allocator) =>
            AllocatorManager.Free(allocator, buffer, UnsafeUtility.SizeOf<WeightData>(), k_BufferAlignment, math.max(length, 1));

        /// <summary>
        /// Releases the mask.
        /// </summary>
        /// <remarks>
        /// For a mask created with <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/>, this method
        /// frees the buffer. Copies of the mask share the buffer, so call this method on only one copy. For any other
        /// mask, this method resets only the copy that you call it on to a default mask. It doesn't touch the memory
        /// that the mask points to. If you call it on a default mask, it has no effect. A mask that owned its buffer is
        /// also a default mask after you dispose it. If safety checks are enabled and you call this method on it
        /// again, it throws an `ObjectDisposedException`.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if safety checks are enabled and the buffer of the mask is
        /// already freed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and a scheduled job still
        /// uses the buffer of the mask.</exception>
        public void Dispose()
        {
            if (!IsCreated)
            {
                ValidateNotAlreadyDisposed();
                return;
            }

            if (m_Allocator <= Allocator.None)
            {
                this = default;
                return;
            }

            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
            unsafe { FreeBuffer(m_TranslationMasks.m_Ptr, m_BufferLength, m_Allocator); }
            ResetAfterFree();
        }

        /// <summary>
        /// Releases the mask after a job completes.
        /// </summary>
        /// <remarks>
        /// For a mask created with <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/>, this method
        /// schedules a job that frees the buffer after <paramref name="inputDeps"/> completes. The mask resets to a
        /// default mask immediately, but the buffer isn't freed until the scheduled job runs. Pass the handles of
        /// all jobs that use the buffer of the mask as <paramref name="inputDeps"/>. For any other mask, this method
        /// resets only the copy that you call it on and returns <paramref name="inputDeps"/>.
        /// </remarks>
        /// <param name="inputDeps">The jobs that must complete before the buffer is freed.</param>
        /// <returns>The handle of the job that frees the buffer. Returns <paramref name="inputDeps"/> if the mask
        /// doesn't own its buffer or isn't created.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if safety checks are enabled and the buffer of the mask is
        /// already freed.</exception>
        [ExcludeFromBurstCompatTesting("Schedules a job")]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                ValidateNotAlreadyDisposed();
                return inputDeps;
            }

            if (m_Allocator <= Allocator.None)
            {
                this = default;
                return inputDeps;
            }

            JobHandle jobHandle;
            unsafe
            {
                var dispose = new AnimationStreamMaskDispose
                {
                    m_Buffer = m_TranslationMasks.m_Ptr,
                    m_Length = m_BufferLength,
                    m_Allocator = m_Allocator,
                    m_Safety = m_Safety,
                };
                jobHandle = new AnimationStreamMaskDisposeJob { Data = dispose }.Schedule(inputDeps);
            }

            AtomicSafetyHandle.Release(m_Safety);
            ResetAfterFree();
            return jobHandle;
        }

        // Keeps the released safety handle, so that disposing the same mask again throws
        void ResetAfterFree()
        {
            var safety = m_Safety;
            this = default;
            m_Safety = safety;
        }

        /// <summary>
        /// Creates a read-only AnimationStreamMask from a RigDefinition and a NativeArray of WeightData.
        /// </summary>
        /// <param name="rig">The rig definition that describes the layout of the weight data.</param>
        /// <param name="buffer">The <see cref="NativeArray{T}"/> of <see cref="WeightData"/> that the mask
        /// views.</param>
        /// <returns></returns>
        public static AnimationStreamMask CreateReadOnly(BlobAssetReference<RigDefinition> rig, NativeArray<WeightData> buffer)
        {
            if (rig == default || buffer.Length == 0)
                return default;

            ValidateWeightDataLength(rig, buffer.Length);
            if (buffer.Length < rig.Value.Bindings.StreamMaskSize)
                return default;

            unsafe { return CreateView(rig, buffer, buffer.GetUnsafeReadOnlyPtr(), true); }
        }

        /// <summary>
        /// Creates a read-write AnimationStreamMask from a RigDefinition and a WeightData pointer.
        /// </summary>
        /// <param name="rig">The rig definition that describes the layout of the weight data.</param>
        /// <param name="weightData">The weight data pointer.</param>
        /// <param name="length">The amount of weight data.</param>
        /// <returns></returns>
        internal static unsafe AnimationStreamMask Create(BlobAssetReference<RigDefinition> rig, WeightData* weightData, int length)
        {
            if (rig == default || length == 0)
                return default;

            ValidateWeightDataLength(rig, length);
            if (length < rig.Value.Bindings.StreamMaskSize)
                return default;

            return new AnimationStreamMask(rig, weightData, false);
        }

        /// <summary>
        /// Creates a read-only AnimationStreamMask that reads the weights of a <see cref="WeightMaskSampler"/>.
        /// </summary>
        /// <remarks>
        /// The mask is a view of the memory of the sampler and uses the safety handle of the sampler. Disposing the
        /// mask doesn't release the memory of the sampler. If safety checks are enabled, using the mask after you
        /// dispose the sampler throws an `ObjectDisposedException`.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the weight data. It must be the rig that
        /// the sampler was created for.</param>
        /// <param name="weightMask">The sampler that holds the weights.</param>
        /// <returns>The read-only mask. A default mask if <paramref name="rig"/> or <paramref name="weightMask"/>
        /// isn't created.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and
        /// <paramref name="weightMask"/> was created for another rig, or you disposed another copy of it.</exception>
        public static AnimationStreamMask CreateReadOnly(BlobAssetReference<RigDefinition> rig, in WeightMaskSampler weightMask)
        {
            if (rig == default || !weightMask.IsCreated)
                return default;

            weightMask.ValidateCanRead();
            weightMask.ValidateRig(rig);
            var mask = CreateReadOnly(rig, weightMask.Instance);
            if (mask.IsCreated)
                mask.m_Safety = weightMask.m_Safety;
            return mask;
        }

        internal static AnimationStreamMask CreateReadOnly(BlobAssetReference<RigDefinition> rig, BlobAssetReference<WeightMaskInstance> weightMask)
        {
            if (rig == default || !weightMask.IsCreated)
                return default;

            ValidateWeightDataLength(rig, weightMask.Value.Value.Length);
            if (weightMask.Value.Value.Length < rig.Value.Bindings.StreamMaskSize)
                return default;

            unsafe { return new AnimationStreamMask(rig, weightMask.Value.Value.GetUnsafePtr(), true); }
        }

        /// <summary>
        /// Returns the total bone count.
        /// </summary>
        public readonly int BoneCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Skeleton.BoneCount;
        }

        /// <summary>
        /// Returns the total translation count.
        /// </summary>
        public readonly int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.TranslationBindings.Length;
        }

        /// <summary>
        /// Returns the total rotation count.
        /// </summary>
        public readonly int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.RotationBindings.Length;
        }

        /// <summary>
        /// Returns the total scale count.
        /// </summary>
        public readonly int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.ScaleBindings.Length;
        }

        /// <summary>
        /// Returns the total generic float count
        /// </summary>
        public readonly int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.FloatBindings.Length;
        }

        /// <summary>
        /// Returns the total generic integer count
        /// </summary>
        public readonly int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.IntBindings.Length;
        }

        /// <summary>
        /// The number of entity ID channels that the mask holds a weight for.
        /// </summary>
        public readonly int EntityIdCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.EntityIdBindings.Length;
        }

        /// <summary>
        /// Returns the total weight data chunk count
        /// </summary>
        public readonly int WeightDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.StreamMaskSize / BindingSet.k_WeightDataChunkSize;
        }


        /// <summary>
        /// Returns the specified bone weights as a float3 containing
        /// translation, rotation and scale weights.
        /// </summary>
        /// <param name="index">The index of the bone to read the weights of.</param>
        /// <returns>The bone weights.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetBoneWeights(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            return math.float3(
                m_TranslationMasks.GetRO(index).x,
                m_RotationMasks.GetRO(index),
                m_ScaleMasks.GetRO(index).x
            );
        }

        /// <summary>
        /// Returns the specified translation weight.
        /// </summary>
        /// <param name="index">The index of the translation to read the weight of.</param>
        /// <returns>The translation weight.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetTranslationWeight(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForTranslation(index);

            return m_TranslationMasks.GetRO(index).x;
        }

        /// <summary>
        /// Returns the specified rotation weight.
        /// </summary>
        /// <param name="index">The index of the rotation to read the weight of.</param>
        /// <returns>The rotation weight.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetRotationWeight(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForRotation(index);

            return m_RotationMasks.GetRO(index);
        }

        /// <summary>
        /// Returns the specified scale weight.
        /// </summary>
        /// <param name="index">The index of the scale to read the weight of.</param>
        /// <returns>The scale weight.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetScaleWeight(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForScale(index);

            return m_ScaleMasks.GetRO(index).x;
        }

        /// <summary>
        /// Returns the specified float weight.
        /// </summary>
        /// <param name="index">The index of the float to read the weight of.</param>
        /// <returns>The float weight.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetFloatWeight(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForFloat(index);

            return m_FloatMasks.GetRO(index);
        }

        /// <summary>
        /// Returns the specified integer weight.
        /// </summary>
        /// <param name="index">The index of the integer to read the weight of.</param>
        /// <returns>The integer weight.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetIntWeight(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForInt(index);

            return m_IntMasks.GetRO(index);
        }

        /// <summary>
        /// Returns the weight of the specified entity ID channel.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="EntityIdCount"/>. Weights that you set with
        /// <see cref="SetEntityIdWeight"/> range from 0 to 1.
        /// </remarks>
        /// <param name="index">The index of the entity ID channel to read the weight of.</param>
        /// <returns>The weight of the entity ID channel, from 0 to 1.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if safety checks are enabled and
        /// <paramref name="index"/> is out of range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetEntityIdWeight(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForEntityId(index);

            return m_EntityIdMasks.GetRO(index).x;
        }

        /// <summary>
        /// Sets bone weights using a float3 containing translation, rotation and scale weights.
        /// </summary>
        /// <param name="index">The index of the bone to set the weights of.</param>
        /// <param name="weights">The translation, rotation, and scale weights of the bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBoneWeights(int index, in float3 weights)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weights);
            ValidateIndexBoundsForSkeleton(index);

            var w = math.saturate(weights);
            m_TranslationMasks.Set(index, w.x);
            m_RotationMasks.Set(index, w.y);
            m_ScaleMasks.Set(index, w.z);
        }

        /// <summary>
        /// Sets the weight of a specific translation.
        /// </summary>
        /// <param name="index">The index of the translation to set the weight of.</param>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTranslationWeight(int index, float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weight);
            ValidateIndexBoundsForTranslation(index);

            m_TranslationMasks.Set(index, math.saturate(weight));
        }

        /// <summary>
        /// Sets the weight of a specific rotation.
        /// </summary>
        /// <param name="index">The index of the rotation to set the weight of.</param>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotationWeight(int index, float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weight);
            ValidateIndexBoundsForRotation(index);

            m_RotationMasks.Set(index, math.saturate(weight));
        }

        /// <summary>
        /// Sets the weight of a specific scale.
        /// </summary>
        /// <param name="index">The index of the scale to set the weight of.</param>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScaleWeight(int index, float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weight);
            ValidateIndexBoundsForScale(index);

            m_ScaleMasks.Set(index, math.saturate(weight));
        }

        /// <summary>
        /// Sets the weight of a specific float.
        /// </summary>
        /// <param name="index">The index of the float to set the weight of.</param>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloatWeight(int index, float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weight);
            ValidateIndexBoundsForFloat(index);

            m_FloatMasks.Set(index, math.saturate(weight));
        }

        /// <summary>
        /// Sets the weight of a specific integer.
        /// </summary>
        /// <param name="index">The index of the integer to set the weight of.</param>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIntWeight(int index, float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weight);
            ValidateIndexBoundsForInt(index);

            m_IntMasks.Set(index, math.saturate(weight));
        }

        /// <summary>
        /// Sets the weight of the specified entity ID channel.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="EntityIdCount"/>.
        /// </remarks>
        /// <param name="index">The index of the entity ID channel to set the weight of.</param>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        /// <exception cref="NotFiniteNumberException">Thrown if safety checks are enabled and
        /// <paramref name="weight"/> isn't finite.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if safety checks are enabled and
        /// <paramref name="index"/> is out of range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEntityIdWeight(int index, float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(weight);
            ValidateIndexBoundsForEntityId(index);

            // Duplicate the weight of an EntityId in a float2 because EntityId are interpreted as couples of integers
            // by blending code.
            m_EntityIdMasks.Set(index, math.saturate(weight));
        }

        /// <summary>
        /// Returns the weight data as float4 chunks.
        /// </summary>
        public NativeArray<float4> WeightDataChunks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                ValidateIsWritable();
                unsafe { return AsArray<float4>(m_TranslationMasks.m_Ptr, WeightDataChunkCount); }
            }
        }

        // The array shares the safety handle of the mask, so it is checked like the mask
        readonly unsafe NativeArray<T> AsArray<T>(void* ptr, int length) where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
            return array;
        }

        /// <summary>
        /// Returns the read only weight data as float4 chunks.
        /// </summary>
        public readonly NativeArray<float4>.ReadOnly WeightDataChunksReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                unsafe { return AsArray<float4>(m_TranslationMasks.m_Ptr, WeightDataChunkCount).AsReadOnly(); }
            }
        }

        /// <summary>
        /// Clear all weight mask values.
        /// </summary>
        public void Clear()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            unsafe { UnsafeUtility.MemClear(m_TranslationMasks.m_Ptr, WeightDataChunkCount * UnsafeUtility.SizeOf<float4>()); }
        }

        /// <summary>
        /// Set all weight mask values to the specified weight.
        /// </summary>
        /// <param name="weight">The weight to set. The value is clamped to the range from 0 to 1.</param>
        public void SetAllWeightsTo(float weight)
        {
            ValidateIsNotNull();
            ValidateIsWritable();

            var w = math.float4(math.saturate(weight));

            var chunks = WeightDataChunks;
            for (int i = 0; i < chunks.Length; ++i)
                chunks[i] = w;
        }

        /// <summary>
        /// Copies all the data from the specified AnimationStreamMask into this AnimationStreamMask.
        /// </summary>
        /// <param name="other">The other AnimationStreamMask to copy.</param>
        public void CopyFrom(in AnimationStreamMask other)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(other);

            unsafe { UnsafeUtility.MemCpy(m_TranslationMasks.m_Ptr, other.m_TranslationMasks.m_Ptr, WeightDataChunkCount * UnsafeUtility.SizeOf<float4>()); }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateIsNotNull()
        {
            if (!IsCreated)
                throw new NullReferenceException("AnimationStreamMask is invalid, it contains a null rig definition.");

            CheckRead();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void CheckWrite()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateNotAlreadyDisposed()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsWritable()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("AnimationStreamMask is read only.");

            CheckWrite();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForSkeleton(int index)
        {
            if ((uint)index >= BoneCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Skeleton index '{index}' is out of range of '{BoneCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForTranslation(int index)
        {
            if ((uint)index >= TranslationCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Translation index '{index}' is out of range of '{TranslationCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForRotation(int index)
        {
            if ((uint)index >= RotationCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Rotation index '{index}' is out of range of '{RotationCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForScale(int index)
        {
            if ((uint)index >= ScaleCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Scale index '{index}' is out of range of '{ScaleCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForFloat(int index)
        {
            if ((uint)index >= FloatCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Float index '{index}' is out of range of '{FloatCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForInt(int index)
        {
            if ((uint)index >= IntCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Int index '{index}' is out of range of '{IntCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIndexBoundsForEntityId(int index)
        {
            if ((uint)index >= EntityIdCount)
                throw new IndexOutOfRangeException($"AnimationStreamMask: Int index '{index}' is out of range of '{EntityIdCount}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsFinite(float value)
        {
            if (!math.isfinite(value))
                throw new NotFiniteNumberException($"AnimationStreamMask: float is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateIsFinite(in float3 value)
        {
            if (!math.all(math.isfinite(value)))
                throw new System.NotFiniteNumberException($"AnimationStreamMask: float3 is not finite");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateCompatibility(in AnimationStreamMask other)
        {
            other.ValidateIsNotNull();

            // Validate data layout compatibility
            if (TranslationCount != other.TranslationCount ||
                RotationCount != other.RotationCount ||
                ScaleCount != other.ScaleCount ||
                FloatCount != other.FloatCount ||
                IntCount != other.IntCount ||
                EntityIdCount != other.EntityIdCount)
                throw new InvalidOperationException("AnimationStreamMask data layouts are not compatible.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateWeightDataLength(BlobAssetReference<RigDefinition> rig, int length)
        {
            if (length != rig.Value.Bindings.StreamMaskSize)
                throw new InvalidOperationException($"AnimationStreamMask: weight data length '{length}' does not match the RigDefinition stream mask size '{rig.Value.Bindings.StreamMaskSize}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateEntityIdSize()
        {
            if (UnsafeUtility.SizeOf<EntityId>() != 2 * sizeof(int))
            {
                throw new InvalidOperationException(
                    $"{nameof(EntityId)} should be the size of 2 integers for AnimationStreamMasks to work properly.");
            }
        }


    }

    [NativeContainer]
    unsafe struct AnimationStreamMaskDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;
        internal int m_Length;
        internal Allocator m_Allocator;

        internal AtomicSafetyHandle m_Safety;

        public void Dispose() => AnimationStreamMask.FreeBuffer(m_Buffer, m_Length, m_Allocator);
    }

    struct AnimationStreamMaskDisposeJob : IJob
    {
        internal AnimationStreamMaskDispose Data;

        public void Execute() => Data.Dispose();
    }

    sealed class AnimationStreamMaskDebugView
    {
        public AnimationStreamMaskDebugView(AnimationStreamMask streamMask)
        {
            BoneCount = streamMask.BoneCount;

            TranslationCount = streamMask.TranslationCount;
            TranslationWeights = new float[TranslationCount];
            for (var i = 0; i < TranslationCount; i++)
                TranslationWeights[i] = streamMask.GetTranslationWeight(i);

            RotationCount = streamMask.RotationCount;
            RotationWeights = new float[RotationCount];
            for (var i = 0; i < RotationCount; i++)
                RotationWeights[i] = streamMask.GetRotationWeight(i);

            ScaleCount = streamMask.ScaleCount;
            ScaleWeights = new float[ScaleCount];
            for (var i = 0; i < ScaleCount; i++)
                ScaleWeights[i] = streamMask.GetScaleWeight(i);

            FloatCount = streamMask.FloatCount;
            FloatWeights = new float[FloatCount];
            for (var i = 0; i < FloatCount; i++)
                FloatWeights[i] = streamMask.GetFloatWeight(i);

            IntCount = streamMask.IntCount;
            IntWeights = new float[IntCount];
            for (var i = 0; i < IntCount; i++)
                IntWeights[i] = streamMask.GetIntWeight(i);

            EntityIdCount = streamMask.EntityIdCount;
            EntityIdWeights = new float[EntityIdCount];
            for (var i = 0; i < EntityIdCount; i++)
                EntityIdWeights[i] = streamMask.GetEntityIdWeight(i);
        }

        public int BoneCount { get; }

        public int TranslationCount { get; }
        public float[] TranslationWeights { get; }

        public int RotationCount { get; }
        public float[] RotationWeights { get; }

        public int ScaleCount { get; }
        public float[] ScaleWeights { get; }

        public int FloatCount { get; }
        public float[] FloatWeights { get; }

        public int IntCount { get; }
        public float[] IntWeights { get; }

        public int EntityIdCount { get; }
        public float[] EntityIdWeights { get; }
    }
}
