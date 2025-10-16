// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
//     };
// } udm_guid_t;
using udm_guid = Unity.DataModel.UdmGuid;
using udm_const_guid = Unity.DataModel.ConstUdmGuid;

// typedef struct udm_utf8string_field
// {
//     uint64_t size; /* Character Count */
//     int64_t location; /* Offset from the start of the udm_utf8string_field_t struct to the data, this can be negative */
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
//     int64_t location; /* Offset from the start of the udm_vector_field_t struct to the data, this can be negative */
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
//     const udm_schema_t* schema;
//     const void*         data;
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
//     udm_underlying_type_id_t underlying_type_id;

//     uint64_t alignment;
//     uint64_t size;                  // size of fixed length data
//     uint64_t default_values_size;   // size of fixed length data + variable length data
//     uint64_t default_values_offset; // offset of data

//     udm_vector_field_t fields;
//     udm_vector_field_t field_keys;
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
    [NativeHeader("Modules/UnityDataModel/Include/UnityDataModel/UnityDataModel.bindings.h")]
    internal sealed partial class UDM
    {
        // typedef void* (*udm_aligned_allocator_t)(uint64_t, uint64_t, const char*, uint64_t);
        //
        // typedef void (*udm_aligned_deallocator_t)(void*, const char*, uint64_t);
        //
        // typedef int64_t (*udm_write_function_t)(void*, const uint8_t*, uint64_t);
        internal unsafe delegate long udm_write_function(IntPtr user_context, byte* buffer, ulong size);
        // typedef void (*udm_data_system_commit_t)(udm_data_system_context_t, udm_hash_t*, const void*, uint64_t, const udm_hash_t*, uint64_t);
        internal unsafe delegate void udm_data_system_commit_t(udm_data_system_context system, udm_hash* id, byte* data, ulong dataSizeBytes, udm_hash* dependencies, ulong dependencyCount);
        // typedef const void* (*udm_data_system_acquire_t)(udm_data_system_context_t, const udm_hash_t*, uint64_t*, udm_data_system_data_context_t*);
        internal unsafe delegate IntPtr udm_data_system_acquire_t(udm_data_system_context system, udm_hash id, udm_data_system_data_context dataContext);
        // typedef void (*udm_data_system_release_t)(udm_data_system_context_t, udm_data_system_data_context_t);
        internal unsafe delegate void udm_data_system_release_t(udm_data_system_context system, udm_data_system_data_context dataContext);

        //typedef udm_schema_id_t     (*udm_get_schema_id_by_type)(udm_schema_manager_context_t schema_manager, const udm_type_id_t* type_id, uint64_t type_version);
        //
        //typedef const udm_schema_t* (*udm_get_schema)(udm_schema_manager_context_t schema_manager, const udm_schema_id_t* schema_id);
        //
        //typedef const udm_schema_t* (*udm_get_or_create_schema)(udm_schema_manager_context_t schema_manager, const udm_schema_id_t* schema_id);
        //
        //typedef udm_schema_id_t     (*udm_get_schema_id_by_schema)(udm_schema_manager_context_t schema_manager, const udm_schema_t* schema);
        //
        //typedef const udm_schema_t* (*udm_register_schema)(udm_schema_manager_context_t schema_manager, const udm_schema_t* schema, udm_schema_id_t* schema_id);
        //

        // typedef const udm_schema_t* (*schema_builder_build_basic_schema_t)(udm_schema_manager_context_t schema_manager,
        //                                                                 const char*                  type_name,
        //                                                                 const udm_type_id*           type_id,
        //                                                                 udm_underlying_type_id_t     underlying_type_id,
        //                                                                 uint64_t                     alignment,
        //                                                                 uint64_t                     size,
        //                                                                 int                          is_fundamental,
        //                                                                 int                          is_trivially_copyable,
        //                                                                 const void*                  default_value);
        //

        // Initialization -------------------------------------------
        // UDM_API void udm_initialize(udm_aligned_allocator_t allocator, udm_aligned_deallocator_t deallocator, udm_data_system_context_t data_system_context, udm_data_system_commit_t commit_data, udm_data_system_acquire_t acquire_data, udm_data_system_release_t release_data, udm_schema_manager_context_t schema_manager, udm_get_schema_id_by_type   get_schema_id_by_type, udm_get_schema get_schema_by_type, udm_get_or_create_schema    get_or_create_schema, udm_get_schema_id_by_schema get_schema_id_by_schema, udm_register_schema register_schema);
        [FreeFunction("udm_initialize", IsThreadSafe = true)]
        unsafe internal static extern void udm_initialize(IntPtr allocator, IntPtr deallocator, IntPtr data_system_context, IntPtr commit_data, IntPtr acquire_data, IntPtr release_data, IntPtr asset_database, IntPtr asset_database_get_schema_id_by_type, IntPtr asset_database_get_schema, IntPtr asset_database_get_or_create_schema, IntPtr asset_database_get_schema_id_by_schema, IntPtr asset_database_register_schema);

        // UDM_API void udm_types_initialize(udm_schema_manager_context_t schema_manager, schema_builder_build_basic_schema_t build_basic_schema);
        [FreeFunction("udm_types_initialize", IsThreadSafe = true)]
        unsafe internal static extern void udm_types_initialize(IntPtr schema_manager, IntPtr build_basic_schema);
        // UDM_API void udm_cleanup();
        // Should be invoked inside the engine
        [FreeFunction("udm_cleanup", IsThreadSafe = true)]
        internal static extern void cleanup();
        // UDM_API udm_schema_manager_context_t udm_get_schema_manager();
        [FreeFunction("udm_get_schema_manager", IsThreadSafe = true)]
        internal static extern IntPtr udm_get_schema_manager();

        // Memory -------------------------------------------
        // TODO: To be Deprecated, needed for schema_manager_new( ... allocator )
        // UDM_API void* udm_allocate_with_tags(uint64_t alignment, uint64_t size, const char* file, uint64_t line);
        [FreeFunction("udm_allocate_with_tags", IsThreadSafe = true)]
        unsafe internal static extern IntPtr udm_allocate_with_tags(ulong alignment, ulong size, string file, ulong line);
        // UDM_API void  udm_deallocate_with_tags(void* ptr, const char* file, uint64_t line);
        // TODO: To be Deprecated, needed for schema_manager_new( ... deallocator )
        [FreeFunction("udm_deallocate_with_tags", IsThreadSafe = true)]
        unsafe internal static extern void udm_deallocate_with_tags(IntPtr ptr, string file, ulong line);

        // UDM_API void* udm_get_default_allocator();
        [FreeFunction("udm_get_default_allocator", IsThreadSafe = true)]
        internal static unsafe extern IntPtr udm_get_default_allocator();
        // UDM_API void* udm_get_default_deallocator();
        [FreeFunction("udm_get_default_deallocator", IsThreadSafe = true)]
        internal static unsafe extern IntPtr udm_get_default_deallocator();

        // Data System ---------------------------------
        // UDM_API void* udm_get_default_data_system_commit();
        [FreeFunction("udm_get_default_data_system_commit", IsThreadSafe = true)]
        internal static unsafe extern IntPtr udm_get_default_data_system_commit();
        // UDM_API void* udm_get_default_data_system_acquire();
        [FreeFunction("udm_get_default_data_system_acquire", IsThreadSafe = true)]
        internal static unsafe extern IntPtr udm_get_default_data_system_acquire();
        // UDM_API void* udm_get_default_data_system_release();
        [FreeFunction("udm_get_default_data_system_release", IsThreadSafe = true)]
        internal static unsafe extern IntPtr udm_get_default_data_system_release();

        // Logging -------------------------------------------
        // typedef void* udm_log_handler_context_t;

        // typedef void (*udm_log_handler_t)(udm_log_handler_context_t, udm_log_type_t, const char*, int, udm_object_id_t, const char*);

        // UDM_API void udm_logger_logf(udm_logger_t* logger, udm_log_type_t type, const char* file, int line, udm_object_id_t object_id, const char* format, ...);
        //
        // UDM_API void udm_logger_vlogf(udm_logger_t* logger, udm_log_type_t type, const char* file, int line, udm_object_id_t object_id, const char* format, va_list args);
        //
        // UDM_API void udm_logger_log(udm_logger_t* logger, udm_log_type_t type, const char* file, int line, udm_object_id_t object_id, const char* message);
        [FreeFunction("udm_logger_log", IsThreadSafe = true)]
        unsafe internal static extern void udm_logger_log(udm_logger* logger, UdmLogType type, string file, int line, udm_object_id object_id, string message);

        // UDM_API udm_logger_t udm_get_stderr_logger();
        [FreeFunction("udm_get_stderr_logger", IsThreadSafe = true)]
        internal static extern udm_logger udm_get_stderr_logger();
        // UDM_API udm_logger_t udm_get_default_logger();
        [FreeFunction("udm_get_default_logger", IsThreadSafe = true)]
        internal static extern udm_logger udm_get_default_logger();
        // UDM_API void         udm_set_default_logger(udm_logger_t logger);
        [FreeFunction("udm_set_default_logger", IsThreadSafe = true)]
        internal static extern void udm_set_default_logger(udm_logger logger);

        // GUID -------------------------------------------
        // UDM_API void udm_guid_initialize(udm_guid_t* guid);
        //
        // UDM_API void udm_guid_initialize_from_hex(udm_guid_t* guid, const char* hex);
        //
        // UDM_API void udm_guid_initialize_from_bytes(udm_guid_t* guid, const uint8_t* bytes);
        //
        // UDM_API int  udm_guid_is_valid(const udm_guid_t* guid);
        //
        // UDM_API void udm_guid_to_hex(const udm_guid_t* guid, char* hex, uint64_t size);
        //

        // Type ID ------------------------------------------
        // UDM_API void       udm_type_id_initialize_from_bytes(udm_type_id_t* type_id, const uint8_t* bytes, uint64_t size);
        //
        // UDM_API udm_type_id_t udm_type_id_combine(const udm_type_id_t* type_id1, const udm_type_id_t* type_id2);
        //

        // UDM_API void udm_type_id_get_vector_type_name(const udm_type_id_t* element_type_id, char* type_name, uint64_t size);
        //
        // UDM_API void udm_type_id_get_pair_type_name(const udm_type_id_t* first_type_id, const udm_type_id_t* second_type_id, char* type_name, uint64_t size);
        //
        // UDM_API void udm_type_id_get_map_type_name(const udm_type_id_t* pair_type_id, char* type_name, uint64_t size);
        //

        // UDM_API udm_type_id_t udm_type_id_get_vector_type_id(const udm_type_id_t* element_type_id);
        [FreeFunction("udm_type_id_get_vector_type_id", IsThreadSafe = true)]
        unsafe internal static extern udm_type_id udm_type_id_get_vector_type_id(udm_const_type_id* element_type_id);
        // UDM_API udm_type_id_t udm_type_id_get_map_type_id(const udm_type_id_t* key_type_id, const udm_type_id_t* value_type_id);
        [FreeFunction("udm_type_id_get_map_type_id", IsThreadSafe = true)]
        unsafe internal static extern udm_type_id udm_type_id_get_map_type_id(udm_const_type_id* key_type_id, udm_const_type_id* value_type_id);

        // Hash -------------------------------------------
        // UDM_API void udm_hash_initialize(udm_hash_t* hash);
        //
        // UDM_API void udm_hash_initialize_from_hex(udm_hash_t* hash, const char* hex);
        //
        // UDM_API int  udm_hash_is_valid(const udm_hash_t* hash);
        //
        // UDM_API void udm_hash_to_hex(const udm_hash_t* hash, char* hex, uint64_t size);
        //

        // String -------------------------------------------
        // UDM_API uint64_t udm_utf8string_string_length(const udm_utf8string_accessor_t* string);
        [FreeFunction("udm_utf8string_string_length", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_utf8string_string_length(udm_utf8string_accessor* str);
        // UDM_API char*    udm_utf8string_c_str(udm_utf8string_accessor_t* string);
        //
        // UDM_API void     udm_utf8string_assign(udm_utf8string_accessor_t* string, const char* value, uint64_t size);
        [FreeFunction("udm_utf8string_assign", IsThreadSafe = true)]
        unsafe internal static extern void udm_utf8string_assign(udm_utf8string_accessor* str, byte* value, ulong size);
        // UDM_API void     udm_utf8string_append(udm_utf8string_accessor_t* string, const char* value, uint64_t size);
        [FreeFunction("udm_utf8string_append", IsThreadSafe = true)]
        unsafe internal static extern void udm_utf8string_append(udm_utf8string_accessor* str, byte* value, ulong size);
        // UDM_API void     udm_utf8string_append_uninitialized(udm_utf8string_accessor_t* string, uint64_t size);
        [FreeFunction("udm_utf8string_append_uninitialized", IsThreadSafe = true)]
        unsafe internal static extern void udm_utf8string_append_uninitialized(udm_utf8string_accessor* str, ulong size);
        // UDM_API void     udm_utf8string_clear(udm_utf8string_accessor_t* string);
        [FreeFunction("udm_utf8string_clear", IsThreadSafe = true)]
        unsafe internal static extern void udm_utf8string_clear(udm_utf8string_accessor* str);
        // UDM_API void     udm_utf8string_reserve(udm_utf8string_accessor_t* string, uint64_t capacity);
        [FreeFunction("udm_utf8string_reserve", IsThreadSafe = true)]
        unsafe internal static extern void udm_utf8string_reserve(udm_utf8string_accessor* str, ulong capacity);
        // UDM_API void     udm_utf8string_replace_uninitialized(udm_utf8string_accessor_t* string, uint64_t size);
        [FreeFunction("udm_utf8string_replace_uninitialized", IsThreadSafe = true)]
        unsafe internal static extern void udm_utf8string_replace_uninitialized(udm_utf8string_accessor* str, ulong size);
        // UDM_API uint64_t udm_utf8string_capacity(udm_utf8string_accessor_t* string);
        [FreeFunction("udm_utf8string_capacity", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_utf8string_capacity(udm_utf8string_accessor* str);

        // UDM_API uint64_t    udm_const_utf8string_string_length(const udm_utf8string_const_accessor_t* string);
        [FreeFunction("udm_const_utf8string_string_length", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_const_utf8string_string_length(udm_utf8string_const_accessor* str);
        // UDM_API const char* udm_const_utf8string_c_str(const udm_utf8string_const_accessor_t* string);
        //

        // Reference -------------------------------------------
        // UDM_API void udm_reference_initialize(udm_reference_t* reference);
        //
        // UDM_API int  udm_reference_is_valid(const udm_reference_t* reference);
        //
        // UDM_API int  udm_reference_is_external(const udm_reference_t* reference);
        //
        // UDM_API int  udm_reference_is_internal(const udm_reference_t* reference);
        //

        // Vector -------------------------------------------
        // UDM_API void*    udm_vector_data(const udm_vector_accessor_t* vector);
        //
        // UDM_API void     udm_vector_clear(udm_vector_accessor_t* vector);
        [FreeFunction("udm_vector_clear", IsThreadSafe = true)]
        unsafe internal static extern void udm_vector_clear(udm_vector_accessor* vector);
        // UDM_API void     udm_vector_reserve(udm_vector_accessor_t* vector, uint64_t capacity);
        [FreeFunction("udm_vector_reserve", IsThreadSafe = true)]
        unsafe internal static extern void udm_vector_reserve(udm_vector_accessor* vector, ulong capacity);
        // UDM_API void*    udm_vector_insert_uninitialized(udm_vector_accessor_t* vector, uint64_t index);
        [FreeFunction("udm_vector_insert_uninitialized", IsThreadSafe = true)]
        unsafe internal static extern IntPtr udm_vector_insert_uninitialized(udm_vector_accessor* vector, ulong index);
        // UDM_API void     udm_vector_erase(udm_vector_accessor_t* vector, uint64_t index);
        [FreeFunction("udm_vector_erase", IsThreadSafe = true)]
        unsafe internal static extern void udm_vector_erase(udm_vector_accessor* vector, ulong index);
        // UDM_API void     udm_vector_resize_uninitialized(udm_vector_accessor_t* vector, uint64_t size);
        [FreeFunction("udm_vector_resize_uninitialized", IsThreadSafe = true)]
        unsafe internal static extern void udm_vector_resize_uninitialized(udm_vector_accessor* vector, ulong size);
        // UDM_API void     udm_vector_assign(udm_vector_accessor_t* vector, const void* data, uint64_t size);
        [FreeFunction("udm_vector_assign", IsThreadSafe = true)]
        unsafe internal static extern void udm_vector_assign(udm_vector_accessor* vector, void* data, ulong size);
        // UDM_API void*    udm_vector_push_back_uninitialized(udm_vector_accessor_t* vector);
        [FreeFunction("udm_vector_push_back_uninitialized", IsThreadSafe = true)]
        unsafe internal static extern IntPtr udm_vector_push_back_uninitialized(udm_vector_accessor* vector);

        // UDM_API const void* udm_const_vector_data(const udm_vector_const_accessor_t* vector);
        //

        // Field -------------------------------------------
        // typedef uint64_t udm_schema_field_flags_t;

        // UDM_API udm_utf8string_const_accessor_t udm_field_get_name(const udm_field_t* field);
        //
        // UDM_API const udm_type_id_t*       udm_field_get_type_id(const udm_field_t* field);
        //
        // UDM_API uint64_t                udm_field_get_type_version(const udm_field_t* field);
        //
        // UDM_API const udm_schema_t*     udm_field_get_schema(const udm_field_t* field);
        //

        // Accessors -------------------------------------------
        // UDM_API udm_accessor_t udm_accessor_get_field_accessor(const udm_accessor_t* accessor, const udm_field_t* field);
        //
        // UDM_API udm_const_accessor_t udm_const_accessor_get_field_accessor(const udm_const_accessor_t* accessor, const udm_field_t* field);
        //
        // UDM_API int                  udm_const_accessor_is_equal(const udm_const_accessor_t* lhs, const udm_const_accessor_t* rhs);
        [FreeFunction("udm_const_accessor_is_equal", IsThreadSafe = true)]
        unsafe internal static extern int udm_const_accessor_is_equal(udm_const_accessor* lhs, udm_const_accessor* rhs);

        // UDM_API void udm_accessor_initialize(udm_accessor_t* destination, const udm_const_accessor_t* source);
        [FreeFunction("udm_accessor_initialize", IsThreadSafe = true)]
        unsafe internal static extern void udm_accessor_initialize(udm_accessor* destination, udm_const_accessor* source);
        // UDM_API void udm_accessor_assign(udm_accessor_t* destination, const udm_const_accessor_t* source);
        [FreeFunction("udm_accessor_assign", IsThreadSafe = true)]
        unsafe internal static extern void udm_accessor_assign(udm_accessor* destination, udm_const_accessor* source);

        // Types -------------------------------------------
        // UDM_API const udm_schema_t* udm_int8_schema();
        [FreeFunction("udm_int8_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_int8_schema();
        // UDM_API const udm_schema_t* udm_uint8_schema();
        [FreeFunction("udm_uint8_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_uint8_schema();
        // UDM_API const udm_schema_t* udm_int16_schema();
        [FreeFunction("udm_int16_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_int16_schema();
        // UDM_API const udm_schema_t* udm_uint16_schema();
        [FreeFunction("udm_uint16_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_uint16_schema();
        // UDM_API const udm_schema_t* udm_int32_schema();
        [FreeFunction("udm_int32_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_int32_schema();
        // UDM_API const udm_schema_t* udm_uint32_schema();
        [FreeFunction("udm_uint32_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_uint32_schema();
        // UDM_API const udm_schema_t* udm_int64_schema();
        [FreeFunction("udm_int64_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_int64_schema();
        // UDM_API const udm_schema_t* udm_uint64_schema();
        [FreeFunction("udm_uint64_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_uint64_schema();
        // UDM_API const udm_schema_t* udm_float_schema();
        [FreeFunction("udm_float_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_float_schema();
        // UDM_API const udm_schema_t* udm_double_schema();
        [FreeFunction("udm_double_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_double_schema();
        // UDM_API const udm_schema_t* udm_hash_schema();
        [FreeFunction("udm_hash_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_hash_schema();
        // UDM_API const udm_schema_t* udm_guid_schema();
        [FreeFunction("udm_guid_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_guid_schema();
        // UDM_API const udm_schema_t* udm_reference_schema();
        [FreeFunction("udm_reference_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_reference_schema();
        // UDM_API const udm_schema_t* udm_utf8string_schema();
        [FreeFunction("udm_utf8string_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_utf8string_schema();

        // Schemas ---------------------------------
        // UDM_API const udm_schema_t*             udm_schema_get_by_id(const udm_schema_id_t* schema_id);
        [FreeFunction("udm_schema_get_by_id", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_schema_get_by_id(udm_const_schema_id* schema_id);
        // UDM_API const udm_schema_t*             udm_schema_get_or_create_by_id(const udm_schema_id_t* schema_id);
        [FreeFunction("udm_schema_get_or_create_by_id", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_schema_get_or_create_by_id(udm_const_schema_id* schema_id);
        // UDM_API const udm_schema_t*             udm_schema_get_by_type(const udm_type_id_t* type_id, uint64_t type_version);
        [FreeFunction("udm_schema_get_by_type", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_schema_get_by_type(udm_const_type_id* type_id, ulong type_version);
        // UDM_API udm_schema_id_t                 udm_schema_get_id(const udm_schema_t* schema);
        [FreeFunction("udm_schema_get_id", IsThreadSafe = true)]
        unsafe internal static extern udm_schema_id udm_schema_get_id(udm_const_schema* schema);
        // UDM_API udm_utf8string_const_accessor_t udm_schema_get_type_name(const udm_schema_t* schema);
        //
        // UDM_API const udm_schema_t*             udm_schema_get_vector_element_schema(const udm_schema_t* schema);
        //
        // UDM_API const udm_schema_t*             udm_schema_get_map_key_schema(const udm_schema_t* schema);
        //
        // UDM_API const udm_schema_t*             udm_schema_get_map_value_schema(const udm_schema_t* schema);
        //
        // UDM_API const udm_field_t*              udm_schema_get_fields(const udm_schema_t* schema);
        //
        // UDM_API const udm_field_t*              udm_schema_get_field_by_name(const udm_schema_t* schema, const char* field_name);
        //
        // UDM_API uint64_t                        udm_schema_get_field_index(const udm_schema_t* schema, const udm_field_t* field);
        //
        // UDM_API int                             udm_schema_has_field(const udm_schema_t* schema, const udm_field_t*  field);
        //
        // UDM_API udm_const_accessor_t            udm_schema_get_const_accessor(const udm_schema_t* schema);
        //
        // UDM_API void                            udm_schema_to_text(const udm_schema_t* schema, void* user_context, udm_write_function_t write_function);
        [FreeFunction("udm_schema_to_text", IsThreadSafe = true)]
        unsafe internal static extern void udm_schema_to_text(udm_const_schema* schema, IntPtr user_context, /*udm_write_function*/ void* write_function);

        // Schema Builder -------------------------------------------
        // UDM_API udm_schema_builder_t* udm_schema_builder_new(udm_schema_manager_context_t schema_manager_ptr,
        //                                                     const udm_schema_t*  base_schema,
        //                                                     const char*          type_name,
        //                                                     const udm_type_id_t* type_id,
        //                                                     uint64_t             type_version,
        //                                                     udm_type_layout_t    typeLayout,
        //                                                     int                  is_fixed_buffer,
        //                                                     int                  inline_text_serialization,
        //                                                     int                  is_managed);
        [FreeFunction("udm_schema_builder_new", IsThreadSafe = true)]
        unsafe internal static extern udm_schema_builder_ptr udm_schema_builder_new(IntPtr schema_manager_ptr, udm_const_schema* base_schema, string type_name, udm_const_type_id* type_id, ulong type_version, TypeLayout typeLayout);

        // UDM_API void                  udm_schema_builder_add_field(udm_schema_builder_t*      schema_builder,
        //                                                         const char*                name,
        //                                                         int                        explicit_offset,
        //                                                         const udm_const_accessor_t default_value_accessor,
        //                                                         udm_schema_field_flags_t   flags);
        [FreeFunction("udm_schema_builder_add_field", IsThreadSafe = true)]
        unsafe internal static extern void udm_schema_builder_add_field(udm_schema_builder_ptr schema_builder, string name, int explicit_offset, udm_const_accessor default_value, SchemaFieldFlags flags = default);

        // UDM_API uint64_t            udm_schema_builder_get_fields_count(const udm_schema_builder_t* schema_builder);
        [FreeFunction("udm_schema_builder_get_fields_count", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_schema_builder_get_fields_count(udm_const_schema_builder_ptr schema_builder);
        // UDM_API void                udm_schema_builder_set_inline_text_serialization(udm_schema_builder_t* schema_builder, int inline_text_serialization);
        [FreeFunction("udm_schema_builder_set_inline_text_serialization", IsThreadSafe = true)]
        unsafe internal static extern void udm_schema_builder_set_inline_text_serialization(udm_schema_builder_ptr schema_builder, int inline_text_serialization);
        // UDM_API const udm_schema_t* udm_schema_builder_build_schema(udm_schema_builder_t* schema_builder);
        [FreeFunction("udm_schema_builder_build_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_schema_builder_build_schema(udm_schema_builder_ptr schema_builder);
        // UDM_API const udm_schema_t* udm_schema_builder_build_vector_schema(udm_schema_manager_context_t schema_manager, const udm_schema_t* element_schema, const char* type_name, const udm_type_id* type_id, int is_managed);
        [FreeFunction("udm_schema_builder_build_vector_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_schema_builder_build_vector_schema(IntPtr schema_manager, udm_const_schema* element_schema, string type_name, udm_const_type_id* type_id, int is_managed);
        // UDM_API const udm_schema_t* udm_schema_builder_build_map_schema(udm_schema_manager_context_t schema_manager, const udm_schema_t* key_schema, const udm_schema_t* value_schema);
        [FreeFunction("udm_schema_builder_build_map_schema", IsThreadSafe = true)]
        unsafe internal static extern udm_const_schema* udm_schema_builder_build_map_schema(IntPtr schema_manager, udm_const_schema* key_schema, udm_const_schema* value_schema);
        // UDM_API void                udm_schema_builder_delete(udm_schema_builder_t* schema_builder);
        [FreeFunction("udm_schema_builder_delete", IsThreadSafe = true)]
        unsafe internal static extern void udm_schema_builder_delete(udm_schema_builder_ptr schema_builder);
        // UDM_API const udm_schema_t* udm_schema_builder_build_basic_schema(udm_schema_manager_context_t schema_manager,
        //                                                                 const char*                  type_name,
        //                                                                 const udm_type_id*           type_id,
        //                                                                 udm_underlying_type_id_t     underlying_type_id,
        //                                                                 uint64_t                     alignment,
        //                                                                 uint64_t                     size,
        //                                                                 int                          is_fundamental,
        //                                                                 int                          is_trivially_copyable,
        //                                                                 const void*                  default_value);
        //

        // UDM_API void* get_schema_builder_build_basic_schema_function();
        [FreeFunction("get_schema_builder_build_basic_schema_function", IsThreadSafe = true)]
        unsafe internal static extern IntPtr get_schema_builder_build_basic_schema_function();

        // Document Model binary header
        // UDM_API int          udm_is_binary_header(const uint8_t* data, uint64_t data_size);
        [FreeFunction("udm_is_binary_header", IsThreadSafe = true)]
        unsafe internal static extern int udm_is_binary_header(byte* data, ulong data_size);
        // UDM_API const udm_schema_t* udm_schema_builder_build_basic_schema_with_underlying_type(udm_schema_manager_context_t schema_manager, const udm_type_id_t* underlying_type_id, const char* type_name, const udm_type_id_t* type_id, uint64_t type_version, int is_managed);
        [FreeFunction("udm_schema_builder_build_basic_schema_with_underlying_type", IsThreadSafe = true)]
        unsafe public static extern udm_const_schema* udm_schema_builder_build_basic_schema_with_underlying_type(IntPtr schema_manager_ptr, udm_const_type_id* underlying_type_id, string type_name, udm_const_type_id* type_id, ulong type_version, int is_managed);
        
        // UDM_API unsigned int udm_get_current_binary_version();
        [FreeFunction("udm_get_current_binary_version", IsThreadSafe = true)]
        unsafe internal static extern uint udm_get_current_binary_version();

        // UDM_API int                                       udm_binary_header_is_valid(const udm_binary_header_t* header);
        [FreeFunction("udm_binary_header_is_valid", IsThreadSafe = true)]
        unsafe internal static extern int udm_binary_header_is_valid(udm_binary_header* header);
        // UDM_API const udm_guid_t*                         udm_binary_header_get_external_document_ids(const udm_binary_header_t* header);
        //
        // UDM_API const uint64_t*                           udm_binary_header_get_object_ids(const udm_binary_header_t* header);
        //
        // UDM_API const udm_object_collection_per_schema_t* udm_binary_header_get_object_collections(const udm_binary_header_t* header);
        //
        // UDM_API const udm_object_collection_per_schema_t* udm_binary_header_get_component_collections(const udm_binary_header_t* header);
        //

        // Document Model -------------------------------------------
        // UDM_API int udm_is_document_model_text(const uint8_t* text_data, uint64_t text_data_size);
        [FreeFunction("udm_is_document_model_text", IsThreadSafe = true)]
        unsafe internal static extern int udm_is_document_model_text(byte* text_data, ulong text_data_size);

        // UDM_API udm_document_model_t*  udm_document_model_new();
        [FreeFunction("udm_document_model_new", IsThreadSafe = true)]
        internal static extern unsafe udm_document_model_ptr udm_document_model_new();
        // UDM_API udm_document_model_t*   udm_document_model_new_from_text(const uint8_t* text_data, uint64_t text_data_size);
        [FreeFunction("udm_document_model_new_from_text", IsThreadSafe = true)]
        internal static extern unsafe udm_const_document_model_ptr udm_document_model_new_from_text(byte* text_data, ulong text_data_size);
        // UDM_API udm_document_model_t*   udm_document_model_new_from_binary_header(const udm_binary_header_t* header);
        [FreeFunction("udm_document_model_new_from_binary_header", IsThreadSafe = true)]
        internal static extern unsafe udm_const_document_model_ptr udm_document_model_new_from_binary_header(udm_binary_header* header);
        // UDM_API void                   udm_document_model_delete(udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_delete", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_delete(udm_document_model_ptr document_model);
        // UDM_API void                   udm_document_model_to_text(const udm_document_model_t* document_model,
        //                                                           void*                       user_context,
        //                                                           udm_write_function_t        write_function);
        [FreeFunction("udm_document_model_to_text", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_to_text(udm_const_document_model_ptr document_model, IntPtr user_context, /*udm_write_function*/ void* write_function);
        // UDM_API udm_hash_t             udm_document_model_to_binary(const udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_to_binary", IsThreadSafe = true)]
        unsafe internal static extern udm_hash udm_document_model_to_binary(udm_const_document_model_ptr document_model);
        // UDM_API udm_object_model_t     udm_document_model_new_object_model(udm_document_model_t* document_model,
        //                                                                    const udm_schema_t*   schema);
        [FreeFunction("udm_document_model_new_object_model", IsThreadSafe = true)]
        internal static extern unsafe udm_object_model udm_document_model_new_object_model(udm_document_model_ptr document_model, udm_const_schema* schema);
        // UDM_API udm_object_model_t     udm_document_model_new_object_model_with_id(udm_document_model_t* document_model,
        //                                                                    const udm_schema_t*   schema,
        //                                                                    udm_object_id_t       object_id);
        [FreeFunction("udm_document_model_new_object_model_with_id", IsThreadSafe = true)]
        internal static extern unsafe udm_object_model udm_document_model_new_object_model_with_id(udm_document_model_ptr document_model, udm_const_schema* schema, udm_object_id out_object_id);
        // UDM_API udm_object_model_t     udm_document_model_copy_object_model_from_source(udm_document_model_t* document_model,
        //                                                                    udm_const_accessor_t* source,
        //                                                                    udm_object_id_t       object_id);
        [FreeFunction("udm_document_model_copy_object_model_from_source", IsThreadSafe = true)]
        internal static extern unsafe udm_object_model udm_document_model_copy_object_model_from_source(udm_document_model_ptr document_model, udm_const_accessor* source, udm_object_id out_object_id);
        // UDM_API void                   udm_document_model_delete_object_model(udm_document_model_t* document_model,
        //                                                                       udm_object_id_t       object_id);
        [FreeFunction("udm_document_model_delete_object_model", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_delete_object_model(udm_document_model_ptr document_model, udm_object_id udmObjectId);
        // UDM_API udm_object_model_t      udm_document_model_get_object_model(udm_document_model_t* document_model,
        //                                                                     udm_object_id_t object_id);
        [FreeFunction("udm_document_model_get_object_model", IsThreadSafe = true)]
        unsafe internal static extern udm_object_model udm_document_model_get_object_model(udm_document_model_ptr document_model, udm_object_id udmObjectId);
        // UDM_API udm_const_object_model_t      udm_document_model_get_const_object_model(const udm_document_model_t* document_model,
        //                                                                                 udm_object_id_t object_id);
        [FreeFunction("udm_document_model_get_const_object_model", IsThreadSafe = true)]
        unsafe internal static extern udm_const_object_model udm_document_model_get_const_object_model(udm_const_document_model_ptr document_model, udm_object_id udmObjectId);
        // UDM_API uint64_t               udm_document_model_get_objects_count(const udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_get_objects_count", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_document_model_get_objects_count(udm_const_document_model_ptr document_model);
        // UDM_API const udm_guid_t*       udm_document_model_get_external_document_ids(const udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_get_external_document_ids", IsThreadSafe = true)]
        unsafe internal static extern udm_const_guid* udm_document_model_get_external_document_ids(udm_const_document_model_ptr document_model);
        //
        // UDM_API const uint64_t          udm_document_model_get_external_document_ids_size(const udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_get_external_document_ids_size", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_document_model_get_external_document_ids_size(udm_const_document_model_ptr document_model);
        // UDM_API uint64_t udm_document_model_get_dynamic_memory_usage(const udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_get_dynamic_memory_usage", IsThreadSafe = true)]
        unsafe internal static extern ulong udm_document_model_get_dynamic_memory_usage(udm_const_document_model_ptr document_model);

        // UDM_API udm_object_model_iterator_t* udm_object_model_iterator_new(udm_document_model_t* document_model);
        [FreeFunction("udm_object_model_iterator_new", IsThreadSafe = true)]
        unsafe internal static extern udm_object_model_iterator* udm_object_model_iterator_new(udm_document_model_ptr document_model);
        // UDM_API void udm_object_model_iterator_next(udm_object_model_iterator_t* object_model_it);
        [FreeFunction("udm_object_model_iterator_next", IsThreadSafe = true)]
        unsafe internal static extern void udm_object_model_iterator_next(udm_object_model_iterator* object_model_it);
                // UDM_API void                    udm_document_model_extract_external_document_ids(udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_extract_external_document_ids", IsThreadSafe = true)]
        unsafe public static extern void udm_document_model_extract_external_document_ids(udm_document_model_ptr document_model);
        // UDM_API void udm_object_model_iterator_reset(udm_object_model_iterator_t* object_model_it);
        [FreeFunction("udm_object_model_iterator_reset", IsThreadSafe = true)]
        unsafe internal static extern void udm_object_model_iterator_reset(udm_object_model_iterator* object_model_it);
        // UDM_API void udm_object_model_iterator_delete(udm_object_model_iterator_t* object_model_it);
        [FreeFunction("udm_object_model_iterator_delete", IsThreadSafe = true)]
        unsafe internal static extern void udm_object_model_iterator_delete(udm_object_model_iterator* object_model_it);

        // UDM_API const udm_reference_t*  udm_document_model_get_references(const udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_get_references", IsThreadSafe = true)]
        unsafe public static extern udm_const_reference* udm_document_model_get_references(udm_const_document_model_ptr document_model);

        // UDM_API udm_const_object_model_iterator_t* udm_const_object_model_iterator_new(const udm_document_model_t* document_model);
        [FreeFunction("udm_const_object_model_iterator_new", IsThreadSafe = true)]
        unsafe internal static extern udm_const_object_model_iterator* udm_const_object_model_iterator_new(udm_const_document_model_ptr document_model);
        // UDM_API void udm_const_object_model_iterator_next(udm_const_object_model_iterator_t* object_model_it);
        [FreeFunction("udm_const_object_model_iterator_next", IsThreadSafe = true)]
        unsafe internal static extern void udm_const_object_model_iterator_next(udm_const_object_model_iterator* object_model_it);
        // UDM_API void udm_const_object_model_iterator_reset(udm_const_object_model_iterator_t* object_model_it);
        [FreeFunction("udm_const_object_model_iterator_reset", IsThreadSafe = true)]
        unsafe internal static extern void udm_const_object_model_iterator_reset(udm_const_object_model_iterator* object_model_it);
        // UDM_API void udm_const_object_model_iterator_delete(udm_const_object_model_iterator_t* object_model_it);
        [FreeFunction("udm_const_object_model_iterator_delete", IsThreadSafe = true)]
        unsafe internal static extern void udm_const_object_model_iterator_delete(udm_const_object_model_iterator* object_model_it);

        // UDM_API udm_document_model_schema_iterator_t* udm_document_model_schema_iterator_new(udm_document_model_t* document_model);
        [FreeFunction("udm_document_model_schema_iterator_new", IsThreadSafe = true)]
        unsafe internal static extern udm_document_model_schema_iterator* udm_document_model_schema_iterator_new(udm_document_model_ptr document_model);
        // UDM_API void udm_document_model_schema_iterator_next(udm_document_model_schema_iterator_t* schema_it);
        [FreeFunction("udm_document_model_schema_iterator_next", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_schema_iterator_next(udm_document_model_schema_iterator* schema_it);
        // UDM_API void udm_document_model_schema_iterator_reset(udm_document_model_schema_iterator_t* schema_it);
        [FreeFunction("udm_document_model_schema_iterator_reset", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_schema_iterator_reset(udm_document_model_schema_iterator* schema_it);
        // UDM_API void udm_document_model_schema_iterator_delete(udm_document_model_schema_iterator_t* schema_it);
        [FreeFunction("udm_document_model_schema_iterator_delete", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_schema_iterator_delete(udm_document_model_schema_iterator* schema_it);

        // UDM_API void udm_document_model_get_objects_per_schema(udm_document_model_t* document_model, const udm_schema_t* schema, udm_object_model_per_schema_t* output_objects);
        [FreeFunction("udm_document_model_get_objects_per_schema", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_get_objects_per_schema(udm_document_model_ptr document_model, udm_const_schema* schema, udm_object_model_per_schema* output_objects);

        // UDM_API void udm_document_model_get_const_objects_per_schema(const udm_document_model_t* document_model, const udm_schema_t* schema, udm_const_object_model_per_schema_t* output_objects);
        [FreeFunction("udm_document_model_get_const_objects_per_schema", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_get_const_objects_per_schema(udm_const_document_model_ptr document_model, udm_const_schema* schema, udm_const_object_model_per_schema* output_objects);

        // UDM_API udm_accessor_t udm_document_model_add_ecs_component(udm_document_model_t* document_model, udm_object_id_t objectId, const udm_schema_t* schema);
        [FreeFunction("udm_document_model_add_ecs_component", IsThreadSafe = true)]
        unsafe internal static extern udm_accessor udm_document_model_add_ecs_component(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);

        // UDM_API void udm_document_model_remove_ecs_component(udm_document_model_t* document_model, udm_object_id_t objectId, const udm_schema_t* schema);
        [FreeFunction("udm_document_model_remove_ecs_component", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_remove_ecs_component(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);

        // UDM_API udm_accessor_t udm_document_model_get_ecs_component(udm_document_model_t* document_model, udm_object_id_t objectId, const udm_schema_t* schema);
        [FreeFunction("udm_document_model_get_ecs_component", IsThreadSafe = true)]
        unsafe internal static extern udm_accessor udm_document_model_get_ecs_component(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);

        // UDM_API udm_const_accessor_t udm_document_model_get_const_ecs_component(const udm_document_model_t* document_model, udm_object_id_t objectId, const udm_schema_t* schema);
        [FreeFunction("udm_document_model_get_const_ecs_component", IsThreadSafe = true)]
        unsafe internal static extern udm_const_accessor udm_document_model_get_const_ecs_component(udm_const_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_schema* schema);

        // UDM_API void udm_document_model_get_ecs_components(udm_document_model_t* document_model, udm_object_id_t objectId, udm_object_model_ecs_components_t* output_components);
        [FreeFunction("udm_document_model_get_ecs_components", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_get_ecs_components(udm_document_model_ptr document_model, udm_object_id udmObjectId, udm_object_model_ecs_components* output_components);

        // UDM_API void udm_document_model_get_const_ecs_components(const udm_document_model_t* document_model, udm_object_id_t objectId, udm_const_object_model_ecs_components_t* output_components);
        [FreeFunction("udm_document_model_get_const_ecs_components", IsThreadSafe = true)]
        unsafe internal static extern void udm_document_model_get_const_ecs_components(udm_const_document_model_ptr document_model, udm_object_id udmObjectId, udm_const_object_model_ecs_components* output_components);
    }
}
