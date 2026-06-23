// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    // Shared filter-pass rendering used by both the regular filter (RenderTreeCompositor) and backdrop-filter.
    static class FilterHelper
    {
        static readonly int s_MainTexId = Shader.PropertyToID("_MainTex");

        // Renders one filter pass from source to target (material, property block, GL state, quad).
        // Optional rects default to the full source/target; usePixelMatrix=false lets the caller set projection.
        public static void ApplyFilterPass(
            RenderTexture source,
            RenderTexture target,
            PostProcessingPass pass,
            FilterFunction filterFunc,
            int filterPassIndex,
            MaterialPropertyBlock propertyBlock,
            bool readsGamma,
            bool writesGamma,
            float pixelsPerPoint,
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

            // Caller owns the property block and clears it before calling (to pre-set its own properties first).
            propertyBlock.SetTexture(s_MainTexId, source);

            // Apply filter-specific parameters via callback
            if (pass.applySettingsCallback != null)
            {
                try
                {
                    pass.applySettingsCallback(propertyBlock, new FilterPassContext
                    {
                        filterFunction = filterFunc,
                        filterPassIndex = filterPassIndex,
                        readsGamma = readsGamma,
                        writesGamma = writesGamma,
                        scaledPixelsPerPoint = pixelsPerPoint
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception thrown in filter settings callback for filter type '{filterFunc.type}' (pass {filterPassIndex}). " +
                                   $"The filter pass will be rendered with default settings. Exception: {e.Message}");
                    Debug.LogException(e);
                }
            }

            RenderTexture.active = target;

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
        // otherwise a temporary texture the caller must release.
        public static RenderTexture ApplyFilterChain(
            RenderTexture source,
            System.ReadOnlySpan<UnmanagedFilterFunction> filters,
            float pixelsPerPoint,
            RenderTextureReadWrite colorSpace,
            bool readsGamma,
            bool writesGamma,
            MaterialPropertyBlock propertyBlock,
            bool usePixelMatrix = true,
            bool skipCustomFilters = false)
        {
            if (filters.Length == 0)
                return source;

            RenderTexture current = source;

            for (int i = 0; i < filters.Length; i++)
            {
                var filterFunc = (FilterFunction)filters[i];

                if (skipCustomFilters && filterFunc.type == FilterFunctionType.Custom)
                    continue;

                var filterDef = filterFunc.GetDefinition();

                if (filterDef == null || filterDef.passes == null)
                    continue;

                for (int j = 0; j < filterDef.passes.Length; j++)
                {
                    var pass = filterDef.passes[j];
                    if (pass.material == null)
                        continue;

                    RenderTexture temp = RenderTexture.GetTemporary(
                        current.width,
                        current.height,
                        0,
                        current.format,
                        colorSpace
                    );
                    temp.filterMode = FilterMode.Bilinear;

                    // Clear so the previous pass's callback properties (e.g. blur sigma) don't leak in.
                    propertyBlock.Clear();

                    ApplyFilterPass(
                        source: current,
                        target: temp,
                        pass: pass,
                        filterFunc: filterFunc,
                        filterPassIndex: j,
                        propertyBlock: propertyBlock,
                        readsGamma: readsGamma,
                        writesGamma: writesGamma,
                        pixelsPerPoint: pixelsPerPoint,
                        usePixelMatrix: usePixelMatrix
                    );

                    if (current != source)
                        RenderTexture.ReleaseTemporary(current);

                    current = temp;
                }
            }

            return current;
        }
    }
}
