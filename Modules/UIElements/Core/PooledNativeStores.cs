// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    // A native store that reserves memory in chunks and reports how much of it is in use
    interface IPooledNativeStore
    {
        string storeName { get; }
        long GetBytesReserved();
        long GetBytesUsed();
        void ReleasePendingSlots();
    }

    // Editor only. Weak references let an undisposed store be collected and finalized
    static partial class PooledNativeStores
    {
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.CaptureInitializationExpression)]
        [IgnoreForUAL0015("Live-store registry, refilled by each store's constructor after cleanup")]
        static List<WeakReference<IPooledNativeStore>> s_Stores = new();

        [Conditional("UNITY_EDITOR")]
        public static void Register(IPooledNativeStore store)
        {
            if (IndexOf(store) < 0)
                s_Stores.Add(new WeakReference<IPooledNativeStore>(store));
        }

        [Conditional("UNITY_EDITOR")]
        public static void Unregister(IPooledNativeStore store)
        {
            var index = IndexOf(store);
            if (index >= 0)
                s_Stores.RemoveAt(index);
        }

        public static void GetStores(List<IPooledNativeStore> stores)
        {
            s_Stores.RemoveAll(reference => !reference.TryGetTarget(out _));
            stores.Add(new StyleDataPoolStore());
            foreach (var reference in s_Stores)
            {
                if (reference.TryGetTarget(out var store))
                    stores.Add(store);
            }
        }

        static int IndexOf(IPooledNativeStore store)
        {
            for (var i = 0; i < s_Stores.Count; ++i)
            {
                if (s_Stores[i].TryGetTarget(out var target) && target == store)
                    return i;
            }
            return -1;
        }
    }
}
