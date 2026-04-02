// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Timeline.Foundation.Common
{
    static class Algorithm
    {
        /// <summary>
        /// Sorts in ascending order. The order of equivalent elements is preserved.
        /// </summary>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparison">The implementation to use when comparing elements.</param>
        public static void StableSort<T>(this List<T> list, IComparer<T> comparison)
        {
            StableSort(list, 0, list.Count, comparison);
        }

        /// <summary>
        /// Sorts in ascending order. The order of equivalent elements is preserved.
        /// </summary>
        /// <param name="list">The list to sort.</param>
        public static void StableSort<T>(this List<T> list) where T : IComparable<T>
        {
            StableSort(list, 0, list.Count, Comparer<T>.Default);
        }

        /// <summary>
        /// Swaps indices <paramref name="index1"/> and <paramref name="index2"/> in <paramref name="list"/>
        /// </summary>
        public static void Swap<T>(this List<T> list, int index1, int index2)
        {
            (list[index1], list[index2]) = (list[index2], list[index1]);
        }

        /// <summary>
        /// Sorts range (<paramref name="start"/>, <paramref name="end"/>) in ascending order.
        /// </summary>
        /// <remarks> https://en.cppreference.com/w/cpp/algorithm/stable_sort </remarks>
        static void StableSort<T>(this List<T> list, int start, int end, IComparer<T> comparison)
        {
            if (end - start < 16)
            {
                list.InsertionSort(start, end, comparison);
                return;
            }

            int middle = start + (end - start) / 2;
            list.StableSort(start, middle, comparison);
            list.StableSort(middle, end, comparison);
            list.MergeInPlace(start, middle, end, comparison);
        }

        static void InsertionSort<T>(this List<T> list, int start, int end, IComparer<T> comparison)
        {
            for (int i = start; i < end; i++)
            {
                int j = i;
                while (j > start && comparison.Compare(list[j - 1], list[j]) > 0)
                {
                    list.Swap(j, j - 1);
                    j--;
                }
            }
        }

        /// <summary>
        /// Merges two sorted ranges (<paramref name="start"/>, <paramref name="middle"/>[exclusive] )
        /// and (<paramref name="middle"/>, <paramref name="end"/>) into one sorted range.
        /// </summary>
        /// <remarks>https://en.cppreference.com/w/cpp/algorithm/inplace_merge</remarks>
        static void MergeInPlace<T>(this List<T> list, int start, int middle, int end, IComparer<T> comparison)
        {
            if (start >= middle || middle >= end)
                return;

            if (end - start == 2)
            {
                if (comparison.Compare(list[middle], list[start]) < 0)
                    list.Swap(middle, start);
                return;
            }

            int firstCut = start;
            int secondCut = middle;

            if (middle - start > end - middle)
            {
                firstCut += (middle - start) / 2;
                secondCut = list.Bound(BoundType.Lower, middle, end, list[firstCut], comparison);
            }
            else
            {
                secondCut += (end - middle) / 2;
                firstCut = list.Bound(BoundType.Upper, start, middle, list[secondCut], comparison);
            }

            middle = Rotate(list, firstCut, middle, secondCut);
            MergeInPlace(list, start, firstCut, middle, comparison);
            MergeInPlace(list, middle, secondCut, end, comparison);
        }

        /// <summary>
        /// Rotates <paramref name="list"/> range (<paramref name="start"/>, <paramref name="end"/>) to the left
        /// by (<paramref name="middle"/> - <paramref name="start"/>) positions
        /// </summary>
        /// <remarks> https://en.cppreference.com/w/cpp/algorithm/rotate </remarks>
        static int Rotate<T>(this List<T> list, int start, int middle, int end)
        {
            if (start == middle) return end;
            if (middle == end) return start;

            int write = start;
            int nextRead = start;

            while (middle != end) //rotate until end of range
            {
                if (write == nextRead)
                    nextRead = middle;
                list.Swap(middle++, write++);
            }

            Rotate(list, write, nextRead, end); //rotate remaining sequence
            return write;
        }

        enum BoundType
        {
            Upper,
            Lower
        }

        /// <summary>
        /// Returns an index to the first element in the range that is
        /// less than <paramref name="value"/> (if using <see cref="BoundType.Lower"/>)
        /// or greater or equal to <paramref name="value"/> (if using <see cref="BoundType.Upper"/>)
        /// </summary>
        /// <remarks>
        /// https://en.cppreference.com/w/cpp/algorithm/lower_bound
        /// https://en.cppreference.com/w/cpp/algorithm/upper_bound
        /// </remarks>
        static int Bound<T>(this List<T> list, BoundType bound, int start, int end, T value, IComparer<T> comparison)
        {
            int count = end - start;
            while (count > 0)
            {
                int current = start;
                int step = count / 2;
                current += step;
                T element = list[current];
                if ((bound == BoundType.Upper && comparison.Compare(value, element) >= 0) ||
                    (bound == BoundType.Lower && comparison.Compare(element, value) < 0))
                {
                    start = ++current;
                    count -= step + 1;
                }
                else
                    count = step;
            }
            return start;
        }
    }
}
