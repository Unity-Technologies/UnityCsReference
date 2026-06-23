// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using L10n = UnityEditor.L10n;

namespace UnityEditorInternal.Profiling
{
    /// <summary>
    /// Compact dashboard-style details view for the Rendering Profiler module.
    /// Shows headline stats, draw calls breakdown, GRD coverage/pipeline (conditional),
    /// culling breakdown, and resource usage — all visible without scrolling.
    /// </summary>
    internal class RenderingDetailsViewController : ProfilerModuleViewController
    {
        // All user-facing strings live here; per Modules/ProfilerEditor BEST_PRACTICES_AGENTS.md,
        // localizable strings go through L10n.Tr() in a nested `Content` class.
        static class Content
        {
            // Card titles / section labels
            public static readonly string GpuResidentDrawer = L10n.Tr("GPU Resident Drawer");
            public static readonly string Coverage = L10n.Tr("GRD coverage");
            public static readonly string Culling = L10n.Tr("Culling");
            public static readonly string LODDistribution = L10n.Tr("LOD distribution");
            public static readonly string Pipeline = L10n.Tr("Pipeline");

            // Headline stats
            public static readonly string SetPassCalls = L10n.Tr("SetPass calls");
            public static readonly string DrawCalls = L10n.Tr("Draw calls");
            public static readonly string Triangles = L10n.Tr("Triangles");
            public static readonly string Vertices = L10n.Tr("Vertices");

            // Resource grid
            public static readonly string Buffers = L10n.Tr("Buffers");
            public static readonly string VBOUploads = L10n.Tr("VBO uploads");
            public static readonly string IBOUploads = L10n.Tr("IBO uploads");
            public static readonly string Textures = L10n.Tr("Textures");
            public static readonly string RTChanges = L10n.Tr("RT changes");
            public static readonly string SkinnedMeshes = L10n.Tr("Skinned meshes");
            public static readonly string SkinnedSuffix = L10n.Tr("visible"); // "{n} visible"

            // Draw Calls Breakdown
            public static readonly string DcStandard = L10n.Tr("Standard");
            public static readonly string DcSRPBatcher = L10n.Tr("SRP Batcher");
            public static readonly string DcBRG = L10n.Tr("BRG");
            public static readonly string DcOther = L10n.Tr("Other");

            // Coverage segments
            public static readonly string GRD = L10n.Tr("GRD");
            public static readonly string Excluded = L10n.Tr("Excluded");
            public static readonly string CoverageTooltip = L10n.Tr(
                "Shows renderers that were considered for GPU Resident Drawer in this frame. " +
                "Renderers are split between those currently using the GRD path and those excluded " +
                "from GRD because of unsupported settings, components, or rendering features.");
            public static readonly string ExclusionReasonsHeader = L10n.Tr("Exclusion reasons:");

            // Culling segments + detail rows
            public static readonly string Visible = L10n.Tr("Visible");
            public static readonly string Frustum = L10n.Tr("Frustum");
            public static readonly string Occlusion = L10n.Tr("Occlusion");
            public static readonly string GPUOcclusion = L10n.Tr("GPU occlusion");
            public static readonly string LODGroup = L10n.Tr("LOD group");
            public static readonly string SmallMesh = L10n.Tr("Small mesh");
            public static readonly string Other = L10n.Tr("Other");
            public static readonly string LayerMask = L10n.Tr("Layer mask");
            public static readonly string RenderingDisabled = L10n.Tr("Rendering disabled");

            // LOD bins
            public static readonly string LOD0 = L10n.Tr("LOD 0");
            public static readonly string LOD1 = L10n.Tr("LOD 1");
            public static readonly string LOD2 = L10n.Tr("LOD 2");
            public static readonly string LOD3Plus = L10n.Tr("LOD 3+");

            // Pipeline stages
            public static readonly string DataCollection = L10n.Tr("Data collection");
            public static readonly string BatchBuilding = L10n.Tr("Batch building");
            public static readonly string Upload = L10n.Tr("Upload");
            public static readonly string CullingSchedule = L10n.Tr("Culling schedule");
            public static readonly string CpuToGpuUpload = L10n.Tr("CPU to GPU upload");
            public static readonly string Transform = L10n.Tr("Transform");
            public static readonly string Motion = L10n.Tr("Motion");
            public static readonly string Probe = L10n.Tr("Probe");
            public static readonly string ComponentOverride = L10n.Tr("Component override");

            // Trailing-value formats
            public static readonly string PercentVisible = L10n.Tr("{0}% visible");      // string.Format
            public static readonly string PercentHaveLOD = L10n.Tr("{0}% have LOD");

