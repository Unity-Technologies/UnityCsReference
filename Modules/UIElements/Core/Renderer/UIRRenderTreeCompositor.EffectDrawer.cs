// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements.UIR
{
    partial class RenderTreeCompositor
    {
        sealed class EffectDrawer
        {
            readonly ExecutionPlan m_Plan;
            readonly RenderTreeAtlas m_Atlas;
            readonly RenderTreeManager m_RenderTreeManager;
            readonly MaterialPropertyBlock m_Block = new();
            readonly List<DrawOperation> m_BatchOps = new();
            readonly List<DrawOperation> m_DrawGroup = new();
            int m_EffectDrawCount;
            int m_InstancedPassCount;

            [NoAutoStaticsCleanup] // per-material log dedup: a persistently broken filter logs once without hiding other filters' failures
            static readonly HashSet<EntityId> s_EffectDrawErrorLogged = new();

            public EffectDrawer(ExecutionPlan plan, RenderTreeAtlas atlas, RenderTreeManager renderTreeManager)
            {
                m_Plan = plan;
                m_Atlas = atlas;
                m_RenderTreeManager = renderTreeManager;
            }

            // In the last frame: an instanced draw counts once, whatever its quad count
            public int effectDrawCount => m_EffectDrawCount;

            // In the last frame: the passes that drew instanced
            public int instancedPassCount => m_InstancedPassCount;

            public void BeginFrame()
            {
                // Clear the property block to avoid accidentally inheriting properties from previous context.
                m_Block.Clear();
                m_EffectDrawCount = 0;
                m_InstancedPassCount = 0;
            }

            // The ops that also write the same page and read the same pages draw together
            public void DrawBatch(int start, int end)
            {
                var executionOrder = m_Plan.order;
                m_BatchOps.Clear();
                for (int i = start; i < end; ++i)
                {
                    var op = executionOrder[i];
                    if (BeginOperation(m_Atlas, op))
                        m_BatchOps.Add(op);
                }
                m_Atlas.PrepareRegions();

                while (m_BatchOps.Count > 0)
                {
                    var first = m_BatchOps[0];
                    m_DrawGroup.Clear();
                    int kept = 0;
                    for (int i = 0; i < m_BatchOps.Count; ++i)
                    {
                        var op = m_BatchOps[i];
                        if (SharePages(first, op))
                            m_DrawGroup.Add(op);
                        else
                            m_BatchOps[kept++] = op;
                    }
                    m_BatchOps.RemoveRange(kept, m_BatchOps.Count - kept);
                    DrawEffects(m_DrawGroup);
                }
            }

            static int GetRequiredInputPage(DrawOperation op) => op.requiredInput != null && op.requiredInput.hasAtlasBlock ? op.requiredInput.dstAtlasBlock.page : -1;

            static bool SharePages(DrawOperation a, DrawOperation b)
            {
                return a.dstAtlasBlock.page == b.dstAtlasBlock.page &&
                    a.inputs[0].dstAtlasBlock.page == b.inputs[0].dstAtlasBlock.page &&
                    GetRequiredInputPage(a) == GetRequiredInputPage(b);
            }

            // In force-gamma rendering, the last filter pass outputs linear because the parent render tree expects texture reads to output linear
            bool OutputsLinear(DrawOperation op) => m_RenderTreeManager.forceGammaRendering && op.isOutput;

            // The ops share their material, pass, target page and input pages, and BeginOperation succeeded for each
            void DrawEffects(List<DrawOperation> ops)
            {
                var first = ops[0];
                Debug.Assert(first.inputs.Count > 0, "An effect draw operation must have at least one input to render from.");
                var dstTex = m_Atlas.GetTexture(first.dstAtlasBlock);
                var srcTex = m_Atlas.GetTexture(first.inputs[0].dstAtlasBlock);

                bool drawn = false;
                try
                {
                    drawn = DrawInstancedEffects(ops, srcTex, dstTex);
                }
                catch (Exception e)
                {
                    LogEffectError(first, e);
                    return;
                }

                if (drawn)
                {
                    ++m_EffectDrawCount;
                    m_InstancedPassCount += ops.Count;
                    return;
                }

                for (int i = 0; i < ops.Count; ++i)
                {
                    var op = ops[i];
                    try
                    {
                        DrawEffect(op, srcTex, dstTex);
                        ++m_EffectDrawCount;
                    }
                    catch (Exception e)
                    {
                        LogEffectError(op, e);
                    }
                }
            }

            static void LogEffectError(DrawOperation op, Exception e)
            {
                // The pass failed to render (e.g. a misconfigured custom filter material); the destination texture is left unwritten.
                var material = op.FilterPass.material;
                if (s_EffectDrawErrorLogged.Add(material != null ? material.GetEntityId() : EntityId.None))
                    Debug.LogException(e, material);
            }

            [NoAutoStaticsCleanup] // pre-allocated scratch array; infrastructure
            static Vector4[] s_UVRects = new Vector4[1];

            FilterParameterValues GetParameterValues(DrawOperation op)
            {
                var renderData = op.visualElement.renderData;
                if (renderData != null && renderData.hasExtraData)
                {
                    var values = m_RenderTreeManager.GetExtraData(renderData).filterParameters.values;
                    if (values != null && values.passCount > op.FlatPassIndex)
                        return values;
                }

                // The chain wasn't walked by the callbacks this frame (e.g. it became non-empty after the walk)
                bool readsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma || m_RenderTreeManager.forceGammaRendering;
                return FilterHelper.GetDefaultParameterValues(m_RenderTreeManager.filterParameterStorage, op.FilterPass, op.FlatPassIndex, op.filter, readsGamma);
            }

            // Null when the chain wasn't walked this frame, like in GetParameterValues: the pass then draws with its defaults
            MaterialPropertyBlock GetPreparedBlock(DrawOperation op)
            {
                if (op.FilterPass.applySettingsCallback == null)
                    return null;

                var renderData = op.visualElement.renderData;
                if (renderData == null || !renderData.hasExtraData)
                    return null;

                ref var chain = ref m_RenderTreeManager.GetExtraData(renderData).filterParameters;
                if (chain.values == null || chain.values.passCount <= op.FlatPassIndex)
                    return null;
                return FilterHelper.GetPreparedBlock(op.FilterPass, chain.blocks, op.FlatPassIndex);
            }

            static Vector4 ToMinMax(Rect rect) => new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);

            // The source content in UVs, without the margins that the pass doesn't read
            Rect ComputeSourceUVRect(DrawOperation op)
            {
                var source = op.inputs[0].dstAtlasBlock;
                var srcUVRect = source.uvRect;
                var pageSize = m_Atlas.GetPageSize(source);
                var texOffsets = op.drawSourceTexOffsets;
                float texWidth = pageSize.x;
                float texHeight = pageSize.y;
                return new Rect(
                    srcUVRect.x + texOffsets.x / texWidth,
                    srcUVRect.y + texOffsets.w / texHeight,
                    srcUVRect.width - (texOffsets.x + texOffsets.z) / texWidth,
                    srcUVRect.height - (texOffsets.y + texOffsets.w) / texHeight);
            }

            // The pass draws drawSourceBounds, normalized to the page (y down). A clamped block squeezes the bounds into it.
            Vector4 ComputeDestRect(DrawOperation op)
            {
                var block = op.dstAtlasBlock;
                var bounds = op.bounds;
                var draw = op.drawSourceBounds;
                var origin = block.contentTopLeft;
                var pageSize = m_Atlas.GetPageSize(block);
                float scaleX = block.width / (float)bounds.width / pageSize.x;
                float scaleY = block.height / (float)bounds.height / pageSize.y;
                float originX = origin.x / (float)pageSize.x;
                float originY = origin.y / (float)pageSize.y;
                return new Vector4(
                    originX + (draw.xMin - bounds.xMin) * scaleX,
                    originY + (draw.yMin - bounds.yMin) * scaleY,
                    originX + (draw.xMax - bounds.xMin) * scaleX,
                    originY + (draw.yMax - bounds.yMin) * scaleY);
            }

            // uvRect is ComputeSourceUVRect(op)
            FilterQuad ComputeQuad(DrawOperation op, Rect uvRect)
            {
                var quad = new FilterQuad
                {
                    destRect = ComputeDestRect(op),
                    sourceUVRect = ToMinMax(uvRect),
                    sourceReadRect = ToMinMax(op.inputs[0].dstAtlasBlock.uvRect),
                    inputScaleOffset = Vector4.zero,
                    inputUVRect = FilterInstanceBatch.k_EmptyRect,
                    outputLinear = OutputsLinear(op),
                };
                var input = op.requiredInput;
                if (input != null && input.hasAtlasBlock)
                    ComputeMappedInput(input, op.drawSourceBounds, uvRect, out quad.inputScaleOffset, out quad.inputUVRect);
                return quad;
            }

            // Returns false, without drawing, when the pass can't draw instanced
            bool DrawInstancedEffects(List<DrawOperation> ops, RenderTexture source, RenderTexture target)
            {
                var first = ops[0];
                if (!FilterHelper.ShouldDrawInstanced(first.FilterPass, ops.Count))
                    return false;

                Texture inputTexture = null;
                int inputTextureId = 0;
                if (first.requiredInput != null && first.requiredInput.hasAtlasBlock)
                {
                    inputTexture = m_Atlas.GetTexture(first.requiredInput.dstAtlasBlock);
                    inputTextureId = FilterHelper.GetInputBindingIds(first.FilterPass.requiredInputTextureName).texId;
                }

                var batch = FilterHelper.instanceBatch;
                batch.Clear();
                for (int i = 0; i < ops.Count; ++i)
                {
                    var op = ops[i];
                    var quad = ComputeQuad(op, ComputeSourceUVRect(op));
                    batch.Add(quad, GetParameterValues(op), op.FlatPassIndex);
                }

                var oldRT = RenderTexture.active;
                RenderTexture.active = target;
                try
                {
                    return batch.Draw(first.FilterPass, target, source, inputTextureId, inputTexture);
                }
                finally
                {
                    RenderTexture.active = oldRT;
                }
            }

            // Draws one op with its property block
            void DrawEffect(DrawOperation op, RenderTexture srcTex, RenderTexture dstTex)
            {
                RectInt bounds = op.bounds;
                bool outputLinear = OutputsLinear(op);

                var dstRect = op.dstAtlasBlock.rect;
                var uvRect = ComputeSourceUVRect(op);

                // Nothing here may run user code: a settings callback filled the block of its pass in the update phase
                var block = GetPreparedBlock(op);
                if (block == null)
                {
                    block = m_Block;
                    block.Clear();
                    GetParameterValues(op).ApplyTo(block, op.FlatPassIndex);
                }

                // unity_uie_UVRect must match the UV domain the quad's UV0 actually spans (uvRect,
                // margin-adjusted), not the raw atlas block (srcUVRect, margin-inflated)
                s_UVRects[0] = new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height);
                block.SetVectorArray(FilterHelper.s_UVRectId, s_UVRects);

                var quad = ComputeQuad(op, uvRect);
                FilterInstanceBatch.SetUniforms(block, quad);

                // Set up projection matrix for compositor rendering
                var projection = ProjectionUtils.Ortho(bounds.xMin, bounds.xMax, bounds.yMax, bounds.yMin, 0, 1);
                GL.LoadProjectionMatrix(projection);
                GL.modelview = Matrix4x4.identity;

                var drawRect = op.drawSourceBounds;
                var viewportRect = new Rect(dstRect.xMin, dstRect.yMin, dstRect.width, dstRect.height);
                var drawBounds = new RectInt(drawRect.xMin, drawRect.yMin, drawRect.width, drawRect.height);

                if (!string.IsNullOrEmpty(op.FilterPass.requiredInputTextureName))
                    BindRequiredInput(block, op, quad);

                // Use shared filter helper (with custom projection already set).
                FilterHelper.ApplyFilterPass(
                    source: srcTex,
                    target: dstTex,
                    pass: op.FilterPass,
                    propertyBlock: block,
                    outputLinear: outputLinear,
                    sourceUVRect: uvRect,
                    drawBounds: drawBounds,
                    viewport: viewportRect,
                    usePixelMatrix: false  // We set up projection matrix ourselves
                );
            }

            // The input texture, and the quad's input mapping under the input's own names, for shaders without the _UIE_ properties
            void BindRequiredInput(MaterialPropertyBlock block, DrawOperation currentOp, in FilterQuad quad)
            {
                string name = currentOp.FilterPass.requiredInputTextureName;

                DrawOperation sourceOp = currentOp.requiredInput;
                if (sourceOp == null)
                {
                    Debug.LogWarning($"Filter pass requested input '{name}' but no matching upstream pass was found.");
                    return;
                }

                var sourceTexture = m_Atlas.GetTexture(sourceOp.dstAtlasBlock);
                if (sourceTexture == null)
                    return;
                if (sourceOp.bounds.width <= 0 || sourceOp.bounds.height <= 0)
                    return;

                var ids = FilterHelper.GetInputBindingIds(name);
                block.SetTexture(ids.texId, sourceTexture);
                block.SetVector(ids.scaleOffsetId, quad.inputScaleOffset);
                block.SetVector(ids.uvRectId, quad.inputUVRect);
            }

            static void ComputeMappedInput(DrawOperation sourceOp, RectInt drawRect, Rect uvRect, out Vector4 scaleOffset, out Vector4 sourceUVRect)
            {
                var srcUV = sourceOp.dstAtlasBlock.uvRect;
                var srcBounds = sourceOp.bounds;

                // IN.uv interpolates uvRect across drawRect
                float Bx = srcUV.width / srcBounds.width;
                float By = srcUV.height / srcBounds.height;
                float Ax = drawRect.width / uvRect.width;
                float Ay = drawRect.height / uvRect.height;

                float scaleX = Bx * Ax;
                float scaleY = By * Ay;
                float offsetX = srcUV.xMin + Bx * (drawRect.xMin - srcBounds.xMin) - uvRect.xMin * scaleX;
                float offsetY = srcUV.yMax - By * (drawRect.yMin - srcBounds.yMin) - uvRect.yMax * scaleY;

                scaleOffset = new Vector4(scaleX, scaleY, offsetX, offsetY);
                sourceUVRect = new Vector4(srcUV.xMin, srcUV.yMin, srcUV.xMax, srcUV.yMax);
            }
        }
    }
}
