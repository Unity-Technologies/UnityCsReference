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
    // Details view for the Web Requests profiler module: a summary strip, every request captured so
    // far, and an inspector for the selected one.
    //
    // The design this follows also specifies an Initiator column and a Message tab. The first needs
    // the calling script and line, which is capture that does not exist; the second is a WebSocket
    // frame log and we observe no frames. Both are left out rather than shown never filling.
    internal class WebRequestDetailsViewController : ProfilerModuleViewController
    {
        const string k_UxmlResourceName = "WebRequest/UXML/Profiler/WebRequestDetailsView.uxml";
        const string k_UssResourceName = "WebRequest/StyleSheets/Profiler/WebRequestDetailsView.uss";
        const string k_UssLightResourceName = "WebRequest/StyleSheets/Profiler/WebRequestDetailsViewLight.uss";
        const string k_UssDarkResourceName = "WebRequest/StyleSheets/Profiler/WebRequestDetailsViewDark.uss";

        const string k_OrderColumn = "order-column";
        const string k_StatusColumn = "status-column";
        const string k_MethodColumn = "method-column";
        const string k_NameColumn = "name-column";
        const string k_SourceColumn = "source-column";
        const string k_TypeColumn = "type-column";
        const string k_SizeColumn = "size-column";
        const string k_TimeColumn = "time-column";

        const string k_CellClass = "web-request-details-view__cell";
        const string k_NameCellClass = "web-request-details-view__name-cell";
        const string k_InsecureIconClass = "web-request-details-view__insecure-icon";
        const string k_StatusSuccessClass = "web-request-details-view__status--success";
        const string k_StatusErrorClass = "web-request-details-view__status--error";
        const string k_StatusInFlightClass = "web-request-details-view__status--in-flight";

        const string k_InsecureTooltip = "Sent over plain HTTP, so the request and response were not encrypted.";

        internal const string k_InspectorVisiblePrefKey = "ProfilerWindow.WebRequests.DetailsPanel.Visible";
        internal const string k_InspectorWidthPrefKey = "ProfilerWindow.WebRequests.DetailsPanel.Width";

        // Must match the inspector's USS min-width, or the drag line is placed where the pane is not.
        const float k_InspectorMinWidth = 220f;

        readonly WebRequestProfilerCaptureLog m_Log = new WebRequestProfilerCaptureLog();

        // What the list shows: the log's records that pass the filter, in the user's sort order. The log
        // itself stays in capture order, because the merge keys off it and the summary totals the whole
        // capture rather than the visible rows.
        readonly List<WebRequestProfilerRecord> m_Rows = new List<WebRequestProfilerRecord>();
        readonly List<SortColumnDescription> m_SortOrder = new List<SortColumnDescription>();

        string m_Filter = string.Empty;

        bool m_InspectorVisible;

        MultiColumnListView m_ListView;
        ToolbarSearchField m_Search;
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
        VisualElement m_Tabs;
        Foldout m_ResponseHeaders;
        Foldout m_RequestHeaders;
        Label m_FieldUrl;
        Label m_FieldMethod;
        Label m_FieldStatus;
        Label m_FieldSource;
        Label m_FieldType;
        Label m_FieldTransferred;
        Label m_FieldDuration;
        Label m_TimingEmpty;
        VisualElement m_TimingPhases;
        Foldout m_QueryParameters;
        Label m_RequestPayloadEmpty;
        TextField m_RequestPayloadText;
        Label m_ResponseBodyEmpty;
        TextField m_ResponseBodyText;

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
            m_Tabs = view.Q<VisualElement>("web-request-details-view__tabs");
            m_ResponseHeaders = view.Q<Foldout>("web-request-details-view__response-headers");
            m_RequestHeaders = view.Q<Foldout>("web-request-details-view__request-headers");
            m_FieldUrl = view.Q<Label>("web-request-details-view__field-url");
            m_FieldMethod = view.Q<Label>("web-request-details-view__field-method");
            m_FieldStatus = view.Q<Label>("web-request-details-view__field-status");
            m_FieldSource = view.Q<Label>("web-request-details-view__field-source");
            m_FieldType = view.Q<Label>("web-request-details-view__field-type");
            m_FieldTransferred = view.Q<Label>("web-request-details-view__field-transferred");
            m_FieldDuration = view.Q<Label>("web-request-details-view__field-duration");
            m_TimingEmpty = view.Q<Label>("web-request-details-view__timing-empty");
            m_TimingPhases = view.Q<VisualElement>("web-request-details-view__timing-phases");

            m_QueryParameters = view.Q<Foldout>("web-request-details-view__query-parameters");
            m_RequestPayloadEmpty = view.Q<Label>("web-request-details-view__request-payload-empty");
            m_RequestPayloadText = view.Q<TextField>("web-request-details-view__request-payload-text");
            m_ResponseBodyEmpty = view.Q<Label>("web-request-details-view__response-body-empty");
            m_ResponseBodyText = view.Q<TextField>("web-request-details-view__response-body-text");

            SetUpCopyButton(view, "web-request-details-view__request-payload-copy", () => m_RequestPayloadText.value);
            SetUpCopyButton(view, "web-request-details-view__response-body-copy", () => m_ResponseBodyText.value);

            SetUpInspectorToggle(view);

            m_Search = view.Q<ToolbarSearchField>("web-request-details-view__search");
            m_Search.RegisterValueChangedCallback(OnFilterChanged);

            m_ListView = view.Q<MultiColumnListView>("web-request-details-view__list");
            m_ListView.sortingMode = ColumnSortingMode.Custom;
            SetUpColumns();
            m_ListView.itemsSource = m_Rows;
            m_ListView.selectionChanged += OnSelectionChanged;
            m_ListView.columnSortingChanged += OnColumnSortingChanged;

            ReloadData(force: true);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            // Frames keep arriving while the cursor stays put: a user who selects a frame during a
            // recording pins selectedFrameIndex, so the event above stops firing and the log would stop
            // ingesting until they moved it again. Cheap per frame, because ReloadData early-outs when
            // Sync reports it read nothing.
            ProfilerDriver.NewProfilerFrameRecorded += OnNewProfilerFrameRecorded;

            // ProfilerModule.Clear() is the usual hook but cannot reach here - the details view
            // controller is private to the base class.
            ProfilerDriver.profileCleared += OnProfileCleared;
            ProfilerDriver.profileLoaded += OnProfileLoaded;

            return view;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            ProfilerDriver.NewProfilerFrameRecorded -= OnNewProfilerFrameRecorded;
            ProfilerDriver.profileCleared -= OnProfileCleared;
            ProfilerDriver.profileLoaded -= OnProfileLoaded;
            if (m_ListView != null)
            {
                m_ListView.selectionChanged -= OnSelectionChanged;
                m_ListView.columnSortingChanged -= OnColumnSortingChanged;
            }

            if (m_Search != null)
                m_Search.UnregisterValueChangedCallback(OnFilterChanged);

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
            BindColumn(k_StatusColumn, (label, record) =>
            {
                label.text = WebRequestProfilerFormatting.FormatStatusCode(record);

                label.RemoveFromClassList(k_StatusSuccessClass);
                label.RemoveFromClassList(k_StatusErrorClass);
                label.RemoveFromClassList(k_StatusInFlightClass);
                label.AddToClassList(StatusClassFor(record));
            });

            // One-based: the first request a user made is "1", not "0".
            BindColumn(k_OrderColumn, (label, record) => label.text = (record.captureIndex + 1).ToString());
            BindColumn(k_MethodColumn, (label, record) => label.text = record.method);
            BindNameColumn();
            BindColumn(k_SourceColumn, (label, record) =>
            {
                label.text = WebRequestProfilerFormatting.FormatSource(record);
                label.tooltip = label.text;
            });
            BindColumn(k_TypeColumn, (label, record) =>
            {
                label.text = record.contentType;
                label.tooltip = label.text;
            });
            BindColumn(k_SizeColumn, (label, record) =>
            {
                label.text = WebRequestProfilerFormatting.FormatDownloadSize(record);
                // Here rather than makeCell: cells are recycled, and a measured row must not keep an
                // unmeasured one's explanation.
                label.tooltip = WebRequestProfilerFormatting.HasMeasuredDownload(record)
                    ? string.Empty
                    : WebRequestProfilerFormatting.NotMeasuredTooltip;
            });
            BindColumn(k_TimeColumn, (label, record) => label.text = WebRequestProfilerFormatting.FormatDuration(record.durationNs));
        }

        void BindColumn(string columnName, Action<Label, WebRequestProfilerRecord> bind)
        {
            var column = m_ListView.columns[columnName];
            column.makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList(k_CellClass);
                AddRowContextMenu(label);
                return label;
            };
            column.bindCell = (element, index) =>
            {
                element.userData = index;
                bind((Label)element, m_Rows[index]);
            };
        }

        // Its own factory because the cell is a row rather than a bare label: the warning sits beside the
        // URL when the request went over plain HTTP.
        void BindNameColumn()
        {
            var column = m_ListView.columns[k_NameColumn];
            column.makeCell = () =>
            {
                var cell = new VisualElement();
                cell.AddToClassList(k_NameCellClass);

                // Ahead of the label, which grows to fill the cell and would otherwise push the icon to
                // the far edge, away from the URL it refers to.
                var warning = new Image { name = "insecure" };
                warning.AddToClassList(k_InsecureIconClass);
                warning.image = EditorGUIUtility.LoadIcon("console.warnicon.sml");
                cell.Add(warning);

                var label = new Label();
                label.AddToClassList(k_CellClass);
                cell.Add(label);

                AddRowContextMenu(cell);
                return cell;
            };
            column.bindCell = (element, index) =>
            {
                element.userData = index;
                var record = m_Rows[index];

                var label = element.Q<Label>();
                label.text = record.url;
                label.tooltip = record.url;

                // Hidden rather than removed from layout, so every URL in the column starts at the same x.
                // A hidden element still hit-tests, so the tooltip and picking come off with it - otherwise
                // hovering the empty icon slot on an HTTPS row reports it as unencrypted.
                var warning = element.Q<Image>("insecure");
                var insecure = IsInsecure(record.url);
                warning.style.visibility = insecure ? Visibility.Visible : Visibility.Hidden;
                warning.pickingMode = insecure ? PickingMode.Position : PickingMode.Ignore;
                warning.tooltip = insecure ? k_InsecureTooltip : null;
            };
        }

        // Loopback is exempt: Unity allows plain HTTP there whatever the insecure-connections setting is,
        // so flagging it would fire on every local test server.
        internal static bool IsInsecure(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            return string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) && !uri.IsLoopback;
        }

        // On every cell, not the row: MultiColumnListView builds a row from independent cell elements, and
        // a right-click lands on whichever one is under the pointer. The bound index rides on the element
        // because the menu opens without changing the selection.
        void AddRowContextMenu(VisualElement cell)
        {
            cell.AddManipulator(new ContextualMenuManipulator(evt => PopulateRowMenu(evt, cell)));
        }

        void PopulateRowMenu(ContextualMenuPopulateEvent evt, VisualElement cell)
        {
            if (!(cell.userData is int index) || index < 0 || index >= m_Rows.Count)
                return;

            var record = m_Rows[index];
            evt.menu.AppendAction("Copy URL", _ => EditorGUIUtility.systemCopyBuffer = record.url);
            evt.menu.AppendAction("Copy Details", _ => EditorGUIUtility.systemCopyBuffer = Describe(record));
        }

        internal static string Describe(WebRequestProfilerRecord record)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append(record.method).Append(' ').Append(record.url).Append('\n');
            builder.Append("Status: ").Append(WebRequestProfilerFormatting.FormatStatus(record)).Append('\n');
            builder.Append("Source: ").Append(WebRequestProfilerFormatting.FormatSource(record)).Append('\n');
            if (!string.IsNullOrEmpty(record.contentType))
                builder.Append("Content type: ").Append(record.contentType).Append('\n');
            builder.Append("Transferred: ")
                .Append(WebRequestProfilerFormatting.FormatDownloadSize(record)).Append(" down / ")
                .Append(WebRequestProfilerFormatting.FormatUploadSize(record)).Append(" up\n");
            builder.Append("Duration: ").Append(WebRequestProfilerFormatting.FormatDuration(record.durationNs));
            return builder.ToString();
        }

        static string StatusClassFor(WebRequestProfilerRecord record)
        {
            switch (record.state)
            {
                case WebRequestProfilerState.InFlight:
                    return k_StatusInFlightClass;
                case WebRequestProfilerState.Failed:
                    return k_StatusErrorClass;
                default:
                    // A request can complete transport-wise and still carry an HTTP error status.
                    return record.statusCode >= 400 ? k_StatusErrorClass : k_StatusSuccessClass;
            }
        }

        void OnSelectedFrameIndexChanged(long _)
        {
            ReloadData();
        }

        // ProfilerDriver hands over the frame range; neither argument is needed here, because Sync
        // reads the range off the driver itself.
        void OnNewProfilerFrameRecorded(int _, int __)
        {
            ReloadData();
        }

        // Clearing need not move the selected frame, and a replacement capture renumbers frames from
        // zero, so neither the frame-changed event nor the ingested range can catch it.
        void OnProfileCleared()
        {
            m_Log.Clear();
            ReloadData(force: true);
        }

        // A loaded capture is a different session: its frames renumber from zero and its request ids
        // start over, so merging it into what is already ingested would mix two sessions under colliding
        // ids. Sync cannot notice on its own - it only ever reads forward, so a load at least as long as
        // the current session leaves the ingested mark ahead of every loaded frame and reads none of them.
        void OnProfileLoaded()
        {
            m_Log.Clear();
            ReloadData(force: true);
        }

        // force covers the cases Sync cannot report on: a Clear leaves it with nothing to ingest, so it
        // returns false while the rows it emptied are still on screen.
        void ReloadData(bool force = false)
        {
            // Tracked by id, not row index: a newly ingested frame can add requests above this one, and
            // so can a re-sort.
            var previouslySelected = SelectedRequestId();

            // Every frame in the buffer, not the selected one. A request is emitted in each frame it is
            // alive in and then dropped, so its completion - and with it the status code and content
            // type - exists in exactly one frame.
            var changed = m_Log.Sync(ProfilerDriver.firstFrameIndex, ProfilerDriver.lastFrameIndex);

            // Scrubbing raises a frame change per frame and the log spans the whole recording, so
            // re-filtering and re-sorting it for an unchanged list gets steadily more expensive.
            if (!changed && !force)
                return;

            RebuildRows();
            RestoreSelection(previouslySelected);
            UpdateSummary();
        }

        void OnColumnSortingChanged()
        {
            var previouslySelected = SelectedRequestId();

            RebuildRows();
            RestoreSelection(previouslySelected);
        }

        void OnFilterChanged(ChangeEvent<string> evt)
        {
            m_Filter = evt.newValue ?? string.Empty;

            var previouslySelected = SelectedRequestId();
            RebuildRows();
            RestoreSelection(previouslySelected);
            UpdateSummary();
        }

        // Filtered and sorted here rather than only when the user acts, so both outlive ingesting a frame.
        void RebuildRows()
        {
            m_Rows.Clear();
            foreach (var record in m_Log.Records)
            {
                if (MatchesFilter(record))
                    m_Rows.Add(record);
            }

            SortRows();

            m_ListView.RefreshItems();
        }

        bool MatchesFilter(WebRequestProfilerRecord record)
        {
            return Matches(record, m_Filter);
        }

        internal static bool Matches(WebRequestProfilerRecord record, string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            // Status is skipped while in flight: its code is still 0, so every such row would match "0".
            return Contains(record.url, filter)
                || Contains(record.method, filter)
                || Contains(record.contentType, filter)
                || Contains(WebRequestProfilerFormatting.FormatSource(record), filter)
                || (record.statusCode != 0 && Contains(record.statusCode.ToString(), filter));
        }

        static bool Contains(string value, string filter)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void RestoreSelection(ulong? requestId)
        {
            var index = requestId.HasValue ? IndexOfRequest(requestId.Value) : -1;
            if (index >= 0)
                m_ListView.SetSelectionWithoutNotify(new[] { index });
            else
                m_ListView.ClearSelection();

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

        void SortRows()
        {
            m_SortOrder.Clear();
            if (m_ListView.sortedColumns != null)
            {
                foreach (var description in m_ListView.sortedColumns)
                    m_SortOrder.Add(description);
            }

            SortRecords(m_Rows, m_SortOrder);
        }

        // Shift-clicking headers builds a multi-column order, so every description is consulted in turn.
        internal static void SortRecords(List<WebRequestProfilerRecord> records, List<SortColumnDescription> sortOrder)
        {
            // No sorted column means capture order, which the caller already holds.
            if (sortOrder.Count == 0)
                return;

            records.Sort((a, b) => CompareRows(a, b, sortOrder));
        }

        // Ties break on requestId, which is handed out in start order. List.Sort is unstable and the
        // rows are re-sorted on every frame change, so without a total order equal rows would swap
        // places as the user scrubs.
        static int CompareRows(WebRequestProfilerRecord a, WebRequestProfilerRecord b, List<SortColumnDescription> sortOrder)
        {
            foreach (var description in sortOrder)
            {
                var comparison = CompareColumn(description.columnName, a, b);
                if (comparison != 0)
                    return description.direction == SortDirection.Ascending ? comparison : -comparison;
            }

            return a.requestId.CompareTo(b.requestId);
        }

        static int CompareColumn(string columnName, WebRequestProfilerRecord a, WebRequestProfilerRecord b)
        {
            switch (columnName)
            {
                // The capture index, not the row's position: sorting by anything else must not change
                // what this column reports, or it stops meaning call order.
                case k_OrderColumn:
                    return a.captureIndex.CompareTo(b.captureIndex);
                // In-flight rows have no status code yet, so they group together at 0.
                case k_StatusColumn:
                    return a.statusCode.CompareTo(b.statusCode);
                case k_MethodColumn:
                    return CompareText(a.method, b.method);
                case k_NameColumn:
                    return CompareText(a.url, b.url);
                // On the displayed name, not the enum, so the order matches what the column reads.
                case k_SourceColumn:
                    return CompareText(
                        WebRequestProfilerFormatting.FormatSource(a),
                        WebRequestProfilerFormatting.FormatSource(b));
                case k_TypeColumn:
                    return CompareText(a.contentType, b.contentType);
                case k_SizeColumn:
                    return a.bytesDownloaded.CompareTo(b.bytesDownloaded);
                case k_TimeColumn:
                    return a.durationNs.CompareTo(b.durationNs);
                default:
                    return 0;
            }
        }

        static int CompareText(string a, string b)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        void UpdateSummary()
        {
            var completed = 0;
            var errors = 0;
            var pending = 0;
            ulong bytesDown = 0;
            ulong bytesUp = 0;
            var records = m_Log.Records;
            var durations = new List<ulong>(records.Count);

            // A capture can mix transports that count bytes with ones that do not, so a sum over some
            // of the rows is a floor and FormatTransferTotal marks it as one.
            var measuredDown = false;
            var measuredUp = false;
            var unmeasuredDown = false;
            var unmeasuredUp = false;

            foreach (var record in records)
            {
                if (WebRequestProfilerFormatting.HasMeasuredDownload(record))
                {
                    bytesDown += record.bytesDownloaded;
                    measuredDown = true;
                }
                else
                {
                    unmeasuredDown = true;
                }

                if (WebRequestProfilerFormatting.HasMeasuredUpload(record))
                {
                    bytesUp += record.bytesUploaded;
                    measuredUp = true;
                }
                else
                {
                    unmeasuredUp = true;
                }

                switch (record.state)
                {
                    case WebRequestProfilerState.InFlight:
                        pending++;
                        break;
                    case WebRequestProfilerState.Failed:
                        errors++;
                        durations.Add(record.durationNs);
                        break;
                    default:
                        completed++;
                        if (record.statusCode >= 400)
                            errors++;
                        durations.Add(record.durationNs);
                        break;
                }
            }

            // Every total covers the whole capture rather than the filtered rows; the request count is the
            // only one that says how many are visible, so a filter cannot make the totals mean two things.
            m_SummaryRequests.text = m_Rows.Count == records.Count
                ? WebRequestProfilerFormatting.FormatCount(records.Count, "Request")
                : $"{m_Rows.Count} of {WebRequestProfilerFormatting.FormatCount(records.Count, "Request")}";
            m_SummaryCompleted.text = $"{completed} Completed";
            // Both directions in one figure, so it is a floor as soon as either of them is.
            var anyUnmeasured = unmeasuredDown || unmeasuredUp;
            m_SummaryTransferred.text = $"{WebRequestProfilerFormatting.FormatTransferTotal(bytesDown + bytesUp, measuredDown || measuredUp, anyUnmeasured)} Transferred";
            m_SummaryTransferred.tooltip = anyUnmeasured ? WebRequestProfilerFormatting.PartialTotalTooltip : string.Empty;
            m_SummaryPending.text = WebRequestProfilerFormatting.FormatCount(pending, "Pending Request");
            // No finished request means no sample, which is not the same as a latency of zero.
            var latency = durations.Count > 0
                ? WebRequestProfilerFormatting.FormatDuration(Percentile(durations, 0.95f))
                : "-";
            m_SummaryLatency.text = $"Latency p95: {latency}";
            m_SummaryErrors.text = WebRequestProfilerFormatting.FormatCount(errors, "Error");
            m_SummaryBytes.text = $"Total Bytes Down/Up: {WebRequestProfilerFormatting.FormatTransferTotal(bytesDown, measuredDown, unmeasuredDown)} / {WebRequestProfilerFormatting.FormatTransferTotal(bytesUp, measuredUp, unmeasuredUp)}";
            m_SummaryBytes.tooltip = anyUnmeasured ? WebRequestProfilerFormatting.PartialTotalTooltip : string.Empty;
        }

        // Nearest-rank percentile: interpolating between neighbours would imply more precision than a
        // capture of a few dozen requests supports.
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
            var index = m_ListView.selectedIndex;
            UpdateInspector(index);
            JumpToCompletionFrame(index);
        }

        // The frame a request finished in is the one whose counters and markers cover its transfer, and
        // it is the only frame carrying its completion, so selecting the request goes there.
        void JumpToCompletionFrame(int index)
        {
            if (index < 0 || index >= m_Rows.Count)
                return;

            var frame = m_Rows[index].completedFrame;
            if (frame < 0 || frame == ProfilerWindow.selectedFrameIndex)
                return;

            // The log outlives the ring buffer, so a request can still be listed after the frame it
            // completed in has been evicted. Staying put beats jumping to an empty frame.
            if (frame < ProfilerDriver.firstFrameIndex || frame > ProfilerDriver.lastFrameIndex)
                return;

            ProfilerWindow.selectedFrameIndex = frame;
        }

        void UpdateInspector(int index)
        {
            var hasSelection = index >= 0 && index < m_Rows.Count;

            m_InspectorEmpty.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;
            // The TabView owns each tab's content, so this one toggle covers all of them.
            m_Tabs.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasSelection)
                return;

            var record = m_Rows[index];
            m_FieldUrl.text = record.url;
            m_FieldMethod.text = record.method;
            m_FieldStatus.text = WebRequestProfilerFormatting.FormatStatus(record);
            m_FieldSource.text = WebRequestProfilerFormatting.FormatSource(record);
            m_FieldType.text = string.IsNullOrEmpty(record.contentType) ? "-" : record.contentType;
            m_FieldTransferred.text = $"{WebRequestProfilerFormatting.FormatDownloadSize(record)} down / {WebRequestProfilerFormatting.FormatUploadSize(record)} up";
            m_FieldTransferred.tooltip = WebRequestProfilerFormatting.HasMeasuredDownload(record)
                && WebRequestProfilerFormatting.HasMeasuredUpload(record)
                ? string.Empty
                : WebRequestProfilerFormatting.NotMeasuredTooltip;
            m_FieldDuration.text = WebRequestProfilerFormatting.FormatDuration(record.durationNs);

            PopulateHeaders(m_ResponseHeaders, record, WebRequestProfilerHeaderKind.Response);
            PopulateHeaders(m_RequestHeaders, record, WebRequestProfilerHeaderKind.Request);
            PopulateQueryParameters(m_QueryParameters, record);
            PopulateBody(m_RequestPayloadEmpty, m_RequestPayloadText, record, record.requestBody);
            PopulateBody(m_ResponseBodyEmpty, m_ResponseBodyText, record, record.responseBody);
            PopulateTiming(record);
        }

        // Copies what the box shows, so a truncated body copies the part that exists.
        static void SetUpCopyButton(VisualElement view, string buttonName, Func<string> textToCopy)
        {
            // Qualified: UnityEditorInternal has a Button too, and this file uses both namespaces.
            var button = view.Q<UnityEngine.UIElements.Button>(buttonName);
            button.clicked += () =>
            {
                var text = textToCopy();
                if (!string.IsNullOrEmpty(text))
                    EditorGUIUtility.systemCopyBuffer = text;
            };
        }

        // Parsed rather than captured: the url is already on the record, so this half of the Payload
        // tab works with body capture off, which is how a user first opens it.
        internal static List<KeyValuePair<string, string>> ParseQueryParameters(string url)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(url))
                return parameters;

            // Fragment first: a '?' inside one starts no query, so host/path#section?example has none.
            var fragment = url.IndexOf('#');
            if (fragment >= 0)
                url = url.Substring(0, fragment);

            var query = url.IndexOf('?');
            if (query < 0)
                return parameters;

            foreach (var pair in url.Substring(query + 1).Split('&'))
            {
                if (pair.Length == 0)
                    continue;

                var equals = pair.IndexOf('=');
                var name = equals >= 0 ? pair.Substring(0, equals) : pair;
                var value = equals >= 0 ? pair.Substring(equals + 1) : string.Empty;

                // Decoded, which is what the parameter means; General above has the raw url. Invalid
                // escaping is shown as it stands rather than throwing into the view.
                parameters.Add(new KeyValuePair<string, string>(Unescape(name), Unescape(value)));
            }

            return parameters;
        }

        static string Unescape(string text)
        {
            try
            {
                return Uri.UnescapeDataString(text);
            }
            catch (UriFormatException)
            {
                return text;
            }
        }

        static void PopulateQueryParameters(Foldout foldout, WebRequestProfilerRecord record)
        {
            foldout.Clear();

            var parameters = ParseQueryParameters(record.url);
            foreach (var parameter in parameters)
            {
                var field = new VisualElement();
                field.AddToClassList("web-request-details-view__field");

                var nameLabel = new Label(parameter.Key);
                nameLabel.AddToClassList("web-request-details-view__field-label");

                var valueLabel = new Label(parameter.Value);
                valueLabel.AddToClassList("web-request-details-view__field-value");

                field.Add(nameLabel);
                field.Add(valueLabel);
                foldout.Add(field);
            }

            if (parameters.Count == 0)
            {
                var empty = new Label("This request's URL has no query string.");
                empty.AddToClassList("web-request-details-view__inspector-empty");
                foldout.Add(empty);
            }
        }

        // Captured, never captured, or skipped by the type gate. Only the first has text.
        static void PopulateBody(Label empty, TextField text, WebRequestProfilerRecord record,
            in WebRequestProfilerBody body)
        {
            if (!body.captured)
            {
                // The row says from its first frame whether a body is coming, so an in-flight request
                // is not told to wait for one that never will.
                var enabled = (record.bodyCapture & WebRequestProfilerBodyCapture.Enabled) != 0;
                var supported = (record.bodyCapture & WebRequestProfilerBodyCapture.Supported) != 0;

                if (!enabled)
                {
                    // Named rather than described: the switch is the whole answer.
                    empty.text = "Not captured: body capture was off when this request started. Set UnityWebRequest.enableProfilerBodyCapture to true in the player you are profiling, then capture again.";
                }
                else if (!supported && (record.bodyCapture & WebRequestProfilerBodyCapture.TransportKnown) != 0)
                {
                    // Once a transport has answered, a request still running on it will never produce a
                    // body. Before that, Supported is clear only because nothing has been asked.
                    empty.text = "Not captured: the transport that ran this request cannot report bodies.";
                }
                else
                {
                    empty.text = "Bodies are recorded when a request finishes.";
                }

                empty.style.display = DisplayStyle.Flex;
                text.style.display = DisplayStyle.None;
                text.SetValueWithoutNotify(string.Empty);
                return;
            }

            if (body.IsBinary)
            {
                empty.text = $"Not captured (binary). {WebRequestProfilerFormatting.FormatBytes(body.totalLength)} of a content type that is not text.";
                empty.style.display = DisplayStyle.Flex;
                text.style.display = DisplayStyle.None;
                text.SetValueWithoutNotify(string.Empty);
                return;
            }

            if (body.totalLength == 0)
            {
                empty.text = "This request carried no body.";
                empty.style.display = DisplayStyle.Flex;
                text.style.display = DisplayStyle.None;
                text.SetValueWithoutNotify(string.Empty);
                return;
            }

            // Both figures are bytes, which is what the cap counts; the decoded string's length would
            // be UTF-16 code units.
            empty.text = body.IsTruncated
                ? $"Showing the first {WebRequestProfilerFormatting.FormatBytes(body.capturedLength)} of {WebRequestProfilerFormatting.FormatBytes(body.totalLength)}."
                : string.Empty;
            empty.style.display = body.IsTruncated ? DisplayStyle.Flex : DisplayStyle.None;

            text.style.display = DisplayStyle.Flex;
            text.SetValueWithoutNotify(body.text);
        }

        static void PopulateHeaders(Foldout foldout, WebRequestProfilerRecord record, WebRequestProfilerHeaderKind kind)
        {
            foldout.Clear();

            var count = 0;
            if (record.headers != null)
            {
                foreach (var header in record.headers)
                {
                    if (header.kind != kind)
                        continue;

                    var field = new VisualElement();
                    field.AddToClassList("web-request-details-view__field");

                    var name = new Label(header.name);
                    name.AddToClassList("web-request-details-view__field-label");

                    var value = new Label(header.value);
                    value.AddToClassList("web-request-details-view__field-value");

                    field.Add(name);
                    field.Add(value);
                    foldout.Add(field);
                    ++count;
                }
            }

            if (count == 0)
            {
                // Headers are recorded when the request finishes, so an in-flight row genuinely has
                // none yet - say so rather than showing an empty section that looks broken. A record
                // that has any header set has been reported on, so an empty section of one kind means
                // the request carried none of that kind: the usual case is a request with response
                // headers and no custom request headers, which must not read as still waiting.
                var text = record.headers != null ? "None captured." : "None captured yet.";
                var empty = new Label(text);
                empty.AddToClassList("web-request-details-view__inspector-empty");
                foldout.Add(empty);
            }
        }

        // The transport reports cumulative marks; what matters is how long each phase took, so show the
        // gaps between them. A mark of zero means the phase never happened - no TLS, or a reused
        // connection - which reads as "skipped" rather than as having taken no time.
        void PopulateTiming(WebRequestProfilerRecord record)
        {
            m_TimingPhases.Clear();

            if (!record.hasTimings)
            {
                m_TimingEmpty.style.display = DisplayStyle.Flex;
                m_TimingEmpty.text = record.state == WebRequestProfilerState.InFlight
                    ? "Timings are measured when the request finishes."
                    : "This request's transport does not measure per-phase timings.";
                return;
            }

            m_TimingEmpty.style.display = DisplayStyle.None;

            var timings = record.timings;

            // Cumulative marks, in the order the phases occur. Names and per-phase colours follow the
            // Editor design system's palette, so the modifier is part of the contract with the two
            // themed stylesheets rather than a detail of this loop.
            //
            // Section is the design's grouping, and null repeats the section above rather than starting a
            // new one, so the headers fall out of this table instead of being tracked separately. The
            // design collapses DNS Lookup, Initial Connection and SSL into a single "Connection" row;
            // that variant was considered and rejected, because the rule is to expose every timing
            // detail curl gives us and the palette assigns those three their own colours. Its grouping is
            // kept, which is what "and Connection" in the first section name is doing.
            var phases = new (string Section, string Label, string Modifier, ulong Mark)[]
            {
                ("Resource Scheduling and Connection", "Queueing", "queueing", timings.queuedUsec),
                (null, "DNS Lookup", "dns-lookup", timings.nameLookupUsec),
                (null, "Initial Connection", "initial-connection", timings.connectUsec),
                (null, "TLS", "tls", timings.tlsHandshakeUsec),
                ("Request/Response", "Request sent", "request-sent", timings.preTransferUsec),
                (null, "Waiting for server response", "waiting", timings.startTransferUsec),
                (null, "Content Download", "content-download", timings.totalUsec),
            };

            var total = timings.totalUsec;

            var marks = new ulong[phases.Length];
            for (var i = 0; i < phases.Length; ++i)
                marks[i] = phases[i].Mark;

            var spans = ComputePhaseSpans(marks, timings.availableMarks);

            // Each section is its own panel, so rows are added to the panel rather than to the list.
            VisualElement panel = null;
            for (var i = 0; i < phases.Length; ++i)
            {
                var (section, label, modifier, _) = phases[i];
                var span = spans[i];

                if (section != null)
                {
                    panel = MakeSectionPanel(section);
                    m_TimingPhases.Add(panel);
                }

                panel.Add(span.skipped
                    ? MakePhaseRow(label, modifier, "Skipped", 0f, 0f, false)
                    : MakePhaseRow(label, modifier, FormatPhaseDuration(span.elapsedUsec),
                        span.start, span.width, true));
            }

            // Its own band below the panels rather than a row inside the last one, which is what the
            // design does and what stops it reading as another phase of Content Download.
            var totalRow = new VisualElement();
            totalRow.AddToClassList("web-request-details-view__phase-total");

            var totalLabel = new Label("Total Time");
            totalLabel.AddToClassList("web-request-details-view__phase-total-label");

            var totalValue = new Label(FormatPhaseDuration(total));
            totalValue.AddToClassList("web-request-details-view__phase-total-value");

            totalRow.Add(totalLabel);
            totalRow.Add(totalValue);
            m_TimingPhases.Add(totalRow);
        }

        // A panel per section, with its own header row carrying the section name and the "Duration"
        // column title. Rows are added to the returned element rather than to the list, so the panel's
        // background groups them the way the design does.
        static VisualElement MakeSectionPanel(string title)
        {
            var panel = new VisualElement();
            panel.AddToClassList("web-request-details-view__phase-section");

            var header = new VisualElement();
            header.AddToClassList("web-request-details-view__phase-section-header");

            var name = new Label(title);
            name.AddToClassList("web-request-details-view__phase-section-title");

            // Sits over the value column, which is what makes it a column heading rather than a caption.
            var duration = new Label("Duration");
            duration.AddToClassList("web-request-details-view__phase-section-duration");

            header.Add(name);
            header.Add(duration);
            panel.Add(header);
            return panel;
        }

        // Milliseconds to two decimals, which is what the Timing tab's design shows and what makes the
        // phases of one request comparable at a glance - a column mixing us and ms has to be read twice.
        // Deliberately not WebRequestProfilerFormatting.FormatDuration: that switches units so a
        // sub-millisecond request does not read as zero, and it is shared with the list's Time column.
        static string FormatPhaseDuration(ulong microseconds)
        {
            return $"{microseconds / 1000f:0.00} ms";
        }

        // Where one phase bar sits on the track, as fractions of the request's total duration.
        internal struct PhaseSpan
        {
            public ulong elapsedUsec;
            public float start;
            public float width;
            public bool skipped;
            // The transport could not read this mark at all, which the view says differently from
            // skipped: one is a phase that did not happen, the other a phase nobody measured.
            public bool unavailable;
        }

        // Split out of PopulateTiming so it can be tested: building the view needs a live panel, and the
        // arithmetic - not the colour - is the half of this that has gone wrong in practice. The bars
        // shipped left-aligned once, every phase starting at zero, which is a correct set of widths and
        // a useless picture.
        //
        // marks are cumulative microseconds from the start of the request, in phase order, with zero
        // meaning the phase never happened. availableMarks carries one bit per mark, in the same
        // order, saying whether the transport could read it - without which a zero cannot be told
        // apart from an unmeasured phase.
        internal static PhaseSpan[] ComputePhaseSpans(ulong[] marks, ulong availableMarks)
        {
            var spans = new PhaseSpan[marks.Length];

            // The last mark is the request total, and it is what every fraction is against. Taking it
            // from the array rather than the largest mark keeps a non-monotonic report from rescaling
            // the whole column.
            var total = marks.Length > 0 ? marks[marks.Length - 1] : 0ul;

            ulong previous = 0;
            for (var i = 0; i < marks.Length; ++i)
            {
                if ((availableMarks & (1ul << i)) == 0)
                {
                    // Unreadable on this transport, so it says nothing about the request either way and
                    // must not move the baseline - the phase after it still starts where the last
                    // measured one ended.
                    spans[i].unavailable = true;
                    continue;
                }

                if (marks[i] == 0)
                {
                    // Never happened, so it has no position either - a zero-width bar parked at the
                    // previous mark would read as an instant phase rather than an absent one.
                    spans[i].skipped = true;
                    continue;
                }

                // The marks should be monotonic, but a transport reporting them out of order must not
                // produce a negative duration.
                var elapsed = marks[i] > previous ? marks[i] - previous : 0ul;

                spans[i].elapsedUsec = elapsed;
                // Starts where the last phase that actually happened ended, which is what makes the
                // column read left to right as elapsed time.
                spans[i].start = total > 0 ? (float)previous / total : 0f;
                spans[i].width = total > 0 ? (float)elapsed / total : 0f;

                // Only ever forward. Rejecting a regressed mark is not enough on its own: moving the
                // baseline back to it would hand the *next* phase the regression as extra duration and
                // start it before a phase that has already finished.
                if (marks[i] > previous)
                    previous = marks[i];
            }

            return spans;
        }

        // start and fraction are both of the request's total duration, so the bars lay out as a
        // waterfall: each one begins where the phase before it ended. Width alone would left-align every
        // phase and lose the only thing the view is for - when each one happened.
        // A phase shorter than a pixel of track still has to be visible - a request whose queueing took
        // half a millisecond out of half a second is a real phase, and rounding it to nothing is how the
        // tab loses the small ones. Two pixels reads as a deliberate hairline rather than an artefact.
        const float k_MinPhaseBarPx = 2f;

        // The track's own width is not known until layout, so this is the fraction of it a hairline may
        // need. Reserving it keeps a bar that starts near the end from being clipped away entirely by
        // the track's overflow, at the cost of placing the very last sliver slightly early - which is a
        // better trade than drawing nothing.
        const float k_MaxPhaseStart = 0.98f;

        static VisualElement MakePhaseRow(string label, string modifier, string value, float start, float fraction,
            bool hasBar)
        {
            var root = new VisualElement();
            root.AddToClassList("web-request-details-view__phase");

            var name = new Label(label);
            name.AddToClassList("web-request-details-view__phase-label");

            var bar = new VisualElement();
            bar.AddToClassList("web-request-details-view__phase-bar");

            // No fill element at all rather than a zero-width one: a skipped phase and the Total row have
            // no bar, and a minimum width applied to an empty fill would draw them a stub that says the
            // phase happened.
            if (hasBar)
            {
                var fill = new VisualElement();
                fill.AddToClassList("web-request-details-view__phase-fill");
                if (modifier != null)
                    fill.AddToClassList($"web-request-details-view__phase-fill--{modifier}");

                var left = Mathf.Clamp(start, 0f, k_MaxPhaseStart);
                // Clamped against what is left of the track rather than against 1, so a phase running to
                // the end stops at the right edge instead of being cut off by the track's overflow.
                var width = Mathf.Clamp(fraction, 0f, 1f - left);
                fill.style.left = Length.Percent(left * 100f);
                fill.style.width = Length.Percent(width * 100f);
                fill.style.minWidth = k_MinPhaseBarPx;
                bar.Add(fill);
            }

            var amount = new Label(value);
            amount.AddToClassList("web-request-details-view__phase-value");

            root.Add(name);
            root.Add(bar);
            root.Add(amount);
            return root;
        }
    }
}
