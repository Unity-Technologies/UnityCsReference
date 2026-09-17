// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEngine.UIElements;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler.UI
{
    [UxmlElement]
    partial class PhysicsCore2DStatisticView : VisualElement
    {
        const string k_UXML = "PhysicsCore2D/Profiler/PhysicsCore2DStatisticView/PhysicsCore2DStatisticView.uxml";

        // Raised when the Live Update toggle changes, so the owner can refresh immediately instead of waiting for the next frame change.
        public event Action liveUpdateChanged;

        Label m_BodyCountLabel;
        Label m_ShapeCountLabel;
        Label m_ContactCountLabel;
        Label m_JointCountLabel;
        Label m_IslandCountLabel;
        Label m_StackUsedLabel;
        Label m_MemoryUsedLabel;
        Label m_TaskCountLabel;
        Label m_BroadphaseHeightLabel;
        Label m_StaticBroadphaseHeightLabel;
        Toggle m_LiveUpdate;

        public PhysicsCore2DStatisticView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);

            m_BodyCountLabel = this.Q<Label>("BodyCountLabel");
            m_ShapeCountLabel = this.Q<Label>("ShapeCountLabel");
            m_ContactCountLabel = this.Q<Label>("ContactCountLabel");
            m_JointCountLabel = this.Q<Label>("JointCountLabel");
            m_IslandCountLabel = this.Q<Label>("IslandCountLabel");
            m_StackUsedLabel = this.Q<Label>("StackUsedLabel");
            m_MemoryUsedLabel = this.Q<Label>("MemoryUsedLabel");
            m_TaskCountLabel = this.Q<Label>("TaskCountLabel");
            m_BroadphaseHeightLabel = this.Q<Label>("BroadphaseHeightLabel");
            m_StaticBroadphaseHeightLabel = this.Q<Label>("StaticBroadphaseHeightLabel");
            m_LiveUpdate = this.Q<Toggle>("EnableStatisticsToggle");
            m_LiveUpdate.RegisterValueChangedCallback(_ => liveUpdateChanged?.Invoke());
        }

        public void SetStatistic(Unity.U2D.Physics.PhysicsWorld.WorldCounters counter)
        {
            m_BodyCountLabel.text = counter.bodyCount.ToString();
            m_ShapeCountLabel.text = counter.shapeCount.ToString();
            m_ContactCountLabel.text = counter.contactCount.ToString();
            m_JointCountLabel.text = counter.jointCount.ToString();
            m_IslandCountLabel.text = counter.islandCount.ToString();
            m_StackUsedLabel.text = FormatBytes(counter.stackUsed);
            m_MemoryUsedLabel.text = FormatBytes(counter.usedMemory);
            m_TaskCountLabel.text = counter.taskCount.ToString();
            m_BroadphaseHeightLabel.text = counter.broadphaseHeight.ToString();
            m_StaticBroadphaseHeightLabel.text = counter.staticBroadphaseHeight.ToString();
        }

        string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0f:F1} KB";
            return $"{bytes / (1024.0f * 1024.0f):F2} MB";
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_LiveUpdate.value;
        }
    }
}
