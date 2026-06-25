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
            public int eventCount;

            // Batch-only
            public uint immediateDraws;
            public uint drawRangeCount;
            public uint kickRangesReason;
            public bool isRenderingNestedTreeRT;
            public string ownerName; // resolved IPanelComponent name, or empty for batches with no owner (flat panels)
        }

        // Mirrors UIRenderDevice's KickRangesReason; entry order MUST match the enum (index N = ordinal N+1).
        static readonly (string Name, string Explanation)[] k_BreakingReasons =
        {
            ("Material Change",         "A different material was needed for the next draw."),
            ("Page Change",             "The next draw used a mesh on a different vertex/index page."),
            ("Texture Slots Exhausted", "All available texture slots were in use; no room for the next draw's texture."),
            ("Stencil Ref Change",      "The stencil reference value changed (typically a clipping mask boundary)."),
            ("Break Batches (debug)",   "Break Batches debug toggle is on — every command forces a break."),
            ("Ranges Buffer Full",      "The internal draw-range buffer ran out of room mid-pass and had to flush."),
            ("Immediate Command",       "An Immediate (or ImmediateCull) command interrupted the chain."),
            ("Render Chain Cut",        "A CutRenderChain command (new render chain owner / world-space root)."),
            ("Default Material Change", "A Push/PopDefaultMaterial command interrupted the chain."),
            ("Scissor Change",          "A Push/PopScissor command changed the clip rectangle."),
            ("View Change",             "A Push/PopView command changed the view matrix."),
        };

        readonly List<TreeViewItemData<TreeRowData>> m_RootItems = new();
        // Reused across frames in BuildRoots to avoid a per-frame Dictionary allocation when the
        // user scrubs the profiler timeline. The value lists are pooled in m_BatchListPool (see
        // RentBatchList / ReleaseBatchLists) so scrubbing doesn't churn one List per panel either.
        readonly Dictionary<EntityId, List<UIToolkitBatchMetricsInfo>> m_BatchesByPanel = new();
        readonly Stack<List<UIToolkitBatchMetricsInfo>> m_BatchListPool = new();
        // Reused across frames in BuildRoots (CollectEventCountsByPanel clears it first) so scrubbing
        // the timeline doesn't allocate a fresh Dictionary per refresh — mirrors the sibling view's field.
        readonly Dictionary<EntityId, int> m_PanelEventCounts = new();
        MultiColumnTreeViewWithTotal m_TreeView;
        Label m_EmptyOverlay;
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
            columns.Add(MakeIntColumn("events", L10n.Tr("Events"), r => r.eventCount, panelOnly: true,
                tooltip: L10n.Tr("Number of events dispatched on this panel during the frame (pointer, keyboard, navigation, and others). Click the row to see the per-event list in the right pane.")));

            m_TreeView = new MultiColumnTreeViewWithTotal(columns)
            {
                fixedItemHeight = 18,
                sortingMode = ColumnSortingMode.Default,
                viewDataKey = "uitoolkit-details-module-treeview",
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                horizontalScrollingEnabled = true,
            };
            m_TreeView.itemsChosen += OnItemsChosen;
            m_TreeView.selectedIndicesChanged += OnSelectionChanged;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarSpacer { flex = true });
            UIToolkitProfilerToolbarHelpers.AddCommonButtons(toolbar);

            m_PanelComponentsPane = new PanelComponentsPaneController(k_ViewDataKeyPrefix);

            var treeStack = UIToolkitProfilerToolbarHelpers.WrapWithEmptyOverlay(
                m_TreeView,
                "uitoolkit-details-module-tree-stack",
                L10n.Tr("No data to show. Start profiling UI Toolkit content to see details."),
                out m_EmptyOverlay);

            var splitView = m_PanelComponentsPane.WireUp(treeStack, toolbar);

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
            // panel rows show the full panel component list plus a roll-up of per-flag counts.
            foreach (var index in selectedIndices)
            {
                var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                m_SelectedPanelEntityId = data.panelEntityId;
                m_SelectedPanelName = data.panelName;
                m_SelectedIsBatch = data.isBatch;
                m_SelectedPassIndex = data.passIndex;
                ForwardSelectionToPane(data.panelEntityId, data.panelName, data.isBatch, data.passIndex, data.kickRangesReason);
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
                        ForwardSelectionToPane(m_SelectedPanelEntityId, m_SelectedPanelName, isBatch: true, m_SelectedPassIndex, child.data.kickRangesReason);
                        return;
                    }
                    // Batch index didn't survive this frame — fall through to selecting the panel row.
                }

                m_TreeView.SetSelectionByIdWithoutNotify(new[] { m_RootItems[i].id });
                ForwardSelectionToPane(m_SelectedPanelEntityId, m_SelectedPanelName, isBatch: false, 0, kickRangesReason: 0);
                return;
            }

            // Selection didn't survive this frame's data — show a panel-row selection without
            // any summary (the summary would be against last frame's children, which are gone).
            m_PanelComponentsPane?.SetSelectedPanel(m_SelectedPanelEntityId, m_SelectedPanelName);
        }

        // Single funnel for handing off selection state to the right pane so the
        // batch-vs-panel routing + summary-string computation only lives in one place.
        void ForwardSelectionToPane(EntityId panelEntityId, string panelName, bool isBatch, int passIndex, uint kickRangesReason)
        {
            if (m_PanelComponentsPane == null)
                return;
            if (isBatch)
            {
                var (heading, details) = FormatBatchReasonForHelpBox(kickRangesReason);
                m_PanelComponentsPane.SetSelectedBatch(panelEntityId, panelName, passIndex, heading, details);
            }
            else
            {
                var (heading, details, hint) = FormatPanelReasonForHelpBox(panelEntityId);
                m_PanelComponentsPane.SetSelectedPanel(panelEntityId, panelName, heading, details, hint);
            }
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
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    if (!alwaysShown && ((panelOnly && data.isBatch) || (batchOnly && !data.isBatch)))
                        label.text = UIToolkitProfilerToolbarHelpers.NoDataCell;
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
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    if (panelOnly && data.isBatch)
                        label.text = UIToolkitProfilerToolbarHelpers.NoDataCell;
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
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    label.text = data.isBatch
                        ? (string.IsNullOrEmpty(data.ownerName) ? UIToolkitProfilerToolbarHelpers.NoDataCell : data.ownerName)
                        : UIToolkitProfilerToolbarHelpers.NoDataCell;
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
                bindCell = (element, index) =>
                {
                    var data = m_TreeView.GetItemDataForIndex<TreeRowData>(index);
                    var label = (Label)element;
                    label.text = data.isBatch ? FormatBreakingReason(data.kickRangesReason) : UIToolkitProfilerToolbarHelpers.NoDataCell;
                },
                comparison = (a, b) => m_TreeView.GetItemDataForIndex<TreeRowData>(a).kickRangesReason
                    .CompareTo(m_TreeView.GetItemDataForIndex<TreeRowData>(b).kickRangesReason),
            };
            AttachHeaderTooltip(column, L10n.Tr("Why the renderer broke this batch instead of continuing to grow it. Lower counts mean better batching."));
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

        // Builds the heading the right pane shows above its HelpBox and the body the HelpBox
        // itself displays for a single batch. kickReason is a single KickRangesReason ordinal
        // (1-based), not a bitmask — see FormatBreakingReason in the Breaking Reason cell.
        // Returning a null body collapses the HelpBox (used for the "batch didn't break" success
        // state) so the pane reads as a one-line confirmation instead of an empty alert box.
        static (string Heading, string Details) FormatBatchReasonForHelpBox(uint kickReason)
        {
            if (kickReason == 0)
                return (L10n.Tr("Batch rendered without breaks."), null);

            // Format: "<Name>: <Explanation>". The colon (rather than an em-dash) reads more
            // naturally as a name → description connector. Bullet glyph is retained here because
            // a separate in-flight PR is removing bullets from this section across the pane —
            // keep them in this change to avoid stomping that work; the end-state after both PRs
            // land is bullet-less + colon.
            int index = (int)kickReason - 1;
            string body = index < 0 || index >= k_BreakingReasons.Length
                // Out-of-range — surface it instead of silently dropping (likely a forgotten enum addition).
                ? $"0x{kickReason:X}"
                : k_BreakingReasons[index].Name + ": " + L10n.Tr(k_BreakingReasons[index].Explanation);
            return (L10n.Tr("Batch breaking reason:"), body);
        }

        // Roll-up for the panel-row selection: counts broken batches and per-reason occurrences
        // across all of the panel's children, then formats them for the right pane's heading +
        // HelpBox + CTA hint. kickRangesReason is a single KickRangesReason ordinal per batch,
        // not a bitmask, so each broken batch contributes to exactly one reason tally.
        //   - All batches OK:  one-line "All N batches rendered without breaks." (no HelpBox).
        //   - Some broke:      heading + comma-separated tally in the HelpBox + "Select a batch
        //                      row to see details." CTA hint.
        //   - No children:     all three null so the reason section collapses for panels that
        //                      didn't render anything this frame.
        (string Heading, string Details, string Hint) FormatPanelReasonForHelpBox(EntityId panelEntityId)
        {
            // Find the matching panel root and pull its children (= batch rows).
            int totalBatches = 0;
            int brokenBatches = 0;
            int unknownReasonCount = 0;
            Span<int> perReasonCounts = stackalloc int[k_BreakingReasons.Length];

            for (var i = 0; i < m_RootItems.Count; i++)
            {
                if (m_RootItems[i].data.panelEntityId != panelEntityId)
                    continue;
                totalBatches = (int)m_RootItems[i].data.batchCount;
                if (m_RootItems[i].children != null)
                {
                    foreach (var child in m_RootItems[i].children)
                    {
                        var kickReason = child.data.kickRangesReason;
                        if (kickReason == 0)
                            continue;
                        brokenBatches++;
                        int index = (int)kickReason - 1;
                        if (index < 0 || index >= k_BreakingReasons.Length)
                            unknownReasonCount++;
                        else
                            perReasonCounts[index]++;
                    }
                }
                break;
            }

            if (totalBatches == 0)
                return (null, null, null);

            if (brokenBatches == 0)
            {
                var clean = totalBatches == 1
                    ? L10n.Tr("This batch rendered without breaks.")
                    : string.Format(L10n.Tr("All {0} batches rendered without breaks."), totalBatches);
                return (clean, null, null);
            }

            var heading = string.Format(L10n.Tr("{0} of {1} batches broke this frame:"), brokenBatches, totalBatches);

            // Per-reason tally, only listing reasons that actually fired this frame so the HelpBox
            // body stays short and scannable. Formatting "<reason> × <count>" (× = multiplication
            // sign) matches the designer's mock and reads more naturally than parenthesized counts
            // because the user is scanning "what broke and how often" — × N is parsed as "times
            // N" at a glance, whereas (N) tends to read like an annotation.
            var details = new StringBuilder();
            for (var i = 0; i < k_BreakingReasons.Length; i++)
            {
                if (perReasonCounts[i] == 0)
                    continue;
                if (details.Length > 0)
                    details.Append(", ");
                details.Append(k_BreakingReasons[i].Name);
                details.Append(" × ");
                details.Append(perReasonCounts[i]);
            }
            if (unknownReasonCount > 0)
            {
                if (details.Length > 0)
                    details.Append(", ");
                details.Append(L10n.Tr("Unknown"));
                details.Append(" × ");
                details.Append(unknownReasonCount);
            }

            return (heading, details.ToString(), L10n.Tr("Select a batch row to see details."));
        }

        static string FormatBreakingReason(uint kickReason)
        {
            if (kickReason == 0)
                return L10n.Tr("None");
            int index = (int)kickReason - 1;
            if (index < 0 || index >= k_BreakingReasons.Length)
                // Out-of-range — surface it instead of silently dropping (likely a forgotten enum addition).
                return $"0x{kickReason:X}";
            return k_BreakingReasons[index].Name;
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
            }
            else
            {
                using (var rawFrameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
                {
                    m_PanelComponentsPane?.LoadFrameMetadata(rawFrameData, frameIndex);

                    if (rawFrameData.valid)
                        BuildRoots(rawFrameData);
                }
            }

            // Always refresh — for no-frame / invalid-frame paths m_RootItems is empty, so totals
            // fall back to "0" / "—" naturally instead of keeping stale values from the previous
            // frame.
            RefreshTotalsHeader();
            UpdateEmptyOverlay();
            ApplyTreeSource();
        }

        void UpdateEmptyOverlay()
        {
            if (m_EmptyOverlay != null)
                m_EmptyOverlay.style.display = m_RootItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void BuildRoots(RawFrameDataView rawFrameData)
        {
            var guid = ProfilerUIToolkit.kProfilerMetadataGuid;

            // Build a panel-id -> batch list map first so child arrays are ready before we
            // walk the panel chunks. Each PANEL_BATCH_METRICS chunk is one panel's full
            // NativeArray<UIToolkitBatchMetricsInfo> — owner attribution lives in the
            // controller (paired PANEL_BATCH_OWNERS chunk by ordinal). Both the outer dict and
            // its per-panel List values are reused across frames (lists returned to m_BatchListPool).
            ReleaseBatchLists();
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
                    if (!m_BatchesByPanel.TryGetValue(panelId, out var list))
                    {
                        list = RentBatchList();
                        m_BatchesByPanel[panelId] = list;
                    }
                    for (var bi = 0; bi < batches.Length; bi++)
                        list.Add(batches[bi]);
                }
            }

            // Per-batch IPanelComponent owners come from the controller (loaded above via
            // LoadFrameMetadata). The view borrows them when stamping batch rows and joins
            // names for the Owner column.

            // Tally PANEL_EVENTS by panel (shared with the other details view) so each panel row gets
            // its dispatched-event count.
            UIToolkitProfilerToolbarHelpers.CollectEventCountsByPanel(rawFrameData, m_PanelEventCounts);

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
                    if (m_BatchesByPanel.TryGetValue(p.panelEntityId, out var batchList) && batchList.Count > 0)
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

                    m_PanelEventCounts.TryGetValue(p.panelEntityId, out var eventCount);
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
                        eventCount = eventCount,
                    };
                    m_RootItems.Add(new TreeViewItemData<TreeRowData>(nextId++, panelRow, children));
                }
            }
        }

        // Return the per-panel batch lists to the pool before clearing the map so timeline
        // scrubbing reuses them instead of allocating one List per panel every frame.
        void ReleaseBatchLists()
        {
            foreach (var list in m_BatchesByPanel.Values)
            {
                list.Clear();
                m_BatchListPool.Push(list);
            }
            m_BatchesByPanel.Clear();
        }

        List<UIToolkitBatchMetricsInfo> RentBatchList()
            => m_BatchListPool.Count > 0 ? m_BatchListPool.Pop() : new List<UIToolkitBatchMetricsInfo>();

        void RefreshTotalsHeader()
        {
            if (m_TreeView == null)
                return;

            ulong batches = 0, drawCalls = 0, vertices = 0, indices = 0, immediateDraws = 0, drawRanges = 0;
            ulong hierarchyChanges = 0, repaintChanges = 0;
            long veCount = 0, eventCount = 0;
            for (var i = 0; i < m_RootItems.Count; i++)
            {
                var p = m_RootItems[i].data;
                batches += p.batchCount;
                drawCalls += p.drawCallCount;
                vertices += p.vertexCount;
                indices += p.indexCount;
                immediateDraws += p.immediateDraws;
                drawRanges += p.drawRangeCount;
                hierarchyChanges += p.hierarchyVersionChanges;
                repaintChanges += p.repaintVersionChanges;
                veCount += p.visualElementCount;
                eventCount += p.eventCount;
            }

            m_TreeView.SetTotalCell("name", L10n.Tr("Total"));
            m_TreeView.SetTotalCell("batches", batches.ToString("N0"));
            m_TreeView.SetTotalCell("drawCalls", drawCalls.ToString("N0"));
            m_TreeView.SetTotalCell("vertices", vertices.ToString("N0"));
            m_TreeView.SetTotalCell("indices", indices.ToString("N0"));
            m_TreeView.SetTotalCell("immediateDraws", immediateDraws.ToString("N0"));
            m_TreeView.SetTotalCell("drawRanges", drawRanges.ToString("N0"));
            // Sums don't make sense for breaking-reason flags or per-batch owner names.
            m_TreeView.SetTotalCell("breakingReason", UIToolkitProfilerToolbarHelpers.NoDataCell);
            m_TreeView.SetTotalCell("owner", UIToolkitProfilerToolbarHelpers.NoDataCell);
            m_TreeView.SetTotalCell("hierarchyChanges", hierarchyChanges.ToString("N0"));
            m_TreeView.SetTotalCell("repaintChanges", repaintChanges.ToString("N0"));
            m_TreeView.SetTotalCell("veCount", veCount.ToString("N0"));
            m_TreeView.SetTotalCell("events", eventCount.ToString("N0"));
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
