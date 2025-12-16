using System;
using System.Runtime.InteropServices;

namespace Unity.DataModel
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectCollectionPerSchema
    {
        internal SchemaId SchemaId;
        internal ulong ObjectCount;
        internal ulong FirstObjectIndex;
        internal ulong ObjectDataOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BinaryHeaderImpl
    {
        internal uint Magic;
        internal uint Version;

        internal ulong ExternalDocumentIdsOffset;
        internal ulong ExternalDocumentIdsCount;

        internal ulong ReferencesOffset;
        internal ulong ReferencesCount;

        internal ulong ObjectCollectionPerSchemaOffset;
        internal ulong ObjectCollectionPerSchemaCount;

        internal ulong ComponentCollectionPerSchemaOffset;
        internal ulong ComponentCollectionPerSchemaCount;

        internal ulong ObjectIdsOffset;
        internal ulong ObjectIdsCount;

        internal Hash BodyHash;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BinaryHeader
    {
        internal bool IsValid() => BinaryHeaderPtr != null &&
                                   UdmInterop.Instance.udm_binary_header_is_valid(BinaryHeaderPtr) != 0;

        internal void ThrowIfInvalid()
        {
            if (!IsValid())
                throw new InvalidOperationException("Trying to use an invalid SchemaField");
        }

        internal ReadOnlySpan<UdmGuid> GetExternalDocumentIds()
        {
            ThrowIfInvalid();
            unsafe
            {
                var ptr = (byte*)BinaryHeaderPtr + BinaryHeaderPtr->ExternalDocumentIdsOffset;
                return new ReadOnlySpan<UdmGuid>((UdmGuid*)ptr, (int)BinaryHeaderPtr->ExternalDocumentIdsCount);
            }
        }

        internal ReadOnlySpan<Reference> GetReferences()
        {
            ThrowIfInvalid();
            unsafe
            {
                var ptr = (byte*)BinaryHeaderPtr + BinaryHeaderPtr->ReferencesOffset;
                return new ReadOnlySpan<Reference>((Reference*)ptr, (int)BinaryHeaderPtr->ReferencesCount);
            }
        }

        internal ReadOnlySpan<UdmObjectId> GetObjectIds()
        {
            ThrowIfInvalid();
            unsafe
            {
                var ptr = (byte*)BinaryHeaderPtr + BinaryHeaderPtr->ObjectIdsOffset;
                return new ReadOnlySpan<UdmObjectId>((UdmObjectId*)ptr, (int)BinaryHeaderPtr->ObjectIdsCount);
            }
        }

        internal ReadOnlySpan<ObjectCollectionPerSchema> GetObjectCollections()
        {
            ThrowIfInvalid();
            unsafe
            {
                var ptr = (byte*)BinaryHeaderPtr + BinaryHeaderPtr->ObjectCollectionPerSchemaOffset;
                return new ReadOnlySpan<ObjectCollectionPerSchema>((ObjectCollectionPerSchema*)ptr, (int)BinaryHeaderPtr->ObjectCollectionPerSchemaCount);
            }
        }

        internal ReadOnlySpan<ObjectCollectionPerSchema> GetComponentCollections()
        {
            ThrowIfInvalid();
            unsafe
            {
                var ptr = (byte*)BinaryHeaderPtr + BinaryHeaderPtr->ComponentCollectionPerSchemaOffset;
                return new ReadOnlySpan<ObjectCollectionPerSchema>((ObjectCollectionPerSchema*)ptr, (int)BinaryHeaderPtr->ComponentCollectionPerSchemaCount);
            }
        }

        public static unsafe implicit operator BinaryHeader(BinaryHeaderImpl* ptr)
        {
            return new BinaryHeader
            {
                BinaryHeaderPtr = ptr
            };
        }

        internal unsafe BinaryHeaderImpl* BinaryHeaderPtr;
    }
}
