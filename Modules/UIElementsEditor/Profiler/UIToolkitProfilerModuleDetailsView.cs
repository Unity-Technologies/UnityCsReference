// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        const string k_DetailsSplitModeEditorPrefKey = "UIToolkitProfilerModule.DetailsSplitMode";
        const string k_ViewDataKeyPrefix = "uitoolkit-profiler";

        readonly ProfilerCounterDescriptor[] m_Counters;
        readonly string[] m_HierarchyMarkerNames;
        readonly string[] m_ColumnDisplayTitles;
        MultiColumnListView m_ListView;
        List<PanelDetailRow> m_Rows = new List<PanelDetailRow>();
        readonly List<int> m_ChildItemIdsScratch = new List<int>(64);
        readonly Stack<int> m_HierarchyWalkStack = new Stack<int>(256);
        IVisualElementScheduledItem m_ApplyListSourceScheduled;

        // Reusable collections for ReloadData; cleared each frame instead of re-allocated.
        readonly Dictionary<int, int> m_MarkerIdToCounterIndex = new Dictionary<int, int>();
        readonly Dictionary<(EntityId, int), float> m_PanelMarkerTimes = new Dictionary<(EntityId, int), float>();
        readonly List<EntityId> m_PanelOrder = new List<EntityId>();
        readonly HashSet<EntityId> m_PanelsSeen = new HashSet<EntityId>();

        PanelComponentsPaneController m_PanelComponentsPane;

        internal struct PanelDetailRow
        {
            public EntityId panelEntityId;
            public string panelName;
            public float[] counterTimesMs;
            public float totalTimeMs;
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
                comparison = (a, b) => EntityId.ToULong(m_Rows[a].panelEntityId).CompareTo(EntityId.ToULong(m_Rows[b].panelEntityId)),
                bindCell = (e, row) => ((Label)e).text = row < m_Rows.Count ? m_Rows[row].panelName : ""
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
                    comparison = (a, b) =>
                    {
                        var ta = rowTimeMs(a, counterIndex);
                        var tb = rowTimeMs(b, counterIndex);
                        return ta.CompareTo(tb);
                    },
                    bindCell = (e, row) =>
                    {
                        var label = (Label)e;
                        if (row < m_Rows.Count && counterIndex < m_Rows[row].counterTimesMs.Length)
                            label.text = FormatTime(m_Rows[row].counterTimesMs[counterIndex]);
                        else
                            label.text = "—";
                    }
                };
                column.bindHeader = (ve) => UIToolkitProfilerToolbarHelpers.BindColumnHeaderWithTooltip(ve, column, headerTooltip);
                columns.Add(column);
            }

            columns.Add(new Column
            {
                name = "total",
                title = L10n.Tr("Total"),
                minWidth = 80,
                comparison = (a, b) => m_Rows[a].totalTimeMs.CompareTo(m_Rows[b].totalTimeMs),
                bindCell = (e, row) =>
                {
                    var label = (Label)e;
                    label.text = row < m_Rows.Count ? FormatTime(m_Rows[row].totalTimeMs) : "—";
                }
            });

            m_ListView = new MultiColumnListView(columns)
            {
                fixedItemHeight = 18,
                reorderable = true,
                sortingMode = ColumnSortingMode.Default,
                viewDataKey = "uitoolkit-profiler-details-listview",
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                style = { flexGrow = 1 }
            };

            m_ListView.makeNoneElement = () =>
            {
                var label = new Label(L10n.Tr("No data to show. Start profiling UI Toolkit content to see details."));
                label.style.flexGrow = 1;
                return label;
            };

            m_ListView.columnSortingChanged += OnColumnSortingChanged;
            m_ListView.itemsChosen += OnItemsChosen;
            m_ListView.selectedIndicesChanged += OnPanelRowSelectionChanged;

            var mainToolbar = new Toolbar();
            mainToolbar.Add(new ToolbarSpacer { flex = true });
            UIToolkitProfilerToolbarHelpers.AddCommonButtons(mainToolbar);

            m_PanelComponentsPane = new PanelComponentsPaneController(k_DetailsSplitModeEditorPrefKey, k_ViewDataKeyPrefix);
            var splitView = m_PanelComponentsPane.WireUp(m_ListView, mainToolbar);

            var root = new VisualElement { name = "uitoolkit-profiler-details-root" };
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.Add(mainToolbar);
            root.Add(splitView);

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            return root;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_ListView != null)
            {
                m_ListView.itemsChosen -= OnItemsChosen;
                m_ListView.columnSortingChanged -= OnColumnSortingChanged;
                m_ListView.selectedIndicesChanged -= OnPanelRowSelectionChanged;
            }
            m_PanelComponentsPane?.Dispose();
            m_PanelComponentsPane = null;
            m_ApplyListSourceScheduled?.Pause();
            m_ApplyListSourceScheduled = null;
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnColumnSortingChanged()
        {
            ScheduleApplyListViewSource();
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
                if (index >= 0 && index < m_Rows.Count)
                {
                    m_PanelComponentsPane.SetSelectedPanel(m_Rows[index].panelEntityId, m_Rows[index].panelName);
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
            m_Rows.Clear();

            if (frameIndex < 0)
            {
                m_PanelComponentsPane?.LoadFrameMetadata(null, -1);
                ApplyListViewSource();
                return;
            }

            using (var hierarchyData = ProfilerDriver.GetHierarchyFrameDataView(
                (int)frameIndex, k_MainThreadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnDontSort, false))
            using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
            {
                m_PanelComponentsPane?.LoadFrameMetadata(rawFrameData, frameIndex);

                if (!hierarchyData.valid)
                {
                    ApplyListViewSource();
                    return;
                }

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

                BuildRows(rawFrameData);
            }

            ApplyListViewSource();
        }

        void ApplyListViewSource()
        {
            ScheduleApplyListViewSource();
        }

        /// <summary>
        /// Defer binding + <see cref="BaseVerticalCollectionView.Rebuild"/> to the next scheduler tick.
        /// Profiler frame changes can fire during layout (e.g. IMGUI toolbar); rebuilding the list then throws
        /// <c>Cannot modify VisualElement hierarchy during layout calculation</c>.
        /// </summary>
        void ScheduleApplyListViewSource()
        {
            if (m_ListView == null)
                return;

            if (m_ListView.panel == null)
            {
                m_ListView.itemsSource = m_Rows;
                m_ListView.Rebuild();
                return;
            }

            if (m_ApplyListSourceScheduled == null)
                m_ApplyListSourceScheduled = m_ListView.schedule.Execute(DeferredApplyListViewSource);
            else if (!m_ApplyListSourceScheduled.isActive)
                m_ApplyListSourceScheduled.Resume();
        }

        void DeferredApplyListViewSource()
        {
            if (m_ListView == null || m_ListView.panel == null)
                return;
            m_ListView.itemsSource = m_Rows;
            m_ListView.Rebuild();
            // Re-trigger selection so the right pane reflects the (possibly preserved) selection on the new frame.
            OnPanelRowSelectionChanged(m_ListView.selectedIndices);
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

        float rowTimeMs(int row, int counterIndex)
        {
            if (row < 0 || row >= m_Rows.Count || counterIndex >= m_Rows[row].counterTimesMs.Length)
                return 0f;
            return m_Rows[row].counterTimesMs[counterIndex];
        }

        void BuildRows(RawFrameDataView rawFrameData)
        {
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

                m_Rows.Add(new PanelDetailRow
                {
                    panelEntityId = panelEntityId,
                    panelName = panelName,
                    counterTimesMs = times,
                    totalTimeMs = total
                });
            }
        }

        static string FormatTime(float timeMs) => $"{timeMs:F2} ms";
    }
}
