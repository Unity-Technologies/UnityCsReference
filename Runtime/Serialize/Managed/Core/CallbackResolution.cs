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

    // Cache entries are raw MethodTable* keys + JIT entry points that dangle when a script
    // ALC unloads, so the cache must be cleared on editor code reload. Guard excludes the
    // native-test-resources stub compile of this file, which doesn't reference Unity.Scripting.
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(IntPtr, SerializationCallbackMethod), IntPtr>
        s_StructCallbackInvokerHandleCache = new System.Collections.Concurrent.ConcurrentDictionary<(IntPtr, SerializationCallbackMethod), IntPtr>();

    // CoreCLR struct-callback dispatch shim. C# function pointers / calli only support
    // STATIC methods, and a fn-ptr to a value-type *instance* method does not match the
    // calling convention a `delegate*<ref byte, void>` calli expects on CoreCLR (it works
    // on Mono). Routing through this static generic shim sidesteps the problem: the C#
    // compiler emits the constrained instance call internally with the correct ABI, while
    // the exposed entry point is an ordinary static `(ref T)` method whose fn-ptr IS
    // directly callable. The `where T : struct` constraint guarantees a non-shared
    // (per-value-type) instantiation, so there is no hidden generic-context argument and
    // `ref T` is layout-identical to the `ref byte` the callsite passes.
    static class StructCallbackInvokerHelper<T> where T : struct, ISerializationCallbackReceiver
    {
        public static void InvokeOnBeforeSerialize(ref T target) => target.OnBeforeSerialize();
        public static void InvokeOnAfterDeserialize(ref T target) => target.OnAfterDeserialize();
    }

    // Never instantiated: a concrete instantiation is needed only so the shim method-name
    // strings in GetInterfaceMethodFunctionPointer below can be written as compile-time-checked
    // nameof(StructCallbackInvokerHelper<…>.Invoke…) anchors that break the build if a shim
    // method is renamed, rather than as bare string literals that would silently go stale.
    private struct StructCallbackNameProbe : ISerializationCallbackReceiver
    {
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { }
    }

    // CoreCLR-only interface-method resolver for struct-callback dispatch. The native side
    // (Common.cpp ResolveInterfaceMethodFunctionPointer, ENABLE_CORECLR) passes the declaring
    // TYPE handle + a SerializationCallbackMethod enum selector (not a method-name string, and
    // never a raw MethodDesc*) — so we avoid the
    // synthetic-handle path that aborts for methods in dynamically-loaded ALCs. We instantiate
    // the StructCallbackInvokerHelper<T> shim for the concrete struct type and return its
    // static entry point; the struct-callback callsites invoke it via `delegate*<ref byte, void>`
    // calli, identical to IL2CPP. Returns IntPtr.Zero (callback skipped) on any failure.
    [RequiredByNativeCode]
    internal static IntPtr GetInterfaceMethodFunctionPointer(IntPtr typeHandleValue, SerializationCallbackMethod callbackMethod)
    {
        var cacheKey = (typeHandleValue, callbackMethod);
        if (s_StructCallbackInvokerHandleCache.TryGetValue(cacheKey, out var cached))
            return cached;

        // Map the callback selector onto the matching static shim method.
        string shimMethodName =
            callbackMethod == SerializationCallbackMethod.OnBeforeSerialize ? nameof(StructCallbackInvokerHelper<StructCallbackNameProbe>.InvokeOnBeforeSerialize) :
            callbackMethod == SerializationCallbackMethod.OnAfterDeserialize ? nameof(StructCallbackInvokerHelper<StructCallbackNameProbe>.InvokeOnAfterDeserialize) :
            null;
        if (shimMethodName == null)
            return IntPtr.Zero;

        try
        {
            // Reuse UnmarshalSystemType (CoreCLR: RuntimeTypeHandle.FromIntPtr) so the raw
            // MethodTable* is resolved the same way as every other type handle on this side.
            Type type = UnmarshalSystemType(typeHandleValue);
            if (type == null || !type.IsValueType)
                return IntPtr.Zero;

            MethodInfo shim = typeof(StructCallbackInvokerHelper<>)
                .MakeGenericType(type)
                .GetMethod(shimMethodName, BindingFlags.Public | BindingFlags.Static);
            if (shim == null)
                return IntPtr.Zero;

            // Static method => GetFunctionPointer returns a directly-callable entry point
            // (the documented "not usable" caveat applies only to instance methods).
            IntPtr fnPtr = shim.MethodHandle.GetFunctionPointer();
            return s_StructCallbackInvokerHandleCache.TryAdd(cacheKey, fnPtr)
                ? fnPtr
                : s_StructCallbackInvokerHandleCache[cacheKey];
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

}
