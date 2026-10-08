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

                // Stage the batch; it commits with the next re-arm.
                ctx->writerPtr += batch * wire;
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

}
