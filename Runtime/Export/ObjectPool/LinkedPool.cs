// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Pool
{
    public class LinkedPool<T> : IDisposable, IObjectPool<T> where T : class
    {
        internal class LinkedPoolItem
        {
            internal LinkedPoolItem poolNext;
            internal T value;
        }

        readonly Func<T> m_CreateFunc;
        readonly Action<T> m_ActionOnGet;
        readonly Action<T> m_ActionOnRelease;
        readonly Action<T> m_ActionOnDestroy;
        readonly int m_Limit; // Used to prevent catastrophic memory retention.
        internal LinkedPoolItem m_PoolFirst; // The pool of available T objects
        internal LinkedPoolItem m_NextAvailableListItem; // When Get is called we place the node here for reuse and to prevent GC
        bool m_CollectionCheck;

        public LinkedPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, bool collectionCheck = true, int maxSize = 10000)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            if (maxSize <= 0)
                throw new ArgumentException(nameof(maxSize), "Max size must be greater than 0");

            m_CreateFunc = createFunc;
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_ActionOnDestroy = actionOnDestroy;
            m_Limit = maxSize;
            m_CollectionCheck = collectionCheck;
        }

        public int CountInactive { get; private set; }

        public T Get()
        {
            T item = null;
            if (m_PoolFirst == null)
            {
                item = m_CreateFunc();
            }
            else
            {
                var first = m_PoolFirst;
                item = first.value;
                m_PoolFirst = first.poolNext;

                // Add the empty node to our pool for reuse and to prevent GC
                first.poolNext = m_NextAvailableListItem;
                m_NextAvailableListItem = first;
                m_NextAvailableListItem.value = null;
                --CountInactive;
            }
            m_ActionOnGet?.Invoke(item);
            return item;
        }

        public PooledObject<T> Get(out T v) => new PooledObject<T>(v = Get(), this);

        public void Release(T item)
        {
            if (m_CollectionCheck)
            {
                var listItem = m_PoolFirst;
                while (listItem != null)
                {
                    if (ReferenceEquals(listItem.value, item))
                        throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
                    listItem = listItem.poolNext;
                }
            }

            m_ActionOnRelease?.Invoke(item);

            if (CountInactive < m_Limit)
            {
                LinkedPoolItem poolItem = m_NextAvailableListItem;
                if (poolItem == null)
                {
                    poolItem = new LinkedPoolItem();
                }
                else
                {
                    m_NextAvailableListItem = poolItem.poolNext;
                }

                poolItem.value = item;
                poolItem.poolNext = m_PoolFirst;
                m_PoolFirst = poolItem;
                ++CountInactive;
            }
            else
            {
                m_ActionOnDestroy?.Invoke(item);
            }
        }

        public void Clear()
        {
            if (m_ActionOnDestroy != null)
            {
                for (var itr = m_PoolFirst; itr != null; itr = itr.poolNext)
                {
                    m_ActionOnDestroy(itr.value);
                }
            }

            m_PoolFirst = null;
            m_NextAvailableListItem = null;
            CountInactive = 0;
        }

        public void Dispose()
        {
            // Ensure we do a clear so the destroy action can be called.
            Clear();
        }
    }
}
