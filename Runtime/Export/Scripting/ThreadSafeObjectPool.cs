// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;

namespace UnityEngine
{
    internal struct ThreadSafeObjectPool<T> where T: class
    {
        private readonly ObjectPool<T> _objectPool;

        public ThreadSafeObjectPool(Func<T> factory, Action<T> onDestroy = null)
        {
            _objectPool = new ObjectPool<T>(factory, collectionCheck: false, actionOnDestroy: onDestroy);
        }

        public T Get()
        {
            lock (_objectPool)
            {
                return _objectPool.Get();
            }
        }

        public void Release(T element)
        {
            lock (_objectPool)
            {
                _objectPool.Release(element);
            }
        }
    }
}
