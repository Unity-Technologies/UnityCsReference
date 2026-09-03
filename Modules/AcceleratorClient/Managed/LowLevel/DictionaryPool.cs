// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AssetDatabase not yet converted
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
namespace Unity.AcceleratorClient.LowLevel;


using System.Collections.Concurrent;
using Unity.Scripting.LifecycleManagement;

[NoAutoStaticsCleanup]
internal static class DictionaryPool
{
    // 32 covers a burst of concurrent GetBatch callers.
    private const int k_MaxRetained = 32;

    private static readonly ConcurrentStack<Dictionary<UInt128, ReadOnlyMemory<byte>>> s_Pool
        = new();

    internal static Dictionary<UInt128, ReadOnlyMemory<byte>> Rent(int suggestedCapacity)
    {
        if (s_Pool.TryPop(out var dict))
        {
            if (suggestedCapacity > dict.Count + 16)
                dict.EnsureCapacity(suggestedCapacity);
            return dict;
        }
        return new Dictionary<UInt128, ReadOnlyMemory<byte>>(suggestedCapacity);
    }

    internal static void Return(Dictionary<UInt128, ReadOnlyMemory<byte>> dict)
    {
        if (dict is null)
            return;
        // Soft cap: Count check is non-atomic with Push, so it may briefly exceed k_MaxRetained.
        if (s_Pool.Count >= k_MaxRetained)
            return;
        dict.Clear();
        s_Pool.Push(dict);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
