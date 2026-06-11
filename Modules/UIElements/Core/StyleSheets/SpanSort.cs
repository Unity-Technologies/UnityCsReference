// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IL2CPP.CompilerServices;

namespace UnityEngine.UIElements.StyleSheets
{
    // Comparer that takes both arguments by reference. Used to sort spans of large value types
    // without copying each operand at every comparison.
    internal delegate int RefComparison<T>(ref T x, ref T y);

    // In-place sort of a Span<T> using a ref-taking comparison. Built for the Style subsystem,
    // where the elements (e.g. SelectorRangeDescriptor) are large enough that the per-comparison
    // copy of System.Comparison<T> shows up. Quicksort with median-of-three pivot, insertion sort
    // for small partitions; not stable.
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    internal static class SpanSort
    {
        const int k_InsertionSortThreshold = 16;

        public static void Sort<T>(Span<T> span, RefComparison<T> comparison)
        {
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));
            if (span.Length < 2) return;
            QuickSort(span, 0, span.Length - 1, comparison);
        }

        static void QuickSort<T>(Span<T> span, int lo, int hi, RefComparison<T> comparison)
        {
            // Iterative tail-recursion on the larger partition keeps stack depth O(log n).
            while (hi - lo + 1 > k_InsertionSortThreshold)
            {
                int p = Partition(span, lo, hi, comparison);
                if (p - lo < hi - p)
                {
                    QuickSort(span, lo, p - 1, comparison);
                    lo = p + 1;
                }
                else
                {
                    QuickSort(span, p + 1, hi, comparison);
                    hi = p - 1;
                }
            }
            InsertionSort(span, lo, hi, comparison);
        }

        static int Partition<T>(Span<T> span, int lo, int hi, RefComparison<T> comparison)
        {
            // Median-of-three: order span[lo], span[mid], span[hi] then park the pivot at hi-1.
            int mid = lo + ((hi - lo) >> 1);
            if (comparison(ref span[mid], ref span[lo]) < 0) Swap(span, lo, mid);
            if (comparison(ref span[hi],  ref span[lo]) < 0) Swap(span, lo, hi);
            if (comparison(ref span[hi],  ref span[mid]) < 0) Swap(span, mid, hi);

            Swap(span, mid, hi - 1);
            int pivot = hi - 1;

            int i = lo;
            int j = hi - 1;
            while (true)
            {
                while (comparison(ref span[++i], ref span[pivot]) < 0) { }
                while (comparison(ref span[pivot], ref span[--j]) < 0) { }
                if (i >= j) break;
                Swap(span, i, j);
            }
            Swap(span, i, hi - 1);
            return i;
        }

        static void InsertionSort<T>(Span<T> span, int lo, int hi, RefComparison<T> comparison)
        {
            // Shift-based insertion (one copy per displaced element) instead of swap-based
            // (three) — matters for large T like the 56B descriptors this is built for.
            for (int i = lo + 1; i <= hi; i++)
            {
                T tmp = span[i];
                int j = i;
                while (j > lo && comparison(ref tmp, ref span[j - 1]) < 0)
                {
                    span[j] = span[j - 1];
                    j--;
                }
                span[j] = tmp;
            }
        }

        static void Swap<T>(Span<T> span, int a, int b)
        {
            T tmp = span[a];
            span[a] = span[b];
            span[b] = tmp;
        }
    }
}
