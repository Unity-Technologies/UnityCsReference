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

        // Null returns for a missing or abstract type; CreateVrtInstance reports the latter.
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
        IntPtr rth = entry->runtimeTypeHandle;
        IntPtr cfp = entry->ctorFunctionPtr;
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

        // The write side instantiates null elements too, and each consumes an
        // inline-RefId slot per [SerializeReference] site (UUM-150957).
        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        for (int e = 0; e < arr.Length; e++)
        {
            object elem = GetOrCreateVrtElement(arr, e, rth, cfp);
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

    private static unsafe void ProcessGatherRecurseClassList(
        ref byte baseAddr, ref byte* pos, IntPtr transferState, IntPtr fnPtr,
        IntPtr resolveMissingTypeFnPtr,
        bool emitCallbacks, bool collectMissingTypes,
        int* indexStack, int indexDepth)
    {
        var entry = (GatherRecurseClassListEntry*)pos;
        uint nestedBytes = entry->nestedByteCount;
        uint fieldOffset = entry->fieldOffset;
        IntPtr rth = entry->runtimeTypeHandle;
        IntPtr cfp = entry->ctorFunctionPtr;
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

        // Null elements materialized — see RecurseClassArray.
        int nestedDepth = indexDepth < kMaxGatherIndexDepth ? indexDepth + 1 : indexDepth;
        for (int e = 0; e < size; e++)
        {
            object elem = GetOrCreateVrtElement(items, e, rth, cfp);
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

        // Instance methods on value types take their `this` as a managed byref to the
        // struct data (NOT as a boxed MonoObject*). On Mono/IL2CPP the build side resolves
        // the function pointer via GetMethodFunctionPointer -> MethodInfo.MethodHandle.
        // GetFunctionPointer, which returns the underlying value-type instance entry. On
        // CoreCLR that instance entry is not directly callable, so GetInterfaceMethodFunctionPointer
        // returns the entry of a static StructCallbackInvokerHelper<T> shim instead. Either way
        // the stored pointer is a `delegate*<ref byte, void>`, so passing `ref baseAddr` to the
        // struct's inline data matches the ABI the calli expects.
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

}
