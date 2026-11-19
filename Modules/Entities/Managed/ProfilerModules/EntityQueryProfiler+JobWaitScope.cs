// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace Unity.Entities
{
    static partial class EntityQueryProfiler
    {
        // Times a wait for running jobs, from construction to Dispose, when it happens inside a recorded query call.
        // Like CallScope, it exists without ENABLE_PROFILER and does nothing then.
        [GenerateTestsForBurstCompatibility]
        internal readonly unsafe struct JobWaitScope : IDisposable
        {
            readonly EntityComponentStore* m_Store;
            readonly long m_StartTimestamp;

            public JobWaitScope(ref ComponentDependencyManager dependencyManager)
            {
                m_Store = null;
                m_StartTimestamp = 0;
                if (!ShouldRecord)
                    return;

                var store = dependencyManager.QueryProfilerStore;
                if (store == null)
                    return;

                m_StartTimestamp = BeginJobWait(store);
                if (m_StartTimestamp != 0)
                    m_Store = store;
            }

            public void Dispose()
            {
                if (m_Store != null)
                    EndJobWait(m_Store, m_StartTimestamp);
            }
        }
    }
}
