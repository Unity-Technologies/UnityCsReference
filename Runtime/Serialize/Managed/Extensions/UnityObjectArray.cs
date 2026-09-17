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
    // type-mismatch discriminator in managed — a fake-null wrapper (m_CachedPtr == 0) stamped with the
    // marker — reading cached-ptr first so bound refs (the common case) short-circuit before the string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ResolveUnityObjectEntityIdForWrite(object slot, int flags)
    {
        if (slot == null)
            return 0UL;
        var o = Unsafe.As<object, UnityEngine.Object>(ref slot);
        if ((flags & UnityObjectTransferFlags.SerializeForGameRelease) != 0
            && o.GetCachedPtr() == IntPtr.Zero
            && o.GetUnityRuntimeErrorString() == kTypeMismatchReferenceError)
            return 0UL; // UUM-143556: never ship a type-mismatched reference to the player.
        return EntityId.ToULong(o.GetEntityIdForSerializationUnchecked());
    }

    // No per-element interpreter frame; bytes match the generic path.
    private static unsafe void ConsumeLinearCollectionUnityObjectArray(
        NativeBufferContext* ctx, byte[] dataAsBytes, int count, long stride, ref BufferDataStager bufferDataStager)
    {
        // Stage the count; it rides the first batch's flush. An empty array leaves it for the
        // surrounding flow's trailing commit.
        Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), count);
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
                // flush to open a fresh window so at least one record fits below.
                if (bufferDataStager.StagingRoom < wire)
                    bufferDataStager.FlushStaged(kManagedBlockMaxPayloadSize);

                int batch = bufferDataStager.StagingRoom / wire;
                if (batch > left)
                    batch = left;

                byte* dst = bufferDataStager.StagingPtr;
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

                // Stage the batch; it flushes with the next window-full batch or, for the
                // last batch, with the surrounding flow.
                bufferDataStager.Stage(batch * wire);
                srcCur += (long)batch * stride;
                left   -= batch;
            }
        }
    }


    // Gated on ENABLE_CORECLR (matching the emitter) so the native-test image still receives the opcode and uses the per-element fallback.
    private static unsafe void ConsumeLinearCollectionUnityObjectArrayRead(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        ref byte* pos)
    {
        var header = (UnityObjectArrayReadHeader*)pos;
        pos += sizeof(UnityObjectArrayReadHeader);

        // Count prefix (4 bytes, always present even for a null/empty source — see ConsumeLinearCollectionRead).
        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        // Allocate/assign even when count == 0 (the null/empty contract).
        Type elementType = UnmarshalSystemType(header->elementTypeHandle);
        Array arr = AllocateOrReuseArrayBacking(
            ref baseAddr, header->kind, header->fieldOffset, elementType, count, out byte[] dataAsBytes);

        if (count > 0)
        {
            const int wire = 12;
            fixed (byte* dataPtr = dataAsBytes)
            {
                long stride    = (long)header->elementStride;
                int  processed = 0;
                while (processed < count)
                {
                    // Resolve as many contiguous LSOIs as the reader holds per pass, then refill;
                    // EnsureReadable guarantees >=1 record so the loop advances.
                    if (ctx->readerEnd - ctx->readerPtr < wire)
                        InvokeEnsureReadable(ctx, wire);
                    int batch = (int)(ctx->readerEnd - ctx->readerPtr) / wire;
                    int remaining = count - processed;
                    if (batch > remaining)
                        batch = remaining;

                    s_readUnityObjectsArrayIntoElements(
                        ctx->resolverHandle, ctx->flags,
                        (IntPtr)(dataPtr + (long)processed * stride), batch, stride,
                        header->klass, header->field, header->fieldParent,
                        (IntPtr)ctx->readerPtr);

                    ctx->readerPtr += batch * wire;
                    processed += batch;
                }
            }
        }

        AssignArrayBacking(ref baseAddr, header->kind, header->fieldOffset, arr, dataAsBytes, count, elementType);
    }

}
