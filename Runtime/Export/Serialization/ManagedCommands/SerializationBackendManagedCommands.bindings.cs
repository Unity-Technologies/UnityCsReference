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
using Unity.Scripting.Marshalling;
// EntityId lives in namespace UnityEngine (UnityEngineObject.bindings.cs:141). The
// test-resources compile context (UNITY_NATIVE_TEST_RESOURCES) provides a stub in the
// same namespace via Runtime/Testing/ScriptWithManagedRefTestFixture.Resources_cs, so
// the bare `EntityId` field type below resolves identically in both compile contexts.
using UnityEngine;

namespace UnityEngine.Serialization;

// Mirrors NativeBufferContext in SerializationCommands.h. Used by every
// variable-sized managed-execution command (fixed-size DirectCopy segments,
// strings, and any future variable-size payloads).
//
// Contract for the write window (see ManagedCommandWireFormat.h's EnsureWritableFunc
// comment for the canonical description):
//   - C# writes at writerPtr and advances it; [writerBase, writerPtr) is staged and
//     uncommitted. ensureWritable, writeBytesDirect and dispatchNative commit it before
//     doing anything else, and native commits it after the executor returns.
//   - Each re-arm passes minNextWrite, the size the caller is about to write. On return
//     (and at entry to the executor) writerBase == writerPtr, and writerPtr / writerEnd
//     bound a writable region sized for it: the cache writer's tail when it holds
//     minNextWrite (zero-copy), or stackBuffer (kManagedBlockSpillBufferSize bytes, copied
//     into the writer on the next commit).
//   - For minNextWrite <= kManagedBlockSpillBufferSize the region always holds it, so C#
//     can write up to minNextWrite bytes at writerPtr unconditionally. Every caller checks
//     writerEnd - writerPtr first and crosses only when the window is short; a payload
//     that may exceed the spill buffer fills the window and hands the rest to
//     writeBytesDirect.
//
// Pack = 8 keeps EntityId's UInt64 8-byte aligned on every runtime, matching the
// native C++ ABI (EntityId.h:68). Without an explicit Pack, some 32-bit Mono
// configurations reduce the alignment to 4, which would shift hostingEntityId
// four bytes earlier than the native struct on 32-bit and corrupt the per-block
// hostingEntityId reads.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal unsafe struct NativeBufferContext
{
    public void*    writer;            // native CachedWriter* — opaque to C#
    public byte*    stackBuffer;       // native-side spill buffer (size = kManagedBlockSpillBufferSize); stable for the lifetime of the call
    public byte*    writerPtr;         // write cursor — inside the writer's tail or stackBuffer; advanced by C#, reset to writerBase by every re-arm
    public byte*    writerEnd;         // one past the writable region; the region holds the last re-arm's minNextWrite (when that is <= kManagedBlockSpillBufferSize)
    public byte*    writerBase;        // start of the window last handed out; [writerBase, writerPtr) is staged, uncommitted
    public delegate* unmanaged[Cdecl]<NativeBufferContext*, int, void> ensureWritable;                  // (ctx, minNextWrite): commit, re-arm
    public delegate* unmanaged[Cdecl]<NativeBufferContext*, byte*, int, int, int, void> writeBytesDirect; // (ctx, src, n, zeroPadBytes, ensureBytes): commit, stream src, pad, re-arm
    public delegate* unmanaged[Cdecl]<NativeBufferContext*, IntPtr, IntPtr, void> dispatchNative;       // (ctx, dispatchFn, nativePtr): commit, run the ExternalDynamic dispatcher, re-arm
    public IntPtr   resolverHandle;    // ILSOIResolver*; forwarded to WriteUnityObjectToBuffer. Null falls back to the global PersistentManager path.
    public int      flags;             // UnityObjectTransferFlags bits (write path consults PackEntityIdInLSOI).
    public int      _pad;              // pad to 8-byte align fuidContext on 64-bit
    public IntPtr   fuidContext;       // native FieldUniqueIdentifierContext*; forwarded to DictionaryFieldUniqueIdentifierStack.Push/PopDictionaryFUIDFrame. IntPtr.Zero when no transfer-side context is active.
    public EntityId hostingEntityId;   // Resolved once per managed block by the native dispatcher (FUID context's value first, falling back to TryGetHostingEntityIdForUnityObject in editor). EntityId.None when neither yields a value.
    public IntPtr   transferState;     // native ManagedReferencesTransferState*; forwarded to WriteManagedReferencesToBuffer for [SerializeReference] sites. IntPtr.Zero on transfers without managed references.
    public IntPtr   transfer;          // native StreamedBinaryWrite*; opaque to the executor, consumed only by ExternalDynamic dispatchers that Transfer() through it.
    public byte*    pathSegments;      // template FUID segment table (V2PathSegment entries); null when the template has no FUID consumers.
    public byte*    pathNames;         // FUID segment name pool; paired with pathSegments.
    public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> registerGatheredRef;          // (transferState, raw managed object pointer)
    public delegate* unmanaged[Cdecl]<IntPtr, byte*, void>  resolveGatheredMissingType;   // (transferState, NUL-terminated identifier); null when no missing types are live
    public ulong    basePosition;      // wire position at transfer start; native-only (alignment asserts), never read here
}

