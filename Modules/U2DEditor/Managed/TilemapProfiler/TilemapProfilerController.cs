// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Collections;

namespace UnityEditor.U2D.Profiling
{
    class TilemapProfilerController : ProfilerModuleViewController
    {
        TilemapProfilerView m_Root;
        int[] m_TilemapPhysicsMarkerIds = new int[TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames.Length];
        int[] m_TilemapSystemMarkerIds = new int[TilemapProfilerMarkers.s_TilemapSystemMarkerNames.Length];
        int[] m_TilemapRendererMarkerIds = new int[TilemapProfilerMarkers.s_TilemapRendererMarkerNames.Length];
        int[] m_TilemapRendererIndividualModeMarkerIds = new int[TilemapProfilerMarkers.s_TilemapRendererIndividualModeMarkerNames.Length];
        int[] m_TilemapRendererSRPBatchModeMarkerIds = new int[TilemapProfilerMarkers.s_TilemapRendererSRPBatchModeMarkerNames.Length];
        int[] m_TilemapRendererChunkModeMarkerIds = new int[TilemapProfilerMarkers.s_TilemapRendererChunkModeMarkerNames.Length];

        public TilemapProfilerController(ProfilerWindow profilerWindow)
            : base(profilerWindow)
        {
            profilerWindow.SelectedFrameIndexChanged += OnProfilerFrameChange;
        }