            // GRD card title-row badges (right-aligned next to the title)
            public static readonly string AssetIssuesBadge = L10n.Tr("⚠ {0} renderer issues");
            public static readonly string AssetIssuesTooltipWithDetail = L10n.Tr("Breakdown: {0}.");
            public static readonly string AssetIssuesTooltip = L10n.Tr("Detected during GRD classification. These are asset/setup issues that other rendering paths may tolerate silently. They are not GRD compatibility exclusions and are not counted in GRD Coverage.");
            public static readonly string InactiveBadge = L10n.Tr("{0} inactive");
            public static readonly string InactiveTooltip = L10n.Tr("Renderers whose GameObject or Renderer component is currently disabled. Not drawn by any path; not counted in Coverage.");
            public static readonly string BadgeSeparator = "·"; // visual only — no translation needed

            // Empty state
            public static readonly string NoFrameData = L10n.Tr("No frame data available. Start a recording or select a frame to view rendering details.");

            // Legacy IMGUI stats fallback text
            public static readonly string LegacyBatching = L10n.Tr(
                "Dynamic Batching: {0} calls, {1} batches\n" +
                "Static Batching: {2} calls, {3} batches\n" +
                "Instancing: {4} calls, {5} batches");
        }

        const string k_UssPath = "Profiler/Modules/Rendering/RenderingDetailsView.uss";

        // ==== Color palette ====
        // Design intent: within EACH card, every segment is a distinct color (so the user can
        // read a stacked bar without consulting the legend). Across cards, colors do repeat —
        // we only have 9 hues in the palette and 22 segments total — but we choose those repeats
        // so they either carry the same meaning (e.g. olive == "good" in Coverage AND Culling)
        // or appear in unrelated cards a user wouldn't read side-by-side.
        //
        // Palette (kept from the original implementation; the soft tones read better against
        // the editor background than the saturated Tableau set we briefly switched to):
        //   B  (54,160,199)  blue        Y  (240,208,68)  yellow      G  (160,160,160) gray
        //   O  (123,158,5)   olive       M  (99,220,195)  mint        DT (47,113,101)  dark teal
        //   R  (243,157,26)  orange      SR (202,98,98)   soft red    DG (100,100,100) darker gray

        // Pipeline — 4 stages running in order; uses one color per stage (no semantic ramp).
        static readonly Color k_PipeDataColl = new( 54 / 255f, 160 / 255f, 199 / 255f); // B
        static readonly Color k_PipeBatchBld = new(123 / 255f, 158 / 255f,   5 / 255f); // O
        static readonly Color k_PipeUpload   = new(243 / 255f, 157 / 255f,  26 / 255f); // R
        static readonly Color k_PipeCullSched  = new(240 / 255f, 208 / 255f,  68 / 255f); // Y

        // Draw Calls Breakdown — categorical.
        static readonly Color k_DcStandard   = new(160 / 255f, 160 / 255f, 160 / 255f); // G
        static readonly Color k_DcSRPBatcher = new( 54 / 255f, 160 / 255f, 199 / 255f); // B
        static readonly Color k_DcBRG        = new(123 / 255f, 158 / 255f,   5 / 255f); // O
        static readonly Color k_DcOther      = new(100 / 255f, 100 / 255f, 100 / 255f); // DG

        // Coverage — semantic binary: olive = on path, orange = off path.
        static readonly Color k_CovGrd       = new(123 / 255f, 158 / 255f,   5 / 255f); // O
        static readonly Color k_CovExcluded  = new(243 / 255f, 157 / 255f,  26 / 255f); // R

        // Culling — 7 segments. Visible reuses Coverage's olive ("good" continuity); the rest
        // are picked so all 7 are mutually distinct within this card.
        static readonly Color k_CullVisible      = new(123 / 255f, 158 / 255f,   5 / 255f); // O  (matches Coverage GRD)
        static readonly Color k_CullFrustum      = new(240 / 255f, 208 / 255f,  68 / 255f); // Y
        static readonly Color k_CullOcclusion    = new(202 / 255f,  98 / 255f,  98 / 255f); // SR
        static readonly Color k_CullGpuOcclusion = new( 47 / 255f, 113 / 255f, 101 / 255f); // DT
        static readonly Color k_CullLODGroup     = new( 99 / 255f, 220 / 255f, 195 / 255f); // M
        static readonly Color k_CullSmallMesh    = new(100 / 255f, 100 / 255f, 100 / 255f); // DG
        static readonly Color k_CullOther        = new(160 / 255f, 160 / 255f, 160 / 255f); // G

        // LOD Distribution — sequential warm (close) → cool (distant).
        static readonly Color k_Lod0     = new(202 / 255f,  98 / 255f,  98 / 255f); // SR
        static readonly Color k_Lod1     = new(243 / 255f, 157 / 255f,  26 / 255f); // R
        static readonly Color k_Lod2     = new(240 / 255f, 208 / 255f,  68 / 255f); // Y
        static readonly Color k_Lod3Plus = new( 99 / 255f, 220 / 255f, 195 / 255f); // M

