// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Pool;

namespace UnityEngine.UIElements.UIR
{
    class RenderTreeCompositor : IDisposable
    {
        enum DrawOperationType
        {
            Undefined,
            RenderTree,
            Effect,
        }

        class DrawOperation
        {
            DrawOperationType m_Type;
            VisualElement m_VisualElement;
            RenderTree m_RenderTree;
            PostProcessingPass m_FilterPass;
            int m_FlatPassIndex;
            FilterFunction m_Filter;

            public DrawOperationType type => m_Type;

            // This rectangle represents, in render-tree space (pixels, not points), the clipping rect into which the
            // draw operation can modify pixels from the cleared state. It represents the area of the draw operation
            // that must be preserved and anything beyond it can be discarded, so will be clipped. There is not a 1:1
            // relationship between this rectangle and the inherited clipping rect. For example, depending on the
            // vertical/horizontal read distance of an effect, we might need to draw larger than it actually ends up
            // being displayed because the effect will read from a larger area.
            //
            // The clipping bounds are expanded by the read margins of the parent operation, if applicable. Also, a
            // safety margin is added to the clipping bounds to avoid reading pixels that are not written to by the
            // shader.
            public RectInt bounds;

            public RectInt drawSourceBounds;
            public Vector4 drawSourceTexOffsets;

            // Only assigned after the operation has been drawn. Only used by Effect draw operations.
            public RenderTreeAtlas.AtlasBlock dstAtlasBlock;

            // Only assigned on operations that are direct children of RenderTree draw operations.
            public TextureId dstTextureId;

            public VisualElement visualElement => m_VisualElement; // The visual element that performs the effect or that creates the nested render tree

            // The render tree that owns this draw operation (should match the nested render tree of the visual element
            // OR the render tree to draw)
            public RenderTree renderTree => m_RenderTree;

            public PostProcessingPass FilterPass => m_FilterPass;

            // Flat slot index of this pass in the element's per-pass MaterialPropertyBlock list
            // (see FilterHelper.CountFilterChainPasses for the slot layout). Captured at op-build
            // time so the op ↔ block correspondence is structural rather than re-derived from the
            // live style at render time.
            public int FlatPassIndex => m_FlatPassIndex;
            public FilterFunction filter => m_Filter;

            // Identifies a specific filter application, all passes belonging to one filter share the same id
            public int filterGroupId;

            public void Init(VisualElement ve, in PostProcessingPass filterPass, int flatPassIndex, FilterFunction filter)
            {
                m_Type = DrawOperationType.Effect;
                m_VisualElement = ve;
                m_FilterPass = filterPass;
                m_FlatPassIndex = flatPassIndex;
                m_Filter = filter;
                m_RenderTree = ve.nestedRenderData.renderTree;
                InitPointers();
            }

            public void Init(RenderTree renderTree)
            {
                m_Type = DrawOperationType.RenderTree;
                m_VisualElement = renderTree.rootRenderData.owner;
                m_RenderTree = renderTree;
                InitPointers();
            }

            void InitPointers()
            {
                parent = null;
                firstChild = null;
                lastChild = null;
                prevSibling = null;
                nextSibling = null;
            }

            public void Reset()
            {
                // Do not call InitPointers() here since the Reset() happens
                // while we are crawling the operation hierarchy.

                m_Type = DrawOperationType.Undefined;
                m_VisualElement = null;
                m_RenderTree = null;
                m_FilterPass = new PostProcessingPass();
                m_FlatPassIndex = -1;
                m_Filter = new FilterFunction();
                filterGroupId = 0;

                dstAtlasBlock = default;
                dstTextureId = TextureId.invalid;
            }

            // Graph data:
            public DrawOperation parent;
            public DrawOperation firstChild;
            public DrawOperation lastChild;
            public DrawOperation prevSibling;
            public DrawOperation nextSibling;

