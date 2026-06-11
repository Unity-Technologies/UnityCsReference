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

namespace UnityEngine.Serialization;

// NOTE: This enum must be kept in sync with RttiDataType in
// Runtime/Mono/SerializationBackend_DirectMemoryAccess/SerializationCommands.h.
// The numeric values drive the accumulator's opcode-selection logic, so the
// native side asserts each variant's value with static_assert; this side is
// covered by a runtime enum-sync test.
//
// Layout invariants:
//   - Executed DirectCopy variants occupy 0..13 contiguously.
//   - Compact (2B per entry) at 0..6; large (_L, 8B per entry) at 7..13.
//   - Within each half: DC1, DC2, DC4, DC8, DC2_Unaligned, DC4_Unaligned, DC8_Unaligned.
//   - DirectCopyBlock (build-time only, never in the execution byte stream) sits at 14,
//     immediately after the executed variants. IsDirectCopy / IsCompactDirectCopy /
//     IsLargeDirectCopy intentionally exclude it since their consumers all run on
//     entries pulled from the byte stream.
//   - Non-DirectCopy opcodes shift up by 7 to 15..24.
//
// Divided-offset encoding: aligned DC2/DC4/DC8 variants store offset / N at flush
// time (N = 2/4/8). The execution-side cases below multiply by N before indexing.
// _Unaligned variants and DC1 store raw offsets.
internal enum RttiDataType : byte
{
    // Compact DirectCopy (entry stream: 2B per entry, DirectCopyCompactEntry).
    DirectCopy1             = 0,
    DirectCopy2             = 1,
    DirectCopy4             = 2,
    DirectCopy8             = 3,
    DirectCopy2_Unaligned   = 4,
    DirectCopy4_Unaligned   = 5,
    DirectCopy8_Unaligned   = 6,

    // Large DirectCopy (entry stream: 8B per entry, DirectCopyLargeEntry).
    DirectCopy1_L           = 7,
    DirectCopy2_L           = 8,
    DirectCopy4_L           = 9,
    DirectCopy8_L           = 10,
    DirectCopy2_L_Unaligned = 11,
    DirectCopy4_L_Unaligned = 12,
    DirectCopy8_L_Unaligned = 13,

    // Build-time-only opcode: AppendToManagedBlock decomposes it into DirectCopy8/4
    // entries and it is never written into the execution byte stream.
    DirectCopyBlock         = 14,

    // Non-DirectCopy opcodes.
    String                  = 15,
    Array                   = 16,
    List                    = 17,
    Reference               = 18,
    UnityObject             = 19,
    EntityId                = 20,
    DynamicBuffer           = 21,
    PropertyNameId          = 22,
    SimpleNativeType        = 23,
    ValueReferenceType      = 24,

    // Write-path metadata header emitted at the start of each fixed segment.
    FixedBlockPrefix        = 25,

    // Inline fixed-size buffer field (C# `unsafe fixed T buf[N]`).
    // See ManagedCommandFixedBuffer in SerializationCommands.h for the wire format.
    FixedBuffer             = 26,

    // Build-time-only (native side); never in the byte stream. Mirrored here only to keep the enum in sync.
    Matrix4x4               = 27,

    // ISerializationCallbackReceiver dispatch (see ManagedCommandCallback in
    // SerializationCommands.h). Class variants cast to the interface and call
    // it directly; struct variants invoke via `delegate*<ref byte, void>` calli
    // through a cached entry-point pointer.
    CallOnBeforeSerializeClass   = 28,
    CallOnBeforeSerializeStruct  = 29,
    CallOnAfterDeserializeClass  = 30,
    CallOnAfterDeserializeStruct = 31,

    Unknown                 = 0xFF,
}

// Mirrors native structs in SerializationCommands.h. Natural sequential layout matches
// the native side exactly. DirectCopyGroupHeader is 4 bytes wide so that the entry
// array immediately following it is 4-byte aligned (required by DirectCopyLargeEntry's
// uint fields); _pad exists to make up that width.

internal struct DirectCopyGroupHeader // 4 bytes
{
    public RttiDataType opCode;
    public byte count;
    public ushort _pad;
}

internal struct DirectCopyCompactEntry // 2 bytes
{
    public byte fieldOffset;
    public byte destOffset;
}

internal struct DirectCopyLargeEntry // 8 bytes
{
    public uint fieldOffset;
    public uint destOffset;
}

// Mirrors ManagedCommandUnityObjectEntry in SerializationCommands.h. The
// write entry doesn't carry klass — WriteUnityObjectToBuffer only needs the
// source field's runtime pointer, which the executor reads from the host
// instance via fieldOffset. Layout coincides with DirectCopyLargeEntry (two
// uint32s), but stays a distinct type so the write-side dispatch reads as
// UnityObject rather than DirectCopyLarge.
internal struct UnityObjectWriteEntry // 8 bytes
{
    public uint fieldOffset;
    public uint destOffset;
}

// Mirrors ManagedCommandUnityObjectReadEntry in SerializationCommands.h. The
// read entry carries klass per-entry so a single group can span PPtr fields
// of differing types. Wire size = 8 + sizeof(IntPtr) — 12B on 32-bit, 16B on
// 64-bit — and matches the native struct exactly because klass is the
// trailing field.
//
// Pack = 4 because entries sit immediately after a 4B FBP header in the
// entry stream; without it, the runtime would assume the 8B alignment
// IntPtr requires on 64-bit and read past the actual buffer alignment.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct UnityObjectReadEntry
{
    public uint fieldOffset;
    public uint destOffset;
    public IntPtr klass;
}

// Mirrors UnityObjectTransferFlags in ReadUnityObjectFromBuffer.h. Used by
// both the read and write paths.
internal static class UnityObjectTransferFlags
{
    public const int IsThreadedSerialization              = 1 << 0;
    public const int DontCreateMonoBehaviourScriptWrapper = 1 << 1;
    public const int AllowPPtrRead                        = 1 << 2;
    public const int PackEntityIdInLSOI                   = 1 << 3;
}

// Mirrors ManagedCommandStringEntry in SerializationCommands.h (8 bytes).
// Natural sequential layout matches the native side exactly: no padding is
// inserted (uint8 + 3xbyte + uint32 fits tightly with no holes).
internal unsafe struct ManagedCommandStringEntry  // 8 bytes
{
    public RttiDataType opCode;
    public fixed byte   reserved[3];
    public uint         fieldOffset;
}

// Mirrors ManagedCommandFixedBlockPrefix in SerializationCommands.h. Brackets
// every DC segment as `FBP(N) DCs FBP(0)`. See the native header for the full
// dual-role description.
internal struct ManagedCommandFixedBlockPrefix  // 4 bytes
{
    public RttiDataType opCode;
    public byte         reserved;
    public ushort       payloadSize;
}

// Mirrors ManagedCommandValueReference in SerializationCommands.h. See the
// native header for the wire / executor contract. Body of nestedByteCount
// bytes immediately follows.
internal struct ValueReferenceHeader  // 16 + 2*sizeof(IntPtr) bytes
{
    public RttiDataType opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         classDataSize;     // size of the inner class's data area (instance size - header)
    public uint         nestedByteCount;   // bytes of FBP-bracketed body (DC + optional String entries) that follow
    public IntPtr       runtimeTypeHandle; // Raw runtime type pointer (MonoType* / Il2CppType* / CoreCLR MethodTable*) for the inner class, populated uniformly across backends by the native build side (ResolveRuntimeTypeHandleForVrt, Common.cpp). ConsumeValueReference funnels it through SerializationBackendManagedCommands.UnmarshalSystemType, which reinterprets it as RuntimeTypeHandle on Mono / IL2CPP (single-IntPtr struct → zero-cost) and routes it through RuntimeTypeHandle.FromIntPtr on CoreCLR (resolved lazily via reflection since the netstandard2.1 reference assembly doesn't expose that .NET 5+ API).
    public IntPtr       ctorFunctionPtr;   // Encoding picked by GetConstructorMethodFunctionPointer; zero means no parameterless ctor.
}

// Mirrors ManagedCommandSimpleNativeTypeEntry in SerializationCommands.h.
// 24 bytes on 64-bit (8 + 2*sizeof(IntPtr)). Sequential layout matches the
// native side exactly: no padding inserted.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ManagedCommandSimpleNativeTypeEntry  // 24 bytes (64-bit)
{
    public RttiDataType opCode;
    public byte         reserved;
    public ushort       reserved2;
    public uint         fieldOffset;
    public IntPtr       fnPtr;
    public IntPtr       userData;
}

// Mirrors ManagedCommandCallback in SerializationCommands.h. Emitted at the
// parent command-stream level; fieldOffset locates the inner object reference
// (class variants) or struct data (struct variants) on the parent's base.
// methodFnPtr is the JIT/AOT entry-point pointer the executor invokes via
// `delegate*<ref byte, void>` calli for the struct opcodes; ignored for the
// class opcodes (which dispatch via interface cast).
internal struct CallbackHeader  // 8 + sizeof(IntPtr) bytes
{
    public RttiDataType opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public IntPtr       methodFnPtr;
}

// Mirrors ManagedCommandLinearCollection in SerializationCommands.h. See the
// native header for the wire / executor contract. Body of nestedByteCount
// bytes immediately follows (empty when flags has bit 0 set).
internal struct LinearCollectionHeader  // 24 + sizeof(IntPtr) bytes
{
    public RttiDataType opCode;            // = RttiDataType.Array or RttiDataType.List
    public byte         kind;              // 0 = Array, 1 = List
    public byte         flags;             // bit 0 = elementIsTriviallyCopyable, bit 1 = elementShufflePath
    public byte         reserved;
    public uint         fieldOffset;       // post-header offset of the collection reference on the parent
    public uint         elementStride;     // bytes between elements in the managed array
    public uint         elementWireSize;   // per-element wire bytes the recursion emits (0 in the trivial path)
    public uint         nestedByteCount;   // bytes of FBP-bracketed body that follow (0 in the trivial path)
    public uint         reserved2;         // pad to align elementTypeHandle on an 8-byte boundary
    public IntPtr       elementTypeHandle; // RuntimeTypeHandle.Value of the element type; consumed by ConsumeLinearCollectionRead
}

internal static class LinearCollectionKind
{
    public const byte Array = 0;
    public const byte List  = 1;
}

