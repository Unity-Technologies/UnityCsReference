// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Hierarchy;
using UnityEngine.Pool;

namespace UnityEditor.Search
{
    sealed class SearchItemHierarchySorting
    {
        enum UpdateMode
        {
            Update,
            UpdateIncremental,
            UpdateIncrementalTimed
        }

        enum UpdateStage
        {
            Hierarchy,
            Sorting,
            Count
        }

        readonly Hierarchy m_Hierarchy;
        readonly HierarchyCommandList m_CommandList;
        readonly SearchItemHierarchyNodeMap m_SearchItemHierarchyNodeMap;
        readonly Stack<HierarchyNodeChildren> m_ChildrenNodesStack = new();
        UpdateStage m_CurrentUpdateStage = UpdateStage.Hierarchy;
        IComparer<SearchItem> m_SearchItemComparer;
        bool m_NeedsSorting = true;

        // Update stopwatches
        readonly Stopwatch m_UpdateTimer = new();
        readonly Stopwatch m_SortingTimer = new();

        public bool UpdateNeeded => m_NeedsSorting;

        public SearchItemHierarchySorting(Hierarchy hierarchy, HierarchyCommandList commandList, SearchItemHierarchyNodeMap map)
        {
            m_Hierarchy = hierarchy;
            m_CommandList = commandList;
            m_SearchItemHierarchyNodeMap = map;
        }

        public void SetSearchItemComparer(IComparer<SearchItem> searchItemComparer)
        {
            m_SearchItemComparer = searchItemComparer ?? new SortByScoreComparer();
            Reset();
        }

        public void Reset()
        {
            m_ChildrenNodesStack.Clear();
            m_NeedsSorting = true;
            m_CurrentUpdateStage = UpdateStage.Hierarchy;
        }

        public void Update()
        {
            while (DoUpdate(UpdateMode.Update, TimeSpan.Zero)) { }
        }

        public bool UpdateIncremental()
        {
            return DoUpdate(UpdateMode.UpdateIncremental, TimeSpan.Zero);
        }

        public bool UpdateIncrementalTimed(TimeSpan timeLimit)
        {
            while (true)
            {
                m_UpdateTimer.Restart();
                if (!DoUpdate(UpdateMode.UpdateIncrementalTimed, timeLimit))
                    return false; // Update completed

                timeLimit -= m_UpdateTimer.Elapsed;
                if (timeLimit <= TimeSpan.Zero)
                    return true; // Timed out
            }
        }

        bool DoUpdate(UpdateMode updateMode, TimeSpan timeLimit)
        {
            // To effectively do the sorting, we need to ensure the hierarchy is up to date first.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateHierarchyByMode(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (mode)
                {
                    case UpdateMode.Update:
                        m_Hierarchy.Update();
                        return false;
                    case UpdateMode.UpdateIncremental:
                        return m_Hierarchy.UpdateIncremental();
                    case UpdateMode.UpdateIncrementalTimed:
                        return m_Hierarchy.UpdateIncrementalTimed(timeLimit.TotalMilliseconds);
                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateSortingByMode(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (mode)
                {
                    case UpdateMode.Update:
                        UpdateSorting();
                        return false;
                    case UpdateMode.UpdateIncremental:
                        return UpdateSortingIncremental();
                    case UpdateMode.UpdateIncrementalTimed:
                        return UpdateSortingIncrementalTimed(timeLimit);
                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool DoUpdateStage(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (m_CurrentUpdateStage)
                {
                    case UpdateStage.Hierarchy:
                        return UpdateHierarchyByMode(mode, timeLimit);
                    case UpdateStage.Sorting:
                        return UpdateSortingByMode(mode, timeLimit);
                    default:
                        throw new NotImplementedException(m_CurrentUpdateStage.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IncrementUpdateStage()
            {
                m_CurrentUpdateStage = (UpdateStage)(((int)m_CurrentUpdateStage + 1) % ((int)UpdateStage.Count));
            }

            // Execute the current stage
            var callAgain = DoUpdateStage(updateMode, timeLimit);

            // Each stage needs to be fully completed before moving to the next one.
            if (!callAgain)
                IncrementUpdateStage();

            return callAgain || UpdateNeeded;
        }

        void UpdateSorting()
        {
            while (DoSort(TimeSpan.MaxValue)) { }
        }

        bool UpdateSortingIncremental()
        {
            return DoSort(TimeSpan.Zero);
        }

        bool UpdateSortingIncrementalTimed(TimeSpan timeLimit)
        {
            return DoSort(timeLimit);
        }

        bool DoSort(TimeSpan timeLimit)
        {
            if (!m_NeedsSorting)
                return false;

            m_SortingTimer.Restart();

            if (m_ChildrenNodesStack.Count == 0)
            {
                var childrenEnumerable = m_Hierarchy.EnumerateChildren(in m_Hierarchy.Root);
                m_ChildrenNodesStack.Push(childrenEnumerable);
            }

            using var _ = ListPool<SearchItem>.Get(out var childrenList);
            while (m_ChildrenNodesStack.Count > 0)
            {
                var childrenEnumerable = m_ChildrenNodesStack.Pop();
                var childrenCount = childrenEnumerable.Count;

                childrenList.Clear();
                childrenList.Capacity = childrenCount;
                foreach (var childNode in childrenEnumerable)
                {
                    if (m_SearchItemHierarchyNodeMap.TryGetSearchItem(in childNode, out var childItem))
                        childrenList.Add(childItem);
                }

                childrenList.Sort(m_SearchItemComparer);
                for (var i = 0; i < childrenList.Count; ++i)
                {
                    if (!m_SearchItemHierarchyNodeMap.TryGetNode(childrenList[i], out var childNode))
                        continue;
                    var grandChildrenEnumerator = m_Hierarchy.EnumerateChildren(in childNode);
                    if (grandChildrenEnumerator.Count > 0)
                        m_ChildrenNodesStack.Push(grandChildrenEnumerator);
                    m_CommandList.SetSortIndex(in childNode, i);
                }

                if (m_SortingTimer.Elapsed > timeLimit)
                    break;
            }

            if (m_ChildrenNodesStack.Count == 0)
            {
                m_CommandList.SortChildrenRecursive(in m_Hierarchy.Root);
                m_NeedsSorting = false;
            }

            return m_NeedsSorting;
        }
    }
}
