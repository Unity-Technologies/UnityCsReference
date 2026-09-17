// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 NativeValueStruct dynamic handler (doc §2.4):
// LoadableSceneId, LoadableObjectId, MonoReloadableIntPtr(Clear) (editor).
// These are inline value structs whose wire format diverges from their
// layout; the per-class native dispatcher (userData) runs the native Transfer
// on the field's own bytes. The interior-pointer crossing gets a scoped pin
// (doc §6.1) because the native Transfer may allocate.
internal static unsafe partial class SerializationBackendManagedCommands
{
    private static unsafe void V2WriteNativeValueStruct(ref byte field, NativeBufferContext* ctx, ref BufferDataStager stager, ulong dispatchFnPtr, ulong userData2)
    {
        stager.FlushStaged(kManagedBlockMaxPayloadSize);

        fixed (byte* fieldPtr = &field)
        {
            ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)(void*)dispatchFnPtr)((IntPtr)fieldPtr, ctx->transfer, IntPtr.Zero);
        }

        stager.ResyncWithNativeBuffer();
    }

    // Read (M7c): the read dispatcher consumes the CachedReader into the
    // field's own address after a syncReader rewind, under a scoped pin for
    // the interior crossing.
    private static unsafe void V2ReadNativeValueStruct(ref byte field, byte* cmdRaw, NativeReadBufferContext* ctx)
    {
        var cmd = (V2CmdExternalDynamic*)cmdRaw;
        fixed (byte* fieldPtr = &field)
        {
            InvokeSyncReader(ctx);
            ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)(void*)cmd->readUserData)((IntPtr)fieldPtr, ctx->transfer, IntPtr.Zero);
        }
    }
}
