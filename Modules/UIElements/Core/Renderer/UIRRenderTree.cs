// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    struct DepthOrderedDirtyTracking // Depth then register-time order
    {
        public RenderTree owner;

        // For each depth level, we keep a double linked list of VisualElements that are dirty for some reason.
        // The actual reason is stored in renderChainData.dirtiedValues.
        public List<RenderData> heads, tails; // Indexed by VE hierarchy depth

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

        public void RegisterDirty(RenderData renderData, RenderDataDirtyTypes dirtyTypes, RenderDataDirtyTypeClasses dirtyTypeClass)
        {
            Debug.Assert(renderData.renderTree == owner);
            Debug.Assert(dirtyTypes != 0);

            int depth = renderData.depthInRenderTree;
            int dirtyTypeClassIndex = (int)dirtyTypeClass;
            minDepths[dirtyTypeClassIndex] = depth < minDepths[dirtyTypeClassIndex] ? depth : minDepths[dirtyTypeClassIndex];
            maxDepths[dirtyTypeClassIndex] = depth > maxDepths[dirtyTypeClassIndex] ? depth : maxDepths[dirtyTypeClassIndex];
            if (renderData.dirtiedValues != 0)
            {
                renderData.dirtiedValues |= dirtyTypes;
                return;
            }

            renderData.dirtiedValues = dirtyTypes;
            if (tails[depth] != null)
            {
                tails[depth].nextDirty = renderData;
                renderData.prevDirty = tails[depth];
                tails[depth] = renderData;
            }
            else heads[depth] = tails[depth] = renderData;
        }

        public void ClearDirty(RenderData renderData, RenderDataDirtyTypes dirtyTypesInverse)
        {
            Debug.Assert(renderData.dirtiedValues != 0);
            renderData.dirtiedValues &= dirtyTypesInverse;
            if (renderData.dirtiedValues == 0)
            {
                // Mend the chain
                if (renderData.prevDirty != null)
                    renderData.prevDirty.nextDirty = renderData.nextDirty;
                if (renderData.nextDirty != null)
                    renderData.nextDirty.prevDirty = renderData.prevDirty;
                if (tails[renderData.depthInRenderTree] == renderData)
                {
                    Debug.Assert(renderData.nextDirty == null);
                    tails[renderData.depthInRenderTree] = renderData.prevDirty;
                }
                if (heads[renderData.depthInRenderTree] == renderData)
                {
                    Debug.Assert(renderData.prevDirty == null);
                    heads[renderData.depthInRenderTree] = renderData.nextDirty;
                }
                renderData.prevDirty = renderData.nextDirty = null;
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

    class RenderTree
    {
        RenderTreeManager m_RenderTreeManager;
        DepthOrderedDirtyTracking m_DirtyTracker;
        RenderChainCommand m_FirstCommand; // Not necessarily the root command, which may not create any commands
        RenderData m_RootRenderData;

        public TextureId quadTextureId;
        public RectInt quadRect;

        internal RenderTreeManager renderTreeManager => m_RenderTreeManager;
        internal RenderData rootRenderData => m_RootRenderData;

        internal RenderTree parent;
        internal RenderTree firstChild;
        internal RenderTree nextSibling;

        internal ref DepthOrderedDirtyTracking dirtyTracker { get { return ref m_DirtyTracker; } }
        internal RenderChainCommand firstCommand { get { return m_FirstCommand; } }

        internal bool isRootRenderTree
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // TODO: Use new "parent" field
                return rootRenderData.owner.parent == null && !rootRenderData.isNestedRenderTreeRoot;
            }
        }

        static readonly ProfilerMarker k_MarkerClipProcessing = new("RenderTree.UpdateClips");
        static readonly ProfilerMarker k_MarkerOpacityProcessing = new("RenderTree.UpdateOpacity");
        static readonly ProfilerMarker k_MarkerColorsProcessing = new("RenderTree.UpdateColors");
        static readonly ProfilerMarker k_MarkerTransformProcessing = new("RenderTree.UpdateTransforms");
        static readonly ProfilerMarker k_MarkerVisualsProcessing = new("RenderTree.UpdateVisuals");

        public void Init(RenderTreeManager renderTreeManager, RenderData rootRenderData)
        {
            m_RenderTreeManager = renderTreeManager;
            m_RootRenderData = rootRenderData;
            m_DirtyTracker.owner = this;

            quadTextureId = TextureId.invalid;

            parent = null;
            firstChild = null;
            nextSibling = null;

            // A reasonable starting depth level suggested here
            m_DirtyTracker.heads = new List<RenderData>(8);
            m_DirtyTracker.tails = new List<RenderData>(8);
            m_DirtyTracker.minDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.maxDepths = new int[(int)RenderDataDirtyTypeClasses.Count];
            m_DirtyTracker.Reset();
        }

        public void Dispose()
        {
            if (m_RootRenderData != null)
                DepthFirstResetTextures(m_RootRenderData);
        }

        // Iterates on render data (caller performs null check)
        void DepthFirstResetTextures(RenderData renderData)
        {
            // Work
            m_RenderTreeManager.ResetTextures(renderData);

            // Recurse
            RenderData child = renderData.firstChild;
            while (child != null)
            {
                DepthFirstResetTextures(child);
                child = child.nextSibling;
            }
        }

        [Flags]
        internal enum AllowedClasses
        {
            Clipping      = 1 << 0,
            Opacity       = 1 << 1,
            Color         = 1 << 2,
            TransformSize = 1 << 3,
            Visuals       = 1 << 4,
            All = Clipping | Opacity | Color | TransformSize | Visuals
        }

        AllowedClasses m_AllowedDirtyClasses = AllowedClasses.All;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataClippingChanged(RenderData renderData, bool hierarchical)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Clipping) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Clipping | (hierarchical ? RenderDataDirtyTypes.ClippingHierarchy : 0), RenderDataDirtyTypeClasses.Clipping);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataOpacityChanged(RenderData renderData, bool hierarchical = false)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Opacity) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Opacity | (hierarchical ? RenderDataDirtyTypes.OpacityHierarchy : 0), RenderDataDirtyTypeClasses.Opacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataColorChanged(RenderData renderData)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Color) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Color, RenderDataDirtyTypeClasses.Color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataTransformOrSizeChanged(RenderData renderData, bool transformChanged, bool clipRectSizeChanged)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.TransformSize) != 0);
            RenderDataDirtyTypes flags =
                (transformChanged ? RenderDataDirtyTypes.Transform : RenderDataDirtyTypes.None) |
                (clipRectSizeChanged ? RenderDataDirtyTypes.ClipRectSize : RenderDataDirtyTypes.None);
            m_DirtyTracker.RegisterDirty(renderData, flags, RenderDataDirtyTypeClasses.TransformSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataOpacityIdChanged(RenderData renderData)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Visuals) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.VisualsOpacityId, RenderDataDirtyTypeClasses.Visuals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRenderDataVisualsChanged(RenderData renderData, bool hierarchical)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Visuals) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Visuals | (hierarchical ? RenderDataDirtyTypes.VisualsHierarchy : 0), RenderDataDirtyTypeClasses.Visuals);
        }

        public void ProcessChanges(ref ChainBuilderStats stats)
        {
            int dirtyClass;
            RenderDataDirtyTypes dirtyFlags;
            RenderDataDirtyTypes clearDirty;

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Clipping;
            dirtyFlags = RenderDataDirtyTypes.Clipping | RenderDataDirtyTypes.ClippingHierarchy;
            clearDirty = ~dirtyFlags;
            m_AllowedDirtyClasses &= ~AllowedClasses.Clipping;
            k_MarkerClipProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                var renderData = m_DirtyTracker.heads[depth];
                while (renderData != null)
                {
                    var nextRenderData = renderData.nextDirty;
                    if ((renderData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (renderData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnClippingChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                        m_DirtyTracker.ClearDirty(renderData, clearDirty);
                    }
                    renderData = nextRenderData;
                    stats.dirtyProcessed++;
                }
            }
            k_MarkerClipProcessing.End();


            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Opacity;
            dirtyFlags = RenderDataDirtyTypes.Opacity | RenderDataDirtyTypes.OpacityHierarchy;
            clearDirty = ~dirtyFlags;
            m_AllowedDirtyClasses &= ~AllowedClasses.Opacity;
            k_MarkerOpacityProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                var renderData = m_DirtyTracker.heads[depth];
                while (renderData != null)
                {
                    var nextRenderData = renderData.nextDirty;
                    if ((renderData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (renderData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnOpacityChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                        m_DirtyTracker.ClearDirty(renderData, clearDirty);
                    }
                    renderData = nextRenderData;
                    stats.dirtyProcessed++;
                }
            }
            k_MarkerOpacityProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Color;
            dirtyFlags = RenderDataDirtyTypes.Color;
            clearDirty = ~dirtyFlags;
            m_AllowedDirtyClasses &= ~AllowedClasses.Color;
            k_MarkerColorsProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                var renderData = m_DirtyTracker.heads[depth];
                while (renderData != null)
                {
                    var nextRenderData = renderData.nextDirty;
                    if ((renderData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (renderData != null && renderData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnColorChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                        m_DirtyTracker.ClearDirty(renderData, clearDirty);
                    }
                    renderData = nextRenderData;
                    stats.dirtyProcessed++;
                }
            }
            k_MarkerColorsProcessing.End();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.TransformSize;
            dirtyFlags = RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.ClipRectSize;
            clearDirty = ~dirtyFlags;
            m_AllowedDirtyClasses &= ~AllowedClasses.TransformSize;
            k_MarkerTransformProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                var renderData = m_DirtyTracker.heads[depth];
                while (renderData != null)
                {
                    var nextRenderData = renderData.nextDirty;
                    if ((renderData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (renderData.dirtyID != m_DirtyTracker.dirtyID)
                            RenderEvents.ProcessOnTransformOrSizeChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                        m_DirtyTracker.ClearDirty(renderData, clearDirty);
                    }
                    renderData = nextRenderData;
                    stats.dirtyProcessed++;
                }
            }
            k_MarkerTransformProcessing.End();

            m_RenderTreeManager.jobManager.CompleteNudgeJobs();

            m_DirtyTracker.dirtyID++;
            dirtyClass = (int)RenderDataDirtyTypeClasses.Visuals;
            dirtyFlags = RenderDataDirtyTypes.AllVisuals;
            clearDirty = ~dirtyFlags;
            m_AllowedDirtyClasses &= ~AllowedClasses.Visuals;
            k_MarkerVisualsProcessing.Begin();
            for (int depth = m_DirtyTracker.minDepths[dirtyClass]; depth <= m_DirtyTracker.maxDepths[dirtyClass]; depth++)
            {
                var renderData = m_DirtyTracker.heads[depth];
                while (renderData != null)
                {
                    var nextRenderData = renderData.nextDirty;
                    if ((renderData.dirtiedValues & dirtyFlags) != 0)
                    {
                        if (renderData.dirtyID != m_DirtyTracker.dirtyID)
                            m_RenderTreeManager.visualChangesProcessor.ProcessOnVisualsChanged(renderData, m_DirtyTracker.dirtyID, ref stats);
                        m_DirtyTracker.ClearDirty(renderData, clearDirty);
                    }
                    renderData = nextRenderData;
                    stats.dirtyProcessed++;
                }
            }

            m_RenderTreeManager.meshGenerationDeferrer.ProcessDeferredWork(m_RenderTreeManager.visualChangesProcessor.meshGenerationContext);

            // Mesh Generation doesn't currently support multiple rounds of generation, so we must flush all deferred
            // work and then schedule the MeshGenerationJobs (and process it's associated callback). Once we make it
            // support multiple rounds, we should move the following call above ProcessDeferredWork and get rid of the
            // second call to ProcessDeferredWork.
            m_RenderTreeManager.visualChangesProcessor.ScheduleMeshGenerationJobs();
            m_RenderTreeManager.meshGenerationDeferrer.ProcessDeferredWork(m_RenderTreeManager.visualChangesProcessor.meshGenerationContext);

            // TODO: Consider postponing this work for later, after each subtrees have been processed.
            // This will help with parallelism.
            m_RenderTreeManager.visualChangesProcessor.ConvertEntriesToCommands(ref stats);

            m_RenderTreeManager.jobManager.CompleteConvertMeshJobs();
            m_RenderTreeManager.jobManager.CompleteCopyMeshJobs();
            m_RenderTreeManager.opacityIdAccelerator.CompleteJobs();
            k_MarkerVisualsProcessing.End();

            // Done with all dirtied elements
            m_DirtyTracker.Reset();

            m_AllowedDirtyClasses = AllowedClasses.All;
        }

        internal void OnRenderCommandAdded(RenderChainCommand command)
        {
            if (command.prev == null)
                m_FirstCommand = command;
        }

        internal void OnRenderCommandsRemoved(RenderChainCommand firstCommand, RenderChainCommand lastCommand)
        {
            if (firstCommand.prev == null)
                m_FirstCommand = lastCommand.next;
        }

        internal void ChildWillBeRemoved(RenderData renderData)
        {
            if (renderData.dirtiedValues != 0)
                m_DirtyTracker.ClearDirty(renderData, ~renderData.dirtiedValues);
            Debug.Assert(renderData.dirtiedValues == 0);
            Debug.Assert(renderData.prevDirty == null);
            Debug.Assert(renderData.nextDirty == null);
        }
    }
}
