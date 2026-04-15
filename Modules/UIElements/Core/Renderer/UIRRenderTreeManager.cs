// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using static UnityEngine.UIElements.UIR.RenderTree;

namespace UnityEngine.UIElements.UIR
{
    internal struct ChainBuilderStats
    {
        public uint elementsAdded, elementsRemoved;
        public uint recursiveClipUpdates, recursiveClipUpdatesExpanded, nonRecursiveClipUpdates;
        public uint recursiveTransformUpdates, recursiveTransformUpdatesExpanded;
        public uint recursiveOpacityUpdates, recursiveOpacityUpdatesExpanded;
        public uint opacityIdUpdates;
        public uint colorUpdates, colorUpdatesExpanded;
        public uint recursiveVisualUpdates, recursiveVisualUpdatesExpanded, nonRecursiveVisualUpdates;
        public uint dirtyProcessed;
        public uint nudgeTransformed, boneTransformed, skipTransformed, visualUpdateTransformed;
        public uint updatedMeshAllocations, newMeshAllocations;
        public uint groupTransformElementsChanged;
    }

    // We want to pool MeshWriteData instances, but unlike usual pooling, we don't explicitly return them to a pool. So instead,
    // here we'll simply keep track of them so they can be reused, but we can only reuse them when a Reset has been performed.
    class MeshWriteDataPool : ImplicitPool<MeshWriteData>
    {
        static readonly Func<MeshWriteData> k_CreateAction = () => new MeshWriteData();

        public MeshWriteDataPool()
            : base(k_CreateAction, null, 100, 1000) { }
    }

    partial class RenderTreeManager : IDisposable
    {
        RenderTreeCompositor m_Compositor;
        VisualChangesProcessor m_VisualChangesProcessor;
        LinkedPool<RenderChainCommand> m_CommandPool = new(() => new RenderChainCommand(), cmd => cmd.Reset());
        LinkedPool<ExtraRenderData> m_ExtraDataPool = new(() => new ExtraRenderData(), null);
        BasicNodePool<MeshHandle> m_MeshHandleNodePool = new();
        BasicNodePool<GraphicEntry> m_GraphicEntryPool = new();
        Dictionary<RenderData, ExtraRenderData> m_ExtraData = new();
        internal List<ElementInsertionData> m_InsertionList = new(1024); // Internal for testing purposes

        MeshGenerationDeferrer m_MeshGenerationDeferrer = new();
        Material m_DefaultMat;
        bool m_BlockDirtyRegistration;
        ChainBuilderStats m_Stats;
        uint m_StatsElementsAdded, m_StatsElementsRemoved;

        TextureRegistry m_TextureRegistry = TextureRegistry.instance;

        internal TextureRegistry textureRegistry => m_TextureRegistry;
        internal VisualChangesProcessor visualChangesProcessor => m_VisualChangesProcessor;

        public OpacityIdAccelerator opacityIdAccelerator { get; private set; }

        // TODO: Consider exposing these pools globally, not per panel
        UnityEngine.Pool.ObjectPool<RenderData> m_RenderDataPool = new(() => new RenderData(), null, null, null, false, 256, 1024);
        UnityEngine.Pool.ObjectPool<RenderTree> m_RenderTreePool = new(() => new RenderTree(), null, null, null, false, 8, 128);

        bool blockDirtyRegistration { get; set; }
        public TextureSlotCount textureSlotCount { get; set;} = TextureSlotCount.Eight;

        internal RenderData GetPooledRenderData()
        {
            var data = m_RenderDataPool.Get();
            data.Init();
            return data;
        }

        internal void ReturnPoolRenderData(RenderData data)
        {
            if (data != null)
            {
                data.Reset();
                m_RenderDataPool.Release(data);
            }
        }

        internal RenderTree GetPooledRenderTree(RenderTreeManager renderTreeManager, RenderData rootRenderData)
        {
            var tree = m_RenderTreePool.Get();
            tree.Init(renderTreeManager, rootRenderData);
            return tree;
        }

        internal void ReturnPoolRenderTree(RenderTree tree)
        {
            if (tree != null)
            {
                tree.Reset();
                m_RenderTreePool.Release(tree);
            }
        }

        static EntryPool s_SharedEntryPool = new(10000);

        // Profiling
        static readonly ProfilerMarker k_MarkerProcess = new(ProfilerCategory.UIToolkit, "RenderTreeManager.Process");
        static readonly ProfilerMarker k_MarkerSerialize = new(ProfilerCategory.UIToolkit, "RenderChain.Serialize");


