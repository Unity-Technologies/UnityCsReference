// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Burst
{
    /// <summary>
    /// A structure that allows to share mutable static data between C# and HPC#.
    /// </summary>
    /// <typeparam name="T">Type of the data to share (must not contain any reference types)</typeparam>
    [VisibleToOtherModules]
    public readonly unsafe struct SharedStatic<T> where T : struct
    {
        private readonly void* _buffer;

        private SharedStatic(void* buffer)
        {
            _buffer = buffer;
            CheckIf_T_IsUnmanagedOrThrow(); // We will remove this once we have full support for unmanaged constraints with C# 8.0
        }

        /// <summary>
        /// Get a writable reference to the shared data.
        /// </summary>
        public ref T Data
        {
            get
            {
                return ref UnsafeUtilityInternal.AsRef<T>(_buffer);
            }
        }

        private static uint SizeOfT =>
            (uint)UnsafeUtilityInternal.SizeOf<T>();

        /// <summary>
        /// Get a direct unsafe pointer to the shared data.
        /// </summary>
        public void* UnsafeDataPointer
        {
            get { return _buffer; }
        }

        /// <summary>
        /// Creates a shared static data for the specified context (usable from both C# and HPC#)
        /// </summary>
        /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static SharedStatic<T> GetOrCreate<TContext>(uint alignment = 0)
        {
            return GetOrCreateUnsafe(
                alignment,
                BurstRuntime.GetHashCode64<TContext>(),
                0);
        }

        /// <summary>
        /// Creates a shared static data for the specified context and sub-context (usable from both C# and HPC#)
        /// </summary>
        /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
        /// <typeparam name="TSubContext">A type class that uniquely identifies this shared data within a sub-context of the primary context</typeparam>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static SharedStatic<T> GetOrCreate<TContext, TSubContext>(uint alignment = 0)
        {
            return GetOrCreateUnsafe(
                alignment,
                BurstRuntime.GetHashCode64<TContext>(),
                BurstRuntime.GetHashCode64<TSubContext>());
        }

        /// <summary>
        /// The default alignment is a user specified one is not provided.
        /// </summary>
        private const uint DefaultAlignment = 16;

        /// <summary>
        /// Creates a shared static data unsafely for the specified context and sub-context (usable from both C# and HPC#).
        /// </summary>
        /// <param name="alignment">The alignment (specified in bytes).</param>
        /// <param name="hashCode">The 64-bit hashcode for the shared-static.</param>
        /// <param name="subHashCode">The 64-bit sub-hashcode for the shared-static.</param>
        /// <returns>A newly created or previously cached shared-static for the hashcodes provided.</returns>
        public static SharedStatic<T> GetOrCreateUnsafe(uint alignment, long hashCode, long subHashCode)
        {
            return new SharedStatic<T>(SharedStatic.GetOrCreateSharedStaticInternal(
                hashCode,
                subHashCode,
                SizeOfT,
                alignment == 0 ? DefaultAlignment : alignment));
        }

        /// <summary>
        /// Creates a shared static data unsafely for the specified context and sub-context (usable from both C# and HPC#).
        /// </summary>
        /// <typeparam name="TSubContext">A type class that uniquely identifies this shared data within a sub-context of the primary context</typeparam>
        /// <param name="alignment">The alignment (specified in bytes).</param>
        /// <param name="hashCode">The 64-bit hashcode for the shared-static.</param>
        /// <returns>A newly created or previously cached shared-static for the hashcodes provided.</returns>
        public static SharedStatic<T> GetOrCreatePartiallyUnsafeWithHashCode<TSubContext>(uint alignment, long hashCode)
        {
            return new SharedStatic<T>(SharedStatic.GetOrCreateSharedStaticInternal(
                hashCode,
                BurstRuntime.GetHashCode64<TSubContext>(),
                SizeOfT,
                alignment == 0 ? DefaultAlignment : alignment));
        }

        /// <summary>
        /// Creates a shared static data unsafely for the specified context and sub-context (usable from both C# and HPC#).
        /// </summary>
        /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
        /// <param name="alignment">The alignment (specified in bytes).</param>
        /// <param name="subHashCode">The 64-bit sub-hashcode for the shared-static.</param>
        /// <returns>A newly created or previously cached shared-static for the hashcodes provided.</returns>
        public static SharedStatic<T> GetOrCreatePartiallyUnsafeWithSubHashCode<TContext>(uint alignment, long subHashCode)
        {
            return new SharedStatic<T>(SharedStatic.GetOrCreateSharedStaticInternal(
                BurstRuntime.GetHashCode64<TContext>(),
                subHashCode,
                SizeOfT,
                alignment == 0 ? DefaultAlignment : alignment));
        }

        /// <summary>
        /// Creates a shared static data for the specified context (reflection based, only usable from C#, but not from HPC#)
        /// </summary>
        /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static SharedStatic<T> GetOrCreate(Type contextType, uint alignment = 0)
        {
            return GetOrCreateUnsafe(
                alignment,
                BurstRuntime.GetHashCode64(contextType),
                0);
        }

        /// <summary>
        /// Creates a shared static data for the specified context and sub-context (usable from both C# and HPC#)
        /// </summary>
        /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
        /// <param name="subContextType">A type class that uniquely identifies this shared data within a sub-context of the primary context</param>
        /// <param name="alignment">Optional alignment</param>
        /// <returns>A shared static for the specified context</returns>
        public static SharedStatic<T> GetOrCreate(Type contextType, Type subContextType, uint alignment = 0)
        {
            return GetOrCreateUnsafe(
                alignment,
                BurstRuntime.GetHashCode64(contextType),
                BurstRuntime.GetHashCode64(subContextType));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIf_T_IsUnmanagedOrThrow()
        {
            if (!Unity.Burst.LowLevel.Unsafe.BurstUnsafeUtility.IsUnmanaged<T>())
                throw new InvalidOperationException($"The type {typeof(T)} used in SharedStatic<{typeof(T)}> must be unmanaged (contain no managed types).");
        }
    }

    internal static class SharedStatic
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSizeOf(uint sizeOf)
        {
            if (sizeOf == 0) throw new ArgumentException("sizeOf must be > 0", nameof(sizeOf));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static unsafe void CheckResult(void* result)
        {
            if (result == null)
                throw new InvalidOperationException("Unable to create a SharedStatic for this key. This is most likely due to the size of the struct inside of the SharedStatic having changed or the same key being reused for differently sized values. To fix this the editor needs to be restarted.");
        }


        /// <summary>
        /// Get or create a shared-static.
        /// </summary>
        /// <param name="getHashCode64">The 64-bit hashcode for the shared-static.</param>
        /// <param name="getSubHashCode64">The 64-bit sub-hashcode for the shared-static.</param>
        /// <param name="sizeOf">The size (in bytes) of the shared static memory region.</param>
        /// <param name="alignment">The alignment (in bytes) of the shared static memory region.</param>
        /// <returns>Either a newly created or a previously created memory region that matches the hashcodes provided.</returns>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void* GetOrCreateSharedStaticInternal(long getHashCode64, long getSubHashCode64, uint sizeOf, uint alignment)
        {
            CheckSizeOf(sizeOf);
            var result =
                Unity.Burst.LowLevel.BurstCompilerService.GetOrCreateSharedMemory(getHashCode64, getSubHashCode64, sizeOf, alignment);
            CheckResult(result);
            return result;
        }
    }
}
