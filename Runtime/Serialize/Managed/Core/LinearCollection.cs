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

    private static unsafe void ConsumeLinearCollectionManagedReferenceArray(
        NativeBufferContext* ctx, int count, ref BufferDataStager bufferDataStager)
    {
        Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), count);

        if (count == 0)
        {
            // Still cross: the icall marks the registry active, as the native command's Activate<>() does.
            WriteManagedReferencesToBuffer(ctx->transferState, IntPtr.Zero, 0);
            return;
        }

        const int kRefIdSize = 8;

        int left = count;
        while (left > 0)
        {
            if (bufferDataStager.StagingRoom < kRefIdSize)
                bufferDataStager.FlushStaged(kManagedBlockMaxPayloadSize);

            int batch = bufferDataStager.StagingRoom / kRefIdSize;
            if (batch > left)
                batch = left;

            WriteManagedReferencesToBuffer(ctx->transferState, (IntPtr)bufferDataStager.StagingPtr, batch);

            bufferDataStager.Stage(batch * kRefIdSize);
            left -= batch;
        }
    }

    // UUM-143556 marker the read path stamps on a type-mismatched fake-null reference. This is the
    // managed write path's own source of truth (the serialization backend is moving to managed; the
    // native path — and its own kTypeMismatchReferenceError in TransferPPtrToMonoObject.cpp — is legacy
    // that will go away). While both exist they must stay byte-identical: the read path stamps with the
    // native copy and the drop below compares against this one, so the round-trip drop test fails if
    // they ever diverge.
    private const string kTypeMismatchReferenceError =
        "The serialized reference's type does not match the field's type; it was removed when building the player (UUM-143556).";



    // Cache elementType -> List<elementType> so the expensive MakeGenericType runs once
    // per element type, not on every null-List allocation during read. Keyed/valued by
    // Type, which can pin a user-script ALC after reload; clear it there. Players never
    // code-reload, and the native-test-resources stub doesn't reference Unity.Scripting.
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Type>
        s_ListTypeCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, Type>();

    private static Type GetCachedListType(Type elementType) =>
        s_ListTypeCache.GetOrAdd(elementType, t => typeof(List<>).MakeGenericType(t));

    // Allocate or reuse same-length backing; returns Array + reinterpreted byte[] for pinning.
    private static unsafe Array AllocateOrReuseArrayBacking(
        ref byte baseAddr, byte kind, uint fieldOffset, Type elementType, int count, out byte[] dataAsBytes)
    {
        Array arr;
        if (kind == LinearCollectionKind.Array)
        {
            Array existingArr = Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
            arr = (existingArr != null && existingArr.Length == count)
                ? existingArr
                : Array.CreateInstance(elementType, count);
        }
        else
        {
            ListLayout existingList = Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
            arr = (existingList != null
                && existingList._size == count
                && existingList._items != null
                && existingList._items.Length >= count)
                ? Unsafe.As<byte[], Array>(ref existingList._items)
                : Array.CreateInstance(elementType, count);
        }
        dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);
        return arr;
    }

    private static unsafe void AssignArrayBacking(
        ref byte baseAddr, byte kind, uint fieldOffset, Array arr, byte[] dataAsBytes, int count, Type elementType)
    {
        if (kind == LinearCollectionKind.Array)
        {
            Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = arr;
        }
        else
        {
            // Refill in place, allocating only when the field is null: a read must not
            // replace the List instance the field holds.
            ref byte fieldSlot = ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset);
            ListLayout layout = Unsafe.As<byte, ListLayout>(ref fieldSlot);
            if (layout == null)
            {
                layout = Unsafe.As<ListLayout>(
                    RuntimeHelpers.GetUninitializedObject(GetCachedListType(elementType)));
                Unsafe.As<byte, ListLayout>(ref fieldSlot) = layout;
            }
            layout._items = dataAsBytes;
            layout._size  = count;
        }
    }

}
