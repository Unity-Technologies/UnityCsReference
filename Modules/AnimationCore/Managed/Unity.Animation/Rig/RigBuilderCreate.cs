// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Jobs;
using UnityEngine.Pool;

namespace Unity.Animations
{
    struct GenericBindingComparer : IComparer<GenericBinding>
    {
        // Sorts by kind first, as RigBuilder.Create pairs the properties of each kind with the bindings by index.
        // The remaining keys make the order independent of the NativeHashSet iteration order.
        public int Compare(GenericBinding x, GenericBinding y)
        {
            int result = GetBindingOrder(x).CompareTo(GetBindingOrder(y));
            if (result != 0)
                return result;

            result = x.transformPathHash.CompareTo(y.transformPathHash);
            if (result != 0)
                return result;

            result = x.propertyNameHash.CompareTo(y.propertyNameHash);
            if (result != 0)
                return result;

            result = x.typeID.CompareTo(y.typeID);
            if (result != 0)
                return result;

            result = x.customTypeID.CompareTo(y.customTypeID);
            if (result != 0)
                return result;

            return x.scriptEntityId.CompareTo(y.scriptEntityId);
        }

        static int GetBindingOrder(GenericBinding binding)
        {
            if (binding.isObjectReference)
                return 2;
            if (binding.isDiscrete)
                return 1;
            return 0;
        }
    }

    public partial struct RigBuilder
    {
        const int k_TransformTypeID = 4;

        /// <summary>
        /// Collect transform and generic bindings from a collection of Animation Clips.
        /// </summary>
        /// <param name="rootTransform">The transform that becomes the root bone. Character space is the space of its
        /// parent.</param>
        /// <param name="animationClips">Collection of Animation Clips.</param>
        /// <param name="allocator">Allocator for the generic bindings array Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <param name="transforms">Resulting array of transforms.</param>
        /// <param name="genericBindings">Resulting native array of generic bindings.</param>
        /// <param name="extraTransformHash">The path hash of a transform to add as a bone even if no clip animates it,
        /// or null.</param>
        [ExcludeFromBurstCompatTesting("Takes managed Transform and AnimationClips")]
        internal static void CollectClipBindings(
            Transform rootTransform,
            AnimationClip[] animationClips,
            Allocator allocator,
            out Transform[] transforms,
            out NativeArray<GenericBinding> genericBindings,
            StringHash? extraTransformHash = null)
        {
            using var uniqueTransformHashes = new NativeHashSet<StringHash>(100, Allocator.Temp);
            using var uniqueGenericBindings = new NativeHashSet<GenericBinding>(100, Allocator.Temp);

            if (extraTransformHash.HasValue)
                uniqueTransformHashes.Add(extraTransformHash.Value);

            foreach (var clip in animationClips)
            {
                // TODO. Figure out proper API to handle Transform GenericBinding.
                //var bindings = GenericBindingUtility.GetCurveBindings(clip);
                var bindingCount = clip.GetGenericBindingCount();

                for (int i = 0; i < bindingCount; ++i)
                {
                    var binding = clip.GetGenericBinding(i);

                    if (binding.typeID == k_TransformTypeID)
                    {
                        uniqueTransformHashes.TryAdd(binding.transformPathHash);
                    }
                    else
                    {
                        uniqueGenericBindings.TryAdd(binding);
                    }
                }
            }

            using var _ = ListPool<Transform>.Get(out var allTransforms);
            rootTransform.GetComponentsInChildren(true, allTransforms);

            var allTransformFlags = new NativeArray<bool>(allTransforms.Count, Allocator.Temp);
            int totalCount = 0;

            for (int i = 0; i < allTransforms.Count; ++i)
            {
                var transform = allTransforms[i];
                var path = TransformUtility.ComputeRelativePath(transform, rootTransform);
                var hash = new StringHash(path);

                if (uniqueTransformHashes.Contains(hash))
                {
                    allTransformFlags[i] = true;
                    ++totalCount;

                    Transform current = transform;
                    while (current != rootTransform)
                    {
                        current = current.parent;

                        var parentIndex = allTransforms.FindIndex(0, i, (t) => t == current);
                        if (!allTransformFlags[parentIndex])
                        {
                            allTransformFlags[parentIndex] = true;
                            ++totalCount;
                        }
                    }
                }
            }

            transforms = new Transform[totalCount];
            int index = 0;
            for (int i = 0; i < allTransformFlags.Length; ++i)
            {
                if (allTransformFlags[i])
                {
                    transforms[index++] = allTransforms[i];
                }
            }

            allTransformFlags.Dispose();

            genericBindings = uniqueGenericBindings.ToNativeArray(allocator);
            genericBindings.Sort(new GenericBindingComparer());
        }