        static readonly string[] k_DrawCallTotalCounters =
        {
            "Standard Draw Calls Count", "Standard Indirect Draw Calls Count",
            "Standard Instanced Draw Calls Count", "SRP Batcher Draw Calls Count",
            "BRG Draw Calls Count", "BRG Indirect Draw Calls Count",
            "Null Geometry Draw Calls Count", "Null Geometry Indirect Draw Calls Count"
        };
        static readonly string[] k_StandardDrawCallCounters =
        {
            "Standard Draw Calls Count", "Standard Indirect Draw Calls Count", "Standard Instanced Draw Calls Count"
        };
        static readonly string[] k_BrgDrawCallCounters =
        {
            "BRG Draw Calls Count", "BRG Indirect Draw Calls Count"
        };
        static readonly string[] k_NullGeoDrawCallCounters =
        {
            "Null Geometry Draw Calls Count", "Null Geometry Indirect Draw Calls Count"
        };

        RenderingProfilerModule m_Module;
        ScrollView m_Root;
        VisualElement m_Content;

        // Persist foldout state across frame changes
        bool m_ExcludedReasonsOpen;
        bool m_CullingDetailsOpen;
        bool m_PipelineDetailsOpen;

        public RenderingDetailsViewController(ProfilerWindow profilerWindow, RenderingProfilerModule module)
            : base(profilerWindow)
        {
            m_Module = module;
        }

        protected override VisualElement CreateView()
        {
            var outer = new VisualElement { name = "rendering-details-view" };
            var uss = EditorGUIUtility.Load(k_UssPath) as StyleSheet;
            if (uss != null) outer.styleSheets.Add(uss);
            // Theme class drives chevron image variant — see RenderingDetailsView.uss .rd-pro-skin selectors.
            if (EditorGUIUtility.isProSkin)
                outer.AddToClassList("rd-pro-skin");

            // IMGUI toolbar — module's toolbar is still IMGUI; wrap it for embedding inside UIElements.
            var toolbar = new IMGUIContainer(() => m_Module.DrawToolbar(Rect.zero));
            toolbar.style.flexShrink = 0;
            outer.Add(toolbar);

            m_Root = new ScrollView(ScrollViewMode.Vertical);
            m_Root.style.flexGrow = 1;
            m_Content = new VisualElement();
            m_Content.AddToClassList("rd-content");
            m_Root.Add(m_Content);
            outer.Add(m_Root);

            ReloadData(ProfilerWindow.selectedFrameIndex);
            ProfilerWindow.SelectedFrameIndexChanged += OnFrameChanged;
            return outer;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            ProfilerWindow.SelectedFrameIndexChanged -= OnFrameChanged;
            base.Dispose(disposing);
        }

        void OnFrameChanged(long idx) => ReloadData(idx);

        void ReloadData(long selectedFrameIndex)
        {
            m_Content.Clear();
            int frame = Convert.ToInt32(selectedFrameIndex);
            if (frame < 0) { AddNoData(); return; }

            using var f = ProfilerDriver.GetRawFrameDataView(frame, 0);
            if (f == null || !f.valid) { AddNoData(); return; }

            // === Card 1: Rendering Overview (headline + draw calls + resources) ===
            BuildRenderingOverviewCard(f);

            // === Card 2: GPU Resident Drawer (conditional) ===
            // Show whenever ANY tracked renderer category is non-zero — including the
            // informational ones (NonRendering / Inactive). Hiding the card when only those
            // are non-zero would silently swallow asset-issue warnings.
            int grd = GetCounter(f, GRDCounterNames.k_GRDRenderers);
            int excl = GetCounter(f, GRDCounterNames.k_ExcludedRenderers);
            int nonRendering = GetCounter(f, GRDCounterNames.k_NonRenderingRenderers);
            int inactive = GetCounter(f, GRDCounterNames.k_InactiveRenderers);
            if (grd > 0 || excl > 0 || nonRendering > 0 || inactive > 0)
                BuildGRDCard(f, grd, excl, nonRendering, inactive);
        }

        // ==================== CARD 1: RENDERING OVERVIEW ====================

