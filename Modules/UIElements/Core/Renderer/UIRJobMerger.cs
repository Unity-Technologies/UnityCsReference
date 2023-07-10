// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.UIElements.UIR
{
    // This class allows to add job handles in a native array in order to combine them when required.
    // When the array is full, all the current job handles are combined and assigned to slot 0.
    class JobMerger : IDisposable
    {
        NativeArray<JobHandle> m_Jobs;
        int m_JobCount;

        public JobMerger(int capacity)
        {
            Debug.Assert(capacity > 1);
            m_Jobs = new NativeArray<JobHandle>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        public void Add(JobHandle job)
        {
            if (m_JobCount < m_Jobs.Length)
            {
                m_Jobs[m_JobCount++] = job;
                return;
            }

            // Full
            m_Jobs[0] = JobHandle.CombineDependencies(m_Jobs);
            m_Jobs[1] = job;
            m_JobCount = 2;
        }

        public JobHandle MergeAndReset()
        {
            JobHandle mergedJob = new JobHandle();
            if (m_JobCount > 1)
                mergedJob = JobHandle.CombineDependencies(m_Jobs.Slice(0, m_JobCount));
            else if (m_JobCount == 1)
                mergedJob = m_Jobs[0];
            m_JobCount = 0;
            return mergedJob;
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_Jobs.Dispose();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
