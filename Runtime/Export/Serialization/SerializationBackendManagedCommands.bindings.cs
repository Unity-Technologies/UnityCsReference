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
// EntityId lives in namespace UnityEngine (UnityEngineObject.bindings.cs:141). The
// test-resources compile context (UNITY_NATIVE_TEST_RESOURCES) provides a stub in the
// same namespace via Runtime/Testing/ScriptWithManagedRefTestFixture.Resources_cs, so
// the bare `EntityId` field type below resolves identically in both compile contexts.
using UnityEngine;

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

    // Dictionary<K,V> field. See ManagedCommandDictionary in SerializationCommands.h
    // for the wire / executor contract. Slot picked to sit past the batch's
    // FixedBuffer (26) / Matrix4x4 renumber and managed-callbacks' 28-31 range.
    Dictionary              = 32,

    //LoadableSceneId/LoadableObjectId
    NativeValueStruct       = 33,

    // [SerializeReference] inline RefId field (write). The gather pass resolved the
    // RefId already (incl. missing-type write-back); the executor pops it from the
    // shared per-host cursor and writes one SInt64 (RefId_Null for null). Same
    // command-group shape as UnityObject.
    ManagedReference        = 34,

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
//
// A parallel (fieldBackendPtr, fieldParentClassPtr) table follows the entry
// array (see FieldTableFor in SerializationCommands.h); the executor forwards
// each slot to ReadUnityObjectFromBuffer for the resolver-miss fake-null wrapper.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct UnityObjectReadEntry
{
    public uint fieldOffset;
    public uint destOffset;
    public IntPtr klass;
}

// EntityId group entries (LazyLoadReference<T>), mirroring ManagedCommandEntityIdEntry:
// {fieldOffset, destOffset}, 12-byte LSOI on the wire. The id is the field value,
// so there's no klass or field-table; read and write entries are identical.
internal struct EntityIdWriteEntry // 8 bytes
{
    public uint fieldOffset;
    public uint destOffset;
}

internal struct EntityIdReadEntry // 8 bytes
{
    public uint fieldOffset;
    public uint destOffset;
}

// Mirrors UnityObjectTransferFlags in ReadUnityObjectFromBuffer.h. Used by
// both the read and write paths.
internal static class UnityObjectTransferFlags
{
    public const int IsThreadedSerialization              = 1 << 0;
    public const int DontCreateMonoBehaviourScriptWrapper = 1 << 1;
    public const int AllowPPtrRead                        = 1 << 2;
    public const int PackEntityIdInLSOI                   = 1 << 3;
    public const int SerializeForGameRelease              = 1 << 4;
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

// Mirrors ManagedCommandPropertyNameEntry in SerializationCommands.h (8 bytes).
// serializesAsId: 1 = persist the decimal id (player / editor game-release),
// 0 = persist the resolved name (editor non-game-release).
internal unsafe struct ManagedCommandPropertyNameEntry  // 8 bytes
{
    public RttiDataType opCode;
    public byte         serializesAsId;
    public fixed byte   reserved[2];
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

// Mirrors ManagedCommandSimpleNativeTypeReadEntry in SerializationCommands.h.
// 48 bytes on 64-bit (8 + 5*sizeof(IntPtr)); the preceding fields sum to 8 bytes
// so the IntPtrs are naturally aligned, no Pack annotation needed.
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ManagedCommandSimpleNativeTypeReadEntry  // 48 bytes (64-bit)
{
    public RttiDataType opCode;
    public byte         reserved;
    public ushort       reserved2;
    public uint         fieldOffset;
    public IntPtr       fnPtr;
    public IntPtr       userData;                 // m_Ptr offset within the wrapper
    public IntPtr       runtimeTypeHandle;
    public IntPtr       ctorFunctionPtr;
    public IntPtr       managedPostDispatchFnPtr;
}

// Mirrors ManagedCommandNativeValueStructEntry in SerializationCommands.h.
// 16 bytes on 64-bit (8 + sizeof(IntPtr)). Inline value struct, transferred via
// the type's native Transfer — no wrapper, so no userData / ctor / post-dispatch.
[StructLayout(LayoutKind.Sequential)]
internal struct ManagedCommandNativeValueStructEntry  // 16 bytes (64-bit)
{
    public RttiDataType opCode;
    public byte         reserved;
    public ushort       reserved2;
    public uint         fieldOffset;
    public IntPtr       fnPtr;
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

// NOTE: This enum must be kept in sync with RttiGatherOp in
// Runtime/Mono/SerializationBackend_DirectMemoryAccess/SerializationCommands.h.
// See the native header for the per-opcode wire / executor contract.
internal enum RttiGatherOp : byte
{
    RegisterRef                      = 0,
    RegisterRefArray                 = 1,
    RegisterRefList                  = 2,
    RecurseClass                     = 3,
    RecurseStruct                    = 4,
    RecurseClassArray                = 5,
    RecurseClassList                 = 6,
    RecurseStructArray               = 7,
    RecurseStructList                = 8,
    InvokeOnBeforeSerializeClass     = 9,
    InvokeOnBeforeSerializeStruct    = 10,
    RecurseDictionary                = 11,

    Unknown                          = 0xFF,
}

// Mirrors of native gather entry structs in SerializationCommands.h. Natural
// sequential layout matches the native side exactly. Reserved bytes pad each
// opcode to its declared size so the gather byte stream stays 4-byte aligned;
// entries with IntPtr fields also pad to 8-byte alignment for the pointer.

internal struct GatherRegisterRefEntry  // 8 + sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public IntPtr       propertyPathTemplate;  // baked template, resolved by MoveToBuffer's gather fixup pass
}

internal struct GatherRegisterRefArrayEntry  // 8 + sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public IntPtr       propertyPathTemplate;
}

internal struct GatherRegisterRefListEntry  // 8 + sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public IntPtr       propertyPathTemplate;
}

internal struct GatherRecurseClassEntry  // 16 + 2*sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
    public uint         reserved3;
    public IntPtr       runtimeTypeHandle;
    public IntPtr       ctorFunctionPtr;
}

internal struct GatherRecurseStructEntry  // 12 bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
}

internal struct GatherRecurseClassArrayEntry  // 16 + 2*sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
    public uint         reserved3;
    public IntPtr       runtimeTypeHandle;
    public IntPtr       ctorFunctionPtr;
}

internal struct GatherRecurseStructArrayEntry  // 16 bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
    public uint         elementSize;
}

internal struct GatherRecurseClassListEntry  // 16 + 2*sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
    public uint         reserved3;  // explicit pad — see native GatherRecurseClassListEntry::_pad2
    public IntPtr       runtimeTypeHandle;
    public IntPtr       ctorFunctionPtr;
}

internal struct GatherRecurseStructListEntry  // 16 bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
    public uint         elementSize;
}

internal struct GatherInvokeOnBeforeSerializeClassEntry  // 8 + sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         reserved3;
    public IntPtr       methodFnPtr;
}

internal struct GatherInvokeOnBeforeSerializeStructEntry  // 8 + sizeof(IntPtr) bytes
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         reserved3;
    public IntPtr       methodFnPtr;
}

