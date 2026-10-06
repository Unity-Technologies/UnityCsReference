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
        // Stage the count; it commits with the first batch's re-arm, or with the surrounding
        // flow's for an empty array.
        if (ctx->writerEnd - ctx->writerPtr < 4)
            ctx->ensureWritable(ctx, 4);
        Unsafe.WriteUnaligned(ctx->writerPtr, count);
        ctx->writerPtr += 4;
        if (count == 0)
            return;

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



    // Cacheable: CoreModule never unloads.
    private static readonly IntPtr s_UnityObjectKlass = typeof(UnityEngine.Object).TypeHandle.Value;

    // Last accepted (klass, MethodTable) pair. Frame-local, not static: MethodTable addresses
    // can be recycled across a domain reload.
    private struct UnityObjectKlassMemo
    {
        public IntPtr Klass;
        public IntPtr MethodTable;
    }

    // Resolves a packed LSOI to its bound wrapper; null sends the caller to the native route.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object TryResolvePackedUnityObjectHit(byte* src, IntPtr klass, ref UnityObjectKlassMemo memo)
    {
        ulong rawId = UnpackEntityIdFromLsoi(src);
        if (rawId == 0)
            return null;

        object hit = UnityEngine.Object.GetManagedObject<UnityEngine.Object>(EntityId.FromULong(rawId));
        if (hit == null)
            return null;

        IntPtr methodTable = GetMethodTable(hit);
        if ((klass != memo.Klass || methodTable != memo.MethodTable)
            && !IsAcceptableFieldKlass(hit, methodTable, klass))
            return null;

        memo.Klass = klass;
        memo.MethodTable = methodTable;
        return hit;
    }

    // The MethodTable sits one pointer before the first field (kObjectHeader — see
    // ScriptingUtility.GetValueAtOffsetObjectInstanceID). Interior ref survives a moving GC.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IntPtr GetMethodTable(object candidate) =>
        Unsafe.Add(ref Unsafe.As<byte, IntPtr>(ref Unsafe.As<ObjectWrapper>(candidate).Data), -1);

    // Mirrors native's scripting_class_has_parent gate (TransferPPtrToMonoObject). Must never accept
    // more than native does: a false negative only costs a crossing, a false positive binds a
    // reference the field rejects and loses the warning.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAcceptableFieldKlass(object candidate, IntPtr methodTable, IntPtr klass)
    {
        if (klass == s_UnityObjectKlass)
            return true;
        if (methodTable == klass)
            return true;
        return IsAssignableFieldKlass(candidate, klass);
    }

    // Out of line to keep the inlined fast path small.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAssignableFieldKlass(object candidate, IntPtr klass)
    {
        Type fieldType = UnmarshalSystemType(klass);
        return fieldType != null && fieldType.IsInstanceOfType(candidate);
    }
}
