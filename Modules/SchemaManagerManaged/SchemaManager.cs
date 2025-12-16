// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using Unity.DataModel;

namespace UnityEngine;

[VisibleToOtherModules]
internal static unsafe partial class SchemaManager
{
// This is temporary, while .net 8 is supported by the rest of the build pipeline
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe int StreamWriteCallback(IntPtr userContext, byte* buffer, ulong size)
    {
        object? target = GCHandle.FromIntPtr(userContext).Target;
        if (target is Stream stream)
        {
            stream.Write(new Span<byte>(buffer, (int)size));
            return 0;
        }

        return 0;
    }

    public static void SchemasToBinary(IntPtr schemaManagerPtr, Stream outputStream)
    {
        var handle = GCHandle.Alloc(outputStream);

        try
        {
// This is temporary, while .net 8 is supported by the rest of the build pipeline
            delegate* unmanaged[Cdecl]<IntPtr, byte*, ulong, int> writeCallback = &StreamWriteCallback;
            SchemaManagerNative.save_schemas(schemaManagerPtr, (IntPtr)handle, writeCallback);
        }
        finally
        {
            handle.Free();
        }
    }

    public static void SchemasFromBinary(IntPtr schemaManagerPtr, byte[] data, ulong size, bool registerToDataSystem)
    {
        unsafe
        {
            fixed (byte* dataPtr = data)
            {
                SchemaManagerNative.load_schemas(schemaManagerPtr, dataPtr, size, registerToDataSystem);
            }
        }
    }

    internal static SchemaManagerSchemas GetSchemas()
    {
        IntPtr ptr = UdmInterop.Instance.udm_get_schema_manager();
        return new SchemaManagerSchemas(ptr);
    }
}
