// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Networking
{
    // Details view for the Web Requests profiler module: a summary strip, the request list for the
    // selected frame, and an inspector for the selected request.
    //
    // The design this follows also specifies an Initiator column and Payload, Response body, Timing
    // and Message tabs. Each needs capture that does not exist yet (the calling script and line,
    // headers, bodies and per-phase timings), so they are left out rather than shown as columns and
    // tabs that never fill.
    internal class WebRequestDetailsViewController : ProfilerModuleViewController
    {
        const string k_UxmlResourceName = "WebRequest/UXML/Profiler/WebRequestDetailsView.uxml";
        const string k_UssResourceName = "WebRequest/StyleSheets/Profiler/WebRequestDetailsView.uss";
        const string k_UssLightResourceName = "WebRequest/StyleSheets/Profiler/WebRequestDetailsViewLight.uss";
        const string k_UssDarkResourceName = "WebRequest/StyleSheets/Profiler/WebRequestDetailsViewDark.uss";

        const string k_StatusColumn = "status-column";
        const string k_MethodColumn = "method-column";
        const string k_NameColumn = "name-column";
        const string k_SourceColumn = "source-column";
        const string k_TypeColumn = "type-column";
        const string k_SizeColumn = "size-column";
        const string k_TimeColumn = "time-column";

        const string k_CellClass = "web-request-details-view__cell";
        const string k_StatusSuccessClass = "web-request-details-view__status--success";
        const string k_StatusErrorClass = "web-request-details-view__status--error";
        const string k_StatusInFlightClass = "web-request-details-view__status--in-flight";

        internal const string k_InspectorVisiblePrefKey = "ProfilerWindow.WebRequests.DetailsPanel.Visible";
        internal const string k_InspectorWidthPrefKey = "ProfilerWindow.WebRequests.DetailsPanel.Width";

        // Must match the inspector's USS min-width, or the drag line is placed where the pane is not.
        const float k_InspectorMinWidth = 220f;

        bool m_InspectorVisible;

        readonly List<WebRequestProfilerRow> m_Rows = new List<WebRequestProfilerRow>();
        byte[] m_Strings = Array.Empty<byte>();

        MultiColumnListView m_ListView;
        Label m_SummaryRequests;
        Label m_SummaryCompleted;
        Label m_SummaryTransferred;
        Label m_SummaryPending;
        Label m_SummaryLatency;
        Label m_SummaryErrors;
        Label m_SummaryBytes;

        TwoPaneSplitView m_Split;
        VisualElement m_SplitDragLineAnchor;
        ToolbarButton m_InspectorToggle;
        VisualElement m_Inspector;
        Label m_InspectorEmpty;
        Foldout m_General;
        Label m_FieldUrl;
        Label m_FieldMethod;
        Label m_FieldStatus;
        Label m_FieldSource;
        Label m_FieldType;
        Label m_FieldTransferred;
        Label m_FieldDuration;

        public WebRequestDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow) {}

        protected override VisualElement CreateView()
        {
            var tree = EditorGUIUtility.LoadRequired(k_UxmlResourceName) as VisualTreeAsset;
            var style = EditorGUIUtility.LoadRequired(k_UssResourceName) as StyleSheet;

            var themedStyle = EditorGUIUtility.LoadRequired(
                EditorGUIUtility.isProSkin ? k_UssDarkResourceName : k_UssLightResourceName) as StyleSheet;

            var view = tree.Instantiate();
            view.style.flexGrow = 1f;
            view.styleSheets.Add(style);
            view.styleSheets.Add(themedStyle);

            m_SummaryRequests = view.Q<Label>("web-request-details-view__summary-requests");
            m_SummaryCompleted = view.Q<Label>("web-request-details-view__summary-completed");
            m_SummaryTransferred = view.Q<Label>("web-request-details-view__summary-transferred");
            m_SummaryPending = view.Q<Label>("web-request-details-view__summary-pending");
            m_SummaryLatency = view.Q<Label>("web-request-details-view__summary-latency");
            m_SummaryErrors = view.Q<Label>("web-request-details-view__summary-errors");
            m_SummaryBytes = view.Q<Label>("web-request-details-view__summary-bytes");

            m_Inspector = view.Q<VisualElement>("web-request-details-view__inspector");
            m_InspectorEmpty = view.Q<Label>("web-request-details-view__inspector-empty");
            m_General = view.Q<Foldout>("web-request-details-view__general");
            m_FieldUrl = view.Q<Label>("web-request-details-view__field-url");
            m_FieldMethod = view.Q<Label>("web-request-details-view__field-method");
            m_FieldStatus = view.Q<Label>("web-request-details-view__field-status");
            m_FieldSource = view.Q<Label>("web-request-details-view__field-source");
            m_FieldType = view.Q<Label>("web-request-details-view__field-type");
            m_FieldTransferred = view.Q<Label>("web-request-details-view__field-transferred");
            m_FieldDuration = view.Q<Label>("web-request-details-view__field-duration");

            SetUpInspectorToggle(view);

            m_ListView = view.Q<MultiColumnListView>("web-request-details-view__list");
            SetUpColumns();
            m_ListView.itemsSource = m_Rows;
            m_ListView.selectionChanged += OnSelectionChanged;

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            // Clearing the profiler does not necessarily move the selected frame, so the frame-changed
            // event above cannot be relied on to empty the list. ProfilerModule.Clear() is the usual
            // hook, but it cannot reach here - the details view controller is private to the base class.
            ProfilerDriver.profileCleared += OnProfileCleared;

            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            ProfilerDriver.profileCleared -= OnProfileCleared;
            if (m_ListView != null)
                m_ListView.selectionChanged -= OnSelectionChanged;

            if (m_InspectorToggle != null)
                m_InspectorToggle.clicked -= ToggleInspector;

            m_SplitDragLineAnchor?.UnregisterCallback<PointerUpEvent>(OnSplitDragFinished);

            base.Dispose(disposing);
        }

        void SetUpInspectorToggle(VisualElement view)
        {
            m_Split = view.Q<TwoPaneSplitView>("web-request-details-view__split");
            m_InspectorToggle = view.Q<ToolbarButton>("web-request-details-view__inspector-toggle");
            m_InspectorToggle.Q<Image>("web-request-details-view__inspector-toggle-icon").image =
                EditorGUIUtility.LoadIcon("RightPanel");
            m_InspectorToggle.clicked += ToggleInspector;

            // Without a viewDataKey the split view forgets a dragged width on its next layout pass.
            m_SplitDragLineAnchor = m_Split.Q("unity-dragline-anchor");
            m_SplitDragLineAnchor?.RegisterCallback<PointerUpEvent>(OnSplitDragFinished);

            ApplyInspectorVisible(EditorPrefs.GetBool(k_InspectorVisiblePrefKey, true));
        }

        // Only a click writes the preference, so the panel shows until the user hides it themselves -
        // opening the view can never persist a state they did not choose.
        internal void ToggleInspector()
        {
            ApplyInspectorVisible(!m_InspectorVisible);
            EditorPrefs.SetBool(k_InspectorVisiblePrefKey, m_InspectorVisible);
        }

        void ApplyInspectorVisible(bool visible)
        {
            m_InspectorVisible = visible;

            if (visible)
            {
                // Absent a banked width, the UXML's dimension stands rather than a second copy of it here.
                m_Split.fixedPaneInitialDimension = Mathf.Max(
                    EditorPrefs.GetFloat(k_InspectorWidthPrefKey, m_Split.fixedPaneInitialDimension),
                    k_InspectorMinWidth);
                m_Split.UnCollapse();
            }
            else
            {
                m_Split.CollapseChild(1);
            }
        }

        void OnSplitDragFinished(PointerUpEvent evt)
        {
            var width = m_Split.fixedPane?.resolvedStyle.width ?? float.NaN;
            if (!float.IsNaN(width) && width >= k_InspectorMinWidth)
                EditorPrefs.SetFloat(k_InspectorWidthPrefKey, width);
        }

        void SetUpColumns()
        {
            BindColumn(k_StatusColumn, (label, row) =>
            {
                label.text = WebRequestProfilerFormatting.FormatStatusCode(row);

                label.RemoveFromClassList(k_StatusSuccessClass);
                label.RemoveFromClassList(k_StatusErrorClass);
                label.RemoveFromClassList(k_StatusInFlightClass);
                label.AddToClassList(StatusClassFor(row));
            });

            BindColumn(k_MethodColumn, (label, row) => label.text = MethodOf(row));
            BindColumn(k_NameColumn, (label, row) =>
            {
                label.text = UrlOf(row);
                label.tooltip = label.text;
            });
            BindColumn(k_SourceColumn, (label, row) =>
            {
                label.text = WebRequestProfilerFormatting.FormatSource(row);
                label.tooltip = label.text;
            });
            BindColumn(k_TypeColumn, (label, row) =>
            {
                label.text = ContentTypeOf(row);
                label.tooltip = label.text;
            });
            BindColumn(k_SizeColumn, (label, row) => label.text = WebRequestProfilerFormatting.FormatTransferSize(row.bytesDownloaded));
            BindColumn(k_TimeColumn, (label, row) => label.text = WebRequestProfilerFormatting.FormatDuration(row.durationNs));
        }

        void BindColumn(string columnName, Action<Label, WebRequestProfilerRow> bind)
        {
            var column = m_ListView.columns[columnName];
            column.makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList(k_CellClass);
                return label;
            };
            column.bindCell = (element, index) => bind((Label)element, m_Rows[index]);
        }

        static string StatusClassFor(WebRequestProfilerRow row)
        {
            switch ((WebRequestProfilerState)row.state)
            {
                case WebRequestProfilerState.InFlight:
                    return k_StatusInFlightClass;
                case WebRequestProfilerState.Failed:
                    return k_StatusErrorClass;
                default:
                    // A request can complete transport-wise and still carry an HTTP error status.
                    return row.statusCode >= 400 ? k_StatusErrorClass : k_StatusSuccessClass;
            }
        }

        string UrlOf(WebRequestProfilerRow row) => WebRequestProfilerFrameReader.Slice(m_Strings, row.urlOffset, row.urlLength);

        string MethodOf(WebRequestProfilerRow row) => WebRequestProfilerFrameReader.Slice(m_Strings, row.methodOffset, row.methodLength);

        // Empty until the response arrives, so an in-flight row shows nothing rather than a stale type.
        string ContentTypeOf(WebRequestProfilerRow row) => WebRequestProfilerFrameReader.Slice(m_Strings, row.contentTypeOffset, row.contentTypeLength);

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            ReloadData(selectedFrameIndex);
        }

        // The frames are already gone by the time this runs, so the reload finds nothing and empties
        // the list, the summary and the inspector through the one path rather than a second one that
        // could drift from it.
        void OnProfileCleared()
        {
            ReloadData(ProfilerWindow.selectedFrameIndex);
        }

        void ReloadData(long selectedFrameIndex)
        {
            // Stepping through frames should not drop the selection: a request in flight appears in
            // every frame it spans, so follow it by id rather than by row index, which shifts as other
            // requests start and finish around it.
            var previouslySelected = SelectedRequestId();

            m_Rows.Clear();
            m_Strings = Array.Empty<byte>();

            var rows = WebRequestProfilerFrameReader.ReadFrame(selectedFrameIndex, out var strings);
            if (rows != null)
            {
                m_Rows.AddRange(rows);
                m_Strings = strings;
            }

            m_ListView.RefreshItems();

            var index = previouslySelected.HasValue ? IndexOfRequest(previouslySelected.Value) : -1;
            if (index >= 0)
                m_ListView.SetSelectionWithoutNotify(new[] { index });
            else
                m_ListView.ClearSelection();

            UpdateSummary();
            UpdateInspector(index);
        }

        ulong? SelectedRequestId()
        {
            var index = m_ListView.selectedIndex;
            return index >= 0 && index < m_Rows.Count ? m_Rows[index].requestId : (ulong?)null;
        }

        int IndexOfRequest(ulong requestId)
        {
            for (var i = 0; i < m_Rows.Count; ++i)
            {
                if (m_Rows[i].requestId == requestId)
                    return i;
            }

            return -1;
        }

        void UpdateSummary()
        {
            var completed = 0;
            var errors = 0;
            var pending = 0;
            ulong bytesDown = 0;
            ulong bytesUp = 0;
            var durations = new List<ulong>(m_Rows.Count);

            foreach (var row in m_Rows)
            {
                bytesDown += row.bytesDownloaded;
                bytesUp += row.bytesUploaded;

                switch ((WebRequestProfilerState)row.state)
                {
                    case WebRequestProfilerState.InFlight:
                        pending++;
                        break;
                    case WebRequestProfilerState.Failed:
                        errors++;
                        durations.Add(row.durationNs);
                        break;
                    default:
                        completed++;
                        if (row.statusCode >= 400)
                            errors++;
                        durations.Add(row.durationNs);
                        break;
                }
            }

            m_SummaryRequests.text = WebRequestProfilerFormatting.FormatCount(m_Rows.Count, "Request");
            m_SummaryCompleted.text = $"{completed} Completed";
            m_SummaryTransferred.text = $"{WebRequestProfilerFormatting.FormatTransferSize(bytesDown + bytesUp)} Transferred";
            m_SummaryPending.text = WebRequestProfilerFormatting.FormatCount(pending, "Pending Request");
            // No finished request in the frame means no sample, which is not the same as a latency of
            // zero - every request being still in flight is ordinary while scrubbing.
            var latency = durations.Count > 0
                ? WebRequestProfilerFormatting.FormatDuration(Percentile(durations, 0.95f))
                : "-";
            m_SummaryLatency.text = $"Latency p95: {latency}";
            m_SummaryErrors.text = WebRequestProfilerFormatting.FormatCount(errors, "Error");
            m_SummaryBytes.text = $"Total Bytes Down/Up: {WebRequestProfilerFormatting.FormatTransferSize(bytesDown)} / {WebRequestProfilerFormatting.FormatTransferSize(bytesUp)}";
        }

        // Nearest-rank percentile: with the handful of requests a frame usually holds, interpolating
        // between neighbours would imply more precision than the sample supports.
        static ulong Percentile(List<ulong> values, float percentile)
        {
            if (values.Count == 0)
                return 0;

            values.Sort();
            var rank = Mathf.CeilToInt(percentile * values.Count);
            var index = Mathf.Clamp(rank - 1, 0, values.Count - 1);
            return values[index];
        }

        void OnSelectionChanged(IEnumerable<object> _)
        {
            UpdateInspector(m_ListView.selectedIndex);
        }

        void UpdateInspector(int index)
        {
            var hasSelection = index >= 0 && index < m_Rows.Count;

            m_InspectorEmpty.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            m_General.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection)
                return;

            var row = m_Rows[index];
            m_FieldUrl.text = UrlOf(row);
            m_FieldMethod.text = MethodOf(row);
            m_FieldStatus.text = WebRequestProfilerFormatting.FormatStatus(row);
            m_FieldSource.text = WebRequestProfilerFormatting.FormatSource(row);
            var contentType = ContentTypeOf(row);
            m_FieldType.text = string.IsNullOrEmpty(contentType) ? "-" : contentType;
            m_FieldTransferred.text = $"{WebRequestProfilerFormatting.FormatTransferSize(row.bytesDownloaded)} down / {WebRequestProfilerFormatting.FormatTransferSize(row.bytesUploaded)} up";
            m_FieldDuration.text = WebRequestProfilerFormatting.FormatDuration(row.durationNs);
        }
    }
}
