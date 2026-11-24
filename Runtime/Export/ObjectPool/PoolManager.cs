// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Pool
{
    static class PoolManager
    {
        // Registry of weak references to live pools — no strong references to user code, and weak
        // references do not pin the old ALC. Must persist across code reload: the readonly s_Pool
        // fields are cleaned by calling Clear() on the existing pool instance (the field is never
        // reassigned), so pools not tied to a collectible ALC survive reload — emptied, but alive.
        // Pools only register in their constructor, so wiping this list would orphan the survivors
        // and Reset() would stop clearing them. Entries whose pools died with their ALC pin
        // nothing and are pruned lazily in Reset().
        [NoAutoStaticsCleanup]
        static readonly List<WeakReference<IPool>> s_WeakPoolReferences = new();

        public static void Reset()
        {
            for (int i = s_WeakPoolReferences.Count - 1; i >= 0; i--)
            {
                if (s_WeakPoolReferences[i].TryGetTarget(out var pool))
                {
                    pool.Reset();
                }
                else
                {
                    s_WeakPoolReferences.RemoveAt(i);
                }
            }
        }

        public static void Register(IPool pool)
        {
            s_WeakPoolReferences.Add(new WeakReference<IPool>(pool));
        }
    }
}
