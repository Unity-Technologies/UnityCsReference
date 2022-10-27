// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    static class ListExtensions
    {
        /// <summary>
        /// Reorders some elements in a list following <see cref="ReorderType"/>.
        /// </summary>
        /// <param name="list">The list that will get reordered.</param>
        /// <param name="elements">The elements to move.</param>
        /// <param name="reorderType">The way to move elements</param>
        /// <typeparam name="T">The type of elements to move.</typeparam>
        public static void ReorderElements<T>(this List<T> list, IReadOnlyList<T> elements, ReorderType reorderType)
        {
            if (elements == null || elements.Count == 0 || list.Count <= 1)
                return;

            bool increaseIndices = reorderType == ReorderType.MoveDown || reorderType == ReorderType.MoveLast;
            bool moveAllTheWay = reorderType == ReorderType.MoveLast || reorderType == ReorderType.MoveFirst;

            var nextEndIdx = increaseIndices ? list.Count - 1 : 0;

            for (var j = 1; j < list.Count; j++)
            {
                int i = increaseIndices ? list.Count - 1 - j : j;
                if (!elements.Contains(list[i]))
                    continue;

                var moveToIdx = increaseIndices ? i + 1 : i - 1;
                if (moveAllTheWay)
                {
                    while (elements.Contains(list[nextEndIdx]) && nextEndIdx != i)
                        nextEndIdx += increaseIndices ? -1 : 1;

                    if (nextEndIdx == i)
                        continue;

                    moveToIdx = nextEndIdx;
                }
                else
                {
                    if (elements.Contains(list[moveToIdx]))
                        continue;
                }

                var element = list[i];
                list.RemoveAt(i);
                list.Insert(moveToIdx, element);
            }
        }

        /// <summary>
        /// Compare the content of two lists and return true if they are the same.
        /// </summary>
        /// <param name="a">The first list to compare.</param>
        /// <param name="b">The second list to compare.</param>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <returns>True if the two lists are the same (same elements in the same order).</returns>
        public static bool ListEquals<T>(this IReadOnlyList<T> a, IReadOnlyList<T> b) where T : IEquatable<T>
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
