// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.UIElements.UIR
{
    class OpacityIdAccelerator : System.IDisposable
    {
        struct OpacityIdUpdateJob : IJobParallelFor
        {
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeSlice<Vertex> oldVerts;
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeSlice<Vertex> newVerts;
            public ushort opacityId;

            public void Execute(int i)
            {
                Vertex vert = oldVerts[i];
                vert.opacityId = opacityId;
                newVerts[i] = vert;
            }
        }

        const int k_VerticesPerBatch = 128;
        const int k_JobLimit = 256;

        static readonly MemoryLabel k_MemoryLabel = new (nameof(UIElements), $"Renderer.{nameof(OpacityIdAccelerator)}");

        NativeArray<JobHandle> m_Jobs = new NativeArray<JobHandle>(k_JobLimit, k_MemoryLabel, NativeArrayOptions.UninitializedMemory);
        int m_NextJobIndex;

        public void CreateJob(NativeSlice<Vertex> oldVerts, NativeSlice<Vertex> newVerts, ushort opacityId, int vertexCount)
        {
            JobHandle jobHandle = new OpacityIdUpdateJob
            {
                oldVerts = oldVerts,
                newVerts = newVerts,
                opacityId = opacityId
            }.Schedule(vertexCount, k_VerticesPerBatch);

            if (m_NextJobIndex == m_Jobs.Length)
            {
                m_Jobs[0] = JobHandle.CombineDependencies(m_Jobs);
                m_NextJobIndex = 1;
                JobHandle.ScheduleBatchedJobs();
            }
            m_Jobs[m_NextJobIndex++] = jobHandle;
        }

        public void CompleteJobs()
        {
            if (m_NextJobIndex > 0)
            {
                if (m_NextJobIndex > 1)
                    JobHandle.CombineDependencies(m_Jobs.Slice(0, m_NextJobIndex)).Complete();
                else // We have only one job, no need to combine
                    m_Jobs[0].Complete();
            }
            m_NextJobIndex = 0;
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                m_Jobs.Dispose();
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