        /// <summary>
        /// Creates a RigBuilder with the bones and channels that a collection of Animation Clips animates.
        /// </summary>
        /// <remarks>
        /// Each transform that a clip animates becomes a bone, and so does each of its parents up to
        /// <paramref name="rootTransform"/>. The identifier of a bone is the <see cref="StringHash"/> of its path
        /// relative to <paramref name="rootTransform"/>, so the clips must use paths relative to
        /// <paramref name="rootTransform"/>. Use <see cref="RigDefinitionUtility.BindTransforms"/> to find the
        /// transform of each bone. Each other property that a clip animates becomes a float, integer, or entity ID
        /// channel.
        ///
        /// When the clips animate a transform, <paramref name="rootTransform"/> becomes the root bone, at index 0.
        /// Character space is the space of the parent of the root bone. If you pass the animated object itself,
        /// character space is the space of its parent, and character-space methods such as
        /// <see cref="AnimationStream.GetLocalToCharacterRotation"/> don't return values relative to the character.
        /// It's recommended to pass a direct child of the animated object, such as the top bone of the skeleton, so
        /// that character space is the space of the animated object.
        ///
        /// The default values of the bones and channels are the values in the hierarchy when you call this method.
        /// They include the local transform of <paramref name="rootTransform"/>, which is its placement relative to
        /// its parent, and any pose or scale that the hierarchy has in the scene. Two copies of a character at
        /// different places get rigs with different default values. To use other default values for a bone, such as
        /// the pose of the skeleton in the model asset, find the bone with <see cref="GetBoneIndex"/> and use
        /// <see cref="TrySetBoneDefaultTranslation"/>, <see cref="TrySetBoneDefaultRotation"/>, and
        /// <see cref="TrySetBoneDefaultScale"/>.
        ///
        /// If several transforms have the same path, the first one in depth-first order becomes the bone. The method
        /// logs a warning and skips the others and their children, so that the rig matches the transforms that
        /// <see cref="RigDefinitionUtility.BindTransforms"/> finds.
        ///
        /// To create the rig definition, call <see cref="ToRigDefinition"/> on the builder. Before that, you can
        /// change the builder, for example to add sockets or bones that only your code animates. Dispose the builder
        /// when you no longer need it.
        ///
        /// The following examples use the `System`, `Unity.Animation`, `Unity.Collections`, `Unity.Entities`,
        /// `Unity.IntegerTime`, `Unity.Mathematics`, `UnityEngine`, and `UnityEngine.Animations` namespaces. Each
        /// example is a `MonoBehaviour` that plays a clip on the hierarchy under its GameObject. It builds a rig
        /// definition from the clip once. Each frame, it samples the clip. The first example writes the pose to the
        /// transforms. The second example writes the float, integer, and object reference channels to the properties
        /// of the components with <see cref="UnityEngine.Animations.GenericBindingUtility"/>.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipToTransformsExample.cs#SampleClipToTransforms}]]></code>
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipToComponentsExample.cs#SampleClipToComponents}]]></code>
        /// </example>
        /// <param name="rootTransform">The transform that becomes the root bone. Character space is the space of its
        /// parent.</param>
        /// <param name="animationClips">Collection of Animation Clips. If null, the builder is empty.</param>
        /// <param name="allocator">Allocator used for RigBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>A RigBuilder with the bones and channels that the clips animate.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="rootTransform"/> is null.</exception>
        [ExcludeFromBurstCompatTesting("Takes managed Transform and AnimationClips")]
        public static RigBuilder Create(
            Transform rootTransform,
            AnimationClip[] animationClips,
            Allocator allocator)
        {
            if (rootTransform == null)
                throw new ArgumentNullException(nameof(rootTransform));

            return Create(rootTransform, animationClips, null, allocator);
        }

