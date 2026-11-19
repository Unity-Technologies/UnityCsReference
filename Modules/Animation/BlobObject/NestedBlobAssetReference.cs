// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Animations;
using Unity.Entities;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Reference to the blob of a BlobObject, an AnimationClip or an Avatar, stored inside the data of a blob and set with BlobObject.SetNestedReferenceValue.
    // It must always be accessed by reference and never by value (data must not be copied/moved)
    [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
    internal unsafe struct NestedBlobAssetReference
    {
        internal BlobObjectReference Reference;

        public readonly bool IsValid => Reference.BlobData != null;

        public readonly EntityId ObjectId => Reference.ObjectId;

        // Root of the referenced blob, after its BlobAssetHeader
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal readonly void* GetUnsafeRootPtr<T>() where T : unmanaged
        {
            ValidateTypeCompatibility<T>();

            var blobData = Reference.BlobData;
            return blobData != null ? blobData + BlobObject.k_BlobAssetHeaderSize : null;
        }

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal readonly BlobAssetReference<T> AsBlobAssetReference<T>() where T : unmanaged
        {
            return BlobAssetExtensions.CreateBorrowedBlobAssetReference<T>(GetUnsafeRootPtr<T>());
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void ValidateTypeCompatibility<T>() where T : unmanaged
        {
            // The expected hash is 0 in Burst code until managed code computed it
            var expectedTypeHash = BlobObjectReference<T>.TypeHash;
            if (IsValid && expectedTypeHash != 0 && Reference.BlobTypeHash != expectedTypeHash)
            {
                throw new InvalidCastException($"The referenced BlobAsset is not of type {typeof(T).Name}");
            }
        }
    }
}