// Read-side mirror of NativeBufferContext. ExecuteV2Read populates this once
// per object and hands it to SerializationBufferToObjectsV2; C# pulls bytes
// through readerPtr/readerEnd, refilling on demand via ensureReadable
// (segment-sized requests) or readBytesDirect (bulk array bodies that bypass
// the spill buffer).
//
// The struct layout must match SerializationCommands.h::NativeReadBufferContext
// exactly. Field order matters: native code reads/writes by offset.
//
// Pack = 8 keeps EntityId's UInt64 8-byte aligned on every runtime, matching the
// native C++ ABI (same reasoning as NativeBufferContext above).
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal unsafe struct NativeReadBufferContext
{
    public void*    reader;            // native CachedReader* — opaque to C#
    public byte*    stackBuffer;       // native-side spill buffer (kManagedBlockSpillBufferSize bytes, the cap on a single ensureReadable request); stable for the lifetime of the call
    public byte*    readerPtr;         // current read source — reader's cache or stackBuffer; advanced by C# as it consumes; reset by ensureReadable
    public byte*    readerEnd;         // one past the readable region at readerPtr; updated by ensureReadable
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, int, void> ensureReadable;
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, byte*, int, int, int, int> readBytesDirect;
    // Rewinds the CachedReader by the unconsumed region and empties the spill window
    // before a SimpleNativeType dispatch reads straight off the CachedReader.
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, void> syncReader;
    public IntPtr   resolverHandle;    // ILSOIResolver*; forwarded to ReadUnityObjectFromBuffer. Null falls back to the global PersistentManager path.
    public int      flags;             // UnityObjectTransferFlags bits forwarded to ReadUnityObjectFromBuffer.
    public bool     warnAboutIgnoredEntries;  // True for serialized-file loads and Object.Instantiate clones; false for Inspector ApplyModifiedProperties and other in-memory transfers.
    public byte     discardCallbackDictSinks;  // V2 read executor only (v1's dispatcher leaves it unset): suppressed-callback transfers discard callback-keyed dictionary sinks. Brackets are handled by stream selection, not a context check.
    public byte     _pad1;
    public byte     _pad2;             // align fuidContext to 8-byte boundary
    public IntPtr   fuidContext;       // native FieldUniqueIdentifierContext*; forwarded to ConsumeDictionaryRead for FUID Push/Pop bracketing. IntPtr.Zero when no transfer-side context is active.
    public EntityId hostingEntityId;   // Resolved once per managed block by the native dispatcher (FUID context's value first, falling back to TryGetHostingEntityIdForUnityObject in editor). EntityId.None when neither yields a value.
    public IntPtr   transferState;     // native ManagedReferencesTransferState*; forwarded to ReadManagedReferenceFromBuffer for the [SerializeReference] inline-RefId read opcode. IntPtr.Zero on transfers without managed references.
    public IntPtr   instance;          // native GeneralMonoObject* (host being read into); forwarded to ReadManagedReferenceFromBuffer for RegisterFixupRequest.
    public IntPtr   registerSrReferenceField;  // V2 read executor only (v1 leaves it zero): native crossing registering one SR site's property path + RefId with the missing-type field map; non-null doubles as the "missing types live" gate for the segment pass.
    public IntPtr   transfer;          // native StreamedBinaryRead*; opaque to the executor, consumed only by ExternalDynamic dispatchers that Transfer() through it.
    public byte*    pathSegments;      // template FUID segment table (V2PathSegment entries); null when the template has no FUID consumers.
    public byte*    pathNames;         // FUID segment name pool; paired with pathSegments.
    public int      readableBytes;     // total bytes from basePosition to the reader's end
    public ulong    basePosition;      // wire position at transfer start; native-only (alignment asserts), never read here
}

