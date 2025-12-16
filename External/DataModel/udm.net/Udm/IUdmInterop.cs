using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

// typedef struct udm_hash
// {
//     union
//     {
//         uint8_t  data[16];
//         uint32_t uint32_data[4];
//         uint64_t uint64_data[2];
//     };
// } udm_hash_t;
using udm_hash = Unity.DataModel.Hash;
using udm_const_hash = Unity.DataModel.Hash;

// typedef struct udm_type_id
// {
//    udm_hash_t hash;
// } udm_type_id_t;
using udm_type_id = Unity.DataModel.UdmTypeId;
using udm_const_type_id = Unity.DataModel.UdmTypeId;

// typedef struct udm_schema_id
// {
//     udm_hash_t hash;
// } udm_schema_id_t;
using udm_schema_id = Unity.DataModel.SchemaId;
using udm_const_schema_id = Unity.DataModel.SchemaId;

// typedef uint64_t udm_object_id_t;
using udm_object_id = Unity.DataModel.UdmObjectId;

// typedef struct udm_logger
// {
//     udm_log_handler_t handler;
//     udm_log_handler_context_t context;
// } udm_logger_t;
using udm_logger = Unity.DataModel.UdmLogger;

// typedef struct udm_guid
// {
//     union
//     {
//         uint8_t  data[16];
//         uint32_t uint32_data[4];
//         uint64_t uint64_data[2];
//     };
// } udm_guid_t;
using udm_guid = Unity.DataModel.UdmGuid;
using udm_const_guid = Unity.DataModel.ConstUdmGuid;

// typedef struct udm_utf8string_field
// {
//     uint64_t size; /* Character Count */
//     uint64_t location;
// } udm_utf8string_field_t;
using udm_utf8string_field_t = Unity.DataModel.UTF8StringField;

// typedef struct udm_utf8string_accessor
// {
//     udm_utf8string_field_t* field;
//     udm_document_model_t*   document_model;
// } udm_utf8string_accessor_t;
using udm_utf8string_accessor = Unity.DataModel.UTF8String;

// typedef struct udm_utf8string_const_accessor
// {
//     const udm_utf8string_field_t* field;
// } udm_utf8string_const_accessor_t;
using udm_utf8string_const_accessor = Unity.DataModel.ConstUTF8String;

// typedef struct udm_reference
// {
//     udm_guid_t      document_id;
//     udm_object_id_t object_id;
//     int32_t         type;
//     int32_t         _padding_;
// } udm_reference_t;
using udm_reference = Unity.DataModel.Reference;
using udm_const_reference = Unity.DataModel.Reference;

// typedef struct udm_reference_field
// {
//     uint64_t index;
// } udm_reference_field_t;
using udm_reference_field = Unity.DataModel.ReferenceField;
using udm_const_reference_field = Unity.DataModel.ReferenceField;

// typedef struct udm_type_layout
// {
//     int8_t hasExplicitLayout;
//     int8_t hasSequentialLayout;
// 
//     int16_t overrideAlignment;
//     int32_t overrideSize;
// } udm_type_layout_t;
using udm_type_layout_t = Unity.DataModel.TypeLayout;

// typedef struct udm_vector_field
// {
//     uint64_t size; /* Element Count */
//     uint64_t location;
// } udm_vector_field_t;
using udm_vector_field_t = Unity.DataModel.VectorField;

// typedef struct udm_vector_accessor
// {
//     const udm_schema_t*   element_schema;
//     udm_vector_field_t*   field;
//     udm_document_model_t* document_model;
// } udm_vector_accessor_t;
using udm_vector_accessor = Unity.DataModel.Vector;

// typedef struct udm_vector_const_accessor
// {
//     const udm_schema_t*       element_schema;
//     const udm_vector_field_t* field;
//     const udm_reference_t*    references;
// } udm_vector_const_accessor_t;
using udm_vector_const_accessor = Unity.DataModel.ConstVector;

