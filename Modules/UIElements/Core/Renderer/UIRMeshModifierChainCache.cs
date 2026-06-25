// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    // Caches effective mesh-modifier chains: equal content shares one List, so reference
    // equality means content equality. Long-lived holders (RenderData.m_EffectiveModifiers)
    // call Acquire/Release; Prune() reclaims chains left with no holder after a walk.
    sealed class MeshModifierChainCache : IDisposable
    {
        sealed class ChainInfo
        {
            public List<MeshModifierRegistration> chain;
            public int refCount;
            public ChainInfo nextFree;
        }

        sealed class RefComparer : IEqualityComparer<List<MeshModifierRegistration>>
        {
            public static readonly RefComparer Instance = new();
            public bool Equals(List<MeshModifierRegistration> x, List<MeshModifierRegistration> y) => ReferenceEquals(x, y);
            public int GetHashCode(List<MeshModifierRegistration> obj) => RuntimeHelpers.GetHashCode(obj);
        }

        readonly Dictionary<int, List<ChainInfo>> m_Buckets = new();
        readonly Dictionary<List<MeshModifierRegistration>, ChainInfo> m_InfoByChain = new(RefComparer.Instance);
        readonly List<int> m_PruneEmptyKeys = new();
        ChainInfo m_FreeChainInfoHead;

        public List<MeshModifierRegistration> GetShared(List<MeshModifierRegistration> content)
        {
            if (content == null || content.Count == 0)
                return null;

            int hash = ComputeHash(content);
            if (m_Buckets.TryGetValue(hash, out var bucket))
            {
                for (int i = 0; i < bucket.Count; ++i)
                    if (ContentEquals(content, bucket[i].chain))
                        return bucket[i].chain;
            }
            else
            {
                bucket = new List<ChainInfo>();
                m_Buckets[hash] = bucket;
            }

            var info = AcquireChainInfo(content);
            bucket.Add(info);
            m_InfoByChain[info.chain] = info;
            return info.chain;
        }

        public void Acquire(List<MeshModifierRegistration> chain)
        {
            if (chain == null)
                return;
            m_InfoByChain[chain].refCount++;
        }

        public void Release(List<MeshModifierRegistration> chain)
        {
            if (chain == null)
                return;
            var info = m_InfoByChain[chain];
            if (--info.refCount == 0)
                RemoveInfo(info);
        }

        // Reclaims chains with refCount == 0. Must be called AFTER every ProcessChanges walk completes, not during.
        public void Prune()
        {
            foreach (var kvp in m_Buckets)
            {
                var bucket = kvp.Value;
                for (int i = bucket.Count - 1; i >= 0; --i)
                {
                    var info = bucket[i];
                    if (info.refCount == 0)
                    {
                        m_InfoByChain.Remove(info.chain);
                        bucket.RemoveAt(i);
                        ReleaseChainInfo(info);
                    }
                }
                if (bucket.Count == 0)
                    m_PruneEmptyKeys.Add(kvp.Key);
            }
            for (int i = 0; i < m_PruneEmptyKeys.Count; ++i)
                m_Buckets.Remove(m_PruneEmptyKeys[i]);
            m_PruneEmptyKeys.Clear();
        }

        public void Dispose()
        {
            m_Buckets.Clear();
            m_InfoByChain.Clear();
            m_FreeChainInfoHead = null;
        }

        void RemoveInfo(ChainInfo info)
        {
            m_InfoByChain.Remove(info.chain);
            int hash = ComputeHash(info.chain);
            if (m_Buckets.TryGetValue(hash, out var bucket))
            {
                bucket.Remove(info);
                if (bucket.Count == 0)
                    m_Buckets.Remove(hash);
            }
            ReleaseChainInfo(info);
        }

        ChainInfo AcquireChainInfo(List<MeshModifierRegistration> content)
        {
            var info = m_FreeChainInfoHead;
            if (info != null)
            {
                m_FreeChainInfoHead = info.nextFree;
                info.nextFree = null;
                info.chain.AddRange(content);
            }
            else
            {
                info = new ChainInfo { chain = new List<MeshModifierRegistration>(content) };
            }
            info.refCount = 0;
            return info;
        }

        void ReleaseChainInfo(ChainInfo info)
        {
            info.chain.Clear();
            info.nextFree = m_FreeChainInfoHead;
            m_FreeChainInfoHead = info;
        }

        static int ComputeHash(List<MeshModifierRegistration> chain)
        {
            int h = 17;
            for (int i = 0; i < chain.Count; ++i)
            {
                var r = chain[i];
                h = unchecked(h * 31 + RuntimeHelpers.GetHashCode(r.callback));
                h = unchecked(h * 31 + r.priority);
                h = unchecked(h * 31 + r.id.GetHashCode());
                h = unchecked(h * 31 + (r.recursive ? 1 : 0));
            }
            return h;
        }

        static bool ContentEquals(List<MeshModifierRegistration> a, List<MeshModifierRegistration> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; ++i)
            {
                var ra = a[i];
                var rb = b[i];
                if (!ReferenceEquals(ra.callback, rb.callback))
                    return false;
                if (ra.priority != rb.priority)
                    return false;
                if (ra.id != rb.id)
                    return false;
                if (ra.recursive != rb.recursive)
                    return false;
            }
            return true;
        }
    }
}
