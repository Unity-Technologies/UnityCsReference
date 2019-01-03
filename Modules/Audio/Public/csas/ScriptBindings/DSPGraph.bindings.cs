// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Experimental.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/DSPGraph.bindings.h")]
    internal partial struct DSPGraph
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_CreateDSPGraph(out DSPGraph graph, SoundFormat outputFormat, uint outputChannels, uint dspBufferSize, uint sampleRate);

        [NativeMethod(IsFreeFunction = true)]
        static extern void Internal_GetDefaultGraph(out DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_DisposeDSPGraph(ref DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_CreateDSPCommandBlock(ref DSPGraph graph, ref DSPCommandBlock block);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern uint Internal_AddNodeEventHandler(
            ref DSPGraph graph, long eventTypeHashCode, object handler);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern bool Internal_RemoveNodeEventHandler(ref DSPGraph graph, uint handlerId);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_GetRootDSP(ref DSPGraph graph, ref DSPNode root);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern ulong Internal_GetDSPClock(ref DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_BeginMix(ref DSPGraph graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_ReadMix(ref DSPGraph graph, void* buffer, int length);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_Update(ref DSPGraph graph);
    }
}

