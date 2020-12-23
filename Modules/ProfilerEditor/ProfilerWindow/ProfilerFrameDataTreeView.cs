// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using UnityEditor.Profiling;
using System;
using UnityEditor;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace UnityEditorInternal
{
    internal class ProfilerFrameDataTreeView : TreeView
    {
        class CallDelay
        {
            float m_DelayTarget;
            Action m_OnSearchChanged;

            public void Start(Action searchChanged, float delayTime)
            {
                m_DelayTarget = Time.realtimeSinceStartup + delayTime;
                m_OnSearchChanged = searchChanged;
            }

            public void Trigger()
            {
                if (!IsDone || m_OnSearchChanged == null)
                    return;

                m_OnSearchChanged.Invoke();
                m_OnSearchChanged = null;
                m_DelayTarget = 0;
            }

            public bool HasTriggered
            {
                get { return m_OnSearchChanged == null; }
            }

            public bool IsDone
            {
                get { return Time.realtimeSinceStartup >= m_DelayTarget; }
            }

            public void Clear()
            {
                m_OnSearchChanged = null;
                EditorApplication.update -= Trigger;
            }
        }

        public static readonly GUIContent kFrameTooltip = EditorGUIUtility.TrTextContent("", "Press 'F' to frame selection");

        const int kMaxPooledRowsCount = 1000000;

        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(1000);
        ProfilerFrameDataMultiColumnHeader m_MultiColumnHeader;

        HierarchyFrameDataView m_FrameDataView;
        // a local cache of the marker Id path, which is modified in frames other than the one originally selected, in case the marker ids changed
        // changing m_SelectionPendingTransfer.markerIdPath instead of this local one would potentially corrupt the markerIdPath in the original frame
        // that would lead to confusion where it is assumed to be valid.
        [NonSerialized] List<int> m_LocalSelectedItemMarkerIdPath = new List<int>();

        [NonSerialized] List<int> m_CachedDeepestRawSampleIndexPath = new List<int>(1024);
        [NonSerialized] ProfilerTimeSampleSelection m_Selected = null;
        [NonSerialized]
        internal ProxySelection proxySelectionInfo = new ProxySelection();
        internal struct ProxySelection
        {
            public bool hasProxySelection;
            public int pathLengthDifferenceForProxy;
            public string nonProxyName;
            public ReadOnlyCollection<string> nonProxySampleStack;
            public GUIContent cachedDisplayContent;
        }

        [NonSerialized]
        protected IProfilerSampleNameProvider m_ProfilerSampleNameProvider;
        [SerializeReference]
        protected IProfilerWindowController m_ProfilerWindowController;

        // Tree of expanded nodes.
        // Each level has a set of expanded marker ids, which are equivalent to sample name.
        class ExpandedMarkerIdHierarchy
        {
            public Dictionary<int, ExpandedMarkerIdHierarchy> expandedMarkers;
        }
        [NonSerialized]
        ExpandedMarkerIdHierarchy m_ExpandedMarkersHierarchy;
        [NonSerialized]

        bool m_ExpandDuringNextSelectionMigration;
        bool m_SelectionNeedsMigration;
        [NonSerialized]
        List<TreeViewItem> m_RowsPool = new List<TreeViewItem>();
        [NonSerialized]
        Stack<List<TreeViewItem>> m_ChildrenPool = new Stack<List<TreeViewItem>>();
        [NonSerialized]
        LinkedList<TreeTraversalState> m_ReusableVisitList = new LinkedList<TreeTraversalState>();
        [NonSerialized]
        List<int> m_ReusableChildrenIds = new List<int>(1024);

        [NonSerialized]
        Stack<LinkedListNode<TreeTraversalState>> m_TreeTraversalStatePool = new Stack<LinkedListNode<TreeTraversalState>>();

        const float k_SearchDelayTime = 0.350f; //350ms
        [NonSerialized]
        CallDelay m_DelayedSearch = new CallDelay();
        [NonSerialized]
        string m_prevSearchPattern = null;

        bool m_ShouldExecuteDelayedSearch = false;
        int m_PrevFrameIndex;

        public event Action<ProfilerTimeSampleSelection> selectionChanged;

        public delegate void SearchChangedCallback(string newSearch);
        public event SearchChangedCallback searchChanged;

        public int sortedProfilerColumn
        {
            get { return m_MultiColumnHeader.sortedProfilerColumn; }
        }

        public bool sortedProfilerColumnAscending
        {
            get { return m_MultiColumnHeader.sortedProfilerColumnAscending; }
        }

        class FrameDataTreeViewItem : TreeViewItem
        {
            HierarchyFrameDataView m_FrameDataView;
            bool m_Initialized;
            string[] m_StringProperties;
            string m_ResolvedCallstack;

            public string[] columnStrings
            {
                get { return m_StringProperties; }
            }

            public string resolvedCallstack
            {
                get
                {
                    // Lazy callstack resolution (only when requested)
                    if (m_ResolvedCallstack == null)
                        m_ResolvedCallstack = m_FrameDataView.ResolveItemCallstack(id);
                    return m_ResolvedCallstack;
                }
            }
            public int samplesCount
            {
                get
                {
                    return m_FrameDataView.GetItemMergedSamplesCount(id);
                }
            }

            public float sortWeight { get; set; }

            public FrameDataTreeViewItem(HierarchyFrameDataView frameDataView, int id, int depth, TreeViewItem parent)
                : base(id, depth, parent, null)
            {
                m_FrameDataView = frameDataView;
                m_Initialized = false;
            }

            internal void Init(HierarchyFrameDataView frameDataView, int id, int depth, TreeViewItem parent)
            {
                this.id = id;
                this.depth = depth;
                this.parent = parent;
                this.displayName = null;
                m_FrameDataView = frameDataView;
                m_Initialized = false;
            }

            public void Init(ProfilerFrameDataMultiColumnHeader.Column[] columns, IProfilerSampleNameProvider profilerSampleNameProvider)
            {
                if (m_Initialized || (m_FrameDataView != null && !m_FrameDataView.valid))
                    return;

                m_StringProperties = new string[columns.Length];
                for (var i = 0; i < columns.Length; i++)
                {
                    var profilerColumn = columns[i].profilerColumn;
                    string data;
                    if (columns[i].profilerColumn == HierarchyFrameDataView.columnName)
                    {
                        data = profilerSampleNameProvider.GetItemName(m_FrameDataView, id);
                        displayName = data;
                    }
                    else
                    {
                        data = m_FrameDataView.GetItemColumnData(id, columns[i].profilerColumn);
                    }
                    m_StringProperties[i] = data;
                }

                m_Initialized = true;
            }
        }

        public ProfilerFrameDataTreeView(TreeViewState state, ProfilerFrameDataMultiColumnHeader multicolumnHeader, IProfilerSampleNameProvider profilerSampleNameProvider, IProfilerWindowController profilerWindowController)
            : base(state, multicolumnHeader)
        {
            Assert.IsNotNull(multicolumnHeader);
            deselectOnUnhandledMouseDown = true;
            m_ProfilerSampleNameProvider = profilerSampleNameProvider;
            m_ProfilerWindowController = profilerWindowController;
            m_MultiColumnHeader = multicolumnHeader;
            m_MultiColumnHeader.sortingChanged += OnSortingChanged;
            profilerWindowController.currentFrameChanged += FrameChanged;
        }

        public void SetFrameDataView(HierarchyFrameDataView frameDataView)
        {
            var needReload = !Equals(m_FrameDataView, frameDataView);
            var needSorting = frameDataView != null && frameDataView.valid &&
                (frameDataView.sortColumn != m_MultiColumnHeader.sortedProfilerColumn ||
                    frameDataView.sortColumnAscending != m_MultiColumnHeader.sortedProfilerColumnAscending);

            if (needReload)
            {
                StoreExpandedState();
                StoreSelectedState();
            }

            m_FrameDataView = frameDataView;
            if (needSorting)
                m_FrameDataView.Sort(m_MultiColumnHeader.sortedProfilerColumn, m_MultiColumnHeader.sortedProfilerColumnAscending);

            if (needReload || needSorting)
            {
                m_ShouldExecuteDelayedSearch = true;
                Reload();
            }
        }

        void AddExpandedChildrenRecursively(TreeViewItem item, ExpandedMarkerIdHierarchy expandedHierarchy)
        {
            if (item.children == null)
                return;

            for (var i = 0; i < item.children.Count; ++i)
            {
                var childItem = item.children[i];
                // Inlining !IsChildListForACollapsedParent without childList.Count == 1 check, as we only create list if we have children
                if (childItem.children != null && childItem.children[0] != null)
                {
                    var subHierarchy = new ExpandedMarkerIdHierarchy();
                    if (expandedHierarchy.expandedMarkers == null)
                        expandedHierarchy.expandedMarkers = new Dictionary<int, ExpandedMarkerIdHierarchy>();
                    try
                    {
                        expandedHierarchy.expandedMarkers.Add(m_FrameDataView.GetItemMarkerID(childItem.id), subHierarchy);
                    }
                    catch (ArgumentException)
                    {
                    }

                    AddExpandedChildrenRecursively(childItem, subHierarchy);
                }
            }
        }

        void StoreExpandedState()
        {
            if (m_ExpandedMarkersHierarchy != null)
                return;
            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return;

            m_ExpandedMarkersHierarchy = new ExpandedMarkerIdHierarchy();
            AddExpandedChildrenRecursively(rootItem, m_ExpandedMarkersHierarchy);
        }

        public void SetSelection(ProfilerTimeSampleSelection selection, bool expandSelection)
        {
            m_Selected = selection;
            m_LocalSelectedItemMarkerIdPath.Clear();
            m_LocalSelectedItemMarkerIdPath.AddRange(selection.markerIdPath);
            m_ExpandDuringNextSelectionMigration = expandSelection;
            m_SelectionNeedsMigration = true;
            proxySelectionInfo = default;
        }

        public void ClearSelection()
        {
            ClearSelection(true);
        }

        // setClearedSelection = false is only supposed to be used when a selection change was reported by the Tree View via SelectionChanged
        // if SetSelection(new List<int>()) would be triggered during SelectionChanged, the TreeViews selection state would get corrupted before it is even fully set.
        void ClearSelection(bool setClearedSelection)
        {
            m_Selected = null;
            m_LocalSelectedItemMarkerIdPath.Clear();
            m_ExpandDuringNextSelectionMigration = false;
            m_SelectionNeedsMigration = false;
            proxySelectionInfo = default;
            if (setClearedSelection)
                SetSelection(new List<int>());
        }

        private bool PropertyPathMatchesSelectedIDs(string legacyPropertyPath, List<int> selectedIDs)
        {
            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return false;

            if (string.IsNullOrEmpty(legacyPropertyPath) || selectedIDs == null || selectedIDs.Count == 0)
            {
                return string.IsNullOrEmpty(legacyPropertyPath) && (selectedIDs == null || selectedIDs.Count == 0);
            }

            return m_FrameDataView.GetItemPath(selectedIDs[0]) == legacyPropertyPath;
        }

        void StoreSelectedState()
        {
            if (m_LocalSelectedItemMarkerIdPath == null)
                m_LocalSelectedItemMarkerIdPath = new List<int>();

            if (m_LocalSelectedItemMarkerIdPath.Count == 0 || m_Selected == null)
                return;

            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return;
            var oldSelection = GetSelection();
            if (oldSelection.Count == 0)
                return;

            proxySelectionInfo = default;
            m_FrameDataView.GetItemMarkerIDPath(oldSelection[0], m_LocalSelectedItemMarkerIdPath);
        }

        void MigrateExpandedState(List<int> newExpandedIds)
        {
            if (newExpandedIds == null)
                return;

            state.expandedIDs = newExpandedIds;
        }

        static readonly ProfilerMarker k_MigrateSelectionStateMarker = new ProfilerMarker($"{nameof(ProfilerFrameDataTreeView)}.{nameof(MigrateSelectedState)}");
        void MigrateSelectedState(bool expandIfNecessary, bool framingAllowed)
        {
            if (m_LocalSelectedItemMarkerIdPath == null || m_Selected == null || m_LocalSelectedItemMarkerIdPath.Count != m_Selected.markerNamePath.Count)
                return;

            m_SelectionNeedsMigration = false;

            var markerNamePath = m_Selected.markerNamePath;

            expandIfNecessary |= m_ExpandDuringNextSelectionMigration;

            using (k_MigrateSelectionStateMarker.Auto())
            {
                var safeFrameWithSafeMarkerIds = m_Selected.frameIndexIsSafe && m_FrameDataView.frameIndex == m_Selected.safeFrameIndex;
                var rawHierarchyView = (m_FrameDataView.viewMode & HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName) == HierarchyFrameDataView.ViewModes.Default;
                var allowProxySelection = !safeFrameWithSafeMarkerIds;

                var finalRawSampleIndex = RawFrameDataView.invalidSampleIndex;

                using (var frameData = new RawFrameDataView(m_FrameDataView.frameIndex, m_FrameDataView.threadIndex))
                {
                    if (!frameData.valid)
                        return;
                    if (!safeFrameWithSafeMarkerIds)
                    {
                        // marker names might have changed Ids between frames, update them if that is the case
                        for (int i = 0; i < m_LocalSelectedItemMarkerIdPath.Count; i++)
                        {
                            m_LocalSelectedItemMarkerIdPath[i] = frameData.GetMarkerId(markerNamePath[i]);
                        }
                    }
                    else if (!allowProxySelection)
                    {
                        for (int i = 0; i < m_LocalSelectedItemMarkerIdPath.Count; i++)
                        {
                            var markerIsEditorOnlyMarker = frameData.GetMarkerFlags(m_LocalSelectedItemMarkerIdPath[i]).HasFlag(Unity.Profiling.LowLevel.MarkerFlags.AvailabilityEditor);
                            if (markerIsEditorOnlyMarker && i < m_LocalSelectedItemMarkerIdPath.Count - 1)
                            {
                                // Technically, proxy selections are not supposed to be allowed when switching between views in the same frame.
                                // However, if there are Editor Only markers in the path that are NOT the last item, Hierarchy View might have collapsed the path from this point forward,
                                // so we need to allow Proxy Selections here.
                                allowProxySelection = true;
                                break;
                            }
                        }
                    }
                    var name = m_Selected.sampleDisplayName;
                    m_CachedDeepestRawSampleIndexPath.Clear();
                    if (m_CachedDeepestRawSampleIndexPath.Capacity < markerNamePath.Count)
                        m_CachedDeepestRawSampleIndexPath.Capacity = markerNamePath.Count;

                    if (allowProxySelection || rawHierarchyView)
                    {
                        finalRawSampleIndex = ProfilerTimelineGUI.FindFirstSampleThroughMarkerPath(
                            frameData, m_ProfilerSampleNameProvider,
                            m_LocalSelectedItemMarkerIdPath, markerNamePath.Count, ref name,
                            longestMatchingPath: m_CachedDeepestRawSampleIndexPath);
                    }
                    else
                    {
                        finalRawSampleIndex = ProfilerTimelineGUI.FindNextSampleThroughMarkerPath(
                            frameData, m_ProfilerSampleNameProvider,
                            m_LocalSelectedItemMarkerIdPath, markerNamePath.Count, ref name,
                            ref m_CachedDeepestRawSampleIndexPath);
                    }
                }
                var newSelectedId = m_FrameDataView.GetRootItemID();
                bool selectedItemsPathIsExpanded = true;
                var proxySelection = new ProxySelection();
                proxySelectionInfo = default;
                var deepestPath = m_CachedDeepestRawSampleIndexPath.Count;

                if (finalRawSampleIndex >= 0 || allowProxySelection && deepestPath > 0)
                {
                    // if a valid raw index was found, find the corresponding HierarchyView Sample id next:
                    newSelectedId = GetItemIdFromRawFrameDataIndexPath(m_FrameDataView, m_CachedDeepestRawSampleIndexPath, out deepestPath, out selectedItemsPathIsExpanded);
                    if (m_LocalSelectedItemMarkerIdPath.Count > deepestPath && newSelectedId >= 0)
                    {
                        proxySelection.hasProxySelection = true;
                        proxySelection.nonProxyName = m_Selected.sampleDisplayName;
                        proxySelection.nonProxySampleStack = m_Selected.markerNamePath;
                        proxySelection.pathLengthDifferenceForProxy = deepestPath - m_LocalSelectedItemMarkerIdPath.Count;
                    }
                }

                var newSelection = (newSelectedId == 0) ? new List<int>() : new List<int>() { newSelectedId };
                state.selectedIDs = newSelection;

                // Framing invalidates expanded state and this is very expensive operation to perform each frame.
                // Thus we auto frame selection only when we are not currently receiving profiler data from the Editor we are profiling, or the user opted into a "Live" view of the data
                if (newSelectedId != 0 && isInitialized && framingAllowed && (selectedItemsPathIsExpanded || expandIfNecessary))
                    FrameItem(newSelectedId);
                m_ExpandDuringNextSelectionMigration = false;

                proxySelectionInfo = proxySelection;
            }
        }

        public int GetItemIDFromRawFrameDataViewIndex(HierarchyFrameDataView frameDataView, int rawSampleIndex, ReadOnlyCollection<int> markerIdPath)
        {
            using (var rawFrameDataView = new RawFrameDataView(frameDataView.frameIndex, frameDataView.threadIndex))
            {
                var unreachableDepth = markerIdPath == null ? frameDataView.maxDepth + 1 : markerIdPath.Count;

                m_CachedDeepestRawSampleIndexPath.Clear();
                if (m_CachedDeepestRawSampleIndexPath.Capacity < unreachableDepth)
                    m_CachedDeepestRawSampleIndexPath.Capacity = unreachableDepth;

                string name = null;
                var foundRawIndex = ProfilerTimelineGUI.FindNextSampleThroughMarkerPath(rawFrameDataView, m_ProfilerSampleNameProvider, markerIdPath, unreachableDepth, ref name, ref m_CachedDeepestRawSampleIndexPath, specificRawSampleIndexToFind: rawSampleIndex);
                if (foundRawIndex < 0 || foundRawIndex != rawSampleIndex)
                    return HierarchyFrameDataView.invalidSampleId;

                // We don't care about the path being extended here so, reduce checks by assuming the path is not expanded (this saves calls to IsExpanded)
                var selectedItemsPathIsExpanded = false;
                var newSelectedId = GetItemIdFromRawFrameDataIndexPath(frameDataView, m_CachedDeepestRawSampleIndexPath, out int deepestPath, out selectedItemsPathIsExpanded);
                if (deepestPath < m_CachedDeepestRawSampleIndexPath.Count)
                    // the path has been cut short and the sample wasn't found
                    return HierarchyFrameDataView.invalidSampleId;
                return newSelectedId;
            }
        }

        int GetItemIdFromRawFrameDataIndexPath(HierarchyFrameDataView m_FrameDataView, List<int> deepestRawSampleIndexPathFound, out int foundDepth, out bool selectedItemsPathIsExpanded)
        {
            selectedItemsPathIsExpanded = true;
            var newSelectedId = m_FrameDataView.GetRootItemID();
            var deepestPath = deepestRawSampleIndexPathFound.Count;

            for (int markerDepth = 0; markerDepth < deepestPath; markerDepth++)
            {
                var oldSelectedId = newSelectedId;

                if (m_FrameDataView.HasItemChildren(newSelectedId))
                {
                    // TODO: maybe HierarchyFrameDataView should just have a method GetChildItemByRawFrameDataViewIndex to avoid this List<int> marshalling need...
                    m_FrameDataView.GetItemChildren(newSelectedId, m_ReusableChildrenIds);

                    for (int i = 0; i < m_ReusableChildrenIds.Count; i++)
                    {
                        var childId = m_ReusableChildrenIds[i];

                        if (m_FrameDataView.ItemContainsRawFrameDataViewIndex(childId, deepestRawSampleIndexPathFound[markerDepth]))
                        {
                            // check if the parent is expanded
                            if (selectedItemsPathIsExpanded && !IsExpanded(newSelectedId))
                                selectedItemsPathIsExpanded = false;

                            newSelectedId = childId;
                            break;
                        }
                    }
                }
                if (oldSelectedId == newSelectedId)
                {
                    // there was no fitting sample in this scope so the path has been cut short here
                    deepestPath = markerDepth;
                    break;
                }
            }
            foundDepth = deepestPath;
            return newSelectedId;
        }

        public IList<int> GetSelectedInstanceIds()
        {
            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return null;
            var selection = GetSelection();
            if (selection == null || selection.Count == 0)
                return null;

            var allInstanceIds = new List<int>();
            var instanceIds = new List<int>();
            foreach (var selectedId in selection)
            {
                m_FrameDataView.GetItemMergedSamplesInstanceID(selectedId, instanceIds);
                allInstanceIds.AddRange(instanceIds);
            }
            return allInstanceIds;
        }

        public void Clear()
        {
            if (m_FrameDataView == null)
                return;

            m_FrameDataView.Dispose();
            m_FrameDataView = null;

            m_RowsPool.Clear();
            m_ChildrenPool.Clear();
            m_ReusableVisitList.Clear();
            m_ReusableChildrenIds.Clear();
            m_TreeTraversalStatePool.Clear();

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var rootID = (m_FrameDataView != null && m_FrameDataView.valid) ? m_FrameDataView.GetRootItemID() : 0;
            return new FrameDataTreeViewItem(m_FrameDataView, rootID, ProfilerFrameDataHierarchyView.invalidTreeViewDepth, null);
        }

        void FrameChanged(int i, bool b)
        {
            m_DelayedSearch.Clear();
            m_ShouldExecuteDelayedSearch = true;
            if (m_prevSearchPattern != searchString && string.IsNullOrEmpty(searchString))
                return;
            if (m_FrameDataView != null && m_FrameDataView.valid)
                Reload();
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (m_RowsPool.Count < kMaxPooledRowsCount)
                m_RowsPool.AddRange(m_Rows);
            m_Rows.Clear();

            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return m_Rows;

            var newExpandedIds = m_ExpandedMarkersHierarchy == null ? null : new List<int>(state.expandedIDs.Count);
            bool requestedDelayedSearch = false;

            bool patternEmpty = string.IsNullOrEmpty(searchString);

            if (patternEmpty)
                m_prevSearchPattern = searchString;

            if (!patternEmpty)
            {
                if (m_prevSearchPattern != searchString || m_DelayedSearch.HasTriggered)
                {
                    m_prevSearchPattern = searchString;
                    requestedDelayedSearch = true;
                    m_ShouldExecuteDelayedSearch = false;
                    m_DelayedSearch.Start(() =>
                    {
                        m_ShouldExecuteDelayedSearch = true;
                        Reload();
                        EditorApplication.update -= m_DelayedSearch.Trigger;
                    }, k_SearchDelayTime);

                    EditorApplication.update -= m_DelayedSearch.Trigger;
                    EditorApplication.update += m_DelayedSearch.Trigger;
                }
                else if (m_ShouldExecuteDelayedSearch)
                {
                    m_ShouldExecuteDelayedSearch = false;
                    m_Rows.Clear();
                    Search(root, searchString, m_Rows);
                }

                if (ProfilerDriver.lastFrameIndex != m_PrevFrameIndex)
                {
                    m_Rows.Clear();
                    Search(root, searchString, m_Rows);
                }
            }
            else
            {
                m_prevSearchPattern = searchString;
                m_Rows.Clear();
                AddAllChildren((FrameDataTreeViewItem)root, m_ExpandedMarkersHierarchy, m_Rows, newExpandedIds);
            }

            if (!requestedDelayedSearch)
            {
                MigrateExpandedState(newExpandedIds);
                MigrateSelectedState(false, !m_ProfilerWindowController.ProfilerWindowOverheadIsAffectingProfilingRecordingData() || m_UpdateViewLive);
            }

            m_PrevFrameIndex = ProfilerDriver.lastFrameIndex;
            return m_Rows;
        }

        void Search(TreeViewItem searchFromThis, string search, List<TreeViewItem> result)
        {
            if (searchFromThis == null)
                throw new ArgumentException("Invalid searchFromThis: cannot be null", "searchFromThis");
            if (string.IsNullOrEmpty(search))
                throw new ArgumentException("Invalid search: cannot be null or empty", "search");

            const int kItemDepth = 0; // tree is flattened when searching

            var stack = new Stack<int>();
            m_FrameDataView.GetItemChildren(searchFromThis.id, m_ReusableChildrenIds);
            foreach (var childId in m_ReusableChildrenIds)
                stack.Push(childId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // Matches search?
                var functionName = m_ProfilerSampleNameProvider.GetItemName(m_FrameDataView, current);
                if (functionName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var item = AcquireFrameDataTreeViewItem(m_FrameDataView, current, kItemDepth, searchFromThis);

                    item.displayName = functionName;
                    item.sortWeight = m_FrameDataView.GetItemColumnDataAsFloat(current, m_FrameDataView.sortColumn);

                    searchFromThis.AddChild(item);
                    result.Add(item);
                }

                m_FrameDataView.GetItemChildren(current, m_ReusableChildrenIds);
                foreach (var childId in m_ReusableChildrenIds)
                    stack.Push(childId);
            }

            // Sort filtered results based on HerarchyFrameData sorting settings
            var sortModifier = m_FrameDataView.sortColumnAscending ? 1 : -1;
            result.Sort((_x, _y) => {
                var x = _x as FrameDataTreeViewItem;
                var y = _y as FrameDataTreeViewItem;
                if ((x == null) || (y == null))
                    return 0;

                int retVal;
                if (x.sortWeight != y.sortWeight)
                    retVal = x.sortWeight < y.sortWeight ? -1 : 1;
                else
                    retVal = EditorUtility.NaturalCompare(x.displayName, y.displayName);

                return retVal * sortModifier;
            });
        }

        // Hierarchy traversal state.
        // Represents node to descent into and expansion state of its children.
        // This way we follow samples and expansion hierarchy simultaneously avoiding expensive
        // expansion lookup by a full sample path in a global table.
        struct TreeTraversalState
        {
            public FrameDataTreeViewItem item;
            public ExpandedMarkerIdHierarchy expandedHierarchy;
        }

        LinkedListNode<TreeTraversalState> AcquireTreeTraversalStateNode(FrameDataTreeViewItem item, ExpandedMarkerIdHierarchy expandedHierarchy)
        {
            if (m_TreeTraversalStatePool.Count == 0)
                return new LinkedListNode<TreeTraversalState>(new TreeTraversalState() { item = item, expandedHierarchy = expandedHierarchy });

            var node = m_TreeTraversalStatePool.Pop();
            node.Value = new TreeTraversalState() { item = item, expandedHierarchy = expandedHierarchy };
            return node;
        }

        FrameDataTreeViewItem AcquireFrameDataTreeViewItem(HierarchyFrameDataView frameDataView, int id, int depth, TreeViewItem parent)
        {
            if (m_RowsPool.Count > 0)
            {
                FrameDataTreeViewItem child = (FrameDataTreeViewItem)m_RowsPool[m_RowsPool.Count - 1];
                m_RowsPool.RemoveAt(m_RowsPool.Count - 1);
                child.Init(m_FrameDataView, id, depth, parent);
                if (child.children != null)
                {
                    m_ChildrenPool.Push(child.children);
                    child.children = null;
                }

                return child;
            }

            return new FrameDataTreeViewItem(m_FrameDataView, id, depth, parent);
        }

        void AddAllChildren(FrameDataTreeViewItem parent, ExpandedMarkerIdHierarchy parentExpandedHierararchy, IList<TreeViewItem> newRows, List<int> newExpandedIds)
        {
            m_ReusableVisitList.AddFirst(AcquireTreeTraversalStateNode(parent, parentExpandedHierararchy));

            // Depth-first traversal.
            // Think of it as an unrolled recursion where stack state is defined by TreeTraversalState.
            while (m_ReusableVisitList.First != null)
            {
                var currentItem = m_ReusableVisitList.First.Value;
                m_TreeTraversalStatePool.Push(m_ReusableVisitList.First);
                m_ReusableVisitList.RemoveFirst();

                if (currentItem.item.depth != ProfilerFrameDataHierarchyView.invalidTreeViewDepth)
                    newRows.Add(currentItem.item);

                m_FrameDataView.GetItemChildren(currentItem.item.id, m_ReusableChildrenIds);
                var childrenCount = m_ReusableChildrenIds.Count;
                if (childrenCount == 0)
                    continue;

                if (currentItem.item.depth != ProfilerFrameDataHierarchyView.invalidTreeViewDepth)
                {
                    // Check expansion state from a previous frame view state (marker id path) or current tree view state (frame-specific id).
                    bool needsExpansion;
                    if (m_ExpandedMarkersHierarchy == null)
                    {
                        // When we alter expansion state of the currently selected frame,
                        // we rely on TreeView's IsExpanded functionality.
                        needsExpansion = IsExpanded(currentItem.item.id);
                    }
                    else
                    {
                        // When we switch to another frame, we rebuild expanded state based on stored m_ExpandedMarkersHierarchy
                        // which represents tree of expanded nodes.
                        needsExpansion = currentItem.expandedHierarchy != null;
                    }

                    if (!needsExpansion)
                    {
                        if (currentItem.item.children == null)
                            currentItem.item.children = CreateChildListForCollapsedParent();
                        continue;
                    }

                    if (newExpandedIds != null)
                        newExpandedIds.Add(currentItem.item.id);
                }

                // Generate children based on the view data.
                if (currentItem.item.children == null)
                {
                    // Reuse existing list.
                    if (m_ChildrenPool.Count > 0)
                        currentItem.item.children = m_ChildrenPool.Pop();
                    else
                        currentItem.item.children = new List<TreeViewItem>();
                }
                currentItem.item.children.Clear();
                currentItem.item.children.Capacity = childrenCount;

                for (var i = 0; i < childrenCount; ++i)
                {
                    var child = AcquireFrameDataTreeViewItem(m_FrameDataView, m_ReusableChildrenIds[i], currentItem.item.depth + 1, currentItem.item);
                    currentItem.item.children.Add(child);
                }

                // Add children to the traversal list.
                // We add all of them in front, so it is depth search, but with preserved siblings order.
                LinkedListNode<TreeTraversalState> prev = null;
                foreach (var child in currentItem.item.children)
                {
                    var childMarkerId = m_FrameDataView.GetItemMarkerID(child.id);
                    ExpandedMarkerIdHierarchy childExpandedHierarchy = null;
                    if (currentItem.expandedHierarchy != null && currentItem.expandedHierarchy.expandedMarkers != null)
                        currentItem.expandedHierarchy.expandedMarkers.TryGetValue(childMarkerId, out childExpandedHierarchy);

                    var traversalState = AcquireTreeTraversalStateNode((FrameDataTreeViewItem)child, childExpandedHierarchy);
                    if (prev == null)
                        m_ReusableVisitList.AddFirst(traversalState);
                    else
                        m_ReusableVisitList.AddAfter(prev, traversalState);
                    prev = traversalState;
                }
            }

            if (newExpandedIds != null)
                newExpandedIds.Sort();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            ProfilerTimeSampleSelection selection;
            // When we navigate through frames and there is no path exists,
            // we still want to be able to frame and select proper sample once it is present again.
            // Thus we invalidate selection only if user selected new item.
            // Same applies to expanded state.
            if (selectedIds.Count > 0)
            {
                ClearSelection(setClearedSelection: false);
            }

            if (selectedIds.Count > 0 && m_FrameDataView.valid)
            {
                List<int> rawIds = new List<int>();
                if (selectedIds.Count > 1)
                {
                    List<int> ids = new List<int>();
                    for (int i = 0; i < selectedIds.Count; i++)
                    {
                        m_FrameDataView.GetItemRawFrameDataViewIndices(selectedIds[i], ids);
                    }
                }
                else
                {
                    m_FrameDataView.GetItemRawFrameDataViewIndices(selectedIds[0], rawIds);
                }
                selection = new ProfilerTimeSampleSelection(m_FrameDataView.frameIndex, m_FrameDataView.threadGroupName, m_FrameDataView.threadName, m_FrameDataView.threadId, rawIds, m_ProfilerSampleNameProvider.GetItemName(m_FrameDataView, selectedIds[0]));
                var selectedId = selectedIds[0];
                var rawSampleIndices = new List<int>(m_FrameDataView.GetItemMergedSamplesCount(selectedId));
                m_FrameDataView.GetItemRawFrameDataViewIndices(selectedId, rawSampleIndices);
                var markerIDs = new List<int>(m_FrameDataView.GetItemDepth(selectedId));
                using (var iterator = new RawFrameDataView(m_FrameDataView.frameIndex, m_FrameDataView.threadIndex))
                {
                    string name = null;
                    ProfilerTimelineGUI.GetItemMarkerIdPath(iterator, m_ProfilerSampleNameProvider, rawSampleIndices[0], ref name, ref markerIDs);
                }
                selection.GenerateMarkerNamePath(m_FrameDataView, markerIDs);
            }
            else
            {
                selection = null;
            }

            var id = selectedIds.Count > 0 ? selectedIds[0] : RawFrameDataView.invalidSampleIndex;
            if (selectionChanged != null)
                selectionChanged.Invoke(selection);
        }

        protected override void ExpandedStateChanged()
        {
            // Invalidate saved expanded state if user altered current state.
            m_ExpandedMarkersHierarchy = null;
        }

        protected override void DoubleClickedItem(int id)
        {
        }

        protected override void ContextClickedItem(int id)
        {
        }

        protected override void ContextClicked()
        {
        }

        protected override void SearchChanged(string newSearch)
        {
            if (searchChanged != null)
                searchChanged.Invoke(newSearch);
        }

        protected override IList<int> GetAncestors(int id)
        {
            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return new List<int>();

            var ancestors = new List<int>();
            m_FrameDataView.GetItemAncestors(id, ancestors);
            return ancestors;
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            if (m_FrameDataView == null || !m_FrameDataView.valid)
                return new List<int>();

            var children = new List<int>();
            m_FrameDataView.GetItemDescendantsThatHaveChildren(id, children);
            return children;
        }

        void OnSortingChanged(MultiColumnHeader header)
        {
            if (m_FrameDataView == null || multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            m_FrameDataView.Sort(m_MultiColumnHeader.sortedProfilerColumn, m_MultiColumnHeader.sortedProfilerColumnAscending);
            Reload();
        }

        bool m_UpdateViewLive = false;
        // Profiler UI should be calling this OnGUI over the base OnGUI
        public void OnGUI(Rect rect, bool updateViewLive)
        {
            m_UpdateViewLive = updateViewLive;
            if (m_SelectionNeedsMigration && m_Selected != null)
            {
                var profilingEditor = m_ProfilerWindowController.ProfilerWindowOverheadIsAffectingProfilingRecordingData();
                if (profilingEditor)
                    m_ExpandDuringNextSelectionMigration = false;
                MigrateSelectedState(m_ExpandDuringNextSelectionMigration, !profilingEditor || updateViewLive);
            }
            if (m_FrameDataView != null && m_FrameDataView.valid)
                base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            var item = (FrameDataTreeViewItem)args.item;
            item.Init(m_MultiColumnHeader.columns, m_ProfilerSampleNameProvider);

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var cellRect = args.GetCellRect(i);
                CellGUI(cellRect, item, i == 0, args.GetColumn(i), ref args);
            }

            // Tooltip logic only when item is selected
            if (!args.selected)
                return;

            var hovered = args.rowRect.Contains(Event.current.mousePosition);
            if (!hovered)
                return;

            // Framing hint when searching
            if (hasSearch)
            {
                GUIStyle.SetMouseTooltip(kFrameTooltip.tooltip, args.rowRect);
            }
        }

        void CellGUI(Rect cellRect, FrameDataTreeViewItem item, bool needsIndent, int column, ref RowGUIArgs args)
        {
            if (needsIndent)
            {
                var indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
            }
            CenterRectUsingSingleLineHeight(ref cellRect);

            var content = GUIContent.Temp(item.columnStrings[column], string.Empty);
            DefaultStyles.label.Draw(cellRect, content, false, false, args.selected, args.focused);
        }
    }

    public class ProfilerFrameDataMultiColumnHeader : MultiColumnHeader
    {
        public struct Column
        {
            public int profilerColumn;
            public GUIContent headerLabel;
        }
        Column[] m_Columns;

        public Column[] columns
        {
            get { return m_Columns; }
        }

        public int sortedProfilerColumn
        {
            get { return GetProfilerColumn(sortedColumnIndex); }
        }
        public bool sortedProfilerColumnAscending
        {
            get { return IsSortedAscending(sortedColumnIndex); }
        }

        public ProfilerFrameDataMultiColumnHeader(MultiColumnHeaderState state, Column[] columns)
            : base(state)
        {
            Assert.IsNotNull(columns);
            m_Columns = columns;
        }

        public int GetMultiColumnHeaderIndex(int profilerColumn)
        {
            for (var i = 0; i < m_Columns.Length; ++i)
            {
                if (m_Columns[i].profilerColumn == profilerColumn)
                    return i;
            }

            return 0;
        }

        public static int GetMultiColumnHeaderIndex(Column[] columns, int profilerColumn)
        {
            for (var i = 0; i < columns.Length; ++i)
            {
                if (columns[i].profilerColumn == profilerColumn)
                    return i;
            }

            return 0;
        }

        public int GetProfilerColumn(int multiColumnHeaderIndex)
        {
            return m_Columns[multiColumnHeaderIndex].profilerColumn;
        }
    }
}
