// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    // ViewController that displayes marker instances in a frame.
    class MarkerInstancesDetailsViewController : ViewController
    {
        const string k_UxmlResourceName = "MarkerInstancesDetailsView.uxml";
        const string k_UssClass_Dark = "marker-instances-details-view__dark";
        const string k_UssClass_Light = "marker-instances-details-view__light";
        const string k_ColumnName_Name = "name";
        const string k_ColumnName_ThreadName = "thread-name";
        const string k_ColumnName_Time = "time";
        const string k_ColumnName_TotalPercent = "time-percent";
        const string k_ColumnName_GcAlloc = "gc-alloc";
        const string k_ColumnName_GcAllocPercent = "gc-alloc-percent";

        const string k_GCAllocMarkerName = "GC.Alloc";
        const string k_GCAllocInFrameMarkerName = "GC Allocated In Frame";
        const string k_ListViewPersistencyKey = "marker-instances-details-view__list-view__state";

        static class Content
        {
            // Column titles and tooltips
            public static readonly string k_ColumnTitle_Name = L10n.Tr("Object");
            public static readonly string k_ColumnTitle_ThreadName = L10n.Tr("Thread");
            public static readonly string k_ColumnTitle_Time = L10n.Tr("Time, ms");
            public static readonly string k_ColumnTitle_TotalPercent = L10n.Tr("Total %");
            public static readonly string k_ColumnTitle_GcAlloc = L10n.Tr("GC Alloc");
            public static readonly string k_ColumnTitle_GcAllocPercent = L10n.Tr("GC Alloc %");

            public static readonly string k_ColumnTooltip_Name = L10n.Tr("Name of the object associated with the marker instance");
            public static readonly string k_ColumnTooltip_ThreadName = L10n.Tr("Name of the thread where the marker instance executed");
            public static readonly string k_ColumnTooltip_Time = L10n.Tr("Total time spent in this marker instance");
            public static readonly string k_ColumnTooltip_TotalPercent = L10n.Tr("Percentage of the total frame time spent in this marker instance");
            public static readonly string k_ColumnTooltip_GcAlloc = L10n.Tr("Total GC memory allocated by this marker instance excluding child allocations");
            public static readonly string k_ColumnTooltip_GcAllocPercent = L10n.Tr("Percentage of total frame GC allocations by this marker instance");
        }

        // Model
        readonly IProfilerCaptureDataService m_DataService;
        
        struct SampleInformation
        {
            public string ThreadName;
            public string Name;
            public float TotalTime;
            public float TotalTimePercent;
            public long GcAlloc;
            public float GcAllocPercent;
        }
        
        List<SampleInformation> m_Instances = new List<SampleInformation>();
        CancellationTokenSource m_BuildModelCancellation;
        bool m_IsGcAllocMode;

        // View
        MultiColumnListView m_ListView;
        VisualElement m_ActivityOverlay;

        public MarkerInstancesDetailsViewController(IProfilerCaptureDataService dataService)
        {
            m_DataService = dataService;
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeClass = EditorGUIUtility.isProSkin ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            SetupListView();
            RefreshView();
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_ListView = view.Q<MultiColumnListView>("marker-instances-details-view__list-view");
            m_ActivityOverlay = view.Q<VisualElement>("marker-instances-details-view__activity-overlay");
        }

        void SetupListView()
        {
            m_ListView.viewDataKey = k_ListViewPersistencyKey;

            ConfigureColumn(k_ColumnName_Name,           Content.k_ColumnTitle_Name,           Content.k_ColumnTooltip_Name,           true,  MakeNameCell, BindNameCell);
            ConfigureColumn(k_ColumnName_ThreadName,     Content.k_ColumnTitle_ThreadName,     Content.k_ColumnTooltip_ThreadName,     true,  MakeNameCell, BindThreadNameCell);
            ConfigureColumn(k_ColumnName_Time,           Content.k_ColumnTitle_Time,           Content.k_ColumnTooltip_Time,           false, MakeCell,     BindTimeCell);
            ConfigureColumn(k_ColumnName_TotalPercent,   Content.k_ColumnTitle_TotalPercent,   Content.k_ColumnTooltip_TotalPercent,   false, MakeCell,     BindPercentCell);
            ConfigureColumn(k_ColumnName_GcAlloc,        Content.k_ColumnTitle_GcAlloc,        Content.k_ColumnTooltip_GcAlloc,        false, MakeCell,     BindGcAllocCell);
            ConfigureColumn(k_ColumnName_GcAllocPercent, Content.k_ColumnTitle_GcAllocPercent, Content.k_ColumnTooltip_GcAllocPercent, false, MakeCell,     BindGcAllocPercentCell);

            m_ListView.columnSortingChanged += OnSortingChanged;
            m_ListView.selectionChanged += OnSelectionChanged;

            m_ListView.itemsSource = m_Instances;
            m_ListView.Rebuild();
        }

        void ConfigureColumn(string columnName, string title, string tooltip, bool optional, Func<VisualElement> makeCell, Action<VisualElement, int> bindCell)
        {
            Column column = m_ListView.columns[columnName];
            column.title = title;
            column.optional = optional;
            column.makeHeader = MakeHeaderLabel;
            column.bindHeader = e => BindHeaderLabel(e, title, tooltip);
            column.makeCell = makeCell;
            column.bindCell = bindCell;
        }

        VisualElement MakeHeaderLabel()
        {
            var label = new Label();
            label.style.flexGrow = 1;
            label.style.marginLeft = 4;
            label.style.marginRight = 4;
            return label;
        }

        void BindHeaderLabel(VisualElement element, string title, string tooltip)
        {
            var label = element as Label;
            label.text = title;
            label.tooltip = tooltip;
        }

        VisualElement MakeNameCell()
        {
            return MakeLabelCell(TextAnchor.MiddleLeft);
        }

        VisualElement MakeCell()
        {
            return MakeLabelCell(TextAnchor.MiddleRight);
        }

        VisualElement MakeLabelCell(TextAnchor alignment)
        {
            var label = new Label();
            label.style.unityTextAlign = alignment;
            label.style.flexGrow = 1;
            label.style.marginLeft = 4;
            label.style.marginRight = 4;
            return label;
        }

        void BindNameCell(VisualElement element, int index)
        {
            BindCell(element, index, (label, obj) => label.text = obj.Name ?? "N/A");
        }

        void BindThreadNameCell(VisualElement element, int index)
        {
            BindCell(element, index, (label, obj) => label.text = obj.ThreadName ?? "N/A");
        }

        void BindTimeCell(VisualElement element, int index)
        {
            BindCell(element, index, (label, obj) => label.text = $"{obj.TotalTime:F2}");
        }

        void BindPercentCell(VisualElement element, int index)
        {
            BindCell(element, index, (label, obj) => label.text = $"{obj.TotalTimePercent:F1}%");
        }

        void BindGcAllocCell(VisualElement element, int index)
        {
            BindCell(element, index, (label, obj) => label.text = obj.GcAlloc > 0 ? EditorUtility.FormatBytes(obj.GcAlloc) : "0 B");
        }

        void BindGcAllocPercentCell(VisualElement element, int index)
        {
            BindCell(element, index, (label, obj) => label.text = $"{obj.GcAllocPercent:F1}%");
        }

        void BindCell(VisualElement element, int index, Action<Label, SampleInformation> setText)
        {
            if (index < 0 || index >= m_Instances.Count)
                return;

            setText(element as Label, m_Instances[index]);
        }

        void OnSortingChanged()
        {
            ApplySorting(m_IsGcAllocMode);
            RebuildListView();
        }

        void ApplySorting(bool isGcAllocMode)
        {
            if (m_Instances.Count == 0)
                return;

            // Get the first sorted column (primary sort)
            SortColumnDescription sortedColumn = default;
            var sortedColumns = m_ListView.sortedColumns;
            if (sortedColumns != null)
            {
                foreach (var column in sortedColumns)
                {
                    sortedColumn = column;
                    break;
                }
            }

            // Apply user-selected sorting
            var columnName = sortedColumn?.columnName;
            // If the selected sort column is not applicable to the current mode, ignore it and apply default sorting.
            if (columnName != null)
            {
                if (!isGcAllocMode && (columnName == k_ColumnName_GcAlloc || columnName == k_ColumnName_GcAllocPercent))
                    columnName = null;
                else if (isGcAllocMode && (columnName == k_ColumnName_Time || columnName == k_ColumnName_TotalPercent))
                    columnName = null;
            }
            // If no sort column is explicitly selected, apply default sorting based on mode
            if (columnName == null)
                columnName = isGcAllocMode ? k_ColumnName_GcAlloc : k_ColumnName_Time;

            var ascending = sortedColumn != null && sortedColumn.direction == SortDirection.Ascending;
            switch (columnName)
            {
                case k_ColumnName_Name:
                    SortInstances(m_Instances, ascending, (a, b) => EditorUtility.NaturalCompare(a.Name, b.Name));
                    break;
                case k_ColumnName_ThreadName:
                    SortInstances(m_Instances, ascending, (a, b) => EditorUtility.NaturalCompare(a.ThreadName, b.ThreadName));
                    break;
                case k_ColumnName_Time:
                    SortInstances(m_Instances, ascending, (a, b) => a.TotalTime.CompareTo(b.TotalTime));
                    break;
                case k_ColumnName_TotalPercent:
                    SortInstances(m_Instances, ascending, (a, b) => a.TotalTimePercent.CompareTo(b.TotalTimePercent));
                    break;
                case k_ColumnName_GcAlloc:
                    SortInstances(m_Instances, ascending, (a, b) => a.GcAlloc.CompareTo(b.GcAlloc));
                    break;
                case k_ColumnName_GcAllocPercent:
                    SortInstances(m_Instances, ascending, (a, b) => a.GcAllocPercent.CompareTo(b.GcAllocPercent));
                    break;
            }
        }

        static void SortInstances(List<SampleInformation> instances, bool ascending, Comparison<SampleInformation> comparison)
        {
            if (ascending)
                instances.Sort(comparison);
            else
                instances.Sort((a, b) => comparison(b, a));
        }

        void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            // TODO: Allow to view specific sample
        }

        void UpdateColumnVisibility()
        {
            var timeColumn = m_ListView.columns[k_ColumnName_Time];
            var totalPercentColumn = m_ListView.columns[k_ColumnName_TotalPercent];
            var gcAllocColumn = m_ListView.columns[k_ColumnName_GcAlloc];
            var gcAllocPercentColumn = m_ListView.columns[k_ColumnName_GcAllocPercent];

            if (m_IsGcAllocMode)
            {
                timeColumn.visible = false;
                totalPercentColumn.visible = false;
                gcAllocColumn.visible = true;
                gcAllocPercentColumn.visible = true;
            }
            else
            {
                timeColumn.visible = true;
                totalPercentColumn.visible = true;
                gcAllocColumn.visible = false;
                gcAllocPercentColumn.visible = false;
            }
        }

        public void ReloadData(Marker marker)
        {
            if (!IsViewLoaded)
                return;

            m_IsGcAllocMode = marker.Units != Marker.Unit.TimeNanoseconds;
            UpdateColumnVisibility();
            ReloadDataAsync(marker);
        }

        public void CancelReloadDataIfNecessary()
        {
            m_BuildModelCancellation?.Cancel();
        }

        async void ReloadDataAsync(Marker marker)
        {
            // If there is already a builder in flight, cancel it.
            m_BuildModelCancellation?.Cancel();

            // Capture mode flag before entering the background task to avoid a data race:
            // BuildMarkerInstancesData runs on a thread pool thread and must not read
            // m_IsGcAllocMode directly, as it could be written concurrently on the UI thread.
            bool isGcAllocMode = m_IsGcAllocMode;

            // Show activity overlay. The overlay intentionally keeps the stale list visible
            // while the task runs rather than clearing it, to avoid an unnecessary visual flash.
            UIUtility.SetElementDisplay(m_ActivityOverlay, true);

            var success = true;
            using (var buildModelCancellation = new CancellationTokenSource())
            {
                // Store a reference to the source so we can cancel it.
                m_BuildModelCancellation = buildModelCancellation;

                try
                {
                    // Build the data model asynchronously
                    var instances = await Task.Run(
                        () => BuildMarkerInstancesData(marker, buildModelCancellation.Token, isGcAllocMode),
                        buildModelCancellation.Token);

                    buildModelCancellation.Token.ThrowIfCancellationRequested();

                    // Assign results on the main thread to avoid data races with OnSortingChanged.
                    m_Instances = instances;
                    ApplySorting(m_IsGcAllocMode);

                    // Refresh the view on the main thread
                    RefreshView();
                }
                catch (OperationCanceledException e) when (e.CancellationToken == buildModelCancellation.Token)
                {
                    // The operation was cancelled.
                    success = false;
                }
                catch (ProfilerFrameIndexOutOfBounds)
                {
                    // The frame index is invalid, cancel the operation.
                    buildModelCancellation.Cancel();
                    success = false;
                }
                catch (Exception e)
                {
                    success = false;
                    UnityEngine.Debug.LogException(e);
                }

                // It's possible for an async reload operation to reach here after another has
                // been started, i.e. it is not the current builder.
                var isCurrentBuilder = m_BuildModelCancellation == buildModelCancellation;
                if (isCurrentBuilder)
                {
                    // Nullify the source reference if it is the current one.
                    m_BuildModelCancellation = null;

                    // Hide activity overlay
                    UIUtility.SetElementDisplay(m_ActivityOverlay, false);

                    if (success == false)
                    {
                        m_Instances = new List<SampleInformation>();
                        RefreshView();
                    }
                }
            }
        }

        List<SampleInformation> BuildMarkerInstancesData(Marker marker, CancellationToken cancellationToken, bool isGcAllocMode)
        {
            var frameIndex = marker.FrameIndex;
            var markerId = marker.MarkerId;

            // Verify the frame index is valid
            BuilderUtility.ThrowIfFrameIndexIsOutOfBounds(frameIndex, m_DataService);

            var instances = new List<SampleInformation>();
            var frameConstantsFetched = false;
            float totalFrameTime = 0f;
            var gcAllocMarkerId = FrameDataView.invalidMarkerId;
            long totalFrameGCAlloc = 0;

            // Iterate through all threads in the frame
            for (int threadIndex = 0;; threadIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var frameData = m_DataService.GetRawFrameDataView(frameIndex, threadIndex))
                {
                    if (!frameData.valid)
                        break;

                    if (!frameConstantsFetched)
                    {
                        // Fetch frame-level constants only once
                        totalFrameTime = frameData.frameTimeMs;
                        gcAllocMarkerId = frameData.GetMarkerId(k_GCAllocMarkerName);
                        var gcAllocInframeMarkerId = frameData.GetMarkerId(k_GCAllocInFrameMarkerName);
                        totalFrameGCAlloc = frameData.GetCounterValueAsLong(gcAllocInframeMarkerId);

                        frameConstantsFetched = true;
                    }

                    string threadName = null;

                    // Search for all samples with the matching marker ID in this thread
                    for (int sampleIndex = 0; sampleIndex < frameData.sampleCount; sampleIndex++)
                    {
                        var sampleMarkerId = frameData.GetSampleMarkerId(sampleIndex);
                        if (sampleMarkerId == markerId)
                        {
                            var sampleTime = frameData.GetSampleTimeMs(sampleIndex);
                            var entityName = GetSampleRelatedObjectName(frameData, sampleIndex);
                            var gcAlloc = isGcAllocMode ? GetSampleSelfGCAlloc(frameData, gcAllocMarkerId, sampleIndex) : 0L;

                            // If thread name is not set yet, cache it once to avoid gc allocations.
                            if (threadName == null)
                                threadName = frameData.threadName;

                            var sampleInfo = new SampleInformation
                            {
                                ThreadName = threadName,
                                Name = entityName,
                                TotalTime = sampleTime,
                                TotalTimePercent = totalFrameTime > 0 ? (sampleTime / totalFrameTime) * 100f : 0f,
                                GcAlloc = gcAlloc,
                                GcAllocPercent = totalFrameGCAlloc > 0 ? (gcAlloc / (float)totalFrameGCAlloc) * 100f : 0f
                            };

                            instances.Add(sampleInfo);
                        }
                    }
                }
            }

            return instances;
        }

        void RefreshView()
        {
            if (m_Instances.Count == 0)
            {
                UIUtility.SetElementDisplay(m_ListView, false);
                return;
            }

            UIUtility.SetElementDisplay(m_ListView, true);
            UpdateColumnVisibility();
            RebuildListView();
        }

        void RebuildListView()
        {
            m_ListView.itemsSource = m_Instances;
            m_ListView.RefreshItems();

            // Auto-select first item if available
            if (m_Instances.Count > 0 && !HasSelection())
                m_ListView.SetSelection(0);
        }

        bool HasSelection()
        {
            return m_ListView.selectedIndices.GetEnumerator().MoveNext();
        }

        static string GetSampleRelatedObjectName(RawFrameDataView rawFrameDataView, int sampleIndex)
        {
            var entityId = rawFrameDataView.GetSampleEntityId(sampleIndex);
            if (entityId == EntityId.None)
                return null;

            rawFrameDataView.GetUnityObjectInfo(entityId, out var objectInfo);
            if (string.IsNullOrEmpty(objectInfo.name) && objectInfo.relatedGameObjectEntityId != EntityId.None)
            {
                // Try to get name again in case of a component
                rawFrameDataView.GetUnityObjectInfo(objectInfo.relatedGameObjectEntityId, out objectInfo);
            }

            return objectInfo.name;
        }

        static long GetSampleSelfGCAlloc(RawFrameDataView rawFrameDataView, int gcAllocMarkerId, int sampleIndex)
        {
            // Iterate all direct samples and extract the first int metadata value from GC.Alloc samples.
            // Sum of those value will be self GC Alloc for the sample.
            var gcAllocSum = 0L;
            var childCount = rawFrameDataView.GetSampleChildrenCount(sampleIndex);
            for (int i = 0; i < childCount; i++)
            {
                sampleIndex++;
                var childMarkerId = rawFrameDataView.GetSampleMarkerId(sampleIndex);
                if (childMarkerId == gcAllocMarkerId)
                {
                    var metadataCount = rawFrameDataView.GetSampleMetadataCount(sampleIndex);
                    if (metadataCount > 0)
                    {
                        // Get the first int metadata value
                        var gcAlloc = rawFrameDataView.GetSampleMetadataAsInt(sampleIndex, 0);
                        gcAllocSum += gcAlloc;
                    }
                }

                // Skip any grandchildren
                var allChildrenCount = rawFrameDataView.GetSampleChildrenCountRecursive(sampleIndex);
                sampleIndex += allChildrenCount;
            }

            return gcAllocSum;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_BuildModelCancellation?.Cancel();

                if (m_ListView != null)
                {
                    m_ListView.columnSortingChanged -= OnSortingChanged;
                    m_ListView.selectionChanged -= OnSelectionChanged;
                }
            }

            base.Dispose(disposing);
        }
    }
}