            public void AddChild(DrawOperation op)
            {
                Debug.Assert(op.prevSibling == null);
                op.parent = this;
                op.nextSibling = firstChild;
                if (firstChild != null)
                    firstChild.prevSibling = op;
                firstChild = op;
            }
        }

        readonly RenderTreeManager m_RenderTreeManager;
        DrawOperation m_RootOperation;
        List<RenderTexture> m_AllocatedTextures = new();
        MaterialPropertyBlock m_Block = new();
        ObjectPool<DrawOperation> m_DrawOperationPool = new(() => new DrawOperation());
        int m_NextFilterGroupId;
        [NoAutoStaticsCleanup] // per-material log dedup: a persistently broken filter logs once without hiding other filters' failures
        static readonly HashSet<EntityId> s_EffectDrawErrorLogged = new();

        public RenderTreeCompositor(RenderTreeManager owner)
        {
            m_RenderTreeManager = owner;
        }

        // Here we do the following:
        // * Compute the rendering bounds of every draw operation
        // * Analyze the render trees and determine which will land in the same atlases based on their dependencies and
        //   the effects that are being applied
        // * Determine their future location in the atlases
        // * Mark the quads for dirty repaint if relevant, to update the UVs and render texture handle. For this reason,
        //   this must run BEFORE the render trees are processed.
        public void Update(RenderTree rootRenderTree)
        {
            CleanupOperationTree();

            if (rootRenderTree == null)
                return;

            BuildDrawOperationTree(rootRenderTree);
            UpdateDrawBounds_PostOrder(m_RootOperation);
            AssignTextureIds_DepthFirst(m_RootOperation);
        }

        void BuildDrawOperationTree(RenderTree rootRenderTree)
        {
            m_NextFilterGroupId = 1; // 0 is reserved for "not a filter pass"

            m_RootOperation = m_DrawOperationPool.Get();
            m_RootOperation.Init(rootRenderTree);

            // The root render tree cannot have a post-processing stack. If an element of the root render tree contains
            // a post-processing stack, it will define a nested render tree which will add the draw operations.
            var childRenderTree = rootRenderTree.firstChild;
            while (childRenderTree != null)
            {
                AddChildrenOperations_DepthFirst(m_RootOperation, childRenderTree);
                childRenderTree = childRenderTree.nextSibling;
            }
        }

        void AddChildrenOperations_DepthFirst(DrawOperation parentOperation, RenderTree renderTree)
        {
            VisualElement ve = renderTree.rootRenderData.owner;
            var computedFilter = ve.computedStyle.filter;

            // Running flat slot base, walked backwards in step with the reverse iteration below so
            // each op stores its flat block index (filter i pass j = base of filter i + j).
            int flatBase = FilterHelper.CountFilterChainPasses(computedFilter);

            // Reverse iteration: outer pass first becomes innermost child in the operation tree.
            for (int i = computedFilter.Length - 1; i >= 0; i--)
            {
                var filterFunc = (FilterFunction)computedFilter[i];

                var filterDef = filterFunc.GetDefinition();

                if (filterDef == null || filterDef.passes == null)
                    continue;

                int filterGroupId = m_NextFilterGroupId++;
                flatBase -= filterDef.passes.Length;

                for (int j = filterDef.passes.Length - 1; j >= 0; j--)
                {
                    var pass = filterDef.passes[j];
                    if (pass.material == null)
                        continue;

                    var operation = m_DrawOperationPool.Get();
                    operation.Init(ve, pass, flatBase + j, filterFunc);
                    operation.filterGroupId = filterGroupId;

                    parentOperation.AddChild(operation);
                    parentOperation = operation;
                }
            }

            var treeDrawOp = m_DrawOperationPool.Get();
            treeDrawOp.Init(renderTree);

            parentOperation.AddChild(treeDrawOp);

            var childRenderTree = renderTree.firstChild;
            while (childRenderTree != null)
            {
                AddChildrenOperations_DepthFirst(treeDrawOp, childRenderTree);
                childRenderTree = childRenderTree.nextSibling;
            }
        }

