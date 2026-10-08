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
    // Scalar group: consecutive SR slots in one fixed run are dest-contiguous
    // (each advances the run by 8), so the whole group is one batched crossing
    // popping count RefIds off the shared cursor.
    private static unsafe void V2WriteSerializeReferenceGroup(ref byte baseAddr, byte* entriesRaw, int count, byte* output, NativeBufferContext* ctx, ulong userData)
    {
        var entry = (V2ExternalFixedEntry*)entriesRaw;
        WriteManagedReferencesToBuffer(ctx->transferState, (IntPtr)(output + entry->destOffset), count);
    }

    // T[] / List<T>: count prefix plus count 8-byte RefIds in staging-window
    // batches. The backing array is not read, so only the count matters. An
    // empty collection still crosses, which marks the registry active; a null
    // one never reaches here (the executor writes its empty count).
    private static unsafe void V2WriteSerializeReferenceArray(byte[] dataAsBytes, int count, uint stride, NativeBufferContext* ctx, ulong userData)
    {
        ConsumeLinearCollectionManagedReferenceArray(ctx, count);
    }

    // Gather arms: mirrors of the batched write handlers above, which never
    // read the field and only emit, so V2BuildGatherStream swaps them into the
    // filtered copy and one executor serves both passes. They take the FUID
    // context the write arms have no use for -- the command's path segments
    // and the live frames -- which is what lets this pass own missing-type
    // resolution; the filtered copy's kExternal*WithFUID opcodes hand it over.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void RegisterGatheredReference(NativeBufferContext* ctx, object value)
    {
        // The raw header pointer forwards without a GCHandle; the object stays
        // rooted in whatever produced it. Null registers too: that is what
        // records RefId_Null.
        IntPtr objPtr = Unsafe.As<object, IntPtr>(ref value);
        ctx->registerGatheredRef(ctx->transferState, objPtr);
    }

    // A null [SerializeReference] whose type went missing on load must still
    // reach the registry, under the key the read registered it with.
    // handlerElementIndex is the site's index in this handler-owned loop,
    // which has no frame of its own; -1 at a scalar site.
    private static unsafe void ResolveGatheredMissingType(NativeBufferContext* ctx, uint segment,
        Span<V2ObjectFrame> frames, int handlerElementIndex)
    {
        if (ctx->resolveGatheredMissingType == null || segment == kV2NoPathSegment || ctx->pathSegments == null)
            return;
        byte[] identifier = BuildV2FuidIdentifier(ctx->pathSegments, ctx->pathNames, segment,
            frames, handlerElementIndex);
        if (identifier == null)
            return;
        fixed (byte* identifierPtr = identifier)
            ctx->resolveGatheredMissingType(ctx->transferState, identifierPtr);
    }

    private static unsafe void V2GatherSerializeReferenceGroup(ref byte baseAddr, byte* entriesRaw, int count,
        byte* output, uint* segments, Span<V2ObjectFrame> frames, NativeBufferContext* ctx, ulong userData)
    {
        var entries = (V2ExternalFixedEntry*)entriesRaw;
        for (int i = 0; i < count; ++i)
        {
            object value = V2FieldRef<object>(ref baseAddr, (nint)entries[i].fieldOffset);
            RegisterGatheredReference(ctx, value);
            if (value == null && segments != null)
                ResolveGatheredMissingType(ctx, segments[i], frames, -1);
        }
    }

    private static unsafe void V2GatherSerializeReferenceArray(byte[] dataAsBytes, int count, uint stride,
        uint segment, Span<V2ObjectFrame> frames, NativeBufferContext* ctx, ulong userData)
    {
        // No count word: the write arm hands the whole framing to the array
        // handler, and this pass emits nothing.
        object[] elements = Unsafe.As<byte[], object[]>(ref dataAsBytes);
        for (int e = 0; e < count; ++e)
        {
            object value = elements[e];
            RegisterGatheredReference(ctx, value);
            if (value == null)
                ResolveGatheredMissingType(ctx, segment, frames, e);
        }
    }

    // T[] / List<T> read: count 8-byte RefIds assigned into the backing array
    // in reader-window batches through the native crossing riding readUserData
    // (TypeSerializeReference.cpp). The referenced instances are already live,
    // because the registry body precedes the object body in the stream.
    //
    // Missing-type property paths: when ExecuteV2Read stamped the
    // registerSrReferenceField crossing (the persistent registry holds
    // missing types) and the command carries a collection segment, every
    // element registers its resolved per-index path, mirroring the native
    // element arm's RegisterReferenceField.
    // The enter read the count, bound the collection to the field and pushed the element frame. It
    // skips the body at count 0, so count is always positive here; ExecuteV2Read marks the registry
    // active once per transfer from templ.hasSerializeReferenceInSubtree, so no per-site activation
    // is needed.
    private static unsafe void V2ReadSerializeReferenceArray(
        byte[] backing, int count, long stride, byte* cmdRaw, NativeReadBufferContext* ctx,
        byte* pathSegments, byte* pathNames)
    {
        var cmd = (V2CmdExternalArray*)cmdRaw;
        byte[] dataAsBytes = backing;
        var assignElements = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, int, void>)(void*)cmd->readUserData;

        bool registerPaths = ctx->registerSrReferenceField != IntPtr.Zero && cmd->segment != kV2NoPathSegment;
        var registerPath = (delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr, void>)(void*)ctx->registerSrReferenceField;

        const int kRefIdSize = 8;
        GCHandle arrayHandle = GCHandle.Alloc(dataAsBytes);
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

                assignElements(ctx->transferState, GCHandle.ToIntPtr(arrayHandle),
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

    // Group read: the whole group is one crossing into the native helper riding
    // userData (TypeSerializeReference.cpp), which assigns one resolved
    // reference per entry. The frame's object goes over as a GCHandle, which
    // keeps the crossing GC-safe on CoreCLR's moving GC; frameOffset carries
    // the element base for struct collection-element frames (0 on object
    // frames).
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
