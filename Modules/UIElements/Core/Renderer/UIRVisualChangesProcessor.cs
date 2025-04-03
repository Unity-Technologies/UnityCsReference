// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
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

            static readonly ProfilerMarker k_GenerateEntriesMarker = new("UIR.GenerateEntries");
            static readonly ProfilerMarker k_ConvertEntriesToCommandsMarker = new("UIR.ConvertEntriesToCommands");
            static readonly ProfilerMarker k_UpdateOpacityIdMarker = new ("UIR.UpdateOpacityId");

            RenderTreeManager m_RenderTreeManager;
            MeshGenerationContext m_MeshGenerationContext;
            BaseElementBuilder m_ElementBuilder;
            List<EntryProcessingInfo> m_EntryProcessingList;
            List<EntryProcessor> m_Processors;

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
                m_Processors = new List<EntryProcessor>(4);
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

                DepthFirstOnVisualsChanged(renderData, dirtyID, hierarchical, ref stats);
            }

            void DepthFirstOnVisualsChanged(RenderData renderData, uint dirtyID, bool hierarchical, ref ChainBuilderStats stats)
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

                if (renderData.owner is TextElement)
                    RenderEvents.UpdateTextCoreSettings(m_RenderTreeManager, renderData.owner);

                if ((renderData.owner.renderHints & RenderHints.DynamicColor) == RenderHints.DynamicColor)
                    RenderEvents.SetColorValues(m_RenderTreeManager, renderData.owner);

                var rootEntry = m_RenderTreeManager.entryPool.Get();
                rootEntry.type = EntryType.DedicatedPlaceholder;

                m_EntryProcessingList.Add(new EntryProcessingInfo
                {
                    type = VisualsProcessingType.Head,
                    renderData = renderData,
                    rootEntry = rootEntry
                });

                k_GenerateEntriesMarker.Begin();
                m_MeshGenerationContext.Begin(rootEntry, renderData.owner, renderData);
                m_ElementBuilder.Build(m_MeshGenerationContext);
                m_MeshGenerationContext.End();
                k_GenerateEntriesMarker.End();

                if (hierarchical)
                {
                    // Recurse on children
                    var child = renderData.firstChild;
                    while (child != null)
                    {
                        DepthFirstOnVisualsChanged(child, dirtyID, true, ref stats);
                        child = child.nextSibling;
                    }
                }

                m_EntryProcessingList.Add(new EntryProcessingInfo
                {
                    type = VisualsProcessingType.Tail,
                    renderData = renderData,
                    rootEntry = rootEntry
                });
            }

            // This can only be called when the element local and the parent world states are clean.
            static void UpdateWorldFlipsWinding(RenderData renderData)
            {
                bool flipsWinding = renderData.localFlipsWinding;
                bool parentFlipsWinding = renderData.parent?.worldFlipsWinding ?? false;
                renderData.worldFlipsWinding = parentFlipsWinding ^ flipsWinding;
            }

            public void ConvertEntriesToCommands(ref ChainBuilderStats stats)
            {
                k_ConvertEntriesToCommandsMarker.Begin();

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

                for (int i = 0; i < m_Processors.Count; ++i)
                    m_Processors[i].ClearReferences();

                k_ConvertEntriesToCommandsMarker.End();
            }


            public static void UpdateOpacityId(RenderData renderData, RenderTreeManager renderTreeManager)
            {
                k_UpdateOpacityIdMarker.Begin();

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

                k_UpdateOpacityIdMarker.End();
            }

            static void DoUpdateOpacityId(RenderData renderData, RenderTreeManager renderTreeManager, MeshHandle mesh)
            {
                int vertCount = (int)mesh.allocVerts.size;
                NativeSlice<Vertex> oldVerts = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, vertCount);
                renderTreeManager.device.Update(mesh, (uint)vertCount, out NativeSlice<Vertex> newVerts);
                Color32 opacityData = renderTreeManager.shaderInfoAllocator.OpacityAllocToVertexData(renderData.opacityID);
                renderTreeManager.opacityIdAccelerator.CreateJob(oldVerts, newVerts, opacityData, vertCount);
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
                }
                else DisposeHelper.NotifyMissingDispose(this);

                disposed = true;
            }

            #endregion // Dispose Pattern
        }
    }
}
