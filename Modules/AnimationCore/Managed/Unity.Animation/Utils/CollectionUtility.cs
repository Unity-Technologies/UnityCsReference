// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    [GenerateTestsForBurstCompatibility]
    internal static class CollectionUtility
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(NativeList<int>) })]
        public static void Add<T, TNativeList>(ref TNativeList list, T element)
            where T : unmanaged
            where TNativeList : INativeList<T>
        {
            var index = list.Length;
            list.Length = list.Length + 1;
            list[index] = element;
        }

        [BurstDiscard]
        public static T[] ToManagedArray<T>(this UnsafeList<T> unsafeList) where T : unmanaged
        {
            if (unsafeList.Length == 0)
                return Array.Empty<T>();

            var array = new T[unsafeList.Length];
            unsafe
            {
                fixed (T* dest = &array[0])
                {
                    UnsafeUtility.MemCpy(dest, unsafeList.Ptr, unsafeList.Length * UnsafeUtility.SizeOf<T>());
                }
            }

            return array;
        }

        [BurstDiscard]
        public static T[] ToManagedArray<T>(this UnsafeHashSet<T> unsafeHashSet) where T : unmanaged, IEquatable<T>
        {
            if (unsafeHashSet.Count == 0)
                return Array.Empty<T>();

            var array = new T[unsafeHashSet.Count];

            var index = 0;
            foreach (var element in unsafeHashSet)
            {
                array[index++] = element;
            }

            return array;
        }
    }
}
