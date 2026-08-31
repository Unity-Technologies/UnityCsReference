// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    // Shared filter-pass rendering used by both the regular filter (RenderTreeCompositor) and backdrop-filter.
    // User applySettingsCallback code only runs in the update phase (InvokeFilterCallbacks), never
    // while a render target is bound; the render-time entry points consume the pre-populated
    // per-pass MaterialPropertyBlocks stored on RenderData.
    static class FilterHelper
    {
        static readonly int s_MainTexId = Shader.PropertyToID("_MainTex");

        // Shared by both filter paths: shaders sharing UnityUIEFilter.cginc read this uniform via
        // GetFilterUVRect. The compositor sets the source UV rect per pass; backdrop-filter passes
        // the full-texture rect. Kept here (the shared filter-rendering home) so a shader rename is
        // a single-point change.
        internal static readonly int s_UVRectId = Shader.PropertyToID("unity_uie_UVRect");

        // Reserved name for the texture that fed the first pass of the current filter.
        public const string k_SourceInputName = "Source";

        public struct InputBindingIds
        {
            public int texId;
            public int scaleOffsetId;
            public int uvRectId;
        }

        [NoAutoStaticsCleanup] // shader property ID cache; no user type references
        static readonly Dictionary<string, InputBindingIds> s_InputBindingIds = new();

        public static InputBindingIds GetInputBindingIds(string name)
        {
            if (!s_InputBindingIds.TryGetValue(name, out var ids))
            {
                ids = new InputBindingIds {
                    texId         = Shader.PropertyToID($"_{name}Tex"),
                    scaleOffsetId = Shader.PropertyToID($"_{name}Tex_ST"),
                    uvRectId      = Shader.PropertyToID($"_{name}Tex_UVRect"),
                };
                s_InputBindingIds[name] = ids;
            }
            return ids;
        }

        public static PostProcessingMargins GetReadMargins(PostProcessingPass pass, FilterFunction filterFunc)
        {
            return pass.computeRequiredReadMarginsCallback != null
                ? pass.computeRequiredReadMarginsCallback(filterFunc)
                : pass.readMargins;
        }

        public static PostProcessingMargins GetWriteMargins(PostProcessingPass pass, FilterFunction filterFunc)
        {
            return pass.computeRequiredWriteMarginsCallback != null
                ? pass.computeRequiredWriteMarginsCallback(filterFunc)
                : pass.writeMargins;
        }

        // Total input inflation (in points) needed so every pass of the chain can read valid data
        // when producing the nominal output rect. Summing per-pass read margins is conservative.
        // Deliberately uncapped: drop-shadow offsets are translations, so truncating them displaces
        // the shadow visibly; memory stays bounded by the clip/source clamps at the capture site.
        public static PostProcessingMargins ComputeChainReadMargins(ReadOnlySpan<UnmanagedFilterFunction> filters)
        {
            return SumChainReadMargins(filters, capturePassesOnly: false);
        }

        // Backdrop capture inflation: counts only expandsBackdropCapture passes. Kernel passes are
        // excluded on purpose — browsers clamp the backdrop at the element edge, so a blur kernel
        // must read clamped edges rather than pull surrounding content into the element rect.
        public static PostProcessingMargins ComputeBackdropCaptureReadMargins(ReadOnlySpan<UnmanagedFilterFunction> filters)
        {
            return SumChainReadMargins(filters, capturePassesOnly: true);
        }

        static PostProcessingMargins SumChainReadMargins(ReadOnlySpan<UnmanagedFilterFunction> filters, bool capturePassesOnly)
        {
            var total = new PostProcessingMargins();
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                var filterDef = filterFunc.GetDefinition();
                if (filterDef == null || filterDef.passes == null)
                    continue;

                for (int j = 0; j < filterDef.passes.Length; j++)
                {
                    // Keep in sync with ApplyFilterChain: a pass that won't run must not inflate the capture.
                    if (filterDef.passes[j].material == null)
                        continue;

                    if (capturePassesOnly && !filterDef.passes[j].expandsBackdropCapture)
                        continue;

                    var margins = GetReadMargins(filterDef.passes[j], filterFunc);
                    total.left += Mathf.Max(0, margins.left);
                    total.top += Mathf.Max(0, margins.top);
                    total.right += Mathf.Max(0, margins.right);
                    total.bottom += Mathf.Max(0, margins.bottom);
                }
            }
            return total;
        }

        // Renders one filter pass from source to target (material, property block, GL state, quad).
        // Optional rects default to the full source/target; usePixelMatrix=false lets the caller set projection.
        // The property block was pre-populated in the update phase; only _MainTex is layered on top.
        // The source texture of a pass is determined by the renderer, so a callback-written
        // _MainTex is deliberately overwritten.
        public static void ApplyFilterPass(
            RenderTexture source,
            RenderTexture target,
            PostProcessingPass pass,
            MaterialPropertyBlock propertyBlock,
            bool outputLinear,
            Rect? sourceUVRect = null,
            RectInt? drawBounds = null,
            Rect? viewport = null,
            bool usePixelMatrix = true)
        {
            var uvRect = sourceUVRect ?? new Rect(0, 0, 1, 1);
            var bounds = drawBounds ?? new RectInt(0, 0, target.width, target.height);
            var viewportRect = viewport ?? new Rect(0, 0, target.width, target.height);

            // Save GL state
            RenderTexture oldRT = RenderTexture.active;

            propertyBlock.SetTexture(s_MainTexId, source);

            RenderTexture.active = target;

            // Filter material is shared across panels: set the keyword every call so a stale value can't leak gamma state between them.
            if (outputLinear)
                pass.material.EnableKeyword(Shaders.k_OutputLinearKeyword);
            else
                pass.material.DisableKeyword(Shaders.k_OutputLinearKeyword);

            pass.material.SetPass(pass.passIndex);
            Utility.SetPropertyBlock(propertyBlock);

            GL.PushMatrix();
            if (usePixelMatrix)
            {
                GL.LoadPixelMatrix(0, target.width, target.height, 0);
            }
            // else: caller has already set up projection matrix

            // Only set viewport if explicitly provided (compositor needs it, backdrop-filter doesn't)
            if (viewport.HasValue)
                GL.Viewport(viewportRect);

            // Draw a full-screen quad with the specified UV rect
            GL.Begin(GL.QUADS);
            GL.TexCoord2(uvRect.xMin, uvRect.yMin); GL.MultiTexCoord2(1, 0.0f, 0.0f); GL.Vertex3(bounds.xMin, bounds.yMax, 0);
            GL.TexCoord2(uvRect.xMin, uvRect.yMax); GL.MultiTexCoord2(1, 0.0f, 0.0f); GL.Vertex3(bounds.xMin, bounds.yMin, 0);
            GL.TexCoord2(uvRect.xMax, uvRect.yMax); GL.MultiTexCoord2(1, 0.0f, 0.0f); GL.Vertex3(bounds.xMax, bounds.yMin, 0);
            GL.TexCoord2(uvRect.xMax, uvRect.yMin); GL.MultiTexCoord2(1, 0.0f, 0.0f); GL.Vertex3(bounds.xMax, bounds.yMax, 0);
            GL.End();

            GL.PopMatrix();

            RenderTexture.active = oldRT;
        }

        // Applies a filter chain, ping-ponging temporary textures. Returns source when there are no filters,
        // otherwise a temporary texture the caller must release. perPassBlocks holds one pre-populated block
        // per pass slot across the chain in forward order, including null-material passes; the caller sizes
        // it with RenderTreeManager.SizeFilterCallbackBlocks before rendering.
        public static RenderTexture ApplyFilterChain(
            RenderTexture source,
            ReadOnlySpan<UnmanagedFilterFunction> filters,
            RenderTextureReadWrite colorSpace,
            List<MaterialPropertyBlock> perPassBlocks,
            bool usePixelMatrix = true)
        {
            if (filters.Length == 0)
                return source;

            RenderTexture current = source;
            int flatBlockIndex = 0;

            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                var filterDef = filterFunc.GetDefinition();

                if (filterDef == null || filterDef.passes == null)
                    continue; // Occupies no block slots (see CountFilterChainPasses)

                bool retainsInput = false;
                for (int j = 0; j < filterDef.passes.Length; j++)
                {
                    if (!string.IsNullOrEmpty(filterDef.passes[j].requiredInputTextureName))
                    {
                        retainsInput = true;
                        break;
                    }
                }

                // Kept alive across this filter's passes when a composite-style pass re-reads it via
                // requiredInputTextureName (e.g. drop-shadow reads the unblurred input as "Source");
                // otherwise null so intermediate temps are released as soon as a pass consumes them.
                RenderTexture filterInput = retainsInput ? current : null;

                for (int j = 0; j < filterDef.passes.Length; j++)
                {
                    var pass = filterDef.passes[j];
                    if (pass.material == null)
                    {
                        flatBlockIndex++;
                        continue;
                    }

                    RenderTexture temp = RenderTexture.GetTemporary(
                        current.width,
                        current.height,
                        0,
                        current.format,
                        colorSpace
                    );
                    temp.filterMode = FilterMode.Bilinear;

                    if (!string.IsNullOrEmpty(pass.requiredInputTextureName))
                        BindChainInput(perPassBlocks[flatBlockIndex], pass.requiredInputTextureName, filterInput);

                    ApplyFilterPass(
                        source: current,
                        target: temp,
                        pass: pass,
                        propertyBlock: perPassBlocks[flatBlockIndex],
                        outputLinear: false,
                        usePixelMatrix: usePixelMatrix
                    );

                    if (current != source && current != filterInput)
                        RenderTexture.ReleaseTemporary(current);

                    current = temp;
                    flatBlockIndex++;
                }

                if (filterInput != null && filterInput != source && filterInput != current)
                    RenderTexture.ReleaseTemporary(filterInput);
            }

            return current;
        }

        // Counts the pass slots of a chain (including null-material passes), i.e. the size of the
        // per-pass MaterialPropertyBlock list.
        public static int CountFilterChainPasses(System.ReadOnlySpan<UnmanagedFilterFunction> filters)
        {
            int total = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var def = ((FilterFunction)filters[i]).GetDefinition();
                if (def != null && def.passes != null) // == operator: must match the fake-null handling of the other chain walks
                    total += def.passes.Length;
            }
            return total;
        }

        // Populates a property block with the "parameter binding" defaults for a single filter
        // pass: each binding maps a FilterFunction parameter to a named uniform on the pass's
        // material. Runs in the update phase before the user callback, which can override these
        // values by writing the same property names.
        public static void ApplyDefaultParameterBindings(MaterialPropertyBlock block, PostProcessingPass pass, FilterFunction filter, bool readsGamma)
        {
            if (pass.parameterBindings == null)
                return;

            var parameters = filter.parameters;
            int count = filter.parameterCount;
            for (int i = 0; i < pass.parameterBindings.Length; ++i)
            {
                if (i >= count)
                    break;
                var binding = pass.parameterBindings[i];
                var p = parameters[i];
                if (p.type == FilterParameterType.Float)
                    block.SetFloat(binding.name, p.floatValue);
                else if (p.type == FilterParameterType.Color)
                    block.SetVector(binding.name, readsGamma ? p.colorValue : p.colorValue.linear);
            }
        }

        // Update-phase entry point for the regular `filter` style. writesGamma follows the
        // compositor's rule: gamma output when the active color space is gamma, or on the last
        // pass when force-gamma rendering is on (the parent tree then samples the output expecting
        // a linear encoding; see RenderTreeCompositor.ExecuteDrawOperation_PostOrder).
        public static void InvokeFilterCallbacksForElement(RenderTreeManager renderTreeManager, RenderData owner)
        {
            VisualElement ve = owner.owner;
            if (ve == null)
                return;

            var filters = ve.computedStyle.filter;
            if (filters.Length == 0)
                return;

            int passCount = CountFilterChainPasses(filters);
            if (passCount == 0)
            {
                // A chain can degrade to zero passes without a style transition (e.g. destroyed definition asset).
                ReleaseFilterCallbackResources(renderTreeManager, owner);
                return;
            }

            var extraData = renderTreeManager.GetOrAddExtraData(owner);
            var blocks = extraData.filterCallbackPropertyBlocks ??= new List<MaterialPropertyBlock>(passCount);
            renderTreeManager.SizeFilterCallbackBlocks(blocks, passCount);

            bool activeIsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;
            bool forceGamma = renderTreeManager.forceGammaRendering;
            InvokeFilterCallbacks(
                filters,
                blocks,
                readsGamma: activeIsGamma || forceGamma,
                writesGamma: activeIsGamma,
                lastPassWritesGamma: activeIsGamma || forceGamma,
                ve.scaledPixelsPerPoint);
        }

        // Called when the element's `filter` chain becomes empty. Element removal returns the
        // blocks through FreeExtraData instead.
        public static void ReleaseFilterCallbackResources(RenderTreeManager renderTreeManager, RenderData owner)
        {
            if (!owner.hasExtraData)
                return;
            renderTreeManager.ReleaseFilterCallbackBlocks(renderTreeManager.GetExtraData(owner).filterCallbackPropertyBlocks);
        }

        // Flat slot index of the last pass that will actually render (material != null), or -1.
        // Null-material passes occupy a slot but never render, so they must not be considered
        // "last" — the compositor's _UIE_OUTPUT_LINEAR last-pass rule is based on rendered ops.
        public static int ComputeLastRenderedSlot(System.ReadOnlySpan<UnmanagedFilterFunction> filters)
        {
            int lastRenderedSlot = -1;
            int flat = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var def = ((FilterFunction)filters[i]).GetDefinition();
                if (def == null || def.passes == null)
                    continue;
                for (int j = 0; j < def.passes.Length; j++)
                {
                    if (def.passes[j].material != null)
                        lastRenderedSlot = flat;
                    flat++;
                }
            }
            return lastRenderedSlot;
        }

        // The only place user filter code executes; must run while no render target is bound.
        // Null-material passes never render, so their slot is skipped. writesGamma only differs per
        // pass for the compositor's force-gamma rule (last rendered slot gets lastPassWritesGamma).
        public static void InvokeFilterCallbacks(
            System.ReadOnlySpan<UnmanagedFilterFunction> filters,
            List<MaterialPropertyBlock> blocks,
            bool readsGamma,
            bool writesGamma,
            bool lastPassWritesGamma,
            float scaledPixelsPerPoint)
        {
            // The last-rendered slot only matters when its gamma differs from the other passes
            // (compositor force-gamma rule); when writesGamma == lastPassWritesGamma the distinction
            // is moot, so skip the extra chain walk (always the case for the backdrop-filter path).
            int lastRenderedSlot = writesGamma != lastPassWritesGamma ? ComputeLastRenderedSlot(filters) : -1;
            int flatBlockIndex = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                var filterDef = filterFunc.GetDefinition();

                if (filterDef == null || filterDef.passes == null)
                    continue;

                for (int j = 0; j < filterDef.passes.Length; j++)
                {
                    var pass = filterDef.passes[j];
                    if (pass.material == null)
                    {
                        flatBlockIndex++;
                        continue;
                    }

                    // A callback earlier in the walk can synchronously remove the element, which
                    // releases the blocks back to the pool and empties the list; stop here.
                    if (flatBlockIndex >= blocks.Count)
                        return;

                    var block = blocks[flatBlockIndex];
                    block.Clear();

                    ApplyDefaultParameterBindings(block, pass, filterFunc, readsGamma);

                    if (pass.applySettingsCallback != null)
                    {
                        try
                        {
                            bool isLastPass = flatBlockIndex == lastRenderedSlot;
                            pass.applySettingsCallback(block, new FilterPassContext
                            {
                                filterFunction = filterFunc,
                                filterPassIndex = j,
                                readsGamma = readsGamma,
                                writesGamma = isLastPass ? lastPassWritesGamma : writesGamma,
                                scaledPixelsPerPoint = scaledPixelsPerPoint
                            });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Exception thrown in filter settings callback for filter type '{filterFunc.type}' (pass {j}). " +
                                           $"The filter pass will be rendered with default settings. Exception: {e.Message}");
                            Debug.LogException(e);

                            // Discard whatever the callback wrote before throwing so the pass really does render with defaults.
                            if (flatBlockIndex < blocks.Count)
                            {
                                block.Clear();
                                ApplyDefaultParameterBindings(block, pass, filterFunc, readsGamma);
                            }
                        }
                    }

                    flatBlockIndex++;
                }
            }
        }

        [NoAutoStaticsCleanup]
        static bool s_UnsupportedChainInputWarned;

        static void BindChainInput(MaterialPropertyBlock propertyBlock, string name, RenderTexture filterInput)
        {
            // Only "Source" is resolvable here: the chain doesn't retain named pass outputs.
            if (name != k_SourceInputName)
            {
                if (!s_UnsupportedChainInputWarned)
                {
                    s_UnsupportedChainInputWarned = true;
                    Debug.LogWarning($"backdrop-filter: pass input '{name}' is not supported; only '{k_SourceInputName}' is available.");
                }
                return;
            }

            // All chain textures share the source's size, so an identity mapping is exact.
            var ids = GetInputBindingIds(name);
            propertyBlock.SetTexture(ids.texId, filterInput);
            propertyBlock.SetVector(ids.scaleOffsetId, new Vector4(1, 1, 0, 0));
            propertyBlock.SetVector(ids.uvRectId, new Vector4(0, 0, 1, 1));
        }
    }
}
