// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class UISystemProfilerRenderService : IDisposable
    {
        private class LRUCache
        {
            private int m_Capacity;
            private Dictionary<long, Texture2D> m_Cache;
            private List<long> m_CacheQueue;
            private int m_CacheQueueFront;

            public LRUCache(int capacity)
            {
                if (capacity <= 0)
                {
                    capacity = 16;
                }

                m_Capacity = capacity;
                m_Cache = new Dictionary<long, Texture2D>(m_Capacity);

                IEnumerable<long> initQueueEnumerable = from value in Enumerable.Repeat(-1L, m_Capacity) select value;
                m_CacheQueue = new List<long>(initQueueEnumerable);

                m_CacheQueueFront = 0;
            }

            public void Clear()
            {
                foreach (long removeKey in m_CacheQueue)
                {
                    Texture2D tex;
                    if (m_Cache.TryGetValue(removeKey, out tex))
                    {
                        ProfilerProperty.ReleaseUISystemProfilerRender(tex);
                    }
                }
                m_Cache.Clear();
                m_CacheQueue.Clear();

                IEnumerable<long> initQueueEnumerable = from value in Enumerable.Repeat(-1L, m_Capacity) select value;
                m_CacheQueue.AddRange(initQueueEnumerable);
                m_CacheQueueFront = 0;
            }

            public void Add(long key, Texture2D data)
            {
                if (Get(key) == null)
                {
                    if (m_CacheQueue[m_CacheQueueFront] != -1)
                    {
                        long removeKey = m_CacheQueue[m_CacheQueueFront];
                        Texture2D tex;
                        if (m_Cache.TryGetValue(removeKey, out tex))
                        {
                            m_Cache.Remove(removeKey);
                            ProfilerProperty.ReleaseUISystemProfilerRender(tex);
                        }
                    }

                    m_CacheQueue[m_CacheQueueFront] = key;
                    m_Cache[key] = data;

                    m_CacheQueueFront++;
                    if (m_CacheQueueFront == m_Capacity)
                        m_CacheQueueFront = 0;
                }
            }

            public Texture2D Get(long key)
            {
                Texture2D tex;
                if (m_Cache.TryGetValue(key, out tex))
                {
                    // Move key in front of the cache queue.
                    // In order to avoid moving all data, we just swap the
                    // queue front and the key.
                    m_CacheQueue[m_CacheQueue.IndexOf(key)] = m_CacheQueue[m_CacheQueueFront]; // it is ok if the key here is -1
                    m_CacheQueue[m_CacheQueueFront] = key;

                    m_CacheQueueFront++;
                    if (m_CacheQueueFront == m_Capacity)
                        m_CacheQueueFront = 0;

                    return tex;
                }
                else
                    return null;
            }
        }

        private LRUCache m_Cache;
        private bool m_Disposed;

        public UISystemProfilerRenderService()
        {
            m_Cache = new LRUCache(10);
        }

        public void Dispose()
        {
            m_Disposed = true;
            m_Cache.Clear();
        }

        private Texture2D Generate(int frameIndex, int renderDataIndex, int renderDataCount, bool overdraw)
        {
            return m_Disposed ? null : ProfilerProperty.UISystemProfilerRender(frameIndex, renderDataIndex, renderDataCount, overdraw);
        }

        public Texture2D GetThumbnail(int frameIndex, int renderDataIndex, int infoRenderDataCount, bool overdraw)
        {
            if (m_Disposed)
                return null;

            //long key = ((long)(ushort)renderDataIndex << 32) | (ushort)(((ushort)infoRenderDataCount) & 0x7FFF) | (ushort)(overdraw ? 0x8000 : 0);
            Texture2D tex = null; //m_Cache.Get(key);
            if (tex == null)
            {
                tex = Generate(frameIndex, renderDataIndex, infoRenderDataCount, overdraw);
                //if (tex != null)
                //{
                //    m_Cache.Add(key, tex);
                //}
            }

            return tex;
        }
    }
}