// typedef struct udm_field
// {
//     udm_utf8string_field_t name;
//
//     udm_schema_id_t field_schema_id;
//
//     uint64_t offset;
//     uint32_t index;
//     uint32_t padding; // padding after the field, not including end-of-struct padding
//
//     udm_schema_field_flags_t flags;
// } udm_field_t;
using udm_const_field = Unity.DataModel.SchemaFieldImpl;

// typedef struct udm_field_key
// {
//     uint32_t field_name_hash;
//     uint32_t index;
// } udm_field_key_t;
using udm_field_key = Unity.DataModel.SchemaFieldKeyImpl;

// typedef struct udm_accessor
// {
//     const udm_schema_t* schema;
//     void* data;
//     udm_document_model_t* document_model;
// } udm_accessor_t;
using udm_accessor = Unity.DataModel.Accessor;

// typedef struct udm_const_accessor
// {
//     const udm_schema_t*         schema;
//     const void*                 data;
//     const udm_reference_t*      references;
// } udm_const_accessor_t;
using udm_const_accessor = Unity.DataModel.ConstAccessor;

// typedef uint64_t udm_schema_flags_t;
using udm_schema_flags = Unity.DataModel.SchemaFlags;

// typedef struct udm_schema udm_schema_t;
// typedef struct udm_schema
// {
//     udm_schema_flags_t       flags;
//     udm_utf8string_field_t   type_name;
//     udm_type_id_t            type_id;
//     uint64_t                 type_version;
//     udm_type_id_t            underlying_type_id;

//     uint64_t alignment;
//     uint64_t size;                  // size of fixed length data
//     uint64_t default_values_size;   // size of fixed length data + variable length data
//     uint64_t default_values_offset; // offset of data

//     udm_vector_field_t fields;
//     udm_vector_field_t field_keys;
//     udm_vector_field_t references;
// } udm_schema_t;
using udm_const_schema = Unity.DataModel.SchemaImpl;

// typedef struct udm_object_model
// {
//     udm_object_id_t       object_id;
//     udm_accessor_t        accessor;
// } udm_object_model_t;
using udm_object_model = Unity.DataModel.ObjectModel;

// typedef struct udm_const_object_model
// {
//     udm_object_id_t       object_id;
//     udm_const_accessor_t  accessor;
// } udm_const_object_model_t;
using udm_const_object_model = Unity.DataModel.ConstObjectModel;

// typedef struct udm_object_collection_per_schema
// {
//     udm_schema_id_t schema_id;
//     uint64_t        object_count;
//     uint64_t        first_object_index;
//     uint64_t        object_data_offset;
// } udm_object_collection_per_schema_t;
using udm_object_collection_per_schema = Unity.DataModel.ObjectCollectionPerSchema;

// typedef struct udm_binary_header
// {
//     uint32_t magic;
//     uint32_t version;

//     uint64_t external_document_ids_offset;
//     uint64_t external_document_ids_count;

//     uint64_t references_offset;
//     uint64_t references_count;

//     uint64_t object_collection_per_schema_offset;
//     uint64_t object_collection_per_schema_count;

//     uint64_t component_collection_per_schema_offset;
//     uint64_t component_collection_per_schema_count;

//     uint64_t object_ids_offset;
//     uint64_t object_ids_count;

//     udm_hash_t body_hash;
// } udm_binary_header_t;
using udm_binary_header = Unity.DataModel.BinaryHeaderImpl;

// typedef struct udm_object_model_iterator
// {
//    void* data_structure;
//    void* iterator;
//    udm_object_model_t current;
// } udm_object_model_iterator_t;
using udm_object_model_iterator = Unity.DataModel.ObjectModels.Enumerator.Iterator;

// typedef struct udm_const_object_model_iterator
// {
//    const void* data_structure;
//    void* iterator;
//    udm_const_object_model_t current;
// } udm_const_object_model_iterator_t;
using udm_const_object_model_iterator = Unity.DataModel.ConstObjectModels.Enumerator.Iterator;