        static PostProcessingMargins GetReadMargins(PostProcessingPass effect, FilterFunction func)
        {
            if (effect.computeRequiredReadMarginsCallback != null)
                return effect.computeRequiredReadMarginsCallback(func);
            return effect.readMargins;
        }

        static PostProcessingMargins GetWriteMargins(PostProcessingPass effect, FilterFunction func)
        {
            if (effect.computeRequiredWriteMarginsCallback != null)
                return effect.computeRequiredWriteMarginsCallback(func);
            return effect.writeMargins;
        }

        void UpdateDrawBounds_PostOrder(DrawOperation op)
        {
            Rect? bounds = null;

            switch (op.type)
            {
                case DrawOperationType.Effect:
                {
                    // An effect doesn't perform any scaling, so we can simply perform the union of the children,
                    // add the effect write margins and the offset of 1-pixel.
                    var child = op.firstChild;
                    if (child != null)
                    {
                        Debug.Assert(child.nextSibling == null); // Effect with multiple children are not supported yet.

                        UpdateDrawBounds_PostOrder(child);
                        if (UIRUtility.RectHasArea(op.drawSourceBounds))
                        {
                            bounds = UIRUtility.CastToRect(op.drawSourceBounds);
                        }
                    }

                    break;
                }
                case DrawOperationType.RenderTree:
                {
                    var child = op.firstChild;
                    while (child != null)
                    {
                        UpdateDrawBounds_PostOrder(child);
                        if (UIRUtility.RectHasArea(child.bounds))
                        {
                            UIRUtility.ComputeMatrixRelativeToRenderTree(child.visualElement.renderData, out Matrix4x4 childOpToParentOp);
                            Rect childBounds = VisualElement.CalculateConservativeRect(ref childOpToParentOp, UIRUtility.CastToRect(child.bounds));
                            bounds = bounds == null ? childBounds : UIRUtility.Encapsulate(bounds.Value, childBounds);
                        }

                        child = child.nextSibling;
                    }

                    Rect veBB = op.renderTree.rootRenderData.owner.boundingBox;
                    if (UIRUtility.RectHasArea(veBB))
                        bounds = bounds == null ? veBB : UIRUtility.Encapsulate(bounds.Value, veBB);
                    else
                        Debug.Assert(bounds == null); // Children bounds should be zero

                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            if (bounds != null)
            {
                Rect r = bounds.Value;
                RectInt rectInt;

                PostProcessingMargins readMargins = new();
                PostProcessingMargins writeMargins = new();

                DrawOperation parentOp = op.parent;
                if (parentOp?.type == DrawOperationType.Effect)
                {
                    // Inflate for the parent read and write margins
                    readMargins = GetReadMargins(parentOp.FilterPass, parentOp.filter);
                    writeMargins = GetWriteMargins(parentOp.FilterPass, parentOp.filter);
                    var inflated = UIRUtility.InflateByMargins(UIRUtility.InflateByMargins(r, readMargins), writeMargins);
                    rectInt = UIRUtility.CastToRectInt(inflated);

                    var sourceBounds = r;
                    sourceBounds = UIRUtility.InflateByMargins(sourceBounds, writeMargins);

                    op.parent.drawSourceBounds = UIRUtility.CastToRectInt(sourceBounds);

                    // Store the texel offsets in "pixels" since we do not know the texture size yet.
                    // They will be converted to UVs once rendered.
                    // Scale by DPI to convert from points to physical pixels.
                    float scale = op.renderTree.rootRenderData.owner.scaledPixelsPerPoint;
                    op.parent.drawSourceTexOffsets = new Vector4(
                        readMargins.left * scale,
                        readMargins.top * scale,
                        readMargins.right * scale,
                        readMargins.bottom * scale);
                }
                else
                {
                    rectInt = UIRUtility.CastToRectInt(r);
                }

                op.bounds = rectInt;
            }
            else
                op.bounds = RectInt.zero;

            if (op.parent != null)
            {
                int width = op.bounds.width;
                int height = op.bounds.height;

                // Request a texture size that accounts for the scaling (DPI) of the render tree
                float scale = op.renderTree.rootRenderData.owner.scaledPixelsPerPoint;
                width = Mathf.CeilToInt(width * scale);
                height = Mathf.CeilToInt(height * scale);

                RenderTreeAtlas.AtlasBlock block;
                if (RenderTreeAtlas.ReserveSize(width, height, out block))
                {
                    op.dstAtlasBlock = block;
                    if (op.parent.type == DrawOperationType.RenderTree)
                    {
                        op.renderTree.quadRect = op.bounds;
                        op.renderTree.quadUVRect = block.uvRect;
                    }
                }
            }
        }

        // In the future, we could reuse textures, but for now we simply allocate one TextureId for each renderTree.
        void AssignTextureIds_DepthFirst(DrawOperation op)
        {
            if (op.parent?.type == DrawOperationType.RenderTree)
            {
                Debug.Assert(!op.renderTree.quadTextureId.IsValid());
                TextureId textureId = m_RenderTreeManager.textureRegistry.AllocAndAcquireDynamic();
                op.dstTextureId = textureId;
                op.renderTree.quadTextureId = textureId;
                op.renderTree.quadIsGammaEncoded = m_RenderTreeManager.forceGammaRendering && op.type == DrawOperationType.RenderTree;
                op.parent.renderTree.OnRenderDataVisualsChanged(op.visualElement.renderData, false);
            }
            else
            {
                Debug.Assert(!op.dstTextureId.IsValid());
            }

            DrawOperation child = op.firstChild;
            while (child != null)
            {
                AssignTextureIds_DepthFirst(child);
                child = child.nextSibling;
            }
        }

        public void RenderNestedPasses()
        {
            // Clear the property block to avoid accidentally inheriting properties from previous context.
            m_Block.Clear();

            ExecuteDrawOperation_PostOrder(m_RootOperation);
        }


        static Vector4[] s_UVRects = new Vector4[1];


        void ExecuteDrawOperation_PostOrder(DrawOperation op)
        {
            var child = op.firstChild;
            while (child != null)
            {
                ExecuteDrawOperation_PostOrder(child);
                child = child.nextSibling;
            }

            if (op.parent == null) // Skip the root
                return;

            RectInt bounds = op.bounds;
            if (bounds.width <= 0)
                return;

            Debug.Assert(bounds.height > 0); // Otherwise, the width should have been set to 0 as well.

            bool forceGamma = m_RenderTreeManager.forceGammaRendering;

            // Under force-gamma: a filter's last pass outputs sRGB (parent samples linear + re-applies gamma); a plain
            // nested-tree quad stays UNorm so it blends in gamma like direct rendering (UI-5094).
            bool isLastFilterPass = op.type == DrawOperationType.Effect && op.parent?.type == DrawOperationType.RenderTree;

            if (RenderTreeAtlas.CreateTextureForAtlasBlock(ref op.dstAtlasBlock, forceGamma && !isLastFilterPass, out bool allocatedNewTexture))
            {
                if (allocatedNewTexture)
                    m_AllocatedTextures.Add(op.dstAtlasBlock.texture);
                if (op.dstTextureId.IsValid())
                    m_RenderTreeManager.textureRegistry.UpdateDynamic(op.dstTextureId, op.dstAtlasBlock.texture);
            }
            else
            {
                Debug.LogError($"Failed to create a texture for draw operation with bounds {bounds}.");
                return;
            }

            switch (op.type)
            {
                case DrawOperationType.Effect:
                {
                    try
                    {
                        Debug.Assert(op.firstChild != null, "An effect draw operation must have at least one child operation to render from.");

                        var dstTex = op.dstAtlasBlock.texture;
                        var dstRect = op.dstAtlasBlock.rect;
                        var srcTexEntry = op.firstChild.dstAtlasBlock;
                        var srcUVRect = srcTexEntry.uvRect;

                        bool readsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma || forceGamma;

                        // Calculate adjusted UV rect with texture offsets
                        var texOffsets = op.drawSourceTexOffsets;
                        float texWidth = srcTexEntry.texture.width;
                        float texHeight = srcTexEntry.texture.height;
                        var uvRect = new Rect(
                            srcUVRect.x + texOffsets.x / texWidth,
                            srcUVRect.y + texOffsets.w / texHeight,
                            srcUVRect.width - (texOffsets.x + texOffsets.z) / texWidth,
                            srcUVRect.height - (texOffsets.y + texOffsets.w) / texHeight);

                        // Look up the pre-built MaterialPropertyBlock for this pass, populated in
                        // the update phase by FilterHelper.InvokeFilterCallbacksForElement (default
                        // parameter bindings + user applySettingsCallback). The compositor layers
                        // its per-pass uniforms on top; nothing here may run user code.
                        var elementRenderData = op.visualElement.renderData;
                        MaterialPropertyBlock perPassBlock = null;
                        if (elementRenderData != null && elementRenderData.hasExtraData)
                        {
                            var perPassBlocks = m_RenderTreeManager.GetExtraData(elementRenderData).filterCallbackPropertyBlocks;
                            if (perPassBlocks != null && (uint)op.FlatPassIndex < (uint)perPassBlocks.Count)
                                perPassBlock = perPassBlocks[op.FlatPassIndex];
                        }
                        if (perPassBlock == null)
                        {
                            // The chain wasn't pre-populated this frame (e.g. it became non-empty
                            // after the update-phase walk). Fall back to the shared block with the
                            // default bindings so the pass still produces a valid output.
                            m_Block.Clear();
                            FilterHelper.ApplyDefaultParameterBindings(m_Block, op.FilterPass, op.filter, readsGamma);
                            perPassBlock = m_Block;
                        }

                        // Set up additional properties for compositor (UV rects, etc)
                        s_UVRects[0] = new Vector4(srcUVRect.x, srcUVRect.y, srcUVRect.width, srcUVRect.height);
                        perPassBlock.SetVectorArray(FilterHelper.s_UVRectId, s_UVRects);

                        // In force-gamma rendering, the last filter pass outputs linear because the parent render tree expects texture reads to output linear.
                        bool outputLinear = forceGamma && isLastFilterPass;

                        // Set up projection matrix for compositor rendering
                        var projection = ProjectionUtils.Ortho(bounds.xMin, bounds.xMax, bounds.yMax, bounds.yMin, 0, 1);
                        GL.LoadProjectionMatrix(projection);
                        GL.modelview = Matrix4x4.identity;

                        var drawRect = op.drawSourceBounds;
                        var viewportRect = new Rect(dstRect.xMin, dstRect.yMin, dstRect.width, dstRect.height);
                        var drawBounds = new RectInt(drawRect.xMin, drawRect.yMin, drawRect.width, drawRect.height);

                        if (!string.IsNullOrEmpty(op.FilterPass.requiredInputTextureName))
                            BindRequiredInput(perPassBlock, op, drawBounds, uvRect);

                        // Use shared filter helper (with custom projection already set).
                        FilterHelper.ApplyFilterPass(
                            source: srcTexEntry.texture,
                            target: dstTex,
                            pass: op.FilterPass,
                            propertyBlock: perPassBlock,
                            outputLinear: outputLinear,
                            sourceUVRect: uvRect,
                            drawBounds: drawBounds,
                            viewport: viewportRect,
                            usePixelMatrix: false  // We set up projection matrix ourselves
                        );
                    }
                    catch (Exception e)
                    {
                        // The pass failed to render (e.g. a misconfigured custom filter material);
                        // the destination texture is left unwritten.
                        var material = op.FilterPass.material;
                        if (s_EffectDrawErrorLogged.Add(material != null ? material.GetEntityId() : EntityId.None))
                            Debug.LogException(e, material);
                    }
                    break;
                }
                case DrawOperationType.RenderTree:
                {
                    m_RenderTreeManager.RenderSingleTree(op.renderTree, op.dstAtlasBlock.texture, op.dstAtlasBlock.rect, UIRUtility.CastToRect(bounds));
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        // Reserved name for the texture that fed the first pass of the current filter.
        const string k_SourceInputName = "Source";

        struct InputBindingIds
        {
            public int texId;
            public int scaleOffsetId;
            public int uvRectId;
        }

        static readonly Dictionary<string, InputBindingIds> s_InputBindingIds = new();

        static InputBindingIds GetInputBindingIds(string name)
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

        void BindRequiredInput(MaterialPropertyBlock block, DrawOperation currentOp, RectInt drawRect, Rect uvRect)
        {
            string name = currentOp.FilterPass.requiredInputTextureName;

            DrawOperation sourceOp = ResolveInputOp(currentOp, name);
            if (sourceOp == null)
            {
                Debug.LogWarning($"Filter pass requested input '{name}' but no matching upstream pass was found.");
                return;
            }

            if (sourceOp.dstAtlasBlock.texture == null)
                return;
            if (sourceOp.bounds.width <= 0 || sourceOp.bounds.height <= 0)
                return;

            BindMappedTexture(block, sourceOp, drawRect, uvRect, GetInputBindingIds(name));
        }

        static DrawOperation ResolveInputOp(DrawOperation currentOp, string name)
        {
            // All passes of one filter share the same filterGroupId while the id matches
            // keeps us inside the current filter's pass chain.
            int groupId = currentOp.filterGroupId;

            if (name == k_SourceInputName)
            {
                var op = currentOp;
                while (op.firstChild != null && op.firstChild.filterGroupId == groupId)
                    op = op.firstChild;
                return op.firstChild;
            }

            // Named output: search only among passes belonging to the current filter.
            {
                var op = currentOp.firstChild;
                while (op != null && op.filterGroupId == groupId)
                {
                    if (op.FilterPass.outputTextureName == name)
                        return op;
                    op = op.firstChild;
                }
            }
            return null;
        }

        static void BindMappedTexture(MaterialPropertyBlock block, DrawOperation sourceOp, RectInt drawRect, Rect uvRect, InputBindingIds ids)
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

            block.SetTexture(ids.texId, sourceOp.dstAtlasBlock.texture);
            block.SetVector(ids.scaleOffsetId, new Vector4(scaleX, scaleY, offsetX, offsetY));
            block.SetVector(ids.uvRectId, new Vector4(srcUV.xMin, srcUV.yMin, srcUV.xMax, srcUV.yMax));
        }

        void CleanupOperationTree()
        {
            if (m_RootOperation != null)
            {
                CleanupOperation_PostOrder(m_RootOperation);
                m_RootOperation = null;
            }

            foreach (var rt in m_AllocatedTextures)
                RenderTexture.ReleaseTemporary(rt);
            m_AllocatedTextures.Clear();
        }

        void CleanupOperation_PostOrder(DrawOperation op)
        {
            DrawOperation child = op.firstChild;
            while (child != null)
            {
                CleanupOperation_PostOrder(child);
                child = child.nextSibling;
            }

            if (op.dstTextureId.IsValid())
            {
                m_RenderTreeManager.textureRegistry.Release(op.dstTextureId);
                op.dstTextureId = TextureId.invalid;
                op.renderTree.quadTextureId = TextureId.invalid;
            }

            op.Reset();
            m_DrawOperationPool.Release(op);
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                CleanupOperationTree();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion
    }
}
