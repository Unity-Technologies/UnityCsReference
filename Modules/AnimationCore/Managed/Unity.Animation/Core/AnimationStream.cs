// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Animations.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    unsafe readonly struct Ptr<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        readonly internal T* m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ptr(T* ptr)
        {
            Core.ValidateIsNotNull(ptr);
            m_Ptr = ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get()
        {
            Core.ValidateIsNotNull(m_Ptr);
            return ref UnsafeUtility.AsRef<T>(m_Ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly T GetRO(int index)
        {
            Core.ValidateIsNotNull(m_Ptr);
            return ref UnsafeUtility.AsRef<T>(m_Ptr + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T GetRW(int index)
        {
            Core.ValidateIsNotNull(m_Ptr);
            return ref UnsafeUtility.AsRef<T>(m_Ptr + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Set(int index, in T value)
        {
            Core.ValidateIsNotNull(m_Ptr);
            UnsafeUtility.AsRef<T>(m_Ptr + index) = value;
        }
    }

    /// <summary>
    /// A proxy for reading and writing the animated channel values of a rig, such as translations, rotations, floats,
    /// integers, and entity IDs.
    /// </summary>
    /// <remarks>
    /// This is `Unity.Animations.AnimationStream`. It is not <see cref="UnityEngine.Animations.AnimationStream"/>,
    /// which an `IAnimationJob` receives from an Animator in a PlayableGraph. That type doesn't work with the functions
    /// of this namespace.
    ///
    /// A stream gives you access to the animated values of one rig. It holds a value for each translation, rotation,
    /// and scale binding of the rig. It also holds the float, integer, and entity ID channels that the rig defines.
    ///
    /// <see cref="ClipSampler"/> samples an animation clip into a stream. The methods of
    /// <see cref="AnimationStreamBlendingExtensions"/> blend streams. Call <see cref="FromDefaultValues"/> to read the
    /// default values of a rig.
    ///
    /// To create a stream, pass a reference to a <see cref="RigDefinition"/> and either an allocator or a buffer of
    /// <see cref="AnimatedData"/> with <see cref="BindingSet.StreamSize"/> elements. To read or write a channel, find
    /// its index with <see cref="BindingUtility.FindBindingIndex"/>, then use a method such as
    /// <see cref="GetFloat(int)"/> or <see cref="SetFloat(int, float)"/>.
    ///
    /// A stream is a native container. How the safety system checks it depends on how you create it:
    ///
    ///- <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/> allocates a buffer that the stream owns.
    /// Call <see cref="Dispose()"/> or <see cref="Dispose(JobHandle)"/> to free it.
    ///- <see cref="Create(BlobAssetReference{RigDefinition}, NativeArray{AnimatedData})"/> and
    /// <see cref="CreateReadOnly(BlobAssetReference{RigDefinition}, NativeArray{AnimatedData})"/> return a view of the
    /// array that uses the safety handle of the array. The array keeps its memory, and you dispose it as usual.
    ///- <see cref="FromDefaultValues"/> and <see cref="GetDefaultValues"/> return streams that the safety system can't
    /// check.
    ///
    /// If safety checks are enabled, a stream throws an `ObjectDisposedException` if you use it after you dispose its
    /// memory. The job system throws an `InvalidOperationException` if two jobs that can run at the same time write to
    /// copies of the same stream. Safety checks are always enabled in the Editor and never in a built Player.
    ///
    /// The job system rejects a job field that holds a stream the safety system can't check. To use such a stream in a
    /// job field, mark the field with `[NativeDisableContainerSafetyRestriction]`. You are then responsible for
    /// keeping the memory valid and for avoiding concurrent writes.
    ///
    /// The following example requires the `Unity.Burst`, `Unity.Collections`, `Unity.Collections.LowLevel.Unsafe`,
    /// `Unity.Entities`, and `Unity.Jobs` namespaces. It runs one job on a stream that owns its buffer, and one job
    /// on a stream that views the default values of a rig. The safety system checks only the first stream.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/AnimationStreamJobExample.cs#AnimationStreamJobs}]]></code>
    /// </example>
    /// <seealso cref="AnimationStreamMask"/>
    /// <seealso cref="RigDefinition"/>
    /// <seealso cref="AnimatedData"/>
    [DebuggerTypeProxy(typeof(AnimationStreamDebugView))]
    [GenerateTestsForBurstCompatibility]
    [NativeContainer]
    public partial struct AnimationStream : IDisposable
    {
        const int k_InvalidIndex = -1;
        const int k_BufferAlignment = 16;

        /// <summary>
        /// Channel Pass masks keep track of which channels have been modified in a pass (ProcessDefaultAnimationGraph vs ProcessLateAnimationGraph).
        /// </summary>
        internal UnsafeBitArray m_ChannelPassMasks;

        /// <summary>
        /// Channel Frame masks keep track of which channels have been modified in a frame.
        /// </summary>
        internal UnsafeBitArray m_ChannelFrameMasks;

        internal readonly BlobAssetReference<RigDefinition> m_Rig;
        internal readonly Ptr<float3> m_LocalTranslationData;
        internal readonly Ptr<float3> m_LocalScaleData;
        internal readonly Ptr<float> m_FloatData;
        internal readonly Ptr<int> m_IntData;
        internal readonly Ptr<EntityId> m_EntityIdData;
        internal readonly Ptr<quaternion4> m_LocalRotationData;

        [MarshalAs(UnmanagedType.U1)]
        readonly bool m_IsReadOnly;

        // Only a stream created with an allocator owns its buffer. The others keep Allocator.Invalid
        Allocator m_Allocator;

        // Lets an owning stream free its buffer after the rig is disposed
        int m_BufferLength;

        internal AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<AnimationStream>();

        /// <summary>
        /// Whether the stream is read-only.
        /// </summary>
        public readonly bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsReadOnly;
        }

        /// <summary>
        /// Whether the stream has a rig definition and a buffer.
        /// </summary>
        /// <remarks>
        /// A stream isn't created if it is the default value, if a creation method returned a default stream, or after
        /// you call <see cref="Dispose()"/> on it.
        /// </remarks>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return m_Rig != default && m_LocalTranslationData.m_Ptr != null; } }
        }

        /// <summary>
        /// Returns the <see cref="RigDefinition"/> used by this AnimationStream.
        /// </summary>
        public readonly BlobAssetReference<RigDefinition> RigDefinition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig;
        }

        /// <summary>
        /// Returns a reference to the <see cref="RigDefinition"/>. This is used internally when implementing the
        /// AnimationStream to prevent <see cref="BlobAssetReference{T}"/> defensive copies when accessing properties of
        /// the RigDefinition.
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

        internal unsafe AnimationStream(BlobAssetReference<RigDefinition> rig, void* ptr, bool isReadOnly)
        {
            ref var bindings = ref rig.Value.Bindings;
            float* floatPtr = (float*)ptr;

            var maskSizeInBytes = Core.AlignUp(bindings.BindingCount, 64) / 8;
            var maskSizeIn4Bytes = Core.AlignUp(bindings.BindingCount, 64) / 32;

            m_Rig = rig;
            m_LocalTranslationData = new Ptr<float3>((float3*)(floatPtr + bindings.TranslationSamplesOffset));
            m_LocalScaleData = new Ptr<float3>((float3*)(floatPtr + bindings.ScaleSamplesOffset));
            m_FloatData = new Ptr<float>((floatPtr + bindings.FloatSamplesOffset));
            m_IntData = new Ptr<int>((int*)(floatPtr + bindings.IntSamplesOffset));
            m_EntityIdData = new Ptr<EntityId>((EntityId*)(floatPtr + bindings.EntityIdSamplesOffset));
            m_LocalRotationData = new Ptr<quaternion4>((quaternion4*)(floatPtr + bindings.RotationSamplesOffset));
            m_ChannelPassMasks = new UnsafeBitArray(floatPtr + bindings.ChannelMaskOffset, maskSizeInBytes, AllocatorManager.None);
            m_ChannelFrameMasks = new UnsafeBitArray(floatPtr + bindings.ChannelMaskOffset + maskSizeIn4Bytes, maskSizeInBytes, AllocatorManager.None);
            m_IsReadOnly = isReadOnly;
            m_Allocator = Allocator.Invalid;

            m_Safety = AtomicSafetyHandle.GetTempUnsafePtrSliceHandle();
            CollectionHelper.SetStaticSafetyId<AnimationStream>(ref m_Safety, ref s_SafetyId.Data);
        }

        static unsafe AnimationStream CreateView(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer, void* ptr, bool isReadOnly)
        {
            var stream = new AnimationStream(rig, ptr, isReadOnly);
            stream.m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(buffer);
            return stream;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateAllocator(Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("The allocator must be a valid allocator.", nameof(allocator));
        }

        // Single place for the buffer size rule, so that every creation path validates the same way.
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateAnimatedDataLength(BlobAssetReference<RigDefinition> rig, int length)
        {
            if (length != rig.Value.Bindings.StreamSize)
                throw new InvalidOperationException($"AnimationStream: animated data length '{length}' does not match the RigDefinition stream size '{rig.Value.Bindings.StreamSize}'.");
        }

        /// <summary>
        /// Creates a read-write AnimationStream from a RigDefinition and a pointer to AnimatedData.
        /// </summary>
        /// <remarks>
        /// The stream reads and writes the memory that <paramref name="buffer"/> points to. It doesn't copy the data or
        /// take ownership of it, so keep the memory valid while you use the stream. The method can't check the size of
        /// the buffer, so make sure that it holds <see cref="BindingSet.StreamSize"/> elements for
        /// <paramref name="rig"/>. If the buffer is smaller, the stream reads and writes memory outside it.
        ///
        /// Use this overload, for example, when the animated data is part of a larger buffer that isn't a `NativeArray`
        /// of exactly one stream.
        ///
        /// The safety system can't check this stream. To use it in a job field, mark the field with
        /// `[NativeDisableContainerSafetyRestriction]`.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the animated data in the buffer.</param>
        /// <param name="buffer">The pointer to the first element of the <see cref="AnimatedData"/> buffer.</param>
        /// <returns>A read-write <see cref="AnimationStream"/>. Returns a default stream if <paramref name="rig"/> is
        /// the default value or <paramref name="buffer"/> is `null`.</returns>
        internal static unsafe AnimationStream Create(BlobAssetReference<RigDefinition> rig, AnimatedData* buffer)
        {
            if (rig == default || buffer == null)
                return default;

            return new AnimationStream(rig, buffer, false);
        }

        /// <summary>
        /// Creates a read-write AnimationStream from a RigDefinition and a NativeArray of AnimatedData.
        /// </summary>
        /// <remarks>
        /// The stream is a view of <paramref name="buffer"/> and uses its safety handle. If safety checks are enabled,
        /// using the stream after you dispose the array throws an `ObjectDisposedException`. Disposing the stream
        /// doesn't dispose the array.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the animated data in
        /// <paramref name="buffer"/>.</param>
        /// <param name="buffer">The <see cref="NativeArray{T}"/> of <see cref="AnimatedData"/> that the stream views.
        /// Its length must equal <see cref="BindingSet.StreamSize"/>.</param>
        /// <returns>A read-write <see cref="AnimationStream"/>. Returns a default stream if <paramref name="rig"/> is
        /// the default value, or if <paramref name="buffer"/> is empty and the rig has a nonempty stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the length of
        /// <paramref name="buffer"/> differs from <see cref="BindingSet.StreamSize"/>. An empty buffer for a rig whose
        /// stream isn't empty doesn't throw, and the method returns a default stream instead. Without safety checks,
        /// the method returns a default stream for any length mismatch.</exception>
        public static AnimationStream Create(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer)
        {
            if (rig == default || (buffer.Length == 0 && rig.Value.Bindings.StreamSize != 0))
                return default;

            ValidateAnimatedDataLength(rig, buffer.Length);
            if (buffer.Length != rig.Value.Bindings.StreamSize)
                return default;

            unsafe { return CreateView(rig, buffer, buffer.GetUnsafePtr(), false); }
        }

        /// <summary>
        /// Creates a read-write AnimationStream from a RigDefinition and an UnsafeList of AnimatedData.
        /// </summary>
        /// <remarks>
        /// The stream reads and writes the memory of <paramref name="buffer"/>. The stream doesn't copy the data or
        /// take ownership of the list. The list must stay valid, and must not reallocate, while you use the stream.
        ///
        /// The safety system can't check this stream. To use it in a job field, mark the field with
        /// `[NativeDisableContainerSafetyRestriction]`.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the animated data in
        /// <paramref name="buffer"/>.</param>
        /// <param name="buffer">The <see cref="UnsafeList{T}"/> of <see cref="AnimatedData"/> that the stream views.
        /// Its length must equal <see cref="BindingSet.StreamSize"/>.</param>
        /// <returns>A read-write <see cref="AnimationStream"/>. Returns a default stream if <paramref name="rig"/> is
        /// the default value, or if <paramref name="buffer"/> is empty and the rig has a nonempty stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the length of
        /// <paramref name="buffer"/> differs from <see cref="BindingSet.StreamSize"/>. An empty buffer for a rig whose
        /// stream isn't empty doesn't throw, and the method returns a default stream instead. Without safety checks,
        /// the method returns a default stream for any length mismatch.</exception>
        internal static AnimationStream Create(BlobAssetReference<RigDefinition> rig, UnsafeList<AnimatedData> buffer)
        {
            if (rig == default || (buffer.Length == 0 && rig.Value.Bindings.StreamSize != 0))
                return default;

            ValidateAnimatedDataLength(rig, buffer.Length);
            if (buffer.Length != rig.Value.Bindings.StreamSize)
                return default;

            unsafe { return new AnimationStream(rig, buffer.Ptr, false); }
        }

        /// <summary>
        /// Creates a read-only AnimationStream from a RigDefinition and a NativeArray of AnimatedData.
        /// </summary>
        /// <remarks>
        /// The stream is a view of <paramref name="buffer"/> and uses its safety handle. If safety checks are enabled,
        /// using the stream after you dispose the array throws an `ObjectDisposedException`. Disposing the stream
        /// doesn't dispose the array.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the animated data in
        /// <paramref name="buffer"/>.</param>
        /// <param name="buffer">The <see cref="NativeArray{T}"/> of <see cref="AnimatedData"/> that the stream views.
        /// Its length must equal <see cref="BindingSet.StreamSize"/>.</param>
        /// <returns>A read-only <see cref="AnimationStream"/>. Returns a default stream if <paramref name="rig"/> is
        /// the default value, or if <paramref name="buffer"/> is empty and the rig has a nonempty stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the length of
        /// <paramref name="buffer"/> differs from <see cref="BindingSet.StreamSize"/>. An empty buffer for a rig whose
        /// stream isn't empty doesn't throw, and the method returns a default stream instead. Without safety checks,
        /// the method returns a default stream for any length mismatch.</exception>
        public static AnimationStream CreateReadOnly(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer)
        {
            if (rig == default || (buffer.Length == 0 && rig.Value.Bindings.StreamSize != 0))
                return default;

            ValidateAnimatedDataLength(rig, buffer.Length);
            if (buffer.Length != rig.Value.Bindings.StreamSize)
                return default;

            unsafe { return CreateView(rig, buffer, buffer.GetUnsafeReadOnlyPtr(), true); }
        }

        /// <summary>
        /// Creates a read-only AnimationStream from a RigDefinition and an UnsafeList of AnimatedData.
        /// </summary>
        /// <remarks>
        /// The stream reads the memory of <paramref name="buffer"/>. The stream doesn't copy the data or take
        /// ownership of the list. The list must stay valid, and must not reallocate, while you use the stream.
        ///
        /// The safety system can't check this stream. To use it in a job field, mark the field with
        /// `[NativeDisableContainerSafetyRestriction]`.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the animated data in
        /// <paramref name="buffer"/>.</param>
        /// <param name="buffer">The <see cref="UnsafeList{T}"/> of <see cref="AnimatedData"/> that the stream views.
        /// Its length must equal <see cref="BindingSet.StreamSize"/>.</param>
        /// <returns>A read-only <see cref="AnimationStream"/>. Returns a default stream if <paramref name="rig"/> is
        /// the default value, or if <paramref name="buffer"/> is empty and the rig has a nonempty stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and the length of
        /// <paramref name="buffer"/> differs from <see cref="BindingSet.StreamSize"/>. An empty buffer for a rig whose
        /// stream isn't empty doesn't throw, and the method returns a default stream instead. Without safety checks,
        /// the method returns a default stream for any length mismatch.</exception>
        internal static AnimationStream CreateReadOnly(BlobAssetReference<RigDefinition> rig, UnsafeList<AnimatedData> buffer)
        {
            if (rig == default || (buffer.Length == 0 && rig.Value.Bindings.StreamSize != 0))
                return default;

            ValidateAnimatedDataLength(rig, buffer.Length);
            if (buffer.Length != rig.Value.Bindings.StreamSize)
                return default;

            unsafe { return new AnimationStream(rig, buffer.Ptr, true); }
        }

        /// <summary>
        /// Creates a read-only AnimationStream from the default values of a RigDefinition.
        /// </summary>
        /// <remarks>
        /// The stream reads the default values that are stored in <paramref name="rig"/>. It doesn't allocate or copy
        /// them. Use <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/> when you need default values
        /// that you can write to.
        ///
        /// The safety system can't check this stream. To use it in a job field, mark the field with
        /// `[NativeDisableContainerSafetyRestriction]`.
        /// </remarks>
        /// <param name="rig">The rig definition whose default values the stream views.</param>
        /// <returns>A read-only <see cref="AnimationStream"/> that views the default values of
        /// <paramref name="rig"/>. Returns a default stream if <paramref name="rig"/> is the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AnimationStream FromDefaultValues(BlobAssetReference<RigDefinition> rig)
        {
            unsafe { return rig == default ? default : new AnimationStream(rig, rig.Value.DefaultValues.GetUnsafePtr(), true); }
        }

        /// <summary>
        /// Creates a read-write AnimationStream that allocates and owns its buffer.
        /// </summary>
        /// <remarks>
        /// The method fills the buffer, which holds <see cref="BindingSet.StreamSize"/> elements, with the default
        /// values of the rig, and clears the channel masks. Call <see cref="Dispose()"/> or
        /// <see cref="Dispose(JobHandle)"/> on one copy of the stream to free the buffer. If safety checks are enabled,
        /// any copy of the stream throws an `ObjectDisposedException` when you use it after you dispose the stream.
        /// </remarks>
        /// <param name="rig">The rig definition that describes the layout of the animated data.</param>
        /// <param name="allocator">The <see cref="Allocator"/> to use to allocate the buffer. The stream frees the
        /// buffer with the same allocator.</param>
        /// <returns>A read-write <see cref="AnimationStream"/>. Returns a default stream if <paramref name="rig"/> is
        /// the default value.</returns>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and <paramref name="allocator"/> is
        /// <see cref="Allocator.Invalid"/> or <see cref="Allocator.None"/>.</exception>
        public static AnimationStream Create(BlobAssetReference<RigDefinition> rig, Allocator allocator)
        {
            if (rig == default)
                return default;

            ValidateAllocator(allocator);

            var length = rig.Value.Bindings.StreamSize;
            unsafe
            {
                var buffer = AllocateBuffer(length, allocator);
                UnsafeUtility.MemCpy(buffer, rig.Value.DefaultValues.GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * length);

                var stream = new AnimationStream(rig, buffer, false);
                stream.m_ChannelPassMasks.Clear();
                stream.m_ChannelFrameMasks.Clear();
                stream.m_Allocator = allocator;
                stream.m_BufferLength = length;
                stream.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
                CollectionHelper.SetStaticSafetyId<AnimationStream>(ref stream.m_Safety, ref s_SafetyId.Data);
                return stream;
            }
        }

        // An empty stream still gets a buffer, because the stream pointers must not be null
        static unsafe void* AllocateBuffer(int length, Allocator allocator) =>
            AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<AnimatedData>(), k_BufferAlignment, math.max(length, 1));

        internal static unsafe void FreeBuffer(void* buffer, int length, Allocator allocator) =>
            AllocatorManager.Free(allocator, buffer, UnsafeUtility.SizeOf<AnimatedData>(), k_BufferAlignment, math.max(length, 1));

        /// <summary>
        /// Releases the stream.
        /// </summary>
        /// <remarks>
        /// For a stream created with <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/>, this method
        /// frees the buffer. Copies of the stream share the buffer, so call this method on only one copy. For any other
        /// stream, this method resets only the copy that you call it on to a default stream. It doesn't touch the
        /// memory that the stream points to. If you call it on a default stream, it has no effect. A stream that owned
        /// its buffer is also a default stream after you dispose it. If safety checks are enabled and you call this method
        /// on it again, it throws an `ObjectDisposedException`.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if safety checks are enabled and the buffer of the stream
        /// is already freed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and a scheduled job still
        /// uses the stream.</exception>
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
            unsafe { FreeBuffer(this.GetUnsafePtr(), m_BufferLength, m_Allocator); }
            ResetAfterFree();
        }

        /// <summary>
        /// Releases the stream after a job completes.
        /// </summary>
        /// <remarks>
        /// For a stream created with <see cref="Create(BlobAssetReference{RigDefinition}, Allocator)"/>, this method
        /// schedules a job that frees the buffer after <paramref name="inputDeps"/> completes. The stream resets to a
        /// default stream immediately, but the buffer isn't freed until the scheduled job runs. Pass the handles of
        /// all jobs that use the stream or its copies as <paramref name="inputDeps"/>. For any other stream, this
        /// method resets only the copy that you call it on and returns <paramref name="inputDeps"/>.
        /// </remarks>
        /// <param name="inputDeps">The jobs that must complete before the buffer is freed.</param>
        /// <returns>The handle of the job that frees the buffer. Returns <paramref name="inputDeps"/> if the stream
        /// doesn't own its buffer or isn't created.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if safety checks are enabled and the buffer of the stream
        /// is already freed.</exception>
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
                var dispose = new AnimationStreamDispose
                {
                    m_Buffer = this.GetUnsafePtr(),
                    m_Length = m_BufferLength,
                    m_Allocator = m_Allocator,
                    m_Safety = m_Safety,
                };
                jobHandle = new AnimationStreamDisposeJob { Data = dispose }.Schedule(inputDeps);
            }

            AtomicSafetyHandle.Release(m_Safety);
            ResetAfterFree();
            return jobHandle;
        }

        // Keeps the released safety handle, so that disposing the same stream again throws
        void ResetAfterFree()
        {
            var safety = m_Safety;
            this = default;
            m_Safety = safety;
        }

        /// <summary>
        /// Copies all the data from the specified AnimationStream into this AnimationStream.
        /// This includes all the animation channel data and channel masks.
        /// </summary>
        /// <param name="other">The stream to copy the data and channel masks from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(in AnimationStream other)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(other);

            unsafe { UnsafeUtility.MemCpy(this.GetUnsafePtr(), other.GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * other.Rig.Bindings.StreamSize); }
        }

        /// <summary>
        /// Returns a read-only AnimationStream containing the default values of the RigDefintion.
        /// </summary>
        /// <returns>A read-only AnimationStream initialized with the rig's default values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AnimationStream GetDefaultValues()
        {
            ValidateIsNotNull();
            unsafe { return new AnimationStream(m_Rig, Rig.DefaultValues.GetUnsafePtr(), true); }
        }

        /// <summary>
        /// Reset the AnimationStream to 0. This includes all animation channel data and channel masks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetToZero()
        {
            ValidateIsNotNull();
            ValidateIsWritable();

            unsafe { UnsafeUtility.MemClear(this.GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * Rig.Bindings.StreamSize); }
        }

        /// <summary>
        /// Reset the stream's rotation to identity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetRotationsToIdentity()
        {
            ValidateIsNotNull();
            ValidateIsWritable();

            for (var i = 0; i < Rig.Bindings.RotationBindings.Length; ++i)
            {
                SetLocalToParentRotation(i, quaternion.identity);
            }
        }

        /// <summary>
        /// Resets the channel values of the stream to the default values of the rig, and clears the channel masks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetToDefaultValues()
        {
            ValidateIsNotNull();
            ValidateIsWritable();

            ClearMasks();

            unsafe { UnsafeUtility.MemCpy(this.GetUnsafePtr(), Rig.DefaultValues.GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * Rig.Bindings.ChannelSize); }
        }

        /// <summary>
        /// Returns the local to parent translation of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="TranslationCount"/>.
        /// </remarks>
        /// <param name="index">The index of the translation channel. The first <see cref="Skeleton.BoneCount"/>
        /// translation channels are the bones, in the order of <see cref="Skeleton.Ids"/>.</param>
        /// <returns>The local to parent translation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetLocalToParentTranslation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForTranslation(index);

            return m_LocalTranslationData.GetRO(index);
        }

        /// <summary>
        /// Returns the local to parent rotation of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="RotationCount"/>.
        /// </remarks>
        /// <param name="index">The index of the rotation channel. The first <see cref="Skeleton.BoneCount"/>
        /// rotation channels are the bones, in the order of <see cref="Skeleton.Ids"/>.</param>
        /// <returns>The local to parent rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion GetLocalToParentRotation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForRotation(index);

            GetRotation(this, index, out var rotation);
            return rotation;
        }

        /// <summary>
        /// Returns the local to parent scale of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="ScaleCount"/>.
        /// </remarks>
        /// <param name="index">The index of the scale channel. The first <see cref="Skeleton.BoneCount"/>
        /// scale channels are the bones, in the order of <see cref="Skeleton.Ids"/>.</param>
        /// <returns>The local to parent scale.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetLocalToParentScale(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForScale(index);

            return m_LocalScaleData.GetRO(index);
        }

        /// <summary>
        /// Returns the generic float value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="FloatCount"/>.
        /// </remarks>
        /// <param name="index">The index of the float channel to read.</param>
        /// <returns>The generic float value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetFloat(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForFloat(index);

            return m_FloatData.GetRO(index);
        }

        /// <summary>
        /// Returns the generic integer value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="IntCount"/>.
        /// </remarks>
        /// <param name="index">The index of the integer channel to read.</param>
        /// <returns>The generic integer value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetInt(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForInt(index);

            return m_IntData.GetRO(index);
        }

        /// <summary>
        /// Returns the entity ID at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="EntityIdCount"/>.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if safety checks are enabled and
        /// <paramref name="index"/> is out of range.</exception>
        /// <param name="index">The index of the entity ID channel to read.</param>
        /// <returns>The entity ID value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly EntityId GetEntityId(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForEntityId(index);

            return m_EntityIdData.GetRO(index);
        }

        /// <summary>
        /// Returns a rotation value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="RotationCount"/>.
        /// </remarks>
        /// <param name="index">The index of the rotation channel to read.</param>
        /// <returns>The rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion GetRotation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForRotation(index);

            GetRotation(this, index, out var rotation);
            return rotation;
        }

        /// <summary>
        /// Returns the local to parent matrix of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <returns>The local to parent matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetLocalToParentMatrix(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);
            ValidateIndexBoundsForScale(index);

            GetLocalToParentMatrix(this, index, out var tx);
            return tx;
        }

        /// <summary>
        /// Returns the local to parent inverse matrix of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <returns>The local to parent inverse matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetLocalToParentInverseMatrix(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);
            ValidateIndexBoundsForScale(index);

            GetLocalToParentInverseMatrix(this, index, out var invTx);
            return invTx;
        }

        /// <summary>
        /// Returns the local to parent translation and rotation of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <param name="translation">The local to parent bone translation.</param>
        /// <param name="rotation">The local to parent rotation of the bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetLocalToParentTR(int index, out float3 translation, out quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);

            GetLocalToParentTR(this, index, out translation, out rotation);
        }

        /// <summary>
        /// Returns the local to parent translation, rotation and scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <param name="translation">The local to parent bone translation.</param>
        /// <param name="rotation">The local to parent rotation of the bone.</param>
        /// <param name="scale">The local to parent scale of the bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetLocalToParentTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);
            ValidateIndexBoundsForScale(index);

            GetLocalToParentTRS(this, index, out translation, out rotation, out scale);
        }

        /// <summary>
        /// Returns the local to parent translation, rotation and scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <returns>The local to parent TRS.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly TRS GetLocalToParentTRS(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);
            ValidateIndexBoundsForScale(index);

            GetLocalToParentTRS(this, index, out var trs);
            return trs;
        }

        /// <summary>
        /// Sets the local to parent translation of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="TranslationCount"/>.
        /// </remarks>
        /// <param name="index">The index of the translation channel. The first <see cref="Skeleton.BoneCount"/>
        /// translation channels are the bones, in the order of <see cref="Skeleton.Ids"/>.</param>
        /// <param name="translation">Local to parent translation to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTranslation(int index, in float3 translation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIndexBoundsForTranslation(index);

            SetLocalToParentTranslation(ref this, index, translation);
        }

        /// <summary>
        /// Sets the local to parent rotation of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="RotationCount"/>.
        /// </remarks>
        /// <param name="index">The index of the rotation channel. The first <see cref="Skeleton.BoneCount"/>
        /// rotation channels are the bones, in the order of <see cref="Skeleton.Ids"/>.</param>
        /// <param name="rotation">The local to parent rotation to store for the bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentRotation(int index, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForRotation(index);

            SetRotation(ref this, index, rotation);
        }

        /// <summary>
        /// Sets the local to parent translation of the bone at the specified index, without setting its bits in
        /// <see cref="PassMask"/> and <see cref="FrameMask"/>.
        /// </summary>
        /// <param name="index">The index of the translation channel.</param>
        /// <param name="translation">Local to parent translation to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetLocalToParentTranslationNoMaskWrite(int index, in float3 translation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIndexBoundsForTranslation(index);

            SetLocalToParentTranslationNoMaskWrite(ref this, index, translation);
        }

        /// <summary>
        /// Sets the local to parent rotation of the bone at the specified index, without setting its bits in
        /// <see cref="PassMask"/> and <see cref="FrameMask"/>.
        /// </summary>
        /// <param name="index">The index of the rotation channel.</param>
        /// <param name="rotation">Local to parent rotation to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetLocalToParentRotationNoMaskWrite(int index, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForRotation(index);

            SetRotationNoMaskWrite(ref this, index, rotation);
        }

        /// <summary>
        /// Sets the local to parent scale of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="ScaleCount"/>.
        /// </remarks>
        /// <param name="index">The index of the scale channel. The first <see cref="Skeleton.BoneCount"/>
        /// scale channels are the bones, in the order of <see cref="Skeleton.Ids"/>.</param>
        /// <param name="scale">The local to parent scale to store for the bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentScale(int index, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(scale);
            ValidateIndexBoundsForScale(index);

            SetLocalToParentScale(ref this, index, scale);
        }

        /// <summary>
        /// Sets the generic float value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="FloatCount"/>.
        /// </remarks>
        /// <param name="index">The index of the float channel to write.</param>
        /// <param name="value">The float value to write to the channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int index, float value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(value);
            ValidateIndexBoundsForFloat(index);

            SetFloat(ref this, index, value);
        }

        /// <summary>
        /// Sets the generic integer value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="IntCount"/>.
        /// </remarks>
        /// <param name="index">The index of the integer channel to write.</param>
        /// <param name="value">The integer value to write to the channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInt(int index, int value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIndexBoundsForInt(index);

            SetInt(ref this, index, value);
        }

        /// <summary>
        /// Sets the entity ID value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="EntityIdCount"/>.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if safety checks are enabled and
        /// <paramref name="index"/> is out of range.</exception>
        /// <param name="index">The index of the entity ID channel to write.</param>
        /// <param name="entityId">The entity ID to store in the channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEntityId(int index, in EntityId entityId)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIndexBoundsForEntityId(index);

            SetEntityId(ref this, index, entityId);
        }

        /// <summary>
        /// Sets the rotation value at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="RotationCount"/>.
        /// </remarks>
        /// <param name="index">The index of the rotation channel to write.</param>
        /// <param name="rotation">The rotation to write to the rotation channel.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotation(int index, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForRotation(index);

            SetRotation(ref this, index, rotation);
        }

        /// <summary>
        /// Sets the local to parent translation and rotation of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <param name="translation">The local to parent translation to set.</param>
        /// <param name="rotation">The local to parent rotation to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTR(int index, in float3 translation, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);

            SetLocalToParentTR(ref this, index, translation, rotation);
        }

        /// <summary>
        /// Sets the local to parent translation, rotation and scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the translation, rotation, and scale channels. The first
        /// <see cref="Skeleton.BoneCount"/> channels of each kind are the bones, in the order of
        /// <see cref="Skeleton.Ids"/>.</param>
        /// <param name="translation">The local to parent translation to set.</param>
        /// <param name="rotation">The local to parent rotation to set.</param>
        /// <param name="scale">The local to parent scale to store for the bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTRS(int index, in float3 translation, in quaternion rotation, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIsFinite(rotation);
            ValidateIsFinite(scale);
            ValidateIndexBoundsForTranslation(index);
            ValidateIndexBoundsForRotation(index);
            ValidateIndexBoundsForScale(index);

            SetLocalToParentTRS(ref this, index, translation, rotation, scale);
        }

        /// <summary>
        /// Returns the local to character translation of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        /// </remarks>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <returns>The local to character translation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetLocalToCharacterTranslation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterTranslation(this, index, out var translation);
            return translation;
        }

        /// <summary>
        /// Sets the local to character translation of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        /// </remarks>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="translation">The local to character translation to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToCharacterTranslation(int index, in float3 translation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIndexBoundsForSkeleton(index);

            SetLocalToCharacterTranslation(ref this, index, translation);
        }

        /// <summary>
        /// Returns the local to character rotation of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <returns>The local to character rotation.</returns>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// When a bone above this bone has a non-uniform scale, the rotation in character space is an approximation.
        /// The combined transform shears, and a rotation can't represent a shear. To get the exact transform, use <see
        /// cref="GetLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion GetLocalToCharacterRotation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterRotation(this, index, out var rotation);
            return rotation;
        }

        /// <summary>
        /// Sets the local to character rotation of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="rotation">The local to character rotation to set.</param>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// The stream stores values relative to the parent bone. This method converts the value with the current local
        /// to parent values of the ancestors of the bone. It writes the result to the local to parent channels of the
        /// bone. If an ancestor changes later, the character space value of the bone changes too.
        ///
        /// When a bone above this bone has a non-uniform scale, the result is an approximation. The combined transform
        /// shears, and a rotation with a scale can't represent a shear. To set values exactly, use the local to parent
        /// setters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToCharacterRotation(int index, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForSkeleton(index);

            SetLocalToCharacterRotation(ref this, index, rotation);
        }

        /// <summary>
        /// Returns the local to character scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <returns>The local to character scale.</returns>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// When a bone above this bone has a non-uniform scale, the scale in character space is an approximation. The
        /// combined transform shears, and a scale can't represent a shear. To get the exact transform, use <see
        /// cref="GetLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetLocalToCharacterScale(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterScale(this, index, out var scale);
            return scale;
        }

        /// <summary>
        /// Sets the local to character scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="scale">The local to character scale to set.</param>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// The stream stores values relative to the parent bone. This method converts the value with the current local
        /// to parent values of the ancestors of the bone. It writes the result to the local to parent channels of the
        /// bone. If an ancestor changes later, the character space value of the bone changes too.
        ///
        /// When a bone above this bone has a non-uniform scale, the result is an approximation. The combined transform
        /// shears, and a rotation with a scale can't represent a shear. To set values exactly, use the local to parent
        /// setters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToCharacterScale(int index, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(scale);
            ValidateIndexBoundsForSkeleton(index);

            SetLocalToCharacterScale(ref this, index, scale);
        }

        /// <summary>
        /// Returns the local to character translation and rotation of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="translation">The local to character translation.</param>
        /// <param name="rotation">The local to character rotation of the bone.</param>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// When a bone above this bone has a non-uniform scale, the rotation in character space is an approximation.
        /// The translation is exact. The combined transform shears, and a rotation can't represent a shear. To get the
        /// exact transform, use <see cref="GetLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetLocalToCharacterTR(int index, out float3 translation, out quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterTR(this, index, out translation, out rotation);
        }

        /// <summary>
        /// Sets the local to character translation and rotation of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="translation">The local to character translation to set.</param>
        /// <param name="rotation">The local to character rotation to set.</param>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// The stream stores values relative to the parent bone. This method converts the value with the current local
        /// to parent values of the ancestors of the bone. It writes the result to the local to parent channels of the
        /// bone. If an ancestor changes later, the character space value of the bone changes too.
        ///
        /// When a bone above this bone has a non-uniform scale, the result is an approximation. The combined transform
        /// shears, and a rotation with a scale can't represent a shear. To set values exactly, use the local to parent
        /// setters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToCharacterTR(int index, in float3 translation, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForSkeleton(index);

            SetLocalToCharacterTR(ref this, index, translation, rotation);
        }

        /// <summary>
        /// Returns the local to character translation, rotation and scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="translation">The local to character translation.</param>
        /// <param name="rotation">The local to character rotation of the bone.</param>
        /// <param name="scale">The local to character scale of the bone.</param>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// When a bone above this bone has a non-uniform scale, the rotation and the scale in character space are
        /// approximations. The translation is exact. The combined transform shears, and a rotation with a scale can't
        /// represent a shear. To get the exact transform, use <see cref="GetLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetLocalToCharacterTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterTRS(this, index, out translation, out rotation, out scale);
        }

        /// <summary>
        /// Sets the local to character translation, rotation and scale of the bone at the specified index.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="translation">The local to character translation to set.</param>
        /// <param name="rotation">The local to character rotation to set.</param>
        /// <param name="scale">The local to character scale to set.</param>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        ///
        /// The stream stores values relative to the parent bone. This method converts the value with the current local
        /// to parent values of the ancestors of the bone. It writes the result to the local to parent channels of the
        /// bone. If an ancestor changes later, the character space value of the bone changes too.
        ///
        /// When a bone above this bone has a non-uniform scale, the result is an approximation. The combined transform
        /// shears, and a rotation with a scale can't represent a shear. To set values exactly, use the local to parent
        /// setters.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToCharacterTRS(int index, in float3 translation, in quaternion rotation, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIsFinite(rotation);
            ValidateIsFinite(scale);
            ValidateIndexBoundsForSkeleton(index);

            SetLocalToCharacterTRS(ref this, index, translation, rotation, scale);
        }

        /// <summary>
        /// Removes the root motion from the pose by expressing the root motion bone relative to a root transform.
        /// </summary>
        /// <remarks>
        /// To sample a clip without its root motion, use
        /// <see cref="ClipSampler.SampleExtractingRootMotion(ref AnimationStream, in TimerState)"/>, which calls this
        /// method for you. Use this method for a pose that you build yourself, for example after you change the pose of
        /// a clip: pass <see cref="MotionOutput.RootTransform"/> from
        /// <see cref="ClipSampler.SampleRootMotion(in TimerState)"/> at the same time. Without this call, the pose
        /// still moves with the curves of the clip, so the character moves twice.
        ///
        /// Call it only when the clip animates the translation or rotation of the root motion bone or of one of its
        /// parent bones. Otherwise the pose doesn't move with the root motion, and this method moves the pose against
        /// it. This method doesn't handle **Loop Pose**: for a clip with **Loop Time** and **Loop Pose** enabled, the
        /// pose was already corrected, so the result differs from `SampleExtractingRootMotion`.
        ///
        /// The method changes the local to parent channels of the bone at <see cref="RigDefinition.RootMotionBoneIndex"/>
        /// so that its new transform in character space is its current transform in character space, relative to the
        /// root transform. The parents of the bone keep their values. The method does nothing if the rig has no
        /// skeleton.
        ///
        /// When a parent of the bone has a non-uniform scale, the result is an approximation, as with
        /// <see cref="SetLocalToCharacterTRS(int, in float3, in quaternion, in float3)"/>.
        /// </remarks>
        /// <param name="rootPosition">The position of the root transform in character space.</param>
        /// <param name="rootRotation">The rotation of the root transform in character space.</param>
        public void ExtractRootMotionBone(float3 rootPosition, quaternion rootRotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(rootPosition);
            ValidateIsFinite(rootRotation);

            var index = Rig.RootMotionBoneIndex;
            if (index < 0)
                return;

            GetLocalToCharacterTRS(this, index, out var translation, out var rotation, out var scale);

            var inverseRootRotation = math.conjugate(math.normalize(rootRotation));
            translation = math.mul(inverseRootRotation, translation - rootPosition);
            rotation = math.normalize(math.mul(inverseRootRotation, rotation));

            SetLocalToCharacterTRS(ref this, index, translation, rotation, scale);
        }

        /// <summary>
        /// Returns the local to character matrix of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        /// </remarks>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <returns>The local to character matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetLocalToCharacterMatrix(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterMatrix(this, index, out var tx);
            return tx;
        }

        /// <summary>
        /// Returns the local to character inverse matrix of the bone at the specified index.
        /// </summary>
        /// <remarks>
        /// The index must be at least 0 and less than <see cref="BoneCount"/>.
        /// </remarks>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <returns>The local to character inverse matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetLocalToCharacterInverseMatrix(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            GetLocalToCharacterInverseMatrix(this, index, out var invTx);
            return invTx;
        }

        /// <summary>
        /// Returns the static translation offset of the socket relative to its parent bone.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's translation offset relative to its parent bone.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetSocketLocalToParentTranslation(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            return Rig.SocketValues[index].LocalTranslationValue;
        }

        /// <summary>
        /// Returns the static rotation offset of the socket relative to its parent bone.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's rotation offset relative to its parent bone.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion GetSocketLocalToParentRotation(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            return Rig.SocketValues[index].LocalRotationValue;
        }

        /// <summary>
        /// Returns the static scale offset of the socket relative to its parent bone.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's scale offset relative to its parent bone.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetSocketLocalToParentScale(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            return Rig.SocketValues[index].LocalScaleValue;
        }

        /// <summary>
        /// Returns the matrix of the socket's transform relative to its parent bone.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's transform matrix relative to its parent bone.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetSocketLocalToParentMatrix(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToParentMatrix(this, index, out var tx);
            return tx;
        }

        /// <summary>
        /// Returns the inverse matrix of the socket's transform relative to its parent bone
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's inverse transform matrix relative to its parent bone.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetSocketLocalToParentInverseMatrix(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToParentInverseMatrix(this, index, out var invTx);
            return invTx;
        }

        /// <summary>
        /// Returns the static translation and rotation offset of the socket relative to its parent bone.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <param name="translation">The socket's translation offset relative to its parent bone.</param>
        /// <param name="rotation">The socket's rotation offset relative to its parent bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetSocketLocalToParentTR(int index, out float3 translation, out quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToParentTR(this, index, out translation, out rotation);
        }

        /// <summary>
        /// Returns the static translation and rotation offset of the socket relative to its parent bone.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <param name="translation">The socket's translation offset relative to its parent bone.</param>
        /// <param name="rotation">The socket's rotation offset relative to its parent bone.</param>
        /// <param name="scale">The socket's scale offset relative to its parent bone.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetSocketLocalToParentTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToParentTRS(this, index, out translation, out rotation, out scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly TRS GetSocketLocalToParentTRS(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToParentTRS(this, index, out var trs);
            return trs;
        }

        /// <summary>
        /// Returns the translation of the socket in character space.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's translation in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetSocketLocalToCharacterTranslation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterTranslation(this, index, out var translation);
            return translation;
        }

        /// <summary>
        /// Returns the rotation of the socket in character space.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's rotation in character space.</returns>
        /// <remarks>
        /// A socket is a fixed local transform that the rig stores relative to a bone. This method applies that
        /// transform to the character space transform of the parent bone of the socket.
        ///
        /// When the parent bone of the socket, or a bone above it, has a non-uniform scale, the rotation in character
        /// space is an approximation. The combined transform shears, and a rotation can't represent a shear. To get the
        /// exact transform, use <see cref="GetSocketLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion GetSocketLocalToCharacterRotation(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterRotation(this, index, out var rotation);
            return rotation;
        }

        /// <summary>
        /// Returns the scale of the socket in character space.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's scale in character space.</returns>
        /// <remarks>
        /// A socket is a fixed local transform that the rig stores relative to a bone. This method applies that
        /// transform to the character space transform of the parent bone of the socket.
        ///
        /// When the parent bone of the socket, or a bone above it, has a non-uniform scale, the scale in character
        /// space is an approximation. The combined transform shears, and a scale can't represent a shear. To get the
        /// exact transform, use <see cref="GetSocketLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 GetSocketLocalToCharacterScale(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterScale(this, index, out var scale);
            return scale;
        }

        /// <summary>
        /// Returns the matrix of the socket's transform in character space.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's transform matrix in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetSocketLocalToCharacterMatrix(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterMatrix(this, index, out var tx);
            return tx;
        }

        /// <summary>
        /// Returns the inverse matrix of the socket's transform in character space
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <returns>The socket's inverse transform matrix in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly AffineTransform GetSocketLocalToCharacterInverseMatrix(int index)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterInverseMatrix(this, index, out var invTx);
            return invTx;
        }

        /// <summary>
        /// Returns the translation and rotation of the socket in character space.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <param name="translation">The socket's translation in character space.</param>
        /// <param name="rotation">The socket's rotation in character space.</param>
        /// <remarks>
        /// A socket is a fixed local transform that the rig stores relative to a bone. This method applies that
        /// transform to the character space transform of the parent bone of the socket.
        ///
        /// When the parent bone of the socket, or a bone above it, has a non-uniform scale, the rotation in character
        /// space is an approximation. The translation is exact. The combined transform shears, and a rotation can't
        /// represent a shear. To get the exact transform, use <see cref="GetSocketLocalToCharacterMatrix"/>.
        ///
        /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, and
        /// `Unity.Mathematics` namespaces. It adds a socket to a rig, then reads the transform of the socket in
        /// character space from an animation stream.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SocketExample.cs#AddAndReadSocket}]]></code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetSocketLocalToCharacterTR(int index, out float3 translation, out quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterTR(this, index, out translation, out rotation);
        }

        /// <summary>
        /// Returns the translation, rotation and scale of the socket in character space.
        /// </summary>
        /// <param name="index">The index of the socket in the rig's socket list.</param>
        /// <param name="translation">The socket's translation in character space.</param>
        /// <param name="rotation">The socket's rotation in character space.</param>
        /// <param name="scale">The socket's scale in character space.</param>
        /// <remarks>
        /// A socket is a fixed local transform that the rig stores relative to a bone. This method applies that
        /// transform to the character space transform of the parent bone of the socket.
        ///
        /// If the parent bone of the socket or a bone above it has a non-uniform scale, the rotation and scale in
        /// character space are approximations. The translation is exact. The combined transform shears, and a
        /// rotation with a scale can't represent a shear. To get the exact transform, use <see
        /// cref="GetSocketLocalToCharacterMatrix"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetSocketLocalToCharacterTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSockets(index);

            GetSocketLocalToCharacterTRS(this, index, out translation, out rotation, out scale);
        }

        /// <summary>
        /// Returns the interpolated data chunks.
        /// </summary>
        public NativeArray<float4> InterpolatedDataChunks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                ValidateIsWritable();
                unsafe { return AsChunkArray<float4>(m_LocalTranslationData.m_Ptr, InterpolatedDataChunkCount); }
            }
        }

        /// <summary>
        /// Returns the read only interpolated data chunks.
        /// </summary>
        public readonly NativeArray<float4>.ReadOnly InterpolatedDataChunksReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                CheckRead();
                unsafe { return AsChunkArray<float4>(m_LocalTranslationData.m_Ptr, InterpolatedDataChunkCount).AsReadOnly(); }
            }
        }

        /// <summary>
        /// Returns the discrete data chunks.
        /// </summary>
        public NativeArray<int4> DiscreteDataChunks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                ValidateIsWritable();
                unsafe { return AsChunkArray<int4>(m_IntData.m_Ptr, DiscreteDataChunkCount); }
            }
        }

        /// <summary>
        /// Returns the read only discrete data chunks.
        /// </summary>
        public readonly NativeArray<int4>.ReadOnly DiscreteDataChunksReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                CheckRead();
                unsafe { return AsChunkArray<int4>(m_IntData.m_Ptr, DiscreteDataChunkCount).AsReadOnly(); }
            }
        }

        /// <summary>
        /// Returns the rotation data chunks.
        /// </summary>
        public NativeArray<quaternion4> RotationDataChunks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                ValidateIsWritable();
                unsafe { return AsChunkArray<quaternion4>(m_LocalRotationData.m_Ptr, RotationDataChunkCount); }
            }
        }

        /// <summary>
        /// Returns the read only rotation data chunks.
        /// </summary>
        public readonly NativeArray<quaternion4>.ReadOnly RotationDataChunksReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateIsNotNull();
                CheckRead();
                unsafe { return AsChunkArray<quaternion4>(m_LocalRotationData.m_Ptr, RotationDataChunkCount).AsReadOnly(); }
            }
        }

        // The array shares the safety handle of the stream, so it is checked like the stream
        readonly unsafe NativeArray<T> AsChunkArray<T>(void* ptr, int length) where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
            return array;
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
        /// Returns the total socket count
        /// </summary>
        public readonly int SocketCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.SocketValues.Length;
        }

        /// <summary>
        /// Returns the bone translation count.
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
        /// Returns the total bone scale count.
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
        /// The number of entity ID channels that the rig animates.
        /// </summary>
        public readonly int EntityIdCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.EntityIdBindings.Length;
        }

        /// <summary>
        /// Returns the total rotation data chunk count.
        /// </summary>
        public readonly int RotationDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.RotationDataChunkCount;
        }

        /// <summary>
        /// Returns the total discrete chunk count
        /// </summary>
        public readonly int DiscreteDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.DiscreteDataChunkCount;
        }

        /// <summary>
        /// Returns the total interpolated chunk count
        /// </summary>
        public readonly int InterpolatedDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Bindings.InterpolatedDataChunkCount;
        }

        /// <summary>
        /// Pass channel masks, cleared at the beginning of each frame.
        /// </summary>
        public readonly ChannelMask PassMask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { ValidateIsNotNull(); CheckRead(); return new ChannelMask(m_ChannelPassMasks.AsReadOnly(), m_Rig); } }
        }

        /// <summary>
        /// Frame channel masks, cleared at the beginning of each frame.
        /// </summary>
        public readonly ChannelMask FrameMask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { ValidateIsNotNull(); CheckRead(); return new ChannelMask(m_ChannelFrameMasks.AsReadOnly(), m_Rig); } }
        }

        /// <summary>
        /// Clears both frame and pass channel masks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearMasks()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelPassMasks.Clear();
            m_ChannelFrameMasks.Clear();
        }

        /// <summary>
        /// Clears pass channel masks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearPassMasks()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelPassMasks.Clear();
        }

        /// <summary>
        /// Clears frame channel masks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearFrameMasks()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelFrameMasks.Clear();
        }

        /// <summary>
        /// Sets both the frame and pass channel mask values.
        /// </summary>
        /// <param name="value">The value to set for the frame and pass masks.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMasks(bool value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelPassMasks.SetBits(value);
            m_ChannelFrameMasks.SetBits(value);
        }

        /// <summary>
        /// Sets the pass mask values.
        /// </summary>
        /// <param name="value">The value to set for the pass masks.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPassMasks(bool value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelPassMasks.SetBits(value);
        }

        /// <summary>
        /// Sets the frame mask values.
        /// </summary>
        /// <param name="value">The value to set for the frame masks.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFrameMasks(bool value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelFrameMasks.SetBits(value);
        }

        /// <summary>
        /// Copy all channel masks from a source AnimationStream.
        /// </summary>
        /// <param name="src">The stream to copy the channel masks from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyMasksFrom(in AnimationStream src)
        {
            src.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(src);
            m_ChannelPassMasks.CopyFrom(src.PassMask);
            m_ChannelFrameMasks.CopyFrom(src.FrameMask);
        }

        /// <summary>
        /// OR all channel masks with other AnimationStream channel masks.
        /// </summary>
        /// <param name="other">Other AnimationStream to OR channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrMasks(in AnimationStream other)
        {
            other.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(other);
            m_ChannelPassMasks.OrBits64(other.PassMask);
            m_ChannelFrameMasks.OrBits64(other.FrameMask);
        }

        /// <summary>
        /// OR all lhs and rhs AnimationStream channel masks and store result.
        /// Channel masks from lhs and rhs aren't modified.
        /// </summary>
        /// <param name="lhs">The input stream whose channel masks the method combines.</param>
        /// <param name="rhs">The input stream whose channel masks the method combines.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrMasks(in AnimationStream lhs, in AnimationStream rhs)
        {
            lhs.ValidateIsNotNull();
            rhs.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(lhs);
            ValidateCompatibility(rhs);
            m_ChannelPassMasks.OrBits64(lhs.PassMask, rhs.PassMask);
            m_ChannelFrameMasks.OrBits64(lhs.FrameMask, rhs.FrameMask);
        }

        // Channels has one bit per binding, with the layout of the channel masks
        internal void OrMasksWhere(in AnimationStream lhs, in AnimationStream rhs, in UnsafeBitArray.ReadOnly channels)
        {
            lhs.ValidateIsNotNull();
            rhs.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(lhs);
            ValidateCompatibility(rhs);
            m_ChannelPassMasks.OrBits64Where(lhs.PassMask.m_Masks, rhs.PassMask.m_Masks, channels);
            m_ChannelFrameMasks.OrBits64Where(lhs.FrameMask.m_Masks, rhs.FrameMask.m_Masks, channels);
        }

        /// <summary>
        /// AND all channel masks with other AnimationStream channel masks.
        /// </summary>
        /// <param name="other">Other AnimationStream to AND channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndMasks(in AnimationStream other)
        {
            other.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(other);
            m_ChannelPassMasks.AndBits64(other.PassMask);
            m_ChannelFrameMasks.AndBits64(other.FrameMask);
        }

        /// <summary>
        /// AND all lhs and rhs AnimationStream channel masks and store result.
        /// Channel masks from lhs and rhs aren't modified.
        /// </summary>
        /// <param name="lhs">The input stream whose channel masks the method combines.</param>
        /// <param name="rhs">The input stream whose channel masks the method combines.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndMasks(in AnimationStream lhs, in AnimationStream rhs)
        {
            lhs.ValidateIsNotNull();
            rhs.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(lhs);
            ValidateCompatibility(rhs);
            m_ChannelPassMasks.AndBits64(lhs.PassMask, rhs.PassMask);
            m_ChannelFrameMasks.AndBits64(lhs.FrameMask, rhs.FrameMask);
        }

        /// <summary>
        /// Transforms a position in local space to character space.
        /// You can perform the opposite conversion, from character to local space using
        /// <see cref="InverseTransformPosition(int, float3)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="position">The position in local space to transform.</param>
        /// <returns>The position in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformPosition(int index, float3 position)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            TransformPosition(this, index, ref position);
            return position;
        }

        /// <summary>
        /// Transforms a position in character space to local space.
        /// You can perform the opposite conversion, from local to character space using
        /// <see cref="TransformPosition(int, float3)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="position">The position in character space to transform.</param>
        /// <returns>The position in local space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 InverseTransformPosition(int index, float3 position)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            InverseTransformPosition(this, index, ref position);
            return position;
        }

        /// <summary>
        /// Transforms a rotation in local space to character space.
        /// You can perform the opposite conversion, from character to local space using
        /// <see cref="InverseTransformRotation(int, quaternion)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="rotation">The rotation in local space to transform.</param>
        /// <returns>The rotation in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion TransformRotation(int index, quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            TransformRotation(this, index, ref rotation);
            return rotation;
        }

        /// <summary>
        /// Transforms a rotation in character space to local space.
        /// You can perform the opposite conversion, from local to character space using
        /// <see cref="TransformRotation(int, quaternion)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="rotation">The rotation in character space to transform.</param>
        /// <returns>The rotation in local space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly quaternion InverseTransformRotation(int index, quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            InverseTransformRotation(this, index, ref rotation);
            return rotation;
        }

        /// <summary>
        /// Transforms a position and rotation in local space to character space.
        /// You can perform the opposite conversion, from character to local space using
        /// <see cref="InverseTransformPositionRotation(int, ref float3, ref quaternion)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="position">The position in local space to transform.</param>
        /// <param name="rotation">The rotation in local space to transform.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void TransformPositionRotation(int index, ref float3 position, ref quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            TransformPositionRotation(this, index, ref position, ref rotation);
        }

        /// <summary>
        /// Transforms a position and rotation in character space to local space.
        /// You can perform the opposite conversion, from local to character space using
        /// <see cref="TransformPositionRotation(int, ref float3, ref quaternion)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="position">The position in character space to transform.</param>
        /// <param name="rotation">The rotation in character space to transform.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void InverseTransformPositionRotation(int index, ref float3 position, ref quaternion rotation)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            InverseTransformPositionRotation(this, index, ref position, ref rotation);
        }

        /// <summary>
        /// Transforms a vector from local space to character space. This operation isn't affected by position of the
        /// transforms, but it is affected by scale. The returned vector may have a different length than the original
        /// vector. You can perform the opposite conversion, from character to local space using
        /// <see cref="InverseTransformVector(int, float3)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="vector">The vector in local space to transform.</param>
        /// <returns>The vector in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformVector(int index, float3 vector)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            TransformVector(this, index, ref vector);
            return vector;
        }

        /// <summary>
        /// Transforms a vector from character space to local space. This operation isn't affected by position of the
        /// transforms, but it is affected by scale. The returned vector may have a different length than the original
        /// vector. You can perform the opposite conversion, from local to character space using
        /// <see cref="TransformVector(int, float3)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="vector">The vector in character space to transform.</param>
        /// <returns>The vector in local space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 InverseTransformVector(int index, float3 vector)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            InverseTransformVector(this, index, ref vector);
            return vector;
        }

        /// <summary>
        /// Transforms direction from local space to character space. This operation isn't affected by scale or position
        /// of the transforms and the returned vector has the same length as the original direction. You can perform the
        /// opposite conversion, from character to local space using
        /// <see cref="InverseTransformDirection(int, float3)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="direction">The direction in local space to transform.</param>
        /// <returns>The direction in character space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 TransformDirection(int index, float3 direction)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            TransformDirection(this, index, ref direction);
            return direction;
        }

        /// <summary>
        /// Transforms direction from character space to local space. This operation isn't affected by scale or position
        /// of the transforms and the returned vector has the same length as the original direction. You can perform the
        /// opposite conversion, from local to character space using <see cref="TransformDirection(int, float3)"/>.
        /// </summary>
        /// <param name="index">The index of the bone in <see cref="Skeleton.Ids"/>, for example from
        /// <see cref="Skeleton.FindBoneIndex"/>.</param>
        /// <param name="direction">The direction in character space to transform.</param>
        /// <returns>The direction in local space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float3 InverseTransformDirection(int index, float3 direction)
        {
            ValidateIsNotNull();
            CheckRead();
            ValidateIndexBoundsForSkeleton(index);

            InverseTransformDirection(this, index, ref direction);
            return direction;
        }

        internal void ConvertToCharacterSpace()
        {
            for (var index = 0; index < BoneCount; ++index)
            {
                var parentTRS = GetLocalToParentTRS(index);
                var translation = parentTRS.t;
                var rotation = parentTRS.r;

                var rootRS = math.float3x3(parentTRS.r);
                rootRS = math.mulScale(rootRS, parentTRS.s);

                float3x3 parentRS;
                int parentIdx = Rig.Skeleton.ParentIndexes[index];
                if (parentIdx != k_InvalidIndex)
                {
                    parentTRS = GetLocalToParentTRS(parentIdx);
                    parentRS = math.float3x3(parentTRS.r);
                    parentRS = math.mulScale(parentRS, parentTRS.s);
                    rootRS = math.mul(parentRS, rootRS);

                    translation = MathUtility.transform(parentTRS, translation);
                    rotation = MathUtility.scaleMulQuat(parentTRS.s, rotation);
                    rotation = math.mul(parentTRS.r, rotation);
                }

                rotation = math.normalize(rotation);
                var scale = MathUtility.extractScale(rotation, rootRS);

                SetLocalToParentTRS(index, translation, rotation, scale);
            }
        }

        internal void ConvertRotationsToCharacterSpace()
        {
            for (var i = 0; i < BoneCount; ++i)
            {
                var parentIdx = Rig.Skeleton.ParentIndexes[i];
                if (parentIdx < 0)
                    continue;

                var parentRotation = GetLocalToParentRotation(parentIdx);
                var rotation = GetLocalToParentRotation(i);

                SetLocalToParentRotationNoMaskWrite(i, math.mul(parentRotation, rotation));
            }
        }

        internal void ConvertRotationsFromCharacterSpace()
        {
            for (var i = 0; i < BoneCount; ++i)
            {
                var parentIdx = Rig.Skeleton.ParentIndexes[i];
                if (parentIdx < 0)
                    continue;

                var parentRotation = GetLocalToCharacterRotation(parentIdx);
                var rotation = GetLocalToParentRotation(i);

                SetLocalToParentRotationNoMaskWrite(i, math.mul(math.inverse(parentRotation), rotation));
            }
        }
    }

    [NativeContainer]
    unsafe struct AnimationStreamDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;
        internal int m_Length;
        internal Allocator m_Allocator;

        internal AtomicSafetyHandle m_Safety;

        public void Dispose() => AnimationStream.FreeBuffer(m_Buffer, m_Length, m_Allocator);
    }

    struct AnimationStreamDisposeJob : IJob
    {
        internal AnimationStreamDispose Data;

        public void Execute() => Data.Dispose();
    }

    sealed class AnimationStreamDebugView
    {
        // Reads the memory without the safety checks, so that the debugger can show a stream that a job uses
        public AnimationStreamDebugView(AnimationStream stream)
        {
            BoneCount = stream.BoneCount;

            TranslationCount = stream.TranslationCount;
            Translations = new float3[TranslationCount];
            for (var i = 0; i < TranslationCount; i++)
                Translations[i] = stream.m_LocalTranslationData.GetRO(i);

            RotationCount = stream.RotationCount;
            Rotations = new quaternion[RotationCount];
            for (var i = 0; i < RotationCount; i++)
            {
                AnimationStream.GetRotation(stream, i, out var rotation);
                Rotations[i] = rotation;
            }

            ScaleCount = stream.ScaleCount;
            Scales = new float3[ScaleCount];
            for (var i = 0; i < ScaleCount; i++)
                Scales[i] = stream.m_LocalScaleData.GetRO(i);

            FloatCount = stream.FloatCount;
            Floats = new float[FloatCount];
            for (var i = 0; i < FloatCount; i++)
                Floats[i] = stream.m_FloatData.GetRO(i);

            IntCount = stream.IntCount;
            Ints = new int[IntCount];
            for (var i = 0; i < IntCount; i++)
                Ints[i] = stream.m_IntData.GetRO(i);

            EntityIdCount = stream.EntityIdCount;
            EntityIds = new EntityId[EntityIdCount];
            for (var i = 0; i < EntityIdCount; i++)
                EntityIds[i] = stream.m_EntityIdData.GetRO(i);

            ChannelPassMask = stream.m_ChannelPassMasks;
            ChannelFrameMask = stream.m_ChannelFrameMasks;
        }

        public int BoneCount { get; }

        public int TranslationCount { get; }
        public float3[] Translations { get; }

        public int RotationCount { get; }
        public quaternion[] Rotations { get; }

        public int ScaleCount { get; }
        public float3[] Scales { get; }

        public int FloatCount { get; }
        public float[] Floats { get; }

        public int IntCount { get; }
        public int[] Ints { get; }

        public int EntityIdCount { get; }
        public EntityId[] EntityIds { get; }

        public UnsafeBitArray ChannelPassMask { get; }
        public UnsafeBitArray ChannelFrameMask { get; }
    }
}
