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
        LinkedPool<RenderChainCommand> m_CommandPool = new(() => new RenderChainCommand(), null);
        LinkedPool<ExtraRenderData> m_ExtraDataPool = new(() => new ExtraRenderData(), null);
        BasicNodePool<MeshHandle> m_MeshHandleNodePool = new();
        BasicNodePool<TextureEntry> m_TexturePool = new();
        Dictionary<RenderData, ExtraRenderData> m_ExtraData = new();
        internal List<ElementInsertionData> m_InsertionList = new(1024); // Internal for testing purposes
        HashSet<UIRenderer> m_RenderersToReset = new();

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

        internal RenderData GetPooledRenderData()
        {
            var data = m_RenderDataPool.Get();
            data.Init();
            return data;
        }

        internal void ReturnPoolRenderData(RenderData data)
        {
            if (data != null)
                m_RenderDataPool.Release(data);
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
                m_RenderTreePool.Release(tree);
        }

        static EntryPool s_SharedEntryPool = new(10000);

        // Profiling
        static readonly ProfilerMarker k_MarkerProcess = new("RenderTreeManager.Process");
        static readonly ProfilerMarker k_MarkerSerialize = new("RenderChain.Serialize");


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
            if (panel.contextType == ContextType.Player)
            {
                var runtimePanel = (BaseRuntimePanel)panel;
                drawInCameras = runtimePanel.drawsInCameras;
                if (drawInCameras)
                    m_DefaultMat = Shaders.runtimeWorldMaterial;
                else
                {
                    m_DefaultMat = Shaders.runtimeMaterial;
                    if (activeColorSpace == ColorSpace.Linear)
                        forceGammaRendering = panel.panelRenderer.forceGammaRendering;
                }
            }
            else // Editor
            {
                if (activeColorSpace == ColorSpace.Linear)
                    forceGammaRendering = true;
                m_DefaultMat = Shaders.editorMaterial;
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

            k_MarkerSerialize.Begin();

            Exception immediateException = null;

            m_BlockDirtyRegistration = true;
            device.EvaluateChain(
                m_RootRenderTree.firstCommand,
                m_DefaultMat,
                m_DefaultMat,
                vectorImageManager?.atlas,
                shaderInfoAllocator.atlas,
                null,
                panel.scaledPixelsPerPoint,
                true,
                ref immediateException);
            m_BlockDirtyRegistration = false;

            // Assign the command lists to the UIRenderer components.
            // Note that the device may be null at this point (e.g., EvaluateChain may had to
            // dispose of the RenderChain when evaluating an immediate element that closed a window).
            List<CommandList> frameCommandLists = device?.currentFrameCommandLists;
            if (drawInCameras && frameCommandLists != null)
            {
                for (int cmdListIndex = 0; cmdListIndex < device.currentFrameCommandListCount; ++cmdListIndex)
                {
                    var cmdList = frameCommandLists[cmdListIndex];
                    if (cmdList.m_Owner.isWorldSpaceRootUIDocument)
                    {
                        var rootUIDocumentElement = cmdList.m_Owner as UIDocumentRootElement;
                        Debug.Assert(rootRenderTree != null); // Otherwise the flag should not be set
                        UIRenderer renderer = rootUIDocumentElement.uiRenderer;
                        if (!m_RenderersToReset.Contains(renderer))
                        {
                            renderer.ResetDrawCallData();
                            m_RenderersToReset.Add(renderer);
                        }
                    }
                }

                for (int cmdListIndex = 0; cmdListIndex < device.currentFrameCommandListCount; ++cmdListIndex)
                {
                    var cmdList = frameCommandLists[cmdListIndex];
                    var renderer = (cmdList.m_Owner as UIDocumentRootElement).uiRenderer;
                    if (renderer != null)
                    {
                        var commandLists = device.commandLists;
                        renderer.commandLists = commandLists;

                        int safeFrameIndex = (int)device.frameIndex % commandLists.Length;
                        renderer.AddDrawCallData(safeFrameIndex, cmdListIndex, cmdList.m_Material);
                    }
                }

                m_RenderersToReset.Clear();
            }

            device.SynchronizeMaterials();

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

            RenderSingleTree(m_RootRenderTree, null, RectInt.zero);

            if (drawStats)
                DrawStats();
        }

        void RenderNestedTrees()
        {
            m_Compositor.RenderNestedPasses();
        }

        public void RenderSingleTree(RenderTree renderTree, RenderTexture nestedTreeRT, RectInt nestedTreeViewport)
        {
            // This function is not supposed to be used to draw the root render tree of a panel that draws in cameras.
            Debug.Assert(!drawInCameras || renderTree != m_RootRenderTree);

            if (renderTree.firstCommand == null)
                return;

            Exception immediateException = null;

            bool shouldResetRT = false;
            RenderTexture oldRT = null;

            Rect viewport;
            if (renderTree == m_RootRenderTree)
            {
                Debug.Assert(nestedTreeRT == null);
                viewport = panel.visualTree.layout;
            }
            else
            {
                Debug.Assert(nestedTreeRT != null);
                viewport = UIRUtility.CastToRect(nestedTreeViewport);
                oldRT = RenderTexture.active;
                Camera.SetupCurrent(null);
                RenderTexture.active = nestedTreeRT;
                shouldResetRT = true;

                GL.Viewport(new Rect(0, 0, viewport.width, viewport.height));
                // TODO: When we introduce atlas support, we should only clear the viewport area
                GL.Clear(true, true, Color.clear, UIRUtility.k_ClearZ);
            }

            if (forceGammaRendering)
                m_DefaultMat.EnableKeyword(Shaders.k_ForceGammaKeyword);
            else
                m_DefaultMat.DisableKeyword(Shaders.k_ForceGammaKeyword);

            m_DefaultMat.SetPass(0);

            var projection = ProjectionUtils.Ortho(viewport.xMin, viewport.xMax, viewport.yMax, viewport.yMin, -0.001f, 1.001f);
            GL.LoadProjectionMatrix(projection);
            GL.modelview = Matrix4x4.identity;

            Rect scissor = new Rect(0, 0, viewport.width, viewport.height);

            //TODO: Reactivate this guard check once InspectorWindow is fixed to stop adding VEs during OnGUI
            m_BlockDirtyRegistration = drawInCameras; // For now, we only enable it for drawInCameras
            device.EvaluateChain(
                renderTree.firstCommand,
                m_DefaultMat,
                m_DefaultMat,
                vectorImageManager?.atlas,
                shaderInfoAllocator.atlas,
                scissor,
                panel.scaledPixelsPerPoint,
                false,
                ref immediateException);
            m_BlockDirtyRegistration = false;

            Utility.DisableScissor();

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

        internal RenderChainCommand AllocCommand()
        {
            var cmd = m_CommandPool.Get();
            cmd.Reset();
            return cmd;
        }

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
            if (renderData.textures != null)
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
            BasicNode<TextureEntry> node = m_TexturePool.Get();
            node.data.source = src;
            node.data.actual = id;
            node.data.replaced = isAtlas;
            node.InsertFirst(ref renderData.textures);
        }

        public void ResetTextures(RenderData renderData)
        {
            AtlasBase atlas = this.atlas;
            TextureRegistry registry = m_TextureRegistry;
            BasicNodePool<TextureEntry> pool = m_TexturePool;

            BasicNode<TextureEntry> current = renderData.textures;
            renderData.textures = null;
            while (current != null)
            {
                var next = current.next;
                if (current.data.replaced)
                    atlas.ReturnAtlas(renderData.owner, current.data.source as Texture2D, current.data.actual);
                else
                    registry.Release(current.data.actual);
                pool.Return(current);
                current = next;
            }
        }

        void DrawStats()
        {
            bool realDevice = device as UIRenderDevice != null;
            float y_off = 12;
            var rc = new Rect(30, 60, 1000, 100);
            GUI.Box(new Rect(20, 40, 200, realDevice ? 380 : 256), "UI Toolkit Draw Stats");
            GUI.Label(rc, "Elements added\t: " + m_Stats.elementsAdded); rc.y += y_off;
            GUI.Label(rc, "Elements removed\t: " + m_Stats.elementsRemoved); rc.y += y_off;
            GUI.Label(rc, "Mesh allocs allocated\t: " + m_Stats.newMeshAllocations); rc.y += y_off;
            GUI.Label(rc, "Mesh allocs updated\t: " + m_Stats.updatedMeshAllocations); rc.y += y_off;
            GUI.Label(rc, "Clip update roots\t: " + m_Stats.recursiveClipUpdates); rc.y += y_off;
            GUI.Label(rc, "Clip update total\t: " + m_Stats.recursiveClipUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Opacity update roots\t: " + m_Stats.recursiveOpacityUpdates); rc.y += y_off;
            GUI.Label(rc, "Opacity update total\t: " + m_Stats.recursiveOpacityUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Opacity ID update\t: " + m_Stats.opacityIdUpdates); rc.y += y_off;
            GUI.Label(rc, "Xform update roots\t: " + m_Stats.recursiveTransformUpdates); rc.y += y_off;
            GUI.Label(rc, "Xform update total\t: " + m_Stats.recursiveTransformUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Xformed by bone\t: " + m_Stats.boneTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by skipping\t: " + m_Stats.skipTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by nudging\t: " + m_Stats.nudgeTransformed); rc.y += y_off;
            GUI.Label(rc, "Xformed by repaint\t: " + m_Stats.visualUpdateTransformed); rc.y += y_off;
            GUI.Label(rc, "Visual update roots\t: " + m_Stats.recursiveVisualUpdates); rc.y += y_off;
            GUI.Label(rc, "Visual update total\t: " + m_Stats.recursiveVisualUpdatesExpanded); rc.y += y_off;
            GUI.Label(rc, "Visual update flats\t: " + m_Stats.nonRecursiveVisualUpdates); rc.y += y_off;
            GUI.Label(rc, "Dirty processed\t: " + m_Stats.dirtyProcessed); rc.y += y_off;
            GUI.Label(rc, "Group-xform updates\t: " + m_Stats.groupTransformElementsChanged); rc.y += y_off;

            if (!realDevice)
                return;

            rc.y += y_off;
            var drawStats = ((UIRenderDevice)device).GatherDrawStatistics();
            GUI.Label(rc, "Frame index\t: " + drawStats.currentFrameIndex); rc.y += y_off;
            GUI.Label(rc, "Command count\t: " + drawStats.commandCount); rc.y += y_off;
            GUI.Label(rc, "Skip cmd counts\t: " + drawStats.skippedCommandCount); rc.y += y_off;
            GUI.Label(rc, "Draw commands\t: " + drawStats.drawCommandCount); rc.y += y_off;
            GUI.Label(rc, "Disable commands\t: " + drawStats.disableCommandCount); rc.y += y_off;
            GUI.Label(rc, "Draw ranges\t: " + drawStats.drawRangeCount); rc.y += y_off;
            GUI.Label(rc, "Draw range calls\t: " + drawStats.drawRangeCallCount); rc.y += y_off;
            GUI.Label(rc, "Material sets\t: " + drawStats.materialSetCount); rc.y += y_off;
            GUI.Label(rc, "Stencil changes\t: " + drawStats.stencilRefChanges); rc.y += y_off;
            GUI.Label(rc, "Immediate draws\t: " + drawStats.immediateDraws); rc.y += y_off;
            GUI.Label(rc, "Total triangles\t: " + (drawStats.totalIndices / 3)); rc.y += y_off;
        }
    }
}
