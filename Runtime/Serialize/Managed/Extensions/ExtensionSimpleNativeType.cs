// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 SimpleNativeType dynamic handler (doc §2.4):
// AnimationCurve, Gradient, RectOffset, GUIStyle. One shared handler. The
// per-class native dispatcher fnptr rides userData and the wrapper's m_Ptr
// post-header offset rides userData2, queried at registration so it is
// correct for the active scripting backend (CoreCLR may reorder fields).
// Sequence: flush staged bytes, read the native peer pointer (a null wrapper
// crosses as Zero and the dispatcher writes a default-constructed value), let
// the native Transfer write through the writer, resync.
internal static unsafe partial class SerializationBackendManagedCommands
{
    private static unsafe void V2WriteSimpleNativeType(ref byte field, NativeBufferContext* ctx, ref BufferDataStager stager, ulong dispatchFnPtr, ulong ptrFieldOffset)
    {
        stager.FlushStaged(kManagedBlockMaxPayloadSize);

        object wrapper = Unsafe.As<byte, object>(ref field);
        IntPtr nativePtr = wrapper != null
            ? Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
                ref Unsafe.As<ObjectWrapper>(wrapper).Data,
                (nint)ptrFieldOffset))
            : IntPtr.Zero;

        ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)(void*)dispatchFnPtr)(nativePtr, ctx->transfer, IntPtr.Zero);

        stager.ResyncWithNativeBuffer();
    }

    // Read (M7c). A null host slot constructs the wrapper first (its
    // parameterless ctor allocates the native peer; registration guarantees a
    // usable ctor), then the native read dispatcher consumes the CachedReader
    // into the peer after a syncReader rewind. readUserData4 is the optional
    // managed post-dispatch hook (GUIStyle.InternalOnAfterDeserialize).
    private static unsafe void V2ReadSimpleNativeType(ref byte field, byte* cmdRaw, NativeReadBufferContext* ctx)
    {
        var cmd = (V2CmdExternalDynamic*)cmdRaw;

        ref object slot = ref Unsafe.As<byte, object>(ref field);
        object wrapper = slot;
        if (wrapper == null)
        {
            wrapper = CreateWrapperInstance((IntPtr)cmd->readUserData2, (IntPtr)cmd->readUserData3);
            slot = wrapper;
        }

        IntPtr nativePtr = Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
            ref Unsafe.As<ObjectWrapper>(wrapper).Data,
            (nint)cmd->userData2));

        // Dispatch reads straight off the CachedReader; rewind the executor's
        // read-ahead first.
        InvokeSyncReader(ctx);
        ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)(void*)cmd->readUserData)(nativePtr, ctx->transfer, IntPtr.Zero);

        if (cmd->readUserData4 != 0)
            ((delegate*<object, IntPtr, void>)(void*)cmd->readUserData4)(wrapper, nativePtr);
    }
}
