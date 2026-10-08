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
    // EntityId to serialize for a UnityObject ref, with the UUM-143556 game-release drop. Resolving the
    // id in managed keeps the object off the native path. The drop reproduces the read path's
    // type-mismatch discriminator in managed: a fake-null wrapper stamped with the marker string. The
    // wrapper's EntityId cannot tell it apart (it carries the id of a live object of another type), only
    // the string can; bound refs (the common case) have a null string and fail the compare at once.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ResolveUnityObjectEntityIdForWrite(object slot, int flags)
    {
        if (slot == null)
            return 0UL;
        var o = Unsafe.As<object, UnityEngine.Object>(ref slot);
        if ((flags & UnityObjectTransferFlags.SerializeForGameRelease) != 0
            && o.GetUnityRuntimeErrorString() == kTypeMismatchReferenceError)
            return 0UL; // UUM-143556: never ship a type-mismatched reference to the player.
        return EntityId.ToULong(o.GetEntityIdForSerializationUnchecked());
    }

    // No per-element interpreter frame; bytes match the generic path.
    private static unsafe void ConsumeLinearCollectionUnityObjectArray(
        NativeBufferContext* ctx, byte[] dataAsBytes, int count, long stride)
    {
        // The enter wrote the count and skips this body at count 0, so count is positive here.

        const int wire = 12;

        fixed (byte* dataPtr = dataAsBytes)
        {
            byte* srcCur = dataPtr;
            int   left   = count;
            while (left > 0)
            {
                // The staged count (or a prior batch's tail) can leave < one record of room;
                // re-arm for at least one record.
                if (ctx->writerEnd - ctx->writerPtr < wire)
                    ctx->ensureWritable(ctx, wire);

                int batch = (int)(ctx->writerEnd - ctx->writerPtr) / wire;
                if (batch > left)
                    batch = left;

                byte* dst = ctx->writerPtr;
                // Resolve each element's EntityId (with the UUM-143556 game-release drop) in managed;
                // the movable element references are never handed to native. See the scalar case.
                bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
                if (!packInLSOI)
                {
                    // Remap batch: pre-write each id into its output slot, resolve in place in one
                    // crossing (src == output, stride == wire) — object-free batching (2ca2f5c).
                    for (int i = 0; i < batch; ++i)
                    {
                        object slot = Unsafe.As<byte, object>(ref Unsafe.AsRef<byte>(srcCur + (long)i * stride));
                        Unsafe.WriteUnaligned<ulong>(dst + i * wire, ResolveUnityObjectEntityIdForWrite(slot, ctx->flags));
                    }
                    s_writeEntityIdsArrayToBuffer(ctx->resolverHandle, ctx->flags, (IntPtr)dst, batch, (long)wire, (IntPtr)dst);
                }
                else
                {
                    for (int i = 0; i < batch; ++i)
                    {
                        object slot = Unsafe.As<byte, object>(ref Unsafe.AsRef<byte>(srcCur + (long)i * stride));
                        byte* d = dst + i * wire;
                        ulong entityId = ResolveUnityObjectEntityIdForWrite(slot, ctx->flags);
                        if (packInLSOI || entityId == 0UL)
                            PackEntityIdIntoLsoi(d, entityId);
                        else
                            s_writeEntityIdToBuffer(entityId, ctx->resolverHandle, (IntPtr)d, ctx->flags);
                    }
                }

                // Stage the batch; it commits with the next re-arm.
                ctx->writerPtr += batch * wire;
                srcCur += (long)batch * stride;
                left   -= batch;
            }
        }
    }

    // Dropped on the reload that clears its V2DeclaredTypeTable indexes. Replaced wholesale, so it
    // is never observed half-cleared.
    [NoAutoStaticsCleanup]
    private static IntPtr[] s_DeclaredTypeKlassCache;

    [OnCodeUnloading]
    private static void ClearDeclaredTypeKlassCache()
    {
        s_DeclaredTypeKlassCache = null;
        s_FakeNullTypes = null;
        s_NoIdFakeNullMessages = null;
        s_UnresolvedFakeNullMessages = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IntPtr DeclaredTypeKlass(uint declaredTypeIndex)
    {
        IntPtr[] cache = s_DeclaredTypeKlassCache;
        if (cache != null && declaredTypeIndex < (uint)cache.Length)
        {
            IntPtr cached = cache[declaredTypeIndex];
            if (cached != IntPtr.Zero)
                return cached;
        }
        return DeclaredTypeKlassSlow(declaredTypeIndex);
    }

    // Out of line to keep the inlined lookup small.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr DeclaredTypeKlassSlow(uint declaredTypeIndex)
    {
        IntPtr klass = GetDeclaredTypeKlass(declaredTypeIndex);
        if (klass == IntPtr.Zero)
            return IntPtr.Zero;

        IntPtr[] cache = s_DeclaredTypeKlassCache;
        if (cache == null || declaredTypeIndex >= (uint)cache.Length)
        {
            int grown = 64;
            while (grown <= (int)declaredTypeIndex)
                grown *= 2;
            var next = new IntPtr[grown];
            if (cache != null)
                System.Array.Copy(cache, next, cache.Length);
            // Published whole: a reader sees the old array, valid for the indexes it covers, or the new one.
            System.Threading.Volatile.Write(ref s_DeclaredTypeKlassCache, next);
            cache = next;
        }
        cache[declaredTypeIndex] = klass;
        return klass;
    }

    // A null class is a real answer, so Fetched distinguishes it from a slot not yet looked up.
    // Scripted is applied per read: the gate it feeds changes between transfers.
    private struct FakeNullTypes
    {
        public Type NoId;
        public Type Unresolved;
        public bool Scripted;
        public bool Fetched;
    }

    [NoAutoStaticsCleanup]
    private static FakeNullTypes[] s_FakeNullTypes;

    // Keyed by field, since the message names it. Separate dictionaries: the wording differs.
    // Unlocked: main thread only, as threaded reads never resolve in managed.
    [NoAutoStaticsCleanup]
    private static Dictionary<IntPtr, string> s_NoIdFakeNullMessages;
    [NoAutoStaticsCleanup]
    private static Dictionary<IntPtr, string> s_UnresolvedFakeNullMessages;

    // Null when the field gets no wrapper.
    private static object BuildNoIdFakeNull(uint declaredTypeIndex, IntPtr field, IntPtr fieldParent)
    {
        if (field == IntPtr.Zero || fieldParent == IntPtr.Zero)
            return null;

        Type wrapperType = FakeNullTypesFor(declaredTypeIndex).NoId;
        if (wrapperType == null)
            return null;

        return UnityEngine.Object.CreateFakeNullWrapper(
            wrapperType, EntityId.None, FakeNullMessageFor(field, fieldParent, false));
    }

    // Stamped with the id so the reference survives a re-save. Null when the field gets no wrapper.
    private static object BuildUnresolvedFakeNull(
        uint declaredTypeIndex, EntityId id, IntPtr field, IntPtr fieldParent, NativeReadBufferContext* ctx)
    {
        if (field == IntPtr.Zero || fieldParent == IntPtr.Zero)
            return null;

        FakeNullTypes types = FakeNullTypesFor(declaredTypeIndex);
        if (types.Unresolved == null)
            return null;

        // Play Mode and building suppress the wrapper for a scripted field type only.
        if (ctx->suppressScriptedFakeNull != 0 && types.Scripted)
            return null;

        return UnityEngine.Object.CreateFakeNullWrapper(
            types.Unresolved, id, FakeNullMessageFor(field, fieldParent, true));
    }

    private static FakeNullTypes FakeNullTypesFor(uint declaredTypeIndex)
    {
        FakeNullTypes[] cache = s_FakeNullTypes;
        if (cache != null && declaredTypeIndex < (uint)cache.Length && cache[declaredTypeIndex].Fetched)
            return cache[declaredTypeIndex];
        return FakeNullTypesForSlow(declaredTypeIndex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FakeNullTypes FakeNullTypesForSlow(uint declaredTypeIndex)
    {
        IntPtr noIdKlass = GetNoIdFakeNullClass(declaredTypeIndex);
        IntPtr unresolvedKlass = GetUnresolvedFakeNullClass(declaredTypeIndex);

        FakeNullTypes resolved;
        resolved.NoId = noIdKlass == IntPtr.Zero ? null : UnmarshalSystemType(noIdKlass);
        resolved.Unresolved = unresolvedKlass == IntPtr.Zero ? null : UnmarshalSystemType(unresolvedKlass);
        resolved.Scripted = GetFakeNullClassIsScripted(declaredTypeIndex) != 0;
        resolved.Fetched = true;

        FakeNullTypes[] cache = s_FakeNullTypes;
        if (cache == null || declaredTypeIndex >= (uint)cache.Length)
        {
            int grown = 64;
            while (grown <= (int)declaredTypeIndex)
                grown *= 2;
            var next = new FakeNullTypes[grown];
            if (cache != null)
                System.Array.Copy(cache, next, cache.Length);
            System.Threading.Volatile.Write(ref s_FakeNullTypes, next);
            cache = next;
        }
        cache[declaredTypeIndex] = resolved;
        return resolved;
    }

    private static string FakeNullMessageFor(IntPtr field, IntPtr fieldParent, bool referenceHadId)
    {
        Dictionary<IntPtr, string> messages =
            referenceHadId ? s_UnresolvedFakeNullMessages : s_NoIdFakeNullMessages;
        if (messages != null && messages.TryGetValue(field, out string cached))
            return cached;

        string message = GetFakeNullMessage(field, fieldParent, referenceHadId ? 1 : 0);
        if (messages == null)
        {
            messages = new Dictionary<IntPtr, string>();
            if (referenceHadId)
                System.Threading.Volatile.Write(ref s_UnresolvedFakeNullMessages, messages);
            else
                System.Threading.Volatile.Write(ref s_NoIdFakeNullMessages, messages);
        }
        messages[field] = message;
        return message;
    }

    // Mirrors kUnityObjectNoIdSentinel in ReadUnityObjectFromBuffer.h. Tested before the store
    // lookup, which would otherwise read it as a dangling reference.
    private const ulong k_NoIdSentinel = 0xFFFFFFUL << 40;
    // Mirrors kUnityObjectDeferSentinel.
    private const ulong k_DeferSentinel = 0xFFFFFEUL << 40;

    // A packed record decodes here; anything else crosses.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPackedUnityObjectReference(int flags) =>
        (flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;

    // Threaded reads go to the native reader, the only one that holds the object read lock.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CanResolveUnityObjectsInManaged(int flags) =>
        (flags & UnityObjectTransferFlags.IsThreadedSerialization) == 0;

    // A real LocalSerializedObjectIdentifier is null when localIdentifierInFile == 0 whatever the
    // file index (PersistentManager::LocalSerializedObjectIdentifierToEntityId).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNullUnityObjectReference(byte* src, bool packed) =>
        packed
            ? UnpackEntityIdFromLsoi(src) == 0
            : Unsafe.ReadUnaligned<long>(src + 4) == 0;

    // A drop reads as a plain null; no-id and unresolved each get their own wrapper.
    private enum UnityObjectResolveOutcome : byte
    {
        Resolved,    // wrapper produced
        Dropped,     // the resolver dropped the reference (Editor only)
        NoId,        // the record decoded to no id at all
        Unresolved,  // an id decoded, but its object is not there
        Defer,       // the native path owns this one
    }

    // Callers must reject a null record first: repeating the test here cost ~4 ns per reference.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe UnityObjectResolveOutcome ResolveNonNullUnityObject(
        byte* src, NativeReadBufferContext* ctx, bool packed, out object wrapper, out EntityId id)
    {
        wrapper = null;

        if (packed)
        {
            return ResolvePackedUnityObject(UnpackEntityIdFromLsoi(src), out wrapper, out id);
        }
        else
        {
            // One crossing decodes and resolves: resolver arm, drop and autoload all happen there.
            ulong resolved = ResolveUnityObjectEntityIdForManaged(ctx->resolverHandle, (IntPtr)src, ctx->flags);

            // Zero is a resolver drop: a plain null in the Editor, as native reads it; a player defers, as its
            // native read ignores drops.
            if (resolved == 0)
            {
                id = EntityId.None;
                return UnityObjectResolveOutcome.Dropped;
            }
            if (resolved == k_NoIdSentinel)
            {
                id = EntityId.None;
                return UnityObjectResolveOutcome.NoId;
            }
            if (resolved == k_DeferSentinel)
            {
                id = EntityId.None;
                return UnityObjectResolveOutcome.Defer;
            }

            id = EntityId.FromULong(resolved);
        }

        return ResolveResidentUnityObject(id, out wrapper);
    }

    // A packed id skipped the crossing that loads, so an unloaded asset still has to reach it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static UnityObjectResolveOutcome ResolvePackedUnityObject(ulong packedId, out object wrapper, out EntityId id)
    {
        id = EntityId.FromULong(packedId);
        UnityObjectResolveOutcome outcome = ResolveResidentUnityObject(id, out wrapper);
        return outcome == UnityObjectResolveOutcome.Unresolved ? UnityObjectResolveOutcome.Defer : outcome;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe UnityObjectResolveOutcome ResolveResidentUnityObject(EntityId id, out object wrapper)
    {
        wrapper = null;

        // An object still loading resolves here too, but one being destroyed must not be bound. Null means nothing
        // resident has this id; the callers decide whether that defers to the native reader or reads as missing.
        void* nativeObj = EntityIdStore.GetLoadingOrPublishedNativeObject(id);
        if (nativeObj == null)
            return UnityObjectResolveOutcome.Unresolved;

        wrapper = UnityEngine.Object.MarshalledUnityObject.EnsureScriptingWrapperFor((IntPtr)nativeObj, id);

        // Null here is a scripted object, whose wrapper the MonoScript machinery creates.
        return wrapper != null
            ? UnityObjectResolveOutcome.Resolved
            : UnityObjectResolveOutcome.Defer;
    }

    // Editor-only: a player binds the wrapper whatever its type, deferring the same outcome.
    // Frame-local, not static: MethodTable addresses can be recycled across a domain reload.
    private struct LastAssignableKlass
    {
        public IntPtr Klass;
        public IntPtr MethodTable;
    }

    // The common case, inlined into the read loops: a packed id whose object is resident and whose
    // type the field accepts. Anything else returns false and takes the full read below.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadResidentPackedUnityObject(
        ulong packedId, IntPtr klass, ref LastAssignableKlass lastAssignable, out object value)
    {
        value = null;
        if (packedId == 0)
            return false;

        if (ResolveResidentUnityObject(EntityId.FromULong(packedId), out object resolved) != UnityObjectResolveOutcome.Resolved)
            return false;

        IntPtr methodTable = GetMethodTable(resolved);
        if (klass != lastAssignable.Klass || methodTable != lastAssignable.MethodTable)
        {
            if (!IsAssignableToField(resolved, methodTable, klass))
                return false;
            lastAssignable.Klass = klass;
            lastAssignable.MethodTable = methodTable;
        }

        value = resolved;
        return true;
    }

    // Returns false only where the native path owns the result: a scripted object, a non-resident
    // packed id, or a reference the declared field type rejects. A fake-null case settles only when
    // a wrapper was built -- which fields get none is native's decision, and a plain null would drop
    // both the inspector diagnostic and the id.
    // packedId: the caller's decode when packed.
    private static bool TryReadUnityObjectReferenceForEditor(
        byte* src, ulong packedId, uint declaredTypeIndex, NativeReadBufferContext* ctx, bool packed,
        IntPtr klass, IntPtr field, IntPtr fieldParent, ref LastAssignableKlass lastAssignable, out object value)
    {
        if (packed ? packedId == 0 : Unsafe.ReadUnaligned<long>(src + 4) == 0)
        {
            value = BuildNoIdFakeNull(declaredTypeIndex, field, fieldParent);
            return value != null;
        }

        UnityObjectResolveOutcome outcome = packed
            ? ResolvePackedUnityObject(packedId, out object resolved, out EntityId id)
            : ResolveNonNullUnityObject(src, ctx, false, out resolved, out id);
        switch (outcome)
        {
            case UnityObjectResolveOutcome.Dropped:
                value = null;
                return true;

            case UnityObjectResolveOutcome.NoId:
                value = BuildNoIdFakeNull(declaredTypeIndex, field, fieldParent);
                return value != null;

            case UnityObjectResolveOutcome.Unresolved:
                value = BuildUnresolvedFakeNull(declaredTypeIndex, id, field, fieldParent, ctx);
                return value != null;

            case UnityObjectResolveOutcome.Resolved:
                break;

            default:
                value = null;
                return false;
        }

        IntPtr methodTable = GetMethodTable(resolved);
        if ((klass != lastAssignable.Klass || methodTable != lastAssignable.MethodTable)
            && !IsAssignableToField(resolved, methodTable, klass))
        {
            // Native stamps the marker the game-release writer reads back, and logs the warning.
            value = null;
            return false;
        }

        lastAssignable.Klass = klass;
        lastAssignable.MethodTable = methodTable;
        value = resolved;
        return true;
    }

    // Cacheable: CoreModule never unloads.
    private static readonly IntPtr s_UnityObjectKlass = typeof(UnityEngine.Object).TypeHandle.Value;

    // The MethodTable sits one pointer before the first field (kObjectHeader — see
    // ScriptingUtility.GetValueAtOffsetObjectInstanceID). Interior ref survives a moving GC.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IntPtr GetMethodTable(object candidate) =>
        Unsafe.Add(ref Unsafe.As<byte, IntPtr>(ref Unsafe.As<ObjectWrapper>(candidate).Data), -1);

    // klass == s_UnityObjectKlass / methodTable == klass are CoreCLR-only shortcuts: klass is a
    // MethodTable pointer on this backend, so both compare directly.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAssignableToField(object candidate, IntPtr methodTable, IntPtr klass)
    {
        if (klass == s_UnityObjectKlass)
            return true;
        if (methodTable == klass)
            return true;
        return IsAssignableToFieldReflective(candidate, klass);
    }

    // Mirrors TransferPPtrToMonoObject's scripting_class_has_parent gate. Must never accept more
    // than the native system does: a false positive binds a reference the field rejects and loses
    // the warning. klass is a ScriptingClassPtr off the wire, which on CoreCLR is the MethodTable.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAssignableToFieldReflective(object candidate, IntPtr klass)
    {
        Type fieldType = UnmarshalSystemType(klass);
        return fieldType != null && fieldType.IsInstanceOfType(candidate);
    }

}
