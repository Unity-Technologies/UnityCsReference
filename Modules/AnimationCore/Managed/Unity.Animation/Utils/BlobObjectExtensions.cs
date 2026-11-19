// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animations
{
    /// <summary>
    /// Extension methods for a BlobObject that you reference with a UnityObjectRef.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AcquireReference{T}(UnityObjectRef{BlobObject}, int)"/> when you hold the
    /// <see cref="BlobObject"/> as a <see cref="UnityObjectRef{T}"/>, for example in a component in the Entities
    /// package. The method returns a <see cref="BlobObjectReference{T}"/> that you dispose when you finish with it. To
    /// store data in the object, and for the type and version rules of the data, refer to
    /// `BlobObject.SetBlobAsset`.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Entities`, and `UnityEngine` namespaces. It reads a
    /// custom blob through a `UnityObjectRef` to a `BlobObject`, and returns 0 if the reference is invalid.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlobObjectExtensionsExample.cs#AcquireReferenceFromObjectRef}]]></code>
    /// </example>
    /// <seealso cref="BlobObject"/>
    /// <seealso cref="BlobObjectReference{T}"/>
    public static class BlobObjectExtensions
    {
        /// <summary>
        /// Acquires a reference to the blob data stored in the object that a UnityObjectRef points to.
        /// </summary>
        /// <remarks>
        /// Use this method when you hold the object as a `UnityObjectRef`, for example in an Entities component. It
        /// does the same as <see cref="BlobObject.AcquireReference{T}(int)"/>, and returns an invalid reference if the
        /// object reference doesn't point to an object. Call this method from the main thread.
        /// </remarks>
        /// <param name="objectRef">The reference to the object. If it is invalid, the returned reference is
        /// invalid.</param>
        /// <param name="version">The version of the layout of <typeparamref name="T"/> that the stored data must
        /// have.</param>
        /// <typeparam name="T">The type of the blob asset.</typeparam>
        /// <returns>The new reference. You must dispose it. The reference is invalid if the object is null, empty, or
        /// stores another version of the blob.</returns>
        public static BlobObjectReference<T> AcquireReference<T>(this UnityObjectRef<BlobObject> objectRef, int version = 0) where T : unmanaged
        {
            var blobObject = objectRef.Value;
            return blobObject != null ? blobObject.AcquireReference<T>(version) : default;
        }

        // Creates a new object holding a copy of the blob asset
        internal static BlobObject ToBlobObject<T>(this BlobAssetReference<T> blobAssetReference, int version = 0) where T : unmanaged
        {
            var blobObject = new BlobObject();
            blobObject.name = typeof(T).Name;
            blobObject.SetBlobAsset(blobAssetReference, version);

            return blobObject;
        }

        // Borrowed view: points into the live blob data without holding a reference, so it is invalidated when the
        // data is replaced or the object is destroyed. Throws on a type mismatch when collections checks are enabled.
        internal static unsafe BlobAssetReference<T> AsBlobAssetReference<T>(this BlobObject blobObject, int version = 0) where T : unmanaged
        {
            if (blobObject == null || !blobObject.IsBlobCompatible<T>(version))
                return default;

            return BlobAssetExtensions.CreateBorrowedBlobAssetReference<T>(blobObject.GetDataUnsafe<T>(version));
        }

        // Borrowed view that never throws: false if the object is null, empty, or stores another type or version
        internal static unsafe bool TryAsBlobAssetReference<T>(this BlobObject blobObject, out BlobAssetReference<T> blobAssetReference, int version = 0) where T : unmanaged
        {
            if (blobObject == null || !blobObject.IsBlobCompatible<T>(version, false))
            {
                blobAssetReference = default;
                return false;
            }

            blobAssetReference = BlobAssetExtensions.CreateBorrowedBlobAssetReference<T>(blobObject.GetDataUnsafe<T>(version));
            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void EnsureValidity<T>(this in BlobObjectReference<T> reference, FixedString64Bytes assetTypeName) where T : unmanaged
        {
            if (!reference.IsValid)
            {
                throw new InvalidOperationException($"Asset of type {assetTypeName.ToString()} is invalid, it has probably been unloaded.");
            }
        }
    }
}
