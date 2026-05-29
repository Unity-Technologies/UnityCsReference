// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Shared right-side "Panel components" pane used by the UI Toolkit profiler details views.
    /// Owns a <see cref="TwoPaneSplitView"/> wrapping the host's main content on the left with a
    /// list of IPanelComponent objects (from PANEL_ENTRIES frame metadata) on the right, plus a
    /// <see cref="ToolbarToggle"/> that collapses/expands the right pane.
    /// </summary>
    internal sealed class PanelComponentsPaneController : IDisposable
    {
        const int k_MainThreadIndex = 0;
        const string k_PanelComponentRowNameLabelName = "uitoolkit-profiler-panel-component-name";
        const string k_PanelComponentRowPingButtonName = "uitoolkit-profiler-panel-component-ping";

        static class Strings
        {
            public static readonly string DetailViewModeTooltip = L10n.Tr("Show or hide UI Toolkit related data next to the panel timing list.");
            public static readonly string PanelComponentsPaneTitle = L10n.Tr("Panel components");
            public static readonly string PanelComponentsPaneTooltip = L10n.Tr("Side pane listing IPanelComponent objects from UI Toolkit frame metadata for the selected panel row.");
            public static readonly string StatusSelectPanelRow = L10n.Tr("Select a panel row on the left to list IPanelComponent objects stored in this frame's UI Toolkit metadata.");
            public static readonly string StatusNoMetadataEntry = L10n.Tr("No UI Toolkit panel metadata entry for this panel in the selected frame.");
            public static readonly string StatusNoPanelComponents = L10n.Tr("No panel components listed for this panel in metadata (expected for editor panels, or runtime panels with no IPanelComponent instances).");
            public static readonly string StatusNoBatchComponents = L10n.Tr("No IPanelComponent contributed to this batch (typical for flat panels with no panel-component-rooted commands).");
            public static readonly string StatusListEmptyCouldNotLoadDetails = L10n.Tr("Panel components are present in frame metadata but details could not be loaded for this frame.");
        }

        enum DetailsSplitMode { NoDetails = 0, RelatedData = 1 }

        sealed class PanelComponentListEntry
        {
            public EntityId entityId;
            public string displayName;
        }

        readonly string m_EditorPrefKey;
        readonly string m_ViewDataKeyPrefix;

        TwoPaneSplitView m_SplitView;
        VisualElement m_RelatedDataColumn;
        ToolbarToggle m_ToolbarToggle;
        ListView m_PanelComponentsListView;
        Label m_PanelComponentsHeading;
        Label m_KickReasonLabel; // Visible only when a batch row is selected.
        Label m_PanelComponentsListEmptyLabel;
        // ListView creates the none-element lazily during layout, so the first
        // SyncPanelComponentsListEmptyMessage call lands before the label exists. Cache the message
        // and apply it from CreatePanelComponentsListViewEmptyElement when the label is finally built.
        string m_PendingEmptyMessage;
        DetailsSplitMode m_DetailsSplitMode = DetailsSplitMode.RelatedData;
        IVisualElementScheduledItem m_RefreshScheduleItem;

        readonly Dictionary<EntityId, List<EntityId>> m_PanelToPanelComponentEntityIds = new();
        // Per-panel ordered owner per batch. Index N is the owners of the Nth PANEL_BATCH_METRICS
        // chunk for that panel (= passIndex on the batch row). Empty inner list when no
        // IPanelComponent contributed to the batch.
        readonly Dictionary<EntityId, List<List<EntityId>>> m_PanelToBatchOwners = new();
        readonly Stack<List<EntityId>> m_PanelComponentIdListPool = new();
        readonly List<PanelComponentListEntry> m_PanelComponentListEntries = new();
        readonly Stack<PanelComponentListEntry> m_PanelComponentListEntryPool = new();
        long m_LastFrameIndex = -1;
        // True when m_LastFrameIndex's frame data was captured by THIS editor process. Frames loaded
        // from a saved profile or streamed from a remote player resolve EntityIds against a different
        // session, so ping/double-click would silently no-op — we hide them instead.
        bool m_LastFrameIsFromCurrentEditorSession;

        EntityId m_SelectedPanelEntityId = EntityId.None;
        string m_SelectedPanelName;
        // -1 = panel-level selection; >= 0 = batch row selected (passIndex within the panel).
        int m_SelectedBatchIndex = -1;
        string m_SelectedBatchKickReasonText; // Pre-formatted by the view; empty when no batch selected.
        bool m_HasSelection;

        public PanelComponentsPaneController(string editorPrefSplitModeKey, string viewDataKeyPrefix)
        {
            m_EditorPrefKey = editorPrefSplitModeKey;
            m_ViewDataKeyPrefix = viewDataKeyPrefix;
        }

        /// <summary>
        /// Build the right pane and split view, appending the visibility toggle to <paramref name="toolbar"/>.
        /// Returns the <see cref="TwoPaneSplitView"/>; caller adds it to its root layout.
        /// </summary>
        public TwoPaneSplitView WireUp(VisualElement leftContent, Toolbar toolbar)
        {
            m_PanelComponentsListView = new ListView
            {
                fixedItemHeight = 18,
                makeItem = CreatePanelComponentListRow,
                bindItem = BindPanelComponentListRow,
                makeNoneElement = CreatePanelComponentsListViewEmptyElement,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
                viewDataKey = m_ViewDataKeyPrefix + "-panel-components-list",
            };
            m_PanelComponentsListView.itemsChosen += OnPanelComponentsItemsChosen;

            var panelComponentsPane = new VisualElement { name = m_ViewDataKeyPrefix + "-panel-components-pane" };
            panelComponentsPane.style.flexDirection = FlexDirection.Column;
            panelComponentsPane.style.flexGrow = 1;
            m_PanelComponentsHeading = new Label(Strings.PanelComponentsPaneTitle)
            {
                tooltip = Strings.PanelComponentsPaneTooltip,
                focusable = false,
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 4,
                    paddingTop = 4,
                    paddingRight = 4,
                    paddingBottom = 4,
                    whiteSpace = WhiteSpace.Normal,
                },
            };
            // Kick-reason explanation; visible only when a batch row is selected. Style mirrors the
            // heading but with normal font weight so it reads as supplementary context.
            m_KickReasonLabel = new Label
            {
                focusable = false,
                style =
                {
                    paddingLeft = 6,
                    paddingTop = 2,
                    paddingRight = 6,
                    paddingBottom = 4,
                    whiteSpace = WhiteSpace.Normal,
                    display = DisplayStyle.None,
                },
            };
            panelComponentsPane.Add(m_PanelComponentsHeading);
            panelComponentsPane.Add(m_PanelComponentsListView);
            panelComponentsPane.Add(m_KickReasonLabel);

            m_ToolbarToggle = new ToolbarToggle
            {
                text = string.Empty,
                tooltip = Strings.DetailViewModeTooltip,
                viewDataKey = m_ViewDataKeyPrefix + "-details-view-mode-toggle",
                style = { flexShrink = 0 },
            };
            m_ToolbarToggle.Add(new Image
            {
                image = EditorGUIUtility.LoadIconRequired("UnityEditor.InspectorWindow"),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            });
            m_ToolbarToggle.RegisterValueChangedCallback(evt =>
            {
                SetDetailsSplitMode(evt.newValue ? DetailsSplitMode.RelatedData : DetailsSplitMode.NoDetails);
            });
            toolbar.Add(m_ToolbarToggle);

            m_RelatedDataColumn = new VisualElement { name = m_ViewDataKeyPrefix + "-related-data-column" };
            m_RelatedDataColumn.viewDataKey = m_ViewDataKeyPrefix + "-related-data-column";
            m_RelatedDataColumn.style.flexDirection = FlexDirection.Column;
            m_RelatedDataColumn.style.flexGrow = 1;
            m_RelatedDataColumn.style.minWidth = 120;
            m_RelatedDataColumn.Add(panelComponentsPane);

            var leftColumn = new VisualElement { name = m_ViewDataKeyPrefix + "-main-column" };
            leftColumn.style.flexDirection = FlexDirection.Column;
            leftColumn.style.flexGrow = 1;
            leftColumn.style.minWidth = 0;
            leftColumn.Add(leftContent);

            m_SplitView = new TwoPaneSplitView(1, 280, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = m_ViewDataKeyPrefix + "-main-related-split",
                viewDataKey = m_ViewDataKeyPrefix + "-main-related-split",
            };
            m_SplitView.Add(leftColumn);
            m_SplitView.Add(m_RelatedDataColumn);

            var savedMode = (DetailsSplitMode)EditorPrefs.GetInt(m_EditorPrefKey, (int)DetailsSplitMode.RelatedData);
            if (savedMode != DetailsSplitMode.NoDetails && savedMode != DetailsSplitMode.RelatedData)
                savedMode = DetailsSplitMode.RelatedData;
            SetDetailsSplitMode(savedMode, persist: false);

            // Defer the initial collapse: TwoPaneSplitView.CollapseChild requires both children to
            // have completed their first layout pass.
            m_SplitView.schedule.Execute(() =>
            {
                if (m_DetailsSplitMode == DetailsSplitMode.NoDetails && m_SplitView != null)
                    m_SplitView.CollapseChild(1);
            });

            return m_SplitView;
        }

        /// <summary>Called from the host's ReloadData to load PANEL_ENTRIES + PANEL_BATCH_METRICS owner tails for the frame.</summary>
        public void LoadFrameMetadata(RawFrameDataView rawFrameData, long frameIndex)
        {
            ReleaseAllPanelComponentEntityIdLists();
            ReleaseAllBatchOwnerLists();
            m_LastFrameIndex = frameIndex;
            m_LastFrameIsFromCurrentEditorSession = false;

            if (rawFrameData == null || !rawFrameData.valid)
            {
                ScheduleRefreshPanelComponentsPane();
                return;
            }

            m_LastFrameIsFromCurrentEditorSession = ProfilerDriver.FrameDataBelongsToCurrentEditorSession(rawFrameData);

            var guid = ProfilerUIToolkit.kProfilerMetadataGuid;
            var panelEntriesTag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelEntries;
            var panelEntriesChunkCount = rawFrameData.GetFrameMetaDataCount(guid, panelEntriesTag);
            for (var chunkIndex = 0; chunkIndex < panelEntriesChunkCount; chunkIndex++)
            {
                using (var ids = rawFrameData.GetFrameMetaData<EntityId>(guid, panelEntriesTag, chunkIndex))
                {
                    if (ids.Length < 1)
                        continue;
                    var panelEntityId = ids[0];
                    if (!m_PanelToPanelComponentEntityIds.TryGetValue(panelEntityId, out var list))
                    {
                        list = RentPanelComponentIdList();
                        m_PanelToPanelComponentEntityIds[panelEntityId] = list;
                    }
                    for (var j = 1; j < ids.Length; j++)
                    {
                        if (ids[j] != EntityId.None)
                            list.Add(ids[j]);
                    }
                }
            }

            // PANEL_BATCH_METRICS + PANEL_BATCH_OWNERS: one chunk pair per panel render.
            //   - PANEL_BATCH_METRICS: NativeArray<UIToolkitBatchMetricsInfo>, one element per batch.
            //   - PANEL_BATCH_OWNERS:  NativeArray<EntityId>, sliced by each batch's
            //                          (ownerOffset, ownerCount).
            // The two tags are paired by per-frame ordinal — both arrays for panel i live at
            // chunk index i.
            var batchMetricsTag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelBatchMetrics;
            var batchOwnersTag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelBatchOwners;
            var batchMetricsChunkCount = rawFrameData.GetFrameMetaDataCount(guid, batchMetricsTag);
            Debug.Assert(batchMetricsChunkCount == rawFrameData.GetFrameMetaDataCount(guid, batchOwnersTag),
                "PANEL_BATCH_METRICS and PANEL_BATCH_OWNERS chunk counts must match — emitter pairs them per panel.");
            for (var chunkIndex = 0; chunkIndex < batchMetricsChunkCount; chunkIndex++)
            {
                using (var batches = rawFrameData.GetFrameMetaData<UIToolkitBatchMetricsInfo>(guid, batchMetricsTag, chunkIndex))
                using (var owners = rawFrameData.GetFrameMetaData<EntityId>(guid, batchOwnersTag, chunkIndex))
                {
                    if (batches.Length < 1)
                        continue;
                    var panelId = batches[0].panelEntityId;
                    if (!m_PanelToBatchOwners.TryGetValue(panelId, out var perBatchLists))
                    {
                        perBatchLists = new List<List<EntityId>>(batches.Length);
                        m_PanelToBatchOwners[panelId] = perBatchLists;
                    }

                    for (var bi = 0; bi < batches.Length; bi++)
                    {
                        var info = batches[bi];
                        var batchOwners = RentPanelComponentIdList();
                        var end = (int)(info.ownerOffset + info.ownerCount);
                        if (end > owners.Length)
                            end = owners.Length;
                        for (var i = (int)info.ownerOffset; i < end; i++)
                        {
                            if (owners[i] != EntityId.None)
                                batchOwners.Add(owners[i]);
                        }
                        perBatchLists.Add(batchOwners);
                    }
                }
            }

            ScheduleRefreshPanelComponentsPane();
        }

        /// <summary>
        /// Returns the IPanelComponent owners contributing to a specific batch (by panel + passIndex),
        /// or null if no data is available. Read-only — do not mutate the returned list.
        /// </summary>
        public IReadOnlyList<EntityId> GetBatchOwners(EntityId panelEntityId, int batchIndex)
        {
            if (m_PanelToBatchOwners.TryGetValue(panelEntityId, out var perBatchLists)
                && batchIndex >= 0 && batchIndex < perBatchLists.Count)
                return perBatchLists[batchIndex];
            return null;
        }

        public void SetSelectedPanel(EntityId panelEntityId, string panelDisplayName)
        {
            m_HasSelection = true;
            m_SelectedPanelEntityId = panelEntityId;
            m_SelectedPanelName = panelDisplayName;
            m_SelectedBatchIndex = -1;
            m_SelectedBatchKickReasonText = null;
            ScheduleRefreshPanelComponentsPane();
        }

        /// <summary>
        /// Select a batch row. <paramref name="batchIndex"/> = passIndex within the parent panel.
        /// <paramref name="kickReasonText"/> is pre-formatted by the caller (the view knows the
        /// kick-reason flag names) and shown verbatim under the heading.
        /// </summary>
        public void SetSelectedBatch(EntityId panelEntityId, string panelDisplayName, int batchIndex, string kickReasonText)
        {
            m_HasSelection = true;
            m_SelectedPanelEntityId = panelEntityId;
            m_SelectedPanelName = panelDisplayName;
            m_SelectedBatchIndex = batchIndex;
            m_SelectedBatchKickReasonText = kickReasonText;
            ScheduleRefreshPanelComponentsPane();
        }

        public void ClearSelection()
        {
            m_HasSelection = false;
            m_SelectedPanelEntityId = EntityId.None;
            m_SelectedPanelName = null;
            m_SelectedBatchIndex = -1;
            m_SelectedBatchKickReasonText = null;
            ScheduleRefreshPanelComponentsPane();
        }

        public void Dispose()
        {
            if (m_PanelComponentsListView != null)
                m_PanelComponentsListView.itemsChosen -= OnPanelComponentsItemsChosen;
            m_RefreshScheduleItem?.Pause();
            m_RefreshScheduleItem = null;
        }

        void SetDetailsSplitMode(DetailsSplitMode mode, bool persist = true)
        {
            if (m_SplitView == null || m_ToolbarToggle == null)
                return;

            m_DetailsSplitMode = mode;
            if (persist)
                EditorPrefs.SetInt(m_EditorPrefKey, (int)mode);

            m_ToolbarToggle.SetValueWithoutNotify(mode == DetailsSplitMode.RelatedData);

            if (mode == DetailsSplitMode.RelatedData)
            {
                m_SplitView.UnCollapse();
                ScheduleRefreshPanelComponentsPane();
            }
            else
                m_SplitView.CollapseChild(1);
        }

        // Profiler frame changes can fire during layout (IMGUI toolbar), and rebuilding ListView then
        // throws "Cannot modify VisualElement hierarchy during layout calculation". Defer to next tick.
        void ScheduleRefreshPanelComponentsPane()
        {
            if (m_PanelComponentsListView == null)
                return;

            m_RefreshScheduleItem?.Pause();
            m_RefreshScheduleItem = m_PanelComponentsListView.schedule.Execute(() =>
            {
                m_RefreshScheduleItem = null;
                RefreshPanelComponentsPane();
            });
        }

        void RefreshPanelComponentsPane()
        {
            if (m_PanelComponentsListView == null || m_DetailsSplitMode != DetailsSplitMode.RelatedData)
                return;

            SetPanelComponentsHeadingForSelection();

            if (!m_HasSelection)
            {
                ResetListAndShowEmptyMessage(Strings.StatusSelectPanelRow);
                return;
            }

            // Pick the right component-id source for the selection scope.
            IReadOnlyList<EntityId> componentIds;
            if (m_SelectedBatchIndex >= 0)
            {
                componentIds = GetBatchOwners(m_SelectedPanelEntityId, m_SelectedBatchIndex);
                if (componentIds == null)
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoMetadataEntry);
                    return;
                }
                if (componentIds.Count == 0)
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoBatchComponents);
                    return;
                }
            }
            else
            {
                if (!m_PanelToPanelComponentEntityIds.TryGetValue(m_SelectedPanelEntityId, out var panelList))
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoMetadataEntry);
                    return;
                }
                if (panelList.Count == 0)
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoPanelComponents);
                    return;
                }
                componentIds = panelList;
            }

            var neededCount = componentIds.Count;
            TrimExcessPanelComponentListEntries(neededCount);
            if (m_LastFrameIndex >= 0)
            {
                using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)m_LastFrameIndex, k_MainThreadIndex))
                {
                    if (rawFrameData.valid)
                    {
                        for (var i = 0; i < neededCount; i++)
                        {
                            PanelComponentListEntry entry;
                            if (i < m_PanelComponentListEntries.Count)
                                entry = m_PanelComponentListEntries[i];
                            else
                            {
                                entry = RentPanelComponentListEntry();
                                m_PanelComponentListEntries.Add(entry);
                            }

                            var componentId = componentIds[i];
                            entry.entityId = componentId;
                            entry.displayName = UIToolkitProfilerToolbarHelpers.GetPanelDisplayName(rawFrameData, componentId);
                        }
                    }
                    else
                        ClearPanelComponentListEntriesReturningToPool();
                }
            }
            else
                ClearPanelComponentListEntriesReturningToPool();

            m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
            m_PanelComponentsListView.Rebuild();
            if (m_PanelComponentListEntries.Count == 0)
                SyncPanelComponentsListEmptyMessage(Strings.StatusListEmptyCouldNotLoadDetails);
        }

        void ResetListAndShowEmptyMessage(string message)
        {
            ClearPanelComponentListEntriesReturningToPool();
            m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
            m_PanelComponentsListView.Rebuild();
            SyncPanelComponentsListEmptyMessage(message);
        }

        void SetPanelComponentsHeadingForSelection()
        {
            if (m_PanelComponentsHeading == null)
                return;

            // Toggle the kick-reason label based on selection scope.
            if (m_KickReasonLabel != null)
            {
                var hasReason = m_SelectedBatchIndex >= 0 && !string.IsNullOrEmpty(m_SelectedBatchKickReasonText);
                m_KickReasonLabel.style.display = hasReason ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasReason)
                    m_KickReasonLabel.text = m_SelectedBatchKickReasonText;
            }

            if (!m_HasSelection)
            {
                m_PanelComponentsHeading.text = Strings.PanelComponentsPaneTitle;
                return;
            }

            var panelName = m_SelectedPanelName ?? string.Empty;

            // Batch-row scope: heading shows the batch index alongside the panel name; the count
            // refers to the batch's own contributors.
            if (m_SelectedBatchIndex >= 0)
            {
                var scopeLabel = string.Format(L10n.Tr("{0} (batch #{1})"), panelName, m_SelectedBatchIndex);
                var batchOwners = GetBatchOwners(m_SelectedPanelEntityId, m_SelectedBatchIndex);
                if (batchOwners == null || batchOwners.Count == 0)
                {
                    m_PanelComponentsHeading.text = string.Format(L10n.Tr("{0}: {1}"), Strings.PanelComponentsPaneTitle, scopeLabel);
                    return;
                }
                var batchCount = batchOwners.Count;
                m_PanelComponentsHeading.text = batchCount == 1
                    ? string.Format(L10n.Tr("{0} panel component used in {1}"), batchCount, scopeLabel)
                    : string.Format(L10n.Tr("{0} panel components used in {1}"), batchCount, scopeLabel);
                return;
            }

            // Panel-row scope: list of all panel components.
            if (!m_PanelToPanelComponentEntityIds.TryGetValue(m_SelectedPanelEntityId, out var componentIds) || componentIds.Count == 0)
            {
                m_PanelComponentsHeading.text = string.Format(L10n.Tr("{0}: {1}"), Strings.PanelComponentsPaneTitle, panelName);
                return;
            }

            var count = componentIds.Count;
            m_PanelComponentsHeading.text = count == 1
                ? string.Format(L10n.Tr("{0} panel component using {1}"), count, panelName)
                : string.Format(L10n.Tr("{0} panel components using {1}"), count, panelName);
        }

        void ReleaseAllPanelComponentEntityIdLists()
        {
            foreach (var list in m_PanelToPanelComponentEntityIds.Values)
            {
                list.Clear();
                m_PanelComponentIdListPool.Push(list);
            }
            m_PanelToPanelComponentEntityIds.Clear();
        }

        void ReleaseAllBatchOwnerLists()
        {
            foreach (var perBatchLists in m_PanelToBatchOwners.Values)
            {
                for (var i = 0; i < perBatchLists.Count; i++)
                {
                    var inner = perBatchLists[i];
                    inner.Clear();
                    m_PanelComponentIdListPool.Push(inner);
                }
                perBatchLists.Clear();
            }
            m_PanelToBatchOwners.Clear();
        }

        List<EntityId> RentPanelComponentIdList()
            => m_PanelComponentIdListPool.Count > 0 ? m_PanelComponentIdListPool.Pop() : new List<EntityId>();

        void ClearPanelComponentListEntriesReturningToPool()
        {
            for (var i = 0; i < m_PanelComponentListEntries.Count; i++)
                m_PanelComponentListEntryPool.Push(m_PanelComponentListEntries[i]);
            m_PanelComponentListEntries.Clear();
        }

        PanelComponentListEntry RentPanelComponentListEntry()
            => m_PanelComponentListEntryPool.Count > 0 ? m_PanelComponentListEntryPool.Pop() : new PanelComponentListEntry();

        void TrimExcessPanelComponentListEntries(int targetCount)
        {
            while (m_PanelComponentListEntries.Count > targetCount)
            {
                var lastIndex = m_PanelComponentListEntries.Count - 1;
                m_PanelComponentListEntryPool.Push(m_PanelComponentListEntries[lastIndex]);
                m_PanelComponentListEntries.RemoveAt(lastIndex);
            }
        }

        VisualElement CreatePanelComponentsListViewEmptyElement()
        {
            var label = new Label
            {
                focusable = false,
                text = m_PendingEmptyMessage ?? string.Empty,
                style =
                {
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    whiteSpace = WhiteSpace.Normal,
                    flexGrow = 1,
                },
            };
            m_PanelComponentsListEmptyLabel = label;
            return label;
        }

        void SyncPanelComponentsListEmptyMessage(string message)
        {
            m_PendingEmptyMessage = message;
            if (m_PanelComponentsListEmptyLabel != null)
                m_PanelComponentsListEmptyLabel.text = message;
        }

        VisualElement CreatePanelComponentListRow()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexGrow = 1;
            container.style.alignItems = Align.Center;
            container.style.minWidth = 0;

            var nameLabel = new Label { name = k_PanelComponentRowNameLabelName, focusable = false };
            nameLabel.style.flexGrow = 1;
            nameLabel.style.flexShrink = 1;
            nameLabel.style.minWidth = 0;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.textOverflow = TextOverflow.Ellipsis;

            var pingBtn = new UnityEngine.UIElements.Button { name = k_PanelComponentRowPingButtonName, text = string.Empty };
            pingBtn.style.backgroundImage = Background.FromTexture2D(EditorGUIUtility.LoadIconRequired("UIPackageResources/Images/pick_uielements.png"));
            pingBtn.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            pingBtn.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            pingBtn.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            pingBtn.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            pingBtn.style.alignItems = Align.FlexEnd;
            pingBtn.tooltip = UIToolkitProfilerToolbarHelpers.PingTooltip;
            pingBtn.style.width = 22;
            pingBtn.style.minWidth = 22;
            pingBtn.style.maxWidth = 22;
            pingBtn.style.flexShrink = 0;
            pingBtn.clicked += () =>
            {
                if (container.userData is int rowIndex && rowIndex >= 0 && rowIndex < m_PanelComponentListEntries.Count)
                    m_PanelComponentsListView.SetSelection(rowIndex);
                if (pingBtn.userData is EntityId id && id != EntityId.None)
                    UIToolkitProfilerToolbarHelpers.PingEntity(id);
            };

            container.Add(nameLabel);
            container.Add(pingBtn);
            return container;
        }

        void BindPanelComponentListRow(VisualElement element, int index)
        {
            var nameLabel = element.Q<Label>(k_PanelComponentRowNameLabelName);
            var pingBtn = element.Q<UnityEngine.UIElements.Button>(k_PanelComponentRowPingButtonName);
            if (nameLabel == null || pingBtn == null)
                return;

            if (index >= 0 && index < m_PanelComponentListEntries.Count)
            {
                var entry = m_PanelComponentListEntries[index];
                element.userData = index;
                nameLabel.text = entry.displayName;
                pingBtn.userData = entry.entityId;
                var pingable = m_LastFrameIsFromCurrentEditorSession && entry.entityId != EntityId.None;
                pingBtn.style.display = pingable ? DisplayStyle.Flex : DisplayStyle.None;
                pingBtn.SetEnabled(pingable);
            }
            else
            {
                element.userData = -1;
                nameLabel.text = string.Empty;
                pingBtn.userData = null;
                pingBtn.style.display = DisplayStyle.None;
                pingBtn.SetEnabled(false);
            }
        }

        void OnPanelComponentsItemsChosen(IEnumerable<object> chosen)
        {
            if (!m_LastFrameIsFromCurrentEditorSession)
                return;
            foreach (var item in chosen)
            {
                if (item is PanelComponentListEntry entry && entry.entityId != EntityId.None)
                    UIToolkitProfilerToolbarHelpers.PingEntity(entry.entityId);
                break;
            }
        }
    }
}
