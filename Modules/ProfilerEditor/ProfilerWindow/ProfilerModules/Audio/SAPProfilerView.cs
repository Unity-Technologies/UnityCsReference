// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using TreeViewController = UnityEditor.IMGUI.Controls.TreeViewController<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewGUI = UnityEditor.IMGUI.Controls.TreeViewGUI<int>;
using TreeViewDataSource = UnityEditor.IMGUI.Controls.TreeViewDataSource<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditorInternal.Profiling
{
    /// <summary>
    /// Standard category names for SAP profiler display.
    /// Keep in sync with SAPProfilerCategories.h in native code.
    /// </summary>
    internal static class SAPProfilerCategory
    {
        public const string Generator = "Generator";
        public const string Effect = "Effect";
        public const string Listener = "Listener";
        public const string RootOutput = "RootOutput";
        public const string Other = "Other";

        /// <summary>
        /// Ordered list of categories for display grouping.
        /// Categories not in this list appear after these, sorted alphabetically.
        /// </summary>
        public static readonly string[] DisplayOrder = { Generator, Effect, Listener, RootOutput, Other };
    }

    /// <summary>
    /// Wrapper for SAPProfilerProcessorInfo with computed properties for display.
    /// </summary>
    internal class SAPProfilerProcessorInfoWrapper
    {
        public SAPProfilerProcessorInfo info;
        public string typeName;
        public string category;
        public string sourceName;
        public ulong dspBufferTimeNs;
        public int index;  // Original capture index, used for parent resolution
        public int sortedIndex;  // Index in sorted list, set during FetchData

        // Cached display strings (computed once at construction to avoid GC in OnContentGUI)
        public readonly string SampleRateString;
        public readonly string ProcessTimeString;
        public readonly string DspLoadString;
        public readonly double DspLoadPercent;

        public SAPProfilerProcessorInfoWrapper(SAPProfilerProcessorInfo info, string typeName, string category, string sourceName, ulong dspBufferTimeNs, int index)
        {
            this.info = info;
            this.typeName = typeName;
            this.category = category;
            this.sourceName = sourceName;
            this.dspBufferTimeNs = dspBufferTimeNs;
            this.index = index;

            // Pre-compute display strings
            SampleRateString = info.sampleRate > 0 ? $"{info.sampleRate} Hz" : "-";
            DspLoadPercent = dspBufferTimeNs > 0 ? (double)info.lastProcessTimeNs / dspBufferTimeNs * 100.0 : 0.0;

            if (info.lastProcessTimeNs == 0)
            {
                ProcessTimeString = "-";
                DspLoadString = "-";
            }
            else
            {
                double us = info.lastProcessTimeNs / 1000.0;
                ProcessTimeString = us < 1000.0 ? $"{us:F1} µs" : $"{us / 1000.0:F2} ms";
                DspLoadString = dspBufferTimeNs > 0 ? $"{DspLoadPercent:F1}%" : "-";
            }
        }

    }

    /// <summary>
    /// Column indices for the SAP Processors view.
    /// </summary>
    internal static class SAPProfilerProcessorHelper
    {
        public enum ColumnIndices
        {
            TypeName,
            Origin,
            SampleRate,
            ProcessTime,
            DspLoad,
            _LastColumn
        }

        public static int GetLastColumnIndex()
        {
            return (int)ColumnIndices._LastColumn - 1;
        }

        public static readonly string[] Headers = { "Type Name", "Origin", "Sample Rate", "Process Time", "DSP Load" };

        /// <summary>
        /// Default column widths for the processor view.
        /// Order must match ColumnIndices enum.
        /// </summary>
        public static class DefaultColumnWidths
        {
            public const float TypeName = 250f;
            public const float Origin = 200f;
            public const float SampleRate = 100f;
            public const float ProcessTime = 120f;
            public const float DspLoad = 100f;

            public static float[] Create() => new float[]
            {
                TypeName, Origin, SampleRate, ProcessTime, DspLoad
            };
        }

        internal class SAPProfilerProcessorComparer : IComparer<SAPProfilerProcessorInfoWrapper>
        {
            public ColumnIndices primarySortKey;
            public ColumnIndices secondarySortKey;
            public bool sortDescending;

            public SAPProfilerProcessorComparer() { }

            public SAPProfilerProcessorComparer(ColumnIndices primary, ColumnIndices secondary, bool descending)
            {
                primarySortKey = primary;
                secondarySortKey = secondary;
                sortDescending = descending;
            }

            public int Compare(SAPProfilerProcessorInfoWrapper a, SAPProfilerProcessorInfoWrapper b)
            {
                int result = CompareBy(a, b, primarySortKey);
                if (result == 0)
                    result = CompareBy(a, b, secondarySortKey);
                return sortDescending ? -result : result;
            }

            private int CompareBy(SAPProfilerProcessorInfoWrapper a, SAPProfilerProcessorInfoWrapper b, ColumnIndices key)
            {
                return key switch
                {
                    ColumnIndices.TypeName => string.Compare(a.typeName, b.typeName, StringComparison.Ordinal),
                    ColumnIndices.Origin => string.Compare(a.sourceName ?? "", b.sourceName ?? "", StringComparison.Ordinal),
                    ColumnIndices.SampleRate => a.info.sampleRate.CompareTo(b.info.sampleRate),
                    ColumnIndices.ProcessTime => a.info.lastProcessTimeNs.CompareTo(b.info.lastProcessTimeNs),
                    ColumnIndices.DspLoad => a.DspLoadPercent.CompareTo(b.DspLoadPercent),
                    _ => 0
                };
            }
        }
    }

    /// <summary>
    /// Data for a single DTM's processors.
    /// </summary>
    internal class SAPProfilerDTMData
    {
        public int dtmId;
        public int dataVersion;
        public List<SAPProfilerProcessorInfoWrapper> processors = new List<SAPProfilerProcessorInfoWrapper>();

        private SAPProfilerSummary? m_Summary;
        private string m_CachedHeaderLabel;
        private GUIContent m_CachedProfilerStatsContent;
        private bool m_CacheDirty = true;

        // Static tooltip strings to avoid allocations
        private const string kBaseTooltip =
            "DTM (DualThreadManager) communication stats since last profiler frame.\n\n" +
            "Batches: Command batches exchanged between control and realtime threads.\n" +
            "  - Sent: Control thread → Realtime thread (processor create/destroy, flush)\n" +
            "  - Returned: Realtime thread → Control thread (disposal confirmations)\n\n" +
            "Data: Bytes transferred via Pipe.SendData() for parameter updates.\n\n" +
            "These stats are typically zero during steady-state playback.\n" +
            "Activity occurs when creating/destroying processors or sending data.";

        private const string kTimeoutTooltip =
            "\n\n⚠ TIMEOUTS: Control thread timed out waiting for realtime thread.\n" +
            "This may indicate the audio thread is overloaded or blocked.";

        public SAPProfilerSummary? summary
        {
            get => m_Summary;
            set
            {
                m_Summary = value;
                m_CacheDirty = true;
            }
        }

        public void InvalidateCache()
        {
            m_CacheDirty = true;
        }

        public string GetHeaderLabel()
        {
            if (m_CacheDirty)
                RebuildCache();
            return m_CachedHeaderLabel;
        }

        public GUIContent GetProfilerStatsContent()
        {
            if (m_CacheDirty)
                RebuildCache();
            return m_CachedProfilerStatsContent;
        }

        void RebuildCache()
        {
            m_CacheDirty = false;

            // Build header label
            if (m_Summary.HasValue)
            {
                var s = m_Summary.Value;
                float bufferMs = s.sampleRate > 0 ? (float)s.dspBufferSize / s.sampleRate * 1000f : 0;
                // Only sum top-level processors to avoid double-counting nested processor time
                ulong totalTimeNs = 0;
                foreach (var p in processors)
                {
                    if (p.info.parentIndex < 0)
                        totalTimeNs += p.info.lastProcessTimeNs;
                }
                float totalDspLoad = s.DspBufferTimeNs > 0 ? (float)totalTimeNs / s.DspBufferTimeNs * 100f : 0;
                string dtmName = dtmId == 1 ? "Built-in" : $"DTM {dtmId}";
                m_CachedHeaderLabel = $"{dtmName} ({s.sampleRate} Hz, {s.dspBufferSize} samples, {bufferMs:F1} ms) - {totalDspLoad:F1}% DSP Load";
            }
            else
            {
                string dtmName = dtmId == 1 ? "Built-in" : $"DTM {dtmId}";
                m_CachedHeaderLabel = $"{dtmName} ({processors.Count} processors)";
            }

            // Build profiler stats content
            if (!m_Summary.HasValue)
            {
                m_CachedProfilerStatsContent = null;
                return;
            }

            var sum = m_Summary.Value;

            // Build label without List allocation
            string label;
            if (sum.flushTimeouts > 0)
            {
                label = $"Batches: {sum.batchesSubmitted} sent, {sum.batchesReturned} returned | " +
                        $"Data: {FormatBytes(sum.bytesSubmitted)} sent, {FormatBytes(sum.bytesReturned)} returned | " +
                        $"⚠ {sum.flushTimeouts} flush timeouts";
            }
            else
            {
                label = $"Batches: {sum.batchesSubmitted} sent, {sum.batchesReturned} returned | " +
                        $"Data: {FormatBytes(sum.bytesSubmitted)} sent, {FormatBytes(sum.bytesReturned)} returned";
            }

            // Use cached tooltip strings, update GUIContent in-place if possible
            string tooltip = sum.flushTimeouts > 0 ? kBaseTooltip + kTimeoutTooltip : kBaseTooltip;

            if (m_CachedProfilerStatsContent == null)
                m_CachedProfilerStatsContent = new GUIContent(label, tooltip);
            else
            {
                m_CachedProfilerStatsContent.text = label;
                m_CachedProfilerStatsContent.tooltip = tooltip;
            }
        }

        static string FormatBytes(ulong bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// Backend data provider for the SAP Processors view.
    /// </summary>
    internal class SAPProfilerProcessorViewBackend
    {
        public List<SAPProfilerProcessorInfoWrapper> items { get; private set; }
        public List<SAPProfilerSummary> summaries { get; private set; }
        public List<SAPProfilerDTMData> dtmDataList { get; private set; }

        public delegate void DataUpdateDelegate();
        public DataUpdateDelegate OnUpdate;
        public SAPProfilerProcessorTreeViewState m_TreeViewState;

        // Cached to avoid GC allocations
        private readonly Dictionary<int, SAPProfilerDTMData> m_DtmGroups = new Dictionary<int, SAPProfilerDTMData>();
        private readonly SAPProfilerProcessorHelper.SAPProfilerProcessorComparer m_Comparer = new SAPProfilerProcessorHelper.SAPProfilerProcessorComparer();

        public SAPProfilerProcessorViewBackend(SAPProfilerProcessorTreeViewState state)
        {
            m_TreeViewState = state;
            items = new List<SAPProfilerProcessorInfoWrapper>();
            summaries = new List<SAPProfilerSummary>();
            dtmDataList = new List<SAPProfilerDTMData>();
        }

        public void SetData(List<SAPProfilerProcessorInfoWrapper> data, List<SAPProfilerSummary> summaryData)
        {
            items = data;
            summaries = summaryData;
            RebuildDTMData();
            UpdateSorting();
        }

        void RebuildDTMData()
        {
            dtmDataList.Clear();

            // Clear processors from existing DTM entries (reuse instances to preserve references)
            foreach (var dtmData in m_DtmGroups.Values)
            {
                dtmData.processors.Clear();
                dtmData.summary = null;
            }

            // Create DTM entries from summaries first (DTM exists even without processors)
            foreach (var summary in summaries)
            {
                if (!m_DtmGroups.TryGetValue(summary.dtmIdentifier, out var dtmData))
                {
                    dtmData = new SAPProfilerDTMData { dtmId = summary.dtmIdentifier };
                    m_DtmGroups[summary.dtmIdentifier] = dtmData;
                }
                dtmData.summary = summary;
            }

            // Add processors to their DTMs
            foreach (var item in items)
            {
                int dtmId = item.info.dtmIdentifier;
                if (!m_DtmGroups.TryGetValue(dtmId, out var dtmData))
                {
                    dtmData = new SAPProfilerDTMData { dtmId = dtmId };
                    m_DtmGroups[dtmId] = dtmData;
                }
                dtmData.processors.Add(item);
            }

            // Invalidate caches, bump version, and collect active DTMs
            foreach (var dtmData in m_DtmGroups.Values)
            {
                // Only include DTMs that have data (summary or processors)
                if (dtmData.summary.HasValue || dtmData.processors.Count > 0)
                {
                    dtmData.InvalidateCache();
                    dtmData.dataVersion++;
                    dtmDataList.Add(dtmData);
                }
            }

            // Sort DTMs by ID (built-in first)
            dtmDataList.Sort((a, b) => a.dtmId.CompareTo(b.dtmId));
        }

        public void UpdateSorting()
        {
            // Sort processors within each DTM (reuse comparer to avoid allocations)
            m_Comparer.primarySortKey = (SAPProfilerProcessorHelper.ColumnIndices)m_TreeViewState.selectedColumn;
            m_Comparer.secondarySortKey = (SAPProfilerProcessorHelper.ColumnIndices)m_TreeViewState.prevSelectedColumn;
            m_Comparer.sortDescending = m_TreeViewState.sortByDescendingOrder;

            foreach (var dtmData in dtmDataList)
                dtmData.processors.Sort(m_Comparer);

            OnUpdate?.Invoke();
        }
    }

    /// <summary>
    /// Tree view state for the SAP Processors view.
    /// </summary>
    [Serializable]
    internal class SAPProfilerProcessorTreeViewState : TreeViewState
    {
        [SerializeField]
        public int selectedColumn = (int)SAPProfilerProcessorHelper.ColumnIndices.TypeName;
        [SerializeField]
        public int prevSelectedColumn = (int)SAPProfilerProcessorHelper.ColumnIndices.TypeName;
        [SerializeField]
        public bool sortByDescendingOrder = false;
        [SerializeField]
        public float[] columnWidths;
        [SerializeField]
        public List<int> dtmHeightIds = new List<int>();
        [SerializeField]
        public List<float> dtmHeights = new List<float>();

        public void SetSelectedColumn(int index)
        {
            if (index != selectedColumn)
                prevSelectedColumn = selectedColumn;
            else
                sortByDescendingOrder = !sortByDescendingOrder;
            selectedColumn = index;
        }

        public float GetDTMHeight(int dtmId, float defaultHeight)
        {
            int idx = dtmHeightIds.IndexOf(dtmId);
            if (idx >= 0 && idx < dtmHeights.Count)
                return dtmHeights[idx];
            return defaultHeight;
        }

        public void SetDTMHeight(int dtmId, float height)
        {
            int idx = dtmHeightIds.IndexOf(dtmId);
            if (idx >= 0 && idx < dtmHeights.Count)
            {
                dtmHeights[idx] = height;
            }
            else
            {
                dtmHeightIds.Add(dtmId);
                dtmHeights.Add(height);
            }
        }
    }

    /// <summary>
    /// View for a single DTM's processors.
    /// </summary>
    internal class SAPProfilerDTMView
    {
        private TreeViewController m_TreeView;
        private SAPProfilerDTMTreeViewDataSource m_DataSource;
        private SAPProfilerProcessorTreeViewState m_TreeViewState;
        private EditorWindow m_EditorWindow;
        private SAPProfilerDTMData m_DTMData;
        private int m_LastDataVersion;
        private float[] m_ColumnWidths;
        private bool m_Initialized;

        public SAPProfilerDTMData DTMData => m_DTMData;

        public SAPProfilerDTMView(EditorWindow editorWindow, SAPProfilerProcessorTreeViewState state, SAPProfilerDTMData dtmData, float[] columnWidths)
        {
            m_EditorWindow = editorWindow;
            m_TreeViewState = state;
            m_DTMData = dtmData;
            m_ColumnWidths = columnWidths;
        }

        public void Init(Rect rect)
        {
            if (m_Initialized)
                return;

            m_TreeView = new TreeViewController(m_EditorWindow, new TreeViewState());
            var treeViewGUI = new SAPProfilerProcessorViewGUI(m_TreeView) { columnWidths = m_ColumnWidths };
            m_DataSource = new SAPProfilerDTMTreeViewDataSource(m_TreeView, m_DTMData);
            m_TreeView.Init(rect, m_DataSource, treeViewGUI, null);
            m_Initialized = true;
        }

        public void UpdateData(SAPProfilerDTMData dtmData)
        {
            // Only rebuild tree when data actually changes (avoids rebuilding on every GUI event)
            if (m_DTMData == dtmData && m_LastDataVersion == dtmData.dataVersion)
                return;

            m_DTMData = dtmData;
            m_LastDataVersion = dtmData.dataVersion;
            if (m_DataSource != null && m_TreeView != null)
            {
                m_DataSource.SetDTMData(dtmData);
                m_TreeView.ReloadData();
            }
        }

        public void ForceRefresh()
        {
            if (m_TreeView != null)
                m_TreeView.ReloadData();
        }

        public float GetContentHeight()
        {
            if (m_TreeView == null || m_DataSource == null)
                return 0;
            return m_TreeView.gui.GetTotalSize().y;
        }

        public void OnGUI(Rect rect)
        {
            if (m_TreeView == null)
                return;
            m_TreeView.OnGUI(rect, 0);
        }
    }

    /// <summary>
    /// Main view class for SAP Processors in the Audio profiler.
    /// Manages multiple DTM views stacked vertically.
    /// </summary>
    internal class SAPProfilerProcessorView
    {
        private SAPProfilerProcessorTreeViewState m_TreeViewState;
        private EditorWindow m_EditorWindow;
        private SAPProfilerProcessorViewBackend m_Backend;
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_FoldoutStyle;
        private Dictionary<int, SAPProfilerDTMView> m_DTMViews = new Dictionary<int, SAPProfilerDTMView>();
        private Vector2 m_ScrollPosition;
        private float[] m_ColumnWidths;

        // Cached collections to avoid GC allocations in OnGUI
        private readonly HashSet<int> m_ExistingDTMIds = new HashSet<int>();
        private readonly List<int> m_ViewsToRemove = new List<int>();
        private const float kDTMTitleHeight = 20f;
        private const float kColumnHeaderHeight = 18f;
        private const float kDTMSpacing = 10f;
        private const float kMinDTMViewHeight = 150f;
        private const float kRowHeight = 16f;
        private const int kMinVisibleRows = 8;
        private const float kResizeHandleHeight = 6f;

        public int GetNumItemsInData()
        {
            return m_Backend?.items?.Count ?? 0;
        }

        public SAPProfilerProcessorView(EditorWindow editorWindow, SAPProfilerProcessorTreeViewState state)
        {
            m_EditorWindow = editorWindow;
            m_TreeViewState = state;
        }

        public void Init(Rect rect, SAPProfilerProcessorViewBackend backend)
        {
            if (m_HeaderStyle == null)
            {
                m_HeaderStyle = "OL title";
                m_FoldoutStyle = "Foldout";
            }

            m_Backend = backend;
            m_Backend.OnUpdate = OnDataUpdated;

            // Reset column widths if null, empty, or mismatched length (e.g., from old serialized layout)
            if (m_TreeViewState.columnWidths == null || m_TreeViewState.columnWidths.Length != (int)SAPProfilerProcessorHelper.ColumnIndices._LastColumn)
            {
                m_TreeViewState.columnWidths = SAPProfilerProcessorHelper.DefaultColumnWidths.Create();
                // Clear cached views so they get recreated with new column widths
                m_DTMViews.Clear();
            }
            m_ColumnWidths = m_TreeViewState.columnWidths;
        }

        void OnDataUpdated()
        {
            // Force refresh all DTM views when sorting changes
            foreach (var view in m_DTMViews.Values)
                view.ForceRefresh();
        }

        public void OnGUI(Rect rect)
        {
            if (m_Backend == null)
                return;

            // Content area with scroll
            Rect contentRect = rect;

            // Calculate total content height
            float totalHeight = 0;
            float minHeight = Mathf.Max(kMinDTMViewHeight, kMinVisibleRows * kRowHeight);
            foreach (var dtmData in m_Backend.dtmDataList)
            {
                totalHeight += kDTMTitleHeight + kColumnHeaderHeight;
                // Add profiler stats line height if it will be shown
                if (dtmData.GetProfilerStatsContent() != null)
                    totalHeight += kDTMTitleHeight;
                float storedHeight = m_TreeViewState.GetDTMHeight(dtmData.dtmId, 0);
                if (storedHeight > 0)
                    totalHeight += Mathf.Max(storedHeight, minHeight);
                else if (m_DTMViews.TryGetValue(dtmData.dtmId, out var view))
                    totalHeight += Mathf.Max(view.GetContentHeight(), minHeight);
                else
                    totalHeight += minHeight; // Estimate for uninitialized views
                totalHeight += kResizeHandleHeight + kDTMSpacing;
            }

            // Calculate total column width for horizontal scrolling
            float totalColumnWidth = 0;
            foreach (var w in m_ColumnWidths)
                totalColumnWidth += w;
            
            // viewRect width should be the max of visible area and total column width
            float viewWidth = Mathf.Max(contentRect.width - 14, totalColumnWidth);
            Rect viewRect = new Rect(0, 0, viewWidth, totalHeight);
            m_ScrollPosition = GUI.BeginScrollView(contentRect, m_ScrollPosition, viewRect);

            float yOffset = 0;
            foreach (var dtmData in m_Backend.dtmDataList)
            {
                // DTM title (simple label)
                Rect titleRect = new Rect(0, yOffset, viewRect.width, kDTMTitleHeight);
                EditorGUI.LabelField(titleRect, dtmData.GetHeaderLabel(), EditorStyles.boldLabel);
                yOffset += kDTMTitleHeight;

                // Profiler stats line (if available)
                var profilerStatsContent = dtmData.GetProfilerStatsContent();
                if (profilerStatsContent != null)
                {
                    Rect profilerStatsRect = new Rect(10, yOffset, viewRect.width - 10, kDTMTitleHeight);
                    EditorGUI.LabelField(profilerStatsRect, profilerStatsContent, EditorStyles.miniLabel);
                    yOffset += kDTMTitleHeight;
                }

                // Column header for this DTM's table
                Rect columnHeaderRect = new Rect(0, yOffset, viewRect.width, kColumnHeaderHeight);
                DrawColumnHeader(columnHeaderRect);
                yOffset += kColumnHeaderHeight;

                // Get or create DTM view
                if (!m_DTMViews.TryGetValue(dtmData.dtmId, out var dtmView))
                {
                    dtmView = new SAPProfilerDTMView(m_EditorWindow, m_TreeViewState, dtmData, m_ColumnWidths);
                    m_DTMViews[dtmData.dtmId] = dtmView;
                }

                // Must init before getting height or updating data
                Rect initRect = new Rect(0, yOffset, viewRect.width, 100);
                dtmView.Init(initRect);
                dtmView.UpdateData(dtmData);

                // Use stored height if available, otherwise use content height
                float storedHeight = m_TreeViewState.GetDTMHeight(dtmData.dtmId, 0);
                float viewHeight;
                if (storedHeight > 0)
                    viewHeight = storedHeight;
                else
                    viewHeight = dtmView.GetContentHeight();
                
                // Ensure a reasonable minimum height for the TreeView
                if (viewHeight < minHeight) viewHeight = minHeight;

                Rect dtmRect = new Rect(0, yOffset, viewRect.width, viewHeight);
                dtmView.OnGUI(dtmRect);
                yOffset += viewHeight;

                // Draw resize handle
                Rect resizeRect = new Rect(0, yOffset, viewRect.width, kResizeHandleHeight);
                EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeVertical);
                
                // Handle resize drag
                float deltaY = EditorGUI.MouseDeltaReader(resizeRect, true).y;
                if (deltaY != 0f)
                {
                    float newHeight = Mathf.Max(viewHeight + deltaY, minHeight);
                    m_TreeViewState.SetDTMHeight(dtmData.dtmId, newHeight);
                    m_EditorWindow?.Repaint();
                }

                // Draw a subtle line for the resize handle
                if (Event.current.type == EventType.Repaint)
                {
                    Color oldColor = GUI.color;
                    GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                    GUI.DrawTexture(new Rect(resizeRect.x + 20, resizeRect.y + 2, resizeRect.width - 40, 2), EditorGUIUtility.whiteTexture);
                    GUI.color = oldColor;
                }

                yOffset += kResizeHandleHeight + kDTMSpacing;
            }

            GUI.EndScrollView();

            // Remove views for DTMs that no longer exist
            m_ExistingDTMIds.Clear();
            foreach (var d in m_Backend.dtmDataList)
                m_ExistingDTMIds.Add(d.dtmId);
            m_ViewsToRemove.Clear();
            foreach (var id in m_DTMViews.Keys)
            {
                if (!m_ExistingDTMIds.Contains(id))
                    m_ViewsToRemove.Add(id);
            }
            foreach (var id in m_ViewsToRemove)
                m_DTMViews.Remove(id);
        }

        void DrawColumnHeader(Rect rect)
        {
            GUI.BeginClip(rect);
            const float dragAreaWidth = 3f;
            float dragWidth = 6f;
            float columnPos = 0;  // No manual scroll offset needed - we're inside the scroll view
            int lastColumnIndex = SAPProfilerProcessorHelper.GetLastColumnIndex();

            for (int i = 0; i <= lastColumnIndex; ++i)
            {
                Rect columnRect = new Rect(columnPos, 0, m_ColumnWidths[i], rect.height - 1);
                columnPos += m_ColumnWidths[i];
                Rect dragRect = new Rect(columnPos - dragWidth / 2, 0, dragAreaWidth, rect.height);
                float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
                if (deltaX != 0f)
                {
                    m_ColumnWidths[i] += deltaX;
                    m_ColumnWidths[i] = Mathf.Max(m_ColumnWidths[i], 10f);
                }

                string title = SAPProfilerProcessorHelper.Headers[i];
                if (i == m_TreeViewState.selectedColumn)
                    title += m_TreeViewState.sortByDescendingOrder ? " ▼" : " ▲";

                GUI.Box(columnRect, title, m_HeaderStyle);
                if (Event.current.type == EventType.MouseDown && columnRect.Contains(Event.current.mousePosition))
                {
                    m_TreeViewState.SetSelectedColumn(i);
                    m_Backend.UpdateSorting();
                }

                if (Event.current.type == EventType.Repaint)
                    EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);
            }
            GUI.EndClip();
        }
    }

    /// <summary>
    /// TreeView data source for a single DTM.
    /// </summary>
    internal class SAPProfilerDTMTreeViewDataSource : TreeViewDataSource
    {
        private SAPProfilerDTMData m_DTMData;

        // Cached collections to avoid GC allocations in FetchData
        private readonly Dictionary<int, int> m_OriginalIndexToSortedIndex = new Dictionary<int, int>();

        private readonly Dictionary<string, List<SAPProfilerProcessorInfoWrapper>> m_CategoryGroups = new Dictionary<string, List<SAPProfilerProcessorInfoWrapper>>();
        private readonly HashSet<string> m_ProcessedCategories = new HashSet<string>();

        // Stable hash function for category names (string.GetHashCode is not stable across runs)
        private static int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash = 5381;
                foreach (char c in str)
                    hash = ((hash << 5) + hash) ^ c;
                return hash & 0x7FFFFFFF;
            }
        }

        public SAPProfilerDTMTreeViewDataSource(TreeViewController treeView, SAPProfilerDTMData dtmData)
            : base(treeView)
        {
            m_DTMData = dtmData;
            showRootItem = false;
            rootIsCollapsable = false;
            FetchData();
            // Ensure hidden root is expanded so its children (categories) are visible
            SetExpanded(m_RootItem, true);
        }

        public void SetDTMData(SAPProfilerDTMData dtmData)
        {
            m_DTMData = dtmData;
            m_NeedRefreshRows = true;
        }

        public override bool IsRenamingItemAllowed(TreeViewItem item)
        {
            return false;
        }

        public override void FetchData()
        {
            m_RootItem = new TreeViewItem(0, -1, null, "Root");
            m_RootItem.children = new List<TreeViewItem>();

            if (m_DTMData == null || m_DTMData.processors.Count == 0)
                return;

            var processors = m_DTMData.processors;
            ulong dspBufferTimeNs = m_DTMData.summary?.DspBufferTimeNs ?? 0;

            // Clear cached collections (clear lists inside dictionary to reuse allocations)
            m_OriginalIndexToSortedIndex.Clear();
            foreach (var list in m_CategoryGroups.Values)
                list.Clear();
            m_ProcessedCategories.Clear();

            // Build mapping from original index to sorted index, and set sortedIndex on wrappers
            for (int i = 0; i < processors.Count; i++)
            {
                processors[i].sortedIndex = i;
                m_OriginalIndexToSortedIndex[processors[i].index] = i;
            }

            // Build tree items for all processors
            var treeItems = new SAPProfilerProcessorViewItem[processors.Count];
            for (int i = 0; i < processors.Count; i++)
            {
                // Use handle bits as stable ID (survives processor creation/destruction)
                // Mask off sign bit to ensure positive value (DTM ID in upper bits can set MSB)
                int stableId = (int)(processors[i].info.handleBits & 0x7FFFFFFF);
                treeItems[i] = new SAPProfilerProcessorViewItem(stableId, 0, null, processors[i].typeName, processors[i]);
            }

            // Group only ROOT-LEVEL processors by category
            // Nested processors are handled separately via parent-child linking
            foreach (var proc in processors)
            {
                bool isRootLevel = proc.info.parentIndex < 0 ||
                                  !m_OriginalIndexToSortedIndex.ContainsKey(proc.info.parentIndex);
                if (!isRootLevel)
                    continue;

                var cat = string.IsNullOrEmpty(proc.category) ? SAPProfilerCategory.Other : proc.category;
                if (!m_CategoryGroups.TryGetValue(cat, out var list))
                {
                    list = new List<SAPProfilerProcessorInfoWrapper>();
                    m_CategoryGroups[cat] = list;
                }
                list.Add(proc);
            }

            // Use negative IDs for categories to avoid collision with processor IDs (which are positive)

            // Process categories in defined order, then remaining
            foreach (var cat in SAPProfilerCategory.DisplayOrder)
            {
                if (!m_CategoryGroups.TryGetValue(cat, out var catList) || catList.Count == 0 || m_ProcessedCategories.Contains(cat))
                    continue;
                int categoryId = -1 - (GetStableHashCode(cat) & 0x7FFFFFFF) % 100000;
                AddCategoryNode(cat, catList, dspBufferTimeNs, categoryId, treeItems);
                m_ProcessedCategories.Add(cat);
            }

            foreach (var kvp in m_CategoryGroups)
            {
                if (kvp.Value.Count == 0 || m_ProcessedCategories.Contains(kvp.Key))
                    continue;
                int categoryId = -1 - (GetStableHashCode(kvp.Key) & 0x7FFFFFFF) % 100000;
                AddCategoryNode(kvp.Key, kvp.Value, dspBufferTimeNs, categoryId, treeItems);
                m_ProcessedCategories.Add(kvp.Key);
            }

            // Handle nested processors - link them to their parent tree items
            foreach (var wrapper in processors)
            {
                if (wrapper.info.parentIndex >= 0 && m_OriginalIndexToSortedIndex.TryGetValue(wrapper.info.parentIndex, out int sortedParentIndex))
                {
                    var treeItem = treeItems[wrapper.sortedIndex];
                    var parentTreeItem = treeItems[sortedParentIndex];

                    treeItem.parent = parentTreeItem;
                    if (parentTreeItem.children == null)
                        parentTreeItem.children = new List<TreeViewItem>();
                    parentTreeItem.children.Add(treeItem);
                }
            }

            // Calculate depths
            SetDepthRecursive(m_RootItem, -1);

            // Ensure hidden root stays expanded (categories are its children)
            SetExpanded(m_RootItem, true);

            m_NeedRefreshRows = true;
        }

        void AddCategoryNode(
            string category,
            List<SAPProfilerProcessorInfoWrapper> rootProcessors,
            ulong dspBufferTimeNs,
            int categoryId,
            SAPProfilerProcessorViewItem[] treeItems)
        {
            // Calculate category totals from root processors (they already exclude nested)
            ulong totalTimeNs = 0;
            foreach (var p in rootProcessors)
                totalTimeNs += p.info.lastProcessTimeNs;

            var summaryInfo = new SAPProfilerProcessorInfo { lastProcessTimeNs = totalTimeNs, parentIndex = -1 };
            var summaryWrapper = new SAPProfilerProcessorInfoWrapper(summaryInfo, $"{category}s ({rootProcessors.Count})", category, "", dspBufferTimeNs, -1);
            var categoryItem = new SAPProfilerProcessorViewItem(categoryId, 0, m_RootItem, summaryWrapper.typeName, summaryWrapper, isCategorySummary: true);
            categoryItem.children = new List<TreeViewItem>();
            m_RootItem.children.Add(categoryItem);

            // Add root processors to category (use sortedIndex set during iteration)
            foreach (var wrapper in rootProcessors)
            {
                var treeItem = treeItems[wrapper.sortedIndex];
                treeItem.parent = categoryItem;
                categoryItem.children.Add(treeItem);
            }
        }

        private void SetDepthRecursive(TreeViewItem item, int depth)
        {
            item.depth = depth;
            if (item.children != null)
            {
                foreach (var child in item.children)
                    SetDepthRecursive(child, depth + 1);
            }
        }

        public override bool CanBeParent(TreeViewItem item)
        {
            return item.hasChildren;
        }
    }

    internal class SAPProfilerProcessorViewItem : TreeViewItem
    {
        public SAPProfilerProcessorInfoWrapper info;
        public bool isCategorySummary;

        public SAPProfilerProcessorViewItem(int id, int depth, TreeViewItem parent, string displayName, SAPProfilerProcessorInfoWrapper info, bool isCategorySummary = false)
            : base(id, depth, parent, displayName)
        {
            this.info = info;
            this.isCategorySummary = isCategorySummary;
        }
    }

    internal class SAPProfilerProcessorViewGUI : TreeViewGUI
    {
        public float[] columnWidths { get; set; }

        public SAPProfilerProcessorViewGUI(TreeViewController treeView)
            : base(treeView)
        {
            k_IconWidth = 0;
        }

        protected override Texture GetIconForItem(TreeViewItem item)
        {
            return null;
        }

        protected override void RenameEnded()
        {
        }

        protected override void SyncFakeItem()
        {
        }

        public override Vector2 GetTotalSize()
        {
            Vector2 size = base.GetTotalSize();
            if (columnWidths != null)
            {
                size.x = 0;
                foreach (var c in columnWidths)
                    size.x += c;
            }
            return size;
        }

        protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var sapItem = item as SAPProfilerProcessorViewItem;
            if (sapItem == null)
                return;

            var info = sapItem.info;
            // Use bold for category summary rows
            bool useBold = useBoldFont || sapItem.isCategorySummary;
            GUIStyle lineStyle = useBold ? Styles.lineBoldStyle : Styles.lineStyle;
            var orgAlignment = lineStyle.alignment;
            var orgPaddingLeft = lineStyle.padding.left;
            lineStyle.alignment = TextAnchor.MiddleLeft;
            lineStyle.padding.left = 0;

            int margin = 2;

            // First column uses base implementation for foldout/indent
            base.OnContentGUI(new Rect(rect.x, rect.y, columnWidths[0] - margin, rect.height), row, item, info.typeName, selected, focused, useBold, isPinging);

            // Draw remaining columns
            rect.x += columnWidths[0] + margin;
            for (int i = 1; i < columnWidths.Length; i++)
            {
                rect.width = columnWidths[i] - 2 * margin;

                string text;
                if (sapItem.isCategorySummary)
                {
                    // Category summaries only show timing data, not per-processor settings
                    text = i switch
                    {
                        (int)SAPProfilerProcessorHelper.ColumnIndices.Origin => "",
                        (int)SAPProfilerProcessorHelper.ColumnIndices.SampleRate => "",
                        (int)SAPProfilerProcessorHelper.ColumnIndices.ProcessTime => info.ProcessTimeString,
                        (int)SAPProfilerProcessorHelper.ColumnIndices.DspLoad => info.DspLoadString,
                        _ => ""
                    };
                }
                else
                {
                    text = i switch
                    {
                        (int)SAPProfilerProcessorHelper.ColumnIndices.Origin => string.IsNullOrEmpty(info.sourceName) ? "-" : info.sourceName,
                        (int)SAPProfilerProcessorHelper.ColumnIndices.SampleRate => info.SampleRateString,
                        (int)SAPProfilerProcessorHelper.ColumnIndices.ProcessTime => info.ProcessTimeString,
                        (int)SAPProfilerProcessorHelper.ColumnIndices.DspLoad => info.DspLoadString,
                        _ => ""
                    };
                }

                lineStyle.Draw(rect, text, false, false, selected, focused);
                rect.x += columnWidths[i];
            }

            lineStyle.alignment = orgAlignment;
            lineStyle.padding.left = orgPaddingLeft;
        }
    }
}
