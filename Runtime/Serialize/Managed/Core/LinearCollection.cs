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
        NativeBufferContext* ctx, int count)
    {
        // The enter wrote the count and skips this body at count 0, so count is positive here.

        const int kRefIdSize = 8;

        int left = count;
        while (left > 0)
        {
            if (ctx->writerEnd - ctx->writerPtr < kRefIdSize)
                ctx->ensureWritable(ctx, kRefIdSize);

            int batch = (int)(ctx->writerEnd - ctx->writerPtr) / kRefIdSize;
            if (batch > left)
                batch = left;

            WriteManagedReferencesToBuffer(ctx->transferState, (IntPtr)ctx->writerPtr, batch);

            ctx->writerPtr += batch * kRefIdSize;
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
}
