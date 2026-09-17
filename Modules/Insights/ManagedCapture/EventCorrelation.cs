// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.ManagedCapture.Internal
{
    // Stitches related events under one shared id. Opting out of statics cleanup (on each static
    // member below) keeps a group's id across Editor code reloads.
    internal static class EventCorrelation
    {
        // Wire values shared with the linker — do not renumber.
        internal const int k_Reuse = 0;
        internal const int k_Fresh = 1;
        internal const int k_Release = 2;

        internal const int k_MaxKeys = 1024;

        [NoAutoStaticsCleanup] static ConditionalWeakTable<object, StrongBox<string>> s_ByInstance = new ConditionalWeakTable<object, StrongBox<string>>();
        [NoAutoStaticsCleanup] static readonly object s_InstanceLock = new object();
        [NoAutoStaticsCleanup] static readonly ConcurrentDictionary<string, Entry> s_ByKey = new ConcurrentDictionary<string, Entry>();

        // ConcurrentDictionary.Count acquires every internal lock, and the trim check runs per event.
        [NoAutoStaticsCleanup] static int s_KeyCount;
        [NoAutoStaticsCleanup] static long s_Sequence;

        [ThreadStatic, NoAutoStaticsCleanup] static object s_Key;
        [ThreadStatic, NoAutoStaticsCleanup] static string s_Field;
        [ThreadStatic, NoAutoStaticsCleanup] static int s_Role;

        internal static int KeyCount => Volatile.Read(ref s_KeyCount);

        // Sequence gives the trim an age to evict by, and lets TryUpdate compare-and-swap on it alone.
        readonly struct Entry : IEquatable<Entry>
        {
            public readonly string Id;
            public readonly long Sequence;

            public Entry(string id, long sequence)
            {
                Id = id;
                Sequence = sequence;
            }

            public bool Equals(Entry other) => Sequence == other.Sequence;
            public override bool Equals(object obj) => obj is Entry other && Equals(other);
            public override int GetHashCode() => Sequence.GetHashCode();
        }

        internal static void Set(object key, string field, int role)
        {
            s_Key = key;
            s_Field = field;
            s_Role = role;
        }

        internal static void Clear()
        {
            s_Key = null;
            s_Field = null;
            s_Role = k_Reuse;
        }

        internal static bool TryResolve(out string field, out string id)
        {
            if (s_Key == null || s_Field == null)
            {
                field = null;
                id = null;
                return false;
            }

            field = s_Field;
            id = ResolveId();
            return true;
        }

        internal static void ResetStateForTests()
        {
            s_ByKey.Clear();
            Volatile.Write(ref s_KeyCount, 0);
            Volatile.Write(ref s_Sequence, 0);

            // ConditionalWeakTable has no Clear() on this API surface (netstandard2.0); swap in a
            // fresh table instead, under the same lock production code uses to touch it.
            lock (s_InstanceLock)
                s_ByInstance = new ConditionalWeakTable<object, StrongBox<string>>();
        }

        static string ResolveId()
        {
            var key = s_Key;
            if (key is string keyString)
                return ResolveStringKeyId(keyString, s_Role);

            return ResolveInstanceKeyId(key, s_Role);
        }

        // Serializes Fresh/Release/Reuse per instance: without it, a Release can drop an entry a
        // concurrent Fresh just wrote, orphaning the fresh id.
        static string ResolveInstanceKeyId(object key, int role)
        {
            lock (s_InstanceLock)
            {
                bool fresh = role == k_Fresh;
                bool release = role == k_Release;
                if (release)
                {
                    if (s_ByInstance.TryGetValue(key, out var box))
                    {
                        s_ByInstance.Remove(key);
                        return box.Value;
                    }
                    return NewId();
                }

                if (fresh)
                {
                    var id = NewId();
                    s_ByInstance.GetValue(key, _ => new StrongBox<string>(id)).Value = id;
                    return id;
                }

                return s_ByInstance.GetValue(key, _ => new StrongBox<string>(NewId())).Value;
            }
        }

        static string ResolveStringKeyId(string keyString, int role)
        {
            if (role == k_Release)
            {
                if (s_ByKey.TryRemove(keyString, out var removed))
                {
                    Interlocked.Decrement(ref s_KeyCount);
                    return removed.Id;
                }
                return NewId();
            }

            if (role == k_Fresh)
            {
                var freshEntry = NewEntry();

                // TryUpdate only replaces the entry we just read, so a concurrently removed one falls
                // through to retrying TryAdd instead of silently becoming an uncounted insert.
                while (!s_ByKey.TryAdd(keyString, freshEntry))
                {
                    if (s_ByKey.TryGetValue(keyString, out var current) && s_ByKey.TryUpdate(keyString, freshEntry, current))
                        return freshEntry.Id;
                }

                Interlocked.Increment(ref s_KeyCount);
                TrimStringKeysIfNeeded();
                return freshEntry.Id;
            }

            if (s_ByKey.TryGetValue(keyString, out var existing))
                return existing.Id;

            var entry = NewEntry();
            if (s_ByKey.TryAdd(keyString, entry))
            {
                Interlocked.Increment(ref s_KeyCount);
                TrimStringKeysIfNeeded();
                return entry.Id;
            }

            // The first writer's id wins so out-of-order siblings still agree.
            return s_ByKey.TryGetValue(keyString, out var raced) ? raced.Id : entry.Id;
        }

        static Entry NewEntry() => new Entry(NewId(), Interlocked.Increment(ref s_Sequence));

        static string NewId() => Guid.NewGuid().ToString("D");

        // Second pass guarantees progress: a burst of same-age entries defeats the age cutoff.
        static void TrimStringKeysIfNeeded()
        {
            if (Volatile.Read(ref s_KeyCount) <= k_MaxKeys)
                return;

            int lowWater = k_MaxKeys * 3 / 4;
            long cutoff = Volatile.Read(ref s_Sequence) - lowWater;

            foreach (var pair in s_ByKey)
            {
                if (Volatile.Read(ref s_KeyCount) <= lowWater)
                    return;
                if (pair.Value.Sequence <= cutoff && s_ByKey.TryRemove(pair.Key, out _))
                    Interlocked.Decrement(ref s_KeyCount);
            }

            foreach (var pair in s_ByKey)
            {
                if (Volatile.Read(ref s_KeyCount) <= lowWater)
                    return;
                if (s_ByKey.TryRemove(pair.Key, out _))
                    Interlocked.Decrement(ref s_KeyCount);
            }
        }
    }
}
