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
        /// The method searches the hierarchy under <paramref name="root"/>, including <paramref name="root"/> itself.
        /// A transform matches a bone when the <see cref="StringHash"/> of its path relative to
        /// <paramref name="root"/> equals the identifier of the bone. The root has an empty path. If several transforms
        /// have the same path, the last one in depth-first order is used. This method matches the behavior of
        /// [Avatar.BindTransforms](https://docs.unity3d.com/ScriptReference/Avatar.BindTransforms.html).
        ///
        /// The following examples use the `Unity.Animation`, `Unity.Collections`, `Unity.Entities`,
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
        /// <param name="root">The root of the hierarchy to search.</param>
        /// <returns>
        /// An array with one transform for each bone, indexed by bone index. The entry of a bone that has no matching
        /// transform is null.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> is null.</exception>
        public static Transform[] BindTransforms(ref RigDefinition rigDefinition, Transform root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            ref var skeleton = ref rigDefinition.Skeleton;
            var boundTransforms = new Transform[skeleton.BoneCount];
            BindTransformsRecursive(ref skeleton, root, 0, boundTransforms);

            return boundTransforms;
        }

        static void BindTransformsRecursive(ref Skeleton skeleton, Transform transform, uint pathHash, Transform[] boundTransforms)
        {
            var boneIndex = skeleton.FindBoneIndex(pathHash);
            if (boneIndex >= 0)
                boundTransforms[boneIndex] = transform;

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