internal struct GatherRecurseDictionaryEntry  // 16 + sizeof(IntPtr) bytes (24 on 64-bit)
{
    public RttiGatherOp opCode;
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;
    public uint         nestedByteCount;
    public uint         elementSize;
    public IntPtr       propertyPathTemplate;  // baked dict-path FUID template; resolved by MoveToBuffer's gather fixup pass
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

// Mirrors ManagedCommandDictionaryWrite in SerializationCommands.h. Body of
// nestedByteCount bytes immediately follows (per-entry FBP-bracketed DC +
// optional String body, walked once per SerializedKeyValue<K,V> entry against
// the entry-pinned base).
internal struct DictionaryHeaderWrite  // 24 + sizeof(IntPtr) bytes
{
    public RttiDataType opCode;                        // = RttiDataType.Dictionary
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;                   // post-header offset of the dictionary reference on the parent
    public uint         entryStride;                   // sizeof(SerializedKeyValue<K,V>)
    public uint         nestedByteCount;               // bytes of FBP-bracketed body that follow
    public int          getEntriesTypedIndex;          // SerializationCommandObjectTable index for closed GetEntriesTyped<K,V>; -1 = falls back to non-typed entry point
    public uint         reserved3;                     // pad to 8-byte align fieldUniqueIdentifierTemplate
    public IntPtr       fieldUniqueIdentifierTemplate; // editor-only; IntPtr.Zero in player builds
}

// Mirrors ManagedCommandDictionaryRead in SerializationCommands.h. Same opcode
// value as DictionaryHeaderWrite — the dispatchers live in separate switches
// (write inside ObjectToSerializationBuffer, read inside SerializationBufferToObject)
// so opcode reuse is unambiguous.
internal struct DictionaryHeaderRead  // 24 + 2*sizeof(IntPtr) bytes
{
    public RttiDataType opCode;                          // = RttiDataType.Dictionary
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;                     // post-header offset of the dictionary reference on the parent
    public uint         entryStride;                     // sizeof(SerializedKeyValue<K,V>)
    public uint         nestedByteCount;                 // bytes of FBP-bracketed body that follow
    public int          dictDefaultAllocateFactoryIndex; // SerializationCommandObjectTable index for Func<object> => new Dictionary<K,V>(); -1 = leave null on read
    public int          setEntriesTypedIndex;            // SerializationCommandObjectTable index for closed SetEntriesTyped<K,V>; -1 = falls back to non-typed entry point
    public IntPtr       elementTypeHandle;               // SerializedKeyValue<K,V> RuntimeTypeHandle.Value for Array.CreateInstance
    public IntPtr       fieldUniqueIdentifierTemplate;   // editor-only; IntPtr.Zero in player builds
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
// Pack = 8 keeps EntityId's UInt64 8-byte aligned on every runtime, matching the
// native C++ ABI (EntityID.h:68). Without an explicit Pack, some 32-bit Mono
// configurations reduce the alignment to 4, which would shift hostingEntityId
// four bytes earlier than the native struct on 32-bit and corrupt the per-block
// hostingEntityId reads.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal unsafe struct NativeBufferContext
{
    public void*    writer;            // native CachedWriter* — opaque to C#
    public byte*    stackBuffer;       // native-side spill buffer (size = kManagedBlockSpillBufferSize); stable for the lifetime of the call
    public byte*    writerPtr;         // current write destination — writer's tail or stackBuffer; updated by flushBuffer
    public int      writerAvailable;   // bytes available at writerPtr; updated by flushBuffer; always >= kManagedBlockMaxPayloadSize after a flush / initial setup
    public delegate* unmanaged[Cdecl]<NativeBufferContext*, byte*, int, void> flushBuffer;
    public IntPtr   resolverHandle;    // ILSOIResolver*; forwarded to WriteUnityObjectToBuffer. Null falls back to the global PersistentManager path.
    public int      flags;             // UnityObjectTransferFlags bits (write path consults PackEntityIdInLSOI).
    public int      _pad;              // pad to 8-byte align fuidContext on 64-bit
    public IntPtr   fuidContext;       // native FieldUniqueIdentifierContext*; forwarded to DictionaryFieldUniqueIdentifierStack.Push/PopDictionaryFUIDFrame. IntPtr.Zero when no transfer-side context is active.
    public EntityId hostingEntityId;   // Resolved once per managed block by the native dispatcher (FUID context's value first, falling back to TryGetHostingEntityIdForUnityObject in editor). EntityId.None when neither yields a value.
    public IntPtr   transferState;     // native ManagedReferencesTransferState*; forwarded to WriteManagedReferenceToBuffer for the [SerializeReference] inline-RefId opcode. IntPtr.Zero on transfers without managed references.
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
//
// Pack = 8 keeps EntityId's UInt64 8-byte aligned on every runtime, matching the
// native C++ ABI (same reasoning as NativeBufferContext above).
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal unsafe struct NativeReadBufferContext
{
    public void*    reader;            // native CachedReader* — opaque to C#
    public byte*    stackBuffer;       // native-side spill buffer (size = stackBufferSize); stable for the lifetime of the call
    public byte*    readerPtr;         // current read source — reader's cache or stackBuffer; updated by ensureReadable
    public int      readerAvailable;   // bytes available at readerPtr; decremented by C# as it consumes; refilled by ensureReadable
    public int      stackBufferSize;   // size of stackBuffer; cap on a single ensureReadable request
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, int, void> ensureReadable;
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, byte*, int, void> readBytesDirect;
    // Rewinds the CachedReader by readerAvailable and empties the spill window before a
    // SimpleNativeType dispatch reads straight off the CachedReader.
    public delegate* unmanaged[Cdecl]<NativeReadBufferContext*, void> syncReader;
    public IntPtr   resolverHandle;    // ILSOIResolver*; forwarded to ReadUnityObjectFromBuffer. Null falls back to the global PersistentManager path.
    public int      flags;             // UnityObjectTransferFlags bits forwarded to ReadUnityObjectFromBuffer.
    public bool     warnOnDuplicates;  // True for serialized-file loads and Object.Instantiate clones; false for Inspector ApplyModifiedProperties and other in-memory transfers.
    public byte     _pad0;
    public byte     _pad1;
    public byte     _pad2;             // align fuidContext to 8-byte boundary
    public IntPtr   fuidContext;       // native FieldUniqueIdentifierContext*; forwarded to ConsumeDictionaryRead for FUID Push/Pop bracketing. IntPtr.Zero when no transfer-side context is active.
    public EntityId hostingEntityId;   // Resolved once per managed block by the native dispatcher (FUID context's value first, falling back to TryGetHostingEntityIdForUnityObject in editor). EntityId.None when neither yields a value.
    public IntPtr   transferState;     // native ManagedReferencesTransferState*; forwarded to ReadManagedReferenceFromBuffer for the [SerializeReference] inline-RefId read opcode. IntPtr.Zero on transfers without managed references.
    public IntPtr   instance;          // native GeneralMonoObject* (host being read into); forwarded to ReadManagedReferenceFromBuffer for RegisterFixupRequest.
}

[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/WriteUnityObjectToBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/WriteManagedReferenceToBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/ReadUnityObjectFromBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/ReadManagedReferenceFromBuffer.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/GatherDictionaryEntries.h")]
[NativeHeader("Runtime/Mono/SerializationBackend_DirectMemoryAccess/DictionaryFieldUniqueIdentifierStack.h")]
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

    // Write-side icall for the RttiDataType.ManagedReference opcode
    // ([SerializeReference] inline RefId). Pops the next inline RefId from the
    // active per-object cursor on transferState (the native
    // ManagedReferencesTransferState* from NativeBufferContext.transferState) — the
    // gather pass resolved and recorded it in field order, so the icall reads no
    // field. outputPtr receives the 8-byte SInt64 RefId.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern void WriteManagedReferenceToBuffer(
        IntPtr transferState,
        IntPtr outputPtr);

    // Gather-pass dictionary enumeration. Returns the dictionary's merged
    // SerializedKeyValue<K,V>[] (live + preserved-duplicate rows) via the native
    // DictionarySerializationProxy so the gather walker doesn't need a C#-compile-time
    // reference to UnityEngine.DictionarySerialization (absent in some native
    // test-resource assemblies). Routes through the same proxy the write uses
    // (DictionaryField::GetArray), and the native side reconstructs the write's FUID
    // context (host refid + array-index stack + dict template) so the duplicate-row
    // lookup matches — keeping gather and write enumeration in lockstep for the
    // inline-RefId cursor. dictObjRaw / transferState / templatePtr / indices are raw
    // pointers (IntPtr, same marshalling rationale as the other icalls); indexCount is
    // the live array-index depth. See GatherDictionaryEntries.h.
    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern unsafe object GetDictionaryEntriesForGather(IntPtr dictObjRaw, IntPtr transferState, IntPtr templatePtr, IntPtr indices, int indexCount);

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
    private static readonly delegate* unmanaged[Cdecl]<ulong, IntPtr, IntPtr, int, void> s_writeEntityIdToBuffer =
        (delegate* unmanaged[Cdecl]<ulong, IntPtr, IntPtr, int, void>)(void*)GetWriteEntityIdToBufferFunctionPointer();
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, ulong> s_readEntityIdFromBuffer =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, ulong>)(void*)GetReadEntityIdFromBufferFunctionPointer();

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetWriteEntityIdToBufferFunctionPointer();

    [MethodImpl(MethodImplOptions.InternalCall)]
    [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
    private static extern IntPtr GetReadEntityIdFromBufferFunctionPointer();

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

    // Read-side helpers (ConsumeDictionaryRead). Format the dict's FUID template
    // against the currently-pushed FUID frame to get the duplicate-storage key
    // (matches what DictionaryField::SetArray does on the legacy path), and
    // emit the clickable duplicate-key Console warning when the read side
    // detects keys that the live dict couldn't accept.
    //
    // [FreeFunction] is incompatible with [MethodImpl(InternalCall)] — the
    // BindingsGenerator processes FreeFunction-attributed methods and rejects
    // ones already marked InternalCall. The gate below mirrors the gate on the
    // sole caller (ConsumeDictionaryRead), so the extern declarations are absent
    // in the UNITY_NATIVE_TEST_RESOURCES compile context where the test
    // TestAssembly.dll doesn't run the BindingsGenerator.
    [FreeFunction("DictionaryFieldUniqueIdentifierBindings::FormatDictionaryFieldUniqueIdentifierForActiveContext", IsThreadSafe = true)]
    private static extern string FormatDictionaryFieldUniqueIdentifier(IntPtr dictionaryIdentifierTemplate);

    [FreeFunction("DictionaryFieldUniqueIdentifierBindings::LogDictionaryDuplicateKeyWarning", IsThreadSafe = true)]
    private static extern void LogDictionaryDuplicateKeyWarning(string message, EntityId hostingEntityId);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeSyncReader(NativeReadBufferContext* ctx)
        => ctx->syncReader(ctx);

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
    internal static Type UnmarshalSystemType(IntPtr handlePtr)
    {
        if (handlePtr == IntPtr.Zero)
            return null;
        return Type.GetTypeFromHandle(
            Unsafe.As<IntPtr, RuntimeTypeHandle>(ref handlePtr));
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
    // netstandard2.1 reference assembly this file builds against, so we
    // resolve it via reflection on first call and cache the delegate.
    //
    // Mono's RuntimeMethodHandle is a single-IntPtr struct, so the reinterpret
    // is benign there and the #else arm is correct.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RuntimeMethodHandle UnmarshalRuntimeMethodHandle(IntPtr methodHandleValue)
    {
        return Unsafe.As<IntPtr, RuntimeMethodHandle>(ref methodHandleValue);
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
    public static unsafe void ObjectToSerializationBuffer(
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

                // [SerializeReference] inline RefId. Reuses the UnityObject write
                // group shape (UnityObjectWriteEntry: {fieldOffset, destOffset}), but
                // the field is NOT read: the gather pass resolved every SR field's
                // inline RefId (incl. the missing-type upgrade) in field order and the
                // icall pops the next one from the per-object cursor on transferState.
                // The native SR-collection arm advances the same cursor, so scalar and
                // collection SR fields stay in lockstep. fieldOffset is unused here
                // (kept for the shared entry shape). transferState is non-null whenever
                // this opcode is emitted (build only emits it for SR fields).
                case RttiDataType.ManagedReference:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectWriteEntry>(ref pos, out var end);
                    do
                    {
                        byte* dst = output + entry->destOffset;
                        WriteManagedReferenceToBuffer(ctx->transferState, (IntPtr)dst);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.EntityId:
                {
                    var entry = ConsumeDirectCopyGroup<EntityIdWriteEntry>(ref pos, out var end);
                    // Clone transfers (and EntityId.None) encode the id in managed code;
                    // serialized-file transfers map it through the native resolver, which
                    // also records the dependency (WriteEntityIdToBuffer).
                    bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
                    do
                    {
                        ref byte fieldByteRef = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                        ulong entityId = Unsafe.ReadUnaligned<ulong>(ref fieldByteRef);
                        byte* dst = output + entry->destOffset;

                        if (packInLSOI || entityId == 0UL)
                            PackEntityIdIntoLsoi(dst, entityId);
                        else
                            s_writeEntityIdToBuffer(entityId, ctx->resolverHandle, (IntPtr)dst, ctx->flags);
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

                case RttiDataType.NativeValueStruct:
                {
                    var entry = (ManagedCommandNativeValueStructEntry*)pos;
                    pos += sizeof(ManagedCommandNativeValueStructEntry);

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

                    // Inline value struct: hand the field's own address to the native
                    // Transfer dispatcher. baseAddr is pinned by the ExecuteWriteCommands
                    // caller, so this interior pointer stays valid for the synchronous call.
                    ref byte nvsField = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                    IntPtr nvsFieldPtr = (IntPtr)Unsafe.AsPointer(ref nvsField);

                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)entry->fnPtr)(nvsFieldPtr, transfer, IntPtr.Zero);

                    InvokeFlushBuffer(ctx, ctx->writerPtr, 0);
                    break;
                }

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

                case RttiDataType.Dictionary:
                    // Same FBP(0)-before-header invariant as LinearCollection above:
                    // build closes any pending segment, dstSize == 0, and the
                    // dispatcher writes the count + per-entry body via writerPtr.
                    if (pendingAdvance > 0)
                    {
                        InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                        pendingAdvance = 0;
                    }
                    ConsumeDictionary(ctx, ref baseAddr, transfer, ref output, ref dstSize, ref pendingAdvance, ref pos);
                    break;

                case RttiDataType.FixedBuffer:
                    // Build emits FBP(0) before every FixedBuffer header, so dstSize == 0
                    if (dstSize != 0)
                        throw new InvalidOperationException("FixedBuffer must be preceded by FBP(0) — see AppendFixedBufferToManagedBlock.");
                    ConsumeFixedBuffer(ctx, ref baseAddr, ref pos, ref pendingAdvance);
                    break;

                case RttiDataType.PropertyNameId:
                    ConsumePropertyNameEditor(ctx, ref baseAddr, ref pos, ref dstSize, ref pendingAdvance);
                    break;

                case RttiDataType.Reference:
                case RttiDataType.DynamicBuffer:
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

    // Write side of the PropertyName opcode. Frames byte-identically to
    // SerializeTraits<PropertyName> so assets round-trip with the native system.

    // Player / game-release: always serializes the decimal id (no editor string table).
    // The native-test fake { int id; } uses this path too.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumePropertyNamePlayer(
        NativeBufferContext* ctx, ref byte baseAddr, ref byte* pos,
        ref int dstSize, ref int pendingAdvance)
    {
        var entry = (ManagedCommandPropertyNameEntry*)pos;
        pos += sizeof(ManagedCommandPropertyNameEntry);
        int id = Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset));
        WriteFramedDecimalInt32(ctx, id, ref dstSize, ref pendingAdvance);
    }

    // Editor non-game-release: persists the resolved name. Reads the whole struct so
    // conflictIndex disambiguates the id (matches the native system). Game-release
    // writes the id.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumePropertyNameEditor(
        NativeBufferContext* ctx, ref byte baseAddr, ref byte* pos,
        ref int dstSize, ref int pendingAdvance)
    {
        var entry = (ManagedCommandPropertyNameEntry*)pos;
        byte serializesAsId = entry->serializesAsId;
        pos += sizeof(ManagedCommandPropertyNameEntry);
        ref byte field = ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);

        if (serializesAsId == 0)
        {
            PropertyName pn = Unsafe.ReadUnaligned<PropertyName>(ref field);
            string s = PropertyNameUtils.StringFromPropertyName(pn);
            // null (unregistered id) → empty string, matching the native system.
            WriteFramedString(ctx, (s ?? string.Empty).AsSpan(), ref dstSize, ref pendingAdvance);
        }
        else
        {
            int id = Unsafe.ReadUnaligned<int>(ref field);
            WriteFramedDecimalInt32(ctx, id, ref dstSize, ref pendingAdvance);
        }
    }

