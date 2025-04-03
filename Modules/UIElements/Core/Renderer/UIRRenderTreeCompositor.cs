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
            PostProcessingPass m_Effect;
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
            public RenderTexture dstTexture;

            // Only assigned on operations that are direct children of RenderTree draw operations.
            public TextureId dstTextureId;

            public VisualElement visualElement => m_VisualElement; // The visual element that performs the effect or that creates the nested render tree

            // The render tree that owns this draw operation (should match the nested render tree of the visual element
            // OR the render tree to draw)
            public RenderTree renderTree => m_RenderTree;

            public PostProcessingPass effect => m_Effect;
            public FilterFunction filter => m_Filter;

            public void Init(VisualElement ve, in PostProcessingPass effect, FilterFunction filter)
            {
                m_Type = DrawOperationType.Effect;
                m_VisualElement = ve;
                m_Effect = effect;
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
                m_Effect = new PostProcessingPass();
                m_Filter = new FilterFunction();

                dstTexture = null;
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
        List<RenderTexture> m_AllocatedRenderTextures = new();
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

        static void UpdateDrawBounds_PostOrder(DrawOperation op)
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
                    readMargins = GetReadMargins(parentOp.effect, parentOp.filter);
                    writeMargins = GetWriteMargins(parentOp.effect, parentOp.filter);
                    var inflated = UIRUtility.InflateByMargins(UIRUtility.InflateByMargins(r, readMargins), writeMargins);
                    rectInt = UIRUtility.CastToRectInt(inflated);
                }
                else
                {
                    rectInt = UIRUtility.CastToRectInt(r);
                }

                if (op.parent?.type == DrawOperationType.Effect)
                {
                    var sourceBounds = r;
                    sourceBounds = UIRUtility.InflateByMargins(sourceBounds, writeMargins);

                    op.parent.drawSourceBounds = UIRUtility.CastToRectInt(sourceBounds);

                    var texOffsets = new Vector4(readMargins.left, readMargins.top, readMargins.right, readMargins.bottom);
                    if (rectInt.width > 0 && rectInt.height > 0)
                    {
                        float oneOverWidth = 1.0f / rectInt.width;
                        float oneOverHeight = 1.0f / rectInt.height;

                        texOffsets.x *= oneOverWidth;
                        texOffsets.y *= oneOverHeight;
                        texOffsets.z *= oneOverWidth;
                        texOffsets.w *= oneOverHeight;
                    }
                    else
                        texOffsets = Vector4.zero;

                    op.parent.drawSourceTexOffsets = texOffsets;
                }

                op.bounds = rectInt;
            }
            else
                op.bounds = RectInt.zero;

            if (op.parent?.type == DrawOperationType.RenderTree)
                op.renderTree.quadRect = op.bounds;
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

            // TODO: Format may change depending on color space and context (editor vs runtime)
            var graphicsFormat = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            var descriptor = new RenderTextureDescriptor(bounds.width, bounds.height, graphicsFormat, GraphicsFormat.D24_UNorm_S8_UInt);
            op.dstTexture = RenderTexture.GetTemporary(descriptor);
            m_AllocatedRenderTextures.Add(op.dstTexture);
            if (op.dstTextureId.IsValid())
                m_RenderTreeManager.textureRegistry.UpdateDynamic(op.dstTextureId, op.dstTexture);

            switch (op.type)
            {
                case DrawOperationType.Effect:
                {
                    try
                    {
                        // Set Uniforms: Conversion to visual element coordinates (relative, pixels, points)
                        var oldRT = RenderTexture.active;
                        RenderTexture.active = op.dstTexture;

                        // TODO: Replace the clear operation with a mesh that covers only the parts of the parent op
                        // read margins that the post-processing effect isn't writing to (assuming it is opaque, which
                        // is expected to be the case right now). Another reason to not clear is when we will introduce
                        // atlases, in which case we would need to be careful not to clear previously rendered parts of
                        // the atlas.
                        GL.Clear(false, true, Color.clear);

                        op.effect.material.SetPass(op.effect.passIndex);
                        m_Block.SetTexture("_MainTex", op.firstChild.dstTexture); // TODO: Use int instead of string

                        if (op.effect.prepareMaterialPropertyBlockCallback != null)
                            op.effect.prepareMaterialPropertyBlockCallback(m_Block, op.filter);
                        else
                            ApplyEffectParameters(op.effect, op.filter, op.visualElement);


                        Utility.SetPropertyBlock(m_Block);

                        var projection = ProjectionUtils.Ortho(bounds.xMin, bounds.xMax, bounds.yMax, bounds.yMin, 0, 1);
                        GL.LoadProjectionMatrix(projection);
                        GL.modelview = Matrix4x4.identity;

                        var drawRect = op.drawSourceBounds;
                        var texOffsets = op.drawSourceTexOffsets;

                        GL.Viewport(new Rect(0, 0, bounds.width, bounds.height));
                        GL.Begin(GL.QUADS);
                        GL.TexCoord2(texOffsets.x, texOffsets.w);
                        GL.Vertex3(drawRect.xMin, drawRect.yMax, 0.5f); // BL
                        GL.TexCoord2(texOffsets.x, 1 - texOffsets.y);
                        GL.Vertex3(drawRect.xMin, drawRect.yMin, 0.5f); // TL
                        GL.TexCoord2(1 - texOffsets.z, 1 - texOffsets.y);
                        GL.Vertex3(drawRect.xMax, drawRect.yMin, 0.5f); // TR
                        GL.TexCoord2(1 - texOffsets.z, texOffsets.w);
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
                    m_RenderTreeManager.RenderSingleTree(op.renderTree, op.dstTexture, bounds);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }

        void ApplyEffectParameters(PostProcessingPass effect, FilterFunction filter, VisualElement source)
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
                    m_Block.SetColor(binding.name, p.colorValue);
            }
        }

        void CleanupOperationTree()
        {
            if (m_RootOperation != null)
            {
                CleanupOperation_PostOrder(m_RootOperation);
                m_RootOperation = null;
            }

            for(int i = 0 ; i < m_AllocatedRenderTextures.Count ; ++i)
                RenderTexture.ReleaseTemporary(m_AllocatedRenderTextures[i]);
            m_AllocatedRenderTextures.Clear();
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