        public RenderTreeManager(BaseVisualElementPanel panel)
        {
            this.panel = panel;
            atlas = panel.atlas;
            vectorImageManager = new VectorImageManager(atlas);

            // TODO: Share across all panels
            m_Compositor = new RenderTreeCompositor(this);
            tempMeshAllocator = new TempMeshAllocatorImpl();
            jobManager = new JobManager();
            opacityIdAccelerator = new OpacityIdAccelerator();
            meshGenerationNodeManager = new MeshGenerationNodeManager(entryRecorder);
            m_VisualChangesProcessor = new VisualChangesProcessor(this);

            ColorSpace activeColorSpace = QualitySettings.activeColorSpace;
            m_DefaultMat = Shaders.defaultMaterial;
            if (panel.contextType == ContextType.Player)
            {
                var runtimePanel = (BaseRuntimePanel)panel;
                drawInCameras = runtimePanel.drawsInCameras;

                if (!drawInCameras && activeColorSpace == ColorSpace.Linear)
                    forceGammaRendering = panel.panelRenderer.forceGammaRendering;
            }
            else // Editor
            {
                if (activeColorSpace == ColorSpace.Linear)
                    forceGammaRendering = true;
            }
            isFlat = panel.isFlat;
            device = new UIRenderDevice(panel.panelRenderer.vertexBudget, 0, isFlat, forceGammaRendering);

            Shaders.Acquire();

            shaderInfoAllocator = new UIRVEShaderInfoAllocator(forceGammaRendering ? ColorSpace.Gamma : activeColorSpace);
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
                Shaders.Release();

                ReverseDepthFirstDisposeRenderTrees(m_RootRenderTree);
                m_RootRenderTree = null;

                tempMeshAllocator.Dispose();
                tempMeshAllocator = null;

                jobManager.Dispose();
                jobManager = null;

                vectorImageManager?.Dispose();
                vectorImageManager = null;

                shaderInfoAllocator.Dispose();
                shaderInfoAllocator = null;

                device?.Dispose();
                device = null;

                opacityIdAccelerator?.Dispose();
                opacityIdAccelerator = null;

                m_VisualChangesProcessor?.Dispose();
                m_VisualChangesProcessor = null;

                m_MeshGenerationDeferrer?.Dispose();
                m_MeshGenerationDeferrer = null;

                meshGenerationNodeManager.Dispose();
                meshGenerationNodeManager = null;

                m_Compositor.Dispose();
                m_Compositor = null;

                m_RenderDataPool.Clear();
                m_RenderDataPool = null;

                foreach (var data in m_InsertionList)
                    data.element.insertionIndex = -1;
                m_InsertionList.Clear();

                atlas = null;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        static void ReverseDepthFirstDisposeRenderTrees(RenderTree renderTree)
        {
            var nextSibling = renderTree?.firstChild;
            while (nextSibling != null)
            {
                ReverseDepthFirstDisposeRenderTrees(nextSibling);
                nextSibling = nextSibling.nextSibling;
            }

            renderTree?.Dispose();
        }

        #endregion // Dispose Pattern

        // Note that this returns a copy of the stats, not a reference. This is typically used in tests to get a
        // snapshot of the stats at a given time.
        internal ChainBuilderStats stats => m_Stats;

        internal ref ChainBuilderStats statsByRef => ref m_Stats;

        RenderTree m_RootRenderTree;
        internal RenderTree rootRenderTree
        {
            get => m_RootRenderTree;
            set
            {
                Debug.Assert(m_RootRenderTree == null);
                m_RootRenderTree = value;
            }
        }

        void DepthFirstProcessChanges(RenderTree renderTree)
        {
            renderTree.ProcessChanges(ref m_Stats);

            var nextTree = renderTree.firstChild;
            while (nextTree != null)
            {
                DepthFirstProcessChanges(nextTree);
                nextTree = nextTree.nextSibling;
            }
        }

        public void ProcessChanges()
        {
            k_MarkerProcess.Begin();

            m_Stats = new ChainBuilderStats();
            m_Stats.elementsAdded += m_StatsElementsAdded;
            m_Stats.elementsRemoved += m_StatsElementsRemoved;
            m_StatsElementsAdded = m_StatsElementsRemoved = 0;

            // Process pending additions
            for (int i = 0; i < m_InsertionList.Count; ++i)
            {
                var data = m_InsertionList[i];
                if (!data.canceled)
                {
                    data.element.insertionIndex = -1;
                    ProcessChildAdded(data.element);
                }
            }
            m_InsertionList.Clear();

            m_BlockDirtyRegistration = true; // The repaint updater is not supposed to register new changes while processing sub-trees
            m_Compositor.Update(m_RootRenderTree);
            device.AdvanceFrame(); // Before making any changes to the buffers
            DepthFirstProcessChanges(m_RootRenderTree);
            m_BlockDirtyRegistration = false;

            meshGenerationNodeManager.ResetAll();
            tempMeshAllocator.Clear();
            meshWriteDataPool.ReturnAll();
            entryPool.ReturnAll();

            // Commit new requests for atlases if any
            atlas?.InvokeUpdateDynamicTextures(panel); // TODO: For a shared atlas + drawInCameras, postpone after all updates have occurred.
            vectorImageManager?.Commit();
            shaderInfoAllocator.IssuePendingStorageChanges();

            device.OnFrameRenderingBegin();

            // Nested render trees must be rendered before we draw the root tree, in order to avoid render texture
            // flushes to memory, and for compatibility with render passes. For now, we render them as part of the
            // update. In the future though, we should render them just before any rendering to the target of the root
            // render tree starts. This would allow to keep GPU memory usage to a minimum.
            RenderNestedTrees();

            if (drawInCameras)
                SerializeRootTreeCommands();

            k_MarkerProcess.End();
        }

        void SerializeRootTreeCommands()
        {
            Debug.Assert(drawInCameras);

            if (m_RootRenderTree?.firstCommand == null)
                return;

            var runtimePanel = (BaseRuntimePanel)panel;
            float ppu = runtimePanel.pixelsPerUnit;
            if (!float.IsFinite(ppu) || ppu < Mathf.Epsilon)
                return;

            k_MarkerSerialize.Begin();

            Exception immediateException = null;

            m_BlockDirtyRegistration = true;
            device.EvaluateChain(
                panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None,
                m_RootRenderTree.firstCommand,
                m_DefaultMat,
                vectorImageManager?.atlas,
                shaderInfoAllocator.atlas,
                null,
                panel.scaledPixelsPerPoint,
                true,
                textureSlotCount,
                false,
                ref immediateException);
            m_BlockDirtyRegistration = false;

            Debug.Assert(immediateException == null); // Not supported for cameras
            k_MarkerSerialize.End();
        }

        public void RenderRootTree()
        {
            Debug.Assert(!drawInCameras);

            PanelClearSettings clearSettings = panel.clearSettings;
            if (clearSettings.clearColor || clearSettings.clearDepthStencil)
            {
                // Case 1277149: Clear color must be pre-multiplied like when we render.
                Color clearColor = clearSettings.color;
                clearColor = clearColor.RGBMultiplied(clearColor.a);

                GL.Clear(clearSettings.clearDepthStencil, // Clearing may impact MVP
                    clearSettings.clearColor, clearColor, UIRUtility.k_ClearZ);
            }

            RenderSingleTree(m_RootRenderTree, null, RectInt.zero, Rect.zero);

        }

        void RenderNestedTrees()
        {
            m_Compositor.RenderNestedPasses();
        }

        public void RenderSingleTree(RenderTree renderTree, RenderTexture nestedTreeRT, RectInt nestedTreeViewport, Rect bounds)
        {
            // This function is not supposed to be used to draw the root render tree of a panel that draws in cameras.
            Debug.Assert(!drawInCameras || renderTree != m_RootRenderTree);

            if (renderTree.firstCommand == null)
                return;

            Exception immediateException = null;

            bool shouldResetRT = false;
            RenderTexture oldRT = null;
            bool prevInvertCulling = GL.invertCulling;
            if (prevInvertCulling)
                GL.invertCulling = false;

            float pixelsPerPoint = panel.scaledPixelsPerPoint;

            Rect scissor;
            if (renderTree == m_RootRenderTree)
            {
                Debug.Assert(nestedTreeRT == null);
                var viewport = panel.visualTree.layout;
                scissor = new Rect(0, 0, viewport.width, viewport.height);
                bounds = viewport;
            }
            else
            {
                Debug.Assert(nestedTreeRT != null);
                oldRT = RenderTexture.active;
                Camera.SetupCurrent(null);
                RenderTexture.active = nestedTreeRT;
                shouldResetRT = true;

                // Filters are rendered in an unscaled render target, so we force the pixelsPerPoint to 1.
                pixelsPerPoint = 1.0f;

                var viewport = UIRUtility.CastToRect(nestedTreeViewport);

                // Flip the scissor rectangle to match the UI Toolkit coordinate system
                scissor = viewport;
                scissor.y = scissor.height - scissor.yMax;

                GL.Viewport(viewport);
            }

            var projection = ProjectionUtils.Ortho(bounds.xMin, bounds.xMax, bounds.yMax, bounds.yMin, -0.001f, 1.001f);
            GL.LoadProjectionMatrix(projection);
            GL.modelview = Matrix4x4.identity;

            //TODO: Reactivate this guard check once InspectorWindow is fixed to stop adding VEs during OnGUI
            m_BlockDirtyRegistration = drawInCameras; // For now, we only enable it for drawInCameras
            device.EvaluateChain(
                panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None,
                renderTree.firstCommand,
                m_DefaultMat,
                vectorImageManager?.atlas,
                shaderInfoAllocator.atlas,
                scissor,
                pixelsPerPoint,
                false,
                textureSlotCount,
                (nestedTreeRT != null),
                ref immediateException);
            m_BlockDirtyRegistration = false;

            Utility.DisableScissor();

            if (prevInvertCulling)
                GL.invertCulling = true;

            if (shouldResetRT)
                RenderTexture.active = oldRT;

            if (immediateException != null)
            {
                Debug.Assert(!drawInCameras);

                if (GUIUtility.IsExitGUIException(immediateException))
                    throw immediateException;

                // Wrap the exception, this plays more nicely with the callstack logging.
                throw new ImmediateModeException(immediateException);
            }
        }

        internal struct ElementInsertionData // Internal for testing purposes
        {
            public VisualElement element;
            public bool canceled;
        }

        public void CancelInsertion(VisualElement ve)
        {
            int index = ve.insertionIndex;
            Debug.Assert(index >= 0 && index < m_InsertionList.Count);

            ElementInsertionData data = m_InsertionList[index];
            data.canceled = true;
            m_InsertionList[index] = data;
            ve.insertionIndex = -1;
        }

        #region UIElements event handling callbacks
        public void UIEOnChildAdded(VisualElement ve)
        {
            // Delay the actual element addition to make sure the styles are processed so we
            // can create the required RenderTrees.

            ve.insertionIndex = m_InsertionList.Count;
            m_InsertionList.Add(new ElementInsertionData() { element = ve, canceled = false });
        }

        void ProcessChildAdded(VisualElement ve)
        {
            VisualElement parent = ve.hierarchy.parent;
            int index = parent != null ? parent.hierarchy.IndexOf(ve) : 0;

            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be added to an active visual tree during generateVisualContent callback execution nor during visual tree rendering");
            if (parent != null && parent.renderData == null)
                return; // Ignore it until its parent gets ultimately added

            uint addedCount = RenderEvents.DepthFirstOnChildAdded(this, parent, ve, index);
            Debug.Assert(ve.renderData != null);
            Debug.Assert(ve.panel == this.panel);
            UIEOnClippingChanged(ve, true);
            UIEOnOpacityChanged(ve);
            UIEOnTransformOrSizeChanged(ve, true, true); // Make sure that transform-related flags are set
            UIEOnVisualsChanged(ve, true);
            ve.MarkRenderHintsClean();

            m_StatsElementsAdded += addedCount;
        }

        public void UIEOnChildrenReordered(VisualElement ve)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be moved under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

            int childrenCount = ve.hierarchy.childCount;
            for (int i = 0; i < childrenCount; i++)
                RenderEvents.DepthFirstOnElementRemoving(this, ve.hierarchy[i]);
            for (int i = 0; i < childrenCount; i++)
                // Add children using the "delayed" addition method, to make sure the render data is created in order
                UIEOnChildAdded(ve.hierarchy[i]);

            UIEOnClippingChanged(ve, true);
            UIEOnOpacityChanged(ve, true);
            UIEOnVisualsChanged(ve, true);
        }

