// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>
    /// Utility methods for rig definitions.
    /// </summary>
    public static class RigDefinitionUtility
    {
        /// <summary>
        /// Finds the transform that matches each bone of a rig in a hierarchy.
        /// </summary>
        /// <remarks>
        /// The method searches the hierarchy under <paramref name="rootTransform"/>, including
        /// <paramref name="rootTransform"/> itself. A transform matches a bone when the <see cref="StringHash"/> of its
        /// path relative to <paramref name="rootTransform"/> equals the identifier of the bone.
        /// <paramref name="rootTransform"/> has an empty path. If several transforms have the same path, the first one
        /// in depth-first order is used, and the method logs a warning and ignores the others and their children. For a
        /// rig that you build with
        /// <see cref="RigBuilder.Create(Transform, AnimationClip[], Unity.Collections.Allocator)"/>, which follows the
        /// same rule, pass the same transform, which matches the root bone.
        ///
        /// The following examples use the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`,
        /// `Unity.IntegerTime`, `Unity.Mathematics`, and `UnityEngine` namespaces. Each example is a `MonoBehaviour`
        /// that plays a clip on the hierarchy under its GameObject. It gets a rig definition and finds the transform of
        /// each bone once. Each frame, it samples the clip into an animation stream and writes the pose to the
        /// transforms. The first example builds the rig definition from the clip. The second example uses the rig
        /// definition that an Avatar stores.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipToTransformsExample.cs#SampleClipToTransforms}]]></code>
        /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipWithAvatarExample.cs#SampleClipWithAvatar}]]></code>
        /// </example>
        /// <param name="rigDefinition">The rig definition that contains the bones to bind.</param>
        /// <param name="rootTransform">The transform of the root bone. It is advised to have the root bone transform as the direct child of the animated GameObject (usually a character).</param>
        /// <returns>
        /// An array with one transform for each bone, indexed by bone index. The entry of a bone that has no matching
        /// transform is null.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="rootTransform"/> is null.</exception>
        public static Transform[] BindTransforms(ref RigDefinition rigDefinition, Transform rootTransform)
        {
            if (rootTransform == null)
                throw new ArgumentNullException(nameof(rootTransform));

            ref var skeleton = ref rigDefinition.Skeleton;
            var boundTransforms = new Transform[skeleton.BoneCount];
            BindTransformsRecursive(ref skeleton, rootTransform, 0, boundTransforms);

            return boundTransforms;
        }

        static void BindTransformsRecursive(ref Skeleton skeleton, Transform transform, uint pathHash, Transform[] boundTransforms)
        {
            var boneIndex = skeleton.FindBoneIndex(pathHash);
            if (boneIndex >= 0)
            {
                if (boundTransforms[boneIndex] != null)
                {
                    Debug.LogWarning(
                        $"The transform '{transform.name}' has the same path as another transform. " +
                        "It and its children aren't bound.",
                        transform);
                    return;
                }

                boundTransforms[boneIndex] = transform;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                var child = transform.GetChild(i);

                // Same as native AppendPathToHash: the root path is empty, so its children have no separator
                var childPathHash = pathHash != 0 ? Crc32Utility.Append(pathHash, "/") : 0;
                childPathHash = Crc32Utility.Append(childPathHash, child.name);

                BindTransformsRecursive(ref skeleton, child, childPathHash, boundTransforms);
            }
        }
    }
}
