// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.UIElements.UIR
{
    // Shared filter-pass rendering used by both the regular filter (RenderTreeCompositor) and backdrop-filter.
    // User callbacks only run in the update phase (InvokeFilterCallbacks), never while a render target is
    // bound; the render-time entry points read the parameter values and blocks stored on the element's ExtraRenderData.
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

        // Hard cap, in points, for each side of a pass's read/write margins. Uncapped margins let a
        // filter inflate its render texture beyond the GPU limit (UUM-134044). 512 comfortably covers
        // the clamped built-in filters (blur margins peak at 3*k_MaxBlurSigma+1) and bounds custom ones.
        public const float k_MaxPostProcessingMargin = 512;

        static PostProcessingMargins CapMargins(PostProcessingMargins margins)
        {
            return new PostProcessingMargins {
                left = Mathf.Min(margins.left, k_MaxPostProcessingMargin),
                top = Mathf.Min(margins.top, k_MaxPostProcessingMargin),
                right = Mathf.Min(margins.right, k_MaxPostProcessingMargin),
                bottom = Mathf.Min(margins.bottom, k_MaxPostProcessingMargin),
            };
        }

        // Keyed on the clamped value so a transition animating past the range warns once, not every frame.
        [NoAutoStaticsCleanup] // identity hashes only, keeps no element reference
        static readonly HashSet<(int element, string property, int definition, int parameter, float clamped)> s_ClampWarningLogged = new();

        public static void WarnIfFilterParametersClamped(VisualElement ve, ReadOnlySpan<UnmanagedFilterFunction> filters, string propertyName)
        {
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                var def = filterFunc.GetDefinition();
                var declarations = def != null ? def.parameters : null;
                if (declarations == null)
                    continue;

                int count = Math.Min(filterFunc.parameterCount, declarations.Length);
                for (int j = 0; j < count; j++)
                {
                    var p = filterFunc.GetParameter(j);
                    if (p.type != FilterParameterType.Float || !declarations[j].hasFloatRange)
                        continue;

                    float clamped = declarations[j].ClampFloat(p.floatValue);
                    if (clamped == p.floatValue)
                        continue;

                    if (!s_ClampWarningLogged.Add((ve.GetHashCode(), propertyName, def.GetHashCode(), j, clamped)))
                        continue;

                    Debug.LogWarning($"The {propertyName} '{def.filterName}' on element '{ve}' has a parameter value of {p.floatValue}, " +
                        $"which is outside the supported range [{declarations[j].minFloatValue}, {declarations[j].maxFloatValue}]; {clamped} is used instead.");
                }
            }
        }

        public static PostProcessingMargins GetReadMargins(PostProcessingPass pass, FilterFunction filterFunc)
        {
            filterFunc = filterFunc.GetClampedToDefinition();
            var margins = pass.computeRequiredReadMarginsCallback != null
                ? pass.computeRequiredReadMarginsCallback(filterFunc)
                : pass.readMargins;
            return CapMargins(margins);
        }

        public static PostProcessingMargins GetWriteMargins(PostProcessingPass pass, FilterFunction filterFunc)
        {
            filterFunc = filterFunc.GetClampedToDefinition();
            var margins = pass.computeRequiredWriteMarginsCallback != null
                ? pass.computeRequiredWriteMarginsCallback(filterFunc)
                : pass.writeMargins;
            return CapMargins(margins);
        }

        // GetReadMargins and GetWriteMargins with a single clamp
        public static void GetMargins(ref PostProcessingPass pass, FilterFunction filterFunc, out PostProcessingMargins readMargins, out PostProcessingMargins writeMargins)
        {
            var clamped = filterFunc.GetClampedToDefinition();
            readMargins = CapMargins(pass.computeRequiredReadMarginsCallback != null
                ? pass.computeRequiredReadMarginsCallback(clamped)
                : pass.readMargins);
            writeMargins = CapMargins(pass.computeRequiredWriteMarginsCallback != null
                ? pass.computeRequiredWriteMarginsCallback(clamped)
                : pass.writeMargins);
        }

        // Total input inflation (in points) needed so every pass of the chain can read valid data
        // when producing the nominal output rect. Summing per-pass read margins is conservative.
        // Each pass contributes at most k_MaxPostProcessingMargin per side (see GetReadMargins);
        // drop-shadow offsets are range-limited to that same cap so translations never truncate.
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
            bool usePixelMatrix = true,
            bool clearTarget = false)
        {
            var uvRect = sourceUVRect ?? new Rect(0, 0, 1, 1);
            var bounds = drawBounds ?? new RectInt(0, 0, target.width, target.height);
            var viewportRect = viewport ?? new Rect(0, 0, target.width, target.height);

            // Save GL state
            RenderTexture oldRT = RenderTexture.active;

            propertyBlock.SetTexture(s_MainTexId, source);

            RenderTexture.active = target;

            if (clearTarget)
            {
                // Right after the bind, so tile-based GPUs fold it into the pass's load action
                GL.Clear(false, true, Color.clear);
            }

            // Filter material is shared across panels: set the keyword every call so a stale value can't leak gamma state between them.
            SetOutputLinearKeyword(pass.material, outputLinear);

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

        // Only shaders that test the keyword instead of calling UIEFilterOutputsLinear() declare it. On other shaders,
        // EnableKeyword(string) would add it to the material's invalid keywords, which are saved with a material asset.
        static void SetOutputLinearKeyword(Material material, bool outputLinear)
        {
            var shader = material.shader;
            if (shader == null)
                return;

            if (LocalKeyword.IsDeclared(shader, Shaders.k_OutputLinearKeyword))
                material.SetKeyword(new LocalKeyword(shader, Shaders.k_OutputLinearKeyword), outputLinear);
        }

        // Not thread-safe; UIR rendering is sequential.
        [NoAutoStaticsCleanup] // reused scratch, cleared before each draw
        static readonly FilterInstanceBatch s_InstanceBatch = new();
        public static FilterInstanceBatch instanceBatch => s_InstanceBatch;

        [NoAutoStaticsCleanup] // reused scratch, cleared before each draw
        static readonly MaterialPropertyBlock s_ChainPassBlock = new();

        [NoAutoStaticsCleanup] // constant
        static readonly Vector4[] k_FullUVRect = { new Vector4(0, 0, 1, 1) };

        // The per-element MaterialPropertyBlock of applySettingsCallback can't be merged into an instanced draw
        public static bool CanDrawInstanced(in PostProcessingPass pass)
        {
            return pass.applySettingsCallback == null && pass.material.enableInstancing && !Testing.disableInstancing;
        }

        // The setup of an instanced draw costs more than the plain draw of a single quad, for the same GPU work
        const int k_MinInstancedQuadCount = 2;

        public static bool ShouldDrawInstanced(in PostProcessingPass pass, int quadCount)
        {
            return (quadCount >= k_MinInstancedQuadCount || Testing.instanceSingleQuads) && CanDrawInstanced(pass);
        }

        // The block that InvokeFilterCallbacks prepared, which only a pass with a settings callback has. The caller checks
        // that the walk was for this chain.
        public static MaterialPropertyBlock GetPreparedBlock(in PostProcessingPass pass, List<MaterialPropertyBlock> blocks, int passSlot)
        {
            if (pass.applySettingsCallback == null || blocks == null || (uint)passSlot >= (uint)blocks.Count)
                return null;
            return blocks[passSlot];
        }

        public static class Testing
        {
            [NoAutoStaticsCleanup] // test-only switch, reset by the tests that set it
            public static bool disableInstancing;

            [NoAutoStaticsCleanup] // test-only switch, reset by the tests that set it
            public static bool instanceSingleQuads;
        }

        // Applies a filter chain, ping-ponging temporary textures. Returns source when there are no filters,
        // otherwise a temporary texture the caller must release. perPassBlocks and parameterValues are what
        // InvokeFilterCallbacks wrote; the passes draw with their defaults when they are for another chain.
        // The chain textures all have the size of the source, so the passes read the full UV rect.
        public static RenderTexture ApplyFilterChain(
            RenderTexture source,
            ReadOnlySpan<UnmanagedFilterFunction> filters,
            RenderTextureReadWrite colorSpace,
            List<MaterialPropertyBlock> perPassBlocks,
            FilterParameterValues parameterValues,
            FilterParameterStorage defaultsStorage,
            bool usePixelMatrix = true)
        {
            if (filters.Length == 0)
                return source;

            RenderTexture current = source;
            int flatBlockIndex = 0;
            if (parameterValues != null && parameterValues.passCount != CountFilterChainPasses(filters))
                parameterValues = null;
            bool readsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;

            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];
                var filterDef = filterFunc.GetDefinition();

                if (filterDef == null || filterDef.passes == null)
                    continue; // Occupies no block slots (see CountFilterChainPasses)

                // Pooled RTs keep another chain's output. Built-in passes overwrite every pixel; a custom pass may blend over it.
                bool clearsPassTargets = filterFunc.type == FilterFunctionType.Custom;

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

                    var values = parameterValues ?? GetDefaultParameterValues(defaultsStorage, pass, flatBlockIndex, filterFunc, readsGamma);
                    if (!ShouldDrawInstanced(pass, 1) || !DrawInstancedChainPass(current, temp, pass, filterInput, values, flatBlockIndex, clearsPassTargets))
                    {
                        var block = parameterValues != null ? GetPreparedBlock(pass, perPassBlocks, flatBlockIndex) : null;
                        if (block == null)
                        {
                            block = s_ChainPassBlock;
                            block.Clear();
                            values.ApplyTo(block, flatBlockIndex);
                        }

                        block.SetVectorArray(s_UVRectId, k_FullUVRect);
                        bool hasInput = !string.IsNullOrEmpty(pass.requiredInputTextureName);
                        if (hasInput)
                            BindChainInput(block, pass.requiredInputTextureName, filterInput);
                        FilterInstanceBatch.SetUniforms(block, GetChainQuad(hasInput));

                        ApplyFilterPass(
                            source: current,
                            target: temp,
                            pass: pass,
                            propertyBlock: block,
                            outputLinear: false,
                            usePixelMatrix: usePixelMatrix,
                            clearTarget: clearsPassTargets
                        );
                    }

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

        // A chain's textures all have the size of its source, and each pass reads all of its input: the quad covers the
        // whole target and source, and "Source" maps one to one.
        static FilterQuad GetChainQuad(bool hasInput) => new FilterQuad
        {
            destRect = FilterInstanceBatch.k_FullRect,
            sourceUVRect = FilterInstanceBatch.k_FullRect,
            sourceReadRect = FilterInstanceBatch.k_FullRect,
            inputScaleOffset = hasInput ? new Vector4(1, 1, 0, 0) : Vector4.zero,
            inputUVRect = hasInput ? FilterInstanceBatch.k_FullRect : FilterInstanceBatch.k_EmptyRect,
        };

        static bool DrawInstancedChainPass(RenderTexture source, RenderTexture target, PostProcessingPass pass, RenderTexture filterInput,
            FilterParameterValues values, int passSlot, bool clearTarget)
        {
            bool hasInput = !string.IsNullOrEmpty(pass.requiredInputTextureName);
            var batch = s_InstanceBatch;
            batch.Clear();
            batch.Add(GetChainQuad(hasInput), values, passSlot);

            bool bindsInput = hasInput && IsSupportedChainInput(pass.requiredInputTextureName);
            RenderTexture oldRT = RenderTexture.active;
            RenderTexture.active = target;
            try
            {
                // Right after the bind, so tile-based GPUs fold it into the pass's load action
                if (clearTarget)
                    GL.Clear(false, true, Color.clear);
                return batch.Draw(pass, target, source,
                    bindsInput ? GetInputBindingIds(pass.requiredInputTextureName).texId : 0,
                    bindsInput ? filterInput : null);
            }
            finally
            {
                RenderTexture.active = oldRT;
            }
        }

        // Counts the pass slots of a chain (including null-material passes), which the parameter values and blocks are indexed by
        public static int CountFilterChainPasses(System.ReadOnlySpan<UnmanagedFilterFunction> filters) => CountFilterChainPasses(filters, out _);

        // hasSettingsCallback: a pass has an applySettingsCallback, so the chain needs its blocks
        public static int CountFilterChainPasses(System.ReadOnlySpan<UnmanagedFilterFunction> filters, out bool hasSettingsCallback)
        {
            int total = 0;
            hasSettingsCallback = false;
            for (int i = 0; i < filters.Length; i++)
            {
                var def = ((FilterFunction)filters[i]).GetDefinition();
                if (def == null || def.passes == null) // == operator: must match the fake-null handling of the other chain walks
                    continue;

                var passes = def.passes;
                total += passes.Length;
                for (int j = 0; j < passes.Length; j++)
                    hasSettingsCallback |= passes[j].applySettingsCallback != null;
            }
            return total;
        }

        // Update-phase entry point for the regular `filter` style. writesGamma follows the
        // compositor's rule: gamma output when the active color space is gamma, or on the last
        // pass when force-gamma rendering is on (the parent tree then samples the output expecting
        // a linear encoding; see RenderTreeCompositor.EffectDrawer.OutputsLinear).
        public static void InvokeFilterCallbacksForElement(RenderTreeManager renderTreeManager, RenderData owner)
        {
            VisualElement ve = owner.owner;
            if (ve == null)
                return;

            var filters = ve.computedStyle.filter;
            if (filters.Length == 0)
                return;

            int passCount = CountFilterChainPasses(filters, out bool hasSettingsCallback);
            if (passCount == 0)
            {
                // A chain can degrade to zero passes without a style transition (e.g. destroyed definition asset).
                ReleaseFilterCallbackResources(renderTreeManager, owner);
                return;
            }

            ref var chain = ref renderTreeManager.GetOrAddExtraData(owner).filterParameters;
            chain.Prepare(renderTreeManager, passCount, hasSettingsCallback);

            bool activeIsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;
            bool forceGamma = renderTreeManager.forceGammaRendering;
            InvokeFilterCallbacks(
                filters,
                chain.blocks,
                readsGamma: activeIsGamma || forceGamma,
                writesGamma: activeIsGamma,
                lastPassWritesGamma: activeIsGamma || forceGamma,
                GetRenderTreePixelsPerPoint(ve.nestedRenderData?.renderTree, ve.scaledPixelsPerPoint),
                chain.values);
        }

        // Set by the compositor, which runs before the callbacks
        public static Vector2 GetRenderTreePixelsPerPoint(RenderTree renderTree, float panelPixelsPerPoint)
        {
            Vector2 density = renderTree?.rasterPixelsPerPoint ?? Vector2.zero;
            if (density.x > 0 && density.y > 0)
                return density;
            return new Vector2(panelPixelsPerPoint, panelPixelsPerPoint);
        }

        // Called when the element's `filter` chain becomes empty. Element removal releases the
        // parameters through FreeExtraData instead.
        public static void ReleaseFilterCallbackResources(RenderTreeManager renderTreeManager, RenderData owner)
        {
            if (owner.hasExtraData)
                renderTreeManager.GetExtraData(owner).filterParameters.Release(renderTreeManager);
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
        // Every pass writes its parameters into parameterValues, which the caller resets for the chain. Only a pass
        // with a settings callback fills its block: the values first, then the callback, which can override them.
        // blocks is empty or null for a chain without a settings callback.
        public static void InvokeFilterCallbacks(
            System.ReadOnlySpan<UnmanagedFilterFunction> filters,
            List<MaterialPropertyBlock> blocks,
            bool readsGamma,
            bool writesGamma,
            bool lastPassWritesGamma,
            Vector2 scaledPixelsPerPoint,
            FilterParameterValues parameterValues)
        {
            // The public context value stays a scalar (geometric mean); axis-bound parameters read
            // the per-axis vector, which differs per axis under a non-uniform backdrop content scale.
            float scalarScaledPixelsPerPoint = Mathf.Sqrt(scaledPixelsPerPoint.x * scaledPixelsPerPoint.y);
            // The last-rendered slot only matters when its gamma differs from the other passes
            // (compositor force-gamma rule); when writesGamma == lastPassWritesGamma the distinction
            // is moot, so skip the extra chain walk (always the case for the backdrop-filter path).
            int lastRenderedSlot = writesGamma != lastPassWritesGamma ? ComputeLastRenderedSlot(filters) : -1;
            int flatBlockIndex = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = ((FilterFunction)filters[i]).GetClampedToDefinition();
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
                    // releases its parameters (FilterChainParameters.Release); stop here.
                    if (flatBlockIndex >= parameterValues.passCount)
                        return;

                    bool isLastPass = flatBlockIndex == lastRenderedSlot;
                    var context = new FilterPassContext
                    {
                        filterFunction = filterFunc,
                        filterPassIndex = j,
                        readsGamma = readsGamma,
                        writesGamma = isLastPass ? lastPassWritesGamma : writesGamma,
                        scaledPixelsPerPoint = scalarScaledPixelsPerPoint,
                        perAxisScaledPixelsPerPoint = scaledPixelsPerPoint
                    };

                    WriteParameters(parameterValues, flatBlockIndex, pass, context);

                    if (pass.applySettingsCallback != null && blocks != null && flatBlockIndex < blocks.Count)
                        ApplySettings(blocks[flatBlockIndex], parameterValues, flatBlockIndex, pass, context);

                    flatBlockIndex++;
                }
            }
        }

        static void WriteParameters(FilterParameterValues values, int passSlot, in PostProcessingPass pass, FilterPassContext context)
        {
            var writer = new FilterParameterWriter(values);
            values.BeginPass(passSlot);
            WriteDefaultParameters(writer, pass, context.filterFunction, context.readsGamma);
            if (pass.writeParametersCallback == null)
                return;

            try
            {
                pass.writeParametersCallback(writer, context);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown in filter parameters callback for filter type '{context.filterFunction.type}' (pass {context.filterPassIndex}). " +
                               $"The filter pass will be rendered with default parameters. Exception: {e.Message}");
                Debug.LogException(e);
                values.BeginPass(passSlot);
                WriteDefaultParameters(writer, pass, context.filterFunction, context.readsGamma);
            }
        }

        static void ApplySettings(MaterialPropertyBlock block, FilterParameterValues values, int passSlot, in PostProcessingPass pass, FilterPassContext context)
        {
            block.Clear();
            values.ApplyTo(block, passSlot);
            try
            {
                pass.applySettingsCallback(block, context);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception thrown in filter settings callback for filter type '{context.filterFunction.type}' (pass {context.filterPassIndex}). " +
                               $"The filter pass will be rendered with default settings. Exception: {e.Message}");
                Debug.LogException(e);

                // Discard whatever the callback wrote before throwing
                block.Clear();
                values.ApplyTo(block, passSlot);
            }
        }

        // Each binding maps a filter parameter to a shader property of the pass
        static void WriteDefaultParameters(FilterParameterWriter writer, in PostProcessingPass pass, FilterFunction filter, bool readsGamma)
        {
            if (pass.parameterBindings == null)
                return;

            var parameters = filter.parameters;
            int count = filter.parameterCount;
            for (int i = 0; i < pass.parameterBindings.Length && i < count; ++i)
            {
                var p = parameters[i];
                int nameId = Shader.PropertyToID(pass.parameterBindings[i].name);
                if (p.type == FilterParameterType.Float)
                    writer.SetFloat(nameId, p.floatValue);
                else if (p.type == FilterParameterType.Color)
                    writer.SetColor(nameId, readsGamma ? p.colorValue : p.colorValue.linear);
            }
        }

        [NoAutoStaticsCleanup] // reused scratch, reset on every use
        static readonly FilterParameterValues s_DefaultParameterValues = new();

        // For a pass whose chain wasn't walked by the callbacks (e.g. it changed after the walk). Valid until the next call.
        public static FilterParameterValues GetDefaultParameterValues(FilterParameterStorage storage, in PostProcessingPass pass, int passSlot, FilterFunction filter, bool readsGamma)
        {
            s_DefaultParameterValues.Reset(storage, passSlot + 1);
            s_DefaultParameterValues.BeginPass(passSlot);
            var writer = new FilterParameterWriter(s_DefaultParameterValues);
            WriteDefaultParameters(writer, pass, filter.GetClampedToDefinition(), readsGamma);
            return s_DefaultParameterValues;
        }

        [NoAutoStaticsCleanup]
        static bool s_UnsupportedChainInputWarned;

        // Only "Source" is resolvable in a chain: it doesn't retain named pass outputs.
        static bool IsSupportedChainInput(string name)
        {
            if (name == k_SourceInputName)
                return true;

            if (!s_UnsupportedChainInputWarned)
            {
                s_UnsupportedChainInputWarned = true;
                Debug.LogWarning($"backdrop-filter: pass input '{name}' is not supported; only '{k_SourceInputName}' is available.");
            }
            return false;
        }

        static void BindChainInput(MaterialPropertyBlock propertyBlock, string name, RenderTexture filterInput)
        {
            if (!IsSupportedChainInput(name))
                return;

            // All chain textures share the source's size, so an identity mapping is exact.
            var ids = GetInputBindingIds(name);
            propertyBlock.SetTexture(ids.texId, filterInput);
            propertyBlock.SetVector(ids.scaleOffsetId, new Vector4(1, 1, 0, 0));
            propertyBlock.SetVector(ids.uvRectId, new Vector4(0, 0, 1, 1));
        }
    }
}
