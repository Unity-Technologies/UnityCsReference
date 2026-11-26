// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Entities;

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// Reference to immutable blob data, that is guaranteed to be alive until the reference is disposed.
    /// </summary>
    [NativeHeader("Modules/Animation/BlobObject/BlobObject.h")]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
    internal unsafe struct BlobObjectReference : IDisposable
    {
        ulong sharedBlobData;
        ulong blobData;
        ulong blobTypeHash;
        EntityId objectId;
        uint  blobSize;

        public void Dispose()
        {
            Release();
        }

        internal bool IsCreated() => blobData != 0;

        internal ulong BlobTypeHash
        {
            [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
            get => blobTypeHash;
        }

        internal byte* BlobData
        {
            [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
            get => (byte*)blobData;
        }

        internal uint BlobSize => blobSize;

        internal EntityId ObjectId
        {
            [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
            get => objectId;
        }

        [NativeMethod(IsThreadSafe = true)]
        internal extern BlobObjectReference Clone();

        [NativeMethod(IsThreadSafe = true)]
        internal extern void Release();

        // Copies the blob whose root is at ptr into new data that no object owns
        [FreeFunction("BlobObjectReference::CreateOrphan", IsThreadSafe = true)]
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal static extern BlobObjectReference CreateOrphan(ulong typeHash, void* ptr, uint size, int version);
    }

    /// <summary>
    /// A Unity object that stores a blob of contiguous bytes that jobs can read without copying the data.
    /// </summary>
    /// <remarks>
    /// Jobs that the Burst compiler compiles can read the blob data in parallel through a
    /// <see cref="BlobObjectReference{T}"/>. Reading the data doesn't copy it, so reading is cheap enough for
    /// performance-critical code.
    ///
    /// A `BlobObject` is a <see cref="UnityEngine.Object"/>, so it also benefits from the object management that Unity
    /// provides. The management includes garbage collection, deduplication of the `BlobObject` instances that the same
    /// <see cref="MonoBehaviour"/> references, and authoring in the Unity Editor.
    ///
    /// To store data, call <see cref="SetBlobAsset{T}(BlobAssetReference{T}, int)"/> with a
    /// <see cref="BlobAssetReference{T}"/>. To read the data, call <see cref="AcquireReference{T}"/> to get a
    /// reference, read the data through <see cref="BlobObjectReference{T}.Value"/>, and dispose the reference when you
    /// finish with it.
    ///
    /// The following example uses the `Unity.Animations`, `Unity.Collections`, `Unity.Entities`, and `UnityEngine`
    /// namespaces. It stores a custom blob in a `BlobObject`, reads the data back through a `BlobObjectReference`, and
    /// then disposes the reference and destroys the object.
    /// </remarks>
    /// <example nocheck="true">
    /// <code lang="cs"><![CDATA[{code Modules/AnimationCore/Tests/UTFTests/Playmode/DocumentationExamples/BlobObjectExample.cs#BlobObjectExample}]]></code>
    /// </example>
    /// <seealso cref="BlobObjectReference{T}"/>
    [NativeHeader("Modules/Animation/BlobObject/BlobObject.h")]
    [NativeClass("BlobObject", PersistentTypeId = 0x2900FE3A)]
    [UsedByNativeCode]
    public class BlobObject : Object
    {
        internal BlobObject(global::UnityEngine.EntityId id) : base(id) {}

        /// <summary>
        /// Creates an empty BlobObject.
        /// </summary>
        /// <remarks>
        /// The new object holds no blob data.
        /// </remarks>
        public BlobObject()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        extern private static EntityId  Internal_Create();

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        // Not thread safe: the data is replaced by the main thread
        [NativeMethod(IsThreadSafe = false)]
        extern internal unsafe void*    GetBlobData(out ulong typeHash, out uint size);

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        [NativeMethod(IsThreadSafe = false)]
        extern internal int             GetBlobVersion();

        // Prepends a BlobAssetHeader to the payload
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        [NativeMethod(IsThreadSafe = false)]
        extern internal unsafe void     SetBlobDataAndHeader(ulong typeHash, void* ptr, uint size, int version);

        [NativeMethod(IsThreadSafe = false)]
        extern internal void            ResetBlobData();

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        [NativeMethod(IsThreadSafe = false)]
        extern internal void            SetNestedReferenceValue(ref BlobObjectReference blobObjectReference, Object target);

        [NativeMethod(IsThreadSafe = false)]
        extern internal void            ReinitializeNestedReferences();

        // entityId must be in the blob data. It is rewritten with the EntityId of the same object when the data is read.
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        [NativeMethod(IsThreadSafe = false)]
        extern internal void            RegisterEntityReference(ref EntityId entityId);

        // Same as RegisterEntityReference for contiguous EntityIds, in a single pass. entityIds must be in the blob data.
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal unsafe void RegisterEntityReferences(Span<EntityId> entityIds)
        {
            fixed (EntityId* entityIdsPtr = entityIds)
                RegisterEntityReferences(entityIdsPtr, entityIds.Length);
        }

        [NativeMethod(IsThreadSafe = false)]
        extern unsafe void              RegisterEntityReferences(EntityId* entityIds, int count);

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        [NativeMethod(IsThreadSafe = false)]
        extern internal BlobObjectReference      AcquireReference();

        // Size of the Unity.Entities BlobAssetHeader stored before the root of typed blobs
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal const int k_BlobAssetHeaderSize = 32;

        // FNV-1a over the UTF-16 code units of the name, as Unity.Burst.BurstRuntime.GetHashCode64 does for
        // assembly-qualified names. AnimationClip::kMuscleClipBlobTypeHash is this hash of "Unity.Animations.Clip".
        internal static ulong ComputeTypeHash(string typeFullName)
        {
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            var hash = offsetBasis;
            foreach (var c in typeFullName)
            {
                hash = prime * (hash ^ (byte)(c & 255));
                hash = prime * (hash ^ (byte)(c >> 8));
            }
            return hash;
        }

        /// <summary>
        /// Acquires a reference that keeps the blob data stored in this object alive, even if the object is destroyed
        /// or its data is replaced.
        /// </summary>
        /// <remarks>
        /// Use the reference to read the blob from jobs and from code that the Burst compiler compiles. This code can't
        /// access this object. Read the data through <see cref="BlobObjectReference{T}.Value"/>. The data stays valid
        /// until you dispose the reference.
        ///
        /// Call this method from the main thread. The method doesn't throw when the object is empty or stores another
        /// version of the blob. Check <see cref="BlobObjectReference{T}.IsValid"/> before you use the reference.
        /// </remarks>
        /// <param name="version">The version of the layout of <typeparamref name="T"/> that the stored data must have.
        /// The default value is 0. If the stored data has another version, the reference is invalid.</param>
        /// <typeparam name="T">The type of the root of the blob. The type must be unmanaged.</typeparam>
        /// <returns>The new reference. You must dispose it. The reference is invalid if the object is empty or stores
        /// another version of the blob.</returns>
        /// <exception cref="InvalidCastException">Thrown if safety checks are enabled and the object stores a blob of
        /// another type. Without safety checks, the reference is invalid.</exception>
        public BlobObjectReference<T> AcquireReference<T>(int version = 0) where T : unmanaged
        {
            if (!IsBlobCompatible<T>(version))
                return default;

            return new BlobObjectReference<T>(AcquireReference());
        }

        // Whether the blob can be read as a T of the given version. Empty blobs are never compatible.
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal bool IsBlobCompatible<T>(int version) where T : unmanaged => IsBlobCompatible<T>(version, true);

        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal unsafe bool IsBlobCompatible<T>(int version, bool throwOnTypeMismatch) where T : unmanaged
        {
            var ptr = GetBlobData(out var typeHash, out var size);
            return IsBlobCompatible<T>(this, ptr, typeHash, size, GetBlobVersion(), version, throwOnTypeMismatch);
        }

        // The check of the objects that hold a blob, such as BlobObject, AnimationClip and Avatar: whether the blob they
        // returned can be read as a T of the given version. Empty blobs are never compatible.
        internal static unsafe bool IsBlobCompatible<T>(Object owner, void* blobData, ulong typeHash, uint size, int blobVersion, int version, bool throwOnTypeMismatch) where T : unmanaged
        {
            if (blobData == null || size <= k_BlobAssetHeaderSize)
                return false;

            if (typeHash != BlobObjectReference<T>.TypeHash)
            {
                if (throwOnTypeMismatch)
                    ThrowIncompatibleType<T>(owner);
                return false;
            }

            return blobVersion == version;
        }

        /// <summary>
        /// Gets the data stored in this object without checking its type or version, for example to migrate data
        /// written with an older layout.
        /// </summary>
        /// <remarks>
        /// The span is a view of the stored data, not a copy. The span becomes invalid when the data of the object is
        /// replaced or the object is destroyed. Copy the data if you need to keep it. Call this method from the main
        /// thread.
        /// </remarks>
        /// <param name="data">The root of the blob, without its header.</param>
        /// <param name="typeHash">The type hash of the stored data. Refer to
        /// <see cref="BlobObjectReference{T}.TypeHash"/>.</param>
        /// <param name="version">The version of the stored data. The value is 0 if the object is empty.</param>
        /// <returns>Returns `true` if the object stores data. Otherwise, returns `false`. In that case,
        /// <paramref name="data"/> is empty, and <paramref name="typeHash"/> and <paramref name="version"/> are
        /// 0.</returns>
        public unsafe bool TryGetUntypedBlobData(out ReadOnlySpan<byte> data, out ulong typeHash, out int version)
        {
            data = default;
            version = 0;

            var ptr = (byte*)GetBlobData(out typeHash, out var size);
            if (ptr == null || size <= k_BlobAssetHeaderSize)
            {
                typeHash = 0;
                return false;
            }

            data = new ReadOnlySpan<byte>(ptr + k_BlobAssetHeaderSize, (int)size - k_BlobAssetHeaderSize);
            version = GetBlobVersion();
            return true;
        }

        /// <summary>
        /// Replaces the data stored in this object with a copy of the given data, type hash, and version.
        /// </summary>
        /// <remarks>
        /// <paramref name="data"/> must be the root of a blob, without its header, as returned by
        /// <see cref="TryGetUntypedBlobData(out ReadOnlySpan{byte}, out ulong, out int)"/>. References that you
        /// acquired earlier keep pointing to the previous data until you dispose them. Call this method from the
        /// main thread.
        /// </remarks>
        /// <param name="data">The data to copy. If it is empty, the object becomes empty.</param>
        /// <param name="typeHash">The type hash of the data. Refer to
        /// <see cref="BlobObjectReference{T}.TypeHash"/>.</param>
        /// <param name="version">The version to store with the data. A typed access, such as
        /// <see cref="AcquireReference{T}(int)"/>, succeeds only if you pass the same version.</param>
        public unsafe void SetUntypedBlobData(ReadOnlySpan<byte> data, ulong typeHash, int version)
        {
            fixed (byte* ptr = data)
            {
                SetBlobDataAndHeader(typeHash, ptr, (uint)data.Length, version);
            }
        }

        /// <summary>
        /// Replaces the data stored in this object with a copy of a blob asset.
        /// </summary>
        /// <remarks>
        /// The object stores a copy of the blob, so you can dispose <paramref name="blobAssetReference"/> after the
        /// call. The object also stores the type of <typeparamref name="T"/> and the version, and
        /// <see cref="AcquireReference{T}(int)"/> returns a valid reference only for the same type and version.
        ///
        /// Call this method from the main thread. References that you acquired earlier keep pointing to the previous
        /// data until you dispose them.
        /// </remarks>
        /// <param name="blobAssetReference">The blob asset to copy. If its `IsCreated` property is `false`, the object
        /// becomes empty.</param>
        /// <param name="version">The version of the layout of <typeparamref name="T"/>. To read the data back, pass the
        /// same version to `AcquireReference`. The default value is 0.</param>
        /// <typeparam name="T">The type of the root of the blob. The type must be unmanaged.</typeparam>
        public unsafe void SetBlobAsset<T>(BlobAssetReference<T> blobAssetReference, int version = 0) where T : unmanaged
        {
            var typeHash = BlobObjectReference<T>.TypeHash;
            if (blobAssetReference.IsCreated)
            {
                SetBlobDataAndHeader(typeHash,
                    blobAssetReference.GetUnsafePtr(),
                    (uint)blobAssetReference.m_data.Header->Length,
                    version);
            }
            else
            {
                SetBlobDataAndHeader(typeHash, null, 0, version);
            }
        }

        // Points into the live blob data; the pointer is invalidated when the data is replaced or unshared
        [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
        internal unsafe T* GetDataUnsafe<T>(int version = 0) where T : unmanaged
        {
            if (!IsBlobCompatible<T>(version))
                return null;

            return (T*)((byte*)GetBlobData(out _, out _) + k_BlobAssetHeaderSize);
        }

        // target must be a BlobObject, an AnimationClip or an Avatar
        internal void SetNestedReferenceValue(ref NestedBlobAssetReference nestedReference, Object target)
        {
            SetNestedReferenceValue(ref nestedReference.Reference, target);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowIncompatibleType<T>(Object owner) where T : unmanaged
        {
            throw new InvalidCastException($"{owner.GetType().Name} '{owner.name}' is not compatible with type {typeof(T).Name}");
        }
    }
}