// Mirrors ManagedCommandFixedBuffer in SerializationCommands.h — see that
// header for the wire / executor contract.
internal struct FixedBufferHeader  // 12 bytes
{
    public RttiDataType opCode;       // = RttiDataType.FixedBuffer
    public byte         reserved;
    public ushort       elementSize;  // 1 / 2 / 4 / 8 — element width
    public uint         fieldOffset;  // post-header offset of the buffer struct on the parent
    public uint         elementCount; // compile-time count; total payload bytes = elementCount * elementSize
}

internal static class LinearCollectionFlags
{
    public const byte TriviallyCopyable = 1 << 0;
    // Body is FBP-bracketed and contains only DC entries (no String / Array / VRT / etc.),
    // and elementWireSize fits in a single segment. The C# consumer reserves
    // count*elementWireSize bytes in one shot and walks the DC entries once per element
    // with a fixed per-element destination, skipping the per-element FBP segment claim
    // and ExecuteWriteCommands recursion. Wire output is byte-identical to the
    // per-element recursion path. See ConsumeLinearCollectionShufflePath.
    public const byte ShufflePath      = 1 << 1;
}

// Mirrors NativeBufferContext in SerializationCommands.h. Used by every
// variable-sized managed-execution command (fixed-size DirectCopy segments,
// strings, and any future variable-size payloads).
//
// Contract for writerPtr / writerAvailable (see SerializationCommands.h's
// FlushBufferFunc comment for the canonical description):
//   - At entry to the C# executor and after every flushBuffer call, writerPtr
//     points at a writable region of writerAvailable bytes, and writerAvailable
//     is always >= kManagedBlockMaxPayloadSize. The region is either the cache
//     writer's tail (when it has at least that much remaining; zero-copy fast
//     path) or stackBuffer (sized kManagedBlockSpillBufferSize; native side
//     memcpys it in on the next flush).
//   - C# can therefore write up to writerAvailable bytes into writerPtr
//     unconditionally, with no per-site stack-vs-writer branching.
//
internal unsafe struct NativeBufferContext
{
    public void*  writer;            // native CachedWriter* — opaque to C#
    public byte*  stackBuffer;       // native-side spill buffer (size = kManagedBlockSpillBufferSize); stable for the lifetime of the call
    public byte*  writerPtr;         // current write destination — writer's tail or stackBuffer; updated by flushBuffer
    public int    writerAvailable;   // bytes available at writerPtr; updated by flushBuffer; always >= kManagedBlockMaxPayloadSize after a flush / initial setup
    public delegate* unmanaged[Cdecl]<NativeBufferContext*, byte*, int, void> flushBuffer;
    public IntPtr resolverHandle;    // ILSOIResolver*; forwarded to WriteUnityObjectToBuffer. Null falls back to the global PersistentManager path.
    public int    flags;             // UnityObjectTransferFlags bits (write path consults PackEntityIdInLSOI).
}

// Read-side mirror of NativeBufferContext. The C++ dispatcher
// (Transfer_ManagedBlock_StreamedBinaryRead) populates this once per managed
// block and hands it to SerializationBufferToObject; C# walks the entry stream
// and pulls bytes through readerPtr/readerAvailable, refilling on demand via
// ensureReadable (segment-sized requests) or readBytesDirect (bulk array bodies
// that bypass the spill buffer).
//
// The struct layout must match SerializationCommands.h::NativeReadBufferContext
// exactly. Field order matters: native code reads/writes by offset.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeReadBufferContext
{
    public void*  reader;            // native CachedReader* — opaque to C#
    public byte*  stackBuffer;       // native-side spill buffer (size = stackBufferSize); stable for the lifetime of the call
    public byte*  readerPtr;         // current read source — reader's cache or stackBuffer; updated by ensureReadable
    public int    readerAvailable;   // bytes available at readerPtr; decremented by C# as it consumes; refilled by ensureReadable
    public int    stackBufferSize;   // size of stackBuffer; cap on a single ensureReadable request
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, int, void> ensureReadable;
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, byte*, int, void> readBytesDirect;
    public IntPtr resolverHandle;    // ILSOIResolver*; forwarded to ReadUnityObjectFromBuffer. Null falls back to the global PersistentManager path.
    public int    flags;             // UnityObjectTransferFlags bits forwarded to ReadUnityObjectFromBuffer.
}

