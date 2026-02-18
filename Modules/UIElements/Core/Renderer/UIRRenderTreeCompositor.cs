// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
            int m_FilterPassIndex;
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
            public int FilterPassIndex => m_FilterPassIndex;
            public FilterFunction filter => m_Filter;

            public void Init(VisualElement ve, in PostProcessingPass filterPass, int filterPassIndex, FilterFunction filter)
            {
                m_Type = DrawOperationType.Effect;
                m_VisualElement = ve;
                m_FilterPass = filterPass;
                m_FilterPassIndex = filterPassIndex;
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
                m_Filter = new FilterFunction();

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

            // Extract the filters as List<> to avoid allocations when dealing with IEnumerable<>
            var filter = ve.resolvedStyle.filter as List<FilterFunction>;
            if (filter == null)
                throw new InvalidOperationException("Filter IEnumerable is not a List<FilterFunction>");

            // Add the filters and effects in reversed order since they are applied in a depth-first order
            for (int i = filter.Count - 1; i >= 0; i--)
            {
                var filterDef = filter[i].GetDefinition();
                if (filterDef?.passes == null)
                    continue;

                for (int j = filterDef.passes.Length - 1; j >= 0; j--)
                {
                    var filterPass = filterDef.passes[j];
                    if (filterPass.material == null)
                        continue;

                    var operation = m_DrawOperationPool.Get();
                    operation.Init(ve, filterPass, j, filter[i]);

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

            // When in force-gamma rendering, the last filter pass of the stack needs to output to an sRGB render
            // texture because when the parent render tree is rendered, the shader will expect a linear output
            // when sampling that texture and will perform a manual linear-to-gamma conversion.
            bool isLastFilterPass = op.parent?.type == DrawOperationType.RenderTree;

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

                        // Set Uniforms: Conversion to visual element coordinates (relative, pixels, points)
                        var oldRT = RenderTexture.active;

                        var dstTex = op.dstAtlasBlock.texture;
                        RenderTexture.active = dstTex;

                        var dstRect = op.dstAtlasBlock.rect;
                        var srcTexEntry = op.firstChild.dstAtlasBlock;
                        var srcUVRect = srcTexEntry.uvRect;

                        var mat = op.FilterPass.material;

                        if (forceGamma && isLastFilterPass)
                            mat.EnableKeyword("_UIE_OUTPUT_LINEAR");
                        else
                            mat.DisableKeyword("_UIE_OUTPUT_LINEAR");

                        mat.SetPass(op.FilterPass.passIndex);

                        m_Block.SetTexture("_MainTex", srcTexEntry.texture); // TODO: Use int instead of string

                        s_UVRects[0] = new Vector4(srcUVRect.x, srcUVRect.y, srcUVRect.width, srcUVRect.height);
                        m_Block.SetVectorArray("unity_uie_UVRect", s_UVRects);

#pragma warning disable 618

                        bool readsGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma || forceGamma;

                        if (op.FilterPass.applySettingsCallback != null)
                            op.FilterPass.applySettingsCallback(m_Block, new FilterPassContext
                            {
                                filterFunction = op.filter,
                                filterPassIndex = op.FilterPassIndex,
                                readsGamma = readsGamma,
                                writesGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma || forceGamma && isLastFilterPass,
                                scaledPixelsPerPoint = op.visualElement.scaledPixelsPerPoint,
                            });
#pragma warning restore 618
                        else
                            ApplyEffectParameters(op.FilterPass, op.filter, op.visualElement, readsGamma);

                        Utility.SetPropertyBlock(m_Block);

                        var projection = ProjectionUtils.Ortho(bounds.xMin, bounds.xMax, bounds.yMax, bounds.yMin, 0, 1);
                        GL.LoadProjectionMatrix(projection);
                        GL.modelview = Matrix4x4.identity;

                        var drawRect = op.drawSourceBounds;
                        var texOffsets = op.drawSourceTexOffsets;

                        float texWidth = srcTexEntry.texture.width;
                        float texHeight = srcTexEntry.texture.height;

                        var uvRect = new Rect(srcUVRect.x + texOffsets.x / texWidth,
                                              srcUVRect.y + texOffsets.y / texHeight,
                                              srcUVRect.width - (texOffsets.x + texOffsets.z) / texWidth,
                                              srcUVRect.height - (texOffsets.y + texOffsets.w) / texHeight);

                        GL.Viewport(new Rect(dstRect.xMin, dstRect.yMin, dstRect.width, dstRect.height));
                        GL.Begin(GL.QUADS);
                        GL.TexCoord2(uvRect.xMin, uvRect.yMin);
                        GL.MultiTexCoord2(1, 0.0f, 0.0f);
                        GL.Vertex3(drawRect.xMin, drawRect.yMax, 0.5f); // BL
                        GL.TexCoord2(uvRect.xMin, uvRect.yMax);
                        GL.MultiTexCoord2(1, 0.0f, 0.0f);
                        GL.Vertex3(drawRect.xMin, drawRect.yMin, 0.5f); // TL
                        GL.TexCoord2(uvRect.xMax, uvRect.yMax);
                        GL.MultiTexCoord2(1, 0.0f, 0.0f);
                        GL.Vertex3(drawRect.xMax, drawRect.yMin, 0.5f); // TR
                        GL.TexCoord2(uvRect.xMax, uvRect.yMin);
                        GL.MultiTexCoord2(1, 0.0f, 0.0f);
                        GL.Vertex3(drawRect.xMax, drawRect.yMax, 0.5f); // BR
                        GL.End();

                        RenderTexture.active = oldRT;
                    }
                    catch
                    {
                        // TODO: Blit instead and report the error
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

        void ApplyEffectParameters(PostProcessingPass effect, FilterFunction filter, VisualElement source, bool readsGamma)
        {
            if (effect.parameterBindings == null)
                return;

            var parameters = filter.parameters;
            var count = filter.parameterCount;
            for (int i = 0; i < effect.parameterBindings.Length; ++i)
            {
                if (i >= count)
                    break;
                var binding = effect.parameterBindings[i];
                var p = parameters[i];
                if (p.type == FilterParameterType.Float)
                    m_Block.SetFloat(binding.name, p.floatValue);
                else if (p.type == FilterParameterType.Color)
                    m_Block.SetVector(binding.name, readsGamma ? p.colorValue : p.colorValue.linear );
            }
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
