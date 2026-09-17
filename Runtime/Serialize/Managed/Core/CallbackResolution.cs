// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;
// EntityId lives in namespace UnityEngine (UnityEngineObject.bindings.cs:141). The
// test-resources compile context (UNITY_NATIVE_TEST_RESOURCES) provides a stub in the
// same namespace via Runtime/Testing/ScriptWithManagedRefTestFixture.Resources_cs, so
// the bare `EntityId` field type below resolves identically in both compile contexts.
using UnityEngine;

namespace UnityEngine.Serialization;

internal static unsafe partial class SerializationBackendManagedCommands
{
    // Returns the JIT/AOT entry-point address for the method identified by
    // methodHandleValue (the backend method-handle pointer the native side
    // resolves via scripting_class_get_method_from_name). The C# executor calls
    // through it directly — e.g. `delegate*<object, void>` for a parameterless
    // ctor or a post-dispatch hook. This is the single CoreCLR-safe replacement
    // for a ScriptingInvocation (SCRIPTING-000): no reflection Invoke, no
    // UnmanagedCallersOnly, just RuntimeMethodHandle.GetFunctionPointer. Despite
    // the name it is method-agnostic — the ctor and post-dispatch-hook resolvers
    // in Common.cpp share it.
    [RequiredByNativeCode]
    internal static IntPtr GetConstructorMethodFunctionPointer(IntPtr methodHandleValue)
    {
        RuntimeMethodHandle handle = UnmarshalRuntimeMethodHandle(methodHandleValue);
        RuntimeHelpers.PrepareMethod(handle);
        return handle.GetFunctionPointer();
    }

    // Generic method-handle to function-pointer resolver used by callsites that
    // dispatch any method (not just ctors) via calli — currently the interface
    // method lookup behind CallOn{Before,After}{Class,Struct} struct callbacks.
    [RequiredByNativeCode]
    internal static IntPtr GetMethodFunctionPointer(IntPtr methodHandleValue)
    {
        RuntimeMethodHandle handle = UnmarshalRuntimeMethodHandle(methodHandleValue);
        RuntimeHelpers.PrepareMethod(handle);
        return handle.GetFunctionPointer();
    }

    // Selects which ISerializationCallbackReceiver method a struct-callback resolution targets.
    // Native (Common.cpp ResolveInterfaceMethodFunctionPointer) passes this as an int enum rather
    // than the interface method name, so no string is marshaled across the native↔managed boundary
    // on every per-type struct-callback resolution.
    internal enum SerializationCallbackMethod
    {
        OnBeforeSerialize,
        OnAfterDeserialize,
    }


    // CoreCLR-only interface-method resolver for struct-callback dispatch. The native side
    // (Common.cpp ResolveInterfaceMethodFunctionPointer, ENABLE_CORECLR) passes the declaring
    // TYPE handle + a SerializationCallbackMethod enum selector (not a method-name string, and
    // never a raw MethodDesc*) — so we avoid the
    // synthetic-handle path that aborts for methods in dynamically-loaded ALCs. We instantiate
    // the StructCallbackInvokerHelper<T> shim for the concrete struct type and return its
    // static entry point; the struct-callback callsites invoke it via `delegate*<ref byte, void>`
    // calli, identical to Mono/IL2CPP. Returns IntPtr.Zero (callback skipped) on any failure.
    [RequiredByNativeCode]
    internal static IntPtr GetInterfaceMethodFunctionPointer(IntPtr typeHandleValue, SerializationCallbackMethod callbackMethod)
    {
        // Not reachable on Mono/IL2CPP — native only calls this under ENABLE_CORECLR
        // (see Common.cpp ResolveInterfaceMethodFunctionPointer).
        return IntPtr.Zero;
    }

}