// typedef struct udm_document_model_schema_iterator
// {
//     const void* data_structure;
//     void* iterator;
//     const udm_schema_t* schema;
// } udm_document_model_schema_iterator_t;
using udm_document_model_schema_iterator = Unity.DataModel.DocumentModelSchemas.Enumerator.Iterator;

// typedef struct udm_object_model_pair_entry
// {
//     udm_object_id_t object_id;
//     void* data;
// } udm_object_model_pair_entry_t;
using udm_object_model_pair_entry = Unity.DataModel.ObjectModelPairEntry;

//typedef struct udm_object_model_per_schema
//{
//    const udm_schema_t* schema;
//    const udm_object_model_pair_entry_t* data;
//    uint64_t count;
//    udm_document_model_t* document_model;
//} udm_object_model_per_schema_t;
using udm_object_model_per_schema = Unity.DataModel.ObjectModelsPerSchema;

//typedef struct udm_const_object_model_per_schema
//{
//    const udm_schema_t* schema;
//    const udm_object_model_pair_entry_t* data;
//    uint64_t count;
//    const udm_document_model_t* document_model;
//} udm_const_object_model_per_schema_t;
using udm_const_object_model_per_schema = Unity.DataModel.ConstObjectModelsPerSchema;

// typedef struct udm_object_model_schema_data_pair
// {
//     const udm_schema_t* schema;
//     void* data;
// } udm_object_model_schema_data_pair_t;
using udm_object_model_schema_data_pair = Unity.DataModel.ObjectModelSchemaDataPair;

// typedef struct udm_object_model_ecs_components
// {
//     const udm_object_model_schema_data_pair_t* entries;
//     uint64_t count;
//     udm_document_model_t* document_model;
// } udm_object_model_ecs_components_t;
using udm_object_model_ecs_components = Unity.DataModel.ObjectModelEcsComponents;

// typedef struct udm_const_object_model_ecs_components
// {
//     const udm_object_model_schema_data_pair_t* entries;
//     uint64_t count;
//     const udm_document_model_t* document_model;
// } udm_const_object_model_ecs_components_t;
using udm_const_object_model_ecs_components = Unity.DataModel.ConstObjectModelEcsComponents;

// Opaque types
// typedef struct udm_schema_builder udm_schema_builder_t;
using udm_schema_builder_ptr = System.IntPtr;
using udm_const_schema_builder_ptr = System.IntPtr;
// typedef struct udm_document_model udm_document_model_t;
using udm_document_model_ptr = System.IntPtr;
using udm_const_document_model_ptr = System.IntPtr;

// typedef void* udm_data_system_context_t;
using udm_data_system_context = System.IntPtr;

// typedef void* udm_data_system_data_context_t;
using udm_data_system_data_context = System.IntPtr;

// typedef void* udm_schema_manager_context_t;
using udm_schema_manager = System.IntPtr;

namespace Unity.DataModel
{
    [VisibleToOtherModules]
    internal interface IUdmInterop
    {
        // Initialization -------------------------------------------
        internal void udm_initialize(IntPtr allocator, IntPtr deallocator, IntPtr data_system_context, IntPtr commit_data,
            IntPtr acquire_data, IntPtr release_data, IntPtr asset_database,
            IntPtr asset_database_get_schema_id_by_type, IntPtr asset_database_get_schema,
            IntPtr asset_database_get_or_create_schema, IntPtr asset_database_get_schema_id_by_schema,
            IntPtr asset_database_register_schema);
        internal void udm_types_initialize(IntPtr schema_manager, IntPtr build_basic_schema);
        internal void cleanup();
        internal unsafe uint udm_xxhash32(byte* data, ulong size);
        internal unsafe udm_hash udm_xxh3_128(byte* data, ulong size);

        [VisibleToOtherModules]
        unsafe internal IntPtr udm_get_schema_manager();

