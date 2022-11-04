// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
using System.Threading;

namespace UnityEngine
{
    public partial class Awaitable
    {
        internal struct ThreadSafeObjectPool<T> where T : class
        {
            private readonly ObjectPool<T> _objectPool;
            private ManagedLockWithSingleThreadBias _spinLock = default;

            public ThreadSafeObjectPool(Func<T> factory, Action<T> onDestroy = null)
            {
                _objectPool = new ObjectPool<T>(factory, collectionCheck: false, actionOnDestroy: onDestroy);
            }

            public T Get()
            {
                try
                {
                    _spinLock.Acquire();
                    return _objectPool.Get();
                }
                finally
                {
                    _spinLock.Release();
                }
            }

            public void Release(T element)
            {

                try
                {
                    _spinLock.Acquire();
                    _objectPool.Release(element);
                }
                finally
                {
                    _spinLock.Release();
                }
            }
        }
    }
}
