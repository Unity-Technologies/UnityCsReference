// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>Extension methods for avatar masks that give access to the weight mask of an avatar mask.</summary>
    /// <remarks>
    /// Call <see cref="ToWeightMaskReference(AvatarMask)"/> to get a reference to the weight mask of an avatar mask.
    /// The weight mask has the weight of each transform of the avatar mask. Humanoid body parts aren't part of the
    /// weight mask. Convert the reference with <see cref="BlobObjectReference{T}.AsBlobAssetReference"/> to read the
    /// data from jobs. Call <see cref="ToWeightMaskSampler(AvatarMask, BlobAssetReference{RigDefinition}, Allocator)"/>
    /// to get the weights of the avatar mask for a rig.
    /// </remarks>
    /// <seealso cref="WeightMask"/>
    public static class AvatarMaskExtensions
    {
        /// <summary>Acquires a reference to the weight mask of an avatar mask.</summary>
        /// <remarks>
        /// Use this method to get the weight mask in a form that jobs and Burst-compiled code can read. The first
        /// call after the avatar mask changes builds the weight mask from the avatar mask. If the avatar mask has no
        /// transforms, the weight mask includes every transform. Otherwise, it includes only the transforms of the
        /// avatar mask, with their weights. The reference keeps the data alive until you dispose it, even if the
        /// avatar mask is destroyed or modified. Call this method from the main thread.
        /// </remarks>
        /// <param name="mask">The avatar mask to get the weight mask from. Can be null.</param>
        /// <returns>The reference to the <see cref="WeightMask"/> of the avatar mask. Dispose the reference when you
        /// finish with it. <see cref="BlobObjectReference{T}.IsValid"/> is `false` if <paramref name="mask"/> is
        /// `null`.</returns>
        public static BlobObjectReference<WeightMask> ToWeightMaskReference(this AvatarMask mask)
        {
            return mask != null ? mask.AcquireReference<WeightMask>(WeightMask.BlobVersion) : default;
        }

        /// <summary>Creates a <see cref="WeightMaskSampler"/> that holds the weights of an avatar mask for a rig.</summary>
        /// <remarks>
        /// Call this method from the main thread. The sampler copies the weights, so it keeps them even if the avatar
        /// mask is destroyed or modified. To use the modified avatar mask, create the sampler again.
        ///
        /// This method is expensive. It matches every transform of the avatar mask with the channels of the rig.
        /// Create one sampler for each pair of avatar mask and rig, for example when your content loads, and keep it.
        /// When you need more samplers for the same avatar mask and rig, call <see cref="WeightMaskSampler.Clone"/> on
        /// the sampler you kept. A clone only copies memory.
        ///
        /// Dispose the sampler when you finish with it.
        /// </remarks>
        /// <param name="mask">The avatar mask to get the weights from. Can be null.</param>
        /// <param name="rig">The rig of the animation streams that the mask applies to.</param>
        /// <param name="allocator">The allocator to use for the memory of the sampler. Use a valid allocator.
        /// <see cref="Allocator.Invalid"/> and <see cref="Allocator.None"/> aren't allowed.</param>
        /// <returns>The new sampler. <see cref="WeightMaskSampler.IsCreated"/> is `false` if
        /// <paramref name="mask"/> is `null` or `rig.IsCreated` is `false`.</returns>
        /// <exception cref="System.ArgumentException">Thrown if safety checks are enabled and <paramref name="allocator"/>
        /// isn't a valid allocator.</exception>
        public static WeightMaskSampler ToWeightMaskSampler(this AvatarMask mask, BlobAssetReference<RigDefinition> rig, Allocator allocator)
        {
            WeightMaskSampler.ValidateAllocator(allocator);

            if (mask == null || !rig.IsCreated)
                return default;

            using var weightMask = mask.ToWeightMaskReference();
            return weightMask.IsValid ? weightMask.AsBlobAssetReference().ToWeightMaskSampler(rig, allocator) : default;
        }
    }
}
