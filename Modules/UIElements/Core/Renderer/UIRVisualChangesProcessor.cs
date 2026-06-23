// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    partial class RenderTreeManager
    {
        internal class VisualChangesProcessor : IDisposable
        {
            enum VisualsProcessingType
            {
                Head,
                Tail
            }

            struct EntryProcessingInfo
            {
                public RenderData renderData;
                public VisualsProcessingType type;
                public Entry rootEntry;
            }

            static readonly ProfilerMarker k_GenerateEntriesMarker = new(ProfilerCategory.UIToolkit, "UIR.GenerateEntries");
            static readonly ProfilerMarker k_ConvertEntriesToCommandsMarker = new(ProfilerCategory.UIToolkit, "UIR.ConvertEntriesToCommands");
            static readonly ProfilerMarker k_MeshModifierMarker = new(ProfilerCategory.UIToolkit, "UIR.MeshModificationCallback");
            static readonly ProfilerMarker k_UpdateOpacityIdMarker = new (ProfilerCategory.UIToolkit, "UIR.UpdateOpacityId");

            RenderTreeManager m_RenderTreeManager;
            MeshGenerationContext m_MeshGenerationContext;
            BaseElementBuilder m_ElementBuilder;
            List<EntryProcessingInfo> m_EntryProcessingList;
            List<EntryProcessingInfo> m_ModifierEntryProcessingList; // Subset of m_EntryProcessingList (head entries with modifiers to execute)
            List<EntryProcessor> m_Processors;
            MeshModifierScheduler m_MeshModifierScheduler;
            readonly MeshModifierChainCache m_ChainCache = new();

            public BaseElementBuilder elementBuilder => m_ElementBuilder;
            public MeshGenerationContext meshGenerationContext => m_MeshGenerationContext;

            public VisualChangesProcessor(RenderTreeManager renderTreeManager)
            {
                m_RenderTreeManager = renderTreeManager;
                m_MeshGenerationContext = new MeshGenerationContext(
                    m_RenderTreeManager.meshWriteDataPool,
                    m_RenderTreeManager.entryRecorder,
                    m_RenderTreeManager.tempMeshAllocator,
                    m_RenderTreeManager.meshGenerationDeferrer,
                    m_RenderTreeManager.meshGenerationNodeManager);
                m_ElementBuilder = new DefaultElementBuilder(m_RenderTreeManager);
                m_EntryProcessingList = new List<EntryProcessingInfo>();
                m_ModifierEntryProcessingList = new List<EntryProcessingInfo>();
                m_Processors = new List<EntryProcessor>(4);
                m_MeshModifierScheduler = new MeshModifierScheduler();
            }

            public void ScheduleMeshGenerationJobs()
            {
                m_ElementBuilder.ScheduleMeshGenerationJobs(m_MeshGenerationContext);
            }

            public void ProcessOnVisualsChanged(RenderData renderData, uint dirtyID, ref ChainBuilderStats stats)
            {
                bool hierarchical = renderData.pendingHierarchicalRepaint || (renderData.dirtiedValues & RenderDataDirtyTypes.VisualsHierarchy) != 0;
                if (hierarchical)
                    stats.recursiveVisualUpdates++;
                else
                    stats.nonRecursiveVisualUpdates++;

                List<MeshModifierRegistration> inheritedRecursive;
                if (renderData.parent != null)
                    inheritedRecursive = DeriveChildModifiers(renderData.parent);
                else if (renderData.isNestedRenderTreeRoot)
                    inheritedRecursive = DeriveOuterInherited(renderData.owner.renderData);
                else
                    inheritedRecursive = null;

                DepthFirstOnVisualsChanged(renderData, dirtyID, hierarchical, inheritedRecursive, ref stats);
            }

            void DepthFirstOnVisualsChanged(RenderData renderData, uint dirtyID, bool hierarchical,
                List<MeshModifierRegistration> inheritedRecursive, ref ChainBuilderStats stats)
            {
                if (dirtyID == renderData.dirtyID)
                    return;
                renderData.dirtyID = dirtyID; // Prevent reprocessing of the same element in the same pass

                if (hierarchical)
                    stats.recursiveVisualUpdatesExpanded++;

                if (!renderData.owner.areAncestorsAndSelfDisplayed)
                {
                    if (hierarchical)
                        renderData.pendingHierarchicalRepaint = true;
                    else
                        renderData.pendingRepaint = true;
                    return;
                }

                renderData.pendingHierarchicalRepaint = false;
                renderData.pendingRepaint = false;

                if (!hierarchical && (renderData.dirtiedValues & RenderDataDirtyTypes.AllVisuals) == RenderDataDirtyTypes.VisualsOpacityId)
                {
                    stats.opacityIdUpdates++;
                    UpdateOpacityId(renderData, m_RenderTreeManager);
                    return;
                }

                UpdateWorldFlipsWinding(renderData);

                Debug.Assert(renderData.clipMethod != ClipMethod.Undetermined);
                Debug.Assert(RenderData.AllocatesID(renderData.transformID) || renderData.parent == null || renderData.transformID.Equals(renderData.parent.transformID) || renderData.isGroupTransform);

                if (renderData.owner is TextElement te)
                    RenderEvents.UpdateTextCoreSettings(m_RenderTreeManager, te);

                if ((renderData.owner.renderHints & RenderHints.DynamicColor) == RenderHints.DynamicColor)
                    RenderEvents.SetColorValues(m_RenderTreeManager, renderData.owner);

                RenderEvents.SyncBackdropFilterState(m_RenderTreeManager, renderData);

                bool effectiveModifiersChanged = RebuildEffectiveModifiers(renderData, inheritedRecursive);

                var rootEntry = m_RenderTreeManager.entryPool.Get();
                rootEntry.type = EntryType.DedicatedPlaceholder;

                var headInfo = new EntryProcessingInfo
                {
                    type = VisualsProcessingType.Head,
                    renderData = renderData,
                    rootEntry = rootEntry
                };
                m_EntryProcessingList.Add(headInfo);
                if (!renderData.isSubTreeQuad
                    && renderData.m_EffectiveModifiers != null
                    && renderData.m_EffectiveModifiers.Count > 0)
                {
                    m_ModifierEntryProcessingList.Add(headInfo);
                }

                using (k_GenerateEntriesMarker.Auto())
                {
                    m_MeshGenerationContext.Begin(rootEntry, renderData.owner, renderData);
                    m_ElementBuilder.Build(m_MeshGenerationContext);
                    m_MeshGenerationContext.End();
                }

                if (hierarchical)
                {
                    var firstChild = renderData.firstChild;
                    if (firstChild != null)
                    {
                        var childModifiers = DeriveChildModifiers(renderData);
                        for (var child = firstChild; child != null; child = child.nextSibling)
                            DepthFirstOnVisualsChanged(child, dirtyID, true, childModifiers, ref stats);
                    }

                    // When modifiers change, we need to dirty the nested render tree as well.
                    if (effectiveModifiersChanged && renderData.isSubTreeQuad)
                    {
                        var nested = renderData.owner.nestedRenderData;
                        nested.renderTree.OnRenderDataVisualsChanged(nested, hierarchical: true);
                    }
                }

                m_EntryProcessingList.Add(new EntryProcessingInfo
                {
                    type = VisualsProcessingType.Tail,
                    renderData = renderData,
                    rootEntry = rootEntry
                });
            }

            static readonly List<MeshModifierRegistration> k_RebuildScratch = new(16);

            // Returns true when m_EffectiveModifiers's reference flipped. The chain cache guarantees that
            // identical content maps to the same List instance, so descendants' ReferenceEquals
            // propagation stays correct without a per-rd stamp.
            bool RebuildEffectiveModifiers(RenderData rd, List<MeshModifierRegistration> inheritedRecursive)
            {
                var own = rd.owner.m_MeshModifiers;
                int ownCount = own?.Count ?? 0;
                int inheritedCount = inheritedRecursive?.Count ?? 0;
                var oldChain = rd.m_EffectiveModifiers;

                if (ownCount == 0)
                {
                    var newChain = inheritedCount == 0 ? null : inheritedRecursive;
                    if (ReferenceEquals(oldChain, newChain))
                        return false;
                    m_ChainCache.Acquire(newChain);
                    m_ChainCache.Release(oldChain);
                    rd.m_EffectiveModifiers = newChain;
                    return true;
                }

                var scratch = k_RebuildScratch;
                if (inheritedCount > 0)
                    scratch.AddRange(inheritedRecursive);
                scratch.AddRange(own);
                if (scratch.Count > 1)
                    scratch.Sort(MeshModifierRegistration.s_Comparer);

                var shared = m_ChainCache.GetShared(scratch);
                scratch.Clear();
                if (ReferenceEquals(oldChain, shared))
                    return false;
                m_ChainCache.Acquire(shared);
                m_ChainCache.Release(oldChain);
                rd.m_EffectiveModifiers = shared;
                return true;
            }

            public void ReleaseChainRef(List<MeshModifierRegistration> chain) => m_ChainCache.Release(chain);

            public void PruneChainCache() => m_ChainCache.Prune();

            List<MeshModifierRegistration> DeriveChildModifiers(RenderData rd)
            {
                var own = rd.owner.m_MeshModifiers;
                int ownCount = own?.Count ?? 0;
                if (ownCount == 0)
                    return rd.m_EffectiveModifiers;

                int ownRecursive = 0;
                bool hasNonRecursive = false;
                for (int i = 0; i < ownCount; ++i)
                {
                    if (own[i].recursive)
                        ++ownRecursive;
                    else
                        hasNonRecursive = true;
                }

                if (!hasNonRecursive)
                    return rd.m_EffectiveModifiers;

                SubtractOwnIntoScratch(rd.m_EffectiveModifiers, own, includeRecursiveOwn: ownRecursive > 0);
                var shared = m_ChainCache.GetShared(k_RebuildScratch);
                k_RebuildScratch.Clear();
                return shared;
            }

            // Q.renderData.m_EffectiveModifiers folds Q.own in; the nested-root seed must subtract it
            // back out, otherwise Q.own.recursive ends up double-counted when the nested root rebuilds.
            List<MeshModifierRegistration> DeriveOuterInherited(RenderData outerRd)
            {
                var own = outerRd.owner.m_MeshModifiers;
                if (own == null || own.Count == 0)
                    return outerRd.m_EffectiveModifiers;
                SubtractOwnIntoScratch(outerRd.m_EffectiveModifiers, own, includeRecursiveOwn: false);
                var shared = m_ChainCache.GetShared(k_RebuildScratch);
                k_RebuildScratch.Clear();
                return shared;
            }

            // Identity is by id — unique per registration via UIRUtility.GetNextMeshModifierId.
            static void SubtractOwnIntoScratch(
                List<MeshModifierRegistration> effective,
                List<MeshModifierRegistration> own,
                bool includeRecursiveOwn)
            {
                if (effective == null)
                    return;
                for (int i = 0; i < effective.Count; ++i)
                {
                    var e = effective[i];
                    bool drop = false;
                    for (int j = 0; j < own.Count; ++j)
                    {
                        if (own[j].id == e.id)
                        {
                            drop = !(includeRecursiveOwn && own[j].recursive);
                            break;
                        }
                    }
                    if (!drop)
                        k_RebuildScratch.Add(e);
                }
            }

            // This can only be called when the element local and the parent world states are clean.
            static void UpdateWorldFlipsWinding(RenderData renderData)
            {
                bool flipsWinding = renderData.localFlipsWinding;
                bool parentFlipsWinding = renderData.parent?.worldFlipsWinding ?? false;
                renderData.worldFlipsWinding = parentFlipsWinding ^ flipsWinding;
            }

            public void RunMeshModifiers()
            {
                if (m_ModifierEntryProcessingList.Count == 0)
                    return;

                using (k_MeshModifierMarker.Auto())
                {
                    for (int i = 0; i < m_ModifierEntryProcessingList.Count; ++i)
                    {
                        var info = m_ModifierEntryProcessingList[i];
                        m_MeshModifierScheduler.RegisterDirtyElement(info.rootEntry, info.renderData);
                    }
                    var panelExtras = m_RenderTreeManager.device?.extraVertexChannels ?? ExtraVertexChannels.None;
                    m_MeshModifierScheduler.Run(m_RenderTreeManager.tempMeshAllocator, panelExtras);
                }
            }

            public void ConvertEntriesToCommands(ref ChainBuilderStats stats)
            {
                using (k_ConvertEntriesToCommandsMarker.Auto())
                {

                    // The depth from the VE that triggered a recursive visuals update. Not necessarily equal
                    // to the depth of the VE in the hierarchy.
                    int depth = 0;
                    for (int i = 0; i < m_EntryProcessingList.Count; ++i)
                    {
                        var processingInfo = m_EntryProcessingList[i];
                        if (processingInfo.type == VisualsProcessingType.Head)
                        {
                            EntryProcessor processor;
                            if (depth < m_Processors.Count)
                                processor = m_Processors[depth];
                            else
                            {
                                processor = new EntryProcessor();
                                m_Processors.Add(processor);
                            }

                            ++depth;
                            processor.Init(processingInfo.rootEntry, m_RenderTreeManager, processingInfo.renderData);
                            processor.ProcessHead();
                            CommandManipulator.ReplaceHeadCommands(m_RenderTreeManager, processingInfo.renderData, processor);
                        }
                        else
                        {
                            --depth;
                            EntryProcessor processor = m_Processors[depth];
                            processor.ProcessTail();
                            CommandManipulator.ReplaceTailCommands(m_RenderTreeManager, processingInfo.renderData, processor);
                        }
                    }

                    m_EntryProcessingList.Clear();
                    m_ModifierEntryProcessingList.Clear();

                    for (int i = 0; i < m_Processors.Count; ++i)
                        m_Processors[i].ClearReferences();

                }
            }


            public static void UpdateOpacityId(RenderData renderData, RenderTreeManager renderTreeManager)
            {
                using (k_UpdateOpacityIdMarker.Auto())
                {

                    if (renderData.headMesh != null)
                        DoUpdateOpacityId(renderData, renderTreeManager, renderData.headMesh);

                    if (renderData.tailMesh != null)
                        DoUpdateOpacityId(renderData, renderTreeManager, renderData.tailMesh);

                    if (renderData.hasExtraMeshes)
                    {
                        ExtraRenderData extraData = renderTreeManager.GetOrAddExtraData(renderData);
                        BasicNode<MeshHandle> extraMesh = extraData.extraMesh;
                        while (extraMesh != null)
                        {
                            DoUpdateOpacityId(renderData, renderTreeManager, extraMesh.data);
                            extraMesh = extraMesh.next;
                        }
                    }

                }
            }

            static void DoUpdateOpacityId(RenderData renderData, RenderTreeManager renderTreeManager, MeshHandle mesh)
            {
                int vertCount = (int)mesh.allocVerts.size;
                RawSlice oldSlice = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
                renderTreeManager.device.Update(mesh, (uint)vertCount, out RawSlice newSlice);

                ushort opacityId = ShaderInfoAllocator.BMPAllocToId(renderData.opacityID);
                renderTreeManager.opacityIdAccelerator.CreateJob(
                    oldSlice.GetUnsafeReadOnlyPtr(), newSlice.GetUnsafePtr(), newSlice.Stride, opacityId, vertCount);
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
                    m_MeshGenerationContext.Dispose();
                    m_MeshGenerationContext = null;
                    m_MeshModifierScheduler.Dispose();
                    m_MeshModifierScheduler = null;
                    m_ChainCache.Dispose();
                }
                else DisposeHelper.NotifyMissingDispose(this);

                disposed = true;
            }

            #endregion // Dispose Pattern
        }
    }
}
