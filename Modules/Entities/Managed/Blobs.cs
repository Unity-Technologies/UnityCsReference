// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    internal struct RetainBlobAssets : ICleanupComponentData
    {
        // DummyBlobAssetReference will always be null, but will make sure the serializer adds the BlobOwner shared componet
        public BlobAssetReference<byte> DummyBlobAssetReference;
        public int FramesToRetainBlobAssets;
    }

    internal unsafe struct RetainBlobAssetBatchPtr : ICleanupComponentData
    {
        public BlobAssetBatch* BlobAssetBatchPtr;
    }

    internal unsafe struct RetainBlobAssetPtr : ICleanupComponentData
    {
        public BlobAssetHeader* BlobAsset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    [BurstCompile]
    internal unsafe struct BlobAssetOwner : ISharedComponentData, IRefCounted
    {
        [FieldOffset(0)]
        public BlobAssetBatch* BlobAssetBatchPtr;

        public BlobAssetOwner(void* buffer, int expectedTotalDataSize)
        {
            BlobAssetBatchPtr = BlobAssetBatch.CreateFromMemory(buffer, expectedTotalDataSize);
        }

        public bool IsCreated => BlobAssetBatchPtr != null;

        [BurstCompile]
        public void Release()
        {
            if (BlobAssetBatchPtr != null)
                BlobAssetBatch.Release(BlobAssetBatchPtr);
        }
        [BurstCompile]
        public void Retain()
        {
            if (BlobAssetBatchPtr != null)
                BlobAssetBatch.Retain(BlobAssetBatchPtr);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal unsafe struct BlobAssetBatch
    {
        [FieldOffset(0)]
        private int     TotalDataSize;
        [FieldOffset(4)]
        private int     BlobAssetHeaderCount;
        [FieldOffset(8)]
        private int     RefCount;
        [FieldOffset(12)]
        private int     Padding;

        internal static BlobAssetBatch CreateForSerialize(int blobAssetCount, int totalDataSize)
        {
            return new BlobAssetBatch
            {
                BlobAssetHeaderCount = blobAssetCount,
                TotalDataSize = totalDataSize,
                RefCount = 1,
                Padding = 0
            };
        }

        internal static BlobAssetBatch* CreateFromMemory(void* buffer, int expectedTotalDataSize)
        {
            var batch = (BlobAssetBatch*)buffer;

            if (batch->TotalDataSize != expectedTotalDataSize)
                throw new System.ArgumentException($"TotalSize '{batch->TotalDataSize}' and expected Total size '{expectedTotalDataSize}' are out of sync");
            if (batch->RefCount != 1)
                throw new System.ArgumentException("BlobAssetBatch.Refcount must be 1 on deserialized");

            var header = (BlobAssetHeader*)(batch + 1);
            for (int i = 0; i != batch->BlobAssetHeaderCount; i++)
            {
                header->ValidationPtr = header + 1;
                if (header->Allocator.ToAllocator != Allocator.None)
                    throw new System.ArgumentException("Blob Allocator should be Allocator.None");
                header = (BlobAssetHeader*)(((byte*) (header+1)) + header->Length);
            }
            header--;

            if (header == (byte*) batch + batch->TotalDataSize)
                throw new System.ArgumentException("");

            return batch;
        }

        public static void Retain(BlobAssetBatch* batch)
        {
            Interlocked.Increment(ref batch->RefCount);
        }

        public static void Release(BlobAssetBatch* batch)
        {
            int newRefCount = Interlocked.Decrement(ref batch->RefCount);
            if (newRefCount <= 0)
            {
                // Debug.Log("Freeing blob");

                if (newRefCount < 0)
                    throw new InvalidOperationException("BlobAssetBatch refcount is less than zero. It has been corrupted.");

                if (batch->TotalDataSize == 0)
                    throw new InvalidOperationException("BlobAssetBatch has been corrupted. Likely it has already been unloaded or released.");

                var header = (BlobAssetHeader*)(batch + 1);
                for (int i = 0; i != batch->BlobAssetHeaderCount; i++)
                {
                    if (header->ValidationPtr != (header + 1))
                        throw new InvalidOperationException("The BlobAssetReference has been corrupted. Likely it has already been unloaded or released.");

                    header->Invalidate();
                    header = (BlobAssetHeader*)(((byte*) (header+1)) + header->Length);
                }
                header--;

                if (header == (byte*) batch + batch->TotalDataSize)
                    throw new InvalidOperationException("BlobAssetBatch has been corrupted. Likely it has already been unloaded or released.");

                batch->TotalDataSize = 0;
                batch->BlobAssetHeaderCount = 0;

                Memory.Unmanaged.Free(batch, Allocator.Persistent);
            }
        }
    }

    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    readonly unsafe struct BlobAssetPtr : IEquatable<BlobAssetPtr>
    {
        public readonly BlobAssetHeader* Header;
        public void* Data => Header + 1;
        public int Length
        {
            get
            {
                Validate();
                return Header->Length;
            }
        }

        public ulong Hash
        {
            get
            {
                Validate();
                return Header->Hash;
            }
        }

        public BlobAssetPtr(BlobAssetHeader* header)
            => Header = header;

        public bool Equals(BlobAssetPtr other)
            => Header == other.Header;

        public override int GetHashCode()
        {
            BlobAssetHeader* onStack = Header;
            return (int)math.hash(&onStack, sizeof(BlobAssetHeader*));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void Validate()
        {
            new BlobAssetReferenceData{ m_Ptr = (byte*)(Header + 1) }.ValidateNotNull();
        }
    }

    // TODO: For now the size of BlobAssetHeader needs to be multiple of 16 to ensure alignment of blob assets
    // TODO: Add proper alignment support to blob assets
    // TODO: Reduce the size of the header at runtime or remove it completely
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    unsafe struct BlobAssetHeader
    {
        [FieldOffset(0)]  public void* ValidationPtr;
        [FieldOffset(8)]  public int Length;
        [FieldOffset(12)] public AllocatorManager.AllocatorHandle Allocator;
        [FieldOffset(16)] public ulong Hash;
        [FieldOffset(24)] private ulong Padding;

        internal static BlobAssetHeader CreateForSerialize(int length, ulong hash)
        {
            return new BlobAssetHeader
            {
                ValidationPtr = null,
                Length = length,
                Allocator = Unity.Collections.Allocator.None,
                Hash = hash,
                Padding = 0
            };
        }

        public void Invalidate()
        {
            ValidationPtr = (void*)0xdddddddddddddddd;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal unsafe struct BlobAssetReferenceData : IEquatable<BlobAssetReferenceData>
    {
        [NativeDisableUnsafePtrRestriction]
        [FieldOffset(0)]
        public byte* m_Ptr;


        /// <summary>
        /// This field overlaps m_Ptr similar to a C union.
        /// It is an internal (so we can initialize the struct) field which
        /// is here to force the alignment of BlobAssetReferenceData to be 8-bytes.
        /// </summary>
        [FieldOffset(0)]
        internal long m_Align8Union;

        internal BlobAssetHeader* Header
        {
            [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
            get
            {
                ThrowIfNull();
                return (BlobAssetHeader*) m_Ptr - 1;
            }
        }

        [BurstDiscard]
        void ValidateNonBurst()
        {
            void* validationPtr = null;
            try
            {
                // Try to read ValidationPtr, this might throw if the memory has been unmapped
                validationPtr = Header->ValidationPtr;
            }
            catch(Exception)
            {
            }

            if (!(validationPtr == m_Ptr || IsProtected))
            {
                throw new InvalidOperationException("The BlobAssetReference is not valid. Likely it has already been unloaded or released.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void ValidateBurst()
        {
            void* validationPtr = Header->ValidationPtr;
            if (!(validationPtr == m_Ptr || IsProtected))
            {
                throw new InvalidOperationException("The BlobAssetReference is not valid. Likely it has already been unloaded or released.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ValidateNotNull()
        {
            ThrowIfNull();

            ValidateNonBurst();
            ValidateBurst();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void ThrowIfNull()
        {
            if (m_Ptr == null)
                throw new NullReferenceException("The BlobAssetReference is null.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ValidateAllowNull()
        {
            if (m_Ptr == null)
                return;

            ValidateNonBurst();
            ValidateBurst();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void ValidateNotDeserialized()
        {
            if (Header->Allocator.ToAllocator == Allocator.None)
                throw new InvalidOperationException("It's not possible to release a blob asset reference that was deserialized. It will be automatically released when the scene is unloaded ");
            Header->Invalidate();
        }

        public void Dispose()
        {
            ValidateNotNull();
            ValidateNotProtected();
            ValidateNotDeserialized();
            Memory.Unmanaged.Free(Header, Header->Allocator);
            m_Ptr = null;
        }

        public bool Equals(BlobAssetReferenceData other)
        {
            return m_Ptr == other.m_Ptr;
        }

        public override int GetHashCode()
        {
            if (IntPtr.Size == 8)
            {
                int low = (int) m_Ptr;
                int hi  = (int) ((nuint)m_Ptr >> 32);
                return (low * 397) ^ hi;
            }
            else
            {
                return (int) m_Ptr;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void ProtectAgainstDisposal()
        {
            ValidateNotNull();

            if (IsProtected)
            {
                throw new InvalidOperationException("Cannot protect this BlobAssetReference, it is already protected.");
            }

            //Header->ValidationPtr = (void*)~(nuint)m_Ptr; // Required to work around BUR-2548
            //Header->ValidationPtr = (void*)((nuint)0 - (nuint)m_Ptr - 1); // Incorrect result due to BUR-2561
            Header->ValidationPtr = (void*)((ulong)0 - (nuint)m_Ptr - 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void UnprotectAgainstDisposal()
        {
            ValidateNotNull();

            if (!IsProtected)
            {
                throw new InvalidOperationException("Cannot unprotect this BlobAssetReference, it is not protected.");
            }

            Header->ValidationPtr = m_Ptr;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void ValidateNotProtected()
        {
            if (IsProtected)
            {
                throw new InvalidOperationException("This BlobAssetReference is owned by a BlobAssetStore and cannot be disposed directly.");
            }
        }

        bool IsProtected
        {
            get
            {
                ThrowIfNull();

                //return Header->ValidationPtr == (void*)~(nuint)m_Ptr; // Required to work around BUR-2548
                //return Header->ValidationPtr == (void*)((nuint)0 - (nuint)m_Ptr - 1); // Incorrect result due to BUR-2561
                return Header->ValidationPtr == (void*)((ulong)0 - (nuint)m_Ptr - 1);
            }
        }
    }

    /// <summary>
    /// A reference to a blob asset stored in unmanaged memory.
    /// </summary>
    /// <remarks>Create a blob asset using a <see cref="BlobBuilder"/> or by deserializing a serialized blob asset.</remarks>
    /// <typeparam name="T">The struct data type defining the data structure of the blob asset.</typeparam>
    [ChunkSerializable]
    [DebuggerTypeProxy(typeof(DebugProxies.BlobAssetReferenceProxy<>))]
    [DebuggerDisplay(nameof(BlobAssetReference<T>))]
    public unsafe struct BlobAssetReference<T> : IDisposable, IEquatable<BlobAssetReference<T>>
        where T : unmanaged
    {
        [Properties.CreateProperty]
        internal BlobAssetReferenceData m_data;
        /// <summary>
        /// Reports whether this instance references a valid blob asset.
        /// </summary>
        /// <value>True, if this instance references a valid blob instance.</value>
        public bool IsCreated
        {
            get { return m_data.m_Ptr != null; }
        }

        /// <summary>
        /// Provides an unsafe pointer to the blob asset data.
        /// </summary>
        /// <remarks>You can only use unsafe pointers in [unsafe contexts].
        /// [unsafe contexts]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/unsafe-code
        /// </remarks>
        /// <returns>An unsafe pointer. The pointer is null for invalid BlobAssetReference instances.</returns>
        public void* GetUnsafePtr()
        {
            m_data.ValidateAllowNull();
            return m_data.m_Ptr;
        }

        /// <summary>
        /// Destroys the referenced blob asset and frees its memory.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if you attempt to dispose a blob asset that loaded as
        /// part of a scene or subscene.</exception>
        public void Dispose()
        {
            m_data.Dispose();
        }


        /// <summary>
        /// A reference to the blob asset data, a struct of type T that is stored in the blob asset.
        /// </summary>
        /// <remarks>The property is a
        /// <see href="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/ref-returns">
        /// reference return</see>.</remarks>
        /// <value>The root data structure of the blob asset data.</value>
        public ref T Value
        {
            get
            {
                m_data.ValidateNotNull();
                return ref UnsafeUtility.AsRef<T>(m_data.m_Ptr);
            }
        }


        /// <summary>
        /// Creates a blob asset from a pointer to data and a specified size.
        /// </summary>
        /// <remarks>The blob asset is created in unmanaged memory. Call <see cref="Dispose"/> to free the asset memory
        /// when it is no longer needed. This function can only be used in an [unsafe context].
        /// [unsafe context]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/unsafe-code
        /// </remarks>
        /// <param name="ptr">A pointer to the buffer containing the data to store in the blob asset.</param>
        /// <param name="length">The length of the buffer in bytes.</param>
        /// <returns>A reference to newly created blob asset.</returns>
        /// <seealso cref="BlobBuilder"/>
        public static BlobAssetReference<T> Create(void* ptr, int length)
        {
            byte* buffer =
                (byte*) Memory.Unmanaged.Allocate(sizeof(BlobAssetHeader) + length, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader), ptr, length);

            BlobAssetHeader* header = (BlobAssetHeader*) buffer;
            *header = new BlobAssetHeader();

            header->Length = length;
            header->Allocator = Allocator.Persistent;

            // @TODO use 64bit hash
            header->Hash = math.hash(ptr, length);

            BlobAssetReference<T> blobAssetReference;
            blobAssetReference.m_data.m_Align8Union = 0;
            header->ValidationPtr = blobAssetReference.m_data.m_Ptr = buffer + sizeof(BlobAssetHeader);
            return blobAssetReference;
        }

        /// <summary>
        /// Creates a blob asset from a byte array.
        /// </summary>
        /// <remarks>The blob asset is created in unmanaged memory. Call <see cref="Dispose"/> to free the asset memory
        /// when it is no longer needed. This function can only be used in an [unsafe context].
        /// [unsafe context]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/unsafe-code
        /// </remarks>
        /// <param name="data">The byte array containing the data to store in the blob asset.</param>
        /// <returns>A reference to newly created blob asset.</returns>
        /// <seealso cref="BlobBuilder"/>
        public static BlobAssetReference<T> Create(byte[] data)
        {
            fixed (byte* ptr = &data[0])
            {
                return Create(ptr, data.Length);
            }
        }

        /// <summary>
        /// Creates a blob asset from an instance of a struct.
        /// </summary>
        /// <remarks>The struct must only contain blittable fields (primitive types, fixed-length arrays, or other structs
        /// meeting these same criteria). The blob asset is created in unmanaged memory. Call <see cref="Dispose"/> to
        /// free the asset memory when it is no longer needed. This function can only be used in an [unsafe context].
        /// [unsafe context]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/unsafe-code</remarks>
        /// <param name="value">An instance of <typeparamref name="T"/>.</param>
        /// <returns>A reference to newly created blob asset.</returns>
        /// <seealso cref="BlobBuilder"/>
        public static BlobAssetReference<T> Create(T value)
        {
            return Create(UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>());
        }

        /// <summary>
        /// Construct a BlobAssetReference from the blob data
        /// </summary>
        /// <param name="blobData">The blob data to attach to the returned object</param>
        /// <returns>The created BlobAssetReference</returns>
        internal static BlobAssetReference<T> Create(BlobAssetReferenceData blobData)
        {
            return new BlobAssetReference<T> { m_data = blobData };
        }

        /// <summary>
        /// Reads bytes from a binary reader, validates the expected serialized version, and deserializes them into a new blob asset.
        /// </summary>
        /// <param name="binaryReader">The reader for the blob data path</param>
        /// <param name="version">Expected version number of the blob data.</param>
        /// <param name="result">The resulting BlobAssetReference if the data was read successful.</param>
        /// <typeparam name="U">The type of binary reader</typeparam>
        /// <returns>True if the data was successfully read, false otherwise</returns>
        public static bool TryRead<U>(U binaryReader, int version, out BlobAssetReference<T> result)
        where U : BinaryReader
        {
            var storedVersion = binaryReader.ReadInt();
            if (storedVersion != version)
            {
                result = default;
                return false;
            }
            result = binaryReader.Read<T>();
            return true;
        }

        /// <summary>
        /// Reads bytes from a fileName, validates the expected serialized version, and deserializes them into a new blob asset.
        /// </summary>
        /// <param name="path">The path of the blob data to read.</param>
        /// <param name="version">Expected version number of the blob data.</param>
        /// <param name="result">The resulting BlobAssetReference if the data was read successful.</param>
        /// <returns>A bool if the read was successful or not.</returns>
        public static bool TryRead(string path, int version, out BlobAssetReference<T> result)
        {
            if (string.IsNullOrEmpty(path))
            {
                result = default;
                return false;
            }
            using (var binaryReader = new StreamBinaryReader(path, UnsafeUtility.SizeOf<T>() + sizeof(int)))
            {
                return TryRead(binaryReader, version, out result);
            }
        }

        /// <summary>
        /// Reads bytes from a buffer, validates the expected serialized version, and deserializes them into a new blob asset.
        /// The returned blob reuses the data from the passed in pointer and is only valid as long as the buffer stays allocated.
        /// Also the returned blob asset reference can not be disposed.
        /// </summary>
        /// <param name="data">A pointer to the buffer containing the serialized blob data.</param>
        /// <param name="size">Size in bytes of the buffer containing the serialized blob data.</param>
        /// <param name="version">Expected version number of the blob data.</param>
        /// <param name="result">The resulting BlobAssetReference if the data was read successful.</param>
        /// <param name="numBytesRead">Number of bytes of the data buffer that are read.</param>
        /// <returns>A bool if the read was successful or not.</returns>
        internal static unsafe bool TryReadInplace(byte* data, long size, int version, out BlobAssetReference<T> result, out int numBytesRead)
        {
            result = default;
            numBytesRead = 0;

            if (size < sizeof(int) + sizeof(BlobAssetHeader))
                return false;

            var storedVersion = *(int*)data;
            if (storedVersion != version)
                return false;

            ref var header = ref *((BlobAssetHeader*)(data + sizeof(int)));

            if (size < sizeof(int) + sizeof(BlobAssetHeader) + header.Length)
                return false;

            numBytesRead = sizeof(int) + sizeof(BlobAssetHeader) + header.Length;

            var buffer = data + sizeof(int);
            header.ValidationPtr = buffer + sizeof(BlobAssetHeader);

            BlobAssetReference<T> blobAssetReference;
            blobAssetReference.m_data.m_Align8Union = 0;
            blobAssetReference.m_data.m_Ptr = buffer + sizeof(BlobAssetHeader);
            result = blobAssetReference;
            return true;
        }

        /// <summary>
        /// Writes the blob data to a path with serialized version.
        /// </summary>
        /// <param name="writer">The binary writer to write the blob with.</param>
        /// <param name="builder">The BlobBuilder containing the blob to write.</param>
        /// <param name="version">Serialized version number of the blob data.</param>
        /// <typeparam name="U">The type of binary writer</typeparam>
        public static void Write<U>(U writer, BlobBuilder builder, int version)
        where U : BinaryWriter
        {
            using (var asset = builder.CreateBlobAssetReference<T>(Allocator.TempJob))
            {
                writer.ImHexWrite<int>("BlobVersion");
                writer.Write(version);
                writer.ImHexWrite<BlobAssetReference<T>>("Asset");
                writer.Write(asset);
            }
        }

        /// <summary>
        /// Writes the blob data to a path with serialized version.
        /// </summary>
        /// <param name="builder">The BlobBuilder containing the blob to write.</param>
        /// <param name="path">The path to write the blob data.</param>
        /// <param name="version">Serialized version number of the blob data.</param>
        public static void Write(BlobBuilder builder, string path, int version)
        {
            using (var writer = new StreamBinaryWriter(path))
            {
                Write(writer, builder, version);
            }
        }

        /// <summary>
        /// A "null" blob asset reference that can be used to test if a BlobAssetReference instance
        /// </summary>
        public static BlobAssetReference<T> Null => new BlobAssetReference<T>();

        /// <summary>
        /// Two BlobAssetReferences are equal when they reference the same data.
        /// </summary>
        /// <param name="lhs">The BlobAssetReference on the left side of the operator.</param>
        /// <param name="rhs">The BlobAssetReference on the right side of the operator.</param>
        /// <returns>True, if both references point to the same data or if both are <see cref="Null"/>.</returns>
        public static bool operator ==(BlobAssetReference<T> lhs, BlobAssetReference<T> rhs)
        {
            return lhs.m_data.m_Ptr == rhs.m_data.m_Ptr;
        }

        /// <summary>
        /// Two BlobAssetReferences are not equal unless they reference the same data.
        /// </summary>
        /// <param name="lhs">The BlobAssetReference on the left side of the operator.</param>
        /// <param name="rhs">The BlobAssetReference on the right side of the operator.</param>
        /// <returns>True, if the references point to different data in memory or if one is <see cref="Null"/>.</returns>
        public static bool operator !=(BlobAssetReference<T> lhs, BlobAssetReference<T> rhs)
        {
            return lhs.m_data.m_Ptr != rhs.m_data.m_Ptr;
        }

        /// <summary>
        /// Two BlobAssetReferences are equal when they reference the same data.
        /// </summary>
        /// <param name="other">The reference to compare to this one.</param>
        /// <returns>True, if both references point to the same data or if both are <see cref="Null"/>.</returns>
        public bool Equals(BlobAssetReference<T> other)
        {
            return m_data.Equals(other.m_data);
        }

        /// <summary>
        /// Two BlobAssetReferences are equal when they reference the same data.
        /// </summary>
        /// <param name="obj">The object to compare to this reference</param>
        /// <returns>True, if the object is a BlobAssetReference instance that references to the same data as this one,
        /// or if both objects are <see cref="Null"/> BlobAssetReference instances.</returns>
        public override bool Equals(object obj)
        {
            return this == (BlobAssetReference<T>)obj;
        }

        /// <summary>
        /// Generates the hash code for this object.
        /// </summary>
        /// <returns>A standard C# value-type hash code.</returns>
        public override int GetHashCode()
        {
            return m_data.GetHashCode();
        }

        internal BlobAssetPtr ToBlobAssetPtr() => new BlobAssetPtr(m_data.Header);
    }

    /// <summary>
    /// A pointer referencing a struct, array, or field inside a blob asset.
    /// </summary>
    /// <typeparam name="T">The data type of the referenced object.</typeparam>
    /// <seealso cref="BlobBuilder"/>
    [MayOnlyLiveInBlobStorage]
    [DebuggerDisplay("Cannot display the value of a " + nameof(BlobPtr<T>) + " by itself. Please inspect the root BlobAssetReference instead.")]
    public unsafe struct BlobPtr<T> where T : struct
    {
        internal int m_OffsetPtr;

        /// <summary>
        /// Returns 'true' if this is a valid pointer (not null)
        /// </summary>
        public bool IsValid => m_OffsetPtr != 0;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void AssertIsValid()
        {
            if (!IsValid)
                throw new System.InvalidOperationException("The accessed BlobPtr hasn't been allocated.");
        }

        /// <summary>
        /// The value, of type <typeparamref name="T"/> to which the pointer refers.
        /// </summary>
        /// <remarks>The property is a
        /// <see href="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/ref-returns">
        /// reference return</see>.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if the pointer does not reference a valid instance of
        /// a data type.</exception>
        public ref T Value
        {
            get
            {
                AssertIsValid();
                fixed (int* thisPtr = &m_OffsetPtr)
                {
                    return ref UnsafeUtility.AsRef<T>((byte*) thisPtr + m_OffsetPtr);
                }
            }
        }

        /// <summary>
        /// Provides an unsafe pointer to the referenced data.
        /// </summary>
        /// <remarks>You can only use unsafe pointers in [unsafe contexts].
        /// [unsafe contexts]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/unsafe-code
        /// </remarks>
        /// <returns>An unsafe pointer.</returns>
        public void* GetUnsafePtr()
        {
            if (m_OffsetPtr == 0)
                return null;

            fixed (int* thisPtr = &m_OffsetPtr)
            {
                return (byte*) thisPtr + m_OffsetPtr;
            }
        }
    }

    /// <summary>
    ///  An immutable array of value types stored in a blob asset.
    /// </summary>
    /// <remarks>When creating a blob asset, use the <see cref="BlobBuilderArray{T}"/> provided by a
    /// <see cref="BlobBuilder"/> instance to set the array elements.</remarks>
    /// <typeparam name="T">The data type of the elements in the array. Must be a struct or other value type.</typeparam>
    /// <seealso cref="BlobBuilder"/>
    [MayOnlyLiveInBlobStorage]
    [DebuggerDisplay("Cannot display the value of a " + nameof(BlobArray<T>) + " by itself. Please inspect the root BlobAssetReference instead.")]
    public unsafe struct BlobArray<T> where T : struct
    {
        internal int m_OffsetPtr;
        internal int m_Length;

        /// <summary>
        /// The number of elements in the array.
        /// </summary>
        /// <remarks>
        /// Unity assigns the length of the array when you call <see cref="BlobBuilder.CreateBlobAssetReference{T}"/>, so until
        /// then this property returns 0. To get the number of elements before that point, use the
        /// <see cref="BlobBuilderArray{T}.Length"/> property of the <see cref="BlobBuilderArray{T}"/> that
        /// <see cref="BlobBuilder.Allocate{T}(ref BlobArray{T}, int)"/> returns.
        /// </remarks>
        public int Length
        {
            get { return m_Length; }
        }

        /// <summary>
        /// Provides an unsafe pointer to the array data.
        /// </summary>
        /// <remarks>You can only use unsafe pointers in [unsafe contexts].
        /// [unsafe contexts]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/unsafe-code
        /// </remarks>
        /// <remarks>
        /// Don't call this method before you call <see cref="BlobBuilder.CreateBlobAssetReference{T}"/>. Unity assigns the array's
        /// internal offset at that point, so until then this method returns a pointer to the <see cref="BlobArray{T}"/> field
        /// itself instead of to the array data, and the <see cref="Length"/> property returns 0. This method performs no
        /// validation and reports no error in this case.
        ///
        /// To get a pointer to the array data before that point, use the <see cref="BlobBuilderArray{T}.GetUnsafePtr"/> method of
        /// the <see cref="BlobBuilderArray{T}"/> that <see cref="BlobBuilder.Allocate{T}(ref BlobArray{T}, int)"/> returns.
        /// </remarks>
        /// <returns>An unsafe pointer.</returns>
        public void* GetUnsafePtr()
        {
            // for an unallocated array this will return an invalid pointer which is ok since it
            // should never be accessed as Length will be 0
            fixed (int* thisPtr = &m_OffsetPtr)
            {
                return (byte*) thisPtr + m_OffsetPtr;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void AssertIndexInRange(int index)
        {
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}",
                    index, m_Length));
        }

        /// <summary>
        /// The element of the array at the <paramref name="index"/> position.
        /// </summary>
        /// <param name="index">The array index.</param>
        /// <remarks>The array element is a
        /// <see href="https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/ref-returns">
        /// reference return</see>.</remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is out of bounds.</exception>
        public ref T this[int index]
        {
            get
            {
                AssertIndexInRange(index);

                fixed (int* thisPtr = &m_OffsetPtr)
                {
                    return ref UnsafeUtility.ArrayElementAsRef<T>((byte*) thisPtr + m_OffsetPtr, index);
                }
            }
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowOnCopyOfBlobArrayFields(Type t)
        {
            ThrowOnCopyOfBlobArrayFieldsRecursive(t, t);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowOnCopyOfBlobArrayFieldsRecursive(Type t, Type rootType)
        {
            if (t.IsPointer || t.IsPrimitive || t.IsEnum || t == typeof(string))
                return;

            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() == typeof(BlobArray<>))
                    throw new InvalidOperationException($"Type {rootType.Name} cannot be copied because it contains nested fields of type BlobArray<>.");
                if (t.GetGenericTypeDefinition() == typeof(BlobPtr<>))
                    throw new InvalidOperationException($"Type {rootType.Name} cannot be copied because it contains nested fields of type BlobPtr<>.");
            }

            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0, count = fields?.Length ?? 0; i < count; ++i)
            {
                var field = fields[i];
                ThrowOnCopyOfBlobArrayFieldsRecursive(field.FieldType, rootType);
            }
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{T}"/> view of the blob array data.
        /// </summary>
        /// <returns>A read-only span over the array elements.</returns>
        public unsafe ReadOnlySpan<T> AsSpan()
        {
            return new ReadOnlySpan<T>(GetUnsafePtr(), m_Length);
        }

        /// <summary>
        /// Copies the elements of this BlobArray to a new managed array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the BlobArray.</returns>
        /// <exception cref="InvalidOperationException">Throws InvalidOperationException if the array type contains
        /// nested <see cref="BlobArray{T}"/>, <see cref="BlobString"/> or <see cref="BlobPtr{T}"/> fields.</exception>
        public T[] ToArray()
        {
            ThrowOnCopyOfBlobArrayFields(typeof(T));

            var result = new T[m_Length];
            if (m_Length > 0)
            {
                var src = GetUnsafePtr();

                var handle = GCHandle.Alloc(result, GCHandleType.Pinned);
                var addr = handle.AddrOfPinnedObject();

                UnsafeUtility.MemCpy((void*)addr, src, m_Length * UnsafeUtility.SizeOf<T>());

                handle.Free();
            }
            return result;
        }
    }

    /// <summary>
    /// Use this attribute if you have structs that use offset pointers that are only valid when they live inside the blob storage.
    /// It will turn ensure a compiler error is generated for every time a reference to the struct is copied, or a field is read
    /// from a reference to the struct this attribute is applied on.
    /// </summary>
    public class MayOnlyLiveInBlobStorageAttribute : Attribute
    {
    }

    /// <summary>
    /// An immutable, variable-length string stored in a blob asset.
    /// </summary>
    /// <seealso cref="BlobBuilder"/>
    [MayOnlyLiveInBlobStorage]
    [DebuggerDisplay("Cannot display the value of a " + nameof(BlobString) + " by itself. Please inspect the root BlobAssetReference instead.")]
    public unsafe struct BlobString
    {
        internal BlobArray<byte> Data;
        /// <summary>
        /// The length of the string in UTF-8 bytes.
        /// </summary>
        public int Length
        {
            get { return math.max(0,Data.Length-1); } // it's null-terminated, but we don't count the trailing null.
        }

        /// <summary>
        /// Converts this BlobString to a standard C# <see cref="string"/>.
        /// </summary>
        /// <returns>The C# string.</returns>
        public new string ToString() => ToString((byte*)Data.GetUnsafePtr(), Length);

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{Byte}"/> view of the UTF-8 encoded string data,
        /// excluding the null terminator.
        /// </summary>
        /// <returns>A read-only span over the UTF-8 bytes.</returns>
        public ReadOnlySpan<byte> AsSpan() => new ReadOnlySpan<byte>(Data.GetUnsafePtr(), Length);

        internal static string ToString(byte* data, int lengthInBytes)
        {
            var utf16Capacity = math.max(1, lengthInBytes * 2);
            var c = stackalloc char[utf16Capacity];
            Unicode.Utf8ToUtf16(data, lengthInBytes, c, out var utf16Length, utf16Capacity);
            return new String(c, 0, utf16Length);
        }

        /// <summary>
        /// Copies the characters from a BlobString to a native container
        /// </summary>
        /// <param name="dest">The destination BlobString.</param>
        /// <typeparam name="T">The type of native container to copy to</typeparam>
        /// <returns>None if the copy fully completes. Otherwise, returns Overflow.</returns>
        public ConversionError CopyTo<T>(ref T dest) where T : INativeList<byte>
        {
            byte* srcBuffer =(byte*)Data.GetUnsafePtr();
            int srcLength = Length;
            dest.Length = srcLength;
            byte* destBuffer = (byte*)UnsafeUtility.AddressOf(ref dest.ElementAt(0));
            int destCapacity = dest.Capacity;
            var err = Unicode.Utf8ToUtf8(srcBuffer, srcLength, destBuffer, out var destLength, destCapacity);
            dest.Length = destLength;
            return err;
        }
    }

    /// <summary>
    /// Extensions that allow the creation of <see cref="BlobString"/> instances by a <see cref="BlobBuilder"/>.
    /// </summary>
    public static class BlobStringExtensions
    {
        /// <summary>
        /// Allocates memory to store the string in a blob asset and copies the string data into it.
        /// </summary>
        /// <param name="builder">The BlobBuilder instance building the blob asset.</param>
        /// <param name="blobStr">A reference to the field in the blob asset that will store the string. This
        /// function allocates memory for that field and sets the string value.</param>
        /// <param name="value">The string to copy into the blob asset.</param>
        public static unsafe void AllocateString(ref this BlobBuilder builder, ref BlobString blobStr, string value)
        {
            fixed (char* c = value)
            {
                var utf8Capacity = value.Length * 2 + 1;
                byte* b = (byte*)UnsafeUtility.Malloc(utf8Capacity, 1, Allocator.Temp);
                Unicode.Utf16ToUtf8(c, value.Length, b, out int utf8Length, utf8Capacity);
                b[utf8Length] = 0;
                var res = builder.Allocate(ref blobStr.Data, utf8Length + 1);
                UnsafeUtility.MemCpy(res.GetUnsafePtr(), b, utf8Length + 1);
            }
        }

        /// <summary>
        /// Allocates memory to store the string in a blob asset and copies the string data into it.
        /// </summary>
        /// <param name="builder">The BlobBuilder instance building the blob asset.</param>
        /// <param name="blobStr">A reference to the field in the blob asset that will store the string. This
        /// function allocates memory for that field and sets the string value.</param>
        /// <param name="value">The string to copy into the blob asset.</param>
        /// <typeparam name="T">The type of native container that contains the source string</typeparam>
        public static unsafe void AllocateString<T>(ref this BlobBuilder builder, ref BlobString blobStr, ref T value) where T : INativeList<byte>
        {
            var utf8Length = value.Length;
            byte* utf8Bytes = (byte*)UnsafeUtility.AddressOf(ref value.ElementAt(0));
            var res = builder.Allocate(ref blobStr.Data, utf8Length + 1);
            UnsafeUtility.MemCpy(res.GetUnsafePtr(), utf8Bytes, utf8Length + 1);
        }
    }

    /// <summary>
    /// Extensions for supporting serialization and deserialization of blob assets.
    /// </summary>
    public static class BlobAssetSerializeExtensions
    {
        /// <summary>
        /// Serializes the blob asset data and writes the bytes to a <see cref="BinaryWriter"/> instance.
        /// </summary>
        /// <param name="binaryWriter">An implementation of the BinaryWriter interface.</param>
        /// <param name="blob">A reference to the blob asset to serialize.</param>
        /// <typeparam name="T">The blob asset's root data type.</typeparam>
        /// <seealso cref="StreamBinaryWriter"/>
        /// <seealso cref="MemoryBinaryWriter"/>
        public static unsafe void Write<T>(this BinaryWriter binaryWriter, BlobAssetReference<T> blob) where T : unmanaged
        {
            var blobAssetLength = blob.m_data.Header->Length;
            var serializeReadyHeader = BlobAssetHeader.CreateForSerialize(blobAssetLength, blob.m_data.Header->Hash);

            binaryWriter.ImHexWrite<BlobAssetHeader>("serializeReadyHeader");
            binaryWriter.WriteBytes(&serializeReadyHeader, sizeof(BlobAssetHeader));
            binaryWriter.ImHexWrite<T>("blobData");
            binaryWriter.WriteBytes(blob.m_data.Header + 1, blobAssetLength);
        }

        /// <summary>
        /// Reads bytes from a <see cref="BinaryReader"/> instance and deserializes them into a new blob asset.
        /// </summary>
        /// <param name="binaryReader">An implementation of the BinaryReader interface.</param>
        /// <typeparam name="T">The blob asset's root data type.</typeparam>
        /// <returns>A reference to the deserialized blob asset.</returns>
        /// <seealso cref="StreamBinaryReader"/>
        /// <seealso cref="MemoryBinaryReader"/>
        public static unsafe BlobAssetReference<T> Read<T>(this BinaryReader binaryReader) where T : unmanaged
        {
            BlobAssetHeader header;
            binaryReader.ReadBytes(&header, sizeof(BlobAssetHeader));

            var buffer = (byte*) Memory.Unmanaged.Allocate(sizeof(BlobAssetHeader) + header.Length, 16, Allocator.Persistent);
            binaryReader.ReadBytes(buffer + sizeof(BlobAssetHeader), header.Length);

            var bufferHeader = (BlobAssetHeader*) buffer;
            bufferHeader->Allocator = Allocator.Persistent;
            bufferHeader->Length = header.Length;
            bufferHeader->ValidationPtr = buffer + sizeof(BlobAssetHeader);

            // @TODO use 64bit hash
            bufferHeader->Hash = header.Hash;

            BlobAssetReference<T> blobAssetReference;
            blobAssetReference.m_data.m_Align8Union = 0;
            blobAssetReference.m_data.m_Ptr = buffer + sizeof(BlobAssetHeader);

            return blobAssetReference;
        }
    }
}

namespace Unity.Entities.LowLevel.Unsafe
{
    /// <summary>
    /// An untyped reference to a blob assets. UnsafeUntypedBlobAssetReference can be cast to specific typed BlobAssetReferences.
    /// </summary>
    [ChunkSerializable]
    public struct UnsafeUntypedBlobAssetReference : IDisposable, IEquatable<UnsafeUntypedBlobAssetReference>
    {
        internal BlobAssetReferenceData m_data;

        /// <summary>
        /// Creates an unsafe untyped blob asset reference from a BlobAssetReference.
        /// </summary>
        /// <typeparam name="T">The unsafe type.</typeparam>
        /// <param name="blob">Reference to the blob asset that will be referenced by the new UnsafeUntypedBlobAssetReference.</param>
        /// <returns>An unsafe untyped blob asset reference to the blob asset.</returns>
        public static UnsafeUntypedBlobAssetReference Create<T> (BlobAssetReference<T> blob) where T : unmanaged
        {
            UnsafeUntypedBlobAssetReference value;
            value.m_data = blob.m_data;
            return value;
        }

        /// <summary>
        /// Returns a typed blob asset reference to the blob asset referenced by this instance.
        /// </summary>
        /// <typeparam name="T">The reinterpreted type.</typeparam>
        /// <returns>A blob asset reference of the reinterpreted type.</returns>
        public BlobAssetReference<T> Reinterpret<T>() where T : unmanaged
        {
            BlobAssetReference<T> value;
            value.m_data = m_data;
            return value;
        }

        /// <summary>
        /// Disposes the UnsafeUntypedBlobAssetReference object.
        /// </summary>
        public void Dispose()
        {
            m_data.Dispose();
        }

        /// <summary>
        /// Two UnsafeUntypedBlobAssetReference are equal when they reference the same data.
        /// </summary>
        /// <param name="other">The reference to compare to this one.</param>
        /// <returns>True, if both references point to the same data or if both are null.</returns>
        public bool Equals(UnsafeUntypedBlobAssetReference other)
        {
            return m_data.Equals(other.m_data);
        }
    }
}
