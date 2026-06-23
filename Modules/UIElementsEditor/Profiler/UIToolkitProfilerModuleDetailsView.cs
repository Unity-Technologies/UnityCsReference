// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class UIToolkitProfilerModuleDetailsView : ProfilerModuleViewController
    {
        const int k_MainThreadIndex = 0;
        const string k_ViewDataKeyPrefix = "uitoolkit-profiler";

        readonly ProfilerCounterDescriptor[] m_Counters;
        readonly string[] m_HierarchyMarkerNames;
        readonly string[] m_ColumnDisplayTitles;
        MultiColumnTreeViewWithTotal m_TreeView;
        Label m_EmptyOverlay;
        readonly List<TreeViewItemData<PanelDetailRow>> m_RootItems = new List<TreeViewItemData<PanelDetailRow>>();
        readonly List<int> m_ChildItemIdsScratch = new List<int>(64);
        readonly Stack<int> m_HierarchyWalkStack = new Stack<int>(256);
        IVisualElementScheduledItem m_ApplyTreeSourceScheduled;

        // Reusable collections for ReloadData; cleared each frame instead of re-allocated.
        readonly Dictionary<int, int> m_MarkerIdToCounterIndex = new Dictionary<int, int>();
        readonly Dictionary<(EntityId, int), float> m_PanelMarkerTimes = new Dictionary<(EntityId, int), float>();
        readonly List<EntityId> m_PanelOrder = new List<EntityId>();
        readonly HashSet<EntityId> m_PanelsSeen = new HashSet<EntityId>();
        readonly Dictionary<EntityId, UIToolkitPanelUpdateMetricsInfo> m_PanelUpdateMetrics = new Dictionary<EntityId, UIToolkitPanelUpdateMetricsInfo>();
        readonly Dictionary<EntityId, int> m_PanelEventCounts = new Dictionary<EntityId, int>();
        readonly float[] m_CounterTotalsScratch;

        PanelComponentsPaneController m_PanelComponentsPane;

        internal struct PanelDetailRow
        {
            public EntityId panelEntityId;
            public string panelName;
            public float[] counterTimesMs;
            public float totalTimeMs;
            // PANEL_METRICS chunk may be absent for a panel that ran updaters (so it shows up via
            // markers) but wasn't captured this frame. Track presence so the cells render "—"
            // instead of a misleading "0".
            public bool hasUpdateMetrics;
            public uint hierarchyVersionChanges;
            public uint repaintVersionChanges;
            public int visualElementCount;
            public int eventCount;
        }

        public UIToolkitProfilerModuleDetailsView(ProfilerWindow profilerWindow, ProfilerCounterDescriptor[] counters, string[] hierarchyMarkerNames, string[] columnDisplayTitles)
            : base(profilerWindow)
        {
            UnityEngine.Debug.Assert(counters != null && hierarchyMarkerNames != null && columnDisplayTitles != null
                && counters.Length == hierarchyMarkerNames.Length && counters.Length == columnDisplayTitles.Length,
                "Profiler counters, hierarchy marker names, and column display titles must be parallel arrays of equal length.");
            m_Counters = counters;
            m_HierarchyMarkerNames = hierarchyMarkerNames;
            m_ColumnDisplayTitles = columnDisplayTitles;
            m_CounterTotalsScratch = new float[counters.Length];
        }

        static string BuildColumnHeaderTooltip(ProfilerCounterDescriptor counter, string hierarchyMarkerName)
        {
            var desc = counter.Description;
            if (!string.IsNullOrEmpty(desc))
                return string.Format(L10n.Tr("{0}\n\nProfiler marker: {1}"), desc, hierarchyMarkerName);
            return string.Format(L10n.Tr("Profiler marker: {0}"), hierarchyMarkerName);
        }

        protected override VisualElement CreateView()
        {
            var columns = new Columns { reorderable = true };
            columns.Add(new Column
            {
                name = "panel",
                title = L10n.Tr("Panel"),
                minWidth = 120,
                optional = false,
                comparison = (a, b) => EntityId.ToULong(GetRowData(a).panelEntityId).CompareTo(EntityId.ToULong(GetRowData(b).panelEntityId)),
                bindCell = (e, index) => ((Label)e).text = GetRowData(index).panelName ?? string.Empty
            });

            for (var i = 0; i < m_Counters.Length; i++)
            {
                var counterIndex = i;
                var headerTooltip = BuildColumnHeaderTooltip(m_Counters[i], m_HierarchyMarkerNames[i]);
                var column = new Column
                {
                    name = m_Counters[i].Name,
                    title = m_ColumnDisplayTitles[i],
                    minWidth = 80,
                    width = 120,
                    makeHeader = UIToolkitProfilerToolbarHelpers.CreateDefaultColumnHeaderContent,
                    comparison = (a, b) => rowTimeMs(a, counterIndex).CompareTo(rowTimeMs(b, counterIndex)),
                    bindCell = (e, index) =>
                    {
                        var label = (Label)e;
                        var data = GetRowData(index);
                        if (data.counterTimesMs != null && counterIndex < data.counterTimesMs.Length)
                            label.text = FormatTime(data.counterTimesMs[counterIndex]);
                        else
                            label.text = UIToolkitProfilerToolbarHelpers.NoDataCell;
                    }
                };
                column.bindHeader = (ve) => UIToolkitProfilerToolbarHelpers.BindColumnHeaderWithTooltip(ve, column, headerTooltip);
                columns.Add(column);
            }

            var eventsColumn = new Column
            {
                name = "events",
                title = L10n.Tr("Events"),
                minWidth = 60,
                width = 80,
                makeHeader = UIToolkitProfilerToolbarHelpers.CreateDefaultColumnHeaderContent,
                comparison = (a, b) => GetRowData(a).eventCount.CompareTo(GetRowData(b).eventCount),
                bindCell = (e, index) => ((Label)e).text = GetRowData(index).eventCount.ToString("N0"),
            };
            var eventsHeaderTooltip = L10n.Tr("Number of events dispatched on this panel during the frame (pointer, keyboard, navigation, and others). Click the row to see the per-event list in the right pane.");
            eventsColumn.bindHeader = (ve) => UIToolkitProfilerToolbarHelpers.BindColumnHeaderWithTooltip(ve, eventsColumn, eventsHeaderTooltip);
            columns.Add(eventsColumn);

            columns.Add(new Column
            {
                name = "total",
                title = L10n.Tr("Total"),
                minWidth = 80,
                comparison = (a, b) => GetRowData(a).totalTimeMs.CompareTo(GetRowData(b).totalTimeMs),
                bindCell = (e, index) => ((Label)e).text = FormatTime(GetRowData(index).totalTimeMs)
            });

            columns.Add(MakePanelCountColumn("hierarchyChanges", L10n.Tr("Hierarchy Changes"),
                L10n.Tr("Number of hierarchy version changes (add/remove/reparent of VisualElements) since the previous frame."),
                r => r.hierarchyVersionChanges));
            columns.Add(MakePanelCountColumn("repaintChanges", L10n.Tr("Repaint Changes"),
                L10n.Tr("Number of repaint version changes since the previous frame. High values indicate elements are being marked dirty frequently."),
                r => r.repaintVersionChanges));
            columns.Add(MakePanelCountColumn("veCount", L10n.Tr("VE Count"),
                L10n.Tr("Total number of VisualElements in this panel's hierarchy."),
                r => r.visualElementCount));

            // Flat list (no children); using the tree variant gives us a totals row and consistent
            // alternating-row rendering with the details view.
            m_TreeView = new MultiColumnTreeViewWithTotal(columns)
            {
                fixedItemHeight = 18,
                sortingMode = ColumnSortingMode.Default,
                viewDataKey = "uitoolkit-profiler-details-treeview",
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                horizontalScrollingEnabled = true,
                style = { flexGrow = 1 }
            };

            m_TreeView.columnSortingChanged += OnColumnSortingChanged;
            m_TreeView.itemsChosen += OnItemsChosen;
            m_TreeView.selectedIndicesChanged += OnPanelRowSelectionChanged;

            var mainToolbar = new Toolbar();
            mainToolbar.Add(new ToolbarSpacer { flex = true });
            UIToolkitProfilerToolbarHelpers.AddCommonButtons(mainToolbar);

            m_PanelComponentsPane = new PanelComponentsPaneController(k_ViewDataKeyPrefix);

            var treeStack = UIToolkitProfilerToolbarHelpers.WrapWithEmptyOverlay(
                m_TreeView,
                "uitoolkit-profiler-details-tree-stack",
                L10n.Tr("No data to show. Start profiling UI Toolkit content to see details."),
                out m_EmptyOverlay);

            var splitView = m_PanelComponentsPane.WireUp(treeStack, mainToolbar);

            var root = new VisualElement { name = "uitoolkit-profiler-details-root" };
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.Add(mainToolbar);
            root.Add(splitView);

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return root;
        }

        // Returns default for indices without a backing tree item (transient state during rebuild)
        // so callers can render NoDataCell instead of throwing.
        PanelDetailRow GetRowData(int index)
        {
            if (m_TreeView == null)
                return default;
            return m_TreeView.GetItemDataForIndex<PanelDetailRow>(index);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_TreeView != null)
            {
                m_TreeView.itemsChosen -= OnItemsChosen;
                m_TreeView.columnSortingChanged -= OnColumnSortingChanged;
                m_TreeView.selectedIndicesChanged -= OnPanelRowSelectionChanged;
            }
            m_PanelComponentsPane?.Dispose();
            m_PanelComponentsPane = null;
            m_ApplyTreeSourceScheduled?.Pause();
            m_ApplyTreeSourceScheduled = null;
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnColumnSortingChanged()
        {
            ScheduleApplyTreeViewSource();
        }

        void OnItemsChosen(IEnumerable<object> chosen)
        {
            foreach (var item in chosen)
            {
                if (item is PanelDetailRow row && row.panelEntityId != EntityId.None)
                {
                    UIToolkitProfilerToolbarHelpers.PingEntity(row.panelEntityId);
                    return;
                }
            }
        }

        void OnPanelRowSelectionChanged(IEnumerable<int> selectedIndices)
        {
            if (m_PanelComponentsPane == null)
                return;
            foreach (var index in selectedIndices)
            {
                var data = GetRowData(index);
                if (data.panelEntityId != EntityId.None)
                {
                    m_PanelComponentsPane.SetSelectedPanel(data.panelEntityId, data.panelName);
                    return;
                }
            }
            m_PanelComponentsPane.ClearSelection();
        }

        void OnSelectedFrameIndexChanged(long frame)
        {
            ReloadData(frame);
        }

        void ReloadData(long frameIndex)
        {
            m_RootItems.Clear();

            if (frameIndex < 0)
            {
                m_PanelComponentsPane?.LoadFrameMetadata(null, -1);
            }
            else
            {
                using (var hierarchyData = ProfilerDriver.GetHierarchyFrameDataView(
                    (int)frameIndex, k_MainThreadIndex,
                    HierarchyFrameDataView.ViewModes.Default,
                    HierarchyFrameDataView.columnDontSort, false))
                using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
                {
                    m_PanelComponentsPane?.LoadFrameMetadata(rawFrameData, frameIndex);

                    if (hierarchyData.valid)
                    {
                        m_MarkerIdToCounterIndex.Clear();
                        for (var i = 0; i < m_HierarchyMarkerNames.Length; i++)
                        {
                            var markerId = hierarchyData.GetMarkerId(m_HierarchyMarkerNames[i]);
                            if (markerId != FrameDataView.invalidMarkerId)
                                m_MarkerIdToCounterIndex[markerId] = i;
                        }

                        m_PanelMarkerTimes.Clear();
                        m_PanelOrder.Clear();
                        m_PanelsSeen.Clear();

                        CollectUpdaterSamplesFromHierarchy(hierarchyData, hierarchyData.GetRootItemID());
                        LoadPanelUpdateMetrics(rawFrameData);
                        UIToolkitProfilerToolbarHelpers.CollectEventCountsByPanel(rawFrameData, m_PanelEventCounts);

                        BuildRows(rawFrameData);
                    }
                }
            }

            // Always refresh — for no-frame / invalid-frame paths m_RootItems is empty, so totals
            // fall back to "0" / "—" naturally instead of keeping stale values from the previous frame.
            RefreshTotalsHeader();
            UpdateEmptyOverlay();
            ScheduleApplyTreeViewSource();
        }

        void UpdateEmptyOverlay()
        {
            if (m_EmptyOverlay != null)
                m_EmptyOverlay.style.display = m_RootItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Defer binding + <see cref="BaseVerticalCollectionView.Rebuild"/> to the next scheduler tick.
        /// Profiler frame changes can fire during layout (e.g. IMGUI toolbar); rebuilding the tree then throws
        /// <c>Cannot modify VisualElement hierarchy during layout calculation</c>.
        /// </summary>
        void ScheduleApplyTreeViewSource()
        {
            if (m_TreeView == null)
                return;

            if (m_TreeView.panel == null)
            {
                m_TreeView.SetRootItems(m_RootItems);
                m_TreeView.Rebuild();
                return;
            }

            if (m_ApplyTreeSourceScheduled == null)
                m_ApplyTreeSourceScheduled = m_TreeView.schedule.Execute(DeferredApplyTreeViewSource);
            else if (!m_ApplyTreeSourceScheduled.isActive)
                m_ApplyTreeSourceScheduled.Resume();
        }

        void DeferredApplyTreeViewSource()
        {
            if (m_TreeView == null || m_TreeView.panel == null)
                return;
            m_TreeView.SetRootItems(m_RootItems);
            m_TreeView.Rebuild();
            // Re-trigger selection so the right pane reflects the (possibly preserved) selection on the new frame.
            OnPanelRowSelectionChanged(m_TreeView.selectedIndices);
        }

        void CollectUpdaterSamplesFromHierarchy(HierarchyFrameDataView hierarchyData, int rootItemId)
        {
            m_HierarchyWalkStack.Clear();
            m_HierarchyWalkStack.Push(rootItemId);
            while (m_HierarchyWalkStack.Count > 0)
            {
                var itemId = m_HierarchyWalkStack.Pop();
                var itemMarkerId = hierarchyData.GetItemMarkerID(itemId);
                if (m_MarkerIdToCounterIndex.TryGetValue(itemMarkerId, out var markerIndex))
                {
                    var entityId = hierarchyData.GetItemEntityId(itemId);
                    var key = (entityId, markerIndex);
                    var timeMs = hierarchyData.GetItemColumnDataAsFloat(itemId, HierarchyFrameDataView.columnTotalTime);
                    if (m_PanelMarkerTimes.TryGetValue(key, out var existing))
                        m_PanelMarkerTimes[key] = existing + timeMs;
                    else
                    {
                        m_PanelMarkerTimes[key] = timeMs;
                        if (m_PanelsSeen.Add(entityId))
                            m_PanelOrder.Add(entityId);
                    }
                }

                if (!hierarchyData.HasItemChildren(itemId))
                    continue;

                m_ChildItemIdsScratch.Clear();
                hierarchyData.GetItemChildren(itemId, m_ChildItemIdsScratch);
                for (var i = m_ChildItemIdsScratch.Count - 1; i >= 0; i--)
                    m_HierarchyWalkStack.Push(m_ChildItemIdsScratch[i]);
            }
        }

        // Per-panel integer column from PANEL_METRICS. Renders "—" when the panel has no
        // PANEL_METRICS chunk in the current frame so an absent value isn't confused with a real zero.
        Column MakePanelCountColumn(string name, string title, string tooltip, Func<PanelDetailRow, long> getter)
        {
            var column = new Column
            {
                name = name,
                title = title,
                minWidth = 90,
                width = 110,
                makeHeader = UIToolkitProfilerToolbarHelpers.CreateDefaultColumnHeaderContent,
                bindCell = (e, index) =>
                {
                    var label = (Label)e;
                    var data = GetRowData(index);
                    if (!data.hasUpdateMetrics)
                        label.text = UIToolkitProfilerToolbarHelpers.NoDataCell;
                    else
                        label.text = getter(data).ToString("N0");
                },
                comparison = (a, b) =>
                {
                    var dataA = GetRowData(a);
                    var dataB = GetRowData(b);
                    // Keep "—" rows pinned to the bottom regardless of sort direction so a user
                    // sorting descending doesn't see the placeholders bubble to the top. The
                    // framework flips comparison results for descending sort, so we flip our
                    // NoData sign back to compensate.
                    if (dataA.hasUpdateMetrics != dataB.hasUpdateMetrics)
                    {
                        var naturalCompare = dataA.hasUpdateMetrics ? -1 : 1;
                        return IsColumnDescending(name) ? -naturalCompare : naturalCompare;
                    }
                    if (!dataA.hasUpdateMetrics)
                        return 0;
                    return getter(dataA).CompareTo(getter(dataB));
                },
            };
            column.bindHeader = ve => UIToolkitProfilerToolbarHelpers.BindColumnHeaderWithTooltip(ve, column, tooltip);
            return column;
        }

        bool IsColumnDescending(string columnName)
        {
            if (m_TreeView == null)
                return false;
            foreach (var sc in m_TreeView.sortedColumns)
                if (sc.columnName == columnName)
                    return sc.direction == SortDirection.Descending;
            return false;
        }

        void LoadPanelUpdateMetrics(RawFrameDataView rawFrameData)
        {
            m_PanelUpdateMetrics.Clear();
            if (rawFrameData == null || !rawFrameData.valid)
                return;
            var guid = ProfilerUIToolkit.kProfilerMetadataGuid;
            var tag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelMetrics;
            var chunkCount = rawFrameData.GetFrameMetaDataCount(guid, tag);
            for (var ci = 0; ci < chunkCount; ci++)
            {
                using (var chunk = rawFrameData.GetFrameMetaData<UIToolkitPanelUpdateMetricsInfo>(guid, tag, ci))
                {
                    if (chunk.Length < 1)
                        continue;
                    var info = chunk[0];
                    m_PanelUpdateMetrics[info.panelEntityId] = info;
                }
            }
        }

        float rowTimeMs(int index, int counterIndex)
        {
            var data = GetRowData(index);
            if (data.counterTimesMs == null || counterIndex < 0 || counterIndex >= data.counterTimesMs.Length)
                return 0f;
            return data.counterTimesMs[counterIndex];
        }

        void BuildRows(RawFrameDataView rawFrameData)
        {
            var nextId = 1;
            foreach (var panelEntityId in m_PanelOrder)
            {
                var panelName = UIToolkitProfilerToolbarHelpers.GetPanelDisplayName(rawFrameData, panelEntityId);
                var times = new float[m_Counters.Length];
                var total = 0f;
                for (var i = 0; i < m_Counters.Length; i++)
                {
                    m_PanelMarkerTimes.TryGetValue((panelEntityId, i), out times[i]);
                    total += times[i];
                }

                var hasUpdateMetrics = m_PanelUpdateMetrics.TryGetValue(panelEntityId, out var updateMetrics);
                m_PanelEventCounts.TryGetValue(panelEntityId, out var eventCount);

                m_RootItems.Add(new TreeViewItemData<PanelDetailRow>(nextId++, new PanelDetailRow
                {
                    panelEntityId = panelEntityId,
                    panelName = panelName,
                    counterTimesMs = times,
                    totalTimeMs = total,
                    hasUpdateMetrics = hasUpdateMetrics,
                    hierarchyVersionChanges = updateMetrics.hierarchyVersionChanges,
                    repaintVersionChanges = updateMetrics.repaintVersionChanges,
                    visualElementCount = updateMetrics.visualElementCount,
                    eventCount = eventCount,
                }));
            }
        }

        void RefreshTotalsHeader()
        {
            if (m_TreeView == null)
                return;

            Array.Clear(m_CounterTotalsScratch, 0, m_CounterTotalsScratch.Length);
            var totalTimeSum = 0f;
            ulong hierarchySum = 0, repaintSum = 0;
            long veSum = 0, eventSum = 0;
            var anyMetrics = false;
            for (var r = 0; r < m_RootItems.Count; r++)
            {
                var row = m_RootItems[r].data;
                for (var i = 0; i < m_CounterTotalsScratch.Length && i < row.counterTimesMs.Length; i++)
                    m_CounterTotalsScratch[i] += row.counterTimesMs[i];
                totalTimeSum += row.totalTimeMs;
                eventSum += row.eventCount;
                if (!row.hasUpdateMetrics)
                    continue;
                anyMetrics = true;
                hierarchySum += row.hierarchyVersionChanges;
                repaintSum += row.repaintVersionChanges;
                veSum += row.visualElementCount;
            }

            m_TreeView.SetTotalCell("panel", L10n.Tr("Total"));
            for (var i = 0; i < m_Counters.Length; i++)
                m_TreeView.SetTotalCell(m_Counters[i].Name, FormatTime(m_CounterTotalsScratch[i]));
            m_TreeView.SetTotalCell("total", FormatTime(totalTimeSum));
            m_TreeView.SetTotalCell("events", eventSum.ToString("N0"));
            // Same '—' convention as the per-row cells when no panel reported metrics this frame.
            m_TreeView.SetTotalCell("hierarchyChanges", anyMetrics ? hierarchySum.ToString("N0") : UIToolkitProfilerToolbarHelpers.NoDataCell);
            m_TreeView.SetTotalCell("repaintChanges", anyMetrics ? repaintSum.ToString("N0") : UIToolkitProfilerToolbarHelpers.NoDataCell);
            m_TreeView.SetTotalCell("veCount", anyMetrics ? veSum.ToString("N0") : UIToolkitProfilerToolbarHelpers.NoDataCell);
        }

        static string FormatTime(float timeMs) => $"{timeMs:F2} ms";
    }
}
