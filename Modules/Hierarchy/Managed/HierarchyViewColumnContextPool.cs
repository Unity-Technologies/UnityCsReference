// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Pool made to store pooled objects per context Id. This pool also gives access to the list of active objects.
    /// </summary>
    /// <typeparam name="TPooledObject">Type of the object to pool</typeparam>
    [VisibleToOtherModules("UnityEditor.HierarchyModule")]
    internal sealed class HierarchyViewColumnContextPool<TPooledObject> where TPooledObject : class
    {
        class ContextPoolImplementation
        {
            public UnityEngine.Pool.ObjectPool<TPooledObject> Pool { get; private set; }
            public HashSet<TPooledObject> Active { get; private set; } = new();

            public ContextPoolImplementation(Func<TPooledObject> creator)
            {
                Pool = new(creator);
            }
        }

        readonly Func<TPooledObject> m_ObjectCreator;
        readonly Dictionary<int, ContextPoolImplementation> m_Pools = new();

        /// <summary>
        /// Create a new ContextPool
        /// </summary>
        /// <param name="objectCreator">Creator function to generate new Pooled object.</param>
        public HierarchyViewColumnContextPool(Func<TPooledObject> objectCreator)
        {
            m_ObjectCreator = objectCreator;
        }

        /// <summary>
        /// Get a reusable Pool object for a specific Cell and bind the editor to the Cell.
        /// </summary>
        /// <param name="contextId">Context id of the pool.</param>
        /// <returns>Returns a Pooled object</returns>
        public TPooledObject Get(int contextId)
        {
            var pool = GetPoolForContext(contextId);
            var obj = pool.Pool.Get();
            pool.Active.Add(obj);
            return obj;
        }

        /// <summary>
        /// Release an object belonging to a specific context id.
        /// </summary>
        /// <param name="contextId">Context id of the pool.</param>
        /// <param name="obj">Pooled Object to release.</param>
        public void Release(int contextId, TPooledObject obj)
        {
            var pool = GetPoolForContext(contextId);
            pool.Pool.Release(obj);
            pool.Active.Remove(obj);
        }

        /// <summary>
        /// Get all the active pooled objects for a specific context id.
        /// </summary>
        /// <param name="contextId">Context id.</param>
        /// <returns>Returns an enumerable of all active pooled objects managed by the pool.</returns>
        public IReadOnlyCollection<TPooledObject> GetActiveObjects(int contextId)
        {
            if (m_Pools.TryGetValue(contextId, out var pool))
            {
                return pool.Active;
            }
            return Array.Empty<TPooledObject>();
        }

        /// <summary>
        /// Clear the Pool for a specific context id. This will clear all Pooled objects corresponding to this context.
        /// This is meant to be called when the context is being Disposed.
        /// </summary>
        /// <param name="contextId">Context id.</param>
        /// <returns>Returns an enumerable of all active cells managed by the pool.</returns>
        public void Clear(int contextId)
        {
            if (m_Pools.TryGetValue(contextId, out var pool))
            {
                pool.Pool.Dispose();
                pool.Active.Clear();
                m_Pools.Remove(contextId);
            }
        }

        internal bool Exists(int contextId)
        {
            return m_Pools.ContainsKey(contextId);
        }

        ContextPoolImplementation GetPoolForContext(int contextId)
        {
            if (!m_Pools.TryGetValue(contextId, out var pool))
            {
                pool = new ContextPoolImplementation(m_ObjectCreator);
                m_Pools[contextId] = pool;
            }
            return pool;
        }
    }
}