        void BuildRenderingOverviewCard(FrameDataView f)
        {
            var card = MakeCard();

            // Headline numbers
            var headline = new VisualElement();
            headline.AddToClassList("rd-headline");

            var batchesCount = GetCounterLong(f, "Batches Count");
            var setPass = GetCounterLong(f, "SetPass Calls Count");

            if (setPass == -1 && batchesCount == -1)
            {
                card.Add(new Label(ProfilerDriver.GetOverviewText(ProfilerArea.Rendering,
                    ProfilerWindow.GetActiveVisibleFrameIndex())));
                m_Content.Add(card);
                return;
            }

            // Calculate total draw calls
            long totalDC;
            bool isNewStats = batchesCount == -1;

            if (isNewStats)
            {
                totalDC = 0;
                foreach (var t in k_DrawCallTotalCounters)
                {
                    long v = GetCounterLong(f, t);
                    if (v > 0) totalDC += v;
                }
            }
            else
            {
                totalDC = GetCounterLong(f, "Draw Calls Count");
            }

            AddStatNumber(headline, Content.SetPassCalls, FmtLong(setPass));
            AddStatNumber(headline, Content.DrawCalls, FmtLong(totalDC));
            AddStatNumber(headline, Content.Triangles, FmtCounter(f, "Triangles Count"));
            AddStatNumber(headline, Content.Vertices, FmtCounter(f, "Vertices Count"));
            card.Add(headline);

            // Draw Calls Breakdown stacked bar
            if (isNewStats)
            {
                long standard = Sum(f, k_StandardDrawCallCounters);
                long srp = GetCounterLong(f, "SRP Batcher Draw Calls Count"); if (srp < 0) srp = 0;
                long brg = Sum(f, k_BrgDrawCallCounters);
                long other = Sum(f, k_NullGeoDrawCallCounters);

                if (totalDC > 0)
                {
                    AddStackedBar(card, Content.DrawCalls, FmtLong(totalDC), totalDC, new[] {
                        (Content.DcStandard, standard, k_DcStandard),
                        (Content.DcSRPBatcher, srp, k_DcSRPBatcher),
                        (Content.DcBRG, brg, k_DcBRG),
                        (Content.DcOther, other, k_DcOther),
                    });
                }
            }
            else
            {
                // Old stats: Dynamic/Static/Instancing batching text
                var batchInfo = new Label(string.Format(Content.LegacyBatching,
                    FmtCounter(f, "Dynamic Batched Draw Calls Count"), FmtCounter(f, "Dynamic Batches Count"),
                    FmtCounter(f, "Static Batched Draw Calls Count"), FmtCounter(f, "Static Batches Count"),
                    FmtCounter(f, "Instanced Batched Draw Calls Count"), FmtCounter(f, "Instanced Batches Count")));
                batchInfo.AddToClassList("rd-detail-text");
                card.Add(batchInfo);
            }

            // Resources inline
            AddResourcesInline(card, f);

            m_Content.Add(card);
        }

        void AddResourcesInline(VisualElement card, FrameDataView f)
        {
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new StyleColor(new Color(1, 1, 1, 0.06f));
            divider.style.marginTop = 6;
            divider.style.marginBottom = 4;
            card.Add(divider);

            var grid = new VisualElement();
            grid.AddToClassList("rd-resource-grid");

            var bufCount = GetCounterLong(f, "Used Buffers Count");
            if (bufCount >= 0) AddResourceStat(grid, Content.Buffers, $"{FmtLong(bufCount)} / {FmtBytes(f, "Used Buffers Bytes")}");

            var vboCount = GetCounterLong(f, "Vertex Buffer Upload In Frame Count");
            if (vboCount >= 0) AddResourceStat(grid, Content.VBOUploads, $"{FmtLong(vboCount)} / {FmtBytes(f, "Vertex Buffer Upload In Frame Bytes")}");

            var iboCount = GetCounterLong(f, "Index Buffer Upload In Frame Count");
            if (iboCount >= 0) AddResourceStat(grid, Content.IBOUploads, $"{FmtLong(iboCount)} / {FmtBytes(f, "Index Buffer Upload In Frame Bytes")}");

            var texCount = GetCounterLong(f, "Used Textures Count");
            if (texCount >= 0) AddResourceStat(grid, Content.Textures, $"{FmtLong(texCount)} / {FmtBytes(f, "Used Textures Bytes")}");

            var rtChanges = GetCounterLong(f, "Render Textures Changes Count");
            if (rtChanges >= 0) AddResourceStat(grid, Content.RTChanges, FmtLong(rtChanges));

            var skinned = GetCounterLong(f, "Visible Skinned Meshes Count");
            if (skinned >= 0) AddResourceStat(grid, Content.SkinnedMeshes, $"{FmtLong(skinned)} {Content.SkinnedSuffix}");

            card.Add(grid);
        }

        // ==================== CARD 2: GPU RESIDENT DRAWER ====================

        void BuildGRDCard(FrameDataView f, int grd, int excl, int nonRendering, int inactive)
        {
            var card = MakeCard();

            // Title row: card title on the left, issue badges right-aligned. The badges
            // (asset-defect warning + inactive count) live here so users see them before
            // the body content but without spending an extra vertical row. Detail
            // breakdowns (e.g. how many missing-mesh vs null-material) move to tooltips.
            BuildGRDCardTitleRow(card, f, nonRendering, inactive);

            BuildGRDCoverageContent(card, f, grd, excl);
            BuildCullingContent(card, f);
            BuildLODDistributionContent(card, f);
            BuildGRDPipelineContent(card, f);

            m_Content.Add(card);
        }

