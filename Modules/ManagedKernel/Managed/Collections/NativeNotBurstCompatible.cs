// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define USE_NOT_BURST_COMPATIBLE_EXTENSIONS

using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections.NotBurstCompatible
{
    /// <summary>
    /// Provides some extension methods for various collections.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns a new managed array with all the elements copied from a set.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="set">The set whose elements are copied to the array.</param>
        /// <returns>A new managed array with all the elements copied from a set.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array")]
        public static T[] ToArray<T>(this NativeHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            var array = set.ToNativeArray(Allocator.TempJob);
            var managed = array.ToArray();
            array.Dispose();
            return managed;
        }

        /// <summary>
        /// Returns a new managed array with all the elements copied from a set.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="set">The set whose elements are copied to the array.</param>
        /// <returns>A new managed array with all the elements copied from a set.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array")]
        public static T[] ToArray<T>(this NativeParallelHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            var array = set.ToNativeArray(Allocator.TempJob);
            var managed = array.ToArray();
            array.Dispose();
            return managed;
        }

        /// <summary>
        /// Returns a new managed array which is a copy of this list.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="list">The list to copy.</param>
        /// <returns>A new managed array which is a copy of this list.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed array")]
        public static T[] ToArrayNBC<T>(this NativeList<T> list)
            where T : unmanaged
        {
            return list.AsArray().ToArray();
        }

        /// <summary>
        /// Clears this list and then copies all the elements of an array to this list.
        /// </summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="list">This list.</param>
        /// <param name="array">The managed array to copy from.</param>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public static void CopyFromNBC<T>(this NativeList<T> list, T[] array)
            where T : unmanaged
        {
            list.Clear();
            list.Resize(array.Length, NativeArrayOptions.UninitializedMemory);
            NativeArray<T> na = list.AsArray();
            na.CopyFrom(array);
        }
    }
}
