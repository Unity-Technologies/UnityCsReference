// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    internal struct AtomicAudioNode
    {
        internal IntPtr   m_Ptr;
        internal Int32    m_Version;

        public bool Equals(AtomicAudioNode other)
        {
            return m_Ptr == other.m_Ptr &&
                m_Version == other.m_Version;
        }
    }

    internal struct DSPNode
    {
        internal AtomicAudioNode m_Handle;
        internal AtomicAudioNode m_Graph;

        public bool Equals(DSPNode other)
        {
            return m_Handle.Equals(other.m_Handle) &&
                m_Graph.Equals(other.m_Graph);
        }
    }

    internal struct DSPConnection
    {
        internal AtomicAudioNode m_Handle;
        internal AtomicAudioNode m_Graph;

        public bool Equals(DSPConnection other)
        {
            return m_Handle.Equals(other.m_Handle) &&
                m_Graph.Equals(other.m_Graph);
        }
    }

    internal partial struct DSPGraph : IDisposable
    {
        internal AtomicAudioNode m_Handle;

        public static DSPGraph Create(SoundFormat outputFormat, uint outputChannels, uint dspBufferSize, uint sampleRate)
        {
            var graph = new DSPGraph();
            Internal_CreateDSPGraph(out graph, outputFormat, outputChannels, dspBufferSize, sampleRate);

            return graph;
        }

        public static DSPGraph GetDefaultGraph()
        {
            var graph = new DSPGraph();
            Internal_GetDefaultGraph(out graph);

            return graph;
        }

        public void Dispose()
        {
            Internal_DisposeDSPGraph(ref this);
        }

        public DSPCommandBlock CreateCommandBlock()
        {
            var block = new DSPCommandBlock();
            Internal_CreateDSPCommandBlock(ref this, ref block);

            return block;
        }

        public DSPNode GetRootDSP()
        {
            var root = new DSPNode();
            Internal_GetRootDSP(ref this, ref root);

            return root;
        }

        public ulong GetDSPClock()
        {
            return Internal_GetDSPClock(ref this);
        }

        public void BeginMix()
        {
            Internal_BeginMix(ref this);
        }

        public unsafe void ReadMix(NativeArray<float> buffer)
        {
            Internal_ReadMix(ref this, buffer.GetUnsafePtr<float>(), buffer.Length);
        }

        public unsafe uint AddNodeEventHandler<TNodeEvent>(Action<DSPNode, TNodeEvent> handler) where TNodeEvent : struct
        {
            return Internal_AddNodeEventHandler(ref this, GetTypeHashCode<TNodeEvent>(), handler);
        }

        public unsafe bool RemoveNodeEventHandler(uint handlerId)
        {
            return Internal_RemoveNodeEventHandler(ref this, handlerId);
        }

        public void Update()
        {
            Internal_Update(ref this);
        }

        static internal long GetTypeHashCode<T>()
        {
            // FIXME: Substitute with Burst.GetHashCode64<T>() when it becomes available.
            // Done in https://gitlab.internal.unity3d.com/burst/burst/commit/47e8a4daef5495373d86dfa00a4516326e9cac19
            return (long)(typeof(T).GetHashCode());
        }
    }
}

