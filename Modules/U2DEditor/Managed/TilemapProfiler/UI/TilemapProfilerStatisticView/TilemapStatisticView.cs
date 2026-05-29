// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    [UxmlElement]
    partial class TilemapStatisticView : VisualElement
    {
        const string k_UXML = "U2DEditor/TilemapProfiler/TilemapProfilerStatisticView/TilemapStatisticView.uxml";

        Label m_TilemapPhysicsTimeLabel;
        Label m_TilemapSystemTimeLabel;
        Label m_TilemapRendererTimeLabel;
        Label m_TilemapRendererIndividualTimeLabel;
        Label m_TilemapRendererSRPBatchTimeLabel;
        Label m_TilemapRendererChunkTimeLabel;
        Label m_TilemapCountLabel;
        Label m_TotalChunksLabel;
        Label m_TotalMeshesLabel;
        Toggle m_LiveUpdate;
        public TilemapStatisticView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);

            // Query and cache label references
            m_TilemapPhysicsTimeLabel = this.Q<Label>("TilemapPhysicsTimeLabel");
            m_TilemapSystemTimeLabel = this.Q<Label>("TilemapSystemTimeLabel");
            m_TilemapRendererTimeLabel = this.Q<Label>("TilemapRendererTimeLabel");
            m_TilemapRendererIndividualTimeLabel = this.Q<Label>("TilemapRendererIndividualTimeLabel");
            m_TilemapRendererSRPBatchTimeLabel = this.Q<Label>("TilemapRendererSRPBatchTimeLabel");
            m_TilemapRendererChunkTimeLabel = this.Q<Label>("TilemapRendererChunkTimeLabel");
            m_TilemapCountLabel = this.Q<Label>("TilemapCountLabel");
            m_TotalChunksLabel = this.Q<Label>("TotalChunksLabel");
            m_TotalMeshesLabel = this.Q<Label>("TotalMeshesLabel");
            m_LiveUpdate = this.Q<Toggle>("EnableStatisticsToggle");
        }

        public void SetStatistic(float tilemapPhysicsTime, float tilemapSystemTime, float tilemapRendererTime, float tilemapRendererIndividualTime, float tilemapRendererSRPBatchTime, float tilemapRendererChunkTime, long tilemapCount, long totalChunks, long totalMeshes)
        {
            m_TilemapPhysicsTimeLabel.text = $"{tilemapPhysicsTime:F2}ms";
            m_TilemapSystemTimeLabel.text = $"{tilemapSystemTime:F2}ms";
            m_TilemapRendererTimeLabel.text = $"{tilemapRendererTime:F2}ms";
            m_TilemapRendererIndividualTimeLabel.text = $"{tilemapRendererIndividualTime:F2}ms";
            m_TilemapRendererSRPBatchTimeLabel.text = $"{tilemapRendererSRPBatchTime:F2}ms";
            m_TilemapRendererChunkTimeLabel.text = $"{tilemapRendererChunkTime:F2}ms";
            m_TilemapCountLabel.text = tilemapCount.ToString();
            m_TotalChunksLabel.text = totalChunks.ToString();
            m_TotalMeshesLabel.text = totalMeshes.ToString();
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_LiveUpdate.value;
        }
    }
}
