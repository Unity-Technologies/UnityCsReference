// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Serialization;

// Managed serialization V2: state and helpers shared by both executors
// (WriteExecutor.cs, ReadExecutor.cs).
internal static unsafe partial class SerializationBackendManagedCommands
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Frame-change rebase. Cursor advances within a frame (loop backedges) add
    // the stride to baseAddr directly instead (byrefs are GC-updated alongside
    // the frame's Instance), so this runs only where the frame itself changes.
    private static ref byte V2RebaseTo(in V2ObjectFrame frame)
        => ref Unsafe.AddByteOffset(ref Unsafe.As<ObjectWrapper>(frame.Instance).Data, frame.Offset);

    // Frame index of `frame` within the executor's stack. Cold paths only
    // (element-source enter/exit bookkeeping); the hot loop never needs
    // depths.
    private static int V2FrameIndexOf(Span<V2ObjectFrame> frames, ref V2ObjectFrame frame)
        => (int)((nint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(frames), ref frame) / Unsafe.SizeOf<V2ObjectFrame>());

    // Per-thread frame storage, owned by one executor invocation at a time
    // and cleared in full on exit so it never roots user objects between
    // serializations. Its slot type is a user-visible struct shape, so it is
    // cleared on reload rather than persisted. Players never code-reload.
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.ResetToDefaultValue)]
    [ThreadStatic] private static V2ObjectFrame[] s_V2Frames;

    // Takes the pooled frame stack, leaving the slot empty: a transfer can
    // re-enter the executor on the same thread (a PPtr resolve mid-read
    // synchronously loads an object whose own read runs the executor, and
    // user callbacks can trigger the same), and a nested run sharing the
    // slot would overwrite the live outer stack and clear it on exit.
    private static V2ObjectFrame[] TakeV2Frames(int maxFrameDepth)
    {
        V2ObjectFrame[] frames = s_V2Frames;
        s_V2Frames = null;
        if (frames == null || frames.Length < maxFrameDepth)
            frames = new V2ObjectFrame[Math.Max(maxFrameDepth, 8)];
        return frames;
    }

    // The caller's finally clears the array before returning it. Last writer
    // wins, so after nesting the outermost call's array ends up pooled.
    private static void ReturnV2Frames(V2ObjectFrame[] frames)
    {
        s_V2Frames = frames;
    }

    // Byte offset from an object's first-field ref (ObjectWrapper.Data) to
    // an SZArray's first element. It is a runtime constant, identical for
    // every element type, so element frames can address array data through a
    // GC-tracked {array, offset} pair without pinning.
    //
    // The value lives in a nested holder with an explicit static constructor
    // (precise-init semantics), NOT in this class's own initializers: the
    // computation touches Unsafe, and the outer class must stay safe to touch
    // on players whose image cannot bind the Unsafe assembly (their v2
    // bootstrap fails, so those players cannot transfer scripted objects at
    // all, but bootstrap wiring and method resolution still initialize the
    // class). The holder's constructor
    // runs at first field access, which only executor code performs.
    private static class V2LayoutFacts
    {
        internal static readonly nint ArrayDataOffset;

        static V2LayoutFacts()
        {
            byte[] probe = new byte[1];
            ArrayDataOffset = Unsafe.ByteOffset(ref Unsafe.As<ObjectWrapper>((object)probe).Data, ref probe[0]);
        }
    }
}