        void BuildGRDCardTitleRow(VisualElement card, FrameDataView f, int nonRendering, int inactive)
        {
            var row = new VisualElement();
            row.AddToClassList("rd-card-title-row");

            var title = new Label(Content.GpuResidentDrawer);
            title.AddToClassList("rd-card-title");
            row.Add(title);

            // Spacer: pushes badges to the right edge.
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            if (nonRendering > 0)
            {
                var sub = BuildSubCountSummary(f, GRDCounterNames.k_NonRenderingCategoryReasonNames);
                var badge = new Label(string.Format(Content.AssetIssuesBadge, FmtInt(nonRendering)));
                badge.AddToClassList("rd-card-title__warn");
                badge.tooltip = sub != null
                    ? string.Format(Content.AssetIssuesTooltipWithDetail, sub) + "\n\n" + Content.AssetIssuesTooltip
                    : Content.AssetIssuesTooltip;
                row.Add(badge);
            }

            if (nonRendering > 0 && inactive > 0)
            {
                var sep = new Label(Content.BadgeSeparator);
                sep.AddToClassList("rd-card-title__badge-sep");
                row.Add(sep);
            }

            if (inactive > 0)
            {
                var badge = new Label(string.Format(Content.InactiveBadge, FmtInt(inactive)));
                badge.AddToClassList("rd-card-title__info");
                badge.tooltip = Content.InactiveTooltip;
                row.Add(badge);
            }

            card.Add(row);
        }

        // Coverage card body (Coverage row only). Issues banner is rendered separately by
        // BuildGRDCard to keep it above this row. Stacked bar is GRD vs Excluded only —
        // NonRendering and Inactive don't represent a GRD-vs-SRP decision and live in the
        // banner above. Foldout breaks down the Excluded category by reason.
        void BuildGRDCoverageContent(VisualElement card, FrameDataView f, int grd, int excl)
        {
            int coverageDenom = grd + excl;
            if (coverageDenom <= 0)
                return; // Nothing competing for the GRD path; skip the row entirely.

            int pct = (int)((float)grd / coverageDenom * 100f);

            var detail = AddStackedBar(card, Content.Coverage, $"{pct}%", coverageDenom, new[] {
                    (Content.GRD, (long)grd, k_CovGrd),
                    (Content.Excluded, (long)excl, k_CovExcluded),
                },
                expandable: excl > 0,
                initialExpanded: m_ExcludedReasonsOpen,
                onExpandedChanged: v => m_ExcludedReasonsOpen = v,
                tooltip: Content.CoverageTooltip);

            if (detail == null) return;

            var header = new Label(Content.ExclusionReasonsHeader);
            header.AddToClassList("rd-reason-header");
            detail.Add(header);

            foreach (var counterName in GRDCounterNames.k_ExcludedCategoryReasonNames)
            {
                int count = GetCounter(f, counterName);
                if (count <= 0) continue;
                AddReasonRow(detail, counterName, count);
            }
        }

        void AddReasonRow(VisualElement parent, string counterName, int count)
        {
            if (!GRDCounterNames.k_ExclusionReasonDisplayLabels.TryGetValue(counterName, out string displayName))
                displayName = counterName.StartsWith("Excl: ") ? counterName.Substring(6) : counterName;
            var row = new VisualElement();
            row.AddToClassList("rd-reason-row");
            if (GRDCounterNames.k_ExclusionReasonTooltips.TryGetValue(counterName, out string tooltip))
                row.tooltip = tooltip;

            row.Add(new Label(displayName) { pickingMode = PickingMode.Ignore });
            var valLbl = new Label(FmtInt(count));
            valLbl.AddToClassList("rd-reason-value");
            row.Add(valLbl);
            parent.Add(row);
        }

        // Builds a "X foo, Y bar" string from per-reason counters. Returns null when no
        // sub-reasons have non-zero counts (caller falls back to the count-only format).
        string BuildSubCountSummary(FrameDataView f, string[] reasonCounterNames)
        {
            System.Text.StringBuilder sb = null;
            foreach (var name in reasonCounterNames)
            {
                int n = GetCounter(f, name);
                if (n <= 0) continue;
                string display = name.StartsWith("Excl: ") ? name.Substring(6) : name;
                if (sb == null) sb = new System.Text.StringBuilder();
                else sb.Append(", ");
                sb.Append(FmtInt(n)).Append(' ').Append(display.ToLowerInvariant());
            }
            return sb?.ToString();
        }

        void BuildCullingContent(VisualElement card, FrameDataView f)
        {
            // All 9 categories — sum equals total instances processed by camera view (matches backend invariant).
            int visible = GetCounter(f, GRDCounterNames.k_VisibleInstances);
            int renderingDisabled = GetCounter(f, GRDCounterNames.k_DisabledRendererCulled);
            int layer = GetCounter(f, GRDCounterNames.k_LayerCulled);
            int frustum = GetCounter(f, GRDCounterNames.k_FrustumCulled);
            int occlusion = GetCounter(f, GRDCounterNames.k_OcclusionCulled);
            int gpuOcclusion = GetCounter(f, GRDCounterNames.k_GpuOcclusionCulled);
            int lodCulled = GetCounter(f, GRDCounterNames.k_LODGroupCulled);
            int smallMesh = GetCounter(f, GRDCounterNames.k_SmallMeshCulled);
            int other = GetCounter(f, GRDCounterNames.k_OtherCulled);

            // "Other" bar segment rolls up the long tail (Layer + Rendering Disabled + Editor cuts).
            // The expandable detail breaks them out individually so percentages still match.
            int otherRollup = layer + renderingDisabled + other;
            int total = visible + frustum + occlusion + gpuOcclusion + lodCulled + smallMesh + otherRollup;

            if (total <= 0) return;

            int visPct = (int)((float)visible / total * 100f);

            var detail = AddStackedBar(card, Content.Culling, string.Format(Content.PercentVisible, visPct), total, new[] {
                    (Content.Visible, (long)visible, k_CullVisible),
                    (Content.Frustum, (long)frustum, k_CullFrustum),
                    (Content.Occlusion, (long)occlusion, k_CullOcclusion),
                    (Content.GPUOcclusion, (long)gpuOcclusion, k_CullGpuOcclusion),
                    (Content.LODGroup, (long)lodCulled, k_CullLODGroup),
                    (Content.SmallMesh, (long)smallMesh, k_CullSmallMesh),
                    (Content.Other, (long)otherRollup, k_CullOther),
                },
                expandable: otherRollup > 0,
                initialExpanded: m_CullingDetailsOpen,
                onExpandedChanged: v => m_CullingDetailsOpen = v);

            if (detail == null) return;

            AddCullingDetailRow(detail, Content.LayerMask, layer);
            AddCullingDetailRow(detail, Content.RenderingDisabled, renderingDisabled);
            AddCullingDetailRow(detail, Content.Other, other);
        }