        public void UIEOnChildRemoving(VisualElement ve)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be removed from an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

            m_StatsElementsRemoved += RenderEvents.DepthFirstOnElementRemoving(this, ve);


            Debug.Assert(ve.renderData == null);
        }

        public void UIEOnRenderHintsChanged(VisualElement ve)
        {
            if (ve.renderData != null)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("Render Hints cannot change under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                bool onlyDynamicColorIsDirty = (ve.renderHints & RenderHints.DirtyAll) == RenderHints.DirtyDynamicColor;
                if (onlyDynamicColorIsDirty)
                {
                    UIEOnVisualsChanged(ve, false);
                }
                else
                {
                    UIEOnChildRemoving(ve);
                    UIEOnChildAdded(ve);
                }

                ve.MarkRenderHintsClean();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RegisterDirty(VisualElement ve, RenderDataDirtyTypes dirtyTypes, RenderDataDirtyTypeClasses dirtyClasses)
        {
            var renderData = ve.renderData;
            if (renderData != null)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change their render data under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                renderData.renderTree.dirtyTracker.RegisterDirty(renderData, dirtyTypes, dirtyClasses);
                if (ve.nestedRenderData != null)
                    ve.nestedRenderData.renderTree.dirtyTracker.RegisterDirty(ve.nestedRenderData, dirtyTypes, dirtyClasses);
            }
        }

        public void UIEOnClippingChanged(VisualElement ve, bool hierarchical)
        {
            RegisterDirty(ve, RenderDataDirtyTypes.Clipping | (hierarchical ? RenderDataDirtyTypes.ClippingHierarchy : 0), RenderDataDirtyTypeClasses.Clipping);
        }

        public void UIEOnOpacityChanged(VisualElement ve, bool hierarchical = false)
        {
            RegisterDirty(ve, RenderDataDirtyTypes.Opacity | (hierarchical ? RenderDataDirtyTypes.OpacityHierarchy : 0), RenderDataDirtyTypeClasses.Opacity);
        }

        public void UIEOnColorChanged(VisualElement ve)
        {
            RegisterDirty(ve, RenderDataDirtyTypes.Color, RenderDataDirtyTypeClasses.Color);
        }

        public void UIEOnTransformOrSizeChanged(VisualElement ve, bool transformChanged, bool clipRectSizeChanged)
        {
            RenderDataDirtyTypes flags =
                (transformChanged ? RenderDataDirtyTypes.Transform : RenderDataDirtyTypes.None) |
                (clipRectSizeChanged ? RenderDataDirtyTypes.ClipRectSize : RenderDataDirtyTypes.None);
            RegisterDirty(ve, flags, RenderDataDirtyTypeClasses.TransformSize);
        }

        public void UIEOnVisualsChanged(VisualElement ve, bool hierarchical)
        {
            RegisterDirty(ve, RenderDataDirtyTypes.Visuals | (hierarchical ? RenderDataDirtyTypes.VisualsHierarchy : 0), RenderDataDirtyTypeClasses.Visuals);
        }

        public void UIEOnOpacityIdChanged(VisualElement ve)
        {
            RegisterDirty(ve, RenderDataDirtyTypes.VisualsOpacityId, RenderDataDirtyTypeClasses.Visuals);
        }

        public void UIEOnDisableRenderingChanged(VisualElement ve)
        {
            if (ve.renderData != null)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change their display style during generateVisualContent callback execution nor during visual tree rendering");

                CommandManipulator.DisableElementRendering(this , ve, ve.disableRendering);
            }
        }

