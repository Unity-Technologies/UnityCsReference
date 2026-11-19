// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.IntegerTime;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animations
{
    unsafe struct ClipSamplerData
    {
        public Allocator Allocator;
        public BlobObjectReference<Clip> Clip;

        public BlobAssetReference<ClipInstance> Instance;

        public ClipSamplingCacheData Cache;

        // Allocated by the first SampleRootMotion call
        public ClipSamplingCacheData* PreviousCache;

        public bool ExcludeRoot;
    }

    /// <summary>
    /// Samples an animation clip into an <see cref="AnimationStream"/>, and returns the root motion of the clip.
    /// </summary>
    /// <remarks>
    /// A clip sampler reads an animation clip at a given time. It computes the value of each curve of the clip at that
    /// time, and writes that value to the matching channel of an animation stream. It also reports how far the root of
    /// the clip moved.
    ///
    /// Call <see cref="AnimationClipExtensions.ToClipSampler(AnimationClip, BlobAssetReference{RigDefinition}, Allocator)"/>
    /// from the main thread to create a sampler for a clip and a rig. Call <see cref="Dispose"/> when you finish with
    /// it. Then use the sampler as follows:
    ///
    ///- <see cref="Sample(ref AnimationStream, in TimerState)"/> overwrites the channels that the clip animates and
    ///  leaves the others unchanged.
    ///- <see cref="SampleAdditive(ref AnimationStream, in TimerState)"/> resets the whole stream to zero first, so that
    ///  the stream holds an additive pose.
    ///- <see cref="SampleRootMotion(in TimerState)"/> returns how much the root of the clip moved from the previous
    ///  time of a timer to its current time.
    ///- <see cref="ExcludeRoot"/> keeps the root curves out of the pose, so that the character moves only with the
    ///  root motion that you apply.
    ///
    /// `Sample` and `SampleAdditive` each have a <see cref="TimerState"/> overload and a <see cref="DiscreteTime"/>
    /// overload. The remarks of each method explain which one to use. The sampler is faster when the time moves
    /// forward. When the time on the clip curves goes backward, for example when a looping clip wraps around, the
    /// sampler restarts from the beginning of the clip data.
    ///
    /// Keep these rules in mind:
    ///
    ///- Creating a sampler is expensive. Create one sampler for each pair of clip and rig, keep it, and call
    ///  <see cref="Clone"/> to get more samplers for the same clip and rig. Create the sampler again to sample an
    ///  edited clip.
    ///- Copies of the struct share the same memory. Dispose only one copy. A clone has its own memory, so dispose
    ///  each clone too.
    ///- Use a sampler from one job at a time, and from one thread at a time. If safety checks are enabled, the job
    ///  system throws an `InvalidOperationException` when two scheduled jobs write the same sampler.
    ///- Don't store a sampler in a component or a buffer element. If safety checks are enabled, the job system refuses
    ///  to schedule a job that accesses a component that holds a native container. To animate many characters, create
    ///  one sampler for each character, keep the samplers of a group of characters in an
    ///  <see cref="UnsafeList{T}"/>, and sample them in one job. The job scheduler can't see the samplers behind the
    ///  list, so it doesn't detect jobs that use the same sampler. Safety checks still detect the use of a disposed
    ///  sampler.
    ///
    /// Safety checks are always enabled in the Editor and never in a built Player.
    ///
    /// The following examples use the `Unity.Animations`, `Unity.Burst`, `Unity.Collections`,
    /// `Unity.Collections.LowLevel.Unsafe`, `Unity.Entities`, `Unity.IntegerTime`, `Unity.Jobs`, and `UnityEngine`
    /// namespaces. The first example samples the `intensity` field of the `SampleClipExampleTarget` component at a
    /// given time into an animation stream. To run it, add a `MonoBehaviour` named `SampleClipExampleTarget` that has
    /// a `float` field named `intensity`, or replace the component and the field with the ones that your animation
    /// clip animates. The second example samples one clip for several characters in a Burst-compiled job.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipExample.cs#SampleClip}]]></code>
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipCharactersExample.cs#SampleClipCharacters}]]></code>
    /// </example>
    /// <seealso cref="AnimationClipExtensions"/>
    [NativeContainer]
    public unsafe struct ClipSampler : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal ClipSamplerData* m_Data;

        internal AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<ClipSampler>();

        // Takes over clip and clipInstance
        internal ClipSampler(BlobObjectReference<Clip> clip, BlobAssetReference<ClipInstance> clipInstance, Allocator allocator)
        {
            ValidateAllocator(allocator);

            this = default;
            m_Data = AllocatorManager.Allocate<ClipSamplerData>(allocator);
            *m_Data = new ClipSamplerData
            {
                Allocator = allocator,
                Clip = clip,
                Instance = clipInstance,
            };

            var muscleClipPtr = (Clip*)clip.GetUnsafeRootPtr();
            var curveCount = muscleClipPtr->m_Clip.IsNull ? 0 : ClipSamplingCache.GetCurveCount((ClipCurves*)muscleClipPtr->m_Clip.Get());
            ClipSamplingCacheData.Create(&m_Data->Cache, curveCount, allocator);
            ClipSamplingCacheData.Initialize(&m_Data->Cache, muscleClipPtr);

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<ClipSampler>(ref m_Safety, ref s_SafetyId.Data);
        }

        /// <summary>Whether the sampler has allocated memory.</summary>
        /// <remarks>
        /// The value is `false` for a default sampler and for the copy of the sampler on which you called
        /// <see cref="Dispose"/>.
        /// </remarks>
        public bool IsCreated => m_Data != null;

        /// <summary>Creates a sampler that samples the same clip for the same rig, with its own memory.</summary>
        /// <remarks>
        /// The clone copies the data that matches the curves of the clip with the rig, so it's much faster than
        /// <see cref="AnimationClipExtensions.ToClipSampler(AnimationClip, BlobAssetReference{RigDefinition}, Allocator)"/>.
        /// It acquires its own reference to the clip data and uses the allocator of this sampler.
        ///
        /// The clone doesn't share memory with this sampler. Dispose it separately. You can use the clone after you
        /// dispose this sampler, and use the two samplers in different jobs at the same time. The clone starts with
        /// empty sampling caches, which doesn't change the values it samples.
        /// </remarks>
        /// <returns>The new sampler. Dispose it when you finish with it. A default sampler if this sampler isn't
        /// created.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler.</exception>
        public ClipSampler Clone()
        {
            if (m_Data == null)
                return default;

            ValidateCanRead();
            return new ClipSampler(m_Data->Clip.Clone(), m_Data->Instance.Clone(m_Data->Allocator), m_Data->Allocator);
        }

        /// <summary>The reference to the clip data that the sampler samples.</summary>
        /// <remarks>
        /// The sampler owns the reference, so don't dispose it. It stays valid until you dispose the sampler. Convert
        /// it with <see cref="BlobObjectReference{T}.AsBlobAssetReference"/> to read the clip data, for example to read
        /// its <see cref="Unity.Animations.Clip.Length"/> or to create a clip timer.
        /// </remarks>
        public BlobObjectReference<Clip> Clip
        {
            get
            {
                ValidateCanRead();
                return m_Data->Clip;
            }
        }

        /// <summary>Whether the clip has root motion curves for the rig of the sampler.</summary>
        /// <remarks>
        /// <see cref="SampleRootMotion(in TimerState)"/> returns the identity transform if this value is `false`.
        ///
        /// The value is `true` when the clip has translation or rotation curves for the root bone of the rig. The
        /// value ignores the root motion curves of a generic clip and the **Bake Into Pose** setting. It's therefore
        /// `false` for a clip that has only those curves.
        /// </remarks>
        public bool HasRootMotion
        {
            get
            {
                ValidateCanRead();
                var samplingData = m_Data->Instance.Value.RootMotionSamplingData;
                return samplingData.RootTranslationIndex >= 0 || samplingData.RootRotationIndex >= 0;
            }
        }

        /// <summary>Whether sampling leaves the root curves of the clip out of the pose.</summary>
        /// <remarks>
        /// The root is the first transform of the rig, which is the root transform that you passed to
        /// <see cref="RigBuilder.Create(Transform, AnimationClip[], Allocator)"/>. By default, `Sample` writes the root
        /// curves of the clip into the root, so the pose moves with the clip. If you also move the character with
        /// <see cref="SampleRootMotion(in TimerState)"/>, the character moves twice. Set this value to `true` to keep
        /// the root still.
        ///
        /// Sampling then handles the root translation and the root rotation like the channels that the clip doesn't
        /// animate. They keep their value, and sampling doesn't set their bits in <see cref="AnimationStream.PassMask"/>
        /// and <see cref="AnimationStream.FrameMask"/>. After
        /// <see cref="SampleAdditive(ref AnimationStream, in TimerState)"/>, they stay at zero.
        ///
        /// The value is `false` by default. Copies of the sampler share it.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler.</exception>
        /// <exception cref="NullReferenceException">Thrown if safety checks are enabled and the sampler isn't created,
        /// or you disposed this copy of it.</exception>
        public readonly bool ExcludeRoot
        {
            get
            {
                ValidateCanRead();
                return m_Data->ExcludeRoot;
            }
            set
            {
                ValidateCanWrite();
                m_Data->ExcludeRoot = value;
            }
        }

        /// <summary>
        /// Samples the clip at the time of a clip timer. The method computes the value of each curve of the clip at
        /// that time, and overwrites the matching channels of the animation stream.
        /// </summary>
        /// <remarks>
        /// The channels that the clip doesn't animate keep their value. To reset them to the rig defaults, call
        /// <see cref="AnimationStream.ResetToDefaultValues"/> before you sample.
        ///
        /// The method samples the clip curves at <see cref="TimerState.Time"/>.
        /// <see cref="Unity.Animations.Clip.ToTimerState(DiscreteTime)"/> adds the cycle offset when it creates the
        /// timer, so the timer time already includes the cycle offset. To sample at a playback time without a timer,
        /// use <see cref="Sample(ref AnimationStream, DiscreteTime)"/>, which adds the cycle offset for you.
        ///
        /// When the clip has **Loop Time** and **Loop Pose** enabled, the method also corrects the pose to close the
        /// gap between the first and last frames. The correction is zero at the start of
        /// <see cref="TimerState.Range"/> and grows until the end of the range, where the pose matches the pose of the
        /// first frame.
        ///
        /// The sampler interpolates between the frames of the clip, so it doesn't snap the time to a frame. To play a
        /// clip backward, set a negative <see cref="TimerState.TimeScale"/> on the timer.
        /// </remarks>
        /// <param name="stream">The stream to write the sampled channels to. It must have the rig that the sampler was
        /// created for.</param>
        /// <param name="timer">The clip timer to sample, from
        /// <see cref="Unity.Animations.Clip.ToTimerState(DiscreteTime)"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler. Also thrown if
        /// safety checks are enabled and the rig of <paramref name="stream"/> isn't the rig of the sampler.</exception>
        /// <exception cref="NullReferenceException">Thrown if safety checks are enabled and the sampler isn't created,
        /// or you disposed this copy of it.</exception>
        public void Sample(ref AnimationStream stream, in TimerState timer)
        {
            ValidateCanWrite();
            SampleUnchecked(ref stream, in timer);
        }

        /// <summary>
        /// Samples the clip at a playback time. The method computes the value of each curve of the clip at that time,
        /// and overwrites the matching channels of the animation stream.
        /// </summary>
        /// <remarks>
        /// The channels that the clip doesn't animate keep their value. To reset them to the rig defaults, call
        /// <see cref="AnimationStream.ResetToDefaultValues"/> before you sample.
        ///
        /// This method already applies the <see cref="Unity.Animations.Clip.LoopTime"/> setting and the cycle offset of
        /// the clip to the playback time you pass. Don't pass the <see cref="TimerState.Time"/> of a clip timer,
        /// because the timer already applied the `LoopTime` setting and the cycle offset to that time. To sample with a
        /// clip timer, use <see cref="Sample(ref AnimationStream, in TimerState)"/>. For how `LoopTime` and the cycle
        /// offset change the time, see <see cref="Unity.Animations.Clip.ToTimerState(DiscreteTime)"/>.
        ///
        /// The sampler interpolates between the frames of the clip, so it doesn't snap the time to a frame. To play a
        /// clip backward, pass a playback time that decreases between calls.
        /// </remarks>
        /// <param name="stream">The stream to write the sampled channels to. It must have the rig that the sampler was
        /// created for.</param>
        /// <param name="time">The playback time in seconds from the start of the clip.</param>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler. Also thrown if
        /// safety checks are enabled and the rig of <paramref name="stream"/> isn't the rig of the sampler.</exception>
        /// <exception cref="NullReferenceException">Thrown if safety checks are enabled and the sampler isn't created,
        /// or you disposed this copy of it.</exception>
        public void Sample(ref AnimationStream stream, DiscreteTime time)
        {
            ValidateCanWrite();
            SampleUnchecked(ref stream, time);
        }

        /// <summary>
        /// Samples the clip at the time of a clip timer into a stream that you can apply as an additive pose.
        /// </summary>
        /// <remarks>
        /// The method doesn't subtract a reference pose from the sampled values, so the curves of the clip must already
        /// hold additive values. For example, blend the stream onto a pose with
        /// <see cref="AnimationStreamBlendingExtensions.BlendAdditive(ref AnimationStream, in AnimationStream, float)"/>.
        ///
        /// The method resets the whole stream to zero, including its channel masks, then samples the clip as
        /// <see cref="Sample(ref AnimationStream, in TimerState)"/> does. The channels that the clip doesn't animate
        /// stay at zero.
        /// </remarks>
        /// <param name="stream">The stream to write the sampled channels to. It must have the rig that the sampler was
        /// created for.</param>
        /// <param name="timer">The clip timer to sample, from
        /// <see cref="Unity.Animations.Clip.ToTimerState(DiscreteTime)"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler. Also thrown if
        /// safety checks are enabled and the rig of <paramref name="stream"/> isn't the rig of the sampler.</exception>
        /// <exception cref="NullReferenceException">Thrown if safety checks are enabled and the sampler isn't created,
        /// or you disposed this copy of it.</exception>
        public void SampleAdditive(ref AnimationStream stream, in TimerState timer)
        {
            ValidateCanWrite();
            stream.ResetToZero();
            SampleUnchecked(ref stream, in timer);
        }

        /// <summary>
        /// Samples the clip at a playback time into a stream that you can apply as an additive pose.
        /// </summary>
        /// <remarks>
        /// The method doesn't subtract a reference pose from the sampled values, so the curves of the clip must already
        /// hold additive values. For example, blend the stream onto a pose with
        /// <see cref="AnimationStreamBlendingExtensions.BlendAdditive(ref AnimationStream, in AnimationStream, float)"/>.
        ///
        /// The method resets the whole stream to zero, including its channel masks, then samples the clip as
        /// <see cref="Sample(ref AnimationStream, DiscreteTime)"/> does. The channels that the clip doesn't animate
        /// stay at zero.
        /// </remarks>
        /// <param name="stream">The stream to write the sampled channels to. It must have the rig that the sampler was
        /// created for.</param>
        /// <param name="time">The playback time in seconds from the start of the clip.</param>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler. Also thrown if
        /// safety checks are enabled and the rig of <paramref name="stream"/> isn't the rig of the sampler.</exception>
        /// <exception cref="NullReferenceException">Thrown if safety checks are enabled and the sampler isn't created,
        /// or you disposed this copy of it.</exception>
        public void SampleAdditive(ref AnimationStream stream, DiscreteTime time)
        {
            ValidateCanWrite();
            stream.ResetToZero();
            SampleUnchecked(ref stream, time);
        }

        /// <summary>
        /// Samples how much the root of the clip moved from the previous time to the time of a clip timer.
        /// </summary>
        /// <remarks>
        /// The result is the root transform of the clip at <see cref="TimerState.Time"/>, relative to its transform
        /// at <see cref="TimerState.PreviousTime"/>. For a looping clip, the result includes the whole cycles that
        /// the timer crossed, from <see cref="TimerState.PreviousLoopCount"/> to <see cref="TimerState.LoopCount"/>.
        /// Call this method once for each update of the timer, and accumulate the results after the transform of the
        /// character to move it, for example `character = math.mul(character, delta)`. If
        /// <see cref="HasRootMotion"/> is `false`, the clip has no root motion and the method returns the identity
        /// transform.
        ///
        /// The stream operations don't carry root motion. To blend the root motion of several clips, combine the
        /// results with the same weights as the poses, with <see cref="RootMotionUtility.Blend"/> or
        /// <see cref="RootMotionUtility.DeltaMixer"/>. <see cref="RootMotionUtility"/> has a helper for each other
        /// stream operation.
        ///
        /// If <see cref="HasRootMotion"/> is `true`, the first call allocates extra memory for the sampler, with the
        /// allocator that you passed to
        /// <see cref="AnimationClipExtensions.ToClipSampler(AnimationClip, BlobAssetReference{RigDefinition}, Allocator)"/>.
        /// <see cref="Dispose"/> frees it.
        ///
        /// Root motion comes only from the curves of the root transform of the rig. The method doesn't support a
        /// generic clip with root motion curves. It ignores the **Bake Into Pose** setting. `Sample` still writes the
        /// root curves into the pose, so set <see cref="ExcludeRoot"/> to `true` to keep the root still while you move
        /// the character with this method.
        ///
        /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`,
        /// `Unity.IntegerTime`, `Unity.Mathematics`, and `UnityEngine` namespaces. It plays a clip and moves the
        /// GameObject of the component with the root motion of the clip.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/RootMotionExample.cs#MoveCharacter}]]></code>
        /// </example>
        /// <param name="timer">The clip timer to sample, from
        /// <see cref="Unity.Animations.Clip.ToTimerState(DiscreteTime)"/>.</param>
        /// <returns>The root transform of the clip at <see cref="TimerState.Time"/>, relative to its transform at
        /// <see cref="TimerState.PreviousTime"/>. The identity transform if <see cref="HasRootMotion"/> is `false`.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler.</exception>
        /// <exception cref="NullReferenceException">Thrown if safety checks are enabled and the sampler isn't created,
        /// or you disposed this copy of it.</exception>
        public RigidTransform SampleRootMotion(in TimerState timer)
        {
            ValidateCanWrite();
            return SampleRootMotionUnchecked(in timer);
        }

        RigidTransform SampleRootMotionUnchecked(in TimerState timer)
        {
            var samplingData = m_Data->Instance.Value.RootMotionSamplingData;
            if (samplingData.RootTranslationIndex < 0 && samplingData.RootRotationIndex < 0)
                return RigidTransform.identity;

            if (m_Data->PreviousCache == null)
            {
                var previousCache = AllocatorManager.Allocate<ClipSamplingCacheData>(m_Data->Allocator);
                ClipSamplingCacheData.Create(previousCache, 0, m_Data->Allocator);
                ClipSamplingCacheData.InitializeRootMotion(previousCache, samplingData.RootTranslationIndex, samplingData.RootRotationIndex);
                m_Data->PreviousCache = previousCache;
            }

            return Core.SampleAnimationClipRootMotion(m_Data->Clip.AsBlobAssetReference(), m_Data->Instance,
                ref m_Data->PreviousCache->Memory, ref m_Data->Cache.Memory, in timer);
        }

        /// <summary>Releases the clip reference and the memory of the sampler.</summary>
        /// <remarks>
        /// Copies of the sampler share the same memory. Call this method on only one copy, and don't use the other
        /// copies afterward. A sampler from <see cref="Clone"/> has its own memory, so this method doesn't affect it.
        /// This method has no effect on a default sampler.
        ///
        /// If safety checks are enabled, a second call to this method throws an `ObjectDisposedException` on any copy.
        /// For a sampler that uses <see cref="Allocator.Temp"/>, the safety system doesn't detect that you use another copy
        /// after this call, as for other containers that use <see cref="Allocator.Temp"/>. The memory of the sampler stays
        /// valid until the end of the scope of the temporary allocations, but the clip data can be released.
        /// Using the copy on which you called `Dispose` throws a `NullReferenceException`.
        /// </remarks>
        public void Dispose()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            if (m_Data == null)
                return;

            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
            var allocator = m_Data->Allocator;
            ClipSamplingCacheData.Free(&m_Data->Cache);
            if (m_Data->PreviousCache != null)
            {
                ClipSamplingCacheData.Free(m_Data->PreviousCache);
                AllocatorManager.Free(allocator, m_Data->PreviousCache);
            }

            m_Data->Clip.Dispose();
            m_Data->Instance.Dispose();

            AllocatorManager.Free(allocator, m_Data);
            m_Data = null;
        }

        void SampleUnchecked(ref AnimationStream stream, in TimerState timer)
        {
            if (m_Data->ExcludeRoot)
            {
                SampleExcludingRootUnchecked(ref stream, in timer);
                return;
            }

            Core.SampleAnimationClipOverwrite(m_Data->Clip.AsBlobAssetReference(), m_Data->Instance,
                ref m_Data->Cache.Memory, in timer, ref stream);
        }

        void SampleUnchecked(ref AnimationStream stream, DiscreteTime time)
        {
            var timer = m_Data->Clip.AsBlobAssetReference().Value.ToTimerState(time);
            SampleUnchecked(ref stream, in timer);
        }

        void SampleExcludingRootUnchecked(ref AnimationStream stream, in TimerState timer)
        {
            var samplingData = m_Data->Instance.Value.RootMotionSamplingData;
            var translationBit = stream.Rig.Bindings.TranslationBindingIndex;
            var rotationBit = stream.Rig.Bindings.RotationBindingIndex;
            var translation = stream.GetLocalToParentTranslation(0);
            var rotation = stream.GetLocalToParentRotation(0);
            var translationPassBit = stream.m_ChannelPassMasks.IsSet(translationBit);
            var translationFrameBit = stream.m_ChannelFrameMasks.IsSet(translationBit);
            var rotationPassBit = stream.m_ChannelPassMasks.IsSet(rotationBit);
            var rotationFrameBit = stream.m_ChannelFrameMasks.IsSet(rotationBit);

            Core.SampleAnimationClipOverwrite(m_Data->Clip.AsBlobAssetReference(), m_Data->Instance,
                ref m_Data->Cache.Memory, in timer, ref stream);

            if (samplingData.RootTranslationIndex >= 0)
            {
                stream.SetLocalToParentTranslationNoMaskWrite(0, translation);
                stream.m_ChannelPassMasks.Set(translationBit, translationPassBit);
                stream.m_ChannelFrameMasks.Set(translationBit, translationFrameBit);
            }

            if (samplingData.RootRotationIndex >= 0)
            {
                stream.SetLocalToParentRotationNoMaskWrite(0, rotation);
                stream.m_ChannelPassMasks.Set(rotationBit, rotationPassBit);
                stream.m_ChannelFrameMasks.Set(rotationBit, rotationFrameBit);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateAllocator(Allocator allocator)
        {
            if ((int)allocator <= (int)Allocator.None)
                throw new ArgumentException("The allocator must be a valid allocator.", "allocator");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateCanRead()
        {
            ValidateIsCreated();
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateCanWrite()
        {
            ValidateIsCreated();
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }

        readonly void ValidateIsCreated()
        {
            if (m_Data == null)
                throw new NullReferenceException("The ClipSampler is not created.");
        }
    }
}
