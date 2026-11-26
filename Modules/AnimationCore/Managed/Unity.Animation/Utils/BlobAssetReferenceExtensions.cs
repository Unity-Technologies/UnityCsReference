// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Entities;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>
    /// Extension methods for a BlobAssetReference.
    /// </summary>
    /// <remarks>
    /// A <see cref="BlobAssetReference{T}"/> doesn't keep its data alive. If you store it in a job and dispose the blob
    /// asset, the job reads freed memory. Call
    /// <see cref="ToBlobObjectReference{T}(BlobAssetReference{T}, int)"/> to copy the blob asset into a
    /// <see cref="BlobObjectReference{T}"/> instead. The copy stays alive until you dispose the reference, so you can
    /// give it to jobs, and you can dispose the blob asset right away.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, `Unity.Jobs`, and
    /// `UnityEngine` namespaces. It builds a rig definition, copies it into a `BlobObjectReference`, disposes the blob
    /// asset, and reads the rig from a job.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlobAssetReferenceExtensionsExample.cs#ToBlobObjectReference}]]></code>
    /// </example>
    /// <seealso cref="BlobObjectReference{T}"/>
    public static class BlobAssetReferenceExtensions
    {
        /// <summary>
        /// Copies a blob asset into new blob data that a BlobObjectReference keeps alive.
        /// </summary>
        /// <remarks>
        /// The method copies the blob, so you can dispose <paramref name="blobAssetReference"/> after the call. No
        /// object owns the copy: the returned reference, and the references that you <see cref="BlobObjectReference{T}.Clone"/>
        /// from it, keep it alive. The copy is freed when you dispose the last of them.
        ///
        /// You can call this method from any thread.
        /// </remarks>
        /// <param name="blobAssetReference">The blob asset to copy.</param>
        /// <param name="version">The version of the layout of <typeparamref name="T"/> to store with the copy. The default
        /// value is 0.</param>
        /// <typeparam name="T">The type of the root of the blob. The type must be unmanaged.</typeparam>
        /// <returns>The new reference. You must dispose it. The reference is invalid if the `IsCreated` property of
        /// <paramref name="blobAssetReference"/> is `false`.</returns>
        public static unsafe BlobObjectReference<T> ToBlobObjectReference<T>(this BlobAssetReference<T> blobAssetReference, int version = 0) where T : unmanaged
        {
            if (!blobAssetReference.IsCreated)
                return default;

            var reference = BlobObjectReference.CreateOrphan(
                BlobObjectReference<T>.TypeHash,
                blobAssetReference.GetUnsafePtr(),
                (uint)blobAssetReference.GetLength(),
                version);

            return new BlobObjectReference<T>(reference);
        }
    }
}
