// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling.Editor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Details view backing <see cref="UIToolkitDetailProfilerModule"/>. Shows a
    /// <see cref="MultiColumnTreeView"/> of panel rows whose children are individual batch
    /// (EvaluateChain pass) records. Rows are built from PANEL_METRICS and PANEL_BATCH_METRICS
    /// frame metadata emitted natively by <c>ProfilerUIToolkit::AddPanelUpdateMetrics</c>.
    /// </summary>
    internal class UIToolkitDetailsProfilerModuleDetailsView : ProfilerModuleViewController
    {
        const int k_MainThreadIndex = 0;
        const string k_DetailsSplitModeEditorPrefKey = "UIToolkitDetailsModule.DetailsSplitMode";
        const string k_ViewDataKeyPrefix = "uitoolkit-details-module";

        // Single row type for both panel parents and batch children — keeps Column.bindCell simple
        // since MultiColumnTreeView shares cell prototypes across levels.
        struct TreeRowData
        {
            public bool isBatch;          // false = panel root, true = batch child
            public string panelName;      // populated on both row types so batch rows can pass it to the panel-components pane
            public EntityId panelEntityId;
            public int passIndex;         // batch-only: 0-based index of EvaluateChain call within the panel

            // Common
            public uint drawCallCount;
            public uint vertexCount;
            public uint indexCount;

            // Panel-only
            public uint batchCount;       // number of batch children for this panel
            public uint hierarchyVersionChanges;
            public uint repaintVersionChanges;
            public int visualElementCount;

            // Batch-only
            public uint immediateDraws;
            public uint drawRangeCount;
            public uint kickRangesReason;
            public bool isRenderingNestedTreeRT;
            public string ownerName; // resolved IPanelComponent name, or empty for batches with no owner (flat panels)
        }

        // Mirrors UIRenderDevice.KickRangesReason — kept local to avoid leaking the enum across
        // assemblies just for a debug string formatter. Bit order MUST match the enum.
        static readonly (string Name, string Explanation)[] k_BreakingReasons =
        {
            ("Material Change",         "A different material was needed for the next draw."),                                  // 1 << 0
            ("Page Change",             "The next draw used a mesh on a different vertex/index page."),                         // 1 << 1
            ("Texture Slots Exhausted", "All available texture slots were in use; no room for the next draw's texture."),       // 1 << 2
            ("Stencil Ref Change",      "The stencil reference value changed (typically a clipping mask boundary)."),           // 1 << 3
            ("Break Batches (debug)",   "Break Batches debug toggle is on — every command forces a break."),                    // 1 << 4
            ("Ranges Buffer Full",      "The internal draw-range buffer ran out of room mid-pass and had to flush."),           // 1 << 5
            ("Immediate Command",       "An Immediate (or ImmediateCull) command interrupted the chain."),                      // 1 << 6
            ("Render Chain Cut",        "A CutRenderChain command (new render chain owner / world-space root)."),               // 1 << 7
            ("Default Material Change", "A Push/PopDefaultMaterial command interrupted the chain."),                            // 1 << 8
            ("Scissor Change",          "A Push/PopScissor command changed the clip rectangle."),                               // 1 << 9
            ("View Change",             "A Push/PopView command changed the view matrix."),                                     // 1 << 10
        };

        readonly List<TreeViewItemData<TreeRowData>> m_RootItems = new();
        MultiColumnTreeView m_TreeView;
        IVisualElementScheduledItem m_RebuildScheduled;

        // Last user-selected row — preserved across frame changes so navigating frames keeps the
        // same panel (or batch within the panel) highlighted instead of dropping selection on every
        // Rebuild. Panel name is cached for the panel-components heading. For batch rows
        // m_SelectedPanelEntityId still points at the parent panel; m_SelectedPassIndex picks the
        // child within it.
        EntityId m_SelectedPanelEntityId = EntityId.None;
        string m_SelectedPanelName;
        bool m_SelectedIsBatch;
        int m_SelectedPassIndex;

        // Panels NOT in this set are expanded (default = expanded, matching the original
        // ExpandAll behaviour). Tracked by EntityId so user-driven collapse survives the row-id
        // reassignment that happens each ReloadData.
        readonly HashSet<EntityId> m_CollapsedPanelEntityIds = new();

        PanelComponentsPaneController m_PanelComponentsPane;

        public UIToolkitDetailsProfilerModuleDetailsView(ProfilerWindow profilerWindow)
            : base(profilerWindow) { }

        protected override VisualElement CreateView()
        {
            var columns = new Columns { reorderable = true };

            // Column visibility per row scope:
            //   alwaysShown: true            → meaningful on both panel rows (sum across batches) and batch rows (per-batch).
            //   panelOnly: true              → batch rows show "—" (panel-level only — e.g. version changes, VE count).
            //   batchOnly: true              → panel rows show "—" (per-batch only).
            // Keep panel-row sums in sync with the batch-row source in ReloadData when adding/removing alwaysShown columns.
            columns.Add(MakeNameColumn());
            columns.Add(MakeUIntColumn("batches", L10n.Tr("Batches"), r => r.batchCount, alwaysShown: false, panelOnly: true,
                tooltip: L10n.Tr("Number of batches emitted by this panel during EvaluateChain. Each batch is one row of the children below.")));
            columns.Add(MakeUIntColumn("drawCalls", L10n.Tr("Draw Calls"), r => r.drawCallCount, alwaysShown: true,
                tooltip: L10n.Tr("Number of Draw commands processed. Panel rows show the sum across all batches; batch rows show the count for that batch only.")));
            columns.Add(MakeUIntColumn("vertices", L10n.Tr("Vertices"), r => r.vertexCount, alwaysShown: true,
                tooltip: L10n.Tr("Total vertices referenced by Draw commands. Sum across batches on panel rows; per-batch on batch rows.")));
            columns.Add(MakeUIntColumn("indices", L10n.Tr("Indices"), r => r.indexCount, alwaysShown: true,
                tooltip: L10n.Tr("Total indices submitted by Draw commands. Sum across batches on panel rows; per-batch on batch rows.")));
            columns.Add(MakeUIntColumn("immediateDraws", L10n.Tr("Imm Draws"), r => r.immediateDraws, alwaysShown: true,
                tooltip: L10n.Tr("Immediate-mode draws (Immediate / ImmediateCull commands). Panel rows show the sum across all batches; batch rows show the count for that batch only. Each one breaks batching.")));
            columns.Add(MakeUIntColumn("drawRanges", L10n.Tr("Draw Ranges"), r => r.drawRangeCount, alwaysShown: true,
                tooltip: L10n.Tr("Number of contiguous draw ranges stashed before each batch was broken. Sum across batches on panel rows; per-batch on batch rows. Many small ranges per batch can indicate fragmentation.")));
            columns.Add(MakeBreakingReasonColumn());
            columns.Add(MakeOwnerColumn());
            columns.Add(MakeUIntColumn("hierarchyChanges", L10n.Tr("Hierarchy Changes"), r => r.hierarchyVersionChanges, alwaysShown: false, panelOnly: true,
                tooltip: L10n.Tr("Number of hierarchy version changes (add/remove/reparent of VisualElements) since the previous frame.")));
            columns.Add(MakeUIntColumn("repaintChanges", L10n.Tr("Repaint Changes"), r => r.repaintVersionChanges, alwaysShown: false, panelOnly: true,
                tooltip: L10n.Tr("Number of repaint version changes since the previous frame. High values indicate elements are being marked dirty frequently.")));
            columns.Add(MakeIntColumn("veCount", L10n.Tr("VE Count"), r => r.visualElementCount, panelOnly: true,
                tooltip: L10n.Tr("Total number of VisualElements in this panel's hierarchy.")));

            m_TreeView = new MultiColumnTreeView(columns)
            {
                fixedItemHeight = 18,
                sortingMode = ColumnSortingMode.Default,
                viewDataKey = "uitoolkit-details-module-treeview",
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
            };
            m_TreeView.itemsChosen += OnItemsChosen;
            m_TreeView.selectedIndicesChanged += OnSelectionChanged;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarSpacer { flex = true });
            UIToolkitProfilerToolbarHelpers.AddCommonButtons(toolbar);

            m_PanelComponentsPane = new PanelComponentsPaneController(k_DetailsSplitModeEditorPrefKey, k_ViewDataKeyPrefix);
            var splitView = m_PanelComponentsPane.WireUp(m_TreeView, toolbar);

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            var root = new VisualElement { name = "uitoolkit-details-module-root" };
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.Add(toolbar);
            root.Add(splitView);
            return root;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            m_RebuildScheduled?.Pause();
            m_RebuildScheduled = null;
            if (m_TreeView != null)
            {
                m_TreeView.itemsChosen -= OnItemsChosen;
                m_TreeView.selectedIndicesChanged -= OnSelectionChanged;
            }
            m_PanelComponentsPane?.Dispose();
            m_PanelComponentsPane = null;
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnSelectionChanged(IEnumerable<int> selectedIndices)
        {
            // Track the selected panel (parent panel for batch rows). When a batch row is selected
            // the right pane scopes to that batch's owners and shows the breaking-reason explanation;
            // panel rows show the full panel component list.
            foreach (var index in selectedIndices)
            {
                var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                m_SelectedPanelEntityId = data.panelEntityId;
                m_SelectedPanelName = data.panelName;
                m_SelectedIsBatch = data.isBatch;
                m_SelectedPassIndex = data.passIndex;
                if (data.isBatch)
                    m_PanelComponentsPane?.SetSelectedBatch(data.panelEntityId, data.panelName, data.passIndex, FormatBreakingReasonExplanation(data.kickRangesReason));
                else
                    m_PanelComponentsPane?.SetSelectedPanel(data.panelEntityId, data.panelName);
                return;
            }
            m_SelectedPanelEntityId = EntityId.None;
            m_SelectedPanelName = null;
            m_SelectedIsBatch = false;
            m_SelectedPassIndex = 0;
            m_PanelComponentsPane?.ClearSelection();
        }

        void RestoreSelection()
        {
            if (m_TreeView == null || m_SelectedPanelEntityId == EntityId.None)
            {
                // No (or no longer relevant) selection — make sure the right pane reflects that.
                m_PanelComponentsPane?.ClearSelection();
                return;
            }

            // Walk root items to find the matching panel; tree IDs are reassigned each ReloadData
            // (sequential ints) so we have to map back via EntityId (and passIndex for batches).
            for (var i = 0; i < m_RootItems.Count; i++)
            {
                if (m_RootItems[i].data.panelEntityId != m_SelectedPanelEntityId)
                    continue;

                // Pick up the freshest panel name from the rebuilt rows (cached one may be stale).
                m_SelectedPanelName = m_RootItems[i].data.panelName;

                if (m_SelectedIsBatch && m_RootItems[i].children != null)
                {
                    foreach (var child in m_RootItems[i].children)
                    {
                        if (child.data.passIndex != m_SelectedPassIndex)
                            continue;
                        m_TreeView.SetSelectionByIdWithoutNotify(new[] { child.id });
                        m_PanelComponentsPane?.SetSelectedBatch(m_SelectedPanelEntityId, m_SelectedPanelName, m_SelectedPassIndex, FormatBreakingReasonExplanation(child.data.kickRangesReason));
                        return;
                    }
                    // Batch index didn't survive this frame — fall through to selecting the panel row.
                }

                m_TreeView.SetSelectionByIdWithoutNotify(new[] { m_RootItems[i].id });
                m_PanelComponentsPane?.SetSelectedPanel(m_SelectedPanelEntityId, m_SelectedPanelName);
                return;
            }

            // Selection didn't survive this frame's data — show the "no metadata for this panel" state.
            m_PanelComponentsPane?.SetSelectedPanel(m_SelectedPanelEntityId, m_SelectedPanelName);
        }

        // Snapshot expanded/collapsed state of the current root items into m_CollapsedPanelEntityIds
        // before we throw the row IDs away in ReloadData. Default = expanded, so we only remember
        // the collapsed ones — newly-discovered panels in the next frame stay expanded.
        void CaptureExpandedState()
        {
            if (m_TreeView == null)
                return;
            for (var i = 0; i < m_RootItems.Count; i++)
            {
                var item = m_RootItems[i];
                if (m_TreeView.IsExpanded(item.id))
                    m_CollapsedPanelEntityIds.Remove(item.data.panelEntityId);
                else
                    m_CollapsedPanelEntityIds.Add(item.data.panelEntityId);
            }
        }

        // Re-apply user-driven collapse state after SetRootItems. New panels (never seen before)
        // default to expanded so the tree behaves like the original ExpandAll on first encounter.
        void ApplyExpansion()
        {
            if (m_TreeView == null)
                return;
            for (var i = 0; i < m_RootItems.Count; i++)
            {
                var item = m_RootItems[i];
                if (m_CollapsedPanelEntityIds.Contains(item.data.panelEntityId))
                    m_TreeView.CollapseItem(item.id);
                else
                    m_TreeView.ExpandItem(item.id);
            }
        }

        // Attach the shared default-header content + tooltip binding so headers match the styling used by
        // UIToolkitProfilerModuleDetailsView (icon slot, title USS classes, hover tooltip on the column).
        static void AttachHeaderTooltip(Column column, string tooltip)
        {
            if (tooltip == null)
                return;
            column.makeHeader = UIToolkitProfilerToolbarHelpers.CreateDefaultColumnHeaderContent;
            column.bindHeader = ve => UIToolkitProfilerToolbarHelpers.BindColumnHeaderWithTooltip(ve, column, tooltip);
        }

        // Comparison callbacks receive displayed-item indices; the controller already restricts
        // the comparison to siblings of the same parent so root and child rows don't intermix.
        Column MakeNameColumn()
        {
            var column = new Column
            {
                name = "name",
                title = L10n.Tr("Panel / Batch"),
                minWidth = 180,
                optional = false,
                width = 220,
                makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    label.text = data.isBatch
                        ? (data.isRenderingNestedTreeRT
                            ? string.Format(L10n.Tr("Batch #{0} (nested RT)"), data.passIndex)
                            : string.Format(L10n.Tr("Batch #{0}"), data.passIndex))
                        : data.panelName;
                },
                comparison = (a, b) =>
                {
                    var da = m_TreeView.GetItemDataForIndex<TreeRowData>(a);
                    var db = m_TreeView.GetItemDataForIndex<TreeRowData>(b);
                    if (da.isBatch && db.isBatch)
                        return da.passIndex.CompareTo(db.passIndex);
                    return string.CompareOrdinal(da.panelName ?? string.Empty, db.panelName ?? string.Empty);
                },
            };
            AttachHeaderTooltip(column, L10n.Tr("Panel root rows aggregate all of the panel's batches for the selected frame. Each child row is one batch inside that panel's render pass."));
            return column;
        }

        Column MakeUIntColumn(string name, string title, Func<TreeRowData, uint> getter,
                              bool alwaysShown, bool panelOnly = false, bool batchOnly = false, string tooltip = null)
        {
            var column = new Column
            {
                name = name,
                title = title,
                minWidth = 70,
                width = 100,
                makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleRight } },
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    if (!alwaysShown && ((panelOnly && data.isBatch) || (batchOnly && !data.isBatch)))
                        label.text = "—";
                    else
                        label.text = getter(data).ToString("N0");
                },
                comparison = (a, b) => getter(m_TreeView.GetItemDataForIndex<TreeRowData>(a))
                    .CompareTo(getter(m_TreeView.GetItemDataForIndex<TreeRowData>(b))),
            };
            AttachHeaderTooltip(column, tooltip);
            return column;
        }

        Column MakeIntColumn(string name, string title, Func<TreeRowData, int> getter, bool panelOnly, string tooltip = null)
        {
            var column = new Column
            {
                name = name,
                title = title,
                minWidth = 70,
                width = 100,
                makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleRight } },
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    if (panelOnly && data.isBatch)
                        label.text = "—";
                    else
                        label.text = getter(data).ToString("N0");
                },
                comparison = (a, b) => getter(m_TreeView.GetItemDataForIndex<TreeRowData>(a))
                    .CompareTo(getter(m_TreeView.GetItemDataForIndex<TreeRowData>(b))),
            };
            AttachHeaderTooltip(column, tooltip);
            return column;
        }

        Column MakeOwnerColumn()
        {
            var column = new Column
            {
                name = "owner",
                title = L10n.Tr("Owner"),
                minWidth = 100,
                width = 160,
                makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    label.text = data.isBatch ? (string.IsNullOrEmpty(data.ownerName) ? "—" : data.ownerName) : "—";
                },
                comparison = (a, b) => string.CompareOrdinal(
                    m_TreeView.GetItemDataForIndex<TreeRowData>(a).ownerName ?? string.Empty,
                    m_TreeView.GetItemDataForIndex<TreeRowData>(b).ownerName ?? string.Empty),
            };
            AttachHeaderTooltip(column, L10n.Tr("IPanelComponent that owned the render chain when this batch was broken. '—' for flat panels and for batches that ran before any CutRenderChain switched the owner. The full panel-component list is shown in the right pane when a panel row is selected."));
            return column;
        }

        Column MakeBreakingReasonColumn()
        {
            var column = new Column
            {
                name = "breakingReason",
                title = L10n.Tr("Breaking Reason"),
                minWidth = 100,
                width = 160,
                makeCell = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    label.text = data.isBatch ? FormatBreakingReason(data.kickRangesReason) : "—";
                },
                comparison = (a, b) => m_TreeView.GetItemDataForIndex<TreeRowData>(a).kickRangesReason
                    .CompareTo(m_TreeView.GetItemDataForIndex<TreeRowData>(b).kickRangesReason),
            };
            AttachHeaderTooltip(column, L10n.Tr("Why the renderer broke this batch instead of continuing to grow it. Multiple flags can combine; lower counts mean better batching."));
            return column;
        }

        static string JoinOwnerNames(RawFrameDataView frameData, IReadOnlyList<EntityId> owners)
        {
            if (owners == null || owners.Count == 0)
                return string.Empty;
            if (owners.Count == 1)
                return UIToolkitProfilerToolbarHelpers.GetPanelDisplayName(frameData, owners[0]);
            var sb = new StringBuilder();
            for (var i = 0; i < owners.Count; i++)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(UIToolkitProfilerToolbarHelpers.GetPanelDisplayName(frameData, owners[i]));
            }
            return sb.ToString();
        }

        static string FormatBreakingReasonExplanation(uint flags)
        {
            if (flags == 0)
                return string.Empty;
            var sb = new StringBuilder();
            sb.Append(L10n.Tr("Batch was broken because:"));
            for (var i = 0; i < k_BreakingReasons.Length; i++)
            {
                if ((flags & (1u << i)) == 0)
                    continue;
                sb.Append('\n');
                sb.Append("• ");
                sb.Append(k_BreakingReasons[i].Name);
                sb.Append(" — ");
                sb.Append(L10n.Tr(k_BreakingReasons[i].Explanation));
            }
            // Surface unknown high bits the same way FormatBreakingReason does.
            uint known = (1u << k_BreakingReasons.Length) - 1u;
            uint extra = flags & ~known;
            if (extra != 0)
            {
                sb.Append('\n');
                sb.Append("• 0x").Append(extra.ToString("X"));
            }
            return sb.ToString();
        }

        static string FormatBreakingReason(uint flags)
        {
            if (flags == 0)
                return L10n.Tr("None");
            var sb = new StringBuilder();
            for (var i = 0; i < k_BreakingReasons.Length; i++)
            {
                if ((flags & (1u << i)) == 0)
                    continue;
                if (sb.Length > 0)
                    sb.Append(" | ");
                sb.Append(k_BreakingReasons[i].Name);
            }
            // Surface unknown high bits so a forgotten enum addition is visible rather than silently dropped.
            uint known = (1u << k_BreakingReasons.Length) - 1u;
            uint extra = flags & ~known;
            if (extra != 0)
            {
                if (sb.Length > 0)
                    sb.Append(" | ");
                sb.Append("0x").Append(extra.ToString("X"));
            }
            return sb.ToString();
        }

        void OnItemsChosen(IEnumerable<object> chosen)
        {
            foreach (var item in chosen)
            {
                if (item is TreeRowData row && !row.isBatch && row.panelEntityId != EntityId.None)
                {
                    UIToolkitProfilerToolbarHelpers.PingEntity(row.panelEntityId);
                    return;
                }
            }
        }

        void OnSelectedFrameIndexChanged(long frame) => ReloadData(frame);

        void ReloadData(long frameIndex)
        {
            // Capture before we clear — m_RootItems still holds the IDs the tree knows about.
            CaptureExpandedState();
            m_RootItems.Clear();

            if (frameIndex < 0)
            {
                m_PanelComponentsPane?.LoadFrameMetadata(null, -1);
                ApplyTreeSource();
                return;
            }

            using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
            {
                m_PanelComponentsPane?.LoadFrameMetadata(rawFrameData, frameIndex);

                if (!rawFrameData.valid)
                {
                    ApplyTreeSource();
                    return;
                }

                var guid = ProfilerUIToolkit.kProfilerMetadataGuid;

                // Build a panel-id -> batch list map first so child arrays are ready before we
                // walk the panel chunks. Each PANEL_BATCH_METRICS chunk is one panel's full
                // NativeArray<UIToolkitBatchMetricsInfo> — owner attribution lives in the
                // controller (paired PANEL_BATCH_OWNERS chunk by ordinal).
                var batchesByPanel = new Dictionary<EntityId, List<UIToolkitBatchMetricsInfo>>();
                var batchTag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelBatchMetrics;
                var batchChunkCount = rawFrameData.GetFrameMetaDataCount(guid, batchTag);
                for (var ci = 0; ci < batchChunkCount; ci++)
                {
                    using (var batches = rawFrameData.GetFrameMetaData<UIToolkitBatchMetricsInfo>(guid, batchTag, ci))
                    {
                        if (batches.Length < 1)
                            continue;
                        // All entries in a chunk share the same panelEntityId (one chunk = one
                        // panel render). Look up the destination list once.
                        var panelId = batches[0].panelEntityId;
                        if (!batchesByPanel.TryGetValue(panelId, out var list))
                        {
                            list = new List<UIToolkitBatchMetricsInfo>(batches.Length);
                            batchesByPanel[panelId] = list;
                        }
                        for (var bi = 0; bi < batches.Length; bi++)
                            list.Add(batches[bi]);
                    }
                }

                // Per-batch IPanelComponent owners come from the controller (loaded above via
                // LoadFrameMetadata). The view borrows them when stamping batch rows and joins
                // names for the Owner column.

                int nextId = 1;
                var panelTag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelMetrics;
                var panelChunkCount = rawFrameData.GetFrameMetaDataCount(guid, panelTag);
                for (var ci = 0; ci < panelChunkCount; ci++)
                {
                    using (var chunk = rawFrameData.GetFrameMetaData<UIToolkitPanelUpdateMetricsInfo>(guid, panelTag, ci))
                    {
                        if (chunk.Length < 1)
                            continue;
                        var p = chunk[0];

                        // Resolve once so batch rows can reuse it for the panel-components heading.
                        var panelName = UIToolkitProfilerToolbarHelpers.GetPanelDisplayName(rawFrameData, p.panelEntityId);

                        // Each batch record represents exactly one batch (one KickRanges call), so
                        // batch count for the panel row is just the number of children.
                        uint sumDrawCalls = 0, sumVerts = 0, sumIndices = 0, sumImmediateDraws = 0, sumDrawRanges = 0;
                        List<TreeViewItemData<TreeRowData>> children = null;
                        uint childBatchCount = 0;
                        if (batchesByPanel.TryGetValue(p.panelEntityId, out var batchList) && batchList.Count > 0)
                        {
                            children = new List<TreeViewItemData<TreeRowData>>(batchList.Count);
                            childBatchCount = (uint)batchList.Count;
                            for (var i = 0; i < batchList.Count; i++)
                            {
                                var b = batchList[i];
                                sumDrawCalls += b.drawCallCount;
                                sumVerts += b.vertexCount;
                                sumIndices += b.indexCount;
                                sumImmediateDraws += b.immediateDraws;
                                sumDrawRanges += b.drawRangeCount;

                                // Resolve owner names from the controller (single source of truth
                                // for batch owner data) and join for the Owner column.
                                var batchOwners = m_PanelComponentsPane?.GetBatchOwners(p.panelEntityId, i);
                                var ownerName = JoinOwnerNames(rawFrameData, batchOwners);

                                var batchRow = new TreeRowData
                                {
                                    isBatch = true,
                                    panelEntityId = p.panelEntityId,
                                    panelName = panelName,
                                    passIndex = i,
                                    drawCallCount = b.drawCallCount,
                                    vertexCount = b.vertexCount,
                                    indexCount = b.indexCount,
                                    immediateDraws = b.immediateDraws,
                                    drawRangeCount = b.drawRangeCount,
                                    kickRangesReason = b.kickRangesReason,
                                    isRenderingNestedTreeRT = b.isRenderingNestedTreeRT != 0,
                                    ownerName = ownerName,
                                };
                                children.Add(new TreeViewItemData<TreeRowData>(nextId++, batchRow));
                            }
                        }

                        var panelRow = new TreeRowData
                        {
                            isBatch = false,
                            panelEntityId = p.panelEntityId,
                            panelName = panelName,
                            batchCount = childBatchCount,
                            drawCallCount = sumDrawCalls,
                            vertexCount = sumVerts,
                            indexCount = sumIndices,
                            immediateDraws = sumImmediateDraws,
                            drawRangeCount = sumDrawRanges,
                            hierarchyVersionChanges = p.hierarchyVersionChanges,
                            repaintVersionChanges = p.repaintVersionChanges,
                            visualElementCount = p.visualElementCount,
                        };
                        m_RootItems.Add(new TreeViewItemData<TreeRowData>(nextId++, panelRow, children));
                    }
                }
            }

            ApplyTreeSource();
        }

        // Profiler frame changes can arrive during layout (IMGUI toolbar etc.). Defer rebuilds to
        // the next scheduler tick to avoid 'Cannot modify VisualElement hierarchy during layout'.
        void ApplyTreeSource()
        {
            if (m_TreeView == null)
                return;
            if (m_TreeView.panel == null)
            {
                m_TreeView.SetRootItems(m_RootItems);
                ApplyExpansion();
                m_TreeView.Rebuild();
                RestoreSelection();
                return;
            }
            if (m_RebuildScheduled == null)
                m_RebuildScheduled = m_TreeView.schedule.Execute(DeferredApply);
            else if (!m_RebuildScheduled.isActive)
                m_RebuildScheduled.Resume();
        }

        void DeferredApply()
        {
            if (m_TreeView == null || m_TreeView.panel == null)
                return;
            m_TreeView.SetRootItems(m_RootItems);
            // Show every batch by default (the whole point of the tree is to drill into batches),
            // but honour user-driven collapses captured before this rebuild.
            ApplyExpansion();
            m_TreeView.Rebuild();
            RestoreSelection();
        }
    }
}
