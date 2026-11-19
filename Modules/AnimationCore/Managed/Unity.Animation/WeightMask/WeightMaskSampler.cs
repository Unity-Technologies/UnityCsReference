// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animations
{
    struct WeightMaskSamplerData
    {
        public Allocator Allocator;
        public BlobAssetReference<WeightMaskInstance> Instance;
    }

    /// <summary>
    /// Holds the weight of each channel of a rig for a weight mask, such as the weight mask of an avatar mask.
    /// </summary>
    /// <remarks>
    /// A weight mask sampler resolves a <see cref="WeightMask"/> for a <see cref="RigDefinition"/>. It holds one weight
    /// for each channel of the rig. Call <see cref="AnimationStreamMask.CreateReadOnly(BlobAssetReference{RigDefinition}, in WeightMaskSampler)"/>
    /// to get an <see cref="AnimationStreamMask"/> that reads these weights, for example to blend only the upper body
    /// of a character.
    ///
    /// Call <see cref="AvatarMaskExtensions.ToWeightMaskSampler(AvatarMask, BlobAssetReference{RigDefinition}, Allocator)"/>
    /// or <see cref="WeightMaskExtensions.ToWeightMaskSampler(BlobAssetReference{WeightMask}, BlobAssetReference{RigDefinition}, Allocator)"/>
    /// to create a sampler for a rig. Call <see cref="Dispose"/> when you finish with it.
    ///
    /// Keep these rules in mind:
    ///
    ///- Creating a sampler is expensive. Create one sampler for each pair of weight mask and rig, keep it, and call
    ///  <see cref="Clone"/> to get more samplers for the same weight mask and rig.
    ///- Copies of the struct share the same memory. Dispose only one copy. A clone has its own memory, so dispose
    ///  each clone too.
    ///- Don't store a sampler in a component or a buffer element. The job system refuses to schedule a job that
    ///  accesses a component that holds a native container.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BodyMaskExample.cs#MaskUpperOrLowerBody}]]></code>
    /// </example>
    /// <seealso cref="AvatarMaskExtensions"/>
    /// <seealso cref="WeightMaskExtensions"/>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct WeightMaskSampler : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal WeightMaskSamplerData* m_Data;

        internal AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<WeightMaskSampler>();

        // Takes over instance
        internal WeightMaskSampler(BlobAssetReference<WeightMaskInstance> instance, Allocator allocator)
        {
            ValidateAllocator(allocator);

            this = default;
            m_Data = AllocatorManager.Allocate<WeightMaskSamplerData>(allocator);
            *m_Data = new WeightMaskSamplerData
            {
                Allocator = allocator,
                Instance = instance,
            };

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<WeightMaskSampler>(ref m_Safety, ref s_SafetyId.Data);
        }

        /// <summary>Whether the sampler has allocated memory.</summary>
        /// <remarks>
        /// The value is `false` for a default sampler and for the copy of the sampler on which you called
        /// <see cref="Dispose"/>.
        /// </remarks>
        public bool IsCreated => m_Data != null;

        internal BlobAssetReference<WeightMaskInstance> Instance => m_Data->Instance;

        /// <summary>Creates a sampler that holds the same weights for the same rig, with its own memory.</summary>
        /// <remarks>
        /// The clone copies the weights of this sampler, so it's much faster than creating a sampler from a weight
        /// mask. It uses the allocator of this sampler. The clone doesn't share memory with this sampler. Dispose it
        /// separately. You can use the clone after you dispose this sampler.
        /// </remarks>
        /// <returns>The new sampler. Dispose it when you finish with it. A default sampler if this sampler isn't
        /// created.</returns>
        /// <exception cref="InvalidOperationException">Thrown if safety checks are enabled and you disposed another
        /// copy of the sampler.</exception>
        public WeightMaskSampler Clone()
        {
            if (m_Data == null)
                return default;

            ValidateCanRead();
            return new WeightMaskSampler(m_Data->Instance.Clone(m_Data->Allocator), m_Data->Allocator);
        }

        /// <summary>Releases the memory of the sampler.</summary>
        /// <remarks>
        /// Copies of the sampler share the same memory. Call this method on only one copy, and don't use the other
        /// copies or the masks that you created from the sampler afterward. A sampler from <see cref="Clone"/> has its
        /// own memory, so this method doesn't affect it. This method has no effect on a default sampler.
        ///
        /// If safety checks are enabled, a second call to this method throws an `ObjectDisposedException` on any copy.
        /// For a sampler that uses <see cref="Allocator.Temp"/>, the safety system doesn't detect that you use another copy
        /// after this call, as for other containers that use <see cref="Allocator.Temp"/>. The memory stays valid until
        /// the end of the scope of the temporary allocations.
        /// </remarks>
        public void Dispose()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            if (m_Data == null)
                return;

            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
            var allocator = m_Data->Allocator;
            m_Data->Instance.Dispose();
            AllocatorManager.Free(allocator, m_Data);
            m_Data = null;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void ValidateAllocator(Allocator allocator)
        {
            if ((int)allocator <= (int)Allocator.None)
                throw new ArgumentException("The allocator must be a valid allocator.", "allocator");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateCanRead()
        {
            if (m_Data == null)
                throw new NullReferenceException("The WeightMaskSampler is not created.");
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        // Call after ValidateCanRead
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal readonly void ValidateRig(BlobAssetReference<RigDefinition> rig)
        {
            if (m_Data->Instance.Value.RigId != (ulong)rig.GetUnsafePtr())
                throw new InvalidOperationException("The WeightMaskSampler was created for another RigDefinition.");
        }
    }

    /// <summary>Extension methods to create a <see cref="WeightMaskSampler"/> from a weight mask.</summary>
    /// <seealso cref="WeightMaskBuilder"/>
    public static class WeightMaskExtensions
    {
        /// <summary>Creates a <see cref="WeightMaskSampler"/> that holds the weights of a weight mask for a rig.</summary>
        /// <remarks>
        /// This method is expensive. It matches every channel of the weight mask with the channels of the rig. Create
        /// one sampler for each pair of weight mask and rig, for example when your content loads, and keep it. When
        /// you need more samplers for the same weight mask and rig, call <see cref="WeightMaskSampler.Clone"/> on the
        /// sampler you kept. A clone only copies memory.
        ///
        /// The sampler copies the weights, so you can dispose <paramref name="weightMask"/> afterward. Dispose the
        /// sampler when you finish with it.
        /// </remarks>
        /// <param name="weightMask">The weight mask, for example from <see cref="WeightMaskBuilder.ToWeightMask"/>.</param>
        /// <param name="rig">The rig of the animation streams that the mask applies to.</param>
        /// <param name="allocator">The allocator to use for the memory of the sampler. Use a valid allocator.
        /// <see cref="Allocator.Invalid"/> and <see cref="Allocator.None"/> aren't allowed.</param>
        /// <returns>The new sampler. <see cref="WeightMaskSampler.IsCreated"/> is `false` if
        /// <paramref name="weightMask"/> or <paramref name="rig"/> isn't created.</returns>
        /// <exception cref="ArgumentException">Thrown if safety checks are enabled and <paramref name="allocator"/>
        /// isn't a valid allocator.</exception>
        public static WeightMaskSampler ToWeightMaskSampler(this BlobAssetReference<WeightMask> weightMask, BlobAssetReference<RigDefinition> rig, Allocator allocator)
        {
            WeightMaskSampler.ValidateAllocator(allocator);

            if (!weightMask.IsCreated || !rig.IsCreated)
                return default;

            var instance = WeightMaskInstance.Create(rig, weightMask, allocator);
            return instance.IsCreated ? new WeightMaskSampler(instance, allocator) : default;
        }
    }
}