        void AddCullingDetailRow(VisualElement parent, string label, int count)
        {
            if (count <= 0) return;
            var row = new VisualElement();
            row.AddToClassList("rd-reason-row");
            row.Add(new Label(label) { pickingMode = PickingMode.Ignore });
            var valLbl = new Label(FmtInt(count));
            valLbl.AddToClassList("rd-reason-value");
            row.Add(valLbl);
            parent.Add(row);
        }

        void BuildLODDistributionContent(VisualElement card, FrameDataView f)
        {
            int lod0 = GetCounter(f, GRDCounterNames.k_LOD0);
            int lod1 = GetCounter(f, GRDCounterNames.k_LOD1);
            int lod2 = GetCounter(f, GRDCounterNames.k_LOD2);
            int lod3Plus = GetCounter(f, GRDCounterNames.k_LOD3Plus);
            int withLod = lod0 + lod1 + lod2 + lod3Plus;

            if (withLod <= 0) return;

            // % of rendered instances that participate in an LOD group. Both numerator and
            // denominator are camera-view instance counts (per-frame) so the unit is consistent;
            // the previous "% of GRD renderers with LOD" mixed instance count over renderer count.
            // Fall back to withLod when totalInstances is unavailable (older players that don't
            // emit the counter yet).
            int totalInstances = GetCounter(f, GRDCounterNames.k_TotalInstances);
            int reference = totalInstances > 0 ? totalInstances : withLod;
            int withLodPct = (int)((float)withLod / reference * 100f);

            AddStackedBar(card, Content.LODDistribution, string.Format(Content.PercentHaveLOD, withLodPct), withLod, new[] {
                (Content.LOD0, (long)lod0, k_Lod0),
                (Content.LOD1, (long)lod1, k_Lod1),
                (Content.LOD2, (long)lod2, k_Lod2),
                (Content.LOD3Plus, (long)lod3Plus, k_Lod3Plus),
            });
        }

        void BuildGRDPipelineContent(VisualElement card, FrameDataView f)
        {
            long dataColl = GetCounterLong(f, GRDCounterNames.k_DataCollection); if (dataColl < 0) dataColl = 0;
            long batchBld = GetCounterLong(f, GRDCounterNames.k_BatchBuilding); if (batchBld < 0) batchBld = 0;
            long upload = GetCounterLong(f, GRDCounterNames.k_CpuToGpuUpload); if (upload < 0) upload = 0;
            long cullSched = GetCounterLong(f, GRDCounterNames.k_CullingSchedule); if (cullSched < 0) cullSched = 0;
            long totalNs = dataColl + batchBld + upload + cullSched;

            if (totalNs <= 0) return;

            var detail = AddStackedBar(card, Content.Pipeline, FmtNsAsMs(totalNs), totalNs, new[] {
                    (Content.DataCollection, dataColl, k_PipeDataColl),
                    (Content.BatchBuilding, batchBld, k_PipeBatchBld),
                    (Content.Upload, upload, k_PipeUpload),
                    (Content.CullingSchedule, cullSched, k_PipeCullSched),
                },
                expandable: true,
                initialExpanded: m_PipelineDetailsOpen,
                onExpandedChanged: v => m_PipelineDetailsOpen = v,
                valueFormatter: FmtNsAsMs);

            long maxNs = Math.Max(dataColl, Math.Max(batchBld, Math.Max(upload, cullSched)));
            if (maxNs <= 0) maxNs = 1;

            AddWaterfallRow(detail, Content.DataCollection, dataColl, maxNs, k_PipeDataColl, 0);
            AddWaterfallRow(detail, Content.BatchBuilding, batchBld, maxNs, k_PipeBatchBld, 0);
            AddWaterfallRow(detail, Content.CpuToGpuUpload, upload, maxNs, k_PipeUpload, 0);

            long transform = GetCounterLong(f, GRDCounterNames.k_TransformDispatch); if (transform < 0) transform = 0;
            long motion = GetCounterLong(f, GRDCounterNames.k_MotionDispatch); if (motion < 0) motion = 0;
            long probe = GetCounterLong(f, GRDCounterNames.k_ProbeDispatch); if (probe < 0) probe = 0;
            long compOverride = GetCounterLong(f, GRDCounterNames.k_ComponentOverride); if (compOverride < 0) compOverride = 0;

            // Upload sub-stages share parent (Pipe Upload) hue with depth-1 indent so the visual
            // grouping reads at a glance.
            AddWaterfallRow(detail, Content.Transform, transform, maxNs, k_PipeUpload, 1);
            AddWaterfallRow(detail, Content.Motion, motion, maxNs, k_PipeUpload, 1);
            AddWaterfallRow(detail, Content.Probe, probe, maxNs, k_PipeUpload, 1);
            AddWaterfallRow(detail, Content.ComponentOverride, compOverride, maxNs, k_PipeUpload, 1);

            AddWaterfallRow(detail, Content.CullingSchedule, cullSched, maxNs, k_PipeCullSched, 0);
        }

