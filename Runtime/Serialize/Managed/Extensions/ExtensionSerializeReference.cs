// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 [SerializeReference] inline-RefId extension
// handlers (doc §2.4, M4). The field is never read: the gather pass resolved
// every SR site's RefId in field order onto a per-host cursor on
// transferState, and these handlers pop it. The icalls also mark the
// references registry active, which is byte-significant: the blob header is
// emitted only when active.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Scalar field: 8-byte inline RefId popped off the shared cursor.
    private static unsafe void V2WriteSerializeReferenceField(ref byte field, byte* dest, NativeBufferContext* ctx, ulong userData)
    {
        WriteManagedReferenceToBuffer(ctx->transferState, (IntPtr)dest);
    }

    // Scalar group: consecutive SR slots in one fixed run are dest-contiguous
    // (each advances the run by 8), so the whole group is one batched crossing
    // popping count RefIds off the shared cursor.
    private static unsafe void V2WriteSerializeReferenceGroup(ref byte baseAddr, byte* entriesRaw, int count, byte* output, NativeBufferContext* ctx, ulong userData)
    {
        var entry = (V2ExternalFixedEntry*)entriesRaw;
        WriteManagedReferencesToBuffer(ctx->transferState, (IntPtr)(output + entry->destOffset), count);
    }

    // T[] / List<T>: count prefix plus count 8-byte RefIds in staging-window
    // batches. The backing array is not read; dataAsBytes may be null, and the
    // handler owns the null case so the count==0 crossing still marks the
    // registry active, matching v1's null-collection behavior.
    private static unsafe void V2WriteSerializeReferenceArray(byte[] dataAsBytes, int count, uint stride, NativeBufferContext* ctx, ref BufferDataStager stager, ulong userData)
    {
        ConsumeLinearCollectionManagedReferenceArray(ctx, count, ref stager);
    }

    // T[] / List<T> read: count prefix plus count 8-byte RefIds. The handler
    // owns the full framing: reuse-or-allocate the backing (v1's null/empty
    // behavior: allocate and assign even at count 0), assign it to the field
    // BEFORE registering fixups so the array stays reachable from the host
    // while deferred fixups hold it, then register one fixup per element in
    // reader-window batches through the native crossing riding readUserData
    // (TypeSerializeReference.cpp). The crossing also marks the references
    // registry active, so the references blob is consumed wrapper-side.
    //
    // Missing-type property paths: when ExecuteV2Read stamped the
    // registerSrReferenceField crossing (the persistent registry holds
    // missing types) and the command carries a collection segment, every
    // element registers its resolved per-index path, mirroring the native
    // element arm's RegisterReferenceField.
    private static unsafe void V2ReadSerializeReferenceArray(ref byte baseAddr, byte* cmdRaw, NativeReadBufferContext* ctx, byte* pathSegments, byte* pathNames)
    {
        var cmd = (V2CmdExternalArray*)cmdRaw;

        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        Type elementType = UnmarshalSystemType((IntPtr)cmd->elementTypeHandle);
        Array arr = AllocateOrReuseArrayBacking(
            ref baseAddr, cmd->kind, cmd->fieldOffset, elementType, count, out byte[] dataAsBytes);
        AssignArrayBacking(ref baseAddr, cmd->kind, cmd->fieldOffset, arr, dataAsBytes, count, elementType);

        var registerFixups = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, int, void>)(void*)cmd->readUserData;
        if (count == 0)
        {
            // Count-0 crossing still marks the registry active (the group
            // crossing's contract; v1 activates on every SR site).
            registerFixups(ctx->transferState, IntPtr.Zero, IntPtr.Zero, 0, 0);
            return;
        }

        bool registerPaths = ctx->registerSrReferenceField != IntPtr.Zero && cmd->segment != kV2NoPathSegment;
        var registerPath = (delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr, void>)(void*)ctx->registerSrReferenceField;

        const int kRefIdSize = 8;
        GCHandle arrayHandle = GCHandle.Alloc(arr);
        try
        {
            int processed = 0;
            while (processed < count)
            {
                if (ctx->readerEnd - ctx->readerPtr < kRefIdSize)
                    InvokeEnsureReadable(ctx, kRefIdSize);
                int batch = (int)(ctx->readerEnd - ctx->readerPtr) / kRefIdSize;
                int remaining = count - processed;
                if (batch > remaining)
                    batch = remaining;

                registerFixups(ctx->transferState, GCHandle.ToIntPtr(arrayHandle),
                    (IntPtr)ctx->readerPtr, batch, processed);

                if (registerPaths)
                {
                    for (int i = 0; i < batch; ++i)
                    {
                        // Handler-owned loop: no frame exists, so the element
                        // index passes directly (exactly one collection level).
                        byte[] identifier = BuildV2FuidIdentifier(pathSegments, pathNames, cmd->segment, default, processed + i);
                        if (identifier == null)
                            continue;
                        long refId = Unsafe.ReadUnaligned<long>(ctx->readerPtr + i * kRefIdSize);
                        fixed (byte* identifierPtr = identifier)
                            registerPath(ctx->transferState, refId, (IntPtr)identifierPtr);
                    }
                }

                ctx->readerPtr += batch * kRefIdSize;
                processed += batch;
            }
        }
        finally
        {
            arrayHandle.Free();
        }
    }

    // Group read: fixup registration. The whole group is one crossing into
    // the native helper riding userData (TypeSerializeReference.cpp). The
    // frame's object goes over as a GCHandle, which keeps the crossing
    // GC-safe on CoreCLR's moving GC; frameOffset carries the element base
    // for struct collection-element frames (0 on object frames). The helper
    // marks the references registry active (the references blob is consumed
    // wrapper-side) and registers one deferred fixup per entry, which
    // PerformFixups resolves once the blob has been read.
    private static unsafe void V2ReadSerializeReferenceGroup(object frameInstance, nint frameOffset, ref byte baseAddr, byte* entriesRaw, byte* readEntriesRaw, byte* fieldTableBase, int count, byte* input, NativeReadBufferContext* ctx, ulong userData)
    {
        var readGroupIntoObject = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, IntPtr, int, IntPtr, void>)(void*)userData;
        GCHandle frameHandle = GCHandle.Alloc(frameInstance);
        try
        {
            readGroupIntoObject(ctx->transferState, GCHandle.ToIntPtr(frameHandle), (int)frameOffset, (IntPtr)entriesRaw, count, (IntPtr)input);
        }
        finally
        {
            frameHandle.Free();
        }
    }
}