        #endregion

        internal BaseVisualElementPanel panel { get; private set; }
        internal UIRenderDevice device { get; private set; }
        public BaseElementBuilder elementBuilder => m_VisualChangesProcessor.elementBuilder;
        internal AtlasBase atlas { get; private set; }
        internal VectorImageManager vectorImageManager { get; private set; }
        internal TempMeshAllocatorImpl tempMeshAllocator { get; private set; }
        internal MeshWriteDataPool meshWriteDataPool { get; } = new();
        public EntryRecorder entryRecorder = new (s_SharedEntryPool);
        internal EntryPool entryPool => s_SharedEntryPool;
        public MeshGenerationDeferrer meshGenerationDeferrer => m_MeshGenerationDeferrer;
        public MeshGenerationNodeManager meshGenerationNodeManager { get; private set; }
        internal JobManager jobManager { get; private set; }
        internal UIRVEShaderInfoAllocator shaderInfoAllocator; // Not a property because this is a struct we want to mutate
        internal bool drawStats { get; set; }
        internal bool drawInCameras { get; }
        internal bool isFlat { get; }
        public bool forceGammaRendering { get; } // This indicates the effective state, unlike Panel.forceGammaRendering.

        internal RenderChainCommand AllocCommand() => m_CommandPool.Get();

