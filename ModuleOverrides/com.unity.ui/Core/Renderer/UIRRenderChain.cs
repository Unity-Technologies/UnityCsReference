// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define UIR_DEBUG_CHAIN_BUILDER
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;

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
        public uint immedateRenderersActive;
    }

    // We want to pool MeshWriteData instances, but unlike usual pooling, we don't explicitly return them to a pool. So instead,
    // here we'll simply keep track of them so they can be reused, but we can only reuse them when a Reset has been performed.
    class MeshWriteDataPool : ImplicitPool<MeshWriteData>
    {
        static readonly Func<MeshWriteData> k_CreateAction = () => new MeshWriteData();

        public MeshWriteDataPool()
            : base(k_CreateAction, null, 100, 1000) { }
    }

    class EntryPool : ImplicitPool<Entry>
    {
        static readonly Func<Entry> k_CreateAction = () => new Entry();
        static readonly Action<Entry> k_ResetAction = e =>
        {
            e.firstChild = null;
            e.nextSibling = null;
            e.texture = null;
            e.material = null;
            e.gradientsOwner = null;
        };

        public EntryPool(int maxCapacity = 1000)
            : base(k_CreateAction, k_ResetAction, 100, maxCapacity) { }
    }

    partial class RenderChain : IDisposable
    {
        struct DepthOrderedDirtyTracking // Depth then register-time order
        {
            // For each depth level, we keep a double linked list of VisualElements that are dirty for some reason.
            // The actual reason is stored in renderChainData.dirtiedValues.
            public List<VisualElement> heads, tails; // Indexed by VE hierarchy depth

            // The following two arrays store, for each dirty type class, the range of depth levels
            // where we have elements that are dirty with that dirty type class.
            public int[] minDepths, maxDepths; // Indexed per dirty type class

            public uint dirtyID; // A monotonically increasing ID used to avoid double processing of some elements

            // As the depth of the hierarchy grows, we need to enlarge as many double linked lists
            public void EnsureFits(int maxDepth)
            {
                while (heads.Count <= maxDepth)
                {
                    heads.Add(null);
                    tails.Add(null);
                }
            }

            public void RegisterDirty(VisualElement ve, RenderDataDirtyTypes dirtyTypes, RenderDataDirtyTypeClasses dirtyTypeClass)
            {
                Debug.Assert(dirtyTypes != 0);
                int depth = ve.renderChainData.hierarchyDepth;
                int dirtyTypeClassIndex = (int)dirtyTypeClass;
                minDepths[dirtyTypeClassIndex] = depth < minDepths[dirtyTypeClassIndex] ? depth : minDepths[dirtyTypeClassIndex];
                maxDepths[dirtyTypeClassIndex] = depth > maxDepths[dirtyTypeClassIndex] ? depth : maxDepths[dirtyTypeClassIndex];
                if (ve.renderChainData.dirtiedValues != 0)
                {
                    ve.renderChainData.dirtiedValues |= dirtyTypes;
                    return;
                }

                ve.renderChainData.dirtiedValues = dirtyTypes;
                if (tails[depth] != null)
                {
                    tails[depth].renderChainData.nextDirty = ve;
                    ve.renderChainData.prevDirty = tails[depth];
                    tails[depth] = ve;
                }
                else heads[depth] = tails[depth] = ve;
            }

            public void ClearDirty(VisualElement ve, RenderDataDirtyTypes dirtyTypesInverse)
            {
                Debug.Assert(ve.renderChainData.dirtiedValues != 0);
                ve.renderChainData.dirtiedValues &= dirtyTypesInverse;
                if (ve.renderChainData.dirtiedValues == 0)
                {
                    // Mend the chain
                    if (ve.renderChainData.prevDirty != null)
                        ve.renderChainData.prevDirty.renderChainData.nextDirty = ve.renderChainData.nextDirty;
                    if (ve.renderChainData.nextDirty != null)
                        ve.renderChainData.nextDirty.renderChainData.prevDirty = ve.renderChainData.prevDirty;
                    if (tails[ve.renderChainData.hierarchyDepth] == ve)
                    {
                        Debug.Assert(ve.renderChainData.nextDirty == null);
                        tails[ve.renderChainData.hierarchyDepth] = ve.renderChainData.prevDirty;
                    }
                    if (heads[ve.renderChainData.hierarchyDepth] == ve)
                    {
                        Debug.Assert(ve.renderChainData.prevDirty == null);
                        heads[ve.renderChainData.hierarchyDepth] = ve.renderChainData.nextDirty;
                    }
                    ve.renderChainData.prevDirty = ve.renderChainData.nextDirty = null;
                }
            }

            public void Reset()
            {
                for (int i = 0; i < minDepths.Length; i++)
                {
                    minDepths[i] = int.MaxValue;
                    maxDepths[i] = int.MinValue;
                }
            }
        }

        struct RenderChainStaticIndexAllocator
        {
            static List<RenderChain> renderChains = new List<RenderChain>(4);
            public static int AllocateIndex(RenderChain renderChain)
            {
                int index = renderChains.IndexOf(null);
                if (index >= 0)
                    renderChains[index] = renderChain;
                else
                {
                    index = renderChains.Count;
                    renderChains.Add(renderChain);
                }
                return index;
            }

            public static void FreeIndex(int index)
            {
                renderChains[index] = null;
            }

            public static RenderChain AccessIndex(int index)
            {
                return renderChains[index];
            }
        };

        struct RenderNodeData
        {
            public Material standardMaterial;
            public Material initialMaterial;
            public MaterialPropertyBlock matPropBlock;
            public RenderChainCommand firstCommand;

            public UIRenderDevice device;
            public Texture vectorAtlas, shaderInfoAtlas;
            public float dpiScale;

            public uint frameIndex;
        };

        RenderChainCommand m_FirstCommand;
        DepthOrderedDirtyTracking m_DirtyTracker;
        VisualChangesProcessor m_VisualChangesProcessor;
        LinkedPool<RenderChainCommand> m_CommandPool = new LinkedPool<RenderChainCommand>(() => new RenderChainCommand(), cmd => {});
        BasicNodePool<TextureEntry> m_TexturePool = new BasicNodePool<TextureEntry>();
        List<RenderNodeData> m_RenderNodesData = new List<RenderNodeData>();
        MeshGenerationDeferrer m_MeshGenerationDeferrer = new();
        Shader m_DefaultShader, m_DefaultWorldSpaceShader;
        Material m_DefaultMat, m_DefaultWorldSpaceMat;
        bool m_BlockDirtyRegistration;
        int m_StaticIndex = -1;
        int m_ActiveRenderNodes = 0;
        int m_CustomMaterialCommands = 0; // TODO: Get rid of this
        ChainBuilderStats m_Stats;
        uint m_StatsElementsAdded, m_StatsElementsRemoved;

        TextureRegistry m_TextureRegistry = TextureRegistry.instance;

        internal RenderChainCommand firstCommand { get { return m_FirstCommand; } }
        public OpacityIdAccelerator opacityIdAccelerator { get; private set; }

        static EntryPool s_SharedEntryPool = new(10000);

        // Profiling
        static readonly ProfilerMarker k_MarkerProcess = new("RenderChain.Process");
        static readonly ProfilerMarker k_MarkerClipProcessing = new("RenderChain.UpdateClips");
        static readonly ProfilerMarker k_MarkerOpacityProcessing = new("RenderChain.UpdateOpacity");
        static readonly ProfilerMarker k_MarkerColorsProcessing = new("RenderChain.UpdateColors");
        static readonly ProfilerMarker k_MarkerTransformProcessing = new("RenderChain.UpdateTransforms");
        static readonly ProfilerMarker k_MarkerVisualsProcessing = new("RenderChain.UpdateVisuals");

        static RenderChain()
        {
            UIR.Utility.RegisterIntermediateRenderers += OnRegisterIntermediateRenderers;
            UIR.Utility.RenderNodeExecute += OnRenderNodeExecute;
        }

        public RenderChain(BaseVisualElementPanel panel) : this(panel, new UIRenderDevice(panel.vertexBudget), panel.atlas, new VectorImageManager(panel.atlas))
        {
        }

        protected RenderChain(BaseVisualElementPanel panel, UIRenderDevice device, AtlasBase atlas, VectorImageManager vectorImageManager)
        {
            // A reasonable starting depth level suggested here
            m_DirtyTracker.heads = new List<VisualElement>(8);
            m_DirtyTracker.tails = new List<VisualElement>(8);
            m_DirtyTracker.minDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.maxDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.Reset();

            if (m_RenderNodesData.Count < 1)
                m_RenderNodesData.Add(new RenderNodeData() { matPropBlock = new MaterialPropertyBlock() });

            this.panel = panel;
            this.device = device;
            this.atlas = atlas;
            this.vectorImageManager = vectorImageManager;

            // TODO: Share across all panels
            tempMeshAllocator = new TempMeshAllocatorImpl();
            jobManager = new JobManager();

            shaderInfoAllocator.Construct();
            opacityIdAccelerator = new OpacityIdAccelerator();

            m_VisualChangesProcessor = new VisualChangesProcessor(this);

            if (panel is BaseRuntimePanel { drawsInCameras: true })
            {
                device.drawsInCameras = drawInCameras = true;
                m_StaticIndex = RenderChainStaticIndexAllocator.AllocateIndex(this);
            }

            device.isFlat = isFlat = panel.isFlat;
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
                if (m_StaticIndex >= 0)
                    RenderChainStaticIndexAllocator.FreeIndex(m_StaticIndex);
                m_StaticIndex = -1;

                var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
                while (ve != null)
                {
                    ResetTextures(ve);
                    ve = ve.renderChainData.next;
                }

                UIRUtility.Destroy(m_DefaultMat);
                UIRUtility.Destroy(m_DefaultWorldSpaceMat);
                m_DefaultMat = m_DefaultWorldSpaceMat = null;

                tempMeshAllocator.Dispose();
                tempMeshAllocator = null;

                jobManager.Dispose();
                jobManager = null;

                vectorImageManager?.Dispose();
                vectorImageManager = null;

                shaderInfoAllocator.Dispose();
                shaderInfoAllocator = new UIRVEShaderInfoAllocator();

                device?.Dispose();
                device = null;

                opacityIdAccelerator?.Dispose();
                opacityIdAccelerator = null;

                m_VisualChangesProcessor?.Dispose();
                m_VisualChangesProcessor = null;

                m_MeshGenerationDeferrer?.Dispose();
                m_MeshGenerationDeferrer = null;

                atlas = null;
                m_ActiveRenderNodes = 0;
                m_RenderNodesData.Clear();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        internal ChainBuilderStats stats { get { return m_Stats; } }

        public void ProcessChanges()
        {
            k_MarkerProcess.Begin();
            m_Stats = new ChainBuilderStats();
            m_Stats.elementsAdded += m_StatsElementsAdded;
            m_Stats.elementsRemoved += m_StatsElementsRemoved;
            m_StatsElementsAdded = m_StatsElementsRemoved = 0;


            int dirtyClass;
            RenderDataDirtyTypes dirtyFlags;
            RenderDataDirtyTypes clearDirty;

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Clipping;
            dirtyFlags = RenderDataDirtyTypes.Clipping | RenderDataDirtyTypes.ClippingHierarchy;
            clearDirty = ~dirtyFlags;
            k_MarkerClipProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnClippingChanged(this, ve, m_DirtyTracker.dirtyID,
                                ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            k_MarkerClipProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Opacity;
            dirtyFlags = RenderDataDirtyTypes.Opacity | RenderDataDirtyTypes.OpacityHierarchy;
            clearDirty = ~dirtyFlags;
            k_MarkerOpacityProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnOpacityChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            k_MarkerOpacityProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Color;
            dirtyFlags = RenderDataDirtyTypes.Color;
            clearDirty = ~dirtyFlags;
            k_MarkerColorsProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnColorChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            k_MarkerColorsProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.TransformSize;
            dirtyFlags = RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.ClipRectSize;
            clearDirty = ~dirtyFlags;
            k_MarkerTransformProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnTransformOrSizeChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            k_MarkerTransformProcessing.End();

            jobManager.CompleteNudgeJobs();

            m_BlockDirtyRegistration = true; // Processing visuals may call generateVisualContent, which must be restricted to the allowed operations
            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Visuals;
            dirtyFlags = RenderDataDirtyTypes.AllVisuals;
            clearDirty = ~dirtyFlags;
            k_MarkerVisualsProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            m_VisualChangesProcessor.ProcessOnVisualsChanged(ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }

            m_MeshGenerationDeferrer.ProcessDeferredWork(m_VisualChangesProcessor.meshGenerationContext);
            m_VisualChangesProcessor.ConvertEntriesToCommands(ref m_Stats);
            jobManager.CompleteConvertMeshJobs();
            jobManager.CompleteCopyMeshJobs();
            opacityIdAccelerator.CompleteJobs();
            k_MarkerVisualsProcessing.End();
            m_BlockDirtyRegistration = false;

            tempMeshAllocator.Clear();
            meshWriteDataPool.ReturnAll();
            entryPool.ReturnAll();

            // Done with all dirtied elements
            m_DirtyTracker.Reset();


            // Commit new requests for atlases if any
            atlas?.InvokeUpdateDynamicTextures(panel); // TODO: For a shared atlas + drawInCameras, postpone after all updates have occurred.
            vectorImageManager?.Commit();
            shaderInfoAllocator.IssuePendingStorageChanges();

            device?.OnFrameRenderingBegin();

            k_MarkerProcess.End();
        }

        public void Render()
        {
            Material standardMaterial = GetStandardMaterial();
            panel.InvokeUpdateMaterial(standardMaterial);

            Exception immediateException = null;
            if (m_FirstCommand != null)
            {
                if (!drawInCameras)
                {
                    var viewport = panel.visualTree.layout;
                    standardMaterial?.SetPass(0);

                    var projection = ProjectionUtils.Ortho(viewport.xMin, viewport.xMax, viewport.yMax, viewport.yMin, -0.001f, 1.001f);
                    GL.LoadProjectionMatrix(projection);
                    GL.modelview = Matrix4x4.identity;
                }

                //TODO: Reactivate this guard check once InspectorWindow is fixed to stop adding VEs during OnGUI
                //m_BlockDirtyRegistration = true;
                device.EvaluateChain(m_FirstCommand, standardMaterial, standardMaterial, vectorImageManager?.atlas, shaderInfoAllocator.atlas,
                    panel.scaledPixelsPerPoint, ref immediateException);
                //m_BlockDirtyRegistration = false;
            }

            if (immediateException != null)
            {
                if (GUIUtility.IsExitGUIException(immediateException))
                    throw immediateException;

                // Wrap the exception, this plays more nicely with the callstack logging.
                throw new ImmediateModeException(immediateException);
            }

            if (drawStats)
                DrawStats();
        }

        #region UIElements event handling callbacks
        public void UIEOnChildAdded(VisualElement ve)
        {
            VisualElement parent = ve.hierarchy.parent;
            int index = parent != null ? parent.hierarchy.IndexOf(ve) : 0;

            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be added to an active visual tree during generateVisualContent callback execution nor during visual tree rendering");
            if (parent != null && !parent.renderChainData.isInChain)
                return; // Ignore it until its parent gets ultimately added

            uint addedCount = RenderEvents.DepthFirstOnChildAdded(this, parent, ve, index, true);
            Debug.Assert(ve.renderChainData.isInChain);
            Debug.Assert(ve.panel == this.panel);
            UIEOnClippingChanged(ve, true);
            UIEOnOpacityChanged(ve);
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
                RenderEvents.DepthFirstOnChildRemoving(this, ve.hierarchy[i]);
            for (int i = 0; i < childrenCount; i++)
                RenderEvents.DepthFirstOnChildAdded(this, ve, ve.hierarchy[i], i, false);

            UIEOnClippingChanged(ve, true);
            UIEOnOpacityChanged(ve, true);
            UIEOnVisualsChanged(ve, true);

        }

        public void UIEOnChildRemoving(VisualElement ve)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be removed from an active visual tree during generateVisualContent callback execution nor during visual tree rendering");


            m_StatsElementsRemoved += RenderEvents.DepthFirstOnChildRemoving(this, ve);
            Debug.Assert(!ve.renderChainData.isInChain);
        }

        public void UIEOnRenderHintsChanged(VisualElement ve)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("Render Hints cannot change under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                UIEOnChildRemoving(ve);
                UIEOnChildAdded(ve);
            }
        }

        public void UIEOnClippingChanged(VisualElement ve, bool hierarchical)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change clipping state under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Clipping | (hierarchical ? RenderDataDirtyTypes.ClippingHierarchy : 0), RenderDataDirtyTypeClasses.Clipping);
            }
        }

        public void UIEOnOpacityChanged(VisualElement ve, bool hierarchical = false)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change opacity under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Opacity | (hierarchical ? RenderDataDirtyTypes.OpacityHierarchy : 0), RenderDataDirtyTypeClasses.Opacity);
            }
        }

        public void UIEOnColorChanged(VisualElement ve)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change background color under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Color, RenderDataDirtyTypeClasses.Color);
            }
        }

        public void UIEOnTransformOrSizeChanged(VisualElement ve, bool transformChanged, bool clipRectSizeChanged)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot change size or transform under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                RenderDataDirtyTypes flags =
                    (transformChanged ? RenderDataDirtyTypes.Transform : RenderDataDirtyTypes.None) |
                    (clipRectSizeChanged ? RenderDataDirtyTypes.ClipRectSize : RenderDataDirtyTypes.None);
                m_DirtyTracker.RegisterDirty(ve, flags, RenderDataDirtyTypeClasses.TransformSize);
            }
        }

        public void UIEOnVisualsChanged(VisualElement ve, bool hierarchical)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot be marked for dirty repaint under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.Visuals | (hierarchical ? RenderDataDirtyTypes.VisualsHierarchy : 0), RenderDataDirtyTypeClasses.Visuals);
            }
        }

        public void UIEOnOpacityIdChanged(VisualElement ve)
        {
            if (ve.renderChainData.isInChain)
            {
                if (m_BlockDirtyRegistration)
                    throw new InvalidOperationException("VisualElements cannot for opacity id change under an active visual tree during generateVisualContent callback execution nor during visual tree rendering");

                m_DirtyTracker.RegisterDirty(ve, RenderDataDirtyTypes.VisualsOpacityId, RenderDataDirtyTypeClasses.Visuals);
            }
        }

        public void UIEOnDisableRenderingChanged(VisualElement ve)
        {
            if (ve.renderChainData.isInChain)
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
        internal EntryPool entryPool => s_SharedEntryPool;
        public MeshGenerationDeferrer meshGenerationDeferrer => m_MeshGenerationDeferrer;
        internal JobManager jobManager { get; private set; }
        internal UIRVEShaderInfoAllocator shaderInfoAllocator; // Not a property because this is a struct we want to mutate
        internal bool drawStats { get; set; }
        internal bool drawInCameras { get; }
        internal bool isFlat { get; }

        internal Shader defaultShader
        {
            get { return m_DefaultShader; }
            set
            {
                if (m_DefaultShader == value)
                    return;
                m_DefaultShader = value;
                UIRUtility.Destroy(m_DefaultMat);
                m_DefaultMat = null;
            }
        }
        internal Shader defaultWorldSpaceShader
        {
            get { return m_DefaultWorldSpaceShader; }
            set
            {
                if (m_DefaultWorldSpaceShader == value)
                    return;
                m_DefaultWorldSpaceShader = value;
                UIRUtility.Destroy(m_DefaultWorldSpaceMat);
                m_DefaultWorldSpaceMat = null;
            }
        }

        internal Material GetStandardMaterial()
        {
            if (m_DefaultMat == null && m_DefaultShader != null)
            {
                m_DefaultMat = new Material(m_DefaultShader);
                m_DefaultMat.hideFlags |= HideFlags.DontSaveInEditor;
            }
            return m_DefaultMat;
        }

        internal Material GetStandardWorldSpaceMaterial()
        {
            if (m_DefaultWorldSpaceMat == null && m_DefaultWorldSpaceShader != null)
            {
                m_DefaultWorldSpaceMat = new Material(m_DefaultWorldSpaceShader);
                m_DefaultWorldSpaceMat.hideFlags |= HideFlags.DontSaveInEditor;
            }
            return m_DefaultWorldSpaceMat;
        }

        internal void EnsureFitsDepth(int depth)
        {
            m_DirtyTracker.EnsureFits(depth);
        }

        internal void ChildWillBeRemoved(VisualElement ve)
        {
            if (ve.renderChainData.dirtiedValues != 0)
                m_DirtyTracker.ClearDirty(ve, ~ve.renderChainData.dirtiedValues);
            Debug.Assert(ve.renderChainData.dirtiedValues == 0);
            Debug.Assert(ve.renderChainData.prevDirty == null);
            Debug.Assert(ve.renderChainData.nextDirty == null);
        }

        internal RenderChainCommand AllocCommand()
        {
            var cmd = m_CommandPool.Get();
            cmd.Reset();
            return cmd;
        }

        internal void FreeCommand(RenderChainCommand cmd)
        {
            if (cmd.state.material != null)
                m_CustomMaterialCommands--;
            cmd.Reset();
            m_CommandPool.Return(cmd);
        }

        internal void OnRenderCommandAdded(RenderChainCommand command)
        {
            if (command.prev == null)
                m_FirstCommand = command;
            if (command.state.material != null)
                m_CustomMaterialCommands++;
        }

        internal void OnRenderCommandsRemoved(RenderChainCommand firstCommand, RenderChainCommand lastCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = lastCommand.next;
        }

        unsafe static RenderNodeData AccessRenderNodeData(IntPtr obj)
        {
            int *indices = (int*)obj.ToPointer();
            RenderChain rc = RenderChainStaticIndexAllocator.AccessIndex(indices[0]);
            return rc.m_RenderNodesData[indices[1]];
        }

        private unsafe static void OnRenderNodeExecute(IntPtr obj)
        {
            RenderNodeData rnd = AccessRenderNodeData(obj);
            rnd.device.ExecuteCommandList(rnd.frameIndex);
        }

        private static void OnRegisterIntermediateRenderers(Camera camera)
        {
            int commandOrder = 0;
            var panels = UIElementsUtility.GetPanelsIterator();
            while (panels.MoveNext())
            {
                var p = panels.Current.Value;
                RenderChain renderChain = (p.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater)?.renderChain;
                if (renderChain == null || renderChain.m_StaticIndex < 0 || renderChain.m_FirstCommand == null)
                    continue;

                BaseRuntimePanel rtp = (BaseRuntimePanel)p;
                Material standardMaterial = renderChain.GetStandardWorldSpaceMaterial();
                RenderNodeData rndSource = new RenderNodeData();
                rndSource.device = renderChain.device;
                rndSource.standardMaterial = standardMaterial;
                rndSource.vectorAtlas = renderChain.vectorImageManager?.atlas;
                rndSource.shaderInfoAtlas = renderChain.shaderInfoAllocator.atlas;
                rndSource.dpiScale = rtp.scaledPixelsPerPoint;
                rndSource.frameIndex = renderChain.device.frameIndex;

                if (renderChain.m_CustomMaterialCommands == 0)
                {
                    // Trivial case, custom materials not used, so we don't have to chop the chain
                    // to multiple intermediate renderers
                    rndSource.initialMaterial = standardMaterial;
                    rndSource.firstCommand = renderChain.m_FirstCommand;
                    OnRegisterIntermediateRendererMat(rtp, renderChain, ref rndSource, camera, commandOrder++);
                    continue;
                }

                // Complex case, custom materials used
                // TODO: Early out once all custom materials have been counted
                Material lastMaterial = null;
                var command = renderChain.m_FirstCommand;
                RenderChainCommand commandToStartWith = command;
                while (command != null)
                {
                    if (command.type != CommandType.Draw)
                    {
                        command = command.next;
                        continue;
                    }
                    Material commandMat = command.state.material == null ? standardMaterial : command.state.material;
                    if (commandMat != lastMaterial)
                    {
                        if (lastMaterial != null)
                        {
                            rndSource.initialMaterial = lastMaterial;
                            rndSource.firstCommand = commandToStartWith;
                            OnRegisterIntermediateRendererMat(rtp, renderChain, ref rndSource, camera, commandOrder++);
                            commandToStartWith = command;
                        }
                        lastMaterial = commandMat;
                    }
                    command = command.next;
                } // While render chain commands to execute

                if (commandToStartWith != null)
                {
                    rndSource.initialMaterial = lastMaterial;
                    rndSource.firstCommand = commandToStartWith;
                    OnRegisterIntermediateRendererMat(rtp, renderChain, ref rndSource, camera, commandOrder++);
                }
            } // For each panel
        }

        private unsafe static void OnRegisterIntermediateRendererMat(BaseRuntimePanel rtp, RenderChain renderChain, ref RenderNodeData rnd, Camera camera, int sameDistanceSortPriority)
        {
            int renderNodeIndex = renderChain.m_ActiveRenderNodes++;
            if (renderNodeIndex < renderChain.m_RenderNodesData.Count)
            {
                var reuseRND = renderChain.m_RenderNodesData[renderNodeIndex];
                rnd.matPropBlock = reuseRND.matPropBlock;
                renderChain.m_RenderNodesData[renderNodeIndex] = rnd;
            }
            else
            {
                rnd.matPropBlock = new MaterialPropertyBlock();
                renderNodeIndex = renderChain.m_RenderNodesData.Count;
                renderChain.m_RenderNodesData.Add(rnd);
            }

            int* userData = stackalloc int[2];
            userData[0] = renderChain.m_StaticIndex;
            userData[1] = renderNodeIndex;
            UIR.Utility.RegisterIntermediateRenderer(camera, rnd.initialMaterial, Matrix4x4.identity,
                new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)),
                rtp.worldSpaceLayer, 0, false, sameDistanceSortPriority, (int)UIR.Utility.RendererCallbacks.RendererCallback_Exec,
                new IntPtr(userData), sizeof(int) * 2);
        }

        internal void RepaintTexturedElements()
        {
            // Invalidate all elements shaderInfoAllocs
            var ve = GetFirstElementInPanel(m_FirstCommand?.owner);
            while (ve != null)
            {
                // Cause a regen on textured elements to get the new UVs from the atlas
                if (ve.renderChainData.textures != null)
                    UIEOnVisualsChanged(ve, false);

                ve = ve.renderChainData.next;
            }
            UIEOnOpacityChanged(panel.visualTree);
        }

        public void AppendTexture(VisualElement ve, Texture src, TextureId id, bool isAtlas)
        {
            BasicNode<TextureEntry> node = m_TexturePool.Get();
            node.data.source = src;
            node.data.actual = id;
            node.data.replaced = isAtlas;
            node.AppendTo(ref ve.renderChainData.textures);
        }

        public void ResetTextures(VisualElement ve)
        {
            AtlasBase atlas = this.atlas;
            TextureRegistry registry = m_TextureRegistry;
            BasicNodePool<TextureEntry> pool = m_TexturePool;

            BasicNode<TextureEntry> current = ve.renderChainData.textures;
            ve.renderChainData.textures = null;
            while (current != null)
            {
                var next = current.next;
                if (current.data.replaced)
                    atlas.ReturnAtlas(ve, current.data.source as Texture2D, current.data.actual);
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

        static VisualElement GetFirstElementInPanel(VisualElement ve)
        {
            while (ve != null && ve.renderChainData.prev?.renderChainData.isInChain == true)
                ve = ve.renderChainData.prev;
            return ve;
        }

    }

    [Flags]
    internal enum RenderDataDirtyTypes
    {
        None = 0,
        Transform = 1 << 0,
        ClipRectSize = 1 << 1,
        Clipping = 1 << 2,           // The clipping state of the VE needs to be reevaluated.
        ClippingHierarchy = 1 << 3,  // Same as above, but applies to all descendants too.
        Visuals = 1 << 4,            // The visuals of the VE need to be repainted.
        VisualsHierarchy = 1 << 5,   // Same as above, but applies to all descendants too.
        VisualsOpacityId = 1 << 6,   // The vertices only need their opacityId to be updated.
        Opacity = 1 << 7,            // The opacity of the VE needs to be updated.
        OpacityHierarchy = 1 << 8,   // Same as above, but applies to all descendants too.
        Color = 1 << 9,              // The background color of the VE needs to be updated.

        AllVisuals = Visuals | VisualsHierarchy | VisualsOpacityId
    }

    internal enum RenderDataDirtyTypeClasses
    {
        Clipping,
        Opacity,
        Color,
        TransformSize,
        Visuals,

        Count
    }

    [Flags]
    enum RenderDataFlags
    {
        IsInChain = 1 << 0,
        IsGroupTransform = 1 << 1,
    }

    struct RenderChainVEData
    {
        public VisualElement prev, next; // This is a flattened view of the visual element hierarchy
        public VisualElement groupTransformAncestor, boneTransformAncestor;
        public VisualElement prevDirty, nextDirty; // Embedded doubly-linked list for dirty updates
        public RenderDataFlags flags;
        public int hierarchyDepth; // 0 is for the root
        public RenderDataDirtyTypes dirtiedValues;
        public uint dirtyID;
        public RenderChainCommand firstHeadCommand, lastHeadCommand; // Sequential for the same owner
        public RenderChainCommand firstTailCommand, lastTailCommand; // Sequential for the same owner
        public bool localFlipsWinding;
        public bool localTransformScaleZero;
        public bool worldFlipsWinding;

        public ClipMethod clipMethod; // Self
        public int childrenStencilRef;
        public int childrenMaskDepth;

        public MeshHandle headMesh, tailMesh;
        public Matrix4x4 verticesSpace; // Transform describing the space which the vertices in 'data' are relative to
        public BMPAlloc transformID, clipRectID, opacityID, textCoreSettingsID;
        public BMPAlloc colorID, backgroundColorID, borderLeftColorID, borderTopColorID, borderRightColorID, borderBottomColorID, tintColorID;
        public float compositeOpacity;
        public Color backgroundColor;

        public BasicNode<TextureEntry> textures;

        public RenderChainCommand lastTailOrHeadCommand { get { return lastTailCommand ?? lastHeadCommand; } }
        public static bool AllocatesID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Owned) && alloc.IsValid(); }
        public static bool InheritsID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Inherited) && alloc.IsValid(); }

        // This is set whenever there is repaint requested when HierarchyDisplayed == false and is used to trigger the repaint when it finally get displayed
        public bool pendingRepaint;
        // This is set whenever a hierarchical repaint was needed when HierarchyDisplayed == false.
        public bool pendingHierarchicalRepaint;

        public bool isInChain
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsInChain) == RenderDataFlags.IsInChain;
        }

        public bool isGroupTransform
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => (flags & RenderDataFlags.IsGroupTransform) == RenderDataFlags.IsGroupTransform;
        }
    }

    struct TextureEntry
    {
        public Texture source;
        public TextureId actual;
        public bool replaced;
    }
}
