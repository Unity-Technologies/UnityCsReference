// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Pool
{
    /// <summary>
    /// Provides a static implementation of <see cref="ObjectPool{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pool.</typeparam>
    public class GenericPool<T>
        where T : class, new()
    {
        // Object pool to avoid allocations.
        internal static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(() => new T(), null, null);

        /// <summary>
        /// Returns an object from the pool.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public static T Get() => s_Pool.Get();

        /// <summary>
        /// Get a new PooledObject which can be used to automatically return the pooled object when disposed.
        /// </summary>
        /// <param name="value">Output typed object.</param>
        /// <returns>A new PooledObject.</returns>
        public static PooledObject<T> Get(out T value) => s_Pool.Get(out value);

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="toRelease">Object to release.</param>
        public static void Release(T toRelease) => s_Pool.Release(toRelease);
    }
}