    // Length-prefixed UTF-8 framing (same wire shape as the String opcode), coalescing
    // into pendingAdvance. Editor PropertyName-name path. The chunked arm handles names
    // larger than one buffer region — rare in practice, kept for correctness.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteFramedString(NativeBufferContext* ctx, ReadOnlySpan<char> chars,
        ref int dstSize, ref int pendingAdvance)
    {
        // Roll any open fixed segment into the deferred region (FBP(0) precedes the opcode, so usually 0).
        if (dstSize > 0)
        {
            pendingAdvance += dstSize;
            dstSize = 0;
        }

        int nullIdx = chars.IndexOf('\0');
        if (nullIdx >= 0)
            chars = chars.Slice(0, nullIdx);

        int totalByteCount = Encoding.UTF8.GetByteCount(chars);
        int padBytes = (4 - (totalByteCount & 3)) & 3;
        int totalFramedSize = 4 + totalByteCount + padBytes;

        // Coalesce path: fits after the deferred bytes — write there and defer; no flush.
        if (ctx->writerAvailable - pendingAdvance >= totalFramedSize)
        {
            byte* dst = ctx->writerPtr + pendingAdvance;
            Unsafe.WriteUnaligned(dst, totalByteCount);
            if (totalByteCount > 0)
                Encoding.UTF8.GetBytes(chars, new Span<byte>(dst + 4, totalByteCount));
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(dst + 4 + totalByteCount, 0, (uint)padBytes);
            pendingAdvance += totalFramedSize;
            return;
        }

        // Won't fit alongside the deferred bytes: flush them, freeing a full region.
        if (pendingAdvance > 0)
        {
            InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
            pendingAdvance = 0;
        }

        // Fits in a fresh region: write at offset 0 and defer so a following field coalesces.
        if (ctx->writerAvailable >= totalFramedSize)
        {
            byte* dst = ctx->writerPtr;
            Unsafe.WriteUnaligned(dst, totalByteCount);
            if (totalByteCount > 0)
                Encoding.UTF8.GetBytes(chars, new Span<byte>(dst + 4, totalByteCount));
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(dst + 4 + totalByteCount, 0, (uint)padBytes);
            pendingAdvance = totalFramedSize;
            return;
        }

        // Chunked path (oversized name). Length header first; pendingAdvance stays 0.
        WriteFramedInt32(ctx, totalByteCount);
        if (totalByteCount > 0)
        {
            // flush:false while input remains so the encoder can hold a high surrogate
            // across chunks until its low surrogate arrives in the next call.
            Encoder encoder = s_Utf8Encoder ??= Encoding.UTF8.GetEncoder();
            encoder.Reset();
            ReadOnlySpan<char> remaining = chars;
            while (!remaining.IsEmpty)
            {
                encoder.Convert(remaining, new Span<byte>(ctx->writerPtr, ctx->writerAvailable),
                                flush: false, out int charsUsed, out int bytesUsed, out _);
                if (bytesUsed > 0)
                    InvokeFlushBuffer(ctx, ctx->writerPtr, bytesUsed);
                remaining = remaining.Slice(charsUsed);
            }
            encoder.Convert(ReadOnlySpan<char>.Empty, new Span<byte>(ctx->writerPtr, ctx->writerAvailable),
                            flush: true, out _, out int tailBytes, out _);
            if (tailBytes > 0)
                InvokeFlushBuffer(ctx, ctx->writerPtr, tailBytes);
        }

        if (padBytes > 0)
        {
            Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
        }
    }

