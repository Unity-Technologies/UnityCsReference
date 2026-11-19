// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace Unity.Entities
{
    static partial class EntityQueryProfiler
    {
        // Times a query creation, from construction to Dispose. The creation sets the query id it points to once the
        // query exists; a creation that throws leaves it at -1 and is not recorded. Like CallScope, it exists without
        // ENABLE_PROFILER and does nothing then.
        [GenerateTestsForBurstCompatibility]
        internal readonly unsafe struct CreationScope : IDisposable
        {
            readonly EntityComponentStore* m_Store;
            readonly int* m_QueryId;
            readonly long m_StartTimestamp;

            public CreationScope(EntityComponentStore* store, int* queryId)
            {
                m_Store = null;
                m_QueryId = queryId;
                m_StartTimestamp = 0;
                if (!ShouldRecord)
                    return;

                m_Store = store;
                m_StartTimestamp = BeginCreation(store);
            }

            public void Dispose()
            {
                if (m_Store != null)
                    EndCreation(m_Store, *m_QueryId, m_StartTimestamp);
            }
        }
    }
}
