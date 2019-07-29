// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using Unity.Jobs;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace Unity.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DSPGraphExecutionNode
    {
        public void* ReflectionData;
        public void* JobStructData;
        public void* JobData;
        public void* ResourceContext;
        public int FunctionIndex;
        public int FenceIndex;
        public int FenceCount;
    }

    [NativeType(Header = "Modules/DSPGraph/Public/DSPGraph.bindings.h")]
    internal struct DSPGraphInternal
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_CreateDSPGraph(out Handle graph, int outputFormat, uint outputChannels, uint dspBufferSize, uint sampleRate);

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

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern void Internal_BeginMix(ref Handle graph, int frameCount, int executionMode);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern unsafe void Internal_ReadMix(ref Handle graph, void* buffer, int frameCount);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_Update(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern bool Internal_AssertMixerThread(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern bool Internal_AssertMainThread(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern Handle Internal_AllocateHandle(ref Handle graph);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern unsafe void Internal_InitializeJob(void* jobStructData, void* jobReflectionData, void* resourceContext);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern unsafe void Internal_ExecuteJob(void* jobStructData, void* jobReflectionData, void* jobData, void* resourceContext);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern unsafe void Internal_ExecuteUpdateJob(void* updateStructMemory, void* updateReflectionData, void* jobStructMemory, void* jobReflectionData, void* resourceContext, ref Handle requestHandle, ref JobHandle fence);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern unsafe void Internal_DisposeJob(void* jobStructData, void* jobReflectionData, void* resourceContext);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern unsafe void Internal_ScheduleGraph(JobHandle inputDeps, void* nodes, int nodeCount, int* childTable, void* dependencies);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        public static extern void Internal_SyncFenceNoWorkSteal(JobHandle handle);
    }
}

