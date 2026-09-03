// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
namespace Unity.AcceleratorClient.LowLevel;

using Unity.AcceleratorClient.Contracts;

internal sealed class BatchGetRaw : IDisposable
{
    // Faults dict isn't pooled (rare, O(fault-count) not O(batch-size)).
    // Callers must Dispose after consuming Hits/Faults; reading Hits after Dispose is undefined.
    private Dictionary<UInt128, ReadOnlyMemory<byte>>? m_HitsPool;

    internal IReadOnlyDictionary<UInt128, ReadOnlyMemory<byte>> Hits { get; }

    internal IReadOnlyDictionary<UInt128, CacheError> Faults { get; }

    internal BatchGetRaw(
        Dictionary<UInt128, ReadOnlyMemory<byte>> hitsPooled,
        IReadOnlyDictionary<UInt128, CacheError> faults)
    {
        m_HitsPool = hitsPooled;
        Hits = hitsPooled;
        Faults = faults;
    }

    public void Dispose()
    {
        // Exchange-to-null makes this idempotent/concurrent-safe: exactly one caller wins.
        var pool = Interlocked.Exchange(ref m_HitsPool, null);
        if (pool is not null)
            DictionaryPool.Return(pool);
    }
}
