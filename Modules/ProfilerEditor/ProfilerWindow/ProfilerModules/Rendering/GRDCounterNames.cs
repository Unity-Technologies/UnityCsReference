// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using L10n = UnityEditor.L10n;

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
    static class GRDCounterNames
    {
        internal const string k_CategoryName = "GPU Resident Drawer";

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

        // Exclusion Reason Display Labels — Editor-only UI text; no runtime equivalent.
        // Wire-format counter names (above) double as Profiler API identifiers and must stay
        // stable. These user-facing labels are decoupled so we can revise copy without breaking
        // the counter API. Keys are the wire-format names; values are wrapped in L10n.Tr.
        // Falls back to strip-Excl behavior for any key not present.
        internal static readonly Dictionary<string, string> k_ExclusionReasonDisplayLabels = new()
        {
            { "Excl: LOD Animate CrossFading", L10n.Tr("LODGroup Animate Cross-fading enabled") },
            { "Excl: Custom MaterialPropertyBlock", L10n.Tr("Unsupported MaterialPropertyBlock properties") },
            { "Excl: Render Callback", L10n.Tr("Renderer uses custom render callback") },
            { "Excl: Non-Standard Sort Key", L10n.Tr("Unsupported renderer sorting configuration") },
            { "Excl: Proxy Volume Probe", L10n.Tr("Light Probe Usage set to Use Proxy Volume") },
            { "Excl: Blend Probes With Anchor", L10n.Tr("Blend Probes mode with custom Probe Anchor Override") },
            { "Excl: Enlighten Vertex Stream", L10n.Tr("Renderer uses Enlighten realtime GI vertex streams") },
            { "Excl: Missing DOTS Instancing", L10n.Tr("Shader does not support DOTS_INSTANCING_ON") },
            { "Excl: Too Many Submeshes", L10n.Tr("Mesh has too many submeshes") },
            { "Excl: GPU Driven Disabled", L10n.Tr("GPU Resident Drawer disabled for this renderer/project") },
            { "Excl: Animation Visibility", L10n.Tr("Renderer visibility controlled by animation") },
            { "Excl: TextMesh Component", L10n.Tr("TextMesh renderer is not supported") },
        };

        // Exclusion Reason Tooltips — Editor-only UI text; no runtime equivalent.
        // Tooltip text is wrapped in L10n.Tr; keys are the wire-format counter names and stay untranslated.
        internal static readonly Dictionary<string, string> k_ExclusionReasonTooltips = new()
        {
            { "Excl: LOD Animate CrossFading", L10n.Tr("Renderers are excluded because their LODGroup has Animate Cross-fading enabled. Disable Animate Cross-fading on the affected LODGroup to allow GRD compatibility.") },
            { "Excl: Custom MaterialPropertyBlock", L10n.Tr("Renderers use MaterialPropertyBlock properties that are not supported by GRD. Remove unsupported per-renderer property overrides or use a supported material/instancing setup.") },
            { "Excl: Render Callback", L10n.Tr("A MonoBehaviour on the renderer implements OnWillRenderObject, OnBecameVisible, or OnBecameInvisible. Remove these callbacks to restore GRD compatibility.") },
            { "Excl: Non-Standard Sort Key", L10n.Tr("Renderer uses a non-default Sorting Layer or Sorting Order that GRD cannot batch. Reset Sorting Layer and Order to their defaults to restore GRD compatibility.") },
            { "Excl: Proxy Volume Probe", L10n.Tr("Light Probe Usage is set to Use Proxy Volume, which GRD does not support. Switch to Blend Probes or Off to restore GRD compatibility.") },
            { "Excl: Blend Probes With Anchor", L10n.Tr("Blend Probes is active with a custom Probe Anchor Override set, which GRD cannot follow. Clear the Anchor Override to restore GRD compatibility.") },
            { "Excl: Enlighten Vertex Stream", L10n.Tr("Enlighten realtime GI is supplying vertex streams to this renderer, which GRD does not consume. Switch to Progressive Lightmapper or a different GI mode to restore GRD compatibility.") },
            { "Excl: Missing DOTS Instancing", L10n.Tr("The renderer's shader does not include DOTS_INSTANCING_ON support, which GRD requires. Use a GRD-compatible shader (URP Lit, HDRP Lit, or one with the DOTS_INSTANCING_ON variant).") },
            { "Excl: Null Material", L10n.Tr("A material slot has no material assigned, or the assigned material has no shader. Assign a valid GRD-compatible material.") },
            { "Excl: Too Many Submeshes", L10n.Tr("The renderer has more than 128 material slots, exceeding the GRD per-renderer limit. Reduce the number of sub-meshes or merge meshes to fit within the limit.") },
            { "Excl: Missing Mesh", L10n.Tr("No mesh is assigned to this renderer. Assign a mesh to enable GRD.") },
            { "Excl: GPU Driven Disabled", L10n.Tr("GPU Resident Drawer is explicitly disabled for this renderer or project. Enable Allow GPU Driven Rendering in the renderer or project settings to restore GRD compatibility.") },
            { "Excl: Animation Visibility", L10n.Tr("The animation system controls this renderer's visibility, which GRD does not support. Remove the animated visibility track or stop animating the renderer's enabled state.") },
            { "Excl: TextMesh Component", L10n.Tr("This renderer uses the legacy TextMesh component, which GRD does not support. Replace it with TextMeshPro to restore GRD compatibility.") },
            { "Excl: Inactive Or Disabled", L10n.Tr("The GameObject or Renderer component is inactive, disabled, or rendering is forced off.") },
        };
    }
}
