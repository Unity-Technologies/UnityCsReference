// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Serialization;

// Managed serialization V2: the built-in extension handler table (doc §2.4).
//
// Types the core cannot express by field composition register a handler.
// Fixed-wire handlers write exactly wireBytes at the given dest inside the
// open fixed block; array handlers own the full collection framing (count,
// payload, pad). The native V2ExtensionRegistry fetches this table once per
// domain via GetV2ExtensionTable and assigns the per-type registry slots.
//
// The handlers live in per-type files (further fragments of this partial
// class): ExtensionUnityObject.cs, ExtensionSerializeReference.cs,
// ExtensionEntityId.cs, and so on.
//
// Handler discipline: handlers receive a GC-tracked interior ref and must not
// stash it; anything crossing into native passes values or native pointers
// (dest is writer memory).
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Mirror of ExtensionTableInternal.h. Must stay in sync.
    [StructLayout(LayoutKind.Sequential)]
    private struct V2ExtensionTable
    {
        public int version;
        public int pptrWireBytes;
        public IntPtr pptrFieldHandler;
        public IntPtr pptrArrayHandler;
        public IntPtr pptrGroupHandler;
        // version 2: [SerializeReference] inline-RefId handlers.
        public int srWireBytes;
        public IntPtr srFieldHandler;
        public IntPtr srArrayHandler;
        public IntPtr srGroupHandler;
        // version 3: EntityId (LazyLoadReference<T> via asset-ref routing).
        public int entityIdWireBytes;
        public IntPtr entityIdFieldHandler;
        public IntPtr entityIdArrayHandler;
        public IntPtr entityIdGroupHandler;
        // version 4: shared dynamic-flavor handlers; per-class native
        // dispatcher fnPtr / m_Ptr offset ride the registration's userDatas.
        public IntPtr simpleNativeTypeHandler;
        public IntPtr nativeValueStructHandler;
        // version 5: PropertyName and C# fixed buffers.
        public IntPtr propertyNameHandler;
        public IntPtr fixedBufferHandler;
        // version 6: READ-side arms (M7b).
        public IntPtr pptrReadGroupHandler;
        public IntPtr pptrReadArrayHandler;
        public IntPtr entityIdReadGroupHandler;
        public IntPtr entityIdReadArrayHandler;
        // version 7: [SerializeReference] scalar read arm.
        public IntPtr srReadGroupHandler;
        // version 8: dynamic-family READ handlers (M7c).
        public IntPtr simpleNativeTypeReadHandler;
        public IntPtr nativeValueStructReadHandler;
        public IntPtr propertyNameReadHandler;
        public IntPtr fixedBufferReadHandler;
        // version 9: packed-args executor entry points. Native calls these
        // instead of the ScriptingInvocation proxy: one V2ExecutorArgs* through
        // a reverse-P/Invoke thunk replaces the nine marshalled arguments and
        // the reflection/runtime-invoke dispatch.
        public IntPtr writeExecutorEntry;
        public IntPtr readExecutorEntry;
        // version 10: dictionary element-source family (doc §2.6), the
        // EnterElementSource source/sink handlers. Per-instantiation bridge
        // indexes ride the command's opaque userData words.
        public IntPtr dictionaryElementSourceHandler;
        public IntPtr dictionaryElementSinkHandler;
        // version 11: compose-time constructor registration (native to
        // managed, reverse-P/Invoke), the composer's only managed crossing
        // besides the one-time table fetch.
        public IntPtr registerConstructor;
        // version 12: [SerializeReference] collection read handler.
        public IntPtr srReadArrayHandler;
        // version 14: exact dictionary instantiation types through managed
        // reflection, which never surfaces the canonical (__Canon) form the
        // native field-signature route can return on CoreCLR.
        public IntPtr dictionaryExactTypesHandler;
        // version 15: executor signature diet. V2ExecutorArgs shrinks to
        // (host, streamStart, context): transfer and the FUID path tables
        // ride the buffer contexts, the stream is End-terminated, frame
        // capacity rides the StreamHeader command, and the ExternalDynamic
        // handler signatures drop the transfer parameter.
        // version 16: unmanaged-base executor entries. Reuse V2ExecutorArgs
        // with `host` reinterpreted as byte* (no GCHandle).
        // Only usable against isUnmanagedSafe templates.
        public IntPtr writeExecutorUnmanagedEntry;
        public IntPtr readExecutorUnmanagedEntry;
    }

    // Mirror of V2ExecutorArgs in ExtensionTable.h. Must stay in sync.
    // Unmanaged-base callers use the same shape with `host` reinterpreted as byte*.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2ExecutorArgs
    {
        public IntPtr host;          // ScriptingGCHandle::ToUIntPtr, or byte* for the unmanaged entries
        public IntPtr streamStart;   // End-terminated exec stream, leading with StreamHeader
        public IntPtr context;       // NativeBufferContext* / NativeReadBufferContext*
    }

    // The executor entries cross from native to managed, so they need real
    // reverse-P/Invoke stubs: Marshal.GetFunctionPointerForDelegate over
    // delegates rooted in statics (collected with the domain; native
    // re-fetches the table after every reload). The C# `&method` fnptrs
    // elsewhere in this table are managed entry points callable only from
    // managed calli, and are not safe to call from native (no GC transition
    // on CoreCLR).
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void V2ExecutorEntryDelegate(V2ExecutorArgs* args);
    // The rooted delegates die with the world that created them; native
    // re-fetches the table (and fresh thunks) after every reload. Players
    // never code-reload.
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    private static V2ExecutorEntryDelegate s_V2WriteExecutorEntryDelegate;
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    private static V2ExecutorEntryDelegate s_V2ReadExecutorEntryDelegate;
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    private static V2ExecutorEntryDelegate s_V2WriteExecutorUnmanagedEntryDelegate;
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    private static V2ExecutorEntryDelegate s_V2ReadExecutorUnmanagedEntryDelegate;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int V2RegisterConstructorDelegate(IntPtr typeHandleRaw, IntPtr ctorFunctionPtr);
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    private static V2RegisterConstructorDelegate s_V2RegisterConstructorDelegate;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void V2DictionaryExactTypesDelegate(IntPtr fieldInfoHandle, IntPtr* dictType, IntPtr* entryType, IntPtr* keyType, IntPtr* valueType);
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    private static V2DictionaryExactTypesDelegate s_V2DictionaryExactTypesDelegate;

    // An exception must never unwind across the reverse-P/Invoke boundary, so
    // these shims catch everything and log. They are deliberately not routed
    // through s_V2CommandExceptionHandler: that policy fnptr rethrows in
    // native-test builds, and a rethrow here would unwind through the
    // reverse-P/Invoke frame. The shims are the boundary backstop, not command
    // policy. [MonoPInvokeCallback] makes the IL2CPP AOT compiler emit the
    // native-to-managed wrapper GetFunctionPointerForDelegate needs (a no-op
    // on the JIT backends).
    [AOT.MonoPInvokeCallback(typeof(V2ExecutorEntryDelegate))]
    private static unsafe void V2WriteExecutorEntry(V2ExecutorArgs* args)
    {
        try
        {
            ObjectsToSerializationBufferV2(GCHandle.FromIntPtr(args->host).Target,
                args->streamStart, args->context);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [AOT.MonoPInvokeCallback(typeof(V2ExecutorEntryDelegate))]
    private static unsafe void V2ReadExecutorEntry(V2ExecutorArgs* args)
    {
        try
        {
            SerializationBufferToObjectsV2(GCHandle.FromIntPtr(args->host).Target,
                args->streamStart, args->context);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [AOT.MonoPInvokeCallback(typeof(V2ExecutorEntryDelegate))]
    private static unsafe void V2WriteExecutorUnmanagedEntry(V2ExecutorArgs* args)
    {
        try
        {
            ObjectsToSerializationBufferV2Unmanaged(args->host, args->streamStart, args->context);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [AOT.MonoPInvokeCallback(typeof(V2ExecutorEntryDelegate))]
    private static unsafe void V2ReadExecutorUnmanagedEntry(V2ExecutorArgs* args)
    {
        try
        {
            SerializationBufferToObjectsV2Unmanaged(args->host, args->streamStart, args->context);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // Compose-time crossing (cold): exact dictionary instantiation types via
    // reflection. The native field-signature route can return the canonical
    // (__Canon) instantiation on CoreCLR when the generic's home image is
    // ReadyToRun, which silently breaks every identity-derived fact
    // downstream; FieldInfo.FieldType is exact by construction. The FieldInfo
    // arrives as a GCHandle (GC-safe on CoreCLR's moving GC). All-zero
    // outputs tell the caller to keep its legacy route.
    [AOT.MonoPInvokeCallback(typeof(V2DictionaryExactTypesDelegate))]
    private static unsafe void V2GetExactDictionaryFieldTypes(IntPtr fieldInfoHandle, IntPtr* dictType, IntPtr* entryType, IntPtr* keyType, IntPtr* valueType)
    {
        *dictType = IntPtr.Zero;
        *entryType = IntPtr.Zero;
        *keyType = IntPtr.Zero;
        *valueType = IntPtr.Zero;
        try
        {
            if (!(GCHandle.FromIntPtr(fieldInfoHandle).Target is System.Reflection.FieldInfo field))
                return;
            var dict = field.FieldType;
            if (!dict.IsConstructedGenericType)
                return;
            var args = dict.GetGenericArguments();
            if (args.Length != 2)
                return;
            var entry = typeof(DictionarySerialization.SerializedKeyValue<,>).MakeGenericType(args);
            *dictType = dict.TypeHandle.Value;
            *entryType = entry.TypeHandle.Value;
            *keyType = args[0].TypeHandle.Value;
            *valueType = args[1].TypeHandle.Value;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // Compose-time crossing (cold). Returning -1 tells the composer to emit
    // the fallback opcode, the same contract as an unavailable registration.
    // Boundary-backstop catch, with the same policy exemption as the executor
    // shims above.
    [AOT.MonoPInvokeCallback(typeof(V2RegisterConstructorDelegate))]
    private static int V2RegisterConstructorEntry(IntPtr typeHandleRaw, IntPtr ctorFunctionPtr)
    {
        try
        {
            return V2RegisterConstructor(typeHandleRaw, ctorFunctionPtr);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return -1;
        }
    }

    // Called once per domain by V2ExtensionRegistry::EnsureBuiltinsRegistered,
    // which passes a caller-owned buffer this fills. Nothing may be allocated
    // on this side: an allocation held past the scripting runtime fails
    // AppVerifier's leak check when a player unloads the engine in-process.
    [RequiredByNativeCode]
    public static unsafe int GetV2ExtensionTable(IntPtr buffer, int bufferSize)
    {
        if (buffer == IntPtr.Zero || bufferSize < sizeof(V2ExtensionTable))
            return -1;
        {
            var table = (V2ExtensionTable*)buffer;
            *table = default;
            table->version = 16;
            table->pptrWireBytes = 12;  // LocalSerializedObjectIdentifier
            table->pptrFieldHandler = (IntPtr)(delegate*<ref byte, byte*, NativeBufferContext*, ulong, void>)&V2WriteUnityObjectField;
            table->pptrArrayHandler = (IntPtr)(delegate*<byte[], int, uint, NativeBufferContext*, ref BufferDataStager, ulong, void>)&V2WriteUnityObjectArray;
            table->pptrGroupHandler = (IntPtr)(delegate*<ref byte, byte*, int, byte*, NativeBufferContext*, ulong, void>)&V2WriteUnityObjectGroup;
            table->srWireBytes = 8;     // RefId (SInt64)
            table->srFieldHandler = (IntPtr)(delegate*<ref byte, byte*, NativeBufferContext*, ulong, void>)&V2WriteSerializeReferenceField;
            table->srArrayHandler = (IntPtr)(delegate*<byte[], int, uint, NativeBufferContext*, ref BufferDataStager, ulong, void>)&V2WriteSerializeReferenceArray;
            table->srGroupHandler = (IntPtr)(delegate*<ref byte, byte*, int, byte*, NativeBufferContext*, ulong, void>)&V2WriteSerializeReferenceGroup;
            table->entityIdWireBytes = 12;  // LSOI, shared shape with UnityObject
            table->entityIdFieldHandler = (IntPtr)(delegate*<ref byte, byte*, NativeBufferContext*, ulong, void>)&V2WriteEntityIdField;
            table->entityIdArrayHandler = (IntPtr)(delegate*<byte[], int, uint, NativeBufferContext*, ref BufferDataStager, ulong, void>)&V2WriteEntityIdArray;
            table->entityIdGroupHandler = IntPtr.Zero;  // per-entry fallback == v1's per-field codec
            table->simpleNativeTypeHandler = (IntPtr)(delegate*<ref byte, NativeBufferContext*, ref BufferDataStager, ulong, ulong, void>)&V2WriteSimpleNativeType;
            table->nativeValueStructHandler = (IntPtr)(delegate*<ref byte, NativeBufferContext*, ref BufferDataStager, ulong, ulong, void>)&V2WriteNativeValueStruct;
            table->propertyNameHandler = (IntPtr)(delegate*<ref byte, NativeBufferContext*, ref BufferDataStager, ulong, ulong, void>)&V2WritePropertyName;
            table->fixedBufferHandler = (IntPtr)(delegate*<ref byte, NativeBufferContext*, ref BufferDataStager, ulong, ulong, void>)&V2WriteFixedBuffer;
            table->pptrReadGroupHandler = (IntPtr)(delegate*<object, nint, ref byte, byte*, byte*, byte*, int, byte*, NativeReadBufferContext*, ulong, void>)&V2ReadUnityObjectGroup;
            table->pptrReadArrayHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, byte*, byte*, void>)&V2ReadUnityObjectArray;
            table->entityIdReadGroupHandler = (IntPtr)(delegate*<object, nint, ref byte, byte*, byte*, byte*, int, byte*, NativeReadBufferContext*, ulong, void>)&V2ReadEntityIdGroup;
            table->entityIdReadArrayHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, byte*, byte*, void>)&V2ReadEntityIdArray;
            // All backends since RD-4: the SR crossing hands the frame OBJECT
            // over as a GCHandle instead of a raw frame pointer, so it is
            // GC-safe on CoreCLR's moving GC too.
            table->srReadGroupHandler = (IntPtr)(delegate*<object, nint, ref byte, byte*, byte*, byte*, int, byte*, NativeReadBufferContext*, ulong, void>)&V2ReadSerializeReferenceGroup;
            table->simpleNativeTypeReadHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, void>)&V2ReadSimpleNativeType;
            table->nativeValueStructReadHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, void>)&V2ReadNativeValueStruct;
            table->propertyNameReadHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, void>)&V2ReadPropertyName;
            table->fixedBufferReadHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, void>)&V2ReadFixedBuffer;
            s_V2WriteExecutorEntryDelegate = V2WriteExecutorEntry;
            s_V2ReadExecutorEntryDelegate = V2ReadExecutorEntry;
            table->writeExecutorEntry = Marshal.GetFunctionPointerForDelegate(s_V2WriteExecutorEntryDelegate);
            table->readExecutorEntry = Marshal.GetFunctionPointerForDelegate(s_V2ReadExecutorEntryDelegate);
            table->dictionaryElementSourceHandler = (IntPtr)(delegate*<object, ref V2FuidBuilder, NativeBufferContext*, ulong, Array>)&V2DictionaryElementSource;
            table->dictionaryElementSinkHandler = (IntPtr)(delegate*<ref byte, Array, ref V2FuidBuilder, NativeReadBufferContext*, ulong, void>)&V2DictionaryElementSink;
            table->srReadArrayHandler = (IntPtr)(delegate*<ref byte, byte*, NativeReadBufferContext*, byte*, byte*, void>)&V2ReadSerializeReferenceArray;
            s_V2RegisterConstructorDelegate = V2RegisterConstructorEntry;
            table->registerConstructor = Marshal.GetFunctionPointerForDelegate(s_V2RegisterConstructorDelegate);
            s_V2DictionaryExactTypesDelegate = V2GetExactDictionaryFieldTypes;
            table->dictionaryExactTypesHandler = Marshal.GetFunctionPointerForDelegate(s_V2DictionaryExactTypesDelegate);
            s_V2WriteExecutorUnmanagedEntryDelegate = V2WriteExecutorUnmanagedEntry;
            s_V2ReadExecutorUnmanagedEntryDelegate = V2ReadExecutorUnmanagedEntry;
            table->writeExecutorUnmanagedEntry = Marshal.GetFunctionPointerForDelegate(s_V2WriteExecutorUnmanagedEntryDelegate);
            table->readExecutorUnmanagedEntry = Marshal.GetFunctionPointerForDelegate(s_V2ReadExecutorUnmanagedEntryDelegate);
            return table->version;
        }
    }
}