[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/WriteUnityObjectToBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/WriteManagedReferenceToBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/ReadUnityObjectFromBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/ReadManagedReferenceFromBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/DictionaryFieldUniqueIdentifierStack.h")]
internal static unsafe partial class SerializationBackendManagedCommands
{
    // IsThreadSafe disables the default serialization-thread guard (the icall
    // is safe — _NoThreadCheck lookup on the native side).
    //
    // [MethodImpl(InternalCall)] is required so the extern resolves in builds
    // where the bindings IL injector does NOT run — specifically the native
    // test image (ExternalCSharpResource compilation). The production IL
    // injector strips this flag and rewrites the body, so it has no effect
    // there.
    // fieldValueRaw is the raw MonoObject* / managed-object pointer loaded
    // from the host's PPtr field. It must be marshalled as IntPtr, not
    // `object`: on Linux Mono, a value obtained from Unsafe.As<byte, object>
    // over pinned-native memory is mangled (replaced with a metadata
    // pointer) when passed through an `object` icall parameter. IntPtr
    // preserves the bits verbatim. The native side reconstructs the
    // ScriptingObjectPtr — see WriteUnityObjectToBuffer.cpp.
    // calli in real builds — shim keeps the call site identical in the native-test image.

    // Write-side icall for [SerializeReference] sites: pops count RefIds off
    // the per-object cursor on transferState, so it reads no field. outputPtr
    // receives count 8-byte SInt64 RefIds.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void WriteManagedReferencesToBuffer(
        IntPtr transferState,
        IntPtr outputPtr,
        int    count);

    // field / fieldParent (from the wire field-table) let the native side stamp
    // the editor fake-null wrapper on resolver-miss; ignored in player builds.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern unsafe object ReadUnityObjectFromBuffer(
        IntPtr resolverHandle,
        IntPtr inputPtr,
        IntPtr klass,
        int flags,
        IntPtr field,
        IntPtr fieldParent);

    // Read-side icall for the RttiDataType.ManagedReference opcode
    // ([SerializeReference] inline RefId). Reads the 8-byte SInt64 RefId from
    // inputPtr, activates the managed-references state so the `references:` blob
    // is read into the registry, and registers a deferred fixup (the existing
    // PerformFixups flow resolves it once the registry blob has been read).
    // transferState / instance are forwarded from NativeReadBufferContext —
    // non-null whenever this opcode is emitted (build only emits it for SR fields
    // in StreamedBinaryRead transfers). fieldOffset is the post-header offset
    // matching the wire format; the icall adds SCRIPTING_OBJECT_HEADERSIZE back
    // before passing to RegisterFixupRequest.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void ReadManagedReferenceFromBuffer(
        IntPtr transferState,
        IntPtr instance,
        int    fieldOffset,
        IntPtr inputPtr);

    // EntityId opcode (LazyLoadReference<T>) leaf codec. Encodes/decodes via the same
    // WriteEntityIdToBuffer / ReadEntityIdFromBuffer the UnityObject path uses
    // (wire-identical), but the resolver arm calls them through these cached pointers —
    // a direct calli, like SimpleNativeType's fnPtr — rather than a per-element icall.
    // The addresses are process-wide constants (no per-type variation, unlike
    // SimpleNativeType), so they are fetched once at type init and live here instead of
    // on the per-block NativeBufferContext. Clone / EntityId.None still pack inline
    // (PackEntityIdIntoLsoi) with no native call at all.
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<ulong, IntPtr, IntPtr, int, void> s_writeEntityIdToBuffer =
        (delegate* unmanaged[Cdecl]<ulong, IntPtr, IntPtr, int, void>)(void*)GetWriteEntityIdToBufferFunctionPointer();
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, ulong> s_readEntityIdFromBuffer =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, ulong>)(void*)GetReadEntityIdFromBufferFunctionPointer();

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetWriteEntityIdToBufferFunctionPointer();

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetReadEntityIdFromBufferFunctionPointer();

    // Real builds only — the native-test image keeps the per-field path.
    // UnityObject writes resolve the EntityId in managed and batch through object-free id codecs (no
    // object crosses to native): scalar copy-group via WriteUnityObjectEntityIdsToBuffer, array via
    // WriteEntityIdsArrayToBuffer with src==output. The old object-reading batch codecs are gone.

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetWriteEntityIdsArrayToBufferFunctionPointer();
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, int, long, IntPtr, void> s_writeEntityIdsArrayToBuffer =
        (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, int, long, IntPtr, void>)(void*)GetWriteEntityIdsArrayToBufferFunctionPointer();

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetWriteEntityIdsToBufferFunctionPointer();
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, IntPtr, int, void> s_writeEntityIdsToBuffer =
        (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, IntPtr, int, void>)(void*)GetWriteEntityIdsToBufferFunctionPointer();

    // GC-safe batched UnityObject write (scalar copy-group): managed pre-writes each resolved
    // EntityId into the low bytes of its output slot; native resolves them in place. Object-free.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetWriteUnityObjectEntityIdsToBufferFunctionPointer();
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, int, void> s_writeUnityObjectEntityIdsToBuffer =
        (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, int, void>)(void*)GetWriteUnityObjectEntityIdsToBufferFunctionPointer();

    // IL2CPP only — CoreCLR's moving GC and non-crossable managed-object return force the per-field path there.

    // EntityId stores values — no write barrier, safe on all runtimes including CoreCLR.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetReadEntityIdsArrayIntoElementsFunctionPointer();
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, int, long, IntPtr, void> s_readEntityIdsArrayIntoElements =
        (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, int, long, IntPtr, void>)(void*)GetReadEntityIdsArrayIntoElementsFunctionPointer();

    // EntityId stores values: safe on all runtimes, no field table needed.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetReadEntityIdsIntoFieldsFunctionPointer();
    [NoAutoStaticsCleanup] // native function pointer resolved once from a native getter, no managed references
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, IntPtr, int, void> s_readEntityIdsIntoFields =
        (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, IntPtr, int, void>)(void*)GetReadEntityIdsIntoFieldsFunctionPointer();

    // FieldUniqueIdentifierContext stack bracketing for dictionary entries.
    // ConsumeDictionary brackets the per-entry walk with these so descendant
    // commands (and the GetDictionaryEntriesForSerialization helper itself,
    // when checking the duplicate-row cache) can resolve the dict's
    // duplicate-storage key via FormatDictionaryFieldUniqueIdentifierForActiveContext.
    //
    // Push returns false when the fixed-capacity native stack is full (depth
    // cap is 64); the dispatcher MUST consult the return value before deciding
    // whether to call Pop, matching the contract that
    // DictionaryFieldUniqueIdentifierStackScope already enforces in C++.
    //
    // Player builds (UNITY_SERIALIZATION_SUPPORT_FIELD_UNIQUE_IDENTIFIER off)
    // get the inline `return false;` / no-op stubs from the native header
    // (DictionaryFieldUniqueIdentifierStack.h:35-36).
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(Name = "PushDictionaryFieldUniqueIdentifierStackFrame", IsFreeFunction = true, IsThreadSafe = true)]
    private static extern bool PushDictionaryFUIDFrame(IntPtr fuidContext);

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(Name = "PopDictionaryFieldUniqueIdentifierStackFrame", IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void PopDictionaryFUIDFrame();

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(Name = "PushFieldUniqueIdentifierArrayIndex", IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void PushFUIDArrayIndex(IntPtr fuidContext, int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(Name = "SetFieldUniqueIdentifierCurrentArrayIndex", IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void SetFUIDCurrentArrayIndex(IntPtr fuidContext, int index);

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(Name = "PopFieldUniqueIdentifierArrayIndex", IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void PopFUIDArrayIndex(IntPtr fuidContext);

    // Read-side helper (ConsumeDictionaryRead). Formats the dict's FUID template
    // against the currently-pushed FUID frame to get the duplicate-storage key
    // (matches what DictionaryField::SetArray does on the legacy path).
    //
    // [FreeFunction] is incompatible with [MethodImpl(InternalCall)] — the
    // BindingsGenerator processes FreeFunction-attributed methods and rejects
    // ones already marked InternalCall. The gate below mirrors the gate on the
    // sole caller (ConsumeDictionaryRead), so the extern declaration is absent
    // in the UNITY_NATIVE_TEST_RESOURCES compile context where the test
    // TestAssembly.dll doesn't run the BindingsGenerator.
    [FreeFunction("DictionaryFieldUniqueIdentifierBindings::FormatDictionaryFieldUniqueIdentifierForActiveContext", IsThreadSafe = true)]
    private static extern string FormatDictionaryFieldUniqueIdentifier(IntPtr dictionaryIdentifierTemplate);

    // Must match the C++ constants in SerializationCommands.h.
    //
    // kManagedBlockSpillBufferSize: size of the native spill buffers
    //   (NativeBufferContext.stackBuffer, NativeReadBufferContext.stackBuffer), handed
    //   back as the window whenever the writer's tail or the reader's block can't hold
    //   the request. It caps a single ensureWritable / ensureReadable request.
    internal const int kManagedBlockSpillBufferSize = 1024;

    // Refills ctx->readerPtr / ctx->readerEnd so at least `needed` bytes
    // are addressable contiguously at the new readerPtr. Caller invariant: only
    // call when readerEnd - readerPtr < needed.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeEnsureReadable(NativeReadBufferContext* ctx, int needed)
    {
        ctx->ensureReadable(ctx, needed);
        if (ctx->readerEnd - ctx->readerPtr < needed)
            ThrowReadOverrun(needed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeSyncReader(NativeReadBufferContext* ctx)
        => ctx->syncReader(ctx);

    // Bulk-stream `n` bytes into `dst` bypassing the spill buffer. Used by
    // linear-collection trivial bodies so large arrays don't chunk through it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeReadBytesDirect(NativeReadBufferContext* ctx,
        byte* dst, int n, int discardBytes, int ensureBytes)
    {
        if (ctx->readBytesDirect(ctx, dst, n, discardBytes, ensureBytes) == 0)
            ThrowReadOverrun(n);
    }

    // A framed length past the object is a layout mismatch, not a corrupt file.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowReadOverrun(int requested)
    {
        throw new InvalidOperationException(
            "Serialized data ended before a read of " + requested + " bytes could complete: the serialized layout does not "
            + "match the type being loaded. Did you #if UNITY_EDITOR a section of your serialized properties in any of your scripts?");
    }

    // Mirrors the layout of System.Collections.Generic.List<T>'s leading
    // instance fields. List<T> uses LayoutKind.Auto, but the CLR (and Mono)
    // place reference fields ahead of value-type fields, so _items (the
    // backing T[] reference) lands at offset 0 of the instance data and
    // _size (the count) lands at offset IntPtr.Size. We declare _items as
    // byte[] so `fixed (byte* p = layout._items)` returns a pointer to the
    // first array element regardless of T (the SZArray pinning helper is
    // identical for every element type).
    private sealed class ListLayout
    {
        // CS0649: fields are never assigned in C# — they take their values
        // from the underlying List<T> instance via Unsafe.As reinterpret.
#pragma warning disable 0649
        public byte[] _items;
        public int    _size;
#pragma warning restore 0649
    }

    // Helper for VRT pinning: Unsafe.As<ObjectWrapper>(obj) reinterprets a
    // child object so `fixed (byte* p = &wrapped.Data)` pins the first byte
    // of its post-header data area (offset zero for the nested entries'
    // fieldOffsets). Avoids a GCHandle.
    private sealed class ObjectWrapper { public byte Data; }

    // Local mirror of UnityEngine.Bindings.SystemReflectionMarshalling.UnmarshalSystemType.
    // Inlined here because this file is also compiled as TestAttributes::
    // ExternalCSharpResource into the native test fixture's auxiliary C#
    // assembly (see ManagedSerializationTestsShared.h), which doesn't have
    // the [VisibleToOtherModules] privilege to reach UnityEngine.Bindings
    // internals. Behaviour matches the BCL helper exactly.
    //
    // The build side stamps the native MethodTable* (via
    // scripting_class_get_type(klass).GetBackendPtr()) into the header's
    // type-handle field. On CoreCLR RuntimeTypeHandle is { RuntimeType m_type; }
    // — a managed reference, NOT a raw IntPtr — so an
    // Unsafe.As<IntPtr, RuntimeTypeHandle> reinterpret produces a bogus handle
    // that Type.GetTypeFromHandle decodes to garbage and crashes
    // Array.CreateInstance / GetUninitializedObject. The supported BCL
    // entry point is RuntimeTypeHandle.FromIntPtr (.NET 5+), but it's not
    // exposed by the netstandard2.1 reference assembly this file builds
    // against. The normal build goes through the shared, allocation-free
    // Unity.Scripting.Marshalling.ReflectionMarshalling; the
    // test-resources compilation can't reference Unity.Scripting, so it
    // resolves FromIntPtr via reflection on first call and caches the
    // resulting delegate.
    //
    // Mono's RuntimeTypeHandle is a single-IntPtr struct, so the reinterpret
    // is correct there with no BCL help.
    //
    // Lazy resolve (vs. static ctor) keeps this class beforefieldinit. Re-binding
    // against the same BCL method after cleanup is idempotent either way.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Type UnmarshalSystemType(IntPtr handlePtr)
    {
        if (handlePtr == IntPtr.Zero)
            return null;
        return ReflectionMarshalling.ResolveType(handlePtr);
    }

    // RuntimeMethodHandle marshalling. Same shape as the RuntimeTypeHandle
    // helper above and for the same reason: on CoreCLR RuntimeMethodHandle is
    // { IRuntimeMethodInfo m_value; } — a managed reference, NOT a raw IntPtr
    // — so an Unsafe.As<IntPtr, RuntimeMethodHandle> reinterpret produces a
    // handle whose m_value is a bogus "managed reference" pointing into
    // runtime metadata. GetFunctionPointer then dispatches the IRuntimeMethodInfo
    // interface call through VSD on that non-object, crashing in
    // VSD_ResolveWorker. The supported BCL entry point is
    // RuntimeMethodHandle.FromIntPtr (.NET 5+), but it's not exposed by the
    // netstandard2.1 reference assembly this file builds against, so the normal
    // build goes through ReflectionMarshalling and the test-resources
    // compilation resolves it via reflection on first call and caches the
    // delegate.
    //
    // Mono's RuntimeMethodHandle is a single-IntPtr struct, so the reinterpret
    // is benign there and the #else arm is correct.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RuntimeMethodHandle UnmarshalRuntimeMethodHandle(IntPtr methodHandleValue)
    {
        return ReflectionMarshalling.GetRuntimeMethodHandle(methodHandleValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T* ConsumeDirectCopyGroup<T>(ref byte* pos, out T* end) where T : unmanaged
    {
        int count = ((DirectCopyGroupHeader*)pos)->count;
        T* entry = (T*)(pos + sizeof(DirectCopyGroupHeader));
        pos = (byte*)(entry + count);
        end = entry + count;
        return entry;
    }

}
