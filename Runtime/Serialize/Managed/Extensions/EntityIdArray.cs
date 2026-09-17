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
    // EntityId counterpart; bytes match the per-element path. id==0 → zeroed LSOI (no resolver call).
    private static unsafe void ConsumeLinearCollectionEntityIdArray(
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
                // Pack arm: encode each id inline (no icall). The branch is per batch, not per element.
                if ((ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0)
                {
                    for (int i = 0; i < batch; ++i)
                    {
                        ulong id = Unsafe.ReadUnaligned<ulong>(srcCur + (long)i * stride);
                        PackEntityIdIntoLsoi(dst + i * wire, id);
                    }
                }
                else
                {
                    // Remap arm: whole batch in one crossing. The leaf zeroes id==0 (no resolver).
                    s_writeEntityIdsArrayToBuffer(
                        ctx->resolverHandle, ctx->flags, (IntPtr)srcCur, batch, stride, (IntPtr)dst);
                }

                // Stage the batch; it flushes with the next window-full batch or, for the
                // last batch, with the surrounding flow.
                bufferDataStager.Stage(batch * wire);
                srcCur += (long)batch * stride;
                left   -= batch;
            }
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


    // EntityId stores values — safe on all runtimes, not gated on ENABLE_CORECLR.
    private static unsafe void ConsumeLinearCollectionEntityIdArrayRead(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        ref byte* pos)
    {
        var header = (EntityIdArrayReadHeader*)pos;
        pos += sizeof(EntityIdArrayReadHeader);

        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        Type elementType = UnmarshalSystemType(header->elementTypeHandle);
        Array arr = AllocateOrReuseArrayBacking(
            ref baseAddr, header->kind, header->fieldOffset, elementType, count, out byte[] dataAsBytes);

        if (count > 0)
        {
            const int wire = 12;
            bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
            fixed (byte* dataPtr = dataAsBytes)
            {
                long stride    = (long)header->elementStride;
                int  processed = 0;
                while (processed < count)
                {
                    if (ctx->readerEnd - ctx->readerPtr < wire)
                        InvokeEnsureReadable(ctx, wire);
                    int batch = (int)(ctx->readerEnd - ctx->readerPtr) / wire;
                    int remaining = count - processed;
                    if (batch > remaining)
                        batch = remaining;

                    if (packInLSOI)
                    {
                        // Clone arm: unpack each id inline (no crossing).
                        for (int i = 0; i < batch; ++i)
                        {
                            ulong id = UnpackEntityIdFromLsoi(ctx->readerPtr + i * wire);
                            Unsafe.WriteUnaligned<ulong>(dataPtr + (long)(processed + i) * stride, id);
                        }
                    }
                    else
                    {
                        // Remap arm: whole batch in one crossing, storing values into the pinned backing.
                        s_readEntityIdsArrayIntoElements(
                            ctx->resolverHandle, ctx->flags,
                            (IntPtr)(dataPtr + (long)processed * stride), batch, stride,
                            (IntPtr)ctx->readerPtr);
                    }

                    ctx->readerPtr += batch * wire;
                    processed += batch;
                }
            }
        }

        AssignArrayBacking(ref baseAddr, header->kind, header->fieldOffset, arr, dataAsBytes, count, elementType);
    }

}
