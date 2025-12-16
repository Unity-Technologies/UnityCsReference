// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Scripting/CoreBindings.h")]
    [NativeHeader("Runtime/Scripting/UnityDataModel.bindings.h")]
    [NativeHeader("Modules/SchemaManager/Include/SchemaManager/SchemaManager.hpp")]
    internal static class SchemaManagerNative
    {
        [FreeFunction("schema_manager_save_schemas")]
        internal static unsafe extern void save_schemas(IntPtr asset_database, IntPtr user_context, void* write_function);

        [FreeFunction("schema_manager_load_schemas")]
        internal static unsafe extern void load_schemas(IntPtr asset_database, void* schemas_data,
            ulong size, bool register_to_data_system);

        [FreeFunction("schema_manager_schema_iterator_new", IsThreadSafe = true)]
        internal static unsafe extern SchemaManagerSchemas.Enumerator.Iterator* schema_iterator_new(IntPtr asset_database);

        [FreeFunction("schema_manager_schema_iterator_next", IsThreadSafe = true)]
        internal static unsafe extern void schema_iterator_next(SchemaManagerSchemas.Enumerator.Iterator* schema_it);

        [FreeFunction("schema_manager_schema_iterator_reset", IsThreadSafe = true)]
        internal static unsafe extern void schema_iterator_reset(SchemaManagerSchemas.Enumerator.Iterator* schema_it);

        [FreeFunction("schema_manager_schema_iterator_delete", IsThreadSafe = true)]
        internal static unsafe extern void schema_iterator_delete(SchemaManagerSchemas.Enumerator.Iterator* schema_it);

        [FreeFunction("get_sm_get_schema_function", IsThreadSafe = true)]
        internal static unsafe extern IntPtr get_sm_get_schema_function();

        [FreeFunction("get_sm_get_or_create_schema_function", IsThreadSafe = true)]
        internal static unsafe extern IntPtr get_sm_get_or_create_schema_function();

        [FreeFunction("get_sm_get_schema_id_by_schema_function", IsThreadSafe = true)]
        internal static unsafe extern IntPtr get_sm_get_schema_id_by_schema_function();

        [FreeFunction("get_sm_register_schema_function", IsThreadSafe = true)]
        internal static unsafe extern IntPtr get_sm_register_schema_function();
}
}
