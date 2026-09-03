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

    // Write-only: one crossing per array instead of the generic Array/List + per-element body.
    UnityObjectArray        = 35,

    // EntityId counterpart of UnityObjectArray. Runs on every runtime — stores values, not managed references.
    EntityIdArray           = 36,

    // LinearCollection counterpart of ManagedReference. Write-only.
    ManagedReferenceArray   = 37,

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

// No klass or field-table — the id is the field value; read and write entries are identical.
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

// Mirrors ManagedCommandFixedBlockPrefix in SerializationCommands.h. Opens
// every DC segment with a leading `FBP(N)`; the consumer commits the segment
// at the next boundary. See the native header for the full description.
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

// Write-only — read layout has additional fields (UnityObjectArrayReadHeader).
internal struct UnityObjectArrayHeader
{
    public RttiDataType opCode;        // = RttiDataType.UnityObjectArray
    public byte         kind;          // 0 = Array, 1 = List
    public byte         reserved0;
    public byte         reserved1;
    public uint         fieldOffset;   // post-header offset of the collection reference on the parent
    public uint         elementStride; // bytes between elements in the managed backing array
}

// Uniform fake-null context (klass/field/fieldParent) covers every element. 48 bytes on 64-bit.
internal struct UnityObjectArrayReadHeader
{
    public RttiDataType opCode;            // = RttiDataType.UnityObjectArray
    public byte         kind;              // 0 = Array, 1 = List
    public byte         reserved0;
    public byte         reserved1;
    public uint         fieldOffset;       // post-header offset of the collection reference on the parent
    public uint         elementStride;     // bytes between elements in the managed backing array
    public uint         reserved2;         // pad to align the pointer-sized members
    public IntPtr       elementTypeHandle; // RuntimeTypeHandle.Value for Array.CreateInstance
    public IntPtr       klass;             // native element class (resolve + editor fake-null type)
    public IntPtr       field;             // array field backend ptr (editor fake-null)
    public IntPtr       fieldParent;       // array field's declaring class backend ptr (editor fake-null)
}

internal struct EntityIdArrayHeader
{
    public RttiDataType opCode;        // = RttiDataType.EntityIdArray
    public byte         kind;          // 0 = Array, 1 = List
    public byte         reserved0;
    public byte         reserved1;
    public uint         fieldOffset;   // post-header offset of the collection reference on the parent
    public uint         elementStride; // bytes between elements in the managed backing array
}

internal struct ManagedReferenceArrayHeader
{
    public RttiDataType opCode;        // = RttiDataType.ManagedReferenceArray
    public byte         kind;          // 0 = Array, 1 = List
    public byte         reserved0;
    public byte         reserved1;
    public uint         fieldOffset;   // post-header offset of the collection reference on the parent
}

// No klass/field/fieldParent — EntityId decodes to a value, no fake-null context. 24 bytes on 64-bit.
internal struct EntityIdArrayReadHeader
{
    public RttiDataType opCode;            // = RttiDataType.EntityIdArray
    public byte         kind;              // 0 = Array, 1 = List
    public byte         reserved0;
    public byte         reserved1;
    public uint         fieldOffset;       // post-header offset of the collection reference on the parent
    public uint         elementStride;     // bytes between elements in the managed backing array
    public uint         reserved2;         // pad to align elementTypeHandle on an 8-byte boundary
    public IntPtr       elementTypeHandle; // RuntimeTypeHandle.Value for Array.CreateInstance
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

// Mirrors ManagedCommandDictionaryWrite in SerializationCommands.h. Body of
// nestedByteCount bytes immediately follows (per-entry FBP-bracketed DC +
// optional String body, walked once per SerializedKeyValue<K,V> entry against
// the entry-pinned base).
internal struct DictionaryHeaderWrite  // 28 bytes (mirrors ManagedCommandDictionaryWrite)
{
    public RttiDataType opCode;                        // = RttiDataType.Dictionary
    public byte         reserved0;
    public byte         reserved1;
    public byte         reserved2;
    public uint         fieldOffset;                   // post-header offset of the dictionary reference on the parent
    public uint         entryStride;                   // sizeof(SerializedKeyValue<K,V>)
    public uint         nestedByteCount;               // bytes of FBP-bracketed body that follow
    public int          getEntriesTypedIndex;          // SerializationCommandObjectTable index for closed GetEntriesTyped<K,V>; -1 = falls back to non-typed entry point
    // Editor-only FUID template stored INLINE right after the nestedByteCount body
    // (null-terminated; strlen+1 here, 0 in player). The template pointer is
    // (bodyStart + nestedByteCount); next-entry advance adds align4(fuidTemplateByteCount).
    public uint         fuidTemplateByteCount;
    public uint         entryWireSize;                 // per-entry wire width; 0 = self-aligned
}

// Mirrors ManagedCommandDictionaryRead in SerializationCommands.h. Same opcode
// value as DictionaryHeaderWrite — the dispatchers live in separate switches
// (write inside ObjectsToSerializationBuffer, read inside SerializationBufferToObjects)
// so opcode reuse is unambiguous.
internal struct DictionaryHeaderRead  // 32 + sizeof(IntPtr) bytes (mirrors ManagedCommandDictionaryRead)
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
    // Editor-only FUID template stored INLINE after the body (see DictionaryHeaderWrite).
    public uint         fuidTemplateByteCount;
    public uint         entryWireSize;                   // per-entry wire width; 0 = self-aligned. Also 8-byte aligns elementTypeHandle.
    public IntPtr       elementTypeHandle;               // SerializedKeyValue<K,V> RuntimeTypeHandle.Value for Array.CreateInstance
}
