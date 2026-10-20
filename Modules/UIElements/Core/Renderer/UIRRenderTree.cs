// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements.UIR
{
    // Each dirty class keeps a flat list of the elements dirty for it, processed in draw order (UUM-154188). A list is
    // sorted by pre-order label if it was not filled in order. When the labels are stale, the pass that rebuilds them also
    // rebuilds the lists in draw order.
    struct RenderTreeDirtyTracker
    {
        public RenderTree owner;

        public uint dirtyID; // A monotonically increasing ID used to avoid double processing of some elements

        // Set when an insertion could not be labelled in place: the labels are rebuilt before the next list of more than one entry is processed.
        public bool labelsStale;

        // Set by an insertion labelled in place, which may change the label of a listed element (a respread, or pooled render
        // data listed in its previous life), and by the removal of a listed element, whose render data may be reused with a
        // label from another tree before its list is processed: the order of every list is checked before it is processed.
        public bool labelsChanged;

        public List<RenderData>[] dirtyLists; // Indexed per dirty type class
        public bool[] needsSort; // Indexed per dirty type class

        // Indexed per dirty type class: elements dirtied while the labels were stale and a list entry was already there.
        // They are not listed: the relabel that has to run before the list is processed lists them in draw order.
        public int[] unlistedCounts;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderDataDirtyTypes GetDirtyTypes(RenderDataDirtyTypeClasses dirtyTypeClass)
        {
            switch (dirtyTypeClass)
            {
                case RenderDataDirtyTypeClasses.Clipping:
                    return RenderDataDirtyTypes.Clipping | RenderDataDirtyTypes.ClippingHierarchy;
                case RenderDataDirtyTypeClasses.Opacity:
                    return RenderDataDirtyTypes.Opacity | RenderDataDirtyTypes.OpacityHierarchy;
                case RenderDataDirtyTypeClasses.Color:
                    return RenderDataDirtyTypes.Color;
                case RenderDataDirtyTypeClasses.TransformSize:
                    return RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.ClipRectSize;
                case RenderDataDirtyTypeClasses.Visuals:
                    return RenderDataDirtyTypes.AllVisuals;
                default:
                    return RenderDataDirtyTypes.None;
            }
        }

        public void RegisterDirty(RenderData renderData, RenderDataDirtyTypes dirtyTypes, RenderDataDirtyTypeClasses dirtyTypeClass)
        {
            Debug.Assert(renderData.renderTree == owner);
            Debug.Assert(dirtyTypes != 0);
            Debug.Assert((dirtyTypes & ~GetDirtyTypes(dirtyTypeClass)) == 0);

            if ((renderData.dirtiedValues & GetDirtyTypes(dirtyTypeClass)) == 0) // not listed yet for this class
            {
                List<RenderData> list = dirtyLists[(int)dirtyTypeClass];
                if (list.Count > 0 && labelsStale)
                    ++unlistedCounts[(int)dirtyTypeClass];
                else
                {
                    if (list.Count > 0 && renderData.preOrderLabel < list[^1].preOrderLabel)
                        needsSort[(int)dirtyTypeClass] = true;
                    list.Add(renderData);
                }
            }

            renderData.dirtiedValues |= dirtyTypes;
        }

        public void ClearDirty(RenderData renderData, RenderDataDirtyTypes dirtyTypesInverse)
        {
            Debug.Assert(renderData.dirtiedValues != 0);
            renderData.dirtiedValues &= dirtyTypesInverse;
        }
    }

    class RenderTree
    {
        RenderTreeManager m_RenderTreeManager;
        RenderTreeDirtyTracker m_DirtyTracker;
        RenderChainCommand m_FirstCommand; // Not necessarily the root command, which may not create any commands
        RenderData m_RootRenderData;

        // Active backdrop-filters in this tree; iterated when a group transform moves to re-tessellate the
        // ones nested under it (their UVs track the world transform). Maintained like the panel count. UI-5170.
        HashSet<RenderData> m_BackdropFilterRenderDatas;

        public TextureId quadTextureId;
        public Rect quadRect;
        public Rect quadUVRect;
        public Vector2 rasterPixelsPerPoint;
        // Gamma-encoded quad (force-gamma); the parent samples it without re-encoding (UI-5094).
        public bool quadIsGammaEncoded;

        public GCHandlePool m_GCHandlePool = new();

        internal RenderTreeManager renderTreeManager => m_RenderTreeManager;
        internal RenderData rootRenderData => m_RootRenderData;

        internal RenderTree parent;
        internal RenderTree firstChild;
        internal RenderTree nextSibling;

        internal ref RenderTreeDirtyTracker dirtyTracker { get { return ref m_DirtyTracker; } }
        internal RenderChainCommand firstCommand { get { return m_FirstCommand; } }

        internal bool isRootRenderTree
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // TODO: Use new "parent" field
                return rootRenderData.owner.parent == null && !rootRenderData.isNestedRenderTreeRoot;
            }
        }

        static readonly ProfilerMarker k_MarkerClipProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateClips");
        static readonly ProfilerMarker k_MarkerOpacityProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateOpacity");
        static readonly ProfilerMarker k_MarkerColorsProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateColors");
        static readonly ProfilerMarker k_MarkerTransformProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateTransforms");
        static readonly ProfilerMarker k_MarkerVisualsProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateVisuals");

        [NoAutoStaticsCleanup] // stateless sort comparator; no captured state
        static readonly Comparison<RenderData> k_ComparePreOrder = (a, b) => a.preOrderLabel.CompareTo(b.preOrderLabel);

        // Pre-order labels are in [0, 2^k_LabelBits). A full relabel spaces them k_RelabelStride apart. An insertion takes a
        // label between its pre-order neighbours, at most k_MaxLabelInsertionStep after the previous one, so that the nodes
        // of an added subtree, which are linked one by one in pre-order, can follow each other in the same gap. When a gap
        // is used up, RespreadLabels renumbers a local range. Labelling stops for the frame once it has visited as many nodes
        // as the last full relabel (k_MinLabellingBudget at least): the labels are then left stale for one full relabel.
        internal const int k_LabelBits = 62; // the sign bit stays clear, and so does the next one: 2^k_LabelBits (end of tree) and range ends fit a long
        internal const long k_RelabelStride = 1L << 32; // gap between consecutive labels after a full relabel (2^30 nodes fit)
        const long k_MaxLabelInsertionStep = 1L << 20; // largest distance between an inserted label and the label before it
        const double k_RespreadDensityGrowth = 1.5; // a range of 2^level labels may hold up to 1.5^level nodes
        const int k_MinLabellingBudget = 64; // floor of the per-frame labelling budget, so that small trees can still label in place

        // A frame with more insertions than a sixteenth of the tree, e.g. a bulk add, is likely to run out of labelling work
        // and pay a full relabel anyway: past that many, the labels are left stale before more labelling work is wasted.
        const int k_MinLabelledInsertions = 16;
        const int k_NodesPerLabelledInsertion = 16;

        int m_LabellingWork; // Nodes visited to label insertions since the last ProcessChanges
        int m_LabelledInsertions; // Since the last ProcessChanges
        int m_LastRelabelNodeCount;

        const int k_InitialDirtyListCapacity = 16;

        public void Init(RenderTreeManager renderTreeManager, RenderData rootRenderData)
        {
            m_RenderTreeManager = renderTreeManager;
            m_RootRenderData = rootRenderData;
            m_DirtyTracker.owner = this;
            m_DirtyTracker.labelsStale = true;
            m_DirtyTracker.labelsChanged = false;
            m_LabellingWork = 0;
            m_LabelledInsertions = 0;
            m_LastRelabelNodeCount = 0;

            // Reused across pooled acquires (Reset clears them)
            if (m_DirtyTracker.dirtyLists == null)
            {
                m_DirtyTracker.dirtyLists = new List<RenderData>[(int)RenderDataDirtyTypeClasses.Count];
                for (int i = 0; i < m_DirtyTracker.dirtyLists.Length; ++i)
                    m_DirtyTracker.dirtyLists[i] = new List<RenderData>(k_InitialDirtyListCapacity);
                m_DirtyTracker.needsSort = new bool[(int)RenderDataDirtyTypeClasses.Count];
                m_DirtyTracker.unlistedCounts = new int[(int)RenderDataDirtyTypeClasses.Count];
            }

            quadTextureId = TextureId.invalid;
            rasterPixelsPerPoint = Vector2.zero;
            quadIsGammaEncoded = false;

            parent = null;
            firstChild = null;
            nextSibling = null;

            m_BackdropFilterRenderDatas ??= new HashSet<RenderData>(); // reused across pooled acquires (Reset clears it)
        }

        public void Reset()
        {
            m_RenderTreeManager = null;
            m_RootRenderData = null;
            parent = null;
            firstChild = null;
            nextSibling = null;

            m_BackdropFilterRenderDatas?.Clear();

            if (m_DirtyTracker.dirtyLists != null)
            {
                for (int i = 0; i < m_DirtyTracker.dirtyLists.Length; ++i)
                {
                    m_DirtyTracker.dirtyLists[i].Clear();
                    m_DirtyTracker.needsSort[i] = false;
                    m_DirtyTracker.unlistedCounts[i] = 0;
                }
            }
        }

        public void Dispose()
        {
            if (m_RootRenderData != null)
                DepthFirstResetTextures(m_RootRenderData);
        }

        // Iterates on render data (caller performs null check)
        void DepthFirstResetTextures(RenderData renderData)
        {
            m_GCHandlePool.ReturnAll();

            // Work
            m_RenderTreeManager.ResetGraphicEntries(renderData);
            BackdropFilterHelper.ReleaseBackdropFilterResources(m_RenderTreeManager, renderData);

            // Recurse
            RenderData child = renderData.firstChild;
            while (child != null)
            {
                DepthFirstResetTextures(child);
                child = child.nextSibling;
            }
        }

        [Flags]
        internal enum AllowedClasses
        {
            Clipping      = 1 << 0,
            Opacity       = 1 << 1,
            Color         = 1 << 2,
            TransformSize = 1 << 3,
            Visuals       = 1 << 4,
            All = Clipping | Opacity | Color | TransformSize | Visuals
        }

        // Nothing can be dirtied for a class while it is processed, which ProcessDirtyClass relies on: ProcessChanges removes
        // the class from here before processing it, and changes from outside the renderer throw (m_BlockDirtyRegistration).
        AllowedClasses m_AllowedDirtyClasses = AllowedClasses.All;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataClippingChanged(RenderData renderData, bool hierarchical)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Clipping) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Clipping | (hierarchical ? RenderDataDirtyTypes.ClippingHierarchy : 0), RenderDataDirtyTypeClasses.Clipping);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataOpacityChanged(RenderData renderData, bool hierarchical = false)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Opacity) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Opacity | (hierarchical ? RenderDataDirtyTypes.OpacityHierarchy : 0), RenderDataDirtyTypeClasses.Opacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataColorChanged(RenderData renderData)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Color) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Color, RenderDataDirtyTypeClasses.Color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataTransformOrSizeChanged(RenderData renderData, bool transformChanged, bool clipRectSizeChanged)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.TransformSize) != 0);
            RenderDataDirtyTypes flags =
                (transformChanged ? RenderDataDirtyTypes.Transform : RenderDataDirtyTypes.None) |
                (clipRectSizeChanged ? RenderDataDirtyTypes.ClipRectSize : RenderDataDirtyTypes.None);
            m_DirtyTracker.RegisterDirty(renderData, flags, RenderDataDirtyTypeClasses.TransformSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataVisualsChanged(RenderData renderData, bool hierarchical)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Visuals) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Visuals | (hierarchical ? RenderDataDirtyTypes.VisualsHierarchy : 0), RenderDataDirtyTypeClasses.Visuals);
        }

        public bool hasBackdropFilters => m_BackdropFilterRenderDatas != null && m_BackdropFilterRenderDatas.Count > 0;

        // Includes the backdrop-filters that will register when this frame's visual changes are processed
        public bool HasBackdropFiltersIncludingPending()
        {
            if (hasBackdropFilters)
                return true;
            if (m_RootRenderData == null)
                return false;

            // Older than the IDs that ProcessChanges takes next, so processing never mistakes these marks for its own
            uint searchID = ++m_DirtyTracker.dirtyID;

            int visualsIndex = (int)RenderDataDirtyTypeClasses.Visuals;
            if (m_DirtyTracker.unlistedCounts[visualsIndex] > 0)
            {
                // Some elements dirtied while the labels were stale are not listed: only a walk of the tree finds them.
                return HasPendingBackdropFilter(m_RootRenderData, false, true, searchID);
            }

            List<RenderData> dirtyList = m_DirtyTracker.dirtyLists[visualsIndex];
            RenderData[] items = NoAllocHelpers.ExtractArrayFromList(dirtyList);
            for (int i = 0; i < dirtyList.Count; ++i)
            {
                RenderData renderData = items[i];
                if (renderData.renderTree == this && HasPendingBackdropFilter(renderData, false, false, searchID))
                    return true;
            }
            return false;
        }

        static bool HasPendingBackdropFilter(RenderData renderData, bool inDirtyHierarchy, bool wholeSubtree, uint searchID)
        {
            if (renderData.dirtyID == searchID)
                return false;

            inDirtyHierarchy |= (renderData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;
            if (inDirtyHierarchy)
                renderData.dirtyID = searchID; // only a node whose whole subtree is searched may be skipped later

            bool visualsDirty = inDirtyHierarchy || (renderData.dirtiedValues & RenderDataDirtyTypes.AllVisuals) != 0;
            if (visualsDirty && !renderData.isNestedRenderTreeRoot && renderData.owner.hasBackdropFilter)
                return true;

            if (!inDirtyHierarchy && !wholeSubtree)
                return false;

            for (RenderData child = renderData.firstChild; child != null; child = child.nextSibling)
            {
                if (HasPendingBackdropFilter(child, inDirtyHierarchy, wholeSubtree, searchID))
                    return true;
            }
            return false;
        }

        public void RegisterBackdropFilter(RenderData renderData)
        {
            m_BackdropFilterRenderDatas.Add(renderData);
        }

        public void UnregisterBackdropFilter(RenderData renderData)
        {
            m_BackdropFilterRenderDatas.Remove(renderData);
        }

        // Re-record the backdrop-filters at or under this element, whose recorded rect bakes in its clip.
        // Testing each registered entry's parent chain is cheaper than walking the subtree.
        public void RefreshBackdropFilterDescendantsOf(RenderData ancestor)
        {
            if (m_BackdropFilterRenderDatas.Count == 0)
                return;

            foreach (RenderData rd in m_BackdropFilterRenderDatas)
            {
                for (RenderData p = rd; p != null; p = p.parent)
                {
                    if (p != ancestor)
                        continue;

                    // Skip if already scheduled to regenerate this pass.
                    if ((rd.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0)
                        OnRenderDataVisualsChanged(rd, false);
                    break;
                }
            }
        }

        // Re-tessellate the backdrop-filters nested under the moved group (their world-derived UVs went stale).
        // Testing each registered entry's group-ancestor chain is cheaper than walking the subtree. UI-5170.
        public void RefreshBackdropFilterDescendantsOfGroup(RenderData group)
        {
            foreach (RenderData rd in m_BackdropFilterRenderDatas)
            {
                for (RenderData g = rd.groupTransformAncestor; g != null; g = g.groupTransformAncestor)
                {
                    if (g != group)
                        continue;

                    // Skip if already scheduled to regenerate this pass.
                    if ((rd.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0)
                        OnRenderDataVisualsChanged(rd, false);
                    break;
                }
            }
        }

        // A removal leaves the labels of the remaining render data in order, so only insertions need a label.
        public void OnRenderDataInserted(RenderData renderData)
        {
            Debug.Assert(renderData.firstChild == null, "Render data is linked before its children");
            if (m_DirtyTracker.labelsStale)
                return;

            if (m_LabelledInsertions >= labelledInsertionLimit || !TryLabelInsertion(renderData))
            {
                m_DirtyTracker.labelsStale = true;
                return;
            }

            ++m_LabelledInsertions;
            m_DirtyTracker.labelsChanged = true;
        }

        // A removed element stays in the dirty lists and is skipped when they are processed, but its render data goes back to
        // the pool, shared by the trees of the panel. Reused in another tree, it gets a label that RegisterDirty may compare
        // with, as the last entry of a list.
        public void OnRenderDataRemoved(RenderData renderData)
        {
            if (renderData.dirtiedValues != 0)
                m_DirtyTracker.labelsChanged = true;
        }

        int labellingBudget => Math.Max(k_MinLabellingBudget, m_LastRelabelNodeCount);
        int labelledInsertionLimit => Math.Max(k_MinLabelledInsertions, m_LastRelabelNodeCount / k_NodesPerLabelledInsertion);

        bool TryLabelInsertion(RenderData renderData)
        {
            if (m_LabellingWork >= labellingBudget)
                return false;

            int steps = 0;
            RenderData prev = PreviousInPreOrder(renderData, ref steps);
            RenderData next = NextInPreOrder(renderData, ref steps);
            m_LabellingWork += steps;

            long prevLabel = prev.preOrderLabel;
            long gap = (next != null ? next.preOrderLabel : 1L << k_LabelBits) - prevLabel;
            Debug.Assert(gap > 0, "The pre-order labels are not increasing");
            if (gap > 1)
            {
                renderData.preOrderLabel = prevLabel + Math.Min(gap / 2, k_MaxLabelInsertionStep);
                return true;
            }

            // We don't have labels left
            return RespreadLabels(renderData, prev, next);
        }

        // Order maintenance (Bender et al.): grows an aligned label range (its size is a power of 2 and it starts at a
        // multiple of that size) around the insertion point, one level at a time, until few enough nodes have their labels
        // in it, then spreads the labels of these nodes evenly over it. The allowed count grows slower than the range, so a
        // larger range leaves larger gaps: this keeps the amortized cost of an insertion logarithmic, where a fixed density
        // would let repeated insertions at one spot renumber a growing range every few insertions.
        //
        // Two terms: the "range" is a span of label values, and the "run" is the sequence of nodes whose labels fall in
        // the range. Labels increase in pre-order, so these nodes are consecutive in pre-order. The inserted node has no
        // label yet but sits right after prev, so it is always part of the run.
        //
        // Levels: at level L, the range is the 2^L consecutive label values that contain prev's label, starting at a
        // multiple of 2^L. Its bounds are fixed by that alignment, not searched for: the walk only extends the run to the
        // nodes whose labels fall inside it. Going up one level doubles the range, so the run also takes the nodes whose
        // labels fall in the half it adds. The run may be respread only if it has at most 1.5^L nodes, i.e. if at most
        // about 0.75^L of the range's label values are used:
        //
        //   level | labels in range | max nodes | max share of labels used
        //   ------+-----------------+-----------+-------------------------
        //       1 |               2 |         1 | 50%   (never respread: prev and the inserted node are already 2)
        //       2 |               4 |         2 | 50%
        //       3 |               8 |         3 | 38%
        //       5 |              32 |         7 | 22%
        //      10 |            1024 |        57 | 5.6%
        //      20 |       1 048 576 |      3325 | 0.32%
        //
        // So the walk goes up until it reaches the first range whose run is small enough. A run that is too large for its
        // range is not worth respreading: the new gaps between its labels would be small and get used up again soon.
        // Doubling the range may add nodes to the run, but the allowed count grows by 1.5 at each level while most of the
        // added label values are usually free, so the search stops quickly unless labels are crowded over a wide area.
        // The allowed share of used labels falls with the level, so a respread over a large range leaves large gaps
        // behind it, and the next insertions at the same spot find room for a long time: this is what keeps the average
        // cost of an insertion logarithmic. Aligned ranges are nested, so a crowded spot is always looked at through the
        // same ranges, level after level.
        //
        // Example: prev label 12, next label 13, no room in between.
        //   level 1, range [12, 14): run = prev, inserted, next: 3 nodes, max 1: go up.
        //   level 2, range [12, 16): the run also takes any node labelled 14 or 15 (say none): 3 nodes, max 2: go up.
        //   level 3, range [8, 16):  the run also takes any node labelled 8 to 11 (say none): 3 nodes, max 3: respread.
        //   The 3 nodes get the labels 9, 11 and 13 (spacing 8 / 3 = 2, starting at 8 + 2 / 2).
        // Returns false, leaving the labels stale for a full relabel, when the run would cost more than the frame's budget.
        bool RespreadLabels(RenderData inserted, RenderData prev, RenderData next)
        {
            int budget = labellingBudget;
            int steps = 0;

            // The run to renumber, from runFirst to runLast in pre-order, and the nodes just outside it on each side.
            RenderData runFirst = prev, runLast = inserted;
            RenderData beforeRun = PreviousInPreOrder(prev, ref steps);
            RenderData afterRun = next;
            int runCount = 2;

            double maxRunCount = 1; // 1.5^level: the most nodes a range of 2^level labels may hold to be respread
            for (int level = 1; level <= k_LabelBits; ++level)
            {
                maxRunCount *= k_RespreadDensityGrowth;

                // The range of 2^level labels that contains prev's label, aligned on its size, so that the ranges of
                // successive levels are nested: each one doubles the previous one.
                long rangeSize = 1L << level;
                long rangeStart = prev.preOrderLabel & ~(rangeSize - 1);
                long rangeEnd = rangeStart + rangeSize;

                // Extend the run on both sides to every node whose label falls in the range. The nodes left outside keep
                // labels below rangeStart and from rangeEnd on, so the new labels stay in order with them.
                while (beforeRun != null && beforeRun.preOrderLabel >= rangeStart)
                {
                    runFirst = beforeRun;
                    beforeRun = PreviousInPreOrder(beforeRun, ref steps);
                    ++runCount;
                }

                while (afterRun != null && afterRun.preOrderLabel < rangeEnd)
                {
                    runLast = afterRun;
                    afterRun = NextInPreOrder(afterRun, ref steps);
                    ++runCount;
                }

                // runCount also counts the walk that writes the labels below.
                if (m_LabellingWork + steps + runCount > budget)
                {
                    // Labelling in place would cost more than the frame's budget: give up. The caller leaves the labels
                    // stale, and one full relabel rebuilds them before a list that needs ordering is processed.
                    m_LabellingWork += steps;
                    return false;
                }

                if (runCount > maxRunCount)
                {
                    // Too many nodes have their labels in this range: try the next level, twice as wide.
                    continue;
                }

                // This level's range has few enough nodes: spread its labels evenly over the nodes of the run.
                // runCount <= 1.5^level < 2^level, so the spacing is at least 1 and the labels stay in the range.
                long spacing = rangeSize / runCount;
                long label = rangeStart + spacing / 2;
                for (RenderData node = runFirst;; node = NextInPreOrder(node, ref steps))
                {
                    node.preOrderLabel = label;
                    label += spacing;
                    if (node == runLast)
                        break;
                }

                m_LabellingWork += steps;
                return true;
            }

            m_LabellingWork += steps;
            return false;
        }

        // Each step is one link followed.
        static RenderData PreviousInPreOrder(RenderData renderData, ref int steps)
        {
            ++steps;
            RenderData previous = renderData.prevSibling;
            if (previous == null)
                return renderData.parent;

            while (previous.lastChild != null)
            {
                previous = previous.lastChild;
                ++steps;
            }
            return previous;
        }

        static RenderData NextInPreOrder(RenderData renderData, ref int steps)
        {
            ++steps;
            if (renderData.firstChild != null)
                return renderData.firstChild;

            while (renderData.nextSibling == null)
            {
                renderData = renderData.parent;
                if (renderData == null)
                    return null;
                ++steps;
            }
            return renderData.nextSibling;
        }

        // Covers exactly the render data linked under the root, which are the ones carrying this tree. The lists of the
        // classes from firstClass on are rebuilt from the same walk, so they come out in draw order and without removed or
        // duplicate entries. The earlier classes were processed this frame and nothing can be dirtied for them anymore.
        void RelabelPreOrderAndRebuildDirtyLists(RenderDataDirtyTypeClasses firstClass)
        {
            List<RenderData>[] dirtyLists = m_DirtyTracker.dirtyLists;
            RenderDataDirtyTypes rebuiltTypes = RenderDataDirtyTypes.None;
            int previousCount = 0;
            for (int i = 0; i < dirtyLists.Length; ++i)
            {
                if (i < (int)firstClass)
                {
                    Debug.Assert(dirtyLists[i].Count == 0 && m_DirtyTracker.unlistedCounts[i] == 0, "A dirty list is not empty after its class was processed");
                    continue;
                }

                rebuiltTypes |= RenderTreeDirtyTracker.GetDirtyTypes((RenderDataDirtyTypeClasses)i);
                List<RenderData> list = dirtyLists[i];
                int listedCount = list.Count + m_DirtyTracker.unlistedCounts[i];
                previousCount += listedCount;
                list.Clear();
                if (list.Capacity < listedCount) // the walk below then stores into the array of the list without growing it
                    list.Capacity = Math.Max(listedCount, 2 * list.Capacity);
                m_DirtyTracker.needsSort[i] = false;
                m_DirtyTracker.unlistedCounts[i] = 0;
            }

            // The classes processed already have no types in rebuiltTypes, so their masks are empty.
            RenderDataDirtyTypes clippingTypes = rebuiltTypes & RenderTreeDirtyTracker.GetDirtyTypes(RenderDataDirtyTypeClasses.Clipping);
            RenderDataDirtyTypes opacityTypes = rebuiltTypes & RenderTreeDirtyTracker.GetDirtyTypes(RenderDataDirtyTypeClasses.Opacity);
            RenderDataDirtyTypes colorTypes = rebuiltTypes & RenderTreeDirtyTracker.GetDirtyTypes(RenderDataDirtyTypeClasses.Color);
            RenderDataDirtyTypes transformSizeTypes = rebuiltTypes & RenderTreeDirtyTracker.GetDirtyTypes(RenderDataDirtyTypeClasses.TransformSize);
            RenderDataDirtyTypes visualsTypes = rebuiltTypes & RenderTreeDirtyTracker.GetDirtyTypes(RenderDataDirtyTypeClasses.Visuals);
            // A render data keeps its tree until it is reset, so the listed and unlisted counts bound each rebuilt list.
            RenderData[] clippingItems = NoAllocHelpers.ExtractArrayFromList(dirtyLists[(int)RenderDataDirtyTypeClasses.Clipping]);
            RenderData[] opacityItems = NoAllocHelpers.ExtractArrayFromList(dirtyLists[(int)RenderDataDirtyTypeClasses.Opacity]);
            RenderData[] colorItems = NoAllocHelpers.ExtractArrayFromList(dirtyLists[(int)RenderDataDirtyTypeClasses.Color]);
            RenderData[] transformSizeItems = NoAllocHelpers.ExtractArrayFromList(dirtyLists[(int)RenderDataDirtyTypeClasses.TransformSize]);
            RenderData[] visualsItems = NoAllocHelpers.ExtractArrayFromList(dirtyLists[(int)RenderDataDirtyTypeClasses.Visuals]);
            int clippingCount = 0, opacityCount = 0, colorCount = 0, transformSizeCount = 0, visualsCount = 0;

            int index = 0;
            RenderDataDirtyTypes allDirtied = RenderDataDirtyTypes.None;
            RenderData node = m_RootRenderData;
            while (node != null)
            {
                node.preOrderLabel = index++ * k_RelabelStride;
                RenderDataDirtyTypes dirtied = node.dirtiedValues;
                allDirtied |= dirtied;
                if ((dirtied & rebuiltTypes) != 0)
                {
                    if ((dirtied & clippingTypes) != 0)
                        clippingItems[clippingCount++] = node;
                    if ((dirtied & opacityTypes) != 0)
                        opacityItems[opacityCount++] = node;
                    if ((dirtied & colorTypes) != 0)
                        colorItems[colorCount++] = node;
                    if ((dirtied & transformSizeTypes) != 0)
                        transformSizeItems[transformSizeCount++] = node;
                    if ((dirtied & visualsTypes) != 0)
                        visualsItems[visualsCount++] = node;
                }

                if (node.firstChild != null)
                {
                    node = node.firstChild;
                    continue;
                }

                while (node != m_RootRenderData && node.nextSibling == null)
                    node = node.parent;
                node = node == m_RootRenderData ? null : node.nextSibling;
            }

            // Remove extra items
            NoAllocHelpers.ResetListSize(dirtyLists[(int)RenderDataDirtyTypeClasses.Clipping], clippingCount);
            NoAllocHelpers.ResetListSize(dirtyLists[(int)RenderDataDirtyTypeClasses.Opacity], opacityCount);
            NoAllocHelpers.ResetListSize(dirtyLists[(int)RenderDataDirtyTypeClasses.Color], colorCount);
            NoAllocHelpers.ResetListSize(dirtyLists[(int)RenderDataDirtyTypeClasses.TransformSize], transformSizeCount);
            NoAllocHelpers.ResetListSize(dirtyLists[(int)RenderDataDirtyTypeClasses.Visuals], visualsCount);

            int rebuiltCount = clippingCount + opacityCount + colorCount + transformSizeCount + visualsCount;
            Debug.Assert((allDirtied & ~rebuiltTypes) == 0, "An element is dirty for a class that was already processed");
            Debug.Assert(rebuiltCount <= previousCount, "An element dirty for a class was missing from its list");

            m_LastRelabelNodeCount = index;
            m_DirtyTracker.labelsStale = false;
            m_DirtyTracker.labelsChanged = false;
        }

        // needsSort and labelsChanged only say that a list may be out of order, and checking the order is cheaper than
        // sorting a list that is in order.
        static void SortByPreOrder(List<RenderData> list)
        {
            RenderData[] items = NoAllocHelpers.ExtractArrayFromList(list);
            for (int i = 1; i < list.Count; ++i)
            {
                if (items[i].preOrderLabel < items[i - 1].preOrderLabel)
                {
                    list.Sort(k_ComparePreOrder); // This is the only overload that doesn't allocate
                    return;
                }
            }
        }

        // Processes the elements dirty for one class in draw order (UUM-154188)
        void ProcessDirtyClass(RenderDataDirtyTypeClasses dirtyClass, ref ChainBuilderStats stats)
        {
            int classIndex = (int)dirtyClass;
            List<RenderData> dirtyList = m_DirtyTracker.dirtyLists[classIndex];
            int count = dirtyList.Count;
            if (count == 0)
                return;

            // A single entry needs no order: the labels stay stale for later lists.
            if (m_DirtyTracker.labelsStale && count + m_DirtyTracker.unlistedCounts[classIndex] > 1)
            {
                RelabelPreOrderAndRebuildDirtyLists(dirtyClass);
                count = dirtyList.Count;
                if (count == 0)
                    return;
            }
            Debug.Assert(m_DirtyTracker.unlistedCounts[classIndex] == 0, "Elements dirtied while the labels were stale were not listed");

            if (count > 1 && (m_DirtyTracker.needsSort[classIndex] || m_DirtyTracker.labelsChanged))
                SortByPreOrder(dirtyList);

            RenderData[] items = NoAllocHelpers.ExtractArrayFromList(dirtyList);
            RenderDataDirtyTypes dirtyFlags = RenderTreeDirtyTracker.GetDirtyTypes(dirtyClass);
            for (int i = 0; i < count; ++i)
            {
                RenderData renderData = items[i];

                // Removed from the tree since it was listed, or listed twice (removed and pooled, then added again)
                if (renderData.renderTree != this || (renderData.dirtiedValues & dirtyFlags) == 0)
                    continue;

                if (renderData.dirtyID != m_DirtyTracker.dirtyID)
                {
                    switch (dirtyClass)
                    {
                        case RenderDataDirtyTypeClasses.Clipping:
                            RenderEvents.ProcessOnClippingChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.Opacity:
                            RenderEvents.ProcessOnOpacityChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.Color:
                            RenderEvents.ProcessOnColorChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.TransformSize:
                            RenderEvents.ProcessOnTransformOrSizeChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.Visuals:
                            m_RenderTreeManager.visualChangesProcessor.ProcessOnVisualsChanged(renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                    }
                }
                m_DirtyTracker.ClearDirty(renderData, ~dirtyFlags);
                stats.dirtyProcessed++;
            }

            Debug.Assert(dirtyList.Count == count, "An element was dirtied for the class being processed");
            dirtyList.RemoveRange(0, count);
            m_DirtyTracker.needsSort[classIndex] = false;
        }

        public void ProcessChanges(ref ChainBuilderStats stats)
        {
            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Clipping;
            using (k_MarkerClipProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Clipping, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Opacity;
            using (k_MarkerOpacityProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Opacity, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Color;
            using (k_MarkerColorsProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Color, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.TransformSize;
            using (k_MarkerTransformProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.TransformSize, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Visuals;
            using (k_MarkerVisualsProcessing.Auto())
            {
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Visuals, ref stats);

                m_RenderTreeManager.meshGenerationDeferrer.ProcessDeferredWork(m_RenderTreeManager.visualChangesProcessor.meshGenerationContext);

                // Mesh Generation doesn't currently support multiple rounds of generation, so we must flush all deferred
                // work and then schedule the MeshGenerationJobs (and process it's associated callback). Once we make it
                // support multiple rounds, we should move the following call above ProcessDeferredWork and get rid of the
                // second call to ProcessDeferredWork.
                m_RenderTreeManager.visualChangesProcessor.ScheduleMeshGenerationJobs();
                m_RenderTreeManager.meshGenerationDeferrer.ProcessDeferredWork(m_RenderTreeManager.visualChangesProcessor.meshGenerationContext);

                m_RenderTreeManager.visualChangesProcessor.RunMeshModifiers();

                // TODO: Consider postponing this work for later, after each subtrees have been processed.
                // This will help with parallelism.
                m_RenderTreeManager.visualChangesProcessor.ConvertEntriesToCommands(ref stats);

                m_RenderTreeManager.jobManager.CompleteConvertMeshJobs();
                m_RenderTreeManager.jobManager.CompleteCopyMeshJobs();
            }

            m_RenderTreeManager.UpdateElementInfoRecords();

            m_DirtyTracker.labelsChanged = false; // every list was processed, and is empty
            m_LabellingWork = 0;
            m_LabelledInsertions = 0;
            m_AllowedDirtyClasses = AllowedClasses.All;
        }

        internal void OnRenderCommandAdded(RenderChainCommand command)
        {
            if (command.prev == null)
                m_FirstCommand = command;
        }

        internal void OnRenderCommandsRemoved(RenderChainCommand firstCommand, RenderChainCommand lastCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = lastCommand.next;
        }
    }
}
