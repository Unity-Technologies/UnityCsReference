// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define UIR_DEBUG_CHAIN_BUILDER
using System;
using System.Collections.Generic;
using Unity.Collections;
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

    internal class RenderChain : IDisposable
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
            public NativeSlice<Transform3x4> transformConstants;
            public NativeSlice<Vector4> clipRectConstants;
        };

        RenderChainCommand m_FirstCommand;
        DepthOrderedDirtyTracking m_DirtyTracker;
        LinkedPool<RenderChainCommand> m_CommandPool = new LinkedPool<RenderChainCommand>(() => new RenderChainCommand(), cmd => {});
        BasicNodePool<TextureEntry> m_TexturePool = new BasicNodePool<TextureEntry>();
        List<RenderNodeData> m_RenderNodesData = new List<RenderNodeData>();
        Shader m_DefaultShader, m_DefaultWorldSpaceShader;
        Material m_DefaultMat, m_DefaultWorldSpaceMat;
        bool m_BlockDirtyRegistration;
        int m_StaticIndex = -1;
        int m_ActiveRenderNodes = 0;
        int m_CustomMaterialCommands = 0;
        ChainBuilderStats m_Stats;
        uint m_StatsElementsAdded, m_StatsElementsRemoved;

        TextureRegistry m_TextureRegistry = TextureRegistry.instance;

        internal RenderChainCommand firstCommand { get { return m_FirstCommand; } }
        public OpacityIdAccelerator opacityIdAccelerator { get; private set; }

        // Profiling
        static ProfilerMarker s_MarkerProcess = new ProfilerMarker("RenderChain.Process");
        static ProfilerMarker s_MarkerClipProcessing = new ProfilerMarker("RenderChain.UpdateClips");
        static ProfilerMarker s_MarkerOpacityProcessing = new ProfilerMarker("RenderChain.UpdateOpacity");
        static ProfilerMarker s_MarkerColorsProcessing = new ProfilerMarker("RenderChain.UpdateColors");
        static ProfilerMarker s_MarkerTransformProcessing = new ProfilerMarker("RenderChain.UpdateTransforms");
        static ProfilerMarker s_MarkerVisualsProcessing = new ProfilerMarker("RenderChain.UpdateVisuals");
        static ProfilerMarker s_MarkerTextRegen = new ProfilerMarker("RenderChain.RegenText");

        static RenderChain()
        {
            UIR.Utility.RegisterIntermediateRenderers += OnRegisterIntermediateRenderers;
            UIR.Utility.RenderNodeExecute += OnRenderNodeExecute;
        }

        public RenderChain(BaseVisualElementPanel panel)
        {
            Constructor(panel, new UIRenderDevice(), panel.atlas, new VectorImageManager(panel.atlas));
        }

        protected RenderChain(BaseVisualElementPanel panel, UIRenderDevice device, AtlasBase atlas, VectorImageManager vectorImageManager)
        {
            Constructor(panel, device, atlas, vectorImageManager);
        }

        void Constructor(BaseVisualElementPanel panelObj, UIRenderDevice deviceObj, AtlasBase atlas, VectorImageManager vectorImageMan)
        {
            if (disposed)
                DisposeHelper.NotifyDisposedUsed(this);

            // A reasonable starting depth level suggested here
            m_DirtyTracker.heads = new List<VisualElement>(8);
            m_DirtyTracker.tails = new List<VisualElement>(8);
            m_DirtyTracker.minDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.maxDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.Reset();

            if (m_RenderNodesData.Count < 1)
                m_RenderNodesData.Add(new RenderNodeData() { matPropBlock = new MaterialPropertyBlock() });

            this.panel = panelObj;
            this.device = deviceObj;
            this.atlas = atlas;
            this.vectorImageManager = vectorImageMan;

            // TODO: Share these across all panels
            vertsPool = new TempAllocator<Vertex>(8192, 2048, 64 * 1024);
            indicesPool = new TempAllocator<UInt16>(8192 << 1, 2048 << 1, (64 * 1024) << 1);
            jobManager = new JobManager();

            this.shaderInfoAllocator.Construct();
            this.opacityIdAccelerator = new OpacityIdAccelerator();

            painter = new Implementation.UIRStylePainter(this);

            var rp = panel as BaseRuntimePanel;
            if (rp != null && rp.drawToCameras)
            {
                drawInCameras = true;
                m_StaticIndex = RenderChainStaticIndexAllocator.AllocateIndex(this);
            }
        }

        void Destructor()
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

            vertsPool.Dispose();
            indicesPool.Dispose();
            jobManager.Dispose();
            vectorImageManager?.Dispose();
            shaderInfoAllocator.Dispose();
            device?.Dispose();
            opacityIdAccelerator?.Dispose();

            if (painter != null)
            {
                // todo: move painter2d to the render chain instead of the mgc
                if (painter.meshGenerationContext.hasPainter2D)
                {
                    painter.meshGenerationContext.painter2D.Dispose();
                }
                painter = null;
            }

            atlas = null;
            shaderInfoAllocator = new UIRVEShaderInfoAllocator();
            device = null;

            m_ActiveRenderNodes = 0;
            m_RenderNodesData.Clear();
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
                Destructor();
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        internal ChainBuilderStats stats { get { return m_Stats; } }

        internal static Action OnPreRender = null;

        public void ProcessChanges()
        {
            s_MarkerProcess.Begin();
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
            s_MarkerClipProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnClippingChanged(this, ve, m_DirtyTracker.dirtyID,
                                ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            s_MarkerClipProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Opacity;
            dirtyFlags = RenderDataDirtyTypes.Opacity | RenderDataDirtyTypes.OpacityHierarchy;
            clearDirty = ~dirtyFlags;
            s_MarkerOpacityProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnOpacityChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            s_MarkerOpacityProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Color;
            dirtyFlags = RenderDataDirtyTypes.Color;
            clearDirty = ~dirtyFlags;
            s_MarkerColorsProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnColorChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            s_MarkerColorsProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.TransformSize;
            dirtyFlags = RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.ClipRectSize;
            clearDirty = ~dirtyFlags;
            s_MarkerTransformProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnTransformOrSizeChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }
            s_MarkerTransformProcessing.End();

            jobManager.CompleteNudgeJobs();

            m_BlockDirtyRegistration = true; // Processing visuals may call generateVisualContent, which must be restricted to the allowed operations
            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Visuals;
            dirtyFlags = RenderDataDirtyTypes.AllVisuals;
            clearDirty = ~dirtyFlags;
            s_MarkerVisualsProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                VisualElement ve = m_DirtyTracker.heads[depth];
                while (ve != null)
                {
                    VisualElement veNext = ve.renderChainData.nextDirty;
                    if ((ve.renderChainData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (ve.renderChainData.isInChain && ve.renderChainData.dirtyID != m_DirtyTracker.dirtyID)
                            Implementation.RenderEvents.ProcessOnVisualsChanged(this, ve, m_DirtyTracker.dirtyID, ref m_Stats);
                        m_DirtyTracker.ClearDirty(ve, clearDirty);
                    }
                    ve = veNext;
                    m_Stats.dirtyProcessed++;
                }
            }

            jobManager.CompleteConvertMeshJobs();
            jobManager.CompleteClosingMeshJobs();

            opacityIdAccelerator.CompleteJobs();
            s_MarkerVisualsProcessing.End();
            m_BlockDirtyRegistration = false;

            vertsPool.Reset();
            indicesPool.Reset();

            // Done with all dirtied elements
            m_DirtyTracker.Reset();


            // Commit new requests for atlases if any
            atlas?.InvokeUpdateDynamicTextures(panel); // TODO: For a shared atlas + drawInCameras, postpone after all updates have occurred.
            vectorImageManager?.Commit();
            shaderInfoAllocator.IssuePendingStorageChanges();

            device?.OnFrameRenderingBegin();

            s_MarkerProcess.End();
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

                    //TODO: Reactivate this guard check once InspectorWindow is fixed to stop adding VEs during OnGUI
                    //m_BlockDirtyRegistration = true;
                    device.EvaluateChain(m_FirstCommand, standardMaterial, standardMaterial, vectorImageManager?.atlas, shaderInfoAllocator.atlas,
                        panel.scaledPixelsPerPoint, shaderInfoAllocator.transformConstants, shaderInfoAllocator.clipRectConstants,
                        m_RenderNodesData[0].matPropBlock, true, ref immediateException);
                    //m_BlockDirtyRegistration = false;
                }
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

            uint addedCount = Implementation.RenderEvents.DepthFirstOnChildAdded(this, parent, ve, index, true);
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
                Implementation.RenderEvents.DepthFirstOnChildRemoving(this, ve.hierarchy[i]);
            for (int i = 0; i < childrenCount; i++)
                Implementation.RenderEvents.DepthFirstOnChildAdded(this, ve, ve.hierarchy[i], i, false);

            UIEOnClippingChanged(ve, true);
            UIEOnOpacityChanged(ve, true);
            UIEOnVisualsChanged(ve, true);

        }

        public void UIEOnChildRemoving(VisualElement ve)
        {
            if (m_BlockDirtyRegistration)
                throw new InvalidOperationException("VisualElements cannot be removed from an active visual tree during generateVisualContent callback execution nor during visual tree rendering");


            m_StatsElementsRemoved += Implementation.RenderEvents.DepthFirstOnChildRemoving(this, ve);
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

        #endregion

        internal BaseVisualElementPanel panel { get; private set; }
        internal UIRenderDevice device { get; private set; }
        internal AtlasBase atlas { get; private set; }
        internal VectorImageManager vectorImageManager { get; private set; }
        internal TempAllocator<Vertex> vertsPool { get; private set; }
        internal TempAllocator<UInt16> indicesPool { get; private set; }
        internal JobManager jobManager { get; private set; }
        internal UIRVEShaderInfoAllocator shaderInfoAllocator; // Not a property because this is a struct we want to mutate
        internal Implementation.UIRStylePainter painter { get; private set; }
        internal bool drawStats { get; set; }
        internal bool drawInCameras { get; private set; }

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
            Exception immediateException = null;
            rnd.device.EvaluateChain(rnd.firstCommand, rnd.initialMaterial, rnd.standardMaterial,
                rnd.vectorAtlas, rnd.shaderInfoAtlas,
                rnd.dpiScale, rnd.transformConstants, rnd.clipRectConstants,
                rnd.matPropBlock, false, ref immediateException);
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
                rndSource.transformConstants = renderChain.shaderInfoAllocator.transformConstants;
                rndSource.clipRectConstants = renderChain.shaderInfoAllocator.clipRectConstants;

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
            UIR.Utility.RegisterIntermediateRenderer(camera, rnd.initialMaterial, rtp.panelToWorld,
                new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)),
                3, 0, false, sameDistanceSortPriority, (ulong)camera.cullingMask, (int)UIR.Utility.RendererCallbacks.RendererCallback_Exec,
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
            GUI.Label(rc, "Draw commands\t: " + drawStats.drawCommandCount); rc.y += y_off;
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

    internal struct RenderChainVEData
    {
        internal VisualElement prev, next; // This is a flattened view of the visual element hierarchy
        internal VisualElement groupTransformAncestor, boneTransformAncestor;
        internal VisualElement prevDirty, nextDirty; // Embedded doubly-linked list for dirty updates
        internal int hierarchyDepth; // 0 is for the root
        internal RenderDataDirtyTypes dirtiedValues;
        internal uint dirtyID;
        internal RenderChainCommand firstCommand, lastCommand; // Sequential for the same owner
        internal RenderChainCommand firstClosingCommand, lastClosingCommand; // Optional, sequential for the same owner, the presence of closing commands requires starting commands too, otherwise certain optimizations will become invalid
        internal bool isInChain;
        internal bool isHierarchyHidden;
        internal bool localFlipsWinding;
        internal bool localTransformScaleZero;
        internal bool worldFlipsWinding;

        internal Implementation.ClipMethod clipMethod; // Self
        internal int childrenStencilRef;
        internal int childrenMaskDepth;

        internal bool disableNudging;
        internal MeshHandle data, closingData;
        internal Matrix4x4 verticesSpace; // Transform describing the space which the vertices in 'data' are relative to
        internal int displacementUVStart, displacementUVEnd;
        internal BMPAlloc transformID, clipRectID, opacityID, textCoreSettingsID;
        internal BMPAlloc colorID, backgroundColorID, borderLeftColorID, borderTopColorID, borderRightColorID, borderBottomColorID, tintColorID;
        internal float compositeOpacity;
        internal Color backgroundColor;

        internal BasicNode<TextureEntry> textures;

        internal RenderChainCommand lastClosingOrLastCommand { get { return lastClosingCommand ?? lastCommand; } }
        static internal bool AllocatesID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Owned) && alloc.IsValid(); }
        static internal bool InheritsID(BMPAlloc alloc) { return (alloc.ownedState == OwnedState.Inherited) && alloc.IsValid(); }
    }

    struct TextureEntry
    {
        public Texture source;
        public TextureId actual;
        public bool replaced;
    }
}
