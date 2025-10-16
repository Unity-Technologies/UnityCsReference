// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.DataModel
{
    internal sealed class UnityUdmInterop : IUdmInterop
    {
        public void cleanup()
        {
            UDM.cleanup();
        }

        public IntPtr udm_get_schema_manager()
        {
            return UDM.udm_get_schema_manager();
        }

        public unsafe void udm_accessor_initialize(Accessor* destination, ConstAccessor* source)
        {
            UDM.udm_accessor_initialize(destination, source);
        }

        public unsafe int udm_const_accessor_is_equal(ConstAccessor* lhs, ConstAccessor* rhs)
        {
            return UDM.udm_const_accessor_is_equal(lhs, rhs);
        }

        public unsafe ulong udm_const_utf8string_string_length(ConstUTF8String* str)
        {
            return UDM.udm_const_utf8string_string_length(str);
        }

        public unsafe Accessor udm_document_model_add_ecs_component(IntPtr document_model, UdmObjectId udmObjectId, SchemaImpl* schema)
        {
            return UDM.udm_document_model_add_ecs_component(document_model, udmObjectId, schema);
        }

        public void udm_document_model_delete(IntPtr document_model)
        {
            UDM.udm_document_model_delete(document_model);
        }

        public void udm_document_model_delete_object_model(IntPtr document_model, UdmObjectId udmObjectId)
        {
            UDM.udm_document_model_delete_object_model(document_model, udmObjectId);
        }

        public ulong udm_document_model_get_dynamic_memory_usage(IntPtr document_model)
        {
            return UDM.udm_document_model_get_dynamic_memory_usage(document_model);
        }

        public unsafe Accessor udm_document_model_get_ecs_component(IntPtr document_model, UdmObjectId udmObjectId, SchemaImpl* schema)
        {
            return UDM.udm_document_model_get_ecs_component(document_model, udmObjectId, schema);
        }

        public unsafe ConstAccessor udm_document_model_get_const_ecs_component(IntPtr document_model, UdmObjectId udmObjectId, SchemaImpl* schema)
        {
            return UDM.udm_document_model_get_const_ecs_component(document_model, udmObjectId, schema);
        }

        public unsafe void udm_document_model_get_ecs_components(IntPtr document_model, UdmObjectId udmObjectId, ObjectModelEcsComponents* output_components)
        {
            UDM.udm_document_model_get_ecs_components(document_model, udmObjectId, output_components);
        }

        public unsafe void udm_document_model_get_const_ecs_components(IntPtr document_model, UdmObjectId udmObjectId, ConstObjectModelEcsComponents* output_components)
        {
            UDM.udm_document_model_get_const_ecs_components(document_model, udmObjectId, output_components);
        }

        public unsafe ConstUdmGuid* udm_document_model_get_external_document_ids(IntPtr document_model)
        {
            return UDM.udm_document_model_get_external_document_ids(document_model);
        }

        public unsafe void udm_document_model_extract_external_document_ids(IntPtr document_model)
        {
            UDM.udm_document_model_extract_external_document_ids(document_model);
        }

        public unsafe Reference* udm_document_model_get_references(IntPtr document_model)
        {
            return UDM.udm_document_model_get_references(document_model);
        }

        public ulong udm_document_model_get_external_document_ids_size(IntPtr document_model)
        {
            return UDM.udm_document_model_get_external_document_ids_size(document_model);
        }

        public ulong udm_document_model_get_objects_count(IntPtr document_model)
        {
            return UDM.udm_document_model_get_objects_count(document_model);
        }

        public unsafe void udm_document_model_get_objects_per_schema(IntPtr document_model, SchemaImpl* schema, ObjectModelsPerSchema* output_objects)
        {
            UDM.udm_document_model_get_objects_per_schema(document_model, schema, output_objects);
        }

        public unsafe void udm_document_model_get_const_objects_per_schema(IntPtr document_model, SchemaImpl* schema, ConstObjectModelsPerSchema* output_objects)
        {
            UDM.udm_document_model_get_const_objects_per_schema(document_model, schema, output_objects);
        }

        public ObjectModel udm_document_model_get_object_model(IntPtr document_model, UdmObjectId udmObjectId)
        {
            return UDM.udm_document_model_get_object_model(document_model, udmObjectId);
        }

        public ConstObjectModel udm_document_model_get_const_object_model(IntPtr document_model, UdmObjectId udmObjectId)
        {
            return UDM.udm_document_model_get_const_object_model(document_model, udmObjectId);
        }

        public IntPtr udm_document_model_new()
        {
            return UDM.udm_document_model_new();
        }

        public unsafe IntPtr udm_document_model_new_from_text(byte* text_data, ulong text_data_size)
        {
            return UDM.udm_document_model_new_from_text(text_data, text_data_size);
        }

        public unsafe IntPtr udm_document_model_new_from_binary_header(BinaryHeaderImpl* header)
        {
            return UDM.udm_document_model_new_from_binary_header(header);
        }

        public unsafe int udm_binary_header_is_valid(BinaryHeaderImpl* header)
        {
            return UDM.udm_binary_header_is_valid(header);
        }

        public unsafe ObjectModel udm_document_model_new_object_model(IntPtr document_model, SchemaImpl* schema)
        {
            return UDM.udm_document_model_new_object_model(document_model, schema);
        }

        public unsafe ObjectModel udm_document_model_new_object_model_with_id(IntPtr document_model, SchemaImpl* schema, UdmObjectId out_object_id)
        {
            return UDM.udm_document_model_new_object_model_with_id(document_model, schema, out_object_id);
        }

        public unsafe void udm_document_model_remove_ecs_component(IntPtr document_model, UdmObjectId udmObjectId, SchemaImpl* schema)
        {
            UDM.udm_document_model_remove_ecs_component(document_model, udmObjectId, schema);
        }

        public unsafe void udm_document_model_schema_iterator_delete(DocumentModelSchemas.Enumerator.Iterator* schema_it)
        {
            UDM.udm_document_model_schema_iterator_delete(schema_it);
        }

        public unsafe DocumentModelSchemas.Enumerator.Iterator* udm_document_model_schema_iterator_new(IntPtr document_model)
        {
            return UDM.udm_document_model_schema_iterator_new(document_model);
        }

        public unsafe void udm_document_model_schema_iterator_next(DocumentModelSchemas.Enumerator.Iterator* schema_it)
        {
            UDM.udm_document_model_schema_iterator_next(schema_it);
        }

        public unsafe void udm_document_model_schema_iterator_reset(DocumentModelSchemas.Enumerator.Iterator* schema_it)
        {
            UDM.udm_document_model_schema_iterator_reset(schema_it);
        }

        public unsafe Hash udm_document_model_to_binary(IntPtr document_model)
        {
            return UDM.udm_document_model_to_binary(document_model);
        }

        public unsafe ObjectModel udm_document_model_copy_object_model_from_source(IntPtr document_model, ConstAccessor* source, UdmObjectId out_object_id)
        {
            return UDM.udm_document_model_copy_object_model_from_source(document_model, source, out_object_id);
        }

        public unsafe SchemaImpl* udm_double_schema()
        {
            return UDM.udm_double_schema();
        }

        public unsafe SchemaImpl* udm_float_schema()
        {
            return UDM.udm_float_schema();
        }

        public IntPtr udm_get_default_allocator()
        {
            return UDM.udm_get_default_allocator();
        }

        public IntPtr udm_get_default_deallocator()
        {
            return UDM.udm_get_default_deallocator();
        }

        public unsafe SchemaImpl* udm_guid_schema()
        {
            return UDM.udm_guid_schema();
        }

        public unsafe SchemaImpl* udm_hash_schema()
        {
            return UDM.udm_hash_schema();
        }

        public void udm_initialize(IntPtr allocator, IntPtr deallocator, IntPtr data_system_context, IntPtr commit_data, IntPtr acquire_data, IntPtr release_data, IntPtr asset_database, IntPtr asset_database_get_schema_id_by_type, IntPtr asset_database_get_schema, IntPtr asset_database_get_or_create_schema, IntPtr asset_database_get_schema_id_by_schema, IntPtr asset_database_register_schema)
        {
            UDM.udm_initialize(allocator, deallocator, data_system_context, commit_data, acquire_data, release_data, asset_database, asset_database_get_schema_id_by_type, asset_database_get_schema, asset_database_get_or_create_schema, asset_database_get_schema_id_by_schema, asset_database_register_schema);
        }

        public unsafe SchemaImpl* udm_int16_schema()
        {
            return UDM.udm_int16_schema();
        }

        public unsafe SchemaImpl* udm_int32_schema()
        {
            return UDM.udm_int32_schema();
        }

        public unsafe SchemaImpl* udm_int64_schema()
        {
            return UDM.udm_int64_schema();
        }

        public unsafe SchemaImpl* udm_int8_schema()
        {
            return UDM.udm_int8_schema();
        }

        public unsafe void udm_object_model_iterator_delete(ObjectModels.Enumerator.Iterator* object_model_it)
        {
            UDM.udm_object_model_iterator_delete(object_model_it);
        }

        public unsafe ObjectModels.Enumerator.Iterator* udm_object_model_iterator_new(IntPtr document_model)
        {
            return UDM.udm_object_model_iterator_new(document_model);
        }

        public unsafe void udm_object_model_iterator_next(ObjectModels.Enumerator.Iterator* object_model_it)
        {
            UDM.udm_object_model_iterator_next(object_model_it);
        }

        public unsafe void udm_object_model_iterator_reset(ObjectModels.Enumerator.Iterator* object_model_it)
        {
            UDM.udm_object_model_iterator_reset(object_model_it);
        }

        public unsafe void udm_const_object_model_iterator_delete(ConstObjectModels.Enumerator.Iterator* object_model_it)
        {
            UDM.udm_const_object_model_iterator_delete(object_model_it);
        }

        public unsafe ConstObjectModels.Enumerator.Iterator* udm_const_object_model_iterator_new(IntPtr document_model)
        {
            return UDM.udm_const_object_model_iterator_new(document_model);
        }

        public unsafe void udm_const_object_model_iterator_next(ConstObjectModels.Enumerator.Iterator* object_model_it)
        {
            UDM.udm_const_object_model_iterator_next(object_model_it);
        }

        public unsafe void udm_const_object_model_iterator_reset(ConstObjectModels.Enumerator.Iterator* object_model_it)
        {
            UDM.udm_const_object_model_iterator_reset(object_model_it);
        }

        public unsafe SchemaImpl* udm_reference_schema()
        {
            return UDM.udm_reference_schema();
        }

        public void udm_schema_builder_add_field(IntPtr schema_builder, string name, int explicit_offset, ConstAccessor default_value, SchemaFieldFlags flags = default)
        {
            UDM.udm_schema_builder_add_field(schema_builder, name, explicit_offset, default_value, flags);
        }

        public unsafe SchemaImpl* udm_schema_builder_build_schema(IntPtr schema_builder)
        {
            return UDM.udm_schema_builder_build_schema(schema_builder);
        }

        public unsafe SchemaImpl* udm_schema_builder_build_vector_schema(IntPtr schema_manager, SchemaImpl* element_schema, string type_name, UdmTypeId* type_id, int is_managed)
        {
            return UDM.udm_schema_builder_build_vector_schema(schema_manager, element_schema, type_name, type_id, is_managed);
        }

        public unsafe SchemaImpl* udm_schema_builder_build_basic_schema_with_underlying_type(IntPtr schema_manager_ptr, UdmTypeId* underlying_type_id, string type_name, UdmTypeId* type_id, ulong type_version, int is_managed)
        {
            return UDM.udm_schema_builder_build_basic_schema_with_underlying_type(schema_manager_ptr, underlying_type_id, type_name, type_id, type_version, is_managed);
        }

        public void udm_schema_builder_delete(IntPtr schema_builder)
        {
            UDM.udm_schema_builder_delete(schema_builder);
        }

        public ulong udm_schema_builder_get_fields_count(IntPtr schema_builder)
        {
            return UDM.udm_schema_builder_get_fields_count(schema_builder);
        }

        public unsafe IntPtr udm_schema_builder_new(IntPtr schema_manager_ptr, SchemaImpl* base_schema, string type_name, UdmTypeId* type_id, ulong type_version, TypeLayout typeLayout)
        {
            return UDM.udm_schema_builder_new(schema_manager_ptr, base_schema, type_name, type_id, type_version, typeLayout);
        }

        public IntPtr get_schema_builder_build_basic_schema_function()
        {
            return UDM.get_schema_builder_build_basic_schema_function();
        }

        public unsafe SchemaImpl* udm_schema_get_by_id(SchemaId* schema_id)
        {
            return UDM.udm_schema_get_by_id(schema_id);
        }

        public unsafe SchemaImpl* udm_schema_get_by_type(UdmTypeId* type_id, ulong type_version)
        {
            return UDM.udm_schema_get_by_type(type_id, type_version);
        }

        public unsafe SchemaId udm_schema_get_id(SchemaImpl* schema)
        {
            return UDM.udm_schema_get_id(schema);
        }

        public unsafe UdmTypeId udm_type_id_get_vector_type_id(UdmTypeId* element_type_id)
        {
            return UDM.udm_type_id_get_vector_type_id(element_type_id);
        }

        public unsafe SchemaImpl* udm_uint16_schema()
        {
            return UDM.udm_uint16_schema();
        }

        public unsafe SchemaImpl* udm_uint32_schema()
        {
            return UDM.udm_uint32_schema();
        }

        public unsafe SchemaImpl* udm_uint64_schema()
        {
            return UDM.udm_uint64_schema();
        }

        public unsafe SchemaImpl* udm_uint8_schema()
        {
            return UDM.udm_uint8_schema();
        }

        public unsafe void udm_utf8string_append(UTF8String* str, byte* value, ulong size)
        {
            UDM.udm_utf8string_append(str, value, size);
        }

        public unsafe void udm_utf8string_append_uninitialized(UTF8String* str, ulong size)
        {
            UDM.udm_utf8string_append_uninitialized(str, size);
        }

        public unsafe void udm_utf8string_assign(UTF8String* str, byte* value, ulong size)
        {
            UDM.udm_utf8string_assign(str, value, size);
        }

        public unsafe void udm_utf8string_clear(UTF8String* str)
        {
            UDM.udm_utf8string_clear(str);
        }

        public unsafe void udm_utf8string_reserve(UTF8String* str, ulong capacity)
        {
            UDM.udm_utf8string_reserve(str, capacity);
        }

        public unsafe void udm_utf8string_replace_uninitialized(UTF8String* str, ulong size)
        {
            UDM.udm_utf8string_replace_uninitialized(str, size);
        }

        public unsafe SchemaImpl* udm_utf8string_schema()
        {
            return UDM.udm_utf8string_schema();
        }

        public unsafe ulong udm_utf8string_string_length(UTF8String* str)
        {
            return UDM.udm_utf8string_string_length(str);
        }

        public unsafe void udm_vector_assign(Vector* vector, void* data, ulong size)
        {
            UDM.udm_vector_assign(vector, data, size);
        }

        public unsafe void udm_vector_clear(Vector* vector)
        {
            UDM.udm_vector_clear(vector);
        }

        public unsafe void udm_vector_erase(Vector* vector, ulong index)
        {
            UDM.udm_vector_erase(vector, index);
        }

        public unsafe IntPtr udm_vector_insert_uninitialized(Vector* vector, ulong index)
        {
            return UDM.udm_vector_insert_uninitialized(vector, index);
        }

        public unsafe IntPtr udm_vector_push_back_uninitialized(Vector* vector)
        {
            return UDM.udm_vector_push_back_uninitialized(vector);
        }

        public unsafe void udm_vector_reserve(Vector* vector, ulong capacity)
        {
            UDM.udm_vector_reserve(vector, capacity);
        }

        public unsafe void udm_vector_resize_uninitialized(Vector* vector, ulong size)
        {
            UDM.udm_vector_resize_uninitialized(vector, size);
        }

        public unsafe void udm_document_model_to_text(IntPtr document_model, IntPtr user_context, void* write_function)
        {
            UDM.udm_document_model_to_text(document_model, user_context, write_function);
        }

        public unsafe void udm_schema_to_text(SchemaImpl* schema, IntPtr user_context, void* write_function)
        {
            UDM.udm_schema_to_text(schema, user_context, write_function);
        }

        public unsafe SchemaImpl* udm_schema_get_or_create_by_id(SchemaId* schema_id)
        {
            return UDM.udm_schema_get_or_create_by_id(schema_id);
        }

        public unsafe ulong udm_utf8string_capacity(UTF8String* str)
        {
            return UDM.udm_utf8string_capacity(str);
        }

        public unsafe int udm_is_document_model_text(byte* text_data, ulong text_data_size)
        {
            return UDM.udm_is_document_model_text(text_data, text_data_size);
        }

        public void udm_schema_builder_set_inline_text_serialization(IntPtr schema_builder, int inline_text_serialization)
        {
            UDM.udm_schema_builder_set_inline_text_serialization(schema_builder, inline_text_serialization);
        }

        public unsafe SchemaImpl* udm_schema_builder_build_map_schema(IntPtr schema_manager, SchemaImpl* key_schema, SchemaImpl* value_schema)
        {
            return UDM.udm_schema_builder_build_map_schema(schema_manager, key_schema, value_schema);
        }

        public unsafe int udm_is_binary_header(byte* data, ulong data_size)
        {
            return UDM.udm_is_binary_header(data, data_size);
        }

        public uint udm_get_current_binary_version()
        {
            return UDM.udm_get_current_binary_version();
        }

        public void udm_types_initialize(IntPtr schema_manager, IntPtr build_basic_schema)
        {
            UDM.udm_types_initialize(schema_manager, build_basic_schema);
        }

        public IntPtr udm_get_default_data_system_commit()
        {
            return UDM.udm_get_default_data_system_commit();
        }

        public IntPtr udm_get_default_data_system_acquire()
        {
            return UDM.udm_get_default_data_system_acquire();
        }

        public IntPtr udm_get_default_data_system_release()
        {
            return UDM.udm_get_default_data_system_release();
        }

        public unsafe UdmTypeId udm_type_id_get_map_type_id(UdmTypeId* key_type_id, UdmTypeId* value_type_id)
        {
            return UDM.udm_type_id_get_map_type_id(key_type_id, value_type_id);
        }

        public IntPtr udm_allocate_with_tags(ulong alignment, ulong size, string file, ulong line)
        {
            return UDM.udm_allocate_with_tags(alignment, size, file, line);
        }

        public void udm_deallocate_with_tags(IntPtr ptr, string file, ulong line)
        {
            UDM.udm_deallocate_with_tags(ptr, file, line);
        }

        unsafe public void udm_logger_log(UdmLogger* logger, UdmLogType type, string file, int line, UdmObjectId object_id, string message)
        {
            UDM.udm_logger_log(logger, type, file, line, object_id, message);
        }

        public UdmLogger udm_get_stderr_logger()
        {
            return UDM.udm_get_stderr_logger();
        }

        public UdmLogger udm_get_default_logger()
        {
            return UDM.udm_get_default_logger();
        }

        public void udm_set_default_logger(UdmLogger logger)
        {
            UDM.udm_set_default_logger(logger);
        }
    }
}
