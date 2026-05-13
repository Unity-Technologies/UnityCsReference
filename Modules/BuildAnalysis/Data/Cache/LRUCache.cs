// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Build.Analysis
{
    internal class LRUCache<TKey, TValue> where TValue : class
    {
        private class CacheNode
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
        }

        private readonly int m_Capacity;
        private readonly LinkedList<CacheNode> m_List;
        private readonly Dictionary<TKey, LinkedListNode<CacheNode>> m_Map;
        private readonly object m_Lock = new object();

        public LRUCache(int capacity = 20)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero", nameof(capacity));

            m_Capacity = capacity;
            m_List = new LinkedList<CacheNode>();
            m_Map = new Dictionary<TKey, LinkedListNode<CacheNode>>(capacity);
        }

        public TValue Get(TKey key)
        {
            if (key == null)
                return null;

            lock (m_Lock)
            {
                if (!m_Map.TryGetValue(key, out var node))
                    return null;

                // Move to front (most recently used)
                if (node != m_List.First)
                {
                    m_List.Remove(node);
                    m_List.AddFirst(node);
                }

                return node.Value.Value;
            }
        }

        public void Put(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            lock (m_Lock)
            {
                // Update existing entry
                if (m_Map.TryGetValue(key, out var existingNode))
                {
                    existingNode.Value.Value = value;

                    // Move to front
                    if (existingNode != m_List.First)
                    {
                        m_List.Remove(existingNode);
                        m_List.AddFirst(existingNode);
                    }
                    return;
                }

                var newNode = new LinkedListNode<CacheNode>(new CacheNode
                {
                    Key = key,
                    Value = value
                });

                m_List.AddFirst(newNode);
                m_Map[key] = newNode;

                // Evict least recently used if over capacity
                if (m_Map.Count > m_Capacity)
                {
                    var lru = m_List.Last;
                    if (lru != null)
                    {
                        m_List.RemoveLast();
                        m_Map.Remove(lru.Value.Key);
                    }
                }
            }
        }

        public void Remove(TKey key)
        {
            if (key == null)
                return;

            lock (m_Lock)
            {
                if (m_Map.TryGetValue(key, out var node))
                {
                    m_List.Remove(node);
                    m_Map.Remove(key);
                }
            }
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                m_Map.Clear();
                m_List.Clear();
            }
        }

        public bool Contains(TKey key)
        {
            if (key == null)
                return false;

            lock (m_Lock)
            {
                return m_Map.ContainsKey(key);
            }
        }

        public int Count
        {
            get
            {
                lock (m_Lock)
                {
                    return m_Map.Count;
                }
            }
        }

        public int Capacity => m_Capacity;
    }
}
