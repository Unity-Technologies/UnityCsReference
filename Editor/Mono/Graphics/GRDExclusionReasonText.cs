// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using L10n = UnityEditor.L10n;

namespace UnityEditor.Rendering
{
    internal static class GRDExclusionReasonText
    {
        internal static readonly string[] k_Labels =
        {
            null,                                                                    // None
            L10n.Tr("LODGroup Animate Cross-fading enabled", null),                        // LODAnimateCrossFading
            L10n.Tr("Unsupported MaterialPropertyBlock properties", null),                 // CustomMaterialPropertyBlock
            L10n.Tr("Renderer uses custom render callback", null),                         // RenderCallback
            L10n.Tr("Unsupported renderer sorting configuration", null),                   // NonStandardSortKey
            L10n.Tr("Light Probe Usage set to Use Proxy Volume", null),                    // ProxyVolumeProbe
            L10n.Tr("Blend Probes mode with custom Probe Anchor Override", null),          // BlendProbesWithAnchor
            L10n.Tr("Renderer uses Enlighten realtime GI vertex streams", null),           // EnlightenVertexStream
            L10n.Tr("Shader does not support DOTS_INSTANCING_ON", null),                   // MissingDOTSInstancing
            L10n.Tr("Null Material", null),                                                // NullMaterial
            L10n.Tr("Mesh has too many submeshes", null),                                  // TooManySubmeshes
            L10n.Tr("Missing Mesh", null),                                                 // MissingMesh
            L10n.Tr("GPU Resident Drawer disabled for this renderer/project", null),       // GPUDrivenDisabled
            L10n.Tr("Renderer visibility controlled by animation", null),                  // AnimationVisibility
            L10n.Tr("TextMesh renderer is not supported", null),                           // TextMeshComponent
            L10n.Tr("Inactive Or Disabled", null),                                         // InactiveOrDisabled
        };

        internal static readonly string[] k_Tooltips =
        {
            null,
            L10n.Tr("Renderers are excluded because their LODGroup has Animate Cross-fading enabled. Disable Animate Cross-fading on the affected LODGroup to allow GRD compatibility.", null),
            L10n.Tr("Renderers use MaterialPropertyBlock properties that are not supported by GRD. Remove unsupported per-renderer property overrides or use a supported material/instancing setup.", null),
            L10n.Tr("A MonoBehaviour on the renderer implements OnWillRenderObject, OnBecameVisible, or OnBecameInvisible. Remove these callbacks to restore GRD compatibility.", null),
            L10n.Tr("Renderer uses a non-default Sorting Layer or Sorting Order that GRD cannot batch. Reset Sorting Layer and Order to their defaults to restore GRD compatibility.", null),
            L10n.Tr("Light Probe Usage is set to Use Proxy Volume, which GRD does not support. Switch to Blend Probes or Off to restore GRD compatibility.", null),
            L10n.Tr("Blend Probes is active with a custom Probe Anchor Override set, which GRD cannot follow. Clear the Anchor Override to restore GRD compatibility.", null),
            L10n.Tr("Enlighten realtime GI is supplying vertex streams to this renderer, which GRD does not consume. Switch to Progressive Lightmapper or a different GI mode to restore GRD compatibility.", null),
            L10n.Tr("The renderer's shader does not include DOTS_INSTANCING_ON support, which GRD requires. Use a GRD-compatible shader (URP Lit, HDRP Lit, or one with the DOTS_INSTANCING_ON variant).", null),
            L10n.Tr("A material slot has no material assigned, or the assigned material has no shader. Assign a valid GRD-compatible material.", null),
            L10n.Tr("The renderer has more than 128 material slots, exceeding the GRD per-renderer limit. Reduce the number of sub-meshes or merge meshes to fit within the limit.", null),
            L10n.Tr("No mesh is assigned to this renderer. Assign a mesh to enable GRD.", null),
            L10n.Tr("GPU Resident Drawer is explicitly disabled for this renderer or project. Enable Allow GPU Driven Rendering in the renderer or project settings to restore GRD compatibility.", null),
            L10n.Tr("The animation system controls this renderer's visibility, which GRD does not support. Remove the animated visibility track or stop animating the renderer's enabled state.", null),
            L10n.Tr("This renderer uses the legacy TextMesh component, which GRD does not support. Replace it with TextMeshPro to restore GRD compatibility.", null),
            L10n.Tr("The GameObject or Renderer component is inactive, disabled, or rendering is forced off.", null),
        };

        internal static int ReasonCount => k_Labels.Length;

        internal static string GetLabel(int reason)
            => (reason > 0 && reason < k_Labels.Length) ? k_Labels[reason] : L10n.Tr("Unknown exclusion reason", null);

        internal static string GetTooltip(int reason)
            => (reason > 0 && reason < k_Tooltips.Length) ? k_Tooltips[reason] : string.Empty;
    }
}
