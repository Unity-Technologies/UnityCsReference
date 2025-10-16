#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;

using udm_const_document_model_ptr = System.IntPtr;
using udm_document_model_ptr = System.IntPtr;
using udm_memory_context_ptr = System.IntPtr;
using udm_object_model_iterator_ptr = System.IntPtr;
using System.Collections.Generic;
using System.Collections;

namespace Unity.DataModel
{

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DocumentModel : IDisposable
    {
        public static DocumentModel CreateNew()
        {
            return UdmInterop.Instance.udm_document_model_new();
        }

        public static DocumentModel CreateFromText(ReadOnlySpan<byte> textData)
        {
            unsafe
            {
                fixed (byte* bytes = textData)
                {
                    return UdmInterop.Instance.udm_document_model_new_from_text(bytes, (ulong)textData.Length);
                }
            }
        }

        internal static unsafe DocumentModel CreateFromText(byte* text, ulong length)
        {
            return UdmInterop.Instance.udm_document_model_new_from_text(text, length);
        }

        public static unsafe DocumentModel CreateFromBinaryHeader(ReadOnlySpan<byte> binaryData)
        {
            unsafe
            {
                fixed (byte* bytes = binaryData)
                {
                    return UdmInterop.Instance.udm_document_model_new_from_binary_header((BinaryHeaderImpl*)bytes);
                }
            }
        }

        internal static unsafe DocumentModel CreateFromBinaryHeader(BinaryHeader header)
        {
            return UdmInterop.Instance.udm_document_model_new_from_binary_header(header.BinaryHeaderPtr);
        }

        // This is temporary, while .net 8 is supported by the rest of the build pipeline
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]

        internal static int TextWriteCallback(IntPtr userContext, byte* buffer, ulong size)
        {
            object? target = GCHandle.FromIntPtr(userContext).Target;
            if (target is Stream writer)
                writer.Write(new ReadOnlySpan<byte>(buffer, (int)size));

            return 0;
        }

        internal unsafe void ToText(Stream writer)
        {
            ThrowIfInvalid();

            if (writer == null)
                throw new InvalidOperationException("Writer is not valid");

            var handle = GCHandle.Alloc(writer);

            try
            {
                // This is temporary, while .net 8 is supported by the rest of the build pipeline
                delegate* unmanaged[Cdecl]<IntPtr, byte*, ulong, int> writeCallback = &TextWriteCallback;

                UdmInterop.Instance.udm_document_model_to_text(GetUnsafeConstData(), (IntPtr)handle, writeCallback);
            }
            finally
            {
                handle.Free();
            }
        }

        internal Hash ToBinary()
        {
            ThrowIfInvalid();

            return UdmInterop.Instance.udm_document_model_to_binary(GetUnsafeConstData());
        }

        private udm_const_document_model_ptr GetUnsafeConstData() => DocumentPtr;