        // Memory -------------------------------------------
        unsafe internal IntPtr udm_allocate_with_tags(ulong alignment, ulong size, string file, ulong line);
        unsafe internal void udm_deallocate_with_tags(IntPtr ptr, string file, ulong line);
        internal unsafe IntPtr udm_get_default_allocator();
        internal unsafe IntPtr udm_get_default_deallocator();

        // Data System ---------------------------------
        unsafe internal IntPtr udm_get_default_data_system_commit();
        unsafe internal IntPtr udm_get_default_data_system_acquire();
        unsafe internal IntPtr udm_get_default_data_system_release();

        // Logging -------------------------------------------
        unsafe internal void udm_logger_log(udm_logger* logger, UdmLogType type, string file, int line, udm_object_id object_id, string message);

        internal udm_logger udm_get_stderr_logger();
        internal udm_logger udm_get_default_logger();
        internal void udm_set_default_logger(udm_logger logger);

        // Type ID ------------------------------------------
        unsafe internal udm_type_id udm_type_id_get_vector_type_id(udm_const_type_id* element_type_id);
        unsafe internal udm_type_id udm_type_id_get_map_type_id(udm_type_id* key_type_id, udm_type_id* value_type_id);

        // String -------------------------------------------
        unsafe internal ulong udm_utf8string_string_length(udm_utf8string_accessor* str);
        unsafe internal void udm_utf8string_assign(udm_utf8string_accessor* str, byte* value, ulong size);
        unsafe internal void udm_utf8string_append(udm_utf8string_accessor* str, byte* value, ulong size);
        unsafe internal void udm_utf8string_append_uninitialized(udm_utf8string_accessor* str, ulong size);
        unsafe internal void udm_utf8string_clear(udm_utf8string_accessor* str);
        unsafe internal void udm_utf8string_reserve(udm_utf8string_accessor* str, ulong capacity);
        unsafe internal void udm_utf8string_replace_uninitialized(udm_utf8string_accessor* str, ulong size);
        unsafe internal ulong udm_utf8string_capacity(udm_utf8string_accessor* str);

        unsafe internal ulong udm_const_utf8string_string_length(udm_utf8string_const_accessor* str);

        // Vector -------------------------------------------
        unsafe internal void udm_vector_clear(udm_vector_accessor* vector);
        unsafe internal void udm_vector_reserve(udm_vector_accessor* vector, ulong capacity);
        unsafe internal IntPtr udm_vector_insert_uninitialized(udm_vector_accessor* vector, ulong index);
        unsafe internal void udm_vector_erase(udm_vector_accessor* vector, ulong index);
        unsafe internal void udm_vector_resize_uninitialized(udm_vector_accessor* vector, ulong size);
        unsafe internal void udm_vector_assign(udm_vector_accessor* vector, void* data, ulong size);
        unsafe internal IntPtr udm_vector_push_back_uninitialized(udm_vector_accessor* vector);

        // Accessors -------------------------------------------
        unsafe internal int udm_const_accessor_is_equal(udm_const_accessor* lhs, udm_const_accessor* rhs);

        unsafe internal void udm_accessor_initialize(udm_accessor* destination, udm_const_accessor* source);

        // Types -------------------------------------------
        unsafe internal udm_const_schema* udm_int8_schema();
        unsafe internal udm_const_schema* udm_uint8_schema();
        unsafe internal udm_const_schema* udm_int16_schema();
        unsafe internal udm_const_schema* udm_uint16_schema();
        unsafe internal udm_const_schema* udm_int32_schema();
        unsafe internal udm_const_schema* udm_uint32_schema();
        unsafe internal udm_const_schema* udm_int64_schema();
        unsafe internal udm_const_schema* udm_uint64_schema();
        unsafe internal udm_const_schema* udm_float_schema();
        unsafe internal udm_const_schema* udm_double_schema();
        unsafe internal udm_const_schema* udm_hash_schema();
        unsafe internal udm_const_schema* udm_guid_schema();
        unsafe internal udm_const_schema* udm_reference_schema();
        unsafe internal udm_const_schema* udm_utf8string_schema();

