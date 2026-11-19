// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animations
{
    public partial struct RigDefinition
    {
        /// <summary>
        /// Create RigDefinition BlobAssetReference from a list of Animation Clips.
        /// The resulting bindings are collected from the Animation Clips and default
        /// values are resolved using the animated transform hierarchy.
        /// </summary>
        /// <remarks>
        /// Each transform that a clip animates becomes a bone, and so does each of its parents up to
        /// <paramref name="root"/>. The identifier of a bone is the <see cref="StringHash"/> of its path relative to
        /// <paramref name="root"/>. Use <see cref="RigDefinitionUtility.BindTransforms"/> to find the transform of
        /// each bone. Each other property that a clip animates becomes a channel, and its default value is the current
        /// value of the property.
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
        /// <param name="root">Root of the animated hierarchy.</param>
        /// <param name="animationClips">Collection of Animation Clips.</param>
        /// <param name="allocator">Allocator used for RigDefinition. Allocator is a member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.
        /// </param>
        /// <returns>Returns a RigDefinition BlobAssetReference</returns>
        [ExcludeFromBurstCompatTesting("Takes managed Transform and collection of Animation Clips")]
        public static BlobAssetReference<RigDefinition> Create(
            Transform root,
            AnimationClip[] animationClips,
            Allocator allocator)
        {
            var rigBuilder = RigBuilder.Create(root, animationClips, Allocator.Temp);

            var rigDefinition = rigBuilder.ToRigDefinition(allocator);

            rigBuilder.Dispose();

            return rigDefinition;
        }
    }
}
