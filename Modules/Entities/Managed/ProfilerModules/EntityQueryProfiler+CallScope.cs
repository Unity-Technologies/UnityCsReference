// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace Unity.Entities
{
    static partial class EntityQueryProfiler
    {
        // Times one main-thread operation on a query, from construction to Dispose. It exists without ENABLE_PROFILER
        // so call sites need no #if; it does nothing then.
        [GenerateTestsForBurstCompatibility]
        internal readonly unsafe struct CallScope : IDisposable
        {
            readonly EntityComponentStore* m_Store;
            readonly long m_StartTimestamp;

            public CallScope(Operation operation, EntityQueryImpl* query)
            {
                m_Store = null;
                m_StartTimestamp = 0;
                if (!ShouldRecord)
                    return;

                m_Store = query->_Access->EntityComponentStore;
                m_StartTimestamp = BeginCall(m_Store, operation, query->_QueryData->QueryDataIndex);
            }

            public void Dispose()
            {
                if (m_Store != null)
                    EndCall(m_Store, m_StartTimestamp);
            }
        }
    }
}
