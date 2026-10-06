// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements.UIR
{
    // Optimizes for the case where there are many children, but few are dirty and we try to skip the others.
    class ChildrenDirtyTracker
    {
        [NoAutoStaticsCleanup] // stateless sort comparator; no captured state
        static readonly Comparison<RenderData> k_CompareSiblingKeys = (a, b) => a.siblingKey.CompareTo(b.siblingKey);

        // We don't need to add the tracker until this amount of children is reached.
        // There's no going back once a tracker has been added.
        public const int k_MinTrackedChildCount = 64;

        // We don't use the tracker when too many children are dirty. We'll just iterate over all children.
        const int k_SaturationDivisor = 16;
        // Entries that arrived in order need no sort, so the tracker stays worth using for a larger share.
        const int k_OrderedSaturationDivisor = 4;

        // Sibling keys are spaced out so that an insertion between two elements can usually pick a value
        // between their keys without touching anything else. The gap only runs out after repeated
        // insertions at the same spot, and appending never consumes one.
        const int k_SiblingKeyStride = 1 << 10;

        readonly List<RenderData> m_Entries = new(); // Dirty children (null <=> removed)
        bool m_Sorted = true;
        bool m_HasNullEntries;

        public static void OnChildLinked(RenderData parent, RenderData child, RenderData prev, RenderData next)
        {
            ++parent.childCount;

            if (parent.hasChildrenTracker)
            {
                if ((parent.flags & RenderDataFlags.ChildrenKeysStale) == 0)
                    AssignSiblingKey(child, prev, next, parent);

                if (child.isOnDirtyPath)
                {
                    // Safety net: a child linked while dirty must be tracked, since OnChildUnlinked dropped it. The only
                    // such path today, a z-index move (RenderTreeManager.UIEOnZIndexChanged), also re-dirties it after.
                    AddDirtyChild(parent, child);
                }
            }
            else if (parent.childCount == k_MinTrackedChildCount)
                StartTracking(parent);
        }

        public static void OnChildUnlinked(RenderData parent, RenderData child)
        {
            Debug.Assert(parent.childCount > 0);
            --parent.childCount;

            if ((child.flags & RenderDataFlags.TrackedByParent) == 0)
                return;

            GetChildrenTracker(parent).RemoveEntry(child);
            child.flags &= ~RenderDataFlags.TrackedByParent;
        }

        // True when the parent tracks its dirty children and this one is not tracked yet.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NeedsTracking(RenderData parent, RenderData child)
        {
            return (child.flags & RenderDataFlags.TrackedByParent) == 0 && IsTracking(parent); // flag tests only: fetching the tracker is a dictionary lookup
        }

        // Adds the child to its parent's tracker. A tracker that gets too full gives up until the subtree comes clean.
        public static void AddDirtyChild(RenderData parent, RenderData child)
        {
            Debug.Assert((child.flags & RenderDataFlags.TrackedByParent) == 0);
            if (!IsTracking(parent))
                return;

            ChildrenDirtyTracker tracker = GetChildrenTracker(parent);
            tracker.Add(child, parent);
            child.flags |= RenderDataFlags.TrackedByParent;

            int divisor = tracker.m_Sorted ? k_OrderedSaturationDivisor : k_SaturationDivisor;
            if (tracker.m_Entries.Count * divisor > parent.childCount)
            {
                // The entries are not dropped here: a walk may be reading them, and dropping them means clearing each
                // one's flag. The next walk goes over the child list, and SiblingWalk does both without an extra pass.
                parent.flags |= RenderDataFlags.ChildrenTrackerSaturated;
            }
        }

        // False when the parent does not track its dirty children: the walk then goes over the child list, with a
        // SiblingWalk.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryIterateTracked(RenderData parent, out Iterator iterator)
        {
            if (!IsTracking(parent))
            {
                iterator = default;
                return false;
            }

            ChildrenDirtyTracker tracker = GetChildrenTracker(parent);
            if (tracker.m_HasNullEntries)
                tracker.RemoveNullEntries();
            tracker.EnsureSorted(parent);
            iterator = new Iterator(parent, tracker);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SiblingWalk BeginSiblingWalk(RenderData parent)
        {
            bool untrack = false;
            if (parent.hasChildrenTracker)
            {
                // Only a saturated parent walks its child list, and it has no use for its entries until its subtree
                // comes clean. The list is emptied without reading them; the walk clears their flag as it passes them.
                ChildrenDirtyTracker tracker = GetChildrenTracker(parent);
                if (tracker.m_Entries.Count > 0)
                {
                    tracker.m_Entries.Clear();
                    tracker.m_Sorted = true;
                    tracker.m_HasNullEntries = false;
                    untrack = true;
                }
            }

            return new SiblingWalk(parent, (parent.flags & RenderDataFlags.ChildrenKeysStale) != 0, untrack);
        }

        // Goes along a walk over the child list, and does on the way what would otherwise need its own pass over the
        // children. Stale keys are relabelled: a parent that keeps saturating never reaches the tracked walk that would
        // otherwise relabel them, and Add would keep reading its entries as out of order.
        public struct SiblingWalk
        {
            readonly RenderData m_Parent;
            readonly bool m_Relabel;
            readonly bool m_Untrack;
            int m_Key;

            public SiblingWalk(RenderData parent, bool relabel, bool untrack)
            {
                m_Parent = parent;
                m_Relabel = relabel;
                m_Untrack = untrack;
                m_Key = 0;
            }

            // Called on every child, before it is processed.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Visit(RenderData child)
            {
                if (m_Relabel)
                {
                    child.siblingKey = m_Key;
                    m_Key += k_SiblingKeyStride;
                }

                if (m_Untrack)
                    child.flags &= ~RenderDataFlags.TrackedByParent;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void End()
            {
                if (m_Relabel)
                    m_Parent.flags &= ~RenderDataFlags.ChildrenKeysStale;

                if (m_Parent.hasChildrenTracker && (m_Parent.flags & RenderDataFlags.SubtreeDirtyAll) == 0)
                {
                    // Nothing under this parent is dirty any more, for any class, so it can track again from the next
                    // batch of changes. Its entries were dropped when the walk began, and a saturated parent takes none.
                    Debug.Assert(GetChildrenTracker(m_Parent).m_Entries.Count == 0);
                    m_Parent.flags &= ~RenderDataFlags.ChildrenTrackerSaturated;
                }
            }
        }

        public static void OnExtraDataFreed(RenderData parent, ExtraRenderData extraData)
        {
            // Drop the tracker as it might not be relevant if the extraData is reused
            ChildrenDirtyTracker tracker = extraData.childrenTracker;
            if (tracker != null)
            {
                // The entries outlive the tracker, so each one has to be told it is no longer tracked.
                List<RenderData> entries = tracker.m_Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null)
                        entries[i].flags &= ~RenderDataFlags.TrackedByParent;
                }
                extraData.childrenTracker = null;
            }

            parent.flags &= ~(RenderDataFlags.HasChildrenTracker | RenderDataFlags.ChildrenTrackerSaturated | RenderDataFlags.ChildrenKeysStale);
        }

        // Nulled rather than removed: removing from the middle shifts everything behind it, which a parent
        // losing many tracked children would pay once per child. Removals happen outside of the walks, and
        // the next walk compacts the list first.
        void RemoveEntry(RenderData child)
        {
            int index = m_Entries.IndexOf(child);
            Debug.Assert(index >= 0);

            int last = m_Entries.Count - 1;
            if (index < last)
            {
                m_Entries[index] = null;
                m_HasNullEntries = true;
                return;
            }

            // The list never ends with a null, which Add relies on to compare with the last entry.
            m_Entries.RemoveAt(last);
            while (m_Entries.Count > 0 && m_Entries[^1] == null)
                m_Entries.RemoveAt(m_Entries.Count - 1);
        }

        void RemoveNullEntries()
        {
            int write = 0;
            for (int read = 0; read < m_Entries.Count; ++read)
            {
                RenderData child = m_Entries[read];
                if (child != null)
                    m_Entries[write++] = child;
            }

            m_Entries.RemoveRange(write, m_Entries.Count - write);
            m_HasNullEntries = false;
        }

        // Entries added during the walk sit after the walked range, and close the gap left by the dropped ones.
        void CompactAfterWalk(int write, int walkedCount)
        {
            for (int read = walkedCount; read < m_Entries.Count; ++read)
                m_Entries[write++] = m_Entries[read];
            m_Entries.RemoveRange(write, m_Entries.Count - write);

            if (m_Entries.Count <= 1)   // compaction keeps the order, so only one entry or none can have become sorted
                m_Sorted = true;
        }

        // Visits the tracked children in sibling order, and drops each one that comes out of it clean.
        public struct Iterator
        {
            readonly RenderData m_Parent;
            readonly ChildrenDirtyTracker m_Tracker;

            // Read once: nothing is dirtied behind a walk for its own class, so the entries added during it are
            // for classes still to come, and are left to their walks.
            readonly int m_WalkedCount;
            int m_Read;
            int m_Write;
            RenderData m_Current;

            public RenderData current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Current;
            }

            public Iterator(RenderData parent, ChildrenDirtyTracker tracker)
            {
                m_Parent = parent;
                m_Tracker = tracker;
                m_WalkedCount = tracker.m_Entries.Count;
                m_Read = 0;
                m_Write = 0;
                m_Current = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // The entry returned last time has been processed. It is judged now rather than after the walk,
                // while it is still in the cache.
                if (m_Current != null)
                {
                    if (m_Current.isOnDirtyPath)
                    {
                        // Still dirty for a class to come: it stays tracked, or that class's walk cannot reach it.
                        m_Tracker.m_Entries[m_Write++] = m_Current;
                    }
                    else
                        m_Current.flags &= ~RenderDataFlags.TrackedByParent;
                }

                if (m_Read == m_WalkedCount)
                {
                    m_Current = null;
                    return false;
                }

                m_Current = m_Tracker.m_Entries[m_Read++];
                return true;
            }

            public void End()
            {
                Debug.Assert(m_Current == null && m_Read == m_WalkedCount);
                m_Tracker.CompactAfterWalk(m_Write, m_WalkedCount);

                if ((m_Parent.flags & RenderDataFlags.SubtreeDirtyAll) == 0)
                {
                    // Nothing leads into this subtree any more, so a parent that gave up on tracking can start
                    // again from the next batch of changes.
                    Debug.Assert(m_Tracker.m_Entries.Count == 0);
                    m_Parent.flags &= ~RenderDataFlags.ChildrenTrackerSaturated;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsTracking(RenderData parent)
        {
            return (parent.flags & (RenderDataFlags.HasChildrenTracker | RenderDataFlags.ChildrenTrackerSaturated)) == RenderDataFlags.HasChildrenTracker;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ChildrenDirtyTracker GetChildrenTracker(RenderData parent)
        {
            Debug.Assert(parent.hasChildrenTracker);
            return parent.renderTree.renderTreeManager.GetExtraData(parent).childrenTracker;
        }

        // Dirty children were not tracked yet, so the tracker starts saturated and children have stale keys.
        // The stale keys will be fixed by the first sibling walk.
        static void StartTracking(RenderData parent)
        {
            ExtraRenderData extraData = parent.renderTree.renderTreeManager.GetOrAddExtraData(parent);
            Debug.Assert(extraData.childrenTracker == null);
            extraData.childrenTracker = new ChildrenDirtyTracker();
            parent.flags |= RenderDataFlags.HasChildrenTracker | RenderDataFlags.ChildrenTrackerSaturated | RenderDataFlags.ChildrenKeysStale;
        }

        static void AssignSiblingKey(RenderData node, RenderData prev, RenderData next, RenderData parent)
        {
            if (prev == null)
            {
                if (next == null)
                    node.siblingKey = 0;
                else if (next.siblingKey >= int.MinValue + k_SiblingKeyStride)
                    node.siblingKey = next.siblingKey - k_SiblingKeyStride;
                else
                    parent.flags |= RenderDataFlags.ChildrenKeysStale;
                return;
            }

            if (next == null)
            {
                if (prev.siblingKey <= int.MaxValue - k_SiblingKeyStride)
                    node.siblingKey = prev.siblingKey + k_SiblingKeyStride;
                else
                    parent.flags |= RenderDataFlags.ChildrenKeysStale;
                return;
            }

            // Widened to avoid overflowing on a pair of keys at opposite ends of the range.
            long gap = (long)next.siblingKey - prev.siblingKey;
            if (gap >= 2)
                node.siblingKey = (int)(prev.siblingKey + gap / 2);
            else
                parent.flags |= RenderDataFlags.ChildrenKeysStale;
        }

        // Regenerates the labels. SiblingWalk can also relabel.
        static void RelabelChildren(RenderData parent)
        {
            int key = 0;
            for (RenderData child = parent.firstChild; child != null; child = child.nextSibling)
            {
                child.siblingKey = key;
                key += k_SiblingKeyStride;
            }
            parent.flags &= ~RenderDataFlags.ChildrenKeysStale;
        }

        // Appended to avoid reorder on each call. The order is restored in one sorting pass when a walk asks for it.
        void Add(RenderData child, RenderData parent)
        {
            // Elements are often dirtied in tree order, which appends in key order and leaves nothing to
            // sort. Only an entry that lands behind the last one costs a sort later.
            if ((parent.flags & RenderDataFlags.ChildrenKeysStale) != 0 ||
                (m_Entries.Count > 0 && child.siblingKey < m_Entries[^1].siblingKey))
                m_Sorted = false;

            m_Entries.Add(child);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureSorted(RenderData parent)
        {
            if ((parent.flags & RenderDataFlags.ChildrenKeysStale) != 0)
            {
                RelabelChildren(parent);
                m_Sorted = false;
            }

            if (m_Sorted)
                return;

            Debug.Assert(!m_HasNullEntries);
            m_Entries.Sort(k_CompareSiblingKeys); // This is the only overload that doesn't allocate
            m_Sorted = true;
        }
    }
}