        // Schema -------------------------------------------
        unsafe internal udm_const_schema* udm_schema_get_by_id(udm_const_schema_id* schema_id);
        unsafe internal udm_const_schema* udm_schema_get_or_create_by_id(udm_const_schema_id* schema_id);
        unsafe internal udm_const_schema* udm_schema_get_by_type(udm_const_type_id* type_id, ulong type_version);
        unsafe internal udm_schema_id udm_schema_get_id(udm_const_schema* schema);
        unsafe internal void udm_schema_to_text(udm_const_schema* schema, IntPtr user_context, /*udm_write_function*/ void* write_function);

        // Schema Builder -------------------------------------------
        unsafe internal udm_schema_builder_ptr udm_schema_builder_new(IntPtr udm_schema_manager_ptr, udm_const_schema* base_schema, string type_name, udm_const_type_id* type_id, ulong type_version, TypeLayout typeLayout);
        unsafe internal void udm_schema_builder_add_field(udm_schema_builder_ptr schema_builder, string name, int explicit_offset, udm_const_accessor default_value, SchemaFieldFlags flags = default);
        unsafe internal ulong udm_schema_builder_get_fields_count(udm_const_schema_builder_ptr schema_builder);
        unsafe internal void udm_schema_builder_set_inline_text_serialization(udm_schema_builder_ptr schema_builder, int inline_text_serialization);
        unsafe internal void udm_schema_builder_set_as_fixed_buffer(udm_schema_builder_ptr schema_builder, int is_fixed_buffer);
        unsafe internal void udm_schema_builder_set_as_managed(udm_schema_builder_ptr schema_builder, int is_managed);
        unsafe internal void udm_schema_builder_set_underlying_type_id(udm_schema_builder_ptr schema_builder, udm_const_type_id* underlying_type_id);
        unsafe internal udm_const_schema* udm_schema_builder_build_schema(udm_schema_builder_ptr schema_builder);
        unsafe internal udm_const_schema* udm_schema_builder_build_vector_schema(IntPtr schema_manager, udm_const_schema* element_schema, string type_name, udm_const_type_id* type_id, int is_managed);
        unsafe internal udm_const_schema* udm_schema_builder_build_map_schema(IntPtr schema_manager, udm_const_schema* key_schema, udm_const_schema* value_schema);
        unsafe internal udm_const_schema* udm_schema_builder_build_basic_schema_with_underlying_type(IntPtr udm_schema_manager_ptr, udm_const_type_id* underlying_type_id, string type_name, udm_const_type_id* type_id, ulong type_version, int is_managed);
        unsafe internal void udm_schema_builder_delete(udm_schema_builder_ptr schema_builder);
        unsafe internal IntPtr get_schema_builder_build_basic_schema_function();

        // Document Model binary header
        internal unsafe int udm_binary_header_is_valid(udm_binary_header* header);
        unsafe internal int udm_is_binary_header(byte* data, ulong data_size);

        unsafe internal uint udm_get_current_binary_version();

        // Document Model -------------------------------------------
        unsafe internal int udm_is_document_model_text(byte* text_data, ulong text_data_size);