        void OnProfilerFrameChange(long obj)
        {
            if (Event.current != null && Event.current.type == EventType.Layout)
                return;

            CreateRootIfNotExist();

            if (UnityEngine.Profiling.Profiler.enabled && !m_Root.IsLiveUpdateEnabled())
                return;

            int selectedFrameIndexInt32 = Convert.ToInt32(obj);
            int id = 100;
            using (RawFrameDataView frameData = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(selectedFrameIndexInt32, 0))
            {
                NativeArray<TilemapChunkInfo> tilemapItems = new NativeArray<TilemapChunkInfo>();
                try
                {
                    tilemapItems = Profiler2D.GetTilemapChunkInfo(frameData);
                }
                catch (Exception)
                {
                    // do nothing. most likley there is no frame data at this point.
                    return;
                }

                Dictionary<EntityId, TilemapHierarchyNodeData> tilemapChunkDataDic = new Dictionary<EntityId, TilemapHierarchyNodeData>();
                for (int i = 0; i < tilemapItems.Length; i++)
                {
                    TilemapChunkInfo item = tilemapItems[i];
                    string name = "Unknown";
                    if (frameData.GetUnityObjectInfo(item.tilemapEntityId, out FrameDataView.UnityObjectInfo unityObjectInfo))
                    {
                        name = unityObjectInfo.name;
                    }

                    TilemapHierarchyNodeData node = null;
                    if (!tilemapChunkDataDic.TryGetValue(item.tilemapEntityId, out node))
                    {
                        node = new TilemapHierarchyNodeData()
                        {
                            name = name,
                            entityId = item.tilemapEntityId,
                            id = id++
                        };
                        tilemapChunkDataDic[item.tilemapEntityId] = node;
                    }
                    node.chunkRecord.Add(new TilemapChunkRecord()
                    {
                        chunkIdX = item.chunkIdX,
                        chunkIdY = item.chunkIdY,
                        meshCountValue = item.meshCount,
                        name = $" Chunk {item.chunkIdX}, {item.chunkIdY}",
                        entityId = item.tilemapEntityId,
                        id = id++
                    });
                    node.meshCountValue += item.meshCount;
                    node.chunkCountValue += 1;
                }

                m_Root.SetHierarchyData(tilemapChunkDataDic.Values);

                float tilemapPhysicsMarkerTime = 0f;
                for(int i = 0; i < TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames.Length; i++)
                {
                    m_TilemapPhysicsMarkerIds[i] = frameData.GetMarkerId(TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames[i]);
                    if (m_TilemapPhysicsMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                float tilemapSystemMarkerTime = 0f;
                for(int i = 0; i < TilemapProfilerMarkers.s_TilemapSystemMarkerNames.Length; i++)
                {
                    m_TilemapSystemMarkerIds[i] = frameData.GetMarkerId(TilemapProfilerMarkers.s_TilemapSystemMarkerNames[i]);
                    if (m_TilemapSystemMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                float tilemapRendererMarkerTime = 0f;
                for(int i = 0; i < TilemapProfilerMarkers.s_TilemapRendererMarkerNames.Length; i++)
                {
                    m_TilemapRendererMarkerIds[i] = frameData.GetMarkerId(TilemapProfilerMarkers.s_TilemapRendererMarkerNames[i]);
                    if (m_TilemapRendererMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{TilemapProfilerMarkers.s_TilemapRendererMarkerNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                float tilemapRendererIndividualModeMarkerTime = 0f;
                for (int i = 0; i < TilemapProfilerMarkers.s_TilemapRendererIndividualModeMarkerNames.Length; i++)
                {
                    m_TilemapRendererIndividualModeMarkerIds[i] = frameData.GetMarkerId(TilemapProfilerMarkers.s_TilemapRendererIndividualModeMarkerNames[i]);
                    if (m_TilemapRendererIndividualModeMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{TilemapProfilerMarkers.s_TilemapRendererIndividualModeMarkerNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                float tilemapRendererSRPBatchModeMarkerTime = 0f;
                for (int i = 0; i < TilemapProfilerMarkers.s_TilemapRendererSRPBatchModeMarkerNames.Length; i++)
                {
                    m_TilemapRendererSRPBatchModeMarkerIds[i] = frameData.GetMarkerId(TilemapProfilerMarkers.s_TilemapRendererSRPBatchModeMarkerNames[i]);
                    if (m_TilemapRendererSRPBatchModeMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{TilemapProfilerMarkers.s_TilemapRendererSRPBatchModeMarkerNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }

                float tilemapRendererChunkModeMarkerTime = 0f;
                for(int i = 0; i < TilemapProfilerMarkers.s_TilemapRendererChunkModeMarkerNames.Length; i++)
                {
                    m_TilemapRendererChunkModeMarkerIds[i] = frameData.GetMarkerId(TilemapProfilerMarkers.s_TilemapRendererChunkModeMarkerNames[i]);
                    if (m_TilemapRendererChunkModeMarkerIds[i] == FrameDataView.invalidMarkerId)
                    {
                        Debug.LogWarning($"{TilemapProfilerMarkers.s_TilemapRendererChunkModeMarkerNames[i]} marker id is invalid, please make sure the marker name is correct and the marker is properly registered.");
                    }
                }


                int sampleCount = frameData.sampleCount;
                for (int i = 0; i < sampleCount; ++i)
                {
                    var markerId = frameData.GetSampleMarkerId(i);
                    float markerTime = frameData.GetSampleTimeMs(i);
                    int j = 0;
                    for (j = 0; j < TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames.Length; j++)
                    {
                        if (markerId == m_TilemapPhysicsMarkerIds[j])
                        {
                            tilemapPhysicsMarkerTime += markerTime;
                            break;
                        }
                    }
                    if(j < TilemapProfilerMarkers.s_TilemapPhysicsMarkerNames.Length)
                        continue;
                    for (j = 0; j < TilemapProfilerMarkers.s_TilemapSystemMarkerNames.Length; j++)
                    {
                        if (markerId == m_TilemapSystemMarkerIds[j])
                        {
                            tilemapSystemMarkerTime += markerTime;
                            break;
                        }
                    }
                    if(j< TilemapProfilerMarkers.s_TilemapSystemMarkerNames.Length)
                        continue;

                    for (j = 0; j < TilemapProfilerMarkers.s_TilemapRendererMarkerNames.Length; j++)
                    {
                        if (markerId == m_TilemapRendererMarkerIds[j])
                        {
                            tilemapRendererMarkerTime += markerTime;
                            break;
                        }
                    }
                    if(j < TilemapProfilerMarkers.s_TilemapRendererMarkerNames.Length)
                        continue;


                    for (j = 0; j < TilemapProfilerMarkers.s_TilemapRendererIndividualModeMarkerNames.Length; j++)
                    {
                        if (markerId == m_TilemapRendererIndividualModeMarkerIds[j])
                        {
                            tilemapRendererIndividualModeMarkerTime += markerTime;
                            break;
                        }
                    }
                    if(j < TilemapProfilerMarkers.s_TilemapRendererIndividualModeMarkerNames.Length)
                        continue;

                    for (j = 0; j < TilemapProfilerMarkers.s_TilemapRendererSRPBatchModeMarkerNames.Length; j++)
                    {
                        if (markerId == m_TilemapRendererSRPBatchModeMarkerIds[j])
                        {
                            tilemapRendererSRPBatchModeMarkerTime += markerTime;
                            break;
                        }
                    }
                    if(j < TilemapProfilerMarkers.s_TilemapRendererSRPBatchModeMarkerNames.Length)
                        continue;


                    for (j = 0; j < TilemapProfilerMarkers.s_TilemapRendererChunkModeMarkerNames.Length; j++)
                    {
                        if (markerId == m_TilemapRendererChunkModeMarkerIds[j])
                        {
                            tilemapRendererChunkModeMarkerTime += markerTime;
                            break;
                        }
                    }
                    if(j < TilemapProfilerMarkers.s_TilemapRendererChunkModeMarkerNames.Length)
                        continue;
                }

                int counterMarkerId = frameData.GetMarkerId(TilemapProfilerMarkers.k_TilemapCounterName);
                long totalTilemap = frameData.GetCounterValueAsLong(counterMarkerId);
                counterMarkerId = frameData.GetMarkerId(TilemapProfilerMarkers.k_TilemapChunkCounterName);
                long totalChunks = frameData.GetCounterValueAsLong(counterMarkerId);
                counterMarkerId = frameData.GetMarkerId(TilemapProfilerMarkers.k_TilemapChunkMeshesName);
                long totalMesh = frameData.GetCounterValueAsLong(counterMarkerId);
                m_Root.SetStatistic(tilemapPhysicsMarkerTime,
                tilemapSystemMarkerTime,
                tilemapRendererMarkerTime,
                tilemapRendererIndividualModeMarkerTime,
                tilemapRendererSRPBatchModeMarkerTime,
                tilemapRendererChunkModeMarkerTime,
                totalTilemap, totalChunks, totalMesh);
            }
        }

        TilemapProfilerView CreateRootIfNotExist()
        {
            if (m_Root == null)
            {
                m_Root = new TilemapProfilerView();
            }
            return m_Root;
        }

        protected override VisualElement CreateView()
        {
            OnProfilerFrameChange(this.ProfilerWindow.selectedFrameIndex);
            return CreateRootIfNotExist();
        }
    }
}
