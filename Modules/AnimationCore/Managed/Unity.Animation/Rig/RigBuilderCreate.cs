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
        /// <param name="root">Root of the animated hierarchy.</param>
        /// <param name="animationClips">Collection of Animation Clips.</param>
        /// <param name="allocator">Allocator for the generic bindings array Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <param name="transforms">Resulting array of transforms.</param>
        /// <param name="genericBindings">Resulting native array of generic bindings.</param>
        [ExcludeFromBurstCompatTesting("Takes managed Transform and AnimationClips")]
        internal static void CollectClipBindings(
            Transform root,
            AnimationClip[] animationClips,
            Allocator allocator,
            out Transform[] transforms,
            out NativeArray<GenericBinding> genericBindings)
        {
            using var uniqueTransformHashes = new NativeHashSet<StringHash>(100, Allocator.Temp);
            using var uniqueGenericBindings = new NativeHashSet<GenericBinding>(100, Allocator.Temp);

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
            root.GetComponentsInChildren(true, allTransforms);

            var allTransformFlags = new NativeArray<bool>(allTransforms.Count, Allocator.Temp);
            int totalCount = 0;

            for (int i = 0; i < allTransforms.Count; ++i)
            {
                var transform = allTransforms[i];
                var path = TransformUtility.ComputeRelativePath(transform, root.transform);
                var hash = new StringHash(path);

                if (uniqueTransformHashes.Contains(hash))
                {
                    allTransformFlags[i] = true;
                    ++totalCount;

                    Transform current = transform;
                    while (current != root.transform)
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
        /// Create RigBuilder from a list of Animation Clips. The resulting bindings are
        /// collected from the Animation Clips and default values are resolved using
        /// the animated transform hierarchy.
        /// </summary>
        /// <param name="root">Root of the animated hierarchy.</param>
        /// <param name="animationClips">Collection of Animation Clips.</param>
        /// <param name="allocator">Allocator used for RigBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a RigBuilder with channels extracted from the Animation Clips.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed Transform and AnimationClips")]
        public static RigBuilder Create(
            Transform root,
            AnimationClip[] animationClips,
            Allocator allocator)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            CollectClipBindings(root, animationClips ?? Array.Empty<AnimationClip>(), Allocator.Temp, out var transforms, out var genericBindings);

            RigBuilder rigBuilder;
            try
            {
                rigBuilder = Create(root, transforms, ref genericBindings, allocator);
            }
            finally
            {
                genericBindings.Dispose();
            }

            return rigBuilder;
        }

        /// <summary>
        /// Create RigBuilder from a list of transforms and generic bindings.
        /// </summary>
        /// <param name="root">Root of the animated hierarchy.</param>
        /// <param name="transforms">List of Transforms to add to the RigBuilder.</param>
        /// <param name="genericBindings">List of generic bindings to add to the RigBuilder.</param>
        /// <param name="allocator">Allocator used for RigBuilder. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a RigBuilder with channels extracted from transforms and generic bindings.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed Transforms")]
        internal static RigBuilder Create(
            Transform root,
            Transform[] transforms,
            ref NativeArray<GenericBinding> genericBindings,
            Allocator allocator)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var rigBuilder = new RigBuilder(allocator);

            var ids = new NativeArray<StringHash>(transforms.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var parentIndices = new NativeArray<int>(transforms.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var transformAccessArray = new TransformAccessArray(transforms);

            for (int i = 0; i < transforms.Length; ++i)
            {
                // IDs need to match those in GenericBinding.
                var transform = transforms[i];

                var path = TransformUtility.ComputeRelativePath(transform, root.transform);
                var hash = new StringHash(path);

                ids[i] = hash;

                if (transform != root.transform)
                {
                    var parentTransform = transform.parent;
                    parentIndices[i] = Array.FindIndex(transforms, t => t == parentTransform);
                }
                else
                {
                    parentIndices[i] = -1;
                }
            }

            AddTransformNodesToRigBuilder(ref rigBuilder, ref transformAccessArray, ref ids, ref parentIndices);

            ids.Dispose();
            parentIndices.Dispose();
            transformAccessArray.Dispose();

            // Add float, discrete and entity id bindings to the RigBuilder
            // and fetch the default values using GenericBindingUtility.
            // The properties of each kind are index-aligned with the sorted bindings of that kind. A binding that
            // doesn't resolve in the hierarchy gets a null property, and its channel keeps the zero default value.
            GenericBindingUtility.BindProperties(root.gameObject, genericBindings, out var floatProperties,
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
            for (var i = 0; i < boneCount; ++i)
            {
                var boneID = avatar.GetBoneID(i);
                rigBuilder.Add(new LocalTranslationChannel() { Id = boneID, DefaultValue = avatar.GetBoneLocalPosition(i) });
                rigBuilder.Add(new LocalRotationChannel() { Id = boneID, DefaultValue = avatar.GetBoneLocalRotation(i) });
                rigBuilder.Add(new LocalScaleChannel() { Id = boneID, DefaultValue = avatar.GetBoneLocalScale(i) });

                rigBuilder.Add(new SkeletonNode()
                {
                    Id = boneID,
                    ParentIndex = avatar.GetParentBoneIndex(i),
                    LocalRotationDefaultValue = avatar.GetBoneLocalRotation(i),
                    LocalTranslationDefaultValue = avatar.GetBoneLocalPosition(i),
                    LocalScaleDefaultValue = avatar.GetBoneLocalScale(i)
                });
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