        internal void FreeCommand(RenderChainCommand cmd)
        {
            cmd.Reset();
            m_CommandPool.Return(cmd);
        }

        internal void RepaintTexturedElements()
        {
            if (m_RootRenderTree != null)
                DepthFirstRepaintTextured(m_RootRenderTree);
        }

        // Iterates on render trees (caller performs null check)
        void DepthFirstRepaintTextured(RenderTree renderTree)
        {
            // Work
            RenderData renderData = renderTree.rootRenderData;
            if (renderData != null)
                DepthFirstRepaintTextured(renderData);

            // Recurse
            RenderTree child = renderTree.firstChild;
            while (child != null)
            {
                DepthFirstRepaintTextured(child);
                child = child.nextSibling;
            }
        }

        // Iterates on render data (caller performs null check)
        void DepthFirstRepaintTextured(RenderData renderData)
        {
            // Work
            if (renderData.graphicEntries != null)
                UIEOnVisualsChanged(renderData.owner, false);

            // Recurse
            RenderData child = renderData.firstChild;
            while (child != null)
            {
                DepthFirstRepaintTextured(child);
                child = child.nextSibling;
            }
        }

        public ExtraRenderData GetOrAddExtraData(RenderData renderData)
        {
            if (!m_ExtraData.TryGetValue(renderData, out ExtraRenderData extraData))
            {
                extraData = m_ExtraDataPool.Get();
                m_ExtraData.Add(renderData, extraData);
                renderData.flags |= RenderDataFlags.HasExtraData;
            }

            return extraData;
        }

