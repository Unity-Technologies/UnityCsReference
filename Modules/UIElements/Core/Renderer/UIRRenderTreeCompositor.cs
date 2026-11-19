// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements.UIR
{
    partial class RenderTreeCompositor : IDisposable
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

            public DrawOperationType type
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Type;
            }

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

            public Vector2 pixelsPerPoint;

            PostProcessingMargins m_ReadMargins;
            PostProcessingMargins m_WriteMargins;
            bool m_HasMargins;

            // Visible area of a RenderTree op in its local points, margins of its passes included; null when unknown
            public Rect? visibleRect;
            public bool hasVisibleRect;

            // Read + write margins of every pass above a RenderTree op, in points
            public Vector4 chainMargins;
            public int chainPassCount;
            public bool hasChainMargins;

            public PostProcessingMargins readMargins { get { EnsureMargins(); return m_ReadMargins; } }
            public PostProcessingMargins writeMargins { get { EnsureMargins(); return m_WriteMargins; } }

            // User margin callbacks run once per op and frame, whoever asks first
            void EnsureMargins()
            {
                if (m_HasMargins)
                    return;
                FilterHelper.GetMargins(ref m_FilterPass, m_Filter, out m_ReadMargins, out m_WriteMargins);
                m_HasMargins = true;
            }

            public RenderTreeAtlas.AtlasBlock dstAtlasBlock = new() { page = -1 };

            public bool hasAtlasBlock
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => dstAtlasBlock.page >= 0;
            }

            // Only assigned on outputs (direct children of RenderTree draw operations): the TextureId that their render
            // tree's quad also holds.
            public TextureId dstTextureId;

            public VisualElement visualElement => m_VisualElement; // The visual element that performs the effect or that creates the nested render tree

            // The render tree that owns this draw operation (should match the nested render tree of the visual element
            // OR the render tree to draw)
            public RenderTree renderTree => m_RenderTree;

            // By reference: the struct is large, and read for every op in the planning loops
            public ref PostProcessingPass FilterPass
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref m_FilterPass;
            }

            // Flat slot index of this pass in the element's per-pass MaterialPropertyBlock list
            // (see FilterHelper.CountFilterChainPasses for the slot layout). Captured at op-build
            // time so the op ↔ block correspondence is structural rather than re-derived from the
            // live style at render time.
            public int FlatPassIndex => m_FlatPassIndex;
            public FilterFunction filter => m_Filter;

            // Identifies a specific filter application, all passes belonging to one filter share the same id
            public int filterGroupId;

            // Ops whose output an Effect op reads: the source pass first, then the requiredInput if any
            readonly List<DrawOperation> m_Inputs = new();
            public List<DrawOperation> inputs
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Inputs;
            }

            // The op bound to FilterPass.requiredInputTextureName, also listed in inputs
            public DrawOperation requiredInput;

            // Effect ops that read this op's output. Render tree quads (dstTextureId) are never counted here.
            readonly List<DrawOperation> m_Readers = new();
            public List<DrawOperation> readers
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Readers;
            }

            // Scratch for the atlas planning passes: the readers that haven't run yet in the replayed order
            public int planReaders;

            // Scratch for the scheduling: the ops this one waits for that haven't been scheduled yet
            public int pendingDependencies;

            // Scratch for k_TallestBlockFirst: the position before the sort, which keeps blocks of the same size in order
            public int sortIndex;

            // Scratch for the scheduling: the chunk that runs the op
            public int chunk;

            // The op renders the texture of a quad that its parent render tree draws
            public bool isOutput
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => parent != null && parent.type == DrawOperationType.RenderTree;
            }

            public void AddInput(DrawOperation input)
            {
                m_Inputs.Add(input);
                input.m_Readers.Add(this);
            }

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
                if (dstTextureId.IsValid())
                    m_RenderTree.quadTextureId = TextureId.invalid; // the atlas owns and releases the TextureId

                InitPointers();
                m_Type = DrawOperationType.Undefined;
                m_VisualElement = null;
                m_RenderTree = null;
                m_FilterPass = new PostProcessingPass();
                m_FlatPassIndex = -1;
                m_Filter = new FilterFunction();
                filterGroupId = 0;
                m_Inputs.Clear();
                requiredInput = null;
                m_Readers.Clear();
                planReaders = 0;
                pendingDependencies = 0;
                sortIndex = 0;
                chunk = 0;

                bounds = default;
                drawSourceBounds = default;
                drawSourceTexOffsets = default;
                pixelsPerPoint = default;
                m_HasMargins = false;
                visibleRect = null;
                hasVisibleRect = false;
                chainMargins = default;
                chainPassCount = 0;
                hasChainMargins = false;
                dstAtlasBlock = new RenderTreeAtlas.AtlasBlock { page = -1 };
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

        // The ops in the order they run, cut into batches, and the batches into chunks
        sealed class ExecutionPlan
        {
            public readonly List<DrawOperation> order = new(); // Every op but the root, children before parents

            // Start of each batch in order. The ops of a batch run together: an effect batch shares its material
            // and pass, and draws one instanced draw per target and input pages.
            public readonly List<int> batchStarts = new();

            // Start of each chunk in batchStarts. A chunk runs to its end before the next one starts.
            public readonly List<int> chunkStarts = new();

            public int GetBatchEnd(int batch) => batch + 1 < batchStarts.Count ? batchStarts[batch + 1] : order.Count;
        }

        readonly RenderTreeManager m_RenderTreeManager;
        readonly RenderTreeAtlas m_Atlas;
        readonly ExecutionPlan m_Plan = new();
        readonly BatchScheduler m_Scheduler;
        readonly AtlasPlanner m_AtlasPlanner;
        readonly EffectDrawer m_EffectDrawer;
        DrawOperation m_RootOperation;
        readonly ImplicitPool<DrawOperation> m_DrawOperationPool = new(() => new DrawOperation(), op => op.Reset(), 32, int.MaxValue);
        int m_NextFilterGroupId;

        // Snapshotted once per frame: the underlying graphics caps are a native read, and they change
        // when the device is re-created (graphics API switch, RenderDoc load), so they can't be cached longer.
        int m_MaxBlockWidth;
        int m_MaxBlockHeight;

        float m_PixelsPerPoint;
        Vector2 m_PanelPixelSize;
        Rect m_PanelRect;
        bool m_PanelIsFlat;
        bool m_HasScreenClip;

        // Absorbs the float error of point/pixel round trips so snapping doesn't add a spurious pixel
        const float k_PixelSnapTolerance = 0.01f;

        public RenderTreeCompositor(RenderTreeManager owner)
        {
            m_RenderTreeManager = owner;
            m_Atlas = new RenderTreeAtlas(owner.textureRegistry);
            m_Scheduler = new BatchScheduler(m_Plan);
            m_AtlasPlanner = new AtlasPlanner(m_Plan, m_Atlas, owner);
            m_EffectDrawer = new EffectDrawer(m_Plan, m_Atlas, owner);
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

            // Without a nested tree, nothing renders into the atlas
            if (rootRenderTree?.firstChild == null)
                return;

            m_MaxBlockWidth = RenderTreeAtlas.maxBlockWidth;
            m_MaxBlockHeight = RenderTreeAtlas.maxBlockHeight;
            var panelRoot = rootRenderTree.rootRenderData.owner;
            m_PixelsPerPoint = panelRoot.scaledPixelsPerPoint;
            m_PanelPixelSize = panelRoot.layout.size * m_PixelsPerPoint;
            m_PanelRect = panelRoot.rect;
            m_PanelIsFlat = m_RenderTreeManager.panel.isFlat;
            m_HasScreenClip = m_PanelIsFlat && !m_RenderTreeManager.drawInCameras;

            using (k_MarkerBuild.Auto())
            {
                BuildDrawOperationTree(rootRenderTree);
                UpdateDrawBounds_PostOrder(m_RootOperation);
                AddToExecutionOrder_PostOrder(m_RootOperation);
            }
            using (k_MarkerSchedule.Auto())
                m_Scheduler.Schedule(m_RenderTreeManager.filterTextureBudget);
            m_AtlasPlanner.Plan();
            using (k_MarkerAssignTextures.Auto())
                AssignTextureIds_DepthFirst(m_RootOperation);
        }

        static readonly ProfilerMarker k_MarkerBuild = new(ProfilerCategory.UIToolkit, "RenderTreeCompositor.Build");
        static readonly ProfilerMarker k_MarkerSchedule = new(ProfilerCategory.UIToolkit, "RenderTreeCompositor.Schedule");
        static readonly ProfilerMarker k_MarkerMeasure = new(ProfilerCategory.UIToolkit, "RenderTreeCompositor.MeasureAtlas");
        static readonly ProfilerMarker k_MarkerPack = new(ProfilerCategory.UIToolkit, "RenderTreeCompositor.PackAtlas");
        static readonly ProfilerMarker k_MarkerAssignTextures = new(ProfilerCategory.UIToolkit, "RenderTreeCompositor.AssignTextures");
        static readonly ProfilerMarker k_MarkerRender = new(ProfilerCategory.UIToolkit, "RenderTreeCompositor.Render");

        void AddToExecutionOrder_PostOrder(DrawOperation op)
        {
            for (var child = op.firstChild; child != null; child = child.nextSibling)
                AddToExecutionOrder_PostOrder(child);

            if (op.parent != null)
                m_Plan.order.Add(op);
        }

        // Packing mixed sizes in arbitrary order fragments the packer's free list and makes packing quadratic
        [NoAutoStaticsCleanup] // stateless sort comparator; no captured state
        static readonly Comparison<DrawOperation> k_TallestBlockFirst = (a, b) =>
        {
            int byHeight = b.dstAtlasBlock.height.CompareTo(a.dstAtlasBlock.height);
            if (byHeight != 0)
                return byHeight;
            int byWidth = b.dstAtlasBlock.width.CompareTo(a.dstAtlasBlock.width);
            return byWidth != 0 ? byWidth : a.sortIndex.CompareTo(b.sortIndex);
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool NeedsAtlasBlock(DrawOperation op) => op.dstAtlasBlock.width > 0 && op.dstAtlasBlock.height > 0;

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
            DrawOperation outermostPass = null;

            // Reverse iteration: outer pass first becomes innermost child in the operation tree.
            for (int i = computedFilter.Length - 1; i >= 0; i--)
            {
                var filterFunc = (FilterFunction)computedFilter[i];

                var filterDef = filterFunc.GetDefinition();

                if (filterDef == null || filterDef.passes == null)
                    continue;

                var passes = filterDef.passes;
                int filterGroupId = m_NextFilterGroupId++;
                flatBase -= passes.Length;

                for (int j = passes.Length - 1; j >= 0; j--)
                {
                    ref var pass = ref passes[j];
                    if (pass.material == null)
                        continue;

                    var operation = m_DrawOperationPool.Get();
                    operation.Init(ve, pass, flatBase + j, filterFunc);
                    operation.filterGroupId = filterGroupId;

                    parentOperation.AddChild(operation);
                    parentOperation = operation;
                    outermostPass ??= operation;
                }
            }

            var treeDrawOp = m_DrawOperationPool.Get();
            treeDrawOp.Init(renderTree);

            parentOperation.AddChild(treeDrawOp);
            LinkPassInputs(outermostPass);

            var childRenderTree = renderTree.firstChild;
            while (childRenderTree != null)
            {
                AddChildrenOperations_DepthFirst(treeDrawOp, childRenderTree);
                childRenderTree = childRenderTree.nextSibling;
            }
        }

        // Requires the whole chain down to the content op, so that "Source" can resolve
        static void LinkPassInputs(DrawOperation outermostPass)
        {
            for (var op = outermostPass; op != null && op.type == DrawOperationType.Effect; op = op.firstChild)
            {
                op.AddInput(op.firstChild);

                string name = op.FilterPass.requiredInputTextureName;
                if (string.IsNullOrEmpty(name))
                    continue;

                op.requiredInput = ResolveInputOp(op, name);
                if (op.requiredInput != null)
                    op.AddInput(op.requiredInput);
            }
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
                        op.pixelsPerPoint = child.pixelsPerPoint;
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
                            Rect childPointBounds = PixelsToPoints(child.bounds, child.pixelsPerPoint);
                            Rect childBounds = VisualElement.CalculateConservativeRect(ref childOpToParentOp, childPointBounds);
                            bounds = bounds == null ? childBounds : UIRUtility.Encapsulate(bounds.Value, childBounds);
                        }

                        child = child.nextSibling;
                    }

                    var owner = op.renderTree.rootRenderData.owner;
                    // Only nested render trees are drawn into a texture sized from these bounds: the root tree doesn't walk the panel
                    Rect veBB = op.parent != null
                        ? op.renderTree.GetRenderedBounds()
                        : BoxShadowBounds.EncapsulateOuterShadow(owner, owner.boundingBox);
                    if (UIRUtility.RectHasArea(veBB))
                        bounds = bounds == null ? veBB : UIRUtility.Encapsulate(bounds.Value, veBB);
                    else
                        Debug.Assert(bounds == null); // Children bounds should be zero

                    bool clipped = false;
                    if (bounds != null && op.parent != null && TryGetVisibleBounds(op, out Rect visible))
                    {
                        clipped = true;
                        bounds = UIRUtility.Intersect(bounds.Value, visible);
                    }

                    op.pixelsPerPoint = ComputePixelsPerPoint(op, bounds, clipped);
                    if (bounds != null)
                        bounds = ScaleRect(bounds.Value, op.pixelsPerPoint);

                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            if (bounds != null)
            {
                Rect r = bounds.Value;
                RectInt rectInt;

                DrawOperation parentOp = op.parent;

                // Last resort when the content alone exceeds the GPU limit: crop it so the texture
                // can always be created; a failed creation renders nothing at all (UUM-134044).
                if (parentOp != null && ExceedsAtlasLimits(SnapOutwardToPixels(r)))
                {
                    r.width = Mathf.Min(r.width, m_MaxBlockWidth - 1);
                    r.height = Mathf.Min(r.height, m_MaxBlockHeight - 1);
                    LogTextureLimitError(op, SnapOutwardToPixels(bounds.Value), "the content was cropped");
                }

                if (parentOp?.type == DrawOperationType.Effect)
                {
                    // Inflate for the parent read and write margins
                    var readMargins = parentOp.readMargins;
                    var writeMargins = parentOp.writeMargins;
                    var inflated = UIRUtility.InflateByMargins(UIRUtility.InflateByMargins(r, readMargins, op.pixelsPerPoint), writeMargins, op.pixelsPerPoint);
                    rectInt = SnapOutwardToPixels(inflated);

                    if (ExceedsAtlasLimits(rectInt))
                    {
                        // Rendering without margins (the effect reads edge-clamped) beats failing to
                        // create the texture and not rendering the element at all.
                        LogTextureLimitError(op, rectInt, "the filter margins were discarded");
                        writeMargins = new();
                        rectInt = SnapOutwardToPixels(r);
                    }

                    RectInt sourceBounds = SnapOutwardToPixels(UIRUtility.InflateByMargins(r, writeMargins, op.pixelsPerPoint));
                    op.parent.drawSourceBounds = sourceBounds;

                    // Whole texels, so the effect samples its source texel-centered
                    op.parent.drawSourceTexOffsets = new Vector4(
                        sourceBounds.xMin - rectInt.xMin,
                        sourceBounds.yMin - rectInt.yMin,
                        rectInt.xMax - sourceBounds.xMax,
                        rectInt.yMax - sourceBounds.yMax);
                }
                else
                {
                    rectInt = SnapOutwardToPixels(r);
                }

                op.bounds = rectInt;
            }
            else
                op.bounds = RectInt.zero;

            if (op.parent != null)
            {
                int width = op.bounds.width;
                int height = op.bounds.height;

                if (op.type == DrawOperationType.RenderTree)
                    op.renderTree.rasterPixelsPerPoint = Vector2.zero;

                RenderTreeAtlas.AtlasBlock block;
                if (RenderTreeAtlas.ReserveSize(width, height, m_MaxBlockWidth, m_MaxBlockHeight, out block))
                {
                    // A clamped reserve squeezes the content; rescale the read-margin offsets to match (UUM-147797)
                    if (op.parent.type == DrawOperationType.Effect && (block.width < width || block.height < height))
                    {
                        var texOffsets = op.parent.drawSourceTexOffsets;
                        float scaleX = block.width / (float)width;
                        float scaleY = block.height / (float)height;
                        op.parent.drawSourceTexOffsets = new Vector4(
                            texOffsets.x * scaleX,
                            texOffsets.y * scaleY,
                            texOffsets.z * scaleX,
                            texOffsets.w * scaleY);
                    }

                    op.dstAtlasBlock = block;

                    // The content op's block is the largest and the one a clamp squeezes
                    if (op.type == DrawOperationType.RenderTree && width > 0 && height > 0)
                        op.renderTree.rasterPixelsPerPoint = Vector2.Scale(op.pixelsPerPoint, new Vector2(block.width / (float)width, block.height / (float)height));

                    if (op.isOutput)
                        op.renderTree.quadRect = PixelsToPoints(op.bounds, op.pixelsPerPoint);
                }
            }
        }

        void EnsureChainMargins(DrawOperation op)
        {
            if (op.hasChainMargins)
                return;
            var sum = Vector4.zero;
            int passCount = 0;
            for (var effectOp = op.parent; effectOp != null && effectOp.type == DrawOperationType.Effect; effectOp = effectOp.parent)
            {
                PostProcessingMargins read = effectOp.readMargins, write = effectOp.writeMargins;
                sum += new Vector4(read.left + write.left, read.top + write.top, read.right + write.right, read.bottom + write.bottom);
                passCount++;
            }
            op.chainMargins = sum;
            op.chainPassCount = passCount;
            op.hasChainMargins = true;
        }

        static Rect InflateBy(Rect r, Vector4 margins)
        {
            return Rect.MinMaxRect(r.xMin - margins.x, r.yMin - margins.y, r.xMax + margins.z, r.yMax + margins.w);
        }

        // In screen points, so the slack stays a fixed amount of screen whatever the element's scale
        const float k_VisibleBoundsGrid = 32;

        // Only the part of the subtree that can be seen is rendered, so a zoomed or oversized element costs at most a screenful
        bool TryGetVisibleBounds(DrawOperation op, out Rect visible)
        {
            visible = default;
            if (!m_HasScreenClip)
                return false;

            Rect? visibleRect = GetVisibleRect(op);
            if (visibleRect == null)
                return false;

            Vector2 grid = new Vector2(k_VisibleBoundsGrid, k_VisibleBoundsGrid) / Vector2.Max(UIRUtility.GetAxisScale(in op.visualElement.worldTransformRef), Vector2.one);
            Rect v = visibleRect.Value;
            visible = Rect.MinMaxRect(
                Mathf.Floor(v.xMin / grid.x) * grid.x,
                Mathf.Floor(v.yMin / grid.y) * grid.y,
                Mathf.Ceil(v.xMax / grid.x) * grid.x,
                Mathf.Ceil(v.yMax / grid.y) * grid.y);
            return true;
        }

        // Top-down from the root: a nested tree sees what its parent tree's texture covers
        Rect? GetVisibleRect(DrawOperation treeOp)
        {
            if (treeOp.hasVisibleRect)
                return treeOp.visibleRect;
            treeOp.hasVisibleRect = true;

            var parentTreeOp = treeOp.parent;
            while (parentTreeOp != null && parentTreeOp.type == DrawOperationType.Effect)
                parentTreeOp = parentTreeOp.parent;
            var outer = treeOp.visualElement.renderData;
            if (parentTreeOp == null || outer == null)
                return null;

            Rect clip;
            if (parentTreeOp.parent == null)
            {
                // The inherited clip: the element's own overflow clip doesn't apply to its border and background
                var clipParent = outer.GetInheritanceParent(outer.parent);
                clip = clipParent != null ? clipParent.clippingRect : m_PanelRect;
            }
            else
            {
                // Overflow clips inside a filtered tree are left out, which only renders more than needed
                Rect? parentVisible = GetVisibleRect(parentTreeOp);
                if (parentVisible == null)
                    return null;
                clip = parentVisible.Value;
            }

            UIRUtility.ComputeMatrixRelativeToRenderTree(outer, out Matrix4x4 localToParentTree);
            if (!TryInvertFlat(localToParentTree, out Matrix4x4 parentTreeToLocal))
                return null;

            // Effects read and write past the visible edge (blur taps, shadow offsets)
            EnsureChainMargins(treeOp);
            Rect visible = InflateBy(VisualElement.CalculateConservativeRect(ref parentTreeToLocal, clip), treeOp.chainMargins);

            // A backdrop-filter in this tree blurs what lies just past that edge too
            treeOp.visibleRect = treeOp.renderTree.ExtendByBackdropReads(visible);
            return treeOp.visibleRect;
        }

        // Trees render flat, dropping depth, so a point maps back through the xy part only; a full 3D inverse would leave the element's plane
        static bool TryInvertFlat(in Matrix4x4 m, out Matrix4x4 inverse)
        {
            inverse = Matrix4x4.identity;
            float det = m.m00 * m.m11 - m.m01 * m.m10;
            if (Mathf.Abs(det) <= UIRUtility.k_Epsilon)
                return false;
            float invDet = 1 / det;
            inverse.m00 = m.m11 * invDet;
            inverse.m01 = -m.m01 * invDet;
            inverse.m10 = -m.m10 * invDet;
            inverse.m11 = m.m00 * invDet;
            inverse.m03 = -(inverse.m00 * m.m03 + inverse.m01 * m.m13);
            inverse.m13 = -(inverse.m10 * m.m03 + inverse.m11 * m.m13);
            return true;
        }

        // Rasterize at the owner's displayed density so a scaled element isn't magnified from a low-res texture
        Vector2 ComputePixelsPerPoint(DrawOperation op, Rect? pointBounds, bool clippedToVisible)
        {
            var panelDensity = new Vector2(m_PixelsPerPoint, m_PixelsPerPoint);

            // World space: the displayed density depends on the camera, so keep the panel's
            if (op.parent == null || !m_PanelIsFlat)
                return panelDensity;

            var ve = op.visualElement;
            Vector2 density = RenderTreeDensity.Compute(UIRUtility.GetAxisScale(in ve.worldTransformRef), ve.computedStyle.filter, m_PixelsPerPoint);

            if (pointBounds == null || m_MaxBlockWidth <= 0 || m_MaxBlockHeight <= 0)
                return density;

            // Without a visible clip, content wider than the panel would spend texels off-screen
            Rect content = pointBounds.Value;
            if (!clippedToVisible)
                density = RenderTreeDensity.LowerToFit(density, panelDensity, content.size, Vector2.Max(m_PanelPixelSize, Vector2.one));

            // Fit the atlas with every pass's margins; outward snapping can add 2 texels per pass
            EnsureChainMargins(op);
            Rect b = InflateBy(content, op.chainMargins);
            var atlasLimit = new Vector2(m_MaxBlockWidth - 1 - 2 * op.chainPassCount, m_MaxBlockHeight - 1 - 2 * op.chainPassCount);
            return RenderTreeDensity.LowerToFit(density, panelDensity, b.size, atlasLimit);
        }

        static Rect ScaleRect(Rect r, Vector2 scale)
        {
            return new Rect(r.x * scale.x, r.y * scale.y, r.width * scale.x, r.height * scale.y);
        }

        static Rect PixelsToPoints(RectInt r, Vector2 pixelsPerPoint)
        {
            if (pixelsPerPoint.x <= 0 || pixelsPerPoint.y <= 0)
                return Rect.zero;
            return ScaleRect(UIRUtility.CastToRect(r), new Vector2(1 / pixelsPerPoint.x, 1 / pixelsPerPoint.y));
        }

        internal static RectInt SnapOutwardToPixels(Rect r)
        {
            int xMin = Mathf.FloorToInt(r.xMin + k_PixelSnapTolerance);
            int yMin = Mathf.FloorToInt(r.yMin + k_PixelSnapTolerance);
            int xMax = SnapMaxOutward(xMin, r.xMax, r.width);
            int yMax = SnapMaxOutward(yMin, r.yMax, r.height);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        // A positive extent must keep at least one pixel, or a sub-tolerance sliver collapses one axis only
        static int SnapMaxOutward(int snappedMin, float max, float extent)
        {
            int minMax = extent > 0 ? snappedMin + 1 : snappedMin;
            return Mathf.Max(minMax, Mathf.CeilToInt(max - k_PixelSnapTolerance));
        }

        bool ExceedsAtlasLimits(RectInt rect)
        {
            // Non-positive limits mean the graphics caps aren't usable yet; cropping against them would
            // produce negative sizes, which is worse than letting the texture allocation report the failure.
            if (m_MaxBlockWidth <= 0 || m_MaxBlockHeight <= 0)
                return false;

            return rect.width > m_MaxBlockWidth || rect.height > m_MaxBlockHeight;
        }

        [NoAutoStaticsCleanup] // per-element log dedup (identity hash, keeps no element reference): the oversize condition persists across frames
        static readonly HashSet<int> s_TextureLimitErrorLogged = new();

        void LogTextureLimitError(DrawOperation op, RectInt rect, string action)
        {
            var ve = op.visualElement;
            if (!s_TextureLimitErrorLogged.Add(ve?.GetHashCode() ?? 0))
                return;

            Debug.LogError($"The filter on element '{ve}' requires a {rect.width}x{rect.height} texture, which exceeds the " +
                $"maximum supported size ({m_MaxBlockWidth}x{m_MaxBlockHeight}); {action}.");
        }

        // The quads whose textures share a page share its TextureId, which the atlas owns
        void AssignTextureIds_DepthFirst(DrawOperation op)
        {
            if (op.isOutput)
            {
                Debug.Assert(!op.renderTree.quadTextureId.IsValid());
                TextureId textureId = op.hasAtlasBlock ? m_Atlas.GetTextureId(op.dstAtlasBlock.page) : TextureId.invalid;
                op.dstTextureId = textureId;
                op.renderTree.quadTextureId = textureId;
                op.renderTree.quadUVRect = op.dstAtlasBlock.uvRect;
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
            m_EffectDrawer.BeginFrame();
            m_ContentGroupCount = 0;
            if (m_Plan.batchStarts.Count == 0)
                return;

            using (k_MarkerRender.Auto())
            {
                // The order must match the one AtlasPlanner replayed
                var executionOrder = m_Plan.order;
                var batchStarts = m_Plan.batchStarts;
                for (int batch = 0; batch < batchStarts.Count; ++batch)
                {
                    m_Atlas.BeginExecution(batch);
                    int start = batchStarts[batch];
                    int end = m_Plan.GetBatchEnd(batch);
                    if (executionOrder[start].type == DrawOperationType.RenderTree)
                        ExecuteTreeBatch(start, end);
                    else
                        m_EffectDrawer.DrawBatch(start, end);
                    m_Atlas.EndExecution(batch);
                }
            }
        }

        readonly List<DrawOperation> m_BatchOps = new();
        readonly List<UIRenderDevice.NestedChain> m_NestedChains = new();
        int m_ContentGroupCount;

        // The trees that draw into the same page render together
        void ExecuteTreeBatch(int start, int end)
        {
            var executionOrder = m_Plan.order;
            m_BatchOps.Clear();
            for (int i = start; i < end; ++i)
            {
                var op = executionOrder[i];
                if (BeginOperation(m_Atlas, op) && op.renderTree.firstCommand != null)
                    m_BatchOps.Add(op);
            }
            m_Atlas.PrepareRegions();

            while (m_BatchOps.Count > 0)
            {
                var block = m_BatchOps[0].dstAtlasBlock;
                m_NestedChains.Clear();
                int kept = 0;
                for (int i = 0; i < m_BatchOps.Count; ++i)
                {
                    var op = m_BatchOps[i];
                    if (op.dstAtlasBlock.page != block.page)
                    {
                        m_BatchOps[kept++] = op;
                        continue;
                    }

                    m_NestedChains.Add(new UIRenderDevice.NestedChain
                    {
                        head = op.renderTree.firstCommand,
                        viewport = op.dstAtlasBlock.rect,
                        bounds = PixelsToPoints(op.bounds, op.pixelsPerPoint),
                        pixelsPerPoint = GetTreePixelsPerPoint(op),
                    });
                }
                m_BatchOps.RemoveRange(kept, m_BatchOps.Count - kept);

                ++m_ContentGroupCount;
                m_RenderTreeManager.RenderNestedTreeGroup(m_NestedChains, m_Atlas.GetTexture(block));
            }
        }

        float GetTreePixelsPerPoint(DrawOperation op)
        {
            float pixelsPerPoint = Mathf.Sqrt(op.pixelsPerPoint.x * op.pixelsPerPoint.y);
            return pixelsPerPoint > 0 ? pixelsPerPoint : m_PixelsPerPoint;
        }

        static bool BeginOperation(RenderTreeAtlas atlas, DrawOperation op)
        {
            if (op.bounds.width <= 0)
                return false;
            Debug.Assert(op.bounds.height > 0); // Otherwise, the width should have been set to 0 as well.
            return op.hasAtlasBlock && atlas.BeginDraw(op.dstAtlasBlock, IsReadByCustomPass(op));
        }

        // Built-in passes are known to sample inside their source rect: only custom passes need the editor's pink margins
        static bool IsReadByCustomPass(DrawOperation op)
        {
            var readers = op.readers;
            for (int i = 0; i < readers.Count; ++i)
            {
                if (readers[i].filter.type == FilterFunctionType.Custom)
                    return true;
            }
            return false;
        }

        // For panels that skip rendering: visits the nested trees RenderNestedPasses would render, without rendering them.
        public void CallNestedImmediateModeElements()
        {
            foreach (var op in m_Plan.order)
            {
                if (op.type == DrawOperationType.RenderTree && op.bounds.width > 0)
                    UIRenderDevice.CallImmediateCommands(op.renderTree.firstCommand, PixelsToPoints(op.bounds, op.pixelsPerPoint));
            }
        }

        static DrawOperation ResolveInputOp(DrawOperation currentOp, string name)
        {
            // All passes of one filter share the same filterGroupId while the id matches
            // keeps us inside the current filter's pass chain.
            int groupId = currentOp.filterGroupId;

            if (name == FilterHelper.k_SourceInputName)
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

        void CleanupOperationTree()
        {
            m_DrawOperationPool.ReturnAll();
            m_RootOperation = null;

            m_Plan.order.Clear();
            m_Plan.batchStarts.Clear();
            m_Plan.chunkStarts.Clear();
            m_Atlas.ReleaseAll();
        }

        public static class Testing
        {
            [NoAutoStaticsCleanup] // test-only switch, reset by the tests that set it
            public static bool disableBatching;

            [NoAutoStaticsCleanup] // test-only switch, reset by the tests that set it
            public static bool disableTreeGrouping;

            // In pixels, and overrides PanelSettings.filterTextureBudget; 0 keeps the panel's budget
            [NoAutoStaticsCleanup] // test-only switch, reset by the tests that set it
            public static long chunkBudget;

            public static int CountChunks(RenderTreeCompositor compositor) => compositor.m_Plan.chunkStarts.Count;

            public static int CountEffectBatches(RenderTreeCompositor compositor)
            {
                int count = 0;
                foreach (var start in compositor.m_Plan.batchStarts)
                {
                    if (compositor.m_Plan.order[start].type == DrawOperationType.Effect)
                        ++count;
                }
                return count;
            }

            // The block that the element's content was drawn into in the last frame; page is -1 when there is none
            public static RenderTreeAtlas.AtlasBlock GetContentBlock(RenderTreeCompositor compositor, VisualElement element)
            {
                foreach (var op in compositor.m_Plan.order)
                {
                    if (op.type == DrawOperationType.RenderTree && op.visualElement == element)
                        return op.dstAtlasBlock;
                }
                return new RenderTreeAtlas.AtlasBlock { page = -1 };
            }

            public static RenderTexture GetTexture(RenderTreeCompositor compositor, in RenderTreeAtlas.AtlasBlock block) => compositor.m_Atlas.GetTexture(block);
            public static int CountPageClears(RenderTreeCompositor compositor) => RenderTreeAtlas.Testing.CountPageClears(compositor.m_Atlas);
            public static long PeakTransientArea(RenderTreeCompositor compositor) => RenderTreeAtlas.Testing.PeakTransientArea(compositor.m_Atlas);

            public static int CountRegionClears(RenderTreeCompositor compositor)
            {
                int count = 0;
                foreach (var op in compositor.m_Plan.order)
                {
                    if (op.hasAtlasBlock && op.dstAtlasBlock.needsClear)
                        ++count;
                }
                return count;
            }

            public static int CountLiveTextures(RenderTreeCompositor compositor) => RenderTreeAtlas.Testing.CountLiveTextures(compositor.m_Atlas);
            public static int CountLiveIntermediateTextures(RenderTreeCompositor compositor) => RenderTreeAtlas.Testing.CountLiveIntermediateTextures(compositor.m_Atlas);
            public static int CountPages(RenderTreeCompositor compositor) => RenderTreeAtlas.Testing.CountPages(compositor.m_Atlas);
            public static long CountPagePixels(RenderTreeCompositor compositor) => RenderTreeAtlas.Testing.CountPagePixels(compositor.m_Atlas);
            public static int CountEffectPasses(RenderTreeCompositor compositor) => CountOperations(compositor, DrawOperationType.Effect);
            public static int CountNestedTreeDraws(RenderTreeCompositor compositor) => CountOperations(compositor, DrawOperationType.RenderTree);

            static int CountOperations(RenderTreeCompositor compositor, DrawOperationType type)
            {
                int count = 0;
                foreach (var op in compositor.m_Plan.order)
                {
                    if (op.type == type)
                        ++count;
                }
                return count;
            }

            // In the last RenderNestedPasses: an instanced draw counts once, whatever its quad count
            public static int CountEffectDraws(RenderTreeCompositor compositor) => compositor.m_EffectDrawer.effectDrawCount;

            // In the last RenderNestedPasses: the trees rendered together count once
            public static int CountContentGroups(RenderTreeCompositor compositor) => compositor.m_ContentGroupCount;

            // In the last RenderNestedPasses
            public static int CountInstancedPasses(RenderTreeCompositor compositor) => compositor.m_EffectDrawer.instancedPassCount;

            // A draw must never sample the texture it renders into
            public static bool AnyOperationReadsItsOwnPage(RenderTreeCompositor compositor)
            {
                foreach (var op in compositor.m_Plan.order)
                {
                    if (op.hasAtlasBlock && compositor.m_AtlasPlanner.ReadsItsOwnPage(op))
                        return true;
                }
                return false;
            }
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