        internal unsafe udm_document_model_ptr udm_document_model_new();
        internal unsafe udm_const_document_model_ptr udm_document_model_new_from_text(byte* text_data, ulong text_data_size);
        internal unsafe udm_const_document_model_ptr udm_document_model_new_from_binary_header(udm_binary_header* header);
        unsafe internal void udm_document_model_delete(udm_document_model_ptr document_model);
        unsafe internal void udm_document_model_to_text(udm_const_document_model_ptr document_model, IntPtr user_context, /*udm_write_function*/ void* write_function);
        unsafe internal udm_hash udm_document_model_to_binary(udm_const_document_model_ptr document_model);
        internal unsafe udm_object_model udm_document_model_new_object_model(udm_document_model_ptr document_model, udm_const_schema* schema);
        internal unsafe udm_object_model udm_document_model_new_object_model_with_id(udm_document_model_ptr document_model, udm_const_schema* schema, udm_object_id out_object_id);
        internal unsafe udm_object_model udm_document_model_copy_object_model_from_source(udm_document_model_ptr document_model, udm_const_accessor* source, udm_object_id out_object_id);
        unsafe internal void udm_document_model_delete_object_model(udm_document_model_ptr document_model, udm_object_id udmObjectId);
        unsafe internal udm_object_model udm_document_model_get_object_model(udm_document_model_ptr document_model, udm_object_id udmObjectId);
        unsafe internal udm_const_object_model udm_document_model_get_const_object_model(udm_const_document_model_ptr document_model, udm_object_id udmObjectId);
        unsafe internal ulong udm_document_model_get_objects_count(udm_const_document_model_ptr document_model);
        unsafe internal void udm_document_model_extract_external_document_ids(udm_document_model_ptr document_model);
        unsafe internal udm_const_guid* udm_document_model_get_external_document_ids(udm_const_document_model_ptr document_model);
        unsafe internal udm_reference* udm_document_model_get_references(udm_const_document_model_ptr document_model);
        unsafe internal ulong udm_document_model_get_references_size(udm_const_document_model_ptr document_model);
        unsafe internal ulong udm_document_model_get_external_document_ids_size(udm_const_document_model_ptr document_model);
        unsafe internal ulong udm_document_model_get_dynamic_memory_usage(udm_const_document_model_ptr document_model);
        unsafe internal void udm_document_model_set_reference(udm_document_model_ptr document_model, udm_reference_field* field, udm_reference reference);

        unsafe internal udm_object_model_iterator* udm_object_model_iterator_new(udm_document_model_ptr document_model);
        unsafe internal void udm_object_model_iterator_next(udm_object_model_iterator* object_model_it);
        unsafe internal void udm_object_model_iterator_reset(udm_object_model_iterator* object_model_it);
        unsafe internal void udm_object_model_iterator_delete(udm_object_model_iterator* object_model_it);

        unsafe internal udm_const_object_model_iterator* udm_const_object_model_iterator_new(udm_const_document_model_ptr document_model);
        unsafe internal void udm_const_object_model_iterator_next(udm_const_object_model_iterator* object_model_it);
        unsafe internal void udm_const_object_model_iterator_reset(udm_const_object_model_iterator* object_model_it);
        unsafe internal void udm_const_object_model_iterator_delete(udm_const_object_model_iterator* object_model_it);

        unsafe internal udm_document_model_schema_iterator* udm_document_model_schema_iterator_new(udm_document_model_ptr document_model);
        unsafe internal void udm_document_model_schema_iterator_next(udm_document_model_schema_iterator* schema_it);
        unsafe internal void udm_document_model_schema_iterator_reset(udm_document_model_schema_iterator* schema_it);
        unsafe internal void udm_document_model_schema_iterator_delete(udm_document_model_schema_iterator* schema_it);

        unsafe internal void udm_document_model_get_objects_per_schema(udm_document_model_ptr document_model, udm_const_schema* schema, udm_object_model_per_schema* output_objects);
        unsafe internal void udm_document_model_get_const_objects_per_schema(udm_const_document_model_ptr document_model, udm_const_schema* schema, udm_const_object_model_per_schema* output_objects);
        unsafe internal udm_accessor udm_document_model_add_ecs_component(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);
        unsafe internal void udm_document_model_remove_ecs_component(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);
        unsafe internal udm_accessor udm_document_model_get_ecs_component(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);
        unsafe internal udm_const_accessor udm_document_model_get_const_ecs_component(udm_const_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);
        unsafe internal void udm_document_model_get_ecs_components(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_object_model_ecs_components* output_components);
        unsafe internal void udm_document_model_get_const_ecs_components(udm_const_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_object_model_ecs_components* output_components);
    }
}
