// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animations
{
    public partial struct WeightMaskBuilder
    {
        /// <summary>
        /// Create WeightMaskBuilder from an AvatarMask asset.
        /// </summary>
        /// <remarks>
        /// Use this method to change the weight mask of an AvatarMask, for example to propagate the weight of a
        /// transform to its children or to mask generic channels. To use the weight mask of an AvatarMask as it is,
        /// read the weight mask that the AvatarMask stores with
        /// <see cref="AvatarMaskExtensions.ToWeightMaskReference(AvatarMask)"/> instead of building a new one.
        ///
        /// The builder has the same transform masking as an AvatarMask in an Animator layer. If the AvatarMask has no
        /// transforms, <see cref="DefaultTransformWeight"/> is 1 and every transform is included. Otherwise,
        /// <see cref="DefaultTransformWeight"/> is 0 and each transform of the AvatarMask has its weight, without
        /// propagating it to its children. <see cref="DefaultGenericWeight"/> is 1. Humanoid body parts aren't part of
        /// the weight mask.
        /// </remarks>
        /// <param name="avatarMask">AvatarMask asset.</param>
        /// <param name="allocator">Allocator used for WeightMaskBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a WeightMaskBuilder with the transform weights of the AvatarMask.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="avatarMask"/> is null.</exception>
        [ExcludeFromBurstCompatTesting("Takes managed AvatarMask")]
        public static WeightMaskBuilder Create(AvatarMask avatarMask, Allocator allocator)
        {
            if (avatarMask == null)
                throw new ArgumentNullException(nameof(avatarMask));

            var transformCount = avatarMask.transformCount;
            var builder = new WeightMaskBuilder(allocator, defaultTransformWeight: transformCount == 0 ? 1f : 0f, defaultGenericWeight: 1f);
            for (int i = 0; i < transformCount; i++)
            {
                var id = new StringHash(avatarMask.GetTransformPath(i));
                var weight = avatarMask.GetTransformWeight(i);

                // An AvatarMask can list a path more than once. An Animator layer includes the transform if any of
                // its entries is active, so keep the highest weight.
                if (builder.m_Transforms.TryGetValue(id, out var existing))
                    weight = math.max(weight, existing.Value);

                builder.SetTransformMask(id, weight, propagateWeight: false);
            }

            return builder;
        }
    }
}
