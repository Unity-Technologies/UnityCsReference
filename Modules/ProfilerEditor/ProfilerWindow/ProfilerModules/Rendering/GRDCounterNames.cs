// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Rendering;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditorInternal.Profiling
{
    // ============================================================================================
    // WIRE-FORMAT MIRROR — KEEP IN SYNC WITH:
    //   Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/Debug/GRDProfilerCounters.cs
    // ============================================================================================
    // Why a copy: ProfilerEditor is a built-in Editor module and cannot reference SRP packages
    // (would invert the layering). The runtime side emits ProfilerCounterValues by string name;
    // this side reads them by string name. The strings ARE the wire format.
    //
    // Drift detection: GRDProfilerCounters.GetCounterNamesForValidation() returns the runtime's
    // canonical list. A test in the package's test assembly cross-checks against this file
    // (see GRDProfilerCounterNamesTests). If you add or rename a counter, update BOTH files
    // and the test will pass.
    // ============================================================================================
    static partial class GRDCounterNames
    {
        internal const string k_CategoryName = "GPU Resident Drawer";

        // Liveness — per-frame "GRD is active this frame" flag (mirror of GRDProfilerCounters).
        internal const string k_Active = "GRD Active";

        // Pipeline Timing (top-level stages)
        internal const string k_DataCollection = "Data Collection";
        internal const string k_BatchBuilding = "Batch Building";
        internal const string k_CpuToGpuUpload = "CPU to GPU Upload";
        internal const string k_CullingSchedule = "Culling Schedule";

        // Pipeline Timing (upload sub-breakdown)
        internal const string k_TransformDispatch = "Transform Dispatch";
        internal const string k_MotionDispatch = "Motion Dispatch";
        internal const string k_ProbeDispatch = "Probe Dispatch";
        internal const string k_ComponentOverride = "Component Override";

        // Coverage — see GRDProfilerCounters.cs for the GRD/Excluded/NonRendering/Inactive
        // semantics. Coverage % = GRD / (GRD + Excluded). NonRendering and Inactive are
        // surfaced in the UI as informational, not in the ratio.
        internal const string k_GRDRenderers = "GRD Renderers";
        internal const string k_ExcludedRenderers = "Excluded Renderers";
        internal const string k_NonRenderingRenderers = "Non-Rendering Renderers";
        internal const string k_InactiveRenderers = "Inactive Renderers";
        internal const string k_CoveragePercent = "Coverage %";

        // Culling — k_TotalInstances == sum of the 9 sub-counters by construction.
        // Emitted explicitly so the LOD card can use it as a denominator without re-summing.
        internal const string k_TotalInstances = "Total Instances";
        internal const string k_VisibleInstances = "Visible Instances";
        internal const string k_DisabledRendererCulled = "Disabled Renderer Culled";
        internal const string k_LayerCulled = "Layer Culled";
        internal const string k_FrustumCulled = "Frustum Culled";
        internal const string k_OcclusionCulled = "Occlusion Culled";
        internal const string k_GpuOcclusionCulled = "GPU Occlusion Culled";
        internal const string k_LODGroupCulled = "LOD Group Culled";
        internal const string k_SmallMeshCulled = "Small Mesh Culled";
        internal const string k_OtherCulled = "Other Culled";

        // LOD Distribution
        internal const string k_LOD0 = "LOD 0";
        internal const string k_LOD1 = "LOD 1";
        internal const string k_LOD2 = "LOD 2";
        internal const string k_LOD3Plus = "LOD 3+";

        // Batch Stats — mirror of GRDProfilerCounters.
        internal const string k_BatchCount = "Batch Count";
        internal const string k_UniqueMaterials = "Unique Materials";
        internal const string k_UniqueMeshes = "Unique Meshes";
        internal const string k_SingleInstanceBatches = "Single-Instance Batches";

        // Exclusion Reasons — index matches GRDExclusionReason enum (index 0 = None = null).
        // The category arrays below partition this list; together they MUST cover every non-null
        // entry exactly once. GRDProfilerCounterNamesTests verifies this and checks parity with
        // the runtime's GRDExclusionReason enum / GetCategory mapping.
        internal static readonly string[] k_ExclusionReasonCounterNames =
        {
            null, // None
            "Excl: LOD Animate CrossFading",
            "Excl: Custom MaterialPropertyBlock",
            "Excl: Render Callback",
            "Excl: Non-Standard Sort Key",
            "Excl: Proxy Volume Probe",
            "Excl: Blend Probes With Anchor",
            "Excl: Enlighten Vertex Stream",
            "Excl: Missing DOTS Instancing",
            "Excl: Null Material",
            "Excl: Too Many Submeshes",
            "Excl: Missing Mesh",
            "Excl: GPU Driven Disabled",
            "Excl: Animation Visibility",
            "Excl: TextMesh Component",
            "Excl: Inactive Or Disabled",
        };

        // Reasons that count toward Coverage % (Excluded category). Render via SRP path.
        internal static readonly string[] k_ExcludedCategoryReasonNames =
        {
            "Excl: LOD Animate CrossFading",
            "Excl: Custom MaterialPropertyBlock",
            "Excl: Render Callback",
            "Excl: Non-Standard Sort Key",
            "Excl: Proxy Volume Probe",
            "Excl: Blend Probes With Anchor",
            "Excl: Enlighten Vertex Stream",
            "Excl: Missing DOTS Instancing",
            "Excl: Too Many Submeshes",
            "Excl: GPU Driven Disabled",
            "Excl: Animation Visibility",
            "Excl: TextMesh Component",
        };

        // Reasons in the NonRendering category — broken assets. Surfaced as warnings, not
        // in Coverage %.
        internal static readonly string[] k_NonRenderingCategoryReasonNames =
        {
            "Excl: Null Material",
            "Excl: Missing Mesh",
        };

        // Reasons in the Inactive category — disabled GameObjects/Renderers. Surfaced as
        // informational footer, not in Coverage %.
        internal static readonly string[] k_InactiveCategoryReasonNames =
        {
            "Excl: Inactive Or Disabled",
        };

        // Maps each wire-format counter name (k_ExclusionReasonCounterNames[i]) to the index-aligned entry in `byReasonIndex`.
        // Index 0(None) and any null entries are skipped.
        // Index alignment between the two arrays is guaranteed by GRDProfilerCounterNamesTests.
        static Dictionary<string, string> BuildReasonTextMap(string[] byReasonIndex)
        {
            var map = new Dictionary<string, string>();
            int n = System.Math.Min(k_ExclusionReasonCounterNames.Length, byReasonIndex.Length);
            for (int i = 1; i < n; i++)
            {
                string key = k_ExclusionReasonCounterNames[i];
                string text = byReasonIndex[i];
                if (key != null && text != null)
                    map[key] = text;
            }
            return map;
        }

        // Exclusion Reason Display Labels — Editor-only UI text; no runtime equivalent.
        // Text source of truth is UnityEditor.Rendering.GRDExclusionReasonText(shared with the Frame Debugger).
        // Edit copy there, not here.
        // Rebuilt from the localized source arrays on code reload so an editor-language change takes effect.
        [AutoStaticsCleanupOnCodeReload]
        internal static Dictionary<string, string> k_ExclusionReasonDisplayLabels = BuildReasonTextMap(GRDExclusionReasonText.k_Labels);

        [AutoStaticsCleanupOnCodeReload]
        internal static Dictionary<string, string> k_ExclusionReasonTooltips = BuildReasonTextMap(GRDExclusionReasonText.k_Tooltips);
    }
}
