// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Animations
{
    /// <summary>Extension methods for avatars that give access to the rig definition of an avatar.</summary>
    /// <remarks>
    /// Call <see cref="ToRigReference(Avatar)"/> to get a reference to the rig definition of an avatar. The rig
    /// definition has a bone for each transform of the avatar. Convert the reference with
    /// <see cref="BlobObjectReference{T}.AsBlobAssetReference"/> to read the data from jobs, or to create a
    /// <see cref="ClipSampler"/> for the avatar.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.IntegerTime`,
    /// `Unity.Mathematics`, and `UnityEngine` namespaces. It plays an animation clip on the hierarchy of an avatar,
    /// with the rig definition of the avatar.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/SampleClipWithAvatarExample.cs#SampleClipWithAvatar}]]></code>
    /// </example>
    /// <seealso cref="RigDefinition"/>
    public static class AvatarExtensions
    {
        /// <summary>Acquires a reference to the rig definition of an avatar.</summary>
        /// <remarks>
        /// Use this method to get the rig definition in a form that jobs and Burst-compiled code can read. The first
        /// call builds the rig definition from the avatar. The reference keeps the data alive until you dispose it,
        /// even if the avatar is destroyed or modified. Call this method from the main thread.
        /// </remarks>
        /// <param name="avatar">The avatar to get the rig definition from. Can be null.</param>
        /// <returns>The reference to the <see cref="RigDefinition"/> of the avatar. Dispose the reference when you
        /// finish with it. <see cref="BlobObjectReference{T}.IsValid"/> is `false` if <paramref name="avatar"/> is
        /// `null` or is not a valid avatar.</returns>
        public static BlobObjectReference<RigDefinition> ToRigReference(this Avatar avatar)
        {
            return avatar != null ? avatar.AcquireReference<RigDefinition>(RigDefinition.BlobVersion) : default;
        }
    }
}
