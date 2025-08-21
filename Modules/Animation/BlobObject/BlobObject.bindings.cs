// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    /// <summary>
    /// Runtime reference to the blob data of a given BlobObject. This struct is unmanaged and can be safely moved in memory
    /// without breaking the reference to the blob data, so it can be safely stored inside an ECS component data.
    /// The blob data referenced by this struct can be accessed from a bursted job in parallel.
    ///
    /// This structure allocates a fixed memory space to store a FixedBlobObjectReference, which has a performance cost for
    /// both creation and data access (double pointer dereferencing), so it should ideally only be used for root BlobObject
    /// references (other references should be nested inside the blob data
    /// itself).
    /// </summary>
    internal unsafe struct BlobObjectReference : IDisposable
    {
        public BlobObjectReference(BlobObject blobObject, Allocator allocator)
        {
            m_allocator = allocator;

            if (blobObject == null)
            {
                m_fixedReference = null;
                return;
            }

            // will throw if not on main thread
            FixedBlobObjectReference* rootReference = (FixedBlobObjectReference*)blobObject.GetRootReference();

            m_fixedReference = (FixedBlobObjectReference*)UnsafeUtility.Malloc(sizeof(FixedBlobObjectReference),
                UnsafeUtility.AlignOf<FixedBlobObjectReference>(),
                allocator);

            if (m_fixedReference == null)
            {
                Debug.LogError($"Cannot initialize {nameof(BlobObjectReference)}.");
                return;
            }

            m_fixedReference->blobData = (ulong)blobObject.GetBlobData(out m_fixedReference->blobTypeHash, out m_fixedReference->blobSize);
            m_fixedReference->prevReference = (ulong)rootReference;
            m_fixedReference->nextReference = rootReference->nextReference;
            if (m_fixedReference->nextReference != 0)
            {
                ((FixedBlobObjectReference*)m_fixedReference->nextReference)->prevReference = (ulong)m_fixedReference;
            }

            rootReference->nextReference = (ulong)m_fixedReference;
        }

        public void Dispose()
        {
            if (m_fixedReference != null)
            {
                m_fixedReference->RemoveFromList();
                UnsafeUtility.Free(m_fixedReference, m_allocator);
                m_fixedReference = null;
            }
        }

        public bool                 IsCreated => m_fixedReference != null;

        public ulong                BlobTypeHash => IsCreated ? m_fixedReference->blobTypeHash : 0;

        public byte*                BlobData => IsCreated ? (byte*)m_fixedReference->blobData : null;

        public uint                 BlobSize => IsCreated ? m_fixedReference->blobSize : 0;

        Allocator                   m_allocator;
        FixedBlobObjectReference*   m_fixedReference;
    }

    /// <summary>
    /// Reference storing the direct pointer to the blob data of a given BlobObject. This reference must be fixed
    /// at the same memory location for all its lifetime, since other fixed references can have a pointer to it to.
    /// As a consequence, this struct cannot be stored inside an ECS component since these components can move in
    /// memory.
    ///
    /// The FixedBlobObjectReferences connected together form the linked list of all references to a given BlobObject.
    /// Every BlobObject keeps track of the linked list of all references to itself. It allows the BlobObject to
    /// update all the references when its blob data changes (typically when it get unloaded, setting all the
    /// references to the null value).
    ///
    /// The reason behind this design is that reading the blob data is a very frequent and performance critical
    /// operation that must be as fast as possible, as opposed to updating the blob reference itself, which is must
    /// less frequent (loading/unloading the BlobObjects). So reading the blob data is made as fast as possible :
    /// dereferencing & casting a data pointer, without going through an expensive operation of fetching the data
    /// from a map (like dereferencing a PPtr<T>, especially with marshalling) or a double pointer.
    ///
    /// Note that FixedBlobObjetReferences are dynamically allocated (which has a performance cost) when used from
    /// a BlobObjectReference, but FixedObjectReferences can also be nested inside other BlobObjects blob data, which
    /// avoid the small memory allocation and double pointer dereferencing.
    /// </summary>
    [NativeHeader("Modules/Animation/BlobObject/BlobObject.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct FixedBlobObjectReference
    {
        public ulong        blobTypeHash;
        public ulong        blobData;
        public uint         blobSize;

        public ulong        prevReference;
        public ulong        nextReference;

        public void RemoveFromList()
        {
            unsafe
            {
                blobData = 0;
                blobSize = 0;

                if (prevReference != 0)
                {
                    ((FixedBlobObjectReference*)prevReference)->nextReference = nextReference;
                }

                if (nextReference != 0)
                {
                    ((FixedBlobObjectReference*)nextReference)->prevReference = prevReference;
                }

                prevReference = nextReference = 0;
            }
        }
    };

    /// <summary>
    /// Unity Object storing a blob of contiguous bytes. The blob data can be retrieved and accessed in parallel bursted jobs through the <see cref="BlobObjectReference"/>
    /// without doing any data copy, making it suited for performance critical operations and compatible with ECS (the blob data can be casted to a BlobAssetReference<>),
    /// while benefiting from Unity Object management (garbage collection, deduplication of BlobObjects referenced by the same MonoBehaviour, authoring in editor...).
    /// </summary>
    [NativeHeader("Modules/Animation/BlobObject/BlobObject.h")]
    [UsedByNativeCode]
    internal class BlobObject : Object
    {
        public BlobObject()
        {
            Internal_Create(this);
        }

        extern private static void      Internal_Create([Writable] BlobObject self);

        [NativeMethod(IsThreadSafe = true)]
        extern internal unsafe void*    GetBlobData(out ulong typeHash, out uint size);

        [NativeMethod(IsThreadSafe = false)]
        extern internal unsafe void     SetBlobData(ulong typeHash, void* ptr, uint size);

        [NativeMethod(IsThreadSafe = false)]
        extern internal unsafe IntPtr   GetRootReference();

        [NativeMethod(IsThreadSafe = false)]
        extern internal void            SetNestedReferenceValue(ref FixedBlobObjectReference blobObjectReference, BlobObject blobObject);

        [NativeMethod(IsThreadSafe = false)]
        extern internal void            ReinitializeNestedReferences();
    }
}
