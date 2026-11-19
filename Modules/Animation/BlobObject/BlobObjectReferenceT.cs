// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Animations;
using Unity.Burst;
using Unity.Entities;
using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// A reference that keeps the blob data stored in a BlobObject alive, so that jobs and Burst-compiled code can read
    /// it.
    /// </summary>
    /// <remarks>
    /// Use this struct to read the blob data of a <see cref="BlobObject"/> from jobs and Burst-compiled code without
    /// copying it. Get a reference from <see cref="BlobObject.AcquireReference{T}(int)"/>, then check
    /// <see cref="IsValid"/> before you use it.
    ///
    /// The data stays alive until you call <see cref="Dispose"/>, even if the object is destroyed or its data is
    /// replaced, so always dispose the reference when you finish with it. Use <see cref="Clone"/> to get another
    /// reference that you dispose separately, for example to give another thread its own reference.
    ///
    /// You can't store the reference in serialized data. Reference the BlobObject itself instead.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, and `UnityEngine`
    /// namespaces. It stores a custom blob in a `BlobObject`, reads the data back through a `BlobObjectReference`, and
    /// then disposes the reference and destroys the object.
    /// </remarks>
    /// <typeparam name="T">Type of the root of the blob.</typeparam>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlobObjectExample.cs#BlobObjectExample}]]></code>
    /// </example>
    /// <seealso cref="BlobObject"/>
    public struct BlobObjectReference<T> : IDisposable where T : unmanaged
    {
        BlobObjectReference m_Reference;

        /// <summary>
        /// Hash that identifies the type of the blob root, used to check that a BlobObject stores the type you expect.
        /// </summary>
        /// <remarks>
        /// The hash is computed from the full name of the type, without its assembly, so it is the same in the Editor
        /// and in players. Renaming the type or moving it to another namespace changes the hash, and data stored with
        /// the old hash no longer matches.
        ///
        /// In Burst-compiled code, the value is 0 until managed code has read this property once.
        /// </remarks>
        public static ulong TypeHash => BlobTypeHash<T>.Get();

        /// <summary>
        /// Whether the reference points to blob data.
        /// </summary>
        /// <remarks>
        /// The value is `false` for a default reference, for a reference that you disposed, and for a reference that
        /// <see cref="BlobObject.AcquireReference{T}(int)"/> returned because the object was empty or stored another
        /// type or version of the blob. Destroying the object after you acquired the reference doesn't make it
        /// invalid.
        /// </remarks>
        public readonly bool IsValid
        {
            get
            {
                unsafe
                {
                    return m_Reference.BlobData != null;
                }
            }
        }

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal readonly EntityId ObjectId => m_Reference.ObjectId;

        // Root of the blob, after its BlobAssetHeader
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal readonly unsafe void* GetUnsafeRootPtr()
        {
            var blobData = m_Reference.BlobData;
            return blobData != null ? blobData + BlobObject.k_BlobAssetHeaderSize : null;
        }

        // Thread safe
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal BlobObjectReference(BlobObjectReference reference)
        {
            m_Reference = reference;
        }

        /// <summary>
        /// Returns a blob asset reference to the data that this reference points to.
        /// </summary>
        /// <remarks>
        /// Use this method to read the blob data through the `Value` property of the returned
        /// <see cref="BlobAssetReference{T}"/>. The returned value is a view of the data, not a copy. It stays valid
        /// only until you dispose this reference, and you must not dispose the returned value yourself.
        /// </remarks>
        /// <returns>The blob asset reference to the data. The value is a default value if <see cref="IsValid"/> is
        /// `false`.</returns>
        public readonly unsafe BlobAssetReference<T> AsBlobAssetReference()
        {
            return BlobAssetExtensions.CreateBorrowedBlobAssetReference<T>(GetUnsafeRootPtr());
        }

        /// <summary>
        /// Converts a blob object reference to a blob asset reference to the same data.
        /// </summary>
        /// <remarks>
        /// The result is a view of the data, not a copy. It is the same value that <see cref="AsBlobAssetReference"/>
        /// returns, and it is a default value if <see cref="IsValid"/> is `false`.
        ///
        /// The result stays valid only until you dispose the `BlobObjectReference`. Don't dispose the result yourself,
        /// and don't keep it after you dispose the `BlobObjectReference`. Reading the data
        /// after the dispose is undefined behavior. Call `AsBlobAssetReference` when you want the dependency on the
        /// reference to be visible in your code.
        /// </remarks>
        /// <param name="reference">The reference to convert.</param>
        /// <returns>The blob asset reference to the same data. The value is a default value if <see cref="IsValid"/> is
        /// `false`.</returns>
        public static implicit operator BlobAssetReference<T>(BlobObjectReference<T> reference) =>
            reference.AsBlobAssetReference();

        /// <summary>
        /// Acquires another reference to the same data. Both references must be disposed.
        /// </summary>
        /// <remarks>
        /// You can call this method from any thread, for example to give a job its own reference to the data. Cloning
        /// an invalid reference returns an invalid reference. Dispose each clone separately from the reference it came
        /// from.
        /// </remarks>
        /// <returns>The new reference to the same blob data. You must dispose it.</returns>
        public BlobObjectReference<T> Clone() => new BlobObjectReference<T>(m_Reference.Clone());

        /// <summary>
        /// Releases this reference to the blob data.
        /// </summary>
        /// <remarks>
        /// After you call this method, <see cref="IsValid"/> is `false` for this copy and you must not use pointers or
        /// blob asset references that you obtained from it. Disposing a default or already disposed reference has no
        /// effect.
        ///
        /// Copies of the struct share the same underlying reference, and disposing one copy doesn't invalidate the
        /// others. Dispose only one copy of each reference, and use <see cref="Clone"/> to get an independent one. This
        /// method is thread safe.
        /// </remarks>
        public void Dispose() => m_Reference.Dispose();
    }

    struct BlobTypeHashContext
    {
    }

    // The hash is computed from the type name on the main thread. Burst code can't use typeof, so it reads the
    // cached value, which is 0 if no managed code computed it yet.
    static class BlobTypeHash<T> where T : unmanaged
    {
        static readonly SharedStatic<ulong> s_Hash = SharedStatic<ulong>.GetOrCreate<BlobTypeHashContext, T>();

        internal static ulong Get()
        {
            if (s_Hash.Data == 0)
                Compute();
            return s_Hash.Data;
        }

        [BurstDiscard]
        static void Compute()
        {
            s_Hash.Data = BlobObject.ComputeTypeHash(typeof(T).FullName);
        }
    }
}
