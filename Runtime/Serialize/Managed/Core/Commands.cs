// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2: the shared command definitions. This is the C#
// mirror of Runtime/Serialize/Managed/Core/Commands.h (opcodes, command
// structs, pad0 masks) plus the frame struct both executors share
// (V2ObjectFrame). Wire layout changes land in Commands.h and here in the
// same commit; the static_asserts on the native side pin the sizes these
// mirrors assume.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Mirror of Runtime/Serialize/Managed/Core/Commands.h; must stay in sync.
    internal enum V2OpCode : byte
    {
        End = 0,
        FixedBlock = 1,
        DirectCopy4 = 2,
        DirectCopy1 = 3,
        DirectCopy2 = 4,
        EnterObject = 5,
        EnterObjectFallback = 6,
        ExitObject = 7,
        String = 8,
        LinearCollectionMemCpy = 9,
        ExternalFixed = 10,
        ExternalDynamic = 11,
        ExternalArray = 12,
        EnterElementSource = 13,
        InvokeCallback = 14,
        DirectCopyRun = 15,
        // Batched copy loops, one opcode per (copy size x entry width); entry
        // offsets are pre-scaled by the copy alignment (dest always /4).
        DirectCopy4N_8 = 16,
        DirectCopy4N_16 = 17,
        DirectCopy4N_32 = 18,
        DirectCopy2N_8 = 19,
        DirectCopy2N_16 = 20,
        DirectCopy2N_32 = 21,
        DirectCopy1N_8 = 22,
        DirectCopy1N_16 = 23,
        DirectCopy1N_32 = 24,
        DirectCopy4N_8x4 = 25,
        EnterObjectNoCtor = 26,
        FixedBlockGuarded = 27,
        ExitObjectSimple = 28,
        DirectCopy4Unaligned = 29,
        DirectCopy2Unaligned = 30,
        // Exec-only stamped exit of an element-source body: the publish
        // lowering replaces the canonical CollectionExit marker closing a
        // EnterElementSource with this, which carries the loop facts
        // (backedge distance, stride, window guarantees) so the executors
        // keep no per-loop state beyond the frame's Offset/EndOffset cursor.
        ExitElementSourceExec = 31,
        // Exec-only stream header, always the first command of a published
        // stream; the executor wrappers consume and skip it.
        StreamHeader = 32,
        // EnterObject through the class's registered factory (one calli owns
        // allocate-and-construct); constructorIndex feeds the catch arm only.
        EnterObjectFactory = 33,
        // Collection commands through a sized factory; the plain variants keep
        // the reflection path. Resolved at compose, never in the executor.
        LinearCollectionMemCpyFactory = 34,
        // Exec-only collection loop, one test (Offset + stride <= EndOffset)
        // in three positions. See Commands.h for the shapes. Names say what
        // each arm does: <fit>Or<failing>, where Exit means pop the frame.
        // Every collection lowers to these except EnterElementSource.
        CollectionEnterOrSkip = 35,
        CollectionEnterOrSkipFactory = 36,
        CollectionBulkTail = 37,
        CollectionRepeatOrFallthrough = 38,
        CollectionRepeatOrExit = 39,
        // Gather-stream forms of ExternalFixed / ExternalArray, identical in
        // layout: their batched handler also takes the command's FUID segments
        // and the live frames. Stamped by V2BuildGatherStream only.
        ExternalFixedWithFUID = 40,
        ExternalArrayWithFUID = 41,
        // A gather-stream bracket: the gather owns its only fire, so it is not
        // gated on the null-slot contract.
        InvokeCallbackIgnoresNullSlot = 42,
        // COMPOSER-ONLY opcodes, kept together at the end so the executed set
        // stays dense from 0: they describe a type rather than being code, the
        // publish lowering rewrites every one of them, and reaching an
        // executor is a lowering bug (the switches default-throw). Mirrored
        // here only to keep the two enums identical.
        CollectionEnter = 43,
        CollectionEnterFactory = 44,
        CollectionExit = 45,
        MergeableCopyList = 46,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdFixedBlock          // 8 bytes
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint wireBytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdDirectCopy          // 12 bytes; shared by DC4/DC1/DC2
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;             // relative to the current frame
        public uint destOffset;              // relative to the enclosing fixed block
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdDirectCopyRun       // 16 bytes; coalesced copy stretch, memcpy both directions
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;             // relative to the current frame
        public uint destOffset;              // relative to the enclosing fixed block
        public uint byteCount;               // multiple of 4, >= 12
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdDirectCopyN         // 8 bytes + count packed entries (padded to a 4-byte boundary)
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public ushort count;
        public ushort pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CopyEntry8             // 2 bytes; offsets pre-scaled
    {
        public byte fieldOffset;
        public byte destOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CopyEntry16            // 4 bytes; offsets pre-scaled
    {
        public ushort fieldOffset;
        public ushort destOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CopyEntry32            // 8 bytes; offsets pre-scaled
    {
        public uint fieldOffset;
        public uint destOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdEnterObject         // 16 bytes
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;             // reference slot, relative to the current frame
        public int constructorIndex;         // registered constructor data; always >= 0 for these opcodes
        public uint pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdEnterObjectFallback // 24 bytes
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;
        public ulong runtimeTypeHandle;
        public ulong ctorFunctionPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdEnterObjectFactory  // 24 bytes
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;
        public ulong factoryFunctionPtr;     // registered factory: allocate-and-construct in one calli
        public int constructorIndex;         // interned {Type, ...} entry; catch arm only
        public uint pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdExitObject          // 4 bytes
    {
        public byte op;
        public byte pad0;
        public ushort size4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdString              // 12 bytes
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;
        public ushort ensureSum;             // doc §12: window bytes to guarantee after this command
        public ushort pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdFixedBlockGuarded   // 12 bytes; self-guaranteeing block form (doc §12)
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint wireBytes;
        public uint ensureSum;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdLinearCollectionMemCpy // 24 bytes
    {
        public byte op;
        public byte kind;                    // 0 = array, 1 = list
        public ushort size4;
        public uint fieldOffset;
        public uint elementStride;
        public ushort ensureSum;             // doc §12
        public ushort pad2;
        public ulong elementTypeHandle;      // READ-side reuse-or-allocate; writes ignore it
    }

    // Factory variant: identical layout with the element handle replaced by
    // the sized-factory calli target. Which variant runs is resolved at
    // compose, never in the executor.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdLinearCollectionMemCpyFactory // 24 bytes
    {
        public byte op;
        public byte kind;                    // 0 = array, 1 = list
        public ushort size4;
        public uint fieldOffset;
        public uint elementStride;
        public ushort ensureSum;             // doc §12
        public ushort pad2;
        public ulong sizedFactoryPtr;
    }

    // Exec exit for element-source bodies; at exhaust the executor recovers
    // the Enter command at (next - bodyBytes - sizeof(V2CmdEnterElementSource))
    // for the sink dispatch.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdExitElementSourceExec // 16 bytes; mirror of Commands.h
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint stride;
        public uint bodyBytes;
        public ushort bodyEnsureSum;
        public ushort exitEnsureSum;
    }

    // Exec-only stream header; the executor wrappers consume and skip it.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdStreamHeader        // 8 bytes; mirror of Commands.h
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public ushort maxFrameDepth;         // frame-stack slots the executor needs (1 = root only)
        public ushort pad;
    }

    // Collection loop commands (mirror of Commands.h). Setup plus the first
    // test; stride is what this position tests (the chunk on bulk sites),
    // elementStride the frame extent and FUID recovery.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdCollectionEnterOrSkip     // 32 bytes; kCollectionEnterOrSkip
    {
        public byte op;
        public byte kind;                    // V2CollectionKind low nibble
        public ushort size4;
        public uint fieldOffset;
        public uint elementStride;
        public uint delta;                   // skip: one-past-this-command + delta = the next test
        public ushort bodyEnsureSum;         // doc §12: body arm
        public ushort exitEnsureSum;         // doc §12: skip arm
        public uint stride;
        public ulong elementTypeHandle;      // READ: reuse-or-allocate
    }

    // Factory variant of V2CmdCollectionEnterOrSkip.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdCollectionEnterOrSkipFactory // 32 bytes; kCollectionEnterOrSkipFactory
    {
        public byte op;
        public byte kind;
        public ushort size4;
        public uint fieldOffset;
        public uint elementStride;
        public uint delta;
        public ushort bodyEnsureSum;
        public ushort exitEnsureSum;
        public uint stride;
        public ulong sizedFactoryPtr;
    }

    // EnterOrSkip without the setup: the element body's test on bulk sites.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdCollectionBulkTail  // 16 bytes; mirror of Commands.h
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint stride;             // elementStride at this position
        public uint delta;                   // skip: one-past-this-command + delta = the element latch
        public ushort bodyEnsureSum;         // doc §12: body arm
        public ushort exitEnsureSum;         // doc §12: skip arm
    }

    // The two latches on one layout: advance, then re-test — backedge on a
    // fit, else fall through, popping the frame on OrExit.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdCollectionRepeat    // 16 bytes; mirror of Commands.h
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint stride;
        public uint delta;                   // backedge: one-past-this-command - delta = body start
        public ushort bodyEnsureSum;         // doc §12: backedge arm
        public ushort exitEnsureSum;         // doc §12: fall-through arm
    }

    private const byte kV2Pad0KindMask = 0x0F;
    private const byte kV2Pad0FlagCallbackRead = 0x10;
    private const byte kV2Pad0FlagPushFuidFrame = 0x20;
    private const byte kV2Pad0FlagDictKeyHasCallbacks = 0x40;
    private const byte kV2Pad0FlagEnterNoFrame = 0x01;
    private const byte kV2CallbackFlavorMask = 0x0F;

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdExternalFixedGroup  // 40 bytes + count entries (+ read tables, see pad0 flag)
    {
        public byte op;
        public byte pad0;                    // kV2ExternalFixedFlag* bits
        public ushort size4;
        public ushort count;
        public ushort wireBytesPerField;
        public ulong fieldHandler;
        public ulong groupHandler;           // 0 = per-entry fallback
        public ulong readGroupHandler;       // 0 = group not readable (template is write-only)
        public ulong userData;
    }

    // After the count x 8B write entries, a flagged group carries the READ
    // side's tables: count x UnityObjectReadEntry-layout records
    // (8 + sizeof(IntPtr)) + count x (field, fieldParent) pointer pairs
    // (2 x sizeof(void*)). Both are pointer-width so v1's walker and the C#
    // handler agree on 32-bit as well. Writers skip them.
    private const byte kV2ExternalFixedFlagHasReadTable = 1;
    // Trailing per-entry region of count x uint32 FUID segment indexes
    // (SerializeReference groups): the missing-type property-path registration
    // consumes them, on reads and on the gather pass. Writers skip the region;
    // the executor still hands the pointer to the group handler.
    private const byte kV2ExternalFixedFlagHasSegments = 4;

    // Layout-compatible with v1's UnityObjectWriteEntry so group handlers can
    // hand the entry table to the existing batched native codecs unchanged.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2ExternalFixedEntry     // 8 bytes
    {
        public uint fieldOffset;
        public uint destOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdExternalDynamic     // 80 bytes; the read* tail is READ-side payload, writes ignore it
    {
        public byte op;
        public byte pad0;
        public ushort size4;
        public uint fieldOffset;
        public ulong handler;
        public ulong userData;               // SimpleNativeType/NativeValueStruct: native WRITE dispatcher fnPtr; FixedBuffer: element count
        public ulong userData2;              // SimpleNativeType: wrapper m_Ptr post-header offset; FixedBuffer: element size
        public ulong readHandler;            // 0 = field not readable (template is write-only)
        public ulong readUserData;           // SimpleNativeType/NativeValueStruct: native READ dispatcher fnPtr
        public ulong readUserData2;          // SimpleNativeType: wrapper RuntimeTypeHandle
        public ulong readUserData3;          // SimpleNativeType: wrapper parameterless-ctor fnptr
        public ulong readUserData4;
        public ushort ensureSum;             // doc §12: window guarantee after the native crossing
        public ushort pad;
        public uint pad2;                    // explicit tail padding (8-byte struct alignment)          // GUIStyle: managed post-dispatch hook
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdExternalArray       // 96 bytes; the read* tail is READ-side payload, writes ignore it
    {
        public byte op;
        public byte kind;                    // V2CollectionKind low nibble
        public ushort size4;
        public uint fieldOffset;
        public uint elementWireBytes;
        public uint elementStride;
        public ulong fieldHandler;
        public ulong arrayHandler;           // 0 = per-element fallback
        public ulong userData;
        public ulong readArrayHandler;       // 0 = collection not readable (template is write-only)
        public ulong elementTypeHandle;      // RuntimeTypeHandle for Array.CreateInstance
        public ulong readKlass;              // element class (resolve + fake-null); 0 when unused
        public ulong readField;              // collection field backend ptr (fake-null); 0 when unused
        public ulong readFieldParent;        // collection field's declaring class; 0 when unused
        public ulong readUserData;           // read-side crossing payload (SR array fixup helper); 0 when unused
        public uint segment;                 // FUID segment for per-element missing-type registration; kV2NoPathSegment when absent
        public ushort ensureSum;             // doc §12: window guarantee after the collection
        public ushort pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdEnterElementSource  // 64 bytes; mirror of Commands.h
    {
        public byte op;
        public byte pad0;                    // kV2Pad0FlagPushFuidFrame
        public ushort size4;
        public uint fieldOffset;
        public uint elementStride;           // staged element managed stride
        public uint bodyBytes;               // body region incl. the trailing exec exit
        public uint segmentIndex;            // FUID path segment; kV2NoPathSegment when FUID is absent
        public ushort bodyEnsureSum;         // doc §12: body-entry guarantee
        public ushort exitEnsureSum;         // doc §12: empty-skip guarantee
        public ulong elementTypeHandle;      // READ: staging Array.CreateInstance
        public ulong sourceHandler;          // write: (object, ref V2FuidBuilder, ctx*, ulong) -> Array
        public ulong sinkHandler;            // read: (ref byte, Array, ref V2FuidBuilder, ctx*, ulong); 0 = template is write-only
        public ulong sourceUserData;         // opaque per-instantiation payload
        public ulong sinkUserData;           // opaque per-instantiation payload
    }

    // ISerializationCallbackReceiver bracket, direction-tagged through flags.
    // Write fires OnBeforeSerialize before the receiver's data, and a null
    // class slot fires nothing (v1's bracket contract). Read fires
    // OnAfterDeserialize after the data, always inline (registry-first reads
    // patch references before the body). Each executor skips the other
    // direction's brackets.
    [StructLayout(LayoutKind.Sequential)]
    internal struct V2CmdInvokeCallback      // 16 bytes
    {
        public byte op;
        public byte flavor;                  // flavor low nibble (0 = class, 1 = struct) | kV2Pad0FlagCallbackRead
        public ushort size4;
        public uint fieldOffset;             // receiver slot on the current frame (0 = element/self)
        public ulong methodFnPtr;            // struct flavor: delegate*<ref byte, void> (OnBeforeSerialize
                                             // on write brackets, OnAfterDeserialize on read brackets);
                                             // 0 for the class flavor, which cast-dispatches the interface
    }

    private const byte kV2CallbackFlavorStruct = 1;

    // One frame per open object: Offset is 0 for class frames and the element
    // cursor for collection frames (doc §6.2). EndOffset is never read on
    // class frames (stale values are harmless; FUID index recovery uses
    // publish-stamped frame depths, so nothing scans frames for open loops).
    //
    // On a collection frame the cursor pair IS the loop state, shared by the
    // enter, the tests and the latches: the enter sets Offset to the data
    // start and EndOffset past the last element; a passing test rebases
    // baseAddr to Offset without advancing it, so a body runs on the element
    // the cursor points at; a latch advances by its stride first and then
    // re-tests, popping on OrExit. Consecutive loops compose on the cursor,
    // the element loop starting on whatever the chunk loop left.
    //
    // A collection with nothing to iterate (null, or count 0) pushes no
    // frame at all: the executors keep the cursor pair in registers, the
    // enter marks the empty case by zeroing the cached EndOffset — no real
    // frame can hold 0, since EndOffset is at least ArrayDataOffset — every
    // test then fails against it, and RepeatOrExit restores the cached pair
    // from the frame beneath instead of popping. Instance is therefore never
    // null on a live frame.

    private struct V2ObjectFrame
    {
        public object Instance;
        public nint Offset;
        public nint EndOffset;
    }
}
