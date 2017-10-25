using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityEngine.CSSLayout
{
    internal class LockDictionary<TKey, TValue>
    {
        object _cacheLock = new object();
        Dictionary<TKey, TValue> _cacheItemDictionary = new Dictionary<TKey, TValue>();

        public void Set(TKey key, TValue value)
        {
            lock (_cacheLock)
            {
                _cacheItemDictionary[key] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue cacheItem)
        {
            bool found;

            lock (_cacheLock)
            {
                found = _cacheItemDictionary.TryGetValue(key, out cacheItem);
            }

            if (!found)
            {
                cacheItem = default(TValue);
            }

            return found;
        }

        public bool ContainsKey(TKey key)
        {
            bool found = false;

            lock (_cacheLock)
            {
                found = _cacheItemDictionary.ContainsKey(key);
            }
            
            return found;
        }

        public void Remove(TKey key)
        {
            lock (_cacheLock)
            {
                _cacheItemDictionary.Remove(key);
            }
        }
    }
}
