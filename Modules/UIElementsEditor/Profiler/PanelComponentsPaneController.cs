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
        const string k_PanelComponentRowIconName = "uitoolkit-profiler-panel-component-icon";
        const string k_PanelComponentRowNameLabelName = "uitoolkit-profiler-panel-component-name";
        const string k_PanelComponentRowPingButtonName = "uitoolkit-profiler-panel-component-ping";

        // External style sheet authored alongside this controller. All colors and spacing values
        // resolve against Editor theme variables (var(--unity-colors-...)) so the pane re-themes
        // for free in light/dark skin without duplicate hex values in C#.
        const string k_StyleSheetPath = "Settings/UIToolkitProfiler.uss";
        const string k_UssPane = "uitoolkit-profiler__pane";
        const string k_UssHeader = "uitoolkit-profiler__pane-header";
        const string k_UssHeaderLabel = "uitoolkit-profiler__pane-header-label";
        const string k_UssHeaderBatch = "uitoolkit-profiler__pane-header-batch";
        const string k_UssReason = "uitoolkit-profiler__pane-reason";
        const string k_UssReasonHeadingRow = "uitoolkit-profiler__pane-reason-heading-row";
        const string k_UssReasonHeadingIcon = "uitoolkit-profiler__pane-reason-heading-icon";
        const string k_UssReasonHeading = "uitoolkit-profiler__pane-reason-heading";
        const string k_UssReasonHelpBox = "uitoolkit-profiler__pane-reason-helpbox";
        const string k_UssReasonHint = "uitoolkit-profiler__pane-reason-hint";
        const string k_UssScroll = "uitoolkit-profiler__pane-scroll";
        const string k_UssFoldout = "uitoolkit-profiler__pane-foldout";
        const string k_UssList = "uitoolkit-profiler__pane-list";
        const string k_UssEventsPlaceholder = "uitoolkit-profiler__pane-events-placeholder";
        const string k_UssEmpty = "uitoolkit-profiler__pane-empty";

        // Height of the ListView when it has no items — gives the "none element" room to render
        // its message without the user needing to expand or scroll to see it.
        const int k_EmptyListHeightPx = 72;
        const int k_ListItemHeightPx = 18;
        const int k_ListVerticalPaddingPx = 2;

        static class Strings
        {
            public static readonly string DetailViewModeTooltip = L10n.Tr("Show or hide UI Toolkit related data next to the panel timing list.");
            public static readonly string ComponentsFoldoutTitle = L10n.Tr("Panel components");
            public static readonly string ComponentsFoldoutTitleCounted = L10n.Tr("Panel components ({0})");
            public static readonly string ComponentsFoldoutTooltip = L10n.Tr("IPanelComponent objects from UI Toolkit frame metadata for the selected panel or batch.");
            public static readonly string EventsFoldoutTitle = L10n.Tr("Events");
            public static readonly string EventsFoldoutTitleCounted = L10n.Tr("Events ({0})");
            public static readonly string EventsPlaceholder = L10n.Tr("Event capture is not wired up yet. This list is a placeholder for the upcoming events overlay.");
            // Plain "Batch #N". The CSS adds a 6px left margin between the panel name and this
            // label for visual separation — no bullet/glyph in the string itself so the heading
            // reads cleanly when long localized panel names cause the strip to wrap or ellipsize.
            public static readonly string HeaderBatchSuffix = L10n.Tr("Batch #{0}");
            public static readonly string EmptyOverviewMessage = L10n.Tr("Select a panel row for an overview of its batches, components, and events. Select a batch row to see why it broke.");
            public static readonly string StatusNoMetadataEntry = L10n.Tr("No UI Toolkit panel metadata for this panel in the selected frame.");
            public static readonly string StatusNoPanelComponents = L10n.Tr("No panel components in metadata for this panel.");
            public static readonly string StatusNoBatchComponents = L10n.Tr("No panel components contributed to this batch.");
            public static readonly string StatusListEmptyCouldNotLoadDetails = L10n.Tr("Panel components are present in frame metadata but details could not be loaded for this frame.");

            // Replacement tooltips shown over the disabled ping button so the user understands
            // why ping is unavailable instead of seeing the affordance silently disappear.
            //   - NoEntity:     metadata didn't include an object reference for this component
            //                   (e.g. emitter cleared the EntityId before flush).
            //   - CrossSession: the frame came from a saved profile or remote player and its
            //                   EntityIds don't resolve against this editor's object table.
            public static readonly string PingUnavailableNoEntityTooltip = L10n.Tr("Ping is unavailable: no object reference recorded for this component in frame metadata.");
            public static readonly string PingUnavailableCrossSessionTooltip = L10n.Tr("Ping is unavailable for frames captured outside this editor session, such as saved profiles or remote players.");
        }

        enum DetailsSplitMode { NoDetails = 0, RelatedData = 1 }

        // Shared across all UI Toolkit profiler details views so the show/hide state of the related-data
        // pane stays in sync between the UI Toolkit module and the UI Toolkit Details module (and across
        // multiple ProfilerWindow instances). EditorPrefs alone only syncs across re-creations; the static
        // event keeps already-open views in lockstep.
        internal const string SharedSplitModeEditorPrefKey = "UIToolkitProfiler.RelatedDataPaneVisible";
        // Shared between modules so the splitter position the user picks in one module is the same
        // when they switch to the other (matches the shared show/hide state of the right pane).
        const string k_SharedSplitViewViewDataKey = "uitoolkit-profiler-main-related-split";
        // Long enough to outlast attach + first layout + view-data-restored selection event firing
        // on the host's tree view. Short enough that the user doesn't perceive lag on the first
        // appearance of the pane.
        const long k_InitialRefreshSettleDelayMs = 50;
        static event Action<DetailsSplitMode> s_SharedSplitModeChanged;

        sealed class PanelComponentListEntry
        {
            public EntityId entityId;
            public string displayName;
            // Managed type used to resolve the row icon. Set from the live object's type for current-
            // session frames; null for captured / cross-session frames (which don't record a usable
            // type), where bind falls back to the PanelRenderer icon.
            public Type iconType;
        }

        readonly string m_ViewDataKeyPrefix;

        TwoPaneSplitView m_SplitView;
        VisualElement m_RelatedDataColumn;
        ToolbarToggle m_ToolbarToggle;
        ListView m_PanelComponentsListView;

        // Sticky header strip (panel name + optional batch indicator). Lives outside the scroll
        // region so the user never loses the "what am I looking at" context when scrolling
        // through the components or events lists below.
        VisualElement m_HeaderStrip;
        Label m_HeaderPanelLabel;
        Label m_HeaderBatchLabel;

        // Sticky breaking-reason block (heading + HelpBox + optional CTA hint). The HelpBox is
        // the primary content the user is looking for when debugging "why did my batch break";
        // pinning it above the foldouts keeps that answer at a fixed location regardless of
        // how many panel components or events the panel has.
        VisualElement m_ReasonSection;
        // Heading row wraps the optional success icon + heading label in a flex row so the icon
        // aligns next to the first line of the heading (and stays put when the heading wraps).
        VisualElement m_ReasonHeadingRow;
        Image m_ReasonHeadingIcon;
        Label m_ReasonHeading;
        HelpBox m_ReasonHelpBox;
        Label m_ReasonHint;

        // Foldouts area (Panel components, Events). Wrapped in a ScrollView so the lower part
        // of the pane scrolls independently of the sticky header / reason block above.
        ScrollView m_ContentScrollView;
        Foldout m_ComponentsFoldout;
        Foldout m_EventsFoldout;

        // Empty-state overlay shown when there is no selection (covers the whole pane, drawn on
        // top via Position.Absolute so it doesn't shift the layout when toggled).
        Label m_NoSelectionOverlay;

        Label m_PanelComponentsListEmptyLabel;
        // ListView creates the none-element lazily during layout, so the first
        // SyncPanelComponentsListEmptyMessage call lands before the label exists. Cache the message
        // and apply it from CreatePanelComponentsListViewEmptyElement when the label is finally built.
        string m_PendingEmptyMessage;
        DetailsSplitMode m_DetailsSplitMode = DetailsSplitMode.RelatedData;
        IVisualElementScheduledItem m_RefreshScheduleItem;
        bool m_HasRunInitialRefresh;

        readonly Dictionary<EntityId, List<EntityId>> m_PanelToPanelComponentEntityIds = new();
        // Per-panel ordered owner per batch. Index N is the owners of the Nth PANEL_BATCH_METRICS
        // chunk for that panel (= passIndex on the batch row). Empty inner list when no
        // IPanelComponent contributed to the batch.
        readonly Dictionary<EntityId, List<List<EntityId>>> m_PanelToBatchOwners = new();
        readonly Stack<List<EntityId>> m_PanelComponentIdListPool = new();
        readonly List<PanelComponentListEntry> m_PanelComponentListEntries = new();
        readonly Stack<PanelComponentListEntry> m_PanelComponentListEntryPool = new();
        long m_LastFrameIndex = -1;

        EntityId m_SelectedPanelEntityId = EntityId.None;
        string m_SelectedPanelName;
        // -1 = panel-level selection; >= 0 = batch row selected (passIndex within the panel).
        int m_SelectedBatchIndex = -1;
        bool m_HasSelection;

        // Pre-formatted breaking-reason copy supplied by the host view. The view knows the kick
        // reason flag names (and, for panel rows, how to count per-flag occurrences across all
        // children) so it produces the final strings; this controller only renders them.
        //   - Heading: single-line summary (e.g. "Batch breaking reason:" or "3 of 5 batches
        //     broke this frame:").
        //   - Details: multi-line HelpBox body. Selectable / copyable so users can paste it into
        //     bug reports without retyping. Null/empty hides the HelpBox entirely.
        //   - Hint:    smaller follow-up line (e.g. "Select a batch row to see details").
        // All three are independently optional. If all three are null the reason section is
        // hidden so panels with no breaking-reason data don't show a blank strip.
        string m_PendingReasonHeading;
        string m_PendingReasonDetails;
        string m_PendingReasonHint;

        public PanelComponentsPaneController(string viewDataKeyPrefix)
        {
            m_ViewDataKeyPrefix = viewDataKeyPrefix;
        }

        /// <summary>
        /// Build the right pane and split view, appending the visibility toggle to <paramref name="toolbar"/>.
        /// Returns the <see cref="TwoPaneSplitView"/>; caller adds it to its root layout.
        /// </summary>
        /// <remarks>
        /// Pane layout (top → bottom):
        ///   1. Header strip       — sticky; panel name + optional "Batch #N" suffix.
        ///   2. Reason section     — sticky; heading + HelpBox + optional CTA hint.
        ///   3. Foldouts area      — scrollable; "Panel components (N)" + "Events" (placeholder).
        ///   4. No-selection overlay — absolutely positioned label; covers (1)–(3) when nothing
        ///      is selected so the user sees a single onboarding message instead of empty regions.
        /// All visual tokens (colors, spacing, font sizes) come from the external stylesheet
        /// <see cref="k_StyleSheetPath"/> which references Editor theme variables.
        /// </remarks>
        public TwoPaneSplitView WireUp(VisualElement leftContent, Toolbar toolbar)
        {
            m_PanelComponentsListView = new ListView
            {
                fixedItemHeight = k_ListItemHeightPx,
                makeItem = CreatePanelComponentListRow,
                bindItem = BindPanelComponentListRow,
                makeNoneElement = CreatePanelComponentsListViewEmptyElement,
                selectionType = SelectionType.Single,
                // Zebra striping matches the left tree view's row rhythm. Use ContentOnly (not
                // All) so empty space below the last item is never striped — striped empty rows
                // look like real interactive rows the user can't click, which is the bug the
                // designer flagged for single-item lists.
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                // Intentionally no viewDataKey: selection in this list is transient (a click pings
                // the entity and exists for the duration of one panel/batch selection). Persisting
                // selectedIndex via view-data caused the first row to show the "selected, not
                // focused" highlight every time the list rebuilt for a new panel — designer
                // flagged this as confusing because it implied something was pre-selected when it
                // wasn't.
            };
            m_PanelComponentsListView.AddToClassList(k_UssList);
            m_PanelComponentsListView.itemsChosen += OnPanelComponentsItemsChosen;

            var panelComponentsPane = new VisualElement { name = m_ViewDataKeyPrefix + "-panel-components-pane" };
            panelComponentsPane.AddToClassList(k_UssPane);

            // (1) Sticky header strip.
            m_HeaderStrip = new VisualElement { name = m_ViewDataKeyPrefix + "-pane-header" };
            m_HeaderStrip.AddToClassList(k_UssHeader);
            m_HeaderPanelLabel = new Label { focusable = false };
            m_HeaderPanelLabel.AddToClassList(k_UssHeaderLabel);
            m_HeaderBatchLabel = new Label { focusable = false, style = { display = DisplayStyle.None } };
            m_HeaderBatchLabel.AddToClassList(k_UssHeaderBatch);
            m_HeaderStrip.Add(m_HeaderPanelLabel);
            m_HeaderStrip.Add(m_HeaderBatchLabel);
            panelComponentsPane.Add(m_HeaderStrip);

            // (2) Sticky breaking-reason section.
            m_ReasonSection = new VisualElement { name = m_ViewDataKeyPrefix + "-pane-reason" };
            m_ReasonSection.AddToClassList(k_UssReason);

            // Heading row: optional success icon + heading label, side by side. Wrapping them in
            // a flex row keeps the icon aligned to the first line of the heading when the
            // localized text wraps to multiple lines.
            m_ReasonHeadingRow = new VisualElement { focusable = false, style = { display = DisplayStyle.None } };
            m_ReasonHeadingRow.AddToClassList(k_UssReasonHeadingRow);
            m_ReasonHeadingIcon = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                style = { display = DisplayStyle.None },
            };
            m_ReasonHeadingIcon.AddToClassList(k_UssReasonHeadingIcon);
            m_ReasonHeading = new Label { focusable = false };
            m_ReasonHeading.AddToClassList(k_UssReasonHeading);
            m_ReasonHeadingRow.Add(m_ReasonHeadingIcon);
            m_ReasonHeadingRow.Add(m_ReasonHeading);

            // Info, not Warning. The user is already inside the profiler explicitly inspecting a
            // batch's breaking reason — they navigated here, so the yellow-triangle "alert" weight
            // is redundant with the surface they're on. Info reads as "here's what's happening",
            // which matches the descriptive nature of the per-flag list. Reserve Warning for
            // cases where the formatter actually wants to grab attention (e.g. an anti-pattern
            // detector with a prescribed fix).
            m_ReasonHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Info)
            {
                style = { display = DisplayStyle.None },
            };
            m_ReasonHelpBox.AddToClassList(k_UssReasonHelpBox);
            m_ReasonHint = new Label { focusable = false, style = { display = DisplayStyle.None } };
            m_ReasonHint.AddToClassList(k_UssReasonHint);
            m_ReasonSection.Add(m_ReasonHeadingRow);
            m_ReasonSection.Add(m_ReasonHelpBox);
            m_ReasonSection.Add(m_ReasonHint);
            panelComponentsPane.Add(m_ReasonSection);

            // (3) Scrollable foldouts area. ScrollView absorbs the remaining vertical space and
            // lets the user scroll through long component lists without losing (1) and (2).
            m_ContentScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = m_ViewDataKeyPrefix + "-pane-scroll",
                viewDataKey = m_ViewDataKeyPrefix + "-pane-scroll",
            };
            m_ContentScrollView.AddToClassList(k_UssScroll);

            m_ComponentsFoldout = new Foldout
            {
                text = Strings.ComponentsFoldoutTitle,
                value = true,
                viewDataKey = m_ViewDataKeyPrefix + "-components-foldout",
            };
            m_ComponentsFoldout.AddToClassList(k_UssFoldout);
            // Tooltip lives on the toggle (header) only — putting it on the Foldout root makes
            // UI Toolkit's TooltipEvent walk find it from any descendant, so the same tooltip
            // would re-appear over every list item. Scoping to the toggle keeps the hover hint
            // limited to the section header where it's actually informative.
            m_ComponentsFoldout.toggle.tooltip = Strings.ComponentsFoldoutTooltip;
            m_ComponentsFoldout.contentContainer.Add(m_PanelComponentsListView);

            m_EventsFoldout = new Foldout
            {
                // Until event capture is wired up the count is always 0 — surface it explicitly
                // so the user reads "this panel has no events this frame" instead of wondering
                // whether the section is loading. When event capture lands, update this title
                // per selection with the real count.
                text = string.Format(Strings.EventsFoldoutTitleCounted, 0),
                value = false,
                viewDataKey = m_ViewDataKeyPrefix + "-events-foldout",
            };
            m_EventsFoldout.AddToClassList(k_UssFoldout);
            // Placeholder until events frame metadata is wired up — keeps the foldout shape in
            // place so the layout doesn't shift when real data lands later.
            var eventsPlaceholder = new Label(Strings.EventsPlaceholder) { focusable = false };
            eventsPlaceholder.AddToClassList(k_UssEventsPlaceholder);
            m_EventsFoldout.contentContainer.Add(eventsPlaceholder);

            m_ContentScrollView.Add(m_ComponentsFoldout);
            m_ContentScrollView.Add(m_EventsFoldout);
            panelComponentsPane.Add(m_ContentScrollView);

            // (4) No-selection overlay. Absolutely positioned so toggling it doesn't reflow the
            // sticky regions above.
            m_NoSelectionOverlay = new Label(Strings.EmptyOverviewMessage) { focusable = false };
            m_NoSelectionOverlay.AddToClassList(k_UssEmpty);
            m_NoSelectionOverlay.style.position = Position.Absolute;
            m_NoSelectionOverlay.style.left = 0;
            m_NoSelectionOverlay.style.right = 0;
            m_NoSelectionOverlay.style.top = 0;
            m_NoSelectionOverlay.style.bottom = 0;
            panelComponentsPane.Add(m_NoSelectionOverlay);

            // Initial visibility — no selection yet, so the overlay covers the (still empty)
            // regions below.
            ApplyPaneVisibilityForSelection();

            // No viewDataKey: the EditorPref (and the static change event) is the single source of
            // truth for the toggle's value, so a per-module persisted view-data value would override
            // the EditorPref-driven init when the toggle is attached and re-introduce the desync the
            // shared key was meant to fix.
            m_ToolbarToggle = new ToolbarToggle
            {
                text = string.Empty,
                tooltip = Strings.DetailViewModeTooltip,
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
            // 250px lower bound for the right pane: anything narrower starts truncating panel
            // names and per-flag tallies in the breaking-reason HelpBox, which defeats the point
            // of having a dedicated debugging surface. The splitter still lets the user expand
            // beyond this; they just can't shrink it small enough to make the content unreadable.
            m_RelatedDataColumn.style.minWidth = 250;
            m_RelatedDataColumn.Add(panelComponentsPane);

            var leftColumn = new VisualElement { name = m_ViewDataKeyPrefix + "-main-column" };
            leftColumn.style.flexDirection = FlexDirection.Column;
            leftColumn.style.flexGrow = 1;
            leftColumn.style.minWidth = 0;
            leftColumn.Add(leftContent);

            m_SplitView = new TwoPaneSplitView(1, 280, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = m_ViewDataKeyPrefix + "-main-related-split",
                viewDataKey = k_SharedSplitViewViewDataKey,
            };
            m_SplitView.Add(leftColumn);
            m_SplitView.Add(m_RelatedDataColumn);

            // Applied at the split-view scope so the rules reach BOTH children: the right components
            // pane and the left tree view (whose per-column totals cells use .uitoolkit-profiler__totals-cell).
            // Every rule is a class selector, so it resolves against descendants of either pane.
            // Missing-file load is non-fatal: controls fall back to their built-in defaults.
            var styleSheet = EditorGUIUtility.Load(k_StyleSheetPath) as StyleSheet;
            if (styleSheet != null)
                m_SplitView.styleSheets.Add(styleSheet);

            var savedMode = (DetailsSplitMode)EditorPrefs.GetInt(SharedSplitModeEditorPrefKey, (int)DetailsSplitMode.RelatedData);
            if (savedMode != DetailsSplitMode.NoDetails && savedMode != DetailsSplitMode.RelatedData)
                savedMode = DetailsSplitMode.RelatedData;
            SetDetailsSplitMode(savedMode, persist: false);

            s_SharedSplitModeChanged += OnSharedSplitModeChanged;

            // Defer the initial collapse: TwoPaneSplitView.CollapseChild requires m_LeftPane to be
            // assigned (PostDisplaySetup must have run). When the split view has a viewDataKey,
            // OnViewDataReady -> PostDisplaySetup runs without honoring m_PendingCollapseToExecute,
            // so the eager CollapseChild call inside ApplyDetailsSplitModeLocally can be silently
            // dropped. Wait for the first GeometryChangedEvent on the split view itself — by then
            // layout is complete and CollapseChild will actually take effect.
            void OnSplitViewFirstGeometry(GeometryChangedEvent _)
            {
                m_SplitView.UnregisterCallback<GeometryChangedEvent>(OnSplitViewFirstGeometry);
                if (m_DetailsSplitMode == DetailsSplitMode.NoDetails && m_SplitView != null)
                    m_SplitView.CollapseChild(1);
            }
            m_SplitView.RegisterCallback<GeometryChangedEvent>(OnSplitViewFirstGeometry);

            return m_SplitView;
        }

        /// <summary>Called from the host's ReloadData to load PANEL_ENTRIES + PANEL_BATCH_METRICS owner tails for the frame.</summary>
        public void LoadFrameMetadata(RawFrameDataView rawFrameData, long frameIndex)
        {
            ReleaseAllPanelComponentEntityIdLists();
            ReleaseAllBatchOwnerLists();
            m_LastFrameIndex = frameIndex;
            // Centralized, shared with the other UI Toolkit profiler views and the name/icon resolver.
            // Handles null/invalid frames as "not current session". Frames loaded from a saved profile
            // or streamed from a remote player resolve EntityIds against a different session, so live-
            // object lookups (icon, ping/double-click) must be gated on this.
            UIToolkitProfilerToolbarHelpers.UpdateCurrentEditorSession(rawFrameData);

            if (rawFrameData == null || !rawFrameData.valid)
            {
                ScheduleRefreshPanelComponentsPane();
                return;
            }

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

        /// <summary>
        /// Select a panel row. The breaking-reason summary args are optional — callers that
        /// don't have batch-level data (e.g. the legacy UI Toolkit module which is panel-only)
        /// can omit them and the reason section stays hidden.
        /// </summary>
        /// <param name="panelEntityId">EntityId of the selected panel; resolves against PANEL_ENTRIES metadata.</param>
        /// <param name="panelDisplayName">Display name shown in the sticky header strip.</param>
        /// <param name="brokenSummaryHeading">Single-line summary headline shown above the HelpBox.</param>
        /// <param name="brokenSummaryDetails">HelpBox body listing per-flag counts; null hides the HelpBox.</param>
        /// <param name="brokenSummaryHint">Small follow-up CTA (e.g. "Select a batch row to see details").</param>
        public void SetSelectedPanel(
            EntityId panelEntityId, string panelDisplayName,
            string brokenSummaryHeading = null, string brokenSummaryDetails = null, string brokenSummaryHint = null)
        {
            m_HasSelection = true;
            m_SelectedPanelEntityId = panelEntityId;
            m_SelectedPanelName = panelDisplayName;
            m_SelectedBatchIndex = -1;
            m_PendingReasonHeading = brokenSummaryHeading;
            m_PendingReasonDetails = brokenSummaryDetails;
            m_PendingReasonHint = brokenSummaryHint;
            ScheduleRefreshPanelComponentsPane();
        }

        /// <summary>
        /// Select a batch row. The reason heading + details are pre-formatted by the caller
        /// (the view owns the kick-reason flag table) and rendered into the sticky reason
        /// section verbatim.
        /// </summary>
        /// <param name="panelEntityId">EntityId of the parent panel for this batch.</param>
        /// <param name="panelDisplayName">Display name of the parent panel; shown in the sticky header strip.</param>
        /// <param name="batchIndex">passIndex of the batch within the parent panel (0-based).</param>
        /// <param name="brokenSummaryHeading">Single-line summary headline (e.g. "Batch breaking reason:").</param>
        /// <param name="brokenSummaryDetails">HelpBox body listing the kick-reason flags that fired; null hides the HelpBox.</param>
        public void SetSelectedBatch(
            EntityId panelEntityId, string panelDisplayName, int batchIndex,
            string brokenSummaryHeading, string brokenSummaryDetails)
        {
            m_HasSelection = true;
            m_SelectedPanelEntityId = panelEntityId;
            m_SelectedPanelName = panelDisplayName;
            m_SelectedBatchIndex = batchIndex;
            m_PendingReasonHeading = brokenSummaryHeading;
            m_PendingReasonDetails = brokenSummaryDetails;
            m_PendingReasonHint = null;
            ScheduleRefreshPanelComponentsPane();
        }

        public void ClearSelection()
        {
            m_HasSelection = false;
            m_SelectedPanelEntityId = EntityId.None;
            m_SelectedPanelName = null;
            m_SelectedBatchIndex = -1;
            m_PendingReasonHeading = null;
            m_PendingReasonDetails = null;
            m_PendingReasonHint = null;
            ScheduleRefreshPanelComponentsPane();
        }

        public void Dispose()
        {
            if (m_PanelComponentsListView != null)
                m_PanelComponentsListView.itemsChosen -= OnPanelComponentsItemsChosen;
            s_SharedSplitModeChanged -= OnSharedSplitModeChanged;
            m_RefreshScheduleItem?.Pause();
            m_RefreshScheduleItem = null;
        }

        void OnSharedSplitModeChanged(DetailsSplitMode mode)
        {
            if (mode == m_DetailsSplitMode)
                return;
            // Apply without persisting/re-broadcasting — the originator already did both.
            ApplyDetailsSplitModeLocally(mode);
        }

        void SetDetailsSplitMode(DetailsSplitMode mode, bool persist = true)
        {
            if (m_SplitView == null || m_ToolbarToggle == null)
                return;

            ApplyDetailsSplitModeLocally(mode);

            if (persist)
            {
                EditorPrefs.SetInt(SharedSplitModeEditorPrefKey, (int)mode);
                s_SharedSplitModeChanged?.Invoke(mode);
            }
        }

        void ApplyDetailsSplitModeLocally(DetailsSplitMode mode)
        {
            m_DetailsSplitMode = mode;
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
        //
        // The very first refresh after WireUp uses a short additional delay so that any selection
        // event triggered by view-data restoration on the host's tree view (which fires after attach
        // + first layout) lands BEFORE this refresh. Otherwise we'd render the "Select a panel row"
        // empty state for one frame and then snap to the restored selection's content — visible as a
        // blink when switching modules.
        void ScheduleRefreshPanelComponentsPane()
        {
            if (m_PanelComponentsListView == null)
                return;

            m_RefreshScheduleItem?.Pause();
            var item = m_PanelComponentsListView.schedule.Execute(() =>
            {
                m_RefreshScheduleItem = null;
                m_HasRunInitialRefresh = true;
                RefreshPanelComponentsPane();
            });
            if (!m_HasRunInitialRefresh)
                item = item.StartingIn(k_InitialRefreshSettleDelayMs);
            m_RefreshScheduleItem = item;
        }

        void RefreshPanelComponentsPane()
        {
            if (m_PanelComponentsListView == null || m_DetailsSplitMode != DetailsSplitMode.RelatedData)
                return;

            ApplyPaneVisibilityForSelection();

            if (!m_HasSelection)
            {
                // No selection — the no-selection overlay is the entire message; clear the list
                // so it doesn't render stale rows behind the overlay.
                ClearPanelComponentListEntriesReturningToPool();
                m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
                m_PanelComponentsListView.Rebuild();
                m_PanelComponentsListView.ClearSelection();
                ApplyListViewHeight();
                return;
            }

            UpdateHeaderForSelection();
            UpdateReasonSectionForSelection();

            // Pick the right component-id source for the selection scope.
            IReadOnlyList<EntityId> componentIds;
            if (m_SelectedBatchIndex >= 0)
            {
                componentIds = GetBatchOwners(m_SelectedPanelEntityId, m_SelectedBatchIndex);
                if (componentIds == null)
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoMetadataEntry);
                    UpdateComponentsFoldoutTitle(0);
                    return;
                }
                if (componentIds.Count == 0)
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoBatchComponents);
                    UpdateComponentsFoldoutTitle(0);
                    return;
                }
            }
            else
            {
                if (!m_PanelToPanelComponentEntityIds.TryGetValue(m_SelectedPanelEntityId, out var panelList))
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoMetadataEntry);
                    UpdateComponentsFoldoutTitle(0);
                    return;
                }
                if (panelList.Count == 0)
                {
                    ResetListAndShowEmptyMessage(Strings.StatusNoPanelComponents);
                    UpdateComponentsFoldoutTitle(0);
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
                            entry.displayName = UIToolkitProfilerToolbarHelpers.GetPanelDisplayName(
                                rawFrameData, componentId, out entry.iconType);
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
            // Clear after every Rebuild so a new panel/batch selection from the left tree
            // always lands on a list with no row highlighted — the highlight only appears when
            // the user explicitly clicks a row to ping its entity, which is the intended cue.
            m_PanelComponentsListView.ClearSelection();
            ApplyListViewHeight();
            UpdateComponentsFoldoutTitle(m_PanelComponentListEntries.Count);
            if (m_PanelComponentListEntries.Count == 0)
                SyncPanelComponentsListEmptyMessage(Strings.StatusListEmptyCouldNotLoadDetails);
        }

        void ResetListAndShowEmptyMessage(string message)
        {
            ClearPanelComponentListEntriesReturningToPool();
            m_PanelComponentsListView.itemsSource = m_PanelComponentListEntries;
            m_PanelComponentsListView.Rebuild();
            m_PanelComponentsListView.ClearSelection();
            ApplyListViewHeight();
            SyncPanelComponentsListEmptyMessage(message);
        }

        // Toggles the sticky regions and the no-selection overlay as a pair. Splitting this out
        // from RefreshPanelComponentsPane keeps the (potentially expensive) list rebuild and the
        // (cheap) visibility update independent — DetailsSplitMode flips can trigger the latter
        // without paying for the former.
        void ApplyPaneVisibilityForSelection()
        {
            if (m_HeaderStrip == null)
                return;
            var show = m_HasSelection;
            m_HeaderStrip.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_ReasonSection.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_ContentScrollView.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            m_NoSelectionOverlay.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void UpdateHeaderForSelection()
        {
            if (m_HeaderPanelLabel == null)
                return;
            m_HeaderPanelLabel.text = m_SelectedPanelName ?? string.Empty;
            if (m_SelectedBatchIndex >= 0)
            {
                m_HeaderBatchLabel.text = string.Format(Strings.HeaderBatchSuffix, m_SelectedBatchIndex);
                m_HeaderBatchLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_HeaderBatchLabel.text = string.Empty;
                m_HeaderBatchLabel.style.display = DisplayStyle.None;
            }
        }

        void UpdateReasonSectionForSelection()
        {
            if (m_ReasonSection == null)
                return;
            var hasHeading = !string.IsNullOrEmpty(m_PendingReasonHeading);
            var hasDetails = !string.IsNullOrEmpty(m_PendingReasonDetails);
            var hasHint = !string.IsNullOrEmpty(m_PendingReasonHint);

            m_ReasonHeading.text = m_PendingReasonHeading ?? string.Empty;
            m_ReasonHeadingRow.style.display = hasHeading ? DisplayStyle.Flex : DisplayStyle.None;

            // "Clean" state = heading present but no HelpBox details or follow-up hint. Surface
            // a success check next to the heading so "Batch rendered without breaks." reads as a
            // positive confirmation instead of a neutral status string. "GreenCheckmark" is a
            // proper 4-asset set in the global Editor Icons folder (GreenCheckmark.png,
            // GreenCheckmark@2x.png, d_GreenCheckmark.png, d_GreenCheckmark@2x.png) so
            // EditorGUIUtility.IconContent resolves the right skin + retina variant for free —
            // no in-C# tinting needed. Resolve it fresh here (not via a static cache) so the
            // icon always tracks the active skin; this runs only on selection refresh, not
            // per-frame. Already used for "complete/done" indications in render-pipelines.core
            // (CoreEditorStyles.iconComplete) and the GameCore troubleshooting window, so this
            // pane stays consistent with editor convention.
            var isCleanState = hasHeading && !hasDetails && !hasHint;
            if (isCleanState)
            {
                m_ReasonHeadingIcon.image = EditorGUIUtility.IconContent("GreenCheckmark").image;
                m_ReasonHeadingIcon.style.display = DisplayStyle.Flex;
            }
            else
                m_ReasonHeadingIcon.style.display = DisplayStyle.None;

            m_ReasonHelpBox.text = m_PendingReasonDetails ?? string.Empty;
            m_ReasonHelpBox.style.display = hasDetails ? DisplayStyle.Flex : DisplayStyle.None;

            m_ReasonHint.text = m_PendingReasonHint ?? string.Empty;
            m_ReasonHint.style.display = hasHint ? DisplayStyle.Flex : DisplayStyle.None;

            // Collapse the whole section if nothing inside it is visible — keeps the layout
            // tight for panels without breaking-reason data (e.g. the legacy panel-only view).
            m_ReasonSection.style.display = (hasHeading || hasDetails || hasHint) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateComponentsFoldoutTitle(int count)
        {
            if (m_ComponentsFoldout == null)
                return;
            m_ComponentsFoldout.text = count > 0
                ? string.Format(Strings.ComponentsFoldoutTitleCounted, count)
                : Strings.ComponentsFoldoutTitle;
        }

        // ListView lives inside a Foldout inside a ScrollView, so flex-grow won't size it (the
        // ScrollView wants its content sized to natural height). Drive the height off the item
        // count so the list shows every row without an inner scrollbar, and let the outer
        // ScrollView handle overflow across the two foldouts combined.
        void ApplyListViewHeight()
        {
            if (m_PanelComponentsListView == null)
                return;
            var itemCount = m_PanelComponentListEntries.Count;
            var height = itemCount == 0
                ? k_EmptyListHeightPx
                : itemCount * k_ListItemHeightPx + k_ListVerticalPaddingPx;
            m_PanelComponentsListView.style.height = height;
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

            // Per-row component icon. The image is data-dependent (it reflects the viewed
            // object's type), so it is assigned per row in BindPanelComponentListRow; makeItem
            // only builds the recycled element shell. Resolving in bind also means the icon
            // tracks the active skin after a light/dark flip.
            var icon = new Image
            {
                name = k_PanelComponentRowIconName,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 4;
            icon.style.flexShrink = 0;

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

            container.Add(icon);
            container.Add(nameLabel);
            container.Add(pingBtn);
            return container;
        }

        void BindPanelComponentListRow(VisualElement element, int index)
        {
            var nameLabel = element.Q<Label>(k_PanelComponentRowNameLabelName);
            var pingBtn = element.Q<UnityEngine.UIElements.Button>(k_PanelComponentRowPingButtonName);
            var icon = element.Q<Image>(k_PanelComponentRowIconName);
            if (nameLabel == null || pingBtn == null || icon == null)
                return;

            if (index >= 0 && index < m_PanelComponentListEntries.Count)
            {
                var entry = m_PanelComponentListEntries[index];
                element.userData = index;
                nameLabel.text = entry.displayName;
                pingBtn.userData = entry.entityId;

                // Icon reflects the live object's type for current-session frames. Captured / cross-
                // session frames don't record a usable type, so fall back to PanelRenderer — the only
                // IPanelComponent type on the supported public path.
                icon.image = EditorGUIUtility.ObjectContent(
                    null, entry.iconType ?? typeof(UnityEngine.UIElements.PanelRenderer)).image;

                // Keep the ping affordance visible even when it can't act — silently hiding it
                // for captured frames or remote-player captures leaves the user wondering where
                // the "select in Hierarchy" button went. Showing a disabled button + a tooltip
                // that explains the cause turns the failure into a teachable moment.
                // PingEntity is already null-safe (returns early on unresolvable EntityIds), so
                // the disabled button is a UX guard, not a correctness guard.
                var hasEntity = entry.entityId != EntityId.None;
                var pingable = UIToolkitProfilerToolbarHelpers.IsCurrentEditorSessionFrame && hasEntity;
                pingBtn.style.display = DisplayStyle.Flex;
                pingBtn.SetEnabled(pingable);
                if (pingable)
                    pingBtn.tooltip = UIToolkitProfilerToolbarHelpers.PingTooltip;
                else if (!hasEntity)
                    pingBtn.tooltip = Strings.PingUnavailableNoEntityTooltip;
                else
                    pingBtn.tooltip = Strings.PingUnavailableCrossSessionTooltip;
            }
            else
            {
                // Placeholder row recycled by ListView during virtualization — no entry to bind
                // yet, so hide the button entirely. Showing a disabled ping with a generic
                // tooltip here would attach an explanation to a row that has no real content.
                // Tooltip is explicitly cleared alongside text/userData to mirror the clear-
                // state pattern used elsewhere (see MultiColumnTreeViewWithTotal.ClearAll and
                // PackageDetailsVersionHistoryItem.RefreshState): the display:None gate already
                // prevents the tooltip from firing today, but a future change to the show logic
                // would re-expose a stale value if we leave it dangling.
                element.userData = -1;
                nameLabel.text = string.Empty;
                icon.image = null;
                pingBtn.userData = null;
                pingBtn.tooltip = string.Empty;
                pingBtn.style.display = DisplayStyle.None;
                pingBtn.SetEnabled(false);
            }
        }

        void OnPanelComponentsItemsChosen(IEnumerable<object> chosen)
        {
            if (!UIToolkitProfilerToolbarHelpers.IsCurrentEditorSessionFrame)
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