        public void FreeExtraData(RenderData renderData)
        {
            Debug.Assert(renderData.hasExtraData);
            Debug.Assert(!renderData.hasExtraMeshes); // Meshes should have been freed before calling this method
            m_ExtraData.Remove(renderData, out ExtraRenderData extraData);
            m_ExtraDataPool.Return(extraData);

            renderData.flags &= ~RenderDataFlags.HasExtraData;
        }

        public void InsertExtraMesh(RenderData renderData, MeshHandle mesh)
        {
            ExtraRenderData extraData = GetOrAddExtraData(renderData);
            var newNode = m_MeshHandleNodePool.Get();
            newNode.data = mesh;
            newNode.InsertFirst(ref extraData.extraMesh);
            renderData.flags |= RenderDataFlags.HasExtraMeshes;
        }

        public void FreeExtraMeshes(RenderData renderData)
        {
            if (!renderData.hasExtraMeshes)
                return;

            ExtraRenderData extraData = m_ExtraData[renderData];
            BasicNode<MeshHandle> mesh = extraData.extraMesh;
            extraData.extraMesh = null;
            while (mesh != null)
            {
                device.Free(mesh.data);
                BasicNode<MeshHandle> next = mesh.next;

                mesh.data = null;
                mesh.next = null;
                m_MeshHandleNodePool.Return(mesh);

                mesh = next;
            }

            renderData.flags &= ~RenderDataFlags.HasExtraMeshes;
        }

        public void InsertTexture(RenderData renderData, Texture src, TextureId id, bool isAtlas)
        {
            BasicNode<GraphicEntry> node = m_GraphicEntryPool.Get();
            node.data.source = src;
            node.data.actual = id;
            node.data.replaced = isAtlas;
            node.InsertFirst(ref renderData.graphicEntries);
        }

        public void InsertVectorImage(RenderData renderData, VectorImage vi)
        {
            BasicNode<GraphicEntry> node = m_GraphicEntryPool.Get();
            node.data.vectorImage = vi;
            node.InsertFirst(ref renderData.graphicEntries);
        }

        public void ResetGraphicEntries(RenderData renderData)
        {
            AtlasBase atlas = this.atlas;
            TextureRegistry registry = m_TextureRegistry;
            BasicNodePool<GraphicEntry> pool = m_GraphicEntryPool;

            BasicNode<GraphicEntry> current = renderData.graphicEntries;
            renderData.graphicEntries = null;
            while (current != null)
            {
                var next = current.next;
                if (current.data.vectorImage != null)
                {
                    vectorImageManager.RemoveUser(current.data.vectorImage);
                    current.data.vectorImage = null;
                }
                else
                {
                    if (current.data.replaced)
                        atlas.ReturnAtlas(renderData.owner, current.data.source as Texture2D, current.data.actual);
                    else
                        registry.Release(current.data.actual);
                    current.data.source = null;
                }
                pool.Return(current);
                current = next;
            }
        }


    }
}