        /// <summary>
        /// Creates a RigBuilder from a list of Animation Clips, with the root motion from a transform of the hierarchy.
        /// </summary>
        /// <remarks>
        /// The method collects the bones and channels as <see cref="Create(Transform, AnimationClip[], Allocator)"/>
        /// does, and sets <see cref="RootMotionBoneId"/> to the bone of
        /// <paramref name="rootMotionTransform"/>. The rig has a bone for <paramref name="rootMotionTransform"/> and each of its parents up to
        /// <paramref name="rootTransform"/>, even if no clip animates them.
        ///
        /// Use this method when the transform that carries the motion of the character isn't
        /// <paramref name="rootTransform"/>, for example a hips bone under a root that clips don't animate.
        /// <see cref="ClipSampler.SampleRootMotion(in TimerState)"/> then returns the motion of this transform in
        /// character space when a clip has no root motion curves, and
        /// <see cref="ClipSampler.SampleExtractingRootMotion(ref AnimationStream, in TimerState)"/>
        /// removes the root motion from this transform in the pose.
        /// </remarks>
        /// <param name="rootTransform">The transform that becomes the root bone. Character space is the space of its
        /// parent.</param>
        /// <param name="animationClips">Collection of Animation Clips. If null, the builder is empty.</param>
        /// <param name="rootMotionTransform">The transform whose motion gives the root motion. It must be
        /// <paramref name="rootTransform"/> or one of its children. If null, the root motion bone is the root bone, as
        /// with <see cref="Create(Transform, AnimationClip[], Allocator)"/>.</param>
        /// <param name="allocator">Allocator used for RigBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a RigBuilder with channels extracted from the Animation Clips.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rootTransform"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="rootMotionTransform"/> isn't
        /// <paramref name="rootTransform"/> or one of its children.</exception>
        [ExcludeFromBurstCompatTesting("Takes managed Transform and AnimationClips")]
        public static RigBuilder Create(
            Transform rootTransform,
            AnimationClip[] animationClips,
            Transform rootMotionTransform,
            Allocator allocator)
        {
            if (rootTransform == null)
                throw new ArgumentNullException(nameof(rootTransform));

            StringHash? rootMotionBoneId = null;
            if (rootMotionTransform != null)
            {
                if (rootMotionTransform != rootTransform && !rootMotionTransform.IsChildOf(rootTransform))
                {
                    throw new ArgumentException(
                        $"The root motion transform '{rootMotionTransform.name}' isn't '{rootTransform.name}' or one of its children.",
                        nameof(rootMotionTransform));
                }

                rootMotionBoneId = new StringHash(TransformUtility.ComputeRelativePath(rootMotionTransform, rootTransform));
            }

            CollectClipBindings(rootTransform, animationClips ?? Array.Empty<AnimationClip>(), Allocator.Temp,
                out var transforms, out var genericBindings, rootMotionBoneId);

            RigBuilder rigBuilder;
            try
            {
                rigBuilder = Create(rootTransform, transforms, ref genericBindings, allocator);
            }
            finally
            {
                genericBindings.Dispose();
            }

            if (rootMotionBoneId.HasValue)
                rigBuilder.RootMotionBoneId = rootMotionBoneId.Value;

            return rigBuilder;
        }

