// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Pool
{
    /// <summary>
    /// Generic pool without collection checks.
    /// This class is an alternative for the <see cref="GenericPool{T}"/> for object that allocate memory when they are being compared.
    /// It is the case for the CullingResult class from Unity, and because of this in HDRP HDCullingResults generates garbage whenever we use ==, .Equals or ReferenceEquals.
    /// This pool doesn't do any of these comparison because we don't check if the stack already contains the element before releasing it.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pool.</typeparam>
    public static class UnsafeGenericPool<T>
        where T : class, new()
    {
        // Object pool to avoid allocations.
        internal static readonly ObjectPool<T> s_Pool = new ObjectPool<T>(() => new T(), null, null, null, false);

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
