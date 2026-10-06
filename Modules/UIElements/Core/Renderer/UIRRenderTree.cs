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
    // Elements dirty for a class are found by walking the render tree from the root, guided by the
    // per-class subtree-dirty bits this tracker sets on their ancestors.
    struct RenderTreeDirtyTracker
    {
        public RenderTree owner;

        public uint dirtyID; // A monotonically increasing ID used to avoid double processing of some elements

        // The bit an element carries when one of its descendants is dirty for this class.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderDataFlags ConvertDirtyClassToFlags(RenderDataDirtyTypeClasses dirtyTypeClass)
        {
            return (RenderDataFlags)((int)RenderDataFlags.SubtreeDirtyClipping << (int)dirtyTypeClass);
        }

        public void RegisterDirty(RenderData renderData, RenderDataDirtyTypes dirtyTypes, RenderDataDirtyTypeClasses dirtyTypeClass)
        {
            Debug.Assert(renderData.renderTree == owner);
            Debug.Assert(dirtyTypes != 0);

            renderData.dirtiedValues |= dirtyTypes;

            // Mark ancestors dirty
            RenderDataFlags dirtyFlags = ConvertDirtyClassToFlags(dirtyTypeClass);
            for (RenderData node = renderData; node.parent != null; node = node.parent)
            {
                RenderData parent = node.parent;

                if (ChildrenDirtyTracker.NeedsTracking(parent, node))
                    ChildrenDirtyTracker.AddDirtyChild(parent, node);

                if ((parent.flags & dirtyFlags) != 0)
                    break;

                parent.flags |= dirtyFlags;
            }
        }

        public void ClearDirty(RenderData renderData, RenderDataDirtyTypes dirtyTypesInverse)
        {
            Debug.Assert(renderData.dirtiedValues != 0);
            renderData.dirtiedValues &= dirtyTypesInverse;
        }
    }

    class RenderTree
    {
        RenderTreeManager m_RenderTreeManager;
        RenderTreeDirtyTracker m_DirtyTracker;
        RenderChainCommand m_FirstCommand; // Not necessarily the root command, which may not create any commands
        RenderData m_RootRenderData;

        // Active backdrop-filters in this tree; iterated when a group transform moves to re-tessellate the
        // ones nested under it (their UVs track the world transform). Maintained like the panel count. UI-5170.
        HashSet<RenderData> m_BackdropFilterRenderDatas;

        public TextureId quadTextureId;
        public Rect quadRect;
        public Rect quadUVRect;
        // Gamma-encoded quad (force-gamma); the parent samples it without re-encoding (UI-5094).
        public bool quadIsGammaEncoded;

        public GCHandlePool m_GCHandlePool = new();

        internal RenderTreeManager renderTreeManager => m_RenderTreeManager;
        internal RenderData rootRenderData => m_RootRenderData;

        internal RenderTree parent;
        internal RenderTree firstChild;
        internal RenderTree nextSibling;

        internal ref RenderTreeDirtyTracker dirtyTracker { get { return ref m_DirtyTracker; } }
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

        static readonly ProfilerMarker k_MarkerClipProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateClips");
        static readonly ProfilerMarker k_MarkerOpacityProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateOpacity");
        static readonly ProfilerMarker k_MarkerColorsProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateColors");
        static readonly ProfilerMarker k_MarkerTransformProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateTransforms");
        static readonly ProfilerMarker k_MarkerVisualsProcessing = new(ProfilerCategory.UIToolkit, "RenderTree.UpdateVisuals");

        public void Init(RenderTreeManager renderTreeManager, RenderData rootRenderData)
        {
            m_RenderTreeManager = renderTreeManager;
            m_RootRenderData = rootRenderData;
            m_DirtyTracker.owner = this;

            quadTextureId = TextureId.invalid;
            quadIsGammaEncoded = false;

            parent = null;
            firstChild = null;
            nextSibling = null;

            m_BackdropFilterRenderDatas ??= new HashSet<RenderData>(); // reused across pooled acquires (Reset clears it)
        }

        public void Reset()
        {
            m_RenderTreeManager = null;
            m_RootRenderData = null;
            parent = null;
            firstChild = null;
            nextSibling = null;

            m_BackdropFilterRenderDatas?.Clear();
        }

        public void Dispose()
        {
            if (m_RootRenderData != null)
                DepthFirstResetTextures(m_RootRenderData);
        }

        // Iterates on render data (caller performs null check)
        void DepthFirstResetTextures(RenderData renderData)
        {
            m_GCHandlePool.ReturnAll();

            // Work
            m_RenderTreeManager.ResetGraphicEntries(renderData);
            BackdropFilterHelper.ReleaseBackdropFilterResources(m_RenderTreeManager, renderData);

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

        // Nothing can be dirtied for a class while it is processed, which the walks rely on: ProcessChanges removes the
        // class from here before its walk, and changes from outside the renderer throw (m_BlockDirtyRegistration).
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
        public void OnRenderDataVisualsChanged(RenderData renderData, bool hierarchical)
        {
            Debug.Assert((m_AllowedDirtyClasses & AllowedClasses.Visuals) != 0);
            m_DirtyTracker.RegisterDirty(renderData, RenderDataDirtyTypes.Visuals | (hierarchical ? RenderDataDirtyTypes.VisualsHierarchy : 0), RenderDataDirtyTypeClasses.Visuals);
        }

        public void RegisterBackdropFilter(RenderData renderData)
        {
            m_BackdropFilterRenderDatas.Add(renderData);
        }

        public void UnregisterBackdropFilter(RenderData renderData)
        {
            m_BackdropFilterRenderDatas.Remove(renderData);
        }

        // Re-record the backdrop-filters at or under this element, whose recorded rect bakes in its clip.
        // Testing each registered entry's parent chain is cheaper than walking the subtree.
        public void RefreshBackdropFilterDescendantsOf(RenderData ancestor)
        {
            if (m_BackdropFilterRenderDatas.Count == 0)
                return;

            foreach (RenderData rd in m_BackdropFilterRenderDatas)
            {
                for (RenderData p = rd; p != null; p = p.parent)
                {
                    if (p != ancestor)
                        continue;

                    // Skip if already scheduled to regenerate this pass.
                    if ((rd.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0)
                        OnRenderDataVisualsChanged(rd, false);
                    break;
                }
            }
        }

        // Re-tessellate the backdrop-filters nested under the moved group (their world-derived UVs went stale).
        // Testing each registered entry's group-ancestor chain is cheaper than walking the subtree. UI-5170.
        public void RefreshBackdropFilterDescendantsOfGroup(RenderData group)
        {
            foreach (RenderData rd in m_BackdropFilterRenderDatas)
            {
                for (RenderData g = rd.groupTransformAncestor; g != null; g = g.groupTransformAncestor)
                {
                    if (g != group)
                        continue;

                    // Skip if already scheduled to regenerate this pass.
                    if ((rd.dirtiedValues & (RenderDataDirtyTypes.Visuals | RenderDataDirtyTypes.VisualsHierarchy)) == 0)
                        OnRenderDataVisualsChanged(rd, false);
                    break;
                }
            }
        }

        // Walks the elements dirty for one class in draw order (UUM-154188)
        void ProcessDirtyClass(RenderDataDirtyTypeClasses dirtyClass, RenderDataDirtyTypes dirtyFlags, ref ChainBuilderStats stats)
        {
            DepthFirstProcessDirty(m_RootRenderData, dirtyClass, dirtyFlags, RenderTreeDirtyTracker.ConvertDirtyClassToFlags(dirtyClass), ref stats);
        }

        void DepthFirstProcessDirty(RenderData renderData, RenderDataDirtyTypeClasses dirtyClass, RenderDataDirtyTypes dirtyFlags, RenderDataFlags subtreeDirty, ref ChainBuilderStats stats)
        {
            if ((renderData.dirtiedValues & dirtyFlags) != 0)
            {
                if (renderData.dirtyID != m_DirtyTracker.dirtyID)
                {
                    switch (dirtyClass)
                    {
                        case RenderDataDirtyTypeClasses.Clipping:
                            RenderEvents.ProcessOnClippingChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.Opacity:
                            RenderEvents.ProcessOnOpacityChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.Color:
                            RenderEvents.ProcessOnColorChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.TransformSize:
                            RenderEvents.ProcessOnTransformOrSizeChanged(m_RenderTreeManager, renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                        case RenderDataDirtyTypeClasses.Visuals:
                            m_RenderTreeManager.visualChangesProcessor.ProcessOnVisualsChanged(renderData, m_DirtyTracker.dirtyID, ref stats);
                            break;
                    }
                }
                m_DirtyTracker.ClearDirty(renderData, ~dirtyFlags);
                stats.dirtyProcessed++;
            }

            if ((renderData.flags & subtreeDirty) == 0)
                return;
            renderData.flags &= ~subtreeDirty;

            if (ChildrenDirtyTracker.TryIterateTracked(renderData, out var trackedChildren))
            {
                while (trackedChildren.MoveNext())
                {
                    RenderData child = trackedChildren.current;
                    if ((child.dirtiedValues & dirtyFlags) != 0 || (child.flags & subtreeDirty) != 0)
                        DepthFirstProcessDirty(child, dirtyClass, dirtyFlags, subtreeDirty, ref stats);
                }
                trackedChildren.End();
            }
            else
            {
                var siblings = ChildrenDirtyTracker.BeginSiblingWalk(renderData);
                for (RenderData child = renderData.firstChild; child != null; child = child.nextSibling)
                {
                    siblings.Visit(child);
                    if ((child.dirtiedValues & dirtyFlags) != 0 || (child.flags & subtreeDirty) != 0)
                        DepthFirstProcessDirty(child, dirtyClass, dirtyFlags, subtreeDirty, ref stats);
                }
                siblings.End();
            }
        }

        public void ProcessChanges(ref ChainBuilderStats stats)
        {
            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Clipping;
            using (k_MarkerClipProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Clipping, RenderDataDirtyTypes.Clipping | RenderDataDirtyTypes.ClippingHierarchy, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Opacity;
            using (k_MarkerOpacityProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Opacity, RenderDataDirtyTypes.Opacity | RenderDataDirtyTypes.OpacityHierarchy, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Color;
            using (k_MarkerColorsProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Color, RenderDataDirtyTypes.Color, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.TransformSize;
            using (k_MarkerTransformProcessing.Auto())
                ProcessDirtyClass(RenderDataDirtyTypeClasses.TransformSize, RenderDataDirtyTypes.Transform | RenderDataDirtyTypes.ClipRectSize, ref stats);

            m_DirtyTracker.dirtyID++;
            m_AllowedDirtyClasses &= ~AllowedClasses.Visuals;
            using (k_MarkerVisualsProcessing.Auto())
            {
                ProcessDirtyClass(RenderDataDirtyTypeClasses.Visuals, RenderDataDirtyTypes.AllVisuals, ref stats);

                m_RenderTreeManager.meshGenerationDeferrer.ProcessDeferredWork(m_RenderTreeManager.visualChangesProcessor.meshGenerationContext);

                // Mesh Generation doesn't currently support multiple rounds of generation, so we must flush all deferred
                // work and then schedule the MeshGenerationJobs (and process it's associated callback). Once we make it
                // support multiple rounds, we should move the following call above ProcessDeferredWork and get rid of the
                // second call to ProcessDeferredWork.
                m_RenderTreeManager.visualChangesProcessor.ScheduleMeshGenerationJobs();
                m_RenderTreeManager.meshGenerationDeferrer.ProcessDeferredWork(m_RenderTreeManager.visualChangesProcessor.meshGenerationContext);

                m_RenderTreeManager.visualChangesProcessor.RunMeshModifiers();

                // TODO: Consider postponing this work for later, after each subtrees have been processed.
                // This will help with parallelism.
                m_RenderTreeManager.visualChangesProcessor.ConvertEntriesToCommands(ref stats);

                m_RenderTreeManager.jobManager.CompleteConvertMeshJobs();
                m_RenderTreeManager.jobManager.CompleteCopyMeshJobs();
            }

            m_RenderTreeManager.UpdateElementInfoRecords();

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
    }
}