        internal ObjectModel CreateObjectModel(Schema schema)
        {
            ThrowIfInvalid();
            schema.ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_new_object_model(DocumentPtr, (SchemaImpl*)schema.SchemaPtr);
            }
        }

        internal ObjectModel CreateObjectModel(Schema schema, UdmObjectId id)
        {
            ThrowIfInvalid();
            schema.ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_new_object_model_with_id(DocumentPtr, (SchemaImpl*)schema.SchemaPtr, id);
            }
        }

        internal ObjectModel CopyObjectModel(ConstAccessor sourceObjectAccessor, UdmObjectId id)
        {
            ThrowIfInvalid();
            sourceObjectAccessor.ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_copy_object_model_from_source(DocumentPtr, &sourceObjectAccessor, id);
            }
        }

        internal void DeleteObjectModel(UdmObjectId id)
        {
            ThrowIfInvalid();
            unsafe
            {
                UdmInterop.Instance.udm_document_model_delete_object_model(DocumentPtr, id);
            }
        }

        internal ObjectModel GetObjectModel(UdmObjectId id)
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_get_object_model(DocumentPtr, id);
            }
        }

        internal ConstObjectModel GetConstObjectModel(UdmObjectId id)
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_get_const_object_model(DocumentPtr, id);
            }
        }

        internal ObjectModelsPerSchema GetObjectModelsPerSchema(Schema schema)
        {
            ThrowIfInvalid();
            unsafe
            {
                return new ObjectModelsPerSchema(this, schema);
            }
        }

        internal ConstObjectModelsPerSchema GetConstObjectModelsPerSchema(Schema schema)
        {
            ThrowIfInvalid();
            unsafe
            {
                return new ConstObjectModelsPerSchema(this, schema);
            }
        }

        internal DocumentModelSchemas GetSchemas()
        {
            ThrowIfInvalid();
            unsafe
            {
                return new DocumentModelSchemas(this);
            }
        }

        internal ulong GetObjectCount()
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_get_objects_count(DocumentPtr);
            }
        }

        internal ulong GetDynamicMemoryUsage()
        {
            ThrowIfInvalid();
            return UdmInterop.Instance.udm_document_model_get_dynamic_memory_usage(DocumentPtr);
        }

        internal Accessor AddEcsComponent(UdmObjectId objectId, Schema schema)
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_add_ecs_component(DocumentPtr, objectId, (SchemaImpl*)schema.SchemaPtr);
            }
        }

        internal void RemoveEcsComponent(UdmObjectId objectId, Schema schema)
        {
            ThrowIfInvalid();
            unsafe
            {
                UdmInterop.Instance.udm_document_model_remove_ecs_component(DocumentPtr, objectId, (SchemaImpl*)schema.SchemaPtr);
            }
        }

        internal Accessor GetEcsComponent(UdmObjectId objectId, Schema schema)
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_get_ecs_component(DocumentPtr, objectId, (SchemaImpl*)schema.SchemaPtr);
            }
        }

        internal ConstAccessor GetConstEcsComponent(UdmObjectId objectId, Schema schema)
        {
            ThrowIfInvalid();
            unsafe
            {
                return UdmInterop.Instance.udm_document_model_get_const_ecs_component(DocumentPtr, objectId, (SchemaImpl*)schema.SchemaPtr);
            }
        }

        internal ObjectModelEcsComponents GetEcsComponents(UdmObjectId objectId)
        {
            ThrowIfInvalid();
            unsafe
            {
                return new ObjectModelEcsComponents(this, objectId);
            }
        }

        internal ConstObjectModelEcsComponents GetConstEcsComponents(UdmObjectId objectId)
        {
            ThrowIfInvalid();
            unsafe
            {
                return new ConstObjectModelEcsComponents(this, objectId);
            }
        }

        internal void ExtractExternalDocumentIDs()
        {
            ThrowIfInvalid();
            UdmInterop.Instance.udm_document_model_extract_external_document_ids(DocumentPtr);
        }

        internal void GetExternalDocumentIDs(List<UdmGuid> guids)
        {
            ThrowIfInvalid();
            var externalDocumentsIdsPtr = (UdmGuid*)UdmInterop.Instance.udm_document_model_get_external_document_ids(DocumentPtr);
            var size = UdmInterop.Instance.udm_document_model_get_external_document_ids_size(DocumentPtr);
            for (var i = 0ul; i < size; ++i)
            {
                guids.Add(*(externalDocumentsIdsPtr + i));
            }
        }

        internal Reference* GetReferences()
        {
            ThrowIfInvalid();
            return UdmInterop.Instance.udm_document_model_get_references(DocumentPtr);
        }

        public bool IsValid()
        {
            unsafe
            {
                return DocumentPtr != IntPtr.Zero;
            }
        }

        internal void ThrowIfInvalid()
        {
            if (!IsValid())
                throw new InvalidOperationException("Trying to use an invalid DocumentModel");
        }

        public static unsafe implicit operator DocumentModel(udm_document_model_ptr ptr)
        {
            return new DocumentModel
            {
                DocumentPtr = ptr
            };
        }

        /*
        internal static unsafe implicit operator DocumentModel(udm_const_document_model_ptr ptr)
        {
            return new DocumentModel
            {
                documentPtr = (udm_document_model_ptr)ptr
            };
        }*/

        internal ObjectModels GetObjectModels()
        {
            ThrowIfInvalid();
            return new ObjectModels(this);
        }

        internal ConstObjectModels GetConstObjectModels()
        {
            ThrowIfInvalid();
            return new ConstObjectModels(this);
        }

        public udm_document_model_ptr DocumentPtr;

        public void Dispose()
        {
            unsafe
            {
                if (IsValid())
                {
                    UdmInterop.Instance.udm_document_model_delete(DocumentPtr);
                    DocumentPtr = System.IntPtr.Zero;
                }
            }
        }
    }
}