[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/WriteUnityObjectToBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/ReadUnityObjectFromBuffer.h")]
internal static unsafe class SerializationBackendManagedCommands
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
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void WriteUnityObjectToBuffer(
        IntPtr fieldValueRaw,
        IntPtr resolverHandle,
        IntPtr outputPtr,
        int flags);

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern object ReadUnityObjectFromBuffer(
        IntPtr resolverHandle,
        IntPtr inputPtr,
        IntPtr klass,
        int flags);

    // Must match the C++ constants in SerializationCommands.h.
    //
    // kManagedBlockMaxPayloadSize: cap on a single FBP-bracketed segment, and
    //   the post-condition floor on ctx->writerAvailable after any flushBuffer
    //   call. Any single segment / string chunk / array chunk that respects
    //   this cap is guaranteed to fit at ctx->writerPtr without re-checking.
    // kManagedBlockSpillBufferSize: size of the stack-allocated spill buffer
    //   on the native side (NativeBufferContext.stackBuffer). FlushBuffer hands
    //   this back as the writable region whenever the cache writer's tail has
    //   < kManagedBlockMaxPayloadSize bytes remaining. Sized equal to the
    //   segment cap: exactly one segment per spill flush.
    private const int kManagedBlockMaxPayloadSize  = 1024;
    private const int kManagedBlockSpillBufferSize = 1024;

    // Cached UTF-8 encoder used by ConsumeString's chunked-flush path. Allocated
    // lazily per thread on first use and reused across calls via Reset(); this
    // keeps the hot path on managed serialization allocation-free for strings
    // that need more than one buffer flush. Strings that fit the current buffer
    // tail in one shot bypass the encoder entirely (see ConsumeString).
    [ThreadStatic]
    private static Encoder s_Utf8Encoder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeFlushBuffer(NativeBufferContext* ctx,
        byte* bufferUsed, int writtenBytes)
        => ctx->flushBuffer(ctx, bufferUsed, writtenBytes);

    // Refills ctx->readerPtr / ctx->readerAvailable so at least `needed` bytes
    // are addressable contiguously at the new readerPtr. Caller invariant: only
    // call when ctx->readerAvailable < needed.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeEnsureReadable(NativeReadBufferContext* ctx, int needed)
        => ctx->ensureReadable(ctx, needed);

    // Bulk-stream `n` bytes into `dst` bypassing the spill buffer. Used by
    // linear-collection trivial bodies so large arrays don't chunk through it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeReadBytesDirect(NativeReadBufferContext* ctx,
        byte* dst, int n)
        => ctx->readBytesDirect(ctx, dst, n);

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
    // against, so we resolve it via reflection on first call and cache the
    // resulting delegate.
    //
    // Mono's RuntimeTypeHandle is a single-IntPtr struct, so the reinterpret
    // is correct there with no BCL help.
    //
    // Lazy resolve (vs. static ctor) keeps this class beforefieldinit and
    // sidesteps the need for [NoAutoStaticsCleanup] (also an
    // UnityEngine.Bindings-adjacent attribute that the test resource compile
    // may not see). If the cached delegate is cleared by an auto-cleanup pass,
    // the next call falls through to ResolveRuntimeTypeHandleFromIntPtr and
    // re-binds against the same BCL method — idempotent.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type UnmarshalSystemType(IntPtr handlePtr)
    {
        if (handlePtr == IntPtr.Zero)
            return null;
        return Type.GetTypeFromHandle(
            Unsafe.As<IntPtr, RuntimeTypeHandle>(ref handlePtr));
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

    // The native writer (FlushManagedBlockToCommandQueue in ManagedBlockAccumulator.h)
    // keeps every DirectCopyGroupHeader at a 4-byte-aligned offset relative to entriesPtr,
    // so each DirectCopyLargeEntry array has the alignment its uint fields need. The loop
    // below re-aligns pos to a 4-byte boundary after consuming each group to match.
    //
    // Source-side reads (baseAddr + entry.fieldOffset) can still be unaligned since managed
    // class layout is outside this writer's control; they are handled separately.
    [RequiredByNativeCode]
    public static unsafe int ObjectToSerializationBuffer(
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int    entryBufferSize,
        IntPtr bufferContext,
        IntPtr transfer)
    {
        var ctx = (NativeBufferContext*)bufferContext;

        byte* output         = null;
        int   dstSize        = 0;
        // Bytes written directly into the writer's current block but not yet committed
        // via InvokeFlushBuffer. Threaded by ref through ExecuteWriteCommands and any
        // recursion (VRT bodies, per-element collection loops) so consecutive segments
        // coalesce into a single flush, regardless of nesting depth. The build side
        // brackets every fixed-size segment with FBP(N)..FBP(0); FBP(0) rolls dstSize
        // into pendingAdvance, so by the time we return here pendingAdvance carries
        // every closed-but-uncommitted segment from the entire stream.
        int   pendingAdvance = 0;
        ExecuteWriteCommands(ctx, pinnedBase, entriesPtr, entryBufferSize, transfer,
            ref output, ref dstSize, ref pendingAdvance);

        // Single trailing commit. pendingAdvance is the common case; dstSize > 0
        // only fires if the build side ever omits the closing FBP(0) — keep the
        // arm as a safety net.
        if (pendingAdvance > 0)
            InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
        if (dstSize > 0)
            InvokeFlushBuffer(ctx, output, dstSize);

        return 0;
    }

    // Walks the entries between [entriesPtr, entriesPtr + entryBufferSize),
    // executing each opcode against the pinned managed object (source) and
    // the buffer chain owned by ctx (destination). Buffer state (output /
    // dstSize / pendingAdvance) is threaded by ref so the inline FBP /
    // String / VRT cases can claim and commit per-segment destinations,
    // and so recursive calls (per-element collection bodies, VRT bodies)
    // accumulate writer-tail bytes across iterations instead of flushing
    // on every return.
    //
    // pendingAdvance is owned by the outermost caller (ObjectToSerializationBuffer)
    // which initializes it to 0 and issues the single final flush after this
    // method returns. Inner recursive callers must not flush it on return —
    // doing so would emit one P/Invoke per nested element/instance, which is
    // exactly what threading by ref avoids.
    private static unsafe void ExecuteWriteCommands(
        NativeBufferContext* ctx,
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int    entryBufferSize,
        IntPtr transfer,
        ref byte* output,
        ref int   dstSize,
        ref int   pendingAdvance)
    {
        ref byte baseAddr = ref Unsafe.AsRef<byte>((void*)pinnedBase);
        byte* pos = (byte*)entriesPtr;
        byte* endPos = pos + entryBufferSize;

        while (pos < endPos)
        {
            var opCode = (RttiDataType)pos[0];

            // Aligned DC2/DC4/DC8 variants store offset / N; the execution side multiplies by N
            // and uses direct typed reads (Unsafe.As<byte, T>(ref ...)) -- single mov on Mono
            // and CoreCLR. _Unaligned variants store the raw offset and use Unsafe.ReadUnaligned /
            // WriteUnaligned because the typed reads would fault on strict-alignment (ARM) targets
            // when the field is not N-aligned (e.g. StructLayout.Pack=1, LayoutKind.Explicit).
            switch (opCode)
            {
                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        // Destination is always 4-byte aligned; widen the 1B source into an int store.
                        *(int*)(output + entry->destOffset) = Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset = (nint)entry->destOffset * 2;
                        *(int*)(output + destOffset) = Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset = (nint)entry->destOffset * 4;
                        *(int*)(output + destOffset) = Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset = (nint)entry->destOffset * 8;
                        // Unaligned: SelectDirectCopyOpCode's destOffset%8 gate only proves
                        // segment-relative alignment, not absolute address alignment. The
                        // segment base (output) can be 4-byte aligned (e.g. inside per-element
                        // bodies after a linear collection's 4B count prefix), which would
                        // SIGBUS on armv7 with a typed 8B store. Build-side fix pending.
                        Unsafe.WriteUnaligned(output + destOffset, Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, (int)Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        *(int*)(output + entry->destOffset) = Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset = entry->destOffset * 2;
                        *(int*)(output + destOffset) = Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset = entry->destOffset * 4;
                        *(int*)(output + destOffset) = Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset = entry->destOffset * 8;
                        // Unaligned: see DirectCopy8 above for the segment-base alignment caveat.
                        Unsafe.WriteUnaligned(output + destOffset, Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, (int)Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.FixedBlockPrefix:
                {
                    var prefix = (ManagedCommandFixedBlockPrefix*)pos;
                    int segmentSize = prefix->payloadSize;
                    pos += sizeof(ManagedCommandFixedBlockPrefix);

                    // Trailing FBP(0): close the prior segment by rolling its bytes
                    // into pendingAdvance. We do NOT flush here — flushes happen only
                    // when the next segment won't fit alongside what's been deferred,
                    // when variable-sized data takes over, or at end of execution.
                    if (dstSize > 0)
                    {
                        pendingAdvance += dstSize;
                        dstSize = 0;
                    }

                    if (segmentSize == 0)
                        break;

                    // FBP(N>0): claim the next segment. Flush only when it doesn't fit
                    // alongside the already-deferred bytes. After any flush the
                    // FlushBuffer contract guarantees ctx->writerAvailable >=
                    // kManagedBlockMaxPayloadSize >= segmentSize, so the segment is
                    // always claimable at writerPtr (offset 0) afterward.
                    if (ctx->writerAvailable - pendingAdvance < segmentSize)
                    {
                        if (pendingAdvance > 0)
                        {
                            InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                            pendingAdvance = 0;
                        }
                    }
                    output  = ctx->writerPtr + pendingAdvance;
                    dstSize = segmentSize;
                    break;
                }

                // dst is 4-byte aligned but not 8-byte, so the null-LSOI
                // pathID write below uses WriteUnaligned to stay UB-free.
                case RttiDataType.UnityObject:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectWriteEntry>(ref pos, out var end);
                    do
                    {
                        // Read the field as a raw pointer rather than going through
                        // `object`. See the WriteUnityObjectToBuffer declaration above
                        // for why `object` marshalling is unsafe here on Linux Mono.
                        ref byte fieldByteRef = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                        IntPtr fieldValueRaw = Unsafe.ReadUnaligned<IntPtr>(ref fieldByteRef);
                        byte* dst = output + entry->destOffset;

                        if (fieldValueRaw == IntPtr.Zero)
                        {
                            Unsafe.As<byte, int>(ref *dst) = 0;
                            Unsafe.WriteUnaligned<long>(ref *(dst + 4), 0L);
                        }
                        else
                        {
                            WriteUnityObjectToBuffer(fieldValueRaw, ctx->resolverHandle, (IntPtr)dst, ctx->flags);
                        }
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.DirectCopyBlock:
                    // DirectCopyBlock is a build-time-only opcode: the accumulator
                    // (AppendToManagedBlock in ManagedBlockAccumulator.h) decomposes it
                    // into DirectCopy8 / DirectCopy4 entries before flushing. Reaching it
                    // here means the builder failed to decompose, or a producer is writing
                    // raw DirectCopyBlock entries into the byte stream — both are bugs.
                    throw new InvalidOperationException(
                        "DirectCopyBlock should never appear in the executed byte stream; "
                        + "the accumulator decomposes it into DirectCopy{4,8} entries.");

                case RttiDataType.String:
                    // FBP(0) precedes every String, so dstSize == 0 and
                    // pendingAdvance may be non-zero. Flush both before the string
                    // takes over the writer.
                    if (pendingAdvance > 0)
                    {
                        InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                        pendingAdvance = 0;
                    }
                    if (dstSize > 0)
                    {
                        InvokeFlushBuffer(ctx, output, dstSize);
                        dstSize = 0;
                    }
                    ConsumeString(ctx, ref baseAddr, ref pos);
                    break;

                case RttiDataType.ValueReferenceType:
                    // Build emits FBP(0) before every VRT header, so dstSize == 0
                    // here. pendingAdvance is threaded into the inner body so
                    // bytes already deferred at the writer's tail survive the
                    // recursion — the inner FBP(N) handler opens its segment
                    // at writerPtr + pendingAdvance and only flushes when the
                    // combined region wouldn't fit. Net result: a class with
                    // many small VRT children coalesces into a single flush.
                    ConsumeValueReference(ctx, ref baseAddr, transfer, ref output, ref dstSize, ref pendingAdvance, ref pos);
                    break;

                case RttiDataType.SimpleNativeType:
                {
                    var entry = (ManagedCommandSimpleNativeTypeEntry*)pos;
                    pos += sizeof(ManagedCommandSimpleNativeTypeEntry);

                    if (pendingAdvance > 0)
                    {
                        InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                        pendingAdvance = 0;
                    }
                    if (dstSize > 0)
                    {
                        InvokeFlushBuffer(ctx, output, dstSize);
                        dstSize = 0;
                    }

                    ref byte field = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);

                    // entry->userData holds the post-header byte offset of m_Ptr within the
                    // wrapper, computed by the C++ initialiser via scripting_field_get_offset so
                    // it is correct for the active scripting backend (Mono preserves declaration
                    // order; CoreCLR may reorder fields, e.g. placing m_SourceStyle before m_Ptr).
                    object wrapper = Unsafe.As<byte, object>(ref field);

                    IntPtr nativePtr = wrapper != null
                        ? Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
                            ref Unsafe.As<ObjectWrapper>(wrapper).Data,
                            entry->userData))
                        : IntPtr.Zero;

                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)entry->fnPtr)(nativePtr, transfer, entry->userData);

                    InvokeFlushBuffer(ctx, ctx->writerPtr, 0);
                    break;
                }

                // Callbacks emit no wire bytes; their wrapping FBP(0)..FBP(0)
                // makes the surrounding payload-size budget trivial, so no
                // flush/buffer bookkeeping is needed here.
                case RttiDataType.CallOnBeforeSerializeClass:
                {
                    var header = (CallbackHeader*)pos;
                    pos += sizeof(CallbackHeader);
                    object target = Unsafe.As<byte, object>(
                        ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));
                    if (target is ISerializationCallbackReceiver receiver)
                        receiver.OnBeforeSerialize();
                    break;
                }

                case RttiDataType.CallOnBeforeSerializeStruct:
                {
                    var header = (CallbackHeader*)pos;
                    pos += sizeof(CallbackHeader);
                    if (header->methodFnPtr != IntPtr.Zero)
                    {
                        ref byte structData = ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset);
                        ((delegate*<ref byte, void>)header->methodFnPtr)(ref structData);
                    }
                    break;
                }

                case RttiDataType.Array:
                case RttiDataType.List:
                    // Build emits FBP(0) before every linear-collection header,
                    // so dstSize == 0 here. The collection's own writes (the
                    // SInt32 count, the trivially-copyable bulk body, the tail
                    // pad) all land at writerPtr — so any deferred bytes there
                    // would be clobbered. Flush before handing off; the per-
                    // element recursion path then re-accumulates pendingAdvance
                    // across elements via the same ref.
                    if (pendingAdvance > 0)
                    {
                        InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                        pendingAdvance = 0;
                    }
                    ConsumeLinearCollection(ctx, ref baseAddr, transfer, ref output, ref dstSize, ref pendingAdvance, ref pos);
                    break;

                case RttiDataType.FixedBuffer:
                    // Build emits FBP(0) before every FixedBuffer header, so dstSize == 0
                    if (dstSize != 0)
                        throw new InvalidOperationException("FixedBuffer must be preceded by FBP(0) — see AppendFixedBufferToManagedBlock.");
                    // Flush deferred bytes before the consumer writes through writerPtr.
                    if (pendingAdvance > 0)
                    {
                        InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                        pendingAdvance = 0;
                    }
                    ConsumeFixedBuffer(ctx, ref baseAddr, ref pos);
                    break;

                case RttiDataType.Reference:
                case RttiDataType.EntityId:
                case RttiDataType.DynamicBuffer:
                case RttiDataType.PropertyNameId:
                case RttiDataType.Unknown:
                    throw new NotSupportedException(
                        $"OpCode {(RttiDataType)pos[0]} is not implemented for managed command blocks.");
                default:
                    throw new NotSupportedException($"OpCode {(RttiDataType)pos[0]} not supported");
            }

            // Re-align pos to a 4-byte offset relative to entriesPtr before reading the
            // next header. Only compact groups with an odd entry count need this
            // (2-byte skip); large groups are already a multiple of 4.
            long entryOffset = pos - (byte*)entriesPtr;
            long aligned = (entryOffset + 3) & ~3L;
            pos = (byte*)entriesPtr + aligned;
        }

        // No trailing flush here. pendingAdvance is owned by the outermost caller
        // (ObjectToSerializationBuffer), which commits it once after its top-level
        // ExecuteWriteCommands returns. Inner recursive callers (ConsumeValueReference,
        // ConsumeLinearCollection's per-element loop) intentionally inherit any
        // accumulated bytes so consecutive elements / nested instances coalesce
        // into the same flush.
    }

    // Writes a string (length prefix + UTF-8 body + 4-byte alignment pad) into the
    // writable region. The flushBuffer contract guarantees writerAvailable is at
    // least kManagedBlockMaxPayloadSize on entry and after each flush, so we always
    // write into ctx->writerPtr — no per-site stack-vs-writer branching.
    //
    // Two paths:
    //   - Fast path: the whole framed payload fits in the current writable region.
    //     One direct write, one flush, no Encoder needed.
    //   - Chunked path: stream codepoints into the region in chunks, flushing
    //     when the encoder fills the available space.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumeString(
        NativeBufferContext* ctx, ref byte baseAddr, ref byte* pos)
    {
        var entry = (ManagedCommandStringEntry*)pos;
        pos += sizeof(ManagedCommandStringEntry);
        // Field offset points at a managed string reference inside the pinned object;
        // Unsafe.As<byte, string> reinterprets that ref as a string ref.
        string str = Unsafe.As<byte, string>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)) ?? string.Empty;

        // Truncate at first '\0' before byte counting so the length prefix,
        // body, and padding agree on the same effective string. Matches the
        // strlen-based write contract: bytes past an embedded null are dropped.
        ReadOnlySpan<char> chars = str.AsSpan();
        int nullIdx = chars.IndexOf('\0');
        if (nullIdx >= 0)
            chars = chars.Slice(0, nullIdx);

        // Computed up front because it goes into the 4-byte SInt32 length prefix and
        // also drives the fast-path / chunked-path decision.
        int totalByteCount = Encoding.UTF8.GetByteCount(chars);
        int padBytes = (4 - (totalByteCount & 3)) & 3;
        int totalFramedSize = 4 + totalByteCount + padBytes;

        // Fast path: whole framed payload fits in the current writable region.
        if (ctx->writerAvailable >= totalFramedSize)
        {
            byte* dst = ctx->writerPtr;
            Unsafe.WriteUnaligned(dst, totalByteCount);
            if (totalByteCount > 0)
                Encoding.UTF8.GetBytes(chars, new Span<byte>(dst + 4, totalByteCount));
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(dst + 4 + totalByteCount, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, dst, totalFramedSize);
            return;
        }

        // Chunked path. Length header first.
        WriteFramedInt32(ctx, totalByteCount);

        // Body: stream chunk by chunk into the current writable region. After each
        // flush the contract guarantees a fresh region of at least
        // kManagedBlockMaxPayloadSize bytes (≥ 4, so even a worst-case 4-byte UTF-8
        // codepoint always fits).
        if (totalByteCount > 0)
        {
            // Pass flush: false while input remains so the encoder can hold a
            // high surrogate across chunks until the matching low surrogate
            // arrives in the next call. Setting flush: true mid-stream would
            // emit a U+FFFD replacement for the held high surrogate and then
            // another U+FFFD for the orphan low surrogate in the next chunk —
            // corrupting any surrogate pair that happens to straddle a chunk.
            Encoder encoder = s_Utf8Encoder ??= Encoding.UTF8.GetEncoder();
            encoder.Reset();
            ReadOnlySpan<char> remaining = chars;

            while (!remaining.IsEmpty)
            {
                encoder.Convert(remaining, new Span<byte>(ctx->writerPtr, ctx->writerAvailable),
                                flush: false,
                                out int charsUsed, out int bytesUsed, out _);
                if (bytesUsed > 0)
                    InvokeFlushBuffer(ctx, ctx->writerPtr, bytesUsed);
                remaining = remaining.Slice(charsUsed);
            }

            // Drain encoder state with a final flush:true call. For valid input this
            // is a no-op; for an unpaired high surrogate it emits a 3-byte
            // replacement character (U+FFFD).
            {
                encoder.Convert(ReadOnlySpan<char>.Empty, new Span<byte>(ctx->writerPtr, ctx->writerAvailable),
                                flush: true,
                                out _, out int bytesUsed, out _);
                if (bytesUsed > 0)
                    InvokeFlushBuffer(ctx, ctx->writerPtr, bytesUsed);
            }
        }

        // Alignment padding (0–3 zero bytes). The contract guarantees
        // writerAvailable >= kManagedBlockMaxPayloadSize >= 3 here, so the pad
        // always fits in the current region.
        if (padBytes > 0)
        {
            Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
        }
    }

    // Writes a 4-byte int32 into the writable region. The flushBuffer contract
    // guarantees writerAvailable >= kManagedBlockMaxPayloadSize >= 4 on entry
    // here (every caller has just flushed or is at start-of-execution), so this
    // is unconditionally a single direct write + flush — no spill arm needed.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteFramedInt32(NativeBufferContext* ctx, int value)
    {
        Unsafe.WriteUnaligned(ctx->writerPtr, value);
        InvokeFlushBuffer(ctx, ctx->writerPtr, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe object GetOrCreateVrtInstance(
        ref byte baseAddr, ValueReferenceHeader* header)
    {
        ref object slot = ref Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
        object obj = slot;
        if (obj != null)
            return obj;

        Type type = UnmarshalSystemType(header->runtimeTypeHandle);
        obj = RuntimeHelpers.GetUninitializedObject(type);
        if (header->ctorFunctionPtr != IntPtr.Zero)
        {
            try
            {
                ((delegate*<object, void>)header->ctorFunctionPtr)(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        slot = obj;
        return obj;
    }

    // Consumes a ValueReferenceType entry: resolves (or allocates) the
    // inner instance, then recurses into ExecuteWriteCommands with that
    // instance pinned as the source. The body's own FBP(N)..FBP(0)
    // bracketing drives buffer claims and flushes.
    private static unsafe void ConsumeValueReference(
        NativeBufferContext* ctx, ref byte baseAddr, IntPtr transfer,
        ref byte* output, ref int dstSize, ref int pendingAdvance, ref byte* pos)
    {
        var header = (ValueReferenceHeader*)pos;
        pos += sizeof(ValueReferenceHeader);
        byte* nestedStart = pos;
        int nestedBytes = (int)header->nestedByteCount;

        if (nestedBytes == 0)
        {
            pos = nestedStart;
            return;
        }

        object obj = GetOrCreateVrtInstance(ref baseAddr, header);

        // ObjectWrapper.Data is the offset-zero reference for the nested
        // entries' fieldOffsets.
        fixed (byte* nestedBase = &Unsafe.As<ObjectWrapper>(obj).Data)
        {
            ExecuteWriteCommands(ctx, (IntPtr)nestedBase,
                (IntPtr)nestedStart, nestedBytes, transfer, ref output, ref dstSize, ref pendingAdvance);
        }

        pos = nestedStart + nestedBytes;
    }

    // TODO: replace the reinterpret with RuntimeMethodHandle.FromIntPtr
    // (.NET 5+) once UnityEngine.dll's reference assembly moves past
    // netstandard2.1.
    [RequiredByNativeCode]
    internal static IntPtr GetConstructorMethodFunctionPointer(IntPtr methodHandleValue)
    {
        var handle = Unsafe.As<IntPtr, RuntimeMethodHandle>(ref methodHandleValue);
        RuntimeHelpers.PrepareMethod(handle);
        return handle.GetFunctionPointer();
    }

    // Generic method-handle to function-pointer resolver used by callsites that
    // dispatch any method (not just ctors) via calli — currently the interface
    // method lookup behind CallOn{Before,After}{Class,Struct} struct callbacks.
    [RequiredByNativeCode]
    internal static IntPtr GetMethodFunctionPointer(IntPtr methodHandleValue)
    {
        var handle = Unsafe.As<IntPtr, RuntimeMethodHandle>(ref methodHandleValue);
        RuntimeHelpers.PrepareMethod(handle);
        return handle.GetFunctionPointer();
    }

    // Consumes one ManagedCommandLinearCollection entry. Three paths share
    // the same header + length prefix, then diverge:
    //   - Trivially-copyable: count*elementStride raw bytes streamed in
    //     one or more chunks, plus a 0..3 byte tail pad. Wire format
    //     matches the legacy native Transfer_Blittable_ArrayField.
    //   - Shuffle path: per-element body is purely DC + FBP and fits in
    //     a single segment. Reserves count*elementWireSize in batches
    //     sized to the writable region, runs the DC entries once per
    //     element against a fixed per-element destination — no per-element
    //     ExecuteWriteCommands frame, no per-element FBP segment claim.
    //     Wire output is byte-identical to the per-element recursion path.
    //   - Per-element recursion (general fallback): nestedByteCount bytes
    //     of FBP-bracketed body executed once per element via
    //     ExecuteWriteCommands with an element-pinned base.
    //
    // Null array / null List → write a 0 length prefix and return; matches
    // the legacy auto-empty-on-write behavior in TransferField_LinearCollection.
    private static unsafe void ConsumeLinearCollection(
        NativeBufferContext* ctx, ref byte baseAddr, IntPtr transfer,
        ref byte* output, ref int dstSize, ref int pendingAdvance, ref byte* pos)
    {
        var header = (LinearCollectionHeader*)pos;
        pos += sizeof(LinearCollectionHeader);
        byte* nestedStart = pos;
        int   nestedBytes = (int)header->nestedByteCount;

        // Resolve the underlying byte[] for pinning (any T[] is pinnable as
        // byte[] — the SZArray pinning helper computes the same data offset
        // regardless of element type) and the element count.
        byte[] dataAsBytes;
        int    count;
        if (header->kind == LinearCollectionKind.Array)
        {
            // Field at (baseAddr + fieldOffset) holds a T[] reference.
            Array arr = Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            if (arr == null)
            {
                WriteFramedInt32(ctx, 0);
                pos = nestedStart + nestedBytes;
                return;
            }
            dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);
            count       = arr.Length;
        }
        else
        {
            // Field at (baseAddr + fieldOffset) holds a List<T> reference.
            // Reinterpret as ListLayout to read _items + _size in one shot.
            ListLayout list = Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            if (list == null || list._items == null)
            {
                WriteFramedInt32(ctx, 0);
                pos = nestedStart + nestedBytes;
                return;
            }
            dataAsBytes = list._items;
            count       = list._size;
        }

        if ((header->flags & LinearCollectionFlags.TriviallyCopyable) != 0)
        {
            long  totalBytesL = (long)count * (long)header->elementStride;
            // The legacy reader walks an SInt32 length then count*elementStride
            // raw bytes; element counts above int.MaxValue are not representable.
            int   totalBytes  = checked((int)totalBytesL);
            int   padBytes    = (4 - (totalBytes & 3)) & 3;

            if (totalBytes == 0)
            {
                // Empty array: just write the count (0). No element data follows.
                WriteFramedInt32(ctx, count);
            }
            else
            {
                fixed (byte* dataPtr = dataAsBytes)
                {
                    int needed = 4 + totalBytes;
                    if (ctx->writerAvailable >= needed)
                    {
                        // Fast path: count + entire body fits in the current
                        // writable region. One MemoryCopy + one flush; the flush
                        // becomes an AdvanceWritePosition with no C++ memcpy.
                        Unsafe.WriteUnaligned(ctx->writerPtr, count);
                        Buffer.MemoryCopy(dataPtr, ctx->writerPtr + 4, totalBytes, totalBytes);
                        InvokeFlushBuffer(ctx, ctx->writerPtr, needed);
                    }
                    else
                    {
                        // Body doesn't fit alongside the count. Write the count,
                        // then hand the pinned source pointer directly to
                        // FlushBuffer — its spill arm runs writer.Write(source,
                        // totalBytes) which fills the writer's tail, transitions
                        // through any number of cache-writer blocks, and lands
                        // partway into the final block, all in one call. One
                        // P/Invoke for an array of any size.
                        WriteFramedInt32(ctx, count);
                        InvokeFlushBuffer(ctx, dataPtr, totalBytes);
                    }
                }
            }

            if (padBytes > 0)
            {
                // The contract guarantees writerAvailable >= kManagedBlockMaxPayloadSize
                // >= 3 here, so the 0..3-byte pad always fits in the current region.
                Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
                InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
            }

            pos = nestedStart + nestedBytes;
            return;
        }

        if ((header->flags & LinearCollectionFlags.ShufflePath) != 0)
        {
            ConsumeLinearCollectionShufflePath(
                ctx, dataAsBytes, count,
                (long)header->elementStride,
                (int)header->elementWireSize,
                nestedStart, nestedBytes);
            pos = nestedStart + nestedBytes;
            return;
        }

        // Per-element recursion path: write SInt32 length prefix, then loop
        // count times calling ExecuteWriteCommands with each element pinned.
        // The body is the element class's command stream (FBP-bracketed
        // DC + String entries). Each iteration's trailing FBP(0) ensures
        // dstSize == 0 between elements, matching the executor invariant.
        WriteFramedInt32(ctx, count);

        if (count > 0)
        {
            fixed (byte* dataPtr = dataAsBytes)
            {
                long stride = (long)header->elementStride;
                for (int i = 0; i < count; ++i)
                {
                    byte* elementPtr = dataPtr + i * stride;
                    // Threading pendingAdvance by ref is the whole point of the
                    // per-element loop fix: each element's FBP(N) opens its
                    // segment at writerPtr + pendingAdvance and only flushes
                    // when the combined region wouldn't fit, so an N-element
                    // array of small structs coalesces into ceil(N*body/cap)
                    // flushes instead of N.
                    ExecuteWriteCommands(ctx, (IntPtr)elementPtr,
                        (IntPtr)nestedStart, nestedBytes, transfer, ref output, ref dstSize, ref pendingAdvance);
                }
            }
        }

        // Commit bytes deferred by per-element coalescing before the tail pad
        // — the loop leaves pendingAdvance non-zero because the last element's
        // closing FBP(0) rolls into it instead of flushing.
        if (pendingAdvance > 0)
        {
            InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
            pendingAdvance = 0;
        }

        // Pad total wire output to 4-byte alignment, matching the trivially-
        // copyable path and the legacy ArrayOfManagedObjectsTransferer (which
        // pads every field to 4 bytes individually). elementWireSize is 0 for
        // variable-length element types (strings); those are already 4-byte
        // aligned, no aggregate pad needed.
        int elementWireSize = (int)header->elementWireSize;
        if (elementWireSize > 0)
        {
            int totalWritten = count * elementWireSize;
            int padBytes     = (4 - (totalWritten & 3)) & 3;
            if (padBytes > 0)
            {
                // FlushBuffer guarantees writerAvailable >= kManagedBlockMaxPayloadSize >= 3.
                Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
                InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
            }
        }

        pos = nestedStart + nestedBytes;
    }

    // Mirrors ConsumeLinearCollection's trivially-copyable arm, but sourced from
    // an inline buffer at baseAddr + fieldOffset — no array reference, null check,
    // or reflection.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumeFixedBuffer(
        NativeBufferContext* ctx, ref byte baseAddr, ref byte* pos)
    {
        var header = (FixedBufferHeader*)pos;
        pos += sizeof(FixedBufferHeader);

        // checked() is defense-in-depth: a corrupted header throws instead of
        // wrapping into a negative MemoryCopy length.
        int count       = checked((int)header->elementCount);
        int totalBytes  = checked(count * (int)header->elementSize);
        int padBytes    = (4 - (totalBytes & 3)) & 3;

        // baseAddr is pinned by the outer ExecuteWriteCommands caller, so the
        // inline buffer address is stable for the duration of this entry.
        byte* dataPtr = (byte*)Unsafe.AsPointer(
            ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));

        // totalBytes == 0 is unreachable: C# forbids `fixed T buf[0]` (CS1665) and
        // the build side rejects zero-sized inline structs. Throw if a regression
        // emits elementCount=0 rather than writing a malformed zero-length record.
        if (totalBytes <= 0)
            throw new InvalidOperationException("FixedBuffer wire payload must be non-zero; build side enforces elementCount > 0.");

        int needed = 4 + totalBytes;
        if (ctx->writerAvailable >= needed + padBytes)
        {
            // Single-flush fast path: count + payload + pad in one P/Invoke. FBP(0)
            // bracketing gives full capacity on entry and the build side caps an
            // in-segment FixedBuffer at kManagedBlockMaxPayloadSize - 4 payload
            // bytes, so only buffers in the top 3-byte size sliver fall through to
            // the two-flush arm below.
            Unsafe.WriteUnaligned(ctx->writerPtr, count);
            Buffer.MemoryCopy(dataPtr, ctx->writerPtr + 4, totalBytes, totalBytes);
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(ctx->writerPtr + needed, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, ctx->writerPtr, needed + padBytes);
            return;
        }

        if (ctx->writerAvailable >= needed)
        {
            Unsafe.WriteUnaligned(ctx->writerPtr, count);
            Buffer.MemoryCopy(dataPtr, ctx->writerPtr + 4, totalBytes, totalBytes);
            InvokeFlushBuffer(ctx, ctx->writerPtr, needed);
        }
        else
        {
            // Hand the inline source straight to FlushBuffer's spill arm so
            // an arbitrarily large buffer crosses any number of cache-writer
            // blocks in a single P/Invoke.
            WriteFramedInt32(ctx, count);
            InvokeFlushBuffer(ctx, dataPtr, totalBytes);
        }

        if (padBytes > 0)
        {
            // Post-flush writerAvailable >= kManagedBlockMaxPayloadSize (>= 3),
            // so the 0..3-byte pad always fits without a fresh refill check.
            Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
        }
    }

    // Shuffle-path consumer for linear collections of value-type elements
    // whose per-element body is purely DC + FBP and fits in a single segment.
    // The body bytes are identical to what the per-element recursion path
    // would walk; we just walk them once per element with a fixed per-element
    // destination and skip all the FBP segment-claim / pendingAdvance bookkeeping.
    //
    // Buffer accounting: the FlushBuffer contract guarantees writerAvailable >=
    // kManagedBlockMaxPayloadSize after every flush, and the build side gates
    // shuffle eligibility on elementWireSize <= kManagedBlockMaxPayloadSize, so
    // the per-batch element count is always >= 1. Each batch fills the writer's
    // current region in a single tight loop and commits with one InvokeFlushBuffer
    // call — taking the per-element P/Invoke count from O(count) (one per element
    // in the per-element recursion path) to O(ceil(count * elementWireSize / cap)).
    private static unsafe void ConsumeLinearCollectionShufflePath(
        NativeBufferContext* ctx,
        byte[] dataAsBytes, int count,
        long stride, int elementWireSize,
        byte* body, int bodyLen)
    {
        WriteFramedInt32(ctx, count);
        if (count == 0)
            return;

        fixed (byte* dataPtr = dataAsBytes)
        {
            byte* srcCur      = dataPtr;
            int   elementsLeft = count;

            while (elementsLeft > 0)
            {
                // Build-side gate (elementWireSize <= kManagedBlockMaxPayloadSize)
                // combined with FlushBuffer's post-flush availability contract
                // makes batch >= 1 unconditionally on entry.
                int batch = ctx->writerAvailable / elementWireSize;
                if (batch > elementsLeft)
                    batch = elementsLeft;

                byte* dst        = ctx->writerPtr;
                int   batchBytes = batch * elementWireSize;

                // Pre-zero the batch's wire window. The transposed walker uses
                // width-correct stores (byte/ushort/int/long matching each DC
                // opcode's wire-slot size); padding bytes between slots — which
                // the prior int-store version implicitly zeroed via 4-byte
                // spillover from DC1/DC2 stores — get their zeros from this
                // memset instead. One memset per batch (<= writerAvailable,
                // typically a single page) is negligible against the
                // count*K -> K body-walk reduction below.
                Unsafe.InitBlockUnaligned(dst, 0, (uint)batchBytes);

                ExecuteShuffleBatch(srcCur, dst, batch, stride, elementWireSize, body, bodyLen);

                InvokeFlushBuffer(ctx, ctx->writerPtr, batchBytes);
                srcCur       += (long)batch * stride;
                elementsLeft -= batch;
            }
        }
    }

    // Transposed walker for shuffle-path bodies. Mirrors the DC opcode
    // dispatch in ExecuteWriteCommands but flips the loop nesting from
    //   for each element: walk body, dispatch every DC group
    // to
    //   walk body once: for each DC group, for each entry, copy across all
    //   `batch` elements with strided pointer arithmetic
    // so the switch dispatch + ConsumeDirectCopyGroup header read +
    // (entryOffset + 3) & ~3L re-align run K times instead of K * batch
    // times. The element loop becomes the innermost — predictable counted
    // iteration over a strided memory copy with constant offsets, which
    // also gives the JIT a fighting chance to vectorise even though stride
    // and elementWireSize are runtime values.
    //
    // Width-correct stores. The per-element predecessor wrote 4 bytes for
    // every DC1 / DC2 entry — only the low N bytes carried meaning, the
    // upper 4-N spilled into adjacent slots and were either overwritten by
    // subsequent entries within the same element, or by the first entries
    // of the next element, or by the trailing FBP(0) marker. Per-element
    // ordering kept that overlap-and-fixup pattern coherent. Transposing
    // breaks it: an entry's spillover for element e now lands in element
    // e+1 *after* element e+1's matching entry has already written its
    // value, with no later write to fix it up. So the transposed walker
    // stores exactly N bytes per DC<N> opcode (byte/ushort/int/long); the
    // intra-element padding that the int-store spillover used to zero
    // comes from the caller's one-shot InitBlockUnaligned of the batch's
    // wire window. Net wire bytes are byte-for-byte identical; the
    // intermediate buffer state along the way differs, but only in the
    // padding bytes which both versions end up with as zero.
    //
    // FBP entries exist in the body for wire-format consistency with the
    // per-element recursion path; we skip them. Any non-DC, non-FBP opcode
    // indicates the build side incorrectly tagged a body as shuffle-
    // eligible — fail fast.
    private static unsafe void ExecuteShuffleBatch(
        byte* srcBase, byte* dstBase,
        int batch,
        long srcStride, int dstStride,
        byte* body, int bodyLen)
    {
        byte* pos    = body;
        byte* endPos = body + bodyLen;

        while (pos < endPos)
        {
            var opCode = (RttiDataType)pos[0];

            switch (opCode)
            {
                case RttiDataType.FixedBlockPrefix:
                    pos += sizeof(ManagedCommandFixedBlockPrefix);
                    continue;

                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(d + (long)i * dstStride) = *(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset  = (nint)entry->destOffset  * 2;
                        byte* s = srcBase + fieldOffset;
                        byte* d = dstBase + destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(ushort*)(d + (long)i * dstStride) = *(ushort*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset  = (nint)entry->destOffset  * 4;
                        byte* s = srcBase + fieldOffset;
                        byte* d = dstBase + destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(int*)(d + (long)i * dstStride) = *(int*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset  = (nint)entry->destOffset  * 8;
                        byte* s = srcBase + fieldOffset;
                        byte* d = dstBase + destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(long*)(d + (long)i * dstStride) = *(long*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<ushort>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<int>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<long>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(d + (long)i * dstStride) = *(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset  = entry->destOffset  * 2;
                        byte* s = srcBase + fieldOffset;
                        byte* d = dstBase + destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(ushort*)(d + (long)i * dstStride) = *(ushort*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset  = entry->destOffset  * 4;
                        byte* s = srcBase + fieldOffset;
                        byte* d = dstBase + destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(int*)(d + (long)i * dstStride) = *(int*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset  = entry->destOffset  * 8;
                        byte* s = srcBase + fieldOffset;
                        byte* d = dstBase + destOffset;
                        for (int i = 0; i < batch; ++i)
                            *(long*)(d + (long)i * dstStride) = *(long*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<ushort>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<int>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->fieldOffset;
                        byte* d = dstBase + entry->destOffset;
                        for (int i = 0; i < batch; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<long>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                default:
                    throw new InvalidOperationException(
                        $"Unexpected opcode {opCode} in shuffle-path body. The build side gates "
                        + "shuffle eligibility on a DC-only body — anything else here is a bug.");
            }

            // Compact groups with an odd entry count leave pos 2 bytes short of
            // a 4-byte boundary; re-align so the next header (FBP or DC group)
            // lands at the alignment its uint fields need.
            long entryOffset = pos - body;
            long aligned     = (entryOffset + 3) & ~3L;
            pos = body + aligned;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string DecodeStringBody(byte* bytes, int length)
    {
        int firstZero = new ReadOnlySpan<byte>(bytes, length).IndexOf((byte)0);
        int effective = firstZero < 0 ? length : firstZero;

        if (effective == 0)
            return string.Empty;

        // Default replacement fallback: malformed subsequences become U+FFFD.
        return Encoding.UTF8.GetString(bytes, effective);
    }

    // Read-path mirror of ConsumeString. Consumes a ManagedCommandStringEntry
    // header from the entry stream, reads the framed wire payload (SInt32 length
    // prefix + UTF-8 body + 4-byte alignment padding), decodes the body via
    // DecodeStringBody, and assigns the result to the field at entry->fieldOffset.
    //
    // Two body paths:
    //   - Spill-buffer path (length <= ctx->stackBufferSize): EnsureReadable
    //     makes 'length' bytes contiguous at ctx->readerPtr (either already in
    //     the CachedReader's window or copied into the native stackBuffer).
    //     Decode happens in-place — zero managed allocation.
    //   - Large-string path (length > ctx->stackBufferSize): allocate byte[length],
    //     bulk-read via InvokeReadBytesDirect, then decode. The allocation is
    //     immediately collectible after the fixed block exits.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumeStringRead(
        NativeReadBufferContext* ctx, ref byte baseAddr, ref byte* pos)
    {
        var entry = (ManagedCommandStringEntry*)pos;
        pos += sizeof(ManagedCommandStringEntry);

        // Length prefix: 4-byte SInt32, little-endian.
        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int length = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        // Reject negative wire lengths explicitly. Without this guard the
        // downstream ReadOnlySpan ctor in DecodeStringBody throws an opaque
        // ArgumentOutOfRangeException; this surfaces corruption at the
        // detection site with a meaningful message.
        if (length < 0)
            throw new InvalidOperationException(
                $"Managed string deserialization read a negative length prefix ({length}). The serialized data is corrupted.");

        int padBytes = (4 - (length & 3)) & 3;

        string result;
        if (length == 0)
        {
            // Wire-format invariant: length=0 → string.Empty (matches how the
            // writer encodes both null and empty source strings).
            result = string.Empty;
        }
        else if (length <= ctx->stackBufferSize)
        {
            // Spill-buffer path: decode in place. Zero allocation; zero P/Invoke
            // when the refill window already covers 'length' bytes (ensureReadable
            // no-ops).
            if (ctx->readerAvailable < length)
                InvokeEnsureReadable(ctx, length);
            result = DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr      += length;
            ctx->readerAvailable -= length;
        }
        else
        {
            // Large-string path: body exceeds the spill buffer, so allocate a
            // one-shot byte[] sized to this string. GC'd when the local exits —
            // no pooling, no retention.
            byte[] buf = new byte[length];
            fixed (byte* bufPtr = buf)
            {
                InvokeReadBytesDirect(ctx, bufPtr, length);
                result = DecodeStringBody(bufPtr, length);
            }
        }

        Unsafe.As<byte, string>(
            ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)) = result;

        // Skip 0-3 bytes of alignment padding.
        if (padBytes > 0)
        {
            if (ctx->readerAvailable < padBytes)
                InvokeEnsureReadable(ctx, padBytes);
            ctx->readerPtr      += padBytes;
            ctx->readerAvailable -= padBytes;
        }
    }

    // Read-path mirror of ConsumeLinearCollection. Reads the count prefix, then
    // routes on the same flag set the writer used:
    //   - Trivially-copyable: bulk memcpy count*elementStride from input into
    //     the freshly allocated array's backing store, then skip the 4-byte
    //     tail pad.
    //   - Shuffle path: input contains count * elementWireSize bytes laid out
    //     as N concatenated per-element bodies; ExecuteReadShuffleBody walks
    //     the FBP-bracketed body once per element with src=input slice and
    //     dst=array element slot, dispatching DC opcodes inline.
    //   - Per-element recursion (general fallback): the FBP-bracketed body is
    //     consumed via ExecuteReadCommands once per element with a fresh
    //     element-pinned baseAddr.
    //
    // The element Type is rebuilt via Type.GetTypeFromHandle from the
    // RuntimeTypeHandle.Value the build side stamped into elementTypeHandle.
    // For List<T> we additionally allocate an uninitialized List<T> and stamp
    // its _items / _size via the same ListLayout reinterpret the write side
    // uses to read them (the _version slot stays at zero — a valid initial
    // state for an uninitialized List<T>). Wire format always emits the
    // header regardless of null source, so a 0-length count leaves the
    // parent's field untouched (default-null).
    private static unsafe void ConsumeLinearCollectionRead(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        ref byte* pos)
    {
        var header = (LinearCollectionHeader*)pos;
        pos += sizeof(LinearCollectionHeader);
        byte* nestedStart = pos;
        int   nestedBytes = (int)header->nestedByteCount;

        // Count prefix: 4 bytes, sits between segments (no FBP bracketing) and
        // is always present even for null/empty source collections.
        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        // Always allocate and assign the collection, even when count == 0.
        // The wire format collapses null and empty source collections to the
        // same `count == 0` framing (see ConsumeLinearCollection on the write
        // side: both `arr == null` and `arr.Length == 0` short-circuit to
        // WriteFramedInt32(0) with no body). The legacy linear-collection
        // readers (Transfer_Blittable_ArrayField via ResizeSTLStyleArray,
        // ArrayOfManagedObjectsTransferer via scripting_array_new(..., 0))
        // both materialize a non-null zero-length collection in that case,
        // so user OnAfterDeserialize callbacks (e.g. UpmCache iterating
        // `m_SerializedProductSearchPackageInfoProductIds.Length`) treat
        // post-deserialize fields as guaranteed-non-null. Skipping the
        // assignment here would leave the field at its CLR default (null)
        // and silently break that contract — observed as a NullReferenceException
        // in UpmCache.OnAfterDeserialize during code-reload backup restore.
        //
        // The build side stamped the native MethodTable* (via
        // scripting_class_get_type(elementClass).GetBackendPtr()) into
        // elementTypeHandle. UnmarshalSystemType (defined above) routes
        // through RuntimeTypeHandle.FromIntPtr on CoreCLR and through an
        // Unsafe.As reinterpret on Mono — see the helper's docs for why
        // a direct reinterpret can't be used on CoreCLR (RuntimeTypeHandle
        // is a managed-reference struct there, not a raw IntPtr).
        Type elementType = UnmarshalSystemType(header->elementTypeHandle);
        // Reuse the existing backing store when it already holds exactly `count`
        // elements, so [NonSerialized] bytes in struct elements survive the read
        // (e.g. undo restoring a same-length array). Mirrors the short-circuit in
        // ResizeSTLStyleArray / the legacy ArrayOfManagedObjectsTransferer path,
        // which checks element count (not byte capacity) for both arrays and lists.
        Array arr;
        if (header->kind == LinearCollectionKind.Array)
        {
            Array existingArr = Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            arr = (existingArr != null && existingArr.Length == count)
                ? existingArr
                : Array.CreateInstance(elementType, count);
        }
        else
        {
            ListLayout existingList = Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            arr = (existingList != null
                && existingList._size == count
                && existingList._items != null
                && existingList._items.Length >= count)
                ? Unsafe.As<byte[], Array>(ref existingList._items)
                : Array.CreateInstance(elementType, count);
        }
        byte[] dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);

        if (count > 0)
        {
            if ((header->flags & LinearCollectionFlags.TriviallyCopyable) != 0)
            {
                long totalBytesL = (long)count * (long)header->elementStride;
                int  totalBytes  = checked((int)totalBytesL);
                int  padBytes    = (4 - (totalBytes & 3)) & 3;
                if (totalBytes > 0)
                {
                    // Bulk-stream straight into the freshly-allocated array's
                    // backing store, bypassing the spill buffer entirely. Drains
                    // any prefix already in readerPtr/readerAvailable, then reads
                    // the remainder directly from the CachedReader.
                    fixed (byte* dataPtr = dataAsBytes)
                    {
                        InvokeReadBytesDirect(ctx, dataPtr, totalBytes);
                    }
                }
                if (padBytes > 0)
                {
                    if (ctx->readerAvailable < padBytes)
                        InvokeEnsureReadable(ctx, padBytes);
                    ctx->readerPtr      += padBytes;
                    ctx->readerAvailable -= padBytes;
                }
            }
            else if ((header->flags & LinearCollectionFlags.ShufflePath) != 0)
            {
                int  elementWireSize = (int)header->elementWireSize;
                long stride          = (long)header->elementStride;

                // Process the wire payload in L1-sized batches: each batch
                // reads its own wire bytes directly from the CachedReader into
                // a stack scratch buffer, then runs the transposed walker
                // against the matching slice of the managed array. Two wins
                // over staging the whole payload in a pooled buffer:
                //   1. Peak memory is just `scratch + managed array`, not
                //      `wireBytes + managed array` — important for large
                //      arrays where wire bytes can be many MB.
                //   2. Both source (scratch, ~kReadShuffleScratchBytes) and
                //      destination (batch slice of the managed array) stay
                //      hot in L1 across all K DC entries of one batch. The
                //      one-shot version strided the dest through the entire
                //      managed array per DC entry, blowing the cache.
                //
                // P/Invoke cost: count / batchElements ReadBytesDirect calls
                // per array. With kReadShuffleScratchBytes = 32 KB and a
                // typical elementWireSize of 60–80 B, batchElements is in the
                // 400–500 range — so a 100k-element array takes ~200–250
                // callbacks. Versus ~8000 if we were chunking through the
                // 1 KB EnsureReadable spill buffer; versus 1 for the prior
                // pooled-buffer version. The middle ground retains the cache
                // win without paying the per-element-EnsureReadable cost.
                //
                // Build-side gate (elementWireSize > 0 and
                // elementWireSize <= kManagedBlockMaxPayloadSize = 256B)
                // makes batchElements >= kReadShuffleScratchBytes / 256 = 128
                // unconditionally on entry.
                const int kReadShuffleScratchBytes = 1024;
                byte* scratch = stackalloc byte[kReadShuffleScratchBytes];
                int batchElements = kReadShuffleScratchBytes / elementWireSize;

                fixed (byte* dataPtr = dataAsBytes)
                {
                    int batchStart   = 0;
                    int elementsLeft = count;
                    while (elementsLeft > 0)
                    {
                        int batch      = (elementsLeft < batchElements) ? elementsLeft : batchElements;
                        int batchBytes = batch * elementWireSize;

                        InvokeReadBytesDirect(ctx, scratch, batchBytes);

                        ExecuteReadShuffleBatch(
                            scratch, dataPtr + (long)batchStart * stride,
                            batch,
                            elementWireSize, stride,
                            nestedStart, nestedBytes);

                        batchStart   += batch;
                        elementsLeft -= batch;
                    }
                }
            }
            else
            {
                // Per-element recursion: each element's FBP-bracketed body is
                // walked by ExecuteReadCommands with the element pinned. The
                // trailing FBP(0) on each iteration advances ctx->readerPtr by
                // elementWireSize, stepping naturally to the next element.
                fixed (byte* dataPtr = dataAsBytes)
                {
                    long stride = (long)header->elementStride;
                    int  segSize = 0;
                    for (int i = 0; i < count; ++i)
                    {
                        byte* elemBase = dataPtr + (long)i * stride;
                        ExecuteReadCommands(
                            ctx,
                            ref Unsafe.AsRef<byte>(elemBase),
                            nestedStart, nestedBytes,
                            ref segSize);
                    }
                }

                // Skip the 0..3-byte tail pad the write side emitted after
                // the per-element loop. Mirrors the trivially-copyable path.
                // elementWireSize is 0 for variable-length elements (strings)
                // — already 4-byte aligned, no pad written.
                int elementWireSize = (int)header->elementWireSize;
                if (elementWireSize > 0)
                {
                    int totalBytes = count * elementWireSize;
                    int padBytes   = (4 - (totalBytes & 3)) & 3;
                    if (padBytes > 0)
                    {
                        if (ctx->readerAvailable < padBytes)
                            InvokeEnsureReadable(ctx, padBytes);
                        ctx->readerPtr      += padBytes;
                        ctx->readerAvailable -= padBytes;
                    }
                }
            }
        }

        if (header->kind == LinearCollectionKind.Array)
        {
            Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset)) = arr;
        }
        else
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
            object listObj = RuntimeHelpers.GetUninitializedObject(listType);
            ListLayout layout = Unsafe.As<ListLayout>(listObj);
            layout._items = dataAsBytes;
            layout._size  = count;
            Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset)) = layout;
        }

        pos = nestedStart + nestedBytes;
    }

    // Read-path mirror of ConsumeFixedBuffer. Truncating on overflow and leaving
    // trailing inline bytes untouched on underflow matches the native
    // Transfer_Blittable_FixedBufferField semantic (Blittable.h), so wire bytes
    // round-trip even when the inline buffer width changed between the asset and
    // the current class.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumeFixedBufferRead(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        ref byte* pos)
    {
        var header = (FixedBufferHeader*)pos;
        pos += sizeof(FixedBufferHeader);

        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int wireCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        // Without this guard a negative length would make copyBytes negative;
        // InvokeReadBytesDirect would then sign-extend to size_t and over-read the
        // stream. Surface the corruption here instead of crashing deeper down.
        if (wireCount < 0)
            throw new InvalidOperationException(
                $"Managed fixed-buffer deserialization read a negative length prefix ({wireCount}). The serialized data is corrupted.");

        // Validate elementSize before any arithmetic uses it: the build side only
        // asserts elementSize ∈ {1, 2, 4, 8} in debug, which the read path can't
        // rely on for a stream that may have been corrupted in transit.
        int elementSize = header->elementSize;
        if (elementSize != 1 && elementSize != 2 && elementSize != 4 && elementSize != 8)
            throw new InvalidOperationException(
                $"Managed fixed-buffer header has invalid elementSize {elementSize}; expected 1, 2, 4, or 8.");

        // checked() rationale: see ConsumeFixedBuffer. The read path can't rely on
        // the build side's debug-only int32 bound, so a wrap throws instead of
        // producing a negative copyBytes that InvokeReadBytesDirect would
        // sign-extend into an over-read.
        int capacity    = checked((int)header->elementCount);
        int copyCount   = wireCount < capacity ? wireCount : capacity;
        int copyBytes   = checked(copyCount * elementSize);
        long wireBytesL = (long)wireCount * (long)elementSize;
        int  alignBytes = (int)((4 - (wireBytesL & 3)) & 3);

        if (copyBytes > 0)
        {
            // Bulk-stream straight into the inline buffer, bypassing the
            // spill buffer (same direct-pin pattern as the trivially-
            // copyable arm of ConsumeLinearCollectionRead).
            byte* dstPtr = (byte*)Unsafe.AsPointer(
                ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));
            InvokeReadBytesDirect(ctx, dstPtr, copyBytes);
        }

        // Discard any wire overflow (assets where the buffer shrunk between
        // versions). Chunked because a single ensureReadable refill is capped at
        // stackBufferSize and a long buffer can exceed it.
        long discardBytes = wireBytesL - copyBytes;
        while (discardBytes > 0)
        {
            int chunk = discardBytes > ctx->stackBufferSize
                ? ctx->stackBufferSize
                : (int)discardBytes;
            if (ctx->readerAvailable < chunk)
                InvokeEnsureReadable(ctx, chunk);
            ctx->readerPtr      += chunk;
            ctx->readerAvailable -= chunk;
            discardBytes        -= chunk;
        }

        if (alignBytes > 0)
        {
            if (ctx->readerAvailable < alignBytes)
                InvokeEnsureReadable(ctx, alignBytes);
            ctx->readerPtr      += alignBytes;
            ctx->readerAvailable -= alignBytes;
        }
    }

    // Read mirror of ExecuteShuffleBatch. Walks the FBP-bracketed DC-only
    // body once, then for each DC entry runs an inner loop across all
    // `count` elements with strided pointer arithmetic — moving the switch
    // dispatch + ConsumeDirectCopyGroup header read + 4-byte re-align cost
    // from O(K * count) down to O(K). Width-correct loads and stores
    // throughout (read N from wire-side `srcBase + destOffset`, store N
    // into managed-side `dstBase + fieldOffset`); the read direction
    // already had no spillover, so the transposition is straight loop
    // inversion with no semantic change. Caller pins the managed array
    // with `fixed`, so raw pointer arithmetic against `dstBase` is safe
    // for the duration of the call. FBP entries are skipped; any non-DC,
    // non-FBP opcode trips the InvalidOperationException because the
    // build side guarantees DC-only bodies for the shuffle flag.
    private static unsafe void ExecuteReadShuffleBatch(
        byte* srcBase, byte* dstBase,
        int count,
        int srcStride, long dstStride,
        byte* body, int bodyLen)
    {
        byte* pos    = body;
        byte* endPos = body + bodyLen;

        while (pos < endPos)
        {
            var opCode = (RttiDataType)pos[0];

            switch (opCode)
            {
                case RttiDataType.FixedBlockPrefix:
                    pos += sizeof(ManagedCommandFixedBlockPrefix);
                    continue;

                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(d + (long)i * dstStride) = *(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset  = (nint)entry->destOffset  * 2;
                        byte* s = srcBase + destOffset;
                        byte* d = dstBase + fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(ushort*)(d + (long)i * dstStride) = *(ushort*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset  = (nint)entry->destOffset  * 4;
                        byte* s = srcBase + destOffset;
                        byte* d = dstBase + fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(int*)(d + (long)i * dstStride) = *(int*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset  = (nint)entry->destOffset  * 8;
                        byte* s = srcBase + destOffset;
                        byte* d = dstBase + fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(long*)(d + (long)i * dstStride) = *(long*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<ushort>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<int>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<long>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(d + (long)i * dstStride) = *(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset  = entry->destOffset  * 2;
                        byte* s = srcBase + destOffset;
                        byte* d = dstBase + fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(ushort*)(d + (long)i * dstStride) = *(ushort*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset  = entry->destOffset  * 4;
                        byte* s = srcBase + destOffset;
                        byte* d = dstBase + fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(int*)(d + (long)i * dstStride) = *(int*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset  = entry->destOffset  * 8;
                        byte* s = srcBase + destOffset;
                        byte* d = dstBase + fieldOffset;
                        for (int i = 0; i < count; ++i)
                            *(long*)(d + (long)i * dstStride) = *(long*)(s + (long)i * srcStride);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<ushort>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<int>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        byte* s = srcBase + entry->destOffset;
                        byte* d = dstBase + entry->fieldOffset;
                        for (int i = 0; i < count; ++i)
                            Unsafe.WriteUnaligned(d + (long)i * dstStride, Unsafe.ReadUnaligned<long>(s + (long)i * srcStride));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                default:
                    throw new InvalidOperationException(
                        $"Unexpected opcode {opCode} in shuffle-path body. The build side gates "
                        + "shuffle eligibility on a DC-only body — anything else here is a bug.");
            }

            long entryOffset = pos - body;
            long aligned     = (entryOffset + 3) & ~3L;
            pos = body + aligned;
        }
    }

    // Read-path mirror of ConsumeValueReference. The inner body is its own
    // self-contained FBP(N)..FBP(0) chain, so we hand ExecuteReadCommands a
    // fresh innerSegmentSize=0.
    private static unsafe void ConsumeValueReferenceRead(
        NativeReadBufferContext* ctx, ref byte baseAddr, ref byte* pos)
    {
        var header = (ValueReferenceHeader*)pos;
        pos += sizeof(ValueReferenceHeader);
        byte* nestedStart = pos;
        int nestedBytes = (int)header->nestedByteCount;

        if (nestedBytes == 0)
        {
            pos = nestedStart;
            return;
        }

        object obj = GetOrCreateVrtInstance(ref baseAddr, header);

        // `fixed` pins the inner instance across the native ensureReadable
        // / readBytesDirect P/Invokes inside the recursion.
        fixed (byte* nestedBase = &Unsafe.As<ObjectWrapper>(obj).Data)
        {
            int innerSegmentSize = 0;
            ExecuteReadCommands(
                ctx,
                ref Unsafe.AsRef<byte>(nestedBase),
                nestedStart, nestedBytes,
                ref innerSegmentSize);
        }

        pos = nestedStart + nestedBytes;
    }

    [RequiredByNativeCode]
    public static unsafe int SerializationBufferToObject(
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int entryBufferSize,
        IntPtr readContext)
    {
        // Managed object memory: accessed via ref so the GC can track it (the caller
        // currently pins, but the contract is "managed memory"). Unmanaged buffers
        // (input, command stream) stay as raw byte*.
        ref byte baseAddr = ref Unsafe.AsRef<byte>((void*)pinnedBase);
        var ctx = (NativeReadBufferContext*)readContext;

        int currentSegmentSize = 0;
        ExecuteReadCommands(
            ctx,
            ref baseAddr,
            (byte*)entriesPtr, entryBufferSize,
            ref currentSegmentSize);
        return 0;
    }

    // Inner loop shared by SerializationBufferToObject (top-level) and
    // ConsumeLinearCollectionRead (per-element recursion). Each segment's DC
    // destOffsets restart at 0; segments are laid out contiguously in the
    // refill window we receive from EnsureReadable. We advance ctx->readerPtr
    // past each completed segment when the trailing FBP(0) fires so the next
    // segment's destOffsets land on the right slice.
    //
    // currentSegmentSize is threaded by ref so a recursion frame opened inside
    // a segment (for nested per-element bodies) sees the parent's outstanding
    // segment size and doesn't drop or double-advance the read cursor.
    private static unsafe void ExecuteReadCommands(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        byte* entryBase, int entryBufSize,
        ref int currentSegmentSize)
    {
        byte* pos    = entryBase;
        byte* endPos = entryBase + entryBufSize;

        while (pos < endPos)
        {
            // Refresh segment-local read cursor each iteration. ctx->readerPtr is
            // stable within a segment (no ensureReadable calls between FBP(N>0)
            // and FBP(0)), but FBP(0), variable-sized entries (LinearCollection,
            // future String/VRT), and the leading FBP(N>0) of the next segment
            // may all move it, so we re-snapshot before reading any DC entry.
            byte* input = ctx->readerPtr;
            var opCode = (RttiDataType)pos[0];

            switch (opCode)
            {
                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset) = *(input + entry->destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset = (nint)entry->destOffset * 2;
                        Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)) = *(ushort*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset = (nint)entry->destOffset * 4;
                        Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)) = *(int*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset = (nint)entry->destOffset * 8;
                        // Unaligned: SelectDirectCopyOpCode's destOffset%8 gate only proves
                        // segment-relative alignment, not absolute address alignment. The
                        // segment base (input = ctx->readerPtr) can be 4-byte aligned (e.g.
                        // inside per-element bodies after a linear collection's 4B count
                        // prefix), which SIGBUS'd on armv7 with a typed 8B load. Build-side
                        // fix pending.
                        Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)) = Unsafe.ReadUnaligned<long>(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<ushort>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<int>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<long>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset) = *(input + entry->destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset = entry->destOffset * 2;
                        Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = *(ushort*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset = entry->destOffset * 4;
                        Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = *(int*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset = entry->destOffset * 8;
                        // Unaligned: see DirectCopy8 above for the segment-base alignment caveat.
                        Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = Unsafe.ReadUnaligned<long>(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<ushort>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<int>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<long>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.FixedBlockPrefix:
                {
                    var prefix = (ManagedCommandFixedBlockPrefix*)pos;
                    pos += sizeof(ManagedCommandFixedBlockPrefix);
                    if (prefix->payloadSize > 0)
                    {
                        // Open-segment marker: ensure the next `payloadSize` bytes
                        // are addressable contiguously at ctx->readerPtr. We don't
                        // advance the cursor here — DC entries within the segment
                        // index off ctx->readerPtr + entry->destOffset; the
                        // matching FBP(0) advances past the segment.
                        currentSegmentSize = prefix->payloadSize;
                        if (ctx->readerAvailable < currentSegmentSize)
                            InvokeEnsureReadable(ctx, currentSegmentSize);
                    }
                    else
                    {
                        // Close-segment marker: commit the segment we just read
                        // by advancing the cursor past it. readerAvailable shrinks
                        // by the same amount; if a subsequent ensureReadable
                        // exceeds what's left, the spill path will refill from the
                        // CachedReader.
                        ctx->readerPtr      += currentSegmentSize;
                        ctx->readerAvailable -= currentSegmentSize;
                        currentSegmentSize = 0;
                    }
                    break;
                }

                // 16B read entry carries klass per-entry. We always invoke the
                // icall — fake-null / EntityId_None handling lives in
                // TransferPPtrToMonoObject.
                case RttiDataType.UnityObject:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectReadEntry>(ref pos, out var end);
                    do
                    {
                        ref object fieldRef = ref Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset));
                        byte* src = input + entry->destOffset;
                        fieldRef = ReadUnityObjectFromBuffer(ctx->resolverHandle, (IntPtr)src, entry->klass, ctx->flags);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.Array:
                case RttiDataType.List:
                {
                    ConsumeLinearCollectionRead(ctx, ref baseAddr, ref pos);
                    break;
                }

                case RttiDataType.FixedBuffer:
                {
                    ConsumeFixedBufferRead(ctx, ref baseAddr, ref pos);
                    break;
                }

                case RttiDataType.ValueReferenceType:
                {
                    ConsumeValueReferenceRead(ctx, ref baseAddr, ref pos);
                    break;
                }

                case RttiDataType.CallOnAfterDeserializeClass:
                {
                    var header = (CallbackHeader*)pos;
                    pos += sizeof(CallbackHeader);
                    object target = Unsafe.As<byte, object>(
                        ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));
                    if (target is ISerializationCallbackReceiver receiver)
                        receiver.OnAfterDeserialize();
                    break;
                }

                case RttiDataType.CallOnAfterDeserializeStruct:
                {
                    var header = (CallbackHeader*)pos;
                    pos += sizeof(CallbackHeader);
                    if (header->methodFnPtr != IntPtr.Zero)
                    {
                        ref byte structData = ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset);
                        ((delegate*<ref byte, void>)header->methodFnPtr)(ref structData);
                    }
                    break;
                }

                case RttiDataType.String:
                {
                    ConsumeStringRead(ctx, ref baseAddr, ref pos);
                    break;
                }

                case RttiDataType.DirectCopyBlock:
                case RttiDataType.Reference:
                case RttiDataType.EntityId:
                case RttiDataType.DynamicBuffer:
                case RttiDataType.PropertyNameId:
                case RttiDataType.SimpleNativeType:
                case RttiDataType.Unknown:
                    throw new NotSupportedException(
                        $"OpCode {opCode} is not implemented for managed command blocks.");
                default:
                    throw new NotSupportedException($"OpCode {opCode} not supported");
            }

            // Match the writer's 4-byte header alignment (see ObjectToSerializationBuffer
            // for details). Compact groups with an odd entry count leave pos 2 bytes short.
            long entryOffset = pos - entryBase;
            long aligned = (entryOffset + 3) & ~3L;
            pos = entryBase + aligned;
        }
    }
}