        /// <summary>
        /// Create RigBuilder from a list of transforms and generic bindings.
        /// </summary>
        /// <param name="rootTransform">The transform that becomes the root bone. Character space is the space of its
        /// parent.</param>
        /// <param name="transforms">List of Transforms to add to the RigBuilder.</param>
        /// <param name="genericBindings">List of generic bindings to add to the RigBuilder.</param>
        /// <param name="allocator">Allocator used for RigBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a RigBuilder with channels extracted from transforms and generic bindings.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed Transforms")]
        internal static RigBuilder Create(
            Transform rootTransform,
            Transform[] transforms,
            ref NativeArray<GenericBinding> genericBindings,
            Allocator allocator)
        {
            if (rootTransform == null)
                throw new ArgumentNullException(nameof(rootTransform));

            var rigBuilder = new RigBuilder(allocator);

            // Of the transforms that share a path, only the first one and its children become bones, like BindTransforms
            var boneIds = new HashSet<StringHash>();
            var boneIndices = new Dictionary<Transform, int>(transforms.Length);
            var bones = new List<Transform>(transforms.Length);
            var ids = new NativeList<StringHash>(transforms.Length, Allocator.Temp);
            var boneParentIndices = new NativeList<int>(transforms.Length, Allocator.Temp);
            foreach (var transform in transforms)
            {
                // IDs need to match those in GenericBinding.
                var id = new StringHash(TransformUtility.ComputeRelativePath(transform, rootTransform));
                var parentIndex = -1;
                if (transform != rootTransform && !boneIndices.TryGetValue(transform.parent, out parentIndex))
                    continue;

                if (!boneIds.Add(id))
                {
                    Debug.LogWarning(
                        $"The transform '{transform.name}' has the same path as another transform under " +
                        $"'{rootTransform.name}'. The rig ignores it and its children.",
                        transform);
                    continue;
                }

                boneIndices.Add(transform, bones.Count);
                bones.Add(transform);
                ids.Add(id);
                boneParentIndices.Add(parentIndex);
            }

            var transformAccessArray = new TransformAccessArray(bones.ToArray());
            var idArray = ids.AsArray();
            var parentIndices = boneParentIndices.AsArray();

            AddTransformNodesToRigBuilder(ref rigBuilder, ref transformAccessArray, ref idArray, ref parentIndices);

            ids.Dispose();
            boneParentIndices.Dispose();
            transformAccessArray.Dispose();

            // Add float, discrete and entity id bindings to the RigBuilder
            // and fetch the default values using GenericBindingUtility.
            // The properties of each kind are index-aligned with the sorted bindings of that kind. A binding that
            // doesn't resolve in the hierarchy gets a null property, and its channel keeps the zero default value.
            GenericBindingUtility.BindProperties(rootTransform.gameObject, genericBindings, out var floatProperties,
                out var discreteProperties, out var entityIDProperties, Allocator.Temp);

            AddGenericChannelsToRigBuilder(ref rigBuilder, ref genericBindings, ref floatProperties, ref discreteProperties, ref entityIDProperties);

            // Cleanup.
            GenericBindingUtility.UnbindProperties(floatProperties);
            GenericBindingUtility.UnbindProperties(discreteProperties);
            GenericBindingUtility.UnbindProperties(entityIDProperties);

            floatProperties.Dispose();
            discreteProperties.Dispose();
            entityIDProperties.Dispose();

            return rigBuilder;
        }