    // Decimal-ASCII Int32 (== native IntToString) in the String wire shape. Framed
    // payload ≤16 bytes always fits after a flush, so no chunked arm. Not inlined:
    // IL2CPP would accumulate the stackalloc into the caller's frame (alloca is only
    // reclaimed on return).
    private static unsafe void WriteFramedDecimalInt32(NativeBufferContext* ctx, int value,
        ref int dstSize, ref int pendingAdvance)
    {
        if (dstSize > 0)
        {
            pendingAdvance += dstSize;
            dstSize = 0;
        }

        // Build digits least-significant-first in a stack scratch, then emit in order.
        // long magnitude so Int32.MinValue is representable.
        bool negative = value < 0;
        long magnitude = negative ? -(long)value : value;
        byte* rev = stackalloc byte[10];             // up to 10 digits
        int d = 0;
        do
        {
            rev[d++] = (byte)('0' + (int)(magnitude % 10));
            magnitude /= 10;
        }
        while (magnitude > 0);

        int n = d + (negative ? 1 : 0);
        int padBytes = (4 - (n & 3)) & 3;
        int totalFramedSize = 4 + n + padBytes;

        if (ctx->writerAvailable - pendingAdvance < totalFramedSize && pendingAdvance > 0)
        {
            InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
            pendingAdvance = 0;
        }

        byte* dst = ctx->writerPtr + pendingAdvance;
        Unsafe.WriteUnaligned(dst, n);
        int w = 4;
        if (negative)
            dst[w++] = (byte)'-';
        for (int i = d - 1; i >= 0; i--)
            dst[w++] = rev[i];
        if (padBytes > 0)
            Unsafe.InitBlockUnaligned(dst + 4 + n, 0, (uint)padBytes);
        pendingAdvance += totalFramedSize;
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

    // Wrapper construction path for SimpleNativeType reads in ExecuteReadCommands.
    // For SimpleNativeType wrappers the parameterless ctor is what allocates the
    // native peer, so running it is required to get a usable m_Ptr afterwards.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe object CreateWrapperInstance(IntPtr runtimeTypeHandle, IntPtr ctorFunctionPtr)
    {
        // ctorFunctionPtr is baked at build time on every backend now that the
        // native ResolveParameterlessCtorFunctionPointer passes kConstructor to
        // the method lookup (CoreCLR included), so no per-backend fallback is
        // needed here. Zero still means the type has no parameterless ctor.
        Type type = UnmarshalSystemType(runtimeTypeHandle);
        object obj = RuntimeHelpers.GetUninitializedObject(type);
        if (ctorFunctionPtr != IntPtr.Zero)
        {
            try
            {
                ((delegate*<object, void>)ctorFunctionPtr)(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        return obj;
    }

    // Takes the individual fields rather than a ValueReferenceHeader* so the
    // gather pass (ProcessGatherRecurseClass) can reuse the same materialization
    // logic with its own GatherRecurseClassEntry layout — the field set
    // (fieldOffset / runtimeTypeHandle / ctorFunctionPtr) is identical between
    // the two opcode spaces, only the surrounding struct layout differs.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe object GetOrCreateVrtInstance(
        ref byte baseAddr, uint fieldOffset, IntPtr runtimeTypeHandle, IntPtr ctorFunctionPtr)
    {
        ref object slot = ref Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        object obj = slot;
        if (obj != null)
            return obj;

        Type type = UnmarshalSystemType(runtimeTypeHandle);
        if (type == null)
            return null;
        // ctorFunctionPtr is baked at build time on every backend (see
        // CreateWrapperInstance); zero means the type has no parameterless ctor.
        obj = RuntimeHelpers.GetUninitializedObject(type);
        if (ctorFunctionPtr != IntPtr.Zero)
        {
            try
            {
                ((delegate*<object, void>)ctorFunctionPtr)(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        slot = obj;
        return obj;
    }

    // Consumes a ValueReferenceType entry and recurses into ExecuteWriteCommands
    // with the inner instance pinned as the source. The body's own FBP(N)..FBP(0)
    // bracketing drives buffer claims and flushes.
    //
    // runtimeTypeHandle discriminates the encoding:
    //   - Non-zero (class field): resolve via GetOrCreateVrtInstance and pin
    //     ObjectWrapper.Data as the offset-zero source.
    //   - Zero (struct field): struct lives inline at baseAddr + fieldOffset;
    //     the outer caller's pin on the containing GC object covers this
    //     recursion, so just shift the base and recurse.
    //
    // ctorFunctionPtr is not the sentinel: a class whose parameterless ctor
    // lookup fails also stamps a zero ctorFunctionPtr and would alias as a
    // struct. runtimeTypeHandle (raw MonoType* / MethodTable*) is non-zero
    // for every real class.
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

        if (header->runtimeTypeHandle == IntPtr.Zero)
        {
            // Struct: inline at baseAddr + fieldOffset. Re-pinning via
            // `fixed (byte* p = &baseAddr)` would create an IL slot the GC
            // root-scan walks as an interior pointer — crashes when baseAddr
            // was reconstructed from a raw IntPtr.
            ref byte nestedBase = ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset);
            ExecuteWriteCommands(ctx,
                (IntPtr)Unsafe.AsPointer(ref nestedBase),
                (IntPtr)nestedStart, nestedBytes, transfer,
                ref output, ref dstSize, ref pendingAdvance);
        }
        else
        {
            // Class: ObjectWrapper.Data is the offset-zero reference for nested fieldOffsets.
            object obj = GetOrCreateVrtInstance(ref baseAddr, header->fieldOffset, header->runtimeTypeHandle, header->ctorFunctionPtr);
            fixed (byte* nestedBase = &Unsafe.As<ObjectWrapper>(obj).Data)
            {
                ExecuteWriteCommands(ctx, (IntPtr)nestedBase,
                    (IntPtr)nestedStart, nestedBytes, transfer, ref output, ref dstSize, ref pendingAdvance);
            }
        }

        pos = nestedStart + nestedBytes;
    }

    // -----------------------------------------------------------------------
    // Gather pass — pre-write walker
    //
    // Walks the parallel gather byte stream emitted by the native build side
    // (see RttiGatherOp in SerializationCommands.h). For each entry:
    //
    //   - Register{Ref,RefArray,RefList}: read the [SerializeReference]
    //     object reference(s) at base+fieldOffset and hand each non-null
    //     reference to the native registry through registerRefFnPtr.
    //   - Recurse{Class,Struct}{,Array,List}: descend into a non-ref class
    //     or struct field; class variants null-materialize the field via
    //     runtimeTypeHandle + ctorFunctionPtr (mirroring GetOrCreateVrtInstance)
    //     so that constructor-initialized [SerializeReference] fields aren't
    //     missed when the parent ctor populates them.
    //   - InvokeOnBeforeSerialize{Class,Struct}: fire the user's
    //     ISerializationCallbackReceiver.OnBeforeSerialize callback so any
    //     [SerializeReference] fields set up in the callback are visible to
    //     the subsequent Register entries in this subtree. The write pass
    //     skips its own OnBeforeSerialize invocations whenever a gather pass
    //     ran for the root (the native side flips IsGatherCompleted on the
    //     ManagedReferencesTransferState).
    //
    // All recurse entries store a uint childCount = number of nested gather
    // entries that follow them; the walker advances by entry size as it
    // dispatches and uses childCount to bound the nested recursion. For
    // arrays / lists the nested block is walked once per element with a
    // per-element base; the byte cursor is rewound to nestedStart before each
    // iteration and ends at the end of the nested block after the last
    // iteration, so the outer loop in GatherWalkN can continue from there.
    // SkipGatherEntries handles the "field is null / collection is empty"
    // edge case by walking the same byte structure without executing.

    // All three gather callbacks (register-ref, mark-OBS-invoked, resolve-
    // missing-type) are invoked via `delegate* unmanaged[Cdecl]<...>` calli
    // directly off the IntPtr the native side hands the walker. Native passes
    // a real C function pointer (& a static function in ExecuteManagedCommands
    // .cpp), so no GCHandle indirection is needed; calli is allocation-free,
    // no per-call delegate marshalling, no thread-static caching.
    //
    // The managed `object` reference holds the raw header pointer on
    // Mono / IL2CPP / CoreCLR — the same value ScriptingObjectPtr carries
    // on the native side — so we forward it directly via Unsafe.As without
    // boxing through GCHandle. The object stays rooted in the array / list /
    // field that produced it, so no extra keep-alive is needed for the
    // duration of the synchronous P/Invoke.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeRegisterGatheredRef(IntPtr fnPtr, IntPtr transferState, object obj)
    {
        IntPtr objPtr = Unsafe.As<object, IntPtr>(ref obj);
        ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)fnPtr)(transferState, objPtr);
    }

    // Missing-type resolve callback. The native shim (ExecuteManagedCommands
    // .cpp / ResolveMissingTypeForGather) formats the template by substituting
    // "%d" placeholders with indices[0..indexCount-1], looks the resulting path
    // up in the MissingTypeRegistry against the gather host refid cached at
    // InvokeManagedGather entry, and stuffs any matching missing-type refid
    // into m_MissingTypeSet. Only invoked from Register* handlers when the
    // SerializeReference value is null AND collectMissingTypes is true.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeResolveMissingTypeForGather(
        IntPtr fnPtr, IntPtr transferState, IntPtr templatePtr, int* indices, int indexCount)
    {
        // Defensive: fnPtr is IntPtr.Zero when native built without FUID/missing-
        // type support. Register handlers gate on collectMissingTypes which is
        // false in that configuration, but a NULL-check here is cheap insurance.
        if (fnPtr == IntPtr.Zero || templatePtr == IntPtr.Zero)
            return;
        ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int*, int, void>)fnPtr)(transferState, templatePtr, indices, indexCount);
    }

    // Top-level entry point invoked from the native side per root object,
    // once the build-time check (hasSerializeReferenceInSubtree) has cleared
    // the gather pass for execution. `rootInstance` is the user object the
    // outer Transfer is about to write; `gatherEntriesPtr` / `gatherEntryCount`
    // describe the per-type gather byte stream produced at build time.
    // Native passes `transferStatePtr` (opaque ManagedReferencesTransferState*)
    // and `registerRefFnPtr` (cdecl void(*)(transferState, objectRef)) so the
    // walker can hand each discovered reference straight to the native registry
    // without going back through a managed proxy class.
    // Max collection-nesting depth for the index stack. Matches
    // FieldUniqueIdentifierContext::kMaxArrayDepth on the native side. The
    // gather walker pushes one slot per active Recurse*Array / Recurse*List /
    // Recurse*Dictionary frame; Register* handlers read indices[0..indexDepth]
    // to substitute %d placeholders in the baked property-path template.
    private const int kMaxGatherIndexDepth = 10;

    [RequiredByNativeCode]
    public static unsafe int GatherRefs(
        object rootInstance,
        IntPtr gatherEntriesPtr,
        int    gatherEntryBufferSize,
        IntPtr transferStatePtr,
        IntPtr registerRefFnPtr,
        IntPtr resolveMissingTypeFnPtr,
        int    emitCallbacksFlag,
        int    collectMissingTypesFlag)
    {
        if (rootInstance == null || gatherEntryBufferSize == 0 || gatherEntriesPtr == IntPtr.Zero)
            return 0;

        bool emitCallbacks = emitCallbacksFlag != 0;
        bool collectMissingTypes = collectMissingTypesFlag != 0;
        // Stack-allocated index stack — zero managed allocations, fits the
        // expected collection-nesting depth comfortably. Recurse*Array/List/
        // Dictionary handlers write indices[indexDepth] before recursing with
        // indexDepth+1; Register* handlers pass indices[0..indexDepth-1] to
        // the missing-type resolve callback.
        int* indexStack = stackalloc int[kMaxGatherIndexDepth];
        fixed (byte* rootBase = &Unsafe.As<ObjectWrapper>(rootInstance).Data)
        {
            byte* pos = (byte*)gatherEntriesPtr;
            byte* end = pos + gatherEntryBufferSize;
            // Top-level walk by byte-end: native passes the buffer size, not the
            // entry count. The total entry count includes children of Recurse
            // entries, but the Recurse handlers consume their own children
            // recursively — using the total count at top-level would walk past
            // the buffer end.
            GatherWalkToEnd(ref *rootBase, rootInstance, rootBase, ref pos, end,
                transferStatePtr, registerRefFnPtr, resolveMissingTypeFnPtr,
                emitCallbacks, collectMissingTypes, indexStack, 0);
        }
        return 0;
    }

    private static unsafe void GatherWalkToEnd(
        ref byte baseAddr, object thisObject, byte* heapObjDataArea,
        ref byte* pos, byte* end,
        IntPtr transferState, IntPtr registerRefFnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        while (pos < end)
        {
            GatherWalkOne(ref baseAddr, thisObject, heapObjDataArea, ref pos,
                transferState, registerRefFnPtr, resolveMissingTypeFnPtr,
                emitCallbacks, collectMissingTypes, indexStack, indexDepth);
        }
    }

    // `thisObject` is the boxed object whose data area `baseAddr` points into,
    // carried through class frames so that InvokeOnBeforeSerializeClass can
    // dispatch via interface cast (avoids backend-specific arithmetic from
    // data-area back to object-header pointer). Set to null when the current
    // frame is a struct (RecurseStruct / per-element of a struct array or
    // list) — struct frames only ever invoke InvokeOnBeforeSerializeStruct,
    // which uses baseAddr.
    //
    // emitCallbacks: false during cloning-without-LSOI; the walker still
    // discovers refs via Register* / Recurse* entries but skips both class
    // and struct OnBeforeSerialize invocations to match the gate in the
    // write-side InvokeMethod handler.
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void GatherWalkOne(
        ref byte baseAddr, object thisObject, byte* heapObjDataArea,
        ref byte* pos,
        IntPtr transferState, IntPtr registerRefFnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var op = (RttiGatherOp)pos[0];
        switch (op)
        {
            case RttiGatherOp.RegisterRef:
                ProcessGatherRegisterRef(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RegisterRefArray:
                ProcessGatherRegisterRefArray(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RegisterRefList:
                ProcessGatherRegisterRefList(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseClass:
                ProcessGatherRecurseClass(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseStruct:
                ProcessGatherRecurseStruct(ref baseAddr, thisObject, heapObjDataArea, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseClassArray:
                ProcessGatherRecurseClassArray(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseClassList:
                ProcessGatherRecurseClassList(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseStructArray:
                ProcessGatherRecurseStructArray(ref baseAddr, thisObject, heapObjDataArea, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseStructList:
                ProcessGatherRecurseStructList(ref baseAddr, thisObject, heapObjDataArea, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.RecurseDictionary:
                ProcessGatherRecurseDictionary(ref baseAddr, ref pos, transferState, registerRefFnPtr,
                    resolveMissingTypeFnPtr, emitCallbacks, collectMissingTypes, indexStack, indexDepth);
                break;
            case RttiGatherOp.InvokeOnBeforeSerializeClass:
                if (emitCallbacks)
                    ProcessGatherInvokeOnBeforeSerializeClass(thisObject, ref pos);
                else
                    pos += sizeof(GatherInvokeOnBeforeSerializeClassEntry);
                break;
            case RttiGatherOp.InvokeOnBeforeSerializeStruct:
                if (emitCallbacks)
                    ProcessGatherInvokeOnBeforeSerializeStruct(ref baseAddr, ref pos);
                else
                    pos += sizeof(GatherInvokeOnBeforeSerializeStructEntry);
                break;
            default:
                throw new InvalidOperationException("Unknown gather opcode: " + op);
        }
    }

    private static unsafe void ProcessGatherRegisterRef(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRegisterRefEntry*)pos;
        uint fieldOffset = entry->fieldOffset;
        IntPtr templatePtr = entry->propertyPathTemplate;
        pos += sizeof(GatherRegisterRefEntry);

        // Always register, including null. The legacy RemapPPtrTransfer walks
        // every SerializeReference value and ends up calling RegisterReference
        // for null too — that's what creates the registry's RefId_Null entry
        // when the user's data actually has a null SerializeReference field.
        object obj = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        InvokeRegisterGatheredRef(fnPtr, transferState, obj);
        // Missing-type resolution for null SerializeReference fields. Mirror of
        // the null arm of ResolveMissingType on the write side: if a missing-
        // type entry was registered at this (hostRefId, propertyPath) on load,
        // record its refid in m_MissingTypeSet so the upstream collection loop
        // pulls it (and its dependencies) into the on-disk refs array. No-op
        // when the field is non-null (the live value supersedes any registered
        // missing-type) or when the gather pass isn't collecting missing types.
        if (obj == null && collectMissingTypes)
            InvokeResolveMissingTypeForGather(resolveMissingTypeFnPtr, transferState, templatePtr, indexStack, indexDepth);
    }

    private static unsafe void ProcessGatherRegisterRefArray(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRegisterRefArrayEntry*)pos;
        uint fieldOffset = entry->fieldOffset;
        IntPtr templatePtr = entry->propertyPathTemplate;
        pos += sizeof(GatherRegisterRefArrayEntry);

        // T[] of a reference type is castable to object[] via array covariance —
        // safe for read-only iteration. The build side only emits this opcode
        // for [SerializeReference] collections (always reference-typed elements).
        object[] arr = Unsafe.As<byte, object[]>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (arr == null)
            return;
        for (int e = 0; e < arr.Length; e++)
        {
            // Null elements still register (RefId_Null) — see ProcessGatherRegisterRef
            // for the rationale.
            object elem = arr[e];
            InvokeRegisterGatheredRef(fnPtr, transferState, elem);
            if (elem == null && collectMissingTypes && indexDepth < kMaxGatherIndexDepth)
            {
                // Per-element index push for the %d at the end of the baked
                // template (e.g. "m_Refs.Array.data[%d]"); index stays on the
                // stack only for the duration of this missing-type lookup.
                indexStack[indexDepth] = e;
                InvokeResolveMissingTypeForGather(resolveMissingTypeFnPtr, transferState, templatePtr, indexStack, indexDepth + 1);
            }
        }
    }

    private static unsafe void ProcessGatherRegisterRefList(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRegisterRefListEntry*)pos;
        uint fieldOffset = entry->fieldOffset;
        IntPtr templatePtr = entry->propertyPathTemplate;
        pos += sizeof(GatherRegisterRefListEntry);

        object listObj = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (listObj == null)
            return;
        var layout = Unsafe.As<ListLayout>(listObj);
        byte[] itemsBytes = layout._items;
        if (itemsBytes == null)
            return;
        // List<T>'s _items is T[]; for a reference T it is castable to object[].
        object[] items = Unsafe.As<byte[], object[]>(ref itemsBytes);
        int size = layout._size;
        for (int e = 0; e < size; e++)
        {
            // Null elements still register (RefId_Null) — same reasoning as
            // ProcessGatherRegisterRef.
            object elem = items[e];
            InvokeRegisterGatheredRef(fnPtr, transferState, elem);
            if (elem == null && collectMissingTypes && indexDepth < kMaxGatherIndexDepth)
            {
                indexStack[indexDepth] = e;
                InvokeResolveMissingTypeForGather(resolveMissingTypeFnPtr, transferState, templatePtr, indexStack, indexDepth + 1);
            }
        }
    }

    private static unsafe void ProcessGatherRecurseClass(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseClassEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        IntPtr rth = entry->runtimeTypeHandle;
        IntPtr cfp = entry->ctorFunctionPtr;
        pos += sizeof(GatherRecurseClassEntry);
        byte* nestedEnd = pos + nestedBytes;

        // Reuse VRT's materialize-if-null helper — same field set
        // (fieldOffset / runtimeTypeHandle / ctorFunctionPtr), same behavior
        // (writes back to the field slot so the materialized instance shows
        // up in the user's data the same way the write transfer would
        // materialize a null class field). GetOrCreateVrtInstance returns
        // null only when UnmarshalSystemType fails (no runtimeTypeHandle).
        object obj = GetOrCreateVrtInstance(ref baseAddr, fieldOffset, rth, cfp);
        if (obj == null)
        {
            pos = nestedEnd;
            return;
        }

        // Entering a new heap-object frame: thisObject and heapObjDataArea
        // both update to this nested class, so any OBS callsites discovered
        // below mark/check using the new instance.
        fixed (byte* objBase = &Unsafe.As<ObjectWrapper>(obj).Data)
        {
            GatherWalkToEnd(ref *objBase, obj, objBase, ref pos, nestedEnd,
                transferState, fnPtr, resolveMissingTypeFnPtr,
                emitCallbacks, collectMissingTypes, indexStack, indexDepth);
        }
    }

    private static unsafe void ProcessGatherRecurseStruct(
        ref byte baseAddr, object thisObject, byte* heapObjDataArea,
        ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseStructEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        pos += sizeof(GatherRecurseStructEntry);
        byte* nestedEnd = pos + nestedBytes;

        // Struct lives inline at base+fieldOffset. Propagate thisObject /
        // heapObjDataArea unchanged — the struct's OBS is keyed off (host,
        // struct field offset), so we need the same host context inside the
        // struct frame.
        GatherWalkToEnd(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset),
            thisObject, heapObjDataArea,
            ref pos, nestedEnd, transferState, fnPtr, resolveMissingTypeFnPtr,
            emitCallbacks, collectMissingTypes, indexStack, indexDepth);
    }

    private static unsafe void ProcessGatherRecurseClassArray(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseClassArrayEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        pos += sizeof(GatherRecurseClassArrayEntry);
        byte* nestedStart = pos;
        byte* nestedEnd = nestedStart + nestedBytes;

        object[] arr = Unsafe.As<byte, object[]>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (arr == null || arr.Length == 0)
        {
            pos = nestedEnd;
            return;
        }

        // Null elements are skipped, NOT materialized: a null element
        // serializes as an empty/default container slot on disk with no live
        // SerializeReference refs, and missing-type entries can only exist at
        // paths the previous write actually traversed — which the legacy
        // RemapPPtrTransfer walk also skipped for null elements. Materializing
        // here would mutate the user's data (arr[i] no longer null) and waste
        // work walking a default-initialized instance that has nothing the
        // accumulator hasn't already seen via real refs.
        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        for (int e = 0; e < arr.Length; e++)
        {
            object elem = arr[e];
            if (elem == null)
                continue;
            if (indexDepth < kMaxGatherIndexDepth)
                indexStack[indexDepth] = e;
            pos = nestedStart;
            fixed (byte* elemBase = &Unsafe.As<ObjectWrapper>(elem).Data)
            {
                GatherWalkToEnd(ref *elemBase, elem, elemBase, ref pos, nestedEnd,
                    transferState, fnPtr, resolveMissingTypeFnPtr,
                    emitCallbacks, collectMissingTypes, indexStack, nestedDepth);
            }
        }
        // Always end at nestedEnd, even if no element was walked (all null).
        // Skipping nested bytes is just pointer advancement — no need to walk
        // entry-by-entry now that the byte size is stored.
        pos = nestedEnd;
    }

    private static unsafe void ProcessGatherRecurseClassList(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseClassListEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        pos += sizeof(GatherRecurseClassListEntry);
        byte* nestedStart = pos;
        byte* nestedEnd = nestedStart + nestedBytes;

        object listObj = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (listObj == null)
        {
            pos = nestedEnd;
            return;
        }
        var layout = Unsafe.As<ListLayout>(listObj);
        byte[] itemsBytes = layout._items;
        int size = layout._size;
        if (itemsBytes == null || size == 0)
        {
            pos = nestedEnd;
            return;
        }
        object[] items = Unsafe.As<byte[], object[]>(ref itemsBytes);

        // Null elements skipped — see RecurseClassArray for rationale.
        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        for (int e = 0; e < size; e++)
        {
            object elem = items[e];
            if (elem == null)
                continue;
            if (indexDepth < kMaxGatherIndexDepth)
                indexStack[indexDepth] = e;
            pos = nestedStart;
            fixed (byte* elemBase = &Unsafe.As<ObjectWrapper>(elem).Data)
            {
                GatherWalkToEnd(ref *elemBase, elem, elemBase, ref pos, nestedEnd,
                    transferState, fnPtr, resolveMissingTypeFnPtr,
                    emitCallbacks, collectMissingTypes, indexStack, nestedDepth);
            }
        }
        pos = nestedEnd;
    }

    private static unsafe void ProcessGatherRecurseStructArray(
        ref byte baseAddr, object thisObject, byte* heapObjDataArea,
        ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseStructArrayEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        uint stride = entry->elementSize;
        pos += sizeof(GatherRecurseStructArrayEntry);
        byte* nestedStart = pos;
        byte* nestedEnd = nestedStart + nestedBytes;

        object arrObj = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (arrObj == null)
        {
            pos = nestedEnd;
            return;
        }
        Array arr = (Array)arrObj;
        int length = arr.Length;
        if (length == 0)
        {
            pos = nestedEnd;
            return;
        }

        // Reinterpret T[] as byte[] for `fixed` pinning. Layout of any T[]
        // is { Length, T[0], T[1], ... }; byte[] of the same managed object
        // exposes the data area as bytes for stride-based addressing.
        // Struct array elements have no class identity of their own; pass
        // thisObject=null so struct-OBS marking is skipped for these
        // elements (an embedded struct array's element-OBS callsite is not
        // representable in the (host, fieldOffset) gate's key space, so the
        // main-write OBS fires normally — at the cost of double-firing for
        // those elements. Tests for this case are not in the editor suite).
        byte[] arrBytes = Unsafe.As<byte[]>(arrObj);
        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        fixed (byte* itemsBase = arrBytes)
        {
            for (int e = 0; e < length; e++)
            {
                if (indexDepth < kMaxGatherIndexDepth)
                    indexStack[indexDepth] = e;
                pos = nestedStart;
                GatherWalkToEnd(
                    ref Unsafe.AsRef<byte>(itemsBase + (uint)e * stride),
                    null, null,
                    ref pos, nestedEnd, transferState, fnPtr, resolveMissingTypeFnPtr,
                    emitCallbacks, collectMissingTypes, indexStack, nestedDepth);
            }
        }
    }

    private static unsafe void ProcessGatherRecurseStructList(
        ref byte baseAddr, object thisObject, byte* heapObjDataArea,
        ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseStructListEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        uint stride = entry->elementSize;
        pos += sizeof(GatherRecurseStructListEntry);
        byte* nestedStart = pos;
        byte* nestedEnd = nestedStart + nestedBytes;

        object listObj = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (listObj == null)
        {
            pos = nestedEnd;
            return;
        }
        var layout = Unsafe.As<ListLayout>(listObj);
        byte[] itemsBytes = layout._items;
        int size = layout._size;
        if (itemsBytes == null || size == 0)
        {
            pos = nestedEnd;
            return;
        }

        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        fixed (byte* itemsBase = itemsBytes)
        {
            for (int e = 0; e < size; e++)
            {
                if (indexDepth < kMaxGatherIndexDepth)
                    indexStack[indexDepth] = e;
                pos = nestedStart;
                GatherWalkToEnd(
                    ref Unsafe.AsRef<byte>(itemsBase + (uint)e * stride),
                    null, null,
                    ref pos, nestedEnd, transferState, fnPtr, resolveMissingTypeFnPtr,
                    emitCallbacks, collectMissingTypes, indexStack, nestedDepth);
            }
        }
    }

    private static unsafe void ProcessGatherRecurseDictionary(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseDictionaryEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        uint stride = entry->elementSize;
        IntPtr templatePtr = entry->propertyPathTemplate;
        pos += sizeof(GatherRecurseDictionaryEntry);
        byte* nestedStart = pos;
        byte* nestedEnd = nestedStart + nestedBytes;

        object dictObj = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (dictObj == null)
        {
            pos = nestedEnd;
            return;
        }

        // Materialize SerializedKeyValue<K, V>[] via the native
        // DictionarySerializationProxy (the same proxy the main write uses through
        // DictionaryField::GetArray), routed through the GetDictionaryEntriesForGather
        // icall. This avoids a C#-compile-time dependency on
        // UnityEngine.DictionarySerialization — which is NOT present in every native
        // test-resource assembly — while still enumerating dicts in those builds. The
        // gather MUST enumerate the SAME entries the write does: the StreamedBinaryWrite
        // managed opcode for each dict value's [SerializeReference] field consumes a
        // per-host cursor that this enumeration populates (the older "register on the
        // fly during main write" fallback no longer applies, because the opcode pops the
        // cursor instead of calling RegisterReference). To match the write's MERGED
        // (live + preserved-duplicate) array — duplicate rows carry real SR refs that
        // must be gathered — the icall reconstructs the write's FUID context from the
        // baked dict template + this host's refid + the live array-index stack, so the
        // duplicate-row lookup keys identically. Returns null when DictionarySerialization
        // is unavailable, in which case gather harmlessly no-ops on this dict.
        IntPtr dictRaw = Unsafe.As<object, IntPtr>(ref dictObj);
        Array entriesArray = GetDictionaryEntriesForGather(dictRaw, transferState, templatePtr, (IntPtr)indexStack, indexDepth) as Array;
        if (entriesArray == null || entriesArray.Length == 0)
        {
            pos = nestedEnd;
            return;
        }

        // Same reinterpret-and-stride pattern as RecurseStructArray: the T[]
        // backing storage IS a byte[] for `fixed` pinning purposes, and we
        // walk each element inline at (itemsBase + e * stride).
        byte[] arrBytes = Unsafe.As<byte[]>(entriesArray);
        int length = entriesArray.Length;
        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        fixed (byte* itemsBase = arrBytes)
        {
            for (int e = 0; e < length; e++)
            {
                if (indexDepth < kMaxGatherIndexDepth)
                    indexStack[indexDepth] = e;
                pos = nestedStart;
                GatherWalkToEnd(
                    ref Unsafe.AsRef<byte>(itemsBase + (uint)e * stride),
                    null, null,
                    ref pos, nestedEnd, transferState, fnPtr, resolveMissingTypeFnPtr,
                    emitCallbacks, collectMissingTypes, indexStack, nestedDepth);
            }
        }
        pos = nestedEnd;
    }

    private static unsafe void ProcessGatherInvokeOnBeforeSerializeClass(
        object thisObject, ref byte* pos)
    {
        // methodFnPtr is unused for the class path — interface dispatch on
        // the boxed instance picks the correct OnBeforeSerialize override
        // without us having to resolve and call a raw function pointer.
        // The field is kept in the wire layout for symmetry with the struct
        // variant (and so future opcodes that need it don't have to widen
        // the entry).
        //
        // No write-side dedup is needed: a type whose subtree contains a
        // [SerializeReference] field emits ONLY the gather OBS entry (never a
        // write-side InvokeMethodCommand), so this fire is the single OBS
        // invocation for the callsite — see EmitInvokeInterfaceMethodCommandIfRequired.
        pos += sizeof(GatherInvokeOnBeforeSerializeClassEntry);
        if (thisObject == null)
            return;

        try
        {
            (thisObject as ISerializationCallbackReceiver)?.OnBeforeSerialize();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private static unsafe void ProcessGatherInvokeOnBeforeSerializeStruct(
        ref byte baseAddr, ref byte* pos)
    {
        var entry = (GatherInvokeOnBeforeSerializeStructEntry*)pos;
        IntPtr fnPtr = entry->methodFnPtr;
        pos += sizeof(GatherInvokeOnBeforeSerializeStructEntry);
        if (fnPtr == IntPtr.Zero)
            return;

        // No write-side dedup is needed: a struct type whose subtree contains a
        // [SerializeReference] field emits ONLY the gather OBS entry (never a
        // write-side InvokeMethodCommand), so this is the single OBS invocation
        // for the callsite — see EmitInvokeInterfaceMethodCommandIfRequired.

        // Instance methods on value types take their `this` as a managed
        // byref to the struct data (NOT as a boxed MonoObject*). The build
        // side resolves the function pointer via GetMethodFunctionPointer
        // below, which goes through MethodInfo.MethodHandle.GetFunctionPointer
        // — that returns the underlying instance entry, not the unboxing
        // thunk — so passing `ref baseAddr` to the struct's inline data
        // matches the ABI calli expects.
        try
        {
            ((delegate*<ref byte, void>)fnPtr)(ref baseAddr);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // -----------------------------------------------------------------------

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


    // CoreCLR-only interface-method resolver for struct-callback dispatch.
    // Reinterpreting the raw MethodDesc* as a RuntimeMethodHandle and calling
    // GetFunctionPointer aborts for methods in dynamically-loaded ALCs, and even when it
    // resolves, invoking the result through a `delegate*<ref byte, void>` calli mismatches
    // CoreCLR's value-type instance-method calling convention (it works on Mono). So the
    // native side (Common.cpp ResolveInterfaceMethodFunctionPointer, ENABLE_CORECLR) passes
    // the declaring TYPE handle + the interface method NAME, and this returns a GCHandle to
    // an Action<IntPtr> invoker that boxes the struct, invokes the method, and copies the
    // (possibly mutated) struct back. The struct-callback consumers decode the GCHandle
    // under ENABLE_CORECLR. Limitation: Marshal.PtrToStructure/StructureToPtr only handle
    // blittable value types; non-blittable structs throw NotSupportedException here.
    [RequiredByNativeCode]
    internal static IntPtr GetInterfaceMethodFunctionPointer(IntPtr typeHandleValue, string methodName)
    {
        // Not reachable on Mono/IL2CPP — native only calls this under ENABLE_CORECLR
        // (see Common.cpp ResolveInterfaceMethodFunctionPointer).
        return IntPtr.Zero;
    }

    // Consumes one ManagedCommandLinearCollection entry. Three paths share
    // the same header + length prefix, then diverge:
    //   - Trivially-copyable: count*elementStride raw bytes streamed in
    //     one or more chunks, plus a 0..3 byte tail pad.
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
    // Null array / null List → write a 0 length prefix and return (a null
    // collection serialises as a zero-length empty one).
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
            // Wire format: SInt32 length then count*elementStride raw bytes;
            // element counts above int.MaxValue are not representable.
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

    // Consumes one ManagedCommandDictionary entry. Bridges the live
    // Dictionary<K,V> to a SerializedKeyValue<K,V>[] via the existing managed
    // helper, then walks the per-entry FBP-bracketed body once per entry
    // against the entry-pinned base (same shape as ConsumeLinearCollection's
    // per-element-recursion path).
    //
    // FUID stack bracketing: PushDictionaryFUIDFrame installs the dict's
    // FieldUniqueIdentifierContext for descendant FormatDictionaryFieldUniqueIdentifierForActiveContext
    // calls, then Pop on the finally arm. Editor-only behavior; player builds
    // get inline no-op stubs from the native header (Push always returns false,
    // Pop is a nop), so the try/finally is a cheap pair of icalls + a branch.
    //
    // Null dictionary → write a 0 length prefix and return; matches the
    // legacy auto-empty-on-write behavior in TransferField_Dictionary.
    private static unsafe void ConsumeDictionary(
        NativeBufferContext* ctx, ref byte baseAddr, IntPtr transfer,
        ref byte* output, ref int dstSize, ref int pendingAdvance, ref byte* pos)
    {
        var header = (DictionaryHeaderWrite*)pos;
        pos += sizeof(DictionaryHeaderWrite);
        byte* nestedStart = pos;
        int   nestedBytes = (int)header->nestedByteCount;

        // Field at (baseAddr + fieldOffset) holds a Dictionary<K,V> reference.
        object dictRef = Unsafe.As<byte, object>(
            ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
        if (dictRef == null)
        {
            WriteFramedInt32(ctx, 0);
            pos = nestedStart + nestedBytes;
            return;
        }

        bool pushed = SerializationBackendManagedCommands.PushDictionaryFUIDFrame(ctx->fuidContext);
        try
        {
            // Bridge live dict → SerializedKeyValue<K,V>[]. The helper handles
            // duplicate-row merging when the host has stored duplicates and
            // dictionaryIdentifierTemplateUtf8 is non-null; otherwise it just
            // walks the live dict. InvokeGetEntriesTyped uses the build-time
            // interned closed delegate via getEntriesTypedIndex to avoid the
            // per-call dict.GetType() + GetGenericArguments() + ConcurrentDictionary
            // lookup; falls back to the non-typed path when the index is -1.
            Array entries = DictionarySerialization.InvokeGetEntriesTyped(
                header->getEntriesTypedIndex,
                ctx->hostingEntityId, dictRef, header->fieldUniqueIdentifierTemplate);

            int count = entries?.Length ?? 0;
            WriteFramedInt32(ctx, count);

            if (count > 0)
            {
                // Pin the entry array as bytes and walk per-entry via
                // ExecuteWriteCommands — same shape as ConsumeLinearCollection's
                // per-element-recursion arm.
                byte[] dataAsBytes = Unsafe.As<byte[]>(entries);
                fixed (byte* dataPtr = dataAsBytes)
                {
                    long stride = (long)header->entryStride;
                    for (int i = 0; i < count; ++i)
                    {
                        byte* entryPtr = dataPtr + i * stride;
                        ExecuteWriteCommands(ctx, (IntPtr)entryPtr,
                            (IntPtr)nestedStart, nestedBytes, transfer,
                            ref output, ref dstSize, ref pendingAdvance);
                    }
                }
            }

            // Same per-element-recursion commit / pad as ConsumeLinearCollection:
            // flush any bytes the last entry's closing FBP(0) rolled into
            // pendingAdvance, then 0..3 byte aggregate alignment pad.
            if (pendingAdvance > 0)
            {
                InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
                pendingAdvance = 0;
            }

            // entryStride doubles as elementWireSize for the dict path — the
            // probe-built per-entry body's wire bytes equal the managed entry
            // size by construction (SerializedKeyValue<K,V> has no inline
            // arrays/strings on the bulk-memcpy path). For bodies that contain
            // variable-length entries (strings, refs), individual writes are
            // already self-aligned and the aggregate pad is a no-op.
            int totalWritten = count * (int)header->entryStride;
            int padBytes     = (4 - (totalWritten & 3)) & 3;
            if (padBytes > 0)
            {
                Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
                InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
            }
        }
        finally
        {
            if (pushed)
                SerializationBackendManagedCommands.PopDictionaryFUIDFrame();
        }

        pos = nestedStart + nestedBytes;
    }

    // Mirrors ConsumeLinearCollection's trivially-copyable arm, but sourced from
    // an inline buffer at baseAddr + fieldOffset — no array reference, null check,
    // or reflection.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumeFixedBuffer(
        NativeBufferContext* ctx, ref byte baseAddr, ref byte* pos, ref int pendingAdvance)
    {
        var header = (FixedBufferHeader*)pos;
        pos += sizeof(FixedBufferHeader);

        int count       = (int)header->elementCount;
        int totalBytes  = count * (int)header->elementSize;
        int padBytes    = (4 - (totalBytes & 3)) & 3;

        // baseAddr is pinned by the ExecuteWriteCommands caller, so this pointer stays valid.
        byte* dataPtr = (byte*)Unsafe.AsPointer(
            ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));

        // 4-byte count + padded payload, so the record keeps pendingAdvance 4-byte aligned.
        int record = 4 + totalBytes + padBytes;

        // Append the record into the writer window and grow pendingAdvance without
        // flushing, so a run of fixed buffers (and adjacent DirectCopy segments)
        // commits in one flush — the batching the DirectCopy segment path relies on.
        if (ctx->writerAvailable - pendingAdvance >= record)
        {
            byte* dst = ctx->writerPtr + pendingAdvance;
            Unsafe.WriteUnaligned(dst, count);
            Buffer.MemoryCopy(dataPtr, dst + 4, totalBytes, totalBytes);
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(dst + 4 + totalBytes, 0, (uint)padBytes);
            pendingAdvance += record;
            return;
        }

        // Record won't fit alongside the deferred bytes: flush them so it can start
        // at writerPtr, then retry against the refilled window.
        if (pendingAdvance > 0)
        {
            InvokeFlushBuffer(ctx, ctx->writerPtr, pendingAdvance);
            pendingAdvance = 0;
        }

        if (ctx->writerAvailable >= record)
        {
            Unsafe.WriteUnaligned(ctx->writerPtr, count);
            Buffer.MemoryCopy(dataPtr, ctx->writerPtr + 4, totalBytes, totalBytes);
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(ctx->writerPtr + 4 + totalBytes, 0, (uint)padBytes);
            pendingAdvance = record;
            return;
        }

        // Payload exceeds a whole window: frame the count, then hand the inline
        // source to FlushBuffer's spill arm so it crosses any number of cache-writer
        // blocks in one flush. The tail pad always fits — FlushBuffer leaves
        // writerAvailable >= kManagedBlockMaxPayloadSize.
        WriteFramedInt32(ctx, count);
        InvokeFlushBuffer(ctx, dataPtr, totalBytes);
        if (padBytes > 0)
        {
            Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
        }
    }

    // EntityId <-> 12-byte LocalSerializedObjectIdentifier, the pure-managed
    // counterparts of PackEntityIdIntoLSOI / UnpackEntityIdFromLSOI (BaseObject.h):
    // low 32 bits -> localSerializedFileIndex, high 32 bits -> localIdentifierInFile.
    // Used for the clone (PackEntityIdInLSOI) path and for EntityId.None, which
    // encodes as a zero record with no native call. The record is only 4-byte
    // aligned, so both halves go through the unaligned intrinsics.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void PackEntityIdIntoLsoi(byte* dst, ulong entityId)
    {
        Unsafe.WriteUnaligned<int>(dst, (int)(entityId & 0xFFFFFFFFu));
        Unsafe.WriteUnaligned<long>(dst + 4, (long)(entityId >> 32));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong UnpackEntityIdFromLsoi(byte* src)
    {
        uint lo = (uint)Unsafe.ReadUnaligned<int>(src);
        uint hi = (uint)Unsafe.ReadUnaligned<long>(src + 4);
        return ((ulong)hi << 32) | lo;
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

    // Reads a length-prefixed UTF-8 string. Editor PropertyName name read path only.
    private static unsafe string ReadFramedString(NativeReadBufferContext* ctx)
    {
        // Length prefix: 4-byte SInt32, little-endian.
        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int length = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        if (length < 0)
            throw new InvalidOperationException(
                $"Managed PropertyName deserialization read a negative length prefix ({length}). The serialized data is corrupted.");

        int padBytes = (4 - (length & 3)) & 3;

        string result;
        if (length == 0)
        {
            result = string.Empty;
        }
        else if (length <= ctx->stackBufferSize)
        {
            if (ctx->readerAvailable < length)
                InvokeEnsureReadable(ctx, length);
            result = DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr      += length;
            ctx->readerAvailable -= length;
        }
        else
        {
            byte[] buf = new byte[length];
            fixed (byte* bufPtr = buf)
            {
                InvokeReadBytesDirect(ctx, bufPtr, length);
                result = DecodeStringBody(bufPtr, length);
            }
        }

        // Skip 0-3 bytes of alignment padding.
        if (padBytes > 0)
        {
            if (ctx->readerAvailable < padBytes)
                InvokeEnsureReadable(ctx, padBytes);
            ctx->readerPtr      += padBytes;
            ctx->readerAvailable -= padBytes;
        }
        return result;
    }

    // Reads [SInt32 len][ascii digits][0..3 pad] and parses the decimal straight from the
    // wire into an Int32 — no managed string, no int.Parse. Avoids a per-field string
    // allocation, whose GC cost dominates on Mono/IL2CPP. long accumulator so
    // Int32.MinValue ("-2147483648") round-trips.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int ReadFramedDecimalInt32(NativeReadBufferContext* ctx)
    {
        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int length = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        // An Int32 decimal is at most 11 bytes ("-2147483648"). Anything longer is corrupt;
        // rejecting it also keeps the digit loop inside the spill buffer (the reader caps a
        // fill at stackBufferSize, so an unbounded length would read out of bounds).
        if (length < 0 || length > 11)
            throw new InvalidOperationException(
                $"Managed PropertyName deserialization read an invalid decimal length prefix ({length}). The serialized data is corrupted.");

        // Writer always emits ≥1 digit (id 0 → "0"), so length 0 only arises from corruption; treat as 0.
        long magnitude = 0;
        bool negative = false;
        if (length > 0)
        {
            if (ctx->readerAvailable < length)
                InvokeEnsureReadable(ctx, length);
            byte* p = ctx->readerPtr;
            int i = 0;
            if (p[0] == (byte)'-')
            {
                negative = true;
                i = 1;
            }
            for (; i < length; i++)
                magnitude = magnitude * 10 + (p[i] - (byte)'0');
            ctx->readerPtr      += length;
            ctx->readerAvailable -= length;
        }

        int padBytes = (4 - (length & 3)) & 3;
        if (padBytes > 0)
        {
            if (ctx->readerAvailable < padBytes)
                InvokeEnsureReadable(ctx, padBytes);
            ctx->readerPtr      += padBytes;
            ctx->readerAvailable -= padBytes;
        }
        return negative ? (int)(-magnitude) : (int)magnitude;
    }

    // Read side of the PropertyName opcode. Reconstructs the PropertyName exactly as
    // SerializeTraits<PropertyName> does: id off the wire, or the name resolved in the editor.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumePropertyNameRead(
        NativeReadBufferContext* ctx, ref byte baseAddr, ref byte* pos)
    {
        var entry = (ManagedCommandPropertyNameEntry*)pos;
        pos += sizeof(ManagedCommandPropertyNameEntry);
        ref byte field = ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);

        PropertyName pn;
        // serializesAsId == 0 means the editor persisted the resolved name; otherwise the id.
        if (entry->serializesAsId == 0)
            pn = new PropertyName(ReadFramedString(ctx));               // == PropertyNameFromString(s)
        else
            pn = new PropertyName(ReadFramedDecimalInt32(ctx));
        // Write the whole struct (8 B editor, 4 B player). In the editor the ctor sets
        // conflictIndex — resolved from the name, or zeroed from an id — matching the native read.
        Unsafe.WriteUnaligned(ref field, pn);
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
        IntPtr transfer,
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
        // WriteFramedInt32(0) with no body). A non-null zero-length collection
        // is the contract user OnAfterDeserialize callbacks (e.g. UpmCache
        // iterating `m_SerializedProductSearchPackageInfoProductIds.Length`)
        // rely on. Skipping the assignment here would leave the field at its
        // CLR default (null) and silently break that contract — observed as
        // a NullReferenceException in UpmCache.OnAfterDeserialize during
        // code-reload backup restore.
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
                            transfer,
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

    // Read-path mirror of ConsumeDictionary. Reads the count prefix and per-entry
    // body (same shape ConsumeLinearCollectionRead's per-element-recursion path
    // produces) into a SerializedKeyValue<K,V>[] staging array, then calls
    // DictionarySerialization.SetEntriesFromSerializedData to populate the live
    // dictionary and store any duplicate-key entries in the Editor cache.
    //
    // FUID bracketing: PushDictionaryFUIDFrame installs the dict's
    // FieldUniqueIdentifierContext so FormatDictionaryFieldUniqueIdentifier
    // (which walks the FUID stack) produces the canonical dict path used as
    // the duplicate-row storage key — must match what the legacy DictionaryField::SetArray
    // produces (DictionaryField.cpp:142-144) so write→read round-trips through
    // the cache are stable.
    //
    // Duplicate-key warning: when ctx->warnOnDuplicates is set (serialized-file
    // load or Object.Instantiate clone) AND SetEntriesFromSerializedData reports
    // hadDuplicates AND we have a non-empty dictionary identifier, emit the
    // clickable Console warning via LogDictionaryDuplicateKeyWarning — same
    // flags + EntityId hookup as DictionaryField::LogDuplicateKeyWarning.
    private static unsafe void ConsumeDictionaryRead(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        IntPtr transfer,
        ref byte* pos)
    {
        var header = (DictionaryHeaderRead*)pos;
        pos += sizeof(DictionaryHeaderRead);
        byte* nestedStart = pos;
        int   nestedBytes = (int)header->nestedByteCount;

        // Count prefix — same framing as ConsumeLinearCollectionRead.
        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        // Allocate the staging entries array. elementTypeHandle was stamped at
        // build time with the SerializedKeyValue<K,V> RuntimeTypeHandle.Value;
        // UnmarshalSystemType handles the Mono/IL2CPP vs CoreCLR backend split.
        Type entryType = UnmarshalSystemType(header->elementTypeHandle);
        Array entries  = Array.CreateInstance(entryType, count);

        bool pushed = PushDictionaryFUIDFrame(ctx->fuidContext);
        try
        {
            if (count > 0)
            {
                // Per-entry recursion: each entry's FBP-bracketed body walked
                // by ExecuteReadCommands with the entry pinned — same shape as
                // ConsumeLinearCollectionRead's per-element-recursion arm.
                byte[] dataAsBytes = Unsafe.As<byte[]>(entries);
                fixed (byte* dataPtr = dataAsBytes)
                {
                    long stride  = (long)header->entryStride;
                    int  segSize = 0;
                    for (int i = 0; i < count; ++i)
                    {
                        byte* entryBase = dataPtr + (long)i * stride;
                        ExecuteReadCommands(
                            ctx,
                            ref Unsafe.AsRef<byte>(entryBase),
                            nestedStart, nestedBytes,
                            transfer,
                            ref segSize);
                    }
                }

                // Skip the 0..3-byte tail pad written by the per-entry write
                // path. entryStride doubles as the per-entry wire size here
                // (SerializedKeyValue<K,V> has no inline arrays/strings on
                // the bulk-memcpy path); for bodies with variable-length
                // entries individual writes self-align and the pad is 0.
                int totalBytes = count * (int)header->entryStride;
                int padBytes   = (4 - (totalBytes & 3)) & 3;
                if (padBytes > 0)
                {
                    if (ctx->readerAvailable < padBytes)
                        InvokeEnsureReadable(ctx, padBytes);
                    ctx->readerPtr      += padBytes;
                    ctx->readerAvailable -= padBytes;
                }
            }

            // Field at (baseAddr + fieldOffset) holds a Dictionary<K,V> reference.
            // Default-allocate when the live reference is null so the deserialized
            // host has a usable (possibly empty) dictionary instead of a null field --
            // matches legacy DictionaryField's ctor at DictionaryField.cpp:100-102
            // ("Default-allocate the dictionary if the field is null, matching
            // List<T>/array behavior"). The Func<object> factory was interned
            // once at build time by DictionarySerialization.InternDictionaryDefaultAllocateFactory
            // and the integer index stamped into the dict header; here we pull it
            // back via SerializationCommandObjectTable and invoke directly -- no
            // execute-time reflection, no hash lookup, no per-call generic dispatch.
            // Index -1 means the helper was unavailable at build time; leave the
            // field null in that case.
            ref byte dictSlot = ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset);
            object dictRef = Unsafe.As<byte, object>(ref dictSlot);
            if (dictRef == null && header->dictDefaultAllocateFactoryIndex >= 0)
            {
                var factory = (Func<object>)SerializationCommandObjectTable.Get(header->dictDefaultAllocateFactoryIndex);
                dictRef = factory();
                Unsafe.As<byte, object>(ref dictSlot) = dictRef;
            }
            if (dictRef != null)
            {
                // Resolve the dict's canonical identifier so SetEntriesFromSerializedData
                // can key the duplicate-row cache by it. Empty when no FUID context or
                // no template — duplicate-row tracking simply doesn't apply in those cases.
                string dictionaryIdentifier = string.Empty;
                if (ctx->hostingEntityId != EntityId.None
                    && header->fieldUniqueIdentifierTemplate != IntPtr.Zero)
                {
                    dictionaryIdentifier = FormatDictionaryFieldUniqueIdentifier(
                        header->fieldUniqueIdentifierTemplate) ?? string.Empty;
                }

                // InvokeSetEntriesTyped uses the build-time interned closed
                // delegate via setEntriesTypedIndex to avoid the per-call
                // dict.GetType() + ConcurrentDictionary lookup; falls back to
                // the non-typed SetEntriesFromSerializedData entry point when
                // the index is -1.
                bool hadDuplicates;
                DictionarySerialization.InvokeSetEntriesTyped(
                    header->setEntriesTypedIndex,
                    ctx->hostingEntityId, dictRef, entries, dictionaryIdentifier, out hadDuplicates);

                // Warn-on-duplicates policy mirrors the legacy DictionaryField::SetArray
                // path: only fires for serialized-file loads + Object.Instantiate clones
                // (ctx->warnOnDuplicates set by the native dispatcher), and only when
                // we actually have a formatted identifier — without one we can't tell
                // the user which dictionary field is affected.
                if (ctx->warnOnDuplicates && hadDuplicates && !string.IsNullOrEmpty(dictionaryIdentifier))
                {
                    string message =
                        "Dictionary field '" + dictionaryIdentifier + "' contains duplicate key entries. " +
                        "Ensure all keys are unique. Only the first occurrence of each key will be added " +
                        "to the dictionary object.";
                    LogDictionaryDuplicateKeyWarning(message, ctx->hostingEntityId);
                }
            }
        }
        finally
        {
            if (pushed)
                PopDictionaryFUIDFrame();
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

        // A negative length would sign-extend into an over-read in InvokeReadBytesDirect.
        if (wireCount < 0)
            throw new InvalidOperationException(
                $"Managed fixed-buffer deserialization read a negative length prefix ({wireCount}). The serialized data is corrupted.");

        int elementSize = header->elementSize;
        int capacity    = (int)header->elementCount;
        int copyCount   = wireCount < capacity ? wireCount : capacity;
        int copyBytes   = copyCount * elementSize;
        long wireBytesL = (long)wireCount * (long)elementSize;
        int  alignBytes = (int)((4 - (wireBytesL & 3)) & 3);

        if (copyBytes > 0)
        {
            byte* dstPtr = (byte*)Unsafe.AsPointer(
                ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));

            // Stream straight into the inline buffer: ReadBytesDirect drains any
            // already-buffered bytes and reads the remainder in a single call,
            // spanning any number of cache-reader blocks at any size.
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
    // self-contained FBP(N)..FBP(0) chain, so ExecuteReadCommands gets a fresh
    // innerSegmentSize=0. Same class / struct split — see ConsumeValueReference.
    private static unsafe void ConsumeValueReferenceRead(
        NativeReadBufferContext* ctx, ref byte baseAddr, IntPtr transfer, ref byte* pos)
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

        if (header->runtimeTypeHandle == IntPtr.Zero)
        {
            // Struct: inline; outer pin covers this recursion (see ConsumeValueReference).
            ref byte nestedBase = ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset);
            int innerSegmentSize = 0;
            ExecuteReadCommands(
                ctx,
                ref nestedBase,
                nestedStart, nestedBytes,
                transfer,
                ref innerSegmentSize);
        }
        else
        {
            // Class: `fixed` pins the inner instance across native P/Invokes in the recursion.
            object obj = GetOrCreateVrtInstance(ref baseAddr, header->fieldOffset, header->runtimeTypeHandle, header->ctorFunctionPtr);
            fixed (byte* nestedBase = &Unsafe.As<ObjectWrapper>(obj).Data)
            {
                int innerSegmentSize = 0;
                ExecuteReadCommands(
                    ctx,
                    ref Unsafe.AsRef<byte>(nestedBase),
                    nestedStart, nestedBytes,
                    transfer,
                    ref innerSegmentSize);
            }
        }

        pos = nestedStart + nestedBytes;
    }

    // Read counterpart of ObjectToSerializationBuffer.
    [RequiredByNativeCode]
    public static unsafe void SerializationBufferToObject(
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int entryBufferSize,
        IntPtr readContext,
        IntPtr transfer)
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
            transfer,
            ref currentSegmentSize);
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
        IntPtr transfer,
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

                // Each entry's field-table slot is forwarded to
                // ReadUnityObjectFromBuffer so a resolver-miss becomes an editor
                // fake-null wrapper that keeps the EntityId for re-save, matching
                // the native Transfer_UnityEngineObject path.
                case RttiDataType.UnityObject:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectReadEntry>(ref pos, out var end);
                    int count = (int)(end - entry);
                    // The field-table mirrors FixedSegment_UnityObjectRead_Emit's
                    // memcpy on the native side: pos lands at a 4-byte-aligned
                    // offset (4B group header + count * 16B Pack=4 entries), so
                    // direct IntPtr* deref is UB on arm64 / SIGBUS under IL2CPP.
                    byte* fieldTableBase = pos;
                    pos += count * 2 * sizeof(IntPtr);

                    int i = 0;
                    do
                    {
                        ref object fieldRef = ref Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset));
                        byte* src = input + entry->destOffset;

                        byte* slotBase = fieldTableBase + i * 2 * sizeof(IntPtr);
                        IntPtr fieldPtr       = Unsafe.ReadUnaligned<IntPtr>(slotBase);
                        IntPtr fieldParentPtr = Unsafe.ReadUnaligned<IntPtr>(slotBase + sizeof(IntPtr));

                        fieldRef = ReadUnityObjectFromBuffer(
                            ctx->resolverHandle, (IntPtr)src, entry->klass, ctx->flags,
                            fieldPtr, fieldParentPtr);
                        entry++;
                        i++;
                    }
                    while (entry < end);
                    break;
                }

                // [SerializeReference] inline RefId read. Same on-wire shape as the
                // ManagedReference write case (UnityObjectWriteEntry: {fieldOffset,
                // destOffset}) — the build emits one descriptor for both directions
                // because the per-entry layout is identical. The icall reads 8 bytes
                // from the wire, activates the managed-references state (so the
                // `references:` blob is consumed into the registry), and registers a
                // deferred fixup; the existing PerformFixups flow resolves it once
                // the registry blob has been read. transferState / instance come
                // from the read context (set by Transfer_ManagedBlock_StreamedBinaryRead);
                // both are non-null whenever this opcode appears (build only emits
                // it for SR fields in StreamedBinaryRead transfers). SR collection
                // elements stay on the native ManagedRefArrayItemTransferer arm,
                // which calls RegisterFixupRequest per element on its own — no
                // cursor coordination needed on read.
                case RttiDataType.ManagedReference:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectWriteEntry>(ref pos, out var end);
                    do
                    {
                        byte* src = input + entry->destOffset;
                        ReadManagedReferenceFromBuffer(ctx->transferState, ctx->instance, (int)entry->fieldOffset, (IntPtr)src);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.EntityId:
                {
                    var entry = ConsumeDirectCopyGroup<EntityIdReadEntry>(ref pos, out var end);
                    bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
                    do
                    {
                        byte* src = input + entry->destOffset;
                        ulong entityId = packInLSOI
                            ? UnpackEntityIdFromLsoi(src)
                            : s_readEntityIdFromBuffer(ctx->resolverHandle, (IntPtr)src, ctx->flags);
                        ref byte fieldByteRef = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                        Unsafe.WriteUnaligned<ulong>(ref fieldByteRef, entityId);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.Array:
                case RttiDataType.List:
                {
                    ConsumeLinearCollectionRead(ctx, ref baseAddr, transfer, ref pos);
                    break;
                }

                case RttiDataType.Dictionary:
                {
                    ConsumeDictionaryRead(ctx, ref baseAddr, transfer, ref pos);
                    break;
                }

                case RttiDataType.FixedBuffer:
                {
                    ConsumeFixedBufferRead(ctx, ref baseAddr, ref pos);
                    break;
                }

                case RttiDataType.ValueReferenceType:
                {
                    ConsumeValueReferenceRead(ctx, ref baseAddr, transfer, ref pos);
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

                case RttiDataType.NativeValueStruct:
                {
                    var entry = (ManagedCommandNativeValueStructEntry*)pos;
                    pos += sizeof(ManagedCommandNativeValueStructEntry);

                    // Inline value struct: the storage is inline (no wrapper to
                    // construct), so just hand the field's own address to the native
                    // Transfer dispatcher. baseAddr is caller-pinned.
                    ref byte nvsField = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                    IntPtr nvsFieldPtr = (IntPtr)Unsafe.AsPointer(ref nvsField);

                    // Dispatch reads straight off the CachedReader; rewind it first.
                    InvokeSyncReader(ctx);
                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)entry->fnPtr)(nvsFieldPtr, transfer, IntPtr.Zero);
                    break;
                }

                case RttiDataType.SimpleNativeType:
                {
                    var entry = (ManagedCommandSimpleNativeTypeReadEntry*)pos;
                    pos += sizeof(ManagedCommandSimpleNativeTypeReadEntry);

                    // Wrapper field is a reference slot in the host instance. If null,
                    // construct via the entry's runtimeTypeHandle + ctorFunctionPtr so
                    // the wrapper's parameterless ctor runs and allocates the native peer.
                    // Init refuses registration when ctorFunctionPtr would be zero, so we
                    // can rely on a usable m_Ptr after construction.
                    ref object slot = ref Unsafe.As<byte, object>(
                        ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset));
                    object wrapper = slot;
                    if (wrapper == null)
                    {
                        wrapper = CreateWrapperInstance(entry->runtimeTypeHandle, entry->ctorFunctionPtr);
                        slot = wrapper;
                    }

                    // m_Ptr lives at userData bytes past the wrapper's post-header data
                    // start (offset queried by scripting_field_get_offset at init time
                    // so it's correct for the active scripting backend).
                    IntPtr nativePtr = Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
                        ref Unsafe.As<ObjectWrapper>(wrapper).Data,
                        entry->userData));

                    // Dispatch reads straight off the CachedReader, which EnsureReadable
                    // has pre-fetched past the cursor; rewind it before handing over.
                    InvokeSyncReader(ctx);

                    // Reads the wire bytes into the native peer.
                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)entry->fnPtr)(nativePtr, transfer, entry->userData);

                    // Managed post-deserialize hook for wrappers that opted in
                    // (e.g. GUIStyle.InternalOnAfterDeserialize).
                    if (entry->managedPostDispatchFnPtr != IntPtr.Zero)
                        ((delegate*<object, IntPtr, void>)entry->managedPostDispatchFnPtr)(wrapper, nativePtr);
                    break;
                }

                case RttiDataType.PropertyNameId:
                    ConsumePropertyNameRead(ctx, ref baseAddr, ref pos);
                    break;

                case RttiDataType.DirectCopyBlock:
                case RttiDataType.Reference:
                case RttiDataType.DynamicBuffer:
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
