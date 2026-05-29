// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    [UxmlElement]
    partial class TilemapProfilerView : VisualElement
    {
        const string k_UXML = "U2DEditor/TilemapProfiler/TilemapProfilerView/TilemapProfilerView.uxml";

        TilemapHierarchyView m_TilemapHierarchyView;
        TilemapStatisticView m_TilemapStatisticView;

        public TilemapProfilerView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);
            TwoPaneSplitView splitView = this.Q<TwoPaneSplitView>();
            splitView.fixedPaneInitialDimension = 300;
            splitView.fixedPaneIndex = 0;
            m_TilemapStatisticView = new TilemapStatisticView();
            m_TilemapHierarchyView = new TilemapHierarchyView();
            splitView.Add(m_TilemapStatisticView);
            splitView.Add(m_TilemapHierarchyView);
        }

        public void SetHierarchyData(IEnumerable<TilemapHierarchyNodeData> values)
        {
            m_TilemapHierarchyView.SetData(values);
        }

        public void SetStatistic(float tilemapPhysicsTime, float tilemapSystemTime, float tilemapRendererTime, float tilemapRendererIndividualTime, float tilemapRendererSRPBatchTime, float tilemapRendererChunkTime, long tilemapCount, long totalChunks, long totalMeshes)
        {
            m_TilemapStatisticView.SetStatistic(tilemapPhysicsTime, tilemapSystemTime, tilemapRendererTime, tilemapRendererIndividualTime, tilemapRendererSRPBatchTime, tilemapRendererChunkTime, tilemapCount, totalChunks, totalMeshes);
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_TilemapStatisticView.IsLiveUpdateEnabled();
        }
    }
}
