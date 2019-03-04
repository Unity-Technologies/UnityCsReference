// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/DSPGraph.bindings.h")]
    internal struct DSPGraphInternal
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_CreateDSPGraph(out Handle graph, int outputFormat, uint outputChannels, uint dspBufferSize, uint sampleRate);

        [NativeMethod(IsFreeFunction = true)]
        public static extern void Internal_GetDefaultGraph(out Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_DisposeDSPGraph(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_CreateDSPCommandBlock(ref Handle graph, ref Handle block);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern uint Internal_AddNodeEventHandler(
            ref Handle graph, long eventTypeHashCode, object handler);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern bool Internal_RemoveNodeEventHandler(ref Handle graph, uint handlerId);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetRootDSP(ref Handle graph, ref Handle root);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern ulong Internal_GetDSPClock(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_BeginMix(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_ReadMix(ref Handle graph, void* buffer, int length);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_Update(ref Handle graph);
    }
}

