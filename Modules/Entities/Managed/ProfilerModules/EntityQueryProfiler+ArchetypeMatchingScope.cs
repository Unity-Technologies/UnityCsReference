// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace Unity.Entities
{
    static partial class EntityQueryProfiler
    {
        // Times the query cache catching up with new archetypes, from construction to Dispose. Like CallScope, it
        // exists without ENABLE_PROFILER and does nothing then.
        [GenerateTestsForBurstCompatibility]
        internal readonly unsafe struct ArchetypeMatchingScope : IDisposable
        {
            readonly EntityComponentStore* m_Store;
            readonly EntityQueryManager* m_Queries;
            readonly int m_ArchetypeCount;
            readonly long m_StartTimestamp;

            public ArchetypeMatchingScope(EntityComponentStore* store, EntityQueryManager* queries, int archetypeCount)
            {
                m_Store = null;
                m_Queries = queries;
                m_ArchetypeCount = archetypeCount;
                m_StartTimestamp = 0;
                if (!ShouldRecord)
                    return;

                m_Store = store;
                m_StartTimestamp = BeginArchetypeMatching(store);
            }

            public void Dispose()
            {
                if (m_Store != null)
                    EndArchetypeMatching(m_Store, m_ArchetypeCount, m_Queries->QueryCacheEntryCount, m_StartTimestamp);
            }
        }
    }
}