        // ==================== UI COMPONENTS ====================

        void AddNoData()
        {
            var lbl = new Label(Content.NoFrameData);
            lbl.style.opacity = 0.5f;
            lbl.style.marginTop = 16;
            lbl.style.marginLeft = 10;
            m_Content.Add(lbl);
        }

        VisualElement MakeCard()
        {
            var card = new VisualElement();
            card.AddToClassList("rd-card");
            return card;
        }

        void AddStatNumber(VisualElement parent, string label, string value)
        {
            var stat = new VisualElement();
            stat.AddToClassList("rd-stat");
            var valLbl = new Label(value);
            valLbl.AddToClassList("rd-stat__value");
            stat.Add(valLbl);
            var nameLbl = new Label(label);
            nameLbl.AddToClassList("rd-stat__name");
            stat.Add(nameLbl);
            parent.Add(stat);
        }

        // Adds a stacked-bar visualization row (label + bar + trailing value + horizontal legend).
        // When `expandable` is true, the row becomes a clickable toggle that reveals an inline detail
        // container. The legend ALWAYS renders below the bar regardless of expand state — only the
        // detail container is hidden when collapsed.
        //
        // Why not UI Toolkit Foldout: Foldout's structure is [toggle][content], with everything in
        // content hidden when collapsed. Our design needs [bar][legend always][detail toggleable],
        // which doesn't map cleanly to Foldout. We use a manual chevron + click handler and add
        // keyboard support (Space/Enter) for accessibility parity.
        //
        // Returns: the inline detail container if `expandable`, else null.
        // valueFormatter controls how each segment's value renders in tooltip + legend
        // (e.g. FmtLong for counts, FmtNsAsMs for time-based bars).
        VisualElement AddStackedBar(
            VisualElement parent, string label, string trailingValue, long total,
            (string name, long value, Color color)[] segments,
            bool expandable = false,
            bool initialExpanded = false,
            Action<bool> onExpandedChanged = null,
            Func<long, string> valueFormatter = null,
            string tooltip = null)
        {
            var fmt = valueFormatter ?? FmtLong;

            var row = BuildBarRow(label, trailingValue, total, segments, fmt, expandable, tooltip);
            parent.Add(row);

            var legendRow = BuildLegendRow(segments, fmt);
            parent.Add(legendRow);

            if (!expandable)
                return null;

            var detail = new VisualElement();
            detail.AddToClassList("rd-bar-row__detail");
            parent.Add(detail);

            bool expanded = initialExpanded;
            void Apply()
            {
                row.EnableInClassList("rd-bar-row--expanded", expanded);
                detail.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
            Apply();

            void Toggle()
            {
                expanded = !expanded;
                Apply();
                onExpandedChanged?.Invoke(expanded);
            }

            row.RegisterCallback<ClickEvent>(_ => Toggle());

            // Keyboard accessibility: Space/Enter toggles when row has focus.
            row.focusable = true;
            row.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    Toggle();
                    evt.StopPropagation();
                }
            });

