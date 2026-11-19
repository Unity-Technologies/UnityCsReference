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
// Sequence: read the native peer pointer (a null wrapper crosses as Zero and
// the dispatcher writes a default-constructed value), then one crossing
// commits the staged bytes, lets the native Transfer write through the writer,
// and re-arms the window.
internal static unsafe partial class SerializationBackendManagedCommands
{
    private static unsafe void V2WriteSimpleNativeType(ref byte field, NativeBufferContext* ctx, ulong dispatchFnPtr, ulong ptrFieldOffset)
    {
        object wrapper = Unsafe.As<byte, object>(ref field);
        IntPtr nativePtr = wrapper != null
            ? Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
                ref Unsafe.As<ObjectWrapper>(wrapper).Data,
                (nint)ptrFieldOffset))
            : IntPtr.Zero;

        ctx->dispatchNative(ctx, (nint)dispatchFnPtr, nativePtr);
    }

    // Read (M7c). A null host slot constructs the wrapper first (its
    // parameterless ctor allocates the native peer; registration guarantees a
    // usable ctor), then the native read dispatcher reads into the peer
    // (V2DispatchNativeRead). readUserData4 is the optional
    // managed post-dispatch hook (GUIStyle.InternalOnAfterDeserialize).
    private static unsafe void V2ReadSimpleNativeType(ref byte field, byte* cmdRaw, NativeReadBufferContext* ctx)
    {
        var cmd = (V2CmdExternalDynamic*)cmdRaw;
        IntPtr nativePtr = V2SimpleNativePeerForRead(ref field, (nint)cmd->readUserData2, (nint)cmd->readUserData3, (nint)cmd->userData2, out object wrapper);

        V2DispatchNativeRead(ctx, (nint)cmd->readUserData, nativePtr);

        if (cmd->readUserData4 != 0)
            ((delegate*<object, IntPtr, void>)(void*)cmd->readUserData4)(wrapper, nativePtr);
    }

    // kMappedNativeRead: the stored node goes back to the mapped read's SafeBinaryRead, which leaves the reader past it.
    private static unsafe void V2ReadMappedNativeType(ref byte field, V2CmdMappedNativeRead* cmd, NativeReadBufferContext* ctx)
    {
        if (ctx->mappedSource == IntPtr.Zero)
            throw new InvalidOperationException("kMappedNativeRead ran outside a mapped read, which has no SafeBinaryRead to hand the stored node to.");
        if (cmd->wrapperTypeHandle == 0)
        {
            InvokeSyncReader(ctx);
            fixed (byte* fieldPtr = &field)
            {
                Unsafe.InitBlockUnaligned(fieldPtr, 0, cmd->clearBytes);
                ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)(void*)cmd->dispatcher)((IntPtr)fieldPtr, ctx->mappedSource, (IntPtr)cmd->storedNode);
            }
            return;
        }

        IntPtr nativePtr = V2SimpleNativePeerForRead(ref field, (nint)cmd->wrapperTypeHandle, (nint)cmd->wrapperCtor, (nint)cmd->ptrFieldOffset, out object wrapper);

        InvokeSyncReader(ctx);
        ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)(void*)cmd->dispatcher)(nativePtr, ctx->mappedSource, (IntPtr)cmd->storedNode);

        if (cmd->postDispatch != 0)
            ((delegate*<object, IntPtr, void>)(void*)cmd->postDispatch)(wrapper, nativePtr);
    }

    private static unsafe IntPtr V2SimpleNativePeerForRead(ref byte field, nint wrapperTypeHandle, nint wrapperCtor, nint ptrFieldOffset, out object wrapper)
    {
        ref object slot = ref Unsafe.As<byte, object>(ref field);
        wrapper = slot;
        if (wrapper == null)
        {
            wrapper = CreateWrapperInstance(wrapperTypeHandle, wrapperCtor);
            slot = wrapper;
        }

        return Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
            ref Unsafe.As<ObjectWrapper>(wrapper).Data,
            ptrFieldOffset));
    }
}
