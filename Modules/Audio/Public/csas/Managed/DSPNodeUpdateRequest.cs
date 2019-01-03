// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    internal partial struct DSPNodeUpdateRequestHandle
    {
        public AtomicAudioNode m_Handle;
        public AtomicAudioNode m_Graph;
    }

    internal struct DSPNodeUpdateRequest<TAudioJobUpdate, TParams, TProvs, TAudioJob> : IDisposable
        where TParams         : struct, IConvertible
        where TProvs          : struct, IConvertible
        where TAudioJob       : struct, IAudioJob<TParams, TProvs>
        where TAudioJobUpdate : struct, IAudioJobUpdate<TParams, TProvs, TAudioJob>
    {
        internal DSPNodeUpdateRequestHandle m_Handle;

        public unsafe TAudioJobUpdate UpdateJob
        {
            get
            {
                var updateData = DSPNodeUpdateRequestHandle.Internal_GetUpdateJobData(ref m_Handle);
                var updateJob = new TAudioJobUpdate();

                if (updateData != null)
                    UnsafeUtility.CopyPtrToStructure(updateData, out updateJob);

                return updateJob;
            }
        }

        public unsafe bool Done
        {
            get
            {
                var updateData = DSPNodeUpdateRequestHandle.Internal_GetUpdateJobData(ref m_Handle);
                return updateData != null;
            }
        }

        public bool HasError
        {
            get
            {
                return DSPNodeUpdateRequestHandle.Internal_HasError(ref m_Handle);
            }
        }

        public DSPNode Node
        {
            get
            {
                var node = new DSPNode();
                DSPNodeUpdateRequestHandle.Internal_GetDSPNode(ref m_Handle, ref node);
                return node;
            }
        }

        public JobHandle Fence
        {
            get
            {
                var fence = new JobHandle();
                DSPNodeUpdateRequestHandle.Internal_GetFence(ref m_Handle, ref fence);
                return fence;
            }
        }

        public void Dispose()
        {
            DSPNodeUpdateRequestHandle.Internal_Dispose(ref m_Handle);
        }
    }
}