        /// <summary>
        /// Create RigBuilder from an Avatar asset.
        /// </summary>
        /// <remarks>
        /// Use this method to change the rig of an Avatar, for example to add sockets. To use the rig of an Avatar as
        /// it is, read the rig definition that the Avatar stores with
        /// <see cref="AvatarExtensions.ToRigReference(Avatar)"/> instead of building a new one.
        ///
        /// If the Avatar is a Generic Avatar with a **Root node**, set in the **Rig** tab of the Model Import Settings,
        /// the method sets that bone as the root motion bone of the builder, in <see cref="RootMotionBoneId"/>.
        ///
        /// The following example uses the `Unity.Animation`, `Unity.Collections`, `Unity.Entities`,
        /// `Unity.IntegerTime`, `Unity.Mathematics`, and `UnityEngine` namespaces. It is a `MonoBehaviour` that plays a
        /// clip on the hierarchy under its GameObject with the rig definition that an Avatar stores.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipWithAvatarExample.cs#SampleClipWithAvatar}]]></code>
        /// </example>
        /// <param name="avatar">Avatar asset</param>
        /// <param name="allocator">Allocator used for RigBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a RigBuilder with channels extracted from the Avatar.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed Avatar")]
        public static RigBuilder Create(Avatar avatar, Allocator allocator)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));

            var rigBuilder = new RigBuilder(allocator);
            var boneCount = avatar.boneCount;

            // Of the bones that share a path, only the first one and its children are kept, like BindTransforms
            var boneIndices = new NativeArray<int>(boneCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < boneCount; ++i)
            {
                StringHash boneID = avatar.GetBoneID(i);
                var avatarParentIndex = avatar.GetParentBoneIndex(i);
                var isParentSkipped =
                    avatarParentIndex >= 0 && avatarParentIndex < i && boneIndices[avatarParentIndex] < 0;
                if (isParentSkipped || rigBuilder.GetBoneIndex(boneID) >= 0)
                {
                    boneIndices[i] = -1;
                    if (!isParentSkipped)
                    {
                        Debug.LogWarning(
                            $"The Avatar '{avatar.name}' has several bones with the same path. " +
                            "The rig ignores all but the first one, and the children of the others.",
                            avatar);
                    }
                    continue;
                }

                boneIndices[i] = rigBuilder.m_SkeletonNodes.Length;
                rigBuilder.Add(new SkeletonNode()
                {
                    Id = boneID,
                    ParentIndex = avatarParentIndex >= 0 && avatarParentIndex < i ? boneIndices[avatarParentIndex] : -1,
                    LocalRotationDefaultValue = avatar.GetBoneLocalRotation(i),
                    LocalTranslationDefaultValue = avatar.GetBoneLocalPosition(i),
                    LocalScaleDefaultValue = avatar.GetBoneLocalScale(i)
                });
            }

            boneIndices.Dispose();

            // A root motion bone under a skipped bone isn't in the rig, which then takes the root motion from the root
            var rootMotionBoneIndex = avatar.GetRootMotionBoneIndex();
            if (rootMotionBoneIndex >= 0 && rootMotionBoneIndex < boneCount)
            {
                StringHash rootMotionBoneId = avatar.GetBoneID(rootMotionBoneIndex);
                if (rigBuilder.GetBoneIndex(rootMotionBoneId) >= 0)
                    rigBuilder.RootMotionBoneId = rootMotionBoneId;
            }

            AddGenericProperties(ref rigBuilder, avatar);

            return rigBuilder;
        }

        [BurstCompile]
        static void AddTransformNodesToRigBuilder(
            ref RigBuilder rigBuilder,
            ref TransformAccessArray transforms,
            ref NativeArray<StringHash> ids,
            ref NativeArray<int> parentIndices)
        {
            for (int i = 0; i < transforms.length; ++i)
            {
                var transformHandle = transforms.GetTransformHandle(i);
                rigBuilder.Add(new SkeletonNode()
                {
                    Id = ids[i],
                    ParentIndex = parentIndices[i],
                    LocalRotationDefaultValue = transformHandle.localRotation,
                    LocalTranslationDefaultValue = transformHandle.localPosition,
                    LocalScaleDefaultValue = transformHandle.localScale
                });
            }
        }

        [BurstCompile]
        static void AddGenericChannelsToRigBuilder(
            ref RigBuilder rigBuilder,
            ref NativeArray<GenericBinding> genericBindings,
            ref NativeArray<BoundProperty> floatProperties,
            ref NativeArray<BoundProperty> discreteProperties,
            ref NativeArray<BoundProperty> entityIDProperties)
        {
            using var floatValues = new NativeArray<float>(floatProperties.Length, Allocator.Temp);
            using var discreteValues = new NativeArray<int>(discreteProperties.Length, Allocator.Temp);
            using var entityIDValues = new NativeArray<EntityId>(entityIDProperties.Length, Allocator.Temp);

            int index = 0;

            GenericBindingUtility.GetValues(floatProperties, floatValues);
            for (int i = 0; i < floatProperties.Length; ++i, ++index)
            {
                var propertyId = genericBindings[index];
                rigBuilder.Add(new FloatChannel { DefaultValue = floatValues[i], Id = propertyId });
            }

            GenericBindingUtility.GetValues(discreteProperties, discreteValues);
            for (int i = 0; i < discreteProperties.Length; ++i, ++index)
            {
                var propertyId = genericBindings[index];
                rigBuilder.Add(new IntChannel { DefaultValue = discreteValues[i], Id = propertyId });
            }

            GenericBindingUtility.GetValues(entityIDProperties, entityIDValues);
            for (int i = 0; i < entityIDProperties.Length; ++i, ++index)
            {
                var propertyId = genericBindings[index];
                rigBuilder.Add(new EntityIdChannel { DefaultValue = entityIDValues[i], Id = propertyId });
            }
        }

        static void AddGenericProperties(ref RigBuilder rigBuilder, Avatar avatar)
        {
            var genericBindings = avatar.genericBindings;
            if (genericBindings == null)
                return;

            using var bindings = new NativeArray<GenericBinding>(genericBindings, Allocator.Temp);
            for (var i = 0; i < genericBindings.Length; ++i)
            {
                var genericBinding = genericBindings[i];
                var propertyId = bindings[i];
                if (genericBinding.isObjectReference)
                    rigBuilder.Add(new EntityIdChannel() { DefaultValue = default, Id = propertyId });
                else if (genericBinding.isDiscrete)
                    rigBuilder.Add(new IntChannel() { DefaultValue = default, Id = propertyId });
                else
                    rigBuilder.Add(new FloatChannel() { DefaultValue = 0.0f, Id = propertyId });
            }
        }
    }
}
