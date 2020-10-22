// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Pool
{
    /// <summary>
    /// A Pooled object wraps a reference to an instance that will be returned to the pool when the Pooled object is disposed.
    /// The purpose is to automate the return of references so that they do not need to be returned manually.
    /// A PooledObject can be used like so:
    /// <code>
    /// MyClass myInstance;
    /// using(myPool.Get(out myInstance)) // When leaving the scope myInstance will be returned to the pool.
    /// {
    ///     // Do something with myInstance
    /// }
    /// </code>
    /// </summary>
    public struct PooledObject<T> : IDisposable where T : class
    {
        readonly T m_ToReturn;
        readonly IObjectPool<T> m_Pool;

        internal PooledObject(T value, IObjectPool<T> pool)
        {
            m_ToReturn = value;
            m_Pool = pool;
        }

        void IDisposable.Dispose() => m_Pool.Release(m_ToReturn);
    }
}