            return detail;
        }

        // [chevron-slot | label | bar | value] row.
        VisualElement BuildBarRow(string label, string trailingValue, long total,
            (string name, long value, Color color)[] segments, Func<long, string> fmt, bool expandable,
            string tooltip = null)
        {
            var row = new VisualElement();
            row.AddToClassList("rd-bar-row");
            if (expandable) row.AddToClassList("rd-bar-row--expandable");
            if (tooltip != null) row.tooltip = tooltip;

            var labelArea = new VisualElement();
            labelArea.AddToClassList("rd-bar-row__label");
            var chevron = new VisualElement();
            chevron.AddToClassList("rd-bar-row__chevron");
            chevron.pickingMode = PickingMode.Ignore;
            labelArea.Add(chevron);
            var labelText = new Label(label) { pickingMode = PickingMode.Ignore };
            labelText.AddToClassList("rd-bar-row__label-text");
            labelArea.Add(labelText);
            row.Add(labelArea);

            row.Add(BuildStackedBar(total, segments, fmt));

            var valueEl = new Label(trailingValue ?? string.Empty);
            valueEl.AddToClassList("rd-bar-row__value");
            row.Add(valueEl);

            return row;
        }

        // Stacked horizontal bar with one segment per non-zero entry; tooltips include name + value + %.
        VisualElement BuildStackedBar(long total, (string name, long value, Color color)[] segments, Func<long, string> fmt)
        {
            var bar = new VisualElement();
            bar.AddToClassList("rd-stacked-bar");
            foreach (var (name, value, color) in segments)
            {
                if (value <= 0) continue;
                float pct = (float)value / total * 100f;
                var seg = new VisualElement();
                seg.style.width = new Length(pct, LengthUnit.Percent);
                seg.style.backgroundColor = new StyleColor(color);
                seg.AddToClassList("rd-stacked-bar__segment");
                seg.tooltip = $"{name}: {fmt(value)} ({(int)pct}%)";
                bar.Add(seg);
            }
            return bar;
        }

        // Horizontal legend (color dot + "Name value" per non-zero entry), wrapped in an indented row.
        VisualElement BuildLegendRow((string name, long value, Color color)[] segments, Func<long, string> fmt)
        {
            var legendRow = new VisualElement();
            legendRow.AddToClassList("rd-bar-row__legend-row");

            var legend = new VisualElement();
            legend.AddToClassList("rd-stacked-legend");
            foreach (var (name, value, color) in segments)
            {
                if (value <= 0) continue;
                var item = new VisualElement();
                item.AddToClassList("rd-stacked-legend__item");
                var dot = new VisualElement();
                dot.AddToClassList("rd-dot");
                dot.style.backgroundColor = new StyleColor(color);
                item.Add(dot);
                item.Add(new Label($"{name} {fmt(value)}"));
                legend.Add(item);
            }
            legendRow.Add(legend);
            return legendRow;
        }

        void AddWaterfallRow(VisualElement parent, string name, long valueNs, long maxNs, Color color, int depth)
        {
            var row = new VisualElement();
            row.AddToClassList("rd-waterfall-row");
            if (depth > 0) row.style.marginLeft = depth * 16;

            var nameLbl = new Label(name);
            nameLbl.AddToClassList("rd-waterfall-row__name");
            row.Add(nameLbl);

            var barBg = new VisualElement();
            barBg.AddToClassList("rd-waterfall-row__bar-bg");
            float pct = maxNs > 0 ? (float)valueNs / maxNs * 100f : 0f;
            var fill = new VisualElement();
            fill.AddToClassList("rd-waterfall-row__bar-fill");
            fill.style.width = new Length(pct, LengthUnit.Percent);
            fill.style.backgroundColor = new StyleColor(color);
            barBg.Add(fill);
            row.Add(barBg);

            var valLbl = new Label(FmtNsAsMs(valueNs));
            valLbl.AddToClassList("rd-waterfall-row__value");
            row.Add(valLbl);

            parent.Add(row);
        }

        void AddResourceStat(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("rd-resource-row");
            var nameLbl = new Label(label);
            nameLbl.AddToClassList("rd-resource-row__name");
            row.Add(nameLbl);
            var valLbl = new Label(value);
            valLbl.AddToClassList("rd-resource-row__value");
            row.Add(valLbl);
            parent.Add(row);
        }

        // ==================== DATA HELPERS ====================

        static int GetCounter(FrameDataView f, string name)
        {
            int id = f.GetMarkerId(name);
            return id == -1 ? 0 : f.GetCounterValueAsInt(id);
        }

        static long GetCounterLong(FrameDataView f, string name)
        {
            int id = f.GetMarkerId(name);
            return id == -1 ? -1 : f.GetCounterValueAsLong(id);
        }

        static long Sum(FrameDataView f, string[] names)
        {
            long total = 0;
            foreach (var n in names)
            {
                long v = GetCounterLong(f, n);
                if (v > 0) total += v;
            }
            return total;
        }

        static string FmtLong(long v)
        {
            if (v < 0) return "N/A";
            if (v >= 1_000_000) return $"{v / 1_000_000f:F2}M";
            if (v >= 1_000) return $"{v / 1_000f:F1}k";
            return v.ToString();
        }

        static string FmtInt(int v)
        {
            if (v >= 1_000_000) return $"{v / 1_000_000f:F2}M";
            if (v >= 1_000) return $"{v / 1_000f:F1}k";
            return v.ToString("N0");
        }

        static string FmtCounter(FrameDataView f, string name)
        {
            long v = GetCounterLong(f, name);
            return FmtLong(v);
        }

        static string FmtBytes(FrameDataView f, string name)
        {
            long v = GetCounterLong(f, name);
            return v < 0 ? "N/A" : EditorUtility.FormatBytes(v);
        }

        static string FmtNsAsMs(long ns) => $"{ns / 1_000_000f:F2} ms";
    }
}
